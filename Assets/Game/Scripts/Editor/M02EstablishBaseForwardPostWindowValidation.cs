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

namespace Game.Editor
{
    public static class M02EstablishBaseForwardPostWindowValidation
    {
        public const string DefinitionPath =
            "Assets/Game/Configs/OperationMaps/Chapter01/OperationMap_Ch01_ForwardPost01.asset";
        public const string SourceDefinitionPath = "Assets/Game/Configs/OperationMaps/Candidates/" +
            "OperationMap_Compatibility_DesertBase01_DenseCity_EntityScene_Candidate.asset";
        public const string BuildingPlacementsPath =
            "Assets/Game/Configs/OperationMaps/OperationMap_Compatibility_DesertBase01_BuildingPlacements.asset";
        public const string RenderDatabasePath =
            "Assets/Game/GeneratedOperationMapEntityPresentationCandidate/VirtualizedPresentation/" +
            "OperationMapRenderDatabaseBakeConfig.asset";
        public const string SourceDefinitionSha256 =
            "f91b737280d8950d97264b54589b963f605a8d8911a0f4e17397bef667e4eba6";
        public const string BuildingPlacementsSha256 =
            "f5d54abe4dca19b4b2deca889f46fb8196bef98e0e4ee7cb3daa511a2606358b";
        public const string MapId = "opmap.ch01.forward_post_01";
        public const string PlanningCameraId = "camera.ch01.m02.forward_post_planning";
        public const string BattleCameraId = "camera.ch01.m02.forward_post_battle";
        public const string MinimapId = "minimap.ch01.m02.forward_post";
        public const int ExpectedAnchorCount = 14;

        public static readonly RectInt PlayableWindow = new(780, 270, 320, 200);
        public static readonly RectInt BuildLotSearch = new(1004, 370, 24, 14);
        public static readonly Vector2Int BuildLotSize = new(24, 14);

        private static readonly RectInt MilitaryBaseOperationalCore = new(820, 340, 200, 105);
        private static readonly RectInt RejectedOldMarketWindow = new(1672, 680, 240, 176);

        private const string Marker =
            "[M02EstablishBaseForwardPostWindowValidation] result=Passed tests=10";

        [MenuItem("Game/Campaign/M02/Build And Validate Forward Post Map")]
        public static void RunFocusedValidation()
        {
            try
            {
                OperationMapDefinition source = Load<OperationMapDefinition>(SourceDefinitionPath);
                MapSurfaceDataAsset surface = Load<MapSurfaceDataAsset>(
                    AssetDatabase.GUIDToAssetPath(source.MapSurfaceDataReference.AssetGUID));
                MapBuildingPlacementConfig placements =
                    Load<MapBuildingPlacementConfig>(BuildingPlacementsPath);
                OperationMapRenderDatabaseBakeConfig renderDatabase =
                    Load<OperationMapRenderDatabaseBakeConfig>(RenderDatabasePath);

                ValidateProtectedPhysicalContent(source);
                Require(surface.TryCreateRuntimeBlobAsset(Allocator.Temp, out BlobAssetReference<MapSurfaceBlob> blob),
                    "Accepted map surface payload could not be opened.");
                using (blob)
                {
                    RectInt buildLot = SelectBuildLot(ref blob.Value, placements, renderDatabase);
                    OperationMapDefinition logical = LoadOrCreateDefinition();
                    PopulateLogicalDefinition(logical, source, ref blob.Value, buildLot);
                    ValidateLogicalDefinition(
                        logical, source, ref blob.Value, placements, renderDatabase, buildLot);
                    Debug.Log($"{Marker} buildLot={buildLot.xMin},{buildLot.yMin},{buildLot.width},{buildLot.height}");
                }
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                Debug.LogError("[M02EstablishBaseForwardPostWindowValidation] result=Failed");
                throw;
            }
        }

        public static RectInt ValidateCurrentDefinition()
        {
            OperationMapDefinition source = Load<OperationMapDefinition>(SourceDefinitionPath);
            OperationMapDefinition logical = Load<OperationMapDefinition>(DefinitionPath);
            MapSurfaceDataAsset surface = Load<MapSurfaceDataAsset>(
                AssetDatabase.GUIDToAssetPath(source.MapSurfaceDataReference.AssetGUID));
            MapBuildingPlacementConfig placements = Load<MapBuildingPlacementConfig>(BuildingPlacementsPath);
            OperationMapRenderDatabaseBakeConfig renderDatabase =
                Load<OperationMapRenderDatabaseBakeConfig>(RenderDatabasePath);
            Require(surface.TryCreateRuntimeBlobAsset(Allocator.Temp, out BlobAssetReference<MapSurfaceBlob> blob),
                "Accepted map surface payload could not be opened.");
            using (blob)
            {
                RectInt buildLot = BuildLotFromAnchor(logical);
                ValidateLogicalDefinition(
                    logical, source, ref blob.Value, placements, renderDatabase, buildLot);
                return buildLot;
            }
        }

        private static void ValidateProtectedPhysicalContent(OperationMapDefinition source)
        {
            Require(Sha256File(SourceDefinitionPath) == SourceDefinitionSha256,
                "Accepted dense-city source definition changed.");
            Require(Sha256File(BuildingPlacementsPath) == BuildingPlacementsSha256,
                "Accepted dense-city building-placement evidence changed.");
            Require(source.OperationMapId == "opmap.skirmish.desert_base_01" &&
                    source.SourceIdentityHash == "2a2a791e8292a4f458bd603ceb86598aa0ba2ca82db41323bf5bf7c748ec6900" &&
                    source.ContentHash == "2713962f0faa2dae49805e1b7e3a1673199a2cca915334d11421b354cd8f591c",
                "Accepted dense-city source identity drifted.");
        }

        private static RectInt SelectBuildLot(
            ref MapSurfaceBlob surface,
            MapBuildingPlacementConfig placements,
            OperationMapRenderDatabaseBakeConfig renderDatabase)
        {
            int halfWidth = BuildLotSize.x / 2;
            int halfHeight = BuildLotSize.y / 2;
            for (int z = BuildLotSearch.yMin + halfHeight;
                 z <= BuildLotSearch.yMax - halfHeight;
                 z += 2)
            {
                for (int x = BuildLotSearch.xMin + halfWidth;
                     x <= BuildLotSearch.xMax - halfWidth;
                     x += 2)
                {
                    RectInt lot = new(x - halfWidth, z - halfHeight, BuildLotSize.x, BuildLotSize.y);
                    if (IsBuildLotSurfaceValid(ref surface, lot) &&
                        !OverlapsAuthoredPlacement(lot, placements) &&
                        !OverlapsDenseCityPresentation(lot, renderDatabase))
                        return lot;
                }
            }

            throw new InvalidOperationException(
                $"The reviewed military-base apron contains no valid " +
                $"{BuildLotSize.x}x{BuildLotSize.y} Barracks lot.");
        }

        private static Bounds TransformBounds(Bounds localBounds, Matrix4x4 matrix)
        {
            Vector3 center = matrix.MultiplyPoint3x4(localBounds.center);
            Vector3 extents = localBounds.extents;
            Vector3 axisX = matrix.MultiplyVector(new Vector3(extents.x, 0f, 0f));
            Vector3 axisY = matrix.MultiplyVector(new Vector3(0f, extents.y, 0f));
            Vector3 axisZ = matrix.MultiplyVector(new Vector3(0f, 0f, extents.z));
            Vector3 worldExtents = new(
                Mathf.Abs(axisX.x) + Mathf.Abs(axisY.x) + Mathf.Abs(axisZ.x),
                Mathf.Abs(axisX.y) + Mathf.Abs(axisY.y) + Mathf.Abs(axisZ.y),
                Mathf.Abs(axisX.z) + Mathf.Abs(axisY.z) + Mathf.Abs(axisZ.z));
            return new Bounds(center, worldExtents * 2f);
        }

        private static bool IsBuildLotSurfaceValid(ref MapSurfaceBlob surface, RectInt lot)
        {
            float minimumHeight = float.MaxValue;
            float maximumHeight = float.MinValue;
            for (int z = lot.yMin; z < lot.yMax; z++)
            for (int x = lot.xMin; x < lot.xMax; x++)
            {
                if (!MapSurfaceBlobAccess.TryGetPrimarySurface(
                        ref surface, new int2(x, z), out MapSurfaceSample sample) ||
                    (sample.MovementMask & MapSurfaceMovementMask.BuildingPlacement) == 0 ||
                    sample.SurfaceType == MapSurfaceType.Blocked ||
                    sample.SurfaceType == MapSurfaceType.Road ||
                    sample.SurfaceType == MapSurfaceType.DirtRoad ||
                    sample.SurfaceType == MapSurfaceType.Highway ||
                    sample.SurfaceType == MapSurfaceType.BridgeDeck ||
                    (sample.Flags & (MapSurfaceFlags.Road | MapSurfaceFlags.Bridge |
                                     MapSurfaceFlags.Highway | MapSurfaceFlags.Reserved)) != 0)
                    return false;

                minimumHeight = Mathf.Min(minimumHeight, sample.Height);
                maximumHeight = Mathf.Max(maximumHeight, sample.Height);
            }

            return maximumHeight - minimumHeight <= 0.5f;
        }

        private static bool OverlapsAuthoredPlacement(RectInt lot, MapBuildingPlacementConfig placements)
        {
            Rect expanded = new(lot.xMin - 8f, lot.yMin - 8f, lot.width + 16f, lot.height + 16f);
            foreach (MapBuildingPlacementConfigEntry placement in placements.Placements)
            {
                Vector3 center = placement.WorldCenter;
                if (expanded.Contains(new Vector2(center.x, center.z)))
                    return true;
            }

            return false;
        }

        private static bool OverlapsDenseCityPresentation(
            RectInt lot,
            OperationMapRenderDatabaseBakeConfig renderDatabase)
        {
            Rect footprint = new(lot.xMin - 2f, lot.yMin - 2f, lot.width + 4f, lot.height + 4f);
            foreach (OperationMapRenderPlacementConfigRecord placement in renderDatabase.Placements)
            {
                if (placement.SemanticCategory == DenseCityPresentationSemanticCategory.Horizon)
                    continue;

                OperationMapRenderPrototypeConfigRecord prototype =
                    renderDatabase.Prototypes[placement.PrototypeIndex];
                Bounds bounds = TransformBounds(prototype.CombinedLocalBounds, placement.WorldMatrix);
                if (footprint.Overlaps(new Rect(
                        bounds.min.x,
                        bounds.min.z,
                        bounds.size.x,
                        bounds.size.z)))
                    return true;
            }

            return false;
        }

        private static void PopulateLogicalDefinition(
            OperationMapDefinition logical,
            OperationMapDefinition source,
            ref MapSurfaceBlob surface,
            RectInt buildLot)
        {
            AnchorSeed[] seeds =
            {
                new("anchor.ch01.m02.friendly_spawn", OperationMapAnchorKind.Deployment, 920, 425, 8f, 1),
                new("anchor.ch01.m02.camera_start", OperationMapAnchorKind.Camera, 935, 390, 4f),
                new("anchor.ch01.m02.forward_post", OperationMapAnchorKind.Base, 937, 348, 12f, 1),
                new("anchor.ch01.m02.build_lot", OperationMapAnchorKind.Build,
                    buildLot.center.x, buildLot.center.y, Mathf.Max(buildLot.width, buildLot.height) * 0.5f, 1),
                new("anchor.ch01.m02.hostile_spawn", OperationMapAnchorKind.Spawn, 800, 300, 8f, 2),
                new("anchor.ch01.m02.lane_a", OperationMapAnchorKind.Lane, 825, 315, 5f, 2, 0),
                new("anchor.ch01.m02.lane_b", OperationMapAnchorKind.Lane, 850, 330, 5f, 2, 1),
                new("anchor.ch01.m02.lane_c", OperationMapAnchorKind.Lane, 885, 342, 5f, 2, 2),
                new("anchor.ch01.m02.defense_boundary", OperationMapAnchorKind.Hostile, 910, 350, 10f, 2),
                new("anchor.ch01.m02.civilian_edge", OperationMapAnchorKind.Civilian, 1060, 430, 10f),
                new("anchor.ch01.m02.civilian_evacuation", OperationMapAnchorKind.Civilian, 1080, 450, 8f),
                new("anchor.ch01.m02.minimap_start", OperationMapAnchorKind.Minimap, 935, 380, 3f),
                new("anchor.ch01.m02.resource_focus", OperationMapAnchorKind.Resource, 830, 375, 8f, 1),
                new("anchor.ch01.m02.comms_focus", OperationMapAnchorKind.Objective, 925, 360, 6f)
            };

            Vector3[] positions = new Vector3[seeds.Length];
            for (int index = 0; index < seeds.Length; index++)
                positions[index] = ResolveAnchorPosition(ref surface, seeds[index]);

            Vector3 planningPosition = new(995f, 95f, 468f);
            Vector3 battlePosition = new(952f, 36f, 430f);
            Vector3 planningTarget = Vector3.Lerp(positions[2], positions[3], 0.56f);
            Vector3 planningEuler = LookEuler(planningPosition, planningTarget);
            Vector3 battleEuler = LookEuler(battlePosition, Vector3.Lerp(positions[7], positions[8], 0.5f));

            SerializedObject target = new(logical);
            SerializedObject sourceSerialized = new(source);
            Copy(target, sourceSerialized, "gridMetadata", "surfaceMetadata", "navigationMetadata",
                "presentationKind", "renderResidencyMode", "sourceSceneReference",
                "optionalHeavyMetadataReference", "staticPresentationManifestReference",
                "mapSurfaceDataReference", "minimapRasterReference", "buildingPlacementsReference",
                "vehiclePlacementsReference");
            Set(target, "operationMapId", MapId);
            Set(target, "schemaVersion", 1);
            Set(target, "contentVersion", 1);
            Set(target, "sourceIdentityHash", source.SourceIdentityHash);
            Set(target, "contentHash", HashText(BuildCanonicalPayload(source, buildLot, positions)));
            Set(target, "generatedMetadataHash", HashText("m02eb-009|authored-military-base-v3|surface-v3"));

            SerializedProperty binding = target.FindProperty("sourceBinding");
            Set(binding, "sourceOperationMapId", source.OperationMapId);
            Set(binding, "sourceIdentityHash", source.SourceIdentityHash);
            Set(binding, "sourceContentHash", source.ContentHash);

            SerializedProperty bounds = target.FindProperty("bounds");
            Set(bounds, "worldMin", source.Bounds.WorldMin);
            Set(bounds, "worldMax", source.Bounds.WorldMax);
            Set(bounds, "playableMin", new Vector3(
                PlayableWindow.xMin, source.Bounds.PlayableMin.y, PlayableWindow.yMin));
            Set(bounds, "playableMax", new Vector3(
                PlayableWindow.xMax, source.Bounds.PlayableMax.y, PlayableWindow.yMax));
            Set(bounds, "cameraMin", new Vector3(PlayableWindow.xMin, 4f, PlayableWindow.yMin));
            Set(bounds, "cameraMax", new Vector3(
                PlayableWindow.xMax, source.Bounds.CameraMax.y, PlayableWindow.yMax));

            SetArray(target, "cameras", 2, (camera, index) =>
            {
                bool planning = index == 0;
                Set(camera, "cameraId", planning ? PlanningCameraId : BattleCameraId);
                Set(camera, "position", planning ? planningPosition : battlePosition);
                Set(camera, "eulerAngles", planning ? planningEuler : battleEuler);
                Set(camera, "orthographic", false);
                Set(camera, "fieldOfView", planning ? 58f : 42f);
                Set(camera, "orthographicSize", 5f);
                Set(camera, "clampToCameraBounds", true);
            });
            Set(target, "planningCameraId", PlanningCameraId);
            Set(target, "battleCameraId", BattleCameraId);

            SerializedProperty minimap = target.FindProperty("minimap");
            Set(minimap, "minimapId", MinimapId);
            Set(minimap, "projectionOrigin", new Vector3(PlayableWindow.xMin, 0f, PlayableWindow.yMin));
            Set(minimap, "projectionSize", new Vector2(PlayableWindow.width, PlayableWindow.height));
            Set(minimap, "orientationDegrees", 0f);

            SetArray(target, "anchors", seeds.Length, (anchor, index) =>
            {
                AnchorSeed seed = seeds[index];
                Set(anchor, "anchorId", seed.Id);
                Set(anchor, "kind", (int)seed.Kind);
                Set(anchor, "position", positions[index]);
                Set(anchor, "eulerAngles", Vector3.zero);
                Set(anchor, "radius", seed.Radius);
                Set(anchor, "factionId", seed.FactionId);
                Set(anchor, "laneIndex", seed.LaneIndex);
            });

            target.ApplyModifiedPropertiesWithoutUndo();
            Require(logical.TryValidateMetadata(out string error), error);
            Require(logical.TryValidateLocalContentReferences(out error), error);
            EditorUtility.SetDirty(logical);
            AssetDatabase.SaveAssets();
            NormalizeGeneratedYaml(DefinitionPath);
        }

        private static void ValidateLogicalDefinition(
            OperationMapDefinition logical,
            OperationMapDefinition source,
            ref MapSurfaceBlob surface,
            MapBuildingPlacementConfig placements,
            OperationMapRenderDatabaseBakeConfig renderDatabase,
            RectInt buildLot)
        {
            Require(logical.TryValidateMetadata(out string error), error);
            Require(logical.TryValidateLocalContentReferences(out error), error);
            Require(logical.OperationMapId == MapId, "M02 logical map identity is incorrect.");
            Require(logical.SourceBinding.SourceOperationMapId == source.OperationMapId &&
                    logical.SourceBinding.SourceIdentityHash == source.SourceIdentityHash &&
                    logical.SourceBinding.SourceContentHash == source.ContentHash,
                "M02 logical map does not bind the exact accepted physical source.");
            Require(logical.SourceSceneReference.AssetGUID == source.SourceSceneReference.AssetGUID &&
                    logical.MapSurfaceDataReference.AssetGUID == source.MapSurfaceDataReference.AssetGUID &&
                    logical.MinimapRasterReference.AssetGUID == source.MinimapRasterReference.AssetGUID,
                "M02 introduced a separate physical scene, surface, or minimap raster.");
            Require(logical.GridMetadata.AssetGuid == source.GridMetadata.AssetGuid &&
                    logical.GridMetadata.ContentHash == source.GridMetadata.ContentHash &&
                    logical.SurfaceMetadata.ContentHash == source.SurfaceMetadata.ContentHash &&
                    logical.NavigationMetadata.AuthoredSubSceneGuid == source.NavigationMetadata.AuthoredSubSceneGuid,
                "M02 canonical grid, surface, or navigation metadata drifted.");
            Require(logical.Bounds.PlayableMin.x == PlayableWindow.xMin &&
                    logical.Bounds.PlayableMin.z == PlayableWindow.yMin &&
                    logical.Bounds.PlayableMax.x == PlayableWindow.xMax &&
                    logical.Bounds.PlayableMax.z == PlayableWindow.yMax,
                "M02 playable bounds left the reviewed authored military-base district.");
            Require(logical.PlanningCameraId == PlanningCameraId &&
                    logical.BattleCameraId == BattleCameraId && logical.Cameras.Length == 2,
                "M02 camera identities are incomplete.");
            Require(logical.Minimap.MinimapId == MinimapId &&
                    logical.Minimap.ProjectionOrigin.x == PlayableWindow.xMin &&
                    logical.Minimap.ProjectionOrigin.z == PlayableWindow.yMin &&
                    logical.Minimap.ProjectionSize == new Vector2(PlayableWindow.width, PlayableWindow.height),
                "M02 minimap projection does not match the playable window.");
            Require(logical.Anchors.Length == ExpectedAnchorCount,
                "M02 logical map must own exactly 14 mission anchors.");
            Require(buildLot.size == BuildLotSize && IsBuildLotSurfaceValid(ref surface, buildLot) &&
                    !OverlapsAuthoredPlacement(buildLot, placements) &&
                    !OverlapsDenseCityPresentation(buildLot, renderDatabase),
                "M02 Barracks lot is no longer clear, flat, visible-content-free, and buildable.");
            ValidateMilitaryBaseSemanticBinding(logical, buildLot);
            ValidateAnchorRoute(logical);
            ValidateCameraSightlines(logical);
        }

        private static void ValidateMilitaryBaseSemanticBinding(
            OperationMapDefinition logical,
            RectInt buildLot)
        {
            string[] militaryCoreAnchors =
            {
                "anchor.ch01.m02.friendly_spawn",
                "anchor.ch01.m02.camera_start",
                "anchor.ch01.m02.forward_post",
                "anchor.ch01.m02.resource_focus",
                "anchor.ch01.m02.comms_focus"
            };
            foreach (string anchorId in militaryCoreAnchors)
            {
                Vector3 position = FindAnchor(logical, anchorId).Position;
                Vector2Int cell = new(Mathf.FloorToInt(position.x), Mathf.FloorToInt(position.z));
                Require(MilitaryBaseOperationalCore.Contains(cell),
                    $"M02 anchor '{anchorId}' left the authored military-base operational core.");
                Require(!RejectedOldMarketWindow.Contains(cell),
                    $"M02 anchor '{anchorId}' incorrectly targets the M01 Old Market/City Hall district.");
            }

            Require(BuildLotSearch.Contains(buildLot.min) &&
                    BuildLotSearch.Contains(new Vector2Int(buildLot.xMax - 1, buildLot.yMax - 1)),
                "M02 Barracks lot left the reviewed clear apron beside the authored military base.");

            Vector3 cameraStart = FindAnchor(logical, "anchor.ch01.m02.camera_start").Position;
            Vector3 forwardPost = FindAnchor(logical, "anchor.ch01.m02.forward_post").Position;
            Vector3 buildLotFocus = FindAnchor(logical, "anchor.ch01.m02.build_lot").Position;
            Vector3 cinematicFocus = Vector3.Lerp(
                forwardPost,
                buildLotFocus,
                0.56f);
            Require(HorizontalDistance(cameraStart, cinematicFocus) <= 65f,
                "M02 opening focus must remain a local zoom over the authored military-base area.");
            Vector2Int focusCell = new(
                Mathf.FloorToInt(cinematicFocus.x),
                Mathf.FloorToInt(cinematicFocus.z));
            Require(MilitaryBaseOperationalCore.Contains(focusCell),
                "M02 opening focus left the authored military-base operational core.");
        }

        private static void ValidateAnchorRoute(OperationMapDefinition logical)
        {
            Vector3 hostile = FindAnchor(logical, "anchor.ch01.m02.hostile_spawn").Position;
            Vector3 laneA = FindAnchor(logical, "anchor.ch01.m02.lane_a").Position;
            Vector3 laneB = FindAnchor(logical, "anchor.ch01.m02.lane_b").Position;
            Vector3 laneC = FindAnchor(logical, "anchor.ch01.m02.lane_c").Position;
            Vector3 defense = FindAnchor(logical, "anchor.ch01.m02.defense_boundary").Position;
            Vector3 forwardPost = FindAnchor(logical, "anchor.ch01.m02.forward_post").Position;
            Vector3[] route = { hostile, laneA, laneB, laneC, defense, forwardPost };
            for (int index = 1; index < route.Length; index++)
            {
                float distance = HorizontalDistance(route[index - 1], route[index]);
                Require(distance >= 12f && distance <= 55f,
                    $"M02 defense-route segment {index - 1} has invalid spacing {distance:F2}.");
            }
        }

        private static void ValidateCameraSightlines(OperationMapDefinition logical)
        {
            OperationMapCameraConfig planning = FindCamera(logical, PlanningCameraId);
            OperationMapCameraConfig battle = FindCamera(logical, BattleCameraId);
            Vector3 buildLot = FindAnchor(logical, "anchor.ch01.m02.build_lot").Position;
            float halfWidth = BuildLotSize.x * 0.5f;
            float halfHeight = BuildLotSize.y * 0.5f;
            Require(IsVisible(planning, FindAnchor(logical, "anchor.ch01.m02.forward_post").Position) &&
                    IsVisible(planning, buildLot) &&
                    IsVisible(planning, buildLot + new Vector3(-halfWidth, 0f, -halfHeight)) &&
                    IsVisible(planning, buildLot + new Vector3(-halfWidth, 0f, halfHeight)) &&
                    IsVisible(planning, buildLot + new Vector3(halfWidth, 0f, -halfHeight)) &&
                    IsVisible(planning, buildLot + new Vector3(halfWidth, 0f, halfHeight)),
                "Planning camera does not frame the base and full Barracks lot.");
            Require(IsVisible(battle, FindAnchor(logical, "anchor.ch01.m02.defense_boundary").Position) &&
                    IsVisible(battle, FindAnchor(logical, "anchor.ch01.m02.lane_c").Position),
                "Battle camera does not frame the defense contact.");
        }

        private static bool IsVisible(OperationMapCameraConfig camera, Vector3 target)
        {
            Vector3 forward = Quaternion.Euler(camera.EulerAngles) * Vector3.forward;
            return Vector3.Angle(forward, target - camera.Position) <= camera.FieldOfView * 0.48f;
        }

        private static Vector3 ResolveAnchorPosition(ref MapSurfaceBlob surface, AnchorSeed seed)
        {
            int centerX = Mathf.RoundToInt(seed.X);
            int centerZ = Mathf.RoundToInt(seed.Z);
            for (int radius = 0; radius <= 18; radius++)
            {
                for (int z = centerZ - radius; z <= centerZ + radius; z++)
                for (int x = centerX - radius; x <= centerX + radius; x++)
                {
                    if (radius > 0 && x != centerX - radius && x != centerX + radius &&
                        z != centerZ - radius && z != centerZ + radius)
                        continue;
                    if (!PlayableWindow.Contains(new Vector2Int(x, z)) ||
                        !MapSurfaceBlobAccess.TryGetPrimarySurface(
                            ref surface, new int2(x, z), out MapSurfaceSample sample) ||
                        (sample.MovementMask & MapSurfaceMovementMask.Infantry) == 0 ||
                        sample.SurfaceType == MapSurfaceType.Blocked ||
                        sample.SurfaceType == MapSurfaceType.BridgeDeck ||
                        (sample.Flags & MapSurfaceFlags.Bridge) != 0)
                        continue;
                    return new Vector3(x + 0.5f, sample.Height, z + 0.5f);
                }
            }

            throw new InvalidOperationException($"M02 anchor '{seed.Id}' has no safe infantry surface.");
        }

        private static RectInt BuildLotFromAnchor(OperationMapDefinition logical)
        {
            Vector3 center = FindAnchor(logical, "anchor.ch01.m02.build_lot").Position;
            return new RectInt(
                Mathf.RoundToInt(center.x - BuildLotSize.x * 0.5f),
                Mathf.RoundToInt(center.z - BuildLotSize.y * 0.5f),
                BuildLotSize.x,
                BuildLotSize.y);
        }

        private static OperationMapAnchorConfig FindAnchor(OperationMapDefinition logical, string id)
        {
            foreach (OperationMapAnchorConfig anchor in logical.Anchors)
                if (anchor.AnchorId == id)
                    return anchor;
            throw new InvalidOperationException($"M02 logical map is missing anchor '{id}'.");
        }

        private static OperationMapCameraConfig FindCamera(OperationMapDefinition logical, string id)
        {
            foreach (OperationMapCameraConfig camera in logical.Cameras)
                if (camera.CameraId == id)
                    return camera;
            throw new InvalidOperationException($"M02 logical map is missing camera '{id}'.");
        }

        private static string BuildCanonicalPayload(
            OperationMapDefinition source,
            RectInt buildLot,
            IReadOnlyList<Vector3> positions)
        {
            StringBuilder value = new();
            value.Append(MapId).Append('|').Append(source.SourceIdentityHash).Append('|')
                .Append(source.ContentHash).Append('|').Append(PlayableWindow).Append('|')
                .Append(buildLot).Append('|').Append(PlanningCameraId).Append('|')
                .Append(BattleCameraId).Append('|').Append(MinimapId);
            for (int index = 0; index < positions.Count; index++)
                value.Append('|').Append(positions[index].x.ToString("R"))
                    .Append(',').Append(positions[index].y.ToString("R"))
                    .Append(',').Append(positions[index].z.ToString("R"));
            return value.ToString();
        }

        private static Vector3 LookEuler(Vector3 position, Vector3 target) =>
            Quaternion.LookRotation(target - position, Vector3.up).eulerAngles;

        private static float HorizontalDistance(Vector3 left, Vector3 right) =>
            Vector2.Distance(new Vector2(left.x, left.z), new Vector2(right.x, right.z));

        private static OperationMapDefinition LoadOrCreateDefinition()
        {
            OperationMapDefinition definition =
                AssetDatabase.LoadAssetAtPath<OperationMapDefinition>(DefinitionPath);
            if (definition != null)
                return definition;
            definition = ScriptableObject.CreateInstance<OperationMapDefinition>();
            AssetDatabase.CreateAsset(definition, DefinitionPath);
            return definition;
        }

        private static T Load<T>(string path) where T : UnityEngine.Object
        {
            T asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset == null)
                throw new InvalidOperationException($"Missing M02 dependency '{path}'.");
            return asset;
        }

        private static string Sha256File(string path)
        {
            using SHA256 sha = SHA256.Create();
            return BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(path)))
                .Replace("-", string.Empty).ToLowerInvariant();
        }

        private static string HashText(string value)
        {
            using SHA256 sha = SHA256.Create();
            return BitConverter.ToString(sha.ComputeHash(Encoding.UTF8.GetBytes(value)))
                .Replace("-", string.Empty).ToLowerInvariant();
        }

        private static void NormalizeGeneratedYaml(string path)
        {
            string yaml = File.ReadAllText(path);
            string[] lines = yaml.Split('\n');
            bool changed = false;
            StringBuilder normalized = new(yaml.Length);
            for (int index = 0; index < lines.Length; index++)
            {
                string line = lines[index].TrimEnd(' ', '\t');
                changed |= line.Length != lines[index].Length;
                normalized.Append(line);
                if (index < lines.Length - 1)
                    normalized.Append('\n');
            }

            if (changed)
                File.WriteAllText(path, normalized.ToString());
        }

        private static void Copy(SerializedObject target, SerializedObject source, params string[] names)
        {
            foreach (string name in names)
                target.CopyFromSerializedProperty(source.FindProperty(name));
        }

        private static void SetArray(
            SerializedObject target,
            string name,
            int size,
            Action<SerializedProperty, int> populate)
        {
            SerializedProperty array = target.FindProperty(name);
            array.arraySize = size;
            for (int index = 0; index < size; index++)
                populate(array.GetArrayElementAtIndex(index), index);
        }

        private static void Set(SerializedObject target, string name, string value) =>
            target.FindProperty(name).stringValue = value;
        private static void Set(SerializedObject target, string name, int value) =>
            target.FindProperty(name).intValue = value;
        private static void Set(SerializedProperty target, string name, string value) =>
            target.FindPropertyRelative(name).stringValue = value;
        private static void Set(SerializedProperty target, string name, int value) =>
            target.FindPropertyRelative(name).intValue = value;
        private static void Set(SerializedProperty target, string name, float value) =>
            target.FindPropertyRelative(name).floatValue = value;
        private static void Set(SerializedProperty target, string name, bool value) =>
            target.FindPropertyRelative(name).boolValue = value;
        private static void Set(SerializedProperty target, string name, Vector2 value) =>
            target.FindPropertyRelative(name).vector2Value = value;
        private static void Set(SerializedProperty target, string name, Vector3 value) =>
            target.FindPropertyRelative(name).vector3Value = value;

        private static void Require(bool condition, string message)
        {
            if (!condition)
                throw new InvalidOperationException(message);
        }

        private readonly struct AnchorSeed
        {
            public readonly string Id;
            public readonly OperationMapAnchorKind Kind;
            public readonly float X;
            public readonly float Z;
            public readonly float Radius;
            public readonly int FactionId;
            public readonly int LaneIndex;

            public AnchorSeed(
                string id,
                OperationMapAnchorKind kind,
                float x,
                float z,
                float radius,
                int factionId = -1,
                int laneIndex = -1)
            {
                Id = id;
                Kind = kind;
                X = x;
                Z = z;
                Radius = radius;
                FactionId = factionId;
                LaneIndex = laneIndex;
            }
        }
    }
}
