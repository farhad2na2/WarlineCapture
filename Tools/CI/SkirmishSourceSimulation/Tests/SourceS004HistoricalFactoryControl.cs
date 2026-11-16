using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Game.Runtime;
using Newtonsoft.Json.Linq;
using Unity.Entities;

// Source-only diagnostic using explicitly historical asset captures. This does
// not assert that their native source closure matches the current project.
internal static class SourceS004HistoricalFactoryControl
{
    static JObject Read(string path, string hash)
        => JObject.Parse(System.Text.Encoding.UTF8.GetString(AutonomousWorld.PinnedFile(path, hash, 16 * 1024 * 1024)));
    static int Main(string[] args)
    {
        if (args.Length != 8) throw new ArgumentException("setup/hash, two prefab captures/hashes and factory/hash required");
        var setup = SourceS004CompiledSetupInput.Read(args[0], args[1]);
        int next = 600000; var em = new EntityManager { LogicalAllocatorAdmitted = true };
        em.AllocateLogicalIdentity = () => new Entity { Index = next++, Version = 1 };
        var prefabs = new Dictionary<string, Entity>(StringComparer.Ordinal);
        for (int i = 2; i <= 4; i += 2)
        {
            var source = Read(args[i], args[i + 1]);
            var keys = ((JArray)source["requested_prefab_keys"]).Values<string>().ToArray();
            var imported = GeneratedReferenceTemplateImporter.Import(em, source, Path.GetDirectoryName(args[i]), keys);
            foreach (var item in imported.Prefabs) if (!prefabs.TryAdd(item.Key, item.Value))
                throw new InvalidDataException("Duplicate historical prefab key");
        }
        var definitions = GeneratedBuildingDefinitions.LoadS004Source(args[6], args[7], prefabs, setup, args[0], args[1]);
        if (definitions.Definitions.Count != 9 || definitions.Definitions.Any(x => x.Prefab == null || !x.SourceMetadataComplete))
            throw new InvalidDataException("Historical source factory definitions incomplete");
        Console.WriteLine("[SourceS004HistoricalFactory] result=Passed definitions=9 historical=1 nativeParity=0 gameplay=0 readiness=0");
        return 0;
    }
}
