# Indexing Internals

<!-- markdown-toc (by Jon Schlinkert) inserts the Table of Contents between the toc/tocstop comments; commandline is: markdown-toc -i <this file> -->
<!-- markdown-toc truncates `IInterface<TProperties>` before the <TProperties>, so manually enter the anchor on the heading using the name markdown-toc generates (markdown-toc does not pick up names in anchor definitions on section headers; you must use the names it generates). Then after each TOC creation, re-add <TProperties> or <TGrainState> before the closing backtick.
The same "manually enter the anchor" requirement applies to headers with a period, such as "Active vs. Total Index...". -->

<!-- toc -->

- [Overview of Components](#overview-of-components)
  * [Indexing Facet Implementation](#indexing-facet-implementation)
  * [`*IndexedState` Hooks Into The Grain's Lifecycle](#indexedstate-hooks-into-the-grains-lifecycle)
  * [`*IndexedState` State Management](#indexedstate-state-management)
      - [Wrapping the `TGrainState`](#wrapping-the-tgrainstate)
  * [Hash Table Implementation](#hash-table-implementation)
    + [Partitioning Schemes](#partitioning-schemes)
  * [IIndexInterface](#iindexinterface)
    + [IHashIndexInterface](#ihashindexinterface)
  * [Workflow Queues](#workflow-queues)
  * [GrainServices Created by Indexing](#grainservices-created-by-indexing)
    + [PerSiloIndexGrainServiceClassAttribute](#persiloindexgrainserviceclassattribute)
  * [Consistency Schemes, Queues, and Eagerness](#consistency-schemes-queues-and-eagerness)
  * [Limitations On Active Indexes](#limitations-on-active-indexes)
  * [Limitations On Total Indexes](#limitations-on-total-indexes)
  * [Code Elements of an Index](#code-elements-of-an-index)
    + [IndexInfo](#indexinfo)
    + [IndexRegistry and GrainIndexes](#indexregistry-and-grainindexes)
    + [IndexManager and SiloIndexManager](#indexmanager-and-siloindexmanager)
- [Flow of Index Updates](#flow-of-index-updates)
  * [MemberUpdates](#memberupdates)
  * [Interception of ApplyIndexUpdate](#interception-of-applyindexupdate)
  * [Workflow Updates](#workflow-updates)
    + [Non-Fault-Tolerant Updates](#non-fault-tolerant-updates)
    + [Fault-Tolerant Updates](#fault-tolerant-updates)
    + [Transactional Updates](#transactional-updates)
      - [TransactionalIndexVariant](#transactionalindexvariant)
      - [Limits of Transactional Updates](#limits-of-transactional-updates)
    + [Testing of Internals via IInjectableCode](#testing-of-internals-via-iinjectablecode)
- [Flow of Index Queries](#flow-of-index-queries)
  * [QueryActiveGrainsNode](#queryactivegrainsnode)
  * [OrleansQueryProvider](#orleansqueryprovider)
  * [QueryIndexedGrainsNode](#queryindexedgrainsnode)
  * [ITransactionalLookupIndex](#itransactionallookupindex)
- [Issues And Potential Work Items](#issues-and-potential-work-items)
  * [TODOs in the Code](#todos-in-the-code)
  * [Performance](#performance)
  * [`IOrleansIndexingStorageProvider`](#iorleansindexingstorageprovider)
  * [Multiple `IIndexedState` Facets per Grain](#multiple-iindexedstate-facets-per-grain)
  * [Testing](#testing)

<!-- tocstop -->

## Overview of Components
This document describes the major internal components and flow of the Indexing system. A research paper describing the interface and implementation can be found [here](http://cidrdb.org/cidr2017/papers/p29-bernstein-cidr17.pdf), and a PDF with additional illustrations is [here](http://cidrdb.org/cidr2017/slides/p29-bernstein-cidr17-slides.pdf).

Before reading this document, you should be familiar with the [Indexing User's Guide](/src/Orleans.Indexing/IndexingFacet.md), because a number of terms (such as Consistency Scheme) will be used here that will reference elements described in that document.

The following subsections provide a 10,000-foot view of the components of indexing and their interaction. Subsequent sections describe these components and interactions in detail. This document is not intended to be exhaustive; many elements will require study of the code before all aspects are totally clear.

### Indexing Facet Implementation
As described in the [Indexing User's Guide](/src/Orleans.Indexing/IndexingFacet.md), the facet is the user's first point of contact with the Indexing system. It consists of an attributed argument on one (or more) of a grain's constructors; the attribute used determines which Consistency Scheme to use, which in turn determines which implementation of `IIndexedState` is created; this implementation then drives all indexing operations.

The base of the `IIndexedState` Facet implementation is `IndexedStateBase`, which manages initializations of the facet as well as launching the virtualized index-update process. 

Workflow indexes build on this with the following hierarchy:
- `WorkflowIndexedStateBase`, which provides common operations for Workflow indexes.
- `NonFaultTolerantWorkflowIndexedState`, which in addition to implementing non-fault-tolerant workflow, is also the base for `FaultTolerantWorkflowIndexedGrainState`, which calls down to its `ApplyIndexUpdates` method if there are no index updates or there are no Total indexes (the latter is a remnant from when Active indexes were allowed to be fault-tolerant).
- `FaultTolerantWorkflowIndexedGrainState`, which implements fault-tolerant indexing.

### `*IndexedState` Hooks Into The Grain's Lifecycle
Rather than requiring the grain to delegate `OnActivateAsync` and `OnDeactivateAsync` calls to the `*IndexedState`, `*IndexedState` hooks itself into the grain's `IGrainLifecycle`. This provides the context for calls to initialize the `*IndexedState`'s state and manage index updates on Active grain activation/deactivation.

### `*IndexedState` State Management
`IIndexedState<TGrainState>` presents a single storage-access interface to the grain. This interface is the same as the `TransactionalState` interface, to enable consistent coding patterns regardless of Consistency Scheme. The `IIndexedState` internally acquires an instance of the `TGrainState`, which is visible to the grain only via the lambdas passed to the `PerformRead` and `PerformUpdate` methods.

For Workflow indexes, the `IIndexedState` implementation creates a contained `NonTransactionalState` instance during the state setup phase of the grain lifecycle. This class has the same interface as `ITransactionalState`. It creates a contained `StateStorageBridge` whose state is visible, and the lambdas passed to `PerformRead` and `PerformUpdate` are given a direct reference to this state. In turn, the lambda may return a direct reference to a property of that state, or the state itself. The `where TGrainState : new()` constraint on the `TGrainState` is a requirement of the `StateStorageBridge`.

For Transactional indexes, the state is obtained during Facet resolution, by calling `ITransactionalStateFactory.Create` to obtain an `ITransactionalState` implementation, which is passed to the `TransactionalIndexedState` constructor. The state managed by the `TransactionalState` of `ITransactionalState` holds the state in its own space and communicates to the lambda via a deep copy of the state, and also deep-copies any return value from the lambda. Because the Transactional context is not available during grain activation, `ITransactionalState.PerformRead` cannot be called; therefore, state initialization is deferred to the first call to `PerformRead` or `PerformUpdate`.

`IndexedStateAttribute` takes parameters identifying the state name and the storage provider. Currently, state name is not necessary; unlike Transactions, there is only one `IIndexedState` facet per constructor. If Indexing implements support for multiple `IIndexedState`s in the future, then this name would be needed. The storage provider name replaces the [StorageProvider] attribute from earlier versions of Indexing. However, [StorageProvider] remains for internal Indexing grains and grain services, as these do not take a storage facet.

##### Wrapping the `TGrainState`
Internally, Indexing stores some information along with the grain state. For non-fault-tolerant indexing, this is done by wrapping `TGrainState` with `IndexableGrainStateWrapper`, whose additional state simply indicates whether the state object has had its non-reference properties initialized to their specified NullValues. If this has not been done, it means the grain was not read from persistent storage, so the `IIndexedState` implementation will assign the NullValues specified for all `TProperties` used by indexable interfaces of that Grain. This cannot be done in the parameterless constructor (used by ActivatorUtilities.CreateInstance) because it needs the propertyNullValues parameter.
Fault-tolerant indexing defines `FaultTolerantIndexableGrainStateWrapper`, a subclass of `IndexableGrainStateWrapper` that also stores the in-flight workflow IDs for that Grain.
These wrappers are not visible to the `IIndexedState` consumer.

### Hash Table Implementation
Currently Orleans supports only hash-based indexing, which in turn supports retrieval only by property equivalence with a search value. The actual implementation of the "hash table" depends on the partitioning scheme.

All partitioning schemes utilize the concept of a "bucket grain", in which the hash-table bucket is implemented by a grain and contains a `HashIndexBucketState`, which is essentially a Dictionary that maps a key (the property value) to one or more grains that have that property value.

#### Partitioning Schemes
The details of the partitioning schemes are:
- SingleBucket: The entire index is in a single grain. The Dictionary of this grain is therefore the spine for the entire index; it has an entry for each value of that property on any grain. Obviously, this is the least scalable approach and, with Transactions, may timeout due to lock contention. Implemented by `HashIndexSingleBucket`. Used only for Total indexes.
- Per Silo: Similar to SingleBucket, except that there is a single bucket on each silo. The bucket is implemented by a grain service, `ActiveHashIndexPartitionedPerSiloBucketImplGrainService`, which maintains its own `HashIndexBucketState` (it does not use `HashIndexSingleBucket`).
- Per Key: Each value of the indexed property maps to a different bucket grain, which may be on any silo. The "spine" of the index is thus distributed across these bucket grains. Each bucket grain is an instance of `HashIndexSingleBucket`, and its `HashIndexBucketState` will contain only the keys that hash to its hash value. Usually there is only one, but there may be collisions, mostly for strings or when `IndexingOptions.MaxHashBuckets` is set low enough to cause collisions. Because Indexing uses the default integer hash code, which is just the integer itself, and `IndexingOptions.MaxHashBuckets` is implemented by simple modulo, some property-value generation schemes can cause high collision rates.

Because of the difference in `HashIndexBucketState` storage by each of these index options, updating `HashIndexBucketState` is done by a static function `HashIndexBucketUtils.UpdateBucketState`. This does not have any concurrency control (locking) because it is only called from methods on bucket grains, and Orleans ensures that no other thread can run concurrently in a grain before we reach an await operation, when execution is yielded back to the Orleans scheduler. `HashIndexBucketUtils.UpdateBucketState` is synchronous, so no await operation is encountered until it has returned.

### IIndexInterface
This is the base interface for both index implementation classes and hash bucket implementation classes; essentially, the index implementation calls through to the bucket implementation.

#### IHashIndexInterface
This adds a single method to IIndexInterface: `LookupUniqueAsync`, which is currently unused. Currently, uniqueness is defined at the index level, and thus this method appears unnecessary. Implementing it would require adding an additional query method, perhaps `IOrleansQueryable<TIGrain, TProperties>.GetUniqueResult`, which would be implemented in `QueryIndexedGrainsNode<TIGrain, TProperties>` to call `IHashIndexInterface.LookupUniqueAsync` (and there should be a transactional form as there is for `IIndexInterface` methods).

### Workflow Queues
Indexing implements a workflow queue system that allows an index update to be lazy; the request is enqueued and then the original operation continues (by enqueueing more updates, or by returning to the caller).

Lazy updates minimize the number of RPCs by enqueueing index updates into a queue on the silo where the grain is active. The queue is implemented by `IndexWorkflowQueueGrainService` (a grain service is a grain that belongs to a specific silo) or a `ReincarnatedIndexWorkflowQueue` grain (which is created by fault-tolerant indexing when a queue is not reachable, and is an ordinary grain). 

These queue implementations contain an instance of `IndexWorkflowQueueBase` which provides the actual queue functionality (adding and removing items) as well as containing in implementation of an `IIndexWorkflowQueueHandler`, which in turn contains an `IndexWorkflowQueueHandlerBase` which does the work.

The queue handler is similar to the queue in terms of location; it is either an `IndexWorkflowQueueHandlerGrainService` on the same silo as the `IndexWorkflowQueueGrainService`, or a `ReincarnatedIndexWorkflowQueueHandler` grain (which may or may not be on the same silo as the `ReincarnatedIndexWorkflowQueue` grain).

Thus, the queue or queue handler is a GrainService or grain "wrapper" around a queue base or queue handler base, which does the actual work, and the queue's background operations are driven by the handler.

The queue handler is controlled by the `IndexWorkflowQueueBase`; when items have been added to the queue, the `IndexWorkflowQueueBase` executes `Handler.HandleWorkflowsUntilPunctuation`. "Punctuation" is simply a delineation between groups of workflow records. The handler obtains a reference to the queue (the code here could be refactored to make some things clearer and reduce copied grain-id generation). Once the handler has completed the current group of records and arrives at a punctuation, it calls the queue's `GiveMoreWorkflowsOrSetAsIdle` method. This in turn calls the queue base's `GiveMoreWorkflowsOrSetAsIdle`. If there are more records to be processed, they are returned; otherwise, the queue becomes idle.

### GrainServices Created by Indexing
A GrainService is a grain that runs on a specific silo. Indexing uses these to keep updates on the same silo as the active grain whenever possible, to minimize RPCs.

There are three types of GrainService used by Indexing:
- `IndexWorkflowQueueGrainService`, as described in [Workflow Queues](#workflow-queues).
- `IndexWorkflowQueueHandlerGrainService`, as described in [Workflow Queues](#workflow-queues).
- `ActiveHashIndexPartitionedPerSiloBucketImplGrainService`, as described in [Partitioning Schemes](#partitioning-schemes)

Orleans requires that GrainServices be registered during Configuration time; they are created during Silo construction, and there is no method to add a GrainService to a Silo once the silo has been built. Therefore, `ApplicationPartsIndexableGrainLoader` employs a two-step approach to creating indexes:
- Register all GrainServices during registration time, via `RegisterGrainServices`. This enumerates grains in all added `ApplicationParts.OfType<AssemblyPart>`, determining which indexes create Queues and Per-Silo indexes, and creates the GrainServices for them. This step must be done during Configuration time, to add the GrainServices to the `IServiceCollection`. Thus, it cannot use information from the `IServiceProvider`; this hasn't been created yet. This requires a bit of special treatment for `IndexingOptions.NumWorkflowQueuesPerInterface`; this is required in order to create the necessary queues (and thus their GrainServices), so the Configuration action (if any) is applied directly in `SiloHostBuilder.UseIndexing` and then the populated `IndexOptions` is passed to `RegisterGrainServices`, where the `NumWorkflowQueuesPerInterface` is extracted for the queue-creation loop.
- Create indexes. This uses the `IServiceProvider` that is created during Silo construction. Because Indexing relies on the GrainServices, the `SiloIndexManager` and `IndexManager` must run after `ServiceLifecycleStage.RuntimeGrainServices`; they run at the next stage, in `ServiceLifecycleStage.ApplicationServices`.

#### PerSiloIndexGrainServiceClassAttribute
Classes that implement an Index that is partitioned Per-Silo are GrainServices and must expose a static `RegisterGrainService` method that is called during `ApplicationPartsIndexableGrainLoader.RegisterGrainServices`. Because only the `interfaceType` is known to `ApplicationPartsIndexableGrainLoader.RegisterGrainServices`, it must carry a `PerSiloIndexGrainServiceClassAttribute` that has one property, `GrainServiceClassType`, which is the `Type` of the class that implements the `GrainService` for that index.

### Consistency Schemes, Queues, and Eagerness
The Transactional consistency scheme does not make use of the Workflow queues.

Non-fault-tolerant indexes may be either Lazy or Eager. If they are Eager, then they do not use the queues; they await the update of the index before control returns to the caller.

Fault-tolerant indexes must be Lazy, because the queues are in integral part of the fault tolerance. Fault-tolerant indexes provide "eventual consistency"; they store the IDs of in-flight workflows along with the grain's state. When the queue executes an index update, it first obtains the set of active workflow Ids from the grain (which in turn retrieves it from the `IIndexedState` implementation), and executes the update if its workflow ID is in that set.

### Limitations On Active Indexes
Active Indexes cannot be Workflow Fault-Tolerant because upon processing deactivation, calling `GetActiveWorkflowIds` causes the grain to be spuriously reactivated. Thus a grain would never be deactivated. However, without fault tolerance, stale entries are possible:
- Assume an Active index is partitioned per key
- Assume the hash bucket grain for one or more of the grain's Active index entries is on a different silo from the one the grain is activated on
- If the silo the grain is hosted on crashes, then we will have stale entries in all Active index hash buckets for that grain on other silos.  

Active Indexes also cannot be Transactional because updating multiple indexes on activation or deactivation would have to be done in a transactional context, and there is no such context during the activation/deactivation phases of the grain lifecycle.

These issues would only have to be solved for Per-Key or SingleBucket partitioning. For Per-Silo partitioning, the silo itself is the single unit of failure for both the grains and the indexes resident on it: If the silo goes down, so do all active grains on it as well as the active indexes that point to those active grains (and thus Per-Silo partitioned indexes do not write their state to storage, because there is no need). If a grain is reactivated on another silo during normal Orleans operation, then the active index partitioned to that silo will be updated for that grain as usual.

Therefore, Active Indexes can be partitioned Per-Silo only.

### Limitations On Total Indexes
In contrast to Active Indexes, Total Indexes *cannot* be partitioned Per-Silo. This is because attempting to process index updates on the same silo as the current grain activation would have to fan out to ensure that no previous activation had taken place on another silo or, if it had, that the index entries are moved to the current activation's silo. There appears to be no gain in doing so, since Total Indexes by their nature are cluster-wide. Therefore, Total Indexes support only Per-Key and SingleBucket partitioning.

### Code Elements of an Index
This section describes what constitutes an "index definition" in the code, as well as where these live in Indexing space.
#### IndexInfo
`IndexFactory.CreateIndex` creates an instance of `IndexInfo`. This contains: 
- `IIndexInterface`: This is the `IIndexInterface` of the class instance that implements the index (e.g. `TotalHashIndexPartitionedPerKey`).
- `IndexMetaData`: The definition of the index--its name, uniqueness and eagerness, and whether the number of entries per bucket is limited. Note that the number of entries refers to the number of keys; there is no limit on the number of grain references stored for a key in a single bucket (keys are not split across buckets).
- `IIndexUpdateGenerator`: This generates the `IMemberUpdates` for an index from the property on the `TProperties` instance. Currently this is always an instance of `IndexUpdateGenerator`.

#### IndexRegistry and GrainIndexes
The global `IndexRegistry` lives in the `IndexManager` instance. It is populated at silo startup through `ApplicationPartsIndexableGrainLoader`. It allows retrieval of indexes by either indexed-interface type or by Grain type.

An instance of `GrainIndexes` is created for each `IIndexedState` implementation instance. It obtains the grain's indexes from the `IndexRegistry` and manages the ephemeral `TProperties` instances that are created for index updates.

#### IndexManager and SiloIndexManager
The `IndexManager` serves as the internal central point for numerous components:
- The `IndexRegistry` which contains all the index definitions
- Components obtained via Dependency Injection, which avoids having to duplicate these on constructors throughout indexing. 
- `IndexingOptions`
- Index loading via `ApplicationPartsIndexableGrainLoader`, which is triggered by the `IndexManager`'s enrolling in the cluster's `ServiceLifecycleStage.RuntimeGrainServices` startup.

The `SiloIndexManager` provides similar functionality at the silo level:
- Silo-level Dependency-Injected components
- Obtaining the list of Silos in the cluster (necessary for cross-silo queries for PerSilo partitioned indexes)
- Obtaining a `StateStorageBridge` for the `IIndexedState` implementations
- A reference to the silo itself. The silo is retrieved from the ServiceProvider and must not be retrieved until *after* the SiloIndexManager's constructor returns; or more precisely, until after the Silo constructor has returned to the ServiceProvider which then sets the Singleton. If `ServiceProvider.GetRequiredService<Silo>()` is called during the Silo constructor, the Singleton is not found so another Silo is constructed. Thus we cannot have the Silo on the `IndexManager` ctor params or retrieve it during the `IndexManager` ctor, because `ISiloLifecycle` participants are constructed during the Silo ctor.

The `SiloIndexManager` is not obtained via constructor injection as other DI components are, because for Unit Testing, Indexing can be loaded into the client. Thus the `SiloIndexManager` is obtained lazily via `IndexManager.GetSiloIndexManager`.

## Flow of Index Updates 
Index updates start with `IndexedStateBase.UpdateIndexes`, which is called from `PerformUpdate` in one of the subclasses for both Transactional and Workflow indexes, or from `OnActivateAsync` or `OnDeactivateAsync` in one of the Workflow indexes.

This calls `IndexedStateBase.GenerateMemberUpdates` to compare the current property values to the stored property values and, from that, to generate the `InterfaceUpdatesMap` which is a map from an interface type to the set of [MemberUpdates](#memberupdates) for the properties indexed on that interface type.

The common base code ends here, with a call to the abstract `ApplyIndexUpdates` method. This is overridden by the specific `IIndexedState` implementation.

### MemberUpdates
`MemberUpdates` contain images for the old value, the new value, and the `IndexOperationType`: Insert, Update, or Delete.

`MemberUpdate`s of type Update are split up before being sent to `HashIndexBucketUtils` for Per-Key indexes. This is because for Per-Key partitioning, the old value and the new value will be in separate buckets; both Per-Silo and SingleBucket indexes have the entire index in the same bucket from the point of view of the indexed grain (with Per-Silo, there is one bucket per index per silo, and an index update will always apply to the silo the grain resides on).

### Interception of ApplyIndexUpdate
`IndexExtensions` is a static class that supplies methods to intercept calls to `ApplyIndexUpdate` (or `ApplyIndexUpdateBatch`). The purpose is to intercept calls to `DirectApplyIndexUpdate` for Per-Silo indexes. A PerSilo index lives in its own grain service; `ApplyIndexUpdate` obtains that grain service on the silo of the indexed grain.

For other index types, `ApplyIndexUpdate(Batch)` simply calls through to `index.DirectApplyIndexUpdate(Batch)`.

### Workflow Updates
Workflow indexes use no locking, and may be fault-tolerant or non-fault-tolerant.

#### Non-Fault-Tolerant Updates
Non-fault-tolerant indexes may be eager or lazy, Active or Total.

These are the simplest of the indexing options. If there are no index updates, then this simply writes the persistent grain state and returns.

Otherwise, if there is more than one unique index update, then all unique index updates are enqueued tentatively. This "saves the slot" and ensures that all uniqueness constraints are satisfied before non-tentative index updates are issued. If there is a uniqueness violation, then the tentative updates are undone and the index update is abandoned.

Finally, non-tentative updates are performed:
- If the index is eager, then the eager updates are executed in parallel with writing the grain state.
- Otherwise, the index updates are enqueued and then the grain state is written.

#### Fault-Tolerant Updates
Fault-tolerant indexes are always lazy and Total. They use a combination of queues and persistent storage of in-flight workflows to restart in case of failure.

If there are no index updates or there are no total indexes in the grain, then this simply calls `NonFaultTolerantWorkflowIndexedState.ApplyIndexUpdates` (`FaultTolerantWorkflowIndexedState` inherits from `NonFaultTolerantWorkflowIndexedState`), which as described above simply writes the state update and returns.

The first step in the fault-tolerant workflow is to enqueue lazy non-tentative updates to the indexes. This persists the current workflow into the queue, and also ensures that later, non-tentative unique-index updates will "commit" the tentative unique updates described next.

Then tentative updates to unique indexes are enqueued, to "reserve" the unique slots and ensure no duplication. If this does not return an error, then we add the workflow IDs for the lazy updates to the grain's state, persist that state, and exit the IIndexedState's PerformUpdate() method. This ensures that the grain state is persisted before the indexes are updated.

There are a couple subtle aspects to this:
- The IIndexedState.PerformUpdate() method is called from a method on one or more of the grain's interfaces, which should not allow Interleaving. The internal fault-tolerant indexing infrastructure is driven by `IndexWorkflowQueueHandlerBase` which calls back to the grain to obtain the set of in-flight workflow IDs. This call goes through the non-interleaved `IIndexableGrain.GetActiveWorkflowIdsSet()` grain interface method. The Orleans messaging system will not allow the `GetActiveWorkflowIdsSet()` call to be made before the method calling `IIndexWriter.PerformUpdate()` completes. Thus, the fault-tolerant indexing system cannot retrieve the set of in-flight workflow IDs from a grain before it has correctly persisted its in-flight workflow set.
- If there is a unique index violation, then the grain will eagerly remove the tentative updates. If the grain crashes between the time the lazy updates are enqueued and the time this removal is done, then when `IndexWorkflowQueueHandlerBase` tries to obtain the workflow IDs from the grain, it will not find them (because the grain state was not persisted with them), so the updates are discarded if they are not unique; for unique indexes, a `MemberUpdateReverseTentative` is enqueued to remove the tentative entry from the index.
- Similarly, if the tentative unique updates pass but the grain crashes before its state is persisted, or crashes or throws an exception during the storage provider's WriteStateAsync(), then again the fault-tolerant infrastructure will not find the tentative workflow IDs in the grain's list of in-flight IDs, so they will be discarded.

#### Transactional Updates
Transactional indexes are always eager and Total. They are very similar to non-fault-tolerant eager indexes; they do not engage with the queue system, and go through the eager route directly to the hash buckets.

##### TransactionalIndexVariant
There is an indirect relationship between the specification of an index and the consistency scheme to be used for it: the index is specified as property annotations on the `TProperties` class, while the consistency scheme is specified on the grain constructor's indexing facet argument. In fact, the exact same index specification may be used for any consistency scheme, as long as the index definition is legal in all those schemes: Total, Eager, and PerKey or SingleBucket partitioning.

Thus, the internal specifications of Transaction-compatible indexes (currently, Total indexes either PerKey or SingleBucket partitioned, or DSMI) are annotated with the `TransactionalIndexVariantAttribute`, which identifies the variant of the index implementation class that has `TransactionAttribute`-annotated methods. The `TransactionAttribute` annotation is required for Transactions to have a chain of such annotated methods (a grain call that is not so annotated does not participate in a transaction, and thus `TransactionalState.PerformRead` and `TransactionalState.PerformUpdate` would fail).

For a Transactional index, `ApplicationPartsIndexableGrainLoader` reads the `TransactionIndexVariant` attribute and, if found, creates an instance of the transactional variant index instead of the non-transactional one.

##### Limits of Transactional Updates
Because `TransactionState.PerformRead` and `.PerformUpdate` lock resources, there are some situations where either deadlocks or timeouts can result.

In particular, `TransactionalIndexedState` cannot issue `Task.WhenAll` updates to multiple indexes as `*WorkflowIndexedState` can. This is because there is no ordering of accesses to the indexes in this case, and thus deadlock can occur. Instead, `TransactionalIndexedState` ensures a canonical ordering of index accesses by interface name and then by index name; deadlock is avoided because all accesses are done in the same order.

The impact of locking can be somewhat mitigated by partitioning the index Per-Key. In this case, each value maps to a different bucket (the "spine" of the index's hash table is distributed across multiple buckets, located on whatever silo they are activated on). This makes lock contention unlikely for different values of the indexed property; this only happens if there are collisions. For strings and well-distributed integers, there are usually few collisions unless the `MaxHashBuckets` configuration option is set. If it is, then some generation schemes for integer property values may yield numerous collisions due to modulo of the property value with `MaxHashBuckets`. For a worst-case example, assume `baseValue == <some multiple of MaxHashBuckets>` and generate values via `(baseValue * x) + y`; the modulo with `MaxHashBuckets` will leave collisions on the `y` values.

In the case of SingleBucket partitioning, or of Per-Key partitioning with high collision rates, it is possible to encounter Transaction timeouts as the load increases. 

`DirectStorageManagedIndex`es (DSMI) are currently supported only for the Orleans.CosmosDB storage provider, which does not yet implement the required interfaces to participate in Orleans Transactions. Because of this, DSMI causes the `TransactionalStateStorageProviderWrapper` to be used. This puts an additional "CommittedState." level into the property path that is indexed by the provider. The DSMI code should be revisited when Orleans.CosmosDB supports transactional interfaces; in this case, the `TransactionalStateStorageProviderWrapper` may still be used by non-supporting providers.

In addition to some Transactional Indexing tests sprinkled through the rest of the Unit Tests, there are two sets of tests targeted specifically for Transactions:
- `*TransactionalPlayer*`: This uses the *Player* properties definitions to test commit and rollback of index inserts and updates.
-  The Indexing benchmarks in the Benchmarks project test how many index operations can be done per second. These benchmarks provide all 3 consistency schemes and select which one to use based on the command-line arguments.

#### Testing of Internals via IInjectableCode
Currently two tests, both for fault-tolerant correctness, use `IInjectableCode` implementations to cause the indexing infrastructure to operate different for these tests. `IInjectableCode` operates by:
- At Silo configuration time, `IndexingRecoveryTestFixture` sets a `TestInjectableCode` configuration object with the requested behavior, for example:
```c#
    hostBuilder.ConfigureServices(services => services.AddSingleton<IInjectableCode>
                                (_ => new TestInjectableCode { SkipQueueThread = true }));

```
- The `TestInjectableCode` implementation has functions that take lambdas which are the small sections of production code to exercise. It uses the value set at configuration time (here, `SkipQueueThread`) to determine whether to force test-desired behavior or simply execute the production code. For example:
```c#
    public bool ShouldRunQueueThread(Func<bool> pred)
    {
        if (this.SkipQueueThread)
        {
            this.SkipQueueThread = false;       // Only do this once.
            return false;
        }
        return pred();
    }
```
- If there is no service implementation registered for `IInjectableCode`, the `SiloIndexManager` creates an instance of `ProductionInjectableCode`. The control values (such as `SkipQueueThread`) are never set for this class, and the lambda-accepting functions merely execute the lambda. In this way, no test code can leak into production.

## Flow of Index Queries
Querying indexed properties is done by LINQ:
```c#
    var indexFactory = (IIndexFactory)client.ServiceProvider.GetService(typeof(IIndexFactory));

    var query = from team in indexFactory.GetActiveGrains<ISportsTeamGrain, SportsTeamIndexedProperties>()
                 where team.Name == "Seahawks"
                 select team;

    await query.ObserveResults(new QueryResultStreamObserver<ISportsTeamGrain>(async team =>
    {
        Console.WriteLine($"\n\n{await team.GetName()} location = {await team.GetLocation()}, pk = {team.GetPrimaryKeyLong()}");
    }));
```

`IndexingTestUtils` in the Indexing Unit Tests provides a dense example of issuing queries and observing the results or fetching the entire result set. A simpler example is in the [SportsTeamIndexing](IndexingFacet.md#sportsteamindexing-sample) sample.

### QueryActiveGrainsNode
`indexFactory.GetActiveGrains()` returns an `IOrleansQueryable<TIGrain, TProperty>`. In this case, that is an instance of `QueryActiveGrainsNode`; this is essentially a placeholder whose purpose is to provide a target for the application of the WHERE clause, which is how the index is accessed/used by `OrleansQueryProvider`. Because of this, `QueryActiveGrainsNode` throws an exception if `GetResults` or `ObserveResults` is called.

### OrleansQueryProvider
`System.Linq.IQueryable.Where()` (called from `OrleansQueryableExtensions.Where`) obtains the `OrleansQueryProvider` from the `QueryGrainsNode<,>.Provider` property of the `QueryActiveGrainsNode`, and calls `OrleansQueryProvider.CreateQuery<TProperties>` (which is an implementation of `System.Linq.IQueryProvider.CreateQuery<TElement>`), passing the LINQ `Expression`. From here, `OrleansQueryProvider` obtains the WHERE clause information from the `Expression` and uses that to obtain the indexed property name. 

### QueryIndexedGrainsNode
From the private `OrleansQueryProvider.CreateQuery` implementation, an instance of `QueryIndexedGrainsNode` is created. Note that rather than the generic type arguments `TIGrain` and `TProperties`, `OrleansQueryProvider.CreateQuery()` uses the generic arguments from the `Expression` to obtain the `grainInterfaceType` and `iPropertiesType` to pass to the `QueryIndexedGrainsNode` activation.

`QueryIndexedGrainsNode.GetResults` obtains the entire result set, while `.ObserveResults` populates a stream for returning results. Currently all results are obtained either way; there is a TODO to make this more efficient in the streaming case. From these, it is fairly straightforward to trace through the `LookupAsync` or `LookupTransactionalAsync` calls on the index and see how the result stream or result set is instantiated and populated.

Currently `OrleansQueryProvider` supports only a single clause, which must be Where. OrderBy is not supported. If Range Indexes are ever supported, this will have to be modified.

### ITransactionalLookupIndex
For queries on Indexes, the Indexing infrastructure creates on-the-fly transactions if none exists, rather than requiring the client to create a transaction (if a transaction exists, the query will join it).

This is done via `ITransactionalLookupIndex`, which is implemented by Transaction-compatible indexes (currently, Total indexes either PerKey or SingleBucket partitioned, or DSMI) and their bucket grains. Internally, where the Indexing infrastructure makes a call to a `LookupAsync` grain method, it will first look to see if the index or bucket supports `ITransactionalLookupIndex` and, if so, will call `LookupTransactionalAsync` instead.

## Issues And Potential Work Items

### TODOs in the Code
There are still a number of TODOs in the code, mostly related to:
  - Async streams. Note: C# 8.0 provides these, so some improvements may be possible when Orleans moves to support VS2019.
  - Grain service and stream interaction
  - On-demand index construction
  - Performance (see below)

### Performance
Some specific performance items are:
  - To reduce lock contention due to hash-bucket collisions for Per-Key indexes or SingleBucket/Per-Silo indexes, consider a secondary hash or other method to granularize by multiple states.
  - For chained buckets, it may be possible to detect in advance that the current bucket will not be modified (the index value is not found in it) and that its NextBucket is already specified, in which case we can avoid locking the current bucket with PerformUpdate. However, doing so would likely involve a PerformRead and then a scheduling point, allowing another operation to change things.
  - We currently require sequentially executing individual transactional index updates in a canonical order. This could be faster if we could execute in parallel without deadlocking.

### `IOrleansIndexingStorageProvider`
[Github issue 5432](https://github.com/dotnet/orleans/issues/5432) discusses an `IOrleansIndexingStorageProvider`. A first cut at this interface might be:
```c#
    // The storage provider creates and returns this
    interface IProviderIndex
    {
        Task UpdateAsync(IMemberUpdate update);

        // TODO: Add streaming and transactional overloads
        Task<GrainReference[]> LookupAsync<TProperty>(TProperty key);
    }

    interface IOrleansIndexingStorageProvider
    {
        // PropertyPath is dot-delimited
        Task<IProviderIndex> CreateIndex<TProperty>(Type interfaceType, Type propertiesClassType, string propertyPath, bool isUnique);
    }
```
### Multiple `IIndexedState` Facets per Grain
Transactions allows multiple `ITransactionalState<TGrainState>` objects per grain (there can be multiple facet parameters to the grain’s ctor(s)). For example, this allows different datastores per state object. It would be useful if Indexing allowed this as well. Some items to be addressed:
  - This would likely have to be Transactional-only. For example, Fault-Tolerant indexes would have to merge `GetActiveWorkflowIdsSet()` and `RemoveFromActiveWorkflowIds()`.
  - Multiple `IIndexedState.PerformRead` and `.PerformUpdate` calls would be necessary, so these would have to be made within a grain method marked `[Transaction(TransactionOption.CreateOrJoin)]`.
  - All facets on all ctors would have to have the same consistency scheme.
  - Default mapping of properties to state would be complicated; if two states have the same property name, who wins? This would likely require custom mapping.
  - DSMI currently uses the [StorageProvider] attribute on a grain class. This would have to change to recognize the providerName from the facet specification, and would require knowing which interface's properties map to which state instance (since DSMI is interface-oriented).

### Testing
A number of areas need to be more thoroughly tested:
- Failure tests:
  - Bad definitions. Currently this will always fail index loading, so define a marker interface (perhaps `IOrleansTestBadInterface`) that is ignored except when there is a service configured (perhaps `IOrleansIndexValidationTest`), in which case call its `AddFailure` method with failures instead of throwing. These tests would be things like:
    - Bad constructor facet definitions
    - All permutations of bad index attributes
    - Go through all the exceptions in `ApplicationPartsIndexableGrainLoader` and make sure each is tested
  - Uniqueness violations
- Fault-tolerant recovery and continuation have two tests in `Orleans.Indexing.Tests.Recovery`, but this needs much more rigorous testing.
- Scalability. The Unit Tests use only a small number of grains. The Benchmarks are better, but this needs to be tested in real Silos with large numbers of grains.
- Transaction tests with multiple permutations of cross-grain interfaces, to test deadlock prevention.
- Joining external transactions, both on grain index update and on query.
- ReincarnatedWorkflowQueue testing
