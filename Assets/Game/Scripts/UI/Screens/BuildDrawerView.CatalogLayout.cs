using UnityEngine;
using UnityEngine.UI;

namespace Game.UI.Runtime
{
    public sealed partial class BuildDrawerView
    {
        internal void SetCatalogItemCount(int count)
        {
            var grid = itemContentRoot != null ? itemContentRoot.GetComponent<GridLayoutGroup>() : null;
            if (grid == null) return;
            var viewport = itemContentRoot.parent as RectTransform;
            float width = viewport != null ? viewport.rect.width : itemContentRoot.rect.width;
            int columns = count == 1 ? 1 : 2;
            grid.constraintCount = columns;
            grid.cellSize = new Vector2(Mathf.Max(1, (width - grid.spacing.x * (columns - 1)) / columns),
                count == 1 ? 620f : 340f);
        }
    }
}
