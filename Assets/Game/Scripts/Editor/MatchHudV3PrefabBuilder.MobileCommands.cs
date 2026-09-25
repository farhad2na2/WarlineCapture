#if UNITY_EDITOR
using System;
using System.Linq;
using Game.Configs;
using Game.UI.Runtime;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Editor
{
    public static partial class MatchHudV3PrefabBuilder
    {
        // Transfer one command slot to the five squad cards; retain the other tap targets.
        internal static void ApplyMobileCommandLayout(GameObject root)
        {
            var tray = FindDeepChild(root.transform, "SquadTray") as RectTransform;
            var rail = FindDeepChild(root.transform, "CommandRail") as RectTransform;
            if (tray == null || rail == null) return;
            SetTopLeft(tray, 10, 731, 707, 201);
            ApplySquadPagination(root, tray);
            SetTopLeft(rail, 719, 765, 943, 167);
            var frame = RequireRect(rail, "Frame");
            var legacyStop = FindDeepChild(frame, "StopCommand");
            if (legacyStop != null) legacyStop.gameObject.SetActive(false);
            foreach (var element in frame.GetComponentsInChildren<LayoutElement>(true))
            {
                if (element.transform.parent != frame) continue;
                float width = element.name == "BuildCommand" ? 152 : element.name == "SupportCommand" ? 130 : 123;
                element.minWidth = element.preferredWidth = width;
                element.flexibleWidth = 1;
            }
            for (int i = 1; i <= 5; i++)
            {
                var card = FindDeepChild(tray, "SquadCard" + i);
                if (card == null) continue;
                var portrait = FindDeepChild(card, "Portrait") as RectTransform;
                if (portrait != null)
                {
                    SetTopLeft(portrait, 7, 9, 125, 126);
                    portrait.anchorMax = Vector2.one;
                    portrait.sizeDelta = new Vector2(-14, 126);
                }
                var health = FindDeepChild(card, "HealthFrame") as RectTransform;
                if (health != null)
                {
                    SetTopLeft(health, 9, 174, 121, 10);
                    health.anchorMax = Vector2.one;
                    health.sizeDelta = new Vector2(-18, 10);
                }
            }
            var feedback = FindDeepChild(tray.parent, "FeedbackPanel") as RectTransform;
            if (feedback != null) SetTopLeft(feedback, 719, 710, 652, 48);
            // Preserve every existing responsive anchor while replacing only authored widths.
            foreach (var layout in root.GetComponentsInChildren<MainMenuV3SectionLayoutView>(true))
            {
                var data = new SerializedObject(layout);
                var targets = data.FindProperty("widthExpandedTargets");
                var sizes = data.FindProperty("widthTargetBaseSizes");
                for (int i = 0; i < targets.arraySize && i < sizes.arraySize; i++)
                {
                    var target = targets.GetArrayElementAtIndex(i).objectReferenceValue;
                    if (target == rail) sizes.GetArrayElementAtIndex(i).vector2Value = new Vector2(943, 167);
                    if (target == feedback) sizes.GetArrayElementAtIndex(i).vector2Value = new Vector2(652, 48);
                }
                data.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        private static Button BuildCeaseFireButton(GameObject root)
        {
            var controls = root.GetComponentInChildren<MatchOverlayCommandControlsView>(true);
            var controller = root.GetComponentInChildren<CommandWheelPanelView>(true);
            var wheel = RequireRect(controller.transform, "Wheel");
            var button = EnsureGeneratedButton(wheel, "CeaseFireButton", 0, 0, 300, 72,
                RedTop, RedBottom, theme.OrangeRed, "CEASE FIRE", 24);
            var rect = (RectTransform)button.transform;
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(.5f, 0);
            rect.anchoredPosition = new Vector2(0, -86);
            var label = button.GetComponentInChildren<TMP_Text>(true);
            var binding = label.GetComponent<V3LocalizedTextBindingView>() ?? label.gameObject.AddComponent<V3LocalizedTextBindingView>();
            binding.Configure("tactical.command.cease_fire", "CEASE FIRE");
            var data = new SerializedObject(controls);
            data.FindProperty("commandWheelStopButton").objectReferenceValue = button;
            data.ApplyModifiedPropertiesWithoutUndo();
            return button;
        }

        private static bool validateMobileCapture;
        private static void ValidateMobileGeometry(GameObject root, Camera camera, int width, int height)
        {
            Rect ScreenRect(RectTransform rect)
            {
                var corners = new Vector3[4]; rect.GetWorldCorners(corners);
                Vector2 low = camera.WorldToScreenPoint(corners[0]), high = camera.WorldToScreenPoint(corners[2]);
                return Rect.MinMaxRect(low.x, low.y, high.x, high.y);
            }
            void Inside(Rect rect, string name)
            {
                if (rect.xMin < -1 || rect.yMin < -1 || rect.xMax > width + 1 || rect.yMax > height + 1)
                    throw new InvalidOperationException(name + " leaves the screen: " + rect);
            }
            var tray = ScreenRect((RectTransform)FindDeepChild(root.transform, "SquadTray"));
            var rail = ScreenRect((RectTransform)FindDeepChild(root.transform, "CommandRail"));
            Inside(tray, "Squad tray"); Inside(rail, "Command rail");
            if (tray.xMax >= rail.xMin) throw new InvalidOperationException("Squad tray overlaps commands.");
            var controls = root.GetComponentInChildren<MatchOverlayCommandControlsView>(true);
            var ceaseFire = ScreenRect((RectTransform)controls.CommandWheelStopButton.transform);
            Inside(ceaseFire, "Cease Fire");
            if (ceaseFire.yMin <= rail.yMax || ceaseFire.height < 44)
                throw new InvalidOperationException("Cease Fire overlaps main commands or is too small.");
            var buttons = FindDeepChild(root.transform, "CommandRail").GetComponentsInChildren<Button>(true)
                .Where(b => b.gameObject.activeSelf && b.transform.parent.name == "Frame")
                .Select(b => ScreenRect((RectTransform)b.transform)).OrderBy(r => r.xMin).ToArray();
            for (int i = 0; i < buttons.Length; i++)
            {
                Inside(buttons[i], "Main command");
                if (buttons[i].width < 44 || buttons[i].height < 44 || (i > 0 && buttons[i-1].xMax > buttons[i].xMin + .5f))
                    throw new InvalidOperationException("Main command tap targets overlap or are too small.");
            }
            Debug.Log("[MobileCommands] geometry=Passed size=" + width + "x" + height);
        }

        public static void ValidateAndCaptureMobileCommands()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            var controls = prefab.GetComponentInChildren<MatchOverlayCommandControlsView>(true);
            var rail = FindDeepChild(prefab.transform, "CommandRail");
            var buttons = rail.GetComponentsInChildren<Button>(true).Where(b => b.transform.parent.name == "Frame" && b.gameObject.activeSelf).ToArray();
            if (buttons.Length != 7 || controls.StopButton.gameObject.activeSelf)
                throw new InvalidOperationException("Expected seven main commands with Stop hidden.");
            if (controls.CommandWheelStopButton == null || controls.CommandWheelStopButton.transform.parent.name != "Wheel")
                throw new InvalidOperationException("Cease Fire is missing from selected-unit Commands.");
            var tray = FindDeepChild(prefab.transform, "SquadTray") as RectTransform;
            if (Mathf.Abs(tray.sizeDelta.x - 707) > .1f)
                throw new InvalidOperationException("Squad tray did not receive the freed command width.");
            var catalog = AssetDatabase.LoadAssetAtPath<GameLocalizationCatalog>(V3UiLocalizationCatalogBuilder.CatalogPath);
            validateMobileCapture = true;
            try { foreach (var language in new[] { GameLocalization.EnglishLocaleCode, GameLocalization.PersianLocaleCode })
            {
                GameLocalization.Initialize(catalog, language, persist: false);
                Capture("/private/tmp/mobile-commands-" + language + ".png", 1920, 1080, true, applyLocalization: true);
                Capture("/private/tmp/mobile-commands-wide-" + language + ".png", 2400, 1080, true, applyLocalization: true);
            }
            } finally { validateMobileCapture = false; }
            Debug.Log("[MobileCommands] validation=Passed mainCommands=7 squadTray=707 languages=en,fa-IR captures=4");
        }

        public static void BuildMobileCommands()
        {
            LoadAssets();
            var root = PrefabUtility.LoadPrefabContents(PrefabPath);
            try
            {
                ApplyMobileCommandLayout(root);
                BuildCeaseFireButton(root);
                PrefabUtility.SaveAsPrefabAsset(root, PrefabPath, out bool saved);
                if (!saved) throw new InvalidOperationException("Mobile command prefab save failed.");
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
            M03RadarWarningLocalizationBuilder.Import();
            // Keep the retired slot stable for saves, but never request the removed toolbar action.
            foreach (string guid in AssetDatabase.FindAssets("t:ScenarioSetupConfig"))
            {
                var asset = AssetDatabase.LoadMainAssetAtPath(AssetDatabase.GUIDToAssetPath(guid));
                var data = new SerializedObject(asset);
                var steps = data.FindProperty("defense.guidanceSteps");
                if (steps == null) continue;
                for (int i = 0; i < steps.arraySize; i++)
                {
                    var step = steps.GetArrayElementAtIndex(i);
                    var action = step.FindPropertyRelative("action");
                    if (action.intValue != (int)Game.Missions.Contracts.MissionGuidanceActionKind.Stop) continue;
                    action.intValue = (int)Game.Missions.Contracts.MissionGuidanceActionKind.Explain;
                    step.FindPropertyRelative("completion").intValue = (int)Game.Missions.Contracts.MissionGuidanceCompletionKind.Acknowledged;
                }
                data.ApplyModifiedPropertiesWithoutUndo();
            }
            AssetDatabase.SaveAssets();
            Debug.Log("[MobileCommands] build=Passed");
        }
    }
}
#endif
