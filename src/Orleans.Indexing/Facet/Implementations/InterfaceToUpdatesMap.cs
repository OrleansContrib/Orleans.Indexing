using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Orleans.Indexing.Facet
{
    internal class InterfaceToUpdatesMap: IEnumerable<KeyValuePair<Type, IReadOnlyDictionary<string, IMemberUpdate>>>
    {
        private readonly IReadOnlyDictionary<Type, IReadOnlyDictionary<string, IMemberUpdate>> updatesByInterface;
        internal IReadOnlyDictionary<Type, Guid> WorkflowIds { get; }

        internal IReadOnlyDictionary<string, IMemberUpdate> this[Type interfaceType] => this.updatesByInterface[interfaceType];

        internal IndexUpdateReason UpdateReason { get; }

        internal InterfaceToUpdatesMap(IndexUpdateReason updateReason, Func<Guid> getWorkflowIdFunc,
                                       IEnumerable<(Type interfaceType, IEnumerable<(string indexName, IMemberUpdate mu)> namedUpdates)> updateEnumerator)
        {
            this.UpdateReason = updateReason;
            this.updatesByInterface = updateEnumerator.Select(x => (itf: x.interfaceType, dict: x.namedUpdates.ToDictionary(upd => upd.indexName, upd => upd.mu)))
                                                      .Where(pair => pair.dict.Count > 0)
                                                      .ToDictionary(pair => pair.itf, pair => (IReadOnlyDictionary<string, IMemberUpdate>)pair.dict);
            this.WorkflowIds = getWorkflowIdFunc == null
                ? new Dictionary<Type, Guid>()
                : this.updatesByInterface.Keys.ToDictionary(key => key, _ => getWorkflowIdFunc());
         }

        internal bool IsEmpty => this.updatesByInterface.Count == 0;

        internal bool HasAnyDeletes => this.updatesByInterface.Values.Any(kvp => kvp.Values.Any(upd => upd.OperationType == IndexOperationType.Delete));

        public IEnumerator<KeyValuePair<Type, IReadOnlyDictionary<string, IMemberUpdate>>> GetEnumerator() => this.updatesByInterface.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();
    }
}
