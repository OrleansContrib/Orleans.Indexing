namespace Orleans.Indexing
{
    /// <summary>
    /// The enumeration of all possible Active Index types in the system
    /// </summary>
    public enum ActiveIndexType
    {
        /// <summary>
        /// Represents a distributed hash-index, and each bucket is maintained by a silo.
        /// 
        /// PerSilo indexes are not supported for Total Indexes, and is the only partition supported for Active Indexes.
        /// </summary>
        HashIndexPartitionedPerSilo
    }
}
