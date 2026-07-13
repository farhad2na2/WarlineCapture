using Game.Components;
using Game.UI.Shell.Contracts.Ecs;
using Game.UI.Shell.Ecs;
#if UNITY_INCLUDE_TESTS && UNITY_EDITOR
using NUnit.Framework;
using Unity.Collections;
using Unity.Entities;

public sealed class UiResourceExchangeReadModelSystemTests
{
    [Test]
    public void WriteReadModel_ProjectsExportCardsDetailWalletAndQueue()
    {
        using World world = new(nameof(WriteReadModel_ProjectsExportCardsDetailWalletAndQueue));
        EntityManager em = world.EntityManager;
        Entity exchange = CreateExchangeData(em);
        DynamicBuffer<ResourceExchangeRecipeComponent> recipes = em.GetBuffer<ResourceExchangeRecipeComponent>(exchange);
        recipes.Add(ExportOilRecipe());
        recipes.Add(ImportFuelRecipe());
        em.GetBuffer<ResourceExchangeQueueComponent>(exchange).Add(new ResourceExchangeQueueComponent
        {
            QueueItemId = 7,
            FactionId = 1,
            RecipeId = new FixedString128Bytes("exchange.export_oil_credits.standard"),
            RouteType = ResourceExchangeRouteType.Export,
            InputResource = ResourceExchangeResourceKind.Oil,
            OutputResource = ResourceExchangeResourceKind.Credits,
            InputAmount = 100,
            ReservedInputAmount = 100,
            OutputAmount = 46,
            State = ResourceExchangeQueueState.InProgress,
            DurationSeconds = 60f,
            RemainingSeconds = 30f
        });
        UiResourceExchangeStateComponent state = new UiResourceExchangeStateComponent
        {
            ActiveTab = UiResourceExchangeTab.Export,
            SelectedRecipeSlot = 0
        };
        UiResourceExchangeDetailComponent detail = default;
        Entity ui = CreateUiData(em);

        UiResourceExchangeReadModelSystem.WriteReadModel(
            Enabled(maxQueueItems: 3),
            Economy(),
            Materials(),
            Wallet(),
            PhysicalResources(),
            new ResourceExchangeSummaryComponent
            {
                ActiveCount = 1,
                CompletedCount = 0,
                Version = 7
            },
            em.GetBuffer<ResourceExchangeRecipeComponent>(exchange),
            em.GetBuffer<ResourceExchangeQueueComponent>(exchange),
            ref state,
            ref detail,
            em.GetBuffer<UiResourceExchangeRecipeCardComponent>(ui),
            em.GetBuffer<UiResourceExchangeQueueRowComponent>(ui));

        DynamicBuffer<UiResourceExchangeRecipeCardComponent> cards =
            em.GetBuffer<UiResourceExchangeRecipeCardComponent>(ui);
        DynamicBuffer<UiResourceExchangeQueueRowComponent> rows =
            em.GetBuffer<UiResourceExchangeQueueRowComponent>(ui);

        Assert.AreEqual(1, state.ExchangeEnabled);
        Assert.AreEqual(1, state.ExportRecipeCount);
        Assert.AreEqual(1, state.ImportRecipeCount);
        Assert.AreEqual("1/3", state.QueueCapacityText.ToString());
        Assert.AreEqual("1000", state.CreditsText.ToString());
        Assert.AreEqual("400", state.OilText.ToString());
        Assert.AreEqual("2", state.RushTicketsText.ToString());
        Assert.AreEqual(1, state.RushAllEnabled);
        Assert.AreEqual(0, state.ClearCompletedEnabled);
        Assert.AreEqual(7u, state.Version);

        Assert.AreEqual(1, cards.Length);
        Assert.AreEqual("Export Oil", cards[0].Title.ToString());
        Assert.AreEqual(1, cards[0].Selected);
        Assert.AreEqual(1, cards[0].Enabled);
        Assert.AreEqual("100 OIL", cards[0].InputText.ToString());
        Assert.AreEqual("46 CREDITS", cards[0].OutputText.ToString());

        Assert.AreEqual("Export Oil", detail.Name.ToString());
        Assert.AreEqual("EXPORT", detail.RouteText.ToString());
        Assert.AreEqual("1 OIL -> 0.47 CREDITS", detail.RateText.ToString());
        Assert.AreEqual("100 OIL", detail.InputCostText.ToString());
        Assert.AreEqual("46 CREDITS", detail.OutputPreviewText.ToString());
        Assert.AreEqual("00:30", detail.DurationText.ToString());
        Assert.AreEqual(1, detail.ConfirmEnabled);
        Assert.AreEqual(0, detail.WarningVisible);

        Assert.AreEqual(1, rows.Length);
        Assert.AreEqual(7, rows[0].QueueItemId);
        Assert.AreEqual(UiResourceExchangeQueueState.InProgress, rows[0].State);
        Assert.AreEqual("Export Oil", rows[0].Name.ToString());
        Assert.AreEqual("00:30", rows[0].TimeText.ToString());
        Assert.AreEqual("50%", rows[0].PercentText.ToString());
        Assert.AreEqual(0.5f, rows[0].Progress01);
        Assert.AreEqual(1, rows[0].RushEnabled);
        Assert.AreEqual(1, rows[0].CancelEnabled);
    }

    [Test]
    public void WriteReadModel_DisablesImportConfirmWhenStorageWouldOverflow()
    {
        using World world = new(nameof(WriteReadModel_DisablesImportConfirmWhenStorageWouldOverflow));
        EntityManager em = world.EntityManager;
        Entity exchange = CreateExchangeData(em);
        em.GetBuffer<ResourceExchangeRecipeComponent>(exchange).Add(ImportFuelRecipe());
        UiResourceExchangeStateComponent state = new UiResourceExchangeStateComponent
        {
            ActiveTab = UiResourceExchangeTab.Import,
            SelectedRecipeSlot = 0
        };
        UiResourceExchangeDetailComponent detail = default;
        Entity ui = CreateUiData(em);

        UiResourceExchangeReadModelSystem.WriteReadModel(
            Enabled(maxQueueItems: 3),
            Economy(),
            Materials(),
            Wallet(),
            PhysicalResources(fuel: 490, fuelCapacity: 500),
            new ResourceExchangeSummaryComponent(),
            em.GetBuffer<ResourceExchangeRecipeComponent>(exchange),
            em.GetBuffer<ResourceExchangeQueueComponent>(exchange),
            ref state,
            ref detail,
            em.GetBuffer<UiResourceExchangeRecipeCardComponent>(ui),
            em.GetBuffer<UiResourceExchangeQueueRowComponent>(ui));

        DynamicBuffer<UiResourceExchangeRecipeCardComponent> cards =
            em.GetBuffer<UiResourceExchangeRecipeCardComponent>(ui);

        Assert.AreEqual(1, cards.Length);
        Assert.AreEqual(1, cards[0].Enabled);
        Assert.AreEqual("Import Fuel", detail.Name.ToString());
        Assert.AreEqual("50 FUEL", detail.OutputPreviewText.ToString());
        Assert.AreEqual(0, detail.ConfirmEnabled);
        Assert.AreEqual(1, detail.WarningVisible);
        Assert.AreEqual("Storage full", detail.InstructionText.ToString());
    }

    [Test]
    public void WriteReadModel_UsesEmptyDetailWhenActiveTabHasNoRoutes()
    {
        using World world = new(nameof(WriteReadModel_UsesEmptyDetailWhenActiveTabHasNoRoutes));
        EntityManager em = world.EntityManager;
        Entity exchange = CreateExchangeData(em);
        em.GetBuffer<ResourceExchangeRecipeComponent>(exchange).Add(ExportOilRecipe());
        UiResourceExchangeStateComponent state = new UiResourceExchangeStateComponent
        {
            ActiveTab = UiResourceExchangeTab.Import,
            SelectedRecipeSlot = 0
        };
        UiResourceExchangeDetailComponent detail = default;
        Entity ui = CreateUiData(em);

        UiResourceExchangeReadModelSystem.WriteReadModel(
            Enabled(maxQueueItems: 3),
            Economy(),
            Materials(),
            Wallet(),
            PhysicalResources(),
            new ResourceExchangeSummaryComponent(),
            em.GetBuffer<ResourceExchangeRecipeComponent>(exchange),
            em.GetBuffer<ResourceExchangeQueueComponent>(exchange),
            ref state,
            ref detail,
            em.GetBuffer<UiResourceExchangeRecipeCardComponent>(ui),
            em.GetBuffer<UiResourceExchangeQueueRowComponent>(ui));

        Assert.AreEqual(1, state.ExportRecipeCount);
        Assert.AreEqual(0, state.ImportRecipeCount);
        Assert.AreEqual(0, em.GetBuffer<UiResourceExchangeRecipeCardComponent>(ui).Length);
        Assert.AreEqual("NO ROUTES", detail.Name.ToString());
        Assert.AreEqual("IMPORT", detail.RouteText.ToString());
        Assert.AreEqual(0, detail.ConfirmEnabled);
        Assert.AreEqual(1, detail.WarningVisible);
    }

    private static Entity CreateExchangeData(EntityManager em)
    {
        Entity entity = em.CreateEntity();
        em.AddBuffer<ResourceExchangeRecipeComponent>(entity);
        em.AddBuffer<ResourceExchangeQueueComponent>(entity);
        return entity;
    }

    private static Entity CreateUiData(EntityManager em)
    {
        Entity entity = em.CreateEntity();
        em.AddBuffer<UiResourceExchangeRecipeCardComponent>(entity);
        em.AddBuffer<UiResourceExchangeQueueRowComponent>(entity);
        return entity;
    }

    private static ResourceExchangeEnabledComponent Enabled(int maxQueueItems)
    {
        return new ResourceExchangeEnabledComponent
        {
            Enabled = 1,
            FactionId = 1,
            AllowRush = 1,
            MaxQueueItems = maxQueueItems,
            ScenarioTag = new FixedString64Bytes("mission.active")
        };
    }

    private static ResourceExchangeWalletComponent Wallet()
    {
        return new ResourceExchangeWalletComponent
        {
            FactionId = 1,
            RushTickets = 2
        };
    }

    private static BuildingRuntimeFactionUsableFuelSummary PhysicalResources(
        float fuel = 20f,
        int fuelCapacity = 500)
    {
        return new BuildingRuntimeFactionUsableFuelSummary
        {
            FactionId = 1,
            StoredOilBarrels = 400f,
            StoredFuelBarrels = fuel,
            OilStorageCapacity = 800,
            FuelStorageCapacity = fuelCapacity
        };
    }

    private static FactionEconomy Economy()
    {
        return new FactionEconomy { FactionId = 1, Money = 1000 };
    }

    private static FactionTacticalMaterialsComponent Materials()
    {
        return new FactionTacticalMaterialsComponent { FactionId = 1, Current = 50, Capacity = 200 };
    }

    private static ResourceExchangeRecipeComponent ExportOilRecipe()
    {
        return new ResourceExchangeRecipeComponent
        {
            RecipeId = new FixedString128Bytes("exchange.export_oil_credits.standard"),
            DisplayName = new FixedString128Bytes("Export Oil"),
            RouteType = ResourceExchangeRouteType.Export,
            InputResource = ResourceExchangeResourceKind.Oil,
            OutputResource = ResourceExchangeResourceKind.Credits,
            InputAmountMin = 100,
            InputAmountMax = 1000,
            InputStep = 100,
            OutputPerInput = 0.55f,
            FeePercent = 0.15f,
            DurationSecondsBase = 30f,
            DurationSecondsPerStep = 0f,
            RushTicketSecondsPerTicket = 10,
            MaxRushTickets = 3,
            Enabled = 1
        };
    }

    private static ResourceExchangeRecipeComponent ImportFuelRecipe()
    {
        return new ResourceExchangeRecipeComponent
        {
            RecipeId = new FixedString128Bytes("exchange.import_fuel.standard"),
            DisplayName = new FixedString128Bytes("Import Fuel"),
            RouteType = ResourceExchangeRouteType.Import,
            InputResource = ResourceExchangeResourceKind.Credits,
            OutputResource = ResourceExchangeResourceKind.Fuel,
            InputAmountMin = 100,
            InputAmountMax = 1000,
            InputStep = 100,
            OutputPerInput = 0.5f,
            FeePercent = 0f,
            DurationSecondsBase = 30f,
            DurationSecondsPerStep = 0f,
            RushTicketSecondsPerTicket = 10,
            MaxRushTickets = 3,
            RequiresStorage = 1,
            Enabled = 1
        };
    }
}
#endif
