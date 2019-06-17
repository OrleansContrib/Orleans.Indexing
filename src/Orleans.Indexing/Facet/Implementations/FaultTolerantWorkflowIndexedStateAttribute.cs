using System;

namespace Orleans.Indexing.Facet
{
    /// <summary>
    /// Marker interface for fault-tolerant indexed state management.
    /// </summary>
    public interface IFaultTolerantWorkflowIndexedStateAttribute
    {
    }

    /// <summary>
    /// Base class for the IIndexedState facet that is implemented by fault-tolerant workflow-based indexing.
    /// </summary>
    [AttributeUsage(AttributeTargets.Parameter)]
    public class FaultTolerantWorkflowIndexedStateAttribute : IndexedStateAttribute, IFacetMetadata, IFaultTolerantWorkflowIndexedStateAttribute, IIndexedStateConfiguration
    {
        public FaultTolerantWorkflowIndexedStateAttribute(string stateName, string storageName = null)
            : base(stateName, storageName) { }
    }
}
