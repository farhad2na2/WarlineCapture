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
    public static class CampaignTutorialEntryEditorProbe
    {
        private const string Active = "Warline.CampaignTutorialEntry";
        private const string Output = "/private/tmp/warline-campaign-tutorial-entry";
        private static readonly string[] Missions = {
            "saga.ch01.m01.first_contact", "saga.ch01.m02.establish_base", "saga.ch01.m03.radar_warning",
            "saga.ch01.m04.airlift", "saga.ch01.m05.breach_assault" };
        private static int entry, stage, firstStep, observedStep, attentionStage;
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
            Directory.CreateDirectory(Output);
            SessionState.SetBool(Active, true);
            entry = stage = 0; prepared = finished = false; failure = null;
            MainMenuV3PrefabBuilder.SetGameViewResolution(1920, 1080);
            EditorSceneManager.OpenScene(M02EstablishBaseNarrativeConfigBuilder.MenuScenePath, OpenSceneMode.Single);
            AssetDatabase.DisallowAutoRefresh(); Attach(); EditorApplication.EnterPlaymode();
        }
        private static void Attach()
        {
            EditorApplication.update -= Tick; EditorApplication.update += Tick;
            Application.logMessageReceived -= Observe; Application.logMessageReceived += Observe;
        }
        private static void Tick()
        {
            if (finished || !EditorApplication.isPlaying) return;
            try
            {
                if (failure != null) throw new InvalidOperationException(failure);
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
                    var save = new SaveService(new JsonSaveRepository(Path.Combine(Output, Guid.NewGuid().ToString("N"))));
                    var store = new CampaignMissionProgressStore(save);
                    foreach (string mission in Missions)
                    { store.EnsureAvailable(mission); store.Settle(mission, "completed-profile", 1, true, 3, 120000, string.Empty); }
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
                    if (UiShellRuntimeGateway.TryEnqueueCampaignMissionAction(UiCampaignMissionActionKind.Deploy, missionId)) Next(1);
                    return;
                }
                SkipNarrative(now);
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
                        throw new InvalidOperationException($"{missionId}/{locale} stage={stage} lesson={panel.TutorialStep}: no indicator or useful Show Me. body={aria.CurrentInstructionBody}");
                    if (cue != null && aria.ShowMeButton.gameObject.activeInHierarchy)
                        throw new InvalidOperationException("Show Me duplicates an already visible UI cue.");
                    if (stage == 1)
                    {
                        firstStep = panel.TutorialStep; attentionStage = 0;
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
                            var tray = UnityEngine.Object.FindAnyObjectByType<MatchHudSquadTrayView>();
                            var cards = typeof(MatchHudSquadTrayView).GetField("cards", BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(tray) as MatchHudSquadTrayView.Card[];
                            next = cards?[0].Button;
                        }
                        else if (entry % 5 == 1) next = UnityEngine.Object.FindAnyObjectByType<MatchOverlayCommandControlsView>()?.BuildButton;
                        if (next != null && next.IsActive() && next.IsInteractable())
                        { next.onClick.Invoke(); waitUntil = now + 1; }
                        return;
                    }
                    if (entry % 5 == 4 && attentionStage < 2)
                    {
                        if (attentionStage == 0) { attentionStage = 1; waitUntil = now + 5; return; }
                        var pulse = aria.ShowMeButton.transform.Find("AttentionBorder");
                        if (!aria.ShowMeButton.IsActive() || pulse == null || !pulse.gameObject.activeInHierarchy)
                            throw new InvalidOperationException("Idle Show Me must visibly pulse for an obscured target.");
                        ScreenCapture.CaptureScreenshot($"{Output}/{entry + 1}-idle-attention.png");
                        aria.ShowMeButton.onClick.Invoke();
                        if (aria.ShowMeButton.gameObject.activeInHierarchy)
                            throw new InvalidOperationException("Show Me must hide immediately after revealing the next target.");
                        attentionStage = 2; waitUntil = now + 3; return;
                    }
                    if (entry % 5 == 4)
                    {
                        var scroll = aria.BodyText.GetComponentInParent<ScrollRect>();
                        if (scroll != null && scroll.vertical)
                        {
                            if (scroll.verticalScrollbar == null || !scroll.verticalScrollbar.gameObject.activeInHierarchy)
                                throw new InvalidOperationException("Long mission instructions need a visible scroll indicator.");
                            scroll.verticalNormalizedPosition = 0;
                        }
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
                    if (++entry == 10) { Complete(true, "M1-M5 completed-profile entries and first-action transitions in English and Farsi"); return; }
                    Next(0);
                }
            }
            catch (Exception error) { Debug.LogException(error); Complete(false, error.Message); }
        }
        private static void Next(int value) { observedStep = 0; stage = value; deadline = EditorApplication.timeSinceStartup + 120; }
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
        }
        private static void Complete(bool pass, string detail)
        {
            if (finished) return; finished = true; SessionState.SetBool(Active, false);
            EditorApplication.update -= Tick; Application.logMessageReceived -= Observe;
            Debug.Log("[CampaignTutorialEntry] result=" + (pass ? "Passed " : "Failed ") + detail);
            MissionEditorValidationExit.Complete(pass);
        }
    }
}
