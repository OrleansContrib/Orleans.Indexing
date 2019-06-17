namespace Orleans.Indexing.Facet
{
    public interface IIndexedStateFactory
    {
        INonFaultTolerantWorkflowIndexedState<TState> CreateNonFaultTolerantWorkflowIndexedState<TState>(IIndexedStateConfiguration config) where TState : class, new();
        IFaultTolerantWorkflowIndexedState<TState> CreateFaultTolerantWorkflowIndexedState<TState>(IIndexedStateConfiguration config) where TState : class, new();
        ITransactionalIndexedState<TState> CreateTransactionalIndexedState<TState>(IIndexedStateConfiguration config) where TState : class, new();
    }
}
