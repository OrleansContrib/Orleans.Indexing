using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Orleans.Concurrency;
using Orleans.Runtime;
using Orleans.Storage;
using Orleans.Transactions;
using Orleans.Utilities;

namespace Orleans.Indexing
{
    /// <summary>
    /// A simple implementation of a direct storage managed index (i.e., without caching)
    /// </summary>
    /// <typeparam name="K">type of hash-index key</typeparam>
    /// <typeparam name="V">type of grain that is being indexed</typeparam>
    [Reentrant]
    //[StatelessWorker]
    //TODO: a bug in OrleansStreams currently prevents streams from working with stateless grains, so this grain cannot be StatelessWorker.
    //TODO: basically, this class does not even need to be a grain, but it's not possible to call a GrainService from a non-grain
    public class DirectStorageManagedIndexImpl<K, V> : DMSIGrain<K, V>, IDirectStorageManagedIndex<K, V> where V : class, IIndexableGrain
    {
        public DirectStorageManagedIndexImpl() : base(isTransactional: false) { }
    }

    public class DirectStorageManagedIndexImplTransactional<K, V> : DMSIGrain<K, V>, IDirectStorageManagedIndexTransactional<K, V>
                                                                    where V : class, IIndexableGrain
    {
        public DirectStorageManagedIndexImplTransactional() : base(isTransactional: true) { }

        #region ITransactionalLookupIndex<K,V>
        public Task LookupTransactionalAsync(IOrleansQueryResultStream<V> result, K key) => this.LookupAsync(result, key);
        public Task<IOrleansQueryResult<V>> LookupTransactionalAsync(K key) => this.LookupAsync(key);
        public Task LookupTransactionalAsync(IOrleansQueryResultStream<IIndexableGrain> result, object key) => ((IIndexInterface<K, V>)this).LookupAsync(result, key);
        public Task<IOrleansQueryResult<IIndexableGrain>> LookupTransactionalAsync(object key) => ((IIndexInterface<K, V>)this).LookupAsync(key);
        public Task<V> LookupTransactionalUniqueAsync(K key) => this.LookupUniqueAsync(key);
        #endregion ITransactionalLookupIndex<K,V>
    }

    public abstract class DMSIGrain<K, V> : Grain, IIndexInterface where V : class, IIndexableGrain
    {
        private IGrainStorage _grainStorage;
        private string _grainClassName;

        private string _indexedField;

        // IndexManager (and therefore logger) cannot be set in ctor because Grain activation has not yet set base.Runtime.
        internal SiloIndexManager SiloIndexManager => IndexManager.GetSiloIndexManager(ref __indexManager, base.ServiceProvider);
        private SiloIndexManager __indexManager;

        private ILogger Logger => __logger ?? (__logger = this.SiloIndexManager.LoggerFactory.CreateLoggerWithFullCategoryName<DirectStorageManagedIndexImpl<K, V>>());
        private ILogger __logger;

        private readonly bool isTransactional;

        private protected DMSIGrain(bool isTransactional) => this.isTransactional = isTransactional;

        public override Task OnActivateAsync()
        {
            var indexName = IndexUtils.GetIndexNameFromIndexGrain(this);
            _indexedField = indexName.Substring(2);
            return base.OnActivateAsync();
        }

        public Task<bool> DirectApplyIndexUpdateBatch(Immutable<IDictionary<IIndexableGrain, IList<IMemberUpdate>>> iUpdates,
                                                        bool isUnique, IndexMetaData idxMetaData, SiloAddress siloAddress = null)
            => Task.FromResult(true);   // The index is maintained by the underlying _grainStorage when its WriteStateAsync is called by the IndexedState implementation

        public Task<bool> DirectApplyIndexUpdate(IIndexableGrain g, Immutable<IMemberUpdate> iUpdate, bool isUniqueIndex,
                                                 IndexMetaData idxMetaData, SiloAddress siloAddress)
            => Task.FromResult(true);   // The index is maintained by the underlying _grainStorage when its WriteStateAsync is called by the IndexedState implementation

        public async Task LookupAsync(IOrleansQueryResultStream<V> result, K key)
        {
            var res = await LookupGrainReferences(key);
            await result.OnNextAsync(res.ToBatch());
            await result.OnCompletedAsync();
        }

        private async Task<List<V>> LookupGrainReferences(K key)
        {
            EnsureGrainStorage();

            // Dynamically find its LookupAsync method (currently only CosmosDB supports this).
            // TODO: define IOrleansIndexingStorageProvider (IOISP) for both this and the StateFieldsToIndex equivalent;
            // see https://github.com/dotnet/orleans/issues/5432.
            dynamic indexableStorageProvider = _grainStorage;

            // TODO: If the storage provider does not implement ITransactionalStorageProvider (ITSP), then the transaction facet
            // will wrap it with TransactionalStateStorageProviderWrapper (TSSPW); this will add another level to the path to the
            // actual data. Currently we always assume a wrapper because TSSPW is internal (so we can't check that the provider is
            // of that type) and we only support CosmosDB which does not implement ITSP yet.
            //   Modify this once CosmosDB supports ITSP, and perhaps include an IOISP.SupportsTransactions property.
            //   Keep this consistent with this.EnsureGrainStorage and BaseIndexingFixture.GetDSMIFieldsForASingleGrainType.
            var qualifiedField = (this.isTransactional ? $"{nameof(TransactionalStateRecord<object>.CommittedState)}." : "")
                                + IndexingConstants.UserStatePrefix + _indexedField;
            List<GrainReference> resultReferences = await indexableStorageProvider.LookupAsync<K>(_grainClassName, qualifiedField, key);
            return resultReferences.Select(grain => grain.Cast<V>()).ToList();
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

        public Task Dispose() => Task.CompletedTask;

        public Task<bool> IsAvailable() => Task.FromResult(true);

        Task IIndexInterface.LookupAsync(IOrleansQueryResultStream<IIndexableGrain> result, object key) => this.LookupAsync(result.Cast<V>(), (K)key);

        public async Task<IOrleansQueryResult<V>> LookupAsync(K key) => new OrleansQueryResult<V>(await this.LookupGrainReferences(key));

        async Task<IOrleansQueryResult<IIndexableGrain>> IIndexInterface.LookupAsync(object key) => await this.LookupAsync((K)key);

        private void EnsureGrainStorage()
        {
            if (_grainStorage == null)
            {
                var grainClassTypes = this.SiloIndexManager.IndexRegistry.GetImplementingGrainClasses(typeof(V));
                if (grainClassTypes.Length == 0)
                {
                    throw new IndexException($"There is no grain implementation class for DSMI grain interface {IndexUtils.GetFullTypeName(typeof(V))}.");
                }
                if (grainClassTypes.Length > 1)
                {
                    throw new IndexException($"There must be only one grain implementation class for DSMI grain interface {IndexUtils.GetFullTypeName(typeof(V))}.");
                }
                var grainClassType = grainClassTypes[0];
                this._grainClassName = IndexUtils.GetFullTypeName(grainClassType);

                _grainStorage = grainClassType.GetGrainStorage(this.SiloIndexManager.ServiceProvider);
                if (this.isTransactional)
                {
                    // TODO: This is also part of the TransactionalStateStorageProviderWrapper workaround; this is the name
                    // it passes to the StateStorageBridge, which ends up as the GrainType field in CosmosDB. Because there
                    // is currently no way to communicate the state name between the wrapper and here, the stateName passed
                    // to the TransactionalIndexAttribute facet for DMSI grains MUST BE IndexingConstants.IndexedGrainStateName.
                    _grainClassName = $"{RuntimeTypeNameFormatter.Format(grainClassType)}-{IndexingConstants.IndexedGrainStateName}";
                }
            }
        }
    }
}
