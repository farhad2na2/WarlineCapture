using System;
using Game.Components;
using Game.Configs;
using Game.Runtime;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

public static class AirCorridorResourceValidation
{
    public static void Run()
    {
        try
        {
            using (ValidationExit.SuppressProcessExit())
            {
                ValidationExit.ClearLastExitCode();
                M02EstablishBaseResourceTests.RunBurstResourceValidation();
                Assert.AreEqual(0, ValidationExit.LastExitCode);
                var m02 = new M02EstablishBaseResourceTests();
                m02.CanonicalProjectionCarriesAttemptResources();
                m02.InitializerAppliesExactResourcesAndZerosLogistics();
                m02.InitializerIsIdempotentWithinAttempt();
                m02.RetryLaunchRearmsInitializerAndRestoresResources();
                m02.DisabledMissionRuntimeLeavesResourcesUntouched();
                m02.AmbiguousPlayerResourceOwnerFailsClosed();
                m02.AttemptInitializerDoesNotReferencePersistence();
                new M02MaterialsEconomyValidation().BarracksAndRiflesUseOnlyMaterialsAndRollbackExactly();
                new BuildingProductionQueueCompositionSystemHelperTests().DefenseBuildingPreflightUsesAuthoredMaterialsPriceAndRecoversAfterRefund();
                FactionConstructionResourceUtilitySystemHelperTests.RunFocusedValidation();
                Assert.AreEqual(0, ValidationExit.LastExitCode);
            }
            var scenario = AssetDatabase.LoadAssetAtPath<ScenarioSetupConfig>(Game.Editor.CH04M01AirCorridorConfigBuilder.ScenarioPath);
            var data = new SerializedObject(scenario);
            Assert.AreEqual(140, data.FindProperty("missionRuntime").FindPropertyRelative("startingMaterials").intValue);
            Assert.AreEqual(0, data.FindProperty("missionRuntime").FindPropertyRelative("startingCredits").intValue);
            var economy = new FactionEconomy { FactionId = 1, Money = 0, MaterialsOnlyConstruction = 2 };
            var materials = new FactionTacticalMaterialsComponent { FactionId = 1, Current = 140, Capacity = 140 };
            Assert.AreEqual(FactionConstructionResourceMutationResult.Applied,
                FactionConstructionResourceUtilitySystemHelper.TrySpend(ref economy, ref materials, 40000, 90));
            Assert.AreEqual(20, materials.Current);
            uint version = materials.Version;
            Assert.AreEqual(FactionConstructionResourceMutationResult.InsufficientMaterials,
                FactionConstructionResourceUtilitySystemHelper.TrySpend(ref economy, ref materials, 22000, 50));
            Assert.AreEqual(20, materials.Current); Assert.AreEqual(version, materials.Version);
            Assert.AreEqual(FactionConstructionResourceMutationResult.Applied,
                FactionConstructionResourceUtilitySystemHelper.TryRollback(ref economy, ref materials, 40000, 90));
            Assert.AreEqual(140, materials.Current); Assert.AreEqual(0, economy.Money);
            Assert.AreEqual(FactionConstructionResourceMutationResult.Applied,
                FactionConstructionResourceUtilitySystemHelper.TrySpend(ref economy, ref materials, 22000, 50));
            Assert.AreEqual(70, materials.Current);
            Debug.Log("[AirCorridorResourceValidation] result=Passed authored=140 Materials barracks=120 shortage=unchanged rollback=140 tower=70 Burst=5 M02-budget=120-30-10 scope=focused-contracts");
            ValidationExit.Passed();
        }
        catch(Exception e) { Debug.LogException(e); ValidationExit.Failed(); }
    }
}
