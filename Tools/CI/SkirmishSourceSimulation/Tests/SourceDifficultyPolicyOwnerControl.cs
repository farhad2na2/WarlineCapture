using System;
using System.IO;
using Game.Configs;
using Game.Skirmish.Contracts;
using Newtonsoft.Json.Linq;

// Isolated policy compilation over real authored difficulty assets. This is
// not a full mission setup, gameplay run, placement check, or readiness gate.
internal static class SourceDifficultyPolicyOwnerControl
{
    static void Need(bool value,string label) { if(!value)throw new Exception(label); }
    static void Main(string[] args)
    {
        if(args.Length!=1)throw new ArgumentException("Pinned authored config graph required");
        var source=new SourceConfigMaterializer(JObject.Parse(File.ReadAllText(args[0])));
        var definition=source.ByPath<SkirmishScenarioDefinitionConfig>("Assets/Game/Configs/SkirmishExpansion/Scenarios/S005/SkirmishScenario_S005.asset");
        Need(definition.CatalogId=="S005" && !string.IsNullOrEmpty(definition.ContentHash),"actual source definition identity");
        var names=new[]{"Recruit","Regular","Veteran","Commander"};
        var difficulties=new[]{SkirmishDifficultyId.Recruit,SkirmishDifficultyId.Regular,SkirmishDifficultyId.Veteran,SkirmishDifficultyId.Commander};
        var responseMin=new[]{5f,3f,1.5f,.8f}; var responseMax=new[]{8f,5f,3f,2f};
        var scoutMin=new[]{45f,25f,15f,10f}; var scoutMax=new[]{60f,40f,25f,20f};
        var fieldOpening=new[]{90f,60f,45f,30f}; var establishedOpening=new[]{60f,40f,25f,15f};
        int cases=0;
        for(int i=0;i<names.Length;i++)
        {
            var difficulty=source.ByPath<SkirmishDifficultyConfig>("Assets/Game/Configs/SkirmishExpansion/Shared/SkirmishDifficulty_"+names[i]+".asset");
            Need(difficulty.Kind==difficulties[i],"authored difficulty identity");
            foreach(var readiness in new[]{SkirmishReadinessStage.Field,SkirmishReadinessStage.Established,SkirmishReadinessStage.FullArsenal})
            {
                var setup=new SkirmishResolvedSetup { CatalogId=definition.CatalogId,ContentHash=definition.ContentHash,
                    ObjectiveKind=SkirmishObjectiveKind.BaseAssault,DifficultyId=difficulties[i],Readiness=readiness,Seed=104731 };
                Need(SkirmishEnemyPolicyCompiler.TryCompile(setup,null,out var policy,difficulty),"original policy compiler");
                Need(policy.ResponseMinSeconds==responseMin[i] && policy.ResponseMaxSeconds==responseMax[i] &&
                    policy.ScoutMinSeconds==scoutMin[i] && policy.ScoutMaxSeconds==scoutMax[i] &&
                    policy.OpeningRestraintSeconds==(readiness==SkirmishReadinessStage.Field?fieldOpening[i]:establishedOpening[i]),"authored policy values");
                cases++;
                setup.DifficultyId=difficulties[(i+1)%difficulties.Length];
                Need(!SkirmishEnemyPolicyCompiler.TryCompile(setup,null,out _,difficulty),"mismatched difficulty asset must reject");
            }
        }
        Console.WriteLine("[SourceDifficultyPolicyOwnerControl] result=Passed authoredAssets=4 readinessCases="+cases+" mismatchedAssets=Rejected fixtureOnly=1 missionSetup=0 gameplay=0 readiness=0");
    }
}
