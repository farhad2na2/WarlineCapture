using System;

namespace Game.Skirmish.Contracts
{
    public enum SkirmishSizeId : byte
    {
        None = 0,
        Standard = 1,
        War = 2,
        LargeWar = 3
    }

    public enum SkirmishDifficultyId : byte
    {
        None = 0,
        Recruit = 1,
        Regular = 2,
        Veteran = 3,
        Commander = 4
    }

    public enum SkirmishArmyProfileId : byte
    {
        None = 0,
        GroundManeuver = 1,
        AirMobile = 2,
        CombinedArms = 3
    }

    public enum SkirmishStartPackageId : byte
    {
        None = 0,
        FieldBase = 1,
        EstablishedBase = 2
    }

    public enum SkirmishSessionPhase : byte
    {
        None = 0,
        Queued = 1,
        Compiling = 2,
        Initializing = 3,
        Spawning = 4,
        Playing = 5,
        Finished = 6,
        Failed = 7,
        Cleaning = 8
    }

    public enum SkirmishPublicationStatus : byte
    {
        Planned = 0,
        InProgress = 1,
        Playable = 2,
        Certified = 3
    }

    public readonly struct SkirmishCatalogId : IEquatable<SkirmishCatalogId>
    {
        public readonly string Value;

        public SkirmishCatalogId(string value)
        {
            Value = value ?? string.Empty;
        }

        public bool IsValid
        {
            get
            {
                if (Value.Length != 4 || Value[0] != 'S')
                    return false;
                for (int i = 1; i < 4; i++)
                {
                    if (Value[i] < '0' || Value[i] > '9')
                        return false;
                }

                int number = ((Value[1] - '0') * 100) + ((Value[2] - '0') * 10) + (Value[3] - '0');
                return number >= 1 && number <= 120;
            }
        }

        public bool Equals(SkirmishCatalogId other) =>
            string.Equals(Value, other.Value, StringComparison.Ordinal);

        public override bool Equals(object obj) => obj is SkirmishCatalogId other && Equals(other);

        public override int GetHashCode() => Value != null ? StringComparer.Ordinal.GetHashCode(Value) : 0;

        public override string ToString() => Value ?? string.Empty;
    }

    public readonly struct SkirmishDefinitionId : IEquatable<SkirmishDefinitionId>
    {
        public readonly string Value;

        public SkirmishDefinitionId(string value)
        {
            Value = value ?? string.Empty;
        }

        public bool IsValid =>
            Value.Length == 13 &&
            Value.StartsWith("skirmish.s", StringComparison.Ordinal) &&
            new SkirmishCatalogId("S" + Value.Substring(10, 3)).IsValid;

        public bool Equals(SkirmishDefinitionId other) =>
            string.Equals(Value, other.Value, StringComparison.Ordinal);

        public override bool Equals(object obj) => obj is SkirmishDefinitionId other && Equals(other);

        public override int GetHashCode() => Value != null ? StringComparer.Ordinal.GetHashCode(Value) : 0;

        public override string ToString() => Value ?? string.Empty;

        public static SkirmishDefinitionId FromCatalog(SkirmishCatalogId catalog)
        {
            if (!catalog.IsValid)
                return new SkirmishDefinitionId(string.Empty);
            return new SkirmishDefinitionId("skirmish.s" + catalog.Value.Substring(1));
        }
    }

    public readonly struct SkirmishScenarioSetupId : IEquatable<SkirmishScenarioSetupId>
    {
        public readonly string Value;

        public SkirmishScenarioSetupId(string value)
        {
            Value = value ?? string.Empty;
        }

        public bool IsValid =>
            Value.Length == 22 &&
            Value.StartsWith("scenario.skirmish.s", StringComparison.Ordinal) &&
            new SkirmishCatalogId("S" + Value.Substring(19, 3)).IsValid;

        public bool Equals(SkirmishScenarioSetupId other) =>
            string.Equals(Value, other.Value, StringComparison.Ordinal);

        public override bool Equals(object obj) => obj is SkirmishScenarioSetupId other && Equals(other);

        public override int GetHashCode() => Value != null ? StringComparer.Ordinal.GetHashCode(Value) : 0;

        public override string ToString() => Value ?? string.Empty;
    }

    public sealed class SkirmishLaunchPayload
    {
        public string SessionId;
        public string CatalogId;
        public string DefinitionId;
        public int ContentVersion;
        public string AllConfigHashes;
        public string MapId;
        public string LayoutId;
        public SkirmishDifficultyId DifficultyId;
        public SkirmishSizeId SizeId;
        public int Seed;
        public bool IsCustom;
        public bool IsLegacy;
        public string TransitionId;
        public string ReturnSelection;

        public static SkirmishLaunchPayload CreateLegacy(int scenarioIndex, int seed)
        {
            return new SkirmishLaunchPayload
            {
                SessionId = string.Empty,
                CatalogId = scenarioIndex == 1 ? "S025" : scenarioIndex == 3 ? "S073" : "S001",
                DefinitionId = string.Empty,
                ContentVersion = 0,
                AllConfigHashes = string.Empty,
                MapId = string.Empty,
                LayoutId = string.Empty,
                DifficultyId = SkirmishDifficultyId.None,
                SizeId = SkirmishSizeId.None,
                Seed = seed,
                IsCustom = false,
                IsLegacy = true,
                TransitionId = string.Empty,
                ReturnSelection = string.Empty
            };
        }
    }

    public sealed class SkirmishContentManifest
    {
        public int SchemaVersion = 1;
        public string[] RequiredFeatureIds = Array.Empty<string>();
        public string[] CertifiedSizeIds = Array.Empty<string>();
        public string[] AvailableDefinitionIds = Array.Empty<string>();
    }

    public readonly struct SkirmishCompileReason
    {
        public readonly SkirmishReasonCode Code;
        public readonly string Field;
        public readonly string Detail;

        public SkirmishCompileReason(SkirmishReasonCode code, string field, string detail)
        {
            Code = code;
            Field = field ?? string.Empty;
            Detail = detail ?? string.Empty;
        }

        public override string ToString() => $"{Code}:{Field}:{Detail}";
    }

    public static class SkirmishLegacyPrototypeMap
    {
        public const int DesertBaseScenarioIndex = 0;
        public const int CityCrossroadsScenarioIndex = 1;
        public const int EditorStressScenarioIndex = 2;
        public const int IndustrialBasinScenarioIndex = 3;
        public const string DesertBaseCatalogId = "S001";
        public const string CityCrossroadsCatalogId = "S025";
        public const string IndustrialBasinCatalogId = "S073";
        public const string DesertBaseOperationMapId = "opmap.skirmish.desert_base_01";
        public const int BaselineCommitHintLength = 9;

        public static bool TryGetLegacyCatalogId(int scenarioIndex, out string catalogId)
        {
            switch (scenarioIndex)
            {
                case DesertBaseScenarioIndex:
                    catalogId = DesertBaseCatalogId;
                    return true;
                case CityCrossroadsScenarioIndex:
                    catalogId = CityCrossroadsCatalogId;
                    return true;
                case IndustrialBasinScenarioIndex:
                    catalogId = IndustrialBasinCatalogId;
                    return true;
                default:
                    catalogId = string.Empty;
                    return false;
            }
        }

        public static bool IsEditorStressIndex(int scenarioIndex) =>
            scenarioIndex == EditorStressScenarioIndex;
    }

    public static class SkirmishIdParsing
    {
        public static bool TryParseSize(string value, out SkirmishSizeId size)
        {
            if (string.Equals(value, "Standard", StringComparison.Ordinal))
            {
                size = SkirmishSizeId.Standard;
                return true;
            }

            if (string.Equals(value, "War", StringComparison.Ordinal))
            {
                size = SkirmishSizeId.War;
                return true;
            }

            if (string.Equals(value, "LargeWar", StringComparison.Ordinal))
            {
                size = SkirmishSizeId.LargeWar;
                return true;
            }

            size = SkirmishSizeId.None;
            return false;
        }

        public static bool TryParseDifficulty(string value, out SkirmishDifficultyId difficulty)
        {
            if (string.Equals(value, "Recruit", StringComparison.Ordinal))
            {
                difficulty = SkirmishDifficultyId.Recruit;
                return true;
            }

            if (string.Equals(value, "Regular", StringComparison.Ordinal))
            {
                difficulty = SkirmishDifficultyId.Regular;
                return true;
            }

            if (string.Equals(value, "Veteran", StringComparison.Ordinal))
            {
                difficulty = SkirmishDifficultyId.Veteran;
                return true;
            }

            if (string.Equals(value, "Commander", StringComparison.Ordinal))
            {
                difficulty = SkirmishDifficultyId.Commander;
                return true;
            }

            difficulty = SkirmishDifficultyId.None;
            return false;
        }

        public static bool TryParseArmy(string value, out SkirmishArmyProfileId army)
        {
            if (string.Equals(value, "G", StringComparison.Ordinal))
            {
                army = SkirmishArmyProfileId.GroundManeuver;
                return true;
            }

            if (string.Equals(value, "A", StringComparison.Ordinal))
            {
                army = SkirmishArmyProfileId.AirMobile;
                return true;
            }

            if (string.Equals(value, "C", StringComparison.Ordinal))
            {
                army = SkirmishArmyProfileId.CombinedArms;
                return true;
            }

            army = SkirmishArmyProfileId.None;
            return false;
        }

        public static bool TryParseStart(string value, out SkirmishStartPackageId start)
        {
            if (string.Equals(value, "F", StringComparison.Ordinal))
            {
                start = SkirmishStartPackageId.FieldBase;
                return true;
            }

            if (string.Equals(value, "E", StringComparison.Ordinal))
            {
                start = SkirmishStartPackageId.EstablishedBase;
                return true;
            }

            start = SkirmishStartPackageId.None;
            return false;
        }

        public static string ToCode(SkirmishSizeId size)
        {
            switch (size)
            {
                case SkirmishSizeId.Standard: return "Standard";
                case SkirmishSizeId.War: return "War";
                case SkirmishSizeId.LargeWar: return "LargeWar";
                default: return string.Empty;
            }
        }

        public static string ToCode(SkirmishArmyProfileId army)
        {
            switch (army)
            {
                case SkirmishArmyProfileId.GroundManeuver: return "G";
                case SkirmishArmyProfileId.AirMobile: return "A";
                case SkirmishArmyProfileId.CombinedArms: return "C";
                default: return string.Empty;
            }
        }

        public static string ToCode(SkirmishStartPackageId start)
        {
            switch (start)
            {
                case SkirmishStartPackageId.FieldBase: return "F";
                case SkirmishStartPackageId.EstablishedBase: return "E";
                default: return string.Empty;
            }
        }

        public static string ToCode(SkirmishDifficultyId difficulty)
        {
            switch (difficulty)
            {
                case SkirmishDifficultyId.Recruit: return "Recruit";
                case SkirmishDifficultyId.Regular: return "Regular";
                case SkirmishDifficultyId.Veteran: return "Veteran";
                case SkirmishDifficultyId.Commander: return "Commander";
                default: return string.Empty;
            }
        }
    }
}
