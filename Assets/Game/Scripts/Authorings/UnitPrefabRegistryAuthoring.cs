using System.Linq;
using Unity.Collections;
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
            DynamicBuffer<UnitTransportAirdropVisualPrefabRegistryEntry> airdropVisualRegistry =
                AddBuffer<UnitTransportAirdropVisualPrefabRegistryEntry>(entity);
            for (int i = 0; i < authoring.unitSpawnPrefabs.Length; i++)
            {
                GameObject prefab = authoring.unitSpawnPrefabs[i];
                if (prefab == null)
                    continue;

                buffer.Add(new UnitPrefabRegistryEntry
                {
                    Prefab = GetEntity(prefab, TransformUsageFlags.Dynamic)
                });
                AddAirdropVisualRegistryEntry(airdropVisualRegistry, prefab);
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

        private void AddAirdropVisualRegistryEntry(
            DynamicBuffer<UnitTransportAirdropVisualPrefabRegistryEntry> registry,
            GameObject unitPrefab)
        {
            if (unitPrefab == null ||
                !unitPrefab.TryGetComponent(out UnitGridAuthoring unitAuthoring) ||
                !unitAuthoring.IsAirUnit ||
                !unitAuthoring.ProductionTransportUsesRunwayLanding ||
                (unitAuthoring.SoldierTransportCapacity <= 0 && unitAuthoring.VehicleTransportCapacity <= 0))
            {
                return;
            }

            FixedString64Bytes sourceKey = new(unitPrefab.name);
            for (int i = 0; i < registry.Length; i++)
            {
                if (registry[i].SourceKey.Equals(sourceKey))
                    return;
            }

            GameObject soldierParachutePrefab = unitAuthoring.SoldierParachuteVisualPrefab;
            GameObject vehicleEmergencyDropPrefab = unitAuthoring.VehicleEmergencyDropVisualPrefab;
            if (unitAuthoring.SoldierTransportCapacity > 0 && soldierParachutePrefab == null)
                ThrowMissingAirdropVisual(unitPrefab, nameof(unitAuthoring.SoldierParachuteVisualPrefab));
            if (unitAuthoring.VehicleTransportCapacity > 0 && vehicleEmergencyDropPrefab == null)
                ThrowMissingAirdropVisual(unitPrefab, nameof(unitAuthoring.VehicleEmergencyDropVisualPrefab));
            RequireValidAirdropVisualPrefab(
                soldierParachutePrefab,
                unitPrefab,
                nameof(unitAuthoring.SoldierParachuteVisualPrefab));
            RequireValidAirdropVisualPrefab(
                vehicleEmergencyDropPrefab,
                unitPrefab,
                nameof(unitAuthoring.VehicleEmergencyDropVisualPrefab));

            DependsOn(unitPrefab);
            if (soldierParachutePrefab != null)
                DependsOn(soldierParachutePrefab);
            if (vehicleEmergencyDropPrefab != null)
                DependsOn(vehicleEmergencyDropPrefab);

            registry.Add(new UnitTransportAirdropVisualPrefabRegistryEntry
            {
                SourceKey = sourceKey,
                SoldierParachuteVisualPrefab = soldierParachutePrefab != null
                    ? GetEntity(soldierParachutePrefab, TransformUsageFlags.Dynamic | TransformUsageFlags.Renderable)
                    : Entity.Null,
                VehicleEmergencyDropVisualPrefab = vehicleEmergencyDropPrefab != null
                    ? GetEntity(vehicleEmergencyDropPrefab, TransformUsageFlags.Dynamic | TransformUsageFlags.Renderable)
                    : Entity.Null
            });
        }

        private static void ThrowMissingAirdropVisual(GameObject unitPrefab, string fieldName)
        {
            throw new System.InvalidOperationException(
                $"{nameof(UnitPrefabRegistryAuthoring)} requires {fieldName} on transport unit '{unitPrefab.name}' for airdrop spawning.");
        }

        private static void RequireValidAirdropVisualPrefab(GameObject prefab, GameObject unitPrefab, string fieldName)
        {
            if (prefab == null)
                return;

            try
            {
                _ = prefab.scene;
            }
            catch (MissingReferenceException exception)
            {
                throw new System.InvalidOperationException(
                    $"{nameof(UnitPrefabRegistryAuthoring)} requires a valid prefab reference for {fieldName} on transport unit '{unitPrefab.name}'. " +
                    "Reassign the prefab in the UnitGridAuthoring config asset and rebake the subscene.",
                    exception);
            }
        }
    }
}
