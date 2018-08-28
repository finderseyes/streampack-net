using StreamPack.Serialization;
using MessagePack;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StreamPack
{
    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="TId"></typeparam>
    public abstract class NetworkObject : NotifyModifiedBase, IUpdateSerializable
    {
        #region [ Propeties ]

        private Dictionary<byte, Property> _properties = new Dictionary<byte, Property>();

        /// <summary>
        /// Registers a property.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="property"></param>
        protected Property<T> RegisterProperty<T>(Property<T> property)
        {
            if (property == null)
                throw new ArgumentNullException();

            if (!_properties.ContainsKey(property.Key))
            {
                _properties[property.Key] = property;
                property.Modified += OnPropertyModifiedInternal;

                return property;
            }
            else
            {
                throw new ArgumentException(string.Format("A property with key '{0}' already exists.", property.Key));
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="e"></param>
        private void OnPropertyModifiedInternal(ModificationEventArgs e)
        {
            var property = (Property)e.Source;
            OnPropertyModified(property);
            RaiseModifiedEvent(e);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="property"></param>
        protected virtual void OnPropertyModified(Property property)
        {
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public Property GetProperty(byte key)
        {
            return _properties[key];
        }

        /// <summary>
        /// 
        /// </summary>
        public int PropertyCount
        {
            get { return _properties.Count; }
        }

        #endregion

        /// <summary>
        /// 
        /// </summary>
        public virtual bool HasUpdates
        {
            get { return _properties.Any(p => p.Value.HasUpdates); }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="data"></param>
        public virtual void Deserialize(byte[] data)
        {
            var propertyDataItems = MessagePackSerializer.Deserialize<PropertyData[]>(data);
            foreach (var item in propertyDataItems)
            {
                var property = GetProperty(item.Key);
                property.Deserialize(item.Data);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="data"></param>
        public virtual void DeserializeUpdates(byte[] data)
        {
            var propertyUpdateDataItems = MessagePackSerializer.Deserialize<PropertyUpdateData[]>(data);
            foreach (var item in propertyUpdateDataItems)
            {
                var property = GetProperty(item.Key);
                property.DeserializeUpdates(item.Data);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public virtual byte[] Serialize()
        {
            var propertyDataItems = _properties.Select(pair =>
            {
                var item = new PropertyData
                {
                    Key = pair.Key,
                    Data = pair.Value.Serialize()
                };

                return item;
            }).ToArray();

            return MessagePackSerializer.Serialize(propertyDataItems);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public virtual byte[] SerializeUpdates()
        {
            var propertyUpdateDataItems = _properties
                .Where(pair => pair.Value.HasUpdates)
                .Select(pair =>
                {
                    var item = new PropertyUpdateData
                    {
                        Key = pair.Key,
                        Data = pair.Value.SerializeUpdates()
                    };

                    return item;
                }).ToArray();

            return MessagePackSerializer.Serialize(propertyUpdateDataItems);
        }

        /// <summary>
        /// 
        /// </summary>
        public virtual void ClearUpdates()
        {
            foreach (var property in _properties.Values)
                property.ClearUpdates();
        }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="TContainer"></typeparam>
    public abstract class NetworkObject<TContainer> : NetworkObject, ISerializable<TContainer>
    {
        public override byte[] Serialize()
        {
            TContainer container = SerializeToContainer();
            return MessagePackSerializer.Serialize(container);
        }

        public override void Deserialize(byte[] data)
        {
            var container = MessagePackSerializer.Deserialize<TContainer>(data);
            DeserializeFromContainer(container);
        }


        public abstract TContainer SerializeToContainer();
        public abstract void DeserializeFromContainer(TContainer container);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="TContainer"></typeparam>
    /// <typeparam name="TUpdateContainer"></typeparam>
    public abstract class NetworkObject<TContainer, TUpdateContainer> : 
        NetworkObject<TContainer>, IUpdateSerializable<TContainer, TUpdateContainer>
    {
        public override byte[] SerializeUpdates()
        {
            TUpdateContainer container = SerializeUpdatesToContainer();
            return MessagePackSerializer.Serialize(container);
        }

        public override void DeserializeUpdates(byte[] data)
        {
            var container = MessagePackSerializer.Deserialize<TUpdateContainer>(data);
            DeserializeUpdatesFromContainer(container);
        }

        public abstract void DeserializeUpdatesFromContainer(TUpdateContainer container);
        public abstract TUpdateContainer SerializeUpdatesToContainer();
    }
}
