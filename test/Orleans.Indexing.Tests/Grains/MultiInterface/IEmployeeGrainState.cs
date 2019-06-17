namespace Orleans.Indexing.Tests.MultiInterface
{
    public interface IEmployeeGrainState : IPersonProperties, IJobProperties, IEmployeeProperties
    {
        // Not indexed
        int Salary { get; set; }
    }
}
