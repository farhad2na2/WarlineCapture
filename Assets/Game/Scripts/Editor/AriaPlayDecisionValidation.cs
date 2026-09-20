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
            return "[AriaPlayDecisionValidation] result=Passed cases=14";
        }
        private static void Check(bool condition, string reason)
        { if (!condition) throw new InvalidOperationException(reason); }
    }
}
#endif
