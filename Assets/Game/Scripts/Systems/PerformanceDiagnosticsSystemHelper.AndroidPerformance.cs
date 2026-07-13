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
        }

        private void DisposeAndroidPerformanceRecorder()
        {
            _androidPerformanceRecorder.Dispose();
        }
    }
}
