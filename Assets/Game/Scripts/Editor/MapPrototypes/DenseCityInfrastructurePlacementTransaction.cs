using System;

namespace Game.Editor
{
    internal readonly struct DenseCityInfrastructureRecordGroup
    {
        internal DenseCityInfrastructureRecordGroup(
            DenseCitySurfaceBakeRecord surface,
            DenseCityPresentationBakeRecord presentation)
        {
            Surface = surface;
            Presentation = presentation;
        }

        internal DenseCitySurfaceBakeRecord Surface { get; }
        internal DenseCityPresentationBakeRecord Presentation { get; }
    }

    internal static class DenseCityInfrastructurePlacementTransaction
    {
        internal static bool TryCommitAndRealize(
            DenseCityGenerationRecordSet records,
            DenseCityInfrastructureRecordGroup group,
            Func<bool> realize)
        {
            if (records == null)
                throw new ArgumentNullException(nameof(records));
            if (realize == null)
                throw new ArgumentNullException(nameof(realize));

            records.AddInfrastructureGroup(group.Surface, group.Presentation);
            try
            {
                if (realize())
                    return true;

                records.RemoveInfrastructureGroup(group.Surface, group.Presentation);
                return false;
            }
            catch
            {
                records.RemoveInfrastructureGroup(group.Surface, group.Presentation);
                throw;
            }
        }
    }

    internal static class DenseCitySurfacePlacementTransaction
    {
        internal static bool TryCommitAndRealize(
            DenseCityGenerationRecordSet records,
            DenseCitySurfaceBakeRecord surface,
            Func<bool> realize)
        {
            if (records == null)
                throw new ArgumentNullException(nameof(records));
            if (realize == null)
                throw new ArgumentNullException(nameof(realize));

            records.Add(surface);
            try
            {
                if (realize())
                    return true;

                records.RemoveSurface(surface);
                return false;
            }
            catch
            {
                records.RemoveSurface(surface);
                throw;
            }
        }
    }
}
