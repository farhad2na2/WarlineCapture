using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using Game.Configs;
using Game.Runtime;
using Game.Skirmish.Contracts;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

internal static class SourceValidatedLayoutJsonControl
{
    private static string Hash(byte[] bytes)
    { using var sha = SHA256.Create(); return BitConverter.ToString(sha.ComputeHash(bytes)).Replace("-", "").ToLowerInvariant(); }

    public static int Main(string[] args)
    {
        if (args.Length != 8 && args.Length != 9) throw new ArgumentException("Graph, graph hash, matrix, matrix hash, root, capture, capture hash, output directory and optional catalog check required");
        var input = SourceExpandedAuthoredSetupInput.Prepare(args[0], args[1], args[2], args[3], args[4], "S004");
        if (args.Length == 9)
        {
            if (args[8] != "require-catalog") throw new ArgumentException("Unknown catalog control");
            var canonical = input.Assets.ByPath<SkirmishBattleCatalogConfig>("Assets/Game/Resources/SkirmishBattleCatalog.asset");
            if (canonical == null || !canonical.TryGet("S004", out var row) || row.DefinitionId != input.Definition.DefinitionId ||
                row.ContentVersion != input.Definition.ContentVersion)
                throw new InvalidDataException("Actual canonical catalog entry differs from the authored S004 definition");
            Console.WriteLine("[SourceValidatedLayoutJsonControl] result=Passed canonicalCatalogIdentity=1 sourceOnly=1 readiness=0");
        }
        var layout = input.Definition.MapLayout;
        var projection = SkirmishMeasuredLayoutBinder.LayoutValues.From(layout);
        int rejected = 0;
        void Reject(Action action)
        {
            try { action(); }
            catch (InvalidDataException) { rejected++; return; }
            catch (InvalidOperationException) { rejected++; return; }
            catch (NotSupportedException) { rejected++; return; }
            throw new Exception("Invalid layout replay was accepted");
        }
        Reject(() => UnityEngine.JsonUtility.ToJson(projection));
        Reject(() => SourceValidatedLayoutJson.Open(layout, args[5], new string('0', 64)));
        var bad = JObject.Parse(File.ReadAllText(args[5]));
        bad["OriginX"] = (float)bad["OriginX"] + 1f;
        byte[] badBytes = Encoding.UTF8.GetBytes(bad.ToString(Formatting.None));
        string badPath = Path.Combine(args[7], "bad-layout.json");
        File.WriteAllBytes(badPath, badBytes);
        Reject(() => SourceValidatedLayoutJson.Open(layout, badPath, Hash(badBytes)));
        var badSchema = JObject.Parse(File.ReadAllText(args[5]));
        badSchema["extra"] = true;
        byte[] schemaBytes = Encoding.UTF8.GetBytes(badSchema.ToString(Formatting.None));
        string schemaPath = Path.Combine(args[7], "bad-schema.json");
        File.WriteAllBytes(schemaPath, schemaBytes);
        Reject(() => SourceValidatedLayoutJson.Open(layout, schemaPath, Hash(schemaBytes)));
        var map = input.Assets.ByPath<OperationMapDefinition>("Assets/Game/Resources/SkirmishS004OperationMap.asset");
        if (map == null || map.OperationMapId != layout.OperationMapId) throw new InvalidDataException("Actual S004 source map required");
        UnityEngine.Resources.BindSourceResources(new Dictionary<string, object>
        {
            [SkirmishExpansionCatalogConfig.ResourceName] = input.Catalog,
            ["SkirmishS004OperationMap"] = map,
            [SkirmishSetupMatrixTable.PackagedResourceName] = new UnityEngine.TextAsset(File.ReadAllText(args[2]))
        });
        var results = new JArray();
        using (SourceValidatedLayoutJson.Open(layout, args[5], args[6]))
        {
            if (UnityEngine.JsonUtility.ToJson(projection) != File.ReadAllText(args[5])) throw new Exception("Exact captured bytes required");
            var changed = SkirmishMeasuredLayoutBinder.LayoutValues.From(layout);
            changed.OriginX += 1f;
            Reject(() => UnityEngine.JsonUtility.ToJson(changed));
            Reject(() => UnityEngine.JsonUtility.ToJson(new object()));
            Reject(() => SourceValidatedLayoutJson.Open(layout, args[5], args[6]));
            foreach (var difficulty in new[] { SkirmishDifficultyId.Recruit, SkirmishDifficultyId.Regular, SkirmishDifficultyId.Veteran, SkirmishDifficultyId.Commander })
            foreach (var size in new[] { SkirmishSizeId.Standard, SkirmishSizeId.War, SkirmishSizeId.LargeWar })
            {
                var setup = input.Compile(difficulty, size, 104733);
                if (!SkirmishSetupCompiler.TryCompile(input.Definition, difficulty, size, 104733,
                        input.Manifest, input.Matrix, out var direct, out var reasons) || direct == null ||
                    !JToken.DeepEquals(JToken.Parse(JsonConvert.SerializeObject(setup)), JToken.Parse(JsonConvert.SerializeObject(direct))))
                    throw new InvalidDataException("Direct original compiler differs: " + String.Join(" | ", reasons));
                results.Add(new JObject { ["difficulty"] = difficulty.ToString(), ["size"] = size.ToString(),
                    ["setup"] = JToken.Parse(JsonConvert.SerializeObject(setup)) });
                Console.WriteLine("[SourceValidatedLayoutJsonControl] result=Passed difficulty=" + difficulty + " size=" + size + " setupHash=" + setup.SetupHash + " sourceOnly=1 readiness=0");
            }
        }
        Reject(() => UnityEngine.JsonUtility.ToJson(projection));
        byte[] originalBytes = File.ReadAllBytes(args[5]);
        string copyPath = Path.Combine(args[7], "mutable-layout.json");
        File.WriteAllBytes(copyPath, originalBytes);
        var mutable = SourceValidatedLayoutJson.Open(layout, copyPath, Hash(originalBytes));
        File.WriteAllBytes(copyPath, badBytes);
        Reject(() => mutable.Dispose());
        Reject(() => UnityEngine.JsonUtility.ToJson(projection));
        if (rejected != 10) throw new Exception("Expected ten layout replay rejections");
        File.WriteAllText(Path.Combine(args[7], "compiled-setups.json"), results.ToString(Formatting.Indented));
        Console.WriteLine("[SourceValidatedLayoutJsonControl] result=Passed invalid=10 scope=CapturedLayoutOnly readiness=0");
        return 0;
    }
}
