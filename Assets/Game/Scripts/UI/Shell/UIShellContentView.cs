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
    private readonly MatchOverlayCommandInputSystem _matchOverlayCommandInputSystem = new();
    private SelectionUiCommandSystem _selectionUiCommandSystem;
    private MainMenuPlayUI _mainMenuPlayUi;
    private System.Action<MatchHudSelectionPanelView> _bindMatchHudSelectionPanel;
    private MatchHudRightQuickRailView _rightQuickRailView;
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
    public int ContentVersion => _contentVersion;

    public void Configure(
        UIShellView view,
        GameObject loadingPrefab,
        GameObject mainMenuPrefab,
        GameObject armoryPrefab,
        GameObject matchHudPrefab,
        GameObject buildDrawerPrefab)
    {
        shellView = view;
        loadingContentPrefab = loadingPrefab;
        mainMenuContentPrefab = mainMenuPrefab;
        armoryContentPrefab = armoryPrefab;
        matchHudContentPrefab = matchHudPrefab;
        buildDrawerPopupPrefab = buildDrawerPrefab;
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
        System.Action<MatchHudSelectionPanelView> bindMatchHudSelectionPanel = null)
    {
        _selectionUiCommandSystem = selectionUiCommandSystem;
        _mainMenuPlayUi = mainMenuPlayUi;
        _bindMatchHudSelectionPanel = bindMatchHudSelectionPanel;
        BindMatchHudSelectionPanelInRegion();
        BindMatchHudCommandControlsInRegion();
        BindMatchHudRightQuickRailInRegion();
        BindMatchHudRuntimeFeedbackInRegion();
        BindMatchHudMinimapInRegion();
        BindMatchHudSquadTrayInRegion();
    }

    public bool TryGetMatchHudSelectionPanelView(out MatchHudSelectionPanelView view)
    {
        view = null;
        if (!TryGetRegionContentRoot(UIShellRegionId.LeftRegion, out RectTransform contentRoot) ||
            contentRoot.childCount == 0)
        {
            return false;
        }

        view = contentRoot.GetChild(0).GetComponent<MatchHudSelectionPanelView>();
        return view != null;
    }

    public bool TryBindMatchHudRightQuickRailView(MatchHudRightQuickRailView view)
    {
        if (view == null || view.BuildButton == null)
            return false;

        if (_rightQuickRailView != null && _rightQuickRailView != view)
            _rightQuickRailView.UnbindBuildCommand();

        _rightQuickRailView = view;
        _rightQuickRailBuildButton = view.BuildButton;
        _rightQuickRailView.BindBuildCommand(OpenBuildDrawerFromRightQuickRail, _selectionUiCommandSystem);
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
        BindMatchHudSelectionPanel(left);
        GameObject right = InstallSection(matchHudContentPrefab, UIShellContentSectionId.Right, UIShellRegionId.RightRegion);
        GameObject footer = InstallSection(matchHudContentPrefab, UIShellContentSectionId.Footer, UIShellRegionId.FooterRegion);
        BindMatchHudCommandControls(footer);
        BindMatchHudRightQuickRail(right);
        BindMatchHudRuntimeFeedback(footer);
        BindMatchHudMinimap(footer);
        BindMatchHudSquadTray(footer);
        ClearRegion(UIShellRegionId.MiddleRegion);
    }

    private void BindMatchHudSelectionPanelInRegion()
    {
        if (!TryGetRegionContentRoot(UIShellRegionId.LeftRegion, out RectTransform contentRoot) ||
            contentRoot.childCount == 0)
            return;

        BindMatchHudSelectionPanel(contentRoot.GetChild(0).gameObject);
    }

    private void BindMatchHudSelectionPanel(GameObject leftContent)
    {
        if (leftContent == null)
            return;

        MatchHudSelectionPanelView view = leftContent.GetComponent<MatchHudSelectionPanelView>();
        view?.HideSelection();
        _mainMenuPlayUi?.BindMatchHudSelectionPanel(view);
        _bindMatchHudSelectionPanel?.Invoke(view);
    }

    private void BindMatchHudCommandControlsInRegion()
    {
        if (!TryGetRegionContentRoot(UIShellRegionId.FooterRegion, out RectTransform contentRoot))
            return;

        MatchOverlayCommandControlsView view = contentRoot.GetComponentInChildren<MatchOverlayCommandControlsView>(true);
        if (view != null)
        {
            _matchOverlayCommandInputSystem.Bind(
                view,
                _selectionUiCommandSystem,
                () => InstallBuildDrawerPopup(),
                CloseBuildDrawerPopup);
            _mainMenuPlayUi?.BindMatchHudCommandControls(view);
        }
    }

    private void BindMatchHudRightQuickRailInRegion()
    {
        if (!TryGetRegionContentRoot(UIShellRegionId.RightRegion, out RectTransform contentRoot) ||
            contentRoot.childCount == 0)
        {
            return;
        }

        BindMatchHudRightQuickRail(contentRoot.GetChild(0).gameObject);
    }

    private void BindMatchHudRightQuickRail(GameObject rightContent)
    {
        UnbindRightQuickRailBuildButton();
        if (rightContent == null)
            return;

        MatchHudRightQuickRailView view = rightContent.GetComponent<MatchHudRightQuickRailView>();
        if (view == null)
            return;

        if (view.BuildButton == null)
        {
            Debug.LogWarning(
                $"Right quick rail build command is missing its Button reference on {rightContent.name}.");
            return;
        }

        TryBindMatchHudRightQuickRailView(view);

        if (!HasRaycastableGraphic(_rightQuickRailBuildButton))
        {
            Debug.LogWarning("Right quick rail build command has no raycastable Graphic target.");
        }
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
            BattleHudRuntimeFeedbackSystem.ApplyStickyCommandMode(TacticalCommandMode.Build);
            return;
        }

        GameObject popup = InstallBuildDrawerPopup();
        if (popup == null)
        {
            Debug.LogWarning("Build drawer popup could not be installed.");
            return;
        }

        BattleHudRuntimeFeedbackSystem.ApplyStickyCommandMode(TacticalCommandMode.Build);
    }

    private static bool HasRaycastableGraphic(Button button)
    {
        if (button == null)
            return false;

        Graphic[] graphics = button.GetComponentsInChildren<Graphic>(true);
        for (int i = 0; i < graphics.Length; i++)
        {
            Graphic graphic = graphics[i];
            if (graphic != null && graphic.raycastTarget)
                return true;
        }

        return false;
    }

    private void BindMatchHudRuntimeFeedbackInRegion()
    {
        if (!TryGetRegionContentRoot(UIShellRegionId.FooterRegion, out RectTransform contentRoot))
            return;

        BattleHudRuntimeFeedbackView view = contentRoot.GetComponentInChildren<BattleHudRuntimeFeedbackView>(true);
        if (view != null)
            BattleHudRuntimeFeedbackSystem.SetActiveView(view);
    }

    private void BindMatchHudMinimapInRegion()
    {
        if (!TryGetRegionContentRoot(UIShellRegionId.FooterRegion, out RectTransform contentRoot))
            return;

        MatchHudMinimapView view = contentRoot.GetComponentInChildren<MatchHudMinimapView>(true);
        if (view != null)
            _mainMenuPlayUi?.BindMatchHudMinimap(view);
    }

    private void BindMatchHudSquadTrayInRegion()
    {
        if (!TryGetRegionContentRoot(UIShellRegionId.FooterRegion, out RectTransform contentRoot))
            return;

        MatchHudSquadTrayView view = contentRoot.GetComponentInChildren<MatchHudSquadTrayView>(true);
        if (view != null)
            _mainMenuPlayUi?.BindMatchHudSquadTray(view);
    }

    private void BindMatchHudCommandControls(GameObject footer)
    {
        if (footer == null)
            return;

        MatchOverlayCommandControlsView view = footer.GetComponent<MatchOverlayCommandControlsView>();
        if (view != null)
        {
            _matchOverlayCommandInputSystem.Bind(
                view,
                _selectionUiCommandSystem,
                () => InstallBuildDrawerPopup(),
                CloseBuildDrawerPopup);
            _mainMenuPlayUi?.BindMatchHudCommandControls(view);
        }
    }

    private static void BindMatchHudRuntimeFeedback(GameObject footer)
    {
        if (footer == null)
            return;

        BattleHudRuntimeFeedbackView view = footer.GetComponentInChildren<BattleHudRuntimeFeedbackView>(true);
        if (view != null)
            BattleHudRuntimeFeedbackSystem.SetActiveView(view);
    }

    private void BindMatchHudMinimap(GameObject footer)
    {
        if (footer == null)
            return;

        MatchHudMinimapView view = footer.GetComponentInChildren<MatchHudMinimapView>(true);
        if (view != null)
            _mainMenuPlayUi?.BindMatchHudMinimap(view);
    }

    private void BindMatchHudSquadTray(GameObject footer)
    {
        if (footer == null)
            return;

        MatchHudSquadTrayView view = footer.GetComponentInChildren<MatchHudSquadTrayView>(true);
        if (view != null)
            _mainMenuPlayUi?.BindMatchHudSquadTray(view);
    }

    public GameObject InstallBuildDrawerPopup()
    {
        UnbindBuildDrawerPopupCloseButton();
        _buildDrawerPopupInstance = InstallRoot(buildDrawerPopupPrefab, UIShellRegionId.PopupLayer);
        BindBuildDrawerPopupCloseButton(_buildDrawerPopupInstance);
        return _buildDrawerPopupInstance;
    }

    public void CloseBuildDrawerPopup()
    {
        UnbindBuildDrawerPopupCloseButton();
        GameObject popup = _buildDrawerPopupInstance;
        _buildDrawerPopupInstance = null;

        if (popup != null)
        {
            DestroyRegionObject(popup);
            MarkContentChanged();
        }

        BattleHudRuntimeFeedbackSystem.ClearStickyCommandMode(TacticalCommandMode.Build);
    }

    private void BindBuildDrawerPopupCloseButton(GameObject popup)
    {
        if (popup == null)
            return;

        UIPopupCloseView closeView = popup.GetComponent<UIPopupCloseView>();
        if (closeView == null || closeView.CloseButton == null)
        {
            Debug.LogWarning("Build drawer popup is missing its close button binding.");
            return;
        }

        _buildDrawerPopupCloseButton = closeView.CloseButton;
        _buildDrawerPopupCloseButtonListener = CloseBuildDrawerPopup;
        _buildDrawerPopupCloseButton.onClick.RemoveListener(_buildDrawerPopupCloseButtonListener);
        _buildDrawerPopupCloseButton.onClick.AddListener(_buildDrawerPopupCloseButtonListener);
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
        else if (regionId == UIShellRegionId.PopupLayer)
            UnbindBuildDrawerPopupCloseButton();

        if (TryGetRegionContentRoot(regionId, out RectTransform contentRoot))
        {
            ClearChildren(contentRoot);
            MarkContentChanged();
        }

        if (regionId == UIShellRegionId.PopupLayer)
            _buildDrawerPopupInstance = null;
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
