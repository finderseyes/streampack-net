using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StreamPack
{
    /// <summary>
    /// An object which can be serialized to binary format.
    /// </summary>
    public interface ISerializable
    {
        /// <summary>
        /// Serializes current object to a byte buffer.
        /// </summary>
        /// <returns>Byte buffer.</returns>
        byte[] Serialize();

        /// <summary>
        /// Deserializes current object from given byte buffer.
        /// </summary>
        /// <param name="data">Byte buffer.</param>
        void Deserialize(byte[] data);
    }

    public interface ISerializable<TContainer>
    {
        TContainer SerializeToContainer();
        void DeserializeFromContainer(TContainer container);
    }
}
