using System;
using System.Collections.Generic;
using System.IO;
using Game.Authoring;
using Game.Components;
using Game.Configs;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;

namespace Game.Editor
{
    /// <summary>M5-only fortifications over the shared terrain. Every perimeter piece owns a real navigation footprint.</summary>
    public static class M05EnemyCompoundBuilder
    {
        public const string Root = "Assets/Game/Prefabs/Buildings/M05Compound";
        public const string PlacementsPath = "Assets/Game/Configs/OperationMaps/Chapter01/M05EnemyCompoundPlacements.asset";
        public const string GateId = "M05_Breach_Road_Barrier";
        private const string Props = "Assets/PolygonMilitary/Prefabs/Props/";
        private const string Buildings = "Assets/PolygonMilitary/Prefabs/Buildings/";

        public static void BuildAssets()
        {
            Directory.CreateDirectory(Root);
            AssetDatabase.Refresh();
            var walls = new Dictionary<int, GameObject>();
            foreach (int length in new[] { 6, 10 })
                walls[length] = Create("M05_Perimeter_" + length, Props + "SM_Prop_Barrier_Tall_01.prefab", new Vector3(length, 4, 2), true);
            var gate = Create(GateId, Props + "SM_Prop_Fence_Gate_01.prefab", new Vector3(8, 4, 2), false);
            var tower = Create("M05_GuardPost", Buildings + "SM_Bld_GuardTower_01.prefab", new Vector3(4, 8, 4), false);
            var archive = Create("M05_Archive", Buildings + "SM_Bld_Barracks_01.prefab", new Vector3(14, 5, 8), false);
            RegisterGate(gate);
            var map = AssetDatabase.LoadAssetAtPath<OperationMapDefinition>(M03RadarWarningMapBuilder.Path);
            var surface = AssetDatabase.LoadAssetAtPath<MapSurfaceDataAsset>(AssetDatabase.GUIDToAssetPath(map.MapSurfaceDataReference.AssetGUID));
            if (!surface.TryCreateRuntimeBlobAsset(Allocator.Temp, out BlobAssetReference<MapSurfaceBlob> blob)) throw new InvalidOperationException("M5 terrain unavailable");
            var entries = new List<MapBuildingPlacementConfigEntry>();
            using (blob)
            {
                void Add(GameObject prefab, float x, float z, bool vertical = false)
                {
                    if (!MapSurfaceBlobAccess.TryGetPrimarySurface(ref blob.Value, new int2((int)x, (int)z), out var ground)) throw new InvalidOperationException("M5 compound outside terrain");
                    var position = new Vector3(x, ground.Height, z);
                    entries.Add(new MapBuildingPlacementConfigEntry("M05Compound/" + entries.Count, "MissionFortification", prefab, 0,
                        position, position, new Vector3(0, vertical ? 90 : 0, 0), Vector3.one, vertical ? 90 : 0, vertical, true, true));
                }
                // South wall has one eight-metre breach. No wall, tower or archive overlaps its access corridor.
                foreach (float x in new[] { 999f, 1009f, 1039f, 1049f }) Add(walls[10], x, 445);
                Add(walls[6], 1017, 445); Add(walls[6], 1031, 445);
                for (int x = 999; x <= 1049; x += 10) Add(walls[10], x, 475);
                foreach (int x in new[] { 995, 1053 })
                    foreach (int z in new[] { 451, 461, 471 }) Add(walls[10], x, z, true);
                Add(tower, 1016, 449); Add(tower, 1032, 449);
                Add(archive, 1007, 470);
            }
            var config = AssetDatabase.LoadAssetAtPath<MapBuildingPlacementConfig>(PlacementsPath);
            if (config == null) { config = ScriptableObject.CreateInstance<MapBuildingPlacementConfig>(); AssetDatabase.CreateAsset(config, PlacementsPath); }
            config.EditorSetPlacements(entries);
            config.EditorSetUseExistingStaticPresentationWhenAuthoringVisualMissing(false);
            AssetDatabase.SaveAssets();
            Debug.Log("[M05EnemyCompound] authored perimeter=" + entries.Count + " gate=" + GateId);
        }

        private static GameObject Create(string name, string sourcePath, Vector3 size, bool wall)
        {
            var source = AssetDatabase.LoadAssetAtPath<GameObject>(sourcePath);
            if (source == null) throw new InvalidOperationException(sourcePath);
            var root = new GameObject(name);
            try
            {
                var geometry = new GameObject("Geometry");
                geometry.transform.SetParent(root.transform, false);
                var visual = (GameObject)PrefabUtility.InstantiatePrefab(source, geometry.transform);
                visual.transform.localPosition = Vector3.zero;
                visual.transform.localRotation = Quaternion.Euler(0, wall ? 90 : 0, 0);
                Bounds bounds = new Bounds(); bool first = true;
                foreach (var renderer in visual.GetComponentsInChildren<Renderer>()) { if (first) { bounds = renderer.bounds; first = false; } else bounds.Encapsulate(renderer.bounds); }
                if (first) throw new InvalidOperationException("Missing compound model " + name);
                var scale = new Vector3(size.x / bounds.size.x, size.y / bounds.size.y, size.z / bounds.size.z);
                // Gate/fence depth stays thin; the two-metre reserved footprint includes posts and clearance.
                if (name == GateId) scale.z = 1;
                geometry.transform.localScale = scale;
                geometry.transform.localPosition = -Vector3.Scale(new Vector3(bounds.center.x, bounds.min.y, bounds.center.z), scale);
                foreach (var lod in visual.GetComponentsInChildren<LODGroup>()) { lod.ForceLOD(0); lod.enabled = false; }
                var authoring = root.AddComponent<BuildingDefinitionAuthoring>();
                var data = new SerializedObject(authoring);
                data.FindProperty("displayName").stringValue = name == GateId ? "Relay Gate" : wall ? "Concrete Perimeter" : name == "M05_Archive" ? "Archive" : "Guard Post";
                data.FindProperty("description").stringValue = string.Empty;
                data.FindProperty("footprintCells").vector2IntValue = new Vector2Int(Mathf.CeilToInt(size.x), Mathf.CeilToInt(size.z));
                data.FindProperty("maxHealth").intValue = name == GateId ? 1000 : 100000;
                data.FindProperty("canRequest").boolValue = false;
                data.FindProperty("isWall").boolValue = wall;
                data.ApplyModifiedPropertiesWithoutUndo();
                return PrefabUtility.SaveAsPrefabAsset(root, Root + "/" + name + ".prefab");
            }
            finally { UnityEngine.Object.DestroyImmediate(root); }
        }

        private static void RegisterGate(GameObject gate)
        {
            var config = AssetDatabase.LoadAssetAtPath<BuildingPlacementSystemConfig>("Assets/Game/Configs/Scene/Game_BuildingPlacement_Config.asset");
            var data = new SerializedObject(config); var list = data.FindProperty("spawnables");
            for (int i = 0; i < list.arraySize; i++) if (list.GetArrayElementAtIndex(i).objectReferenceValue == gate) return;
            list.InsertArrayElementAtIndex(list.arraySize); list.GetArrayElementAtIndex(list.arraySize - 1).objectReferenceValue = gate;
            data.ApplyModifiedPropertiesWithoutUndo(); EditorUtility.SetDirty(config);
        }
    }
}
