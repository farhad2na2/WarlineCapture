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
        public const string Marker = "[CityCrossroadsFloatingShelfValidation] result=Passed cases=10";
        private const string SurfacePath = "Assets/Game/Data/MapSurfaces/Match_Map_MapSurfaceData.asset";
        private const string MapPath = "Assets/Game/Configs/Skirmish/CityCrossroads/OperationMap_CityCrossroads.asset";
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

                if (cases != 10)
                    throw new InvalidOperationException($"Expected 10 cases, executed {cases}.");
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

        private static string Sha256File(string path)
        {
            using var stream = File.OpenRead(path);
            return BitConverter.ToString(SHA256.Create().ComputeHash(stream)).Replace("-", "").ToLowerInvariant();
        }
    }
}
#endif
