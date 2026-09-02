using System;
using UnityEngine;

namespace Game.UI.Runtime
{
    [ExecuteAlways]
    [DisallowMultipleComponent]
    public sealed class OperationsDashboardMapResponsiveView : MonoBehaviour
    {
        [SerializeField] private RectTransform mapClip;
        [SerializeField] private RectTransform[] districtZones = Array.Empty<RectTransform>();
        [SerializeField] private RectTransform[] districtMarkers = Array.Empty<RectTransform>();
        [SerializeField] private float authoredMapWidth = 873f;
        [SerializeField, HideInInspector] private Vector2[] authoredMarkerPositions = Array.Empty<Vector2>();

        private float _lastWidth = -1f;

        public RectTransform MapClip => mapClip;
        public RectTransform[] DistrictZones => districtZones;
        public RectTransform[] DistrictMarkers => districtMarkers;
        public float AuthoredMapWidth => authoredMapWidth;

        public void Configure(
            RectTransform configuredMapClip,
            RectTransform[] configuredDistrictZones,
            RectTransform[] configuredDistrictMarkers,
            float configuredAuthoredMapWidth)
        {
            mapClip = configuredMapClip;
            districtZones = configuredDistrictZones ?? Array.Empty<RectTransform>();
            districtMarkers = configuredDistrictMarkers ?? Array.Empty<RectTransform>();
            authoredMapWidth = Mathf.Max(1f, configuredAuthoredMapWidth);
            CaptureAuthoredMarkerPositions();
            RefreshLayout();
        }

        public void RefreshLayout()
        {
            if (mapClip == null || authoredMapWidth <= 0f)
                return;

            float width = mapClip.rect.width > 0f ? mapClip.rect.width : mapClip.sizeDelta.x;
            if (width <= 0f)
                return;
            if (authoredMarkerPositions.Length != districtMarkers.Length)
                CaptureAuthoredMarkerPositions();

            for (int i = 0; i < districtZones.Length; i++)
            {
                RectTransform zone = districtZones[i];
                if (zone == null)
                    continue;
                Vector2 size = zone.sizeDelta;
                size.x = width;
                zone.sizeDelta = size;
                V3PolygonGraphic polygon = zone.GetComponent<V3PolygonGraphic>();
                if (polygon != null)
                    polygon.SetVerticesDirty();
            }

            float scaleX = width / authoredMapWidth;
            int markerCount = Mathf.Min(districtMarkers.Length, authoredMarkerPositions.Length);
            for (int i = 0; i < markerCount; i++)
            {
                RectTransform marker = districtMarkers[i];
                if (marker == null)
                    continue;
                Vector2 position = authoredMarkerPositions[i];
                position.x *= scaleX;
                marker.anchoredPosition = position;
            }

            _lastWidth = width;
        }

        private void CaptureAuthoredMarkerPositions()
        {
            authoredMarkerPositions = new Vector2[districtMarkers.Length];
            for (int i = 0; i < districtMarkers.Length; i++)
                authoredMarkerPositions[i] = districtMarkers[i] != null ? districtMarkers[i].anchoredPosition : Vector2.zero;
        }

        private void OnEnable() => RefreshLayout();
        private void Start() => RefreshLayout();
        private void OnRectTransformDimensionsChange() => RefreshLayout();

        private void LateUpdate()
        {
            if (mapClip == null)
                return;
            float width = mapClip.rect.width > 0f ? mapClip.rect.width : mapClip.sizeDelta.x;
            if (!Mathf.Approximately(width, _lastWidth))
                RefreshLayout();
        }
    }
}
