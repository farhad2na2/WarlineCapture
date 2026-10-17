#if UNITY_EDITOR
using System;
using System.Linq;
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
    public sealed class SkirmishEnemyAirCombined13Tests
    {
        static readonly string[] ExactIds={"S006","S027","S028","S030","S051","S052","S054","S075","S076","S078","S099","S100","S102"};
        static void WithCompiled(string id,Action<SkirmishResolvedSetup> assertion,bool allSizes=false)
        {
            SkirmishGenericMissionDispatchTests.WithSourceBoundUnqueuedDefinition(id,(definition,owned)=>
            {
                Assert.IsTrue(SkirmishSetupMatrixTable.TryLoadPackaged(out var matrix,out string error),error);
                var p=Resources.Load<SkirmishExpansionCatalogConfig>(SkirmishExpansionCatalogConfig.ResourceName).ToAuthoredSet();
                Assert.IsTrue(SkirmishSetupMatrixTable.TryFind(matrix,id,SkirmishSizeId.Standard,out var canonical));
                var source=definition.CampaignMap;
                definition.Configure(id,p.ObjectiveBa,canonical.ArmyProfile==SkirmishArmyProfileId.AirMobile?p.ArmyAir:p.ArmyCombined,
                    canonical.StartProfile==SkirmishStartPackageId.FieldBase?p.StartField:p.StartEstablished,
                    p.Economy,p.Upgrades,p.Intel,p.Roles,definition.MapLayout);definition.BindCampaignMap(source);
                foreach(var size in allSizes?new[]{SkirmishSizeId.Standard,SkirmishSizeId.War,SkirmishSizeId.LargeWar}:new[]{SkirmishSizeId.Standard})
                {
                    Assert.IsTrue(SkirmishSetupCompiler.TryCompile(definition,SkirmishDifficultyId.Regular,size,104729,
                        new SkirmishContentManifest{RequiredFeatureIds=definition.RequiredFeatureIds},matrix,out var setup,out var reasons),string.Join(";",reasons));
                    Assert.IsTrue(SkirmishSetupMatrixTable.TryFind(matrix,id,size,out var row));
                    Assert.AreEqual(row.MaterialsEach,setup.MaterialsEach);Assert.AreEqual(row.OilEach,setup.OilEach);Assert.AreEqual(row.UsableFuelEach,setup.UsableFuelEach);
                    Assert.AreEqual(row.EnemyAir,setup.EnemyAir);Assert.AreEqual(row.EnemySupply,setup.EnemySupply);
                    Assert.IsNotNull(setup.EnemyPolicy);Assert.IsTrue(SkirmishGenericAirCombinedEnemyRollout.Supports(setup));assertion(setup);
                }
            });
        }
        [Test] public void AllThirteenCanonicalTuplesAndSizesAttachPolicyWithoutChangingBudgets()
        {
            CollectionAssert.AreEqual(ExactIds,SkirmishGenericAirCombinedEnemyRollout.ReadCatalogIds());
            foreach(string id in ExactIds)WithCompiled(id,setup=>Assert.IsNotNull(setup.EnemyPolicy),true);
        }
        [Test] public void ProtectedThreeKeepIndependentGoldenIdentities()
        {
            var old=new SkirmishEnemyGroundRolloutTests();
            old.ExistingExcludedPackagedSetupsKeepExactPrePolicyIdentity("S003",SkirmishSizeId.Standard,0x0EA5BD22u);
            old.ExistingExcludedPackagedSetupsKeepExactPrePolicyIdentity("S003",SkirmishSizeId.War,0xAD60B105u);
            old.ExistingExcludedPackagedSetupsKeepExactPrePolicyIdentity("S003",SkirmishSizeId.LargeWar,0x7FFA8B7Bu);
            old.ExistingExcludedPackagedSetupsKeepExactPrePolicyIdentity("S004",SkirmishSizeId.Standard,0x2E6B4FBEu);
            old.ExistingExcludedPackagedSetupsKeepExactPrePolicyIdentity("S004",SkirmishSizeId.War,0x9B589E6Cu);
            old.ExistingExcludedPackagedSetupsKeepExactPrePolicyIdentity("S004",SkirmishSizeId.LargeWar,0x9BEAAC24u);
            var catalog=Resources.Load<SkirmishExpansionCatalogConfig>(SkirmishExpansionCatalogConfig.ResourceName);var definition=catalog.ToAuthoredSet().DefinitionS005;
            Assert.IsTrue(SkirmishSetupMatrixTable.TryLoadPackaged(out var matrix,out _));
            var sizes=new[]{SkirmishSizeId.Standard,SkirmishSizeId.War,SkirmishSizeId.LargeWar};
            // Independently captured pre-integration native baseline20261014, seed155926.
            var golden=new uint[]{0xE3D3D033u,0xF16543C6u,0x8F1D10D8u};
            for(int i=0;i<3;i++)
            {
                Assert.IsTrue(SkirmishSetupCompiler.TryCompile(definition,SkirmishDifficultyId.Regular,sizes[i],155926,
                    new SkirmishContentManifest{RequiredFeatureIds=definition.RequiredFeatureIds},matrix,out var setup,out _));
                Assert.AreEqual(golden[i],setup.SetupHash);Assert.IsFalse(SkirmishGenericAirCombinedEnemyRollout.Supports(setup));
            }
        }
        [Test] public void IllegalRoleTupleSourceAndAirCountCannotEnterNewScope()
        {
            WithCompiled("S027",setup=>
            {
                string json=JsonUtility.ToJson(setup);
                foreach(string mutation in new[]{"coded","ground","start","objective","source","fingerprint","role","key","aircount"})
                {
                    var bad=JsonUtility.FromJson<SkirmishResolvedSetup>(json);
                    if(mutation=="coded")bad.CatalogId="S003";
                    if(mutation=="ground")bad.ArmyProfileId=SkirmishArmyProfileId.GroundManeuver;
                    if(mutation=="start")bad.StartPackageId=SkirmishStartPackageId.EstablishedBase;
                    if(mutation=="objective")bad.ObjectiveKind=SkirmishObjectiveKind.FrontlineControl;
                    if(mutation=="source")bad.MeasuredLayoutBound=false;
                    if(mutation=="fingerprint")bad.LayoutBindingFingerprint="";
                    if(mutation=="role"){bad.Forces[0].RoleKind=SkirmishRoleKind.Tank;bad.Forces[0].RoleId=SkirmishRoleIds.Tank;bad.Forces[0].RuntimePrefabKey=SkirmishRoleCatalogConfig.RuntimePrefabKey(SkirmishRoleKind.Tank);}
                    if(mutation=="key")bad.Forces[0].RuntimePrefabKey="Unit_FixtureIllegal";
                    if(mutation=="aircount")bad.EnemyAir++;
                    Assert.IsFalse(SkirmishGenericAirCombinedEnemyRollout.Supports(bad),mutation);
                }
            });
        }
        [Test] public void AirProfileCannotSelectTankReadinessButExistingPaidAirChoicesRemain()
        {
            var facts=new SkirmishEnemyPerceptionBuilder.Snapshot();facts.Visible.Add(new SkirmishEnemyPerceptionBuilder.Contact{Category=SkirmishPopulationCategory.Ground,Role=SkirmishRoleKind.Tank});
            var package=new SkirmishEnemyGroupAllocation.Package();package.Assault.Add(new SkirmishEnemyPerceptionBuilder.Group());
            package.Escort.Add(new SkirmishEnemyPerceptionBuilder.Group());package.Reserve.Add(new SkirmishEnemyPerceptionBuilder.Group());
            Assert.AreEqual(SkirmishRoleKind.None,SkirmishEnemyCommanderService.GroundReadinessGoal(facts,package,SkirmishArmyProfileConfig.ResolveCached(SkirmishArmyProfileId.AirMobile)));
            Assert.AreEqual(SkirmishRoleKind.Tank,SkirmishEnemyCommanderService.GroundReadinessGoal(facts,package,SkirmishArmyProfileConfig.ResolveCached(SkirmishArmyProfileId.CombinedArms)));
            facts.Visible.Add(new SkirmishEnemyPerceptionBuilder.Contact{Category=SkirmishPopulationCategory.Air});
            Assert.AreEqual(SkirmishRoleKind.Fighter,SkirmishEnemyCapabilityService.ChooseDesiredRole(new System.Collections.Generic.List<SkirmishEnemyCapabilityService.OwnActor>(),facts,package,false,600,
                role=>SkirmishArmyProfileConfig.ResolveCached(SkirmishArmyProfileId.AirMobile).Allows(SkirmishRoleIds.ToId(role))));
        }
        [Test] public void ExactInitialHeliNativeProjectionAndPartsRestoreRejectUnpaidImpostors()
        {
            WithCompiled("S028",setup=>
            {
                using var world=new World("Enemy13InitialHeliParts");var em=world.EntityManager;
                var session=em.CreateEntity(typeof(SkirmishExpandedSessionComponent),typeof(SkirmishObjectiveClockComponent));
                var attempt=new FixedString64Bytes("enemy13-native-parts");em.SetComponentData(session,new SkirmishExpandedSessionComponent{SessionId=attempt,Phase=SkirmishSessionPhase.Playing,SpawnComplete=1});
                em.SetComponentData(session,new SkirmishObjectiveClockComponent{Playing=1,ElapsedSeconds=10});
                em.AddComponentObject(session,new SkirmishResolvedSetupRecord{Setup=setup});Assert.IsTrue(SkirmishEnemyPlannerService.EnsureSession(em,session,setup));
                int index=Array.FindIndex(setup.Forces,f=>f.FactionId==2&&f.RoleKind==SkirmishRoleKind.TransportHeli);Assert.GreaterOrEqual(index,0);
                var force=setup.Forces[index];var stable=new FixedString64Bytes(force.RoleId+".2."+index+".0");
                var actor=em.CreateEntity(typeof(SkirmishAttemptOwnedComponent),typeof(Faction),typeof(UnitHealth),typeof(LocalTransform),typeof(UnitAirComponent),typeof(UnitSourcePrefabKey),
                    typeof(UnitTransportCapacity),typeof(SkirmishUnitRoleComponent),typeof(SkirmishArmyGroupMembershipComponent));
                em.SetComponentData(actor,new SkirmishAttemptOwnedComponent{SessionId=attempt,FactionId=2,StableObjectId=stable});
                em.SetComponentData(actor,new Faction{Id=2});em.SetComponentData(actor,new UnitHealth{Current=100,Max=100});
                em.SetComponentData(actor,new UnitSourcePrefabKey{Value=new FixedString64Bytes(force.RuntimePrefabKey)});em.SetComponentData(actor,new UnitTransportCapacity{SoldierCapacity=4});
                em.SetComponentData(actor,new SkirmishUnitRoleComponent{Role=force.RoleKind,Category=SkirmishPopulationCategory.Air});
                em.SetComponentData(actor,new SkirmishArmyGroupMembershipComponent{GroupId=1,Domain=SkirmishPopulationCategory.Air});
                Assert.AreEqual(actor,SkirmishEnemyCapabilityService.ReadOwnActors(em,session).Single().Entity);
                var own=em.GetComponentData<SkirmishAttemptOwnedComponent>(actor);own.StableObjectId="unpaid-produced-heli";em.SetComponentData(actor,own);
                Assert.IsEmpty(SkirmishEnemyCapabilityService.ReadOwnActors(em,session));own.StableObjectId=stable;em.SetComponentData(actor,own);
                em.AddBuffer<SkirmishArmyGroupRecord>(session).Add(new SkirmishArmyGroupRecord{GroupId=1,FactionId=2,Role=force.RoleKind,MemberCount=1,AliveCount=1});
                em.AddBuffer<SkirmishProductionReservation>(session);
                em.SetComponentData(session,new SkirmishEnemyCapabilityStateV1Component{Version=1,NextOperationId=2});
                var operation=new SkirmishEnemyCapabilityOperationV1{OperationId=1,GroupId=1,Role=force.RoleKind,Kind=SkirmishEnemyCapabilityKind.Transport,
                    Phase=SkirmishEnemyCapabilityPhase.Ready,SourceStableId=stable,StartedAt=2,RetryAt=3,NextSurveyAt=8};
                em.GetBuffer<SkirmishEnemyCapabilityOperationV1>(session).Add(operation);
                string Id(Entity entity)=>entity==session?"$session":stable.ToString();
                SkirmishNativeCheckpointSnapshotService.Snapshot Saved()=>new SkirmishNativeCheckpointSnapshotService.Snapshot{SessionId=attempt.ToString(),SessionParts=SkirmishNativeCheckpointParts.Capture(em,session,Id),
                    Actors=new[]{new SkirmishNativeCheckpointSnapshotService.Actor{Id=stable.ToString(),PrefabKey=force.RuntimePrefabKey,Parts=SkirmishNativeCheckpointParts.Capture(em,actor,Id)}},
                    Banks=Array.Empty<SkirmishNativeCheckpointSnapshotService.Bank>(),MapReferences=Array.Empty<SkirmishNativeCheckpointSnapshotService.MapReference>()};
                var saved=Saved();Assert.IsTrue(SkirmishEnemyCapabilityCheckpoint.Validate(saved,setup));
                var restored=em.CreateEntity();SkirmishNativeCheckpointParts.Apply(em,restored,saved.SessionParts,_=>Entity.Null);
                Assert.AreEqual(operation,em.GetBuffer<SkirmishEnemyCapabilityOperationV1>(restored)[0]);
                Assert.AreEqual(100,em.GetComponentData<UnitHealth>(actor).Current,"Parts restore cannot heal or charge a new purchase");
                saved.Actors[0].PrefabKey="Unit_FixtureForeign";Assert.IsFalse(SkirmishEnemyCapabilityCheckpoint.Validate(saved,setup));
                var changed=operation;changed.SourceStableId="unpaid-produced-heli";var changedOperations=em.GetBuffer<SkirmishEnemyCapabilityOperationV1>(session);changedOperations[0]=changed;
                Assert.IsFalse(SkirmishEnemyCapabilityCheckpoint.Validate(Saved(),setup));
            });
        }
        public static void RunFocusedValidation()
        {
            var tests=new SkirmishEnemyAirCombined13Tests();tests.AllThirteenCanonicalTuplesAndSizesAttachPolicyWithoutChangingBudgets();
            tests.ProtectedThreeKeepIndependentGoldenIdentities();tests.IllegalRoleTupleSourceAndAirCountCannotEnterNewScope();
            tests.AirProfileCannotSelectTankReadinessButExistingPaidAirChoicesRemain();tests.ExactInitialHeliNativeProjectionAndPartsRestoreRejectUnpaidImpostors();
            Debug.Log("[SkirmishEnemyAirCombined13Tests] result=Passed controls=5 genericTuples=13 sizes=39 protectedIdentities=9 sourceFitCertified=0 nativePlayerWins=0");
        }
    }
}
#endif
