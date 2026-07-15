using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using Game.Tactical.Contracts;
using Game.UI.Contracts;

namespace Game.UI.Runtime
{
    [DisallowMultipleComponent]
    public sealed class UIShellContentView : MonoBehaviour
    {
        [SerializeField] private UIShellView shellView;
        [SerializeField] private GameObject loadingContentPrefab;
        [SerializeField] private GameObject mainMenuContentPrefab;
        [SerializeField] private GameObject commanderProfileContentPrefab;
        [SerializeField] private GameObject skirmishSetupContentPrefab;
        [SerializeField] private GameObject campaignContentPrefab;
        [SerializeField] private GameObject missionBriefingContentPrefab;
        [SerializeField] private GameObject operationsContentPrefab;
        [SerializeField] private GameObject armoryContentPrefab;
        [SerializeField] private GameObject matchHudContentPrefab;
        [SerializeField] private GameObject buildDrawerPopupPrefab;
        [SerializeField] private GameObject fullMapPopupPrefab;
        [SerializeField] private GameObject settingsPopupPrefab;
        [SerializeField] private GameObject resourceExchangePopupPrefab;
        [SerializeField] private GameObject ariaCommandAssistantPopupPrefab;
        [SerializeField] private GameObject buildPlacementConfirmationBarPrefab;
        private readonly MatchOverlayCommandInputUiSystemHelper _matchOverlayCommandInputSystem = new();
        private ISelectionUiCommand _selectionUiCommandSystem;
        private ISelectionUiReadModel _selectionUiReadModelSystem;
        private IBuildingUiCommand _buildingUiCommandSystem;
        private IBuildingUiQuery _buildingUiQuerySystem;
        private IGameTextResolver _gameTextResolver = FallbackGameTextResolver.Instance;
        private IQuickCustomGameConfigStore _quickCustomGameConfigStore;
        private IMatchLaunchCommand _matchLaunchCommand;
        private ISelectionDiagnosticsSink _selectionDiagnosticsSink;
        private MainMenuPlayUI _mainMenuPlayUi;
        private System.Action<IMatchHudSelectionPanelView> _bindMatchHudSelectionPanel;
        private MatchHudSelectionPanelView _matchHudSelectionPanelView;
        private MatchOverlayCommandControlsView _matchHudCommandControlsView;
        private MatchHudFooterContentView _matchHudFooterContentView;
        private MatchHudRightQuickRailView _rightQuickRailView;
        private GameObject _matchHudHeaderContent;
        private MatchHudCurrentOrderBannerView _matchHudCurrentOrderBannerView;
        private BuildPlacementConfirmationBarView _buildPlacementConfirmationBarView;
        private ArmoryContentListView _armoryContentListView;
        private TryResolveUiBuildingCatalogMetadata _tryResolveBuildingCatalogMetadata;
        private TryResolveUiUnitCatalogMetadata _tryResolveUnitCatalogMetadata;
        private Button _rightQuickRailBuildButton;
        private Button _buildDrawerPopupCloseButton;
        private UnityAction _buildDrawerPopupCloseButtonListener;
        private GameObject _buildDrawerPopupInstance;
        private GameObject _fullMapPopupInstance;
        private GameObject _settingsPopupInstance;
        private GameObject _resourceExchangePopupInstance;
        private SettingsPopupView _settingsPopupView;
        private ResourceExchangePopupView _resourceExchangePopupView;
        private Button _resourceExchangePopupCloseButton;
        private UnityAction _resourceExchangePopupCloseButtonListener;
        private MatchHudFullMapPopupView _fullMapPopupView;
        private int _contentVersion;

        public UIShellView ShellView => shellView;
        public GameObject LoadingContentPrefab => loadingContentPrefab;
        public GameObject MainMenuContentPrefab => mainMenuContentPrefab;
        public GameObject CommanderProfileContentPrefab => commanderProfileContentPrefab;
        public GameObject SkirmishSetupContentPrefab => skirmishSetupContentPrefab;
        public GameObject CampaignContentPrefab => campaignContentPrefab;
        public GameObject MissionBriefingContentPrefab => missionBriefingContentPrefab;
        public GameObject OperationsContentPrefab => operationsContentPrefab;
        public GameObject ArmoryContentPrefab => armoryContentPrefab;
        public GameObject MatchHudContentPrefab => matchHudContentPrefab;
        public GameObject BuildDrawerPopupPrefab => buildDrawerPopupPrefab;
        public GameObject FullMapPopupPrefab => fullMapPopupPrefab;
        public GameObject SettingsPopupPrefab => settingsPopupPrefab;
        public GameObject ResourceExchangePopupPrefab => resourceExchangePopupPrefab;
        public GameObject AriaCommandAssistantPopupPrefab => ariaCommandAssistantPopupPrefab;
        public GameObject BuildPlacementConfirmationBarPrefab => buildPlacementConfirmationBarPrefab;
        public int ContentVersion => _contentVersion;
        internal IGameTextResolver GameTextResolver => _gameTextResolver;

        public void Configure(
            UIShellView view,
            GameObject loadingPrefab,
            GameObject mainMenuPrefab,
            GameObject armoryPrefab,
            GameObject matchHudPrefab,
            GameObject buildDrawerPrefab,
            GameObject fullMapPrefab = null,
            GameObject buildPlacementConfirmationPrefab = null,
            GameObject settingsPrefab = null,
            GameObject resourceExchangePrefab = null,
            GameObject ariaCommandAssistantPrefab = null,
            GameObject commanderProfilePrefab = null,
            GameObject skirmishSetupPrefab = null,
            GameObject campaignPrefab = null,
            GameObject missionBriefingPrefab = null,
            GameObject operationsPrefab = null)
        {
            shellView = view;
            loadingContentPrefab = loadingPrefab;
            mainMenuContentPrefab = mainMenuPrefab;
            armoryContentPrefab = armoryPrefab;
            matchHudContentPrefab = matchHudPrefab;
            buildDrawerPopupPrefab = buildDrawerPrefab;
            if (commanderProfilePrefab != null)
                commanderProfileContentPrefab = commanderProfilePrefab;
            if (skirmishSetupPrefab != null)
                skirmishSetupContentPrefab = skirmishSetupPrefab;
            if (campaignPrefab != null)
                campaignContentPrefab = campaignPrefab;
            if (missionBriefingPrefab != null)
                missionBriefingContentPrefab = missionBriefingPrefab;
            if (operationsPrefab != null)
                operationsContentPrefab = operationsPrefab;
            if (fullMapPrefab != null)
                fullMapPopupPrefab = fullMapPrefab;
            if (buildPlacementConfirmationPrefab != null)
                buildPlacementConfirmationBarPrefab = buildPlacementConfirmationPrefab;
            if (settingsPrefab != null)
                settingsPopupPrefab = settingsPrefab;
            if (resourceExchangePrefab != null)
                resourceExchangePopupPrefab = resourceExchangePrefab;
            if (ariaCommandAssistantPrefab != null)
                ariaCommandAssistantPopupPrefab = ariaCommandAssistantPrefab;
        }

        public void BindGameTextResolver(IGameTextResolver gameTextResolver)
        {
            _gameTextResolver = gameTextResolver ?? FallbackGameTextResolver.Instance;
        }

        public void PrepareForCommandSequence(IReadOnlyList<UiShellPresentationCommandModel> commands)
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
                        CommanderProfileRouteLifecyclePresentation.InstallMainMenu(this);
                        break;
                    case UiShellCommandKind.EnterMatchHud:
                        InstallMatchHud();
                        break;
                    case UiShellCommandKind.ShowPopup:
                        InstallPopup(commands[i]);
                        break;
                }
            }
        }

        public void BindGameplayRuntimeDependencies(
            ISelectionUiCommand selectionUiCommandSystem,
            MainMenuPlayUI mainMenuPlayUi = null,
            System.Action<IMatchHudSelectionPanelView> bindMatchHudSelectionPanel = null,
            IBuildingUiCommand buildingUiCommandSystem = null,
            ISelectionDiagnosticsSink selectionDiagnosticsSink = null,
            ISelectionUiReadModel selectionUiReadModelSystem = null)
        {
            UnbindFullMapPopupRequests();
            _mainMenuPlayUi?.ConfigureLargeTacticalPopupCloseActions(null, null, null);
            _selectionUiCommandSystem = selectionUiCommandSystem;
            _selectionUiReadModelSystem = selectionUiReadModelSystem;
            _buildingUiCommandSystem = buildingUiCommandSystem;
            _selectionDiagnosticsSink = selectionDiagnosticsSink;
            _mainMenuPlayUi = mainMenuPlayUi;
            _bindMatchHudSelectionPanel = bindMatchHudSelectionPanel;
            BindFullMapPopupRequests();
            _mainMenuPlayUi?.ConfigureLargeTacticalPopupCloseActions(
                CloseBuildDrawerPopup,
                CloseFullMapPopup,
                RequestCloseResourceExchangePopup);
            _mainMenuPlayUi?.BindMatchHudThreatJumpPanel(_matchHudHeaderContent);
            BindMatchHudAssistant();
            BindMatchHudSelectionPanel(_matchHudSelectionPanelView);
            BindMatchHudFooter(_matchHudFooterContentView);
            BindMatchHudRightQuickRail(_rightQuickRailView);
            BindBuildPlacementConfirmationBarInRegion();
        }

        public void RefreshMatchHudCommandControlState()
        {
            _matchOverlayCommandInputSystem.RefreshCommandControlState(_selectionUiReadModelSystem);
        }

        private void Update()
        {
            RefreshMatchHudCommandControlState();
        }

        public void BindBuildDrawerRuntimeQueries(IBuildingUiQuery buildingUiQuerySystem)
        {
            _buildingUiQuerySystem = buildingUiQuerySystem;
            BindBuildDrawerRuntimeCommands(_buildDrawerPopupInstance);
        }

        public void BindQuickCustomRuntimeDependencies(
            IQuickCustomGameConfigStore configStore,
            IMatchLaunchCommand launchCommand)
        {
            _quickCustomGameConfigStore = configStore;
            _matchLaunchCommand = launchCommand;
            BindQuickCustomScreens(shellView != null ? shellView.gameObject : null);
            BindGameStartButtons(shellView != null ? shellView.gameObject : null);
        }

        public void ConfigureCatalogMetadataResolvers(
            TryResolveUiBuildingCatalogMetadata tryResolveBuildingMetadata,
            TryResolveUiUnitCatalogMetadata tryResolveUnitMetadata)
        {
            _tryResolveBuildingCatalogMetadata = tryResolveBuildingMetadata;
            _tryResolveUnitCatalogMetadata = tryResolveUnitMetadata;
            BindArmoryCatalogMetadataResolvers(_armoryContentListView);
            BindBuildDrawerCatalogMetadataResolvers(_buildDrawerPopupInstance);
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
                ResolveMatchHudRuntimeFeedback(),
                _gameTextResolver);
            _mainMenuPlayUi?.BindMatchHudRightQuickRail(view);
            return true;
        }

        private void InstallLoading()
        {
            UnbindMatchHudThreatWarningHeader();
            ClearRegion(UIShellRegionId.MenuBackgroundRegion);
            InstallRoot(loadingContentPrefab, UIShellRegionId.LoadingLayer);
        }

        public void InstallMenuRouteBody(UIRoute route) => MenuOverlayRoutePresentation.Install(this, route);

        internal void InstallArmoryBody()
        {
            UnbindMatchHudThreatWarningHeader();
            InstallSection(armoryContentPrefab, UIShellContentSectionId.MenuBackground, UIShellRegionId.MenuBackgroundRegion);
            InstallSection(armoryContentPrefab, UIShellContentSectionId.Header, UIShellRegionId.HeaderRegion);
            InstallSection(armoryContentPrefab, UIShellContentSectionId.Left, UIShellRegionId.LeftRegion);
            GameObject middle = InstallSection(armoryContentPrefab, UIShellContentSectionId.Middle, UIShellRegionId.MiddleRegion);
            GameObject right = InstallSection(armoryContentPrefab, UIShellContentSectionId.Right, UIShellRegionId.RightRegion);
            WireArmorySections(middle, right);
            InstallSection(armoryContentPrefab, UIShellContentSectionId.Footer, UIShellRegionId.FooterRegion);
            ClearRegion(UIShellRegionId.PopupLayer);
        }

        private void WireArmorySections(GameObject middle, GameObject right)
        {
            if (middle == null || right == null)
                return;

            ArmoryContentListView listView = middle.GetComponent<ArmoryContentListView>();
            ArmoryRightContentView rightView = right.GetComponent<ArmoryRightContentView>();
            if (listView == null || rightView == null)
                return;

            _armoryContentListView = listView;
            BindArmoryCatalogMetadataResolvers(listView);
            listView.SetInspectionPanel(rightView.InspectionPanel);
        }

        internal void BindQuickCustomScreens(params GameObject[] roots)
        {
            if (roots == null)
                return;

            for (int i = 0; i < roots.Length; i++)
            {
                GameObject root = roots[i];
                if (root == null)
                    continue;

                QuickCustomScreenView[] views = root.GetComponentsInChildren<QuickCustomScreenView>(true);
                for (int j = 0; j < views.Length; j++)
                    views[j]?.BindRuntimeDependencies(_quickCustomGameConfigStore, _matchLaunchCommand);
            }
        }

        internal void BindGameStartButtons(params GameObject[] roots)
        {
            if (roots == null)
                return;

            for (int i = 0; i < roots.Length; i++)
            {
                GameObject root = roots[i];
                if (root == null)
                    continue;

                UIGameStartButtonView[] views = root.GetComponentsInChildren<UIGameStartButtonView>(true);
                for (int j = 0; j < views.Length; j++)
                    views[j]?.BindMatchLaunchCommand(_matchLaunchCommand);
            }
        }

        private void InstallMatchHud()
        {
            ClearRegion(UIShellRegionId.LoadingLayer);
            ClearRegion(UIShellRegionId.MenuBackgroundRegion);
            _matchHudHeaderContent = InstallSection(matchHudContentPrefab, UIShellContentSectionId.Header, UIShellRegionId.HeaderRegion);
            _matchHudCurrentOrderBannerView = _matchHudHeaderContent != null
                ? _matchHudHeaderContent.GetComponent<MatchHudCurrentOrderBannerView>()
                : null;
            _mainMenuPlayUi?.BindMatchHudThreatJumpPanel(_matchHudHeaderContent);
            BindMatchHudAssistant();
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
            if (footer != null && footer.RuntimeFeedback != null)
                footer.RuntimeFeedback.BindCurrentOrderBanner(_matchHudCurrentOrderBannerView);
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
                BattleHudRuntimeFeedbackUiSystemHelper.ApplyStickyCommandMode(ResolveMatchHudRuntimeFeedback(), TacticalCommandMode.Build, _gameTextResolver);
                return;
            }

            GameObject popup = InstallBuildDrawerPopup();
            if (popup == null)
            {
                BattleHudRuntimeFeedbackUiSystemHelper.ApplyCommandResult(ResolveMatchHudRuntimeFeedback(), TacticalCommandResult.Rejected(
                    TacticalCommandReasonCode.BuildUnavailable,
                    _gameTextResolver.Get("build.feedback.drawer_not_ready", "Build drawer is not ready.")), _gameTextResolver);
                return;
            }

            BattleHudRuntimeFeedbackUiSystemHelper.ApplyStickyCommandMode(ResolveMatchHudRuntimeFeedback(), TacticalCommandMode.Build, _gameTextResolver);
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
                    CloseBuildDrawerPopup,
                    _selectionDiagnosticsSink,
                    _selectionUiReadModelSystem,
                    _mainMenuPlayUi != null ? _mainMenuPlayUi.CaptureGameplayUiClickSequence : null,
                    _gameTextResolver);
                _mainMenuPlayUi?.BindMatchHudCommandControls(view);
                RefreshMatchHudCommandControlState();
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
                ResolveMatchHudRuntimeFeedback(),
                _gameTextResolver);
            _mainMenuPlayUi?.BindBuildPlacementConfirmationBar(_buildPlacementConfirmationBarView);
        }

        public GameObject InstallBuildDrawerPopup()
        {
            _mainMenuPlayUi?.PrepareToOpenLargeTacticalPopup(MatchHudLargeTacticalPopup.BuildDrawer);
            UnbindBuildDrawerPopupCloseButton();
            _buildDrawerPopupInstance = InstallRoot(buildDrawerPopupPrefab, UIShellRegionId.PopupLayer);
            BindBuildDrawerPopupInputBlocker(_buildDrawerPopupInstance);
            BindBuildDrawerPopupCloseButton(_buildDrawerPopupInstance);
            BindBuildDrawerRuntimeCommands(_buildDrawerPopupInstance);
            return _buildDrawerPopupInstance;
        }

        private void InstallPopup(UiShellPresentationCommandModel command)
        {
            switch (command.PopupKind)
            {
                case UiShellPopupKind.BuildDrawer:
                    InstallBuildDrawerPopup();
                    break;
                case UiShellPopupKind.Settings:
                    InstallSettingsPopup(command.Route);
                    break;
                case UiShellPopupKind.ResourceExchange:
                    InstallResourceExchangePopup();
                    break;
            }
        }

        public GameObject InstallSettingsPopup(UIRoute activeRoute)
        {
            _settingsPopupInstance = InstallRoot(settingsPopupPrefab, UIShellRegionId.PopupLayer);
            _settingsPopupView = _settingsPopupInstance != null
                ? _settingsPopupInstance.GetComponent<SettingsPopupView>()
                : null;
            if (_settingsPopupView != null)
            {
                _settingsPopupView.ConfigureContext(activeRoute == UIRoute.Match
                    ? SettingsPopupContext.Match
                    : SettingsPopupContext.Menu);
                _settingsPopupView.BindClose(CloseSettingsPopup);
            }

            return _settingsPopupInstance;
        }

        public GameObject InstallResourceExchangePopup()
        {
            _mainMenuPlayUi?.PrepareToOpenLargeTacticalPopup(MatchHudLargeTacticalPopup.ResourceExchange);
            UnbindResourceExchangePopupCloseButton();
            _resourceExchangePopupInstance = InstallRoot(resourceExchangePopupPrefab, UIShellRegionId.PopupLayer);
            _resourceExchangePopupView = _resourceExchangePopupInstance != null
                ? _resourceExchangePopupInstance.GetComponent<ResourceExchangePopupView>()
                : null;
            _mainMenuPlayUi?.BindResourceExchangePopup(_resourceExchangePopupView);
            BindResourceExchangePopupCloseButton(_resourceExchangePopupView);
            return _resourceExchangePopupInstance;
        }

        private void OpenFullMapPopup()
        {
            _selectionUiCommandSystem?.CaptureUiClickSequence();
            if (_fullMapPopupInstance != null)
            {
                _mainMenuPlayUi?.BindMatchHudFullMapPopup(_fullMapPopupView);
                return;
            }

            _mainMenuPlayUi?.PrepareToOpenLargeTacticalPopup(MatchHudLargeTacticalPopup.FullMap);
            _fullMapPopupInstance = InstallRoot(fullMapPopupPrefab, UIShellRegionId.PopupLayer);
            _fullMapPopupView = _fullMapPopupInstance != null
                ? _fullMapPopupInstance.GetComponent<MatchHudFullMapPopupView>()
                : null;
            if (_fullMapPopupView == null)
            {
                BattleHudRuntimeFeedbackUiSystemHelper.ApplyCommandResult(ResolveMatchHudRuntimeFeedback(), TacticalCommandResult.Rejected(
                    TacticalCommandReasonCode.CommandUnavailable,
                    _gameTextResolver.Get("match.feedback.tactical_map_not_ready", "Tactical map is not ready.")), _gameTextResolver);
                return;
            }

            _mainMenuPlayUi?.BindMatchHudFullMapPopup(_fullMapPopupView);
        }

        private void CloseFullMapPopup()
        {
            _mainMenuPlayUi?.BindMatchHudFullMapPopup(null);
            GameObject popup = _fullMapPopupInstance;
            _fullMapPopupInstance = null;
            _fullMapPopupView = null;

            if (popup == null)
                return;

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

        public void CloseBuildDrawerPopup()
        {
            UnbindBuildDrawerPopupCloseButton();
            _mainMenuPlayUi?.BindBuildDrawer(null);
            GameObject popup = _buildDrawerPopupInstance;
            _buildDrawerPopupInstance = null;

            bool hasActivePlacement = _buildingUiCommandSystem != null &&
                                      _buildingUiCommandSystem.HasPendingBuildingPlacement;
            if (!hasActivePlacement)
                BattleHudRuntimeFeedbackUiSystemHelper.ClearStickyCommandMode(ResolveMatchHudRuntimeFeedback(), TacticalCommandMode.Build);

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

        public void CloseSettingsPopup()
        {
            GameObject popup = _settingsPopupInstance;
            _settingsPopupInstance = null;
            _settingsPopupView = null;

            if (popup == null)
                return;

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

        public void CloseResourceExchangePopup()
        {
            UnbindResourceExchangePopupCloseButton();
            _mainMenuPlayUi?.BindResourceExchangePopup(null);
            GameObject popup = _resourceExchangePopupInstance;
            _resourceExchangePopupInstance = null;
            _resourceExchangePopupView = null;

            if (popup == null)
                return;

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

        private void BindResourceExchangePopupCloseButton(ResourceExchangePopupView view)
        {
            if (view == null || view.CloseButton == null)
                return;

            _resourceExchangePopupCloseButton = view.CloseButton;
            _resourceExchangePopupCloseButtonListener = RequestCloseResourceExchangePopup;
            _resourceExchangePopupCloseButton.onClick.RemoveListener(_resourceExchangePopupCloseButtonListener);
            _resourceExchangePopupCloseButton.onClick.AddListener(_resourceExchangePopupCloseButtonListener);
        }

        private void RequestCloseResourceExchangePopup()
        {
            _mainMenuPlayUi?.CaptureGameplayUiClickSequence();
            UiShellRuntimeGateway.TryEnqueueUiAction(UiActionKind.CloseResourceExchange, 0);
            CloseResourceExchangePopup();
        }

        private void BindBuildDrawerRuntimeCommands(GameObject popup)
        {
            if (popup == null)
                return;

            BuildDrawerCatalogRuntimeView presenter = popup.GetComponent<BuildDrawerCatalogRuntimeView>();
            if (presenter == null)
                return;

            presenter.ConfigureCatalogMetadataResolvers(
                _tryResolveBuildingCatalogMetadata,
                _tryResolveUnitCatalogMetadata);
            presenter.BindRuntimeCommands(
                _buildingUiCommandSystem,
                CloseBuildDrawerPopup,
                ResolveMatchHudRuntimeFeedback(),
                _gameTextResolver);
            presenter.BindRuntimeQueries(_buildingUiQuerySystem);
        }

        private void BindArmoryCatalogMetadataResolvers(ArmoryContentListView listView)
        {
            listView?.ConfigureCatalogMetadataResolvers(
                _tryResolveBuildingCatalogMetadata,
                _tryResolveUnitCatalogMetadata);
        }

        private void BindBuildDrawerCatalogMetadataResolvers(GameObject popup)
        {
            if (popup == null)
                return;

            BuildDrawerCatalogRuntimeView presenter = popup.GetComponent<BuildDrawerCatalogRuntimeView>();
            presenter?.ConfigureCatalogMetadataResolvers(
                _tryResolveBuildingCatalogMetadata,
                _tryResolveUnitCatalogMetadata);
        }

        private void UnbindBuildDrawerPopupCloseButton()
        {
            if (_buildDrawerPopupCloseButton != null && _buildDrawerPopupCloseButtonListener != null)
                _buildDrawerPopupCloseButton.onClick.RemoveListener(_buildDrawerPopupCloseButtonListener);

            _buildDrawerPopupCloseButton = null;
            _buildDrawerPopupCloseButtonListener = null;
        }

        private void UnbindResourceExchangePopupCloseButton()
        {
            if (_resourceExchangePopupCloseButton != null && _resourceExchangePopupCloseButtonListener != null)
                _resourceExchangePopupCloseButton.onClick.RemoveListener(_resourceExchangePopupCloseButtonListener);

            _resourceExchangePopupCloseButton = null;
            _resourceExchangePopupCloseButtonListener = null;
        }

        internal GameObject InstallRoot(GameObject prefab, UIShellRegionId regionId)
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

        internal GameObject InstallSection(GameObject prefab, UIShellContentSectionId sectionId, UIShellRegionId regionId)
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

        internal void ClearRegion(UIShellRegionId regionId)
        {
            if (regionId == UIShellRegionId.RightRegion)
                UnbindRightQuickRailBuildButton();
            else if (regionId == UIShellRegionId.HeaderRegion)
            {
                UnbindMatchHudThreatWarningHeader();
                _matchHudHeaderContent = null;
                _matchHudCurrentOrderBannerView = null;
            }
            else if (regionId == UIShellRegionId.LeftRegion)
            {
                _matchHudSelectionPanelView = null;
                _mainMenuPlayUi?.BindMatchHudSelectionPanel(null);
                _bindMatchHudSelectionPanel?.Invoke(null);
            }
            else if (regionId == UIShellRegionId.PopupLayer)
            {
                UnbindBuildDrawerPopupCloseButton();
                UnbindResourceExchangePopupCloseButton();
                _mainMenuPlayUi?.BindBuildDrawer(null);
                _mainMenuPlayUi?.BindMatchHudFullMapPopup(null);
                _mainMenuPlayUi?.BindResourceExchangePopup(null);
                _fullMapPopupInstance = null;
                _fullMapPopupView = null;
                _settingsPopupInstance = null;
                _settingsPopupView = null;
                _resourceExchangePopupInstance = null;
                _resourceExchangePopupView = null;
            }

            if (TryGetRegionContentRoot(regionId, out RectTransform contentRoot))
            {
                ClearChildren(contentRoot);
                MarkContentChanged();
            }

            if (regionId == UIShellRegionId.PopupLayer)
            {
                _buildDrawerPopupInstance = null;
                _fullMapPopupInstance = null;
                _fullMapPopupView = null;
                _settingsPopupInstance = null;
                _settingsPopupView = null;
                _resourceExchangePopupInstance = null;
                _resourceExchangePopupView = null;
            }
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

        internal void MarkContentChanged()
        {
            unchecked
            {
                _contentVersion++;
            }
        }

        internal void UnbindMatchHudThreatWarningHeader()
        {
            _matchHudHeaderContent = null;
            _mainMenuPlayUi?.BindMatchHudThreatJumpPanel(null);
            _mainMenuPlayUi?.BindMatchHudAssistant(null, null, null);
        }

        private void BindMatchHudAssistant()
        {
            RectTransform popupLayer = null;
            TryGetRegionContentRoot(UIShellRegionId.PopupLayer, out popupLayer);
            _mainMenuPlayUi?.BindMatchHudAssistant(
                _matchHudHeaderContent,
                popupLayer,
                ariaCommandAssistantPopupPrefab);
        }

        private void BindFullMapPopupRequests()
        {
            if (_mainMenuPlayUi == null)
                return;

            _mainMenuPlayUi.FullMapPopupRequested -= OpenFullMapPopup;
            _mainMenuPlayUi.FullMapPopupCloseRequested -= CloseFullMapPopup;
            _mainMenuPlayUi.FullMapPopupRequested += OpenFullMapPopup;
            _mainMenuPlayUi.FullMapPopupCloseRequested += CloseFullMapPopup;
        }

        private void UnbindFullMapPopupRequests()
        {
            if (_mainMenuPlayUi == null)
                return;

            _mainMenuPlayUi.FullMapPopupRequested -= OpenFullMapPopup;
            _mainMenuPlayUi.FullMapPopupCloseRequested -= CloseFullMapPopup;
        }

        internal bool TryGetRegionContentRoot(UIShellRegionId regionId, out RectTransform contentRoot)
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

        internal static void DestroyRegionObject(UnityEngine.Object target)
        {
            if (target == null)
                return;

            if (Application.isPlaying)
                Destroy(target);
            else
                DestroyImmediate(target);
        }

        internal static void Stretch(RectTransform rect)
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
}
