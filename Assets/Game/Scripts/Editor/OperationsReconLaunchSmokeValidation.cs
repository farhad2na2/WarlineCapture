using System;
using System.IO;
using Game.Components;
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
    /// <summary>Integration smoke only; gateway/button invocation is not a manual or ARIA acceptance win.</summary>
    public static class OperationsReconLaunchSmokeValidation
    {
        private static double started;
        private static int stage;
        private static int captureFrames;
        private static string capturePath;
        private static int exitCode;
        private static string loadFailure;
        private static bool lifecycle;

        public static void Run() => Start(false);
        public static void RunLifecycle() => Start(true);

        private static void Start(bool validateLifecycle)
        {
            lifecycle = validateLifecycle;
            string saveRoot = Path.Combine(Path.GetTempPath(), "o001-launch-smoke-" + Guid.NewGuid().ToString("N"));
            Environment.SetEnvironmentVariable("WARLINE_VALIDATION_SAVE_ROOT", saveRoot);
            var save = SaveService.CreateDefault();
            save.SaveProfile(new PlayerProfileSaveData
            { firstLaunchStatus = FirstLaunchProfileState.Completed, firstLaunchLanguage = "English" });
            capturePath = Path.GetFullPath("Build/EditorEvidence/O001SharedWorldLaunch.png");
            Directory.CreateDirectory(Path.GetDirectoryName(capturePath));
            if (File.Exists(capturePath)) File.Delete(capturePath);
            EditorSceneManager.OpenScene("Assets/Game/Scenes/Menu.unity", OpenSceneMode.Single);
            started = EditorApplication.timeSinceStartup; stage = 0; captureFrames = 0; loadFailure = null;
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
                if (EditorApplication.timeSinceStartup - started > 240)
                { Complete(false, "timeout stage=" + stage); return; }
                if (!UiShellRuntimeGateway.TryReadShellState(out var shell) || shell.IsTransitionRunning) return;
                if (stage == 0 && shell.CurrentMode == UiShellMode.MainMenu)
                {
                    if (UiShellRuntimeGateway.TryEnqueueRouteRequest(UiShellRouteIntent.OpenMenuRoute, UIRoute.Operations, false)) stage = 1;
                    return;
                }
                if (stage == 1 && shell.ActiveRoute == UIRoute.Operations && UiShellRuntimeGateway.TryReadOperationsMission(out var model) && model.CanDeploy)
                {
                    foreach (var button in UnityEngine.Object.FindObjectsByType<Button>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
                        if (button.name == "DEPLOY" && button.interactable && button.GetComponentInParent<OperationsMissionScreenView>() != null)
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
                    var root = query.GetSingletonEntity();
                    int original = world.EntityManager.GetBuffer<OperationsReconRosterElement>(root).Length;
                    using var units = world.EntityManager.CreateEntityQuery(new EntityQueryDesc
                    { All = new[] { ComponentType.ReadOnly<OperationsReconMemberComponent>() }, Options = EntityQueryOptions.IncludeDisabledEntities });
                    if (original != 16 || captureFrames == 0 && units.CalculateEntityCount() != 36) throw new InvalidOperationException("Finite roster mismatch.");
                    captureFrames++;
                    // Editor update callbacks can outpace game frames. Require actual
                    // simulation progress so a frozen world cannot pass the launch gate.
                    if (mission.ElapsedSeconds < 5f) return;
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
                    if (!lifecycle)
                    { Complete(true, "route=Operations phase=Playing clockAdvanced=5s original=16 total=36 capture=" + capturePath); return; }
                    if (Click("WITHDRAW")) stage = 4;
                    return;
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
                    if (owned.CalculateEntityCount() != 36 || SaveService.CreateDefault().LoadProfile().operations.activeRun.actionPoints != 1)
                        throw new InvalidOperationException("Redeployment roster or AP reservation mismatch.");
                    Complete(true, "journey=deploy-withdraw-save-return-redeploy input=button-event-smoke original=16 total=36 ap=1");
                }
            }
            catch (Exception exception) { Complete(false, exception.ToString()); }
        }

        private static bool Click(string name, string ancestor = null)
        {
            foreach (var button in UnityEngine.Object.FindObjectsByType<Button>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            {
                if (button.name != name || !button.interactable || button.GetComponentInParent<OperationsMissionScreenView>() == null) continue;
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
                (message.StartsWith("[OperationMapSourceScene]", StringComparison.Ordinal) || trace.Contains("OperationsMissionPresentationSystem")))
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
