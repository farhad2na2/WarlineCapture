using System;
using System.IO;
using System.Reflection;
using Game.Composition;
using Game.Configs;
using Game.Runtime;
using Game.UI.Runtime;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Game.Editor
{
    public static class MobileVisualQualityPlayModeCapture
    {
        private const string MenuScenePath = "Assets/Game/Scenes/Menu.unity";
        private const string RuntimeUiConfigPath = "Assets/Game/Data/UI/RuntimeUiConfig.asset";
        private const string CurrentProfilePath = "Assets/Game/Rendering/VisualQualityConfig.asset";
        private const string CandidateProfilePath = "Assets/Game/Rendering/VisualQualityConfig_MobileCandidate.asset";
        private const string DefaultArtifactDirectory = "Design/AgentReports/Captures/MobileVisualQuality";
        private const int DefaultCaptureWidth = 1920;
        private const int DefaultCaptureHeight = 1080;
        private const int WarmupFrames = 90;
        private const int ZoomSettleFrames = 90;
        private const int EnvironmentSettleFrames = 24;
        private const float DayCaptureHour = 12f;
        private const float DuskCaptureHour = 21f;
        private const float NightCaptureHour = 23f;

        private enum CaptureProfile
        {
            Current,
            Candidate
        }

        private enum CapturePhase
        {
            WaitingForPlayMode,
            WaitingForMenu,
            WaitingForMatch,
            WarmupGameplay,
            SetDay,
            WaitingForDay,
            CaptureGameplayZoom,
            SetMaxZoomOut,
            WaitingForMaxZoomOut,
            CaptureMaxZoomOut,
            SetDusk,
            WaitingForDusk,
            CaptureDusk,
            SetNight,
            WaitingForNight,
            CaptureNight,
            Complete
        }

        private static CaptureProfile captureProfile;
        private static VisualQualityProfileAsset selectedProfile;
        private static string artifactDirectory;
        private static string manifestPath;
        private static int frameCount;
        private static int deployFrame;
        private static int matchReadyFrame;
        private static int phaseFrame;
        private static bool deploySubmitted;
        private static bool profileApplied;
        private static bool completed;
        private static bool exitEditorAfterCapture;
        private static double startedAt;
        private static MobileVisualQualityCaptureMatrix matrixCapture;

        [MenuItem("Game/Rendering/Capture Mobile Visual Quality/Current")]
        public static void CaptureCurrent()
        {
            exitEditorAfterCapture = false;
            Run(CaptureProfile.Current);
        }

        [MenuItem("Game/Rendering/Capture Mobile Visual Quality/Candidate")]
        public static void CaptureCandidate()
        {
            exitEditorAfterCapture = false;
            Run(CaptureProfile.Candidate);
        }

        public static void CaptureFromEnvironment()
        {
            exitEditorAfterCapture = true;
            string profile = Environment.GetEnvironmentVariable("WARLINE_MOBILE_VISUAL_CAPTURE_PROFILE");
            Run(string.Equals(profile, "candidate", StringComparison.OrdinalIgnoreCase)
                ? CaptureProfile.Candidate
                : CaptureProfile.Current);
        }

        private static void Run(CaptureProfile profile)
        {
            try
            {
                RuntimeUiConfig runtimeUiConfig = AssetDatabase.LoadAssetAtPath<RuntimeUiConfig>(RuntimeUiConfigPath);
                if (runtimeUiConfig == null)
                    throw new InvalidOperationException($"Missing runtime UI config: {RuntimeUiConfigPath}");

                SetRuntimeUiMode(runtimeUiConfig, RuntimeUiMode.Canvas);
                EditorUtility.SetDirty(runtimeUiConfig);
                AssetDatabase.SaveAssets();

                captureProfile = profile;
                selectedProfile = LoadProfile(profile);
                bool matrixRequested = string.Equals(
                    Environment.GetEnvironmentVariable(MobileVisualQualityCaptureMatrix.ModeEnvironmentVariable),
                    MobileVisualQualityCaptureMatrix.MatrixMode,
                    StringComparison.OrdinalIgnoreCase);
                artifactDirectory = Environment.GetEnvironmentVariable("WARLINE_MOBILE_VISUAL_CAPTURE_DIR");
                if (string.IsNullOrWhiteSpace(artifactDirectory))
                {
                    artifactDirectory = matrixRequested
                        ? MobileVisualQualityCaptureMatrix.ArtifactDirectory
                        : DefaultArtifactDirectory;
                }
                manifestPath = Path.Combine(artifactDirectory, $"{GetProfileLabel()}_manifest.md");
                matrixCapture = MobileVisualQualityCaptureMatrix.TryCreateFromEnvironment(
                    GetProfileLabel(),
                    artifactDirectory,
                    ApplyTimeProofState,
                    TryRenderCamera);
                if (matrixCapture == null)
                {
                    Directory.CreateDirectory(artifactDirectory);
                    DeleteProfileArtifacts();
                }

                EditorSceneManager.OpenScene(MenuScenePath, OpenSceneMode.Single);
                frameCount = 0;
                deployFrame = 0;
                matchReadyFrame = 0;
                phaseFrame = 0;
                deploySubmitted = false;
                profileApplied = false;
                completed = false;
                startedAt = EditorApplication.timeSinceStartup;
                SetPhase(CapturePhase.WaitingForPlayMode);

                SceneManager.sceneLoaded -= OnSceneLoaded;
                SceneManager.sceneLoaded += OnSceneLoaded;
                EditorApplication.playModeStateChanged -= ExitBatchAfterPlayMode;
                EditorApplication.update -= Continue;
                EditorApplication.update += Continue;
                EditorApplication.EnterPlaymode();
            }
            catch (Exception exception)
            {
                Debug.LogError($"[MobileVisualQualityPlayModeCapture] result=Failed\n{exception}");
                if (Application.isBatchMode)
                    EditorApplication.Exit(1);
            }
        }

        private static void Continue()
        {
            if (completed)
                return;

            try
            {
                if (!EditorApplication.isPlaying)
                    return;

                frameCount++;
                if (frameCount == 1)
                    startedAt = EditorApplication.timeSinceStartup;

                if (EditorApplication.timeSinceStartup - startedAt > 240d)
                {
                    string timeoutPhase = matrixCapture != null ? matrixCapture.PhaseLabel : SessionPhase().ToString();
                    Complete(false, $"Timed out profile={GetProfileLabel()} phase={timeoutPhase} frame={frameCount} deploy={deploySubmitted} profileApplied={profileApplied}");
                    return;
                }

                if (frameCount < 45)
                    return;

                MenuBootstrapView bootstrap = UnityEngine.Object.FindAnyObjectByType<MenuBootstrapView>(FindObjectsInactive.Include);
                if (bootstrap != null)
                {
                    bootstrap.ApplyRuntimeUiMode();
                    if (bootstrap.UiMode != RuntimeUiMode.Canvas)
                    {
                        Complete(false, "Runtime UI mode is not Canvas.");
                        return;
                    }
                }

                if (!deploySubmitted)
                {
                    if (matrixCapture != null && !matrixCapture.TryCaptureMenu(bootstrap))
                        return;

                    Button deployButton = FindDeployButton();
                    if (deployButton == null)
                        return;

                    deployButton.onClick.Invoke();
                    deploySubmitted = true;
                    deployFrame = frameCount;
                    SetPhase(CapturePhase.WaitingForMatch);
                    return;
                }

                MatchSceneView matchScene = UnityEngine.Object.FindAnyObjectByType<MatchSceneView>(FindObjectsInactive.Exclude);
                if (matchScene == null || !SceneManager.GetSceneByName("Match").isLoaded)
                    return;

                ApplyProfileIfNeeded(matchScene);
                if (!matchScene.GameplayStartComplete && frameCount - deployFrame < 420)
                    return;

                if (matchReadyFrame == 0)
                {
                    matchReadyFrame = frameCount;
                    SetPhase(CapturePhase.WarmupGameplay);
                    return;
                }

                if (matrixCapture != null)
                {
                    if (frameCount - matchReadyFrame < WarmupFrames)
                        return;
                    if (matrixCapture.Tick(matchScene, frameCount))
                        Complete(true, $"profile={GetProfileLabel()} metadata={matrixCapture.MetadataPath}");
                    return;
                }

                CapturePhase phase = SessionPhase();
                switch (phase)
                {
                    case CapturePhase.WarmupGameplay:
                        if (frameCount - matchReadyFrame >= WarmupFrames)
                            SetPhase(CapturePhase.SetDay);
                        break;

                    case CapturePhase.SetDay:
                        ApplyTimeProofState(matchScene, DayCaptureHour);
                        SetPhase(CapturePhase.WaitingForDay);
                        break;

                    case CapturePhase.WaitingForDay:
                        if (frameCount - phaseFrame >= EnvironmentSettleFrames)
                            SetPhase(CapturePhase.CaptureGameplayZoom);
                        break;

                    case CapturePhase.CaptureGameplayZoom:
                        CaptureViewpoint(matchScene, "gameplay_zoom");
                        SetPhase(CapturePhase.SetMaxZoomOut);
                        break;

                    case CapturePhase.SetMaxZoomOut:
                        RequestMaxZoomOut(matchScene);
                        SetPhase(CapturePhase.WaitingForMaxZoomOut);
                        break;

                    case CapturePhase.WaitingForMaxZoomOut:
                        if (frameCount - phaseFrame >= ZoomSettleFrames)
                            SetPhase(CapturePhase.CaptureMaxZoomOut);
                        break;

                    case CapturePhase.CaptureMaxZoomOut:
                        CaptureViewpoint(matchScene, "max_zoom_out");
                        SetPhase(CapturePhase.SetDusk);
                        break;

                    case CapturePhase.SetDusk:
                        ApplyTimeProofState(matchScene, DuskCaptureHour);
                        SetPhase(CapturePhase.WaitingForDusk);
                        break;

                    case CapturePhase.WaitingForDusk:
                        if (frameCount - phaseFrame >= EnvironmentSettleFrames)
                            SetPhase(CapturePhase.CaptureDusk);
                        break;

                    case CapturePhase.CaptureDusk:
                        CaptureViewpoint(matchScene, "dusk_phase");
                        SetPhase(CapturePhase.SetNight);
                        break;

                    case CapturePhase.SetNight:
                        ApplyTimeProofState(matchScene, NightCaptureHour);
                        SetPhase(CapturePhase.WaitingForNight);
                        break;

                    case CapturePhase.WaitingForNight:
                        if (frameCount - phaseFrame >= EnvironmentSettleFrames)
                            SetPhase(CapturePhase.CaptureNight);
                        break;

                    case CapturePhase.CaptureNight:
                        CaptureViewpoint(matchScene, "night_phase");
                        WriteManifest(matchScene);
                        Complete(true, $"profile={GetProfileLabel()} manifest={manifestPath}");
                        break;
                }
            }
            catch (Exception exception)
            {
                Complete(false, exception.ToString());
            }
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (!string.Equals(scene.name, "Match", StringComparison.Ordinal))
                return;

            MatchSceneView matchScene = UnityEngine.Object.FindAnyObjectByType<MatchSceneView>(FindObjectsInactive.Exclude);
            if (matchScene != null)
                ApplyProfileIfNeeded(matchScene);
        }

        private static void ApplyProfileIfNeeded(MatchSceneView matchScene)
        {
            if (profileApplied || matchScene == null || selectedProfile == null)
                return;

            SerializedObject serializedObject = new(matchScene);
            SerializedProperty profileProperty = serializedObject.FindProperty("visualQualityProfile");
            if (profileProperty == null)
                throw new InvalidOperationException("MatchSceneView is missing serialized visualQualityProfile field.");

            profileProperty.objectReferenceValue = selectedProfile;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
            profileApplied = true;
        }

        private static void RequestMaxZoomOut(MatchSceneView matchScene)
        {
            SelectionUiCameraSystemHelper cameraControl = matchScene.MatchBootstrap.SelectionUiCamera;
            if (cameraControl == null)
                return;

            cameraControl.RequestZoomOutLevel();
        }

        private static void ApplyTimeProofState(MatchSceneView matchScene, float hour)
        {
            DayNightSystem dayNight = matchScene.MatchBootstrap.DayNight;
            if (dayNight == null)
                throw new InvalidOperationException("Match Day/Night owner is unavailable for visual proof capture.");

            Type type = typeof(DayNightSystem);
            FieldInfo hourField = type.GetField("_currentHour", BindingFlags.Instance | BindingFlags.NonPublic);
            FieldInfo visualRefreshField = type.GetField("_nextVisualRefreshTime", BindingFlags.Instance | BindingFlags.NonPublic);
            MethodInfo applyVisualState = type.GetMethod("ApplyVisualState", BindingFlags.Instance | BindingFlags.NonPublic);
            if (hourField == null || visualRefreshField == null || applyVisualState == null)
                throw new InvalidOperationException("DayNightSystem no longer exposes the expected editor proof internals.");

            hourField.SetValue(dayNight, hour);
            visualRefreshField.SetValue(dayNight, 0f);
            applyVisualState.Invoke(dayNight, Array.Empty<object>());
        }

        private static void CaptureViewpoint(MatchSceneView matchScene, string viewpoint)
        {
            Camera camera = matchScene.WorldCamera;
            if (camera == null)
                throw new InvalidOperationException("Match scene is missing WorldCamera.");

            string extension = SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null ? ".txt" : ".png";
            string path = Path.Combine(artifactDirectory, $"{GetProfileLabel()}_{viewpoint}{extension}");
            if (!TryRenderCamera(camera, path, out string error))
                throw new InvalidOperationException(error);
        }

        private static bool TryRenderCamera(Camera camera, string path, out string error)
        {
            return TryRenderCamera(
                camera,
                path,
                ResolvePositiveInt("WARLINE_MOBILE_VISUAL_CAPTURE_WIDTH", DefaultCaptureWidth),
                ResolvePositiveInt("WARLINE_MOBILE_VISUAL_CAPTURE_HEIGHT", DefaultCaptureHeight),
                false,
                out error);
        }

        private static bool TryRenderCamera(
            Camera camera,
            string path,
            int width,
            int height,
            bool requireGraphicsDevice,
            out string error)
        {
            error = string.Empty;
            if (camera == null)
            {
                error = "Cannot render mobile visual quality proof because world camera is null.";
                return false;
            }

            if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null)
            {
                if (requireGraphicsDevice)
                {
                    error = "Matrix visual capture requires windowed Unity with a non-Null graphics device.";
                    return false;
                }
                File.WriteAllText(
                    path,
                    $"Camera render skipped because Unity is running with a Null graphics device.\n" +
                    $"profile={GetProfileLabel()}\n" +
                    $"cameraPosition={Format(camera.transform.position)}\n" +
                    $"cameraRotation={camera.transform.rotation.eulerAngles}\n" +
                    $"fieldOfView={camera.fieldOfView:0.00}\n" +
                    $"orthographic={camera.orthographic}\n" +
                    $"orthographicSize={camera.orthographicSize:0.00}\n");
                return true;
            }

            RenderTexture previousTarget = camera.targetTexture;
            RenderTexture previousActive = RenderTexture.active;
            RenderTexture renderTexture = null;
            Texture2D texture = null;
            try
            {
                renderTexture = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32)
                {
                    name = "MobileVisualQualityProofRenderTexture"
                };
                texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
                camera.targetTexture = renderTexture;
                RenderTexture.active = renderTexture;
                camera.Render();
                texture.ReadPixels(new Rect(0, 0, width, height), 0, 0);
                texture.Apply(false, false);
                File.WriteAllBytes(path, texture.EncodeToPNG());
                return true;
            }
            catch (Exception exception)
            {
                error = $"Failed to render mobile visual quality proof camera capture path={path}\n{exception}";
                return false;
            }
            finally
            {
                camera.targetTexture = previousTarget;
                RenderTexture.active = previousActive;
                if (renderTexture != null)
                    UnityEngine.Object.DestroyImmediate(renderTexture);
                if (texture != null)
                    UnityEngine.Object.DestroyImmediate(texture);
            }
        }

        private static Button FindDeployButton()
        {
            Button[] buttons = UnityEngine.Object.FindObjectsByType<Button>(FindObjectsInactive.Exclude);
            for (int i = 0; i < buttons.Length; i++)
            {
                Button candidate = buttons[i];
                if (candidate == null || !candidate.isActiveAndEnabled)
                    continue;

                string objectName = candidate.gameObject.name;
                if (string.Equals(objectName, "DeployCommandButton", StringComparison.Ordinal) ||
                    string.Equals(objectName, "DeployOperationButton", StringComparison.Ordinal))
                {
                    return candidate;
                }
            }

            return null;
        }

        private static void WriteManifest(MatchSceneView matchScene)
        {
            Camera camera = matchScene.WorldCamera;
            string profileLabel = GetProfileLabel();
            string profilePath = captureProfile == CaptureProfile.Candidate ? CandidateProfilePath : CurrentProfilePath;
            File.WriteAllText(
                manifestPath,
                $"# Mobile Visual Quality {profileLabel} Capture\n\n" +
                $"- Profile: `{profilePath}`\n" +
                $"- Graphics device: `{SystemInfo.graphicsDeviceType}`\n" +
                $"- Resolution: `{ResolvePositiveInt("WARLINE_MOBILE_VISUAL_CAPTURE_WIDTH", DefaultCaptureWidth)}x{ResolvePositiveInt("WARLINE_MOBILE_VISUAL_CAPTURE_HEIGHT", DefaultCaptureHeight)}`\n" +
                $"- Day phase ({DayCaptureHour:0}:00), gameplay zoom: `{profileLabel}_gameplay_zoom.png`\n" +
                $"- Day phase ({DayCaptureHour:0}:00), max zoom out: `{profileLabel}_max_zoom_out.png`\n" +
                $"- Dusk phase ({DuskCaptureHour:0}:00): `{profileLabel}_dusk_phase.png`\n" +
                $"- Night phase ({NightCaptureHour:0}:00): `{profileLabel}_night_phase.png`\n" +
                $"- Camera position after captures: `{(camera != null ? Format(camera.transform.position) : "missing")}`\n" +
                $"- Camera rotation after captures: `{(camera != null ? Format(camera.transform.rotation.eulerAngles) : "missing")}`\n");
        }

        private static VisualQualityProfileAsset LoadProfile(CaptureProfile profile)
        {
            string path = profile == CaptureProfile.Candidate ? CandidateProfilePath : CurrentProfilePath;
            VisualQualityProfileAsset asset = AssetDatabase.LoadAssetAtPath<VisualQualityProfileAsset>(path);
            if (asset == null)
                throw new InvalidOperationException($"Missing visual quality profile: {path}");
            return asset;
        }

        private static void SetRuntimeUiMode(RuntimeUiConfig runtimeConfig, RuntimeUiMode mode)
        {
            SerializedObject serialized = new(runtimeConfig);
            SerializedProperty modeProperty = serialized.FindProperty("mode");
            if (modeProperty == null)
                throw new InvalidOperationException("RuntimeUiConfig is missing serialized mode field.");

            modeProperty.intValue = (int)mode;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetPhase(CapturePhase phase)
        {
            SessionState.SetInt("MobileVisualQualityPlayModeCapture.Phase", (int)phase);
            phaseFrame = frameCount;
        }

        private static CapturePhase SessionPhase()
        {
            return (CapturePhase)SessionState.GetInt(
                "MobileVisualQualityPlayModeCapture.Phase",
                (int)CapturePhase.WaitingForPlayMode);
        }

        private static string GetProfileLabel()
        {
            return captureProfile == CaptureProfile.Candidate ? "candidate" : "current";
        }

        private static void DeleteProfileArtifacts()
        {
            string label = GetProfileLabel();
            DeleteIfExists(Path.Combine(artifactDirectory, $"{label}_gameplay_zoom.png"));
            DeleteIfExists(Path.Combine(artifactDirectory, $"{label}_gameplay_zoom.txt"));
            DeleteIfExists(Path.Combine(artifactDirectory, $"{label}_max_zoom_out.png"));
            DeleteIfExists(Path.Combine(artifactDirectory, $"{label}_max_zoom_out.txt"));
            DeleteIfExists(Path.Combine(artifactDirectory, $"{label}_dusk_phase.png"));
            DeleteIfExists(Path.Combine(artifactDirectory, $"{label}_dusk_phase.txt"));
            DeleteIfExists(Path.Combine(artifactDirectory, $"{label}_night_phase.png"));
            DeleteIfExists(Path.Combine(artifactDirectory, $"{label}_night_phase.txt"));
            DeleteIfExists(manifestPath);
        }

        private static void DeleteIfExists(string path)
        {
            if (File.Exists(path))
                File.Delete(path);
        }

        private static int ResolvePositiveInt(string variableName, int fallback)
        {
            string value = Environment.GetEnvironmentVariable(variableName);
            return int.TryParse(value, out int parsed) && parsed > 0 ? parsed : fallback;
        }

        private static string Format(Vector3 value)
        {
            return $"{value.x:0.###},{value.y:0.###},{value.z:0.###}";
        }

        private static void Complete(bool success, string message)
        {
            if (completed)
                return;

            completed = true;
            matrixCapture = null;
            SceneManager.sceneLoaded -= OnSceneLoaded;
            EditorApplication.update -= Continue;
            EditorApplication.playModeStateChanged -= ExitBatchAfterPlayMode;
            pendingBatchExitCode = success ? 0 : 1;
            Debug.Log((success ? "[MobileVisualQualityPlayModeCapture] result=Passed " : "[MobileVisualQualityPlayModeCapture] result=Failed ") + message);
            EditorApplication.playModeStateChanged += ExitBatchAfterPlayMode;
            EditorApplication.ExitPlaymode();
        }

        private static int pendingBatchExitCode = int.MinValue;

        private static void ExitBatchAfterPlayMode(PlayModeStateChange state)
        {
            if (state != PlayModeStateChange.EnteredEditMode)
                return;

            EditorApplication.playModeStateChanged -= ExitBatchAfterPlayMode;
            if ((Application.isBatchMode || exitEditorAfterCapture) &&
                pendingBatchExitCode != int.MinValue)
                EditorApplication.Exit(pendingBatchExitCode);
        }
    }
}
