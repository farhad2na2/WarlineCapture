using System;
using System.Globalization;
using System.IO;
using Game.Components;
using Game.Composition;
using Game.Configs;
using Game.Runtime;
using Game.UI.Contracts;
using Game.UI.Runtime;
using Game.UI.Shell.Contracts.Ecs;
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

namespace Game.Editor
{
    public static class TacticalFollowAttackCinematicPlayModeValidation
    {
        private const string MenuScenePath = "Assets/Game/Scenes/Menu.unity";
        private const string RuntimeUiConfigPath = "Assets/Game/Data/UI/RuntimeUiConfig.asset";
        private const string DefaultArtifactDirectory = "/private/tmp/warline-attack-cinematic-playmode";
        private const int DefaultCaptureWidth = 1280;
        private const int DefaultCaptureHeight = 720;
        private const int TimeoutFrames = 12000;
        private const int DeployWarmupFrames = 45;
        private const int CinematicWaitFrames = 480;

        private static int frameCount;
        private static int deployFrame;
        private static int matchReadyFrame;
        private static int requestFrame;
        private static bool deploySubmitted;
        private static bool matchReady;
        private static bool followArmed;
        private static bool requestsSubmitted;
        private static bool cinematicObserved;
        private static bool launchCaptured;
        private static bool pathCaptured;
        private static bool impactCaptured;
        private static bool flyoverCaptured;
        private static bool returnCaptured;
        private static bool completed;
        private static double startedAt;
        private static Entity sourceEntity;
        private static Entity targetEntity;
        private static float3 launchPosition;
        private static float3 impactPosition;
        private static string artifactDirectory;
        private static int pendingBatchExitCode = int.MinValue;

        public static void RunInMatchAttackCinematicProof()
        {
            try
            {
                RuntimeUiConfig config = AssetDatabase.LoadAssetAtPath<RuntimeUiConfig>(RuntimeUiConfigPath);
                if (config == null)
                    throw new InvalidOperationException($"Missing runtime UI config: {RuntimeUiConfigPath}");

                SetRuntimeUiMode(config, RuntimeUiMode.Canvas);
                EditorUtility.SetDirty(config);
                AssetDatabase.SaveAssets();

                artifactDirectory = Environment.GetEnvironmentVariable("WARLINE_ATTACK_CINEMATIC_PLAYMODE_DIR");
                if (string.IsNullOrWhiteSpace(artifactDirectory))
                    artifactDirectory = DefaultArtifactDirectory;
                Directory.CreateDirectory(artifactDirectory);
                DeleteCapture("01-launch");
                DeleteCapture("02-missile-path");
                DeleteCapture("03-impact");
                DeleteCapture("04-flyover");
                DeleteCapture("05-return");

                EditorSceneManager.OpenScene(MenuScenePath, OpenSceneMode.Single);
                frameCount = 0;
                deployFrame = 0;
                matchReadyFrame = 0;
                requestFrame = 0;
                deploySubmitted = false;
                matchReady = false;
                followArmed = false;
                requestsSubmitted = false;
                cinematicObserved = false;
                launchCaptured = false;
                pathCaptured = false;
                impactCaptured = false;
                flyoverCaptured = false;
                returnCaptured = false;
                completed = false;
                sourceEntity = Entity.Null;
                targetEntity = Entity.Null;
                pendingBatchExitCode = int.MinValue;
                startedAt = EditorApplication.timeSinceStartup;

                EditorApplication.playModeStateChanged -= ExitBatchAfterPlayMode;
                EditorApplication.update -= Continue;
                EditorApplication.update += Continue;
                EditorApplication.EnterPlaymode();
            }
            catch (Exception exception)
            {
                Debug.LogError($"[TacticalFollowAttackCinematicPlayModeValidation] result=Failed\n{exception}");
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

                if (frameCount > TimeoutFrames || EditorApplication.timeSinceStartup - startedAt > 240d)
                {
                    Complete(false, DescribeTimeout());
                    return;
                }

                if (frameCount < DeployWarmupFrames)
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
                    if (!matchScene.GameplayStartComplete ||
                        !IsMatchRuntimeReady(out _))
                        return;

                    matchReady = true;
                    matchReadyFrame = frameCount;
                    return;
                }

                if (!followArmed)
                {
                    if (!TryArmFollowedAirUnit(matchScene, out string armError))
                    {
                        if (frameCount - matchReadyFrame < 300)
                            return;

                        Complete(false, armError);
                        return;
                    }

                    followArmed = true;
                    return;
                }

                if (!requestsSubmitted)
                {
                    if (!TrySubmitAttackRequests(out string requestError))
                    {
                        Complete(false, requestError);
                        return;
                    }

                    requestsSubmitted = true;
                    requestFrame = frameCount;
                    return;
                }

                if (!TryReadCinematic(out TacticalFollowAttackCinematicStateComponent cinematic))
                {
                    if (frameCount - requestFrame < CinematicWaitFrames)
                        return;

                    Complete(false, "Attack cinematic state entity was not created after in-match VFX requests.");
                    return;
                }

                if (cinematic.Active != 0)
                    cinematicObserved = true;

                if (cinematic.Active != 0)
                    CaptureActivePhase(matchScene.WorldCamera, cinematic);

                if (cinematicObserved && cinematic.Active == 0)
                {
                    if (!returnCaptured &&
                        frameCount - requestFrame > 12 &&
                        !TryRenderCamera(matchScene.WorldCamera, CapturePath("05-return"), out string returnError))
                    {
                        Complete(false, returnError);
                        return;
                    }

                    returnCaptured = true;
                    if (!launchCaptured || !pathCaptured || !impactCaptured || !flyoverCaptured)
                    {
                        Complete(
                            false,
                            $"Cinematic completed without all captures. launch={launchCaptured} path={pathCaptured} impact={impactCaptured} flyover={flyoverCaptured} return={returnCaptured} active={cinematic.Active} completed={cinematic.Completed} reason={cinematic.AbortReason}");
                        return;
                    }

                    if (math.abs(Time.timeScale - 1f) > 0.01f)
                    {
                        Complete(false, $"Cinematic completed but Time.timeScale was not restored. timeScale={Time.timeScale:0.000}");
                        return;
                    }

                    Complete(
                        true,
                        $"source={sourceEntity} target={targetEntity} launch={Format(launchPosition)} impact={Format(impactPosition)} dir={artifactDirectory}");
                }
            }
            catch (Exception exception)
            {
                Complete(false, exception.ToString());
            }
        }

        private static void CaptureActivePhase(Camera camera, in TacticalFollowAttackCinematicStateComponent cinematic)
        {
            TacticalFollowAttackCinematicPhase phase =
                TacticalFollowAttackCinematicHelper.EvaluatePhase(cinematic.ElapsedUnscaledSeconds, out _);

            if (!launchCaptured && phase == TacticalFollowAttackCinematicPhase.Launch && cinematic.ElapsedUnscaledSeconds > 0.2f)
            {
                CaptureOrFail(camera, "01-launch");
                launchCaptured = true;
            }

            if (!pathCaptured && phase == TacticalFollowAttackCinematicPhase.MissilePath)
            {
                CaptureOrFail(camera, "02-missile-path");
                pathCaptured = true;
            }

            if (!impactCaptured && cinematic.ImpactEventTriggered != 0)
            {
                CaptureOrFail(camera, "03-impact");
                impactCaptured = true;
            }

            if (!flyoverCaptured && phase == TacticalFollowAttackCinematicPhase.Flyover)
            {
                CaptureOrFail(camera, "04-flyover");
                flyoverCaptured = true;
            }
        }

        private static void CaptureOrFail(Camera camera, string name)
        {
            if (!TryRenderCamera(camera, CapturePath(name), out string error))
                throw new InvalidOperationException(error);
        }

        private static bool TryArmFollowedAirUnit(MatchSceneView matchScene, out string error)
        {
            error = string.Empty;
            World world = World.DefaultGameObjectInjectionWorld;
            if (world == null || !world.IsCreated)
            {
                error = "Default ECS world is not created.";
                return false;
            }

            EntityManager em = world.EntityManager;
            if (!TryFindPlayerAirUnit(em, out sourceEntity))
            {
                error = "No player air unit with UnitAirMovement/LocalTransform/Faction was found for in-match attack cinematic proof.";
                return false;
            }

            LocalTransform sourceTransform = em.GetComponentData<LocalTransform>(sourceEntity);
            if (!TryFindEnemyTarget(em, sourceTransform.Position, out targetEntity, out impactPosition))
            {
                error = "No enemy target with Faction/LocalTransform was found for in-match attack cinematic proof.";
                return false;
            }

            float3 attackDirection = math.normalizesafe(impactPosition - sourceTransform.Position, new float3(0f, 0f, 1f));
            launchPosition = sourceTransform.Position + attackDirection * 3f + new float3(0f, 1.5f, 0f);
            Entity modeEntity = EnsureModeEntity(em);
            TacticalFollowCameraModeComponent mode = em.GetComponentData<TacticalFollowCameraModeComponent>(modeEntity);
            if (matchScene.WorldCamera != null)
            {
                mode.RestorePoseValid = 1;
                mode.RestorePosition = matchScene.WorldCamera.transform.position;
                mode.RestoreRotation = matchScene.WorldCamera.transform.rotation;
                mode.RestoreFieldOfView = matchScene.WorldCamera.fieldOfView;
                mode.RestoreOrthographicSize = matchScene.WorldCamera.orthographicSize;
                mode.RestoreOrthographic = matchScene.WorldCamera.orthographic ? (byte)1 : (byte)0;
            }

            mode.Enabled = 1;
            mode.PanInputLocked = 1;
            mode.HasBaseTarget = 1;
            mode.BaseTargetKind = TacticalFollowCameraTargetKind.Unit;
            mode.BaseTargetEntity = sourceEntity;
            mode.HasTemporaryTarget = 0;
            mode.TemporaryTargetKind = TacticalFollowCameraTargetKind.None;
            mode.TemporaryTargetEntity = Entity.Null;
            mode.ModeEnteredFrame = Time.frameCount;
            em.SetComponentData(modeEntity, mode);

            DynamicBuffer<TacticalFollowCameraBaseTargetElement> baseTargets =
                em.HasBuffer<TacticalFollowCameraBaseTargetElement>(modeEntity)
                    ? em.GetBuffer<TacticalFollowCameraBaseTargetElement>(modeEntity)
                    : em.AddBuffer<TacticalFollowCameraBaseTargetElement>(modeEntity);
            baseTargets.Clear();
            baseTargets.Add(new TacticalFollowCameraBaseTargetElement { Entity = sourceEntity });
            return true;
        }

        private static bool TrySubmitAttackRequests(out string error)
        {
            error = string.Empty;
            World world = World.DefaultGameObjectInjectionWorld;
            if (world == null || !world.IsCreated)
            {
                error = "Default ECS world is not created while submitting attack cinematic proof requests.";
                return false;
            }

            EntityManager em = world.EntityManager;
            if (sourceEntity == Entity.Null || !em.Exists(sourceEntity))
            {
                error = "Attack cinematic proof source entity no longer exists.";
                return false;
            }

            if (!em.HasComponent<UnitAttackImpactVfxReference>(sourceEntity))
            {
                error = $"Attack cinematic proof source entity {sourceEntity} has no UnitAttackImpactVfxReference.";
                return false;
            }

            UnityObjectRef<GameObject> launchPrefab = default;
            float launchForwardOffset = 0f;
            float launchHeightOffset = 0f;
            if (em.HasComponent<UnitMuzzleFlashVfxReference>(sourceEntity))
            {
                UnitMuzzleFlashVfxReference muzzle = em.GetComponentData<UnitMuzzleFlashVfxReference>(sourceEntity);
                launchPrefab = muzzle.Prefab;
                launchForwardOffset = muzzle.ForwardOffset;
                launchHeightOffset = muzzle.HeightOffset;
            }

            UnitAttackImpactVfxReference impact = em.GetComponentData<UnitAttackImpactVfxReference>(sourceEntity);
            float3 direction = math.normalizesafe(impactPosition - launchPosition, new float3(0f, 0f, 1f));
            quaternion rotation = quaternion.LookRotationSafe(direction, new float3(0f, 1f, 0f));
            float3 sideRight = math.normalizesafe(math.cross(new float3(0f, 1f, 0f), direction), new float3(1f, 0f, 0f));
            float3 requestLaunchPosition = launchPosition + direction * launchForwardOffset + new float3(0f, launchHeightOffset, 0f);

            Entity muzzleRequest = em.CreateEntity(typeof(UnitAttackVfxRequest));
            em.SetComponentData(muzzleRequest, new UnitAttackVfxRequest
            {
                Kind = (byte)UnitAttackVfxRequestKind.MuzzleFlash,
                Source = sourceEntity,
                Target = targetEntity,
                SourcePosition = launchPosition,
                TargetPosition = impactPosition,
                Prefab = launchPrefab,
                PlaybackPosition = requestLaunchPosition,
                PlaybackRotation = rotation,
                SideRight = sideRight,
                OriginCount = 2,
                LateralOffset = 0.9f
            });

            Entity impactRequest = em.CreateEntity(typeof(UnitAttackVfxRequest));
            em.SetComponentData(impactRequest, new UnitAttackVfxRequest
            {
                Kind = (byte)UnitAttackVfxRequestKind.Impact,
                Source = sourceEntity,
                Target = targetEntity,
                SourcePosition = launchPosition,
                TargetPosition = impactPosition,
                Prefab = impact.Prefab,
                PlaybackPosition = impactPosition,
                PlaybackRotation = rotation,
                SideRight = sideRight
            });
            return true;
        }

        private static bool TryFindPlayerAirUnit(EntityManager em, out Entity unit)
        {
            unit = Entity.Null;
            using EntityQuery query = em.CreateEntityQuery(
                ComponentType.ReadOnly<Faction>(),
                ComponentType.ReadOnly<UnitAirMovement>(),
                ComponentType.ReadOnly<LocalTransform>());
            using NativeArray<Entity> entities = query.ToEntityArray(Allocator.Temp);
            for (int i = 0; i < entities.Length; i++)
            {
                Entity candidate = entities[i];
                if (em.GetComponentData<Faction>(candidate).Id != FactionIdentity.PlayerFactionId)
                    continue;

                unit = candidate;
                return true;
            }

            return false;
        }

        private static bool TryFindEnemyTarget(EntityManager em, float3 sourcePosition, out Entity target, out float3 position)
        {
            target = Entity.Null;
            position = default;
            float bestDistanceSq = 0f;
            using EntityQuery query = em.CreateEntityQuery(
                ComponentType.ReadOnly<Faction>(),
                ComponentType.ReadOnly<LocalTransform>());
            using NativeArray<Entity> entities = query.ToEntityArray(Allocator.Temp);
            for (int i = 0; i < entities.Length; i++)
            {
                Entity candidate = entities[i];
                if (candidate == sourceEntity)
                    continue;

                if (em.GetComponentData<Faction>(candidate).Id == FactionIdentity.PlayerFactionId)
                    continue;

                LocalTransform transform = em.GetComponentData<LocalTransform>(candidate);
                float distanceSq = math.lengthsq(transform.Position - sourcePosition);
                if (distanceSq < 45f * 45f)
                    continue;

                if (target != Entity.Null && distanceSq >= bestDistanceSq)
                    continue;

                target = candidate;
                position = transform.Position + new float3(0f, 1.5f, 0f);
                bestDistanceSq = distanceSq;
            }

            return target != Entity.Null;
        }

        private static Entity EnsureModeEntity(EntityManager em)
        {
            using EntityQuery query = em.CreateEntityQuery(ComponentType.ReadWrite<TacticalFollowCameraModeComponent>());
            if (!query.IsEmptyIgnoreFilter)
                return query.GetSingletonEntity();

            return em.CreateEntity(typeof(TacticalFollowCameraModeComponent));
        }

        private static bool TryReadCinematic(out TacticalFollowAttackCinematicStateComponent cinematic)
        {
            cinematic = default;
            World world = World.DefaultGameObjectInjectionWorld;
            if (world == null || !world.IsCreated)
                return false;

            EntityManager em = world.EntityManager;
            using EntityQuery query = em.CreateEntityQuery(ComponentType.ReadOnly<TacticalFollowAttackCinematicStateComponent>());
            if (query.IsEmptyIgnoreFilter)
                return false;

            cinematic = em.GetComponentData<TacticalFollowAttackCinematicStateComponent>(query.GetSingletonEntity());
            return true;
        }

        private static bool IsMatchRuntimeReady(out string status)
        {
            status = "waiting";
            if (!TryGetShellState(out UiShellStateComponent shellState))
                return false;

            if (!TryGetRuntimeGameplayState(out RuntimeGameplayStateComponent runtimeState))
                return false;

            if (!TryGetMatchIntroState(out MatchIntroTransitionComponent matchIntro))
                return false;

            bool matchSceneLoaded = SceneManager.GetSceneByName("Match").isLoaded;
            status =
                $"mode={shellState.CurrentMode} route={shellState.ActiveRoute} phase={shellState.Phase} " +
                $"transition={shellState.IsTransitionRunning} playRequested={runtimeState.PlayRequested} " +
                $"matchIntro={matchIntro.State} inputLocked={matchIntro.InputLocked} matchSceneLoaded={(matchSceneLoaded ? 1 : 0)}";

            return shellState.CurrentMode == UiShellMode.MatchHud &&
                   shellState.ActiveRoute == UIRoute.Match &&
                   shellState.IsTransitionRunning == 0 &&
                   runtimeState.PlayRequested != 0 &&
                   matchIntro.State == MatchIntroTransitionStateKind.Complete &&
                   matchIntro.InputLocked == 0 &&
                   matchSceneLoaded;
        }

        private static bool TryGetShellState(out UiShellStateComponent shellState)
        {
            shellState = default;
            World world = World.DefaultGameObjectInjectionWorld;
            if (world == null || !world.IsCreated)
                return false;

            EntityManager em = world.EntityManager;
            using EntityQuery query = em.CreateEntityQuery(
                ComponentType.ReadOnly<UiShellRootComponent>(),
                ComponentType.ReadOnly<UiShellStateComponent>());
            if (query.IsEmptyIgnoreFilter)
                return false;

            shellState = em.GetComponentData<UiShellStateComponent>(query.GetSingletonEntity());
            return true;
        }

        private static bool TryGetRuntimeGameplayState(out RuntimeGameplayStateComponent runtimeState)
        {
            runtimeState = default;
            World world = World.DefaultGameObjectInjectionWorld;
            if (world == null || !world.IsCreated)
                return false;

            EntityManager em = world.EntityManager;
            using EntityQuery query = em.CreateEntityQuery(ComponentType.ReadOnly<RuntimeGameplayStateComponent>());
            if (query.IsEmptyIgnoreFilter)
                return false;

            runtimeState = em.GetComponentData<RuntimeGameplayStateComponent>(query.GetSingletonEntity());
            return true;
        }

        private static bool TryGetMatchIntroState(out MatchIntroTransitionComponent matchIntro)
        {
            matchIntro = default;
            World world = World.DefaultGameObjectInjectionWorld;
            if (world == null || !world.IsCreated)
                return false;

            EntityManager em = world.EntityManager;
            using EntityQuery query = em.CreateEntityQuery(
                ComponentType.ReadOnly<UiShellRootComponent>(),
                ComponentType.ReadOnly<MatchIntroTransitionComponent>());
            if (query.IsEmptyIgnoreFilter)
                return false;

            matchIntro = em.GetComponentData<MatchIntroTransitionComponent>(query.GetSingletonEntity());
            return true;
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

        private static bool TryRenderCamera(Camera camera, string path, out string error)
        {
            error = string.Empty;
            if (camera == null)
            {
                error = "Cannot render attack cinematic proof because world camera is null.";
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

            int width = ResolvePositiveInt("WARLINE_ATTACK_CINEMATIC_CAPTURE_WIDTH", DefaultCaptureWidth);
            int height = ResolvePositiveInt("WARLINE_ATTACK_CINEMATIC_CAPTURE_HEIGHT", DefaultCaptureHeight);
            RenderTexture previousTarget = camera.targetTexture;
            RenderTexture previousActive = RenderTexture.active;
            RenderTexture renderTexture = null;
            Texture2D texture = null;
            try
            {
                renderTexture = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32)
                {
                    name = "Runtime_AttackCinematicProofRenderTexture",
                    antiAliasing = 2
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
                error = $"Failed to render attack cinematic proof camera capture path={path}\n{exception}";
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

        private static string CapturePath(string name)
        {
            string extension = SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null ? ".txt" : ".png";
            return Path.Combine(artifactDirectory, name + extension);
        }

        private static void DeleteCapture(string name)
        {
            string pngPath = Path.Combine(artifactDirectory, name + ".png");
            string textPath = Path.Combine(artifactDirectory, name + ".txt");
            if (File.Exists(pngPath))
                File.Delete(pngPath);
            if (File.Exists(textPath))
                File.Delete(textPath);
        }

        private static string DescribeTimeout()
        {
            IsMatchRuntimeReady(out string runtimeStatus);
            return $"Timed out frame={frameCount} scene={SceneManager.GetActiveScene().name} deploy={deploySubmitted} matchReady={matchReady} followArmed={followArmed} requests={requestsSubmitted} observed={cinematicObserved} captures=({launchCaptured},{pathCaptured},{impactCaptured},{flyoverCaptured},{returnCaptured}) runtime=({runtimeStatus})";
        }

        private static string Format(float3 value) => $"({value.x:0.00},{value.y:0.00},{value.z:0.00})";

        private static string Format(Vector3 value) => $"({value.x:0.00},{value.y:0.00},{value.z:0.00})";

        private static void Complete(bool success, string message)
        {
            if (completed)
                return;

            completed = true;
            EditorApplication.update -= Continue;
            if (success)
                Debug.Log($"[TacticalFollowAttackCinematicPlayModeValidation] result=Passed {message}");
            else
                Debug.LogError($"[TacticalFollowAttackCinematicPlayModeValidation] result=Failed {message}");

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
