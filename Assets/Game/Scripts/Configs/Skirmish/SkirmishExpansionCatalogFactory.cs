using Game.Skirmish.Contracts;

namespace Game.Configs
{
    public sealed class SkirmishExpansionAuthoredSet
    {
        public SkirmishObjectiveConfig ObjectiveBa;
        public SkirmishArmyProfileConfig ArmyGround;
        public SkirmishStartPackageConfig StartField;
        public SkirmishStartPackageConfig StartEstablished;
        public SkirmishSizeConfig SizeStandard;
        public SkirmishSizeConfig SizeWar;
        public SkirmishSizeConfig SizeLargeWar;
        public SkirmishDifficultyConfig DifficultyRecruit;
        public SkirmishDifficultyConfig DifficultyRegular;
        public SkirmishDifficultyConfig DifficultyVeteran;
        public SkirmishDifficultyConfig DifficultyCommander;
        public SkirmishEconomyConfig Economy;
        public SkirmishUpgradeConfig Upgrades;
        public SkirmishIntelConfig Intel;
        public SkirmishRoleCatalogConfig Roles;
        public SkirmishRoleOverlayCatalog Overlays;
        public SkirmishGroundStagingConfig GroundStaging;
        public SkirmishMapLayoutConfig LayoutDbBa;
        public SkirmishPublicationConfig Publication;
        public SkirmishScenarioDefinitionConfig DefinitionS002;
    }

    public static class SkirmishExpansionCatalogFactory
    {
        public static SkirmishExpansionAuthoredSet CreateInMemory()
        {
            var set = new SkirmishExpansionAuthoredSet
            {
                ObjectiveBa = Create<SkirmishObjectiveConfig>(),
                ArmyGround = Create<SkirmishArmyProfileConfig>(),
                StartField = Create<SkirmishStartPackageConfig>(),
                StartEstablished = Create<SkirmishStartPackageConfig>(),
                SizeStandard = Create<SkirmishSizeConfig>(),
                SizeWar = Create<SkirmishSizeConfig>(),
                SizeLargeWar = Create<SkirmishSizeConfig>(),
                DifficultyRecruit = Create<SkirmishDifficultyConfig>(),
                DifficultyRegular = Create<SkirmishDifficultyConfig>(),
                DifficultyVeteran = Create<SkirmishDifficultyConfig>(),
                DifficultyCommander = Create<SkirmishDifficultyConfig>(),
                Economy = Create<SkirmishEconomyConfig>(),
                Upgrades = Create<SkirmishUpgradeConfig>(),
                Intel = Create<SkirmishIntelConfig>(),
                Roles = Create<SkirmishRoleCatalogConfig>(),
                Overlays = Create<SkirmishRoleOverlayCatalog>(),
                GroundStaging = Create<SkirmishGroundStagingConfig>(),
                LayoutDbBa = Create<SkirmishMapLayoutConfig>(),
                Publication = Create<SkirmishPublicationConfig>(),
                DefinitionS002 = Create<SkirmishScenarioDefinitionConfig>()
            };
            set.ObjectiveBa.ConfigureBaseAssault();
            set.ArmyGround.ConfigureGroundManeuver();
            set.StartField.ConfigureField();
            set.StartEstablished.ConfigureEstablished();
            set.SizeStandard.Configure(SkirmishSizeId.Standard);
            set.SizeWar.Configure(SkirmishSizeId.War);
            set.SizeLargeWar.Configure(SkirmishSizeId.LargeWar);
            set.DifficultyRecruit.Configure(SkirmishDifficultyId.Recruit);
            set.DifficultyRegular.Configure(SkirmishDifficultyId.Regular);
            set.DifficultyVeteran.Configure(SkirmishDifficultyId.Veteran);
            set.DifficultyCommander.Configure(SkirmishDifficultyId.Commander);
            set.Economy.ConfigureDefault();
            set.Upgrades.ConfigureDefault();
            set.Intel.ConfigureSharedFog();
            set.Roles.ConfigureCanonicalGroundSlice();
            set.Overlays.ConfigureS002GroundSlice();
            set.GroundStaging.ConfigureEstablished();
            set.LayoutDbBa.ConfigureDesertBaseAssault();
            set.Publication.ConfigureGroundSlice();
            set.DefinitionS002.Configure(
                "S002",
                set.ObjectiveBa,
                set.ArmyGround,
                set.StartEstablished,
                set.Economy,
                set.Upgrades,
                set.Intel,
                set.Roles,
                set.LayoutDbBa);
            return set;
        }

        public static bool TryGetDefinition(
            SkirmishExpansionAuthoredSet set,
            string catalogId,
            out SkirmishScenarioDefinitionConfig definition)
        {
            if (set != null && set.DefinitionS002 != null && set.DefinitionS002.CatalogId == catalogId)
            {
                definition = set.DefinitionS002;
                return true;
            }

            definition = null;
            return false;
        }

        public static SkirmishSizeConfig Size(SkirmishExpansionAuthoredSet set, SkirmishSizeId size)
        {
            switch (size)
            {
                case SkirmishSizeId.War: return set.SizeWar;
                case SkirmishSizeId.LargeWar: return set.SizeLargeWar;
                default: return set.SizeStandard;
            }
        }

        public static SkirmishDifficultyConfig Difficulty(SkirmishExpansionAuthoredSet set, SkirmishDifficultyId difficulty)
        {
            switch (difficulty)
            {
                case SkirmishDifficultyId.Recruit: return set.DifficultyRecruit;
                case SkirmishDifficultyId.Veteran: return set.DifficultyVeteran;
                case SkirmishDifficultyId.Commander: return set.DifficultyCommander;
                default: return set.DifficultyRegular;
            }
        }

        private static T Create<T>() where T : UnityEngine.ScriptableObject =>
            UnityEngine.ScriptableObject.CreateInstance<T>();
    }
}
