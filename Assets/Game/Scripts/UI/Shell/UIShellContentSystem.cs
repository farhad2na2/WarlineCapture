using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class UIShellContentSystem : MonoBehaviour
{
    private static readonly List<UIShellContentSystem> RegisteredInstances = new();

    [SerializeField] private UIShellView shellView;
    [SerializeField] private GameObject loadingContentPrefab;
    [SerializeField] private GameObject mainMenuContentPrefab;
    [SerializeField] private GameObject armoryContentPrefab;
    [SerializeField] private GameObject matchHudContentPrefab;
    [SerializeField] private GameObject buildDrawerPopupPrefab;
    private readonly MatchOverlayCommandInputSystem _matchOverlayCommandInputSystem = new();
    private SelectionUiCommandSystem _selectionUiCommandSystem;
    private MainMenuPlayUI _mainMenuPlayUi;

    public UIShellView ShellView => shellView;
    public GameObject LoadingContentPrefab => loadingContentPrefab;
    public GameObject MainMenuContentPrefab => mainMenuContentPrefab;
    public GameObject ArmoryContentPrefab => armoryContentPrefab;
    public GameObject MatchHudContentPrefab => matchHudContentPrefab;
    public GameObject BuildDrawerPopupPrefab => buildDrawerPopupPrefab;
    public static IReadOnlyList<UIShellContentSystem> Instances => RegisteredInstances;

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

    public void BindGameplayRuntimeDependencies(SelectionUiCommandSystem selectionUiCommandSystem, MainMenuPlayUI mainMenuPlayUi = null)
    {
        _selectionUiCommandSystem = selectionUiCommandSystem;
        _mainMenuPlayUi = mainMenuPlayUi;
        BindMatchHudCommandControlsInRegion();
        BindMatchHudMinimapInRegion();
        BindMatchHudSquadTrayInRegion();
    }

    private void OnEnable()
    {
        if (!RegisteredInstances.Contains(this))
            RegisteredInstances.Add(this);
    }

    private void OnDisable()
    {
        RegisteredInstances.Remove(this);
    }

    private void InstallLoading()
    {
        ClearRegion(UIShellRegionId.MenuBackgroundRegion);
        InstallRoot(loadingContentPrefab, UIShellRegionId.LoadingLayer);
    }

    private void InstallMainMenu()
    {
        InstallSection(mainMenuContentPrefab, "MenuBackgroundContent", UIShellRegionId.MenuBackgroundRegion);
        InstallSection(mainMenuContentPrefab, "HeaderContent", UIShellRegionId.HeaderRegion);
        InstallMainMenuBody();
        ClearRegion(UIShellRegionId.PopupLayer);
    }

    private void InstallMainMenuBody()
    {
        InstallSection(mainMenuContentPrefab, "LeftContent", UIShellRegionId.LeftRegion);
        InstallSection(mainMenuContentPrefab, "MiddleContent", UIShellRegionId.MiddleRegion);
        InstallSection(mainMenuContentPrefab, "RightContent", UIShellRegionId.RightRegion);
        InstallSection(mainMenuContentPrefab, "FooterContent", UIShellRegionId.FooterRegion);
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
        InstallSection(armoryContentPrefab, "MenuBackgroundContent", UIShellRegionId.MenuBackgroundRegion);
        InstallSection(armoryContentPrefab, "HeaderContent", UIShellRegionId.HeaderRegion);
        InstallSection(armoryContentPrefab, "LeftContent", UIShellRegionId.LeftRegion);
        GameObject middle = InstallSection(armoryContentPrefab, "MiddleContent", UIShellRegionId.MiddleRegion);
        GameObject right = InstallSection(armoryContentPrefab, "RightContent", UIShellRegionId.RightRegion);
        WireArmorySections(middle, right);
        InstallSection(armoryContentPrefab, "FooterContent", UIShellRegionId.FooterRegion);
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
        InstallSection(matchHudContentPrefab, "HeaderContent", UIShellRegionId.HeaderRegion);
        GameObject left = InstallSection(matchHudContentPrefab, "LeftContent", UIShellRegionId.LeftRegion);
        HideMatchHudSelectedSquadPanel(left);
        InstallSection(matchHudContentPrefab, "RightContent", UIShellRegionId.RightRegion);
        GameObject footer = InstallSection(matchHudContentPrefab, "FooterContent", UIShellRegionId.FooterRegion);
        BindMatchHudCommandControls(footer);
        BindMatchHudMinimap(footer);
        BindMatchHudSquadTray(footer);
        ClearRegion(UIShellRegionId.MiddleRegion);
    }

    private static void HideMatchHudSelectedSquadPanel(GameObject leftContent)
    {
        if (leftContent == null)
            return;

        MatchHudSelectionPanelSystem panelSystem = leftContent.GetComponent<MatchHudSelectionPanelSystem>();
        if (panelSystem != null)
        {
            panelSystem.HideSelection();
            return;
        }

        Transform panel = leftContent.transform.Find("SelectedSquadPanel");
        if (panel != null)
            panel.gameObject.SetActive(false);
    }

    private void BindMatchHudCommandControlsInRegion()
    {
        if (!TryGetRegionContentRoot(UIShellRegionId.FooterRegion, out RectTransform contentRoot))
            return;

        MatchOverlayCommandControlsView view = contentRoot.GetComponentInChildren<MatchOverlayCommandControlsView>(true);
        if (view != null)
            _matchOverlayCommandInputSystem.Bind(
                view,
                _selectionUiCommandSystem,
                () => InstallBuildDrawerPopup(),
                CloseBuildDrawerPopup);
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
            _matchOverlayCommandInputSystem.Bind(
                view,
                _selectionUiCommandSystem,
                () => InstallBuildDrawerPopup(),
                CloseBuildDrawerPopup);
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
        return InstallRoot(buildDrawerPopupPrefab, UIShellRegionId.PopupLayer);
    }

    public void CloseBuildDrawerPopup()
    {
        if (buildDrawerPopupPrefab == null ||
            !TryGetRegionContentRoot(UIShellRegionId.PopupLayer, out RectTransform contentRoot))
            return;

        Transform popup = contentRoot.Find(buildDrawerPopupPrefab.name);
        if (popup != null)
            DestroyRegionObject(popup.gameObject);
    }

    private GameObject InstallRoot(GameObject prefab, UIShellRegionId regionId)
    {
        if (prefab == null || !TryGetRegionContentRoot(regionId, out RectTransform contentRoot))
            return null;

        ClearChildren(contentRoot);
        GameObject instance = Instantiate(prefab, contentRoot, false);
        instance.name = prefab.name;
        Stretch(instance.GetComponent<RectTransform>());
        return instance;
    }

    private GameObject InstallSection(GameObject prefab, string sectionName, UIShellRegionId regionId)
    {
        if (prefab == null || !TryGetRegionContentRoot(regionId, out RectTransform contentRoot))
            return null;

        Transform source = prefab.transform.Find(sectionName);
        if (source == null)
            return null;

        ClearChildren(contentRoot);
        GameObject instance = Instantiate(source.gameObject, contentRoot, false);
        instance.name = sectionName;
        Stretch(instance.GetComponent<RectTransform>());
        return instance;
    }

    private void ClearRegion(UIShellRegionId regionId)
    {
        if (TryGetRegionContentRoot(regionId, out RectTransform contentRoot))
            ClearChildren(contentRoot);
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
