#if UNITY_INCLUDE_TESTS && UNITY_EDITOR
using System.Collections.Generic;
using System.Reflection;
using Game.Scripts.UI;
using NUnit.Framework;
using TMPro;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.UI;

public sealed class BootstrapAndMenuPlayModeTests
{
    private readonly List<Object> _createdObjects = new();

    [TearDown]
    public void TearDown()
    {
        InitialUnitsRuntimeState.PlayRequested = false;
        InitialUnitsRuntimeState.SelectionModeActive = false;
        InitialUnitsRuntimeState.BuildModeActive = false;
        InitialUnitsRuntimeState.ZoomInHeld = false;
        InitialUnitsRuntimeState.ZoomOutHeld = false;
        InitialUnitsRuntimeState.SuppressNextWorldClick = false;
        InitialUnitsRuntimeState.FullscreenMapOpen = false;
        InitialUnitsRuntimeState.FullscreenMapIsoMode = false;
        InitialUnitsRuntimeState.PlayerAutoModeEnabled = false;
        AISettingsRuntimeState.ResetDefaults();

        for (int i = _createdObjects.Count - 1; i >= 0; i--)
        {
            Object unityObject = _createdObjects[i];
            if (unityObject != null)
                Object.Destroy(unityObject);
        }

        _createdObjects.Clear();
    }

    [Test]
    public void GameBootstrap_AwakeDoesNotInitializeGameplayBeforePlayRequest()
    {
        InitialUnitsRuntimeState.PlayRequested = false;

        GameObject bootstrapObject = Track(new GameObject("Bootstrap"));
        GameBootstrap bootstrap = bootstrapObject.AddComponent<GameBootstrap>();

        Assert.NotNull(bootstrap.DayNight, "Bootstrap should create core dependencies during Awake.");
        Assert.NotNull(bootstrap.RoadBuildReadModel, "Bootstrap should create road read-model dependencies during Awake.");
        Assert.NotNull(bootstrap.BuildingSelectionClick, "Bootstrap should create building selection click dependencies during Awake.");
        Assert.NotNull(bootstrap.BuildingRuntimeUpdate, "Bootstrap should create building runtime update dependencies during Awake.");
        Assert.NotNull(bootstrap.SelectionUiCommand, "Bootstrap should create selection command dependencies during Awake.");
        Assert.NotNull(bootstrap.SelectionUiReadModel, "Bootstrap should create selection read-model dependencies during Awake.");
        Assert.NotNull(bootstrap.SelectionUiCamera, "Bootstrap should create selection camera dependencies during Awake.");
        Assert.NotNull(bootstrap.SelectionScreenMarkers, "Bootstrap should create selection marker dependencies during Awake.");
        Assert.IsFalse(InitialUnitsRuntimeState.PlayRequested, "PlayRequested must remain false when the scene first starts.");
        Assert.IsFalse(bootstrap.GameplayInitialized, "Gameplay systems must not initialize before the menu play request.");
        Assert.IsNull(bootstrap.RuntimeCity, "Runtime city must not be created before gameplay starts.");
        Assert.IsNull(bootstrap.RuntimeGridBlockers, "Runtime grid blockers must not be created before gameplay starts.");
        Assert.IsNull(bootstrap.RuntimeDecorations, "Runtime decorations must not be created before gameplay starts.");
    }

    [Test]
    public void MenuView_ButtonFlow_TransitionsBetweenMenuStatsGameAndMap()
    {
        MenuView menu = CreateMenuView();
        bool gameRequested = false;
        menu.GameRequested += () => gameRequested = true;

        InitMenu(menu, Camera.main);
        menu.NotifyBootstrapReady();

        Assert.IsTrue(menu.panelMenu.gameObject.activeSelf);
        Assert.IsFalse(menu.buttonBack.gameObject.activeSelf);
        Assert.IsTrue(menu.buttonGame.gameObject.activeSelf);

        menu.buttonStats.onClick.Invoke();

        Assert.IsTrue(menu.panelStats.gameObject.activeSelf);
        Assert.IsTrue(menu.buttonBack.gameObject.activeSelf);

        menu.buttonBack.onClick.Invoke();

        Assert.IsTrue(menu.panelMenu.gameObject.activeSelf);
        Assert.IsFalse(menu.buttonBack.gameObject.activeSelf);

        menu.buttonGame.onClick.Invoke();

        Assert.IsTrue(gameRequested);
        Assert.IsTrue(menu.panelLoading.activeSelf);
        Assert.IsFalse(menu.buttonGame.gameObject.activeSelf);

        menu.NotifyGameplayReady();

        Assert.IsTrue(menu.panelGame.gameObject.activeSelf);
        Assert.IsTrue(menu.gamePanelFree.gameObject.activeSelf);
        Assert.IsFalse(menu.gamePanelMap.gameObject.activeSelf);

        menu.buttonMap.onClick.Invoke();

        Assert.IsTrue(menu.gamePanelMap.gameObject.activeSelf);
        Assert.IsFalse(menu.gamePanelFree.gameObject.activeSelf);

        menu.buttonBack.onClick.Invoke();

        Assert.IsTrue(menu.panelGame.gameObject.activeSelf);
        Assert.IsTrue(menu.gamePanelFree.gameObject.activeSelf);
        Assert.IsFalse(menu.gamePanelMap.gameObject.activeSelf);

        menu.buttonStats.onClick.Invoke();

        Assert.IsTrue(menu.panelStats.gameObject.activeSelf);

        menu.buttonBack.onClick.Invoke();

        Assert.IsTrue(menu.panelGame.gameObject.activeSelf);
        Assert.IsTrue(menu.gamePanelFree.gameObject.activeSelf);
    }

    [Test]
    public void MenuView_CanvasAutoModeButton_TogglesPlayerControlMode()
    {
        MenuView menu = CreateMenuView();

        InitMenu(menu, Camera.main);

        GameObject autoModeObject = FindDescendantByName(menu.transform, "Button_AutoMode")?.gameObject;
        Assert.NotNull(autoModeObject, "MenuView should resolve the scene-owned UI_Canvas auto/manual control.");
        Assert.IsFalse(autoModeObject.activeSelf, "Auto/manual control should stay hidden before gameplay starts.");

        InitialUnitsRuntimeState.PlayRequested = true;
        menu.NotifyGameplayReady();

        Button autoModeButton = autoModeObject.GetComponent<Button>();
        TMP_Text autoModeLabel = autoModeObject.GetComponentInChildren<TMP_Text>(true);
        Assert.NotNull(autoModeButton);
        Assert.NotNull(autoModeLabel);
        Assert.IsTrue(autoModeObject.activeSelf);
        Assert.AreEqual("Manual", autoModeLabel.text);

        autoModeButton.onClick.Invoke();

        Assert.IsTrue(InitialUnitsRuntimeState.PlayerAutoModeEnabled);
        Assert.AreEqual("Auto", autoModeLabel.text);
    }

    [Test]
    public void MenuView_SettingsButton_ShowsSceneOwnedPanelAndGameplaySpeedDropdown()
    {
        MenuView menu = CreateMenuView();

        InitMenu(menu, Camera.main);

        GameObject settingsPanel = menu.panelSettings;
        Button settingsButton = menu.buttonSettings;
        TMP_Dropdown dropdown = menu.gameplaySpeedDropdown;

        Assert.NotNull(settingsPanel);
        Assert.NotNull(settingsButton);
        Assert.NotNull(dropdown);
        Assert.IsFalse(settingsPanel.activeSelf);
        Assert.AreEqual(12, dropdown.options.Count);
        Assert.AreEqual("1x", dropdown.options[0].text);
        Assert.AreEqual("10x", dropdown.options[11].text);

        settingsButton.onClick.Invoke();

        Assert.IsTrue(settingsPanel.activeSelf);
        Assert.IsTrue(menu.buttonBack.gameObject.activeSelf);

        menu.buttonBack.onClick.Invoke();

        Assert.IsFalse(settingsPanel.activeSelf);
    }

    [Test]
    public void MenuView_SelectAllSoldiersButton_CapturesUiClickAndSelectsVisibleSoldiers()
    {
        World previousWorld = World.DefaultGameObjectInjectionWorld;
        using World world = new("MenuView_SelectAllSoldiersButton");
        World.DefaultGameObjectInjectionWorld = world;

        try
        {
            Camera camera = Track(new GameObject("Selection Camera")).AddComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = 20f;
            camera.transform.position = new Vector3(0f, 20f, -20f);
            camera.transform.rotation = Quaternion.Euler(45f, 0f, 0f);

            EntityManager em = world.EntityManager;
            CreateSelectableUnit(em, "Unit_Chr_Test", new float3(0f, 0f, 0f));
            CreateSelectableUnit(em, "Unit_Veh_Test", new float3(4f, 0f, 0f));

            MenuView menu = CreateMenuView();
            Button soldiersButton = CreateButton("Button_Select_All_Soldiers", menu.gamePanelFree.transform);
            InitMenu(menu, camera);

            InitialUnitsRuntimeState.PlayRequested = true;
            menu.NotifyGameplayReady();
            soldiersButton.onClick.Invoke();

            Assert.IsTrue(
                TryFindSelectionCommand(RtsSelectionCommandIntentKind.SelectAllSoldiers),
                "The UI select-all-soldiers button should enqueue a soldier-only selection command.");
            Assert.IsTrue(TryReadSelectionInputState(out RtsSelectionInputStateComponent state));
            Assert.AreEqual(1, state.IgnoreUiClickUntilRelease, "The button must capture the UI click so the same release cannot become a world move/deselect click.");
            Assert.AreEqual(1, state.IgnoreNextLeftMouseRelease, "The selection command must suppress the matching mouse release.");
        }
        finally
        {
            World.DefaultGameObjectInjectionWorld = previousWorld;
        }
    }

    [Test]
    public void MainMenuPlayUI_ToolbarPointerDown_DoesNotDeselectSelectedSoldiers()
    {
        World previousWorld = World.DefaultGameObjectInjectionWorld;
        using World world = new("MainMenuPlayUI_ToolbarPointerDown");
        World.DefaultGameObjectInjectionWorld = world;

        try
        {
            EntityManager em = world.EntityManager;
            Entity soldier = CreateSelectableUnit(em, "Unit_Chr_Test", new float3(0f, 0f, 0f));
            em.AddComponent<SelectedUnitTag>(soldier);

            MainMenuPlayUI mainMenu = new();
            mainMenu.Init(new SelectionUiCommandSystem(), null);

            MethodInfo pointerDown = typeof(MainMenuPlayUI).GetMethod("OnToolbarUiPointerDown", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull(pointerDown);
            pointerDown.Invoke(mainMenu, new object[] { null });
            MethodInfo mouseDown = typeof(MainMenuPlayUI).GetMethod("OnToolbarUiMouseDown", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull(mouseDown);
            mouseDown.Invoke(mainMenu, new object[] { null });

            Assert.IsTrue(
                em.HasComponent<SelectedUnitTag>(soldier),
                "Toolbar pointer capture must not clear selected soldiers before the next world click can issue a boarding command.");
        }
        finally
        {
            World.DefaultGameObjectInjectionWorld = previousWorld;
        }
    }

    [Test]
    public void MenuView_AISettingsDropdowns_UpdateRuntimeStateAndExistingPlans()
    {
        World previousWorld = World.DefaultGameObjectInjectionWorld;
        World world = new World("AI Settings UI Test World");
        World.DefaultGameObjectInjectionWorld = world;

        try
        {
            EntityManager em = world.EntityManager;
            Entity economy = em.CreateEntity(typeof(FactionEconomy), typeof(FactionEconomyPolicy));
            em.SetComponentData(economy, new FactionEconomy { FactionId = 1, Money = 1000 });
            em.SetComponentData(economy, new FactionEconomyPolicy { Enabled = 1, IncomeMultiplier = 1f });
            Entity playerEconomy = em.CreateEntity(typeof(FactionEconomy), typeof(FactionEconomyPolicy));
            em.SetComponentData(playerEconomy, new FactionEconomy { FactionId = 0, Money = 1000 });
            em.SetComponentData(playerEconomy, new FactionEconomyPolicy { Enabled = 0, IncomeMultiplier = 1f });
            Entity buildPlan = em.CreateEntity(typeof(AIBuildPlan));
            em.SetComponentData(buildPlan, new AIBuildPlan { FactionId = 1, Enabled = 1, BuildIntervalSeconds = 8f });
            Entity playerBuildPlan = em.CreateEntity(typeof(AIBuildPlan));
            em.SetComponentData(playerBuildPlan, new AIBuildPlan { FactionId = 0, Enabled = 0, BuildIntervalSeconds = 10f, LastBuildTime = 5f });
            Entity productionPlan = em.CreateEntity(typeof(AIProductionPlan));
            em.SetComponentData(productionPlan, new AIProductionPlan { FactionId = 1, Enabled = 1, UnitProductionIntervalSeconds = 6f });
            Entity playerProductionPlan = em.CreateEntity(typeof(AIProductionPlan));
            em.SetComponentData(playerProductionPlan, new AIProductionPlan { FactionId = 0, Enabled = 0, UnitProductionIntervalSeconds = 8f, LastProductionTime = 5f });
            Entity squadPlan = em.CreateEntity(typeof(AISquadPlan));
            em.SetComponentData(squadPlan, new AISquadPlan { FactionId = 1, Enabled = 1, MinUnits = 3, MaxUnits = 8, MaxActiveSquads = 2 });
            Entity playerSquadPlan = em.CreateEntity(typeof(AISquadPlan));
            em.SetComponentData(playerSquadPlan, new AISquadPlan { FactionId = 0, Enabled = 0, MinUnits = 2, MaxUnits = 6, MaxActiveSquads = 1 });
            Entity targetPriority = em.CreateEntity(typeof(AITargetPrioritySetting));
            em.SetComponentData(targetPriority, new AITargetPrioritySetting { FactionId = 1, Priority = (byte)AITargetPriority.Balanced });
            Entity controlEntity = em.CreateEntity(typeof(FactionControlConfigTag));
            DynamicBuffer<FactionControlEntry> controls = em.AddBuffer<FactionControlEntry>(controlEntity);
            controls.Add(new FactionControlEntry { FactionId = 0, AIControlled = 0, IsPlayerFaction = 1 });
            controls.Add(new FactionControlEntry { FactionId = 1, AIControlled = 1 });

            MenuView menu = CreateMenuView();
            InitMenu(menu, Camera.main);

            AssertAISettingsDropdownOptions(menu);

            menu.aiDifficultyDropdown.value = 3;
            menu.aiStartingMoneyDropdown.value = 2;
            menu.aiIncomeMultiplierDropdown.value = 4;
            menu.aiBuildSpeedDropdown.value = 2;
            menu.aiUnitProductionSpeedDropdown.value = 2;
            menu.aiAttackGroupSizeDropdown.value = 2;
            menu.aiAttackFrequencyDropdown.value = 2;
            menu.aiAggressionDropdown.value = 2;
            menu.aiExpansionDropdown.value = 3;
            menu.aiTargetPriorityDropdown.value = 2;
            menu.aiPlayerAutoDropdown.value = 1;
            menu.aiEnemyCountDropdown.value = 2;

            Assert.AreEqual(AIDifficultySetting.Brutal, AISettingsRuntimeState.Difficulty);
            Assert.AreEqual(AIStartingMoneySetting.High, AISettingsRuntimeState.StartingMoney);
            Assert.AreEqual(2f, AISettingsRuntimeState.IncomeMultiplier);
            Assert.AreEqual(AISpeedSetting.Fast, AISettingsRuntimeState.BuildSpeed);
            Assert.AreEqual(AISpeedSetting.Fast, AISettingsRuntimeState.UnitProductionSpeed);
            Assert.AreEqual(AIAttackGroupSizeSetting.Large, AISettingsRuntimeState.AttackGroupSize);
            Assert.AreEqual(AIAttackFrequencySetting.Frequent, AISettingsRuntimeState.AttackFrequency);
            Assert.AreEqual(AIAggressionSetting.Aggressive, AISettingsRuntimeState.Aggression);
            Assert.AreEqual(AIExpansionSetting.Fast, AISettingsRuntimeState.Expansion);
            Assert.AreEqual(AITargetPriority.Economy, AISettingsRuntimeState.TargetPriority);
            Assert.IsTrue(AISettingsRuntimeState.PlayerAutoAIEnabled);
            Assert.IsTrue(InitialUnitsRuntimeState.PlayerAutoModeEnabled);
            Assert.AreEqual(3, AISettingsRuntimeState.EnemyAICount);

            Assert.Greater(em.GetComponentData<FactionEconomyPolicy>(economy).IncomeMultiplier, 1f);
            Assert.Less(em.GetComponentData<AIBuildPlan>(buildPlan).BuildIntervalSeconds, 8f);
            Assert.Less(em.GetComponentData<AIProductionPlan>(productionPlan).UnitProductionIntervalSeconds, 6f);
            Assert.Greater(em.GetComponentData<AISquadPlan>(squadPlan).MaxUnits, 8);
            Assert.AreEqual((byte)AITargetPriority.Economy, em.GetComponentData<AITargetPrioritySetting>(targetPriority).Priority);
            Assert.AreEqual(1, em.GetComponentData<FactionEconomyPolicy>(playerEconomy).Enabled);
            Assert.AreEqual(1, em.GetComponentData<AIBuildPlan>(playerBuildPlan).Enabled);
            Assert.AreEqual(-999f, em.GetComponentData<AIBuildPlan>(playerBuildPlan).LastBuildTime);
            Assert.AreEqual(1, em.GetComponentData<AIProductionPlan>(playerProductionPlan).Enabled);
            Assert.AreEqual(-999f, em.GetComponentData<AIProductionPlan>(playerProductionPlan).LastProductionTime);
            Assert.AreEqual(1, em.GetComponentData<AISquadPlan>(playerSquadPlan).Enabled);
            Assert.AreEqual(1, controls[0].AIControlled);
        }
        finally
        {
            World.DefaultGameObjectInjectionWorld = previousWorld;
            world.Dispose();
        }
    }

    private MenuView CreateMenuView()
    {
        GameObject root = Track(new GameObject("MenuView_PlayModeTest"));
        MenuView menu = root.AddComponent<MenuView>();

        menu.panelMenu = CreatePanel("Panel_Menu", root.transform);
        menu.panelCamp = CreatePanel("Panel_Camp", root.transform);
        menu.panelGame = CreatePanel("Panel_Game", root.transform);
        menu.panelStats = CreatePanel("Panel_Stats", root.transform);

        menu.gamePanelFree = CreatePanel("Panel_Free", menu.panelGame.transform);
        menu.gamePanelSelect = CreatePanel("Panel_Selected", menu.panelGame.transform);
        menu.gamePanelMap = CreatePanel("Panel_Map", menu.panelGame.transform);
        menu.panelConfirm = CreatePanel("Panel_Confirm", root.transform);
        menu.panelWarning = CreatePanel("Panel_Warning", root.transform);

        menu.panelLoading = CreateChild("Panel_Loading", root.transform);
        menu.panelZoom = CreateChild("Panel_Zoom", root.transform);
        menu.panelTime = CreateChild("Panel_Time", root.transform);
        menu.panelCampButtons = CreateChild("Panel_Camp_Buttons", root.transform);
        CreateMoneyPanel(menu.panelGame.transform);
        CreateAutoModeButton(menu.panelGame.transform);
        menu.panelSettings = CreateSettingsPanel(root.transform, out TMP_Dropdown gameplaySpeedDropdown);
        menu.gameplaySpeedDropdown = gameplaySpeedDropdown;
        menu.aiDifficultyDropdown = CreateDropdown("Dropdown_AIDifficulty", menu.panelSettings.transform);
        menu.aiStartingMoneyDropdown = CreateDropdown("Dropdown_AIStartingMoney", menu.panelSettings.transform);
        menu.aiIncomeMultiplierDropdown = CreateDropdown("Dropdown_AIIncomeMultiplier", menu.panelSettings.transform);
        menu.aiBuildSpeedDropdown = CreateDropdown("Dropdown_AIBuildSpeed", menu.panelSettings.transform);
        menu.aiUnitProductionSpeedDropdown = CreateDropdown("Dropdown_AIUnitProductionSpeed", menu.panelSettings.transform);
        menu.aiAttackGroupSizeDropdown = CreateDropdown("Dropdown_AIAttackGroupSize", menu.panelSettings.transform);
        menu.aiAttackFrequencyDropdown = CreateDropdown("Dropdown_AIAttackFrequency", menu.panelSettings.transform);
        menu.aiAggressionDropdown = CreateDropdown("Dropdown_AIAggression", menu.panelSettings.transform);
        menu.aiExpansionDropdown = CreateDropdown("Dropdown_AIExpansion", menu.panelSettings.transform);
        menu.aiTargetPriorityDropdown = CreateDropdown("Dropdown_AITargetPriority", menu.panelSettings.transform);
        menu.aiPlayerAutoDropdown = CreateDropdown("Dropdown_AIPlayerAuto", menu.panelSettings.transform);
        menu.aiEnemyCountDropdown = CreateDropdown("Dropdown_AIEnemyCount", menu.panelSettings.transform);

        menu.buttonGame = CreateButton("Button_Game", root.transform);
        menu.buttonStats = CreateButton("Button_Stats", root.transform);
        menu.buttonBack = CreateButton("Button_Back", root.transform);
        menu.buttonSettings = CreateButton("Button_Settings", root.transform);
        menu.buttonMap = CreateButton("Button_Map", menu.panelGame.transform);
        menu.buttonCampAmmo = CreateButton("Button_Ammo", root.transform);
        menu.buttonCampSoldiers = CreateButton("Button_Soldiers", root.transform);
        menu.buttonCampVehicles = CreateButton("Button_Vehicles", root.transform);
        menu.buttonCampBuildings = CreateButton("Button_Buildings", root.transform);

        menu.panelCamp.gameObject.SetActive(false);
        menu.panelGame.gameObject.SetActive(false);
        menu.panelStats.gameObject.SetActive(false);
        menu.gamePanelSelect.gameObject.SetActive(false);
        menu.gamePanelMap.gameObject.SetActive(false);
        menu.panelConfirm.gameObject.SetActive(false);
        menu.panelWarning.gameObject.SetActive(false);

        return menu;
    }

    private static void InitMenu(MenuView menu, Camera camera)
    {
        var rtsCamera = new RtsCameraSystem();
        var rtsCameraRequests = new RtsCameraRequestSystem();
        var selectionUiCamera = new SelectionUiCameraSystem(rtsCamera, rtsCameraRequests);
        selectionUiCamera.Init(null, camera);
        menu.Init(
            new SelectionUiCommandSystem(),
            new SelectionUiReadModelSystem(),
            selectionUiCamera,
            new SelectionScreenMarkerSystem(),
            camera);
    }

    private static bool TryFindSelectionCommand(RtsSelectionCommandIntentKind kind)
    {
        var inputSystem = new RtsSelectionInputSystem();
        if (!inputSystem.TryGetCommandBuffers(
                out _,
                out DynamicBuffer<RtsSelectionCommandIntentRequestElement> requests,
                out _))
        {
            return false;
        }

        for (int i = 0; i < requests.Length; i++)
        {
            if (requests[i].Kind == kind)
                return true;
        }

        return false;
    }

    private static bool TryReadSelectionInputState(out RtsSelectionInputStateComponent state)
    {
        return new RtsSelectionInputStateSystem().TryRead(out _, out state);
    }

    private static void AssertAISettingsDropdownOptions(MenuView menu)
    {
        Assert.AreEqual(4, menu.aiDifficultyDropdown.options.Count);
        Assert.AreEqual("Brutal", menu.aiDifficultyDropdown.options[3].text);
        Assert.AreEqual(3, menu.aiStartingMoneyDropdown.options.Count);
        Assert.AreEqual(5, menu.aiIncomeMultiplierDropdown.options.Count);
        Assert.AreEqual("2x", menu.aiIncomeMultiplierDropdown.options[4].text);
        Assert.AreEqual(3, menu.aiBuildSpeedDropdown.options.Count);
        Assert.AreEqual(3, menu.aiUnitProductionSpeedDropdown.options.Count);
        Assert.AreEqual(3, menu.aiAttackGroupSizeDropdown.options.Count);
        Assert.AreEqual(3, menu.aiAttackFrequencyDropdown.options.Count);
        Assert.AreEqual(3, menu.aiAggressionDropdown.options.Count);
        Assert.AreEqual(4, menu.aiExpansionDropdown.options.Count);
        Assert.AreEqual(4, menu.aiTargetPriorityDropdown.options.Count);
        Assert.AreEqual(2, menu.aiPlayerAutoDropdown.options.Count);
        Assert.AreEqual(3, menu.aiEnemyCountDropdown.options.Count);
    }

    private Animator CreatePanel(string name, Transform parent)
    {
        GameObject panel = CreateChild(name, parent);
        return panel.AddComponent<Animator>();
    }

    private Button CreateButton(string name, Transform parent)
    {
        GameObject buttonObject = CreateChild(name, parent);
        buttonObject.AddComponent<Image>();
        return buttonObject.AddComponent<Button>();
    }

    private static Entity CreateSelectableUnit(EntityManager em, string sourceName, float3 position)
    {
        Entity entity = em.CreateEntity(
            typeof(Faction),
            typeof(UnitGrid),
            typeof(UnitMove),
            typeof(UnitFootprint),
            typeof(UnitMovementBehavior),
            typeof(UnitSourcePrefabKey),
            typeof(LocalTransform),
            typeof(LocalToWorld));

        em.SetComponentData(entity, new Faction { Id = 0 });
        em.SetComponentData(entity, new UnitGrid { Cell = new int2((int)math.round(position.x), (int)math.round(position.z)) });
        em.SetComponentData(entity, new UnitMove { Speed = 1f, WalkSpeed = 1f, RoadSpeedMultiplier = 1f, ArriveDistance = 0.1f });
        em.SetComponentData(entity, new UnitFootprint { Size = new int2(1, 1) });
        em.SetComponentData(entity, new UnitMovementBehavior());
        em.SetComponentData(entity, new UnitSourcePrefabKey { Value = new FixedString64Bytes(sourceName) });
        em.SetComponentData(entity, LocalTransform.FromPosition(position));
        em.SetComponentData(entity, new LocalToWorld { Value = float4x4.Translate(position) });
        return entity;
    }

    private void CreateMoneyPanel(Transform parent)
    {
        GameObject moneyButton = CreateChild("Button_Money", parent);
        moneyButton.AddComponent<Image>();
        moneyButton.AddComponent<Button>();

        GameObject amountText = CreateChild("AmountText", moneyButton.transform);
        amountText.AddComponent<TextMeshProUGUI>();
    }

    private void CreateAutoModeButton(Transform parent)
    {
        GameObject autoModeButton = CreateChild("Button_AutoMode", parent);
        autoModeButton.AddComponent<Image>();
        autoModeButton.AddComponent<Button>();

        GameObject label = CreateChild("Label_AutoMode", autoModeButton.transform);
        label.AddComponent<TextMeshProUGUI>();
        autoModeButton.SetActive(false);
    }

    private GameObject CreateSettingsPanel(Transform parent, out TMP_Dropdown dropdown)
    {
        GameObject settingsPanel = CreateChild("Panel_Settings", parent);
        settingsPanel.AddComponent<Animator>();

        GameObject dropdownObject = CreateChild("Dropdown_GameplaySpeed", settingsPanel.transform);
        dropdownObject.AddComponent<Image>();
        dropdown = dropdownObject.AddComponent<TMP_Dropdown>();

        GameObject label = CreateChild("Label", dropdownObject.transform);
        TMP_Text labelText = label.AddComponent<TextMeshProUGUI>();
        dropdown.captionText = labelText;
        settingsPanel.SetActive(false);
        return settingsPanel;
    }

    private TMP_Dropdown CreateDropdown(string name, Transform parent)
    {
        GameObject dropdownObject = CreateChild(name, parent);
        dropdownObject.AddComponent<Image>();
        TMP_Dropdown dropdown = dropdownObject.AddComponent<TMP_Dropdown>();

        GameObject label = CreateChild("Label", dropdownObject.transform);
        TMP_Text labelText = label.AddComponent<TextMeshProUGUI>();
        dropdown.captionText = labelText;
        return dropdown;
    }

    private GameObject CreateChild(string name, Transform parent)
    {
        GameObject child = new GameObject(name, typeof(RectTransform));
        child.transform.SetParent(parent, false);
        return child;
    }

    private static Transform FindDescendantByName(Transform root, string childName)
    {
        if (root == null || string.IsNullOrEmpty(childName))
            return null;

        if (root.name == childName)
            return root;

        for (int i = 0; i < root.childCount; i++)
        {
            Transform result = FindDescendantByName(root.GetChild(i), childName);
            if (result != null)
                return result;
        }

        return null;
    }

    private T Track<T>(T unityObject) where T : Object
    {
        _createdObjects.Add(unityObject);
        return unityObject;
    }
}
#endif
