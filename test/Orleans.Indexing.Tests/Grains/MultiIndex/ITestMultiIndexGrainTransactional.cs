using System.Threading.Tasks;

namespace Orleans.Indexing.Tests
{
    public interface ITestMultiIndexGrainTransactional : IGrainWithIntegerKey
    {
        [Transaction(TransactionOption.CreateOrJoin, ReadOnly = true)]
        Task<string> GetUnIndexedString();
        [Transaction(TransactionOption.CreateOrJoin)]
        Task SetUnIndexedString(string value);

        [Transaction(TransactionOption.CreateOrJoin, ReadOnly = true)]
        Task<int> GetUniqueInt();
        [Transaction(TransactionOption.CreateOrJoin)]
        Task SetUniqueInt(int value);

        [Transaction(TransactionOption.CreateOrJoin, ReadOnly = true)]
        Task<string> GetUniqueString();
        [Transaction(TransactionOption.CreateOrJoin)]
        Task SetUniqueString(string value);

        [Transaction(TransactionOption.CreateOrJoin, ReadOnly = true)]
        Task<int> GetNonUniqueInt();
        [Transaction(TransactionOption.CreateOrJoin)]
        Task SetNonUniqueInt(int value);

        [Transaction(TransactionOption.CreateOrJoin, ReadOnly = true)]
        Task<string> GetNonUniqueString();
        [Transaction(TransactionOption.CreateOrJoin)]
        Task SetNonUniqueString(string value);

        Task Deactivate();
    }
}
