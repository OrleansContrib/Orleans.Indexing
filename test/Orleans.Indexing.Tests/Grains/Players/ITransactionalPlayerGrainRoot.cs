using System.Threading.Tasks;

namespace Orleans.Indexing.Tests
{
    public interface ITransactionalPlayerGrainRoot : IGrainWithIntegerKey
    {
        [Transaction(TransactionOption.Create)]
        Task InsertAsync(int score, string location, int count, int abortAfter = -1);

        [Transaction(TransactionOption.Create)]
        Task UpdateAsync(int fromScore, int toScore, string fromLocation, string toLocation, int count, int abortAfter = -1);
    }
}
