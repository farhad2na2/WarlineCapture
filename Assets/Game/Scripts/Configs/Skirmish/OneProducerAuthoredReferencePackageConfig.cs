using System;
using Game.Simulation;
using UnityEngine;

namespace Game.Configs
{
    [CreateAssetMenu(menuName = "Game/SkirmishExpansion/One Producer Input Package")]
    public sealed class OneProducerAuthoredReferencePackageConfig : ScriptableObject
    {
        public const string ResourceName = "OneProducerAuthoredReferencePackage";

        [SerializeField] private TextAsset packageJson;
        [SerializeField] private GameObject producerPrefabReference;
        [SerializeField] private GameObject unitPrefabReference;
        [SerializeField] private GameObject transportPrefabReference;
        [SerializeField] private BuildingDefinitionAuthoringConfig producerConfigReference;
        [SerializeField] private UnitGridAuthoringConfig unitConfigReference;
        [SerializeField] private UnitGridAuthoringConfig transportConfigReference;

        public TextAsset PackageJson => packageJson;

        public OneProducerAuthoredReferencePackage ReadAndValidatePackage()
        {
            if (packageJson == null || string.IsNullOrWhiteSpace(packageJson.text))
                throw new InvalidOperationException("Explicit authored-reference package data required");
            var package = JsonUtility.FromJson<OneProducerAuthoredReferencePackage>(packageJson.text);
            OneProducerAuthoredReferenceContract.RequireValid(package);
            return package;
        }

        public bool IsBoundToNativeReferences(OneProducerAuthoredReferencePackage package,
            GameObject producer, GameObject unit, GameObject transport,
            BuildingDefinitionAuthoringConfig producerConfig, UnitGridAuthoringConfig unitConfig,
            UnitGridAuthoringConfig transportConfig, int recipeSlot, int quantity)
        {
            OneProducerAuthoredReferenceContract.RequireValid(package);
            return producer != null && unit != null && transport != null &&
                producerPrefabReference != null && unitPrefabReference != null && transportPrefabReference != null &&
                producerConfigReference != null && unitConfigReference != null && transportConfigReference != null &&
                ReferenceEquals(producerPrefabReference, producer) && ReferenceEquals(unitPrefabReference, unit) &&
                ReferenceEquals(transportPrefabReference, transport) &&
                ReferenceEquals(producerConfigReference, producerConfig) &&
                ReferenceEquals(unitConfigReference, unitConfig) &&
                ReferenceEquals(transportConfigReference, transportConfig) &&
                producer.name == package.producer.key && unit.name == package.unit.key &&
                transport.name == package.transport.key && recipeSlot == package.recipeSlot && quantity == package.quantity;
        }
    }
}
