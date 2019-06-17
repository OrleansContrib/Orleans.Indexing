using Orleans.Concurrency;
using Orleans.Runtime;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Orleans.Indexing
{
    internal class IndexWorkflowQueueHandlerBase : IIndexWorkflowQueueHandler
    {
        private IIndexWorkflowQueue __workflowQueue;
        private IIndexWorkflowQueue WorkflowQueue => __workflowQueue ?? InitIndexWorkflowQueue();

        private int _queueSeqNum;
        private Type _grainInterfaceType;

        private bool _isDefinedAsFaultTolerantGrain;
        private bool _hasAnyTotalIndex;
        private bool HasAnyTotalIndex { get { EnsureGrainIndexes(); return _hasAnyTotalIndex; } }
        private bool IsFaultTolerant => _isDefinedAsFaultTolerantGrain && HasAnyTotalIndex;

        private NamedIndexMap __grainIndexes;

        private NamedIndexMap GrainIndexes => EnsureGrainIndexes();

        private SiloAddress _silo;
        private SiloIndexManager _siloIndexManager;
        private Lazy<GrainReference> _lazyParent;

        internal IndexWorkflowQueueHandlerBase(SiloIndexManager siloIndexManager, Type grainInterfaceType, int queueSeqNum, SiloAddress silo,
                                               bool isDefinedAsFaultTolerantGrain, Func<GrainReference> parentFunc)
        {
            _grainInterfaceType = grainInterfaceType;
            _queueSeqNum = queueSeqNum;
            _isDefinedAsFaultTolerantGrain = isDefinedAsFaultTolerantGrain;
            _hasAnyTotalIndex = false;
            __grainIndexes = null;
            __workflowQueue = null;
            _silo = silo;
            _siloIndexManager = siloIndexManager;
            _lazyParent = new Lazy<GrainReference>(parentFunc, true);
        }

        public async Task HandleWorkflowsUntilPunctuation(Immutable<IndexWorkflowRecordNode> workflowRecords)
        {
            try
            {
                for (var workflowNode = workflowRecords.Value; workflowNode != null; workflowNode = (await this.WorkflowQueue.GiveMoreWorkflowsOrSetAsIdle()).Value)
                {
                    var grainsToActiveWorkflows = IsFaultTolerant ? await this.FtGetActiveWorkflowSetsFromGrains(workflowNode) : emptyDictionary;
                    var updatesToIndexes = this.PopulateUpdatesToIndexes(workflowNode, grainsToActiveWorkflows);
                    await Task.WhenAll(PrepareIndexUpdateTasks(updatesToIndexes));
                    if (this.IsFaultTolerant)
                    {
                        Task.WhenAll(this.FtRemoveFromActiveWorkflowsInGrainsTasks(grainsToActiveWorkflows)).Ignore();
                    }
                }
            }
            catch (Exception e)
            {
                throw e;    // TODO empty handler; add logic or remove
            }
        }

        private IEnumerable<Task> FtRemoveFromActiveWorkflowsInGrainsTasks(Dictionary<IIndexableGrain, HashSet<Guid>> grainsToActiveWorkflows)
            => grainsToActiveWorkflows.Select(kvp => kvp.Key.RemoveFromActiveWorkflowIds(kvp.Value));

        private IEnumerable<Task<bool>> PrepareIndexUpdateTasks(Dictionary<string, IDictionary<IIndexableGrain, IList<IMemberUpdate>>> updatesToIndexes)
            => updatesToIndexes.Select(updt => (indexInfo: this.GrainIndexes[updt.Key], updatesToIndex: updt.Value))
                                .Where(pair => pair.updatesToIndex.Count > 0)
                                .Select(pair => pair.indexInfo.IndexInterface.ApplyIndexUpdateBatch(this._siloIndexManager, pair.updatesToIndex.AsImmutable(),
                                                                                pair.indexInfo.MetaData.IsUniqueIndex, pair.indexInfo.MetaData, _silo));

        private Dictionary<string, IDictionary<IIndexableGrain, IList<IMemberUpdate>>> PopulateUpdatesToIndexes(
                        IndexWorkflowRecordNode currentWorkflow, Dictionary<IIndexableGrain, HashSet<Guid>> grainsToActiveWorkflows)
        {
            var updatesToIndexes = new Dictionary<string, IDictionary<IIndexableGrain, IList<IMemberUpdate>>>();
            bool faultTolerant = IsFaultTolerant;
            for (; !currentWorkflow.IsPunctuation; currentWorkflow = currentWorkflow.Next)
            {
                IndexWorkflowRecord workflowRec = currentWorkflow.WorkflowRecord;
                IIndexableGrain g = workflowRec.Grain;
                bool existsInActiveWorkflows = faultTolerant && grainsToActiveWorkflows.TryGetValue(g, out HashSet<Guid> activeWorkflowRecs)
                                                             && activeWorkflowRecs.Contains(workflowRec.WorkflowId);

                foreach (var (indexName, updt) in currentWorkflow.WorkflowRecord.MemberUpdates.Where(kvp => kvp.Value.OperationType != IndexOperationType.None))
                {
                    var updatesByGrain = updatesToIndexes.GetOrAdd(indexName, () => new Dictionary<IIndexableGrain, IList<IMemberUpdate>>());
                    var updatesForGrain = updatesByGrain.GetOrAdd(g, () => new List<IMemberUpdate>());

                    if (!faultTolerant || existsInActiveWorkflows)
                    {
                        updatesForGrain.Add(updt);
                    }
                    else if (GrainIndexes[indexName].MetaData.IsUniqueIndex)
                    {
                        // If the workflow record does not exist in the set of active workflows and the index is fault-tolerant,
                        // enqueue a reversal (undo) to any possible remaining tentative updates to unique indexes.
                        updatesForGrain.Add(new MemberUpdateReverseTentative(updt));
                    }
                }
            }
            return updatesToIndexes;
        }

        private static HashSet<Guid> emptyHashset = new HashSet<Guid>();
        private static Dictionary<IIndexableGrain, HashSet<Guid>> emptyDictionary = new Dictionary<IIndexableGrain, HashSet<Guid>>();

        private async Task<Dictionary<IIndexableGrain, HashSet<Guid>>> FtGetActiveWorkflowSetsFromGrains(IndexWorkflowRecordNode currentWorkflow)
        {
            var activeWorkflowSetTasksByGrain = new Dictionary<IIndexableGrain, Task<Immutable<HashSet<Guid>>>>();
            var currentWorkflowIds = new HashSet<Guid>();

            for (; !currentWorkflow.IsPunctuation; currentWorkflow = currentWorkflow.Next)
            {
                var record = currentWorkflow.WorkflowRecord;
                currentWorkflowIds.Add(record.WorkflowId);
                IIndexableGrain g = record.Grain;
                if (!activeWorkflowSetTasksByGrain.ContainsKey(g) && record.MemberUpdates.Where(ups => ups.Value.OperationType != IndexOperationType.None).Any())
                {
                    activeWorkflowSetTasksByGrain[g] = g.AsReference<IIndexableGrain>(this._siloIndexManager, this._grainInterfaceType).GetActiveWorkflowIdsSet();
                }
            }

            if (activeWorkflowSetTasksByGrain.Count > 0)
            {
                await Task.WhenAll(activeWorkflowSetTasksByGrain.Values);

                // Intersect so we do not include workflowIds that are not in our work queue.
                return activeWorkflowSetTasksByGrain.ToDictionary(kvp => kvp.Key,
                                                                  kvp => new HashSet<Guid>(kvp.Value.Result.Value.Intersect(currentWorkflowIds)));
            }

            return new Dictionary<IIndexableGrain, HashSet<Guid>>();
        }

        private NamedIndexMap EnsureGrainIndexes()
        {
            if (__grainIndexes == null)
            {
                __grainIndexes = _siloIndexManager.IndexFactory.GetGrainIndexes(_grainInterfaceType);
                _hasAnyTotalIndex = __grainIndexes.HasAnyTotalIndex;
            }
            return __grainIndexes;
        }

        // TODO clean up some of the duplicated id-generation code.
        private IIndexWorkflowQueue InitIndexWorkflowQueue()
            => __workflowQueue = _lazyParent.Value.IsGrainService
                    ? _siloIndexManager.GetGrainService<IIndexWorkflowQueue>(IndexWorkflowQueueBase.CreateIndexWorkflowQueueGrainReference(_siloIndexManager, _grainInterfaceType, _queueSeqNum, _silo))
                    : _siloIndexManager.GrainFactory.GetGrain<IIndexWorkflowQueue>(IndexWorkflowQueueBase.CreateIndexWorkflowQueuePrimaryKey(_grainInterfaceType, _queueSeqNum));

        public static GrainReference CreateIndexWorkflowQueueHandlerGrainReference(SiloIndexManager siloIndexManager, Type grainInterfaceType, int queueSeqNum, SiloAddress siloAddress)
            => siloIndexManager.MakeGrainServiceGrainReference(IndexingConstants.INDEX_WORKFLOW_QUEUE_HANDLER_GRAIN_SERVICE_TYPE_CODE,
                                                               IndexWorkflowQueueBase.CreateIndexWorkflowQueuePrimaryKey(grainInterfaceType, queueSeqNum), siloAddress);

        public Task Initialize(IIndexWorkflowQueue oldParentGrainService)
            => throw new NotSupportedException();
    }
}
