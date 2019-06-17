using Orleans.Runtime;
using System;
using System.Collections.Generic;

namespace Orleans.Indexing
{
    /// <summary>
    /// Contains the state that should be stored for each HashIndexSingleBucket
    /// </summary>
    /// <typeparam name="K">type of hash-index key</typeparam>
    /// <typeparam name="V">type of grain that is being indexed</typeparam>
    [Serializable]
    public class HashIndexBucketState<K, V> where V : IIndexableGrain
    {
        /// <summary>
        /// The actual storage of the indexed values
        /// </summary>
        public Dictionary<K, HashIndexSingleBucketEntry<V>> IndexMap { get; set; }

        /// <summary>
        /// Contains the status of the index regarding its population process, which can be either
        /// UnderConstruction or Available. Available means that the index has already been populated.
        /// </summary>
        public IndexStatus IndexStatus { get; set; }

        /// <summary>
        /// Chains from one bucket to the next.
        /// </summary>
        public GrainReference NextBucket { get; set; }
    }

    /// <summary>
    /// Represents an index entry in the hash-index
    /// </summary>
    /// <typeparam name="T">the type of elements stored in the entry</typeparam>
    [Serializable]
    public sealed class HashIndexSingleBucketEntry<T>
    {
        /// <summary>
        /// The set of values associated with a single key of the hash-index. The hash-set can contain more
        /// than one value if there is no uniqueness constraint on the hash-index.
        /// </summary>
        public HashSet<T> Values = new HashSet<T>();

        public const byte TENTATIVE_TYPE_NONE = 0;
        public const byte TENTATIVE_TYPE_DELETE = 1;
        public const byte TENTATIVE_TYPE_INSERT = 2;
        public byte TentativeOperationType = TENTATIVE_TYPE_NONE;

        internal void Remove(T item, IndexUpdateMode indexUpdateMode, bool isUniqueIndex)
        {
            if (indexUpdateMode == IndexUpdateMode.Tentative)
            {
                this.SetTentativeDelete();
                return;
            }

            // In order to make the index update operations idempotent, non-transactional unique indexes must only do their action if the index entry
            // is still marked as tentative. Otherwise, it means that tentative flag was removed by an earlier attempt and should not be done again.
            // There is no concern about non-unique indexes, because they cannot affect the operations among different grains and therefore
            // cannot fail the operations on other grains.
            if (!isUniqueIndex || indexUpdateMode == IndexUpdateMode.Transactional || this.IsTentative)
            {
                this.ClearTentativeFlag();
                this.Values.Remove(item);
            }
        }

        internal void Add(T item, IndexUpdateMode indexUpdateMode, bool isUniqueIndex)
        {
            this.Values.Add(item);
            if (indexUpdateMode == IndexUpdateMode.Tentative)
            {
                this.SetTentativeInsert();
                return;
            }

            // No condition check is necessary: if the flag is set, we will unset it, and if it's unset, we will unset it again, which is a no-op.
            this.ClearTentativeFlag();
        }

        internal bool IsTentative => this.IsTentativeDelete || this.IsTentativeInsert;

        internal bool IsTentativeDelete => this.TentativeOperationType == TENTATIVE_TYPE_DELETE;

        internal bool IsTentativeInsert => this.TentativeOperationType == TENTATIVE_TYPE_INSERT;

        internal void SetTentativeDelete() => this.TentativeOperationType = TENTATIVE_TYPE_DELETE;

        internal void SetTentativeInsert() => this.TentativeOperationType = TENTATIVE_TYPE_INSERT;

        internal void ClearTentativeFlag() => this.TentativeOperationType = TENTATIVE_TYPE_NONE;
    }
}