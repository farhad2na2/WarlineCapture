using System;
using System.Collections.Generic;
using Game.Catalog.Contracts;
using Game.Tactical.Contracts;
using Game.UI.Contracts;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Game.UI.Runtime
{
    [DisallowMultipleComponent]
    public sealed class BuildDrawerCatalogRuntimeView : MonoBehaviour
    {
        private const float QueueRefreshIntervalSeconds = 0.2f;

        [SerializeField] private BuildDrawerView view;
        [SerializeField] private ScriptableObject unitPrefabRegistryConfig;
        [SerializeField] private ScriptableObject buildingPlacementConfig;

        private readonly BuildDrawerCatalogQueryUiSystemHelper _query = new();
        private readonly List<BuildDrawerCatalogItem> _items = new();
        private readonly List<BuildDrawerCatalogItem> _countScratch = new();
        private readonly List<BuildingPendingProductionUiEntry> _pendingProductions = new();
        private readonly List<BuildingPendingProductionUiEntry> _clearProductionScratch = new();
        private readonly List<BuildDrawerItemView> _runtimeItems = new();
        private readonly List<BuildDrawerQueueItemView> _runtimeQueueItems = new();
        private readonly List<BuildDrawerCatalogPresentationSystemHelper.ButtonBinding> _tabBindings = new();
        private readonly List<BuildDrawerCatalogPresentationSystemHelper.ButtonBinding> _itemBindings = new();
        private BuildDrawerCategory _activeCategory = BuildDrawerCategory.Buildings;
        private BuildDrawerItemView _selectedItemView;
        private BuildDrawerCatalogItem _selectedItem;
        private bool _hasSelectedItem;
        private IBuildingUiCommand _uiCommandSystem;
        private IBuildingUiQuery _uiQuerySystem;
        private IGameTextResolver _gameTextResolver = FallbackGameTextResolver.Instance;
        private BattleHudRuntimeFeedbackView _runtimeFeedbackView;
        private Action _closeDrawer;
        private Button _primaryActionButton;
        private UnityAction _primaryActionListener;
        private Button _cancelButton;
        private UnityAction _cancelButtonListener;
        private Button _clearButton;
        private UnityAction _clearButtonListener;
        private float _nextQueueRefreshTime;
        private ICatalogPrefabSource _unitPrefabSourceOverride;
        private ICatalogPrefabSource _buildingPrefabSourceOverride;

        private ICatalogPrefabSource UnitPrefabSource =>
            _unitPrefabSourceOverride ?? unitPrefabRegistryConfig as ICatalogPrefabSource;

        private ICatalogPrefabSource BuildingPrefabSource =>
            _buildingPrefabSourceOverride ?? buildingPlacementConfig as ICatalogPrefabSource;

        private void Awake()
        {
            if (view == null)
                view = GetComponent<BuildDrawerView>();
        }

        private void OnEnable()
        {
            _nextQueueRefreshTime = 0f;
            BuildDrawerCatalogPresentationSystemHelper.WireTabs(view, _tabBindings, SelectCategory);
            WirePrimaryAction();
            WireQueueControls();
            Refresh();
        }

        private void Update()
        {
            if (view == null || _uiQuerySystem == null || Time.unscaledTime < _nextQueueRefreshTime)
                return;

            _nextQueueRefreshTime = Time.unscaledTime + QueueRefreshIntervalSeconds;
            RefreshQueue();
        }

        private void OnDisable()
        {
            UnwirePrimaryAction();
            UnwireQueueControls();
            BuildDrawerCatalogPresentationSystemHelper.ClearBindings(_tabBindings);
            BuildDrawerCatalogPresentationSystemHelper.ClearBindings(_itemBindings);
            BuildDrawerCatalogPresentationSystemHelper.ClearRuntimeItems(view, _runtimeItems);
            BuildDrawerProductionQueueUiSystemHelper.ClearRuntimeItems(_runtimeQueueItems);
            _selectedItemView = null;
            _hasSelectedItem = false;
            _nextQueueRefreshTime = 0f;
        }

        public void ConfigureForTests(
            BuildDrawerView drawerView,
            ICatalogPrefabSource unitRegistry,
            ICatalogPrefabSource buildingPlacement)
        {
            view = drawerView;
            _unitPrefabSourceOverride = unitRegistry;
            _buildingPrefabSourceOverride = buildingPlacement;
        }

        public void ConfigureCatalogMetadataResolvers(
            TryResolveUiBuildingCatalogMetadata tryResolveBuildingMetadata,
            TryResolveUiUnitCatalogMetadata tryResolveUnitMetadata)
        {
            _query.ConfigureMetadataResolvers(tryResolveBuildingMetadata, tryResolveUnitMetadata);
            if (isActiveAndEnabled)
                Refresh();
        }

        public void SelectCategoryForTests(BuildDrawerCategory category)
        {
            SelectCategory(category);
        }

        public void RefreshForTests()
        {
            Refresh();
        }

        public void ApplyQueueSnapshotForTests(IReadOnlyList<BuildingPendingProductionUiEntry> entries)
        {
            BuildDrawerProductionQueueUiSystemHelper.ApplySnapshot(CreateQueueContext(), entries);
        }

        public void BindRuntimeCommands(
            IBuildingUiCommand uiCommandSystem,
            Action closeDrawer,
            BattleHudRuntimeFeedbackView runtimeFeedbackView = null,
            IGameTextResolver gameTextResolver = null)
        {
            _uiCommandSystem = uiCommandSystem;
            _closeDrawer = closeDrawer;
            _runtimeFeedbackView = runtimeFeedbackView;
            _gameTextResolver = gameTextResolver ?? FallbackGameTextResolver.Instance;
            WirePrimaryAction();
            WireQueueControls();
            RefreshQueue();
            ApplyInstructionForCurrentSelection();
        }

        public void BindRuntimeQueries(IBuildingUiQuery uiQuerySystem)
        {
            _uiQuerySystem = uiQuerySystem;
            _nextQueueRefreshTime = 0f;
            RefreshQueue();
        }

        private void SelectCategory(BuildDrawerCategory category)
        {
            if (_activeCategory == category)
                return;

            _activeCategory = category;
            Refresh();
        }

        private void Refresh()
        {
            if (view == null)
                return;

            bool hasItems = BuildDrawerCatalogPresentationSystemHelper.RefreshCatalog(
                CreatePresentationContext(),
                _activeCategory);
            if (hasItems)
                SelectItem(view.ItemTemplate, _items[0]);
            else
                ClearSelection();

            RefreshQueue();
        }

        private void SelectItem(BuildDrawerItemView item, BuildDrawerCatalogItem model)
        {
            BuildDrawerCatalogPresentationSystemHelper.SelectItem(
                CreatePresentationContext(),
                item,
                model,
                ref _selectedItemView);
            _selectedItem = model;
            _hasSelectedItem = true;
            ApplyInstructionForCurrentSelection();
        }

        private void ClearSelection()
        {
            _selectedItemView = null;
            _hasSelectedItem = false;
            BuildDrawerCatalogPresentationSystemHelper.ClearDetail(view);
            ApplyInstruction(
                BuildDrawerCatalogPresentationSystemHelper.FormatEmptyCategoryInstruction(
                    _gameTextResolver,
                    _activeCategory),
                BuildDrawerInstructionSeverity.Warning);
        }

        private void OnPrimaryActionClicked()
        {
            if (!_hasSelectedItem || _selectedItem.Prefab == null)
            {
                ApplyBuildDrawerCommandResult(BuildingUiCommandFailure.InvalidSelection, string.Empty);
                return;
            }

            if (_uiCommandSystem == null)
            {
                string connecting = _gameTextResolver.Get("build.drawer.failure.connecting", "Build drawer is still connecting. Try again in a moment.");
                ApplyInstruction(connecting, BuildDrawerInstructionSeverity.Error);
                BattleHudRuntimeFeedbackUiSystemHelper.ApplyCommandResult(_runtimeFeedbackView, TacticalCommandResult.Rejected(
                    TacticalCommandReasonCode.BuildUnavailable,
                    _gameTextResolver.Get("build.feedback.drawer_not_ready", "Build drawer is not ready.")), _gameTextResolver);
                return;
            }

            BuildingUiCommandFailure failure = _uiCommandSystem.TryRequestCampItem(
                _selectedItem.Prefab,
                _selectedItem.Price,
                out string requiredBuildingDisplayName,
                false);
            if (failure != BuildingUiCommandFailure.None)
            {
                ApplyBuildDrawerCommandResult(failure, requiredBuildingDisplayName);
                return;
            }

            if (_selectedItem.Category == BuildDrawerCategory.Buildings)
            {
                ApplyInstruction(_gameTextResolver.Format("build.drawer.action.place_choose_footprint", "Place {0}: choose a valid footprint.", _selectedItem.DisplayName), BuildDrawerInstructionSeverity.Ready);
                BattleHudRuntimeFeedbackUiSystemHelper.ApplyStickyCommandMode(_runtimeFeedbackView, TacticalCommandMode.Build, _gameTextResolver);
                BattleHudRuntimeFeedbackUiSystemHelper.ApplyCommandResult(_runtimeFeedbackView, TacticalCommandResult.Success(_gameTextResolver.Get("build.feedback.place_building", "PLACE BUILDING")), _gameTextResolver);
                _closeDrawer?.Invoke();
                return;
            }

            ApplyInstruction(BuildDrawerCatalogPresentationSystemHelper.FormatPrimarySuccessInstruction(_gameTextResolver, _selectedItem), BuildDrawerInstructionSeverity.Ready);
            BattleHudRuntimeFeedbackUiSystemHelper.ApplyCommandResult(_runtimeFeedbackView, TacticalCommandResult.Success(
                _gameTextResolver.Format("build.feedback.production_requested", "{0}: {1}", _selectedItem.ActionLabel, _selectedItem.DisplayName)), _gameTextResolver);
            RefreshQueue();
        }

        private void OnCancelProductionClicked()
        {
            BuildDrawerProductionQueueUiSystemHelper.Context context = CreateQueueContext();
            bool cancelled = BuildDrawerProductionQueueUiSystemHelper.TryCancelActive(
                context,
                out BuildingPendingProductionUiEntry active,
                out bool requestAvailable);
            if (!requestAvailable)
            {
                string unavailable = _gameTextResolver.Get("build.feedback.production_cancel_unavailable", "Production cancel unavailable.");
                ApplyInstruction(unavailable, BuildDrawerInstructionSeverity.Error);
                BattleHudRuntimeFeedbackUiSystemHelper.ApplyCommandResult(_runtimeFeedbackView, TacticalCommandResult.Rejected(
                    TacticalCommandReasonCode.BuildUnavailable,
                    unavailable), _gameTextResolver);
                return;
            }

            ApplyInstruction(cancelled
                    ? _gameTextResolver.Format("build.feedback.production_cancelled_named", "Cancelled {0}.", BuildDrawerProductionQueueUiSystemHelper.ResolveDisplayName(context, active))
                    : _gameTextResolver.Get("build.feedback.production_cancel_unavailable", "Production cancel unavailable."),
                cancelled ? BuildDrawerInstructionSeverity.Warning : BuildDrawerInstructionSeverity.Error);
            BattleHudRuntimeFeedbackUiSystemHelper.ApplyCommandResult(_runtimeFeedbackView, cancelled
                ? TacticalCommandResult.Success(_gameTextResolver.Get("build.feedback.production_cancelled", "PRODUCTION CANCELLED"))
                : TacticalCommandResult.Rejected(TacticalCommandReasonCode.BuildUnavailable, _gameTextResolver.Get("build.feedback.production_cancel_unavailable", "Production cancel unavailable.")), _gameTextResolver);
            RefreshQueue();
        }

        private void OnClearProductionsClicked()
        {
            bool requestAvailable = BuildDrawerProductionQueueUiSystemHelper.TryClear(
                CreateQueueContext(),
                out int cancelledCount);
            if (!requestAvailable)
            {
                string empty = _gameTextResolver.Get("build.feedback.production_queue_empty", "Production queue is empty.");
                ApplyInstruction(empty, BuildDrawerInstructionSeverity.Warning);
                BattleHudRuntimeFeedbackUiSystemHelper.ApplyCommandResult(_runtimeFeedbackView, TacticalCommandResult.Rejected(
                    TacticalCommandReasonCode.BuildUnavailable,
                    empty), _gameTextResolver);
                return;
            }

            ApplyInstruction(cancelledCount > 0
                    ? _gameTextResolver.Get("build.feedback.production_queue_cleared_sentence", "Production queue cleared.")
                    : _gameTextResolver.Get("build.feedback.production_clear_unavailable", "Production clear unavailable."),
                cancelledCount > 0 ? BuildDrawerInstructionSeverity.Warning : BuildDrawerInstructionSeverity.Error);
            BattleHudRuntimeFeedbackUiSystemHelper.ApplyCommandResult(_runtimeFeedbackView, cancelledCount > 0
                ? TacticalCommandResult.Success(_gameTextResolver.Get("build.feedback.production_queue_cleared", "PRODUCTION QUEUE CLEARED"))
                : TacticalCommandResult.Rejected(TacticalCommandReasonCode.BuildUnavailable, _gameTextResolver.Get("build.feedback.production_clear_unavailable", "Production clear unavailable.")), _gameTextResolver);
            RefreshQueue();
        }

        private void RefreshQueue()
        {
            BuildDrawerProductionQueueUiSystemHelper.Refresh(CreateQueueContext());
        }

        private void ApplyBuildDrawerCommandResult(
            BuildingUiCommandFailure failure,
            string requiredBuildingDisplayName)
        {
            ApplyInstruction(
                BuildDrawerCatalogPresentationSystemHelper.FormatInstructionFailureMessage(
                    _gameTextResolver,
                    failure,
                    requiredBuildingDisplayName,
                    _selectedItem,
                    _hasSelectedItem,
                    MaxQueuedUnitProductions),
                BuildDrawerInstructionSeverity.Error);
            TacticalCommandReasonCode reason = IsResourceFailure(failure)
                ? TacticalCommandReasonCode.InsufficientResources
                : TacticalCommandReasonCode.BuildUnavailable;
            BattleHudRuntimeFeedbackUiSystemHelper.ApplyCommandResult(_runtimeFeedbackView, TacticalCommandResult.Rejected(
                reason,
                BuildDrawerCatalogPresentationSystemHelper.FormatFailureMessage(
                    _gameTextResolver,
                    failure,
                    requiredBuildingDisplayName,
                    MaxQueuedUnitProductions)), _gameTextResolver);
        }

        private void ApplyInstructionForCurrentSelection()
        {
            if (view == null)
                return;

            if (!_hasSelectedItem || _selectedItem.Prefab == null)
            {
                ApplyInstruction(BuildDrawerCatalogPresentationSystemHelper.FormatEmptyCategoryInstruction(_gameTextResolver, _activeCategory), BuildDrawerInstructionSeverity.Warning);
                return;
            }

            if (_uiCommandSystem == null)
            {
                ApplyInstruction(BuildDrawerCatalogPresentationSystemHelper.FormatReadyInstruction(_gameTextResolver, _selectedItem), BuildDrawerInstructionSeverity.Ready);
                return;
            }

            BuildingUiCommandFailure failure = _uiCommandSystem.GetCampRequestFailure(
                _selectedItem.Prefab,
                _selectedItem.Price,
                out string requiredBuildingDisplayName);
            if (failure == BuildingUiCommandFailure.None)
            {
                if (_selectedItem.Category == BuildDrawerCategory.Buildings && _uiCommandSystem.HasPendingBuildingPlacement)
                {
                    string status = _uiCommandSystem.PlacementStatusText;
                    bool canConfirm = _uiCommandSystem.CanConfirmBuildingPlacement;
                    ApplyInstruction(
                        canConfirm
                            ? _gameTextResolver.Format("build.drawer.instruction.place_pending_confirm", "Place {0}: drag to position, then confirm.", _selectedItem.DisplayName)
                            : _gameTextResolver.Format("build.drawer.instruction.cannot_place_here", "Cannot place here: {0}.", BuildDrawerCatalogPresentationSystemHelper.FormatPlacementStatus(_gameTextResolver, status)),
                        canConfirm ? BuildDrawerInstructionSeverity.Ready : BuildDrawerInstructionSeverity.Error);
                    return;
                }

                ApplyInstruction(BuildDrawerCatalogPresentationSystemHelper.FormatReadyInstruction(_gameTextResolver, _selectedItem), BuildDrawerInstructionSeverity.Ready);
                return;
            }

            ApplyInstruction(
                BuildDrawerCatalogPresentationSystemHelper.FormatInstructionFailureMessage(
                    _gameTextResolver,
                    failure,
                    requiredBuildingDisplayName,
                    _selectedItem,
                    _hasSelectedItem,
                    MaxQueuedUnitProductions),
                BuildDrawerInstructionSeverity.Error);
        }

        private int MaxQueuedUnitProductions =>
            Mathf.Max(0, _uiCommandSystem != null ? _uiCommandSystem.MaxQueuedUnitProductions : 25);

        private BuildingUiCommandFailure GetCampRequestFailure(
            BuildDrawerCatalogItem item,
            out string requiredBuildingDisplayName)
        {
            requiredBuildingDisplayName = string.Empty;
            return _uiCommandSystem != null
                ? _uiCommandSystem.GetCampRequestFailure(item.Prefab, item.Price, out requiredBuildingDisplayName)
                : BuildingUiCommandFailure.InvalidSelection;
        }

        private void ApplyInstruction(string text, BuildDrawerInstructionSeverity severity)
        {
            view?.ApplyInstruction(text, severity);
        }

        private void WirePrimaryAction()
        {
            if (view == null)
                return;

            Button button = view.PrimaryActionButton;
            if (button == null || button == _primaryActionButton)
                return;

            UnwirePrimaryAction();
            _primaryActionButton = button;
            _primaryActionListener = OnPrimaryActionClicked;
            _primaryActionButton.onClick.RemoveListener(_primaryActionListener);
            _primaryActionButton.onClick.AddListener(_primaryActionListener);
        }

        private void UnwirePrimaryAction()
        {
            if (_primaryActionButton != null && _primaryActionListener != null)
                _primaryActionButton.onClick.RemoveListener(_primaryActionListener);

            _primaryActionButton = null;
            _primaryActionListener = null;
        }

        private void WireQueueControls()
        {
            BuildDrawerProductionQueueUiSystemHelper.WireControls(
                view,
                OnCancelProductionClicked,
                OnClearProductionsClicked,
                ref _cancelButton,
                ref _cancelButtonListener,
                ref _clearButton,
                ref _clearButtonListener);
        }

        private void UnwireQueueControls()
        {
            BuildDrawerProductionQueueUiSystemHelper.UnwireControls(
                ref _cancelButton,
                ref _cancelButtonListener,
                ref _clearButton,
                ref _clearButtonListener);
        }

        private BuildDrawerCatalogPresentationSystemHelper.Context CreatePresentationContext()
        {
            return new BuildDrawerCatalogPresentationSystemHelper.Context(
                view,
                _query,
                UnitPrefabSource,
                BuildingPrefabSource,
                _gameTextResolver,
                _items,
                _countScratch,
                _runtimeItems,
                _itemBindings,
                SelectItem,
                GetCampRequestFailure);
        }

        private BuildDrawerProductionQueueUiSystemHelper.Context CreateQueueContext()
        {
            return new BuildDrawerProductionQueueUiSystemHelper.Context(
                view,
                _uiQuerySystem,
                _uiCommandSystem,
                _query,
                UnitPrefabSource,
                BuildingPrefabSource,
                _gameTextResolver,
                _pendingProductions,
                _clearProductionScratch,
                _runtimeQueueItems);
        }

        private static bool IsResourceFailure(BuildingUiCommandFailure failure)
        {
            return failure == BuildingUiCommandFailure.NotEnoughMoney ||
                   failure == BuildingUiCommandFailure.InsufficientCredits ||
                   failure == BuildingUiCommandFailure.InsufficientMaterials ||
                   failure == BuildingUiCommandFailure.InsufficientCreditsAndMaterials;
        }
    }
}
