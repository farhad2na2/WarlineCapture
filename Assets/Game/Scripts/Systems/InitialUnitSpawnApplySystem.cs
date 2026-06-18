using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

[DisableAutoCreation]
public partial struct InitialUnitSpawnApplySystem : ISystem
{
    public void OnCreate(ref SystemState state)
    {
        state.Enabled = false;
    }

    public void OnUpdate(ref SystemState state)
    {
    }

    public Entity InstantiateAndConfigureSpawnedUnit(
        EntityManager em,
        EntityCommandBuffer ecb,
        Entity prefab,
        bool hasPrefab,
        byte faction,
        int2 cell,
        float3 pos)
    {
        Entity instance = ecb.Instantiate(prefab);
        SetOrAddComponent(em, ecb, instance, prefab, hasPrefab, new UnitGrid { Cell = cell });
        SetOrAddComponent(em, ecb, instance, prefab, hasPrefab, LocalTransform.FromPosition(pos));
        SetOrAddComponent(em, ecb, instance, prefab, hasPrefab, new UnitPrevWorldPos { Value = pos });
        SetOrAddComponent(em, ecb, instance, prefab, hasPrefab, new UnitMoveVisualComponent { IsMoving = 0, StillSeconds = 0f });
        SetOrAddComponent(em, ecb, instance, prefab, hasPrefab, new Faction { Id = faction });
        SetOrAddComponent(em, ecb, instance, prefab, hasPrefab, new UnitRespawnPrefab { Prefab = Entity.Null });
        if (hasPrefab && em.HasComponent<UnitSourcePrefabKey>(prefab))
            SetOrAddComponent(em, ecb, instance, prefab, hasPrefab, em.GetComponentData<UnitSourcePrefabKey>(prefab));
        if (hasPrefab && em.HasComponent<UnitTransportAirdropVisualPrefabs>(prefab))
            SetOrAddComponent(em, ecb, instance, prefab, hasPrefab, em.GetComponentData<UnitTransportAirdropVisualPrefabs>(prefab));
        SetOrAddComponent(em, ecb, instance, prefab, hasPrefab, new UnitAttackCooldownComponent { CooldownRemaining = 0f });
        return instance;
    }

    private static void SetOrAddComponent<T>(
        EntityManager em,
        EntityCommandBuffer ecb,
        Entity instance,
        Entity prefab,
        bool hasPrefab,
        T component)
        where T : unmanaged, IComponentData
    {
        if (hasPrefab && em.HasComponent<T>(prefab))
            ecb.SetComponent(instance, component);
        else
            ecb.AddComponent(instance, component);
    }
}
