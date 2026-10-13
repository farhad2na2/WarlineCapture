#if UNITY_EDITOR
using System;
using Game.UI.Contracts;
using Game.UI.Shell.Contracts.Ecs;
using Game.UI.Shell.Ecs;
using UnityEngine;

namespace Game.Editor
{
    public static class SquadScrollCompatibilityValidation
    {
        public static void RunFocusedValidation()
        {
            AriaSkirmishPlanValidation.RunFocusedValidation();
            int checks = 0;
            void Check(bool value, string reason)
            { if (!value) throw new InvalidOperationException(reason); checks++; }

            var observation = new AriaPlayObservationComponent
            { Kind = AriaPlayObservationKind.Control, TargetId = 101, GoalId = 1,
                Drag = 1, ScrollTargetId = 401, ScrollDistanceViewports = 6, Time = 10,
                Position = new Vector2(500, 150), DragEnd = new Vector2(100, 150) };
            var session = new AriaPlaySessionComponent
            { Phase = AriaPlayPhase.Verifying, TargetId = 101, GoalId = 1,
                Attempts = 3, Actions = 4, ScrollTargetId = 401, ScrollCreditedActions = 3,
                BestScrollDistanceViewports = 6.5f, LastProgressAt = 9,
                LastObjectiveProgressAt = 9, ObjectiveWatchdogInitialized = 1, VisitedGoalMask = 2 };
            for (int i = 0; i < 8; i++)
            {
                AriaPlayDecisionSystem.Step(observation, ref session);
                Check(session.Phase != AriaPlayPhase.Blocked && session.Attempts < 3,
                    "Completed scrolling progress must allow a long roster, not block after three swipes.");
                session.Actions++; session.Phase = AriaPlayPhase.Verifying; session.Attempts = 3;
                observation.Time += 1; observation.ScrollDistanceViewports -= .5f;
            }
            session.Phase = AriaPlayPhase.Verifying; session.Attempts = 3;
            session.BestScrollDistanceViewports = observation.ScrollDistanceViewports;
            AriaPlayDecisionSystem.Step(observation, ref session);
            Check(session.Phase == AriaPlayPhase.Blocked,
                "Completed input without actual roster movement must remain bounded.");

            var view = new AriaSkirmishObservation
            { Active = true, ExpandedSession = true, Time = 100, Infantry = 16,
                PlayerHealth = 800, EnemyHealth = 800, EnemyDesignatedAlive = true,
                CombinedArmsField = true, OwnStructureAttackUnits = 8, SelectionVisible = true,
                ExpandedPageCount = 3, ExpandedTotalGroups = 12, ExpandedPageIndex = 1,
                ExpandedAssaultMask = 31, ExpandedStructureMask = 4, ExpandedSelectedMask = 1,
                ExpandedSelectedGroups = 1,
                SelectedSquadToToggle = new AriaTouchTarget { Available = true, Id = 99, Drag = true,
                    Position = new Vector2(100, 150), DragEnd = new Vector2(500, 150),
                    ScrollTargetId = 407, ScrollDistanceViewports = 2 },
                NextSquadPage = new AriaTouchTarget { Available = true, Id = 98, Drag = true },
                PreviousSquadPage = new AriaTouchTarget { Available = true, Id = 97, Drag = true },
                Squad0 = new AriaTouchTarget { Available = true, Id = 40 },
                Squad1 = new AriaTouchTarget { Available = true, Id = 41 },
                Squad2 = new AriaTouchTarget { Available = true, Id = 42 },
                Squad3 = new AriaTouchTarget { Available = true, Id = 43 },
                Squad4 = new AriaTouchTarget { Available = true, Id = 44 } };
            var plan = new AriaSkirmishPlanComponent { AssaultStarted = 1, PagedToAssault = 1 };
            var touch = new AriaPlaySessionComponent { Phase = AriaPlayPhase.Observing };
            var output = new AriaPlayObservationComponent();
            AriaSkirmishPlanSystem.Step(view, ref plan, ref touch, ref output);
            Check(output.TargetId == 99 && output.Drag == 1 && output.ScrollTargetId == 407 &&
                plan.AssaultSelecting == 2 && plan.SelectionResetPage == 1,
                "ARIA must scroll to the selected card, remembering the intended assault window.");
            view.SelectedSquadToToggle.Drag = false;
            AriaSkirmishPlanSystem.Step(view, ref plan, ref touch, ref output);
            Check(output.TargetId == 99 && output.Drag == 0,
                "ARIA must deselect using a presented card tap, without a Clear control.");
            view.ExpandedSelectedGroups = view.ExpandedSelectedMask = 0;
            view.ExpandedPageIndex = 0;
            AriaSkirmishPlanSystem.Step(view, ref plan, ref touch, ref output);
            Check(output.TargetId == 98 && output.Drag == 1 && plan.AssaultSelecting == 2,
                "Reset must restore its original logical window through a physical swipe.");
            view.ExpandedPageIndex = 1;
            AriaSkirmishPlanSystem.Step(view, ref plan, ref touch, ref output);
            Check(output.TargetId == 40 && plan.AssaultSelecting == 1,
                "Assault selection must resume only after observing the restored window.");
            view.PresentedHomeThreat = true;
            view.ExpandedPageIndex = 2;
            view.ExpandedSelectedGroups = 1;
            plan.HomeDefenseStage = 7;
            plan.HomeDefensePage = 1;
            AriaSkirmishPlanSystem.Step(view, ref plan, ref touch, ref output);
            Check(plan.HomeDefenseStage == 2 && plan.HomeDefensePage == 2,
                "Defense must remember each newly observed roster window before clearing selection.");
            AriaSkirmishPlanSystem.Step(view, ref plan, ref touch, ref output);
            Check(output.TargetId == 99,
                "Defense reset must use the selected card target, including scrolling preparation.");
            view.ExpandedSelectedGroups = view.ExpandedSelectedMask = 0;
            view.ExpandedPageIndex = 0;
            AriaSkirmishPlanSystem.Step(view, ref plan, ref touch, ref output);
            Check(output.TargetId == 98 && output.Drag == 1 && plan.HomeDefenseStage == 2,
                "Defense must restore the later window after deselecting an offscreen card.");
            view.ExpandedPageIndex = 2;
            AriaSkirmishPlanSystem.Step(view, ref plan, ref touch, ref output);
            Check(output.TargetId == 40 && plan.HomeDefenseStage == 3,
                "Defense must select from its restored window instead of restarting page zero.");
            Debug.Log("[SquadScrollCompatibility] result=Passed checks=" + checks +
                " nativeInputOrMissionCertified=0");
        }
    }
}
#endif
