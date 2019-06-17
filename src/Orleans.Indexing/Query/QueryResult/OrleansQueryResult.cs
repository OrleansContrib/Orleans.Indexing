using System;
using System.Collections.Generic;
using System.Collections;

namespace Orleans.Indexing
{
    /// <summary>
    /// This class represents the whole result of a query.
    /// </summary>
    /// <typeparam name="TIGrain">type of grain for query result</typeparam>
    [Serializable]
    public class OrleansQueryResult<TIGrain> : IOrleansQueryResult<TIGrain> where TIGrain : IIndexableGrain
    {
        protected IEnumerable<TIGrain> _results;

        public OrleansQueryResult(IEnumerable<TIGrain> results) => this._results = results;

        public void Dispose() => this._results = null;

        public IEnumerator<TIGrain> GetEnumerator() => this._results.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => this._results.GetEnumerator();
    }
}
