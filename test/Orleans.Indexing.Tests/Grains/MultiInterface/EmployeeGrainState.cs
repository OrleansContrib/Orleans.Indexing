using System;

namespace Orleans.Indexing.Tests.MultiInterface
{
    [Serializable]
    public class EmployeeGrainState : IEmployeeGrainState
    {
        #region IPersonProperties
        public string Name { get; set; }
        public int Age { get; set; }
        #endregion IPersonProperties

        #region IJobProperties
        public string Title { get; set; }
        public string Department { get; set; }
        #endregion IJobProperties

        #region IEmployeeProperties
        public int EmployeeId { get; set; }
        #endregion IJobProperties

        #region IEmployeeGrainState - not indexed
        public int Salary { get; set; }
        #endregion IEmployeeGrainState - not indexed
    }
}
