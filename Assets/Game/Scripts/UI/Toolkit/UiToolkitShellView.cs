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
    private const int ArmoryCategoryCount = 5;
    private const int ArmoryItemCount = 8;

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
    [SerializeField] private VisualTreeAsset armoryScreenAsset;
    [SerializeField] private VisualTreeAsset commanderProfileScreenAsset;
    [SerializeField] private VisualTreeAsset buildDrawerPopupAsset;
    [SerializeField] private VisualTreeAsset missionResultPopupAsset;
    [SerializeField] private VisualTreeAsset settingsPopupAsset;
    [SerializeField] private VisualTreeAsset inboxPopupAsset;
    [SerializeField] private VisualTreeAsset buildPlacementConfirmationBarAsset;

    private VisualElement root;
    private VisualElement safeAreaRoot;
    private VisualElement headerBar;
    private VisualElement contentRoot;
    private VisualElement footerBar;
    private VisualElement modalOverlay;
    private VisualElement tooltipLayer;
    private VisualElement diagnosticsOverlay;
    private VisualElement diagnosticsLogPanel;
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
    private TemplateContainer armoryScreenContainer;
    private TemplateContainer commanderProfileScreenContainer;
    private TemplateContainer buildDrawerPopupContainer;
    private TemplateContainer missionResultPopupContainer;
    private TemplateContainer settingsPopupContainer;
    private TemplateContainer inboxPopupContainer;
    private TemplateContainer buildPlacementConfirmationBarContainer;
    private VisualElement loadingContentRoot;
    private VisualElement mainMenuContentRoot;
    private VisualElement matchHudContentRoot;
    private VisualElement armoryContentRoot;
    private VisualElement commanderProfileContentRoot;
    private VisualElement buildDrawerPopupRoot;
    private VisualElement missionResultPopupRoot;
    private VisualElement settingsPopupRoot;
    private VisualElement inboxPopupRoot;
    private VisualElement buildPlacementConfirmationBarRoot;
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
    private VisualElement matchHudMinimapMap;
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
    private EventCallback<ClickEvent> buildPlacementConfirmActionCallback;
    private EventCallback<ClickEvent> buildPlacementCancelActionCallback;
    private EventCallback<ClickEvent> buildPlacementRotateActionCallback;
    private readonly EventCallback<ClickEvent>[] buildDrawerCatalogActionCallbacks = new EventCallback<ClickEvent>[BuildDrawerCatalogItemCount];
    private readonly EventCallback<ClickEvent>[] buildDrawerQueueActionCallbacks = new EventCallback<ClickEvent>[BuildDrawerQueueItemCount];
    private readonly EventCallback<ClickEvent>[] armoryCategoryCallbacks = new EventCallback<ClickEvent>[ArmoryCategoryCount];
    private readonly EventCallback<ClickEvent>[] armoryItemCallbacks = new EventCallback<ClickEvent>[ArmoryItemCount];
    private readonly Button[] armoryCategoryActions = new Button[ArmoryCategoryCount];
    private readonly Button[] armoryItems = new Button[ArmoryItemCount];
    private readonly Label[] armoryItemTitleLabels = new Label[ArmoryItemCount];
    private readonly Label[] armoryItemStateLabels = new Label[ArmoryItemCount];
    private readonly Label[] armoryItemLevelLabels = new Label[ArmoryItemCount];
    private readonly Label[] armoryItemTypeLabels = new Label[ArmoryItemCount];
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
    private Label buildPlacementTitleLabel;
    private Label buildPlacementStatusLabel;
    private Label buildPlacementCostLabel;
    private Label buildPlacementDurationLabel;
    private Label buildPlacementInstructionLabel;
    private Button buildPlacementCancelAction;
    private Button buildPlacementRotateAction;
    private Button buildPlacementConfirmAction;
    private ScrollView armoryScrollView;
    private VisualElement armoryCatalogContent;
    private VisualElement armoryInspectionPanel;
    private Label armoryInspectionNameLabel;
    private Label armoryInspectionTypeLabel;
    private VisualElement armoryInspectionPortraitArt;
    private Button armoryFilterAction;
    private Button armorySortAction;
    private Button armoryUpgradeAction;
    private Button armoryEquipAction;
    private Button armoryCloseAction;
    private Button armoryTabAction;
    private Button armoryWorkshopTabAction;
    private Button armoryDoctrineTabAction;
    private Button armoryDepotTabAction;
    private Button armoryOfficersTabAction;
    private EventCallback<ClickEvent> armoryFilterCallback;
    private EventCallback<ClickEvent> armorySortCallback;
    private EventCallback<ClickEvent> armoryUpgradeCallback;
    private EventCallback<ClickEvent> armoryEquipCallback;
    private EventCallback<ClickEvent> armoryCloseCallback;
    private EventCallback<ClickEvent> armoryTabCallback;
    private EventCallback<ClickEvent> armoryWorkshopTabCallback;
    private EventCallback<ClickEvent> armoryDoctrineTabCallback;
    private EventCallback<ClickEvent> armoryDepotTabCallback;
    private EventCallback<ClickEvent> armoryOfficersTabCallback;
    private int selectedArmoryItemIndex;
    private Button commanderProfileBackAction;
    private Button commanderProfileOverviewTabAction;
    private Button commanderProfileStatsTabAction;
    private Button commanderProfileBadgesTabAction;
    private Button commanderProfileHistoryTabAction;
    private Button commanderProfileUpgradesTabAction;
    private Button commanderProfileOpenArmoryAction;
    private Button commanderProfileDetailAction;
    private Button commanderProfileReplayAction;
    private VisualElement commanderProfilePortrait;
    private VisualElement commanderProfileBadge;
    private Label commanderProfileTitleLabel;
    private Label commanderProfileNameLabel;
    private Label commanderProfileSubtitleLabel;
    private Label commanderProfileLevelLabel;
    private EventCallback<ClickEvent> commanderProfileBackCallback;
    private EventCallback<ClickEvent> commanderProfileOverviewTabCallback;
    private EventCallback<ClickEvent> commanderProfileStatsTabCallback;
    private EventCallback<ClickEvent> commanderProfileBadgesTabCallback;
    private EventCallback<ClickEvent> commanderProfileHistoryTabCallback;
    private EventCallback<ClickEvent> commanderProfileUpgradesTabCallback;
    private EventCallback<ClickEvent> commanderProfileOpenArmoryCallback;
    private EventCallback<ClickEvent> commanderProfileDetailCallback;
    private EventCallback<ClickEvent> commanderProfileReplayCallback;
    private Label missionResultTitleLabel;
    private Label missionResultSubtitleLabel;
    private Label missionResultSummaryBodyLabel;
    private VisualElement missionResultBadge;
    private Button missionResultContinueAction;
    private Button missionResultReplayAction;
    private EventCallback<ClickEvent> missionResultContinueCallback;
    private EventCallback<ClickEvent> missionResultReplayCallback;
    private Label settingsTitleLabel;
    private Button settingsCloseAction;
    private EventCallback<ClickEvent> settingsCloseCallback;
    private Label inboxTitleLabel;
    private Button inboxCloseAction;
    private EventCallback<ClickEvent> inboxCloseCallback;
    private Button diagnosticsFpsAction;
    private Button diagnosticsCloseAction;
    private Label diagnosticsFpsValueLabel;
    private Label diagnosticsLogTextLabel;
    private EventCallback<ClickEvent> diagnosticsFpsCallback;
    private EventCallback<ClickEvent> diagnosticsCloseCallback;

    public UIDocument Document => document;
    public VisualTreeAsset ShellAsset => shellAsset;
    public VisualTreeAsset LoadingScreenAsset => loadingScreenAsset;
    public VisualTreeAsset MainMenuScreenAsset => mainMenuScreenAsset;
    public VisualTreeAsset MatchHudScreenAsset => matchHudScreenAsset;
    public VisualTreeAsset ArmoryScreenAsset => armoryScreenAsset;
    public VisualTreeAsset CommanderProfileScreenAsset => commanderProfileScreenAsset;
    public VisualTreeAsset BuildDrawerPopupAsset => buildDrawerPopupAsset;
    public VisualTreeAsset MissionResultPopupAsset => missionResultPopupAsset;
    public VisualTreeAsset SettingsPopupAsset => settingsPopupAsset;
    public VisualTreeAsset InboxPopupAsset => inboxPopupAsset;
    public VisualTreeAsset BuildPlacementConfirmationBarAsset => buildPlacementConfirmationBarAsset;
    public VisualElement Root => root;
    public VisualElement SafeAreaRoot => safeAreaRoot;
    public VisualElement HeaderBar => headerBar;
    public VisualElement ContentRoot => contentRoot;
    public VisualElement FooterBar => footerBar;
    public VisualElement ModalOverlay => modalOverlay;
    public VisualElement TooltipLayer => tooltipLayer;
    public VisualElement DiagnosticsOverlay => diagnosticsOverlay;
    public VisualElement DiagnosticsLogPanel => diagnosticsLogPanel;
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
    public VisualElement ArmoryContentRoot => armoryContentRoot;
    public VisualElement CommanderProfileContentRoot => commanderProfileContentRoot;
    public VisualElement BuildDrawerPopupRoot => buildDrawerPopupRoot;
    public VisualElement MissionResultPopupRoot => missionResultPopupRoot;
    public VisualElement SettingsPopupRoot => settingsPopupRoot;
    public VisualElement InboxPopupRoot => inboxPopupRoot;
    public VisualElement BuildPlacementConfirmationBarRoot => buildPlacementConfirmationBarRoot;
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
    public VisualElement MatchHudMinimapMap => matchHudMinimapMap;
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
    public Label BuildPlacementTitleLabel => buildPlacementTitleLabel;
    public Label BuildPlacementStatusLabel => buildPlacementStatusLabel;
    public Label BuildPlacementCostLabel => buildPlacementCostLabel;
    public Label BuildPlacementDurationLabel => buildPlacementDurationLabel;
    public Label BuildPlacementInstructionLabel => buildPlacementInstructionLabel;
    public Button BuildPlacementCancelAction => buildPlacementCancelAction;
    public Button BuildPlacementRotateAction => buildPlacementRotateAction;
    public Button BuildPlacementConfirmAction => buildPlacementConfirmAction;
    public ScrollView ArmoryScrollView => armoryScrollView;
    public VisualElement ArmoryCatalogContent => armoryCatalogContent;
    public VisualElement ArmoryInspectionPanel => armoryInspectionPanel;
    public Label ArmoryInspectionNameLabel => armoryInspectionNameLabel;
    public Label ArmoryInspectionTypeLabel => armoryInspectionTypeLabel;
    public Button ArmoryUpgradeAction => armoryUpgradeAction;
    public Button ArmoryEquipAction => armoryEquipAction;
    public Button ArmoryCloseAction => armoryCloseAction;
    public IReadOnlyList<Button> ArmoryCategoryActions => armoryCategoryActions;
    public IReadOnlyList<Button> ArmoryItems => armoryItems;
    public IReadOnlyList<Label> ArmoryItemTitleLabels => armoryItemTitleLabels;
    public int SelectedArmoryItemIndex => selectedArmoryItemIndex;
    public Button CommanderProfileBackAction => commanderProfileBackAction;
    public Button CommanderProfileOpenArmoryAction => commanderProfileOpenArmoryAction;
    public Label CommanderProfileNameLabel => commanderProfileNameLabel;
    public Label CommanderProfileSubtitleLabel => commanderProfileSubtitleLabel;
    public Label MissionResultTitleLabel => missionResultTitleLabel;
    public Label MissionResultSubtitleLabel => missionResultSubtitleLabel;
    public Label MissionResultSummaryBodyLabel => missionResultSummaryBodyLabel;
    public VisualElement MissionResultBadge => missionResultBadge;
    public Button MissionResultContinueAction => missionResultContinueAction;
    public Button MissionResultReplayAction => missionResultReplayAction;
    public Label SettingsTitleLabel => settingsTitleLabel;
    public Button SettingsCloseAction => settingsCloseAction;
    public Label InboxTitleLabel => inboxTitleLabel;
    public Button InboxCloseAction => inboxCloseAction;
    public Button DiagnosticsFpsAction => diagnosticsFpsAction;
    public Button DiagnosticsCloseAction => diagnosticsCloseAction;
    public Label DiagnosticsFpsValueLabel => diagnosticsFpsValueLabel;
    public Label DiagnosticsLogTextLabel => diagnosticsLogTextLabel;
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
    public bool HasMountedArmoryScreen =>
        armoryScreenContainer != null
        && armoryContentRoot != null
        && armoryScreenContainer.parent == armoryScreenSlot;
    public bool HasMountedCommanderProfileScreen =>
        commanderProfileScreenContainer != null
        && commanderProfileContentRoot != null
        && commanderProfileScreenContainer.parent == commanderProfileScreenSlot;
    public bool HasMountedBuildDrawerPopup =>
        buildDrawerPopupContainer != null
        && buildDrawerPopupRoot != null
        && buildDrawerPopupContainer.parent == popupScreenSlot;
    public bool HasMountedMissionResultPopup =>
        missionResultPopupContainer != null
        && missionResultPopupRoot != null
        && missionResultPopupContainer.parent == popupScreenSlot;
    public bool HasMountedSettingsPopup =>
        settingsPopupContainer != null
        && settingsPopupRoot != null
        && settingsPopupContainer.parent == popupScreenSlot;
    public bool HasMountedInboxPopup =>
        inboxPopupContainer != null
        && inboxPopupRoot != null
        && inboxPopupContainer.parent == popupScreenSlot;
    public bool HasMountedBuildPlacementConfirmationBar =>
        buildPlacementConfirmationBarContainer != null
        && buildPlacementConfirmationBarRoot != null
        && buildPlacementConfirmationBarContainer.parent == matchScreenSlot;
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
    public bool HasRequiredArmoryBindings =>
        armoryContentRoot != null
        && armoryContentRoot.Q<VisualElement>("HeaderContent") != null
        && armoryContentRoot.Q<Button>("Nav_Units") != null
        && armoryContentRoot.Q<Button>("Nav_Vehicles") != null
        && armoryContentRoot.Q<Button>("Nav_Aircraft") != null
        && armoryContentRoot.Q<Button>("Nav_Buildings") != null
        && armoryContentRoot.Q<Button>("Nav_Upgrades") != null
        && armoryContentRoot.Q<ScrollView>("Scroll_View") != null
        && armoryContentRoot.Q<VisualElement>("Content") != null
        && armoryContentRoot.Q<VisualElement>("InspectionPanel") != null
        && armoryContentRoot.Q<Button>("UpgradeButton") != null
        && armoryContentRoot.Q<Button>("EquipButton") != null
        && armoryContentRoot.Q<Button>("CloseButton") != null;
    public bool HasRequiredCommanderProfileBindings =>
        commanderProfileContentRoot != null
        && commanderProfileBackAction != null
        && commanderProfileOverviewTabAction != null
        && commanderProfileStatsTabAction != null
        && commanderProfileBadgesTabAction != null
        && commanderProfileHistoryTabAction != null
        && commanderProfileUpgradesTabAction != null
        && commanderProfileOpenArmoryAction != null
        && commanderProfileDetailAction != null
        && commanderProfileReplayAction != null
        && commanderProfilePortrait != null
        && commanderProfileBadge != null
        && commanderProfileTitleLabel != null
        && commanderProfileNameLabel != null
        && commanderProfileSubtitleLabel != null
        && commanderProfileLevelLabel != null;
    public bool HasRequiredArmoryRuntimeBindings =>
        HasRequiredArmoryBindings
        && armoryScrollView != null
        && armoryCatalogContent != null
        && armoryInspectionPanel != null
        && armoryInspectionNameLabel != null
        && armoryInspectionTypeLabel != null
        && armoryInspectionPortraitArt != null
        && armoryFilterAction != null
        && armorySortAction != null
        && armoryUpgradeAction != null
        && armoryEquipAction != null
        && armoryCloseAction != null
        && armoryTabAction != null
        && armoryWorkshopTabAction != null
        && armoryDoctrineTabAction != null
        && armoryDepotTabAction != null
        && armoryOfficersTabAction != null
        && armoryCategoryActions[0] != null
        && armoryCategoryActions[4] != null
        && armoryItems[0] != null
        && armoryItems[7] != null
        && armoryItemTitleLabels[0] != null
        && armoryItemStateLabels[0] != null
        && armoryItemLevelLabels[0] != null
        && armoryItemTypeLabels[0] != null;
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
    public bool HasRequiredMissionResultBindings =>
        missionResultPopupRoot != null
        && missionResultTitleLabel != null
        && missionResultSubtitleLabel != null
        && missionResultSummaryBodyLabel != null
        && missionResultBadge != null
        && missionResultContinueAction != null
        && missionResultReplayAction != null;
    public bool HasRequiredSettingsBindings =>
        settingsPopupRoot != null
        && settingsTitleLabel != null
        && settingsCloseAction != null;
    public bool HasRequiredInboxBindings =>
        inboxPopupRoot != null
        && inboxTitleLabel != null
        && inboxCloseAction != null;
    public bool HasRequiredBuildPlacementConfirmationBarBindings =>
        buildPlacementConfirmationBarRoot != null
        && buildPlacementTitleLabel != null
        && buildPlacementStatusLabel != null
        && buildPlacementCostLabel != null
        && buildPlacementDurationLabel != null
        && buildPlacementInstructionLabel != null
        && buildPlacementCancelAction != null
        && buildPlacementRotateAction != null
        && buildPlacementConfirmAction != null;
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
        && diagnosticsOverlay != null
        && loadingLayer != null;
    public bool HasRequiredDiagnosticsBindings =>
        diagnosticsOverlay != null
        && diagnosticsLogPanel != null
        && diagnosticsFpsAction != null
        && diagnosticsCloseAction != null
        && diagnosticsFpsValueLabel != null
        && diagnosticsLogTextLabel != null;
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
        Configure(
            configuredDocument,
            configuredShellAsset,
            configuredLoadingScreenAsset,
            configuredMainMenuScreenAsset,
            configuredMatchHudScreenAsset,
            configuredBuildDrawerPopupAsset,
            null);
    }

    public void Configure(
        UIDocument configuredDocument,
        VisualTreeAsset configuredShellAsset,
        VisualTreeAsset configuredLoadingScreenAsset,
        VisualTreeAsset configuredMainMenuScreenAsset,
        VisualTreeAsset configuredMatchHudScreenAsset,
        VisualTreeAsset configuredBuildDrawerPopupAsset,
        VisualTreeAsset configuredBuildPlacementConfirmationBarAsset)
    {
        Configure(
            configuredDocument,
            configuredShellAsset,
            configuredLoadingScreenAsset,
            configuredMainMenuScreenAsset,
            configuredMatchHudScreenAsset,
            null,
            configuredBuildDrawerPopupAsset,
            configuredBuildPlacementConfirmationBarAsset);
    }

    public void Configure(
        UIDocument configuredDocument,
        VisualTreeAsset configuredShellAsset,
        VisualTreeAsset configuredLoadingScreenAsset,
        VisualTreeAsset configuredMainMenuScreenAsset,
        VisualTreeAsset configuredMatchHudScreenAsset,
        VisualTreeAsset configuredArmoryScreenAsset,
        VisualTreeAsset configuredBuildDrawerPopupAsset,
        VisualTreeAsset configuredBuildPlacementConfirmationBarAsset,
        VisualTreeAsset configuredCommanderProfileScreenAsset = null,
        VisualTreeAsset configuredMissionResultPopupAsset = null,
        VisualTreeAsset configuredSettingsPopupAsset = null,
        VisualTreeAsset configuredInboxPopupAsset = null)
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
        if (configuredArmoryScreenAsset != null)
            armoryScreenAsset = configuredArmoryScreenAsset;
        if (configuredCommanderProfileScreenAsset != null)
            commanderProfileScreenAsset = configuredCommanderProfileScreenAsset;
        if (configuredBuildDrawerPopupAsset != null)
            buildDrawerPopupAsset = configuredBuildDrawerPopupAsset;
        if (configuredMissionResultPopupAsset != null)
            missionResultPopupAsset = configuredMissionResultPopupAsset;
        if (configuredSettingsPopupAsset != null)
            settingsPopupAsset = configuredSettingsPopupAsset;
        if (configuredInboxPopupAsset != null)
            inboxPopupAsset = configuredInboxPopupAsset;
        if (configuredBuildPlacementConfirmationBarAsset != null)
            buildPlacementConfirmationBarAsset = configuredBuildPlacementConfirmationBarAsset;
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
            MountArmoryScreen();
            MountCommanderProfileScreen();
            MountBuildPlacementConfirmationBar();
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
        StretchTemplateContainer(loadingScreenContainer);
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
        StretchTemplateContainer(mainMenuScreenContainer);
        mainMenuScreenContainer.name = "SCN02_MainMenuContent_Template";
        mainMenuContentRoot = mainMenuScreenContainer.Q<VisualElement>("SCN02_MainMenuContent");
        mainMenuScreenSlot.Add(mainMenuScreenContainer);
        BindMainMenuScreen();
        return HasRequiredMainMenuBindings;
    }

    public bool EnsureMainMenuVisible(UIRoute route)
    {
        if (!IsMounted)
            return false;

        MountMainMenuScreen();
        if (route == UIRoute.Armory)
            MountArmoryScreen();
        if (route == UIRoute.CommandFeed)
            MountCommanderProfileScreen();

        ApplyMainMenuRouteState(route);
        SetShellHidden(loadingLayer, true);
        SetShellHidden(matchScreenSlot, true);
        SetShellHidden(resultScreenSlot, true);
        if (route == UIRoute.Armory && HasMountedArmoryScreen)
        {
            SetShellHidden(mainMenuScreenSlot, true);
            SetShellHidden(armoryScreenSlot, false);
            SetShellHidden(commanderProfileScreenSlot, true);
            ApplyShellMotion(armoryScreenSlot, UiToolkitShellMotionState.Visible);
            return true;
        }

        SetShellHidden(mainMenuScreenSlot, false);
        SetShellHidden(armoryScreenSlot, true);
        SetShellHidden(commanderProfileScreenSlot, route != UIRoute.CommandFeed);
        ApplyShellMotion(mainMenuScreenSlot, UiToolkitShellMotionState.Visible);
        if (route == UIRoute.CommandFeed && HasMountedCommanderProfileScreen)
            ApplyShellMotion(commanderProfileScreenSlot, UiToolkitShellMotionState.Visible);
        return true;
    }

    public bool MountMatchHudScreen()
    {
        if (matchScreenSlot == null || matchHudScreenAsset == null)
            return false;

        if (HasMountedMatchHudScreen)
            return true;

        matchScreenSlot.Clear();
        matchHudScreenContainer = matchHudScreenAsset.Instantiate();
        StretchTemplateContainer(matchHudScreenContainer);
        matchHudScreenContainer.name = "SCN08_MatchHudContent_Template";
        matchHudContentRoot = matchHudScreenContainer.Q<VisualElement>("SCN08_MatchHudContent");
        matchScreenSlot.Add(matchHudScreenContainer);
        BindMatchHudScreen();
        SetShellHidden(matchScreenSlot, true);
        return HasRequiredMatchHudBindings;
    }

    public bool MountArmoryScreen()
    {
        if (armoryScreenSlot == null || armoryScreenAsset == null)
            return false;

        if (HasMountedArmoryScreen)
            return true;

        armoryScreenSlot.Clear();
        armoryScreenContainer = armoryScreenAsset.Instantiate();
        StretchTemplateContainer(armoryScreenContainer);
        armoryScreenContainer.name = "SCN19_ArmoryContent_Template";
        armoryContentRoot = armoryScreenContainer.Q<VisualElement>("SCN19_ArmoryContent");
        armoryScreenSlot.Add(armoryScreenContainer);
        BindArmoryScreen();
        SetShellHidden(armoryScreenSlot, true);
        return HasRequiredArmoryRuntimeBindings;
    }

    public bool MountCommanderProfileScreen()
    {
        if (commanderProfileScreenSlot == null || commanderProfileScreenAsset == null)
            return false;

        if (HasMountedCommanderProfileScreen)
            return true;

        commanderProfileScreenSlot.Clear();
        commanderProfileScreenContainer = commanderProfileScreenAsset.Instantiate();
        StretchTemplateContainer(commanderProfileScreenContainer);
        commanderProfileScreenContainer.name = "SCN03_CommanderProfileContent_Template";
        commanderProfileContentRoot = commanderProfileScreenContainer.Q<VisualElement>("SCN03_CommanderProfileContent");
        commanderProfileScreenSlot.Add(commanderProfileScreenContainer);
        BindCommanderProfileScreen();
        SetShellHidden(commanderProfileScreenSlot, true);
        return HasRequiredCommanderProfileBindings;
    }

    public bool MountBuildDrawerPopup()
    {
        if (popupScreenSlot == null || buildDrawerPopupAsset == null)
            return false;

        if (HasMountedBuildDrawerPopup)
            return true;

        popupScreenSlot.Clear();
        buildDrawerPopupContainer = buildDrawerPopupAsset.Instantiate();
        StretchTemplateContainer(buildDrawerPopupContainer);
        buildDrawerPopupContainer.name = "SCN09_BuildDrawerPopup_Template";
        buildDrawerPopupRoot = buildDrawerPopupContainer.Q<VisualElement>("SCN09_BuildDrawerPopup");
        popupScreenSlot.Add(buildDrawerPopupContainer);
        BindBuildDrawerPopup();
        SetShellHidden(popupScreenSlot, true);
        SetShellHidden(modalOverlay, true);
        return HasRequiredBuildDrawerBindings;
    }

    public bool MountMissionResultPopup()
    {
        if (popupScreenSlot == null || missionResultPopupAsset == null)
            return false;

        if (HasMountedMissionResultPopup)
            return true;

        popupScreenSlot.Clear();
        missionResultPopupContainer = missionResultPopupAsset.Instantiate();
        StretchTemplateContainer(missionResultPopupContainer);
        missionResultPopupContainer.name = "POP05_MissionResultPopup_Template";
        missionResultPopupRoot = missionResultPopupContainer.Q<VisualElement>("POP05_MissionResultPopup");
        popupScreenSlot.Add(missionResultPopupContainer);
        BindMissionResultPopup();
        SetShellHidden(popupScreenSlot, true);
        SetShellHidden(modalOverlay, true);
        return HasRequiredMissionResultBindings;
    }

    public bool MountSettingsPopup()
    {
        if (popupScreenSlot == null || settingsPopupAsset == null)
            return false;

        if (HasMountedSettingsPopup)
            return true;

        popupScreenSlot.Clear();
        settingsPopupContainer = settingsPopupAsset.Instantiate();
        StretchTemplateContainer(settingsPopupContainer);
        settingsPopupContainer.name = "POP06_SettingsPopup_Template";
        settingsPopupRoot = settingsPopupContainer.Q<VisualElement>("POP06_SettingsPopup");
        popupScreenSlot.Add(settingsPopupContainer);
        BindSettingsPopup();
        SetShellHidden(popupScreenSlot, true);
        SetShellHidden(modalOverlay, true);
        return HasRequiredSettingsBindings;
    }

    public bool MountInboxPopup()
    {
        if (popupScreenSlot == null || inboxPopupAsset == null)
            return false;

        if (HasMountedInboxPopup)
            return true;

        popupScreenSlot.Clear();
        inboxPopupContainer = inboxPopupAsset.Instantiate();
        StretchTemplateContainer(inboxPopupContainer);
        inboxPopupContainer.name = "POP07_InboxPopup_Template";
        inboxPopupRoot = inboxPopupContainer.Q<VisualElement>("POP07_InboxPopup");
        popupScreenSlot.Add(inboxPopupContainer);
        BindInboxPopup();
        SetShellHidden(popupScreenSlot, true);
        SetShellHidden(modalOverlay, true);
        return HasRequiredInboxBindings;
    }

    public bool MountBuildPlacementConfirmationBar()
    {
        if (matchScreenSlot == null || buildPlacementConfirmationBarAsset == null)
            return false;

        if (HasMountedBuildPlacementConfirmationBar)
            return true;

        buildPlacementConfirmationBarContainer = buildPlacementConfirmationBarAsset.Instantiate();
        StretchTemplateContainer(buildPlacementConfirmationBarContainer);
        buildPlacementConfirmationBarContainer.name = "SCN08_BuildPlacementConfirmationBar_Template";
        buildPlacementConfirmationBarRoot = buildPlacementConfirmationBarContainer.Q<VisualElement>("SCN08_BuildPlacementConfirmationBar");
        matchScreenSlot.Add(buildPlacementConfirmationBarContainer);
        BindBuildPlacementConfirmationBar();
        SetShellHidden(buildPlacementConfirmationBarContainer, true);
        SetShellHidden(buildPlacementConfirmationBarRoot, true);
        return HasRequiredBuildPlacementConfirmationBarBindings;
    }

    private static void StretchTemplateContainer(TemplateContainer container)
    {
        if (container == null)
            return;

        container.style.position = Position.Absolute;
        container.style.left = 0f;
        container.style.right = 0f;
        container.style.top = 0f;
        container.style.bottom = 0f;
        container.style.flexGrow = 1f;
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

    public bool TrySubmitCommanderProfileAction(string actionName)
    {
        switch (actionName)
        {
            case "BackButton":
                return EnqueueMainMenuRoute(
                    UiShellRouteIntent.BackMenuRoute,
                    UIRoute.MainMenu,
                    pushHistory: false);
            case "OverviewTab":
                return EnqueueMainMenuRoute(
                    UiShellRouteIntent.OpenMenuRoute,
                    UIRoute.CommandFeed,
                    pushHistory: false);
            case "OpenArmoryButton":
                return EnqueueMainMenuRoute(
                    UiShellRouteIntent.OpenMenuRoute,
                    UIRoute.Armory,
                    pushHistory: true);
            default:
                return false;
        }
    }

    public bool TrySubmitMissionResultAction(string actionName)
    {
        switch (actionName)
        {
            case "ContinueButton":
                return EnqueueMainMenuRoute(UiShellRouteIntent.ReturnToMainMenu, UIRoute.MainMenu, pushHistory: false);
            case "ReplayButton":
                return true;
            default:
                return false;
        }
    }

    public bool TrySubmitSettingsAction(string actionName)
    {
        switch (actionName)
        {
            case "CloseButton":
                return EnqueueMainMenuRoute(UiShellRouteIntent.BackMenuRoute, UIRoute.MainMenu, pushHistory: false);
            default:
                return false;
        }
    }

    public bool TrySubmitInboxAction(string actionName)
    {
        switch (actionName)
        {
            case "CloseButton":
                return EnqueueMainMenuRoute(UiShellRouteIntent.BackMenuRoute, UIRoute.MainMenu, pushHistory: false);
            default:
                return false;
        }
    }

    public bool TrySubmitArmoryCategory(ArmoryCatalogCategory category)
    {
        bool queued = UiShellRuntimeGateway.TryEnqueueArmoryCategory(category);
        ApplyArmoryCategory(category);
        return queued;
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

    public bool ApplyDiagnosticsOverlay(UiDiagnosticsOverlayModel diagnostics)
    {
        if (!HasRequiredDiagnosticsBindings)
            return false;

        SetLabelText(diagnosticsFpsValueLabel, Mathf.Max(0, diagnostics.Fps).ToString(), "0");

        SetLabelText(diagnosticsLogTextLabel, diagnostics.LogText, "Runtime log ready.");
        SetShellHidden(diagnosticsLogPanel, !diagnostics.LogVisible);
        SetClass(diagnosticsOverlay, "diagnostics-overlay-expanded", diagnostics.LogVisible);
        return true;
    }

    public bool ApplyMissionResult(UiMissionResultPopupModel result)
    {
        if (!HasRequiredMissionResultBindings)
            return false;

        SetLabelText(missionResultTitleLabel, result.Title, "VICTORY");
        SetLabelText(missionResultSubtitleLabel, result.Subtitle, "Sector secured. Command net restored.");
        SetLabelText(missionResultSummaryBodyLabel, result.SummaryBody, UiMissionResultPopupModel.VictoryDefault.SummaryBody);

        missionResultBadge.RemoveFromClassList("victory-badge");
        missionResultBadge.RemoveFromClassList("loss-badge");
        missionResultBadge.AddToClassList(result.Outcome == UiMissionResultOutcome.Loss ? "loss-badge" : "victory-badge");
        SetElementEnabled(missionResultReplayAction, result.ReplayEnabled);
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

        if (IsSelfOrDescendantOf(element, contentRoot) ||
            IsSelfOrDescendantOf(element, tooltipLayer) ||
            IsSelfOrDescendantOf(element, diagnosticsOverlay))
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
        diagnosticsOverlay = null;
        diagnosticsLogPanel = null;
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
        armoryScreenContainer = null;
        commanderProfileScreenContainer = null;
        buildDrawerPopupContainer = null;
        missionResultPopupContainer = null;
        settingsPopupContainer = null;
        inboxPopupContainer = null;
        buildPlacementConfirmationBarContainer = null;
        loadingContentRoot = null;
        mainMenuContentRoot = null;
        matchHudContentRoot = null;
        armoryContentRoot = null;
        commanderProfileContentRoot = null;
        buildDrawerPopupRoot = null;
        missionResultPopupRoot = null;
        settingsPopupRoot = null;
        inboxPopupRoot = null;
        buildPlacementConfirmationBarRoot = null;
        mainMenuHeaderContent = null;
        ClearLoadingBindings();
        ClearMainMenuBindings();
        ClearMatchHudBindings();
        ClearArmoryBindings();
        ClearCommanderProfileBindings();
        ClearBuildDrawerBindings();
        ClearMissionResultBindings();
        ClearSettingsBindings();
        ClearInboxBindings();
        ClearDiagnosticsBindings();
        ClearBuildPlacementConfirmationBarBindings();
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
                if (command.Route == UIRoute.Armory)
                    MountArmoryScreen();
                if (command.Route == UIRoute.CommandFeed)
                    MountCommanderProfileScreen();
                ApplyMainMenuRouteState(command.Route);
                if (command.Route == UIRoute.Armory && HasMountedArmoryScreen)
                {
                    SetShellHidden(mainMenuScreenSlot, true);
                    SetShellHidden(armoryScreenSlot, false);
                    SetShellHidden(commanderProfileScreenSlot, true);
                    SetShellHidden(resultScreenSlot, true);
                    ApplyShellMotion(armoryScreenSlot, UiToolkitShellMotionState.Visible);
                }
                else
                {
                    SetShellHidden(mainMenuScreenSlot, false);
                    SetShellHidden(armoryScreenSlot, true);
                    SetShellHidden(commanderProfileScreenSlot, command.Route != UIRoute.CommandFeed);
                    SetShellHidden(resultScreenSlot, true);
                    ApplyShellMotion(mainMenuScreenSlot, UiToolkitShellMotionState.Visible);
                    if (command.Route == UIRoute.CommandFeed && HasMountedCommanderProfileScreen)
                        ApplyShellMotion(commanderProfileScreenSlot, UiToolkitShellMotionState.Visible);
                }
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
                if (command.Route == UIRoute.Settings)
                    MountSettingsPopup();
                else if (command.Route == UIRoute.Inbox)
                    MountInboxPopup();
                else
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
            if (target.pickingMode != PickingMode.Ignore)
                target.pickingMode = PickingMode.Ignore;
            return;
        }

        if (target.ClassListContains("shell-hidden"))
            target.RemoveFromClassList("shell-hidden");
        if (target.pickingMode != PickingMode.Position)
            target.pickingMode = PickingMode.Position;
    }

    private static void SetElementEnabled(VisualElement target, bool enabled)
    {
        if (target != null && target.enabledSelf != enabled)
            target.SetEnabled(enabled);
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
        bool showCommanderProfile = route == UIRoute.CommandFeed && MountCommanderProfileScreen();
        SetShellHidden(commanderProfileScreenSlot, !showCommanderProfile);
        if (route == UIRoute.Armory && MountArmoryScreen())
        {
            SetShellHidden(mainMenuScreenSlot, true);
            SetShellHidden(armoryScreenSlot, false);
        }
        else
        {
            SetShellHidden(mainMenuScreenSlot, false);
            SetShellHidden(armoryScreenSlot, true);
        }
        return true;
    }

    public bool ApplyArmoryCategory(ArmoryCatalogCategory category)
    {
        if (!HasRequiredArmoryRuntimeBindings)
            return false;

        for (int i = 0; i < armoryCategoryActions.Length; i++)
            SetClass(armoryCategoryActions[i], "category-selected", i == (int)category);

        return true;
    }

    public bool SelectArmoryItem(int index)
    {
        if (!HasRequiredArmoryRuntimeBindings || index < 0 || index >= armoryItems.Length)
            return false;

        Button item = armoryItems[index];
        if (item == null || item.ClassListContains("locked"))
            return false;

        selectedArmoryItemIndex = index;
        for (int i = 0; i < armoryItems.Length; i++)
        {
            Button candidate = armoryItems[i];
            if (candidate == null)
                continue;

            bool selected = i == selectedArmoryItemIndex;
            SetClass(candidate, "selected", selected);
            SetClass(candidate, "default", !selected && !candidate.ClassListContains("locked"));
        }

        SetLabelText(armoryInspectionNameLabel, armoryItemTitleLabels[index]?.text, "ITEM");
        SetLabelText(armoryInspectionTypeLabel, armoryItemTypeLabels[index]?.text, "UNIT");
        SetElementEnabled(armoryUpgradeAction, true);
        SetElementEnabled(armoryEquipAction, true);
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

    public bool ApplyCommanderProfile(UiShellCommanderProfileModel profile)
    {
        if (commanderProfileContentRoot == null)
            return false;

        string name = string.IsNullOrWhiteSpace(profile.Name)
            ? DefaultCommanderName
            : profile.Name;
        string subtitle = string.IsNullOrWhiteSpace(profile.Subtitle)
            ? DefaultCommanderSubtitle
            : profile.Subtitle;
        string portraitClass = string.IsNullOrWhiteSpace(profile.PortraitClass)
            ? DefaultCommanderPortraitClass
            : profile.PortraitClass;

        SetLabelText(commanderProfileTitleLabel, "FIELD COMMANDER", "FIELD COMMANDER");
        SetLabelText(commanderProfileNameLabel, name, DefaultCommanderName);
        SetLabelText(commanderProfileSubtitleLabel, subtitle, DefaultCommanderSubtitle);
        SetLabelText(commanderProfileLevelLabel, "LEVEL 38", "LEVEL 38");
        ApplyKnownClass(commanderProfilePortrait, MainMenuCommanderPortraitClasses, portraitClass);
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
        SetClass(matchHudSelectedBoardAction, "selected-action-selected", activeMode == TacticalCommandMode.Board);
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

    public bool ApplyBuildPlacementConfirmationBar(UiBuildPlacementConfirmationBarModel placementBar)
    {
        if (!HasRequiredBuildPlacementConfirmationBarBindings)
            return false;

        SetShellHidden(buildPlacementConfirmationBarContainer, !placementBar.Visible);
        SetShellHidden(buildPlacementConfirmationBarRoot, !placementBar.Visible);
        SetLabelText(buildPlacementTitleLabel, placementBar.Title, "PLACE BUILDING");
        SetLabelText(buildPlacementStatusLabel, placementBar.Status, placementBar.CanConfirm ? "VALID GROUND" : "INVALID PLACEMENT");
        SetLabelText(buildPlacementCostLabel, placementBar.CostText, "0");
        SetLabelText(buildPlacementDurationLabel, placementBar.DurationText, "00:00");
        SetLabelText(buildPlacementInstructionLabel, placementBar.InstructionText, "DRAG TO POSITION, CONFIRM TO BUILD");
        SetElementEnabled(buildPlacementCancelAction, placementBar.CanCancel);
        SetElementEnabled(buildPlacementRotateAction, placementBar.CanRotate);
        SetElementEnabled(buildPlacementConfirmAction, placementBar.CanConfirm);
        SetClass(buildPlacementConfirmationBarRoot, "placement-valid", placementBar.Visible && placementBar.CanConfirm);
        SetClass(buildPlacementConfirmationBarRoot, "placement-invalid", placementBar.Visible && !placementBar.CanConfirm);
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

        SetLabelText(mainMenuCreditsValueLabel, resources.CreditsText, DefaultCreditsText);
        SetLabelText(mainMenuSuppliesValueLabel, resources.SuppliesText, DefaultSuppliesText);
        SetLabelText(mainMenuCommandValueLabel, resources.CommandText, DefaultCommandText);
        return true;
    }

    private void ApplyMainMenuRouteClass(UIRoute route)
    {
        string resolvedRouteClass = ResolveMainMenuRouteClass(route);
        bool hasResolvedRoute = false;
        bool hasStaleRoute = false;
        for (int i = 0; i < MainMenuRouteClasses.Length; i++)
        {
            bool hasClass = mainMenuContentRoot.ClassListContains(MainMenuRouteClasses[i]);
            hasResolvedRoute |= hasClass && MainMenuRouteClasses[i] == resolvedRouteClass;
            hasStaleRoute |= hasClass && MainMenuRouteClasses[i] != resolvedRouteClass;
        }

        if (hasResolvedRoute && !hasStaleRoute)
            return;

        for (int i = 0; i < MainMenuRouteClasses.Length; i++)
        {
            if (mainMenuContentRoot.ClassListContains(MainMenuRouteClasses[i]))
                mainMenuContentRoot.RemoveFromClassList(MainMenuRouteClasses[i]);
        }

        if (!mainMenuContentRoot.ClassListContains(resolvedRouteClass))
            mainMenuContentRoot.AddToClassList(resolvedRouteClass);
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

        if (target.ClassListContains(className))
            target.RemoveFromClassList(className);
    }

    private static void ApplyKnownClass(VisualElement target, string[] knownClasses, string className)
    {
        if (target == null)
            return;

        bool hasRequestedClass = !string.IsNullOrWhiteSpace(className) && target.ClassListContains(className);
        bool hasStaleClass = false;
        for (int i = 0; i < knownClasses.Length; i++)
        {
            if (knownClasses[i] != className && target.ClassListContains(knownClasses[i]))
                hasStaleClass = true;
        }

        if (hasRequestedClass && !hasStaleClass)
            return;

        for (int i = 0; i < knownClasses.Length; i++)
        {
            if (target.ClassListContains(knownClasses[i]))
                target.RemoveFromClassList(knownClasses[i]);
        }

        if (!string.IsNullOrWhiteSpace(className) && !target.ClassListContains(className))
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

        string nextText = string.IsNullOrWhiteSpace(text)
            ? fallback
            : text;
        if (label.text != nextText)
            label.text = nextText;
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
        diagnosticsOverlay = root.Q<VisualElement>("DiagnosticsOverlay");
        diagnosticsLogPanel = root.Q<VisualElement>("DiagnosticsLogPanel");
        loadingLayer = root.Q<VisualElement>("LoadingLayer");
        menuBackgroundRegion = root.Q<VisualElement>("MenuBackgroundRegion");
        if (menuBackgroundRegion != null)
            menuBackgroundRegion.pickingMode = PickingMode.Ignore;

        VisualElement headerRegion = root.Q<VisualElement>("HeaderRegion");
        if (headerRegion != null)
            headerRegion.pickingMode = PickingMode.Ignore;

        VisualElement leftRegion = root.Q<VisualElement>("LeftRegion");
        if (leftRegion != null)
            leftRegion.pickingMode = PickingMode.Ignore;

        VisualElement rightRegion = root.Q<VisualElement>("RightRegion");
        if (rightRegion != null)
            rightRegion.pickingMode = PickingMode.Ignore;

        VisualElement footerRegion = root.Q<VisualElement>("FooterRegion");
        if (footerRegion != null)
            footerRegion.pickingMode = PickingMode.Ignore;

        loadingScreenSlot = root.Q<VisualElement>("LoadingScreenSlot");
        mainMenuScreenSlot = root.Q<VisualElement>("MainMenuScreenSlot");
        matchScreenSlot = root.Q<VisualElement>("MatchScreenSlot");
        armoryScreenSlot = root.Q<VisualElement>("ArmoryScreenSlot");
        commanderProfileScreenSlot = root.Q<VisualElement>("CommanderProfileScreenSlot");
        resultScreenSlot = root.Q<VisualElement>("ResultScreenSlot");
        popupScreenSlot = root.Q<VisualElement>("PopupScreenSlot");
        if (tooltipLayer != null)
            tooltipLayer.pickingMode = PickingMode.Ignore;

        BindDiagnosticsOverlay();
    }

    private void BindDiagnosticsOverlay()
    {
        ClearDiagnosticsBindings();
        if (diagnosticsOverlay == null)
            return;

        diagnosticsFpsAction = diagnosticsOverlay.Q<Button>("DiagnosticsFpsButton");
        diagnosticsCloseAction = diagnosticsOverlay.Q<Button>("DiagnosticsCloseButton");
        diagnosticsFpsValueLabel = diagnosticsOverlay.Q<Label>("DiagnosticsFpsValue");
        diagnosticsLogTextLabel = diagnosticsOverlay.Q<Label>("DiagnosticsLogText");
        RegisterDiagnosticsFpsAction();
        RegisterDiagnosticsCloseAction();
        ApplyDiagnosticsOverlay(UiDiagnosticsOverlayModel.Default);
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

    private void BindMissionResultPopup()
    {
        ClearMissionResultBindings();
        if (missionResultPopupRoot == null)
            return;

        missionResultTitleLabel = missionResultPopupRoot.Q<Label>("Title");
        missionResultSubtitleLabel = missionResultPopupRoot.Q<Label>("Subtitle");
        missionResultSummaryBodyLabel = missionResultPopupRoot.Q<Label>("SummaryBody");
        missionResultBadge = missionResultPopupRoot.Q<VisualElement>("ResultBadge");
        missionResultContinueAction = missionResultPopupRoot.Q<Button>("ContinueButton");
        missionResultReplayAction = missionResultPopupRoot.Q<Button>("ReplayButton");
        RegisterMissionResultContinueAction();
        RegisterMissionResultReplayAction();
    }

    private void BindSettingsPopup()
    {
        ClearSettingsBindings();
        if (settingsPopupRoot == null)
            return;

        settingsTitleLabel = settingsPopupRoot.Q<Label>("Title");
        settingsCloseAction = settingsPopupRoot.Q<Button>("CloseButton");
        RegisterSettingsCloseAction();
    }

    private void BindInboxPopup()
    {
        ClearInboxBindings();
        if (inboxPopupRoot == null)
            return;

        inboxTitleLabel = inboxPopupRoot.Q<Label>("Title");
        inboxCloseAction = inboxPopupRoot.Q<Button>("CloseButton");
        RegisterInboxCloseAction();
    }

    private void BindBuildPlacementConfirmationBar()
    {
        ClearBuildPlacementConfirmationBarBindings();
        if (buildPlacementConfirmationBarRoot == null)
            return;

        buildPlacementTitleLabel = buildPlacementConfirmationBarRoot.Q<Label>("Title");
        buildPlacementStatusLabel = buildPlacementConfirmationBarRoot.Q<Label>("Status");
        buildPlacementCostLabel = buildPlacementConfirmationBarRoot.Q<Label>("Cost");
        buildPlacementDurationLabel = buildPlacementConfirmationBarRoot.Q<Label>("Duration");
        buildPlacementInstructionLabel = buildPlacementConfirmationBarRoot.Q<Label>("Instruction");
        buildPlacementCancelAction = buildPlacementConfirmationBarRoot.Q<Button>("CancelButton");
        buildPlacementRotateAction = buildPlacementConfirmationBarRoot.Q<Button>("RotateButton");
        buildPlacementConfirmAction = buildPlacementConfirmationBarRoot.Q<Button>("ConfirmButton");
        RegisterBuildPlacementAction(buildPlacementCancelAction, ref buildPlacementCancelActionCallback, UiActionKind.BuildPlacementCancel);
        RegisterBuildPlacementAction(buildPlacementRotateAction, ref buildPlacementRotateActionCallback, UiActionKind.BuildPlacementRotate);
        RegisterBuildPlacementAction(buildPlacementConfirmAction, ref buildPlacementConfirmActionCallback, UiActionKind.BuildPlacementConfirm);
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

    private void BindArmoryScreen()
    {
        ClearArmoryBindings();
        if (armoryContentRoot == null)
            return;

        armoryScrollView = armoryContentRoot.Q<ScrollView>("Scroll_View");
        armoryCatalogContent = armoryContentRoot.Q<VisualElement>("Content");
        armoryInspectionPanel = armoryContentRoot.Q<VisualElement>("InspectionPanel");
        armoryInspectionNameLabel = armoryInspectionPanel?.Q<Label>("Name");
        armoryInspectionTypeLabel = armoryInspectionPanel?.Q<Label>("Type");
        armoryInspectionPortraitArt = armoryInspectionPanel?.Q<VisualElement>("PortraitArt");
        armoryFilterAction = armoryContentRoot.Q<Button>("FilterDropdown");
        armorySortAction = armoryContentRoot.Q<Button>("SortDropdown");
        armoryUpgradeAction = armoryContentRoot.Q<Button>("UpgradeButton");
        armoryEquipAction = armoryContentRoot.Q<Button>("EquipButton");
        armoryCloseAction = armoryContentRoot.Q<Button>("CloseButton");
        armoryTabAction = armoryContentRoot.Q<Button>("ArmoryTab");
        armoryWorkshopTabAction = armoryContentRoot.Q<Button>("WorkshopTab");
        armoryDoctrineTabAction = armoryContentRoot.Q<Button>("DoctrineTab");
        armoryDepotTabAction = armoryContentRoot.Q<Button>("DepotTab");
        armoryOfficersTabAction = armoryContentRoot.Q<Button>("OfficersTab");

        CacheArmoryCategory(0, "Nav_Units", ArmoryCatalogCategory.Characters);
        CacheArmoryCategory(1, "Nav_Vehicles", ArmoryCatalogCategory.Vehicles);
        CacheArmoryCategory(2, "Nav_Aircraft", ArmoryCatalogCategory.Aircrafts);
        CacheArmoryCategory(3, "Nav_Buildings", ArmoryCatalogCategory.Buildings);
        CacheArmoryCategory(4, "Nav_Upgrades", ArmoryCatalogCategory.Support);

        CacheArmoryItem(0, "ItemView");
        CacheArmoryItem(1, "ItemView_FastApc");
        CacheArmoryItem(2, "ItemView_ReconDrone");
        CacheArmoryItem(3, "ItemView_BombSuit");
        CacheArmoryItem(4, "ItemView_HeavyTank");
        CacheArmoryItem(5, "ItemView_AttackHelicopter");
        CacheArmoryItem(6, "ItemView_RocketArtillery");
        CacheArmoryItem(7, "ItemView_SniperTeam");

        RegisterArmoryRouteAction(armoryCloseAction, ref armoryCloseCallback, UIRoute.MainMenu);
        RegisterArmoryRouteAction(armoryTabAction, ref armoryTabCallback, UIRoute.Armory);
        RegisterArmoryRouteAction(armoryWorkshopTabAction, ref armoryWorkshopTabCallback, UIRoute.CommandExchange);
        RegisterArmoryRouteAction(armoryDoctrineTabAction, ref armoryDoctrineTabCallback, UIRoute.Events);
        RegisterArmoryRouteAction(armoryDepotTabAction, ref armoryDepotTabCallback, UIRoute.LoadoutSquadPrep);
        RegisterArmoryRouteAction(armoryOfficersTabAction, ref armoryOfficersTabCallback, UIRoute.CommandFeed);
        RegisterArmoryNoopAction(armoryFilterAction, ref armoryFilterCallback);
        RegisterArmoryNoopAction(armorySortAction, ref armorySortCallback);
        RegisterArmoryNoopAction(armoryUpgradeAction, ref armoryUpgradeCallback);
        RegisterArmoryNoopAction(armoryEquipAction, ref armoryEquipCallback);

        ApplyArmoryCategory(UiShellRuntimeGateway.TryReadArmoryCategory(out ArmoryCatalogCategory category)
            ? category
            : ArmoryCatalogCategory.Characters);
        SelectArmoryItem(0);
    }

    private void BindCommanderProfileScreen()
    {
        ClearCommanderProfileBindings();
        if (commanderProfileContentRoot == null)
            return;

        commanderProfileBackAction = commanderProfileContentRoot.Q<Button>("BackButton");
        commanderProfileOverviewTabAction = commanderProfileContentRoot.Q<Button>("OverviewTab");
        commanderProfileStatsTabAction = commanderProfileContentRoot.Q<Button>("StatsTab");
        commanderProfileBadgesTabAction = commanderProfileContentRoot.Q<Button>("BadgesTab");
        commanderProfileHistoryTabAction = commanderProfileContentRoot.Q<Button>("HistoryTab");
        commanderProfileUpgradesTabAction = commanderProfileContentRoot.Q<Button>("UpgradesTab");
        commanderProfileOpenArmoryAction = commanderProfileContentRoot.Q<Button>("OpenArmoryButton");
        commanderProfileDetailAction = commanderProfileContentRoot.Q<Button>("DetailButton");
        commanderProfileReplayAction = commanderProfileContentRoot.Q<Button>("ReplayButton");
        commanderProfilePortrait = commanderProfileContentRoot.Q<VisualElement>("Portrait");
        commanderProfileBadge = commanderProfileContentRoot.Q<VisualElement>("Badge");
        VisualElement identityCard = commanderProfileContentRoot.Q<VisualElement>("IdentityCard");
        commanderProfileTitleLabel = identityCard?.Q<Label>("Title");
        commanderProfileNameLabel = identityCard?.Q<Label>("Name");
        commanderProfileSubtitleLabel = identityCard?.Q<Label>("Subtitle");
        commanderProfileLevelLabel = identityCard?.Q<Label>("Level");

        RegisterCommanderProfileRouteAction(
            commanderProfileBackAction,
            ref commanderProfileBackCallback,
            UiShellRouteIntent.BackMenuRoute,
            UIRoute.MainMenu,
            pushHistory: false);
        RegisterCommanderProfileRouteAction(
            commanderProfileOverviewTabAction,
            ref commanderProfileOverviewTabCallback,
            UiShellRouteIntent.OpenMenuRoute,
            UIRoute.CommandFeed,
            pushHistory: false);
        RegisterCommanderProfileRouteAction(
            commanderProfileOpenArmoryAction,
            ref commanderProfileOpenArmoryCallback,
            UiShellRouteIntent.OpenMenuRoute,
            UIRoute.Armory,
            pushHistory: true);
        RegisterCommanderProfileNoopAction(commanderProfileStatsTabAction, ref commanderProfileStatsTabCallback);
        RegisterCommanderProfileNoopAction(commanderProfileBadgesTabAction, ref commanderProfileBadgesTabCallback);
        RegisterCommanderProfileNoopAction(commanderProfileHistoryTabAction, ref commanderProfileHistoryTabCallback);
        RegisterCommanderProfileNoopAction(commanderProfileUpgradesTabAction, ref commanderProfileUpgradesTabCallback);
        RegisterCommanderProfileNoopAction(commanderProfileDetailAction, ref commanderProfileDetailCallback);
        RegisterCommanderProfileNoopAction(commanderProfileReplayAction, ref commanderProfileReplayCallback);

        ApplyCommanderProfile(UiShellRuntimeGateway.TryReadCommanderProfile(out UiShellCommanderProfileModel profile)
            ? profile
            : new UiShellCommanderProfileModel(
                DefaultCommanderName,
                DefaultCommanderSubtitle,
                DefaultCommanderPortraitClass));
    }

    private void CacheArmoryCategory(int index, string name, ArmoryCatalogCategory category)
    {
        if (index < 0 || index >= armoryCategoryActions.Length)
            return;

        Button target = armoryContentRoot?.Q<Button>(name);
        armoryCategoryActions[index] = target;
        if (target == null)
            return;

        armoryCategoryCallbacks[index] = evt =>
        {
            TrySubmitArmoryCategory(category);
            evt?.StopPropagation();
        };
        RegisterClick(target, armoryCategoryCallbacks[index]);
    }

    private void CacheArmoryItem(int index, string name)
    {
        if (index < 0 || index >= armoryItems.Length)
            return;

        Button item = armoryCatalogContent?.Q<Button>(name);
        armoryItems[index] = item;
        armoryItemTitleLabels[index] = item?.Q<Label>("Title");
        armoryItemStateLabels[index] = item?.Q<Label>("StateLabel");
        armoryItemLevelLabels[index] = item?.Q<Label>("Level");
        armoryItemTypeLabels[index] = item?.Q<Label>("Type");
        if (item == null)
            return;

        int itemIndex = index;
        armoryItemCallbacks[index] = evt =>
        {
            SelectArmoryItem(itemIndex);
            evt?.StopPropagation();
        };
        RegisterClick(item, armoryItemCallbacks[index]);
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
        matchHudMinimapMap = matchHudMinimapPanel?.Q<VisualElement>("Map");
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

    private void ClearArmoryBindings()
    {
        for (int i = 0; i < armoryCategoryActions.Length; i++)
        {
            UnregisterClick(armoryCategoryActions[i], armoryCategoryCallbacks[i]);
            armoryCategoryCallbacks[i] = null;
            armoryCategoryActions[i] = null;
        }

        for (int i = 0; i < armoryItems.Length; i++)
        {
            UnregisterClick(armoryItems[i], armoryItemCallbacks[i]);
            armoryItemCallbacks[i] = null;
            armoryItems[i] = null;
            armoryItemTitleLabels[i] = null;
            armoryItemStateLabels[i] = null;
            armoryItemLevelLabels[i] = null;
            armoryItemTypeLabels[i] = null;
        }

        UnregisterClick(armoryFilterAction, armoryFilterCallback);
        UnregisterClick(armorySortAction, armorySortCallback);
        UnregisterClick(armoryUpgradeAction, armoryUpgradeCallback);
        UnregisterClick(armoryEquipAction, armoryEquipCallback);
        UnregisterClick(armoryCloseAction, armoryCloseCallback);
        UnregisterClick(armoryTabAction, armoryTabCallback);
        UnregisterClick(armoryWorkshopTabAction, armoryWorkshopTabCallback);
        UnregisterClick(armoryDoctrineTabAction, armoryDoctrineTabCallback);
        UnregisterClick(armoryDepotTabAction, armoryDepotTabCallback);
        UnregisterClick(armoryOfficersTabAction, armoryOfficersTabCallback);

        armoryScrollView = null;
        armoryCatalogContent = null;
        armoryInspectionPanel = null;
        armoryInspectionNameLabel = null;
        armoryInspectionTypeLabel = null;
        armoryInspectionPortraitArt = null;
        armoryFilterAction = null;
        armorySortAction = null;
        armoryUpgradeAction = null;
        armoryEquipAction = null;
        armoryCloseAction = null;
        armoryTabAction = null;
        armoryWorkshopTabAction = null;
        armoryDoctrineTabAction = null;
        armoryDepotTabAction = null;
        armoryOfficersTabAction = null;
        armoryFilterCallback = null;
        armorySortCallback = null;
        armoryUpgradeCallback = null;
        armoryEquipCallback = null;
        armoryCloseCallback = null;
        armoryTabCallback = null;
        armoryWorkshopTabCallback = null;
        armoryDoctrineTabCallback = null;
        armoryDepotTabCallback = null;
        armoryOfficersTabCallback = null;
        selectedArmoryItemIndex = 0;
    }

    private void ClearCommanderProfileBindings()
    {
        UnregisterClick(commanderProfileBackAction, commanderProfileBackCallback);
        UnregisterClick(commanderProfileOverviewTabAction, commanderProfileOverviewTabCallback);
        UnregisterClick(commanderProfileStatsTabAction, commanderProfileStatsTabCallback);
        UnregisterClick(commanderProfileBadgesTabAction, commanderProfileBadgesTabCallback);
        UnregisterClick(commanderProfileHistoryTabAction, commanderProfileHistoryTabCallback);
        UnregisterClick(commanderProfileUpgradesTabAction, commanderProfileUpgradesTabCallback);
        UnregisterClick(commanderProfileOpenArmoryAction, commanderProfileOpenArmoryCallback);
        UnregisterClick(commanderProfileDetailAction, commanderProfileDetailCallback);
        UnregisterClick(commanderProfileReplayAction, commanderProfileReplayCallback);

        commanderProfileBackAction = null;
        commanderProfileOverviewTabAction = null;
        commanderProfileStatsTabAction = null;
        commanderProfileBadgesTabAction = null;
        commanderProfileHistoryTabAction = null;
        commanderProfileUpgradesTabAction = null;
        commanderProfileOpenArmoryAction = null;
        commanderProfileDetailAction = null;
        commanderProfileReplayAction = null;
        commanderProfilePortrait = null;
        commanderProfileBadge = null;
        commanderProfileTitleLabel = null;
        commanderProfileNameLabel = null;
        commanderProfileSubtitleLabel = null;
        commanderProfileLevelLabel = null;
        commanderProfileBackCallback = null;
        commanderProfileOverviewTabCallback = null;
        commanderProfileStatsTabCallback = null;
        commanderProfileBadgesTabCallback = null;
        commanderProfileHistoryTabCallback = null;
        commanderProfileUpgradesTabCallback = null;
        commanderProfileOpenArmoryCallback = null;
        commanderProfileDetailCallback = null;
        commanderProfileReplayCallback = null;
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
        matchHudMinimapMap = null;
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

    private void ClearBuildPlacementConfirmationBarBindings()
    {
        UnregisterClick(buildPlacementCancelAction, buildPlacementCancelActionCallback);
        UnregisterClick(buildPlacementRotateAction, buildPlacementRotateActionCallback);
        UnregisterClick(buildPlacementConfirmAction, buildPlacementConfirmActionCallback);
        buildPlacementCancelActionCallback = null;
        buildPlacementRotateActionCallback = null;
        buildPlacementConfirmActionCallback = null;
        buildPlacementTitleLabel = null;
        buildPlacementStatusLabel = null;
        buildPlacementCostLabel = null;
        buildPlacementDurationLabel = null;
        buildPlacementInstructionLabel = null;
        buildPlacementCancelAction = null;
        buildPlacementRotateAction = null;
        buildPlacementConfirmAction = null;
    }

    private void ClearMissionResultBindings()
    {
        UnregisterClick(missionResultContinueAction, missionResultContinueCallback);
        UnregisterClick(missionResultReplayAction, missionResultReplayCallback);
        missionResultContinueCallback = null;
        missionResultReplayCallback = null;
        missionResultTitleLabel = null;
        missionResultSubtitleLabel = null;
        missionResultSummaryBodyLabel = null;
        missionResultBadge = null;
        missionResultContinueAction = null;
        missionResultReplayAction = null;
    }

    private void ClearSettingsBindings()
    {
        UnregisterClick(settingsCloseAction, settingsCloseCallback);
        settingsCloseCallback = null;
        settingsTitleLabel = null;
        settingsCloseAction = null;
    }

    private void ClearInboxBindings()
    {
        UnregisterClick(inboxCloseAction, inboxCloseCallback);
        inboxCloseCallback = null;
        inboxTitleLabel = null;
        inboxCloseAction = null;
    }

    private void ClearDiagnosticsBindings()
    {
        UnregisterClick(diagnosticsFpsAction, diagnosticsFpsCallback);
        UnregisterClick(diagnosticsCloseAction, diagnosticsCloseCallback);
        diagnosticsFpsCallback = null;
        diagnosticsCloseCallback = null;
        diagnosticsFpsAction = null;
        diagnosticsCloseAction = null;
        diagnosticsFpsValueLabel = null;
        diagnosticsLogTextLabel = null;
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

    private void RegisterBuildPlacementAction(
        Button target,
        ref EventCallback<ClickEvent> callback,
        UiActionKind kind)
    {
        if (target == null)
            return;

        callback = evt =>
        {
            if (target.enabledInHierarchy)
                TrySubmitMatchHudAction(kind);
            evt?.StopPropagation();
        };
        RegisterClick(target, callback);
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

    private void RegisterMissionResultContinueAction()
    {
        if (missionResultContinueAction == null)
            return;

        missionResultContinueCallback = evt =>
        {
            TrySubmitMissionResultAction("ContinueButton");
            evt?.StopPropagation();
        };
        RegisterClick(missionResultContinueAction, missionResultContinueCallback);
    }

    private void RegisterMissionResultReplayAction()
    {
        if (missionResultReplayAction == null)
            return;

        missionResultReplayCallback = evt =>
        {
            TrySubmitMissionResultAction("ReplayButton");
            evt?.StopPropagation();
        };
        RegisterClick(missionResultReplayAction, missionResultReplayCallback);
    }

    private void RegisterSettingsCloseAction()
    {
        if (settingsCloseAction == null)
            return;

        settingsCloseCallback = evt =>
        {
            TrySubmitSettingsAction("CloseButton");
            evt?.StopPropagation();
        };
        RegisterClick(settingsCloseAction, settingsCloseCallback);
    }

    private void RegisterInboxCloseAction()
    {
        if (inboxCloseAction == null)
            return;

        inboxCloseCallback = evt =>
        {
            TrySubmitInboxAction("CloseButton");
            evt?.StopPropagation();
        };
        RegisterClick(inboxCloseAction, inboxCloseCallback);
    }

    private void RegisterDiagnosticsFpsAction()
    {
        if (diagnosticsFpsAction == null)
            return;

        diagnosticsFpsCallback = evt =>
        {
            TrySubmitMatchHudAction(UiActionKind.ToggleDiagnosticsOverlay);
            evt?.StopPropagation();
        };
        RegisterClick(diagnosticsFpsAction, diagnosticsFpsCallback);
    }

    private void RegisterDiagnosticsCloseAction()
    {
        if (diagnosticsCloseAction == null)
            return;

        diagnosticsCloseCallback = evt =>
        {
            TrySubmitMatchHudAction(UiActionKind.CloseDiagnosticsOverlay);
            evt?.StopPropagation();
        };
        RegisterClick(diagnosticsCloseAction, diagnosticsCloseCallback);
    }

    private void RegisterArmoryRouteAction(
        Button target,
        ref EventCallback<ClickEvent> callback,
        UIRoute route)
    {
        if (target == null)
            return;

        callback = evt =>
        {
            EnqueueMainMenuRoute(UiShellRouteIntent.OpenMenuRoute, route, pushHistory: route != UIRoute.MainMenu);
            evt?.StopPropagation();
        };
        RegisterClick(target, callback);
    }

    private void RegisterCommanderProfileRouteAction(
        Button target,
        ref EventCallback<ClickEvent> callback,
        UiShellRouteIntent intent,
        UIRoute route,
        bool pushHistory)
    {
        if (target == null)
            return;

        callback = evt =>
        {
            if (target.name == "BackButton" ||
                target.name == "OverviewTab" ||
                target.name == "OpenArmoryButton")
            {
                TrySubmitCommanderProfileAction(target.name);
            }
            else
            {
                EnqueueMainMenuRoute(intent, route, pushHistory);
            }
            evt?.StopPropagation();
        };
        RegisterClick(target, callback);
    }

    private void RegisterCommanderProfileNoopAction(Button target, ref EventCallback<ClickEvent> callback)
    {
        if (target == null)
            return;

        callback = evt => evt?.StopPropagation();
        RegisterClick(target, callback);
    }

    private void RegisterArmoryNoopAction(Button target, ref EventCallback<ClickEvent> callback)
    {
        if (target == null)
            return;

        callback = evt => evt?.StopPropagation();
        RegisterClick(target, callback);
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
