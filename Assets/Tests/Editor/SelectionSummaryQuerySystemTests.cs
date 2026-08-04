using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.UI;
using Game.UI.Contracts;
using Game.Components;
using Game.UI.Runtime;
using Game.Runtime;

public sealed class SelectionSummaryQuerySystemTests
{
    private World _world;
    private World _previousWorld;
    private readonly List<GameObject> _createdObjects = new();

    public static void RunFocusedValidation()
    {
        try
        {
            RunCase(test => test.SoldierMultiSelectionUsesInfantryCopyAndAggregateHealth());
            RunCase(test => test.MixedSoldierAndVehicleUsesMixedCopyAndAggregateHealth());
            RunCase(test => test.MixedSoldierAndAircraftUsesAirInfantryPortraitKind());
            RunCase(test => test.MixedSoldierVehicleAndAircraftUsesCombinedArmsPortraitKind());
            RunCase(test => test.MixedVehicleAndAircraftUsesAirVehiclePortraitKind());
            RunCase(test => test.MultiVehicleSelectionUsesVehiclePortraitKind());
            RunCase(test => test.MultiTransportSelectionUsesTransportPortraitKind());
            RunCase(test => test.MixedGroundVehicleAndTransportUsesVehiclePortraitKind());
            RunCase(test => test.GroundTransportAndAirTransportUsesAirVehiclePortraitKind());
            RunCase(test => test.MixedSelectedOrdersDisplaysMixedOrders());
            RunCase(test => test.FocusedTransportPlaneSelectionPanelUsesResolvedPortrait());
            RunCase(test => test.FocusedResourceHaulerSelectionPanelUsesEcsCargoStorage());
            RunCase(test => test.FocusedResourceHaulerSelectionPanelShowsTypedLogisticsStatus());
            RunCase(test => test.SelectedBuildingSelectionPanelShowsOilFuelStorageChips());
            RunCase(test => test.SelectedBuildingSelectionPanelReplacesStaleOilFuelStorageValues());
            RunCase(test => test.SelectedBuildingSelectionPanelOverridesStaleFocusedEntity());
            RunCase(test => test.SelectedBuildingResourceStoragePanelSkipsApplyUntilVersionChanges());
            RunCase(test => test.SelectedBuildingRefineryStoragePanelReportsConversionStatus());
            RunCase(test => test.SelectedBuildingFabricationPanelPrefersFabricationReadModel());
            RunCase(test => test.SelectedBuildingFabricationPanelSkipsApplyUntilVersionChanges());
            RunCase(test => test.SelectedBuildingFabricationPanelShowsTypedStatusCopy());
            Debug.Log("[SelectionSummaryFocusedValidation] result=Passed tests=21");
        }
        catch (System.Exception ex)
        {
            Debug.LogException(ex);
            Debug.LogError("[SelectionSummaryFocusedValidation] result=Failed");
            throw;
        }
    }

    private static void RunCase(System.Action<SelectionSummaryQuerySystemTests> testCase)
    {
        var tests = new SelectionSummaryQuerySystemTests();
        try
        {
            tests.SetUp();
            testCase(tests);
        }
        finally
        {
            tests.TearDown();
        }
    }

    [SetUp]
    public void SetUp()
    {
        _previousWorld = World.DefaultGameObjectInjectionWorld;
        _world = new World("SelectionSummaryQuerySystemTests");
        World.DefaultGameObjectInjectionWorld = _world;
    }

    [TearDown]
    public void TearDown()
    {
        for (int i = 0; i < _createdObjects.Count; i++)
            Object.DestroyImmediate(_createdObjects[i]);
        _createdObjects.Clear();

        if (_world != null && _world.IsCreated)
            _world.Dispose();

        World.DefaultGameObjectInjectionWorld = _previousWorld;
    }

    [Test]
    public void SoldierMultiSelectionUsesInfantryCopyAndAggregateHealth()
    {
        EntityManager em = _world.EntityManager;
        Entity first = CreatePlayerUnit(em, "Rifle Squad", new int2(1, 1), 80);
        Entity second = CreatePlayerUnit(em, "Security Squad", new int2(2, 1), 60);
        em.AddComponent<SelectedUnitTag>(first);
        em.AddComponent<SelectedUnitTag>(second);

        SelectionHudFeedbackUiSystemHelper.SelectedSummary summary = SelectionHudFeedbackUiSystemHelper.BuildSelectedSummary(
            em,
            new SelectionUiReadModelLookup(),
            false);

        Assert.AreEqual(2, summary.UnitCount);
        Assert.AreEqual(2, summary.SoldierCount);
        Assert.AreEqual("2 SOLDIERS", summary.Title);
        Assert.AreEqual("Infantry Squad", summary.Subtitle);
        Assert.AreEqual("140/200", summary.HealthText);
        Assert.AreEqual(0.7f, summary.Health01, 0.001f);
        Assert.AreEqual(SelectionSummaryPortraitKind.Soldiers, summary.PortraitKind);
    }

    [Test]
    public void MixedSoldierAndVehicleUsesMixedCopyAndAggregateHealth()
    {
        EntityManager em = _world.EntityManager;
        Entity soldier = CreatePlayerUnit(em, "Rifle Squad", new int2(1, 1), 50);
        Entity vehicle = CreatePlayerUnit(em, "Recon Vehicle", new int2(2, 1), 75);
        em.AddComponent<SelectedUnitTag>(soldier);
        em.AddComponent<SelectedUnitTag>(vehicle);
        em.AddComponentData(vehicle, new UnitMovementBehavior { UsesVehicleMotion = 1 });

        SelectionHudFeedbackUiSystemHelper.SelectedSummary summary = SelectionHudFeedbackUiSystemHelper.BuildSelectedSummary(
            em,
            new SelectionUiReadModelLookup(),
            false);

        Assert.AreEqual(2, summary.UnitCount);
        Assert.AreEqual(1, summary.SoldierCount);
        Assert.AreEqual(1, summary.VehicleCount);
        Assert.AreEqual("MIXED SQUAD", summary.Title);
        Assert.AreEqual("1 Infantry / 1 Vehicles", summary.Subtitle);
        Assert.AreEqual("125/200", summary.HealthText);
        Assert.AreEqual(0.625f, summary.Health01, 0.001f);
        Assert.AreEqual(SelectionSummaryPortraitKind.MixedSoldierVehicle, summary.PortraitKind);
    }

    [Test]
    public void MixedSoldierAndAircraftUsesAirInfantryPortraitKind()
    {
        EntityManager em = _world.EntityManager;
        Entity soldier = CreatePlayerUnit(em, "Rifle Squad", new int2(1, 1), 50);
        Entity aircraft = CreatePlayerUnit(em, "Attack Helicopter", new int2(2, 1), 75);
        em.AddComponent<SelectedUnitTag>(soldier);
        em.AddComponent<SelectedUnitTag>(aircraft);
        em.AddComponentData(aircraft, new UnitAirMovement { CruiseHeight = 8f, RunwayTaxiSpeed = 5f });

        SelectionHudFeedbackUiSystemHelper.SelectedSummary summary = SelectionHudFeedbackUiSystemHelper.BuildSelectedSummary(
            em,
            new SelectionUiReadModelLookup(),
            false);

        Assert.AreEqual(1, summary.SoldierCount);
        Assert.AreEqual(1, summary.AircraftCount);
        Assert.AreEqual(SelectionSummaryPortraitKind.MixedSoldierAircraft, summary.PortraitKind);
    }

    [Test]
    public void MixedSoldierVehicleAndAircraftUsesCombinedArmsPortraitKind()
    {
        EntityManager em = _world.EntityManager;
        Entity soldier = CreatePlayerUnit(em, "Rifle Squad", new int2(1, 1), 50);
        Entity vehicle = CreatePlayerUnit(em, "Recon Vehicle", new int2(2, 1), 75);
        Entity aircraft = CreatePlayerUnit(em, "Attack Helicopter", new int2(3, 1), 80);
        em.AddComponent<SelectedUnitTag>(soldier);
        em.AddComponent<SelectedUnitTag>(vehicle);
        em.AddComponent<SelectedUnitTag>(aircraft);
        em.AddComponentData(vehicle, new UnitMovementBehavior { UsesVehicleMotion = 1 });
        em.AddComponentData(aircraft, new UnitAirMovement { CruiseHeight = 8f, RunwayTaxiSpeed = 5f });

        SelectionHudFeedbackUiSystemHelper.SelectedSummary summary = SelectionHudFeedbackUiSystemHelper.BuildSelectedSummary(
            em,
            new SelectionUiReadModelLookup(),
            false);

        Assert.AreEqual(1, summary.SoldierCount);
        Assert.AreEqual(1, summary.VehicleCount);
        Assert.AreEqual(1, summary.AircraftCount);
        Assert.AreEqual(SelectionSummaryPortraitKind.MixedSoldierVehicleAircraft, summary.PortraitKind);
    }

    [Test]
    public void MixedVehicleAndAircraftUsesAirVehiclePortraitKind()
    {
        EntityManager em = _world.EntityManager;
        Entity vehicle = CreatePlayerUnit(em, "Recon Vehicle", new int2(2, 1), 75);
        Entity aircraft = CreatePlayerUnit(em, "Attack Helicopter", new int2(3, 1), 80);
        em.AddComponent<SelectedUnitTag>(vehicle);
        em.AddComponent<SelectedUnitTag>(aircraft);
        em.AddComponentData(vehicle, new UnitMovementBehavior { UsesVehicleMotion = 1 });
        em.AddComponentData(aircraft, new UnitAirMovement { CruiseHeight = 8f, RunwayTaxiSpeed = 5f });

        SelectionHudFeedbackUiSystemHelper.SelectedSummary summary = SelectionHudFeedbackUiSystemHelper.BuildSelectedSummary(
            em,
            new SelectionUiReadModelLookup(),
            false);

        Assert.AreEqual(1, summary.VehicleCount);
        Assert.AreEqual(1, summary.AircraftCount);
        Assert.AreEqual(SelectionSummaryPortraitKind.MixedVehicleAircraft, summary.PortraitKind);
    }

    [Test]
    public void MultiVehicleSelectionUsesVehiclePortraitKind()
    {
        EntityManager em = _world.EntityManager;
        Entity firstVehicle = CreatePlayerUnit(em, "Recon Vehicle", new int2(2, 1), 75);
        Entity secondVehicle = CreatePlayerUnit(em, "Armored Vehicle", new int2(3, 1), 80);
        em.AddComponent<SelectedUnitTag>(firstVehicle);
        em.AddComponent<SelectedUnitTag>(secondVehicle);
        em.AddComponentData(firstVehicle, new UnitMovementBehavior { UsesVehicleMotion = 1 });
        em.AddComponentData(secondVehicle, new UnitMovementBehavior { UsesVehicleMotion = 1 });

        SelectionHudFeedbackUiSystemHelper.SelectedSummary summary = SelectionHudFeedbackUiSystemHelper.BuildSelectedSummary(
            em,
            new SelectionUiReadModelLookup(),
            false);

        Assert.AreEqual(2, summary.VehicleCount);
        Assert.AreEqual("2 VEHICLES", summary.Title);
        Assert.AreEqual("Vehicle Squad", summary.Subtitle);
        Assert.AreEqual(SelectionSummaryPortraitKind.Vehicles, summary.PortraitKind);
    }

    [Test]
    public void MultiTransportSelectionUsesTransportPortraitKind()
    {
        EntityManager em = _world.EntityManager;
        Entity firstTransport = CreatePlayerUnit(em, "APC Transport", new int2(2, 1), 75);
        Entity secondTransport = CreatePlayerUnit(em, "Troop Truck", new int2(3, 1), 80);
        em.AddComponent<SelectedUnitTag>(firstTransport);
        em.AddComponent<SelectedUnitTag>(secondTransport);
        em.AddComponentData(firstTransport, new UnitMovementBehavior { UsesVehicleMotion = 1 });
        em.AddComponentData(secondTransport, new UnitMovementBehavior { UsesVehicleMotion = 1 });
        em.AddComponentData(firstTransport, new UnitTransportCapacity { SoldierCapacity = 4 });
        em.AddComponentData(secondTransport, new UnitTransportCapacity { SoldierCapacity = 4 });

        SelectionHudFeedbackUiSystemHelper.SelectedSummary summary = SelectionHudFeedbackUiSystemHelper.BuildSelectedSummary(
            em,
            new SelectionUiReadModelLookup(),
            false);

        Assert.AreEqual(2, summary.TransportCount);
        Assert.AreEqual("2 TRANSPORTS", summary.Title);
        Assert.AreEqual(SelectionSummaryPortraitKind.Transports, summary.PortraitKind);
    }

    [Test]
    public void MixedGroundVehicleAndTransportUsesVehiclePortraitKind()
    {
        EntityManager em = _world.EntityManager;
        Entity vehicle = CreatePlayerUnit(em, "Battle Tank", new int2(2, 1), 75);
        Entity transport = CreatePlayerUnit(em, "APC Transport", new int2(3, 1), 80);
        em.AddComponent<SelectedUnitTag>(vehicle);
        em.AddComponent<SelectedUnitTag>(transport);
        em.AddComponentData(vehicle, new UnitMovementBehavior { UsesVehicleMotion = 1 });
        em.AddComponentData(transport, new UnitMovementBehavior { UsesVehicleMotion = 1 });
        em.AddComponentData(transport, new UnitTransportCapacity { SoldierCapacity = 4 });

        SelectionHudFeedbackUiSystemHelper.SelectedSummary summary = SelectionHudFeedbackUiSystemHelper.BuildSelectedSummary(
            em,
            new SelectionUiReadModelLookup(),
            false);

        Assert.AreEqual(1, summary.VehicleCount);
        Assert.AreEqual(1, summary.TransportCount);
        Assert.AreEqual(SelectionSummaryPortraitKind.Vehicles, summary.PortraitKind);
    }

    [Test]
    public void GroundTransportAndAirTransportUsesAirVehiclePortraitKind()
    {
        EntityManager em = _world.EntityManager;
        Entity groundTransport = CreatePlayerUnit(em, "APC Transport", new int2(2, 1), 75);
        Entity airTransport = CreatePlayerUnit(em, "Transport Helicopter", new int2(3, 1), 80);
        em.AddComponent<SelectedUnitTag>(groundTransport);
        em.AddComponent<SelectedUnitTag>(airTransport);
        em.AddComponentData(groundTransport, new UnitMovementBehavior { UsesVehicleMotion = 1 });
        em.AddComponentData(airTransport, new UnitMovementBehavior { UsesVehicleMotion = 1 });
        em.AddComponentData(groundTransport, new UnitTransportCapacity { SoldierCapacity = 4 });
        em.AddComponentData(airTransport, new UnitTransportCapacity { SoldierCapacity = 6 });
        em.AddComponentData(airTransport, new UnitAirMovement { CruiseHeight = 8f, RunwayTaxiSpeed = 5f });

        SelectionHudFeedbackUiSystemHelper.SelectedSummary summary = SelectionHudFeedbackUiSystemHelper.BuildSelectedSummary(
            em,
            new SelectionUiReadModelLookup(),
            false);

        Assert.AreEqual(1, summary.TransportCount);
        Assert.AreEqual(1, summary.AircraftCount);
        Assert.AreEqual(SelectionSummaryPortraitKind.MixedVehicleAircraft, summary.PortraitKind);
    }

    [Test]
    public void MixedSelectedOrdersDisplaysMixedOrders()
    {
        EntityManager em = _world.EntityManager;
        Entity idle = CreatePlayerUnit(em, "Rifle Squad", new int2(1, 1), 90);
        Entity moving = CreatePlayerUnit(em, "Security Squad", new int2(2, 1), 90);
        em.AddComponent<SelectedUnitTag>(idle);
        em.AddComponent<SelectedUnitTag>(moving);
        em.AddComponentData(moving, new UnitTarget { Cell = new int2(8, 8) });

        SelectionHudFeedbackUiSystemHelper.SelectedSummary summary = SelectionHudFeedbackUiSystemHelper.BuildSelectedSummary(
            em,
            new SelectionUiReadModelLookup(),
            false);

        Assert.AreEqual("Mixed orders", summary.OrderText);
    }

    [Test]
    public void SelectionPanelResolvesConfiguredFallbackPortraits()
    {
        var panelHost = new GameObject("MatchHudSelectionPanel");
        _createdObjects.Add(panelHost);

        Texture2D genericTexture = new Texture2D(1, 1);
        Texture2D vehicleTexture = new Texture2D(1, 1);
        Texture2D transportTexture = new Texture2D(1, 1);
        Texture2D mixedTexture = new Texture2D(1, 1);
        Texture2D mixedAirTexture = new Texture2D(1, 1);
        Sprite genericSprite = Sprite.Create(genericTexture, new Rect(0f, 0f, 1f, 1f), Vector2.one * 0.5f);
        Sprite vehicleSprite = Sprite.Create(vehicleTexture, new Rect(0f, 0f, 1f, 1f), Vector2.one * 0.5f);
        Sprite transportSprite = Sprite.Create(transportTexture, new Rect(0f, 0f, 1f, 1f), Vector2.one * 0.5f);
        Sprite mixedSprite = Sprite.Create(mixedTexture, new Rect(0f, 0f, 1f, 1f), Vector2.one * 0.5f);
        Sprite mixedAirSprite = Sprite.Create(mixedAirTexture, new Rect(0f, 0f, 1f, 1f), Vector2.one * 0.5f);
        try
        {
            var panel = panelHost.AddComponent<MatchHudSelectionPanelView>();
            SetPrivateField(panel, "genericSquadPortraitSprite", genericSprite);
            SetPrivateField(panel, "vehicleSquadPortraitSprite", vehicleSprite);
            SetPrivateField(panel, "transportSquadPortraitSprite", transportSprite);
            SetPrivateField(panel, "mixedForcePortraitSprite", mixedSprite);
            SetPrivateField(panel, "mixedSoldierAircraftPortraitSprite", mixedAirSprite);

            Assert.AreSame(genericSprite, panel.ResolveFallbackPortraitSprite(SelectionSummaryPortraitKind.Soldiers));
            Assert.AreSame(vehicleSprite, panel.ResolveFallbackPortraitSprite(SelectionSummaryPortraitKind.Vehicles));
            Assert.AreSame(transportSprite, panel.ResolveFallbackPortraitSprite(SelectionSummaryPortraitKind.Transports));
            Assert.AreSame(mixedSprite, panel.ResolveFallbackPortraitSprite(SelectionSummaryPortraitKind.MixedForce));
            Assert.AreSame(mixedAirSprite, panel.ResolveFallbackPortraitSprite(SelectionSummaryPortraitKind.MixedSoldierAircraft));
        }
        finally
        {
            Object.DestroyImmediate(genericSprite);
            Object.DestroyImmediate(vehicleSprite);
            Object.DestroyImmediate(transportSprite);
            Object.DestroyImmediate(mixedSprite);
            Object.DestroyImmediate(mixedAirSprite);
            Object.DestroyImmediate(genericTexture);
            Object.DestroyImmediate(vehicleTexture);
            Object.DestroyImmediate(transportTexture);
            Object.DestroyImmediate(mixedTexture);
            Object.DestroyImmediate(mixedAirTexture);
        }
    }

    [Test]
    public void SquadTrayViewReturnsConfiguredPortraitSpriteForSlot()
    {
        var trayHost = new GameObject("MatchHudSquadTray");
        var portraitHost = new GameObject("Portrait");
        _createdObjects.Add(trayHost);
        _createdObjects.Add(portraitHost);

        Texture2D portraitTexture = new Texture2D(1, 1);
        Sprite portraitSprite = Sprite.Create(portraitTexture, new Rect(0f, 0f, 1f, 1f), Vector2.one * 0.5f);
        try
        {
            Image portraitImage = portraitHost.AddComponent<Image>();
            portraitImage.sprite = portraitSprite;

            var tray = trayHost.AddComponent<MatchHudSquadTrayView>();
            var cards = new MatchHudSquadTrayView.Card[5];
            cards[1] = new MatchHudSquadTrayView.Card { PortraitImage = portraitImage };
            SetPrivateField(tray, "cards", cards);

            Assert.IsTrue(tray.TryGetPortraitSprite(MatchHudSquadTraySlot.CombatVehicles, out Sprite resolved));
            Assert.AreSame(portraitSprite, resolved);
        }
        finally
        {
            Object.DestroyImmediate(portraitSprite);
            Object.DestroyImmediate(portraitTexture);
        }
    }

    [Test]
    public void FocusedTransportPlaneSelectionPanelUsesResolvedPortrait()
    {
        EntityManager em = _world.EntityManager;
        Texture2D portraitTexture = new Texture2D(1, 1);
        Texture2D fallbackTexture = new Texture2D(1, 1);
        Sprite portraitSprite = Sprite.Create(portraitTexture, new Rect(0f, 0f, 1f, 1f), Vector2.one * 0.5f);
        Sprite fallbackSprite = Sprite.Create(fallbackTexture, new Rect(0f, 0f, 1f, 1f), Vector2.one * 0.5f);
        try
        {
            Entity transport = CreatePlayerUnit(em, "Transport Plane", new int2(4, 2), 100);
            em.AddComponent<SelectedUnitTag>(transport);
            em.AddComponentData(transport, new UnitMovementBehavior { UsesVehicleMotion = 1 });
            em.AddComponentData(transport, new UnitAirMovement
            {
                CruiseHeight = 55f,
                RunwayTaxiSpeed = 12f
            });
            em.AddComponentData(transport, new UnitSourcePrefabKey { Value = new FixedString64Bytes("Unit_Veh_Plane_Transport") });

            var selectionState = new SelectionStateCompositionSystemHelper();
            selectionState.SetFocusedUnit(transport);
            var lifecycle = new FocusedUnitLifecycleCompositionSystemHelper();
            var panel = new RecordingSelectionPanelView(fallbackSprite);
            var feedback = new SelectionHudFeedbackUiSystemHelper();
            feedback.BindMatchHudSelectionPanel(panel);

            var context = new SelectionHudFeedbackUiSystemHelper.Context(
                new SelectionUiReadModelLookup(),
                TryGetEntityManager,
                (_, entity) => entity == transport ? portraitSprite : null);
            var passengers = new List<MatchHudSelectionPanelPassengerItemModel>();

            feedback.UpdateMatchHudSelectionPanel(
                context,
                selectionState,
                lifecycle,
                null,
                passengers,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                null);

            Assert.IsTrue(panel.AppliedModel.Visible);
            Assert.AreSame(portraitSprite, panel.AppliedModel.PortraitSprite);
            Assert.AreEqual("Transport Plane", panel.AppliedModel.Title);
            Assert.IsFalse(panel.AppliedModel.BadgeVisible);
        }
        finally
        {
            Object.DestroyImmediate(portraitSprite);
            Object.DestroyImmediate(fallbackSprite);
            Object.DestroyImmediate(portraitTexture);
            Object.DestroyImmediate(fallbackTexture);
        }

        bool TryGetEntityManager(out EntityManager entityManager)
        {
            entityManager = em;
            return true;
        }
    }

    [Test]
    public void FocusedResourceHaulerSelectionPanelUsesEcsCargoStorage()
    {
        EntityManager em = _world.EntityManager;
        Entity hauler = CreatePlayerUnit(em, "Fuel Truck", new int2(6, 3), 100);
        em.SetComponentData(hauler, new Faction { Id = FactionIdentity.PlayerFactionId });
        em.AddComponent<SelectedUnitTag>(hauler);
        em.AddComponentData(hauler, new UnitMovementBehavior { UsesVehicleMotion = 1 });
        em.AddComponentData(hauler, new UnitSourcePrefabKey { Value = new FixedString64Bytes("Unit_Veh_Transport_FuelTruck") });
        em.AddComponentData(hauler, new UnitResourceHauler
        {
            BarrelCapacity = 40,
            CargoOilBarrels = 11f,
            CargoFuelBarrels = 17f
        });

        var selectionState = new SelectionStateCompositionSystemHelper();
        selectionState.SetFocusedUnit(hauler);
        var lifecycle = new FocusedUnitLifecycleCompositionSystemHelper();
        var readModel = new FocusedUnitUiReadModelUiSystemHelper();
        var panel = new RecordingSelectionPanelView(null);
        var feedback = new SelectionHudFeedbackUiSystemHelper();
        feedback.BindMatchHudSelectionPanel(panel);

        var context = new SelectionHudFeedbackUiSystemHelper.Context(new SelectionUiReadModelLookup(), TryGetEntityManager);
        feedback.RefreshFocusedSelectionReadModels(
            context,
            selectionState,
            readModel,
            new UnitTransportCapacitySystem(),
            null,
            null,
            0f);

        feedback.UpdateMatchHudSelectionPanel(
            context,
            selectionState,
            lifecycle,
            readModel,
            new List<MatchHudSelectionPanelPassengerItemModel>(),
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null);

        Assert.IsTrue(panel.AppliedModel.Visible);
        Assert.IsTrue(panel.AppliedTransportPassengers.Visible);
        Assert.AreEqual(MatchHudStorageChipKind.ResourceCargo, panel.AppliedTransportPassengers.StorageKind);
        Assert.AreEqual(28, panel.AppliedTransportPassengers.PassengerCount);
        Assert.AreEqual(40, panel.AppliedTransportPassengers.Capacity);
        Assert.AreEqual(11, panel.AppliedTransportPassengers.OilCurrent);
        Assert.AreEqual(40, panel.AppliedTransportPassengers.OilCapacity);
        Assert.AreEqual(17, panel.AppliedTransportPassengers.FuelCurrent);
        Assert.AreEqual(40, panel.AppliedTransportPassengers.FuelCapacity);

        bool TryGetEntityManager(out EntityManager entityManager)
        {
            entityManager = em;
            return true;
        }
    }

    [Test]
    public void FocusedResourceHaulerSelectionPanelShowsTypedLogisticsStatus()
    {
        EntityManager em = _world.EntityManager;
        Entity hauler = CreatePlayerUnit(em, "Fuel Truck", new int2(6, 3), 100);
        em.SetComponentData(hauler, new Faction { Id = FactionIdentity.PlayerFactionId });
        em.AddComponent<SelectedUnitTag>(hauler);
        em.AddComponentData(hauler, new UnitMovementBehavior { UsesVehicleMotion = 1 });
        em.AddComponentData(hauler, new UnitSourcePrefabKey { Value = new FixedString64Bytes("Unit_Veh_Transport_FuelTruck") });
        em.AddComponentData(hauler, new UnitResourceHauler
        {
            BarrelCapacity = 40,
            CargoOilBarrels = 0f,
            CargoFuelBarrels = 0f
        });
        em.AddComponentData(hauler, new UnitResourceHaulStatus
        {
            StatusCode = (byte)FuelLogisticsTaskStatusCode.Blocked,
            ReasonCode = (byte)FuelLogisticsBlockReasonCode.SourceUnavailable,
            ResourceKind = (byte)ResourceHaulerUtilitySystemHelper.ResourceHaulKind.Fuel
        });

        var selectionState = new SelectionStateCompositionSystemHelper();
        selectionState.SetFocusedUnit(hauler);
        var lifecycle = new FocusedUnitLifecycleCompositionSystemHelper();
        var readModel = new FocusedUnitUiReadModelUiSystemHelper();
        var panel = new RecordingSelectionPanelView(null);
        var feedback = new SelectionHudFeedbackUiSystemHelper();
        feedback.BindMatchHudSelectionPanel(panel);

        var context = new SelectionHudFeedbackUiSystemHelper.Context(new SelectionUiReadModelLookup(), TryGetEntityManager);
        feedback.RefreshFocusedSelectionReadModels(
            context,
            selectionState,
            readModel,
            new UnitTransportCapacitySystem(),
            null,
            null,
            0f);

        feedback.UpdateMatchHudSelectionPanel(
            context,
            selectionState,
            lifecycle,
            readModel,
            new List<MatchHudSelectionPanelPassengerItemModel>(),
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null);

        Assert.IsTrue(panel.AppliedTransportPassengers.Visible);
        Assert.AreEqual(MatchHudStorageChipKind.ResourceCargo, panel.AppliedTransportPassengers.StorageKind);
        Assert.AreEqual("WAITING FUEL", panel.AppliedTransportPassengers.StatusText);

        bool TryGetEntityManager(out EntityManager entityManager)
        {
            entityManager = em;
            return true;
        }
    }

    [Test]
    public void SelectedBuildingSelectionPanelShowsOilFuelStorageChips()
    {
        AssertSelectedBuildingStorageChip(
            oilCurrent: 23,
            oilCapacity: 80,
            fuelCurrent: 0,
            fuelCapacity: 0,
            MatchHudStorageChipKind.OilBarrels);
        AssertSelectedBuildingStorageChip(
            oilCurrent: 0,
            oilCapacity: 0,
            fuelCurrent: 37,
            fuelCapacity: 120,
            MatchHudStorageChipKind.FuelBarrels);
        AssertSelectedBuildingStorageChip(
            oilCurrent: 12,
            oilCapacity: 70,
            fuelCurrent: 31,
            fuelCapacity: 90,
            MatchHudStorageChipKind.OilAndFuel);
    }

    [Test]
    public void SelectedBuildingSelectionPanelReplacesStaleOilFuelStorageValues()
    {
        EntityManager em = _world.EntityManager;
        var selectionState = new SelectionStateCompositionSystemHelper();
        var lifecycle = new FocusedUnitLifecycleCompositionSystemHelper();
        var panel = new RecordingSelectionPanelView(null);
        var feedback = new SelectionHudFeedbackUiSystemHelper();
        feedback.BindMatchHudSelectionPanel(panel);

        var context = new SelectionHudFeedbackUiSystemHelper.Context(new SelectionUiReadModelLookup(), TryGetEntityManager);
        int oilCurrent = 18;
        int oilCapacity = 80;
        int fuelCurrent = 0;
        int fuelCapacity = 0;
        string title = "Oil Pump";

        ApplyBuildingSelection();

        Assert.IsTrue(panel.AppliedTransportPassengers.Visible);
        Assert.AreEqual(MatchHudStorageChipKind.OilBarrels, panel.AppliedTransportPassengers.StorageKind);
        Assert.AreEqual(18, panel.AppliedTransportPassengers.OilCurrent);
        Assert.AreEqual(80, panel.AppliedTransportPassengers.OilCapacity);
        Assert.AreEqual(0, panel.AppliedTransportPassengers.FuelCurrent);
        Assert.AreEqual(0, panel.AppliedTransportPassengers.FuelCapacity);

        oilCurrent = 0;
        oilCapacity = 0;
        fuelCurrent = 41;
        fuelCapacity = 120;
        title = "Fuel Bladder";

        ApplyBuildingSelection();

        Assert.IsTrue(panel.AppliedTransportPassengers.Visible);
        Assert.AreEqual("Fuel Bladder", panel.AppliedModel.Title);
        Assert.AreEqual(MatchHudStorageChipKind.FuelBarrels, panel.AppliedTransportPassengers.StorageKind);
        Assert.AreEqual(0, panel.AppliedTransportPassengers.OilCurrent);
        Assert.AreEqual(0, panel.AppliedTransportPassengers.OilCapacity);
        Assert.AreEqual(41, panel.AppliedTransportPassengers.FuelCurrent);
        Assert.AreEqual(120, panel.AppliedTransportPassengers.FuelCapacity);

        bool TryGetEntityManager(out EntityManager entityManager)
        {
            entityManager = em;
            return true;
        }

        bool TryGetStorage(out int storageOilCurrent, out int storageOilCapacity, out int storageFuelCurrent, out int storageFuelCapacity)
        {
            storageOilCurrent = oilCurrent;
            storageOilCapacity = oilCapacity;
            storageFuelCurrent = fuelCurrent;
            storageFuelCapacity = fuelCapacity;
            return true;
        }

        void ApplyBuildingSelection()
        {
            feedback.UpdateMatchHudSelectionPanel(
                context,
                selectionState,
                lifecycle,
                null,
                new List<MatchHudSelectionPanelPassengerItemModel>(),
                null,
                null,
                null,
                null,
                null,
                () => true,
                () => title,
                TryGetStorage,
                null,
                null,
                null);
        }
    }

    [Test]
    public void SelectedBuildingSelectionPanelOverridesStaleFocusedEntity()
    {
        EntityManager em = _world.EntityManager;
        Entity staleBarracks = CreatePlayerUnit(em, "Barracks", new int2(100, 100), 100);
        em.AddComponent<SelectedUnitTag>(staleBarracks);
        var selectionState = new SelectionStateCompositionSystemHelper();
        selectionState.SetFocusedUnit(staleBarracks);
        var lifecycle = new FocusedUnitLifecycleCompositionSystemHelper();
        Texture2D contractorTexture = new Texture2D(1, 1);
        Sprite contractorPortrait = Sprite.Create(contractorTexture, new Rect(0f, 0f, 1f, 1f), Vector2.one * 0.5f);
        try
        {
            var panel = new RecordingSelectionPanelView(null);
            var feedback = new SelectionHudFeedbackUiSystemHelper();
            feedback.BindMatchHudSelectionPanel(panel);
            var context = new SelectionHudFeedbackUiSystemHelper.Context(new SelectionUiReadModelLookup(), TryGetEntityManager);
            bool hasSelectedBuilding = true;
            int contractorHealth = 350;

            ApplySelectionPanel();
            Assert.AreEqual("Contractor Tent (113,100)", panel.AppliedModel.Title);

            hasSelectedBuilding = false;
            ApplySelectionPanel();
            Assert.AreEqual("Barracks", panel.AppliedModel.Title);

            hasSelectedBuilding = true;
            ApplySelectionPanel();

            Assert.IsTrue(panel.AppliedModel.Visible);
            Assert.AreEqual("Contractor Tent (113,100)", panel.AppliedModel.Title);
            Assert.AreEqual("Base Structure", panel.AppliedModel.Subtitle);
            Assert.AreEqual("Structure selected", panel.AppliedModel.CurrentOrder);
            Assert.AreEqual("350/350", panel.AppliedModel.HealthText);
            Assert.AreEqual(1f, panel.AppliedModel.Health01);
            Assert.AreSame(contractorPortrait, panel.AppliedModel.PortraitSprite);

            contractorHealth = 225;
            ApplySelectionPanel();
            Assert.AreEqual("225/350", panel.AppliedModel.HealthText);
            Assert.AreEqual(225f / 350f, panel.AppliedModel.Health01, 0.0001f);

            void ApplySelectionPanel()
            {
                feedback.UpdateMatchHudSelectionPanel(
                    context,
                    selectionState,
                    lifecycle,
                    null,
                    new List<MatchHudSelectionPanelPassengerItemModel>(),
                    null,
                    null,
                    null,
                    () => contractorPortrait,
                    null,
                    () => hasSelectedBuilding,
                    () => "Contractor Tent (113,100)",
                    null,
                    null,
                    null,
                    null,
                    null,
                    TryGetContractorHealth);
            }

            bool TryGetContractorHealth(out int current, out int max)
            {
                current = contractorHealth;
                max = 350;
                return true;
            }
        }
        finally
        {
            Object.DestroyImmediate(contractorPortrait);
            Object.DestroyImmediate(contractorTexture);
        }

        bool TryGetEntityManager(out EntityManager entityManager)
        {
            entityManager = em;
            return true;
        }
    }

    [Test]
    public void SelectedBuildingResourceStoragePanelSkipsApplyUntilVersionChanges()
    {
        EntityManager em = _world.EntityManager;
        var selectionState = new SelectionStateCompositionSystemHelper();
        var lifecycle = new FocusedUnitLifecycleCompositionSystemHelper();
        var panel = new RecordingSelectionPanelView(null);
        var feedback = new SelectionHudFeedbackUiSystemHelper();
        feedback.BindMatchHudSelectionPanel(panel);

        var context = new SelectionHudFeedbackUiSystemHelper.Context(new SelectionUiReadModelLookup(), TryGetEntityManager);
        uint storageVersion = 4u;
        int oilCurrent = 18;
        int oilCapacity = 80;

        ApplyBuildingSelection();
        ApplyBuildingSelection();

        Assert.AreEqual(1, panel.ApplyCount);
        Assert.AreEqual(1, panel.ApplyTransportPassengersCount);
        Assert.AreEqual(18, panel.AppliedTransportPassengers.OilCurrent);

        storageVersion++;
        oilCurrent = 21;
        ApplyBuildingSelection();

        Assert.AreEqual(1, panel.ApplyCount);
        Assert.AreEqual(2, panel.ApplyTransportPassengersCount);
        Assert.AreEqual(21, panel.AppliedTransportPassengers.OilCurrent);

        bool TryGetEntityManager(out EntityManager entityManager)
        {
            entityManager = em;
            return true;
        }

        bool TryGetStorageSnapshot(out SelectedBuildingResourceStorageSnapshot snapshot)
        {
            snapshot = new SelectedBuildingResourceStorageSnapshot(
                91,
                oilCurrent,
                oilCapacity,
                0,
                0,
                storageVersion);
            return true;
        }

        void ApplyBuildingSelection()
        {
            feedback.UpdateMatchHudSelectionPanel(
                context,
                selectionState,
                lifecycle,
                null,
                new List<MatchHudSelectionPanelPassengerItemModel>(),
                null,
                null,
                null,
                null,
                null,
                () => true,
                () => "Oil Pump",
                null,
                TryGetStorageSnapshot,
                null,
                null);
        }
    }

    [Test]
    public void SelectedBuildingRefineryStoragePanelReportsConversionStatus()
    {
        EntityManager em = _world.EntityManager;
        var selectionState = new SelectionStateCompositionSystemHelper();
        var lifecycle = new FocusedUnitLifecycleCompositionSystemHelper();
        var panel = new RecordingSelectionPanelView(null);
        var feedback = new SelectionHudFeedbackUiSystemHelper();
        feedback.BindMatchHudSelectionPanel(panel);

        var context = new SelectionHudFeedbackUiSystemHelper.Context(new SelectionUiReadModelLookup(), TryGetEntityManager);
        uint storageVersion = 1u;
        int oilCurrent = 0;
        int fuelCurrent = 4;
        const int oilCapacity = 80;
        const int fuelCapacity = 100;
        const float fuelBarrelsPerDay = 20f;

        ApplyBuildingSelection();
        Assert.AreEqual("WAITING OIL", panel.AppliedTransportPassengers.StatusText);

        oilCurrent = 8;
        fuelCurrent = fuelCapacity;
        storageVersion++;
        ApplyBuildingSelection();
        Assert.AreEqual("FUEL FULL", panel.AppliedTransportPassengers.StatusText);

        fuelCurrent = 40;
        storageVersion++;
        ApplyBuildingSelection();
        Assert.AreEqual("CONVERTING", panel.AppliedTransportPassengers.StatusText);
        Assert.AreEqual(3, panel.ApplyTransportPassengersCount);

        bool TryGetEntityManager(out EntityManager entityManager)
        {
            entityManager = em;
            return true;
        }

        bool TryGetStorageSnapshot(out SelectedBuildingResourceStorageSnapshot snapshot)
        {
            snapshot = new SelectedBuildingResourceStorageSnapshot(
                92,
                oilCurrent,
                oilCapacity,
                fuelCurrent,
                fuelCapacity,
                storageVersion,
                oilBarrelsPerDay: 0f,
                fuelBarrelsPerDay: fuelBarrelsPerDay);
            return true;
        }

        void ApplyBuildingSelection()
        {
            feedback.UpdateMatchHudSelectionPanel(
                context,
                selectionState,
                lifecycle,
                null,
                new List<MatchHudSelectionPanelPassengerItemModel>(),
                null,
                null,
                null,
                null,
                null,
                () => true,
                () => "Oil Refinery",
                null,
                TryGetStorageSnapshot,
                null,
                null);
        }
    }

    [Test]
    public void SelectedBuildingFabricationPanelPrefersFabricationReadModel()
    {
        EntityManager em = _world.EntityManager;
        var selectionState = new SelectionStateCompositionSystemHelper();
        var lifecycle = new FocusedUnitLifecycleCompositionSystemHelper();
        var panel = new RecordingSelectionPanelView(null);
        var feedback = new SelectionHudFeedbackUiSystemHelper();
        feedback.BindMatchHudSelectionPanel(panel);

        var context = new SelectionHudFeedbackUiSystemHelper.Context(new SelectionUiReadModelLookup(), TryGetEntityManager);
        bool genericStorageRead = false;

        feedback.UpdateMatchHudSelectionPanel(
            context,
            selectionState,
            lifecycle,
            null,
            new List<MatchHudSelectionPanelPassengerItemModel>(),
            null,
            null,
            null,
            null,
            null,
            () => true,
            () => "Field Fabrication Depot",
            TryGetGenericStorage,
            null,
            TryGetFabrication,
            null,
            null);

        MatchHudTransportPassengersModel model = panel.AppliedTransportPassengers;
        Assert.IsTrue(panel.AppliedModel.Visible);
        Assert.AreEqual("Field Fabrication Depot", panel.AppliedModel.Title);
        Assert.IsFalse(genericStorageRead, "Fabrication must take precedence over the generic storage fallback.");
        Assert.IsTrue(model.Visible);
        Assert.AreEqual(MatchHudStorageChipKind.MaterialFabrication, model.StorageKind);
        Assert.AreEqual(18, model.OilCurrent);
        Assert.AreEqual(60, model.OilCapacity);
        Assert.AreEqual(3.5f, model.OilConsumedPerCycle, 0.001f);
        Assert.AreEqual(7, model.MaterialsOutputPerCycle);
        Assert.AreEqual(12f, model.CycleDurationSeconds, 0.001f);
        Assert.AreEqual(0.45f, model.CycleProgress01, 0.001f);
        Assert.AreEqual(32, model.MaterialsCurrent);
        Assert.AreEqual(80, model.MaterialsCapacity);
        Assert.IsTrue(model.ProductionEnabled);
        Assert.AreEqual("FABRICATING", model.StatusText);

        bool TryGetEntityManager(out EntityManager entityManager)
        {
            entityManager = em;
            return true;
        }

        bool TryGetGenericStorage(out int oilCurrent, out int oilCapacity, out int fuelCurrent, out int fuelCapacity)
        {
            genericStorageRead = true;
            oilCurrent = 1;
            oilCapacity = 2;
            fuelCurrent = 3;
            fuelCapacity = 4;
            return true;
        }

        bool TryGetFabrication(out UiMaterialFabricationReadModel readModel)
        {
            readModel = CreateMaterialFabricationReadModel(
                version: 9u,
                status: MaterialFabricationStatusCode.Producing,
                blockReason: MaterialFabricationBlockReasonCode.None);
            return true;
        }
    }

    [Test]
    public void SelectedBuildingFabricationPanelSkipsApplyUntilVersionChanges()
    {
        EntityManager em = _world.EntityManager;
        var selectionState = new SelectionStateCompositionSystemHelper();
        var lifecycle = new FocusedUnitLifecycleCompositionSystemHelper();
        var panel = new RecordingSelectionPanelView(null);
        var feedback = new SelectionHudFeedbackUiSystemHelper();
        feedback.BindMatchHudSelectionPanel(panel);

        var context = new SelectionHudFeedbackUiSystemHelper.Context(new SelectionUiReadModelLookup(), TryGetEntityManager);
        uint version = 14u;

        ApplyBuildingSelection();
        ApplyBuildingSelection();

        Assert.AreEqual(1, panel.ApplyCount);
        Assert.AreEqual(1, panel.ApplyTransportPassengersCount);

        version++;
        ApplyBuildingSelection();

        Assert.AreEqual(1, panel.ApplyCount);
        Assert.AreEqual(2, panel.ApplyTransportPassengersCount);

        bool TryGetEntityManager(out EntityManager entityManager)
        {
            entityManager = em;
            return true;
        }

        bool TryGetFabrication(out UiMaterialFabricationReadModel readModel)
        {
            readModel = CreateMaterialFabricationReadModel(
                version,
                MaterialFabricationStatusCode.Producing,
                MaterialFabricationBlockReasonCode.None);
            return true;
        }

        void ApplyBuildingSelection()
        {
            feedback.UpdateMatchHudSelectionPanel(
                context,
                selectionState,
                lifecycle,
                null,
                new List<MatchHudSelectionPanelPassengerItemModel>(),
                null,
                null,
                null,
                null,
                null,
                () => true,
                () => "Field Fabrication Depot",
                null,
                null,
                TryGetFabrication,
                null,
                null);
        }
    }

    [Test]
    public void SelectedBuildingFabricationPanelShowsTypedStatusCopy()
    {
        EntityManager em = _world.EntityManager;
        var selectionState = new SelectionStateCompositionSystemHelper();
        var lifecycle = new FocusedUnitLifecycleCompositionSystemHelper();
        var panel = new RecordingSelectionPanelView(null);
        var feedback = new SelectionHudFeedbackUiSystemHelper();
        feedback.BindMatchHudSelectionPanel(panel);

        var context = new SelectionHudFeedbackUiSystemHelper.Context(new SelectionUiReadModelLookup(), TryGetEntityManager);
        uint version = 1u;
        MaterialFabricationStatusCode status = MaterialFabricationStatusCode.Producing;
        MaterialFabricationBlockReasonCode blockReason = MaterialFabricationBlockReasonCode.None;

        AssertStatus("FABRICATING");
        status = MaterialFabricationStatusCode.Blocked;
        blockReason = MaterialFabricationBlockReasonCode.NoOilInput;
        AssertStatus("WAITING OIL");
        blockReason = MaterialFabricationBlockReasonCode.MaterialsCapacityFull;
        AssertStatus("MATERIALS FULL");
        blockReason = MaterialFabricationBlockReasonCode.BuildingDisabled;
        AssertStatus("BUILDING DISABLED");
        status = MaterialFabricationStatusCode.Disabled;
        blockReason = MaterialFabricationBlockReasonCode.ProductionDisabled;
        AssertStatus("PRODUCTION DISABLED");
        Assert.AreEqual(5, panel.ApplyTransportPassengersCount);

        bool TryGetEntityManager(out EntityManager entityManager)
        {
            entityManager = em;
            return true;
        }

        bool TryGetFabrication(out UiMaterialFabricationReadModel readModel)
        {
            readModel = CreateMaterialFabricationReadModel(version++, status, blockReason);
            return true;
        }

        void AssertStatus(string expectedStatus)
        {
            feedback.UpdateMatchHudSelectionPanel(
                context,
                selectionState,
                lifecycle,
                null,
                new List<MatchHudSelectionPanelPassengerItemModel>(),
                null,
                null,
                null,
                null,
                null,
                () => true,
                () => "Field Fabrication Depot",
                null,
                null,
                TryGetFabrication,
                null,
                null);
            Assert.AreEqual(expectedStatus, panel.AppliedTransportPassengers.StatusText);
        }
    }

    private static UiMaterialFabricationReadModel CreateMaterialFabricationReadModel(
        uint version,
        MaterialFabricationStatusCode status,
        MaterialFabricationBlockReasonCode blockReason)
    {
        return new UiMaterialFabricationReadModel(
            runtimeBuildingId: 117,
            ownerFactionId: FactionIdentity.PlayerFactionId,
            oilInputCurrentBarrels: 18f,
            oilInputCapacityBarrels: 60,
            oilConsumedPerCycle: 3.5f,
            cycleDurationSeconds: 12f,
            cycleProgressSeconds: 5.4f,
            progress01: 0.45f,
            materialsOutputPerCycle: 7,
            factionMaterialsCurrent: 32,
            factionMaterialsCapacity: 80,
            productionEnabled: status != MaterialFabricationStatusCode.Disabled,
            status,
            blockReason,
            version);
    }

    private void AssertSelectedBuildingStorageChip(
        int oilCurrent,
        int oilCapacity,
        int fuelCurrent,
        int fuelCapacity,
        MatchHudStorageChipKind expectedKind)
    {
        EntityManager em = _world.EntityManager;
        var selectionState = new SelectionStateCompositionSystemHelper();
        var lifecycle = new FocusedUnitLifecycleCompositionSystemHelper();
        var panel = new RecordingSelectionPanelView(null);
        var feedback = new SelectionHudFeedbackUiSystemHelper();
        feedback.BindMatchHudSelectionPanel(panel);

        var context = new SelectionHudFeedbackUiSystemHelper.Context(new SelectionUiReadModelLookup(), TryGetEntityManager);
        feedback.UpdateMatchHudSelectionPanel(
            context,
            selectionState,
            lifecycle,
            null,
            new List<MatchHudSelectionPanelPassengerItemModel>(),
            null,
            null,
            null,
            null,
            null,
            () => true,
            () => "Storage Building",
            TryGetStorage,
            null,
            null,
            null);

        Assert.IsTrue(panel.AppliedModel.Visible);
        Assert.AreEqual("Storage Building", panel.AppliedModel.Title);
        Assert.IsTrue(panel.AppliedTransportPassengers.Visible);
        Assert.AreEqual(expectedKind, panel.AppliedTransportPassengers.StorageKind);
        Assert.AreEqual(oilCurrent + fuelCurrent, panel.AppliedTransportPassengers.PassengerCount);
        Assert.AreEqual(oilCapacity + fuelCapacity, panel.AppliedTransportPassengers.Capacity);
        Assert.AreEqual(oilCurrent, panel.AppliedTransportPassengers.OilCurrent);
        Assert.AreEqual(oilCapacity, panel.AppliedTransportPassengers.OilCapacity);
        Assert.AreEqual(fuelCurrent, panel.AppliedTransportPassengers.FuelCurrent);
        Assert.AreEqual(fuelCapacity, panel.AppliedTransportPassengers.FuelCapacity);

        bool TryGetEntityManager(out EntityManager entityManager)
        {
            entityManager = em;
            return true;
        }

        bool TryGetStorage(out int storageOilCurrent, out int storageOilCapacity, out int storageFuelCurrent, out int storageFuelCapacity)
        {
            storageOilCurrent = oilCurrent;
            storageOilCapacity = oilCapacity;
            storageFuelCurrent = fuelCurrent;
            storageFuelCapacity = fuelCapacity;
            return true;
        }
    }

    private static Entity CreatePlayerUnit(EntityManager em, string displayName, int2 cell, int health)
    {
        Entity entity = em.CreateEntity();
        em.AddComponentData(entity, new Faction { Id = 0 });
        em.AddComponentData(entity, new UnitGrid { Cell = cell });
        em.AddComponentData(entity, new UnitHealth { Current = health, Max = 100 });
        em.AddComponentData(entity, new UnitDisplayInfo
        {
            Name = new FixedString64Bytes(displayName),
            Description = new FixedString128Bytes("Selection summary test unit")
        });
        em.AddComponentData(entity, new UnitMove
        {
            Speed = 5f,
            WalkSpeed = 5f,
            RoadSpeedMultiplier = 1f,
            ArriveDistance = 0.05f
        });
        em.AddComponentData(entity, LocalTransform.FromPosition(new float3(cell.x, 0f, cell.y)));
        return entity;
    }

    private static void SetPrivateField<T>(object target, string fieldName, T value)
    {
        FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(field, $"Missing private field {fieldName} on {target.GetType().Name}.");
        field.SetValue(target, value);
    }

    private sealed class RecordingSelectionPanelView : IMatchHudSelectionPanelView
    {
        private readonly Sprite _fallbackSprite;

        public RecordingSelectionPanelView(Sprite fallbackSprite)
        {
            _fallbackSprite = fallbackSprite;
        }

        public MatchHudSelectionPanelModel AppliedModel { get; private set; }
        public MatchHudTransportPassengersModel AppliedTransportPassengers { get; private set; }
        public int ApplyCount { get; private set; }
        public int ApplyTransportPassengersCount { get; private set; }

        public void BindActions(System.Action returnRequested, System.Action destroyRequested, System.Action boardRequested)
        {
        }

        public void BindCameraAction(System.Action cameraRequested)
        {
        }

        public void BindTransportPassengerActions(
            System.Action passengerChipRequested,
            System.Action passengerDrawerCloseRequested,
            System.Action passengerExitAllRequested,
            System.Action<UiEntityHandle> passengerExitRequested)
        {
        }

        public void BindMaterialFabricationProductionAction(System.Action<bool> productionEnabledRequested)
        {
        }

        public void HideSelection()
        {
            AppliedModel = MatchHudSelectionPanelModel.Hidden;
        }

        public void SetSelectionVisible(bool visible)
        {
        }

        public void SetSelectionVisible(bool visible, Sprite portraitSprite)
        {
        }

        public void SetBoardActionSelected(bool selected)
        {
        }

        public void SetCameraActionSelected(bool selected)
        {
        }

        public void SetCameraActionEnabled(bool enabled)
        {
        }

        public Sprite ResolveFallbackPortraitSprite(SelectionSummaryPortraitKind kind)
        {
            return _fallbackSprite;
        }

        public void Apply(MatchHudSelectionPanelModel model)
        {
            AppliedModel = model;
            ApplyCount++;
        }

        public void ApplyTransportPassengers(MatchHudTransportPassengersModel model)
        {
            AppliedTransportPassengers = model;
            ApplyTransportPassengersCount++;
        }
    }
}
