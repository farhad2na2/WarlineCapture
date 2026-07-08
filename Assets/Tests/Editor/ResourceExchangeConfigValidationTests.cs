using Game.Components;
using Game.Configs;
#if UNITY_INCLUDE_TESTS && UNITY_EDITOR
using NUnit.Framework;

public sealed class ResourceExchangeConfigValidationTests
{
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
}
#endif
