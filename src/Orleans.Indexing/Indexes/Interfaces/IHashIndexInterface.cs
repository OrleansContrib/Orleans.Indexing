using Orleans.Concurrency;
using System.Threading.Tasks;

namespace Orleans.Indexing
{
    /// <summary>
    /// Defines the interface for hash-indexes
    /// </summary>
    /// <typeparam name="K">the type of key for the hash index</typeparam>
    /// <typeparam name="V">the type of grain interface that is being indexed</typeparam>
    [Unordered]
    public interface IHashIndexInterface<K, V> : IIndexInterface<K, V> where V : IIndexableGrain
    {
        /// <summary>
        /// This method retrieves the unique result of a lookup into the hash-index
        /// </summary>
        /// <param name="key">the lookup key</param>
        /// <returns>the result of lookup into the hash-index</returns>
        /// <remarks>TODO LookupUnique is currently unused. <see cref="IOrleansQueryable{TIGrain, TProperties}"/> could be extended with
        ///     a GetUniqueResult method, which would be implemented in <see cref="QueryIndexedGrainsNode{TIGrain, TProperties}"/>
        ///     to call LookupUnique(Transactional)Async.</remarks>
        [ReadOnly]
        [AlwaysInterleave]
        [Transaction(TransactionOption.Supported)]
        Task<V> LookupUniqueAsync(K key);
    }
}
