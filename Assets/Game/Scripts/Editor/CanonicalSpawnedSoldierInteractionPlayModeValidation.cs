using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Game.Components;
using Game.Composition;
using Game.Runtime;
using Game.UI.Contracts;
using Game.UI.Runtime;
using TMPro;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Graphics;
using Unity.Mathematics;
using Unity.Rendering;
using Unity.Transforms;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Game.Editor
{
    public static class CanonicalSpawnedSoldierInteractionPlayModeValidation
    {
        private const string MenuScenePath = "Assets/Game/Scenes/Menu.unity";
        private const string Marker = "[CanonicalSpawnedSoldierInteractionPlayModeValidation]";
        private const int TimeoutSeconds = 240;
        private const int StableBaselineFrames = 600;

        private static readonly HashSet<Entity> BaselineUnits = new();
        private static bool _completed;
        private static bool _deploySubmitted;
        private static bool _matchReady;
        private static bool _drawerOpened;
        private static bool _soldiersSelected;
        private static bool _recruitSubmitted;
        private static bool _cameraFocused;
        private static bool _selectionSubmitted;
        private static bool _captureRequested;
        private static int _frame;
        private static int _stateFrame;
        private static int _stableUnitCount;
        private static int _stableUnitFrames;
        private static int _baselineUnitCount;
        private static int _captureFrame;
        private static double _startedAt;
        private static string _evidencePath;
        private static Entity _spawnedUnit;
        private static int _pendingExitCode = int.MinValue;

        [MenuItem("Tools/Validation/Canonical Spawned Soldier Interaction")]
        public static void Run()
        {
            try
            {
                string evidenceDirectory = Path.GetFullPath(Path.Combine(Application.dataPath, "../Build/EditorEvidence"));
                Directory.CreateDirectory(evidenceDirectory);
                _evidencePath = Path.Combine(evidenceDirectory, "CanonicalSpawnedSoldierSelected.png");
                if (File.Exists(_evidencePath))
                    File.Delete(_evidencePath);

                EditorSceneManager.OpenScene(MenuScenePath, OpenSceneMode.Single);
                BaselineUnits.Clear();
                _completed = false;
                _deploySubmitted = false;
                _matchReady = false;
                _drawerOpened = false;
                _soldiersSelected = false;
                _recruitSubmitted = false;
                _cameraFocused = false;
                _selectionSubmitted = false;
                _captureRequested = false;
                _frame = 0;
                _stateFrame = 0;
                _stableUnitCount = -1;
                _stableUnitFrames = 0;
                _baselineUnitCount = -1;
                _captureFrame = -1;
                _startedAt = EditorApplication.timeSinceStartup;
                _spawnedUnit = Entity.Null;
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

                if (!_drawerOpened)
                {
                    int currentUnits = CountCanonicalUnits();
                    if (currentUnits <= 0)
                        return;

                    if (currentUnits != _stableUnitCount)
                    {
                        _stableUnitCount = currentUnits;
                        _stableUnitFrames = 0;
                        return;
                    }

                    _stableUnitFrames++;
                    if (_stableUnitFrames < StableBaselineFrames)
                        return;

                    SnapshotBaselineUnits();
                    _baselineUnitCount = BaselineUnits.Count;
                    MatchHudRightQuickRailView rail = UnityEngine.Object.FindAnyObjectByType<MatchHudRightQuickRailView>(FindObjectsInactive.Exclude);
                    if (rail == null || rail.BuildButton == null || !rail.BuildButton.interactable)
                        return;

                    rail.BuildButton.onClick.Invoke();
                    _drawerOpened = true;
                    _stateFrame = _frame;
                    return;
                }

                BuildDrawerView drawer = UnityEngine.Object.FindAnyObjectByType<BuildDrawerView>(FindObjectsInactive.Include);
                if (!_recruitSubmitted && (drawer == null || !drawer.IsOpen))
                    return;

                if (!_soldiersSelected)
                {
                    BuildDrawerTabView soldiers = FindSoldiersTab(drawer);
                    if (soldiers?.Button == null || !soldiers.Button.interactable)
                        return;

                    soldiers.Button.onClick.Invoke();
                    _soldiersSelected = true;
                    _stateFrame = _frame;
                    return;
                }

                if (!_recruitSubmitted)
                {
                    if (_frame - _stateFrame < 20)
                        return;

                    Button recruit = drawer.PrimaryActionButton;
                    if (recruit == null || !recruit.gameObject.activeInHierarchy || !recruit.interactable)
                    {
                        if (_frame - _stateFrame > 300)
                            Complete(false, $"Recruit never became available instruction={drawer.InstructionText?.text}");
                        return;
                    }

                    recruit.onClick.Invoke();
                    _recruitSubmitted = true;
                    _stateFrame = _frame;
                    return;
                }

                if (_spawnedUnit == Entity.Null)
                {
                    if (!TryFindNewSpawnedSoldier(out _spawnedUnit))
                        return;

                    _stateFrame = _frame;
                    Debug.Log($"{Marker} spawnedUnit={_spawnedUnit.Index}:{_spawnedUnit.Version}");
                    return;
                }

                if (!TryGetEntityManager(out EntityManager em) || !em.Exists(_spawnedUnit))
                {
                    Complete(false, BuildStatus("spawned entity no longer exists"));
                    return;
                }

                if (!IsUnitVisiblyRendered(em, _spawnedUnit))
                {
                    if (_frame - _stateFrame > 600)
                        Complete(false, BuildStatus("spawned entity never acquired a visible render tree"));
                    return;
                }

                if (!_cameraFocused)
                {
                    if (drawer != null && drawer.IsOpen && drawer.CloseButton != null)
                        drawer.CloseButton.onClick.Invoke();

                    FocusCameraOnUnit(em, _spawnedUnit);
                    _cameraFocused = true;
                    _stateFrame = _frame;
                    return;
                }

                if (!_selectionSubmitted)
                {
                    if (_frame - _stateFrame < 90)
                        return;

                    MatchHudSquadTrayView tray = UnityEngine.Object.FindAnyObjectByType<MatchHudSquadTrayView>(FindObjectsInactive.Exclude);
                    Button soldiersButton = FindSoldiersSquadButton(tray);
                    if (soldiersButton == null || !soldiersButton.interactable)
                    {
                        Complete(false, BuildStatus("soldier squad button was unavailable"));
                        return;
                    }

                    soldiersButton.onClick.Invoke();
                    _selectionSubmitted = true;
                    _stateFrame = _frame;
                    return;
                }

                if (_frame - _stateFrame < 30)
                    return;

                if (!em.HasComponent<SelectedUnitTag>(_spawnedUnit))
                {
                    if (_frame - _stateFrame > 300)
                        Complete(false, BuildStatus("normal squad selection did not select the spawned entity"));
                    return;
                }

                if (!TryReadFocusedUnit(em, out FocusedUnitUiReadModelComponent focused) ||
                    focused.HasFocusedUnit == 0 || focused.FocusedUnit != _spawnedUnit)
                {
                    if (_frame - _stateFrame > 300)
                        Complete(false, BuildStatus("focused selection read model did not resolve the spawned entity"));
                    return;
                }

                if (!IsSelectionPanelVisible())
                {
                    if (_frame - _stateFrame > 300)
                        Complete(false, BuildStatus("selection panel did not become visible"));
                    return;
                }

                Vector3 viewport = ResolveViewportPosition(em, _spawnedUnit);
                if (viewport.z <= 0f || viewport.x < 0f || viewport.x > 1f || viewport.y < 0f || viewport.y > 1f)
                {
                    Complete(false, BuildStatus($"spawned entity was outside camera viewport viewport={viewport}"));
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
                    BuildStatus($"canonicalUnits={_baselineUnitCount}->{CountCanonicalUnits()} selected=1 focused=1 panel=1 visibleRender=1 viewport={viewport} evidence={_evidencePath}"));
            }
            catch (Exception exception)
            {
                Complete(false, exception.ToString());
            }
        }

        private static bool TryFindNewSpawnedSoldier(out Entity spawned)
        {
            spawned = Entity.Null;
            if (!TryGetEntityManager(out EntityManager em))
                return false;

            using EntityQuery query = em.CreateEntityQuery(new EntityQueryDesc
            {
                All = new[] { ComponentType.ReadOnly<UnitSourcePrefabKey>() },
                None = new[] { ComponentType.ReadOnly<Prefab>() }
            });
            using NativeArray<Entity> entities = query.ToEntityArray(Allocator.Temp);
            for (int i = 0; i < entities.Length; i++)
            {
                Entity entity = entities[i];
                if (BaselineUnits.Contains(entity))
                    continue;

                string source = em.GetComponentData<UnitSourcePrefabKey>(entity).Value.ToString();
                if (source.Contains("chr_soldier", StringComparison.OrdinalIgnoreCase) ||
                    source.Contains("_soldier_", StringComparison.OrdinalIgnoreCase))
                {
                    spawned = entity;
                    return true;
                }
            }

            return false;
        }

        private static void SnapshotBaselineUnits()
        {
            BaselineUnits.Clear();
            if (!TryGetEntityManager(out EntityManager em))
                return;

            using EntityQuery query = em.CreateEntityQuery(new EntityQueryDesc
            {
                All = new[] { ComponentType.ReadOnly<UnitSourcePrefabKey>() },
                None = new[] { ComponentType.ReadOnly<Prefab>() }
            });
            using NativeArray<Entity> entities = query.ToEntityArray(Allocator.Temp);
            for (int i = 0; i < entities.Length; i++)
                BaselineUnits.Add(entities[i]);
        }

        private static bool IsUnitVisiblyRendered(EntityManager em, Entity unit)
        {
            if (em.HasComponent<UnitDetailedVisualReference>(unit) &&
                IsRenderableVisibleRecursive(em, em.GetComponentData<UnitDetailedVisualReference>(unit).Root, true))
                return true;
            if (em.HasComponent<UnitModelInstanceReference>(unit) &&
                IsRenderableVisibleRecursive(em, em.GetComponentData<UnitModelInstanceReference>(unit).Instance, true))
                return true;
            if (em.HasComponent<UnitMidLodInstanceReference>(unit) &&
                IsRenderableVisibleRecursive(em, em.GetComponentData<UnitMidLodInstanceReference>(unit).Instance, true))
                return true;
            if (em.HasComponent<UnitLowLodInstanceReference>(unit) &&
                IsRenderableVisibleRecursive(em, em.GetComponentData<UnitLowLodInstanceReference>(unit).Instance, true))
                return true;
            return IsRenderableVisibleRecursive(em, unit, true);
        }

        private static bool IsRenderableVisibleRecursive(EntityManager em, Entity entity, bool parentVisible)
        {
            using NativeHashSet<Entity> visited = new(16, Allocator.Temp);
            return IsRenderableVisibleRecursive(em, entity, parentVisible, visited);
        }

        private static bool IsRenderableVisibleRecursive(
            EntityManager em,
            Entity entity,
            bool parentVisible,
            NativeHashSet<Entity> visited)
        {
            if (entity == Entity.Null || !em.Exists(entity) || !parentVisible || !visited.Add(entity))
                return false;

            bool transformVisible = !em.HasComponent<LocalTransform>(entity) ||
                                    math.abs(em.GetComponentData<LocalTransform>(entity).Scale) > 0.001f;
            bool entityVisible = transformVisible &&
                                 !em.HasComponent<Disabled>(entity) &&
                                 !em.HasComponent<DisableRendering>(entity) &&
                                 !em.HasComponent<UnitRenderBudgetCulledTag>(entity);
            if (entityVisible &&
                (em.HasComponent<RenderFilterSettings>(entity) ||
                 em.HasComponent<RenderBounds>(entity) ||
                 em.HasComponent<MaterialMeshInfo>(entity)))
                return true;

            if (em.HasBuffer<LinkedEntityGroup>(entity))
            {
                DynamicBuffer<LinkedEntityGroup> linked = em.GetBuffer<LinkedEntityGroup>(entity);
                for (int i = 0; i < linked.Length; i++)
                {
                    if (linked[i].Value != entity && IsRenderableVisibleRecursive(em, linked[i].Value, true, visited))
                        return true;
                }
            }

            if (!em.HasBuffer<Child>(entity))
                return false;

            DynamicBuffer<Child> children = em.GetBuffer<Child>(entity);
            for (int i = 0; i < children.Length; i++)
            {
                bool childVisible = transformVisible && !em.HasComponent<Disabled>(entity);
                if (IsRenderableVisibleRecursive(em, children[i].Value, childVisible, visited))
                    return true;
            }

            return false;
        }

        private static void FocusCameraOnUnit(EntityManager em, Entity unit)
        {
            if (!em.HasComponent<LocalToWorld>(unit) || Camera.main == null)
                throw new InvalidOperationException("Spawned unit has no camera-focusable LocalToWorld position.");

            Vector3 worldPosition = em.GetComponentData<LocalToWorld>(unit).Position;
            RtsCameraSystem cameraSystem = World.DefaultGameObjectInjectionWorld?.GetExistingSystemManaged<RtsCameraSystem>();
            if (cameraSystem == null)
                throw new InvalidOperationException("RTS camera system is unavailable.");
            cameraSystem.MoveCameraGroundCenterTo(Camera.main, worldPosition);
        }

        private static Vector3 ResolveViewportPosition(EntityManager em, Entity unit)
        {
            if (Camera.main == null || !em.HasComponent<LocalToWorld>(unit))
                return new Vector3(-1f, -1f, -1f);
            return Camera.main.WorldToViewportPoint(em.GetComponentData<LocalToWorld>(unit).Position);
        }

        private static Button FindSoldiersSquadButton(MatchHudSquadTrayView tray)
        {
            if (tray == null)
                return null;

            Button[] buttons = tray.GetComponentsInChildren<Button>(true);
            for (int i = 0; i < buttons.Length; i++)
            {
                TMP_Text[] labels = buttons[i].GetComponentsInChildren<TMP_Text>(true);
                for (int labelIndex = 0; labelIndex < labels.Length; labelIndex++)
                {
                    if (string.Equals(labels[labelIndex].text?.Trim(), "RIFLE SQUAD", StringComparison.OrdinalIgnoreCase))
                        return buttons[i];
                }
            }

            return null;
        }

        private static bool IsSelectionPanelVisible()
        {
            MatchHudSelectionPanelView panel = UnityEngine.Object.FindAnyObjectByType<MatchHudSelectionPanelView>(FindObjectsInactive.Include);
            if (panel == null)
                return false;

            FieldInfo field = typeof(MatchHudSelectionPanelView).GetField(
                "selectedSquadPanel",
                BindingFlags.Instance | BindingFlags.NonPublic);
            return field?.GetValue(panel) is GameObject root && root.activeInHierarchy;
        }

        private static bool TryReadFocusedUnit(EntityManager em, out FocusedUnitUiReadModelComponent focused)
        {
            using EntityQuery query = em.CreateEntityQuery(ComponentType.ReadOnly<FocusedUnitUiReadModelComponent>());
            if (query.CalculateEntityCount() != 1)
            {
                focused = default;
                return false;
            }

            focused = query.GetSingleton<FocusedUnitUiReadModelComponent>();
            return true;
        }

        private static int CountCanonicalUnits()
        {
            if (!TryGetEntityManager(out EntityManager em))
                return -1;

            using EntityQuery query = em.CreateEntityQuery(new EntityQueryDesc
            {
                All = new[] { ComponentType.ReadOnly<UnitSourcePrefabKey>() },
                None = new[] { ComponentType.ReadOnly<Prefab>() }
            });
            return query.CalculateEntityCount();
        }

        private static bool TryGetEntityManager(out EntityManager em)
        {
            World world = World.DefaultGameObjectInjectionWorld;
            if (world == null || !world.IsCreated)
            {
                em = default;
                return false;
            }

            em = world.EntityManager;
            return true;
        }

        private static BuildDrawerTabView FindSoldiersTab(BuildDrawerView drawer)
        {
            BuildDrawerTabView[] tabs = drawer.Tabs;
            for (int i = 0; tabs != null && i < tabs.Length; i++)
            {
                if (tabs[i] != null && tabs[i].Category == BuildDrawerCategory.Soldiers)
                    return tabs[i];
            }

            return null;
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
                    return button;
            }

            return null;
        }

        private static string BuildStatus(string message)
        {
            return $"{message} deploy={(_deploySubmitted ? 1 : 0)} matchReady={(_matchReady ? 1 : 0)} drawer={(_drawerOpened ? 1 : 0)} soldiers={(_soldiersSelected ? 1 : 0)} recruit={(_recruitSubmitted ? 1 : 0)} cameraFocused={(_cameraFocused ? 1 : 0)} selectionSubmitted={(_selectionSubmitted ? 1 : 0)} spawned={(_spawnedUnit == Entity.Null ? 0 : 1)} baselineUnits={_baselineUnitCount}";
        }

        private static void Complete(bool success, string message)
        {
            if (_completed)
                return;

            _completed = true;
            EditorApplication.update -= Continue;
            if (success)
                Debug.Log($"{Marker} result=Passed {message}");
            else
                Debug.LogError($"{Marker} result=Failed {message}");

            _pendingExitCode = success ? 0 : 1;
            EditorApplication.playModeStateChanged -= ExitAfterPlayMode;
            EditorApplication.playModeStateChanged += ExitAfterPlayMode;
            if (EditorApplication.isPlaying)
                EditorApplication.ExitPlaymode();
            else
                EditorApplication.Exit(_pendingExitCode);
        }

        private static void ExitAfterPlayMode(PlayModeStateChange change)
        {
            if (change != PlayModeStateChange.EnteredEditMode || _pendingExitCode == int.MinValue)
                return;

            int exitCode = _pendingExitCode;
            _pendingExitCode = int.MinValue;
            EditorApplication.playModeStateChanged -= ExitAfterPlayMode;
            EditorApplication.Exit(exitCode);
        }
    }
}
