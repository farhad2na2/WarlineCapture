using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class UIShellContentView : MonoBehaviour
{
    [SerializeField] private UIShellView shellView;
    [SerializeField] private GameObject loadingContentPrefab;
    [SerializeField] private GameObject mainMenuContentPrefab;
    [SerializeField] private GameObject armoryContentPrefab;
    [SerializeField] private GameObject matchHudContentPrefab;
    [SerializeField] private GameObject buildDrawerPopupPrefab;
    [SerializeField] private GameObject buildPlacementConfirmationBarPrefab;
    private readonly MatchOverlayCommandInputSystem _matchOverlayCommandInputSystem = new();
    private SelectionUiCommandSystem _selectionUiCommandSystem;
    private BuildingUiCommandSystem _buildingUiCommandSystem;
    private BuildingUiCommandSystem.Context _buildingUiCommandContext;
    private BuildingUiQuerySystem _buildingUiQuerySystem;
    private BuildingUiQuerySystem.Context _buildingUiQueryContext;
    private MainMenuPlayUI _mainMenuPlayUi;
    private System.Action<MatchHudSelectionPanelView> _bindMatchHudSelectionPanel;
    private MatchHudSelectionPanelView _matchHudSelectionPanelView;
    private MatchOverlayCommandControlsView _matchHudCommandControlsView;
    private MatchHudFooterContentView _matchHudFooterContentView;
    private MatchHudRightQuickRailView _rightQuickRailView;
    private BuildPlacementConfirmationBarView _buildPlacementConfirmationBarView;
    private Button _rightQuickRailBuildButton;
    private Button _buildDrawerPopupCloseButton;
    private UnityAction _buildDrawerPopupCloseButtonListener;
    private GameObject _buildDrawerPopupInstance;
    private int _contentVersion;

    public UIShellView ShellView => shellView;
    public GameObject LoadingContentPrefab => loadingContentPrefab;
    public GameObject MainMenuContentPrefab => mainMenuContentPrefab;
    public GameObject ArmoryContentPrefab => armoryContentPrefab;
    public GameObject MatchHudContentPrefab => matchHudContentPrefab;
    public GameObject BuildDrawerPopupPrefab => buildDrawerPopupPrefab;
    public GameObject BuildPlacementConfirmationBarPrefab => buildPlacementConfirmationBarPrefab;
    public int ContentVersion => _contentVersion;

    public void Configure(
        UIShellView view,
        GameObject loadingPrefab,
        GameObject mainMenuPrefab,
        GameObject armoryPrefab,
        GameObject matchHudPrefab,
        GameObject buildDrawerPrefab,
        GameObject buildPlacementConfirmationPrefab = null)
    {
        shellView = view;
        loadingContentPrefab = loadingPrefab;
        mainMenuContentPrefab = mainMenuPrefab;
        armoryContentPrefab = armoryPrefab;
        matchHudContentPrefab = matchHudPrefab;
        buildDrawerPopupPrefab = buildDrawerPrefab;
        if (buildPlacementConfirmationPrefab != null)
            buildPlacementConfirmationBarPrefab = buildPlacementConfirmationPrefab;
    }

    public void PrepareForCommandSequence(IReadOnlyList<UiShellPresentationCommandComponent> commands)
    {
        if (commands == null)
            return;

        for (int i = 0; i < commands.Count; i++)
        {
            switch (commands[i].Kind)
            {
                case UiShellCommandKind.ShowLoading:
                    InstallLoading();
                    break;
                case UiShellCommandKind.EnterMenu:
                    InstallMainMenu();
                    break;
                case UiShellCommandKind.EnterMatchHud:
                    InstallMatchHud();
                    break;
            }
        }
    }

    public void BindGameplayRuntimeDependencies(
        SelectionUiCommandSystem selectionUiCommandSystem,
        MainMenuPlayUI mainMenuPlayUi = null,
        System.Action<MatchHudSelectionPanelView> bindMatchHudSelectionPanel = null,
        BuildingUiCommandSystem buildingUiCommandSystem = null,
        BuildingUiCommandSystem.Context buildingUiCommandContext = default)
    {
        _selectionUiCommandSystem = selectionUiCommandSystem;
        _buildingUiCommandSystem = buildingUiCommandSystem;
        _buildingUiCommandContext = buildingUiCommandContext;
        _mainMenuPlayUi = mainMenuPlayUi;
        _bindMatchHudSelectionPanel = bindMatchHudSelectionPanel;
        BindMatchHudSelectionPanel(_matchHudSelectionPanelView);
        BindMatchHudFooter(_matchHudFooterContentView);
        BindMatchHudRightQuickRail(_rightQuickRailView);
        BindBuildPlacementConfirmationBarInRegion();
    }

    internal void BindBuildDrawerRuntimeQueries(
        BuildingUiQuerySystem buildingUiQuerySystem,
        BuildingUiQuerySystem.Context buildingUiQueryContext)
    {
        _buildingUiQuerySystem = buildingUiQuerySystem;
        _buildingUiQueryContext = buildingUiQueryContext;
        BindBuildDrawerRuntimeCommands(_buildDrawerPopupInstance);
    }

    public bool TryGetMatchHudSelectionPanelView(out MatchHudSelectionPanelView view)
    {
        view = _matchHudSelectionPanelView;
        return view != null;
    }

    private bool TryBindMatchHudRightQuickRailView(MatchHudRightQuickRailView view)
    {
        if (view == null || view.BuildButton == null)
            return false;

        if (_rightQuickRailView != null && _rightQuickRailView != view)
            _rightQuickRailView.UnbindBuildCommand();

        _rightQuickRailView = view;
        _rightQuickRailBuildButton = view.BuildButton;
        _rightQuickRailView.BindBuildCommand(
            OpenBuildDrawerFromRightQuickRail,
            _selectionUiCommandSystem,
            ResolveMatchHudRuntimeFeedback());
        _mainMenuPlayUi?.BindMatchHudRightQuickRail(view);
        return true;
    }

    private void InstallLoading()
    {
        ClearRegion(UIShellRegionId.MenuBackgroundRegion);
        InstallRoot(loadingContentPrefab, UIShellRegionId.LoadingLayer);
    }

    private void InstallMainMenu()
    {
        InstallSection(mainMenuContentPrefab, UIShellContentSectionId.MenuBackground, UIShellRegionId.MenuBackgroundRegion);
        InstallSection(mainMenuContentPrefab, UIShellContentSectionId.Header, UIShellRegionId.HeaderRegion);
        InstallMainMenuBody();
        ClearRegion(UIShellRegionId.PopupLayer);
    }

    private void InstallMainMenuBody()
    {
        InstallSection(mainMenuContentPrefab, UIShellContentSectionId.Left, UIShellRegionId.LeftRegion);
        InstallSection(mainMenuContentPrefab, UIShellContentSectionId.Middle, UIShellRegionId.MiddleRegion);
        InstallSection(mainMenuContentPrefab, UIShellContentSectionId.Right, UIShellRegionId.RightRegion);
        InstallSection(mainMenuContentPrefab, UIShellContentSectionId.Footer, UIShellRegionId.FooterRegion);
        ClearRegion(UIShellRegionId.PopupLayer);
    }

    public void InstallMenuRouteBody(UIRoute route)
    {
        if (route == UIRoute.Armory)
        {
            InstallArmoryBody();
            return;
        }

        InstallMainMenuBody();
    }

    private void InstallArmoryBody()
    {
        InstallSection(armoryContentPrefab, UIShellContentSectionId.MenuBackground, UIShellRegionId.MenuBackgroundRegion);
        InstallSection(armoryContentPrefab, UIShellContentSectionId.Header, UIShellRegionId.HeaderRegion);
        InstallSection(armoryContentPrefab, UIShellContentSectionId.Left, UIShellRegionId.LeftRegion);
        GameObject middle = InstallSection(armoryContentPrefab, UIShellContentSectionId.Middle, UIShellRegionId.MiddleRegion);
        GameObject right = InstallSection(armoryContentPrefab, UIShellContentSectionId.Right, UIShellRegionId.RightRegion);
        WireArmorySections(middle, right);
        InstallSection(armoryContentPrefab, UIShellContentSectionId.Footer, UIShellRegionId.FooterRegion);
        ClearRegion(UIShellRegionId.PopupLayer);
    }

    private static void WireArmorySections(GameObject middle, GameObject right)
    {
        if (middle == null || right == null)
            return;

        ArmoryContentListView listView = middle.GetComponent<ArmoryContentListView>();
        ArmoryRightContentView rightView = right.GetComponent<ArmoryRightContentView>();
        if (listView == null || rightView == null)
            return;

        listView.SetInspectionPanel(rightView.InspectionPanel);
    }

    private void InstallMatchHud()
    {
        ClearRegion(UIShellRegionId.MenuBackgroundRegion);
        InstallSection(matchHudContentPrefab, UIShellContentSectionId.Header, UIShellRegionId.HeaderRegion);
        GameObject left = InstallSection(matchHudContentPrefab, UIShellContentSectionId.Left, UIShellRegionId.LeftRegion);
        _matchHudSelectionPanelView = left != null ? left.GetComponent<MatchHudSelectionPanelView>() : null;
        BindMatchHudSelectionPanel(_matchHudSelectionPanelView);
        GameObject right = InstallSection(matchHudContentPrefab, UIShellContentSectionId.Right, UIShellRegionId.RightRegion);
        _rightQuickRailView = right != null ? right.GetComponent<MatchHudRightQuickRailView>() : null;
        GameObject footer = InstallSection(matchHudContentPrefab, UIShellContentSectionId.Footer, UIShellRegionId.FooterRegion);
        _matchHudFooterContentView = footer != null ? footer.GetComponent<MatchHudFooterContentView>() : null;
        BindMatchHudFooter(_matchHudFooterContentView);
        BindMatchHudRightQuickRail(_rightQuickRailView);
        BindBuildPlacementConfirmationBar(footer);
        ClearRegion(UIShellRegionId.MiddleRegion);
    }

    private void BindMatchHudSelectionPanel(MatchHudSelectionPanelView view)
    {
        view?.HideSelection();
        _mainMenuPlayUi?.BindMatchHudSelectionPanel(view);
        _bindMatchHudSelectionPanel?.Invoke(view);
    }

    private void BindMatchHudFooter(MatchHudFooterContentView footer)
    {
        _matchHudCommandControlsView = footer != null ? footer.CommandControls : null;
        BindMatchHudCommandControls(_matchHudCommandControlsView);
        BindMatchHudRuntimeFeedback(footer != null ? footer.RuntimeFeedback : null);
        BindMatchHudMinimap(footer != null ? footer.Minimap : null);
        BindMatchHudSquadTray(footer != null ? footer.SquadTray : null);
    }

    private void BindMatchHudRightQuickRail(MatchHudRightQuickRailView view)
    {
        UnbindRightQuickRailBuildButton();
        TryBindMatchHudRightQuickRailView(view);
    }

    private void UnbindRightQuickRailBuildButton()
    {
        _rightQuickRailView?.UnbindBuildCommand();
        _mainMenuPlayUi?.BindMatchHudRightQuickRail(null);
        _rightQuickRailView = null;
        _rightQuickRailBuildButton = null;
    }

    private void OpenBuildDrawerFromRightQuickRail()
    {
        _selectionUiCommandSystem?.CaptureUiClickSequence();
        if (_buildDrawerPopupInstance != null)
        {
            BattleHudRuntimeFeedbackSystem.ApplyStickyCommandMode(ResolveMatchHudRuntimeFeedback(), TacticalCommandMode.Build);
            return;
        }

        GameObject popup = InstallBuildDrawerPopup();
        if (popup == null)
        {
            BattleHudRuntimeFeedbackSystem.ApplyCommandResult(ResolveMatchHudRuntimeFeedback(), TacticalCommandResult.Rejected(
                TacticalCommandReasonCode.BuildUnavailable,
                "Build drawer is not ready."));
            return;
        }

        BattleHudRuntimeFeedbackSystem.ApplyStickyCommandMode(ResolveMatchHudRuntimeFeedback(), TacticalCommandMode.Build);
    }

    private void BindBuildPlacementConfirmationBarInRegion()
    {
        RectTransform contentRoot = shellView != null ? shellView.transform as RectTransform : null;
        if (contentRoot == null)
            return;

        BindBuildPlacementConfirmationBar(contentRoot.gameObject);
    }

    private void BindMatchHudCommandControls(MatchOverlayCommandControlsView view)
    {
        if (view != null)
        {
            _matchOverlayCommandInputSystem.Bind(
                view,
                _selectionUiCommandSystem,
                _matchHudFooterContentView != null ? _matchHudFooterContentView.RuntimeFeedback : null,
                () => InstallBuildDrawerPopup(),
                CloseBuildDrawerPopup);
            _mainMenuPlayUi?.BindMatchHudCommandControls(view);
        }
    }

    private void BindMatchHudRuntimeFeedback(BattleHudRuntimeFeedbackView view)
    {
        if (view != null)
        {
            _mainMenuPlayUi?.BindMatchHudRuntimeFeedback(view);
        }
        else
        {
            _mainMenuPlayUi?.BindMatchHudRuntimeFeedback(null);
        }
    }

    private void BindMatchHudMinimap(MatchHudMinimapView view)
    {
        if (view != null)
            _mainMenuPlayUi?.BindMatchHudMinimap(view);
    }

    private void BindMatchHudSquadTray(MatchHudSquadTrayView view)
    {
        if (view != null)
            _mainMenuPlayUi?.BindMatchHudSquadTray(view);
    }

    private void BindBuildPlacementConfirmationBar(GameObject footer)
    {
        RectTransform parent = shellView != null ? shellView.transform as RectTransform : null;
        if (parent == null)
            parent = footer != null ? footer.transform as RectTransform : null;
        if (parent == null)
            return;

        _buildPlacementConfirmationBarView = BuildPlacementConfirmationBarView.Ensure(buildPlacementConfirmationBarPrefab, parent);
        if (_buildPlacementConfirmationBarView == null)
            return;

        _buildPlacementConfirmationBarView.transform.SetAsLastSibling();
        _buildPlacementConfirmationBarView?.BindRuntimeCommands(
            _buildingUiCommandSystem,
            _buildingUiCommandContext,
            ResolveMatchHudRuntimeFeedback());
        _mainMenuPlayUi?.BindBuildPlacementConfirmationBar(_buildPlacementConfirmationBarView);
    }

    public GameObject InstallBuildDrawerPopup()
    {
        UnbindBuildDrawerPopupCloseButton();
        _buildDrawerPopupInstance = InstallRoot(buildDrawerPopupPrefab, UIShellRegionId.PopupLayer);
        BindBuildDrawerPopupInputBlocker(_buildDrawerPopupInstance);
        BindBuildDrawerPopupCloseButton(_buildDrawerPopupInstance);
        BindBuildDrawerRuntimeCommands(_buildDrawerPopupInstance);
        return _buildDrawerPopupInstance;
    }

    public void CloseBuildDrawerPopup()
    {
        UnbindBuildDrawerPopupCloseButton();
        _mainMenuPlayUi?.BindBuildDrawer(null);
        GameObject popup = _buildDrawerPopupInstance;
        _buildDrawerPopupInstance = null;

        bool hasActivePlacement = _buildingUiCommandSystem != null &&
                                  _buildingUiCommandSystem.HasPendingBuildingPlacement(_buildingUiCommandContext);
        if (!hasActivePlacement)
            BattleHudRuntimeFeedbackSystem.ClearStickyCommandMode(ResolveMatchHudRuntimeFeedback(), TacticalCommandMode.Build);

        if (popup != null)
        {
            if (Application.isPlaying)
            {
                UIPopupMotionView motionView = popup.GetComponent<UIPopupMotionView>();
                if (motionView != null && motionView.PlayHide(() =>
                    {
                        DestroyRegionObject(popup);
                        MarkContentChanged();
                    }))
                {
                    return;
                }
            }

            DestroyRegionObject(popup);
            MarkContentChanged();
        }
    }

    private void BindBuildDrawerPopupInputBlocker(GameObject popup)
    {
        BuildDrawerView view = popup != null ? popup.GetComponent<BuildDrawerView>() : null;
        _mainMenuPlayUi?.BindBuildDrawer(view);
    }

    private BattleHudRuntimeFeedbackView ResolveMatchHudRuntimeFeedback()
    {
        return _matchHudFooterContentView != null ? _matchHudFooterContentView.RuntimeFeedback : null;
    }

    private void BindBuildDrawerPopupCloseButton(GameObject popup)
    {
        if (popup == null)
            return;

        UIPopupCloseButtonView directCloseView = popup.GetComponent<UIPopupCloseButtonView>();
        if (directCloseView != null)
        {
            directCloseView.BindRuntimeFeedback(_matchHudFooterContentView != null
                ? _matchHudFooterContentView.RuntimeFeedback
                : null);
            directCloseView.enabled = false;
        }

        UIPopupCloseView closeView = popup.GetComponent<UIPopupCloseView>();
        if (closeView == null || closeView.CloseButton == null)
            return;

        _buildDrawerPopupCloseButton = closeView.CloseButton;
        _buildDrawerPopupCloseButtonListener = CloseBuildDrawerPopup;
        _buildDrawerPopupCloseButton.onClick.RemoveListener(_buildDrawerPopupCloseButtonListener);
        _buildDrawerPopupCloseButton.onClick.AddListener(_buildDrawerPopupCloseButtonListener);
    }

    private void BindBuildDrawerRuntimeCommands(GameObject popup)
    {
        if (popup == null)
            return;

        BuildDrawerCatalogRuntimeView presenter = popup.GetComponent<BuildDrawerCatalogRuntimeView>();
        if (presenter == null)
            return;

        presenter.BindRuntimeCommands(
            _buildingUiCommandSystem,
            _buildingUiCommandContext,
            CloseBuildDrawerPopup,
            ResolveMatchHudRuntimeFeedback());
        presenter.BindRuntimeQueries(
            _buildingUiQuerySystem,
            _buildingUiQueryContext);
    }

    private void UnbindBuildDrawerPopupCloseButton()
    {
        if (_buildDrawerPopupCloseButton != null && _buildDrawerPopupCloseButtonListener != null)
            _buildDrawerPopupCloseButton.onClick.RemoveListener(_buildDrawerPopupCloseButtonListener);

        _buildDrawerPopupCloseButton = null;
        _buildDrawerPopupCloseButtonListener = null;
    }

    private GameObject InstallRoot(GameObject prefab, UIShellRegionId regionId)
    {
        if (prefab == null || !TryGetRegionContentRoot(regionId, out RectTransform contentRoot))
            return null;

        ClearChildren(contentRoot);
        GameObject instance = Instantiate(prefab, contentRoot, false);
        instance.name = prefab.name;
        Stretch(instance.GetComponent<RectTransform>());
        if (regionId == UIShellRegionId.PopupLayer)
            UIPopupMotionView.Ensure(instance)?.PlayShow();
        MarkContentChanged();
        return instance;
    }

    private GameObject InstallSection(GameObject prefab, UIShellContentSectionId sectionId, UIShellRegionId regionId)
    {
        if (prefab == null || !TryGetRegionContentRoot(regionId, out RectTransform contentRoot))
            return null;

        UIShellContentSectionsView sectionsView = prefab.GetComponent<UIShellContentSectionsView>();
        if (sectionsView == null || !sectionsView.TryGetSection(sectionId, out GameObject source) || source == null)
            return null;

        ClearChildren(contentRoot);
        GameObject instance = Instantiate(source, contentRoot, false);
        instance.name = source.name;
        Stretch(instance.GetComponent<RectTransform>());
        MarkContentChanged();
        return instance;
    }

    private void ClearRegion(UIShellRegionId regionId)
    {
        if (regionId == UIShellRegionId.RightRegion)
            UnbindRightQuickRailBuildButton();
        else if (regionId == UIShellRegionId.LeftRegion)
        {
            _matchHudSelectionPanelView = null;
            _mainMenuPlayUi?.BindMatchHudSelectionPanel(null);
            _bindMatchHudSelectionPanel?.Invoke(null);
        }
        else if (regionId == UIShellRegionId.PopupLayer)
        {
            UnbindBuildDrawerPopupCloseButton();
            _mainMenuPlayUi?.BindBuildDrawer(null);
        }

        if (TryGetRegionContentRoot(regionId, out RectTransform contentRoot))
        {
            ClearChildren(contentRoot);
            MarkContentChanged();
        }

        if (regionId == UIShellRegionId.PopupLayer)
            _buildDrawerPopupInstance = null;
        if (regionId == UIShellRegionId.FooterRegion)
        {
            _buildPlacementConfirmationBarView = null;
            _matchHudFooterContentView = null;
            _matchHudCommandControlsView = null;
            _mainMenuPlayUi?.BindBuildPlacementConfirmationBar(null);
            _mainMenuPlayUi?.BindMatchHudCommandControls(null);
            _mainMenuPlayUi?.BindMatchHudRuntimeFeedback(null);
            _mainMenuPlayUi?.BindMatchHudMinimap(null);
            _mainMenuPlayUi?.BindMatchHudSquadTray(null);
        }
    }

    private void MarkContentChanged()
    {
        unchecked
        {
            _contentVersion++;
        }
    }

    private bool TryGetRegionContentRoot(UIShellRegionId regionId, out RectTransform contentRoot)
    {
        contentRoot = null;
        if (shellView == null || !shellView.TryGetRegion(regionId, out UIShellRegionView region) || region == null)
            return false;

        contentRoot = region.ContentRoot;
        return contentRoot != null;
    }

    private static void ClearChildren(Transform parent)
    {
        for (int i = parent.childCount - 1; i >= 0; i--)
        {
            Transform child = parent.GetChild(i);
            DestroyRegionObject(child.gameObject);
        }
    }

    private static void DestroyRegionObject(UnityEngine.Object target)
    {
        if (target == null)
            return;

        if (Application.isPlaying)
            Destroy(target);
        else
            DestroyImmediate(target);
    }

    private static void Stretch(RectTransform rect)
    {
        if (rect == null)
            return;

        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.localScale = Vector3.one;
        rect.localRotation = Quaternion.identity;
    }
}
