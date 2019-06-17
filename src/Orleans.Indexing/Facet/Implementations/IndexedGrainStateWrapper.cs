using System;
using System.Collections.Generic;

namespace Orleans.Indexing.Facet
{
    /// <summary>
    /// A wrapper around a user-defined state, TGrainState, which indicates whether the grain has been persisted.
    /// </summary>
    /// <typeparam name="TGrainState">the type of user state</typeparam>
    [Serializable]
    public class IndexedGrainStateWrapper<TGrainState>
        where TGrainState: new()
    {
        /// <summary>
        /// Indicates whether the grain was read from storage (used on startup to set null values).
        /// </summary>
        public bool AreNullValuesInitialized;

        /// <summary>
        /// The actual user state.
        /// </summary>
        public TGrainState UserState = (TGrainState)Activator.CreateInstance(typeof(TGrainState));

        internal void EnsureNullValues(IReadOnlyDictionary<string, object> propertyNullValues)
        {
            if (!this.AreNullValuesInitialized)
            {
                foreach (var propInfo in typeof(TGrainState).GetProperties())
                {
                    var nullValue = IndexUtils.GetNullValue(propInfo);
                    if (nullValue != null || propertyNullValues.TryGetValue(propInfo.Name, out nullValue))
                    {
                        propInfo.SetValue(this.UserState, nullValue);
                    }
                }
                this.AreNullValuesInitialized = true;
            }
        }
    }
}
