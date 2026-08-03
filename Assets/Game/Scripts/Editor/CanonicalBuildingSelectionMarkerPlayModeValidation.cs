using System;
using System.Collections;
using System.IO;
using System.Reflection;
using Game.Components;
using Game.Composition;
using Game.Configs;
using Game.Runtime;
using Game.UI.Runtime;
using TMPro;
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
        private const string BarracksConfigPath = "Assets/Game/Configs/Prefabs/Prefab_BuildingDefinition_Building_Barrack_Config.asset";
        private const string Marker = "[CanonicalBuildingSelectionMarkerPlayModeValidation]";
        private const int TimeoutSeconds = 180;

        private static bool _completed;
        private static bool _deploySubmitted;
        private static bool _matchReady;
        private static bool _cameraFocused;
        private static bool _selectionSubmitted;
        private static bool _captureRequested;
        private static bool _barracksValidated;
        private static bool _ownershipCameraFocused;
        private static bool _ownershipClickSubmitted;
        private static bool _ownershipCaptureRequested;
        private static int _frame;
        private static int _stateFrame;
        private static int _captureFrame;
        private static int _ownershipCaptureFrame;
        private static double _startedAt;
        private static string _evidencePath;
        private static string _ownershipEvidencePath;
        private static object _tent;
        private static GameObject _tentInstance;
        private static int _tentId;
        private static Vector3 _tentPosition;
        private static Vector3 _tentPresentationSize;
        private static float _tentPresentationYaw;
        private static Vector2Int _originCell;
        private static Vector2Int _footprintCells;
        private static Vector2Int _conflictingTentCell;
        private static bool _hasConflictingTentCell;
        private static object _contractorTent;
        private static int _contractorTentId;
        private static Vector3 _contractorTentPosition;
        private static object _buildingUiQueryContext;
        private static Sprite _expectedPortrait;
        private static Vector3 _markerBaseRendererSize;
        private static int _pendingExitCode = int.MinValue;

        [MenuItem("Tools/Validation/Canonical Building Selection Marker")]
        public static void Run()
        {
            try
            {
                BuildingDefinitionAuthoringConfig config = AssetDatabase.LoadAssetAtPath<BuildingDefinitionAuthoringConfig>(BarracksConfigPath);
                if (config == null)
                    throw new InvalidOperationException($"Barracks config is missing at {BarracksConfigPath}.");
                _expectedPortrait = config.PortraitActionSprite != null
                    ? config.PortraitActionSprite
                    : config.PortraitCardSprite;
                if (_expectedPortrait == null)
                    throw new InvalidOperationException("Barracks selection portrait is not configured.");

                _markerBaseRendererSize = ResolveMarkerBaseRendererSize();
                string evidenceDirectory = Path.GetFullPath(Path.Combine(Application.dataPath, "../Build/EditorEvidence"));
                Directory.CreateDirectory(evidenceDirectory);
                _evidencePath = Path.Combine(evidenceDirectory, "CanonicalBarracksSelectionPresentation.png");
                _ownershipEvidencePath = Path.Combine(evidenceDirectory, "CanonicalContractorTentClickOwnership.png");
                if (File.Exists(_evidencePath))
                    File.Delete(_evidencePath);
                if (File.Exists(_ownershipEvidencePath))
                    File.Delete(_ownershipEvidencePath);

                EditorSceneManager.OpenScene(MenuScenePath, OpenSceneMode.Single);
                _completed = false;
                _deploySubmitted = false;
                _matchReady = false;
                _cameraFocused = false;
                _selectionSubmitted = false;
                _captureRequested = false;
                _barracksValidated = false;
                _ownershipCameraFocused = false;
                _ownershipClickSubmitted = false;
                _ownershipCaptureRequested = false;
                _frame = 0;
                _stateFrame = 0;
                _captureFrame = -1;
                _ownershipCaptureFrame = -1;
                _startedAt = EditorApplication.timeSinceStartup;
                _tent = null;
                _tentInstance = null;
                _tentId = 0;
                _tentPosition = default;
                _originCell = default;
                _footprintCells = default;
                _conflictingTentCell = default;
                _hasConflictingTentCell = false;
                _contractorTent = null;
                _contractorTentId = 0;
                _contractorTentPosition = default;
                _buildingUiQueryContext = null;
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
                    if (_frame - _stateFrame < 120 || !TryFindSelectionTargets(match))
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

                    // Phase one selects the known runtime Barracks by stable id so its
                    // marker/portrait assertion is independent from screen-hit ownership.
                    // Phase two below is the real conflicting-cell screen-click gate.
                    if (!TrySelectKnownBuilding(_buildingUiQueryContext, _tentId))
                    {
                        Complete(false, BuildStatus("Barracks setup selection rejected"));
                        return;
                    }

                    _selectionSubmitted = true;
                    _stateFrame = _frame;
                    return;
                }

                if (_frame - _stateFrame < 30)
                    return;

                if (!_barracksValidated)
                {
                    GameObject markerObject = GameObject.Find("BuildingSelectionMarkerRuntime");
                    if (markerObject == null || !markerObject.activeInHierarchy)
                    {
                        if (_frame - _stateFrame > 300)
                            Complete(false, BuildStatus("runtime Barracks marker did not become visible"));
                        return;
                    }

                    if (!TryGetGrid(match, out GridConfig grid))
                    {
                        Complete(false, BuildStatus("runtime grid unavailable"));
                        return;
                    }

                    Vector3 expectedCenter = new(
                        _tentPosition.x,
                        markerObject.transform.position.y,
                        _tentPosition.z);
                    Vector3 expectedScale = new(
                        Mathf.Max(grid.CellSize, _tentPresentationSize.x) / Mathf.Max(0.001f, _markerBaseRendererSize.x),
                        1f,
                        Mathf.Max(grid.CellSize, _tentPresentationSize.z) / Mathf.Max(0.001f, _markerBaseRendererSize.z));
                    Vector3 actualPosition = markerObject.transform.position;
                    Vector3 actualScale = markerObject.transform.localScale;
                    float yawDelta = Mathf.Abs(Mathf.DeltaAngle(
                        markerObject.transform.eulerAngles.y,
                        _tentPresentationYaw));
                    float centerTolerance = Mathf.Max(0.05f, grid.CellSize * 0.05f);
                    if (Mathf.Abs(actualPosition.x - expectedCenter.x) > centerTolerance ||
                        Mathf.Abs(actualPosition.z - expectedCenter.z) > centerTolerance ||
                        Mathf.Abs(actualScale.x - expectedScale.x) > 0.02f ||
                        Mathf.Abs(actualScale.z - expectedScale.z) > 0.02f ||
                        yawDelta > 0.1f)
                    {
                        Complete(false, BuildStatus(
                            $"Barracks marker mismatch expectedCenter={expectedCenter} actualPosition={actualPosition} expectedScale={expectedScale} actualScale={actualScale} yaw={markerObject.transform.eulerAngles.y}"));
                        return;
                    }

                    if (HasActiveSelectionObjectOutline(_tentInstance))
                    {
                        Complete(false, BuildStatus(
                            "map-authored Barracks created an object outline from shared renderer geometry"));
                        return;
                    }

                    MatchHudSelectionPanelView panel = UnityEngine.Object.FindAnyObjectByType<MatchHudSelectionPanelView>(FindObjectsInactive.Include);
                    if (!TryReadPanel(panel, out GameObject panelRoot, out Image portraitImage, out TMP_Text titleText) ||
                        panelRoot == null || !panelRoot.activeInHierarchy)
                    {
                        Complete(false, BuildStatus("Barracks selection panel did not become visible"));
                        return;
                    }
                    if (portraitImage.sprite != _expectedPortrait)
                    {
                        Complete(false, BuildStatus($"wrong Barracks portrait expected={_expectedPortrait.name} actual={portraitImage.sprite?.name ?? "null"} title={titleText?.text ?? "null"}"));
                        return;
                    }
                    if (titleText == null || !titleText.text.Contains("Barracks", StringComparison.OrdinalIgnoreCase))
                    {
                        Complete(false, BuildStatus($"wrong selected-owner title title={titleText?.text ?? "null"}"));
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

                    _barracksValidated = true;
                    _stateFrame = _frame;
                    return;
                }

                if (!_ownershipCameraFocused)
                {
                    FocusCameraOnTent(_contractorTentPosition);
                    _ownershipCameraFocused = true;
                    _stateFrame = _frame;
                    return;
                }

                if (!_ownershipClickSubmitted)
                {
                    if (_frame - _stateFrame < 120)
                        return;

                    Vector3 contractorScreen = Camera.main != null
                        ? Camera.main.WorldToScreenPoint(_contractorTentPosition)
                        : new Vector3(-1f, -1f, -1f);
                    if (contractorScreen.z <= 0f || !SubmitBuildingClick(match, contractorScreen, _originCell))
                    {
                        Complete(false, BuildStatus($"Contractor Tent conflicting-cell click rejected screen={contractorScreen}"));
                        return;
                    }

                    _ownershipClickSubmitted = true;
                    _stateFrame = _frame;
                    return;
                }

                if (_frame - _stateFrame < 30)
                    return;

                GameObject ownershipMarker = GameObject.Find("BuildingSelectionMarkerRuntime");
                if (ownershipMarker == null || !ownershipMarker.activeInHierarchy ||
                    Mathf.Abs(ownershipMarker.transform.position.x - _contractorTentPosition.x) > 0.05f ||
                    Mathf.Abs(ownershipMarker.transform.position.z - _contractorTentPosition.z) > 0.05f)
                {
                    Complete(false, BuildStatus(
                        $"Contractor Tent did not own conflicting-cell marker expectedCenter={_contractorTentPosition} actual={ownershipMarker?.transform.position ?? default}"));
                    return;
                }

                MatchHudSelectionPanelView ownershipPanel = UnityEngine.Object.FindAnyObjectByType<MatchHudSelectionPanelView>(FindObjectsInactive.Include);
                string expectedOrigin = $"({_conflictingTentCell.x},{_conflictingTentCell.y})";
                if (!TryReadPanel(ownershipPanel, out GameObject ownershipPanelRoot, out _, out TMP_Text ownershipTitle) ||
                    ownershipPanelRoot == null || !ownershipPanelRoot.activeInHierarchy ||
                    ownershipTitle == null ||
                    !ownershipTitle.text.Contains("Contractor Tent", StringComparison.OrdinalIgnoreCase) ||
                    !ownershipTitle.text.Contains(expectedOrigin, StringComparison.Ordinal))
                {
                    Complete(false, BuildStatus($"Contractor Tent ownership panel mismatch expectedOrigin={expectedOrigin} title={ownershipTitle?.text ?? "null"}"));
                    return;
                }

                if (!_ownershipCaptureRequested)
                {
                    ScreenCapture.CaptureScreenshot(_ownershipEvidencePath, 1);
                    _ownershipCaptureRequested = true;
                    _ownershipCaptureFrame = _frame;
                    return;
                }

                if (_frame - _ownershipCaptureFrame < 3 || !File.Exists(_ownershipEvidencePath))
                    return;

                Complete(true, BuildStatus(
                    $"barracksMarker=placementPrefabGeometry presentationSize={_tentPresentationSize} presentationYaw={_tentPresentationYaw:F2} barracksPortrait={_expectedPortrait.name} contractorTentClickOwner={_contractorTentId} barracksEvidence={_evidencePath} ownershipEvidence={_ownershipEvidencePath}"));
            }
            catch (Exception exception)
            {
                Complete(false, exception.ToString());
            }
        }

        private static bool TryFindSelectionTargets(MatchSceneView match)
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
            _buildingUiQueryContext = queryContext;

            foreach (object entry in entries)
            {
                object building = entry?.GetType().GetProperty("Value")?.GetValue(entry);
                object definition = building?.GetType().GetField("Definition")?.GetValue(building);
                string display = definition?.GetType().GetField("DisplayName")?.GetValue(definition) as string;
                if (string.IsNullOrWhiteSpace(display))
                    continue;

                bool hasOwner = (bool?)building.GetType().GetProperty("HasOwnerFaction")?.GetValue(building) ?? false;
                byte owner = (byte?)building.GetType().GetProperty("OwnerFactionId")?.GetValue(building) ?? 0;
                if (hasOwner && !FactionIdentity.IsPlayerControlled(owner))
                    continue;

                GameObject instance = building.GetType().GetField("Instance")?.GetValue(building) as GameObject;
                if (instance == null)
                    continue;

                int buildingId = (int?)building.GetType().GetProperty("Id")?.GetValue(building) ?? 0;
                bool hasPresentationCenter = TryResolvePresentationCenter(instance, definition, out Vector3 presentationCenter);

                if (_contractorTent == null && display.Contains("Contractor Tent", StringComparison.OrdinalIgnoreCase))
                {
                    _conflictingTentCell = (Vector2Int?)building.GetType().GetField("OriginCell")?.GetValue(building) ?? default;
                    if (hasPresentationCenter)
                    {
                        _contractorTent = building;
                        _contractorTentId = buildingId;
                        _contractorTentPosition = presentationCenter;
                        _hasConflictingTentCell = true;
                    }
                }

                if (!display.Contains("Barracks", StringComparison.OrdinalIgnoreCase) || _tent != null)
                    continue;

                _tent = building;
                _tentInstance = instance;
                _tentId = buildingId;
                if (!hasPresentationCenter)
                {
                    _tent = null;
                    continue;
                }
                _tentPosition = presentationCenter;
                if (!instance.TryGetComponent(out MapAuthoredBuildingVisualComponent authoredVisual) ||
                    !authoredVisual.HasPresentationGeometry)
                {
                    _tent = null;
                    continue;
                }
                _tentPresentationSize = authoredVisual.PresentationWorldSize;
                _tentPresentationYaw = authoredVisual.PresentationYawDegrees;
                _originCell = (Vector2Int?)building.GetType().GetField("OriginCell")?.GetValue(building) ?? default;
                _footprintCells = (Vector2Int?)definition.GetType().GetField("FootprintCells")?.GetValue(definition) ?? default;
                if (_footprintCells.x <= 0 || _footprintCells.y <= 0)
                    _tent = null;
            }

            return _tent != null && _contractorTent != null && _hasConflictingTentCell;
        }

        private static bool TrySelectKnownBuilding(object queryContext, int buildingId)
        {
            FieldInfo activeIdField = queryContext?.GetType()
                .GetField("GetActiveBuildingId", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            Delegate activeIdDelegate = activeIdField?.GetValue(queryContext) as Delegate;
            RuntimeBuildingCollection<RuntimeBuildingEntity> runtimeBuildings =
                FindRuntimeBuildingCollection(activeIdDelegate?.Target, 0);
            if (runtimeBuildings == null || !runtimeBuildings.ContainsBuilding(buildingId))
                return false;

            runtimeBuildings.SelectBuilding(buildingId);
            return runtimeBuildings.CurrentActiveBuildingId == buildingId;
        }

        private static RuntimeBuildingCollection<RuntimeBuildingEntity> FindRuntimeBuildingCollection(object value, int depth)
        {
            if (value is RuntimeBuildingCollection<RuntimeBuildingEntity> runtimeBuildings)
                return runtimeBuildings;
            if (value == null || depth > 3)
                return null;

            Type type = value.GetType();
            foreach (FieldInfo field in type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
            {
                object child;
                try
                {
                    child = field.GetValue(value);
                }
                catch
                {
                    continue;
                }

                if (child is RuntimeBuildingCollection<RuntimeBuildingEntity> collection)
                    return collection;

                Type childType = child?.GetType();
                if (child == null ||
                    childType.IsPrimitive ||
                    childType.IsEnum ||
                    child is string ||
                    child is Delegate ||
                    !(childType.Namespace?.StartsWith("Game", StringComparison.Ordinal) == true ||
                      childType.Name.Contains("DisplayClass", StringComparison.Ordinal)))
                {
                    continue;
                }

                RuntimeBuildingCollection<RuntimeBuildingEntity> nested = FindRuntimeBuildingCollection(child, depth + 1);
                if (nested != null)
                    return nested;
            }

            return null;
        }

        private static bool TryResolvePresentationBounds(GameObject instance, object definition, out Bounds bounds)
        {
            if (TryCalculateRendererBounds(instance, out bounds))
                return true;

            bool hasLocalBounds = (bool?)definition?.GetType().GetField("HasLocalBounds")?.GetValue(definition) ?? false;
            object localBoundsValue = definition?.GetType().GetField("LocalBounds")?.GetValue(definition);
            if (!hasLocalBounds || localBoundsValue is not Bounds localBounds || instance == null)
            {
                bounds = default;
                return false;
            }

            bool hasPoint = false;
            bounds = default;
            for (int corner = 0; corner < 8; corner++)
            {
                Vector3 local = new(
                    (corner & 1) == 0 ? localBounds.min.x : localBounds.max.x,
                    (corner & 2) == 0 ? localBounds.min.y : localBounds.max.y,
                    (corner & 4) == 0 ? localBounds.min.z : localBounds.max.z);
                Vector3 world = instance.transform.TransformPoint(local);
                if (hasPoint)
                    bounds.Encapsulate(world);
                else
                {
                    bounds = new Bounds(world, Vector3.zero);
                    hasPoint = true;
                }
            }

            return hasPoint;
        }

        private static bool TryResolvePresentationCenter(GameObject instance, object definition, out Vector3 center)
        {
            center = default;
            if (instance != null &&
                instance.TryGetComponent(out MapAuthoredBuildingVisualComponent authoredVisual) &&
                authoredVisual.HasPresentationWorldCenter)
            {
                center = authoredVisual.PresentationWorldCenter;
                return true;
            }

            if (!TryResolvePresentationBounds(instance, definition, out Bounds bounds))
                return false;

            center = bounds.center;
            return true;
        }

        private static bool TryCalculateRendererBounds(GameObject instance, out Bounds bounds)
        {
            bounds = default;
            Renderer[] renderers = instance != null ? instance.GetComponentsInChildren<Renderer>(false) : null;
            bool hasBounds = false;
            if (renderers == null)
                return false;

            for (int i = 0; i < renderers.Length; i++)
            {
                Renderer renderer = renderers[i];
                if (renderer == null || !renderer.enabled || renderer.name.Contains("SelectionObjectOutline", StringComparison.OrdinalIgnoreCase))
                    continue;
                if (hasBounds)
                    bounds.Encapsulate(renderer.bounds);
                else
                {
                    bounds = renderer.bounds;
                    hasBounds = true;
                }
            }

            return hasBounds;
        }

        private static bool HasActiveSelectionObjectOutline(GameObject instance)
        {
            if (instance == null)
                return false;

            Renderer[] renderers = instance.GetComponentsInChildren<Renderer>(true);
            for (int i = 0; i < renderers.Length; i++)
            {
                Renderer renderer = renderers[i];
                if (renderer == null || !renderer.gameObject.activeInHierarchy)
                    continue;

                Transform current = renderer.transform;
                while (current != null && current != instance.transform.parent)
                {
                    if (current.name.Contains("SelectionObjectOutline", StringComparison.OrdinalIgnoreCase))
                        return true;
                    current = current.parent;
                }
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

        private static bool SubmitBuildingClick(MatchSceneView match, Vector3 screen, Vector2Int cell)
        {
            object bootstrap = GetMatchBootstrap(match);
            object context = bootstrap?.GetType()
                .GetProperty("BuildingSelectionClickContext", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                ?.GetValue(bootstrap);
            return context is BuildingSelectionClickUtilitySystemHelper.Context typedContext &&
                   typedContext.HandleCellSelection != null &&
                   typedContext.HandleCellSelection(screen, cell);
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

        private static bool TryReadPanel(
            MatchHudSelectionPanelView panel,
            out GameObject panelRoot,
            out Image portraitImage,
            out TMP_Text titleText)
        {
            panelRoot = null;
            portraitImage = null;
            titleText = null;
            if (panel == null)
                return false;

            const BindingFlags Flags = BindingFlags.Instance | BindingFlags.NonPublic;
            panelRoot = typeof(MatchHudSelectionPanelView).GetField("selectedSquadPanel", Flags)?.GetValue(panel) as GameObject;
            portraitImage = typeof(MatchHudSelectionPanelView).GetField("selectedPortraitImage", Flags)?.GetValue(panel) as Image;
            titleText = typeof(MatchHudSelectionPanelView).GetField("titleText", Flags)?.GetValue(panel) as TMP_Text;
            return panelRoot != null && portraitImage != null && titleText != null;
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
            return $"{message} deploy={(_deploySubmitted ? 1 : 0)} matchReady={(_matchReady ? 1 : 0)} barracks={(_tent == null ? 0 : 1)} barracksId={_tentId} origin={_originCell} footprint={_footprintCells} contractorTentId={_contractorTentId} contractorTentCell={_conflictingTentCell} barracksValidated={(_barracksValidated ? 1 : 0)} ownershipClicked={(_ownershipClickSubmitted ? 1 : 0)}";
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
