using Game.Components;
using Unity.Entities;

namespace Game.Runtime
{
    public sealed partial class PerformanceDiagnosticsSystemHelper
    {
        private readonly AndroidPerformanceRecorder _androidPerformanceRecorder = new();

        public void MarkMatchReady()
        {
            _androidPerformanceRecorder.MarkMatchReady();
        }

        private void InitializeAndroidPerformanceRecorder()
        {
            _androidPerformanceRecorder.Initialize(_enableProfilerMarkerDiagnostics);
        }

        private void SampleAndroidPerformanceRecorder(bool gameplayActive)
        {
            _androidPerformanceRecorder.Sample(
                gameplayActive,
                _batchesRecorder,
                _setPassCallsRecorder,
                _trianglesRecorder,
                _verticesRecorder);
            SampleRenderVirtualizationMetrics(gameplayActive);
        }

        private void SampleRenderVirtualizationMetrics(bool gameplayActive)
        {
            if (!gameplayActive ||
                !_androidPerformanceRecorder
                    .ShouldSampleRenderVirtualizationMetrics)
            {
                return;
            }

            World world = World.DefaultGameObjectInjectionWorld;
            if (world == null || !world.IsCreated)
                return;

            EntityManager entityManager = world.EntityManager;
            using EntityQuery query = entityManager.CreateEntityQuery(
                ComponentType.ReadOnly<
                    OperationMapRenderVirtualizationMetricsComponent>());
            if (query.CalculateEntityCount() != 1)
                return;

            _androidPerformanceRecorder.RecordRenderVirtualizationMetrics(
                query.GetSingleton<
                    OperationMapRenderVirtualizationMetricsComponent>());
        }

        private void DisposeAndroidPerformanceRecorder()
        {
            _androidPerformanceRecorder.Dispose();
        }
    }
}
