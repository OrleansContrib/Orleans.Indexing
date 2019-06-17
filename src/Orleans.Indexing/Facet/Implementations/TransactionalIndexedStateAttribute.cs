using System;

namespace Orleans.Indexing.Facet
{
    /// <summary>
    /// Marker interface for transactional indexed state.
    /// </summary>
    public interface ITransactionalIndexedStateAttribute
    {
    }

    /// <summary>
    /// Base class for the IIndexWriter facet that is implemented by transaction-based indexing.
    /// </summary>
    [AttributeUsage(AttributeTargets.Parameter)]
    public class TransactionalIndexedStateAttribute : IndexedStateAttribute, IFacetMetadata, ITransactionalIndexedStateAttribute, IIndexedStateConfiguration
    {
        public TransactionalIndexedStateAttribute(string stateName, string storageName = null)
            : base(stateName, storageName) { }
    }
}
