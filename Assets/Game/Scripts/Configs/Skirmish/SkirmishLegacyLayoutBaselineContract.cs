// GENERATED from actual pinned pre-integration capture. No native readiness claim.
namespace Game.Configs
{
    public static class SkirmishLegacyLayoutBaselineContract
    {
        public const string ArtifactSha256 = "0d03f125c9fc77a13a74f82e1109e7bc5950bf3837325081b7a04cc32e558837";
        public const string ArtifactRepositoryPath = "Design/AgentReports/SkirmishExpansion/ProductionFoundation/Validation/legacy-layout-and-setup-baselines-20261014.json";
        public const string CapturedUnityVersion = "6000.5.2f1";
        public static bool TryGetReviewedLayoutFingerprint(string catalog, out string fingerprint)
        {
            switch (catalog)
            {
                case "S002": fingerprint = "ca1b6e178c54b8cfdf476618887b6e3abaa7b42562d58cd96b7476357cd305d6"; return true;
                case "S003": fingerprint = "ca1b6e178c54b8cfdf476618887b6e3abaa7b42562d58cd96b7476357cd305d6"; return true;
                case "S004": fingerprint = "e37af2f2f7b827017b8e30af40b2507d0863ef335e154e1eb67c89157a6eea00"; return true;
                case "S005": fingerprint = "7cc111d99da6df58f350fafe56c8f811a5a9a2b947cde86e1d4c22b47773ff5a"; return true;
                default: fingerprint = null; return false;
            }
        }
        public struct BaselineRow
        {
            public string CatalogId, Size, LayoutSha256;
            public int Seed;
            public uint SetupHash, PolicylessHash;
        }
        public static BaselineRow[] ReadRows() => new[]
        {
            new BaselineRow { CatalogId = "S002", Size = "Standard", LayoutSha256 = "ca1b6e178c54b8cfdf476618887b6e3abaa7b42562d58cd96b7476357cd305d6", Seed = 155926, SetupHash = 0x8E506E74u, PolicylessHash = 0x8E506E74u },
            new BaselineRow { CatalogId = "S002", Size = "War", LayoutSha256 = "ca1b6e178c54b8cfdf476618887b6e3abaa7b42562d58cd96b7476357cd305d6", Seed = 155926, SetupHash = 0x4A3BD3C3u, PolicylessHash = 0x4A3BD3C3u },
            new BaselineRow { CatalogId = "S002", Size = "LargeWar", LayoutSha256 = "ca1b6e178c54b8cfdf476618887b6e3abaa7b42562d58cd96b7476357cd305d6", Seed = 155926, SetupHash = 0xD82D7F8Cu, PolicylessHash = 0xD82D7F8Cu },
            new BaselineRow { CatalogId = "S003", Size = "Standard", LayoutSha256 = "ca1b6e178c54b8cfdf476618887b6e3abaa7b42562d58cd96b7476357cd305d6", Seed = 155926, SetupHash = 0x81B55A11u, PolicylessHash = 0x81B55A11u },
            new BaselineRow { CatalogId = "S003", Size = "War", LayoutSha256 = "ca1b6e178c54b8cfdf476618887b6e3abaa7b42562d58cd96b7476357cd305d6", Seed = 155926, SetupHash = 0xEBD46626u, PolicylessHash = 0xEBD46626u },
            new BaselineRow { CatalogId = "S003", Size = "LargeWar", LayoutSha256 = "ca1b6e178c54b8cfdf476618887b6e3abaa7b42562d58cd96b7476357cd305d6", Seed = 155926, SetupHash = 0x48B47168u, PolicylessHash = 0x48B47168u },
            new BaselineRow { CatalogId = "S004", Size = "Standard", LayoutSha256 = "e37af2f2f7b827017b8e30af40b2507d0863ef335e154e1eb67c89157a6eea00", Seed = 155926, SetupHash = 0x4197E539u, PolicylessHash = 0x4197E539u },
            new BaselineRow { CatalogId = "S004", Size = "War", LayoutSha256 = "e37af2f2f7b827017b8e30af40b2507d0863ef335e154e1eb67c89157a6eea00", Seed = 155926, SetupHash = 0xC367BF7Du, PolicylessHash = 0xC367BF7Du },
            new BaselineRow { CatalogId = "S004", Size = "LargeWar", LayoutSha256 = "e37af2f2f7b827017b8e30af40b2507d0863ef335e154e1eb67c89157a6eea00", Seed = 155926, SetupHash = 0xDFE40279u, PolicylessHash = 0xDFE40279u },
            new BaselineRow { CatalogId = "S005", Size = "Standard", LayoutSha256 = "7cc111d99da6df58f350fafe56c8f811a5a9a2b947cde86e1d4c22b47773ff5a", Seed = 155926, SetupHash = 0xE3D3D033u, PolicylessHash = 0xCEE5C4FDu },
            new BaselineRow { CatalogId = "S005", Size = "War", LayoutSha256 = "7cc111d99da6df58f350fafe56c8f811a5a9a2b947cde86e1d4c22b47773ff5a", Seed = 155926, SetupHash = 0xF16543C6u, PolicylessHash = 0xA9BDE012u },
            new BaselineRow { CatalogId = "S005", Size = "LargeWar", LayoutSha256 = "7cc111d99da6df58f350fafe56c8f811a5a9a2b947cde86e1d4c22b47773ff5a", Seed = 155926, SetupHash = 0x8F1D10D8u, PolicylessHash = 0xC2000664u },
        };
    }
}
