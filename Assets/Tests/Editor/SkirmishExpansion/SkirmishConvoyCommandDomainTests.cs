#if UNITY_EDITOR
using System;
using Game.Components;
using Game.Configs;
using Game.Runtime;
using Game.Skirmish.Contracts;
using NUnit.Framework;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace Game.Tests.Editor
{
    public sealed class SkirmishConvoyCommandDomainTests
    {
        private sealed class Fixture:IDisposable
        {
            public readonly World World=new World("ActualNativeConvoyMoveCommandFixture");public EntityManager Em=>World.EntityManager;
            public Entity Session;public Entity[] Trucks=new Entity[3];
            private NativeArray<int> counts;private NativeArray<byte> friendly;
            private NativeBitArray blocked,occupied;private NativeList<int2> pool;
            public Fixture()
            {
                const int size=1600;counts=new NativeArray<int>(size,Allocator.Persistent);friendly=new NativeArray<byte>(size,Allocator.Persistent);
                blocked=new NativeBitArray(size,Allocator.Persistent,NativeArrayOptions.ClearMemory);occupied=new NativeBitArray(size,Allocator.Persistent,NativeArrayOptions.ClearMemory);
                pool=new NativeList<int2>(1024,Allocator.Persistent);for(int i=0;i<size;i++)friendly[i]=byte.MaxValue;
                var grid=Em.CreateEntity(typeof(GridConfig),typeof(DynamicBlockerComponent),typeof(DynamicOccupancyComponent),typeof(PathPoolComponent));
                Em.SetComponentData(grid,new GridConfig{Width=40,Height=40,CellSize=1,Origin=float3.zero});
                Em.SetComponentData(grid,new DynamicBlockerComponent{GridSize=size,Counts=counts,Blocked=blocked,FriendlyPassFactionIds=friendly});
                Em.SetComponentData(grid,new DynamicOccupancyComponent{GridSize=size,Occupied=occupied});Em.SetComponentData(grid,new PathPoolComponent{Cells=pool});
                Em.AddBuffer<GridWalkable>(grid);Em.AddBuffer<GridRoad>(grid);Em.AddBuffer<GridRoadSidewalk>(grid);Em.AddBuffer<GridRoadDirt>(grid);
                for(int i=0;i<size;i++){Em.GetBuffer<GridWalkable>(grid).Add(new GridWalkable{Value=1});Em.GetBuffer<GridRoad>(grid).Add(default);Em.GetBuffer<GridRoadSidewalk>(grid).Add(default);Em.GetBuffer<GridRoadDirt>(grid).Add(default);}
                Session=Em.CreateEntity(typeof(SkirmishExpandedSessionComponent),typeof(SkirmishObjectiveClockComponent),typeof(SkirmishCapacityComponent));
                Em.SetComponentData(Session,new SkirmishExpandedSessionComponent{SessionId="convoy.command",DefinitionId="fixture.convoy",SetupHash=123,ContentVersion=1,
                    ObjectiveKind=SkirmishObjectiveKind.ConvoyEscort,Phase=SkirmishSessionPhase.Playing,SpawnComplete=1,SizeId=SkirmishSizeId.Standard});
                Em.SetComponentData(Session,new SkirmishObjectiveClockComponent{Playing=1});
                Em.SetComponentData(Session,new SkirmishCapacityComponent{InfantryLive=12,GroundLive=1,SupplyLive=18});
                var setup=new SkirmishResolvedSetup{SetupHash=123,ObjectiveKind=SkirmishObjectiveKind.ConvoyEscort,PlayerObjectiveTrucks=3,ObjectiveSupportExtraPlayer=3,
                    ObjectiveActorDefinitionFingerprint=new string('c',64),Forces=new SkirmishResolvedForceEntry[3]};
                Em.AddComponentObject(Session,new SkirmishResolvedSetupRecord{Setup=setup});Em.AddBuffer<SkirmishArmyGroupRecord>(Session);
                for(int i=0;i<3;i++)
                {
                    setup.Forces[i]=new SkirmishResolvedForceEntry{FactionId=1,RoleId=SkirmishRoleIds.ConvoyLogic,RoleKind=SkirmishRoleKind.ConvoyLogic,
                        RuntimePrefabKey=SkirmishConvoyTruckDefinition.ExistingChassisKey,Quantity=1,ObjectiveRoleId="convoy."+(i+1)};
                    var actor=Em.CreateEntity();Trucks[i]=actor;string id=SkirmishOriginalObjectiveBindingPlan.StableId(SkirmishRoleIds.ConvoyLogic,1,i,0);
                    Em.AddComponentData(actor,new SkirmishAttemptOwnedComponent{SessionId="convoy.command",StableObjectId=new FixedString64Bytes(id),FactionId=1});
                    Em.AddComponentData(actor,new SkirmishUnitRoleComponent{Role=SkirmishRoleKind.ConvoyLogic,Category=SkirmishPopulationCategory.ObjectiveSupport});
                    Em.AddComponentData(actor,new SkirmishObjectiveRoleComponent{StableObjectId=new FixedString64Bytes(id),FactionId=1,Role=SkirmishObjectiveRoleKind.ConvoyTruck});
                    Em.AddComponentData(actor,new SkirmishConvoyObjectiveTruckComponent{Slot=(byte)(i+1),DefinitionVersion=1,DefinitionFingerprint=new FixedString128Bytes(setup.ObjectiveActorDefinitionFingerprint)});
                    Em.AddComponentData(actor,new SkirmishArmyGroupMembershipComponent{GroupId=(uint)(i+1),Domain=SkirmishPopulationCategory.ObjectiveSupport});
                    Em.AddComponentData(actor,new Faction{Id=1});Em.AddComponentData(actor,new UnitHealth{Current=220,Max=220});Em.AddComponent<SkirmishSharedActorTag>(actor);
                    Em.AddComponentData(actor,new UnitGrid{Cell=new int2(10+i*4,10)});Em.AddComponentData(actor,new UnitFootprint{Size=new int2(3,3)});
                    Em.AddComponentData(actor,new UnitMove{Speed=8,WalkSpeed=8,RoadSpeedMultiplier=1,ArriveDistance=.05f});
                    Em.AddComponentData(actor,new UnitMovementBehavior{UsesVehicleMotion=1});Em.AddComponentData(actor,new UnitSourcePrefabKey{Value=SkirmishConvoyTruckDefinition.ExistingChassisKey});
                    Em.AddComponentData(actor,LocalTransform.FromPosition(new float3(10.5f+i*4,0,10.5f)));Em.AddComponentData(actor,new LocalToWorld{Value=float4x4.identity});
                    Em.AddComponentData(actor,new CombatTargetPolicy{Domain=CombatTargetDomain.Ground,AllowedTargets=0,Visible=1});
                    Em.GetBuffer<SkirmishArmyGroupRecord>(Session).Add(new SkirmishArmyGroupRecord{GroupId=(uint)(i+1),FactionId=1,Role=SkirmishRoleKind.ConvoyLogic,AliveCount=1,MemberCount=1});
                }
                var expected=new SkirmishObjectiveAreaBinding{DefinitionId="fixture.convoy",SourceConfigId="synthetic.only",SourceAssetGuid=new string('a',32),
                    SourceContentHash="fixture",SourceSemanticFingerprint=new string('b',64),LayoutId="fixture",LayoutBindingFingerprint=new string('c',64),SourceMaxX=40,SourceMaxZ=40};
                var area=new SkirmishObjectiveAreaConfig{AreaSetId="synthetic-only",ObjectiveKind=SkirmishObjectiveKind.ConvoyEscort,Binding=expected,
                    Areas=new[]{new SkirmishObjectiveArea{StableAreaId="destination",Role=SkirmishObjectiveAreaRole.ConvoyDestination,Shape=SkirmishObjectiveAreaShape.AxisAlignedRectangle,
                        Points=new[]{new SkirmishObjectiveAreaPoint(30,30),new SkirmishObjectiveAreaPoint(35,35)}}}};
                area.Measurement=new SkirmishObjectiveAreaEvidence{ArtifactId="synthetic-only",ArtifactSha256=new string('d',64),GeometryFingerprint=SkirmishObjectiveAreaCompiler.ComputeGeometryFingerprint(area)};
                Assert.IsTrue(SkirmishObjectiveAreaCompiler.TryCompile(area,expected,area.ObjectiveKind,out var compiled,out string error),error);
                Assert.IsTrue(SkirmishNativeObjectiveCollector.TrySeal(Em,Session,compiled,compiled.LogicalFingerprint,out var collector,out error),error);
                Em.AddComponentObject(Session,new SkirmishNativeObjectiveRecord{Areas=compiled,Collector=collector,Convoy=SkirmishConvoyReducer.Create(SkirmishSizeId.Standard,"destination",collector.OriginalStableIds())});
            }
            public void Dispose(){Em.CompleteAllTrackedJobs();World.Dispose();pool.Dispose();occupied.Dispose();blocked.Dispose();friendly.Dispose();counts.Dispose();}
        }

        [Test] public void ObjectiveTruckCombatDomainIsGroundWhilePopulationAndSupplyStaySeparate()
        {
            Assert.AreEqual(SkirmishPopulationCategory.ObjectiveSupport,SkirmishRoleIds.Category(SkirmishRoleKind.ConvoyLogic));
            Assert.IsTrue(SkirmishExpandedEngagementService.DomainAllows(SkirmishTargetDomain.Ground,false,SkirmishPopulationCategory.ObjectiveSupport));
            Assert.IsFalse(SkirmishExpandedEngagementService.DomainAllows(SkirmishTargetDomain.Air|SkirmishTargetDomain.Infantry,false,SkirmishPopulationCategory.ObjectiveSupport));
            Assert.IsFalse(SkirmishExpandedEngagementService.DomainAllows(SkirmishTargetDomain.Ground,true,SkirmishPopulationCategory.ObjectiveSupport));
        }
        [Test] public void ActualNormalGroupMoveUsesNativeReceiptWithoutChangingTransformOrBudgets()
        {
            using var f=new Fixture();var actor=f.Trucks[0];var before=f.Em.GetComponentData<LocalTransform>(actor).Position;
            Assert.IsTrue(SkirmishArmyCommandService.TryIssueGroupOrder(f.Em,f.Session,1,1,SkirmishGroupOrderKind.Move,Entity.Null,new float3(25.5f,0,25.5f),out var decision),decision.Reason.ToString());
            Assert.AreEqual(1,decision.IssuedCount);Assert.AreEqual(before,f.Em.GetComponentData<LocalTransform>(actor).Position);
            Assert.AreEqual(1,f.Em.GetComponentData<SkirmishMoveIntentComponent>(actor).Active);
            Assert.IsTrue(f.Em.HasComponent<UnitTarget>(actor)||f.Em.HasComponent<UnitPathRequest>(actor));
            var cap=f.Em.GetComponentData<SkirmishCapacityComponent>(f.Session);Assert.AreEqual(12,cap.InfantryLive);Assert.AreEqual(1,cap.GroundLive);Assert.AreEqual(18,cap.SupplyLive);
            Assert.AreEqual(SkirmishPopulationCategory.ObjectiveSupport,f.Em.GetComponentData<SkirmishUnitRoleComponent>(actor).Category);
        }
        [Test] public void ForeignCopiedMarkerAndUnplannedOriginalCannotUseSupportMoveException()
        {
            foreach(string condition in new[]{"marker","unplanned","wrong-role","wrong-attempt"})
            {
                using var f=new Fixture();var actor=f.Trucks[0];
                if(condition=="marker"){var marker=f.Em.GetComponentData<SkirmishConvoyObjectiveTruckComponent>(actor);marker.DefinitionFingerprint="changed";f.Em.SetComponentData(actor,marker);}
                if(condition=="unplanned"){var own=f.Em.GetComponentData<SkirmishAttemptOwnedComponent>(actor);own.StableObjectId="not-original";f.Em.SetComponentData(actor,own);}
                if(condition=="wrong-role"){var role=f.Em.GetComponentData<SkirmishUnitRoleComponent>(actor);role.Role=SkirmishRoleKind.LogisticsTruck;f.Em.SetComponentData(actor,role);}
                if(condition=="wrong-attempt"){var own=f.Em.GetComponentData<SkirmishAttemptOwnedComponent>(actor);own.SessionId="other";f.Em.SetComponentData(actor,own);}
                Assert.IsFalse(SkirmishArmyCommandService.TryIssueGroupOrder(f.Em,f.Session,1,1,SkirmishGroupOrderKind.Move,Entity.Null,new float3(25.5f,0,25.5f),out _),condition);
                Assert.IsFalse(f.Em.HasComponent<SkirmishMoveIntentComponent>(actor),condition);
            }
        }
        [Test] public void OrdinaryLogisticsMoveRetainsExistingCommandPath()
        {
            using var f=new Fixture();var actor=f.Trucks[0];f.Em.RemoveComponent<SkirmishConvoyObjectiveTruckComponent>(actor);f.Em.RemoveComponent<SkirmishObjectiveRoleComponent>(actor);
            var role=f.Em.GetComponentData<SkirmishUnitRoleComponent>(actor);role.Role=SkirmishRoleKind.LogisticsTruck;role.Category=SkirmishPopulationCategory.LogisticsSupport;f.Em.SetComponentData(actor,role);
            var membership=f.Em.GetComponentData<SkirmishArmyGroupMembershipComponent>(actor);membership.Domain=SkirmishPopulationCategory.LogisticsSupport;f.Em.SetComponentData(actor,membership);
            Assert.IsTrue(SkirmishArmyCommandService.TryIssueGroupOrder(f.Em,f.Session,1,1,SkirmishGroupOrderKind.Move,Entity.Null,new float3(25.5f,0,25.5f),out var decision),decision.Reason.ToString());
        }
        [Test] public void NativeCommandedSiegeMayTargetVisibleCargoButBridgeNeverAppliesDamage()
        {
            foreach(bool visible in new[]{false,true})
            {
                using var f=new Fixture();var target=f.Trucks[0];f.Em.SetComponentData(target,LocalTransform.FromPosition(new float3(30,0,0)));
                var policy=f.Em.GetComponentData<CombatTargetPolicy>(target);policy.Visible=0;f.Em.SetComponentData(target,policy);
                // Reveal is the player-observer convenience API. The attacker in
                // this fixture is faction 2, so set that observer's actual contact
                // record independently; player sight must never leak into it.
                SkirmishFogService.Reveal(f.Em,target);
                var knowledge=f.Em.GetBuffer<SkirmishContactKnowledgeElement>(target);
                for(int i=0;i<knowledge.Length;i++)if(knowledge[i].ObserverFactionId==2)
                {var contact=knowledge[i];contact.Sight=visible?SkirmishContactSight.Visible:SkirmishContactSight.Unknown;knowledge[i]=contact;}
                Assert.IsTrue(SkirmishFogService.IsVisibleToFaction(f.Em,target,1));
                Assert.AreEqual(visible,SkirmishFogService.IsVisibleToFaction(f.Em,target,2));
                var shooter=f.Em.CreateEntity(typeof(SkirmishAttemptOwnedComponent),typeof(SkirmishSharedActorTag),typeof(SkirmishMoveIntentComponent),
                    typeof(SkirmishRoleOverlayComponent),typeof(UnitHealth),typeof(LocalTransform),typeof(SkirmishUnitRoleComponent),typeof(GroundMissileLauncherComponent),typeof(GroundMissileLauncherStateComponent));
                f.Em.SetComponentData(shooter,new SkirmishAttemptOwnedComponent{SessionId="convoy.command",FactionId=2});
                f.Em.SetComponentData(shooter,new UnitHealth{Current=100,Max=100});f.Em.SetComponentData(shooter,LocalTransform.Identity);
                f.Em.SetComponentData(shooter,new SkirmishUnitRoleComponent{Role=SkirmishRoleKind.Siege,Category=SkirmishPopulationCategory.Ground});
                f.Em.SetComponentData(shooter,new GroundMissileLauncherComponent{MinRange=24,MaxRange=48,Damage=40,PrepareSeconds=3,ReloadSeconds=8});
                f.Em.SetComponentData(shooter,new SkirmishRoleOverlayComponent{Damage=40,RangeWorld=48,TargetDomains=SkirmishTargetDomain.Ground});
                f.Em.SetComponentData(shooter,new SkirmishMoveIntentComponent{Order=SkirmishGroupOrderKind.Attack,AttackTarget=target});
                Assert.AreEqual(0,SkirmishExpandedEngagementService.Step(f.Em,f.Session,1,false));
                Assert.AreEqual(visible,f.Em.HasComponent<EngageTarget>(shooter));Assert.AreEqual(220,f.Em.GetComponentData<UnitHealth>(target).Current);
            }
        }
        public static void RunFocusedValidation()
        {
            var tests=new SkirmishConvoyCommandDomainTests();tests.ObjectiveTruckCombatDomainIsGroundWhilePopulationAndSupplyStaySeparate();
            tests.ActualNormalGroupMoveUsesNativeReceiptWithoutChangingTransformOrBudgets();tests.ForeignCopiedMarkerAndUnplannedOriginalCannotUseSupportMoveException();
            tests.OrdinaryLogisticsMoveRetainsExistingCommandPath();tests.NativeCommandedSiegeMayTargetVisibleCargoButBridgeNeverAppliesDamage();
            Debug.Log("[SkirmishConvoyCommandDomainTests] result=Passed tests=5 nativeMissionReadiness=Pending");
        }
    }
}
#endif
