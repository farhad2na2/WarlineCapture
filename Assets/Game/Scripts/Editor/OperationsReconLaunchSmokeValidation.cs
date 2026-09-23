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

namespace Game.Editor
{
    /// <summary>Integration smoke only; gateway/button invocation is not a manual or ARIA acceptance win.</summary>
    public static class OperationsReconLaunchSmokeValidation
    {
        private static double started;
        private static int stage;
        private static int captureFrames;
        private static int menuCaptureFrames;
        private static int menuReturnFrames;
        private static int ariaTraceBucket;
        private static bool ariaWasActive;
        private static string capturePath;
        private static int exitCode;
        private static string loadFailure;
        private static bool lifecycle, checkpointResume, processSave, processRestore, ariaVictory;
        private static string checkpointSession;
        private static float checkpointElapsed;
        private const string ProcessCheckpointMarker = "/private/tmp/o001-process-checkpoint.json";
        [Serializable] private sealed class ProcessCheckpoint { public string root, session; public float elapsed; }
        private static bool recovery, interrupted;
        private static string interruptedSession;
        private static bool manual, persian;
        private static OperationsReconOutcome expectedManualOutcome, observedManualOutcome;
        private static bool manualSaved;
        private static string manualCaptureRoot, lastManualMilestone;
        private static double nextManualCapture;
        private static int manualCaptureIndex;

        public static void Run() => Start(false);
        public static void RunLifecycle() => Start(true);
        public static void RunCheckpointResume() { checkpointResume = true; Start(false); }
        public static void RunCheckpointSaveForRestart() { checkpointResume = processSave = true; Start(false); }
        public static void RunCheckpointResumeAfterRestart() { processRestore = true; Start(false); }
        public static void RunAriaVictory() { ariaVictory = true; Start(false); }
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
            lifecycle = validateLifecycle;
            if (processSave && File.Exists(ProcessCheckpointMarker)) File.Delete(ProcessCheckpointMarker);
            ProcessCheckpoint prior = processRestore ? JsonUtility.FromJson<ProcessCheckpoint>(File.ReadAllText(ProcessCheckpointMarker)) : null;
            string saveRoot = prior?.root ?? Path.Combine(Path.GetTempPath(), "o001-launch-smoke-" + Guid.NewGuid().ToString("N"));
            Environment.SetEnvironmentVariable("WARLINE_VALIDATION_SAVE_ROOT", saveRoot);
            if (processRestore) { checkpointSession = prior.session; checkpointElapsed = prior.elapsed; }
            var save = SaveService.CreateDefault();
            if (!processRestore)
            {
                save.SaveProfile(new PlayerProfileSaveData
                { firstLaunchStatus = FirstLaunchProfileState.Completed, firstLaunchLanguage = "English" });
                save.SaveSettings(new SettingsSaveData { language = persian ? "Persian" : "English" });
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
            Application.logMessageReceived += ObserveLog;
            EditorApplication.update += Tick;
            EditorApplication.EnterPlaymode();
        }

        private static void Tick()
        {
            if (!EditorApplication.isPlaying) return;
            Application.runInBackground = true;
            try
            {
                if (loadFailure != null) { Complete(false, loadFailure); return; }
                if (EditorApplication.timeSinceStartup - started > (manual || ariaVictory ? 1200 : 240))
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
                        { button.onClick.Invoke(); stage = 2; Debug.Log("[OperationsReconLaunchSmokeValidation] deployButtonInvoked=1 input=button-event-smoke"); break; }
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
                    if (processRestore)
                    {
                        if (mission.SessionId.ToString() != checkpointSession || mission.ElapsedSeconds < checkpointElapsed ||
                            SaveService.CreateDefault().LoadProfile().operations.activeRun.actionPoints != 2)
                            throw new InvalidOperationException("Process restart changed the session, clock, or AP reservation.");
                        Complete(true, "journey=process-restart-resume input=button-event-smoke session=" + checkpointSession +
                            " elapsedBefore=" + checkpointElapsed + " elapsedAfter=" + mission.ElapsedSeconds + " ap=2");
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
                        var tracker = candidate.transform.Find("MissionCard") as RectTransform;
                        compactHud = tracker != null && tracker.anchorMin.x >= .75f &&
                            !candidate.GuideOpen && !tracker.gameObject.activeSelf &&
                            candidate.GuideButton.gameObject.activeSelf &&
                            candidate.transform.Find("MissionGuideDrawer") is Transform drawer && !drawer.gameObject.activeSelf;
                        break;
                    }
                    if (!compactHud) throw new InvalidOperationException("Operation HUD did not preserve a compact, closed guide at launch.");
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
                    if (ariaVictory)
                    {
                        if (Click("ARIA PLAY")) stage = 20;
                        return;
                    }
                    if (checkpointResume)
                    {
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
                            candidate.GuideButton.onClick.Invoke();
                            if (!candidate.GuideOpen) throw new InvalidOperationException("Mission Guide did not open from its button.");
                            ScreenCapture.CaptureScreenshot(Path.GetFullPath("Build/EditorEvidence/O001MissionGuidePopup.png"));
                            stage = 31; captureFrames = 0; return;
                        }
                        throw new InvalidOperationException("Mission Guide button is missing.");
                    }
                    if (Click("WITHDRAW")) stage = 4;
                    return;
                }
                if (stage == 31 && ++captureFrames > 30 &&
                    File.Exists(Path.GetFullPath("Build/EditorEvidence/O001MissionGuidePopup.png")))
                {
                    foreach (var candidate in UnityEngine.Object.FindObjectsByType<OperationsMissionScreenView>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
                    {
                        if (candidate.GuideButton == null) continue;
                        candidate.GuideButton.onClick.Invoke();
                        if (candidate.GuideOpen) throw new InvalidOperationException("Mission Guide did not close from its button.");
                        Complete(true, "route=Operations phase=Playing clockAdvanced=5s original=16 total=36 guide=open-closed capture=" + capturePath);
                        return;
                    }
                }
                if (stage == 4)
                {
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
                        { root = Environment.GetEnvironmentVariable("WARLINE_VALIDATION_SAVE_ROOT"), session = checkpointSession, elapsed = checkpointElapsed }));
                        Complete(true, "journey=process-restart-save input=button-event-smoke session=" + checkpointSession +
                            " elapsed=" + checkpointElapsed + " ap=2 marker=" + ProcessCheckpointMarker);
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
                    Complete(true, "journey=save-exit-resume input=button-event-smoke session=" + checkpointSession +
                        " elapsedBefore=" + checkpointElapsed + " elapsedAfter=" + resumed.ElapsedSeconds + " ap=2");
                }
                if (stage == 20)
                {
                    var world = World.DefaultGameObjectInjectionWorld;
                    if (world == null || !world.IsCreated ||
                        !OperationsReconLaunchProjection.TryGet(world.EntityManager, out var root, out var operation)) return;
                    int traceBucket = Mathf.FloorToInt(operation.ElapsedSeconds / 30f);
                    var ariaState = UiShellRuntimeGateway.ReadAriaPlay();
                    if (ariaState.Active) ariaWasActive = true;
                    using var sessions = world.EntityManager.CreateEntityQuery(typeof(AriaPlaySessionComponent));
                    var session = sessions.IsEmptyIgnoreFilter ? default : sessions.GetSingleton<AriaPlaySessionComponent>();
                    if (traceBucket > ariaTraceBucket)
                    {
                        ariaTraceBucket = traceBucket;
                        using var observations = world.EntityManager.CreateEntityQuery(typeof(AriaPlayObservationComponent));
                        var observation = observations.IsEmptyIgnoreFilter ? default : observations.GetSingleton<AriaPlayObservationComponent>();
                        Debug.Log("[OperationsReconAriaTrace] elapsed=" + operation.ElapsedSeconds.ToString("F0") +
                            " phase=" + ariaState.Phase + " actions=" + ariaState.Actions + " scans=" + operation.CompletedScans +
                            " goal=" + observation.GoalId + " kind=" + observation.Kind + " target=" + observation.TargetId +
                            " stopReason=" + session.StopReason + " focused=" + Application.isFocused + " timeScale=" + Time.timeScale);
                    }
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
                            " scans=" + operation.CompletedScans + " actions=" + UiShellRuntimeGateway.ReadAriaPlay().Actions);
                    if (Click("CONTINUE")) stage = 21;
                    return;
                }
                if (stage == 21 && shell.ActiveRoute == UIRoute.Operations && shell.CurrentMode == UiShellMode.MainMenu)
                {
                    if (SaveService.CreateDefault().LoadProfile().operations.activeRun.actionPoints != 2)
                        throw new InvalidOperationException("ARIA victory did not settle one AP.");
                    Complete(true, "journey=aria-victory-return input=visible-touch seed=1102 ap=2");
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

        private static bool Click(string name, string ancestor = null)
        {
            foreach (var button in UnityEngine.Object.FindObjectsByType<Button>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            {
                if (button.name != Visible(name) || !button.interactable || button.GetComponentInParent<OperationsMissionScreenView>() == null) continue;
                if (ancestor != null)
                {
                    bool found = false;
                    for (var parent = button.transform.parent; parent != null; parent = parent.parent)
                        if (parent.name == ancestor) { found = true; break; }
                    if (!found) continue;
                }
                button.onClick.Invoke(); return true;
            }
            return false;
        }

        private static string Visible(string name) => name switch
        {
            "DEPLOY" => UiShellRuntimeGateway.Localization.Get("operations.deploy", name),
            "RESTART ATTEMPT" => UiShellRuntimeGateway.Localization.Get("operations.o001.restart_attempt", name),
            "SAVE & EXIT" => UiShellRuntimeGateway.Localization.Get("operations.o001.save_exit", name),
            "RESUME ATTEMPT" => UiShellRuntimeGateway.Localization.Get("operations.o001.resume_attempt", name),
            "ARIA PLAY" => UiShellRuntimeGateway.Localization.Get("operations.o001.aria_play", name),
            "WITHDRAW" => UiShellRuntimeGateway.Localization.Get("operations.withdraw", name),
            "CONTINUE" => UiShellRuntimeGateway.Localization.Get("ui.common.continue", name),
            _ => name
        };

        private static void Complete(bool passed, string detail)
        {
            EditorApplication.update -= Tick;
            Application.logMessageReceived -= ObserveLog;
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
