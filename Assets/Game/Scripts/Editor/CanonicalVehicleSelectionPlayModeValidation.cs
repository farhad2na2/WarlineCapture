using System;
using System.IO;
using System.Reflection;
using Game.Components;
using Game.Composition;
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
    public static class CanonicalVehicleSelectionPlayModeValidation
    {
        private const string MenuScenePath = "Assets/Game/Scenes/Menu.unity";
        private const string Marker = "[CanonicalVehicleSelectionPlayModeValidation]";
        private const int TimeoutSeconds = 180;
        private const int StableVehicleFrames = 180;

        private static bool _completed;
        private static bool _deploySubmitted;
        private static bool _matchReady;
        private static bool _cameraFocused;
        private static bool _worldSelectionSubmitted;
        private static bool _worldCaptureRequested;
        private static bool _worldAccepted;
        private static bool _rosterSelectionSubmitted;
        private static bool _rosterCaptureRequested;
        private static int _frame;
        private static int _stateFrame;
        private static int _stableVehicleCount;
        private static int _stableVehicleFrames;
        private static bool _packedVehicleDiagnosticEmitted;
        private static int _captureFrame;
        private static int _worldSelectedCount;
        private static int _rosterSelectedCount;
        private static double _startedAt;
        private static string _worldEvidencePath;
        private static string _rosterEvidencePath;
        private static string _targetSource;
        private static Entity _target;
        private static int _pendingExitCode = int.MinValue;

        [MenuItem("Tools/Validation/Canonical Vehicle World And Roster Selection")]
        public static void Run()
        {
            try
            {
                string evidenceDirectory = Path.GetFullPath(Path.Combine(Application.dataPath, "../Build/EditorEvidence"));
                Directory.CreateDirectory(evidenceDirectory);
                _worldEvidencePath = Path.Combine(evidenceDirectory, "CanonicalVehicleWorldSelection.png");
                _rosterEvidencePath = Path.Combine(evidenceDirectory, "CanonicalVehicleRosterSelection.png");
                DeleteEvidence(_worldEvidencePath);
                DeleteEvidence(_rosterEvidencePath);

                EditorSceneManager.OpenScene(MenuScenePath, OpenSceneMode.Single);
                _completed = false;
                _deploySubmitted = false;
                _matchReady = false;
                _cameraFocused = false;
                _worldSelectionSubmitted = false;
                _worldCaptureRequested = false;
                _worldAccepted = false;
                _rosterSelectionSubmitted = false;
                _rosterCaptureRequested = false;
                _frame = 0;
                _stateFrame = 0;
                _stableVehicleCount = -1;
                _stableVehicleFrames = 0;
                _packedVehicleDiagnosticEmitted = false;
                _captureFrame = -1;
                _worldSelectedCount = -1;
                _rosterSelectedCount = -1;
                _startedAt = EditorApplication.timeSinceStartup;
                _targetSource = string.Empty;
                _target = Entity.Null;
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

                if (!TryGetEntityManager(out EntityManager em))
                    return;

                if (_target == Entity.Null)
                {
                    int vehicleCount = CountSelectablePlayerCombatVehicles(em);
                    if (vehicleCount <= 0 &&
                        !_packedVehicleDiagnosticEmitted &&
                        _frame - _stateFrame >= 300)
                    {
                        _packedVehicleDiagnosticEmitted = true;
                        Complete(false, BuildPackedVehicleDiagnostic(em));
                        return;
                    }
                    if (vehicleCount <= 0)
                        return;

                    if (vehicleCount != _stableVehicleCount)
                    {
                        _stableVehicleCount = vehicleCount;
                        _stableVehicleFrames = 0;
                        return;
                    }

                    _stableVehicleFrames++;
                    if (_stableVehicleFrames < StableVehicleFrames || !TryFindTargetTank(em, out _target, out _targetSource))
                        return;

                    _stateFrame = _frame;
                    return;
                }

                if (!em.Exists(_target) || !em.HasComponent<LocalToWorld>(_target))
                {
                    Complete(false, BuildStatus("target vehicle no longer exists"));
                    return;
                }

                if (!_cameraFocused)
                {
                    FocusCameraOnVehicle(em, _target);
                    _cameraFocused = true;
                    _stateFrame = _frame;
                    return;
                }

                if (!_worldSelectionSubmitted)
                {
                    if (_frame - _stateFrame < 120)
                        return;

                    Vector3 screen = Camera.main != null
                        ? Camera.main.WorldToScreenPoint(em.GetComponentData<LocalToWorld>(_target).Position)
                        : new Vector3(-1f, -1f, -1f);
                    if (screen.z <= 0f || !QueueNormalWorldFocus(em, screen))
                    {
                        Complete(false, BuildStatus($"world focus command rejected screen={screen}"));
                        return;
                    }

                    _worldSelectionSubmitted = true;
                    _stateFrame = _frame;
                    return;
                }

                if (!_worldAccepted)
                {
                    if (_frame - _stateFrame < 30)
                        return;

                    _worldSelectedCount = CountSelectedUnits(em);
                    if (!em.HasComponent<SelectedUnitTag>(_target) ||
                        !TryReadFocusedUnit(em, out FocusedUnitUiReadModelComponent focused) ||
                        focused.HasFocusedUnit == 0 || focused.FocusedUnit != _target ||
                        !IsSelectionPanelVisible())
                    {
                        if (_frame - _stateFrame > 300)
                            Complete(false, BuildStatus("world selection did not resolve the intended vehicle"));
                        return;
                    }

                    if (!_worldCaptureRequested)
                    {
                        ScreenCapture.CaptureScreenshot(_worldEvidencePath, 1);
                        _worldCaptureRequested = true;
                        _captureFrame = _frame;
                        return;
                    }

                    if (_frame - _captureFrame < 3 || !File.Exists(_worldEvidencePath))
                        return;

                    _worldAccepted = true;
                    _stateFrame = _frame;
                    return;
                }

                if (!_rosterSelectionSubmitted)
                {
                    if (_frame - _stateFrame < 30)
                        return;

                    MatchHudSquadTrayView tray = UnityEngine.Object.FindAnyObjectByType<MatchHudSquadTrayView>(FindObjectsInactive.Exclude);
                    Button armor = FindCardButton(tray, "ARMOR");
                    if (armor == null || !armor.interactable)
                    {
                        Complete(false, BuildStatus("ARMOR roster card was unavailable"));
                        return;
                    }

                    armor.onClick.Invoke();
                    _rosterSelectionSubmitted = true;
                    _stateFrame = _frame;
                    return;
                }

                if (_frame - _stateFrame < 30)
                    return;

                _rosterSelectedCount = CountSelectedUnits(em);
                if (_rosterSelectedCount <= 0 ||
                    !em.HasComponent<SelectedUnitTag>(_target) ||
                    !EverySelectedUnitIsPlayerCombatVehicle(em) ||
                    !IsSelectionPanelVisible())
                {
                    if (_frame - _stateFrame > 300)
                        Complete(false, BuildStatus("ARMOR roster selection did not resolve canonical combat vehicles"));
                    return;
                }

                if (!_rosterCaptureRequested)
                {
                    ScreenCapture.CaptureScreenshot(_rosterEvidencePath, 1);
                    _rosterCaptureRequested = true;
                    _captureFrame = _frame;
                    return;
                }

                if (_frame - _captureFrame < 3 || !File.Exists(_rosterEvidencePath))
                    return;

                Complete(true, BuildStatus(
                    $"target={_target.Index}:{_target.Version} source={_targetSource} worldSelected={_worldSelectedCount} rosterSelected={_rosterSelectedCount} worldEvidence={_worldEvidencePath} rosterEvidence={_rosterEvidencePath}"));
            }
            catch (Exception exception)
            {
                Complete(false, exception.ToString());
            }
        }

        private static bool QueueNormalWorldFocus(EntityManager em, Vector3 screen)
        {
            using EntityQuery query = em.CreateEntityQuery(
                ComponentType.ReadOnly<RtsSelectionInputStateComponent>(),
                ComponentType.ReadWrite<RtsSelectionCommandIntentRequestElement>());
            if (query.CalculateEntityCount() != 1)
                return false;

            Entity queueEntity = query.GetSingletonEntity();
            DynamicBuffer<RtsSelectionCommandIntentRequestElement> requests =
                em.GetBuffer<RtsSelectionCommandIntentRequestElement>(queueEntity);
            requests.Add(new RtsSelectionCommandIntentRequestElement
            {
                Kind = RtsSelectionCommandIntentKind.FocusUnit,
                Frame = Time.frameCount,
                ScreenPosition = new float2(screen.x, screen.y),
                HasScreenPosition = 1
            });
            return true;
        }

        private static bool TryFindTargetTank(EntityManager em, out Entity target, out string source)
        {
            target = Entity.Null;
            source = string.Empty;
            using EntityQuery query = CreateVehicleQuery(em);
            using NativeArray<Entity> entities = query.ToEntityArray(Allocator.Temp);
            for (int pass = 0; pass < 2; pass++)
            {
                for (int i = 0; i < entities.Length; i++)
                {
                    Entity candidate = entities[i];
                    if (!IsPlayerCombatVehicle(em, candidate))
                        continue;

                    string candidateSource = em.GetComponentData<UnitSourcePrefabKey>(candidate).Value.ToString();
                    bool preferredTank = candidateSource.Contains("tank_usa", StringComparison.OrdinalIgnoreCase);
                    if (pass == 0 && !preferredTank)
                        continue;

                    target = candidate;
                    source = candidateSource;
                    return true;
                }
            }

            return false;
        }

        private static int CountSelectablePlayerCombatVehicles(EntityManager em)
        {
            int count = 0;
            using EntityQuery query = CreateVehicleQuery(em);
            using NativeArray<Entity> entities = query.ToEntityArray(Allocator.Temp);
            for (int i = 0; i < entities.Length; i++)
            {
                if (IsPlayerCombatVehicle(em, entities[i]))
                    count++;
            }

            return count;
        }

        private static string BuildPackedVehicleDiagnostic(EntityManager em)
        {
            using EntityQuery query = em.CreateEntityQuery(
                ComponentType.ReadOnly<OperationMapAuthoredVehiclePresentation>());
            using NativeArray<Entity> entities = query.ToEntityArray(Allocator.Temp);
            var message = new System.Text.StringBuilder(2048);
            message.Append("no selectable player combat vehicles; authored=")
                .Append(entities.Length);

            for (int i = 0; i < entities.Length; i++)
            {
                Entity entity = entities[i];
                bool hasDetail = em.HasComponent<UnitDetailedVisualReference>(entity);
                Entity detailRoot = hasDetail
                    ? em.GetComponentData<UnitDetailedVisualReference>(entity).Root
                    : Entity.Null;
                bool detailExists = detailRoot != Entity.Null && em.Exists(detailRoot);
                bool hasIdentity = detailExists &&
                                   em.HasComponent<OperationMapEntityPresentationIdentity>(detailRoot);
                OperationMapEntityPresentationIdentity identity = hasIdentity
                    ? em.GetComponentData<OperationMapEntityPresentationIdentity>(detailRoot)
                    : default;
                string source = em.HasComponent<UnitSourcePrefabKey>(entity)
                    ? em.GetComponentData<UnitSourcePrefabKey>(entity).Value.ToString()
                    : "<missing>";
                string faction = em.HasComponent<Faction>(entity)
                    ? em.GetComponentData<Faction>(entity).Id.ToString()
                    : "<missing>";

                message.Append("\n  [")
                    .Append(i)
                    .Append("] entity=").Append(entity)
                    .Append(" prefab=").Append(em.HasComponent<Prefab>(entity) ? 1 : 0)
                    .Append(" disabled=").Append(em.HasComponent<Disabled>(entity) ? 1 : 0)
                    .Append(" faction=").Append(faction)
                    .Append(" player=").Append(
                        em.HasComponent<Faction>(entity) &&
                        FactionIdentity.IsPlayerControlled(em.GetComponentData<Faction>(entity).Id)
                            ? 1
                            : 0)
                    .Append(" source=").Append(source)
                    .Append(" grid=").Append(em.HasComponent<UnitGrid>(entity) ? 1 : 0)
                    .Append(" move=").Append(em.HasComponent<UnitMove>(entity) ? 1 : 0)
                    .Append(" footprint=").Append(em.HasComponent<UnitFootprint>(entity) ? 1 : 0)
                    .Append(" world=").Append(em.HasComponent<LocalToWorld>(entity) ? 1 : 0)
                    .Append(" movementBehavior=").Append(em.HasComponent<UnitMovementBehavior>(entity) ? 1 : 0)
                    .Append(" transportCapacity=").Append(em.HasComponent<UnitTransportCapacity>(entity) ? 1 : 0)
                    .Append(" air=").Append(em.HasComponent<UnitAirMovement>(entity) ? 1 : 0)
                    .Append(" detail=").Append(hasDetail ? 1 : 0)
                    .Append(" detailExists=").Append(detailExists ? 1 : 0)
                    .Append(" identity=").Append(hasIdentity ? 1 : 0)
                    .Append(" identityRole=").Append(hasIdentity ? identity.Role : -1)
                    .Append(" placement=").Append(hasIdentity ? identity.PlacementIndex : -1)
                    .Append(" operationMap=").Append(hasIdentity ? identity.OperationMapId.ToString() : "<missing>");
            }

            return message.ToString();
        }

        private static EntityQuery CreateVehicleQuery(EntityManager em)
        {
            return em.CreateEntityQuery(new EntityQueryDesc
            {
                All = new[]
                {
                    ComponentType.ReadOnly<Faction>(),
                    ComponentType.ReadOnly<UnitGrid>(),
                    ComponentType.ReadOnly<UnitMove>(),
                    ComponentType.ReadOnly<UnitFootprint>(),
                    ComponentType.ReadOnly<UnitSourcePrefabKey>(),
                    ComponentType.ReadOnly<LocalToWorld>()
                },
                None = new[]
                {
                    ComponentType.ReadOnly<Prefab>(),
                    ComponentType.ReadOnly<Disabled>(),
                    ComponentType.ReadOnly<StaticGridBlocker>(),
                    ComponentType.ReadOnly<UnitTransportPassenger>()
                }
            });
        }

        private static bool IsPlayerCombatVehicle(EntityManager em, Entity entity)
        {
            if (!FactionIdentity.IsPlayerControlled(em.GetComponentData<Faction>(entity).Id))
                return false;
            if (em.HasComponent<UnitHealth>(entity) && em.GetComponentData<UnitHealth>(entity).Current <= 0)
                return false;

            string source = em.GetComponentData<UnitSourcePrefabKey>(entity).Value.ToString();
            bool isAir = em.HasComponent<UnitAirMovement>(entity);
            bool hasTransport = em.HasComponent<UnitTransportCapacity>(entity) &&
                                em.GetComponentData<UnitTransportCapacity>(entity).SoldierCapacity > 0;
            bool usesVehicleMotion = isAir ||
                                     em.HasComponent<UnitMovementBehavior>(entity) &&
                                     em.GetComponentData<UnitMovementBehavior>(entity).UsesVehicleMotion != 0;
            bool namedTransport = ContainsAny(source, "transport", "apc", "truck", "tanker", "hauler", "canopy");
            return usesVehicleMotion &&
                   !isAir &&
                   !hasTransport &&
                   !namedTransport &&
                   ContainsAny(source, "veh", "tank", "armored", "launcher", "radar");
        }

        private static bool EverySelectedUnitIsPlayerCombatVehicle(EntityManager em)
        {
            using EntityQuery query = em.CreateEntityQuery(ComponentType.ReadOnly<SelectedUnitTag>());
            using NativeArray<Entity> entities = query.ToEntityArray(Allocator.Temp);
            if (entities.Length == 0)
                return false;

            for (int i = 0; i < entities.Length; i++)
            {
                Entity entity = entities[i];
                if (!em.HasComponent<Faction>(entity) ||
                    !em.HasComponent<UnitSourcePrefabKey>(entity) ||
                    !IsPlayerCombatVehicle(em, entity))
                    return false;
            }

            return true;
        }

        private static int CountSelectedUnits(EntityManager em)
        {
            using EntityQuery query = em.CreateEntityQuery(ComponentType.ReadOnly<SelectedUnitTag>());
            return query.CalculateEntityCount();
        }

        private static void FocusCameraOnVehicle(EntityManager em, Entity target)
        {
            if (Camera.main == null)
                throw new InvalidOperationException("Main camera is unavailable.");

            RtsCameraSystem cameraSystem = World.DefaultGameObjectInjectionWorld?.GetExistingSystemManaged<RtsCameraSystem>();
            if (cameraSystem == null)
                throw new InvalidOperationException("RTS camera system is unavailable.");
            cameraSystem.MoveCameraGroundCenterTo(Camera.main, em.GetComponentData<LocalToWorld>(target).Position);
        }

        private static Button FindCardButton(MatchHudSquadTrayView tray, string expectedLabel)
        {
            if (tray == null)
                return null;

            Button[] buttons = tray.GetComponentsInChildren<Button>(true);
            for (int i = 0; i < buttons.Length; i++)
            {
                TMP_Text[] labels = buttons[i].GetComponentsInChildren<TMP_Text>(true);
                for (int labelIndex = 0; labelIndex < labels.Length; labelIndex++)
                {
                    if (string.Equals(labels[labelIndex].text?.Trim(), expectedLabel, StringComparison.OrdinalIgnoreCase))
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

            FieldInfo selectedUnitField = typeof(MatchHudSelectionPanelView).GetField(
                "selectedSquadPanel",
                BindingFlags.Instance | BindingFlags.NonPublic);
            return selectedUnitField?.GetValue(panel) is GameObject root && root.activeInHierarchy;
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

        private static bool ContainsAny(string value, params string[] tokens)
        {
            if (string.IsNullOrWhiteSpace(value))
                return false;
            for (int i = 0; i < tokens.Length; i++)
            {
                if (value.Contains(tokens[i], StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            return false;
        }

        private static void DeleteEvidence(string path)
        {
            if (File.Exists(path))
                File.Delete(path);
        }

        private static string BuildStatus(string message)
        {
            return $"{message} deploy={(_deploySubmitted ? 1 : 0)} matchReady={(_matchReady ? 1 : 0)} target={(_target == Entity.Null ? 0 : 1)} source={_targetSource} camera={(_cameraFocused ? 1 : 0)} worldSubmitted={(_worldSelectionSubmitted ? 1 : 0)} worldAccepted={(_worldAccepted ? 1 : 0)} rosterSubmitted={(_rosterSelectionSubmitted ? 1 : 0)} stableVehicles={_stableVehicleCount} worldSelected={_worldSelectedCount} rosterSelected={_rosterSelectedCount}";
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
