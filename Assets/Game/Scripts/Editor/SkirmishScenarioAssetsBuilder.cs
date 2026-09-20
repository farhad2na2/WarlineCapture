#if UNITY_EDITOR
using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Game.Components;
using Game.Configs;
using Game.UI.Runtime;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Editor
{
    public static class SkirmishScenarioAssetsBuilder
    {
        private const string Root = "Assets/Game/Configs/Skirmish/CityCrossroads";
        private const string SetupPath = "Assets/Game/Prefabs/UI/Shell/Content/SCN13_SkirmishSetupContent.prefab";
        private const string SourceMap = "Assets/Game/Configs/OperationMaps/Candidates/OperationMap_Compatibility_DesertBase01_DenseCity_EntityScene_Candidate.asset";
        public const string MapId = "opmap.skirmish.city_crossroads";

        public static string Build()
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Author scenarios in Edit mode.");
            System.IO.Directory.CreateDirectory(Root); AssetDatabase.Refresh();
            var initial = Copy<InitialUnitsSpawnerAuthoringConfig>("Assets/Game/Configs/Skirmish/InitialForces.asset", Root + "/InitialForces.asset");
            var so = new SerializedObject(initial);
            var factions = so.FindProperty("factions");
            for (int i = 0; i < 2; i++)
            {
                var faction = factions.GetArrayElementAtIndex(i);
                faction.FindPropertyRelative("spawnCell").vector2IntValue = new Vector2Int(i == 0 ? 1020 : 1100, i == 0 ? 750 : 400);
                // Authored clear lots, checked against the live road/water/static obstruction masks.
                Vector2Int[] sites = i == 0
                    ? new[] { new Vector2Int(1020,730), new Vector2Int(1000,760), new Vector2Int(1035,769), new Vector2Int(990,738), new Vector2Int(1000,720) }
                    : new[] { new Vector2Int(1116,361), new Vector2Int(1125,394), new Vector2Int(1170,406), new Vector2Int(1040,395), new Vector2Int(1198,400) };
                var buildings = faction.FindPropertyRelative("buildings");
                for (int j = 0; j < buildings.arraySize; j++)
                    buildings.GetArrayElementAtIndex(j).FindPropertyRelative("originOffset").vector2IntValue =
                        sites[j] - faction.FindPropertyRelative("spawnCell").vector2IntValue;
                var units = faction.FindPropertyRelative("units");
                for (int j = 0; j < units.arraySize; j++)
                {
                    Vector2Int offset = j < 2 ? new Vector2Int(j == 0 ? -8 : 8, i == 0 ? -45 : 50)
                        : j == 2 ? new Vector2Int(0, i == 0 ? -35 : 50)
                        : new Vector2Int(j == 3 ? -20 : 20, i == 0 ? -35 : 18);
                    units.GetArrayElementAtIndex(j).FindPropertyRelative("spawnOffset").vector2IntValue = offset;
                }
            }
            so.ApplyModifiedPropertiesWithoutUndo(); EditorUtility.SetDirty(initial);
            var construction = Copy<BuildingPlacementSystemConfig>("Assets/Game/Configs/Skirmish/Construction.asset", Root + "/Construction.asset");
            so = new SerializedObject(construction);
            so.FindProperty("initialUnitsConfig").objectReferenceValue = initial;
            so.ApplyModifiedPropertiesWithoutUndo(); EditorUtility.SetDirty(construction);
            var map = Copy<OperationMapDefinition>(SourceMap, Root + "/OperationMap_CityCrossroads.asset");
            ConfigureMap(map, AssetDatabase.LoadAssetAtPath<OperationMapDefinition>(SourceMap));
            var preset = Copy<SkirmishPresetConfig>("Assets/Game/Resources/SkirmishBaseAssault.asset", "Assets/Game/Resources/SkirmishCityCrossroads.asset");
            preset.buildingPlacement = construction;
            preset.operationMap = map;
            preset.playerReinforcementRallyOffset = new Vector3(16, 0, -35);
            EditorUtility.SetDirty(preset);
            var root = PrefabUtility.LoadPrefabContents(SetupPath);
            try { ConfigureChoices(root); PrefabUtility.SaveAsPrefabAsset(root, SetupPath); }
            finally { PrefabUtility.UnloadPrefabContents(root); }
            Localize(); AssetDatabase.SaveAssets();
            return "[SkirmishScenarioAssets] authored=2 validation=pending";
        }

        private static void ConfigureMap(OperationMapDefinition map, OperationMapDefinition source)
        {
            var so = new SerializedObject(map);
            so.FindProperty("operationMapId").stringValue = MapId;
            so.FindProperty("contentVersion").intValue = 1;
            so.FindProperty("contentHash").stringValue = Hash("city-crossroads-v1|1020,750|1100,400|900,260,1300,875|" + source.ContentHash);
            so.FindProperty("generatedMetadataHash").stringValue = Hash("city-crossroads-v1|north-south");
            var binding = so.FindProperty("sourceBinding");
            binding.FindPropertyRelative("sourceOperationMapId").stringValue = source.OperationMapId;
            binding.FindPropertyRelative("sourceIdentityHash").stringValue = source.SourceIdentityHash;
            binding.FindPropertyRelative("sourceContentHash").stringValue = source.ContentHash;
            var bounds = so.FindProperty("bounds");
            bounds.FindPropertyRelative("playableMin").vector3Value = new Vector3(900, source.Bounds.PlayableMin.y, 260);
            bounds.FindPropertyRelative("playableMax").vector3Value = new Vector3(1300, source.Bounds.PlayableMax.y, 875);
            bounds.FindPropertyRelative("cameraMin").vector3Value = new Vector3(900, 4, 260);
            bounds.FindPropertyRelative("cameraMax").vector3Value = new Vector3(1300, 100, 875);
            var cameras = so.FindProperty("cameras"); cameras.arraySize = 1;
            var camera = cameras.GetArrayElementAtIndex(0);
            const string cameraId = "camera.skirmish.city_crossroads.start";
            camera.FindPropertyRelative("cameraId").stringValue = cameraId;
            Vector3 position = new(1020, 70, 680), target = new(1020, 0, 735);
            camera.FindPropertyRelative("position").vector3Value = position;
            camera.FindPropertyRelative("eulerAngles").vector3Value = Quaternion.LookRotation(target - position).eulerAngles;
            camera.FindPropertyRelative("fieldOfView").floatValue = 50;
            so.FindProperty("planningCameraId").stringValue = cameraId;
            so.FindProperty("battleCameraId").stringValue = cameraId;
            var minimap = so.FindProperty("minimap");
            minimap.FindPropertyRelative("minimapId").stringValue = "minimap.skirmish.city_crossroads";
            minimap.FindPropertyRelative("projectionOrigin").vector3Value = new Vector3(900, 0, 260);
            minimap.FindPropertyRelative("projectionSize").vector2Value = new Vector2(400, 615);
            var anchors = so.FindProperty("anchors"); anchors.arraySize = 2;
            for (int i = 0; i < 2; i++)
            {
                var anchor = anchors.GetArrayElementAtIndex(i);
                anchor.FindPropertyRelative("anchorId").stringValue = "anchor.skirmish.city_crossroads.deployment.faction_" + (i + 1);
                anchor.FindPropertyRelative("kind").intValue = (int)OperationMapAnchorKind.Deployment;
                anchor.FindPropertyRelative("position").vector3Value = new Vector3(i == 0 ? 1020 : 1100, 0, i == 0 ? 750 : 400);
                anchor.FindPropertyRelative("eulerAngles").vector3Value = Vector3.zero;
                anchor.FindPropertyRelative("radius").floatValue = 8;
                anchor.FindPropertyRelative("factionId").intValue = i + 1;
                anchor.FindPropertyRelative("laneIndex").intValue = -1;
            }
            so.ApplyModifiedPropertiesWithoutUndo(); EditorUtility.SetDirty(map);
            if (!map.TryValidateMetadata(out string error) || !map.TryValidateLocalContentReferences(out error))
                throw new InvalidOperationException(error);
        }

        // Also called by the full prefab builder, so a future rebuild preserves the chooser.
        public static void ConfigureChoices(GameObject root)
        {
            var panel = root.transform.Find("SkirmishSetupComposition/OperationPreview");
            if (panel == null) throw new InvalidOperationException("Skirmish preview panel missing.");
            var existing = panel.Find("ScenarioChoices");
            if (existing != null) UnityEngine.Object.DestroyImmediate(existing.gameObject);
            var title = panel.Find("OperationName").GetComponent<TMP_Text>();
            Layout(title.rectTransform, 0, 1, 8, 8, 106, 52); title.fontSize = 31;
            Layout((RectTransform)panel.Find("TitleRule"), 0, 1, 3, 3, 160, 3);
            Layout((RectTransform)panel.Find("MapPreviewClip"), 0, 1, 4, 4, 166, 272);
            var row = new GameObject("ScenarioChoices", typeof(RectTransform)).GetComponent<RectTransform>();
            row.SetParent(panel, false); Layout(row, 0, 1, 10, 10, 10, 88);
            var buttons = new Button[2];
            for (int i = 0; i < 2; i++)
            {
                var rect = new GameObject("Scenario" + (i + 1), typeof(RectTransform), typeof(CanvasRenderer), typeof(V3GradientGraphic), typeof(Button)).GetComponent<RectTransform>();
                rect.SetParent(row, false); Layout(rect, i * .5f, (i + 1) * .5f, i == 0 ? 0 : 6, i == 0 ? 6 : 0, 0, 88);
                var graphic = rect.GetComponent<V3GradientGraphic>(); graphic.Configure(new Color(0,.3f,.4f), Color.black, Color.cyan, 3);
                var button = rect.GetComponent<Button>(); button.targetGraphic = graphic; buttons[i] = button;
                var label = new GameObject("Label", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI)).GetComponent<TextMeshProUGUI>();
                label.transform.SetParent(rect, false); label.font = title.font; label.fontSize = 25;
                label.alignment = TextAlignmentOptions.Center; label.raycastTarget = false;
                label.text = i == 0 ? "1 · DESERT BASE\nSELECTED" : "2 · CITY CROSSROADS\nSELECT BATTLE";
                Layout(label.rectTransform, 0, 1, 8, 8, 6, 76);
            }
            var description = panel.Find("SeedHelp").GetComponent<TMP_Text>();
            Layout(description.rectTransform, 0, 1, 20, 20, 512, 82);
            description.textWrappingMode = TextWrappingModes.Normal;
            var oldBinding = description.GetComponent<V3LocalizedTextBindingView>();
            if (oldBinding != null) UnityEngine.Object.DestroyImmediate(oldBinding);
            var view = new SerializedObject(root.GetComponent<QuickCustomScreenView>());
            var controls = view.FindProperty("scenarioButtons"); controls.arraySize = 2;
            for (int i = 0; i < 2; i++) controls.GetArrayElementAtIndex(i).objectReferenceValue = buttons[i];
            view.FindProperty("scenarioDescription").objectReferenceValue = description;
            view.ApplyModifiedPropertiesWithoutUndo();
        }
        private static void Layout(RectTransform rect, float min, float max, float left, float right, float top, float height)
        {
            rect.anchorMin = new Vector2(min,1); rect.anchorMax = new Vector2(max,1); rect.pivot = new Vector2(.5f,1);
            rect.offsetMin = new Vector2(left,-top-height); rect.offsetMax = new Vector2(-right,-top);
        }
        private static T Copy<T>(string source, string path) where T : ScriptableObject
        {
            var original = AssetDatabase.LoadAssetAtPath<T>(source);
            if (original == null) throw new InvalidOperationException(source);
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset == null) { asset = UnityEngine.Object.Instantiate(original); AssetDatabase.CreateAsset(asset,path); }
            else EditorUtility.CopySerialized(original,asset);
            asset.name = System.IO.Path.GetFileNameWithoutExtension(path); return asset;
        }
        private static string Hash(string text) => string.Concat(SHA256.Create().ComputeHash(Encoding.UTF8.GetBytes(text)).Select(b => b.ToString("x2")));
        private static void Localize()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<GameLocalizationCatalog>(V3UiLocalizationCatalogBuilder.CatalogPath);
            var tables = catalog.Locales.Select(locale =>
            {
                var entries = locale.Entries.ToDictionary(e => e.Key, e => e.Value);
                entries["ui.skirmish.city_crossroads_map"] = locale.LocaleCode == "fa-IR" ? "چهارراه شهر" : "CITY CROSSROADS";
                return new GameLocaleTable(locale.LocaleCode, locale.DisplayName, locale.ShortLabel, locale.RightToLeft,
                    locale.FontAsset, entries.OrderBy(e => e.Key).Select(e => new GameLocalizedStringRecord(e.Key,e.Value)));
            }).ToArray();
            catalog.Configure(catalog.SourceLocaleCode,tables); EditorUtility.SetDirty(catalog);
        }
    }
}
#endif
