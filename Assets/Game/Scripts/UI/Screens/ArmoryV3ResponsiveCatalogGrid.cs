using UnityEngine;
using UnityEngine.UI;

namespace Game.UI.Runtime
{
    [ExecuteAlways]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RectTransform), typeof(GridLayoutGroup))]
    public sealed class ArmoryV3ResponsiveCatalogGrid : MonoBehaviour
    {
        [SerializeField, Min(1)] private int columnCount = 4;
        [SerializeField, Min(1f)] private float cellHeight = 296f;

        private RectTransform _rect;
        private GridLayoutGroup _grid;
        private float _lastWidth = -1f;

        public int ColumnCount => columnCount;

        public void Configure(int columns, float height)
        {
            columnCount = Mathf.Max(1, columns);
            cellHeight = Mathf.Max(1f, height);
            Apply();
        }

        private void OnEnable() => Apply();
        private void Start() => Apply();
        private void OnRectTransformDimensionsChange() => Apply();

        private void LateUpdate()
        {
            EnsureReferences();
            if (_rect != null && !Mathf.Approximately(_lastWidth, _rect.rect.width))
                Apply();
        }

        private void Apply()
        {
            EnsureReferences();
            if (_rect == null || _grid == null || _rect.rect.width <= 0f)
                return;

            float spacingWidth = _grid.spacing.x * (columnCount - 1);
            float horizontalPadding = _grid.padding.left + _grid.padding.right;
            float available = Mathf.Max(columnCount, _rect.rect.width - spacingWidth - horizontalPadding);
            _grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            _grid.constraintCount = columnCount;
            _grid.cellSize = new Vector2(available / columnCount, cellHeight);
            _lastWidth = _rect.rect.width;
        }

        private void EnsureReferences()
        {
            if (_rect == null)
                _rect = transform as RectTransform;
            if (_grid == null)
                _grid = GetComponent<GridLayoutGroup>();
        }
    }
}
