using System;
using System.IO;
using System.Security.Cryptography;
using Game.Configs;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Game.Runtime
{
    // Source-only typed setup materialization. Caller must independently establish
    // evidence provenance, source pins and all runtime admission prerequisites.
    internal static class SourceS004CompiledSetupInput
    {
        // Bind a live source setup to every field in the previously checked export.
        // The original setup hash is deliberately not a full-state digest.
        internal static void RequireExact(SkirmishResolvedSetup actual, string path, string expectedSha256)
        {
            var expected = Read(path, expectedSha256);
            if (actual == null || !JToken.DeepEquals(JObject.FromObject(actual), JObject.FromObject(expected)))
                throw new InvalidDataException("S004 live setup differs from the pinned original compiler output");
        }

        internal static SkirmishResolvedSetup Read(string path, string expectedSha256)
        {
            if (!Path.IsPathRooted(path ?? "") || expectedSha256 == null || expectedSha256.Length != 64)
                throw new InvalidDataException("Absolute pinned S004 setup input required");
            byte[] bytes = File.ReadAllBytes(path);
            string actual;
            using (var hash = SHA256.Create()) actual = BitConverter.ToString(hash.ComputeHash(bytes)).Replace("-", "").ToLowerInvariant();
            if (!StringComparer.Ordinal.Equals(actual, expectedSha256)) throw new InvalidDataException("S004 setup input hash changed");
            var export = JObject.Parse(System.Text.Encoding.UTF8.GetString(bytes));
            bool legacy = (string)export["kind"] == "S004StandardCompiledSetupSourceExport" && (string)export["status"] == "Passed";
            bool difficultyReadback = (string)export["kind"] == "S004StandardDifficultySourceCompilerReadback" &&
                (string)export["status"] == "Collected" && (bool?)export["runtime_admission"] == false;
            if ((!legacy && !difficultyReadback) || (bool?)export["world_initialized"] != false || (bool?)export["physical_source_fit_verified"] != false ||
                (bool?)export["mission_readiness"] != false || !(export["compiled_setup"] is JObject payload))
                throw new InvalidDataException("Exact source-only compiled S004 export required");
            var setup = payload.ToObject<SkirmishResolvedSetup>();
            uint expectedHash = setup?.DifficultyId switch
            {
                Game.Skirmish.Contracts.SkirmishDifficultyId.Recruit when difficultyReadback => 106654757u,
                Game.Skirmish.Contracts.SkirmishDifficultyId.Regular => 1130726651u,
                Game.Skirmish.Contracts.SkirmishDifficultyId.Veteran when difficultyReadback => 3038972422u,
                Game.Skirmish.Contracts.SkirmishDifficultyId.Commander when difficultyReadback => 3329072857u,
                _ => 0u
            };
            if (setup == null || setup.CatalogId != "S004" || setup.DefinitionId != "skirmish.s004" ||
                setup.Seed != 104733 || expectedHash == 0 || setup.SetupHash != expectedHash ||
                (difficultyReadback && (string)export["difficulty"] != setup.DifficultyId.ToString()) ||
                setup.Forces == null || setup.Forces.Length != 18 ||
                setup.Structures == null || setup.Structures.Length != 20 || SkirmishSetupCompiler.ComputeHash(setup, setup.Seed) != setup.SetupHash)
                throw new InvalidDataException("Exact compiled S004 setup identity and original hash required");
            // The legacy setup hash does not cover measured-layout state. A
            // caller's separately verified export must still be spatially usable.
            if (!setup.MeasuredLayoutBound ||
                setup.SizeId != Game.Skirmish.Contracts.SkirmishSizeId.Standard ||
                !FinitePositive(setup.WorldWidthMetres) || !FinitePositive(setup.WorldDepthMetres) || !FinitePositive(setup.CellSize))
                throw new InvalidDataException("Measured S004 Standard layout with finite positive dimensions required");
            // Full JSON roundtrip guards fields not covered by the original setup hash.
            var roundtrip = JObject.Parse(JsonConvert.SerializeObject(setup, Formatting.None));
            if (!JToken.DeepEquals(payload, roundtrip)) throw new InvalidDataException("S004 setup field roundtrip differs");
            return setup;
        }

        private static bool FinitePositive(float value) => value > 0 && !float.IsNaN(value) && !float.IsInfinity(value);
    }
}
