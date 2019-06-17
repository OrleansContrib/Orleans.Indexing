using Orleans.Runtime;
using System;
using System.Linq;
using System.Reflection;

namespace Orleans.Indexing
{
    public static class IndexValidator
    {
        public static void Validate(Assembly assembly)
        {
            var grainClassTypes = ApplicationPartsIndexableGrainLoader.GetAssemblyIndexedConcreteGrainClasses(assembly);
            var _ = ApplicationPartsIndexableGrainLoader.GetIndexRegistry(null, grainClassTypes);
        }
    }
}
