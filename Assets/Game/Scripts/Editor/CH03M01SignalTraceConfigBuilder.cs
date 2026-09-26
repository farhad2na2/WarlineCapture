using System;
using System.IO;
using Game.Configs;
using Game.Missions.Contracts;
using UnityEditor;
using UnityEngine;

namespace Game.Editor
{
    public static class CH03M01SignalTraceConfigBuilder
    {
        public const string MissionId=CampaignMissionSequence.SignalTrace,ScenarioId="scenario.ch03.m01.signal_trace",MapId="opmap.ch03.signal_trace_01";
        public const string MissionPath="Assets/Game/Configs/Missions/Chapter03/MissionDefinition_Ch03_M01_SignalTrace.asset";
        public const string ScenarioPath="Assets/Game/Configs/Scenarios/Chapter03/ScenarioSetup_Ch03_M01_SignalTrace.asset";
        public const string MapPath="Assets/Game/Configs/OperationMaps/Chapter03/OperationMap_Ch03_SignalTrace01.asset";

        [MenuItem("Game/Campaign/Signal Trace/Build Configuration")]
        public static void Build()
        {
            foreach(string category in new[]{"Missions","Scenarios","OperationMaps"})Directory.CreateDirectory("Assets/Game/Configs/"+category+"/Chapter03");
            AssetDatabase.Refresh();BuildMap();BuildScenario();BuildMission();M01FirstContactConfigBuilder.RefreshChapterCatalogs();
            Require(MissionDefinitionContractValidation.TryValidateCatalog(Load<MissionDefinitionCatalogConfig>(M01FirstContactConfigBuilder.CatalogPath),out string error),error);
            AssetDatabase.SaveAssets();Debug.Log("[SignalTraceConfig] result=Passed chapter=3 mission=1 fallback=authored-observation-points controls=Select,Move,Attack,Hold");
        }

        private static void BuildMap()
        {
            OperationMapDefinition map=Clone<OperationMapDefinition>(CH02M03MarketLifelineConfigBuilder.MapPath,MapPath);
            SerializedObject data=new(map);ReplaceStrings(data,"ch02.m03","ch03.m01");Set(data,"operationMapId",MapId);Set(data,"planningCameraId","camera.ch03.m01.overview");Set(data,"battleCameraId","camera.ch03.m01.battle");
            SerializedProperty anchors=data.FindProperty("anchors");
            for(int i=0;i<anchors.arraySize;i++)
            {
                SerializedProperty anchor=anchors.GetArrayElementAtIndex(i);
                if(anchor.FindPropertyRelative("anchorId").stringValue!="anchor.ch03.m01.manifest")continue;
                float y=anchor.FindPropertyRelative("position").vector3Value.y;
                anchor.FindPropertyRelative("position").vector3Value=new Vector3(707f,y,430f);
                break;
            }
            data.ApplyModifiedPropertiesWithoutUndo();
            Require(map.TryValidateMetadata(out string error)&&map.TryValidateLocalContentReferences(out error),error);EditorUtility.SetDirty(map);
        }

        private static void BuildScenario()
        {
            ScenarioSetupConfig scenario=Clone<ScenarioSetupConfig>(CH02M03MarketLifelineConfigBuilder.ScenarioPath,ScenarioPath);
            SerializedObject data=new(scenario);ReplaceStrings(data,"ch02.m03","ch03.m01");Set(data,"scenarioId",ScenarioId);Set(data,"operationMapId",MapId);Set(data,"deterministicSeed",3001001);data.ApplyModifiedPropertiesWithoutUndo();
            Require(scenario.TryValidate(out string error),error);EditorUtility.SetDirty(scenario);
        }

        private static void BuildMission()
        {
            MissionDefinitionConfig mission=Clone<MissionDefinitionConfig>(CH02M03MarketLifelineConfigBuilder.MissionPath,MissionPath);
            SerializedObject data=new(mission);Set(data,"missionId",MissionId);Set(data,"scenarioId",ScenarioId);Set(data,"operationMapId",MapId);Set(data,"displayNameKey","mission.signal_trace.name");Set(data,"displaySummaryKey","mission.signal_trace.summary");Set(data,"locationNameKey","mission.signal_trace.location");Set(data,"briefingSequenceId","seq.ch03.m01.brief");Set(data,"commsSequenceId","seq.ch03.m01.comms");Set(data,"debriefSequenceId","seq.ch03.m01.debrief");
            string[] ids={"compare","recover","intercept"};MissionObjectiveRuleKind[] rules={MissionObjectiveRuleKind.VerifyMarketManifest,MissionObjectiveRuleKind.DeliverMarketRelief,MissionObjectiveRuleKind.KeepMarketOpen};int[] counts={2,3,1};string[] roles={"role.market.manifest","role.market.relief_convoy","role.market.trade"};
            SerializedProperty objectives=data.FindProperty("objectives");objectives.arraySize=3;for(int i=0;i<3;i++){SerializedProperty e=objectives.GetArrayElementAtIndex(i);Set(e,"objectiveId","obj.ch03.m01."+ids[i]);Set(e,"displayTextKey","mission.signal_trace.objective."+ids[i]);Set(e,"rule",(int)rules[i]);Set(e,"missionRoleId",roles[i]);Set(e,"targetConfigId",string.Empty);Set(e,"requiredCount",counts[i]);Set(e,"failureOnRuleBreak",true);}
            SerializedProperty stars=data.FindProperty("stars");for(int i=0;i<stars.arraySize;i++)Set(stars.GetArrayElementAtIndex(i),"displayTextKey","mission.signal_trace.star."+(i+1));
            SerializedProperty rewards=data.FindProperty("firstClearRewards");if(rewards.arraySize>0)Set(rewards.GetArrayElementAtIndex(0),"amount",1000);if(rewards.arraySize>1)Set(rewards.GetArrayElementAtIndex(1),"amount",5000);
            data.ApplyModifiedPropertiesWithoutUndo();Require(MissionDefinitionContractValidation.TryValidateDefinition(mission,out string error),error);EditorUtility.SetDirty(mission);
        }

        private static void ReplaceStrings(SerializedObject data,string from,string to)
        {
            SerializedProperty p=data.GetIterator();if(!p.Next(true))return;do{if(p.propertyType==SerializedPropertyType.String&&p.stringValue.IndexOf(from,StringComparison.Ordinal)>=0)p.stringValue=p.stringValue.Replace(from,to);}while(p.Next(true));
        }
        private static T Load<T>(string path) where T:UnityEngine.Object=>AssetDatabase.LoadAssetAtPath<T>(path)??throw new InvalidOperationException(path);
        private static T Clone<T>(string source,string target) where T:ScriptableObject{T asset=AssetDatabase.LoadAssetAtPath<T>(target);if(asset==null){asset=ScriptableObject.CreateInstance<T>();AssetDatabase.CreateAsset(asset,target);}EditorUtility.CopySerialized(Load<T>(source),asset);asset.name=Path.GetFileNameWithoutExtension(target);return asset;}
        private static void Set(SerializedObject data,string field,object value)=>Set(data.FindProperty(field),value);
        private static void Set(SerializedProperty parent,string field,object value)=>Set(parent.FindPropertyRelative(field),value);
        private static void Set(SerializedProperty p,object value){switch(value){case string s:p.stringValue=s;break;case int i:p.intValue=i;break;case bool b:p.boolValue=b;break;default:throw new ArgumentException(p.propertyPath);}}
        private static void Require(bool value,string error){if(!value)throw new InvalidOperationException(error);}
    }
}
