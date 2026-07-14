using System;
using System.Collections.Generic;
using System.Globalization;
using Game.Catalog.Contracts;
using Game.UI.Contracts;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Game.UI.Runtime
{
    internal static class BuildDrawerCatalogPresentationSystemHelper
    {
        internal delegate BuildingUiCommandFailure GetCampRequestFailureDelegate(
            BuildDrawerCatalogItem item,
            out string requiredBuildingDisplayName);

        internal readonly struct ButtonBinding
        {
            public readonly Button Button;
            public readonly UnityAction Action;

            public ButtonBinding(Button button, UnityAction action)
            {
                Button = button;
                Action = action;
            }
        }

        internal readonly struct Context
        {
            public readonly BuildDrawerView View;
            public readonly BuildDrawerCatalogQueryUiSystemHelper Query;
            public readonly ICatalogPrefabSource UnitPrefabSource;
            public readonly ICatalogPrefabSource BuildingPrefabSource;
            public readonly IGameTextResolver TextResolver;
            public readonly List<BuildDrawerCatalogItem> Items;
            public readonly List<BuildDrawerCatalogItem> CountScratch;
            public readonly List<BuildDrawerItemView> RuntimeItems;
            public readonly List<ButtonBinding> ItemBindings;
            public readonly Action<BuildDrawerItemView, BuildDrawerCatalogItem> SelectItem;
            public readonly GetCampRequestFailureDelegate GetCampRequestFailure;

            public Context(
                BuildDrawerView view,
                BuildDrawerCatalogQueryUiSystemHelper query,
                ICatalogPrefabSource unitPrefabSource,
                ICatalogPrefabSource buildingPrefabSource,
                IGameTextResolver textResolver,
                List<BuildDrawerCatalogItem> items,
                List<BuildDrawerCatalogItem> countScratch,
                List<BuildDrawerItemView> runtimeItems,
                List<ButtonBinding> itemBindings,
                Action<BuildDrawerItemView, BuildDrawerCatalogItem> selectItem,
                GetCampRequestFailureDelegate getCampRequestFailure)
            {
                View = view;
                Query = query;
                UnitPrefabSource = unitPrefabSource;
                BuildingPrefabSource = buildingPrefabSource;
                TextResolver = textResolver;
                Items = items;
                CountScratch = countScratch;
                RuntimeItems = runtimeItems;
                ItemBindings = itemBindings;
                SelectItem = selectItem;
                GetCampRequestFailure = getCampRequestFailure;
            }
        }

        internal static void WireTabs(
            BuildDrawerView view,
            List<ButtonBinding> bindings,
            Action<BuildDrawerCategory> selectCategory)
        {
            if (view == null || view.Tabs == null || bindings == null || selectCategory == null)
                return;

            for (int i = 0; i < view.Tabs.Length; i++)
            {
                BuildDrawerTabView tab = view.Tabs[i];
                if (tab == null || tab.Button == null || HasBinding(bindings, tab.Button))
                    continue;

                BuildDrawerCategory category = tab.Category;
                UnityAction action = () => selectCategory(category);
                tab.Button.onClick.AddListener(action);
                bindings.Add(new ButtonBinding(tab.Button, action));
            }
        }

        internal static void ClearBindings(List<ButtonBinding> bindings)
        {
            if (bindings == null)
                return;

            for (int i = 0; i < bindings.Count; i++)
            {
                ButtonBinding binding = bindings[i];
                if (binding.Button != null)
                    binding.Button.onClick.RemoveListener(binding.Action);
            }

            bindings.Clear();
        }

        internal static bool RefreshCatalog(Context context, BuildDrawerCategory activeCategory)
        {
            if (context.View == null)
                return false;

            int[] counts = CountCategories(context);
            context.View.ApplyTabVisuals(activeCategory, counts, BuildEnabledStates(counts));
            context.Query.Collect(context.UnitPrefabSource, context.BuildingPrefabSource, activeCategory, context.Items);
            return PopulateItems(context);
        }

        internal static void SelectItem(
            Context context,
            BuildDrawerItemView item,
            BuildDrawerCatalogItem model,
            ref BuildDrawerItemView selectedItemView)
        {
            if (selectedItemView != null && selectedItemView != item)
                selectedItemView.SetSelected(false, context.View.SelectedItemFrameSprite);

            selectedItemView = item;
            selectedItemView?.SetSelected(true, context.View.SelectedItemFrameSprite);
            BindDetail(context, model);
        }

        internal static void ClearDetail(BuildDrawerView view)
        {
            if (view == null)
                return;

            view.BindDetail(
                string.Empty,
                string.Empty,
                string.Empty,
                string.Empty,
                string.Empty,
                string.Empty,
                string.Empty,
                "No requestable items.",
                null,
                null,
                string.Empty,
                false);
        }

        internal static void ClearRuntimeItems(
            BuildDrawerView view,
            List<BuildDrawerItemView> runtimeItems)
        {
            if (runtimeItems == null)
                return;

            for (int i = runtimeItems.Count - 1; i >= 0; i--)
            {
                BuildDrawerItemView item = runtimeItems[i];
                if (item == null)
                    continue;

                if (Application.isPlaying)
                    UnityEngine.Object.Destroy(item.gameObject);
                else
                    UnityEngine.Object.DestroyImmediate(item.gameObject);
            }

            runtimeItems.Clear();
            if (view != null && view.ItemTemplate != null)
                view.ItemTemplate.SetSelected(false, view.SelectedItemFrameSprite);
        }

        internal static string FormatFailureMessage(
            IGameTextResolver textResolver,
            BuildingUiCommandFailure failure,
            string requiredBuildingDisplayName,
            int maxQueuedUnitProductions)
        {
            return failure switch
            {
                BuildingUiCommandFailure.NotEnoughMoney => textResolver.Get("build.drawer.failure.short.not_enough_money", "Insufficient credits."),
                BuildingUiCommandFailure.InsufficientCredits => textResolver.Get("build.drawer.failure.short.insufficient_credits", "Insufficient credits."),
                BuildingUiCommandFailure.InsufficientMaterials => textResolver.Get("build.drawer.failure.short.insufficient_materials", "Insufficient materials."),
                BuildingUiCommandFailure.InsufficientCreditsAndMaterials => textResolver.Get("build.drawer.failure.short.insufficient_credits_and_materials", "Insufficient credits and materials."),
                BuildingUiCommandFailure.MissingProducerBuilding when !string.IsNullOrWhiteSpace(requiredBuildingDisplayName) =>
                    textResolver.Format("build.drawer.failure.short.requires_named", "Requires {0}.", requiredBuildingDisplayName),
                BuildingUiCommandFailure.MissingProducerBuilding => textResolver.Get("build.drawer.failure.short.missing_producer", "Required producer is missing."),
                BuildingUiCommandFailure.ProductionQueueFull when !string.IsNullOrWhiteSpace(requiredBuildingDisplayName) =>
                    textResolver.Format("build.drawer.failure.short.queue_full_named", "{0} production slots are full.", requiredBuildingDisplayName),
                BuildingUiCommandFailure.ProductionQueueFull => textResolver.Get("build.drawer.failure.short.queue_full", "All compatible production slots are full."),
                BuildingUiCommandFailure.GlobalProductionQueueFull => textResolver.Format("build.drawer.failure.short.global_queue_full", "Production queue limit reached ({0} max).", maxQueuedUnitProductions),
                BuildingUiCommandFailure.InvalidSelection => textResolver.Get("build.drawer.failure.invalid_selection", "Select a build drawer item first."),
                _ => textResolver.Get("build.drawer.failure.short.unavailable", "Build request unavailable.")
            };
        }

        internal static string FormatInstructionFailureMessage(
            IGameTextResolver textResolver,
            BuildingUiCommandFailure failure,
            string requiredBuildingDisplayName,
            BuildDrawerCatalogItem selectedItem,
            bool hasSelectedItem,
            int maxQueuedUnitProductions)
        {
            string itemName = hasSelectedItem
                ? selectedItem.DisplayName
                : textResolver.Get("build.drawer.item.fallback_name", "item");
            string verb = FormatActionVerb(textResolver, selectedItem.Category).ToLowerInvariant();
            return failure switch
            {
                BuildingUiCommandFailure.NotEnoughMoney =>
                    textResolver.Format("build.drawer.failure.insufficient_credits", "Cannot {0} {1}: insufficient credits.", verb, itemName),
                BuildingUiCommandFailure.InsufficientCredits =>
                    textResolver.Format("build.drawer.failure.insufficient_credits", "Cannot {0} {1}: insufficient credits.", verb, itemName),
                BuildingUiCommandFailure.InsufficientMaterials =>
                    textResolver.Format("build.drawer.failure.insufficient_materials", "Cannot {0} {1}: insufficient materials.", verb, itemName),
                BuildingUiCommandFailure.InsufficientCreditsAndMaterials =>
                    textResolver.Format("build.drawer.failure.insufficient_credits_and_materials", "Cannot {0} {1}: insufficient credits and materials.", verb, itemName),
                BuildingUiCommandFailure.MissingProducerBuilding when !string.IsNullOrWhiteSpace(requiredBuildingDisplayName) =>
                    textResolver.Format("build.drawer.failure.missing_producer_named", "Cannot {0} {1}: requires {2}.", verb, itemName, requiredBuildingDisplayName),
                BuildingUiCommandFailure.MissingProducerBuilding =>
                    textResolver.Format("build.drawer.failure.missing_producer", "Cannot {0} {1}: {2}.", verb, itemName, FormatMissingProducerFallback(textResolver, selectedItem.Category)),
                BuildingUiCommandFailure.ProductionQueueFull when !string.IsNullOrWhiteSpace(requiredBuildingDisplayName) =>
                    textResolver.Format("build.drawer.failure.queue_full_named", "Cannot {0} {1}: all {2} production slots are full.", verb, itemName, requiredBuildingDisplayName),
                BuildingUiCommandFailure.ProductionQueueFull =>
                    textResolver.Format("build.drawer.failure.queue_full", "Cannot {0} {1}: all compatible production slots are full.", verb, itemName),
                BuildingUiCommandFailure.GlobalProductionQueueFull =>
                    textResolver.Format("build.drawer.failure.global_queue_full", "Cannot {0} {1}: production queue limit reached ({2} max).", verb, itemName, maxQueuedUnitProductions),
                BuildingUiCommandFailure.InvalidSelection => textResolver.Get("build.drawer.failure.invalid_selection", "Select a build drawer item first."),
                _ => textResolver.Format("build.drawer.failure.unavailable", "Cannot {0} {1}: request unavailable.", verb, itemName)
            };
        }

        internal static string FormatReadyInstruction(IGameTextResolver textResolver, BuildDrawerCatalogItem model)
        {
            return model.Category switch
            {
                BuildDrawerCategory.Buildings => textResolver.Format("build.drawer.ready.buildings", "PLACE: choose a location for {0}.", model.DisplayName),
                BuildDrawerCategory.Vehicles => textResolver.Format("build.drawer.ready.vehicles", "PRODUCE: add {0} to the vehicle queue.", model.DisplayName),
                BuildDrawerCategory.Aircrafts => textResolver.Format("build.drawer.ready.aircraft", "PRODUCE: add {0} to the aircraft queue.", model.DisplayName),
                BuildDrawerCategory.Soldiers => textResolver.Format("build.drawer.ready.soldiers", "RECRUIT: add {0} to the training queue.", model.DisplayName),
                _ => textResolver.Format("build.drawer.ready.default", "Select {0}.", model.DisplayName)
            };
        }

        internal static string FormatPrimarySuccessInstruction(IGameTextResolver textResolver, BuildDrawerCatalogItem model)
        {
            return model.Category == BuildDrawerCategory.Soldiers
                ? textResolver.Format("build.drawer.success.recruitment_queued", "{0} added to recruitment queue.", model.DisplayName)
                : textResolver.Format("build.drawer.success.production_queued", "{0} added to production queue.", model.DisplayName);
        }

        internal static string FormatEmptyCategoryInstruction(IGameTextResolver textResolver, BuildDrawerCategory category)
        {
            return category switch
            {
                BuildDrawerCategory.Buildings => textResolver.Get("build.drawer.empty.buildings", "No requestable buildings are configured."),
                BuildDrawerCategory.Vehicles => textResolver.Get("build.drawer.empty.vehicles", "No requestable vehicles are configured."),
                BuildDrawerCategory.Aircrafts => textResolver.Get("build.drawer.empty.aircraft", "No requestable aircraft are configured."),
                BuildDrawerCategory.Soldiers => textResolver.Get("build.drawer.empty.soldiers", "No requestable soldiers are configured."),
                _ => textResolver.Get("build.drawer.empty.select_item", "Select an item to place, produce, or recruit.")
            };
        }

        internal static string FormatPlacementStatus(IGameTextResolver textResolver, string status)
        {
            return string.IsNullOrWhiteSpace(status)
                ? textResolver.Get("build.drawer.placement.invalid", "invalid placement")
                : status;
        }

        private static int[] CountCategories(Context context)
        {
            int[] counts = new int[4];
            for (int i = 0; i < counts.Length; i++)
            {
                context.Query.Collect(
                    context.UnitPrefabSource,
                    context.BuildingPrefabSource,
                    (BuildDrawerCategory)i,
                    context.CountScratch);
                counts[i] = context.CountScratch.Count;
            }

            context.CountScratch.Clear();
            return counts;
        }

        private static bool[] BuildEnabledStates(int[] counts)
        {
            bool[] states = new bool[4];
            for (int i = 0; i < states.Length; i++)
                states[i] = counts != null && i < counts.Length && counts[i] > 0;

            return states;
        }

        private static bool PopulateItems(Context context)
        {
            ClearBindings(context.ItemBindings);
            ClearRuntimeItems(context.View, context.RuntimeItems);
            HideStaticPlaceholderItems(context.View);

            BuildDrawerItemView template = context.View.ItemTemplate;
            if (template == null)
                return false;

            if (context.Items == null || context.Items.Count == 0)
            {
                template.gameObject.SetActive(false);
                return false;
            }

            BindItem(context, template, context.Items[0]);
            template.gameObject.SetActive(true);
            for (int i = 1; i < context.Items.Count; i++)
            {
                BuildDrawerItemView item = UnityEngine.Object.Instantiate(
                    template,
                    context.View.ItemContentRoot,
                    false);
                item.gameObject.name = $"ItemView - {context.Items[i].DisplayName}";
                BindItem(context, item, context.Items[i]);
                item.gameObject.SetActive(true);
                context.RuntimeItems.Add(item);
            }

            return true;
        }

        private static void BindItem(Context context, BuildDrawerItemView item, BuildDrawerCatalogItem model)
        {
            if (item == null)
                return;

            item.gameObject.name = item == context.View.ItemTemplate ? "ItemView" : $"ItemView - {model.DisplayName}";
            item.BindText(
                model.DisplayName,
                model.TypeLabel,
                model.Description,
                FormatPrice(model.Price),
                string.Empty,
                FormatDuration(model),
                FormatRequirements(context.TextResolver, model));
            item.BindThumbnail(model.CardPortrait);
            BuildingUiCommandFailure failure = context.GetCampRequestFailure(model, out _);
            item.SetInteractable(failure == BuildingUiCommandFailure.None);
            item.SetSelected(false, context.View.SelectedItemFrameSprite);

            Button button = item.SelectionButton;
            if (button == null)
                return;

            UnityAction action = () => context.SelectItem(item, model);
            button.onClick.AddListener(action);
            context.ItemBindings.Add(new ButtonBinding(button, action));
        }

        private static void BindDetail(Context context, BuildDrawerCatalogItem model)
        {
            BuildingUiCommandFailure failure = context.GetCampRequestFailure(model, out _);
            context.View.BindDetail(
                model.DisplayName,
                model.TypeLabel,
                model.Description,
                FormatPrice(model.Price),
                string.Empty,
                FormatDuration(model),
                FormatPlacement(model),
                FormatRequirements(context.TextResolver, model),
                model.ActionPortrait,
                model.CardPortrait,
                model.ActionLabel,
                failure == BuildingUiCommandFailure.None);
        }

        private static void HideStaticPlaceholderItems(BuildDrawerView view)
        {
            RectTransform root = view.ItemContentRoot;
            BuildDrawerItemView template = view.ItemTemplate;
            if (root == null || template == null)
                return;

            Transform templateTransform = template.transform;
            for (int i = 0; i < root.childCount; i++)
            {
                Transform child = root.GetChild(i);
                if (child != templateTransform)
                    child.gameObject.SetActive(false);
            }
        }

        private static bool HasBinding(List<ButtonBinding> bindings, Button button)
        {
            for (int i = 0; i < bindings.Count; i++)
            {
                if (bindings[i].Button == button)
                    return true;
            }

            return false;
        }

        private static string FormatPrice(int price)
        {
            return Mathf.Max(0, price).ToString(CultureInfo.InvariantCulture);
        }

        private static string FormatDuration(BuildDrawerCatalogItem model)
        {
            if (model.ProductionDurationSeconds <= 0f)
                return "-";

            int seconds = Mathf.CeilToInt(model.ProductionDurationSeconds);
            return $"{seconds / 60:00}:{seconds % 60:00}";
        }

        private static string FormatPlacement(BuildDrawerCatalogItem model)
        {
            return model.Category == BuildDrawerCategory.Buildings
                ? $"{model.FootprintCells.x}x{model.FootprintCells.y}"
                : "-";
        }

        private static string FormatRequirements(IGameTextResolver textResolver, BuildDrawerCatalogItem model)
        {
            return model.Category switch
            {
                BuildDrawerCategory.Buildings => textResolver.Get("build.drawer.requirements.buildings", "Valid footprint required."),
                BuildDrawerCategory.Aircrafts => textResolver.Get("build.drawer.requirements.aircraft", "Requires compatible air production."),
                BuildDrawerCategory.Vehicles => textResolver.Get("build.drawer.requirements.vehicles", "Requires compatible vehicle production."),
                BuildDrawerCategory.Soldiers => textResolver.Get("build.drawer.requirements.soldiers", "Requires compatible recruitment building."),
                _ => string.Empty
            };
        }

        private static string FormatMissingProducerFallback(IGameTextResolver textResolver, BuildDrawerCategory category)
        {
            return category switch
            {
                BuildDrawerCategory.Vehicles => textResolver.Get("build.drawer.missing_producer.vehicles", "no compatible vehicle producer is available"),
                BuildDrawerCategory.Aircrafts => textResolver.Get("build.drawer.missing_producer.aircraft", "no compatible air producer is available"),
                BuildDrawerCategory.Soldiers => textResolver.Get("build.drawer.missing_producer.soldiers", "no compatible training building is available"),
                _ => textResolver.Get("build.drawer.missing_producer.default", "required producer is missing")
            };
        }

        private static string FormatActionVerb(IGameTextResolver textResolver, BuildDrawerCategory category)
        {
            return category switch
            {
                BuildDrawerCategory.Buildings => textResolver.Get("build.drawer.verb.place", "Place"),
                BuildDrawerCategory.Soldiers => textResolver.Get("build.drawer.verb.recruit", "Recruit"),
                BuildDrawerCategory.Vehicles => textResolver.Get("build.drawer.verb.produce", "Produce"),
                BuildDrawerCategory.Aircrafts => textResolver.Get("build.drawer.verb.produce", "Produce"),
                _ => textResolver.Get("build.drawer.verb.request", "Request")
            };
        }
    }
}
