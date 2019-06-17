using System;

namespace Orleans.Indexing.Tests
{
    [Serializable]
    public class TestMultiIndexState : ITestMultiIndexState
    {
        #region ITestMultiIndexState
        public int UniqueInt { get; set; }
        public string UniqueString { get; set; }
        public int NonUniqueInt { get; set; }
        public string NonUniqueString { get; set; }
        #endregion ITestMultiIndexState

        #region Not Indexed
        public string UnIndexedString { get; set; }
        #endregion Not Indexed
    }
}
