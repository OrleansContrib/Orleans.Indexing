namespace Orleans.Indexing.Facet
{
    /// <summary>
    /// Indicates whether an index update is happening due to activation, deactivation, or while remaining active.
    /// </summary>
    internal enum IndexUpdateReason
    {
        OnActivate,
        WriteState,
        OnDeactivate
    }
}
