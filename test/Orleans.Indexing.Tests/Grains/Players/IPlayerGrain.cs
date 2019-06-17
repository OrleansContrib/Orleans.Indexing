using System.Threading.Tasks;

namespace Orleans.Indexing.Tests
{
    public interface IPlayerGrain : IGrainWithIntegerKey
    {
        Task<string> GetEmail();
        Task<string> GetLocation();
        Task<int> GetScore();

        Task SetEmail(string email);
        Task SetLocation(string location);
        Task SetScore(int score);

        Task Deactivate();
    }

    public interface IPlayerGrainTransactional : IGrainWithIntegerKey
    {
        [Transaction(TransactionOption.CreateOrJoin, ReadOnly = true)]
        Task<string> GetEmail();
        [Transaction(TransactionOption.CreateOrJoin, ReadOnly = true)]
        Task<string> GetLocation();
        [Transaction(TransactionOption.CreateOrJoin, ReadOnly = true)]
        Task<int> GetScore();

        [Transaction(TransactionOption.CreateOrJoin, ReadOnly = false)]
        Task SetEmail(string email);
        [Transaction(TransactionOption.CreateOrJoin, ReadOnly = false)]
        Task SetLocation(string location);
        [Transaction(TransactionOption.CreateOrJoin, ReadOnly = false)]
        Task SetScore(int score);

        Task Deactivate();
    }
}
