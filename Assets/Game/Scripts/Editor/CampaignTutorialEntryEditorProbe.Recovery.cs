using System;
using System.Linq;
using Game.Components;
using Game.Composition;
using Game.Runtime;
using Game.Tactical.Contracts;
using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;
using Game.UI.Runtime;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Editor
{
    public static partial class CampaignTutorialEntryEditorProbe
    {
        private const string RecoveryJourney = "Warline.Readiness.RecoveryJourney";
        private static int canceledEntry = -1, pausedEntry = -1;
        private static bool recoveryPauseOpen;
        private static int rotatedEntry = -1, selectionClearedEntry = -1, wrongTargetEntry = -1;
        private static bool wrongTargetPending;

        private static void VerifyRecoveryCoverage()
        {
            if (pausedEntry != entry || recoveryPauseOpen)
                throw new InvalidOperationException("Recovery did not exercise pause/resume: " + CapturePrefix);
            if (entry % 5 is 1 or 2 && (canceledEntry != entry || rotatedEntry != entry))
                throw new InvalidOperationException("Recovery did not rotate, cancel and reopen placement: " + CapturePrefix);
            if (entry % 5 == 3 && selectionClearedEntry != entry)
                throw new InvalidOperationException("Recovery did not clear selection before helicopter boarding: " + CapturePrefix);
            if (entry % 5 is 0 or 4 && (wrongTargetEntry != entry || wrongTargetPending))
                throw new InvalidOperationException("Recovery did not reject a wrong friendly attack target: " + CapturePrefix);
            Debug.Log("[ReadinessRecovery] coverage=Passed entry=" + CapturePrefix);
        }

        public static void RunM02RecoveryJourney()
        {
            RunJourney(1);
            SessionState.SetBool(RecoveryJourney, true);
        }

        private static bool TickRecoveryJourney(EntityManager em, Entity root, double now)
        {
            if (!SessionState.GetBool(RecoveryJourney, false)) return false;
            if (!recoveryPauseOpen && UiShellRuntimeGateway.TryReadMissionHudRestrictions(out var restrictions) &&
                restrictions.CinematicInteractionLocked) return false;
            if (recoveryPauseOpen)
            {
                var pause = UnityEngine.Object.FindAnyObjectByType<PauseOptionsV3PopupView>();
                if (pause == null || !Ready(pause.ResumeButton)) return true;
                ClickVisible(pause.ResumeButton); recoveryPauseOpen = false;
                RecordAction("recovery:resume", now); return true;
            }
            var placement = UnityEngine.Object.FindAnyObjectByType<BuildPlacementConfirmationBarView>();
            if (rotatedEntry != entry && placement != null && placement.HasPendingPlacement && Ready(placement.RotateButton))
            {
                ClickVisible(placement.RotateButton); rotatedEntry = entry;
                RecordAction("recovery:rotate-preview-before-cancel", now); return true;
            }
            if (canceledEntry != entry && placement != null && placement.HasPendingPlacement && Ready(placement.CancelButton))
            {
                if (CaptureBeforeAction("recovery-before-cancel", now)) return true;
                ClickVisible(placement.CancelButton); canceledEntry = entry;
                RecordAction("recovery:cancel-placement", now); return true;
            }
            // The M3 threat report intentionally owns input while open. Finish
            // that player-facing action before testing the underlying Pause HUD.
            var warning = UnityEngine.Object.FindAnyObjectByType<ThreatAlertV3PopupView>();
            bool warningOpen = warning != null && Ready(warning.JumpToThreatButton);
            if (pausedEntry != entry && !warningOpen && UiShellRuntimeGateway.TryReadMatchHudAssistantPanel(out var panel) && panel.TutorialStep >= 2)
            {
                var pause = UnityEngine.Object.FindObjectsByType<Button>(FindObjectsSortMode.None)
                    .FirstOrDefault(b => b.name == "PauseButton" && Ready(b));
                if (pause == null) throw new System.InvalidOperationException("No usable Pause control during recovery.");
                if (CaptureBeforeAction("recovery-before-pause", now)) return true;
                ClickVisible(pause); pausedEntry = entry; recoveryPauseOpen = true;
                RecordAction("recovery:pause", now); return true;
            }
            var feedback = UnityEngine.Object.FindAnyObjectByType<BattleHudRuntimeFeedbackView>();
            if (wrongTargetPending)
            {
                var rejectionText = feedback?.FeedbackText;
                if (feedback == null || !feedback.HasLastCommandResult || feedback.LastCommandResult.Accepted ||
                    rejectionText == null || !rejectionText.gameObject.activeInHierarchy || string.IsNullOrWhiteSpace(rejectionText.text))
                    throw new InvalidOperationException("Wrong friendly attack target did not produce visible feedback: has=" + feedback?.HasLastCommandResult +
                        " accepted=" + feedback?.LastCommandResult.Accepted + " reason=" + feedback?.LastCommandResult.ReasonCode + " text=" + rejectionText?.text);
                CaptureJourney("recovery-wrong-target-feedback");
                Debug.Log("[ReadinessRecovery] rejected=" + feedback.LastCommandResult.ReasonCode + " message=" + rejectionText.text);
                wrongTargetPending = false;
            }
            var match = UnityEngine.Object.FindAnyObjectByType<MatchSceneView>();
            var input = Field<RtsSelectionInputCompositionSystemHelper>(match?.MatchBootstrap?.SelectionUiCommand, "_inputSystem");
            if (input == null || !UiShellRuntimeGateway.TryReadMatchHudAssistantPanel(out var activePanel)) return false;
            if (entry % 5 == 3 && selectionClearedEntry != entry && activePanel.TutorialStep == 9)
            {
                if (!input.QueueCommandIntentRequest(RtsSelectionCommandIntentKind.DeselectAll, Time.frameCount)) return false;
                selectionClearedEntry = entry;
                RecordAction("recovery:clear-selection-before-helicopter-boarding", now); return true;
            }
            if (entry % 5 is 0 or 4 && wrongTargetEntry != entry && activePanel.TutorialStep >= 2)
            {
                using var selected = em.CreateEntityQuery(typeof(SelectedUnitTag), typeof(LocalTransform), typeof(Faction));
                using var entities = selected.ToEntityArray(Allocator.Temp);
                foreach (var unit in entities)
                {
                    if (em.GetComponentData<Faction>(unit).Id != 1) continue;
                    Vector3 screen = match.MatchBootstrap.WorldCamera.WorldToScreenPoint(em.GetComponentData<LocalTransform>(unit).Position);
                    if (screen.z <= 0 || !new Rect(Screen.width * .24f, Screen.height * .24f, Screen.width * .5f, Screen.height * .55f).Contains(screen)) continue;
                    if (!UiShellRuntimeGateway.TryReadMatchHudCommandState(out var commands)) return false;
                    if (commands.ActiveCommandMode != TacticalCommandMode.Attack)
                    {
                        var attack = UnityEngine.Object.FindObjectsByType<Button>(FindObjectsSortMode.None).FirstOrDefault(b => b.name == "AttackCommand" && Ready(b));
                        if (attack == null) return false;
                        ClickVisible(attack); RecordAction("recovery:attack-mode-for-wrong-target", now); return true;
                    }
                    if (!input.QueueAttackCommandRequest(screen, true, Time.frameCount)) return false;
                    wrongTargetEntry = entry; wrongTargetPending = true;
                    RecordAction("recovery:try-attacking-friendly", now); return true;
                }
            }
            return false;
        }
    }
}
