using System;
using System.IO;
using System.Reflection;
using Game.Components;
using Game.Configs;
using Game.Missions.Contracts;
using Game.Runtime;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEditor;
using UnityEngine;
namespace Game.Editor
{
    public static class CH05M04LastCorridorRulesValidation
    {
        public static void Run()
        {
            var runtime = new CampaignMissionRuntimeComponent { MissionId = CampaignMissionSequence.LastCorridor, ScenarioId = "scenario.ch05.m04.last_corridor", OperationMapId = "opmap.ch05.last_corridor", SessionToken = "corridor-rules", AttemptOrdinal = 1, SourceVersion = 3, Version = 1, Phase = MissionPhaseKind.Engage, DeterministicSeed = 1 };
            var s = new CampaignMissionLastCorridorState { SessionToken = runtime.SessionToken, AttemptOrdinal = 1, SourceVersion = 3, Initialized = 1, Ready = 1, MilitaryCleared = 1, LinkRecovered = 1, MedicineDelivered = 1, FuelDelivered = 1, ReinforcementsDelivered = 1, EngineerAboard = 1, EngineerDelivered = 1, KeysDelivered = 1, DeliveredFuelBarrels = 40, KeyMilliseconds = 6000 };
            var facts = new CampaignMissionAttemptFactsComponent { HostileTotalCount = 7, HostileDefeatedCount = 7, CivilianTotalCount = 4 };
            Require(CampaignMissionLastCorridorRuleUtility.TryAdvance(in runtime, in facts, in s, true, out var won) && won.Outcome == MissionOutcomeKind.Victory, "Qualified five-category victory rejected");
            Require(!CampaignMissionLastCorridorRuleUtility.TryAdvance(in won, in facts, in s, true, out _), "Terminal outcome repeated");
            var bad = s; bad.DeliveredFuelBarrels = 0; Require(!CampaignMissionLastCorridorRuleUtility.VictoryQualified(in bad, in facts), "Fuel label substituted for real transfer");
            bad = s; bad.EngineerAboard = 0; Require(!CampaignMissionLastCorridorRuleUtility.VictoryQualified(in bad, in facts), "Keys substituted for original engineer occupancy");
            bad = s; bad.AttemptOrdinal++; Require(!CampaignMissionLastCorridorRuleUtility.TryAdvance(in runtime, in facts, in bad, true, out _), "Stale attempt accepted");
            bad = s; bad.SourceVersion++; Require(!CampaignMissionLastCorridorRuleUtility.TryAdvance(in runtime, in facts, in bad, true, out _), "Stale source accepted");
            bad = s; bad.SessionToken = "other-session"; Require(!CampaignMissionLastCorridorRuleUtility.TryAdvance(in runtime, in facts, in bad, true, out _), "Stale session accepted");
            foreach (LastCorridorFailure failure in Enum.GetValues(typeof(LastCorridorFailure))) if (failure != LastCorridorFailure.None) { bad = s; bad.Failure = failure; Require(CampaignMissionLastCorridorRuleUtility.TryAdvance(in runtime, in facts, in bad, true, out var failed) && failed.Outcome == MissionOutcomeKind.Defeat, "Failure did not precede apparent deliveries: " + failure); }
            Require(CampaignMissionLastCorridorRuleUtility.AdvanceHold(5999, 10, false) == 0, "Off-zone hold did not reset");
            float3 entry = new(0,0,0), middle = new(20,0,0), exit = new(40,0,0);
            Require(CampaignMissionLastCorridorRuleUtility.AdvanceRoute(0, exit, entry, middle, exit) == 0, "Far-side-only route shortcut accepted");
            var alternateEntry=new float3(980,0,432);var alternateMiddle=new float3(1100,0,432);var alternateExit=new float3(1230,0,432);
            Require(CampaignMissionLastCorridorRuleUtility.AdvanceRoute(2,new float3(1195.06f,0,440.4f),alternateEntry,alternateMiddle,alternateExit)==2,"Observed legal vehicle junction turn reset route proof");
            Require(CampaignMissionLastCorridorRuleUtility.AdvanceRoute(2,new float3(1113,0,443.5f),alternateEntry,alternateMiddle,alternateExit)==2,"Full legal vehicle junction turn reset route proof");
            Require(CampaignMissionLastCorridorRuleUtility.AdvanceRoute(2,new float3(1195,0,444),alternateEntry,alternateMiddle,alternateExit)==0,"Leaving assigned lane kept route proof");
            byte route = CampaignMissionLastCorridorRuleUtility.AdvanceRoute(0, entry, entry, middle, exit);
            Require(CampaignMissionLastCorridorRuleUtility.AdvanceRoute(route, exit, entry, middle, exit) == 1, "Midpoint bypass accepted");
            Require(CampaignMissionLastCorridorRuleUtility.AdvanceRoute(route, new float3(20,0,12), entry, middle, exit) == 0, "Leaving assigned lane kept incomplete proof");
            route = CampaignMissionLastCorridorRuleUtility.AdvanceRoute(route, middle, entry, middle, exit); route = CampaignMissionLastCorridorRuleUtility.AdvanceRoute(route, exit, entry, middle, exit); Require(route == 3, "Real connected route rejected");
            ValidateTutorialMapping(); ValidateWatchProgress(); ValidateNarrativePolicy(); ValidateInventoryAndCustody(runtime); ValidateResultSettlement(runtime);
            Debug.Log("[LastCorridorRules] result=Passed fiveCategories=Actual route=Ordered Fuel=Inventory custody=Original settlement=Idempotent");
        }
        private static void ValidateWatchProgress()
        {
            var session=new Game.UI.Shell.Contracts.Ecs.AriaPlaySessionComponent {Phase=Game.UI.Contracts.AriaPlayPhase.Observing};
            var observation=new Game.UI.Shell.Contracts.Ecs.AriaPlayObservationComponent {Kind=Game.UI.Contracts.AriaPlayObservationKind.Waiting};
            foreach(var milestone in new[]{(4,3,1f),(5,0,170f),(6,0,300f)})
            {
                observation.GoalId=Game.UI.Contracts.UiLastCorridorProgress.WatchGoal(milestone.Item1,7,milestone.Item2,false);
                observation.Time=milestone.Item3;
                Game.UI.Shell.Ecs.AriaPlayDecisionSystem.Step(observation,ref session);
                Require(session.Phase!=Game.UI.Contracts.AriaPlayPhase.Blocked,"Actual fuel/reinforcement/engineer milestones collided in ARIA's bounded watchdog");
            }
            observation.Time=481;
            Game.UI.Shell.Ecs.AriaPlayDecisionSystem.Step(observation,ref session);
            Require(session.Phase==Game.UI.Contracts.AriaPlayPhase.Blocked,"Stationary Corridor objective bypassed ARIA's progress timeout");
        }
        private static void ValidateTutorialMapping()
        {
            var utility = typeof(Game.UI.Shell.Ecs.AssistantObjectiveProjectionUtility);
            var step = utility.GetMethod("TutorialStepFor", BindingFlags.NonPublic | BindingFlags.Static);
            var count = utility.GetMethod("TutorialStepCountFor", BindingFlags.NonPublic | BindingFlags.Static);
            Require(step != null && count != null, "Actual tutorial mapping methods missing");
            for (int i = 1; i <= 8; i++)
            {
                var prompt = (CampaignMissionGuidancePromptKind)(124 + i);
                Require((byte)step.Invoke(null, new object[] { prompt }) == i && (byte)count.Invoke(null, new object[] { prompt }) == 8, "Corridor prompt lost its player step/count: " + prompt);
                var previousMission = (CampaignMissionGuidancePromptKind)(116 + i);
                Require((byte)step.Invoke(null, new object[] { previousMission }) == i && (byte)count.Invoke(null, new object[] { previousMission }) == 8, "Corridor mapping changed Network tutorial progression");
            }
        }
        private static void ValidateNarrativePolicy()
        {
            var policy = typeof(CampaignMissionRuntimeSystem).Assembly.GetType("Game.Runtime.CampaignMissionNarrativePolicy", true);
            var sequences = policy.GetMethod("UsesMissionSequences", BindingFlags.Static | BindingFlags.NonPublic);
            var blocking = policy.GetMethod("UsesBlockingComms", BindingFlags.Static | BindingFlags.NonPublic);
            Require(sequences != null && blocking != null, "Actual narrative policy methods missing");
            FixedString64Bytes mission = CampaignMissionSequence.LastCorridor;
            Require((bool)sequences.Invoke(null, new object[] { mission }), "Last Corridor actual briefing/debrief sequence policy omitted");
            var facts = new CampaignMissionAttemptFactsComponent { NetworkCommsReady = 1 };
            Require(!(bool)blocking.Invoke(null, new object[] { mission, facts }), "Unrelated mission comms fabricated Corridor readiness");
            facts.CorridorCommsReady = 1;
            Require((bool)blocking.Invoke(null, new object[] { mission, facts }), "Actual Corridor military-clear comms policy omitted");
            FixedString64Bytes unrelated = "saga.ch01.m01.first_contact";
            Require(!(bool)blocking.Invoke(null, new object[] { unrelated, facts }), "Corridor comms leaked into unrelated mission");
        }
        private static void ValidateInventoryAndCustody(CampaignMissionRuntimeComponent r)
        {
            using var world = new World("Last Corridor actual inventory and original custody"); var em = world.EntityManager;
            Entity Actor() { var e = em.CreateEntity(typeof(UnitHealth), typeof(LocalTransform)); em.SetComponentData(e, new UnitHealth { Current = 100, Max = 100 }); em.SetComponentData(e, LocalTransform.FromPosition(float3.zero)); return e; }
            var s = new CampaignMissionLastCorridorState { SessionToken = r.SessionToken, AttemptOrdinal = r.AttemptOrdinal, SourceVersion = r.SourceVersion, Initialized = 1, Engineer = Actor(), KeyCarrier = Actor(), FuelCarrier = Actor(), Reserve = Actor(), Receiver = Actor() };
            em.AddComponentData(s.FuelCarrier, new CampaignMissionCorridorCargo { SessionToken = r.SessionToken, AttemptOrdinal = r.AttemptOrdinal, SourceVersion = r.SourceVersion, Kind = CorridorCargoKind.Fuel });
            em.AddComponentData(s.Reserve, new BuildingResourceStorageComponent { OwnerFactionId = 1, StoredFuelBarrels = 160, CivilianFuelReserveBarrels = 20, FuelStorageCapacity = 240 });
            em.AddComponentData(s.Receiver, new BuildingResourceStorageComponent { OwnerFactionId = 1, FuelStorageCapacity = 240 });
            Require(CampaignMissionRuntimeSystem.TryLoadCorridorFuel(em, in r, ref s), "Original actual Fuel load rejected");
            Require(em.GetComponentData<BuildingResourceStorageComponent>(s.Reserve).StoredFuelBarrels == 120, "Cargo load did not debit actual source inventory");
            Require(!CampaignMissionRuntimeSystem.TryLoadCorridorFuel(em, in r, ref s), "Repeat load debited Fuel twice");
            var stale = r; stale.AttemptOrdinal++; Require(!CampaignMissionRuntimeSystem.TryReceiveCorridorFuel(em, s.Receiver, s.FuelCarrier, in stale), "Prior-attempt cargo entered receiving stock");
            Require(CampaignMissionRuntimeSystem.TryReceiveCorridorFuel(em, s.Receiver, s.FuelCarrier, in r), "Actual cargo not received");
            Require(em.GetComponentData<BuildingResourceStorageComponent>(s.Receiver).StoredFuelBarrels == 40, "Delivery did not credit real receiving storage");
            Require(!CampaignMissionRuntimeSystem.TryReceiveCorridorFuel(em, s.Receiver, s.FuelCarrier, in r) && em.GetComponentData<BuildingResourceStorageComponent>(s.Receiver).StoredFuelBarrels == 40, "Repeat delivery duplicated Fuel");
            em.AddComponentData(s.Engineer, new UnitTransportPassenger { Transport = s.KeyCarrier }); em.AddBuffer<UnitTransportPassengerElement>(s.KeyCarrier);
            Require(!CampaignMissionRuntimeSystem.CorridorEngineerAboard(em, in s), "Orphan passenger component counted as custody");
            em.GetBuffer<UnitTransportPassengerElement>(s.KeyCarrier).Add(new UnitTransportPassengerElement { Passenger = Actor() }); Require(!CampaignMissionRuntimeSystem.CorridorEngineerAboard(em, in s), "Wrong passenger replaced original engineer");
            em.GetBuffer<UnitTransportPassengerElement>(s.KeyCarrier).Add(new UnitTransportPassengerElement { Passenger = s.Engineer }); Require(CampaignMissionRuntimeSystem.CorridorEngineerAboard(em, in s), "Actual original occupancy rejected");
            em.RemoveComponent<UnitTransportPassenger>(s.Engineer); s.BrokenLink = Actor(); em.AddComponent<StaticGridBlocker>(s.BrokenLink); s.MilitaryCleared = 1; s.RepairGate = float3.zero; s.RepairMilliseconds = 5999;
            Require(!CampaignMissionRuntimeSystem.TryRecoverCorridorLink(em, ref s) && em.Exists(s.BrokenLink), "Incomplete recovery removed real road obstruction");
            s.RepairMilliseconds = 6000; em.AddComponent<UnitPathRequest>(s.Engineer);
            Require(!CampaignMissionRuntimeSystem.TryRecoverCorridorLink(em, ref s) && em.Exists(s.BrokenLink), "Moving engineer removed road obstruction");
            em.RemoveComponent<UnitPathRequest>(s.Engineer); Require(CampaignMissionRuntimeSystem.TryRecoverCorridorLink(em, ref s) && !em.Exists(s.BrokenLink) && s.LinkRecovered != 0, "Qualified original engineer failed to clear actual obstruction");
            Require(!CampaignMissionRuntimeSystem.TryRecoverCorridorLink(em, ref s), "Original repair repeated");

        }
        private static void ValidateResultSettlement(CampaignMissionRuntimeComponent runtime)
        {
            var authored=AssetDatabase.LoadAssetAtPath<MissionDefinitionConfig>(CH05M04LastCorridorConfigBuilder.MissionPath);
            Require(authored!=null,"Authored result contract missing");
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
            using var world=new World("Last Corridor actual result and reward settlement");
            var em=world.EntityManager;
            var root=em.CreateEntity(typeof(CampaignMissionRootComponent),typeof(CampaignMissionCatalogComponent),typeof(CampaignMissionRuntimeComponent),typeof(CampaignMissionAttemptFactsComponent));
            runtime.Phase=MissionPhaseKind.Result;runtime.Outcome=MissionOutcomeKind.Victory;
            runtime.ReturnDestination=MissionReturnDestinationKind.CampaignOperations;runtime.RunKind=MissionRunKind.FirstClear;
            runtime.LaunchOrigin=MissionLaunchOriginKind.CampaignOperations;
            // Reproduce the actual mission facts: no generic defense forward post exists.
            var facts=new CampaignMissionAttemptFactsComponent { CommandSquadSpawned=1,CommandSquadAlive=1,HostileTotalCount=7,HostileDefeatedCount=7,
                CivilianTotalCount=4,ElapsedMilliseconds=300000,CorridorSuppliesDelivered=3,CorridorFuelReceived=40,CorridorLinkRecovered=1,CorridorEngineerDelivered=1,CorridorKeysDelivered=1 };
            ref var published=ref blob.Value.Missions[0];
            Require(CampaignMissionResultProjectionSystem.TryProject(in runtime,in facts,ref published,out var projected)&&projected.Stars==3,"Qualified Last Corridor victory rejected by generic defense result gate");
            var incomplete=facts;incomplete.CorridorFuelReceived=0;
            Require(!CampaignMissionResultProjectionSystem.TryProject(in runtime,in incomplete,ref published,out _),"Result accepted Fuel label without actual transferred inventory");
            incomplete=facts;incomplete.CorridorEngineerDelivered=0;
            Require(!CampaignMissionResultProjectionSystem.TryProject(in runtime,in incomplete,ref published,out _),"Result accepted keys without original engineer");
            incomplete=facts;incomplete.HostileDefeatedCount=6;
            Require(!CampaignMissionResultProjectionSystem.TryProject(in runtime,in incomplete,ref published,out _),"Result ignored surviving original hostile");
            incomplete=facts;incomplete.CorridorFailure=LastCorridorFailure.StaffLost;incomplete.CivilianLossCount=1;
            Require(!CampaignMissionResultProjectionSystem.TryProject(in runtime,in incomplete,ref published,out _),"Result ignored original civic staff loss");
            em.SetComponentData(root,runtime);em.SetComponentData(root,facts);
            em.SetComponentData(root,new CampaignMissionCatalogComponent{Blob=blob,SourceVersion=runtime.SourceVersion});
            em.AddBuffer<CampaignMissionSettlementRequestElement>(root);
            var writer=world.GetOrCreateSystem<CampaignMissionResultProjectionSystem>();
            void Update()=>world.Unmanaged.GetUnsafeSystemRef<CampaignMissionResultProjectionSystem>(writer).OnUpdate(ref world.Unmanaged.ResolveSystemStateRef(writer));
            Update();Update();
            Require(em.HasComponent<CampaignMissionResultComponent>(root)&&em.GetBuffer<CampaignMissionSettlementRequestElement>(root).Length==1,"Real result writer failed or queued duplicate settlement");
            string savePath=Path.Combine(Path.GetTempPath(),"corridor-result-regression-"+Guid.NewGuid().ToString("N"));
            try
            {
                var save=new SaveService(new JsonSaveRepository(savePath));var store=new CampaignMissionProgressStore(save);
                store.EnsureAvailable(authored.MissionId);var before=save.LoadProfile();
                var request=em.GetBuffer<CampaignMissionSettlementRequestElement>(root)[0];
                var result=em.GetComponentData<CampaignMissionResultComponent>(root);
                var stale=request;stale.AttemptOrdinal--;
                Require(CampaignMissionProgressSettlementSystem.Settle(store,in stale,in runtime,in result,ref published).Accepted==0,"Prior retry reward settlement accepted");
                var receipt=CampaignMissionProgressSettlementSystem.Settle(store,in request,in runtime,in result,ref published);
                Require(receipt.Accepted!=0&&receipt.FirstClear!=0,"Real Last Corridor first-clear settlement rejected");
                var earned=save.LoadProfile();

                Require(earned.commanderXp>before.commanderXp&&earned.credits>before.credits,"Authored Last Corridor rewards not granted");
                Require(CampaignMissionProgressSettlementSystem.Settle(store,in request,in runtime,in result,ref published).Accepted!=0,"Duplicate receipt not recognized");
                var duplicate=save.LoadProfile();

                Require(duplicate.commanderXp==earned.commanderXp&&duplicate.credits==earned.credits,"Duplicate result paid rewards twice");
            }
            finally {if(Directory.Exists(savePath))Directory.Delete(savePath,true);}
        }
        private static void Require(bool condition,string error) { if(!condition)throw new InvalidOperationException(error); }
    }
}
