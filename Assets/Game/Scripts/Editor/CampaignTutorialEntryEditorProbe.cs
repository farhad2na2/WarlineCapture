using System;
using System.IO;
using System.Reflection;
using Game.Components;
using Game.Configs;
using Game.Missions.Contracts;
using Game.Runtime;
using Game.UI.Contracts;
using Game.UI.Runtime;
using Unity.Entities;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Editor
{
    // Real campaign-menu entries using an isolated completed profile; no objective facts are changed.
    [InitializeOnLoad]
    public static partial class CampaignTutorialEntryEditorProbe
    {
        private const string Active = "Warline.CampaignTutorialEntry";
        private const string StoryReplay = "Warline.CampaignTutorialEntry.StoryReplay";
        private const string OutputKey = "Warline.CampaignTutorialEntry.Output";
        private static string Output => SessionState.GetString(OutputKey, "/private/tmp/warline-campaign-tutorial-entry");
        private static readonly string[] Missions = {
            "saga.ch01.m01.first_contact", "saga.ch01.m02.establish_base", "saga.ch01.m03.radar_warning",
            "saga.ch01.m04.airlift", "saga.ch01.m05.breach_assault" };
        private static int entry, stage, firstStep, observedStep;
        private static double observedAt;
        private static double deadline, waitUntil, lastSkip;
        private static bool prepared, finished;
        private static string failure;
        static CampaignTutorialEntryEditorProbe()
        {
            if (SessionState.GetBool(Active, false)) Attach();
        }
        public static void Run()
        {
            SessionState.SetString(OutputKey, "/private/tmp/warline-readiness-" + DateTime.UtcNow.ToString("yyyyMMdd-HHmmss"));
            Directory.CreateDirectory(Output);
            Debug.Log("[CampaignTutorialEntry] evidence=" + Output);
            SessionState.SetBool(Active, true);
            SessionState.SetBool(StoryReplay, false);
            SessionState.SetBool(Journey, false);
            entry = stage = 0; prepared = finished = false; failure = null;
            MainMenuV3PrefabBuilder.SetGameViewResolution(1920, 1080);
            EditorSceneManager.OpenScene(M02EstablishBaseNarrativeConfigBuilder.MenuScenePath, OpenSceneMode.Single);
            AssetDatabase.DisallowAutoRefresh(); Attach(); EditorApplication.EnterPlaymode();
        }
        private static void Attach()
        {
            EditorApplication.update -= Tick; EditorApplication.update += Tick;
            Application.logMessageReceived -= Observe; Application.logMessageReceived += Observe;
            Canvas.willRenderCanvases -= TickGameView; Canvas.willRenderCanvases += TickGameView;
        }
        private static void Tick()
        {
            if (finished || !EditorApplication.isPlaying) return;
            try
            {
                if (failure != null) throw new InvalidOperationException(failure);
                if (SessionState.GetBool(RecoveryJourney, false) || SessionState.GetBool(StoryReplay, false)) AuditReadinessPresentation();
                if (SessionState.GetBool(StoryReplay, false)) AuditM01StorySetup();
                double now = EditorApplication.timeSinceStartup;
                if (deadline == 0) deadline = now + 180;
                if (now > deadline) throw new TimeoutException($"entry={entry} stage={stage}");
                if (now < waitUntil) return;
                var world = World.DefaultGameObjectInjectionWorld;
                if (world == null || !world.IsCreated) return;
                var em = world.EntityManager;
                using var query = em.CreateEntityQuery(typeof(CampaignMissionRootComponent), typeof(CampaignMissionRuntimeComponent));
                if (query.CalculateEntityCount() != 1) return;
                var root = query.GetSingletonEntity();
                if (!prepared)
                {
                    if (!em.HasComponent<CampaignMissionProgressStoreReferenceComponent>(root)) return;
                    var save = new SaveService(new JsonSaveRepository(ProfileDirectory));
                    var store = new CampaignMissionProgressStore(save);
                    if (SessionState.GetBool(CampaignChain, false)) store.EnsureAvailable(Missions[0]);
                    else foreach (string mission in Missions)
                    { store.EnsureAvailable(mission); if (!SessionState.GetBool(Journey, false)) store.Settle(mission, "completed-profile", 1, true, 3, 120000, string.Empty); }
                    em.GetComponentObject<CampaignMissionProgressStoreReferenceComponent>(root).Store = store;
                    using var settings = em.CreateEntityQuery(typeof(AssistantSettingsComponent));
                    if (settings.CalculateEntityCount() == 1)
                    {
                        var value = settings.GetSingleton<AssistantSettingsComponent>(); value.GuidanceLevel = AssistantGuidanceLevel.Off;
                        em.SetComponentData(settings.GetSingletonEntity(), value);
                    }
                    prepared = true;
                }
                string missionId = Missions[entry % 5], locale = entry < 5 ? "en" : "fa-IR";
                if (GameLocalization.CurrentLocaleCode != locale) GameLocalization.SetLocale(locale, false);
                if (stage == 0)
                {
                    if (!UiShellRuntimeGateway.TryReadCampaignOperations(out var campaign) || !campaign.IsValid) return;
                    if (campaign.SelectedMission.MissionId != missionId)
                    { UiShellRuntimeGateway.TryEnqueueCampaignMissionAction(UiCampaignMissionActionKind.Select, missionId); return; }
                    if (SessionState.GetBool(CampaignChain, false)) CheckCampaignBeforeDeploy(campaign);
                    if (UiShellRuntimeGateway.TryEnqueueCampaignMissionAction(UiCampaignMissionActionKind.Deploy, missionId)) Next(1);
                    return;
                }
                if (!SessionState.GetBool(RecoveryJourney, false) && !SessionState.GetBool(StoryReplay, false)) SkipNarrative(now);
                if (SessionState.GetBool(Journey, false) && stage == 2)
                { if (insideGameView) TickJourney(em, root, now); return; }
                if (stage == 1 || stage == 2)
                {
                    if (UiShellRuntimeGateway.TryReadMissionHudRestrictions(out var restrictions) && restrictions.CinematicInteractionLocked) return;
                    var aria = UnityEngine.Object.FindAnyObjectByType<AriaTutorialBriefingView>();
                    if (aria == null || !aria.IsPresentationVisible || !UiShellRuntimeGateway.TryReadMatchHudAssistantPanel(out var panel)) return;
                    var runtime = em.GetComponentData<CampaignMissionRuntimeComponent>(root);
                    if (runtime.MissionId.ToString() != missionId || !panel.HasRecommendation) return;
                    if (string.IsNullOrWhiteSpace(aria.CurrentInstructionBody) || string.IsNullOrWhiteSpace(aria.TitleText.text))
                        throw new InvalidOperationException("Visible ARIA must include title and instruction.");
                    if (observedStep != panel.TutorialStep)
                    { observedStep = panel.TutorialStep; observedAt = now; return; }
                    if (now - observedAt < .5) return; // Let ECS projection and the rendered HUD settle together.
                    var cue = GameObject.Find("AriaAssistantTargetIndicatorRuntime");
                    var ring = GameObject.Find("AriaAssistantPreviewHighlightRuntime");
                    if (cue == null && ring == null && !aria.ShowMeButton.gameObject.activeInHierarchy)
                    {
                        ScreenCapture.CaptureScreenshot($"{Output}/{entry + 1}-missing-cue.png");
                        var selection = aria.SelectionButton;
                        Debug.Log("[CampaignTutorialEntry] selection=" + (selection != null ? selection.name + " active=" + selection.gameObject.activeInHierarchy + " interactable=" + selection.IsInteractable() : "null"));
                        if (selection != null)
                        {
                            Debug.Log("[CampaignTutorialEntry] local interactable=" + selection.interactable);
                            for (Transform parent = selection.transform; parent != null; parent = parent.parent)
                            {
                                string detail = parent.name;
                                foreach (var group in parent.GetComponents<CanvasGroup>())
                                    detail += $" CanvasGroup alpha={group.alpha} interactable={group.interactable} blocks={group.blocksRaycasts}";
                                Debug.Log("[CampaignTutorialEntry] ancestor=" + detail);
                            }
                            foreach (var component in selection.GetComponents<Component>())
                                Debug.Log("[CampaignTutorialEntry] component=" + component.GetType().FullName);
                        }
                        throw new InvalidOperationException($"{missionId}/{locale} stage={stage} lesson={panel.TutorialStep}: no indicator or useful Show Me. body={aria.CurrentInstructionBody}");
                    }
                    if (cue != null && aria.ShowMeButton.gameObject.activeInHierarchy)
                        throw new InvalidOperationException("Show Me duplicates an already visible UI cue.");
                    if (stage == 1)
                    {
                        if (SessionState.GetBool(StoryReplay, false)) VerifyM01StoryPlayback(locale);
                        firstStep = panel.TutorialStep;
                        ScreenCapture.CaptureScreenshot($"{Output}/{entry + 1}-opening.png");
                        Debug.Log($"[CampaignTutorialEntry] {missionId}/{locale} replay={runtime.RunKind} lesson={firstStep} visible text and cue");
                        Next(2); waitUntil = now + 1; return;
                    }
                    // Verify the actionable opening in M1, M2, M4 and M5, then inspect its next instruction.
                    if (entry % 5 != 2 && panel.TutorialStep == firstStep)
                    {
                        if (aria.DoItButton.gameObject.activeInHierarchy)
                            throw new InvalidOperationException("Do It must stay hidden.");
                        Button next = aria.ContinueButton;
                        if (entry % 5 == 0)
                        {
                            next = aria.SelectionButton;
                        }
                        else if (entry % 5 == 1) next = UnityEngine.Object.FindAnyObjectByType<MatchOverlayCommandControlsView>()?.BuildButton;
                        if (next != null && next.IsActive() && next.IsInteractable())
                        { next.onClick.Invoke(); waitUntil = now + 1; }
                        return;
                    }
                    ScreenCapture.CaptureScreenshot($"{Output}/{entry + 1}-next.png");
                    UiShellRuntimeGateway.TryEnqueueUiAction(UiActionKind.Pause); Next(3); return;
                }
                if (stage == 3)
                {
                    var pause = UnityEngine.Object.FindAnyObjectByType<PauseOptionsV3PopupView>();
                    if (pause == null || !pause.ExitButton.IsInteractable()) return;
                    pause.ExitButton.onClick.Invoke(); Next(4); return;
                }
                if (stage == 4)
                {
                    if (!UiShellRuntimeGateway.TryReadShellState(out var shell) || shell.IsTransitionRunning ||
                        shell.CurrentMode != UiShellMode.MainMenu || shell.Phase != UiShellTransitionPhase.MenuReady) return;
                    if (SessionState.GetBool(StoryReplay, false))
                    {
                        if (entry == 5) { Complete(true, "M1 completed-profile replay: full English and Farsi opening, no language/identity/guidance setup, actionable gameplay handoff, no duplicate story"); return; }
                        entry = 5; Next(0); return;
                    }
                    if (SessionState.GetBool(Journey, false))
                    {
                        if (SessionState.GetBool(CampaignChain, false)) { AdvanceCampaignChain(em, root); return; }
                        if (entry >= 5)
                        {
                            if (SessionState.GetBool(FeedbackRegression, false) && entry == 6)
                            {
                                Debug.Log("[CampaignPlayerJourney] bilingual-complete=" + missionId);
                                entry = 2;
                                journeyStep = 0; lastJourneyAction = null;
                                Next(0); return;
                            }
                            if (SessionState.GetBool(SharedCueRegression, false) && entry is 5 or 7)
                            {
                                Debug.Log("[CampaignPlayerJourney] bilingual-complete=" + missionId);
                                entry = entry == 5 ? 2 : 3;
                                journeyStep = 0; lastJourneyAction = null;
                                Next(0); return;
                            }
                            Complete(true, SessionState.GetBool(SharedCueRegression, false)
                                ? "Bilingual shared-control player journeys, victory and campaign return; see mission markers for scope"
                                : "Bilingual player-action journey, victory and campaign return for " + missionId); return;
                        }
                        entry += 5; journeyStep = 0; lastJourneyAction = null;
                    }
                    else if (++entry == 10) { Complete(true, "M1-M5 completed-profile entries and first-action transitions in English and Farsi"); return; }
                    Next(0);
                }
            }
            catch (Exception error) { Debug.LogException(error); Complete(false, error.Message); }
        }
        private static void Next(int value) { observedStep = 0; stage = value; deadline = EditorApplication.timeSinceStartup + (SessionState.GetBool(Journey, false) && value == 2 ? 1000 : value == 1 && entry % 5 == 0 ? 420 : 120); }
        private static bool Visible(object owner, string field)
        {
            var group = owner.GetType().GetField(field, BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(owner) as CanvasGroup;
            return group != null && group.alpha > .9f && group.gameObject.activeInHierarchy;
        }
        private static void SkipNarrative(double now)
        {
            var view = UnityEngine.Object.FindAnyObjectByType<NarrativeSequenceView>(FindObjectsInactive.Include);
            if (view == null || !Visible(view, "rootGroup") || now - lastSkip < .7) return;
            var confirm = view.SkipConfirmationView;
            bool confirming = confirm != null && Visible(confirm, "group");
            object owner = confirming ? (object)confirm : view.PlaybackControlsView;
            var button = owner?.GetType().GetField(confirming ? "confirmButton" : "skipButton", BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(owner) as Button;
            if (button == null || !button.IsActive() || !button.IsInteractable()) return;
            button.onClick.Invoke(); lastSkip = now;
        }
        private static void Observe(string message, string stack, LogType type)
        {
            if (MissionEditorQaLogClassification.IsEditorCloudTokenFailure(message, stack, type)) return;
            if (type is LogType.Exception or LogType.Assert) failure = message;
            if (type == LogType.Error && message.Contains("Burst error", StringComparison.Ordinal)) failure = message;
        }
        private static void Complete(bool pass, string detail)
        {
            if (finished) return; finished = true; SessionState.SetBool(Active, false);
            EditorApplication.update -= Tick; Application.logMessageReceived -= Observe;
            Canvas.willRenderCanvases -= TickGameView;
            Debug.Log("[CampaignTutorialEntry] result=" + (pass ? "Passed " : "Failed ") + detail);
            MissionEditorValidationExit.Complete(pass);
        }
    }
}
