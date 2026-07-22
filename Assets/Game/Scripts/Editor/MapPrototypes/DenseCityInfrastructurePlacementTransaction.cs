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

    internal readonly struct DenseCityBridgeRecordGroup
    {
        internal DenseCityBridgeRecordGroup(
            DenseCitySurfaceBakeRecord bridge,
            DenseCityPresentationBakeRecord presentation,
            DenseCitySurfaceBakeRecord firstApproachRamp,
            DenseCitySurfaceBakeRecord secondApproachRamp)
        {
            Bridge = bridge;
            Presentation = presentation;
            FirstApproachRamp = firstApproachRamp;
            SecondApproachRamp = secondApproachRamp;
        }

        internal DenseCitySurfaceBakeRecord Bridge { get; }
        internal DenseCityPresentationBakeRecord Presentation { get; }
        internal DenseCitySurfaceBakeRecord FirstApproachRamp { get; }
        internal DenseCitySurfaceBakeRecord SecondApproachRamp { get; }
    }

    internal readonly struct DenseCityRoadRecordGroup
    {
        internal DenseCityRoadRecordGroup(
            DenseCitySurfaceBakeRecord road,
            DenseCityPresentationBakeRecord presentation,
            DenseCitySurfaceBakeRecord[] shoulders)
        {
            Road = road;
            Presentation = presentation;
            Shoulders = shoulders ?? throw new ArgumentNullException(nameof(shoulders));
        }

        internal DenseCitySurfaceBakeRecord Road { get; }
        internal DenseCityPresentationBakeRecord Presentation { get; }
        internal DenseCitySurfaceBakeRecord[] Shoulders { get; }
    }

    internal readonly struct DenseCityCanalWaterRecordGroup
    {
        internal DenseCityCanalWaterRecordGroup(
            DenseCitySurfaceBakeRecord exclusion,
            DenseCityPresentationBakeRecord bedPresentation,
            DenseCityPresentationBakeRecord waterPresentation)
        {
            Exclusion = exclusion;
            BedPresentation = bedPresentation;
            WaterPresentation = waterPresentation;
        }

        internal DenseCitySurfaceBakeRecord Exclusion { get; }
        internal DenseCityPresentationBakeRecord BedPresentation { get; }
        internal DenseCityPresentationBakeRecord WaterPresentation { get; }
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

    internal static class DenseCityBridgePlacementTransaction
    {
        internal static bool TryCommitAndRealize(
            DenseCityGenerationRecordSet records,
            DenseCityBridgeRecordGroup group,
            Func<bool> realize)
        {
            if (records == null)
                throw new ArgumentNullException(nameof(records));
            if (realize == null)
                throw new ArgumentNullException(nameof(realize));

            records.AddBridgeGroup(
                group.Bridge,
                group.Presentation,
                group.FirstApproachRamp,
                group.SecondApproachRamp);
            try
            {
                if (realize())
                    return true;

                Remove(records, group);
                return false;
            }
            catch
            {
                Remove(records, group);
                throw;
            }
        }

        private static void Remove(
            DenseCityGenerationRecordSet records,
            DenseCityBridgeRecordGroup group) =>
            records.RemoveBridgeGroup(
                group.Bridge,
                group.Presentation,
                group.FirstApproachRamp,
                group.SecondApproachRamp);
    }

    internal static class DenseCityCanalWaterPlacementTransaction
    {
        internal static bool TryCommitAndRealize(
            DenseCityGenerationRecordSet records,
            DenseCityCanalWaterRecordGroup group,
            Func<bool> realize)
        {
            if (records == null)
                throw new ArgumentNullException(nameof(records));
            if (realize == null)
                throw new ArgumentNullException(nameof(realize));

            records.AddCanalWaterGroup(
                group.Exclusion,
                group.BedPresentation,
                group.WaterPresentation);
            try
            {
                if (realize())
                    return true;

                Remove(records, group);
                return false;
            }
            catch
            {
                Remove(records, group);
                throw;
            }
        }

        private static void Remove(
            DenseCityGenerationRecordSet records,
            DenseCityCanalWaterRecordGroup group) =>
            records.RemoveCanalWaterGroup(
                group.Exclusion,
                group.BedPresentation,
                group.WaterPresentation);
    }

    internal static class DenseCityRoadPlacementTransaction
    {
        internal static bool TryCommitAndRealize(
            DenseCityGenerationRecordSet records,
            DenseCityRoadRecordGroup group,
            Func<bool> realize)
        {
            if (records == null)
                throw new ArgumentNullException(nameof(records));
            if (realize == null)
                throw new ArgumentNullException(nameof(realize));

            records.AddRoadGroup(group.Road, group.Presentation, group.Shoulders);
            try
            {
                if (realize())
                    return true;

                records.RemoveRoadGroup(group.Road, group.Presentation, group.Shoulders);
                return false;
            }
            catch
            {
                records.RemoveRoadGroup(group.Road, group.Presentation, group.Shoulders);
                throw;
            }
        }
    }
}
