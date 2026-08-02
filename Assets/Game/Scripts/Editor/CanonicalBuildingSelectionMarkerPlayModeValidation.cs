using System;
using System.Collections;
using System.IO;
using System.Reflection;
using Game.Components;
using Game.Composition;
using Game.Runtime;
using Unity.Entities;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Game.Editor
{
    public static class CanonicalBuildingSelectionMarkerPlayModeValidation
    {
        private const string MenuScenePath = "Assets/Game/Scenes/Menu.unity";
        private const string MarkerPrefabPath = "Assets/Game/Prefabs/Buildings/BuildingSelectionMarker.prefab";
        private const string Marker = "[CanonicalBuildingSelectionMarkerPlayModeValidation]";
        private const int TimeoutSeconds = 180;

        private static bool _completed;
        private static bool _deploySubmitted;
        private static bool _matchReady;
        private static bool _cameraFocused;
        private static bool _selectionSubmitted;
        private static bool _captureRequested;
        private static int _frame;
        private static int _stateFrame;
        private static int _captureFrame;
        private static double _startedAt;
        private static string _evidencePath;
        private static object _tent;
        private static int _tentId;
        private static Vector3 _tentPosition;
        private static Vector2Int _originCell;
        private static Vector2Int _footprintCells;
        private static Vector3 _markerBaseRendererSize;
        private static int _pendingExitCode = int.MinValue;

        [MenuItem("Tools/Validation/Canonical Building Selection Marker")]
        public static void Run()
        {
            try
            {
                _markerBaseRendererSize = ResolveMarkerBaseRendererSize();
                string evidenceDirectory = Path.GetFullPath(Path.Combine(Application.dataPath, "../Build/EditorEvidence"));
                Directory.CreateDirectory(evidenceDirectory);
                _evidencePath = Path.Combine(evidenceDirectory, "CanonicalBuildingSelectionMarkerFootprint.png");
                if (File.Exists(_evidencePath))
                    File.Delete(_evidencePath);

                EditorSceneManager.OpenScene(MenuScenePath, OpenSceneMode.Single);
                _completed = false;
                _deploySubmitted = false;
                _matchReady = false;
                _cameraFocused = false;
                _selectionSubmitted = false;
                _captureRequested = false;
                _frame = 0;
                _stateFrame = 0;
                _captureFrame = -1;
                _startedAt = EditorApplication.timeSinceStartup;
                _tent = null;
                _tentId = 0;
                _tentPosition = default;
                _originCell = default;
                _footprintCells = default;
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

                if (_tent == null)
                {
                    if (_frame - _stateFrame < 120 || !TryFindContractorTent(match))
                        return;

                    _stateFrame = _frame;
                    return;
                }

                if (!_cameraFocused)
                {
                    FocusCameraOnTent(_tentPosition);
                    _cameraFocused = true;
                    _stateFrame = _frame;
                    return;
                }

                if (!_selectionSubmitted)
                {
                    if (_frame - _stateFrame < 120)
                        return;

                    Vector3 screen = Camera.main != null
                        ? Camera.main.WorldToScreenPoint(_tentPosition)
                        : new Vector3(-1f, -1f, -1f);
                    if (screen.z <= 0f || !SubmitNormalBuildingClick(match, screen))
                    {
                        Complete(false, BuildStatus($"managed building click rejected screen={screen}"));
                        return;
                    }

                    _selectionSubmitted = true;
                    _stateFrame = _frame;
                    return;
                }

                if (_frame - _stateFrame < 30)
                    return;

                GameObject markerObject = GameObject.Find("BuildingSelectionMarkerRuntime");
                if (markerObject == null || !markerObject.activeInHierarchy)
                {
                    if (_frame - _stateFrame > 300)
                        Complete(false, BuildStatus("runtime building marker did not become visible"));
                    return;
                }

                if (!TryGetGrid(match, out GridConfig grid))
                {
                    Complete(false, BuildStatus("runtime grid unavailable"));
                    return;
                }

                Vector3 expectedCenter = new(
                    grid.Origin.x + (_originCell.x + _footprintCells.x * 0.5f) * grid.CellSize,
                    markerObject.transform.position.y,
                    grid.Origin.z + (_originCell.y + _footprintCells.y * 0.5f) * grid.CellSize);
                Vector3 expectedScale = new(
                    Mathf.Max(grid.CellSize, _footprintCells.x * grid.CellSize) / Mathf.Max(0.001f, _markerBaseRendererSize.x),
                    1f,
                    Mathf.Max(grid.CellSize, _footprintCells.y * grid.CellSize) / Mathf.Max(0.001f, _markerBaseRendererSize.z));
                Vector3 actualPosition = markerObject.transform.position;
                Vector3 actualScale = markerObject.transform.localScale;
                float centerTolerance = Mathf.Max(0.05f, grid.CellSize * 0.05f);
                if (Mathf.Abs(actualPosition.x - expectedCenter.x) > centerTolerance ||
                    Mathf.Abs(actualPosition.z - expectedCenter.z) > centerTolerance ||
                    Mathf.Abs(actualScale.x - expectedScale.x) > 0.02f ||
                    Mathf.Abs(actualScale.z - expectedScale.z) > 0.02f ||
                    Mathf.Abs(markerObject.transform.eulerAngles.y) > 0.1f)
                {
                    Complete(false, BuildStatus(
                        $"marker mismatch expectedCenter={expectedCenter} actualPosition={actualPosition} expectedScale={expectedScale} actualScale={actualScale} yaw={markerObject.transform.eulerAngles.y}"));
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
                    $"marker=1 footprint={_footprintCells.x}x{_footprintCells.y} center={actualPosition} scale={actualScale} base={_markerBaseRendererSize} evidence={_evidencePath}"));
            }
            catch (Exception exception)
            {
                Complete(false, exception.ToString());
            }
        }

        private static bool TryFindContractorTent(MatchSceneView match)
        {
            object bootstrap = GetMatchBootstrap(match);
            object queryContext = bootstrap?.GetType()
                .GetProperty("BuildingUiQueryContext", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                ?.GetValue(bootstrap);
            object runtimeBuildings = queryContext?.GetType()
                .GetField("RuntimeBuildings", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                ?.GetValue(queryContext);
            if (runtimeBuildings is not IEnumerable entries)
                return false;

            foreach (object entry in entries)
            {
                object building = entry?.GetType().GetProperty("Value")?.GetValue(entry);
                object definition = building?.GetType().GetField("Definition")?.GetValue(building);
                string display = definition?.GetType().GetField("DisplayName")?.GetValue(definition) as string;
                if (string.IsNullOrWhiteSpace(display) ||
                    !display.Contains("Contractor Tent", StringComparison.OrdinalIgnoreCase))
                    continue;

                bool hasOwner = (bool?)building.GetType().GetProperty("HasOwnerFaction")?.GetValue(building) ?? false;
                byte owner = (byte?)building.GetType().GetProperty("OwnerFactionId")?.GetValue(building) ?? 0;
                if (hasOwner && !FactionIdentity.IsPlayerControlled(owner))
                    continue;

                GameObject instance = building.GetType().GetField("Instance")?.GetValue(building) as GameObject;
                if (instance == null)
                    continue;

                _tent = building;
                _tentId = (int?)building.GetType().GetProperty("Id")?.GetValue(building) ?? 0;
                _tentPosition = instance.transform.position;
                _originCell = (Vector2Int?)building.GetType().GetField("OriginCell")?.GetValue(building) ?? default;
                _footprintCells = (Vector2Int?)definition.GetType().GetField("FootprintCells")?.GetValue(definition) ?? default;
                return _footprintCells.x > 0 && _footprintCells.y > 0;
            }

            return false;
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

        private static bool TryGetGrid(MatchSceneView match, out GridConfig grid)
        {
            grid = default;
            object bootstrap = GetMatchBootstrap(match);
            object context = bootstrap?.GetType()
                .GetProperty("BuildingSelectionClickContext", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                ?.GetValue(bootstrap);
            return context is BuildingSelectionClickUtilitySystemHelper.Context typedContext &&
                   typedContext.TryGetGrid != null &&
                   typedContext.TryGetGrid(out grid);
        }

        private static void FocusCameraOnTent(Vector3 position)
        {
            if (Camera.main == null)
                throw new InvalidOperationException("Main camera is unavailable.");

            RtsCameraSystem cameraSystem = World.DefaultGameObjectInjectionWorld?.GetExistingSystemManaged<RtsCameraSystem>();
            if (cameraSystem == null)
                throw new InvalidOperationException("RTS camera system is unavailable.");
            cameraSystem.MoveCameraGroundCenterTo(Camera.main, position);
        }

        private static Vector3 ResolveMarkerBaseRendererSize()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(MarkerPrefabPath);
            if (prefab == null)
                throw new InvalidOperationException($"Missing marker prefab at {MarkerPrefabPath}.");

            GameObject instance = UnityEngine.Object.Instantiate(prefab);
            try
            {
                Renderer[] renderers = instance.GetComponentsInChildren<Renderer>(true);
                bool hasBounds = false;
                Bounds bounds = default;
                for (int i = 0; i < renderers.Length; i++)
                {
                    Renderer renderer = renderers[i];
                    if (renderer == null)
                        continue;
                    if (hasBounds)
                        bounds.Encapsulate(renderer.bounds);
                    else
                    {
                        bounds = renderer.bounds;
                        hasBounds = true;
                    }
                }

                if (!hasBounds)
                    throw new InvalidOperationException("Marker prefab has no renderer bounds.");
                return bounds.size;
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(instance);
            }
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
            return $"{message} deploy={(_deploySubmitted ? 1 : 0)} matchReady={(_matchReady ? 1 : 0)} tent={(_tent == null ? 0 : 1)} tentId={_tentId} origin={_originCell} footprint={_footprintCells} camera={(_cameraFocused ? 1 : 0)} clicked={(_selectionSubmitted ? 1 : 0)}";
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
