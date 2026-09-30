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
    /// <summary>Mission-owned clinic and guarded custody post; Demo2 remains unchanged.</summary>
    public static class CH03M04EvidenceChainWorldBuilder
    {
        private const string Root = "Assets/Game/Prefabs/Buildings/CH03M04EvidenceChain";
        public const string PlacementsPath = "Assets/Game/Configs/OperationMaps/Chapter03/EvidenceChainPlacements.asset";

        public static void Build()
        {
            Directory.CreateDirectory(Root);
            AssetDatabase.Refresh();
            GameObject clinic = CreateClinic();
            GameObject barracks = CreateBarracks();
            var map = AssetDatabase.LoadAssetAtPath<OperationMapDefinition>(M04AirliftConfigBuilder.LegacyMapPath);
            var surface = AssetDatabase.LoadAssetAtPath<MapSurfaceDataAsset>(
                AssetDatabase.GUIDToAssetPath(map.MapSurfaceDataReference.AssetGUID));
            if (surface == null || !surface.TryCreateRuntimeBlobAsset(Allocator.Temp,
                    out BlobAssetReference<MapSurfaceBlob> blob))
                throw new InvalidOperationException("Evidence Chain map surface is unavailable.");
            var entries = new List<MapBuildingPlacementConfigEntry>();
            using (blob)
            {
                Add(entries, ref blob.Value, clinic, 806, 447);
                Add(entries, ref blob.Value, barracks, 930, 447);
            }
            var placements = AssetDatabase.LoadAssetAtPath<MapBuildingPlacementConfig>(PlacementsPath);
            if (placements == null)
            {
                placements = ScriptableObject.CreateInstance<MapBuildingPlacementConfig>();
                AssetDatabase.CreateAsset(placements, PlacementsPath);
            }
            placements.EditorSetPlacements(entries);
            placements.EditorSetUseExistingStaticPresentationWhenAuthoringVisualMissing(false);
            EditorUtility.SetDirty(placements);
            AssetDatabase.SaveAssets();
            Debug.Log("[EvidenceChainWorld] result=Passed landmarks=clinic,barracks demo2=adapted scene=untouched");
        }

        private static void Add(List<MapBuildingPlacementConfigEntry> entries, ref MapSurfaceBlob surface,
            GameObject prefab, int x, int z)
        {
            if (!MapSurfaceBlobAccess.TryGetPrimarySurface(ref surface, new int2(x, z), out var ground))
                throw new InvalidOperationException("Evidence Chain landmark has no ground: " + prefab.name);
            var position = new Vector3(x, ground.Height, z);
            entries.Add(new MapBuildingPlacementConfigEntry("EvidenceChain/" + prefab.name,
                "CustodyInfrastructure", prefab, 0, position, position, Vector3.zero, Vector3.one,
                0, false, true, true));
        }

        private static GameObject CreateClinic()
        {
            var root = new GameObject("EvidenceChain_Clinic");
            try
            {
                var geometry = new GameObject("Geometry");
                geometry.transform.SetParent(root.transform, false);
                AddVisual("Assets/Synty/PolygonBattleRoyale/Prefabs/Buildings/SM_Bld_SmallBuilding_02.prefab",
                    geometry.transform, Vector3.zero, Vector3.zero, Vector3.one);
                AddVisual("Assets/PolygonMilitary/Prefabs/Props/Signs/SM_Prop_Sign_Medical_01.prefab",
                    geometry.transform, new Vector3(0, 3, -4.8f), Vector3.zero, Vector3.one);
                AddVisual("Assets/Synty/PolygonBattleRoyale/Prefabs/Props/SM_Prop_Crate_Medical_01.prefab",
                    geometry.transform, new Vector3(4.5f, 0, -5f), Vector3.zero, Vector3.one);
                ConfigureStaticBuilding(root, "East District Clinic", "Lina's protected witness and archive handoff.",
                    new Vector2Int(14, 12));
                return PrefabUtility.SaveAsPrefabAsset(root, Root + "/EvidenceChain_Clinic.prefab");
            }
            finally { UnityEngine.Object.DestroyImmediate(root); }
        }

        private static GameObject CreateBarracks()
        {
            var root = new GameObject("EvidenceChain_Custody_Barracks");
            try
            {
                var geometry = new GameObject("Geometry");
                geometry.transform.SetParent(root.transform, false);
                AddVisual("Assets/PolygonMilitary/Prefabs/Buildings/SM_Bld_Barracks_01.prefab",
                    geometry.transform, Vector3.zero, Vector3.zero, Vector3.one);
                ConfigureStaticBuilding(root, "Custody Barracks", "Guarded transfer checkpoint for the archive convoy.",
                    new Vector2Int(18, 16));
                return PrefabUtility.SaveAsPrefabAsset(root, Root + "/EvidenceChain_Custody_Barracks.prefab");
            }
            finally { UnityEngine.Object.DestroyImmediate(root); }
        }

        private static void AddVisual(string path, Transform parent, Vector3 position, Vector3 rotation, Vector3 scale)
        {
            var source = AssetDatabase.LoadAssetAtPath<GameObject>(path) ??
                throw new InvalidOperationException("Missing Evidence Chain scenery: " + path);
            var visual = (GameObject)PrefabUtility.InstantiatePrefab(source, parent);
            visual.transform.localPosition = position;
            visual.transform.localEulerAngles = rotation;
            visual.transform.localScale = scale;
        }

        private static void ConfigureStaticBuilding(GameObject root, string name, string description,
            Vector2Int footprint)
        {
            var authoring = root.AddComponent<BuildingDefinitionAuthoring>();
            var data = new SerializedObject(authoring);
            data.FindProperty("displayName").stringValue = name;
            data.FindProperty("description").stringValue = description;
            data.FindProperty("footprintCells").vector2IntValue = footprint;
            data.FindProperty("canRequest").boolValue = false;
            data.FindProperty("maxHealth").intValue = 1000;
            data.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
