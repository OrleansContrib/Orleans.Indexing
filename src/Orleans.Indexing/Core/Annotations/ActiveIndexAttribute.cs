using System;

namespace Orleans.Indexing
{
    /// <summary>
    /// The attribute for declaring the property fields of an indexed grain interface to have an "Active Index".
    /// 
    /// An "Active Index" indexes all the grains that are currently active in the silos.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public sealed class ActiveIndexAttribute : IndexAttribute
    {
        /// <summary>
        /// The default constructor for ActiveIndex.
        /// </summary>
        public ActiveIndexAttribute() : this(false)
        {
        }

        /// <summary>
        /// The constructor for ActiveIndex.
        /// </summary>
        /// <param name="isEager">Determines whether the index should be updated eagerly upon any change in the indexed grains.
        ///     Otherwise, the update propagation happens lazily after applying the update to the grain itself.</param>
        public ActiveIndexAttribute(bool isEager) : this(ActiveIndexType.HashIndexPartitionedPerSilo, isEager)
        {
        }

        /// <summary>
        /// The full-option constructor for ActiveIndex.
        /// </summary>
        /// <param name="type">The index type for the active index</param>
        /// <param name="isEager">Determines whether the index should be updated eagerly upon any change in the indexed grains.
        ///     Otherwise, the update propagation happens lazily after applying the update to the grain itself.</param>
        /// <param name="maxEntriesPerBucket">The maximum number of entries that should be stored in each bucket of a distributed
        ///     index. This option is only considered if the index is a distributed index. Use -1 to declare no limit.</param>
        public ActiveIndexAttribute(ActiveIndexType type, bool isEager = false, int maxEntriesPerBucket = -1)
        {
            switch (type)
            {
                // All other Active Index types were removed
                case ActiveIndexType.HashIndexPartitionedPerSilo:
                default:
                    this.IndexType = typeof(IActiveHashIndexPartitionedPerSilo<,>);
                    break;
            }
            this.IsEager = isEager;

            // An Active Index cannot be defined as unique for the following reason:
            //  1. Suppose there's a unique Active Index over persistent objects.
            //  2. The activation of an initialized object could create a conflict in the Active Index.
            //     E.g., there's an active player PA with email foo and a non-active persistent player PP with email foo.
            //  3. An attempt to activate PP will cause a violation of the Active Index on email.
            // In other words, having a Total unique index prevents the possibility of such a conflict; having an Active unique index does not,
            // because one could activate a Grain, set its email to something already there and persist it (and then deactivate it and activate
            // a new one, etc.). The only use case would be "only one such value can be active at a time", but this would lead to more issues
            // than gain. This implies we should disallow such indexes, which happens during assembly load in ValidateSingleIndex.
            if (this.IsUnique)
            {
                throw new InvalidOperationException("Active indexes cannot be defined as unique; this should have been caught in ValidateSingleIndex.");
            }
            this.MaxEntriesPerBucket = maxEntriesPerBucket;
        }
    }
}
