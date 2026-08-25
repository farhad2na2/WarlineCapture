using Game.Catalog.Contracts;
using Game.UI.Contracts;

namespace Game.UI.Runtime
{
    public sealed partial class BuildDrawerCatalogRuntimeView
    {
        private const byte SelectRecommendationKind = 1;
        private const byte BuildRecommendationKind = 4;
        private const byte UiSurfaceTargetKind = 4;

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
            UiShellRuntimeGateway.TryAcknowledgeCampaignGuidanceTarget(
                UiCampaignGuidanceTargetKind.BarracksCatalogItem);
        }

        internal static bool RequiresExplicitMissionSelection(UiAssistantPanelModel model) =>
            model.HasRecommendation &&
            model.RecommendationTargetKind == UiSurfaceTargetKind &&
            (model.RecommendationKind == BuildRecommendationKind ||
             model.RecommendationKind == SelectRecommendationKind) &&
            (model.TutorialStep == 2 || model.TutorialStep == 3) &&
            model.TutorialStepCount == 9;

        private static bool RequiresExplicitMissionSelection() =>
            UiShellRuntimeGateway.TryReadMatchHudAssistantPanel(out UiAssistantPanelModel model) &&
            RequiresExplicitMissionSelection(model);
    }
}
