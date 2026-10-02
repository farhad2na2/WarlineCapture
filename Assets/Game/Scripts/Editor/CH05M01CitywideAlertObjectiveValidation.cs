using System;
using Game.Components;
using Game.Configs;
using Game.Missions.Contracts;
using Game.Runtime;
using Unity.Collections;
using Unity.Entities;
using UnityEditor;
using UnityEngine;

namespace Game.Editor
{
    public static class CH05M01CitywideAlertObjectiveValidation
    {
        public static void Run()
        {
            var authored = AssetDatabase.LoadAssetAtPath<MissionDefinitionConfig>(CH05M01CitywideAlertConfigBuilder.MissionPath);
            Require(authored != null, "Citywide Alert authored mission missing");
            using var builder = new BlobBuilder(Allocator.Temp);
            ref var catalogBlob = ref builder.ConstructRoot<CampaignMissionCatalogBlob>();
            var definitions = builder.Allocate(ref catalogBlob.Missions, 1);
            definitions[0].MissionId = authored.MissionId; definitions[0].ScenarioId = authored.ScenarioId;
            definitions[0].OperationMapId = authored.OperationMapId; definitions[0].Defense.Enabled = 1;
            var objectives = builder.Allocate(ref definitions[0].Objectives, authored.Objectives.Length);
            for (int i = 0; i < objectives.Length; i++)
            {
                var source = authored.Objectives[i];
                objectives[i] = new CampaignMissionObjectiveBlob { ObjectiveId = source.ObjectiveId, DisplayTextKey = source.DisplayTextKey,
                    MissionRoleId = source.MissionRoleId, TargetConfigId = source.TargetConfigId ?? string.Empty,
                    Rule = source.Rule, RequiredCount = source.RequiredCount, FailureOnRuleBreak = source.FailureOnRuleBreak ? (byte)1 : (byte)0 };
            }
            using var blob = builder.CreateBlobAssetReference<CampaignMissionCatalogBlob>(Allocator.Persistent);
            using var world = new World("Citywide Alert objective writer regression");
            var em = world.EntityManager;
            var root = em.CreateEntity(typeof(CampaignMissionRootComponent), typeof(CampaignMissionCatalogComponent), typeof(CampaignMissionRuntimeComponent), typeof(CampaignMissionAttemptFactsComponent));
            em.SetComponentData(root, new CampaignMissionCatalogComponent { Blob = blob, SourceVersion = 1 });
            em.SetComponentData(root, new CampaignMissionRuntimeComponent { MissionId = authored.MissionId, ScenarioId = authored.ScenarioId,
                OperationMapId = authored.OperationMapId, SessionToken = "citywide-objective", Version = 1, SourceVersion = 1, Phase = MissionPhaseKind.Engage });
            var facts = new CampaignMissionAttemptFactsComponent { CommandSquadSpawned = 1, CommandSquadAlive = 1, HostileTotalCount = 11, HostileDefeatedCount = 10, CivilianTotalCount=4 };
            em.SetComponentData(root, facts);
            var boundary = em.CreateEntity(typeof(MatchObjectiveProjectionBoundaryComponent));
            var writer = world.GetOrCreateSystem<CampaignMissionObjectiveProjectionSystem>();
            void Update() => world.Unmanaged.GetUnsafeSystemRef<CampaignMissionObjectiveProjectionSystem>(writer).OnUpdate(ref world.Unmanaged.ResolveSystemStateRef(writer));
            Update();
            Require(em.HasComponent<MatchObjectiveRuntimeStateComponent>(boundary), "Real writer rejected Citywide Alert authored objective contract");
            Require(em.GetBuffer<MatchObjectiveRuntimeElement>(boundary)[0].State==MatchObjectiveState.Active,"Clinic started recovered without engineer hold");
            facts.CitywideClinicRecovered=1;em.SetComponentData(root,facts);Update();
            Require(em.GetBuffer<MatchObjectiveRuntimeElement>(boundary)[0].State==MatchObjectiveState.Complete,"Actual clinic recovery not published");
            Require(em.GetBuffer<MatchObjectiveRuntimeElement>(boundary)[1].State==MatchObjectiveState.Active,"Clinic substituted for independent utility recovery");
            facts.CitywideUtilityRecovered=1;facts.HostileDefeatedCount=11;facts.CitywideMilitaryCleared=1;em.SetComponentData(root,facts);Update();
            Require(em.GetBuffer<MatchObjectiveRuntimeElement>(boundary)[1].State==MatchObjectiveState.Complete,"Actual utility recovery not published");
            Require(em.GetBuffer<MatchObjectiveRuntimeElement>(boundary)[2].State==MatchObjectiveState.Active,"Military defeat substituted for recruited perimeter");
            facts.CitywideReinforcementReady=1;facts.CitywideStableMilliseconds=6000;em.SetComponentData(root,facts);Update();
            Require(em.GetBuffer<MatchObjectiveRuntimeElement>(boundary)[2].State==MatchObjectiveState.Complete,"Real stable perimeter not published");
            facts.CitywideFailure=CitywideAlertFailure.ClinicLost;facts.CitywideClinicRecovered=0;em.SetComponentData(root,facts);Update();
            Require(em.GetBuffer<MatchObjectiveRuntimeElement>(boundary)[0].State==MatchObjectiveState.Failed,"Real service loss ignored");
            Require(em.GetBuffer<MatchObjectiveRuntimeElement>(boundary)[0].ProtectsTarget==1,"Clinic protection marker missing");
            Require(em.GetBuffer<MatchObjectiveRuntimeElement>(boundary)[1].OperationMapAnchorId.ToString()=="anchor.ch05.m01.utility","Utility marker points at unrelated map anchor");
            Debug.Log("[CitywideObjectives] result=Passed writer=published services=independent perimeter=actual protection=qualified");
        }
        private static void Require(bool value, string error) { if (!value) throw new InvalidOperationException(error); }
    }
}
