using UnityEngine;
using UnityEngine.UIElements;

public enum UiToolkitShellMotionState
{
    Visible,
    FadeOut,
    SlideLeftOut,
    SlideRightOut,
    SlideTopOut,
    SlideBottomOut,
    ScaleOut,
    PopupVisible,
    PopupHidden
}

[DisallowMultipleComponent]
public sealed class UiToolkitShellView : MonoBehaviour
{
    public const string MotionBaseClass = "shell-motion";

    private static readonly string[] MotionStateClasses =
    {
        "shell-motion-visible",
        "shell-motion-fade-out",
        "shell-motion-slide-left-out",
        "shell-motion-slide-right-out",
        "shell-motion-slide-top-out",
        "shell-motion-slide-bottom-out",
        "shell-motion-scale-out",
        "shell-motion-popup-visible",
        "shell-motion-popup-hidden"
    };

    [SerializeField] private UIDocument document;
    [SerializeField] private VisualTreeAsset shellAsset;

    private VisualElement root;
    private VisualElement safeAreaRoot;
    private VisualElement headerBar;
    private VisualElement contentRoot;
    private VisualElement footerBar;
    private VisualElement modalOverlay;
    private VisualElement tooltipLayer;
    private VisualElement loadingLayer;
    private VisualElement loadingScreenSlot;
    private VisualElement mainMenuScreenSlot;
    private VisualElement matchScreenSlot;
    private VisualElement armoryScreenSlot;
    private VisualElement commanderProfileScreenSlot;
    private VisualElement resultScreenSlot;
    private VisualElement popupScreenSlot;

    public UIDocument Document => document;
    public VisualTreeAsset ShellAsset => shellAsset;
    public VisualElement Root => root;
    public VisualElement SafeAreaRoot => safeAreaRoot;
    public VisualElement HeaderBar => headerBar;
    public VisualElement ContentRoot => contentRoot;
    public VisualElement FooterBar => footerBar;
    public VisualElement ModalOverlay => modalOverlay;
    public VisualElement TooltipLayer => tooltipLayer;
    public VisualElement LoadingLayer => loadingLayer;
    public VisualElement LoadingScreenSlot => loadingScreenSlot;
    public VisualElement MainMenuScreenSlot => mainMenuScreenSlot;
    public VisualElement MatchScreenSlot => matchScreenSlot;
    public VisualElement ArmoryScreenSlot => armoryScreenSlot;
    public VisualElement CommanderProfileScreenSlot => commanderProfileScreenSlot;
    public VisualElement ResultScreenSlot => resultScreenSlot;
    public VisualElement PopupScreenSlot => popupScreenSlot;
    public bool IsMounted => root != null;
    public bool HasRequiredRegions =>
        root != null
        && safeAreaRoot != null
        && headerBar != null
        && contentRoot != null
        && footerBar != null
        && modalOverlay != null
        && tooltipLayer != null
        && loadingLayer != null;
    public bool HasRequiredScreenSlots =>
        loadingScreenSlot != null
        && mainMenuScreenSlot != null
        && matchScreenSlot != null
        && armoryScreenSlot != null
        && commanderProfileScreenSlot != null
        && resultScreenSlot != null
        && popupScreenSlot != null;

    public void Configure(UIDocument configuredDocument, VisualTreeAsset configuredShellAsset)
    {
        if (configuredDocument != null)
            document = configuredDocument;
        if (configuredShellAsset != null)
            shellAsset = configuredShellAsset;
    }

    public static string GetMotionStateClass(UiToolkitShellMotionState state)
    {
        switch (state)
        {
            case UiToolkitShellMotionState.Visible:
                return MotionStateClasses[0];
            case UiToolkitShellMotionState.FadeOut:
                return MotionStateClasses[1];
            case UiToolkitShellMotionState.SlideLeftOut:
                return MotionStateClasses[2];
            case UiToolkitShellMotionState.SlideRightOut:
                return MotionStateClasses[3];
            case UiToolkitShellMotionState.SlideTopOut:
                return MotionStateClasses[4];
            case UiToolkitShellMotionState.SlideBottomOut:
                return MotionStateClasses[5];
            case UiToolkitShellMotionState.ScaleOut:
                return MotionStateClasses[6];
            case UiToolkitShellMotionState.PopupVisible:
                return MotionStateClasses[7];
            case UiToolkitShellMotionState.PopupHidden:
                return MotionStateClasses[8];
            default:
                return MotionStateClasses[0];
        }
    }

    public bool Mount()
    {
        if (document == null)
            document = GetComponent<UIDocument>();

        if (document == null || shellAsset == null)
        {
            root = null;
            return false;
        }

        if (document.visualTreeAsset != shellAsset)
            document.visualTreeAsset = shellAsset;

        root = document.rootVisualElement?.Q<VisualElement>("UIShellAppCanvas");
        BindRegions();
        return HasRequiredRegions && HasRequiredScreenSlots;
    }

    public void ApplyShellMotion(VisualElement target, UiToolkitShellMotionState state)
    {
        if (target == null)
            return;

        RemoveMotionStateClasses(target);
        target.AddToClassList(MotionBaseClass);
        target.AddToClassList(GetMotionStateClass(state));
    }

    public void RemoveShellMotion(VisualElement target)
    {
        if (target == null)
            return;

        target.RemoveFromClassList(MotionBaseClass);
        RemoveMotionStateClasses(target);
    }

    public void ClearCache()
    {
        root = null;
        safeAreaRoot = null;
        headerBar = null;
        contentRoot = null;
        footerBar = null;
        modalOverlay = null;
        tooltipLayer = null;
        loadingLayer = null;
        loadingScreenSlot = null;
        mainMenuScreenSlot = null;
        matchScreenSlot = null;
        armoryScreenSlot = null;
        commanderProfileScreenSlot = null;
        resultScreenSlot = null;
        popupScreenSlot = null;
    }

    private static void RemoveMotionStateClasses(VisualElement target)
    {
        for (int i = 0; i < MotionStateClasses.Length; i++)
            target.RemoveFromClassList(MotionStateClasses[i]);
    }

    private void BindRegions()
    {
        if (root == null)
        {
            ClearCache();
            return;
        }

        safeAreaRoot = root.Q<VisualElement>("SafeAreaRoot");
        headerBar = root.Q<VisualElement>("HeaderBar");
        contentRoot = root.Q<VisualElement>("ContentRoot");
        footerBar = root.Q<VisualElement>("FooterBar");
        modalOverlay = root.Q<VisualElement>("ModalOverlay");
        tooltipLayer = root.Q<VisualElement>("TooltipLayer");
        loadingLayer = root.Q<VisualElement>("LoadingLayer");
        loadingScreenSlot = root.Q<VisualElement>("LoadingScreenSlot");
        mainMenuScreenSlot = root.Q<VisualElement>("MainMenuScreenSlot");
        matchScreenSlot = root.Q<VisualElement>("MatchScreenSlot");
        armoryScreenSlot = root.Q<VisualElement>("ArmoryScreenSlot");
        commanderProfileScreenSlot = root.Q<VisualElement>("CommanderProfileScreenSlot");
        resultScreenSlot = root.Q<VisualElement>("ResultScreenSlot");
        popupScreenSlot = root.Q<VisualElement>("PopupScreenSlot");
    }
}
