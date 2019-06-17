using System.Reflection;
using Orleans.Runtime;

namespace Orleans.Indexing.Facet
{
    class TransactionalIndexedStateAttributeMapper : IndexedStateAttributeMapperBase,
                                                    IAttributeToFactoryMapper<TransactionalIndexedStateAttribute>
    {
        private static readonly MethodInfo CreateMethod = typeof(IIndexedStateFactory).GetMethod(nameof(IIndexedStateFactory.CreateTransactionalIndexedState));

        public Factory<IGrainActivationContext, object> GetFactory(ParameterInfo parameter, TransactionalIndexedStateAttribute attribute)
            => base.GetFactory(CreateMethod, parameter, attribute);
    }
}
