using System;
using Game.Components;
using Game.Configs;
using Game.Runtime;
using NUnit.Framework;
using UnityEngine;

public sealed class M02MaterialsEconomyValidation
{
    [Test]
    public void BarracksAndRiflesUseOnlyMaterialsAndRollbackExactly()
    {
        var economy = new FactionEconomy { FactionId = 1, Money = 0, MaterialsOnlyConstruction = 1 };
        var materials = new FactionTacticalMaterialsComponent { FactionId = 1, Current = 120, Capacity = 120 };
        Assert.AreEqual(FactionConstructionResourceMutationResult.Applied,
            FactionConstructionResourceUtilitySystemHelper.TrySpend(ref economy, ref materials, 40000, 90));
        Assert.AreEqual(30, materials.Current);
        Assert.AreEqual(FactionConstructionResourceMutationResult.Applied,
            FactionConstructionResourceUtilitySystemHelper.TrySpend(ref economy, ref materials, 10000, 20));
        Assert.AreEqual(10, materials.Current);
        Assert.AreEqual(0, economy.Money);
        Assert.AreEqual(FactionConstructionResourceMutationResult.InsufficientMaterials,
            FactionConstructionResourceUtilitySystemHelper.TrySpend(ref economy, ref materials, 10000, 20));
        Assert.AreEqual(10, materials.Current);
        Assert.AreEqual(FactionConstructionResourceMutationResult.Applied,
            FactionConstructionResourceUtilitySystemHelper.TryRollback(ref economy, ref materials, 10000, 20));
        Assert.AreEqual(30, materials.Current);
        Assert.AreEqual(FactionConstructionResourceMutationResult.InvalidCost,
            FactionConstructionResourceUtilitySystemHelper.TrySpend(ref economy, ref materials, 10000, 0));
        Assert.AreEqual(30, materials.Current, "An unmigrated Credits-only action must not become free.");
        Assert.AreEqual(0, economy.Money, "Refunding a cancelled rifle order must not mint Credits.");
    }

    public static void RunPlayAcceptance()
    {
        try
        {
        using (ValidationExit.SuppressProcessExit())
        {
            ValidationExit.ClearLastExitCode();
            Run();
            Assert.AreEqual(0, ValidationExit.LastExitCode);
        }
        new ProductionSourceGrowthArchitectureTests().NewSystemHelperPathsRequireApprovedException();
        new ProductionSourceGrowthArchitectureTests().ApprovedExceptionsUseExactTrackedAuthorization();
        new M02MaterialsLessonLayoutTests().EveryM2InstructionFitsEnglishAndFarsiAtBothTextSizes();
        M02EstablishBaseNarrativeTests.TutorialVoiceAssetsMatchEveryDisplayedInstruction();
        new M02MaterialsLessonLayoutTests().TutorialStaysAboveInheritedPopupCanvasAndRestoresItsOrder();
        Debug.Log("[M02FinalPreflight] result=Passed layouts=28 voices=14 growthChecks=2");
        Game.Editor.M02BuildingPlacementEditorProbe.RunRifleSingleClickValidation();
        }
        catch (Exception error) { Debug.LogException(error); UnityEditor.EditorApplication.Exit(1); }
    }

    public static void RunPresentationAndArchitecture()
    {
        try
        {
            new M02MaterialsLessonLayoutTests().EveryM2InstructionFitsEnglishAndFarsiAtBothTextSizes();
            M02EstablishBaseNarrativeTests.TutorialVoiceAssetsMatchEveryDisplayedInstruction();
            using (ValidationExit.SuppressProcessExit())
            {
                ValidationExit.ClearLastExitCode();
                M02EstablishBaseBarracksProductionTests.RunFocusedValidation();
                Assert.AreEqual(0, ValidationExit.LastExitCode);
                MissionReadinessArchitectureValidation.Run();
                Assert.AreEqual(0, ValidationExit.LastExitCode);
            }
            Debug.Log("[M02MaterialsPresentationArchitecture] result=Passed voices=14");
            ValidationExit.Passed();
        }
        catch (Exception error)
        {
            Debug.LogException(error);
            ValidationExit.Failed();
        }
    }

    public static void Run()
    {
        try
        {
            Game.Editor.V3UiLocalizationCatalogBuilder.ApplyConfiguredUiStrings();
            new M02MaterialsEconomyValidation().BarracksAndRiflesUseOnlyMaterialsAndRollbackExactly();
            Action[] suites = { M02EstablishBaseResourceTests.RunFocusedValidation,
                M01FirstContactHudRestrictionTests.RunFocusedValidation,
                FactionConstructionResourceUtilitySystemHelperTests.RunFocusedValidation };
            foreach (var suite in suites)
            {
                using (ValidationExit.SuppressProcessExit())
                {
                    ValidationExit.ClearLastExitCode();
                    suite();
                    Assert.AreEqual(0, ValidationExit.LastExitCode, suite.Method.Name);
                }
            }
            Debug.Log("[M02MaterialsEconomyValidation] result=Passed");
            ValidationExit.Passed();
        }
        catch (Exception error)
        {
            Debug.LogException(error);
            Debug.Log("[M02MaterialsEconomyValidation] result=Failed");
            ValidationExit.Failed();
        }
    }
}
