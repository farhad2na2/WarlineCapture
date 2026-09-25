using Game.UI.Contracts;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI.Runtime
{
    public sealed partial class BuildDrawerCatalogRuntimeView
    {
        internal IBuildingUiQuery ProductionQuery => _uiQuerySystem;
        internal bool HasPendingProduction => _pendingProductions.Count > 0;
        // Resolve from live drawer state so manual clicks and ARIA use the same next control.
        internal Button ResolveBuildingTutorialTarget(bool defense, bool placement, out string captionKey)
        {
            captionKey = "tutorial.next.buildings";
            if (view == null || !view.IsOpen) return null;
            if (_activeCategory != BuildDrawerCategory.Buildings)
                return ResolveCategoryButton(BuildDrawerCategory.Buildings);
            if (_hasSelectedItem && IsTutorialBuilding(_selectedItem.Prefab, defense) && placement)
            {
                captionKey = "tutorial.next.place";
                return _primaryActionButton;
            }
            captionKey = defense ? "tutorial.next.defense" : "ui.guidance.select_barracks";
            // Prefer the cheaper road barrier; a selected guard tower also satisfies this lesson.
            string preferred = defense ? "Building_Road_Barrier" : BarracksPrefabName;
            Button fallback = null;
            for (int i = 0; i < _items.Count; i++)
            {
                if (!IsTutorialBuilding(_items[i].Prefab, defense)) continue;
                var item = i == 0 ? view.ItemTemplate : i - 1 < _runtimeItems.Count ? _runtimeItems[i - 1] : null;
                var button = item?.SelectionButton;
                if (button == null || !button.IsActive() || !button.IsInteractable()) continue;
                if (_items[i].Prefab.name == preferred) return button;
                fallback = button;
            }
            return fallback;
        }

        public Button ResolveCatalogTarget(BuildDrawerCategory category, string prefabKey)
        {
            if (view == null || !view.IsOpen) return null;
            if (_activeCategory != category) return ResolveCategoryButton(category);
            if (_hasSelectedItem && _selectedItem.Prefab != null && _selectedItem.Prefab.name == prefabKey)
                return _primaryActionButton;
            for (int i = 0; i < _items.Count; i++)
            {
                if (_items[i].Prefab == null || _items[i].Prefab.name != prefabKey) continue;
                var item = i == 0 ? view.ItemTemplate : i - 1 < _runtimeItems.Count ? _runtimeItems[i - 1] : null;
                return item?.SelectionButton;
            }
            return null;
        }

        private static bool IsTutorialBuilding(GameObject prefab, bool defense) => prefab != null &&
            (defense ? prefab.name is "Building_Road_Barrier" or "Building_GuardTower" : prefab.name == BarracksPrefabName);

        internal string ResolveProductionTutorialCaption() => _activeCategory != BuildDrawerCategory.Soldiers
            ? "tutorial.next.soldiers" : !_hasSelectedItem || _selectedItem.Category != BuildDrawerCategory.Soldiers
                ? "tutorial.next.rifle" : "tutorial.next.produce";
    }
}
