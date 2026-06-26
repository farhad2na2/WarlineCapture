using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.UI;

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
            RunCase(test => test.MultiTransportSelectionUsesVehiclePortraitKind());
            RunCase(test => test.MixedGroundVehicleAndTransportUsesVehiclePortraitKind());
            RunCase(test => test.GroundTransportAndAirTransportUsesAirVehiclePortraitKind());
            RunCase(test => test.MixedSelectedOrdersDisplaysMixedOrders());
            RunCase(test => test.FocusedTransportPlaneSelectionPanelUsesResolvedPortrait());
            Debug.Log("[SelectionSummaryFocusedValidation] result=Passed tests=11");
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

        SelectionHudFeedbackBoundary.SelectedSummary summary = SelectionHudFeedbackBoundary.BuildSelectedSummary(
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

        SelectionHudFeedbackBoundary.SelectedSummary summary = SelectionHudFeedbackBoundary.BuildSelectedSummary(
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

        SelectionHudFeedbackBoundary.SelectedSummary summary = SelectionHudFeedbackBoundary.BuildSelectedSummary(
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

        SelectionHudFeedbackBoundary.SelectedSummary summary = SelectionHudFeedbackBoundary.BuildSelectedSummary(
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

        SelectionHudFeedbackBoundary.SelectedSummary summary = SelectionHudFeedbackBoundary.BuildSelectedSummary(
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

        SelectionHudFeedbackBoundary.SelectedSummary summary = SelectionHudFeedbackBoundary.BuildSelectedSummary(
            em,
            new SelectionUiReadModelLookup(),
            false);

        Assert.AreEqual(2, summary.VehicleCount);
        Assert.AreEqual("2 VEHICLES", summary.Title);
        Assert.AreEqual("Vehicle Squad", summary.Subtitle);
        Assert.AreEqual(SelectionSummaryPortraitKind.Vehicles, summary.PortraitKind);
    }

    [Test]
    public void MultiTransportSelectionUsesVehiclePortraitKind()
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

        SelectionHudFeedbackBoundary.SelectedSummary summary = SelectionHudFeedbackBoundary.BuildSelectedSummary(
            em,
            new SelectionUiReadModelLookup(),
            false);

        Assert.AreEqual(2, summary.TransportCount);
        Assert.AreEqual("2 TRANSPORTS", summary.Title);
        Assert.AreEqual(SelectionSummaryPortraitKind.Vehicles, summary.PortraitKind);
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

        SelectionHudFeedbackBoundary.SelectedSummary summary = SelectionHudFeedbackBoundary.BuildSelectedSummary(
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

        SelectionHudFeedbackBoundary.SelectedSummary summary = SelectionHudFeedbackBoundary.BuildSelectedSummary(
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

        SelectionHudFeedbackBoundary.SelectedSummary summary = SelectionHudFeedbackBoundary.BuildSelectedSummary(
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
        Texture2D mixedTexture = new Texture2D(1, 1);
        Texture2D mixedAirTexture = new Texture2D(1, 1);
        Sprite genericSprite = Sprite.Create(genericTexture, new Rect(0f, 0f, 1f, 1f), Vector2.one * 0.5f);
        Sprite vehicleSprite = Sprite.Create(vehicleTexture, new Rect(0f, 0f, 1f, 1f), Vector2.one * 0.5f);
        Sprite mixedSprite = Sprite.Create(mixedTexture, new Rect(0f, 0f, 1f, 1f), Vector2.one * 0.5f);
        Sprite mixedAirSprite = Sprite.Create(mixedAirTexture, new Rect(0f, 0f, 1f, 1f), Vector2.one * 0.5f);
        try
        {
            var panel = panelHost.AddComponent<MatchHudSelectionPanelView>();
            SetPrivateField(panel, "genericSquadPortraitSprite", genericSprite);
            SetPrivateField(panel, "vehicleSquadPortraitSprite", vehicleSprite);
            SetPrivateField(panel, "mixedForcePortraitSprite", mixedSprite);
            SetPrivateField(panel, "mixedSoldierAircraftPortraitSprite", mixedAirSprite);

            Assert.AreSame(genericSprite, panel.ResolveFallbackPortraitSprite(SelectionSummaryPortraitKind.Soldiers));
            Assert.AreSame(vehicleSprite, panel.ResolveFallbackPortraitSprite(SelectionSummaryPortraitKind.Vehicles));
            Assert.AreSame(vehicleSprite, panel.ResolveFallbackPortraitSprite(SelectionSummaryPortraitKind.Transports));
            Assert.AreSame(mixedSprite, panel.ResolveFallbackPortraitSprite(SelectionSummaryPortraitKind.MixedForce));
            Assert.AreSame(mixedAirSprite, panel.ResolveFallbackPortraitSprite(SelectionSummaryPortraitKind.MixedSoldierAircraft));
        }
        finally
        {
            Object.DestroyImmediate(genericSprite);
            Object.DestroyImmediate(vehicleSprite);
            Object.DestroyImmediate(mixedSprite);
            Object.DestroyImmediate(mixedAirSprite);
            Object.DestroyImmediate(genericTexture);
            Object.DestroyImmediate(vehicleTexture);
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
            var feedback = new SelectionHudFeedbackBoundary();
            feedback.BindMatchHudSelectionPanel(panel);

            var context = new SelectionHudFeedbackBoundary.Context(
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

        public void BindActions(System.Action returnRequested, System.Action destroyRequested, System.Action boardRequested)
        {
        }

        public void BindTransportPassengerActions(
            System.Action passengerChipRequested,
            System.Action passengerDrawerCloseRequested,
            System.Action passengerExitAllRequested,
            System.Action<UiEntityHandle> passengerExitRequested)
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

        public Sprite ResolveFallbackPortraitSprite(SelectionSummaryPortraitKind kind)
        {
            return _fallbackSprite;
        }

        public void Apply(MatchHudSelectionPanelModel model)
        {
            AppliedModel = model;
        }

        public void ApplyTransportPassengers(MatchHudTransportPassengersModel model)
        {
        }
    }
}
