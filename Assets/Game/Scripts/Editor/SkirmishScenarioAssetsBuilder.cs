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
            // Reinforcements join the starting infantry instead of arriving on the exposed eastern flank.
            preset.playerReinforcementRallyOffset = new Vector3(-14, 0, -32);
            EditorUtility.SetDirty(preset);
            var root = PrefabUtility.LoadPrefabContents(SetupPath);
            try { ConfigureChoices(root); PrefabUtility.SaveAsPrefabAsset(root, SetupPath); }
            finally { PrefabUtility.UnloadPrefabContents(root); }
            Localize();
            SkirmishBattleCatalogBuilder.Rebuild();
            AssetDatabase.SaveAssets();
            return "[SkirmishScenarioAssets] authored=2 catalog=rebuilt validation=pending";
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

        // Also called by the full prefab builder, so a future rebuild preserves the library shell.
        public static void ConfigureChoices(GameObject root)
        {
            var panel = root.transform.Find("SkirmishSetupComposition/OperationPreview");
            if (panel == null) throw new InvalidOperationException("Skirmish preview panel missing.");
            foreach (string legacyName in new[] { "Metrics", "ObjectiveRow", "SeedMetric", "ScenarioChoices" })
            {
                Transform legacyNode = panel.Find(legacyName);
                if (legacyNode != null)
                    legacyNode.gameObject.SetActive(false);
            }
            Transform legacy = panel.Find("ScenarioChoices");
            if (legacy != null) UnityEngine.Object.DestroyImmediate(legacy.gameObject);
            Transform existingLibrary = panel.Find("BattleLibrary");
            if (existingLibrary != null) UnityEngine.Object.DestroyImmediate(existingLibrary.gameObject);
            Transform existingTabs = panel.Find("BattleLibraryMapTabs");
            if (existingTabs != null) UnityEngine.Object.DestroyImmediate(existingTabs.gameObject);
            Transform existingSearch = panel.Find("BattleLibrarySearch");
            if (existingSearch != null) UnityEngine.Object.DestroyImmediate(existingSearch.gameObject);
            Transform existingCount = panel.Find("BattleLibraryCount");
            if (existingCount != null) UnityEngine.Object.DestroyImmediate(existingCount.gameObject);

            // Steal width from the right rules column so map tabs and the list fit mobile touch targets.
            var preview = (RectTransform)panel;
            preview.anchorMin = new Vector2(0f, 1f);
            preview.anchorMax = new Vector2(0f, 1f);
            preview.pivot = new Vector2(0f, 1f);
            preview.anchoredPosition = new Vector2(14f, -125f);
            preview.sizeDelta = new Vector2(1080f, 671f);
            var rules = root.transform.Find("SkirmishSetupComposition/BaseAssaultRules") as RectTransform;
            if (rules != null)
            {
                rules.anchorMin = new Vector2(0f, 1f);
                rules.anchorMax = new Vector2(0f, 1f);
                rules.pivot = new Vector2(0f, 1f);
                rules.anchoredPosition = new Vector2(1108f, -125f);
                rules.sizeDelta = new Vector2(550f, 671f);
            }

            var title = panel.Find("OperationName").GetComponent<TMP_Text>();
            Layout(title.rectTransform, 0, 1, 10, 10, 8, 36); title.fontSize = 28;
            Layout((RectTransform)panel.Find("TitleRule"), 0, 1, 4, 4, 46, 3);

            var tabs = new GameObject("BattleLibraryMapTabs", typeof(RectTransform), typeof(HorizontalLayoutGroup))
                .GetComponent<RectTransform>();
            tabs.SetParent(panel, false);
            Layout(tabs, 0, 1, 8, 8, 54, 76);
            var tabsLayout = tabs.GetComponent<HorizontalLayoutGroup>();
            tabsLayout.spacing = 8f;
            tabsLayout.padding = new RectOffset(0, 0, 0, 0);
            tabsLayout.childAlignment = TextAnchor.MiddleCenter;
            tabsLayout.childControlWidth = true;
            tabsLayout.childControlHeight = true;
            tabsLayout.childForceExpandWidth = true;
            tabsLayout.childForceExpandHeight = true;

            var searchRoot = new GameObject("BattleLibrarySearch", typeof(RectTransform), typeof(Image), typeof(TMP_InputField))
                .GetComponent<RectTransform>();
            searchRoot.SetParent(panel, false);
            Layout(searchRoot, 0, 1, 8, 8, 138, 52);
            var searchImage = searchRoot.GetComponent<Image>();
            searchImage.color = new Color(.06f, .09f, .1f, 1f);
            var searchTextGo = new GameObject("Text", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            searchTextGo.transform.SetParent(searchRoot, false);
            var searchText = searchTextGo.GetComponent<TextMeshProUGUI>();
            searchText.font = title.font;
            searchText.fontSize = 24f;
            searchText.color = Color.white;
            searchText.raycastTarget = false;
            LayoutFill(searchText.rectTransform, 14, 14, 8, 8);
            var placeholderGo = new GameObject("Placeholder", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            placeholderGo.transform.SetParent(searchRoot, false);
            var placeholder = placeholderGo.GetComponent<TextMeshProUGUI>();
            placeholder.font = title.font;
            placeholder.fontSize = 24f;
            placeholder.color = new Color(1f, 1f, 1f, 0.35f);
            placeholder.text = "Search battles (S001, Desert, BA, …)";
            placeholder.raycastTarget = false;
            LayoutFill(placeholder.rectTransform, 14, 14, 8, 8);
            var search = searchRoot.GetComponent<TMP_InputField>();
            search.textViewport = searchRoot;
            search.textComponent = searchText;
            search.placeholder = placeholder;
            search.caretColor = Color.cyan;
            search.pointSize = 24f;

            var count = new GameObject("BattleLibraryCount", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI))
                .GetComponent<TextMeshProUGUI>();
            count.transform.SetParent(panel, false);
            count.font = title.font;
            count.fontSize = 20f;
            count.color = new Color(.7f, .78f, .8f, 1f);
            count.alignment = TextAlignmentOptions.MidlineLeft;
            count.text = "SHOWING 120 OF 120 BATTLES";
            Layout(count.rectTransform, 0, 1, 12, 12, 198, 26);

            var library = new GameObject("BattleLibrary", typeof(RectTransform), typeof(Image), typeof(RectMask2D), typeof(ScrollRect))
                .GetComponent<RectTransform>();
            library.SetParent(panel, false);
            Layout(library, 0, 1, 8, 8, 230, 342);
            var libraryImage = library.GetComponent<Image>();
            libraryImage.color = new Color(0f, 0f, 0f, 0.35f);
            libraryImage.raycastTarget = true;

            var viewport = new GameObject("Viewport", typeof(RectTransform), typeof(Image), typeof(RectMask2D))
                .GetComponent<RectTransform>();
            viewport.SetParent(library, false);
            viewport.anchorMin = Vector2.zero;
            viewport.anchorMax = Vector2.one;
            viewport.offsetMin = new Vector2(4f, 4f);
            viewport.offsetMax = new Vector2(-4f, -4f);
            var viewportImage = viewport.GetComponent<Image>();
            viewportImage.color = new Color(0f, 0f, 0f, 0.01f);
            viewportImage.raycastTarget = true;

            var content = new GameObject("Content", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter))
                .GetComponent<RectTransform>();
            content.SetParent(viewport, false);
            content.anchorMin = new Vector2(0f, 1f);
            content.anchorMax = new Vector2(1f, 1f);
            content.pivot = new Vector2(0.5f, 1f);
            content.anchoredPosition = Vector2.zero;
            content.sizeDelta = new Vector2(0f, 0f);
            var layoutGroup = content.GetComponent<VerticalLayoutGroup>();
            layoutGroup.spacing = 6f;
            layoutGroup.childAlignment = TextAnchor.UpperCenter;
            layoutGroup.childControlWidth = true;
            layoutGroup.childControlHeight = false;
            layoutGroup.childForceExpandWidth = true;
            layoutGroup.childForceExpandHeight = false;
            layoutGroup.padding = new RectOffset(2, 2, 2, 2);
            var fitter = content.GetComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var scroll = library.GetComponent<ScrollRect>();
            scroll.viewport = viewport;
            scroll.content = content;
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 32f;

            Layout((RectTransform)panel.Find("MapPreviewClip"), 0, 1, 8, 8, 580, 48);
            var mapFitter = panel.Find("MapPreviewClip").GetComponent<AspectRatioFitter>();
            if (mapFitter != null)
                mapFitter.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;

            var description = panel.Find("SeedHelp").GetComponent<TMP_Text>();
            Layout(description.rectTransform, 0, 1, 12, 12, 634, 28);
            description.fontSize = 22;
            description.textWrappingMode = TextWrappingModes.Normal;
            var oldBinding = description.GetComponent<V3LocalizedTextBindingView>();
            if (oldBinding != null) UnityEngine.Object.DestroyImmediate(oldBinding);

            var view = new SerializedObject(root.GetComponent<QuickCustomScreenView>());
            var legacyButtons = view.FindProperty("scenarioButtons");
            if (legacyButtons != null)
                legacyButtons.arraySize = 0;
            view.FindProperty("battleLibraryContent").objectReferenceValue = content;
            view.FindProperty("battleLibraryMapTabs").objectReferenceValue = tabs;
            view.FindProperty("battleLibrarySearch").objectReferenceValue = search;
            view.FindProperty("battleLibraryCountLabel").objectReferenceValue = count;
            view.FindProperty("scenarioDescription").objectReferenceValue = description;
            view.ApplyModifiedPropertiesWithoutUndo();

            // Keep SCN-13 responsive bases in sync so ultrawide does not open a gap
            // between the widened preview and the right rules column.
            RecaptureSectionLayoutBases(root);
        }

        private static void RecaptureSectionLayoutBases(GameObject root)
        {
            var layout = root.GetComponentInChildren<MainMenuV3SectionLayoutView>(true);
            if (layout == null)
                return;

            var so = new SerializedObject(layout);
            SerializedProperty widthTargets = so.FindProperty("widthExpandedTargets");
            SerializedProperty widthBases = so.FindProperty("widthTargetBaseSizes");
            if (widthTargets != null && widthBases != null)
            {
                widthBases.arraySize = widthTargets.arraySize;
                for (int i = 0; i < widthTargets.arraySize; i++)
                {
                    var target = widthTargets.GetArrayElementAtIndex(i).objectReferenceValue as RectTransform;
                    widthBases.GetArrayElementAtIndex(i).vector2Value =
                        target != null ? target.sizeDelta : Vector2.zero;
                }
            }

            SerializedProperty rightTargets = so.FindProperty("rightAnchoredTargets");
            SerializedProperty rightBases = so.FindProperty("rightTargetBasePositions");
            if (rightTargets != null && rightBases != null)
            {
                rightBases.arraySize = rightTargets.arraySize;
                for (int i = 0; i < rightTargets.arraySize; i++)
                {
                    var target = rightTargets.GetArrayElementAtIndex(i).objectReferenceValue as RectTransform;
                    rightBases.GetArrayElementAtIndex(i).vector2Value =
                        target != null ? target.anchoredPosition : Vector2.zero;
                }
            }

            so.ApplyModifiedPropertiesWithoutUndo();
            layout.RefreshLayout();
        }

        private static void LayoutFill(RectTransform rect, float left, float right, float top, float bottom)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(left, bottom);
            rect.offsetMax = new Vector2(-right, -top);
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
