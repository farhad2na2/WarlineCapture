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

    internal static class DenseCityRenderOnlyPresentationGroupPlacementTransaction
    {
        internal static bool TryCommitAndRealize(
            DenseCityGenerationRecordSet records,
            DenseCityPresentationBakeRecord[] presentations,
            Func<bool> realize)
        {
            if (records == null)
                throw new ArgumentNullException(nameof(records));
            if (presentations == null || presentations.Length == 0)
                throw new ArgumentOutOfRangeException(nameof(presentations));
            if (realize == null)
                throw new ArgumentNullException(nameof(realize));

            int committed = 0;
            try
            {
                for (; committed < presentations.Length; committed++)
                    records.AddRenderOnlyPresentation(presentations[committed]);
                if (realize())
                    return true;

                RollBack();
                return false;
            }
            catch
            {
                RollBack();
                throw;
            }

            void RollBack()
            {
                for (int index = committed - 1; index >= 0; index--)
                    records.RemoveRenderOnlyPresentation(presentations[index]);
                committed = 0;
            }
        }
    }
}
