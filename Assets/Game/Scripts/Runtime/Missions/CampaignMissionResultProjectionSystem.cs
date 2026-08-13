using Game.Components;
using Game.Missions.Contracts;
using Unity.Entities;

namespace Game.Runtime
{
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(CampaignMissionRuntimeSystem))]
    public partial struct CampaignMissionResultProjectionSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<CampaignMissionRootComponent>();
            state.RequireForUpdate<CampaignMissionCatalogComponent>();
            state.RequireForUpdate<CampaignMissionRuntimeComponent>();
            state.RequireForUpdate<CampaignMissionAttemptFactsComponent>();
            state.RequireForUpdate<CampaignMissionSettlementRequestElement>();
        }

        public void OnUpdate(ref SystemState state)
        {
            if (!SystemAPI.TryGetSingletonEntity<CampaignMissionRootComponent>(out Entity root) ||
                !SystemAPI.TryGetSingleton(out CampaignMissionCatalogComponent catalog) ||
                !SystemAPI.TryGetSingleton(out CampaignMissionRuntimeComponent runtime) ||
                !SystemAPI.TryGetSingleton(out CampaignMissionAttemptFactsComponent facts) ||
                !CampaignMissionSpawnSystem.TryFindDefinition(in catalog, in runtime, out int definitionIndex))
                return;

            ref CampaignMissionDefinitionBlob definition = ref catalog.Blob.Value.Missions[definitionIndex];
            if (!TryProject(in runtime, in facts, ref definition, out CampaignMissionResultComponent result))
                return;

            EntityManager entityManager = state.EntityManager;
            if (entityManager.HasComponent<CampaignMissionResultComponent>(root))
            {
                CampaignMissionResultComponent current =
                    entityManager.GetComponentData<CampaignMissionResultComponent>(root);
                if (SameAttempt(in current, in result))
                    return;
                entityManager.SetComponentData(root, result);
            }
            else
            {
                entityManager.AddComponentData(root, result);
            }

            DynamicBuffer<CampaignMissionSettlementRequestElement> requests =
                entityManager.GetBuffer<CampaignMissionSettlementRequestElement>(root);
            requests.Add(new CampaignMissionSettlementRequestElement
            {
                SourceVersion = result.SourceVersion,
                MissionId = result.MissionId,
                SessionToken = result.SessionToken,
                AttemptOrdinal = result.AttemptOrdinal,
                Outcome = result.Outcome
            });
        }

        internal static bool TryProject(
            in CampaignMissionRuntimeComponent runtime,
            in CampaignMissionAttemptFactsComponent facts,
            ref CampaignMissionDefinitionBlob definition,
            out CampaignMissionResultComponent result)
        {
            result = default;
            if (runtime.Version == 0 || runtime.SourceVersion == 0 || runtime.MissionId.IsEmpty ||
                runtime.SessionToken.IsEmpty || runtime.AttemptOrdinal < 0 ||
                runtime.Phase < MissionPhaseKind.Result || runtime.Outcome == MissionOutcomeKind.None ||
                runtime.ReturnDestination == MissionReturnDestinationKind.None ||
                facts.ElapsedMilliseconds < 0 || facts.SquadLossCount < 0 ||
                facts.CommandSquadSpawned == 0 || facts.HostileTotalCount <= 0 ||
                facts.HostileDefeatedCount < 0 || facts.HostileDefeatedCount > facts.HostileTotalCount ||
                !FactsMatchOutcome(runtime.Outcome, in facts) || !TryEvaluateStars(
                    runtime.Outcome, facts.ElapsedMilliseconds, facts.SquadLossCount,
                    ref definition.StarRules, out byte stars))
                return false;

            result = new CampaignMissionResultComponent
            {
                MissionId = runtime.MissionId,
                SessionToken = runtime.SessionToken,
                AttemptOrdinal = runtime.AttemptOrdinal,
                SourceVersion = runtime.Version,
                Outcome = runtime.Outcome,
                ReturnDestination = runtime.ReturnDestination,
                Stars = stars,
                ElapsedMilliseconds = facts.ElapsedMilliseconds,
                SquadLossCount = facts.SquadLossCount
            };
            return true;
        }

        internal static bool TryEvaluateStars(
            MissionOutcomeKind outcome, int elapsedMilliseconds, int squadLossCount,
            ref BlobArray<CampaignMissionStarRuleBlob> rules, out byte stars)
        {
            stars = 0;
            if (rules.Length is < 1 or > 3 || elapsedMilliseconds < 0 || squadLossCount < 0)
                return false;
            byte seen = 0;
            for (int i = 0; i < rules.Length; i++)
            {
                ref CampaignMissionStarRuleBlob rule = ref rules[i];
                if (rule.StarIndex is < 1 or > 3 || rule.Rule == MissionStarRuleKind.None ||
                    (seen & (1 << rule.StarIndex)) != 0 ||
                    (rule.Rule == MissionStarRuleKind.CompleteUnderMilliseconds
                        ? rule.Threshold <= 0 : rule.Threshold != 0))
                    return false;
                seen |= (byte)(1 << rule.StarIndex);
                bool earned = rule.Rule switch
                {
                    MissionStarRuleKind.CompleteMission => outcome == MissionOutcomeKind.Victory,
                    MissionStarRuleKind.NoSquadLoss => outcome == MissionOutcomeKind.Victory && squadLossCount == 0,
                    MissionStarRuleKind.CompleteUnderMilliseconds =>
                        outcome == MissionOutcomeKind.Victory && elapsedMilliseconds < rule.Threshold,
                    _ => false
                };
                if (earned) stars++;
            }
            return true;
        }

        private static bool FactsMatchOutcome(
            MissionOutcomeKind outcome, in CampaignMissionAttemptFactsComponent facts) =>
            outcome == MissionOutcomeKind.Victory
                ? facts.CommandSquadAlive != 0 && facts.HostileDefeatedCount == facts.HostileTotalCount
                : outcome == MissionOutcomeKind.Defeat && facts.CommandSquadAlive == 0;

        private static bool SameAttempt(
            in CampaignMissionResultComponent left, in CampaignMissionResultComponent right) =>
            left.MissionId.Equals(right.MissionId) && left.SessionToken.Equals(right.SessionToken) &&
            left.AttemptOrdinal == right.AttemptOrdinal;
    }
}
