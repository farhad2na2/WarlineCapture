using System;
using System.Collections.Generic;
using System.IO;
using Game.Configs;
using Game.Skirmish.Contracts;
using UnityEngine;

namespace Game.Tests.Editor
{
    // Import only reviewed standalone binder/areas and tests. Do not apply compiler
    // seam first: this captures the current compiler's immutable before-baseline.
    public static class SkirmishObjectiveFoundationValidation
    {
        [Serializable] private sealed class BaselineRecord
        {
            public int SchemaVersion = 1;
            public string UnityVersion;
            public string Purpose = "PreCompilerIntegrationLegacyLayoutAndSetupBaselineOnly";
            public bool NativeCandidateProof = false;
            public BaselineRow[] Rows;
        }
        [Serializable] private sealed class BaselineRow
        {
            public string CatalogId, DefinitionId, LayoutId, LayoutSemanticSha256;
            public string Difficulty, Size, SetupHashHex, PolicylessHashHex;
            public int Seed;
        }
        private static BaselineRecord Capture()
        {
            var catalog = SkirmishExpansionCatalogConfig.Load();
            if (catalog == null) throw new InvalidOperationException("Existing packaged catalog missing.");
            string project = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            if (!SkirmishSetupMatrixTable.TryLoad(project, out var matrix, out string error)) throw new InvalidOperationException(error);
            var rows = new List<BaselineRow>();
            foreach (string id in new[] { "S002", "S003", "S004", "S005" })
            {
                if (!catalog.TryGetDefinition(id, out var definition) || definition.MapLayout == null)
                    throw new InvalidOperationException("Existing packaged legacy definition/layout missing: " + id);
                string layoutHash = SkirmishMeasuredLayoutBinder.ComputeLayoutSemanticFingerprint(definition.MapLayout);
                foreach (var size in new[] { SkirmishSizeId.Standard, SkirmishSizeId.War, SkirmishSizeId.LargeWar })
                {
                    // Reproducible baseline sample seed, not a new gameplay seed rule.
                    const int seed = 155926;
                    if (!SkirmishSetupCompiler.TryCompile(definition, SkirmishDifficultyId.Regular, size, seed,
                        new SkirmishContentManifest { RequiredFeatureIds = definition.RequiredFeatureIds }, matrix,
                        out var setup, out var reasons)) throw new InvalidOperationException("Baseline compile failed " + id + "/" + size + ": " + string.Join(";", reasons));
                    rows.Add(new BaselineRow { CatalogId = id, DefinitionId = definition.DefinitionId,
                        LayoutId = definition.MapLayoutId, LayoutSemanticSha256 = layoutHash,
                        Difficulty = SkirmishDifficultyId.Regular.ToString(), Size = size.ToString(), Seed = seed,
                        SetupHashHex = setup.SetupHash.ToString("X8"), PolicylessHashHex = SkirmishSetupCompiler.ComputePolicylessHash(setup, seed).ToString("X8") });
                }
            }
            return new BaselineRecord { UnityVersion = Application.unityVersion, Rows = rows.ToArray() };
        }
        public static void RunFocusedValidation()
        {
            try
            {
                string before = JsonUtility.ToJson(Capture(), true);
                string project = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
                // Unity deletes Temp during Editor lifecycle cleanup. Evidence must
                // survive process exit so the coordinator can verify and pin it.
                string evidenceRoot = Path.GetFullPath(Path.Combine(project, "Logs", "SkirmishProduction", "Baselines")) + Path.DirectorySeparatorChar;
                string configured = Environment.GetEnvironmentVariable("WARLINE_OBJECTIVE_BASELINE_JSON");
                string output = Path.GetFullPath(string.IsNullOrEmpty(configured) ? Path.Combine(evidenceRoot,
                    "legacy-objectives-" + DateTime.UtcNow.Ticks + ".json") : configured);
                if (!output.StartsWith(evidenceRoot, StringComparison.OrdinalIgnoreCase)) throw new InvalidOperationException("Baseline output must remain inside this project's persistent ignored validation directory.");
                Directory.CreateDirectory(Path.GetDirectoryName(output));
                using (var stream = new FileStream(output, FileMode.CreateNew, FileAccess.Write, FileShare.Read))
                using (var writer = new StreamWriter(stream, new System.Text.UTF8Encoding(false))) writer.Write(before);
                Debug.Log("[SkirmishObjectiveFoundationBaseline] result=Captured catalogs=4 configurations=12 path=" + output + " nativeCandidateProof=0");
                SkirmishMeasuredLayoutBinderTests.RunFocusedValidation();
                SkirmishObjectiveAreaConfigTests.RunFocusedValidation();
                string after = JsonUtility.ToJson(Capture(), true);
                if (before != after) throw new InvalidOperationException("Standalone logical tests changed pre-integration packaged baseline.");
                Game.Editor.SkirmishProductionNativeIdentityCensus.ExportS005RegularStandard155926Fa();
                Debug.Log("[SkirmishObjectiveFoundationValidation] result=Passed binder=8 areas=15 tests=23 baselineCatalogs=4 baselineConfigurations=12 compilerPatchApplied=0 nativeGameplayCertified=0");
            }
            catch (Exception error)
            {
                Debug.LogError("[SkirmishObjectiveFoundationValidation] result=Failed\n" + error);
                throw;
            }
        }
    }
}
