using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Orleans.Concurrency;
using Orleans.Indexing.Facet;
using Xunit;
using Xunit.Abstractions;

namespace Orleans.Indexing.Tests.SharedGrainInterfaces
{
    // "Shared" means the interface is present on more than one grain class. However, as for multiple
    // occurrences of a single grain class, the values of property getting and setting are per-grain.
    public interface IProperties1 { int Int1 { get; set; } }
    public interface IPropertiesShared { int IntShared { get; set; } }
    public interface IProperties2 { int Int2 { get; set; } }

    public class Properties1 : IProperties1
    {
        [ActiveIndex(IsEager = true, IsUnique = false, NullValue = "-1")]
        public int Int1 { get; set; }
    }

    public class PropertiesShared : IPropertiesShared
    {
        [ActiveIndex(IsEager = true, IsUnique = false, NullValue = "-1")]
        public int IntShared { get; set; }
    }

    public class Properties2 : IProperties2
    {
        [ActiveIndex(IsEager = true, IsUnique = false, NullValue = "-1")]
        public int Int2 { get; set; }
    }

    public class GrainState : IProperties1, IPropertiesShared, IProperties2
    {
        public int Int1 { get; set; }
        public int IntShared { get; set; }
        public int Int2 { get; set; }
    }

    public interface IGrainInterface1 : IIndexableGrain<Properties1>, IGrainWithIntegerKey
    {
        Task<int> Get1();
        Task Set1(int value);
    }
    public interface IGrainInterfaceShared : IIndexableGrain<PropertiesShared>, IGrainWithIntegerKey
    {
        Task<int> GetShared();
        Task SetShared(int value);
    }
    public interface IGrainInterface2 : IIndexableGrain<Properties2>, IGrainWithIntegerKey
    {
        Task<int> Get2();
        Task Set2(int value);
    }

    public class GrainShared1 : GrainBase, IGrainInterface1, IGrainInterfaceShared
    {
        public GrainShared1(
            [NonFaultTolerantWorkflowIndexedState(IndexingConstants.IndexedGrainStateName, IndexingConstants.MEMORY_STORAGE_PROVIDER_NAME)]
            IIndexedState<GrainState> indexedState)
            : base(indexedState) { }
    }

    public class GrainShared2 : GrainBase, IGrainInterface2, IGrainInterfaceShared
    {
        public GrainShared2(
            [NonFaultTolerantWorkflowIndexedState(IndexingConstants.IndexedGrainStateName, IndexingConstants.MEMORY_STORAGE_PROVIDER_NAME)]
            IIndexedState<GrainState> indexedState)
            : base(indexedState) { }
    }

    public abstract class GrainBase : Grain
    {
        private protected IIndexedState<GrainState> indexedState;

        public GrainBase(IIndexedState<GrainState> indexedState) => this.indexedState = indexedState;

        public Task<int> Get1() => this.indexedState.PerformRead(state => state.Int1);
        public Task Set1(int value) => this.indexedState.PerformUpdate(state => state.Int1 = value);

        public Task<int> GetShared() => this.indexedState.PerformRead(state => state.IntShared);
        public Task SetShared(int value) => this.indexedState.PerformUpdate(state => state.IntShared = value);

        public Task<int> Get2() => this.indexedState.PerformRead(state => state.Int2);
        public Task Set2(int value) => this.indexedState.PerformUpdate(state => state.Int2 = value);

        #region FT not used
        public Task<Immutable<HashSet<Guid>>> GetActiveWorkflowIdsSet() => throw new NotImplementedException();
        public Task RemoveFromActiveWorkflowIds(HashSet<Guid> removedWorkflowId) => throw new NotImplementedException();
        #endregion FT not used
    }

    public abstract class SharedGrainInterfaceRunner : IndexingTestRunnerBase
    {
        protected SharedGrainInterfaceRunner(BaseIndexingFixture fixture, ITestOutputHelper output)
            : base(fixture, output)
        {
        }

        [Fact, TestCategory("BVT"), TestCategory("Indexing")]
        public async Task Test_SharedGrain_Interfaces()
        {
            var grain11 = base.GetGrain<IGrainInterface1>(11);
            var grain12 = base.GetGrain<IGrainInterface1>(12);
            await grain11.Set1(100);
            await grain12.Set1(100);

            var grainShared11 = base.GetGrain<IGrainInterfaceShared, GrainShared1>(1011);
            var grainShared12 = base.GetGrain<IGrainInterfaceShared, GrainShared1>(1012);
            var grainShared13 = base.GetGrain<IGrainInterfaceShared, GrainShared1>(1013);
            await grainShared11.SetShared(150);
            await grainShared12.SetShared(150);
            await grainShared13.SetShared(150);

            var grainShared21 = base.GetGrain<IGrainInterfaceShared, GrainShared2>(1021);
            var grainShared22 = base.GetGrain<IGrainInterfaceShared, GrainShared2>(1022);
            var grainShared23 = base.GetGrain<IGrainInterfaceShared, GrainShared2>(1023);
            await grainShared21.SetShared(150);
            await grainShared22.SetShared(150);
            await grainShared23.SetShared(150);

            var grain21 = base.GetGrain<IGrainInterface2>(21);
            var grain22 = base.GetGrain<IGrainInterface2>(22);
            var grain23 = base.GetGrain<IGrainInterface2>(23);
            var grain24 = base.GetGrain<IGrainInterface2>(24);
            await grain21.Set2(200);
            await grain22.Set2(200);
            await grain23.Set2(200);
            await grain24.Set2(200);

            IOrleansQueryable<TIGrain, TProperties> queryActiveGrains<TIGrain, TProperties>() where TIGrain : IIndexableGrain
                => base.IndexFactory.GetActiveGrains<TIGrain, TProperties>();

            async Task<int> getCount1(int queryValue)
                => (await(from item in queryActiveGrains<IGrainInterface1, Properties1>() where item.Int1 == queryValue select item).GetResults()).Count();
            async Task<int> getCountShared(int queryValue)
                => (await (from item in queryActiveGrains<IGrainInterfaceShared, PropertiesShared>() where item.IntShared == queryValue select item).GetResults()).Count();
            async Task<int> getCount2(int queryValue)
                => (await (from item in queryActiveGrains<IGrainInterface2, Properties2>() where item.Int2 == queryValue select item).GetResults()).Count();

            Assert.Equal(2, await getCount1(100));
            Assert.Equal(6, await getCountShared(150));
            Assert.Equal(4, await getCount2(200));

            // Cast to the non-Shared interface and set, then verify counts.
            async void castTo1AndSet(IGrainInterfaceShared grain, int value) => await grain.Cast<IGrainInterface1>().Set1(value);
            async void castTo2AndSet(IGrainInterfaceShared grain, int value) => await grain.Cast<IGrainInterface2>().Set2(value);
            async void castToSharedFrom1AndSet(IGrainInterface1 grain, int value) => await grain.Cast<IGrainInterfaceShared>().SetShared(value);
            async void castToSharedFrom2AndSet(IGrainInterface2 grain, int value) => await grain.Cast<IGrainInterfaceShared>().SetShared(value);

            castTo1AndSet(grainShared11, 100);
            castTo1AndSet(grainShared12, 100);
            castTo1AndSet(grainShared13, 100);

            castTo2AndSet(grainShared21, 200);
            castTo2AndSet(grainShared22, 200);
            castTo2AndSet(grainShared23, 200);

            castToSharedFrom1AndSet(grain11, 150);
            castToSharedFrom1AndSet(grain12, 150);

            castToSharedFrom2AndSet(grain21, 150);
            castToSharedFrom2AndSet(grain22, 150);
            castToSharedFrom2AndSet(grain23, 150);
            castToSharedFrom2AndSet(grain24, 150);

            Assert.Equal(5, await getCount1(100));
            Assert.Equal(12, await getCountShared(150));
            Assert.Equal(7, await getCount2(200));

            //
            // Get the non-Shared interface using the Shared interface's ID.
            //
            var grain1FromShared = base.GetGrain<IGrainInterface1>(1011);
            await grain1FromShared.Set1(101);
            var grain2FromShared = base.GetGrain<IGrainInterface2>(1021);
            await grain2FromShared.Set2(201);

            Assert.Equal(4, await getCount1(100));
            Assert.Equal(1, await getCount1(101));
            Assert.Equal(12, await getCountShared(150));
            Assert.Equal(6, await getCount2(200));
            Assert.Equal(1, await getCount2(201));

            // Verify
            var grainSharedFrom11 = base.GetGrain<IGrainInterfaceShared, GrainShared1>(1011);
            Assert.Equal(101, await grainSharedFrom11.Cast<IGrainInterface1>().Get1());
            var grainSharedFrom21 = base.GetGrain<IGrainInterfaceShared, GrainShared2>(1021);
            Assert.Equal(201, await grainSharedFrom21.Cast<IGrainInterface2>().Get2());

            //
            // Get the non-Shared interface using the Shared interface's ID.
            //
            var grainSharedFrom12 = base.GetGrain<IGrainInterfaceShared, GrainShared1>(1012);
            await grainSharedFrom12.SetShared(151);
            var grainSharedFrom22 = base.GetGrain<IGrainInterfaceShared, GrainShared2>(1022);
            await grainSharedFrom22.SetShared(151);

            Assert.Equal(4, await getCount1(100));
            Assert.Equal(1, await getCount1(101));
            Assert.Equal(10, await getCountShared(150));
            Assert.Equal(2, await getCountShared(151));
            Assert.Equal(6, await getCount2(200));
            Assert.Equal(1, await getCount2(201));

            // Verify
            var grain12FromShared = base.GetGrain<IGrainInterface1>(1012);
            Assert.Equal(151, await grain12FromShared.Cast<IGrainInterfaceShared>().GetShared());
            var grain22FromShared = base.GetGrain<IGrainInterface2>(1022);
            Assert.Equal(151, await grain22FromShared.Cast<IGrainInterfaceShared>().GetShared());
        }
    }
}
