namespace Orleans.Indexing
{
    /// <summary>
    /// This interface specifies a method that each index should define for extracting the part of the grain state it is interested in. 
    /// The interface also specifies a method creating an update object after an update happens on the indexed grain
    /// </summary>
    public interface IIndexUpdateGenerator
    {
        /// <summary>
        /// Extracts some part of the state of the grain that this index is interested in
        /// </summary>
        /// <param name="indexedGrainProperties">the grain from which we want to extract some state to be indexed</param>
        /// <returns>an encapsulation of the part of the grain state that this index is interested in</returns>
        object ExtractIndexImage(object indexedGrainProperties);

        /// <summary>
        /// Creates an update object after receiving the current state of the grain and an earlier image of the grain 
        /// </summary>
        /// <param name="indexedGrainProperties">the properties of the grain from which we want to extract some state to be indexed</param>
        /// <param name="beforeImage">the before-image of the indexedGrain, which was captured earlier via a call to ExtractIndexImage(indexedGrain)</param>
        /// <returns>an IMemberUpdate instance that contains the update information</returns>
        IMemberUpdate CreateMemberUpdate(object indexedGrainProperties, object beforeImage);

        /// <summary>
        /// Creates an update object using the current image of the grain (used during activation)
        /// </summary>
        /// <param name="afterImage">the after-image of the indexedGrain, which was captured earlier via a call to ExtractIndexImage(indexedGrain)</param>
        /// <returns>an IMemberUpdate instance that contains the update information</returns>
        IMemberUpdate CreateMemberUpdate(object afterImage);
    }

    internal abstract class IIndexUpdateGenerator<K, TProperties> : IIndexUpdateGenerator
    {
        object IIndexUpdateGenerator.ExtractIndexImage(object indexedGrainProperties)
            => ExtractIndexImage((TProperties)indexedGrainProperties);

        /// <summary>
        /// This method is the typed version of ExtractIndexImage
        /// </summary>
        /// <param name="indexedGrainProperties">the grain from which we want to extract some state to be indexed</param>
        /// <returns>an encapsulation of the part of the grain state that this index is interested in</returns>
        abstract public K ExtractIndexImage(TProperties indexedGrainProperties);

        IMemberUpdate IIndexUpdateGenerator.CreateMemberUpdate(object indexedGrainProperties, object beforeImage)
            => CreateMemberUpdate((TProperties)indexedGrainProperties, (K)beforeImage);

        IMemberUpdate IIndexUpdateGenerator.CreateMemberUpdate(object afterImage)
            => CreateMemberUpdate((K)afterImage);

        /// <summary>
        /// This method is the typed version of CreateMemberUpdate
        /// </summary>
        /// <param name="indexedGrainProperties">the grain from which we want to extract some state to be indexed</param>
        /// <param name="beforeImage">the before-image of the indexedGrain, which was captured earlier via a call to ExtractIndexImage(indexedGrain)</param>
        /// <returns>an IMemberUpdate instance that contains the update information</returns>
        /// <returns></returns>
        public abstract IMemberUpdate CreateMemberUpdate(TProperties indexedGrainProperties, K beforeImage);

        /// <summary>
        /// Creates an update object using the current image of the grain (used during activation)
        /// </summary>
        /// <param name="afterImage">the after-image of the indexedGrain,
        /// which was captured earlier via a call to ExtractIndexImage(indexedGrain)</param>
        /// <returns>an IMemberUpdate instance that contains the update information</returns>
        public abstract IMemberUpdate CreateMemberUpdate(K afterImage);
    }
}
