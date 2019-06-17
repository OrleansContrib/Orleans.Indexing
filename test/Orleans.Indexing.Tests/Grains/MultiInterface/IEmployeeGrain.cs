using System.Threading.Tasks;

namespace Orleans.Indexing.Tests.MultiInterface
{
    public interface IEmployeeGrain
    {
        Task<int> GetEmployeeId();
        Task SetEmployeeId(int value);

        Task<int> GetSalary();
        Task SetSalary(int value);

        // For testing
        Task WriteState();
        Task Deactivate();
    }
}
