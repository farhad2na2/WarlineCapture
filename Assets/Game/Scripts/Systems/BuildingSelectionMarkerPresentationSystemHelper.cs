using System;
using System.Collections.Generic;
using UnityEngine;
using Game.Components;

namespace Game.Runtime
{
    internal sealed class BuildingSelectionMarkerPresentationSystemHelper
    {
        private const float MarkerSurfaceClearance = 0.12f;
        private const string RuntimeMarkerName = "BuildingSelectionMarkerRuntime";
        private const string SelectionObjectOutlineToken = "SelectionObjectOutline";
        private static readonly Color PremiumSelectionColor = new(0.05f, 0.88f, 1f, 0.94f);
        private static readonly Color PremiumSelectionEmissionColor = new(0.08f, 0.96f, 1f, 1f);
        private static readonly Color PremiumSelectionAccentColor = new(0.82f, 1f, 1f, 1f);

        public delegate bool TryGetGridDelegate(out GridConfig grid);
        public delegate Vector3 GetFootprintCenterDelegate(Vector2Int originCell, Vector2Int footprintCells, GridConfig grid);
        public delegate void DestroyObjectDelegate(UnityEngine.Object target);

        public readonly struct Context
        {
            public readonly RuntimeBuildingCollection<RuntimeBuildingEntity> RuntimeBuildingSystem;
            public readonly IReadOnlyDictionary<int, RuntimeBuildingEntity> RuntimeBuildings;
            public readonly TryGetGridDelegate TryGetGrid;
            public readonly GetFootprintCenterDelegate GetFootprintCenter;
            public readonly GameObject MarkerPrefab;
            public readonly Transform MarkerParent;
            public readonly BuildingVisualSystem VisualSystem;
            public readonly FactionVisualSettings FactionVisualSettings;
            public readonly MaterialPropertyBlock MarkerPropertyBlock;
            public readonly DestroyObjectDelegate DestroyObject;

            public Context(
                RuntimeBuildingCollection<RuntimeBuildingEntity> runtimeBuildingSystem,
                IReadOnlyDictionary<int, RuntimeBuildingEntity> runtimeBuildings,
                TryGetGridDelegate tryGetGrid,
                GetFootprintCenterDelegate getFootprintCenter,
                GameObject markerPrefab,
                Transform markerParent,
                BuildingVisualSystem visualSystem,
                FactionVisualSettings factionVisualSettings,
                MaterialPropertyBlock markerPropertyBlock,
                DestroyObjectDelegate destroyObject)
            {
                RuntimeBuildingSystem = runtimeBuildingSystem;
                RuntimeBuildings = runtimeBuildings;
                TryGetGrid = tryGetGrid;
                GetFootprintCenter = getFootprintCenter;
                MarkerPrefab = markerPrefab;
                MarkerParent = markerParent;
                VisualSystem = visualSystem;
                FactionVisualSettings = factionVisualSettings;
                MarkerPropertyBlock = markerPropertyBlock;
                DestroyObject = destroyObject;
            }
        }

        private GameObject _markerInstance;
        private Renderer[] _markerRenderers;
        private Vector3 _baseRendererSize = Vector3.one;
        private PremiumWorldSelectionFrameView _boundaryView;
        private PremiumWorldSelectionObjectOutlineView _objectOutlineView;
        private Color _markerColor = PremiumSelectionColor;

        internal GameObject RuntimeMarkerForTests => _markerInstance;

        public void Refresh(Context context)
        {
            if (!TryResolveSelection(context, out RuntimeBuildingEntity building, out GridConfig grid))
            {
                Hide();
                return;
            }

            if (!EnsureMarker(context))
            {
                Hide();
                return;
            }

            Vector2Int footprint = building.Definition != null
                ? building.Definition.FootprintCells
                : Vector2Int.one;
            Vector3 center = context.GetFootprintCenter(building.OriginCell, footprint, grid);
            Quaternion rotation = Quaternion.identity;
            Bounds bounds = default;
            bool hasPresentationBounds = building.Instance != null &&
                                         TryCalculateRendererBounds(building.Instance, out bounds);
            MapAuthoredBuildingVisualComponent authoredVisual = null;
            bool isMapAuthored = building.Instance != null &&
                                 building.Instance.TryGetComponent(out authoredVisual);
            if (isMapAuthored && authoredVisual.HasPresentationWorldCenter)
            {
                center.x = authoredVisual.PresentationWorldCenter.x;
                center.z = authoredVisual.PresentationWorldCenter.z;
            }
            float surfaceY = hasPresentationBounds
                ? ResolveMarkerSurfaceY(building.Instance, bounds, grid)
                : building.Instance != null
                    ? building.Instance.transform.position.y
                    : center.y;
            center.y = surfaceY;

            // Selection presentation is a gameplay footprint contract. Aggregate
            // prefab/renderer bounds may include broad compounds, props, or static
            // presentation owned elsewhere and must never enlarge the click frame.
            Vector2 markerWorldSize = ResolveMarkerWorldSize(footprint, grid);
            Transform markerTransform = _markerInstance.transform;
            markerTransform.SetPositionAndRotation(center, rotation);
            markerTransform.localScale = ResolveScale(markerWorldSize);
            SetActive(true);
            LiftMarkerRendererBoundsAbove(surfaceY + MarkerSurfaceClearance);
            ConfigureBoundaryView(markerWorldSize, center, rotation, surfaceY, hasPresentationBounds ? bounds : default);
            ConfigureObjectOutline(building, hasPresentationBounds ? bounds : default);
        }

        public void Hide()
        {
            SetActive(false);
            _objectOutlineView?.Hide();
        }

        public void Dispose(Context context)
        {
            if (_markerInstance == null)
                return;

            if (context.DestroyObject != null)
            {
                context.DestroyObject(_markerInstance);
            }
            else if (Application.isPlaying)
            {
                UnityEngine.Object.Destroy(_markerInstance);
            }
            else
            {
                UnityEngine.Object.DestroyImmediate(_markerInstance);
            }

            _markerInstance = null;
            _markerRenderers = null;
            _baseRendererSize = Vector3.one;
            _boundaryView = null;
            _objectOutlineView = null;
        }

        private bool TryResolveSelection(Context context, out RuntimeBuildingEntity building, out GridConfig grid)
        {
            building = null;
            grid = default;

            if (context.RuntimeBuildingSystem == null ||
                context.RuntimeBuildings == null ||
                context.TryGetGrid == null ||
                context.GetFootprintCenter == null ||
                !context.TryGetGrid(out grid))
            {
                return false;
            }

            int? activeBuildingId = context.RuntimeBuildingSystem.CurrentActiveBuildingId;
            if (!activeBuildingId.HasValue)
                return false;

            return context.RuntimeBuildings.TryGetValue(activeBuildingId.Value, out building) &&
                building != null &&
                !building.IsDestroyed &&
                building.Definition != null;
        }

        private bool EnsureMarker(Context context)
        {
            if (_markerInstance != null)
                return true;

            if (context.MarkerPrefab == null)
                return false;

            _markerInstance = UnityEngine.Object.Instantiate(context.MarkerPrefab, context.MarkerParent);
            _markerInstance.name = RuntimeMarkerName;
            _markerInstance.SetActive(false);
            _markerRenderers = _markerInstance.GetComponentsInChildren<Renderer>(true);
            _baseRendererSize = CalculateRendererSize(_markerRenderers);

            _markerColor = PremiumSelectionColor;
            context.VisualSystem?.ApplyMarkerColor(_markerRenderers, _markerColor, context.MarkerPropertyBlock);
            _boundaryView = _markerInstance.GetComponent<PremiumWorldSelectionFrameView>();
            if (_boundaryView == null)
                _boundaryView = _markerInstance.AddComponent<PremiumWorldSelectionFrameView>();
            _objectOutlineView = _markerInstance.GetComponent<PremiumWorldSelectionObjectOutlineView>();
            if (_objectOutlineView == null)
                _objectOutlineView = _markerInstance.AddComponent<PremiumWorldSelectionObjectOutlineView>();
            return true;
        }

        private void ConfigureBoundaryView(
            Vector2 worldSize,
            Vector3 markerCenter,
            Quaternion markerRotation,
            float surfaceY,
            Bounds rendererBounds)
        {
            if (_boundaryView == null)
                return;

            bool hasRendererBounds = rendererBounds.size.sqrMagnitude > 0.0001f;
            float height = hasRendererBounds
                ? Mathf.Max(0.8f, rendererBounds.size.y)
                : Mathf.Max(1.1f, Mathf.Min(4.5f, Mathf.Max(worldSize.x, worldSize.y) * 0.38f));
            Vector3 center = markerCenter;
            center.y = surfaceY;

            _boundaryView.Configure(center, markerRotation, worldSize, surfaceY, height, _markerColor, PremiumSelectionAccentColor);
        }

        private void ConfigureObjectOutline(RuntimeBuildingEntity building, Bounds rendererBounds)
        {
            if (_objectOutlineView == null || building?.Instance == null)
                return;

            float longestAxis = rendererBounds.size.sqrMagnitude > 0.0001f
                ? Mathf.Max(rendererBounds.size.x, rendererBounds.size.y, rendererBounds.size.z)
                : 4f;
            float outlineWidth = Mathf.Clamp(longestAxis * 0.0045f, 0.022f, 0.06f);
            _objectOutlineView.Configure(
                building.Instance,
                PremiumSelectionColor,
                PremiumSelectionEmissionColor,
                outlineWidth);
        }

        private static Vector2 ResolveMarkerWorldSize(Vector2Int footprint, GridConfig grid)
        {
            return new Vector2(
                Mathf.Max(grid.CellSize, footprint.x * grid.CellSize),
                Mathf.Max(grid.CellSize, footprint.y * grid.CellSize));
        }

        private Vector3 ResolveScale(Vector2 markerWorldSize)
        {
            float baseX = Mathf.Max(0.001f, _baseRendererSize.x);
            float baseZ = Mathf.Max(0.001f, _baseRendererSize.z);
            return new Vector3(
                Mathf.Max(0.01f, markerWorldSize.x) / baseX,
                1f,
                Mathf.Max(0.01f, markerWorldSize.y) / baseZ);
        }

        private static Vector3 CalculateRendererSize(Renderer[] renderers)
        {
            if (renderers == null || renderers.Length == 0)
                return Vector3.one;

            bool hasBounds = false;
            Bounds bounds = default;
            for (int i = 0; i < renderers.Length; i++)
            {
                Renderer renderer = renderers[i];
                if (renderer == null)
                    continue;

                if (!hasBounds)
                {
                    bounds = renderer.bounds;
                    hasBounds = true;
                }
                else
                {
                    bounds.Encapsulate(renderer.bounds);
                }
            }

            return hasBounds ? bounds.size : Vector3.one;
        }

        private static float ResolveMarkerSurfaceY(GameObject instance, Bounds bounds, GridConfig grid)
        {
            float surfaceY = Mathf.Max(grid.Origin.y, bounds.min.y);
            if (instance != null && bounds.min.y < grid.Origin.y - 0.001f)
                surfaceY = Mathf.Max(surfaceY, instance.transform.position.y);
            return surfaceY;
        }

        private void LiftMarkerRendererBoundsAbove(float minimumWorldY)
        {
            if (!TryCalculateRendererBounds(_markerRenderers, out Bounds markerBounds))
                return;

            float lift = minimumWorldY - markerBounds.min.y;
            if (lift <= 0.001f)
                return;

            _markerInstance.transform.position += Vector3.up * lift;
        }

        private static bool TryCalculateRendererBounds(Renderer[] renderers, out Bounds bounds)
        {
            bounds = default;
            if (renderers == null)
                return false;

            bool hasBounds = false;
            for (int i = 0; i < renderers.Length; i++)
            {
                Renderer renderer = renderers[i];
                if (renderer == null || !renderer.enabled || IsSelectionObjectOutlineRenderer(renderer))
                    continue;

                if (hasBounds)
                {
                    bounds.Encapsulate(renderer.bounds);
                }
                else
                {
                    bounds = renderer.bounds;
                    hasBounds = true;
                }
            }

            return hasBounds;
        }

        private static bool TryCalculateRendererBounds(GameObject instance, out Bounds bounds)
        {
            bounds = default;
            if (instance == null)
                return false;

            Renderer[] renderers = instance.GetComponentsInChildren<Renderer>(false);
            bool hasBounds = false;
            for (int i = 0; i < renderers.Length; i++)
            {
                Renderer renderer = renderers[i];
                if (renderer == null || !renderer.enabled || IsSelectionObjectOutlineRenderer(renderer))
                    continue;

                if (hasBounds)
                {
                    bounds.Encapsulate(renderer.bounds);
                }
                else
                {
                    bounds = renderer.bounds;
                    hasBounds = true;
                }
            }

            return hasBounds;
        }

        private static bool IsSelectionObjectOutlineRenderer(Renderer renderer)
        {
            if (renderer == null)
                return false;

            Transform current = renderer.transform;
            while (current != null)
            {
                if (current.name.IndexOf(SelectionObjectOutlineToken, StringComparison.OrdinalIgnoreCase) >= 0)
                    return true;
                current = current.parent;
            }

            return false;
        }

        private void SetActive(bool active)
        {
            if (_markerInstance != null && _markerInstance.activeSelf != active)
                _markerInstance.SetActive(active);
        }
    }
}
