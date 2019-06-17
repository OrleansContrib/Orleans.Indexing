using System;

namespace Orleans.Indexing.Facet
{
    /// <summary>
    /// Marker interface for indexed state management.
    /// </summary>
    public abstract class IndexedStateAttribute : Attribute
    {
        public string StateName { get; private protected set; }

        public string StorageName { get; private protected set; }

        public IndexedStateAttribute(string stateName, string storageName)
        {
            this.StateName = stateName;
            this.StorageName = storageName;
        }
    }
}
