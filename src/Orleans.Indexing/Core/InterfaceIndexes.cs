using System;
using System.Collections.Generic;
using System.Linq;
using Orleans.Concurrency;

namespace Orleans.Indexing
{
    internal class InterfaceIndexes
    {
        internal NamedIndexMap NamedIndexes { get; }
        internal object Properties { get; set; }
        internal Type PropertiesType => this.NamedIndexes.PropertiesClassType;

        /// <summary>
        /// An immutable copy of before-images of the indexed fields
        /// </summary>
        internal Immutable<IDictionary<string, object>> BeforeImages = new Dictionary<string, object>().AsImmutable<IDictionary<string, object>>();

        internal InterfaceIndexes(NamedIndexMap indexes) => this.NamedIndexes = indexes;

        internal bool HasIndexImages => this.BeforeImages.Value.Values.Any(obj => obj != null);
    }
}
