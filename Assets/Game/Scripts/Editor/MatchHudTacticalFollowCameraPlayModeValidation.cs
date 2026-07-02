using System;
using System.Globalization;
using System.IO;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Game.UI.Contracts;
using Game.UI.Shell.Contracts.Ecs;
using Game.Components;
using Game.Configs;
using Game.UI.Runtime;
using Game.Runtime;
using Game.Composition;

namespace Game.Editor
{
    public static class MatchHudTacticalFollowCameraPlayModeValidation
    {
        private const string MenuScenePath = "Assets/Game/Scenes/Menu.unity";
        private const string RuntimeUiConfigPath = "Assets/Game/Data/UI/RuntimeUiConfig.asset";
        private const string DefaultArtifactDirectory = "/private/tmp/warline-tactical-follow-camera-playmode";
        private const int DefaultCaptureWidth = 1280;
        private const int DefaultCaptureHeight = 720;
        private const float CameraMoveThreshold = 0.75f;

        private static int frameCount;
        private static int deployFrame;
        private static int matchReadyFrame;
        private static int enterClickFrame;
        private static int exitClickFrame;
        private static bool deploySubmitted;
        private static bool matchReady;
        private static bool selectionPrepared;
        private static bool enterClicked;
        private static bool enterObserved;
        private static bool exitClicked;
        private static bool exitObserved;
        private static bool completed;
        private static double startedAt;
        private static Vector3 startCameraPosition;
        private static Vector3 followCameraPosition;
        private static Entity followedEntity;
        private static string artifactDirectory;
        private static string enterCapturePath;
        private static string exitCapturePath;
        private static int pendingBatchExitCode = int.MinValue;

        public static void RunCameraButtonEnterExitProof()
        {
            try
            {
                RuntimeUiConfig config = AssetDatabase.LoadAssetAtPath<RuntimeUiConfig>(RuntimeUiConfigPath);
                if (config == null)
                    throw new InvalidOperationException($"Missing runtime UI config: {RuntimeUiConfigPath}");

                SetRuntimeUiMode(config, RuntimeUiMode.Canvas);
                EditorUtility.SetDirty(config);
                AssetDatabase.SaveAssets();

                artifactDirectory = Environment.GetEnvironmentVariable("WARLINE_TACTICAL_FOLLOW_CAMERA_PROOF_DIR");
                if (string.IsNullOrWhiteSpace(artifactDirectory))
                    artifactDirectory = DefaultArtifactDirectory;
                Directory.CreateDirectory(artifactDirectory);
                bool graphicsCaptureAvailable = SystemInfo.graphicsDeviceType != GraphicsDeviceType.Null;
                enterCapturePath = Path.Combine(
                    artifactDirectory,
                    graphicsCaptureAvailable ? "camera_follow_enter.png" : "camera_follow_enter_nographics.txt");
                exitCapturePath = Path.Combine(
                    artifactDirectory,
                    graphicsCaptureAvailable ? "camera_follow_exit.png" : "camera_follow_exit_nographics.txt");
                DeleteIfExists(enterCapturePath);
                DeleteIfExists(exitCapturePath);

                EditorSceneManager.OpenScene(MenuScenePath, OpenSceneMode.Single);
                frameCount = 0;
                deployFrame = 0;
                matchReadyFrame = 0;
                enterClickFrame = 0;
                exitClickFrame = 0;
                deploySubmitted = false;
                matchReady = false;
                selectionPrepared = false;
                enterClicked = false;
                enterObserved = false;
                exitClicked = false;
                exitObserved = false;
                completed = false;
                followedEntity = Entity.Null;
                pendingBatchExitCode = int.MinValue;
                startedAt = EditorApplication.timeSinceStartup;

                EditorApplication.playModeStateChanged -= ExitBatchAfterPlayMode;
                EditorApplication.update -= Continue;
                EditorApplication.update += Continue;
                EditorApplication.EnterPlaymode();
            }
            catch (Exception exception)
            {
                Debug.LogError($"[MatchHudTacticalFollowCameraPlayModeValidation] result=Failed\n{exception}");
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

                if (EditorApplication.timeSinceStartup - startedAt > 180d)
                {
                    Complete(false, $"Timed out frame={frameCount} scene={SceneManager.GetActiveScene().name} deploy={deploySubmitted} matchReady={matchReady} selectionPrepared={selectionPrepared} enterClicked={enterClicked} enterObserved={enterObserved} exitClicked={exitClicked} exitObserved={exitObserved}");
                    return;
                }

                if (frameCount < 45)
                    return;

                if (!deploySubmitted)
                {
                    MenuBootstrapView bootstrap = UnityEngine.Object.FindAnyObjectByType<MenuBootstrapView>(FindObjectsInactive.Include);
                    if (bootstrap == null)
                    {
                        Complete(false, "Menu scene is missing MenuBootstrapView.");
                        return;
                    }

                    bootstrap.ApplyRuntimeUiMode();
                    if (bootstrap.UiMode != RuntimeUiMode.Canvas)
                    {
                        Complete(false, "Runtime UI mode is not Canvas.");
                        return;
                    }

                    Button deployButton = FindDeployButton();
                    if (deployButton == null)
                        return;

                    deployButton.onClick.Invoke();
                    deploySubmitted = true;
                    deployFrame = frameCount;
                    return;
                }

                MatchSceneView matchScene = UnityEngine.Object.FindAnyObjectByType<MatchSceneView>(FindObjectsInactive.Exclude);
                if (matchScene == null || !SceneManager.GetSceneByName("Match").isLoaded)
                    return;

                if (!matchReady)
                {
                    if (!matchScene.GameplayStartComplete && frameCount - deployFrame < 420)
                        return;

                    matchReady = true;
                    matchReadyFrame = frameCount;
                    return;
                }

                if (!selectionPrepared)
                {
                    if (!IsMatchIntroComplete(out string introStatus))
                    {
                        if (frameCount - matchReadyFrame < 600)
                            return;

                        Complete(false, $"Match intro input did not unlock before CameraButton proof. {introStatus}");
                        return;
                    }

                    if (!TryPrepareSelectedUnit(matchScene, out string prepareError))
                    {
                        if (frameCount - matchReadyFrame < 240)
                            return;

                        Complete(false, prepareError);
                        return;
                    }

                    selectionPrepared = true;
                    startCameraPosition = matchScene.WorldCamera != null ? matchScene.WorldCamera.transform.position : Vector3.zero;
                    return;
                }

                MatchHudSelectionPanelView panel = UnityEngine.Object.FindAnyObjectByType<MatchHudSelectionPanelView>(FindObjectsInactive.Include);
                Button cameraButton = FindCameraButton(panel);
                if (panel == null || cameraButton == null)
                {
                    Complete(false, "Match HUD CameraButton is not present after match HUD binding.");
                    return;
                }

                if (!enterClicked)
                {
                    if (!EnsureDirectSelectedUnit(matchScene, out string directSelectionError))
                    {
                        Complete(false, directSelectionError);
                        return;
                    }

                    panel.ShowSelection();
                    if (matchScene.MatchBootstrap.SelectionUiCommand == null)
                    {
                        Complete(false, "Match bootstrap SelectionUiCommand is missing before CameraButton click.");
                        return;
                    }

                    panel.BindCameraAction(() => matchScene.MatchBootstrap.SelectionUiCommand.RequestToggleTacticalFollowCameraMode());
                    panel.SetCameraActionEnabled(true);
                    if (!IsCameraActionAvailable(out string actionStatus))
                    {
                        if (frameCount - matchReadyFrame < 900)
                            return;

                        Complete(false, $"CameraButton read model did not become available before click. {actionStatus}");
                        return;
                    }

                    cameraButton.onClick.Invoke();
                    enterClicked = true;
                    enterClickFrame = frameCount;
                    return;
                }

                if (!enterObserved)
                {
                    if (!TryReadTacticalFollowMode(out TacticalFollowCameraModeComponent mode))
                    {
                        if (frameCount - enterClickFrame < 120)
                            return;

                        Complete(false, "CameraButton click did not create tactical follow mode state.");
                        return;
                    }

                    if (mode.Enabled == 0)
                    {
                        if (frameCount - enterClickFrame < 120)
                            return;

                        Complete(false, $"CameraButton click did not enable tactical follow mode. {DescribeFollowDiagnostics()}");
                        return;
                    }

                    if (mode.PanInputLocked == 0)
                    {
                        Complete(false, "Tactical follow mode enabled without locking pan input.");
                        return;
                    }

                    if (matchScene.WorldCamera == null)
                    {
                        Complete(false, "Match scene has no world camera for tactical follow proof.");
                        return;
                    }

                    if (frameCount - enterClickFrame < 45)
                        return;

                    followCameraPosition = matchScene.WorldCamera.transform.position;
                    if ((followCameraPosition - startCameraPosition).sqrMagnitude < CameraMoveThreshold * CameraMoveThreshold)
                    {
                        if (frameCount - enterClickFrame < 150)
                            return;

                        Complete(false, $"Camera did not move enough after follow enter. start={Format(startCameraPosition)} current={Format(followCameraPosition)}");
                        return;
                    }

                    if (!TryRenderCamera(matchScene.WorldCamera, enterCapturePath, out string enterRenderError))
                    {
                        Complete(false, enterRenderError);
                        return;
                    }

                    enterObserved = true;
                    return;
                }

                if (!exitClicked)
                {
                    cameraButton.onClick.Invoke();
                    exitClicked = true;
                    exitClickFrame = frameCount;
                    return;
                }

                if (!exitObserved)
                {
                    if (!TryReadTacticalFollowMode(out TacticalFollowCameraModeComponent mode))
                    {
                        if (frameCount - exitClickFrame < 120)
                            return;

                        Complete(false, "Tactical follow mode state disappeared before exit could be observed.");
                        return;
                    }

                    if (mode.Enabled != 0 || mode.PanInputLocked != 0)
                    {
                        if (frameCount - exitClickFrame < 150)
                            return;

                        Complete(false, $"CameraButton exit did not clear follow mode. enabled={mode.Enabled} panLocked={mode.PanInputLocked}");
                        return;
                    }

                    if (frameCount - exitClickFrame < 45)
                        return;

                    if (!TryRenderCamera(matchScene.WorldCamera, exitCapturePath, out string exitRenderError))
                    {
                        Complete(false, exitRenderError);
                        return;
                    }

                    exitObserved = true;
                    Complete(true, $"followedEntity={followedEntity} start={Format(startCameraPosition)} follow={Format(followCameraPosition)} end={Format(matchScene.WorldCamera.transform.position)} enterCapture={enterCapturePath} exitCapture={exitCapturePath}");
                }
            }
            catch (Exception exception)
            {
                Complete(false, exception.ToString());
            }
        }

        private static bool TryPrepareSelectedUnit(MatchSceneView matchScene, out string error)
        {
            error = string.Empty;
            World world = World.DefaultGameObjectInjectionWorld;
            if (world == null || !world.IsCreated)
            {
                error = "Default ECS world is not created.";
                return false;
            }

            EntityManager em = world.EntityManager;
            if (!TryFindPlayerUnit(em, out Entity unit))
            {
                error = "No player unit with LocalTransform/Faction was found for CameraButton PlayMode proof.";
                return false;
            }

            followedEntity = unit;
            if (!em.HasComponent<SelectedUnitTag>(unit))
                em.AddComponent<SelectedUnitTag>(unit);
            MatchHudSelectionPanelView panel = UnityEngine.Object.FindAnyObjectByType<MatchHudSelectionPanelView>(FindObjectsInactive.Include);
            if (panel == null)
            {
                error = "MatchHudSelectionPanelView was not found.";
                return false;
            }

            panel.ShowSelection();
            panel.SetSelectionVisible(true, null);
            panel.SetCameraActionEnabled(true);
            panel.SetCameraActionSelected(false);
            return true;
        }

        private static bool EnsureDirectSelectedUnit(MatchSceneView matchScene, out string error)
        {
            error = string.Empty;
            World world = World.DefaultGameObjectInjectionWorld;
            if (world == null || !world.IsCreated)
            {
                error = "Default ECS world is not created while arming CameraButton proof selection.";
                return false;
            }

            EntityManager em = world.EntityManager;
            if (followedEntity == Entity.Null ||
                !em.Exists(followedEntity) ||
                em.GetComponentData<Faction>(followedEntity).Id != FactionIdentity.PlayerFactionId)
            {
                if (!TryFindPlayerUnit(em, out followedEntity))
                {
                    error = "Could not resolve a player unit immediately before CameraButton click.";
                    return false;
                }
            }

            if (!em.HasComponent<SelectedUnitTag>(followedEntity))
                em.AddComponent<SelectedUnitTag>(followedEntity);

            return true;
        }

        private static bool TryFindPlayerUnit(EntityManager em, out Entity unit)
        {
            unit = Entity.Null;
            using EntityQuery query = em.CreateEntityQuery(
                ComponentType.ReadOnly<Faction>(),
                ComponentType.ReadOnly<UnitGrid>(),
                ComponentType.ReadOnly<UnitFootprint>(),
                ComponentType.ReadOnly<LocalTransform>());
            using NativeArray<Entity> entities = query.ToEntityArray(Allocator.Temp);
            for (int i = 0; i < entities.Length; i++)
            {
                Entity entity = entities[i];
                if (em.GetComponentData<Faction>(entity).Id != FactionIdentity.PlayerFactionId)
                    continue;

                unit = entity;
                return true;
            }

            return false;
        }

        private static bool TryReadTacticalFollowMode(out TacticalFollowCameraModeComponent mode)
        {
            mode = default;
            World world = World.DefaultGameObjectInjectionWorld;
            if (world == null || !world.IsCreated)
                return false;

            EntityManager em = world.EntityManager;
            using EntityQuery query = em.CreateEntityQuery(ComponentType.ReadOnly<TacticalFollowCameraModeComponent>());
            if (query.IsEmptyIgnoreFilter)
                return false;

            mode = em.GetComponentData<TacticalFollowCameraModeComponent>(query.GetSingletonEntity());
            return true;
        }

        private static bool IsMatchIntroComplete(out string status)
        {
            status = "matchIntro=missing";
            World world = World.DefaultGameObjectInjectionWorld;
            if (world == null || !world.IsCreated)
                return false;

            EntityManager em = world.EntityManager;
            using EntityQuery query = em.CreateEntityQuery(
                ComponentType.ReadOnly<UiShellRootComponent>(),
                ComponentType.ReadOnly<MatchIntroTransitionComponent>());
            if (query.IsEmptyIgnoreFilter)
                return true;

            MatchIntroTransitionComponent state =
                em.GetComponentData<MatchIntroTransitionComponent>(query.GetSingletonEntity());
            status = $"matchIntroState={state.State} inputLocked={state.InputLocked} progress={state.Progress01:0.00}";
            return state.State == MatchIntroTransitionStateKind.Complete && state.InputLocked == 0;
        }

        private static bool IsCameraActionAvailable(out string status)
        {
            status = "cameraReadModel=missing";
            World world = World.DefaultGameObjectInjectionWorld;
            if (world == null || !world.IsCreated)
                return false;

            EntityManager em = world.EntityManager;
            using EntityQuery query = em.CreateEntityQuery(ComponentType.ReadOnly<TacticalFollowCameraUiReadModelComponent>());
            if (query.IsEmptyIgnoreFilter)
                return false;

            TacticalFollowCameraUiReadModelComponent readModel =
                em.GetComponentData<TacticalFollowCameraUiReadModelComponent>(query.GetSingletonEntity());
            status = $"cameraReadModel enabled={readModel.Enabled} selected={readModel.Selected} reason={readModel.ReasonCode}";
            return readModel.Enabled != 0;
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

        private static Button FindCameraButton(MatchHudSelectionPanelView panel)
        {
            if (panel != null)
            {
                SerializedObject serializedPanel = new(panel);
                SerializedProperty cameraActionProperty = serializedPanel.FindProperty("cameraAction");
                if (cameraActionProperty?.objectReferenceValue is Button serializedButton)
                    return serializedButton;

                Button[] panelButtons = panel.GetComponentsInChildren<Button>(true);
                for (int i = 0; i < panelButtons.Length; i++)
                {
                    Button candidate = panelButtons[i];
                    if (candidate != null && string.Equals(candidate.gameObject.name, "CameraButton", StringComparison.Ordinal))
                        return candidate;
                }
            }

            Button[] buttons = UnityEngine.Object.FindObjectsByType<Button>(FindObjectsInactive.Include);
            for (int i = 0; i < buttons.Length; i++)
            {
                Button candidate = buttons[i];
                if (candidate != null && string.Equals(candidate.gameObject.name, "CameraButton", StringComparison.Ordinal))
                    return candidate;
            }

            return null;
        }

        private static string DescribeFollowDiagnostics()
        {
            World world = World.DefaultGameObjectInjectionWorld;
            if (world == null || !world.IsCreated)
                return "world=missing";

            EntityManager em = world.EntityManager;
            int selectedCount;
            using (EntityQuery selectedQuery = em.CreateEntityQuery(ComponentType.ReadOnly<SelectedUnitTag>()))
                selectedCount = selectedQuery.CalculateEntityCount();

            int requestCount = 0;
            int lastReason = -1;
            int uiEnabled = -1;
            int uiSelected = -1;
            using (EntityQuery requestQuery = em.CreateEntityQuery(
                       ComponentType.ReadOnly<TacticalFollowCameraRequestQueueComponent>(),
                       ComponentType.ReadOnly<TacticalFollowCameraRequestElement>()))
            {
                if (!requestQuery.IsEmptyIgnoreFilter)
                    requestCount = em.GetBuffer<TacticalFollowCameraRequestElement>(requestQuery.GetSingletonEntity()).Length;
            }

            using (EntityQuery readModelQuery = em.CreateEntityQuery(ComponentType.ReadOnly<TacticalFollowCameraUiReadModelComponent>()))
            {
                if (!readModelQuery.IsEmptyIgnoreFilter)
                {
                    TacticalFollowCameraUiReadModelComponent readModel =
                        em.GetComponentData<TacticalFollowCameraUiReadModelComponent>(readModelQuery.GetSingletonEntity());
                    lastReason = readModel.ReasonCode;
                    uiEnabled = readModel.Enabled;
                    uiSelected = readModel.Selected;
                }
            }

            return $"selectedCount={selectedCount} requestCount={requestCount} reason={lastReason} uiEnabled={uiEnabled} uiSelected={uiSelected} followedEntity={followedEntity}";
        }

        private static bool TryRenderCamera(Camera camera, string path, out string error)
        {
            error = string.Empty;
            if (camera == null)
            {
                error = "Cannot render tactical follow proof because world camera is null.";
                return false;
            }

            if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null)
            {
                File.WriteAllText(
                    path,
                    $"Camera render skipped because Unity is running with a Null graphics device.\n" +
                    $"cameraPosition={Format(camera.transform.position)}\n" +
                    $"cameraRotation={camera.transform.rotation.eulerAngles}\n" +
                    $"fieldOfView={camera.fieldOfView:0.00}\n" +
                    $"orthographic={camera.orthographic}\n");
                return true;
            }

            int width = ResolvePositiveInt("WARLINE_TACTICAL_FOLLOW_CAMERA_CAPTURE_WIDTH", DefaultCaptureWidth);
            int height = ResolvePositiveInt("WARLINE_TACTICAL_FOLLOW_CAMERA_CAPTURE_HEIGHT", DefaultCaptureHeight);
            RenderTexture previousTarget = camera.targetTexture;
            RenderTexture previousActive = RenderTexture.active;
            RenderTexture renderTexture = null;
            Texture2D texture = null;
            try
            {
                renderTexture = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32)
                {
                    name = "Runtime_TacticalFollowCameraProofRenderTexture"
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
                error = $"Failed to render tactical follow proof camera capture path={path}\n{exception}";
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

        private static void SetRuntimeUiMode(RuntimeUiConfig runtimeConfig, RuntimeUiMode mode)
        {
            SerializedObject serialized = new(runtimeConfig);
            SerializedProperty modeProperty = serialized.FindProperty("mode");
            if (modeProperty == null)
                throw new InvalidOperationException("RuntimeUiConfig is missing serialized mode field.");

            modeProperty.enumValueIndex = (int)mode;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static int ResolvePositiveInt(string name, int fallback)
        {
            string configured = Environment.GetEnvironmentVariable(name);
            return int.TryParse(configured, NumberStyles.Integer, CultureInfo.InvariantCulture, out int value) && value > 0
                ? value
                : fallback;
        }

        private static string Format(Vector3 value)
        {
            return $"({value.x:0.00},{value.y:0.00},{value.z:0.00})";
        }

        private static void DeleteIfExists(string path)
        {
            if (File.Exists(path))
                File.Delete(path);
        }

        private static void Complete(bool success, string message)
        {
            if (completed)
                return;

            completed = true;
            EditorApplication.update -= Continue;
            if (success)
                Debug.Log($"[MatchHudTacticalFollowCameraPlayModeValidation] result=Passed {message}");
            else
                Debug.LogError($"[MatchHudTacticalFollowCameraPlayModeValidation] result=Failed {message}");

            int exitCode = success ? 0 : 1;
            if (Application.isBatchMode)
            {
                pendingBatchExitCode = exitCode;
                EditorApplication.playModeStateChanged -= ExitBatchAfterPlayMode;
                EditorApplication.playModeStateChanged += ExitBatchAfterPlayMode;
            }

            if (EditorApplication.isPlaying)
                EditorApplication.ExitPlaymode();
            else if (Application.isBatchMode)
                EditorApplication.Exit(exitCode);
        }

        private static void ExitBatchAfterPlayMode(PlayModeStateChange change)
        {
            if (change != PlayModeStateChange.EnteredEditMode || pendingBatchExitCode == int.MinValue)
                return;

            int exitCode = pendingBatchExitCode;
            pendingBatchExitCode = int.MinValue;
            EditorApplication.playModeStateChanged -= ExitBatchAfterPlayMode;
            EditorApplication.Exit(exitCode);
        }
    }
}
