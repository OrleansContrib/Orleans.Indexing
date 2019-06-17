namespace Orleans.Indexing
{
    public enum IndexUpdateMode
    {
        /// <summary>
        /// Tentative update for a unique index during workflow.
        /// This is part of the two-step workflow operation to ensure the change is not visible by others, but still
        /// blocks further violation of constraints such as uniqueness constraint.
        /// </summary>
        Tentative,

        /// <summary>
        /// Non-tentative update, possibly for a unique index, during workflow. This makes any tentative update permanent.
        /// </summary>
        NonTentative,

        /// <summary>
        /// Non-tentative transactional update; always permanent if the transaction commits, else it is rolled back.
        /// </summary>
        Transactional
    }
}
