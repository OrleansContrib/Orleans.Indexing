namespace Orleans.Indexing.Facet
{
    /// <summary>
    /// Per-instance configuration information. The <see cref="FaultTolerantWorkflowIndexedStateAttribute"/>
    /// and <see cref="NonFaultTolerantWorkflowIndexedStateAttribute"/> classes implement this, which is how
    /// the attribute parameters are communicated to the <see cref="IIndexedState{TGrainState}"/> implementation.
    /// </summary>
    public interface IIndexedStateConfiguration
    {
        string StateName { get; }

        string StorageName { get; }
    }
}
