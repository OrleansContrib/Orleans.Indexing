using Orleans.TestingHost;
using Orleans.Hosting;
using System;

namespace Orleans.Indexing.Tests
{
    public class DSMIndexingTestFixture : IndexingTestFixture
    {
        internal class SiloBuilderConfiguratorDSMI : ISiloBuilderConfigurator
        {
            // Each class is an Xunit collection receiving the class fixture; we drop the database, so must
            // use a different DB name for each class.
            protected const string DatabaseNamePrefix = "IndexStorageTest_";
            internal virtual string GetDatabaseName() => throw new NotImplementedException();

            public void Configure(ISiloHostBuilder hostBuilder) =>
                BaseIndexingFixture.Configure(hostBuilder, GetDatabaseName())
                                   .UseIndexing(indexingOptions => ConfigureBasicOptions(indexingOptions));
        }
    }

    public class DSMI_EG_IndexingTestFixture : DSMIndexingTestFixture
    {
        internal override void AddSiloBuilderConfigurator(TestClusterBuilder builder) => builder.AddSiloBuilderConfigurator<SiloBuilderConfiguratorDSMI_EG>();

        internal class SiloBuilderConfiguratorDSMI_EG : SiloBuilderConfiguratorDSMI
        {
            internal override string GetDatabaseName() => DatabaseNamePrefix + "DSMI_EG";
        }
    }

    public class DSMI_LZ_IndexingTestFixture : DSMIndexingTestFixture
    {
        internal override void AddSiloBuilderConfigurator(TestClusterBuilder builder) => builder.AddSiloBuilderConfigurator<SiloBuilderConfiguratorDSMI_LZ>();

        internal class SiloBuilderConfiguratorDSMI_LZ : SiloBuilderConfiguratorDSMI
        {
            internal override string GetDatabaseName() => DatabaseNamePrefix + "DSMI_LZ";
        }
    }
}
