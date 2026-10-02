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
    public static class CH04M05ArmorBreakObjectiveValidation
    {
        public static void Run()
        {
            var authored = AssetDatabase.LoadAssetAtPath<MissionDefinitionConfig>(CH04M05ArmorBreakConfigBuilder.MissionPath);
            Require(authored != null, "Armor Break authored mission missing");
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
            using var world = new World("Armor Break objective writer regression");
            var em = world.EntityManager;
            var root = em.CreateEntity(typeof(CampaignMissionRootComponent), typeof(CampaignMissionCatalogComponent), typeof(CampaignMissionRuntimeComponent), typeof(CampaignMissionAttemptFactsComponent));
            em.SetComponentData(root, new CampaignMissionCatalogComponent { Blob = blob, SourceVersion = 1 });
            em.SetComponentData(root, new CampaignMissionRuntimeComponent { MissionId = authored.MissionId, ScenarioId = authored.ScenarioId,
                OperationMapId = authored.OperationMapId, SessionToken = "armor-objective", Version = 1, SourceVersion = 1, Phase = MissionPhaseKind.Engage });
            var facts = new CampaignMissionAttemptFactsComponent { CommandSquadSpawned = 1, CommandSquadAlive = 1, HostileTotalCount = 12, HostileDefeatedCount = 11, CivilianTotalCount=3 };
            em.SetComponentData(root, facts);
            var boundary = em.CreateEntity(typeof(MatchObjectiveProjectionBoundaryComponent));
            var writer = world.GetOrCreateSystem<CampaignMissionObjectiveProjectionSystem>();
            void Update() => world.Unmanaged.GetUnsafeSystemRef<CampaignMissionObjectiveProjectionSystem>(writer).OnUpdate(ref world.Unmanaged.ResolveSystemStateRef(writer));
            Update();
            Require(em.HasComponent<MatchObjectiveRuntimeStateComponent>(boundary), "Real writer rejected Armor Break authored objective contract");
            Require(em.GetBuffer<MatchObjectiveRuntimeElement>(boundary)[0].State == MatchObjectiveState.Active, "Surviving original military member ignored");
            facts.HostileDefeatedCount=12; em.SetComponentData(root,facts); Update();
            Require(em.GetBuffer<MatchObjectiveRuntimeElement>(boundary)[0].State==MatchObjectiveState.Complete,"Full original military roster did not complete");
            Require(em.GetBuffer<MatchObjectiveRuntimeElement>(boundary)[1].State==MatchObjectiveState.Active,"Military defeat fabricated authority custody");
            facts.ArmorBreakAuthorityRecovered=1; em.SetComponentData(root,facts); Update();
            Require(em.GetBuffer<MatchObjectiveRuntimeElement>(boundary)[1].State==MatchObjectiveState.Complete,"Qualified custody not published");
            facts.ArmorBreakReliefLost=1; facts.CivilianLossCount=1; em.SetComponentData(root,facts); Update();
            Require(em.GetBuffer<MatchObjectiveRuntimeElement>(boundary)[2].State==MatchObjectiveState.Failed,"Original relief loss ignored");
            Require(em.GetBuffer<MatchObjectiveRuntimeElement>(boundary)[2].ProtectsTarget==1,"Relief protection marker missing");
            Require(em.GetBuffer<MatchObjectiveRuntimeElement>(boundary)[1].OperationMapAnchorId.ToString()=="anchor.ch04.m05.authority","Authority marker points at unrelated map anchor");
            Debug.Log("[ArmorBreakObjectives] result=Passed writer=published military=original12 custody=independent relief=protected anchors=qualified");
        }
        private static void Require(bool value, string error) { if (!value) throw new InvalidOperationException(error); }
    }
}
