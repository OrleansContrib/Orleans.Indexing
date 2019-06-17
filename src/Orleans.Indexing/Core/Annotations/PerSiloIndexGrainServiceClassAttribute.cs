using System;

namespace Orleans.Indexing
{
    [AttributeUsage(AttributeTargets.Interface)]
    public class PerSiloIndexGrainServiceClassAttribute : Attribute
    {
        internal Type GrainServiceClassType { get; }

        public PerSiloIndexGrainServiceClassAttribute(Type grainServiceClassType) => this.GrainServiceClassType = grainServiceClassType;
    }
}
