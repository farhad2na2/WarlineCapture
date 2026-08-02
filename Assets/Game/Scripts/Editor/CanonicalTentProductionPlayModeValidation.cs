using System;
using System.IO;
using Game.Components;
using Game.Composition;
using Game.Runtime;
using Game.UI.Contracts;
using Game.UI.Runtime;
using Unity.Entities;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Game.Editor
{
    public static class CanonicalTentProductionPlayModeValidation
    {
        private const string MenuScenePath = "Assets/Game/Scenes/Menu.unity";
        private const string Marker = "[CanonicalTentProductionPlayModeValidation]";
        private const int TimeoutSeconds = 210;
        private const int StableBaselineFrames = 600;

        private static bool _completed;
        private static bool _deploySubmitted;
        private static bool _matchReady;
        private static bool _drawerOpened;
        private static bool _soldiersSelected;
        private static bool _recruitSubmitted;
        private static bool _sawTransport;
        private static bool _sawRope;
        private static bool _sawDropVisual;
        private static bool _captureRequested;
        private static CanvasGroup _captureDrawerGroup;
        private static float _captureDrawerAlpha;
        private static bool _captureDrawerBlocksRaycasts;
        private static int _captureFrame;
        private static int _frame;
        private static int _stateFrame;
        private static int _baselineUnitCount;
        private static int _stableUnitCount;
        private static int _stableUnitFrames;
        private static double _startedAt;
        private static string _evidencePath;
        private static int _pendingExitCode = int.MinValue;

        [MenuItem("Tools/Validation/Canonical Tent Full In-Map Recruit")]
        public static void RunFullInMapRecruitFlow()
        {
            try
            {
                string evidenceDirectory = Path.GetFullPath(Path.Combine(Application.dataPath, "../Build/EditorEvidence"));
                Directory.CreateDirectory(evidenceDirectory);
                _evidencePath = Path.Combine(evidenceDirectory, "CanonicalTentFullInMapRecruitDrop.png");
                if (File.Exists(_evidencePath))
                    File.Delete(_evidencePath);

                EditorSceneManager.OpenScene(MenuScenePath, OpenSceneMode.Single);
                _completed = false;
                _deploySubmitted = false;
                _matchReady = false;
                _drawerOpened = false;
                _soldiersSelected = false;
                _recruitSubmitted = false;
                _sawTransport = false;
                _sawRope = false;
                _sawDropVisual = false;
                _captureRequested = false;
                _captureDrawerGroup = null;
                _captureDrawerAlpha = 1f;
                _captureDrawerBlocksRaycasts = true;
                _captureFrame = -1;
                _frame = 0;
                _stateFrame = 0;
                _baselineUnitCount = -1;
                _stableUnitCount = -1;
                _stableUnitFrames = 0;
                _startedAt = EditorApplication.timeSinceStartup;
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

                    MatchHudRightQuickRailView rail = UnityEngine.Object.FindAnyObjectByType<MatchHudRightQuickRailView>(FindObjectsInactive.Exclude);
                    if (rail == null || rail.BuildButton == null || !rail.BuildButton.interactable)
                        return;

                    rail.BuildButton.onClick.Invoke();
                    _drawerOpened = true;
                    _stateFrame = _frame;
                    return;
                }

                BuildDrawerView drawer = UnityEngine.Object.FindAnyObjectByType<BuildDrawerView>(FindObjectsInactive.Include);
                if (drawer == null || !drawer.IsOpen)
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

                    _baselineUnitCount = CountCanonicalUnits();
                    if (_baselineUnitCount <= 0)
                    {
                        Complete(false, $"invalid canonical baseline units={_baselineUnitCount}");
                        return;
                    }

                    recruit.onClick.Invoke();
                    _recruitSubmitted = true;
                    _stateFrame = _frame;
                    return;
                }

                ObserveDeliveryPresentation();
                if (_captureRequested &&
                    (_frame - _captureFrame < 3 || !File.Exists(_evidencePath)))
                {
                    return;
                }

                RestoreDeliveryEvidenceUi();

                int currentUnitCount = CountCanonicalUnits();
                if (currentUnitCount > _baselineUnitCount)
                {
                    if (!_sawTransport || !_sawRope || !_sawDropVisual)
                    {
                        Complete(false, BuildStatus($"unit completed without full delivery presentation currentUnits={currentUnitCount}"));
                        return;
                    }

                    Complete(true, BuildStatus($"canonicalUnits={_baselineUnitCount}->{currentUnitCount} evidence={_evidencePath}"));
                }
            }
            catch (Exception exception)
            {
                Complete(false, exception.ToString());
            }
        }

        private static void ObserveDeliveryPresentation()
        {
            GameObject root = GameObject.Find("RuntimeTransports");
            if (root == null)
                return;

            Renderer[] renderers = root.GetComponentsInChildren<Renderer>(false);
            for (int i = 0; i < renderers.Length; i++)
            {
                Renderer renderer = renderers[i];
                if (renderer != null && renderer.enabled && renderer.gameObject.activeInHierarchy)
                {
                    _sawTransport = true;
                    break;
                }
            }

            LineRenderer[] ropes = root.GetComponentsInChildren<LineRenderer>(false);
            for (int i = 0; i < ropes.Length; i++)
            {
                LineRenderer rope = ropes[i];
                if (rope != null &&
                    rope.enabled &&
                    rope.gameObject.activeInHierarchy &&
                    rope.positionCount >= 2 &&
                    Vector3.Distance(rope.GetPosition(0), rope.GetPosition(1)) >= 3f)
                {
                    _sawRope = true;
                    break;
                }
            }

            Transform[] descendants = root.GetComponentsInChildren<Transform>(false);
            for (int i = 0; i < descendants.Length; i++)
            {
                Transform child = descendants[i];
                if (child != null && child.gameObject.activeInHierarchy && child.name.EndsWith("_TransportDrop", StringComparison.Ordinal))
                {
                    _sawDropVisual = true;
                    break;
                }
            }

            if (_sawTransport && _sawRope && _sawDropVisual && !_captureRequested)
            {
                CaptureDeliveryEvidence();
                _captureRequested = true;
                _captureFrame = _frame;
                Debug.Log($"{Marker} deliveryVisible=1 transport=1 rope=1 dropVisual=1 evidence={_evidencePath}");
            }
        }

        private static void CaptureDeliveryEvidence()
        {
            FocusCameraOnDelivery();

            BuildDrawerView drawer = UnityEngine.Object.FindAnyObjectByType<BuildDrawerView>(FindObjectsInactive.Include);
            if (drawer != null && drawer.IsOpen && drawer.DrawerRoot != null)
            {
                _captureDrawerGroup = drawer.DrawerRoot.GetComponent<CanvasGroup>();
                if (_captureDrawerGroup == null)
                    _captureDrawerGroup = drawer.DrawerRoot.AddComponent<CanvasGroup>();

                _captureDrawerAlpha = _captureDrawerGroup.alpha;
                _captureDrawerBlocksRaycasts = _captureDrawerGroup.blocksRaycasts;
                _captureDrawerGroup.alpha = 0f;
                _captureDrawerGroup.blocksRaycasts = false;
            }

            ScreenCapture.CaptureScreenshot(_evidencePath, 1);
        }

        private static void FocusCameraOnDelivery()
        {
            GameObject root = GameObject.Find("RuntimeTransports");
            Camera worldCamera = Camera.main;
            if (root == null || worldCamera == null)
                return;

            Transform[] descendants = root.GetComponentsInChildren<Transform>(false);
            for (int i = 0; i < descendants.Length; i++)
            {
                Transform child = descendants[i];
                if (child == null || !child.gameObject.activeInHierarchy ||
                    !child.name.EndsWith("_TransportDrop", StringComparison.Ordinal))
                {
                    continue;
                }

                World world = World.DefaultGameObjectInjectionWorld;
                RtsCameraSystem cameraSystem = world != null && world.IsCreated
                    ? world.GetExistingSystemManaged<RtsCameraSystem>()
                    : null;
                cameraSystem?.MoveCameraGroundCenterTo(worldCamera, child.position);
                return;
            }
        }

        private static void RestoreDeliveryEvidenceUi()
        {
            if (_captureDrawerGroup == null)
                return;

            _captureDrawerGroup.alpha = _captureDrawerAlpha;
            _captureDrawerGroup.blocksRaycasts = _captureDrawerBlocksRaycasts;
            _captureDrawerGroup = null;
        }

        private static int CountCanonicalUnits()
        {
            World world = World.DefaultGameObjectInjectionWorld;
            if (world == null || !world.IsCreated)
                return -1;

            EntityQuery query = world.EntityManager.CreateEntityQuery(new EntityQueryDesc
            {
                All = new[] { ComponentType.ReadOnly<UnitSourcePrefabKey>() },
                None = new[] { ComponentType.ReadOnly<Prefab>() }
            });
            int count = query.CalculateEntityCount();
            query.Dispose();
            return count;
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
                if (button == null)
                    continue;
                if (string.Equals(button.name, "DeployCommandButton", StringComparison.Ordinal) ||
                    string.Equals(button.name, "DeployOperationButton", StringComparison.Ordinal))
                {
                    return button;
                }
            }

            return null;
        }

        private static string BuildStatus(string message)
        {
            return $"{message} deploy={(_deploySubmitted ? 1 : 0)} matchReady={(_matchReady ? 1 : 0)} drawer={(_drawerOpened ? 1 : 0)} soldiers={(_soldiersSelected ? 1 : 0)} recruit={(_recruitSubmitted ? 1 : 0)} transport={(_sawTransport ? 1 : 0)} rope={(_sawRope ? 1 : 0)} dropVisual={(_sawDropVisual ? 1 : 0)} baselineUnits={_baselineUnitCount}";
        }

        private static void Complete(bool success, string message)
        {
            if (_completed)
                return;

            _completed = true;
            RestoreDeliveryEvidenceUi();
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
