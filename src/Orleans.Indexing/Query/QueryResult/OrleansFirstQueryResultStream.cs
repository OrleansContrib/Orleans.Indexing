using Orleans.Streams;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Orleans.Indexing
{
    /// <summary>
    /// This class limits the query result to a single record.
    /// As soon as the single record is found, it stops the underlying stream.
    /// </summary>
    /// <typeparam name="TIGrain">type of grain for query result</typeparam>
    [Serializable]
    public class OrleansFirstQueryResultStream<TIGrain> : OrleansQueryResultStream<TIGrain> where TIGrain : IIndexableGrain
    {
        public OrleansFirstQueryResultStream() : this(CreateNewStream())
        {
        }

        // Accept a queryResult instance which we shall observe
        public OrleansFirstQueryResultStream(IAsyncStream<TIGrain> stream) : base(stream)
        {
        }

        // Creates a temporary new stream for the query result, when a stream is not provided from caller
        private static IAsyncStream<TIGrain> CreateNewStream() => throw new NotImplementedException();

        public override async Task OnNextAsync(TIGrain item, StreamSequenceToken token = null)
        {
            await this._stream.OnNextAsync(item, token);
            await this._stream.OnCompletedAsync();
        }

        public override async Task OnNextAsync(IList<SequentialItem<TIGrain>> batch)
        {
            var item = batch.FirstOrDefault();
            await (item != null ? this.OnNextAsync(item.Item, item.Token) : Task.CompletedTask);
        }
    }
}
