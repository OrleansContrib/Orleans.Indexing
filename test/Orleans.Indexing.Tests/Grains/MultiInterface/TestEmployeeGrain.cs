using System.Threading.Tasks;
using Orleans.Concurrency;
using Orleans.Indexing.Facet;
using System;

namespace Orleans.Indexing.Tests.MultiInterface
{
    public abstract class TestEmployeeGrain<TGrainState> : Grain
        where TGrainState : class, IEmployeeGrainState, new()
    {
        // This is populated by Orleans.Indexing with the indexes from the implemented interfaces on this class.
        private readonly IIndexedState<TGrainState> indexedState;

        private TGrainState cachedState = new TGrainState();

        // This illustrates implementing the Grain interfaces to get and set the properties.
        #region IPersonInterface
        public Task<string> GetName() => Task.FromResult(this.cachedState.Name);
        public Task SetName(string value) { this.cachedState.Name = value; return Task.CompletedTask; }

        public Task<int> GetAge() => Task.FromResult(this.cachedState.Age);
        public Task SetAge(int value) { this.cachedState.Age = value; return Task.CompletedTask; }
        #endregion IPersonInterface

        #region IJobInterface
        public Task<string> GetTitle() => Task.FromResult(this.cachedState.Title);
        public Task SetTitle(string value) { this.cachedState.Title = value; return Task.CompletedTask; }

        public Task<string> GetDepartment() => Task.FromResult(this.cachedState.Department);
        public Task SetDepartment(string value) { this.cachedState.Department = value; return Task.CompletedTask; }
        #endregion IJobInterface

        #region IEmployeeProperties
        public Task<int> GetEmployeeId() => Task.FromResult(this.cachedState.EmployeeId);
        public Task SetEmployeeId(int value) { this.cachedState.EmployeeId = value; return Task.CompletedTask; }
        #endregion IEmployeeProperties

        #region IEmployeeGrainState - not indexed
        public Task<int> GetSalary() => Task.FromResult(this.cachedState.Salary);
        public Task SetSalary(int value) { this.cachedState.Salary = value; return Task.CompletedTask; }
        #endregion IEmployeeGrainState - not indexed

        public Task InitializeStateTxn() => InitializeState();
        public Task WriteStateTxn() => WriteState();

        public Task InitializeState() => this.indexedState.PerformRead(state => { this.cachedState.ShallowCopyFrom(state); return true; });

        public Task WriteState() => this.indexedState.PerformUpdate(state => state.ShallowCopyFrom(this.cachedState));

        public Task Deactivate() { base.DeactivateOnIdle(); return Task.CompletedTask; }

        protected TestEmployeeGrain(IIndexedState<TGrainState> indexedState) => this.indexedState = indexedState;

        #region Required shims for IIndexableGrain methods for fault tolerance
        public Task<Immutable<System.Collections.Generic.HashSet<Guid>>> GetActiveWorkflowIdsSet() => this.indexedState.GetActiveWorkflowIdsSet();
        public Task RemoveFromActiveWorkflowIds(System.Collections.Generic.HashSet<Guid> removedWorkflowId) => this.indexedState.RemoveFromActiveWorkflowIds(removedWorkflowId);
        #endregion Required shims for IIndexableGrain methods for fault tolerance
    }
}
