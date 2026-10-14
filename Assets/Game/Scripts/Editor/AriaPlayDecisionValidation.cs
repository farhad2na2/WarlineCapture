#if UNITY_EDITOR
using System;
using Game.UI.Contracts;
using Game.UI.Shell.Contracts.Ecs;
using Game.UI.Shell.Ecs;
using UnityEngine;
namespace Game.Editor
{
    public static class AriaPlayDecisionValidation
    {
        public static string Run()
        {
            var s = new AriaPlaySessionComponent();
            var o = new AriaPlayObservationComponent { Kind = AriaPlayObservationKind.Control, TargetId = 3, GoalId = 501, Position = new Vector2(100, 200), Time = 1 };
            AriaPlayDecisionSystem.Step(o, ref s);
            Check(s.Phase == AriaPlayPhase.Manual, "never starts without confirmation");
            s.Phase = AriaPlayPhase.Observing;
            AriaPlayDecisionSystem.Step(o, ref s);
            Check(s.Phase == AriaPlayPhase.Aiming && s.GestureRequested == 0, "visible preparation before touch");
            o.Time = 2; o.Position += Vector2.right * 100;
            AriaPlayDecisionSystem.Step(o, ref s);
            Check(s.GestureRequested == 0 && s.Target == o.Position, "moving control re-aims");
            o.Time = 2.4f; AriaPlayDecisionSystem.Step(o, ref s);
            Check(s.Phase == AriaPlayPhase.Touching && s.GestureRequested == 1, "stable control taps");
            s.GestureRequested = 0; o.Time = 4;
            AriaPlayDecisionSystem.Step(o, ref s);
            Check(s.GestureRequested == 0 && s.Attempts == 1, "no overlapping gestures");
            s.Phase = AriaPlayPhase.Verifying; s.Attempts = 3;
            AriaPlayDecisionSystem.Step(o, ref s);
            Check(s.Phase == AriaPlayPhase.Blocked, "bounded retries");
            s = new AriaPlaySessionComponent { Phase = AriaPlayPhase.Observing, GoalId = 501, LastProgressAt = 1 };
            o.Kind = AriaPlayObservationKind.Waiting; o.Time = 50;
            AriaPlayDecisionSystem.Step(o, ref s);
            Check(s.Phase == AriaPlayPhase.Waiting && s.GestureRequested == 0, "defend timer is not an input prompt");
            o.Time = 182; AriaPlayDecisionSystem.Step(o, ref s);
            Check(s.Phase == AriaPlayPhase.Blocked, "silent wait bounded");
            s = new AriaPlaySessionComponent { Phase = AriaPlayPhase.Observing };
            o = new AriaPlayObservationComponent { Kind = AriaPlayObservationKind.WorldTarget, TargetId = 7,
                GoalId = 804, Position = new Vector2(400, 300), Time = 1 };
            AriaPlayDecisionSystem.Step(o, ref s);
            o.Kind = AriaPlayObservationKind.Waiting; o.Time = 1.2f; AriaPlayDecisionSystem.Step(o, ref s);
            Check(s.Phase == AriaPlayPhase.Aiming, "transient wait preserves a prepared world-target aim");
            o.Kind = AriaPlayObservationKind.WorldTarget; o.Time = 2; AriaPlayDecisionSystem.Step(o, ref s);
            Check(s.Phase == AriaPlayPhase.Touching && s.GestureRequested == 1,
                "world target submits after a transient waiting frame");
            s = new AriaPlaySessionComponent { Phase = AriaPlayPhase.Aiming, DueAt = 2, LastProgressAt = 1 };
            o = new AriaPlayObservationComponent { Kind = AriaPlayObservationKind.Waiting, Time = 4 };
            AriaPlayDecisionSystem.Step(o, ref s);
            Check(s.Phase == AriaPlayPhase.Waiting, "a stale hidden aim returns to waiting");
            s.Phase = AriaPlayPhase.Observing; o.Kind = AriaPlayObservationKind.Finished;
            AriaPlayDecisionSystem.Step(o, ref s);
            Check(s.Phase == AriaPlayPhase.Manual, "result ends session");
            s = new AriaPlaySessionComponent { Phase = AriaPlayPhase.Aiming, TargetId = 2, GoalId = 501, Target = o.Position };
            o.Kind = AriaPlayObservationKind.Control; o.Time = 200;
            AriaPlayDecisionSystem.Step(o, ref s);
            Check(s.Phase == AriaPlayPhase.Aiming && s.GestureRequested == 0 && s.DueAt > o.Time,
                "replacement control at same position receives a fresh aiming delay");
            Check(!AriaPlayInputSystem.IsCurrentMatchFinished(true, Game.Components.SkirmishPhase.Playing,
                true, Game.Missions.Contracts.MissionOutcomeKind.Victory), "previous campaign victory does not stop Skirmish");
            Check(AriaPlayInputSystem.IsCurrentMatchFinished(true, Game.Components.SkirmishPhase.Finished,
                true, Game.Missions.Contracts.MissionOutcomeKind.None), "Skirmish terminal outcome stops control");
            Check(AriaPlayInputSystem.IsCurrentMatchFinished(false, default,
                true, Game.Missions.Contracts.MissionOutcomeKind.Victory), "campaign terminal outcome still stops control");
            s = new AriaPlaySessionComponent { Phase = AriaPlayPhase.Observing };
            o = new AriaPlayObservationComponent { Kind = AriaPlayObservationKind.WorldTarget, TargetId = -20002,
                Position = new Vector2(300, 300), Time = 1 };
            AriaPlayDecisionSystem.Step(o, ref s);
            o.Time = 2; o.Position = new Vector2(340, 300);
            AriaPlayDecisionSystem.Step(o, ref s);
            Check(s.GestureRequested == 1 && s.Target == o.Position, "moving hostile receives tap at current visible position");
            s=new AriaPlaySessionComponent {Phase=AriaPlayPhase.Observing};
            o=new AriaPlayObservationComponent {Kind=AriaPlayObservationKind.WorldTarget,TargetId=-30001,GoalId=1001,
                Position=new Vector2(100,200),DragEnd=new Vector2(350,450),Drag=1,Time=1};
            AriaPlayDecisionSystem.Step(o,ref s);o.Time=2;AriaPlayDecisionSystem.Step(o,ref s);
            Check(s.GestureRequested==1 && s.Drag==1 && s.Target==o.Position && s.DragEnd==o.DragEnd,"visible selection box becomes a normal drag gesture");
            s=new AriaPlaySessionComponent {Phase=AriaPlayPhase.Observing};
            o=new AriaPlayObservationComponent {Kind=AriaPlayObservationKind.Control,TargetId=1,GoalId=1001,Time=1};
            AriaPlayDecisionSystem.Step(o,ref s);
            for(int i=1;i<=9;i++)
            {
                s.Phase=AriaPlayPhase.Observing;
                o.TargetId=i%2+1;o.GoalId=1000+i%2;o.Time=1+i*30;
                AriaPlayDecisionSystem.Step(o,ref s);
            }
            Check(s.Phase==AriaPlayPhase.Blocked && s.GestureRequested==0,"changing targets and revisiting goals cannot reset the objective watchdog");
            s=new AriaPlaySessionComponent {Phase=AriaPlayPhase.Observing};
            o.GoalId=1001;o.Time=1;AriaPlayDecisionSystem.Step(o,ref s);
            o.GoalId=1002;o.Time=170;AriaPlayDecisionSystem.Step(o,ref s);
            s.Phase=AriaPlayPhase.Observing;o.Time=200;AriaPlayDecisionSystem.Step(o,ref s);
            Check(s.Phase!=AriaPlayPhase.Blocked,"advancing the public objective renews the watchdog");
            // The same off-screen catalog control may require several real drags.
            s = new AriaPlaySessionComponent { Phase = AriaPlayPhase.Observing };
            o = new AriaPlayObservationComponent { Kind = AriaPlayObservationKind.Control,
                TargetId = 77, GoalId = 503, Position = new Vector2(100, 200),
                Drag = 1, DragEnd = new Vector2(100, 500), ScrollTargetId = 9001,
                ScrollDistanceViewports = 1.3f, Time = 1 };
            AriaPlayDecisionSystem.Step(o, ref s);
            o.Time = 2; AriaPlayDecisionSystem.Step(o, ref s);
            Check(s.Phase == AriaPlayPhase.Touching && s.Attempts == 1, "first scroll gesture counts");
            s.Phase = AriaPlayPhase.Verifying; s.GestureRequested = 0; s.Actions = 1; s.DueAt = 2.5f;
            o.Time = 3; o.ScrollDistanceViewports = .8f; AriaPlayDecisionSystem.Step(o, ref s);
            Check(s.Attempts == 0 && s.ScrollCreditedActions == 1 && s.Phase == AriaPlayPhase.Aiming,
                "measured catalog movement renews the same target retry budget once");
            o.Time = 4; AriaPlayDecisionSystem.Step(o, ref s);
            Check(s.Phase == AriaPlayPhase.Touching && s.Attempts == 1,
                "next drag remains available after measured progress");
            s.Phase = AriaPlayPhase.Verifying; s.GestureRequested = 0; s.Actions = 2; s.DueAt = 4.5f;
            o.Time = 5; o.ScrollDistanceViewports = .3f; AriaPlayDecisionSystem.Step(o, ref s);
            Check(s.Attempts == 0 && s.ScrollCreditedActions == 2,
                "second productive drag renews the same target without an arbitrary three-drag cap");
            o.Time = 6; AriaPlayDecisionSystem.Step(o, ref s);
            s.Phase = AriaPlayPhase.Verifying; s.GestureRequested = 0; s.Actions = 3; s.DueAt = 6.5f;
            o.Time = 7; AriaPlayDecisionSystem.Step(o, ref s);
            o.Time = 8; AriaPlayDecisionSystem.Step(o, ref s);
            s.Phase = AriaPlayPhase.Verifying; s.GestureRequested = 0; s.Actions = 4; s.DueAt = 8.5f;
            o.Time = 9; AriaPlayDecisionSystem.Step(o, ref s);
            o.Time = 10; AriaPlayDecisionSystem.Step(o, ref s);
            s.Phase = AriaPlayPhase.Verifying; s.GestureRequested = 0; s.Actions = 5; s.DueAt = 10.5f;
            o.Time = 11; AriaPlayDecisionSystem.Step(o, ref s);
            Check(s.Phase == AriaPlayPhase.Blocked && s.Attempts == 3,
                "three completed drags without further movement remain bounded");
            s = new AriaPlaySessionComponent { Phase = AriaPlayPhase.Verifying,
                TargetId = 77, GoalId = 503, Attempts = 3, Actions = 5,
                ScrollTargetId = 9001, ScrollCreditedActions = 5, BestScrollDistanceViewports = .3f,
                ObjectiveWatchdogInitialized = 1, VisitedGoalMask = 1UL << (503 & 63),
                LastObjectiveProgressAt = 1, LastProgressAt = 1 };
            o.Time = 12; o.ScrollTargetId = 9002; o.ScrollDistanceViewports = 1.1f;
            AriaPlayDecisionSystem.Step(o, ref s);
            Check(s.Phase != AriaPlayPhase.Blocked && s.Attempts == 0 && s.ScrollTargetId == 9002 &&
                s.LastObjectiveProgressAt == 1, "new catalog item gets its own budget without forging objective progress");
            s.Phase = AriaPlayPhase.Verifying; s.Attempts = 3; s.Actions = 6;
            s.ScrollCreditedActions = 6; s.BestScrollDistanceViewports = 1.1f;
            o.Time = 13; o.ScrollDistanceViewports = .3f;
            AriaPlayDecisionSystem.Step(o, ref s);
            Check(s.Phase == AriaPlayPhase.Blocked,
                "scroll movement without a new completed action cannot erase no-progress retries");
            s.Phase = AriaPlayPhase.Verifying; s.Actions = 7; s.Attempts = 3;
            s.ScrollCreditedActions = 6; s.BestScrollDistanceViewports = 1.1f;
            o.Time = 14; o.ScrollDistanceViewports = 1.07f;
            AriaPlayDecisionSystem.Step(o, ref s);
            Check(s.Phase == AriaPlayPhase.Blocked,
                "sub-threshold catalog motion does not clear a no-progress block");
            s.Phase = AriaPlayPhase.Verifying; s.Actions = 8; s.Attempts = 3;
            o.ScrollDistanceViewports = .2f;
            o.Time = 182; AriaPlayDecisionSystem.Step(o, ref s);
            Check(s.Phase == AriaPlayPhase.Blocked && s.LastObjectiveProgressAt == 1,
                "scroll progress cannot extend the objective watchdog");
            return "[AriaPlayDecisionValidation] result=Passed cases=29";
        }
        private static void Check(bool condition, string reason)
        { if (!condition) throw new InvalidOperationException(reason); }
    }
}
#endif
