using System;
using System.Collections.Generic;
using System.IO;
using Game.Components;
using Game.Configs;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;

namespace Game.Editor
{
    public static class M01FirstContactOldMarketWindowValidation
    {
        private const string DefinitionPath =
            "Assets/Game/Configs/OperationMaps/Chapter01/OperationMap_Ch01_DistrictEdge01.asset";
        private const string SourceDefinitionPath = "Assets/Game/Configs/OperationMaps/Candidates/" +
            "OperationMap_Compatibility_DesertBase01_DenseCity_EntityScene_Candidate.asset";
        private const string SurfacePath = "Assets/Game/Data/MapSurfaces/Match_Map_MapSurfaceData.asset";
        private const string PresentationPath = "Assets/Game/Scenes/OperationMaps/Skirmish/Candidates/" +
            "opmap_skirmish_desert_base_01_entity_presentation_dense_city_candidate.unity";
        private const string PresentationHash = "c1bc203591b3f32ae3d8410eaa0988e694b1d9d449ba1e938d9f38058698b598";
        private const string Marker = "[M01FirstContactOldMarketWindowValidation] result=Passed tests=9";

        private static readonly RectInt Window = new(1672, 680, 240, 176);
        private static readonly RectInt Corridor = new(1728, 720, 128, 80);
        private static int2[] Route = { new(1746, 736), new(1770, 748), new(1798, 760), new(1826, 772), new(1846, 786) };

        [MenuItem("Game/Campaign/Validate M01 Old Market Window")]
        public static void RunFocusedValidation()
        {
            try
            {
                OperationMapDefinition source = Load<OperationMapDefinition>(SourceDefinitionPath);
                MapSurfaceDataAsset surface = Load<MapSurfaceDataAsset>(SurfacePath);
                OperationMapDefinition logical = LoadOrCreateDefinition();
                string[] panelHashes = ValidateApprovedComicAuthority();
                ValidateSourceContract(source, surface);
                WindowAnalysis analysis = AnalyzeWindow(surface);
                ValidateWindowAnalysis(analysis);
                PopulateLogicalDefinition(logical, source);
                ValidateLogicalDefinition(logical);
                M01FirstContactOldMarketWindowEvidence.Write(panelHashes, analysis, Window, Corridor, Route, Marker);
                Debug.Log(Marker);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                Debug.Log("[M01FirstContactOldMarketWindowValidation] result=Failed");
                throw;
            }
        }

        private static string[] ValidateApprovedComicAuthority()
        {
            string[] paths = {
                "Assets/Game/Art/Narrative/FirstLaunch/Panels/16x9/FL-P15.png",
                "Assets/Game/Art/Narrative/FirstLaunch/Panels/16x9/FL-P16.png",
                "Assets/Game/Art/Narrative/FirstLaunch/Panels/16x9/FL-P17.png",
                "Assets/Game/Art/Narrative/FirstLaunch/Panels/16x9/FL-P18.png",
                "Assets/Game/Art/Narrative/FirstLaunch/Panels/20x9/FL-P15.png",
                "Assets/Game/Art/Narrative/FirstLaunch/Panels/20x9/FL-P16.png",
                "Assets/Game/Art/Narrative/FirstLaunch/Panels/20x9/FL-P17.png",
                "Assets/Game/Art/Narrative/FirstLaunch/Panels/20x9/FL-P18.png"
            };
            string[] hashes = new string[paths.Length];
            for (int index = 0; index < paths.Length; index++)
            {
                if (!File.Exists(paths[index]))
                    throw new InvalidOperationException($"Approved comic authority is missing: {paths[index]}");
                hashes[index] = M01FirstContactOldMarketWindowEvidence.Sha256File(paths[index]);
            }

            Require(hashes[3] == "d68d9a3341ab9493d68d491b1d51eb481bc2fc862c47b57c60affc9572216a54", "FL-P18 16:9 authority hash drifted.");
            Require(hashes[7] == "078abb9f5b759a3c606a030e6d44187194c156681db749bd4dbf8bed6cc4d548", "FL-P18 20:9 authority hash drifted.");
            return hashes;
        }

        private static void ValidateSourceContract(OperationMapDefinition source, MapSurfaceDataAsset surface)
        {
            Require(source != null && surface != null, "Accepted source definition and surface are required.");
            Require(source.OperationMapId == "opmap.skirmish.desert_base_01", "Physical source identity drifted.");
            Require(source.SourceIdentityHash == "2a2a791e8292a4f458bd603ceb86598aa0ba2ca82db41323bf5bf7c748ec6900", "Physical source identity hash drifted.");
            Require(source.ContentHash == "2713962f0faa2dae49805e1b7e3a1673199a2cca915334d11421b354cd8f591c", "Physical source content hash drifted.");
            Require(surface.Dimensions == new Vector2Int(2048, 1024) && Mathf.Approximately(surface.CellSize, 1f), "Accepted surface dimensions drifted.");
            Require(M01FirstContactOldMarketWindowEvidence.Sha256File(PresentationPath) == PresentationHash, "Accepted dense-city presentation scene drifted.");
        }

        private static WindowAnalysis AnalyzeWindow(MapSurfaceDataAsset surface)
        {
            Require(surface.TryCreateRuntimeBlobAsset(Allocator.Temp, out BlobAssetReference<MapSurfaceBlob> blob),
                "Accepted surface payload could not create a runtime blob.");
            try
            {
                WindowAnalysis analysis = new();
                for (int z = Window.yMin; z < Window.yMax; z++)
                for (int x = Window.xMin; x < Window.xMax; x++)
                {
                    MapSurfaceSample sample = Sample(ref blob.Value, x, z);
                    analysis.TotalCells++;
                    if ((sample.MovementMask & MapSurfaceMovementMask.Infantry) != 0) analysis.InfantryCells++;
                    if (sample.SurfaceType == MapSurfaceType.Blocked) analysis.BlockedCells++;
                    if (sample.SurfaceType == MapSurfaceType.Road || sample.SurfaceType == MapSurfaceType.DirtRoad ||
                        (sample.Flags & MapSurfaceFlags.Road) != 0) analysis.RoadCells++;
                    if (sample.SurfaceType == MapSurfaceType.Plaza) analysis.PlazaCells++;
                    if (sample.SurfaceType == MapSurfaceType.BridgeDeck || (sample.Flags & MapSurfaceFlags.Bridge) != 0)
                        analysis.BridgeCells++;
                    analysis.SurfaceKinds[x - Window.xMin + (z - Window.yMin) * Window.width] = Classify(sample);
                    analysis.MinHeight = math.min(analysis.MinHeight, sample.Height);
                    analysis.MaxHeight = math.max(analysis.MaxHeight, sample.Height);
                }

                int2 seed = FindCorridorSeed(ref blob.Value);
                analysis.ReachableCells = FloodReachable(ref blob.Value, seed, Window, out bool[] reachable);
                Route = SelectContactRoute(ref blob.Value, reachable);
                for (int index = 0; index < Route.Length; index++)
                {
                    MapSurfaceSample sample = Sample(ref blob.Value, Route[index].x, Route[index].y);
                    Require((sample.MovementMask & MapSurfaceMovementMask.Infantry) != 0, $"Contact route cell {Route[index]} is not infantry-reachable.");
                    Require(sample.SurfaceType != MapSurfaceType.Blocked, $"Contact route cell {Route[index]} is blocked.");
                    Require(sample.SurfaceType != MapSurfaceType.BridgeDeck &&
                            (sample.Flags & MapSurfaceFlags.Bridge) == 0,
                        $"Contact route cell {Route[index]} incorrectly crosses bridge/water space.");
                }

                return analysis;
            }
            finally
            {
                blob.Dispose();
            }
        }
        private static void ValidateWindowAnalysis(WindowAnalysis analysis)
        {
            Require(analysis.TotalCells == Window.width * Window.height, "Window surface scan is incomplete.");
            Require(analysis.InfantryCells >= 30000, "Old Market window lacks readable infantry capacity.");
            Require(analysis.ReachableCells >= 28000, "Old Market contact corridor is not broadly connected.");
            Require(analysis.RoadCells == 0 && analysis.PlazaCells == 0, "Accepted civic-bazaar navigation contract drifted.");
            Require(analysis.BridgeCells == 0, "Old Market window intersects bridge/water traversal.");
            Require(Window.xMin > 1600 && Window.yMin > 640, "Window left the accepted civic-bazaar core.");
            Require(Corridor.xMin >= Window.xMin && Corridor.xMax <= Window.xMax &&
                Corridor.yMin >= Window.yMin && Corridor.yMax <= Window.yMax, "Contact corridor must remain inside the playable window.");
        }
        private static void PopulateLogicalDefinition(OperationMapDefinition logical, OperationMapDefinition source)
        {
            SerializedObject target = new(logical);
            Set(target, "operationMapId", "opmap.ch01.district_edge_01");
            Set(target, "schemaVersion", 1);
            Set(target, "contentVersion", 1);
            Set(target, "sourceIdentityHash", source.SourceIdentityHash);
            Set(target, "contentHash", M01FirstContactOldMarketWindowEvidence.Sha256Text($"{Window.x},{Window.y},{Window.width},{Window.height}|{Corridor}"));
            Set(target, "generatedMetadataHash", M01FirstContactOldMarketWindowEvidence.Sha256Text("m01dc-011|approved-comic-fl-p15-p18|surface-v3"));
            SerializedProperty binding = target.FindProperty("sourceBinding");
            Set(binding, "sourceOperationMapId", source.OperationMapId);
            Set(binding, "sourceIdentityHash", source.SourceIdentityHash);
            Set(binding, "sourceContentHash", source.ContentHash);
            SerializedProperty bounds = target.FindProperty("bounds");
            Set(bounds, "worldMin", source.Bounds.WorldMin);
            Set(bounds, "worldMax", source.Bounds.WorldMax);
            Set(bounds, "playableMin", new Vector3(Window.xMin, source.Bounds.PlayableMin.y, Window.yMin));
            Set(bounds, "playableMax", new Vector3(Window.xMax, source.Bounds.PlayableMax.y, Window.yMax));
            Set(bounds, "cameraMin", new Vector3(Window.xMin, 4f, Window.yMin));
            Set(bounds, "cameraMax", new Vector3(Window.xMax, source.Bounds.CameraMax.y, Window.yMax));
            Copy(target, source, "gridMetadata", "surfaceMetadata", "navigationMetadata", "cameras",
                "planningCameraId", "battleCameraId", "minimap", "anchors", "presentationKind",
                "renderResidencyMode", "sourceSceneReference", "optionalHeavyMetadataReference",
                "staticPresentationManifestReference", "mapSurfaceDataReference", "minimapRasterReference",
                "buildingPlacementsReference", "vehiclePlacementsReference");
            target.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(logical);
            AssetDatabase.SaveAssets();
        }
        private static void ValidateLogicalDefinition(OperationMapDefinition logical)
        {
            Require(logical.OperationMapId == "opmap.ch01.district_edge_01", "Logical map identity is incorrect.");
            Require(logical.SourceBinding.SourceOperationMapId == "opmap.skirmish.desert_base_01", "Logical map does not bind the accepted physical source.");
            Require(logical.Bounds.PlayableMin.x == Window.xMin && logical.Bounds.PlayableMin.z == Window.yMin &&
                    logical.Bounds.PlayableMax.x == Window.xMax && logical.Bounds.PlayableMax.z == Window.yMax,
                "Logical playable bounds do not match the reviewed Old Market window.");
            Require(logical.Bounds.TryValidate(out string error), error);
            Require(logical.GridMetadata.TryValidate(out error), error);
            Require(logical.SurfaceMetadata.TryValidate(out error), error);
            Require(logical.NavigationMetadata.TryValidate(out error), error);
        }

        private static int2 FindCorridorSeed(ref MapSurfaceBlob blob)
        {
            for (int z = Corridor.yMin; z < Corridor.yMax; z++)
            for (int x = Corridor.xMin; x < Corridor.xMax; x++)
            {
                MapSurfaceSample sample = Sample(ref blob, x, z);
                if (IsSafeInfantry(sample)) return new int2(x, z);
            }
            throw new InvalidOperationException("Contact corridor contains no safe infantry seed.");
        }

        private static int2[] SelectContactRoute(ref MapSurfaceBlob blob, bool[] reachable)
        {
            int2[] route = new int2[5];
            for (int band = 0; band < route.Length; band++)
            {
                int xMin = Corridor.xMin + Corridor.width * band / route.Length;
                int xMax = Corridor.xMin + Corridor.width * (band + 1) / route.Length;
                bool found = TrySelectBandCell(ref blob, reachable, xMin, xMax, true, out route[band]) ||
                             TrySelectBandCell(ref blob, reachable, xMin, xMax, false, out route[band]);
                Require(found, $"Contact corridor band {band} has no connected infantry route cell.");
            }
            return route;
        }

        private static bool TrySelectBandCell(
            ref MapSurfaceBlob blob,
            bool[] reachable,
            int xMin,
            int xMax,
            bool preferRoad,
            out int2 selected)
        {
            int middleZ = Corridor.yMin + Corridor.height / 2;
            for (int offset = 0; offset <= Corridor.height / 2; offset++)
            {
                int[] rows = offset == 0 ? new[] { middleZ } : new[] { middleZ - offset, middleZ + offset };
                for (int rowIndex = 0; rowIndex < rows.Length; rowIndex++)
                {
                    int z = rows[rowIndex];
                    if (z < Corridor.yMin || z >= Corridor.yMax) continue;
                    for (int x = xMin; x < xMax; x++)
                    {
                        int index = x - Window.xMin + (z - Window.yMin) * Window.width;
                        if (!reachable[index]) continue;
                        MapSurfaceSample sample = Sample(ref blob, x, z);
                        bool road = sample.SurfaceType == MapSurfaceType.Road ||
                                    sample.SurfaceType == MapSurfaceType.DirtRoad ||
                                    sample.SurfaceType == MapSurfaceType.Plaza ||
                                    (sample.Flags & MapSurfaceFlags.Road) != 0;
                        if (preferRoad && !road) continue;
                        selected = new int2(x, z);
                        return true;
                    }
                }
            }
            selected = default;
            return false;
        }

        private static int FloodReachable(
            ref MapSurfaceBlob blob,
            int2 start,
            RectInt bounds,
            out bool[] visited)
        {
            visited = new bool[bounds.width * bounds.height];
            Queue<int2> queue = new();
            queue.Enqueue(start);
            int count = 0;
            int2[] directions = { new(1, 0), new(-1, 0), new(0, 1), new(0, -1) };
            while (queue.Count > 0)
            {
                int2 cell = queue.Dequeue();
                if (!bounds.Contains(new Vector2Int(cell.x, cell.y))) continue;
                int index = cell.x - bounds.xMin + (cell.y - bounds.yMin) * bounds.width;
                if (visited[index]) continue;
                MapSurfaceSample sample = Sample(ref blob, cell.x, cell.y);
                if (!IsSafeInfantry(sample)) continue;
                visited[index] = true;
                count++;
                for (int direction = 0; direction < directions.Length; direction++)
                    queue.Enqueue(cell + directions[direction]);
            }
            return count;
        }

        private static bool IsSafeInfantry(MapSurfaceSample sample) =>
            (sample.MovementMask & MapSurfaceMovementMask.Infantry) != 0 &&
            sample.SurfaceType != MapSurfaceType.Blocked &&
            sample.SurfaceType != MapSurfaceType.BridgeDeck &&
            (sample.Flags & MapSurfaceFlags.Bridge) == 0;
        private static byte Classify(MapSurfaceSample sample) => sample.SurfaceType == MapSurfaceType.BridgeDeck ||
            (sample.Flags & MapSurfaceFlags.Bridge) != 0 ? (byte)3 : sample.SurfaceType == MapSurfaceType.Road ||
            sample.SurfaceType == MapSurfaceType.DirtRoad || (sample.Flags & MapSurfaceFlags.Road) != 0 ? (byte)2 :
            IsSafeInfantry(sample) ? (byte)1 : (byte)0;

        private static MapSurfaceSample Sample(ref MapSurfaceBlob blob, int x, int z)
        {
            if (!MapSurfaceBlobAccess.TryGetPrimarySurface(ref blob, new int2(x, z), out MapSurfaceSample sample))
                throw new InvalidOperationException($"Surface cell ({x},{z}) did not resolve.");
            return sample;
        }

        private static OperationMapDefinition LoadOrCreateDefinition()
        {
            string folder = Path.GetDirectoryName(DefinitionPath)?.Replace("\\", "/");
            EnsureFolder(folder);
            OperationMapDefinition definition = AssetDatabase.LoadAssetAtPath<OperationMapDefinition>(DefinitionPath);
            if (definition != null) return definition;
            definition = ScriptableObject.CreateInstance<OperationMapDefinition>();
            AssetDatabase.CreateAsset(definition, DefinitionPath);
            return definition;
        }

        private static void Copy(SerializedObject target, UnityEngine.Object source, params string[] names)
        {
            SerializedObject serializedSource = new(source);
            for (int index = 0; index < names.Length; index++)
                target.CopyFromSerializedProperty(serializedSource.FindProperty(names[index]));
        }

        private static void Set(SerializedObject target, string name, string value) => target.FindProperty(name).stringValue = value;
        private static void Set(SerializedObject target, string name, int value) => target.FindProperty(name).intValue = value;
        private static void Set(SerializedProperty target, string name, string value) => target.FindPropertyRelative(name).stringValue = value;
        private static void Set(SerializedProperty target, string name, Vector3 value) => target.FindPropertyRelative(name).vector3Value = value;

        private static T Load<T>(string path) where T : UnityEngine.Object => AssetDatabase.LoadAssetAtPath<T>(path);
        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            string parent = Path.GetDirectoryName(path)?.Replace("\\", "/");
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
        }
        private static void Require(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
        }

        internal sealed class WindowAnalysis
        {
            public int TotalCells, InfantryCells, ReachableCells, RoadCells, PlazaCells, BlockedCells, BridgeCells;
            public float MinHeight = float.PositiveInfinity, MaxHeight = float.NegativeInfinity;
            public readonly byte[] SurfaceKinds = new byte[Window.width * Window.height];
        }
    }
}
