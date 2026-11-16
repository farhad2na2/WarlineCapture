using System;
using System.IO;
using System.Linq;
using Game.Components;
using Game.Runtime;
using Newtonsoft.Json.Linq;
using Unity.Entities;

internal static class S004FourPrefabImportControl
{
    static void Need(bool ok, string message) { if (!ok) throw new InvalidOperationException(message); }
    static int Main(string[] args)
    {
        try
        {
            var source = JObject.Parse(File.ReadAllText(args[0]));
            string[] keys = ((JArray)source["requested_prefab_keys"]).Values<string>().ToArray();
            var directory = Path.GetDirectoryName(Path.GetFullPath(args[0]));
            var preflight = GeneratedReferenceTemplateImporter.Preflight(source, directory, keys);
            Need((int?)preflight["roots"] == 4 && (int?)preflight["entities"] == 214 &&
                 (int?)preflight["unsupported_unconsumed_values"] == 9 && (int?)preflight["entity_identities_allocated"] == 0,
                 "Actual four-root preflight changed");
            var bad = (JObject)source.DeepClone(); bad["unsupported_values"][0]["type"] = "Game.Components.SimulationValue";
            int rejectedAllocations = 0;
            var rejectedManager = new EntityManager { LogicalAllocatorAdmitted = true };
            rejectedManager.AllocateLogicalIdentity = () => new Entity { Index = 700000 + ++rejectedAllocations, Version = 1 };
            bool rejected = false;
            try { GeneratedReferenceTemplateImporter.Import(rejectedManager, bad, directory, keys); }
            catch (InvalidOperationException) { rejected = true; }
            Need(rejected && rejectedAllocations == 0, "Unknown gameplay omissions must reject before allocation");
            int next = 500000;
            var manager = new EntityManager { LogicalAllocatorAdmitted = true };
            manager.AllocateLogicalIdentity = () => new Entity { Index = next++, Version = 1 };
            var imported = GeneratedReferenceTemplateImporter.Import(manager, source, directory, keys);
            Need(imported.Prefabs.Count == 4 && imported.ImportedEntities == 214 && ReferenceEquals(imported.Manager, manager), "Actual import domain changed");
            int[] health = { 900, 1250, 560, 220 };
            for (int i = 0; i < keys.Length; i++)
            {
                var entity = imported.Prefabs[keys[i]];
                Need(manager.HasComponent<Prefab>(entity) && manager.GetComponentData<UnitSourcePrefabKey>(entity).Value.ToString() == keys[i], "Source prefab key mismatch");
                var hp = manager.GetComponentData<UnitHealth>(entity);
                Need(hp.Current == health[i] && hp.Max == health[i], "Source health mismatch: " + keys[i]);
            }
            var launcher = manager.GetComponentData<GroundMissileLauncherComponent>(imported.Prefabs["Unit_Veh_Missle_Launcher_Ground"]);
            Need(launcher.Damage == 650 && launcher.MinRange == 35f && launcher.MaxRange == 600f && launcher.DamageRadius == 8f, "Ground missile data mismatch");
            Need(manager.GetComponentData<UnitTransportCapacity>(imported.Prefabs["Unit_Veh_APC_Heavy"]).SoldierCapacity == 10 &&
                 manager.GetComponentData<UnitTransportCapacity>(imported.Prefabs["Unit_Veh_Truck_Canopy"]).SoldierCapacity == 10, "Transport capacity mismatch");
            var all = imported.SourceToLogical.Values.ToArray();
            foreach (var entity in all) manager.DestroyEntity(entity);
            Need(all.All(entity => !manager.Exists(entity)), "Disposable import cleanup failed");
            Console.WriteLine("[S004FourPrefabImportControl] result=Passed roots=4 entities=214 groundMissile=Passed transport=Passed negative=Passed disposable=1 readiness=0");
            return 0;
        }
        catch (Exception error) { Console.Error.WriteLine(error); return 1; }
    }
}
