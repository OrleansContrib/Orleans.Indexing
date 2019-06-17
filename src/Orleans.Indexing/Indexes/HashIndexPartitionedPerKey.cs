using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Orleans.Concurrency;
using Orleans.Runtime;

namespace Orleans.Indexing
{
    /// <summary>
    /// A simple implementation of a partitioned in-memory hash-index
    /// </summary>
    /// <typeparam name="K">type of hash-index key</typeparam>
    /// <typeparam name="V">type of grain interface that is being indexed</typeparam>
    /// <typeparam name="BucketT">type of bucket for the index</typeparam>
    public abstract class HashIndexPartitionedPerKey<K, V, BucketT> : IHashIndexInterface<K, V> where V : class, IIndexableGrain
        where BucketT : IHashIndexPartitionedPerKeyBucketInterface<K, V>, IGrainWithStringKey
    {
        private string _indexName;
        private bool isTransactional = false;

        private readonly IndexManager indexManager;
        private readonly ILogger logger;
        private int maxHashBuckets;

        public HashIndexPartitionedPerKey(IServiceProvider serviceProvider, string indexName, bool isUniqueIndex, bool isTransactional = false)
        {
            this._indexName = indexName;
            this.isTransactional = isTransactional;
            this.indexManager = IndexManager.GetIndexManager(serviceProvider);
            this.logger = this.indexManager.LoggerFactory.CreateLoggerWithFullCategoryName<HashIndexPartitionedPerKey<K, V, BucketT>>();
            this.maxHashBuckets = this.indexManager.IndexingOptions.MaxHashBuckets;
        }

        public async Task<bool> DirectApplyIndexUpdateBatch(Immutable<IDictionary<IIndexableGrain, IList<IMemberUpdate>>> iUpdates, bool isUnique, IndexMetaData idxMetaData, SiloAddress siloAddress = null)
        {
            logger.Trace($"Started calling DirectApplyIndexUpdateBatch with the following parameters: isUnique = {isUnique}, siloAddress = {siloAddress}," +
                         $" iUpdates = {MemberUpdate.UpdatesToString(iUpdates.Value)}");

            IDictionary<IIndexableGrain, IList<IMemberUpdate>> updates = iUpdates.Value;
            IDictionary<int, IDictionary<IIndexableGrain, IList<IMemberUpdate>>> bucketUpdates = new Dictionary<int, IDictionary<IIndexableGrain, IList<IMemberUpdate>>>();

            void AddUpdateToBucket(IIndexableGrain g, int bucket, IMemberUpdate update)
            {
                var bucketUpdatesMap = bucketUpdates.GetOrAdd(bucket, () => new Dictionary<IIndexableGrain, IList<IMemberUpdate>>());
                var bucketUpdatesList = bucketUpdatesMap.GetOrAdd(g, () => new List<IMemberUpdate>());
                bucketUpdatesList.Add(update);
            }

            foreach (var kv in updates)
            {
                IIndexableGrain g = kv.Key;
                IList<IMemberUpdate> gUpdates = kv.Value;
                foreach (IMemberUpdate update in gUpdates)
                {
                    IndexOperationType opType = update.OperationType;
                    if (opType == IndexOperationType.Update)
                    {
                        int befImgHash = GetBucketIndexFromHashCode(update.GetBeforeImage());
                        int aftImgHash = GetBucketIndexFromHashCode(update.GetAfterImage());

                        if (befImgHash == aftImgHash)
                        {
                            AddUpdateToBucket(g, befImgHash, update);
                        }
                        else
                        {
                            AddUpdateToBucket(g, befImgHash, new MemberUpdateOverriddenOperation(update, IndexOperationType.Delete));
                            AddUpdateToBucket(g, aftImgHash, new MemberUpdateOverriddenOperation(update, IndexOperationType.Insert));
                        }
                    }
                    else if (opType == IndexOperationType.Insert)
                    {
                        int aftImgHash = GetBucketIndexFromHashCode(update.GetAfterImage());
                        AddUpdateToBucket(g, aftImgHash, update);
                    }
                    else if (opType == IndexOperationType.Delete)
                    {
                        int befImgHash = GetBucketIndexFromHashCode(update.GetBeforeImage());
                        AddUpdateToBucket(g, befImgHash, update);
                    }
                }
            }

            var results = await Task.WhenAll(bucketUpdates.Select(kv =>
            {
                BucketT bucket = this.GetBucketGrain(kv.Key);
                return bucket.DirectApplyIndexUpdateBatch(kv.Value.AsImmutable(), isUnique, idxMetaData, siloAddress);
            }));

            logger.Trace($"Finished calling DirectApplyIndexUpdateBatch with the following parameters: isUnique = {isUnique}, siloAddress = {siloAddress}," +
                         $" iUpdates = {MemberUpdate.UpdatesToString(iUpdates.Value)}, results = '{string.Join(", ", results)}'");
            return true;
        }

        private BucketT GetBucketGrain(int keyValueHash)
        {
            var bucketPrimaryKey = IndexUtils.GetIndexGrainPrimaryKey(typeof(V), this._indexName) + "_" + keyValueHash;
            return this.indexManager.GrainFactory.GetGrain<BucketT>(bucketPrimaryKey);
        }

        public async Task<bool> DirectApplyIndexUpdate(IIndexableGrain g, Immutable<IMemberUpdate> iUpdate, bool isUniqueIndex, IndexMetaData idxMetaData, SiloAddress siloAddress)
        {
            IMemberUpdate update = iUpdate.Value;
            IndexOperationType opType = update.OperationType;
            if (opType == IndexOperationType.Update)
            {
                int befImgHash = GetBucketIndexFromHashCode(update.GetBeforeImage());
                int aftImgHash = GetBucketIndexFromHashCode(update.GetAfterImage());
                BucketT befImgBucket = this.GetBucketGrain(befImgHash);
                if (befImgHash == aftImgHash)
                {
                    return await befImgBucket.DirectApplyIndexUpdate(g, iUpdate, isUniqueIndex, idxMetaData);
                }

                BucketT aftImgBucket = this.GetBucketGrain(aftImgHash);
                var befTask = befImgBucket.DirectApplyIndexUpdate(g, new MemberUpdateOverriddenOperation(iUpdate.Value, IndexOperationType.Delete).AsImmutable<IMemberUpdate>(), isUniqueIndex, idxMetaData);
                var aftTask = aftImgBucket.DirectApplyIndexUpdate(g, new MemberUpdateOverriddenOperation(iUpdate.Value, IndexOperationType.Insert).AsImmutable<IMemberUpdate>(), isUniqueIndex, idxMetaData);
                bool[] results = await Task.WhenAll(befTask, aftTask);
                return results[0] && results[1];
            }
            else if (opType == IndexOperationType.Insert)
            {
                int aftImgHash = GetBucketIndexFromHashCode(update.GetAfterImage());
                BucketT aftImgBucket = this.GetBucketGrain(aftImgHash);
                return await aftImgBucket.DirectApplyIndexUpdate(g, iUpdate, isUniqueIndex, idxMetaData);
            }
            else if (opType == IndexOperationType.Delete)
            {
                int befImgHash = GetBucketIndexFromHashCode(update.GetBeforeImage());
                BucketT befImgBucket = this.GetBucketGrain(befImgHash);
                return await befImgBucket.DirectApplyIndexUpdate(g, iUpdate, isUniqueIndex, idxMetaData);
            }
            return true;
        }

        public Task LookupAsync(IOrleansQueryResultStream<V> result, K key)
        {
            logger.Trace($"Streamed index lookup called for key = {key}");

            var keyHash = GetBucketIndexFromHashCode(key);
            BucketT targetBucket = this.GetBucketGrain(keyHash);
            return isTransactional ? ((ITransactionalLookupIndex<K, V>)targetBucket).LookupTransactionalAsync(result, key)
                                   : targetBucket.LookupAsync(result, key);
        }

        public async Task<V> LookupUniqueAsync(K key)
        {
            var result = new OrleansFirstQueryResultStream<V>();
            var taskCompletionSource = new TaskCompletionSource<V>();
            Task<V> tsk = taskCompletionSource.Task;
            Action<V> responseHandler = taskCompletionSource.SetResult;
            await result.SubscribeAsync(new QueryFirstResultStreamObserver<V>(responseHandler));
            await LookupAsync(result, key);
            return await tsk;
        }

        public Task Dispose()
        {
            // TODO Right now we cannot do anything; we need to know the list of buckets.
            Debug.Assert(!isTransactional, "Should not call Dispose on Transactional indexes");
            return Task.CompletedTask;
        }

        public Task<bool> IsAvailable() => Task.FromResult(true);

        private int GetBucketIndexFromHashCode<T>(T img)
        {
            // HashIndexSingleBucketEntry uses a HashSet of GrainReference, which has its own GetHashCode,
            // so hash flooding with string key values is not an issue and we can use a non-randomized hashcode.
            var hashCode = img.GetInvariantHashCode();
            return this.maxHashBuckets > 0 ? hashCode % this.maxHashBuckets : hashCode;
        }

        Task IIndexInterface.LookupAsync(IOrleansQueryResultStream<IIndexableGrain> result, object key) => this.LookupAsync(result.Cast<V>(), (K)key);

        public Task<IOrleansQueryResult<V>> LookupAsync(K key)
        {
            logger.Trace($"Eager index lookup called for key = {key}");

            var keyHash = GetBucketIndexFromHashCode(key);
            BucketT targetBucket = this.GetBucketGrain(keyHash);
            return this.isTransactional ? ((ITransactionalLookupIndex<K, V>)targetBucket).LookupTransactionalAsync(key)
                                        : targetBucket.LookupAsync(key);
        }

        async Task<IOrleansQueryResult<IIndexableGrain>> IIndexInterface.LookupAsync(object key)
            => await this.LookupAsync((K)key);
    }
}
