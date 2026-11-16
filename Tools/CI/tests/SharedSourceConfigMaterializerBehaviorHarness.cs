using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using Newtonsoft.Json.Linq;
using UnityEngine;
using Game.Configs;
using Game.Skirmish.Contracts;

namespace Game.Skirmish.Contracts
{
    public enum SkirmishArmyProfileId { GroundManeuver = 1, AirMobile = 2, CombinedArms = 3 }
}

namespace UnityEngine
{
    [AttributeUsage(AttributeTargets.Field)]
    public sealed class SerializeField : Attribute { }

    [AttributeUsage(AttributeTargets.Field)]
    public sealed class SerializeReference : Attribute { }

    public class Object
    {
        public static void DestroyImmediate(Object value) { }
    }

    public class ScriptableObject : Object
    {
        public static ScriptableObject CreateInstance(Type type)
            => (ScriptableObject)Activator.CreateInstance(type, true);
    }

    public struct Vector2Int
    {
        public int x;
        public int y;
        public Vector2Int(int x, int y) { this.x = x; this.y = y; }
    }
}

namespace Game.Configs
{
    public class SkirmishScenarioDefinitionConfig : ScriptableObject { }
    public class SkirmishMapLayoutConfig : ScriptableObject { }
    public class SkirmishMissionLogicalBindingConfig : ScriptableObject { }
    public class OperationMapDefinition : ScriptableObject { }
    public class GridAuthoringSceneConfigAsset : ScriptableObject
    {
        public int width;
        public int height;
        public Vector2Int[] blockedCells;
    }
    public sealed class SkirmishArmyProfileConfig : ScriptableObject
    {
        [SerializeField] private string profileId = "G";
        [SerializeField] private SkirmishArmyProfileId kind = SkirmishArmyProfileId.GroundManeuver;
        [SerializeField] private string[] allowedRoleIds = new string[0];
        [SerializeField] private string[] excludedRoleIds = new string[0];
        [SerializeField] private int contentVersion = 1;
        private string nonSerializedCache;
        public string ProfileId => profileId;
        public SkirmishArmyProfileId Kind => kind;
        public string[] AllowedRoleIds => allowedRoleIds;
        public string[] ExcludedRoleIds => excludedRoleIds;
        public int ContentVersion => contentVersion;
    }
    public sealed class ProbeConfig : ScriptableObject
    {
        public ProbeConfig next;
        public ProbeConfig first;
        public ProbeConfig second;
    }
}

internal static class SharedSourceConfigMaterializerBehaviorHarness
{
    static string Hash(byte[] bytes)
    {
        using (var sha = SHA256.Create())
            return BitConverter.ToString(sha.ComputeHash(bytes)).Replace("-", "").ToLowerInvariant();
    }

    static string WritePinnedFile(string root, string relative, byte[] bytes)
    {
        string path = Path.Combine(root, relative.Replace('/', Path.DirectorySeparatorChar));
        Directory.CreateDirectory(Path.GetDirectoryName(path));
        File.WriteAllBytes(path, bytes);
        return path;
    }

    static JObject Node(string guid, string type, string path, string sha, JObject fields)
        => new JObject
        {
            ["asset_guid"] = guid,
            ["source_type"] = type,
            ["source_path"] = path,
            ["source_sha256"] = sha,
            ["fields"] = fields,
            ["scalar_spellings"] = new JObject()
        };

    static JObject Graph(string pinPath, string pinSha, params JObject[] nodes)
        => new JObject
        {
            ["schema_version"] = 1,
            ["authority"] = "AuthoredConfigAssets",
            ["source_pins"] = new JObject { [pinPath] = pinSha },
            ["nodes"] = new JArray(nodes)
        };

    static bool Rejected(Action action)
    {
        try { action(); return false; }
        catch (Exception) { return true; }
    }

    static bool RejectedWith(Action action, string messageFragment)
    {
        try { action(); return false; }
        catch (Exception error)
        {
            for (Exception current = error; current != null; current = current.InnerException)
                if (current.Message.IndexOf(messageFragment, StringComparison.OrdinalIgnoreCase) >= 0) return true;
            return false;
        }
    }

    static void Need(bool value, string message)
    {
        if (!value) throw new InvalidOperationException(message);
    }

    static int Main(string[] args)
    {
        if (args.Length != 1) throw new ArgumentException("A fresh scratch directory is required");
        string root = Path.GetFullPath(args[0]);
        Directory.CreateDirectory(root);
        var shared = Node("bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb", nameof(ProbeConfig), "Assets/Shared.asset", "", new JObject());
        var owner = Node("aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa", nameof(ProbeConfig), "Assets/Owner.asset", "",
            new JObject
            {
                ["first"] = new JObject { ["fileID"] = 1, ["guid"] = "bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb", ["type"] = 2 },
                ["second"] = new JObject { ["fileID"] = 1, ["guid"] = "bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb", ["type"] = 2 }
            });
        // Use distinct serialized source paths for two graph nodes.
        owner["source_path"] = "Assets/Owner.asset";
        string ownerPath = WritePinnedFile(root, "Assets/Owner.asset", new byte[] { 4, 5, 6 });
        string ownerSha = Hash(File.ReadAllBytes(ownerPath));
        owner["source_sha256"] = ownerSha;
        shared["source_path"] = "Assets/Shared.asset";
        string sharedPath = WritePinnedFile(root, "Assets/Shared.asset", new byte[] { 7, 8, 9 });
        string sharedSha = Hash(File.ReadAllBytes(sharedPath));
        shared["source_sha256"] = sharedSha;
        var sharedGraph = new JObject
        {
            ["schema_version"] = 1,
            ["authority"] = "AuthoredConfigAssets",
            ["source_pins"] = new JObject { [ownerPath] = ownerSha, [sharedPath] = sharedSha },
            ["nodes"] = new JArray(owner, shared)
        };
        using (var materializer = new AuthoredSourceConfigGraphMaterializer(sharedGraph, new[] { nameof(ProbeConfig) }))
        {
            var result = materializer.ByPath<ProbeConfig>("Assets/Owner.asset");
            Need(result.first != null && ReferenceEquals(result.first, result.second), "Shared acyclic reference must preserve object identity");
        }

        string directPath = WritePinnedFile(root, "Assets/Cycle.asset", new byte[] { 14, 15 });
        string directSha = Hash(File.ReadAllBytes(directPath));
        var directCycle = Node("cccccccccccccccccccccccccccccccc", nameof(ProbeConfig), "Assets/Cycle.asset", directSha,
            new JObject { ["next"] = new JObject { ["fileID"] = 1, ["guid"] = "cccccccccccccccccccccccccccccccc", ["type"] = 2 } });
        Need(RejectedWith(() => new AuthoredSourceConfigGraphMaterializer(Graph(directPath, directSha, directCycle), new[] { nameof(ProbeConfig) })
            .ByPath<ProbeConfig>("Assets/Cycle.asset"), "cycle"), "Direct source reference cycle must be rejected");

        string cycleAPath = WritePinnedFile(root, "Assets/CycleA.asset", new byte[] { 16, 17 });
        string cycleBPath = WritePinnedFile(root, "Assets/CycleB.asset", new byte[] { 18, 19 });
        string cycleASha = Hash(File.ReadAllBytes(cycleAPath));
        string cycleBSha = Hash(File.ReadAllBytes(cycleBPath));
        var cycleA = Node("dddddddddddddddddddddddddddddddd", nameof(ProbeConfig), "Assets/CycleA.asset", cycleASha,
            new JObject { ["next"] = new JObject { ["fileID"] = 1, ["guid"] = "eeeeeeeeeeeeeeeeeeeeeeeeeeeeeeee", ["type"] = 2 } });
        var cycleB = Node("eeeeeeeeeeeeeeeeeeeeeeeeeeeeeeee", nameof(ProbeConfig), "Assets/CycleB.asset", cycleBSha,
            new JObject { ["next"] = new JObject { ["fileID"] = 1, ["guid"] = "dddddddddddddddddddddddddddddddd", ["type"] = 2 } });
        var cycleGraph = new JObject
        {
            ["schema_version"] = 1,
            ["authority"] = "AuthoredConfigAssets",
            ["source_pins"] = new JObject { [cycleAPath] = cycleASha, [cycleBPath] = cycleBSha },
            ["nodes"] = new JArray(cycleA, cycleB)
        };
        Need(RejectedWith(() => new AuthoredSourceConfigGraphMaterializer(cycleGraph, new[] { nameof(ProbeConfig) })
            .ByPath<ProbeConfig>("Assets/CycleA.asset"), "cycle"), "Indirect source reference cycle must be rejected");

        string mismatchPath = WritePinnedFile(root, "Assets/Pinned.asset", new byte[] { 10, 11 });
        string mismatchSha = Hash(File.ReadAllBytes(mismatchPath));
        var mismatch = Node("ffffffffffffffffffffffffffffffff", nameof(ProbeConfig), "Assets/Other.asset", mismatchSha, new JObject());
        Need(RejectedWith(() => new AuthoredSourceConfigGraphMaterializer(Graph(mismatchPath, mismatchSha, mismatch), new[] { nameof(ProbeConfig) })
            .ByPath<ProbeConfig>("Assets/Other.asset"), "join"), "Node source path must join to its exact source pin");

        string target = WritePinnedFile(root, "Assets/GridTarget.asset", new byte[] { 12, 13 });
        string targetSha = Hash(File.ReadAllBytes(target));
        string link = Path.Combine(root, "Assets", "Grid.asset");
        File.CreateSymbolicLink(link, target);
        var blocked = new JArray();
        for (int x = 0; x < 4097; x++) blocked.Add(new JObject { ["x"] = x, ["y"] = 0 });
        var grid = Node("11111111111111111111111111111111", nameof(GridAuthoringSceneConfigAsset), "Assets/Grid.asset", targetSha,
            new JObject { ["width"] = 4097, ["height"] = 1, ["blockedCells"] = blocked });
        Need(Rejected(() => new AuthoredSourceConfigGraphMaterializer(Graph(link, targetSha, grid), new[] { nameof(GridAuthoringSceneConfigAsset) })
            .ByPath<GridAuthoringSceneConfigAsset>("Assets/Grid.asset")), "Symbolic-link source pin must be rejected");

        string armyPath = WritePinnedFile(root, "Assets/ArmyProfile.asset", new byte[] { 21, 22, 23 });
        string armySha = Hash(File.ReadAllBytes(armyPath));
        var armyFields = new JObject
        {
            ["profileId"] = "C",
            ["kind"] = (int)SkirmishArmyProfileId.CombinedArms,
            ["allowedRoleIds"] = new JArray("Rifle", "Tank"),
            ["excludedRoleIds"] = new JArray(),
            ["contentVersion"] = 1
        };
        var army = Node("22222222222222222222222222222222", nameof(SkirmishArmyProfileConfig),
            "Assets/ArmyProfile.asset", armySha, armyFields);
        using (var materializer = new AuthoredSourceConfigGraphMaterializer(Graph(armyPath, armySha, army),
            new[] { nameof(SkirmishArmyProfileConfig) }))
        {
            var profile = materializer.ByPath<SkirmishArmyProfileConfig>("Assets/ArmyProfile.asset");
            Need(profile.ProfileId == "C" && profile.Kind == SkirmishArmyProfileId.CombinedArms &&
                profile.AllowedRoleIds.SequenceEqual(new[] { "Rifle", "Tank" }) &&
                profile.ExcludedRoleIds.Length == 0 && profile.ContentVersion == 1,
                "Private Unity serialized army-profile fields must materialize exactly");
        }
        armyFields["nonSerializedCache"] = "forbidden";
        var invalidArmy = Node("33333333333333333333333333333333", nameof(SkirmishArmyProfileConfig),
            "Assets/ArmyProfile.asset", armySha, armyFields);
        Need(RejectedWith(() => new AuthoredSourceConfigGraphMaterializer(Graph(armyPath, armySha, invalidArmy),
            new[] { nameof(SkirmishArmyProfileConfig) }).ByPath<SkirmishArmyProfileConfig>("Assets/ArmyProfile.asset"),
            "nonserialized source config field"), "Private unannotated army-profile field must remain rejected");

        Console.WriteLine("[SharedSourceConfigMaterializerBehavior] Passed shared-reference, cycles, pin-path, symlink, serialized private fields, and unannotated-field rejection");
        return 0;
    }
}
