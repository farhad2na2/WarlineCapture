using System.Collections.Generic;
using System.Globalization;
using Game.Catalog.Contracts;
using Game.UI.Contracts;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Game.UI.Runtime
{
    internal static class BuildDrawerProductionQueueUiSystemHelper
    {
        internal readonly struct Context
        {
            public readonly BuildDrawerView View;
            public readonly IBuildingUiQuery QuerySystem;
            public readonly IBuildingUiCommand CommandSystem;
            public readonly BuildDrawerCatalogQueryUiSystemHelper CatalogQuery;
            public readonly ICatalogPrefabSource UnitPrefabSource;
            public readonly ICatalogPrefabSource BuildingPrefabSource;
            public readonly IGameTextResolver TextResolver;
            public readonly List<BuildingPendingProductionUiEntry> PendingProductions;
            public readonly List<BuildingPendingProductionUiEntry> ClearScratch;
            public readonly List<BuildDrawerQueueItemView> RuntimeItems;

            public Context(
                BuildDrawerView view,
                IBuildingUiQuery querySystem,
                IBuildingUiCommand commandSystem,
                BuildDrawerCatalogQueryUiSystemHelper catalogQuery,
                ICatalogPrefabSource unitPrefabSource,
                ICatalogPrefabSource buildingPrefabSource,
                IGameTextResolver textResolver,
                List<BuildingPendingProductionUiEntry> pendingProductions,
                List<BuildingPendingProductionUiEntry> clearScratch,
                List<BuildDrawerQueueItemView> runtimeItems)
            {
                View = view;
                QuerySystem = querySystem;
                CommandSystem = commandSystem;
                CatalogQuery = catalogQuery;
                UnitPrefabSource = unitPrefabSource;
                BuildingPrefabSource = buildingPrefabSource;
                TextResolver = textResolver;
                PendingProductions = pendingProductions;
                ClearScratch = clearScratch;
                RuntimeItems = runtimeItems;
            }
        }

        internal static void WireControls(
            BuildDrawerView view,
            UnityAction cancelAction,
            UnityAction clearAction,
            ref Button cancelButton,
            ref UnityAction cancelListener,
            ref Button clearButton,
            ref UnityAction clearListener)
        {
            if (view == null)
                return;

            Button resolvedCancelButton = ResolveProductionCancelButton(view);
            if (resolvedCancelButton != null && resolvedCancelButton != cancelButton)
            {
                UnwireButton(ref cancelButton, ref cancelListener);
                cancelButton = resolvedCancelButton;
                cancelListener = cancelAction;
                cancelButton.onClick.RemoveListener(cancelListener);
                cancelButton.onClick.AddListener(cancelListener);
            }

            if (view.ClearButton != null && view.ClearButton != clearButton)
            {
                UnwireButton(ref clearButton, ref clearListener);
                clearButton = view.ClearButton;
                clearListener = clearAction;
                clearButton.onClick.RemoveListener(clearListener);
                clearButton.onClick.AddListener(clearListener);
            }
        }

        internal static void UnwireControls(
            ref Button cancelButton,
            ref UnityAction cancelListener,
            ref Button clearButton,
            ref UnityAction clearListener)
        {
            UnwireButton(ref cancelButton, ref cancelListener);
            UnwireButton(ref clearButton, ref clearListener);
        }

        internal static void Refresh(Context context)
        {
            HideRuntimeItems(context.RuntimeItems, 0);
            if (context.View == null)
                return;

            BuildDrawerQueueItemView activeItem = context.View.ActiveItemView;
            BuildDrawerQueueItemView queuedTemplate = context.View.QueuedItemTemplate;
            HideStaticPlaceholderItems(context.View, activeItem, queuedTemplate);
            if (context.QuerySystem == null)
            {
                ApplyEmptyQueue(context, activeItem, queuedTemplate);
                return;
            }

            context.QuerySystem.GetFriendlyPendingProductionUiEntries(context.PendingProductions);
            ApplyEntries(context);
        }

        internal static void ApplySnapshot(
            Context context,
            IReadOnlyList<BuildingPendingProductionUiEntry> entries)
        {
            context.PendingProductions.Clear();
            if (entries != null)
            {
                for (int i = 0; i < entries.Count; i++)
                    context.PendingProductions.Add(entries[i]);
            }

            ApplyEntries(context);
        }

        internal static bool TryCancelActive(
            Context context,
            out BuildingPendingProductionUiEntry active,
            out bool requestAvailable)
        {
            active = default;
            requestAvailable = false;
            if (context.PendingProductions.Count == 0 ||
                context.PendingProductions[0].PendingProductionIndex < 0 ||
                context.CommandSystem == null)
            {
                return false;
            }

            active = context.PendingProductions[0];
            requestAvailable = true;
            return context.CommandSystem.CancelProduction(active.BuildingId, active.PendingProductionIndex);
        }

        internal static bool TryClear(Context context, out int cancelledCount)
        {
            cancelledCount = 0;
            if (context.PendingProductions.Count == 0 || context.CommandSystem == null)
                return false;

            context.ClearScratch.Clear();
            for (int i = 0; i < context.PendingProductions.Count; i++)
            {
                BuildingPendingProductionUiEntry entry = context.PendingProductions[i];
                if (entry.PendingProductionIndex >= 0)
                    context.ClearScratch.Add(entry);
            }

            context.ClearScratch.Sort(CompareProductionCancelOrder);
            for (int i = 0; i < context.ClearScratch.Count; i++)
            {
                BuildingPendingProductionUiEntry entry = context.ClearScratch[i];
                if (context.CommandSystem.CancelProduction(entry.BuildingId, entry.PendingProductionIndex))
                    cancelledCount++;
            }

            context.ClearScratch.Clear();
            return true;
        }

        internal static string ResolveDisplayName(
            Context context,
            BuildingPendingProductionUiEntry entry)
        {
            return context.CatalogQuery.TryResolvePrefab(
                    context.UnitPrefabSource,
                    context.BuildingPrefabSource,
                    entry.Prefab,
                    out BuildDrawerCatalogItem item)
                ? item.DisplayName
                : entry.Prefab != null
                    ? entry.Prefab.name
                    : context.TextResolver.Get("build.drawer.production.fallback_name", "Production");
        }

        internal static void ClearRuntimeItems(List<BuildDrawerQueueItemView> runtimeItems)
        {
            if (runtimeItems == null)
                return;

            for (int i = runtimeItems.Count - 1; i >= 0; i--)
            {
                BuildDrawerQueueItemView item = runtimeItems[i];
                if (item == null)
                    continue;

                if (Application.isPlaying)
                    UnityEngine.Object.Destroy(item.gameObject);
                else
                    UnityEngine.Object.DestroyImmediate(item.gameObject);
            }

            runtimeItems.Clear();
        }

        private static void ApplyEntries(Context context)
        {
            HideRuntimeItems(context.RuntimeItems, 0);
            if (context.View == null)
                return;

            BuildDrawerQueueItemView activeItem = context.View.ActiveItemView;
            BuildDrawerQueueItemView queuedTemplate = context.View.QueuedItemTemplate;
            HideStaticPlaceholderItems(context.View, activeItem, queuedTemplate);
            if (context.PendingProductions.Count == 0)
            {
                ApplyEmptyQueue(context, activeItem, queuedTemplate);
                return;
            }

            BuildingPendingProductionUiEntry active = context.PendingProductions[0];
            BindQueueItem(context, activeItem, active, 1);
            if (activeItem != null)
                activeItem.gameObject.SetActive(true);

            if (queuedTemplate != null)
            {
                if (context.PendingProductions.Count > 1)
                {
                    BindQueueItem(context, queuedTemplate, context.PendingProductions[1], 2);
                    queuedTemplate.gameObject.SetActive(true);
                }
                else
                {
                    queuedTemplate.gameObject.SetActive(false);
                }

                for (int i = 2; i < context.PendingProductions.Count; i++)
                {
                    BuildDrawerQueueItemView item = GetOrCreateRuntimeItem(context, i - 2, queuedTemplate);
                    if (item == null)
                        continue;

                    item.gameObject.name = $"ProductionItemView - {ResolveDisplayName(context, context.PendingProductions[i])}";
                    BindQueueItem(context, item, context.PendingProductions[i], i + 1);
                    item.gameObject.SetActive(true);
                }

                HideRuntimeItems(context.RuntimeItems, Mathf.Max(0, context.PendingProductions.Count - 2));
            }

            context.View.ApplyQueueSummary(
                true,
                active.Progress01,
                FormatPercent(active.Progress01),
                FormatRemaining(active.RemainingSeconds),
                context.PendingProductions.Count.ToString(CultureInfo.InvariantCulture));
            context.View.ApplySecondaryQueueControls(
                context.CommandSystem != null && active.PendingProductionIndex >= 0,
                false,
                context.CommandSystem != null && context.PendingProductions.Count > 0);
        }

        private static void ApplyEmptyQueue(
            Context context,
            BuildDrawerQueueItemView activeItem,
            BuildDrawerQueueItemView queuedTemplate)
        {
            if (activeItem != null)
                activeItem.gameObject.SetActive(false);
            if (queuedTemplate != null)
                queuedTemplate.gameObject.SetActive(false);
            HideRuntimeItems(context.RuntimeItems, 0);

            context.View.ApplyQueueSummary(false, 0f, string.Empty, string.Empty, string.Empty);
            context.View.ApplySecondaryQueueControls(false, false, false);
        }

        private static BuildDrawerQueueItemView GetOrCreateRuntimeItem(
            Context context,
            int poolIndex,
            BuildDrawerQueueItemView queuedTemplate)
        {
            if (poolIndex < 0 || queuedTemplate == null || context.View == null || context.View.QueueContentRoot == null)
                return null;

            while (context.RuntimeItems.Count <= poolIndex)
            {
                BuildDrawerQueueItemView item = UnityEngine.Object.Instantiate(
                    queuedTemplate,
                    context.View.QueueContentRoot,
                    false);
                item.gameObject.SetActive(false);
                context.RuntimeItems.Add(item);
            }

            return context.RuntimeItems[poolIndex];
        }

        private static void BindQueueItem(
            Context context,
            BuildDrawerQueueItemView item,
            BuildingPendingProductionUiEntry entry,
            int queueNumber)
        {
            if (item == null)
                return;

            item.Bind(
                queueNumber,
                ResolveDisplayName(context, entry),
                string.IsNullOrWhiteSpace(entry.ProducerDisplayName)
                    ? $"Building {entry.BuildingId}"
                    : entry.ProducerDisplayName,
                FormatRemaining(entry.RemainingSeconds),
                entry.Progress01,
                ResolveThumbnail(context, entry),
                queueNumber == 1 && context.CommandSystem != null && entry.PendingProductionIndex >= 0);
        }

        private static Sprite ResolveThumbnail(Context context, BuildingPendingProductionUiEntry entry)
        {
            return context.CatalogQuery.TryResolvePrefab(
                    context.UnitPrefabSource,
                    context.BuildingPrefabSource,
                    entry.Prefab,
                    out BuildDrawerCatalogItem item)
                ? item.CardPortrait
                : null;
        }

        private static void HideStaticPlaceholderItems(
            BuildDrawerView view,
            BuildDrawerQueueItemView activeItem,
            BuildDrawerQueueItemView queuedTemplate)
        {
            RectTransform root = view.QueueContentRoot;
            if (root == null)
                return;

            Transform activeTransform = activeItem != null ? activeItem.transform : null;
            Transform queuedTemplateTransform = queuedTemplate != null ? queuedTemplate.transform : null;
            for (int i = 0; i < root.childCount; i++)
            {
                Transform child = root.GetChild(i);
                if (child != activeTransform && child != queuedTemplateTransform)
                    child.gameObject.SetActive(false);
            }
        }

        private static void HideRuntimeItems(List<BuildDrawerQueueItemView> runtimeItems, int usedCount)
        {
            int clampedUsedCount = Mathf.Clamp(usedCount, 0, runtimeItems.Count);
            for (int i = clampedUsedCount; i < runtimeItems.Count; i++)
            {
                BuildDrawerQueueItemView item = runtimeItems[i];
                if (item != null)
                    item.gameObject.SetActive(false);
            }
        }

        private static Button ResolveProductionCancelButton(BuildDrawerView view)
        {
            return view.ActiveItemView != null && view.ActiveItemView.CancelButton != null
                ? view.ActiveItemView.CancelButton
                : view.CancelButton;
        }

        private static void UnwireButton(ref Button button, ref UnityAction listener)
        {
            if (button != null && listener != null)
                button.onClick.RemoveListener(listener);

            button = null;
            listener = null;
        }

        private static string FormatRemaining(float seconds)
        {
            int totalSeconds = Mathf.Max(0, Mathf.CeilToInt(seconds));
            return $"{totalSeconds / 60:00}:{totalSeconds % 60:00}";
        }

        private static string FormatPercent(float progress01)
        {
            return Mathf.RoundToInt(Mathf.Clamp01(progress01) * 100f).ToString(CultureInfo.InvariantCulture) + "%";
        }

        private static int CompareProductionCancelOrder(
            BuildingPendingProductionUiEntry left,
            BuildingPendingProductionUiEntry right)
        {
            int buildingComparison = left.BuildingId.CompareTo(right.BuildingId);
            return buildingComparison != 0
                ? buildingComparison
                : right.PendingProductionIndex.CompareTo(left.PendingProductionIndex);
        }
    }
}
