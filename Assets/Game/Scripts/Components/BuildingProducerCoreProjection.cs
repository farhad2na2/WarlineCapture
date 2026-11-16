using Unity.Mathematics;

namespace Game.Components
{
    /// <summary>
    /// Data-only view of the factory-owned, static producer facts used by both
    /// the native definition factory and the headless source host. Mission recipe
    /// overlays and unit production timing remain owned by their existing systems.
    /// </summary>
    public readonly struct BuildingProducerCoreProjection
    {
        public readonly string PrefabKey;
        public readonly int2 FootprintCells;
        public readonly int CreditsCost;
        public readonly int MaterialsCost;
        public readonly int MaxHealth;
        public readonly float ConstructionDurationSeconds;

        private BuildingProducerCoreProjection(
            string prefabKey,
            int2 footprintCells,
            int creditsCost,
            int materialsCost,
            int maxHealth,
            float constructionDurationSeconds)
        {
            PrefabKey = prefabKey;
            FootprintCells = footprintCells;
            CreditsCost = creditsCost;
            MaterialsCost = materialsCost;
            MaxHealth = maxHealth;
            ConstructionDurationSeconds = constructionDurationSeconds;
        }

        /// <summary>Copies factory values without adding policy or validation.</summary>
        public static BuildingProducerCoreProjection FromFactoryValues(
            string prefabKey,
            int2 footprintCells,
            int creditsCost,
            int materialsCost,
            int maxHealth,
            float constructionDurationSeconds)
        {
            return new BuildingProducerCoreProjection(prefabKey, footprintCells, creditsCost, materialsCost,
                maxHealth, constructionDurationSeconds);
        }
    }
}
