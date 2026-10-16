#if UNITY_EDITOR
using System;
using System.Linq;
using Game.Configs;
using Game.Skirmish.Contracts;
using Newtonsoft.Json.Linq;
using Unity.Entities;
using UnityEditor;
using UnityEngine;

namespace Game.Editor
{
    public static partial class SkirmishGroundSourceFitSurvey
    {
        public static void RunAll120RefinementAuthoringControls()
        {
            Require(!EditorApplication.isPlayingOrWillChangePlaymode,"Idle nonstreaming authoring controls required");
            var worlds=CaptureMaterializationWorldCensus();var oldDefault=World.DefaultGameObjectInjectionWorld;
            var scenes=Ground30SceneOwnership();int generic=0,protectedRows=0,vectors=0,negative=0,exactVectors=0,cePending=0;
            Require(SkirmishSetupMatrixTable.TryLoadPackaged(out var matrix,out string error),error);
            void Check(GroundObjective30CandidateOwner owner,JObject row)
            {
                Require(owner.Definition.CatalogId==(string)row["catalog_id"]&&
                    owner.Definition.DefinitionId==(string)row["definition_id"]&&
                    owner.Definition.ScenarioSetupId==(string)row["scenario_setup_id"]&&
                    owner.Definition.MissionLogicalBinding==null&&!owner.Layout.WorldBinding.HasMeasuredDeployments&&
                    !owner.Layout.WorldBinding.BindAllConfigurations,"Exact unmeasured own candidate identity required");
                bool ba=(string)row["tuple"]["objective_id"]=="BA";
                Require(ba?owner.AreaDraft==null:owner.AreaDraft!=null,"BA omits areas; non-BA retains exact typed draft");
                foreach(var size in new[]{SkirmishSizeId.Standard,SkirmishSizeId.War,SkirmishSizeId.LargeWar})
                {
                    Require(SkirmishSetupCompiler.TryInspectStaticAuthoringBudget(owner.Definition,SkirmishDifficultyId.Regular,size,1,
                        new SkirmishContentManifest{RequiredFeatureIds=owner.Definition.RequiredFeatureIds},matrix,out var budget,out var reasons)&&
                        budget!=null,"Actual typed budget inspection required: "+string.Join(";",reasons));
                    if((string)row["tuple"]["objective_id"]=="CE")
                    {
                        // The native source core rebuilds three distinct bays from
                        // its actual grid+truck prefab before demanding exact budget.
                        // This nonstreaming factory control cannot invent that fit.
                        var canonical=((JArray)row["canonical_size_rows"]).OfType<JObject>().Single(v=>(string)v["size_id"]==size.ToString());
                        Require((string)canonical["player_objective_trucks"]=="3"&&(string)canonical["enemy_objective_trucks"]=="0",
                            "Immutable CE request must retain exactly three own objective trucks");cePending++;
                    }
                    else{Require(budget.ExactCanonicalQuantities,"Actual non-CE canonical quantities required: "+string.Join(";",budget.CanonicalBudgetErrors));exactVectors++;}
                    Require(!budget.NormalCompilerPassed,"Unmeasured candidate must not acquire normal launch eligibility");vectors++;
                }
                generic++;
            }
            foreach(var row in GroundBa10Rows())
            {
                if(row["protected_binding"].Type!=JTokenType.Null)
                {
                    Require(!TryCreateGroundBa10Candidate(row,null,out var forbidden,out _),"Protected Ground BA cannot materialize");
                    forbidden?.Dispose();protectedRows++;continue;
                }
                var template=GroundBaTemplate(row);
                var source=AssetDatabase.LoadAssetAtPath<OperationMapDefinition>((string)template["source_candidate"]["sourcePath"]);
                Require(TryCreateGroundBa10Candidate(row,source,out var owner,out error),error);
                using(owner){Check(owner,row);}
                var bad=(JObject)row.DeepClone();bad["canonical_size_rows"][0]["player_rifle"]="999";
                Require(!TryCreateGroundBa10Candidate(bad,source,out var rejected,out _),"Changed Ground BA quantity must reject");rejected?.Dispose();negative++;
            }
            var air=ReadAirCombined80Envelope();
            foreach(JObject row in air["missions"])
            {
                if(row["protected_binding"].Type!=JTokenType.Null)
                {
                    Require(TryReadAirCombined80ProtectedRecord(row,out var record,out error),error);
                    Require((bool?)record["familySourceApplied"]==false,"Protected A/C cannot use family source");protectedRows++;continue;
                }
                var source=AssetDatabase.LoadAssetAtPath<OperationMapDefinition>((string)air["source_candidates"][(string)row["family_source_candidate_key"]]["sourcePath"]);
                Require(TryCreateAirCombined80Candidate(row,source,out var owner,out error),error);
                using(owner)
                using(var bridge=new GroundObjective30CandidateOwner{Definition=owner.Definition,Layout=owner.Layout,AreaDraft=owner.AreaDraft})
                {Check(bridge,row);}
                var bad=(JObject)row.DeepClone();bad["canonical_size_rows"][0]["player_rifle"]="999";
                Require(!TryCreateAirCombined80Candidate(bad,source,out var rejected,out _),"Changed A/C quantity must reject");rejected?.Dispose();negative++;
            }
            Require(generic==83&&protectedRows==7&&vectors==249&&negative==83&&exactVectors==189&&cePending==60,"Exact83 generic/7 protected/249 inspected vectors/189 exact non-CE/60 native CE pending/83 negatives required");
            Require(worlds.SequenceEqual(CaptureMaterializationWorldCensus())&&ReferenceEquals(oldDefault,World.DefaultGameObjectInjectionWorld)&&
                JToken.DeepEquals(scenes,Ground30SceneOwnership()),"Authoring controls must not create World/scene/root state");
            Debug.Log("[All120RefinementAuthoring] result=Passed additionalGeneric=83 protectedReadOnly=7 canonicalSizeVectorsInspected=249 exactNonCeBudgetVectors=189 ceNativeBayBudgetPending=60 negativeControls=83 streamingWorldsCreated=0 sourceFitCertified=0 cameraAccepted=0");
        }
    }
}
#endif
