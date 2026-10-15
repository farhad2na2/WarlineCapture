#if UNITY_EDITOR
using System;
using Game.Components;
using Game.Runtime;
using Game.Configs;
using Game.Skirmish.Contracts;
using NUnit.Framework;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace Game.Tests.Editor
{

    public sealed class SkirmishEnemyVehicleRepairPolicyTests
    {
        private sealed class Fixture
        {
            public EntityManager Em;public Entity Session,Actor,Facility,Bank;
            public Fixture(EntityManager em,Entity session)
            {
                Em=em;Session=session;var attempt=em.GetComponentData<SkirmishExpandedSessionComponent>(session).SessionId;
                if(!em.HasComponent<SkirmishEconomyStockComponent>(session))em.AddComponentData(session,new SkirmishEconomyStockComponent{FactionId=2});
                if(!em.HasComponent<SkirmishCapacityComponent>(session))em.AddComponentData(session,new SkirmishCapacityComponent{SupplyLive=18});
                var definition=em.GetComponentObject<SkirmishLaunchDefinitionRecord>(session).Definition;
                Assert.IsTrue(definition.MapLayout.TryGetPad(2,SkirmishLegalPadKind.Service,out var pad));
                Assert.IsTrue(definition.MapLayout.TryProjectLocalToMap(pad.CenterX,pad.CenterZ,out float x,out float z));
                Actor=em.CreateEntity();em.AddComponent<SkirmishSharedActorTag>(Actor);
                em.AddComponentData(Actor,new SkirmishAttemptOwnedComponent{SessionId=attempt,StableObjectId="repair.own.tank",FactionId=2});
                em.AddComponentData(Actor,new Faction{Id=2});em.AddComponentData(Actor,new UnitHealth{Current=50,Max=200});
                em.AddComponentData(Actor,new SkirmishUnitRoleComponent{Role=SkirmishRoleKind.Tank,Category=SkirmishPopulationCategory.Ground});
                em.AddComponentData(Actor,new UnitMovementBehavior{UsesVehicleMotion=1});
                em.AddComponentData(Actor,LocalTransform.FromPosition(new float3(x,0,z)));
                Facility=em.CreateEntity();em.AddComponentData(Facility,new SkirmishAttemptOwnedComponent{SessionId=attempt,StableObjectId="repair.ground.facility",FactionId=2,IsStructure=1});
                em.AddComponent<SkirmishSharedActorTag>(Facility);
                em.AddComponentData(Facility,new Faction{Id=2});em.AddComponentData(Facility,new UnitHealth{Current=800,Max=800});
                em.AddComponentData(Facility,LocalTransform.FromPosition(new float3(x,0,z)));
                em.AddComponentData(Facility,new SkirmishStructureIdentityComponent{StructureId=new FixedString64Bytes(SkirmishStructureIds.GroundStaging)});
                em.AddComponentData(Actor,new SkirmishArmyGroupMembershipComponent{GroupId=900,Domain=SkirmishPopulationCategory.Ground});
                Bank=em.CreateEntity();em.AddComponentData(Bank,new FactionEconomy{FactionId=2});
                em.AddComponentData(Bank,new FactionTacticalMaterialsComponent{FactionId=2,Current=100,Capacity=200,Version=1});
            }
            public void Start()
            {Assert.IsTrue(VehicleFieldRepairService.TryEnqueue(Em,Session,Actor,2,VehicleRepairAction.Start,out var error),error);Assert.IsTrue(VehicleFieldRepairService.ProcessRequest(Em,Actor,out error),error);}
            public void Time(float seconds,bool paused=false)
            {var clock=Em.GetComponentData<SkirmishObjectiveClockComponent>(Session);clock.ElapsedSeconds=seconds;clock.Paused=(byte)(paused?1:0);Em.SetComponentData(Session,clock);Assert.IsTrue(VehicleFieldRepairService.Tick(Em,Actor,out string error),error);}
            public VehicleRepairStateComponent State=>Em.GetComponentData<VehicleRepairStateComponent>(Actor);
            public int Money=>Em.GetComponentData<FactionTacticalMaterialsComponent>(Bank).Current;
            public int Health=>Em.GetComponentData<UnitHealth>(Actor).Current;
        }

        static void With(Action<Fixture> callback)=>SkirmishMissionLogicalBindingNativeTestFixture.WithActiveFrontline((em,session)=>callback(new Fixture(em,session)));
        static SkirmishEnemyPerceptionBuilder.Snapshot Facts(Fixture f)
        {var hp=f.Em.GetComponentData<UnitHealth>(f.Actor);var facts=new SkirmishEnemyPerceptionBuilder.Snapshot();facts.Groups.Add(new(){Id=900,Role=SkirmishRoleKind.Tank,
            Category=SkirmishPopulationCategory.Ground,Members=1,Supply=4,Health=hp.Current,MaximumHealth=hp.Max,Position=f.Em.GetComponentData<LocalTransform>(f.Actor).Position});return facts;}
        [Test] public void EnemyUsesSameQuotedPaidRequestAndKeepsWorkingGroupReserved()
        {With(f=>{Assert.AreEqual(1,SkirmishEnemyVehicleRepairPolicy.Step(f.Em,f.Session,Facts(f),1,out var reserved));Assert.IsTrue(reserved.Contains(900));
            Assert.AreEqual(100,f.Money);Assert.AreEqual(50,f.Health);var request=f.Em.GetComponentData<VehicleRepairRequestComponent>(f.Actor);
            Assert.AreEqual(1,request.HasExpectedPlan);Assert.AreEqual(2,request.Faction);Assert.AreEqual(20,request.ExpectedMaterials);
            Assert.IsTrue(VehicleFieldRepairService.ProcessRequest(f.Em,f.Actor,out string error),error);Assert.AreEqual(80,f.Money);
            Assert.AreEqual(0,SkirmishEnemyVehicleRepairPolicy.Step(f.Em,f.Session,Facts(f),1,out reserved));Assert.IsTrue(reserved.Contains(900));
            Assert.IsFalse(f.Em.HasComponent<ManualMoveOrderTag>(f.Actor));Assert.AreEqual(VehicleRepairPhase.Working,f.State.Phase);
            f.Time(20);Assert.AreEqual(100,f.Health);Assert.AreEqual(80,f.Money);Assert.IsFalse(SkirmishEnemyVehicleRepairPolicy.ReservedGroups(f.Em,f.Session).Contains(900));});}
        [Test] public void HiddenHostileCannotSuppressEnemyRepairButVisibleArmedContactDoes()
        {foreach(bool visible in new[]{false,true})With(f=>{var enemy=f.Em.CreateEntity();var attempt=f.Em.GetComponentData<SkirmishExpandedSessionComponent>(f.Session).SessionId;
            f.Em.AddComponentData(enemy,new SkirmishAttemptOwnedComponent{SessionId=attempt,FactionId=1,StableObjectId="player.hidden"});f.Em.AddComponentData(enemy,new Faction{Id=1});
            f.Em.AddComponentData(enemy,new UnitHealth{Current=100,Max=100});f.Em.AddComponentData(enemy,new UnitAttack{Damage=1});f.Em.AddComponentData(enemy,f.Em.GetComponentData<LocalTransform>(f.Actor));
            if(visible)f.Em.AddBuffer<SkirmishContactKnowledgeElement>(enemy).Add(new(){ObserverFactionId=2,Sight=SkirmishContactSight.Visible});
            Assert.AreEqual(visible?0:1,SkirmishEnemyVehicleRepairPolicy.Step(f.Em,f.Session,Facts(f),1,out _));Assert.AreEqual(100,f.Money);Assert.AreEqual(50,f.Health);});}
        [Test] public void HealthyForeignAndFacilitylessCandidatesCannotReserveOrSpend()
        {foreach(string condition in new[]{"healthy","foreign","facility"})With(f=>{if(condition=="healthy")f.Em.SetComponentData(f.Actor,new UnitHealth{Current=200,Max=200});
            if(condition=="foreign"){var own=f.Em.GetComponentData<SkirmishAttemptOwnedComponent>(f.Actor);own.FactionId=1;f.Em.SetComponentData(f.Actor,own);}
            if(condition=="facility")f.Em.DestroyEntity(f.Facility);
            Assert.AreEqual(0,SkirmishEnemyVehicleRepairPolicy.Step(f.Em,f.Session,Facts(f),1,out var reserved));Assert.IsFalse(reserved.Contains(900));Assert.AreEqual(100,f.Money);
            Assert.IsFalse(f.Em.HasComponent<VehicleRepairRequestComponent>(f.Actor));});}
        [Test] public void PendingAndWorkingReservationsJoinExistingCapabilityExclusion()
        {With(f=>{Assert.AreEqual(1,SkirmishEnemyVehicleRepairPolicy.Step(f.Em,f.Session,Facts(f),1,out _));
            Assert.IsTrue(SkirmishEnemyCapabilityService.ReservedGroups(f.Em,f.Session).Contains(900));
            Assert.IsTrue(VehicleFieldRepairService.ProcessRequest(f.Em,f.Actor,out _));Assert.AreEqual(VehicleRepairPhase.Working,f.State.Phase);
            var facts=Facts(f);facts.CombatSupply=4;SkirmishEnemyCapabilityService.RemoveReservedGroundGroups(f.Em,f.Session,facts);
            Assert.AreEqual(0,facts.Groups.Count);Assert.AreEqual(0,facts.CombatSupply);Assert.AreEqual(80,f.Money);Assert.AreEqual(50,f.Health);});}
        [Test] public void ZeroBudgetAndIllegalRetreatNeverTeleportOrBypassNormalMove()
        {With(f=>{var initial=f.Em.GetComponentData<LocalTransform>(f.Actor);Assert.AreEqual(0,SkirmishEnemyVehicleRepairPolicy.Step(f.Em,f.Session,Facts(f),0,out _));
            var moved=initial;moved.Position+=new float3(30,0,0);f.Em.SetComponentData(f.Actor,moved);
            // This bounded World intentionally has no native navigation grid.
            // The real normal Move owner must reject rather than mutate position.
            Assert.AreEqual(0,SkirmishEnemyVehicleRepairPolicy.Step(f.Em,f.Session,Facts(f),1,out _));Assert.AreEqual(moved.Position,f.Em.GetComponentData<LocalTransform>(f.Actor).Position);
            Assert.AreEqual(100,f.Money);Assert.AreEqual(50,f.Health);Assert.IsFalse(f.Em.HasComponent<VehicleRepairRequestComponent>(f.Actor));});}
        [Test] public void RetreatUsesActualNormalNativeMoveWithoutTeleportOrRepairCharge()
        {With(f=>{const int width=80,size=width*width;var pad=f.Em.GetComponentData<LocalTransform>(f.Actor).Position;
            using var counts=new NativeArray<int>(size,Allocator.Temp);using var friendlyOwner=new NativeArray<byte>(size,Allocator.Temp);var friendly=friendlyOwner;
            using var blocked=new NativeBitArray(size,Allocator.Temp);using var occupied=new NativeBitArray(size,Allocator.Temp);using var pool=new NativeList<int2>(1024,Allocator.Temp);
            for(int i=0;i<size;i++)friendly[i]=byte.MaxValue;
            var grid=f.Em.CreateEntity(typeof(GridConfig),typeof(DynamicBlockerComponent),typeof(DynamicOccupancyComponent),typeof(PathPoolComponent));
            f.Em.SetComponentData(grid,new GridConfig{Width=width,Height=width,CellSize=1,Origin=pad-new float3(20,0,20)});
            f.Em.SetComponentData(grid,new DynamicBlockerComponent{GridSize=size,Counts=counts,Blocked=blocked,FriendlyPassFactionIds=friendly});
            f.Em.SetComponentData(grid,new DynamicOccupancyComponent{GridSize=size,Occupied=occupied});f.Em.SetComponentData(grid,new PathPoolComponent{Cells=pool});
            f.Em.AddBuffer<GridWalkable>(grid);f.Em.AddBuffer<GridRoad>(grid);f.Em.AddBuffer<GridRoadSidewalk>(grid);f.Em.AddBuffer<GridRoadDirt>(grid);
            for(int i=0;i<size;i++){f.Em.GetBuffer<GridWalkable>(grid).Add(new(){Value=1});f.Em.GetBuffer<GridRoad>(grid).Add(default);f.Em.GetBuffer<GridRoadSidewalk>(grid).Add(default);f.Em.GetBuffer<GridRoadDirt>(grid).Add(default);}
            if(!f.Em.HasBuffer<SkirmishArmyGroupRecord>(f.Session))f.Em.AddBuffer<SkirmishArmyGroupRecord>(f.Session);
            f.Em.GetBuffer<SkirmishArmyGroupRecord>(f.Session).Add(new(){GroupId=900,FactionId=2,Role=SkirmishRoleKind.Tank,AliveCount=1,MemberCount=1});
            f.Em.AddComponentData(f.Actor,new UnitMove{Speed=8,WalkSpeed=8,RoadSpeedMultiplier=1,ArriveDistance=.05f});
            f.Em.AddComponentData(f.Actor,new UnitGrid{Cell=new int2(50,20)});f.Em.AddComponentData(f.Actor,new UnitFootprint{Size=new int2(1,1)});
            f.Em.AddComponentData(f.Actor,new LocalToWorld{Value=float4x4.Translate(pad+new float3(30,0,0))});
            f.Em.SetComponentData(f.Actor,LocalTransform.FromPosition(pad+new float3(30,0,0)));var before=f.Em.GetComponentData<LocalTransform>(f.Actor).Position;
            Assert.AreEqual(1,SkirmishEnemyVehicleRepairPolicy.Step(f.Em,f.Session,Facts(f),1,out var reserved));Assert.IsTrue(reserved.Contains(900));
            Assert.AreEqual(before,f.Em.GetComponentData<LocalTransform>(f.Actor).Position);Assert.AreEqual(100,f.Money);Assert.AreEqual(50,f.Health);
            Assert.IsTrue(f.Em.HasComponent<SkirmishMoveIntentComponent>(f.Actor));var intent=f.Em.GetComponentData<SkirmishMoveIntentComponent>(f.Actor);
            Assert.AreEqual(SkirmishGroupOrderKind.Move,intent.Order);Assert.AreEqual(pad.x,intent.DestinationX);Assert.AreEqual(pad.z,intent.DestinationZ);
            Assert.IsFalse(f.Em.HasComponent<VehicleRepairRequestComponent>(f.Actor));f.Em.CompleteAllTrackedJobs();});}
        [Test] public void PauseAndTerminalObjectivePreventNewEnemyRepairOrRetreat()
        {foreach(bool terminal in new[]{false,true})With(f=>{if(terminal){var state=f.Em.GetComponentData<SkirmishObjectiveStateComponent>(f.Session);state.Terminal=1;f.Em.SetComponentData(f.Session,state);}
            else{var clock=f.Em.GetComponentData<SkirmishObjectiveClockComponent>(f.Session);clock.Paused=1;f.Em.SetComponentData(f.Session,clock);}
            Assert.AreEqual(0,SkirmishEnemyVehicleRepairPolicy.Step(f.Em,f.Session,Facts(f),1,out _));Assert.IsFalse(f.Em.HasComponent<VehicleRepairRequestComponent>(f.Actor));Assert.AreEqual(100,f.Money);});}
        [Test] public void ActualBankFullCancelledDebtPermitsNormalMoveAndRefundsExactlyOnce()
        {With(f=>{const int width=80,size=width*width;var pad=f.Em.GetComponentData<LocalTransform>(f.Actor).Position;
            using var counts=new NativeArray<int>(size,Allocator.Temp);using var friendlyOwner=new NativeArray<byte>(size,Allocator.Temp);var friendly=friendlyOwner;
            using var blocked=new NativeBitArray(size,Allocator.Temp);using var occupied=new NativeBitArray(size,Allocator.Temp);using var pool=new NativeList<int2>(1024,Allocator.Temp);
            for(int i=0;i<size;i++)friendly[i]=byte.MaxValue;
            var grid=f.Em.CreateEntity(typeof(GridConfig),typeof(DynamicBlockerComponent),typeof(DynamicOccupancyComponent),typeof(PathPoolComponent));
            f.Em.SetComponentData(grid,new GridConfig{Width=width,Height=width,CellSize=1,Origin=pad-new float3(20,0,20)});
            f.Em.SetComponentData(grid,new DynamicBlockerComponent{GridSize=size,Counts=counts,Blocked=blocked,FriendlyPassFactionIds=friendly});
            f.Em.SetComponentData(grid,new DynamicOccupancyComponent{GridSize=size,Occupied=occupied});f.Em.SetComponentData(grid,new PathPoolComponent{Cells=pool});
            f.Em.AddBuffer<GridWalkable>(grid);f.Em.AddBuffer<GridRoad>(grid);f.Em.AddBuffer<GridRoadSidewalk>(grid);f.Em.AddBuffer<GridRoadDirt>(grid);
            for(int i=0;i<size;i++){f.Em.GetBuffer<GridWalkable>(grid).Add(new(){Value=1});f.Em.GetBuffer<GridRoad>(grid).Add(default);f.Em.GetBuffer<GridRoadSidewalk>(grid).Add(default);f.Em.GetBuffer<GridRoadDirt>(grid).Add(default);}
            if(!f.Em.HasBuffer<SkirmishArmyGroupRecord>(f.Session))f.Em.AddBuffer<SkirmishArmyGroupRecord>(f.Session);
            f.Em.GetBuffer<SkirmishArmyGroupRecord>(f.Session).Add(new(){GroupId=900,FactionId=2,Role=SkirmishRoleKind.Tank,AliveCount=1,MemberCount=1});
            f.Em.AddComponentData(f.Actor,new UnitMove{Speed=8,WalkSpeed=8,RoadSpeedMultiplier=1,ArriveDistance=.05f});
            f.Em.AddComponentData(f.Actor,new UnitGrid{Cell=new int2(50,20)});f.Em.AddComponentData(f.Actor,new UnitFootprint{Size=new int2(1,1)});
            f.Em.AddComponentData(f.Actor,new LocalToWorld{Value=float4x4.Translate(pad+new float3(30,0,0))});
            f.Start();var money=f.Em.GetComponentData<FactionTacticalMaterialsComponent>(f.Bank);money.Current=money.Capacity;f.Em.SetComponentData(f.Bank,money);
            var clock=f.Em.GetComponentData<SkirmishObjectiveClockComponent>(f.Session);clock.ElapsedSeconds=1;f.Em.SetComponentData(f.Session,clock);
            VehicleFieldRepairService.NotifyNewMovement(f.Em,f.Actor);Assert.IsFalse(VehicleFieldRepairService.Tick(f.Em,f.Actor,out _));
            Assert.AreEqual(VehicleRepairPhase.AccountingFailure,f.State.Phase);Assert.AreEqual(15,f.State.RefundDue);Assert.AreEqual(0,f.State.RefundedMaterials);
            f.Em.SetComponentData(f.Actor,LocalTransform.FromPosition(pad+new float3(30,0,0)));var before=f.Em.GetComponentData<LocalTransform>(f.Actor).Position;
            Assert.AreEqual(0,SkirmishEnemyVehicleRepairPolicy.Step(f.Em,f.Session,Facts(f),1,out var reserved));Assert.IsFalse(reserved.Contains(900));
            Assert.IsFalse(SkirmishEnemyCapabilityService.ReservedGroups(f.Em,f.Session).Contains(900));
            Assert.IsFalse(VehicleFieldRepairService.TryEnqueue(f.Em,f.Session,f.Actor,2,VehicleRepairAction.Start,out _));
            Assert.IsTrue(SkirmishArmyCommandService.TryIssueGroupOrder(f.Em,f.Session,900,2,SkirmishGroupOrderKind.Move,Entity.Null,pad,out var decision),decision.Reason.ToString());
            Assert.AreEqual(before,f.Em.GetComponentData<LocalTransform>(f.Actor).Position);Assert.AreEqual(200,f.Money);Assert.AreEqual(50,f.Health);
            Assert.AreEqual(SkirmishGroupOrderKind.Move,f.Em.GetComponentData<SkirmishMoveIntentComponent>(f.Actor).Order);
            money=f.Em.GetComponentData<FactionTacticalMaterialsComponent>(f.Bank);
            Assert.AreEqual(FactionTacticalMaterialsMutationResult.Applied,FactionTacticalMaterialsUtilitySystemHelper.TrySpend(ref money,20,FactionTacticalMaterialsSpendKind.Construction));f.Em.SetComponentData(f.Bank,money);
            Assert.IsTrue(VehicleFieldRepairService.Tick(f.Em,f.Actor,out _));Assert.IsTrue(VehicleFieldRepairService.Tick(f.Em,f.Actor,out _));
            Assert.AreEqual(195,f.Money);Assert.AreEqual(15,f.State.RefundedMaterials);Assert.AreEqual(50,f.Health);
            f.Em.CompleteAllTrackedJobs();});}
        [Test] public void ActualApcDecisionFilterRetainsRetreatAndArrivalExclusion()
        {With(f=>{const int width=80,size=width*width;var pad=f.Em.GetComponentData<LocalTransform>(f.Actor).Position;
            using var counts=new NativeArray<int>(size,Allocator.Temp);using var friendlyOwner=new NativeArray<byte>(size,Allocator.Temp);var friendly=friendlyOwner;
            using var blocked=new NativeBitArray(size,Allocator.Temp);using var occupied=new NativeBitArray(size,Allocator.Temp);using var pool=new NativeList<int2>(1024,Allocator.Temp);
            for(int i=0;i<size;i++)friendly[i]=byte.MaxValue;
            var grid=f.Em.CreateEntity(typeof(GridConfig),typeof(DynamicBlockerComponent),typeof(DynamicOccupancyComponent),typeof(PathPoolComponent));
            f.Em.SetComponentData(grid,new GridConfig{Width=width,Height=width,CellSize=1,Origin=pad-new float3(20,0,20)});
            f.Em.SetComponentData(grid,new DynamicBlockerComponent{GridSize=size,Counts=counts,Blocked=blocked,FriendlyPassFactionIds=friendly});
            f.Em.SetComponentData(grid,new DynamicOccupancyComponent{GridSize=size,Occupied=occupied});f.Em.SetComponentData(grid,new PathPoolComponent{Cells=pool});
            f.Em.AddBuffer<GridWalkable>(grid);f.Em.AddBuffer<GridRoad>(grid);f.Em.AddBuffer<GridRoadSidewalk>(grid);f.Em.AddBuffer<GridRoadDirt>(grid);
            for(int i=0;i<size;i++){f.Em.GetBuffer<GridWalkable>(grid).Add(new(){Value=1});f.Em.GetBuffer<GridRoad>(grid).Add(default);f.Em.GetBuffer<GridRoadSidewalk>(grid).Add(default);f.Em.GetBuffer<GridRoadDirt>(grid).Add(default);}
            if(!f.Em.HasBuffer<SkirmishArmyGroupRecord>(f.Session))f.Em.AddBuffer<SkirmishArmyGroupRecord>(f.Session);
            f.Em.GetBuffer<SkirmishArmyGroupRecord>(f.Session).Add(new(){GroupId=900,FactionId=2,Role=SkirmishRoleKind.Tank,AliveCount=1,MemberCount=1});
            f.Em.AddComponentData(f.Actor,new UnitMove{Speed=8,WalkSpeed=8,RoadSpeedMultiplier=1,ArriveDistance=.05f});
            f.Em.AddComponentData(f.Actor,new UnitGrid{Cell=new int2(50,20)});f.Em.AddComponentData(f.Actor,new UnitFootprint{Size=new int2(1,1)});
            f.Em.AddComponentData(f.Actor,new LocalToWorld{Value=float4x4.Translate(pad+new float3(30,0,0))});
            var role=f.Em.GetComponentData<SkirmishUnitRoleComponent>(f.Actor);role.Role=SkirmishRoleKind.ApcFast;f.Em.SetComponentData(f.Actor,role);
            f.Em.AddComponentData(f.Actor,new UnitTransportCapacity{SoldierCapacity=4});
            f.Em.SetComponentData(f.Actor,LocalTransform.FromPosition(pad+new float3(30,0,0)));var before=f.Em.GetComponentData<LocalTransform>(f.Actor).Position;
            Assert.AreEqual(1,SkirmishEnemyVehicleRepairPolicy.Step(f.Em,f.Session,Facts(f),1,out var reserved));Assert.IsTrue(reserved.Contains(900));
            Assert.AreEqual(before,f.Em.GetComponentData<LocalTransform>(f.Actor).Position);Assert.AreEqual(100,f.Money);Assert.AreEqual(50,f.Health);
            Assert.IsTrue(f.Em.HasComponent<SkirmishMoveIntentComponent>(f.Actor));var intent=f.Em.GetComponentData<SkirmishMoveIntentComponent>(f.Actor);
            Assert.AreEqual(SkirmishGroupOrderKind.Move,intent.Order);Assert.AreEqual(pad.x,intent.DestinationX);Assert.AreEqual(pad.z,intent.DestinationZ);
            Assert.IsFalse(f.Em.HasComponent<VehicleRepairRequestComponent>(f.Actor));Assert.IsTrue(SkirmishEnemyCapabilityService.ReadOwnActors(f.Em,f.Session).Exists(a=>a.GroupId==900&&a.Carrier));
            Assert.IsFalse(SkirmishEnemyCapabilityService.ReadUnreservedOwnActors(f.Em,f.Session,reserved).Exists(a=>a.GroupId==900));
            f.Em.SetComponentData(f.Actor,LocalTransform.FromPosition(pad));
            Assert.AreEqual(1,SkirmishEnemyVehicleRepairPolicy.Step(f.Em,f.Session,Facts(f),1,out reserved));Assert.IsTrue(reserved.Contains(900));
            Assert.IsFalse(SkirmishEnemyCapabilityService.ReadUnreservedOwnActors(f.Em,f.Session,reserved).Exists(a=>a.GroupId==900));
            Assert.AreEqual(100,f.Money);Assert.AreEqual(50,f.Health);f.Em.CompleteAllTrackedJobs();});}
        public static void RunFocusedValidation()
        {var t=new SkirmishEnemyVehicleRepairPolicyTests();t.EnemyUsesSameQuotedPaidRequestAndKeepsWorkingGroupReserved();
            t.HiddenHostileCannotSuppressEnemyRepairButVisibleArmedContactDoes();t.HealthyForeignAndFacilitylessCandidatesCannotReserveOrSpend();
            t.PendingAndWorkingReservationsJoinExistingCapabilityExclusion();t.ZeroBudgetAndIllegalRetreatNeverTeleportOrBypassNormalMove();
            t.RetreatUsesActualNormalNativeMoveWithoutTeleportOrRepairCharge();
            t.PauseAndTerminalObjectivePreventNewEnemyRepairOrRetreat();t.ActualBankFullCancelledDebtPermitsNormalMoveAndRefundsExactlyOnce();t.ActualApcDecisionFilterRetainsRetreatAndArrivalExclusion();
            Debug.Log("[SkirmishEnemyVehicleRepairPolicyTests] result=Passed tests=9 normalMissionAcceptance=0");}
    }
}
#endif
