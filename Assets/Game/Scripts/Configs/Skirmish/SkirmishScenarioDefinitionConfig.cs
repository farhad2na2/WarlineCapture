using System;
using Game.Skirmish.Contracts;
using UnityEngine;

namespace Game.Configs
{
    [CreateAssetMenu(menuName = "Game/SkirmishExpansion/Scenario Definition")]
    public sealed class SkirmishScenarioDefinitionConfig : ScriptableObject
    {
        [SerializeField] private string catalogId = "S002";
        [SerializeField] private string definitionId = "skirmish.s002";
        [SerializeField] private string scenarioSetupId = "scenario.skirmish.s002";
        [SerializeField] private string operationMapId = "opmap.skirmish.desert_base_01";
        [SerializeField] private string mapLayoutId = "layout.skirmish.db.ba";
        [SerializeField] private int contentVersion = 1;
        [SerializeField] private string contentHash = "skirmish.s002.v1";
        [SerializeField] private SkirmishObjectiveConfig objectiveConfig;
        [SerializeField] private SkirmishArmyProfileConfig armyProfileConfig;
        [SerializeField] private SkirmishStartPackageConfig startPackageConfig;
        [SerializeField] private SkirmishDifficultyId defaultDifficulty = SkirmishDifficultyId.Regular;
        [SerializeField] private SkirmishSizeId firstVisitSize = SkirmishSizeId.Standard;
        [SerializeField] private SkirmishSizeId recommendedSize = SkirmishSizeId.Standard;
        [SerializeField] private SkirmishEconomyConfig economyConfig;
        [SerializeField] private SkirmishUpgradeConfig upgradeCatalog;
        [SerializeField] private SkirmishIntelConfig intelConfig;
        [SerializeField] private SkirmishRoleCatalogConfig roleCatalog;
        [SerializeField] private SkirmishMapLayoutConfig mapLayout;
        [SerializeField] private byte playerFaction = 1;
        [SerializeField] private byte enemyFaction = 2;
        [SerializeField] private string sideRule = "symmetric.ba";
        [SerializeField] private SkirmishSizeId[] allowedCertifiedSizeIds = { SkirmishSizeId.Standard };
        [SerializeField] private string[] requiredFeatureIds =
        {
            "ground", "intel", "transport", "advanced_ground", "objective_ba"
        };
        [SerializeField] private string[] ariaCapabilityIds = Array.Empty<string>();
        [SerializeField] private string briefingKey = "skirmish.s002.brief";
        [SerializeField] private string titleKey = "skirmish.s002.title";
        [SerializeField] private string objectiveKey = "skirmish.s002.objective";
        [SerializeField] private string resultVictoryKey = "skirmish.s002.result.victory";
        [SerializeField] private string resultDefeatKey = "skirmish.s002.result.defeat";
        [SerializeField] private string readinessManifestId = "publication.skirmish.s002";

        public string CatalogId => catalogId;
        public string DefinitionId => definitionId;
        public string ScenarioSetupId => scenarioSetupId;
        public string OperationMapId => operationMapId;
        public string MapLayoutId => mapLayoutId;
        public int ContentVersion => contentVersion;
        public string ContentHash => contentHash;
        public SkirmishObjectiveConfig ObjectiveConfig => objectiveConfig;
        public SkirmishArmyProfileConfig ArmyProfileConfig => armyProfileConfig;
        public SkirmishStartPackageConfig StartPackageConfig => startPackageConfig;
        public SkirmishDifficultyId DefaultDifficulty => defaultDifficulty;
        public SkirmishSizeId FirstVisitSize => firstVisitSize;
        public SkirmishSizeId RecommendedSize => recommendedSize;
        public SkirmishEconomyConfig EconomyConfig => economyConfig;
        public SkirmishUpgradeConfig UpgradeCatalog => upgradeCatalog;
        public SkirmishIntelConfig IntelConfig => intelConfig;
        public SkirmishRoleCatalogConfig RoleCatalog => roleCatalog;
        public SkirmishMapLayoutConfig MapLayout => mapLayout;
        public byte PlayerFaction => playerFaction;
        public byte EnemyFaction => enemyFaction;
        public string SideRule => sideRule;
        public SkirmishSizeId[] AllowedCertifiedSizeIds => allowedCertifiedSizeIds;
        public string[] RequiredFeatureIds => requiredFeatureIds;
        public string[] AriaCapabilityIds => ariaCapabilityIds;
        public string BriefingKey => briefingKey;
        public string TitleKey => titleKey;
        public string ObjectiveKey => objectiveKey;
        public string ResultVictoryKey => resultVictoryKey;
        public string ResultDefeatKey => resultDefeatKey;
        public string ReadinessManifestId => readinessManifestId;

        public void Configure(
            string configuredCatalogId,
            SkirmishObjectiveConfig objective,
            SkirmishArmyProfileConfig army,
            SkirmishStartPackageConfig start,
            SkirmishEconomyConfig economy,
            SkirmishUpgradeConfig upgrades,
            SkirmishIntelConfig intel,
            SkirmishRoleCatalogConfig roles,
            SkirmishMapLayoutConfig layout)
        {
            catalogId = configuredCatalogId;
            definitionId = SkirmishDefinitionId.FromCatalog(new SkirmishCatalogId(configuredCatalogId)).Value;
            scenarioSetupId = "scenario.skirmish.s" + configuredCatalogId.Substring(1);
            operationMapId = layout != null ? layout.OperationMapId : "opmap.skirmish.desert_base_01";
            mapLayoutId = layout != null ? layout.LayoutId : "layout.skirmish.db.ba";
            contentVersion = 1;
            contentHash = definitionId + ".v1";
            objectiveConfig = objective;
            armyProfileConfig = army;
            startPackageConfig = start;
            defaultDifficulty = SkirmishDifficultyId.Regular;
            firstVisitSize = SkirmishSizeId.Standard;
            recommendedSize = SkirmishSizeId.Standard;
            economyConfig = economy;
            upgradeCatalog = upgrades;
            intelConfig = intel;
            roleCatalog = roles;
            mapLayout = layout;
            playerFaction = 1;
            enemyFaction = 2;
            sideRule = "symmetric.ba";
            allowedCertifiedSizeIds = new[] { SkirmishSizeId.Standard };
            requiredFeatureIds = new[] { "ground", "intel", "transport", "advanced_ground", "objective_ba" };
            ariaCapabilityIds = Array.Empty<string>();
            string prefix = "skirmish.s" + configuredCatalogId.Substring(1);
            briefingKey = prefix + ".brief";
            titleKey = prefix + ".title";
            objectiveKey = prefix + ".objective";
            resultVictoryKey = prefix + ".result.victory";
            resultDefeatKey = prefix + ".result.defeat";
            readinessManifestId = "publication." + definitionId;
        }
    }
}
