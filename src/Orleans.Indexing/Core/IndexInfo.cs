namespace Orleans.Indexing
{
    internal class IndexInfo
    {
        /// <summary>
        /// The index object (that implements IndexInterface)
        /// </summary>
        public IIndexInterface IndexInterface { get; private set; }

        /// <summary>
        /// The IndexMetaData object for this index
        /// </summary>
        public IndexMetaData MetaData { get; private set; }

        /// <summary>
        /// The IndexUpdateGenerator instance for this index
        /// </summary>
        public IIndexUpdateGenerator UpdateGenerator { get; private set; }

        internal IndexInfo(IIndexInterface indexInterface, IndexMetaData metaData, IIndexUpdateGenerator updateGenerator)
        {
            this.IndexInterface = indexInterface;
            this.MetaData = metaData;
            this.UpdateGenerator = updateGenerator;
        }
    }
}
