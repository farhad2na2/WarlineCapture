#if UNITY_EDITOR
using System;
using Game.Components;
using Game.Runtime;
using NUnit.Framework;
using Unity.Core;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
namespace Game.Tests.Editor
{
    public sealed class SupportSmokeValidation
    {
        public static void RunFocusedValidation()
        {
            try{var t=new SupportSmokeValidation();t.CoverResetsAndNeverStacks();t.RealShotsAndObservationsAgree(false);t.RealShotsAndObservationsAgree(true);t.BuildingDefenseUsesSameMitigation();
                Debug.Log("[SupportSmokeValidation] result=Passed tests=4");ValidationExit.Exit(0);}
            catch(Exception ex){Debug.LogException(ex);Debug.LogError("[SupportSmokeValidation] result=Failed");ValidationExit.Exit(1);throw;}
        }
        [Test] public void CoverResetsAndNeverStacks()
        {
            Assert.AreEqual(65,SupportDamageUtilitySystemHelper.Apply(100,650));Assert.AreEqual(7,SupportDamageUtilitySystemHelper.Apply(10,650));
            Assert.AreEqual(1,SupportDamageUtilitySystemHelper.Apply(1,650));Assert.AreEqual(0,SupportDamageUtilitySystemHelper.Apply(0,650));
            Assert.AreEqual(1395864371,SupportDamageUtilitySystemHelper.Apply(int.MaxValue,650));
            using var f=new SupportValidationFixture();f.Store(3);SupportAbilityRequestSystem.Process(f.Em,f.Root,f.Request());
            var zone=f.Em.CreateEntity(typeof(SupportSmokeZoneComponent));f.Em.SetComponentData(zone,new SupportSmokeZoneComponent {SessionToken="support-test",AttemptOrdinal=1,RadiusSquared=144,ExpiresAt=15,DirectDamagePermille=650});
            var friendly=f.Unit(float3.zero,1);var enemy=f.Unit(new float3(12,0,0));var air=f.Unit(float3.zero);f.Em.AddComponentData(air,new UnitAirMovement());
            var para=f.Unit(float3.zero);f.Em.AddComponent<UnitTransportParachuteDropComponent>(para);
            var cargo=f.Unit(float3.zero);f.Em.AddComponent<UnitTransportCargoDropComponent>(cargo);
            var rope=f.Unit(float3.zero);f.Em.AddComponent<UnitTransportRopeDropComponent>(rope);
            var system=f.World.CreateSystem<SupportSmokeSystem>();system.Update(f.World.Unmanaged);
            Assert.AreEqual(650,f.Em.GetComponentData<SupportRangedCoverComponent>(friendly).DirectDamagePermille);Assert.AreEqual(650,f.Em.GetComponentData<SupportRangedCoverComponent>(enemy).DirectDamagePermille);
            Assert.AreEqual(1000,f.Em.GetComponentData<SupportRangedCoverComponent>(air).DirectDamagePermille);
            Assert.AreEqual(1000,f.Em.GetComponentData<SupportRangedCoverComponent>(para).DirectDamagePermille);
            Assert.AreEqual(1000,f.Em.GetComponentData<SupportRangedCoverComponent>(cargo).DirectDamagePermille);
            Assert.AreEqual(1000,f.Em.GetComponentData<SupportRangedCoverComponent>(rope).DirectDamagePermille);
            f.Em.SetComponentData(enemy,LocalTransform.FromPosition(new float3(13,0,0)));system.Update(f.World.Unmanaged);Assert.AreEqual(1000,f.Em.GetComponentData<SupportRangedCoverComponent>(enemy).DirectDamagePermille);
            var session=f.Em.GetComponentData<SupportSessionComponent>(f.Root);session.SimulationSeconds=15;f.Em.SetComponentData(f.Root,session);system.Update(f.World.Unmanaged);
            Assert.AreEqual(1000,f.Em.GetComponentData<SupportRangedCoverComponent>(friendly).DirectDamagePermille);
        }
        [TestCase(false)] [TestCase(true)] public void RealShotsAndObservationsAgree(bool mainBranch)
        {
            using var f=new SupportValidationFixture();var em=f.Em;
            var grid=em.CreateEntity(typeof(GridConfig));em.SetComponentData(grid,new GridConfig {Width=100,Height=100,CellSize=1});
            var target=f.Unit(new float3(5,0,0));em.AddComponentData(target,new SupportRangedCoverComponent {DirectDamagePermille=650});
            var queue=em.CreateEntity(typeof(CombatDamageObservationQueueComponent));em.AddBuffer<CombatDamageObservationElement>(queue);
            for(int i=0;i<2;i++)
            {
                var attacker=f.Unit(float3.zero,1);em.AddComponentData(attacker,new EngageTarget {Target=target,Position=new float3(5,0,0),IsCommanded=1});
                em.AddComponentData(attacker,new UnitCombat {CanAttack=1});
                em.AddComponentData(attacker,new UnitAttack {Damage=100,Range=50,CooldownSeconds=1,TraceVisibleSeconds=.1f});
                em.AddComponent<UnitAttackCooldownComponent>(attacker);em.AddComponent<UnitAttackTraceComponent>(attacker);em.AddComponent<UnitAttackAnimationComponent>(attacker);
                // No launcher state: exercise the main direct-shot fallback without creating a missile.
                if(mainBranch)em.AddComponent<GroundMissileLauncherComponent>(attacker);
            }
            f.World.SetTime(new TimeData(.1,.1f));var system=f.World.CreateSystem<UnitAttackSystem>();system.Update(f.World.Unmanaged);
            Assert.AreEqual(870,em.GetComponentData<UnitHealth>(target).Current,"Each 100 shot must reduce to 65 before aggregation.");
            var observations=em.GetBuffer<CombatDamageObservationElement>(queue);Assert.AreEqual(1,observations.Length);Assert.AreEqual(130,observations[0].DamageApplied);
        }
        [Test] public void BuildingDefenseUsesSameMitigation()
        {
            using var f=new SupportValidationFixture();var em=f.Em;var target=f.Unit(new float3(5,0,0));em.AddComponentData(target,new SupportRangedCoverComponent {DirectDamagePermille=650});
            var queue=em.CreateEntity(typeof(CombatDamageObservationQueueComponent));em.AddBuffer<CombatDamageObservationElement>(queue);
            var tower=em.CreateEntity(typeof(RuntimeBuildingCombatTag),typeof(BuildingDefenseWeapon),typeof(UnitHealth),typeof(Faction),typeof(LocalTransform),typeof(UnitAttackTraceComponent));
            em.SetComponentData(tower,new BuildingDefenseWeapon {Range=100,CooldownSeconds=1,Damage=100,MaxConcurrentAttacks=1});
            em.SetComponentData(tower,new UnitHealth {Current=700,Max=700});em.SetComponentData(tower,new Faction {Id=1});em.SetComponentData(tower,LocalTransform.FromPosition(float3.zero));em.AddBuffer<BuildingDefenseAttackSlot>(tower);
            f.World.SetTime(new TimeData(.1,.1f));var system=f.World.CreateSystem<BuildingDefenseAttackSystem>();system.Update(f.World.Unmanaged);
            Assert.AreEqual(935,em.GetComponentData<UnitHealth>(target).Current);Assert.AreEqual(65,em.GetBuffer<CombatDamageObservationElement>(queue)[0].DamageApplied);
        }
    }
}
#endif
