using System;
using System.Collections.Generic;
using System.Linq;

namespace Orleans.Indexing
{
    /// <summary>
    /// This class is a wrapper around another IMemberUpdate which reverses its operation
    /// </summary>
    [Serializable]
    internal class MemberUpdateReverseTentative : IMemberUpdate
    {
        private IMemberUpdate _update;

        public IndexUpdateMode UpdateMode => _update.UpdateMode;

        public MemberUpdateReverseTentative(IMemberUpdate update) => this._update = update;

        public object GetBeforeImage() => this._update.GetAfterImage();

        public object GetAfterImage() => this._update.GetBeforeImage();

        public IndexOperationType OperationType
        {
            get
            {
                IndexOperationType op = this._update.OperationType;
                switch (op)
                {
                    case IndexOperationType.Delete: return IndexOperationType.Insert;
                    case IndexOperationType.Insert: return IndexOperationType.Delete;
                    default: return op;
                }
            }
        }

        public override string ToString() => MemberUpdate.ToString(this);

        /// <summary>
        /// Reverses a dictionary of updates by converting all updates to MemberUpdateReverseTentative
        /// </summary>
        /// <param name="updates">the dictionary of updates to be reverse</param>
        /// <returns>the reversed dictionary of updates</returns>
        internal static IReadOnlyDictionary<string, IMemberUpdate> Reverse(IReadOnlyDictionary<string, IMemberUpdate> updates)
            => (IReadOnlyDictionary<string, IMemberUpdate>)updates.ToDictionary(kvp => kvp.Key, kvp => new MemberUpdateReverseTentative(kvp.Value) as IMemberUpdate);
    }
}
