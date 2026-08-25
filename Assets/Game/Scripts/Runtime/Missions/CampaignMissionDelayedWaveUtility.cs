using Game.Components;
using Game.Missions.Contracts;
using Unity.Collections;
using Unity.Entities;

namespace Game.Runtime
{
    internal static class CampaignMissionDelayedWaveUtility
    {
        internal static bool ShouldSuppressAtSpawn(
            ref CampaignMissionDefinitionBlob definition,
            in FixedString64Bytes unitGroupId) =>
            definition.MissionRuntimeEnabled != 0 &&
            !definition.DelayedWaveUnitGroupId.IsEmpty &&
            definition.DelayedWaveUnitGroupId.Equals(unitGroupId);

        internal static void ApplyCombatHoldAtSpawn(
            EntityManager entityManager,
            Entity entity,
            ref CampaignMissionDefinitionBlob definition,
            in FixedString64Bytes unitGroupId)
        {
            if (!ShouldSuppressAtSpawn(ref definition, in unitGroupId) ||
                !entityManager.HasComponent<UnitCombat>(entity))
                return;

            UnitCombat combat = entityManager.GetComponentData<UnitCombat>(entity);
            combat.AutoEngage = 0;
            entityManager.SetComponentData(entity, combat);
        }

        internal static bool ShouldIssuePatrolRoute(
            in FixedString64Bytes missionId,
            MissionPhaseKind phase,
            byte missionRuntimeEnabled,
            byte delayedWaveActivated) =>
            missionRuntimeEnabled != 0
                ? delayedWaveActivated != 0
                : CampaignMissionPatrolOrderSystem.ShouldIssuePatrolRoute(missionId, phase);

        internal static bool TryResolveDefinition(
            ref CampaignMissionDefinitionBlob definition,
            out int expectedUnitCount,
            out byte expectedFactionId)
        {
            expectedUnitCount = 0;
            expectedFactionId = FactionIdentity.NeutralFactionId;
            if (definition.MissionRuntimeEnabled == 0 ||
                definition.DelayedWaveUnitGroupId.IsEmpty ||
                definition.DelayedWaveRouteId.IsEmpty ||
                definition.DelayedWaveTargetMissionRoleId.IsEmpty ||
                definition.DelayedWaveWarningAtMilliseconds < 0 ||
                definition.DelayedWaveActivationAtMilliseconds <=
                definition.DelayedWaveWarningAtMilliseconds)
                return false;

            int groupMatches = 0;
            for (int i = 0; i < definition.ForceGroups.Length; i++)
            {
                ref CampaignMissionForceGroupBlob group = ref definition.ForceGroups[i];
                if (!group.GroupId.Equals(definition.DelayedWaveUnitGroupId))
                    continue;

                groupMatches++;
                if (group.FactionId == FactionIdentity.NeutralFactionId ||
                    FactionIdentity.IsPlayerControlled(group.FactionId))
                    return false;
                expectedFactionId = group.FactionId;
                for (int unitIndex = 0; unitIndex < group.Units.Length; unitIndex++)
                {
                    int count = group.Units[unitIndex].Count;
                    if (count < 1)
                        return false;
                    expectedUnitCount += count;
                }
            }

            int routeMatches = 0;
            for (int i = 0; i < definition.PatrolRoutes.Length; i++)
            {
                ref CampaignMissionPatrolRouteBlob route = ref definition.PatrolRoutes[i];
                if (!route.RouteId.Equals(definition.DelayedWaveRouteId))
                    continue;

                routeMatches++;
                if (!route.UnitGroupId.Equals(definition.DelayedWaveUnitGroupId) ||
                    route.StartDelayMilliseconds != definition.DelayedWaveActivationAtMilliseconds ||
                    route.AnchorIds.Length == 0)
                    return false;
            }

            return groupMatches == 1 && routeMatches == 1 && expectedUnitCount > 0;
        }

        internal static bool ShouldIssueWarning(
            int elapsedMilliseconds,
            int warningAtMilliseconds,
            byte warningIssued) =>
            warningIssued == 0 && elapsedMilliseconds >= warningAtMilliseconds;

        internal static bool ShouldActivate(
            int elapsedMilliseconds,
            int activationAtMilliseconds,
            byte warningIssued,
            byte activated) =>
            warningIssued != 0 && activated == 0 &&
            elapsedMilliseconds >= activationAtMilliseconds;
    }
}
