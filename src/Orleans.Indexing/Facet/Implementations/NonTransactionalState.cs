using System.Threading.Tasks;
using Orleans.Core;
using Orleans.Transactions.Abstractions;

namespace Orleans.Indexing.Facet
{
    internal class NonTransactionalState<TGrainState> : ITransactionalState<TGrainState>
        where TGrainState : class, new()
    {
        private readonly IStorage<TGrainState> storage;

        private NonTransactionalState(IStorage<TGrainState> storage)    // private; use Create()
            => this.storage = storage;

        internal TGrainState State => this.storage.State;

        internal static async Task<NonTransactionalState<TGrainState>> CreateAsync(IStorage<TGrainState> storage)
        {
            await storage.ReadStateAsync();
            return new NonTransactionalState<TGrainState>(storage);
        }

        public Task<TResult> PerformRead<TResult>(System.Func<TGrainState, TResult> readFunction)
            => Task.FromResult(readFunction(this.State));

        public Task PerformUpdate() => this.PerformUpdate(_ => true);

        public async Task<TResult> PerformUpdate<TResult>(System.Func<TGrainState, TResult> updateFunction)
        {
            var result = updateFunction(this.State);
            await this.storage.WriteStateAsync();
            return result;
        }
    }
}
