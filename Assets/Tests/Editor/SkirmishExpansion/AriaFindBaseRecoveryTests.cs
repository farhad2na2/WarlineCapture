using Game.Simulation;
using NUnit.Framework;

namespace Game.Tests.Editor
{
    // Lane 15 r2 S002 155923: the base search gave up after two presses of one focus control, but its
    // Attack-off press never went out (Attack not offered). The search restarted from a fresh count and
    // pressed the same control a third and fourth time; the input layer stopped ARIA (Blocked 115).
    // S004 warm-up: ARIA stopped while still Starting, 4 s after consent, without a touch or a stop code.
    public sealed class AriaFindBaseRecoveryTests
    {
        // Mirrors AriaPlayDecisionSystem: a fourth consecutive press of one target is Blocked 115.
        struct InputLayer
        {
            public int Target, Attempts, Actions; public bool Blocked;
            public void Press(int target)
            {
                if (target != Target) { Target = target; Attempts = 0; }
                if (Attempts >= 3) { Blocked = true; return; }
                Attempts++; Actions++;
            }
        }

        const int FocusControl = -61514, AttackControl = -56152;

        // One planner step of the search branch (Attack armed, no base contact), as in both planners.
        static int Step(ref AriaFindBaseState find, ref AriaFindBaseRecovery recovery, ref bool continuing,
            in InputLayer input, float time, bool attackMode, bool attackOffered, ref int giveUps)
        {
            var fallback = AriaFindBasePolicy.Recover(ref recovery, time, input.Actions, attackMode, attackOffered);
            if (fallback != AriaFindBaseFallback.Search)
            { continuing = false; return fallback == AriaFindBaseFallback.Disarm ? AttackControl : 0; }
            bool giveUp = AriaFindBasePolicy.GiveUp(ref find, input.Actions, FocusControl, continuing) ||
                AriaFindBasePolicy.SameControlExhausted(input.Target, input.Attempts, FocusControl);
            if (giveUp)
            {
                giveUps++; AriaFindBasePolicy.Reset(ref find);
                AriaFindBasePolicy.BeginRecovery(ref recovery, time);
                fallback = AriaFindBasePolicy.Recover(ref recovery, time, input.Actions, attackMode, attackOffered);
                continuing = false;
                return fallback == AriaFindBaseFallback.Disarm ? AttackControl : 0;
            }
            continuing = true;
            return FocusControl;
        }

        [Test]
        public void SearchNeverAsksForAThirdPressOfOneControlWhenAttackIsNotOffered()
        {
            var find = default(AriaFindBaseState); var recovery = default(AriaFindBaseRecovery);
            var input = new InputLayer(); bool continuing = false; int giveUps = 0, focusPresses = 0;
            for (float time = 200f; time < 400f; time += 1.8f)
            {
                int target = Step(ref find, ref recovery, ref continuing, input, time, attackMode: true, attackOffered: false, ref giveUps);
                if (target == 0) continue;
                Assert.AreEqual(FocusControl, target, "Attack is not offered: only the search control or waiting");
                input.Press(target); focusPresses++;
                Assert.IsFalse(input.Blocked, "Blocked 115 at t=" + time);
                Assert.IsTrue(input.Attempts <= AriaFindBasePolicy.MaxSameAttempts, "never a third consecutive press");
            }
            Assert.IsTrue(giveUps >= 2, "the search keeps retrying after its cool-down");
            Assert.IsTrue(focusPresses <= giveUps * 2 + 2, "bounded presses per search");
        }

        [Test]
        public void GiveUpDisarmsAttackAtMostTwiceWithASettle()
        {
            var r = default(AriaFindBaseRecovery);
            AriaFindBasePolicy.BeginRecovery(ref r, 100f);
            Assert.AreEqual(AriaFindBaseFallback.Disarm, AriaFindBasePolicy.Recover(ref r, 100f, 10, true, true));
            Assert.AreEqual(AriaFindBaseFallback.Disarm, AriaFindBasePolicy.Recover(ref r, 100.5f, 10, true, true), "in flight: the same press is kept");
            // Completed but Attack still shows armed: wait for the settle before a second press.
            Assert.AreEqual(AriaFindBaseFallback.Wait, AriaFindBasePolicy.Recover(ref r, 101f, 11, true, true));
            Assert.AreEqual(1, r.Disarms);
            Assert.AreEqual(AriaFindBaseFallback.Disarm, AriaFindBasePolicy.Recover(ref r, 103f, 11, true, true));
            Assert.AreEqual(AriaFindBaseFallback.Wait, AriaFindBasePolicy.Recover(ref r, 104f, 12, true, true));
            Assert.AreEqual(AriaFindBaseFallback.Wait, AriaFindBasePolicy.Recover(ref r, 110f, 12, true, true), "no third Attack press");
            Assert.AreEqual(2, r.Disarms);
            Assert.AreEqual(AriaFindBaseFallback.Search, AriaFindBasePolicy.Recover(ref r, 100f + AriaFindBasePolicy.RetrySeconds, 12, true, true), "the search may resume");
        }

        [Test]
        public void DisarmedAttackOrMissingControlWaitsInsteadOfPressing()
        {
            var r = default(AriaFindBaseRecovery);
            AriaFindBasePolicy.BeginRecovery(ref r, 50f);
            Assert.AreEqual(AriaFindBaseFallback.Wait, AriaFindBasePolicy.Recover(ref r, 50f, 3, attackMode: false, attackAvailable: true));
            Assert.AreEqual(AriaFindBaseFallback.Wait, AriaFindBasePolicy.Recover(ref r, 51f, 3, attackMode: true, attackAvailable: false));
            Assert.AreEqual(0, r.Disarms);
            Assert.AreEqual(AriaFindBaseFallback.Search, AriaFindBasePolicy.Recover(ref r, 71f, 3, true, false));
            var none = default(AriaFindBaseRecovery);
            Assert.AreEqual(AriaFindBaseFallback.Search, AriaFindBasePolicy.Recover(ref none, 5f, 0, true, true), "no recovery: search as before");
        }

        [Test]
        public void SameControlExhaustionReadsTheInputLayerCount()
        {
            Assert.IsFalse(AriaFindBasePolicy.SameControlExhausted(FocusControl, 1, FocusControl));
            Assert.IsTrue(AriaFindBasePolicy.SameControlExhausted(FocusControl, 2, FocusControl));
            Assert.IsFalse(AriaFindBasePolicy.SameControlExhausted(AttackControl, 2, FocusControl), "a different last target resets the count");
            Assert.IsFalse(AriaFindBasePolicy.SameControlExhausted(0, 3, 0), "map navigation has no control id here");
        }

        [Test]
        public void StartIsRetriedForAGracePeriodThenStopsWithACode()
        {
            Assert.AreEqual(0, AriaStartPolicy.Decide(3.5f, true), "a 3 s hitch no longer stops ARIA");
            Assert.AreEqual(0, AriaStartPolicy.Decide(AriaStartPolicy.GraceSeconds, false));
            Assert.AreEqual(AriaStartPolicy.StaleObservation, AriaStartPolicy.Decide(AriaStartPolicy.GraceSeconds + .1f, true));
            Assert.AreEqual(AriaStartPolicy.StartRefused, AriaStartPolicy.Decide(AriaStartPolicy.GraceSeconds + .1f, false));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void S002MapWithoutApproachFallsBackToReportedPositionThenOrders(bool baseVisible)
        {
            var openMap = new AriaS002ActionTarget { Id = -20010, Admitted = true, Action = AriaS002Action.InspectObjective };
            var closeMap = new AriaS002ActionTarget { Id = -414482, Admitted = true, Action = AriaS002Action.CloseInspection };
            var reportedPosition = new AriaS002ActionTarget { Id = -343866, Admitted = true, Action = AriaS002Action.InspectObjective };
            var view = new AriaS002Observation
            {
                Scope = AriaS002Scope.NativeRendered, SourceScenarioIndex = 4, Active = true,
                ExpandedSession = true, ExpandedBaseAssault = true, EnemyDesignatedAlive = true,
                Time = 309f, PlayerHealth = 520, Infantry = 24, AttackMode = true, SelectionVisible = true,
                SelectedCount = 5, ExpandedSelectedMask = 24, ExpandedAssaultMask = 28, ExpandedAttackOrderMask = 4,
                FocusAdvance = openMap, FocusEnemy = reportedPosition
            };
            var plan = new AriaS002PlanState { LastProgressAt = 226f, PlayerHealth = 520, Infantry = 24 };
            var touch = new AriaS002ContactState { Phase = AriaS002ContactPhase.Observing, Actions = 114, LastObjectiveProgressAt = 226f };
            AriaS002Decision Step()
            {
                AriaS002Policy.Step(view, ref plan, ref touch, out var decision);
                return decision;
            }
            void Complete(AriaS002ActionTarget target)
            {
                touch.Actions++;
                touch.LastCompletedTargetId = target.Id;
            }

            Assert.AreEqual(openMap.Id, Step().TargetId);
            Complete(openMap);
            view.Time = 310f;
            view.InspectionOpen = true;
            view.FocusAdvance = view.FocusEnemy = default;
            view.CloseMap = closeMap;
            Assert.AreEqual(AriaS002DecisionKind.Waiting, Step().Kind);
            view.Time = 315f;
            Assert.AreEqual(closeMap.Id, Step().TargetId);

            Complete(closeMap);
            view.Time = 316f;
            view.InspectionOpen = false;
            view.FocusAdvance = openMap;
            view.FocusEnemy = reportedPosition;
            var focus = Step();
            Assert.AreEqual(reportedPosition.Id, focus.TargetId);
            Assert.AreEqual(AriaS002DecisionKind.Control, focus.Kind);
            Assert.AreEqual(AriaS002Action.InspectObjective, focus.Action);

            Complete(reportedPosition);
            view.Time = 318f;
            Assert.AreEqual(AriaS002DecisionKind.Waiting, Step().Kind, "Do not repeat a completed camera tap while its view settles.");
            Assert.AreEqual(226f, touch.LastObjectiveProgressAt);

            view.Time = 320f;
            var order = new AriaS002ActionTarget { Id = baseVisible ? -20001 : -20006, Admitted = true,
                Action = baseVisible ? AriaS002Action.AttackVisibleObjective : AriaS002Action.AttackPublicTerrain };
            if (baseVisible) view.EnemyBase = order;
            else view.AdvanceGround = order;
            var attack = Step();
            Assert.AreEqual(AriaS002DecisionKind.WorldTarget, attack.Kind);
            Assert.AreEqual(order.Id, attack.TargetId);
            Assert.AreEqual(order.Action, attack.Action);
        }

        [Test]
        public void BothPlannersRecoverAndTheTouchDriverIgnoresItsOwnDevices()
        {
            string root = System.IO.Path.GetFullPath(System.IO.Path.Combine(ThisFile(), "..", "..", "..", ".."));
            string Read(string relative) => System.IO.File.ReadAllText(System.IO.Path.Combine(root, relative));
            string s002 = Read("Game/Scripts/Simulation/AriaGroundBaseAssaultPolicy.cs");
            string generic = Read("Game/Scripts/UI/Shell/Ecs/AriaSkirmishPlanSystem.cs");
            foreach (var source in new[] { s002, generic })
            {
                Assert.IsTrue(source.Contains("StepFindBaseRecovery(view, ref plan, ref touch, ref output)) return;"), "StepFindBaseRecovery(view, ref plan, ref touch, ref output)) return;");
                Assert.IsTrue(source.Contains("AriaFindBasePolicy.SameControlExhausted(touch.TargetId, touch.Attempts, nextFind)"), "AriaFindBasePolicy.SameControlExhausted(touch.TargetId, touch.Attempts, nextFind)");
                Assert.IsTrue(source.Contains("AriaFindBasePolicy.BeginRecovery(ref recovery, view.Time)"), "AriaFindBasePolicy.BeginRecovery(ref recovery, view.Time)");
            }
            string driver = Read("Game/Scripts/UI/Screens/AriaTouchInputUiSystemHelper.cs");
            Assert.IsTrue(driver.Contains("if (!device.enabled || IsDemonstrationDevice(device)) continue;"), "if (!device.enabled || IsDemonstrationDevice(device)) continue;");
            string input = Read("Game/Scripts/UI/Shell/Ecs/AriaPlayInputSystem.cs");
            Assert.IsTrue(input.Contains("AriaStartPolicy.Decide(startNow - current.DueAt, stale)"), "AriaStartPolicy.Decide(startNow - current.DueAt, stale)");
            Assert.IsTrue(input.Contains("[AriaBlocked] reason="), "[AriaBlocked] reason=");
        }

        static string ThisFile([System.Runtime.CompilerServices.CallerFilePath] string path = "") => path;
    }
}
