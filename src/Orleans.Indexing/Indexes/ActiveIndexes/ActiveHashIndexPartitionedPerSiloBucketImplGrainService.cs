using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Orleans.Concurrency;
using Orleans.Runtime;
using K = System.Object;
using V = Orleans.Indexing.IIndexableGrain;
using Orleans.Providers;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging;
using Orleans.Core;
using Microsoft.Extensions.DependencyInjection;

namespace Orleans.Indexing
{
    /// <summary>
    /// A simple implementation of a single-grain in-memory hash-index.
    /// 
    /// TODO: Generic GrainServices are not supported yet, and that's why the implementation is non-generic.
    /// </summary>
    //Per comments for IActiveHashIndexPartitionedPerSiloBucket, we cannot use generics here.
    //<typeparam name="K">type of hash-index key</typeparam>
    //<typeparam name="V">type of grain that is being indexed</typeparam>
    [StorageProvider(ProviderName = IndexingConstants.MEMORY_STORAGE_PROVIDER_NAME)]
    [Reentrant]
    internal class ActiveHashIndexPartitionedPerSiloBucketImplGrainService/*<K, V>*/ : GrainService, IActiveHashIndexPartitionedPerSiloBucket/*<K, V> where V : IIndexableGrain*/
    {
        private HashIndexBucketState<K, V> state;
        private readonly SiloIndexManager siloIndexManager;
        private readonly ILogger logger;
        private readonly string _parentIndexName;

        public ActiveHashIndexPartitionedPerSiloBucketImplGrainService(SiloIndexManager siloIndexManager, Type grainInterfaceType, string parentIndexName)
            : base(GetGrainIdentity(siloIndexManager, grainInterfaceType, parentIndexName), siloIndexManager.Silo, siloIndexManager.LoggerFactory)
        {
            this.state = new HashIndexBucketState<K, V>
            {
                IndexMap = new Dictionary<K, HashIndexSingleBucketEntry<V>>(),
                IndexStatus = IndexStatus.Available
            };

            _parentIndexName = parentIndexName;
            this.siloIndexManager = siloIndexManager;
            this.logger = siloIndexManager.LoggerFactory.CreateLoggerWithFullCategoryName<ActiveHashIndexPartitionedPerSiloBucketImplGrainService>();
        }

        private static IGrainIdentity GetGrainIdentity(SiloIndexManager siloIndexManager, Type grainInterfaceType, string indexName)
            => GetGrainReference(siloIndexManager, grainInterfaceType, indexName).GrainIdentity;

        internal static GrainReference GetGrainReference(SiloIndexManager siloIndexManager, Type grainInterfaceType, string indexName, SiloAddress siloAddress = null)
            => siloIndexManager.MakeGrainServiceGrainReference(IndexingConstants.HASH_INDEX_PARTITIONED_PER_SILO_BUCKET_GRAIN_SERVICE_TYPE_CODE,
                                                               IndexUtils.GetIndexGrainPrimaryKey(grainInterfaceType, indexName),
                                                               siloAddress ?? siloIndexManager.SiloAddress);

        // Called by ApplicationPartsIndexableGrainLoader.RegisterGrainServices
        internal static void RegisterGrainService(IServiceCollection services, Type grainInterfaceType, string indexName)
            => services.AddGrainService(sp => new ActiveHashIndexPartitionedPerSiloBucketImplGrainService(sp.GetRequiredService<SiloIndexManager>(), grainInterfaceType, indexName));

        public async Task<bool> DirectApplyIndexUpdateBatch(Immutable<IDictionary<IIndexableGrain, IList<IMemberUpdate>>> iUpdates, bool isUnique, IndexMetaData idxMetaData, SiloAddress siloAddress = null)
        {
            logger.Trace($"ParentIndex {_parentIndexName}: Started calling DirectApplyIndexUpdateBatch with the following parameters: isUnique = {isUnique}," +
                         $" siloAddress = {siloAddress}, iUpdates = {MemberUpdate.UpdatesToString(iUpdates.Value)}", isUnique, siloAddress);

            await Task.WhenAll(iUpdates.Value.Select(kvp => DirectApplyIndexUpdates(kvp.Key, kvp.Value, isUnique, idxMetaData, siloAddress)));

            logger.Trace($"Finished calling DirectApplyIndexUpdateBatch with the following parameters: isUnique = {isUnique}, siloAddress = {siloAddress}," +
                         $" iUpdates = {MemberUpdate.UpdatesToString(iUpdates.Value)}");
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private async Task DirectApplyIndexUpdates(IIndexableGrain g, IList<IMemberUpdate> updates, bool isUniqueIndex, IndexMetaData idxMetaData, SiloAddress siloAddress)
        {
            foreach (IMemberUpdate updt in updates)
            {
                await DirectApplyIndexUpdate(g, updt, isUniqueIndex, idxMetaData, siloAddress);
            }
        }

        public Task<bool> DirectApplyIndexUpdate(V updatedGrain, Immutable<IMemberUpdate> iUpdate, bool isUniqueIndex, IndexMetaData idxMetaData, SiloAddress siloAddress)
            => this.DirectApplyIndexUpdate(updatedGrain, iUpdate.Value, isUniqueIndex, idxMetaData, siloAddress);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private Task<bool> DirectApplyIndexUpdate(V updatedGrain, IMemberUpdate updt, bool isUniqueIndex, IndexMetaData idxMetaData, SiloAddress siloAddress)
            // Updates the index bucket synchronously (note that no other thread can run concurrently before we reach an await operation,
            // when execution is yielded back to the Orleans scheduler, so no concurrency control mechanism (e.g., locking) is required).
            => Task.FromResult(HashIndexBucketUtils.UpdateBucketState(updatedGrain, updt, state, isUniqueIndex, idxMetaData));

        private Exception LogException(string message, IndexingErrorCode errorCode)
        {
            var e = new Exception(message);
            this.logger.Error(errorCode, message, e);
            return e;
        }

        public async Task LookupAsync(IOrleansQueryResultStream<V> result, K key)
        {
            logger.Trace($"Streamed index lookup called for key = {key}");

            if (!(state.IndexStatus == IndexStatus.Available))
            {
                throw LogException($"ParentIndex {_parentIndexName}: Index is not still available.", IndexingErrorCode.IndexingIndexIsNotReadyYet_GrainServiceBucket1);
            }
            if (state.IndexMap.TryGetValue(key, out HashIndexSingleBucketEntry<V> entry) && !entry.IsTentative)
            {
                await result.OnNextAsync(entry.Values.ToBatch());
                await result.OnCompletedAsync();
            }
            else
            {
                await result.OnCompletedAsync();
            }
        }

        public Task<IOrleansQueryResult<V>> LookupAsync(K key)
        {
            logger.Trace($"ParentIndex {_parentIndexName}: Eager index lookup called for key = {key}");

            if (!(state.IndexStatus == IndexStatus.Available))
            {
                throw LogException($"ParentIndex {_parentIndexName}: Index is not still available.", IndexingErrorCode.IndexingIndexIsNotReadyYet_GrainServiceBucket2);
            }
            var entryValues = (state.IndexMap.TryGetValue(key, out HashIndexSingleBucketEntry<V> entry) && !entry.IsTentative) ? entry.Values : Enumerable.Empty<V>();
            return Task.FromResult((IOrleansQueryResult<V>)new OrleansQueryResult<V>(entryValues));
        }

        public Task<V> LookupUniqueAsync(K key)
        {
            if (!(state.IndexStatus == IndexStatus.Available))
            {
                throw LogException($"ParentIndex {_parentIndexName}: Index is not still available.", IndexingErrorCode.IndexingIndexIsNotReadyYet_GrainServiceBucket3);
            }
            if (state.IndexMap.TryGetValue(key, out HashIndexSingleBucketEntry<V> entry) && !entry.IsTentative)
            {
                return (entry.Values.Count == 1)
                    ? Task.FromResult(entry.Values.GetEnumerator().Current)
                    : throw LogException($"ParentIndex {_parentIndexName}: There are {entry.Values.Count} values for the unique lookup key \"{key}\" on index \"{IndexUtils.GetIndexNameFromIndexGrain(this)}\".",
                                      IndexingErrorCode.IndexingIndexIsNotReadyYet_GrainServiceBucket4);
            }
            throw LogException($"ParentIndex {_parentIndexName}The lookup key \"{key}\" does not exist on index \"{IndexUtils.GetIndexNameFromIndexGrain(this)}\".",
                                   IndexingErrorCode.IndexingIndexIsNotReadyYet_GrainServiceBucket5);
        }

        public Task Dispose()
        {
            state.IndexStatus = IndexStatus.Disposed;
            state.IndexMap.Clear();

            // For now we do not call Dispose(); this will be needed for dynamic addition/removal of indexes.
            //return this.siloIndexManager.Silo.RemoveGrainService(this);
            throw new NotImplementedException("GrainService removal is not yet implemented");
        }

        public Task<bool> IsAvailable()
            => Task.FromResult(state.IndexStatus == IndexStatus.Available);
    }
}
