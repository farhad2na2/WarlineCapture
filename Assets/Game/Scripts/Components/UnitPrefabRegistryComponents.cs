using Unity.Entities;

public struct UnitPrefabRegistryTag : IComponentData
{
}

public struct UnitPrefabRegistryEntry : IBufferElementData
{
    public Entity Prefab;
}
