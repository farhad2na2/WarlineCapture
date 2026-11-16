using System;
using System.IO;
using Game.Components;
using Game.Composition;
using Game.Configs;
using Game.Runtime;
using Game.Skirmish.Contracts;
using Newtonsoft.Json.Linq;
using Unity.Entities;

internal static class S004OriginalLaunchOwnerControl
{
    private static void Main(string[] args)
    {
        if(args.Length!=3)throw new ArgumentException("setup, hash, and source graph required");
        string root=Environment.CurrentDirectory;
        string matrix=Path.Combine(root,"Assets/Game/Resources/SkirmishExpansion/InitialSetupMatrix.csv");
        string original=Path.Combine(root,"T Logs/SkirmishProduction/s004-current-standard-setup-20261104-T1/original-compiler-result.json");
        string replay=Path.Combine(root,"T Logs/SkirmishProduction/s004-current-layout-replay-20261104-T1/replayed-layout.json");
        if(S004StandardSetupExportSourceBinding.Main(new[]{args[2],matrix,"104733",original,replay,"1130726651"})!=0)throw new Exception("Source layout replay failed");
        var setup=SourceS004CompiledSetupInput.Read(args[0],args[1]);
        var graph=JObject.Parse(File.ReadAllText(args[2]));
        var definition=new SourceConfigMaterializer(graph).ByPath<SkirmishScenarioDefinitionConfig>((string)graph["definition_path"]);
        var payload=new SkirmishLaunchPayload {SessionId="s004-source-launch-fixture",CatalogId=setup.CatalogId,
            DefinitionId=setup.DefinitionId,ContentVersion=setup.ContentVersion,AllConfigHashes=setup.SetupHash.ToString("X8"),
            MapId=setup.OperationMapId,LayoutId=setup.LayoutId,DifficultyId=setup.DifficultyId,SizeId=setup.SizeId,Seed=setup.Seed};
        var em=new EntityManager();int next=1;em.LogicalAllocatorAdmitted=true;em.AllocateLogicalIdentity=()=>new Entity{Index=next++,Version=1};
        payload.Seed++;if(SkirmishExpandedLaunchProjection.TryQueue(em,payload,setup,null,definition))throw new Exception("Wrong seed accepted");payload.Seed--;
        bool registryRejected=false;try{SkirmishExpandedLaunchProjection.TryQueue(em,payload,setup,new object(),definition);}catch(NotSupportedException){registryRejected=true;}
        if(!registryRejected)throw new Exception("Managed visual registry was accepted");
        using(var query=em.CreateEntityQuery(typeof(SkirmishExpandedSessionComponent)))if(query.CalculateEntityCount()!=0)throw new Exception("Rejected input created a session");
        if(!SkirmishExpandedLaunchProjection.TryQueue(em,payload,setup,null,definition))throw new Exception("Actual source launch owner rejected setup");
        using var sessions=em.CreateEntityQuery(typeof(SkirmishExpandedSessionComponent));
        if(sessions.CalculateEntityCount()!=1)throw new Exception("Expected exactly one queued session");
        var entity=sessions.GetSingletonEntity();var session=em.GetComponentData<SkirmishExpandedSessionComponent>(entity);
        var resolved=em.GetComponentData<SkirmishResolvedSetupComponent>(entity);
        if(session.Phase!=SkirmishSessionPhase.Queued||session.ObjectiveKind!=setup.ObjectiveKind||resolved.SetupHash!=setup.SetupHash||resolved.DeadlineSeconds!=setup.DeadlineSeconds)
            throw new Exception("Original launch projection did not preserve compiled setup");
        if(SkirmishExpandedLaunchProjection.TryQueue(em,payload,setup,null,definition))throw new Exception("Duplicate launch accepted");
        Console.WriteLine("[S004OriginalLaunchOwnerControl] result=Passed wrongSeed=Rejected visualRegistry=Rejected duplicate=Rejected objective="+session.ObjectiveKind+" nativeInput=0 mission=0 readiness=0");
    }
}
