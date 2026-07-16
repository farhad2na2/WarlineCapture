using Game.Configs;

namespace Game.Runtime
{
    internal sealed class RuntimeOperationMapGenerationRecoverySystemHelper
    {
        private RuntimeOperationMapVisualRecipe _fallbackRecipe;
        private string _failureReason;
        private int _fallbackAfterFrame = -1;
        private int _fallbackAttemptCount;
        private bool _fallbackScheduled;
        private bool _fallbackActive;

        public bool IsFallbackScheduled => _fallbackScheduled;
        public bool IsFallbackActive => _fallbackActive;
        public int FallbackAttemptCount => _fallbackAttemptCount;
        public string FailureReason => _failureReason;
        public RuntimeOperationMapVisualRecipe FallbackRecipe => _fallbackRecipe;

        public bool TryScheduleFallback(
            int frameCount,
            bool fallbackEnabled,
            RuntimeOperationMapVisualRecipe primaryRecipe,
            RuntimeOperationMapVisualRecipe fallbackRecipe,
            string failureReason)
        {
            if (!fallbackEnabled ||
                fallbackRecipe == null ||
                _fallbackAttemptCount > 0 ||
                ReferenceEquals(primaryRecipe, fallbackRecipe))
            {
                return false;
            }

            _fallbackRecipe = fallbackRecipe;
            _failureReason = string.IsNullOrEmpty(failureReason) ? "unspecified" : failureReason;
            _fallbackAfterFrame = frameCount + 1;
            _fallbackAttemptCount = 1;
            _fallbackScheduled = true;
            _fallbackActive = false;
            return true;
        }

        public bool TryActivateFallback(int frameCount)
        {
            if (!_fallbackScheduled || frameCount < _fallbackAfterFrame)
                return false;

            _fallbackScheduled = false;
            _fallbackAfterFrame = -1;
            _fallbackActive = true;
            return true;
        }

        public void Reset()
        {
            _fallbackRecipe = null;
            _failureReason = null;
            _fallbackAfterFrame = -1;
            _fallbackAttemptCount = 0;
            _fallbackScheduled = false;
            _fallbackActive = false;
        }

        public static RuntimeCityGenerationProgress CreateTerminalProgress(
            RuntimeCityGenerationProgress progress,
            RuntimeCityGenerationStage terminalStage)
        {
            return new RuntimeCityGenerationProgress(
                terminalStage,
                progress.Seed,
                progress.RequestedCityCount,
                progress.GeneratedCityCount,
                progress.CompletedWorkItems,
                progress.TotalWorkItems,
                progress.Progress01);
        }
    }
}
