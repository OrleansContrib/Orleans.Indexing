using System.Linq;

namespace Orleans.Indexing
{
    internal static class HashIndexBucketUtils
    {
        /// <summary>
        /// This method contains the common functionality for updating the hash-index bucket.
        /// </summary>
        /// <typeparam name="K">key type</typeparam>
        /// <typeparam name="V">value type</typeparam>
        /// <param name="updatedGrain">the updated grain that is being indexed</param>
        /// <param name="iUpdate">the update information</param>
        /// <param name="state">the index bucket to be updated</param>
        /// <param name="isUniqueIndex">a flag to indicate whether the hash-index has a uniqueness constraint</param>
        /// <param name="idxMetaData">the index metadata</param>
        internal static bool UpdateBucketState<K, V>(V updatedGrain, IMemberUpdate iUpdate, HashIndexBucketState<K, V> state, bool isUniqueIndex,
                                                     IndexMetaData idxMetaData) where V : IIndexableGrain
            => UpdateBucketState(updatedGrain, iUpdate, state, isUniqueIndex, idxMetaData, out bool _);

        /// <summary>
        /// This method contains the common functionality for updating the hash-index bucket.
        /// </summary>
        /// <typeparam name="K">key type</typeparam>
        /// <typeparam name="V">value type</typeparam>
        /// <param name="updatedGrain">the updated grain that is being indexed</param>
        /// <param name="update">the update information</param>
        /// <param name="state">the index bucket to be updated</param>
        /// <param name="isUniqueIndex">a flag to indicate whether the hash-index has a uniqueness constraint</param>
        /// <param name="idxMetaData">the index metadata</param>
        /// <param name="fixIndexUnavailableOnDelete">output parameter: this variable determines whether
        ///             the index was still unavailable when we received a delete operation</param>
        internal static bool UpdateBucketState<K, V>(V updatedGrain, IMemberUpdate update, HashIndexBucketState<K, V> state, bool isUniqueIndex,
                                                     IndexMetaData idxMetaData, out bool fixIndexUnavailableOnDelete) where V : IIndexableGrain
        {
            fixIndexUnavailableOnDelete = false;

            var indexUpdateMode = update.UpdateMode;
            var opType = update.OperationType;
            HashIndexSingleBucketEntry<V> aftEntry;

            // Insert is done for both IndexOperationType.Update and IndexOperationType.Update, so use a local function.
            bool doInsert(K afterImage, out bool uniquenessViolation)
            {
                uniquenessViolation = false;
                if (state.IndexMap.TryGetValue(afterImage, out aftEntry))
                {
                    if (!aftEntry.Values.Contains(updatedGrain))
                    {
                        if (isUniqueIndex && aftEntry.Values.Count > 0)
                        {
                            uniquenessViolation = true;
                            return false;
                        }
                        aftEntry.Add(updatedGrain, indexUpdateMode, isUniqueIndex);
                    }
                    else if (indexUpdateMode == IndexUpdateMode.Tentative)
                    {
                        aftEntry.SetTentativeInsert();
                    }
                    else
                    {
                        aftEntry.ClearTentativeFlag();
                    }
                    return true;
                }

                // Add a new entry
                if (idxMetaData.IsCreatingANewBucketNecessary(state.IndexMap.Count))
                {
                    return false;  // the bucket is full
                }

                aftEntry = new HashIndexSingleBucketEntry<V>();
                aftEntry.Add(updatedGrain, indexUpdateMode, isUniqueIndex);
                state.IndexMap.Add(afterImage, aftEntry);
                return true;
            }

            if (opType == IndexOperationType.Update)
            {
                K aftImg = (K)update.GetAfterImage();
                var befImg = (K)update.GetBeforeImage();
                if (state.IndexMap.TryGetValue(befImg, out var befEntry) && befEntry.Values.Contains(updatedGrain))
                {
                    // Delete and Insert
                    if (state.IndexMap.TryGetValue(aftImg, out aftEntry))
                    {
                        if (aftEntry.Values.Contains(updatedGrain))
                        {
                            if (indexUpdateMode == IndexUpdateMode.Tentative)
                            {
                                aftEntry.SetTentativeInsert();
                            }
                            else
                            {
                                aftEntry.ClearTentativeFlag();
                                befEntry.Remove(updatedGrain, indexUpdateMode, isUniqueIndex);
                            }
                        }
                        else
                        {
                            if (isUniqueIndex && aftEntry.Values.Count > 0)
                            {
                                throw new UniquenessConstraintViolatedException(
                                        $"The uniqueness property of index {idxMetaData.IndexName} is would be violated for an update operation" +
                                        $" for before-image = {befImg}, after-image = {aftImg} and grain = {updatedGrain.GetPrimaryKey()}");
                            }
                            befEntry.Remove(updatedGrain, indexUpdateMode, isUniqueIndex);
                            aftEntry.Add(updatedGrain, indexUpdateMode, isUniqueIndex);
                        }
                    }
                    else
                    {
                        aftEntry = new HashIndexSingleBucketEntry<V>();
                        befEntry.Remove(updatedGrain, indexUpdateMode, isUniqueIndex);
                        aftEntry.Add(updatedGrain, indexUpdateMode, isUniqueIndex);
                        state.IndexMap.Add(aftImg, aftEntry);
                    }
                }
                else
                {
                    // Insert-only, because Delete was not found. If there's a NextBucket so return false to search it;
                    // otherwise, the desired Delete result is met (the previous value is not present), so proceed to Insert.
                    if (idxMetaData.IsChainedBuckets && state.NextBucket != null)
                    {
                        return false;
                    }

                    if (!doInsert(aftImg, out bool uniquenessViolation))
                    {
                        return uniquenessViolation
                            ? throw new UniquenessConstraintViolatedException(
                                    $"The uniqueness property of index {idxMetaData.IndexName} would be violated for an update operation" +
                                    $" for (not found before-image = {befImg}), after-image = {aftImg} and grain = {updatedGrain.GetPrimaryKey()}")
                            : false;    // The bucket is full
                    }
                }
            }
            else if (opType == IndexOperationType.Insert)
            {
                if (!doInsert((K)update.GetAfterImage(), out bool uniquenessViolation))
                {
                    return uniquenessViolation
                        ? throw new UniquenessConstraintViolatedException(
                                $"The uniqueness property of index {idxMetaData.IndexName} would be violated for an insert operation" +
                                $" for after-image = {(K)update.GetAfterImage()} and grain = {updatedGrain.GetPrimaryKey()}")
                        : false;
                }
            }
            else if (opType == IndexOperationType.Delete)
            {
                var befImg = (K)update.GetBeforeImage();

                if (state.IndexMap.TryGetValue(befImg, out var befEntry) && befEntry.Values.Contains(updatedGrain))
                {
                    befEntry.Remove(updatedGrain, indexUpdateMode, isUniqueIndex);
                    if (state.IndexStatus != IndexStatus.Available)
                    {
                        fixIndexUnavailableOnDelete = true;
                    }
                }
                else if (idxMetaData.IsChainedBuckets)
                {
                    // Not found in this bucket. If there's a NextBucket, return false to search it;
                    // otherwise, the desired Delete result is met (the value is not present), so return true.
                    return state.NextBucket == null;
                }
            }
            return true;
        }
    }
}
