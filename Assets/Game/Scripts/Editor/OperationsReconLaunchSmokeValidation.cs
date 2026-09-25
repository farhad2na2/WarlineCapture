using System;
using System.IO;
using Game.Components;
using Game.Composition;
using Game.Runtime;
using Game.Operations.Contracts;
using Game.UI.Contracts;
using Game.UI.Runtime;
using Game.UI.Shell.Contracts.Ecs;
using Unity.Entities;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

namespace Game.Editor
{
    /// <summary>Integration smoke only; gateway/button invocation is not a manual or ARIA acceptance win.</summary>
    public static class OperationsReconLaunchSmokeValidation
    {
        private static double started;
        private static int stage;
        private static bool renderingTick;
        private static int lastRenderingFrame = -1;
        private static int captureFrames, capturedTourStage, capturedTourMask;
        private static double introductionObservedAt, guidanceRequestedAt, ariaRequestedAt;
        private static bool firstScanOnly, activeScanCaptured, selectionDiagnosticsAttached;
        private static bool stopResumeVerified;
        private static double stoppedAt;
        private static float stoppedElapsed;
        private static int stoppedActions;
        private static int resultCaptureFrames;
        private static double resumeRecapAt;
        private static bool wideLayout;
        private static bool withdrawalCancelChecked;
        private static double withdrawalPromptAt;
        private static float withdrawalElapsed;
        private static double checkpointPauseAt;
        private static InputSettings originalInputSettings, validationInputSettings;
        private static readonly System.Collections.Generic.List<InputDevice> isolatedNativeDevices = new();
        public static void RunFirstScan() { firstScanOnly = true; RunAriaVictory(); }
        private static int menuCaptureFrames;
        private static int menuReturnFrames;
        private static int ariaTraceBucket;
        private static bool ariaWasActive;
        private static string capturePath;
        private static int exitCode;
        private static string loadFailure;
        private static bool lifecycle, checkpointResume, checkpointDuringScan, checkpointDuringCarrier, processSave, processRestore, ariaVictory;
        private static int ariaSeed = 1102;
        private static OperationsDifficultyKind ariaDifficulty = OperationsDifficultyKind.Regular;
        private static string checkpointSession;
        private static float checkpointElapsed;
        private static float checkpointScanSeconds;
        private static int checkpointCarrierIndex;
        private const string ProcessCheckpointMarker = "/private/tmp/o001-process-checkpoint.json";
        [Serializable] private sealed class ProcessCheckpoint
        {
            public string root, session;
            public float elapsed, scan;
            public bool carrier;
            public int carrierIndex;
        }
        private static bool recovery, interrupted;
        private static string interruptedSession;
        private static bool manual, persian;
        private static OperationsReconOutcome expectedManualOutcome, observedManualOutcome;
        private static bool manualSaved;
        private static string manualCaptureRoot, lastManualMilestone;
        private static double nextManualCapture;
        private static int manualCaptureIndex;

        public static void Run() => Start(false);
        public static void RunWideLayout() { wideLayout = true; Start(false); }
        public static void RunWideLayoutPersian() { persian = true; RunWideLayout(); }
        public static void RunLifecycle() => Start(true);
        public static void RunCheckpointResume() { checkpointResume = true; Start(false); }
        public static void RunCheckpointResumeDuringScan() { checkpointResume = checkpointDuringScan = true; Start(false); }
        public static void RunCheckpointSaveDuringScanForRestart() { checkpointResume = checkpointDuringScan = processSave = true; Start(false); }
        public static void RunCheckpointSaveWithCarrierForRestart() { checkpointResume = checkpointDuringCarrier = processSave = true; Start(false); }
        public static void RunCheckpointSaveForRestart() { checkpointResume = processSave = true; Start(false); }
        public static void RunCheckpointResumeAfterRestart()
        {
            processRestore = true;
            try { Start(false); }
            catch (Exception exception)
            {
                Debug.LogError("[OperationsReconLaunchSmokeValidation] result=Failed setup=" + exception);
                EditorApplication.Exit(1);
            }
        }
        public static void RunAriaVictory()
        {
            ariaVictory = true;
            try { Start(false); }
            catch (Exception exception)
            {
                Debug.LogError("[OperationsReconLaunchSmokeValidation] result=Failed setup=" + exception);
                EditorApplication.Exit(1);
            }
        }
        public static void RunAriaVictoryPersian() { persian = true; RunAriaVictory(); }
        public static void RunInterruptedRecovery() { interrupted = true; Start(true); }
        public static void RunRecovery() { recovery = true; Start(true); }
        public static void RunManualVictory() => StartManual(OperationsReconOutcome.Victory);
        public static void RunManualVictoryPersian() { persian = true; StartManual(OperationsReconOutcome.Victory); }
        public static void RunManualPartial() => StartManual(OperationsReconOutcome.Partial);

        private static void StartManual(OperationsReconOutcome expected)
        {
            manual = true;
            expectedManualOutcome = expected;
            observedManualOutcome = OperationsReconOutcome.None;
            manualSaved = false;
            lastManualMilestone = string.Empty;
            nextManualCapture = 0;
            manualCaptureIndex = 0;
            manualCaptureRoot = Path.GetFullPath("Build/EditorEvidence/O001Manual/" + DateTime.UtcNow.ToString("yyyyMMdd-HHmmss") + "-" + expected + (persian ? "-fa" : "-en"));
            Directory.CreateDirectory(manualCaptureRoot);
            Start(false);
        }

        // Opens a disposable profile for normal mouse/keyboard acceptance. No mission
        // commands, entity changes, time scaling or automatic outcomes are injected.
        public static void OpenInteractive()
        {
            string saveRoot = Path.Combine(Path.GetTempPath(), "o001-interactive-" + Guid.NewGuid().ToString("N"));
            Environment.SetEnvironmentVariable("WARLINE_VALIDATION_SAVE_ROOT", saveRoot);
            SaveService.CreateDefault().SaveProfile(new PlayerProfileSaveData
            { firstLaunchStatus = FirstLaunchProfileState.Completed, firstLaunchLanguage = "English" });
            EditorSceneManager.OpenScene("Assets/Game/Scenes/Menu.unity", OpenSceneMode.Single);
            Application.runInBackground = true;
            Debug.Log("[OperationsInteractiveValidation] profile=" + saveRoot + " input=normal-mouse-keyboard");
            EditorApplication.EnterPlaymode();
        }

        private static void Start(bool validateLifecycle)
        {
            MainMenuV3PrefabBuilder.SetGameViewResolution(wideLayout ? 4800 : 1920, wideLayout ? 2160 : 1080);
            V3UiLocalizationCatalogBuilder.ApplyConfiguredUiTables();

            lifecycle = validateLifecycle;
            if (processSave && File.Exists(ProcessCheckpointMarker)) File.Delete(ProcessCheckpointMarker);
            ProcessCheckpoint prior = processRestore ? JsonUtility.FromJson<ProcessCheckpoint>(File.ReadAllText(ProcessCheckpointMarker)) : null;
            string saveRoot = prior?.root ?? Path.Combine(Path.GetTempPath(), "o001-launch-smoke-" + Guid.NewGuid().ToString("N"));
            Environment.SetEnvironmentVariable("WARLINE_VALIDATION_SAVE_ROOT", saveRoot);
            if (processRestore)
            {
                checkpointSession = prior.session;
                checkpointElapsed = prior.elapsed;
                checkpointScanSeconds = prior.scan;
                checkpointDuringScan = prior.scan > 0f;
                checkpointDuringCarrier = prior.carrier;
                checkpointCarrierIndex = prior.carrierIndex;
            }
            var save = SaveService.CreateDefault();
            if (!processRestore)
            {
                save.SaveProfile(new PlayerProfileSaveData
                { firstLaunchStatus = FirstLaunchProfileState.Completed, firstLaunchLanguage = persian ? "Persian" : "English" });
                save.SaveSettings(new SettingsSaveData { language = persian ? "Persian" : "English" });
                string locale = persian ? Game.Configs.GameLocalization.PersianLocaleCode : Game.Configs.GameLocalization.EnglishLocaleCode;
                if (!Game.Configs.GameLocalization.SetLocale(locale))
                    throw new InvalidOperationException("Could not select O001 validation locale: " + locale);
            }
            ariaSeed = 1102;
            ariaDifficulty = OperationsDifficultyKind.Regular;
            if (ariaVictory && Environment.GetEnvironmentVariable("WARLINE_VALIDATION_O001_DIFFICULTY") is { Length: > 0 } difficultyText &&
                (!Enum.TryParse(difficultyText, true, out ariaDifficulty) || !Enum.IsDefined(typeof(OperationsDifficultyKind), ariaDifficulty)))
                throw new InvalidOperationException("Invalid O001 ARIA validation difficulty: " + difficultyText);
            if (ariaVictory && Environment.GetEnvironmentVariable("WARLINE_VALIDATION_O001_SEED") is { Length: > 0 } seedText)
            {
                if (!int.TryParse(seedText, out ariaSeed) || ariaSeed <= 0)
                    throw new InvalidOperationException("Invalid O001 ARIA validation seed: " + seedText);
            }
            if (ariaVictory && (ariaSeed != 1102 || ariaDifficulty != OperationsDifficultyKind.Regular))
            {
                var commands = new OperationsProfileCommandService(save);
                var state = commands.Read();
                if (!commands.TryNewRun(new OperationsCommand("cmd.operations.aria" + ariaSeed, state.profileRevision,
                        OperationsCommandKind.NewRun, "", "", ""), ariaSeed, ariaDifficulty,
                        out var created, out _) || !created.Accepted)
                    throw new InvalidOperationException("Could not create seeded O001 validation run: " + ariaSeed + " / " + ariaDifficulty);
            }
            if (interrupted)
            {
                var commands = new OperationsProfileCommandService(save);
                if (!commands.TryNewRun(new OperationsCommand("cmd.operations.interrupted01", 0, OperationsCommandKind.NewRun, "", "", ""),
                    1102, OperationsDifficultyKind.Regular, out var created, out _) || !created.Accepted)
                    throw new InvalidOperationException("Could not create interrupted fixture.");
                var state = commands.Read();
                var offer = Array.Find(state.activeRun.offers, item => item.missionId == "operation.o001");
                if (!commands.TrySubmit(new OperationsCommand("cmd.operations.interrupted02", state.profileRevision, OperationsCommandKind.Deploy,
                    offer.districtId, offer.offerId, ""), out var reserved, out _) || !reserved.Accepted)
                    throw new InvalidOperationException("Could not reserve interrupted fixture.");
                interruptedSession = commands.Read().pendingDeployment.sessionId;
            }
            capturePath = Path.GetFullPath("Build/EditorEvidence/O001SharedWorldLaunch.png");
            Directory.CreateDirectory(Path.GetDirectoryName(capturePath));
            if (!manual && File.Exists(capturePath)) File.Delete(capturePath);
            EditorSceneManager.OpenScene("Assets/Game/Scenes/Menu.unity", OpenSceneMode.Single);
            started = EditorApplication.timeSinceStartup; stage = 0; captureFrames = menuCaptureFrames = menuReturnFrames = 0; ariaTraceBucket = -1; ariaWasActive = false; loadFailure = null;
            if (!processRestore) { checkpointScanSeconds = 0f; checkpointCarrierIndex = -1; }
            Application.logMessageReceived += ObserveLog;
            lastRenderingFrame = -1;
            Canvas.willRenderCanvases += TickInGameView;
            EditorApplication.EnterPlaymode();
        }

        private static void TickInGameView()
        {
            // GraphicRaycaster uses Screen dimensions internally. Run in the
            // rendering context, not an Editor window's update context.
            if (renderingTick || !EditorApplication.isPlaying || lastRenderingFrame == Time.frameCount) return;
            lastRenderingFrame = Time.frameCount;
            renderingTick = true;
            try { Tick(); }
            finally { renderingTick = false; }
        }

        private static void Tick()
        {
            if (!EditorApplication.isPlaying) return;
            Application.runInBackground = true;
            if (ariaVictory) SelectionRuntimeDiagnosticsSystemHelper.EditorClickDiagnosticsEnabled = true;
            if (AriaTouchInputUiSystemHelper.AllowBackgroundValidation && validationInputSettings == null)
            {
                originalInputSettings = InputSystem.settings;
                validationInputSettings = UnityEngine.Object.Instantiate(originalInputSettings);
                validationInputSettings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
                validationInputSettings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
                InputSystem.settings = validationInputSettings;
                foreach (var device in InputSystem.devices)
                    if (device.native && device.enabled && (device is Pointer || device is Keyboard))
                    { InputSystem.ResetDevice(device); InputSystem.DisableDevice(device); isolatedNativeDevices.Add(device); }
                Debug.Log("[OperationsReconLaunchSmokeValidation] inputFixture=background-game-view-only runtimeSettingsClone=1 nativeInputIsolated=" + isolatedNativeDevices.Count);
            }
            try
            {
                if (loadFailure != null) { Complete(false, loadFailure); return; }
                if (EditorApplication.timeSinceStartup - started > (manual || ariaVictory || checkpointDuringCarrier ? 1200 : 240))
                { Complete(false, "timeout stage=" + stage); return; }
                if (manual) { ObserveManualJourney(); return; }
                if (recovery && stage == 2)
                {
                    var failureWorld = World.DefaultGameObjectInjectionWorld;
                    if (failureWorld != null && failureWorld.IsCreated &&
                        OperationsReconLaunchProjection.TryGet(failureWorld.EntityManager, out var failedRoot, out var preparing))
                    {
                        if (preparing.Phase != OperationsReconPhase.Preparing)
                            throw new InvalidOperationException("Missed startup failure injection window.");
                        failureWorld.EntityManager.GetComponentObject<OperationsReconLaunchReference>(failedRoot).StartupFailure =
                            "Editor integration fixture: startup failure";
                        stage = 8;
                        Debug.Log("[OperationsReconLaunchSmokeValidation] injectedStartupFailure=1");
                    }
                }
                if (!UiShellRuntimeGateway.TryReadShellState(out var shell) || shell.IsTransitionRunning) return;
                if (stage == 8 && shell.ActiveRoute == UIRoute.Operations && shell.CurrentMode == UiShellMode.MainMenu)
                {
                    var saved = SaveService.CreateDefault().LoadProfile().operations;
                    var em = World.DefaultGameObjectInjectionWorld.EntityManager;
                    using var roots = em.CreateEntityQuery(typeof(OperationsReconMissionComponent));
                    if (!roots.IsEmptyIgnoreFilter) return;
                    if (saved.pendingDeployment?.reserved == true || saved.activeRun.actionPoints != 3)
                        throw new InvalidOperationException("Failed startup did not refund the reservation.");
                    if (Click("DEPLOY")) stage = 7;
                    return;
                }
                if (stage == 0 && shell.CurrentMode == UiShellMode.MainMenu)
                {
                    if (UiShellRuntimeGateway.TryEnqueueRouteRequest(UiShellRouteIntent.OpenMenuRoute, UIRoute.Operations, false)) stage = 1;
                    return;
                }
                if (stage == 1 && shell.ActiveRoute == UIRoute.Operations && UiShellRuntimeGateway.TryReadOperationsMission(out var model) && model.CanDeploy)
                {
                    string expectedLocale = persian ? Game.Configs.GameLocalization.PersianLocaleCode : Game.Configs.GameLocalization.EnglishLocaleCode;
                    if (UiShellRuntimeGateway.Localization.CurrentLocaleCode != expectedLocale)
                        throw new InvalidOperationException("O001 validation started in " + UiShellRuntimeGateway.Localization.CurrentLocaleCode +
                            " instead of " + expectedLocale);
                    var dashboard = UnityEngine.Object.FindAnyObjectByType<OperationsDashboardScreenView>();
                    if (dashboard == null || dashboard.DailyBriefing == null ||
                        dashboard.transform.Find("DistrictMap") == null ||
                        dashboard.DailyBriefing.Find("StreetSignalsMissionCard") == null)
                        throw new InvalidOperationException("Operations dashboard was replaced instead of presenting O001 inside its briefing.");
                    if (menuCaptureFrames++ < 90) return;
                    if (dashboard.DayLabel.text.Contains("12") ||
                        dashboard.transform.Find("Credits/Value")?.GetComponent<TMPro.TMP_Text>().text != "0")
                        throw new InvalidOperationException("Fresh Operations dashboard still shows authored resource or day placeholders: day=" +
                            dashboard.DayLabel.text + " credits=" + dashboard.transform.Find("Credits/Value")?.GetComponent<TMPro.TMP_Text>().text +
                            " expected=fresh city and zero credits");
                    if (menuCaptureFrames == 91)
                        ScreenCapture.CaptureScreenshot(Path.GetFullPath("Build/EditorEvidence/O001OperationsDashboard.png"));
                    if (menuCaptureFrames < 110) return;
                    if (processRestore)
                    {
                        if (!model.CanResume) throw new InvalidOperationException("Fresh Editor did not offer the saved attempt.");
                        if (Click("RESUME ATTEMPT")) stage = 2;
                        return;
                    }
                    if (interrupted && !model.InterruptedAttempt) throw new InvalidOperationException("Interrupted attempt was not presented for explicit recovery.");
                    foreach (var button in UnityEngine.Object.FindObjectsByType<Button>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
                        if (button.name == Visible("DEPLOY") && button.interactable && button.GetComponentInParent<OperationsMissionScreenView>() != null &&
                            (!interrupted || button.GetComponentInChildren<TMPro.TMP_Text>().text == Visible("RESTART ATTEMPT")))
                        { AssertButtonReachable(button); button.onClick.Invoke(); stage = 2; Debug.Log("[OperationsReconLaunchSmokeValidation] deployButtonInvoked=1 reachable=1 input=button-event-smoke"); break; }
                    return;
                }
                if (stage == 2)
                {
                    if (UiShellRuntimeGateway.TryReadOperationsMission(out var launchModel) &&
                        !launchModel.InMission && !string.IsNullOrEmpty(launchModel.Status))
                        throw new InvalidOperationException(launchModel.Status);
                    var world = World.DefaultGameObjectInjectionWorld;
                    if (world == null || !world.IsCreated) return;
                    using var query = world.EntityManager.CreateEntityQuery(typeof(OperationsReconMissionComponent));
                    if (query.CalculateEntityCount() != 1) return;
                    var mission = query.GetSingleton<OperationsReconMissionComponent>();
                    if (mission.Phase != OperationsReconPhase.Playing) return;
                    if (launchModel.Introduction && !processRestore)
                    {
                        if (introductionObservedAt == 0)
                        {
                            introductionObservedAt = EditorApplication.timeSinceStartup;
                            ScreenCapture.CaptureScreenshot(Path.GetFullPath("Build/EditorEvidence/O001DeploymentBriefing.png"));
                        }
                        if (!launchModel.Resumed && mission.ElapsedSeconds > .1f)
                            throw new InvalidOperationException("The briefing/tour consumed mission time.");
                        if (launchModel.Touring && capturedTourStage != launchModel.IntroductionStage)
                        {
                            var introState = world.EntityManager.GetComponentData<OperationsReconIntroduction>(query.GetSingletonEntity());
                            if (UnityEngine.Time.realtimeSinceStartupAsDouble >= introState.NextStageAt - 1.5 &&
                                Game.Rendering.RuntimeCameraReferenceSystem.TryGetWorldCamera(world, out var tourCamera))
                            {
                                var target = launchModel.IntroductionStage < 4
                                    ? world.EntityManager.GetBuffer<OperationsReconSiteElement>(query.GetSingletonEntity())[launchModel.IntroductionStage-1].Position
                                    : mission.ExitPosition;
                                var framed = tourCamera.WorldToViewportPoint(target);
                                if (framed.z > 0 && framed.x >= .1f && framed.x <= .73f && framed.y >= .2f && framed.y <= .85f)
                                {
                                    capturedTourStage = launchModel.IntroductionStage; capturedTourMask |= 1 << capturedTourStage;
                                    ScreenCapture.CaptureScreenshot(Path.GetFullPath("Build/EditorEvidence/O001Tour" + capturedTourStage + ".png"));
                                }
                            }
                        }
                        if (!launchModel.Touring && EditorApplication.timeSinceStartup - introductionObservedAt > 2)
                        {
                            AssertMissionTextFits();
                            foreach (var view in UnityEngine.Object.FindObjectsByType<OperationsMissionScreenView>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
                                if (view.IsHud && view.IntroductionButton.IsActive()) { AssertButtonReachable(view.IntroductionButton); view.IntroductionButton.onClick.Invoke(); }
                        }
                        return;
                    }

                    if (processRestore)
                    {
                        if (mission.SessionId.ToString() != checkpointSession || mission.ElapsedSeconds < checkpointElapsed ||
                            SaveService.CreateDefault().LoadProfile().operations.activeRun.actionPoints != 2)
                            throw new InvalidOperationException("Process restart changed the session, clock, or AP reservation.");
                        if (checkpointDuringScan)
                        {
                            var sites = world.EntityManager.GetBuffer<OperationsReconSiteElement>(query.GetSingletonEntity());
                            if (sites.Length == 0 || sites[0].ChannelSeconds < checkpointScanSeconds)
                                throw new InvalidOperationException("Process restart lost active scan progress: before=" + checkpointScanSeconds +
                                    " after=" + (sites.Length == 0 ? -1f : sites[0].ChannelSeconds));
                        }
                        if (checkpointDuringCarrier)
                        {
                            var evidence = world.EntityManager.GetComponentData<OperationsReconEvidenceComponent>(query.GetSingletonEntity());
                            if (evidence.Recovered == 0 || evidence.Carrier == Entity.Null ||
                                !world.EntityManager.HasComponent<OperationsReconMemberComponent>(evidence.Carrier) ||
                                world.EntityManager.GetComponentData<OperationsReconMemberComponent>(evidence.Carrier).StableIndex != checkpointCarrierIndex)
                                throw new InvalidOperationException("Process restart lost evidence carrier " + checkpointCarrierIndex);
                        }
                        if (!ResumeRecapHasHandedBackControl()) return;
                        Complete(true, "journey=process-restart-resume input=button-event-smoke session=" + checkpointSession +
                            " elapsedBefore=" + checkpointElapsed + " elapsedAfter=" + mission.ElapsedSeconds +
                            " scanBefore=" + checkpointScanSeconds + " carrierIndex=" + checkpointCarrierIndex + " ap=2");
                        return;
                    }
                    var root = query.GetSingletonEntity();
                    if (interrupted)
                    {
                        var archive = SaveService.CreateDefault().LoadOperationsCheckpoint(interruptedSession);
                        if (mission.SessionId.ToString() != interruptedSession || archive?.restartCount != 1 ||
                            SaveService.CreateDefault().LoadProfile().operations.activeRun.actionPoints != 2)
                            throw new InvalidOperationException("Explicit restart changed the reservation or failed to record restart count.");
                    }
                    int original = world.EntityManager.GetBuffer<OperationsReconRosterElement>(root).Length;
                    using var units = world.EntityManager.CreateEntityQuery(new EntityQueryDesc
                    { All = new[] { ComponentType.ReadOnly<OperationsReconMemberComponent>() }, Options = EntityQueryOptions.IncludeDisabledEntities });
                    if (original != 16 || captureFrames == 0 && units.CalculateEntityCount() != 36) throw new InvalidOperationException("Finite roster mismatch.");
                    captureFrames++;
                    // Editor update callbacks can outpace game frames. Require actual
                    // simulation progress so a frozen world cannot pass the launch gate.
                    if (mission.ElapsedSeconds < 5f) return;
                    var operationHud = UnityEngine.Object.FindObjectsByType<OperationsMissionScreenView>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
                    bool compactHud = false;
                    foreach (var candidate in operationHud)
                    {
                        if (candidate.ScanButton(0) == null) continue;
                        var tracker = candidate.transform.Find("SafeArea/ObjectiveTracker") as RectTransform;
                        compactHud = tracker != null && candidate.GuideOpen && tracker.gameObject.activeSelf &&
                            candidate.ObjectiveButton.gameObject.activeInHierarchy && candidate.transform.Find("WorldObjective0") == null;
                        break;
                    }
                    if (!launchModel.Resumed && capturedTourMask != 62)
                        throw new InvalidOperationException("Tour did not visibly frame all five stages: mask=" + capturedTourMask);
                    if (!compactHud) throw new InvalidOperationException("Operation HUD did not expose the persistent objective tracker at handoff.");
                    if (!Game.Rendering.RuntimeCameraReferenceSystem.TryGetWorldCamera(world, out var camera))
                        throw new InvalidOperationException("Missing shared world camera.");
                    var first = world.EntityManager.GetBuffer<OperationsReconRosterElement>(root)[0].Unit;
                    var position = world.EntityManager.GetComponentData<Unity.Transforms.LocalTransform>(first).Position;
                    var viewport = camera.WorldToViewportPoint(position);
                    using var focuses = world.EntityManager.CreateEntityQuery(typeof(RuntimeCameraFocusRequestComponent));
                    Debug.Log("[OperationsReconLaunchSmokeValidation] camera=" + camera.transform.position +
                        " rotation=" + camera.transform.eulerAngles + " player=" + position + " viewport=" + viewport +
                        " focusCount=" + focuses.CalculateEntityCount());
                    if (viewport.z <= 0 || viewport.x < .05f || viewport.x > .95f || viewport.y < .22f || viewport.y > .6f)
                        throw new InvalidOperationException("Starting infantry are not framed in the playable camera area.");
                    ScreenCapture.CaptureScreenshot(capturePath); stage = 3; captureFrames = 0;
                    return;
                }
                if (stage == 3 && ++captureFrames > 30 && File.Exists(capturePath))
                {
                    AssertMissionTextFits();
                    if (ariaVictory)
                    {
                        if (!selectionDiagnosticsAttached)
                        {
                            var tray = UnityEngine.Object.FindAnyObjectByType<MatchHudSquadTrayView>();
                            foreach (var button in tray.GetComponentsInChildren<Button>(true))
                            { var observedButton = button; observedButton.onClick.AddListener(() => Debug.Log("[OperationsInputTrace] squadCardClick=" + observedButton.name)); }
                            selectionDiagnosticsAttached = true;
                        }
                        if (Click("ARIA PLAY")) { stage = 20; ariaRequestedAt = EditorApplication.timeSinceStartup; }
                        return;
                    }
                    if (checkpointResume)
                    {
                        if (checkpointDuringScan || checkpointDuringCarrier)
                        {
                            if (Click("ARIA PLAY")) stage = checkpointDuringScan ? 22 : 23;
                            return;
                        }
                        var em = World.DefaultGameObjectInjectionWorld.EntityManager;
                        using var roots = em.CreateEntityQuery(typeof(OperationsReconMissionComponent));
                        var active = roots.GetSingleton<OperationsReconMissionComponent>();
                        checkpointSession = active.SessionId.ToString();
                        checkpointElapsed = active.ElapsedSeconds;
                        if (Click("SAVE & EXIT")) stage = 10;
                        return;
                    }
                    if (!lifecycle)
                    {
                        var operationView = UnityEngine.Object.FindObjectsByType<OperationsMissionScreenView>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
                        foreach (var candidate in operationView)
                        {
                            if (candidate.GuideButton == null) continue;
                            AssertButtonReachable(candidate.GuideButton); candidate.GuideButton.onClick.Invoke();
                            if (!candidate.GuideOpen) throw new InvalidOperationException("Objective tracker disappeared while focusing the objective.");
                            guidanceRequestedAt = EditorApplication.timeSinceStartup;
                            stage = 31; captureFrames = 0; return;
                        }
                        throw new InvalidOperationException("Show objective button is missing.");
                    }
                    if (Click("WITHDRAW")) stage = 4;
                    return;
                }
                if (stage == 22)
                {
                    var em = World.DefaultGameObjectInjectionWorld.EntityManager;
                    using var roots = em.CreateEntityQuery(typeof(OperationsReconMissionComponent));
                    if (roots.CalculateEntityCount() != 1) return;
                    var root = roots.GetSingletonEntity();
                    var sites = em.GetBuffer<OperationsReconSiteElement>(root);
                    var active = em.GetComponentData<OperationsReconMissionComponent>(root);
                    var ariaState = UiShellRuntimeGateway.ReadAriaPlay();
                    if (ariaState.Active) ariaWasActive = true;
                    int traceBucket = Mathf.FloorToInt(active.ElapsedSeconds / 30f);
                    if (traceBucket > ariaTraceBucket)
                    {
                        ariaTraceBucket = traceBucket;
                        AssertMissionTextFits();
                        Debug.Log("[OperationsReconCheckpointTrace] elapsed=" + active.ElapsedSeconds.ToString("F0") +
                            " scan=" + (sites.Length == 0 ? -1f : sites[0].ChannelSeconds) +
                            " completed=" + active.CompletedScans + " aria=" + ariaState.Phase +
                            " actions=" + ariaState.Actions + " focused=" + Application.isFocused);
                    }
                    if (ariaWasActive && !ariaState.Active)
                        throw new InvalidOperationException("ARIA stopped before active scan: phase=" + ariaState.Phase +
                            " elapsed=" + active.ElapsedSeconds + " focused=" + Application.isFocused);
                    if (sites.Length == 0 || sites[0].ChannelSeconds < 2f) return;
                    checkpointScanSeconds = sites[0].ChannelSeconds;
                    if (sites[0].Completed != 0 || checkpointScanSeconds >= active.ScanSeconds)
                        throw new InvalidOperationException("Active-scan checkpoint missed the interaction window.");
                    checkpointSession = active.SessionId.ToString();
                    checkpointElapsed = active.ElapsedSeconds;
                    UiShellRuntimeGateway.StopAriaPlay();
                    stage = 24;
                    return;
                }
                if (stage == 23)
                {
                    var world = World.DefaultGameObjectInjectionWorld;
                    if (world == null || !world.IsCreated ||
                        !OperationsReconLaunchProjection.TryGet(world.EntityManager, out var root, out var active)) return;
                    var em = world.EntityManager;
                    var evidence = em.GetComponentData<OperationsReconEvidenceComponent>(root);
                    var ariaState = UiShellRuntimeGateway.ReadAriaPlay();
                    if (ariaState.Active) ariaWasActive = true;
                    int traceBucket = Mathf.FloorToInt(active.ElapsedSeconds / 30f);
                    if (traceBucket > ariaTraceBucket)
                    {
                        ariaTraceBucket = traceBucket;
                        Debug.Log("[OperationsReconCheckpointTrace] elapsed=" + active.ElapsedSeconds.ToString("F0") +
                            " scans=" + active.CompletedScans + " recovered=" + evidence.Recovered +
                            " carrier=" + (evidence.Carrier != Entity.Null) + " aria=" + ariaState.Phase +
                            " actions=" + ariaState.Actions + " focused=" + Application.isFocused);
                    }
                    if (ariaWasActive && !ariaState.Active && evidence.Recovered == 0)
                        throw new InvalidOperationException("ARIA stopped before evidence pickup: phase=" + ariaState.Phase +
                            " elapsed=" + active.ElapsedSeconds + " focused=" + Application.isFocused);
                    if (evidence.Recovered == 0 || evidence.Carrier == Entity.Null) return;
                    if (!em.HasComponent<OperationsReconMemberComponent>(evidence.Carrier))
                        throw new InvalidOperationException("Evidence carrier is not an O001 roster member.");
                    checkpointCarrierIndex = em.GetComponentData<OperationsReconMemberComponent>(evidence.Carrier).StableIndex;
                    checkpointSession = active.SessionId.ToString();
                    checkpointElapsed = active.ElapsedSeconds;
                    UiShellRuntimeGateway.StopAriaPlay();
                    stage = 24;
                    return;
                }
                if (stage == 24)
                {
                    if (UiShellRuntimeGateway.TryReadOperationsMission(out var checkpointModel) && checkpointModel.Paused)
                    {
                        if (checkpointPauseAt == 0)
                        {
                            checkpointPauseAt = EditorApplication.timeSinceStartup;
                            ScreenCapture.CaptureScreenshot(Path.GetFullPath("Build/EditorEvidence/O001Pause.png"));
                            return;
                        }
                        if (EditorApplication.timeSinceStartup - checkpointPauseAt < 2) return;
                    }
                    if (Click("SAVE & EXIT")) stage = 10;
                    return;
                }
                if (stage == 31 && EditorApplication.timeSinceStartup - guidanceRequestedAt > 3)
                {
                    foreach (var candidate in UnityEngine.Object.FindObjectsByType<OperationsMissionScreenView>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
                    {
                        if (candidate.GuideButton == null) continue;
                        if (!UiShellRuntimeGateway.TryReadOperationsMission(out var focused) || !focused.ViewingObjective || !candidate.GuideOpen)
                            throw new InvalidOperationException("Show objective did not focus the objective with the tracker visible.");
                        if (!Game.Rendering.RuntimeCameraReferenceSystem.TryGetWorldCamera(World.DefaultGameObjectInjectionWorld, out var objectiveCamera))
                            throw new InvalidOperationException("Missing camera during objective focus.");
                        var targetViewport = objectiveCamera.WorldToViewportPoint(focused.ObjectivePosition);
                        if (targetViewport.z <= 0 || targetViewport.x < .1f || targetViewport.x > .73f || targetViewport.y < .25f || targetViewport.y > .85f)
                            throw new InvalidOperationException("Show objective did not frame its world target: " + targetViewport);
                        ScreenCapture.CaptureScreenshot(Path.GetFullPath("Build/EditorEvidence/O001ObjectiveFocus.png"));
                        stage = 34; captureFrames = 0;
                        return;
                    }
                }
                if (stage == 34 && ++captureFrames > 30)
                {
                    UiShellRuntimeGateway.TryRequestOperationsMission(UiOperationsMissionAction.FocusSquad);
                    stage = 32; guidanceRequestedAt = EditorApplication.timeSinceStartup;
                }
                if (stage == 32 && EditorApplication.timeSinceStartup - guidanceRequestedAt > 3)
                {
                    if (!UiShellRuntimeGateway.TryReadOperationsMission(out var returned) || returned.ViewingObjective)
                        throw new InvalidOperationException("Return to squad did not release objective focus.");
                    Complete(true, "route=Operations phase=Playing clockAdvanced=5s original=16 total=36 briefing=tour-handoff tracker=persistent camera=objective-squad capture=" + capturePath);
                    return;
                }
                if (stage == 4)
                {
                    if (!withdrawalCancelChecked)
                    {
                        if (GameObject.Find("ConfirmWithdraw") == null) return;
                        var em = World.DefaultGameObjectInjectionWorld.EntityManager;
                        using var roots = em.CreateEntityQuery(typeof(OperationsReconMissionComponent));
                        float elapsed = roots.GetSingleton<OperationsReconMissionComponent>().ElapsedSeconds;
                        if (withdrawalPromptAt == 0)
                        {
                            withdrawalPromptAt = EditorApplication.timeSinceStartup; withdrawalElapsed = elapsed;
                            ScreenCapture.CaptureScreenshot(Path.GetFullPath("Build/EditorEvidence/O001WithdrawConfirmation.png"));
                            return;
                        }
                        if (EditorApplication.timeSinceStartup - withdrawalPromptAt < 2) return;
                        if (elapsed != withdrawalElapsed || Time.timeScale != 0)
                            throw new InvalidOperationException("Withdrawal confirmation resumed the mission underneath the modal.");
                        if (!Click("CANCEL", "ConfirmWithdraw")) throw new InvalidOperationException("Withdrawal confirmation has no usable Cancel.");
                        withdrawalCancelChecked = true; stage = 3;
                        Debug.Log("[OperationsReconLaunchSmokeValidation] withdrawCancel=Passed missionRemainedPaused=1");
                        return;
                    }
                    if (Click("WITHDRAW", "ConfirmWithdraw")) stage = 5;
                    return;
                }
                if (stage == 5 && UiShellRuntimeGateway.TryReadOperationsMission(out var result) && result.Finished && result.Saved)
                {
                    var saved = SaveService.CreateDefault().LoadProfile().operations;
                    if (saved.pendingDeployment?.reserved == true || saved.activeRun.actionPoints != 2)
                        throw new InvalidOperationException("Withdrawal did not durably settle the single AP reservation.");
                    if (Click("CONTINUE")) stage = 6;
                    return;
                }
                if (stage == 6 && shell.ActiveRoute == UIRoute.Operations && shell.CurrentMode == UiShellMode.MainMenu)
                {
                    if (menuReturnFrames++ < 90) return;
                    var dashboard = UnityEngine.Object.FindAnyObjectByType<OperationsDashboardScreenView>();
                    if (dashboard == null || !dashboard.DayLabel.text.Contains("1") ||
                        !dashboard.DailyBriefing.Find("Time").GetComponent<TMPro.TMP_Text>().text.Contains("2"))
                        throw new InvalidOperationException("Operations dashboard did not reflect the settled day and action points: day=" +
                            dashboard?.DayLabel?.text + " ap=" + dashboard?.DailyBriefing?.Find("Time")?.GetComponent<TMPro.TMP_Text>()?.text);
                    var em = World.DefaultGameObjectInjectionWorld.EntityManager;
                    using var roots = em.CreateEntityQuery(typeof(OperationsReconMissionComponent));
                    using var owned = em.CreateEntityQuery(new EntityQueryDesc
                    { All = new[] { ComponentType.ReadOnly<OperationsReconMemberComponent>() }, Options = EntityQueryOptions.IncludeDisabledEntities });
                    if (!roots.IsEmptyIgnoreFilter || !owned.IsEmptyIgnoreFilter)
                        throw new InvalidOperationException("Operations actors survived return to the menu.");
                    if (Click("DEPLOY")) stage = 7;
                    return;
                }
                if (stage == 7 && shell.CurrentMode == UiShellMode.MatchHud)
                {
                    var em = World.DefaultGameObjectInjectionWorld.EntityManager;
                    using var roots = em.CreateEntityQuery(typeof(OperationsReconMissionComponent));
                    if (roots.CalculateEntityCount() != 1) return;
                    var mission = roots.GetSingleton<OperationsReconMissionComponent>();
                    if (UiShellRuntimeGateway.TryReadOperationsMission(out var redeploy) && redeploy.Introduction)
                    { UiShellRuntimeGateway.TryRequestOperationsMission(UiOperationsMissionAction.SkipIntroduction); return; }
                    if (mission.Phase != OperationsReconPhase.Playing || mission.ElapsedSeconds < 2) return;
                    using var owned = em.CreateEntityQuery(new EntityQueryDesc
                    { All = new[] { ComponentType.ReadOnly<OperationsReconMemberComponent>() }, Options = EntityQueryOptions.IncludeDisabledEntities });
                    int expectedAp = recovery ? 2 : 1;
                    if (owned.CalculateEntityCount() != 36 || SaveService.CreateDefault().LoadProfile().operations.activeRun.actionPoints != expectedAp)
                        throw new InvalidOperationException("Redeployment roster or AP reservation mismatch.");
                    Complete(true, "journey=" + (recovery ? "deploy-failed-startup-refund-return-redeploy" : (interrupted ? "interrupted-restart-withdraw-save-return-redeploy" : "deploy-withdraw-save-return-redeploy")) +
                        " input=button-event-smoke original=16 total=36 ap=" + expectedAp);
                }
                if (stage == 10 && shell.ActiveRoute == UIRoute.Operations && shell.CurrentMode == UiShellMode.MainMenu &&
                    UiShellRuntimeGateway.TryReadOperationsMission(out var resumable) && resumable.CanResume)
                {
                    var save = SaveService.CreateDefault();
                    var profile = save.LoadProfile();
                    var archive = save.LoadOperationsCheckpoint(checkpointSession);
                    if (profile.operations.activeRun.actionPoints != 2 || archive == null ||
                        !OperationsReconCheckpointCodec.TryDecode(archive.current, checkpointSession,
                            Resources.Load<Game.Configs.OperationsReconMissionConfig>(Game.Configs.OperationsReconMissionConfig.ResourcePath).operationMap.ContentHash, out _))
                        throw new InvalidOperationException("Save & Exit did not retain a valid same-attempt checkpoint and one AP cost.");
                    if (processSave)
                    {
                        File.WriteAllText(ProcessCheckpointMarker, JsonUtility.ToJson(new ProcessCheckpoint
                        { root = Environment.GetEnvironmentVariable("WARLINE_VALIDATION_SAVE_ROOT"), session = checkpointSession,
                            elapsed = checkpointElapsed, scan = checkpointScanSeconds,
                            carrier = checkpointDuringCarrier, carrierIndex = checkpointCarrierIndex }));
                        Complete(true, "journey=process-restart-save input=button-event-smoke session=" + checkpointSession +
                            " elapsed=" + checkpointElapsed + " scan=" + checkpointScanSeconds +
                            " ap=2 marker=" + ProcessCheckpointMarker);
                        return;
                    }
                    if (Click("RESUME ATTEMPT")) stage = 11;
                    return;
                }
                if (stage == 11 && shell.CurrentMode == UiShellMode.MatchHud)
                {
                    var em = World.DefaultGameObjectInjectionWorld.EntityManager;
                    using var roots = em.CreateEntityQuery(typeof(OperationsReconMissionComponent));
                    if (roots.CalculateEntityCount() != 1) return;
                    var resumed = roots.GetSingleton<OperationsReconMissionComponent>();
                    if (resumed.Phase != OperationsReconPhase.Playing) return;
                    if (resumed.SessionId.ToString() != checkpointSession || resumed.ElapsedSeconds < checkpointElapsed ||
                        SaveService.CreateDefault().LoadProfile().operations.activeRun.actionPoints != 2)
                        throw new InvalidOperationException("Resume changed the session, clock, or AP reservation.");
                    if (checkpointDuringScan)
                    {
                        var sites = em.GetBuffer<OperationsReconSiteElement>(roots.GetSingletonEntity());
                        if (sites.Length == 0 || sites[0].ChannelSeconds < checkpointScanSeconds)
                            throw new InvalidOperationException("Resume lost active scan progress: before=" + checkpointScanSeconds +
                                " after=" + (sites.Length == 0 ? -1f : sites[0].ChannelSeconds));
                    }
                    if (!ResumeRecapHasHandedBackControl()) return;
                    Complete(true, "journey=save-exit-resume input=button-event-smoke session=" + checkpointSession +
                        " elapsedBefore=" + checkpointElapsed + " elapsedAfter=" + resumed.ElapsedSeconds +
                        " scanBefore=" + checkpointScanSeconds + " ap=2");
                }
                if (stage == 33 && ++captureFrames > 30 && File.Exists(Path.GetFullPath("Build/EditorEvidence/O001FirstScanComplete.png")))
                { Complete(true, "journey=first-scan input=visible-touch scans=1"); return; }
                if (stage == 40)
                {
                    var state = UiShellRuntimeGateway.ReadAriaPlay();
                    if (state.Active)
                    {
                        if (EditorApplication.timeSinceStartup - stoppedAt > 3)
                            throw new InvalidOperationException("Visible STOP ARIA did not stop the session.");
                        return;
                    }
                    stoppedActions = state.Actions;
                    stoppedAt = EditorApplication.timeSinceStartup;
                    ScreenCapture.CaptureScreenshot(Path.GetFullPath("Build/EditorEvidence/O001AriaStopped.png"));
                    stage = 41;
                    return;
                }
                if (stage == 41)
                {
                    var state = UiShellRuntimeGateway.ReadAriaPlay();
                    if (state.Active || state.Actions != stoppedActions)
                        throw new InvalidOperationException("ARIA continued issuing input after STOP ARIA.");
                    if (EditorApplication.timeSinceStartup - stoppedAt < 4) return;
                    if (!UiShellRuntimeGateway.TryReadOperationsMission(out var stoppedModel) || stoppedModel.Paused || stoppedModel.Finished)
                        throw new InvalidOperationException("Stopping ARIA paused or ended the mission.");
                    var em = World.DefaultGameObjectInjectionWorld.EntityManager;
                    using var roots = em.CreateEntityQuery(typeof(OperationsReconMissionComponent));
                    if (roots.GetSingleton<OperationsReconMissionComponent>().ElapsedSeconds <= stoppedElapsed + 1)
                        throw new InvalidOperationException("The mission clock stopped with ARIA.");
                    if (!Click("ARIA PLAY")) throw new InvalidOperationException("Visible ARIA PLAY was not restored after stopping.");
                    stopResumeVerified = true; ariaWasActive = false;
                    ariaRequestedAt = EditorApplication.timeSinceStartup; stage = 20;
                    Debug.Log("[OperationsReconLaunchSmokeValidation] ariaStopResume=Passed input=visible-button-events missionContinued=1");
                    return;
                }
                if (stage == 20)
                {
                    var world = World.DefaultGameObjectInjectionWorld;
                    if (world == null || !world.IsCreated ||
                        !OperationsReconLaunchProjection.TryGet(world.EntityManager, out var root, out var operation)) return;
                    if (firstScanOnly && stopResumeVerified && operation.CompletedScans >= 1)
                    {
                        ScreenCapture.CaptureScreenshot(Path.GetFullPath("Build/EditorEvidence/O001FirstScanComplete.png"));
                        stage = 33; captureFrames = 0;
                        return;
                    }
                    if (!stopResumeVerified && operation.CompletedScans >= 1 && UiShellRuntimeGateway.ReadAriaPlay().Active)
                    {
                        if (!Click("STOP ARIA")) throw new InvalidOperationException("Visible STOP ARIA is missing while ARIA is active.");
                        stoppedAt = EditorApplication.timeSinceStartup; stoppedElapsed = operation.ElapsedSeconds;
                        stage = 40;
                        return;
                    }
                    if (!activeScanCaptured)
                        foreach (var site in world.EntityManager.GetBuffer<OperationsReconSiteElement>(root))
                            if (site.ChannelSeconds >= 5 && site.Completed == 0)
                            { ScreenCapture.CaptureScreenshot(Path.GetFullPath("Build/EditorEvidence/O001ActiveScan.png")); activeScanCaptured = true; break; }
                    int traceBucket = Mathf.FloorToInt(operation.ElapsedSeconds / 30f);
                    var ariaState = UiShellRuntimeGateway.ReadAriaPlay();
                    if (ariaState.Active) ariaWasActive = true;
                    using var sessions = world.EntityManager.CreateEntityQuery(typeof(AriaPlaySessionComponent));
                    var session = sessions.IsEmptyIgnoreFilter ? default : sessions.GetSingleton<AriaPlaySessionComponent>();
                    if (traceBucket > ariaTraceBucket)
                    {
                        ariaTraceBucket = traceBucket;
                        using var observations = world.EntityManager.CreateEntityQuery(typeof(AriaPlayObservationComponent));
                        AssertMissionTextFits();
                        var observation = observations.IsEmptyIgnoreFilter ? default : observations.GetSingleton<AriaPlayObservationComponent>();
                        using var selectedUnits = world.EntityManager.CreateEntityQuery(typeof(SelectedUnitTag));
                        ScreenCapture.CaptureScreenshot(Path.GetFullPath("Build/EditorEvidence/O001AriaLatest.png"));
                        Debug.Log("[OperationsReconAriaTrace] selected=" + selectedUnits.CalculateEntityCount() + " elapsed=" + operation.ElapsedSeconds.ToString("F0") +
                            " phase=" + ariaState.Phase + " actions=" + ariaState.Actions + " scans=" + operation.CompletedScans +
                            " surviving=" + operation.SurvivingInfantry + " atExit=" + operation.InfantryAtExit +
                            " evidenceRecovered=" + world.EntityManager.GetComponentData<OperationsReconEvidenceComponent>(root).Recovered +
                            " goal=" + observation.GoalId + " kind=" + observation.Kind + " target=" + observation.TargetId +
                            " stopReason=" + session.StopReason + " focused=" + Application.isFocused + " timeScale=" + Time.timeScale);
                    }
                    if (!ariaWasActive && EditorApplication.timeSinceStartup - ariaRequestedAt > 20)
                        throw new InvalidOperationException("ARIA failed to start: stopReason=" + session.StopReason + " focused=" + Application.isFocused);
                    if (ariaWasActive && !ariaState.Active && operation.Phase != OperationsReconPhase.Terminal)
                        throw new InvalidOperationException("ARIA stopped before O001 finished: elapsed=" + operation.ElapsedSeconds.ToString("F0") +
                            " phase=" + ariaState.Phase + " actions=" + ariaState.Actions +
                            " attempts=" + session.Attempts + " goal=" + session.GoalId +
                            " target=" + session.TargetId + " evidenceProgress=" +
                            world.EntityManager.GetComponentData<OperationsReconEvidenceComponent>(root).ChannelSeconds.ToString("F1") +
                            " stopReason=" + session.StopReason +
                            " focused=" + Application.isFocused + " timeScale=" + Time.timeScale);
                    if (operation.Phase != OperationsReconPhase.Terminal) return;
                    if (!UiShellRuntimeGateway.TryReadOperationsMission(out var outcome) || !outcome.Saved) return;
                    if (operation.Outcome != OperationsReconOutcome.Victory)
                        throw new InvalidOperationException("ARIA did not win O001: outcome=" + operation.Outcome +
                            " elapsed=" + operation.ElapsedSeconds.ToString("F1") +
                            " scans=" + operation.CompletedScans +
                            " surviving=" + operation.SurvivingInfantry +
                            " atExit=" + operation.InfantryAtExit +
                            " evidenceRecovered=" + world.EntityManager.GetComponentData<OperationsReconEvidenceComponent>(root).Recovered +
                            " actions=" + UiShellRuntimeGateway.ReadAriaPlay().Actions);
                    if (resultCaptureFrames++ == 0)
                    {
                        ScreenCapture.CaptureScreenshot(Path.GetFullPath("Build/EditorEvidence/O001MissionResult.png"));
                        Debug.Log("[OperationsReconAriaVictory] elapsed=" + operation.ElapsedSeconds.ToString("F1") +
                            " scans=" + operation.CompletedScans + " survivors=" + operation.SurvivingInfantry +
                            " extracted=" + operation.InfantryAtExit + " saved=1");
                    }
                    if (resultCaptureFrames < 30) return;
                    if (Click("CONTINUE")) stage = 21;
                    return;
                }
                if (stage == 21 && shell.ActiveRoute == UIRoute.Operations && shell.CurrentMode == UiShellMode.MainMenu)
                {
                    if (SaveService.CreateDefault().LoadProfile().operations.activeRun.actionPoints != 2)
                        throw new InvalidOperationException("ARIA victory did not settle one AP.");
                    string expectedLocale = persian ? Game.Configs.GameLocalization.PersianLocaleCode : Game.Configs.GameLocalization.EnglishLocaleCode;
                    if (UiShellRuntimeGateway.Localization.CurrentLocaleCode != expectedLocale)
                        throw new InvalidOperationException("O001 validation returned in the wrong locale: " +
                            UiShellRuntimeGateway.Localization.CurrentLocaleCode);
                    Complete(true, "journey=aria-victory-return input=visible-touch seed=" + ariaSeed +
                        " difficulty=" + ariaDifficulty + " language=" + UiShellRuntimeGateway.Localization.CurrentLocaleCode + " ap=2");
                }
            }
            catch (Exception exception) { Complete(false, exception.ToString()); }
        }

        // This observer never issues a mission/selection command or mutates the tactical
        // world. CUA mouse/keyboard inputs drive the actual player controls throughout.
        private static void ObserveManualJourney()
        {
            if (!UiShellRuntimeGateway.TryReadShellState(out var shell) || shell.IsTransitionRunning) return;
            var world = World.DefaultGameObjectInjectionWorld;
            if (world == null || !world.IsCreated) return;
            var em = world.EntityManager;
            if (OperationsReconLaunchProjection.TryGet(em, out var root, out var mission))
            {
                if (mission.Phase == OperationsReconPhase.Preparing) return;
                stage = 1;
                var evidence = em.GetComponentData<OperationsReconEvidenceComponent>(root);
                string milestone = mission.CompletedScans + ":" + evidence.Recovered + ":" + (evidence.Carrier != Entity.Null) + ":" + mission.Outcome;
                if (milestone != lastManualMilestone || EditorApplication.timeSinceStartup >= nextManualCapture)
                {
                    lastManualMilestone = milestone;
                    nextManualCapture = EditorApplication.timeSinceStartup + 60;
                    var trace = new ManualTrace
                    {
                        session = mission.SessionId.ToString(), elapsed = mission.ElapsedSeconds,
                        scans = mission.CompletedScans, survivors = mission.SurvivingInfantry,
                        extracted = mission.InfantryAtExit, recovered = evidence.Recovered != 0,
                        carried = evidence.Carrier != Entity.Null, outcome = mission.Outcome.ToString()
                    };
                    string stem = Path.Combine(manualCaptureRoot, (++manualCaptureIndex).ToString("D3"));
                    File.WriteAllText(stem + ".json", JsonUtility.ToJson(trace, true));
                    ScreenCapture.CaptureScreenshot(stem + ".png");
                    Debug.Log("[OperationsReconManualAcceptance] milestone=" + JsonUtility.ToJson(trace));
                }
                if (mission.Phase == OperationsReconPhase.Terminal &&
                    UiShellRuntimeGateway.TryReadOperationsMission(out var model) && model.Saved)
                {
                    observedManualOutcome = mission.Outcome;
                    manualSaved = true;
                }
                return;
            }
            if (stage == 1 && manualSaved && shell.ActiveRoute == UIRoute.Operations && shell.CurrentMode == UiShellMode.MainMenu)
            {
                var saved = SaveService.CreateDefault().LoadProfile().operations;
                bool passed = observedManualOutcome == expectedManualOutcome && saved.pendingDeployment?.reserved != true && saved.activeRun.actionPoints == 2;
                string detail = "input=normal-mouse-keyboard expected=" + expectedManualOutcome + " outcome=" + observedManualOutcome +
                    " saved=" + manualSaved + " returned=1 ap=" + saved.activeRun.actionPoints + " evidence=" + manualCaptureRoot;
                Debug.Log("[OperationsReconManualAcceptance] result=" + (passed ? "Passed" : "Failed") + " " + detail);
                Complete(passed, detail);
            }
        }

        [Serializable]
        private sealed class ManualTrace
        {
            public string session, outcome;
            public float elapsed;
            public int scans, survivors, extracted;
            public bool recovered, carried;
        }

        private static bool ResumeRecapHasHandedBackControl()
        {
            if (!UiShellRuntimeGateway.TryReadOperationsMission(out var model) || !model.Resumed) return false;
            if (resumeRecapAt == 0)
            {
                if (!model.Introduction) throw new InvalidOperationException("Saved mission skipped its resume recap.");
                resumeRecapAt = EditorApplication.timeSinceStartup;
                ScreenCapture.CaptureScreenshot(Path.GetFullPath("Build/EditorEvidence/O001ResumeRecap.png"));
                return false;
            }
            if (model.Introduction)
            {
                if (!model.Touring && EditorApplication.timeSinceStartup - resumeRecapAt > 2)
                {
                    AssertMissionTextFits();
                    foreach (var view in UnityEngine.Object.FindObjectsByType<OperationsMissionScreenView>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
                        if (view.IsHud && view.IntroductionButton.IsActive()) { AssertButtonReachable(view.IntroductionButton); view.IntroductionButton.onClick.Invoke(); }
                }
                return false;
            }
            return !model.Paused;
        }

        private static void AssertMissionTextFits()
        {
            Canvas.ForceUpdateCanvases();
            foreach (var view in UnityEngine.Object.FindObjectsByType<OperationsMissionScreenView>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            {
                if (!view.IsHud) continue;
                foreach (var label in view.GetComponentsInChildren<TMPro.TMP_Text>())
                {
                    label.ForceMeshUpdate();
                    if (label.isTextOverflowing)
                        throw new InvalidOperationException("Mission text overflows its native box: " + label.text + " rect=" + label.rectTransform.rect);
                }
            }
        }

        private static bool Click(string name, string ancestor = null)
        {
            if (name is "ARIA PLAY" or "STOP ARIA")
            {
                bool stopping = name == "STOP ARIA";
                foreach (var view in UnityEngine.Object.FindObjectsByType<OperationsMissionScreenView>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
                    if (view.IsHud && view.AriaButton != null && view.AriaButton.IsActive() && view.AriaButton.interactable &&
                        UiShellRuntimeGateway.ReadAriaPlay().Active == stopping)
                    { AssertButtonReachable(view.AriaButton); view.AriaButton.onClick.Invoke(); return true; }
            }
            foreach (var button in UnityEngine.Object.FindObjectsByType<Button>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            {
                bool matching = button.name == Visible(name) || button.GetComponentInChildren<TMPro.TMP_Text>()?.text == Visible(name);
                bool owner = button.GetComponentInParent<OperationsMissionScreenView>() != null || button.GetComponentInParent<PauseOptionsV3PopupView>() != null;
                if (!matching || !button.interactable || !owner) continue;
                if (ancestor != null)
                {
                    bool found = false;
                    for (var parent = button.transform.parent; parent != null; parent = parent.parent)
                        if (parent.name == ancestor) { found = true; break; }
                    if (!found) continue;
                }
                AssertButtonReachable(button); button.onClick.Invoke(); return true;
            }
            if (name is "ARIA PLAY" or "SAVE & EXIT" or "WITHDRAW")
                UiShellRuntimeGateway.TryEnqueueUiAction(UiActionKind.Pause);
            return false;
        }

        private static void AssertButtonReachable(Button button)
        {
            Canvas.ForceUpdateCanvases();
            var rect = (RectTransform)button.transform;
            var canvas = button.GetComponentInParent<Canvas>().rootCanvas;
            var camera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
            // Use the same pixel coordinates as the Canvas camera and Game view.
            var gameSize = Handles.GetMainGameViewSize();
            var screenBounds = camera != null ? camera.pixelRect : new Rect(Vector2.zero, gameSize);
            var corners = new Vector3[4]; rect.GetWorldCorners(corners);
            foreach (var corner in corners)
            {
                var point = RectTransformUtility.WorldToScreenPoint(camera, corner);
                if (point.x < screenBounds.xMin || point.x > screenBounds.xMax ||
                    point.y < screenBounds.yMin || point.y > screenBounds.yMax)
                    throw new InvalidOperationException("Visible button extends outside the screen: " + button.name +
                        " point=" + point + " viewport=" + screenBounds + " canvas=" + canvas.renderMode);
            }
            var center = RectTransformUtility.WorldToScreenPoint(camera, rect.TransformPoint(rect.rect.center));
            var hits = new System.Collections.Generic.List<UnityEngine.EventSystems.RaycastResult>();
            var events = UnityEngine.EventSystems.EventSystem.current;
            if (events == null) throw new InvalidOperationException("No EventSystem for visible button: " + button.name);
            events.RaycastAll(new UnityEngine.EventSystems.PointerEventData(events) { position = center }, hits);
            if (hits.Count == 0 || !hits[0].gameObject.transform.IsChildOf(button.transform))
                throw new InvalidOperationException("Visible button is covered or clipped: " + button.name + " hit=" +
                    (hits.Count == 0 ? "none" : hits[0].gameObject.name));
        }

        private static string Visible(string name) => name switch
        {
            "DEPLOY" => UiShellRuntimeGateway.Localization.Get("operations.deploy", name),
            "RESTART ATTEMPT" => UiShellRuntimeGateway.Localization.Get("operations.o001.restart_attempt", name),
            "SAVE & EXIT" => UiShellRuntimeGateway.Localization.Get("operations.o001.save_exit", name),
            "RESUME ATTEMPT" => UiShellRuntimeGateway.Localization.Get("operations.o001.resume_attempt", name),
            "ARIA PLAY" => UiShellRuntimeGateway.Localization.Get("operations.o001.aria_play", name),
            "STOP ARIA" => UiShellRuntimeGateway.Localization.Get("operations.o001.aria_stop", name),
            "WITHDRAW" => UiShellRuntimeGateway.Localization.Get("operations.withdraw", name),
            "CONTINUE" => UiShellRuntimeGateway.Localization.Get("ui.common.continue", name),
            _ => name
        };

        private static void Complete(bool passed, string detail)
        {
            Canvas.willRenderCanvases -= TickInGameView;
            Application.logMessageReceived -= ObserveLog;
            foreach (var device in isolatedNativeDevices) if (device.added) InputSystem.EnableDevice(device);
            isolatedNativeDevices.Clear();
            if (validationInputSettings != null)
            { InputSystem.settings = originalInputSettings; UnityEngine.Object.Destroy(validationInputSettings); validationInputSettings = null; }
            exitCode = passed ? 0 : 1;
            Debug.Log("[OperationsReconLaunchSmokeValidation] result=" + (passed ? "Passed" : "Failed") + " " + detail);
            EditorApplication.playModeStateChanged += Exit;
            EditorApplication.ExitPlaymode();
        }

        private static void ObserveLog(string message, string trace, LogType type)
        {
            if (type is LogType.Error or LogType.Exception &&
                (message.StartsWith("[OperationMapSourceScene]", StringComparison.Ordinal) ||
                 message.Contains("OperationsRecon") || trace.Contains("OperationsRecon") ||
                 trace.Contains("OperationsMissionPresentationSystem")))
                loadFailure = message;
        }

        private static void Exit(PlayModeStateChange state)
        {
            if (state != PlayModeStateChange.EnteredEditMode) return;
            EditorApplication.playModeStateChanged -= Exit;
            EditorApplication.Exit(exitCode);
        }
    }
}
