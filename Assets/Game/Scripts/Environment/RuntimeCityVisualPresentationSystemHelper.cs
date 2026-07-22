using UnityEngine;
using Game.Components;

namespace Game.Runtime
{
    internal sealed partial class RuntimeCityVisualPresentationSystemHelper
    {
        RuntimeCitySurfaceIntegrationUtilitySystemHelper _surfaceIntegrationUtilitySystemHelper;
        Transform _runtimeRoot;
        Transform _cityVisualRoot;

        public void SetRuntimeRoot(Transform runtimeRoot)
        {
            _runtimeRoot = runtimeRoot;
            _cityVisualRoot = null;
        }

        public void Dispose()
        {
            _surfaceIntegrationUtilitySystemHelper?.Clear();
            DestroyRoot(_cityVisualRoot);
            _cityVisualRoot = null;
            _runtimeRoot = null;
        }

        public void ConfigureSurface(MapSurfaceComponent surface)
        {
            RuntimeCitySurfaceIntegrationUtilitySystemHelper?.Configure(surface);
        }

        public void ClearSurface()
        {
            RuntimeCitySurfaceIntegrationUtilitySystemHelper?.Clear();
        }

        public void EnsureCityVisualRoot()
        {
            if (_cityVisualRoot != null)
                return;

            var root = new GameObject("RuntimeCityVisuals");
            _cityVisualRoot = root.transform;
            _cityVisualRoot.SetParent(_runtimeRoot, false);
            _cityVisualRoot.localPosition = Vector3.zero;
            _cityVisualRoot.localRotation = Quaternion.identity;
            _cityVisualRoot.localScale = Vector3.one;
        }

        public Transform CityVisualRoot
        {
            get
            {
                EnsureCityVisualRoot();
                return _cityVisualRoot;
            }
        }

        public GameObject SpawnVisualOnlyPrefab(
            GameObject prefab,
            Vector2Int originCell,
            Vector2Int footprintCells,
            Quaternion rotation,
            GridConfig grid)
        {
            if (prefab == null)
                return null;

            EnsureCityVisualRoot();

            var wrapper = new GameObject($"{prefab.name}_Visual");
            wrapper.transform.SetParent(_cityVisualRoot, false);
            Vector3 center = GetFootprintCenter(originCell, footprintCells, grid);
            center = RuntimeCitySurfaceIntegrationUtilitySystemHelper?.ResolveFootprintCenter(originCell, footprintCells, grid, center) ?? center;
            wrapper.transform.SetPositionAndRotation(center, rotation);
            wrapper.transform.localScale = Vector3.one;

            GameObject visual;
            var combinedMesh = FindDescendantByName(prefab.transform, "CombinedMesh");
            if (combinedMesh != null)
                visual = UnityEngine.Object.Instantiate(combinedMesh.gameObject, wrapper.transform);
            else
                visual = UnityEngine.Object.Instantiate(prefab, wrapper.transform);

            visual.transform.localRotation = Quaternion.identity;
            visual.transform.localScale = Vector3.one;
            if (TryGetLocalBounds(visual, out Bounds bounds))
                visual.transform.localPosition = new Vector3(-bounds.center.x, -bounds.min.y, -bounds.center.z);
            else
                visual.transform.localPosition = Vector3.zero;

            SetChildVisibleByName(visual.transform, "Destroyed", false);
            return wrapper;
        }

        public Vector3 GetFootprintCenter(Vector2Int originCell, Vector2Int footprintCells, GridConfig grid)
        {
            return new Vector3(
                grid.Origin.x + (originCell.x + footprintCells.x * 0.5f) * grid.CellSize,
                0f,
                grid.Origin.z + (originCell.y + footprintCells.y * 0.5f) * grid.CellSize);
        }

        private RuntimeCitySurfaceIntegrationUtilitySystemHelper RuntimeCitySurfaceIntegrationUtilitySystemHelper =>
            _surfaceIntegrationUtilitySystemHelper ??= ResolveRuntimeCitySurfaceIntegrationUtilitySystemHelper();

        private static RuntimeCitySurfaceIntegrationUtilitySystemHelper ResolveRuntimeCitySurfaceIntegrationUtilitySystemHelper()
        {
            return new RuntimeCitySurfaceIntegrationUtilitySystemHelper();
        }

        private static bool TryGetLocalBounds(GameObject target, out Bounds bounds)
        {
            bounds = default;
            var renderers = target.GetComponentsInChildren<Renderer>(true);
            bool hasBounds = false;
            Matrix4x4 worldToLocal = target.transform.worldToLocalMatrix;
            for (int i = 0; i < renderers.Length; i++)
            {
                var renderer = renderers[i];
                Bounds rendererBounds = renderer.bounds;
                Vector3 min = rendererBounds.min;
                Vector3 max = rendererBounds.max;

                for (int x = 0; x < 2; x++)
                {
                    for (int y = 0; y < 2; y++)
                    {
                        for (int z = 0; z < 2; z++)
                        {
                            Vector3 corner = new(
                                x == 0 ? min.x : max.x,
                                y == 0 ? min.y : max.y,
                                z == 0 ? min.z : max.z);
                            Vector3 localCorner = worldToLocal.MultiplyPoint3x4(corner);
                            if (!hasBounds)
                            {
                                bounds = new Bounds(localCorner, Vector3.zero);
                                hasBounds = true;
                            }
                            else
                            {
                                bounds.Encapsulate(localCorner);
                            }
                        }
                    }
                }
            }

            return hasBounds;
        }

        private static void SetChildVisibleByName(Transform root, string targetName, bool visible)
        {
            var child = FindDescendantByName(root, targetName);
            if (child != null)
                child.gameObject.SetActive(visible);
        }
        private static Transform FindDescendantByName(Transform root, string targetName)
        {
            if (root == null || string.IsNullOrEmpty(targetName))
                return null;
            if (root.name == targetName)
                return root;

            for (int i = 0; i < root.childCount; i++)
            {
                var found = FindDescendantByName(root.GetChild(i), targetName);
                if (found != null)
                    return found;
            }

            return null;
        }
    }
}
