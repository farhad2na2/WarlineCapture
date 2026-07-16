using System;
using Game.Components;
using Game.Composition;
using Game.Configs;
using UnityEditor;
using UnityEngine;

namespace Game.Editor
{
    [CustomEditor(typeof(OperationMapSceneView))]
    public sealed class OperationMapSceneViewEditor : UnityEditor.Editor
    {
        private static readonly Vector3[] MinimapCorners = new Vector3[5];
        private static bool showSpatialOverlay = true;
        private static bool showLabels = true;

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField("Operation Map Debug", EditorStyles.boldLabel);
            EditorGUI.BeginChangeCheck();
            showSpatialOverlay = EditorGUILayout.Toggle("Show Spatial Overlay", showSpatialOverlay);
            showLabels = EditorGUILayout.Toggle("Show Labels", showLabels);
            if (EditorGUI.EndChangeCheck())
                SceneView.RepaintAll();

            OperationMapSceneView view = (OperationMapSceneView)target;
            using (new EditorGUI.DisabledScope(view.MapSurfaceAuthoring == null))
            {
                if (GUILayout.Button("Preview Surface Heights"))
                {
                    MapSurfacePreviewOverlaySystem.ShowAuthoringPreview(
                        view.MapSurfaceAuthoring,
                        MapSurfaceEditorOverlaySystem.OverlayMode.Height);
                }

                if (GUILayout.Button("Preview Blocked Cells"))
                {
                    MapSurfacePreviewOverlaySystem.ShowAuthoringPreview(
                        view.MapSurfaceAuthoring,
                        MapSurfaceEditorOverlaySystem.OverlayMode.Blocked);
                }
            }

            using (new EditorGUI.DisabledScope(!MapSurfacePreviewOverlaySystem.HasPreview))
            {
                if (GUILayout.Button("Clear Surface Preview"))
                    MapSurfacePreviewOverlaySystem.ClearPreview();
            }
        }

        private void OnSceneGUI()
        {
            if (!showSpatialOverlay)
                return;

            OperationMapSceneView view = (OperationMapSceneView)target;
            OperationMapDefinition definition = view.Definition;
            if (definition == null)
                return;

            DrawBounds(definition.Bounds);
            DrawMinimap(definition.Minimap);
            DrawCameras(definition.Cameras);
            DrawAnchors(definition.Anchors);
        }

        private static void DrawBounds(OperationMapBoundsConfig bounds)
        {
            if (OperationMapSceneOverlayGeometry.TryCreateBounds(bounds.WorldMin, bounds.WorldMax, out Bounds world))
                DrawWireBounds(world, new Color(0.75f, 0.75f, 0.75f, 0.7f), "World");
            if (OperationMapSceneOverlayGeometry.TryCreateBounds(bounds.PlayableMin, bounds.PlayableMax, out Bounds playable))
                DrawWireBounds(playable, new Color(0.2f, 0.9f, 0.25f, 0.85f), "Playable");
            if (OperationMapSceneOverlayGeometry.TryCreateBounds(bounds.CameraMin, bounds.CameraMax, out Bounds camera))
                DrawWireBounds(camera, new Color(0.15f, 0.8f, 1f, 0.85f), "Camera");
        }

        private static void DrawWireBounds(Bounds bounds, Color color, string label)
        {
            Handles.color = color;
            Handles.DrawWireCube(bounds.center, bounds.size);
            if (showLabels)
                Handles.Label(bounds.center + Vector3.up * bounds.extents.y, label);
        }

        private static void DrawMinimap(OperationMapMinimapConfig minimap)
        {
            if (!OperationMapSceneOverlayGeometry.TryCreateHorizontalRectangle(
                    minimap.ProjectionOrigin,
                    minimap.ProjectionSize,
                    minimap.OrientationDegrees,
                    MinimapCorners))
            {
                return;
            }

            Handles.color = new Color(1f, 0.75f, 0.1f, 0.9f);
            Handles.DrawAAPolyLine(3f, MinimapCorners);
            if (showLabels)
                Handles.Label(MinimapCorners[0], minimap.MinimapId);
        }

        private static void DrawCameras(ReadOnlySpan<OperationMapCameraConfig> cameras)
        {
            Handles.color = new Color(0.2f, 0.85f, 1f, 0.95f);
            for (int index = 0; index < cameras.Length; index++)
            {
                OperationMapCameraConfig camera = cameras[index];
                Quaternion rotation = Quaternion.Euler(camera.EulerAngles);
                float markerSize = Mathf.Max(2f, HandleUtility.GetHandleSize(camera.Position) * 0.08f);
                Vector3 forward = rotation * Vector3.forward;
                Handles.DrawWireDisc(camera.Position, Vector3.up, markerSize);
                Handles.DrawLine(camera.Position, camera.Position + forward * markerSize * 4f, 3f);
                if (showLabels)
                    Handles.Label(camera.Position + Vector3.up * markerSize, camera.CameraId);
            }
        }

        private static void DrawAnchors(ReadOnlySpan<OperationMapAnchorConfig> anchors)
        {
            for (int index = 0; index < anchors.Length; index++)
            {
                OperationMapAnchorConfig anchor = anchors[index];
                Handles.color = ResolveAnchorColor(anchor.Kind);
                float radius = Mathf.Max(anchor.Radius, HandleUtility.GetHandleSize(anchor.Position) * 0.04f);
                Handles.DrawWireDisc(anchor.Position, Vector3.up, radius);
                Vector3 forward = Quaternion.Euler(anchor.EulerAngles) * Vector3.forward;
                Handles.DrawLine(anchor.Position, anchor.Position + forward * Mathf.Max(radius * 1.5f, 1f), 2f);
                if (showLabels)
                    Handles.Label(anchor.Position + Vector3.up * radius, anchor.AnchorId);
            }
        }

        internal static Color ResolveAnchorColor(OperationMapAnchorKind kind)
        {
            return kind switch
            {
                OperationMapAnchorKind.Spawn => new Color(0.1f, 1f, 0.35f, 0.95f),
                OperationMapAnchorKind.Deployment => new Color(0.15f, 0.75f, 1f, 0.95f),
                OperationMapAnchorKind.Objective => new Color(1f, 0.25f, 0.15f, 0.95f),
                OperationMapAnchorKind.Runway => new Color(1f, 0.85f, 0.2f, 0.95f),
                OperationMapAnchorKind.Helipad => new Color(0.25f, 1f, 0.9f, 0.95f),
                OperationMapAnchorKind.Lane => new Color(1f, 0.25f, 1f, 0.95f),
                _ => new Color(0.9f, 0.9f, 0.9f, 0.9f)
            };
        }
    }

    internal static class OperationMapSceneOverlayGeometry
    {
        public static bool TryCreateBounds(Vector3 minimum, Vector3 maximum, out Bounds bounds)
        {
            bounds = default;
            if (!IsFinite(minimum) || !IsFinite(maximum) ||
                maximum.x <= minimum.x || maximum.y < minimum.y || maximum.z <= minimum.z)
            {
                return false;
            }

            bounds = new Bounds((minimum + maximum) * 0.5f, maximum - minimum);
            return true;
        }

        public static bool TryCreateHorizontalRectangle(
            Vector3 origin,
            Vector2 size,
            float orientationDegrees,
            Vector3[] corners)
        {
            if (corners == null || corners.Length < 5 ||
                !IsFinite(origin) || !IsFinite(size) || !float.IsFinite(orientationDegrees) ||
                size.x <= 0f || size.y <= 0f)
            {
                return false;
            }

            Quaternion rotation = Quaternion.Euler(0f, orientationDegrees, 0f);
            corners[0] = origin;
            corners[1] = origin + rotation * new Vector3(size.x, 0f, 0f);
            corners[2] = origin + rotation * new Vector3(size.x, 0f, size.y);
            corners[3] = origin + rotation * new Vector3(0f, 0f, size.y);
            corners[4] = corners[0];
            return true;
        }

        private static bool IsFinite(Vector3 value) =>
            float.IsFinite(value.x) && float.IsFinite(value.y) && float.IsFinite(value.z);

        private static bool IsFinite(Vector2 value) =>
            float.IsFinite(value.x) && float.IsFinite(value.y);
    }
}
