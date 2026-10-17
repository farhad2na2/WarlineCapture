using Game.Components;
using Game.Configs;
using Game.Runtime;
using Game.Skirmish.Contracts;
using NUnit.Framework;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
namespace Game.Tests.Editor
{
    public sealed class AriaOffensiveAirNativeTests
    {
        // Tests the passive normal-result observer, not a flight/source-fit fixture.
        [TestCase(0)][TestCase(1)][TestCase(2)][TestCase(3)][TestCase(4)][TestCase(5)]
        public void NativeAcceptedOrderReadoutRejectsForeignUnarmedChangedAndDuplicateSelection(int mutation)
        {
            using var world=new World("PublicAirPressure");var em=world.EntityManager;
            var session=em.CreateEntity(typeof(SkirmishExpandedSessionComponent));
            em.SetComponentData(session,new SkirmishExpandedSessionComponent{SessionId="air-attempt",SetupHash=99,Phase=SkirmishSessionPhase.Playing,ObjectiveKind=SkirmishObjectiveKind.ConvoyEscort});
            var role=mutation==2?SkirmishRoleKind.TransportPlane:SkirmishRoleKind.Strike;
            em.AddComponentObject(session,new SkirmishResolvedSetupRecord {Setup=new SkirmishResolvedSetup {SetupHash=99,
                ArmyProfileId=mutation==5?SkirmishArmyProfileId.GroundManeuver:SkirmishArmyProfileId.CombinedArms,
                RoleOverlays=new[]{new SkirmishRoleOverlay{RoleKind=role,RoleId=role==SkirmishRoleKind.TransportPlane?SkirmishRoleIds.TransportPlane:SkirmishRoleIds.Strike}}}});
            var actor=em.CreateEntity(typeof(SelectedUnitTag),typeof(SkirmishAttemptOwnedComponent),typeof(Faction),typeof(UnitHealth),typeof(LocalTransform),typeof(UnitGrid),
                typeof(UnitAirComponent),typeof(UnitAirMovement),typeof(SkirmishUnitRoleComponent),typeof(UnitSourcePrefabKey),typeof(SkirmishArmyGroupMembershipComponent),
                typeof(UnitAttack),typeof(UnitCombat),typeof(CombatTargetPolicy),typeof(AttackMoveOrder));
            em.SetComponentData(actor,new SkirmishAttemptOwnedComponent{SessionId=mutation==1?"foreign":"air-attempt",StableObjectId="air.1",FactionId=1});
            em.SetComponentData(actor,new Faction{Id=1});em.SetComponentData(actor,new UnitHealth{Current=70,Max=100});em.SetComponentData(actor,LocalTransform.Identity);
            em.SetComponentData(actor,new SkirmishUnitRoleComponent{Role=role,Category=SkirmishPopulationCategory.Air});
            em.SetComponentData(actor,new UnitSourcePrefabKey{Value=new FixedString64Bytes(SkirmishRoleCatalogConfig.RuntimePrefabKey(role))});
            em.SetComponentData(actor,new SkirmishArmyGroupMembershipComponent{GroupId=7,Domain=SkirmishPopulationCategory.Air});
            em.SetComponentData(actor,new UnitAttack{Damage=30});em.SetComponentData(actor,new UnitCombat{CanAttack=1});
            em.SetComponentData(actor,new CombatTargetPolicy{AllowedTargets=CombatTargetDomain.Ground});
            em.SetComponentData(actor,new AttackMoveOrder{Destination=new int2(4,5),Position=new float3(4,0,5)});
            em.AddBuffer<SkirmishArmyGroupRecord>(session).Add(new SkirmishArmyGroupRecord{GroupId=7,FactionId=1,Role=role,Domain=SkirmishPopulationCategory.Air,MemberCount=1,AliveCount=1});
            if(mutation==4)em.CreateEntity(typeof(SelectedUnitTag));
            var request=new RtsSelectionCommandIntentRequestElement{Kind=RtsSelectionCommandIntentKind.Attack,RequestId=5,Frame=20,HasScreenPosition=1,HasTargetCell=1,ScreenPosition=new float2(70,80),TargetCell=new int2(4,5)};
            SkirmishPublicAirPressureFeedback.Observe(em,request,false);Assert.IsFalse(em.HasComponent<SkirmishPublicAirPressureReceipt>(actor));
            SkirmishPublicAirPressureFeedback.Observe(em,request,true);
            if(mutation==3)em.SetComponentData(actor,new AttackMoveOrder{Destination=new int2(8,8)});
            Assert.AreEqual(mutation==0,SkirmishPublicAirPressureFeedback.TryRead(em,actor,out var receipt));
            Assert.AreEqual(70,em.GetComponentData<UnitHealth>(actor).Current,"Readout cannot heal or issue damage");
            if(mutation==0)
            {
                Assert.AreEqual(5,receipt.RequestId);Assert.AreEqual(actor,receipt.Actor);
                var own=em.GetComponentData<SkirmishAttemptOwnedComponent>(actor);own.StableObjectId="replaced";em.SetComponentData(actor,own);
                Assert.IsFalse(SkirmishPublicAirPressureFeedback.TryRead(em,actor,out _));
            }
        }
    }
}
