using Orleans.Concurrency;

namespace Orleans.Indexing
{
    /// <summary>
    /// The interface for <see cref="HashIndexSingleBucket{K, V}"/> grain,
    /// which is created in order to guide Orleans to find the grain instances more efficiently.
    /// </summary>
    /// <typeparam name="K">type of hash-index key</typeparam>
    /// <typeparam name="V">type of grain that is being indexed</typeparam>
    [Unordered]
    public interface IHashIndexSingleBucketInterface<K, V> : IHashIndexInterface<K, V> where V : IIndexableGrain
    {
    }
}
