using System.Collections.Generic;
using System.Reflection;
using Game.Simulation;
using Game.Skirmish.Contracts;
using Game.UI.Contracts;
using Game.UI.Shell.Contracts.Ecs;
using Game.UI.Shell.Ecs;
using NUnit.Framework;

namespace Game.Tests.Editor
{
    // Lane 25 follow-up: FC/BT/CE gate 3 (every offered type used) and the S019 convoy escort.
    // Root causes covered: the breacher gate waited for a base-assault order objective sessions never issue
    // (so coverage never started), the 250 base-assault reserve on a FieldBase economy, coverage starved or
    // abandoned by objective orders, and CE infantry-on-truck orders whose receipt can never be credited.
    public sealed class AriaObjectiveCoverageTests
    {
        public static void RunFocusedValidation()
        {
            var t = new AriaObjectiveCoverageTests();
            t.RifleTopUpDropsToTheFloorOnceCoverageStarts();
            t.OneTimeRocketeerBuyIsBounded();
            t.ConvoyInfantryEscortsToTheDestination();
            t.ConvoyChooserOrdersOnlyTrucksToTheDestination();
            t.BreacherStartsAfterTheFirstAcceptedObjectiveOrder();
            t.CoverageUsesTheObjectiveReserveMask();
            t.CoverageInFlightFinishesBeforeObjectiveOrders();
            t.BaseAssaultCoverageKeepsItsOwnReserveMask();
            t.StartingGroupsOfEveryRoleAreEscortCandidates();
            UnityEngine.Debug.Log("[AriaObjectiveCoverage] result=Passed controls=9");
        }

        static AriaTouchTarget T(int id) => new AriaTouchTarget { Available = true, Id = id };

        // W32 (gate 3: ARIA uses every starting role on FC/BT/CE): car, APC, tank, AA, transport heli and the
        // infantry the objective chooser did not pick are all escort candidates; the chosen group, originals,
        // radar and logistics never are. The offer expires on attempt time, not elapsed time.
        [Test]
        public void StartingGroupsOfEveryRoleAreEscortCandidates()
        {
            AriaEscortCandidate C(uint id, SkirmishRoleKind role, SkirmishPopulationCategory domain) => new AriaEscortCandidate
            { GroupId = id, Role = role, Domain = domain, CanMove = true, Target = true };
            foreach (var type in new[] { AriaObjectiveType.FrontlineControl, AriaObjectiveType.Breakthrough, AriaObjectiveType.ConvoyEscort })
            {
                Assert.IsTrue(AriaObjectiveEscortPolicy.Eligible(C(6, SkirmishRoleKind.Car, SkirmishPopulationCategory.Ground), type));
                Assert.IsTrue(AriaObjectiveEscortPolicy.Eligible(C(7, SkirmishRoleKind.ApcArmored, SkirmishPopulationCategory.Ground), type));
                Assert.IsTrue(AriaObjectiveEscortPolicy.Eligible(C(8, SkirmishRoleKind.Tank, SkirmishPopulationCategory.Ground), type));
                Assert.IsTrue(AriaObjectiveEscortPolicy.Eligible(C(9, SkirmishRoleKind.AntiAir, SkirmishPopulationCategory.Ground), type));
                Assert.IsTrue(AriaObjectiveEscortPolicy.Eligible(C(10, SkirmishRoleKind.TransportHeli, SkirmishPopulationCategory.Air), type), type + " heli");
                Assert.IsTrue(AriaObjectiveEscortPolicy.Eligible(C(3, SkirmishRoleKind.Gunner, SkirmishPopulationCategory.Infantry), type), type + " gunner");
                Assert.IsTrue(AriaObjectiveEscortPolicy.Eligible(C(4, SkirmishRoleKind.Rocketeer, SkirmishPopulationCategory.Infantry), type), type + " rocketeer");
                var chosen = C(2, SkirmishRoleKind.Rifle, SkirmishPopulationCategory.Infantry); chosen.Chosen = true;
                Assert.IsFalse(AriaObjectiveEscortPolicy.Eligible(chosen, type), type + " chosen");
                Assert.IsFalse(AriaObjectiveEscortPolicy.Eligible(C(11, SkirmishRoleKind.Radar, SkirmishPopulationCategory.Ground), type));
                Assert.IsFalse(AriaObjectiveEscortPolicy.Eligible(C(12, SkirmishRoleKind.AttackHeli, SkirmishPopulationCategory.Air), type), "combat air has its own sortie path");
            }
            Assert.IsFalse(AriaObjectiveEscortPolicy.Eligible(C(10, SkirmishRoleKind.TransportHeli, SkirmishPopulationCategory.Air)), "base assault keeps vehicles only");
            Assert.IsFalse(AriaObjectiveEscortPolicy.OfferExpired(AriaObjectiveEscortPolicy.OfferSeconds));
            Assert.IsTrue(AriaObjectiveEscortPolicy.OfferExpired(AriaObjectiveEscortPolicy.OfferSeconds + .1f));
            string root = System.IO.Path.GetFullPath(System.IO.Path.Combine(ThisFile(), "..", "..", "..", ".."));
            string hud = System.IO.File.ReadAllText(System.IO.Path.Combine(root, "Game/Scripts/UI/Screens/MatchHudAssistantUiSystemHelper.ObjectiveSkill.cs"));
            Assert.IsTrue(hud.Contains("if (elapsed > 0 && frame.ExclusiveSelectedGroup == chosen.GroupId) escortAttemptSeconds += elapsed;"), "attempt-time expiry");
            Assert.IsTrue(hud.Contains("Chosen = publicObjectiveChoice.Available && publicObjectiveChoice.GroupId == policy.GroupId,"));
            // W32b (S022/S024/S040/S042 gate 3: transport_heli unused): the public own-group row lets a transport
            // helicopter move (never hold), so it can become an escort candidate at all.
            string gateway = System.IO.File.ReadAllText(System.IO.Path.Combine(root, "Game/Scripts/UI/Shell/Ecs/UiShellEcsGateway.PublicObjective.cs"));
            Assert.IsTrue(gateway.Contains("bool airMove=group.Domain==SkirmishPopulationCategory.Air&&group.Role==SkirmishRoleKind.TransportHeli;"));
            Assert.IsTrue(gateway.Contains("CanMove=(ground||airMove)&&!mounted,CanHold=ground&&!mounted,"));
        }

        static string ThisFile([System.Runtime.CompilerServices.CallerFilePath] string path = "") => path;

        // A presented FC frame with no current objective choice, after one accepted order.
        static AriaSkirmishObservation View(float time) => new AriaSkirmishObservation
        {
            Active = true, ExpandedSession = true, Frame = 900, Time = time, ObjectiveType = 1, Infantry = 20, OwnMaterials = 400,
            PublicObjective = new AriaObjectiveTouchObservation { Applicable = true, Supported = true, Presented = true, PresentationEpoch = 1,
                Intent = AriaSkirmishIntent.ObserveObjective, CameraSettled = true },
            ObjectiveSupport = new AriaObjectiveSupportObservation { Presented = true, SimulationSeconds = time - 50, DismountedCombatGroups = 3, CaptureMajority = true },
            RecruitBreacher = T(41), RecruitMarksman = T(42), RecruitRocketeer = T(43)
        };

        static AriaSkirmishPlanComponent Plan() => new AriaSkirmishPlanComponent { ObjectiveAccepted = 1, ObjectiveRocketeerStage = 2 };

        static void Step(in AriaSkirmishObservation v, ref AriaSkirmishPlanComponent p, ref AriaPlaySessionComponent t, out AriaPlayObservationComponent o)
        { o = default; AriaSkirmishPlanSystem.Step(v, ref p, ref t, ref o); }

        [Test]
        public void RifleTopUpDropsToTheFloorOnceCoverageStarts()
        {
            Assert.AreEqual(24, AriaObjectiveSkillPolicy.RifleTarget(AriaObjectiveType.FrontlineControl, false));
            Assert.AreEqual(AriaObjectiveSkillPolicy.CoverageInfantryFloor, AriaObjectiveSkillPolicy.RifleTarget(AriaObjectiveType.FrontlineControl, true));
            Assert.AreEqual(16, AriaObjectiveSkillPolicy.RifleTarget(AriaObjectiveType.ConvoyEscort, true), "never raised above the type's own target");
            Assert.AreEqual(0, AriaObjectiveSkillPolicy.RifleTarget(AriaObjectiveType.None, true));
        }

        [Test]
        public void OneTimeRocketeerBuyIsBounded()
        {
            byte stage = 0; float deadline = 0;
            Assert.AreEqual(AriaObjectiveBuy.None, AriaObjectiveSkillPolicy.OneTimeBuy(ref stage, ref deadline, false, false, true, false, true, 10), "after the first accepted order");
            Assert.AreEqual(AriaObjectiveBuy.Recruit, AriaObjectiveSkillPolicy.OneTimeBuy(ref stage, ref deadline, true, false, true, false, true, 10));
            Assert.AreEqual(AriaObjectiveBuy.Waiting, AriaObjectiveSkillPolicy.OneTimeBuy(ref stage, ref deadline, true, false, true, false, false, 11), "card not shown yet");
            Assert.AreEqual(AriaObjectiveBuy.None, AriaObjectiveSkillPolicy.OneTimeBuy(ref stage, ref deadline, true, false, true, true, true, 12));
            Assert.AreEqual(2, stage, "queued");
            stage = 0;
            AriaObjectiveSkillPolicy.OneTimeBuy(ref stage, ref deadline, true, false, true, false, true, 20);
            Assert.AreEqual(AriaObjectiveBuy.None, AriaObjectiveSkillPolicy.OneTimeBuy(ref stage, ref deadline, true, false, true, false, true, 20.1f + AriaObjectiveSkillPolicy.OneTimeBuySeconds));
            Assert.AreEqual(2, stage, "never retried");
            Assert.AreEqual(AriaObjectiveBuy.None, AriaObjectiveSkillPolicy.OneTimeBuy(ref stage, ref deadline, true, false, true, false, true, 99));
        }

        [Test]
        public void ConvoyInfantryEscortsToTheDestination()
        {
            var rifle = new AriaEscortCandidate { GroupId = 3, Role = SkirmishRoleKind.Rifle, Domain = SkirmishPopulationCategory.Infantry, CanMove = true, Target = true };
            var trucks = new AriaEscortCandidate { GroupId = 1, Role = SkirmishRoleKind.ConvoyLogic, Domain = SkirmishPopulationCategory.ObjectiveSupport, CanMove = true, Target = true, Originals = true };
            var list = new List<AriaEscortCandidate> { trucks, rifle };
            Assert.AreEqual(1, AriaObjectiveEscortPolicy.Pick(list, AriaObjectiveType.ConvoyEscort));
            Assert.AreEqual(1, AriaObjectiveEscortPolicy.Pick(list, AriaObjectiveType.FrontlineControl), "W32: unassigned FC infantry take a Move to the zone");
            var chosen = list[1]; chosen.Chosen = true; list[1] = chosen;
            Assert.AreEqual(-1, AriaObjectiveEscortPolicy.Pick(list, AriaObjectiveType.FrontlineControl), "the chosen group follows the zone chooser");
            chosen.Chosen = false; list[1] = chosen;
            Assert.AreEqual(-1, AriaObjectiveEscortPolicy.Pick(list), "base overload keeps vehicles only");
        }

        [Test]
        public void ConvoyChooserOrdersOnlyTrucksToTheDestination()
        {
            var truck = new SkirmishPublicGoalNode { PublicId = "truck.1", Kind = SkirmishPublicGoalNodeKind.OwnDesignatedSubject,
                Provenance = SkirmishPublicGoalProvenance.OwnRoster, PositionKnown = true, WorldX = 5, WorldZ = 5, OwnerKnown = true, OwnerFaction = 1,
                RevealedSubjectKind = SkirmishPublicSubjectKind.ObjectiveConvoyTruck, SubjectDisposition = SkirmishPublicSubjectDisposition.Live,
                HealthKnown = true, Health = 100, MaximumHealth = 100, HealthProvenance = SkirmishPublicHealthProvenance.OwnRoster };
            var destination = new SkirmishPublicGoalNode { PublicId = "destination", Kind = SkirmishPublicGoalNodeKind.DeliveryDestination,
                Provenance = SkirmishPublicGoalProvenance.PublicObjective, PositionKnown = true, WorldX = 40, WorldZ = 20 };
            Assert.IsTrue(SkirmishPublicGoalSnapshot.TryCreate("ce-coverage", 1, 1, SkirmishObjectiveKind.ConvoyEscort, SkirmishPublicGoalPerspective.Escort,
                0, 100, SkirmishOutcomeKind.None, default, new[] { destination, truck }, out var goal, out var error), error);
            var approaches = new List<SkirmishPublicPlayerApproach>();
            foreach (uint g in new uint[] { 1, 2 })
                foreach (var id in new[] { "destination", "truck.1" })
                    approaches.Add(new SkirmishPublicPlayerApproach { GroupId = g, GoalId = id, Inspected = true, GroundCompatible = true });
            var rifles = new SkirmishPublicPlayerGroup { GroupId = 1, Own = true, Living = true, CombatInfantry = true, Dismounted = true, CanMove = true, CanHold = true, PositionKnown = true };
            var trucks = new SkirmishPublicPlayerGroup { GroupId = 2, Own = true, Living = true, Dismounted = true, CanMove = true, CanHold = true, PositionKnown = true,
                DesignatedSubjects = new[] { "truck.1" } };
            Assert.IsFalse(SkirmishPlayerObjectivePolicyUtilitySystemHelper.Choose(goal, new[] { rifles }, approaches, default).Available,
                "lane 25 S019: an infantry Move onto a moving truck is never credited");
            var choice = SkirmishPlayerObjectivePolicyUtilitySystemHelper.Choose(goal, new[] { rifles, trucks }, approaches, default);
            Assert.AreEqual((2u, "destination", SkirmishPublicPlayerAction.Move), (choice.GroupId, choice.GoalId, choice.Action));
        }

        [Test]
        public void BreacherStartsAfterTheFirstAcceptedObjectiveOrder()
        {
            var v = View(200); v.CanAffordBreacher = true;
            var p = Plan(); var t = new AriaPlaySessionComponent { Phase = AriaPlayPhase.Observing };
            Step(v, ref p, ref t, out var o);
            Assert.AreEqual(41, o.TargetId, "lane 25: the breacher waited for a base-assault order and blocked all coverage");
            Assert.AreEqual(1, p.BreacherStage);
            Assert.IsTrue(AriaSkirmishPlanSystem.ObjectiveCoverageInFlight(p));
        }

        [Test]
        public void CoverageUsesTheObjectiveReserveMask()
        {
            var v = View(200); v.CoverageAffordMask = 0; v.CoverageObjectiveAffordMask = 1; // marksman
            var p = Plan(); p.BreacherStage = 2; var t = new AriaPlaySessionComponent { Phase = AriaPlayPhase.Observing };
            Step(v, ref p, ref t, out var o);
            Assert.AreEqual(42, o.TargetId, "FieldBase materials stay below the 250 base-assault reserve");
            Assert.AreEqual((byte)AriaCoverageKind.Marksman, p.CoverageCurrent);
        }

        [Test]
        public void CoverageInFlightFinishesBeforeObjectiveOrders()
        {
            var v = View(200); v.CoverageObjectiveAffordMask = 1;
            v.PublicObjective.GroupId = 11; v.PublicObjective.PublicKey = 22; v.PublicObjective.Action = AriaObjectiveAction.Move;
            v.PublicObjective.SelectGroup = T(5);
            var p = Plan(); p.BreacherStage = 2; p.CoverageAssaultSeen = 1;
            p.CoverageCurrent = (byte)AriaCoverageKind.Marksman; p.CoverageStarted = 199; p.CoverageDeadline = 209;
            var t = new AriaPlaySessionComponent { Phase = AriaPlayPhase.Observing };
            Step(v, ref p, ref t, out var o);
            Assert.AreEqual(42, o.TargetId, "the marksman purchase is not abandoned for a squad selection");
            p.CoverageCurrent = 0; p.CoverageDone = AriaCoverageRecruitPolicy.All; v.Frame++;
            Step(v, ref p, ref t, out o);
            Assert.AreEqual(5, o.TargetId, "then the objective order");
        }

        [Test]
        public void BaseAssaultCoverageKeepsItsOwnReserveMask()
        {
            // Base Assault (no public objective): the objective mask is ignored, so S002/S026/army A are unchanged.
            var v = new AriaSkirmishObservation { Active = true, ExpandedSession = true, ExpandedBaseAssault = true, EnemyDesignatedAlive = true,
                Time = 100, CoverageAffordMask = 0, CoverageObjectiveAffordMask = 1, RecruitMarksman = T(42) };
            var p = new AriaSkirmishPlanComponent { AssaultIssued = 1, StructureOrdered = 1, BreacherStage = 2 };
            var t = new AriaPlaySessionComponent { Phase = AriaPlayPhase.Observing };
            AriaPlayObservationComponent o = default;
            var method = typeof(AriaSkirmishPlanSystem).GetMethod("StepGenericCoverageRecruit", BindingFlags.NonPublic | BindingFlags.Static);
            Assert.IsNotNull(method);
            var args = new object[] { v, p, t, o };
            Assert.IsFalse((bool)method.Invoke(null, args));
            v.CoverageAffordMask = 1; args = new object[] { v, p, t, o };
            Assert.IsTrue((bool)method.Invoke(null, args));
            Assert.AreEqual(42, ((AriaPlayObservationComponent)args[3]).TargetId);
        }
    }
}
