using System;

namespace Game.Editor
{
    internal static class DenseCityBuildingAttachmentTransaction
    {
        internal static bool TryCommitAndRealize(
            DenseCityGenerationRecordSet records,
            DenseCityPresentationBakeRecord attachment,
            Func<bool> realize)
        {
            if (records == null)
                throw new ArgumentNullException(nameof(records));
            if (realize == null)
                throw new ArgumentNullException(nameof(realize));

            records.AddBuildingAttachment(attachment);
            try
            {
                if (realize())
                    return true;

                records.RemoveBuildingAttachment(attachment);
                return false;
            }
            catch
            {
                records.RemoveBuildingAttachment(attachment);
                throw;
            }
        }
    }
}
