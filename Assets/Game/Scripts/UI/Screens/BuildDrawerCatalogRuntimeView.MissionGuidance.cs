using Game.Catalog.Contracts;
using Game.UI.Contracts;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI.Runtime
{
    public sealed partial class BuildDrawerCatalogRuntimeView
    {
        private const string BarracksPrefabName = "Building_Barrack";
        private const byte SelectRecommendationKind = 1;
        private const byte BuildRecommendationKind = 4;
        private const byte ProduceRecommendationKind = 5;
        private const byte UiSurfaceTargetKind = 4;
        private bool _lastPrimaryActionAccepted;

        private void OnEnable()
        {
            _nextQueueRefreshTime = 0f;
            BuildDrawerCatalogPresentationSystemHelper.WireTabs(view, _tabBindings, SelectCategory);
            WirePrimaryAction();
            WireQueueControls();
            Refresh();
            UiShellRuntimeGateway.TryAcknowledgeCampaignGuidanceTarget(
                UiCampaignGuidanceTargetKind.BuildButton);
        }

        private void Refresh()
        {
            if (view == null)
                return;

            _cat.Refresh(UnitPrefabSource, BuildingPrefabSource);
            bool hasItems = BuildDrawerCatalogPresentationSystemHelper.RefreshCatalog(
                CreatePresentationContext(),
                _activeCategory);
            if (hasItems)
            {
                if (RequiresExplicitMissionSelection())
                    ClearSelection();
                else
                    SelectItem(view.ItemTemplate, _items[0]);
            }
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
            if (model.Category == BuildDrawerCategory.Buildings)
            {
                UiShellRuntimeGateway.TryAcknowledgeCampaignGuidanceTarget(
                    UiCampaignGuidanceTargetKind.BarracksCatalogItem);
            }
        }

        internal static bool RequiresExplicitMissionSelection(UiAssistantPanelModel model) =>
            model.HasRecommendation &&
            model.RecommendationTargetKind == UiSurfaceTargetKind &&
            (model.RecommendationKind == BuildRecommendationKind ||
             model.RecommendationKind == SelectRecommendationKind ||
             model.RecommendationKind == ProduceRecommendationKind) &&
            (model.TutorialStep == 2 || model.TutorialStep == 3 || model.TutorialStep == 6) &&
            model.TutorialStepCount == 9;

        private static bool RequiresExplicitMissionSelection() =>
            UiShellRuntimeGateway.TryReadMatchHudAssistantPanel(out UiAssistantPanelModel model) &&
            RequiresExplicitMissionSelection(model);

        internal bool TryInvokePrimaryActionFromGuidance()
        {
            if (!_hasSelectedItem ||
                _selectedItem.Category != BuildDrawerCategory.Buildings ||
                _primaryActionButton == null ||
                !_primaryActionButton.IsActive() ||
                !_primaryActionButton.IsInteractable())
            {
                return false;
            }

            _primaryActionButton.onClick.Invoke();
            return _uiCommandSystem?.HasPendingBuildingPlacement == true;
        }

        internal Button ResolveBarracksGuidanceButton()
        {
            if (view == null || !view.IsOpen || _activeCategory != BuildDrawerCategory.Buildings)
                return null;

            for (int index = 0; index < _items.Count; index++)
            {
                GameObject prefab = _items[index].Prefab;
                if (prefab == null || prefab.name != BarracksPrefabName)
                    continue;

                BuildDrawerItemView item = index == 0
                    ? view.ItemTemplate
                    : index - 1 < _runtimeItems.Count
                        ? _runtimeItems[index - 1]
                        : null;
                return item != null && item.gameObject.activeInHierarchy
                    ? item.SelectionButton
                    : null;
            }

            return null;
        }

        internal bool TryInvokeRifleProductionFromGuidance()
        {
            if (view == null || !view.IsOpen)
                return false;

            if (_activeCategory != BuildDrawerCategory.Soldiers)
            {
                Button soldiersTab = ResolveCategoryButton(BuildDrawerCategory.Soldiers);
                if (soldiersTab == null || !soldiersTab.IsActive() || !soldiersTab.IsInteractable())
                    return false;
                soldiersTab.onClick.Invoke();
            }

            if (!_hasSelectedItem || _selectedItem.Category != BuildDrawerCategory.Soldiers)
            {
                Button itemButton = view.ItemTemplate != null
                    ? view.ItemTemplate.SelectionButton
                    : null;
                if (itemButton == null || !itemButton.IsActive() || !itemButton.IsInteractable())
                    return false;
                itemButton.onClick.Invoke();
            }

            if (!_hasSelectedItem || _selectedItem.Category != BuildDrawerCategory.Soldiers ||
                _primaryActionButton == null || !_primaryActionButton.IsActive() ||
                !_primaryActionButton.IsInteractable())
            {
                return false;
            }

            _lastPrimaryActionAccepted = false;
            _primaryActionButton.onClick.Invoke();
            return _lastPrimaryActionAccepted;
        }

        private void AcceptUnit()
        {
            _lastPrimaryActionAccepted = true;
            RefreshQueue();
            UiShellRuntimeGateway.TryAcknowledgeCampaignGuidanceTarget(
                UiCampaignGuidanceTargetKind.RifleProduction);
        }

        internal RectTransform ResolveRifleProductionGuidanceTarget()
        {
            if (view == null || !view.IsOpen)
                return null;
            if (_activeCategory != BuildDrawerCategory.Soldiers)
                return ResolveCategoryButton(BuildDrawerCategory.Soldiers)?.transform as RectTransform;
            if (!_hasSelectedItem || _selectedItem.Category != BuildDrawerCategory.Soldiers)
                return view.ItemTemplate?.SelectionButton?.transform as RectTransform;
            return _primaryActionButton != null
                ? _primaryActionButton.transform as RectTransform
                : null;
        }

        private Button ResolveCategoryButton(BuildDrawerCategory category)
        {
            BuildDrawerTabView[] tabs = view?.Tabs;
            if (tabs == null)
                return null;
            for (int index = 0; index < tabs.Length; index++)
            {
                BuildDrawerTabView tab = tabs[index];
                if (tab != null && tab.Category == category)
                    return tab.Button;
            }

            return null;
        }
    }
}
