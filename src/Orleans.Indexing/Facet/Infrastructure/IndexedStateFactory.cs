using Microsoft.Extensions.DependencyInjection;
using Orleans.Runtime;

namespace Orleans.Indexing.Facet
{
    public class IndexedStateFactory : IIndexedStateFactory
    {
        private readonly IGrainActivationContext activationContext;

        public IndexedStateFactory(IGrainActivationContext activationContext, ITypeResolver typeResolver, IGrainFactory grainFactory)
            => this.activationContext = activationContext;

        public INonFaultTolerantWorkflowIndexedState<TState> CreateNonFaultTolerantWorkflowIndexedState<TState>(IIndexedStateConfiguration config)
            where TState : class, new()
            => this.CreateIndexedState<NonFaultTolerantWorkflowIndexedState<TState, IndexedGrainStateWrapper<TState>>>(config);

        public IFaultTolerantWorkflowIndexedState<TState> CreateFaultTolerantWorkflowIndexedState<TState>(IIndexedStateConfiguration config)
            where TState : class, new()
            => this.CreateIndexedState<FaultTolerantWorkflowIndexedState<TState>>(config);

        public ITransactionalIndexedState<TState> CreateTransactionalIndexedState<TState>(IIndexedStateConfiguration config)
            where TState : class, new()
            => this.CreateIndexedState<TransactionalIndexedState<TState>>(config);

        private TWrappedIndexedStateImplementation CreateIndexedState<TWrappedIndexedStateImplementation>(IIndexedStateConfiguration config)
            where TWrappedIndexedStateImplementation : ILifecycleParticipant<IGrainLifecycle>
        {
            var indexedState = ActivatorUtilities.CreateInstance<TWrappedIndexedStateImplementation>(this.activationContext.ActivationServices, config);
            indexedState.Participate(activationContext.ObservableLifecycle);
            return indexedState;
        }
    }
}
