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
internal static class SourceS002DifficultyCompilerProbe
{
    static void Main(string[] args)
    {
        if(args.Length!=4)throw new ArgumentException("graph, pinned matrix, matrix hash and fresh output required");
        var graph=JObject.Parse(File.ReadAllText(args[0]));
        var source=new SourceConfigMaterializer(graph);
        var definition=source.ByPath<SkirmishScenarioDefinitionConfig>("Assets/Game/Configs/SkirmishExpansion/Scenarios/S002/SkirmishScenario_S002.asset");
        if(definition.CatalogId!="S002")throw new InvalidDataException("Exact authored S002 definition required");
        var catalog=source.ByPath<SkirmishExpansionCatalogConfig>((string)graph["catalog_path"]);
        if(catalog==null || !catalog.TryGetDefinition("S002",definition.ContentVersion,out var actualDefinition) ||
           !Object.ReferenceEquals(actualDefinition,definition))throw new InvalidDataException("Exact graph catalog identity required");
        var helipadGrant=typeof(SkirmishSetupCompiler).GetMethod("GrantsEstablishedHelipad",BindingFlags.NonPublic|BindingFlags.Static);
        if(helipadGrant==null)throw new InvalidDataException("Original helipad grant owner required");
        bool legacyHelipad=(bool)helipadGrant.Invoke(null,new object[]{definition,true});
        bool genericHelipad=(bool)helipadGrant.Invoke(null,new object[]{definition,false});
        UnityEngine.Resources.BindSourceResources(new Dictionary<string,object> { [SkirmishExpansionCatalogConfig.ResourceName]=catalog });
        var rows=SourceExternalSetupMatrix.ReadVerified(File.ReadAllBytes(args[1]),args[2]);
        var manifest=new SkirmishContentManifest { RequiredFeatureIds=definition.RequiredFeatureIds };
        var output=new JArray();
        foreach(var size in new[]{SkirmishSizeId.Standard,SkirmishSizeId.War,SkirmishSizeId.LargeWar})
        foreach(var difficulty in new[]{SkirmishDifficultyId.Recruit,SkirmishDifficultyId.Regular,SkirmishDifficultyId.Veteran,SkirmishDifficultyId.Commander})
        {
            var row=SourceExternalSetupMatrix.Select(rows,"S002",size);
            bool passed=false; SkirmishResolvedSetup setup=null; List<SkirmishCompileReason> reasons=null; string error=null;
            try { passed=SkirmishSetupCompiler.TryCompile(definition,difficulty,size,104731,manifest,rows,out setup,out reasons); }
            catch(Exception ex) { error=ex.ToString(); }
            bool inspected=false; SkirmishStaticAuthoringBudget budget=null; List<SkirmishCompileReason> budgetReasons=null; string budgetError=null;
            try { inspected=SkirmishSetupCompiler.TryInspectStaticAuthoringBudget(definition,difficulty,size,104731,manifest,rows,out budget,out budgetReasons); }
            catch(Exception ex) { budgetError=ex.ToString(); }
            bool reviewed=SkirmishLegacyLayoutBaselineContract.TryGetReviewedLayoutFingerprint(definition.CatalogId,out string reviewedLayout);
            bool protectedLegacy=false; string protectedError=null;
            try { protectedLegacy=passed && reviewed && SkirmishMeasuredLayoutBinder.PreservesPackagedLegacyBinding(setup,definition,reviewedLayout); }
            catch(Exception ex) { protectedError=ex.ToString(); }
            output.Add(new JObject { ["size"]=size.ToString(),["difficulty"]=difficulty.ToString(),["seed"]=104731,
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
                ["legacy_reviewed_fingerprint_available"]=reviewed,["protected_legacy_contract"]=protectedLegacy,["protected_legacy_exception"]=protectedError,
                ["original_legacy_helipad_grant"]=legacyHelipad,["original_nonlegacy_helipad_grant"]=genericHelipad,
                ["matrix_player_support"]=row.PlayerLogisticsSupport,["matrix_enemy_support"]=row.EnemyLogisticsSupport,
                ["matrix_player_structures"]=row.PlayerStartingStructures,["matrix_enemy_structures"]=row.EnemyStartingStructures,
                ["actual_forces"]=setup==null?null:new JArray(setup.Forces.GroupBy(x=>new { x.FactionId,x.RoleId }).Select(x=>new JObject { ["faction"]=x.Key.FactionId,["role_id"]=x.Key.RoleId,["quantity"]=x.Sum(v=>v.Quantity) })),
                ["actual_structures"]=setup==null?null:new JArray(setup.Structures.GroupBy(x=>x.FactionId).Select(x=>new JObject { ["faction"]=x.Key,["count"]=x.Count() })),
                ["actual_structure_ids"]=setup==null?null:new JArray(setup.Structures.Select(x=>new JObject { ["faction"]=x.FactionId,["structure_id"]=x.StructureId })),
                ["budget_reasons"]=budgetReasons==null?null:new JArray(budgetReasons.Select(x=>new JObject { ["code"]=x.Code.ToString(),["field"]=x.Field,["detail"]=x.Detail })) });
        }
        var result=new JObject { ["kind"]="SourceS002DifficultyCompilerProbe",["scope"]="Original compiler on authored source inputs; no world or outcome",
            ["rows"]=output,["world_allocated"]=false,["source_fit_certified"]=false,["runtime_admission"]=false,["mission_ready"]=false };
        using(var f=new FileStream(args[3],FileMode.CreateNew,FileAccess.Write))using(var writer=new StreamWriter(f))writer.Write(result.ToString(Formatting.Indented));
        Console.WriteLine("[SourceS002DifficultyCompilerProbe] result=Collected tuples="+output.Count+" world=0 admission=0 readiness=0");
    }
}
