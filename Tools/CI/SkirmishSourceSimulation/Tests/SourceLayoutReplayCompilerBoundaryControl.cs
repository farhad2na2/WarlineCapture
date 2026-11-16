using System;
using System.Collections.Generic;
using System.IO;
using Game.Configs;
using Game.Runtime;
using Game.Skirmish.Contracts;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

internal static class SourceLayoutReplayCompilerBoundaryControl
{
    public static int Main(string[] args)
    {
        if (args.Length != 13) throw new ArgumentException("S004 graph/hash, S002 graph/hash, matrix/hash, capture/hash, root, expected setups, mode and resource bindings/hash required");
        var s004 = SourceExpandedAuthoredSetupInput.Prepare(args[0], args[1], args[4], args[5], args[8], "S004");
        var s002 = SourceExpandedAuthoredSetupInput.Prepare(args[2], args[3], args[4], args[5], args[8], "S002");
        var canonical = s002.Assets.ByPath<SkirmishBattleCatalogConfig>("Assets/Game/Resources/SkirmishBattleCatalog.asset");
        if (args[10] == "s002-default")
        {
            var baseline = new JObject { ["setup_matrix"] = new JObject { ["path"] = args[4], ["sha256"] = args[5] },
                ["selection"] = new JObject { ["difficulty_id"] = (int)SkirmishDifficultyId.Regular, ["size_id"] = (int)SkirmishSizeId.Standard, ["seed"] = 104731 } };
            var actual = AutonomousWorld.CompileOriginalSetup(baseline, s002.Definition, canonical, s002.Catalog, out var rows, out var content);
            var expected = s002.Compile(SkirmishDifficultyId.Regular, SkirmishSizeId.Standard, 104731);
            if (!JToken.DeepEquals(JToken.Parse(JsonConvert.SerializeObject(actual)), JToken.Parse(JsonConvert.SerializeObject(expected))))
                throw new InvalidDataException("Default S002 compiler output differs");
            Console.WriteLine("[SourceLayoutReplayCompilerBoundaryControl] result=Passed mode=s002-default setup=" + actual.SetupHash + " world=0 readiness=0");
            return 0;
        }
        var bindings = JArray.Parse(File.ReadAllText(args[11]));
        var resourceDescriptor = new JObject { ["path"] = args[11], ["sha256"] = args[12] };
        var resources = SourceExpandedAuthoredSetupInput.LoadOperationMapResources(s004.Assets, s004.Definition, resourceDescriptor);
        if (canonical == null || resources.Count != 1) throw new InvalidDataException("Actual pinned catalog and S004 source resource required");
        int resourceRejections = 0;
        void RejectResource(JToken token)
        {
            try { s004.MaterializeOperationMapResources(token); }
            catch (InvalidDataException) { resourceRejections++; return; }
            catch (NotSupportedException) { resourceRejections++; return; }
            throw new Exception("Invalid source resource binding was accepted");
        }
        var wrongName = (JArray)bindings.DeepClone(); wrongName[0]["name"] = "Alias"; RejectResource(wrongName);
        var wrongPath = (JArray)bindings.DeepClone(); wrongPath[0]["asset_path"] = "Assets/Game/Resources/../SkirmishS004OperationMap.asset"; RejectResource(wrongPath);
        var extra = (JArray)bindings.DeepClone(); extra[0]["extra"] = true; RejectResource(extra);
        var duplicate = (JArray)bindings.DeepClone(); duplicate.Add(duplicate[0].DeepClone()); RejectResource(duplicate);
        RejectResource(new JObject());
        if (resourceRejections != 5) throw new Exception("Expected five resource binding rejections");
        int descriptorRejections = 0;
        void RejectDescriptor(JObject descriptor)
        {
            try { SourceExpandedAuthoredSetupInput.LoadOperationMapResources(s004.Assets, s004.Definition, descriptor); }
            catch (InvalidDataException) { descriptorRejections++; return; }
            catch (InvalidOperationException) { descriptorRejections++; return; }
            throw new Exception("Invalid source resource descriptor was accepted");
        }
        RejectDescriptor(new JObject { ["path"] = args[11], ["sha256"] = new string('0', 64) });
        RejectDescriptor(new JObject { ["path"] = args[11], ["sha256"] = args[12], ["extra"] = true });
        if (descriptorRejections != 2) throw new Exception("Expected two source resource descriptor rejections");
        var replay = new JObject { ["path"] = args[6], ["sha256"] = args[10] == "bad-hash" ? new string('0', 64) : args[7] };
        if (args[10] == "bad-shape") replay["extra"] = true;
        if (args[10] != "good" && args[10] != "bad-hash" && args[10] != "bad-shape") throw new ArgumentException("Unknown control mode");
        var request = new JObject { ["setup_matrix"] = new JObject { ["path"] = args[4], ["sha256"] = args[5] },
            ["selection"] = new JObject { ["difficulty_id"] = (int)SkirmishDifficultyId.Regular, ["size_id"] = (int)SkirmishSizeId.Standard, ["seed"] = 104733 },
            ["layout_json_replay"] = replay };
        try
        {
            var setup = AutonomousWorld.CompileOriginalSetup(request, s004.Definition, canonical, s004.Catalog,
                out var matrix, out var manifest, resources);
            if (args[10] != "good") throw new Exception("Invalid replay reached original compiler");
            var reference = JArray.Parse(File.ReadAllText(args[9]));
            JToken expected = null;
            foreach (JObject row in reference)
                if ((string)row["difficulty"] == "Regular" && (string)row["size"] == "Standard")
                { if (expected != null) throw new InvalidDataException("Ambiguous expected setup"); expected = row["setup"]; }
            if (expected == null || !JToken.DeepEquals(JToken.Parse(JsonConvert.SerializeObject(setup)), expected))
                throw new InvalidDataException("Actual initializer compiler output differs from retained current-source control");
            bool cleared = false;
            try { UnityEngine.JsonUtility.ToJson(SkirmishMeasuredLayoutBinder.LayoutValues.From(s004.Definition.MapLayout)); }
            catch (NotSupportedException) { cleared = true; }
            if (!cleared) throw new InvalidOperationException("Replay must be released after compilation");
            Console.WriteLine("[SourceLayoutReplayCompilerBoundaryControl] result=Passed mode=good setup=" + setup.SetupHash + " resourceRejections=5 descriptorRejections=2 replayReleased=1 world=0 readiness=0");
            return 0;
        }
        catch (Exception error) when (args[10] != "good" && (error is InvalidDataException || error is InvalidOperationException || error is NotSupportedException))
        {
            Console.WriteLine("[SourceLayoutReplayCompilerBoundaryControl] result=Passed mode=" + args[10] + " rejected=1 world=0 readiness=0");
            return 0;
        }
    }
}
