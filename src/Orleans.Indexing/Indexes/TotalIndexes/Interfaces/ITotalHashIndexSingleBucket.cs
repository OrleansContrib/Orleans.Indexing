using Orleans.Concurrency;

namespace Orleans.Indexing
{
    /// <summary>
    /// The interface for <see cref="HashIndexSingleBucket{K, V}"/> grain,
    /// which is created in order to guide Orleans to find the grain instances more efficiently.
    /// </summary>
    /// <typeparam name="K">type of hash-index key</typeparam>
    /// <typeparam name="V">type of grain interface that is being indexed</typeparam>
    [Unordered]
    [TransactionalIndexVariant(typeof(ITotalHashIndexSingleBucketTransactional<,>))]
    public interface ITotalHashIndexSingleBucket<K, V> : IGrainWithStringKey,
                                                         IHashIndexSingleBucketInterface<K, V>,
                                                         ITotalIndex where V : IIndexableGrain
    {
    }

    [Unordered]
    public interface ITotalHashIndexSingleBucketTransactional<K, V> : IGrainWithStringKey,
                                                         IHashIndexSingleBucketInterface<K, V>,
                                                         ITransactionalLookupIndex<K, V>,
                                                         ITotalIndex where V : IIndexableGrain
    {
    }
}
