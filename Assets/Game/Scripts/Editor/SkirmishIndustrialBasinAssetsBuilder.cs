#if UNITY_EDITOR
using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Game.Components;
using Game.Configs;
using Game.UI.Runtime;
using UnityEditor;
using UnityEngine;

namespace Game.Editor
{
    /// <summary>
    /// Authors Skirmish mission 3 — Industrial Basin Base Assault (catalog S073).
    /// Carves a western industrial window from the dense-city physical map with NW/SE bases.
    /// </summary>
    public static class SkirmishIndustrialBasinAssetsBuilder
    {
        private const string Root = "Assets/Game/Configs/Skirmish/IndustrialBasin";
        private const string SourceMap =
            "Assets/Game/Configs/OperationMaps/Candidates/OperationMap_Compatibility_DesertBase01_DenseCity_EntityScene_Candidate.asset";
        public const string MapId = "opmap.skirmish.industrial_basin";
        public const string MissionId = SkirmishPresetConfig.IndustrialBasinMissionId;
        public const string ScenarioSetupId = SkirmishPresetConfig.IndustrialBasinScenarioSetupId;

        /// <summary>Playable window in grid/world XZ cells; west of City Crossroads and Desert Base lots.</summary>
        public static readonly Vector2Int PlayableMin = new(480, 220);
        public static readonly Vector2Int PlayableMax = new(860, 640);

        [MenuItem("Tools/Warline/Skirmish/Rebuild Industrial Basin Scenario")]
        public static void RebuildMenu() => Debug.Log(Build());

        public static string Build()
        {
            if (EditorApplication.isPlaying)
                throw new InvalidOperationException("Author scenarios in Edit mode.");

            System.IO.Directory.CreateDirectory(Root);
            AssetDatabase.Refresh();

            var initial = Copy<InitialUnitsSpawnerAuthoringConfig>(
                "Assets/Game/Configs/Skirmish/InitialForces.asset", Root + "/InitialForces.asset");
            var so = new SerializedObject(initial);
            var factions = so.FindProperty("factions");
            for (int i = 0; i < 2; i++)
            {
                var faction = factions.GetArrayElementAtIndex(i);
                // Player northwest industrial yard; opponent southeast approach.
                faction.FindPropertyRelative("spawnCell").vector2IntValue =
                    new Vector2Int(i == 0 ? 560 : 760, i == 0 ? 550 : 280);
                Vector2Int[] sites = i == 0
                    ? new[]
                    {
                        new Vector2Int(560, 530), new Vector2Int(540, 560), new Vector2Int(575, 570),
                        new Vector2Int(530, 540), new Vector2Int(545, 520)
                    }
                    : new[]
                    {
                        // Live spawn evidence retained Barrack@795,260, Refinery@760,250 and
                        // Fuel@705,280; pack the remaining pads into that clear SE lot.
                        new Vector2Int(795, 260), new Vector2Int(730, 275), new Vector2Int(750, 295),
                        new Vector2Int(760, 250), new Vector2Int(705, 280)
                    };
                var buildings = faction.FindPropertyRelative("buildings");
                Vector2Int spawn = faction.FindPropertyRelative("spawnCell").vector2IntValue;
                for (int j = 0; j < buildings.arraySize; j++)
                    buildings.GetArrayElementAtIndex(j).FindPropertyRelative("originOffset").vector2IntValue =
                        sites[j] - spawn;

                var units = faction.FindPropertyRelative("units");
                for (int j = 0; j < units.arraySize; j++)
                {
                    // Face toward the opposing yard along the NW→SE diagonal.
                    Vector2Int offset = i == 0
                        ? (j < 2
                            ? new Vector2Int(j == 0 ? 12 : 28, j == 0 ? -28 : -20)
                            : j == 2
                                ? new Vector2Int(20, -14)
                                : new Vector2Int(j == 3 ? 8 : 32, j == 3 ? -8 : -4))
                        : (j < 2
                            ? new Vector2Int(j == 0 ? -28 : -12, j == 0 ? 20 : 28)
                            : j == 2
                                ? new Vector2Int(-20, 14)
                                : new Vector2Int(j == 3 ? -32 : -8, j == 3 ? 4 : 8));
                    units.GetArrayElementAtIndex(j).FindPropertyRelative("spawnOffset").vector2IntValue = offset;
                }
            }

            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(initial);

            var construction = Copy<BuildingPlacementSystemConfig>(
                "Assets/Game/Configs/Skirmish/Construction.asset", Root + "/Construction.asset");
            so = new SerializedObject(construction);
            so.FindProperty("initialUnitsConfig").objectReferenceValue = initial;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(construction);

            var map = Copy<OperationMapDefinition>(SourceMap, Root + "/OperationMap_IndustrialBasin.asset");
            ConfigureMap(map, AssetDatabase.LoadAssetAtPath<OperationMapDefinition>(SourceMap));

            var preset = Copy<SkirmishPresetConfig>(
                "Assets/Game/Resources/SkirmishBaseAssault.asset",
                "Assets/Game/Resources/SkirmishIndustrialBasin.asset");
            preset.buildingPlacement = construction;
            preset.operationMap = map;
            // Keep reinforcements with the opening infantry on the NW pad.
            preset.playerReinforcementRallyOffset = new Vector3(18, 0, -22);
            EditorUtility.SetDirty(preset);

            Localize();
            SkirmishBattleCatalogBuilder.Rebuild();
            AssetDatabase.SaveAssets();
            return "[SkirmishIndustrialBasin] authored=1 catalog=rebuilt validation=pending";
        }

        private static void ConfigureMap(OperationMapDefinition map, OperationMapDefinition source)
        {
            var so = new SerializedObject(map);
            so.FindProperty("operationMapId").stringValue = MapId;
            so.FindProperty("contentVersion").intValue = 1;
            so.FindProperty("contentHash").stringValue = Hash(
                "industrial-basin-v3|560,550|760,280|" +
                $"{PlayableMin.x},{PlayableMin.y},{PlayableMax.x},{PlayableMax.y}|" + source.ContentHash);
            so.FindProperty("generatedMetadataHash").stringValue = Hash("industrial-basin-v1|northwest-southeast");

            var binding = so.FindProperty("sourceBinding");
            binding.FindPropertyRelative("sourceOperationMapId").stringValue = source.OperationMapId;
            binding.FindPropertyRelative("sourceIdentityHash").stringValue = source.SourceIdentityHash;
            binding.FindPropertyRelative("sourceContentHash").stringValue = source.ContentHash;

            var bounds = so.FindProperty("bounds");
            bounds.FindPropertyRelative("playableMin").vector3Value =
                new Vector3(PlayableMin.x, source.Bounds.PlayableMin.y, PlayableMin.y);
            bounds.FindPropertyRelative("playableMax").vector3Value =
                new Vector3(PlayableMax.x, source.Bounds.PlayableMax.y, PlayableMax.y);
            bounds.FindPropertyRelative("cameraMin").vector3Value =
                new Vector3(PlayableMin.x, 4, PlayableMin.y);
            bounds.FindPropertyRelative("cameraMax").vector3Value =
                new Vector3(PlayableMax.x, 100, PlayableMax.y);

            var cameras = so.FindProperty("cameras");
            cameras.arraySize = 1;
            var camera = cameras.GetArrayElementAtIndex(0);
            const string cameraId = "camera.skirmish.industrial_basin.start";
            camera.FindPropertyRelative("cameraId").stringValue = cameraId;
            Vector3 position = new(560, 70, 480);
            Vector3 target = new(560, 0, 545);
            camera.FindPropertyRelative("position").vector3Value = position;
            camera.FindPropertyRelative("eulerAngles").vector3Value =
                Quaternion.LookRotation(target - position).eulerAngles;
            camera.FindPropertyRelative("fieldOfView").floatValue = 50;
            so.FindProperty("planningCameraId").stringValue = cameraId;
            so.FindProperty("battleCameraId").stringValue = cameraId;

            var minimap = so.FindProperty("minimap");
            minimap.FindPropertyRelative("minimapId").stringValue = "minimap.skirmish.industrial_basin";
            minimap.FindPropertyRelative("projectionOrigin").vector3Value =
                new Vector3(PlayableMin.x, 0, PlayableMin.y);
            minimap.FindPropertyRelative("projectionSize").vector2Value =
                new Vector2(PlayableMax.x - PlayableMin.x, PlayableMax.y - PlayableMin.y);

            var anchors = so.FindProperty("anchors");
            anchors.arraySize = 2;
            for (int i = 0; i < 2; i++)
            {
                var anchor = anchors.GetArrayElementAtIndex(i);
                anchor.FindPropertyRelative("anchorId").stringValue =
                    "anchor.skirmish.industrial_basin.deployment.faction_" + (i + 1);
                anchor.FindPropertyRelative("kind").intValue = (int)OperationMapAnchorKind.Deployment;
                anchor.FindPropertyRelative("position").vector3Value =
                    new Vector3(i == 0 ? 560 : 760, 0, i == 0 ? 550 : 280);
                anchor.FindPropertyRelative("eulerAngles").vector3Value = Vector3.zero;
                anchor.FindPropertyRelative("radius").floatValue = 8;
                anchor.FindPropertyRelative("factionId").intValue = i + 1;
                anchor.FindPropertyRelative("laneIndex").intValue = -1;
            }

            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(map);
            if (!map.TryValidateMetadata(out string error) || !map.TryValidateLocalContentReferences(out error))
                throw new InvalidOperationException(error);
        }

        private static T Copy<T>(string source, string path) where T : ScriptableObject
        {
            var original = AssetDatabase.LoadAssetAtPath<T>(source);
            if (original == null)
                throw new InvalidOperationException(source);
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset == null)
            {
                asset = UnityEngine.Object.Instantiate(original);
                AssetDatabase.CreateAsset(asset, path);
            }
            else
                EditorUtility.CopySerialized(original, asset);
            asset.name = System.IO.Path.GetFileNameWithoutExtension(path);
            return asset;
        }

        private static string Hash(string text) =>
            string.Concat(SHA256.Create().ComputeHash(Encoding.UTF8.GetBytes(text))
                .Select(b => b.ToString("x2")));

        private static void Localize()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<GameLocalizationCatalog>(V3UiLocalizationCatalogBuilder.CatalogPath);
            var tables = catalog.Locales.Select(locale =>
            {
                var entries = locale.Entries.ToDictionary(e => e.Key, e => e.Value);
                entries["ui.skirmish.industrial_basin_map"] =
                    locale.LocaleCode == "fa-IR" ? "حوضه صنعتی" : "INDUSTRIAL BASIN";
                return new GameLocaleTable(
                    locale.LocaleCode, locale.DisplayName, locale.ShortLabel, locale.RightToLeft,
                    locale.FontAsset, entries.OrderBy(e => e.Key).Select(e => new GameLocalizedStringRecord(e.Key, e.Value)));
            }).ToArray();
            catalog.Configure(catalog.SourceLocaleCode, tables);
            EditorUtility.SetDirty(catalog);
        }
    }
}
#endif
