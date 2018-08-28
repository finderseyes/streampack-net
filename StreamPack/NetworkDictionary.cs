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
    /// 
    /// </summary>
    /// <typeparam name="TKey"></typeparam>
    /// <typeparam name="TValue"></typeparam>
    public class NetworkDictionary<TKey, TValue> : NotifyModifiedBase, IUpdateSerializable, INotifyModified
    {
        internal static readonly bool TypeIsUpdateSerializable = typeof(IUpdateSerializable).IsAssignableFrom(typeof(TValue));
        internal static readonly bool TypeIsSerializable = typeof(ISerializable).IsAssignableFrom(typeof(TValue));
        internal static readonly bool TypeIsNotifyModified = typeof(INotifyModified).IsAssignableFrom(typeof(TValue));

        /// <summary>
        /// 
        /// </summary>
        internal enum EntryUpdateOperation : byte
        {
            AddOrReset,
            Remove,
        }

        /// <summary>
        /// 
        /// </summary>
        internal class EntryUpdate
        {
            public TKey Key { get; private set; }
            public EntryUpdateOperation Operation { get; set; }
            public bool KeyExistedBeforeUpdate { get; set; }

            /// <summary>
            /// 
            /// </summary>
            /// <param name="key"></param>
            public EntryUpdate(TKey key)
            {
                this.Key = key;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        internal class Update
        {
            private NetworkDictionary<TKey, TValue> _owner;

            public bool IsClearedFirst { get; set; }
            public Dictionary<TKey, EntryUpdate> EntryUpdates { get; private set; }

            public Update(NetworkDictionary<TKey, TValue> owner)
            {
                _owner = owner;
                this.EntryUpdates = new Dictionary<TKey, EntryUpdate>();
            }

            /// <summary>
            /// 
            /// </summary>
            /// <param name="key"></param>
            /// <returns></returns>
            public EntryUpdate GetEntryUpdate(TKey key)
            {
                EntryUpdate entryUpdate;

                if (!this.EntryUpdates.TryGetValue(key, out entryUpdate))
                {
                    entryUpdate = new EntryUpdate(key);
                    entryUpdate.KeyExistedBeforeUpdate = _owner.ContainsKey(key);
                    this.EntryUpdates.Add(key, entryUpdate);
                }

                return entryUpdate;
            }

            /// <summary>
            /// 
            /// </summary>
            public bool HasUpdates
            {
                get
                {
                    return (this.IsClearedFirst || EntryUpdates.Count > 0);
                }
            }

            /// <summary>
            /// 
            /// </summary>
            /// <param name="key"></param>
            public void RemoveEntryUpdate(TKey key)
            {
                EntryUpdates.Remove(key);
            }

            /// <summary>
            /// 
            /// </summary>
            public void Clear()
            {
                this.IsClearedFirst = false;
                this.EntryUpdates.Clear();
            }
        }

        #region [ Private Data ]
        readonly internal Dictionary<TKey, TValue> _data;
        readonly internal Update _updates;
        readonly internal Dictionary<TKey, Action<ModificationEventArgs>> _entryValueModifiedCallbacks;
        readonly internal HashSet<TKey> _modifiedEntries;
        #endregion

        /// <summary>
        /// 
        /// </summary>
        public NetworkDictionary()
        {
            _data = new Dictionary<TKey, TValue>();
            _updates = new Update(this);
            _entryValueModifiedCallbacks = new Dictionary<TKey, Action<ModificationEventArgs>>();
            _modifiedEntries = new HashSet<TKey>();
        }

        /// <summary>
        /// 
        /// </summary>
        protected Dictionary<TKey, TValue> Data
        {
            get { return _data; }
        }

        #region [ Dictionary ]

        /// <summary>
        /// 
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public TValue Get(TKey key)
        {
            return _data[key];
        }

        /// <summary>
        /// Try gets value from dictinary.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public bool TryGet(TKey key, out TValue value)
        {
            return _data.TryGetValue(key, out value);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        public void Add(TKey key, TValue value)
        {
            if (ContainsKey(key))
                throw new ArgumentException("An element with the same key already exists");

            var entryUpdate = _updates.GetEntryUpdate(key);
            entryUpdate.Operation = EntryUpdateOperation.AddOrReset;

            // Actually add.
            _data.Add(key, value);
            InstallEntryHooks(key, value);

            OnEntryAdded(key, value);

            RaiseModifiedEvent();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        protected virtual void OnEntryAdded(TKey key, TValue value)
        {
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="key"></param>
        /// <param name="oldValue"></param>
        protected virtual void OnEntryReset(TKey key, TValue oldValue, TValue newValue)
        {

        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        public void Set(TKey key, TValue value)
        {
            if (ContainsKey(key))
            {
                var previousValue = _data[key];
                if ((previousValue == null && value != null) || (previousValue != null && !previousValue.Equals(value)))
                {
                    var entryUpdate = _updates.GetEntryUpdate(key);
                    entryUpdate.Operation = EntryUpdateOperation.AddOrReset;

                    // Actually add.
                    _data[key] = value;
                    UninstallEntryHooks(key, previousValue);
                    InstallEntryHooks(key, value);
                    OnEntryReset(key, previousValue, value);
                    RaiseModifiedEvent();
                }
            }
            else
            {
                var entryUpdate = _updates.GetEntryUpdate(key);
                entryUpdate.Operation = EntryUpdateOperation.AddOrReset;

                // Actually add.
                _data[key] = value;
                InstallEntryHooks(key, value);
                OnEntryAdded(key, value);
                RaiseModifiedEvent();
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="value"></param>
        private void InstallEntryHooks(TKey key, TValue value)
        {
            if (TypeIsUpdateSerializable && value != null)
            {
                Action<ModificationEventArgs> callback = (e) =>
                {
                    _modifiedEntries.Add(key);
                    RaiseModifiedEvent(e);
                };

                _entryValueModifiedCallbacks.Add(key, callback);

                INotifyModified notifyModified = value as INotifyModified;
                notifyModified.Modified += callback;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        private void UninstallEntryHooks(TKey key, TValue value)
        {
            if (TypeIsUpdateSerializable && value != null)
            {
                Action<ModificationEventArgs> callback;

                if (!_entryValueModifiedCallbacks.TryGetValue(key, out callback))
                    throw new Exception("Cannot find entry callback.");

                _entryValueModifiedCallbacks.Remove(key);

                INotifyModified notifyModified = value as INotifyModified;
                notifyModified.Modified -= callback;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public bool Remove(TKey key)
        {
            if (ContainsKey(key))
            {
                var entryUpdate = _updates.GetEntryUpdate(key);
                if (entryUpdate.KeyExistedBeforeUpdate)
                    entryUpdate.Operation = EntryUpdateOperation.Remove;
                else
                    _updates.RemoveEntryUpdate(key);

                // Actually remove.
                var value = _data[key];
                _data.Remove(key);
                UninstallEntryHooks(key, value);
                OnEntryRemoved(key, value);
                RaiseModifiedEvent();

                return true;
            }
            else
                return false;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        protected virtual void OnEntryRemoved(TKey key, TValue value)
        {

        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public bool ContainsKey(TKey key)
        {
            return _data.ContainsKey(key);
        }

        /// <summary>
        /// 
        /// </summary>
        public int Count
        {
            get { return _data.Count; }
        }

        /// <summary>
        /// 
        /// </summary>
        public IEnumerable<TKey> Keys
        {
            get { return _data.Keys; }
        }

        /// <summary>
        /// 
        /// </summary>
        public IEnumerable<TValue> Values
        {
            get { return _data.Values; }
        }

        /// <summary>
        /// 
        /// </summary>
        public void Clear()
        {
            var pairs = _data.ToList();

            _updates.Clear();
            _updates.IsClearedFirst = true;
            _data.Clear();

            foreach (var pair in pairs)
                UninstallEntryHooks(pair.Key, pair.Value);

            RaiseModifiedEvent();
        }

        #endregion

        #region [ IUpdateSerializable ]

        /// <summary>
        /// Determines if the object has updates.
        /// </summary>
        public bool HasUpdates
        {
            get
            {
                return _updates.HasUpdates || _modifiedEntries.Count > 0;
            }
        }

        /// <summary>
        /// Clears the object's updates.
        /// </summary>
        public void ClearUpdates()
        {
            _updates.Clear();
            _modifiedEntries.Clear();

            if (TypeIsUpdateSerializable)
            {
                foreach (var item in _data.Values)
                    ((IUpdateSerializable)item).ClearUpdates();
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        protected virtual byte[] SerializeValue(TValue value)
        {
            if (TypeIsSerializable)
            {
                var serializable = value as ISerializable;
                var serializableObjectData = new SerializableObjectData
                {
                    Data = serializable != null ? serializable.Serialize() : null
                };

                return MessagePackSerializer.Serialize(serializableObjectData);
            }
            else
                return MessagePackSerializer.Serialize(value);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        protected virtual TValue DeserializeValue(byte[] data)
        {
            if (TypeIsSerializable)
            {
                var serializableObjectData = MessagePackSerializer.Deserialize<SerializableObjectData>(data);
                TValue value;

                if (serializableObjectData.Data == null)
                {
                    // NOTE: default(T) is null.
                    value = default(TValue);
                }
                else
                {
                    value = Activator.CreateInstance<TValue>();
                    ((ISerializable)value).Deserialize(serializableObjectData.Data);
                }

                return value;
            }
            else
                return MessagePackSerializer.Deserialize<TValue>(data);
        }

        /// <summary>
        /// Serializes current object to a byte buffer.
        /// </summary>
        /// <returns>Byte buffer.</returns>
        public virtual byte[] Serialize()
        {
            var entryDataItems = _data.Select(pair =>
            {
                var item = new NetworkDictionaryEntryData
                {
                    KeyData = MessagePackSerializer.Serialize(pair.Key),
                    ValueData = SerializeValue(pair.Value)
                };

                return item;
            }).ToArray();

            return MessagePackSerializer.Serialize(entryDataItems);
        }

        /// <summary>
        /// Deserializes current object from given byte buffer.
        /// </summary>
        /// <param name="data">Byte buffer.</param>
        public virtual void Deserialize(byte[] data)
        {
            var entryDataItems = MessagePackSerializer.Deserialize<NetworkDictionaryEntryData[]>(data);

            this.Clear();
            this.ClearUpdates();

            foreach (var item in entryDataItems)
            {
                var key = MessagePackSerializer.Deserialize<TKey>(item.KeyData);
                var value = DeserializeValue(item.ValueData);

                Add(key, value);
            }
        }

        /// <summary>
        /// Serializes the object's updates to a byte buffer.
        /// </summary>
        /// <returns>Byte buffer.</returns>
        public virtual byte[] SerializeUpdates()
        {
            var updateData = new NetworkDictionaryUpdateData();
            updateData.IsClearedFirst = _updates.IsClearedFirst;

            // Entry updates
            var entryUpdates = _updates.EntryUpdates;
            if (entryUpdates.Count > 0)
            {
                updateData.EntryUpdateDataItems = entryUpdates.Select(pair =>
                {
                    var entryUpdateDataItem = new NetworkDictionaryEntryUpdateData
                    {
                        KeyData = MessagePackSerializer.Serialize(pair.Key),
                        Operation = (byte)pair.Value.Operation,
                    };

                    if (pair.Value.Operation == EntryUpdateOperation.AddOrReset)
                        entryUpdateDataItem.ValueData = SerializeValue(_data[pair.Key]);

                    return entryUpdateDataItem;
                }).ToArray();
            }

            // Existing entries
            if (TypeIsUpdateSerializable)
            {
                var partiallyChangedEntries = GetPartiallyModifiedUpdateSerializableEntries();
                updateData.EntryPartialUpdateDataItems = partiallyChangedEntries.Select(pair =>
                {
                    var updateSerialiable = (IUpdateSerializable)pair.Value;
                    var partialUpdateDataItem = new NetworkDictionaryEntryPartialUpdateData
                    {
                        KeyData = MessagePackSerializer.Serialize(pair.Key),
                        Data = updateSerialiable.SerializeUpdates()
                    };

                    return partialUpdateDataItem;
                }).ToArray();
            }

            return MessagePackSerializer.Serialize(updateData);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        protected IEnumerable<KeyValuePair<TKey, TValue>> GetPartiallyModifiedUpdateSerializableEntries()
        {
            if (TypeIsUpdateSerializable)
            {
                var entryUpdates = _updates.EntryUpdates;
                return _data
                    .Where(pair =>
                    {
                        var updateSerialiable = (IUpdateSerializable)pair.Value;
                        return (!entryUpdates.ContainsKey(pair.Key) && updateSerialiable != null && _modifiedEntries.Contains(pair.Key));
                    });
            }

            return Enumerable.Empty<KeyValuePair<TKey, TValue>>();
        }

        /// <summary>
        /// Deserializes the object's updates from a byte buffer.
        /// </summary>
        /// <param name="data">Byte buffer.</param>
        public virtual void DeserializeUpdates(byte[] data)
        {
            var updateData = MessagePackSerializer.Deserialize<NetworkDictionaryUpdateData>(data);

            if (updateData.IsClearedFirst)
                Clear();

            if (updateData.EntryUpdateDataItems != null && updateData.EntryUpdateDataItems.Length > 0)
            {
                foreach (var item in updateData.EntryUpdateDataItems)
                {
                    var key = MessagePackSerializer.Deserialize<TKey>(item.KeyData);
                    var operation = (EntryUpdateOperation)item.Operation;
                    if (operation == EntryUpdateOperation.AddOrReset)
                    {
                        var value = DeserializeValue(item.ValueData);
                        Set(key, value);
                    }
                    else if (operation == EntryUpdateOperation.Remove)
                    {
                        Remove(key);
                    }
                }
            }

            if (TypeIsUpdateSerializable)
            {
                if (updateData.EntryPartialUpdateDataItems != null && updateData.EntryPartialUpdateDataItems.Length > 0)
                {
                    foreach (var item in updateData.EntryPartialUpdateDataItems)
                    {
                        var key = MessagePackSerializer.Deserialize<TKey>(item.KeyData);
                        var value = (IUpdateSerializable)_data[key];
                        value.DeserializeUpdates(item.Data);
                    }
                }
            }
        }

        #endregion
    }

    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="TKey"></typeparam>
    /// <typeparam name="TValue"></typeparam>
    /// <typeparam name="TValueContainer"></typeparam>
    /// <typeparam name="TValueUpdateContainer"></typeparam>
    public class NetworkDictionary<TKey, TValue,
        TValueContainer, TContainer> :
        NetworkDictionary<TKey, TValue>, ISerializable<TContainer>
        where TValue : ISerializable<TValueContainer>
        where TContainer : DictionaryContainer<TKey, TValueContainer>
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

        /// <summary>
        /// 
        /// </summary>
        /// <param name="container"></param>
        public void DeserializeFromContainer(TContainer container)
        {
            this.Clear();
            this.ClearUpdates();

            for (int i = 0; i < container.Keys.Length; i++)
            {
                var key = container.Keys[i];
                var value = Activator.CreateInstance<TValue>();
                value.DeserializeFromContainer(container.Values[i]);

                Add(key, value);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public TContainer SerializeToContainer()
        {
            var container = Activator.CreateInstance<TContainer>();

            container.Keys = _data.Select(p => p.Key).ToArray();
            container.Values = _data.Select(p => p.Value.SerializeToContainer()).ToArray();

            return container;
        }
    }

    public class NetworkDictionary<TKey, TValue,
        TValueContainer, TValueUpdateContainer,
        TContainer, TUpdateContainer> :
        NetworkDictionary<TKey, TValue, TValueContainer, TContainer>, IUpdateSerializable<TContainer, TUpdateContainer>
        where TValue : IUpdateSerializable<TValueContainer, TValueUpdateContainer>
        where TContainer : DictionaryContainer<TKey, TValueContainer>
        where TUpdateContainer : DictionaryUpdateContainer<TKey, TValueContainer, TValueUpdateContainer>
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

        public TUpdateContainer SerializeUpdatesToContainer()
        {
            var container = Activator.CreateInstance<TUpdateContainer>();

            container.IsClearedFirst = (byte)(_updates.IsClearedFirst ? 1 : 0);

            // Entry updates
            var entryUpdates = _updates.EntryUpdates;
            if (entryUpdates.Count > 0)
            {
                container.EntryUpdateKeys = entryUpdates.Select(p => p.Key).ToArray();
                container.EntryUpdateOperations = entryUpdates.Select(p => (byte)p.Value.Operation).ToArray();
                container.EntryUpdateValues = entryUpdates.Select(p =>
                {
                    if (p.Value.Operation == EntryUpdateOperation.AddOrReset)
                    {
                        var value = _data[p.Key];
                        if (value != null)
                            return value.SerializeToContainer();
                    }

                    return default(TValueContainer);
                }).ToArray();
            }

            // Modified entries.
            var modifiedEntries = GetPartiallyModifiedUpdateSerializableEntries().ToList();
            container.ModifiedEntryKeys = modifiedEntries.Select(p => p.Key).ToArray();
            container.ModifiedEntryData = modifiedEntries.Select(p => p.Value.SerializeUpdatesToContainer()).ToArray();

            return container;
        }

        public void DeserializeUpdatesFromContainer(TUpdateContainer container)
        {
            if (container.IsClearedFirst != 0)
                this.Clear();

            if (container.EntryUpdateKeys != null)
            {
                for (int i = 0; i < container.EntryUpdateKeys.Length; i++)
                {
                    var key = container.EntryUpdateKeys[i];
                    var operation = (EntryUpdateOperation)container.EntryUpdateOperations[i];
                    if (operation == EntryUpdateOperation.AddOrReset)
                    {
                        var data = container.EntryUpdateValues[i];
                        if (data == null)
                            Set(key, default(TValue));
                        else
                        {
                            var value = Activator.CreateInstance<TValue>();
                            value.DeserializeFromContainer(container.EntryUpdateValues[i]);
                            Set(key, value);
                        }
                    }
                    else if (operation == EntryUpdateOperation.Remove)
                    {
                        Remove(key);
                    }
                }
            }

            if (container.ModifiedEntryKeys != null)
            {
                for (int i = 0; i < container.ModifiedEntryKeys.Length; i++)
                {
                    var key = container.ModifiedEntryKeys[i];
                    var value = _data[key];
                    value.DeserializeUpdatesFromContainer(container.ModifiedEntryData[i]);
                }
            }
        }
    }
}
