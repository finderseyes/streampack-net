using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StreamPack
{
    /// <summary>
    /// An object whose updates can seialized to binary format.
    /// </summary>
    public interface IUpdateSerializable : ISerializable, INotifyModified
    {
        /// <summary>
        /// Determines if the object has updates.
        /// </summary>
        bool HasUpdates { get; }

        /// <summary>
        /// Clears the object's updates.
        /// </summary>
        void ClearUpdates();

        /// <summary>
        /// Serializes the object's updates to a byte buffer.
        /// </summary>
        /// <returns>Byte buffer.</returns>
        byte[] SerializeUpdates();

        /// <summary>
        /// Deserializes the object's updates from a byte buffer.
        /// </summary>
        /// <param name="data">Byte buffer.</param>
        void DeserializeUpdates(byte[] data);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="TContainer"></typeparam>
    /// <typeparam name="TUpdateContainer"></typeparam>
    public interface IUpdateSerializable<TContainer, TUpdateContainer> : ISerializable<TContainer>
    {
        TUpdateContainer SerializeUpdatesToContainer();
        void DeserializeUpdatesFromContainer(TUpdateContainer container);
    }
}
