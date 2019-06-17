using Orleans.Concurrency;
using Orleans.Indexing.Facet;
using Orleans.Transactions.Abstractions;
using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace Orleans.Indexing.Tests
{
    public abstract class TestMultiIndexGrainNonFaultTolerant<TGrainState> : TestMultiIndexGrain<TGrainState>
        where TGrainState : class, ITestMultiIndexState, new()
    {
        public TestMultiIndexGrainNonFaultTolerant(IIndexedState<TGrainState> indexedState) : base(indexedState)
            => Debug.Assert(this.GetType().GetConsistencyScheme() == ConsistencyScheme.NonFaultTolerantWorkflow);
    }

    public abstract class TestMultiIndexGrainFaultTolerant<TGrainState> : TestMultiIndexGrain<TGrainState>
        where TGrainState : class, ITestMultiIndexState, new()
    {
        public TestMultiIndexGrainFaultTolerant(IIndexedState<TGrainState> indexedState) : base(indexedState)
            => Debug.Assert(this.GetType().GetConsistencyScheme() == ConsistencyScheme.FaultTolerantWorkflow);
    }

    public abstract class TestMultiIndexGrainTransactional<TGrainState> : TestMultiIndexGrain<TGrainState>, ITestMultiIndexGrainTransactional
        where TGrainState : class, ITestMultiIndexState, new()
    {
        public TestMultiIndexGrainTransactional(IIndexedState<TGrainState> indexedState) : base(indexedState)
            => Debug.Assert(this.GetType().GetConsistencyScheme() == ConsistencyScheme.Transactional);

        Task<string> ITestMultiIndexGrainTransactional.GetUnIndexedString() => base.GetUnIndexedString();
        Task ITestMultiIndexGrainTransactional.SetUnIndexedString(string value) => base.SetUnIndexedString(value);

        Task<int> ITestMultiIndexGrainTransactional.GetUniqueInt() => base.GetUniqueInt();
        Task ITestMultiIndexGrainTransactional.SetUniqueInt(int value) => base.SetUniqueInt(value);

        Task<string> ITestMultiIndexGrainTransactional.GetUniqueString() => base.GetUniqueString();
        Task ITestMultiIndexGrainTransactional.SetUniqueString(string value) => base.SetUniqueString(value);

        Task<int> ITestMultiIndexGrainTransactional.GetNonUniqueInt() => base.GetNonUniqueInt();
        Task ITestMultiIndexGrainTransactional.SetNonUniqueInt(int value) => base.SetNonUniqueInt(value);

        Task<string> ITestMultiIndexGrainTransactional.GetNonUniqueString() => base.GetNonUniqueString();
        Task ITestMultiIndexGrainTransactional.SetNonUniqueString(string value) => base.SetNonUniqueString(value);
    }

    public abstract class TestMultiIndexGrain<TGrainState> : Grain, ITestMultiIndexGrain
        where TGrainState : class, ITestMultiIndexState, new()
    {
        private readonly TestMultiIndexGrainBase<TGrainState> testBase;

        public Task<string> GetUnIndexedString() => this.testBase.GetProperty(state => state.UnIndexedString);
        public Task SetUnIndexedString(string value) => this.testBase.SetAndWritePropertyAsync(state => state.UnIndexedString = value, retry:false);

        public Task<int> GetUniqueInt() => this.testBase.GetProperty(state => state.UniqueInt);
        public Task SetUniqueInt(int value) => this.testBase.SetAndWritePropertyAsync(state => state.UniqueInt = value, retry: this.testBase.IsUniqueIntIndexed);

        public Task<string> GetUniqueString() => this.testBase.GetProperty(state => state.UniqueString);
        public Task SetUniqueString(string value) => this.testBase.SetAndWritePropertyAsync(state => state.UniqueString = value, retry: this.testBase.IsUniqueStringIndexed);

        public Task<int> GetNonUniqueInt() => this.testBase.GetProperty(state => state.NonUniqueInt);
        public Task SetNonUniqueInt(int value) => this.testBase.SetAndWritePropertyAsync(state => state.NonUniqueInt = value, retry: this.testBase.IsNonUniqueIntIndexed);

        public Task<string> GetNonUniqueString() => this.testBase.GetProperty(state => state.NonUniqueString);
        public Task SetNonUniqueString(string value) => this.testBase.SetAndWritePropertyAsync(state => state.NonUniqueString = value, retry: this.testBase.IsNonUniqueStringIndexed);

        public Task Deactivate()
        {
            base.DeactivateOnIdle();
            return Task.CompletedTask;
        }

        public TestMultiIndexGrain(IIndexedState<TGrainState> indexedState) => this.testBase = new TestMultiIndexGrainBase<TGrainState>(this.GetType(), indexedState);

        #region Required shims for IIndexableGrain methods for fault tolerance
        public Task<Immutable<System.Collections.Generic.HashSet<Guid>>> GetActiveWorkflowIdsSet() => this.testBase.IndexedState.GetActiveWorkflowIdsSet();
        public Task RemoveFromActiveWorkflowIds(System.Collections.Generic.HashSet<Guid> removedWorkflowId) => this.testBase.IndexedState.RemoveFromActiveWorkflowIds(removedWorkflowId);
        #endregion Required shims for IIndexableGrain methods for fault tolerance
    }
}
