using Unity.Entities;

namespace Game.Components
{
    public struct UnitPrefabRegistryTag : IComponentData
    {
    }

    public struct UnitPrefabRegistryEntry : IBufferElementData
    {
        public Entity Prefab;
    }

    public struct UnitSharedVisualPrefabReferences : IComponentData
    {
        public Entity SelectionMarkerPrefab;
        public Entity HealthBarPrefab;
    }
}
