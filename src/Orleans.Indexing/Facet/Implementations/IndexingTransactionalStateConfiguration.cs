using Orleans.Transactions.Abstractions;

namespace Orleans.Indexing.Facet
{
    internal class IndexingTransactionalStateConfiguration : ITransactionalStateConfiguration
    {
        public string StateName { get; set; }
        public string StorageName { get; set; }

        internal IndexingTransactionalStateConfiguration(string stateName, string storageName)
        {
            this.StateName = stateName;
            this.StorageName = storageName;
        }
    }
}
