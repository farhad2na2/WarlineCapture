using System.Collections.Generic;
using System;
using NUnit.Framework;
using UnityEditor;
using TMPro;
using UnityEngine;
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
                nameof(CurrentBuildDrawerPrefabBindsProductionQueueSnapshot),
                test => test.CurrentBuildDrawerPrefabBindsProductionQueueSnapshot(),
                ref passed);
            RunValidationStep(
                nameof(BuildDrawerCancelButton_RoutesActiveProductionCancelRequest),
                test => test.BuildDrawerCancelButton_RoutesActiveProductionCancelRequest(),
                ref passed);
            RunValidationStep(
                nameof(BuildDrawerItemSelection_DoesNotMutateFirstCardThumbnail),
                test => test.BuildDrawerItemSelection_DoesNotMutateFirstCardThumbnail(),
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
        unitConfig.UnitSpawnPrefabs.Add(activePrefab);
        unitConfig.UnitSpawnPrefabs.Add(queuedPrefab);
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
            new BuildingUiQuerySystem.PendingProductionUiEntry(7, queuedPrefab, 12f, 24f, 0.5f, 0f, 24f, "Factory")
        };
        presenter.ApplyQueueSnapshotForTests(entries);

        Assert.IsTrue(view.ActiveItemView.gameObject.activeSelf);
        Assert.IsTrue(view.QueuedItemTemplate.gameObject.activeSelf);
        Assert.IsTrue(productionPanel.activeSelf);
        Assert.IsTrue(productionPanelActive.activeSelf);
        Assert.IsFalse(noProductionView.activeSelf);
        Assert.AreEqual("Queue Vehicle Active", GetQueueText(view.ActiveItemView, "nameText"));
        Assert.AreEqual("26%", GetQueueText(view.ActiveItemView, "percentageText"));
        Assert.AreEqual("Queue Vehicle Waiting", GetQueueText(view.QueuedItemTemplate, "nameText"));

        Assert.AreEqual("26%", GetSerializedReference<TMP_Text>(viewObject, "queuePercentageText").text);
        Assert.AreEqual("01:14", GetSerializedReference<TMP_Text>(viewObject, "queueTimeText").text);
        Assert.AreEqual("2", GetSerializedReference<TMP_Text>(viewObject, "queueNumbersText").text);
        Assert.IsFalse(view.RushButton == null || view.RushButton.interactable);
        Assert.IsFalse(view.ClearButton == null || view.ClearButton.interactable);
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
}
