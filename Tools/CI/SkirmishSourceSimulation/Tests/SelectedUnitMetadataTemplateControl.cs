using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using Game.Configs;
using Game.Runtime;
using Newtonsoft.Json.Linq;
using Unity.Entities;

// Isolated import control: rehydrates a fixture setup hash, never a mission world.
internal static class SelectedUnitMetadataTemplateControl
{
    static void Need(bool value,string message){if(!value)throw new InvalidDataException(message);}
    static SkirmishResolvedSetup FixtureSetup(string path)
    {
        var doc=JObject.Parse(File.ReadAllText(path));
        var entry=((JArray)doc["result"]["compiler"]).OfType<JObject>().Single(x=>(string)x["size"]=="Standard");
        var row=(JObject)entry["compiled_setup"].DeepClone();string fingerprint=(string)row["EnemyPolicy"]?["Fingerprint"];
        row["EnemyPolicy"]=JValue.CreateNull();var setup=row.ToObject<SkirmishResolvedSetup>();
        if(!String.IsNullOrEmpty(fingerprint))
        {
            var policy=(SkirmishEnemyPolicySnapshot)System.Runtime.Serialization.FormatterServices.GetUninitializedObject(typeof(SkirmishEnemyPolicySnapshot));
            var field=typeof(SkirmishEnemyPolicySnapshot).GetField("<Fingerprint>k__BackingField",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic);
            Need(field!=null,"Fixture fingerprint field required");field.SetValue(policy,fingerprint);setup.EnemyPolicy=policy;
        }
        return setup;
    }
    static int Main(string[] args)
    {
        if(args.Length!=7)throw new ArgumentException("Projection, metadata, receipt, log, identities, setup receipt and repository required");
        string projectionPath=Path.GetFullPath(args[0]),metadataPath=Path.GetFullPath(args[1]),receiptPath=Path.GetFullPath(args[2]);
        string logPath=Path.GetFullPath(args[3]),identityPath=Path.GetFullPath(args[4]),root=Path.GetFullPath(args[6]);
        var setup=FixtureSetup(Path.GetFullPath(args[5]));
        string metadataHash=AutonomousWorld.Hash(File.ReadAllBytes(metadataPath));
        var receipt=JObject.Parse(File.ReadAllText(receiptPath));
        Need((string)receipt["identity_sha256"]==AutonomousWorld.Hash(File.ReadAllBytes(identityPath)),"Identity readback hash differs");
        string marker=(string)receipt["identity_marker"];
        Need(!String.IsNullOrEmpty(marker)&&File.ReadAllText(logPath).Split(new[]{marker},StringSplitOptions.None).Length==2,"Independent identity marker required");
        var authority=SourceAuthoredUnitMetadataAuthority.FromCapture(setup,metadataPath,metadataHash,receiptPath,AutonomousWorld.Hash(File.ReadAllBytes(receiptPath)),logPath,root);
        var projection=JObject.Parse(File.ReadAllText(projectionPath));var metadata=JObject.Parse(File.ReadAllText(metadataPath));
        var keys=((JArray)metadata["units"]).Select(row=>(string)row["key"]).ToArray();
        var capturedKeys=((JArray)projection["requested_prefab_keys"]).Select(x=>(string)x).ToArray();
        Need(keys.Length==22 && capturedKeys.Length>=keys.Length && keys.SequenceEqual(capturedKeys.Take(keys.Length)),"Exact configured template order required");
        var preflight=GeneratedReferenceTemplateImporter.Preflight(projection,Path.GetDirectoryName(projectionPath),keys);
        Need((bool?)preflight["world_allocated"]==false,"Preflight allocated a world");
        int next=500000;var em=new EntityManager { LogicalAllocatorAdmitted=true };
        em.AllocateLogicalIdentity=()=>new Entity { Index=next++,Version=1 };
        var imported=GeneratedReferenceTemplateImporter.Import(em,projection,Path.GetDirectoryName(projectionPath),keys);
        Need(imported.Prefabs.Count==capturedKeys.Length && imported.OmittedPresentationComponents==0,"Actual selected template graph incomplete");
        var readback=JObject.Parse(File.ReadAllText(identityPath));
        Need((string)readback["metadata_sha256"]==metadataHash && (bool?)readback["runtime_templates_verified"]==false && (bool?)readback["mission_ready"]==false,"Asset identity scope differs");
        var assets=new Dictionary<string,SourceUnitPrefab>(StringComparer.Ordinal);var identities=new Dictionary<string,JObject>(StringComparer.Ordinal);
        foreach(JObject row in (JArray)readback["identities"])
        {
            string key=(string)row["key"],path=(string)row["path"],guid=(string)row["guid"];long local=(long)row["local_id"];
            Need(imported.Prefabs.TryGetValue(key,out var entity)&&em.Exists(entity),"Actual source template missing: "+key);
            assets.Add(key,new SourceUnitPrefab(key,entity,path,guid,local));
            identities.Add(key,new JObject { ["codec"]="source-asset-reference-v1",["type"]="UnityEngine.GameObject",["key"]=key,["path"]=path,["guid"]=guid,["local_id"]=local });
        }
        var loaded=GeneratedUnitProductionMetadata.Load(metadataPath,metadataHash,setup.SetupHash,setup.Seed,root,assets,imported.Prefabs,identities,sourceAuthority:authority);
        Need(loaded.Ordered.Count==22 && loaded.SourceAuthoredDomainQualified && !loaded.FactoryDomainQualified && loaded.Ordered.All(row=>row.HasActualTemplate),"All source templates and metadata required");
        foreach(var row in loaded.Ordered)Need(ReferenceEquals(loaded.RequireDeliverable(row.Prefab),row),"Actual template metadata mismatch");
        var missing=new Dictionary<string,JObject>(identities,StringComparer.Ordinal);missing.Remove(keys[0]);bool rejected=false;
        try{GeneratedUnitProductionMetadata.Load(metadataPath,metadataHash,setup.SetupHash,setup.Seed,root,assets,imported.Prefabs,missing,sourceAuthority:authority);}catch(InvalidDataException){rejected=true;}
        Need(rejected,"Missing independent identity must reject");
        var wrong=new Dictionary<string,Entity>(imported.Prefabs,StringComparer.Ordinal);wrong[keys[0]]=wrong[keys[1]];bool wrongTemplate=false;
        try{GeneratedUnitProductionMetadata.Load(metadataPath,metadataHash,setup.SetupHash,setup.Seed,root,assets,wrong,identities,sourceAuthority:authority);}catch(InvalidDataException){wrongTemplate=true;}
        Need(wrongTemplate,"Wrong source template must reject");
        Console.WriteLine(new JObject { ["status"]="JoinedDiagnosticOnly",["unit_rows"]=loaded.Ordered.Count,["source_templates"]=imported.Prefabs.Count,
            ["source_entities"]=imported.ImportedEntities,["missing_identity_rejected"]=rejected,["wrong_template_rejected"]=wrongTemplate,
            ["source_authored_domain_qualified"]=loaded.SourceAuthoredDomainQualified,["factory_domain_qualified"]=loaded.FactoryDomainQualified,["native_object_values_resolved"]=false,["mission_ready"]=false }.ToString(Newtonsoft.Json.Formatting.None));
        return 0;
    }
}
