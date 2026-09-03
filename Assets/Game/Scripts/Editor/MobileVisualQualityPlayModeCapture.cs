using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Game.Authoring;
using Game.Composition;
using Game.Components;
using Game.Configs;
using Game.Runtime;
using Game.UI.Contracts;
using Game.UI.Runtime;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Unity.Rendering;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Unity.Transforms;

namespace Game.Editor
{
    public static class MobileVisualQualityPlayModeCapture
    {
        private const string MenuScenePath = "Assets/Game/Scenes/Menu.unity";
        private const string DemoScenePath = "Assets/Game/Scenes/Demo.unity";
        private const string RuntimeUiConfigPath = "Assets/Game/Data/UI/RuntimeUiConfig.asset";
        private const string CurrentProfilePath = "Assets/Game/Rendering/VisualQualityConfig.asset";
        private const string CandidateProfilePath = "Assets/Game/Rendering/VisualQualityConfig_MobileCandidate.asset";
        private const string DefaultArtifactDirectory = "Design/AgentReports/Captures/MobileVisualQuality";
        private const string CaptureMissionEnvironmentVariable = "WARLINE_MOBILE_VISUAL_CAPTURE_MISSION";
        private const string FirstContactMissionId = "saga.ch01.m01.first_contact";
        private const string EstablishBaseMissionId = "saga.ch01.m02.establish_base";
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
        private static bool captureProgressPrepared;
        private static string captureMissionId;
        private static string captureProgressDirectory;
        private static bool captureTutorialCueProof;
        private static int tutorialCueResizeFrame;
        private static int tutorialCueScreenshotFrame;
        private static string tutorialCueScreenshotPath;
        private static int tutorialUiCueScreenshotFrame;
        private static string tutorialUiCueScreenshotPath;
        private static int tutorialBuildDrawerOpenFrame;
        private static int tutorialBuildDrawerScreenshotFrame;
        private static string tutorialBuildDrawerScreenshotPath;
        private static int tutorialBarracksSelectedFrame;
        private static int tutorialPlacementStartFrame;
        private static int tutorialPlacementScreenshotFrame;
        private static string tutorialPlacementScreenshotPath;
        private static bool tutorialNarrativeSkipSubmitted;
        private static bool tutorialNarrativeSkipConfirmed;
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

        [MenuItem("Game/Rendering/Capture Mobile Visual Quality/Mission 1 Grounding Proof")]
        public static void CaptureMissionOneGroundingProof()
        {
            exitEditorAfterCapture = false;
            Run(
                CaptureProfile.Current,
                FirstContactMissionId,
                Path.Combine(Path.GetTempPath(), "warline-m01-grounding-proof"));
        }

        [MenuItem("Game/Rendering/Capture Mobile Visual Quality/Mission 2 Playable Review")]
        public static void CaptureMissionTwoPlayableReview()
        {
            exitEditorAfterCapture = false;
            Run(
                CaptureProfile.Current,
                EstablishBaseMissionId,
                Path.Combine(Path.GetTempPath(), "warline-m02-playable-review"));
        }

        [MenuItem("Game/Rendering/Capture Mobile Visual Quality/Mission 2 V3 Tutorial Cue Proof")]
        public static void CaptureMissionTwoV3TutorialCueProof()
        {
            exitEditorAfterCapture = false;
            Run(
                CaptureProfile.Current,
                EstablishBaseMissionId,
                Path.Combine(Path.GetTempPath(), "warline-m02-v3-tutorial-cue-proof"),
                tutorialCueProof: true);
        }

        public static void CaptureMissionTwoV3TutorialCueProofFromCommandLine()
        {
            exitEditorAfterCapture = true;
            Run(
                CaptureProfile.Current,
                EstablishBaseMissionId,
                Path.Combine(Path.GetTempPath(), "warline-m02-v3-tutorial-cue-proof"),
                tutorialCueProof: true);
        }

        public static void CaptureFromEnvironment()
        {
            exitEditorAfterCapture = true;
            string profile = Environment.GetEnvironmentVariable("WARLINE_MOBILE_VISUAL_CAPTURE_PROFILE");
            Run(string.Equals(profile, "candidate", StringComparison.OrdinalIgnoreCase)
                ? CaptureProfile.Candidate
                : CaptureProfile.Current);
        }

        public static void CaptureDemoReferenceFromEnvironment()
        {
            try
            {
                string directory = Environment.GetEnvironmentVariable("WARLINE_DEMO_VISUAL_CAPTURE_DIR");
                if (string.IsNullOrWhiteSpace(directory))
                    directory = Path.Combine(DefaultArtifactDirectory, "DemoReference");
                Directory.CreateDirectory(directory);

                EditorSceneManager.OpenScene(DemoScenePath, OpenSceneMode.Single);
                Camera camera = UnityEngine.Object.FindAnyObjectByType<Camera>(FindObjectsInactive.Exclude);
                if (camera == null)
                    throw new InvalidOperationException($"Demo scene is missing a camera: {DemoScenePath}");

                string imagePath = Path.Combine(directory, "demo_camera.png");
                if (!TryRenderCamera(
                        camera,
                        imagePath,
                        ResolvePositiveInt("WARLINE_MOBILE_VISUAL_CAPTURE_WIDTH", DefaultCaptureWidth),
                        ResolvePositiveInt("WARLINE_MOBILE_VISUAL_CAPTURE_HEIGHT", DefaultCaptureHeight),
                        true,
                        out string error))
                    throw new InvalidOperationException(error);

                WriteEnvironmentDiagnostics(camera, Path.Combine(directory, "demo_environment.txt"));
                CaptureDemoDirtRoadReference(camera, directory);
                Debug.Log($"[MobileVisualQualityPlayModeCapture] result=Passed demo={imagePath}");
                EditorApplication.Exit(0);
            }
            catch (Exception exception)
            {
                Debug.LogError($"[MobileVisualQualityPlayModeCapture] result=Failed demo\n{exception}");
                EditorApplication.Exit(1);
            }
        }

        private static void CaptureDemoDirtRoadReference(Camera sourceCamera, string directory)
        {
            MeshFilter[] filters = UnityEngine.Object.FindObjectsByType<MeshFilter>(
                FindObjectsInactive.Exclude,
                FindObjectsSortMode.InstanceID);
            MeshFilter selected = null;
            float selectedDistance = float.MaxValue;
            for (int i = 0; i < filters.Length; i++)
            {
                MeshFilter filter = filters[i];
                if (filter.sharedMesh == null || filter.sharedMesh.name != "SM_Env_DirtRoad_Straight_01")
                    continue;

                float distance = filter.transform.position.sqrMagnitude;
                if (distance >= selectedDistance)
                    continue;
                selected = filter;
                selectedDistance = distance;
            }

            if (selected == null || !selected.TryGetComponent(out MeshRenderer renderer))
                throw new InvalidOperationException("Demo scene has no rendered straight dirt-road reference.");

            Camera proofCamera = UnityEngine.Object.Instantiate(sourceCamera);
            proofCamera.name = "DemoDirtRoadProofCamera";
            proofCamera.transform.position = renderer.bounds.center + new Vector3(-10f, 22f, -16f);
            proofCamera.transform.LookAt(renderer.bounds.center);
            proofCamera.fieldOfView = 38f;
            proofCamera.nearClipPlane = 0.1f;
            proofCamera.farClipPlane = 500f;

            try
            {
                string path = Path.Combine(directory, "demo_dirt_road.png");
                if (!TryRenderCamera(
                        proofCamera,
                        path,
                        ResolvePositiveInt("WARLINE_MOBILE_VISUAL_CAPTURE_WIDTH", DefaultCaptureWidth),
                        ResolvePositiveInt("WARLINE_MOBILE_VISUAL_CAPTURE_HEIGHT", DefaultCaptureHeight),
                        true,
                        out string error))
                    throw new InvalidOperationException(error);

                File.WriteAllText(
                    Path.Combine(directory, "demo_dirt_road.txt"),
                    $"object={GetHierarchyPath(selected.transform)}\n" +
                    $"position={Format(selected.transform.position)}\n" +
                    $"mesh={selected.sharedMesh.name}\n" +
                    $"material={renderer.sharedMaterial?.name ?? "null"}\n" +
                    $"boundsCenter={Format(renderer.bounds.center)}\n" +
                    $"boundsSize={Format(renderer.bounds.size)}\n");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(proofCamera.gameObject);
            }
        }

        private static string GetHierarchyPath(Transform transform)
        {
            string path = transform.name;
            while (transform.parent != null)
            {
                transform = transform.parent;
                path = $"{transform.name}/{path}";
            }
            return path;
        }

        private static void Run(
            CaptureProfile profile,
            string forcedMissionId = null,
            string forcedArtifactDirectory = null,
            bool tutorialCueProof = false)
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
                captureTutorialCueProof = tutorialCueProof;
                selectedProfile = LoadProfile(profile);
                captureMissionId = string.IsNullOrWhiteSpace(forcedMissionId)
                    ? ResolveCaptureMissionId()
                    : forcedMissionId;
                bool matrixRequested = string.Equals(
                    Environment.GetEnvironmentVariable(MobileVisualQualityCaptureMatrix.ModeEnvironmentVariable),
                    MobileVisualQualityCaptureMatrix.MatrixMode,
                    StringComparison.OrdinalIgnoreCase);
                artifactDirectory = forcedArtifactDirectory;
                if (string.IsNullOrWhiteSpace(artifactDirectory))
                    artifactDirectory = Environment.GetEnvironmentVariable("WARLINE_MOBILE_VISUAL_CAPTURE_DIR");
                if (string.IsNullOrWhiteSpace(artifactDirectory))
                {
                    artifactDirectory = matrixRequested
                        ? MobileVisualQualityCaptureMatrix.ArtifactDirectory
                        : DefaultArtifactDirectory;
                }
                manifestPath = Path.Combine(artifactDirectory, $"{GetProfileLabel()}_manifest.md");
                // A focused tutorial proof must never be diverted into a stale visual-matrix
                // session inherited by a long-running Editor. It owns a single deterministic
                // M02 frame and its explicit output directory.
                matrixCapture = tutorialCueProof
                    ? null
                    : MobileVisualQualityCaptureMatrix.TryCreateFromEnvironment(
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
                captureProgressPrepared = false;
                captureProgressDirectory = string.Empty;
                tutorialCueResizeFrame = 0;
                tutorialCueScreenshotFrame = 0;
                tutorialUiCueScreenshotFrame = 0;
                tutorialBuildDrawerOpenFrame = 0;
                tutorialBuildDrawerScreenshotFrame = 0;
                tutorialBarracksSelectedFrame = 0;
                tutorialPlacementStartFrame = 0;
                tutorialPlacementScreenshotFrame = 0;
                tutorialNarrativeSkipSubmitted = false;
                tutorialNarrativeSkipConfirmed = false;
                tutorialCueScreenshotPath = Path.Combine(
                    artifactDirectory,
                    "m02_v3_tutorial_ground_cue.png");
                tutorialUiCueScreenshotPath = Path.Combine(
                    artifactDirectory,
                    "m02_v3_tutorial_build_button_cue.png");
                tutorialBuildDrawerScreenshotPath = Path.Combine(
                    artifactDirectory,
                    "m02_v3_tutorial_barracks_cue.png");
                tutorialPlacementScreenshotPath = Path.Combine(
                    artifactDirectory,
                    "m02_v3_barracks_placement_centered.png");
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

                    if (!TrySubmitDeployment())
                        return;
                    deploySubmitted = true;
                    deployFrame = frameCount;
                    SetPhase(CapturePhase.WaitingForMatch);
                    return;
                }

                MatchSceneView matchScene = UnityEngine.Object.FindAnyObjectByType<MatchSceneView>(FindObjectsInactive.Exclude);
                if (matchScene == null || !SceneManager.GetSceneByName("Match").isLoaded)
                    return;
                if (!IsExpectedMissionActive())
                    return;

                ApplyProfileIfNeeded(matchScene);
                if (!matchScene.GameplayStartComplete && frameCount - deployFrame < 420)
                    return;
                if (captureTutorialCueProof && matchReadyFrame == 0 && !TryEnterLiveTutorialHud())
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
                        if (frameCount - matchReadyFrame < WarmupFrames)
                            break;
                        if (captureTutorialCueProof)
                        {
                            TickTutorialCueProof(matchScene, bootstrap);
                            break;
                        }
                        if (frameCount - matchReadyFrame >= WarmupFrames)
                            SetPhase(CapturePhase.SetDay);
                        break;

                    case CapturePhase.SetDay:
                        if (string.IsNullOrEmpty(captureMissionId))
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
                        if (!string.IsNullOrEmpty(captureMissionId))
                        {
                            WriteManifest(matchScene);
                            Complete(true, $"profile={GetProfileLabel()} mission={captureMissionId} manifest={manifestPath}");
                            break;
                        }
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

        private static void TickTutorialCueProof(
            MatchSceneView matchScene,
            MenuBootstrapView bootstrap)
        {
            if (tutorialCueResizeFrame == 0)
            {
                Screen.SetResolution(DefaultCaptureWidth, DefaultCaptureHeight, false);
                tutorialCueResizeFrame = frameCount;
                return;
            }

            if (tutorialCueScreenshotFrame == 0)
            {
                if (frameCount - tutorialCueResizeFrame < 12)
                    return;

                StageV3TutorialCue(matchScene, bootstrap);
                Directory.CreateDirectory(artifactDirectory);
                DeleteIfExists(tutorialCueScreenshotPath);
                ScreenCapture.CaptureScreenshot(tutorialCueScreenshotPath, 1);
                tutorialCueScreenshotFrame = frameCount;
                return;
            }

            if (frameCount - tutorialCueScreenshotFrame < 12)
                return;
            if (!File.Exists(tutorialCueScreenshotPath))
                throw new FileNotFoundException(
                    "The V3 tutorial cue screenshot was not written.",
                    tutorialCueScreenshotPath);

            if (tutorialUiCueScreenshotFrame == 0)
            {
                StageV3TutorialUiCue(matchScene, bootstrap, 4);
                DeleteIfExists(tutorialUiCueScreenshotPath);
                ScreenCapture.CaptureScreenshot(tutorialUiCueScreenshotPath, 1);
                tutorialUiCueScreenshotFrame = frameCount;
                return;
            }

            if (frameCount - tutorialUiCueScreenshotFrame < 12)
                return;
            if (!File.Exists(tutorialUiCueScreenshotPath))
                throw new FileNotFoundException(
                    "The V3 build-button cue screenshot was not written.",
                    tutorialUiCueScreenshotPath);

            if (tutorialBuildDrawerOpenFrame == 0)
            {
                if (!TryExecuteTutorialUiSurface(matchScene, bootstrap, 4))
                    throw new InvalidOperationException("The live M02 Build button did not open the build drawer.");
                tutorialBuildDrawerOpenFrame = frameCount;
                return;
            }

            if (frameCount - tutorialBuildDrawerOpenFrame < 24)
                return;

            if (tutorialBuildDrawerScreenshotFrame == 0)
            {
                StageV3TutorialUiCue(matchScene, bootstrap, 1);
                DeleteIfExists(tutorialBuildDrawerScreenshotPath);
                ScreenCapture.CaptureScreenshot(tutorialBuildDrawerScreenshotPath, 1);
                tutorialBuildDrawerScreenshotFrame = frameCount;
                return;
            }

            if (frameCount - tutorialBuildDrawerScreenshotFrame < 12)
                return;
            if (!File.Exists(tutorialBuildDrawerScreenshotPath))
                throw new FileNotFoundException(
                    "The V3 Barracks cue screenshot was not written.",
                    tutorialBuildDrawerScreenshotPath);

            if (tutorialBarracksSelectedFrame == 0)
            {
                if (!TryExecuteTutorialUiSurface(matchScene, bootstrap, 1))
                    throw new InvalidOperationException("The live M02 Barracks card did not become selected.");
                tutorialBarracksSelectedFrame = frameCount;
                return;
            }

            if (frameCount - tutorialBarracksSelectedFrame < 24)
                return;

            if (tutorialPlacementStartFrame == 0)
            {
                BuildDrawerView buildDrawer = UnityEngine.Object.FindAnyObjectByType<BuildDrawerView>(
                    FindObjectsInactive.Exclude);
                Button placeButton = buildDrawer != null ? buildDrawer.BuildButton : null;
                if (placeButton == null || !placeButton.IsActive() || !placeButton.IsInteractable())
                    throw new InvalidOperationException("The selected M02 Barracks did not enable the green Place button.");
                placeButton.onClick.Invoke();
                tutorialPlacementStartFrame = frameCount;
                return;
            }

            if (frameCount - tutorialPlacementStartFrame < ZoomSettleFrames)
                return;

            if (tutorialPlacementScreenshotFrame == 0)
            {
                BuildDrawerView buildDrawer = UnityEngine.Object.FindAnyObjectByType<BuildDrawerView>(
                    FindObjectsInactive.Include);
                if (buildDrawer != null && buildDrawer.IsOpen)
                    throw new InvalidOperationException("The M02 build drawer remained open after pressing Place.");
                DeleteIfExists(tutorialPlacementScreenshotPath);
                ScreenCapture.CaptureScreenshot(tutorialPlacementScreenshotPath, 1);
                tutorialPlacementScreenshotFrame = frameCount;
                return;
            }

            if (frameCount - tutorialPlacementScreenshotFrame < 12)
                return;
            if (!File.Exists(tutorialPlacementScreenshotPath))
                throw new FileNotFoundException(
                    "The centered M02 Barracks placement screenshot was not written.",
                    tutorialPlacementScreenshotPath);

            Complete(
                true,
                $"mission={captureMissionId} tutorialCue={tutorialCueScreenshotPath} " +
                $"buttonCue={tutorialUiCueScreenshotPath} barracksCue={tutorialBuildDrawerScreenshotPath} " +
                $"placement={tutorialPlacementScreenshotPath}");
        }

        private static bool TryEnterLiveTutorialHud()
        {
            NarrativeSequenceView narrative = UnityEngine.Object.FindAnyObjectByType<NarrativeSequenceView>(
                FindObjectsInactive.Include);
            if (narrative != null && IsCanvasGroupVisible(narrative, "rootGroup"))
            {
                NarrativeSkipConfirmationView confirmation = narrative.SkipConfirmationView;
                if (confirmation != null && IsCanvasGroupVisible(confirmation, "group"))
                {
                    if (!tutorialNarrativeSkipConfirmed && TryInvokeButton(confirmation.transform, "ConfirmButton"))
                        tutorialNarrativeSkipConfirmed = true;
                    return false;
                }

                if (!tutorialNarrativeSkipSubmitted &&
                    TryInvokeButton(narrative.PlaybackControlsView?.transform, "SkipButton"))
                {
                    tutorialNarrativeSkipSubmitted = true;
                }
                return false;
            }

            if (UiShellRuntimeGateway.TryReadMissionHudRestrictions(
                    out UiMissionHudRestrictionsModel restrictions) &&
                restrictions.CinematicInteractionLocked)
            {
                return false;
            }

            MatchOverlayCommandControlsView controls =
                UnityEngine.Object.FindAnyObjectByType<MatchOverlayCommandControlsView>(FindObjectsInactive.Exclude);
            return controls != null && controls.gameObject.activeInHierarchy;
        }

        private static bool IsCanvasGroupVisible(object owner, string fieldName)
        {
            if (owner == null)
                return false;

            FieldInfo field = owner.GetType().GetField(
                fieldName,
                BindingFlags.Instance | BindingFlags.NonPublic);
            CanvasGroup group = field?.GetValue(owner) as CanvasGroup;
            return group != null && group.gameObject.activeInHierarchy &&
                   group.alpha > 0.5f && group.interactable;
        }

        private static bool TryInvokeButton(Transform root, string buttonName)
        {
            if (root == null)
                return false;

            Button[] buttons = root.GetComponentsInChildren<Button>(true);
            for (int i = 0; i < buttons.Length; i++)
            {
                Button button = buttons[i];
                if (button == null ||
                    !string.Equals(button.name, buttonName, StringComparison.Ordinal) ||
                    !button.gameObject.activeInHierarchy || !button.interactable)
                {
                    continue;
                }

                button.onClick.Invoke();
                return true;
            }

            return false;
        }

        private static void StageV3TutorialCue(
            MatchSceneView matchScene,
            MenuBootstrapView bootstrap)
        {
            if (matchScene == null || matchScene.WorldCamera == null)
                throw new InvalidOperationException("The M02 tutorial proof requires the live Match world camera.");
            if (bootstrap == null)
                throw new InvalidOperationException("The M02 tutorial proof requires the live Menu bootstrap.");

            ResolveTutorialProofBindings(
                bootstrap,
                out MainMenuPlayUI mainMenu,
                out object helper,
                out object highlightPresentation);

            Camera camera = matchScene.WorldCamera;
            Ray ray = camera.ViewportPointToRay(new Vector3(0.56f, 0.53f, 0f));
            Plane ground = new(Vector3.up, Vector3.zero);
            Vector3 target;
            if (ground.Raycast(ray, out float distance))
                target = ray.GetPoint(distance);
            else
                target = camera.transform.position + camera.transform.forward * 80f;

            var model = new UiAssistantHighlightModel(
                0xF3000001u,
                true,
                9301,
                9302,
                2,
                1,
                target.x,
                target.y,
                target.z,
                1f);

            MethodInfo applyReadModel = helper.GetType().GetMethod(
                "ApplyHighlightReadModel",
                BindingFlags.Instance | BindingFlags.Public);
            MethodInfo beginShowMe = highlightPresentation?.GetType().GetMethod(
                "BeginPendingShowMe",
                BindingFlags.Instance | BindingFlags.Public);
            MethodInfo tick = highlightPresentation?.GetType().GetMethod(
                "Tick",
                BindingFlags.Instance | BindingFlags.Public);
            if (applyReadModel == null || highlightPresentation == null ||
                beginShowMe == null || tick == null)
                throw new MissingMethodException("The live ARIA tutorial cue methods are unavailable.");

            mainMenu.BindGuidanceWorldCamera(camera);
            HoldTutorialProofHighlight(mainMenu);
            applyReadModel.Invoke(helper, new object[] { model });
            mainMenu.AcknowledgeMatchHudGuidedCommandMode(
                Game.Tactical.Contracts.TacticalCommandMode.Move);
            beginShowMe.Invoke(highlightPresentation, new object[] { (byte)2, (byte)1 });
            tick.Invoke(highlightPresentation, null);

            // Keep the live shell enabled through the captured frame. Disabling its owner
            // here stopped Match presentation maintenance, producing a black battlefield
            // behind an otherwise valid HUD and preventing the proof from completing.
            Canvas.ForceUpdateCanvases();
        }

        private static void StageV3TutorialUiCue(
            MatchSceneView matchScene,
            MenuBootstrapView bootstrap,
            byte recommendationKind)
        {
            if (matchScene == null || matchScene.WorldCamera == null)
                throw new InvalidOperationException("The M02 UI cue proof requires the live Match world camera.");

            ResolveTutorialProofBindings(
                bootstrap,
                out MainMenuPlayUI mainMenu,
                out object helper,
                out object highlightPresentation);
            MethodInfo applyReadModel = helper.GetType().GetMethod(
                "ApplyHighlightReadModel",
                BindingFlags.Instance | BindingFlags.Public);
            MethodInfo tick = highlightPresentation.GetType().GetMethod(
                "Tick",
                BindingFlags.Instance | BindingFlags.Public);
            if (applyReadModel == null || tick == null)
                throw new MissingMethodException("The live ARIA UI cue methods are unavailable.");

            var model = new UiAssistantHighlightModel(
                0xF3000010u + recommendationKind,
                true,
                9310 + recommendationKind,
                9320 + recommendationKind,
                recommendationKind,
                4,
                0f,
                0f,
                0f,
                1f);
            mainMenu.BindGuidanceWorldCamera(matchScene.WorldCamera);
            HoldTutorialProofHighlight(mainMenu);
            applyReadModel.Invoke(helper, new object[] { model });
            tick.Invoke(highlightPresentation, null);
            Canvas.ForceUpdateCanvases();
        }

        private static bool TryExecuteTutorialUiSurface(
            MatchSceneView matchScene,
            MenuBootstrapView bootstrap,
            byte recommendationKind)
        {
            ResolveTutorialProofBindings(
                bootstrap,
                out MainMenuPlayUI mainMenu,
                out _,
                out object highlightPresentation);
            MethodInfo execute = highlightPresentation.GetType().GetMethod(
                "TryExecuteUiSurface",
                BindingFlags.Instance | BindingFlags.Public);
            if (execute == null)
                throw new MissingMethodException("The live ARIA UI-surface executor is unavailable.");

            HoldTutorialProofHighlight(mainMenu);
            object result = execute.Invoke(highlightPresentation, new object[] { recommendationKind, (byte)4 });
            Canvas.ForceUpdateCanvases();
            return result is true;
        }

        private static void ResolveTutorialProofBindings(
            MenuBootstrapView bootstrap,
            out MainMenuPlayUI mainMenu,
            out object helper,
            out object highlightPresentation)
        {
            if (bootstrap == null)
                throw new InvalidOperationException("The M02 tutorial proof requires the live Menu bootstrap.");

            FieldInfo compositionField = typeof(MenuBootstrapView).GetField(
                "menuBootstrapSystem",
                BindingFlags.Instance | BindingFlags.NonPublic);
            object composition = compositionField?.GetValue(bootstrap);
            FieldInfo mainMenuField = composition?.GetType().GetField(
                "boundMainMenu",
                BindingFlags.Instance | BindingFlags.NonPublic);
            mainMenu = mainMenuField?.GetValue(composition) as MainMenuPlayUI;
            if (mainMenu == null)
                throw new InvalidOperationException("The live Match HUD MainMenuPlayUI binding is unavailable.");

            FieldInfo helperField = typeof(MainMenuPlayUI).GetField(
                "_matchHudAssistantUiSystem",
                BindingFlags.Instance | BindingFlags.NonPublic);
            helper = helperField?.GetValue(mainMenu);
            FieldInfo highlightPresentationField = helper?.GetType().GetField(
                "_highlightPresentationSystem",
                BindingFlags.Instance | BindingFlags.NonPublic);
            highlightPresentation = highlightPresentationField?.GetValue(helper);
            if (helper == null || highlightPresentation == null)
                throw new InvalidOperationException("The live Match HUD ARIA helper is unavailable.");
        }

        private static void HoldTutorialProofHighlight(MainMenuPlayUI mainMenu)
        {
            FieldInfo nextAssistantRefreshField = typeof(MainMenuPlayUI).GetField(
                "_nextAssistantPanelRefreshTime",
                BindingFlags.Instance | BindingFlags.NonPublic);
            nextAssistantRefreshField?.SetValue(mainMenu, Time.unscaledTime + 8f);
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

            if (!string.IsNullOrEmpty(captureMissionId))
            {
                WriteCenterRayDiagnostics(camera, viewpoint);
                WriteDirtRoadEntityDiagnostics(viewpoint);
                WriteGroundingDiagnostics(viewpoint);
                WriteEnvironmentDiagnostics(
                    camera,
                    Path.Combine(artifactDirectory, $"{GetProfileLabel()}_{viewpoint}_environment.txt"));
            }

            string extension = SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null ? ".txt" : ".png";
            string path = Path.Combine(artifactDirectory, $"{GetProfileLabel()}_{viewpoint}{extension}");
            if (!TryRenderCamera(camera, path, out string error))
                throw new InvalidOperationException(error);
        }

        private static void WriteDirtRoadEntityDiagnostics(string viewpoint)
        {
            World world = World.DefaultGameObjectInjectionWorld;
            if (world == null || !world.IsCreated)
                return;

            EntityManager entityManager = world.EntityManager;
            using EntityQuery query = entityManager.CreateEntityQuery(
                ComponentType.ReadOnly<MaterialMeshInfo>(),
                ComponentType.ReadOnly<RenderMeshArray>(),
                ComponentType.ReadOnly<WorldRenderBounds>(),
                ComponentType.ReadOnly<LocalToWorld>());
            using NativeArray<Entity> entities = query.ToEntityArray(Allocator.Temp);
            using StreamWriter writer = new(Path.Combine(
                artifactDirectory,
                $"{GetProfileLabel()}_{viewpoint}_dirt_roads.txt"));
            for (int i = 0; i < entities.Length; i++)
            {
                Entity entity = entities[i];
                MaterialMeshInfo meshInfo = entityManager.GetComponentData<MaterialMeshInfo>(entity);
                if (meshInfo.HasMaterialMeshIndexRange)
                    continue;
                RenderMeshArray array = entityManager.GetSharedComponentManaged<RenderMeshArray>(entity);
                Mesh mesh = array.GetMesh(meshInfo);
                if (mesh == null || mesh.name.IndexOf("DirtRoad", StringComparison.Ordinal) < 0)
                    continue;

                LocalToWorld localToWorld = entityManager.GetComponentData<LocalToWorld>(entity);
                float4x4 matrix = localToWorld.Value;
                float3x3 orientation = new(
                    matrix.c0.xyz,
                    matrix.c1.xyz,
                    matrix.c2.xyz);
                float determinant = math.determinant(orientation);
                OperationMapRenderProxySlotComponent slot =
                    entityManager.HasComponent<OperationMapRenderProxySlotComponent>(entity)
                        ? entityManager.GetComponentData<OperationMapRenderProxySlotComponent>(entity)
                        : default;
                string componentTypes = GetRelevantRenderComponentTypes(entityManager, entity);
                writer.WriteLine(
                    $"entity={entity.Index}:{entity.Version} mesh={mesh.name} " +
                    $"material={array.GetMaterial(meshInfo)?.name ?? "null"} " +
                    $"placement={slot.PlacementIndex} part={slot.PartIndex} determinant={determinant:0.######} " +
                    $"worldUp={Format((Vector3)math.normalizesafe(math.mul(orientation, new float3(0f, 1f, 0f))))} " +
                    $"position={Format((Vector3)matrix.c3.xyz)} components={componentTypes}");
            }
        }

        private static void WriteGroundingDiagnostics(string viewpoint)
        {
            World world = World.DefaultGameObjectInjectionWorld;
            if (world == null || !world.IsCreated)
                return;

            EntityManager entityManager = world.EntityManager;
            using StreamWriter writer = new(Path.Combine(
                artifactDirectory,
                $"{GetProfileLabel()}_{viewpoint}_grounding.txt"));

            MapSurfaceAuthoring[] surfaceAuthorings = UnityEngine.Object.FindObjectsByType<MapSurfaceAuthoring>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.InstanceID);
            MapBakeGroupAuthoring[] bakeGroups = UnityEngine.Object.FindObjectsByType<MapBakeGroupAuthoring>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.InstanceID);
            writer.WriteLine($"managedSurfaceAuthorings={surfaceAuthorings.Length} managedBakeGroups={bakeGroups.Length}");

            NativeArray<MapSurfaceSceneOverlay> overlays = default;
            using (EntityQuery surfaceQuery = entityManager.CreateEntityQuery(
                       ComponentType.ReadOnly<MapSurfaceComponent>()))
            {
                using NativeArray<Entity> surfaceEntities = surfaceQuery.ToEntityArray(Allocator.Temp);
                writer.WriteLine($"surfaceEntities={surfaceEntities.Length}");
                if (surfaceEntities.Length == 1 &&
                    entityManager.HasBuffer<MapSurfaceSceneOverlay>(surfaceEntities[0]))
                {
                    DynamicBuffer<MapSurfaceSceneOverlay> buffer =
                        entityManager.GetBuffer<MapSurfaceSceneOverlay>(surfaceEntities[0], true);
                    overlays = buffer.ToNativeArray(Allocator.Temp);
                }
            }
            writer.WriteLine($"sceneOverlays={(overlays.IsCreated ? overlays.Length : 0)}");

            using EntityQuery roadQuery = entityManager.CreateEntityQuery(
                ComponentType.ReadOnly<MaterialMeshInfo>(),
                ComponentType.ReadOnly<RenderMeshArray>(),
                ComponentType.ReadOnly<WorldRenderBounds>());
            using NativeArray<Entity> roadEntities = roadQuery.ToEntityArray(Allocator.Temp);
            using EntityQuery unitQuery = entityManager.CreateEntityQuery(
                ComponentType.ReadOnly<CampaignMissionUnitRoleComponent>(),
                ComponentType.ReadOnly<LocalTransform>(),
                ComponentType.ReadOnly<UnitSurfaceComponent>());
            using NativeArray<Entity> unitEntities = unitQuery.ToEntityArray(Allocator.Temp);
            writer.WriteLine($"missionUnits={unitEntities.Length}");
            for (int i = 0; i < unitEntities.Length; i++)
            {
                Entity unit = unitEntities[i];
                CampaignMissionUnitRoleComponent role =
                    entityManager.GetComponentData<CampaignMissionUnitRoleComponent>(unit);
                LocalTransform transform = entityManager.GetComponentData<LocalTransform>(unit);
                UnitSurfaceComponent surface = entityManager.GetComponentData<UnitSurfaceComponent>(unit);
                float groundOffset = entityManager.HasComponent<UnitGroundOffsetComponent>(unit)
                    ? entityManager.GetComponentData<UnitGroundOffsetComponent>(unit).Value
                    : 0f;
                int health = entityManager.HasComponent<UnitHealth>(unit)
                    ? entityManager.GetComponentData<UnitHealth>(unit).Current
                    : int.MinValue;
                writer.WriteLine(
                    $"unit={unit.Index}:{unit.Version} role={role.MissionRoleId} health={health} " +
                    $"position={Format((Vector3)transform.Position)} sampledHeight={surface.LastSampledHeight:0.######} " +
                    $"groundOffset={groundOffset:0.######} hasSurface={surface.HasSurface} grounded={surface.IsGrounded}");

                if (overlays.IsCreated)
                {
                    for (int overlayIndex = 0; overlayIndex < overlays.Length; overlayIndex++)
                    {
                        MapSurfaceSceneOverlay overlay = overlays[overlayIndex];
                        float3 local = math.mul(math.inverse(overlay.Rotation), transform.Position - overlay.Center);
                        if (math.abs(local.x) > overlay.HalfExtents.x ||
                            math.abs(local.z) > overlay.HalfExtents.y)
                            continue;
                        writer.WriteLine(
                            $"  overlay={overlayIndex} type={overlay.SurfaceType} height={overlay.Height:0.######} " +
                            $"center={Format((Vector3)overlay.Center)} halfExtents={overlay.HalfExtents.x:0.###},{overlay.HalfExtents.y:0.###}");
                    }
                }

                for (int roadIndex = 0; roadIndex < roadEntities.Length; roadIndex++)
                {
                    Entity road = roadEntities[roadIndex];
                    MaterialMeshInfo meshInfo = entityManager.GetComponentData<MaterialMeshInfo>(road);
                    if (meshInfo.HasMaterialMeshIndexRange)
                        continue;
                    RenderMeshArray array = entityManager.GetSharedComponentManaged<RenderMeshArray>(road);
                    Mesh mesh = array.GetMesh(meshInfo);
                    if (mesh == null || mesh.name.IndexOf("DirtRoad", StringComparison.Ordinal) < 0)
                        continue;
                    AABB bounds = entityManager.GetComponentData<WorldRenderBounds>(road).Value;
                    if (math.abs(transform.Position.x - bounds.Center.x) > bounds.Extents.x ||
                        math.abs(transform.Position.z - bounds.Center.z) > bounds.Extents.z)
                        continue;
                    writer.WriteLine(
                        $"  road={road.Index}:{road.Version} mesh={mesh.name} " +
                        $"boundsCenter={Format((Vector3)bounds.Center)} boundsExtents={Format((Vector3)bounds.Extents)} " +
                        $"roadTop={(bounds.Center.y + bounds.Extents.y):0.######}");
                }
            }

            if (overlays.IsCreated)
                overlays.Dispose();
        }

        private static string GetRelevantRenderComponentTypes(EntityManager entityManager, Entity entity)
        {
            using NativeArray<ComponentType> types = entityManager.GetComponentTypes(entity, Allocator.Temp);
            List<string> relevant = new(8);
            for (int i = 0; i < types.Length; i++)
            {
                string name = types[i].GetManagedType()?.Name ?? types[i].ToString();
                if (name.IndexOf("Probe", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    name.IndexOf("WorldToLocal", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    name.IndexOf("SHCoefficient", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    relevant.Add(name);
                }
            }
            return relevant.Count == 0 ? "none" : string.Join(",", relevant);
        }

        private static void WriteCenterRayDiagnostics(Camera camera, string viewpoint)
        {
            World world = World.DefaultGameObjectInjectionWorld;
            if (world == null || !world.IsCreated)
                return;

            EntityManager entityManager = world.EntityManager;
            using EntityQuery query = entityManager.CreateEntityQuery(new EntityQueryDesc
            {
                All = new[]
                {
                    ComponentType.ReadOnly<MaterialMeshInfo>(),
                    ComponentType.ReadOnly<RenderMeshArray>(),
                    ComponentType.ReadOnly<WorldRenderBounds>()
                },
                None = new[]
                {
                    ComponentType.ReadOnly<Disabled>(),
                    ComponentType.ReadOnly<DisableRendering>()
                }
            });

            Ray ray = camera.ViewportPointToRay(new Vector3(0.64f, 0.5f, 0f));
            List<CenterRayHit> hits = new(16);
            using NativeArray<Entity> entities = query.ToEntityArray(Allocator.Temp);
            for (int i = 0; i < entities.Length; i++)
            {
                Entity entity = entities[i];
                AABB aabb = entityManager.GetComponentData<WorldRenderBounds>(entity).Value;
                Bounds bounds = new((Vector3)aabb.Center, (Vector3)(aabb.Extents * 2f));
                if (!bounds.IntersectRay(ray, out float distance) || distance < 0f)
                    continue;

                MaterialMeshInfo meshInfo = entityManager.GetComponentData<MaterialMeshInfo>(entity);
                RenderMeshArray renderMeshArray = entityManager.GetSharedComponentManaged<RenderMeshArray>(entity);
                Mesh mesh = meshInfo.HasMaterialMeshIndexRange ? null : renderMeshArray.GetMesh(meshInfo);
                Material material = meshInfo.HasMaterialMeshIndexRange ? null : renderMeshArray.GetMaterial(meshInfo);
                int partIndex = entityManager.HasComponent<OperationMapRenderProxySlotComponent>(entity)
                    ? entityManager.GetComponentData<OperationMapRenderProxySlotComponent>(entity).PartIndex
                    : -1;
                Vector4 baseColor = entityManager.HasComponent<URPMaterialPropertyBaseColor>(entity)
                    ? entityManager.GetComponentData<URPMaterialPropertyBaseColor>(entity).Value
                    : Vector4.one;
                hits.Add(new CenterRayHit(
                    distance,
                    entity,
                    mesh != null ? mesh.name : "range",
                    material != null ? material.name : "range",
                    partIndex,
                    baseColor,
                    bounds.center,
                    bounds.size));
            }

            hits.Sort(static (a, b) => a.Distance.CompareTo(b.Distance));
            using StreamWriter writer = new(Path.Combine(
                artifactDirectory,
                $"{GetProfileLabel()}_{viewpoint}_center_ray.txt"));
            writer.WriteLine($"mission={captureMissionId}");
            writer.WriteLine($"cameraPosition={Format(camera.transform.position)}");
            writer.WriteLine($"rayOrigin={Format(ray.origin)} rayDirection={Format(ray.direction)}");
            int count = Mathf.Min(hits.Count, 64);
            for (int i = 0; i < count; i++)
            {
                CenterRayHit hit = hits[i];
                writer.WriteLine(
                    $"hit={i} distance={hit.Distance:0.###} entity={hit.Entity.Index}:{hit.Entity.Version} " +
                    $"mesh={hit.MeshName} material={hit.MaterialName} part={hit.PartIndex} " +
                    $"baseColor={hit.BaseColor.x:0.###},{hit.BaseColor.y:0.###},{hit.BaseColor.z:0.###},{hit.BaseColor.w:0.###} " +
                    $"boundsCenter={Format(hit.BoundsCenter)} boundsSize={Format(hit.BoundsSize)}");
            }
        }

        private readonly struct CenterRayHit
        {
            public CenterRayHit(
                float distance,
                Entity entity,
                string meshName,
                string materialName,
                int partIndex,
                Vector4 baseColor,
                Vector3 boundsCenter,
                Vector3 boundsSize)
            {
                Distance = distance;
                Entity = entity;
                MeshName = meshName;
                MaterialName = materialName;
                PartIndex = partIndex;
                BaseColor = baseColor;
                BoundsCenter = boundsCenter;
                BoundsSize = boundsSize;
            }

            public float Distance { get; }
            public Entity Entity { get; }
            public string MeshName { get; }
            public string MaterialName { get; }
            public int PartIndex { get; }
            public Vector4 BaseColor { get; }
            public Vector3 BoundsCenter { get; }
            public Vector3 BoundsSize { get; }
        }

        private static void WriteEnvironmentDiagnostics(Camera camera, string path)
        {
            using StreamWriter writer = new(path);
            writer.WriteLine($"scene={SceneManager.GetActiveScene().path}");
            writer.WriteLine($"camera={camera.name} position={Format(camera.transform.position)} rotation={Format(camera.transform.rotation.eulerAngles)}");
            Component cameraData = FindComponentByFullName(
                camera.gameObject,
                "UnityEngine.Rendering.Universal.UniversalAdditionalCameraData");
            if (cameraData != null)
            {
                writer.WriteLine(
                    $"cameraPostProcessing={GetReflectedValue(cameraData, "renderPostProcessing")} " +
                    $"cameraAntialiasing={GetReflectedValue(cameraData, "antialiasing")} " +
                    $"cameraVolumeLayerMask={GetReflectedValue(cameraData, "volumeLayerMask")}");
            }

            writer.WriteLine(
                $"renderSettings fog={RenderSettings.fog} fogColor={Format(RenderSettings.fogColor)} " +
                $"fogDensity={RenderSettings.fogDensity:0.######} ambientMode={RenderSettings.ambientMode} " +
                $"ambientSky={Format(RenderSettings.ambientSkyColor)} ambientEquator={Format(RenderSettings.ambientEquatorColor)} " +
                $"ambientGround={Format(RenderSettings.ambientGroundColor)} ambientIntensity={RenderSettings.ambientIntensity:0.###} " +
                $"reflectionIntensity={RenderSettings.reflectionIntensity:0.###}");
            SphericalHarmonicsL2 ambientProbe = RenderSettings.ambientProbe;
            for (int rgb = 0; rgb < 3; rgb++)
            {
                writer.Write($"ambientProbe[{rgb}]=");
                for (int coefficient = 0; coefficient < 9; coefficient++)
                {
                    if (coefficient > 0)
                        writer.Write(',');
                    writer.Write(ambientProbe[rgb, coefficient].ToString("0.######"));
                }
                writer.WriteLine();
            }

            Light[] lights = UnityEngine.Object.FindObjectsByType<Light>(FindObjectsInactive.Exclude, FindObjectsSortMode.InstanceID);
            for (int i = 0; i < lights.Length; i++)
            {
                Light light = lights[i];
                writer.WriteLine(
                    $"light={light.name} type={light.type} color={Format(light.color)} intensity={light.intensity:0.###} " +
                    $"shadowStrength={light.shadowStrength:0.###} rotation={Format(light.transform.rotation.eulerAngles)}");
            }

            MonoBehaviour[] behaviours = UnityEngine.Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Exclude, FindObjectsSortMode.InstanceID);
            for (int i = 0; i < behaviours.Length; i++)
            {
                MonoBehaviour volume = behaviours[i];
                if (volume == null || volume.GetType().FullName != "UnityEngine.Rendering.Volume")
                    continue;

                bool instantiated = string.Equals(
                    InvokeReflectedMethod(volume, "HasInstantiatedProfile"),
                    bool.TrueString,
                    StringComparison.OrdinalIgnoreCase);
                UnityEngine.Object activeProfile = GetReflectedMember(
                    volume,
                    instantiated ? "profile" : "sharedProfile") as UnityEngine.Object;
                writer.WriteLine(
                    $"volume={volume.name} global={GetReflectedValue(volume, "isGlobal")} " +
                    $"priority={GetReflectedValue(volume, "priority")} weight={GetReflectedValue(volume, "weight")} " +
                    $"instantiated={instantiated} profile={activeProfile?.name ?? "null"} " +
                    $"profilePath={AssetDatabase.GetAssetPath(activeProfile)}");
            }
        }

        private static Component FindComponentByFullName(GameObject gameObject, string fullName)
        {
            Component[] components = gameObject.GetComponents<Component>();
            for (int i = 0; i < components.Length; i++)
            {
                Component component = components[i];
                if (component != null && component.GetType().FullName == fullName)
                    return component;
            }

            return null;
        }

        private static object GetReflectedMember(object target, string memberName)
        {
            if (target == null)
                return null;

            Type type = target.GetType();
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
            PropertyInfo property = type.GetProperty(memberName, flags);
            if (property != null)
                return property.GetValue(target);
            return type.GetField(memberName, flags)?.GetValue(target);
        }

        private static string GetReflectedValue(object target, string memberName)
        {
            object value = GetReflectedMember(target, memberName);
            if (value is LayerMask layerMask)
                return layerMask.value.ToString();
            return value?.ToString() ?? "null";
        }

        private static string InvokeReflectedMethod(object target, string methodName)
        {
            object value = target?.GetType()
                .GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                ?.Invoke(target, null);
            return value?.ToString() ?? "null";
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

        private static bool TrySubmitDeployment()
        {
            if (!string.IsNullOrEmpty(captureMissionId))
            {
                if (!PrepareCaptureProgress())
                    return false;
                if (!UiShellRuntimeGateway.TryReadCampaignOperations(out UiCampaignOperationsModel campaign) ||
                    !campaign.IsValid)
                    return false;

                if (!string.Equals(campaign.SelectedMission.MissionId, captureMissionId, StringComparison.Ordinal))
                {
                    UiShellRuntimeGateway.TryEnqueueCampaignMissionAction(
                        UiCampaignMissionActionKind.Select,
                        captureMissionId);
                    return false;
                }

                return UiShellRuntimeGateway.TryEnqueueCampaignMissionAction(
                    UiCampaignMissionActionKind.Deploy,
                    captureMissionId);
            }

            Button deployButton = FindDeployButton();
            if (deployButton == null)
                return false;

            deployButton.onClick.Invoke();
            return true;
        }

        private static bool PrepareCaptureProgress()
        {
            if (!string.Equals(captureMissionId, EstablishBaseMissionId, StringComparison.Ordinal))
                return true;
            if (captureProgressPrepared)
                return true;

            World world = World.DefaultGameObjectInjectionWorld;
            if (world == null || !world.IsCreated)
                return false;

            EntityManager entityManager = world.EntityManager;
            using EntityQuery query = entityManager.CreateEntityQuery(
                ComponentType.ReadOnly<CampaignMissionRootComponent>(),
                ComponentType.ReadOnly<CampaignMissionProgressStoreReferenceComponent>());
            if (query.CalculateEntityCount() != 1)
                return false;

            captureProgressDirectory = Path.Combine(
                Path.GetTempPath(),
                "warline-mobile-visual-capture-progress",
                "m02");
            if (Directory.Exists(captureProgressDirectory))
                Directory.Delete(captureProgressDirectory, true);
            Directory.CreateDirectory(captureProgressDirectory);

            CampaignMissionProgressStore store = new(
                new SaveService(new JsonSaveRepository(captureProgressDirectory)));
            store.EnsureAvailable(EstablishBaseMissionId);
            CampaignMissionProgressStoreReferenceComponent reference = entityManager.GetComponentObject<
                CampaignMissionProgressStoreReferenceComponent>(query.GetSingletonEntity());
            reference.Store = store;
            captureProgressPrepared = true;
            return false;
        }

        private static bool IsExpectedMissionActive()
        {
            if (string.IsNullOrEmpty(captureMissionId))
                return true;

            World world = World.DefaultGameObjectInjectionWorld;
            if (world == null || !world.IsCreated)
                return false;

            EntityManager entityManager = world.EntityManager;
            using EntityQuery query = entityManager.CreateEntityQuery(
                ComponentType.ReadOnly<ActiveOperationMapComponent>());
            if (query.CalculateEntityCount() != 1)
                return false;

            ActiveOperationMapComponent active = query.GetSingleton<ActiveOperationMapComponent>();
            return string.Equals(active.MissionId.ToString(), captureMissionId, StringComparison.Ordinal);
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
                $"- Mission: `{(string.IsNullOrEmpty(captureMissionId) ? "generic-deployment" : captureMissionId)}`\n" +
                $"- Graphics device: `{SystemInfo.graphicsDeviceType}`\n" +
                $"- Resolution: `{ResolvePositiveInt("WARLINE_MOBILE_VISUAL_CAPTURE_WIDTH", DefaultCaptureWidth)}x{ResolvePositiveInt("WARLINE_MOBILE_VISUAL_CAPTURE_HEIGHT", DefaultCaptureHeight)}`\n" +
                (string.IsNullOrEmpty(captureMissionId)
                    ? $"- Day phase ({DayCaptureHour:0}:00), gameplay zoom: `{profileLabel}_gameplay_zoom.png`\n" +
                      $"- Day phase ({DayCaptureHour:0}:00), max zoom out: `{profileLabel}_max_zoom_out.png`\n" +
                      $"- Dusk phase ({DuskCaptureHour:0}:00): `{profileLabel}_dusk_phase.png`\n" +
                      $"- Night phase ({NightCaptureHour:0}:00): `{profileLabel}_night_phase.png`\n"
                    : $"- Authored mission lighting, gameplay zoom: `{profileLabel}_gameplay_zoom.png`\n" +
                      $"- Authored mission lighting, max zoom out: `{profileLabel}_max_zoom_out.png`\n") +
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

        private static string ResolveCaptureMissionId()
        {
            return ResolveCaptureMissionId(
                Environment.GetEnvironmentVariable(CaptureMissionEnvironmentVariable));
        }

        internal static string ResolveCaptureMissionId(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return string.Empty;
            if (string.Equals(value.Trim(), "m01", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(value.Trim(), FirstContactMissionId, StringComparison.Ordinal))
            {
                return FirstContactMissionId;
            }
            if (string.Equals(value.Trim(), "m02", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(value.Trim(), EstablishBaseMissionId, StringComparison.Ordinal))
            {
                return EstablishBaseMissionId;
            }

            throw new InvalidOperationException(
                $"Unsupported visual capture mission '{value}'. Expected m01, m02, " +
                $"'{FirstContactMissionId}', or '{EstablishBaseMissionId}'.");
        }

        private static string Format(Vector3 value)
        {
            return $"{value.x:0.###},{value.y:0.###},{value.z:0.###}";
        }

        private static string Format(Color value)
        {
            return $"{value.r:0.###},{value.g:0.###},{value.b:0.###},{value.a:0.###}";
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
