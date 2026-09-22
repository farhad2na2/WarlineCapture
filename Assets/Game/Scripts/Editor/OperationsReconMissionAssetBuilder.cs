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
    public static class OperationsReconMissionAssetBuilder
    {
        private const string Root = "Assets/Game/Resources/Operations";
        private static readonly RectInt Window = new(1672, 680, 240, 176);

        [MenuItem("Game/Operations/Build Street Signals Shared World Content")]
        public static void Build()
        {
            Directory.CreateDirectory(Root);
            AssetDatabase.Refresh();
            var source = Load<OperationMapDefinition>("Assets/Game/Configs/OperationMaps/Chapter01/OperationMap_Ch01_DistrictEdge01.asset");
            var surface = Load<MapSurfaceDataAsset>("Assets/Game/Data/MapSurfaces/Match_Map_MapSurfaceData.asset");
            if (!surface.TryCreateRuntimeBlobAsset(Allocator.Temp, out var blob)) throw new InvalidOperationException("Missing ground surface.");
            try
            {
                Vector3 start = FindOpen(ref blob.Value, new int2(1690, 700));
                // C sits in the southern forecourt. The old roof-side point (1870,815)
                // passed the surface bake but lies inside the runtime building blockers.
                Vector3[] scans = { FindOpen(ref blob.Value, new int2(1705, 805)), FindOpen(ref blob.Value, new int2(1790, 790)), FindOpen(ref blob.Value, new int2(1882, 796)) };
                Vector3 evidence = FindOpen(ref blob.Value, new int2(1860, 710));
                Vector3 guard = FindOpen(ref blob.Value, new int2(1825, 755));
                Vector3 patrol = FindOpen(ref blob.Value, new int2(1745, 760));
                Vector3[] patrolRoute = { patrol, FindOpen(ref blob.Value, new int2(1745, 800)), FindOpen(ref blob.Value, new int2(1785, 760)) };
                Vector3 waveA = FindOpen(ref blob.Value, new int2(1900, 825));
                Vector3 waveB = FindOpen(ref blob.Value, new int2(1900, 695));
                RequireReachable(ref blob.Value, start, new[] { scans[0], scans[1], scans[2], evidence, guard, patrol, waveA, waveB });
                RequireReachable(ref blob.Value, start, patrolRoute);

                var map = Clone(source, Root + "/OldQuarter.asset");
                var mapEdit = new SerializedObject(map);
                mapEdit.FindProperty("operationMapId").stringValue = "opmap.operations.old_quarter";
                // The camera's entire perspective footprint is clamped. Give edge
                // objectives enough framing space while preserving the playable window.
                var bounds = mapEdit.FindProperty("bounds");
                bounds.FindPropertyRelative("cameraMin").vector3Value = new Vector3(Window.xMin - 80, 4, Window.yMin - 80);
                bounds.FindPropertyRelative("cameraMax").vector3Value = new Vector3(Window.xMax + 80, 100, Window.yMax + 80);
                mapEdit.FindProperty("additionalBuildingPlacements").arraySize = 0;
                var anchors = mapEdit.FindProperty("anchors"); anchors.arraySize = 9;
                Anchor(anchors.GetArrayElementAtIndex(0), "spawn.player", start, OperationMapAnchorKind.Deployment);
                for (int i = 0; i < 3; i++) Anchor(anchors.GetArrayElementAtIndex(i + 1), "signal_" + (char)('a' + i), scans[i], OperationMapAnchorKind.Objective);
                Anchor(anchors.GetArrayElementAtIndex(4), "relay_evidence", evidence, OperationMapAnchorKind.Objective);
                Anchor(anchors.GetArrayElementAtIndex(5), "exit.ground", start, OperationMapAnchorKind.Objective);
                Anchor(anchors.GetArrayElementAtIndex(6), "spawn.enemy_a", waveA, OperationMapAnchorKind.Hostile);
                Anchor(anchors.GetArrayElementAtIndex(7), "spawn.enemy_b", waveB, OperationMapAnchorKind.Hostile);
                Anchor(anchors.GetArrayElementAtIndex(8), "route.flank", patrol, OperationMapAnchorKind.Lane);
                mapEdit.FindProperty("contentHash").stringValue = Hash(source.ContentHash + "|o001-v1|" + start + string.Join("|", scans) + evidence + guard + patrol + waveA + waveB);
                mapEdit.FindProperty("generatedMetadataHash").stringValue = Hash("o001-v1|finite16-20|scan15|evidence15|deadline720");
                mapEdit.ApplyModifiedPropertiesWithoutUndo();

                var startup = Clone(Load<BuildingPlacementSystemConfig>("Assets/Game/Configs/Scene/Game_BuildingPlacement_Config.asset"), Root + "/StreetSignalsStartup.asset");
                var initial = Clone(startup.InitialUnitsConfig, Root + "/StreetSignalsInitial.asset");
                var initialEdit = new SerializedObject(initial);
                initialEdit.FindProperty("factions").arraySize = 0;
                initialEdit.FindProperty("blockerCount").intValue = 0;
                initialEdit.FindProperty("createFactionBases").boolValue = false;
                initialEdit.FindProperty("enableBlockerChurn").boolValue = false;
                initialEdit.FindProperty("initialDollars").intValue = 0;
                initialEdit.FindProperty("initialMaterials").intValue = 80;
                foreach (string field in new[] { "initialAiMaterials", "initialOil", "initialFuel" }) initialEdit.FindProperty(field).intValue = 0;
                initialEdit.ApplyModifiedPropertiesWithoutUndo();
                var startupEdit = new SerializedObject(startup);
                startupEdit.FindProperty("initialUnitsConfig").objectReferenceValue = initial;
                startupEdit.FindProperty("spawnables").arraySize = 0;
                startupEdit.FindProperty("maxQueuedUnitProductions").intValue = 0;
                startupEdit.ApplyModifiedPropertiesWithoutUndo();

                var mission = AssetDatabase.LoadAssetAtPath<OperationsReconMissionConfig>(Root + "/StreetSignals.asset");
                if (mission == null) { mission = ScriptableObject.CreateInstance<OperationsReconMissionConfig>(); AssetDatabase.CreateAsset(mission, Root + "/StreetSignals.asset"); }
                mission.operationMap = map; mission.startupConfig = startup; mission.scanPositions = scans;
                mission.evidencePosition = evidence; mission.exitPosition = start;
                mission.patrolRoute = patrolRoute;
                mission.reinforcementWarningSeconds = 30f; mission.evidenceReinforcementWarningSeconds = 45f;
                mission.forces = new[]
                {
                    Force("Unit_Chr_Soldier_Male_02_Alt_02", start, 12, 1),
                    Force("Unit_Chr_Soldier_Male_02_Alt_04", start + new Vector3(0,0,4), 2, 1, 0, true),
                    Force("Unit_Chr_Soldier_Female_01_Alt_01", start + new Vector3(0,0,-4), 2, 1),
                    Force("Unit_Chr_Insurgent_Male_03", guard, 6, 2),
                    Force("Unit_Chr_Insurgent_Female_01", guard + new Vector3(0,0,4), 2, 2),
                    Force("Unit_Chr_Insurgent_Male_03", patrol, 4, 2, patrol: true),
                    Force("Unit_Chr_Insurgent_Male_03", waveA, 4, 2, 1),
                    Force("Unit_Chr_Insurgent_Male_03", waveB, 4, 2, 2)
                };
                var signature = new StringBuilder(source.ContentHash);
                signature.Append('|').Append(AssetDatabase.GetAssetDependencyHash(AssetDatabase.GetAssetPath(startup.UnitPrefabRegistryConfig)));
                foreach (var point in scans) AppendPosition(signature, point);
                foreach (var point in patrolRoute) AppendPosition(signature, point);
                AppendPosition(signature, evidence); AppendPosition(signature, start);
                foreach (var force in mission.forces)
                {
                    signature.Append('|').Append(force.sourceKey).Append('|').Append(force.count).Append('|').Append(force.faction)
                        .Append('|').Append(force.wave).Append('|').Append(force.recon).Append('|').Append(force.patrol);
                    AppendPosition(signature, force.position);
                }
                signature.Append("|scan15|evidence15|deadline720|warning30-45|materials80|cameraMargin80|noProduction|version3");
                mapEdit.FindProperty("contentHash").stringValue = Hash(signature.ToString());
                mapEdit.ApplyModifiedPropertiesWithoutUndo();
                foreach (var asset in new UnityEngine.Object[] { map, initial, startup, mission }) EditorUtility.SetDirty(asset);
                AssetDatabase.SaveAssets();
                if (!mission.TryValidate(out string error)) throw new InvalidOperationException(error);
                V3UiLocalizationCatalogBuilder.ApplyConfiguredUiTables();
                int missingHudScripts = 0;
                var hud = Load<GameObject>("Assets/Game/Prefabs/UI/Shell/Content/SCN08_MatchHudContent.prefab");
                foreach (var child in hud.GetComponentsInChildren<Transform>(true))
                    missingHudScripts += GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(child.gameObject);
                if (missingHudScripts != 0) throw new InvalidOperationException("Shared HUD has missing scripts: " + missingHudScripts);
                Debug.Log("[OperationsReconMissionAssetBuilder] hudMissingScripts=0");
                Debug.Log("[OperationsReconMissionAssetBuilder] result=Passed player=16 enemy=20 scans=3 reachable=8");
            }
            finally { blob.Dispose(); }
        }

        private static OperationsReconForceEntry Force(string key, Vector3 position, int count, byte faction, byte wave = 0, bool recon = false, bool patrol = false) =>
            new() { sourceKey = key, position = position, count = count, faction = faction, wave = wave, recon = recon, patrol = patrol };

        private static void Anchor(SerializedProperty anchor, string role, Vector3 position, OperationMapAnchorKind kind)
        {
            anchor.FindPropertyRelative("anchorId").stringValue = "anchor.operations.d01." + role;
            anchor.FindPropertyRelative("kind").intValue = (int)kind;
            anchor.FindPropertyRelative("position").vector3Value = position;
            anchor.FindPropertyRelative("eulerAngles").vector3Value = Vector3.zero;
            anchor.FindPropertyRelative("radius").floatValue = 8f;
            anchor.FindPropertyRelative("factionId").intValue = -1;
            anchor.FindPropertyRelative("laneIndex").intValue = -1;
        }

        private static Vector3 FindOpen(ref MapSurfaceBlob blob, int2 near)
        {
            for (int radius = 0; radius <= 24; radius++)
            for (int z = -radius; z <= radius; z++)
            for (int x = -radius; x <= radius; x++)
            {
                int2 point = near + new int2(x, z);
                bool clear = true;
                for (int dz = -3; dz <= 3 && clear; dz++)
                for (int dx = -3; dx <= 3 && clear; dx++) clear &= Walkable(ref blob, point + new int2(dx, dz));
                if (clear && MapSurfaceBlobAccess.TryGetPrimarySurface(ref blob, point, out var sample))
                    return new Vector3(point.x + .5f, sample.Height, point.y + .5f);
            }
            throw new InvalidOperationException("No clear ground near " + near);
        }

        private static bool Walkable(ref MapSurfaceBlob blob, int2 cell) =>
            Window.Contains(new Vector2Int(cell.x, cell.y)) && MapSurfaceBlobAccess.TryGetPrimarySurface(ref blob, cell, out var sample) &&
            sample.SurfaceType != MapSurfaceType.Blocked && (sample.MovementMask & MapSurfaceMovementMask.Infantry) != 0;

        private static void RequireReachable(ref MapSurfaceBlob blob, Vector3 start, Vector3[] destinations)
        {
            var seen = new HashSet<int2>(); var queue = new Queue<int2>();
            int2 first = new((int)start.x, (int)start.z); seen.Add(first); queue.Enqueue(first);
            int2[] offsets = { new(1,0), new(-1,0), new(0,1), new(0,-1) };
            while (queue.Count > 0)
            {
                int2 current = queue.Dequeue();
                foreach (var offset in offsets) { int2 next = current + offset; if (!seen.Contains(next) && Walkable(ref blob, next)) { seen.Add(next); queue.Enqueue(next); } }
            }
            foreach (var target in destinations)
                if (!seen.Contains(new int2((int)target.x, (int)target.z))) throw new InvalidOperationException("Unreachable mission role at " + target);
        }

        private static T Clone<T>(T source, string path) where T : ScriptableObject
        {
            if (source == null) throw new InvalidOperationException("Missing source for " + path);
            var target = AssetDatabase.LoadAssetAtPath<T>(path);
            if (target == null) { target = UnityEngine.Object.Instantiate(source); AssetDatabase.CreateAsset(target, path); }
            else EditorUtility.CopySerialized(source, target);
            return target;
        }
        private static T Load<T>(string path) where T : UnityEngine.Object => AssetDatabase.LoadAssetAtPath<T>(path) ?? throw new InvalidOperationException("Missing " + path);
        private static string Hash(string text) { using var sha = SHA256.Create(); return BitConverter.ToString(sha.ComputeHash(Encoding.UTF8.GetBytes(text))).Replace("-", "").ToLowerInvariant(); }
        private static void AppendPosition(StringBuilder target, Vector3 value)
        {
            var culture = System.Globalization.CultureInfo.InvariantCulture;
            target.Append('|').Append(value.x.ToString("R", culture)).Append(',').Append(value.y.ToString("R", culture)).Append(',').Append(value.z.ToString("R", culture));
        }
    }
}
