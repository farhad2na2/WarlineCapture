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
            var manifest = new SkirmishContentManifest
            {
                RequiredFeatureIds = authored.DefinitionS002.RequiredFeatureIds
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
