using UnityEngine;

namespace Game.Configs
{
    // Native-only serialized opt-in. Source-host manifests exclude this Unity-object adapter.
    public sealed partial class SkirmishExpansionCatalogConfig
    {
        [SerializeField] private OneProducerAuthoredReferencePackageConfig productionInputPackage;
        public OneProducerAuthoredReferencePackageConfig ProductionInputPackage => productionInputPackage;
    }
}
