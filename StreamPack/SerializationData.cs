using MessagePack;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StreamPack.Serialization
{
    [MessagePackObject]
    public class NetworkCollectionItemData
    {
        [Key(0)]
        public byte[] Id { get; set; }

        [Key(1)]
        public byte[] Value { get; set; }
    }

    [MessagePackObject]
    public class NetworkCollectionUpdateData
    {
        [Key(0)]
        public byte Operation { get; set; }

        [Key(1)]
        public byte[] Id { get; set; }

        [Key(2)]
        public byte[] Value { get; set; }
    }

    [MessagePackObject]
    public class NetworkCollectionData
    {
        [Key(0)]
        public NetworkCollectionItemData[] Items;

        [Key(1)]
        public NetworkCollectionUpdateData[] ItemUpdates;
    }

    [MessagePackObject]
    public class PropertyData
    {
        [Key(0)]
        public byte Key { get; set; }

        [Key(1)]
        public byte[] Data { get; set; }
    }

    [MessagePackObject]
    public class PropertyUpdateData
    {
        [Key(0)]
        public byte Key { get; set; }

        [Key(1)]
        public byte[] Data { get; set; }
    }

    /// <summary>
    /// Serialization data of an <see cref="ISerializable"/> object.
    /// </summary>
    [MessagePackObject]
    public class SerializableObjectData
    {
        //[Key(0)]
        //public bool IsNull { get; set; }

        [Key(0)]
        public byte[] Data { get; set; }
    }

    /// <summary>
    /// 
    /// </summary>
    [MessagePackObject]
    public class PropertyUpdateOperationData
    {
        [Key(0)]
        public byte Operation { get; set; }

        [Key(1)]
        public byte[] Data { get; set; }
    }

    [MessagePackObject]
    public class BaseNetworkObjectData
    {
        [Key(0)]
        public PropertyData[] PropertyDataItems { get; set; }

        [Key(1)]
        public PropertyUpdateData[] PropertyUpdateDataItems { get; set; }
    }

    #region [ NetworkDictionary ]

    [MessagePackObject]
    public class NetworkDictionaryEntryData
    {
        [Key(0)]
        public byte[] KeyData { get; set; }

        [Key(1)]
        public byte[] ValueData { get; set; }
    }

    [MessagePackObject]
    public class NetworkDictionaryData
    {
        [Key(0)]
        public NetworkDictionaryEntryData[] EntryDataItems { get; set; }
    }

    [MessagePackObject]
    public class NetworkDictionaryEntryUpdateData
    {
        [Key(0)]
        public byte[] KeyData { get; set; }

        [Key(1)]
        public byte Operation { get; set; }

        [Key(2)]
        public byte[] ValueData { get; set; }
    }

    [MessagePackObject]
    public class NetworkDictionaryEntryPartialUpdateData
    {
        [Key(0)]
        public byte[] KeyData { get; set; }

        [Key(1)]
        public byte[] Data { get; set; }
    }

    [MessagePackObject]
    public class NetworkDictionaryUpdateData
    {
        [Key(0)]
        public bool IsClearedFirst { get; set; }

        [Key(1)]
        public NetworkDictionaryEntryUpdateData[] EntryUpdateDataItems { get; set; }

        [Key(2)]
        public NetworkDictionaryEntryPartialUpdateData[] EntryPartialUpdateDataItems { get; set; }
    }

    /**
     * Custom container dictionary.
     * */
    public abstract class DictionaryContainer<TKey, TValueContainer>
    {
        [Key(0)]
        public TKey[] Keys { get; set; }

        [Key(1)]
        public TValueContainer[] Values { get; set; }
    }

    public abstract class DictionaryUpdateContainer<TKey, TValueContainer, TValueUpdateContainer>
    {
        [Key(0)]
        public byte IsClearedFirst { get; set; }

        [Key(1)]
        public TKey[] EntryUpdateKeys { get; set; }

        [Key(2)]
        public byte[] EntryUpdateOperations { get; set; }

        [Key(3)]
        public TValueContainer[] EntryUpdateValues { get; set; }

        [Key(4)]
        public TKey[] ModifiedEntryKeys { get; set; }

        [Key(5)]
        public TValueUpdateContainer[] ModifiedEntryData { get; set; }
    }

    /**
     * Custom container set.
     * */
    public abstract class SetContainer<TId, TValueContainer> : DictionaryContainer<TId, TValueContainer>
    {
    }

    public abstract class SetUpdateContainer<TId, TValueContainer, TValueUpdateContainer>
        : DictionaryUpdateContainer<TId, TValueContainer, TValueUpdateContainer>
    {

    }
    #endregion
}
