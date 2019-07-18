using Orleans.Runtime;
using System;
using System.Linq;
using System.Reflection;

namespace Orleans.Indexing
{
    public static class IndexValidator
    {
        public static void Validate(Type[] types)
        {
            var _ = ApplicationPartsIndexableGrainLoader.GetIndexRegistry(null, types);
        }
    }
}
