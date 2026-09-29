using Unity.Entities;

namespace Game.Components
{
    // Preparation-only prefab references. No unit spawns automatically and this is never
    // registered by a mission or production catalog.
    public struct MapPreparationUnitFixtureComponent : IComponentData
    {
        public Entity InfantryPrefab;
        public Entity HaulerPrefab;
        public Entity ArmorPrefab;
    }
}
