using System;
using System.Collections.Generic;

namespace Orleans.Indexing
{
    [Serializable]
    internal class IndexWorkflowRecord
    {
        /// <summary>
        /// The grain being indexed, which its ID is the first part of the workflowID
        /// </summary>
        internal IIndexableGrain Grain { get; }

        /// <summary>
        /// The sequence number of update on the Grain, which is the second part of the workflowID
        /// </summary>
        internal Guid WorkflowId { get; }

        /// <summary>
        /// The list of updated values to all updated indexed properties of the Grain
        /// </summary>
        internal IReadOnlyDictionary<string, IMemberUpdate> MemberUpdates { get; }

        internal IndexWorkflowRecord(Guid workflowId, IIndexableGrain grain, IReadOnlyDictionary<string, IMemberUpdate> memberUpdates)
        {
            Grain = grain;
            WorkflowId = workflowId;
            MemberUpdates = memberUpdates;
        }

        public override bool Equals(object other)
            => other is IndexWorkflowRecord otherW ? WorkflowId.Equals(otherW.WorkflowId) : false;

        public override int GetHashCode() => WorkflowId.GetInvariantHashCode();

        public override string ToString() => string.Format("<Grain: {0}, WorkflowId: {1}>", Grain, WorkflowId);
    }
}
