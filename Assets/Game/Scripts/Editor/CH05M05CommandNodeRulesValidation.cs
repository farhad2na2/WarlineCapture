using System;
using System.Reflection;
using System.IO;
using Game.Configs;
using UnityEditor;
using Game.Components;
using Game.Missions.Contracts;
using Game.Runtime;
using Game.UI.Contracts;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
namespace Game.Editor
{
    public static class CH05M05CommandNodeRulesValidation
    {
        public static void Run()
        {
            var runtime = new CampaignMissionRuntimeComponent { MissionId = CampaignMissionSequence.CommandNode, SessionToken = "command-rules", AttemptOrdinal = 1, SourceVersion = 3, Version = 1, Phase = MissionPhaseKind.Engage };
            var s = new CampaignMissionCommandNodeState { SessionToken = runtime.SessionToken, AttemptOrdinal = 1, SourceVersion = 3, Initialized = 1, Ready = 1, ExteriorCleared = 1, ClinicIsolated = 1, UtilityIsolated = 1, NodeDisabled = 1, Breached = 1, QassemDefeated = 1, MilitaryCleared = 1, AuditPreserved = 1, ReleaseOrdered = 1, AuditReleased = 1, SpecialistsSafe = 1 };
            var facts = new CampaignMissionAttemptFactsComponent { HostileTotalCount = 10, HostileDefeatedCount = 10, CivilianTotalCount = 4 };
            Require(CampaignMissionCommandNodeRuleUtility.TryAdvance(in runtime, in facts, in s, true, out var won) && won.Outcome == MissionOutcomeKind.Victory, "Qualified bounded finale victory rejected");
            Require(!CampaignMissionCommandNodeRuleUtility.TryAdvance(in won, in facts, in s, true, out _), "Terminal outcome repeated");
            var bad = s; bad.ReleaseOrdered = 0; Require(!CampaignMissionCommandNodeRuleUtility.CanRelease(in bad) && !CampaignMissionCommandNodeRuleUtility.VictoryQualified(in bad, in facts), "Prepositioned engineer substituted for explicit release order");
            bad = s; bad.UtilityIsolated = 0; Require(!CampaignMissionCommandNodeRuleUtility.VictoryQualified(in bad, in facts), "Unisolated service accepted");
            bad = s; bad.AttemptOrdinal++; Require(!CampaignMissionCommandNodeRuleUtility.TryAdvance(in runtime, in facts, in bad, true, out _), "Stale attempt accepted");
            bad = s; bad.SourceVersion++; Require(!CampaignMissionCommandNodeRuleUtility.TryAdvance(in runtime, in facts, in bad, true, out _), "Stale source accepted");
            bad = s; bad.SessionToken = "other-session"; Require(!CampaignMissionCommandNodeRuleUtility.TryAdvance(in runtime, in facts, in bad, true, out _), "Stale session accepted");
            foreach (CommandNodeFailure failure in Enum.GetValues(typeof(CommandNodeFailure))) if (failure != CommandNodeFailure.None) { bad = s; bad.Failure = failure; Require(CampaignMissionCommandNodeRuleUtility.TryAdvance(in runtime, in facts, in bad, true, out var failed) && failed.Outcome == MissionOutcomeKind.Defeat, "Failure did not precede apparent finale completion: " + failure); }
            Require(CampaignMissionCommandNodeRuleUtility.AdvanceHold(5999, 10, false) == 0, "Interrupted hold did not reset");
            ValidateCover(); ValidateReleaseReceipt(runtime); ValidateOriginalSuppression(runtime); ValidateResultSettlement(runtime); ValidateWatchProgress(); ValidateTutorialMapping();
            Debug.Log("[CommandNodeRules] result=Passed release=AcceptedNormalMove stale=Rejected holds=Continuous progress=Unique");
        }
        private static void ValidateCover()
        {
            using var world = new World("Command Node real armored escort cover"); var em = world.EntityManager; var root = em.CreateEntity();
            var actor = em.CreateEntity(typeof(UnitHealth), typeof(LocalTransform), typeof(UnitSourcePrefabKey), typeof(UnitCombat), typeof(UnitAttack));
            em.SetComponentData(actor, new UnitHealth { Max = 1250, Current = 1250 });
            em.SetComponentData(actor, new UnitSourcePrefabKey { Value = "unit_veh_tank_usa" });
            em.SetComponentData(actor, new UnitCombat { CanAttack = 1 }); em.SetComponentData(actor, new UnitAttack { Damage = 223, Range = 120 });
            var mission = new CampaignMissionCommandNodeState { BreachGate = new float3(1100,0,440) };
            em.SetComponentData(actor, LocalTransform.FromPosition(new float3(1100,0,460)));
            em.AddBuffer<CampaignMissionCommandNodeMember>(root).Add(new CampaignMissionCommandNodeMember { Entity = actor, Kind = CommandNodeMemberKind.Escort });
            Require(CampaignMissionRuntimeSystem.CommandCoverReady(em, root, in mission), "Living stopped original tank did not qualify cover");
            em.AddComponentData(actor, new UnitPathRequest { Goal = new int2(240,160) });
            Require(!CampaignMissionRuntimeSystem.CommandCoverReady(em, root, in mission), "Moving tank qualified stopped cover");
            em.RemoveComponent<UnitPathRequest>(actor); em.SetComponentData(actor, LocalTransform.FromPosition(new float3(1100,0,440)));
            Require(!CampaignMissionRuntimeSystem.CommandCoverReady(em, root, in mission), "Tank behind engineer qualified forward cover");
            em.SetComponentData(actor, LocalTransform.FromPosition(new float3(1100,0,460))); em.SetComponentData(actor, new UnitSourcePrefabKey { Value = "unit_chr_soldier_male_02_alt_02" });
            Require(!CampaignMissionRuntimeSystem.CommandCoverReady(em, root, in mission), "Infantry substituted for armored cover");
            em.SetComponentData(actor, new UnitSourcePrefabKey { Value = "unit_veh_tank_usa" }); em.SetComponentData(actor, new UnitHealth { Max = 1250, Current = 0 });
            Require(!CampaignMissionRuntimeSystem.CommandCoverReady(em, root, in mission), "Destroyed tank preserved cover");
            Require(CampaignMissionCommandNodeRuleUtility.AdvanceHold(5999, 1, false) == 0, "Lost cover completed breach hold");
        }
        private static void ValidateOriginalSuppression(CampaignMissionRuntimeComponent runtime)
        {
            using var world = new World("Command Node actual actor initialization and combat isolation"); var em = world.EntityManager; var root = em.CreateEntity();
            var groups = new (string role, int count)[] { ("role.friendly.escort",6), ("role.friendly.engineer",1), ("role.friendly.specialist",2), ("role.hostile.exterior",4), ("role.hostile.node",1), ("role.hostile.core",4), ("role.hostile.qassem",1), ("role.civilian.protected",4) };
            foreach (var group in groups) for (int i = 0; i < group.count; i++) { var actor = em.CreateEntity(typeof(CampaignMissionUnitRoleComponent), typeof(UnitHealth), typeof(LocalTransform)); em.SetComponentData(actor, new CampaignMissionUnitRoleComponent { SessionToken = runtime.SessionToken, MissionRoleId = new FixedString64Bytes(group.role) }); em.SetComponentData(actor, new UnitHealth { Max = 100, Current = 100 }); }
            using var builder = new BlobBuilder(Allocator.Temp); ref var map = ref builder.ConstructRoot<OperationMapBlob>();
            var names = new[] { "isolate_clinic", "isolate_utility", "breach", "core_audit", "audit_release", "safe_receiving" }; var anchors = builder.Allocate(ref map.Anchors, names.Length);
            for (int i = 0; i < names.Length; i++) anchors[i] = new OperationMapAnchorBlob { Id = new FixedString64Bytes("anchor.ch05.m05." + names[i]), Position = new float3(i*20,0,0), Radius = 6 };
            using var blob = builder.CreateBlobAssetReference<OperationMapBlob>(Allocator.Temp);
            CampaignMissionRuntimeSystem.InitializeCommandNode(em, root, in runtime, ref blob.Value);
            var mission = em.GetComponentData<CampaignMissionCommandNodeState>(root);
            Require(mission.Failure == CommandNodeFailure.None && em.GetBuffer<CampaignMissionCommandNodeMember>(root).Length == 23, "Actual original finale initialization failed");
            var combat = typeof(UnitAttackSystem).GetMethod("IsTargetAvailableForCombat", BindingFlags.NonPublic | BindingFlags.Static);
            Require(combat != null && !(bool)combat.Invoke(null, new object[] {em, mission.Engineer, mission.Node}), "Unisolated node accepted by actual ordinary combat gate");
            Require(!(bool)combat.Invoke(null, new object[] {em, mission.Engineer, mission.Qassem}), "Core activated before engineer breach");
            em.RemoveComponent<CampaignMissionCombatSuppressedTag>(mission.Node);
            Require((bool)combat.Invoke(null, new object[] {em, mission.Engineer, mission.Node}), "Qualified isolated node remains untargetable");
        }
        private static void ValidateReleaseReceipt(CampaignMissionRuntimeComponent runtime)
        {
            using var world = new World("Command Node actual normal order receipt"); var em = world.EntityManager;
            var actor = em.CreateEntity(typeof(UnitHealth), typeof(LocalTransform), typeof(CampaignMissionUnitRoleComponent));
            em.SetComponentData(actor, new UnitHealth { Max = 100, Current = 100 });
            em.SetComponentData(actor, new CampaignMissionUnitRoleComponent { SessionToken = runtime.SessionToken, MissionRoleId = "role.friendly.engineer" });
            var mission = new CampaignMissionCommandNodeState { SessionToken = runtime.SessionToken, AttemptOrdinal = runtime.AttemptOrdinal, SourceVersion = runtime.SourceVersion, Initialized = 1, Engineer = actor, AuditPreserved = 1, AuditRelease = new float3(1120, 0, 476), ReleaseOrderBaseline = 10 };
            var grid = new OperationMapGridBlob { Origin = new float3(860,0,300), CellSize = 1, Dimensions = new int2(440,220) };
            var owner = em.CreateEntity(); var receipts = em.AddBuffer<UnitMoveOrderResultElement>(owner);
            int2 gate = CampaignMissionSpawnSystem.ToGridCell(mission.AuditRelease, in grid);
            var accepted = new UnitMoveOrderResultElement { RequestId = 10, Entity = actor, Goal = gate, Kind = UnitMoveOrderRequestKind.GroupedManual, Issued = 1 };
            receipts.Add(accepted);
            Require(!CampaignMissionRuntimeSystem.HasCommandReleaseOrder(em, in runtime, in mission, in grid), "Old prepositioning order granted release");
            receipts.Clear(); accepted.RequestId = 11; accepted.Issued = 0; receipts.Add(accepted);
            Require(!CampaignMissionRuntimeSystem.HasCommandReleaseOrder(em, in runtime, in mission, in grid), "Refused move granted release");
            receipts.Clear(); accepted.Issued = 1; accepted.Kind = UnitMoveOrderRequestKind.Immediate; receipts.Add(accepted);
            Require(!CampaignMissionRuntimeSystem.HasCommandReleaseOrder(em, in runtime, in mission, in grid), "System movement granted player authority");
            receipts.Clear(); accepted.Kind = UnitMoveOrderRequestKind.GroupedManual; accepted.Goal += new int2(20,0); receipts.Add(accepted);
            Require(!CampaignMissionRuntimeSystem.HasCommandReleaseOrder(em, in runtime, in mission, in grid), "Unrelated move granted release");
            receipts.Clear(); accepted.Goal = gate; receipts.Add(accepted);
            Require(CampaignMissionRuntimeSystem.HasCommandReleaseOrder(em, in runtime, in mission, in grid), "Accepted normal release move not observed");
            mission.AttemptOrdinal++;
            Require(!CampaignMissionRuntimeSystem.HasCommandReleaseOrder(em, in runtime, in mission, in grid), "Stale receipt accepted");
        }
        private static void ValidateWatchProgress()
        {
            var slots = new System.Collections.Generic.HashSet<int>();
            for (int kills = 0; kills <= 4; kills++) Require(slots.Add(UiCommandNodeProgress.WatchGoal(1, kills, 0, false) % 64), "Exterior progress aliases");
            for (int isolation = 0; isolation <= 2; isolation++) Require(slots.Add(UiCommandNodeProgress.WatchGoal(2, 4, isolation, false) % 64), "Isolation progress aliases");
            foreach (int stage in new[] {3,6,8}) Require(slots.Add(UiCommandNodeProgress.WatchGoal(stage, 5, 2, false) % 64), "Finale stage aliases");
            foreach (bool covered in new[] {false,true}) Require(slots.Add(UiCommandNodeProgress.WatchGoal(4, 5, 2, false, covered) % 64), "Cover-to-engineer handoff aliases");
            for (int kills = 5; kills <= 10; kills++) Require(slots.Add(UiCommandNodeProgress.WatchGoal(5, kills, 2, false) % 64), "Core progress aliases");
            foreach (bool ordered in new[] {false,true}) Require(slots.Add(UiCommandNodeProgress.WatchGoal(7, 10, 2, ordered) % 64), "Bounded authority aliases");
            var session = new Game.UI.Shell.Contracts.Ecs.AriaPlaySessionComponent { Phase = AriaPlayPhase.Observing };
            var observation = new Game.UI.Shell.Contracts.Ecs.AriaPlayObservationComponent { Kind = AriaPlayObservationKind.Waiting };
            foreach (var milestone in new[] {(2,0,1f),(2,1,170f),(3,2,300f)}) { observation.GoalId = UiCommandNodeProgress.WatchGoal(milestone.Item1, 4, milestone.Item2, false); observation.Time = milestone.Item3; Game.UI.Shell.Ecs.AriaPlayDecisionSystem.Step(observation, ref session); Require(session.Phase != AriaPlayPhase.Blocked, "Real isolation progression stalled ARIA watchdog"); }
            observation.Time = 481; Game.UI.Shell.Ecs.AriaPlayDecisionSystem.Step(observation, ref session);
            Require(session.Phase == AriaPlayPhase.Blocked, "Unchanged goal bypassed progress timeout");
        }
        private static void ValidateTutorialMapping()
        {
            var utility = typeof(Game.UI.Shell.Ecs.AssistantObjectiveProjectionUtility);
            var step = utility.GetMethod("TutorialStepFor", BindingFlags.NonPublic | BindingFlags.Static);
            var count = utility.GetMethod("TutorialStepCountFor", BindingFlags.NonPublic | BindingFlags.Static);
            for (int i = 1; i <= 8; i++) { var prompt = (CampaignMissionGuidancePromptKind)(132+i); Require((byte)step.Invoke(null, new object[] {prompt}) == i && (byte)count.Invoke(null, new object[] {prompt}) == 8, "Finale tutorial step mapping missing"); }
        }
        private static void ValidateResultSettlement(CampaignMissionRuntimeComponent runtime)
        {
            var authored=AssetDatabase.LoadAssetAtPath<MissionDefinitionConfig>(CH05M05CommandNodeConfigBuilder.MissionPath);
            Require(authored!=null,"Authored result contract missing");
            runtime.MissionId = authored.MissionId; runtime.ScenarioId = authored.ScenarioId; runtime.OperationMapId = authored.OperationMapId;
            using var builder=new BlobBuilder(Allocator.Temp);
            ref var catalog=ref builder.ConstructRoot<CampaignMissionCatalogBlob>();
            var definitions=builder.Allocate(ref catalog.Missions,1);
            ref var definition=ref definitions[0];
            definition.MissionId=authored.MissionId;definition.ScenarioId=authored.ScenarioId;
            definition.OperationMapId=authored.OperationMapId;definition.Defense.Enabled=1;
            var objectives=builder.Allocate(ref definition.Objectives,authored.Objectives.Length);
            for(int i=0;i<objectives.Length;i++)
            {
                var objective=authored.Objectives[i];
                objectives[i]=new CampaignMissionObjectiveBlob{ObjectiveId=objective.ObjectiveId,Rule=objective.Rule,
                    RequiredCount=objective.RequiredCount,MissionRoleId=objective.MissionRoleId,TargetConfigId=objective.TargetConfigId??string.Empty};
            }
            var stars=builder.Allocate(ref definition.StarRules,authored.Stars.Length);
            for(int i=0;i<stars.Length;i++)stars[i]=new CampaignMissionStarRuleBlob{StarIndex=authored.Stars[i].StarIndex,
                Rule=authored.Stars[i].Rule,Threshold=authored.Stars[i].Threshold,DisplayTextKey=authored.Stars[i].DisplayTextKey};
            var rewards=builder.Allocate(ref definition.FirstClearRewards,authored.FirstClearRewards.Length);
            for(int i=0;i<rewards.Length;i++)rewards[i]=new CampaignMissionRewardBlob{Kind=authored.FirstClearRewards[i].Kind,
                RewardConfigId=authored.FirstClearRewards[i].RewardConfigId??string.Empty,Amount=authored.FirstClearRewards[i].Amount,
                DisplayTextKey=authored.FirstClearRewards[i].DisplayTextKey};
            using var blob=builder.CreateBlobAssetReference<CampaignMissionCatalogBlob>(Allocator.Persistent);
            using var world=new World("Command Node actual result and reward settlement");
            var em=world.EntityManager;
            var root=em.CreateEntity(typeof(CampaignMissionRootComponent),typeof(CampaignMissionCatalogComponent),typeof(CampaignMissionRuntimeComponent),typeof(CampaignMissionAttemptFactsComponent));
            runtime.Phase=MissionPhaseKind.Result;runtime.Outcome=MissionOutcomeKind.Victory;
            runtime.ReturnDestination=MissionReturnDestinationKind.CampaignOperations;runtime.RunKind=MissionRunKind.FirstClear;
            runtime.LaunchOrigin=MissionLaunchOriginKind.CampaignOperations;
            // Reproduce the actual mission facts: no generic defense forward post exists.
            var facts=new CampaignMissionAttemptFactsComponent { CommandSquadSpawned=1,CommandSquadAlive=1,HostileTotalCount=10,HostileDefeatedCount=10,
                CivilianTotalCount=4,ElapsedMilliseconds=300000,CommandNodeNetworkSeparated=1,CommandNodeAuditReleased=1,CommandNodeSpecialistsSafe=1,CommandNodeServicesIntact=1 };
            ref var published=ref blob.Value.Missions[0];
            Require(CampaignMissionResultProjectionSystem.TryProject(in runtime,in facts,ref published,out var projected)&&projected.Stars==3,"Qualified Command Node victory rejected by generic defense result gate");
            var incomplete=facts;incomplete.CommandNodeNetworkSeparated=0;
            Require(!CampaignMissionResultProjectionSystem.TryProject(in runtime,in incomplete,ref published,out _),"Result accepted unisolated network");
            incomplete=facts;incomplete.CommandNodeAuditReleased=0;
            Require(!CampaignMissionResultProjectionSystem.TryProject(in runtime,in incomplete,ref published,out _),"Result accepted unreleased audit");
            incomplete=facts;incomplete.HostileDefeatedCount=9;
            Require(!CampaignMissionResultProjectionSystem.TryProject(in runtime,in incomplete,ref published,out _),"Result ignored surviving original hostile");
            incomplete=facts;incomplete.CommandNodeFailure=CommandNodeFailure.StaffLost;incomplete.CivilianLossCount=1;
            Require(!CampaignMissionResultProjectionSystem.TryProject(in runtime,in incomplete,ref published,out _),"Result ignored original civic staff loss");
            em.SetComponentData(root,runtime);em.SetComponentData(root,facts);
            em.SetComponentData(root,new CampaignMissionCatalogComponent{Blob=blob,SourceVersion=runtime.SourceVersion});
            em.AddBuffer<CampaignMissionSettlementRequestElement>(root);
            var writer=world.GetOrCreateSystem<CampaignMissionResultProjectionSystem>();
            void Update()=>world.Unmanaged.GetUnsafeSystemRef<CampaignMissionResultProjectionSystem>(writer).OnUpdate(ref world.Unmanaged.ResolveSystemStateRef(writer));
            Update();Update();
            Require(em.HasComponent<CampaignMissionResultComponent>(root)&&em.GetBuffer<CampaignMissionSettlementRequestElement>(root).Length==1,"Real result writer failed or queued duplicate settlement");
            string savePath=Path.Combine(Path.GetTempPath(),"command-result-regression-"+Guid.NewGuid().ToString("N"));
            try
            {
                var save=new SaveService(new JsonSaveRepository(savePath));var store=new CampaignMissionProgressStore(save);
                store.EnsureAvailable(authored.MissionId);var before=save.LoadProfile();
                var request=em.GetBuffer<CampaignMissionSettlementRequestElement>(root)[0];
                var result=em.GetComponentData<CampaignMissionResultComponent>(root);
                var stale=request;stale.AttemptOrdinal--;
                Require(CampaignMissionProgressSettlementSystem.Settle(store,in stale,in runtime,in result,ref published).Accepted==0,"Prior retry reward settlement accepted");
                var receipt=CampaignMissionProgressSettlementSystem.Settle(store,in request,in runtime,in result,ref published);
                Require(receipt.Accepted!=0&&receipt.FirstClear!=0,"Real Command Node first-clear settlement rejected");
                var earned=save.LoadProfile();

                Require(earned.commanderXp>before.commanderXp&&earned.credits>before.credits,"Authored Command Node rewards not granted");
                Require(CampaignMissionProgressSettlementSystem.Settle(store,in request,in runtime,in result,ref published).Accepted!=0,"Duplicate receipt not recognized");
                var duplicate=save.LoadProfile();

                Require(duplicate.commanderXp==earned.commanderXp&&duplicate.credits==earned.credits,"Duplicate result paid rewards twice");
            }
            finally {if(Directory.Exists(savePath))Directory.Delete(savePath,true);}
        }
        private static void Require(bool value, string error) { if (!value) throw new InvalidOperationException(error); }
    }
}
