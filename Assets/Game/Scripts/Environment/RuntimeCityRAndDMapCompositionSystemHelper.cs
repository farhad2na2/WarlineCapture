using System;
using System.Collections;
using System.Collections.Generic;
using Game.Components;
using Game.Configs;
using Unity.Mathematics;
using UnityEngine;

namespace Game.Runtime
{
    internal sealed class RuntimeCityRAndDMapCompositionSystemHelper
    {
        private RuntimeCityRAndDMapView _view;
        private Material _roadMaterial;
        private Transform _generatedRoot;
        private RuntimeCityCompositionSystemHelper _runtimeCity;
        private RuntimeCityRoadVisualPrototypeSystemHelper _roadVisuals;
        private RuntimeOperationMapVisualQualitySystemHelper _algorithmicVisualQuality;
        private RuntimeCityAlgorithmicDistrictPresentationSystemHelper _algorithmicDistrictPresentation;
        private RuntimeCityAlgorithmicAftermathPresentationSystemHelper _algorithmicAftermathPresentation;
        private RuntimeOperationMapVisualRecipePresentationSystemHelper _visualRecipePresentation;
        private IEnumerator _visualRoutine;
        private bool _visualRoutineRunning;
        private bool _generationActive;
        private bool _generateRequested;
        private bool _restartRequested;
        private bool _clearRequested;
        private int _restartAfterFrame = -1;
        private RuntimeCityGenerationProgress _lastProgress = RuntimeCityGenerationProgress.Idle;
        private RuntimeCityGenerationStage _lastLoggedStage = RuntimeCityGenerationStage.Idle;
        private float _generationStartedAt;
        private int _generationStartedFrame;
        private string _statusMessage = "Ready";
        private Camera _presentationCamera;
        private RuntimeOperationMapVisualStage _cameraStage;
        private Vector3 _cameraStartPosition;
        private Quaternion _cameraStartRotation;
        private float _cameraStartFieldOfView;
        private RuntimeOperationMapCameraPose _cameraTargetPose;
        private float _cameraTransitionElapsed;
        private bool _cameraStageAssigned;
        private bool _cameraTransitionActive;
        private RuntimeOperationMapVisualStage _algorithmicVisualStage = RuntimeOperationMapVisualStage.TerrainAndRoads;
        private float _algorithmicStageElapsed;
        private bool _algorithmicCompletionRevealActive;

        public RuntimeCityGenerationProgress Progress => GetCombinedProgress();
        public string StatusMessage => _statusMessage;
        public int RoadStrokeCount => _roadVisuals?.StrokeCount ?? 0;
        public int RoadCellCount => _roadVisuals?.RoadCellCount ?? 0;
        public int VisualBuildingCount => _runtimeCity?.VisualBuildingCount ?? 0;
        public int PlannedBuildingCount => _runtimeCity?.PlannedBuildingCount ?? 0;
        public int MaxObservedConsecutivePrefabSelections =>
            _runtimeCity?.MaxObservedConsecutivePrefabSelections ?? 0;
        public int VisualRecipeEntryCount => _visualRecipePresentation?.SpawnedEntryCount ?? 0;
        public int VisualRecipeRendererCount => _visualRecipePresentation?.RendererCount ?? 0;
        public int FoundationVisualCount =>
            _visualRecipePresentation?.FoundationVisualCount ??
            _algorithmicVisualQuality?.FoundationVisualCount ??
            0;
        public int SuppressedObstructionCount => _visualRecipePresentation?.SuppressedObstructionCount ?? 0;
        public int AlgorithmicDistrictSurfaceCount => _algorithmicDistrictPresentation?.SurfaceCount ?? 0;
        public int AlgorithmicAftermathDressingCount => _algorithmicAftermathPresentation?.DressingCount ?? 0;
        public float MaxVisualBatchMilliseconds => _visualRecipePresentation?.MaxBatchMilliseconds ?? 0f;
        public int FrameBudgetYieldCount => _visualRecipePresentation?.FrameBudgetYieldCount ?? 0;
        public RuntimeOperationMapVisualStage CurrentVisualStage =>
            _visualRecipePresentation != null
                ? _visualRecipePresentation.CurrentVisualStage
                : _algorithmicVisualStage;
        public bool IsPresentationComplete =>
            _lastProgress.Stage == RuntimeCityGenerationStage.Completed &&
            (_view == null || _view.VisualRecipe != null || !_algorithmicCompletionRevealActive);

        public void Configure(RuntimeCityRAndDMapView view, Material roadMaterial)
        {
            _view = view;
            _roadMaterial = roadMaterial;
            _generatedRoot = view != null ? view.GeneratedRoot : null;
            _presentationCamera = view != null ? view.PresentationCamera : null;
            _lastProgress = RuntimeCityGenerationProgress.Idle;
            _statusMessage = "Ready";
            _algorithmicVisualStage = RuntimeOperationMapVisualStage.TerrainAndRoads;
            _algorithmicStageElapsed = 0f;
            _algorithmicCompletionRevealActive = false;
        }

        public void AdvancePresentation(float unscaledDeltaTime)
        {
            if (_view == null)
                return;
            if (_view.VisualRecipe == null)
                AdvanceAlgorithmicPresentation(unscaledDeltaTime);
            if (_presentationCamera == null)
                return;

            RuntimeOperationMapVisualStage stage = CurrentVisualStage;
            if (!_cameraStageAssigned || stage != _cameraStage)
                BeginCameraTransition(stage);

            if (!_cameraTransitionActive)
                return;

            _cameraTransitionElapsed += Mathf.Max(0f, unscaledDeltaTime);
            float duration = _cameraTargetPose.TransitionSeconds;
            float progress = duration <= 0f ? 1f : Mathf.Clamp01(_cameraTransitionElapsed / duration);
            float easedProgress = progress * progress * (3f - (2f * progress));
            Quaternion targetRotation = GetCameraRotation(_cameraTargetPose);
            Transform cameraTransform = _presentationCamera.transform;
            cameraTransform.position = Vector3.Lerp(_cameraStartPosition, _cameraTargetPose.Position, easedProgress);
            cameraTransform.rotation = Quaternion.Slerp(_cameraStartRotation, targetRotation, easedProgress);
            _presentationCamera.fieldOfView = Mathf.Lerp(
                _cameraStartFieldOfView,
                _cameraTargetPose.FieldOfView,
                easedProgress);

            if (progress < 1f)
                return;

            ApplyCameraPose(_cameraTargetPose);
            _cameraTransitionActive = false;
            Debug.Log(
                $"[RuntimeMapCamera] action=arrived stage={_cameraStage} " +
                $"position={_cameraTargetPose.Position} fov={_cameraTargetPose.FieldOfView:0.0}",
                _view);
        }

        public void RequestGeneration()
        {
            if (!_generationActive)
                _generateRequested = true;
        }

        public void RequestRestart()
        {
            _restartRequested = true;
            _clearRequested = false;
        }

        public void RequestClear()
        {
            _clearRequested = true;
            _restartRequested = false;
            _generateRequested = false;
            _restartAfterFrame = -1;
        }

        public void Tick(int frameCount)
        {
            if (_view == null)
                return;

            if (_clearRequested)
            {
                _clearRequested = false;
                ClearGeneratedMap();
                return;
            }

            if (_restartRequested)
            {
                _restartRequested = false;
                _generateRequested = false;
                DisposeGeneration();
                ClearGeneratedRootChildren();
                _lastProgress = RuntimeCityGenerationProgress.Idle;
                _statusMessage = "Restarting";
                _restartAfterFrame = frameCount + 1;
                return;
            }

            if (_restartAfterFrame >= 0 && frameCount >= _restartAfterFrame)
            {
                _restartAfterFrame = -1;
                _generateRequested = true;
            }

            if (_generateRequested && !_generationActive)
            {
                _generateRequested = false;
                BeginGeneration(frameCount);
            }

            if (_generationActive)
                StepGeneration(frameCount);
        }

        public void Dispose()
        {
            DisposeGeneration();
            ClearGeneratedRootChildren();
            _view = null;
            _roadMaterial = null;
            _generatedRoot = null;
            _presentationCamera = null;
            _lastProgress = RuntimeCityGenerationProgress.Idle;
            _statusMessage = "Disposed";
            _generateRequested = false;
            _restartRequested = false;
            _clearRequested = false;
            _restartAfterFrame = -1;
            _cameraStageAssigned = false;
            _cameraTransitionActive = false;
            _algorithmicVisualStage = RuntimeOperationMapVisualStage.TerrainAndRoads;
            _algorithmicStageElapsed = 0f;
            _algorithmicCompletionRevealActive = false;
        }

        private void BeginGeneration(int frameCount)
        {
            if (!TryConfigureGeneration(frameCount))
                return;

            ResetCameraPresentation();

            RuntimeOperationMapVisualRecipe visualRecipe = _view.VisualRecipe;
            if (visualRecipe != null)
            {
                _statusMessage = "Planning and building accepted visual recipe";
                _visualRecipePresentation = new RuntimeOperationMapVisualRecipePresentationSystemHelper();
                _visualRoutine = _visualRecipePresentation.Build(
                    visualRecipe,
                    _generatedRoot,
                    _view.VisualRecipeEntriesPerFrame,
                    _view.VisualRecipeFrameBudgetMilliseconds);
                _visualRoutineRunning = true;
            }
            else
            {
                _statusMessage = GetVisualStageLabel(RuntimeOperationMapVisualStage.TerrainAndRoads);
            }

            _generationActive = (_runtimeCity != null && _runtimeCity.IsGenerating) || _visualRoutineRunning;
            if (!_generationActive)
                CompleteGeneration();
        }

        private void StepGeneration(int frameCount)
        {
            try
            {
                if (_runtimeCity != null && _runtimeCity.IsGenerating)
                    _runtimeCity.Update(frameCount);
                if (_visualRoutineRunning)
                    _visualRoutineRunning = _visualRoutine != null && _visualRoutine.MoveNext();

                if (_visualRoutineRunning && _visualRecipePresentation != null)
                    _statusMessage = GetVisualStageLabel(_visualRecipePresentation.CurrentVisualStage);

                _lastProgress = GetCombinedProgress();
                LogStageChange(_lastProgress, frameCount);
                if ((_runtimeCity != null && _runtimeCity.IsGenerating) || _visualRoutineRunning)
                    return;

                CompleteGeneration();
            }
            catch (Exception exception)
            {
                RuntimeCityGenerationProgress progress = GetCombinedProgress();
                _lastProgress = new RuntimeCityGenerationProgress(
                    RuntimeCityGenerationStage.Failed,
                    progress.Seed,
                    progress.RequestedCityCount,
                    progress.GeneratedCityCount,
                    progress.CompletedWorkItems,
                    progress.TotalWorkItems,
                    progress.Progress01);
                _statusMessage = $"Generation failed: {exception.GetType().Name}";
                _generationActive = false;
                _visualRoutineRunning = false;
                Debug.LogException(exception, _view);
                Debug.LogError(
                    $"[RuntimeCityRnD] {RuntimeCityGenerationProgress.VersionTag} result=Failed reason=exception type={exception.GetType().Name}",
                    _view);
            }
        }

        private void CompleteGeneration()
        {
            RuntimeCityGenerationProgress cityProgress = _runtimeCity != null
                ? _runtimeCity.GenerationProgress
                : RuntimeCityGenerationProgress.Idle;
            if (_runtimeCity != null && cityProgress.Stage != RuntimeCityGenerationStage.Completed)
            {
                _lastProgress = cityProgress;
                _statusMessage = $"Generation stopped: {cityProgress.Stage}";
                _generationActive = false;
                return;
            }

            CreateAlgorithmicAftermathDressing();
            _lastProgress = GetCombinedProgress();
            if (_visualRecipePresentation == null)
            {
                _algorithmicCompletionRevealActive = true;
                SetAlgorithmicVisualStage(RuntimeOperationMapVisualStage.Aftermath);
            }
            else
            {
                _statusMessage = _lastProgress.Stage == RuntimeCityGenerationStage.Completed
                    ? "Generation complete"
                    : $"Generation stopped: {_lastProgress.Stage}";
            }
            _generationActive = false;
        }

        private static string GetVisualStageLabel(RuntimeOperationMapVisualStage stage)
        {
            switch (stage)
            {
                case RuntimeOperationMapVisualStage.TerrainAndRoads:
                    return "Preparing terrain and road network";
                case RuntimeOperationMapVisualStage.DistrictModules:
                    return "Establishing district silhouettes";
                case RuntimeOperationMapVisualStage.Market:
                    return "Opening the Old Market";
                case RuntimeOperationMapVisualStage.Compound:
                    return "Securing the utility compound";
                case RuntimeOperationMapVisualStage.Aftermath:
                    return "Placing mission aftermath";
                case RuntimeOperationMapVisualStage.Horizon:
                    return "Closing the desert horizon";
                default:
                    return "Generating operation map";
            }
        }

        private bool TryConfigureGeneration(int frameCount)
        {
            RuntimeCitySpawnerSystemConfig config = _view.Config;
            if (config == null)
            {
                _statusMessage = "Missing RuntimeCity config";
                Debug.LogError(
                    $"[RuntimeCityRnD] {RuntimeCityGenerationProgress.VersionTag} result=Failed reason=missingConfig",
                    _view);
                return false;
            }

            if (config.CityCount <= 0)
            {
                _statusMessage = "R&D config must generate at least one city";
                Debug.LogError(
                    $"[RuntimeCityRnD] {RuntimeCityGenerationProgress.VersionTag} result=Failed reason=cityCountZero config={config.name}",
                    _view);
                return false;
            }

            EnsureGeneratedRoot();
            GridConfig grid = CreateGrid();
            bool createAlgorithmicVisuals = _view.VisualRecipe == null;
            if (createAlgorithmicVisuals)
            {
                CreateAlgorithmicFoundation(grid);
                CreateAlgorithmicDistrictSurfaces(config, grid);
            }

            _roadVisuals = new RuntimeCityRoadVisualPrototypeSystemHelper();
            _roadVisuals.Configure(
                _generatedRoot,
                grid,
                _view.RoadCellSizeInGridCells,
                _roadMaterial,
                _view.RoadShoulderMaterial,
                _view.AlgorithmicRoadColor,
                _view.AlgorithmicRoadShoulderColor,
                createAlgorithmicVisuals);

            var roadRuntime = new RoadRuntimeGenerationCompositionSystemHelper();
            var roadContext = new RoadRuntimeGenerationCompositionSystemHelper.Context(
                _roadVisuals.TryGetRoadCellSize,
                null,
                null,
                _roadVisuals.CreateStroke,
                null,
                default);

            _runtimeCity = new RuntimeCityCompositionSystemHelper();
            _generationStartedAt = Time.realtimeSinceStartup;
            _generationStartedFrame = frameCount;
            _lastLoggedStage = RuntimeCityGenerationStage.Idle;
            _statusMessage = "Generating";

            bool started = _runtimeCity.ConfigureVisualPrototype(
                config,
                roadRuntime,
                roadContext,
                _generatedRoot,
                grid,
                _view.RoadCellSizeInGridCells,
                frameCount,
                createAlgorithmicVisuals,
                _view.AlgorithmicRoadTerminalPolicy);
            _lastProgress = _runtimeCity.GenerationProgress;
            LogStageChange(_lastProgress, frameCount);
            if (started)
                return true;

            _statusMessage = "Generator did not start";
            Debug.LogError(
                $"[RuntimeCityRnD] {RuntimeCityGenerationProgress.VersionTag} result=Failed reason=notStarted seed={config.RandomSeed} cityCount={config.CityCount}",
                _view);
            DisposeGeneration();
            return false;
        }

        private GridConfig CreateGrid()
        {
            Vector3 origin = _view.GridOrigin;
            return new GridConfig
            {
                Width = _view.GridWidth,
                Height = _view.GridHeight,
                CellSize = _view.GridCellSize,
                Origin = new float3(origin.x, origin.y, origin.z)
            };
        }

        private RuntimeCityGenerationProgress GetCombinedProgress()
        {
            if (_visualRecipePresentation != null)
            {
                RuntimeCityGenerationProgress recipeProgress = _visualRecipePresentation.Progress;
                float combined = 0.10f + (recipeProgress.Progress01 * 0.90f);
                bool cityStillGenerating = _runtimeCity != null && _runtimeCity.IsGenerating;
                return new RuntimeCityGenerationProgress(
                    recipeProgress.Stage == RuntimeCityGenerationStage.Completed && cityStillGenerating
                        ? RuntimeCityGenerationStage.Finalizing
                        : recipeProgress.Stage,
                    recipeProgress.Seed,
                    recipeProgress.RequestedCityCount,
                    recipeProgress.GeneratedCityCount,
                    recipeProgress.CompletedWorkItems,
                    recipeProgress.TotalWorkItems,
                    recipeProgress.Stage == RuntimeCityGenerationStage.Completed && !cityStillGenerating
                        ? 1f
                        : Mathf.Min(0.99f, combined));
            }

            RuntimeCityGenerationProgress cityProgress = _runtimeCity != null
                ? _runtimeCity.GenerationProgress
                : _lastProgress;
            RuntimeOperationMapVisualRecipe visualRecipe = _view != null ? _view.VisualRecipe : null;
            if (visualRecipe == null)
                return cityProgress;

            RuntimeCityGenerationStage stage = cityProgress.Stage == RuntimeCityGenerationStage.Completed
                ? RuntimeCityGenerationStage.Planning
                : cityProgress.Stage;
            return new RuntimeCityGenerationProgress(
                stage,
                visualRecipe.Seed,
                1,
                0,
                cityProgress.CompletedWorkItems,
                cityProgress.TotalWorkItems,
                Mathf.Min(0.10f, cityProgress.Progress01 * 0.10f));
        }

        private void EnsureGeneratedRoot()
        {
            if (_generatedRoot != null)
                return;

            var root = new GameObject("Generated_RuntimeCity_RnD");
            _generatedRoot = root.transform;
            _generatedRoot.SetParent(_view.transform, false);
        }

        private void ClearGeneratedMap()
        {
            DisposeGeneration();
            ClearGeneratedRootChildren();
            _lastProgress = RuntimeCityGenerationProgress.Idle;
            _statusMessage = "Cleared";
        }

        private void DisposeGeneration()
        {
            _algorithmicAftermathPresentation?.Dispose();
            _algorithmicAftermathPresentation = null;
            if (_runtimeCity != null)
            {
                _runtimeCity.Dispose();
                _lastProgress = _runtimeCity.GenerationProgress;
                _runtimeCity = null;
            }

            _roadVisuals?.Dispose();
            _roadVisuals = null;
            _algorithmicVisualQuality?.Dispose();
            _algorithmicVisualQuality = null;
            _algorithmicDistrictPresentation?.Dispose();
            _algorithmicDistrictPresentation = null;
            _visualRecipePresentation?.Dispose();
            _visualRecipePresentation = null;
            _visualRoutine = null;
            _visualRoutineRunning = false;
            _generationActive = false;
        }

        private void CreateAlgorithmicAftermathDressing()
        {
            if (_visualRecipePresentation != null ||
                _algorithmicAftermathPresentation != null ||
                _view == null ||
                _view.Config == null ||
                _generatedRoot == null)
            {
                return;
            }

            float roadCellWorldSize = _view.RoadCellSizeInGridCells * _view.GridCellSize;
            Vector2Int centerRoadCell =
                _view.Config.StartCell / _view.RoadCellSizeInGridCells;
            Vector3 cityCenter = new(
                _view.GridOrigin.x + ((centerRoadCell.x + 0.5f) * roadCellWorldSize),
                _view.GridOrigin.y,
                _view.GridOrigin.z + ((centerRoadCell.y + 0.5f) * roadCellWorldSize));
            _algorithmicAftermathPresentation =
                new RuntimeCityAlgorithmicAftermathPresentationSystemHelper();
            _algorithmicAftermathPresentation.CreateGroupedDressing(
                _view.AlgorithmicAftermath,
                _view.Config.CityDecorationPrefabs,
                _view.Config.RandomSeed,
                cityCenter,
                roadCellWorldSize,
                _generatedRoot);
        }

        private void ResetCameraPresentation()
        {
            _cameraStageAssigned = false;
            _cameraTransitionActive = false;
            _cameraTransitionElapsed = 0f;
            _algorithmicVisualStage = RuntimeOperationMapVisualStage.TerrainAndRoads;
            _algorithmicStageElapsed = 0f;
            _algorithmicCompletionRevealActive = false;
            if (!TryGetCameraPose(RuntimeOperationMapVisualStage.TerrainAndRoads, out RuntimeOperationMapCameraPose pose))
                return;

            _cameraStage = RuntimeOperationMapVisualStage.TerrainAndRoads;
            _cameraStageAssigned = true;
            _cameraTargetPose = pose;
            ApplyCameraPose(pose);
            Debug.Log(
                $"[RuntimeMapCamera] action=initial stage={_cameraStage} position={pose.Position} fov={pose.FieldOfView:0.0}",
                _view);
        }

        private void CreateAlgorithmicFoundation(GridConfig grid)
        {
            Material material = _view.AlgorithmicGroundMaterial ??
                                _view.RoadShoulderMaterial ??
                                _roadMaterial;
            if (material == null)
            {
                Debug.LogError(
                    $"[RuntimeCityRnD] {RuntimeCityGenerationProgress.VersionTag} " +
                    "result=Failed reason=missingAlgorithmicGroundMaterial",
                    _view);
                return;
            }

            float width = grid.Width * grid.CellSize;
            float depth = grid.Height * grid.CellSize;
            Vector3 position = new(
                grid.Origin.x + width * 0.5f,
                grid.Origin.y - 0.2f,
                grid.Origin.z + depth * 0.5f);
            var settings = new RuntimeOperationMapFoundationSettings(
                material,
                position,
                new Vector3(width, 0.4f, depth),
                _view.AlgorithmicGroundColor);
            _algorithmicVisualQuality = new RuntimeOperationMapVisualQualitySystemHelper();
            _algorithmicVisualQuality.CreateFoundation(settings, _generatedRoot);
        }

        private void CreateAlgorithmicDistrictSurfaces(
            RuntimeCitySpawnerSystemConfig config,
            GridConfig grid)
        {
            int roadCellSize = _view.RoadCellSizeInGridCells;
            Vector2Int centerRoadCell = config.StartCell / roadCellSize;
            float roadCellWorldSize = roadCellSize * grid.CellSize;
            Vector3 cityCenter = new(
                grid.Origin.x + (centerRoadCell.x + 0.5f) * roadCellWorldSize,
                grid.Origin.y,
                grid.Origin.z + (centerRoadCell.y + 0.5f) * roadCellWorldSize);
            _algorithmicDistrictPresentation = new RuntimeCityAlgorithmicDistrictPresentationSystemHelper();
            _algorithmicDistrictPresentation.CreateSurfaces(
                _view.AlgorithmicDistrictSurfaces,
                config.RandomSeed,
                cityCenter,
                roadCellWorldSize,
                _view.AlgorithmicGroundColor,
                _generatedRoot);
        }

        private void AdvanceAlgorithmicPresentation(float unscaledDeltaTime)
        {
            float deltaTime = Mathf.Max(0f, unscaledDeltaTime);
            if (_algorithmicCompletionRevealActive)
            {
                _algorithmicStageElapsed += deltaTime;
                RuntimeOperationMapVisualStage stage = _algorithmicVisualStage;
                float minimumDuration = GetAlgorithmicStageMinimumDuration(stage);
                if (_algorithmicStageElapsed < minimumDuration)
                    return;

                if (stage == RuntimeOperationMapVisualStage.Aftermath)
                {
                    SetAlgorithmicVisualStage(RuntimeOperationMapVisualStage.Horizon);
                    return;
                }

                _algorithmicCompletionRevealActive = false;
                _statusMessage = "Generation complete";
                return;
            }

            SetAlgorithmicVisualStage(GetAlgorithmicVisualStage(_lastProgress.Stage));
        }

        private void SetAlgorithmicVisualStage(RuntimeOperationMapVisualStage stage)
        {
            if (_algorithmicVisualStage == stage)
                return;

            _algorithmicVisualStage = stage;
            _algorithmicStageElapsed = 0f;
            _statusMessage = GetVisualStageLabel(stage);
        }

        private float GetAlgorithmicStageMinimumDuration(RuntimeOperationMapVisualStage stage)
        {
            float duration = _view.AlgorithmicReveal.GetMinimumDuration(stage);
            if (TryGetCameraPose(stage, out RuntimeOperationMapCameraPose pose))
                duration = Mathf.Max(duration, pose.TransitionSeconds);

            return duration;
        }

        internal static RuntimeOperationMapVisualStage GetAlgorithmicVisualStage(
            RuntimeCityGenerationStage generationStage)
        {
            switch (generationStage)
            {
                case RuntimeCityGenerationStage.Landmarks:
                    return RuntimeOperationMapVisualStage.Market;
                case RuntimeCityGenerationStage.Buildings:
                    return RuntimeOperationMapVisualStage.DistrictModules;
                case RuntimeCityGenerationStage.Decorations:
                case RuntimeCityGenerationStage.Finalizing:
                    return RuntimeOperationMapVisualStage.Compound;
                case RuntimeCityGenerationStage.Completed:
                    return RuntimeOperationMapVisualStage.Horizon;
                default:
                    return RuntimeOperationMapVisualStage.TerrainAndRoads;
            }
        }

        private void BeginCameraTransition(RuntimeOperationMapVisualStage stage)
        {
            if (!TryGetCameraPose(stage, out RuntimeOperationMapCameraPose pose))
                return;

            _cameraStage = stage;
            _cameraStageAssigned = true;
            _cameraStartPosition = _presentationCamera.transform.position;
            _cameraStartRotation = _presentationCamera.transform.rotation;
            _cameraStartFieldOfView = _presentationCamera.fieldOfView;
            _cameraTargetPose = pose;
            _cameraTransitionElapsed = 0f;
            _cameraTransitionActive = pose.TransitionSeconds > 0f;
            if (!_cameraTransitionActive)
                ApplyCameraPose(pose);

            Debug.Log(
                $"[RuntimeMapCamera] action=target stage={stage} transition={pose.TransitionSeconds:0.00}s " +
                $"position={pose.Position} target={pose.Target} fov={pose.FieldOfView:0.0}",
                _view);
        }

        private bool TryGetCameraPose(
            RuntimeOperationMapVisualStage stage,
            out RuntimeOperationMapCameraPose pose)
        {
            pose = default;
            RuntimeOperationMapVisualRecipe recipe = _view != null ? _view.VisualRecipe : null;
            IReadOnlyList<RuntimeOperationMapCameraPose> cameraPoses = recipe != null
                ? recipe.CameraPoses
                : _view?.AlgorithmicCameraPoses;
            if (cameraPoses == null)
                return false;

            for (int i = 0; i < cameraPoses.Count; i++)
            {
                RuntimeOperationMapCameraPose candidate = cameraPoses[i];
                if (candidate.Stage != stage || !candidate.IsConfigured)
                    continue;

                pose = candidate;
                return true;
            }

            return false;
        }

        private void ApplyCameraPose(RuntimeOperationMapCameraPose pose)
        {
            Transform cameraTransform = _presentationCamera.transform;
            cameraTransform.position = pose.Position;
            cameraTransform.rotation = GetCameraRotation(pose);
            _presentationCamera.fieldOfView = pose.FieldOfView;
        }

        private static Quaternion GetCameraRotation(RuntimeOperationMapCameraPose pose)
        {
            return Quaternion.LookRotation((pose.Target - pose.Position).normalized, Vector3.up);
        }

        private void ClearGeneratedRootChildren()
        {
            if (_generatedRoot == null)
                return;

            for (int i = _generatedRoot.childCount - 1; i >= 0; i--)
            {
                GameObject child = _generatedRoot.GetChild(i).gameObject;
                if (Application.isPlaying)
                    UnityEngine.Object.Destroy(child);
                else
                    UnityEngine.Object.DestroyImmediate(child);
            }
        }

        private void LogStageChange(RuntimeCityGenerationProgress progress, int frameCount)
        {
            if (progress.Stage == _lastLoggedStage)
                return;

            _lastLoggedStage = progress.Stage;
            Debug.Log(
                $"[RuntimeCityRnD] {RuntimeCityGenerationProgress.VersionTag} stage={progress.Stage} " +
                $"progress={progress.Progress01:0.000} seed={progress.Seed} " +
                $"cities={progress.GeneratedCityCount}/{progress.RequestedCityCount} " +
                $"work={progress.CompletedWorkItems}/{progress.TotalWorkItems} " +
                $"roads={RoadStrokeCount}/{RoadCellCount} plannedBuildings={PlannedBuildingCount} " +
                $"visualBuildings={VisualBuildingCount} recipeEntries={VisualRecipeEntryCount} renderers={VisualRecipeRendererCount} " +
                $"ageFrames={frameCount - _generationStartedFrame} elapsed={Time.realtimeSinceStartup - _generationStartedAt:0.000}s",
                _view);
        }
    }
}
