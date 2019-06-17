using System.Threading.Tasks;

namespace Orleans.Indexing.Tests
{
    public interface ITestTransactionalPersistence
    {
        [Transaction(TransactionOption.CreateOrJoin, ReadOnly = true)]
        Task InitializeStateTxn();

        [Transaction(TransactionOption.CreateOrJoin)]
        Task WriteStateTxn();
    }
}
