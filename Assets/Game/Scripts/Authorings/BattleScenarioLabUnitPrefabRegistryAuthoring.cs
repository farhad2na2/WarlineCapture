using Unity.Entities;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class BattleScenarioLabUnitPrefabRegistryAuthoring : MonoBehaviour
{
    [SerializeField] private GameObject[] unitSpawnPrefabs = System.Array.Empty<GameObject>();

    private sealed class BakerImpl : Baker<BattleScenarioLabUnitPrefabRegistryAuthoring>
    {
        public override void Bake(BattleScenarioLabUnitPrefabRegistryAuthoring authoring)
        {
            Entity entity = GetEntity(TransformUsageFlags.None);
            AddComponent<UnitPrefabRegistryTag>(entity);
            DynamicBuffer<UnitPrefabRegistryEntry> buffer = AddBuffer<UnitPrefabRegistryEntry>(entity);

            if (authoring.unitSpawnPrefabs == null)
                return;

            for (int i = 0; i < authoring.unitSpawnPrefabs.Length; i++)
            {
                GameObject prefab = authoring.unitSpawnPrefabs[i];
                if (prefab == null)
                    continue;

                buffer.Add(new UnitPrefabRegistryEntry
                {
                    Prefab = GetEntity(prefab, TransformUsageFlags.Dynamic)
                });
            }
        }
    }
}
