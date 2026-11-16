using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using Game.Runtime;
using Newtonsoft.Json.Linq;
using Unity.Entities;

internal static class S004CurrentTemplateDomainsControl
{
    static void Need(bool ok, string message) { if (!ok) throw new InvalidOperationException(message); }
    static void Visit(object value, Type type, Action<Entity> found)
    {
        if (type == typeof(Entity)) { found((Entity)value); return; }
        if (!type.IsValueType || type.IsPrimitive || type.IsEnum) return;
        foreach (var f in type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
            if (!f.IsStatic && (f.FieldType == typeof(Entity) || f.FieldType.IsValueType && !f.FieldType.IsPrimitive && !f.FieldType.IsEnum)) Visit(f.GetValue(value), f.FieldType, found);
    }
    static int CheckReferences(EntityManager manager, GeneratedReferenceTemplateImporter.Result result)
    {
        var domain = new HashSet<Entity>(result.SourceToLogical.Values); int references = 0;
        var component = typeof(EntityManager).GetMethods().Single(m => m.Name == "GetComponentData" && m.IsGenericMethodDefinition && m.GetParameters().Length == 1);
        var buffer = typeof(EntityManager).GetMethods().Single(m => m.Name == "GetBuffer" && m.IsGenericMethodDefinition && m.GetParameters().Length == 1);
        Action<Entity> check = entity => { if (entity == Entity.Null) return; references++; Need(domain.Contains(entity), "Entity reference escaped its capture domain"); };
        foreach (var entity in domain) foreach (var type in manager.ComponentTypes(entity))
        {
            if (typeof(IBufferElementData).IsAssignableFrom(type))
            {
                var values = buffer.MakeGenericMethod(type).Invoke(manager, new object[] { entity });
                int count = (int)values.GetType().GetProperty("Length").GetValue(values);
                var item = values.GetType().GetProperty("Item");
                for (int i = 0; i < count; i++) Visit(item.GetValue(values, new object[] { i }), type, check);
            }
            else Visit(component.MakeGenericMethod(type).Invoke(manager, new object[] { entity }), type, check);
        }
        return references;
    }
    static int Main(string[] args)
    {
        try
        {
            Need(args.Length == 2, "Two source captures required");
            var first = JObject.Parse(File.ReadAllText(args[0])); var second = JObject.Parse(File.ReadAllText(args[1]));
            var firstKeys = ((JArray)first["requested_prefab_keys"]).Values<string>().ToArray();
            var secondKeys = ((JArray)second["requested_prefab_keys"]).Values<string>().ToArray();
            Need(firstKeys.Length == 19 && secondKeys.Length == 4 && !firstKeys.Intersect(secondKeys, StringComparer.Ordinal).Any(), "Disjoint 19+4 keys required");
            string firstDir = Path.GetDirectoryName(Path.GetFullPath(args[0])), secondDir = Path.GetDirectoryName(Path.GetFullPath(args[1]));
            Need((int?)GeneratedReferenceTemplateImporter.Preflight(first, firstDir, firstKeys)["roots"] == 19 &&
                 (int?)GeneratedReferenceTemplateImporter.Preflight(second, secondDir, secondKeys)["roots"] == 4, "Source decoder rejected capture");
            int next = 900000; var manager = new EntityManager { LogicalAllocatorAdmitted = true };
            manager.AllocateLogicalIdentity = () => new Entity { Index = next++, Version = 1 };
            var importedFirst = GeneratedReferenceTemplateImporter.Import(manager, first, firstDir, firstKeys);
            var firstEntities = importedFirst.SourceToLogical.Values.ToArray();
            int attempts = 0; Entity partial = Entity.Null;
            manager.AllocateLogicalIdentity = () => { if (++attempts == 1) return partial = new Entity { Index = 990000, Version = 1 }; throw new InvalidOperationException("Injected second-import failure"); };
            bool rejected = false;
            try { GeneratedReferenceTemplateImporter.Import(manager, second, secondDir, secondKeys); } catch (InvalidOperationException) { rejected = true; }
            Need(rejected && attempts == 2 && !manager.Exists(partial) && firstEntities.All(manager.Exists), "Failed second import damaged first domain or leaked partial entity");
            manager.AllocateLogicalIdentity = () => new Entity { Index = next++, Version = 1 };
            var importedSecond = GeneratedReferenceTemplateImporter.Import(manager, second, secondDir, secondKeys);
            var secondEntities = importedSecond.SourceToLogical.Values.ToArray();
            Need(importedFirst.Prefabs.Count == 19 && importedSecond.Prefabs.Count == 4 && !firstEntities.Intersect(secondEntities).Any(), "Import identities overlap");
            var sharedIds = importedFirst.SourceToLogical.Keys.Intersect(importedSecond.SourceToLogical.Keys).ToArray();
            Need(sharedIds.Length > 0 && sharedIds.All(id => importedFirst.SourceToLogical[id] != importedSecond.SourceToLogical[id]), "Shared source identities require separate remaps");
            int firstReferences = CheckReferences(manager, importedFirst), secondReferences = CheckReferences(manager, importedSecond);
            Need(firstReferences > 0 && secondReferences > 0, "Actual reference domains were not exercised");
            foreach (var entity in secondEntities) manager.DestroyEntity(entity);
            Need(secondEntities.All(entity => !manager.Exists(entity)) && firstEntities.All(manager.Exists), "Second cleanup affected first domain");
            foreach (var entity in firstEntities) manager.DestroyEntity(entity);
            Need(firstEntities.All(entity => !manager.Exists(entity)), "First cleanup failed");
            Console.WriteLine("[S004CurrentTemplateDomainsControl] result=Passed roots=19+4 rollback=Passed references=Disjoint cleanup=Passed mission=0 admission=0 readiness=0");
            return 0;
        }
        catch (Exception error) { Console.Error.WriteLine(error); return 1; }
    }
}
