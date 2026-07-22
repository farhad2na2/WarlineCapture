using System;

namespace Game.Editor
{
    internal static class DenseCityRenderOnlyPresentationPlacementTransaction
    {
        internal static bool TryCommitAndRealize(
            DenseCityGenerationRecordSet records,
            DenseCityPresentationBakeRecord presentation,
            Func<bool> realize)
        {
            if (records == null)
                throw new ArgumentNullException(nameof(records));
            if (realize == null)
                throw new ArgumentNullException(nameof(realize));

            records.AddRenderOnlyPresentation(presentation);
            try
            {
                if (realize())
                    return true;

                records.RemoveRenderOnlyPresentation(presentation);
                return false;
            }
            catch
            {
                records.RemoveRenderOnlyPresentation(presentation);
                throw;
            }
        }
    }
}
