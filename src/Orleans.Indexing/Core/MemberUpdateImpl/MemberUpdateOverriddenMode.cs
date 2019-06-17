using System;

namespace Orleans.Indexing
{
    /// <summary>
    /// This class is a wrapper around another IMemberUpdate which adds an update mode.
    /// </summary>
    [Serializable]
    internal class MemberUpdateOverriddenMode : IMemberUpdate
    {
        private IMemberUpdate _update;

        public IndexUpdateMode UpdateMode { get; }

        public MemberUpdateOverriddenMode(IMemberUpdate update, IndexUpdateMode indexUpdateMode)
        {
            this._update = update;
            this.UpdateMode = indexUpdateMode;
        }

        public object GetBeforeImage() => this._update.GetBeforeImage();

        public object GetAfterImage() => this._update.GetAfterImage();

        public IndexOperationType OperationType => this._update.OperationType;

        public override string ToString() => MemberUpdate.ToString(this);
    }
}
