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
    public static class CH05M04LastCorridorObjectiveValidation
    {
        public static void Run()
        {
            var authored = AssetDatabase.LoadAssetAtPath<MissionDefinitionConfig>(CH05M04LastCorridorConfigBuilder.MissionPath);
            Require(authored != null, "Last Corridor authored mission missing");
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
            using var world = new World("Last Corridor objective writer regression");
            var em = world.EntityManager;
            var root = em.CreateEntity(typeof(CampaignMissionRootComponent), typeof(CampaignMissionCatalogComponent), typeof(CampaignMissionRuntimeComponent), typeof(CampaignMissionAttemptFactsComponent));
            em.SetComponentData(root, new CampaignMissionCatalogComponent { Blob = blob, SourceVersion = 1 });
            em.SetComponentData(root, new CampaignMissionRuntimeComponent { MissionId = authored.MissionId, ScenarioId = authored.ScenarioId,
                OperationMapId = authored.OperationMapId, SessionToken = "corridor-objective", Version = 1, SourceVersion = 1, Phase = MissionPhaseKind.Engage });
            var facts = new CampaignMissionAttemptFactsComponent { CommandSquadSpawned = 1, CommandSquadAlive = 1, HostileTotalCount = 7, HostileDefeatedCount = 6, CivilianTotalCount=4 };
            em.SetComponentData(root, facts);
            var boundary = em.CreateEntity(typeof(MatchObjectiveProjectionBoundaryComponent));
            var writer = world.GetOrCreateSystem<CampaignMissionObjectiveProjectionSystem>();
            void Update() => world.Unmanaged.GetUnsafeSystemRef<CampaignMissionObjectiveProjectionSystem>(writer).OnUpdate(ref world.Unmanaged.ResolveSystemStateRef(writer));
            Update();
            Require(em.HasComponent<MatchObjectiveRuntimeStateComponent>(boundary), "Real writer rejected Last Corridor authored objective contract");
            Require(em.GetBuffer<MatchObjectiveRuntimeElement>(boundary)[0].State == MatchObjectiveState.Active, "Supplies start complete without delivery");
            facts.CorridorSuppliesDelivered = 3; facts.CorridorFuelReceived = 0; em.SetComponentData(root, facts); Update();
            Require(em.GetBuffer<MatchObjectiveRuntimeElement>(boundary)[0].State == MatchObjectiveState.Active, "Delivery label substituted for actual Fuel transfer");
            facts.CorridorFuelReceived = 40; em.SetComponentData(root, facts); Update();
            Require(em.GetBuffer<MatchObjectiveRuntimeElement>(boundary)[0].State == MatchObjectiveState.Complete, "Actual three supply receipts not published");
            Require(em.GetBuffer<MatchObjectiveRuntimeElement>(boundary)[1].State == MatchObjectiveState.Active, "Delivery fabricated link recovery");
            facts.CorridorLinkRecovered = 1; em.SetComponentData(root, facts); Update();
            Require(em.GetBuffer<MatchObjectiveRuntimeElement>(boundary)[1].State == MatchObjectiveState.Complete, "Original engineer recovery not published");
            facts.CorridorKeysDelivered = 1; em.SetComponentData(root, facts); Update();
            Require(em.GetBuffer<MatchObjectiveRuntimeElement>(boundary)[2].State == MatchObjectiveState.Active, "Keys alone substituted for original engineer");
            facts.CorridorEngineerDelivered = 1; em.SetComponentData(root, facts); Update();
            Require(em.GetBuffer<MatchObjectiveRuntimeElement>(boundary)[2].State == MatchObjectiveState.Complete, "Complete original authority custody not published");
            facts.CorridorFailure = LastCorridorFailure.KeysLost; facts.CorridorKeysDelivered = 0; em.SetComponentData(root, facts); Update();
            Require(em.GetBuffer<MatchObjectiveRuntimeElement>(boundary)[2].State == MatchObjectiveState.Failed, "Lost physical keys ignored");
            Require(em.GetBuffer<MatchObjectiveRuntimeElement>(boundary)[2].ProtectsTarget == 1, "Key custody protection marker missing");
            Require(em.GetBuffer<MatchObjectiveRuntimeElement>(boundary)[1].OperationMapAnchorId.ToString() == "anchor.ch05.m04.repair", "Repair marker references unrelated source");
            Debug.Log("[LastCorridorObjectives] result=Passed writer=Actual Fuel=Transferred recovery=Original custody=Both");
        }
        private static void Require(bool value, string error) { if (!value) throw new InvalidOperationException(error); }
    }
}
