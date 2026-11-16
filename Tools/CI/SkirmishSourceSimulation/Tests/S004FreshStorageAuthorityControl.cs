using System;
using Game.Configs;
using Game.Runtime;

internal static class S004FreshStorageAuthorityControl
{
    private static void Main(string[] args)
    {
        if(args.Length!=4)throw new ArgumentException("Pinned setup and bundle paths/hashes required");
        string root=Environment.CurrentDirectory;
        string graph=System.IO.Path.Combine(root,"T Logs/SkirmishProduction/s004-current-physical-input-projection-20261104-T1/source-config-graph-with-cityedge-physical-inputs.json");
        string matrix=System.IO.Path.Combine(root,"Assets/Game/Resources/SkirmishExpansion/InitialSetupMatrix.csv");
        string original=System.IO.Path.Combine(root,"T Logs/SkirmishProduction/s004-current-standard-setup-20261104-T1/original-compiler-result.json");
        string replay=System.IO.Path.Combine(root,"T Logs/SkirmishProduction/s004-current-layout-replay-20261104-T1/replayed-layout.json");
        if(S004StandardSetupExportSourceBinding.Main(new[]{graph,matrix,"104733",original,replay,"1130726651"})!=0)
            throw new Exception("Exact original S004 replay bootstrap failed");
        var setup=SourceS004CompiledSetupInput.Read(args[0],args[1]);
        var authority=FreshSourceStorageLifetimeAuthority.FromS004SourceDiagnostic(args[2],args[3],setup,out SkirmishScenarioDefinitionConfig definition);
        bool wrongHash=false;try{FreshSourceStorageLifetimeAuthority.FromS004SourceDiagnostic(args[2],new string('0',64),setup,out _);}catch(NotSupportedException){wrongHash=true;}
        int oldSeed=setup.Seed;setup.Seed=oldSeed+1;bool wrongSetup=false;
        try{FreshSourceStorageLifetimeAuthority.FromS004SourceDiagnostic(args[2],args[3],setup,out _);}catch(NotSupportedException){wrongSetup=true;}finally{setup.Seed=oldSeed;}
        bool premature=false;try{authority.RequireClaimedActual(definition,setup,args[3]);}catch(NotSupportedException){premature=true;}
        authority.Claim();authority.RequireClaimedActual(definition,setup,args[3]);
        bool repeat=false;try{authority.Claim();}catch(NotSupportedException){repeat=true;}
        if(!wrongHash||!wrongSetup||!premature||!repeat)throw new Exception("S004 fresh source authority control failed");
        Console.WriteLine("[S004FreshStorageAuthorityControl] result=Passed originalCompiler=1 sourceOnly=1 wrongHash=Rejected wrongSetup=Rejected premature=Rejected repeat=Rejected world=0 readiness=0");
    }
}
