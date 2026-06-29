using Unity.Collections;
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
            DynamicBuffer<UnitTransportAirdropVisualPrefabRegistryEntry> airdropVisualRegistry =
                AddBuffer<UnitTransportAirdropVisualPrefabRegistryEntry>(entity);

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
                AddAirdropVisualRegistryEntry(airdropVisualRegistry, prefab);
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
                $"{nameof(BattleScenarioLabUnitPrefabRegistryAuthoring)} requires {fieldName} on transport unit '{unitPrefab.name}' for Scenario Lab airdrop spawning.");
        }
    }
}
