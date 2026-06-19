using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;

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
    private const string DefaultLoadingStatus = "Preparing command interface";
    private const string DefaultCommanderName = "COL. ALEX MORGAN";
    private const string DefaultCommanderSubtitle = "VICTORY IS PLANNED";
    private const string DefaultCommanderPortraitClass = "commander-portrait-default";
    private const string DefaultCreditsText = "12,450";
    private const string DefaultSuppliesText = "1,280";
    private const string DefaultCommandText = "78/100";
    private const int BuildDrawerCatalogItemCount = 7;
    private const int BuildDrawerQueueItemCount = 2;

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
    private static readonly string[] MainMenuRouteClasses =
    {
        "main-menu-route-root",
        "main-menu-route-armory",
        "main-menu-route-supply",
        "main-menu-route-command",
        "main-menu-route-tech-tree",
        "main-menu-route-profile",
        "main-menu-route-skirmish"
    };
    private static readonly string[] MainMenuCommanderPortraitClasses =
    {
        DefaultCommanderPortraitClass
    };
    private static readonly string[] ObjectiveIconClasses =
    {
        "objective-unchecked",
        "objective-checked",
        "objective-star"
    };
    private static readonly string[] PercentLabels = BuildPercentLabels();

    [SerializeField] private UIDocument document;
    [SerializeField] private VisualTreeAsset shellAsset;
    [SerializeField] private VisualTreeAsset loadingScreenAsset;
    [SerializeField] private VisualTreeAsset mainMenuScreenAsset;
    [SerializeField] private VisualTreeAsset matchHudScreenAsset;
    [SerializeField] private VisualTreeAsset buildDrawerPopupAsset;

    private VisualElement root;
    private VisualElement safeAreaRoot;
    private VisualElement headerBar;
    private VisualElement contentRoot;
    private VisualElement footerBar;
    private VisualElement modalOverlay;
    private VisualElement tooltipLayer;
    private VisualElement loadingLayer;
    private VisualElement menuBackgroundRegion;
    private VisualElement loadingScreenSlot;
    private VisualElement mainMenuScreenSlot;
    private VisualElement matchScreenSlot;
    private VisualElement armoryScreenSlot;
    private VisualElement commanderProfileScreenSlot;
    private VisualElement resultScreenSlot;
    private VisualElement popupScreenSlot;
    private TemplateContainer loadingScreenContainer;
    private TemplateContainer mainMenuScreenContainer;
    private TemplateContainer matchHudScreenContainer;
    private TemplateContainer buildDrawerPopupContainer;
    private VisualElement loadingContentRoot;
    private VisualElement mainMenuContentRoot;
    private VisualElement matchHudContentRoot;
    private VisualElement buildDrawerPopupRoot;
    private VisualElement mainMenuHeaderContent;
    private Button mainMenuInboxAction;
    private Button mainMenuSettingsAction;
    private Button mainMenuMenuAction;
    private Button mainMenuNavCampaignAction;
    private Button mainMenuNavArmoryAction;
    private Button mainMenuNavSupplyAction;
    private Button mainMenuNavCommandAction;
    private Button mainMenuNavTechTreeAction;
    private Button mainMenuNavProfileAction;
    private Button mainMenuCardCampaignAction;
    private Button mainMenuCardSkirmishAction;
    private Button mainMenuCardOperationsAction;
    private Button mainMenuCommanderAction;
    private Button mainMenuDeployAction;
    private Label mainMenuCreditsValueLabel;
    private Label mainMenuSuppliesValueLabel;
    private Label mainMenuCommandValueLabel;
    private VisualElement mainMenuCommanderPortrait;
    private Label mainMenuCommanderNameLabel;
    private Label mainMenuCommanderSubtitleLabel;
    private VisualElement loadingBody;
    private VisualElement loadingBackdrop;
    private VisualElement loadingLogoLockup;
    private Label loadingTitleLabel;
    private VisualElement loadingPanelFrame;
    private Label loadingStatusLabel;
    private Label loadingPercentLabel;
    private VisualElement loadingProgressFrame;
    private VisualElement loadingProgressFill;
    private VisualElement loadingSpinner;
    private Label loadingBottomStatusLabel;
    private int lastLoadingPercent = -1;
    private bool hasLastLoadingStatus;
    private string lastLoadingStatus;
    private readonly Dictionary<Button, EventCallback<ClickEvent>> matchHudActionCallbacks = new();
    private VisualElement matchHudSelectedPanel;
    private VisualElement matchHudSelectedBadge;
    private Label matchHudSelectedTitleLabel;
    private Label matchHudSelectedSubtitleLabel;
    private VisualElement matchHudSelectedHealthFill;
    private Label matchHudSelectedHealthTextLabel;
    private Label matchHudSelectedOrderValueLabel;
    private Label matchHudOrderTextLabel;
    private Label matchHudSquadTextLabel;
    private Label matchHudCreditsValueLabel;
    private Label matchHudFuelValueLabel;
    private Label matchHudSupplyValueLabel;
    private Label matchHudCivilianRiskValueLabel;
    private VisualElement matchHudObjectivesPanel;
    private Label matchHudObjectivesTitleLabel;
    private Label matchHudObjective0Label;
    private Label matchHudObjective1Label;
    private Label matchHudObjective2Label;
    private VisualElement matchHudObjective0Icon;
    private VisualElement matchHudObjective1Icon;
    private VisualElement matchHudObjective2Icon;
    private Label matchHudObjectivesElapsedLabel;
    private VisualElement matchHudThreatJumpPanel;
    private Label matchHudThreatTitleLabel;
    private Label matchHudThreatSubtitleLabel;
    private Button matchHudThreatJumpAction;
    private VisualElement matchHudFeedbackPanel;
    private Label matchHudFeedbackTextLabel;
    private VisualElement matchHudFeedbackActions;
    private Button matchHudFeedbackBoardAllAction;
    private Button matchHudFeedbackCancelAction;
    private VisualElement matchHudMinimapPanel;
    private VisualElement matchHudMinimapViewport;
    private VisualElement matchHudMinimapFriendlyA;
    private VisualElement matchHudMinimapFriendlyB;
    private VisualElement matchHudMinimapHostileA;
    private VisualElement matchHudMinimapCivilian;
    private Button matchHudMinimapZoomInAction;
    private Button matchHudMinimapZoomOutAction;
    private Button matchHudMinimapFocusAction;
    private Button matchHudSelectedReturnAction;
    private Button matchHudSelectedDestroyAction;
    private Button matchHudSelectedBoardAction;
    private Button matchHudPassengerChip;
    private VisualElement matchHudPassengerDrawer;
    private Label matchHudPassengerChipLabel;
    private Label matchHudPassengerDrawerHeaderLabel;
    private VisualElement matchHudPassengerEmptyState;
    private readonly VisualElement[] matchHudPassengerRows = new VisualElement[UiMatchHudPassengerDrawerModel.MaxRows];
    private readonly Label[] matchHudPassengerNameLabels = new Label[UiMatchHudPassengerDrawerModel.MaxRows];
    private readonly Label[] matchHudPassengerRoleLabels = new Label[UiMatchHudPassengerDrawerModel.MaxRows];
    private readonly VisualElement[] matchHudPassengerHealthFills = new VisualElement[UiMatchHudPassengerDrawerModel.MaxRows];
    private readonly Label[] matchHudPassengerHealthLabels = new Label[UiMatchHudPassengerDrawerModel.MaxRows];
    private readonly Button[] matchHudSquadCards = new Button[UiMatchHudSquadTrayModel.MaxCards];
    private readonly Label[] matchHudSquadTitleLabels = new Label[UiMatchHudSquadTrayModel.MaxCards];
    private readonly VisualElement[] matchHudSquadHealthFills = new VisualElement[UiMatchHudSquadTrayModel.MaxCards];
    private readonly Label[] matchHudSquadHealthLabels = new Label[UiMatchHudSquadTrayModel.MaxCards];
    private Button matchHudSelectCommand;
    private Button matchHudMoveCommand;
    private Button matchHudAttackCommand;
    private Button matchHudHoldCommand;
    private Button matchHudStopCommand;
    private Button matchHudBuildCommand;
    private Button matchHudScanCommand;
    private Button matchHudSupportCommand;
    private Button matchHudRightBuildCommand;
    private Button matchHudRightSupportCommand;
    private Button buildDrawerBuildAction;
    private Button buildDrawerRushAction;
    private Button buildDrawerClearAction;
    private Button buildDrawerCloseAction;
    private EventCallback<ClickEvent> buildDrawerBuildActionCallback;
    private EventCallback<ClickEvent> buildDrawerCloseActionCallback;
    private EventCallback<ClickEvent> buildDrawerRushActionCallback;
    private EventCallback<ClickEvent> buildDrawerClearActionCallback;
    private EventCallback<ClickEvent> buildDrawerActiveProductionCancelActionCallback;
    private readonly EventCallback<ClickEvent>[] buildDrawerCatalogActionCallbacks = new EventCallback<ClickEvent>[BuildDrawerCatalogItemCount];
    private readonly EventCallback<ClickEvent>[] buildDrawerQueueActionCallbacks = new EventCallback<ClickEvent>[BuildDrawerQueueItemCount];
    private ScrollView buildDrawerCatalogScrollView;
    private ScrollView buildDrawerProductionScrollView;
    private VisualElement buildDrawerBuildPanel;
    private VisualElement buildDrawerProductionPanel;
    private VisualElement buildDrawerBuildIcon;
    private VisualElement buildDrawerPreview;
    private VisualElement buildDrawerFootprintIcon;
    private VisualElement buildDrawerPlacementIcon;
    private VisualElement buildDrawerProductionTimeIcon;
    private VisualElement buildDrawerCreditsCostIcon;
    private VisualElement buildDrawerSuppliesCostIcon;
    private VisualElement buildDrawerInstructionCursorIcon;
    private VisualElement buildDrawerInstructionInfoIcon;
    private VisualElement buildDrawerRushIcon;
    private VisualElement buildDrawerClearIcon;
    private Label buildDrawerNameLabel;
    private Label buildDrawerRoleLabel;
    private Label buildDrawerDescriptionLabel;
    private Label buildDrawerFootprintValueLabel;
    private Label buildDrawerRequirementsValueLabel;
    private Label buildDrawerPlacementValueLabel;
    private Label buildDrawerProductionTimeValueLabel;
    private Label buildDrawerCreditsCostValueLabel;
    private Label buildDrawerSuppliesCostValueLabel;
    private Label buildDrawerInstructionLabel;
    private Label buildDrawerNoProductionLabel;
    private Label buildDrawerProductionTitleLabel;
    private Label buildDrawerProductionCountLabel;
    private VisualElement buildDrawerActiveProductionRow;
    private VisualElement buildDrawerActiveProductionImage;
    private Label buildDrawerActiveProductionNameLabel;
    private Label buildDrawerActiveProductionPercentLabel;
    private VisualElement buildDrawerActiveProductionFill;
    private Button buildDrawerActiveProductionCancelAction;
    private readonly Button[] buildDrawerCatalogItems = new Button[BuildDrawerCatalogItemCount];
    private readonly VisualElement[] buildDrawerCatalogThumbs = new VisualElement[BuildDrawerCatalogItemCount];
    private readonly Label[] buildDrawerCatalogTitleLabels = new Label[BuildDrawerCatalogItemCount];
    private readonly Label[] buildDrawerCatalogRoleLabels = new Label[BuildDrawerCatalogItemCount];
    private readonly VisualElement[] buildDrawerCatalogCreditsIcons = new VisualElement[BuildDrawerCatalogItemCount];
    private readonly VisualElement[] buildDrawerCatalogSuppliesIcons = new VisualElement[BuildDrawerCatalogItemCount];
    private readonly VisualElement[] buildDrawerCatalogTimeIcons = new VisualElement[BuildDrawerCatalogItemCount];
    private readonly Label[] buildDrawerCatalogCreditsLabels = new Label[BuildDrawerCatalogItemCount];
    private readonly Label[] buildDrawerCatalogSuppliesLabels = new Label[BuildDrawerCatalogItemCount];
    private readonly Label[] buildDrawerCatalogTimeLabels = new Label[BuildDrawerCatalogItemCount];
    private readonly VisualElement[] buildDrawerQueueRows = new VisualElement[BuildDrawerQueueItemCount];
    private readonly VisualElement[] buildDrawerQueueImages = new VisualElement[BuildDrawerQueueItemCount];
    private readonly Label[] buildDrawerQueueNumberLabels = new Label[BuildDrawerQueueItemCount];
    private readonly Label[] buildDrawerQueueNameLabels = new Label[BuildDrawerQueueItemCount];
    private readonly Label[] buildDrawerQueueTimeLabels = new Label[BuildDrawerQueueItemCount];
    private readonly Button[] buildDrawerQueueOrderActions = new Button[BuildDrawerQueueItemCount];

    public UIDocument Document => document;
    public VisualTreeAsset ShellAsset => shellAsset;
    public VisualTreeAsset LoadingScreenAsset => loadingScreenAsset;
    public VisualTreeAsset MainMenuScreenAsset => mainMenuScreenAsset;
    public VisualTreeAsset MatchHudScreenAsset => matchHudScreenAsset;
    public VisualTreeAsset BuildDrawerPopupAsset => buildDrawerPopupAsset;
    public VisualElement Root => root;
    public VisualElement SafeAreaRoot => safeAreaRoot;
    public VisualElement HeaderBar => headerBar;
    public VisualElement ContentRoot => contentRoot;
    public VisualElement FooterBar => footerBar;
    public VisualElement ModalOverlay => modalOverlay;
    public VisualElement TooltipLayer => tooltipLayer;
    public VisualElement LoadingLayer => loadingLayer;
    public VisualElement MenuBackgroundRegion => menuBackgroundRegion;
    public VisualElement LoadingScreenSlot => loadingScreenSlot;
    public VisualElement MainMenuScreenSlot => mainMenuScreenSlot;
    public VisualElement MatchScreenSlot => matchScreenSlot;
    public VisualElement ArmoryScreenSlot => armoryScreenSlot;
    public VisualElement CommanderProfileScreenSlot => commanderProfileScreenSlot;
    public VisualElement ResultScreenSlot => resultScreenSlot;
    public VisualElement PopupScreenSlot => popupScreenSlot;
    public VisualElement LoadingContentRoot => loadingContentRoot;
    public VisualElement MainMenuContentRoot => mainMenuContentRoot;
    public VisualElement MatchHudContentRoot => matchHudContentRoot;
    public VisualElement BuildDrawerPopupRoot => buildDrawerPopupRoot;
    public VisualElement MainMenuHeaderContent => mainMenuHeaderContent;
    public VisualElement LoadingProgressFill => loadingProgressFill;
    public Label LoadingStatusLabel => loadingStatusLabel;
    public Label LoadingPercentLabel => loadingPercentLabel;
    public Label LoadingBottomStatusLabel => loadingBottomStatusLabel;
    public VisualElement MainMenuCommanderPortrait => mainMenuCommanderPortrait;
    public Label MainMenuCommanderNameLabel => mainMenuCommanderNameLabel;
    public Label MainMenuCommanderSubtitleLabel => mainMenuCommanderSubtitleLabel;
    public Label MainMenuCreditsValueLabel => mainMenuCreditsValueLabel;
    public Label MainMenuSuppliesValueLabel => mainMenuSuppliesValueLabel;
    public Label MainMenuCommandValueLabel => mainMenuCommandValueLabel;
    public VisualElement MatchHudSelectedPanel => matchHudSelectedPanel;
    public VisualElement MatchHudSelectedBadge => matchHudSelectedBadge;
    public Label MatchHudSelectedTitleLabel => matchHudSelectedTitleLabel;
    public Label MatchHudSelectedSubtitleLabel => matchHudSelectedSubtitleLabel;
    public VisualElement MatchHudSelectedHealthFill => matchHudSelectedHealthFill;
    public Label MatchHudSelectedHealthTextLabel => matchHudSelectedHealthTextLabel;
    public Label MatchHudSelectedOrderValueLabel => matchHudSelectedOrderValueLabel;
    public Label MatchHudOrderTextLabel => matchHudOrderTextLabel;
    public Label MatchHudSquadTextLabel => matchHudSquadTextLabel;
    public Label MatchHudCreditsValueLabel => matchHudCreditsValueLabel;
    public Label MatchHudFuelValueLabel => matchHudFuelValueLabel;
    public Label MatchHudSupplyValueLabel => matchHudSupplyValueLabel;
    public Label MatchHudCivilianRiskValueLabel => matchHudCivilianRiskValueLabel;
    public VisualElement MatchHudObjectivesPanel => matchHudObjectivesPanel;
    public Label MatchHudObjectivesTitleLabel => matchHudObjectivesTitleLabel;
    public Label MatchHudObjective0Label => matchHudObjective0Label;
    public Label MatchHudObjective1Label => matchHudObjective1Label;
    public Label MatchHudObjective2Label => matchHudObjective2Label;
    public VisualElement MatchHudObjective0Icon => matchHudObjective0Icon;
    public VisualElement MatchHudObjective1Icon => matchHudObjective1Icon;
    public VisualElement MatchHudObjective2Icon => matchHudObjective2Icon;
    public Label MatchHudObjectivesElapsedLabel => matchHudObjectivesElapsedLabel;
    public VisualElement MatchHudThreatJumpPanel => matchHudThreatJumpPanel;
    public Label MatchHudThreatTitleLabel => matchHudThreatTitleLabel;
    public Label MatchHudThreatSubtitleLabel => matchHudThreatSubtitleLabel;
    public Button MatchHudThreatJumpAction => matchHudThreatJumpAction;
    public VisualElement MatchHudFeedbackPanel => matchHudFeedbackPanel;
    public Label MatchHudFeedbackTextLabel => matchHudFeedbackTextLabel;
    public Button MatchHudFeedbackBoardAllAction => matchHudFeedbackBoardAllAction;
    public Button MatchHudFeedbackCancelAction => matchHudFeedbackCancelAction;
    public VisualElement MatchHudMinimapPanel => matchHudMinimapPanel;
    public VisualElement MatchHudMinimapViewport => matchHudMinimapViewport;
    public VisualElement MatchHudMinimapFriendlyA => matchHudMinimapFriendlyA;
    public VisualElement MatchHudMinimapFriendlyB => matchHudMinimapFriendlyB;
    public VisualElement MatchHudMinimapHostileA => matchHudMinimapHostileA;
    public VisualElement MatchHudMinimapCivilian => matchHudMinimapCivilian;
    public Button MatchHudMinimapZoomInAction => matchHudMinimapZoomInAction;
    public Button MatchHudMinimapZoomOutAction => matchHudMinimapZoomOutAction;
    public Button MatchHudMinimapFocusAction => matchHudMinimapFocusAction;
    public Button MatchHudSelectedReturnAction => matchHudSelectedReturnAction;
    public Button MatchHudSelectedDestroyAction => matchHudSelectedDestroyAction;
    public Button MatchHudSelectedBoardAction => matchHudSelectedBoardAction;
    public Button MatchHudPassengerChip => matchHudPassengerChip;
    public VisualElement MatchHudPassengerDrawer => matchHudPassengerDrawer;
    public Label MatchHudPassengerChipLabel => matchHudPassengerChipLabel;
    public Label MatchHudPassengerDrawerHeaderLabel => matchHudPassengerDrawerHeaderLabel;
    public VisualElement MatchHudPassengerEmptyState => matchHudPassengerEmptyState;
    public IReadOnlyList<VisualElement> MatchHudPassengerRows => matchHudPassengerRows;
    public IReadOnlyList<Label> MatchHudPassengerNameLabels => matchHudPassengerNameLabels;
    public IReadOnlyList<Label> MatchHudPassengerRoleLabels => matchHudPassengerRoleLabels;
    public IReadOnlyList<VisualElement> MatchHudPassengerHealthFills => matchHudPassengerHealthFills;
    public IReadOnlyList<Label> MatchHudPassengerHealthLabels => matchHudPassengerHealthLabels;
    public IReadOnlyList<Button> MatchHudSquadCards => matchHudSquadCards;
    public IReadOnlyList<Label> MatchHudSquadTitleLabels => matchHudSquadTitleLabels;
    public IReadOnlyList<VisualElement> MatchHudSquadHealthFills => matchHudSquadHealthFills;
    public IReadOnlyList<Label> MatchHudSquadHealthLabels => matchHudSquadHealthLabels;
    public Button MatchHudSelectCommand => matchHudSelectCommand;
    public Button MatchHudMoveCommand => matchHudMoveCommand;
    public Button MatchHudAttackCommand => matchHudAttackCommand;
    public Button MatchHudHoldCommand => matchHudHoldCommand;
    public Button MatchHudStopCommand => matchHudStopCommand;
    public Button MatchHudBuildCommand => matchHudBuildCommand;
    public Button MatchHudScanCommand => matchHudScanCommand;
    public Button MatchHudSupportCommand => matchHudSupportCommand;
    public Button MatchHudRightBuildCommand => matchHudRightBuildCommand;
    public Button MatchHudRightSupportCommand => matchHudRightSupportCommand;
    public Button BuildDrawerBuildAction => buildDrawerBuildAction;
    public Button BuildDrawerRushAction => buildDrawerRushAction;
    public Button BuildDrawerClearAction => buildDrawerClearAction;
    public Button BuildDrawerCloseAction => buildDrawerCloseAction;
    public ScrollView BuildDrawerCatalogScrollView => buildDrawerCatalogScrollView;
    public ScrollView BuildDrawerProductionScrollView => buildDrawerProductionScrollView;
    public Label BuildDrawerNameLabel => buildDrawerNameLabel;
    public Label BuildDrawerRoleLabel => buildDrawerRoleLabel;
    public Label BuildDrawerInstructionLabel => buildDrawerInstructionLabel;
    public Label BuildDrawerProductionCountLabel => buildDrawerProductionCountLabel;
    public VisualElement BuildDrawerActiveProductionRow => buildDrawerActiveProductionRow;
    public VisualElement BuildDrawerActiveProductionImage => buildDrawerActiveProductionImage;
    public VisualElement BuildDrawerActiveProductionFill => buildDrawerActiveProductionFill;
    public Button BuildDrawerActiveProductionCancelAction => buildDrawerActiveProductionCancelAction;
    public VisualElement BuildDrawerBuildIcon => buildDrawerBuildIcon;
    public VisualElement BuildDrawerInstructionInfoIcon => buildDrawerInstructionInfoIcon;
    public VisualElement BuildDrawerRushIcon => buildDrawerRushIcon;
    public VisualElement BuildDrawerClearIcon => buildDrawerClearIcon;
    public IReadOnlyList<Button> BuildDrawerCatalogItems => buildDrawerCatalogItems;
    public IReadOnlyList<VisualElement> BuildDrawerCatalogThumbs => buildDrawerCatalogThumbs;
    public IReadOnlyList<Label> BuildDrawerCatalogTitleLabels => buildDrawerCatalogTitleLabels;
    public IReadOnlyList<VisualElement> BuildDrawerQueueRows => buildDrawerQueueRows;
    public IReadOnlyList<VisualElement> BuildDrawerQueueImages => buildDrawerQueueImages;
    public IReadOnlyList<Label> BuildDrawerQueueNameLabels => buildDrawerQueueNameLabels;
    public IReadOnlyList<Button> BuildDrawerQueueOrderActions => buildDrawerQueueOrderActions;
    public bool IsMounted => root != null;
    public bool HasMountedLoadingScreen =>
        loadingScreenContainer != null
        && loadingContentRoot != null
        && loadingScreenContainer.parent == loadingScreenSlot;
    public bool HasMountedMainMenuScreen =>
        mainMenuScreenContainer != null
        && mainMenuContentRoot != null
        && mainMenuScreenContainer.parent == mainMenuScreenSlot;
    public bool HasMountedMatchHudScreen =>
        matchHudScreenContainer != null
        && matchHudContentRoot != null
        && matchHudScreenContainer.parent == matchScreenSlot;
    public bool HasMountedBuildDrawerPopup =>
        buildDrawerPopupContainer != null
        && buildDrawerPopupRoot != null
        && buildDrawerPopupContainer.parent == popupScreenSlot;
    public bool HasRequiredMainMenuBindings =>
        mainMenuContentRoot != null
        && mainMenuHeaderContent != null
        && mainMenuInboxAction != null
        && mainMenuSettingsAction != null
        && mainMenuMenuAction != null
        && mainMenuNavCampaignAction != null
        && mainMenuNavArmoryAction != null
        && mainMenuNavSupplyAction != null
        && mainMenuNavCommandAction != null
        && mainMenuNavTechTreeAction != null
        && mainMenuNavProfileAction != null
        && mainMenuCardCampaignAction != null
        && mainMenuCardSkirmishAction != null
        && mainMenuCardOperationsAction != null
        && mainMenuCommanderAction != null
        && mainMenuDeployAction != null
        && mainMenuCreditsValueLabel != null
        && mainMenuSuppliesValueLabel != null
        && mainMenuCommandValueLabel != null
        && mainMenuCommanderPortrait != null
        && mainMenuCommanderNameLabel != null
        && mainMenuCommanderSubtitleLabel != null;
    public bool HasPersistentMainMenuHeader =>
        mainMenuHeaderContent != null
        && mainMenuHeaderContent.parent != null
        && !IsHiddenBySelfOrAncestor(mainMenuHeaderContent);
    public bool HasRequiredMatchHudBindings =>
        matchHudContentRoot != null
        && matchHudContentRoot.Q<VisualElement>("HeaderContent") != null
        && matchHudOrderTextLabel != null
        && matchHudSquadTextLabel != null
        && matchHudCreditsValueLabel != null
        && matchHudFuelValueLabel != null
        && matchHudSupplyValueLabel != null
        && matchHudCivilianRiskValueLabel != null
        && matchHudObjectivesPanel != null
        && matchHudObjectivesTitleLabel != null
        && matchHudObjective0Label != null
        && matchHudObjective1Label != null
        && matchHudObjective2Label != null
        && matchHudObjective0Icon != null
        && matchHudObjective1Icon != null
        && matchHudObjective2Icon != null
        && matchHudObjectivesElapsedLabel != null
        && matchHudThreatJumpPanel != null
        && matchHudThreatTitleLabel != null
        && matchHudThreatSubtitleLabel != null
        && matchHudThreatJumpAction != null
        && matchHudFeedbackPanel != null
        && matchHudFeedbackTextLabel != null
        && matchHudFeedbackBoardAllAction != null
        && matchHudFeedbackCancelAction != null
        && matchHudMinimapPanel != null
        && matchHudMinimapViewport != null
        && matchHudMinimapFriendlyA != null
        && matchHudMinimapFriendlyB != null
        && matchHudMinimapHostileA != null
        && matchHudMinimapCivilian != null
        && matchHudMinimapZoomInAction != null
        && matchHudMinimapZoomOutAction != null
        && matchHudMinimapFocusAction != null
        && matchHudSelectedPanel != null
        && matchHudSelectedTitleLabel != null
        && matchHudSelectedSubtitleLabel != null
        && matchHudSelectedHealthFill != null
        && matchHudSelectedHealthTextLabel != null
        && matchHudSelectedOrderValueLabel != null
        && matchHudSelectedReturnAction != null
        && matchHudSelectedDestroyAction != null
        && matchHudSelectedBoardAction != null
        && matchHudPassengerChip != null
        && matchHudPassengerDrawer != null
        && matchHudPassengerChipLabel != null
        && matchHudPassengerDrawerHeaderLabel != null
        && matchHudPassengerEmptyState != null
        && matchHudPassengerRows[0] != null
        && matchHudPassengerNameLabels[0] != null
        && matchHudPassengerRoleLabels[0] != null
        && matchHudPassengerHealthFills[0] != null
        && matchHudPassengerHealthLabels[0] != null
        && matchHudContentRoot.Q<VisualElement>("SquadTray") != null
        && matchHudSquadCards[0] != null
        && matchHudSquadTitleLabels[0] != null
        && matchHudSquadHealthFills[0] != null
        && matchHudSquadHealthLabels[0] != null
        && matchHudContentRoot.Q<VisualElement>("CommandRail") != null
        && matchHudSelectCommand != null
        && matchHudMoveCommand != null
        && matchHudAttackCommand != null
        && matchHudHoldCommand != null
        && matchHudStopCommand != null
        && matchHudBuildCommand != null
        && matchHudScanCommand != null
        && matchHudSupportCommand != null
        && matchHudRightBuildCommand != null
        && matchHudRightSupportCommand != null
        && matchHudContentRoot.Q<VisualElement>("MinimapPanel") != null
        && matchHudContentRoot.Q<VisualElement>("FeedbackPanel") != null
        && matchHudContentRoot.Q<Button>("BoardAllButton") != null
        && matchHudContentRoot.Q<Button>("CancelButton") != null;
    public bool HasRequiredBuildDrawerBindings =>
        buildDrawerPopupRoot != null
        && buildDrawerBuildPanel != null
        && buildDrawerProductionPanel != null
        && buildDrawerBuildIcon != null
        && buildDrawerPreview != null
        && buildDrawerFootprintIcon != null
        && buildDrawerPlacementIcon != null
        && buildDrawerProductionTimeIcon != null
        && buildDrawerCreditsCostIcon != null
        && buildDrawerSuppliesCostIcon != null
        && buildDrawerInstructionCursorIcon != null
        && buildDrawerInstructionInfoIcon != null
        && buildDrawerRushIcon != null
        && buildDrawerClearIcon != null
        && buildDrawerCatalogScrollView != null
        && buildDrawerProductionScrollView != null
        && buildDrawerBuildAction != null
        && buildDrawerRushAction != null
        && buildDrawerClearAction != null
        && buildDrawerCloseAction != null
        && buildDrawerNameLabel != null
        && buildDrawerRoleLabel != null
        && buildDrawerDescriptionLabel != null
        && buildDrawerFootprintValueLabel != null
        && buildDrawerRequirementsValueLabel != null
        && buildDrawerPlacementValueLabel != null
        && buildDrawerProductionTimeValueLabel != null
        && buildDrawerCreditsCostValueLabel != null
        && buildDrawerSuppliesCostValueLabel != null
        && buildDrawerInstructionLabel != null
        && buildDrawerNoProductionLabel != null
        && buildDrawerProductionTitleLabel != null
        && buildDrawerProductionCountLabel != null
        && buildDrawerActiveProductionRow != null
        && buildDrawerActiveProductionImage != null
        && buildDrawerActiveProductionNameLabel != null
        && buildDrawerActiveProductionPercentLabel != null
        && buildDrawerActiveProductionFill != null
        && buildDrawerActiveProductionCancelAction != null
        && buildDrawerCatalogItems[0] != null
        && buildDrawerCatalogThumbs[0] != null
        && buildDrawerCatalogTitleLabels[0] != null
        && buildDrawerCatalogRoleLabels[0] != null
        && buildDrawerCatalogCreditsIcons[0] != null
        && buildDrawerCatalogSuppliesIcons[0] != null
        && buildDrawerCatalogTimeIcons[0] != null
        && buildDrawerCatalogCreditsLabels[0] != null
        && buildDrawerCatalogSuppliesLabels[0] != null
        && buildDrawerCatalogTimeLabels[0] != null
        && buildDrawerQueueRows[0] != null
        && buildDrawerQueueImages[0] != null
        && buildDrawerQueueNumberLabels[0] != null
        && buildDrawerQueueNameLabels[0] != null
        && buildDrawerQueueTimeLabels[0] != null
        && buildDrawerQueueOrderActions[0] != null;
    public bool IsCommanderProfileSubRouteVisible =>
        commanderProfileScreenSlot != null
        && !IsHiddenBySelfOrAncestor(commanderProfileScreenSlot);
    public bool HasRequiredLoadingBindings =>
        loadingContentRoot != null
        && loadingBody != null
        && loadingBackdrop != null
        && loadingLogoLockup != null
        && loadingTitleLabel != null
        && loadingPanelFrame != null
        && loadingStatusLabel != null
        && loadingPercentLabel != null
        && loadingProgressFrame != null
        && loadingProgressFill != null
        && loadingSpinner != null
        && loadingBottomStatusLabel != null;
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
        Configure(configuredDocument, configuredShellAsset, null, null, null);
    }

    public void Configure(UIDocument configuredDocument, VisualTreeAsset configuredShellAsset, VisualTreeAsset configuredLoadingScreenAsset)
    {
        Configure(configuredDocument, configuredShellAsset, configuredLoadingScreenAsset, null, null);
    }

    public void Configure(
        UIDocument configuredDocument,
        VisualTreeAsset configuredShellAsset,
        VisualTreeAsset configuredLoadingScreenAsset,
        VisualTreeAsset configuredMainMenuScreenAsset)
    {
        Configure(
            configuredDocument,
            configuredShellAsset,
            configuredLoadingScreenAsset,
            configuredMainMenuScreenAsset,
            null);
    }

    public void Configure(
        UIDocument configuredDocument,
        VisualTreeAsset configuredShellAsset,
        VisualTreeAsset configuredLoadingScreenAsset,
        VisualTreeAsset configuredMainMenuScreenAsset,
        VisualTreeAsset configuredMatchHudScreenAsset)
    {
        Configure(
            configuredDocument,
            configuredShellAsset,
            configuredLoadingScreenAsset,
            configuredMainMenuScreenAsset,
            configuredMatchHudScreenAsset,
            null);
    }

    public void Configure(
        UIDocument configuredDocument,
        VisualTreeAsset configuredShellAsset,
        VisualTreeAsset configuredLoadingScreenAsset,
        VisualTreeAsset configuredMainMenuScreenAsset,
        VisualTreeAsset configuredMatchHudScreenAsset,
        VisualTreeAsset configuredBuildDrawerPopupAsset)
    {
        if (configuredDocument != null)
            document = configuredDocument;
        if (configuredShellAsset != null)
            shellAsset = configuredShellAsset;
        if (configuredLoadingScreenAsset != null)
            loadingScreenAsset = configuredLoadingScreenAsset;
        if (configuredMainMenuScreenAsset != null)
            mainMenuScreenAsset = configuredMainMenuScreenAsset;
        if (configuredMatchHudScreenAsset != null)
            matchHudScreenAsset = configuredMatchHudScreenAsset;
        if (configuredBuildDrawerPopupAsset != null)
            buildDrawerPopupAsset = configuredBuildDrawerPopupAsset;
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
        bool mountedShell = HasRequiredRegions && HasRequiredScreenSlots;
        if (mountedShell)
        {
            MountLoadingScreen();
            MountMainMenuScreen();
            MountMatchHudScreen();
            MountBuildDrawerPopup();
        }

        return mountedShell;
    }

    public bool MountLoadingScreen()
    {
        if (loadingScreenSlot == null || loadingScreenAsset == null)
            return false;

        if (HasMountedLoadingScreen)
            return true;

        loadingScreenSlot.Clear();
        loadingScreenContainer = loadingScreenAsset.Instantiate();
        loadingScreenContainer.name = "SCN01_LoadingContent_Template";
        loadingContentRoot = loadingScreenContainer.Q<VisualElement>("SCN01_LoadingContent");
        loadingScreenSlot.Add(loadingScreenContainer);
        BindLoadingScreen();
        ResetLoadingPresentationCache();
        ApplyLoadingProgress(new UiShellLoadingProgressModel(0f, DefaultLoadingStatus, false));
        return HasRequiredLoadingBindings;
    }

    public bool MountMainMenuScreen()
    {
        if (mainMenuScreenSlot == null || mainMenuScreenAsset == null)
            return false;

        if (HasMountedMainMenuScreen)
            return true;

        mainMenuScreenSlot.Clear();
        mainMenuScreenContainer = mainMenuScreenAsset.Instantiate();
        mainMenuScreenContainer.name = "SCN02_MainMenuContent_Template";
        mainMenuContentRoot = mainMenuScreenContainer.Q<VisualElement>("SCN02_MainMenuContent");
        mainMenuScreenSlot.Add(mainMenuScreenContainer);
        BindMainMenuScreen();
        return HasRequiredMainMenuBindings;
    }

    public bool MountMatchHudScreen()
    {
        if (matchScreenSlot == null || matchHudScreenAsset == null)
            return false;

        if (HasMountedMatchHudScreen)
            return true;

        matchScreenSlot.Clear();
        matchHudScreenContainer = matchHudScreenAsset.Instantiate();
        matchHudScreenContainer.name = "SCN08_MatchHudContent_Template";
        matchHudContentRoot = matchHudScreenContainer.Q<VisualElement>("SCN08_MatchHudContent");
        matchScreenSlot.Add(matchHudScreenContainer);
        BindMatchHudScreen();
        SetShellHidden(matchScreenSlot, true);
        return HasRequiredMatchHudBindings;
    }

    public bool MountBuildDrawerPopup()
    {
        if (popupScreenSlot == null || buildDrawerPopupAsset == null)
            return false;

        if (HasMountedBuildDrawerPopup)
            return true;

        popupScreenSlot.Clear();
        buildDrawerPopupContainer = buildDrawerPopupAsset.Instantiate();
        buildDrawerPopupContainer.name = "SCN09_BuildDrawerPopup_Template";
        buildDrawerPopupRoot = buildDrawerPopupContainer.Q<VisualElement>("SCN09_BuildDrawerPopup");
        popupScreenSlot.Add(buildDrawerPopupContainer);
        BindBuildDrawerPopup();
        SetShellHidden(popupScreenSlot, true);
        SetShellHidden(modalOverlay, true);
        return HasRequiredBuildDrawerBindings;
    }

    public bool TrySubmitMainMenuAction(string actionName)
    {
        switch (actionName)
        {
            case "DeployOperationButton":
                return EnqueueMainMenuRoute(UiShellRouteIntent.EnterMatch, UIRoute.Match, pushHistory: false);
            case "SettingsButton":
                return EnqueueMainMenuRoute(UiShellRouteIntent.OpenSettings, UIRoute.Settings, pushHistory: true);
            case "InboxButton":
                return EnqueueMainMenuRoute(UiShellRouteIntent.OpenMenuRoute, UIRoute.Inbox, pushHistory: true);
            case "MenuButton":
            case "Nav_Campaign":
            case "Card_Campaign":
                return EnqueueMainMenuRoute(UiShellRouteIntent.OpenMenuRoute, UIRoute.MainMenu, pushHistory: false);
            case "Nav_Armory":
                return EnqueueMainMenuRoute(UiShellRouteIntent.OpenMenuRoute, UIRoute.Armory, pushHistory: true);
            case "Nav_Supply":
                return EnqueueMainMenuRoute(UiShellRouteIntent.OpenMenuRoute, UIRoute.LoadoutSquadPrep, pushHistory: true);
            case "Nav_Command":
            case "Card_Operations":
                return EnqueueMainMenuRoute(UiShellRouteIntent.OpenMenuRoute, UIRoute.CommandExchange, pushHistory: true);
            case "Nav_TechTree":
                return EnqueueMainMenuRoute(UiShellRouteIntent.OpenMenuRoute, UIRoute.Events, pushHistory: true);
            case "Nav_Profile":
            case "CommanderPanel":
                return EnqueueMainMenuRoute(UiShellRouteIntent.OpenMenuRoute, UIRoute.CommandFeed, pushHistory: true);
            case "Card_Skirmish":
                return EnqueueMainMenuRoute(UiShellRouteIntent.OpenMenuRoute, UIRoute.QuickCustomSetup, pushHistory: true);
            default:
                return false;
        }
    }

    public bool TrySubmitMatchHudAction(UiActionKind kind, int payloadId = 0)
    {
        return kind != UiActionKind.None &&
            UiShellRuntimeGateway.TryEnqueueUiAction(kind, payloadId);
    }

    public bool ApplyLoadingProgress(UiShellLoadingProgressModel loading)
    {
        if (!HasRequiredLoadingBindings)
            return false;

        float clamped = Mathf.Clamp01(loading.Progress01);
        int percent = Mathf.Clamp(Mathf.RoundToInt(clamped * 100f), 0, 100);

        if (loadingProgressFill != null)
            loadingProgressFill.style.width = Length.Percent(percent);

        if (loadingPercentLabel != null && percent != lastLoadingPercent)
            loadingPercentLabel.text = PercentLabels[percent];

        lastLoadingPercent = percent;

        string resolvedStatus = string.IsNullOrEmpty(loading.Status) ? DefaultLoadingStatus : loading.Status;
        if (!hasLastLoadingStatus || resolvedStatus != lastLoadingStatus)
        {
            if (loadingStatusLabel != null)
                loadingStatusLabel.text = resolvedStatus;

            lastLoadingStatus = resolvedStatus;
            hasLastLoadingStatus = true;
        }

        return true;
    }

    public bool ApplyPresentationCommands(IReadOnlyList<UiShellPresentationCommandModel> commands)
    {
        if (commands == null || commands.Count == 0)
            return false;

        for (int i = 0; i < commands.Count; i++)
            ApplyPresentationCommand(commands[i]);

        return true;
    }

    public bool IsPointerOverAnyGameplayUi(Vector2 screenPosition, out string source)
    {
        source = null;
        return TryPickScreenPoint(screenPosition, out VisualElement pickedElement) &&
            IsElementBlockingUi(pickedElement, out source);
    }

    public bool IsPointerOverPlacementUi(Vector2 screenPosition)
    {
        return IsPointerOverAnyGameplayUi(screenPosition, out _);
    }

    public bool IsPointerOverRaycastableUi(Vector2 screenPosition, out string source)
    {
        return IsPointerOverAnyGameplayUi(screenPosition, out source);
    }

    public bool IsElementBlockingUi(VisualElement element, out string source)
    {
        source = null;
        if (root == null || element == null || !IsSelfOrDescendantOf(element, root))
            return false;

        if (IsHiddenBySelfOrAncestor(element))
            return false;

        if (IsSelfOrDescendantOf(element, loadingLayer))
        {
            source = DescribeUiElement(element, "LoadingLayer");
            return true;
        }

        if (IsSelfOrDescendantOf(element, modalOverlay))
        {
            source = DescribeUiElement(element, "ModalOverlay");
            return true;
        }

        if (IsSelfOrDescendantOf(element, headerBar))
        {
            source = DescribeUiElement(element, "HeaderBar");
            return true;
        }

        if (IsSelfOrDescendantOf(element, footerBar))
        {
            source = DescribeUiElement(element, "FooterBar");
            return true;
        }

        if (IsShellStructuralElement(element))
            return false;

        if (IsSelfOrDescendantOf(element, contentRoot) || IsSelfOrDescendantOf(element, tooltipLayer))
        {
            source = DescribeUiElement(element, "UiToolkitShell");
            return true;
        }

        return false;
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
        menuBackgroundRegion = null;
        loadingScreenSlot = null;
        mainMenuScreenSlot = null;
        matchScreenSlot = null;
        armoryScreenSlot = null;
        commanderProfileScreenSlot = null;
        resultScreenSlot = null;
        popupScreenSlot = null;
        loadingScreenContainer = null;
        mainMenuScreenContainer = null;
        matchHudScreenContainer = null;
        buildDrawerPopupContainer = null;
        loadingContentRoot = null;
        mainMenuContentRoot = null;
        matchHudContentRoot = null;
        buildDrawerPopupRoot = null;
        mainMenuHeaderContent = null;
        ClearLoadingBindings();
        ClearMainMenuBindings();
        ClearMatchHudBindings();
        ClearBuildDrawerBindings();
        ResetLoadingPresentationCache();
    }

    private static void RemoveMotionStateClasses(VisualElement target)
    {
        for (int i = 0; i < MotionStateClasses.Length; i++)
            target.RemoveFromClassList(MotionStateClasses[i]);
    }

    private void ApplyPresentationCommand(UiShellPresentationCommandModel command)
    {
        switch (command.Kind)
        {
            case UiShellCommandKind.ShowLoading:
                SetShellHidden(loadingLayer, false);
                ApplyShellMotion(loadingScreenSlot, UiToolkitShellMotionState.Visible);
                break;
            case UiShellCommandKind.ExitLoading:
                ApplyShellMotion(loadingScreenSlot, UiToolkitShellMotionState.FadeOut);
                SetShellHidden(loadingLayer, true);
                break;
            case UiShellCommandKind.EnterMenu:
            case UiShellCommandKind.SwapMenuMiddle:
                MountMainMenuScreen();
                ApplyMainMenuRouteState(command.Route);
                SetShellHidden(mainMenuScreenSlot, false);
                ApplyShellMotion(mainMenuScreenSlot, UiToolkitShellMotionState.Visible);
                break;
            case UiShellCommandKind.ExitMenu:
                ApplyShellMotion(mainMenuScreenSlot, UiToolkitShellMotionState.ScaleOut);
                SetShellHidden(mainMenuScreenSlot, true);
                break;
            case UiShellCommandKind.EnterMatchHud:
                MountMatchHudScreen();
                SetShellHidden(matchScreenSlot, false);
                ApplyShellMotion(matchScreenSlot, UiToolkitShellMotionState.Visible);
                break;
            case UiShellCommandKind.ExitMatchHud:
                ApplyShellMotion(matchScreenSlot, UiToolkitShellMotionState.ScaleOut);
                SetShellHidden(matchScreenSlot, true);
                break;
            case UiShellCommandKind.ShowPopup:
                MountBuildDrawerPopup();
                SetShellHidden(modalOverlay, false);
                SetShellHidden(popupScreenSlot, false);
                ApplyShellMotion(popupScreenSlot, UiToolkitShellMotionState.PopupVisible);
                break;
            case UiShellCommandKind.HidePopup:
                ApplyShellMotion(popupScreenSlot, UiToolkitShellMotionState.PopupHidden);
                SetShellHidden(popupScreenSlot, true);
                SetShellHidden(modalOverlay, true);
                break;
        }
    }

    private static void SetShellHidden(VisualElement target, bool hidden)
    {
        if (target == null)
            return;

        if (hidden)
        {
            if (!target.ClassListContains("shell-hidden"))
                target.AddToClassList("shell-hidden");
            return;
        }

        target.RemoveFromClassList("shell-hidden");
    }

    private static void SetElementEnabled(VisualElement target, bool enabled)
    {
        target?.SetEnabled(enabled);
    }

    public bool ApplyMainMenuRouteState(UIRoute route)
    {
        if (mainMenuContentRoot == null)
            return false;

        ApplyMainMenuRouteClass(route);
        SetClass(mainMenuNavCampaignAction, "nav-item-selected", route == UIRoute.MainMenu);
        SetClass(mainMenuNavArmoryAction, "nav-item-selected", route == UIRoute.Armory);
        SetClass(mainMenuNavSupplyAction, "nav-item-selected", route == UIRoute.LoadoutSquadPrep);
        SetClass(mainMenuNavCommandAction, "nav-item-selected", route == UIRoute.CommandExchange);
        SetClass(mainMenuNavTechTreeAction, "nav-item-selected", route == UIRoute.Events);
        SetClass(mainMenuNavProfileAction, "nav-item-selected", route == UIRoute.CommandFeed);

        SetClass(mainMenuCardCampaignAction, "mode-card-selected", route == UIRoute.MainMenu);
        SetClass(mainMenuCardSkirmishAction, "mode-card-selected", route == UIRoute.QuickCustomSetup);
        SetClass(mainMenuCardOperationsAction, "mode-card-selected", route == UIRoute.CommandExchange);
        SetShellHidden(commanderProfileScreenSlot, route != UIRoute.CommandFeed);
        return true;
    }

    public bool ApplyMainMenuCommanderProfile(UiShellCommanderProfileModel profile)
    {
        if (mainMenuCommanderPortrait == null ||
            mainMenuCommanderNameLabel == null ||
            mainMenuCommanderSubtitleLabel == null)
        {
            return false;
        }

        mainMenuCommanderNameLabel.text = string.IsNullOrWhiteSpace(profile.Name)
            ? DefaultCommanderName
            : profile.Name;
        mainMenuCommanderSubtitleLabel.text = string.IsNullOrWhiteSpace(profile.Subtitle)
            ? DefaultCommanderSubtitle
            : profile.Subtitle;

        string portraitClass = string.IsNullOrWhiteSpace(profile.PortraitClass)
            ? DefaultCommanderPortraitClass
            : profile.PortraitClass;
        ApplyKnownClass(mainMenuCommanderPortrait, MainMenuCommanderPortraitClasses, portraitClass);
        return true;
    }

    public bool ApplyMatchHudSelection(UiMatchHudSelectionPanelModel selection)
    {
        if (matchHudSelectedPanel == null)
            return false;

        SetShellHidden(matchHudSelectedPanel, !selection.Visible);
        if (!selection.Visible)
            return true;

        if (matchHudSelectedTitleLabel != null)
        {
            matchHudSelectedTitleLabel.text = string.IsNullOrWhiteSpace(selection.Title)
                ? "SELECTED UNIT"
                : selection.Title;
        }

        if (matchHudSelectedSubtitleLabel != null)
        {
            matchHudSelectedSubtitleLabel.text = string.IsNullOrWhiteSpace(selection.Subtitle)
                ? "TACTICAL ASSET"
                : selection.Subtitle;
        }

        if (matchHudSelectedOrderValueLabel != null)
        {
            matchHudSelectedOrderValueLabel.text = string.IsNullOrWhiteSpace(selection.CurrentOrder)
                ? "IDLE"
                : selection.CurrentOrder;
        }

        if (matchHudSelectedHealthTextLabel != null)
        {
            matchHudSelectedHealthTextLabel.text = string.IsNullOrWhiteSpace(selection.HealthText)
                ? "HEALTH -"
                : selection.HealthText;
        }

        if (matchHudSelectedHealthFill != null)
            matchHudSelectedHealthFill.style.width = Length.Percent(Mathf.Clamp01(selection.Health01) * 100f);

        SetShellHidden(matchHudSelectedBadge, !selection.BadgeVisible);
        SetElementEnabled(matchHudSelectedReturnAction, selection.ReturnEnabled);
        SetElementEnabled(matchHudSelectedDestroyAction, selection.DestroyEnabled);
        SetElementEnabled(matchHudSelectedBoardAction, selection.BoardEnabled);
        return true;
    }

    public bool ApplyMatchHudCommandState(UiMatchHudCommandStateModel commandState)
    {
        if (matchHudContentRoot == null)
            return false;

        TacticalCommandMode activeMode = commandState.ActiveCommandMode;
        bool buildSelected = commandState.BuildDrawerVisible || activeMode == TacticalCommandMode.Build;
        SetClass(matchHudSelectCommand, "command-button-selected", activeMode == TacticalCommandMode.Select);
        SetClass(matchHudMoveCommand, "command-button-selected", activeMode == TacticalCommandMode.Move);
        SetClass(matchHudAttackCommand, "command-button-selected", activeMode == TacticalCommandMode.Attack);
        SetClass(matchHudHoldCommand, "command-button-selected", activeMode == TacticalCommandMode.Hold);
        SetClass(matchHudStopCommand, "command-button-selected", activeMode == TacticalCommandMode.Stop);
        SetClass(matchHudBuildCommand, "command-button-selected", buildSelected);
        SetClass(matchHudScanCommand, "command-button-selected", activeMode == TacticalCommandMode.Scan);
        SetClass(matchHudSupportCommand, "command-button-selected", activeMode == TacticalCommandMode.Special);
        SetClass(matchHudRightBuildCommand, "quick-command-selected", buildSelected);
        SetClass(matchHudRightSupportCommand, "quick-command-selected", activeMode == TacticalCommandMode.Special);
        return true;
    }

    public bool ApplyBuildDrawer(UiBuildDrawerModel drawer)
    {
        if (buildDrawerPopupRoot == null || buildDrawerCatalogItems[0] == null)
            return false;

        SetLabelText(buildDrawerNameLabel, drawer.Name, "SELECT STRUCTURE");
        SetLabelText(buildDrawerRoleLabel, drawer.Role, "BUILD CATALOG");
        SetLabelText(buildDrawerDescriptionLabel, drawer.Description, "Select a structure to view details and start construction.");
        SetLabelText(buildDrawerFootprintValueLabel, drawer.FootprintText, "-");
        SetLabelText(buildDrawerRequirementsValueLabel, drawer.RequirementsText, "READY");
        SetLabelText(buildDrawerPlacementValueLabel, drawer.PlacementText, "VALID GROUND");
        SetLabelText(buildDrawerProductionTimeValueLabel, drawer.ProductionTimeText, "00:00");
        SetLabelText(buildDrawerCreditsCostValueLabel, drawer.CreditsCostText, "0");
        SetLabelText(buildDrawerSuppliesCostValueLabel, drawer.SuppliesCostText, "0");
        SetLabelText(buildDrawerInstructionLabel, drawer.InstructionText, "Select a structure to view details and start construction.");
        SetLabelText(buildDrawerProductionTitleLabel, drawer.ProductionTitle, "PRODUCTION");
        SetLabelText(buildDrawerProductionCountLabel, drawer.ProductionCountText, "0/0");
        SetElementEnabled(buildDrawerBuildAction, drawer.BuildEnabled);
        SetElementEnabled(buildDrawerRushAction, drawer.RushEnabled);
        SetElementEnabled(buildDrawerClearAction, drawer.ClearEnabled);

        SetShellHidden(buildDrawerNoProductionLabel, !drawer.NoProductionVisible);
        ApplyBuildDrawerActiveProduction(drawer.ActiveProduction);

        int catalogCount = Mathf.Clamp(drawer.CatalogItemCount, 0, buildDrawerCatalogItems.Length);
        for (int i = 0; i < buildDrawerCatalogItems.Length; i++)
        {
            bool visible = i < catalogCount;
            UiBuildDrawerCatalogItemModel item = drawer.GetCatalogItem(i);
            ApplyBuildDrawerCatalogItem(i, visible, item);
        }

        int queueCount = Mathf.Clamp(drawer.QueueRowCount, 0, buildDrawerQueueRows.Length);
        for (int i = 0; i < buildDrawerQueueRows.Length; i++)
        {
            bool visible = i < queueCount;
            UiBuildDrawerQueueRowModel row = drawer.GetQueueRow(i);
            ApplyBuildDrawerQueueRow(i, visible, row);
        }

        return true;
    }

    public bool ApplyMatchHudPassengerDrawer(UiMatchHudPassengerDrawerModel passengerDrawer)
    {
        if (matchHudPassengerChip == null || matchHudPassengerDrawer == null)
            return false;

        string countText = $"PASSENGERS {passengerDrawer.PassengerCount}/{passengerDrawer.PassengerCapacity}";
        SetClass(matchHudPassengerChip, "passenger-chip-visible", passengerDrawer.ChipVisible);
        SetClass(matchHudPassengerDrawer, "transport-passenger-drawer-visible", passengerDrawer.ChipVisible && passengerDrawer.DrawerVisible);

        if (matchHudPassengerChipLabel != null)
            matchHudPassengerChipLabel.text = countText;
        if (matchHudPassengerDrawerHeaderLabel != null)
            matchHudPassengerDrawerHeaderLabel.text = countText;

        bool hasRows = passengerDrawer.RowCount > 0;
        SetShellHidden(matchHudPassengerEmptyState, hasRows);

        for (int i = 0; i < matchHudPassengerRows.Length; i++)
        {
            bool rowVisible = i < passengerDrawer.RowCount;
            SetShellHidden(matchHudPassengerRows[i], !rowVisible);
            if (!rowVisible)
                continue;

            UiMatchHudPassengerRowModel row = passengerDrawer.GetRow(i);
            if (matchHudPassengerNameLabels[i] != null)
            {
                matchHudPassengerNameLabels[i].text = string.IsNullOrWhiteSpace(row.Name)
                    ? "PASSENGER"
                    : row.Name;
            }

            if (matchHudPassengerRoleLabels[i] != null)
            {
                matchHudPassengerRoleLabels[i].text = string.IsNullOrWhiteSpace(row.Role)
                    ? "ONBOARD"
                    : row.Role;
            }

            if (matchHudPassengerHealthLabels[i] != null)
            {
                matchHudPassengerHealthLabels[i].text = string.IsNullOrWhiteSpace(row.HealthText)
                    ? "HEALTH -"
                    : row.HealthText;
            }

            if (matchHudPassengerHealthFills[i] != null)
                matchHudPassengerHealthFills[i].style.width = Length.Percent(Mathf.Clamp01(row.Health01) * 100f);
        }

        return true;
    }

    public bool ApplyMatchHudSquadTray(UiMatchHudSquadTrayModel squadTray)
    {
        if (matchHudContentRoot == null || matchHudSquadCards[0] == null)
            return false;

        for (int i = 0; i < matchHudSquadCards.Length; i++)
        {
            UiMatchHudSquadTrayCardModel card = squadTray.GetCard(i);
            SetShellHidden(matchHudSquadCards[i], !card.Visible);
            SetClass(matchHudSquadCards[i], "squad-card-selected", ToSquadTraySlot(i) == squadTray.SelectedSlot);
            if (!card.Visible)
                continue;

            if (matchHudSquadTitleLabels[i] != null)
            {
                matchHudSquadTitleLabels[i].text = string.IsNullOrWhiteSpace(card.Title)
                    ? "SQUAD"
                    : card.Title;
            }

            if (matchHudSquadHealthLabels[i] != null)
            {
                matchHudSquadHealthLabels[i].text = string.IsNullOrWhiteSpace(card.HealthText)
                    ? "0/0"
                    : card.HealthText;
            }

            if (matchHudSquadHealthFills[i] != null)
                matchHudSquadHealthFills[i].style.width = Length.Percent(Mathf.Clamp01(card.Health01) * 100f);
        }

        return true;
    }

    public bool ApplyMatchHudHeader(UiMatchHudHeaderModel header)
    {
        if (matchHudOrderTextLabel == null ||
            matchHudSquadTextLabel == null ||
            matchHudCreditsValueLabel == null ||
            matchHudFuelValueLabel == null ||
            matchHudSupplyValueLabel == null ||
            matchHudCivilianRiskValueLabel == null)
        {
            return false;
        }

        matchHudOrderTextLabel.text = header.OrderText;
        matchHudSquadTextLabel.text = header.SquadText;
        matchHudCreditsValueLabel.text = header.CreditsText;
        matchHudFuelValueLabel.text = header.FuelText;
        matchHudSupplyValueLabel.text = header.SupplyText;
        matchHudCivilianRiskValueLabel.text = header.CivilianRiskText;
        return true;
    }

    public bool ApplyMatchHudStatusSurfaces(UiMatchHudStatusSurfacesModel statusSurfaces)
    {
        if (matchHudObjectivesTitleLabel == null ||
            matchHudObjective0Label == null ||
            matchHudObjective1Label == null ||
            matchHudObjective2Label == null ||
            matchHudObjectivesElapsedLabel == null ||
            matchHudThreatJumpPanel == null ||
            matchHudThreatTitleLabel == null ||
            matchHudThreatSubtitleLabel == null ||
            matchHudThreatJumpAction == null ||
            matchHudFeedbackPanel == null ||
            matchHudFeedbackTextLabel == null ||
            matchHudFeedbackBoardAllAction == null ||
            matchHudFeedbackCancelAction == null)
        {
            return false;
        }

        matchHudObjectivesTitleLabel.text = statusSurfaces.ObjectivesTitle;
        ApplyObjectiveRow(matchHudObjective0Label, matchHudObjective0Icon, statusSurfaces.Objective0);
        ApplyObjectiveRow(matchHudObjective1Label, matchHudObjective1Icon, statusSurfaces.Objective1);
        ApplyObjectiveRow(matchHudObjective2Label, matchHudObjective2Icon, statusSurfaces.Objective2);
        matchHudObjectivesElapsedLabel.text = statusSurfaces.ElapsedText;

        SetShellHidden(matchHudThreatJumpPanel, !statusSurfaces.ThreatVisible);
        matchHudThreatTitleLabel.text = statusSurfaces.ThreatTitle;
        matchHudThreatSubtitleLabel.text = statusSurfaces.ThreatSubtitle;
        matchHudThreatJumpAction.SetEnabled(statusSurfaces.JumpEnabled);

        SetShellHidden(matchHudFeedbackPanel, !statusSurfaces.FeedbackVisible);
        matchHudFeedbackTextLabel.text = statusSurfaces.FeedbackText;
        SetShellHidden(matchHudFeedbackBoardAllAction, !statusSurfaces.BoardAllVisible);
        matchHudFeedbackBoardAllAction.SetEnabled(statusSurfaces.BoardAllEnabled);
        SetShellHidden(matchHudFeedbackCancelAction, !statusSurfaces.CancelVisible);
        matchHudFeedbackCancelAction.SetEnabled(statusSurfaces.CancelEnabled);
        SetShellHidden(matchHudFeedbackActions, !statusSurfaces.BoardAllVisible && !statusSurfaces.CancelVisible);
        return true;
    }

    public bool ApplyMatchHudMinimap(UiMatchHudMinimapModel minimap)
    {
        if (matchHudMinimapViewport == null ||
            matchHudMinimapFriendlyA == null ||
            matchHudMinimapFriendlyB == null ||
            matchHudMinimapHostileA == null ||
            matchHudMinimapCivilian == null ||
            matchHudMinimapZoomInAction == null ||
            matchHudMinimapZoomOutAction == null ||
            matchHudMinimapFocusAction == null)
        {
            return false;
        }

        matchHudMinimapViewport.style.left = Length.Percent(Mathf.Clamp(minimap.ViewportLeftPercent, 0f, 100f));
        matchHudMinimapViewport.style.top = Length.Percent(Mathf.Clamp(minimap.ViewportTopPercent, 0f, 100f));
        matchHudMinimapViewport.style.width = Length.Percent(Mathf.Clamp(minimap.ViewportWidthPercent, 1f, 100f));
        matchHudMinimapViewport.style.height = Length.Percent(Mathf.Clamp(minimap.ViewportHeightPercent, 1f, 100f));

        ApplyMinimapMarker(matchHudMinimapFriendlyA, minimap.FriendlyA);
        ApplyMinimapMarker(matchHudMinimapFriendlyB, minimap.FriendlyB);
        ApplyMinimapMarker(matchHudMinimapHostileA, minimap.HostileA);
        ApplyMinimapMarker(matchHudMinimapCivilian, minimap.Civilian);

        matchHudMinimapZoomInAction.SetEnabled(minimap.ZoomInEnabled);
        matchHudMinimapZoomOutAction.SetEnabled(minimap.ZoomOutEnabled);
        matchHudMinimapFocusAction.SetEnabled(minimap.FocusEnabled);
        return true;
    }

    public bool ApplyMainMenuResources(UiShellMainMenuResourcesModel resources)
    {
        if (mainMenuCreditsValueLabel == null ||
            mainMenuSuppliesValueLabel == null ||
            mainMenuCommandValueLabel == null)
        {
            return false;
        }

        mainMenuCreditsValueLabel.text = string.IsNullOrWhiteSpace(resources.CreditsText)
            ? DefaultCreditsText
            : resources.CreditsText;
        mainMenuSuppliesValueLabel.text = string.IsNullOrWhiteSpace(resources.SuppliesText)
            ? DefaultSuppliesText
            : resources.SuppliesText;
        mainMenuCommandValueLabel.text = string.IsNullOrWhiteSpace(resources.CommandText)
            ? DefaultCommandText
            : resources.CommandText;
        return true;
    }

    private void ApplyMainMenuRouteClass(UIRoute route)
    {
        for (int i = 0; i < MainMenuRouteClasses.Length; i++)
            mainMenuContentRoot.RemoveFromClassList(MainMenuRouteClasses[i]);

        mainMenuContentRoot.AddToClassList(ResolveMainMenuRouteClass(route));
    }

    private static string ResolveMainMenuRouteClass(UIRoute route)
    {
        switch (route)
        {
            case UIRoute.Armory:
                return "main-menu-route-armory";
            case UIRoute.LoadoutSquadPrep:
                return "main-menu-route-supply";
            case UIRoute.CommandExchange:
                return "main-menu-route-command";
            case UIRoute.Events:
                return "main-menu-route-tech-tree";
            case UIRoute.CommandFeed:
                return "main-menu-route-profile";
            case UIRoute.QuickCustomSetup:
                return "main-menu-route-skirmish";
            default:
                return "main-menu-route-root";
        }
    }

    private static MatchHudSquadTraySlot ToSquadTraySlot(int index)
    {
        return index switch
        {
            0 => MatchHudSquadTraySlot.Soldiers,
            1 => MatchHudSquadTraySlot.CombatVehicles,
            2 => MatchHudSquadTraySlot.AttackHelicopter,
            3 => MatchHudSquadTraySlot.Jet,
            4 => MatchHudSquadTraySlot.Transport,
            _ => MatchHudSquadTraySlot.None
        };
    }

    private static void SetClass(VisualElement target, string className, bool enabled)
    {
        if (target == null)
            return;

        if (enabled)
        {
            if (!target.ClassListContains(className))
                target.AddToClassList(className);
            return;
        }

        target.RemoveFromClassList(className);
    }

    private static void ApplyKnownClass(VisualElement target, string[] knownClasses, string className)
    {
        if (target == null)
            return;

        for (int i = 0; i < knownClasses.Length; i++)
            target.RemoveFromClassList(knownClasses[i]);

        if (!string.IsNullOrWhiteSpace(className))
            target.AddToClassList(className);
    }

    private static void ApplyObjectiveRow(
        Label label,
        VisualElement icon,
        UiMatchHudObjectiveRowModel objective)
    {
        if (label != null)
            label.text = objective.Text;

        string iconClass = objective.IconKind switch
        {
            UiMatchHudObjectiveIconKind.Checked => "objective-checked",
            UiMatchHudObjectiveIconKind.Star => "objective-star",
            _ => "objective-unchecked"
        };
        ApplyKnownClass(icon, ObjectiveIconClasses, iconClass);
    }

    private static void ApplyMinimapMarker(VisualElement marker, UiMatchHudMinimapMarkerModel model)
    {
        if (marker == null)
            return;

        SetShellHidden(marker, !model.Visible);
        marker.style.left = Length.Percent(Mathf.Clamp(model.LeftPercent, 0f, 100f));
        marker.style.top = Length.Percent(Mathf.Clamp(model.TopPercent, 0f, 100f));
    }

    private static void SetLabelText(Label label, string text, string fallback)
    {
        if (label == null)
            return;

        label.text = string.IsNullOrWhiteSpace(text)
            ? fallback
            : text;
    }

    private void ApplyBuildDrawerActiveProduction(UiBuildDrawerActiveProductionModel activeProduction)
    {
        SetShellHidden(buildDrawerActiveProductionRow, !activeProduction.Visible);
        if (!activeProduction.Visible)
            return;

        SetLabelText(buildDrawerActiveProductionNameLabel, activeProduction.Name, "PRODUCTION ITEM");
        SetLabelText(buildDrawerActiveProductionPercentLabel, activeProduction.PercentText, "0%");
        if (buildDrawerActiveProductionFill != null)
            buildDrawerActiveProductionFill.style.width = Length.Percent(Mathf.Clamp01(activeProduction.Progress01) * 100f);
        SetElementEnabled(buildDrawerActiveProductionCancelAction, activeProduction.CancelEnabled);
    }

    private void ApplyBuildDrawerCatalogItem(int index, bool visible, UiBuildDrawerCatalogItemModel item)
    {
        Button button = buildDrawerCatalogItems[index];
        SetShellHidden(button, !visible || !item.Visible);
        if (!visible || !item.Visible)
            return;

        SetElementEnabled(button, item.Enabled);
        SetLabelText(buildDrawerCatalogTitleLabels[index], item.Title, "STRUCTURE");
        SetLabelText(buildDrawerCatalogRoleLabels[index], item.Role, "BUILDING");
        SetLabelText(buildDrawerCatalogCreditsLabels[index], item.CreditsText, "0");
        SetLabelText(buildDrawerCatalogSuppliesLabels[index], item.SuppliesText, "0");
        SetLabelText(buildDrawerCatalogTimeLabels[index], item.TimeText, "00:00");
    }

    private void ApplyBuildDrawerQueueRow(int index, bool visible, UiBuildDrawerQueueRowModel row)
    {
        VisualElement rowElement = buildDrawerQueueRows[index];
        SetShellHidden(rowElement, !visible || !row.Visible);
        if (!visible || !row.Visible)
            return;

        SetLabelText(buildDrawerQueueNumberLabels[index], row.NumberText, (index + 1).ToString());
        SetLabelText(buildDrawerQueueNameLabels[index], row.Name, "PRODUCTION ITEM");
        SetLabelText(buildDrawerQueueTimeLabels[index], row.TimeText, "00:00");
        SetElementEnabled(buildDrawerQueueOrderActions[index], row.ActionEnabled);
    }

    private static string[] BuildPercentLabels()
    {
        string[] labels = new string[101];
        for (int i = 0; i < labels.Length; i++)
            labels[i] = i + "%";
        return labels;
    }

    private bool TryPickScreenPoint(Vector2 screenPosition, out VisualElement pickedElement)
    {
        pickedElement = null;
        if (root == null || root.panel == null)
            return false;

        Vector2 panelPosition = RuntimePanelUtils.ScreenToPanel(root.panel, screenPosition);
        pickedElement = root.panel.Pick(panelPosition);
        return pickedElement != null;
    }

    private bool IsShellStructuralElement(VisualElement element)
    {
        return ReferenceEquals(element, root)
            || ReferenceEquals(element, safeAreaRoot)
            || ReferenceEquals(element, contentRoot)
            || ReferenceEquals(element, menuBackgroundRegion)
            || ReferenceEquals(element, tooltipLayer)
            || ReferenceEquals(element, loadingScreenSlot)
            || ReferenceEquals(element, mainMenuScreenSlot)
            || ReferenceEquals(element, matchScreenSlot)
            || ReferenceEquals(element, armoryScreenSlot)
            || ReferenceEquals(element, commanderProfileScreenSlot)
            || ReferenceEquals(element, resultScreenSlot)
            || ReferenceEquals(element, popupScreenSlot);
    }

    private static bool IsSelfOrDescendantOf(VisualElement element, VisualElement ancestor)
    {
        if (element == null || ancestor == null)
            return false;

        for (VisualElement current = element; current != null; current = current.parent)
        {
            if (ReferenceEquals(current, ancestor))
                return true;
        }

        return false;
    }

    private static bool IsHiddenBySelfOrAncestor(VisualElement element)
    {
        for (VisualElement current = element; current != null; current = current.parent)
        {
            if (current.ClassListContains("shell-hidden"))
                return true;
        }

        return false;
    }

    private static string DescribeUiElement(VisualElement element, string fallback)
    {
        if (element == null)
            return fallback;

        for (VisualElement current = element; current != null; current = current.parent)
        {
            if (!string.IsNullOrWhiteSpace(current.name))
                return current.name;
        }

        return fallback;
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
        menuBackgroundRegion = root.Q<VisualElement>("MenuBackgroundRegion");
        loadingScreenSlot = root.Q<VisualElement>("LoadingScreenSlot");
        mainMenuScreenSlot = root.Q<VisualElement>("MainMenuScreenSlot");
        matchScreenSlot = root.Q<VisualElement>("MatchScreenSlot");
        armoryScreenSlot = root.Q<VisualElement>("ArmoryScreenSlot");
        commanderProfileScreenSlot = root.Q<VisualElement>("CommanderProfileScreenSlot");
        resultScreenSlot = root.Q<VisualElement>("ResultScreenSlot");
        popupScreenSlot = root.Q<VisualElement>("PopupScreenSlot");
    }

    private void BindLoadingScreen()
    {
        ClearLoadingBindings();
        if (loadingContentRoot == null)
            return;

        loadingBody = loadingContentRoot.Q<VisualElement>("LoadingBody");
        loadingBackdrop = loadingContentRoot.Q<VisualElement>("Background");
        loadingLogoLockup = loadingContentRoot.Q<VisualElement>("Brand_LogoLockup");
        loadingTitleLabel = loadingContentRoot.Q<Label>("CommandSystem_Text");
        loadingPanelFrame = loadingContentRoot.Q<VisualElement>("LoadingPanel_Frame");
        loadingStatusLabel = loadingContentRoot.Q<Label>("LoadingPanel_Status");
        loadingPercentLabel = loadingContentRoot.Q<Label>("LoadingPanel_Percent");
        loadingProgressFrame = loadingContentRoot.Q<VisualElement>("Progress_Frame");
        loadingProgressFill = loadingContentRoot.Q<VisualElement>("Progress_Fill");
        loadingSpinner = loadingContentRoot.Q<VisualElement>("BottomStatus_Spinner");
        loadingBottomStatusLabel = loadingContentRoot.Q<Label>("BottomStatus_Text");
    }

    private void BindBuildDrawerPopup()
    {
        ClearBuildDrawerBindings();
        if (buildDrawerPopupRoot == null)
            return;

        buildDrawerBuildPanel = buildDrawerPopupRoot.Q<VisualElement>("BuildPanel");
        buildDrawerProductionPanel = buildDrawerPopupRoot.Q<VisualElement>("ProductionPanel");
        buildDrawerBuildIcon = buildDrawerPopupRoot.Q<VisualElement>("BuildIcon");
        buildDrawerCatalogScrollView = buildDrawerPopupRoot.Q<ScrollView>("CatalogScrollView");
        buildDrawerProductionScrollView = buildDrawerPopupRoot.Q<ScrollView>("ProductionScrollView");
        buildDrawerBuildAction = buildDrawerPopupRoot.Q<Button>("BuildButton");
        buildDrawerRushAction = buildDrawerPopupRoot.Q<Button>("RushButton");
        buildDrawerClearAction = buildDrawerPopupRoot.Q<Button>("ClearButton");
        buildDrawerCloseAction = buildDrawerPopupRoot.Q<Button>("CloseButton");
        RegisterBuildDrawerCloseAction();
        RegisterBuildDrawerProductionAction(buildDrawerBuildAction, ref buildDrawerBuildActionCallback, UiActionKind.BuildDrawerPrimaryBuild);
        RegisterBuildDrawerProductionAction(buildDrawerRushAction, ref buildDrawerRushActionCallback, UiActionKind.BuildProductionRush);
        RegisterBuildDrawerProductionAction(buildDrawerClearAction, ref buildDrawerClearActionCallback, UiActionKind.BuildProductionClear);
        buildDrawerNameLabel = buildDrawerBuildPanel?.Q<Label>("Name");
        buildDrawerRoleLabel = buildDrawerBuildPanel?.Q<Label>("Role");
        buildDrawerPreview = buildDrawerBuildPanel?.Q<VisualElement>("Preview");
        buildDrawerDescriptionLabel = buildDrawerBuildPanel?.Q<Label>("Description");
        VisualElement footprintPanel = buildDrawerBuildPanel?.Q<VisualElement>("SizePanel");
        buildDrawerFootprintIcon = footprintPanel?.Q<VisualElement>("Icon");
        buildDrawerFootprintValueLabel = footprintPanel?.Q<Label>("Value");
        buildDrawerRequirementsValueLabel = buildDrawerBuildPanel?.Q<VisualElement>("RequirementsPanel")?.Q<Label>("Placement");
        VisualElement placementPanel = buildDrawerBuildPanel?.Q<VisualElement>("PlacementPanel");
        buildDrawerPlacementIcon = placementPanel?.Q<VisualElement>("Icon");
        buildDrawerPlacementValueLabel = placementPanel?.Q<Label>("Placement");
        VisualElement productionTimePanel = buildDrawerBuildPanel?.Q<VisualElement>("ProductionTimePanel");
        buildDrawerProductionTimeIcon = productionTimePanel?.Q<VisualElement>("Icon");
        buildDrawerProductionTimeValueLabel = productionTimePanel?.Q<Label>("Value");
        VisualElement creditsCostPanel = buildDrawerBuildPanel?.Q<VisualElement>("CreditsCost");
        buildDrawerCreditsCostIcon = creditsCostPanel?.Q<VisualElement>("Icon");
        buildDrawerCreditsCostValueLabel = creditsCostPanel?.Q<Label>("Value");
        VisualElement suppliesCostPanel = buildDrawerBuildPanel?.Q<VisualElement>("SuppliesCost");
        buildDrawerSuppliesCostIcon = suppliesCostPanel?.Q<VisualElement>("Icon");
        buildDrawerSuppliesCostValueLabel = suppliesCostPanel?.Q<Label>("Value");
        VisualElement instructionStrip = buildDrawerPopupRoot.Q<VisualElement>("InstructionStrip");
        buildDrawerInstructionCursorIcon = instructionStrip?.Q<VisualElement>("CursorIcon");
        buildDrawerInstructionInfoIcon = instructionStrip?.Q<VisualElement>("Icon");
        buildDrawerInstructionLabel = instructionStrip?.Q<Label>("Instruction");
        buildDrawerNoProductionLabel = buildDrawerProductionPanel?.Q<Label>("NoProduction");
        VisualElement productionActivePanel = buildDrawerProductionPanel?.Q<VisualElement>("ProductionPanelActive");
        buildDrawerProductionTitleLabel = productionActivePanel?.Q<Label>("Name");
        buildDrawerProductionCountLabel = productionActivePanel?.Q<Label>("Numbers");
        buildDrawerActiveProductionRow = productionActivePanel?.Q<VisualElement>("ProductionActiveItemView");
        buildDrawerActiveProductionImage = buildDrawerActiveProductionRow?.Q<VisualElement>("Image");
        buildDrawerActiveProductionNameLabel = buildDrawerActiveProductionRow?.Q<Label>("Name");
        buildDrawerActiveProductionPercentLabel = buildDrawerActiveProductionRow?.Q<Label>("PercentageCompleteText");
        buildDrawerActiveProductionFill = buildDrawerActiveProductionRow?.Q<VisualElement>("Fill");
        buildDrawerActiveProductionCancelAction = buildDrawerActiveProductionRow?.Q<Button>("CancelButton");
        RegisterBuildDrawerProductionAction(buildDrawerActiveProductionCancelAction, ref buildDrawerActiveProductionCancelActionCallback, UiActionKind.BuildProductionCancelActive);
        buildDrawerRushIcon = buildDrawerRushAction?.Q<VisualElement>("Icon");
        buildDrawerClearIcon = buildDrawerClearAction?.Q<VisualElement>("Icon");

        for (int i = 0; i < buildDrawerCatalogItems.Length; i++)
            CacheBuildDrawerCatalogItem(i, i == 0 ? "ItemView" : "ItemView_" + i);

        for (int i = 0; i < buildDrawerQueueRows.Length; i++)
            CacheBuildDrawerQueueRow(i, i == 0 ? "ProductionItemView" : "ProductionItemView_" + i);
    }

    private void CacheBuildDrawerCatalogItem(int index, string itemName)
    {
        if (index < 0 || index >= buildDrawerCatalogItems.Length)
            return;

        VisualElement itemScope = buildDrawerCatalogScrollView?.Q<VisualElement>(itemName);
        Button item = itemScope as Button ?? itemScope?.Q<Button>("ItemView");
        buildDrawerCatalogItems[index] = item;
        buildDrawerCatalogThumbs[index] = item?.Q<VisualElement>("Thumb");
        buildDrawerCatalogTitleLabels[index] = item?.Q<Label>("Title");
        buildDrawerCatalogRoleLabels[index] = item?.Q<Label>("Role");
        VisualElement credits = item?.Q<VisualElement>("CreditsTinyCost");
        VisualElement supplies = item?.Q<VisualElement>("SuppliesTinyCost");
        VisualElement time = item?.Q<VisualElement>("TimeTinyCost");
        buildDrawerCatalogCreditsIcons[index] = credits?.Q<VisualElement>("Icon");
        buildDrawerCatalogSuppliesIcons[index] = supplies?.Q<VisualElement>("Icon");
        buildDrawerCatalogTimeIcons[index] = time?.Q<VisualElement>("Icon");
        buildDrawerCatalogCreditsLabels[index] = credits?.Q<Label>("Value");
        buildDrawerCatalogSuppliesLabels[index] = supplies?.Q<Label>("Value");
        buildDrawerCatalogTimeLabels[index] = time?.Q<Label>("Value");
        RegisterBuildDrawerCatalogAction(index);
    }

    private void CacheBuildDrawerQueueRow(int index, string rowName)
    {
        if (index < 0 || index >= buildDrawerQueueRows.Length)
            return;

        VisualElement rowScope = buildDrawerProductionScrollView?.Q<VisualElement>(rowName);
        VisualElement row = rowScope?.name == "ProductionItemView"
            ? rowScope
            : rowScope?.Q<VisualElement>("ProductionItemView");
        buildDrawerQueueRows[index] = row;
        buildDrawerQueueImages[index] = row?.Q<VisualElement>("Image");
        buildDrawerQueueNumberLabels[index] = row?.Q<Label>("Number");
        buildDrawerQueueNameLabels[index] = row?.Q<Label>("Name");
        buildDrawerQueueTimeLabels[index] = row?.Q<Label>("TimeText");
        buildDrawerQueueOrderActions[index] = row?.Q<Button>("OrderButton");
        RegisterBuildDrawerQueueAction(index);
    }

    private void BindMainMenuScreen()
    {
        ClearMainMenuBindings();
        if (mainMenuContentRoot == null)
            return;

        mainMenuHeaderContent = mainMenuContentRoot.Q<VisualElement>("HeaderContent");
        mainMenuInboxAction = mainMenuContentRoot.Q<Button>("InboxButton");
        mainMenuSettingsAction = mainMenuContentRoot.Q<Button>("SettingsButton");
        mainMenuMenuAction = mainMenuContentRoot.Q<Button>("MenuButton");
        mainMenuNavCampaignAction = mainMenuContentRoot.Q<Button>("Nav_Campaign");
        mainMenuNavArmoryAction = mainMenuContentRoot.Q<Button>("Nav_Armory");
        mainMenuNavSupplyAction = mainMenuContentRoot.Q<Button>("Nav_Supply");
        mainMenuNavCommandAction = mainMenuContentRoot.Q<Button>("Nav_Command");
        mainMenuNavTechTreeAction = mainMenuContentRoot.Q<Button>("Nav_TechTree");
        mainMenuNavProfileAction = mainMenuContentRoot.Q<Button>("Nav_Profile");
        mainMenuCardCampaignAction = mainMenuContentRoot.Q<Button>("Card_Campaign");
        mainMenuCardSkirmishAction = mainMenuContentRoot.Q<Button>("Card_Skirmish");
        mainMenuCardOperationsAction = mainMenuContentRoot.Q<Button>("Card_Operations");
        mainMenuCommanderAction = mainMenuContentRoot.Q<Button>("CommanderPanel");
        mainMenuDeployAction = mainMenuContentRoot.Q<Button>("DeployOperationButton");
        VisualElement creditsPanel = mainMenuContentRoot.Q<VisualElement>("CreditsPanel");
        VisualElement suppliesPanel = mainMenuContentRoot.Q<VisualElement>("SuppliesPanel");
        VisualElement commandPanel = mainMenuContentRoot.Q<VisualElement>("CommandPanel");
        mainMenuCreditsValueLabel = creditsPanel?.Q<Label>("Value");
        mainMenuSuppliesValueLabel = suppliesPanel?.Q<Label>("Value");
        mainMenuCommandValueLabel = commandPanel?.Q<Label>("Value");
        mainMenuCommanderPortrait = mainMenuContentRoot.Q<VisualElement>("Portrait");
        mainMenuCommanderNameLabel = mainMenuContentRoot.Q<Label>("Name");
        mainMenuCommanderSubtitleLabel = mainMenuContentRoot.Q<Label>("Level");

        RegisterMainMenuCallbacks();
        ApplyMainMenuResources(new UiShellMainMenuResourcesModel(
            DefaultCreditsText,
            DefaultSuppliesText,
            DefaultCommandText));
        ApplyMainMenuCommanderProfile(new UiShellCommanderProfileModel(
            DefaultCommanderName,
            DefaultCommanderSubtitle,
            DefaultCommanderPortraitClass));
        ApplyMainMenuRouteState(UIRoute.MainMenu);
    }

    private void RegisterMainMenuCallbacks()
    {
        RegisterClick(mainMenuInboxAction, OnMainMenuInboxClick);
        RegisterClick(mainMenuSettingsAction, OnMainMenuSettingsClick);
        RegisterClick(mainMenuMenuAction, OnMainMenuRootClick);
        RegisterClick(mainMenuNavCampaignAction, OnMainMenuRootClick);
        RegisterClick(mainMenuNavArmoryAction, OnMainMenuArmoryClick);
        RegisterClick(mainMenuNavSupplyAction, OnMainMenuSupplyClick);
        RegisterClick(mainMenuNavCommandAction, OnMainMenuCommandClick);
        RegisterClick(mainMenuNavTechTreeAction, OnMainMenuTechTreeClick);
        RegisterClick(mainMenuNavProfileAction, OnMainMenuProfileClick);
        RegisterClick(mainMenuCardCampaignAction, OnMainMenuRootClick);
        RegisterClick(mainMenuCardSkirmishAction, OnMainMenuSkirmishClick);
        RegisterClick(mainMenuCardOperationsAction, OnMainMenuCommandClick);
        RegisterClick(mainMenuCommanderAction, OnMainMenuProfileClick);
        RegisterClick(mainMenuDeployAction, OnMainMenuDeployClick);
    }

    private void BindMatchHudScreen()
    {
        ClearMatchHudBindings();
        if (matchHudContentRoot == null)
            return;

        VisualElement currentOrderBanner = matchHudContentRoot.Q<VisualElement>("CurrentOrderBanner");
        matchHudOrderTextLabel = currentOrderBanner?.Q<Label>("OrderText");
        matchHudSquadTextLabel = currentOrderBanner?.Q<Label>("SquadText");
        matchHudCreditsValueLabel = matchHudContentRoot.Q<VisualElement>("CreditsSlot")?.Q<Label>("Value");
        matchHudFuelValueLabel = matchHudContentRoot.Q<VisualElement>("FuelSlot")?.Q<Label>("Value");
        matchHudSupplyValueLabel = matchHudContentRoot.Q<VisualElement>("SupplySlot")?.Q<Label>("Value");
        matchHudCivilianRiskValueLabel = matchHudContentRoot.Q<VisualElement>("CivilianRiskSlot")?.Q<Label>("Value");
        matchHudObjectivesPanel = matchHudContentRoot.Q<VisualElement>("ObjectivesPanel");
        matchHudObjectivesTitleLabel = matchHudObjectivesPanel?.Q<Label>("Title");
        matchHudObjective0Label = matchHudObjectivesPanel?.Q<VisualElement>("NeutralizeHostiles")?.Q<Label>("Text");
        matchHudObjective1Label = matchHudObjectivesPanel?.Q<VisualElement>("ProtectCivilians")?.Q<Label>("Text");
        matchHudObjective2Label = matchHudObjectivesPanel?.Q<VisualElement>("KeepLossesLow")?.Q<Label>("Text");
        matchHudObjective0Icon = matchHudObjectivesPanel?.Q<VisualElement>("NeutralizeHostiles")?.Q<VisualElement>("Icon");
        matchHudObjective1Icon = matchHudObjectivesPanel?.Q<VisualElement>("ProtectCivilians")?.Q<VisualElement>("Icon");
        matchHudObjective2Icon = matchHudObjectivesPanel?.Q<VisualElement>("KeepLossesLow")?.Q<VisualElement>("Icon");
        matchHudObjectivesElapsedLabel = matchHudObjectivesPanel?.Q<Label>("Elapsed");
        matchHudThreatJumpPanel = matchHudContentRoot.Q<VisualElement>("ThreatJumpPanel");
        matchHudThreatTitleLabel = matchHudThreatJumpPanel?.Q<Label>("Title");
        matchHudThreatSubtitleLabel = matchHudThreatJumpPanel?.Q<Label>("Subtitle");
        matchHudThreatJumpAction = matchHudThreatJumpPanel?.Q<Button>("JumpButton");
        matchHudFeedbackPanel = matchHudContentRoot.Q<VisualElement>("FeedbackPanel");
        matchHudFeedbackTextLabel = matchHudFeedbackPanel?.Q<Label>("Feedback");
        matchHudFeedbackActions = matchHudFeedbackPanel?.Q<VisualElement>("Actions");
        matchHudFeedbackBoardAllAction = matchHudFeedbackPanel?.Q<Button>("BoardAllButton");
        matchHudFeedbackCancelAction = matchHudFeedbackPanel?.Q<Button>("CancelButton");
        matchHudMinimapPanel = matchHudContentRoot.Q<VisualElement>("MinimapPanel");
        matchHudMinimapViewport = matchHudMinimapPanel?.Q<VisualElement>("Viewport");
        matchHudMinimapFriendlyA = matchHudMinimapPanel?.Q<VisualElement>("FriendlyA");
        matchHudMinimapFriendlyB = matchHudMinimapPanel?.Q<VisualElement>("FriendlyB");
        matchHudMinimapHostileA = matchHudMinimapPanel?.Q<VisualElement>("HostileA");
        matchHudMinimapCivilian = matchHudMinimapPanel?.Q<VisualElement>("Civilian");
        matchHudMinimapZoomInAction = matchHudMinimapPanel?.Q<Button>("ZoomIn");
        matchHudMinimapZoomOutAction = matchHudMinimapPanel?.Q<Button>("ZoomOut");
        matchHudMinimapFocusAction = matchHudMinimapPanel?.Q<Button>("ZoomFocus");
        matchHudSelectedPanel = matchHudContentRoot.Q<VisualElement>("SelectedSquadPanel");
        matchHudSelectedBadge = matchHudSelectedPanel?.Q<VisualElement>("Badge");
        matchHudSelectedTitleLabel = matchHudSelectedPanel?.Q<Label>("Title");
        matchHudSelectedSubtitleLabel = matchHudSelectedPanel?.Q<Label>("Subtitle");
        matchHudSelectedHealthFill = matchHudSelectedPanel?.Q<VisualElement>("HealthFill");
        matchHudSelectedHealthTextLabel = matchHudSelectedPanel?.Q<Label>("HealthText");
        matchHudSelectedOrderValueLabel = matchHudSelectedPanel?.Q<Label>("OrderValue");
        matchHudSelectedReturnAction = matchHudSelectedPanel?.Q<Button>("ReturnButton");
        matchHudSelectedDestroyAction = matchHudSelectedPanel?.Q<Button>("DestroyButton");
        matchHudSelectedBoardAction = matchHudSelectedPanel?.Q<Button>("BoardButton");
        matchHudPassengerChip = matchHudContentRoot.Q<Button>("PassengerChip");
        matchHudPassengerDrawer = matchHudContentRoot.Q<VisualElement>("TransportPassengerDrawer");
        matchHudPassengerChipLabel = matchHudPassengerChip?.Q<Label>("Label");
        matchHudPassengerDrawerHeaderLabel = matchHudPassengerDrawer?.Q<Label>("Header");
        matchHudPassengerEmptyState = matchHudPassengerDrawer?.Q<VisualElement>("EmptyState");
        CacheMatchHudPassengerRow(0, "PassengerItemView");
        CacheMatchHudPassengerRow(1, "PassengerItemView_02");
        CacheMatchHudPassengerRow(2, "PassengerItemView_03");
        CacheMatchHudSquadCard(0, "SquadCard1");
        CacheMatchHudSquadCard(1, "SquadCard2");
        CacheMatchHudSquadCard(2, "SquadCard3");
        CacheMatchHudSquadCard(3, "SquadCard4");
        CacheMatchHudSquadCard(4, "SquadCard5");
        matchHudSelectCommand = matchHudContentRoot.Q<Button>("SelectCommand");
        matchHudMoveCommand = matchHudContentRoot.Q<Button>("MoveCommand");
        matchHudAttackCommand = matchHudContentRoot.Q<Button>("AttackCommand");
        matchHudHoldCommand = matchHudContentRoot.Q<Button>("HoldCommand");
        matchHudStopCommand = matchHudContentRoot.Q<Button>("StopCommand");
        matchHudBuildCommand = matchHudContentRoot.Q<Button>("BuildCommand");
        matchHudScanCommand = matchHudContentRoot.Q<Button>("ScanCommand");
        matchHudSupportCommand = matchHudContentRoot.Q<Button>("SupportCommand");
        matchHudRightBuildCommand = matchHudContentRoot.Q<Button>("RightBuildCommand");
        matchHudRightSupportCommand = matchHudContentRoot.Q<Button>("RightSupportCommand");

        RegisterMatchHudAction("MenuButton", UiActionKind.MatchMenu);
        RegisterMatchHudAction("ReturnButton", UiActionKind.ReturnSelection);
        RegisterMatchHudAction("DestroyButton", UiActionKind.DestroySelection);
        RegisterMatchHudAction("BoardButton", UiActionKind.BoardSelection);
        RegisterMatchHudAction("PassengerChip", UiActionKind.TogglePassengerDrawer);
        RegisterMatchHudAction("ExitAllButton", UiActionKind.ExitAllPassengers);
        RegisterMatchHudAction("CloseButton", UiActionKind.ClosePassengerDrawer);
        RegisterMatchHudAction("JumpButton", UiActionKind.JumpToThreat);
        RegisterMatchHudAction("PauseButton", UiActionKind.Pause);
        RegisterMatchHudAction("SettingsButton", UiActionKind.OpenSettings);
        RegisterMatchHudAction("RightBuildCommand", UiActionKind.RightBuild);
        RegisterMatchHudAction("RightSupportCommand", UiActionKind.RightSupport);
        RegisterMatchHudAction("SquadCard1", UiActionKind.SquadSlot1, 1);
        RegisterMatchHudAction("SquadCard2", UiActionKind.SquadSlot2, 2);
        RegisterMatchHudAction("SquadCard3", UiActionKind.SquadSlot3, 3);
        RegisterMatchHudAction("SquadCard4", UiActionKind.SquadSlot4, 4);
        RegisterMatchHudAction("SquadCard5", UiActionKind.SquadSlot5, 5);
        RegisterMatchHudAction("SelectCommand", UiActionKind.Select);
        RegisterMatchHudAction("MoveCommand", UiActionKind.Move);
        RegisterMatchHudAction("AttackCommand", UiActionKind.Attack);
        RegisterMatchHudAction("HoldCommand", UiActionKind.Hold);
        RegisterMatchHudAction("StopCommand", UiActionKind.Stop);
        RegisterMatchHudAction("BuildCommand", UiActionKind.Build);
        RegisterMatchHudAction("ScanCommand", UiActionKind.Scan);
        RegisterMatchHudAction("SupportCommand", UiActionKind.Support);
        RegisterMatchHudAction("ZoomIn", UiActionKind.MinimapZoomIn);
        RegisterMatchHudAction("ZoomOut", UiActionKind.MinimapZoomOut);
        RegisterMatchHudAction("ZoomFocus", UiActionKind.MinimapFocus);
        RegisterMatchHudAction("BoardAllButton", UiActionKind.BoardAll);
        RegisterMatchHudAction("CancelButton", UiActionKind.CancelFeedback);
        ApplyMatchHudSelection(UiMatchHudSelectionPanelModel.Hidden);
        ApplyMatchHudCommandState(default);
        ApplyMatchHudHeader(UiMatchHudHeaderModel.Default);
        ApplyMatchHudStatusSurfaces(UiMatchHudStatusSurfacesModel.Default);
        ApplyMatchHudMinimap(UiMatchHudMinimapModel.Default);
        ApplyMatchHudPassengerDrawer(UiMatchHudPassengerDrawerModel.Hidden);
        ApplyMatchHudSquadTray(UiMatchHudSquadTrayModel.Default);
    }

    private void CacheMatchHudPassengerRow(int index, string rowName)
    {
        if (index < 0 || index >= matchHudPassengerRows.Length)
            return;

        VisualElement row = matchHudPassengerDrawer?.Q<VisualElement>(rowName);
        matchHudPassengerRows[index] = row;
        matchHudPassengerNameLabels[index] = row?.Q<Label>("Name");
        matchHudPassengerRoleLabels[index] = row?.Q<Label>("Role");
        matchHudPassengerHealthFills[index] = row?.Q<VisualElement>("HealthFill");
        matchHudPassengerHealthLabels[index] = row?.Q<Label>("Health");
    }

    private void CacheMatchHudSquadCard(int index, string cardName)
    {
        if (index < 0 || index >= matchHudSquadCards.Length)
            return;

        Button card = matchHudContentRoot?.Q<Button>(cardName);
        matchHudSquadCards[index] = card;
        matchHudSquadTitleLabels[index] = card?.Q<Label>("Title");
        matchHudSquadHealthFills[index] = card?.Q<VisualElement>("HealthFill");
        matchHudSquadHealthLabels[index] = card?.Q<Label>("HealthText");
    }

    private void ClearMainMenuBindings()
    {
        UnregisterClick(mainMenuInboxAction, OnMainMenuInboxClick);
        UnregisterClick(mainMenuSettingsAction, OnMainMenuSettingsClick);
        UnregisterClick(mainMenuMenuAction, OnMainMenuRootClick);
        UnregisterClick(mainMenuNavCampaignAction, OnMainMenuRootClick);
        UnregisterClick(mainMenuNavArmoryAction, OnMainMenuArmoryClick);
        UnregisterClick(mainMenuNavSupplyAction, OnMainMenuSupplyClick);
        UnregisterClick(mainMenuNavCommandAction, OnMainMenuCommandClick);
        UnregisterClick(mainMenuNavTechTreeAction, OnMainMenuTechTreeClick);
        UnregisterClick(mainMenuNavProfileAction, OnMainMenuProfileClick);
        UnregisterClick(mainMenuCardCampaignAction, OnMainMenuRootClick);
        UnregisterClick(mainMenuCardSkirmishAction, OnMainMenuSkirmishClick);
        UnregisterClick(mainMenuCardOperationsAction, OnMainMenuCommandClick);
        UnregisterClick(mainMenuCommanderAction, OnMainMenuProfileClick);
        UnregisterClick(mainMenuDeployAction, OnMainMenuDeployClick);

        mainMenuHeaderContent = null;
        mainMenuInboxAction = null;
        mainMenuSettingsAction = null;
        mainMenuMenuAction = null;
        mainMenuNavCampaignAction = null;
        mainMenuNavArmoryAction = null;
        mainMenuNavSupplyAction = null;
        mainMenuNavCommandAction = null;
        mainMenuNavTechTreeAction = null;
        mainMenuNavProfileAction = null;
        mainMenuCardCampaignAction = null;
        mainMenuCardSkirmishAction = null;
        mainMenuCardOperationsAction = null;
        mainMenuCommanderAction = null;
        mainMenuDeployAction = null;
        mainMenuCreditsValueLabel = null;
        mainMenuSuppliesValueLabel = null;
        mainMenuCommandValueLabel = null;
        mainMenuCommanderPortrait = null;
        mainMenuCommanderNameLabel = null;
        mainMenuCommanderSubtitleLabel = null;
    }

    private void ClearMatchHudBindings()
    {
        foreach (KeyValuePair<Button, EventCallback<ClickEvent>> callback in matchHudActionCallbacks)
            callback.Key?.UnregisterCallback(callback.Value);

        matchHudActionCallbacks.Clear();
        matchHudSelectedPanel = null;
        matchHudSelectedBadge = null;
        matchHudSelectedTitleLabel = null;
        matchHudSelectedSubtitleLabel = null;
        matchHudSelectedHealthFill = null;
        matchHudSelectedHealthTextLabel = null;
        matchHudSelectedOrderValueLabel = null;
        matchHudOrderTextLabel = null;
        matchHudSquadTextLabel = null;
        matchHudCreditsValueLabel = null;
        matchHudFuelValueLabel = null;
        matchHudSupplyValueLabel = null;
        matchHudCivilianRiskValueLabel = null;
        matchHudObjectivesPanel = null;
        matchHudObjectivesTitleLabel = null;
        matchHudObjective0Label = null;
        matchHudObjective1Label = null;
        matchHudObjective2Label = null;
        matchHudObjective0Icon = null;
        matchHudObjective1Icon = null;
        matchHudObjective2Icon = null;
        matchHudObjectivesElapsedLabel = null;
        matchHudThreatJumpPanel = null;
        matchHudThreatTitleLabel = null;
        matchHudThreatSubtitleLabel = null;
        matchHudThreatJumpAction = null;
        matchHudFeedbackPanel = null;
        matchHudFeedbackTextLabel = null;
        matchHudFeedbackActions = null;
        matchHudFeedbackBoardAllAction = null;
        matchHudFeedbackCancelAction = null;
        matchHudMinimapPanel = null;
        matchHudMinimapViewport = null;
        matchHudMinimapFriendlyA = null;
        matchHudMinimapFriendlyB = null;
        matchHudMinimapHostileA = null;
        matchHudMinimapCivilian = null;
        matchHudMinimapZoomInAction = null;
        matchHudMinimapZoomOutAction = null;
        matchHudMinimapFocusAction = null;
        matchHudSelectedReturnAction = null;
        matchHudSelectedDestroyAction = null;
        matchHudSelectedBoardAction = null;
        matchHudPassengerChip = null;
        matchHudPassengerDrawer = null;
        matchHudPassengerChipLabel = null;
        matchHudPassengerDrawerHeaderLabel = null;
        matchHudPassengerEmptyState = null;
        for (int i = 0; i < matchHudPassengerRows.Length; i++)
        {
            matchHudPassengerRows[i] = null;
            matchHudPassengerNameLabels[i] = null;
            matchHudPassengerRoleLabels[i] = null;
            matchHudPassengerHealthFills[i] = null;
            matchHudPassengerHealthLabels[i] = null;
        }
        for (int i = 0; i < matchHudSquadCards.Length; i++)
        {
            matchHudSquadCards[i] = null;
            matchHudSquadTitleLabels[i] = null;
            matchHudSquadHealthFills[i] = null;
            matchHudSquadHealthLabels[i] = null;
        }
        matchHudSelectCommand = null;
        matchHudMoveCommand = null;
        matchHudAttackCommand = null;
        matchHudHoldCommand = null;
        matchHudStopCommand = null;
        matchHudBuildCommand = null;
        matchHudScanCommand = null;
        matchHudSupportCommand = null;
        matchHudRightBuildCommand = null;
        matchHudRightSupportCommand = null;
    }

    private void ClearBuildDrawerBindings()
    {
        UnregisterClick(buildDrawerCloseAction, buildDrawerCloseActionCallback);
        UnregisterClick(buildDrawerBuildAction, buildDrawerBuildActionCallback);
        UnregisterClick(buildDrawerRushAction, buildDrawerRushActionCallback);
        UnregisterClick(buildDrawerClearAction, buildDrawerClearActionCallback);
        UnregisterClick(buildDrawerActiveProductionCancelAction, buildDrawerActiveProductionCancelActionCallback);
        buildDrawerBuildActionCallback = null;
        buildDrawerCloseActionCallback = null;
        buildDrawerRushActionCallback = null;
        buildDrawerClearActionCallback = null;
        buildDrawerActiveProductionCancelActionCallback = null;
        buildDrawerBuildAction = null;
        buildDrawerRushAction = null;
        buildDrawerClearAction = null;
        buildDrawerCloseAction = null;
        buildDrawerCatalogScrollView = null;
        buildDrawerProductionScrollView = null;
        buildDrawerBuildPanel = null;
        buildDrawerProductionPanel = null;
        buildDrawerBuildIcon = null;
        buildDrawerPreview = null;
        buildDrawerFootprintIcon = null;
        buildDrawerPlacementIcon = null;
        buildDrawerProductionTimeIcon = null;
        buildDrawerCreditsCostIcon = null;
        buildDrawerSuppliesCostIcon = null;
        buildDrawerInstructionCursorIcon = null;
        buildDrawerInstructionInfoIcon = null;
        buildDrawerRushIcon = null;
        buildDrawerClearIcon = null;
        buildDrawerNameLabel = null;
        buildDrawerRoleLabel = null;
        buildDrawerDescriptionLabel = null;
        buildDrawerFootprintValueLabel = null;
        buildDrawerRequirementsValueLabel = null;
        buildDrawerPlacementValueLabel = null;
        buildDrawerProductionTimeValueLabel = null;
        buildDrawerCreditsCostValueLabel = null;
        buildDrawerSuppliesCostValueLabel = null;
        buildDrawerInstructionLabel = null;
        buildDrawerNoProductionLabel = null;
        buildDrawerProductionTitleLabel = null;
        buildDrawerProductionCountLabel = null;
        buildDrawerActiveProductionRow = null;
        buildDrawerActiveProductionImage = null;
        buildDrawerActiveProductionNameLabel = null;
        buildDrawerActiveProductionPercentLabel = null;
        buildDrawerActiveProductionFill = null;
        buildDrawerActiveProductionCancelAction = null;
        for (int i = 0; i < buildDrawerCatalogItems.Length; i++)
        {
            UnregisterClick(buildDrawerCatalogItems[i], buildDrawerCatalogActionCallbacks[i]);
            buildDrawerCatalogActionCallbacks[i] = null;
            buildDrawerCatalogItems[i] = null;
            buildDrawerCatalogThumbs[i] = null;
            buildDrawerCatalogTitleLabels[i] = null;
            buildDrawerCatalogRoleLabels[i] = null;
            buildDrawerCatalogCreditsIcons[i] = null;
            buildDrawerCatalogSuppliesIcons[i] = null;
            buildDrawerCatalogTimeIcons[i] = null;
            buildDrawerCatalogCreditsLabels[i] = null;
            buildDrawerCatalogSuppliesLabels[i] = null;
            buildDrawerCatalogTimeLabels[i] = null;
        }
        for (int i = 0; i < buildDrawerQueueRows.Length; i++)
        {
            UnregisterClick(buildDrawerQueueOrderActions[i], buildDrawerQueueActionCallbacks[i]);
            buildDrawerQueueActionCallbacks[i] = null;
            buildDrawerQueueRows[i] = null;
            buildDrawerQueueImages[i] = null;
            buildDrawerQueueNumberLabels[i] = null;
            buildDrawerQueueNameLabels[i] = null;
            buildDrawerQueueTimeLabels[i] = null;
            buildDrawerQueueOrderActions[i] = null;
        }
    }

    private void ClearLoadingBindings()
    {
        loadingBody = null;
        loadingBackdrop = null;
        loadingLogoLockup = null;
        loadingTitleLabel = null;
        loadingPanelFrame = null;
        loadingStatusLabel = null;
        loadingPercentLabel = null;
        loadingProgressFrame = null;
        loadingProgressFill = null;
        loadingSpinner = null;
        loadingBottomStatusLabel = null;
    }

    private static void RegisterClick(Button target, EventCallback<ClickEvent> callback)
    {
        target?.RegisterCallback(callback);
    }

    private static void UnregisterClick(Button target, EventCallback<ClickEvent> callback)
    {
        target?.UnregisterCallback(callback);
    }

    private void RegisterMatchHudAction(string elementName, UiActionKind kind, int payloadId = 0)
    {
        Button target = matchHudContentRoot?.Q<Button>(elementName);
        if (target == null)
            return;

        EventCallback<ClickEvent> callback = evt =>
        {
            TrySubmitMatchHudAction(kind, payloadId);
            evt?.StopPropagation();
        };
        target.RegisterCallback(callback);
        matchHudActionCallbacks[target] = callback;
    }

    private void RegisterBuildDrawerCloseAction()
    {
        if (buildDrawerCloseAction == null)
            return;

        buildDrawerCloseActionCallback = evt =>
        {
            TrySubmitMatchHudAction(UiActionKind.CloseBuildDrawer);
            evt?.StopPropagation();
        };
        RegisterClick(buildDrawerCloseAction, buildDrawerCloseActionCallback);
    }

    private void RegisterBuildDrawerCatalogAction(int index)
    {
        if (index < 0 || index >= buildDrawerCatalogItems.Length)
            return;

        Button target = buildDrawerCatalogItems[index];
        if (target == null)
            return;

        int payloadId = index;
        buildDrawerCatalogActionCallbacks[index] = evt =>
        {
            TrySubmitMatchHudAction(UiActionKind.BuildCatalogItem, payloadId);
            evt?.StopPropagation();
        };
        RegisterClick(target, buildDrawerCatalogActionCallbacks[index]);
    }

    private void RegisterBuildDrawerProductionAction(
        Button target,
        ref EventCallback<ClickEvent> callback,
        UiActionKind kind,
        int payloadId = 0)
    {
        if (target == null)
            return;

        callback = evt =>
        {
            TrySubmitMatchHudAction(kind, payloadId);
            evt?.StopPropagation();
        };
        RegisterClick(target, callback);
    }

    private void RegisterBuildDrawerQueueAction(int index)
    {
        if (index < 0 || index >= buildDrawerQueueOrderActions.Length)
            return;

        Button target = buildDrawerQueueOrderActions[index];
        if (target == null)
            return;

        int payloadId = index;
        buildDrawerQueueActionCallbacks[index] = evt =>
        {
            TrySubmitMatchHudAction(UiActionKind.BuildProductionCancelQueued, payloadId);
            evt?.StopPropagation();
        };
        RegisterClick(target, buildDrawerQueueActionCallbacks[index]);
    }

    private static bool EnqueueMainMenuRoute(UiShellRouteIntent intent, UIRoute route, bool pushHistory)
    {
        return UiShellRuntimeGateway.TryEnqueueRouteRequest(intent, route, pushHistory);
    }

    private void OnMainMenuDeployClick(ClickEvent evt)
    {
        TrySubmitMainMenuAction("DeployOperationButton");
        evt?.StopPropagation();
    }

    private void OnMainMenuRootClick(ClickEvent evt)
    {
        TrySubmitMainMenuAction(evt?.currentTarget is VisualElement element ? element.name : "MenuButton");
        evt?.StopPropagation();
    }

    private void OnMainMenuArmoryClick(ClickEvent evt)
    {
        TrySubmitMainMenuAction("Nav_Armory");
        evt?.StopPropagation();
    }

    private void OnMainMenuSupplyClick(ClickEvent evt)
    {
        TrySubmitMainMenuAction("Nav_Supply");
        evt?.StopPropagation();
    }

    private void OnMainMenuCommandClick(ClickEvent evt)
    {
        TrySubmitMainMenuAction(evt?.currentTarget is VisualElement element ? element.name : "Nav_Command");
        evt?.StopPropagation();
    }

    private void OnMainMenuTechTreeClick(ClickEvent evt)
    {
        TrySubmitMainMenuAction("Nav_TechTree");
        evt?.StopPropagation();
    }

    private void OnMainMenuProfileClick(ClickEvent evt)
    {
        TrySubmitMainMenuAction(evt?.currentTarget is VisualElement element ? element.name : "Nav_Profile");
        evt?.StopPropagation();
    }

    private void OnMainMenuSkirmishClick(ClickEvent evt)
    {
        TrySubmitMainMenuAction("Card_Skirmish");
        evt?.StopPropagation();
    }

    private void OnMainMenuInboxClick(ClickEvent evt)
    {
        TrySubmitMainMenuAction("InboxButton");
        evt?.StopPropagation();
    }

    private void OnMainMenuSettingsClick(ClickEvent evt)
    {
        TrySubmitMainMenuAction("SettingsButton");
        evt?.StopPropagation();
    }

    private void ResetLoadingPresentationCache()
    {
        lastLoadingPercent = -1;
        hasLastLoadingStatus = false;
        lastLoadingStatus = default;
    }
}
