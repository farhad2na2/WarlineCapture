using System;

namespace Game.Runtime
{
    [Flags]
    internal enum MaterialsScenarioRecoveryPathCode : byte
    {
        None = 0,
        StartingMaterials = 1 << 0,
        SeededFabricationChain = 1 << 1,
        RebuildableFabricationChain = 1 << 2,
        ExchangeImport = 1 << 3,
        MaterialsNotRequired = 1 << 4
    }

    internal enum MaterialsScenarioRecoveryValidationCode : byte
    {
        Valid = 0,
        MissingStartupState = 1,
        MissingFactionControls = 2,
        DuplicateFaction = 3,
        MissingMaterialsCapacity = 4,
        NoRecoveryPath = 5,
        InvalidConstructionPlan = 6,
        MissingConstructionDefinition = 7,
        CatalogNotReady = 8
    }

    internal readonly struct MaterialsScenarioRecoveryValidationInput
    {
        public readonly byte FactionId;
        public readonly bool MaterialsRequired;
        public readonly int MinimumRequiredMaterials;
        public readonly int StartingMaterialsRequirement;
        public readonly int StartingMaterials;
        public readonly int MaterialsCapacity;
        public readonly bool HasSeededDepot;
        public readonly bool HasSeededOilSource;
        public readonly bool HasSeededOilHauler;
        public readonly bool CanRebuildDepot;
        public readonly bool CanRebuildOilSource;
        public readonly bool CanAcquireOilHauler;
        public readonly bool CanAffordRebuildChain;
        public readonly bool ExchangeImportEnabled;

        public MaterialsScenarioRecoveryValidationInput(
            byte factionId,
            bool materialsRequired,
            int minimumRequiredMaterials,
            int startingMaterialsRequirement,
            int startingMaterials,
            int materialsCapacity,
            bool hasSeededDepot,
            bool hasSeededOilSource,
            bool hasSeededOilHauler,
            bool canRebuildDepot,
            bool canRebuildOilSource,
            bool canAcquireOilHauler,
            bool canAffordRebuildChain,
            bool exchangeImportEnabled)
        {
            FactionId = factionId;
            MaterialsRequired = materialsRequired;
            MinimumRequiredMaterials = minimumRequiredMaterials;
            StartingMaterialsRequirement = startingMaterialsRequirement;
            StartingMaterials = startingMaterials;
            MaterialsCapacity = materialsCapacity;
            HasSeededDepot = hasSeededDepot;
            HasSeededOilSource = hasSeededOilSource;
            HasSeededOilHauler = hasSeededOilHauler;
            CanRebuildDepot = canRebuildDepot;
            CanRebuildOilSource = canRebuildOilSource;
            CanAcquireOilHauler = canAcquireOilHauler;
            CanAffordRebuildChain = canAffordRebuildChain;
            ExchangeImportEnabled = exchangeImportEnabled;
        }
    }

    internal readonly struct MaterialsScenarioRecoveryValidationResult
    {
        public readonly bool IsValid;
        public readonly MaterialsScenarioRecoveryValidationCode Code;
        public readonly MaterialsScenarioRecoveryPathCode Paths;
        public readonly byte FactionId;
        public readonly int ValidatedFactionCount;

        public MaterialsScenarioRecoveryValidationResult(
            bool isValid,
            MaterialsScenarioRecoveryValidationCode code,
            MaterialsScenarioRecoveryPathCode paths,
            byte factionId,
            int validatedFactionCount)
        {
            IsValid = isValid;
            Code = code;
            Paths = paths;
            FactionId = factionId;
            ValidatedFactionCount = validatedFactionCount;
        }
    }

    internal static class MaterialsScenarioRecoveryPolicyUtilitySystemHelper
    {
        internal static MaterialsScenarioRecoveryValidationResult Evaluate(
            in MaterialsScenarioRecoveryValidationInput input)
        {
            if (!input.MaterialsRequired)
            {
                return Valid(input.FactionId, MaterialsScenarioRecoveryPathCode.MaterialsNotRequired);
            }

            int minimumRequiredMaterials = Math.Max(1, input.MinimumRequiredMaterials);
            if (input.MaterialsCapacity < minimumRequiredMaterials)
            {
                return Invalid(
                    MaterialsScenarioRecoveryValidationCode.MissingMaterialsCapacity,
                    input.FactionId,
                    validatedFactionCount: 1);
            }

            MaterialsScenarioRecoveryPathCode paths = MaterialsScenarioRecoveryPathCode.None;
            int startingMaterialsRequirement = Math.Max(minimumRequiredMaterials, input.StartingMaterialsRequirement);
            if (input.StartingMaterials >= startingMaterialsRequirement)
                paths |= MaterialsScenarioRecoveryPathCode.StartingMaterials;
            if (input.HasSeededDepot && input.HasSeededOilSource && input.HasSeededOilHauler)
                paths |= MaterialsScenarioRecoveryPathCode.SeededFabricationChain;
            if (input.CanRebuildDepot && input.CanRebuildOilSource && input.CanAcquireOilHauler && input.CanAffordRebuildChain)
                paths |= MaterialsScenarioRecoveryPathCode.RebuildableFabricationChain;
            if (input.ExchangeImportEnabled)
                paths |= MaterialsScenarioRecoveryPathCode.ExchangeImport;

            return paths != MaterialsScenarioRecoveryPathCode.None
                ? Valid(input.FactionId, paths)
                : Invalid(
                    MaterialsScenarioRecoveryValidationCode.NoRecoveryPath,
                    input.FactionId,
                    validatedFactionCount: 1);
        }

        internal static MaterialsScenarioRecoveryValidationResult Valid(
            byte factionId,
            MaterialsScenarioRecoveryPathCode paths,
            int validatedFactionCount = 1)
        {
            return new MaterialsScenarioRecoveryValidationResult(
                true,
                MaterialsScenarioRecoveryValidationCode.Valid,
                paths,
                factionId,
                validatedFactionCount);
        }

        internal static MaterialsScenarioRecoveryValidationResult Invalid(
            MaterialsScenarioRecoveryValidationCode code,
            byte factionId = 0,
            int validatedFactionCount = 0,
            MaterialsScenarioRecoveryPathCode paths = MaterialsScenarioRecoveryPathCode.None)
        {
            return new MaterialsScenarioRecoveryValidationResult(
                false,
                code,
                paths,
                factionId,
                validatedFactionCount);
        }
    }
}
