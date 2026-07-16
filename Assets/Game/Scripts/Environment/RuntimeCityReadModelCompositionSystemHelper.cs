namespace Game.Runtime
{
    public sealed class RuntimeCityReadModelCompositionSystemHelper
    {
        public bool SpawnOnStartEnabled { get; private set; }
        public bool HasSpawned { get; private set; }
        public bool IsGenerating { get; private set; }
        public RuntimeCityGenerationProgress GenerationProgress { get; private set; } = RuntimeCityGenerationProgress.Idle;

        public void Publish(
            bool spawnOnStartEnabled,
            bool hasSpawned,
            bool isGenerating,
            RuntimeCityGenerationProgress generationProgress)
        {
            SpawnOnStartEnabled = spawnOnStartEnabled;
            HasSpawned = hasSpawned;
            IsGenerating = isGenerating;
            GenerationProgress = generationProgress;
        }
    }
}
