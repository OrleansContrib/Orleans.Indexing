namespace Orleans.Indexing
{
    /// <summary>
    /// IMemberUpdate encapsulates the information related to a grain update with respect to a specific index
    /// </summary>
    public interface IMemberUpdate
    {
        /// <summary>
        /// Returns the before-image of the grain, before applying this update
        /// </summary>
        /// <returns>the before-image of the grain, before applying this update</returns>
        object GetBeforeImage();

        /// <summary>
        /// Produces the after-image of the grain, after applying this update
        /// </summary>
        /// <returns>the after-image of the grain, after applying this update</returns>
        object GetAfterImage();

        /// <summary>
        /// Determines the type of operation done, which can be:
        ///  - Insert
        ///  - Update
        ///  - Delete
        ///  - None, which implies there was no change
        /// </summary>
        /// <returns>the type of operation in this update</returns>
        IndexOperationType OperationType { get; }

        /// <summary>
        /// Returns whether the index update should be non-tentative, tentative, or transactional.
        /// </summary>
        IndexUpdateMode UpdateMode { get; }
    }
}
