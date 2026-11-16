using System;
using System.IO;
using System.Linq;
using Game.Components;
using Game.Runtime;
using Newtonsoft.Json.Linq;
using Unity.Entities;

internal static class S004TankCaptureImportControl
{
    static void Need(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
    static int Main(string[] args)
    {
        try
        {
            var source = JObject.Parse(File.ReadAllText(args[0]));
            var keys = ((JArray)source["requested_prefab_keys"]).Values<string>().ToArray();
            const string tank = "Unit_Veh_Tank_USA";
            var directory = Path.GetDirectoryName(Path.GetFullPath(args[0]));
            var preflight = GeneratedReferenceTemplateImporter.Preflight(source, directory, keys);
            Need((int?)preflight["roots"] == 12 && (int?)preflight["entities"] == 352 &&
                 (int?)preflight["unsupported_unconsumed_values"] == 14 &&
                 (int?)preflight["entity_identities_allocated"] == 0, "Scoped source domain changed");
            int allocated = 0;
            var rejectManager = new EntityManager { LogicalAllocatorAdmitted = true };
            rejectManager.AllocateLogicalIdentity = () => new Entity { Index = 700000 + ++allocated, Version = 1 };
            var bad = (JObject)source.DeepClone();
            bad["unsupported_values"][0]["type"] = "Game.Components.SimulationValue";
            bool rejected = false;
            try { GeneratedReferenceTemplateImporter.Import(rejectManager, bad, directory, keys); }
            catch (InvalidOperationException) { rejected = true; }
            Need(rejected && allocated == 0, "Unrecognized gameplay omission must reject before allocation");
            int next = 500000;
            var manager = new EntityManager { LogicalAllocatorAdmitted = true };
            manager.AllocateLogicalIdentity = () => new Entity { Index = next++, Version = 1 };
            var imported = GeneratedReferenceTemplateImporter.Import(manager, source, directory, keys);
            Need(ReferenceEquals(imported.Manager, manager) && imported.Prefabs.Count == 12 && imported.ImportedEntities == 352,
                "Actual import domain changed");
            var entity = imported.Prefabs[tank];
            Need(manager.HasComponent<Prefab>(entity) && manager.GetComponentData<UnitSourcePrefabKey>(entity).Value.ToString() == tank,
                "Tank root identity missing");
            var health = manager.GetComponentData<UnitHealth>(entity);
            var combat = manager.GetComponentData<UnitCombat>(entity);
            var attack = manager.GetComponentData<UnitAttack>(entity);
            Need(health.Current == 1250 && health.Max == 1250 && combat.CanAttack == 1 && combat.AggroRangeCells == 13 &&
                 attack.Damage == 180 && attack.Range == 120f, "Tank gameplay values changed");
            var entities = imported.SourceToLogical.Values.ToArray();
            foreach (var value in entities) manager.DestroyEntity(value);
            Need(entities.All(value => !manager.Exists(value)), "Disposable import cleanup failed");
            Console.WriteLine("[S004TankCaptureImportControl] result=Passed roots=12 entities=352 tank=1 gameplay=Passed negative=Passed disposable=1 readiness=0");
            return 0;
        }
        catch (Exception error) { Console.Error.WriteLine(error); return 1; }
    }
}
