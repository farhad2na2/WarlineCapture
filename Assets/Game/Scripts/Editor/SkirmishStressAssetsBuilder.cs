#if UNITY_EDITOR
using System;
using Game.Configs;
using UnityEditor;
using UnityEngine;

namespace Game.Editor
{
    public static class SkirmishStressAssetsBuilder
    {
        public const string Root = "Assets/Game/Configs/Skirmish/Stress";
        public const string PresetPath = "Assets/Game/Resources/SkirmishStressScaleProbe.asset";

        [MenuItem("Tools/Warline/Skirmish/Rebuild E0.3 Stress Preset")]
        public static void Rebuild()
        {
            Debug.Log(Build());
        }

        public static string Build()
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Author the stress preset in Edit mode.");
            System.IO.Directory.CreateDirectory(Root);
            AssetDatabase.Refresh();
            var initial = Copy<InitialUnitsSpawnerAuthoringConfig>(
                "Assets/Game/Configs/Skirmish/InitialForces.asset", Root + "/InitialForces.asset");
            WriteDefaultForces(initial);
            var registry = Copy<UnitPrefabRegistryAuthoringConfig>(
                "Assets/Game/Configs/Skirmish/UnitRegistry.asset", Root + "/UnitRegistry.asset");
            registry.UnitSpawnPrefabs.Clear();
            foreach (string path in SkirmishStressRecipe.RosterPrefabPaths)
                registry.UnitSpawnPrefabs.Add(Prefab(path));
            EditorUtility.SetDirty(registry);
            var construction = Copy<BuildingPlacementSystemConfig>(
                "Assets/Game/Configs/Skirmish/Construction.asset", Root + "/Construction.asset");
            var so = new SerializedObject(construction);
            so.FindProperty("initialUnitsConfig").objectReferenceValue = initial;
            so.FindProperty("unitPrefabRegistryConfig").objectReferenceValue = registry;
            string[] buildings =
            {
                "Building_Barrack", "Building_GuardTower", "Building_Ammunition_Depot",
                "Building_OilPump", "Building_Refinery", "Building_Fuel_Bladder", "Building_Helipad"
            };
            var spawnables = so.FindProperty("spawnables");
            spawnables.arraySize = buildings.Length;
            for (int i = 0; i < buildings.Length; i++)
                spawnables.GetArrayElementAtIndex(i).objectReferenceValue = Prefab("Buildings/" + buildings[i]);
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(construction);
            var preset = Copy<SkirmishPresetConfig>(
                "Assets/Game/Resources/SkirmishBaseAssault.asset", PresetPath);
            preset.buildingPlacement = construction;
            preset.firstAttackSeconds = 90f;
            preset.playerReinforcementRallyOffset = new Vector3(40, 0, 10);
            EditorUtility.SetDirty(preset);
            AssetDatabase.SaveAssets();
            return "[SkirmishStressAssets] authored=1 defaultScale=100 defaultLayout=spread validation=pending";
        }

        private static void WriteDefaultForces(InitialUnitsSpawnerAuthoringConfig initial)
        {
            var so = new SerializedObject(initial);
            so.FindProperty("blockerCount").intValue = 0;
            so.FindProperty("createFactionBases").boolValue = false;
            so.FindProperty("enableBlockerChurn").boolValue = false;
            so.FindProperty("spawnRadiusCells").intValue =
                SkirmishStressRecipe.SpawnRadiusCells(SkirmishStressRecipe.DefaultScale, SkirmishStressLayout.Spread);
            so.FindProperty("randomSeed").intValue = SkirmishStressRecipe.FixedSeed;
            var lines = SkirmishStressRecipe.UnitsFor(
                SkirmishStressRecipe.DefaultScale, SkirmishStressPhase.Warmup, true);
            var factions = so.FindProperty("factions");
            factions.arraySize = 2;
            for (int i = 0; i < 2; i++)
            {
                var faction = factions.GetArrayElementAtIndex(i);
                faction.FindPropertyRelative("factionId").intValue = i + 1;
                faction.FindPropertyRelative("spawnCell").vector2IntValue = SkirmishStressRecipe.SpawnCell(i + 1);
                var units = faction.FindPropertyRelative("units");
                units.arraySize = 0;
                for (int line = 0; line < lines.Length; line++)
                {
                    if (lines[line].CountPerSide <= 0) continue;
                    units.arraySize++;
                    var entry = units.GetArrayElementAtIndex(units.arraySize - 1);
                    entry.FindPropertyRelative("prefab").objectReferenceValue = Prefab(lines[line].PrefabPath);
                    entry.FindPropertyRelative("count").intValue = lines[line].CountPerSide;
                    entry.FindPropertyRelative("spawnOffset").vector2IntValue =
                        SkirmishStressRecipe.UnitOffset(lines[line], i + 1, SkirmishStressLayout.Spread);
                }
                var buildings = faction.FindPropertyRelative("buildings");
                Vector2Int[] sites = i == 0
                    ? new[]
                    {
                        new Vector2Int(-18, -21), new Vector2Int(-15, 35), new Vector2Int(-10, 65),
                        new Vector2Int(-16, 12), new Vector2Int(-10, -50), new Vector2Int(22, -42)
                    }
                    : new[]
                    {
                        new Vector2Int(-15, -21), new Vector2Int(-7, 32), new Vector2Int(-10, 52),
                        new Vector2Int(-30, 57), new Vector2Int(-10, -57), new Vector2Int(22, -48)
                    };
                buildings.arraySize = SkirmishStressRecipe.BuildingPrefabs.Length;
                for (int b = 0; b < sites.Length; b++)
                {
                    var building = buildings.GetArrayElementAtIndex(b);
                    building.FindPropertyRelative("prefab").objectReferenceValue = Prefab(SkirmishStressRecipe.BuildingPrefabs[b]);
                    building.FindPropertyRelative("originOffset").vector2IntValue = sites[b];
                }
            }
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(initial);
        }

        private static T Copy<T>(string source, string path) where T : ScriptableObject
        {
            var original = AssetDatabase.LoadAssetAtPath<T>(source);
            if (original == null) throw new InvalidOperationException(source);
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset == null)
            {
                asset = UnityEngine.Object.Instantiate(original);
                AssetDatabase.CreateAsset(asset, path);
            }
            else EditorUtility.CopySerialized(original, asset);
            asset.name = System.IO.Path.GetFileNameWithoutExtension(path);
            return asset;
        }

        private static GameObject Prefab(string path)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Game/Prefabs/" + path + ".prefab");
            if (prefab == null) throw new InvalidOperationException(path);
            return prefab;
        }
    }
}
#endif
