namespace Orleans.Indexing.Facet
{
    /// <summary>
    /// The interface definition for a class that implements the indexing facet of a grain using Transactions.
    /// </summary>
    /// <typeparam name="TGrainState">The state implementation class.</typeparam>
    public interface ITransactionalIndexedState<TGrainState> : IIndexedState<TGrainState> where TGrainState : new()
    {
    }
}
