using System;
using System.IO;
using System.Collections.Generic;
using Game.Configs;
using Game.Runtime;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Unity.Entities;

internal static class SourceAuthoredUnitMetadataAuthorityControl
{
    private static void Need(bool value,string message) { if(!value)throw new Exception(message); }
    private static int Main(string[] args)
    {
        if(args.Length!=7)throw new ArgumentException("setup receipt, metadata, capture receipt, log, repository root, output directory, identity readback required");
        string setupPath=Path.GetFullPath(args[0]),metadataPath=Path.GetFullPath(args[1]),receiptPath=Path.GetFullPath(args[2]);
        string logPath=Path.GetFullPath(args[3]),root=Path.GetFullPath(args[4]),output=Path.GetFullPath(args[5]);
        var setupDoc=JObject.Parse(File.ReadAllText(setupPath));
        var matches=((JArray)setupDoc["result"]["compiler"]).SelectTokens("$[?(@.size == 'Standard')]");
        SkirmishResolvedSetup setup=null;int count=0;
        foreach(var match in matches)
        {
            // This isolated fixture rehydrates only the fingerprint needed by
            // ComputeHash; it is not a live policy or mission-state capture.
            var serialized=(JObject)match["compiled_setup"].DeepClone();
            string fingerprint=(string)serialized["EnemyPolicy"]?["Fingerprint"];
            serialized["EnemyPolicy"]=JValue.CreateNull();
            setup=serialized.ToObject<SkirmishResolvedSetup>();
            if(!String.IsNullOrEmpty(fingerprint))
            {
                var policy=(SkirmishEnemyPolicySnapshot)System.Runtime.Serialization.FormatterServices.GetUninitializedObject(typeof(SkirmishEnemyPolicySnapshot));
                var field=typeof(SkirmishEnemyPolicySnapshot).GetField("<Fingerprint>k__BackingField",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic);
                Need(field!=null,"Fixture fingerprint field required");field.SetValue(policy,fingerprint);setup.EnemyPolicy=policy;
            }
            count++;
        }
        Need(count==1 && setup!=null,"Unique actual compiled setup required");
        string metadataHash=AutonomousWorld.Hash(File.ReadAllBytes(metadataPath));
        string receiptHash=AutonomousWorld.Hash(File.ReadAllBytes(receiptPath));
        var authority=SourceAuthoredUnitMetadataAuthority.FromCapture(setup,metadataPath,metadataHash,receiptPath,receiptHash,logPath,root);
        Need(authority.UnitCount==22,"Actual captured count differs");
        var doc=JObject.Parse(File.ReadAllText(metadataPath));authority.Require(metadataPath,metadataHash,setup.SetupHash,setup.Seed,doc);
        var changed=(JObject)doc.DeepClone();changed["units"][0]["key"]="different";
        bool changedOrder=false;try{authority.Require(metadataPath,metadataHash,setup.SetupHash,setup.Seed,changed);}catch(InvalidDataException){changedOrder=true;}
        Need(changedOrder,"Changed ordered unit identity accepted");
        bool wrongHash=false;try{authority.Require(metadataPath,new string('0',64),setup.SetupHash,setup.Seed,doc);}catch(InvalidDataException){wrongHash=true;}
        Need(wrongHash,"Changed metadata hash accepted");
        int seed=setup.Seed;setup.Seed++;bool wrongSeed=false;
        try{SourceAuthoredUnitMetadataAuthority.FromCapture(setup,metadataPath,metadataHash,receiptPath,receiptHash,logPath,root);}catch(InvalidDataException){wrongSeed=true;}finally{setup.Seed=seed;}
        Need(wrongSeed,"Changed seed accepted");
        var receipt=JObject.Parse(File.ReadAllText(receiptPath));receipt["marker"]="[SkirmishAuthoredUnitProductionMetadata] result=Passed forged";
        string badReceipt=Path.Combine(output,"wrong-marker-receipt.json");File.WriteAllText(badReceipt,receipt.ToString(Formatting.Indented));
        bool wrongMarker=false;try{SourceAuthoredUnitMetadataAuthority.FromCapture(setup,metadataPath,metadataHash,badReceipt,AutonomousWorld.Hash(File.ReadAllBytes(badReceipt)),logPath,root);}catch(InvalidDataException){wrongMarker=true;}
        Need(wrongMarker,"Changed marker accepted");
        string identityPath=Path.GetFullPath(args[6]);
        var captureReceipt=JObject.Parse(File.ReadAllText(receiptPath));
        Need(AutonomousWorld.Hash(File.ReadAllBytes(identityPath))==(string)captureReceipt["identity_sha256"],"Exact identity readback required");
        string identityMarker=(string)captureReceipt["identity_marker"];
        Need(!String.IsNullOrEmpty(identityMarker) && File.ReadAllText(logPath).Split(new[]{identityMarker},StringSplitOptions.None).Length==2,"Identity marker required once");
        var readback=JObject.Parse(File.ReadAllText(identityPath));
        Need((string)readback["metadata_sha256"]==metadataHash && (bool?)readback["runtime_templates_verified"]==false &&
             (bool?)readback["mission_ready"]==false,"Identity readback scope differs");
        var checkedIdentities=authority.ReadIndependentIdentities(identityPath);
        Need(checkedIdentities.Count==22,"Checked independent identity domain differs");
        var changedReadback=(JObject)readback.DeepClone();changedReadback["identities"][0]["guid"]=new string('0',32);
        string wrongIdentityPath=Path.Combine(output,"wrong-identity-readback.json");File.WriteAllText(wrongIdentityPath,changedReadback.ToString(Formatting.Indented));
        bool wrongIdentityBytes=false;try{authority.ReadIndependentIdentities(wrongIdentityPath);}catch(InvalidOperationException){wrongIdentityBytes=true;}
        Need(wrongIdentityBytes,"Changed independent identity readback accepted");
        var assets=new Dictionary<string,SourceUnitPrefab>(StringComparer.Ordinal);
        var identities=new Dictionary<string,JObject>(StringComparer.Ordinal);
        foreach(JObject row in (JArray)readback["identities"])
        {
            string key=(string)row["key"],path=(string)row["path"],guid=(string)row["guid"];long local=(long)row["local_id"];
            assets.Add(key,SourceUnitPrefab.FromAssetIdentity(key,path,guid,local));
            identities.Add(key,new JObject { ["codec"]="source-asset-reference-v1",["type"]="UnityEngine.GameObject",
                ["key"]=key,["path"]=path,["guid"]=guid,["local_id"]=local });
            Need(checkedIdentities.TryGetValue(key,out var checkedIdentity) && JToken.DeepEquals(checkedIdentity,identities[key]),"Independent identity verification differs");
        }
        Need(assets.Count==22,"Actual 22 independent asset identities required");
        var templates=new Dictionary<string,Entity>(StringComparer.Ordinal);
        var loaded=GeneratedUnitProductionMetadata.Load(metadataPath,metadataHash,setup.SetupHash,setup.Seed,root,assets,templates,identities,sourceAuthority:authority);
        Need(loaded.Ordered.Count==22 && !loaded.FactoryDomainQualified,"Asset-only materialized domain required");
        bool missingTemplate=false;try{loaded.RequireDeliverable(loaded.Ordered[0].Prefab);}catch(NotSupportedException){missingTemplate=true;}
        Need(missingTemplate,"Missing runtime template must reject delivery");
        bool noAuthority=false;try{GeneratedUnitProductionMetadata.Load(metadataPath,metadataHash,setup.SetupHash,setup.Seed,root,assets,templates,identities);}catch(InvalidDataException){noAuthority=true;}
        Need(noAuthority,"Unqualified 22-unit input must reject");
        var missingIdentities=new Dictionary<string,JObject>(identities,StringComparer.Ordinal);missingIdentities.Remove(loaded.Ordered[0].Prefab.name);
        bool missingIdentity=false;try{GeneratedUnitProductionMetadata.Load(metadataPath,metadataHash,setup.SetupHash,setup.Seed,root,assets,templates,missingIdentities,sourceAuthority:authority);}catch(InvalidDataException){missingIdentity=true;}
        Need(missingIdentity,"Missing independent asset identity must reject");
        Console.WriteLine("[SourceAuthoredUnitMetadataAuthorityControl] result=Passed units=22 negatives=8 templates=0 scope=AssetOnly");return 0;
    }
}
