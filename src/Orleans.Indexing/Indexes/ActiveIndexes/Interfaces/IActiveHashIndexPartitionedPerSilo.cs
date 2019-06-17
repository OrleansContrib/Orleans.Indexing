using Orleans.Concurrency;

namespace Orleans.Indexing
{
    /// <summary>
    /// This is a marker interface for <see cref="IActiveHashIndexPartitionedPerSilo{K, V}"/> generic interface
    /// </summary>
    public interface IActiveHashIndexPartitionedPerSilo : IGrain
    {
    }

    /// <summary>
    /// The interface for the <see cref="ActiveHashIndexPartitionedPerSiloBucketImplGrainService"/> GrainService,
    /// which is created in order to guide Orleans to find the grain instances more efficiently.
    /// </summary>
    /// <typeparam name="K">type of hash-index key</typeparam>
    /// <typeparam name="V">type of grain that is being indexed</typeparam>
    [Unordered]
    [PerSiloIndexGrainServiceClass(typeof(ActiveHashIndexPartitionedPerSiloBucketImplGrainService))]
    public interface IActiveHashIndexPartitionedPerSilo<K, V> : IActiveHashIndexPartitionedPerSilo, IHashIndexInterface<K, V> where V : IIndexableGrain
    {
    }
}
