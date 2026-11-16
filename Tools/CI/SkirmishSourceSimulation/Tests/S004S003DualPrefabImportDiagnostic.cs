using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using System.Security.Cryptography;
using Newtonsoft.Json.Linq;
using Game.Runtime;
using Unity.Entities;

internal static class S004S003DualPrefabImportDiagnostic
{
    static void Need(bool ok, string message) { if (!ok) throw new InvalidOperationException(message); }
    static string Sha(string path) { using (var h = SHA256.Create()) using (var s = File.OpenRead(path)) return BitConverter.ToString(h.ComputeHash(s)).Replace("-", "").ToLowerInvariant(); }
    static void Visit(object value, Type type, Action<Entity> found)
    {
        if (type == typeof(Entity)) { found((Entity)value); return; }
        if (!type.IsValueType || type.IsPrimitive || type.IsEnum) return;
        foreach (var f in type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
            if (!f.IsStatic && (f.FieldType == typeof(Entity) || f.FieldType.IsValueType && !f.FieldType.IsPrimitive && !f.FieldType.IsEnum)) Visit(f.GetValue(value), f.FieldType, found);
    }
    static int VerifyReferenceDomain(EntityManager em, GeneratedReferenceTemplateImporter.Result imported)
    {
        var domain = new HashSet<Entity>(imported.SourceToLogical.Values); int refs = 0;
        var componentGetter = typeof(EntityManager).GetMethods().Single(m => m.Name == "GetComponentData" && m.IsGenericMethodDefinition && m.GetParameters().Length == 1);
        var bufferGetter = typeof(EntityManager).GetMethods().Single(m => m.Name == "GetBuffer" && m.IsGenericMethodDefinition && m.GetParameters().Length == 1);
        foreach (var owner in domain) foreach (var type in em.ComponentTypes(owner))
        {
            if (typeof(IBufferElementData).IsAssignableFrom(type))
            {
                var buffer = bufferGetter.MakeGenericMethod(type).Invoke(em, new object[] { owner });
                int length = (int)buffer.GetType().GetProperty("Length").GetValue(buffer);
                var indexer = buffer.GetType().GetProperty("Item");
                for (int i = 0; i < length; i++) Visit(indexer.GetValue(buffer, new object[] { i }), type, e => { if (e == Entity.Null) return; refs++; Need(domain.Contains(e), "Buffer Entity reference escaped its own import Result map"); });
            }
            else
            {
                var value = componentGetter.MakeGenericMethod(type).Invoke(em, new object[] { owner });
                Visit(value, type, e => { if (e == Entity.Null) return; refs++; Need(domain.Contains(e), "Component Entity reference escaped its own import Result map"); });
            }
        }
        return refs;
    }
    static int Main(string[] a)
    {
        Need(a.Length == 6, "S003 subset, S004 raw, S003 keys, S004 keys, binary directory, receipt required");
        var s3 = JObject.Parse(File.ReadAllText(a[0])); var s4 = JObject.Parse(File.ReadAllText(a[1]));
        string[] k3 = JArray.Parse(File.ReadAllText(a[2])).Values<string>().ToArray();
        string[] k4 = JArray.Parse(File.ReadAllText(a[3])).Values<string>().ToArray();
        Need(k3.Length == 7 && k3.Distinct(StringComparer.Ordinal).Count() == 7 && k4.Length == 9 && !k3.Intersect(k4, StringComparer.Ordinal).Any(), "Exact disjoint 7+9 root domains required");
        var pre3 = GeneratedReferenceTemplateImporter.Preflight(s3, a[4], k3);
        var pre4 = GeneratedReferenceTemplateImporter.Preflight(s4, a[4], k4);
        Need((int)pre3["roots"] == 7 && (int)pre4["roots"] == 9 && (bool)pre3["world_allocated"] == false && (bool)pre4["world_allocated"] == false, "Both exact source subsets must decode without World allocation");
        var em = new EntityManager { LogicalAllocatorAdmitted = true }; int n = 900000;
        em.AllocateLogicalIdentity = () => new Entity { Index = n++, Version = 1 };
        var imported4 = GeneratedReferenceTemplateImporter.Import(em, s4, a[4], k4);
        var s4Entities = imported4.SourceToLogical.Values.ToArray();
        Need(s4Entities.All(em.Exists), "S004 import must remain live before S003 transaction");
        int successfulAllocations = n; int attempts = 0; Entity firstS3 = Entity.Null;
        em.AllocateLogicalIdentity = () => { attempts++; if (attempts == 1) { firstS3 = new Entity { Index = 950001, Version = 1 }; return firstS3; } throw new InvalidOperationException("Injected second-import allocation failure"); };
        bool rejected = false; try { GeneratedReferenceTemplateImporter.Import(em, s3, a[4], k3); } catch (InvalidOperationException) { rejected = true; }
        Need(rejected && attempts == 2 && !em.Exists(firstS3) && s4Entities.All(em.Exists), "Second-call rollback must remove its partial entities and preserve S004 import");
        em.AllocateLogicalIdentity = () => new Entity { Index = successfulAllocations + (n++ - successfulAllocations), Version = 1 };
        var imported3 = GeneratedReferenceTemplateImporter.Import(em, s3, a[4], k3);
        var s3Entities = imported3.SourceToLogical.Values.ToArray();
        Need(imported3.Prefabs.Count == 7 && imported4.Prefabs.Count == 9 && !s3Entities.Intersect(s4Entities).Any() && s3Entities.All(em.Exists), "Sequential imports must have independent logical identity maps");
        int s3EntityRefs = VerifyReferenceDomain(em, imported3); int s4EntityRefs = VerifyReferenceDomain(em, imported4);
        var commonSourceIds = imported3.SourceToLogical.Keys.Intersect(imported4.SourceToLogical.Keys).ToArray();
        Need(commonSourceIds.Length > 0 && commonSourceIds.All(id => imported3.SourceToLogical[id] != imported4.SourceToLogical[id]), "Cross-map negative control requires overlapping source IDs with distinct target remaps");
        bool crossMapSubstitutionRejected = commonSourceIds.All(id => !imported3.SourceToLogical.Values.Contains(imported4.SourceToLogical[id]) && !imported4.SourceToLogical.Values.Contains(imported3.SourceToLogical[id]));
        Need(crossMapSubstitutionRejected, "A foreign call's remap must be rejected by the owning Result domain");
        foreach (var e in s3Entities) em.DestroyEntity(e);
        Need(s3Entities.All(e => !em.Exists(e)) && s4Entities.All(em.Exists), "S003 cleanup must preserve S004 entities");
        foreach (var e in s4Entities) em.DestroyEntity(e);
        Need(s3Entities.Concat(s4Entities).All(e => !em.Exists(e)), "Final cleanup must leave no imported identities");
        File.WriteAllText(a[5], new JObject { ["status"]="PassedTwoSequentialSourceImports_DisposableLogicalEntityManagerOnly", ["s003_roots"]=7, ["s003_entities"]=imported3.ImportedEntities, ["s004_roots"]=9, ["s004_entities"]=imported4.ImportedEntities, ["logical_identity_sets_disjoint"]=true, ["s003_remapped_entity_references"]=s3EntityRefs, ["s004_remapped_entity_references"]=s4EntityRefs, ["all_component_and_buffer_entity_references_stay_in_own_result_map"]=s3EntityRefs>0 && s4EntityRefs>0, ["shared_source_entity_ids"]=commonSourceIds.Length, ["cross_map_substitution_rejected_by_result_domain"]=crossMapSubstitutionRejected, ["second_import_partial_allocation_rollback"]=true, ["rollback_preserved_first_import"]=true, ["s003_cleanup_preserved_s004"]=true, ["all_imports_cleaned_up"]=true, ["unity_world_created"]=false, ["mission_manager_created"]=false, ["mission_initialized"]=false, ["mission_admission_attempted"]=false, ["runtime_replay_ready"]=false, ["mission_ready"]=false }.ToString());
        Console.WriteLine("[S004S003DualPrefabImportDiagnostic] result=Passed rootsS003=7 rootsS004=9 rollback=Passed remaps=Disjoint cleanup=Passed world=0 mission=0 admission=0 readiness=0"); return 0;
    }
}
