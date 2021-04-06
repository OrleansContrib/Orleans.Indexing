using Orleans.TestingHost;
using Orleans.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Orleans.Indexing.TestInjection;

namespace Orleans.Indexing.Tests
{

    public class IndexingGrainRecoveryTestFixture : SingleSiloIndexingTestFixture
    {
        internal override void AddSiloBuilderConfigurator(TestClusterBuilder builder)
        {
            builder.AddSiloBuilderConfigurator<GrainRecoverySiloBuilderConfigurator>();
            base.AddSiloBuilderConfigurator(builder);
        }

        internal class GrainRecoverySiloBuilderConfigurator : ISiloConfigurator
        {
            public void Configure(ISiloBuilder hostBuilder) =>
                hostBuilder.ConfigureServices(services => services.AddSingleton<IInjectableCode>(_ => new TestInjectableCode { SkipQueueThread = true }));
        }
    }

    public class IndexingQueueRecoveryTestFixture : IndexingGrainRecoveryTestFixture
    {
        internal override void AddSiloBuilderConfigurator(TestClusterBuilder builder)
        {
            builder.AddSiloBuilderConfigurator<QueueRecoverySiloBuilderConfigurator>();
            base.AddSiloBuilderConfigurator(builder);
        }

        internal class QueueRecoverySiloBuilderConfigurator : ISiloConfigurator
        {
            public void Configure(ISiloBuilder hostBuilder) =>
                hostBuilder.ConfigureServices(services => services.AddSingleton<IInjectableCode>(_ => new TestInjectableCode { ForceReincarnatedQueue = true }));
        }
    }
}
