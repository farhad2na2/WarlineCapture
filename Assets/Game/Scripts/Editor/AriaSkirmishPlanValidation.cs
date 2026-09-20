#if UNITY_EDITOR
using System;
using Game.UI.Contracts;
using Game.UI.Shell.Ecs;
using Game.UI.Shell.Contracts.Ecs;
namespace Game.Editor
{
    public static class AriaSkirmishPlanValidation
    {
        public static string Run()
        {
            int checks = 0;
            void Check(bool valid, string name) { if (!valid) throw new InvalidOperationException(name); checks++; }
            var view = new AriaSkirmishObservation { Active = true, Time = 1, Infantry = 8, AvailableSquads = 1, SelectedSlot = -1,
                Recruit = new AriaTouchTarget { Available = true, Id = 11 }, Squad0 = new AriaTouchTarget { Available = true, Id = 12 },
                FocusEnemy = new AriaTouchTarget { Available = true, Id = 13 }, Attack = new AriaTouchTarget { Available = true, Id = 14 } };
            var plan = new AriaSkirmishPlanComponent { AssaultStarted = 1 }; var touch = new AriaPlaySessionComponent(); var output = new AriaPlayObservationComponent();
            AriaSkirmishPlanSystem.Step(view, ref plan, ref touch, ref output);
            Check(output.Kind == AriaPlayObservationKind.Waiting, "no action before consent");
            touch.Phase = AriaPlayPhase.Observing;
            AriaSkirmishPlanSystem.Step(view, ref plan, ref touch, ref output);
            Check(output.TargetId == 11, "visible recruitment control");
            view.Recruit = default; view.DrawerOpen = true; view.CloseDrawer = new AriaTouchTarget { Available = true, Id = 15 };
            AriaSkirmishPlanSystem.Step(view, ref plan, ref touch, ref output);
            view.Time += 4;
            AriaSkirmishPlanSystem.Step(view, ref plan, ref touch, ref output);
            Check(output.TargetId == 15 && plan.NextRecruitAt > view.Time, "unaffordable order closes drawer and backs off");
            plan.RecruitBurstRemaining = 2; view.Time += .1f;
            AriaSkirmishPlanSystem.Step(view, ref plan, ref touch, ref output);
            Check(output.TargetId == 15, "recruitment backoff must not cancel an in-flight close-drawer gesture");
            plan.RecruitBurstRemaining = 0;
            view.DrawerOpen = false;
            AriaSkirmishPlanSystem.Step(view, ref plan, ref touch, ref output);
            Check(output.TargetId == 12, "select existing squad");
            view.SelectionVisible = true; view.SelectedSlot = 0;
            AriaSkirmishPlanSystem.Step(view, ref plan, ref touch, ref output);
            Check(output.TargetId == 13, "offscreen target uses visible navigation");
            view.EnemyBase = new AriaTouchTarget { Available = true, Id = -20001 };
            AriaSkirmishPlanSystem.Step(view, ref plan, ref touch, ref output);
            Check(output.TargetId == 14, "attack mode before world touch");
            view.AttackMode = true;
            AriaSkirmishPlanSystem.Step(view, ref plan, ref touch, ref output);
            Check(output.Kind == AriaPlayObservationKind.WorldTarget, "only observable target can be tapped");
            touch.TargetId = -20001; touch.Actions++;
            AriaSkirmishPlanSystem.Step(view, ref plan, ref touch, ref output);
            Check(output.Kind == AriaPlayObservationKind.Waiting && plan.Slot == 0 && plan.Cycle == 1, "wait after accepted gesture, no rapid retapping");
            view.Time += 15; view.AvailableSquads = 1; view.SelectedSlot = 0; plan.NextRecruitAt = 100;
            view.FocusThreat = new AriaTouchTarget { Available = true, Id = -20003 };
            AriaSkirmishPlanSystem.Step(view, ref plan, ref touch, ref output);
            Check(output.TargetId == -20003, "navigate to the nearest observed threat instead of attacking distant defenders");
            view.MapOpen = true; view.FocusThreat.Id = -20004; view.CloseMap = new AriaTouchTarget { Available = true, Id = 16 };
            AriaSkirmishPlanSystem.Step(view, ref plan, ref touch, ref output);
            Check(output.TargetId == -20004, "focus the observed threat once on the open map");
            view.FocusThreatDrag = true; view.FocusThreatDragEnd = new UnityEngine.Vector2(500, 400);
            AriaSkirmishPlanSystem.Step(view, ref plan, ref touch, ref output);
            Check(output.Drag == 1 && output.DragEnd == view.FocusThreatDragEnd, "inside-viewport focus requires a real map drag");
            view.FocusThreatDrag = false;
            touch.TargetId = -20003; touch.Actions = plan.MapFocusActions + 1;
            AriaSkirmishPlanSystem.Step(view, ref plan, ref touch, ref output);
            Check(output.TargetId == -20004, "opening-map completion is not focus completion");
            touch.TargetId = -20004; touch.Actions = plan.MapFocusActions + 2;
            AriaSkirmishPlanSystem.Step(view, ref plan, ref touch, ref output);
            Check(output.TargetId == 16, "one focus gesture closes map even for moving contact");
            touch.TargetId = 16;
            AriaSkirmishPlanSystem.Step(view, ref plan, ref touch, ref output);
            Check(output.TargetId == 16, "close intent persists while hand approaches close");
            view.MapContactInView = true;
            AriaSkirmishPlanSystem.Step(view, ref plan, ref touch, ref output);
            Check(output.TargetId == 16, "close tactical map before world order");
            view.MapOpen = false;
            AriaSkirmishPlanSystem.Step(view, ref plan, ref touch, ref output);
            Check(plan.Intent == AriaSkirmishIntent.FindThreat, "camera settle cannot interrupt attack with recruitment");
            view.Time += 2;
            view.Threat = new AriaTouchTarget { Available = true, Id = -20002 };
            AriaSkirmishPlanSystem.Step(view, ref plan, ref touch, ref output);
            Check(output.TargetId == -20002 && plan.Intent == AriaSkirmishIntent.TargetThreat, "attack presented threat before base");
            touch.TargetId = -20002; touch.Actions++;
            AriaSkirmishPlanSystem.Step(view, ref plan, ref touch, ref output);
            Check(output.Kind == AriaPlayObservationKind.Waiting, "threat tap also advances and waits");
            view.Time = 30; plan.ObserveUntil = 0; plan.Intent = AriaSkirmishIntent.Attack;
            plan.NextRecruitAt = 0; view.Recruit = new AriaTouchTarget { Available = true, Id = 11 };
            view.AttackMode = false; view.SelectedSlot = 0;
            AriaSkirmishPlanSystem.Step(view, ref plan, ref touch, ref output);
            Check(output.TargetId == 14, "recruitment cannot interrupt a combat sequence");
            view.Time = 200;
            AriaSkirmishPlanSystem.Step(view, ref plan, ref touch, ref output);
            Check(touch.Phase == AriaPlayPhase.Blocked, "tap loop is not match progress");
            view.Finished = true;
            AriaSkirmishPlanSystem.Step(view, ref plan, ref touch, ref output);
            Check(output.Kind == AriaPlayObservationKind.Finished, "terminal outcome is never replayed");
            // Group selection uses the same visible controls and drag input as the player.
            view = new AriaSkirmishObservation { Active = true, Time = 1, Infantry = 24,
                AvailableSquads = 1, FocusPlayer = new AriaTouchTarget { Available = true, Id = 20 },
                Select = new AriaTouchTarget { Available = true, Id = 21 },
                FocusEnemy = new AriaTouchTarget { Available = true, Id = 22 } };
            plan = new AriaSkirmishPlanComponent { AssaultStarted = 1 }; touch = new AriaPlaySessionComponent { Phase = AriaPlayPhase.Observing };
            Step(); Check(output.TargetId == 20, "focus troops before selecting a group");
            touch.Actions++; touch.TargetId = 20; view.Time = 2;
            Step(); Check(output.Kind == AriaPlayObservationKind.Waiting, "wait for camera to settle");
            view.Time = 5;
            Step(); Check(output.Kind == AriaPlayObservationKind.Waiting, "never press Select without a usable rectangle");
            view.GroupStart = new AriaTouchTarget { Available = true, Id = -20005, Position = new UnityEngine.Vector2(200, 200) };
            view.GroupEnd = new AriaTouchTarget { Available = true, Id = -20005, Position = new UnityEngine.Vector2(600, 500) };
            view.GroupEnd.Available = false;
            Step(); Check(output.TargetId != 21, "one clear corner is insufficient for Select");
            view.GroupEnd.Available = true;
            var end = view.GroupEnd.Position;
            view.GroupEnd.Position = view.GroupStart.Position;
            Step(); Check(output.TargetId != 21, "zero-size rectangle cannot arm Select");
            view.GroupEnd.Position = end;
            Step(); Check(output.TargetId == 21, "activate rectangle selection only with valid drag ready");
            touch.Actions++; touch.TargetId = 21; view.SelectionMode = true;
            Step(); Check(output.Drag == 1 && output.DragEnd == view.GroupEnd.Position, "selection is a drag, never a soldier tap");
            touch.Actions++; touch.TargetId = -20005; view.SelectionVisible = true;
            Step(); Check(plan.GroupStage == 3 && output.TargetId == 22 && output.Drag == 0, "completed selection advances without repeating drag");
            view.SelectedSlot = -1; view.SelectionVisible = true; view.EnemyBase = new AriaTouchTarget { Available = true, Id = -20001 };
            plan.TargetPending = 1; plan.ActionsAtTarget = touch.Actions; touch.TargetId = -20001; touch.Actions++;
            Step(); Check(plan.GroupStage == 3 && plan.Slot == 0 && plan.Cycle == 1,
                "completed group attack preserves multi-selection instead of splitting into cards");
            plan.Cycle = 0; plan.ObserveUntil = 0;
            plan.GroupStage = 1; plan.GroupReadyAt = 1; plan.GroupAction = 0;
            view.Time = 10; view.GroupStart = default; view.GroupEnd = default;
            view.SelectedSlot = 0;
            Step(); Check(plan.GroupStage == 4, "card fallback is never treated as whole-army selection");
            plan.TargetPending = 1; plan.ActionsAtTarget = touch.Actions;
            touch.TargetId = -20001; touch.Actions++;
            Step(); Check(plan.Slot == 0 && plan.Cycle == 1, "fallback completes the cycle when other squads are empty");
            view = new AriaSkirmishObservation { Active = true, Time = 10, Infantry = 8,
                AvailableSquads = 1, Squad0 = new AriaTouchTarget { Available = true, Id = 20 } };
            plan = new AriaSkirmishPlanComponent { RecruitDrawerSeen = 1 };
            touch = new AriaPlaySessionComponent { Phase = AriaPlayPhase.Observing };
            Step(); Check(plan.AssaultStarted == 0 && output.Kind == AriaPlayObservationKind.Waiting,
                "opening preserves normal engagement while waiting for the next recruitment");
            view.SelectionVisible = true; view.SelectedSlot = 0;
            view.Hold = new AriaTouchTarget { Available = true, Id = 30 };
            Step(); Check(output.TargetId != 30, "opening does not pin defenders with Hold");
            touch.TargetId = 30; touch.Actions++;
            Step(); Check(plan.Intent == AriaSkirmishIntent.ObserveBattle, "opening returns to recruitment rather than issuing more Hold orders");
            view.AvailableSquads = 5; view.Infantry = 12; view.Time = 12;
            Step(); Check(output.Kind == AriaPlayObservationKind.Waiting,
                "new arrivals retain their automatic rally instead of being stopped at delivery");
            view.AvailableSquads = 0; view.Infantry = 0; view.Time = 20;
            view.Recruit = new AriaTouchTarget { Available = true, Id = 11 };
            plan.Intent = AriaSkirmishIntent.GroupForce; plan.AssaultStarted = 1;
            Step(); Check(output.TargetId == 11 && plan.Intent == AriaSkirmishIntent.Recruit,
                "empty army recruits instead of selecting building markers");
            view.AvailableSquads = 9; view.Infantry = 6; view.Time = 30;
            plan = new AriaSkirmishPlanComponent { AssaultStarted = 1, Slot = 3,
                TargetPending = 1, ActionsAtTarget = touch.Actions };
            touch.TargetId = -20002; touch.Actions++;
            Step(); Check(plan.Slot == 0 && plan.Cycle == 1 && plan.Intent == AriaSkirmishIntent.ObserveBattle,
                "empty final vehicle slot still finishes the cycle and opens recruitment window");
            view.AvailableSquads = 31; view.Time = 40; view.Infantry = 3;
            plan = new AriaSkirmishPlanComponent { AssaultStarted = 1, GroupStage = 4,
                TargetPending = 1, ActionsAtTarget = touch.Actions };
            touch.TargetId = -20002; touch.Actions++;
            Step(); Check(output.TargetId == 11 && plan.Slot == 1,
                "depleted army recruits after an accepted order without waiting for every squad");
            view.Infantry = 12; plan.TargetPending = 1; plan.ActionsAtTarget = touch.Actions;
            plan.Intent = AriaSkirmishIntent.Attack; plan.RecruitBurstRemaining = 0;
            touch.Actions++; Step();
            Check(plan.Intent != AriaSkirmishIntent.Recruit && plan.Slot == 2,
                "viable army finishes its squad orders before returning to production");
            plan.Slot = 1; plan.Intent = AriaSkirmishIntent.Recruit; plan.RecruitBurstRemaining = 2;
            plan.RecruitDrawerSeen = 1; view.Time = 41;
            view.FocusPlayer = new AriaTouchTarget { Available = true, Id = 22 };
            view.Squad1 = new AriaTouchTarget { Available = true, Id = 23 };
            view.SelectedSlot = -1; view.SelectionVisible = false;
            Step(); Check(plan.RecruitBurstRemaining == 1 && output.Kind == AriaPlayObservationKind.Waiting,
                "depleted force waits briefly for a second recruitment without regrouping");
            view.Time = 45;
            Step(); Check(output.TargetId == 11, "second replenishment uses visible production controls");
            plan.RecruitDrawerSeen = 1; view.Time = 46;
            Step(); Check(plan.GroupStage == 4 && output.TargetId == 23 && plan.RecruitBurstRemaining == 0,
                "bounded replenishment resumes squad rotation without moving camera home");
            plan.GroupStage = 3; plan.RegroupAt = 0; view.Time = 47;
            Step(); Check(plan.GroupStage == 0 && plan.AssaultStarted == 0,
                "lost group selection starts a coordinated recovery");
            view = new AriaSkirmishObservation { Active = true, Time = 1, Infantry = 8,
                DefenseBuild = new AriaTouchTarget { Id = 50, Available = true },
                FocusPlayer = new AriaTouchTarget { Id = 51, Available = true } };
            plan = default; touch = new AriaPlaySessionComponent { Phase = AriaPlayPhase.Observing };
            Step(); Check(output.TargetId == 51, "defensive construction first focuses our base through its button");
            touch.Actions++; view.Time = 2; Step();
            Check(output.Kind == AriaPlayObservationKind.Waiting, "construction waits for camera");
            view.Time = 5; Step(); Check(output.TargetId == 50, "construction follows visible catalog controls");
            view.PlacementOpen = true;
            view.PlacementConfirm = new AriaTouchTarget { Id = 52, Available = true };
            Step(); Check(output.TargetId == 52 && plan.DefensesPlaced == 0, "valid placement still requires actual confirmation gesture");
            touch.Actions++; view.PlacementOpen = false; view.Time = 6; Step();
            Check(plan.DefensesPlaced == 1, "closed placement after confirmation acknowledges one construction");
            view.Time = 8; view.PlacementOpen = true; view.PlacementConfirm = default;
            view.PlacementCancel = new AriaTouchTarget { Id = 53, Available = true };
            Step(); Check(output.TargetId == 53 && plan.DefensesPlaced == 1, "invalid site cancels without counting a tower");
            view.PlacementOpen = false; view.Time = 9; Step();
            Check(plan.DefenseStage == 4, "failed construction cannot trap the rest of the match");
            view = new AriaSkirmishObservation { Active = true, Time = 10, Infantry = 24, AvailableSquads = 1,
                SelectionVisible = true, SelectedSlot = 0, AdvancePreferred = true,
                Attack = new AriaTouchTarget { Available = true, Id = 14 },
                AdvanceGround = new AriaTouchTarget { Available = true, Id = -20006 },
                Threat = new AriaTouchTarget { Available = true, Id = -20002 } };
            plan = new AriaSkirmishPlanComponent { AssaultStarted = 1, DefenseStage = 4, GroupStage = 4 };
            touch = new AriaPlaySessionComponent { Phase = AriaPlayPhase.Observing };
            Step(); Check(output.TargetId == 14, "advance first arms the existing Attack control");
            view.AttackMode = true;
            Step(); Check(output.TargetId == -20006 && output.Kind == AriaPlayObservationKind.WorldTarget,
                "advance uses a visible ground touch instead of chasing a moving enemy");
            view.AdvanceGround = default; view.Threat = default;
            view.EnemyBase = new AriaTouchTarget { Available = true, Id = -20001 };
            Step(); Check(output.TargetId == -20001, "visible base remains actionable when approach is obscured");
            view.EnemyBase = default;
            view.AdvanceGround = new AriaTouchTarget { Available = true, Id = -20006 };
            view.Threat = new AriaTouchTarget { Available = true, Id = -20002 };
            view.ThreatNearForce = true;
            Step(); Check(output.TargetId == -20002, "visible frontline threat gets coordinated focus instead of another advance");
            view.ThreatNearForce = false;
            Step();
            touch.TargetId = -20006; touch.Actions++;
            Step(); Check(plan.Cycle == 1 && plan.TargetPending == 0,
                "ground gesture completes squad order without repeat tapping");
            view = new AriaSkirmishObservation { Active = true, Time = 10, Infantry = 8, PlacementOpen = true,
                Site0 = new AriaTouchTarget { Available = true, Id = -20100 },
                Site1 = new AriaTouchTarget { Available = true, Id = -20101 },
                PlacementConfirm = new AriaTouchTarget { Available = true, Id = 52 } };
            plan = new AriaSkirmishPlanComponent { DefenseStage = 2, DefenseDeadline = 100 };
            touch = new AriaPlaySessionComponent { Phase = AriaPlayPhase.Observing };
            Step(); Check(output.TargetId == -20100 && output.Kind == AriaPlayObservationKind.WorldTarget,
                "defense position is an actual ground gesture before confirm");
            Step(); Check(output.TargetId == -20100, "placement target persists until gesture completes");
            touch.Actions++; view.Time = 11; Step();
            view.Time = 12; view.PlacementConfirm = default;
            Step(); Check(output.TargetId == -20101 && plan.DefensesPlaced == 0,
                "invalid site tries another visible position without forced placement");
            touch.Actions++; view.Time = 13; Step();
            view.Time = 14; view.PlacementConfirm = new AriaTouchTarget { Available = true, Id = 52 };
            Step(); Check(output.TargetId == 52 && plan.DefensesPlaced == 0,
                "green placement requires confirmation touch before counting construction");
            view = new AriaSkirmishObservation { Active = true, Time = 100, Infantry = 24, AvailableSquads = 1,
                ThreatNearForce = true, FocusEnemy = new AriaTouchTarget { Available = true, Id = 13 } };
            plan = new AriaSkirmishPlanComponent { DefenseStage = 4, NextRecruitAt = 999 };
            touch = new AriaPlaySessionComponent { Phase = AriaPlayPhase.Observing };
            Step(); Check(plan.AssaultStarted == 0, "a full army finishes the nearby defensive fight before departing");
            view.Infantry = 15; view.ThreatNearForce = false; view.Time = 200;
            Step(); Check(plan.AssaultStarted == 0, "a partial army does not depart at the old ninety-second deadline");
            view.Infantry = 24;
            Step(); Check(plan.AssaultStarted == 1, "assembled army can advance after the threat clears");
            view.AdvancePreferred = true; view.ThreatNearForce = true; view.SelectionVisible = true; view.SelectedSlot = 0;
            view.FocusThreat = new AriaTouchTarget { Available = true, Id = -20003 };
            plan.GroupStage = 4; plan.Intent = AriaSkirmishIntent.Attack;
            Step(); Check(output.TargetId == -20003, "offscreen frontline threat is brought into view instead of sending troops past it");
            view = new AriaSkirmishObservation { Active = true, Time = 10, Infantry = 20, AvailableSquads = 1,
                SelectionVisible = true, SelectedCount = 16, SelectedSlot = -1,
                Recruit = new AriaTouchTarget { Available = true, Id = 11 },
                FocusEnemy = new AriaTouchTarget { Available = true, Id = 13 } };
            plan = new AriaSkirmishPlanComponent { DefenseStage = 4, GroupStage = 3, AssaultStarted = 1,
                Intent = AriaSkirmishIntent.ObserveBattle };
            touch = new AriaPlaySessionComponent { Phase = AriaPlayPhase.Observing };
            Step(); Check(output.TargetId == 13 && plan.GroupStage == 3,
                "healthy selected force continues its assault instead of opening production mid-fight");
            view.SelectedCount = 6; view.Infantry = 10;
            Step(); Check(plan.GroupStage == 0 && plan.AssaultStarted == 0 && plan.OpeningUntil > view.Time,
                "depleted assault rebuilds a force instead of feeding isolated replacements");
            view = new AriaSkirmishObservation { Active = true, Time = 100, Infantry = 16, AvailableSquads = 1,
                SelectedCount = 16, SelectionVisible = true, SelectedSlot = -1, AssaultAtBase = true,
                AttackMode = true, EnemyBase = new AriaTouchTarget { Available = true, Id = -20001 },
                ThreatNearForce = true, Threat = new AriaTouchTarget { Available = true, Id = -20002 } };
            plan = new AriaSkirmishPlanComponent { DefenseStage = 4, GroupStage = 3, AssaultStarted = 1 };
            touch = new AriaPlaySessionComponent { Phase = AriaPlayPhase.Observing };
            Step(); Check(output.TargetId == -20001, "gathered assault attacks the base instead of endlessly chasing recruits");
            touch.TargetId = -20001; touch.Actions++;
            Step(); Check(plan.ObserveUntil >= view.Time + 12 && plan.GroupStage == 3,
                "base assault keeps its group and gets time to fire before replanning");
            view.Time += 13; view.SelectedCount = 4; view.Infantry = 4; view.EnemyHealth = 294;
            Step(); Check(plan.GroupStage == 3 && output.TargetId == -20001,
                "survivors finish a critically damaged base instead of abandoning their attack");
            view.SelectionVisible = false; view.SelectedCount = 0; view.AssaultAtBase = false;
            view.Infantry = 5; plan.TargetPending = 0; plan.ObserveUntil = 0;
            Step(); Check(plan.GroupStage == 0 && plan.AssaultStarted == 0 && plan.OpeningUntil > view.Time,
                "entire selected group dying rebuilds before sending isolated replacement squads");
            view = new AriaSkirmishObservation { Active = true, Time = 20, Infantry = 24,
                AvailableSquads = 1, FocusPlayer = new AriaTouchTarget { Available = true, Id = 20 },
                FocusGroup = new AriaTouchTarget { Available = true, Id = -20007 },
                Select = new AriaTouchTarget { Available = true, Id = 24 } };
            plan = new AriaSkirmishPlanComponent { DefenseStage = 4, AssaultStarted = 1, NextRecruitAt = 999 };
            touch = new AriaPlaySessionComponent { Phase = AriaPlayPhase.Observing };
            Step(); Check(output.TargetId == -20007 && plan.GroupStage == 7, "grouping navigates through the actual map control");
            touch.Phase = AriaPlayPhase.Aiming; view.Time += .5f;
            Step(); Check(output.TargetId == -20007 && plan.GroupStage == 7,
                "map opening persists during hand approach instead of becoming a squad order");
            touch.TargetId = -20007; touch.Actions++; touch.Phase = AriaPlayPhase.Verifying;
            Step(); Check(output.TargetId == -20007,
                "map opening waits for the visible popup after touch completion");
            view.MapOpen = true; view.FocusGroup.Id = -20008; view.FocusGroupDrag = true;
            view.FocusGroupDragEnd = new UnityEngine.Vector2(400, 500);
            view.CloseMap = new AriaTouchTarget { Available = true, Id = 25 };
            Step(); Check(output.TargetId == -20008 && output.Drag == 1, "group focus drags map viewport when required");
            touch.TargetId = -20008; touch.Actions++;
            Step(); Check(output.TargetId == 25, "one group focus closes the map once");
            view.MapOpen = false; view.Time = 21; Step();
            Check(plan.GroupStage == 6 && output.Kind == AriaPlayObservationKind.Waiting, "map focus settles before group selection");
            view.Time = 29;
            view.GroupStart = new AriaTouchTarget { Available = true, Id = -20005, Position = new UnityEngine.Vector2(200, 200) };
            view.GroupEnd = new AriaTouchTarget { Available = true, Id = -20005, Position = new UnityEngine.Vector2(600, 500) };
            Step(); Check(output.TargetId == 24, "map-focused group arms Select with a valid box");
            view.SelectionMode = true;
            Step(); Check(output.Drag == 1 && plan.GroupStage == 2, "map grouping completes with the real selection drag");
            view = new AriaSkirmishObservation { Active = true, Time = 10, Infantry = 8, PlacementOpen = true,
                Site1 = new AriaTouchTarget { Available = true, Id = -20101 },
                PlacementConfirm = new AriaTouchTarget { Available = true, Id = 52 },
                PlacementCancel = new AriaTouchTarget { Available = true, Id = 53 } };
            plan = new AriaSkirmishPlanComponent { DefenseStage = 2, DefenseDeadline = 100,
                DefenseSitePending = 1, DefenseSiteAction = 4 };
            touch = new AriaPlaySessionComponent { Phase = AriaPlayPhase.Aiming, Actions = 4, TargetId = -20100 };
            Step(); Check(output.TargetId == -20101 && plan.DefenseSite == 1,
                "camera-obscured pending site changes to a visible ground target before hand timeout");
            view.Site1 = default;
            Step(); Check(output.TargetId == 52 && plan.DefenseStage == 3 && plan.DefensesPlaced == 0,
                "no visible sites uses the normal valid preview confirmation without counting unbuilt defense");
            plan = new AriaSkirmishPlanComponent { DefenseStage = 2, DefenseDeadline = 100,
                DefenseSitePending = 1, DefenseSiteAction = 4 };
            view.PlacementConfirm = default;
            Step(); Check(output.TargetId == 53 && plan.DefensesPlaced == 0,
                "obscured sites and invalid preview cancel normally instead of blocking forever");
            return $"[AriaSkirmishPlanValidation] result=Passed cases={checks}";

            void Step() => AriaSkirmishPlanSystem.Step(view, ref plan, ref touch, ref output);
        }
    }
}
#endif
