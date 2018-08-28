using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StreamPack
{
    /// <summary>
    /// The event fired when an <see cref="INetworkObject{TKey}"/> is updated.
    /// </summary>
    /// <typeparam name="TId"></typeparam>
    /// <param name="networkObject"></param>
    public delegate void NetworkObjectUpdated<TDerived, TId>(TDerived networkObject) where TDerived : INetworkObject<TDerived, TId>;

    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="TId"></typeparam>
    public interface INetworkObject<TDerived, TId> : IUpdateSerializable where TDerived : INetworkObject<TDerived, TId>
    {
        /// <summary>
        /// The object id, which must be unique.
        /// </summary>
        TId Id { get; }

        /// <summary>
        /// Event fired when the object is updated.
        /// </summary>
        event NetworkObjectUpdated<TDerived, TId> Updated;
    }
}
