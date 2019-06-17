using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Orleans.Runtime;

namespace Orleans.Indexing.Facet
{
    abstract class IndexedStateAttributeMapperBase
    {
        public Factory<IGrainActivationContext, object> GetFactory(MethodInfo creator, ParameterInfo parameter, IIndexedStateConfiguration indexingConfig)
        {
            // Use generic type args to specialize the generic method and create the factory lambda.
            var genericCreate = creator.MakeGenericMethod(parameter.ParameterType.GetGenericArguments());
            var args = new object[] { indexingConfig };
            return context => this.Create(context, genericCreate, args);
        }

        private object Create(IGrainActivationContext context, MethodInfo genericCreate, object[] args)
        {
            var factory = context.ActivationServices.GetRequiredService<IIndexedStateFactory>();
            return genericCreate.Invoke(factory, args);
        }
    }
}
