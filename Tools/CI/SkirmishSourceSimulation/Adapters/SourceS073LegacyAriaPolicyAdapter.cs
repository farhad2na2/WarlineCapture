using System;
using Game.Components;
using Game.Configs;
using Game.Simulation;
using Game.Skirmish.Contracts;
using Game.Tactical.Contracts;
using Unity.Entities;
using Newtonsoft.Json.Linq;

namespace Game.Runtime
{
    // Thin legacy adapter for the existing shared ground-assault ARIA policy.
    // It only forwards policy actions to the original legacy selection and
    // selected-unit movement owners; it does not create readiness or lifecycle state.
    internal sealed partial class AutonomousWorld
    {
        private AriaS002PlanState s073LegacyAriaPlan;
        private AriaS002ContactState s073LegacyAriaContact;
        private bool s073LegacyAriaAttackMode;
        private bool s073LegacyAriaStopped;
        private bool s073LegacyAriaBusy;
        private long s073LegacyAriaLastTick = -1;
        private float s073LegacyAriaLastTime = float.NegativeInfinity;

        internal JObject StepS073LegacyAriaPolicy(
            SkirmishBattleCatalogConfig canonicalCatalog, int frame, float ariaTime)
        {
            if (s073LegacyAriaStopped || s073LegacyAriaBusy || frame <= 0 ||
                !float.IsFinite(ariaTime) || ariaTime < s073LegacyAriaLastTime || Tick <= s073LegacyAriaLastTick)
                throw new InvalidOperationException("S073 ARIA adapter is stopped/reentrant/repeated or has invalid source clocks");

            s073LegacyAriaBusy = true;
            try
            {
                // This source-owned read fails closed until the actual legacy
                // session is Playing and the public fog/objective facts exist.
                var view = ReadS073LegacyAriaPolicyObservation(
                    canonicalCatalog, frame, ariaTime, s073LegacyAriaAttackMode,
                    ReadS073LegacyAcceptedAttackOrderMask(), out var advanceCell, out var advancePosition);

                SourceS002ContactCompletion.Observe(ref s073LegacyAriaContact, view.Time);

                AriaGroundBaseAssaultPolicy.Step(
                    s073LegacyAriaContext, view, ref s073LegacyAriaPlan,
                    ref s073LegacyAriaContact, out var decision);

                s073LegacyAriaLastTick = Tick;
                s073LegacyAriaLastTime = ariaTime;
                JObject sourceResult = null;
                string sourceApi = null;
                bool? accepted = null;

                if (decision.Kind == AriaS002DecisionKind.Finished ||
                    s073LegacyAriaContact.Phase == AriaS002ContactPhase.Blocked)
                {
                    s073LegacyAriaStopped = true;
                }
                else if (decision.Action != AriaS002Action.None &&
                    decision.Kind is AriaS002DecisionKind.Control or AriaS002DecisionKind.WorldTarget)
                {
                    switch (decision.Action)
                    {
                        case AriaS002Action.SelectSquad:
                        {
                            if (decision.Slot < 0 || decision.Slot > 4)
                                throw new NotSupportedException("Shared ARIA selected a slot outside the legacy five-slot roster");
                            sourceApi = "SkirmishArmySelectionService.TrySelectLegacySquad";
                            sourceResult = SelectS073LegacySquad(Entities, Session, decision.Slot, out bool selected);
                            accepted = selected;
                            break;
                        }
                        case AriaS002Action.ArmAttack:
                            s073LegacyAriaAttackMode = true;
                            sourceApi = "SharedAriaGroundPolicy.AttackMode";
                            accepted = true;
                            sourceResult = new JObject { ["attack_mode"] = true, ["troop_order"] = false };
                            break;
                        case AriaS002Action.AttackPublicTerrain:
                        {
                            if (!s073LegacyAriaAttackMode)
                                throw new InvalidOperationException("Shared ARIA terrain order requires its actual attack-mode step");
                            sourceApi = "AttackMoveSystem.IssueSelected";
                            sourceResult = IssueS073LegacySelectedAttackMove(
                                Entities, advanceCell, advancePosition, frame, out bool moved);
                            accepted = moved;
                            s073LegacyAriaAttackMode = false;
                            break;
                        }
                        default:
                            // Camera/UI and unsupported legacy actions remain absent
                            // until their actual source presentation owner is bound.
                            throw new NotSupportedException("S073 legacy ARIA action has no admitted native owner: " + decision.Action);
                    }

                    if (!CompleteS073LegacyAcceptedAction(ref s073LegacyAriaContact,
                            decision.TargetId, frame, view.Time, accepted == true))
                    {
                        // The headless adapter has no observed native gesture to
                        // acknowledge a rejected source command. Stop this
                        // diagnostic without advancing its logical contact state.
                        s073LegacyAriaStopped = true;
                        s073LegacyAriaAttackMode = false;
                    }
                }

                return new JObject
                {
                    ["kind"] = "S073LegacySharedAriaPolicyStep",
                    ["planner_route"] = "LogicalSourceGroundBaseAssaultPolicy_NotNativeS073LegacyPlanner",
                    ["native_s073_planner_equivalence"] = false,
                    ["world_id"] = WorldId,
                    ["tick"] = Tick,
                    ["frame"] = frame,
                    ["aria_time"] = ariaTime,
                    ["decision_kind"] = decision.Kind.ToString(),
                    ["action"] = decision.Action.ToString(),
                    ["target_id"] = decision.TargetId,
                    ["source_api"] = sourceApi,
                    ["source_accepted"] = accepted,
                    ["source_result"] = sourceResult,
                    ["observation"] = JObject.FromObject(view),
                    ["scope"] = "LogicalPublicData",
                    ["source_contact_phase"] = s073LegacyAriaContact.Phase.ToString(),
                    ["completed_logical_contacts"] = s073LegacyAriaContact.Actions,
                    ["native_rendered_input"] = false,
                    ["readiness_claimed"] = false,
                    ["mission_outcome_claimed"] = false,
                    ["stopped"] = s073LegacyAriaStopped
                };
            }
            catch
            {
                s073LegacyAriaStopped = true;
                s073LegacyAriaAttackMode = false;
                throw;
            }
            finally
            {
                s073LegacyAriaBusy = false;
            }
        }

        internal static bool CompleteS073LegacyAcceptedAction(ref AriaS002ContactState contact,
            int targetId, int frame, float now, bool sourceAccepted)
        {
            if (!sourceAccepted) return false;
            contact.TargetId = targetId;
            contact.Phase = AriaS002ContactPhase.Touching;
            SourceS002ContactCompletion.Complete(ref contact, frame, now, true);
            return true;
        }

        internal static JObject SelectS073LegacySquad(
            EntityManager em, Entity session, int slot, out bool accepted)
        {
            if (em == null) throw new ArgumentNullException(nameof(em));
            if (slot < 0 || slot > 4)
                throw new NotSupportedException("Shared ARIA selected a slot outside the legacy five-slot roster");
            accepted = SkirmishArmySelectionService.TrySelectLegacySquad(
                em, session, checked((byte)slot), false, out var decision);
            return new JObject
            {
                ["source_api"] = "SkirmishArmySelectionService.TrySelectLegacySquad",
                ["accepted"] = accepted,
                ["source_result"] = JObject.FromObject(decision),
                ["slot"] = slot
            };
        }

        internal static JObject IssueS073LegacySelectedAttackMove(
            EntityManager em, Unity.Mathematics.int2 cell,
            Unity.Mathematics.float3 position, int frame, out bool accepted)
        {
            if (em == null) throw new ArgumentNullException(nameof(em));
            if (frame <= 0) throw new ArgumentOutOfRangeException(nameof(frame));
            var result = AttackMoveSystem.IssueSelected(em, cell, position, frame);
            accepted = result.Accepted;
            return new JObject
            {
                ["source_api"] = "AttackMoveSystem.IssueSelected",
                ["accepted"] = accepted,
                ["source_result"] = JObject.FromObject(result),
                ["requested_cell_x"] = cell.x,
                ["requested_cell_y"] = cell.y,
                ["requested_world_x"] = position.x,
                ["requested_world_y"] = position.y,
                ["requested_world_z"] = position.z
            };
        }

        private int ReadS073LegacyAcceptedAttackOrderMask()
        {
            int mask = 0;
            using var query = Entities.CreateEntityQuery(
                typeof(SkirmishSquadMember), typeof(Faction), typeof(UnitHealth),
                typeof(UnitGrid), typeof(AttackMoveOrder));
            using var units = query.ToEntityArray(Unity.Collections.Allocator.Temp);
            foreach (var unit in units)
            {
                if (Entities.GetComponentData<Faction>(unit).Id != FactionIdentity.PlayerFactionId ||
                    Entities.GetComponentData<UnitHealth>(unit).Current <= 0 ||
                    Entities.HasComponent<RuntimeBuildingCombatTag>(unit) ||
                    Entities.HasComponent<StaticGridBlocker>(unit) ||
                    Entities.HasComponent<UnitTransportPassenger>(unit))
                    continue;
                byte slot = Entities.GetComponentData<SkirmishSquadMember>(unit).Slot;
                if (slot > 4) throw new NotSupportedException("Accepted legacy attack order uses an unsupported squad slot");
                mask |= 1 << slot;
            }
            return mask;
        }

        internal void StopS073LegacyAriaPolicy()
        {
            s073LegacyAriaStopped = true;
            s073LegacyAriaAttackMode = false;
            s073LegacyAriaContact.Phase = AriaS002ContactPhase.Manual;
            s073LegacyAriaContact.GestureRequested = 0;
        }
    }
}
