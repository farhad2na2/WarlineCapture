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
    public static class CH04M04GroundedSignalObjectiveValidation
    {
        public static void Run()
        {
            var authored = AssetDatabase.LoadAssetAtPath<MissionDefinitionConfig>(CH04M04GroundedSignalConfigBuilder.MissionPath);
            Require(authored != null, "Grounded Signal authored mission missing");
            using var builder = new BlobBuilder(Allocator.Temp);
            ref var catalogBlob = ref builder.ConstructRoot<CampaignMissionCatalogBlob>();
            var definitions = builder.Allocate(ref catalogBlob.Missions, 1);
            definitions[0].MissionId = authored.MissionId; definitions[0].ScenarioId = authored.ScenarioId;
            definitions[0].OperationMapId = authored.OperationMapId; definitions[0].Extraction.Enabled = 1;
            var objectives = builder.Allocate(ref definitions[0].Objectives, authored.Objectives.Length);
            for (int i = 0; i < objectives.Length; i++)
            {
                var source = authored.Objectives[i];
                objectives[i] = new CampaignMissionObjectiveBlob { ObjectiveId = source.ObjectiveId, DisplayTextKey = source.DisplayTextKey,
                    MissionRoleId = source.MissionRoleId, TargetConfigId = source.TargetConfigId ?? string.Empty,
                    Rule = source.Rule, RequiredCount = source.RequiredCount, FailureOnRuleBreak = source.FailureOnRuleBreak ? (byte)1 : (byte)0 };
            }
            using var blob = builder.CreateBlobAssetReference<CampaignMissionCatalogBlob>(Allocator.Persistent);
            using var world = new World("Grounded Signal objective writer regression");
            var em = world.EntityManager;
            var root = em.CreateEntity(typeof(CampaignMissionRootComponent), typeof(CampaignMissionCatalogComponent), typeof(CampaignMissionRuntimeComponent), typeof(CampaignMissionAttemptFactsComponent));
            em.SetComponentData(root, new CampaignMissionCatalogComponent { Blob = blob, SourceVersion = 1 });
            em.SetComponentData(root, new CampaignMissionRuntimeComponent { MissionId = authored.MissionId, ScenarioId = authored.ScenarioId,
                OperationMapId = authored.OperationMapId, SessionToken = "grounded-objective", Version = 1, SourceVersion = 1, Phase = MissionPhaseKind.Engage });
            var facts = new CampaignMissionAttemptFactsComponent { CommandSquadSpawned = 1, CommandSquadAlive = 1, HostileTotalCount = 4, HostileDefeatedCount = 3 };
            em.SetComponentData(root, facts);
            var boundary = em.CreateEntity(typeof(MatchObjectiveProjectionBoundaryComponent));
            var writer = world.GetOrCreateSystem<CampaignMissionObjectiveProjectionSystem>();
            void Update() => world.Unmanaged.GetUnsafeSystemRef<CampaignMissionObjectiveProjectionSystem>(writer).OnUpdate(ref world.Unmanaged.ResolveSystemStateRef(writer));
            Update();
            Require(em.HasComponent<MatchObjectiveRuntimeStateComponent>(boundary), "Real writer rejected one relay alongside three guards");
            Require(em.GetBuffer<MatchObjectiveRuntimeElement>(boundary)[1].State == MatchObjectiveState.Active, "Guard deaths falsely completed relay objective");
            facts.GroundedSignalRelayDisabled = 1; facts.HostileDefeatedCount = 1; em.SetComponentData(root, facts); Update();
            Require(em.GetBuffer<MatchObjectiveRuntimeElement>(boundary)[1].State == MatchObjectiveState.Active, "Same-attempt regressing death facts were accepted");
            // A fresh attempt isolates the relay-only result from the guards-only case.
            // Never decrement an existing attempt's observed deaths to fabricate this state.
            var freshRuntime = em.GetComponentData<CampaignMissionRuntimeComponent>(root);
            freshRuntime.SessionToken = "grounded-relay-only"; freshRuntime.AttemptOrdinal = 1; freshRuntime.Version++;
            em.SetComponentData(root, freshRuntime);
            var observedFacts = em.GetComponentData<CampaignMissionAttemptFactsComponent>(root);
            var observedRuntime = em.GetComponentData<CampaignMissionRuntimeComponent>(root);
            ref var beforeDefinition = ref blob.Value.Missions[0];
            var beforeState = em.GetComponentData<MatchObjectiveRuntimeStateComponent>(boundary);
            Require(observedFacts.GroundedSignalRelayDisabled == 1 && observedFacts.HostileDefeatedCount == 1, "Test facts did not reach ECS");
            Require(CampaignMissionObjectiveProjectionSystem.IsPublishable(in observedRuntime, in observedFacts, ref beforeDefinition), "Fresh relay attempt facts not publishable");
            Require(!CampaignMissionObjectiveProjectionSystem.IsStale(in beforeState, in observedRuntime, in observedFacts), "Fresh relay attempt classified stale");
            Update();
            Require(em.GetBuffer<MatchObjectiveRuntimeElement>(boundary)[1].State == MatchObjectiveState.Complete, $"Disabled relay waited for unrelated guards: publishedSession={em.GetComponentData<MatchObjectiveRuntimeStateComponent>(boundary).SessionToken} expectedSession={freshRuntime.SessionToken} stateVersion={em.GetComponentData<MatchObjectiveRuntimeStateComponent>(boundary).Version} role={blob.Value.Missions[0].Objectives[1].MissionRoleId} flag={em.GetComponentData<CampaignMissionAttemptFactsComponent>(root).GroundedSignalRelayDisabled} state={em.GetBuffer<MatchObjectiveRuntimeElement>(boundary)[1].State}");
            facts.GroundedSignalTerminalLost = 1; em.SetComponentData(root, facts); Update();
            Require(em.GetBuffer<MatchObjectiveRuntimeElement>(boundary)[2].State == MatchObjectiveState.Failed, "Staff loss ignored while escort alive");
            ref var definition = ref blob.Value.Missions[0];
            var runtime = em.GetComponentData<CampaignMissionRuntimeComponent>(root);
            var relay = definition.Objectives[1]; relay.MissionRoleId = "role.hostile.unrelated"; definition.Objectives[1] = relay;
            Require(!CampaignMissionObjectiveProjectionSystem.IsPublishable(in runtime, in facts, ref definition), "Wrong role bypassed whole-roster validation");
            Debug.Log("[GroundedSignalObjectives] result=Passed writer=published relay=exact-role guards=independent terminal=original-staff generic-contract=preserved");
        }
        private static void Require(bool value, string error) { if (!value) throw new InvalidOperationException(error); }
    }
}
