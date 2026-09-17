using System;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;
using Game.Components;
using Game.Composition;
using Game.Missions.Contracts;
using Game.Runtime;
using Game.Tactical.Contracts;
using Game.UI.Runtime;
using Unity.Entities;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Game.Editor
{
    public static partial class CampaignTutorialEntryEditorProbe
    {
        private const string Journey = "Warline.Readiness.PlayerJourney";
        private const string FeedbackRegression = "Warline.Readiness.FeedbackRegression";
        private const string SharedCueRegression = "Warline.Readiness.SharedCueRegression";
        private static int journeyStep;
        private static string lastJourneyAction, capturedJourneyAction;
        private static double journeyStepAt, journeyActionAt;
        private static bool insideGameView;
        private static int lastStableStep = -1;
        private static int trainingWaitEntry = -1;
        private static int gatePoseCheckedEntry = -1;
        private static double missingDefenseInstructionSince, missingDefenseStatusSince;
        private static int defenseStatusCapture = -1;
        private static void TickGameView()
        {
            if (insideGameView || stage != 2 || !SessionState.GetBool(Journey, false) || !EditorApplication.isPlaying) return;
            var aria = UnityEngine.Object.FindAnyObjectByType<AriaTutorialBriefingView>(FindObjectsInactive.Include);
            var canvas = aria?.GetComponentInParent<Canvas>()?.rootCanvas;
            // EditorApplication.update can expose the Editor window's Screen dimensions.
            // Pointer replay must run with the rendered Game view's coordinate system.
            if (canvas == null || Mathf.Abs(canvas.pixelRect.width - Screen.width) > 1) return;
            if (entry % 5 == 1 && trainingWaitEntry == entry && failure == null)
            {
                var match = UnityEngine.Object.FindAnyObjectByType<MatchSceneView>();
                var assistant = Field<object>(match?.MatchBootstrap?.MainMenu, "_matchHudAssistantUiSystem");
                var highlight = Field<object>(assistant, "_highlightPresentationSystem");
                var target = Field<RectTransform>(highlight, "_directTutorialTarget");
                if (target?.name == "BuildCommand" && GameObject.Find("AriaAssistantTargetIndicatorRuntime") != null)
                {
                    failure = "M2 flashed Build guidance after recruitment was accepted.";
                    CaptureJourney("repeated-build-failure");
                }
            }
            insideGameView = true;
            try { Tick(); } finally { insideGameView = false; }
        }
        public static void RunM02M03FeedbackJourney()
        {
            RunJourney(1);
            SessionState.SetBool(FeedbackRegression, true);
        }
        public static void RunM01Journey() => RunJourney(0);
        public static void RunM02Journey() => RunJourney(1);
        public static void RunM03Journey() => RunJourney(2);
        public static void RunM04Journey() => RunJourney(3);
        public static void RunM05Journey() => RunJourney(4);
        public static void RunSharedCueRegression(int firstMission = 0)
        {
            RunJourney(firstMission);
            SessionState.SetBool(SharedCueRegression, true);
        }

        private static void RunJourney(int mission)
        {
            Run();
            SessionState.SetBool(CampaignChain, false);
            SessionState.SetBool(FeedbackRegression, false);
            SessionState.SetBool(RecoveryJourney, false);
            SessionState.SetBool(SharedCueRegression, false);
            SessionState.SetBool(Journey, true);
            entry = mission; journeyStep = 0; lastJourneyAction = null;
        }

        // Editor replay of visible controls and screen-position input. Does not alter
        // health, positions, tutorial facts, time scale or mission outcomes.
        private static void TickJourney(EntityManager em, Entity root, double now)
        {
            var runtime = em.GetComponentData<CampaignMissionRuntimeComponent>(root);
            if (entry % 5 == 4 && gatePoseCheckedEntry != entry && em.HasComponent<CampaignMissionBreachState>(root) &&
                em.GetComponentData<CampaignMissionBreachState>(root).Ready != 0)
            {
                var test = AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetType("Game.Tests.Editor.M05EnemyCompoundTests")).First(t => t != null);
                test.GetMethod("ValidateLiveClosedGate").Invoke(null, null);
                gatePoseCheckedEntry = entry;
                CaptureJourney("gate-wall-opening");
            }
            if (runtime.Outcome == MissionOutcomeKind.Defeat)
                throw new InvalidOperationException("Player journey ended in defeat, mission=" + Missions[entry % 5]);
            if (runtime.Outcome == MissionOutcomeKind.Victory)
            {
                if (entry % 5 == 1 && trainingWaitEntry != entry)
                    throw new InvalidOperationException("M2 reached victory without showing its recruitment wait.");
                var view = UnityEngine.Object.FindAnyObjectByType<MissionResultPopupView>();
                if (view == null || !UiShellRuntimeGateway.TryReadMissionResult(out var result)) return;
                if (result.SettlementFailed) throw new InvalidOperationException("Victory reward settlement failed.");
                var primary = Field<Button>(view, "primaryButton");
                if (!Ready(primary)) return;
                if (CaptureBeforeAction("victory", now)) return;
                if (SessionState.GetBool(CampaignChain, false)) CheckCampaignSettlement(em, root, runtime);
                ClickVisible(primary);
                Debug.Log("[CampaignPlayerJourney] victory=" + Missions[entry % 5] + " locale=" + Game.Configs.GameLocalization.CurrentLocaleCode);
                Next(4); return;
            }
            if (TickRecoveryJourney(em, root, now)) return;
            if (UiShellRuntimeGateway.TryReadMissionHudRestrictions(out var restrictions) && restrictions.CinematicInteractionLocked) return;
            var aria = UnityEngine.Object.FindAnyObjectByType<AriaTutorialBriefingView>(FindObjectsInactive.Include);
            bool hasPanel = UiShellRuntimeGateway.TryReadMatchHudAssistantPanel(out var panel);
            if (entry % 5 == 2 && runtime.Phase == MissionPhaseKind.Engage && journeyStep >= 9)
            {
                bool missing = aria == null || !hasPanel || !panel.HasRecommendation || panel.TutorialStep == 0 ||
                    !aria.IsPresentationVisible || string.IsNullOrWhiteSpace(aria.CurrentInstructionBody);
                if (missing && missingDefenseInstructionSince == 0) missingDefenseInstructionSince = now;
                if (missing && now - missingDefenseInstructionSince > 1.5)
                    throw new InvalidOperationException("M3 lost its defense instruction between convoy elements.");
                if (!missing) missingDefenseInstructionSince = 0;
            }
            else missingDefenseInstructionSince = 0;
            if (entry % 5 == 2 && runtime.Phase == MissionPhaseKind.Engage &&
                em.HasComponent<CampaignMissionDefenseStateComponent>(root) &&
                (em.GetComponentData<CampaignMissionDefenseStateComponent>(root).AcknowledgedGuidanceMask & 0x1FFu) == 0x1FFu)
            {
                var status = GameObject.Find("ThreatJumpPanel");
                var text = status != null ? status.GetComponentInChildren<TMPro.TMP_Text>() : null;
                bool missing = text == null || string.IsNullOrWhiteSpace(text.text);
                if (missing && missingDefenseStatusSince == 0) missingDefenseStatusSince = now;
                if (missing && now - missingDefenseStatusSince > 1.5)
                    throw new InvalidOperationException("M3 defense countdown/status disappeared during the wait.");
                if (!missing)
                {
                    missingDefenseStatusSince = 0;
                    int seconds = em.GetComponentData<CampaignMissionAttemptFactsComponent>(root).ElapsedMilliseconds / 1000;
                    int capture = entry * 1000 + seconds / 20;
                    if (capture != defenseStatusCapture)
                    {
                        defenseStatusCapture = capture;
                        CaptureJourney("defense-status-" + seconds);
                        Debug.Log("[CampaignPlayerJourney] defense-status elapsed=" + seconds + " text=" + text.text);
                    }
                }
            }
            else missingDefenseStatusSince = 0;
            if (aria == null || !hasPanel || !panel.HasRecommendation) return;
            if (aria.DoItButton.gameObject.activeInHierarchy)
                throw new InvalidOperationException("Removed Do It control reappeared during the player journey.");
            if (entry % 5 == 1 && panel.TutorialStep == 6 &&
                aria.CurrentInstructionBody == Game.Configs.GameLocalization.Get("tutorial.m02.training_wait.body"))
            {
                if (UnityEngine.Object.FindAnyObjectByType<BuildDrawerView>()?.IsOpen == true)
                    throw new InvalidOperationException("Recruitment accepted but its Build drawer did not close.");
                if (trainingWaitEntry != entry)
                {
                    trainingWaitEntry = entry;
                    CaptureJourney("training-wait");
                    Debug.Log("[CampaignPlayerJourney] training-wait locale=" + Game.Configs.GameLocalization.CurrentLocaleCode +
                        " drawer=closed body=" + aria.CurrentInstructionBody);
                }
            }
            if (journeyStep != panel.TutorialStep)
            {
                journeyStep = panel.TutorialStep; journeyStepAt = now; lastJourneyAction = capturedJourneyAction = null;
                CaptureJourney("lesson-" + journeyStep);
                Debug.Log("[CampaignPlayerJourney] mission=" + Missions[entry % 5] + " locale=" + Game.Configs.GameLocalization.CurrentLocaleCode + " lesson=" + journeyStep + " body=" + aria.CurrentInstructionBody);
                waitUntil = now + 1; return;
            }
            if (now - journeyStepAt > 3 && lastStableStep != entry * 100 + journeyStep)
            {
                lastStableStep = entry * 100 + journeyStep;
                CaptureJourney("lesson-" + journeyStep + "-settled");
                Debug.Log("[CampaignPlayerJourney] settled lesson=" + journeyStep + " phase=" + runtime.Phase + " outcome=" + runtime.Outcome +
                    " produced=" + em.GetComponentData<CampaignMissionAttemptFactsComponent>(root).RequiredUnitProducedCount +
                    " guidance=" + em.GetComponentData<CampaignMissionGuidanceProjectionComponent>(root).Prompt +
                    " body=" + aria.CurrentInstructionBody);
            }
            if (now - journeyStepAt > 160)
                throw new TimeoutException("Player journey stalled: " + Missions[entry % 5] + " lesson=" + journeyStep + " last=" + lastJourneyAction + " body=" + aria.CurrentInstructionBody);
            var match = UnityEngine.Object.FindAnyObjectByType<MatchSceneView>();
            if (match?.MatchBootstrap?.MainMenu == null) return;
            var assistant = Field<object>(match.MatchBootstrap.MainMenu, "_matchHudAssistantUiSystem");
            var highlight = Field<object>(assistant, "_highlightPresentationSystem");
            var targetRect = Field<RectTransform>(highlight, "_directTutorialTarget");
            var cue = GameObject.Find("AriaAssistantTargetIndicatorRuntime");
            Button button = cue != null ? targetRect?.GetComponent<Button>() : null;
            if (entry % 5 == 1 && trainingWaitEntry == entry && button?.name == "BuildCommand")
                throw new InvalidOperationException("M2 repeated Build guidance after recruitment was accepted.");
            if (button == null && Ready(aria.ShowMeButton)) button = aria.ShowMeButton;
            if (button != null)
            {
                string action = journeyStep + ":" + button.name;
                if (lastJourneyAction == action && now - journeyActionAt < 4) return;
                if (CaptureBeforeAction("lesson-" + journeyStep + "-" + button.name, now)) return;
                if ((button.name == "BoardSector" || button.name == "BuildCommand") && cue.GetComponentInChildren<TutorialChevronGraphic>() == null)
                    throw new InvalidOperationException(button.name + " has a frame but no visible directional arrow.");
                if (entry % 5 == 1 && journeyStep == 6)
                {
                    var catalog = Field<BuildDrawerCatalogRuntimeView>(highlight, "_buildDrawerCatalogRuntimeView");
                    var pending = Field<System.Collections.ICollection>(catalog, "_pendingProductions");
                    Debug.Log("[CampaignPlayerJourney] production-before=" + button.name + " catalog=" + (catalog != null) +
                        " active=" + (catalog != null && catalog.isActiveAndEnabled) + " queue=" + pending?.Count +
                        " body=" + aria.CurrentInstructionBody);
                }
                ClickVisible(button); RecordAction(action, now); return;
            }
            var ring = GameObject.Find("AriaAssistantPreviewHighlightRuntime");
            if (ring == null || !UiShellRuntimeGateway.TryReadMissionTutorialTarget(out var target) || target.NeedsSelection || target.Moving || target.ExecutingAttack || target.BattleAction == Game.UI.Contracts.UiTutorialBattleAction.Watch) return;
            if (!UiShellRuntimeGateway.TryReadMatchHudCommandState(out var commands)) return;
            string worldAction = journeyStep + ":" + commands.ActiveCommandMode + ":" + target.Destination;
            if (lastJourneyAction == worldAction && now - journeyActionAt < 8) return;
            var camera = match.MatchBootstrap.WorldCamera;
            Vector3 screen = camera.WorldToScreenPoint(target.Destination);
            if (screen.z <= 0 || !new Rect(0, 0, Screen.width, Screen.height).Contains(screen))
                throw new InvalidOperationException("Required world target is offscreen without Show Me.");
            if (CaptureBeforeAction("lesson-" + journeyStep + "-world-" + commands.ActiveCommandMode, now)) return;
            var input = Field<RtsSelectionInputCompositionSystemHelper>(match.MatchBootstrap.SelectionUiCommand, "_inputSystem");
            bool queued = commands.ActiveCommandMode switch
            {
                TacticalCommandMode.Attack => input.QueueAttackCommandRequest(screen, true, Time.frameCount),
                TacticalCommandMode.Move => input.QueueMoveCommandRequest(screen, Time.frameCount),
                TacticalCommandMode.Board => input.QueueBoardTransportCommandRequest(screen, Time.frameCount),
                _ => false
            };
            if (!queued) return; // A named observation/hold step has no player gesture.
            CaptureJourney("lesson-" + journeyStep + "-world"); RecordAction(worldAction, now);
        }

        private static void RecordAction(string action, double now)
        {
            Debug.Log("[CampaignPlayerJourney] action=" + action);
            lastJourneyAction = action; journeyActionAt = now; waitUntil = now + 1.5;
        }
        private static T Field<T>(object owner, string name) where T : class => owner?.GetType()
            .GetField(name, BindingFlags.NonPublic | BindingFlags.Instance)?.GetValue(owner) as T;
        private static bool Ready(Button button) => button != null && button.IsActive() && button.IsInteractable();
        private static void ClickVisible(Button button)
        {
            if (!Ready(button)) throw new InvalidOperationException("Guided button is not usable: " + button?.name);
            if (button.name == "BuildCommand" && button.targetGraphic?.material?.shader?.name == "Warline/UI/Disabled Grayscale")
            {
                string state = string.Join(";", button.GetComponentsInChildren<Component>(true)
                    .Where(c => c != null && c.GetType().Name.Contains("Disabled"))
                    .Select(c => c.name + ":" + c.GetType().Name + ":" + string.Join(",", c.GetType().GetFields().Select(f => f.Name + "=" + f.GetValue(c)))));
                throw new InvalidOperationException("Guided Build is clickable but still displays the disabled material. " + state);
            }
            var labels = button.GetComponentsInChildren<TMPro.TMP_Text>();
            if (Array.Exists(labels, text => !string.IsNullOrWhiteSpace(text.text)) &&
                !Array.Exists(labels, text => !string.IsNullOrWhiteSpace(text.text) && text.color.a > .01f && text.canvasRenderer.GetAlpha() > .01f))
                throw new InvalidOperationException("Guided button has invisible labels: " + button.name);
            var rect = (RectTransform)button.transform;
            var canvas = button.GetComponentInParent<Canvas>()?.rootCanvas;
            var camera = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay ? canvas.worldCamera : null;
            Vector2 screen = RectTransformUtility.WorldToScreenPoint(camera, rect.TransformPoint(rect.rect.center));
            var pointer = new PointerEventData(EventSystem.current) { position = screen, button = PointerEventData.InputButton.Left };
            var hits = new List<RaycastResult>(); EventSystem.current.RaycastAll(pointer, hits);
            // A radial sector's rectangular center is the portrait hole. Find a
            // real raycastable point on that same visible button, never bypass it.
            if (hits.Count == 0 || hits[0].gameObject.GetComponentInParent<Button>() != button)
            {
                bool found = false;
                for (int y = 1; y < 10 && !found; y++)
                    for (int x = 1; x < 10 && !found; x++)
                    {
                        Vector2 local = new(Mathf.Lerp(rect.rect.xMin, rect.rect.xMax, x / 10f),
                            Mathf.Lerp(rect.rect.yMin, rect.rect.yMax, y / 10f));
                        pointer.position = RectTransformUtility.WorldToScreenPoint(camera, rect.TransformPoint(local));
                        hits.Clear(); EventSystem.current.RaycastAll(pointer, hits);
                        found = hits.Count > 0 && hits[0].gameObject.GetComponentInParent<Button>() == button;
                    }
                if (!found) throw new InvalidOperationException("Visible guide points to an occluded button: " + button.name);
            }
            ExecuteEvents.Execute(button.gameObject, pointer, ExecuteEvents.pointerClickHandler);
        }
        private static void CaptureJourney(string state) => ScreenCapture.CaptureScreenshot($"{Output}/{CapturePrefix}-journey-{state}.png");
        private static bool CaptureBeforeAction(string action, double now)
        {
            if (capturedJourneyAction == action) return false;
            capturedJourneyAction = action; CaptureJourney(action); waitUntil = now + .6; return true;
        }
    }
}
