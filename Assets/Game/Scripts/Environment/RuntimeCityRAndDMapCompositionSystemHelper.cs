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
        private readonly RuntimeOperationMapGenerationRecoverySystemHelper _recovery = new();
        private IEnumerator _visualRoutine;
        private bool _visualRoutineRunning;
        private bool _generationActive;
        private bool _generateRequested;
        private bool _restartRequested;
        private bool _clearRequested;
        private bool _cancelRequested;
        private int _restartAfterFrame = -1;
        private RuntimeOperationMapVisualRecipe _activeVisualRecipe;
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
        public bool IsGenerationActive =>
            _generationActive ||
            _visualRoutineRunning ||
            (_runtimeCity?.IsGenerating ?? false) ||
            _recovery.IsFallbackScheduled;
        public bool IsUsingFallback => _recovery.IsFallbackActive;
        public int FallbackAttemptCount => _recovery.FallbackAttemptCount;
        public string RecoveryReason => _recovery.FailureReason;
        public bool IsPresentationComplete =>
            _lastProgress.Stage == RuntimeCityGenerationStage.Completed &&
            (_view == null || _activeVisualRecipe != null || !_algorithmicCompletionRevealActive);

        public void Configure(RuntimeCityRAndDMapView view, Material roadMaterial)
        {
            _recovery.Reset();
            _view = view;
            _roadMaterial = roadMaterial;
            _generatedRoot = view != null ? view.GeneratedRoot : null;
            _presentationCamera = view != null ? view.PresentationCamera : null;
            _activeVisualRecipe = view != null ? view.VisualRecipe : null;
            _cancelRequested = false;
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
            if (_activeVisualRecipe == null)
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
            if (!IsGenerationActive)
                _generateRequested = true;
        }

        public void RequestCancel()
        {
            _cancelRequested = true;
            _clearRequested = false;
            _restartRequested = false;
            _generateRequested = false;
            _restartAfterFrame = -1;
        }

        public void RequestRestart()
        {
            _restartRequested = true;
            _cancelRequested = false;
            _clearRequested = false;
        }

        public void RequestClear()
        {
            _clearRequested = true;
            _cancelRequested = false;
            _restartRequested = false;
            _generateRequested = false;
            _restartAfterFrame = -1;
        }

        public void Tick(int frameCount)
        {
            if (_view == null)
                return;

            if (_cancelRequested)
            {
                _cancelRequested = false;
                CancelGeneration("requested", clearGeneratedMap: true);
                return;
            }

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
                if (IsGenerationActive)
                    CancelGeneration("restartRequested", clearGeneratedMap: false);
                else
                    DisposeGeneration();
                ClearGeneratedRootChildren();
                _recovery.Reset();
                _activeVisualRecipe = _view.VisualRecipe;
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

            if (_recovery.TryActivateFallback(frameCount))
            {
                BeginFallbackGeneration(frameCount);
                return;
            }

            if (_generateRequested && !IsGenerationActive)
            {
                _generateRequested = false;
                try
                {
                    BeginGeneration(frameCount);
                }
                catch (Exception exception)
                {
                    HandleFailure("startupException", exception, frameCount);
                }
            }

            if (_generationActive)
                StepGeneration(frameCount);
        }

        public void CancelForExit()
        {
            if (IsGenerationActive)
                CancelGeneration("viewUnbound", clearGeneratedMap: false);
        }

        public void Dispose()
        {
            if (IsGenerationActive)
                CancelGeneration("disposed", clearGeneratedMap: false);
            DisposeGeneration();
            ClearGeneratedRootChildren();
            _recovery.Reset();
            _view = null;
            _roadMaterial = null;
            _generatedRoot = null;
            _presentationCamera = null;
            _activeVisualRecipe = null;
            _lastProgress = RuntimeCityGenerationProgress.Idle;
            _statusMessage = "Disposed";
            _generateRequested = false;
            _restartRequested = false;
            _clearRequested = false;
            _cancelRequested = false;
            _restartAfterFrame = -1;
            _cameraStageAssigned = false;
            _cameraTransitionActive = false;
            _algorithmicVisualStage = RuntimeOperationMapVisualStage.TerrainAndRoads;
            _algorithmicStageElapsed = 0f;
            _algorithmicCompletionRevealActive = false;
        }

        private void BeginGeneration(int frameCount)
        {
            _recovery.Reset();
            _activeVisualRecipe = _view.VisualRecipe;
            RuntimeCitySpawnerSystemConfig config = _view.Config;
            RuntimeOperationMapVisualRecipe seedRecipe =
                _activeVisualRecipe != null
                    ? _activeVisualRecipe
                    : _view.DeterministicFallbackRecipe;
            uint attemptSeed = config != null
                ? config.RandomSeed
                : seedRecipe != null ? seedRecipe.Seed : 0u;
            _lastProgress = new RuntimeCityGenerationProgress(
                RuntimeCityGenerationStage.Planning,
                attemptSeed,
                config != null ? Mathf.Max(0, config.CityCount) : 0,
                generatedCityCount: 0,
                completedWorkItems: 0,
                totalWorkItems: 1,
                progress01: 0f);
            if (!TryConfigureGeneration(frameCount, out string failureReason))
            {
                HandleFailure(failureReason, exception: null, frameCount);
                return;
            }

            ResetCameraPresentation();

            RuntimeOperationMapVisualRecipe visualRecipe = _activeVisualRecipe;
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
                HandleFailure($"exception:{exception.GetType().Name}", exception, frameCount);
            }
        }

        private void CompleteGeneration()
        {
            RuntimeCityGenerationProgress cityProgress = _runtimeCity != null
                ? _runtimeCity.GenerationProgress
                : RuntimeCityGenerationProgress.Idle;
            if (_runtimeCity != null && cityProgress.Stage != RuntimeCityGenerationStage.Completed)
            {
                HandleFailure($"runtimeCityStopped:{cityProgress.Stage}", exception: null, Time.frameCount);
                return;
            }

            CreateAlgorithmicAftermathDressing();
            _lastProgress = GetCombinedProgress();
            if (_recovery.IsFallbackActive)
            {
                _statusMessage = "Deterministic fallback ready";
                _generationActive = false;
                Debug.Log(
                    $"[RuntimeCityRnD] {RuntimeCityGenerationProgress.VersionTag} result=FallbackCompleted " +
                    $"reason={_recovery.FailureReason} seed={_lastProgress.Seed} " +
                    $"recipe={_activeVisualRecipe?.RecipeVersion}",
                    _view);
                return;
            }

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

        private void BeginFallbackGeneration(int frameCount)
        {
            RuntimeOperationMapVisualRecipe fallbackRecipe = _recovery.FallbackRecipe;
            if (fallbackRecipe == null)
            {
                HandleFailure("missingFallbackRecipe", exception: null, frameCount);
                return;
            }

            try
            {
                DisposeGeneration(preserveProgress: true);
                ClearGeneratedRootChildren();
                EnsureGeneratedRoot();
                _activeVisualRecipe = fallbackRecipe;
                _generationStartedAt = Time.realtimeSinceStartup;
                _generationStartedFrame = frameCount;
                _lastLoggedStage = RuntimeCityGenerationStage.Idle;
                ResetCameraPresentation();
                _statusMessage = "Loading deterministic fallback map";
                _visualRecipePresentation = new RuntimeOperationMapVisualRecipePresentationSystemHelper();
                _visualRoutine = _visualRecipePresentation.Build(
                    fallbackRecipe,
                    _generatedRoot,
                    _view.VisualRecipeEntriesPerFrame,
                    _view.VisualRecipeFrameBudgetMilliseconds);
                _visualRoutineRunning = _visualRoutine != null;
                _generationActive = _visualRoutineRunning;
                if (!_generationActive)
                {
                    HandleFailure("fallbackRoutineMissing", exception: null, frameCount);
                    return;
                }

                Debug.Log(
                    $"[RuntimeCityRnD] {RuntimeCityGenerationProgress.VersionTag} action=FallbackStarted " +
                    $"reason={_recovery.FailureReason} seed={fallbackRecipe.Seed} " +
                    $"recipe={fallbackRecipe.RecipeVersion}",
                    _view);
            }
            catch (Exception exception)
            {
                HandleFailure($"fallbackStartupException:{exception.GetType().Name}", exception, frameCount);
            }
        }

        private void HandleFailure(string reason, Exception exception, int frameCount)
        {
            string retainedReason = string.IsNullOrEmpty(reason) ? "unspecified" : reason;
            RuntimeCityGenerationProgress progress = GetCombinedProgress();
            _lastProgress = RuntimeOperationMapGenerationRecoverySystemHelper.CreateTerminalProgress(
                progress,
                RuntimeCityGenerationStage.Failed);
            bool fallbackScheduled = _recovery.TryScheduleFallback(
                frameCount,
                _view != null && _view.DeterministicFallbackEnabled,
                _activeVisualRecipe,
                _view != null ? _view.DeterministicFallbackRecipe : null,
                retainedReason);
            _statusMessage = fallbackScheduled
                ? "Generation failed; preparing deterministic fallback"
                : $"Generation failed: {retainedReason}";
            _generationActive = false;
            _visualRoutineRunning = false;
            _algorithmicCompletionRevealActive = false;

            if (exception != null)
                Debug.LogException(exception, _view);

            DisposeGeneration(preserveProgress: true);
            ClearGeneratedRootChildren();
            Debug.LogError(
                $"[RuntimeCityRnD] {RuntimeCityGenerationProgress.VersionTag} result=Failed " +
                $"reason={retainedReason} stage={progress.Stage} seed={progress.Seed} " +
                $"fallbackScheduled={(fallbackScheduled ? 1 : 0)}",
                _view);
        }

        private void CancelGeneration(string reason, bool clearGeneratedMap)
        {
            if (!IsGenerationActive)
                return;

            RuntimeCityGenerationProgress progress = GetCombinedProgress();
            RuntimeCityGenerationStage interruptedStage = progress.Stage;
            _lastProgress = RuntimeOperationMapGenerationRecoverySystemHelper.CreateTerminalProgress(
                progress,
                RuntimeCityGenerationStage.Cancelled);
            _statusMessage = $"Generation cancelled: {reason}";
            _generationActive = false;
            _visualRoutineRunning = false;
            _algorithmicCompletionRevealActive = false;
            _recovery.Reset();
            DisposeGeneration(preserveProgress: true);
            if (clearGeneratedMap)
                ClearGeneratedRootChildren();
            _activeVisualRecipe = _view != null ? _view.VisualRecipe : null;

            Debug.LogWarning(
                $"[RuntimeCityRnD] {RuntimeCityGenerationProgress.VersionTag} result=Cancelled " +
                $"reason={reason} stage={interruptedStage} seed={progress.Seed} " +
                $"work={progress.CompletedWorkItems}/{progress.TotalWorkItems}",
                _view);
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

        private bool TryConfigureGeneration(int frameCount, out string failureReason)
        {
            failureReason = null;
            RuntimeCitySpawnerSystemConfig config = _view.Config;
            if (config == null)
            {
                failureReason = "missingConfig";
                return false;
            }

            if (config.CityCount <= 0)
            {
                failureReason = "cityCountZero";
                return false;
            }

            EnsureGeneratedRoot();
            GridConfig grid = CreateGrid();
            bool createAlgorithmicVisuals = _activeVisualRecipe == null;
            if (createAlgorithmicVisuals)
            {
                if (!CreateAlgorithmicFoundation(grid))
                {
                    failureReason = "missingAlgorithmicGroundMaterial";
                    return false;
                }

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

            failureReason = "notStarted";
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
            RuntimeOperationMapVisualRecipe visualRecipe = _activeVisualRecipe;
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
            if (IsGenerationActive)
                CancelGeneration("clearRequested", clearGeneratedMap: false);
            else
                DisposeGeneration();

            ClearGeneratedRootChildren();
            _recovery.Reset();
            _activeVisualRecipe = _view != null ? _view.VisualRecipe : null;
            _lastProgress = RuntimeCityGenerationProgress.Idle;
            _statusMessage = "Cleared";
        }

        private void DisposeGeneration(bool preserveProgress = false)
        {
            RuntimeCityGenerationProgress retainedProgress = _lastProgress;
            _algorithmicAftermathPresentation?.Dispose();
            _algorithmicAftermathPresentation = null;
            if (_runtimeCity != null)
            {
                _runtimeCity.Dispose();
                if (!preserveProgress)
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
            if (preserveProgress)
                _lastProgress = retainedProgress;
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

        private bool CreateAlgorithmicFoundation(GridConfig grid)
        {
            Material material = _view.AlgorithmicGroundMaterial ??
                                _view.RoadShoulderMaterial ??
                                _roadMaterial;
            if (material == null)
                return false;

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
            return _algorithmicVisualQuality.FoundationVisualCount == 1;
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
            RuntimeOperationMapVisualRecipe recipe = _activeVisualRecipe;
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
