#if UNITY_EDITOR
using System;
using System.IO;
using System.Security.Cryptography;
using Game.Components;
using Game.Configs;
using Game.Runtime;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;

namespace Game.Editor
{
    public static class CityCrossroadsFloatingShelfValidation
    {
        public const string Marker = "[CityCrossroadsFloatingShelfValidation] result=Passed cases=18";
        private const string SurfacePath = "Assets/Game/Data/MapSurfaces/Match_Map_MapSurfaceData.asset";
        private const string MapPath = "Assets/Game/Configs/Skirmish/CityCrossroads/OperationMap_CityCrossroads.asset";
        private const string EntityPresentationPath =
            "Assets/Game/Scenes/OperationMaps/Skirmish/Candidates/opmap_skirmish_desert_base_01_entity_presentation_dense_city_candidate.unity";
        private const string SurfaceGuid = "12f517deb32ab49698acbfdaf7c3eac7";
        private const string SurfaceSha256 = "1402d769704008e254563ff7ecda835294db83afc2cee6d5bb456987f0392b4d";

        [MenuItem("Game/Skirmish/Validate City Crossroads Floating Shelf")]
        public static void RunFocusedValidation()
        {
            int cases = 0;
            void Check(bool condition, string reason)
            {
                if (!condition)
                    throw new InvalidOperationException(reason);
                cases++;
            }

            try
            {
                MapSurfaceDataAsset surface = AssetDatabase.LoadAssetAtPath<MapSurfaceDataAsset>(SurfacePath);
                OperationMapDefinition map = AssetDatabase.LoadAssetAtPath<OperationMapDefinition>(MapPath);
                Check(surface != null && map != null, "City Crossroads surface and map assets are required.");
                Check(AssetDatabase.AssetPathToGUID(SurfacePath) == SurfaceGuid, "Shared surface GUID drifted.");
                Check(Sha256File(SurfacePath) == SurfaceSha256, "Shared surface file hash drifted. Re-pin E0.1 evidence if a rebake is intentional.");
                Check(map.OperationMapId == SkirmishScenarioAssetsBuilder.MapId, "City Crossroads map identity drifted.");
                Check(map.MapSurfaceDataReference.AssetGUID == SurfaceGuid, "City Crossroads must keep the shared desert/city surface binding.");

                Check(surface.TryCreateRuntimeBlobAsset(Allocator.Temp, out BlobAssetReference<MapSurfaceBlob> blob),
                    "Shared surface blob failed to load.");
                try
                {
                    ref MapSurfaceBlob data = ref blob.Value;
                    Check(Sample(ref data, 1010, 710) < 1.5f && Sample(ref data, 1008, 708) < 1.5f,
                        "Northern City Crossroads shelf must sit on authored grade.");
                    Check(Sample(ref data, 1027, 481) < 1.5f &&
                        Sample(ref data, 1080, 600) < 1.5f &&
                        Sample(ref data, 1035, 482) < 1.5f,
                        "Civic City Crossroads shelves and leftover 3.9 m rim must sit on authored grade.");
                    Check(Sample(ref data, 1031, 540) < 1.5f &&
                        Sample(ref data, 1043, 488) < 1.5f &&
                        Sample(ref data, 965, 688) < 1.5f,
                        "Statue-side civic hall / plinth cells must sit on authored grade.");
                    Check(Sample(ref data, 1000, 655) < 1.5f &&
                        Sample(ref data, 1072, 520) < 1.5f &&
                        Sample(ref data, 1110, 600) < 1.5f,
                        "Leftover 2 m shelf skirts beside the collapsed stamps must sit on authored grade.");
                    Check(Sample(ref data, 1020, 750) < 0.75f && Sample(ref data, 1100, 400) < 0.75f,
                        "City Crossroads bases must remain near grade.");
                    Check(Sample(ref data, 768, 550) > 4f, "Skirmish mountain peak must remain raised.");

                    var component = new MapSurfaceComponent
                    {
                        SurfaceBlob = blob,
                        GridOrigin = surface.GridOrigin,
                        CellSize = surface.CellSize,
                        Dimensions = new int2(surface.Dimensions.x, surface.Dimensions.y),
                        HasSurfaceData = 1
                    };
                    Check(!BuildingPlacementAdapterCompositionSystemHelper.IsSkirmishFootprintLevel(
                            component, new RectInt(766, 550, 8, 8)) &&
                        BuildingPlacementAdapterCompositionSystemHelper.IsSkirmishFootprintLevel(
                            component, new RectInt(800, 600, 8, 8)) &&
                        BuildingPlacementAdapterCompositionSystemHelper.IsSkirmishFootprintLevel(
                            component, new RectInt(1020, 730, 8, 8)),
                        "Mountain rejection and CC/S1 flat lots must stay legal.");
                }
                finally
                {
                    blob.Dispose();
                }

                var hall = CityCrossroadsFloatingShelfBuildingCorrection.CivicHallPlate;
                var civicPlinth = CityCrossroadsFloatingShelfBuildingCorrection.CivicPlinthPlate;
                var northernPlinth = CityCrossroadsFloatingShelfBuildingCorrection.NorthernPlinthPlate;
                var mountain = new float3(768f, 9.1f, 550f);
                Check(CityCrossroadsFloatingShelfBuildingCorrection.TryCorrect(ref hall) &&
                    CityCrossroadsFloatingShelfBuildingCorrection.TryCorrect(ref civicPlinth) &&
                    CityCrossroadsFloatingShelfBuildingCorrection.TryCorrect(ref northernPlinth) &&
                    hall.y <= 0.02f && civicPlinth.y <= 0.02f && northernPlinth.y <= 0.02f &&
                    !CityCrossroadsFloatingShelfBuildingCorrection.TryCorrect(ref mountain) &&
                    mountain.y > 4f,
                    "Statue-side hall/plinth geometry must drop to grade; the mountain must stay raised.");

                Check(EntityPresentationAuthorsPlate(
                        CityCrossroadsFloatingShelfBuildingCorrection.CivicHallBuildingName,
                        CityCrossroadsFloatingShelfBuildingCorrection.CivicPlinthBuildingName,
                        CityCrossroadsFloatingShelfBuildingCorrection.NorthernPlinthBuildingName),
                    "Dense-city entity presentation must still author the statue-side hall and sand plinths.");

                var civicSquare = CityCrossroadsFloatingShelfBuildingCorrection.CivicGroundSquarePlate;
                var civicStatue = CityCrossroadsFloatingShelfBuildingCorrection.CivicStatue;
                var civicBase = CityCrossroadsFloatingShelfBuildingCorrection.CivicStatueBase;
                Check(CityCrossroadsFloatingShelfBuildingCorrection.TryCorrectResidentVisual(
                        civicSquare,
                        civicSquare.y,
                        out float civicDelta) &&
                    CityCrossroadsFloatingShelfBuildingCorrection.TryCorrectResidentVisual(
                        civicStatue,
                        civicStatue.y,
                        out float statueDelta) &&
                    CityCrossroadsFloatingShelfBuildingCorrection.TryCorrectResidentVisual(
                        civicBase,
                        civicBase.y,
                        out _) &&
                    civicDelta < -5f &&
                    math.abs(statueDelta - civicDelta) < 0.001f &&
                    civicSquare.y + civicDelta <= 0.02f &&
                    civicStatue.y + statueDelta > 4f &&
                    civicStatue.y + statueDelta < 5f,
                    "Civic sand tile must drop to grade; the golden statue must keep its pedestal height.");

                var northernSquare = CityCrossroadsFloatingShelfBuildingCorrection.NorthernGroundSquarePlate;
                var northernStatue = CityCrossroadsFloatingShelfBuildingCorrection.NorthernStatue;
                var mountainWorld = new float3(768f, 9.1f, 550f);
                var childFollower = CityCrossroadsFloatingShelfBuildingCorrection.CivicGroundSquarePlate;
                Check(CityCrossroadsFloatingShelfBuildingCorrection.TryCorrectResidentVisual(
                        northernSquare,
                        northernSquare.y,
                        out float northernDelta) &&
                    CityCrossroadsFloatingShelfBuildingCorrection.TryCorrectResidentVisual(
                        northernStatue,
                        northernStatue.y,
                        out _) &&
                    northernSquare.y + northernDelta <= 0.02f &&
                    !CityCrossroadsFloatingShelfBuildingCorrection.TryCorrectResidentVisual(
                        mountainWorld,
                        mountainWorld.y,
                        out _) &&
                    !CityCrossroadsFloatingShelfBuildingCorrection.TryCorrectResidentVisual(
                        childFollower,
                        0f,
                        out _),
                    "Northern base-join tile must drop; mountain and parented children must not.");

                Check(EntityPresentationAuthorsPlate(
                        CityCrossroadsFloatingShelfBuildingCorrection.CivicGroundSquareName,
                        CityCrossroadsFloatingShelfBuildingCorrection.CivicStatueName,
                        CityCrossroadsFloatingShelfBuildingCorrection.CivicStatueBaseName),
                    "Dense-city entity presentation must still author the civic sand tile and golden statue.");

                if (cases != 18)
                    throw new InvalidOperationException($"Expected 18 cases, executed {cases}.");
                Debug.Log(Marker);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                Debug.Log("[CityCrossroadsFloatingShelfValidation] result=Failed");
                throw;
            }
        }

        private static float Sample(ref MapSurfaceBlob blob, int x, int y)
        {
            if (!MapSurfaceBlobAccess.TryGetPrimarySurface(ref blob, new int2(x, y), out MapSurfaceSample sample))
                throw new InvalidOperationException($"Missing surface sample at ({x},{y}).");
            return sample.Height;
        }

        private static bool EntityPresentationAuthorsPlate(params string[] requiredNames)
        {
            if (!File.Exists(EntityPresentationPath))
                return false;

            var remaining = new System.Collections.Generic.HashSet<string>(requiredNames);
            using var reader = new StreamReader(EntityPresentationPath);
            string line;
            while ((line = reader.ReadLine()) != null && remaining.Count > 0)
            {
                if (!line.StartsWith("  m_Name:", StringComparison.Ordinal))
                    continue;
                string name = line.Substring("  m_Name:".Length).Trim();
                remaining.Remove(name);
            }

            return remaining.Count == 0;
        }

        private static string Sha256File(string path)
        {
            using var stream = File.OpenRead(path);
            return BitConverter.ToString(SHA256.Create().ComputeHash(stream)).Replace("-", "").ToLowerInvariant();
        }
    }
}
#endif
