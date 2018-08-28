using StreamPack.Serialization;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StreamPack
{
    /// <summary>
    /// A set of values distinguished by their ids.
    /// </summary>
    /// <typeparam name="TId">The id type</typeparam>
    /// <typeparam name="TValue">Value type</typeparam>
    public class NetworkSet<TId, TValue> : NotifyModifiedBase, IUpdateSerializable, IEnumerable<TValue>
        where TValue : IHasUniqueId<TId>
    {
        #region [ Private data ] 
        private NetworkDictionary<TId, TValue> _data;
        #endregion

        /// <summary>
        /// 
        /// </summary>
        protected NetworkDictionary<TId, TValue> Data
        {
            get { return _data; }
        }

        /// <summary>
        /// 
        /// </summary>
        public NetworkSet()
        {
            _data = CreateInternalData();
            _data.Modified += (e) => RaiseModifiedEvent(e.InnerSource);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        protected virtual NetworkDictionary<TId, TValue> CreateInternalData()
        {
            return new NetworkDictionary<TId, TValue>();
        }

        #region [ Set ] 

        /// <summary>
        /// Adds an item to the set.
        /// </summary>
        /// <param name="value"></param>
        public void Add(TValue value)
        {
            _data.Add(value.Id, value);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="id"></param>
        public bool Remove(TId id)
        {
            return _data.Remove(id);
        }

        /// <summary>
        /// 
        /// </summary>
        public void Clear()
        {
            _data.Clear();
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
        /// <param name="id"></param>
        /// <returns></returns>
        public bool Contains(TId id)
        {
            return _data.ContainsKey(id);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public TValue Get(TId id)
        {
            return _data.Get(id);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="id"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public bool TryGet(TId id, out TValue value)
        {
            return _data.TryGet(id, out value);
        }

        #endregion

        #region [ IEnumerable ]

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public IEnumerator<TValue> GetEnumerator()
        {
            return _data.Values.GetEnumerator();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable)_data.Values).GetEnumerator();
        }

        #endregion

        #region [ IUpdateSerializable ]

        public bool HasUpdates
        {
            get { return _data.HasUpdates; }
        }

        public void ClearUpdates()
        {
            _data.ClearUpdates();
        }

        public void Deserialize(byte[] data)
        {
            _data.Deserialize(data);
        }

        public void DeserializeUpdates(byte[] data)
        {
            _data.DeserializeUpdates(data);
        }

        public byte[] Serialize()
        {
            return _data.Serialize();
        }

        public byte[] SerializeUpdates()
        {
            return _data.SerializeUpdates();
        }

        #endregion
    }

    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="TId"></typeparam>
    /// <typeparam name="TValue"></typeparam>
    /// <typeparam name="TValueContainer"></typeparam>
    /// <typeparam name="TContainer"></typeparam>
    public class NetworkSet<TId, TValue, TValueContainer, TContainer> :
        NetworkSet<TId, TValue>, ISerializable<TContainer>
        where TValue : IHasUniqueId<TId>, ISerializable<TValueContainer>
        where TContainer : SetContainer<TId, TValueContainer>
    {
        private class InternalDictionary : NetworkDictionary<TId, TValue, TValueContainer, TContainer>
        {
            protected override void OnEntryAdded(TId key, TValue value)
            {
                base.OnEntryAdded(key, value);
                value.Id = key;
            }

            protected override void OnEntryReset(TId key, TValue oldValue, TValue newValue)
            {
                base.OnEntryReset(key, oldValue, newValue);
                newValue.Id = key;
            }
        }

        protected override NetworkDictionary<TId, TValue> CreateInternalData()
        {
            return new InternalDictionary();
        }

        public void DeserializeFromContainer(TContainer container)
        {
            ((ISerializable<TContainer>)Data).DeserializeFromContainer(container);
        }

        public TContainer SerializeToContainer()
        {
            return ((ISerializable<TContainer>)Data).SerializeToContainer();
        }
    }

    public class NetworkSet<TId, TValue,
        TValueContainer, TValueUpdateContainer,
        TContainer, TUpdateContainer> :
        NetworkSet<TId, TValue, TValueContainer, TContainer>, IUpdateSerializable<TContainer, TUpdateContainer>
        where TValue : IHasUniqueId<TId>, IUpdateSerializable<TValueContainer, TValueUpdateContainer>
        where TContainer : SetContainer<TId, TValueContainer>
        where TUpdateContainer : SetUpdateContainer<TId, TValueContainer, TValueUpdateContainer>
    {
        private class InternalDictionary
            : NetworkDictionary<TId, TValue, TValueContainer, TValueUpdateContainer, TContainer, TUpdateContainer>
        {
            protected override void OnEntryAdded(TId key, TValue value)
            {
                base.OnEntryAdded(key, value);
                value.Id = key;
            }

            protected override void OnEntryReset(TId key, TValue oldValue, TValue newValue)
            {
                base.OnEntryReset(key, oldValue, newValue);
                newValue.Id = key;
            }
        }

        protected override NetworkDictionary<TId, TValue> CreateInternalData()
        {
            return new InternalDictionary();
        }

        public TUpdateContainer SerializeUpdatesToContainer()
        {
            return ((IUpdateSerializable<TContainer, TUpdateContainer>)Data).SerializeUpdatesToContainer();
        }

        public void DeserializeUpdatesFromContainer(TUpdateContainer container)
        {
            ((IUpdateSerializable<TContainer, TUpdateContainer>)Data).DeserializeUpdatesFromContainer(container);
        }
    }
}
