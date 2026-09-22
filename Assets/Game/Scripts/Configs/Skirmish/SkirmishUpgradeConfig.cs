using UnityEngine;

namespace Game.Configs
{
    [CreateAssetMenu(menuName = "Game/SkirmishExpansion/Upgrade Catalog")]
    public sealed class SkirmishUpgradeConfig : ScriptableObject
    {
        [SerializeField] private string catalogId = "skirmish.upgrades.v1";
        [SerializeField] private int startingCategoryLevel;
        [SerializeField] private int readinessResearchMaterials = 240;
        [SerializeField] private int readinessResearchSeconds = 45;
        [SerializeField] private int contentVersion = 1;

        public string CatalogId => catalogId;
        public int StartingCategoryLevel => startingCategoryLevel;
        public int ReadinessResearchMaterials => readinessResearchMaterials;
        public int ReadinessResearchSeconds => readinessResearchSeconds;
        public int ContentVersion => contentVersion;

        public void ConfigureDefault()
        {
            catalogId = "skirmish.upgrades.v1";
            startingCategoryLevel = 0;
            readinessResearchMaterials = 240;
            readinessResearchSeconds = 45;
            contentVersion = 1;
        }
    }
}
