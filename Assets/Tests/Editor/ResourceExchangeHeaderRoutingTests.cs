using System;
using System.Collections.Generic;
using NUnit.Framework;
using TMPro;
using Unity.Entities;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Game.Components;
using Game.Tactical.Contracts;
using Game.UI.Contracts;
using Game.UI.Runtime;
using Game.UI.Shell.Contracts.Ecs;
using Game.UI.Shell.Ecs;

public sealed class ResourceExchangeHeaderRoutingTests
{
    private const string MenuScenePath = "Assets/Game/Scenes/Menu.unity";

    public static void RunFocusedValidation()
    {
        int passed = 0;
        try
        {
            RunValidationStep(
                nameof(ResourceHeaderClick_EnqueuesOpenResourceExchangeAction),
                test => test.ResourceHeaderClick_EnqueuesOpenResourceExchangeAction(),
                ref passed);
            RunValidationStep(
                nameof(UiActionRequestSystem_OpenResourceExchangeRequiresEnabledExchange),
                test => test.UiActionRequestSystem_OpenResourceExchangeRequiresEnabledExchange(),
                ref passed);
            RunValidationStep(
                nameof(UiActionRequestSystem_OpenResourceExchangeRejectsEnemyOnlyCapability),
                test => test.UiActionRequestSystem_OpenResourceExchangeRejectsEnemyOnlyCapability(),
                ref passed);
            RunValidationStep(
                nameof(UiActionRequestSystem_IntroLockedTapIsConsumedWithoutStaleOpen),
                test => test.UiActionRequestSystem_IntroLockedTapIsConsumedWithoutStaleOpen(),
                ref passed);
            RunValidationStep(
                nameof(UiActionRequestSystem_CloseResourceExchangeHidesPopupAndSuppressesWorldClick),
                test => test.UiActionRequestSystem_CloseResourceExchangeHidesPopupAndSuppressesWorldClick(),
                ref passed);
            RunValidationStep(
                nameof(UiShellFlowSystem_ResourceExchangeHideRestoresMatchHudPopupState),
                test => test.UiShellFlowSystem_ResourceExchangeHideRestoresMatchHudPopupState(),
                ref passed);
            RunValidationStep(
                nameof(MenuSceneShell_InstallsResourceExchangePopup),
                test => test.MenuSceneShell_InstallsResourceExchangePopup(),
                ref passed);
            RunValidationStep(
                nameof(MenuSceneShell_DirectResourceExchangeCloseIsIdempotent),
                test => test.MenuSceneShell_DirectResourceExchangeCloseIsIdempotent(),
                ref passed);
            RunValidationStep(
                nameof(MenuSceneShell_PopupLayerClearRemovesResourceExchangeCloseListener),
                test => test.MenuSceneShell_PopupLayerClearRemovesResourceExchangeCloseListener(),
                ref passed);
            RunValidationStep(
                nameof(MenuSceneShell_RebindingRuntimeUiTransfersOpenResourceExchangePopup),
                test => test.MenuSceneShell_RebindingRuntimeUiTransfersOpenResourceExchangePopup(),
                ref passed);

            Debug.Log($"[ResourceExchangeHeaderRoutingValidation] result=Passed tests={passed}");
            ValidationExit.Exit(0);
        }
        catch (Exception exception)
        {
            Debug.LogError($"[ResourceExchangeHeaderRoutingValidation] result=Failed passed={passed}\n{exception}");
            ValidationExit.Exit(1);
        }
    }

    [TearDown]
    public void TearDown()
    {
        UiShellRuntimeGateway.Register(null);
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene);
    }

    [Test]
    public void ResourceHeaderClick_EnqueuesOpenResourceExchangeAction()
    {
        var gateway = new RecordingGateway();
        UiShellRuntimeGateway.Register(gateway);

        MainMenuPlayUI runtimeUi = new();
        GameObject header = CreateMatchHudHeaderContent();
        try
        {
            runtimeUi.BindMatchHudThreatJumpPanel(header);
            Button creditsButton = header.transform.Find("ResourceStrip/CreditsSlot").GetComponent<Button>();
            Button oilButton = header.transform.Find("ResourceStrip/OilSlot").GetComponent<Button>();
            Button fuelButton = header.transform.Find("ResourceStrip/FuelSlot").GetComponent<Button>();
            Button supplyButton = header.transform.Find("ResourceStrip/SupplySlot").GetComponent<Button>();

            Assert.NotNull(creditsButton, "Credits slot must be clickable for Resource Exchange access.");
            Assert.NotNull(oilButton, "Oil slot must be clickable for Resource Exchange access.");
            Assert.NotNull(fuelButton, "Fuel slot must be clickable for Resource Exchange access.");
            Assert.NotNull(supplyButton, "Supply slot must be clickable for Resource Exchange access.");
            TMP_Text materialsLabel =
                header.transform.Find("ResourceStrip/SupplySlot/Label").GetComponent<TMP_Text>();
            Assert.AreEqual("Materials", materialsLabel.text);
            Assert.IsTrue(materialsLabel.enableAutoSizing);
            Assert.GreaterOrEqual(materialsLabel.rectTransform.rect.width, 300f);

            oilButton.onClick.Invoke();

            Assert.AreEqual(1, gateway.ActionCount);
            Assert.AreEqual(UiActionKind.OpenResourceExchange, gateway.LastActionKind);
            Assert.AreEqual(0, gateway.LastActionPayloadId);
        }
        finally
        {
            runtimeUi.Dispose();
            UnityEngine.Object.DestroyImmediate(header);
        }
    }

    [Test]
    public void UiActionRequestSystem_OpenResourceExchangeRequiresEnabledExchange()
    {
        using World enabledWorld = new("ResourceExchangeHeaderRouting_Enabled");
        ResourceExchangeActionResult enabledResult = RunResourceExchangeAction(enabledWorld, UiActionKind.OpenResourceExchange, exchangeEnabled: true);
        Assert.AreEqual(0, enabledResult.ActionRequestCount, "OpenResourceExchange action should be consumed when exchange is enabled.");
        Assert.AreEqual(1, enabledResult.PopupRequestCount, "OpenResourceExchange should show the popup when exchange is enabled.");
        Assert.AreEqual(UiShellPopupKind.ResourceExchange, enabledResult.PopupKind);
        Assert.AreEqual(UiShellPopupIntent.Show, enabledResult.PopupIntent);
        Assert.IsTrue(enabledResult.WorldInputSuppressed, "Resource header click must suppress the matching world click.");

        using World disabledWorld = new("ResourceExchangeHeaderRouting_Disabled");
        ResourceExchangeActionResult disabledResult = RunResourceExchangeAction(disabledWorld, UiActionKind.OpenResourceExchange, exchangeEnabled: false);
        Assert.AreEqual(0, disabledResult.ActionRequestCount, "OpenResourceExchange action should be consumed when exchange is disabled.");
        Assert.AreEqual(0, disabledResult.PopupRequestCount, "OpenResourceExchange must not show the popup when exchange is disabled.");
        Assert.IsTrue(disabledResult.WorldInputSuppressed, "Disabled Resource Exchange clicks still need to suppress the matching world click.");
    }

    [Test]
    public void UiActionRequestSystem_OpenResourceExchangeRejectsEnemyOnlyCapability()
    {
        using World world = new(nameof(UiActionRequestSystem_OpenResourceExchangeRejectsEnemyOnlyCapability));
        EntityManager entityManager = world.EntityManager;
        Entity boundary = CreateUiBoundary(entityManager);
        Entity selectionInput = CreateSelectionInput(entityManager);
        CreateResourceExchange(entityManager, enabled: true, factionId: 2);
        entityManager.GetBuffer<UiActionRequestComponent>(boundary).Add(new UiActionRequestComponent
        {
            Kind = UiActionKind.OpenResourceExchange
        });

        SystemHandle system = world.CreateSystem<UiActionRequestSystem>();
        system.Update(world.Unmanaged);

        Assert.AreEqual(0, entityManager.GetBuffer<UiActionRequestComponent>(boundary).Length);
        Assert.AreEqual(0, entityManager.GetBuffer<UiShellPopupRequestComponent>(boundary).Length);
        RtsSelectionInputStateComponent inputState =
            entityManager.GetComponentData<RtsSelectionInputStateComponent>(selectionInput);
        Assert.AreEqual(1, inputState.IgnoreUiClickUntilRelease);
        Assert.AreEqual(1, inputState.IgnoreNextLeftMouseRelease);
    }

    [Test]
    public void UiActionRequestSystem_IntroLockedTapIsConsumedWithoutStaleOpen()
    {
        using World world = new(nameof(UiActionRequestSystem_IntroLockedTapIsConsumedWithoutStaleOpen));
        EntityManager entityManager = world.EntityManager;
        Entity boundary = CreateUiBoundary(entityManager, introInputLocked: true);
        Entity selectionInput = CreateSelectionInput(entityManager);
        CreateResourceExchange(entityManager, enabled: true);
        entityManager.GetBuffer<UiActionRequestComponent>(boundary).Add(new UiActionRequestComponent
        {
            Kind = UiActionKind.OpenResourceExchange
        });

        SystemHandle system = world.CreateSystem<UiActionRequestSystem>();
        system.Update(world.Unmanaged);

        Assert.AreEqual(0, entityManager.GetBuffer<UiActionRequestComponent>(boundary).Length);
        Assert.AreEqual(0, entityManager.GetBuffer<UiShellPopupRequestComponent>(boundary).Length);
        MatchIntroTransitionComponent intro =
            entityManager.GetComponentData<MatchIntroTransitionComponent>(boundary);
        intro.InputLocked = 0;
        entityManager.SetComponentData(boundary, intro);
        system.Update(world.Unmanaged);
        Assert.AreEqual(0, entityManager.GetBuffer<UiShellPopupRequestComponent>(boundary).Length);
        RtsSelectionInputStateComponent inputState =
            entityManager.GetComponentData<RtsSelectionInputStateComponent>(selectionInput);
        Assert.AreEqual(1, inputState.IgnoreUiClickUntilRelease);
    }

    [Test]
    public void UiActionRequestSystem_CloseResourceExchangeHidesPopupAndSuppressesWorldClick()
    {
        using World world = new("ResourceExchangeHeaderRouting_Close");
        ResourceExchangeActionResult result = RunResourceExchangeAction(world, UiActionKind.CloseResourceExchange, exchangeEnabled: true);

        Assert.AreEqual(0, result.ActionRequestCount, "CloseResourceExchange action should be consumed.");
        Assert.AreEqual(1, result.PopupRequestCount, "CloseResourceExchange must enqueue one popup hide request.");
        Assert.AreEqual(UiShellPopupKind.ResourceExchange, result.PopupKind);
        Assert.AreEqual(UiShellPopupIntent.Hide, result.PopupIntent);
        Assert.IsTrue(result.WorldInputSuppressed, "Resource Exchange close clicks must suppress the matching world click.");
    }

    [Test]
    public void UiShellFlowSystem_ResourceExchangeHideRestoresMatchHudPopupState()
    {
        using World world = new("ResourceExchangeHeaderRouting_FlowHide");
        EntityManager entityManager = world.EntityManager;
        Entity boundary = CreateShellFlowBoundary(entityManager);
        DynamicBuffer<UiShellPopupRequestComponent> popupRequests =
            entityManager.GetBuffer<UiShellPopupRequestComponent>(boundary);
        popupRequests.Add(new UiShellPopupRequestComponent
        {
            PopupKind = UiShellPopupKind.ResourceExchange,
            Intent = UiShellPopupIntent.Hide,
            PayloadId = 0
        });

        SystemHandle system = world.CreateSystem<UiShellFlowSystem>();
        system.Update(world.Unmanaged);

        UiShellActivePopupComponent activePopup =
            entityManager.GetComponentData<UiShellActivePopupComponent>(boundary);
        UiShellStateComponent shellState =
            entityManager.GetComponentData<UiShellStateComponent>(boundary);
        DynamicBuffer<UiShellPresentationCommandComponent> commands =
            entityManager.GetBuffer<UiShellPresentationCommandComponent>(boundary);

        popupRequests = entityManager.GetBuffer<UiShellPopupRequestComponent>(boundary);
        Assert.AreEqual(0, popupRequests.Length, "Popup hide request should be consumed.");
        Assert.AreEqual(0, activePopup.Visible, "Resource Exchange hide must clear the active popup visible flag.");
        Assert.AreEqual(UiShellMode.MatchHud, shellState.CurrentMode, "Closing the popup must restore the underlying Match HUD mode.");
        Assert.AreEqual(UiShellTransitionPhase.HidingPopup, shellState.Phase);
        Assert.AreEqual(1, commands.Length, "Popup hide should produce one presentation command.");
        Assert.AreEqual(UiShellCommandKind.HidePopup, commands[0].Kind);
        Assert.AreEqual(UiShellPopupKind.ResourceExchange, commands[0].PopupKind);
    }

    [Test]
    public void MenuSceneShell_InstallsResourceExchangePopup()
    {
        Scene scene = EditorSceneManager.OpenScene(MenuScenePath, OpenSceneMode.Single);
        UIShellContentView content = FindInScene<UIShellContentView>(scene);
        Assert.NotNull(content, "Menu scene must contain the shell content binder.");
        Assert.NotNull(content.ResourceExchangePopupPrefab, "Menu scene shell must serialize the Resource Exchange popup prefab.");
        Assert.AreEqual("POP12_ResourceExchangePopup", content.ResourceExchangePopupPrefab.name);

        var gateway = new RecordingGateway();
        UiShellRuntimeGateway.Register(gateway);
        MainMenuPlayUI runtimeUi = new();
        content.BindGameplayRuntimeDependencies(null, runtimeUi);

        content.PrepareForCommandSequence(new[]
        {
            new UiShellPresentationCommandModel(
                UiShellCommandKind.EnterMatchHud,
                UiShellRegionId.None,
                UIRoute.Match,
                UiShellMode.MatchHud,
                1),
            new UiShellPresentationCommandModel(
                UiShellCommandKind.ShowPopup,
                UiShellRegionId.PopupLayer,
                UIRoute.Match,
                UiShellMode.PopupOnly,
                2,
                UiShellPopupKind.ResourceExchange)
        });

        GameObject popup = AssertRegionHasChild(content.ShellView, UIShellRegionId.PopupLayer);
        Assert.AreEqual("POP12_ResourceExchangePopup", popup.name);
        ResourceExchangePopupView popupView = popup.GetComponent<ResourceExchangePopupView>();
        Assert.NotNull(popupView, "Installed Resource Exchange popup must expose its runtime view.");
        Assert.NotNull(popupView.CloseButton, "Installed Resource Exchange popup must expose its close button.");

        Canvas.ForceUpdateCanvases();
        Vector2 popupCenter = RectTransformUtility.WorldToScreenPoint(null, popup.transform.position);
        Assert.IsTrue(
            runtimeUi.IsPointerOverAnyGameplayUi(popupCenter, out string source),
            "Open Resource Exchange popup must block world input over its screen area.");
        Assert.AreEqual("ResourceExchangePopup", source);

        popupView.CloseButton.onClick.Invoke();

        Assert.AreEqual(UiActionKind.CloseResourceExchange, gateway.LastActionKind, "Close button must enqueue the typed close action.");
        AssertRegionIsEmpty(content.ShellView, UIShellRegionId.PopupLayer);
        bool hitAfterClose = runtimeUi.IsPointerOverAnyGameplayUi(popupCenter, out string sourceAfterClose);
        Assert.IsFalse(
            hitAfterClose && sourceAfterClose == "ResourceExchangePopup",
            "Closing Resource Exchange must remove the modal popup from gameplay UI hit testing.");
        runtimeUi.Dispose();
    }

    [Test]
    public void MenuSceneShell_DirectResourceExchangeCloseIsIdempotent()
    {
        Scene scene = EditorSceneManager.OpenScene(MenuScenePath, OpenSceneMode.Single);
        UIShellContentView content = FindInScene<UIShellContentView>(scene);
        Assert.NotNull(content);

        MainMenuPlayUI runtimeUi = new();
        try
        {
            content.BindGameplayRuntimeDependencies(null, runtimeUi);
            Assert.NotNull(content.InstallResourceExchangePopup());
            int installedVersion = content.ContentVersion;

            content.CloseResourceExchangePopup();
            int firstCloseVersion = content.ContentVersion;
            content.CloseResourceExchangePopup();

            AssertRegionIsEmpty(content.ShellView, UIShellRegionId.PopupLayer);
            Assert.AreEqual(installedVersion + 1, firstCloseVersion);
            Assert.AreEqual(firstCloseVersion, content.ContentVersion, "Closing an absent popup must not mutate shell content state.");
        }
        finally
        {
            runtimeUi.Dispose();
        }
    }

    [Test]
    public void MenuSceneShell_PopupLayerClearRemovesResourceExchangeCloseListener()
    {
        Scene scene = EditorSceneManager.OpenScene(MenuScenePath, OpenSceneMode.Single);
        UIShellContentView content = FindInScene<UIShellContentView>(scene);
        Assert.NotNull(content);

        var gateway = new RecordingGateway();
        UiShellRuntimeGateway.Register(gateway);
        MainMenuPlayUI runtimeUi = new();
        try
        {
            content.BindGameplayRuntimeDependencies(null, runtimeUi);
            GameObject popup = content.InstallResourceExchangePopup();
            ResourceExchangePopupView popupView = popup.GetComponent<ResourceExchangePopupView>();
            Assert.NotNull(popupView);
            Assert.NotNull(popupView.CloseButton);
            int installedVersion = content.ContentVersion;

            content.ClearRegion(UIShellRegionId.PopupLayer);
            popupView.CloseButton.onClick.Invoke();

            AssertRegionIsEmpty(content.ShellView, UIShellRegionId.PopupLayer);
            Assert.AreEqual(installedVersion + 1, content.ContentVersion);
            Assert.AreEqual(0, gateway.ActionCount, "A cleared popup must not retain its typed close listener.");
        }
        finally
        {
            runtimeUi.Dispose();
        }
    }

    [Test]
    public void MenuSceneShell_RebindingRuntimeUiTransfersOpenResourceExchangePopup()
    {
        Scene scene = EditorSceneManager.OpenScene(MenuScenePath, OpenSceneMode.Single);
        UIShellContentView content = FindInScene<UIShellContentView>(scene);
        Assert.NotNull(content);

        MainMenuPlayUI firstRuntimeUi = new();
        MainMenuPlayUI secondRuntimeUi = new();
        try
        {
            content.BindGameplayRuntimeDependencies(null, firstRuntimeUi);
            GameObject popup = content.InstallResourceExchangePopup();
            Canvas.ForceUpdateCanvases();
            Vector2 popupCenter = RectTransformUtility.WorldToScreenPoint(null, popup.transform.position);
            Assert.IsTrue(firstRuntimeUi.IsPointerOverAnyGameplayUi(popupCenter, out string firstSource));
            Assert.AreEqual("ResourceExchangePopup", firstSource);

            content.BindGameplayRuntimeDependencies(null, secondRuntimeUi);

            Assert.IsFalse(
                firstRuntimeUi.IsPointerOverAnyGameplayUi(popupCenter, out string staleSource) &&
                staleSource == "ResourceExchangePopup");
            Assert.IsTrue(secondRuntimeUi.IsPointerOverAnyGameplayUi(popupCenter, out string secondSource));
            Assert.AreEqual("ResourceExchangePopup", secondSource);
        }
        finally
        {
            firstRuntimeUi.Dispose();
            secondRuntimeUi.Dispose();
        }
    }

    private static ResourceExchangeActionResult RunResourceExchangeAction(
        World world,
        UiActionKind actionKind,
        bool exchangeEnabled)
    {
        EntityManager entityManager = world.EntityManager;
        Entity boundary = CreateUiBoundary(entityManager);
        Entity selectionInput = CreateSelectionInput(entityManager);
        if (exchangeEnabled)
            CreateResourceExchange(entityManager, enabled: true);
        else
            CreateResourceExchange(entityManager, enabled: false);

        DynamicBuffer<UiActionRequestComponent> actionRequests =
            entityManager.GetBuffer<UiActionRequestComponent>(boundary);
        actionRequests.Add(new UiActionRequestComponent
        {
            Kind = actionKind,
            PayloadId = 0
        });

        SystemHandle system = world.CreateSystem<UiActionRequestSystem>();
        system.Update(world.Unmanaged);

        DynamicBuffer<UiShellPopupRequestComponent> popupRequests =
            entityManager.GetBuffer<UiShellPopupRequestComponent>(boundary);
        RtsSelectionInputStateComponent inputState =
            entityManager.GetComponentData<RtsSelectionInputStateComponent>(selectionInput);

        return new ResourceExchangeActionResult(
            entityManager.GetBuffer<UiActionRequestComponent>(boundary).Length,
            popupRequests.Length,
            popupRequests.Length > 0 ? popupRequests[0].PopupKind : default,
            popupRequests.Length > 0 ? popupRequests[0].Intent : default,
            inputState.IgnoreUiClickUntilRelease != 0 &&
            inputState.IgnoreNextLeftMouseRelease != 0 &&
            inputState.PointerPressedOverUi != 0);
    }

    private static Entity CreateUiBoundary(EntityManager entityManager, bool introInputLocked = false)
    {
        Entity boundary = entityManager.CreateEntity(
            typeof(UiShellRootComponent),
            typeof(UiShellStateComponent),
            typeof(MatchIntroTransitionComponent),
            typeof(UiDiagnosticsOverlayComponent),
            typeof(UiMatchHudPassengerDrawerStateComponent),
            typeof(UiMatchHudSquadTrayStateComponent),
            typeof(UiBuildDrawerStateComponent));
        entityManager.AddBuffer<UiActionRequestComponent>(boundary);
        entityManager.AddBuffer<UiShellPopupRequestComponent>(boundary);
        entityManager.AddBuffer<UiShellRouteRequestComponent>(boundary);
        entityManager.AddBuffer<UiBuildCatalogRequestComponent>(boundary);
        entityManager.AddBuffer<UiBuildProductionRequestComponent>(boundary);
        entityManager.AddBuffer<UiBuildPrimaryRequestComponent>(boundary);
        entityManager.SetComponentData(boundary, new UiShellStateComponent
        {
            CurrentMode = UiShellMode.MatchHud,
            ActiveRoute = UIRoute.Match,
            IsTransitionRunning = 0
        });
        entityManager.SetComponentData(boundary, new MatchIntroTransitionComponent
        {
            InputLocked = introInputLocked ? (byte)1 : (byte)0
        });
        return boundary;
    }

    private static Entity CreateShellFlowBoundary(EntityManager entityManager)
    {
        Entity boundary = entityManager.CreateEntity(
            typeof(UiShellRootComponent),
            typeof(UiShellStateComponent),
            typeof(UiShellLoadingProgressComponent),
            typeof(MatchIntroTransitionComponent),
            typeof(UiShellActivePopupComponent));
        entityManager.SetComponentData(boundary, new UiShellStateComponent
        {
            CurrentMode = UiShellMode.MatchHud,
            ActiveRoute = UIRoute.Match,
            Phase = UiShellTransitionPhase.PopupVisible,
            IsTransitionRunning = 0
        });
        entityManager.SetComponentData(boundary, new UiShellActivePopupComponent
        {
            PopupKind = UiShellPopupKind.ResourceExchange,
            Visible = 1
        });
        entityManager.AddBuffer<UiShellLoadingProgressRequestComponent>(boundary);
        entityManager.AddBuffer<UiShellRouteRequestComponent>(boundary);
        entityManager.AddBuffer<UiShellRouteHistoryComponent>(boundary);
        entityManager.AddBuffer<UiShellPopupRequestComponent>(boundary);
        entityManager.AddBuffer<UiShellPresentationCommandComponent>(boundary);
        entityManager.AddBuffer<UiShellTransitionCompleteComponent>(boundary);
        return boundary;
    }

    private static Entity CreateSelectionInput(EntityManager entityManager)
    {
        Entity selectionInput = entityManager.CreateEntity(
            typeof(RtsSelectionInputStateComponent),
            typeof(RtsSelectionInputRequestQueueComponent));
        entityManager.AddBuffer<RtsSelectionPointerRequestElement>(selectionInput);
        entityManager.AddBuffer<RtsSelectionCommandIntentRequestElement>(selectionInput);
        entityManager.AddBuffer<RtsSelectionCommandResultElement>(selectionInput);
        return selectionInput;
    }

    private static Entity CreateResourceExchange(
        EntityManager entityManager,
        bool enabled,
        byte factionId = FactionIdentity.PlayerFactionId)
    {
        Entity exchange = entityManager.CreateEntity(
            typeof(ResourceExchangeEnabledComponent),
            typeof(ResourceExchangeRequestQueueComponent));
        entityManager.SetComponentData(exchange, new ResourceExchangeEnabledComponent
        {
            Enabled = enabled ? (byte)1 : (byte)0,
            FactionId = factionId,
            AllowRush = 1,
            MaxQueueItems = 6,
            ScenarioTag = new Unity.Collections.FixedString64Bytes("mission.header-routing")
        });
        entityManager.AddBuffer<ResourceExchangeRequestComponent>(exchange);
        DynamicBuffer<ResourceExchangeRecipeComponent> recipes =
            entityManager.AddBuffer<ResourceExchangeRecipeComponent>(exchange);
        recipes.Add(new ResourceExchangeRecipeComponent
        {
            RecipeId = new Unity.Collections.FixedString128Bytes("credits-to-materials"),
            Enabled = 1,
            MissionTag = new Unity.Collections.FixedString64Bytes("mission.header-routing")
        });
        return exchange;
    }

    private static GameObject CreateMatchHudHeaderContent()
    {
        GameObject header = new("HeaderContent", typeof(RectTransform));
        GameObject strip = new("ResourceStrip", typeof(RectTransform));
        strip.transform.SetParent(header.transform, false);

        CreateResourceSlot(strip.transform, "CreditsSlot");
        CreateResourceSlot(strip.transform, "OilSlot");
        CreateResourceSlot(strip.transform, "FuelSlot");
        CreateResourceSlot(strip.transform, "SupplySlot");
        CreateResourceSlot(strip.transform, "CivilianRiskSlot");
        return header;
    }

    private static void CreateResourceSlot(Transform parent, string name)
    {
        GameObject slot = new(name, typeof(RectTransform), typeof(Image));
        slot.transform.SetParent(parent, false);
        slot.GetComponent<RectTransform>().sizeDelta = new Vector2(300f, 80f);
        slot.GetComponent<Image>().raycastTarget = true;
        GameObject label = new("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
        label.transform.SetParent(slot.transform, false);
        RectTransform labelRect = label.GetComponent<RectTransform>();
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.sizeDelta = Vector2.zero;
        label.GetComponent<TMP_Text>().text = name == "SupplySlot" ? "Supply" : name;
    }

    private static T FindInScene<T>(Scene scene) where T : UnityEngine.Object
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            T component = root.GetComponentInChildren<T>(true);
            if (component != null)
                return component;
        }

        return null;
    }

    private static GameObject AssertRegionHasChild(UIShellView shellView, UIShellRegionId regionId)
    {
        Assert.IsTrue(shellView.TryGetRegion(regionId, out UIShellRegionView region), $"Shell must expose {regionId}.");
        Assert.NotNull(region.ContentRoot, $"{regionId} must expose a content root.");
        Assert.Greater(region.ContentRoot.childCount, 0, $"{regionId} must contain installed content.");
        return region.ContentRoot.GetChild(0).gameObject;
    }

    private static void AssertRegionIsEmpty(UIShellView shellView, UIShellRegionId regionId)
    {
        Assert.IsTrue(shellView.TryGetRegion(regionId, out UIShellRegionView region), $"Shell must expose {regionId}.");
        Assert.NotNull(region.ContentRoot, $"{regionId} must expose a content root.");
        Assert.AreEqual(0, region.ContentRoot.childCount, $"{regionId} must be empty.");
    }

    private static void RunValidationStep(string name, Action<ResourceExchangeHeaderRoutingTests> action, ref int passed)
    {
        var test = new ResourceExchangeHeaderRoutingTests();
        try
        {
            action(test);
            passed++;
        }
        finally
        {
            test.TearDown();
        }
    }

    private readonly struct ResourceExchangeActionResult
    {
        public readonly int ActionRequestCount;
        public readonly int PopupRequestCount;
        public readonly UiShellPopupKind PopupKind;
        public readonly UiShellPopupIntent PopupIntent;
        public readonly bool WorldInputSuppressed;

        public ResourceExchangeActionResult(
            int actionRequestCount,
            int popupRequestCount,
            UiShellPopupKind popupKind,
            UiShellPopupIntent popupIntent,
            bool worldInputSuppressed)
        {
            ActionRequestCount = actionRequestCount;
            PopupRequestCount = popupRequestCount;
            PopupKind = popupKind;
            PopupIntent = popupIntent;
            WorldInputSuppressed = worldInputSuppressed;
        }
    }

    private sealed class RecordingGateway : IUiShellRuntimeGateway
    {
        public int ActionCount { get; private set; }
        public UiActionKind LastActionKind { get; private set; }
        public int LastActionPayloadId { get; private set; }

        public bool TryEnqueueRouteRequest(UiShellRouteIntent intent, UIRoute route, bool pushHistory) => false;

        public bool TryEnqueueUiAction(UiActionKind kind, int payloadId)
        {
            ActionCount++;
            LastActionKind = kind;
            LastActionPayloadId = payloadId;
            return true;
        }

        public bool TryEnqueueAssistantCommandIntent(UiAssistantCommandIntentKind kind, bool fromTakeover) => false;
        public bool TryReadLoadingProgress(out UiShellLoadingProgressModel loading) { loading = default; return false; }
        public bool TrySetLoadingProgress(float progress01, string status, bool complete) => false;
        public bool TryReadDiagnosticsOverlay(out UiDiagnosticsOverlayModel diagnostics) { diagnostics = UiDiagnosticsOverlayModel.Default; return false; }
        public bool TryReadShellState(out UiShellStateModel state) { state = default; return false; }
        public bool TryReadCommanderProfile(out UiShellCommanderProfileModel profile) { profile = default; return false; }
        public bool TryReadMainMenuResources(out UiShellMainMenuResourcesModel resources) { resources = default; return false; }
        public bool TryReadMissionResult(out UiMissionResultPopupModel result) { result = UiMissionResultPopupModel.VictoryDefault; return false; }
        public bool TryReadMatchHudSelection(out UiMatchHudSelectionPanelModel selection) { selection = UiMatchHudSelectionPanelModel.Hidden; return false; }
        public bool TryReadMatchHudCommandState(out UiMatchHudCommandStateModel commandState) { commandState = default; return false; }
        public bool TryReadMatchHudHeader(out UiMatchHudHeaderModel header) { header = UiMatchHudHeaderModel.Default; return false; }
        public bool TryReadMatchHudStatusSurfaces(out UiMatchHudStatusSurfacesModel statusSurfaces) { statusSurfaces = UiMatchHudStatusSurfacesModel.Default; return false; }
        public bool TryReadMatchHudAssistantPanel(out UiAssistantPanelModel assistantPanel) { assistantPanel = UiAssistantPanelModel.Empty; return false; }
        public bool TryReadMatchHudAssistantHighlight(out UiAssistantHighlightModel assistantHighlight) { assistantHighlight = UiAssistantHighlightModel.Empty; return false; }
        public bool TryReadMatchHudMinimap(out UiMatchHudMinimapModel minimap) { minimap = UiMatchHudMinimapModel.Default; return false; }
        public bool TryReadMatchHudPassengerDrawer(out UiMatchHudPassengerDrawerModel passengerDrawer) { passengerDrawer = UiMatchHudPassengerDrawerModel.Hidden; return false; }
        public bool TryReadMatchHudSquadTray(out UiMatchHudSquadTrayModel squadTray) { squadTray = UiMatchHudSquadTrayModel.Default; return false; }
        public bool TryReadBuildDrawer(out UiBuildDrawerModel drawer) { drawer = UiBuildDrawerModel.Empty; return false; }
        public bool TryReadResourceExchange(out UiResourceExchangeModel exchange) { exchange = UiResourceExchangeModel.Empty; return false; }
        public bool TryReadBuildPlacementConfirmationBar(out UiBuildPlacementConfirmationBarModel placementBar) { placementBar = UiBuildPlacementConfirmationBarModel.Hidden; return false; }
        public bool TryReadArmoryCategory(out ArmoryCatalogCategory category) { category = ArmoryCatalogCategory.Characters; return false; }
        public bool TryEnqueueArmoryCategory(ArmoryCatalogCategory category) => false;
        public bool TryConsumePresentationCommands(List<UiShellPresentationCommandModel> commands) => false;
        public bool TryEnqueueTransitionComplete(UiShellTransitionCompleteModel completion) => false;
    }
}
