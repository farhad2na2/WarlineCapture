using System;
using System.IO;
using System.Linq;
using Game.Runtime;
using Newtonsoft.Json.Linq;
using Unity.Entities;

internal static class S003RefreshedPrefabImportControl
{
    static void Need(bool condition,string message) { if(!condition) throw new InvalidOperationException(message); }
    static int Main(string[] args)
    {
        try {
            var source=JObject.Parse(File.ReadAllText(args[0]));var keys=JArray.Parse(File.ReadAllText(args[1])).Values<string>().ToArray();
            string directory=Path.GetDirectoryName(Path.GetFullPath(args[0]));
            var preflight=GeneratedReferenceTemplateImporter.Preflight(source,directory,keys);
            Need((int?)preflight["roots"]==19&&(int?)preflight["entities"]==647&&(int?)preflight["unsupported_unconsumed_values"]==32&&(int?)preflight["entity_identities_allocated"]==0,"Actual preflight domain changed");
            var bad=(JObject)source.DeepClone();bad["unsupported_values"][0]["type"]="Game.Components.SimulationValue";int rejected=0;
            var badManager=new EntityManager{LogicalAllocatorAdmitted=true};badManager.AllocateLogicalIdentity=()=>{rejected++;return new Entity{Index=700000+rejected,Version=1};};
            bool caught=false;try{GeneratedReferenceTemplateImporter.Import(badManager,bad,directory,keys);}catch(InvalidOperationException){caught=true;}
            Need(caught&&rejected==0,"Unsupported gameplay value must reject before allocation");
            int foreignAllocations=0;var foreignManager=new EntityManager{LogicalAllocatorAdmitted=true};foreignManager.AllocateLogicalIdentity=()=>{foreignAllocations++;return new Entity{Index=710000+foreignAllocations,Version=1};};caught=false;
            try{GeneratedReferenceTemplateImporter.Import(foreignManager,source,directory,keys.Concat(new[]{"Unit_NotInS003Capture"}));}catch(InvalidOperationException){caught=true;}
            Need(caught&&foreignAllocations==0,"Foreign root must reject before allocation");
            string movementPath=Path.GetFullPath(args[2]);var movementDescriptor=new JObject{["path"]=movementPath,["sha256"]=AutonomousWorld.Hash(File.ReadAllBytes(movementPath))};
            var movement=SourceUnitMoveAuthoringSource.Load(movementDescriptor,source,"Unit_Chr_Soldier_Male_02_Alt_04",Directory.GetCurrentDirectory(),(path,digest,maxBytes)=>AutonomousWorld.PinnedFile(path,digest,maxBytes));
            var wrongCombat=movement.Combat;wrongCombat.ChaseBreakDistance+=1f;int mismatchAllocations=0;var mismatchManager=new EntityManager{LogicalAllocatorAdmitted=true};mismatchManager.AllocateLogicalIdentity=()=>{mismatchAllocations++;return new Entity{Index=720000+mismatchAllocations,Version=1};};caught=false;
            try{GeneratedReferenceTemplateImporter.Import(mismatchManager,source,directory,keys,"Unit_Chr_Soldier_Male_02_Alt_04",movement.Projection,wrongCombat,movement.Health,movement.Attack);}catch(InvalidOperationException){caught=true;}
            Need(caught&&mismatchAllocations==0,"Incorrect source combat projection must reject before allocation");
            int next=500000;var manager=new EntityManager{LogicalAllocatorAdmitted=true};manager.AllocateLogicalIdentity=()=>new Entity{Index=next++,Version=1};
            var imported=GeneratedReferenceTemplateImporter.Import(manager,source,directory,keys,"Unit_Chr_Soldier_Male_02_Alt_04",movement.Projection,movement.Combat,movement.Health,movement.Attack);
            Need(ReferenceEquals(imported.Manager,manager)&&imported.Prefabs.Count==19&&imported.ImportedEntities==647&&imported.OmittedPresentationComponents==26,"Actual importer result changed");
            var entities=imported.SourceToLogical.Values.ToArray();foreach(var entity in entities)manager.DestroyEntity(entity);Need(entities.All(e=>!manager.Exists(e)),"Disposable import cleanup failed");
            Console.WriteLine("[S003RefreshedPrefabImportControl] result=Passed roots=19 entities=647 unsupported=32 omitted=26 negatives=3 sourceProjection=Passed world=0 readiness=0");return 0;
        } catch(Exception error){Console.Error.WriteLine(error);return 1;}
    }
}
