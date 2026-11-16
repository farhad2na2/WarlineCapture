using System;
using Game.Components;
using Game.Configs;
using Game.Simulation;
using Game.Skirmish.Contracts;
using Newtonsoft.Json.Linq;
using Unity.Entities;
using Unity.Mathematics;

namespace Game.Runtime
{
    // Source-only bridge to the same gameplay decision used by the native planner.
    // It covers selected shared policy subtrees and does not emulate UI.
    internal sealed class SourceS003AirOpeningController
    {
        readonly EntityManager em;
        readonly Entity session;
        readonly SkirmishArmyProfileConfig army;
        readonly SourceAriaRecruitmentBoundary paid;
        readonly SourceCommandDispatch commands;
        readonly SourcePublicPlayerContactOwner contacts;
        readonly SourceAriaObjectiveApproach approach;
        readonly string worldId;
        AriaPreparationState state;
        AriaGroundColumnState groundState;
        Entity lastObjective;
        int objectiveIdentity, commandSequence;
        bool logicalAttackMode;
        long lastTick = -1;
        bool stopped;

        internal SourceS003AirOpeningController(EntityManager em, Entity session,
            SkirmishArmyProfileConfig army, SourceAriaRecruitmentBoundary paid, string worldId, GroundMissionContext context)
        {
            this.em = em ?? throw new ArgumentNullException(nameof(em));
            this.session = session;
            this.army = army ?? throw new ArgumentNullException(nameof(army));
            this.paid = paid ?? throw new ArgumentNullException(nameof(paid));
            this.worldId = worldId ?? throw new ArgumentNullException(nameof(worldId));
            if (!ReferenceEquals(paid.WorldIdentity, em) || paid.SessionIdentity != session ||
                !em.Exists(session) || !em.BankBound)
                throw new InvalidOperationException("Same actual source world, session and paid owner required.");
            commands = new SourceCommandDispatch(em, session, army);
            contacts = new SourcePublicPlayerContactOwner(em, session, true);
            approach = new SourceAriaObjectiveApproach(em, session, contacts, context);
        }

        internal JObject Step(long tick, int frame, float time, float productionTime)
        {
            if (stopped || tick <= lastTick || frame <= 0 || !float.IsFinite(time) || !float.IsFinite(productionTime))
                throw new InvalidOperationException("Active ordered source gameplay clock required.");
            lastTick = tick;
            long readAt = System.Diagnostics.Stopwatch.GetTimestamp();
            var sessionState = em.GetComponentData<SkirmishExpandedSessionComponent>(session);
            var match = em.GetComponentData<SkirmishMatchState>(session);
            if (sessionState.CatalogId.ToString() != "S003" || sessionState.Phase != SkirmishSessionPhase.Playing ||
                sessionState.SpawnComplete == 0 || match.ScenarioIndex != 5 || match.Phase != SkirmishPhase.Playing ||
                match.Outcome != SkirmishOutcome.None)
                throw new InvalidOperationException("Actual active S003 source session required.");
            var view = SkirmishAriaPublicProjection.FromSession(em, session, army);
            if (!view.AirProfile || view.Finished)
                throw new InvalidOperationException("Actual active air profile required.");
            var offer = paid.Read(SkirmishRoleKind.Rifle);
            var observation = new AriaPreparationView
            {
                AirProfile = view.AirProfile,
                Infantry = view.OwnInfantry,
                Time = time,
                RifleRecruitPending = view.RifleRecruitPending,
                CanAffordRifle = view.CanAffordRifle
            };
            var logistics = paid.Read(SkirmishRoleKind.LogisticsTruck);
            var tanker = paid.Read(SkirmishRoleKind.Tanker);
            if (!em.HasComponent<SkirmishResolvedSetupRecord>(session))
                throw new NotSupportedException("Same-session resolved setup required for public army profile.");
            var resolved = em.GetComponentObject<SkirmishResolvedSetupRecord>(session).Setup;
            if (resolved == null)
                throw new NotSupportedException("Same-session resolved setup is missing.");
            // Match UiShellEcsGateway's public projection from the actual setup.
            observation.CombinedArmsField = resolved.ArmyProfileId == SkirmishArmyProfileId.CombinedArms &&
                                            resolved.StartPackageId == SkirmishStartPackageId.FieldBase;
            observation.OwnStructureAttackUnits = view.OwnStructureAttackUnits;
            observation.LogisticsTruckCommitted = view.LogisticsTruckCommitted;
            observation.CanAffordLogisticsTruck = view.CanAffordLogisticsTruck;
            observation.LogisticsTargetAvailable = logistics.Present && logistics.CanRequest;
            observation.TankersCommitted = view.TankersCommitted;
            observation.CanAffordTanker = view.CanAffordTanker;
            observation.TankerTargetAvailable = tanker.Present && tanker.CanRequest;
            double readSeconds = (System.Diagnostics.Stopwatch.GetTimestamp() - readAt) /
                                 (double)System.Diagnostics.Stopwatch.Frequency;
            long policyAt = System.Diagnostics.Stopwatch.GetTimestamp();
            var supply = AriaExpandedPreparationPolicy.Supply(observation, ref state);
            var decision = supply == AriaPreparationAction.None
                ? AriaExpandedPreparationPolicy.AirOpening(observation, ref state) : AriaAirOpeningAction.NotApplicable;
            double policySeconds = (System.Diagnostics.Stopwatch.GetTimestamp() - policyAt) /
                                   (double)System.Diagnostics.Stopwatch.Frequency;
            long dispatchAt = System.Diagnostics.Stopwatch.GetTimestamp();
            JObject receipt = null;
            string action = supply != AriaPreparationAction.None ? supply.ToString() : decision.ToString();
            if (supply == AriaPreparationAction.RecruitLogisticsTruck || supply == AriaPreparationAction.RecruitTanker)
            {
                var role = supply == AriaPreparationAction.RecruitLogisticsTruck ? SkirmishRoleKind.LogisticsTruck : SkirmishRoleKind.Tanker;
                var actual = paid.Request(role, productionTime);
                receipt = new JObject { ["accepted"] = actual.Accepted, ["source_reason"] = actual.SourceReason,
                    ["source_receipt_id"] = actual.ReceiptId, ["delivery_complete"] = false,
                    ["source_api"] = "Typed unit semantic request -> SourceProductionDeliveryOwner.TryQueue" };
            }
            else if (decision == AriaAirOpeningAction.RecruitRifle && offer.Present && offer.CanRequest)
            {
                var actual = paid.Request(SkirmishRoleKind.Rifle, productionTime);
                receipt = new JObject { ["accepted"] = actual.Accepted, ["source_reason"] = actual.SourceReason,
                    ["source_receipt_id"] = actual.ReceiptId, ["delivery_complete"] = false,
                    ["source_api"] = "Typed unit semantic request -> SourceProductionDeliveryOwner.TryQueue" };
            }
            else if (supply == AriaPreparationAction.None && state.AssaultStarted != 0)
                receipt = StepGround(tick, frame, time, match, ref action);
            double dispatchSeconds = (System.Diagnostics.Stopwatch.GetTimestamp() - dispatchAt) /
                                     (double)System.Diagnostics.Stopwatch.Frequency;
            return new JObject {
                ["aria_profile_seconds"] = new JObject { ["observation"] = readSeconds, ["policy"] = policySeconds,
                    ["dispatch"] = dispatchSeconds, ["observation_trace_format"] = 0.0 },
                ["logical_policy"] = "OriginalSharedAirOpeningSupplyAndGroundColumn", ["scope"] = "SourceGameplayOnly_PartialPlanner_PriorityClosureUnproven",
                ["native_rendered_input"] = false, ["full_policy_loaded"] = false, ["full_strategy"] = false,
                ["frame"] = frame, ["time"] = time, ["action"] = action, ["executed"] = receipt != null,
                ["observation"] = new JObject { ["air_profile"] = observation.AirProfile,
                    ["combined_arms_field"] = observation.CombinedArmsField, ["infantry"] = observation.Infantry,
                    ["rifle_pending"] = observation.RifleRecruitPending, ["can_afford_rifle"] = observation.CanAffordRifle,
                    ["offer_present"] = offer.Present, ["offer_can_request"] = offer.CanRequest, ["offer_reason"] = offer.Reason,
                    ["selected_approach"] = action == "GroundObjective_InspectObjective"
                        ? JObject.FromObject(approach.LastSelectedOwnProjection) : null },
                ["opening_until"] = state.OpeningUntil, ["assault_started"] = state.AssaultStarted,
                ["receipt"] = receipt, ["stopped"] = stopped, ["mission_ready"] = false };
        }

        JObject StepGround(long tick, int frame, float time, SkirmishMatchState match, ref string action)
        {
            var current = PublicCommandCatalog.Read(em, session, army);
            var page = current.Page;
            if (!current.Active || !page.Expanded || !page.BaseAssault)
                throw new NotSupportedException("Actual active expanded public roster required.");
            contacts.UpdateAfterFog(frame, time);
            var objective = SkirmishPublicObjectiveProjection.Read(em, session);
            if (!objective.EnemyObjectiveAlive) return null;
            if (match.EnemyMainBase != lastObjective) { lastObjective = match.EnemyMainBase; objectiveIdentity = checked(objectiveIdentity + 1); }
            var goal = SkirmishAcceptedArmyOrders.PublicGoal(em, session, approach.PublicGoalKind, objectiveIdentity,
                objective.EnemyPosition.X, objective.EnemyPosition.Z);
            int acceptedMask = SkirmishAcceptedArmyOrders.ReadMask(em, session, goal, page.AttackOrderMask,
                page.Group0, page.Group1, page.Group2, page.Group3, page.Group4);
            if (logicalAttackMode && current.HasSelectedLivingPlayerMember)
            {
                Entity visible = Entity.Null;
                foreach (var target in current.Targets)
                    if (target.Entity == match.EnemyMainBase && target.PublicKnowledge.HasValue &&
                        target.PublicKnowledge.Value.Sight == SkirmishContactSight.Visible) { visible = target.Entity; break; }
                var terrain = visible == Entity.Null ? approach.Read() : null;
                var selected = AriaGroundObjectivePolicy.Contact(visible != Entity.Null, terrain.HasValue);
                action = "GroundObjective_" + selected;
                if (selected == AriaObjectiveContact.VisibleEnemyTarget)
                {
                    logicalAttackMode = false;
                    return Dispatch(tick, "army/attack/" + visible.Index + ":" + visible.Version);
                }
                if (selected == AriaObjectiveContact.PublicTerrainApproach)
                {
                    var actual = terrain.Value;
                    using var goalScope = AriaCommandEvidence.EnterLogicalGoal("source-shared-" + checked(commandSequence + 1), goal);
                    bool evidence = Environment.GetEnvironmentVariable("WARLINE_S003_SELECTED_ORDER_EVIDENCE") == "1";
                    var before = evidence ? SourceMissionValidatorSnapshot.SelectedForIssueSelected(em) : null;
                    var result = AttackMoveSystem.IssueSelected(em, actual.Cell, actual.Position, frame);
                    var after = evidence ? SourceMissionValidatorSnapshot.SelectedForIssueSelected(em) : null;
                    commandSequence++;
                    logicalAttackMode = false;
                    return new JObject { ["accepted"] = result.Accepted, ["source_reason"] = result.ReasonCode.ToString(),
                        ["source_api"] = "AttackMoveSystem.IssueSelected", ["source_result"] = JObject.FromObject(result),
                        ["argument_cell"] = new JArray(actual.Cell.x, actual.Cell.y), ["native_ui_binding"] = "UNKNOWN",
                        ["validator_only_dispatch"] = evidence ? new JObject { ["feeds_aria"] = false,
                            ["selected_query_before"] = before, ["selected_query_after"] = after,
                            ["accepted_member_count_available"] = false } : null };
                }
                return null;
            }
            uint selectedGroup = 0;
            // The native selected-card target searches the whole roster and can
            // scroll to an off-page selection. Resolve that same logical group
            // directly in this headless host; no presentation swipe is implied.
            if (em.HasBuffer<SkirmishArmyGroupRecord>(session))
            {
                var groups = em.GetBuffer<SkirmishArmyGroupRecord>(session);
                for (int i = 0; i < groups.Length; i++)
                    if (groups[i].FactionId == 1 && groups[i].AliveCount > 0 &&
                        SkirmishArmySelectionService.HasSelectedLivingPlayerMember(em, session, groups[i].GroupId))
                    { selectedGroup = groups[i].GroupId; break; }
            }
            var facts = new AriaGroundColumnView {
                ExpandedPageCount = page.PageCount, ExpandedPageIndex = page.PageIndex,
                ExpandedTotalGroups = page.TotalGroups, EnemyDesignatedAlive = current.PublicView.EnemyDesignatedAlive,
                ExpandedCanPreviousPage = page.PreviousPage, ExpandedCanNextPage = page.NextPage,
                ExpandedSelectedGroups = page.SelectedGroups, ExpandedAssaultMask = page.AssaultMask,
                ExpandedAttackOrderMask = acceptedMask, ExpandedSelectedMask = page.SelectedMask,
                ExpandedStructureMask = page.StructureMask, PreviousAvailable = page.PreviousPage,
                NextAvailable = page.NextPage, ToggleAvailable = selectedGroup != 0,
                AttackAvailable = current.HasSelectedLivingPlayerMember };
            for (int i = 0; i < 5; i++) if (page.Group(i) != 0) facts.AvailableSquadMask |= 1 << i;
            if (!AriaGroundColumnPolicy.StepGroundAssault(facts, ref groundState, out var decision)) return null;
            action = "GroundColumn_" + decision.Action;
            switch (decision.Action)
            {
                case AriaGroundAction.SelectSlot: return Dispatch(tick, "presented/slot/" + decision.Slot);
                case AriaGroundAction.PreviousPage: return Dispatch(tick, "presented/page/-1");
                case AriaGroundAction.NextPage: return Dispatch(tick, "presented/page/+1");
                case AriaGroundAction.ToggleSelected: return Dispatch(tick, "presented/toggle/" + selectedGroup);
                case AriaGroundAction.ArmAttack: logicalAttackMode = true; return null;
                default: return null;
            }
        }

        JObject Dispatch(long tick, string control) => commands.Dispatch(worldId, tick,
            "source-shared-" + checked(++commandSequence), control, tick);

        internal void Stop() => stopped = true;
    }
}
