using System.Linq;
using Unity.Entities;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class UnitPrefabRegistryAuthoring : MonoBehaviour
{
    [SerializeField] private UnitPrefabRegistryAuthoringConfig config;
    [SerializeField, HideInInspector] private GameObject[] unitSpawnPrefabs = System.Array.Empty<GameObject>();
    [SerializeField, HideInInspector] private GameObject unitSelectionMarkerPrefab;
    [SerializeField, HideInInspector] private GameObject unitHealthBarPrefab;

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
        if (config.UnitSelectionMarkerPrefab != null)
            unitSelectionMarkerPrefab = config.UnitSelectionMarkerPrefab;
        if (config.UnitHealthBarPrefab != null)
            unitHealthBarPrefab = config.UnitHealthBarPrefab;
    }

    private sealed class BakerImpl : Baker<UnitPrefabRegistryAuthoring>
    {
        public override void Bake(UnitPrefabRegistryAuthoring authoring)
        {
            if (authoring.config != null)
                DependsOn(authoring.config);

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

            if (authoring.unitSelectionMarkerPrefab != null || authoring.unitHealthBarPrefab != null)
            {
                AddComponent(entity, new UnitSharedVisualPrefabReferences
                {
                    SelectionMarkerPrefab = authoring.unitSelectionMarkerPrefab != null
                        ? GetEntity(authoring.unitSelectionMarkerPrefab, TransformUsageFlags.Dynamic)
                        : Entity.Null,
                    HealthBarPrefab = authoring.unitHealthBarPrefab != null
                        ? GetEntity(authoring.unitHealthBarPrefab, TransformUsageFlags.Dynamic)
                        : Entity.Null
                });
            }
        }
    }
}
