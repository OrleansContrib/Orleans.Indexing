using Orleans.Concurrency;
using Orleans.Services;

namespace Orleans.Indexing
{
    /// <summary>
    /// The interface for <see cref="ActiveHashIndexPartitionedPerSiloBucketImplGrainService"/> GrainService,
    /// which is created in order to guide Orleans to find the grain instances more efficiently.
    /// 
    /// TODO Generic GrainServices are not supported yet, and that's why the interface is non-generic.
    /// </summary>
    //<typeparam name="K">type of hash-index key</typeparam>
    //<typeparam name="V">type of grain that is being indexed</typeparam>
    //internal interface ActiveHashIndexPartitionedPerSiloBucket<K, V> : IGrainService, HashIndexInterface<K, V> where V : IIndexableGrain
    [Unordered]
    internal interface IActiveHashIndexPartitionedPerSiloBucket : IGrainService, IHashIndexInterface<object, IIndexableGrain>
    {
    }
}
