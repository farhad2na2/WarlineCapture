// Import with the pure SkirmishProductionCanaryRequest serializer only.
// No harness seam, Play session, catalog publication or gameplay mutation.
#if UNITY_EDITOR
using System;
using System.IO;
using System.Text;
using Game.Configs;
using Game.Skirmish.Contracts;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;

namespace Game.Editor
{
    public static class SkirmishProductionNativeIdentityCensus
    {
        public static void ExportS005RegularStandard155926Fa()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || EditorApplication.isUpdating)
                throw new InvalidOperationException("Native compiled census requires idle Edit mode.");
            const string path = "Assets/Game/Configs/SkirmishExpansion/Scenarios/S005/SkirmishScenario_S005.asset";
            var definition = AssetDatabase.LoadAssetAtPath<SkirmishScenarioDefinitionConfig>(path);
            var catalog = SkirmishExpansionCatalogConfig.Load();
            if (definition == null || catalog == null || !catalog.TryGetDefinition("S005", out var packaged) || packaged != definition)
                throw new InvalidDataException("Exact packaged native S005 asset/registry binding is missing.");
            if (!SkirmishSetupMatrixTable.TryLoadPackaged(out var matrix, out string error)) throw new InvalidDataException(error);
            var manifest = new SkirmishContentManifest { RequiredFeatureIds = definition.RequiredFeatureIds };
            if (!SkirmishSetupCompiler.TryCompile(definition, SkirmishDifficultyId.Regular, SkirmishSizeId.Standard,
                155926, manifest, matrix, out var setup, out var reasons))
                throw new InvalidDataException("Actual packaged S005 compile failed: " + string.Join(";", reasons));
            string previousLocale = GameLocalization.CurrentLocaleCode;
            JObject envelope;
            try
            {
                // Temporary native localization context only; persist=false never
                // writes PlayerPrefs. Actual applied locale is independently read.
                if (!GameLocalization.SetLocale(GameLocalization.PersianLocaleCode, false) ||
                    GameLocalization.CurrentLocaleCode != GameLocalization.PersianLocaleCode)
                    throw new InvalidDataException("Native FA locale did not resolve exactly.");
                var identity = SkirmishProductionCanaryRequest.CreateNativeSetupIdentity(setup.CatalogId,
                    setup.DefinitionId, setup.ContentVersion, setup.DifficultyId.ToString(), setup.SizeId.ToString(),
                    setup.Seed, GameLocalization.CurrentLocaleCode, setup.SetupHash, setup.ContentHash);
                envelope = new JObject { ["schema_version"] = 1, ["collector_kind"] = "SkirmishNativeCompiledSetupCensus",
                    ["native_setup_identity"] = identity, ["actual_native_locale"] = GameLocalization.CurrentLocaleCode,
                    ["native_config_sha256"] = SkirmishAcceptanceCensusCapture.ComputeConfigHash(setup) };
            }
            finally
            {
                if (!GameLocalization.SetLocale(previousLocale, false) || GameLocalization.CurrentLocaleCode != previousLocale)
                    throw new InvalidDataException("Native census failed to restore previous locale.");
            }
            string root = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            string output = Environment.GetEnvironmentVariable("WARLINE_NATIVE_IDENTITY_OUTPUT");
            if (string.IsNullOrWhiteSpace(output)) output = Path.Combine(root, "Logs", "SkirmishProduction", "NativeIdentity",
                "s005-regular-standard-155926-fa-" + Guid.NewGuid().ToString("N") + ".json");
            output = Path.GetFullPath(output);
            foreach (string tree in new[] { "Assets", "Packages", "ProjectSettings" })
            {
                string directory = Path.Combine(root, tree);
                if (string.Equals(output, directory, StringComparison.OrdinalIgnoreCase) ||
                    output.StartsWith(directory + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
                    throw new InvalidDataException("Census output cannot enter source trees.");
            }
            Directory.CreateDirectory(Path.GetDirectoryName(output));
            using (var stream = new FileStream(output, FileMode.CreateNew, FileAccess.Write, FileShare.Read))
            using (var writer = new StreamWriter(stream, new UTF8Encoding(false))) writer.Write(envelope.ToString(Formatting.Indented));
            Debug.Log("[SkirmishProductionNativeIdentityCensus] result=Passed catalog=S005 seed=155926 locale=FA nativeSetupHash=" +
                setup.SetupHash.ToString("X8") + " output=" + output + " gameplayCertified=0");
        }
    }
}
#endif
