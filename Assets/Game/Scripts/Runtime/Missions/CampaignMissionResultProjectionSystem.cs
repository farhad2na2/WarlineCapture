using Game.Components;
using Game.Missions.Contracts;
using Unity.Collections;
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
                facts.HostileTotalCount < 0 ||
                facts.HostileDefeatedCount < 0 || facts.HostileDefeatedCount > facts.HostileTotalCount ||
                facts.RequiredBuildingCompletedCount < 0 || facts.RequiredUnitProducedCount < 0 ||
                facts.CivilianTotalCount < 0 || facts.CivilianLossCount < 0 ||
                facts.CivilianLossCount > facts.CivilianTotalCount ||
                !FactsMatchOutcome(runtime.Outcome, in facts, ref definition) || !TryEvaluateStars(
                    runtime.Outcome, facts.ElapsedMilliseconds, facts.SquadLossCount, facts.CivilianLossCount,
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
                SquadLossCount = facts.SquadLossCount,
                CivilianLossCount = facts.CivilianLossCount
            };
            return true;
        }

        internal static bool TryEvaluateStars(
            MissionOutcomeKind outcome, int elapsedMilliseconds, int squadLossCount,
            ref BlobArray<CampaignMissionStarRuleBlob> rules, out byte stars)
            => TryEvaluateStars(
                outcome, elapsedMilliseconds, squadLossCount, 0, ref rules, out stars);

        internal static bool TryEvaluateStars(
            MissionOutcomeKind outcome, int elapsedMilliseconds, int squadLossCount, int civilianLossCount,
            ref BlobArray<CampaignMissionStarRuleBlob> rules, out byte stars)
        {
            stars = 0;
            if (rules.Length is < 1 or > 3 || elapsedMilliseconds < 0 || squadLossCount < 0 ||
                civilianLossCount < 0)
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
                    MissionStarRuleKind.NoCivilianLoss =>
                        outcome == MissionOutcomeKind.Victory && civilianLossCount == 0,
                    MissionStarRuleKind.CompleteUnderMilliseconds =>
                        outcome == MissionOutcomeKind.Victory && elapsedMilliseconds < rule.Threshold,
                    _ => false
                };
                if (earned) stars++;
            }
            return true;
        }

        private static bool FactsMatchOutcome(
            MissionOutcomeKind outcome,
            in CampaignMissionAttemptFactsComponent facts,
            ref CampaignMissionDefinitionBlob definition)
        {
            if (definition.Objectives.Length == 0)
                return false;

            bool allComplete = true;
            bool failureBroken = false;
            for (int index = 0; index < definition.Objectives.Length; index++)
            {
                ref CampaignMissionObjectiveBlob objective = ref definition.Objectives[index];
                if (!IsValidObjective(ref definition, index, in objective))
                    return false;

                bool complete;
                bool broken = false;
                switch (objective.Rule)
                {
                    case MissionObjectiveRuleKind.DestroyMissionRole:
                        complete = facts.HostileTotalCount == objective.RequiredCount &&
                                   facts.HostileDefeatedCount >= objective.RequiredCount;
                        break;
                    case MissionObjectiveRuleKind.ProtectMissionRole:
                        complete = facts.CommandSquadSpawned != 0 && facts.CommandSquadAlive != 0;
                        broken = facts.CommandSquadSpawned != 0 && facts.CommandSquadAlive == 0;
                        break;
                    case MissionObjectiveRuleKind.BuildStructure:
                        complete = facts.RequiredBuildingCompletedCount >= objective.RequiredCount;
                        break;
                    case MissionObjectiveRuleKind.ProduceUnit:
                        complete = facts.RequiredUnitProducedCount >= objective.RequiredCount;
                        break;
                    case MissionObjectiveRuleKind.DefendMissionRole:
                        complete = facts.ForwardPostBound != 0 && facts.ForwardPostDestroyed == 0 &&
                                   facts.DefenseWaveActivated != 0 && facts.HostileTotalCount > 0 &&
                                   facts.HostileDefeatedCount >= facts.HostileTotalCount;
                        broken = facts.ForwardPostBound != 0 && facts.ForwardPostDestroyed != 0;
                        break;
                    default:
                        return false;
                }
                allComplete &= complete;
                failureBroken |= objective.FailureOnRuleBreak != 0 && broken;
            }

            return outcome == MissionOutcomeKind.Victory
                ? allComplete
                : outcome == MissionOutcomeKind.Defeat && failureBroken;
        }

        private static bool IsValidObjective(
            ref CampaignMissionDefinitionBlob definition,
            int index,
            in CampaignMissionObjectiveBlob objective)
        {
            if (objective.ObjectiveId.IsEmpty || objective.RequiredCount <= 0)
                return false;
            for (int previous = 0; previous < index; previous++)
                if (definition.Objectives[previous].ObjectiveId.Equals(objective.ObjectiveId))
                    return false;

            return objective.Rule switch
            {
                MissionObjectiveRuleKind.DestroyMissionRole or MissionObjectiveRuleKind.ProtectMissionRole or
                    MissionObjectiveRuleKind.DefendMissionRole =>
                    !objective.MissionRoleId.IsEmpty && objective.TargetConfigId.IsEmpty,
                MissionObjectiveRuleKind.BuildStructure or MissionObjectiveRuleKind.ProduceUnit =>
                    objective.MissionRoleId.IsEmpty && !objective.TargetConfigId.IsEmpty,
                _ => false
            };
        }

        private static bool SameAttempt(
            in CampaignMissionResultComponent left, in CampaignMissionResultComponent right) =>
            left.MissionId.Equals(right.MissionId) && left.SessionToken.Equals(right.SessionToken) &&
            left.AttemptOrdinal == right.AttemptOrdinal;
    }

    internal static class CampaignMissionResultDebriefTransitionUtility
    {
        private static readonly FixedString64Bytes ResultNotSettledReason = "result-not-settled";
        private static readonly FixedString64Bytes InvalidResultTransitionReason =
            "invalid-result-transition";

        internal static bool TryContinueResult(
            EntityManager entityManager,
            Entity root,
            ref CampaignMissionRuntimeComponent runtime,
            out FixedString64Bytes reason)
        {
            reason = default;
            if (runtime.Phase == MissionPhaseKind.ResultAfterDebrief)
                return TryTransition(MissionPhaseKind.ReturnReplay, ref runtime, out reason);
            if (runtime.Outcome != MissionOutcomeKind.Victory ||
                !entityManager.HasBuffer<CampaignMissionSettlementResultElement>(root))
            {
                reason = ResultNotSettledReason;
                return false;
            }

            DynamicBuffer<CampaignMissionSettlementResultElement> settlements =
                entityManager.GetBuffer<CampaignMissionSettlementResultElement>(root, true);
            for (int index = settlements.Length - 1; index >= 0; index--)
            {
                CampaignMissionSettlementResultElement candidate = settlements[index];
                if (candidate.SourceVersion != runtime.Version ||
                    !candidate.SessionToken.Equals(runtime.SessionToken) || candidate.Accepted == 0)
                {
                    continue;
                }
                MissionPhaseKind phase = candidate.FirstClear != 0
                    ? MissionPhaseKind.DebriefFirstClear
                    : MissionPhaseKind.ReturnReplay;
                return TryTransition(phase, ref runtime, out reason);
            }

            reason = ResultNotSettledReason;
            return false;
        }

        internal static bool TryQueueFirstClearDebrief(
            EntityManager entityManager,
            EntityQuery rootQuery,
            in FixedString64Bytes requiredMissionId)
        {
            if (rootQuery.CalculateEntityCount() != 1)
                return false;
            Entity root = rootQuery.GetSingletonEntity();
            if (!entityManager.HasComponent<CampaignMissionResultComponent>(root) ||
                !entityManager.HasBuffer<CampaignMissionSettlementResultElement>(root) ||
                !entityManager.HasBuffer<CampaignMissionActionRequestElement>(root))
            {
                return false;
            }

            CampaignMissionRuntimeComponent runtime =
                entityManager.GetComponentData<CampaignMissionRuntimeComponent>(root);
            if (!runtime.MissionId.Equals(requiredMissionId) ||
                runtime.Phase != MissionPhaseKind.Result ||
                runtime.Outcome != MissionOutcomeKind.Victory)
            {
                return false;
            }

            CampaignMissionResultComponent result =
                entityManager.GetComponentData<CampaignMissionResultComponent>(root);
            if (!result.SessionToken.Equals(runtime.SessionToken) ||
                result.AttemptOrdinal != runtime.AttemptOrdinal ||
                result.Outcome != runtime.Outcome ||
                !HasAcceptedFirstClearSettlement(entityManager, root, in result) ||
                !HasDebriefSequence(entityManager, root, in runtime))
            {
                return false;
            }

            DynamicBuffer<CampaignMissionActionRequestElement> requests =
                entityManager.GetBuffer<CampaignMissionActionRequestElement>(root);
            for (int index = 0; index < requests.Length; index++)
            {
                CampaignMissionActionRequestElement pending = requests[index];
                if (pending.Action == MissionActionKind.Continue &&
                    pending.TransitionToken == runtime.TransitionToken &&
                    pending.SessionToken.Equals(runtime.SessionToken) &&
                    pending.AttemptOrdinal == runtime.AttemptOrdinal)
                {
                    return true;
                }
            }

            requests.Add(new CampaignMissionActionRequestElement
            {
                Action = MissionActionKind.Continue,
                TransitionToken = runtime.TransitionToken,
                SessionToken = runtime.SessionToken,
                AttemptOrdinal = runtime.AttemptOrdinal,
                ReplayTutorialEnabled = runtime.ReplayTutorialEnabled
            });
            return true;
        }

        private static bool HasAcceptedFirstClearSettlement(
            EntityManager entityManager,
            Entity root,
            in CampaignMissionResultComponent result)
        {
            DynamicBuffer<CampaignMissionSettlementResultElement> settlements =
                entityManager.GetBuffer<CampaignMissionSettlementResultElement>(root, true);
            for (int index = settlements.Length - 1; index >= 0; index--)
            {
                CampaignMissionSettlementResultElement candidate = settlements[index];
                if (candidate.SourceVersion == result.SourceVersion &&
                    candidate.SessionToken.Equals(result.SessionToken) && candidate.Accepted != 0)
                {
                    return candidate.FirstClear != 0;
                }
            }
            return false;
        }

        private static bool HasDebriefSequence(
            EntityManager entityManager,
            Entity root,
            in CampaignMissionRuntimeComponent runtime)
        {
            CampaignMissionCatalogComponent catalog =
                entityManager.GetComponentData<CampaignMissionCatalogComponent>(root);
            return CampaignMissionSpawnSystem.TryFindDefinition(
                       in catalog, in runtime, out int definitionIndex) &&
                   !catalog.Blob.Value.Missions[definitionIndex].DebriefSequenceId.IsEmpty;
        }

        private static bool TryTransition(
            MissionPhaseKind phase,
            ref CampaignMissionRuntimeComponent runtime,
            out FixedString64Bytes reason)
        {
            CampaignMissionRuntimeComponent current = runtime;
            if (CampaignMissionRuntimeSystem.TryTransition(
                    in current,
                    phase,
                    current.Outcome,
                    current.ReturnDestination,
                    out runtime))
            {
                reason = default;
                return true;
            }

            reason = InvalidResultTransitionReason;
            return false;
        }
    }
}
