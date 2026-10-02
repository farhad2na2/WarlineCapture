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
        private static readonly FixedString64Bytes CitywideResultMissionId = CampaignMissionSequence.CitywideAlert;
        private static readonly FixedString64Bytes CitywideResultResponseRole = "role.friendly.response.clinic";
        private static readonly FixedString64Bytes ArmorBreakMissionId = CampaignMissionSequence.ArmorBreak;
        private static readonly FixedString64Bytes ArmorBreakMilitaryRole = "role.hostile.command";
        private static readonly FixedString64Bytes ArmorBreakRecoveryRole = "role.friendly.command_squad";
        private static readonly FixedString64Bytes ArmorBreakReliefRole = "role.civilian.protected";
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
                    facts.ForwardPostDamaged, ref definition.StarRules, out byte stars, facts.BreachSupportLost))
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
            => TryEvaluateStars(outcome, elapsedMilliseconds, squadLossCount, civilianLossCount, 0, ref rules, out stars);

        internal static bool TryEvaluateStars(
            MissionOutcomeKind outcome, int elapsedMilliseconds, int squadLossCount, int civilianLossCount,
            byte postDamaged, ref BlobArray<CampaignMissionStarRuleBlob> rules, out byte stars, byte supportLost = 0)
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
                    MissionStarRuleKind.BreachSupportSurvives => outcome == MissionOutcomeKind.Victory && supportLost == 0,
                    MissionStarRuleKind.NoPostDamage => outcome == MissionOutcomeKind.Victory && postDamaged == 0,
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
            if (definition.RouteReopened.Enabled != 0)
                return facts.RouteReopenedFailure != RouteReopenedFailure.Integrity && (outcome == MissionOutcomeKind.Victory
                    ? facts.RouteReopenedFailure == RouteReopenedFailure.None && facts.RouteReliefDelivered != 0 && facts.RouteFuelDelivered != 0 &&
                      facts.RouteLinkRestored != 0 && facts.RouteHubEntered != 0 && facts.RouteGarrisonCleared != 0 && facts.RouteRecordsPreserved != 0 &&
                      facts.HostileTotalCount > 0 && facts.HostileDefeatedCount >= facts.HostileTotalCount
                    : facts.RouteReopenedFailure != RouteReopenedFailure.None);
            if (definition.PowerRelay.Enabled != 0)
                return facts.PowerRelayFailure != PowerRelayFailure.Integrity && (outcome == MissionOutcomeKind.Victory
                    ? facts.PowerRelayFailure == PowerRelayFailure.None && facts.PowerSafeRouteConfirmed != 0 &&
                      facts.PowerFamiliesSheltered != 0 && facts.PowerRestored != 0 && facts.PowerRelaySecured != 0 &&
                      facts.HostileTotalCount > 0 && facts.HostileDefeatedCount >= facts.HostileTotalCount
                    : facts.PowerRelayFailure != PowerRelayFailure.None);
            if (definition.MarketLifeline.Enabled != 0)
                return facts.MarketFailure != MarketLifelineFailure.Integrity && (outcome == MissionOutcomeKind.Victory
                    ? facts.MarketFailure == MarketLifelineFailure.None && facts.MarketReliefDelivered != 0 &&
                      facts.MarketManifestVerified != 0 && facts.MarketOpen != 0 && facts.HostileTotalCount > 0 &&
                      facts.HostileDefeatedCount >= facts.HostileTotalCount
                    : facts.MarketFailure != MarketLifelineFailure.None);
            if (definition.SupplyLine.Enabled != 0)
                return facts.SupplyFailure != SupplyLineFailure.Integrity && (outcome == MissionOutcomeKind.Victory
                    ? facts.SupplyFailure == SupplyLineFailure.None && facts.SupplyOilTransferred!=0 && facts.SupplyFuelTransferred!=0 && facts.SupplyReserveComplete!=0
                    : facts.SupplyFailure != SupplyLineFailure.None);
            if (definition.Gridlock.Enabled != 0)
                return facts.GridlockFailure != GridlockFailure.Integrity && (outcome == MissionOutcomeKind.Victory
                    ? facts.GridlockFailure == GridlockFailure.None && facts.GridlockDelivered != 0 && facts.GridlockSiteAComplete != 0 && facts.GridlockSiteBComplete != 0
                    : facts.GridlockFailure != GridlockFailure.None);
            if (definition.Breach.Enabled != 0)
                return outcome == MissionOutcomeKind.Victory ? CampaignMissionBreachRuleUtility.IsVictory(in facts) : CampaignMissionBreachRuleUtility.IsFailure(in facts);
            if (definition.Extraction.Enabled != 0)
                return outcome == MissionOutcomeKind.Victory
                    ? CampaignMissionExtractionRuleUtility.IsVictory(in facts, definition.Extraction.RequiredPassengers)
                    : CampaignMissionExtractionRuleUtility.IsFailure(in facts);
            // Armor Break uses the defense transport/roster contract, but its victory is
            // combined-arms mastery and authority custody, not a forward-post defense.
            if (definition.MissionId.Equals(ArmorBreakMissionId))
                return ArmorBreakFactsMatchOutcome(outcome, in facts, ref definition);
            if (definition.MissionId.Equals(CitywideResultMissionId))
                return CitywideFactsMatchOutcome(outcome,in facts,ref definition);
            if (definition.Defense.Enabled != 0)
                return outcome == MissionOutcomeKind.Victory
                    ? CampaignMissionDefenseRuleUtility.IsVictory(in facts)
                    : CampaignMissionDefenseRuleUtility.IsFailure(in facts);
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

        private static bool CitywideFactsMatchOutcome(MissionOutcomeKind outcome,in CampaignMissionAttemptFactsComponent facts,ref CampaignMissionDefinitionBlob definition)
        {
            if (definition.Defense.Enabled==0 || definition.Objectives.Length!=3 ||
                definition.Objectives[0].Rule!=MissionObjectiveRuleKind.StabilizeCitywideClinic || definition.Objectives[0].RequiredCount!=2 ||
                !definition.Objectives[0].MissionRoleId.Equals(ArmorBreakReliefRole) || !definition.Objectives[0].TargetConfigId.IsEmpty ||
                definition.Objectives[1].Rule!=MissionObjectiveRuleKind.StabilizeCitywideUtility || definition.Objectives[1].RequiredCount!=2 ||
                !definition.Objectives[1].MissionRoleId.Equals(ArmorBreakReliefRole) || !definition.Objectives[1].TargetConfigId.IsEmpty ||
                definition.Objectives[2].Rule!=MissionObjectiveRuleKind.EstablishCitywidePerimeter || definition.Objectives[2].RequiredCount!=11 ||
                !definition.Objectives[2].MissionRoleId.Equals(CitywideResultResponseRole) || !definition.Objectives[2].TargetConfigId.IsEmpty ||
                facts.HostileRosterIntegrityFault!=0 || facts.CitywideFailure==CitywideAlertFailure.Integrity) return false;
            if (outcome==MissionOutcomeKind.Defeat) return facts.CitywideFailure!=CitywideAlertFailure.None;
            return outcome==MissionOutcomeKind.Victory && facts.CitywideFailure==CitywideAlertFailure.None && facts.CitywideClinicRecovered!=0 &&
                facts.CitywideUtilityRecovered!=0 && facts.CitywideReinforcementReady!=0 && facts.CitywideCoverageReady!=0 && facts.CitywideAirCleared!=0 &&
                facts.CitywideMilitaryCleared!=0 && facts.CitywideStableMilliseconds>=CampaignMissionCitywideAlertRuleUtility.HoldMilliseconds &&
                facts.HostileTotalCount==11 && facts.HostileDefeatedCount==11 && facts.CommandSquadSpawned!=0 && facts.CommandSquadAlive!=0 &&
                facts.CivilianTotalCount==4 && facts.CivilianLossCount==0;
        }

        private static bool ArmorBreakFactsMatchOutcome(
            MissionOutcomeKind outcome, in CampaignMissionAttemptFactsComponent facts,
            ref CampaignMissionDefinitionBlob definition)
        {
            if (definition.Defense.Enabled == 0 || definition.Objectives.Length != 3 ||
                definition.Objectives[0].Rule != MissionObjectiveRuleKind.DefeatArmorBreakMilitary ||
                definition.Objectives[0].RequiredCount != 12 || !definition.Objectives[0].MissionRoleId.Equals(ArmorBreakMilitaryRole) ||
                definition.Objectives[1].Rule != MissionObjectiveRuleKind.RecoverArmorBreakAuthority ||
                definition.Objectives[1].RequiredCount != 4 || !definition.Objectives[1].MissionRoleId.Equals(ArmorBreakRecoveryRole) ||
                definition.Objectives[2].Rule != MissionObjectiveRuleKind.ProtectArmorBreakRelief ||
                definition.Objectives[2].RequiredCount != 3 || !definition.Objectives[2].MissionRoleId.Equals(ArmorBreakReliefRole) ||
                facts.HostileRosterIntegrityFault != 0 || facts.ArmorBreakFailure == ArmorBreakFailure.Integrity)
                return false;
            if (outcome == MissionOutcomeKind.Defeat)
                return facts.ArmorBreakFailure != ArmorBreakFailure.None;
            return outcome == MissionOutcomeKind.Victory && facts.ArmorBreakFailure == ArmorBreakFailure.None &&
                facts.ArmorBreakCoverageReady != 0 && facts.ArmorBreakAirCleared != 0 &&
                facts.ArmorBreakHeavyDisabled != 0 && facts.ArmorBreakLauncherShots > 0 &&
                facts.ArmorBreakAircraftUsed != 0 && facts.ArmorBreakArmoredThreatsCleared != 0 &&
                facts.ArmorBreakArmorApproached != 0 && facts.ArmorBreakCommandDisabled != 0 &&
                facts.ArmorBreakAuthorityRecovered != 0 &&
                facts.ArmorBreakRecoveryMilliseconds >= CampaignMissionArmorBreakRuleUtility.HoldMilliseconds &&
                facts.HostileTotalCount == 12 && facts.HostileDefeatedCount == 12 &&
                facts.CommandSquadSpawned != 0 && facts.CommandSquadAlive != 0 &&
                facts.CivilianTotalCount == 3 && facts.CivilianLossCount == 0 && facts.ArmorBreakReliefLost == 0;
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
        private static readonly FixedString64Bytes EstablishBaseMissionId =
            "saga.ch01.m02.establish_base";
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
                MissionPhaseKind phase = candidate.FirstClear != 0 ||
                                         CampaignMissionNarrativePolicy.UsesMissionSequences(runtime.MissionId)
                    ? MissionPhaseKind.DebriefFirstClear
                    : MissionPhaseKind.ReturnReplay;
                return TryTransition(phase, ref runtime, out reason);
            }

            reason = ResultNotSettledReason;
            return false;
        }

        internal static bool TryQueueDebrief(EntityManager entityManager, EntityQuery rootQuery)
        {
            if (rootQuery.CalculateEntityCount() != 1) return false;
            var runtime = entityManager.GetComponentData<CampaignMissionRuntimeComponent>(rootQuery.GetSingletonEntity());
            return CampaignMissionNarrativePolicy.UsesMissionSequences(runtime.MissionId) &&
                TryQueueDebrief(entityManager, rootQuery, runtime.MissionId);
        }

        internal static bool TryQueueDebrief(
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
                runtime.Outcome != MissionOutcomeKind.Victory ||
                CampaignMissionFinalKillCinematicSystem.IsHoldingMissionResult(entityManager, root))
            {
                return false;
            }

            CampaignMissionResultComponent result =
                entityManager.GetComponentData<CampaignMissionResultComponent>(root);
            if (!result.SessionToken.Equals(runtime.SessionToken) ||
                result.AttemptOrdinal != runtime.AttemptOrdinal ||
                result.Outcome != runtime.Outcome ||
                !HasAcceptedSettlement(entityManager, root, in result) ||
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

        private static bool HasAcceptedSettlement(
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
                    return true;
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
