namespace Orleans.Indexing.Facet
{
    /// <summary>
    /// The interface definition for a class that implements the indexing facet of a grain using a workflow
    /// implementation that is fault-tolerant.
    /// </summary>
    /// <typeparam name="TGrainState">The state implementation class.</typeparam>
    public interface IFaultTolerantWorkflowIndexedState<TGrainState> : IIndexedState<TGrainState> where TGrainState : new()
    {
    }
}
