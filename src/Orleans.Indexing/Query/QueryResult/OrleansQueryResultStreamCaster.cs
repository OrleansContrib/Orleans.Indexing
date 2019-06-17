using Orleans.Streams;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Orleans.Indexing
{
    /// <summary>
    /// This class casts IOrleansQueryResultStream{FromTP} to IOrleansQueryResultStream{ToTP}.
    /// 
    /// As IOrleansQueryResultStream{T} cannot be a covariant type (because it extends IAsyncObservable),
    /// this class is required when a conversion between two IOrleansQueryResultStream types is required.
    /// 
    /// It is not possible to subscribe to an instance of this class directly.
    /// One should use the original IOrleansQueryResultStream{FromTP} for subscription.
    /// </summary>
    /// <typeparam name="FromTP">type of grain for input IOrleansQueryResultStream</typeparam>
    /// <typeparam name="ToTP">type of grain for output IOrleansQueryResultStream</typeparam>

    [Serializable]
    public class OrleansQueryResultStreamCaster<FromTP, ToTP> : IOrleansQueryResultStream<ToTP> where FromTP : IIndexableGrain where ToTP : IIndexableGrain
    {
        protected IOrleansQueryResultStream<FromTP> _stream;

        // Accept a queryResult instance which we shall observe
        public OrleansQueryResultStreamCaster(IOrleansQueryResultStream<FromTP> stream)
            => this._stream = stream;

        public IOrleansQueryResultStream<TOGrain> Cast<TOGrain>() where TOGrain : IIndexableGrain 
            => typeof(TOGrain) == typeof(FromTP)
                ? (IOrleansQueryResultStream<TOGrain>)this._stream
                : new OrleansQueryResultStreamCaster<FromTP, TOGrain>(this._stream);

        public void Dispose() => this._stream.Dispose();

        public Task OnCompletedAsync() => this._stream.OnCompletedAsync();

        public Task OnErrorAsync(Exception ex) => this._stream.OnErrorAsync(ex);

        public Task OnNextAsync(IList<SequentialItem<ToTP>> batch)
        {
            var newBatch = batch.Select(item => new SequentialItem<FromTP>(item.Item.AsReference<FromTP>(), item.Token)).ToList();
            return _stream.OnNextAsync(newBatch);
        }

        public Task<StreamSubscriptionHandle<ToTP>> SubscribeAsync(IAsyncBatchObserver<ToTP> observer)
            => throw new NotSupportedException();

        public Task<StreamSubscriptionHandle<ToTP>> SubscribeAsync(IAsyncBatchObserver<ToTP> observer, StreamSequenceToken token)
            => throw new NotSupportedException();
    }
}
