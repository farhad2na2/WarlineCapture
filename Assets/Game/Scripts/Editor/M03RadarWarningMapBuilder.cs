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
    public static partial class M03RadarWarningMapBuilder
    {
        public const string MapId = "opmap.ch01.convoy_approach_01";
        public const string Path = "Assets/Game/Configs/OperationMaps/Chapter01/OperationMap_Ch01_ConvoyApproach01.asset";
        public const string Prefix = "anchor.ch01.m03.";
        // Authored control tower at the accepted M02 post anchor, with the real Airport building definition (1800 HP).
        public const string ForwardPostStableId = "GlobalObjectId_V1-2-ca1f2d7f265d8495f8c815441d68fda0-1990580264897520-8699699288898649154";
        public static readonly RectInt Window = new(540, 270, 560, 220);
        private readonly struct Seed
        {
            public readonly string Name;
            public readonly OperationMapAnchorKind Kind;
            public readonly int2 Cell;
            public readonly int Clearance, Faction;
            public readonly float Radius;
            public Seed(string name, OperationMapAnchorKind kind, int x, int z, int clearance = 2,
                float radius = 3f, int faction = 0)
            { Name = name; Kind = kind; Cell = new int2(x,z); Clearance = clearance; Radius = radius; Faction = faction; }
        }

        [MenuItem("Game/Campaign/M03/Build Convoy Approach Map")]
        public static void Build()
        {
            M02EstablishBaseForwardPostWindowValidation.ValidateCurrentDefinition();
            OperationMapDefinition source = Load<OperationMapDefinition>(M02EstablishBaseForwardPostWindowValidation.SourceDefinitionPath);
            OperationMapDefinition basis = Load<OperationMapDefinition>(M02EstablishBaseForwardPostWindowValidation.DefinitionPath);
            MapSurfaceDataAsset data = Load<MapSurfaceDataAsset>(AssetDatabase.GUIDToAssetPath(source.MapSurfaceDataReference.AssetGUID));
            Require(data.TryCreateRuntimeBlobAsset(Allocator.Temp, out BlobAssetReference<MapSurfaceBlob> blob), "Surface unavailable.");
            using (blob)
            {
                var seeds = new List<Seed>
                {
                    new("forward_post", OperationMapAnchorKind.Base, 937,348,-1,12,1),
                    new("initial_barracks", OperationMapAnchorKind.Build,1006,330,-1,1,1),
                    new("build_zone", OperationMapAnchorKind.Build,940,390,-1,60,1),
                    // Surveyed with the live GridWalkable/DynamicBlocker intersection, including
                    // each formation's spawn offsets. Surface clearance alone misses buildings.
                    new("squad_a", OperationMapAnchorKind.Deployment,910,426,5,4,1),
                    new("squad_b", OperationMapAnchorKind.Deployment,930,426,5,4,1),
                    new("ground_sensor", OperationMapAnchorKind.Deployment,949,428,4,2,1),
                    new("civilians", OperationMapAnchorKind.Civilian,1060,428,5,4,0),
                    new("evacuation", OperationMapAnchorKind.Civilian,1085,465,3,3,0),
                    new("vanguard_spawn", OperationMapAnchorKind.Spawn,590,426,6,5,2),
                    new("main_spawn", OperationMapAnchorKind.Spawn,565,426,6,5,2),
                    new("contact", OperationMapAnchorKind.Hostile,830,426,4,4,2),
                    new("fork", OperationMapAnchorKind.Lane,876,426,3,3),
                    new("inner_core", OperationMapAnchorKind.Base,942,342,2,6,1),
                    new("return_rts", OperationMapAnchorKind.Camera,930,402,-1,2),
                    new("defense_tower", OperationMapAnchorKind.Build,875,412,3,4,1),
                    new("defense_barrier", OperationMapAnchorKind.Build,883,427,3,3,1)
                };
                var cells = new List<int2>();
                foreach (Seed seed in seeds) cells.Add(seed.Clearance < 0 ? seed.Cell : Nearest(ref blob.Value, seed.Cell, seed.Clearance));
                List<int2> route = FindRoute(ref blob.Value, AuthoredConvoyStops());
                for (int i = 0; i < route.Count; i++)
                {
                    seeds.Add(new Seed("convoy_path_" + i.ToString("000"), OperationMapAnchorKind.Lane, route[i].x,route[i].y));
                    cells.Add(route[i]);
                }
                OperationMapDefinition map = AssetDatabase.LoadAssetAtPath<OperationMapDefinition>(Path);
                if (map == null) { map = ScriptableObject.CreateInstance<OperationMapDefinition>(); AssetDatabase.CreateAsset(map, Path); }
                EditorUtility.CopySerialized(basis, map);
                SerializedObject serialized = new(map);
                Set(serialized.FindProperty("operationMapId"), MapId);
                Set(serialized.FindProperty("planningCameraId"), "camera.ch01.m03.planning");
                Set(serialized.FindProperty("battleCameraId"), "camera.ch01.m03.battle");
                SerializedProperty bounds = serialized.FindProperty("bounds");
                Set(bounds.FindPropertyRelative("playableMin"), new Vector3(Window.xMin, source.Bounds.PlayableMin.y, Window.yMin));
                Set(bounds.FindPropertyRelative("playableMax"), new Vector3(Window.xMax, source.Bounds.PlayableMax.y, Window.yMax));
                MissionCameraBoundsAuthoring.Apply(bounds);
                SerializedProperty minimap = serialized.FindProperty("minimap");
                Set(minimap.FindPropertyRelative("minimapId"), "minimap.ch01.m03.convoy");
                Set(minimap.FindPropertyRelative("projectionOrigin"), new Vector3(Window.xMin,0,Window.yMin));
                minimap.FindPropertyRelative("projectionSize").vector2Value = new Vector2(Window.width,Window.height);
                SerializedProperty cameras = serialized.FindProperty("cameras");
                for (int i = 0; i < 2; i++)
                {
                    SerializedProperty camera = cameras.GetArrayElementAtIndex(i);
                    Vector3 position = i == 0 ? new Vector3(937,95,478) : new Vector3(937,78,465);
                    Set(camera.FindPropertyRelative("cameraId"), i == 0 ? "camera.ch01.m03.planning" : "camera.ch01.m03.battle");
                    Set(camera.FindPropertyRelative("position"), position);
                    Set(camera.FindPropertyRelative("eulerAngles"), Quaternion.LookRotation(new Vector3(925,0,399)-position).eulerAngles);
                    camera.FindPropertyRelative("fieldOfView").floatValue = 58f;
                }
                SerializedProperty anchors = serialized.FindProperty("anchors"); anchors.arraySize = seeds.Count;
                var report = new StringBuilder("# M03 map geometry\n\nPhysical source preserved; route clearance is a five-cell square. Runtime collision and playthrough checks follow.\n\n| Anchor | Cell | Height |\n|---|---|---|\n");
                for (int i = 0; i < seeds.Count; i++)
                {
                    Seed seed = seeds[i]; int2 cell = cells[i];
                    Require(MapSurfaceBlobAccess.TryGetPrimarySurface(ref blob.Value,cell,out MapSurfaceSample sample), "Missing anchor surface.");
                    SerializedProperty anchor = anchors.GetArrayElementAtIndex(i);
                    Set(anchor.FindPropertyRelative("anchorId"), Prefix + seed.Name);
                    anchor.FindPropertyRelative("kind").intValue = (int)seed.Kind;
                    Set(anchor.FindPropertyRelative("position"), new Vector3(cell.x,sample.Height,cell.y));
                    Set(anchor.FindPropertyRelative("eulerAngles"), Vector3.zero);
                    anchor.FindPropertyRelative("radius").floatValue = seed.Radius;
                    anchor.FindPropertyRelative("factionId").intValue = seed.Faction;
                    anchor.FindPropertyRelative("laneIndex").intValue = 0;
                    report.AppendLine($"| {seed.Name} | {cell.x}, {cell.y} | {sample.Height:F3} |");
                }
                string hash = Hash(source.ContentHash + report + ":camera-padding-540-220-1280-640-v1");
                Set(serialized.FindProperty("contentHash"), hash);
                Set(serialized.FindProperty("generatedMetadataHash"), hash);
                serialized.ApplyModifiedPropertiesWithoutUndo();
                Require(map.TryValidateMetadata(out string error) && map.TryValidateLocalContentReferences(out error), error);
                EditorUtility.SetDirty(map); AssetDatabase.SaveAssets();
                Directory.CreateDirectory("Design/AgentReports/M03RadarWarning");
                File.WriteAllText("Design/AgentReports/M03RadarWarning/map_geometry.md", report.ToString());
                Debug.Log($"[M03RadarWarningMapBuilder] result=Passed anchors={seeds.Count} routePoints={route.Count} hash={hash}");
            }
        }

        private static T Load<T>(string path) where T : UnityEngine.Object => AssetDatabase.LoadAssetAtPath<T>(path) ?? throw new InvalidOperationException(path);
        private static void Require(bool value, string error) { if (!value) throw new InvalidOperationException(error); }
        private static void Set(SerializedProperty property, string value) => property.stringValue = value;
        private static void Set(SerializedProperty property, Vector3 value) => property.vector3Value = value;
        private static string Hash(string value)
        { using SHA256 sha = SHA256.Create(); return BitConverter.ToString(sha.ComputeHash(Encoding.UTF8.GetBytes(value))).Replace("-", "").ToLowerInvariant(); }
    }
}
