using System.Collections.Generic;
using Game.Configs;
using Game.Skirmish.Contracts;
using Unity.Entities;

namespace Game.Composition
{
    public static class SkirmishExpandedLaunchResolver
    {
        public static bool TryCompileAndQueue(
            EntityManager em,
            string catalogId,
            SkirmishDifficultyId difficulty,
            SkirmishSizeId size,
            int seed,
            SkirmishExpansionAuthoredSet authored,
            IReadOnlyList<SkirmishSetupMatrixRow> matrix,
            SkirmishContentManifest manifest,
            out SkirmishResolvedSetup setup,
            out SkirmishLaunchPayload payload,
            out List<SkirmishCompileReason> reasons,
            UnitPrefabRegistryAuthoringConfig registry = null)
        {
            setup = null;
            payload = null;
            reasons = new List<SkirmishCompileReason>();
            if (!SkirmishExpansionCatalogFactory.TryGetDefinition(authored, catalogId, out SkirmishScenarioDefinitionConfig definition))
            {
                reasons.Add(new SkirmishCompileReason(SkirmishReasonCode.MissingDefinition, "catalogId", catalogId));
                return false;
            }

            if (!SkirmishSetupCompiler.TryCompile(
                    definition,
                    difficulty,
                    size,
                    seed,
                    manifest,
                    matrix,
                    out setup,
                    out reasons))
                return false;

            payload = new SkirmishLaunchPayload
            {
                SessionId = "expanded-" + catalogId.ToLowerInvariant() + "-" + seed,
                CatalogId = setup.CatalogId,
                DefinitionId = setup.DefinitionId,
                ContentVersion = setup.ContentVersion,
                AllConfigHashes = setup.SetupHash.ToString("X8"),
                MapId = setup.OperationMapId,
                LayoutId = setup.LayoutId,
                DifficultyId = difficulty,
                SizeId = size,
                Seed = seed,
                IsCustom = false,
                IsLegacy = false
            };
            return SkirmishExpandedLaunchProjection.TryQueue(em, payload, setup, registry);
        }
    }
}
