namespace Game.Runtime
{
    public sealed class RuntimeCityReadModelCompositionSystemHelper
    {
        public bool SpawnOnStartEnabled { get; private set; }
        public bool HasSpawned { get; private set; }
        public bool IsGenerating { get; private set; }

        public void Publish(bool spawnOnStartEnabled, bool hasSpawned, bool isGenerating)
        {
            SpawnOnStartEnabled = spawnOnStartEnabled;
            HasSpawned = hasSpawned;
            IsGenerating = isGenerating;
        }
    }
}
