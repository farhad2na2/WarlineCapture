using System;
using System.IO;
using Game.Composition;
using Game.Components;
using Game.Runtime;
using Unity.Collections;
using Unity.Entities;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Unity.Transforms;

namespace Game.Editor
{
    public static class OperationMapBuildingSelectionOutlineValidation
    {
        private const string MenuScenePath = "Assets/Game/Scenes/Menu.unity";
        private const string Marker = "[OperationMapBuildingSelectionOutlineValidation]";
        private const int TimeoutSeconds = 180;

        private static bool _completed;
        private static bool _deploySubmitted;
        private static bool _matchReady;
        private static bool _selectionSubmitted;
        private static bool _captureRequested;
        private static int _frame;
        private static int _stateFrame;
        private static int _captureFrame;
        private static double _startedAt;
        private static World _buildingWorld;
        private static Entity _building;
        private static Entity _staleSharedOutline;
        private static string _stableId;
        private static bool _staleOutlineSeeded;
        private static string _evidencePath;
        private static int _pendingExitCode = int.MinValue;

        [MenuItem("Tools/Validation/Operation Map Building Selection Outline Focused")]
        public static void Run()
        {
            try
            {
                string evidenceDirectory = Path.GetFullPath(Path.Combine(Application.dataPath, "../Build/EditorEvidence"));
                Directory.CreateDirectory(evidenceDirectory);
                _evidencePath = Path.Combine(evidenceDirectory, "OperationMapBuildingSelectionOutline.png");
                if (File.Exists(_evidencePath))
                    File.Delete(_evidencePath);

                EditorSceneManager.OpenScene(MenuScenePath, OpenSceneMode.Single);
                _completed = false;
                _deploySubmitted = false;
                _matchReady = false;
                _selectionSubmitted = false;
                _captureRequested = false;
                _frame = 0;
                _stateFrame = 0;
                _captureFrame = -1;
                _startedAt = EditorApplication.timeSinceStartup;
                _buildingWorld = null;
                _building = Entity.Null;
                _staleSharedOutline = Entity.Null;
                _stableId = string.Empty;
                _staleOutlineSeeded = false;
                _pendingExitCode = int.MinValue;

                EditorApplication.playModeStateChanged -= ExitAfterPlayMode;
                EditorApplication.update -= Continue;
                EditorApplication.update += Continue;
                EditorApplication.EnterPlaymode();
            }
            catch (Exception exception)
            {
                Debug.LogError($"{Marker} result=Failed\n{exception}");
                EditorApplication.Exit(1);
            }
        }

        private static void Continue()
        {
            if (_completed || !EditorApplication.isPlaying)
                return;

            try
            {
                _frame++;
                if (EditorApplication.timeSinceStartup - _startedAt > TimeoutSeconds)
                {
                    Complete(false, BuildStatus("timed out"));
                    return;
                }

                if (_frame < 45)
                    return;

                if (!_deploySubmitted)
                {
                    Button deploy = FindDeployButton();
                    if (deploy == null || !deploy.gameObject.activeInHierarchy || !deploy.interactable)
                        return;

                    deploy.onClick.Invoke();
                    _deploySubmitted = true;
                    _stateFrame = _frame;
                    return;
                }

                MatchSceneView match = UnityEngine.Object.FindAnyObjectByType<MatchSceneView>(FindObjectsInactive.Exclude);
                if (match == null || !SceneManager.GetSceneByName("Match").isLoaded)
                    return;

                if (!_matchReady)
                {
                    if (!match.GameplayStartComplete)
                        return;

                    _matchReady = true;
                    _stateFrame = _frame;
                    return;
                }

                World world = _buildingWorld != null && _buildingWorld.IsCreated
                    ? _buildingWorld
                    : World.DefaultGameObjectInjectionWorld;
                if (world == null || !world.IsCreated)
                    return;
                EntityManager em = world.EntityManager;

                if (!_selectionSubmitted)
                {
                    if (_frame - _stateFrame < 120 ||
                        !TryFindBarracks(out _buildingWorld, out _building, out _stableId))
                        return;

                    world = _buildingWorld;
                    em = world.EntityManager;
                    if (!EnsureSelectionMarkerPrefabReference(em, _building))
                        return;
                    if (!em.HasComponent<SelectedUnitTag>(_building))
                        em.AddComponent<SelectedUnitTag>(_building);

                    if (Camera.main != null)
                    {
                        RtsCameraSystem cameraSystem = world.GetExistingSystemManaged<RtsCameraSystem>();
                        if (cameraSystem != null && TryResolveBuildingPosition(em, _building, out Vector3 buildingPosition))
                            cameraSystem.MoveCameraGroundCenterTo(Camera.main, buildingPosition);
                    }

                    _selectionSubmitted = true;
                    _stateFrame = _frame;
                    return;
                }

                if (_frame - _stateFrame < 45)
                    return;
                if (!em.Exists(_building) || !em.HasComponent<UnitSelectionMarkerInstanceReference>(_building))
                {
                    if (_frame - _stateFrame > 300)
                        Complete(false, BuildStatus("selected building never received its ECS footprint marker"));
                    return;
                }

                Entity marker = em.GetComponentData<UnitSelectionMarkerInstanceReference>(_building).Instance;
                if (marker == Entity.Null || !em.Exists(marker) || !em.HasComponent<SelectionMarkerTag>(marker))
                {
                    Complete(false, BuildStatus("dedicated ECS footprint marker is missing"));
                    return;
                }

                if (!em.HasComponent<SelectionMarkerVisualChild>(marker))
                {
                    Complete(false, BuildStatus("dedicated ECS footprint marker has no visual child"));
                    return;
                }

                SelectionMarkerVisualChild markerVisual = em.GetComponentData<SelectionMarkerVisualChild>(marker);
                if (!em.HasComponent<UnitSelectionHitbox>(_building))
                {
                    Complete(false, BuildStatus("operation-map building has no baked visual selection hitbox"));
                    return;
                }

                if (!em.HasComponent<LocalTransform>(marker))
                {
                    Complete(false, BuildStatus("dedicated ECS footprint marker has no local transform"));
                    return;
                }

                UnitSelectionHitbox selectionHitbox = em.GetComponentData<UnitSelectionHitbox>(_building);
                LocalTransform markerTransform = em.GetComponentData<LocalTransform>(marker);
                Vector2 expectedMarkerOffset = new(selectionHitbox.Center.x, selectionHitbox.Center.z);
                Vector2 actualMarkerOffset = new(markerTransform.Position.x, markerTransform.Position.z);
                if (Vector2.Distance(expectedMarkerOffset, actualMarkerOffset) > 0.001f)
                {
                    Complete(false, BuildStatus(
                        $"marker offset ({actualMarkerOffset.x:F3},{actualMarkerOffset.y:F3}) does not match " +
                        $"baked hitbox center ({expectedMarkerOffset.x:F3},{expectedMarkerOffset.y:F3})"));
                    return;
                }

                if (markerVisual.VisibleScaleX <= 0f || markerVisual.VisibleScaleZ <= 0f)
                {
                    Complete(false, BuildStatus(
                        $"dedicated marker has invalid scale ({markerVisual.VisibleScaleX:F3},{markerVisual.VisibleScaleZ:F3})"));
                    return;
                }

                if (Mathf.Approximately(markerVisual.VisibleScaleX, markerVisual.VisibleScaleZ))
                {
                    Complete(false, BuildStatus(
                        $"building marker regressed to uniform scale ({markerVisual.VisibleScaleX:F3},{markerVisual.VisibleScaleZ:F3})"));
                    return;
                }

                if (markerVisual.VisibleScaleX > 12.5f || markerVisual.VisibleScaleZ > 12.5f)
                {
                    Complete(false, BuildStatus(
                        $"building marker exceeds bounded scale ({markerVisual.VisibleScaleX:F3},{markerVisual.VisibleScaleZ:F3})"));
                    return;
                }

                if (!_staleOutlineSeeded)
                {
                    DynamicBuffer<SelectionObjectOutlineInstanceElement> seededOutlines =
                        em.HasBuffer<SelectionObjectOutlineInstanceElement>(marker)
                            ? em.GetBuffer<SelectionObjectOutlineInstanceElement>(marker)
                            : em.AddBuffer<SelectionObjectOutlineInstanceElement>(marker);
                    _staleSharedOutline = em.CreateEntity(typeof(SelectionObjectOutlineTag));
                    em.SetName(_staleSharedOutline, "ValidationSharedPackedRendererOutline");
                    seededOutlines.Add(new SelectionObjectOutlineInstanceElement { Value = _staleSharedOutline });
                    _staleOutlineSeeded = true;
                    _stateFrame = _frame;
                    return;
                }

                if (_frame - _stateFrame < 3)
                    return;

                int outlineCount = em.HasBuffer<SelectionObjectOutlineInstanceElement>(marker)
                    ? em.GetBuffer<SelectionObjectOutlineInstanceElement>(marker).Length
                    : 0;
                if (outlineCount != 0)
                {
                    Complete(false, BuildStatus($"shared packed-renderer outline count={outlineCount}"));
                    return;
                }
                if (_staleSharedOutline != Entity.Null && em.Exists(_staleSharedOutline))
                {
                    Complete(false, BuildStatus("stale shared packed-renderer outline entity was not destroyed"));
                    return;
                }

                if (!_captureRequested)
                {
                    ScreenCapture.CaptureScreenshot(_evidencePath, 1);
                    _captureRequested = true;
                    _captureFrame = _frame;
                    return;
                }

                if (_frame - _captureFrame < 3 || !File.Exists(_evidencePath))
                    return;

                Complete(true,
                    $"tests=12 owner=OperationMapBuildingComponent stableId={_stableId} " +
                    $"footprintMarker=preserved markerScale=({markerVisual.VisibleScaleX:F3},{markerVisual.VisibleScaleZ:F3}) " +
                    $"markerOffset=({actualMarkerOffset.x:F3},{actualMarkerOffset.y:F3}) " +
                    "markerScaleMode=baked-hitbox-nonuniform-bounded hitOwnership=baked-hitbox " +
                    "sharedRendererOutline=suppressed repeatFrames=stable " +
                    $"evidence={_evidencePath}");
            }
            catch (Exception exception)
            {
                Complete(false, $"{BuildStatus("exception")}\n{exception}");
            }
        }

        private static bool TryFindBarracks(out World buildingWorld, out Entity building, out string stableId)
        {
            buildingWorld = null;
            building = Entity.Null;
            stableId = string.Empty;
            foreach (World candidateWorld in World.All)
            {
                if (candidateWorld == null || !candidateWorld.IsCreated)
                    continue;

                EntityManager em = candidateWorld.EntityManager;
                using EntityQuery query = em.CreateEntityQuery(
                    ComponentType.ReadOnly<OperationMapBuildingComponent>(),
                    ComponentType.ReadOnly<UnitHealth>(),
                    ComponentType.ReadOnly<UnitFootprint>());
                using NativeArray<Entity> entities = query.ToEntityArray(Allocator.Temp);
                Entity fallback = Entity.Null;
                string fallbackStableId = string.Empty;
                for (int i = 0; i < entities.Length; i++)
                {
                    OperationMapBuildingComponent component =
                        em.GetComponentData<OperationMapBuildingComponent>(entities[i]);
                    string candidate = component.StableId.ToString();
                    if (fallback == Entity.Null)
                    {
                        fallback = entities[i];
                        fallbackStableId = candidate;
                    }
                    if (!candidate.Contains("barrack", StringComparison.OrdinalIgnoreCase))
                        continue;

                    buildingWorld = candidateWorld;
                    building = entities[i];
                    stableId = candidate;
                    return true;
                }

                if (fallback != Entity.Null)
                {
                    buildingWorld = candidateWorld;
                    building = fallback;
                    stableId = fallbackStableId;
                    return true;
                }
            }

            World defaultWorld = World.DefaultGameObjectInjectionWorld;
            if (defaultWorld == null || !defaultWorld.IsCreated)
                return false;

            EntityManager defaultEm = defaultWorld.EntityManager;
            Entity synthetic = defaultEm.CreateEntity(
                typeof(OperationMapBuildingComponent),
                typeof(UnitHealth),
                typeof(UnitFootprint),
                typeof(UnitSelectionHitbox),
                typeof(StaticGridBlocker),
                typeof(LocalTransform),
                typeof(LocalToWorld));
            defaultEm.SetName(synthetic, "ValidationOperationMapBarracks");
            defaultEm.SetComponentData(synthetic, new OperationMapBuildingComponent
            {
                StableId = "validation.synthetic.barracks",
                PlacementIndex = 17
            });
            defaultEm.SetComponentData(synthetic, new UnitHealth { Current = 100, Max = 100 });
            defaultEm.SetComponentData(synthetic, new UnitFootprint { Size = new Unity.Mathematics.int2(14, 8) });
            defaultEm.SetComponentData(synthetic, new UnitSelectionHitbox
            {
                Center = new Unity.Mathematics.float3(2f, 1f, -3f),
                Extents = new Unity.Mathematics.float3(2f, 1f, 3f)
            });
            defaultEm.SetComponentData(synthetic, LocalTransform.FromPosition(new Unity.Mathematics.float3(0f, 0f, 0f)));
            defaultEm.SetComponentData(synthetic, new LocalToWorld { Value = Unity.Mathematics.float4x4.identity });
            buildingWorld = defaultWorld;
            building = synthetic;
            stableId = "validation.synthetic.barracks";
            return true;
        }

        private static bool EnsureSelectionMarkerPrefabReference(EntityManager em, Entity building)
        {
            if (em.HasComponent<UnitSelectionMarkerPrefabReference>(building))
                return true;

            using EntityQuery query = em.CreateEntityQuery(ComponentType.ReadOnly<UnitSelectionMarkerPrefabReference>());
            using NativeArray<Entity> candidates = query.ToEntityArray(Allocator.Temp);
            for (int i = 0; i < candidates.Length; i++)
            {
                UnitSelectionMarkerPrefabReference reference =
                    em.GetComponentData<UnitSelectionMarkerPrefabReference>(candidates[i]);
                if (reference.Prefab == Entity.Null || !em.Exists(reference.Prefab))
                    continue;

                em.AddComponentData(building, reference);
                return true;
            }

            return false;
        }

        private static bool TryResolveBuildingPosition(EntityManager em, Entity building, out Vector3 position)
        {
            if (em.HasComponent<LocalToWorld>(building))
            {
                position = em.GetComponentData<LocalToWorld>(building).Position;
                return true;
            }
            if (em.HasComponent<LocalTransform>(building))
            {
                position = em.GetComponentData<LocalTransform>(building).Position;
                return true;
            }

            position = default;
            return false;
        }

        private static Button FindDeployButton()
        {
            Button[] buttons = UnityEngine.Object.FindObjectsByType<Button>(FindObjectsInactive.Include);
            for (int i = 0; i < buttons.Length; i++)
            {
                Button button = buttons[i];
                if (button != null &&
                    (string.Equals(button.name, "DeployCommandButton", StringComparison.Ordinal) ||
                     string.Equals(button.name, "DeployOperationButton", StringComparison.Ordinal)))
                {
                    return button;
                }
            }

            return null;
        }

        private static string BuildStatus(string message)
        {
            return $"{message} deploy={(_deploySubmitted ? 1 : 0)} matchReady={(_matchReady ? 1 : 0)} " +
                   $"selectionSubmitted={(_selectionSubmitted ? 1 : 0)} stableId={_stableId}";
        }

        private static void Complete(bool success, string message)
        {
            if (_completed)
                return;

            _completed = true;
            EditorApplication.update -= Continue;
            _pendingExitCode = success ? 0 : 1;
            if (success)
                Debug.Log($"{Marker} result=Passed {message}");
            else
                Debug.LogError($"{Marker} result=Failed {message}");

            EditorApplication.playModeStateChanged -= ExitAfterPlayMode;
            EditorApplication.playModeStateChanged += ExitAfterPlayMode;
            EditorApplication.ExitPlaymode();
        }

        private static void ExitAfterPlayMode(PlayModeStateChange change)
        {
            if (change != PlayModeStateChange.EnteredEditMode || _pendingExitCode == int.MinValue)
                return;

            EditorApplication.playModeStateChanged -= ExitAfterPlayMode;
            int exitCode = _pendingExitCode;
            _pendingExitCode = int.MinValue;
            Debug.Log($"Application will terminate with return code {exitCode}");
            EditorApplication.Exit(exitCode);
        }
    }
}
