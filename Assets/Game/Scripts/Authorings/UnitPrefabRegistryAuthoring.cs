using System.Linq;
using Unity.Entities;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class UnitPrefabRegistryAuthoring : MonoBehaviour
{
    [SerializeField] private UnitPrefabRegistryAuthoringConfig config;
    [SerializeField, HideInInspector] private GameObject[] unitSpawnPrefabs = System.Array.Empty<GameObject>();

    private void OnValidate()
    {
        ApplyConfigIfAvailable();
    }

    private void ApplyConfigIfAvailable()
    {
        if (config == null || config.UnitSpawnPrefabs == null)
        {
            unitSpawnPrefabs = System.Array.Empty<GameObject>();
            return;
        }

        unitSpawnPrefabs = config.UnitSpawnPrefabs.ToArray();
    }

    private sealed class BakerImpl : Baker<UnitPrefabRegistryAuthoring>
    {
        public override void Bake(UnitPrefabRegistryAuthoring authoring)
        {
            authoring.ApplyConfigIfAvailable();

            Entity entity = GetEntity(TransformUsageFlags.None);
            AddComponent<UnitPrefabRegistryTag>(entity);

            DynamicBuffer<UnitPrefabRegistryEntry> buffer = AddBuffer<UnitPrefabRegistryEntry>(entity);
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
