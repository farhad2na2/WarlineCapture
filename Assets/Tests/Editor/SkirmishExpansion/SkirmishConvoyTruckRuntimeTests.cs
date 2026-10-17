#if UNITY_EDITOR
using System;
using System.IO;
using System.Security.Cryptography;
using Game.Components;
using Game.Configs;
using Game.Runtime;
using Game.Skirmish.Contracts;
using NUnit.Framework;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;

namespace Game.Tests.Editor
{
    public sealed class SkirmishConvoyTruckRuntimeTests
    {
        private sealed class Fixture:IDisposable
        {
            public readonly World World=new World("ConvoyFreshChassisFixture");
            public EntityManager Em=>World.EntityManager;
            public readonly SkirmishConvoyTruckDefinition Definition;
            public Fixture()
            {
                Definition=ScriptableObject.CreateInstance<SkirmishConvoyTruckDefinition>();
                const string prefab="Assets/Game/Prefabs/Vehicles/Unit_Veh_Truck_Tray.prefab";
                Definition.ChassisPrefab=AssetDatabase.LoadAssetAtPath<GameObject>(prefab);
                Definition.ChassisConfig=AssetDatabase.LoadAssetAtPath<UnitGridAuthoringConfig>(SkirmishConvoyTruckDefinition.ExistingConfigPath);
                Definition.ChassisPrefabGuid=AssetDatabase.AssetPathToGUID(prefab);Definition.ChassisPrefabSha256=Sha(prefab);
                Definition.ChassisConfigGuid=AssetDatabase.AssetPathToGUID(SkirmishConvoyTruckDefinition.ExistingConfigPath);
                Definition.ChassisConfigSha256=Sha(SkirmishConvoyTruckDefinition.ExistingConfigPath);
                Assert.IsTrue(Definition.IsValid,"Actual existing chassis/config required; no physical fixture assets created.");
            }
            public Entity Raw(string id="convoy_logic.1.10.0",SkirmishRoleKind kind=SkirmishRoleKind.ConvoyLogic)
            {
                var e=Em.CreateEntity();Em.AddComponent<SkirmishSharedActorTag>(e);
                Em.AddComponentData(e,new SkirmishAttemptOwnedComponent{SessionId="convoy.attempt",StableObjectId=new FixedString64Bytes(id),FactionId=1});
                Em.AddComponentData(e,new Faction{Id=1});
                Em.AddComponentData(e,new SkirmishUnitRoleComponent{Role=kind,Category=SkirmishRoleIds.Category(kind)});
                Em.AddComponentData(e,new UnitSourcePrefabKey{Value=SkirmishConvoyTruckDefinition.ExistingChassisKey});
                Em.AddComponentData(e,new UnitHealth{Current=Definition.ChassisConfig.MaxHealth,Max=Definition.ChassisConfig.MaxHealth});
                Em.AddComponentData(e,new UnitMove{Speed=Definition.ChassisConfig.Speed});
                Em.AddComponentData(e,new UnitFootprint{Size=new int2(3,3)});
                Em.AddComponentData(e,new UnitMovementBehavior{AllowIdleWander=1});
                Em.AddComponentData(e,new UnitCombat{CanAttack=1,AutoEngage=1});
                Em.AddComponent<UnitFuelConsumption>(e);Em.AddComponent<UnitFuelConsumptionState>(e);
                Em.AddComponent<UnitResourceHauler>(e);Em.AddComponent<UnitResourceHaulOrder>(e);Em.AddComponent<UnitAttack>(e);
                Em.AddComponent<UnitTransportCapacity>(e);Em.AddBuffer<UnitTransportPassengerElement>(e);
                Em.AddComponent<CampaignMissionCombatSuppressedTag>(e);Em.AddComponent<Disabled>(e);return e;
            }
            public void Dispose(){World.Dispose();UnityEngine.Object.DestroyImmediate(Definition);}
        }
        private static string Sha(string path)
        {using var hash=SHA256.Create();return BitConverter.ToString(hash.ComputeHash(File.ReadAllBytes(path))).Replace("-","").ToLowerInvariant();}

        [Test] public void FreshObjectiveSpecializationStripsPrivilegesAndKeepsNativeChassisValues()
        {
            using var f=new Fixture();var e=f.Raw();var hp=f.Em.GetComponentData<UnitHealth>(e);var move=f.Em.GetComponentData<UnitMove>(e);
            Assert.IsTrue(SkirmishConvoyTruckRuntime.TrySpecializeFreshInstance(f.Em,e,f.Definition,"convoy.attempt","convoy_logic.1.10.0",1,out var error),error);
            Assert.IsTrue(SkirmishConvoyTruckRuntime.ValidateSpecialized(f.Em,e,f.Definition,out error),error);
            Assert.AreEqual(hp.Max,f.Em.GetComponentData<UnitHealth>(e).Max);Assert.AreEqual(move.Speed,f.Em.GetComponentData<UnitMove>(e).Speed);
            Assert.AreEqual(new int2(3,3),f.Em.GetComponentData<UnitFootprint>(e).Size);
            Assert.IsFalse(f.Em.HasComponent<CampaignMissionCombatSuppressedTag>(e));
        }
        [Test] public void OrdinaryLogisticsCannotBeSpecializedAndRejectedCallDoesNotStripPrivileges()
        {
            using var f=new Fixture();var e=f.Raw(kind:SkirmishRoleKind.LogisticsTruck);
            Assert.IsFalse(SkirmishConvoyTruckRuntime.TrySpecializeFreshInstance(f.Em,e,f.Definition,"convoy.attempt","convoy_logic.1.10.0",1,out _));
            Assert.IsTrue(f.Em.HasComponent<UnitResourceHauler>(e));Assert.IsTrue(f.Em.HasComponent<UnitFuelConsumption>(e));
            Assert.IsFalse(f.Em.HasComponent<SkirmishConvoyObjectiveTruckComponent>(e));
        }
        [Test] public void FreshWorldRecoveryRetainsSuppressionAndRejectsResurrectedPrivileges()
        {
            using var before=new Fixture();var original=before.Raw();
            Assert.IsTrue(SkirmishConvoyTruckRuntime.TrySpecializeFreshInstance(before.Em,original,before.Definition,"convoy.attempt","convoy_logic.1.10.0",1,out _));
            string pinned=before.Em.GetComponentData<SkirmishConvoyObjectiveTruckComponent>(original).DefinitionFingerprint.ToString();
            using var after=new Fixture();var rebuilt=after.Raw();
            Assert.IsTrue(after.Em.HasComponent<UnitResourceHauler>(rebuilt),"Raw shared chassis really has forbidden capability in the fresh-world fixture");
            Assert.IsTrue(SkirmishConvoyTruckRuntime.TrySpecializeFreshInstance(after.Em,rebuilt,after.Definition,"convoy.attempt","convoy_logic.1.10.0",1,out _,true));
            Assert.AreEqual(pinned,after.Em.GetComponentData<SkirmishConvoyObjectiveTruckComponent>(rebuilt).DefinitionFingerprint.ToString());
            Assert.IsTrue(after.Em.HasComponent<Disabled>(rebuilt));Assert.IsTrue(after.Em.HasComponent<CampaignMissionCombatSuppressedTag>(rebuilt));
            Assert.IsTrue(SkirmishConvoyTruckRuntime.ValidateSpecialized(after.Em,rebuilt,after.Definition,out _));
            after.Em.AddComponent<UnitResourceHauler>(rebuilt);
            Assert.IsFalse(SkirmishConvoyTruckRuntime.ValidateSpecialized(after.Em,rebuilt,after.Definition,out _),"Saved parts must not resurrect logistics privileges before unsuppression");
            Assert.IsTrue(after.Em.HasComponent<Disabled>(rebuilt));
        }
        [Test] public void NativeParkingChecksWholeFootprintAndRejectsNearbyFallbackOutsideItsOwnBay()
        {
            using var f=new Fixture();var actor=f.Raw();var grid=f.Em.CreateEntity();
            f.Em.AddComponentData(grid,new GridConfig{Width=100,Height=100,CellSize=1});
            f.Em.AddComponentData(actor,new UnitGrid{Cell=new int2(15,15)});
            var bay=new SkirmishResolvedConvoyBay{ObjectiveRoleId="convoy.1",CenterX=15.5f,CenterZ=15.5f,
                ForwardX=1,AcrossZ=1,HalfForward=1.5f,HalfAcross=1.5f};
            Assert.IsTrue(SkirmishConvoySpawnHooks.TryValidateNativeParkingFootprint(f.Em,actor,bay,out var error),error);
            f.Em.SetComponentData(actor,new UnitGrid{Cell=new int2(16,15)});
            Assert.IsFalse(SkirmishConvoySpawnHooks.TryValidateNativeParkingFootprint(f.Em,actor,bay,out _));
            f.Em.SetComponentData(actor,new UnitGrid{Cell=new int2(15,15)});bay.ForwardX=float.NaN;
            Assert.IsFalse(SkirmishConvoySpawnHooks.TryValidateNativeParkingFootprint(f.Em,actor,bay,out _));
        }
        public static void RunFocusedValidation()
        {
            var t=new SkirmishConvoyTruckRuntimeTests();
            t.FreshObjectiveSpecializationStripsPrivilegesAndKeepsNativeChassisValues();
            t.OrdinaryLogisticsCannotBeSpecializedAndRejectedCallDoesNotStripPrivileges();
            t.FreshWorldRecoveryRetainsSuppressionAndRejectsResurrectedPrivileges();
            t.NativeParkingChecksWholeFootprintAndRejectsNearbyFallbackOutsideItsOwnBay();
            t.ThreeObjectiveActorsDoNotIncreaseAnyFixedCombatOrSupplyBudget();
            Debug.Log("[SkirmishConvoyTruckRuntimeTests] result=Passed tests=5 nativeMissionReadiness=Pending");
        }
        [Test] public void ThreeObjectiveActorsDoNotIncreaseAnyFixedCombatOrSupplyBudget()
        {
            // Invoke the actual compiler tally with a normal opening and the
            // additional objective actors; do not duplicate its accounting logic.
            var forces=new System.Collections.Generic.List<SkirmishResolvedForceEntry>{
                new SkirmishResolvedForceEntry{FactionId=1,RoleKind=SkirmishRoleKind.Rifle,Quantity=12,SupplyCost=12},
                new SkirmishResolvedForceEntry{FactionId=1,RoleKind=SkirmishRoleKind.Tank,Quantity=1,SupplyCost=6}};
            var method=typeof(SkirmishSetupCompiler).GetMethod("Tally",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Static);
            Assert.IsNotNull(method);object[] before={forces,(byte)1,0,0,0,0,0};method.Invoke(null,before);
            for(int i=0;i<3;i++)forces.Add(new SkirmishResolvedForceEntry{FactionId=1,RoleKind=SkirmishRoleKind.ConvoyLogic,Quantity=1,SupplyCost=0});
            object[] after={forces,(byte)1,0,0,0,0,0};method.Invoke(null,after);
            for(int i=2;i<7;i++)Assert.AreEqual(before[i],after[i],"Fixed tally output "+i);
            Assert.AreEqual(12,after[2]);Assert.AreEqual(1,after[3]);Assert.AreEqual(0,after[4]);Assert.AreEqual(13,after[5]);Assert.AreEqual(18,after[6]);
        }
    }
}
#endif
