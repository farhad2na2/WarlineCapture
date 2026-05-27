using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class WarlineCaptureShellContentPresenterView : MonoBehaviour
{
    [SerializeField] private WarlineCaptureShellView shellView;
    [SerializeField] private GameObject loadingContentPrefab;
    [SerializeField] private GameObject mainMenuContentPrefab;
    [SerializeField] private GameObject commanderProfileContentPrefab;
    [SerializeField] private GameObject matchHudContentPrefab;
    [SerializeField] private GameObject resultPopupPrefab;

    public WarlineCaptureShellView ShellView => shellView;
    public GameObject LoadingContentPrefab => loadingContentPrefab;
    public GameObject MainMenuContentPrefab => mainMenuContentPrefab;
    public GameObject CommanderProfileContentPrefab => commanderProfileContentPrefab;
    public GameObject MatchHudContentPrefab => matchHudContentPrefab;
    public GameObject ResultPopupPrefab => resultPopupPrefab;

    public void Configure(
        WarlineCaptureShellView view,
        GameObject loadingPrefab,
        GameObject mainMenuPrefab,
        GameObject commanderProfilePrefab,
        GameObject matchHudPrefab,
        GameObject popupPrefab)
    {
        shellView = view;
        loadingContentPrefab = loadingPrefab;
        mainMenuContentPrefab = mainMenuPrefab;
        commanderProfileContentPrefab = commanderProfilePrefab;
        matchHudContentPrefab = matchHudPrefab;
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
        ClearRegion(WarlineCaptureShellRegionId.FooterRegion);
        ClearRegion(WarlineCaptureShellRegionId.PopupLayer);
    }

    private void InstallMainMenuBody()
    {
        InstallSection(mainMenuContentPrefab, "LeftContent", WarlineCaptureShellRegionId.LeftRegion);
        InstallSection(mainMenuContentPrefab, "MiddleContent", WarlineCaptureShellRegionId.MiddleRegion);
        InstallSection(mainMenuContentPrefab, "RightContent", WarlineCaptureShellRegionId.RightRegion);
        ClearRegion(WarlineCaptureShellRegionId.FooterRegion);
        ClearRegion(WarlineCaptureShellRegionId.PopupLayer);
    }

    public void InstallMenuRouteBody(WarlineCaptureRoute route)
    {
        if (route == WarlineCaptureRoute.CommanderProfile)
        {
            InstallCommanderProfileBody();
            return;
        }

        InstallMainMenuBody();
    }

    private void InstallCommanderProfileBody()
    {
        InstallSection(commanderProfileContentPrefab, "LeftContent", WarlineCaptureShellRegionId.LeftRegion);
        InstallSection(commanderProfileContentPrefab, "MiddleContent", WarlineCaptureShellRegionId.MiddleRegion);
        InstallSection(commanderProfileContentPrefab, "RightContent", WarlineCaptureShellRegionId.RightRegion);
        ClearRegion(WarlineCaptureShellRegionId.FooterRegion);
        ClearRegion(WarlineCaptureShellRegionId.PopupLayer);
    }

    private void InstallMatchHud()
    {
        ClearRegion(WarlineCaptureShellRegionId.MenuBackgroundRegion);
        InstallSection(matchHudContentPrefab, "HeaderContent", WarlineCaptureShellRegionId.HeaderRegion);
        InstallSection(matchHudContentPrefab, "LeftContent", WarlineCaptureShellRegionId.LeftRegion);
        InstallSection(matchHudContentPrefab, "RightContent", WarlineCaptureShellRegionId.RightRegion);
        InstallSection(matchHudContentPrefab, "FooterContent", WarlineCaptureShellRegionId.FooterRegion);
        ClearRegion(WarlineCaptureShellRegionId.MiddleRegion);
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
            if (Application.isPlaying)
                Destroy(child.gameObject);
            else
                DestroyImmediate(child.gameObject);
        }
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
