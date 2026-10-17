#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using Game.Configs;
using Game.Skirmish.Contracts;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;

namespace Game.Editor
{
    public static partial class SkirmishGroundSourceFitSurvey
    {
        const string All120RefinementIndex="Design/AgentReports/SkirmishExpansion/Full120/source-fit-authoring-batches-20261015-v2.json";
        const string All120RefinementIndexSha="0a37b558422b2d95ee4338fda61cb9b0c43c3f429ac4ef15b189e79248107540";

        static JObject ReadAll120RefinementIndex()
        {
            Require(Hash(All120RefinementIndex)==All120RefinementIndexSha,"Exact Full120 authority required");
            return JObject.Parse(File.ReadAllText(All120RefinementIndex));
        }

        static JObject[] GroundBa10Rows()
        {
            var rows=((JArray)ReadAll120RefinementIndex()["missions"]).OfType<JObject>()
                .Where(r=>(string)r["tuple"]["army_profile"]=="G"&&(string)r["tuple"]["objective_id"]=="BA").ToArray();
            Require(rows.Length==10&&rows.Select(r=>(string)r["catalog_id"]).Distinct().Count()==10&&
                rows.Sum(r=>((JArray)r["canonical_size_rows"]).Count)==30,"Exact Ground BA10/30 authority required");
            Require(rows.Where(r=>r["protected_binding"].Type!=JTokenType.Null).Select(r=>(string)r["catalog_id"]).OrderBy(v=>v)
                .SequenceEqual(new[]{"S001","S002","S025","S073"}),"Four exact Ground BA protected records required");
            return rows;
        }

        static JObject GroundBaTemplate(JObject mission,bool counterpart=false)
        {
            var rows=GroundBa10Rows().Where(r=>(string)r["catalog_id"]==(string)mission?["catalog_id"]).ToArray();
            Require(rows.Length==1&&JToken.DeepEquals(rows[0],mission),"Exact immutable Ground BA row required");
            Require(counterpart?SkirmishMissionDispatch.IsExpandedCounterpart((string)mission["catalog_id"]):mission["protected_binding"].Type==JTokenType.Null,"Protected Ground BA binding is read-only except exact expanded counterpart authoring");
            var ground=ReadGround30Envelope();
            var templates=((JArray)ground["missions"]).OfType<JObject>().Where(r=>
                (string)r["tuple"]["map_id"]==(string)mission["tuple"]["map_id"]&&
                (string)r["tuple"]["start_profile"]==(string)mission["tuple"]["start_profile"]&&
                (string)r["tuple"]["objective_id"]=="FC").ToArray();
            Require(templates.Length==1,"One exact measured-source Ground FC layout template required");
            return templates[0];
        }

        public static bool TryCreateGroundBa10Candidate(JObject mission,OperationMapDefinition source,
            out GroundObjective30CandidateOwner owner,out string error)
            => TryCreateGroundBaCandidate(mission,source,false,out owner,out error);

        public static bool TryCreateProtectedExpandedBaseAssaultCandidate(JObject mission,OperationMapDefinition source,
            out GroundObjective30CandidateOwner owner,out string error)
            => TryCreateGroundBaCandidate(mission,source,true,out owner,out error);

        static bool TryCreateGroundBaCandidate(JObject mission,OperationMapDefinition source,bool counterpart,
            out GroundObjective30CandidateOwner owner,out string error)
        {
            owner=null;error=null;var created=new GroundObjective30CandidateOwner();
            try
            {
                var template=GroundBaTemplate(mission,counterpart);
                Require(TryCreateGroundObjective30Candidate(template,source,out var original,out string failure),failure);
                using(original)
                {
                    created.Layout=created.Own(CloneAirCombined80AdaptedLayout(original.Layout,source,(string)mission["catalog_id"]));
                    var objective=created.Own(UnityEngine.Object.Instantiate(original.Definition.ObjectiveConfig));
                    objective.ConfigureBaseAssault();
                    var serialized=new SerializedObject(objective);
                    foreach(JObject row in mission["canonical_size_rows"])
                    {
                        string size=(string)row["size_id"],field=size=="Standard"?"standardDeadlineSeconds":size=="War"?"warDeadlineSeconds":"largeWarDeadlineSeconds";
                        Require(size=="Standard"||size=="War"||size=="LargeWar","Canonical size required");
                        serialized.FindProperty(field).intValue=int.Parse((string)row["deadline_seconds"],System.Globalization.CultureInfo.InvariantCulture);
                    }
                    serialized.ApplyModifiedPropertiesWithoutUndo();
                    var catalog=Resources.Load<SkirmishExpansionCatalogConfig>(SkirmishExpansionCatalogConfig.ResourceName);
                    Require(catalog!=null&&!EditorUtility.IsDirty(catalog),"Clean actual catalog required");
                    var profiles=catalog.ToAuthoredSet();
                    Require(original.Definition.ArmyProfileConfig==profiles.ArmyGround,"Exact persistent Ground profile required");
                    created.Definition=created.Own(ScriptableObject.CreateInstance<SkirmishScenarioDefinitionConfig>());
                    created.Definition.Configure((string)mission["catalog_id"],objective,profiles.ArmyGround,
                        (string)mission["tuple"]["start_profile"]=="F"?profiles.StartField:profiles.StartEstablished,
                        profiles.Economy,profiles.Upgrades,profiles.Intel,original.Definition.RoleCatalog,created.Layout,
                        SkirmishSizeId.Standard,mission["required_capabilities"].ToObject<string[]>());
                    created.Definition.BindCampaignMap(source);
                    Require(created.Definition.DefinitionId==(string)mission["definition_id"]&&
                        created.Definition.ScenarioSetupId==(string)mission["scenario_setup_id"]&&
                        created.Definition.MissionLogicalBinding==null&&created.AreaDraft==null&&
                        !created.Layout.WorldBinding.HasMeasuredDeployments&&!created.Layout.WorldBinding.BindAllConfigurations,
                        "Ground BA candidate must retain exact identity without non-BA areas or measurement");
                }
                owner=created;return true;
            }
            catch(Exception failure){created.Dispose();error="Ground BA candidate "+(string)mission?["catalog_id"]+": "+failure;return false;}
        }

        static JObject All120MeasurementMission(JObject authority,JObject template)
        {
            // The common allocator needs the reviewed source and search compound;
            // all budgets come from the actual candidate definition, never template quantities.
            var row=(JObject)template.DeepClone();
            foreach(var key in new[]{"catalog_id","definition_id","scenario_setup_id","tuple","canonical_size_rows","required_capabilities"})
                row[key]=authority[key].DeepClone();
            return row;
        }
    }
}
#endif
