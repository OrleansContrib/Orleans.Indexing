using Xunit.Abstractions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Orleans.TestingHost;
using Orleans.Indexing.Tests.MultiInterface;
using System.Threading.Tasks;
using System.Linq;
using System;
using Xunit;

namespace Orleans.Indexing.Tests
{
    using ITC = IndexingTestConstants;

    public class IndexingTestRunnerBase
    {
        private BaseIndexingFixture fixture;

        internal readonly ITestOutputHelper Output;
        internal IClusterClient ClusterClient => this.fixture.Client;

        internal IGrainFactory GrainFactory => this.fixture.GrainFactory;

        internal IIndexFactory IndexFactory { get; }

        internal ILoggerFactory LoggerFactory { get; }

        protected TestCluster HostedCluster => this.fixture.HostedCluster;

        protected IndexingTestRunnerBase(BaseIndexingFixture fixture, ITestOutputHelper output)
        {
            this.fixture = fixture;
            this.Output = output;
            this.LoggerFactory = this.ClusterClient.ServiceProvider.GetRequiredService<ILoggerFactory>();
            this.IndexFactory = this.ClusterClient.ServiceProvider.GetRequiredService<IIndexFactory>();
        }

        protected TInterface GetGrain<TInterface>(long primaryKey) where TInterface : IGrainWithIntegerKey
            => this.GrainFactory.GetGrain<TInterface>(primaryKey);

        protected TInterface GetGrain<TInterface, TImplClass>(long primaryKey) where TInterface : IGrainWithIntegerKey
            => this.GetGrain<TInterface>(primaryKey, typeof(TImplClass));

        protected TInterface GetGrain<TInterface>(long primaryKey, Type grainImplType) where TInterface : IGrainWithIntegerKey
            => this.GrainFactory.GetGrain<TInterface>(primaryKey, grainImplType.FullName.Replace("+", "."));

        protected IIndexInterface<TKey, TValue> GetIndex<TKey, TValue>(string indexName) where TValue : IIndexableGrain
            => this.IndexFactory.GetIndex<TKey, TValue>(indexName);

        protected async Task<IIndexInterface<TKey, TValue>> GetAndWaitForIndex<TKey, TValue>(string indexName) where TValue : IIndexableGrain
        {
            var index = this.IndexFactory.GetIndex<TKey, TValue>(IndexUtils.PropertyNameToIndexName(indexName));
            while (!await index.IsAvailable())
            {
                await Task.Delay(50);
            }
            return index;
        }

        protected async Task<IIndexInterface<TKey, TValue>[]> GetAndWaitForIndexes<TKey, TValue>(params string[] propertyNames) where TValue : IIndexableGrain
        {
            var indexes = propertyNames.Select(name => this.IndexFactory.GetIndex<TKey, TValue>(IndexUtils.PropertyNameToIndexName(name))).ToArray();

            const int MaxRetries = 100;
            int retries = 0;
            foreach (var index in indexes)
            {
                while (!await index.IsAvailable())
                {
                    ++retries;
                    Assert.True(retries < MaxRetries, "Maximum number of GetAndWaitForIndexes retries was exceeded");
                    await Task.Delay(50);
                }
            }
            return indexes;
        }

        internal async Task TestIndexesWithDeactivations<TIGrain, TProperties>(int intAdjustBase = 0)
            where TIGrain : ITestMultiIndexGrain, IIndexableGrain where TProperties : ITestMultiIndexProperties
        {
            using (var tw = new TestConsoleOutputWriter(this.Output, $"start test: TIGrain = {nameof(TIGrain)}, TProperties = {nameof(TProperties)}"))
            {
                // Use intAdjust to test that different values for the same grain type are handled correctly; see MultiIndex_All.
                var intAdjust = intAdjustBase * 1000000;
                var adj1 = intAdjust + 1;
                var adj11 = intAdjust + 11;
                var adj111 = intAdjust + 111;
                var adj1111 = intAdjust + 1111;
                var adj2 = intAdjust + 2;
                var adj3 = intAdjust + 3;
                var adj4 = intAdjust + 4;
                var adj1000 = intAdjust + 1000;
                var adj2000 = intAdjust + 2000;
                var adj3000 = intAdjust + 3000;
                var adj4000 = intAdjust + 4000;
                var adjOne = "one" + intAdjust;
                var adjEleven = "eleven" + intAdjust;
                var adjOneEleven = "oneeleven" + intAdjust;
                var adjElevenEleven = "eleveneleven" + intAdjust;
                var adjTwo = "two" + intAdjust;
                var adjThree = "three" + intAdjust;
                var adjFour = "four" + intAdjust;
                var adj1k = "1k" + intAdjust;
                var adj2k = "2k" + intAdjust;
                var adj3k = "3k" + intAdjust;
                var adj4k = "4k" + intAdjust;
                const string unindexedString = "unindexed_";

                async Task<TIGrain> makeGrain(int uInt, string uString, int nuInt, string nuString)
                {
                    var grain = this.GetGrain<TIGrain>(GrainPkFromUniqueInt(uInt));
                    await grain.SetUniqueInt(uInt);
                    await grain.SetUniqueString(uString);
                    await grain.SetNonUniqueInt(nuInt);
                    await grain.SetNonUniqueString(nuString);
                    await grain.SetUnIndexedString(unindexedString + uString);
                    return grain;
                }
                var p1 = await makeGrain(adj1, adjOne, adj1000, adj1k);
                var p11 = await makeGrain(adj11, adjEleven, adj1000, adj1k);
                var p111 = await makeGrain(adj111, adjOneEleven, adj1000, adj1k);
                var p1111 = await makeGrain(adj1111, adjElevenEleven, adj1000, adj1k);
                var p2 = await makeGrain(adj2, adjTwo, adj2000, adj2k);
                var p3 = await makeGrain(adj3, adjThree, adj3000, adj3k);

                // UniqueInt and UniqueString are defined as Unique for non-PerSilo partitioning only; we do not test duplicates here.
                var intIndexes = await this.GetAndWaitForIndexes<int, TIGrain>(ITC.UniqueIntProperty, ITC.NonUniqueIntProperty);
                var isActiveUqInt = intIndexes[0].GetType().IsActiveIndex();
                var isActiveNonUqInt = intIndexes[1].GetType().IsActiveIndex();
                var stringIndexes = await this.GetAndWaitForIndexes<string, TIGrain>(ITC.UniqueStringProperty, ITC.NonUniqueStringProperty);
                var isActiveUqString = stringIndexes[0].GetType().IsActiveIndex();
                var isActiveNonUqString = stringIndexes[1].GetType().IsActiveIndex();

                Assert.Equal(1, await this.GetUniqueIntCount<TIGrain, TProperties>(adj1));
                Assert.Equal(1, await this.GetUniqueIntCount<TIGrain, TProperties>(adj11));
                Assert.Equal(1, await this.GetUniqueStringCount<TIGrain, TProperties>(adjOne));
                Assert.Equal(1, await this.GetUniqueStringCount<TIGrain, TProperties>(adjEleven));
                Assert.Equal(1, await this.GetUniqueIntCount<TIGrain, TProperties>(adj2));
                Assert.Equal(1, await this.GetUniqueIntCount<TIGrain, TProperties>(adj3));
                Assert.Equal(1, await this.GetUniqueStringCount<TIGrain, TProperties>(adjTwo));
                Assert.Equal(1, await this.GetUniqueStringCount<TIGrain, TProperties>(adjThree));
                Assert.Equal(1, await this.GetNonUniqueIntCount<TIGrain, TProperties>(adj2000));
                Assert.Equal(1, await this.GetNonUniqueIntCount<TIGrain, TProperties>(adj3000));
                Assert.Equal(1, await this.GetNonUniqueStringCount<TIGrain, TProperties>(adj2k));
                Assert.Equal(1, await this.GetNonUniqueStringCount<TIGrain, TProperties>(adj3k));

                async Task verifyCount(int expected1, int expected11, int expected1000)
                {
                    Assert.Equal(expected1, await this.GetUniqueIntCount<TIGrain, TProperties>(adj1));
                    Assert.Equal(expected1, await this.GetUniqueStringCount<TIGrain, TProperties>(adjOne));
                    Assert.Equal(isActiveUqInt ? expected11 : 1, await this.GetUniqueIntCount<TIGrain, TProperties>(adj11));
                    Assert.Equal(isActiveUqString ? expected11 : 1, await this.GetUniqueStringCount<TIGrain, TProperties>(adjEleven));
                    Assert.Equal(isActiveNonUqInt ? expected1000 : 4, await this.GetNonUniqueIntCount<TIGrain, TProperties>(adj1000));
                    Assert.Equal(isActiveNonUqString ? expected1000 : 4, await this.GetNonUniqueStringCount<TIGrain, TProperties>(adj1k));
                }

                Console.WriteLine("*** First Verify ***");
                await verifyCount(1, 1, 4);

                Console.WriteLine("*** First Deactivate ***");
                await p11.Deactivate();
                await Task.Delay(ITC.DelayUntilIndexesAreUpdatedLazily);

                Console.WriteLine("*** Second Verify ***");
                await verifyCount(1, 0, 3);

                Console.WriteLine("*** Second and Third Deactivate ***");
                await p111.Deactivate();
                await p1111.Deactivate();
                await Task.Delay(ITC.DelayUntilIndexesAreUpdatedLazily);

                Console.WriteLine("*** Third Verify ***");
                await verifyCount(1, 0, 1);

                Console.WriteLine("*** GetGrain ***");
                p11 = this.GetGrain<TIGrain>(p11.GetPrimaryKeyLong());
                Assert.Equal(adj1000, await p11.GetNonUniqueInt());
                Assert.Equal(unindexedString + adjEleven, await p11.GetUnIndexedString());

                Console.WriteLine("*** Fourth Verify ***");
                await verifyCount(1, 1, 2);

                Console.WriteLine("*** Fifth Verify ***");
                Assert.Equal(1, await this.GetUniqueIntCount<TIGrain, TProperties>(adj3));
                Assert.Equal(1, await this.GetUniqueStringCount<TIGrain, TProperties>(adjThree));
                Assert.Equal(1, await this.GetNonUniqueIntCount<TIGrain, TProperties>(adj3000));
                Assert.Equal(1, await this.GetNonUniqueStringCount<TIGrain, TProperties>(adj3k));

                Console.WriteLine("*** Update 3x to 4x ***");
                await p3.SetUniqueInt(adj4);
                await p3.SetUniqueString(adjFour);
                await p3.SetNonUniqueInt(adj4000);
                await p3.SetNonUniqueString(adj4k);

                Console.WriteLine("*** Sixth Verify ***");
                Assert.Equal(0, await this.GetUniqueIntCount<TIGrain, TProperties>(adj3));
                Assert.Equal(0, await this.GetUniqueStringCount<TIGrain, TProperties>(adjThree));
                Assert.Equal(0, await this.GetNonUniqueIntCount<TIGrain, TProperties>(adj3000));
                Assert.Equal(0, await this.GetNonUniqueStringCount<TIGrain, TProperties>(adj3k));
                Assert.Equal(1, await this.GetUniqueIntCount<TIGrain, TProperties>(adj4));
                Assert.Equal(1, await this.GetUniqueStringCount<TIGrain, TProperties>(adjFour));
                Assert.Equal(1, await this.GetNonUniqueIntCount<TIGrain, TProperties>(adj4000));
                Assert.Equal(1, await this.GetNonUniqueStringCount<TIGrain, TProperties>(adj4k));
            }
        }

        internal async Task TestIndexesWithDeactivationsTxn<TIGrain, TProperties>(int intAdjustBase = 0)
            where TIGrain : ITestMultiIndexGrainTransactional, IIndexableGrain where TProperties : ITestMultiIndexProperties
        {
            using (var tw = new TestConsoleOutputWriter(this.Output, $"start test: TIGrain = {nameof(TIGrain)}, TProperties = {nameof(TProperties)}"))
            {
                // Use intAdjust to test that different values for the same grain type are handled correctly; see MultiIndex_All.
                var intAdjust = intAdjustBase * 1000000;
                var adj1 = intAdjust + 1;
                var adj11 = intAdjust + 11;
                var adj111 = intAdjust + 111;
                var adj1111 = intAdjust + 1111;
                var adj2 = intAdjust + 2;
                var adj3 = intAdjust + 3;
                var adj4 = intAdjust + 4;
                var adj1000 = intAdjust + 1000;
                var adj2000 = intAdjust + 2000;
                var adj3000 = intAdjust + 3000;
                var adj4000 = intAdjust + 4000;
                var adjOne = "one" + intAdjust;
                var adjEleven = "eleven" + intAdjust;
                var adjOneEleven = "oneeleven" + intAdjust;
                var adjElevenEleven = "eleveneleven" + intAdjust;
                var adjTwo = "two" + intAdjust;
                var adjThree = "three" + intAdjust;
                var adjFour = "four" + intAdjust;
                var adj1k = "1k" + intAdjust;
                var adj2k = "2k" + intAdjust;
                var adj3k = "3k" + intAdjust;
                var adj4k = "4k" + intAdjust;
                const string unindexedString = "unindexed_";

                async Task<TIGrain> makeGrain(int uInt, string uString, int nuInt, string nuString)
                {
                    var grain = this.GetGrain<TIGrain>(GrainPkFromUniqueInt(uInt));
                    await grain.SetUniqueInt(uInt);
                    await grain.SetUniqueString(uString);
                    await grain.SetNonUniqueInt(nuInt);
                    await grain.SetNonUniqueString(nuString);
                    await grain.SetUnIndexedString(unindexedString + uString);
                    return grain;
                }
                var p1 = await makeGrain(adj1, adjOne, adj1000, adj1k);
                var p11 = await makeGrain(adj11, adjEleven, adj1000, adj1k);
                var p111 = await makeGrain(adj111, adjOneEleven, adj1000, adj1k);
                var p1111 = await makeGrain(adj1111, adjElevenEleven, adj1000, adj1k);
                var p2 = await makeGrain(adj2, adjTwo, adj2000, adj2k);
                var p3 = await makeGrain(adj3, adjThree, adj3000, adj3k);

                // UniqueInt and UniqueString are defined as Unique for non-PerSilo partitioning only; we do not test duplicates here.
                var intIndexes = await this.GetAndWaitForIndexes<int, TIGrain>(ITC.UniqueIntProperty, ITC.NonUniqueIntProperty);
                var isActiveUqInt = intIndexes[0].GetType().IsActiveIndex();
                var isActiveNonUqInt = intIndexes[1].GetType().IsActiveIndex();
                var stringIndexes = await this.GetAndWaitForIndexes<string, TIGrain>(ITC.UniqueStringProperty, ITC.NonUniqueStringProperty);
                var isActiveUqString = stringIndexes[0].GetType().IsActiveIndex();
                var isActiveNonUqString = stringIndexes[1].GetType().IsActiveIndex();

                Assert.Equal(1, await this.GetUniqueIntCountTxn<TIGrain, TProperties>(adj1));
                Assert.Equal(1, await this.GetUniqueIntCountTxn<TIGrain, TProperties>(adj11));
                Assert.Equal(1, await this.GetUniqueStringCountTxn<TIGrain, TProperties>(adjOne));
                Assert.Equal(1, await this.GetUniqueStringCountTxn<TIGrain, TProperties>(adjEleven));
                Assert.Equal(1, await this.GetUniqueIntCountTxn<TIGrain, TProperties>(adj2));
                Assert.Equal(1, await this.GetUniqueIntCountTxn<TIGrain, TProperties>(adj3));
                Assert.Equal(1, await this.GetUniqueStringCountTxn<TIGrain, TProperties>(adjTwo));
                Assert.Equal(1, await this.GetUniqueStringCountTxn<TIGrain, TProperties>(adjThree));
                Assert.Equal(1, await this.GetNonUniqueIntCountTxn<TIGrain, TProperties>(adj2000));
                Assert.Equal(1, await this.GetNonUniqueIntCountTxn<TIGrain, TProperties>(adj3000));
                Assert.Equal(1, await this.GetNonUniqueStringCountTxn<TIGrain, TProperties>(adj2k));
                Assert.Equal(1, await this.GetNonUniqueStringCountTxn<TIGrain, TProperties>(adj3k));

                async Task verifyCount(int expected1, int expected11, int expected1000)
                {
                    Assert.Equal(expected1, await this.GetUniqueIntCountTxn<TIGrain, TProperties>(adj1));
                    Assert.Equal(expected1, await this.GetUniqueStringCountTxn<TIGrain, TProperties>(adjOne));
                    Assert.Equal(isActiveUqInt ? expected11 : 1, await this.GetUniqueIntCountTxn<TIGrain, TProperties>(adj11));
                    Assert.Equal(isActiveUqString ? expected11 : 1, await this.GetUniqueStringCountTxn<TIGrain, TProperties>(adjEleven));
                    Assert.Equal(isActiveNonUqInt ? expected1000 : 4, await this.GetNonUniqueIntCountTxn<TIGrain, TProperties>(adj1000));
                    Assert.Equal(isActiveNonUqString ? expected1000 : 4, await this.GetNonUniqueStringCountTxn<TIGrain, TProperties>(adj1k));
                }

                Console.WriteLine("*** First Verify ***");
                await verifyCount(1, 1, 4);

                Console.WriteLine("*** First Deactivate ***");
                await p11.Deactivate();
                await Task.Delay(ITC.DelayUntilIndexesAreUpdatedLazily);

                Console.WriteLine("*** Second Verify ***");
                await verifyCount(1, 0, 3);

                Console.WriteLine("*** Second and Third Deactivate ***");
                await p111.Deactivate();
                await p1111.Deactivate();
                await Task.Delay(ITC.DelayUntilIndexesAreUpdatedLazily);

                Console.WriteLine("*** Third Verify ***");
                await verifyCount(1, 0, 1);

                Console.WriteLine("*** GetGrain ***");
                p11 = this.GetGrain<TIGrain>(p11.GetPrimaryKeyLong());
                Assert.Equal(adj1000, await p11.GetNonUniqueInt());
                Assert.Equal(unindexedString + adjEleven, await p11.GetUnIndexedString());

                Console.WriteLine("*** Fourth Verify ***");
                await verifyCount(1, 1, 2);

                Console.WriteLine("*** Fifth Verify ***");
                Assert.Equal(1, await this.GetUniqueIntCountTxn<TIGrain, TProperties>(adj3));
                Assert.Equal(1, await this.GetUniqueStringCountTxn<TIGrain, TProperties>(adjThree));
                Assert.Equal(1, await this.GetNonUniqueIntCountTxn<TIGrain, TProperties>(adj3000));
                Assert.Equal(1, await this.GetNonUniqueStringCountTxn<TIGrain, TProperties>(adj3k));

                Console.WriteLine("*** Update 3x to 4x ***");
                await p3.SetUniqueInt(adj4);
                await p3.SetUniqueString(adjFour);
                await p3.SetNonUniqueInt(adj4000);
                await p3.SetNonUniqueString(adj4k);

                Console.WriteLine("*** Sixth Verify ***");
                Assert.Equal(0, await this.GetUniqueIntCountTxn<TIGrain, TProperties>(adj3));
                Assert.Equal(0, await this.GetUniqueStringCountTxn<TIGrain, TProperties>(adjThree));
                Assert.Equal(0, await this.GetNonUniqueIntCountTxn<TIGrain, TProperties>(adj3000));
                Assert.Equal(0, await this.GetNonUniqueStringCountTxn<TIGrain, TProperties>(adj3k));
                Assert.Equal(1, await this.GetUniqueIntCountTxn<TIGrain, TProperties>(adj4));
                Assert.Equal(1, await this.GetUniqueStringCountTxn<TIGrain, TProperties>(adjFour));
                Assert.Equal(1, await this.GetNonUniqueIntCountTxn<TIGrain, TProperties>(adj4000));
                Assert.Equal(1, await this.GetNonUniqueStringCountTxn<TIGrain, TProperties>(adj4k));
            }
        }

        internal async Task TestEmployeeIndexesWithDeactivations<TIPersonGrain, TPersonProperties, TIJobGrain, TJobProperties, TIEmployeeGrain, TEmployeeProperties>(int intAdjustBase = 0)
            where TIPersonGrain : IIndexableGrain, IPersonGrain, IGrainWithIntegerKey
            where TPersonProperties: IPersonProperties
            where TIJobGrain : IIndexableGrain, IJobGrain, IGrainWithIntegerKey
            where TJobProperties : IJobProperties
            where TIEmployeeGrain : IIndexableGrain, IEmployeeGrain, IGrainWithIntegerKey
            where TEmployeeProperties : IEmployeeProperties
        {
            using (var tw = new TestConsoleOutputWriter(this.Output, $"start test: TIPersonGrain = {nameof(TIPersonGrain)}, TIJobGrain = {nameof(TIJobGrain)}, TIEmployeeGrain = {nameof(TIEmployeeGrain)}"))
            {
                // Use intAdjust to test that different values for the same grain type are handled correctly; see MultiInterface_All.
                const int grainIdBase = 1000000;
                var intAdjust = intAdjustBase * grainIdBase;
                var name1 = $"name_{intAdjust + 1}";
                var name11 = $"name__{intAdjust + 11}";
                var name111 = $"name__{intAdjust + 111}";
                var name1111 = $"name__{intAdjust + 1111}";
                var name2 = $"name_2_{intAdjust}";
                var name3 = $"name_3_{intAdjust}";
                var name4 = $"name_4_{intAdjust}";
                var age1 = intAdjust + 1;
                var age2 = intAdjust + 2;
                var age3 = intAdjust + 3;
                var age4 = intAdjust + 4;

                var title1 = $"title_{intAdjust + 1}";
                var title11 = $"title_{intAdjust + 11}";
                var title111 = $"title_{intAdjust + 111}";
                var title1111 = $"title_{intAdjust + 1111}";
                var title2 = $"title2_{intAdjust}";
                var title3 = $"title3_{intAdjust}";
                var title4 = $"title4_{intAdjust}";
                var dept1 = $"department_{intAdjust + 1}";
                var dept2 = $"department_{intAdjust + 2}";
                var dept3 = $"department_{intAdjust + 3}";
                var dept4 = $"department_{intAdjust + 4}";

                const int employeeIdBase = grainIdBase * 100;
                int id = intAdjust;
                async Task<(TIPersonGrain person, TIJobGrain job, IEmployeeGrain employee)> makeGrain(string name, int age, string title, string dept)
                {
                    var personGrain = this.GetGrain<TIPersonGrain>(GrainPkFromUniqueInt(++id));
                    var transactionalPersistence = personGrain as ITestTransactionalPersistence;
                    if (transactionalPersistence != null)
                    {
                        await transactionalPersistence.InitializeStateTxn();
                    }

                    await personGrain.SetName(name);
                    await personGrain.SetAge(age);
                    var jobGrain = personGrain.Cast<TIJobGrain>();
                    await jobGrain.SetTitle(title);
                    await jobGrain.SetDepartment(dept);

                    var employeeGrain = personGrain.Cast<TIEmployeeGrain>();
                    await employeeGrain.SetEmployeeId(id + employeeIdBase);
                    await employeeGrain.SetSalary(id);  // not indexed
                    await jobGrain.SetDepartment(dept);

                    Task writeGrainAsync()
                    {
                        var selector = id % 3;
                        if (transactionalPersistence != null)
                        {
                            return transactionalPersistence.WriteStateTxn();
                        }
                        return selector == 0
                            ? personGrain.WriteState()
                            : (selector == 1) ? jobGrain.WriteState() : employeeGrain.WriteState();
                    }
                    await writeGrainAsync();
                    return (personGrain, jobGrain, employeeGrain);
                }
                var p1 = await makeGrain(name1, age1, title1, dept1);
                var p11 = await makeGrain(name11, age1, title11, dept1);
                var p111 = await makeGrain(name111, age1, title111, dept1);
                var p1111 = await makeGrain(name1111, age1, title1111, dept1);
                var p2 = await makeGrain(name2, age2, title2, dept2);
                var p3 = await makeGrain(name3, age3, title3, dept3);

                // Name and Title are defined as Unique for non-PerSilo partitioning only; we do not test duplicates here.
                // Age and Department may have multiple entries; additionally, they may or may not be of a type that
                // is "Total" -- either TotalIndex or DSMI, in which case deactivations do not really deactivate them.
                var nameIndex = await this.GetAndWaitForIndex<string, TIPersonGrain>(ITC.NameProperty);
                var isActiveName = nameIndex.GetType().IsActiveIndex();
                var ageIndex = await this.GetAndWaitForIndex<int, TIPersonGrain>(ITC.AgeProperty);
                var isActiveAge = ageIndex.GetType().IsActiveIndex();
                var jobIndexes = await this.GetAndWaitForIndexes<string, TIJobGrain>(ITC.TitleProperty, ITC.DepartmentProperty);
                var isActiveTitle = jobIndexes[0].GetType().IsActiveIndex();
                var isActiveDept = jobIndexes[1].GetType().IsActiveIndex();
                var employeeIdIndex = await this.GetAndWaitForIndex<int, TIEmployeeGrain>(ITC.EmployeeIdProperty);
                var isActiveEmployeeId = employeeIdIndex.GetType().IsActiveIndex();

                Assert.Equal(1, await this.GetNameCount<TIPersonGrain, TPersonProperties>(name1));
                Assert.Equal(1, await this.GetNameCount<TIPersonGrain, TPersonProperties>(name11));
                Assert.Equal(1, await this.GetNameCount<TIPersonGrain, TPersonProperties>(name2));
                Assert.Equal(1, await this.GetNameCount<TIPersonGrain, TPersonProperties>(name3));
                Assert.Equal(1, await this.GetPersonAgeCount<TIPersonGrain, TPersonProperties>(age2));
                Assert.Equal(1, await this.GetPersonAgeCount<TIPersonGrain, TPersonProperties>(age3));

                Assert.Equal(1, await this.GetJobTitleCount<TIJobGrain, TJobProperties>(title1));
                Assert.Equal(1, await this.GetJobTitleCount<TIJobGrain, TJobProperties>(title11));
                Assert.Equal(1, await this.GetJobTitleCount<TIJobGrain, TJobProperties>(title2));
                Assert.Equal(1, await this.GetJobTitleCount<TIJobGrain, TJobProperties>(title3));
                Assert.Equal(1, await this.GetJobDepartmentCount<TIJobGrain, TJobProperties>(dept2));
                Assert.Equal(1, await this.GetJobDepartmentCount<TIJobGrain, TJobProperties>(dept3));

                async Task verifyCount(int expectedDups, int expected11, int expected111, int expected1111)
                {
                    // Verify the duplicated count as well as sanity-checking for some of the non-duplicated ones.
                    Assert.Equal(1, await this.GetNameCount<TIPersonGrain, TPersonProperties>(name1));
                    Assert.Equal(isActiveName ? expected11 : 1, await this.GetNameCount<TIPersonGrain, TPersonProperties>(name11));
                    Assert.Equal(isActiveName ? expected111 : 1, await this.GetNameCount<TIPersonGrain, TPersonProperties>(name111));
                    Assert.Equal(isActiveName ? expected1111 : 1, await this.GetNameCount<TIPersonGrain, TPersonProperties>(name1111));
                    Assert.Equal(1, await this.GetNameCount<TIPersonGrain, TPersonProperties>(name2));
                    Assert.Equal(isActiveAge ? expectedDups : 4, await this.GetPersonAgeCount<TIPersonGrain, TPersonProperties>(age1));
                    Assert.Equal(1, await this.GetPersonAgeCount<TIPersonGrain, TPersonProperties>(age2));

                    Assert.Equal(1, await this.GetJobTitleCount<TIJobGrain, TJobProperties>(title1));
                    Assert.Equal(isActiveTitle ? expected11 : 1, await this.GetJobTitleCount<TIJobGrain, TJobProperties>(title11));
                    Assert.Equal(isActiveTitle ? expected111 : 1, await this.GetJobTitleCount<TIJobGrain, TJobProperties>(title111));
                    Assert.Equal(isActiveTitle ? expected1111 : 1, await this.GetJobTitleCount<TIJobGrain, TJobProperties>(title1111));
                    Assert.Equal(1, await this.GetJobTitleCount<TIJobGrain, TJobProperties>(title2));
                    Assert.Equal(isActiveDept ? expectedDups : 4, await this.GetJobDepartmentCount<TIJobGrain, TJobProperties>(dept1));
                    Assert.Equal(1, await this.GetJobDepartmentCount<TIJobGrain, TJobProperties>(dept2));

                    // EmployeeId is in parallel with expected11(1(1))
                    var employeeId0 = employeeIdBase + intAdjust;
                    Assert.Equal(1, await this.GetEmployeeIdCount<TIEmployeeGrain, TEmployeeProperties>(employeeId0 + 1));
                    Assert.Equal(isActiveEmployeeId ? expected11 : 1, await this.GetEmployeeIdCount<TIEmployeeGrain, TEmployeeProperties>(employeeId0 + 2));
                    Assert.Equal(isActiveEmployeeId ? expected111 : 1, await this.GetEmployeeIdCount<TIEmployeeGrain, TEmployeeProperties>(employeeId0 + 3));
                    Assert.Equal(isActiveEmployeeId ? expected1111 : 1, await this.GetEmployeeIdCount<TIEmployeeGrain, TEmployeeProperties>(employeeId0 + 4));
                }

                Console.WriteLine("*** First Verify ***");
                await verifyCount(4, 1, 1, 1);

                Console.WriteLine("*** First Deactivate ***");
                await p11.person.Deactivate();
                await Task.Delay(ITC.DelayUntilIndexesAreUpdatedLazily);

                Console.WriteLine("*** Second Verify ***");
                await verifyCount(3, 0, 1, 1);

                Console.WriteLine("*** Second and Third Deactivate ***");
                await p111.person.Deactivate();
                await p1111.person.Deactivate();
                await Task.Delay(ITC.DelayUntilIndexesAreUpdatedLazily);

                Console.WriteLine("*** Third Verify ***");
                await verifyCount(1, 0, 0, 0);

                Console.WriteLine("*** GetGrain ***");
                var p11person = this.GetGrain<TIPersonGrain>(p11.person.GetPrimaryKeyLong());
                var p11transactionalPersistence = p11person as ITestTransactionalPersistence;
                await (p11transactionalPersistence != null ? p11transactionalPersistence.InitializeStateTxn() : p11person.InitializeState());

                Assert.Equal(name11, await p11person.GetName());
                var p11job = p11person.Cast<TIJobGrain>();
                Assert.Equal(title11, await p11job.GetTitle());
                var p11employee = p11job.Cast<TIEmployeeGrain>();
                // EmployeeId is incremented in parallel with intAdjust
                Assert.Equal(intAdjust + 2, await p11employee.GetSalary());

                Console.WriteLine("*** Fourth Verify ***");
                await verifyCount(2, 1, 0, 0);

                Console.WriteLine("*** Fifth Verify ***");
                Assert.Equal(1, await this.GetNameCount<TIPersonGrain, TPersonProperties>(name3));
                Assert.Equal(1, await this.GetPersonAgeCount<TIPersonGrain, TPersonProperties>(age3));
                Assert.Equal(1, await this.GetJobTitleCount<TIJobGrain, TJobProperties>(title3));
                Assert.Equal(1, await this.GetJobDepartmentCount<TIJobGrain, TJobProperties>(dept3));

                Console.WriteLine("*** Update 3x to 4x ***");
                await p3.person.SetName(name4);
                await p3.person.SetAge(age4);
                await p3.job.SetTitle(title4);
                await p3.job.SetDepartment(dept4);
                var p3transactionalPersistence = p3.person as ITestTransactionalPersistence;
                await (p3transactionalPersistence != null ? p3transactionalPersistence.WriteStateTxn() : p3.person.WriteState());

                Console.WriteLine("*** Sixth Verify ***");
                Assert.Equal(0, await this.GetNameCount<TIPersonGrain, TPersonProperties>(name3));
                Assert.Equal(0, await this.GetPersonAgeCount<TIPersonGrain, TPersonProperties>(age3));
                Assert.Equal(0, await this.GetJobTitleCount<TIJobGrain, TJobProperties>(title3));
                Assert.Equal(0, await this.GetJobDepartmentCount<TIJobGrain, TJobProperties>(dept3));
                Assert.Equal(1, await this.GetNameCount<TIPersonGrain, TPersonProperties>(name4));
                Assert.Equal(1, await this.GetPersonAgeCount<TIPersonGrain, TPersonProperties>(age4));
                Assert.Equal(1, await this.GetJobTitleCount<TIJobGrain, TJobProperties>(title4));
                Assert.Equal(1, await this.GetJobDepartmentCount<TIJobGrain, TJobProperties>(dept4));
            }
        }

        public static long GrainPkFromUniqueInt(int uInt) => uInt + 4200000000000;

        protected Task StartAndWaitForSecondSilo()
        {
            if (this.HostedCluster.SecondarySilos.Count == 0)
            {
                this.HostedCluster.StartAdditionalSilo();
                return this.HostedCluster.WaitForLivenessToStabilizeAsync();
            }
            return Task.CompletedTask;
        }
    }
}
