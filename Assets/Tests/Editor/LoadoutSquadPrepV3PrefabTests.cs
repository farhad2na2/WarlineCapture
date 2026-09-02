using System.Collections.Generic;
using Game.UI.Contracts;
using Game.UI.Runtime;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public sealed class LoadoutSquadPrepV3PrefabTests
{
    private const string PrefabPath = "Assets/Game/Prefabs/UI/Shell/Content/SCN07_LoadoutSquadPrepContent.prefab";
    private GameObject _instance;

    public static void RunFocusedValidation()
    {
        try
        {
            RunStep(nameof(Prefab_HasFunctionalFooterActions), tests => tests.Prefab_HasFunctionalFooterActions());
            RunStep(nameof(DeployButton_QueuesSelectedMissionDeploy), tests => tests.DeployButton_QueuesSelectedMissionDeploy());
            RunStep(nameof(Prefab_UsesProceduralV3GradientsAndSharedLogo), tests => tests.Prefab_UsesProceduralV3GradientsAndSharedLogo());
            Debug.Log("[LoadoutSquadPrepV3Validation] result=Passed tests=3");
            ValidationExit.Exit(0);
        }
        catch (System.Exception exception)
        {
            Debug.LogException(exception);
            Debug.LogError("[LoadoutSquadPrepV3Validation] result=Failed");
            ValidationExit.Exit(1);
        }
    }

    private static void RunStep(string name, System.Action<LoadoutSquadPrepV3PrefabTests> step)
    {
        var tests = new LoadoutSquadPrepV3PrefabTests();
        try
        {
            step(tests);
            Debug.Log($"[LoadoutSquadPrepV3Validation] step={name} result=Passed");
        }
        finally
        {
            tests.TearDown();
        }
    }

    [TearDown]
    public void TearDown()
    {
        UiShellRuntimeGateway.Register(null);
        if (_instance != null)
            Object.DestroyImmediate(_instance);
    }

    [Test]
    public void Prefab_HasFunctionalFooterActions()
    {
        GameObject prefab = RequirePrefab();
        LoadoutSquadPrepScreenView view = prefab.GetComponent<LoadoutSquadPrepScreenView>();
        Assert.NotNull(view);
        Assert.NotNull(view.EditLoadoutButton);
        Assert.NotNull(view.DeployButton);
        UIShellRouteButtonView editRoute = view.EditLoadoutButton.GetComponent<UIShellRouteButtonView>();
        Assert.NotNull(editRoute);
        Assert.AreEqual(UiShellRouteIntent.OpenMenuRoute, editRoute.Intent);
        Assert.AreEqual(UIRoute.Armory, editRoute.Route);
        Assert.IsTrue(editRoute.PushHistory);
    }

    [Test]
    public void DeployButton_QueuesSelectedMissionDeploy()
    {
        var gateway = new RecordingGateway();
        UiShellRuntimeGateway.Register(gateway);
        _instance = Object.Instantiate(RequirePrefab());
        LoadoutSquadPrepScreenView view = _instance.GetComponent<LoadoutSquadPrepScreenView>();
        Assert.NotNull(view);
        view.RefreshBindings();
        Assert.IsTrue(view.DeployButton.interactable);

        view.DeployButton.onClick.Invoke();

        Assert.AreEqual(1, gateway.DeployCount);
        Assert.AreEqual(UiCampaignMissionActionKind.Deploy, gateway.LastCampaignAction);
        Assert.AreEqual(UiCampaignMissionProjectionIds.M02, gateway.LastMissionId);
        Assert.IsFalse(view.DeployButton.interactable);
    }

    [Test]
    public void Prefab_UsesProceduralV3GradientsAndSharedLogo()
    {
        GameObject prefab = RequirePrefab();
        Assert.GreaterOrEqual(prefab.GetComponentsInChildren<V3GradientGraphic>(true).Length, 18);
        MainMenuV3SectionLayoutView responsive = prefab.GetComponentInChildren<MainMenuV3SectionLayoutView>(true);
        Assert.NotNull(responsive);
        Assert.IsTrue(responsive.ExpandToCanvasWidth);
        Assert.AreEqual(5, responsive.RightAnchoredTargets.Length);
        Transform logo = FindChild(prefab.transform, "WarlineLogo");
        Assert.NotNull(logo);
        Assert.NotNull(logo.GetComponentInChildren<Image>(true));
        Transform rifleArt = FindChild(prefab.transform, "RifleSquadArt");
        Assert.NotNull(rifleArt);
        Assert.NotNull(rifleArt.GetComponent<AspectRatioFitter>());
    }

    private static GameObject RequirePrefab()
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        Assert.NotNull(prefab, $"Missing prefab at {PrefabPath}.");
        return prefab;
    }

    private static Transform FindChild(Transform root, string name)
    {
        foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
            if (child != null && child.name == name)
                return child;
        return null;
    }

    private sealed class RecordingGateway : IUiShellRuntimeGateway
    {
        public int DeployCount { get; private set; }
        public UiCampaignMissionActionKind LastCampaignAction { get; private set; }
        public string LastMissionId { get; private set; }

        public bool TryEnqueueCampaignMissionAction(UiCampaignMissionActionKind action, string missionId, bool value = false)
        {
            DeployCount++;
            LastCampaignAction = action;
            LastMissionId = missionId;
            return true;
        }

        public bool TryEnqueueRouteRequest(UiShellRouteIntent intent, UIRoute route, bool pushHistory) => false;
        public bool TryEnqueueUiAction(UiActionKind kind, int payloadId) => false;
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
