using UnityEngine;

namespace Game.Runtime
{
    public enum RuntimeCityGenerationStage
    {
        Idle = 0,
        Planning = 1,
        Roads = 2,
        Landmarks = 3,
        Buildings = 4,
        Decorations = 5,
        Finalizing = 6,
        Completed = 7,
        Cancelled = 8,
        Failed = 9
    }

    public readonly struct RuntimeCityGenerationProgress
    {
        public const string VersionTag = "RuntimeCityM01BlockedRoute_R60_2026-07-16";

        public RuntimeCityGenerationProgress(
            RuntimeCityGenerationStage stage,
            uint seed,
            int requestedCityCount,
            int generatedCityCount,
            int completedWorkItems,
            int totalWorkItems,
            float progress01)
        {
            Stage = stage;
            Seed = seed;
            RequestedCityCount = Mathf.Max(0, requestedCityCount);
            GeneratedCityCount = Mathf.Max(0, generatedCityCount);
            CompletedWorkItems = Mathf.Max(0, completedWorkItems);
            TotalWorkItems = Mathf.Max(0, totalWorkItems);
            Progress01 = Mathf.Clamp01(progress01);
        }

        public RuntimeCityGenerationStage Stage { get; }
        public uint Seed { get; }
        public int RequestedCityCount { get; }
        public int GeneratedCityCount { get; }
        public int CompletedWorkItems { get; }
        public int TotalWorkItems { get; }
        public float Progress01 { get; }

        public bool IsTerminal =>
            Stage == RuntimeCityGenerationStage.Completed ||
            Stage == RuntimeCityGenerationStage.Cancelled ||
            Stage == RuntimeCityGenerationStage.Failed;

        public static RuntimeCityGenerationProgress Idle =>
            new(RuntimeCityGenerationStage.Idle, 0, 0, 0, 0, 0, 0f);
    }

    internal sealed class RuntimeCityGenerationProgressState
    {
        private RuntimeCityGenerationProgress _current = RuntimeCityGenerationProgress.Idle;

        public RuntimeCityGenerationProgress Current => _current;

        public void Begin(uint seed, int requestedCityCount)
        {
            _current = Create(
                RuntimeCityGenerationStage.Planning,
                seed,
                requestedCityCount,
                generatedCityCount: 0,
                completedWorkItems: 0,
                totalWorkItems: 1);
        }

        public void Report(
            RuntimeCityGenerationStage stage,
            int generatedCityCount,
            int completedWorkItems,
            int totalWorkItems)
        {
            _current = Create(
                stage,
                _current.Seed,
                _current.RequestedCityCount,
                generatedCityCount,
                completedWorkItems,
                totalWorkItems);
        }

        public void Complete(int generatedCityCount)
        {
            int total = Mathf.Max(1, _current.TotalWorkItems);
            _current = new RuntimeCityGenerationProgress(
                RuntimeCityGenerationStage.Completed,
                _current.Seed,
                _current.RequestedCityCount,
                generatedCityCount,
                total,
                total,
                1f);
        }

        public void Cancel()
        {
            if (_current.Stage == RuntimeCityGenerationStage.Idle || _current.IsTerminal)
                return;

            _current = new RuntimeCityGenerationProgress(
                RuntimeCityGenerationStage.Cancelled,
                _current.Seed,
                _current.RequestedCityCount,
                _current.GeneratedCityCount,
                _current.CompletedWorkItems,
                _current.TotalWorkItems,
                _current.Progress01);
        }

        public void Fail()
        {
            if (_current.Stage == RuntimeCityGenerationStage.Completed)
                return;

            _current = new RuntimeCityGenerationProgress(
                RuntimeCityGenerationStage.Failed,
                _current.Seed,
                _current.RequestedCityCount,
                _current.GeneratedCityCount,
                _current.CompletedWorkItems,
                _current.TotalWorkItems,
                _current.Progress01);
        }

        private static RuntimeCityGenerationProgress Create(
            RuntimeCityGenerationStage stage,
            uint seed,
            int requestedCityCount,
            int generatedCityCount,
            int completedWorkItems,
            int totalWorkItems)
        {
            int safeTotal = Mathf.Max(1, totalWorkItems);
            float stageProgress = Mathf.Clamp01((float)Mathf.Max(0, completedWorkItems) / safeTotal);
            GetStageRange(stage, out float start, out float end);
            return new RuntimeCityGenerationProgress(
                stage,
                seed,
                requestedCityCount,
                generatedCityCount,
                completedWorkItems,
                safeTotal,
                Mathf.Lerp(start, end, stageProgress));
        }

        private static void GetStageRange(RuntimeCityGenerationStage stage, out float start, out float end)
        {
            switch (stage)
            {
                case RuntimeCityGenerationStage.Planning:
                    start = 0.02f;
                    end = 0.10f;
                    return;
                case RuntimeCityGenerationStage.Roads:
                    start = 0.10f;
                    end = 0.35f;
                    return;
                case RuntimeCityGenerationStage.Landmarks:
                    start = 0.35f;
                    end = 0.45f;
                    return;
                case RuntimeCityGenerationStage.Buildings:
                    start = 0.45f;
                    end = 0.85f;
                    return;
                case RuntimeCityGenerationStage.Decorations:
                    start = 0.85f;
                    end = 0.95f;
                    return;
                case RuntimeCityGenerationStage.Finalizing:
                    start = 0.95f;
                    end = 0.99f;
                    return;
                case RuntimeCityGenerationStage.Completed:
                    start = 1f;
                    end = 1f;
                    return;
                default:
                    start = 0f;
                    end = 0f;
                    return;
            }
        }
    }
}
