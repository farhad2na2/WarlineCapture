using System;
using System.Collections.Generic;
using Game.UI.Contracts;
using Game.UI.Runtime;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public sealed class ResourceExchangePopupPrefabTests
{
    private const string PrefabPath = "Assets/Game/Prefabs/UI/Shell/Popups/POP12_ResourceExchangePopup.prefab";
    private const string ApprovedSpriteRoot = "Assets/Game/Art/UI/Generated/ResourceExchange/LayeredOneGo/";
    private const string CanonicalResourceSpriteRoot = "Assets/Game/Art/UI/Resources/";

    public static void RunFocusedValidation()
    {
        int passed = 0;
        try
        {
            RunValidationStep(
                nameof(ResourceExchangePopupPrefab_ExposesRequiredViewReferences),
                test => test.ResourceExchangePopupPrefab_ExposesRequiredViewReferences(),
                ref passed);
            RunValidationStep(
                nameof(ResourceExchangePopupPrefab_RecipeCardsAndQueueRowsExposeButtons),
                test => test.ResourceExchangePopupPrefab_RecipeCardsAndQueueRowsExposeButtons(),
                ref passed);
            RunValidationStep(
                nameof(ResourceExchangePopupPrefab_UsesOnlySeparatedResourceExchangeSprites),
                test => test.ResourceExchangePopupPrefab_UsesOnlySeparatedResourceExchangeSprites(),
                ref passed);
            RunValidationStep(
                nameof(ResourceExchangePopupPrefab_UsesLiveOxaniumTmpTextOnly),
                test => test.ResourceExchangePopupPrefab_UsesLiveOxaniumTmpTextOnly(),
                ref passed);
            RunValidationStep(
                nameof(ResourceExchangePopupPrefab_HasStableNamedInteractionPaths),
                test => test.ResourceExchangePopupPrefab_HasStableNamedInteractionPaths(),
                ref passed);
            RunValidationStep(
                nameof(ResourceExchangePopupRuntimeView_ButtonsEnqueueTypedResourceExchangeActions),
                test => test.ResourceExchangePopupRuntimeView_ButtonsEnqueueTypedResourceExchangeActions(),
                ref passed);
            RunValidationStep(
                nameof(ResourceExchangePopupRuntimeView_DisablingNewestOverlappingInstanceRefreshesPrevious),
                test => test.ResourceExchangePopupRuntimeView_DisablingNewestOverlappingInstanceRefreshesPrevious(),
                ref passed);

            Debug.Log($"[ResourceExchangePopupPrefabValidation] result=Passed tests={passed}");
            ValidationExit.Exit(0);
        }
        catch (Exception exception)
        {
            Debug.LogError($"[ResourceExchangePopupPrefabValidation] result=Failed passed={passed}\n{exception}");
            ValidationExit.Exit(1);
        }
    }

    [TearDown]
    public void TearDown()
    {
        UiShellRuntimeGateway.Register(null);
    }

    [Test]
    public void ResourceExchangePopupPrefab_ExposesRequiredViewReferences()
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        Assert.NotNull(prefab, $"Missing Resource Exchange popup prefab at {PrefabPath}.");

        ResourceExchangePopupView view = prefab.GetComponent<ResourceExchangePopupView>();
        Assert.NotNull(view, "Resource Exchange popup must own ResourceExchangePopupView.");
        ResourceExchangePopupRuntimeView runtimeView = prefab.GetComponent<ResourceExchangePopupRuntimeView>();
        Assert.NotNull(runtimeView, "Resource Exchange popup must own ResourceExchangePopupRuntimeView.");
        Assert.AreSame(view, runtimeView.View, "Runtime presenter must target the serialized Resource Exchange view.");
        Assert.NotNull(view.CloseButton, "Close button must be serialized.");
        Assert.NotNull(view.ExportTabButton, "Export tab must be serialized.");
        Assert.NotNull(view.ImportTabButton, "Import tab must be serialized.");
        Assert.NotNull(view.ConfirmButton, "Confirm button must be serialized.");
        Assert.NotNull(view.AmountDecreaseButton, "Amount decrease button must be serialized.");
        Assert.NotNull(view.AmountIncreaseButton, "Amount increase button must be serialized.");
        Assert.NotNull(view.RushAllButton, "Rush All button must be serialized.");
        Assert.NotNull(view.ClearCompletedButton, "Clear Completed button must be serialized.");
        Assert.NotNull(view.RecipeContentRoot, "Recipe content root must be serialized.");
        Assert.NotNull(view.RecipeCardTemplate, "Recipe card template must be serialized.");
        Assert.NotNull(view.QueueContentRoot, "Queue content root must be serialized.");
        Assert.NotNull(view.QueueRowTemplate, "Queue row template must be serialized.");
        Assert.That(view.StaticRecipeCards.Length, Is.GreaterThanOrEqualTo(6), "Prefab must expose six route card views.");
        Assert.That(view.StaticQueueRows.Length, Is.GreaterThanOrEqualTo(4), "Prefab must expose four exchange queue rows.");
    }

    [Test]
    public void ResourceExchangePopupPrefab_RecipeCardsAndQueueRowsExposeButtons()
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        Assert.NotNull(prefab);

        ResourceExchangePopupView view = prefab.GetComponent<ResourceExchangePopupView>();
        Assert.NotNull(view);

        for (int i = 0; i < view.StaticRecipeCards.Length; i++)
        {
            ResourceExchangeRecipeCardView card = view.StaticRecipeCards[i];
            Assert.NotNull(card, $"Recipe card {i} must be serialized.");
            Assert.NotNull(card.SelectionButton, $"Recipe card {i} must expose its selection button.");
            Assert.NotNull(card.FrameImage, $"Recipe card {i} must expose its frame image.");
            Assert.NotNull(card.ThumbnailImage, $"Recipe card {i} must expose its thumbnail image.");
            Assert.NotNull(card.TitleText, $"Recipe card {i} must expose its title text.");
        }

        for (int i = 0; i < view.StaticQueueRows.Length; i++)
        {
            ResourceExchangeQueueItemView row = view.StaticQueueRows[i];
            Assert.NotNull(row, $"Queue row {i} must be serialized.");
            Assert.NotNull(row.RushButton, $"Queue row {i} must expose its rush button.");
            Assert.NotNull(row.CancelButton, $"Queue row {i} must expose its cancel button.");
            Assert.NotNull(row.ProgressFillImage, $"Queue row {i} must expose its progress fill.");
            Assert.NotNull(row.NameText, $"Queue row {i} must expose its name text.");
        }
    }

    [Test]
    public void ResourceExchangePopupPrefab_UsesOnlySeparatedResourceExchangeSprites()
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        Assert.NotNull(prefab);

        Image[] images = prefab.GetComponentsInChildren<Image>(true);
        Assert.That(images.Length, Is.GreaterThan(20), "Prefab should be built from reusable Image layers.");
        for (int i = 0; i < images.Length; i++)
        {
            Sprite sprite = images[i].sprite;
            if (sprite == null)
                continue;

            string path = AssetDatabase.GetAssetPath(sprite);
            Assert.IsTrue(
                path.StartsWith(ApprovedSpriteRoot, System.StringComparison.Ordinal) ||
                path.StartsWith(CanonicalResourceSpriteRoot, System.StringComparison.Ordinal),
                $"{images[i].name} uses {path}; Resource Exchange popup must not use target screenshots or unrelated sprite folders.");
        }
    }

    [Test]
    public void ResourceExchangePopupPrefab_UsesLiveOxaniumTmpTextOnly()
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        Assert.NotNull(prefab);

        Text[] legacyText = prefab.GetComponentsInChildren<Text>(true);
        Assert.AreEqual(0, legacyText.Length, "POP-12 must use live TMP text only; legacy UnityEngine.UI.Text is not allowed.");

        TMP_Text[] texts = prefab.GetComponentsInChildren<TMP_Text>(true);
        Assert.That(texts.Length, Is.GreaterThan(40), "POP-12 must expose live TMP labels, values, reasons, and queue text.");
        for (int i = 0; i < texts.Length; i++)
        {
            TMP_Text text = texts[i];
            Assert.NotNull(text.font, $"{GetHierarchyPath(text.transform)} must have a TMP font asset.");
            StringAssert.Contains(
                "Oxanium",
                text.font.name,
                $"{GetHierarchyPath(text.transform)} uses {text.font.name}; POP-12 text must use the Oxanium TMP family.");
            Assert.IsFalse(
                text.raycastTarget,
                $"{GetHierarchyPath(text.transform)} must not intercept popup button or card clicks.");
        }
    }

    [Test]
    public void ResourceExchangePopupPrefab_HasStableNamedInteractionPaths()
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        Assert.NotNull(prefab);

        AssertHasButton(prefab.transform, "ResourceExchangeRoot/Header/CloseButton");
        AssertHasButton(prefab.transform, "ResourceExchangeRoot/ExportTab");
        AssertHasButton(prefab.transform, "ResourceExchangeRoot/ImportTab");
        AssertHasButton(prefab.transform, "ResourceExchangeRoot/DetailPanel/AmountStepper/AmountMinus");
        AssertHasButton(prefab.transform, "ResourceExchangeRoot/DetailPanel/AmountStepper/AmountPlus");
        AssertHasButton(prefab.transform, "ResourceExchangeRoot/DetailPanel/ConfirmButton");
        AssertHasButton(prefab.transform, "ResourceExchangeRoot/ExchangeQueuePanel/RushAllButton");
        AssertHasButton(prefab.transform, "ResourceExchangeRoot/ExchangeQueuePanel/ClearCompletedButton");

        for (int i = 1; i <= 6; i++)
        {
            Transform card = AssertHasTransform(prefab.transform, $"ResourceExchangeRoot/RecipeCards/RecipeCard{i}");
            Assert.NotNull(card.GetComponent<ResourceExchangeRecipeCardView>(), $"RecipeCard{i} must carry ResourceExchangeRecipeCardView.");
            Assert.NotNull(card.GetComponent<Button>(), $"RecipeCard{i} must be directly selectable.");
        }

        for (int i = 1; i <= 4; i++)
        {
            Transform row = AssertHasTransform(prefab.transform, $"ResourceExchangeRoot/ExchangeQueuePanel/Rows/QueueRow{i}");
            Assert.NotNull(row.GetComponent<ResourceExchangeQueueItemView>(), $"QueueRow{i} must carry ResourceExchangeQueueItemView.");
            AssertHasButton(row, "RushButton");
            AssertHasButton(row, "CancelButton");
        }

        Transform lockedCard = AssertHasTransform(prefab.transform, "ResourceExchangeRoot/RecipeCards/RecipeCard6");
        Assert.IsTrue(lockedCard.Find("DisabledOverlay").gameObject.activeSelf, "Locked route card must expose a disabled overlay.");
        Assert.IsTrue(lockedCard.Find("Lock").gameObject.activeSelf, "Locked route card must expose a lock icon.");
        Assert.IsTrue(lockedCard.Find("Warning").gameObject.activeSelf, "Locked route card must expose a warning icon.");
    }

    [Test]
    public void ResourceExchangePopupRuntimeView_ButtonsEnqueueTypedResourceExchangeActions()
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        Assert.NotNull(prefab);

        var gateway = new RecordingGateway(CreateRuntimeButtonModel());
        UiShellRuntimeGateway.Register(gateway);
        GameObject instance = UnityEngine.Object.Instantiate(prefab);
        try
        {
            instance.SetActive(true);
            ResourceExchangePopupView view = instance.GetComponent<ResourceExchangePopupView>();
            ResourceExchangePopupRuntimeView runtimeView = instance.GetComponent<ResourceExchangePopupRuntimeView>();
            Assert.NotNull(view);
            Assert.NotNull(runtimeView);
            runtimeView.ConfigureForTests(view);
            runtimeView.SendMessage("OnEnable");

            view.ExportTabButton.onClick.Invoke();
            AssertLastAction(gateway, UiActionKind.ResourceExchangeTab, (int)UiResourceExchangeTabKind.Export);
            view.ImportTabButton.onClick.Invoke();
            AssertLastAction(gateway, UiActionKind.ResourceExchangeTab, (int)UiResourceExchangeTabKind.Import);
            view.AmountDecreaseButton.onClick.Invoke();
            AssertLastAction(gateway, UiActionKind.ResourceExchangeAmountDecrease, 0);
            view.AmountIncreaseButton.onClick.Invoke();
            AssertLastAction(gateway, UiActionKind.ResourceExchangeAmountIncrease, 0);
            view.ConfirmButton.onClick.Invoke();
            AssertLastAction(gateway, UiActionKind.ResourceExchangeConfirm, 0);
            view.RushAllButton.onClick.Invoke();
            AssertLastAction(gateway, UiActionKind.ResourceExchangeRushAll, 0);
            view.ClearCompletedButton.onClick.Invoke();
            AssertLastAction(gateway, UiActionKind.ResourceExchangeClearCompleted, 0);

            view.StaticRecipeCards[2].SelectionButton.onClick.Invoke();
            AssertLastAction(gateway, UiActionKind.ResourceExchangeRecipe, 2);
            view.StaticQueueRows[0].RushButton.onClick.Invoke();
            AssertLastAction(gateway, UiActionKind.ResourceExchangeQueueRush, 401);
            view.StaticQueueRows[1].CancelButton.onClick.Invoke();
            AssertLastAction(gateway, UiActionKind.ResourceExchangeQueueCancel, 402);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(instance);
        }
    }

    [Test]
    public void ResourceExchangePopupRuntimeView_DisablingNewestOverlappingInstanceRefreshesPrevious()
    {
        var gateway = new RecordingGateway(CreateRuntimeButtonModel());
        UiShellRuntimeGateway.Register(gateway);

        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        Assert.NotNull(prefab);
        GameObject firstObject = UnityEngine.Object.Instantiate(prefab);
        GameObject secondObject = UnityEngine.Object.Instantiate(prefab);
        firstObject.name = "ResourceExchangePopup_First";
        secondObject.name = "ResourceExchangePopup_Second";
        firstObject.SetActive(false);
        secondObject.SetActive(false);
        ResourceExchangePopupRuntimeView firstRuntimeView = firstObject.GetComponent<ResourceExchangePopupRuntimeView>();
        ResourceExchangePopupRuntimeView secondRuntimeView = secondObject.GetComponent<ResourceExchangePopupRuntimeView>();
        firstRuntimeView.ConfigureForTests(firstObject.GetComponent<ResourceExchangePopupView>());
        secondRuntimeView.ConfigureForTests(secondObject.GetComponent<ResourceExchangePopupView>());

        try
        {
            firstObject.SetActive(true);
            firstRuntimeView.SendMessage("OnEnable");
            secondObject.SetActive(true);
            secondRuntimeView.SendMessage("OnEnable");
            firstRuntimeView.View.Show();
            secondRuntimeView.View.Show();
            secondObject.SetActive(false);
            secondRuntimeView.SendMessage("OnDisable");
            Assert.IsTrue(firstRuntimeView.View.IsOpen, "The fallback popup fixture must be open.");
            int readsBeforeDirectRefresh = gateway.ResourceExchangeReadCount;
            firstRuntimeView.RefreshNow(force: true);
            Assert.AreEqual(
                readsBeforeDirectRefresh + 1,
                gateway.ResourceExchangeReadCount,
                "The fallback popup fixture must be directly refreshable before routing is tested.");
            int readsBeforeRefresh = gateway.ResourceExchangeReadCount;

            ResourceExchangePopupRuntimeView.RefreshActiveView();

            Assert.IsTrue(
                ResourceExchangePopupRuntimeView.IsActiveViewForTests(firstRuntimeView),
                "Presentation refresh must restore the previous enabled popup as its active target.");
            Assert.IsTrue(firstRuntimeView.isActiveAndEnabled);
            Assert.AreEqual(
                readsBeforeRefresh + 1,
                gateway.ResourceExchangeReadCount,
                "Disabling the newest overlapping popup must restore presentation refreshes to the previous enabled popup.");
        }
        finally
        {
            firstRuntimeView.SendMessage("OnDisable");
            UnityEngine.Object.DestroyImmediate(secondObject);
            UnityEngine.Object.DestroyImmediate(firstObject);
        }
    }

    private static void RunValidationStep(string name, Action<ResourceExchangePopupPrefabTests> action, ref int passed)
    {
        var test = new ResourceExchangePopupPrefabTests();
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

    private static Transform AssertHasTransform(Transform root, string path)
    {
        Transform transform = root.Find(path);
        Assert.NotNull(transform, $"Missing stable POP-12 object path {path}.");
        return transform;
    }

    private static void AssertHasButton(Transform root, string path)
    {
        Transform transform = AssertHasTransform(root, path);
        Assert.NotNull(transform.GetComponent<Button>(), $"{path} must carry a Button.");
    }

    private static string GetHierarchyPath(Transform transform)
    {
        if (transform == null)
            return string.Empty;

        string path = transform.name;
        while (transform.parent != null)
        {
            transform = transform.parent;
            path = transform.name + "/" + path;
        }

        return path;
    }

    private static void AssertLastAction(RecordingGateway gateway, UiActionKind kind, int payloadId)
    {
        Assert.AreEqual(kind, gateway.LastActionKind);
        Assert.AreEqual(payloadId, gateway.LastActionPayloadId);
    }

    private static UiResourceExchangeModel CreateRuntimeButtonModel()
    {
        UiResourceExchangeRecipeCardModel recipe0 = new(true, true, true, false, false, 0, "oil_materials", "OIL TO MATERIALS", "100 OIL", "300 MATERIALS", "00:45", string.Empty);
        UiResourceExchangeRecipeCardModel recipe1 = new(true, true, false, false, false, 1, "materials_oil", "MATERIALS TO OIL", "100 MATERIALS", "15 OIL", "00:45", string.Empty);
        UiResourceExchangeRecipeCardModel recipe2 = new(true, true, false, false, false, 2, "oil_fuel", "OIL TO FUEL", "100 OIL", "33 FUEL", "00:45", string.Empty);
        UiResourceExchangeRecipeCardModel recipe3 = new(true, true, false, false, false, 3, "fuel_oil", "RECOVER OIL", "100 FUEL", "120 OIL", "01:30", string.Empty);
        UiResourceExchangeRecipeCardModel recipe4 = new(true, true, false, false, false, 4, "fuel_materials", "RECOVER MATERIALS", "100 FUEL", "180 MATERIALS", "01:30", string.Empty);
        UiResourceExchangeRecipeCardModel recipe5 = new(true, false, false, true, true, 5, "locked", "LOCKED ROUTE", "LOCKED", "SCENARIO GATED", "--:--", "LOCKED");
        UiResourceExchangeDetailModel detail = new("oil_materials", "Convert Oil to Materials", "EXPORT", "1 OIL -> 3 MATERIALS", "100", "100 OIL", "300 MATERIALS", "00:45", "Requires storage", "Confirm to start a timed logistics exchange.", true, false);
        UiResourceExchangeQueueRowModel row0 = new(true, true, true, false, false, 401, 0, UiResourceExchangeQueueStateKind.InProgress, "1", "Convert Oil to Materials", "100 OIL", "300 MATERIALS", "00:11", "65%", "IN PROGRESS", 0.65f);
        UiResourceExchangeQueueRowModel row1 = new(true, false, true, false, false, 402, 1, UiResourceExchangeQueueStateKind.Pending, "2", "Convert Oil to Fuel", "100 OIL", "33 FUEL", "00:30", "0%", "QUEUED", 0f);
        UiResourceExchangeQueueRowModel row2 = new(true, false, true, false, false, 403, 2, UiResourceExchangeQueueStateKind.Pending, "3", "Convert Materials to Oil", "100 MATERIALS", "15 OIL", "00:40", "0%", "QUEUED", 0f);
        UiResourceExchangeQueueRowModel row3 = new(true, false, false, true, false, 404, 3, UiResourceExchangeQueueStateKind.Completed, "4", "Recover Materials", "100 FUEL", "180 MATERIALS", "DONE", "100%", "COMPLETE", 1f);

        return new UiResourceExchangeModel(
            10,
            UiResourceExchangeTabKind.Export,
            0,
            3,
            3,
            4,
            3,
            1,
            6,
            "4/6",
            "180",
            "620",
            "310",
            "7",
            true,
            true,
            true,
            detail,
            6,
            recipe0,
            recipe1,
            recipe2,
            recipe3,
            recipe4,
            recipe5,
            default,
            4,
            row0,
            row1,
            row2,
            row3);
    }

    private sealed class RecordingGateway : IUiShellRuntimeGateway
    {
        private readonly UiResourceExchangeModel exchangeModel;

        public UiActionKind LastActionKind { get; private set; }
        public int LastActionPayloadId { get; private set; }
        public int ResourceExchangeReadCount { get; private set; }

        public RecordingGateway(UiResourceExchangeModel exchangeModel)
        {
            this.exchangeModel = exchangeModel;
        }

        public bool TryEnqueueRouteRequest(UiShellRouteIntent intent, UIRoute route, bool pushHistory) => false;

        public bool TryEnqueueUiAction(UiActionKind kind, int payloadId)
        {
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
        public bool TryReadResourceExchange(out UiResourceExchangeModel exchange)
        {
            ResourceExchangeReadCount++;
            exchange = exchangeModel;
            return true;
        }
        public bool TryReadBuildPlacementConfirmationBar(out UiBuildPlacementConfirmationBarModel placementBar) { placementBar = UiBuildPlacementConfirmationBarModel.Hidden; return false; }
        public bool TryReadArmoryCategory(out ArmoryCatalogCategory category) { category = ArmoryCatalogCategory.Characters; return false; }
        public bool TryEnqueueArmoryCategory(ArmoryCatalogCategory category) => false;
        public bool TryConsumePresentationCommands(List<UiShellPresentationCommandModel> commands) => false;
        public bool TryEnqueueTransitionComplete(UiShellTransitionCompleteModel completion) => false;
    }
}
