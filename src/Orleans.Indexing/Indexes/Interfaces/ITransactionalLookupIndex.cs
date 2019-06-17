using System.Threading.Tasks;
using Orleans.Concurrency;

namespace Orleans.Indexing
{
    /// <summary>
    /// Provide Transaction-initializing wrappers for <see cref="IIndexInterface"/>. These are because
    /// lookups for Transactional Indexes must be within a transaction, and this removes the need for
    /// the caller to start a transaction for simple lookups (if a transaction already exists, it will
    /// be joined).
    /// </summary>
    public interface ITransactionalLookupIndex
    {
        /// <summary>
        /// This method retrieves the result of a lookup into the hash-index
        /// </summary>
        /// <param name="result">the stream to search</param>
        /// <param name="key">the lookup key</param>
        /// <returns>the result of lookup into the hash-index</returns>
        [ReadOnly]
        [AlwaysInterleave]
        [Transaction(TransactionOption.CreateOrJoin)]
        Task LookupTransactionalAsync(IOrleansQueryResultStream<IIndexableGrain> result, object key);

        /// <summary>
        /// This method is used for extracting the whole result of a lookup from an ActiveHashIndexPartitionedPerSiloBucket.
        /// 
        /// TODO: This should not be necessary if we could call streams from within a GrainService, and the stream were efficient enough
        /// </summary>
        /// <param name="key">the lookup key</param>
        /// <returns>the result of the lookup</returns>
        [ReadOnly]
        [AlwaysInterleave]
        [Transaction(TransactionOption.CreateOrJoin)]
        Task<IOrleansQueryResult<IIndexableGrain>> LookupTransactionalAsync(object key);
    }

    /// <summary>
    /// Provide Transaction-initializing wrappers for <see cref="IHashIndexInterface{K,V}"/>
    /// </summary>
    [Unordered]
    public interface ITransactionalLookupIndex<K, V> : ITransactionalLookupIndex where V : IIndexableGrain
    {
        /// <summary>
        /// This method retrieves the result of a lookup into the hash-index
        /// </summary>
        /// <param name="result">the stream to search</param>
        /// <param name="key">the lookup key</param>
        /// <returns>the result of lookup into the hash-index</returns>
        [ReadOnly]
        [AlwaysInterleave]
        [Transaction(TransactionOption.CreateOrJoin)]
        Task LookupTransactionalAsync(IOrleansQueryResultStream<V> result, K key);

        /// <summary>
        /// This method is used for extracting the whole result of a lookup from an ActiveHashIndexPartitionedPerSiloBucket.
        /// 
        /// TODO: This should not be necessary if we could call streams from within a GrainService, and the stream were efficient enough
        /// </summary>
        /// <param name="key">the lookup key</param>
        /// <returns>the result of the lookup</returns>
        [ReadOnly]
        [AlwaysInterleave]
        [Transaction(TransactionOption.CreateOrJoin)]
        Task<IOrleansQueryResult<V>> LookupTransactionalAsync(K key);

        /// <summary>
        /// This method retrieves the unique result of a lookup into the hash-index
        /// </summary>
        /// <param name="key">the lookup key</param>
        /// <returns>the result of lookup into the hash-index</returns>
        [ReadOnly]
        [AlwaysInterleave]
        [Transaction(TransactionOption.CreateOrJoin)]
        Task<V> LookupTransactionalUniqueAsync(K key);
    }
}
