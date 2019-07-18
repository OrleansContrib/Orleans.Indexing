using System;
using Orleans.Runtime;
using System.Reflection;
using Orleans.Streams;
using System.Threading.Tasks;
using System.Linq.Expressions;
using Orleans.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Orleans.Indexing
{
    /// <summary>
    /// A utility class for the index operations
    /// </summary>
    internal class IndexFactory : IIndexFactory
    {
        // Both indexManager and siloIndexManager are needed as this may be run in the ClusterClient or Silo.
        private IndexManager indexManager;
        private SiloIndexManager siloIndexManager;
        private IGrainFactory grainFactory;

        private bool IsInSilo => this.siloIndexManager != null;

        public IndexFactory(IndexManager im, IGrainFactory gf)
        {
            this.indexManager = im;
            this.siloIndexManager = im as SiloIndexManager;
            this.grainFactory = gf;
        }

        #region IIndexFactory

        /// <summary>
        /// This method queries the active grains for the given grain interface and the filter expression. The filter
        /// expression should contain an indexed field.
        /// </summary>
        /// <typeparam name="TIGrain">the given indexable grain interface type to query over its active instances</typeparam>
        /// <typeparam name="TProperties">the property type to query over</typeparam>
        /// <param name="filterExpr">the filter expression of the query</param>
        /// <param name="queryResultObserver">the observer object to be called on every grain found for the query</param>
        /// <returns>the result of the query</returns>
        public Task GetActiveGrains<TIGrain, TProperties>(Expression<Func<TProperties, bool>> filterExpr,
                                IAsyncBatchObserver<TIGrain> queryResultObserver) where TIGrain : IIndexableGrain
            => this.GetActiveGrains<TIGrain, TProperties>().Where(filterExpr).ObserveResults(queryResultObserver);

        /// <summary>
        /// This method queries the active grains for the given grain interface and the filter expression. The filter
        /// expression should contain an indexed field.
        /// </summary>
        /// <typeparam name="TIGrain">the given indexable grain interface type to query over its active instances</typeparam>
        /// <typeparam name="TProperties">the property type to query over</typeparam>
        /// <param name="streamProvider">the stream provider for the query results</param>
        /// <param name="filterExpr">the filter expression of the query</param>
        /// <param name="queryResultObserver">the observer object to be called on every grain found for the query</param>
        /// <returns>the result of the query</returns>
        public Task GetActiveGrains<TIGrain, TProperties>(IStreamProvider streamProvider,
                                Expression<Func<TProperties, bool>> filterExpr, IAsyncBatchObserver<TIGrain> queryResultObserver) where TIGrain : IIndexableGrain
            => this.GetActiveGrains<TIGrain, TProperties>(streamProvider).Where(filterExpr).ObserveResults(queryResultObserver);

        /// <summary>
        /// This method queries the active grains for the given grain interface.
        /// </summary>
        /// <typeparam name="TIGrain">the given indexable grain interface type to query over its active instances</typeparam>
        /// <typeparam name="TProperty">the property type to query over</typeparam>
        /// <returns>the query to lookup all active grains of a given type</returns>
        public IOrleansQueryable<TIGrain, TProperty> GetActiveGrains<TIGrain, TProperty>() where TIGrain : IIndexableGrain
            => this.GetActiveGrains<TIGrain, TProperty>(this.indexManager.ServiceProvider.GetRequiredServiceByName<IStreamProvider>(IndexingConstants.INDEXING_STREAM_PROVIDER_NAME));

        /// <summary>
        /// This method queries the active grains for the given grain interface.
        /// </summary>
        /// <typeparam name="TIGrain">the given indexable grain interface type to query over its active instances</typeparam>
        /// <typeparam name="TProperty">the property type to query over</typeparam>
        /// <param name="streamProvider">the stream provider for the query results</param>
        /// <returns>the query to lookup all active grains of a given type</returns>
        public IOrleansQueryable<TIGrain, TProperty> GetActiveGrains<TIGrain, TProperty>(IStreamProvider streamProvider) where TIGrain : IIndexableGrain
            => new QueryActiveGrainsNode<TIGrain, TProperty>(this, streamProvider);

        /// <summary>
        /// Gets an <see cref="IIndexInterface{K,V}"/> given its name
        /// </summary>
        /// <typeparam name="K">key type of the index</typeparam>
        /// <typeparam name="V">value type of the index, which is the grain being indexed</typeparam>
        /// <param name="indexName">the name of the index, which is the identifier of the index</param>
        /// <returns>the <see cref="IIndexInterface{K,V}"/> with the specified name</returns>
        public IIndexInterface<K, V> GetIndex<K, V>(string indexName) where V : IIndexableGrain
            => (IIndexInterface<K, V>)this.GetIndex(typeof(V), indexName);

        /// <summary>
        /// Gets an IndexInterface given its name and grain interface type
        /// </summary>
        /// <param name="indexName">the name of the index, which is the identifier of the index</param>
        /// <param name="iInterfaceType">the grain interface type that is being indexed</param>
        /// <returns>the IndexInterface with the specified name on the given grain interface type</returns>
        public IIndexInterface GetIndex(Type iInterfaceType, string indexName)
        {
            // It should never happen that the indexes are not loaded if the index is registered in the index registry
            return GetGrainIndexes(iInterfaceType).TryGetValue(indexName, out IndexInfo indexInfo)
                ? indexInfo.IndexInterface
                : throw new IndexException(string.Format("Index \"{0}\" does not exist for {1}.", indexName, iInterfaceType));
        }

        #endregion IIndexFactory

        #region internal functions

        /// <summary>
        /// Provides the index information for a given grain interface type.
        /// </summary>
        /// <param name="iInterfaceType">The target grain interface type</param>
        /// <returns>A per-grain index registry, which may be empty if the grain does not have indexes.</returns>
        internal NamedIndexMap GetGrainIndexes(Type iInterfaceType)
        {
            return this.indexManager.IndexRegistry.TryGetValue(iInterfaceType, out var grainIndexes)
                ? grainIndexes
                : throw new IndexException($"No indexes exist for interface {iInterfaceType.Name}");
        }

        /// <summary>
        /// This is a helper method for creating an index on a field of an actor.
        /// </summary>
        /// <param name="idxType">The type of index to be created</param>
        /// <param name="indexName">The index name to be created</param>
        /// <param name="isUniqueIndex">Determines whether this is a unique index that needs to be created</param>
        /// <param name="isEager">Determines whether updates to this index should be applied eagerly or not</param>
        /// <param name="maxEntriesPerBucket">Determines the maximum number of entries in
        /// each bucket of a distributed index, if this index type is a distributed one.</param>
        /// <param name="indexedProperty">the PropertyInfo object for the indexed field.
        /// This object helps in creating a default instance of IndexUpdateGenerator.</param>
        /// <returns>An <see cref="IndexInfo"/> for the specified idxType and indexName.</returns>
        internal IndexInfo CreateIndex(Type idxType, string indexName, bool isUniqueIndex, bool isEager, int maxEntriesPerBucket, PropertyInfo indexedProperty)
        {
            ValidateIndexType(idxType, indexedProperty, out var iIndexGenericType, out var grainInterfaceType);

            // This must call the static Silo(IndexManager) methods because we may not be in the silo.
            var index = typeof(IGrain).IsAssignableFrom(idxType)
                ? (IIndexInterface)this.grainFactory.GetGrain(idxType, IndexUtils.GetIndexGrainPrimaryKey(grainInterfaceType, indexName))
                : idxType.IsClass
                    ? (IIndexInterface)Activator.CreateInstance(idxType, this.indexManager.ServiceProvider, indexName, isUniqueIndex)
                    : throw new IndexException(string.Format("{0} is neither a grain nor a class. Index \"{1}\" cannot be created.", idxType, indexName));

            return new IndexInfo(index, new IndexMetaData(idxType, indexName, isUniqueIndex, isEager, maxEntriesPerBucket), CreateIndexUpdateGenFromProperty(indexedProperty));
        }

        internal static void ValidateIndexType(Type idxType, PropertyInfo indexedProperty, out Type iIndexGenericType, out Type grainInterfaceType)
        {
            iIndexGenericType = idxType.GetGenericType(typeof(IIndexInterface<,>))
                                    ?? throw new NotSupportedException("Adding an index that does not implement IndexInterface<K,V> is not supported yet." +
                                                                       $" Your requested index ({idxType.ToString()}) is invalid.");

            Type[] indexTypeArgs = iIndexGenericType.GetGenericArguments();
            var keyType = indexTypeArgs[0];
            grainInterfaceType = indexTypeArgs[1];
            if (keyType != indexedProperty.PropertyType)
            {
                throw new NotSupportedException($"Index <{idxType.ToString()}, V> does not match property '{indexedProperty.PropertyType} {indexedProperty.Name}'");
            }
        }

        internal static void RegisterIndexWorkflowQueueGrainServices(IServiceCollection services, Type grainInterfaceType, IndexingOptions indexingOptions, bool isFaultTolerant)
        {
            for (int i = 0; i < indexingOptions.NumWorkflowQueuesPerInterface; ++i)
            {
                var seq = i;    // Captured by the lambda
                services.AddGrainService(sp => new IndexWorkflowQueueGrainService(sp.GetRequiredService<SiloIndexManager>(), grainInterfaceType, seq, isFaultTolerant));
                services.AddGrainService(sp => new IndexWorkflowQueueHandlerGrainService(sp.GetRequiredService<SiloIndexManager>(), grainInterfaceType, seq, isFaultTolerant));
            }
        }

        #endregion internal functions

        #region private functions

        private static IIndexUpdateGenerator CreateIndexUpdateGenFromProperty(PropertyInfo indexedProperty)
            => new IndexUpdateGenerator(indexedProperty);

        #endregion private functions
    }
}
