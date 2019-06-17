using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Collections;
using System.Linq.Expressions;
using Orleans.Streams;

namespace Orleans.Indexing
{
    /// <summary>
    /// The top-level class for query objects
    /// </summary>
    public abstract class QueryGrainsNode
    {
        public IIndexFactory IndexFactory { get; }

        public IStreamProvider StreamProvider { get; }

        public QueryGrainsNode(IIndexFactory indexFactory, IStreamProvider streamProvider)
        {
            this.IndexFactory = indexFactory;
            this.StreamProvider = streamProvider;
        }
    }

    /// <summary>
    /// The top-level class for query objects, which implements <see cref="IOrleansQueryable{G, P}"/>
    /// </summary>
    public abstract class QueryGrainsNode<TIGrain, TProperties> : QueryGrainsNode, IOrleansQueryable<TIGrain, TProperties> where TIGrain : IIndexableGrain
    {
        public QueryGrainsNode(IIndexFactory indexFactory, IStreamProvider streamProvider) : base(indexFactory, streamProvider)
        {
        }

        public virtual Type ElementType => typeof(TIGrain);

        public virtual Expression Expression => Expression.Constant(this);

        public virtual IQueryProvider Provider => new OrleansQueryProvider<TIGrain, TProperties>();

        /// <summary>
        /// This method gets the result of executing the query on this query object
        /// </summary>
        /// <returns>the query result</returns>
        public abstract Task ObserveResults(IAsyncBatchObserver<TIGrain> observer);

        public abstract Task<IOrleansQueryResult<TIGrain>> GetResults();

        public IEnumerator<TProperties> GetEnumerator()
            => throw new NotSupportedException("GetEnumerator is not supported on QueryGrainsNode.");

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
