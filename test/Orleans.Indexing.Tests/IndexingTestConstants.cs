using Orleans.Indexing.Tests.MultiInterface;

namespace Orleans.Indexing.Tests
{
    public static class IndexingTestConstants
    {
        // storage providers
        public const string GrainStore = "GrainStore";
        public const string CosmosDBGrainStorage = "CosmosDBGrainStorage";
        public const string TestGrainState = "TestGrainState";

        public const string Seattle = "Seattle";
        public const string SanFrancisco = "San Francisco";
        public const string SanJose = "SanJose";
        public const string Bellevue = "Bellevue";
        public const string Redmond = "Redmond";
        public const string Kirkland = "Kirkland";
        public const string Tehran = "Tehran";
        public const string Yazd = "Yazd";
        public const string NewYork = "New York";
        public const string LosAngeles = "Los Angeles";

        public const string LocationProperty = nameof(IPlayerProperties.Location);
        public const string ScoreProperty = nameof(IPlayerProperties.Score);
        public const string NameProperty = nameof(IPersonProperties.Name);
        public const string AgeProperty = nameof(IPersonProperties.Age);
        public const string TitleProperty = nameof(IJobProperties.Title);
        public const string DepartmentProperty = nameof(IJobProperties.Department);
        public const string EmployeeIdProperty = nameof(IEmployeeProperties.EmployeeId);

        public const string UniqueIntProperty = nameof(ITestMultiIndexProperties.UniqueInt);
        public const string UniqueStringProperty = nameof(ITestMultiIndexProperties.UniqueString);
        public const string NonUniqueIntProperty = nameof(ITestMultiIndexProperties.NonUniqueInt);
        public const string NonUniqueStringProperty = nameof(ITestMultiIndexProperties.NonUniqueString);

        public const int DelayUntilIndexesAreUpdatedLazily = 1000; // One-second delay for writes to the in-memory indexes should be enough
    }
}
