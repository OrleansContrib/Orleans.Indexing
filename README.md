# Orleans.Indexing

> THIS PROJECT IS NOT READY FOR PRODUCTION USE. It has undergone a modest amount of testing and code review. It is published to collect community feedback and attract others to make it production-ready. 

A research paper describing the interface and implementation can be found [here](http://cidrdb.org/cidr2017/papers/p29-bernstein-cidr17.pdf).

[IndexingFacet.md](/src/Orleans.Indexing/IndexingFacet.md) is a user's guide to Orleans Indexing, and the internal architecture is described in [IndexingInternals.md](/src/Orleans.Indexing/IndexingInternals.md).

## Features

- Index all grains of a class 
- Fault-tolerant multi-step workflow for index update

### Configuration Options

- Store an index as a single grain
- Partition an index with a bucket for each key value
- Index only the activated grains of a class
- Physically paritition an index over activated grains, so grains and their index are on the same silo
- Allow index to have very large buckets, to handle highly-skewed distribution of values

## Source Code

See [src/Orleans.Indexing](/src/Orleans.Indexing) (this directory) and [test/Orleans.Indexing.Tests](/test/Orleans.Indexing.Tests).

To build, run the `Build.cmd` or open `src\Orleans.sln`.

## Example Usage

In the following simple example usage, we assume that we are indexing the location of players in a game and we want to query the players based on their location. First, we describe the steps for defining an index on a grain. Then, we explain the steps for using an index in the queries.

### Defining an index

All the indexable properties of a grain should be defined in a properties class. The type of index is declared by adding annotations on the indexed property. Currently, three index annotations are available:
- [ActiveIndex](/src/Orleans.Indexing/Core/Annotations/ActiveIndexAttribute.cs) (all grains that are currently active in the silos)
- [TotalIndex](/src/Orleans.Indexing/Core/Annotations/TotalIndexAttribute.cs) (all grains that have been created during the lifetime of the application)
- [StorageManagedIndex](/src/Orleans.Indexing/Core/Annotations/StorageManagedIndexAttribute.cs) (managed directly via storage; i.e. no caching)

In this example, we want to index the location of all the players that are currently active.
- IPlayerGrain contains all properties of a player.
- PlayerProperties contains the only indexed properties of a player, which in this case is only its Location.

```c#
    public class PlayerProperties
    {
        [ActiveIndex]
        string Location { get; set; }
    }
```

The grain interface for the player should implement the `IIndexableGrain<PlayerProperties>` interface. This is a marker interface that declares the IPlayerGrain as an indexed grain interface where its indexed properties are defined in the PlayerProperties class. As with all grain methods, getting and setting property values is done via Task-returning methods.

```c#
    public interface IPlayerGrain : IGrainWithIntegerKey, IIndexableGrain<PlayerProperties>
    {
        Task<string> GetLocation();
        Task SetLocation(string location);
    }
```

The grain must contain state that is the backing storage for properties values and will be written to persistent storage; thus, PlayerState (and not PlayerProperties) must have the [Serializable] attribute. The indexing implementation will handle mapping of PlayerProperties to PlayerState when persisting the index. In most cases, PlayerState will simply inherit from PlayerProperties; if this is not the case, direct name mapping is used. 
```c#
    [Serializable]
    public class PlayerState : PlayerProperties
    {
        // Other, non-indexed properties may be here
    }
```

The grain implementation for the player should extend the Grain<TWrappedPlayerState> class, where TWrappedPlayerState is either IndexableGrainStateWrapper or FaultTolerantIndexableGrainStateWrapper. Additionally, it must provide a constructor that accepts one "facet" parameter that defines the indexing to use, and this definition must match the TWrappedPlayerProperties specification. Note that the properties class (in this case,PlayerProperties) is not specified on the Grain definition; the IIndexWriter implementation has already been populated with this information via reflection on the class' IIndexableGrain<> implementations. Lastly, it must provide some interface implementations as simple redirectors.

```c#
    public class PlayerGrain : IndexableGrain<FaultTolerantIndexableGrainStateWrapper<PlayerState>>, IPlayerGrain
    {
        IIndexWriter<PlayerState> indexWriter;

        public PlayerGrain(
            [FaultTolerantWorkflowIndexWriter]
            IIndexWriter<PlayerState> indexWriter) => this.indexWriter = indexWriter;

        public string Location { get { return State.Location; } }

        public Task<string> GetLocation()
        {
            return Task.FromResult(Location);
        }

        public async Task SetLocation(string location)
        {
            State.Location = location;
            // the call to base.WriteStateAsync() determines
            // when the changes to the grain are applied to its
            // corresponding indexes.
            await base.WriteStateAsync();
        }

        #region Facet methods - required overrides of Grain<TGrainState>
        public override Task OnActivateAsync() => this.indexWriter.OnActivateAsync(this, base.State, () => base.WriteStateAsync(), () => Task.CompletedTask);
        public override Task OnDeactivateAsync() => this.indexWriter.OnDeactivateAsync(() => Task.CompletedTask);
        protected override Task WriteStateAsync() => this.indexWriter.WriteAsync();
        #endregion Facet methods - required overrides of Grain<TGrainState>

        #region Required implementations of IIndexableGrain methods; they are only called for fault-tolerant index writing
        public Task<Immutable<System.Collections.Generic.HashSet<Guid>>> GetActiveWorkflowIdsSet() => this.indexWriter.GetActiveWorkflowIdsSet();
        public Task RemoveFromActiveWorkflowIds(System.Collections.Generic.HashSet<Guid> removedWorkflowId) => this.indexWriter.RemoveFromActiveWorkflowIds(removedWorkflowId);
        #endregion required implementations of IIndexableGrain methods    }
```

### Using an index to query the grains

The code below queries all the players based on their locations and prints the information related to all the player grains that are located in Zurich.

```c#
var q = from player in GrainClient.GrainFactory.GetActiveGrains<IPlayerGrain, PlayerProperties>()
        where player.Location == "Zurich"
        select player;
        
q.ObserveResults(new QueryResultStreamObserver<IPlayerGrain>(async entry =>
{
    output.WriteLine("primary key = {0}, location = {1}", entry.GetPrimaryKeyLong(), await entry.GetLocation());
}));
```

See [test/Orleans.Indexing.Tests/IndexingTestUtils.cs](/test/Orleans.Indexing.Tests/IndexingTestUtils.cs) for a detailed example in test code (CountItemsStreamingIn).

### Testing examples

For detailed implementation examples, please have a look at [test/Orleans.Indexing.Tests](/test/Orleans.Indexing.Tests).

#### *Player\** tests
The *Player* series of tests focuses primarily on indexing Player Location (and occasionally Score).
- Interfaces and classes are defined in the [test/Orleans.Indexing.Tests/Grains/Players](/test/Orleans.Indexing.Tests/Grains/Players) subdirectory.
- Test runners are defined in the [test/Orleans.Indexing.Tests/Runners/Players](/test/Orleans.Indexing.Tests/Runners/Players) subdirectory.

These use a number X for a grouping of tests, with the following form:
- `IPlayerProperties` defines the properties of a player that may be indexed: Location and Score.
- `IPlayerState` defines an additional non-indexed property: Email.
- `IPlayerGrain` defines all properties of a player, expressed in paired async Get/Set properties returning Tasks.
  - For these tests, players have three properties: Location and Score may be indexed, and Email is not indexed.
- `PlayerXProperties : IPlayerProperties` defines the indexed properties of the player.
- `IPlayerXGrain : IPlayerGrain, IIndexableGrain<PlayerXProperties>` defines the interface for the player implementation.
- `PlayerGrain<TState, TProps> : IndexableGrain<TState, TProps>, IPlayerGrain where TState : IPlayerState where TProps : new()` is the base class implementing common functionality for all player grains:
  - TState must be IPlayerState or a subclass
  - TProps must be a class (and should derive from IPlayerProperties)
- `PlayerXGrain : PlayerGrain<PlayerXGrainState, PlayerXProperties>, IPlayerXGrain` defines the implementing class for PlayerX.

#### *MultiIndex\** tests
The *MultiIndex\** series of tests is separate from the *Player* series. This series of tests focuses on multiple indexes per grain.
- Base State, Property, and Grain interfaces are defined in the [test/Orleans.Indexing.Tests/Grains/MultiIndex](/test/Orleans.Indexing.Tests/Grains/MultiIndex) subdirectory.
- Test runners are also defined in the [test/Orleans.Indexing.Tests/Runners/MultiIndex](/test/Orleans.Indexing.Tests/Runners/MultiIndex) subdirectory.
  - Each file contains the state, property, interface, and grain implementation definitions, as defined by the file name.
  - [test/Orleans.Indexing.Tests/Grains/MultiIndex/ITestMultiIndexProperties.cs](/test/Orleans.Indexing.Tests/Grains/MultiIndex/ITestMultiIndexProperties.cs) describes the abbreviations used in the file and test names.
    - For example, MultiIndex_AI_EG_Runner defines all interfaces, classes, and tests to implement testing for Eager Active indexes.
  - Testing includes unique and nonunique indexes on string and int. Additional combinations are TBD.

#### *MultiInterface\** tests
The *MultiInterface\** series of tests focuses on multiple indexed interfaces, each with one or more indexed properties, per grain. The multi-interface capability was introduced along with the Facet implementation. The tests are organized similarly to the *MultiIndex\** series:
- Base State, Property, and Grain interfaces are defined in the [test/Orleans.Indexing.Tests/Grains/MultiInterface](/test/Orleans.Indexing.Tests/Grains/MultiInterface) subdirectory.
- Test runners are also defined in the [test/Orleans.Indexing.Tests/Runners/MultiInterface](/test/Orleans.Indexing.Tests/Runners/MultiInterface) subdirectory.
  - Each file contains the state, property, interface, and grain implementation definitions, as defined by the file name.
  - [test/Orleans.Indexing.Tests/Grains/MultiIndex/ITestMultiIndexProperties.cs](/test/Orleans.Indexing.Tests/Grains/MultiIndex/ITestMultiIndexProperties.cs) describes the abbreviations used in the file and test names (except for those related to property names; MultiInterface uses the properties interface name instead), but MultiInterface does not otherwise use ITestIndexProperties.
    - For example, MultiInterface_AI_EG_Runner defines all interfaces, classes, and tests to implement testing for Eager Active indexes.
  - Testing uses IPersonGrain, IJobGrain, and IEmployeeGrain indexed interfaces on an Employee grain.

  ## To Do

- Improve interface for Direct Storage Managed Indexes (see IOrleansIndexingStorageProvider in [IndexingFacet.md](/src/Orleans.Indexing/IndexingFacet.md))
- Range indexes

## License

- This project is licensed under the [MIT license](https://github.com/dotnet/orleans/blob/master/LICENSE).
