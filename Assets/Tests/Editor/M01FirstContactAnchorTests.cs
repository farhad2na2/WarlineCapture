using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using Game.Components;
using Game.Configs;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;

public static class M01FirstContactAnchorTests
{
    private const string MapPath =
        "Assets/Game/Configs/OperationMaps/Chapter01/OperationMap_Ch01_DistrictEdge01.asset";
    private const string ScenarioPath =
        "Assets/Game/Configs/Scenarios/Chapter01/ScenarioSetup_Ch01_M01_FirstContact.asset";
    private const string SurfacePath = "Assets/Game/Data/MapSurfaces/Match_Map_MapSurfaceData.asset";
    private const string ReportPath =
        "Design/AgentReports/M01FirstContact/m01dc_013_anchor_manifest.json";
    private const string Marker = "[M01FirstContactAnchorValidation] result=Passed tests=13";
    private const float ConservativePatrolSpeed = 5f;
    private const float RequiredPostContextSeconds = 6f;

    private static readonly AnchorSpec[] Specs =
    {
        new("anchor.ch01.m01.player_spawn", OperationMapAnchorKind.Deployment, new int2(1792, 716), 5f, 0f),
        new("anchor.ch01.m01.camera_start", OperationMapAnchorKind.Camera, new int2(1792, 768), 1f, 0f),
        new("anchor.ch01.m01.move_target", OperationMapAnchorKind.Objective, new int2(1792, 744), 3f, 0f),
        new("anchor.ch01.m01.patrol_spawn", OperationMapAnchorKind.Spawn, new int2(1792, 806), 4f, 180f),
        new("anchor.ch01.m01.patrol_route_a", OperationMapAnchorKind.Lane, new int2(1792, 792), 2f, 180f),
        new("anchor.ch01.m01.patrol_route_b", OperationMapAnchorKind.Lane, new int2(1792, 778), 2f, 180f),
        new("anchor.ch01.m01.patrol_route_c", OperationMapAnchorKind.Lane, new int2(1792, 764), 2f, 180f),
        new("anchor.ch01.m01.patrol_objective", OperationMapAnchorKind.Hostile, new int2(1792, 754), 3f, 180f),
        new("anchor.ch01.m01.civilian_safe_zone", OperationMapAnchorKind.Civilian, new int2(1840, 824), 7f, 45f),
        new("anchor.ch01.m01.civilian_evacuation", OperationMapAnchorKind.Civilian, new int2(1870, 842), 7f, 45f),
        new("anchor.ch01.m01.minimap_start", OperationMapAnchorKind.Minimap, new int2(1792, 768), 1f, 0f)
    };

    public static void RunFocusedValidation()
    {
        try
        {
            OperationMapDefinition map = Load<OperationMapDefinition>(MapPath);
            ScenarioSetupConfig scenario = Load<ScenarioSetupConfig>(ScenarioPath);
            MapSurfaceDataAsset surface = Load<MapSurfaceDataAsset>(SurfacePath);
            Require(map != null && scenario != null && surface != null, "M01 map, scenario, and surface are required.");
            Require(surface.TryCreateRuntimeBlobAsset(Allocator.Temp, out BlobAssetReference<MapSurfaceBlob> blob),
                "Accepted surface could not create a runtime blob.");
            try
            {
                OperationMapAnchorConfig[] anchors = ResolveAnchors(ref blob.Value, map);
                Author(map, anchors);
                ValidateScenarioRequirements(scenario, anchors);
                ValidateMetadataAndUniqueness(map, anchors);
                ValidateSurfaceClearance(ref blob.Value, map, anchors);
                ValidateUnitSpacing(anchors);
                ValidatePatrolTiming(scenario, anchors);
                WriteReport(map, scenario, anchors);
            }
            finally { blob.Dispose(); }
            Debug.Log(Marker);
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            Debug.Log("[M01FirstContactAnchorValidation] result=Failed");
            throw;
        }
    }

    private static OperationMapAnchorConfig[] ResolveAnchors(ref MapSurfaceBlob blob, OperationMapDefinition map)
    {
        List<OperationMapAnchorConfig> anchors = new(Specs.Length);
        foreach (AnchorSpec spec in Specs)
        {
            int2 cell = FindNearestClear(ref blob, map, spec.Desired, spec.Radius, anchors);
            MapSurfaceSample sample = Sample(ref blob, cell);
            anchors.Add(new OperationMapAnchorConfig(spec.Id, spec.Kind,
                new Vector3(cell.x, sample.Height, cell.y), new Vector3(0f, spec.Yaw, 0f), spec.Radius));
        }
        return anchors.ToArray();
    }

    private static int2 FindNearestClear(
        ref MapSurfaceBlob blob,
        OperationMapDefinition map,
        int2 desired,
        float radius,
        IReadOnlyList<OperationMapAnchorConfig> existing)
    {
        for (int ring = 0; ring <= 18; ring++)
        for (int dz = -ring; dz <= ring; dz++)
        for (int dx = -ring; dx <= ring; dx++)
        {
            if (ring > 0 && math.abs(dx) != ring && math.abs(dz) != ring) continue;
            int2 cell = desired + new int2(dx, dz);
            if (!InsidePlayable(map, cell) || !HasClearDisk(ref blob, cell, radius)) continue;
            bool separated = true;
            foreach (OperationMapAnchorConfig other in existing)
            {
                float required = math.max(2f, radius + other.Radius + 1f);
                if (math.distance(new float2(cell.x, cell.y), new float2(other.Position.x, other.Position.z)) < required)
                {
                    separated = false;
                    break;
                }
            }
            if (separated) return cell;
        }
        throw new InvalidOperationException($"No fully clear anchor disk found near {desired} with radius {radius}.");
    }

    private static bool HasClearDisk(ref MapSurfaceBlob blob, int2 center, float radius)
    {
        int extent = Mathf.CeilToInt(radius);
        for (int z = -extent; z <= extent; z++)
        for (int x = -extent; x <= extent; x++)
        {
            if (x * x + z * z > radius * radius) continue;
            MapSurfaceSample sample = Sample(ref blob, center + new int2(x, z));
            if (!IsSafeInfantry(sample)) return false;
        }
        return true;
    }

    private static void Author(OperationMapDefinition map, OperationMapAnchorConfig[] anchors)
    {
        SerializedObject serialized = new(map);
        SerializedProperty property = serialized.FindProperty("anchors");
        property.arraySize = anchors.Length;
        for (int index = 0; index < anchors.Length; index++)
        {
            OperationMapAnchorConfig anchor = anchors[index];
            SerializedProperty item = property.GetArrayElementAtIndex(index);
            item.FindPropertyRelative("anchorId").stringValue = anchor.AnchorId;
            item.FindPropertyRelative("kind").enumValueIndex = (int)anchor.Kind;
            item.FindPropertyRelative("position").vector3Value = anchor.Position;
            item.FindPropertyRelative("eulerAngles").vector3Value = anchor.EulerAngles;
            item.FindPropertyRelative("radius").floatValue = anchor.Radius;
            item.FindPropertyRelative("factionId").intValue = anchor.FactionId;
            item.FindPropertyRelative("laneIndex").intValue = anchor.LaneIndex;
        }
        serialized.FindProperty("contentHash").stringValue = Sha256(AnchorAuthority(anchors));
        serialized.FindProperty("generatedMetadataHash").stringValue =
            Sha256("m01dc-013|surface-aware-anchors-v1|accepted-old-market-window");
        serialized.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(map);
        AssetDatabase.SaveAssets();
    }

    private static void ValidateScenarioRequirements(
        ScenarioSetupConfig scenario,
        OperationMapAnchorConfig[] anchors)
    {
        Require(scenario.RequiredAnchors.Length == Specs.Length, "Scenario no longer requires exactly eleven anchors.");
        foreach (ScenarioAnchorRequirementConfig requirement in scenario.RequiredAnchors)
        {
            int matches = 0;
            foreach (OperationMapAnchorConfig anchor in anchors)
                if (anchor.AnchorId == requirement.AnchorId && anchor.Kind == requirement.Kind) matches++;
            Require(matches == 1, $"Required anchor {requirement.AnchorId}/{requirement.Kind} resolves {matches} times.");
        }
    }

    private static void ValidateMetadataAndUniqueness(
        OperationMapDefinition map,
        OperationMapAnchorConfig[] anchors)
    {
        Require(map.TryValidateMetadata(out string error), error);
        HashSet<string> ids = new(StringComparer.Ordinal);
        foreach (OperationMapAnchorConfig anchor in anchors)
        {
            Require(ids.Add(anchor.AnchorId), $"Duplicate anchor identity: {anchor.AnchorId}.");
            Require(anchor.TryValidate(out error), error);
            Require(InsidePlayable(map, new int2(Mathf.RoundToInt(anchor.Position.x), Mathf.RoundToInt(anchor.Position.z))),
                $"Anchor {anchor.AnchorId} left the Old Market playable window.");
        }
    }

    private static void ValidateSurfaceClearance(
        ref MapSurfaceBlob blob,
        OperationMapDefinition map,
        OperationMapAnchorConfig[] anchors)
    {
        foreach (OperationMapAnchorConfig anchor in anchors)
        {
            int2 cell = new(Mathf.RoundToInt(anchor.Position.x), Mathf.RoundToInt(anchor.Position.z));
            Require(HasClearDisk(ref blob, cell, anchor.Radius), $"Anchor {anchor.AnchorId} lost clearance.");
            MapSurfaceSample sample = Sample(ref blob, cell);
            Require(math.abs(sample.Height - anchor.Position.y) <= 0.001f, $"Anchor {anchor.AnchorId} height drifted.");
        }
    }

    private static void ValidateUnitSpacing(OperationMapAnchorConfig[] anchors)
    {
        string[] unitAnchors =
        {
            "anchor.ch01.m01.player_spawn", "anchor.ch01.m01.patrol_spawn",
            "anchor.ch01.m01.civilian_safe_zone", "anchor.ch01.m01.civilian_evacuation"
        };
        for (int i = 0; i < unitAnchors.Length; i++)
        for (int j = i + 1; j < unitAnchors.Length; j++)
        {
            OperationMapAnchorConfig a = Find(anchors, unitAnchors[i]);
            OperationMapAnchorConfig b = Find(anchors, unitAnchors[j]);
            Require(Vector3.Distance(a.Position, b.Position) > a.Radius + b.Radius + 4f,
                $"Unit-bearing anchors overlap: {a.AnchorId} and {b.AnchorId}.");
        }
    }

    private static void ValidatePatrolTiming(ScenarioSetupConfig scenario, OperationMapAnchorConfig[] anchors)
    {
        Require(scenario.PatrolRoutes.Length == 1, "M01 must have exactly one patrol route.");
        ScenarioPatrolRouteConfig route = scenario.PatrolRoutes[0];
        Require(route.StartDelayMilliseconds >= scenario.EncounterStartMilliseconds,
            "Patrol begins before the encounter grants player context.");
        OperationMapAnchorConfig patrol = Find(anchors, "anchor.ch01.m01.patrol_spawn");
        OperationMapAnchorConfig civilian = Find(anchors, "anchor.ch01.m01.civilian_safe_zone");
        float directSeconds = Vector3.Distance(patrol.Position, civilian.Position) / ConservativePatrolSpeed;
        float earliestCivilianSeconds = route.StartDelayMilliseconds / 1000f + directSeconds;
        Require(earliestCivilianSeconds >= scenario.EncounterStartMilliseconds / 1000f + RequiredPostContextSeconds,
            "Patrol could reach civilian presentation before sufficient player-control context.");
        foreach (string anchorId in route.AnchorIds)
            Require(Vector3.Distance(Find(anchors, anchorId).Position, civilian.Position) >= 36f,
                $"Patrol waypoint {anchorId} violates the civilian separation boundary.");
    }

    private static void WriteReport(
        OperationMapDefinition map,
        ScenarioSetupConfig scenario,
        OperationMapAnchorConfig[] anchors)
    {
        StringBuilder rows = new();
        for (int index = 0; index < anchors.Length; index++)
        {
            OperationMapAnchorConfig anchor = anchors[index];
            if (index > 0) rows.Append(",\n");
            rows.Append("    {\"id\":\"").Append(anchor.AnchorId).Append("\",\"kind\":\"")
                .Append(anchor.Kind).Append("\",\"position\":[")
                .Append(F(anchor.Position.x)).Append(',').Append(F(anchor.Position.y)).Append(',')
                .Append(F(anchor.Position.z)).Append("],\"yaw\":").Append(F(anchor.EulerAngles.y))
                .Append(",\"clearanceRadius\":").Append(F(anchor.Radius)).Append('}');
        }
        ScenarioPatrolRouteConfig route = scenario.PatrolRoutes[0];
        OperationMapAnchorConfig patrol = Find(anchors, "anchor.ch01.m01.patrol_spawn");
        OperationMapAnchorConfig civilian = Find(anchors, "anchor.ch01.m01.civilian_safe_zone");
        float earliest = route.StartDelayMilliseconds / 1000f +
                         Vector3.Distance(patrol.Position, civilian.Position) / ConservativePatrolSpeed;
        string json = $@"{{
  ""artifactId"":""m01dc-013-anchor-manifest-v1"", ""taskId"":""M01DC-013"", ""result"":""Passed"",
  ""operationMapId"":""{map.OperationMapId}"", ""scenarioId"":""{scenario.ScenarioId}"",
  ""anchorCount"":{anchors.Length}, ""surfaceAuthority"":""accepted physical source; unmodified"",
  ""anchors"": [
{rows}
  ],
  ""patrolTiming"":{{""encounterStartMs"":{scenario.EncounterStartMilliseconds},""patrolDelayMs"":{route.StartDelayMilliseconds},""conservativeSpeedMps"":{F(ConservativePatrolSpeed)},""earliestDirectCivilianSeconds"":{F(earliest)},""requiredPostContextSeconds"":{F(RequiredPostContextSeconds)}}},
  ""validation"":""{Marker}""
}}";
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(ReportPath)) ?? throw new InvalidOperationException());
        File.WriteAllText(ReportPath, json, new UTF8Encoding(false));
    }

    private static string AnchorAuthority(OperationMapAnchorConfig[] anchors)
    {
        StringBuilder value = new("opmap.ch01.district_edge_01|camera-minimap-v1|");
        foreach (OperationMapAnchorConfig anchor in anchors)
            value.Append(anchor.AnchorId).Append('|').Append(anchor.Kind).Append('|').Append(anchor.Position)
                .Append('|').Append(anchor.EulerAngles).Append('|').Append(anchor.Radius).Append(';');
        return value.ToString();
    }
    private static OperationMapAnchorConfig Find(OperationMapAnchorConfig[] anchors, string id)
    {
        foreach (OperationMapAnchorConfig anchor in anchors) if (anchor.AnchorId == id) return anchor;
        throw new InvalidOperationException($"Missing anchor: {id}.");
    }
    private static MapSurfaceSample Sample(ref MapSurfaceBlob blob, int2 cell)
    {
        Require(MapSurfaceBlobAccess.TryGetPrimarySurface(ref blob, cell, out MapSurfaceSample sample),
            $"Surface cell {cell} did not resolve.");
        return sample;
    }
    private static bool IsSafeInfantry(MapSurfaceSample sample) =>
        (sample.MovementMask & MapSurfaceMovementMask.Infantry) != 0 &&
        sample.SurfaceType != MapSurfaceType.Blocked && sample.SurfaceType != MapSurfaceType.BridgeDeck &&
        (sample.Flags & MapSurfaceFlags.Bridge) == 0;
    private static bool InsidePlayable(OperationMapDefinition map, int2 cell) =>
        cell.x >= map.Bounds.PlayableMin.x && cell.y >= map.Bounds.PlayableMin.z &&
        cell.x <= map.Bounds.PlayableMax.x && cell.y <= map.Bounds.PlayableMax.z;
    private static T Load<T>(string path) where T : UnityEngine.Object => AssetDatabase.LoadAssetAtPath<T>(path);
    private static string Sha256(string value)
    {
        using SHA256 hash = SHA256.Create();
        return BitConverter.ToString(hash.ComputeHash(Encoding.UTF8.GetBytes(value))).Replace("-", string.Empty).ToLowerInvariant();
    }
    private static string F(float value) => value.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture);
    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }

    private readonly struct AnchorSpec
    {
        public AnchorSpec(string id, OperationMapAnchorKind kind, int2 desired, float radius, float yaw)
        { Id = id; Kind = kind; Desired = desired; Radius = radius; Yaw = yaw; }
        public string Id { get; }
        public OperationMapAnchorKind Kind { get; }
        public int2 Desired { get; }
        public float Radius { get; }
        public float Yaw { get; }
    }
}
