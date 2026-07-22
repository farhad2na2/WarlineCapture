using System;

namespace Game.Editor
{
    internal static class DenseCityBuildingPlacementTransaction
    {
        internal static bool TryCommitAndRealize(
            DenseCityGenerationRecordSet records,
            DenseCityBuildingRecordGroup group,
            Func<bool> realize)
        {
            if (records == null)
                throw new ArgumentNullException(nameof(records));
            if (realize == null)
                throw new ArgumentNullException(nameof(realize));

            DenseCityBuildingRecordFactory.Add(records, group);
            try
            {
                if (realize())
                    return true;

                records.RemoveBuildingGroup(group.Building);
                return false;
            }
            catch
            {
                records.RemoveBuildingGroup(group.Building);
                throw;
            }
        }
    }
}
