using System.Collections.Generic;
using System;
using NUnit.Framework;
using Unity.Entities;
using UnityEditor;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Game.UI.Contracts;
using Game.Configs;
using Game.Authoring;
using Game.UI.Runtime;
using Game.Runtime;
using Game.Composition;

public sealed class BuildDrawerCatalogQueryUiSystemHelperTests
{
    private const string BuildDrawerPrefabPath = "Assets/Game/Prefabs/UI/Shell/Popups/SCN09_BuildDrawerPopup.prefab";

    private readonly List<UnityEngine.Object> _createdObjects = new();
    private readonly List<BuildDrawerCatalogItem> _results = new();
    private readonly BuildDrawerCatalogQueryUiSystemHelper _query = new();

    public BuildDrawerCatalogQueryUiSystemHelperTests()
    {
        ConfigureCatalogMetadataResolvers(_query);
    }

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
                nameof(CurrentBuildDrawerPrefabRefreshesCatalogAfterRuntimeMetadataBinding),
                test => test.CurrentBuildDrawerPrefabRefreshesCatalogAfterRuntimeMetadataBinding(),
                ref passed);
            RunValidationStep(
                nameof(CurrentBuildDrawerPrefabBindsSelectionDetailAndActionLabel),
                test => test.CurrentBuildDrawerPrefabBindsSelectionDetailAndActionLabel(),
                ref passed);
            RunValidationStep(
                nameof(BuildDrawerItemSelection_UsesModelDrivenFrameState),
                test => test.BuildDrawerItemSelection_UsesModelDrivenFrameState(),
                ref passed);
            RunValidationStep(
                nameof(CurrentBuildDrawerPrefabBindsInstructionStripAndIcons),
                test => test.CurrentBuildDrawerPrefabBindsInstructionStripAndIcons(),
                ref passed);
            RunValidationStep(
                nameof(BuildDrawerInstruction_ShowsMissingProducerRequirement),
                test => test.BuildDrawerInstruction_ShowsMissingProducerRequirement(),
                ref passed);
            RunValidationStep(
                nameof(BuildDrawerInstruction_ShowsInsufficientCredits),
                test => test.BuildDrawerInstruction_ShowsInsufficientCredits(),
                ref passed);
            RunValidationStep(
                nameof(BuildDrawerInstruction_ShowsProductionQueueFull),
                test => test.BuildDrawerInstruction_ShowsProductionQueueFull(),
                ref passed);
            RunValidationStep(
                nameof(BuildDrawerInstruction_ShowsGlobalProductionQueueFull),
                test => test.BuildDrawerInstruction_ShowsGlobalProductionQueueFull(),
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
                nameof(BuildDrawerClearButton_RoutesAllProductionCancelRequests),
                test => test.BuildDrawerClearButton_RoutesAllProductionCancelRequests(),
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
            ValidationExit.Passed();
        }
        catch (Exception exception)
        {
            Debug.LogError($"[BuildDrawerCatalogQueryValidation] result=Failed passed={passed}\n{exception}");
            ValidationExit.Failed();
        }
    }

    public static void RunProductionQueueSnapshotValidation()
    {
        int passed = 0;
        try
        {
            RunValidationStep(
                nameof(CurrentBuildDrawerPrefabBindsProductionQueueSnapshot),
                test => test.CurrentBuildDrawerPrefabBindsProductionQueueSnapshot(),
                ref passed);
            Debug.Log("[BuildDrawerProductionQueueSnapshotValidation] result=Passed tests=1");
            ValidationExit.Passed();
        }
        catch (Exception exception)
        {
            Debug.LogError($"[BuildDrawerProductionQueueSnapshotValidation] result=Failed passed={passed}\n{exception}");
            ValidationExit.Failed();
        }
    }

    private static void RunValidationStep(
        string name,
        Action<BuildDrawerCatalogQueryUiSystemHelperTests> action,
        ref int passed)
    {
        var tests = new BuildDrawerCatalogQueryUiSystemHelperTests();
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
        BuildDrawerCatalogRuntimeView presenter = instance.GetComponent<BuildDrawerCatalogRuntimeView>();
        Assert.NotNull(view, "Build drawer popup must serialize BuildDrawerView on the root.");
        Assert.NotNull(presenter, "Build drawer popup must serialize BuildDrawerCatalogRuntimeView on the root.");
        ConfigureCatalogMetadataResolvers(presenter);
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
    public void CurrentBuildDrawerPrefabRefreshesCatalogAfterRuntimeMetadataBinding()
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(BuildDrawerPrefabPath);
        Assert.NotNull(prefab, "Build drawer popup prefab must exist.");

        GameObject instance = UnityEngine.Object.Instantiate(prefab);
        _createdObjects.Add(instance);

        BuildDrawerView view = instance.GetComponent<BuildDrawerView>();
        BuildDrawerCatalogRuntimeView presenter = instance.GetComponent<BuildDrawerCatalogRuntimeView>();
        Assert.NotNull(view);
        Assert.NotNull(presenter);

        SerializedObject presenterObject = new SerializedObject(presenter);
        BuildingPlacementSystemConfig buildingConfig = GetSerializedReference<BuildingPlacementSystemConfig>(
            presenterObject,
            "buildingPlacementConfig");
        Assert.NotNull(buildingConfig, "Build drawer presenter must serialize the building placement config.");

        _query.Collect(null, buildingConfig, BuildDrawerCategory.Buildings, _results);
        Assert.Greater(_results.Count, 0, "Current project configs should expose at least one requestable building for the drawer.");
        presenter.RefreshForTests();
        Assert.AreEqual(0, CountActiveCatalogItemRows(view), "The active prefab instance starts before live metadata resolvers are bound.");

        ConfigureCatalogMetadataResolvers(presenter);

        Assert.AreEqual(
            _results.Count,
            CountActiveCatalogItemRows(view),
            "Binding runtime metadata after popup instantiation must refresh visible drawer catalog rows.");
    }

    [Test]
    public void CurrentBuildDrawerPrefabBindsSelectionDetailAndActionLabel()
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(BuildDrawerPrefabPath);
        Assert.NotNull(prefab, "Build drawer popup prefab must exist.");

        GameObject instance = UnityEngine.Object.Instantiate(prefab);
        _createdObjects.Add(instance);

        BuildDrawerView view = instance.GetComponent<BuildDrawerView>();
        BuildDrawerCatalogRuntimeView presenter = instance.GetComponent<BuildDrawerCatalogRuntimeView>();
        Assert.NotNull(view);
        Assert.NotNull(presenter);
        ConfigureCatalogMetadataResolvers(presenter);

        SerializedObject presenterObject = new SerializedObject(presenter);
        UnitPrefabRegistryAuthoringConfig unitConfig = GetSerializedReference<UnitPrefabRegistryAuthoringConfig>(
            presenterObject,
            "unitPrefabRegistryConfig");
        BuildingPlacementSystemConfig buildingConfig = GetSerializedReference<BuildingPlacementSystemConfig>(
            presenterObject,
            "buildingPlacementConfig");

        _query.Collect(unitConfig, buildingConfig, BuildDrawerCategory.Vehicles, _results);
        Assert.Greater(_results.Count, 0, "Current project configs should expose at least one requestable vehicle for the drawer.");

        BindPermissiveRuntimeCommands(presenter);
        presenter.SelectCategoryForTests(BuildDrawerCategory.Vehicles);

        TMP_Text detailNameText = GetSerializedReference<TMP_Text>(new SerializedObject(view), "nameText");
        TMP_Text actionLabelText = GetSerializedReference<TMP_Text>(new SerializedObject(view), "primaryActionLabelText");
        TMP_Text materialsCostText = GetSerializedReference<TMP_Text>(new SerializedObject(view), "materialsCostText");
        TMP_Text fuelCostText = GetSerializedReference<TMP_Text>(new SerializedObject(view), "fuelCostText");
        Assert.NotNull(detailNameText, "Build drawer detail panel must serialize the selected item name text.");
        Assert.NotNull(actionLabelText, "Build drawer detail panel must serialize the primary action label text.");
        Assert.NotNull(materialsCostText, "Build drawer detail panel must serialize its materials cost text.");
        Assert.NotNull(fuelCostText, "Build drawer detail panel must serialize its fuel cost text.");
        Assert.AreEqual(_results[0].DisplayName, detailNameText.text);
        Assert.AreEqual("PRODUCE", actionLabelText.text);
        Assert.AreEqual(_results[0].MaterialsCost.ToString("N0"), materialsCostText.text);
        Assert.AreEqual(
            _results[0].FuelCost > 0 ? _results[0].FuelCost.ToString("N0") : string.Empty,
            fuelCostText.text);
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
    public void BuildDrawerItemSelection_UsesModelDrivenFrameState()
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(BuildDrawerPrefabPath);
        Assert.NotNull(prefab);

        GameObject instance = UnityEngine.Object.Instantiate(prefab);
        _createdObjects.Add(instance);

        BuildDrawerView view = instance.GetComponent<BuildDrawerView>();
        BuildDrawerCatalogRuntimeView presenter = instance.GetComponent<BuildDrawerCatalogRuntimeView>();
        Assert.NotNull(view);
        Assert.NotNull(presenter);

        BuildingPlacementSystemConfig buildingConfig = CreateAsset<BuildingPlacementSystemConfig>();
        buildingConfig.Spawnables.Add(CreateBuilding("Airport", true, BuildingRole.MilitaryCamp, false));
        buildingConfig.Spawnables.Add(CreateBuilding("Barracks", true, BuildingRole.MilitaryCamp, false));

        ConfigurePresenterForTests(presenter, view, null, buildingConfig);
        presenter.RefreshForTests();

        List<BuildDrawerItemView> activeRows = GetActiveCatalogItemRows(view);
        Assert.GreaterOrEqual(activeRows.Count, 2);
        Assert.AreEqual(Selectable.Transition.None, activeRows[0].SelectionButton.transition);
        Assert.AreEqual(Selectable.Transition.None, activeRows[1].SelectionButton.transition);
        Assert.AreSame(view.SelectedItemFrameSprite, activeRows[0].FrameImage.sprite);
        Assert.AreNotSame(view.SelectedItemFrameSprite, activeRows[1].FrameImage.sprite);

        activeRows[1].SelectionButton.onClick.Invoke();

        Assert.AreNotSame(view.SelectedItemFrameSprite, activeRows[0].FrameImage.sprite);
        Assert.AreSame(view.SelectedItemFrameSprite, activeRows[1].FrameImage.sprite);
        Assert.AreEqual(1, CountSelectedRows(activeRows, view.SelectedItemFrameSprite));
    }

    [Test]
    public void CurrentBuildDrawerPrefabBindsInstructionStripAndIcons()
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(BuildDrawerPrefabPath);
        Assert.NotNull(prefab, "Build drawer popup prefab must exist.");

        GameObject instance = UnityEngine.Object.Instantiate(prefab);
        _createdObjects.Add(instance);

        BuildDrawerView view = instance.GetComponent<BuildDrawerView>();
        Assert.NotNull(view, "Build drawer popup must serialize BuildDrawerView on the root.");
        Assert.NotNull(view.InstructionText, "Build drawer must serialize the instruction text.");
        Assert.NotNull(view.InstructionIcon, "Build drawer must serialize the instruction icon image.");
        Assert.AreSame(
            view.InstructionText.transform.parent,
            view.InstructionIcon.transform.parent,
            "Instruction text and instruction icon must be siblings under InstructionStrip/Frame.");

        SerializedObject viewObject = new SerializedObject(view);
        Sprite infoIcon = GetSerializedReference<Sprite>(viewObject, "instructionInfoIcon");
        Sprite readyIcon = GetSerializedReference<Sprite>(viewObject, "instructionReadyIcon");
        Sprite warningIcon = GetSerializedReference<Sprite>(viewObject, "instructionWarningIcon");
        Sprite errorIcon = GetSerializedReference<Sprite>(viewObject, "instructionErrorIcon");
        Assert.NotNull(infoIcon);
        Assert.NotNull(readyIcon);
        Assert.NotNull(warningIcon);
        Assert.NotNull(errorIcon);

        view.ApplyInstruction("Ready", BuildDrawerInstructionSeverity.Ready);
        Assert.AreSame(readyIcon, view.InstructionIcon.sprite);

        view.ApplyInstruction("Warning", BuildDrawerInstructionSeverity.Warning);
        Assert.AreSame(warningIcon, view.InstructionIcon.sprite);

        view.ApplyInstruction("Error", BuildDrawerInstructionSeverity.Error);
        Assert.AreSame(errorIcon, view.InstructionIcon.sprite);

        view.ApplyInstruction("Info", BuildDrawerInstructionSeverity.Neutral);
        Assert.AreSame(infoIcon, view.InstructionIcon.sprite);
    }

    [Test]
    public void BuildDrawerInstruction_ShowsMissingProducerRequirement()
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(BuildDrawerPrefabPath);
        Assert.NotNull(prefab);

        GameObject instance = UnityEngine.Object.Instantiate(prefab);
        _createdObjects.Add(instance);

        BuildDrawerView view = instance.GetComponent<BuildDrawerView>();
        BuildDrawerCatalogRuntimeView presenter = instance.GetComponent<BuildDrawerCatalogRuntimeView>();
        UnitPrefabRegistryAuthoringConfig unitConfig = CreateAsset<UnitPrefabRegistryAuthoringConfig>();
        GameObject soldier = CreateUnit("Bomb Suit Specialist", true, false, Vector2Int.one, 0);
        unitConfig.UnitSpawnPrefabs.Add(soldier);

        ConfigurePresenterForTests(presenter, view, unitConfig, null);
        presenter.BindRuntimeCommands(
            new BuildingUiCommandAdapter(new BuildingUiCommandSystemHelper(), CreateCommandContext(
                null,
                null,
                100000,
                (GameObject requestPrefab, int price, out string requiredBuilding) =>
                {
                    requiredBuilding = "Barracks";
                    return BuildingUiCommandSystemHelper.CampRequestFailure.MissingProducerBuilding;
                })),
            null);
        presenter.SelectCategoryForTests(BuildDrawerCategory.Soldiers);

        Assert.NotNull(view.InstructionText);
        Assert.AreEqual(
            "Cannot recruit Bomb Suit Specialist: requires Barracks.",
            view.InstructionText.text);
    }

    [Test]
    public void BuildDrawerInstruction_ShowsInsufficientCredits()
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(BuildDrawerPrefabPath);
        Assert.NotNull(prefab);

        GameObject instance = UnityEngine.Object.Instantiate(prefab);
        _createdObjects.Add(instance);

        BuildDrawerView view = instance.GetComponent<BuildDrawerView>();
        BuildDrawerCatalogRuntimeView presenter = instance.GetComponent<BuildDrawerCatalogRuntimeView>();
        UnitPrefabRegistryAuthoringConfig unitConfig = CreateAsset<UnitPrefabRegistryAuthoringConfig>();
        GameObject vehicle = CreateUnit("Light Vehicle", true, false, new Vector2Int(2, 2), 0);
        unitConfig.UnitSpawnPrefabs.Add(vehicle);

        ConfigurePresenterForTests(presenter, view, unitConfig, null);
        presenter.BindRuntimeCommands(
            new BuildingUiCommandAdapter(new BuildingUiCommandSystemHelper(), CreateCommandContext(
                null,
                null,
                1000,
                (GameObject requestPrefab, int price, out string requiredBuilding) =>
                {
                    requiredBuilding = string.Empty;
                    return BuildingUiCommandSystemHelper.CampRequestFailure.NotEnoughMoney;
                })),
            null);
        presenter.SelectCategoryForTests(BuildDrawerCategory.Vehicles);

        Assert.NotNull(view.InstructionText);
        Assert.AreEqual(
            "Cannot produce Light Vehicle: insufficient credits.",
            view.InstructionText.text);
    }

    [Test]
    public void BuildDrawerInstruction_ShowsProductionQueueFull()
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(BuildDrawerPrefabPath);
        Assert.NotNull(prefab);

        GameObject instance = UnityEngine.Object.Instantiate(prefab);
        _createdObjects.Add(instance);

        BuildDrawerView view = instance.GetComponent<BuildDrawerView>();
        BuildDrawerCatalogRuntimeView presenter = instance.GetComponent<BuildDrawerCatalogRuntimeView>();
        UnitPrefabRegistryAuthoringConfig unitConfig = CreateAsset<UnitPrefabRegistryAuthoringConfig>();
        GameObject soldier = CreateUnit("Rifle Infantry", true, false, Vector2Int.one, 0);
        unitConfig.UnitSpawnPrefabs.Add(soldier);

        ConfigurePresenterForTests(presenter, view, unitConfig, null);
        presenter.BindRuntimeCommands(
            new BuildingUiCommandAdapter(new BuildingUiCommandSystemHelper(), CreateCommandContext(
                null,
                null,
                100000,
                (GameObject requestPrefab, int price, out string requiredBuilding) =>
                {
                    requiredBuilding = "Soldier Tent";
                    return BuildingUiCommandSystemHelper.CampRequestFailure.ProductionQueueFull;
                })),
            null);
        presenter.SelectCategoryForTests(BuildDrawerCategory.Soldiers);

        Assert.NotNull(view.InstructionText);
        Assert.AreEqual(
            "Cannot recruit Rifle Infantry: all Soldier Tent production slots are full.",
            view.InstructionText.text);
    }

    [Test]
    public void BuildDrawerInstruction_ShowsGlobalProductionQueueFull()
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(BuildDrawerPrefabPath);
        Assert.NotNull(prefab);

        GameObject instance = UnityEngine.Object.Instantiate(prefab);
        _createdObjects.Add(instance);

        BuildDrawerView view = instance.GetComponent<BuildDrawerView>();
        BuildDrawerCatalogRuntimeView presenter = instance.GetComponent<BuildDrawerCatalogRuntimeView>();
        UnitPrefabRegistryAuthoringConfig unitConfig = CreateAsset<UnitPrefabRegistryAuthoringConfig>();
        GameObject soldier = CreateUnit("Rifle Infantry", true, false, Vector2Int.one, 0);
        unitConfig.UnitSpawnPrefabs.Add(soldier);

        ConfigurePresenterForTests(presenter, view, unitConfig, null);
        presenter.BindRuntimeCommands(
            new BuildingUiCommandAdapter(new BuildingUiCommandSystemHelper(), CreateCommandContext(
                null,
                null,
                100000,
                (GameObject requestPrefab, int price, out string requiredBuilding) =>
                {
                    requiredBuilding = string.Empty;
                    return BuildingUiCommandSystemHelper.CampRequestFailure.GlobalProductionQueueFull;
                })),
            null);
        presenter.SelectCategoryForTests(BuildDrawerCategory.Soldiers);

        Assert.NotNull(view.InstructionText);
        Assert.AreEqual(
            "Cannot recruit Rifle Infantry: production queue limit reached (25 max).",
            view.InstructionText.text);
    }

    [Test]
    public void BuildDrawerPrimaryAction_RoutesBuildingPlacementRequestAndClosesDrawer()
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(BuildDrawerPrefabPath);
        Assert.NotNull(prefab);

        GameObject instance = UnityEngine.Object.Instantiate(prefab);
        _createdObjects.Add(instance);

        BuildDrawerView view = instance.GetComponent<BuildDrawerView>();
        BuildDrawerCatalogRuntimeView presenter = instance.GetComponent<BuildDrawerCatalogRuntimeView>();
        BuildingPlacementSystemConfig buildingConfig = CreateAsset<BuildingPlacementSystemConfig>();
        GameObject building = CreateBuilding("Requestable Barracks", true, BuildingRole.MilitaryCamp, false);
        buildingConfig.Spawnables.Add(building);

        GameObject requestedPrefab = null;
        int requestedPrice = -1;
        bool requestedFocus = true;
        bool closed = false;
        ConfigurePresenterForTests(presenter, view, null, buildingConfig);
        presenter.BindRuntimeCommands(
            new BuildingUiCommandAdapter(new BuildingUiCommandSystemHelper(), CreateCommandContext((GameObject requestPrefab, int price, out string requiredBuilding, bool focusProducer) =>
            {
                requestedPrefab = requestPrefab;
                requestedPrice = price;
                requestedFocus = focusProducer;
                requiredBuilding = string.Empty;
                return BuildingUiCommandSystemHelper.CampRequestFailure.None;
            })),
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
        BuildDrawerCatalogRuntimeView presenter = instance.GetComponent<BuildDrawerCatalogRuntimeView>();
        UnitPrefabRegistryAuthoringConfig unitConfig = CreateAsset<UnitPrefabRegistryAuthoringConfig>();
        GameObject vehicle = CreateUnit("Requestable Vehicle", true, false, new Vector2Int(2, 2), 0);
        Sprite vehiclePortrait = CreateTestSprite(Color.cyan);
        AssignUnitPortraitSprites(vehicle, vehiclePortrait);
        unitConfig.UnitSpawnPrefabs.Add(vehicle);

        GameObject requestedPrefab = null;
        int requestedPrice = -1;
        bool requestedFocus = true;
        bool closed = false;
        ConfigurePresenterForTests(presenter, view, unitConfig, null);
        presenter.BindRuntimeCommands(
            new BuildingUiCommandAdapter(new BuildingUiCommandSystemHelper(), CreateCommandContext((GameObject requestPrefab, int price, out string requiredBuilding, bool focusProducer) =>
            {
                requestedPrefab = requestPrefab;
                requestedPrice = price;
                requestedFocus = focusProducer;
                requiredBuilding = string.Empty;
                return BuildingUiCommandSystemHelper.CampRequestFailure.None;
            })),
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
        BuildDrawerCatalogRuntimeView presenter = instance.GetComponent<BuildDrawerCatalogRuntimeView>();
        BuildingPlacementSystemConfig buildingConfig = CreateAsset<BuildingPlacementSystemConfig>();
        GameObject buildingPrefab = CreateBuilding("Requestable Airport", true, BuildingRole.MilitaryCamp, false);
        buildingConfig.Spawnables.Add(buildingPrefab);

        BuildingDefinition buildingDefinition = new()
        {
            DisplayName = "Requestable Airport",
            Prefab = buildingPrefab,
            CreditsCost = 1234
        };
        var requestSystem = new BuildingProductionRequestSystemHelper();
        bool beganPlacement = false;
        bool closed = false;
        BuildingProductionRequestSystemHelper.Context requestContext = CreateProductionRequestContext(
            new Dictionary<int, RuntimeBuildingEntity>(),
            requestSystem,
            new BuildingProductionQueueCompositionSystemHelper(),
            Array.Empty<GameObject>(),
            new Dictionary<GameObject, BuildingDefinition> { { buildingPrefab, buildingDefinition } },
            _ => { beganPlacement = true; return true; },
            amount => true,
            _ => { });

        ConfigurePresenterForTests(presenter, view, null, buildingConfig);
        presenter.BindRuntimeCommands(
            new BuildingUiCommandAdapter(new BuildingUiCommandSystemHelper(), CreateRealCommandContext(requestSystem, () => requestContext)),
            () => closed = true);
        presenter.RefreshForTests();

        view.PrimaryActionButton.onClick.Invoke();

        Assert.IsTrue(beganPlacement, "Building PLACE must enter configured building placement.");
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
        BuildDrawerCatalogRuntimeView presenter = instance.GetComponent<BuildDrawerCatalogRuntimeView>();
        UnitPrefabRegistryAuthoringConfig unitConfig = CreateAsset<UnitPrefabRegistryAuthoringConfig>();
        GameObject vehicle = CreateUnit("Requestable Vehicle", true, false, new Vector2Int(2, 2), 0);
        Sprite vehiclePortrait = CreateTestSprite(Color.cyan);
        AssignUnitPortraitSprites(vehicle, vehiclePortrait);
        unitConfig.UnitSpawnPrefabs.Add(vehicle);

        RuntimeBuildingEntity producer = CreateRuntimeProducerBuilding(7, "Vehicle Factory", vehicle);
        var runtimeBuildings = new Dictionary<int, RuntimeBuildingEntity> { { producer.Id, producer } };
        var productionSystem = new BuildingProductionQueueCompositionSystemHelper();
        var requestSystem = new BuildingProductionRequestSystemHelper();
        int materials = 100000;
        BuildingProductionRequestSystemHelper.Context requestContext = CreateProductionRequestContext(
            runtimeBuildings,
            requestSystem,
            productionSystem,
            new[] { vehicle },
            new Dictionary<GameObject, BuildingDefinition>(),
            _ => false,
            amount =>
            {
                if (materials < amount)
                    return false;

                materials -= amount;
                return true;
            },
            _ => { },
            world.EntityManager);
        BuildingUiQueryUiSystemHelper.Context queryContext = CreateProductionQueryContext(
            runtimeBuildings,
            requestSystem,
            productionSystem,
            () => requestContext,
            world.EntityManager);

        bool closed = false;
        ConfigurePresenterForTests(presenter, view, unitConfig, null);
        presenter.BindRuntimeCommands(
            new BuildingUiCommandAdapter(new BuildingUiCommandSystemHelper(), CreateRealCommandContext(requestSystem, () => requestContext)),
            () => closed = true);
        presenter.BindRuntimeQueries(new BuildingUiQueryAdapter(new BuildingUiQueryUiSystemHelper(), queryContext));
        presenter.SelectCategoryForTests(BuildDrawerCategory.Vehicles);

        Assert.AreEqual(0, producer.PendingProductions.Count);
        view.PrimaryActionButton.onClick.Invoke();

        Assert.IsFalse(closed, "Production requests should keep the drawer open.");
        Assert.AreEqual(1, producer.PendingProductions.Count);
        Assert.AreSame(vehicle, producer.PendingProductions[0].Prefab);
        Assert.AreEqual(0, producer.PendingProductions[0].ProductionIndex);
        Assert.AreEqual(100000 - 5678, materials);
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
        BuildDrawerCatalogRuntimeView presenter = instance.GetComponent<BuildDrawerCatalogRuntimeView>();
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
        ConfigurePresenterForTests(presenter, view, unitConfig, null);

        SerializedObject viewObject = new SerializedObject(view);
        GameObject productionPanel = GetSerializedReference<GameObject>(viewObject, "productionPanel");
        GameObject productionPanelActive = GetSerializedReference<GameObject>(viewObject, "productionPanelActive");
        GameObject noProductionView = GetSerializedReference<GameObject>(viewObject, "noProductionView");
        TMP_Text noProductionText = GetSerializedReference<TMP_Text>(viewObject, "noProductionText");
        Assert.NotNull(productionPanel, "Build drawer must serialize the production panel container.");
        Assert.NotNull(productionPanelActive, "Build drawer must serialize the active production panel state.");
        Assert.NotNull(noProductionView, "Build drawer must serialize the empty production panel state.");
        Assert.NotNull(noProductionText, "Build drawer must serialize the empty production label.");

        presenter.ApplyQueueSnapshotForTests(Array.Empty<BuildingPendingProductionUiEntry>());
        Assert.IsFalse(view.ActiveItemView.gameObject.activeSelf);
        Assert.IsFalse(view.QueuedItemTemplate.gameObject.activeSelf);
        Assert.IsTrue(productionPanel.activeSelf, "ProductionPanel should remain visible so the empty state can be shown.");
        Assert.IsFalse(productionPanelActive.activeSelf);
        Assert.IsTrue(noProductionView.activeSelf);
        Assert.AreEqual("NO PRODUCTION QUEUED", noProductionText.text);

        List<BuildingPendingProductionUiEntry> entries = new()
        {
            new BuildingPendingProductionUiEntry(7, activePrefab, 74f, 100f, 0.26f, 0f, 100f, "Factory"),
            new BuildingPendingProductionUiEntry(7, queuedPrefab, 12f, 24f, 0.5f, 0f, 24f, "Factory"),
            new BuildingPendingProductionUiEntry(7, thirdPrefab, 8f, 20f, 0.6f, 0f, 20f, "Factory")
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

        BuildDrawerQueueItemView retainedThirdRow = activeQueueItems[2];
        int queueChildCount = view.QueueContentRoot.childCount;
        presenter.ApplyQueueSnapshotForTests(entries);
        Assert.AreSame(
            retainedThirdRow,
            GetActiveQueueItemViews(view)[2],
            "Refreshing an unchanged queue must retain the extra queue row instance.");

        presenter.ApplyQueueSnapshotForTests(Array.Empty<BuildingPendingProductionUiEntry>());
        Assert.AreEqual(
            queueChildCount,
            view.QueueContentRoot.childCount,
            "Clearing the queue snapshot must keep pooled queue rows inactive instead of destroying them.");

        presenter.ApplyQueueSnapshotForTests(entries);
        Assert.AreSame(
            retainedThirdRow,
            GetActiveQueueItemViews(view)[2],
            "Refilling the queue must reuse the pooled extra queue row instance.");
    }

    [Test]
    public void CurrentBuildDrawerPrefabShowsSingleActiveProductionForOneQueueEntry()
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(BuildDrawerPrefabPath);
        Assert.NotNull(prefab);

        GameObject instance = UnityEngine.Object.Instantiate(prefab);
        _createdObjects.Add(instance);

        BuildDrawerView view = instance.GetComponent<BuildDrawerView>();
        BuildDrawerCatalogRuntimeView presenter = instance.GetComponent<BuildDrawerCatalogRuntimeView>();
        Assert.NotNull(view);
        Assert.NotNull(presenter);

        UnitPrefabRegistryAuthoringConfig unitConfig = CreateAsset<UnitPrefabRegistryAuthoringConfig>();
        GameObject activePrefab = CreateUnit("Queue Vehicle Active", true, false, new Vector2Int(2, 2), 0);
        Sprite activePortrait = CreateTestSprite(Color.magenta);
        AssignUnitPortraitSprites(activePrefab, activePortrait);
        unitConfig.UnitSpawnPrefabs.Add(activePrefab);
        ConfigurePresenterForTests(presenter, view, unitConfig, null);

        presenter.ApplyQueueSnapshotForTests(new[]
        {
            new BuildingPendingProductionUiEntry(7, activePrefab, 74f, 100f, 0.26f, 0f, 100f, "Factory")
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
        BuildDrawerCatalogRuntimeView presenter = instance.GetComponent<BuildDrawerCatalogRuntimeView>();
        Assert.NotNull(view);
        Assert.NotNull(presenter);
        Assert.NotNull(view.CancelButton, "Build drawer must serialize the production cancel button.");

        UnitPrefabRegistryAuthoringConfig unitConfig = CreateAsset<UnitPrefabRegistryAuthoringConfig>();
        GameObject activePrefab = CreateUnit("Queue Vehicle Active", true, false, new Vector2Int(2, 2), 0);
        unitConfig.UnitSpawnPrefabs.Add(activePrefab);

        int cancelledBuildingId = -1;
        int cancelledPendingIndex = -1;
        ConfigurePresenterForTests(presenter, view, unitConfig, null);
        presenter.BindRuntimeCommands(
            new BuildingUiCommandAdapter(new BuildingUiCommandSystemHelper(), CreateCommandContext(
                (GameObject requestPrefab, int price, out string requiredBuilding, bool focusProducer) =>
                {
                    requiredBuilding = string.Empty;
                    return BuildingUiCommandSystemHelper.CampRequestFailure.InvalidSelection;
                },
                (buildingId, pendingIndex) =>
                {
                    cancelledBuildingId = buildingId;
                    cancelledPendingIndex = pendingIndex;
                    return true;
                })),
            null);

        List<BuildingPendingProductionUiEntry> entries = new()
        {
            new BuildingPendingProductionUiEntry(7, 3, activePrefab, 74f, 100f, 0.26f, 0f, 100f, "Factory")
        };
        presenter.ApplyQueueSnapshotForTests(entries);
        Assert.IsTrue(view.CancelButton.interactable);
        Assert.NotNull(view.ActiveItemView.CancelButton, "Active production row must expose its own cancel button.");
        Assert.IsTrue(view.ActiveItemView.CancelButton.interactable);

        ClickButtonThroughTargetGraphic(view.ActiveItemView.CancelButton, "active production cancel");

        Assert.AreEqual(7, cancelledBuildingId);
        Assert.AreEqual(3, cancelledPendingIndex);
    }

    [Test]
    public void BuildDrawerClearButton_RoutesAllProductionCancelRequests()
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(BuildDrawerPrefabPath);
        Assert.NotNull(prefab);

        GameObject instance = UnityEngine.Object.Instantiate(prefab);
        _createdObjects.Add(instance);

        BuildDrawerView view = instance.GetComponent<BuildDrawerView>();
        BuildDrawerCatalogRuntimeView presenter = instance.GetComponent<BuildDrawerCatalogRuntimeView>();
        Assert.NotNull(view);
        Assert.NotNull(presenter);
        Assert.NotNull(view.ClearButton, "Build drawer must serialize the production clear button.");

        UnitPrefabRegistryAuthoringConfig unitConfig = CreateAsset<UnitPrefabRegistryAuthoringConfig>();
        GameObject activePrefab = CreateUnit("Queue Vehicle Active", true, false, new Vector2Int(2, 2), 0);
        unitConfig.UnitSpawnPrefabs.Add(activePrefab);

        var cancelled = new List<(int BuildingId, int PendingIndex)>();
        ConfigurePresenterForTests(presenter, view, unitConfig, null);
        presenter.BindRuntimeCommands(
            new BuildingUiCommandAdapter(new BuildingUiCommandSystemHelper(), CreateCommandContext(
                (GameObject requestPrefab, int price, out string requiredBuilding, bool focusProducer) =>
                {
                    requiredBuilding = string.Empty;
                    return BuildingUiCommandSystemHelper.CampRequestFailure.InvalidSelection;
                },
                (buildingId, pendingIndex) =>
                {
                    cancelled.Add((buildingId, pendingIndex));
                    return true;
                })),
            null);

        List<BuildingPendingProductionUiEntry> entries = new()
        {
            new BuildingPendingProductionUiEntry(7, 0, activePrefab, 74f, 100f, 0.26f, 0f, 100f, "Factory"),
            new BuildingPendingProductionUiEntry(7, 1, activePrefab, 12f, 24f, 0.5f, 0f, 24f, "Factory"),
            new BuildingPendingProductionUiEntry(7, 2, activePrefab, 8f, 20f, 0.6f, 0f, 20f, "Factory")
        };
        presenter.ApplyQueueSnapshotForTests(entries);
        Assert.IsTrue(view.ClearButton.interactable);

        ClickButtonThroughTargetGraphic(view.ClearButton, "clear production queue");

        Assert.AreEqual(3, cancelled.Count);
        Assert.AreEqual((7, 2), cancelled[0]);
        Assert.AreEqual((7, 1), cancelled[1]);
        Assert.AreEqual((7, 0), cancelled[2]);
    }

    [Test]
    public void BuildDrawerItemSelection_DoesNotMutateFirstCardThumbnail()
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(BuildDrawerPrefabPath);
        Assert.NotNull(prefab);

        GameObject instance = UnityEngine.Object.Instantiate(prefab);
        _createdObjects.Add(instance);

        BuildDrawerView view = instance.GetComponent<BuildDrawerView>();
        BuildDrawerCatalogRuntimeView presenter = instance.GetComponent<BuildDrawerCatalogRuntimeView>();
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

        ConfigurePresenterForTests(presenter, view, null, buildingConfig);
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
        BuildDrawerCatalogRuntimeView presenter = instance.GetComponent<BuildDrawerCatalogRuntimeView>();
        Assert.NotNull(view);
        Assert.NotNull(presenter);

        BuildingPlacementSystemConfig buildingConfig = CreateAsset<BuildingPlacementSystemConfig>();
        buildingConfig.Spawnables.Add(CreateBuilding("Requestable Barracks", true, BuildingRole.MilitaryCamp, false));
        ConfigurePresenterForTests(presenter, view, null, buildingConfig);
        BindPermissiveRuntimeCommands(presenter);
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

    private static void ConfigurePresenterForTests(
        BuildDrawerCatalogRuntimeView presenter,
        BuildDrawerView view,
        UnitPrefabRegistryAuthoringConfig unitRegistry,
        BuildingPlacementSystemConfig buildingPlacement)
    {
        presenter.ConfigureForTests(view, unitRegistry, buildingPlacement);
        ConfigureCatalogMetadataResolvers(presenter);
    }

    private static void ConfigureCatalogMetadataResolvers(BuildDrawerCatalogRuntimeView presenter)
    {
        presenter.ConfigureCatalogMetadataResolvers(
            UiCatalogAuthoringMetadataUiSystemHelper.TryGetBuildingMetadata,
            UiCatalogAuthoringMetadataUiSystemHelper.TryGetUnitMetadata);
    }

    private static void ConfigureCatalogMetadataResolvers(BuildDrawerCatalogQueryUiSystemHelper query)
    {
        query.ConfigureMetadataResolvers(
            UiCatalogAuthoringMetadataUiSystemHelper.TryGetBuildingMetadata,
            UiCatalogAuthoringMetadataUiSystemHelper.TryGetUnitMetadata);
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

    private static BuildingProductionRequestSystemHelper.Context CreateProductionRequestContext(
        IReadOnlyDictionary<int, RuntimeBuildingEntity> runtimeBuildings,
        BuildingProductionRequestSystemHelper requestSystem,
        BuildingProductionQueueCompositionSystemHelper productionSystem,
        IReadOnlyList<GameObject> unitPrefabs,
        IReadOnlyDictionary<GameObject, BuildingDefinition> configuredDefinitionsByPrefab,
        BuildingProductionRequestSystemHelper.BeginPlacementForConfiguredSpawnableDelegate beginPlacement,
        BuildingProductionRequestSystemHelper.TrySpendMaterialsDelegate trySpendMaterials,
        BuildingProductionRequestSystemHelper.SetActivePlacementCostDelegate setActivePlacementCost,
        EntityManager entityManager = default)
    {
        IReadOnlyDictionary<string, GameObject> unitPrefabsByKey = new Dictionary<string, GameObject>();
        BuildingProductionQueueCompositionSystemHelper.QueueContext queueContext = new(
            unitPrefabs,
            unitPrefabsByKey,
            new BuildingProductionSlotUtilitySystemHelper(),
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

        return new BuildingProductionRequestSystemHelper.Context(
            runtimeBuildings,
            configuredDefinitions,
            configuredDefinitionsByPrefab,
            unitPrefabs,
            unitPrefabsByKey,
            100000,
            25,
            productionSystem,
            queueContext,
            null,
            BuildingDefinitionPrefabSystemHelper.GetProductionPrefab,
            null,
            beginPlacement,
            trySpendMaterials,
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

    private static BuildingUiCommandSystemHelper.Context CreateRealCommandContext(
        BuildingProductionRequestSystemHelper requestSystem,
        Func<BuildingProductionRequestSystemHelper.Context> createRequestContext)
    {
        return new BuildingUiCommandSystemHelper.Context(
            () => 100000,
            () => 25,
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
            () => false,
            () => false,
            () => string.Empty,
            () => 0,
            () => 0f,
            null,
            null,
            null);
    }

    private static BuildingUiQueryUiSystemHelper.Context CreateProductionQueryContext(
        IReadOnlyDictionary<int, RuntimeBuildingEntity> runtimeBuildings,
        BuildingProductionRequestSystemHelper requestSystem,
        BuildingProductionQueueCompositionSystemHelper productionSystem,
        Func<BuildingProductionRequestSystemHelper.Context> createRequestContext,
        EntityManager entityManager)
    {
        return new BuildingUiQueryUiSystemHelper.Context(
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

    private static BuildingUiCommandSystemHelper.Context CreateCommandContext(
        BuildingUiCommandSystemHelper.TryRequestCampItemDelegate tryRequestCampItem,
        BuildingUiCommandSystemHelper.CancelProductionDelegate cancelProduction = null,
        int currentDollars = 100000,
        BuildingUiCommandSystemHelper.GetCampRequestFailureDelegate getCampRequestFailure = null)
    {
        return new BuildingUiCommandSystemHelper.Context(
            () => currentDollars,
            () => 25,
            () => 0,
            null,
            () => 0,
            null,
            null,
            getCampRequestFailure,
            tryRequestCampItem,
            () => false,
            () => false,
            () => string.Empty,
            () => 0,
            () => 0f,
            null,
            null,
            cancelProduction,
            null);
    }

    private static void BindPermissiveRuntimeCommands(BuildDrawerCatalogRuntimeView presenter)
    {
        presenter.BindRuntimeCommands(
            new BuildingUiCommandAdapter(new BuildingUiCommandSystemHelper(), CreateCommandContext(
                null,
                null,
                getCampRequestFailure: (GameObject requestPrefab, int price, out string requiredBuilding) =>
                {
                    requiredBuilding = string.Empty;
                    return BuildingUiCommandSystemHelper.CampRequestFailure.None;
                })),
            null);
    }

    private static T GetSerializedReference<T>(SerializedObject serializedObject, string propertyName)
        where T : UnityEngine.Object
    {
        return (T)serializedObject.FindProperty(propertyName).objectReferenceValue;
    }

    private void ClickButtonThroughTargetGraphic(Button button, string label)
    {
        Assert.NotNull(button, $"{label} button must exist.");
        Assert.IsTrue(button.gameObject.activeInHierarchy, $"{label} button must be active.");
        Assert.IsTrue(button.interactable, $"{label} button must be interactable.");
        Assert.NotNull(button.targetGraphic, $"{label} button must have a target graphic.");
        Assert.IsTrue(button.targetGraphic.raycastTarget, $"{label} button target graphic must receive raycasts.");

        GameObject eventSystemObject = new($"EventSystem - {label}", typeof(EventSystem), typeof(StandaloneInputModule));
        _createdObjects.Add(eventSystemObject);
        EventSystem eventSystem = eventSystemObject.GetComponent<EventSystem>();
        Canvas.ForceUpdateCanvases();

        RectTransform targetRect = button.targetGraphic.rectTransform;
        Assert.Greater(targetRect.rect.width, 0f, $"{label} target graphic must have visible width.");
        Assert.Greater(targetRect.rect.height, 0f, $"{label} target graphic must have visible height.");

        Vector2 center = RectTransformUtility.WorldToScreenPoint(
            null,
            targetRect.TransformPoint(targetRect.rect.center));
        Assert.IsTrue(
            RectTransformUtility.RectangleContainsScreenPoint(targetRect, center, null),
            $"{label} target graphic must contain its own center point.");

        var pointerEvent = new PointerEventData(eventSystem)
        {
            position = center,
            button = PointerEventData.InputButton.Left
        };
        bool handled = ExecuteEvents.ExecuteHierarchy(
            button.targetGraphic.gameObject,
            pointerEvent,
            ExecuteEvents.pointerClickHandler);

        Assert.IsTrue(handled, $"{label} pointer click must dispatch to a Button.");
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
