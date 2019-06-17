namespace Orleans.Indexing
{
    /// <summary>
    /// The enumeration of all possible Total Index types in the system
    /// </summary>
    public enum TotalIndexType
    {
        /// <summary>
        /// Represents a hash-index that comprises a single bucket.
        /// 
        /// This type of index is not distributed and should be used with caution.
        /// The whole index should not have many entries, because it should be maintainable in a single grain on a single silo.
        /// </summary>
        HashIndexSingleBucket,

        /// <summary>
        /// Represents a distributed hash-index, and each bucket maintains a single value for the hash of the key.
        /// </summary>
        HashIndexPartitionedPerKeyHash
    }
}
