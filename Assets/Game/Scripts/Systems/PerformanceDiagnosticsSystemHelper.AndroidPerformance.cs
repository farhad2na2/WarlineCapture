using Game.Components;
using Unity.Entities;
using UnityEngine;

namespace Game.Runtime
{
    public sealed partial class PerformanceDiagnosticsSystemHelper
    {
        private readonly AndroidPerformanceRecorder _androidPerformanceRecorder = new();
        private bool _performanceRecorderSuppressesStandardDiagnostics;

        public void MarkMatchReady()
        {
            _androidPerformanceRecorder.MarkMatchReady();
        }

        private void InitializeAndroidPerformanceRecorder()
        {
            _androidPerformanceRecorder.Initialize(_enableProfilerMarkerDiagnostics);
            _performanceRecorderSuppressesStandardDiagnostics =
                _androidPerformanceRecorder.SuppressesStandardDiagnostics;
            if (_performanceRecorderSuppressesStandardDiagnostics)
                PerformanceDiagnosticsCapturePolicy.SetSuppressLogging(true);
        }

        private void SampleAndroidPerformanceRecorder(
            bool gameplayActive,
            Camera camera)
        {
            _androidPerformanceRecorder.Sample(
                gameplayActive,
                _batchesRecorder,
                _setPassCallsRecorder,
                _trianglesRecorder,
                _verticesRecorder);
            _androidPerformanceRecorder.SampleVrp067DestructionMatrix(
                gameplayActive,
                camera);
            _androidPerformanceRecorder.SampleVrp095StateScenario(
                gameplayActive,
                camera);
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
            if (_performanceRecorderSuppressesStandardDiagnostics)
            {
                PerformanceDiagnosticsCapturePolicy.SetSuppressLogging(false);
                _performanceRecorderSuppressesStandardDiagnostics = false;
            }
        }
    }
}
