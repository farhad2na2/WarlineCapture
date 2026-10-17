#if UNITY_EDITOR
using System;
using Game.Components;
using Game.Configs;
using Game.Runtime;
using Game.Skirmish.Contracts;
using NUnit.Framework;
using Unity.Entities;
using Unity.Transforms;
using UnityEngine;

namespace Game.Tests.Editor
{
    public sealed class SkirmishGenericGroundEnemyTests
    {
        private static SkirmishResolvedSetup Setup(bool established=false) => new SkirmishResolvedSetup
        {
            ObjectiveKind=SkirmishObjectiveKind.BaseAssault, ArmyProfileId=SkirmishArmyProfileId.GroundManeuver,
            StartPackageId=established?SkirmishStartPackageId.EstablishedBase:SkirmishStartPackageId.FieldBase,
            Readiness=established?SkirmishReadinessStage.Established:SkirmishReadinessStage.Field,
            MeasuredLayoutBound=true,LayoutBindingFingerprint=new string('b',64),PlayerFaction=1,EnemyFaction=2,
            Forces=new[]{new SkirmishResolvedForceEntry{FactionId=2,RoleId=SkirmishRoleIds.TransportHeli,
                RoleKind=SkirmishRoleKind.TransportHeli,RuntimePrefabKey=SkirmishRoleCatalogConfig.RuntimePrefabKey(SkirmishRoleKind.TransportHeli),Quantity=1}}
        };
        [Test] public void BothLegalStartsUseGenericBindingAndLegacyWithoutItRemainsExcluded()
        {
            Assert.IsTrue(SkirmishGenericGroundEnemyRollout.Supports(Setup()));
            Assert.IsTrue(SkirmishGenericGroundEnemyRollout.Supports(Setup(true)));
            var legacy=Setup();legacy.LayoutBindingFingerprint="";
            Assert.IsFalse(SkirmishGenericGroundEnemyRollout.Supports(legacy));
            Assert.IsFalse(SkirmishSetupCompiler.SupportsGroundCommanderRollout(legacy));
            var existingCombined=Setup();existingCombined.ArmyProfileId=SkirmishArmyProfileId.CombinedArms;
            existingCombined.PlayerAir=existingCombined.EnemyAir=0;
            Assert.IsTrue(SkirmishSetupCompiler.SupportsGroundCommanderRollout(existingCombined));
        }
        [Test] public void UnsupportedObjectiveReadinessAndStartingAircraftRemainExcluded()
        {
            var setup=Setup();setup.ObjectiveKind=SkirmishObjectiveKind.ConvoyEscort;
            Assert.IsFalse(SkirmishGenericGroundEnemyRollout.Supports(setup));
            setup=Setup();setup.Readiness=SkirmishReadinessStage.FullArsenal;
            Assert.IsFalse(SkirmishGenericGroundEnemyRollout.Supports(setup));
            foreach(var role in new[]{SkirmishRoleKind.Strike,SkirmishRoleKind.TransportPlane,SkirmishRoleKind.AntiAir,SkirmishRoleKind.Drone})
            {setup=Setup();setup.Forces[0].RoleKind=role;Assert.IsFalse(SkirmishGenericGroundEnemyRollout.Supports(setup),role.ToString());}
        }
        [Test] public void AuthoredTransportIdentityIsExactAndNeverAProducedReceiptAlias()
        {
            var setup=Setup(true);string id=SkirmishRoleIds.TransportHeli+".2.0.0";
            Assert.IsTrue(SkirmishGenericGroundEnemyRollout.IsAuthoredStartingTransport(setup,id,SkirmishRoleKind.TransportHeli,2));
            Assert.IsFalse(SkirmishGenericGroundEnemyRollout.IsAuthoredStartingTransport(setup,id,SkirmishRoleKind.TransportHeli,1));
            Assert.IsFalse(SkirmishGenericGroundEnemyRollout.IsAuthoredStartingTransport(setup,id+"0",SkirmishRoleKind.TransportHeli,2));
            setup.Forces[0].RuntimePrefabKey="wrong";
            Assert.IsFalse(SkirmishGenericGroundEnemyRollout.IsAuthoredStartingTransport(setup,id,SkirmishRoleKind.TransportHeli,2));
        }
        [Test] public void ProhibitedAntiAirCannotBlockLegalGroundPurchaseIntent()
        {
            var facts=new SkirmishEnemyPerceptionBuilder.Snapshot();
            facts.Visible.Add(new SkirmishEnemyPerceptionBuilder.Contact{Category=SkirmishPopulationCategory.Air});
            var package=new SkirmishEnemyGroupAllocation.Package();var setup=Setup();
            Assert.AreEqual(SkirmishRoleKind.Rocketeer,SkirmishEnemyCommanderService.WantedRole(facts,package,setup));
            setup.ArmyProfileId=SkirmishArmyProfileId.CombinedArms;
            Assert.AreEqual(SkirmishRoleKind.AntiAir,SkirmishEnemyCommanderService.WantedRole(facts,package,setup));
        }
        [Test] public void ActualOwnProjectionAdmitsOnlyExactAuthoredHelicopterWithoutInventingReceipt()
        {
            using var world=new World("Generic ground authored transport provenance");var em=world.EntityManager;
            var setup=Setup(true);var session=em.CreateEntity(typeof(SkirmishExpandedSessionComponent));
            em.SetComponentData(session,new SkirmishExpandedSessionComponent{SessionId="ground.enemy"});
            em.AddComponentObject(session,new SkirmishResolvedSetupRecord{Setup=setup});
            var actor=em.CreateEntity(typeof(SkirmishAttemptOwnedComponent),typeof(SkirmishUnitRoleComponent),
                typeof(SkirmishArmyGroupMembershipComponent),typeof(LocalTransform),typeof(UnitHealth),typeof(Faction),
                typeof(SkirmishSharedActorTag),typeof(UnitAirComponent),typeof(UnitTransportCapacity),typeof(UnitSourcePrefabKey));
            em.SetComponentData(actor,new SkirmishAttemptOwnedComponent{SessionId="ground.enemy",StableObjectId=SkirmishRoleIds.TransportHeli+".2.0.0",FactionId=2});
            em.SetComponentData(actor,new Faction{Id=2});em.SetComponentData(actor,new UnitHealth{Current=100,Max=100});
            em.SetComponentData(actor,new UnitTransportCapacity{SoldierCapacity=4});
            em.SetComponentData(actor,new UnitSourcePrefabKey{Value=SkirmishRoleCatalogConfig.RuntimePrefabKey(SkirmishRoleKind.TransportHeli)});
            em.SetComponentData(actor,new SkirmishUnitRoleComponent{Role=SkirmishRoleKind.TransportHeli,Category=SkirmishPopulationCategory.Air});
            em.SetComponentData(actor,new SkirmishArmyGroupMembershipComponent{GroupId=7,Domain=SkirmishPopulationCategory.Air});
            Assert.AreEqual(1,SkirmishEnemyCapabilityService.ReadOwnActors(em,session).Count);
            Assert.IsFalse(em.HasBuffer<SkirmishProductionReservation>(session),"Authored actor must not manufacture a paid receipt.");
            var own=em.GetComponentData<SkirmishAttemptOwnedComponent>(actor);own.ReservationId=9;em.SetComponentData(actor,own);
            Assert.AreEqual(0,SkirmishEnemyCapabilityService.ReadOwnActors(em,session).Count,"Unpaid produced actor cannot impersonate initial actor.");
            own.ReservationId=0;own.StableObjectId="different";em.SetComponentData(actor,own);
            Assert.AreEqual(0,SkirmishEnemyCapabilityService.ReadOwnActors(em,session).Count);
            own.StableObjectId=SkirmishRoleIds.TransportHeli+".2.0.0";em.SetComponentData(actor,own);
            em.SetComponentData(actor,new UnitSourcePrefabKey{Value="Unit_Veh_Plane_Transport"});
            Assert.AreEqual(0,SkirmishEnemyCapabilityService.ReadOwnActors(em,session).Count,"Wrong native chassis cannot alias an authored helicopter.");
        }
        [Test] public void GenericGroundCombatReplenishmentIncludesAuthoredVehicleRolesButNoSupportOrAir()
        {
            var setup=Setup();
            setup.RoleOverlays=SkirmishRoleOverlayCatalog.CreateGroundManeuverSlice();
            foreach(var role in new[]{SkirmishRoleKind.Marksman,SkirmishRoleKind.Breacher,SkirmishRoleKind.Car,
                SkirmishRoleKind.ApcFast,SkirmishRoleKind.ApcArmored,SkirmishRoleKind.ApcHeavy,SkirmishRoleKind.Siege})
                Assert.IsTrue(SkirmishGenericGroundEnemyRollout.IsReplenishableCombatRole(setup,role),role.ToString());
            foreach(var role in new[]{SkirmishRoleKind.LogisticsTruck,SkirmishRoleKind.Tanker,SkirmishRoleKind.Radar,
                SkirmishRoleKind.TransportHeli,SkirmishRoleKind.Drone,SkirmishRoleKind.AntiAir})
                Assert.IsFalse(SkirmishGenericGroundEnemyRollout.IsReplenishableCombatRole(setup,role),role.ToString());
            setup.LayoutBindingFingerprint="";
            Assert.IsFalse(SkirmishGenericGroundEnemyRollout.IsReplenishableCombatRole(setup,SkirmishRoleKind.Siege));
        }
        public static void RunFocusedValidation()
        {
            var tests=new SkirmishGenericGroundEnemyTests();
            tests.BothLegalStartsUseGenericBindingAndLegacyWithoutItRemainsExcluded();
            tests.UnsupportedObjectiveReadinessAndStartingAircraftRemainExcluded();
            tests.AuthoredTransportIdentityIsExactAndNeverAProducedReceiptAlias();
            tests.ProhibitedAntiAirCannotBlockLegalGroundPurchaseIntent();
            tests.ActualOwnProjectionAdmitsOnlyExactAuthoredHelicopterWithoutInventingReceipt();
            tests.GenericGroundCombatReplenishmentIncludesAuthoredVehicleRolesButNoSupportOrAir();
            Debug.Log("[SkirmishGenericGroundEnemyTests] result=Passed tests=6 nativeMissionReadiness=Pending");
        }
    }
}
#endif
