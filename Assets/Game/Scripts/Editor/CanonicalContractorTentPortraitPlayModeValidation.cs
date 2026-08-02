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
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Game.Editor
{
    public static class CanonicalContractorTentPortraitPlayModeValidation
    {
        private const string MenuScenePath = "Assets/Game/Scenes/Menu.unity";
        private const string ConfigPath = "Assets/Game/Configs/Prefabs/Prefab_BuildingDefinition_Tent_Contractor_Config.asset";
        private const string Marker = "[CanonicalContractorTentPortraitPlayModeValidation]";
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
        private static string _tentPrefabName;
        private static Sprite _expectedPortrait;
        private static int _pendingExitCode = int.MinValue;

        [MenuItem("Tools/Validation/Canonical Contractor Tent Portrait")]
        public static void Run()
        {
            try
            {
                BuildingDefinitionAuthoringConfig config = AssetDatabase.LoadAssetAtPath<BuildingDefinitionAuthoringConfig>(ConfigPath);
                if (config == null || config.PortraitSprite == null)
                    throw new InvalidOperationException($"Contractor Tent portrait config is missing at {ConfigPath}.");

                _expectedPortrait = config.PortraitActionSprite != null
                    ? config.PortraitActionSprite
                    : config.PortraitCardSprite;
                string evidenceDirectory = Path.GetFullPath(Path.Combine(Application.dataPath, "../Build/EditorEvidence"));
                Directory.CreateDirectory(evidenceDirectory);
                _evidencePath = Path.Combine(evidenceDirectory, "CanonicalContractorTentSelectionPortrait.png");
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
                _tentPrefabName = string.Empty;
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
                    if (_frame - _stateFrame < 120 ||
                        !TryFindContractorTent(match, out _tent, out _tentId, out _tentPosition, out _tentPrefabName))
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

                    Vector3 screen = ResolveScreenPosition(_tentPosition);
                    if (screen.z <= 0f || screen.x < 0f || screen.x > Screen.width || screen.y < 0f || screen.y > Screen.height)
                    {
                        Complete(false, BuildStatus($"Contractor Tent was outside the camera screen position={screen}"));
                        return;
                    }

                    if (!SubmitNormalBuildingClick(match, screen))
                    {
                        Complete(false, BuildStatus("managed building click path rejected the Contractor Tent"));
                        return;
                    }

                    _selectionSubmitted = true;
                    _stateFrame = _frame;
                    return;
                }

                if (_frame - _stateFrame < 30)
                    return;

                MatchHudSelectionPanelView panel = UnityEngine.Object.FindAnyObjectByType<MatchHudSelectionPanelView>(FindObjectsInactive.Include);
                if (!TryReadPanel(panel, out GameObject panelRoot, out Image portraitImage, out TMP_Text titleText) ||
                    panelRoot == null || !panelRoot.activeInHierarchy)
                {
                    if (_frame - _stateFrame > 300)
                        Complete(false, BuildStatus("selection panel did not become visible"));
                    return;
                }

                if (portraitImage.sprite != _expectedPortrait)
                {
                    Complete(false, BuildStatus($"wrong portrait expected={_expectedPortrait.name} actual={portraitImage.sprite?.name ?? "null"} title={titleText?.text}"));
                    return;
                }

                if (titleText == null || !titleText.text.Contains("Contractor Tent", StringComparison.OrdinalIgnoreCase))
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

                Complete(true,
                    BuildStatus($"selected=1 panel=1 title=\"{titleText.text}\" portrait={portraitImage.sprite.name} evidence={_evidencePath}"));
            }
            catch (Exception exception)
            {
                Complete(false, exception.ToString());
            }
        }

        private static bool TryFindContractorTent(
            MatchSceneView match,
            out object tent,
            out int buildingId,
            out Vector3 worldPosition,
            out string prefabName)
        {
            tent = null;
            buildingId = 0;
            worldPosition = default;
            prefabName = string.Empty;
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
                    (!display.Contains("Contractor Tent", StringComparison.OrdinalIgnoreCase) &&
                     !display.Contains("Tent_Contractor", StringComparison.OrdinalIgnoreCase)))
                    continue;

                bool hasOwner = (bool?)building.GetType().GetProperty("HasOwnerFaction")?.GetValue(building) ?? false;
                byte owner = (byte?)building.GetType().GetProperty("OwnerFactionId")?.GetValue(building) ?? 0;
                if (hasOwner && !FactionIdentity.IsPlayerControlled(owner))
                    continue;

                GameObject instance = building.GetType().GetField("Instance")?.GetValue(building) as GameObject;
                GameObject prefab = definition.GetType().GetField("Prefab")?.GetValue(definition) as GameObject;
                if (instance == null)
                    continue;

                tent = building;
                buildingId = (int?)building.GetType().GetProperty("Id")?.GetValue(building) ?? 0;
                worldPosition = instance.transform.position;
                prefabName = prefab != null ? prefab.name : "<null>";
                return true;
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

        private static void FocusCameraOnTent(Vector3 position)
        {
            if (Camera.main == null)
                throw new InvalidOperationException("Main camera is unavailable.");

            RtsCameraSystem cameraSystem = World.DefaultGameObjectInjectionWorld?.GetExistingSystemManaged<RtsCameraSystem>();
            if (cameraSystem == null)
                throw new InvalidOperationException("RTS camera system is unavailable.");
            cameraSystem.MoveCameraGroundCenterTo(Camera.main, position);
        }

        private static Vector3 ResolveScreenPosition(Vector3 position)
        {
            return Camera.main == null
                ? new Vector3(-1f, -1f, -1f)
                : Camera.main.WorldToScreenPoint(position);
        }

        private static bool SubmitNormalBuildingClick(MatchSceneView match, Vector3 screen)
        {
            object bootstrap = GetMatchBootstrap(match);
            if (bootstrap == null)
                return false;

            var click = bootstrap.GetType()
                .GetProperty("BuildingSelectionClick", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                ?.GetValue(bootstrap) as BuildingSelectionClickUtilitySystemHelper;
            object context = bootstrap.GetType()
                .GetProperty("BuildingSelectionClickContext", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                ?.GetValue(bootstrap);
            return click != null &&
                   context is BuildingSelectionClickUtilitySystemHelper.Context typedContext &&
                   click.HandleBuildingSelectionClick(typedContext, screen);
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
            return $"{message} deploy={(_deploySubmitted ? 1 : 0)} matchReady={(_matchReady ? 1 : 0)} tent={(_tent == null ? 0 : 1)} tentId={_tentId} prefab={_tentPrefabName} camera={(_cameraFocused ? 1 : 0)} clicked={(_selectionSubmitted ? 1 : 0)}";
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
