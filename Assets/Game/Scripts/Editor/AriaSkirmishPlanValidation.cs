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
            var view = new AriaSkirmishObservation { Active = true, Time = 1, Infantry = 8, AvailableSquads = 1, SelectedSlot = -1,
                Recruit = new AriaTouchTarget { Available = true, Id = 11 }, Squad0 = new AriaTouchTarget { Available = true, Id = 12 },
                FocusEnemy = new AriaTouchTarget { Available = true, Id = 13 }, Attack = new AriaTouchTarget { Available = true, Id = 14 } };
            var plan = new AriaSkirmishPlanComponent(); var touch = new AriaPlaySessionComponent(); var output = new AriaPlayObservationComponent();
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
            Check(output.Kind == AriaPlayObservationKind.Waiting && plan.Slot == 1, "wait after accepted gesture, no rapid retapping");
            view.Time += 10; view.AvailableSquads = 1; view.SelectedSlot = 0;
            view.FocusThreat = new AriaTouchTarget { Available = true, Id = -20003 };
            AriaSkirmishPlanSystem.Step(view, ref plan, ref touch, ref output);
            Check(output.TargetId == -20003 && plan.Intent == AriaSkirmishIntent.FindThreat, "visible map threat takes priority over base");
            view.MapOpen = true; view.FocusThreat.Id = -20004; view.CloseMap = new AriaTouchTarget { Available = true, Id = 16 };
            AriaSkirmishPlanSystem.Step(view, ref plan, ref touch, ref output);
            Check(output.TargetId == -20004, "focus map contact through tactical map");
            touch.TargetId = -20004; touch.Actions = plan.MapFocusActions;
            AriaSkirmishPlanSystem.Step(view, ref plan, ref touch, ref output);
            Check(output.TargetId == 16, "one focus gesture closes map even for moving contact");
            touch.TargetId = 16;
            AriaSkirmishPlanSystem.Step(view, ref plan, ref touch, ref output);
            Check(output.TargetId == 16, "close intent persists while hand approaches close");
            view.MapContactInView = true;
            AriaSkirmishPlanSystem.Step(view, ref plan, ref touch, ref output);
            Check(output.TargetId == 16, "close tactical map before world order");
            view.MapOpen = false;
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
            return "[AriaSkirmishPlanValidation] result=Passed cases=18";
        }
        private static void Check(bool valid, string name) { if (!valid) throw new InvalidOperationException(name); }
    }
}
#endif
