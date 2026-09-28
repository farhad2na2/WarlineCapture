using System;
using System.Reflection;
using Game.Components;
using Game.Configs;
using Game.Missions.Contracts;
using Game.Runtime;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;
using UnityEditor;

namespace Game.Editor
{
    public static class CH04M02SteelPushRulesValidation
    {
        public static void Run()
        {
            ValidateGroundOnlyContract();
            using var world=new World("Steel Push isolated rule checks");var em=world.EntityManager;
            var root=em.CreateEntity(typeof(CampaignMissionSteelPushState));var building=em.CreateEntity(typeof(UnitHealth),typeof(BuildingResourceStorageComponent));
            var runtime=new CampaignMissionRuntimeComponent{MissionId=new FixedString64Bytes(CampaignMissionSequence.SteelPush),SessionToken="steel-rule",AttemptOrdinal=1,Phase=MissionPhaseKind.Engage};
            em.AddComponentData(root,runtime);
            em.SetComponentData(building,new UnitHealth{Current=1800,Max=1800});
            em.SetComponentData(building,new BuildingResourceStorageComponent{OwnerFactionId=1,FuelStorageCapacity=200,StoredFuelBarrels=160,CivilianFuelReserveBarrels=40});
            em.SetComponentData(root,new CampaignMissionSteelPushState{SessionToken=runtime.SessionToken,AttemptOrdinal=1,Initialized=1,FuelReserve=building,StartingUsableFuel=120,LowestUsableFuel=120});
            var ambient=em.CreateEntity(typeof(BuildingResourceStorageComponent));
            em.SetComponentData(ambient,new BuildingResourceStorageComponent{OwnerFactionId=1,FuelStorageCapacity=5000,StoredFuelBarrels=5000});
            Require(CampaignSteelPushFuelScope.TryGet(em,out var scoped,out _)&&scoped==building&&CampaignSteelPushFuelScope.Usable(em,scoped)==120,"Ambient demo stock leaked into finite mission Fuel");
            CampaignSteelPushFuelScope.Drain(em,scoped,10000);
            Require(em.GetComponentData<BuildingResourceStorageComponent>(building).StoredFuelBarrels==40&&em.GetComponentData<BuildingResourceStorageComponent>(ambient).StoredFuelBarrels==5000,"Scoped Fuel drain violated floor or ambient storage");
            var initialStorage=em.GetComponentData<BuildingResourceStorageComponent>(building);initialStorage.StoredFuelBarrels=160;em.SetComponentData(building,initialStorage);
            var facts=Project(em,root,runtime,default);Require(facts.ForwardPostBound==1&&facts.CoreBreached==0,"Fuel site not bound");
            var storage=em.GetComponentData<BuildingResourceStorageComponent>(building);storage.StoredFuelBarrels=155;em.SetComponentData(building,storage);
            facts=Project(em,root,runtime,facts);Require(em.GetComponentData<CampaignMissionSteelPushState>(root).FuelSpent==1&&facts.CoreBreached==0,"Finite Fuel spending not observed");
            storage.StoredFuelBarrels=40;em.SetComponentData(building,storage);facts=Project(em,root,runtime,facts);Require(facts.CoreBreached==0,"Protected civilian floor incorrectly fails");
            storage.StoredFuelBarrels=39;em.SetComponentData(building,storage);Require(Project(em,root,runtime,facts).CoreBreached==1,"Civilian reserve violation did not fail");
            storage.StoredFuelBarrels=160;em.SetComponentData(building,storage);em.SetComponentData(building,new UnitHealth{Current=1700,Max=1800});
            facts=Project(em,root,runtime,default);Require(facts.ForwardPostDamaged==1&&facts.ForwardPostDestroyed==0,"Fuel damage not projected");
            em.SetComponentData(building,new UnitHealth{Current=0,Max=1800});Require(Project(em,root,runtime,default).ForwardPostDestroyed==1,"Fuel destruction did not fail");
            em.SetComponentData(building,new UnitHealth{Current=1800,Max=1800});
            Require(Project(em,root,runtime,new CampaignMissionAttemptFactsComponent{ElapsedMilliseconds=240000}).CoreBreached==1,"Deadline not enforced");
            Require(CampaignMissionSequence.IndexOf(CampaignMissionSequence.SteelPush)==16&&CampaignMissionSequence.Next(CampaignMissionSequence.AirCorridor)==CampaignMissionSequence.SteelPush,"Progression mismatch");
            Debug.Log("[SteelPushRules] result=Passed fuel=binding,spending,civilian-floor,damage,destruction deadline=240s progression=17");
        }
        private static void ValidateGroundOnlyContract()
        {
            var steel=AssetDatabase.LoadAssetAtPath<ScenarioSetupConfig>(CH04M02SteelPushConfigBuilder.ScenarioPath);
            Require(steel!=null&&steel.TryValidate(out _),"Steel Push scenario invalid");
            Require(string.IsNullOrEmpty(steel.Defense.SensorMissionRoleId)&&steel.Defense.RadarPingCharges==0&&steel.Defense.GuidanceSteps.Length==4,"Ground-only guidance/radar mismatch");
            bool reserveAllowed=false;foreach(var entry in steel.MissionRuntime.BuildCatalog)
                reserveAllowed|=entry.BuildingConfigId==CH04M02SteelPushConfigBuilder.ReserveBuildingId&&entry.MaxCount==1;
            Require(reserveAllowed,"Fixed reserve is missing from native placement catalogue");
            var previous=AssetDatabase.LoadAssetAtPath<ScenarioSetupConfig>(M03RadarWarningConfigBuilder.ScenarioPath);
            Require(previous!=null&&previous.TryValidate(out _),"Previous radar mission regressed");
            var invalid=UnityEngine.Object.Instantiate(previous);
            try
            {
                var data=new SerializedObject(invalid);data.FindProperty("defense").FindPropertyRelative("radarPingCharges").intValue=0;data.ApplyModifiedPropertiesWithoutUndo();
                Require(!invalid.TryValidate(out _),"Radar mission must still require charges");
            }
            finally{UnityEngine.Object.DestroyImmediate(invalid);}
        }
        private static CampaignMissionAttemptFactsComponent Project(EntityManager em,Entity root,CampaignMissionRuntimeComponent runtime,CampaignMissionAttemptFactsComponent facts)
        {
            object[] arguments={em,root,runtime,facts};
            typeof(CampaignMissionRuntimeSystem).GetMethod("ProjectSteelPushProtection",BindingFlags.NonPublic|BindingFlags.Static).Invoke(null,arguments);
            return (CampaignMissionAttemptFactsComponent)arguments[3];
        }
        private static void Require(bool value,string error){if(!value)throw new InvalidOperationException(error);}
    }
}
