using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Orleans.Concurrency;
using Orleans.Indexing.Facet;

namespace Orleans.Indexing.Tests
{
    public abstract class PlayerGrainNonFaultTolerant<TGrainState> : PlayerGrain<TGrainState>
        where TGrainState : PlayerGrainState, new()
    {
        public PlayerGrainNonFaultTolerant(IIndexedState<TGrainState> indexedState) : base(indexedState)
            => Debug.Assert(this.GetType().GetConsistencyScheme() == ConsistencyScheme.NonFaultTolerantWorkflow);
    }

    public abstract class PlayerGrainFaultTolerant<TGrainState> : PlayerGrain<TGrainState>
        where TGrainState : PlayerGrainState, new()
    {
        public PlayerGrainFaultTolerant(IIndexedState<TGrainState> indexedState) : base(indexedState)
            => Debug.Assert(this.GetType().GetConsistencyScheme() == ConsistencyScheme.FaultTolerantWorkflow);
    }

    public abstract class PlayerGrainTransactional<TGrainState> : PlayerGrain<TGrainState>
        where TGrainState : PlayerGrainState, new()
    {
        public PlayerGrainTransactional(IIndexedState<TGrainState> indexedState) : base(indexedState)
            => Debug.Assert(this.GetType().GetConsistencyScheme() == ConsistencyScheme.Transactional);
    }

    /// <summary>
    /// A simple grain that represents a player in a game
    /// </summary>
    public abstract class PlayerGrain<TGrainState> : Grain, IPlayerGrain
        where TGrainState : PlayerGrainState, new()
    {
        // This is populated by Orleans.Indexing with the indexes from the implemented interfaces on this class.
        private readonly IIndexedState<TGrainState> indexedState;

        internal Task<TResult> GetProperty<TResult>(Func<TGrainState, TResult> readFunction)
            => this.indexedState.PerformRead(readFunction);

        internal Task SetProperty(Action<TGrainState> setterAction, bool retry)
            => IndexingTestUtils.SetPropertyAndWriteStateAsync(setterAction, this.indexedState, retry);

        public Task<string> GetLocation() => this.GetProperty(state => state.Location);

        public async Task SetLocation(string value) => await this.SetProperty(state => state.Location = value, retry:true);

        public Task<int> GetScore() => this.GetProperty(state => state.Score);

        public Task SetScore(int value) => this.SetProperty(state => state.Score = value, retry: false);

        public Task<string> GetEmail() => this.GetProperty(state => state.Email);

        public Task SetEmail(string value) => this.SetProperty(state => state.Email = value, retry: false);

        public Task Deactivate()
        {
            this.DeactivateOnIdle();
            return Task.CompletedTask;
        }

        public PlayerGrain(IIndexedState<TGrainState> indexedState) => this.indexedState = indexedState;

        #region Required shims for IIndexableGrain methods for fault tolerance
        public Task<Immutable<System.Collections.Generic.HashSet<Guid>>> GetActiveWorkflowIdsSet() => this.indexedState.GetActiveWorkflowIdsSet();
        public Task RemoveFromActiveWorkflowIds(System.Collections.Generic.HashSet<Guid> removedWorkflowId) => this.indexedState.RemoveFromActiveWorkflowIds(removedWorkflowId);
        #endregion Required shims for IIndexableGrain methods for fault tolerance
    }
}
