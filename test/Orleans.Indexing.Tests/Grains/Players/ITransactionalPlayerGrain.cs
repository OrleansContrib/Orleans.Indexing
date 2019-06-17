using System.Threading.Tasks;

namespace Orleans.Indexing.Tests
{
    public interface ITransactionalPlayerGrain : IPlayerGrainTransactional, IIndexableGrain<TransactionalPlayerProperties>
    {
    }
}
