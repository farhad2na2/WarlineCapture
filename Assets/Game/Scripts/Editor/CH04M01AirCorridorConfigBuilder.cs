using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using Game.Components;
using Game.Configs;
using Game.Missions.Contracts;
using UnityEditor;
using UnityEngine;

namespace Game.Editor
{
    public static class CH04M01AirCorridorConfigBuilder
    {
        public const string MissionPath="Assets/Game/Configs/Missions/Chapter04/MissionDefinition_Ch04_M01_AirCorridor.asset";
        public const string ScenarioPath="Assets/Game/Configs/Scenarios/Chapter04/ScenarioSetup_Ch04_M01_AirCorridor.asset";
        public const string MapPath="Assets/Game/Configs/OperationMaps/Chapter04/OperationMap_Ch04_AirCorridor01.asset";
        public const string MapId="opmap.ch04.air_corridor_01",ScenarioId="scenario.ch04.m01.air_corridor";
        public const string Prefix="anchor.ch04.m01.";
        public static void Build()
        {
            foreach(string category in new[]{"Missions","Scenarios","OperationMaps"}) Directory.CreateDirectory("Assets/Game/Configs/"+category+"/Chapter04");
            AssetDatabase.Refresh();
            var map=Clone<OperationMapDefinition>(M03RadarWarningMapBuilder.Path,MapPath);
            var md=new SerializedObject(map); Replace(md);
            S(md.FindProperty("operationMapId"),MapId);
            using(var sha=SHA256.Create())
            {
                string hash=BitConverter.ToString(sha.ComputeHash(Encoding.UTF8.GetBytes(map.ContentHash+":air-corridor-west-north-v1"))).Replace("-","").ToLowerInvariant();
                S(md.FindProperty("contentHash"),hash);S(md.FindProperty("generatedMetadataHash"),hash);
            }
            var anchors=md.FindProperty("anchors");
            for(int i=0;i<anchors.arraySize;i++)
            {
                var a=anchors.GetArrayElementAtIndex(i); string id=a.FindPropertyRelative("anchorId").stringValue;
                if(id==Prefix+"vanguard_spawn") a.FindPropertyRelative("position").vector3Value=new Vector3(590,18,426);
                if(id==Prefix+"main_spawn") a.FindPropertyRelative("position").vector3Value=new Vector3(1050,18,280);
            }
            md.ApplyModifiedPropertiesWithoutUndo();
            Require(map.TryValidateMetadata(out string error)&&map.TryValidateLocalContentReferences(out error),error);EditorUtility.SetDirty(map);
            var scenario=Clone<ScenarioSetupConfig>(M03RadarWarningConfigBuilder.ScenarioPath,ScenarioPath);
            var sd=new SerializedObject(scenario);Replace(sd);
            S(sd.FindProperty("scenarioId"),ScenarioId);S(sd.FindProperty("operationMapId"),MapId);I(sd.FindProperty("deterministicSeed"),4001001);
            I(sd.FindProperty("missionRuntime").FindPropertyRelative("startingCredits"),0);
            I(sd.FindProperty("missionRuntime").FindPropertyRelative("startingMaterials"),140);
            S(sd.FindProperty("missionRuntime").FindPropertyRelative("baseMissionRoleId"),"role.friendly.ground_sensor");
            var groups=sd.FindProperty("unitGroups");groups.arraySize=6;
            Group(groups.GetArrayElementAtIndex(0),"launcher_west",1,"squad_a","role.friendly.launcher",new[]{"Veh_Missle_Launcher_Air"});
            Group(groups.GetArrayElementAtIndex(1),"launcher_north",1,"squad_b","role.friendly.launcher",new[]{"Veh_Missle_Launcher_Air"});
            Group(groups.GetArrayElementAtIndex(2),"ground_sensor",1,"ground_sensor","role.friendly.ground_sensor",new[]{"Veh_Radar_Tank"});
            Group(groups.GetArrayElementAtIndex(3),"escort",1,"civilians","role.friendly.command_squad",new[]{"Chr_Soldier_Male_02_Alt_04","Chr_Soldier_Female_02_Alt_01"});
            Group(groups.GetArrayElementAtIndex(4),"vanguard",2,"vanguard_spawn","role.hostile.aircraft",new[]{"Veh_Drone","Veh_Drone","Veh_Jet_01"});
            Group(groups.GetArrayElementAtIndex(5),"main_body",2,"main_spawn","role.hostile.aircraft",new[]{"Veh_Jet_01","Veh_Jet_01","Veh_Drone"});
            var routes=sd.FindProperty("patrolRoutes");routes.arraySize=2;
            for(int i=0;i<2;i++)
            {
                var route=routes.GetArrayElementAtIndex(i);string group=i==0?"vanguard":"main_body";
                S(route.FindPropertyRelative("routeId"),"route.ch04.m01."+group);S(route.FindPropertyRelative("unitGroupId"),"group.ch04.m01."+group);
                I(route.FindPropertyRelative("startDelayMilliseconds"),i==0?20000:65000);
                var points=route.FindPropertyRelative("anchorIds");points.arraySize=2;
                S(points.GetArrayElementAtIndex(0),Prefix+"fork");S(points.GetArrayElementAtIndex(1),Prefix+"inner_core");
            }
            var defense=sd.FindProperty("defense");
            var elements=defense.FindPropertyRelative("convoyElements");
            for(int i=0;i<2;i++)
            {
                var e=elements.GetArrayElementAtIndex(i);
                I(e.FindPropertyRelative("warningAtMilliseconds"),i==0?0:45000);
                I(e.FindPropertyRelative("activationAtMilliseconds"),i==0?20000:65000);
                I(e.FindPropertyRelative("contactAtMilliseconds"),i==0?50000:95000);
            }
            var tour=defense.FindPropertyRelative("cameraTour");tour.FindPropertyRelative("SmoothTimeSeconds").floatValue=1.5f;
            I(tour.FindPropertyRelative("StartHoldMilliseconds"),500);I(tour.FindPropertyRelative("PostHoldMilliseconds"),650);
            I(tour.FindPropertyRelative("ApproachHoldMilliseconds"),850);I(tour.FindPropertyRelative("ReturnHoldMilliseconds"),350);
            sd.ApplyModifiedPropertiesWithoutUndo();Require(scenario.TryValidate(out error),error);EditorUtility.SetDirty(scenario);
            var mission=Clone<MissionDefinitionConfig>(M03RadarWarningConfigBuilder.MissionPath,MissionPath);
            var data=new SerializedObject(mission);Replace(data);
            S(data.FindProperty("missionId"),CampaignMissionSequence.AirCorridor);S(data.FindProperty("scenarioId"),ScenarioId);S(data.FindProperty("operationMapId"),MapId);
            S(data.FindProperty("displayNameKey"),"mission.air_corridor.name");S(data.FindProperty("displaySummaryKey"),"mission.air_corridor.summary");S(data.FindProperty("locationNameKey"),"mission.air_corridor.location");
            string[] stages={"briefing","comms","debrief"};foreach(string stage in stages)S(data.FindProperty(stage+"SequenceId"),"seq.ch04.m01."+(stage=="briefing"?"brief":stage));
            string[] objectives={"aircraft","radar","corridor"};var os=data.FindProperty("objectives");
            for(int i=0;i<3;i++)
            {var o=os.GetArrayElementAtIndex(i);S(o.FindPropertyRelative("objectiveId"),"obj.ch04.m01."+objectives[i]);S(o.FindPropertyRelative("displayTextKey"),"mission.air_corridor.objective."+objectives[i]);S(o.FindPropertyRelative("missionRoleId"),i==0?"role.hostile.aircraft":"role.friendly.ground_sensor");I(o.FindPropertyRelative("requiredCount"),i==0?6:1);}
            var stars=data.FindProperty("stars");for(int i=0;i<3;i++)S(stars.GetArrayElementAtIndex(i).FindPropertyRelative("displayTextKey"),"mission.air_corridor.star."+(i+1));
            I(stars.GetArrayElementAtIndex(1).FindPropertyRelative("rule"),(int)MissionStarRuleKind.NoSquadLoss);
            var rewards=data.FindProperty("firstClearRewards");rewards.arraySize=2;
            I(rewards.GetArrayElementAtIndex(0).FindPropertyRelative("amount"),1800);I(rewards.GetArrayElementAtIndex(1).FindPropertyRelative("amount"),8500);
            data.ApplyModifiedPropertiesWithoutUndo();Require(MissionDefinitionContractValidation.TryValidateDefinition(mission,out error),error);EditorUtility.SetDirty(mission);
            M01FirstContactConfigBuilder.RefreshChapterCatalogs();AssetDatabase.SaveAssets();
            Debug.Log("[AirCorridorConfig] result=Passed aircraft=6 launchers=2 radar=1 waves=2 terrain=urban-airfield controls=existing-only");
        }
        private static void Group(SerializedProperty group,string id,int faction,string anchor,string role,string[] prefabs)
        {
            S(group.FindPropertyRelative("groupId"),"group.ch04.m01."+id);I(group.FindPropertyRelative("factionIndex"),faction);
            var units=group.FindPropertyRelative("units");units.arraySize=prefabs.Length;
            for(int i=0;i<prefabs.Length;i++)
            {
                var u=units.GetArrayElementAtIndex(i);string key="Unit_"+prefabs[i];
                string path="Assets/Game/Prefabs/"+(prefabs[i].StartsWith("Veh_")?"Vehicles/":"Characters/")+key+".prefab";
                Require(AssetDatabase.LoadAssetAtPath<GameObject>(path)!=null,path);
                S(u.FindPropertyRelative("unitConfigKey"),"unit.ch04.m01."+prefabs[i].ToLowerInvariant());S(u.FindPropertyRelative("runtimePrefabSourceKey"),key);
                S(u.FindPropertyRelative("expectedAssetGuid"),AssetDatabase.AssetPathToGUID(path));S(u.FindPropertyRelative("spawnAnchorId"),Prefix+anchor);
                S(u.FindPropertyRelative("missionRoleId"),role);I(u.FindPropertyRelative("count"),1);
            }
        }
        private static T Clone<T>(string source,string target)where T:ScriptableObject
        {var value=AssetDatabase.LoadAssetAtPath<T>(target);if(value==null){value=ScriptableObject.CreateInstance<T>();AssetDatabase.CreateAsset(value,target);}EditorUtility.CopySerialized(AssetDatabase.LoadAssetAtPath<T>(source)??throw new InvalidOperationException(source),value);value.name=System.IO.Path.GetFileNameWithoutExtension(target);return value;}
        private static void Replace(SerializedObject data)
        {var p=data.GetIterator();if(!p.Next(true))return;do{if(p.propertyType==SerializedPropertyType.String)p.stringValue=p.stringValue.Replace("ch01.m03","ch04.m01").Replace("ch01.convoy_approach","ch04.air_corridor").Replace("mission.m03.","mission.air_corridor.");}while(p.Next(true));}
        private static void S(SerializedProperty p,string v)=>p.stringValue=v;
        private static void I(SerializedProperty p,int v)=>p.intValue=v;
        private static void Require(bool value,string error){if(!value)throw new InvalidOperationException(error);}
    }
}
