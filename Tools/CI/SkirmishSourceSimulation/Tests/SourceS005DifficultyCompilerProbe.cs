using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using Game.Configs;
using Game.Runtime;
using Game.Skirmish.Contracts;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

// Actual authored inputs and original compiler. This reports every rejection;
// it does not establish world startup, source fit, or mission acceptance.
internal static class SourceS005DifficultyCompilerProbe
{
    static void Main(string[] args)
    {
        if(args.Length!=5)throw new ArgumentException("graph, pinned matrix, matrix hash, historical native layout log and fresh output required");
        var graph=JObject.Parse(File.ReadAllText(args[0]));
        var source=new SourceConfigMaterializer(graph);
        var definition=source.ByPath<SkirmishScenarioDefinitionConfig>("Assets/Game/Configs/SkirmishExpansion/Scenarios/S005/SkirmishScenario_S005.asset");
        if(definition.CatalogId!="S005")throw new InvalidDataException("Exact authored S005 definition required");
        var catalog=source.ByPath<SkirmishExpansionCatalogConfig>((string)graph["catalog_path"]);
        if(catalog==null || !catalog.TryGetDefinition("S005",definition.ContentVersion,out var actualDefinition) ||
           !Object.ReferenceEquals(actualDefinition,definition))throw new InvalidDataException("Exact graph catalog identity required");
        UnityEngine.Resources.BindSourceResources(new Dictionary<string,object> { [SkirmishExpansionCatalogConfig.ResourceName]=catalog });
        // Replay only the already captured native bytes after the original
        // source replay's exact schema and bitwise value checks succeed.
        var binding=typeof(S005LayoutCaptureReplaySourceBinding); const BindingFlags flags=BindingFlags.Static|BindingFlags.NonPublic;
        var layoutType=typeof(SkirmishMeasuredLayoutBinder).GetNestedType("LayoutValues",BindingFlags.NonPublic);
        var typed=layoutType.GetMethod("From",BindingFlags.Public|BindingFlags.Static).Invoke(null,new object[]{definition.MapLayout});
        string raw=(string)binding.GetMethod("ExtractJson",flags).Invoke(null,new object[]{File.ReadAllText(args[3]),"authored asset="});
        object captured=JsonConvert.DeserializeObject(raw,layoutType);
        if(!(bool)binding.GetMethod("CompareSchema",flags).Invoke(null,new object[]{layoutType,JObject.Parse(raw)}) ||
           !(bool)binding.GetMethod("Equal",flags).Invoke(null,new object[]{typed,captured,layoutType}))
            throw new InvalidDataException("Historical layout capture does not exactly match current source values");
        binding.GetField("replayType",flags).SetValue(null,layoutType);
        binding.GetField("replayValues",flags).SetValue(null,captured);
        binding.GetField("replayJson",flags).SetValue(null,raw);
        binding.GetField("replayCalls",flags).SetValue(null,0);
        var rows=SourceExternalSetupMatrix.ReadVerified(File.ReadAllBytes(args[1]),args[2]);
        var manifest=new SkirmishContentManifest { RequiredFeatureIds=definition.RequiredFeatureIds };
        var output=new JArray();
        foreach(var size in new[]{SkirmishSizeId.Standard,SkirmishSizeId.War,SkirmishSizeId.LargeWar})
        foreach(var difficulty in new[]{SkirmishDifficultyId.Recruit,SkirmishDifficultyId.Regular,SkirmishDifficultyId.Veteran,SkirmishDifficultyId.Commander})
        {
            var row=SourceExternalSetupMatrix.Select(rows,"S005",size);
            bool passed=false; SkirmishResolvedSetup setup=null; List<SkirmishCompileReason> reasons=null; string error=null;
            try { passed=SkirmishSetupCompiler.TryCompile(definition,difficulty,size,1,manifest,rows,out setup,out reasons); }
            catch(Exception ex) { error=ex.ToString(); }
            bool inspected=false; SkirmishStaticAuthoringBudget budget=null; List<SkirmishCompileReason> budgetReasons=null; string budgetError=null;
            try { inspected=SkirmishSetupCompiler.TryInspectStaticAuthoringBudget(definition,difficulty,size,1,manifest,rows,out budget,out budgetReasons); }
            catch(Exception ex) { budgetError=ex.ToString(); }
            output.Add(new JObject { ["size"]=size.ToString(),["difficulty"]=difficulty.ToString(),["seed"]=1,
                ["matrix_player_infantry"]=row.PlayerInfantry,["matrix_enemy_infantry"]=row.EnemyInfantry,
                ["matrix_materials_each"]=row.MaterialsEach,["compiled"]=passed,
                ["setup_hash"]=setup==null?null:(JToken)setup.SetupHash,
                ["enemy_policy_present"]=setup==null?null:(JToken)(setup.EnemyPolicy!=null),
                ["policy_response_min"]=setup?.EnemyPolicy==null?null:(JToken)setup.EnemyPolicy.ResponseMinSeconds,
                ["policy_response_max"]=setup?.EnemyPolicy==null?null:(JToken)setup.EnemyPolicy.ResponseMaxSeconds,
                ["policy_scout_min"]=setup?.EnemyPolicy==null?null:(JToken)setup.EnemyPolicy.ScoutMinSeconds,
                ["policy_scout_max"]=setup?.EnemyPolicy==null?null:(JToken)setup.EnemyPolicy.ScoutMaxSeconds,
                ["policy_opening_restraint"]=setup?.EnemyPolicy==null?null:(JToken)setup.EnemyPolicy.OpeningRestraintSeconds,
                ["player_infantry"]=setup==null?null:(JToken)setup.PlayerInfantry,
                ["enemy_infantry"]=setup==null?null:(JToken)setup.EnemyInfantry,
                ["materials_each"]=setup==null?null:(JToken)setup.MaterialsEach,
                ["compiler_exception"]=error,
                ["reasons"]=reasons==null?null:new JArray(reasons.Select(x=>new JObject { ["code"]=x.Code.ToString(),["field"]=x.Field,["detail"]=x.Detail })),
                ["budget_inspected"]=inspected,["budget_normal_compiler_passed"]=budget==null?null:(JToken)budget.NormalCompilerPassed,
                ["budget_exact_canonical_quantities"]=budget==null?null:(JToken)budget.ExactCanonicalQuantities,
                ["budget_errors"]=budget==null?null:new JArray(budget.CanonicalBudgetErrors),["budget_exception"]=budgetError,
                ["budget_reasons"]=budgetReasons==null?null:new JArray(budgetReasons.Select(x=>new JObject { ["code"]=x.Code.ToString(),["field"]=x.Field,["detail"]=x.Detail })) });
        }
        var result=new JObject { ["kind"]="SourceS005DifficultyCompilerProbe",["scope"]="Original compiler on authored source inputs; no world or outcome",
            ["rows"]=output,["world_allocated"]=false,["source_fit_certified"]=false,["runtime_admission"]=false,["mission_ready"]=false };
        using(var f=new FileStream(args[4],FileMode.CreateNew,FileAccess.Write))using(var writer=new StreamWriter(f))writer.Write(result.ToString(Formatting.Indented));
        Console.WriteLine("[SourceS005DifficultyCompilerProbe] result=Collected tuples="+output.Count+" world=0 admission=0 readiness=0");
    }
}
