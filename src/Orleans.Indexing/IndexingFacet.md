# Indexing User's Guide

<!-- markdown-toc (by Jon Schlinkert) inserts the Table of Contents between the toc/tocstop comments; commandline is: markdown-toc -i <this file> -->
<!-- markdown-toc truncates `IInterface<TProperties>` before the <TProperties>, so manually enter the anchor on the heading using the name markdown-toc generates (markdown-toc does not pick up names in anchor definitions on section headers; you must use the names it generates). Then after each TOC creation, re-add <TProperties> or <TGrainState> before the closing backtick.
The same "manually enter the anchor" requirement applies to headers with a period, such as "Active vs. Total Index...". -->

<!-- toc -->

- [Overview of Indexing](#overview-of-indexing)
  * [Features](#features)
    + [Partitioning Options](#partitioning-options)
      - [Entire Index in a Single Grain](#entire-index-in-a-single-grain)
      - [Partitioned Per Silo](#partitioned-per-silo)
      - [Partitioned Per Key Hash](#partitioned-per-key-hash)
    + [Eager vs. Lazy Index Updates](#eager-vs-lazy-index-updates)
    + [Consistency scheme](#consistency-scheme)
      - [Workflow Without Fault Tolerance](#workflow-without-fault-tolerance)
      - [Workflow Fault Tolerance](#workflow-fault-tolerance)
      - [Transactional](#transactional)
    + [Active, Total, and Storage-Managed Indexes](#active-total-and-storage-managed-indexes)
    + [Large Hash Buckets](#large-hash-buckets)
  * [Source Code](#source-code)
- [Application Level](#application-level)
  * [Application Properties Classes](#application-properties-classes)
    + [Property Attributes](#property-attributes)
      - [Index type](#index-type)
      - [Other Parameters](#other-parameters)
    + [Interaction of Indexed Properties and Grain State](#interaction-of-indexed-properties-and-grain-state)
      - [NullValue for Non-Nullable Property Types](#nullvalue-for-non-nullable-property-types)
  * [Application Grain Interfaces](#application-grain-interfaces)
    + [Multiple Grain Implementations Of An Indexed Grain Interface](#multiple-grain-implementations-of-an-indexed-grain-interface)
  * [Indexing Facet Specification](#indexing-facet-specification)
    + [Attribute Specification](#attribute-specification)
      - [Indexing Consistency Scheme](#indexing-consistency-scheme)
      - [Storage Provider](#storage-provider)
        * [Direct Storage Managed Indexes (DSMI)](#direct-storage-managed-indexes-dsmi)
      - [Example Attribute Specification](#example-attribute-specification)
    + [Facet Lifecycle](#facet-lifecycle)
  * [Application Grain Implementation Classes](#application-grain-implementation-classes)
    + [Indexed Grain Implementation Requirements](#indexed-grain-implementation-requirements)
      - [Register the Storage Provider](#register-the-storage-provider)
      - [Implement the Indexed Interface `TProperties` Accessors](#implement-the-indexed-interface-tproperties-accessors)
      - [Implement Calls To `PerformRead` and `PerformUpdate`](#implement-calls-to-performread-and-performupdate)
      - [Implement IIndexableGrain Methods](#implement-iindexablegrain-methods)
  * [Querying Indexes](#querying-indexes)
  * [Things To Consider When Defining Indexes](#things-to-consider-when-defining-indexes)
    + [Supported Index Definitions](#supported-index-definitions)
    + [Partitioning Per-Silo Is Implicitly Fault-Tolerant](#partitioning-per-silo-is-implicitly-fault-tolerant)
    + [Mixing Total and Active Indexes on a Single Grain](#mixing-total-and-active-indexes-on-a-single-grain)
  * [Testing Indexes](#testing-indexes)
    + [*Player\** Tests](#player-tests)
      - [*TransactionalPlayer\** Tests](#transactionalplayer-tests)
    + [*MultiIndex\** Tests](#multiindex-tests)
    + [*MultiInterface\** Tests](#multiinterface-tests)
    + [*SharedGrainInterface* Tests](#sharedgraininterface-tests)
    + [*SportsTeamIndexing* Sample](#sportsteamindexing-sample)
- [Orleans Level](#orleans-level)
  * [Reading Property Attributes and Creating Indexes](#reading-property-attributes-and-creating-indexes)
  * [Orleans Indexing Interfaces](#orleans-indexing-interfaces)
    + [`IIndexableGrain<TProperties>`](#iindexablegrain)
  * [Orleans Indexing Implementation Classes](#orleans-indexing-implementation-classes)
    + [Inheritance-based (obsolete and removed)](#inheritance-based-obsolete-and-removed)
      - [`IndexableGrainNonFaultTolerant<TProperties>`](#indexablegrainnonfaulttolerant)
      - [`IndexableGrain<TProperties>`](#indexablegrain)
    + [Facet-based](#facet-based)
      - [Facet Attribute](#facet-attribute)
      - [The `IIndexedState<TGrainState>` Interface](#the-iindexedstate-interface)
        * [Access To The Grain State](#access-to-the-grain-state)
        * [Orleans-Supplied `IIndexedState<TGrainState>` Implementations](#orleans-supplied-iindexedstate-implementations)
  * [Data Integrity Considerations](#data-integrity-considerations)
  * [Active vs. Total Index Implementations](#active-vs-total-index-implementations)
- [Constraints on Indexing](#constraints-on-indexing)
  * [Incompatible definitions](#incompatible-definitions)
    + [Total Indexes Cannot be Partitioned Per-Silo](#total-indexes-cannot-be-partitioned-per-silo)
    + [Unique Indexes Cannot Be Active](#unique-indexes-cannot-be-active)
    + [Unique Indexes Cannot Be Partitioned Per-Silo](#unique-indexes-cannot-be-partitioned-per-silo)
    + [Active Indexes Must be Partitioned Per Silo](#active-indexes-must-be-partitioned-per-silo)
    + [Active Indexes Cannot Be Workflow Fault Tolerant](#active-indexes-cannot-be-workflow-fault-tolerant)
    + [Active Indexes Cannot Be Transactional](#active-indexes-cannot-be-transactional)
    + [Fault-Tolerant Indexes Cannot Be Eager](#fault-tolerant-indexes-cannot-be-eager)
    + [Cannot Define Both Eager And Lazy Indexes on a Single Grain](#cannot-define-both-eager-and-lazy-indexes-on-a-single-grain)
  * [Only One Indexing Consistency Scheme (FT, NFT, TRX) per Grain](#only-one-indexing-consistency-scheme-ft-nft-trx-per-grain)
  * [Only One Index per Query (==)](#only-one-index-per-query-)
    + [No Compound Indexes](#no-compound-indexes)
    + [No Conjunctions (&&)](#no-conjunctions-)
    + [No Disjunctions (||)](#no-disjunctions-)
    + [No Negations (!=)](#no-negations-)
  * [No Range Indexes (>, <, etc.)](#no-range-indexes---etc)
- [Possible Extensions to Current Design Proposal:](#possible-extensions-to-current-design-proposal)
  * [Compound Indexes](#compound-indexes)
  * [Index Conjunctions (&&)](#index-conjunctions-)
  * [Index Disjunctions (||)](#index-disjunctions-)
  * [Negations and Ranges](#negations-and-ranges)
  * [Adding Explicit TState-to-TProperties Name Mapping](#adding-explicit-tstate-to-tproperties-name-mapping)
  * [Unique Indexes Partitioned Per-Silo](#unique-indexes-partitioned-per-silo)
  * [Clean Up LookupAsync for DSMI](#clean-up-lookupasync-for-dsmi)
  * [Default IIndexableGrain implementations](#default-iindexablegrain-implementations)

<!-- tocstop -->

## Overview of Indexing
Indexing enables grains to be efficiently queried by scalar properties. A research paper describing the interface and implementation can be found [here](http://cidrdb.org/cidr2017/papers/p29-bernstein-cidr17.pdf), and a PDF with additional illustrations is [here](http://cidrdb.org/cidr2017/slides/p29-bernstein-cidr17-slides.pdf).

For more information about the indexing implementation, see the [Indexing Internals](IndexingInternals.md) document.

Indexing is defined at two levels: at the application level as property attributes on application properties classes, and by implementation classes supplied by Orleans to either update the indexes created from these attributes, or to translate LINQ queries into the retrieval of grains keyed by these indexes.

In this discussion, generic type arguments are prefaced with 'T'. `TProperties` is the type of an application-level class containing properties to be indexed (which are marked with `IndexAttribute` or a subclass). `TIProperties` is the type of an underlying interface for such a class. `TGrainState` is the type of the persistent state of the indexed grain; it must be a class type with an empty constructor, and an instance is created by the IIndexedState implementation as its State property, which functions as the backing store for all of the grain's persistent properties, indexed or not. `TIIndexableGrain` is the type of an interface that has been marked as indexable (details below).

The examples in this discussion use the [SportsTeamIndexing](#sportsteamindexing-sample) sample at Samples\2.1\SportsTeamIndexing. View the readme.md in that directory for instructions on running the sample. The SportsTeamIndexing.Interfaces project defines the grain and properties interfaces (and property classes, because the grain interface definition requires the property class definition), the SportsTeamIndexing.Grains directory defines the Grain (and GrainState) classes, and OrleansClient\ClientProgram.cs creates and queries the indexed grains.

The [SportsTeamIndexing](#sportsteamindexing-sample) sample is intended to be simple, and implements only a single indexed interface on the SportsTeamGrain. More complicated scenarios that implement multiple indexed interfaces on a single grain are also supported. This discussion will also refer to the Indexing Unit Tests for MultipleInterface, which implement three indexed interfaces on TestEmployeeGrain; those interfaces use TProperties classes that implement IPersonProperties, IJobProperties, and IEmployeeProperties. These classes are defined in test\Orleans.Indexing.Tests\Grains\MultiInterface.
### Features
Indexing is done on a per-interface, per-property basis; details are described below. An interface conceptually provides a namespace for the indexes on each of the properties of the properties-class object associated with that interface. If a grain class has one or more indexed interfaces, then all instances of that class are indexed. Currently indexes can be defined on only a single property.
#### Partitioning Options
Indexing is implemented within grains, which use storage providers to persist the index. In a cluster with multiple silos, the question naturally arises as to how the index values are partitioned across the various silos.
##### Entire Index in a Single Grain
The simplest approach is to store the entire index as a single grain on whatever silo the Orleans activation process assigns it to. As the number of indexed grains grows, this single grain becomes a bottleneck. SingleBucket indexes must be Total indexes.

SingleBucket indexes internally create a single bucket grain for the entire index; this bucket's hashtable contain an entry for each key value that has been stored for that index.
##### Partitioned Per Silo
An index may be physically partitioned such that grains and their index are on the same silo. This option is available only for Active indexes, and is the only partitioning option supported for Active indexes. This allows the silo to function as the single unit of failure, since an index and the grains it references fail together, which simplifies recovery.

Total indexes, on the other hand, cannot be partitioned PerSilo, because this would require moving them from silo to silo as they were activated. Thus there is a symmetry between Active indexes using PerSilo only, and Total indexes using only SingleBucket or PerKey.

Indexes that are partitioned Per Silo may be thought of as SingleBucket Per Silo indexes; the implementation creates one index grain per Silo (implemented by a GrainService).
##### Partitioned Per Key Hash
An index may be partitioned with a bucket for each key's hash value. Thus, one index grain contains entries for all indexed grains, across all silos, whose value for the indexed property hashes to a given value. Per Key Hash indexes must be Total indexes.

Unlike SingleBucket (and thus Per Silo) indexes, Per Key Hash indexes create a bucket grain for each individual key value that has been stored for an index. Thus, each bucket grain's hash table contains only the entries whose keys hashed to that bucket's hashcode (thus, with the default of an unlimited number of hash buckets for these indexes, there is usually only one key in each bucket's hash table).
#### <a name="eager-vs-lazy-index-updates"></a>Eager vs. Lazy Index Updates
The index hash buckets may be updated eagerly (the index updates the hash bucket directly), or lazily (the index enqueues a workflow record to perform the update).
#### Consistency scheme
The following options define how indexing assures consistency in the face of conflict or failure.
##### Workflow Without Fault Tolerance
When this option is configured (by specifying the `NonFaultTolerantWorkflowIndexedStateAttribute` on the grain's Indexing facet), no fault tolerance is provided; in the event of failure, updates may be lost. This option is most appropriate for Active indexes, which can only be partitioned index per silo; in this case the silo is the single unit of failure.
##### Workflow Fault Tolerance
When this option is configured (by specifying the `FaultTolerantWorkflowIndexedStateAttribute` on the grain's Indexing facet), a multi-step workflow is done for index updates, using the same queues that a lazy update uses. The fault-tolerance is provided by storing the list of in-flight index updates as part of the grain state; if the grain crashes, then when it is next activated, the workflows remaining in its state once again commence executing. Only total indexes can be Workflow Fault Tolerant.
##### Transactional
When this option is configured (by specifying the `TransactionalIndexedStateAttribute` on the grain's Indexing facet), the grain's state and index updates are done as part of an Orleans transaction. Only Total indexes can participate in a transaction (and thus cannot be partitioned per silo), and they cannot be lazy. 
#### Active, Total, and Storage-Managed Indexes
An Active index maintains entries for only the grains currently active; when a grain is deactivated, it is removed from the index. An Active index can only be partitioned per silo and may not use a Workflow fault tolerant or Transactional consistency scheme. These restrictions are discussed in more detail in [Constraints on Indexing](#constraints-on-indexing). They are consistent with the definition of an active index, for which it is most appropriate that their index entries live on the same silo they are active on, and the silo functions as a single unit of failure.

A Total index maintains entries for all grains that have been activated over the life of the index. Total indexes cannot be partitioned per silo. They may participate in all consistency schemes.

Storage-Managed indexes delegate all indexing to the storage provider and do not maintain any cached index state within Orleans. Currently only CosmosDB has a storage provider that implements the necessary "interface" (a way to specify the indexed properties, and a LookupAsync method that is invoked dynamically). See [Direct Storage Managed Indexes (DSMI)](#direct-storage-managed-indexes-dsmi) for more information.
#### Large Hash Buckets
An index can be configured to have very large hash buckets, to handle highly-skewed distributions of values more efficiently. The default is no limit; the actual number varies depending on the number of indexed grains and the distribution of the hash.
### Source Code
See [src/Orleans.Indexing](/src/Orleans.Indexing) (the directory containing this .md file) and [test/Orleans.Indexing.Tests](/test/Orleans.Indexing.Tests).

To build, run the `Build.cmd` or open `src\Orleans.sln`.

## Application Level
This section describes the indexing interface that is presented to the application developer.
### Application Properties Classes
Applications define the serializable data classes whose properties are indexed, for example:
```c#
    [Serializable]
    public class SportsTeamIndexedProperties : ISportsTeamIndexedProperties
    {
        [Index(typeof(IActiveHashIndexPartitionedPerSilo<string, ISportsTeamGrain>), IsEager = true, IsUnique = false)]
        public string Name { get; set; }
        // ...
    }
```
#### Property Attributes
As shown above, an index on a property is defined by placing an attribute on it. While we support placing annotations on multiple properties in a `TProperties` class, each annotation defines a separate index; we currently do not support true compound indexes (they can be simulated by using computed properties such as QualifiedName in the sample). The name of the index is derived from the property name; e.g., the Location property will result in the creation of an index currently named "__Location", under the interface deriving from `IIndexableGrain<TProperties>`. In the sample, the Interface is ISportsTeamGrain, which implements `IIndexableGrain<SportsTeamIndexedProperties>`. Details of the indexable interface are presented below.

Attributes can be specified in two ways. Above, the form `[Index(typeof(IndexInterfaceOrClassType<propertyType, interfaceType), ...)]` is used. You can also use `[ActiveIndex(ActiveIndexType, ...)]` or `[TotalIndex(TotalIndexType, ...)]`. The currently supported `[Index(...)]` types are:
- `[Index(typeof(IActiveHashIndexPartitionedPerSilo<keyType, interfaceType>), ...)]`
- `[Index(typeof(ITotalHashIndexSingleBucket<keyType, interfaceType>), ...)]`
- `[Index(typeof(TotalHashIndexPartitionedPerKey<keyType, interfaceType>), ...)]`. This uses the type of a grain implementation class rather than an interface, because there is no underlying grain implementation for Per-Key indexes themselves (there is, of course, for their buckets).

For indexes specified with `[ActiveIndex(ActiveIndexType, ...)]` or `[TotalIndex(TotalIndexType, ...)]`, the Indexing loading code will fill in the generic parameters according to the `GrainInterface<TProperties>` on which it finds the property annotation (this is done in `ApplicationPartsIndexableGrainLoader`).

##### Index type
The Index attribute may be `Index` or one of the attribute classes inheriting from it; currently these are `ActiveIndex`, `TotalIndex`, or `StorageManagedIndex`.
##### Other Parameters
Index attributes have other parameters for partitioning and other configuration objects as described under [Features](#features).
#### Interaction of Indexed Properties and Grain State
Generally, the `TGrainProperties` class is a base class of `TGrainState`, because the indexing implementation casts `TGrainState` to `TProperties` if possible. If there are multiple `TIIndexableGrain`s with multiple `TProperties`, then direct inheritance is not possible. For any `TProperties` that is not a base of `TGrainState`, the Indexing implementation creates an ephemeral `TProperties` instance and copies `TGrainState`'s property values using direct name matching. (Note that the application Grain need not create an instance of `TProperties`; rather, it interacts with the the `IIndexedState<TGrainState>` implementation.) The properties of `TProperties` are then written to index storage.

The grain state is the backing storage for properties values and will be written to persistent storage; thus, the state class (and not the properties class) must have the [Serializable] attribute. Additionally, because the index specifications are not relevant to the state persistence, the same state class may be used as the backing storage specification for multiple properties classes.

For a single indexed interface, the `TGrainState` class can inherit from the `TProperties` class. For example:
```c#
    public interface ISportsTeamIndexedProperties
    {
        string Name { get; set; }
        string QualifiedName { get; set; }
        string Location { get; set; }
        string League { get; set; }
    }

    public class SportsTeamIndexedProperties : ISportsTeamIndexedProperties
    {
        [Index(typeof(IActiveHashIndexPartitionedPerSilo<string, ISportsTeamGrain>), IsEager = true, IsUnique = false)]
        public string Name { get; set; }

        [Index(typeof(IActiveHashIndexPartitionedPerSilo<string, ISportsTeamGrain>), IsEager = true, IsUnique = true)]
        public string QualifiedName { get => JoinName(this.League, this.Name); set => SplitName(value); }

        [ActiveIndex(ActiveIndexType.HashIndexPartitionedByKeyHash, IsEager = true, IsUnique = false)]
        public string Location { get; set; }

        [ActiveIndex(ActiveIndexType.HashIndexPartitionedByKeyHash, IsEager = true)]
        public string League { get; set; }
    }

    [Serializable]
    public class SportsTeamState : SportsTeamIndexedProperties
    {
        // This property is not indexed.
        public string Venue { get; set; }
    }
```

For multiple indexed interfaces, the `TGrainState` class and `TProperties` classes should implement common interfaces, since multiple inheritance is not available. For example:
```c#
    // This code block is a simplification of what is in the MultiInterface Unit Tests.
    public interface IPersonProperties
    {
        string Name { get; set; }
        int Age { get; set; }
    }

    public interface IJobProperties
    {
        string Title { get; set; }
        string Department { get; set; }
    }

    public interface IEmployeeProperties
    {
        int EmployeeId { get; set; }
    }

    public class PersonProperties : IPersonProperties
    {
        [Index(typeof(IActiveHashIndexSingleBucket<string, IPersonGrain>), IsEager = true, IsUnique = true)]
        public string Name { get; set; }

        [Index(typeof(IActiveHashIndexSingleBucket<int, IPersonGrain>), IsEager = true, IsUnique = false, NullValue = "0")]
        public int Age { get; set; }
    }

    [Serializable]
    public class JobProperties : IJobProperties
    {
        [Index(typeof(IActiveHashIndexSingleBucket<string, IJobGrain>), IsEager = true, IsUnique = true)]
        public string Title { get; set; }

        [Index(typeof(IActiveHashIndexSingleBucket<string, IJobGrain>), IsEager = true, IsUnique = false)]
        public string Department { get; set; }
    }

    [Serializable]
    public class EmployeeProperties : IEmployeeProperties
    {
        [Index(typeof(IActiveHashIndexSingleBucket<int, IEmployeeGrain>), IsEager = true, IsUnique = true, NullValue = "-1")]
        public int EmployeeId { get; set; }
    }
```
##### NullValue for Non-Nullable Property Types
In the foregoing code example, the IndexAttribute specifications for the properties of type int that are unique must have the "NullValue" attribute. This is because, unlike string properties, integers cannot be null, so Indexing will write an index entry for a new grain even when its values have not yet been set or the grain's state saved, which is bad in general and, for unique indexes, will result in erroneous uniqueness violations when more than one such grain is created. Because 0 may be a valid value, the index must specify the value for which the property is to be considered null.

### Application Grain Interfaces
Indexes are grouped by the interface for which they are defined, although the indexes are named for the `TProperties` properties, not any properties defined on the interfaces. Essentially, the interface name functions as a namespace for the group of indexes defined on the `TProperties` of the interface's base `IIndexableGrain<TProperties>` specification.

`TIIndexableGrain`s defined by the application must inherit from [`IIndexableGrain<TProperties>`](#iindexablegrain). Additionally, they may define Task-based properties to set the underlying properties; the properties are Task-based properties because they are implemented on the [Grain implementation class](#application-grain-implementation-classes). These property settings are usally a convenience for the applications; in general the implementations on the grain will simply call PerformRead or PerformUpdate, as described in [Access To The Grain State](#access-to-the-grain-state). For example:
```c#
    public interface ISportsTeamGrain : IGrainWithIntegerKey, IIndexableGrain<SportsTeamIndexedProperties>
    {
        #region indexed as a computed property
#if USE_TRANSACTIONS
        [Transaction(TransactionOption.CreateOrJoin, ReadOnly = true)]
#endif
        Task<string> GetQualifiedName();
        #endregion indexed as a computed property

        #region indexed as single properties
#if USE_TRANSACTIONS
        [Transaction(TransactionOption.CreateOrJoin, ReadOnly = true)]
#endif
        Task<string> GetName();
#if USE_TRANSACTIONS
        [Transaction(TransactionOption.CreateOrJoin)]
#endif
        Task SetName(string name);

#if USE_TRANSACTIONS
        [Transaction(TransactionOption.CreateOrJoin, ReadOnly = true)]
#endif
        Task<string> GetLocation();
#if USE_TRANSACTIONS
        [Transaction(TransactionOption.CreateOrJoin)]
#endif
        Task SetLocation(string location);

#if USE_TRANSACTIONS
        [Transaction(TransactionOption.CreateOrJoin, ReadOnly = true)]
#endif
        Task<string> GetLeague();
#if USE_TRANSACTIONS
        [Transaction(TransactionOption.CreateOrJoin)]
#endif
        Task SetLeague(string league);
        #endregion indexed as single properties

        #region not indexed
#if USE_TRANSACTIONS
        [Transaction(TransactionOption.CreateOrJoin, ReadOnly = true)]
#endif
        Task<string> GetVenue();
#if USE_TRANSACTIONS
        [Transaction(TransactionOption.CreateOrJoin)]
#endif
        Task SetVenue(string venue);
        #endregion not indexed

#if USE_TRANSACTIONS
        [Transaction(TransactionOption.CreateOrJoin, ReadOnly = true)]
#endif
        Task<SportsTeamState> ReadStateAsync();

#if USE_TRANSACTIONS
        [Transaction(TransactionOption.CreateOrJoin)]
#endif
        Task WriteStateAsync(SportsTeamState state);
    }
```
The presence of "#if USE_TRANSACTIONS" illustrates switching between indexing consistency schemes to determine which is best for your application. This switch is on the grain interface (so the `Transaction` attribute is conditionally added to grain methods) and on the grain constructor (so the appropriate facet implementation is specified).

In addition to the Task-based property accessors, this interface also defines a method SaveAsync to save the state of the Grain and its indexes.

#### Multiple Grain Implementations Of An Indexed Grain Interface
It is possible to implement an indexed grain interface on multiple grain classes; the [*SharedGrainInterface* Tests](#sharedgraininterface-tests) provides a simple illustratration of how to do this. 

Sharing interfaces on multiple grains allows creating a hierarchical system of indexed interfaces, though some design will be required in order to know what valid casts exist.

The key point to doing this is to call `IGrainFactory.GetGrain()` with the grainClassNamePrefix set to a unique prefix of the implementing class name (if it is not unique, or if this is not done, then Orleans will throw an exception due to ambiguous class resolution). The tests illustrate passing `nameof(implementingClass)` for this parameter. Care must be taken that one implementing class name is not a substring of another implementing class name.

### Indexing Facet Specification
The grain developer specifies which indexing consistency scheme to use via a Facet: this is done by adding an [`IIndexedState<TGrainState>`](#the-iindexedstate-interface) parameter to an indexed grain's constructor, and decorating it with an attribute that identifies the indexing consistency scheme to use for that grain. The Indexing implementation and the underlying Orleans Facet System create an appropriate subclass of IIndexedState to pass as the actual constructor argument.

#### Attribute Specification
The attribute determines the consistency scheme and storage provider to use for this grain.
##### Indexing Consistency Scheme
The attribute type determines whether a workflow-based (either fault-tolerant or non-fault-tolerant) or transactional indexing consistency scheme is to be used, and specifies the name of the storage provider
The attribute must also implement the Orleans IFacetMetadata marker interface; this tells the Orleans infrastructure to instantiate the facet implementation. The Orleans-supplied attributes are listed here briefly, and are described in detail in the [Indexing Implementation](#orleans-indexing-implementation-classes) section:
```c#
    public class NonFaultTolerantWorkflowIndexedStateAttribute : IndexedStateAttribute, IFacetMetadata, INonFaultTolerantWorkflowIndexedStateAttribute, IIndexedStateConfiguration {...}

    public class FaultTolerantWorkflowIndexedStateAttribute : IndexedStateAttribute, IFacetMetadata, IFaultTolerantWorkflowIndexedStateAttribute, IIndexedStateConfiguration {...}

    public class TransactionalIndexedStateAttribute : IndexedStateAttribute, IFacetMetadata, IFaultTolerantWorkflowIndexedStateAttribute, IIndexedStateConfiguration {...}
```

##### Storage Provider
All of the IndexedStateAttribute subclasses have a constructor of the form:
```c#
        public /* class name */(string stateName, string storageName = null) 
        {
            base.StateName = stateName;
            base.StorageName = storageName;
        }
```
Any `[StorageProvider]` attribute on an indexed grain class is ignored. This effectively means that the grain's storage specification is an aspect of the Indexing facet. However, there is one additional requirement for Direct Storage Managed Indexes.

###### Direct Storage Managed Indexes (DSMI)
These indexes are managed entirely by the underlying storage provider: rather than hash buckets maintained by Orleans Indexing, the indexing storage is implemented by the storage provider (such as a database table). For these indexes, Orleans Indexing still relies upon inspecting the [StorageProvider] attribute in order to know what provider to issue a lookup to, for example:
```c#
    [StorageProvider(ProviderName = IndexingTestConstants.CosmosDBGrainStorage)]
    public class MyIndexedGrain : Grain, ...
    { ... }
```


The storage provider must implement the following method, which is invoked via "dynamic" by Orleans Indexing:
```c#
    public async Task<List<GrainReference>> LookupAsync<K>(string grainType, string indexedField, K key)
    { ... }
```

An interface that would be implemented by providers, tentatively named `IOrleansIndexingStorageProvider`, has been proposed in [Github issue 5432](https://github.com/dotnet/orleans/issues/5432).

Currently the only recognized storage provider is [Orleans.CosmosDB](https://github.com/OrleansContrib/Orleans.CosmosDB).

While the Indexing infrastructure has been updated to place these indexes within a transaction context, the CosmosDB provider does not yet support the required [Orleans Transactions](http://dotnet.github.io/orleans/Documentation/grains/transactions.html) interfaces (there is currently a PR pending for this). Because of this, the `IStorage` implementation is wrapped in a storage bridge by the Orleans Transactions implementation. Because this wrapping adds an additional element to the indexed-property specified in the CosmosDB StateFieldsToIndex configuration (see BaseIndexingFixture.cs in the Indexing unit tests), a `TransactionalIndexedState` attribute must specify `IndexUtils.IndexedGrainStateName` as the `stateName`.

##### Example Attribute Specification
Here is an example of IndexedStateAttribute use; the IIndexedState constructor parameter is instantiated by the Orleans Facet infrastructure based upon the Attribute type and is stored in the Grain class for later use. For more information see the [Grain implementation class](#application-grain-implementation-classes) section below.
```c#
    IIndexedState<SportsTeamState> indexedState;

    public SportsTeamGrain(
        [NonFaultTolerantWorkflowIndexedState(GrainStoreName)]
        IIndexedState<SportsTeamState> indexedState) => this.indexedState = indexedState;
```
#### Facet Lifecycle
The lifecycle of the Indexing Facet is hooked into the Grain's activation lifecycle. As such, there is no longer a need to override the Grain's `OnActivateAsync()` and `OnDeactivateAsync()` methods.
### <a name="application-grain-implementation-classes"></a>Application Grain Implementation Classes
Grain classes that implement a `TIIndexableGrain` must specify an Indexing facet on the constructor, as described elsewhere. This constructor argument is an `IIndexedState<TGrainState>` instance which stores the actual state data (the `TProperties` instance is ephemeral, created only during the process of index writing). For example: 
```c#
    public class SportsTeamGrain : Grain, ISportsTeamGrain, IIndexableGrain<SportsTeamIndexedProperties>
    {
        // This must be configured when setting up the Silo; see SiloHost.cs StartSilo().
        public const string GrainStoreName = "SportsTeamGrainMemoryStore";

        private readonly IIndexedState<SportsTeamState> indexedState;

        public SportsTeamGrain(
#if USE_TRANSACTIONS
            [TransactionalIndexedState("stateName", GrainStoreName)]
#else
            [NonFaultTolerantWorkflowIndexedState("stateName", GrainStoreName)]
#endif
            IIndexedState<SportsTeamState> indexedState) => this.indexedState = indexedState;

        #region indexed as a computed property
        public Task<string> GetQualifiedName() => this.indexedState.PerformRead(state => state.QualifiedName);
        #endregion indexed as a computed property

        #region indexed as single properties
        public Task<string> GetName() => this.indexedState.PerformRead(state => state.Name);
        public Task SetName(string value) => this.indexedState.PerformUpdate(state => state.Name = value);

        public Task<string> GetLocation() => this.indexedState.PerformRead(state => state.Location);
        public Task SetLocation(string value) => this.indexedState.PerformUpdate(state => state.Location = value);

        public Task<string> GetLeague() => this.indexedState.PerformRead(state => state.League);
        public Task SetLeague(string value) => this.indexedState.PerformUpdate(state => state.League = value);
        #endregion indexed as single properties

        #region not indexed
        public Task<string> GetVenue() => this.indexedState.PerformRead(state => state.Venue);
        public Task SetVenue(string value) => this.indexedState.PerformUpdate(state => state.Venue = value);
        #endregion not indexed

        public Task<SportsTeamState> ReadStateAsync() => this.indexedState.PerformRead(state => state);

        public Task WriteStateAsync(SportsTeamState value) => this.indexedState.PerformUpdate(state =>
                                                                {
                                                                    if (value.Name != null) state.Name = value.Name;
                                                                    if (value.Name != null) state.Location = value.Location;
                                                                    if (value.Name != null) state.League = value.League;
                                                                    if (value.Name != null) state.Venue = value.Venue;
                                                                });

        #region required implementations of IIndexableGrain methods; they are only called for FaultTolerant index writing
        public Task<Immutable<System.Collections.Generic.HashSet<Guid>>> GetActiveWorkflowIdsSet() => this.indexedState.GetActiveWorkflowIdsSet();
        public Task RemoveFromActiveWorkflowIds(System.Collections.Generic.HashSet<Guid> removedWorkflowId) => this.indexedState.RemoveFromActiveWorkflowIds(removedWorkflowId);
        #endregion required implementations of IIndexableGrain methods
```

Grain classes may implement multiple `TIIndexableGrain`s. For example:
```c#
    // This code block is a simplification of what is in the MultiInterface Unit Tests.
    public interface IPersonGrain : IIndexableGrain<PersonProperties>, IGrainWithIntegerKey
    {
    }

    public interface IJobGrain : IIndexableGrain<JobProperties>, IGrainWithIntegerKey
    {
    }

    public interface IEmployeeGrain : IIndexableGrain<EmployeeProperties>, IGrainWithIntegerKey
    {
    }

    public class EmployeeGrain<EmployeeGrainState, IndexableGrainStateWrapper<EmployeeGrainState>>,
                                               IPersonGrain, IJobGrain, IEmployeeGrain
    {
        ...
    }
```
#### Indexed Grain Implementation Requirements
Using the SportsTeamGrain example above, there are a few things the grain implementation must do for indexing to work properly.
##### Register the Storage Provider
The above example defines a name for the Storage Provider that will be used for the grain. A Storage Provider with this name must be registered during Silo startup.
##### Implement the Indexed Interface `TProperties` Accessors
As shown above, the grain may implement property setting by supplying Task-based property wrappers. As with all Orleans Grains, such methods must be Task-based as part of the remoting system. The grain simply implements an interface method call as a property get or set on its contained `TGrainState` instrance. See the actual code files for the implementation of a SetProperty() private method.

These per-property getters and setters are usually just a convenience for the application; the implementations are usually just a wrapper around `PerformRead` or `PerformUpdate`, as described in [Access To The Grain State](#access-to-the-grain-state).

Again, notice that there is no `TProperties` instantiated; the index interface implementations operate on the `TGrainState` instance. The IIndexedState creates ephemeral `TProperties` instances as part of its index-writing operation.
##### Implement Calls To `PerformRead` and `PerformUpdate`
Writing the grain state can be done on a per-property basis (where each property-set writes the state to storage), or the grain may provide a method on one or more interfaces that allows a consumer of that grain to tell it explicitly to save its state (including any modified index entries). As illustrated here, the grain might implement its own `WriteStateAsync()` method (again, this is totally separate from `Grain<TGrainState>.WriteStateAsync`). In its simplest form this method is simply a redirector to `IIndexWriter<TGrainState>.PerformUpdate()`, and it may do other operations as well, either inside or outside the lambda passed to `PerformUpdate`.

Reading the grain state can be done on a per-property basis (where each property-get reads the state from storage), or the grain may provide a method on one or more interfaces that allows a consumer of that grain to tell it explicitly to re-read its state. As illustrated here, the grain might implement its own `ReadStateAsync()` method (again, this is totally separate from `Grain<TGrainState>.ReadStateAsync`). In its simplest form, this method is simply a redirector to `IIndexWriter<TGrainState>PerformRead()`, and it may do other operations as well. Because `IIndexWriter<TGrainState>.PerformRead()` reloads the backing storage for the grain's indexed properties, it discards any non-persisted changes to the indexed properties as well as to the non-indexed state.

##### Implement IIndexableGrain Methods
The `IIndexableGrain` methods `GetActiveWorkflowIdsSet()` and `RemoveFromActiveWorkflowIds()` are necessary for the fault-tolerant internal implementation to communicate back to the grain to retrieve and update the set of in-flight workflows. As shown above, these simply redirect to the matching implementation method of the `indexedState`. (In the future, Indexing should provide [Default IIndexableGrain implementations](#default-iindexablegrain-implementations) for these.)
### Querying Indexes
Querying is done by LINQ:
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
Orleans.Indexing reads the ExpressionTrees created by LINQ to determine the property that is being requested and translates this into a read operation on the index.

For an index that is partitioned per silo, queries fan out to all silos.

For a transactional index, the query need not run within a transaction; one will be created if necessary.
### Things To Consider When Defining Indexes
#### Supported Index Definitions
This table describes the elements of an index definition.
<table>
    <tr>
        <th>Consistency Scheme</th>
        <th>Type</th>
        <th>Access</th>
        <th>Partitioning</th>
    </tr>
    <tr>
        <td>FT = Fault Tolerant<br/>NFT = Non Fault Tolerant<br/>TXN = Transactional</td>
        <td>AI = Active Index<br/>TI = Total Index</td>
        <td>EG = Eager<br/>LZ = Lazy</td>
        <td>PK = Per Key Hash<br/>PS = Per Silo<br>SB = Single Bucket (not partitioned)</td>
    </tr>
</table>


This table lists the combinations of these elements that are currently valid for index definitions. See [Constraints on Indexing](#constraints-on-indexing) for details of why other combinations are not valid, as well as other limitations such as invalid Uniqueness specifications.

<table>
    <tr>
        <th>Consistency Scheme</th>
        <th>Type</th>
        <th>Access</th>
        <th>Partitioning</th>
    </tr>
    <tr>
        <td>FT</td>
        <td>TI</td>
        <td>LZ</td>
        <td>PK, SB</td>
    </tr>
    <tr>
        <td>NFT</td>
        <td>AI</td>
        <td>LZ, EG</td>
        <td>PS</td>
    </tr>
    <tr>
        <td>NFT</td>
        <td>TI</td>
        <td>LZ, EG</td>
        <td>PK, SB</td>
    </tr>
    <tr>
        <td>TXN</td>
        <td>TI</td>
        <td>EG</td>
        <td>PK, SB</td>
    </tr>
</table>

#### Partitioning Per-Silo Is Implicitly Fault-Tolerant
For per-silo partitioning, a grain writes its index entries to the same silo it is activated on. Thus, the silo becomes the single unit of failure. This is the only form of workflow-based fault-tolerance currently supported for Active indexes. 
#### Mixing Total and Active Indexes on a Single Grain
It is possible to mix Total and Active indexes on a single grain that uses non-fault-tolerant workflow indexing or transactional indexing. However, it requires some care to avoid potentially unexpected grain activation. Indexed queries return GrainReferences, which does not cause grain activation. However, retrieving a property or calling a method on a grain interface will activate the grain. Retrieving objects by querying a Total index and then enumerating them to retrieve a property will result in activating them all. This can be avoided by considering the Total index to be used for counting only, while an Active index is used for actual operations. For example:
- Create a Total index on Player.Location
- Create an Active index on Player.Game
- Now you can intersect the results of querying these indexes to get Active Halo players in Seattle and call methods on those grains.
- In contrast, if you had looped through the Total index to retrieve player.Location, then all grains would have been activated.
- Variation: create an Active index on another interface, also on Player.Location. Now you can report "How many Total players are in Seattle? How many are currently Active?"
### Testing Indexes
See test\Orleans.Indexing.Tests for the Unit Tests. These are clustered into 3 groups as defined in the following sections.
#### *Player\** Tests
The *Player* series of tests focuses primarily on indexing Player Location (and occasionally Score).
- Interfaces and classes are defined in the [test/Orleans.Indexing.Tests/Grains/Players](/test/Orleans.Indexing.Tests/Grains/Players) subdirectory.
- Test runners are defined in the [test/Orleans.Indexing.Tests/Runners/Players](/test/Orleans.Indexing.Tests/Runners/Players) subdirectory.

These use a number X for a grouping of tests, with the following form:
- `IPlayerProperties` defines the properties of a player that may be indexed: Location and Score.
- `IPlayerState` defines an additional non-indexed property: Email.
- `IPlayerGrain` defines all properties of a player, expressed in paired async Get/Set properties returning Tasks.
  - For these tests, players have three properties: Location and Score may be indexed, and Email is not indexed.
- [test/Orleans.Indexing.Tests/Grains/ITestIndexProperties.cs](/test/Orleans.Indexing.Tests/Grains/ITestIndexProperties.cs) describes the abbreviations used in the file and test names (including the interface names and class names for properties and grains, but *not* the property names; MultiInterface uses the properties interface name instead), but MultiInterface does not otherwise use ITestIndexProperties. These abbreviations are inserted where "_*" is shown in the following description.
- `PlayerProperties_* : IPlayerProperties` defines the indexed properties of the player.
- `IPlayer_* : IPlayerGrain, IIndexableGrain<PlayerProperties_*>` defines the interface for the player implementation.
- `PlayerGrain*<TState, TProps> : IndexableGrain<TState, TProps>, IPlayerGrain where TState : IPlayerState where TProps : new()` is the base class implementing common functionality for all player grains, where:
  - TState must be IPlayerState or a subclass
  - TProps must be a class (and should derive from IPlayerProperties)
  - "*" is one of "NonFaultTolerant", "FaultTolerant", or "Transactional"
    - All of these inherit the base PlayerGrain implementation
- `Player_* : PlayerGrain*<PlayerGrainState, PlayerProperties_*>, IPlayer_*` defines the implementing class for the IPlayer_* grain interface.

##### *TransactionalPlayer\** Tests
The *Player* tests also include tests for rolling back inserts and updates to the indexes.

#### *MultiIndex\** Tests
The *MultiIndex\** series of tests is separate from the *Player* series. This series of tests focuses on multiple indexes per grain.
- Base State, Property, and Grain interfaces are defined in the [test/Orleans.Indexing.Tests/Grains/MultiIndex](/test/Orleans.Indexing.Tests/Grains/MultiIndex) subdirectory.
- Test runners are also defined in the [test/Orleans.Indexing.Tests/Runners/MultiIndex](/test/Orleans.Indexing.Tests/Runners/MultiIndex) subdirectory.
  - Each file contains the state, property, interface, and grain implementation definitions, as defined by the file name.
  - [test/Orleans.Indexing.Tests/Grains/ITestIndexProperties.cs](/test/Orleans.Indexing.Tests/Grains/ITestIndexProperties.cs) describes the abbreviations used in the file and test names.
    - For example, MultiIndex_AI_EG_Runner defines all interfaces, classes, and tests to implement testing for Eager Active indexes.
  - Testing includes unique and nonunique indexes on string and int. Additional combinations are TBD.

#### *MultiInterface\** Tests
The *MultiInterface\** series of tests focuses on multiple indexed interfaces, each with one or more indexed properties, per grain. The multi-interface capability was introduced along with the Facet implementation. The tests are organized similarly to the *MultiIndex\** series:
- Base State, Property, and Grain interfaces are defined in the [test/Orleans.Indexing.Tests/Grains/MultiInterface](/test/Orleans.Indexing.Tests/Grains/MultiInterface) subdirectory.
- Test runners are also defined in the [test/Orleans.Indexing.Tests/Runners/MultiInterface](/test/Orleans.Indexing.Tests/Runners/MultiInterface) subdirectory.
  - Each file contains the state, property, interface, and grain implementation definitions, as defined by the file name.
  - [test/Orleans.Indexing.Tests/Grains/ITestIndexProperties.cs](/test/Orleans.Indexing.Tests/Grains/ITestIndexProperties.cs) describes the abbreviations used in the file and test names (except for those related to property names; MultiInterface uses the properties interface name instead), but MultiInterface does not otherwise use ITestIndexProperties.
    - For example, MultiInterface_AI_EG_Runner defines all interfaces, classes, and tests to implement testing for Eager Active indexes.
  - Testing uses IPersonGrain, IJobGrain, and IEmployeeGrain indexed interfaces on an Employee grain.

#### *SharedGrainInterface* Tests
There is a single test runner that tests (and illustrates how to use) an indexed grain interface that is implemented on multiple grain classes. These are implemented in a single file, [test/Orleans.Indexing.Tests/Runners/SharedGrainInterfaceRunner.cs](/test/Orleans.Indexing.Tests/Runners/SharedGrainInterfaceRunner.cs), which includes all definitions for grain interfaces and for state, grain, and grain classes.

#### *SportsTeamIndexing* Sample
The SportsTeamIndexing sample at Samples\2.1\SportsTeamIndexing illustrates creating a simple indexed application, and serves as an example for creating tests outside the Unit Testing framework. It also illustrates some of the changes required when moving an application's consistency scheme from Workflow to Transactions.

## Orleans Level
This section defines the Indexing implementation within Orleans.Indexing.

### Reading Property Attributes and Creating Indexes
At Silo startup, the Indexing implementation uses reflection on each Grain class in all assemblies in its ApplicationParts. It reads the list of implemented interfaces for the Grain class and for any Indexable interfaces, it creates and validates the indexes defined on the properties of the `TProperties` of the `IIndexableGrain<TProperties>` underlying that `TIIndexableGrain`. (There is also a public API, `IndexValidator.Validate(Assembly assembly)`, to allow indexes to be validated during Unit Tests.) These indexes are cached, and the cache is keyed on `TIIndexableGrain`, with the value being the list of indexes for that interface. The cache also contains a dictionary of grain implementation classes to their indexed interfaces, for efficient retrieval during `IIndexedState` instantiation.
### Orleans Indexing Interfaces
#### <a name="iindexablegrain"></a>`IIndexableGrain<TProperties>`
In both the old inheritance-based system and the new Facet system, this not only marks the interface as being indexable, but it also defines a couple of methods necessary for the internal fault-tolerant indexing to communicate with the grain to manage the set of in-flight workflows. In the inheritance-based system, the indexing-specific subclass of `Grain<TGrainState>` implemented these; in the Facet system, the grain must implement these as simple call-throughs to the `IIndexedState<TGrainState>` implementation, which as the name implies also manages the `TGrainState` instance.
```c#
    // Interface for a grain interface that will "contain" indexed properties (which are the properties of TProperties).
    public interface IIndexableGrain<TProperties> : IGrain where TProperties: new()
    {
    }

    // Non-generic base interface for indexable grains; provides methods that allow the fault-tolerant indexing
    // implementation to call back to the grain to retrieve and update the list of in-flight workflows.
    public interface IIndexableGrain : IGrain
    {
        /// <summary>
        /// This method returns the set of active workflow IDs for a Total Index
        /// </summary>
        Task<Immutable<HashSet<Guid>>> GetActiveWorkflowIdsSet();

        /// <summary>
        /// This method removes a workflow ID from the list of active workflow IDs for a Total Index
        /// </summary>
        Task RemoveFromActiveWorkflowIds(HashSet<Guid> removedWorkflowId);
    }
```
### Orleans Indexing Implementation Classes
Orleans.Indexing has changed from inheritance to containment; rather than inheriting from an intermediate `Grain<TGrainState>` class implementation that overrides `Grain<TGrainState>.WriteStateAsync()` and other Grain functionality, exposing a facet provides the ability to utilize a contained implementation through constructor injection. This allows multiple facets (such as Transactions and Indexing) to be implemented, as well as allowing customized implementations. The facet is embodied in an `IIndexedState<TGrainState>` implementation that is passed to the grain's Indexing facet constructor argument.
#### Inheritance-based (obsolete and removed)
Under the inheritance design, each indexed grain had to inherit from one of these grain classes, which override WriteStateAsync to provide persistence. This approach has been entirely removed, along with the implementation classes listed here; this section remains in order to assist in migration.
##### <a name="indexablegrainnonfaulttolerant"></a>`IndexableGrainNonFaultTolerant<TProperties>`
An application grain inherits from this if its indexes do not have to be fault-tolerant. This is also the base class for `IndexableGrain<TProperties>`.
##### <a name="indexablegrain"></a>`IndexableGrain<TProperties>`
An application grain inherits from this if its indexes must be fault-tolerant. With fault-tolerant indexing, the in-flight workflows and queues are persisted along with the Grain state (the implementation swaps the base State with a wrapper that includes the original base State as well as the set of in-flight workflow IDs and the active queues). When the grain is reactivated (such as if a server crashed during a previous call), the in-flight workflow state is restored with it, and any pending workflows resume executing. 
#### Facet-based
##### Facet Attribute
The attribute determines whether workflow (fault-tolerant or non-fault-tolerant) or transactional indexing is to be used, and specifies any necessary parameters between the two. See [Indexing Facet Specification](#indexing-facet-specification) above for more information.
##### <a name="the-iindexedState-interface"></a>The `IIndexedState<TGrainState>` Interface
This is the base class for the indexing Facet implementation. One `IIndexedState` parameter must be on at least one constructor of the indexed grain (there is validation to ensure that there is exactly one for any constructor of a grain that implements a `TIIndexableGrain`, and that there is at least one constructor that has such a parameter). 

The implementation of this interface coordinates the writing of all indexed interfaces defined on the grain. It will retrieve the list of indexed interfaces for the grain from caches that are created during assembly load when indexes are read, validated, and created. It uses the IndexRegistry to maintain cached per-grain-class lists of interfaces and their indexes and properties to do the mapping from `TGrainState` to `TProperties`. If `TGrainState` inherits from `TProperties`, then a simple assignment to the `TProperties` instance is possible; otherwise, an ephemeral instance of `TProperties` is created and the `TGrainState`'s properties are mapped to the corresponding `TProperties` properties. If the index is workflow-based, the indexedState includes the grain state update in the workflow appropriately.

###### Access To The Grain State
`IIndexedState` uses the same approach to state access as [`ITransactionalState<TGrainState>`](http://dotnet.github.io/orleans/Documentation/grains/transactions.html): the actual storage member is hidden from the `IIndexedState` consumer, and access to it is through the `PerformRead` and `PerformUpdate` methods. (In fact, `ITransactionalIndexedState<TGrainState>` internally wraps an instance of `ITransactionalState<TGrainState>`). These state access methods take a lambda argument which receives a `TGrainState` instance and returns an arbitrary type and functions as follows:
- `PerformRead`: the lambda provides access to the state after it has been read. For example, the application can read the stored state and return the result of some action on one or more fields. For Workflow indexes, for efficiency the state is a reference to the contained storage provider's state. For Transactions, it is a copy of the underlying state, which cannot be modified. Actions in this lambda should be read-only.
- `PerformUpdate`: the lambda provides access to the state before it has been written. For example, the application can modify one or more properties before the state is written, and return a value based on those properties. For Workflow indexes, for efficiency the state is a reference to the contained storage provider's state. For Transactions, it is a separate instance of state class which is copied to the underlying state.

As the [SportsTeamIndexing](#sportsteamindexing-sample) sample shows, there are two primary approaches to updating grain state: per-property or batched for multiple properties. In the per-property approach, each grain-interface property update writes its corresponding state via `PerformUpdate(state => state.<Property> = <value>);`. In the batched approach, the application buffers changes to one or more properties in local variables (or perhaps a local instance of `TGrainState` or `TProperties`) until all property updates are ready, and then calls `PerformUpdate(state => {state.<Property1> = <value1>; state.<Property2> = <value2>; ...});`. In the latter case, the grain-interface properties may function only as metadata for index definitions; setting grain state may bypass them entirely.

When local state is buffered in an instance of `TGrainState` or `TProperties`, care must be taken that the NullValues for any non-nullable types are correctly initialized. This can, of course, be done by a grain method that reads and returns the entire state, as illustrated here and in `ITestTransactionalPersistence.InitializeStateTxn()` in the Indexing Unit Tests. One scenario for batching together multiple property updates is at the initial creation of a grain.

When Transactional indexes are used, the grain should not buffer the state itself. Any such state buffered in the grain is outside the transaction space and thus may contain inconsistent values in the event of a rollback.

###### <a name="orleans-supplied-iindexedstate-implementations"></a>Orleans-Supplied `IIndexedState<TGrainState>` Implementations
Orleans Indexing supplies three interfaces (and their implementation classes) deriving from `IIndexedState`; the workflow implementations contain the implementation moved from the previous inheritance-based approach. An indexed Grain class should store the IIndexedState as a data member assigned from the constructor's facet parameter. The Orleans implementations for the Orleans-provided interfaces are injected at Silo startup time, and an application can define its own as well.

With the exception of the workflow ID set methods on IIndexedGrain, which an indexed Grain implements by simply passing the call along to the matching methods on the IIndexedState, the details of the various IIndexedState implementations are completely opaque to the indexed grain. The indexed grain class specifies the desired implementation on the Facet attribute of the IIndexedState constructor parameter, and the IIndexedState is instantiated with the specified implementation.

```c#
    // The base interface definition for a class that implements the indexing facet of a grain.
    public interface IIndexedState<TGrainState> where TGrainState : new()
    {
        /// <summary>
        /// Reads the grain state, which resets the value of all indexed and non-indexed properties.
        /// </summary>
        Task<TResult> PerformRead<TResult>(Func<TGrainState, TResult> readFunction);

        /// <summary>
        /// Executes <paramref name="updateFunction"/> then writes the grain state and the index entries for all indexed interfaces
        /// defined on the grain.
        /// </summary>
        Task<TResult> PerformUpdate<TResult>(Func<TGrainState, TResult> updateFunction);

        #region Workflow Fault-Tolerant support
        /// <summary>
        /// This method returns the set of active workflow IDs for a fault-tolerant Total Index
        /// </summary>
        Task<Immutable<HashSet<Guid>>> GetActiveWorkflowIdsSet();

        /// <summary>
        /// This method removes a workflow ID from the list of active workflow IDs for a fault-tolerant Total Index
        /// </summary>
        Task RemoveFromActiveWorkflowIds(HashSet<Guid> removedWorkflowId);
        #endregion Workflow Fault-Tolerant support
    }

    public interface INonFaultTolerantWorkflowIndexedState<TGrainState> : IIndexedState<TGrainState> where TGrainState : new()
    {
    }

    public interface IFaultTolerantWorkflowIndexedState<TGrainState> : IIndexedState<TGrainState> where TGrainState : new()
    {
    }

    public interface ITransactionalIndexedState<TGrainState> : IIndexedState<TGrainState> where TGrainState : new()
    {
    }
```
### Data Integrity Considerations
As described in [the paper](http://cidrdb.org/cidr2017/papers/p29-bernstein-cidr17.pdf), the Grain state and Index state must remain consistent. Additionally, unique indexes must have zero or one entry.

The [Constraints on Indexing](#constraints-on-indexing) section below describes some limitations on index definitions to prevent some scenarios for which these requirements cannot be guaranteed. This section clarifies some considerations for the supported index definitions and partitioning specifications.

Per-Silo partitioned indexes provide grain and index state consistency automatically. In this scenario, a grain always updates the index on the same silo the grain is activated on, so the unit of failure is the silo itself. Per-Silo indexes are not persisted, because everything remains in RAM and consistent unless the silo goes down. However, other than fanning out for queries, cross-silo consistency is not checked by design, and thus we do not allow Total or Unique indexes to be partitioned Per-Silo.

For Per-Key-Hash indexes, the index may reside on a silo other than the one the grain is activated on. For these indexes, the index state is persisted. For example, if an Active index bucket on one silo contains entries for grains that are spread out on all silos of a cluster, then if the silo containing the index goes down, the index bucket will be reactivated the next time it is referenced by the indexing code (via index operations on grains that map to that bucket). An index bucket is obtained by GetGrain() using an ID that is formed from the interface name, index name, and key hash value. When a crashed index bucket is again referenced by GetGrain on its ID, it will be reactivated and will load its state, which for this example includes all grains that were active at the time it crashed. Those active grains that were on the same silo as the index bucket when it crashed are therefore still available for querying; they will be reactivated when an operation is performed on their GrainReferences.

We ensure data consistency between grain and index state by using a fault-tolerant workflow or by using Transactions.

For Fault-tolerant workflow indexes, we provide "eventual consistency". Fault-tolerant indexes store the IDs of in-flight workflows along with the grain's state, and are always Lazy. When the queue executes an index update, it first obtains the set of active workflow Ids from the grain (which in turn retrieves it from the `IIndexedState` implementation), and executes the update if its workflow ID is in that set.

Non-fault-tolerant indexes do not provide the above guarantees. For these indexes, the write operations are essentially parallel executions of the task set {[write indexes], write grain state}, where [write indexes] depends on Eager vs. Lazy; for Eager it is [write to index hash buckets], and for Lazy it is [write to index workflow queues]. Thus, it is possible for inconsistencies to result from failures during these tasks.

Transactional indexes become part of any existing [Orleans Transaction](http://dotnet.github.io/orleans/Documentation/grains/transactions.html), or create one if none exists. 
### <a name="active-vs-total-index-implementations"></a>Active vs. Total Index Implementations
These indexes operate very similarly, and it likely would have been possible to implement the distinction via an IndexAttribute parameter rather than separate classes. However, the current API does enforce the constraint against Total indexes being partitioned per-silo in a straightforward way: there is no way to specify such an index (the TotalIndexType does not include PartitionedPerSilo).

In the code, the main difference is that Active indexes are updated on grain activation (when the grain's entries are added to the index buckets) and deactivation (when the grain's entries are removed from the index buckets). This is not done for a Total index, because when we deactivate a grain, by definition we do not remove its entries from Total indexes--and thus, when we activate a grain, its Total index entries are already present if the grain was previously activated, and if not, then there is no non-default state and thus no index entries should be created. (This correctness is ensured by prohibiting per-silo partitioning of Total indexes.)
## Constraints on Indexing
### Incompatible definitions
Some index definitions are not compatible with certain partitioning specifications or Active vs. Total indexing.
#### Total Indexes Cannot be Partitioned Per-Silo
The indexing paper describes this limitation in detail in section 5.2: Per-Silo (physically partitioned) indexes would have to do fan-out operations to update the status of an index entry on a grain, because the grain may have previously been active on a different silo. The API does not contain definitions that allow such an index to be specified. There appears to be no need to implement this.
#### Unique Indexes Cannot Be Active
An Active Index cannot be defined as unique for the following reason:
1. Suppose there's a unique Active Index over persistent objects.
2. The activation of an initialized object could create a conflict in the Active Index.
   E.g., there's an active player PA with email foo and a non-active persistent player PP with email foo.
3. An attempt to activate PP will cause a violation of the Active Index on email.
   In other words, having a Total unique index prevents the possibility of such a conflict; having an Active unique index does not, because one could activate a Grain, set its email to something already there and persist it (and then deactivate it and activate a new one, etc.). The only use case would be "only one such value can be active at a time", but this would lead to more issues than gain. This implies we should disallow such indexes.
#### Unique Indexes Cannot Be Partitioned Per-Silo
As with Total indexes, Unique Indexes partitioned per Silo (physically) would require fan-out operations to all silos to ensure that the indexed property value is unique. This is currently not implemented.
#### Active Indexes Must be Partitioned Per Silo
An Active index can only be partitioned per silo because they cannot be part of a Workflow fault tolerant or Transactional consistency scheme. For active indexes, the silo functions as the single unit of failure; all indexes on the silo disappear along with any active grains if the silo crashes. Note that queries on indexes that are partitioned per silo will fan out to all silos.
#### Active Indexes Cannot Be Workflow Fault Tolerant
Fault-Tolerant Indexes process index activation and deactivation in the workflow queues. When the fault-tolerant infrastructure processes a workflow, it retrieves the grain's list of active workflow IDs. If the workflow is due to a grain deactivation, then retrieving the active workflow IDs will cause the grain to be falsely reactivated, in effect preventing grain deactivation.
#### Active Indexes Cannot Be Transactional
An Active index cannot be Transactional because the grain is not yet fully initialized during activation. 
#### Fault-Tolerant Indexes Cannot Be Eager
Fault-Tolerant Indexes are based on the workflow queues, so they cannot be Eager.
Transactional indexes do not use the workflow queues, so they cannot be Lazy.
#### Cannot Define Both Eager And Lazy Indexes on a Single Grain
Allowing both Eager and Lazy indexes on a single grain would lead to potential difficulties in ensuring correctness.
### Only One Indexing Consistency Scheme (FT, NFT, TRX) per Grain
Orleans only supports a single indexing consistency scheme per Grain class: fault-tolerant or non-fault-tolerant workflow, or transactional. This is reflected in the presence of only a single `IIndexedState` facet parameter.
### <a name="only-one-index-per-query-"></a>Only One Index per Query (==)
The Orleans query syntax currently allows only a single equivalence condition, e.g.: where team.Name == "Seahawks". Following are some specific scenarios that are not supported, together with workarounds where possible.
#### No Compound Indexes
Orleans currently does not have a syntax on property annotations (or property classes) to support indexes across multiple properties, such as specifying an index on League and then on Name. The [SportsTeamIndexing](#sportsteamindexing-sample) sample illustrates how this can be simulated at the application level by defining a computed property (QualifiedName) on the properties class. Note that the class also exposes static methods to compose the component property strings into a single compound string, and to decompose the compound string into its component parts. This ensures consistency and hides the implementation details from the consumer of the class.
#### <a name="no-conjunctions-"></a>No Conjunctions (&&)
Orleans currently does not support intersecting multiple index-query `GrainReference` result sets, such as: where team.Location == "New York, NY" && team.League == "NFL". The [SportsTeamIndexing](#sportsteamindexing-sample) sample illustrates how this can be done at the application level by intersecting HashSets.
#### <a name="no-disjunctions-"></a>No Disjunctions (||)
Orleans currently does not support unioning multiple index-query `GrainReference` result sets, such as: where team.League == "WNBA" || team.League == "NWSL". The [SportsTeamIndexing](#sportsteamindexing-sample) sample illustrates how this can be done at the application level by unioning the result sets.
#### <a name="no-negations-"></a>No Negations (!=)
Orleans currently does not support returning all Grains that do not match a predicate, such as: where team.League != "MLB". There is no workaround for this.
### <a name="no-range-indexes---etc"></a>No Range Indexes (>, <, etc.)
Orleans currently does not support returning all Grains in a range, such as: where player.Level >= 5. There is no workaround for this.

## Possible Extensions to Current Design Proposal:
### Compound Indexes
As noted above, the application can hack together its own implementation of computed properties to provide indexes on more than one property. This would be much more convenient if supplied by Orleans.Indexing. To do so, Orleans would have to define the syntax of the annotations, provide the encoding on write, process the LINQ expression tree to obtain the requested properties, and determine which of the available indexes to apply.
### <a name="index-conjunctions-"></a>Index Conjunctions (&&)
As noted above, the application can intersect the returned `GrainReference`s from multiple index queries. Again, this would be much more convenient if done in Orleans.Indexing. To do so, Orleans would have to process the LINQ expression tree to obtain the requested properties, issue multiple index lookups, and intersect the results.
### <a name="index-disjunctions-"></a>Index Disjunctions (||)
As noted above, the application can union the returned `GrainReference`s from multiple index queries. Again, this would be much more convenient if done in Orleans.Indexing. To do so, Orleans would have to process the LINQ expression tree to obtain the requested properties, issue multiple index lookups, and union the results.
### Negations and Ranges
These cannot be done using the current hash-based approach.
### Adding Explicit TState-to-TProperties Name Mapping
Currently, if `TGrainState` does not inherit from `TProperties`, Indexing maps them by assuming the same property names. We could add an explicit mapping, either by attributes on `TState` and/or `TProperties`, by passing a Dictionary to IIndexedState.Write(), or by some other mapping specification.
### Unique Indexes Partitioned Per-Silo
As stated above, Unique Indexes partitioned per Silo (physically) would require fan-out operations to all silos to ensure that the indexed property value is unique. It is not clear how useful this would be.
### Clean Up LookupAsync for DSMI
Currently the DSMI indexes dynamically invoke a LookupAsync method as described [above](#direct-storage-managed-indexes-dsmi). It would be cleaner to define an IOrleansIndexingStorageProvider interface.
### Default IIndexableGrain implementations
`IIndexableGrain` contains methods that are relevant only for Workflow Fault-Tolerant indexes. When C# 8.0 becomes available, these should be given a default implementation.
