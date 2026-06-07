using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class WarlineCaptureShellContentSystem : MonoBehaviour
{
    [SerializeField] private WarlineCaptureShellView shellView;
    [SerializeField] private GameObject loadingContentPrefab;
    [SerializeField] private GameObject mainMenuContentPrefab;
    [SerializeField] private GameObject commanderProfileContentPrefab;
    [SerializeField] private GameObject armoryContentPrefab;
    [SerializeField] private GameObject matchHudContentPrefab;
    [SerializeField] private GameObject buildDrawerPopupPrefab;
    [SerializeField] private GameObject resultPopupPrefab;
    private readonly MatchOverlayCommandInputSystem _matchOverlayCommandInputSystem = new();
    private SelectionUiCommandSystem _selectionUiCommandSystem;
    private MainMenuPlayUI _mainMenuPlayUi;

    public WarlineCaptureShellView ShellView => shellView;
    public GameObject LoadingContentPrefab => loadingContentPrefab;
    public GameObject MainMenuContentPrefab => mainMenuContentPrefab;
    public GameObject CommanderProfileContentPrefab => commanderProfileContentPrefab;
    public GameObject ArmoryContentPrefab => armoryContentPrefab;
    public GameObject MatchHudContentPrefab => matchHudContentPrefab;
    public GameObject BuildDrawerPopupPrefab => buildDrawerPopupPrefab;
    public GameObject ResultPopupPrefab => resultPopupPrefab;

    public void Configure(
        WarlineCaptureShellView view,
        GameObject loadingPrefab,
        GameObject mainMenuPrefab,
        GameObject commanderProfilePrefab,
        GameObject armoryPrefab,
        GameObject matchHudPrefab,
        GameObject buildDrawerPrefab,
        GameObject popupPrefab)
    {
        shellView = view;
        loadingContentPrefab = loadingPrefab;
        mainMenuContentPrefab = mainMenuPrefab;
        commanderProfileContentPrefab = commanderProfilePrefab;
        armoryContentPrefab = armoryPrefab;
        matchHudContentPrefab = matchHudPrefab;
        buildDrawerPopupPrefab = buildDrawerPrefab;
        resultPopupPrefab = popupPrefab;
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
                case UiShellCommandKind.ShowPopup:
                    InstallResultPopup();
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

    private void InstallLoading()
    {
        ClearRegion(WarlineCaptureShellRegionId.MenuBackgroundRegion);
        InstallRoot(loadingContentPrefab, WarlineCaptureShellRegionId.LoadingLayer);
    }

    private void InstallMainMenu()
    {
        InstallSection(mainMenuContentPrefab, "MenuBackgroundContent", WarlineCaptureShellRegionId.MenuBackgroundRegion);
        InstallSection(mainMenuContentPrefab, "HeaderContent", WarlineCaptureShellRegionId.HeaderRegion);
        InstallMainMenuBody();
        ClearRegion(WarlineCaptureShellRegionId.PopupLayer);
    }

    private void InstallMainMenuBody()
    {
        InstallSection(mainMenuContentPrefab, "LeftContent", WarlineCaptureShellRegionId.LeftRegion);
        InstallSection(mainMenuContentPrefab, "MiddleContent", WarlineCaptureShellRegionId.MiddleRegion);
        InstallSection(mainMenuContentPrefab, "RightContent", WarlineCaptureShellRegionId.RightRegion);
        InstallSection(mainMenuContentPrefab, "FooterContent", WarlineCaptureShellRegionId.FooterRegion);
        ClearRegion(WarlineCaptureShellRegionId.PopupLayer);
    }

    public void InstallMenuRouteBody(WarlineCaptureRoute route)
    {
        if (route == WarlineCaptureRoute.CommanderProfile)
        {
            InstallCommanderProfileBody();
            return;
        }

        if (route == WarlineCaptureRoute.Armory)
        {
            InstallArmoryBody();
            return;
        }

        InstallMainMenuBody();
    }

    private void InstallCommanderProfileBody()
    {
        InstallSection(commanderProfileContentPrefab, "MenuBackgroundContent", WarlineCaptureShellRegionId.MenuBackgroundRegion);
        InstallSection(commanderProfileContentPrefab, "HeaderContent", WarlineCaptureShellRegionId.HeaderRegion);
        InstallSection(commanderProfileContentPrefab, "LeftContent", WarlineCaptureShellRegionId.LeftRegion);
        InstallSection(commanderProfileContentPrefab, "MiddleContent", WarlineCaptureShellRegionId.MiddleRegion);
        InstallSection(commanderProfileContentPrefab, "RightContent", WarlineCaptureShellRegionId.RightRegion);
        InstallSection(commanderProfileContentPrefab, "FooterContent", WarlineCaptureShellRegionId.FooterRegion);
        ClearRegion(WarlineCaptureShellRegionId.PopupLayer);
    }

    private void InstallArmoryBody()
    {
        InstallSection(armoryContentPrefab, "MenuBackgroundContent", WarlineCaptureShellRegionId.MenuBackgroundRegion);
        InstallSection(armoryContentPrefab, "HeaderContent", WarlineCaptureShellRegionId.HeaderRegion);
        InstallSection(armoryContentPrefab, "LeftContent", WarlineCaptureShellRegionId.LeftRegion);
        GameObject middle = InstallSection(armoryContentPrefab, "MiddleContent", WarlineCaptureShellRegionId.MiddleRegion);
        GameObject right = InstallSection(armoryContentPrefab, "RightContent", WarlineCaptureShellRegionId.RightRegion);
        WireArmorySections(middle, right);
        InstallSection(armoryContentPrefab, "FooterContent", WarlineCaptureShellRegionId.FooterRegion);
        ClearRegion(WarlineCaptureShellRegionId.PopupLayer);
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
        ClearRegion(WarlineCaptureShellRegionId.MenuBackgroundRegion);
        InstallSection(matchHudContentPrefab, "HeaderContent", WarlineCaptureShellRegionId.HeaderRegion);
        GameObject left = InstallSection(matchHudContentPrefab, "LeftContent", WarlineCaptureShellRegionId.LeftRegion);
        HideMatchHudSelectedSquadPanel(left);
        InstallSection(matchHudContentPrefab, "RightContent", WarlineCaptureShellRegionId.RightRegion);
        GameObject footer = InstallSection(matchHudContentPrefab, "FooterContent", WarlineCaptureShellRegionId.FooterRegion);
        BindMatchHudCommandControls(footer);
        BindMatchHudMinimap(footer);
        BindMatchHudSquadTray(footer);
        ClearRegion(WarlineCaptureShellRegionId.MiddleRegion);
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
        if (!TryGetRegionContentRoot(WarlineCaptureShellRegionId.FooterRegion, out RectTransform contentRoot))
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
        if (!TryGetRegionContentRoot(WarlineCaptureShellRegionId.FooterRegion, out RectTransform contentRoot))
            return;

        MatchHudMinimapView view = contentRoot.GetComponentInChildren<MatchHudMinimapView>(true);
        if (view != null)
            _mainMenuPlayUi?.BindMatchHudMinimap(view);
    }

    private void BindMatchHudSquadTrayInRegion()
    {
        if (!TryGetRegionContentRoot(WarlineCaptureShellRegionId.FooterRegion, out RectTransform contentRoot))
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

    private void InstallResultPopup()
    {
        GameObject popup = InstallRoot(resultPopupPrefab, WarlineCaptureShellRegionId.PopupLayer);
        if (popup == null)
            return;

        RectTransform frame = popup.transform.Find("PopupFrame") as RectTransform;
        if (frame == null)
            return;

        frame.anchorMin = new Vector2(0.5f, 0.5f);
        frame.anchorMax = new Vector2(0.5f, 0.5f);
        frame.pivot = new Vector2(0.5f, 0.5f);
        frame.anchoredPosition = Vector2.zero;
    }

    public GameObject InstallBuildDrawerPopup()
    {
        return InstallRoot(buildDrawerPopupPrefab, WarlineCaptureShellRegionId.PopupLayer);
    }

    public void CloseBuildDrawerPopup()
    {
        if (buildDrawerPopupPrefab == null ||
            !TryGetRegionContentRoot(WarlineCaptureShellRegionId.PopupLayer, out RectTransform contentRoot))
            return;

        Transform popup = contentRoot.Find(buildDrawerPopupPrefab.name);
        if (popup != null)
            DestroyRegionObject(popup.gameObject);
    }

    private GameObject InstallRoot(GameObject prefab, WarlineCaptureShellRegionId regionId)
    {
        if (prefab == null || !TryGetRegionContentRoot(regionId, out RectTransform contentRoot))
            return null;

        ClearChildren(contentRoot);
        GameObject instance = Instantiate(prefab, contentRoot, false);
        instance.name = prefab.name;
        Stretch(instance.GetComponent<RectTransform>());
        return instance;
    }

    private GameObject InstallSection(GameObject prefab, string sectionName, WarlineCaptureShellRegionId regionId)
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

    private void ClearRegion(WarlineCaptureShellRegionId regionId)
    {
        if (TryGetRegionContentRoot(regionId, out RectTransform contentRoot))
            ClearChildren(contentRoot);
    }

    private bool TryGetRegionContentRoot(WarlineCaptureShellRegionId regionId, out RectTransform contentRoot)
    {
        contentRoot = null;
        if (shellView == null || !shellView.TryGetRegion(regionId, out WarlineCaptureShellRegionView region) || region == null)
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
