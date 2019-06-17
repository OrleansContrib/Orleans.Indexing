using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Orleans.Concurrency;
using Orleans.Runtime;

namespace Orleans.Indexing
{
    /// <summary>
    /// A simple implementation of a single-grain in-memory hash-index
    /// </summary>
    /// <typeparam name="K">type of hash-index key</typeparam>
    /// <typeparam name="V">type of grain that is being indexed</typeparam>
    [Reentrant]
    //[StatelessWorker]
    //TODO: because of a bug in OrleansStreams, this grain cannot be StatelessWorker. It should be fixed later. TODO which bug?
    //TODO: basically, this class does not even need to be a grain, but it's not possible to call a GrainService from a non-grain
    public class ActiveHashIndexPartitionedPerSiloImpl<K, V> : Grain, IActiveHashIndexPartitionedPerSilo<K, V> where V : class, IIndexableGrain
    {
        private IndexStatus _status;

        // IndexManager (and therefore logger) cannot be set in ctor because Grain activation has not yet set base.Runtime.
        internal SiloIndexManager SiloIndexManager => IndexManager.GetSiloIndexManager(ref __siloIndexManager, base.ServiceProvider);
        private SiloIndexManager __siloIndexManager;

        private ILogger Logger => __logger ?? (__logger = this.SiloIndexManager.LoggerFactory.CreateLoggerWithFullCategoryName<ActiveHashIndexPartitionedPerSiloImpl<K, V>>());
        private ILogger __logger;

        public override Task OnActivateAsync()
        {
            _status = IndexStatus.Available;
            return base.OnActivateAsync();
        }

        /// <summary>
        /// DirectApplyIndexUpdateBatch is not supported on ActiveHashIndexPartitionedPerSiloImpl, because it will be skipped
        /// via IndexExtensions.ApplyIndexUpdateBatch which goes directly to the IActiveHashIndexPartitionedPerSiloBucket grain service
        /// </summary>
        public Task<bool> DirectApplyIndexUpdateBatch(Immutable<IDictionary<IIndexableGrain, IList<IMemberUpdate>>> iUpdates, bool isUnique, IndexMetaData idxMetaData, SiloAddress siloAddress = null)
            => throw new NotSupportedException();

        /// <summary>
        /// DirectApplyIndexUpdate is not supported on ActiveHashIndexPartitionedPerSiloImpl, because it will be skipped
        /// via IndexExtensions.ApplyIndexUpdate which goes directly to the IActiveHashIndexPartitionedPerSiloBucket grain service
        /// </summary>
        public Task<bool> DirectApplyIndexUpdate(IIndexableGrain g, Immutable<IMemberUpdate> iUpdate, bool isUniqueIndex, IndexMetaData idxMetaData, SiloAddress siloAddress)
            => throw new NotSupportedException();

        private static GrainReference GetGrainReference(SiloIndexManager siloIndexManager, string indexName, SiloAddress siloAddress = null)
            => ActiveHashIndexPartitionedPerSiloBucketImplGrainService.GetGrainReference(siloIndexManager, typeof(V), indexName, siloAddress);

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

        public async Task Dispose()
        {
            _status = IndexStatus.Disposed;
            var indexName = IndexUtils.GetIndexNameFromIndexGrain(this);
            GrainReference makeGrainReference(SiloAddress siloAddress) => GetGrainReference(this.SiloIndexManager, indexName, siloAddress);

            // Get and Dispose() all buckets in silos
            Dictionary<SiloAddress, SiloStatus> hosts = await this.SiloIndexManager.GetSiloHosts(true);
            await Task.WhenAll(hosts.Keys.Select(siloAddress => this.SiloIndexManager
                                                                    .GetGrainService<IActiveHashIndexPartitionedPerSiloBucket>(makeGrainReference(siloAddress))
                                                                    .Dispose()));
        }

        public Task<bool> IsAvailable() => Task.FromResult(_status == IndexStatus.Available);

        async Task<IOrleansQueryResult<IIndexableGrain>> IIndexInterface.LookupAsync(object key)
        {
            Logger.Trace($"Eager index lookup called for key = {key}");

            //get all silos
            Dictionary<SiloAddress, SiloStatus> hosts = await this.SiloIndexManager.GetSiloHosts(true);
            IEnumerable<IIndexableGrain>[] queriesToSilos = await Task.WhenAll(GetResultQueries(hosts, key));
            return new OrleansQueryResult<V>(queriesToSilos.SelectMany(res => res.Select(e => e.AsReference<V>())).ToList());
        }

        public async Task<IOrleansQueryResult<V>> LookupAsync(K key) => (IOrleansQueryResult<V>)await ((IIndexInterface)this).LookupAsync(key);

        private ISet<Task<IOrleansQueryResult<IIndexableGrain>>> GetResultQueries(Dictionary<SiloAddress, SiloStatus> hosts, object key)
        {
            ISet<Task<IOrleansQueryResult<IIndexableGrain>>> queriesToSilos = new HashSet<Task<IOrleansQueryResult<IIndexableGrain>>>();

            int i = 0;
            var indexName = IndexUtils.GetIndexNameFromIndexGrain(this);
            foreach (SiloAddress siloAddress in hosts.Keys)
            {
                //query each silo
                queriesToSilos.Add(this.SiloIndexManager.GetGrainService<IActiveHashIndexPartitionedPerSiloBucket>(
                    GetGrainReference(this.SiloIndexManager, indexName, siloAddress
                )).LookupAsync(/*result, */key)); //TODO: a bug in OrleansStream currently prevents a GrainService from working with streams.
                ++i;
            }

            return queriesToSilos;
        }

        public Task LookupAsync(IOrleansQueryResultStream<V> result, K key)
            => ((IIndexInterface)this).LookupAsync(result.Cast<IIndexableGrain>(), key);

        async Task IIndexInterface.LookupAsync(IOrleansQueryResultStream<IIndexableGrain> result, object key)
        {
            Logger.Trace($"Streamed index lookup called for key = {key}");

            // Get all silos
            Dictionary<SiloAddress, SiloStatus> hosts = await this.SiloIndexManager.GetSiloHosts(true);
            ISet<Task<IOrleansQueryResult<IIndexableGrain>>> queriesToSilos = GetResultQueries(hosts, key);

            //TODO: After fixing the problem with OrleansStream, this part is not needed anymore.
            while (queriesToSilos.Count > 0)
            {
                // Identify the first task that completes.
                Task<IOrleansQueryResult<IIndexableGrain>> firstFinishedTask = await Task.WhenAny(queriesToSilos);

                // ***Remove the selected task from the list so that you don't process it more than once.
                queriesToSilos.Remove(firstFinishedTask);

                // Await the completed task.
                IOrleansQueryResult<IIndexableGrain> partialResult = await firstFinishedTask;
                await result.OnNextAsync(partialResult.ToBatch());
            }
            await result.OnCompletedAsync();
        }
    }
}
