using Orleans.Streams;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Orleans.Indexing
{
    /// <summary>
    /// This class is used for creating IAsyncBatchObserver in order to watch the first result of a query
    /// </summary>
    /// <typeparam name="T">type of object that is being observed</typeparam>
    public class QueryFirstResultStreamObserver<T> : IAsyncBatchObserver<T>
    {
        private Action<T> _action;

        public QueryFirstResultStreamObserver(Action<T> action) => this._action = action;

        public Task OnCompletedAsync() => Task.CompletedTask;

        public Task OnErrorAsync(Exception ex) => throw ex;

        private Task OnNextAsync(T item)
        {
            if (this._action != null)
            {
                this._action(item);
                this._action = null;
            }
            return Task.CompletedTask;
        }

        public async Task OnNextAsync(IList<SequentialItem<T>> items)
        {
            foreach (var item in items)
                await OnNextAsync(item.Item);
        }
    }
}
