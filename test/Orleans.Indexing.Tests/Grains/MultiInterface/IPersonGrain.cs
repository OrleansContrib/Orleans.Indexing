using System.Threading.Tasks;

namespace Orleans.Indexing.Tests.MultiInterface
{
    public interface IPersonGrain
    {
        Task<string> GetName();
        Task SetName(string value);

        Task<int> GetAge();
        Task SetAge(int value);

        // For testing
        Task InitializeState();
        Task WriteState();
        Task Deactivate();
    }
}
