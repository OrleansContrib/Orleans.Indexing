using System;

namespace Orleans.Indexing
{
    /// <summary>
    /// The attribute for declaring the property fields of an indexed grain interface to have a "Total Index",
    /// which is also known as "Initialized Index".
    /// 
    /// A "Total Index" indexes all the grains that have been created during the lifetime of the application.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public sealed class TotalIndexAttribute : IndexAttribute
    {
        /// <summary>
        /// The default constructor for TotalIndex.
        /// </summary>
        public TotalIndexAttribute() : this(false)
        {
        }

        /// <summary>
        /// The constructor for TotalIndex.
        /// </summary>
        /// <param name="isEager">Determines whether the index should be updated eagerly upon any change in the indexed grains.
        ///     Otherwise, the update propagation happens lazily after applying the update to the grain itself.</param>
        public TotalIndexAttribute(bool isEager) : this(TotalIndexType.HashIndexSingleBucket, isEager, false)
        {
        }

        /// <summary>
        /// The full-option constructor for TotalIndex.
        /// </summary>
        /// <param name="type">The index type for the Total index</param>
        /// <param name="isEager">Determines whether the index should be updated eagerly upon any change in the indexed grains.
        ///     Otherwise, the update propagation happens lazily after applying the update to the grain itself.</param>
        /// <param name="isUnique">Determines whether the index should maintain a uniqueness constraint.</param>
        /// <param name="maxEntriesPerBucket">The maximum number of entries that should be stored in each bucket of a distributed index.
        ///     This option is only considered if the index is a distributed index. Use -1 to declare no limit.</param>
        public TotalIndexAttribute(TotalIndexType type, bool isEager = false, bool isUnique = false, int maxEntriesPerBucket = -1)
        {
            switch (type)
            {
                case TotalIndexType.HashIndexSingleBucket:
                    this.IndexType = typeof(ITotalHashIndexSingleBucket<,>);
                    break;
                case TotalIndexType.HashIndexPartitionedPerKeyHash:
                    // This uses the class, not an interface, because there is no underlying grain implementation for per-key indexes
                    // themselves (there is, of course, for their buckets).
                    this.IndexType = typeof(TotalHashIndexPartitionedPerKey<,>);
                    break;
                default:
                    this.IndexType = typeof(ITotalHashIndexSingleBucket<,>);
                    break;
            }
            this.IsEager = isEager;
            this.IsUnique = isUnique;
            this.MaxEntriesPerBucket = maxEntriesPerBucket;
        }
    }
}
