using System;

namespace Orleans.Indexing
{
    /// <summary>
    /// Supplies the transactional variant form of the index class or interface that carries this annotation.
    /// </summary>
    [AttributeUsage(AttributeTargets.Interface | AttributeTargets.Class)]
    internal class TransactionalIndexVariantAttribute : Attribute
    {
        internal Type TransactionalIndexType { get; }

        internal TransactionalIndexVariantAttribute(Type transactionalImplementationType)
            => this.TransactionalIndexType = transactionalImplementationType;
    }
}
