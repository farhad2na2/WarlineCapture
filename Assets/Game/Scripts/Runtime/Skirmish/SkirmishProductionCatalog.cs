using System;
using System.Collections.Generic;
using Game.Components;
using Game.Configs;
using Game.Skirmish.Contracts;
using Unity.Entities;
using UnityEngine;

namespace Game.Runtime
{
    public sealed class SkirmishProductionCatalogRecord : IComponentData
    {
        public SkirmishProductionCatalog Catalog;
    }

    /// <summary>Compiles the resolved army into the shared production catalog without changing authored assets.</summary>
    public sealed class SkirmishProductionCatalog : IDisposable
    {
        public BuildingPlacementSystemConfig Config { get; private set; }
        private UnitPrefabRegistryAuthoringConfig registry;
        private readonly Dictionary<string, SkirmishRoleOverlay> roles = new(StringComparer.Ordinal);
        public SkirmishResolvedSetup Setup { get; private set; }
        private readonly Dictionary<SkirmishRoleKind,float> productionDeliverySeconds = new();

        public static SkirmishProductionCatalog Create(SkirmishResolvedSetup setup,
            BuildingPlacementSystemConfig baseline, BuildingPlacementSystemConfig scene) => Create(setup,baseline,scene,null);

        public static SkirmishProductionCatalog Create(SkirmishResolvedSetup setup,
            BuildingPlacementSystemConfig baseline, BuildingPlacementSystemConfig scene,
            BuildingProductionQueueCompositionSystemHelper.TryGetUnitProductionMetadataDelegate timingResolver)
        {
            if (setup == null || baseline == null || scene == null || scene.UnitPrefabRegistryConfig == null)
                throw new ArgumentException("The mission production catalog requires resolved setup and authored catalogs.");
            var result = new SkirmishProductionCatalog { Setup = setup };
            try
            {
                var buildings = new List<GameObject>(baseline.Spawnables);
                if (setup.CatalogId == "S004")
                {
                    var environment = UnityEngine.Resources.Load<MapBuildingPlacementConfig>(SkirmishS004Environment.ResourceName);
                    if (environment != null)
                        foreach (var placement in environment.Placements)
                            if (placement?.BuildingPrefab != null && !buildings.Contains(placement.BuildingPrefab))
                                buildings.Add(placement.BuildingPrefab);
                }
                var availableBuildings = new Dictionary<string, GameObject>(StringComparer.Ordinal);
                foreach (var prefab in scene.Spawnables) if (prefab != null) availableBuildings[prefab.name] = prefab;
                foreach (var prefab in baseline.Spawnables) if (prefab != null) availableBuildings[prefab.name] = prefab;
                var availableUnits = new Dictionary<string, GameObject>(StringComparer.Ordinal);
                foreach (var prefab in scene.UnitPrefabRegistryConfig.UnitSpawnPrefabs)
                    if (prefab != null) availableUnits[prefab.name] = prefab;
                result.registry = UnityEngine.Object.Instantiate(scene.UnitPrefabRegistryConfig);
                result.registry.hideFlags = HideFlags.HideAndDontSave;
                result.registry.UnitSpawnPrefabs.Clear();
                var recipes = new List<BuildingProductionRecipe>();
                var profile = SkirmishArmyProfileConfig.ResolveCached(setup.ArmyProfileId);
                foreach (var overlay in setup.RoleOverlays)
                {
                    if (profile == null || !profile.Allows(overlay.RoleId)) continue;
                    string unitKey = SkirmishRoleCatalogConfig.RuntimePrefabKey(overlay.RoleKind);
                    string producerKey = ProducerPrefabKey(overlay.Producer);
                    if (!availableUnits.TryGetValue(unitKey, out var unit) ||
                        !availableBuildings.TryGetValue(producerKey, out var producer))
                        throw new InvalidOperationException("Missing authored production binding: " + unitKey + " at " + producerKey);
                    if (!buildings.Contains(producer)) buildings.Add(producer);
                    result.registry.UnitSpawnPrefabs.Add(unit);
                    result.roles.Add(unitKey, overlay);
                    // Capture the same authored metadata used by the native queue;
                    // optional callers without a resolver keep timing unavailable.
                    if (timingResolver != null && timingResolver(unit,out var timing) &&
                        float.IsFinite(timing.ProductionDurationSeconds) && timing.ProductionDurationSeconds > 0 &&
                        float.IsFinite(timing.ProductionTransportArrivalSeconds) && timing.ProductionTransportArrivalSeconds >= 0)
                    {
                        bool groundExit = overlay.Producer == SkirmishProducerKind.GroundStaging ||
                            (overlay.Producer == SkirmishProducerKind.IntelStation && overlay.RoleKind == SkirmishRoleKind.Drone);
                        float seconds = timing.ProductionDurationSeconds +
                            (groundExit || timing.ProductionTransportPrefab == null ? 0 : timing.ProductionTransportArrivalSeconds);
                        if (float.IsFinite(seconds)) result.productionDeliverySeconds[overlay.RoleKind] = seconds;
                    }
                    recipes.Add(new BuildingProductionRecipe {
                        ProducerPrefab = producer, UnitPrefab = unit, Quantity = Math.Max(1, overlay.SquadMembers),
                        MaterialsCost = overlay.MaterialsCost, CreditsCost = 0,
                        // Expanded recon belongs to its IntelStation launch pad.
                        // Keep the shared Campaign Drone arrival recipe unchanged.
                        UseProducerGroundExit = overlay.Producer == SkirmishProducerKind.GroundStaging ||
                            (overlay.Producer == SkirmishProducerKind.IntelStation &&
                             overlay.RoleKind == SkirmishRoleKind.Drone)
                    });
                }
                // Starting structures are granted through the same configured-spawnable
                // definitions as producers; a non-producer start (e.g. the Satellite Dish
                // in S002 EstablishedBase) must be bound or the grant preflight rejects it.
                if (setup.Structures != null)
                    foreach (var structure in setup.Structures)
                    {
                        string key = SkirmishStructureIds.VisualKey(structure.StructureId);
                        if (!string.IsNullOrEmpty(key) && availableBuildings.TryGetValue(key, out var structurePrefab) &&
                            !buildings.Contains(structurePrefab))
                            buildings.Add(structurePrefab);
                    }
                result.Config = BuildingPlacementSystemConfig.CreateRuntimeCatalogOverlay(
                    baseline, result.registry, buildings, recipes.ToArray());
                return result;
            }
            catch { result.Dispose(); throw; }
        }

        public bool TryRole(string prefabKey, out SkirmishRoleOverlay overlay) => roles.TryGetValue(prefabKey, out overlay);
        public bool TryReadProductionDeliverySeconds(SkirmishRoleKind role, out float seconds) =>
            productionDeliverySeconds.TryGetValue(role,out seconds);
        public bool Allows(GameObject prefab, bool building) => prefab != null && Config != null &&
            (building ? Config.Spawnables.Contains(prefab) : registry.UnitSpawnPrefabs.Contains(prefab));

        public static string ProducerPrefabKey(SkirmishProducerKind producer) => producer switch
        {
            SkirmishProducerKind.Barracks => SkirmishStructureIds.VisualKey(SkirmishStructureIds.Barracks),
            SkirmishProducerKind.GroundStaging => SkirmishStructureIds.VisualKey(SkirmishStructureIds.GroundStaging),
            SkirmishProducerKind.Helipad => SkirmishStructureIds.VisualKey(SkirmishStructureIds.Helipad),
            SkirmishProducerKind.Airport => SkirmishStructureIds.VisualKey(SkirmishStructureIds.Airport),
            SkirmishProducerKind.IntelStation => SkirmishStructureIds.VisualKey(SkirmishStructureIds.SatelliteDish),
            _ => string.Empty
        };

        public void Dispose()
        {
            if (Config != null) SkirmishVisualLifecycle.DestroyOwned(Config);
            if (registry != null) SkirmishVisualLifecycle.DestroyOwned(registry);
            Config = null; registry = null;
        }
    }
}
