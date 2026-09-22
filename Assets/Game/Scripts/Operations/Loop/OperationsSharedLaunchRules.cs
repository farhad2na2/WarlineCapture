using Game.Operations.Contracts;

namespace Game.Operations.Loop
{
    public enum OperationsSharedLaunchKind : byte
    {
        NotRequested = 0,
        Operations = 1,
        ExclusiveConflict = 2,
        RejectedIdentity = 3
    }

    /// <summary>
    /// Mode tag for the shipping match launch. Package 3 keeps the default request closed.
    /// The player path asks for the shared scene and is rejected while Campaign or Skirmish
    /// already owns the slot. This slice accepts operation.o001 only.
    /// </summary>
    public static class OperationsSharedLaunchRules
    {
        public const string PlayerMissionId = "operation.o001";

        public static OperationsSharedLaunchKind Classify(
            bool invokeSharedSceneView,
            string missionId,
            string scenarioId,
            string mapId,
            bool campaignOccupied,
            bool skirmishOccupied)
        {
            if (!invokeSharedSceneView)
                return OperationsSharedLaunchKind.NotRequested;
            if (campaignOccupied || skirmishOccupied)
                return OperationsSharedLaunchKind.ExclusiveConflict;
            if (missionId != PlayerMissionId ||
                !OperationsIdentityRules.IsValidMissionId(missionId) ||
                scenarioId != OperationsIdentityRules.ScenarioId(1) ||
                mapId != OperationsIdentityRules.MapIdForDistrict(1))
                return OperationsSharedLaunchKind.RejectedIdentity;
            return OperationsSharedLaunchKind.Operations;
        }
    }
}
