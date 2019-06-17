using System;

namespace Orleans.Indexing
{
    /// <summary>
    /// The generic attribute for declaring the property fields of an indexed grain interface to have an index.
    /// 
    /// This property should only be used for the index-types introduced by third-party libraries.
    /// Otherwise, we suggest to use one of the following descendants of the IndexAttribute based on your requirements:
    ///  - ActiveIndexAttribute
    ///  - TotalIndexAttribute
    ///  - StorageManagedIndexAttribute
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public class IndexAttribute : Attribute
    {
        public Type IndexType { get; set; }
        public bool IsUnique { get; set; }
        public bool IsEager { get; set; }
        public int MaxEntriesPerBucket { get; set; }

        // For non-nullable types this is the value to represent 'null' (unset), so new grains don't try to write
        // the default value (0); this causes a uniqueness violation for Unique indexes.
        public string NullValue { get; set; }

        /// <summary>
        /// The default constructor for Index.
        /// </summary>
        public IndexAttribute() : this(false)
        {
        }

        /// <summary>
        /// The constructor for Index.
        /// </summary>
        /// <param name="isEager">Determines whether the index should be updated eagerly upon any change in the indexed grains. Otherwise,
        ///     the update propagation happens lazily after applying the update to the grain itself.</param>
        public IndexAttribute(bool isEager) : this(typeof(IActiveHashIndexPartitionedPerSilo<,>), isEager, false)
        {
        }

        /// <summary>
        /// The full-option constructor for ActiveIndex.
        /// </summary>
        /// <param name="indexType">Type of the index implementation class.</param>
        /// <param name="isEager">Determines whether the index should be updated eagerly upon any change in the indexed grains. Otherwise,
        ///     the update propagation happens lazily after applying the update to the grain itself.</param>
        /// <param name="isUnique">Determines whether the index should maintain a uniqueness constraint.</param>
        /// <param name="maxEntriesPerBucket">The maximum number of entries that should be stored in each bucket of a distributed index. This
        ///     option is only considered if the index is a distributed index. Use -1 to declare no limit.</param>
        public IndexAttribute(Type indexType, bool isEager = false, bool isUnique = false, int maxEntriesPerBucket = -1)
        {
            this.IndexType = indexType;
            this.IsUnique = isUnique;
            this.IsEager = isEager;
            this.MaxEntriesPerBucket = maxEntriesPerBucket;
        }
    }
}
