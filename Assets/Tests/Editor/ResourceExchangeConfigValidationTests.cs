using System;
using Game.Components;
using Game.Configs;
using Game.Runtime;
#if UNITY_INCLUDE_TESTS && UNITY_EDITOR
using NUnit.Framework;
using UnityEngine;

public sealed class ResourceExchangeConfigValidationTests
{
    public static void RunFocusedValidation()
    {
        int passed = 0;
        try
        {
            RunValidationStep(
                nameof(ValidateRecipe_AcceptsAuthoredExportAndImportRoutes),
                test => test.ValidateRecipe_AcceptsAuthoredExportAndImportRoutes(),
                ref passed);
            RunValidationStep(
                nameof(ValidateRecipe_RejectsInvalidAmountsRatesDurationAndRushRules),
                test => test.ValidateRecipe_RejectsInvalidAmountsRatesDurationAndRushRules(),
                ref passed);
            RunValidationStep(
                nameof(ValidateRecipe_RejectsUnsafeDataSanityCaps),
                test => test.ValidateRecipe_RejectsUnsafeDataSanityCaps(),
                ref passed);
            RunValidationStep(
                nameof(ValidateRecipeSet_RejectsRoundTripFarmingRisk),
                test => test.ValidateRecipeSet_RejectsRoundTripFarmingRisk(),
                ref passed);
            RunValidationStep(
                nameof(ValidateRecipeAndScenarioGateSet_AcceptsExplicitMissionAndSkirmishGates),
                test => test.ValidateRecipeAndScenarioGateSet_AcceptsExplicitMissionAndSkirmishGates(),
                ref passed);
            RunValidationStep(
                nameof(ValidateScenarioGateSet_RejectsUnsafeFtueGateAuthoring),
                test => test.ValidateScenarioGateSet_RejectsUnsafeFtueGateAuthoring(),
                ref passed);

            Debug.Log($"[ResourceExchangeConfigValidation] result=Passed tests={passed}");
            ValidationExit.Exit(0);
        }
        catch (Exception exception)
        {
            Debug.LogError($"[ResourceExchangeConfigValidation] result=Failed passed={passed}\n{exception}");
            ValidationExit.Exit(1);
        }
    }

    [Test]
    public void ResourceExchangeEnums_KeepStablePhaseOneValues()
    {
        Assert.AreEqual(0, (int)ResourceExchangeRouteType.Export);
        Assert.AreEqual(1, (int)ResourceExchangeRouteType.Import);
        Assert.AreEqual(4, (int)ResourceExchangeQueueState.Completed);
        Assert.AreEqual(4, (int)ResourceExchangeResourceKind.RushTickets);
    }

    [Test]
    public void ValidateRecipe_AcceptsAuthoredExportAndImportRoutes()
    {
        Assert.AreEqual(
            ResourceExchangeReason.None,
            ResourceExchangeRecipeConfigValidator.ValidateRecipe(
                new ResourceExchangeRecipeConfigEntry(
                    "exchange.export_oil_credits.standard",
                    ResourceExchangeRouteType.Export,
                    ResourceExchangeResourceKind.Oil,
                    ResourceExchangeResourceKind.Credits)));

        Assert.AreEqual(
            ResourceExchangeReason.None,
            ResourceExchangeRecipeConfigValidator.ValidateRecipe(
                new ResourceExchangeRecipeConfigEntry(
                    "exchange.import_fuel_credits.standard",
                    ResourceExchangeRouteType.Import,
                    ResourceExchangeResourceKind.Credits,
                    ResourceExchangeResourceKind.Fuel)));
    }

    [Test]
    public void ValidateRecipeSet_RejectsDuplicateRecipeIds()
    {
        var recipes = new[]
        {
            new ResourceExchangeRecipeConfigEntry(
                "exchange.export_oil_credits.standard",
                ResourceExchangeRouteType.Export,
                ResourceExchangeResourceKind.Oil,
                ResourceExchangeResourceKind.Credits),
            new ResourceExchangeRecipeConfigEntry(
                "exchange.export_oil_credits.standard",
                ResourceExchangeRouteType.Export,
                ResourceExchangeResourceKind.Materials,
                ResourceExchangeResourceKind.Credits)
        };

        Assert.AreEqual(
            ResourceExchangeReason.DuplicateRecipeId,
            ResourceExchangeRecipeConfigValidator.ValidateRecipeSet(recipes));
    }

    [Test]
    public void ValidateRecipe_RejectsInvalidAmountsRatesDurationAndRushRules()
    {
        Assert.AreEqual(
            ResourceExchangeReason.MissingRecipeId,
            ResourceExchangeRecipeConfigValidator.ValidateRecipe(
                string.Empty,
                ResourceExchangeRouteType.Export,
                ResourceExchangeResourceKind.Oil,
                ResourceExchangeResourceKind.Credits,
                100,
                1000,
                100,
                0.55f,
                0.15f,
                30f,
                2f,
                30,
                3));

        Assert.AreEqual(
            ResourceExchangeReason.InvalidRecipe,
            ResourceExchangeRecipeConfigValidator.ValidateRecipe(
                "exchange.invalid.amount",
                ResourceExchangeRouteType.Export,
                ResourceExchangeResourceKind.Oil,
                ResourceExchangeResourceKind.Credits,
                1000,
                100,
                100,
                0.55f,
                0.15f,
                30f,
                2f,
                30,
                3));

        Assert.AreEqual(
            ResourceExchangeReason.InputStepInvalid,
            ResourceExchangeRecipeConfigValidator.ValidateRecipe(
                "exchange.invalid.step",
                ResourceExchangeRouteType.Export,
                ResourceExchangeResourceKind.Oil,
                ResourceExchangeResourceKind.Credits,
                100,
                950,
                200,
                0.55f,
                0.15f,
                30f,
                2f,
                30,
                3));

        Assert.AreEqual(
            ResourceExchangeReason.InvalidRate,
            ResourceExchangeRecipeConfigValidator.ValidateRecipe(
                "exchange.invalid.rate",
                ResourceExchangeRouteType.Export,
                ResourceExchangeResourceKind.Oil,
                ResourceExchangeResourceKind.Credits,
                100,
                1000,
                100,
                0f,
                0.15f,
                30f,
                2f,
                30,
                3));

        Assert.AreEqual(
            ResourceExchangeReason.InvalidDuration,
            ResourceExchangeRecipeConfigValidator.ValidateRecipe(
                "exchange.invalid.duration",
                ResourceExchangeRouteType.Export,
                ResourceExchangeResourceKind.Oil,
                ResourceExchangeResourceKind.Credits,
                100,
                1000,
                100,
                0.55f,
                0.15f,
                -1f,
                2f,
                30,
                3));

        Assert.AreEqual(
            ResourceExchangeReason.InvalidRushRule,
            ResourceExchangeRecipeConfigValidator.ValidateRecipe(
                "exchange.invalid.rush",
                ResourceExchangeRouteType.Export,
                ResourceExchangeResourceKind.Oil,
                ResourceExchangeResourceKind.Credits,
                100,
                1000,
                100,
                0.55f,
                0.15f,
                30f,
                2f,
                0,
                3));
    }

    [Test]
    public void ValidateRecipe_RejectsUnsafeDataSanityCaps()
    {
        Assert.AreEqual(
            ResourceExchangeReason.InvalidRecipe,
            ResourceExchangeRecipeConfigValidator.ValidateRecipe(
                "exchange.invalid.amount_cap",
                ResourceExchangeRouteType.Export,
                ResourceExchangeResourceKind.Oil,
                ResourceExchangeResourceKind.Credits,
                100,
                ResourceExchangeRecipeConfigValidator.MaximumInputAmountPerExchange + 100,
                100,
                0.55f,
                0.15f,
                30f,
                2f,
                30,
                3));

        Assert.AreEqual(
            ResourceExchangeReason.InvalidRate,
            ResourceExchangeRecipeConfigValidator.ValidateRecipe(
                "exchange.invalid.rate_cap",
                ResourceExchangeRouteType.Export,
                ResourceExchangeResourceKind.Oil,
                ResourceExchangeResourceKind.Credits,
                100,
                1000,
                100,
                ResourceExchangeRecipeConfigValidator.MaximumOutputPerInput + 0.01f,
                0.15f,
                30f,
                2f,
                30,
                3));

        Assert.AreEqual(
            ResourceExchangeReason.InvalidRate,
            ResourceExchangeRecipeConfigValidator.ValidateRecipe(
                "exchange.invalid.fee_nan",
                ResourceExchangeRouteType.Export,
                ResourceExchangeResourceKind.Oil,
                ResourceExchangeResourceKind.Credits,
                100,
                1000,
                100,
                0.55f,
                float.NaN,
                30f,
                2f,
                30,
                3));

        Assert.AreEqual(
            ResourceExchangeReason.InvalidDuration,
            ResourceExchangeRecipeConfigValidator.ValidateRecipe(
                "exchange.invalid.instant",
                ResourceExchangeRouteType.Export,
                ResourceExchangeResourceKind.Oil,
                ResourceExchangeResourceKind.Credits,
                100,
                1000,
                100,
                0.55f,
                0.15f,
                ResourceExchangeRecipeConfigValidator.MinimumBaseDurationSeconds - 0.1f,
                2f,
                30,
                3));

        Assert.AreEqual(
            ResourceExchangeReason.InvalidRushRule,
            ResourceExchangeRecipeConfigValidator.ValidateRecipe(
                "exchange.invalid.rush_cap",
                ResourceExchangeRouteType.Export,
                ResourceExchangeResourceKind.Oil,
                ResourceExchangeResourceKind.Credits,
                100,
                1000,
                100,
                0.55f,
                0.15f,
                30f,
                2f,
                30,
                ResourceExchangeRecipeConfigValidator.MaximumRushTicketsPerJob + 1));
    }

    [Test]
    public void ValidateRecipeSet_RejectsRoundTripFarmingRisk()
    {
        var safeRecipes = new[]
        {
            new ResourceExchangeRecipeConfigEntry(
                "exchange.export_fuel_credits.safe",
                ResourceExchangeRouteType.Export,
                ResourceExchangeResourceKind.Fuel,
                ResourceExchangeResourceKind.Credits,
                outputPerInput: 0.55f,
                feePercent: 0.15f),
            new ResourceExchangeRecipeConfigEntry(
                "exchange.import_fuel_credits.safe",
                ResourceExchangeRouteType.Import,
                ResourceExchangeResourceKind.Credits,
                ResourceExchangeResourceKind.Fuel,
                outputPerInput: 0.5f,
                feePercent: 0.15f)
        };

        Assert.AreEqual(
            ResourceExchangeReason.None,
            ResourceExchangeRecipeConfigValidator.ValidateRecipeSet(safeRecipes));

        var farmingRiskRecipes = new[]
        {
            new ResourceExchangeRecipeConfigEntry(
                "exchange.export_fuel_credits.farming",
                ResourceExchangeRouteType.Export,
                ResourceExchangeResourceKind.Fuel,
                ResourceExchangeResourceKind.Credits,
                outputPerInput: 1f,
                feePercent: 0.05f),
            new ResourceExchangeRecipeConfigEntry(
                "exchange.import_fuel_credits.farming",
                ResourceExchangeRouteType.Import,
                ResourceExchangeResourceKind.Credits,
                ResourceExchangeResourceKind.Fuel,
                outputPerInput: 1f,
                feePercent: 0.05f)
        };

        Assert.AreEqual(
            ResourceExchangeReason.InvalidRate,
            ResourceExchangeRecipeConfigValidator.ValidateRecipeSet(farmingRiskRecipes));
    }

    [Test]
    public void ValidateRecipeAndScenarioGateSet_AcceptsExplicitMissionAndSkirmishGates()
    {
        var recipes = new[]
        {
            new ResourceExchangeRecipeConfigEntry(
                "exchange.export_oil_credits.mission_active",
                ResourceExchangeRouteType.Export,
                ResourceExchangeResourceKind.Oil,
                ResourceExchangeResourceKind.Credits,
                missionTag: "mission.active"),
            new ResourceExchangeRecipeConfigEntry(
                "exchange.import_fuel_credits.skirmish_quick",
                ResourceExchangeRouteType.Import,
                ResourceExchangeResourceKind.Credits,
                ResourceExchangeResourceKind.Fuel,
                missionTag: "custom.skirmish.quick")
        };

        var gates = new[]
        {
            new ResourceExchangeScenarioGateConfigEntry(
                "chapter.01.ftue",
                false,
                maxQueueItems: 0,
                allowRush: false,
                allowWorldPresentation: false,
                disabledReason: ResourceExchangeReason.ExchangeUnavailable),
            new ResourceExchangeScenarioGateConfigEntry(
                "mission.active",
                true,
                maxQueueItems: 2),
            new ResourceExchangeScenarioGateConfigEntry(
                "custom.skirmish.quick",
                true,
                maxQueueItems: 3)
        };

        Assert.AreEqual(
            ResourceExchangeReason.None,
            ResourceExchangeRecipeConfigValidator.ValidateRecipeAndScenarioGateSet(recipes, gates));
    }

    [Test]
    public void ValidateScenarioGateSet_RejectsUnsafeFtueGateAuthoring()
    {
        var validGates = new[]
        {
            new ResourceExchangeScenarioGateConfigEntry("mission.active", true, maxQueueItems: 2)
        };

        var blankRecipeGate = new[]
        {
            new ResourceExchangeRecipeConfigEntry(
                "exchange.export_oil_credits.blank_gate",
                ResourceExchangeRouteType.Export,
                ResourceExchangeResourceKind.Oil,
                ResourceExchangeResourceKind.Credits)
        };

        Assert.AreEqual(
            ResourceExchangeReason.InvalidScenarioGate,
            ResourceExchangeRecipeConfigValidator.ValidateScenarioGateSet(validGates, blankRecipeGate));

        var unknownRecipeGate = new[]
        {
            new ResourceExchangeRecipeConfigEntry(
                "exchange.export_oil_credits.unknown_gate",
                ResourceExchangeRouteType.Export,
                ResourceExchangeResourceKind.Oil,
                ResourceExchangeResourceKind.Credits,
                missionTag: "mission.late")
        };

        Assert.AreEqual(
            ResourceExchangeReason.InvalidScenarioGate,
            ResourceExchangeRecipeConfigValidator.ValidateScenarioGateSet(validGates, unknownRecipeGate));

        Assert.AreEqual(
            ResourceExchangeReason.InvalidScenarioGate,
            ResourceExchangeRecipeConfigValidator.ValidateScenarioGateSet(
                new[] { new ResourceExchangeScenarioGateConfigEntry(string.Empty, true, maxQueueItems: 2) },
                unknownRecipeGate));

        Assert.AreEqual(
            ResourceExchangeReason.InvalidScenarioGate,
            ResourceExchangeRecipeConfigValidator.ValidateScenarioGateSet(
                new[] { new ResourceExchangeScenarioGateConfigEntry("sandbox.debug", true, maxQueueItems: 2) },
                unknownRecipeGate));

        Assert.AreEqual(
            ResourceExchangeReason.InvalidScenarioGate,
            ResourceExchangeRecipeConfigValidator.ValidateScenarioGateSet(
                new[]
                {
                    new ResourceExchangeScenarioGateConfigEntry("mission.active", true, maxQueueItems: 2),
                    new ResourceExchangeScenarioGateConfigEntry("mission.active", true, maxQueueItems: 2)
                },
                unknownRecipeGate));

        Assert.AreEqual(
            ResourceExchangeReason.InvalidScenarioGate,
            ResourceExchangeRecipeConfigValidator.ValidateScenarioGateSet(
                new[] { new ResourceExchangeScenarioGateConfigEntry("mission.active", true, maxQueueItems: 0) },
                unknownRecipeGate));

        Assert.AreEqual(
            ResourceExchangeReason.InvalidScenarioGate,
            ResourceExchangeRecipeConfigValidator.ValidateScenarioGateSet(
                new[]
                {
                    new ResourceExchangeScenarioGateConfigEntry(
                        "chapter.01.ftue",
                        false,
                        maxQueueItems: 0,
                        disabledReason: ResourceExchangeReason.None)
                },
                unknownRecipeGate));
    }

    [Test]
    public void ValidateRecipe_RejectsDisallowedConversionRoutes()
    {
        Assert.AreEqual(
            ResourceExchangeReason.InvalidResource,
            ResourceExchangeRecipeConfigValidator.ValidateRecipe(
                new ResourceExchangeRecipeConfigEntry(
                    "exchange.import_oil_credits.invalid",
                    ResourceExchangeRouteType.Import,
                    ResourceExchangeResourceKind.Credits,
                    ResourceExchangeResourceKind.Oil)));

        Assert.AreEqual(
            ResourceExchangeReason.InvalidResource,
            ResourceExchangeRecipeConfigValidator.ValidateRecipe(
                new ResourceExchangeRecipeConfigEntry(
                    "exchange.rush_to_credits.invalid",
                    ResourceExchangeRouteType.Export,
                    ResourceExchangeResourceKind.RushTickets,
                    ResourceExchangeResourceKind.Credits)));
    }

    private static void RunValidationStep(
        string name,
        Action<ResourceExchangeConfigValidationTests> action,
        ref int passed)
    {
        var test = new ResourceExchangeConfigValidationTests();
        try
        {
            action(test);
            passed++;
            Debug.Log($"[ResourceExchangeConfigValidation] passed {name}");
        }
        catch (Exception exception)
        {
            Debug.LogError($"[ResourceExchangeConfigValidation] failed {name}\n{exception}");
            throw;
        }
    }
}
#endif
