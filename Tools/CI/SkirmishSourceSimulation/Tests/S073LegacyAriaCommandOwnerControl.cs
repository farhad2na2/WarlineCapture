using System;
using System.Runtime.Serialization;
using Game.Components;
using Game.Configs;
using Game.Runtime;
using Game.Simulation;
using Game.Skirmish.Contracts;
using Game.Tactical.Contracts;
using Newtonsoft.Json.Linq;
using Unity.Entities;
using Unity.Mathematics;

internal static class S073LegacyAriaCommandOwnerControl
{
    static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }

    // Isolated policy fixtures only: these model observations explicitly and do
    // not claim that logical catalog metadata is a native HUD observation.
    static void CheckOpeningDefenseControlAdmission()
    {
        var touch = new AriaS002ContactState { Phase = AriaS002ContactPhase.Observing };
        var unknown = new AriaS002Observation
        {
            Infantry = 16,
            DefenseControlsKnown = false,
            DefenseBuild = new AriaS002ActionTarget { Action = AriaS002Action.OpenDefensePreview, Id = 90500, Admitted = true },
            FocusPlayer = new AriaS002ActionTarget { Action = AriaS002Action.InspectHome, Id = 90001, Admitted = true }
        };
        var unknownPlan = default(AriaS002PlanState);
        var unknownOutput = default(AriaS002Decision);
        Require(AriaGroundBaseAssaultPolicy.OpeningDefense(unknown, ref unknownPlan, ref touch, ref unknownOutput) &&
                unknownOutput.Kind == AriaS002DecisionKind.Unavailable && unknownOutput.ManagedGuardFailure == 1,
            "Unknown controls must produce the original managed guard marker before any input");
        bool unknownRejected = false;
        try
        {
            AriaGroundBaseAssaultPolicy.ThrowManagedGuardFailure(unknownOutput.ManagedGuardFailure);
        }
        catch (NotSupportedException e) when (e.Message.Contains("source control availability unknown"))
        {
            unknownRejected = true;
        }
        Require(unknownRejected,
            "Catalog eligibility must not substitute for known original defense-control availability");

        var knownUnavailable = new AriaS002Observation
        {
            Infantry = 16,
            DefenseControlsKnown = true,
            DefenseBuild = new AriaS002ActionTarget { Action = AriaS002Action.OpenDefensePreview, Id = 90500, Admitted = false },
            FocusPlayer = new AriaS002ActionTarget { Action = AriaS002Action.InspectHome, Id = 90001, Admitted = false }
        };
        var unavailablePlan = default(AriaS002PlanState);
        var unavailableOutput = default(AriaS002Decision);
        Require(!AriaGroundBaseAssaultPolicy.OpeningDefense(knownUnavailable, ref unavailablePlan, ref touch, ref unavailableOutput) &&
                unavailablePlan.DefenseStage == 4,
            "Known unavailable controls must skip opening defense without issuing a fallback input");

        var knownAvailable = new AriaS002Observation
        {
            Infantry = 16,
            DefenseControlsKnown = true,
            DefenseBuild = new AriaS002ActionTarget { Action = AriaS002Action.OpenDefensePreview, Id = 90500, Admitted = true },
            FocusPlayer = new AriaS002ActionTarget { Action = AriaS002Action.InspectHome, Id = 90001, Admitted = true }
        };
        var availablePlan = default(AriaS002PlanState);
        var availableOutput = default(AriaS002Decision);
        Require(AriaGroundBaseAssaultPolicy.OpeningDefense(knownAvailable, ref availablePlan, ref touch, ref availableOutput) &&
                availableOutput.Action == AriaS002Action.InspectHome && availableOutput.TargetId == 90001,
            "An explicitly known available control should preserve the initial focus step in an isolated policy fixture");
    }

    static void Main()
    {
        CheckOpeningDefenseControlAdmission();
        int nextId = 1;
        var em = new EntityManager
        {
            LogicalAllocatorAdmitted = true,
            AllocateLogicalIdentity = () => new Entity { Index = nextId++, Version = 1 }
        };

        var match = em.CreateEntity();
        em.AddComponentData(match, new SkirmishMatchState
        {
            SessionId = new Unity.Collections.FixedString64Bytes("S073-aria-owner-fixture"),
            ScenarioIndex = 3,
            Phase = SkirmishPhase.Playing,
            StartupFailure = SkirmishStartupFailureCode.None,
            Outcome = SkirmishOutcome.None
        });

        var catalog = (SkirmishBattleCatalogConfig)FormatterServices.GetUninitializedObject(typeof(SkirmishBattleCatalogConfig));
        catalog.Configure(new[]
        {
            new SkirmishBattleCatalogEntry
            {
                ScenarioId = "S073", MapId = "IB", ObjectiveId = "BA", ArmyProfile = "G", StartProfile = "F",
                Status = SkirmishBattleCatalogStatus.Playable, PlayableScenarioIndex = 3
            }
        });
        Require(GroundMissionContextFactory.TryCreateS073LegacyLogicalPublicData(
                    catalog, em.GetComponentData<SkirmishMatchState>(match), out var context, out var contextError),
            "Source-owned S073 legacy ARIA context rejected: " + contextError);

        var squad = em.CreateEntity();
        em.AddComponentData(squad, new SkirmishSquadMember { Slot = 2 });
        em.AddComponentData(squad, new Faction { Id = FactionIdentity.PlayerFactionId });
        em.AddComponentData(squad, new UnitHealth { Current = 10, Max = 10 });
        em.AddComponentData(squad, new UnitGrid { Cell = new int2(1, 1) });
        em.AddComponentData(squad, new UnitMove { Speed = 1, WalkSpeed = 1 });

        var view = new AriaS002Observation
        {
            Scope = AriaS002Scope.LogicalPublicData,
            SourceScenarioIndex = 3,
            Active = true,
            Frame = 1,
            Time = 1f,
            EnemyDesignatedAlive = true,
            ExpandedPageCount = 1,
            ExpandedTotalGroups = 1,
            ExpandedAssaultMask = 1 << 2,
            ExpandedStructureMask = 1 << 2,
            Squad2 = new AriaS002ActionTarget
            {
                Action = AriaS002Action.SelectSquad, Id = 90102, Slot = 2, Admitted = true
            }
        };
        var plan = default(AriaS002PlanState);
        var contact = new AriaS002ContactState { Phase = AriaS002ContactPhase.Observing };
        var verifyContact = new AriaS002ContactState
        {
            Phase = AriaS002ContactPhase.Verifying,
            DueAt = 2f
        };
        SourceS002ContactCompletion.Observe(ref verifyContact, 1.99f);
        Require(verifyContact.Phase == AriaS002ContactPhase.Verifying,
            "Shared contact owner must preserve verification before due time");
        SourceS002ContactCompletion.Observe(ref verifyContact, 2f);
        Require(verifyContact.Phase == AriaS002ContactPhase.Observing,
            "Shared contact owner must return to observation at due time");
        AriaGroundBaseAssaultPolicy.Step(context, view, ref plan, ref contact, out var policyDecision);
        Require(policyDecision.Kind == AriaS002DecisionKind.Control &&
                policyDecision.Action == AriaS002Action.SelectSquad && policyDecision.Slot == 2,
            "Shared S073 policy did not emit the live admitted legacy squad slot");

        var selected = Game.Runtime.AutonomousWorld.SelectS073LegacySquad(
            em, match, policyDecision.Slot, out bool selectAccepted);
        Require(selectAccepted && (bool)selected["accepted"] && em.HasComponent<SelectedUnitTag>(squad),
            "Original legacy selection owner did not accept the live slot 2 squad");
        Require((string)selected["source_api"] == "SkirmishArmySelectionService.TrySelectLegacySquad",
            "Selection did not report the original source owner");

        SourceS002ContactCompletion.Complete(ref contact, 1, view.Time, true);
        view.Frame = 2;
        view.Time = 2f;
        view.SelectionVisible = true;
        view.SelectedCount = 1;
        view.ExpandedSelectedGroups = 1;
        view.ExpandedSelectedMask = 1 << 2;
        view.Attack = new AriaS002ActionTarget
        {
            Action = AriaS002Action.ArmAttack, Id = 90008, Admitted = true
        };
        SourceS002ContactCompletion.Observe(ref contact, view.Time);
        AriaGroundBaseAssaultPolicy.Step(context, view, ref plan, ref contact, out var armDecision);
        Require(armDecision.Kind == AriaS002DecisionKind.Control &&
                armDecision.Action == AriaS002Action.ArmAttack,
            "Shared policy did not request the visible legacy Attack control after selection");

        SourceS002ContactCompletion.Complete(ref contact, 2, view.Time, true);
        view.Frame = 3;
        view.Time = 3f;
        view.AttackMode = true;
        view.AdvanceGround = new AriaS002ActionTarget
        {
            Action = AriaS002Action.AttackPublicTerrain, Id = 90010, Admitted = true
        };
        SourceS002ContactCompletion.Observe(ref contact, view.Time);
        AriaGroundBaseAssaultPolicy.Step(context, view, ref plan, ref contact, out var moveDecision);
        Require(moveDecision.Kind == AriaS002DecisionKind.WorldTarget &&
                moveDecision.Action == AriaS002Action.AttackPublicTerrain,
            "Shared policy did not choose the actual public terrain approach for the legacy roster");

        var missing = Game.Runtime.AutonomousWorld.SelectS073LegacySquad(em, match, 4, out bool missingAccepted);
        Require(!missingAccepted && !(bool)missing["accepted"] && em.HasComponent<SelectedUnitTag>(squad),
            "Unavailable slot should be rejected without clearing the accepted selection");

        var rejectedContact = new AriaS002ContactState { Phase = AriaS002ContactPhase.Observing, Actions = 7, TargetId = 91 };
        bool rejectedCompleted = Game.Runtime.AutonomousWorld.CompleteS073LegacyAcceptedAction(
            ref rejectedContact, 123, 4, 4f, missingAccepted);
        Require(!rejectedCompleted && rejectedContact.Actions == 7 && rejectedContact.TargetId == 91 &&
                rejectedContact.Phase == AriaS002ContactPhase.Observing,
            "Rejected actual selection must not synthesize a completed logical contact");
        bool acceptedCompleted = Game.Runtime.AutonomousWorld.CompleteS073LegacyAcceptedAction(
            ref rejectedContact, 124, 5, 5f, selectAccepted);
        Require(acceptedCompleted && rejectedContact.Actions == 8 && rejectedContact.TargetId == 124 &&
                rejectedContact.Phase == AriaS002ContactPhase.Verifying,
            "Accepted source action must advance exactly one logical contact");
        var groundContact = new AriaS002ContactState { Phase = AriaS002ContactPhase.Observing, Actions = 3, TargetId = 41 };
        var groundDecision = new AriaS002Decision { Action = AriaS002Action.SelectSquad, TargetId = 42 };
        Require(!SourceS002ContactCompletion.TryCompleteLogicalAction(ref groundContact, groundDecision,
                    new JObject { ["accepted"] = false }, 6, 6f) && groundContact.Actions == 3 &&
                groundContact.TargetId == 41 && groundContact.Phase == AriaS002ContactPhase.Observing,
            "Rejected expanded source command must not become a completed contact");
        bool missingResultRejected = false;
        try { SourceS002ContactCompletion.TryCompleteLogicalAction(ref groundContact, groundDecision, new JObject(), 6, 6f); }
        catch (NotSupportedException) { missingResultRejected = true; }
        Require(missingResultRejected && groundContact.Actions == 3,
            "Source gameplay command without acceptance evidence must fail closed");
        Require(SourceS002ContactCompletion.TryCompleteLogicalAction(ref groundContact, groundDecision,
                    new JObject { ["accepted"] = true }, 6, 6f) && groundContact.Actions == 4,
            "Accepted expanded source command must complete once");

        var move = Game.Runtime.AutonomousWorld.IssueS073LegacySelectedAttackMove(
            em, default, default, 1, out bool moveAccepted);
        Require(!moveAccepted && !(bool)move["accepted"] &&
                move["source_result"]?["ReasonCode"]?.ToObject<int>() == (int)TacticalCommandReasonCode.NoSelection,
            "Original attack-move owner must truthfully reject a selected non-combat unit");
        Require((string)move["source_api"] == "AttackMoveSystem.IssueSelected",
            "Move result did not report the original source owner");

        Console.WriteLine("[S073LegacyAriaCommandOwnerControl] result=Passed shared_policy=SelectSquad,ArmAttack,AttackPublicTerrain opening_defense=unknown-rejected,known-unavailable-skipped,known-available-issued slot=2 source_selection=Accepted missing_slot=4=Rejected selected_noncombat_move=NoSelection scope=BoundedOwnerFixture_NotMapReadiness");
    }
}
