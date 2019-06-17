using Orleans.Streams;
using System;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace Orleans.Indexing
{
    public interface IIndexFactory
    {
        /// <summary>
        /// This method queries the active grains for the given grain interface and the filter expression. The filter
        /// expression should contain an indexed field.
        /// </summary>
        /// <typeparam name="TIGrain">the given grain interface type to query over its active instances</typeparam>
        /// <typeparam name="TProperties">the property type to query over</typeparam>
        /// <param name="filterExpr">the filter expression of the query</param>
        /// <param name="queryResultObserver">the observer object to be called on every grain found for the query</param>
        /// <returns>the result of the query</returns>
        Task GetActiveGrains<TIGrain, TProperties>(Expression<Func<TProperties, bool>> filterExpr,
                                IAsyncBatchObserver<TIGrain> queryResultObserver) where TIGrain : IIndexableGrain;

        /// <summary>
        /// This method queries the active grains for the given grain interface and the filter expression. The filter
        /// expression should contain an indexed field.
        /// </summary>
        /// <typeparam name="TIGrain">the given grain interface type to query over its active instances</typeparam>
        /// <typeparam name="TProperties">the property type to query over</typeparam>
        /// <param name="streamProvider">the stream provider for the query results</param>
        /// <param name="filterExpr">the filter expression of the query</param>
        /// <param name="queryResultObserver">the observer object to be called on every grain found for the query</param>
        /// <returns>the result of the query</returns>
        Task GetActiveGrains<TIGrain, TProperties>(IStreamProvider streamProvider,
                                Expression<Func<TProperties, bool>> filterExpr, IAsyncBatchObserver<TIGrain> queryResultObserver) where TIGrain : IIndexableGrain;

        /// <summary>
        /// This method queries the active grains for the given grain interface.
        /// </summary>
        /// <typeparam name="TIGrain">the given grain interface type to query over its active instances</typeparam>
        /// <typeparam name="TProperty">the property type to query over</typeparam>
        /// <returns>the query to lookup all active grains of a given type</returns>
        IOrleansQueryable<TIGrain, TProperty> GetActiveGrains<TIGrain, TProperty>() where TIGrain : IIndexableGrain;

        /// <summary>
        /// This method queries the active grains for the given grain interface.
        /// </summary>
        /// <typeparam name="TIGrain">the given grain interface type to query over its active instances</typeparam>
        /// <typeparam name="TProperty">the property type to query over</typeparam>
        /// <param name="streamProvider">the stream provider for the query results</param>
        /// <returns>the query to lookup all active grains of a given type</returns>
        IOrleansQueryable<TIGrain, TProperty> GetActiveGrains<TIGrain, TProperty>(IStreamProvider streamProvider) where TIGrain : IIndexableGrain;

        /// <summary>
        /// Gets an <see cref="IIndexInterface{K,V}"/> given its name
        /// </summary>
        /// <typeparam name="K">key type of the index</typeparam>
        /// <typeparam name="V">value type of the index, which is the grain being indexed</typeparam>
        /// <param name="indexName">the name of the index, which is the identifier of the index</param>
        /// <returns>the <see cref="IIndexInterface{K,V}"/> with the specified name</returns>
        IIndexInterface<K, V> GetIndex<K, V>(string indexName) where V : IIndexableGrain;

        /// <summary>
        /// Gets an IndexInterface given its name and grain interface type
        /// </summary>
        /// <param name="indexName">the name of the index, which is the identifier of the index</param>
        /// <param name="grainInterfaceType">the grain interface type that is being indexed</param>
        /// <returns>the IndexInterface with the specified name on the given grain interface type</returns>
        IIndexInterface GetIndex(Type grainInterfaceType, string indexName);
    }
}
