using System;

namespace StreamPack
{
    /// <summary>
    /// Interface Has unique identifier.
    /// </summary>
    public interface IHasUniqueId<TId>
    {
        /// <summary>
        /// The object id, which must be unique.
        /// </summary>
        TId Id { get; set; }
    }
}
