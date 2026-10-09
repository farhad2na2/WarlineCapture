using Unity.Entities;

namespace Game.Runtime
{
    // Runtime-only link to the authoritative production owner. Never serialize
    // this reference, a GameObject instance ID or a cross-world Entity handle.
    internal sealed class SkirmishNativeBuildingCheckpointRecord : IComponentData
    {
        internal RuntimeBuildingEntity Building;
        internal SkirmishNativeCheckpointPrefabRegistry Prefabs;
        internal System.Func<SkirmishNativeProductionCheckpoint.Prepared, bool> TryRestoreProduction;
        internal System.Action<SkirmishNativeProductionCheckpoint.Prepared> RollbackProduction;
    }
}
