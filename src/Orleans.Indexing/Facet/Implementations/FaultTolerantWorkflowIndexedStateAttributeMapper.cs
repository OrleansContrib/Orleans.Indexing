using System.Reflection;
using Orleans.Runtime;

namespace Orleans.Indexing.Facet
{
    internal class FaultTolerantWorkflowIndexedStateAttributeMapper : IndexedStateAttributeMapperBase,
                                                                     IAttributeToFactoryMapper<FaultTolerantWorkflowIndexedStateAttribute>
    {
        private static readonly MethodInfo CreateMethod = typeof(IIndexedStateFactory).GetMethod(nameof(IIndexedStateFactory.CreateFaultTolerantWorkflowIndexedState));

        public Factory<IGrainActivationContext, object> GetFactory(ParameterInfo parameter, FaultTolerantWorkflowIndexedStateAttribute attribute)
            => base.GetFactory(CreateMethod, parameter, attribute);
    }
}
