using System;

namespace Orleans.Indexing
{
    /// <summary>
    /// The meta data that is stored beside the index
    /// </summary>
    [Serializable]
    public class IndexMetaData
    {
        private Type _indexType;
        private int _maxEntriesPerBucket;

        /// <summary>
        /// Constructs an IndexMetaData, which currently only consists of the type of the index
        /// </summary>
        /// <param name="indexType">Type of the index implementation class.</param>
        /// <param name="indexName">Name of the index (taken from the indexed property).</param>
        /// <param name="isEager">Determines whether the index should be updated eagerly upon any change in the indexed grains. Otherwise,
        /// the update propagation happens lazily after applying the update to the grain itself.</param>
        /// <param name="isUniqueIndex">Determines whether the index should maintain a uniqueness constraint.</param>
        /// <param name="maxEntriesPerBucket">The maximum number of entries that should be stored in each bucket of a distributed index. This
        /// option is only considered if the index is a distributed index. Use -1 to declare no limit.</param>
        public IndexMetaData(Type indexType, string indexName, bool isUniqueIndex, bool isEager, int maxEntriesPerBucket)
        {
            this._indexType = indexType;
            this.IndexName = indexName;
            this.IsUniqueIndex = isUniqueIndex;
            this.IsEager = isEager;
            this._maxEntriesPerBucket = maxEntriesPerBucket;
        }

        internal string IndexName { get; }

        public bool IsUniqueIndex { get; }

        public bool IsEager { get; }

        public bool IsChainedBuckets => this._maxEntriesPerBucket > 0;

        internal bool IsCreatingANewBucketNecessary(int currentSize) => this.IsChainedBuckets && currentSize >= this._maxEntriesPerBucket;
    }
}
