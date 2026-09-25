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

        public static SkirmishProductionCatalog Create(SkirmishResolvedSetup setup,
            BuildingPlacementSystemConfig baseline, BuildingPlacementSystemConfig scene)
        {
            if (setup == null || baseline == null || scene == null || scene.UnitPrefabRegistryConfig == null)
                throw new ArgumentException("The mission production catalog requires resolved setup and authored catalogs.");
            var result = new SkirmishProductionCatalog { Setup = setup };
            try
            {
                var buildings = new List<GameObject>(baseline.Spawnables);
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
                    recipes.Add(new BuildingProductionRecipe {
                        ProducerPrefab = producer, UnitPrefab = unit, Quantity = Math.Max(1, overlay.SquadMembers),
                        MaterialsCost = overlay.MaterialsCost, CreditsCost = 0,
                        UseProducerGroundExit = overlay.Producer == SkirmishProducerKind.GroundStaging
                    });
                }
                result.Config = BuildingPlacementSystemConfig.CreateRuntimeCatalogOverlay(
                    baseline, result.registry, buildings, recipes.ToArray());
                return result;
            }
            catch { result.Dispose(); throw; }
        }

        public bool TryRole(string prefabKey, out SkirmishRoleOverlay overlay) => roles.TryGetValue(prefabKey, out overlay);
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
