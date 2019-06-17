using System.Linq;

namespace Orleans.Indexing
{
    /// <summary>
    /// Extension for the built-in <see cref="IQueryProvider"/> allowing for Orleans specific operations
    /// </summary>
    public interface IOrleansQueryProvider : IQueryProvider
    {
    }
}
