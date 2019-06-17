using System;

namespace Orleans.Indexing.Facet
{
    /// <summary>
    /// Marker interface for non-fault-tolerant indexed state management.
    /// </summary>
    public interface INonFaultTolerantWorkflowIndexedStateAttribute
    {
    }

    /// <summary>
    /// Base class for the IIndexedState facet that is implemented by non-fault-tolerant workflow-based indexing.
    /// </summary>
    [AttributeUsage(AttributeTargets.Parameter)]
    public class NonFaultTolerantWorkflowIndexedStateAttribute : IndexedStateAttribute, IFacetMetadata, INonFaultTolerantWorkflowIndexedStateAttribute, IIndexedStateConfiguration
    {
        public NonFaultTolerantWorkflowIndexedStateAttribute(string stateName, string storageName = null)
            : base(stateName, storageName) { }
    }
}
