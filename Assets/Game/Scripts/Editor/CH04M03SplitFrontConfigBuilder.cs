using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using Game.Components;
using Game.Configs;
using Game.Missions.Contracts;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;

namespace Game.Editor
{
    public static partial class CH04M03SplitFrontConfigBuilder
    {
        public const string MissionPath="Assets/Game/Configs/Missions/Chapter04/MissionDefinition_Ch04_M03_SplitFront.asset";
        public const string ScenarioPath="Assets/Game/Configs/Scenarios/Chapter04/ScenarioSetup_Ch04_M03_SplitFront.asset";
        public const string LegacyMapPath="Assets/Game/Configs/OperationMaps/Chapter04/OperationMap_Ch04_SplitFront01.asset";
        public const string MapPath="Assets/Game/Configs/OperationMaps/Chapter04/OperationMap_Ch04_SplitFrontRefineryReview.asset";
        public const string MapId="opmap.ch04.split_front_refinery_review",ScenarioId="scenario.ch04.m03.split_front",Prefix="anchor.ch04.m03.";
        public const string ReserveBuildingId="Building_SplitFront_SupportDepot";
        public const string ReservePrefabPath="Assets/Game/Prefabs/Buildings/CH04M03SplitFront/"+ReserveBuildingId+".prefab";
        public static void Build()
        {
            foreach(var pair in new[]{("Assets/Game/Configs/Missions/Chapter04/MissionDefinition_Ch04_M02_SplitFront.asset",MissionPath),("Assets/Game/Configs/Scenarios/Chapter04/ScenarioSetup_Ch04_M02_SplitFront.asset",ScenarioPath)})if(AssetDatabase.LoadMainAssetAtPath(pair.Item1)!=null&&AssetDatabase.LoadMainAssetAtPath(pair.Item2)==null)Require(string.IsNullOrEmpty(AssetDatabase.MoveAsset(pair.Item1,pair.Item2)),"Mission asset migration failed");
            foreach(string category in new[]{"Missions","Scenarios","OperationMaps"})Directory.CreateDirectory("Assets/Game/Configs/"+category+"/Chapter04");
            AssetDatabase.Refresh();BuildSupportReservePrefab();BuildRefineryMap();BuildScenario();BuildMission();CH04M03SplitFrontSupportBuilder.Build();
            M01FirstContactConfigBuilder.RefreshChapterCatalogs();AssetDatabase.SaveAssets();
            Debug.Log("[SplitFrontConfig] result=Passed chapter=4 mission=3 launcher=1 defenders=7 hostiles=5 protectedCivilians=3 terrain=prepared-refinery");
        }
        private static void BuildSupportReservePrefab()
        {
            Directory.CreateDirectory("Assets/Game/Prefabs/Buildings/CH04M03SplitFront");AssetDatabase.Refresh();
            var basis=AssetDatabase.LoadAssetAtPath<GameObject>(CH02M02SupplyLineEnvironmentBuilder.DepotPath)??throw new InvalidOperationException("Qualified depot missing");
            var root=(GameObject)PrefabUtility.InstantiatePrefab(basis);
            try
            {
                root.name=ReserveBuildingId;
                var authoring=new SerializedObject(root.GetComponent<Game.Authoring.BuildingDefinitionAuthoring>());
                authoring.FindProperty("fuelStorageCapacity").intValue=200;
                authoring.FindProperty("description").stringValue="Finite Smoke allocation: 4 barrels for optional support; 40 protected for hospital generators and water pumps.";
                authoring.ApplyModifiedPropertiesWithoutUndo();
                var prefab=PrefabUtility.SaveAsPrefabAsset(root,ReservePrefabPath);
                var config=AssetDatabase.LoadAssetAtPath<BuildingPlacementSystemConfig>("Assets/Game/Configs/Scene/Game_BuildingPlacement_Config.asset");
                var data=new SerializedObject(config);var list=data.FindProperty("spawnables");bool found=false;
                for(int i=0;i<list.arraySize;i++)found|=list.GetArrayElementAtIndex(i).objectReferenceValue==prefab;
                if(!found){int index=list.arraySize++;list.GetArrayElementAtIndex(index).objectReferenceValue=prefab;data.ApplyModifiedPropertiesWithoutUndo();EditorUtility.SetDirty(config);}
            }
            finally{UnityEngine.Object.DestroyImmediate(root);}
        }
        private static void BuildScenario()
        {
            var scenario=Clone<ScenarioSetupConfig>(M03RadarWarningConfigBuilder.ScenarioPath,ScenarioPath);var data=new SerializedObject(scenario);
            Replace(data,"ch01.m03","ch04.m03");Replace(data,"ch01.convoy_approach","ch04.split_front");Replace(data,"mission.m03.","mission.split_front.");
            S(data.FindProperty("scenarioId"),ScenarioId);S(data.FindProperty("operationMapId"),MapId);I(data.FindProperty("deterministicSeed"),4003001);
            var map=AssetDatabase.LoadAssetAtPath<OperationMapDefinition>(MapPath);var required=data.FindProperty("requiredAnchors");required.arraySize=map.Anchors.Length;
            for(int i=0;i<required.arraySize;i++){var a=required.GetArrayElementAtIndex(i);S(a.FindPropertyRelative("anchorId"),map.Anchors[i].AnchorId);I(a.FindPropertyRelative("kind"),(int)map.Anchors[i].Kind);}
            var runtime=data.FindProperty("missionRuntime");S(runtime.FindPropertyRelative("baseMissionRoleId"),"role.friendly.forward_base");
            I(runtime.FindPropertyRelative("startingCredits"),0);
            I(runtime.FindPropertyRelative("startingMaterials"),200);
            var buildZone=runtime.FindPropertyRelative("buildZone");I(buildZone.FindPropertyRelative("halfWidthCells"),65);I(buildZone.FindPropertyRelative("halfHeightCells"),30);
            // Scripted starting buildings pass the same mission catalogue policy
            // as player construction. The already-present reserve uses the one slot.
            var buildCatalog=runtime.FindPropertyRelative("buildCatalog");buildCatalog.arraySize=4;
            S(buildCatalog.GetArrayElementAtIndex(3).FindPropertyRelative("buildingConfigId"),ReserveBuildingId);
            I(buildCatalog.GetArrayElementAtIndex(3).FindPropertyRelative("maxCount"),1);
            var groups=data.FindProperty("unitGroups");groups.arraySize=6;
            Group(groups.GetArrayElementAtIndex(0),"launcher",1,"squad_a","role.friendly.command_squad",new[]{"Veh_Missle_Launcher_Ground"});
            Group(groups.GetArrayElementAtIndex(1),"defenders",1,"squad_b","role.friendly.command_squad",new[]{"Veh_Tank_USA","Veh_Tank_USA","Veh_Tank_USA"});
            // Separate tanks keep the native eleven-cell formation spacing
            // on the seven-cell clear service corridor.
            var tanks=groups.GetArrayElementAtIndex(1).FindPropertyRelative("units");
            S(tanks.GetArrayElementAtIndex(1).FindPropertyRelative("spawnAnchorId"),Prefix+"tank_support_b");
            S(tanks.GetArrayElementAtIndex(2).FindPropertyRelative("spawnAnchorId"),Prefix+"tank_support_c");
            Group(groups.GetArrayElementAtIndex(2),"infantry",1,"armor_support","role.friendly.command_squad",new[]{"Chr_Soldier_Male_02_Alt_02","Chr_Soldier_Male_02_Alt_04","Chr_Soldier_Female_01_Alt_01","Chr_Soldier_Female_02_Alt_01"});
            Group(groups.GetArrayElementAtIndex(3),"battery",2,"vanguard_spawn","role.hostile.battery",new[]{"Veh_Missle_Launcher_Ground"});
            Group(groups.GetArrayElementAtIndex(4),"diversion",2,"main_spawn","role.hostile.convoy",new[]{"Veh_Light_Armored_Car","Veh_APC_Fast","Chr_Soldier_Male_02_Alt_02","Chr_Soldier_Male_02_Alt_04"});
            Group(groups.GetArrayElementAtIndex(5),"civilians",0,"civilians","role.civilian.protected",new[]{"Chr_Civilian_Male_02","Chr_Civilian_Female_01","Chr_Civilian_Male_01"});
            var routes=data.FindProperty("patrolRoutes");routes.arraySize=2;
            for(int i=0;i<2;i++)
            {
                var route=routes.GetArrayElementAtIndex(i);string name=i==0?"battery":"diversion";
                S(route.FindPropertyRelative("routeId"),"route.ch04.m03."+name);S(route.FindPropertyRelative("unitGroupId"),"group.ch04.m03."+name);
                I(route.FindPropertyRelative("startDelayMilliseconds"),i==0?0:45000);var points=route.FindPropertyRelative("anchorIds");points.arraySize=3;
                string[] stops={"contact","fork","inner_core"};for(int n=0;n<3;n++)S(points.GetArrayElementAtIndex(n),Prefix+stops[n]);
            }
            var defense=data.FindProperty("defense");defense.FindPropertyRelative("vehiclesSelfSupplied").boolValue=true;
            S(defense.FindPropertyRelative("forwardPostStableId"),"split_front.fuel_reserve");
            S(defense.FindPropertyRelative("sensorMissionRoleId"),string.Empty);I(defense.FindPropertyRelative("radarPingCharges"),0);
            var guidance=defense.FindPropertyRelative("guidanceSteps");guidance.arraySize=4;
            int[] guideActions={1,2,4,5},guideCompletions={1,2,4,5};
            for(int i=0;i<4;i++)
            {
                var step=guidance.GetArrayElementAtIndex(i);
                S(step.FindPropertyRelative("stepId"),"tutorial.ch04.m03."+(i+1));
                S(step.FindPropertyRelative("titleKey"),"mission.split_front.tutorial."+(i+1)+".title");
                S(step.FindPropertyRelative("bodyKey"),"mission.split_front.tutorial."+(i+1)+".body");
                I(step.FindPropertyRelative("action"),guideActions[i]);I(step.FindPropertyRelative("completion"),guideCompletions[i]);
                step.FindPropertyRelative("optional").boolValue=false;
            }
            var elements=defense.FindPropertyRelative("convoyElements");
            for(int i=0;i<2;i++){var e=elements.GetArrayElementAtIndex(i);string elementName=i==0?"battery":"diversion";S(e.FindPropertyRelative("unitGroupId"),"group.ch04.m03."+elementName);S(e.FindPropertyRelative("routeId"),"route.ch04.m03."+elementName);I(e.FindPropertyRelative("warningAtMilliseconds"),i==0?0:25000);I(e.FindPropertyRelative("activationAtMilliseconds"),i==0?0:45000);I(e.FindPropertyRelative("contactAtMilliseconds"),i==0?1000:70000);}
            var tour=defense.FindPropertyRelative("cameraTour");tour.FindPropertyRelative("SmoothTimeSeconds").floatValue=1.25f;
            I(tour.FindPropertyRelative("StartHoldMilliseconds"),350);I(tour.FindPropertyRelative("PostHoldMilliseconds"),600);I(tour.FindPropertyRelative("ApproachHoldMilliseconds"),650);I(tour.FindPropertyRelative("ReturnHoldMilliseconds"),300);
            data.ApplyModifiedPropertiesWithoutUndo();Require(scenario.TryValidate(out string error),error);EditorUtility.SetDirty(scenario);
        }
        private static void BuildMission()
        {
            var mission=Clone<MissionDefinitionConfig>(M03RadarWarningConfigBuilder.MissionPath,MissionPath);var data=new SerializedObject(mission);
            Replace(data,"ch01.m03","ch04.m03");Replace(data,"mission.m03.","mission.split_front.");
            S(data.FindProperty("missionId"),CampaignMissionSequence.SplitFront);S(data.FindProperty("scenarioId"),ScenarioId);S(data.FindProperty("operationMapId"),MapId);
            string[] names={"battery","base","civilians"};var objectives=data.FindProperty("objectives");
            for(int i=0;i<3;i++){var o=objectives.GetArrayElementAtIndex(i);S(o.FindPropertyRelative("objectiveId"),"obj.ch04.m03."+names[i]);S(o.FindPropertyRelative("displayTextKey"),"mission.split_front.objective."+names[i]);S(o.FindPropertyRelative("missionRoleId"),i==0?"role.hostile.convoy":"role.friendly.forward_base");I(o.FindPropertyRelative("requiredCount"),i==0?5:1);}
            var stars=data.FindProperty("stars");for(int i=0;i<3;i++)S(stars.GetArrayElementAtIndex(i).FindPropertyRelative("displayTextKey"),"mission.split_front.star."+(i+1));I(stars.GetArrayElementAtIndex(1).FindPropertyRelative("rule"),(int)MissionStarRuleKind.NoSquadLoss);I(stars.GetArrayElementAtIndex(2).FindPropertyRelative("rule"),(int)MissionStarRuleKind.NoCivilianLoss);
            var rewards=data.FindProperty("firstClearRewards");rewards.arraySize=3;I(rewards.GetArrayElementAtIndex(0).FindPropertyRelative("amount"),2000);I(rewards.GetArrayElementAtIndex(1).FindPropertyRelative("amount"),9000);var smoke=rewards.GetArrayElementAtIndex(2);I(smoke.FindPropertyRelative("kind"),0);S(smoke.FindPropertyRelative("rewardConfigId"),"reward.ch04.m03.precision_strike_unlock");S(smoke.FindPropertyRelative("displayTextKey"),"mission.reward.precision_strike");I(smoke.FindPropertyRelative("amount"),1);
            data.ApplyModifiedPropertiesWithoutUndo();Require(MissionDefinitionContractValidation.TryValidateDefinition(mission,out string error),error);EditorUtility.SetDirty(mission);
        }
        private static void Group(SerializedProperty group,string name,int faction,string anchor,string role,string[] prefabs)
        {
            S(group.FindPropertyRelative("groupId"),"group.ch04.m03."+name);I(group.FindPropertyRelative("factionIndex"),faction);
            var units=group.FindPropertyRelative("units");units.arraySize=prefabs.Length;
            for(int i=0;i<prefabs.Length;i++)
            {
                string key="Unit_"+prefabs[i],path="Assets/Game/Prefabs/"+(prefabs[i].StartsWith("Veh_")?"Vehicles/":"Characters/")+key+".prefab";
                Require(AssetDatabase.LoadAssetAtPath<GameObject>(path)!=null,path);var u=units.GetArrayElementAtIndex(i);
                S(u.FindPropertyRelative("unitConfigKey"),"unit.ch04.m03."+prefabs[i].ToLowerInvariant());S(u.FindPropertyRelative("runtimePrefabSourceKey"),key);
                S(u.FindPropertyRelative("expectedAssetGuid"),AssetDatabase.AssetPathToGUID(path));S(u.FindPropertyRelative("spawnAnchorId"),Prefix+anchor);S(u.FindPropertyRelative("missionRoleId"),role);I(u.FindPropertyRelative("count"),1);
            }
        }
        private static T Clone<T>(string source,string target)where T:ScriptableObject
        {var value=AssetDatabase.LoadAssetAtPath<T>(target);if(value==null){value=ScriptableObject.CreateInstance<T>();AssetDatabase.CreateAsset(value,target);}EditorUtility.CopySerialized(AssetDatabase.LoadAssetAtPath<T>(source)??throw new InvalidOperationException(source),value);value.name=Path.GetFileNameWithoutExtension(target);return value;}
        private static void Replace(SerializedObject data,string from,string to){var p=data.GetIterator();if(!p.Next(true))return;do{if(p.propertyType==SerializedPropertyType.String)p.stringValue=p.stringValue.Replace(from,to);}while(p.Next(true));}
        private static void S(SerializedProperty p,string value)=>p.stringValue=value;
        private static void I(SerializedProperty p,int value)=>p.intValue=value;
        private static void Require(bool value,string error){if(!value)throw new InvalidOperationException(error);}
    }
}
