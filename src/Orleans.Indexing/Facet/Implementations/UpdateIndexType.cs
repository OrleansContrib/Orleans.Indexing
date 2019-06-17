using System;

namespace Orleans.Indexing.Facet
{
    /// <summary>
    /// Indicates whether an update should apply exclusively to unique or non-unique indexes.
    /// </summary>
    [Flags]
    internal enum UpdateIndexType
    {
        None = 0,
        Unique = 1,
        NonUnique = 2,
        Both = Unique | NonUnique
    }
}
