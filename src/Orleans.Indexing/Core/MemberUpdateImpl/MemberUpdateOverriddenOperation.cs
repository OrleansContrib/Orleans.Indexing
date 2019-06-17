using System;

namespace Orleans.Indexing
{
    /// <summary>
    /// This class is a wrapper around another IMemberUpdate, which overrides
    /// the actual operation in the original update
    /// </summary>
    [Serializable]
    internal class MemberUpdateOverriddenOperation : IMemberUpdate
    {
        private IMemberUpdate _update;

        public IndexUpdateMode UpdateMode => _update.UpdateMode;

        public MemberUpdateOverriddenOperation(IMemberUpdate update, IndexOperationType opType)
        {
            this._update = update;
            this.OperationType = opType;
        }
        public object GetBeforeImage()
            => (this.OperationType == IndexOperationType.Update || this.OperationType == IndexOperationType.Delete) ? this._update.GetBeforeImage() : null;

        public object GetAfterImage()
            => (this.OperationType == IndexOperationType.Update || this.OperationType == IndexOperationType.Insert) ? this._update.GetAfterImage() : null;

        public IndexOperationType OperationType { get; }

        public override string ToString() => MemberUpdate.ToString(this);
    }
}
