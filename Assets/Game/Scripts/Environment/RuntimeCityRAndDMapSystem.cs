using Unity.Entities;
using UnityEngine;
using Game.Configs;

namespace Game.Runtime
{
    [UpdateInGroup(typeof(PresentationSystemGroup))]
    public sealed partial class RuntimeCityRAndDMapSystem : SystemBase
    {
        private RuntimeCityRAndDMapCompositionSystemHelper _composition;
        private RuntimeCityRAndDMapView _view;
        private RuntimeCityGenerationStage _lastPresentedStage = (RuntimeCityGenerationStage)(-1);
        private uint _lastPresentedSeed = uint.MaxValue;
        private int _lastPresentedPercent = -1;
        private int _lastPresentedRoadStrokeCount = -1;
        private int _lastPresentedRoadCellCount = -1;
        private int _lastPresentedVisualCount = -1;
        private int _lastPresentedFoundationCount = -1;
        private string _lastPresentedMessage;
        private bool _presentationVisible;

        public RuntimeCityGenerationProgress Progress =>
            _composition?.Progress ?? RuntimeCityGenerationProgress.Idle;
        public int RoadStrokeCount => _composition?.RoadStrokeCount ?? 0;
        public int RoadCellCount => _composition?.RoadCellCount ?? 0;
        public int VisualBuildingCount => _composition?.VisualBuildingCount ?? 0;
        public int PlannedBuildingCount => _composition?.PlannedBuildingCount ?? 0;
        public int MaxObservedConsecutivePrefabSelections =>
            _composition?.MaxObservedConsecutivePrefabSelections ?? 0;
        public int VisualRecipeEntryCount => _composition?.VisualRecipeEntryCount ?? 0;
        public int VisualRecipeRendererCount => _composition?.VisualRecipeRendererCount ?? 0;
        public int FoundationVisualCount => _composition?.FoundationVisualCount ?? 0;
        public int SuppressedObstructionCount => _composition?.SuppressedObstructionCount ?? 0;
        public int AlgorithmicDistrictSurfaceCount => _composition?.AlgorithmicDistrictSurfaceCount ?? 0;
        public int AlgorithmicAftermathDressingCount => _composition?.AlgorithmicAftermathDressingCount ?? 0;
        public float MaxVisualBatchMilliseconds => _composition?.MaxVisualBatchMilliseconds ?? 0f;
        public int FrameBudgetYieldCount => _composition?.FrameBudgetYieldCount ?? 0;
        public RuntimeOperationMapVisualStage CurrentVisualStage =>
            _composition?.CurrentVisualStage ?? RuntimeOperationMapVisualStage.TerrainAndRoads;

        protected override void OnCreate()
        {
            _composition = new RuntimeCityRAndDMapCompositionSystemHelper();
            Enabled = false;
        }

        protected override void OnUpdate()
        {
            if (_view == null)
            {
                Enabled = false;
                return;
            }

            _composition.Tick(UnityEngine.Time.frameCount);
            _composition.AdvancePresentation(UnityEngine.Time.unscaledDeltaTime);
            PublishPresentation();
        }

        protected override void OnDestroy()
        {
            _composition?.Dispose();
            _composition = null;
            _view = null;
        }

        public void Bind(RuntimeCityRAndDMapView view)
        {
            if (view == null || ReferenceEquals(_view, view))
                return;

            if (_view != null)
                Unbind(_view);

            ResetPresentationCache();
            _view = view;
            Material roadMaterial = view.RoadMaterial;
            _composition.Configure(view, roadMaterial);
            Enabled = true;
            if (view.GenerateOnStart)
                _composition.RequestGeneration();
            PublishPresentation();
        }

        public void Unbind(RuntimeCityRAndDMapView view)
        {
            if (!ReferenceEquals(_view, view))
                return;

            _composition.Dispose();
            _view.ApplyPresentation(null);
            _view = null;
            ResetPresentationCache();
            Enabled = false;
        }

        public void RequestGeneration()
        {
            _composition?.RequestGeneration();
        }

        public void RequestRestart()
        {
            _composition?.RequestRestart();
        }

        public void RequestClear()
        {
            _composition?.RequestClear();
        }

        private void PublishPresentation()
        {
            if (_view == null || !_view.ShowDebugOverlay)
                return;

            RuntimeCityGenerationProgress progress = Progress;
            if (progress.Stage == RuntimeCityGenerationStage.Completed &&
                _composition.IsPresentationComplete)
            {
                if (_presentationVisible)
                    _view.ApplyPresentation(string.Empty);
                _presentationVisible = false;
                return;
            }

            int percent = Mathf.RoundToInt(progress.Progress01 * 100f);
            int roadStrokeCount = RoadStrokeCount;
            int roadCellCount = RoadCellCount;
            int recipeVisualCount = VisualRecipeEntryCount;
            int visualCount = recipeVisualCount > 0
                ? recipeVisualCount : VisualBuildingCount;
            int foundationCount = FoundationVisualCount;
            string message = _composition.StatusMessage;
            bool presentationChanged =
                progress.Stage != _lastPresentedStage ||
                progress.Seed != _lastPresentedSeed ||
                percent != _lastPresentedPercent ||
                roadStrokeCount != _lastPresentedRoadStrokeCount ||
                roadCellCount != _lastPresentedRoadCellCount ||
                visualCount != _lastPresentedVisualCount ||
                foundationCount != _lastPresentedFoundationCount ||
                !string.Equals(message, _lastPresentedMessage, System.StringComparison.Ordinal);
            if (!presentationChanged)
                return;

            _lastPresentedStage = progress.Stage;
            _lastPresentedSeed = progress.Seed;
            _lastPresentedPercent = percent;
            _lastPresentedRoadStrokeCount = roadStrokeCount;
            _lastPresentedRoadCellCount = roadCellCount;
            _lastPresentedVisualCount = visualCount;
            _lastPresentedFoundationCount = foundationCount;
            _lastPresentedMessage = message;
            string status =
                $"{message}\n" +
                $"{percent}% | Stage {progress.Stage} | Roads {roadStrokeCount}/{roadCellCount}\n" +
                $"Seed {progress.Seed} | {RuntimeCityGenerationProgress.VersionTag} | Visuals {visualCount} | Foundation {foundationCount}";
            _view.ApplyPresentation(status);
            _presentationVisible = true;
        }

        private void ResetPresentationCache()
        {
            _lastPresentedStage = (RuntimeCityGenerationStage)(-1);
            _lastPresentedSeed = uint.MaxValue;
            _lastPresentedPercent = -1;
            _lastPresentedRoadStrokeCount = -1;
            _lastPresentedRoadCellCount = -1;
            _lastPresentedVisualCount = -1;
            _lastPresentedFoundationCount = -1;
            _lastPresentedMessage = null;
            _presentationVisible = false;
        }
    }
}
