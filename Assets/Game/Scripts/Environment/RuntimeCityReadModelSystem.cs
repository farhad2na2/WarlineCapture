using Unity.Entities;

public sealed partial class RuntimeCityReadModelSystem : SystemBase
{
    public bool SpawnOnStartEnabled { get; private set; }
    public bool HasSpawned { get; private set; }
    public bool IsGenerating { get; private set; }

    protected override void OnCreate()
    {
        Enabled = false;
    }

    protected override void OnUpdate()
    {
    }

    protected override void OnDestroy()
    {
        Publish(false, false, false);
    }

    public void Publish(bool spawnOnStartEnabled, bool hasSpawned, bool isGenerating)
    {
        SpawnOnStartEnabled = spawnOnStartEnabled;
        HasSpawned = hasSpawned;
        IsGenerating = isGenerating;
    }
}
