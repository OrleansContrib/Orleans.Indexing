using System;
using System.Collections.Generic;
using System.Text;

namespace Orleans.Indexing
{
    /// <summary>
    /// MemberUpdate is a generic implementation of IMemberUpdate that relies on a copy of beforeImage and afterImage, without
    /// keeping any semantic information about the actual change that happened.
    /// 
    /// This class assumes that befImg and aftImg passed to it won't be altered later on, so they are immutable.
    /// </summary>
    [Serializable]
    internal class MemberUpdate : IMemberUpdate
    {
        private object _befImg;
        private object _aftImg;

        public IndexUpdateMode UpdateMode => IndexUpdateMode.NonTentative;

        public MemberUpdate(object befImg, object aftImg, IndexOperationType opType)
        {
            this.OperationType = opType;
            if (opType == IndexOperationType.Update || opType == IndexOperationType.Delete)
            {
                this._befImg = befImg;
            }
            if (opType == IndexOperationType.Update || opType == IndexOperationType.Insert)
            {
                this._aftImg = aftImg;
            }
        }

        public MemberUpdate(object befImg, object aftImg) : this(befImg, aftImg, GetOperationType(befImg, aftImg))
        {
        }

        private static IndexOperationType GetOperationType(object befImg, object aftImg)
        {
            if (befImg == null)
            {
                return (aftImg == null) ? IndexOperationType.None : IndexOperationType.Insert;
            }
            return aftImg == null
                ? IndexOperationType.Delete
                : befImg.Equals(aftImg) ? IndexOperationType.None : IndexOperationType.Update;
        }

        /// <summary>
        /// Exposes the stored before-image.
        /// </summary>
        /// <returns>the before-image of the indexed attribute(s) that is before applying the current update</returns>
        public object GetBeforeImage() => (this.OperationType == IndexOperationType.Update || this.OperationType == IndexOperationType.Delete) ? this._befImg : null;

        public object GetAfterImage() => (this.OperationType == IndexOperationType.Update || this.OperationType == IndexOperationType.Insert) ? this._aftImg : null;

        public IndexOperationType OperationType { get; }

        public override string ToString() => ToString(this);

        internal static string ToString(IMemberUpdate update)
        {
            switch (update.OperationType)
            {
                case IndexOperationType.None: return update.GetType().Name + ": No operation";
                case IndexOperationType.Insert: return update.GetType().Name + ": Inserted " + update.GetAfterImage();
                case IndexOperationType.Delete: return update.GetType().Name + ": Deleted " + update.GetBeforeImage();
                case IndexOperationType.Update: return update.GetType().Name + ": Updated " + update.GetBeforeImage() + " into " + update.GetAfterImage();
                default: return update.GetType().Name + ": Unsupported operation";
            }
        }

        internal static string UpdatesToString(IDictionary<IIndexableGrain, IList<IMemberUpdate>> iUpdates)
        {
            var sb = new StringBuilder();
            foreach (var grainUpdate in iUpdates)
            {
                sb.Append(Environment.NewLine).Append(grainUpdate.Key).Append(" =>");
                grainUpdate.Value.ForEach(updt => sb.Append(Environment.NewLine).Append("\t").Append(updt));
            }
            return sb.ToString();
        }
    }
}
