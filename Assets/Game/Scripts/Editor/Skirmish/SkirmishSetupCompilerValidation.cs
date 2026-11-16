#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using Game.Configs;
using Game.Skirmish.Contracts;
using UnityEditor;
using UnityEngine;

namespace Game.Editor
{
    public static class SkirmishSetupCompilerValidation
    {
        public static string Run()
        {
            SkirmishDefinitionBuilder.AssertCatalogCsvUnchanged();
            string root = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            if (!SkirmishSetupMatrixTable.TryLoad(root, out List<SkirmishSetupMatrixRow> matrix, out string error))
                throw new InvalidOperationException(error);

            SkirmishExpansionAuthoredSet authored = SkirmishExpansionCatalogFactory.CreateInMemory();
            SkirmishExpansionCatalogConfig packagedCatalog = SkirmishExpansionCatalogConfig.Load();
            if (packagedCatalog == null || !packagedCatalog.TryGetDefinition("S002", 2, out SkirmishScenarioDefinitionConfig packagedS002) ||
                packagedS002 == null || packagedS002.CampaignMap != null || packagedS002.MapLayout == null)
                throw new InvalidOperationException("Packaged S002 v2 legacy definition and layout are required.");

            // S002's coded legacy binding is validated against its packaged definition.
            // Keep the other definitions from the in-memory authored set for catalog coverage.
            authored.DefinitionS002 = packagedS002;
            var manifest = new SkirmishContentManifest
            {
                RequiredFeatureIds = packagedS002.RequiredFeatureIds
            };

            SkirmishSizeId[] sizes =
            {
                SkirmishSizeId.Standard, SkirmishSizeId.War, SkirmishSizeId.LargeWar
            };
            int compiled = 0;
            for (int i = 0; i < sizes.Length; i++)
            {
                if (!SkirmishSetupCompiler.TryCompile(
                        authored.DefinitionS002,
                        SkirmishDifficultyId.Regular,
                        sizes[i],
                        sizes[i] == SkirmishSizeId.Standard ? 104731 : 0,
                        manifest,
                        matrix,
                        out SkirmishResolvedSetup setup,
                        out List<SkirmishCompileReason> reasons))
                    throw new InvalidOperationException("S002 " + sizes[i] + " failed: " + Format(reasons));
                if (setup.CatalogId != "S002")
                    throw new InvalidOperationException("Compiled catalog id drifted.");
                if (setup.ContentVersion != 2 || setup.OperationMapId != packagedS002.OperationMapId ||
                    setup.LayoutId != packagedS002.MapLayout.LayoutId)
                    throw new InvalidOperationException("S002 must retain the packaged v2 Desert Base legacy binding.");
                if (sizes[i] == SkirmishSizeId.Standard && !setup.MeasuredLayoutBound)
                    throw new InvalidOperationException("Regular Standard must bind the measured Desert Base layout.");
                compiled++;
            }

            if (SkirmishSetupCompiler.TryCompile(
                    null,
                    SkirmishDifficultyId.Regular,
                    SkirmishSizeId.Standard,
                    104731,
                    manifest,
                    matrix,
                    out _,
                    out List<SkirmishCompileReason> missing))
                throw new InvalidOperationException("Missing definition must fail.");
            if (missing.Count == 0 || missing[0].Code != SkirmishReasonCode.MissingDefinition)
                throw new InvalidOperationException("Missing definition must report MissingDefinition.");

            uint first = CompileHash(authored, manifest, matrix, 104731);
            uint second = CompileHash(authored, manifest, matrix, 104731);
            if (first != second)
                throw new InvalidOperationException("Same seed must produce the same setup hash.");

            SkirmishSetupCompiler.TryCompileCatalog(authored, matrix, manifest, out List<SkirmishResolvedSetup> all, out List<SkirmishCompileReason> catalogReasons);
            if (all.Count != 3)
                throw new InvalidOperationException("Expected three compiled S002 sizes, got " + all.Count);
            int missingDefinitions = 0;
            for (int i = 0; i < catalogReasons.Count; i++)
            {
                if (catalogReasons[i].Code == SkirmishReasonCode.MissingDefinition)
                    missingDefinitions++;
            }

            return "[SkirmishSetupCompilerValidation] result=Passed compiled=" + compiled +
                   " missingDefinitions=" + missingDefinitions +
                   " matrixRows=" + matrix.Count;
        }

        public static void RunFocusedValidation()
        {
            try
            {
                Debug.Log(Run());
                EditorApplication.Exit(0);
            }
            catch (Exception exception)
            {
                Debug.LogError("[SkirmishSetupCompilerValidation] result=Failed\n" + exception);
                EditorApplication.Exit(1);
            }
        }

        private static uint CompileHash(
            SkirmishExpansionAuthoredSet authored,
            SkirmishContentManifest manifest,
            List<SkirmishSetupMatrixRow> matrix,
            int seed)
        {
            if (!SkirmishSetupCompiler.TryCompile(
                    authored.DefinitionS002,
                    SkirmishDifficultyId.Regular,
                    SkirmishSizeId.Standard,
                    seed,
                    manifest,
                    matrix,
                    out SkirmishResolvedSetup setup,
                    out List<SkirmishCompileReason> reasons))
                throw new InvalidOperationException(Format(reasons));
            return setup.SetupHash;
        }

        private static string Format(List<SkirmishCompileReason> reasons)
        {
            if (reasons == null || reasons.Count == 0)
                return "no reasons";
            return reasons[0].ToString();
        }
    }
}
#endif
