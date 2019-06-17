using System;

namespace Orleans.Indexing
{
    /// <summary>
    /// Enumeration of all index update operation types
    /// </summary>
    public enum IndexOperationType { None, Insert, Update, Delete }

    public static class IndexOperationTypeExtensions
    {
        /// <summary>
        /// Combines two OperationTypes
        /// </summary>
        /// <param name="thisOp">OperationType1</param>
        /// <param name="otherOp">OperationType2</param>
        /// <returns></returns>
        public static IndexOperationType CombineWith(this IndexOperationType thisOp, IndexOperationType otherOp)
        {
            switch (thisOp)
            {
                case IndexOperationType.None:
                    return otherOp;
                case IndexOperationType.Insert:
                    switch (otherOp)
                    {
                        case IndexOperationType.Insert:
                            throw new IndexOperationException(string.Format("Two subsequent Insert operations are not allowed."));
                        case IndexOperationType.Update:
                            return IndexOperationType.Insert;
                        case IndexOperationType.Delete:
                            return IndexOperationType.None;
                        default: //case IndexOperationType.None
                            return thisOp;
                    }
                case IndexOperationType.Update:
                    switch (otherOp)
                    {
                        case IndexOperationType.Insert:
                            throw new IndexOperationException(string.Format("An Insert operation after an Update operation is not allowed."));
                        case IndexOperationType.Delete:
                            return otherOp; //i.e., IndexOperationType.Delete
                        default: //case IndexOperationType.None or IndexOperationType.Update
                            return thisOp;
                    }
                case IndexOperationType.Delete:
                    switch (otherOp)
                    {
                        case IndexOperationType.Insert:
                            return IndexOperationType.Update;
                        case IndexOperationType.Update:
                            throw new IndexOperationException(string.Format("An Update operation after a Delete operation is not allowed."));
                        default: //case IndexOperationType.None or IndexOperationType.Delete
                            return thisOp;
                    }
                default:
                    throw new IndexOperationException(string.Format("Operation type {0} is not valid", thisOp));
            }
        }
    }
}
