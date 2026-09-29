using Game.Components;
using Unity.Entities;
using UnityEngine;

namespace Game.Authoring
{
    public sealed class MapPreparationUnitFixtureAuthoring : MonoBehaviour
    {
        public GameObject InfantryPrefab;
        public GameObject HaulerPrefab;
        public GameObject ArmorPrefab;
        [BakingVersion("warline.map-preparation-unit-fixture", 1)]
        private sealed class FixtureBaker : Baker<MapPreparationUnitFixtureAuthoring>
        {
            public override void Bake(MapPreparationUnitFixtureAuthoring authoring)
            {
                if (authoring.InfantryPrefab == null || authoring.HaulerPrefab == null || authoring.ArmorPrefab == null) return;
                AddComponent(GetEntity(TransformUsageFlags.None), new MapPreparationUnitFixtureComponent
                {
                    InfantryPrefab = GetEntity(authoring.InfantryPrefab, TransformUsageFlags.Dynamic),
                    HaulerPrefab = GetEntity(authoring.HaulerPrefab, TransformUsageFlags.Dynamic),
                    ArmorPrefab = GetEntity(authoring.ArmorPrefab, TransformUsageFlags.Dynamic)
                });
            }
        }
    }
}
