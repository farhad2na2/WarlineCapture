using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Game.Components;
using Game.Composition;
using Game.Runtime;
using Game.UI.Runtime;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Game.Editor
{
    public static class CanonicalBuildingPlacementSpawnPlayModeValidation
    {
        private const string MenuScenePath = "Assets/Game/Scenes/Menu.unity";
        private const string Marker = "[CanonicalBuildingPlacementSpawnPlayModeValidation]";
        private const int TimeoutSeconds = 240;
        private const int StableBaselineFrames = 180;

        private static readonly HashSet<int> BaselineBuildingIds = new();
        private static bool _completed;
        private static bool _deploySubmitted;
        private static bool _matchReady;
        private static bool _drawerOpened;
        private static bool _buildingsSelected;
        private static bool _affordableBuildingSelected;
        private static bool _placementSubmitted;
        private static bool _confirmSubmitted;
        private static bool _cameraFocused;
        private static bool _selectionSubmitted;
        private static bool _captureRequested;
        private static int _frame;
        private static int _stateFrame;
        private static int _stableBuildingCount;
        private static int _stableBuildingFrames;
        private static int _baselineBuildingCount;
        private static int _captureFrame;
        private static int _newRuntimeBuildingCount;
        private static int _matchingRuntimeBuildingCount;
        private static int _spawnedBuildingId;
        private static double _startedAt;
        private static string _evidencePath;
        private static string _spawnedDisplayName;
        private static string _requestedItemName;
        private static Vector2Int _spawnedOrigin;
        private static Vector2Int _spawnedFootprint;
        private static GameObject _spawnedInstance;
        private static object _spawnedBuilding;
        private static int _pendingExitCode = int.MinValue;

        [MenuItem("Tools/Validation/Canonical Building Placement And Spawn")]
        public static void Run()
        {
            try
            {
                string evidenceDirectory = Path.GetFullPath(Path.Combine(Application.dataPath, "../Build/EditorEvidence"));
                Directory.CreateDirectory(evidenceDirectory);
                _evidencePath = Path.Combine(evidenceDirectory, "CanonicalBuildingPlacementSpawn.png");
                if (File.Exists(_evidencePath))
                    File.Delete(_evidencePath);

                EditorSceneManager.OpenScene(MenuScenePath, OpenSceneMode.Single);
                BaselineBuildingIds.Clear();
                _completed = false;
                _deploySubmitted = false;
                _matchReady = false;
                _drawerOpened = false;
                _buildingsSelected = false;
                _affordableBuildingSelected = false;
                _placementSubmitted = false;
                _confirmSubmitted = false;
                _cameraFocused = false;
                _selectionSubmitted = false;
                _captureRequested = false;
                _frame = 0;
                _stateFrame = 0;
                _stableBuildingCount = -1;
                _stableBuildingFrames = 0;
                _baselineBuildingCount = -1;
                _captureFrame = -1;
                _newRuntimeBuildingCount = 0;
                _matchingRuntimeBuildingCount = 0;
                _spawnedBuildingId = 0;
                _startedAt = EditorApplication.timeSinceStartup;
                _spawnedDisplayName = string.Empty;
                _requestedItemName = string.Empty;
                _spawnedOrigin = default;
                _spawnedFootprint = default;
                _spawnedInstance = null;
                _spawnedBuilding = null;
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
                    int currentBuildings = CountRuntimeBuildings(match);
                    if (currentBuildings <= 0)
                        return;

                    if (currentBuildings != _stableBuildingCount)
                    {
                        _stableBuildingCount = currentBuildings;
                        _stableBuildingFrames = 0;
                        return;
                    }

                    _stableBuildingFrames++;
                    if (_stableBuildingFrames < StableBaselineFrames)
                        return;

                    SnapshotBaselineBuildings(match);
                    _baselineBuildingCount = BaselineBuildingIds.Count;
                    MatchHudRightQuickRailView rail = UnityEngine.Object.FindAnyObjectByType<MatchHudRightQuickRailView>(FindObjectsInactive.Exclude);
                    if (rail == null || rail.BuildButton == null || !rail.BuildButton.interactable)
                        return;

                    rail.BuildButton.onClick.Invoke();
                    _drawerOpened = true;
                    _stateFrame = _frame;
                    return;
                }

                BuildDrawerView drawer = UnityEngine.Object.FindAnyObjectByType<BuildDrawerView>(FindObjectsInactive.Include);
                if (!_placementSubmitted && (drawer == null || !drawer.IsOpen))
                    return;

                if (!_buildingsSelected)
                {
                    BuildDrawerTabView buildings = FindBuildingsTab(drawer);
                    if (buildings?.Button == null || !buildings.Button.interactable)
                        return;

                    buildings.Button.onClick.Invoke();
                    _buildingsSelected = true;
                    _stateFrame = _frame;
                    return;
                }

                if (!_placementSubmitted)
                {
                    if (!_affordableBuildingSelected)
                    {
                        if (!TrySelectAffordableBuildingItem(drawer, out _requestedItemName))
                        {
                            if (_frame - _stateFrame > 300)
                                Complete(false, BuildStatus("no affordable building catalog item was available"));
                            return;
                        }

                        _affordableBuildingSelected = true;
                        _stateFrame = _frame;
                        return;
                    }

                    if (_frame - _stateFrame < 20)
                        return;

                    Button place = drawer.PrimaryActionButton;
                    if (place == null || !place.gameObject.activeInHierarchy || !place.interactable)
                    {
                        if (_frame - _stateFrame > 300)
                            Complete(false, BuildStatus($"Place never became available instruction={drawer.InstructionText?.text}"));
                        return;
                    }

                    place.onClick.Invoke();
                    _placementSubmitted = true;
                    _stateFrame = _frame;
                    return;
                }

                if (!_confirmSubmitted)
                {
                    BuildPlacementConfirmationBarView bar = UnityEngine.Object.FindAnyObjectByType<BuildPlacementConfirmationBarView>(FindObjectsInactive.Include);
                    Button confirm = GetPrivateButton(bar, "confirmButton");
                    if (bar == null || bar.Root == null || !bar.Root.gameObject.activeInHierarchy ||
                        confirm == null || !confirm.gameObject.activeInHierarchy || !confirm.interactable)
                    {
                        if (_frame - _stateFrame > 300)
                            Complete(false, BuildStatus("placement confirmation never became valid"));
                        return;
                    }

                    confirm.onClick.Invoke();
                    _confirmSubmitted = true;
                    _stateFrame = _frame;
                    return;
                }

                if (_spawnedBuilding == null)
                {
                    if (!TryFindNewRuntimeBuilding(match))
                    {
                        if (_frame - _stateFrame > 300)
                            Complete(false, BuildStatus("confirmed placement did not register one new runtime building"));
                        return;
                    }

                    if (!ValidateSpawnedBuilding(out string failure))
                    {
                        Complete(false, BuildStatus(failure));
                        return;
                    }

                    _stateFrame = _frame;
                    return;
                }

                if (!_cameraFocused)
                {
                    FocusCameraOnBuilding(_spawnedInstance.transform.position);
                    _cameraFocused = true;
                    _stateFrame = _frame;
                    return;
                }

                if (!_selectionSubmitted)
                {
                    if (_frame - _stateFrame < 120)
                        return;

                    Vector3 screen = Camera.main != null
                        ? Camera.main.WorldToScreenPoint(_spawnedInstance.transform.position)
                        : new Vector3(-1f, -1f, -1f);
                    if (screen.z <= 0f || screen.x < 0f || screen.x > Screen.width || screen.y < 0f || screen.y > Screen.height ||
                        !SubmitNormalBuildingClick(match, screen))
                    {
                        Complete(false, BuildStatus($"normal building click rejected screen={screen}"));
                        return;
                    }

                    _selectionSubmitted = true;
                    _stateFrame = _frame;
                    return;
                }

                if (_frame - _stateFrame < 30)
                    return;

                if (GetCurrentActiveBuildingId(match) != _spawnedBuildingId)
                {
                    if (_frame - _stateFrame > 300)
                        Complete(false, BuildStatus($"normal selection did not focus spawned building active={GetCurrentActiveBuildingId(match)}"));
                    return;
                }

                if (!TryReadSelectionPanel(out GameObject panelRoot, out TMP_Text title) ||
                    panelRoot == null || !panelRoot.activeInHierarchy || title == null ||
                    !title.text.Contains(_spawnedDisplayName, StringComparison.OrdinalIgnoreCase))
                {
                    if (_frame - _stateFrame > 300)
                        Complete(false, BuildStatus($"selection panel did not present spawned building title={title?.text ?? "null"}"));
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

                Complete(true, BuildStatus(
                    $"buildings={_baselineBuildingCount}->{CountRuntimeBuildings(match)} owner={FactionIdentity.PlayerFactionId} presentation=1 link=1 selected=1 panel=1 evidence={_evidencePath}"));
            }
            catch (Exception exception)
            {
                Complete(false, exception.ToString());
            }
        }

        private static bool ValidateSpawnedBuilding(out string failure)
        {
            failure = string.Empty;
            if (_spawnedInstance == null || !_spawnedInstance.activeInHierarchy)
            {
                failure = "spawned building instance is missing or inactive";
                return false;
            }

            bool hasOwner = (bool?)_spawnedBuilding.GetType().GetProperty("HasOwnerFaction")?.GetValue(_spawnedBuilding) ?? false;
            byte owner = (byte?)_spawnedBuilding.GetType().GetProperty("OwnerFactionId")?.GetValue(_spawnedBuilding) ?? 0;
            if (!hasOwner || !FactionIdentity.IsPlayerControlled(owner))
            {
                failure = $"spawned building ownership mismatch hasOwner={(hasOwner ? 1 : 0)} owner={owner}";
                return false;
            }

            if (_spawnedFootprint.x <= 0 || _spawnedFootprint.y <= 0)
            {
                failure = $"spawned building footprint is invalid footprint={_spawnedFootprint}";
                return false;
            }

            if (_spawnedInstance.GetComponent<RuntimeBuildingEntityLink>() == null)
            {
                failure = "spawned building has no canonical runtime interaction link";
                return false;
            }

            Renderer[] renderers = _spawnedInstance.GetComponentsInChildren<Renderer>(true);
            for (int i = 0; i < renderers.Length; i++)
            {
                if (renderers[i] != null && renderers[i].enabled && renderers[i].gameObject.activeInHierarchy)
                    return true;
            }

            failure = "spawned building has no active visible presentation renderer";
            return false;
        }

        private static void SnapshotBaselineBuildings(MatchSceneView match)
        {
            BaselineBuildingIds.Clear();
            foreach (object building in EnumerateRuntimeBuildings(match))
            {
                int id = (int?)building?.GetType().GetProperty("Id")?.GetValue(building) ?? 0;
                if (id > 0)
                    BaselineBuildingIds.Add(id);
            }
        }

        private static bool TryFindNewRuntimeBuilding(MatchSceneView match)
        {
            _newRuntimeBuildingCount = 0;
            _matchingRuntimeBuildingCount = 0;
            object found = null;
            foreach (object building in EnumerateRuntimeBuildings(match))
            {
                int id = (int?)building?.GetType().GetProperty("Id")?.GetValue(building) ?? 0;
                if (id <= 0 || BaselineBuildingIds.Contains(id))
                    continue;

                _newRuntimeBuildingCount++;
                object candidateDefinition = building.GetType().GetField("Definition")?.GetValue(building);
                string displayName = candidateDefinition?.GetType().GetField("DisplayName")?.GetValue(candidateDefinition) as string ?? string.Empty;
                if (string.IsNullOrWhiteSpace(displayName) ||
                    !_requestedItemName.EndsWith(displayName, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                _matchingRuntimeBuildingCount++;
                found = building;
            }

            if (_matchingRuntimeBuildingCount != 1 || found == null)
                return false;

            object definition = found.GetType().GetField("Definition")?.GetValue(found);
            _spawnedBuilding = found;
            _spawnedBuildingId = (int?)found.GetType().GetProperty("Id")?.GetValue(found) ?? 0;
            _spawnedInstance = found.GetType().GetField("Instance")?.GetValue(found) as GameObject;
            _spawnedOrigin = (Vector2Int?)found.GetType().GetField("OriginCell")?.GetValue(found) ?? default;
            _spawnedDisplayName = definition?.GetType().GetField("DisplayName")?.GetValue(definition) as string ?? string.Empty;
            _spawnedFootprint = (Vector2Int?)definition?.GetType().GetField("FootprintCells")?.GetValue(definition) ?? default;
            return true;
        }

        private static int CountRuntimeBuildings(MatchSceneView match)
        {
            int count = 0;
            foreach (object _ in EnumerateRuntimeBuildings(match))
                count++;
            return count;
        }

        private static IEnumerable EnumerateRuntimeBuildings(MatchSceneView match)
        {
            object runtimeBuildings = GetRuntimeBuildingDictionary(match);
            if (runtimeBuildings is not IEnumerable enumerable)
                yield break;

            foreach (object entry in enumerable)
            {
                object building = entry?.GetType().GetProperty("Value")?.GetValue(entry);
                if (building != null)
                    yield return building;
            }
        }

        private static object GetBuildingUiQueryContext(MatchSceneView match)
        {
            object bootstrap = GetMatchBootstrap(match);
            return bootstrap?.GetType()
                .GetProperty("BuildingUiQueryContext", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                ?.GetValue(bootstrap);
        }

        private static object GetRuntimeBuildingDictionary(MatchSceneView match)
        {
            object queryContext = GetBuildingUiQueryContext(match);
            return queryContext?.GetType()
                .GetField("RuntimeBuildings", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                ?.GetValue(queryContext);
        }

        private static int? GetCurrentActiveBuildingId(MatchSceneView match)
        {
            object queryContext = GetBuildingUiQueryContext(match);
            object getter = queryContext?.GetType()
                .GetField("GetActiveBuildingId", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                ?.GetValue(queryContext);
            return getter is Delegate callback ? (int?)callback.DynamicInvoke() : null;
        }

        private static object GetMatchBootstrap(MatchSceneView match)
        {
            return match == null
                ? null
                : typeof(MatchSceneView)
                    .GetProperty("MatchBootstrap", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                    ?.GetValue(match);
        }

        private static bool SubmitNormalBuildingClick(MatchSceneView match, Vector3 screen)
        {
            object bootstrap = GetMatchBootstrap(match);
            var click = bootstrap?.GetType()
                .GetProperty("BuildingSelectionClick", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                ?.GetValue(bootstrap) as BuildingSelectionClickUtilitySystemHelper;
            object context = bootstrap?.GetType()
                .GetProperty("BuildingSelectionClickContext", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                ?.GetValue(bootstrap);
            return click != null &&
                   context is BuildingSelectionClickUtilitySystemHelper.Context typedContext &&
                   click.HandleBuildingSelectionClick(typedContext, screen);
        }

        private static void FocusCameraOnBuilding(Vector3 position)
        {
            if (Camera.main == null)
                throw new InvalidOperationException("Main camera is unavailable.");

            RtsCameraSystem cameraSystem = Unity.Entities.World.DefaultGameObjectInjectionWorld?.GetExistingSystemManaged<RtsCameraSystem>();
            if (cameraSystem == null)
                throw new InvalidOperationException("RTS camera system is unavailable.");
            cameraSystem.MoveCameraGroundCenterTo(Camera.main, position);
        }

        private static bool TryReadSelectionPanel(out GameObject panelRoot, out TMP_Text title)
        {
            panelRoot = null;
            title = null;
            MatchHudSelectionPanelView panel = UnityEngine.Object.FindAnyObjectByType<MatchHudSelectionPanelView>(FindObjectsInactive.Include);
            if (panel == null)
                return false;

            const BindingFlags Flags = BindingFlags.Instance | BindingFlags.NonPublic;
            panelRoot = typeof(MatchHudSelectionPanelView).GetField("selectedSquadPanel", Flags)?.GetValue(panel) as GameObject;
            title = typeof(MatchHudSelectionPanelView).GetField("titleText", Flags)?.GetValue(panel) as TMP_Text;
            return panelRoot != null && title != null;
        }

        private static Button GetPrivateButton(object owner, string fieldName)
        {
            return owner?.GetType()
                .GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic)
                ?.GetValue(owner) as Button;
        }

        private static BuildDrawerTabView FindBuildingsTab(BuildDrawerView drawer)
        {
            if (drawer?.Tabs == null)
                return null;

            for (int i = 0; i < drawer.Tabs.Length; i++)
            {
                BuildDrawerTabView tab = drawer.Tabs[i];
                if (tab != null && tab.Category == Game.UI.Contracts.BuildDrawerCategory.Buildings)
                    return tab;
            }

            return null;
        }

        private static bool TrySelectAffordableBuildingItem(BuildDrawerView drawer, out string itemName)
        {
            itemName = string.Empty;
            if (drawer?.ItemContentRoot == null)
                return false;

            BuildDrawerItemView[] items = drawer.ItemContentRoot.GetComponentsInChildren<BuildDrawerItemView>(true);
            for (int i = 0; i < items.Length; i++)
            {
                BuildDrawerItemView item = items[i];
                Button button = item?.SelectionButton;
                if (button == null || !item.gameObject.activeInHierarchy || !button.interactable)
                    continue;

                itemName = item.gameObject.name;
                button.onClick.Invoke();
                return true;
            }

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
                    return button;
            }

            return null;
        }

        private static string BuildStatus(string message)
        {
            return $"{message} deploy={(_deploySubmitted ? 1 : 0)} matchReady={(_matchReady ? 1 : 0)} drawer={(_drawerOpened ? 1 : 0)} buildingsTab={(_buildingsSelected ? 1 : 0)} affordable={(_affordableBuildingSelected ? 1 : 0)} requested={_requestedItemName} place={(_placementSubmitted ? 1 : 0)} confirm={(_confirmSubmitted ? 1 : 0)} newRuntimeBuildings={_newRuntimeBuildingCount} matchingRuntimeBuildings={_matchingRuntimeBuildingCount} spawnedId={_spawnedBuildingId} name={_spawnedDisplayName} origin={_spawnedOrigin} footprint={_spawnedFootprint} camera={(_cameraFocused ? 1 : 0)} selected={(_selectionSubmitted ? 1 : 0)}";
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
            EditorApplication.Exit(exitCode);
        }
    }
}
