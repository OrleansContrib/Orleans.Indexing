using Orleans.Concurrency;

namespace Orleans.Indexing
{
    /// <summary>
    /// The interface for <see cref="HashIndexPartitionedPerKeyBucket{K, V}"/> grain,
    /// which is created in order to guide Orleans to find the grain instances more efficiently.
    /// </summary>
    /// <typeparam name="K">type of hash-index key</typeparam>
    /// <typeparam name="V">type of grain that is being indexed</typeparam>
    [Unordered]
    public interface ITotalHashIndexPartitionedPerKeyBucket<K, V> : IGrainWithStringKey,
                                                                    IHashIndexPartitionedPerKeyBucketInterface<K, V>,
                                                                    ITotalIndex where V : IIndexableGrain
    {
    }

    [Unordered]
    public interface ITotalHashIndexPartitionedPerKeyBucketTransactional<K, V> :
                                                                    IGrainWithStringKey,
                                                                    IHashIndexPartitionedPerKeyBucketInterface<K, V>,
                                                                    ITransactionalLookupIndex<K, V>,
                                                                    ITotalIndex where V : IIndexableGrain
    {
    }
}
