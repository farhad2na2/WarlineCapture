using UnityEngine;

namespace Game.Components
{
    /// <summary>
    /// Serialized inputs needed for the narrow BuildingDefinitionAuthoring
    /// scalar metadata projection. This carries no Unity object references.
    /// </summary>
    public readonly struct BuildingDefinitionAuthoringScalarSource
    {
        public readonly bool HasConfig;
        public readonly int PrefabMaxHealth;
        public readonly int FootprintWidth;
        public readonly int FootprintDepth;
        public readonly int PrefabPrice;
        public readonly int PrefabMaterialsCost;
        public readonly float PrefabProductionDurationSeconds;
        public readonly int ConfigMaxHealth;
        public readonly int ConfigRawPrice;
        public readonly int ConfigRawMaterialsCost;
        public readonly float ConfigRawProductionDurationSeconds;

        public BuildingDefinitionAuthoringScalarSource(
            bool hasConfig,
            int prefabMaxHealth,
            int footprintWidth,
            int footprintDepth,
            int prefabPrice,
            int prefabMaterialsCost,
            float prefabProductionDurationSeconds,
            int configMaxHealth,
            int configRawPrice,
            int configRawMaterialsCost,
            float configRawProductionDurationSeconds)
        {
            HasConfig = hasConfig;
            PrefabMaxHealth = prefabMaxHealth;
            FootprintWidth = footprintWidth;
            FootprintDepth = footprintDepth;
            PrefabPrice = prefabPrice;
            PrefabMaterialsCost = prefabMaterialsCost;
            PrefabProductionDurationSeconds = prefabProductionDurationSeconds;
            ConfigMaxHealth = configMaxHealth;
            ConfigRawPrice = configRawPrice;
            ConfigRawMaterialsCost = configRawMaterialsCost;
            ConfigRawProductionDurationSeconds = configRawProductionDurationSeconds;
        }
    }

    /// <summary>Effective scalar values returned by the original metadata owner.</summary>
    public readonly struct BuildingDefinitionAuthoringScalarMetadata
    {
        public readonly int MaxHealth;
        public readonly int FootprintWidth;
        public readonly int FootprintDepth;
        public readonly int Price;
        public readonly int MaterialsCost;
        public readonly float ProductionDurationSeconds;

        internal BuildingDefinitionAuthoringScalarMetadata(
            int maxHealth,
            int footprintWidth,
            int footprintDepth,
            int price,
            int materialsCost,
            float productionDurationSeconds)
        {
            MaxHealth = maxHealth;
            FootprintWidth = footprintWidth;
            FootprintDepth = footprintDepth;
            Price = price;
            MaterialsCost = materialsCost;
            ProductionDurationSeconds = productionDurationSeconds;
        }
    }

    /// <summary>
    /// Shared policy for the original BuildingDefinitionAuthoring metadata owner
    /// and source-only projections. Mathf is used directly so float edge behavior
    /// matches the Unity getters, including NaN and infinities.
    /// </summary>
    public static class BuildingDefinitionAuthoringScalarProjection
    {
        public static BuildingDefinitionAuthoringScalarMetadata Project(
            BuildingDefinitionAuthoringScalarSource source)
        {
            int maxHealth = source.HasConfig ? source.ConfigMaxHealth : source.PrefabMaxHealth;
            int price = source.HasConfig ? Mathf.Max(0, source.ConfigRawPrice) : source.PrefabPrice;
            int materialsCost = source.HasConfig
                ? Mathf.Max(0, source.ConfigRawMaterialsCost)
                : source.PrefabMaterialsCost;
            float productionDuration = source.HasConfig
                ? Mathf.Max(0.01f, source.ConfigRawProductionDurationSeconds)
                : source.PrefabProductionDurationSeconds;

            // Config getters clamp once while ApplyConfigIfAvailable copies their
            // values. The authoring getters then apply the same clamps again.
            price = Mathf.Max(0, price);
            materialsCost = Mathf.Max(0, materialsCost);
            productionDuration = Mathf.Max(0.01f, productionDuration);

            return new BuildingDefinitionAuthoringScalarMetadata(
                maxHealth, source.FootprintWidth, source.FootprintDepth,
                price, materialsCost, productionDuration);
        }
    }
}
