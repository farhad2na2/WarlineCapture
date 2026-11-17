using Game.UI.Contracts;
using Game.UI.Shell.Contracts.Ecs;

namespace Game.UI.Shell.Ecs
{
    // Gate-3 unit coverage for the generic expanded base-assault planner (every expanded
    // ground base assault other than the S002 native adapter, e.g. S026). The same pure
    // policies as the S002 adapter: one breacher squad, then one squad of each other
    // available kind (marksman, apc_fast, radar, apc_heavy, drone, transport heli), one at
    // a time after the first assault order, through Build and the catalog card; then one
    // ordinary order for each fielded support unit (radar / transport heli Move behind the
    // column, drone Scan ahead of it). Kinds the mission's catalog does not offer are never
    // affordable and are skipped. Visible controls only; no gameplay state is written.
    public partial struct AriaSkirmishPlanSystem
    {
        static void ClearFindBase(ref AriaSkirmishPlanComponent p)
        { p.FindBaseActive = 0; p.FindBaseTaps = 0; p.FindBaseRun = 0; p.FindBaseLastTarget = 0; }

        static bool CoverageEligible(in AriaSkirmishObservation v) =>
            v.ExpandedSession && v.ExpandedBaseAssault && !v.AirProfile && !v.CombinedArmsField && !v.PublicObjective.Applicable;

        static bool CoverageOtherOwner(in AriaSkirmishObservation v, in AriaSkirmishPlanComponent p, in AriaPlaySessionComponent t) =>
            t.Phase == AriaPlayPhase.Manual || v.AttackMode || v.MoveMode || v.ScanMode || v.MapOpen || v.PlacementOpen ||
            v.PresentedHomeThreat || p.HomeDefenseStage != 0 || p.PrecisionStage != 0 || p.AdvanceNavigation != 0 ||
            p.AssaultInspectStage != 0 || p.InterceptStage != 0 || p.PadStage != 0 || p.SupportStage != 0 ||
            p.ResearchCurrent != 0 || p.BuildCurrent != 0;

        // The whole structure-capable force is gone (lane 10 S049: no tank, rocketeer or breacher left,
        // the base untouched, ARIA idle until the progress watchdog): rebuild it through the drawer,
        // breachers first, rocketeers as the fallback, exactly like the S002 adapter.
        static bool StepGenericStructureReplacement(in AriaSkirmishObservation v, ref AriaSkirmishPlanComponent p,
            ref AriaPlayObservationComponent o)
        {
            // Only after the first accepted assault: never delays the opening.
            if (p.AssaultIssued != 0 && p.StructureOrdered != 0) p.CoverageAssaultSeen = 1;
            if (!CoverageEligible(v) || p.CoverageAssaultSeen == 0 || !v.EnemyDesignatedAlive || v.OwnStructureAttackUnits != 0 || v.MapOpen || v.AttackMode ||
                v.PresentedHomeThreat || p.HomeDefenseStage != 0 || (v.DrawerOpen && p.Intent != AriaSkirmishIntent.Recruit))
                return false;
            var replacement = Game.Simulation.AriaStructureReplacementPolicy.Choose(
                v.BreacherRecruitPending, v.RocketeerRecruitPending,
                v.CanAffordBreacher, v.RecruitBreacher.Available,
                v.CanAffordRocketeer, v.RecruitRocketeer.Available);
            switch (replacement)
            {
                case Game.Simulation.AriaStructureReplacement.Wait:
                    p.Intent = AriaSkirmishIntent.ObserveBattle; o.Kind = AriaPlayObservationKind.Waiting; return true;
                case Game.Simulation.AriaStructureReplacement.RecruitBreacher:
                    p.Intent = AriaSkirmishIntent.Recruit; Target(v.RecruitBreacher, false, ref o); return true;
                case Game.Simulation.AriaStructureReplacement.RecruitRocketeer:
                    p.Intent = AriaSkirmishIntent.Recruit; Target(v.RecruitRocketeer, false, ref o); return true;
                default: return false;
            }
        }

        static bool StepGenericBreacherCoverage(in AriaSkirmishObservation v, ref AriaSkirmishPlanComponent p,
            ref AriaPlaySessionComponent t, ref AriaPlayObservationComponent o)
        {
            if (!CoverageEligible(v)) return false;
            var facts = new Game.Simulation.AriaBreacherView {
                AssaultIssued = p.AssaultIssued != 0 && p.StructureOrdered != 0,
                // A drawer opened by this purchase is ours; any other open drawer belongs to another step.
                Busy = CoverageOtherOwner(v, p, t) || p.CoverageCurrent != 0 || (p.BreacherStage == 0 && v.DrawerOpen),
                EnemyAlive = v.EnemyDesignatedAlive,
                CanAffordBreacher = v.CanAffordBreacher, BreacherPending = v.BreacherRecruitPending,
                BreacherFielded = v.BreacherFielded, RecruitAvailable = v.RecruitBreacher.Available,
                RifleRecruitPending = v.RifleRecruitPending, RocketeerRecruitPending = v.RocketeerRecruitPending,
                CompletedContacts = t.Actions, Time = v.Time };
            var state = new Game.Simulation.AriaBreacherState { Stage = p.BreacherStage, Actions = p.BreacherActions, Deadline = p.BreacherDeadline };
            byte before = p.BreacherStage;
            var action = Game.Simulation.AriaBreacherPolicy.Step(facts, ref state);
            p.BreacherStage = state.Stage; p.BreacherActions = state.Actions; p.BreacherDeadline = state.Deadline;
            // Done buying: hand the open drawer to the ordinary close step, not to a rifle burst.
            if (before == 1 && state.Stage == 2) p.Intent = AriaSkirmishIntent.Inspect;
            if (action == Game.Simulation.AriaBreacherAction.Recruit)
            { p.Intent = AriaSkirmishIntent.Recruit; Target(v.RecruitBreacher, false, ref o); return true; }
            if (action == Game.Simulation.AriaBreacherAction.Waiting)
            { p.Intent = AriaSkirmishIntent.Recruit; o.Kind = AriaPlayObservationKind.Waiting; return true; }
            return false;
        }

        static bool StepGenericCoverageRecruit(in AriaSkirmishObservation v, ref AriaSkirmishPlanComponent p,
            ref AriaPlaySessionComponent t, ref AriaPlayObservationComponent o)
        {
            if (!CoverageEligible(v)) return false;
            int cards = (v.RecruitMarksman.Available ? 1 : 0) | (v.RecruitApcFast.Available ? 2 : 0) | (v.RecruitRadar.Available ? 4 : 0) |
                (v.RecruitApcHeavy.Available ? 8 : 0) | (v.RecruitDrone.Available ? 16 : 0) | (v.RecruitTransportHeli.Available ? 32 : 0) |
                (v.RecruitGunner.Available ? 64 : 0) | (v.RecruitTank.Available ? 128 : 0) | (v.RecruitSiege.Available ? 256 : 0);
            // The first accepted assault is latched (see the S002 adapter).
            if (p.AssaultIssued != 0 && p.StructureOrdered != 0) p.CoverageAssaultSeen = 1;
            var facts = new Game.Simulation.AriaCoverageView {
                AssaultIssued = p.CoverageAssaultSeen != 0,
                BreacherSettled = p.BreacherStage == 2,
                Busy = CoverageOtherOwner(v, p, t) || p.BreacherStage == 1 || (p.CoverageCurrent == 0 && v.DrawerOpen),
                EnemyAlive = v.EnemyDesignatedAlive,
                OtherRecruitPending = v.RifleRecruitPending || v.RocketeerRecruitPending || v.BreacherRecruitPending,
                AffordMask = v.CoverageAffordMask, PendingMask = v.CoveragePendingMask,
                FieldedMask = v.CoverageFieldedMask, CardMask = cards, CompletedContacts = t.Actions,
                DrawerOpen = v.DrawerOpen, Time = v.Time };
            var state = new Game.Simulation.AriaCoverageState { Done = p.CoverageDone, Current = p.CoverageCurrent, Deadline = p.CoverageDeadline,
                Started = p.CoverageStarted, Actions = p.CoverageActions, Batch = p.CoverageBatch };
            byte before = p.CoverageCurrent;
            var action = Game.Simulation.AriaCoverageRecruitPolicy.Step(facts, ref state, out var kind);
            p.CoverageDone = state.Done; p.CoverageCurrent = state.Current; p.CoverageDeadline = state.Deadline;
            p.CoverageStarted = state.Started; p.CoverageActions = state.Actions; p.CoverageBatch = state.Batch;
            if (before != 0 && state.Current == 0) p.Intent = AriaSkirmishIntent.Inspect;
            if (action == Game.Simulation.AriaCoverageAction.Recruit)
            { p.Intent = AriaSkirmishIntent.Recruit; Target(CoverageTarget(v, kind), false, ref o); return true; }
            if (action == Game.Simulation.AriaCoverageAction.Waiting)
            { p.Intent = AriaSkirmishIntent.Recruit; o.Kind = AriaPlayObservationKind.Waiting; return true; }
            return false;
        }

        static AriaTouchTarget CoverageTarget(in AriaSkirmishObservation v, Game.Simulation.AriaCoverageKind kind) => kind switch
        {
            Game.Simulation.AriaCoverageKind.Marksman => v.RecruitMarksman,
            Game.Simulation.AriaCoverageKind.ApcFast => v.RecruitApcFast,
            Game.Simulation.AriaCoverageKind.Radar => v.RecruitRadar,
            Game.Simulation.AriaCoverageKind.ApcHeavy => v.RecruitApcHeavy,
            Game.Simulation.AriaCoverageKind.Drone => v.RecruitDrone,
            Game.Simulation.AriaCoverageKind.TransportHeli => v.RecruitTransportHeli,
            Game.Simulation.AriaCoverageKind.Gunner => v.RecruitGunner,
            Game.Simulation.AriaCoverageKind.Tank => v.RecruitTank,
            Game.Simulation.AriaCoverageKind.Siege => v.RecruitSiege,
            _ => default
        };

        static bool StepGenericSupportOrder(in AriaSkirmishObservation v, ref AriaSkirmishPlanComponent p,
            ref AriaPlaySessionComponent t, ref AriaPlayObservationComponent o)
        {
            if (!CoverageEligible(v)) return false;
            var facts = new Game.Simulation.AriaSupportView {
                FieldedMask = ((v.CoverageFieldedMask & 4) != 0 ? 1 : 0) | ((v.CoverageFieldedMask & 16) != 0 ? 2 : 0) | ((v.CoverageFieldedMask & 32) != 0 ? 4 : 0),
                Busy = t.Phase == AriaPlayPhase.Manual || v.DrawerOpen || v.PlacementOpen || v.MapOpen || v.PresentedHomeThreat ||
                    p.HomeDefenseStage != 0 || p.PrecisionStage != 0 || p.AdvanceNavigation != 0 || p.AssaultInspectStage != 0 ||
                    p.PadStage != 0 || p.InterceptStage != 0 || p.AssaultFocusStage == 1 || p.BreacherStage == 1 || p.CoverageCurrent != 0,
                ArmyMoving = p.AssaultIssued != 0, EnemyAlive = v.EnemyDesignatedAlive,
                Mode = v.AttackMode ? Game.Simulation.AriaSupportMode.Attack : v.MoveMode ? Game.Simulation.AriaSupportMode.Move :
                    v.ScanMode ? Game.Simulation.AriaSupportMode.Scan : Game.Simulation.AriaSupportMode.None,
                ToggleAvailable = v.SelectedSquadToToggle.Available, MoveAvailable = v.Move.Available, AttackAvailable = v.Attack.Available,
                ScanAvailable = v.Scan.Available, RearAvailable = v.RearGround.Available, FrontAvailable = v.FrontGround.Available,
                SupportSlots = v.ExpandedSupportSlots,
                SelectedMask = v.ExpandedSelectedMask, SelectedGroups = v.ExpandedSelectedGroups, SelectedOffPage = v.ExpandedSelectedOffPage,
                CompletedContacts = t.Actions, Time = v.Time };
            for (int slot = 0; slot < 5; slot++) if (v.Squad(slot).Available) facts.AvailableSquadMask |= 1 << slot;
            var state = new Game.Simulation.AriaSupportState { Stage = p.SupportStage, Kind = p.SupportKind, Tried = p.SupportTried, Done = p.SupportDone,
                Slot = p.SupportSlot, Actions = p.SupportActions, Deadline = p.SupportDeadline, ReadyAt = p.SupportReadyAt };
            bool handled = Game.Simulation.AriaSupportOrderPolicy.Step(facts, ref state, out var decision);
            p.SupportStage = state.Stage; p.SupportKind = state.Kind; p.SupportTried = state.Tried; p.SupportDone = state.Done;
            p.SupportSlot = state.Slot; p.SupportActions = state.Actions; p.SupportDeadline = state.Deadline; p.SupportReadyAt = state.ReadyAt;
            if (!handled) return false;
            switch (decision.Action)
            {
                case Game.Simulation.AriaSupportAction.ToggleSelected: p.Intent = AriaSkirmishIntent.SelectSquad; Target(v.SelectedSquadToToggle, false, ref o); break;
                case Game.Simulation.AriaSupportAction.SelectSlot: p.Intent = AriaSkirmishIntent.SelectSquad; Target(v.Squad(decision.Slot), false, ref o); break;
                case Game.Simulation.AriaSupportAction.ArmMove: p.Intent = AriaSkirmishIntent.Advance; Target(v.Move, false, ref o); break;
                case Game.Simulation.AriaSupportAction.ArmAttack: p.Intent = AriaSkirmishIntent.Attack; Target(v.Attack, false, ref o); break;
                case Game.Simulation.AriaSupportAction.ArmScan: p.Intent = AriaSkirmishIntent.Scout; Target(v.Scan, false, ref o); break;
                case Game.Simulation.AriaSupportAction.TargetRear: p.Intent = AriaSkirmishIntent.Advance; Target(v.RearGround, true, ref o); break;
                case Game.Simulation.AriaSupportAction.TargetFront: p.Intent = AriaSkirmishIntent.Scout; Target(v.FrontGround, true, ref o); break;
                default: p.Intent = AriaSkirmishIntent.Advance; o.Kind = AriaPlayObservationKind.Waiting; break;
            }
            return true;
        }
    
        // One readiness research per stage when it unlocks something unused (S049: Established for the
        // tank, radar, apc_heavy, helipad and satellite dish; Full Arsenal for the siege).
        static bool StepGenericResearch(in AriaSkirmishObservation v, ref AriaSkirmishPlanComponent p,
            ref AriaPlaySessionComponent t, ref AriaPlayObservationComponent o)
        {
            if (!CoverageEligible(v)) return false;
            var facts = new Game.Simulation.AriaResearchView {
                AssaultSeen = p.CoverageAssaultSeen != 0,
                Busy = OtherThanResearch(v, p, t) || p.CoverageCurrent != 0 || p.BreacherStage == 1 ||
                    (p.ResearchCurrent == 0 && v.DrawerOpen),
                EnemyAlive = v.EnemyDesignatedAlive,
                CoverageIdle = Game.Simulation.AriaCoverageProgress.Idle(p.CoverageDone, p.CoverageCurrent, v.CoverageAffordMask, v.CoveragePendingMask),
                InProgress = v.ResearchInProgress, CanUpgrade = v.CanUpgradeReadiness, UpgradeAvailable = v.UpgradeReadiness.Available,
                Stage = v.ReadinessStage, Cost = v.ResearchCost, Materials = v.OwnMaterials,
                Reserve = Game.Simulation.AriaResearchPolicy.Reserve, NeedMask = v.ResearchNeedMask,
                CompletedContacts = t.Actions, Time = v.Time };
            var state = new Game.Simulation.AriaResearchState { Current = p.ResearchCurrent, Tried = p.ResearchTried, Done = p.ResearchDone,
                Actions = p.ResearchActions, Deadline = p.ResearchDeadline };
            byte before = p.ResearchCurrent;
            bool handled = Game.Simulation.AriaResearchPolicy.Step(facts, ref state, out var decision);
            p.ResearchCurrent = state.Current; p.ResearchTried = state.Tried; p.ResearchDone = state.Done;
            p.ResearchActions = state.Actions; p.ResearchDeadline = state.Deadline;
            if (before != 0 && state.Current == 0) p.Intent = AriaSkirmishIntent.Inspect; // close the drawer
            if (!handled) return false;
            p.Intent = AriaSkirmishIntent.Recruit; // keeps the drawer open between Build and the readiness button
            if (decision.Action == Game.Simulation.AriaResearchAction.Upgrade) Target(v.UpgradeReadiness, false, ref o);
            else o.Kind = AriaPlayObservationKind.Waiting;
            return true;
        }

        // Owners other than this research (its own drawer is not "another owner").
        static bool OtherThanResearch(in AriaSkirmishObservation v, in AriaSkirmishPlanComponent p, in AriaPlaySessionComponent t) =>
            t.Phase == AriaPlayPhase.Manual || v.AttackMode || v.MoveMode || v.ScanMode || v.MapOpen || v.PlacementOpen ||
            v.PresentedHomeThreat || p.HomeDefenseStage != 0 || p.PrecisionStage != 0 || p.AdvanceNavigation != 0 ||
            p.AssaultInspectStage != 0 || p.InterceptStage != 0 || p.PadStage != 0 || p.SupportStage != 0 || p.BuildCurrent != 0;

        // Producer buildings the mission offers and the player lacks (S049: helipad, satellite dish), built once
        // each through the Build drawer and a legal home pad site (StepHomePadPlacement), after the coverage
        // purchases and research. Gives up cleanly (cancelling any open preview) when no legal site is offered.
        static bool StepGenericBuilding(in AriaSkirmishObservation v, ref AriaSkirmishPlanComponent p,
            ref AriaPlaySessionComponent t, ref AriaPlayObservationComponent o)
        {
            if (!CoverageEligible(v)) return false;
            bool placing = p.BuildCurrent != 0;
            bool busy = t.Phase == AriaPlayPhase.Manual || v.PresentedHomeThreat || p.HomeDefenseStage != 0 || p.PrecisionStage != 0 ||
                p.AdvanceNavigation != 0 || p.AssaultInspectStage != 0 || p.InterceptStage != 0 || p.SupportStage != 0 ||
                p.CoverageCurrent != 0 || p.BreacherStage == 1 || p.ResearchCurrent != 0 ||
                (!placing && (v.AttackMode || v.MoveMode || v.ScanMode || v.MapOpen || v.DrawerOpen || p.PadStage != 0));
            int cards = (v.AirPad.Available ? 1 : 0) | (v.BuildDish.Available ? 2 : 0);
            var facts = new Game.Simulation.AriaBuildingView {
                AssaultSeen = p.CoverageAssaultSeen != 0, Busy = busy, EnemyAlive = v.EnemyDesignatedAlive,
                CoverageIdle = Game.Simulation.AriaCoverageProgress.Idle(p.CoverageDone, p.CoverageCurrent, v.CoverageAffordMask, v.CoveragePendingMask),
                ResearchIdle = p.ResearchCurrent == 0 && !v.ResearchInProgress, PlacementOpen = v.PlacementOpen,
                PlacementGaveUp = placing && p.PadStage == 8,
                NeedMask = v.BuildingNeedMask, AffordMask = v.BuildingAffordMask,
                PresentMask = v.BuildingPresentMask | (placing && p.PadStage == BuildingPlaced ? Game.Simulation.AriaBuildingPolicy.Bit(p.BuildCurrent) : 0),
                CardMask = cards, Time = v.Time };
            var state = new Game.Simulation.AriaBuildingState { Current = p.BuildCurrent, Tried = p.BuildTried, Done = p.BuildDone, Deadline = p.BuildDeadline };
            bool handled = Game.Simulation.AriaBuildingPolicy.Step(facts, ref state, out var decision);
            p.BuildCurrent = state.Current; p.BuildTried = state.Tried; p.BuildDone = state.Done; p.BuildDeadline = state.Deadline;
            if (placing && state.Current == 0) { p.PadStage = 0; p.Intent = AriaSkirmishIntent.Inspect; }
            if (!handled) return false;
            switch (decision.Action)
            {
                case Game.Simulation.AriaBuildingAction.Start:
                    p.PadStage = 1; p.PadDeadline = v.Time + 45f; p.PadReadyAt = 0;
                    StepHomePadPlacement(v, ref p, ref t, ref o); return true;
                case Game.Simulation.AriaBuildingAction.Continue:
                    StepHomePadPlacement(v, ref p, ref t, ref o); return true;
                case Game.Simulation.AriaBuildingAction.Cancel:
                    Target(v.PlacementCancel, false, ref o); return true;
            }
            return false;
        }

        // Pad placement stage meaning "this building's preview was confirmed and closed".
        public const int BuildingPlaced = 11;
        // The build card StepHomePadPlacement taps: the helipad (AirPad) or, for coverage, the satellite dish.
        static AriaTouchTarget PadCard(in AriaSkirmishObservation v, in AriaSkirmishPlanComponent p) =>
            p.BuildCurrent == 2 ? v.BuildDish : v.AirPad;
}
}
