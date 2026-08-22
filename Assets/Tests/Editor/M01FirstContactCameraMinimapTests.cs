using System;
using System.Collections.Generic;
using System.Reflection;
using Game.Components;
using Game.Configs;
using Game.Runtime;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;

public static class M01FirstContactCameraMinimapTests
{
    private const string MapPath =
        "Assets/Game/Configs/OperationMaps/Chapter01/OperationMap_Ch01_DistrictEdge01.asset";
    private const string Marker = "[M01FirstContactCameraMinimapValidation] result=Passed tests=12";
    private static readonly OperationMapCameraConfig Planning = new(
        "camera.ch01.m01.planning", new Vector3(1792f, 15f, 680f), new Vector3(35f, 0f, 0f),
        false, 50f, 5f, true);
    private static readonly OperationMapCameraConfig Battle = new(
        "camera.ch01.m01.battle_start", new Vector3(1792f, 9f, 680f), new Vector3(30f, 0f, 0f),
        false, 42f, 5f, true);
    private static readonly OperationMapMinimapConfig Minimap = new(
        "minimap.ch01.m01.projection", new Vector3(1672f, 0f, 680f), new Vector2(240f, 176f), 0f);
    private static readonly Vector2[] Aspects =
    {
        new(1920f, 1080f), new(2400f, 1080f), new(1920f, 1200f)
    };

    public static void RunFocusedValidation()
    {
        try
        {
            OperationMapDefinition map = AssetDatabase.LoadAssetAtPath<OperationMapDefinition>(MapPath);
            Require(map != null, "M01 logical operation-map asset is missing.");
            Author(map);
            Require(map.TryValidateMetadata(out string error), error);
            ValidateIdentities(map);
            ValidateCameraBounds(map);
            ValidateAspectFraming(map);
            ValidateMinimapRoundTrips(map);
            ValidateClampAndTransitionPolicy(map);
            M01FirstContactCameraMinimapEvidence.Write(map, Aspects, Marker);
            Debug.Log(Marker);
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            Debug.Log("[M01FirstContactCameraMinimapValidation] result=Failed");
            throw;
        }
    }

    private static void Author(OperationMapDefinition map)
    {
        SerializedObject serialized = new(map);
        serialized.FindProperty("cameras").arraySize = 2;
        SetCamera(serialized.FindProperty("cameras").GetArrayElementAtIndex(0), Planning);
        SetCamera(serialized.FindProperty("cameras").GetArrayElementAtIndex(1), Battle);
        serialized.FindProperty("planningCameraId").stringValue = Planning.CameraId;
        serialized.FindProperty("battleCameraId").stringValue = Battle.CameraId;
        SetMinimap(serialized.FindProperty("minimap"), Minimap);
        serialized.FindProperty("contentHash").stringValue =
            M01FirstContactCameraMinimapEvidence.Sha256Text(CameraAuthorityText());
        serialized.FindProperty("generatedMetadataHash").stringValue =
            M01FirstContactCameraMinimapEvidence.Sha256Text("m01dc-012|camera-minimap-v1|fl-p18-current");
        serialized.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(map);
        AssetDatabase.SaveAssets();
    }

    private static void ValidateIdentities(OperationMapDefinition map)
    {
        Require(map.PlanningCameraId == Planning.CameraId, "Planning camera identity drifted.");
        Require(map.BattleCameraId == Battle.CameraId, "Battle-start camera identity drifted.");
        Require(map.Minimap.MinimapId == Minimap.MinimapId, "Minimap identity drifted.");
        Require(map.Cameras.Length == 2, "M01 must own exactly two camera projections.");
    }

    private static void ValidateCameraBounds(OperationMapDefinition map)
    {
        foreach (OperationMapCameraConfig camera in map.Cameras)
        {
            Require(camera.ClampToCameraBounds, $"{camera.CameraId} must clamp to M01 camera bounds.");
            Require(Contains(map.Bounds.CameraMin, map.Bounds.CameraMax, camera.Position),
                $"{camera.CameraId} lies outside M01 camera bounds.");
            Require(!camera.Orthographic && camera.FieldOfView >= 30f && camera.FieldOfView <= 50f,
                $"{camera.CameraId} zoom is outside the readable perspective envelope.");
            Require(camera.EulerAngles.x >= 25f && camera.EulerAngles.x <= 35f,
                $"{camera.CameraId} must keep a low cinematic street pitch, not a top-down/full-map pitch.");
            Require(Mathf.Abs(Mathf.DeltaAngle(camera.EulerAngles.y, 0f)) <= 0.01f,
                $"{camera.CameraId} must look north along the bazaar street from the road-side squad toward the hall.");
        }
        Require(Mathf.Approximately(RuntimeCameraFocusRequestUtility.TacticalRevealPitch, 30f) &&
                Mathf.Approximately(RuntimeCameraFocusRequestUtility.SquadRevealPitch, 30f) &&
                Mathf.Approximately(RuntimeCameraFocusRequestUtility.TacticalRevealYaw, 0f),
            "Tutorial focus requests must stay centered on the bazaar street from the road toward the civic hall.");
        Require(Mathf.Approximately(RuntimeCameraFocusRequestUtility.TacticalRevealHeight, 9f) &&
                Mathf.Approximately(RuntimeCameraFocusRequestUtility.SquadRevealHeight, 9f) &&
                Mathf.Approximately(RuntimeCameraFocusRequestUtility.TacticalRevealFieldOfView, 38f) &&
                Mathf.Approximately(RuntimeCameraFocusRequestUtility.SquadRevealFieldOfView, 42f) &&
                Mathf.Approximately(RuntimeCameraFocusRequestUtility.BazaarEstablishingHeight, 15f) &&
                Mathf.Approximately(RuntimeCameraFocusRequestUtility.BazaarEstablishingPitch, 35f) &&
                Mathf.Approximately(RuntimeCameraFocusRequestUtility.BazaarEstablishingYaw, 0f) &&
                Mathf.Approximately(RuntimeCameraFocusRequestUtility.BazaarEstablishingFieldOfView, 50f) &&
                Mathf.Approximately(RuntimeCameraFocusRequestUtility.CombatRevealHeight, 12f) &&
                Mathf.Approximately(RuntimeCameraFocusRequestUtility.CombatRevealPitch, 30f) &&
                Mathf.Approximately(RuntimeCameraFocusRequestUtility.CombatRevealFieldOfView, 38f),
            "The opening must establish the accepted civic bazaar, zoom on the patrol, and return to the full squad.");
    }

    private static void ValidateAspectFraming(OperationMapDefinition map)
    {
        Vector3[] planningSubjects =
        {
            new(1792f, 0f, 690f), new(1790.5f, 0f, 691f), new(1793.5f, 0f, 691f),
            new(1792f, 0f, 735f), new(1790.5f, 0f, 734f), new(1793.5f, 0f, 734f)
        };
        Vector3[] battleSubjects =
        {
            new(1792f, 0f, 690f), new(1790.5f, 0f, 691f), new(1793.5f, 0f, 691f)
        };
        foreach (Vector2 resolution in Aspects)
        {
            ValidateSubjects(Planning, resolution.x / resolution.y, planningSubjects, new Rect(0.05f, 0.05f, 0.90f, 0.90f));
            ValidateSubjects(Battle, resolution.x / resolution.y, battleSubjects, new Rect(0.06f, 0.06f, 0.88f, 0.88f));
        }
        Require(map.Bounds.PlayableMin.x == 1672f && map.Bounds.PlayableMax.z == 856f,
            "Camera review no longer targets the accepted Old Market window.");
    }

    private static void ValidateMinimapRoundTrips(OperationMapDefinition map)
    {
        Require(map.TryCreatePersistentMetadataBlob(out BlobAssetReference<OperationMapBlob> blob, out string error), error);
        try
        {
            ref OperationMapMinimapBlob projection = ref blob.Value.Minimap;
            Vector3[] points =
            {
                new(1672f, 0f, 680f), new(1912f, 0f, 856f), new(1792f, 0f, 768f), new(1848f, 0f, 806f)
            };
            foreach (Vector3 point in points)
            {
                float3 world = new(point.x, point.y, point.z);
                Require(OperationMapMetadataUtility.TryWorldToMinimapNormalized(in projection, world, out float2 uv),
                    "World-to-minimap projection failed.");
                Require(OperationMapMetadataUtility.IsInsideNormalizedProjection(uv), "Playable point left minimap bounds.");
                Require(OperationMapMetadataUtility.TryMinimapNormalizedToWorldClamped(in projection, uv, point.y,
                    out float3 roundTrip), "Minimap-to-world projection failed.");
                Require(math.distance(world, roundTrip) <= 0.001f, "Minimap round trip was not exact.");
            }
            Require(OperationMapMetadataUtility.TryMinimapNormalizedToWorldClamped(in projection,
                new float2(-1f, 2f), 0f, out float3 clamped), "Clamped minimap projection failed.");
            Require(math.distance(clamped, new float3(1672f, 0f, 856f)) <= 0.001f,
                "Out-of-range minimap input did not clamp exactly.");
        }
        finally { blob.Dispose(); }
    }

    private static void ValidateClampAndTransitionPolicy(OperationMapDefinition map)
    {
        Vector3 outside = new(2200f, 160f, 150f);
        Vector3 clamped = Vector3.Max(map.Bounds.CameraMin, Vector3.Min(map.Bounds.CameraMax, outside));
        Require(clamped == new Vector3(1912f, 100f, 680f), "Camera clamp result drifted.");
        Require(M01FirstContactCameraMinimapEvidence.NormalBlendSeconds == 1.25f,
            "Planning-to-battle normal blend policy drifted.");
        Require(M01FirstContactCameraMinimapEvidence.ReducedMotionBlendSeconds == 0f,
            "Reduced-motion camera transition must be an immediate cut.");
    }

    private static void ValidateSubjects(
        OperationMapCameraConfig config, float aspect, IReadOnlyList<Vector3> subjects, Rect safeArea)
    {
        GameObject owner = new("M01DC012CameraValidation", typeof(Camera));
        try
        {
            Camera camera = owner.GetComponent<Camera>();
            owner.transform.SetPositionAndRotation(config.Position, Quaternion.Euler(config.EulerAngles));
            camera.orthographic = config.Orthographic;
            camera.fieldOfView = config.FieldOfView;
            camera.aspect = aspect;
            foreach (Vector3 subject in subjects)
            {
                Vector3 viewport = camera.WorldToViewportPoint(subject);
                Require(viewport.z > 0f && safeArea.Contains(new Vector2(viewport.x, viewport.y)),
                    $"{config.CameraId} loses subject {subject} at aspect {aspect:0.###}: {viewport}.");
            }
        }
        finally { UnityEngine.Object.DestroyImmediate(owner); }
    }

    private static void SetCamera(SerializedProperty property, OperationMapCameraConfig value)
    {
        property.FindPropertyRelative("cameraId").stringValue = value.CameraId;
        property.FindPropertyRelative("position").vector3Value = value.Position;
        property.FindPropertyRelative("eulerAngles").vector3Value = value.EulerAngles;
        property.FindPropertyRelative("orthographic").boolValue = value.Orthographic;
        property.FindPropertyRelative("fieldOfView").floatValue = value.FieldOfView;
        property.FindPropertyRelative("orthographicSize").floatValue = value.OrthographicSize;
        property.FindPropertyRelative("clampToCameraBounds").boolValue = value.ClampToCameraBounds;
    }

    private static void SetMinimap(SerializedProperty property, OperationMapMinimapConfig value)
    {
        property.FindPropertyRelative("minimapId").stringValue = value.MinimapId;
        property.FindPropertyRelative("projectionOrigin").vector3Value = value.ProjectionOrigin;
        property.FindPropertyRelative("projectionSize").vector2Value = value.ProjectionSize;
        property.FindPropertyRelative("orientationDegrees").floatValue = value.OrientationDegrees;
    }

    private static string CameraAuthorityText() =>
        $"{Planning.CameraId}|{Planning.Position}|{Planning.EulerAngles}|{Planning.FieldOfView}|" +
        $"{Battle.CameraId}|{Battle.Position}|{Battle.EulerAngles}|{Battle.FieldOfView}|" +
        $"{Minimap.MinimapId}|{Minimap.ProjectionOrigin}|{Minimap.ProjectionSize}|{Minimap.OrientationDegrees}";
    private static bool Contains(Vector3 min, Vector3 max, Vector3 value) =>
        value.x >= min.x && value.y >= min.y && value.z >= min.z &&
        value.x <= max.x && value.y <= max.y && value.z <= max.z;
    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
