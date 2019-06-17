using System.Reflection;
using Orleans.Runtime;

namespace Orleans.Indexing.Facet
{
    internal class NonFaultTolerantWorkflowIndexedStateAttributeMapper : IndexedStateAttributeMapperBase,
                                                                        IAttributeToFactoryMapper<NonFaultTolerantWorkflowIndexedStateAttribute>
    {
        private static readonly MethodInfo CreateMethod = typeof(IIndexedStateFactory).GetMethod(nameof(IIndexedStateFactory.CreateNonFaultTolerantWorkflowIndexedState));

        public Factory<IGrainActivationContext, object> GetFactory(ParameterInfo parameter, NonFaultTolerantWorkflowIndexedStateAttribute attribute)
            => base.GetFactory(CreateMethod, parameter, attribute);
    }
}
