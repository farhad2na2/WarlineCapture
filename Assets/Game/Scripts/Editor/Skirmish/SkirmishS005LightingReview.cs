#if UNITY_EDITOR
using System.IO;
using Game.UI.Runtime;
using UnityEditor;
using UnityEngine;

namespace Game.Editor
{
    /// <summary>Native visual diagnosis only; never counts as a mission playthrough.</summary>
    public static class SkirmishS005LightingReview
    {
        private static Light sun;
        private static LightShadows original;
        private static int stage;
        private static double next;
        private static string directory;

        public static void RunNativeComparisonAndExit()
        {
            foreach (string key in new[] { "WARLINE_S003_HELIPAD_PROBE", "WARLINE_SKIRMISH_AIRCRAFT_CYCLE",
                "WARLINE_S003_PRODUCTION_PROBE", "WARLINE_SKIRMISH_INPUT_DIAGNOSTIC" })
                System.Environment.SetEnvironmentVariable(key, null);
            directory = Path.Combine(Path.GetTempPath(), "warline-s005-lighting-review-" +
                System.DateTime.UtcNow.ToString("yyyyMMdd-HHmmss"));
            Directory.CreateDirectory(directory);
            stage = 0;
            next = 0;
            sun = null;
            EditorApplication.update += Tick;
            EditorApplication.playModeStateChanged += OnPlayMode;
            SkirmishS002AriaRunHarness.RunS005MapReviewAndExit();
        }

        private static void OnPlayMode(PlayModeStateChange change)
        {
            if (change != PlayModeStateChange.ExitingPlayMode) return;
            if (sun != null) sun.shadows = original;
            EditorApplication.update -= Tick;
            EditorApplication.playModeStateChanged -= OnPlayMode;
            if (stage != 5)
                Debug.LogError("[S005LightingReview] result=Incomplete stage=" + stage);
        }

        private static void Tick()
        {
            if (!EditorApplication.isPlaying || EditorApplication.timeSinceStartup < next) return;
            if (stage == 0)
            {
                if (Object.FindAnyObjectByType<SkirmishMatchView>() == null ||
                    Camera.main == null || ShaderUtil.anythingCompiling ||
                    !UiShellRuntimeGateway.TryReadSkirmish(out _)) return;
                foreach (var light in Object.FindObjectsByType<Light>(FindObjectsSortMode.None))
                    if (light.enabled && light.gameObject.activeInHierarchy && light.type == LightType.Directional)
                    { sun = light; break; }
                if (sun == null) return;
                original = sun.shadows;
                next = EditorApplication.timeSinceStartup + 3;
                stage = 1;
                return;
            }
            if (stage == 1)
            {
                if (!UiShellRuntimeGateway.TryReadShellState(out var shell) || shell.IsTransitionRunning ||
                    shell.CurrentMode != Game.UI.Contracts.UiShellMode.MatchHud) return;
                var world = Unity.Entities.World.DefaultGameObjectInjectionWorld;
                var cameraSystem = world?.GetExistingSystemManaged<Game.Runtime.RtsCameraSystem>();
                if (cameraSystem == null) return;
                cameraSystem.ClearSmoothPerspectiveTarget();
                cameraSystem.ApplyPerspectiveCameraModeInstant(Camera.main, 100f, 50f, -90f, 50f);
                cameraSystem.SetSmoothFocusTarget(new Vector3(436f, 0f, 520f), true, .25f);
                Debug.Log("[S005LightingReview] camera=DiagnosticGuidance focus=(436,0,520) height=100 ordersIssued=0");
                next = EditorApplication.timeSinceStartup + 3;
                stage = 11;
                return;
            }
            if (stage == 11)
            {
                Debug.Log("[S005LightingReview] cameraActual=" + Camera.main.transform.position +
                    " angles=" + Camera.main.transform.eulerAngles);
                ScreenCapture.CaptureScreenshot(Path.Combine(directory, "native-original.png"));
                next = EditorApplication.timeSinceStartup + 2;
                stage = 2;
                return;
            }
            if (stage == 2)
            {
                sun.shadows = LightShadows.None;
                next = EditorApplication.timeSinceStartup + 2;
                stage = 3;
                return;
            }
            if (stage == 3)
            {
                if (ShaderUtil.anythingCompiling)
                { next = EditorApplication.timeSinceStartup + 2; return; }
                ScreenCapture.CaptureScreenshot(Path.Combine(directory, "native-without-sun-shadows.png"));
                next = EditorApplication.timeSinceStartup + 2;
                stage = 4;
                return;
            }
            if (stage == 4)
            {
                sun.shadows = original;
                if (!File.Exists(Path.Combine(directory, "native-original.png")) ||
                    !File.Exists(Path.Combine(directory, "native-without-sun-shadows.png")))
                    return;
                stage = 5;
                EditorApplication.update -= Tick;
                Debug.Log("[S005LightingReview] result=Captured diagnosticOnly=1 shadowsRestored=1 directory=" + directory);
            }
        }
    }
}
#endif
