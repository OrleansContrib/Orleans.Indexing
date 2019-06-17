using System;
using System.Collections.Generic;

namespace Orleans.Indexing
{
    /// <summary>
    /// Extension for the built-in <see cref="IEnumerable{T}"/> and <see cref="IDisposable"/>
    /// allowing for Orleans specific operations, which represents the results of a query.
    /// 
    /// IOrleansQueryResult contains the whole result of a query.
    /// </summary>
    /// <typeparam name="TIGrain">the grain interface type, which is the type of elements in the query result</typeparam>
    public interface IOrleansQueryResult<out TIGrain> : IEnumerable<TIGrain>, IDisposable where TIGrain : IIndexableGrain
    {
    }
}
