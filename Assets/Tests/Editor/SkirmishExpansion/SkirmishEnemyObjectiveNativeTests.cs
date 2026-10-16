#if UNITY_EDITOR
using System;
using Game.Components;
using Game.Configs;
using Game.Runtime;
using Game.Skirmish.Contracts;
using NUnit.Framework;
using Unity.Core;
using Unity.Entities;

namespace Game.Tests.Editor
{
    public sealed class SkirmishEnemyObjectiveNativeTests
    {
        private static SkirmishResolvedSetup Setup(SkirmishObjectiveKind kind)=>new SkirmishResolvedSetup {
            CatalogId="S031",DefinitionId="fixture.objective",ContentVersion=1,ContentHash="same-fixture-content",
            SetupHash=123,ObjectiveKind=kind,ArmyProfileId=SkirmishArmyProfileId.GroundManeuver,
            PlayerFaction=1,EnemyFaction=2,DifficultyId=SkirmishDifficultyId.Regular,SizeId=SkirmishSizeId.Standard,
            StartPackageId=SkirmishStartPackageId.FieldBase,Readiness=SkirmishReadinessStage.Field,
            DeadlineSeconds=1200,MeasuredLayoutBound=true,LayoutBindingFingerprint=new string('a',64),
            MissionLogicalBindingFingerprint=new string('b',64),RoleOverlays=SkirmishRoleOverlayCatalog.CreateGroundManeuverSlice(),
            Forces=new[]{new SkirmishResolvedForceEntry {FactionId=2,RoleId=SkirmishRoleIds.Rifle,RoleKind=SkirmishRoleKind.Rifle,Quantity=4,
                RuntimePrefabKey=SkirmishRoleCatalogConfig.RuntimePrefabKey(SkirmishRoleKind.Rifle)}}};

        [Test] public void NonBaScopeRequiresAllIndependentLogicalPinsAndExcludesUnsupportedStartingAir()
        {
            foreach(var kind in new[]{SkirmishObjectiveKind.FrontlineControl,SkirmishObjectiveKind.Breakthrough,SkirmishObjectiveKind.ConvoyEscort})
            {
                var setup=Setup(kind);Assert.IsTrue(SkirmishEnemyObjectivePolicyScope.Supports(setup));
                setup.MissionLogicalBindingFingerprint="";Assert.IsFalse(SkirmishEnemyObjectivePolicyScope.Supports(setup));
                setup=Setup(kind);setup.Forces[0].RoleKind=SkirmishRoleKind.Strike;Assert.IsFalse(SkirmishEnemyObjectivePolicyScope.Supports(setup));
            }
            Assert.IsFalse(SkirmishEnemyObjectivePolicyScope.Supports(Setup(SkirmishObjectiveKind.BaseAssault)));
        }
        [Test] public void ActualPolicyCompilerBindsObjectiveAndLogicalDigestWithoutChangingBaScope()
        {
            var fc=Setup(SkirmishObjectiveKind.FrontlineControl);var bt=Setup(SkirmishObjectiveKind.Breakthrough);
            Assert.IsTrue(SkirmishEnemyPolicyCompiler.TryCompile(fc,null,out var first));
            Assert.IsTrue(SkirmishEnemyPolicyCompiler.TryCompile(bt,null,out var second));
            Assert.AreNotEqual(first.Fingerprint,second.Fingerprint);
            fc.MissionLogicalBindingFingerprint=new string('c',64);
            Assert.IsTrue(SkirmishEnemyPolicyCompiler.TryCompile(fc,null,out var changed));Assert.AreNotEqual(first.Fingerprint,changed.Fingerprint);
            fc.MissionLogicalBindingFingerprint="";Assert.IsFalse(SkirmishEnemyPolicyCompiler.TryCompile(fc,null,out _));
        }
        [Test] public void MissingActualProviderCannotCreatePlannerOrdersOrSpendStocks()
        {
            using var world=new World("NonBAEnemyMissingProvider");world.SetTime(new TimeData(1,.1f));var em=world.EntityManager;
            var setup=Setup(SkirmishObjectiveKind.FrontlineControl);Assert.IsTrue(SkirmishEnemyPolicyCompiler.TryCompile(setup,null,out setup.EnemyPolicy));
            var session=em.CreateEntity(typeof(SkirmishExpandedSessionComponent),typeof(SkirmishObjectiveClockComponent),typeof(SkirmishEnemyStockComponent));
            em.SetComponentData(session,new SkirmishExpandedSessionComponent{SessionId="policy.fixture",ObjectiveKind=setup.ObjectiveKind,Phase=SkirmishSessionPhase.Playing,SpawnComplete=1});
            em.SetComponentData(session,new SkirmishObjectiveClockComponent{Playing=1});
            em.SetComponentData(session,new SkirmishEnemyStockComponent{Materials=37,Oil=12,Fuel=9});
            em.AddComponentObject(session,new SkirmishResolvedSetupRecord{Setup=setup});
            int before=em.UniversalQuery.CalculateEntityCount();
            SkirmishEnemyObjectiveCommanderService.Evaluate(em,session,setup,SkirmishArmyProfileConfig.ResolveCached(setup.ArmyProfileId));
            Assert.AreEqual(before,em.UniversalQuery.CalculateEntityCount());
            Assert.IsFalse(em.HasComponent<SkirmishEnemyPlannerV1Component>(session));
            Assert.IsFalse(em.HasComponent<SkirmishEnemyProductionIntentV1Component>(session));
            Assert.IsFalse(em.HasBuffer<SkirmishEnemyGroupAssignmentV1>(session));
            var stock=em.GetComponentData<SkirmishEnemyStockComponent>(session);Assert.AreEqual(37,stock.Materials);Assert.AreEqual(12,stock.Oil);Assert.AreEqual(9,stock.Fuel);
        }
        [Test] public void NonBaStrategyCannotFallThroughToBaCounterOrBaseAttack()
        {
            using var world=new World("NonBAEnemyNoBAFallback");world.SetTime(new TimeData(1,.1f));var em=world.EntityManager;
            var setup=Setup(SkirmishObjectiveKind.Breakthrough);var session=em.CreateEntity();
            em.AddComponentObject(session,new SkirmishResolvedSetupRecord{Setup=setup});
            using var owned=em.CreateEntityQuery(typeof(SkirmishAttemptOwnedComponent));
            var result=SkirmishEnemyStrategySystem.Evaluate(em,session,owned,SkirmishArmyProfileConfig.ResolveCached(setup.ArmyProfileId));
            Assert.AreEqual(SkirmishStrategyPriority.None,result.Priority);
            Assert.IsFalse(em.HasComponent<SkirmishEnemyStrategyComponent>(session));Assert.IsFalse(em.HasComponent<SkirmishEnemyPlannerV1Component>(session));
        }
        [Test] public void MissingPublicProviderRejectsWithoutReadingOrPublishingHiddenOriginals()
        {
            using var world=new World("NonBAEnemyNoHiddenOriginals");var em=world.EntityManager;var session=em.CreateEntity();
            var setup=Setup(SkirmishObjectiveKind.ConvoyEscort);var facts=new SkirmishEnemyPerceptionBuilder.Snapshot();
            int before=em.UniversalQuery.CalculateEntityCount();
            Assert.IsFalse(SkirmishEnemyObjectivePublicPerspective.TryBuild(em,session,setup,facts,out var goal,out string error));
            Assert.IsNull(goal);Assert.IsNotEmpty(error);Assert.AreEqual(before,em.UniversalQuery.CalculateEntityCount());
            string first=SkirmishEnemyObjectivePublicPerspective.PublicContactId(new string('x',61));
            Assert.AreEqual(61,first.Length);Assert.AreEqual(first,SkirmishEnemyObjectivePublicPerspective.PublicContactId(new string('x',61)));
            Assert.AreNotEqual(first,SkirmishEnemyObjectivePublicPerspective.PublicContactId(new string('y',61)));
        }
    }
}
#endif
