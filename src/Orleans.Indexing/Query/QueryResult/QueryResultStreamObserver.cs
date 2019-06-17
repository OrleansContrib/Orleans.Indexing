using Orleans.Streams;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Orleans.Indexing
{
    /// <summary>
    /// This class is used for creating IAsyncBatchObserver in order to watch the result of a query
    /// </summary>
    /// <typeparam name="T">type of objects that are being observed</typeparam>
    public class QueryResultStreamObserver<T> : IAsyncBatchObserver<T>
    {
        private Func<T, Task> _onNext;
        private Func<Task> _onCompleted;

        public QueryResultStreamObserver(Func<T, Task> onNext, Func<Task> onCompleted = null)
        {
            this._onNext = onNext;
            this._onCompleted = onCompleted;
        }

        public Task OnCompletedAsync()
            => this._onCompleted != null ? this._onCompleted() : Task.CompletedTask;

        public Task OnErrorAsync(Exception ex) => throw ex;

        public Task OnNextAsync(T item, StreamSequenceToken token = null)
            => this._onNext(item);

        public Task OnNextAsync(IList<SequentialItem<T>> batch)
            => Task.WhenAll(batch.Select(item => this._onNext(item.Item)));
    }
}
