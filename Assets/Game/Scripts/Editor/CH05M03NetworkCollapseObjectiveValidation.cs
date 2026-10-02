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
    public static class CH05M03NetworkCollapseObjectiveValidation
    {
        public static void Run()
        {
            var authored = AssetDatabase.LoadAssetAtPath<MissionDefinitionConfig>(CH05M03NetworkCollapseConfigBuilder.MissionPath);
            Require(authored != null, "Network Collapse authored mission missing");
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
            using var world = new World("Network Collapse objective writer regression");
            var em = world.EntityManager;
            var root = em.CreateEntity(typeof(CampaignMissionRootComponent), typeof(CampaignMissionCatalogComponent), typeof(CampaignMissionRuntimeComponent), typeof(CampaignMissionAttemptFactsComponent));
            em.SetComponentData(root, new CampaignMissionCatalogComponent { Blob = blob, SourceVersion = 1 });
            em.SetComponentData(root, new CampaignMissionRuntimeComponent { MissionId = authored.MissionId, ScenarioId = authored.ScenarioId,
                OperationMapId = authored.OperationMapId, SessionToken = "network-objective", Version = 1, SourceVersion = 1, Phase = MissionPhaseKind.Engage });
            var facts = new CampaignMissionAttemptFactsComponent { CommandSquadSpawned = 1, CommandSquadAlive = 1, HostileTotalCount = 9, HostileDefeatedCount = 8, CivilianTotalCount=4 };
            em.SetComponentData(root, facts);
            var boundary = em.CreateEntity(typeof(MatchObjectiveProjectionBoundaryComponent));
            var writer = world.GetOrCreateSystem<CampaignMissionObjectiveProjectionSystem>();
            void Update() => world.Unmanaged.GetUnsafeSystemRef<CampaignMissionObjectiveProjectionSystem>(writer).OnUpdate(ref world.Unmanaged.ResolveSystemStateRef(writer));
            Update();
            Require(em.HasComponent<MatchObjectiveRuntimeStateComponent>(boundary), "Real writer rejected Network Collapse authored objective contract");
            Require(em.GetBuffer<MatchObjectiveRuntimeElement>(boundary)[0].State==MatchObjectiveState.Active,"Network starts disabled without actual proof");
            facts.NetworkNodesDisabled=3;facts.NetworkNodesVerified=2;em.SetComponentData(root,facts);Update();Require(em.GetBuffer<MatchObjectiveRuntimeElement>(boundary)[0].State==MatchObjectiveState.Active,"Destruction bypassed ordered verification");
            facts.NetworkNodesVerified=3;em.SetComponentData(root,facts);Update();Require(em.GetBuffer<MatchObjectiveRuntimeElement>(boundary)[0].State==MatchObjectiveState.Complete,"Verified disabled network not published");
            Require(em.GetBuffer<MatchObjectiveRuntimeElement>(boundary)[1].State==MatchObjectiveState.Active,"Node destruction fabricated audit custody");facts.NetworkAuditRecovered=1;em.SetComponentData(root,facts);Update();Require(em.GetBuffer<MatchObjectiveRuntimeElement>(boundary)[1].State==MatchObjectiveState.Complete,"Actual recovered audit not published");
            Require(em.GetBuffer<MatchObjectiveRuntimeElement>(boundary)[2].State==MatchObjectiveState.Active,"Audit custody fabricated extraction");facts.NetworkExtracted=1;em.SetComponentData(root,facts);Update();Require(em.GetBuffer<MatchObjectiveRuntimeElement>(boundary)[2].State==MatchObjectiveState.Complete,"Actual original evidence extraction not published");
            facts.NetworkFailure=NetworkCollapseFailure.AuditLost;facts.NetworkAuditRecovered=0;em.SetComponentData(root,facts);Update();Require(em.GetBuffer<MatchObjectiveRuntimeElement>(boundary)[1].State==MatchObjectiveState.Failed,"Destroyed original audit ignored");
            Require(em.GetBuffer<MatchObjectiveRuntimeElement>(boundary)[1].ProtectsTarget==1,"Audit custody protection marker missing");Require(em.GetBuffer<MatchObjectiveRuntimeElement>(boundary)[2].OperationMapAnchorId.ToString()=="anchor.ch05.m03.extraction","Extraction marker is on unrelated source");
            Debug.Log("[NetworkCollapseObjectives] result=Passed writer=published nodes=verified audit=actual extraction=original protection=qualified");
        }
        private static void Require(bool value, string error) { if (!value) throw new InvalidOperationException(error); }
    }
}
