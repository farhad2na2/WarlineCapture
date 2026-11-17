using Game.Simulation;
using NUnit.Framework;

namespace Game.Tests.Editor
{
    // Lane 6 (2026-11-17): bounded recoveries so ARIA never waits on a control that is not
    // offered, never repeats an order that never shows, and never searches with Attack armed
    // indefinitely. Pure policy checks.
    public sealed class AriaColumnRecoveryTests
    {
        static AriaGroundColumnView Page(float time) => new AriaGroundColumnView
        {
            EnemyDesignatedAlive = true, ExpandedPageCount = 3, ExpandedTotalGroups = 11, ExpandedCanNextPage = true,
            NextAvailable = true, PreviousAvailable = true, ToggleAvailable = true, AttackAvailable = true, Time = time
        };

        // S002 155923: page 1 cards (three assault groups) never resolved for 147 s; ARIA waited.
        [Test]
        public void CardNeverOfferedGivesThePageUpAfterTheStallTime()
        {
            var plan = new AriaGroundColumnState { PagedToAssault = 1, PagesOrdered = 1, StructureOrdered = 1 };
            var view = Page(66f); view.ExpandedPageIndex = 1; view.ExpandedAssaultMask = 0b111; view.AvailableSquadMask = 0;
            Assert.IsFalse(AriaGroundColumnPolicy.StepGroundAssault(view, ref plan, out _), "first frame: wait for the card");
            view.Time = 66f + AriaGroundColumnPolicy.CardStallSeconds - .5f;
            Assert.IsFalse(AriaGroundColumnPolicy.StepGroundAssault(view, ref plan, out _));
            Assert.AreEqual(0, plan.Skips);
            view.Time = 66f + AriaGroundColumnPolicy.CardStallSeconds + .1f;
            Assert.IsTrue(AriaGroundColumnPolicy.StepGroundAssault(view, ref plan, out var decision));
            Assert.AreEqual(1, plan.Skips, "the page is given up");
            Assert.AreEqual(2, plan.PagesOrdered, "the sweep moves past it");
            Assert.AreEqual(AriaGroundAction.NextPage, decision.Action);
            Assert.IsTrue(AriaGroundColumnPolicy.Skipped(plan.SkipKeyA, plan.SkipKeyB, 1, 0b111), "remembered so it does not reopen the sweep");
            Assert.IsFalse(AriaGroundColumnPolicy.Skipped(plan.SkipKeyA, plan.SkipKeyB, 0, 0b111));
        }

        // S049: the structure group's Attack order never showed; Attack / tap base alternated until
        // the progress watchdog stopped ARIA. The page is given up and the column completes.
        [Test]
        public void OrderThatNeverShowsIsGivenUpAndTheColumnCompletes()
        {
            var plan = new AriaGroundColumnState { PagedToAssault = 1, AssaultSelecting = 1 };
            var view = Page(150f); view.ExpandedPageCount = 1; view.ExpandedCanNextPage = false; view.NextAvailable = false; view.PreviousAvailable = false;
            view.ExpandedAssaultMask = 0b1001; view.ExpandedAttackOrderMask = 0b0001; view.ExpandedStructureMask = 0b1000;
            view.ExpandedSelectedMask = 0b1000; view.ExpandedSelectedGroups = 1; view.AvailableSquadMask = 0b1111;
            Assert.IsTrue(AriaGroundColumnPolicy.StepGroundAssault(view, ref plan, out var decision));
            Assert.AreEqual(AriaGroundAction.ArmAttack, decision.Action);
            view.Time = 150f + AriaGroundColumnPolicy.OrderStallSeconds - 1f;
            AriaGroundColumnPolicy.StepGroundAssault(view, ref plan, out decision);
            Assert.AreEqual(AriaGroundAction.ArmAttack, decision.Action, "still within the bound");
            view.Time = 150f + AriaGroundColumnPolicy.OrderStallSeconds + .1f;
            Assert.IsFalse(AriaGroundColumnPolicy.StepGroundAssault(view, ref plan, out _));
            Assert.AreEqual(1, plan.AssaultIssued, "a one-page roster completes in place");
            Assert.AreEqual(1, plan.StructureOrdered);
            Assert.AreEqual(1, plan.Skips);
        }

        [Test]
        public void AnOrderThatShowsResetsTheGuardAndLegacyCallersAreUnchanged()
        {
            var plan = new AriaGroundColumnState { PagedToAssault = 1, AssaultSelecting = 1 };
            var view = Page(10f); view.ExpandedPageCount = 1; view.NextAvailable = view.PreviousAvailable = view.ExpandedCanNextPage = false;
            view.ExpandedAssaultMask = 1; view.ExpandedStructureMask = 1; view.ExpandedSelectedMask = 1; view.ExpandedSelectedGroups = 1; view.AvailableSquadMask = 1;
            AriaGroundColumnPolicy.StepGroundAssault(view, ref plan, out _);
            view.ExpandedAttackOrderMask = 1; view.Time = 12f;
            Assert.IsFalse(AriaGroundColumnPolicy.StepGroundAssault(view, ref plan, out _));
            Assert.AreEqual(1, plan.AssaultIssued);
            Assert.AreEqual(0, plan.Skips);
            Assert.AreEqual(0, plan.StallKey);

            var legacy = new AriaGroundColumnState { PagedToAssault = 1, PagesOrdered = 1 };
            var old = Page(0f); old.ExpandedPageIndex = 1; old.ExpandedAssaultMask = 0b111; old.AvailableSquadMask = 0;
            for (int i = 0; i < 100; i++) Assert.IsFalse(AriaGroundColumnPolicy.StepGroundAssault(old, ref legacy, out _));
            Assert.AreEqual(0, legacy.Skips, "no clock: no guard");
        }

        // S026 lane 6: the base search looped with Attack armed for 50+ s; q1: the enemy focus was
        // pressed three times with Attack armed (Blocked 115).
        [Test]
        public void BaseSearchIsBoundedByTapsAndNeverPressesOneControlThreeTimes()
        {
            var s = default(AriaFindBaseState);
            Assert.IsFalse(AriaFindBasePolicy.GiveUp(ref s, 50, 0), "map navigation starts");
            Assert.IsFalse(AriaFindBasePolicy.GiveUp(ref s, 51, 0));
            Assert.IsFalse(AriaFindBasePolicy.GiveUp(ref s, 52, 0));
            Assert.IsFalse(AriaFindBasePolicy.GiveUp(ref s, 53, 0));
            Assert.IsTrue(AriaFindBasePolicy.GiveUp(ref s, 54, 0), "four search taps without the base: give up");

            s = default;
            const int focusEnemy = -301000;
            Assert.IsFalse(AriaFindBasePolicy.GiveUp(ref s, 10, focusEnemy));
            Assert.IsFalse(AriaFindBasePolicy.GiveUp(ref s, 10, focusEnemy), "same press in flight");
            Assert.IsFalse(AriaFindBasePolicy.GiveUp(ref s, 11, focusEnemy), "second press of the focus");
            Assert.IsTrue(AriaFindBasePolicy.GiveUp(ref s, 12, focusEnemy), "never a third press of the same control");

            s = default;
            Assert.IsFalse(AriaFindBasePolicy.GiveUp(ref s, 20, 0));
            Assert.IsFalse(AriaFindBasePolicy.GiveUp(ref s, 21, 0));
            // Home defence takes 30 taps in between; the search resumes afterwards.
            Assert.IsFalse(AriaFindBasePolicy.GiveUp(ref s, 51, 0, continuing: false), "other owners' taps are not search taps");
            Assert.AreEqual(1, s.Taps);
        }

        // Radar (lane 6, S002 130365/104731): Build, Vehicles tab, scroll, card and produce gave up at +10.0 s.
        [Test]
        public void CoveragePurchaseThatKeepsProgressingGetsMoreTimeButIsCapped()
        {
            var plan = new AriaCoverageState { Done = 3 };
            var view = new AriaCoverageView { AssaultIssued = true, BreacherSettled = true, EnemyAlive = true,
                AffordMask = 7, CardMask = 7, FieldedMask = 3, CompletedContacts = 100, Time = 165f };
            Assert.AreEqual(AriaCoverageAction.Recruit, AriaCoverageRecruitPolicy.Step(view, ref plan, out var kind));
            Assert.AreEqual(AriaCoverageKind.Radar, kind);
            for (int i = 1; i <= 3; i++)
            {
                view.CompletedContacts = 100 + i; view.Time = 165f + i * 3f; // a tap lands every 3 s
                Assert.AreNotEqual(AriaCoverageAction.None, AriaCoverageRecruitPolicy.Step(view, ref plan, out _));
            }
            view.Time = 165f + 11f; view.CardMask = 0;
            Assert.AreEqual(AriaCoverageAction.Waiting, AriaCoverageRecruitPolicy.Step(view, ref plan, out _), "past 10 s but still progressing");
            view.CardMask = 7;
            for (int i = 4; i <= 12; i++) { view.CompletedContacts = 100 + i; view.Time = 165f + i * 3f; AriaCoverageRecruitPolicy.Step(view, ref plan, out _); }
            Assert.AreEqual(0, plan.Current, "capped at MaxBuySeconds even while taps land");
            Assert.AreEqual(7, plan.Done);
        }
    
        // Lane 8 S050: the radar order released (rear ground never offered) with Move still armed.
        [Test]
        public void SupportReleaseTogglesItsArmedModeOff()
        {
            var plan = new AriaSupportState { Stage = 3, Kind = (byte)AriaSupportKind.Radar, Slot = 1, Actions = 40, Deadline = 279f };
            var view = new AriaSupportView { FieldedMask = 1, ArmyMoving = true, EnemyAlive = true, MoveAvailable = true, AttackAvailable = true,
                Mode = AriaSupportMode.Move, RearAvailable = false, SelectedGroups = 1, SelectedMask = 2,
                SupportSlots = AriaSupportOrderPolicy.Slots(AriaSupportKind.None, AriaSupportKind.Radar, AriaSupportKind.None, AriaSupportKind.None, AriaSupportKind.None),
                AvailableSquadMask = 31, CompletedContacts = 40, Time = 279.5f };
            Assert.IsTrue(AriaSupportOrderPolicy.Step(view, ref plan, out var d));
            Assert.AreEqual(AriaSupportAction.ArmMove, d.Action, "the Move toggle disarms it");
            Assert.AreEqual(AriaSupportOrderPolicy.StageDisarm, plan.Stage);
            view.Time = 280f;
            Assert.AreEqual(AriaSupportAction.ArmMove, (AriaSupportOrderPolicy.Step(view, ref plan, out d), d.Action).Item2, "same toggle in flight");
            view.Mode = AriaSupportMode.None; view.CompletedContacts = 41; view.Time = 281f;
            Assert.IsFalse(AriaSupportOrderPolicy.Step(view, ref plan, out d));
            Assert.AreEqual(3, d.Trace, "released with nothing armed");
            Assert.AreEqual(0, plan.Stage);
        }

        // Lane 8 S026: the full map's Close was pressed three times while the map stayed open.
        [Test]
        public void MapCloseVariesTheControlAndNeverPressesCloseThreeTimesInARow()
        {
            byte closes = 0, centered = 0; int last = 0;
            var pressed = new System.Collections.Generic.List<AriaMapCloseAction>();
            int completed = 24;
            for (int i = 0; i < 8; i++)
            {
                var a = AriaMapCloseRecovery.Step(ref closes, ref centered, ref last, completed, true);
                pressed.Add(a);
                if (a != AriaMapCloseAction.Wait) completed++; // each press completes, the map stays open
            }
            int run = 0, longest = 0; AriaMapCloseAction prev = AriaMapCloseAction.Wait;
            foreach (var a in pressed) { if (a == AriaMapCloseAction.Wait) continue; run = a == prev ? run + 1 : 1; prev = a; if (run > longest) longest = run; }
            Assert.Less(longest, 3);
            Assert.Contains(AriaMapCloseAction.CenterHome, pressed);
            Assert.AreEqual(AriaMapCloseAction.Wait, pressed[pressed.Count - 1], "bounded: nothing more after MaxCloses");
        }

        static string ThisFile([System.Runtime.CompilerServices.CallerFilePath] string path = "") => path;

        // Lane 8 S050 BlockAllocator budget: ARIA's per-frame reads use queries created once.
        [Test]
        public void AriaInputSystemCreatesNoQueryOutsideOnCreate()
        {
            var here = ThisFile();
            var root = System.IO.Path.GetFullPath(System.IO.Path.Combine(System.IO.Path.GetDirectoryName(here), "..", "..", "..", ".."));
            var input = System.IO.File.ReadAllText(System.IO.Path.Combine(root, "Assets/Game/Scripts/UI/Shell/Ecs/AriaPlayInputSystem.cs"));
            Assert.AreEqual(-1, input.IndexOf("CreateEntityQuery", System.StringComparison.Ordinal), "use the system-owned queries from OnCreate");
            var projection = System.IO.File.ReadAllText(System.IO.Path.Combine(root, "Assets/Game/Scripts/Runtime/Skirmish/SkirmishAriaPublicProjection.cs"));
            int roleQueries = 0, at = 0;
            const string roleQuery = "CreateEntityQuery(typeof(SkirmishAttemptOwnedComponent), typeof(SkirmishUnitRoleComponent))";
            while ((at = projection.IndexOf(roleQuery, at, System.StringComparison.Ordinal)) >= 0) { roleQueries++; at++; }
            Assert.AreEqual(1, roleQueries, "the unit-role query is created once per World (UnitRoleQuery)");
        }
}
}
