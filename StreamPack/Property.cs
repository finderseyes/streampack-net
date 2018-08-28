using StreamPack.Serialization;
using MessagePack;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StreamPack
{
    /// <summary>
    /// Base property.
    /// </summary>
    public abstract class Property : NotifyModifiedBase, IUpdateSerializable, INotifyModified
    {
        public byte Key { get; private set; }
        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="key"></param>
        protected Property(byte key)
        {
            this.Key = key;
        }

        protected abstract bool ValueHasUpdates { get; }

        #region [ INetworkUpdateSerializable ]

        public abstract bool HasUpdates { get; }

        public abstract void ClearUpdates();
        public abstract byte[] SerializeUpdates();
        public abstract void DeserializeUpdates(byte[] data);
        public abstract byte[] Serialize();
        public abstract void Deserialize(byte[] data);

        #endregion
    }

    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public sealed class Property<T> : Property
    {
        private enum UpdateOperation
        {
            /// <summary>
            /// The value referece is changed to another.
            /// </summary>
            ValueReset, 

            /// <summary>
            /// The value reference is the same, yet its content is modified.
            /// </summary>
            ValuePartialUpdate
        }

        #region [ Private Data ]

        private static readonly bool TypeIsUpdateSerializable = typeof(IUpdateSerializable).IsAssignableFrom(typeof(T));
        private static readonly bool TypeIsSerializable = typeof(ISerializable).IsAssignableFrom(typeof(T));
        private static readonly bool TypeIsNotifyModified = typeof(INotifyModified).IsAssignableFrom(typeof(T));

        private bool _hasUpdates;        
        private T _value;

        #endregion
        /// <summary>
        /// 
        /// </summary>
        public T Value
        {
            get { return _value; }
            set { SetValue(value); }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="key"></param>
        public Property(byte key, T value = default(T)) : base(key)
        {
            SetValueInternal(value);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="value"></param>
        public void SetValue(T value)
        {
            if ((value == null && _value != null) || (value != null && !value.Equals(this._value)))
            {
                _hasUpdates = true;
                SetValueInternal(value);
                RaiseModifiedEvent();
            }
        }        

        /// <summary>
        /// 
        /// </summary>
        /// <param name="value"></param>
        private void SetValueInternal(T value)
        {
            // Old value.
            if (TypeIsNotifyModified && _value != null)
            {
                INotifyModified notifyModified = _value as INotifyModified;
                notifyModified.Modified -= OnValueModified;
            }

            _value = value;

            // New value.
            if (TypeIsNotifyModified && _value != null)
            {
                INotifyModified notifyModified = _value as INotifyModified;
                notifyModified.Modified += OnValueModified;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="e"></param>
        private void OnValueModified(ModificationEventArgs e)
        {
            RaiseModifiedEvent(e);
        }

        /// <summary>
        /// 
        /// </summary>
        public override bool HasUpdates
        {
            get { return _hasUpdates || ValueHasUpdates; }
        }

        /// <summary>
        /// 
        /// </summary>
        protected override bool ValueHasUpdates
        {
            get
            {
                return (TypeIsUpdateSerializable && _value != null && ((IUpdateSerializable)_value).HasUpdates);
            }
        }

        /// <summary>
        /// Clears updates.
        /// </summary>
        public override void ClearUpdates()
        {
            _hasUpdates = false;
            if (TypeIsUpdateSerializable && _value != null)
                ((IUpdateSerializable)_value).ClearUpdates();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public override byte[] Serialize()
        {
            if (TypeIsSerializable)
            {
                return Serialize((ISerializable)_value);
            }
            else
                return MessagePackSerializer.Serialize(_value);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="serializable"></param>
        /// <returns></returns>
        private byte[] Serialize(ISerializable serializable)
        {
            var serializableObjectData = new SerializableObjectData
            {
                Data = serializable != null ? serializable.Serialize() : null
            };

            return MessagePackSerializer.Serialize(serializableObjectData);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="data"></param>
        public override void Deserialize(byte[] data)
        {
            if (TypeIsSerializable)
            {
                var serializableObjectData = MessagePackSerializer.Deserialize<SerializableObjectData>(data);
                T value;

                if (serializableObjectData.Data == null)
                {
                    // NOTE: default(T) is null.
                    value = default(T);
                }
                else
                {
                    value = Activator.CreateInstance<T>();
                    ((ISerializable)value).Deserialize(serializableObjectData.Data);
                }

                SetValue(value);
            }
            else
                SetValue(MessagePackSerializer.Deserialize<T>(data));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="data"></param>
        public override void DeserializeUpdates(byte[] data)
        {
            var updateOperationData = MessagePackSerializer.Deserialize<PropertyUpdateOperationData>(data);
            var operation = (UpdateOperation)updateOperationData.Operation;

            if (operation == UpdateOperation.ValueReset)
            {
                Deserialize(updateOperationData.Data);
            }
            else if (TypeIsUpdateSerializable && _value != null && operation == UpdateOperation.ValuePartialUpdate)
            {
                ((IUpdateSerializable)_value).DeserializeUpdates(updateOperationData.Data);
            }
            else
            {
                throw new Exception("Should not reach here.");
            }
        }
        
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public override byte[] SerializeUpdates()
        {
            if (_hasUpdates)
            {
                var updateOperationData = new PropertyUpdateOperationData
                {
                    Operation = (byte)UpdateOperation.ValueReset,
                    Data = this.Serialize()
                };

                return MessagePackSerializer.Serialize(updateOperationData);
            }
            else if (this.ValueHasUpdates)
            {
                var updateOperationData = new PropertyUpdateOperationData
                {
                    Operation = (byte)UpdateOperation.ValuePartialUpdate,
                    Data = ((IUpdateSerializable)_value).SerializeUpdates()
                };

                return MessagePackSerializer.Serialize(updateOperationData);                
            }
            else
            {
                throw new Exception("Should not reach here.");
            }
        }
    }
}
