using System.Collections.Generic;
using System;
using NUnit.Framework;
using Unity.Entities;
using UnityEditor;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public sealed class BuildDrawerCatalogQuerySystemTests
{
    private const string BuildDrawerPrefabPath = "Assets/Game/Prefabs/UI/Shell/Popups/SCN09_BuildDrawerPopup.prefab";

    private readonly List<UnityEngine.Object> _createdObjects = new();
    private readonly List<BuildDrawerCatalogItem> _results = new();
    private readonly BuildDrawerCatalogQuerySystem _query = new();

    public static void RunFocusedValidation()
    {
        int passed = 0;
        try
        {
            RunValidationStep(
                nameof(CollectBuildings_IncludesOnlyRequestableBuildingConfigs),
                test => test.CollectBuildings_IncludesOnlyRequestableBuildingConfigs(),
                ref passed);
            RunValidationStep(
                nameof(CollectUnits_FiltersNonRequestableUnits),
                test => test.CollectUnits_FiltersNonRequestableUnits(),
                ref passed);
            RunValidationStep(
                nameof(CollectUnits_CategorizesVehiclesAircraftsAndSoldiers),
                test => test.CollectUnits_CategorizesVehiclesAircraftsAndSoldiers(),
                ref passed);
            RunValidationStep(
                nameof(CollectAll_ReturnsEachRequestableEntryOnce),
                test => test.CollectAll_ReturnsEachRequestableEntryOnce(),
                ref passed);
            RunValidationStep(
                nameof(CurrentBuildDrawerPrefabBindsTabsAndCatalogItems),
                test => test.CurrentBuildDrawerPrefabBindsTabsAndCatalogItems(),
                ref passed);
            RunValidationStep(
                nameof(CurrentBuildDrawerPrefabBindsSelectionDetailAndActionLabel),
                test => test.CurrentBuildDrawerPrefabBindsSelectionDetailAndActionLabel(),
                ref passed);
            RunValidationStep(
                nameof(BuildDrawerPrimaryAction_RoutesBuildingPlacementRequestAndClosesDrawer),
                test => test.BuildDrawerPrimaryAction_RoutesBuildingPlacementRequestAndClosesDrawer(),
                ref passed);
            RunValidationStep(
                nameof(BuildDrawerPrimaryAction_RoutesUnitProductionRequestAndKeepsDrawerOpen),
                test => test.BuildDrawerPrimaryAction_RoutesUnitProductionRequestAndKeepsDrawerOpen(),
                ref passed);
            RunValidationStep(
                nameof(BuildDrawerPrimaryAction_RealBuildingRequestBeginsPlacementAndClosesDrawer),
                test => test.BuildDrawerPrimaryAction_RealBuildingRequestBeginsPlacementAndClosesDrawer(),
                ref passed);
            RunValidationStep(
                nameof(BuildDrawerPrimaryAction_RealUnitRequestQueuesProductionAndRefreshesQueue),
                test => test.BuildDrawerPrimaryAction_RealUnitRequestQueuesProductionAndRefreshesQueue(),
                ref passed);
            RunValidationStep(
                nameof(CurrentBuildDrawerPrefabBindsProductionQueueSnapshot),
                test => test.CurrentBuildDrawerPrefabBindsProductionQueueSnapshot(),
                ref passed);
            RunValidationStep(
                nameof(CurrentBuildDrawerPrefabShowsSingleActiveProductionForOneQueueEntry),
                test => test.CurrentBuildDrawerPrefabShowsSingleActiveProductionForOneQueueEntry(),
                ref passed);
            RunValidationStep(
                nameof(BuildDrawerCancelButton_RoutesActiveProductionCancelRequest),
                test => test.BuildDrawerCancelButton_RoutesActiveProductionCancelRequest(),
                ref passed);
            RunValidationStep(
                nameof(BuildDrawerItemSelection_DoesNotMutateFirstCardThumbnail),
                test => test.BuildDrawerItemSelection_DoesNotMutateFirstCardThumbnail(),
                ref passed);
            RunValidationStep(
                nameof(BuildDrawerPopup_BlocksGameplayAndPlacementPointerInput),
                test => test.BuildDrawerPopup_BlocksGameplayAndPlacementPointerInput(),
                ref passed);
            RunValidationStep(
                nameof(BuildDrawerPopup_ReportsOpenStateForProductionCameraFocusGate),
                test => test.BuildDrawerPopup_ReportsOpenStateForProductionCameraFocusGate(),
                ref passed);
            RunValidationStep(
                nameof(BuildDrawerPrimaryActionButton_ReceivesPointerRaycastAtCenter),
                test => test.BuildDrawerPrimaryActionButton_ReceivesPointerRaycastAtCenter(),
                ref passed);

            Debug.Log($"[BuildDrawerCatalogQueryValidation] result=Passed tests={passed}");
            EditorApplication.Exit(0);
        }
        catch (Exception exception)
        {
            Debug.LogError($"[BuildDrawerCatalogQueryValidation] result=Failed passed={passed}\n{exception}");
            EditorApplication.Exit(1);
        }
    }

    private static void RunValidationStep(
        string name,
        Action<BuildDrawerCatalogQuerySystemTests> action,
        ref int passed)
    {
        var tests = new BuildDrawerCatalogQuerySystemTests();
        try
        {
            action(tests);
            passed++;
            Debug.Log($"[BuildDrawerCatalogQueryValidation] step={name} result=Passed");
        }
        finally
        {
            tests.TearDown();
        }
    }

    [TearDown]
    public void TearDown()
    {
        for (int i = _createdObjects.Count - 1; i >= 0; i--)
        {
            if (_createdObjects[i] != null)
                UnityEngine.Object.DestroyImmediate(_createdObjects[i]);
        }

        _createdObjects.Clear();
        _results.Clear();
    }

    [Test]
    public void CollectBuildings_IncludesOnlyRequestableBuildingConfigs()
    {
        BuildingPlacementSystemConfig config = CreateAsset<BuildingPlacementSystemConfig>();
        GameObject requestable = CreateBuilding("Requestable Barracks", true, BuildingRole.MilitaryCamp, false);
        GameObject blocked = CreateBuilding("Blocked Shop", false, BuildingRole.Shop, false);
        config.Spawnables.Add(requestable);
        config.Spawnables.Add(blocked);

        _query.Collect(null, config, BuildDrawerCategory.Buildings, _results);

        Assert.AreEqual(1, _results.Count);
        Assert.AreSame(requestable, _results[0].Prefab);
        Assert.AreEqual(BuildDrawerCategory.Buildings, _results[0].Category);
        Assert.AreEqual("PLACE", _results[0].ActionLabel);
    }

    [Test]
    public void CollectUnits_FiltersNonRequestableUnits()
    {
        UnitPrefabRegistryAuthoringConfig config = CreateAsset<UnitPrefabRegistryAuthoringConfig>();
        GameObject requestable = CreateUnit("Requestable Soldier", true, false, Vector2Int.one, 0);
        GameObject blocked = CreateUnit("Blocked Soldier", false, false, Vector2Int.one, 0);
        config.UnitSpawnPrefabs.Add(requestable);
        config.UnitSpawnPrefabs.Add(blocked);

        _query.Collect(config, null, BuildDrawerCategory.Soldiers, _results);

        Assert.AreEqual(1, _results.Count);
        Assert.AreSame(requestable, _results[0].Prefab);
        Assert.AreEqual(BuildDrawerCategory.Soldiers, _results[0].Category);
        Assert.AreEqual("RECRUIT", _results[0].ActionLabel);
    }

    [Test]
    public void CollectUnits_CategorizesVehiclesAircraftsAndSoldiers()
    {
        UnitPrefabRegistryAuthoringConfig config = CreateAsset<UnitPrefabRegistryAuthoringConfig>();
        GameObject soldier = CreateUnit("Rifle Soldier", true, false, Vector2Int.one, 0);
        GameObject vehicle = CreateUnit("Light Vehicle", true, false, new Vector2Int(2, 2), 0);
        GameObject aircraft = CreateUnit("Attack Aircraft", true, true, new Vector2Int(3, 3), 0);
        config.UnitSpawnPrefabs.Add(soldier);
        config.UnitSpawnPrefabs.Add(vehicle);
        config.UnitSpawnPrefabs.Add(aircraft);

        _query.Collect(config, null, BuildDrawerCategory.Soldiers, _results);
        Assert.AreEqual(1, _results.Count);
        Assert.AreSame(soldier, _results[0].Prefab);

        _query.Collect(config, null, BuildDrawerCategory.Vehicles, _results);
        Assert.AreEqual(1, _results.Count);
        Assert.AreSame(vehicle, _results[0].Prefab);
        Assert.AreEqual("PRODUCE", _results[0].ActionLabel);

        _query.Collect(config, null, BuildDrawerCategory.Aircrafts, _results);
        Assert.AreEqual(1, _results.Count);
        Assert.AreSame(aircraft, _results[0].Prefab);
        Assert.AreEqual("PRODUCE", _results[0].ActionLabel);
    }

    [Test]
    public void CollectAll_ReturnsEachRequestableEntryOnce()
    {
        UnitPrefabRegistryAuthoringConfig units = CreateAsset<UnitPrefabRegistryAuthoringConfig>();
        BuildingPlacementSystemConfig buildings = CreateAsset<BuildingPlacementSystemConfig>();
        GameObject building = CreateBuilding("Requestable Tent", true, BuildingRole.TentRefugee, false);
        GameObject soldier = CreateUnit("Requestable Soldier", true, false, Vector2Int.one, 0);
        GameObject blockedAircraft = CreateUnit("Blocked Aircraft", false, true, new Vector2Int(3, 3), 0);
        buildings.Spawnables.Add(building);
        units.UnitSpawnPrefabs.Add(soldier);
        units.UnitSpawnPrefabs.Add(blockedAircraft);

        _query.CollectAll(units, buildings, _results);

        Assert.AreEqual(2, _results.Count);
        Assert.AreSame(building, _results[0].Prefab);
        Assert.AreSame(soldier, _results[1].Prefab);
    }

    [Test]
    public void CurrentBuildDrawerPrefabBindsTabsAndCatalogItems()
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(BuildDrawerPrefabPath);
        Assert.NotNull(prefab, "Build drawer popup prefab must exist.");

        GameObject instance = UnityEngine.Object.Instantiate(prefab);
        _createdObjects.Add(instance);

        BuildDrawerView view = instance.GetComponent<BuildDrawerView>();
        BuildDrawerCatalogPresenterView presenter = instance.GetComponent<BuildDrawerCatalogPresenterView>();
        Assert.NotNull(view, "Build drawer popup must serialize BuildDrawerView on the root.");
        Assert.NotNull(presenter, "Build drawer popup must serialize BuildDrawerCatalogPresenterView on the root.");
        Assert.NotNull(view.ItemTemplate, "Build drawer must serialize its item template.");
        Assert.NotNull(view.ItemTemplate.SelectionButton, "Build drawer item template must expose a selection button.");
        Assert.NotNull(view.ItemTemplate.FrameImage, "Build drawer item template must expose a frame image for selected state.");
        Assert.NotNull(view.SelectedItemFrameSprite, "Build drawer must serialize the selected item frame sprite.");

        SerializedObject presenterObject = new SerializedObject(presenter);
        UnitPrefabRegistryAuthoringConfig unitConfig = (UnitPrefabRegistryAuthoringConfig)presenterObject
            .FindProperty("unitPrefabRegistryConfig")
            .objectReferenceValue;
        BuildingPlacementSystemConfig buildingConfig = (BuildingPlacementSystemConfig)presenterObject
            .FindProperty("buildingPlacementConfig")
            .objectReferenceValue;
        Assert.NotNull(unitConfig, "Build drawer presenter must serialize the unit registry config.");
        Assert.NotNull(buildingConfig, "Build drawer presenter must serialize the building placement config.");

        _query.Collect(unitConfig, buildingConfig, BuildDrawerCategory.Vehicles, _results);
        Assert.Greater(_results.Count, 0, "Current project configs should expose at least one requestable vehicle for the drawer.");
        presenter.SelectCategoryForTests(BuildDrawerCategory.Vehicles);

        TMP_Text titleText = (TMP_Text)new SerializedObject(view.ItemTemplate)
            .FindProperty("nameText")
            .objectReferenceValue;
        Assert.NotNull(titleText, "Build drawer item template must serialize its title/name text.");
        Assert.AreEqual(_results[0].DisplayName, titleText.text);

        int activeItemRows = CountActiveCatalogItemRows(view);
        Assert.AreEqual(_results.Count, activeItemRows, "Visible drawer item rows must match the requestable catalog count for the selected category.");
    }

    [Test]
    public void CurrentBuildDrawerPrefabBindsSelectionDetailAndActionLabel()
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(BuildDrawerPrefabPath);
        Assert.NotNull(prefab, "Build drawer popup prefab must exist.");

        GameObject instance = UnityEngine.Object.Instantiate(prefab);
        _createdObjects.Add(instance);

        BuildDrawerView view = instance.GetComponent<BuildDrawerView>();
        BuildDrawerCatalogPresenterView presenter = instance.GetComponent<BuildDrawerCatalogPresenterView>();
        Assert.NotNull(view);
        Assert.NotNull(presenter);

        SerializedObject presenterObject = new SerializedObject(presenter);
        UnitPrefabRegistryAuthoringConfig unitConfig = GetSerializedReference<UnitPrefabRegistryAuthoringConfig>(
            presenterObject,
            "unitPrefabRegistryConfig");
        BuildingPlacementSystemConfig buildingConfig = GetSerializedReference<BuildingPlacementSystemConfig>(
            presenterObject,
            "buildingPlacementConfig");

        _query.Collect(unitConfig, buildingConfig, BuildDrawerCategory.Vehicles, _results);
        Assert.Greater(_results.Count, 0, "Current project configs should expose at least one requestable vehicle for the drawer.");

        presenter.SelectCategoryForTests(BuildDrawerCategory.Vehicles);

        TMP_Text detailNameText = GetSerializedReference<TMP_Text>(new SerializedObject(view), "nameText");
        TMP_Text actionLabelText = GetSerializedReference<TMP_Text>(new SerializedObject(view), "primaryActionLabelText");
        Assert.NotNull(detailNameText, "Build drawer detail panel must serialize the selected item name text.");
        Assert.NotNull(actionLabelText, "Build drawer detail panel must serialize the primary action label text.");
        Assert.AreEqual(_results[0].DisplayName, detailNameText.text);
        Assert.AreEqual("PRODUCE", actionLabelText.text);
        Assert.IsTrue(view.PrimaryActionButton != null && view.PrimaryActionButton.interactable);
        if (view.BuildButton != null && view.OrderButton != null)
        {
            Assert.IsTrue(view.BuildButton.gameObject.activeSelf, "BuildButton is the canonical build drawer CTA when both buttons exist.");
            Assert.IsFalse(view.OrderButton.gameObject.activeSelf, "OrderButton should stay hidden as the duplicate CTA when BuildButton exists.");
        }

        List<BuildDrawerItemView> activeRows = GetActiveCatalogItemRows(view);
        Assert.Greater(activeRows.Count, 0, "Build drawer must show at least one item row after selecting a populated category.");
        Assert.AreSame(view.SelectedItemFrameSprite, activeRows[0].FrameImage.sprite);

        if (activeRows.Count > 1)
        {
            activeRows[1].SelectionButton.onClick.Invoke();
            Assert.AreNotSame(view.SelectedItemFrameSprite, activeRows[0].FrameImage.sprite);
            Assert.AreSame(view.SelectedItemFrameSprite, activeRows[1].FrameImage.sprite);
            Assert.AreEqual(1, CountSelectedRows(activeRows, view.SelectedItemFrameSprite));
            Assert.AreEqual(_results[1].DisplayName, detailNameText.text);
        }
    }

    [Test]
    public void BuildDrawerPrimaryAction_RoutesBuildingPlacementRequestAndClosesDrawer()
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(BuildDrawerPrefabPath);
        Assert.NotNull(prefab);

        GameObject instance = UnityEngine.Object.Instantiate(prefab);
        _createdObjects.Add(instance);

        BuildDrawerView view = instance.GetComponent<BuildDrawerView>();
        BuildDrawerCatalogPresenterView presenter = instance.GetComponent<BuildDrawerCatalogPresenterView>();
        BuildingPlacementSystemConfig buildingConfig = CreateAsset<BuildingPlacementSystemConfig>();
        GameObject building = CreateBuilding("Requestable Barracks", true, BuildingRole.MilitaryCamp, false);
        buildingConfig.Spawnables.Add(building);

        GameObject requestedPrefab = null;
        int requestedPrice = -1;
        bool requestedFocus = true;
        bool closed = false;
        presenter.ConfigureForTests(view, null, buildingConfig);
        presenter.BindRuntimeCommands(
            new BuildingUiCommandSystem(),
            CreateCommandContext((GameObject requestPrefab, int price, out string requiredBuilding, bool focusProducer) =>
            {
                requestedPrefab = requestPrefab;
                requestedPrice = price;
                requestedFocus = focusProducer;
                requiredBuilding = string.Empty;
                return BuildingUiCommandSystem.CampRequestFailure.None;
            }),
            () => closed = true);
        presenter.RefreshForTests();

        view.PrimaryActionButton.onClick.Invoke();

        Assert.AreSame(building, requestedPrefab);
        Assert.AreEqual(1234, requestedPrice);
        Assert.IsFalse(requestedFocus);
        Assert.IsTrue(closed, "Building PLACE should close the drawer after entering placement mode.");
    }

    [Test]
    public void BuildDrawerPrimaryAction_RoutesUnitProductionRequestAndKeepsDrawerOpen()
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(BuildDrawerPrefabPath);
        Assert.NotNull(prefab);

        GameObject instance = UnityEngine.Object.Instantiate(prefab);
        _createdObjects.Add(instance);

        BuildDrawerView view = instance.GetComponent<BuildDrawerView>();
        BuildDrawerCatalogPresenterView presenter = instance.GetComponent<BuildDrawerCatalogPresenterView>();
        UnitPrefabRegistryAuthoringConfig unitConfig = CreateAsset<UnitPrefabRegistryAuthoringConfig>();
        GameObject vehicle = CreateUnit("Requestable Vehicle", true, false, new Vector2Int(2, 2), 0);
        Sprite vehiclePortrait = CreateTestSprite(Color.cyan);
        AssignUnitPortraitSprites(vehicle, vehiclePortrait);
        unitConfig.UnitSpawnPrefabs.Add(vehicle);

        GameObject requestedPrefab = null;
        int requestedPrice = -1;
        bool requestedFocus = true;
        bool closed = false;
        presenter.ConfigureForTests(view, unitConfig, null);
        presenter.BindRuntimeCommands(
            new BuildingUiCommandSystem(),
            CreateCommandContext((GameObject requestPrefab, int price, out string requiredBuilding, bool focusProducer) =>
            {
                requestedPrefab = requestPrefab;
                requestedPrice = price;
                requestedFocus = focusProducer;
                requiredBuilding = string.Empty;
                return BuildingUiCommandSystem.CampRequestFailure.None;
            }),
            () => closed = true);
        presenter.SelectCategoryForTests(BuildDrawerCategory.Vehicles);

        view.PrimaryActionButton.onClick.Invoke();

        Assert.AreSame(vehicle, requestedPrefab);
        Assert.AreEqual(5678, requestedPrice);
        Assert.IsFalse(requestedFocus);
        Assert.IsFalse(closed, "Production/recruitment should keep the drawer open for queue feedback.");
    }

    [Test]
    public void BuildDrawerPrimaryAction_RealBuildingRequestBeginsPlacementAndClosesDrawer()
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(BuildDrawerPrefabPath);
        Assert.NotNull(prefab);

        GameObject instance = UnityEngine.Object.Instantiate(prefab);
        _createdObjects.Add(instance);

        BuildDrawerView view = instance.GetComponent<BuildDrawerView>();
        BuildDrawerCatalogPresenterView presenter = instance.GetComponent<BuildDrawerCatalogPresenterView>();
        BuildingPlacementSystemConfig buildingConfig = CreateAsset<BuildingPlacementSystemConfig>();
        GameObject buildingPrefab = CreateBuilding("Requestable Airport", true, BuildingRole.MilitaryCamp, false);
        buildingConfig.Spawnables.Add(buildingPrefab);

        BuildingDefinition buildingDefinition = new()
        {
            DisplayName = "Requestable Airport",
            Prefab = buildingPrefab
        };
        var requestSystem = new BuildingProductionRequestSystem();
        bool beganPlacement = false;
        int activePlacementCost = -1;
        bool closed = false;
        BuildingProductionRequestSystem.Context requestContext = CreateProductionRequestContext(
            new Dictionary<int, RuntimeBuildingEntity>(),
            requestSystem,
            new BuildingProductionSystem(),
            Array.Empty<GameObject>(),
            new Dictionary<GameObject, BuildingDefinition> { { buildingPrefab, buildingDefinition } },
            _ => { beganPlacement = true; return true; },
            amount => true,
            amount => activePlacementCost = amount);

        presenter.ConfigureForTests(view, null, buildingConfig);
        presenter.BindRuntimeCommands(
            new BuildingUiCommandSystem(),
            CreateRealCommandContext(requestSystem, () => requestContext),
            () => closed = true);
        presenter.RefreshForTests();

        view.PrimaryActionButton.onClick.Invoke();

        Assert.IsTrue(beganPlacement, "Building PLACE must enter configured building placement.");
        Assert.AreEqual(1234, activePlacementCost);
        Assert.IsTrue(closed, "Building PLACE should close the drawer after placement is armed.");
    }

    [Test]
    public void BuildDrawerPrimaryAction_RealUnitRequestQueuesProductionAndRefreshesQueue()
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(BuildDrawerPrefabPath);
        Assert.NotNull(prefab);

        using World world = new("BuildDrawerProductionRequestTest");
        GameObject instance = UnityEngine.Object.Instantiate(prefab);
        _createdObjects.Add(instance);

        BuildDrawerView view = instance.GetComponent<BuildDrawerView>();
        BuildDrawerCatalogPresenterView presenter = instance.GetComponent<BuildDrawerCatalogPresenterView>();
        UnitPrefabRegistryAuthoringConfig unitConfig = CreateAsset<UnitPrefabRegistryAuthoringConfig>();
        GameObject vehicle = CreateUnit("Requestable Vehicle", true, false, new Vector2Int(2, 2), 0);
        Sprite vehiclePortrait = CreateTestSprite(Color.cyan);
        AssignUnitPortraitSprites(vehicle, vehiclePortrait);
        unitConfig.UnitSpawnPrefabs.Add(vehicle);

        RuntimeBuildingEntity producer = CreateRuntimeProducerBuilding(7, "Vehicle Factory", vehicle);
        var runtimeBuildings = new Dictionary<int, RuntimeBuildingEntity> { { producer.Id, producer } };
        var productionSystem = new BuildingProductionSystem();
        var requestSystem = new BuildingProductionRequestSystem();
        int dollars = 100000;
        BuildingProductionRequestSystem.Context requestContext = CreateProductionRequestContext(
            runtimeBuildings,
            requestSystem,
            productionSystem,
            new[] { vehicle },
            new Dictionary<GameObject, BuildingDefinition>(),
            _ => false,
            amount =>
            {
                if (dollars < amount)
                    return false;

                dollars -= amount;
                return true;
            },
            _ => { },
            world.EntityManager);
        BuildingUiQuerySystem.Context queryContext = CreateProductionQueryContext(
            runtimeBuildings,
            requestSystem,
            productionSystem,
            () => requestContext,
            world.EntityManager);

        bool closed = false;
        presenter.ConfigureForTests(view, unitConfig, null);
        presenter.BindRuntimeCommands(
            new BuildingUiCommandSystem(),
            CreateRealCommandContext(requestSystem, () => requestContext),
            () => closed = true);
        presenter.BindRuntimeQueries(new BuildingUiQuerySystem(), queryContext);
        presenter.SelectCategoryForTests(BuildDrawerCategory.Vehicles);

        Assert.AreEqual(0, producer.PendingProductions.Count);
        view.PrimaryActionButton.onClick.Invoke();

        Assert.IsFalse(closed, "Production requests should keep the drawer open.");
        Assert.AreEqual(1, producer.PendingProductions.Count);
        Assert.AreSame(vehicle, producer.PendingProductions[0].Prefab);
        Assert.AreEqual(0, producer.PendingProductions[0].ProductionIndex);
        Assert.AreEqual(100000 - 5678, dollars);
        Assert.IsTrue(view.ActiveItemView.gameObject.activeSelf, "Queue should refresh immediately after production starts.");
        Assert.AreEqual("Requestable Vehicle", GetQueueText(view.ActiveItemView, "nameText"));
        Assert.IsFalse(view.QueuedItemTemplate.gameObject.activeSelf, "One pending production should only show the active queue row.");
        Image activeThumbnail = GetSerializedReference<Image>(new SerializedObject(view.ActiveItemView), "thumbnailImage");
        Assert.NotNull(activeThumbnail, "Active queue item must serialize its thumbnail image.");
        Assert.AreSame(vehiclePortrait, activeThumbnail.sprite);
        Assert.IsTrue(activeThumbnail.enabled);
        Assert.IsFalse(GetSerializedReference<GameObject>(new SerializedObject(view), "noProductionView").activeSelf);
    }

    [Test]
    public void CurrentBuildDrawerPrefabBindsProductionQueueSnapshot()
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(BuildDrawerPrefabPath);
        Assert.NotNull(prefab);

        GameObject instance = UnityEngine.Object.Instantiate(prefab);
        _createdObjects.Add(instance);

        BuildDrawerView view = instance.GetComponent<BuildDrawerView>();
        BuildDrawerCatalogPresenterView presenter = instance.GetComponent<BuildDrawerCatalogPresenterView>();
        Assert.NotNull(view);
        Assert.NotNull(presenter);
        Assert.NotNull(view.ActiveItemView, "Build drawer must serialize the active queue item view.");
        Assert.NotNull(view.QueuedItemTemplate, "Build drawer must serialize the queued item template.");

        UnitPrefabRegistryAuthoringConfig unitConfig = CreateAsset<UnitPrefabRegistryAuthoringConfig>();
        GameObject activePrefab = CreateUnit("Queue Vehicle Active", true, false, new Vector2Int(2, 2), 0);
        GameObject queuedPrefab = CreateUnit("Queue Vehicle Waiting", true, false, new Vector2Int(2, 2), 0);
        GameObject thirdPrefab = CreateUnit("Queue Vehicle Third", true, false, new Vector2Int(2, 2), 0);
        Sprite activePortrait = CreateTestSprite(Color.green);
        Sprite queuedPortrait = CreateTestSprite(Color.yellow);
        Sprite thirdPortrait = CreateTestSprite(Color.cyan);
        AssignUnitPortraitSprites(activePrefab, activePortrait);
        AssignUnitPortraitSprites(queuedPrefab, queuedPortrait);
        AssignUnitPortraitSprites(thirdPrefab, thirdPortrait);
        unitConfig.UnitSpawnPrefabs.Add(activePrefab);
        unitConfig.UnitSpawnPrefabs.Add(queuedPrefab);
        unitConfig.UnitSpawnPrefabs.Add(thirdPrefab);
        presenter.ConfigureForTests(view, unitConfig, null);

        SerializedObject viewObject = new SerializedObject(view);
        GameObject productionPanel = GetSerializedReference<GameObject>(viewObject, "productionPanel");
        GameObject productionPanelActive = GetSerializedReference<GameObject>(viewObject, "productionPanelActive");
        GameObject noProductionView = GetSerializedReference<GameObject>(viewObject, "noProductionView");
        TMP_Text noProductionText = GetSerializedReference<TMP_Text>(viewObject, "noProductionText");
        Assert.NotNull(productionPanel, "Build drawer must serialize the production panel container.");
        Assert.NotNull(productionPanelActive, "Build drawer must serialize the active production panel state.");
        Assert.NotNull(noProductionView, "Build drawer must serialize the empty production panel state.");
        Assert.NotNull(noProductionText, "Build drawer must serialize the empty production label.");

        presenter.ApplyQueueSnapshotForTests(Array.Empty<BuildingUiQuerySystem.PendingProductionUiEntry>());
        Assert.IsFalse(view.ActiveItemView.gameObject.activeSelf);
        Assert.IsFalse(view.QueuedItemTemplate.gameObject.activeSelf);
        Assert.IsTrue(productionPanel.activeSelf, "ProductionPanel should remain visible so the empty state can be shown.");
        Assert.IsFalse(productionPanelActive.activeSelf);
        Assert.IsTrue(noProductionView.activeSelf);
        Assert.AreEqual("NO PRODUCTION QUEUED", noProductionText.text);

        List<BuildingUiQuerySystem.PendingProductionUiEntry> entries = new()
        {
            new BuildingUiQuerySystem.PendingProductionUiEntry(7, activePrefab, 74f, 100f, 0.26f, 0f, 100f, "Factory"),
            new BuildingUiQuerySystem.PendingProductionUiEntry(7, queuedPrefab, 12f, 24f, 0.5f, 0f, 24f, "Factory"),
            new BuildingUiQuerySystem.PendingProductionUiEntry(7, thirdPrefab, 8f, 20f, 0.6f, 0f, 20f, "Factory")
        };
        presenter.ApplyQueueSnapshotForTests(entries);

        Assert.IsTrue(view.ActiveItemView.gameObject.activeSelf);
        Assert.IsTrue(view.QueuedItemTemplate.gameObject.activeSelf);
        Assert.IsTrue(productionPanel.activeSelf);
        Assert.IsTrue(productionPanelActive.activeSelf);
        Assert.IsFalse(noProductionView.activeSelf);
        Assert.AreEqual("Queue Vehicle Active", GetQueueText(view.ActiveItemView, "nameText"));
        Assert.AreEqual("1", GetQueueText(view.ActiveItemView, "numberText"));
        Assert.AreEqual("26%", GetQueueText(view.ActiveItemView, "percentageText"));
        Assert.AreEqual("Queue Vehicle Waiting", GetQueueText(view.QueuedItemTemplate, "nameText"));
        Assert.AreEqual("2", GetQueueText(view.QueuedItemTemplate, "numberText"));
        Image activeThumbnail = GetSerializedReference<Image>(new SerializedObject(view.ActiveItemView), "thumbnailImage");
        Image queuedThumbnail = GetSerializedReference<Image>(new SerializedObject(view.QueuedItemTemplate), "thumbnailImage");
        Assert.NotNull(activeThumbnail);
        Assert.NotNull(queuedThumbnail);
        Assert.AreSame(activePortrait, activeThumbnail.sprite);
        Assert.AreSame(queuedPortrait, queuedThumbnail.sprite);
        List<BuildDrawerQueueItemView> activeQueueItems = GetActiveQueueItemViews(view);
        Assert.AreEqual(3, activeQueueItems.Count);
        Assert.AreEqual("Queue Vehicle Third", GetQueueText(activeQueueItems[2], "nameText"));
        Assert.AreEqual("3", GetQueueText(activeQueueItems[2], "numberText"));
        Image thirdThumbnail = GetSerializedReference<Image>(new SerializedObject(activeQueueItems[2]), "thumbnailImage");
        Assert.NotNull(thirdThumbnail);
        Assert.AreSame(thirdPortrait, thirdThumbnail.sprite);

        Assert.AreEqual("26%", GetSerializedReference<TMP_Text>(viewObject, "queuePercentageText").text);
        Assert.AreEqual("01:14", GetSerializedReference<TMP_Text>(viewObject, "queueTimeText").text);
        Assert.AreEqual("3", GetSerializedReference<TMP_Text>(viewObject, "queueNumbersText").text);
        Assert.IsFalse(view.RushButton == null || view.RushButton.interactable);
        Assert.IsFalse(view.ClearButton == null || view.ClearButton.interactable);
    }

    [Test]
    public void CurrentBuildDrawerPrefabShowsSingleActiveProductionForOneQueueEntry()
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(BuildDrawerPrefabPath);
        Assert.NotNull(prefab);

        GameObject instance = UnityEngine.Object.Instantiate(prefab);
        _createdObjects.Add(instance);

        BuildDrawerView view = instance.GetComponent<BuildDrawerView>();
        BuildDrawerCatalogPresenterView presenter = instance.GetComponent<BuildDrawerCatalogPresenterView>();
        Assert.NotNull(view);
        Assert.NotNull(presenter);

        UnitPrefabRegistryAuthoringConfig unitConfig = CreateAsset<UnitPrefabRegistryAuthoringConfig>();
        GameObject activePrefab = CreateUnit("Queue Vehicle Active", true, false, new Vector2Int(2, 2), 0);
        Sprite activePortrait = CreateTestSprite(Color.magenta);
        AssignUnitPortraitSprites(activePrefab, activePortrait);
        unitConfig.UnitSpawnPrefabs.Add(activePrefab);
        presenter.ConfigureForTests(view, unitConfig, null);

        presenter.ApplyQueueSnapshotForTests(new[]
        {
            new BuildingUiQuerySystem.PendingProductionUiEntry(7, activePrefab, 74f, 100f, 0.26f, 0f, 100f, "Factory")
        });

        Assert.IsTrue(view.ActiveItemView.gameObject.activeSelf);
        Assert.IsFalse(view.QueuedItemTemplate.gameObject.activeSelf);
        Assert.AreEqual("Queue Vehicle Active", GetQueueText(view.ActiveItemView, "nameText"));
        Assert.AreEqual("26%", GetQueueText(view.ActiveItemView, "percentageText"));

        Image activeThumbnail = GetSerializedReference<Image>(new SerializedObject(view.ActiveItemView), "thumbnailImage");
        Assert.NotNull(activeThumbnail, "Active queue item must serialize its thumbnail image.");
        Assert.AreSame(activePortrait, activeThumbnail.sprite);
        Assert.IsTrue(activeThumbnail.enabled);

        RectTransform queueRoot = view.QueueContentRoot;
        Assert.NotNull(queueRoot);
        for (int i = 0; i < queueRoot.childCount; i++)
        {
            Transform child = queueRoot.GetChild(i);
            if (child != view.ActiveItemView.transform && child != view.QueuedItemTemplate.transform)
                Assert.IsFalse(child.gameObject.activeSelf, $"Static queue placeholder '{child.name}' must not appear as an extra production row.");
        }
    }

    [Test]
    public void BuildDrawerCancelButton_RoutesActiveProductionCancelRequest()
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(BuildDrawerPrefabPath);
        Assert.NotNull(prefab);

        GameObject instance = UnityEngine.Object.Instantiate(prefab);
        _createdObjects.Add(instance);

        BuildDrawerView view = instance.GetComponent<BuildDrawerView>();
        BuildDrawerCatalogPresenterView presenter = instance.GetComponent<BuildDrawerCatalogPresenterView>();
        Assert.NotNull(view);
        Assert.NotNull(presenter);
        Assert.NotNull(view.CancelButton, "Build drawer must serialize the production cancel button.");

        UnitPrefabRegistryAuthoringConfig unitConfig = CreateAsset<UnitPrefabRegistryAuthoringConfig>();
        GameObject activePrefab = CreateUnit("Queue Vehicle Active", true, false, new Vector2Int(2, 2), 0);
        unitConfig.UnitSpawnPrefabs.Add(activePrefab);

        int cancelledBuildingId = -1;
        int cancelledPendingIndex = -1;
        presenter.ConfigureForTests(view, unitConfig, null);
        presenter.BindRuntimeCommands(
            new BuildingUiCommandSystem(),
            CreateCommandContext(
                (GameObject requestPrefab, int price, out string requiredBuilding, bool focusProducer) =>
                {
                    requiredBuilding = string.Empty;
                    return BuildingUiCommandSystem.CampRequestFailure.InvalidSelection;
                },
                (buildingId, pendingIndex) =>
                {
                    cancelledBuildingId = buildingId;
                    cancelledPendingIndex = pendingIndex;
                    return true;
                }),
            null);

        List<BuildingUiQuerySystem.PendingProductionUiEntry> entries = new()
        {
            new BuildingUiQuerySystem.PendingProductionUiEntry(7, 3, activePrefab, 74f, 100f, 0.26f, 0f, 100f, "Factory")
        };
        presenter.ApplyQueueSnapshotForTests(entries);
        Assert.IsTrue(view.CancelButton.interactable);

        view.CancelButton.onClick.Invoke();

        Assert.AreEqual(7, cancelledBuildingId);
        Assert.AreEqual(3, cancelledPendingIndex);
    }

    [Test]
    public void BuildDrawerItemSelection_DoesNotMutateFirstCardThumbnail()
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(BuildDrawerPrefabPath);
        Assert.NotNull(prefab);

        GameObject instance = UnityEngine.Object.Instantiate(prefab);
        _createdObjects.Add(instance);

        BuildDrawerView view = instance.GetComponent<BuildDrawerView>();
        BuildDrawerCatalogPresenterView presenter = instance.GetComponent<BuildDrawerCatalogPresenterView>();
        Assert.NotNull(view);
        Assert.NotNull(presenter);

        Image detailThumbnail = GetSerializedReference<Image>(new SerializedObject(view), "thumbnailImage");
        Assert.AreNotSame(view.ItemTemplate.ThumbnailImage, detailThumbnail, "Detail thumbnail must not share the first catalog card thumbnail image.");

        BuildingPlacementSystemConfig buildingConfig = CreateAsset<BuildingPlacementSystemConfig>();
        Sprite airportSprite = CreateTestSprite(Color.blue);
        Sprite barracksSprite = CreateTestSprite(Color.red);
        GameObject airport = CreateBuilding("Airport", true, BuildingRole.MilitaryCamp, false);
        GameObject barracks = CreateBuilding("Barracks", true, BuildingRole.MilitaryCamp, false);
        AssignBuildingPortraitSprites(airport, airportSprite);
        AssignBuildingPortraitSprites(barracks, barracksSprite);
        buildingConfig.Spawnables.Add(airport);
        buildingConfig.Spawnables.Add(barracks);

        presenter.ConfigureForTests(view, null, buildingConfig);
        presenter.RefreshForTests();

        List<BuildDrawerItemView> activeRows = GetActiveCatalogItemRows(view);
        Assert.GreaterOrEqual(activeRows.Count, 2);
        Assert.AreSame(airportSprite, activeRows[0].ThumbnailImage.sprite);
        Assert.AreSame(barracksSprite, activeRows[1].ThumbnailImage.sprite);

        activeRows[1].SelectionButton.onClick.Invoke();

        Assert.AreSame(airportSprite, activeRows[0].ThumbnailImage.sprite, "Selecting another building must not mutate the first card thumbnail.");
        Assert.AreSame(barracksSprite, activeRows[1].ThumbnailImage.sprite);
    }

    [Test]
    public void BuildDrawerPopup_BlocksGameplayAndPlacementPointerInput()
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(BuildDrawerPrefabPath);
        Assert.NotNull(prefab);

        GameObject canvasObject = new GameObject("Test Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        _createdObjects.Add(canvasObject);
        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        RectTransform canvasRect = canvasObject.GetComponent<RectTransform>();
        canvasRect.sizeDelta = new Vector2(1920f, 1080f);

        GameObject instance = UnityEngine.Object.Instantiate(prefab, canvasRect, false);
        _createdObjects.Add(instance);
        RectTransform instanceRect = instance.transform as RectTransform;
        Assert.NotNull(instanceRect);
        instanceRect.anchorMin = Vector2.zero;
        instanceRect.anchorMax = Vector2.one;
        instanceRect.offsetMin = Vector2.zero;
        instanceRect.offsetMax = Vector2.zero;

        BuildDrawerView view = instance.GetComponent<BuildDrawerView>();
        Assert.NotNull(view);

        GameObject drawerRoot = GetSerializedReference<GameObject>(new SerializedObject(view), "drawerRoot");
        Assert.NotNull(drawerRoot, "Build drawer must serialize the root rect used to block world input.");
        RectTransform drawerRect = drawerRoot.transform as RectTransform;
        Assert.NotNull(drawerRect);

        Vector2 drawerCenter = RectTransformUtility.WorldToScreenPoint(
            null,
            drawerRect.TransformPoint(drawerRect.rect.center));
        Assert.IsTrue(view.ContainsScreenPoint(drawerCenter), "Drawer view must contain its own center point.");

        MainMenuPlayUI mainMenu = new MainMenuPlayUI();
        mainMenu.BindBuildDrawer(view);
        Assert.IsTrue(mainMenu.IsPointerOverAnyGameplayUi(drawerCenter, out string source));
        Assert.AreEqual("BuildDrawer", source);
        Assert.IsTrue(mainMenu.IsPointerOverPlacementUi(drawerCenter), "Build drawer must block placement input as well as selection input.");

        mainMenu.BindBuildDrawer(null);
        Assert.IsFalse(mainMenu.IsPointerOverAnyGameplayUi(drawerCenter, out _));
    }

    [Test]
    public void BuildDrawerPopup_ReportsOpenStateForProductionCameraFocusGate()
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(BuildDrawerPrefabPath);
        Assert.NotNull(prefab);

        GameObject instance = UnityEngine.Object.Instantiate(prefab);
        _createdObjects.Add(instance);

        BuildDrawerView view = instance.GetComponent<BuildDrawerView>();
        Assert.NotNull(view);

        MainMenuPlayUI mainMenu = new MainMenuPlayUI();
        Assert.IsFalse(mainMenu.IsBuildDrawerOpen);

        mainMenu.BindBuildDrawer(view);
        Assert.IsTrue(mainMenu.IsBuildDrawerOpen);

        GameObject drawerRoot = GetSerializedReference<GameObject>(new SerializedObject(view), "drawerRoot");
        Assert.NotNull(drawerRoot, "Build drawer open state must be based on the serialized drawer root.");

        drawerRoot.SetActive(false);
        Assert.IsFalse(mainMenu.IsBuildDrawerOpen);

        drawerRoot.SetActive(true);
        Assert.IsTrue(mainMenu.IsBuildDrawerOpen);

        mainMenu.BindBuildDrawer(null);
        Assert.IsFalse(mainMenu.IsBuildDrawerOpen);
    }

    [Test]
    public void BuildDrawerPrimaryActionButton_ReceivesPointerRaycastAtCenter()
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(BuildDrawerPrefabPath);
        Assert.NotNull(prefab);

        GameObject eventSystemObject = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
        _createdObjects.Add(eventSystemObject);
        EventSystem eventSystem = eventSystemObject.GetComponent<EventSystem>();
        GameObject canvasObject = new GameObject("Test Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        _createdObjects.Add(canvasObject);
        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        RectTransform canvasRect = canvasObject.GetComponent<RectTransform>();
        canvasRect.sizeDelta = new Vector2(1920f, 1080f);

        GameObject instance = UnityEngine.Object.Instantiate(prefab, canvasRect, false);
        _createdObjects.Add(instance);
        RectTransform instanceRect = instance.transform as RectTransform;
        Assert.NotNull(instanceRect);
        instanceRect.anchorMin = Vector2.zero;
        instanceRect.anchorMax = Vector2.one;
        instanceRect.offsetMin = Vector2.zero;
        instanceRect.offsetMax = Vector2.zero;

        BuildDrawerView view = instance.GetComponent<BuildDrawerView>();
        BuildDrawerCatalogPresenterView presenter = instance.GetComponent<BuildDrawerCatalogPresenterView>();
        Assert.NotNull(view);
        Assert.NotNull(presenter);

        BuildingPlacementSystemConfig buildingConfig = CreateAsset<BuildingPlacementSystemConfig>();
        buildingConfig.Spawnables.Add(CreateBuilding("Requestable Barracks", true, BuildingRole.MilitaryCamp, false));
        presenter.ConfigureForTests(view, null, buildingConfig);
        presenter.RefreshForTests();
        Canvas.ForceUpdateCanvases();

        Button button = view.PrimaryActionButton;
        Assert.NotNull(button);
        Assert.IsTrue(button.gameObject.activeInHierarchy);
        Assert.IsTrue(button.interactable);
        Assert.NotNull(button.targetGraphic);
        Assert.IsTrue(button.targetGraphic.raycastTarget, "Primary build drawer button target graphic must receive raycasts across the full CTA frame.");

        Vector2 center = RectTransformUtility.WorldToScreenPoint(
            null,
            button.targetGraphic.rectTransform.TransformPoint(button.targetGraphic.rectTransform.rect.center));
        Assert.IsTrue(
            RectTransformUtility.RectangleContainsScreenPoint(button.targetGraphic.rectTransform, center, null),
            "Primary build drawer button target graphic must contain its own center point.");

        var pointerEvent = new PointerEventData(eventSystem)
        {
            position = center,
            button = PointerEventData.InputButton.Left
        };
        bool clicked = false;
        button.onClick.AddListener(() => clicked = true);
        bool handled = ExecuteEvents.ExecuteHierarchy(
            button.targetGraphic.gameObject,
            pointerEvent,
            ExecuteEvents.pointerClickHandler);

        Assert.IsTrue(handled, "Primary build drawer button center raycast must dispatch to a Button.");
        Assert.IsTrue(clicked, "Primary build drawer button center click must invoke the Button onClick.");
    }

    private T CreateAsset<T>() where T : ScriptableObject
    {
        T asset = ScriptableObject.CreateInstance<T>();
        _createdObjects.Add(asset);
        return asset;
    }

    private GameObject CreateBuilding(string displayName, bool canRequest, BuildingRole role, bool isWall)
    {
        GameObject prefab = new GameObject(displayName);
        _createdObjects.Add(prefab);
        BuildingDefinitionAuthoring authoring = prefab.AddComponent<BuildingDefinitionAuthoring>();
        SerializedObject serialized = new SerializedObject(authoring);
        serialized.FindProperty("displayName").stringValue = displayName;
        serialized.FindProperty("description").stringValue = $"{displayName} description";
        serialized.FindProperty("canRequest").boolValue = canRequest;
        serialized.FindProperty("role").enumValueIndex = (int)role;
        serialized.FindProperty("isWall").boolValue = isWall;
        serialized.FindProperty("price").intValue = 1234;
        serialized.FindProperty("footprintCells").vector2IntValue = new Vector2Int(2, 2);
        serialized.ApplyModifiedPropertiesWithoutUndo();
        return prefab;
    }

    private void AssignBuildingPortraitSprites(GameObject prefab, Sprite sprite)
    {
        BuildingDefinitionAuthoring authoring = prefab.GetComponent<BuildingDefinitionAuthoring>();
        SerializedObject serialized = new SerializedObject(authoring);
        serialized.FindProperty("portraitSprite").objectReferenceValue = sprite;
        serialized.FindProperty("portraitCardSprite").objectReferenceValue = sprite;
        serialized.FindProperty("portraitActionSprite").objectReferenceValue = sprite;
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private Sprite CreateTestSprite(Color color)
    {
        Texture2D texture = new Texture2D(2, 2);
        texture.SetPixels(new[] { color, color, color, color });
        texture.Apply();
        _createdObjects.Add(texture);

        Sprite sprite = Sprite.Create(texture, new Rect(0, 0, 2, 2), new Vector2(0.5f, 0.5f));
        _createdObjects.Add(sprite);
        return sprite;
    }

    private GameObject CreateUnit(string displayName, bool canRequest, bool isAir, Vector2Int footprint, int transportCapacity)
    {
        GameObject prefab = new GameObject(displayName);
        _createdObjects.Add(prefab);
        UnitGridAuthoring authoring = prefab.AddComponent<UnitGridAuthoring>();
        SerializedObject serialized = new SerializedObject(authoring);
        serialized.FindProperty("displayName").stringValue = displayName;
        serialized.FindProperty("description").stringValue = $"{displayName} description";
        serialized.FindProperty("canRequest").boolValue = canRequest;
        serialized.FindProperty("isAirUnit").boolValue = isAir;
        serialized.FindProperty("footprintCells").vector2IntValue = footprint;
        serialized.FindProperty("soldierTransportCapacity").intValue = transportCapacity;
        serialized.FindProperty("price").intValue = 5678;
        serialized.FindProperty("productionDurationSeconds").floatValue = 42f;
        serialized.ApplyModifiedPropertiesWithoutUndo();
        return prefab;
    }

    private void AssignUnitPortraitSprites(GameObject prefab, Sprite sprite)
    {
        UnitGridAuthoring authoring = prefab.GetComponent<UnitGridAuthoring>();
        SerializedObject serialized = new SerializedObject(authoring);
        serialized.FindProperty("portraitSprite").objectReferenceValue = sprite;
        serialized.FindProperty("portraitCardSprite").objectReferenceValue = sprite;
        serialized.FindProperty("portraitActionSprite").objectReferenceValue = sprite;
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private static RuntimeBuildingEntity CreateRuntimeProducerBuilding(int id, string displayName, GameObject producedPrefab)
    {
        return new RuntimeBuildingEntity
        {
            Id = id,
            Definition = new BuildingDefinition
            {
                DisplayName = displayName,
                ProductionSlots = new List<BuildingDefinition.ProductionSlotDefinition>
                {
                    new() { SpawnUnitPrefab = producedPrefab }
                }
            },
            ProducedUnits = new List<Entity>(),
            ProducedUnitPrefabs = new Dictionary<Entity, GameObject>(),
            PendingProductions = new List<RuntimeBuildingEntity.PendingProduction>()
        };
    }

    private static BuildingProductionRequestSystem.Context CreateProductionRequestContext(
        IReadOnlyDictionary<int, RuntimeBuildingEntity> runtimeBuildings,
        BuildingProductionRequestSystem requestSystem,
        BuildingProductionSystem productionSystem,
        IReadOnlyList<GameObject> unitPrefabs,
        IReadOnlyDictionary<GameObject, BuildingDefinition> configuredDefinitionsByPrefab,
        BuildingProductionRequestSystem.BeginPlacementForConfiguredSpawnableDelegate beginPlacement,
        BuildingProductionRequestSystem.TrySpendDollarsDelegate trySpendDollars,
        BuildingProductionRequestSystem.SetActivePlacementCostDelegate setActivePlacementCost,
        EntityManager entityManager = default)
    {
        IReadOnlyDictionary<string, GameObject> unitPrefabsByKey = new Dictionary<string, GameObject>();
        BuildingProductionSystem.QueueContext queueContext = new(
            unitPrefabs,
            unitPrefabsByKey,
            new BuildingProductionSlotSystem(),
            null,
            null);
        var configuredDefinitions = new List<BuildingDefinition>();
        if (configuredDefinitionsByPrefab != null)
        {
            foreach (KeyValuePair<GameObject, BuildingDefinition> pair in configuredDefinitionsByPrefab)
            {
                if (pair.Value != null)
                    configuredDefinitions.Add(pair.Value);
            }
        }

        return new BuildingProductionRequestSystem.Context(
            runtimeBuildings,
            configuredDefinitions,
            configuredDefinitionsByPrefab,
            unitPrefabs,
            unitPrefabsByKey,
            100000,
            productionSystem,
            queueContext,
            null,
            BuildingDefinitionSystem.GetProductionPrefab,
            null,
            beginPlacement,
            trySpendDollars,
            _ => { },
            setActivePlacementCost,
            (building, productionIndex, spawnUnitPrefab) => productionSystem.TryQueuePlayerUnitFromBuilding(
                queueContext,
                building,
                productionIndex,
                spawnUnitPrefab,
                entityManager,
                10f),
            _ => { },
            () => { },
            () => { },
            () => { },
            _ => { },
            building => building?.Instance != null ? building.Instance.transform.position : Vector3.zero,
            _ => { },
            Debug.LogWarning,
            (_, _) => 0,
            (_, _) => 0);
    }

    private static BuildingUiCommandSystem.Context CreateRealCommandContext(
        BuildingProductionRequestSystem requestSystem,
        Func<BuildingProductionRequestSystem.Context> createRequestContext)
    {
        return new BuildingUiCommandSystem.Context(
            () => 100000,
            () => 0,
            null,
            () => 0,
            null,
            null,
            (GameObject requestPrefab, int price, out string requiredBuilding) => requestSystem.GetCampRequestFailure(
                createRequestContext(),
                requestPrefab,
                price,
                out requiredBuilding),
            (GameObject requestPrefab, int price, out string requiredBuilding, bool focusProducer) => requestSystem.TryRequestCampItem(
                createRequestContext(),
                requestPrefab,
                price,
                focusProducer,
                100,
                out requiredBuilding),
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
    }

    private static BuildingUiQuerySystem.Context CreateProductionQueryContext(
        IReadOnlyDictionary<int, RuntimeBuildingEntity> runtimeBuildings,
        BuildingProductionRequestSystem requestSystem,
        BuildingProductionSystem productionSystem,
        Func<BuildingProductionRequestSystem.Context> createRequestContext,
        EntityManager entityManager)
    {
        return new BuildingUiQuerySystem.Context(
            runtimeBuildings,
            () => null,
            (out EntityManager em) =>
            {
                em = entityManager;
                return true;
            },
            productionSystem,
            () => 10f,
            () => false,
            () => false,
            () => string.Empty,
            () => string.Empty,
            () => string.Empty,
            () => string.Empty,
            (out int current, out int max) =>
            {
                current = 0;
                max = 0;
                return false;
            },
            (out GameObject prefab) =>
            {
                prefab = null;
                return false;
            },
            requestSystem,
            createRequestContext,
            _ => false,
            _ => false,
            (int buildingId, out byte ownerFactionId) =>
            {
                ownerFactionId = 0;
                return false;
            },
            _ => false,
            (Entity unit, out GameObject prefab) =>
            {
                prefab = null;
                return false;
            });
    }

    private static int CountActiveCatalogItemRows(BuildDrawerView view)
    {
        if (view == null || view.ItemContentRoot == null)
            return 0;

        int count = 0;
        for (int i = 0; i < view.ItemContentRoot.childCount; i++)
        {
            Transform child = view.ItemContentRoot.GetChild(i);
            if (!child.gameObject.activeSelf)
                continue;

            if (child.GetComponent<BuildDrawerItemView>() != null)
                count++;
        }

        return count;
    }

    private static List<BuildDrawerItemView> GetActiveCatalogItemRows(BuildDrawerView view)
    {
        List<BuildDrawerItemView> rows = new();
        if (view == null || view.ItemContentRoot == null)
            return rows;

        for (int i = 0; i < view.ItemContentRoot.childCount; i++)
        {
            Transform child = view.ItemContentRoot.GetChild(i);
            if (!child.gameObject.activeSelf)
                continue;

            BuildDrawerItemView itemView = child.GetComponent<BuildDrawerItemView>();
            if (itemView != null)
                rows.Add(itemView);
        }

        return rows;
    }

    private static int CountSelectedRows(IReadOnlyList<BuildDrawerItemView> rows, Sprite selectedFrameSprite)
    {
        int count = 0;
        for (int i = 0; i < rows.Count; i++)
        {
            BuildDrawerItemView row = rows[i];
            if (row != null && row.FrameImage != null && row.FrameImage.sprite == selectedFrameSprite)
                count++;
        }

        return count;
    }

    private static BuildingUiCommandSystem.Context CreateCommandContext(
        BuildingUiCommandSystem.TryRequestCampItemDelegate tryRequestCampItem,
        BuildingUiCommandSystem.CancelProductionDelegate cancelProduction = null)
    {
        return new BuildingUiCommandSystem.Context(
            () => 100000,
            () => 0,
            null,
            () => 0,
            null,
            null,
            null,
            tryRequestCampItem,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            cancelProduction,
            null,
            null);
    }

    private static T GetSerializedReference<T>(SerializedObject serializedObject, string propertyName)
        where T : UnityEngine.Object
    {
        return (T)serializedObject.FindProperty(propertyName).objectReferenceValue;
    }

    private static string GetQueueText(BuildDrawerQueueItemView view, string propertyName)
    {
        TMP_Text text = GetSerializedReference<TMP_Text>(new SerializedObject(view), propertyName);
        return text != null ? text.text : string.Empty;
    }

    private static List<BuildDrawerQueueItemView> GetActiveQueueItemViews(BuildDrawerView view)
    {
        var results = new List<BuildDrawerQueueItemView>();
        RectTransform root = view.QueueContentRoot;
        if (root == null)
            return results;

        for (int i = 0; i < root.childCount; i++)
        {
            BuildDrawerQueueItemView item = root.GetChild(i).GetComponent<BuildDrawerQueueItemView>();
            if (item != null && item.gameObject.activeSelf)
                results.Add(item);
        }

        return results;
    }
}
