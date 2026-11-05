#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Game.Configs;
using Game.UI.Runtime;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Editor
{
    /// <summary>Native authoring pass for the owner-approved 21 October HUD mockups.</summary>
    public static class ApprovedMatchHudPolishBuilder
    {
        public const string Hud = "Assets/Game/Prefabs/UI/Shell/Content/SCN08_MatchHudContent.prefab";
        public const string BuildDrawer = "Assets/Game/Prefabs/UI/Shell/Popups/SCN09_BuildDrawerPopup.prefab";
        public const string Placement = "Assets/Game/Prefabs/UI/Shell/Content/SCN08_BuildPlacementConfirmationBar.prefab";
        public const string Exchange = "Assets/Game/Prefabs/UI/Shell/Popups/POP12_ResourceExchangePopup.prefab";
        public const string Pause = "Assets/Game/Prefabs/UI/Popups/PauseMenuPopup.prefab";
        public const string Settings = "Assets/Game/Prefabs/UI/Shell/Popups/SCN_SettingsPopup.prefab";
        public const string Report = "Design/AgentReports/MatchHudReview20261021/Implementation";
        static readonly Color Cyan = new Color32(0, 194, 232, 255);
        static readonly Color Line = new Color32(82, 108, 120, 255);
        static readonly Color White = new Color32(244, 246, 242, 255);
        static readonly Color Muted = new Color32(164, 184, 194, 255);
        static TMP_FontAsset Font => AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Synty/InterfaceMilitaryCombatHUD/Fonts/Oxanium/Oxanium-Bold SDF.asset");

        [MenuItem("Game/UI/Approved/Apply Match HUD Polish")]
        public static void Build()
        {
            BuildNative();
            MissionEditorValidationExit.Complete(true);
        }

        public static void BuildAndReviewMenus()
        {
            BuildNative();
            if (!ApprovedMatchHudPolishValidation.ValidateNativeViews()) { MissionEditorValidationExit.Complete(false); return; }
            CampaignTutorialEntryEditorProbe.CaptureApprovedHudSuite();
        }

        public static void BuildAndReviewPassengers()
        {
            BuildNative();
            if (!ApprovedMatchHudPolishValidation.ValidateNativeViews()) { MissionEditorValidationExit.Complete(false); return; }
            CampaignTutorialEntryEditorProbe.CaptureCurrentPassengers();
        }

        static void BuildNative()
        {
            Directory.CreateDirectory(Report);
            AddStrings();
            Patch(Hud, ApplyHud);
            Patch(BuildDrawer, ApplyBuild);
            Patch(Placement, ApplyPlacement);
            Patch(Exchange, ApplyExchange);
            Patch(Pause, ApplyPause);
            Patch(Settings, ApplySettings);
            foreach (var path in new[] { "Assets/Game/Prefabs/UI/Shell/Popups/SCN02_MenuSettingsPopup.prefab", "Assets/Game/Prefabs/UI/Shell/Popups/SCN08_MatchSettingsPopup.prefab" })
                if (AssetDatabase.LoadAssetAtPath<GameObject>(path) != null) Patch(path, ApplySettings);
            AssetDatabase.SaveAssets();
            Debug.Log("[ApprovedMatchHudPolishBuilder] result=Passed nativePrefabs=true gameplayGatesUnchanged=true squadTray=DirectScroll");
        }

        static void Patch(string path, Action<GameObject> action)
        {
            var root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                action(root);
                PrefabUtility.SaveAsPrefabAsset(root, path, out bool saved);
                if (!saved) throw new InvalidOperationException("Cannot save " + path);
                string tree = string.Join("\n", root.GetComponentsInChildren<RectTransform>(true).Select(r => PathOf(r, root.transform) + " pos=" + r.anchoredPosition + " size=" + r.sizeDelta));
                File.WriteAllText(Report + "/" + System.IO.Path.GetFileNameWithoutExtension(path) + "-tree.txt", tree);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }

        public static void ApplyHud(GameObject root)
        {
            // Directly scrolling cards remain the only squad navigation surface.
            var obsolete = Find(root.transform, "SquadPagination");
            if (obsolete != null) UnityEngine.Object.DestroyImmediate(obsolete.gameObject);
            var feedback = root.GetComponentInChildren<BattleHudRuntimeFeedbackView>(true);
            if (feedback != null)
            {
                var rect = feedback.FeedbackPanel.transform as RectTransform;
                Place(rect, 719, 683, 943, 70); UpdateBase(root, rect);
                Place(feedback.FeedbackIcon != null ? feedback.FeedbackIcon.rectTransform : null, 16, 15, 40, 40);
                if (feedback.FeedbackText != null)
                {
                    Place(feedback.FeedbackText.rectTransform, 76, 7, 847, 56);
                    feedback.FeedbackText.fontSize = 26;
                    feedback.FeedbackText.enableAutoSizing = false;
                    feedback.FeedbackText.color = White;
                    feedback.FeedbackText.margin = Vector4.zero;
                    feedback.FeedbackText.textWrappingMode = TextWrappingModes.Normal;
                }
            }
            foreach (var minimap in root.GetComponentsInChildren<MatchHudMinimapView>(true))
            {
                Zoom(minimap.ZoomInButton, true); Zoom(minimap.ZoomOutButton, false);
            }
            var wheel = root.GetComponentInChildren<CommandWheelPanelView>(true);
            if (wheel != null)
            {
                var data = new SerializedObject(wheel);
                var ring = data.FindProperty("wheelRoot").objectReferenceValue as GameObject;
                if (ring != null)
                {
                    var rect = ring.transform as RectTransform;
                    rect.localScale = Vector3.one * .9f;
                    var radial = data.FindProperty("wheelTransform").objectReferenceValue as RectTransform;
                    if (radial == null) throw new InvalidOperationException("Radial layout binding missing");
                    Place(radial, 635, 265, 470, 470); radial.localScale = Vector3.one * .85f; UpdateBase(root, radial);
                    foreach (string name in new[] { "ApprovedHeading", "ApprovedClose" })
                    {
                        var stale = rect.Find(name); if (stale != null) stale.SetParent(radial, false);
                    }
                    var heading = Text(radial, "ApprovedHeading", "COMMANDS", 0, -64, 400, 52, 28, White, "hud.commands.heading");
                    heading.alignment = TextAlignmentOptions.Center;
                    Gradient(heading.rectTransform, new Color32(22, 45, 58, 255), new Color32(4, 15, 24, 255), Line, 2).raycastTarget = false;
                    var close = MakeButton(radial, "ApprovedClose", 416, -64, 54, 52, "×", 32, "neutral");
                    data.FindProperty("closeButton").objectReferenceValue = close;
                    data.ApplyModifiedPropertiesWithoutUndo();
                }
            }
            foreach (var drawer in root.GetComponentsInChildren<MatchHudTransportPassengerDrawerView>(true)) Passenger(drawer);
            foreach (var b in root.GetComponentsInChildren<Button>(true))
                if (b.name.Contains("Settings") || b.name.Contains("Pause")) CenterIcon(b);
        }

        static void Zoom(Button button, bool plus)
        {
            if (button == null) return;
            foreach (var image in button.GetComponentsInChildren<Image>(true))
            {
                image.sprite = null; image.color = Color.clear;
            }
            foreach (var t in button.GetComponentsInChildren<TMP_Text>(true)) t.gameObject.SetActive(false);
            Paint(button, "blue");
            Bar(button.transform, "ApprovedMinus", new Vector2(27, 4));
            if (plus) Bar(button.transform, "ApprovedPlus", new Vector2(4, 27));
            button.transition = Selectable.Transition.ColorTint;
            button.spriteState = default;
        }

        static void Passenger(MatchHudTransportPassengerDrawerView view)
        {
            var data = new SerializedObject(view);
            var rect = view.transform as RectTransform;
            Place(rect, 396, 214, 450, 494);
            var surface = rect.GetComponent<Image>(); if (surface != null) surface.color = Color.clear;
            var oldLayer = rect.Find("V3GradientLayer"); if (oldLayer != null) oldLayer.gameObject.SetActive(false);
            Gradient(rect, new Color32(19, 39, 52, 255), new Color32(4, 15, 24, 255), Line, 2);
            var header = Ref<TMP_Text>(view, "headerText");
            Place(header != null ? header.rectTransform : null, 14, 10, 422, 32);
            if (header != null) { header.fontSize = 21; header.enableAutoSizing = false; }
            var slots = Ref<RectTransform>(view, "capacitySlotsRoot");
            Place(slots, 14, 51, 422, 31);
            if (slots != null)
            {
                var layout = slots.GetComponent<HorizontalLayoutGroup>();
                if (layout != null) { layout.spacing = 5; layout.childForceExpandWidth = true; }
                for (int i = 0; i < slots.childCount; i++)
                {
                    var element = slots.GetChild(i).GetComponent<LayoutElement>();
                    if (element != null) { element.minWidth = 32; element.preferredWidth = 32; element.flexibleWidth = 1; }
                }
            }
            var item = Ref<MatchHudTransportPassengerItemView>(view, "itemTemplate");
            if (item != null)
            {
                var layout = item.GetComponent<LayoutElement>();
                if (layout != null) layout.minHeight = layout.preferredHeight = 76;
                Place(Find(item.transform, "Portrait") as RectTransform, 8, 6, 64, 64);
                Place(Find(item.transform, "Name") as RectTransform, 84, 4, 224, 27);
                Place(Find(item.transform, "Role") as RectTransform, 84, 29, 224, 22);
                Place(Find(item.transform, "HealthFrame") as RectTransform, 84, 56, 126, 12);
                var hp = Find(item.transform, "Health");
                if (hp != null) Place(hp as RectTransform, 130, -4, 78, 22);
                var exit = Ref<Button>(item, "exitButton");
                if (exit != null) { Place(exit.transform as RectTransform, 314, 10, 98, 56); Paint(exit, "blue"); }
                foreach (var t in item.GetComponentsInChildren<TMP_Text>(true))
                {
                    t.fontSize = t.name == "Name" ? 19 : t.name == "Role" ? 14 : 16;
                    t.margin = Vector4.zero;
                }
            }
            var content = Ref<RectTransform>(view, "contentRoot");
            if (content != null)
            {
                var layout = content.GetComponent<VerticalLayoutGroup>();
                if (layout != null) layout.spacing = 6;
            }
            var empty = Ref<GameObject>(view, "emptyStateRoot");
            if (empty != null)
            {
                var label = Ref<TMP_Text>(view, "emptyStateText");
                Place(label != null ? label.rectTransform : null, 8, 24, 394, 40);
                if (label != null) { label.fontSize = 21; label.color = Muted; }
                Text(empty.transform, "ApprovedHint", "Select infantry, then Board and tap this transport.", 12, 76, 386, 64, 17, Muted,
                    "hud.passenger.empty_hint");
            }
            var exitAll = Ref<Button>(view, "exitAllButton"); if (exitAll != null) Paint(exitAll, "blue");
            var close = Ref<Button>(view, "closeButton"); if (close != null) Paint(close, "neutral");
        }

        public static void ApplyBuild(GameObject root)
        {
            var view = root.GetComponent<BuildDrawerView>();
            if (view == null) throw new InvalidOperationException("Build drawer binding missing");
            foreach (var rt in root.GetComponentsInChildren<RectTransform>(true))
            {
                if (rt.name != "ItemView" && !rt.name.StartsWith("PreviewCard")) continue;
                Horizontal(Find(rt, "Title") as RectTransform, 12, 12, 4, 39);
                StretchBetween(Find(rt, "ArtClip") as RectTransform, 3, 3, 45, 51);
                var cost = Find(rt, "CostPanel") as RectTransform;
                Bottom(cost, 3, 3, 3, 48);
                if (cost != null)
                    foreach (var label in cost.GetComponentsInChildren<TMP_Text>(true)) { Typography(label, 24); }
                if (cost != null)
                {
                    var shade = Find(cost, "CostShade") as RectTransform; if (shade != null) Stretch(shade, 0);
                    string[] groups = { "MaterialsTinyCost", "FuelTinyCost", "TimeTinyCost" };
                    for (int i = 0; i < groups.Length; i++)
                    {
                        var group = Find(cost, groups[i]) as RectTransform;
                        if (group == null) continue;
                        group.anchorMin = new Vector2(i / 3f, 1); group.anchorMax = new Vector2((i + 1) / 3f, 1);
                        group.pivot = new Vector2(0, 1); group.anchoredPosition = new Vector2(8, -4); group.sizeDelta = new Vector2(-12, 40);
                    }
                }
                StretchBetween(Find(rt, "DisabledOverlay") as RectTransform, 3, 3, 45, 3);
                var gradient = rt.GetComponent<V3GradientGraphic>();
                if (gradient != null) gradient.Configure(new Color32(22, 45, 58, 255), new Color32(4, 15, 24, 255), Line, 2);
            }
            var itemTitle = Find(view.ItemTemplate.transform, "Title")?.GetComponent<TMP_Text>();
            if (itemTitle != null) Typography(itemTitle, 30);
            foreach (var tab in view.Tabs ?? Array.Empty<BuildDrawerTabView>())
            {
                if (tab == null || tab.Button == null) continue;
                foreach (var t in tab.Button.GetComponentsInChildren<TMP_Text>(true)) if (t.text == "AIRCRAFTS")
                {
                    t.text = "AIRCRAFT";
                    var binding = t.GetComponent<V3LocalizedTextBindingView>();
                    if (binding != null) binding.Configure("hud.build.aircraft", "AIRCRAFT");
                }
            }
            // Keep RTL labels inside their frames and separate numeric stat values.
            foreach (var tab in view.Tabs ?? Array.Empty<BuildDrawerTabView>())
                if (tab != null && tab.Button != null)
                    foreach (var text in tab.Button.GetComponentsInChildren<TMP_Text>(true))
                        text.margin = new Vector4(4, 2, 12, 2);
            var detail = Find(root.transform, "DetailPanel");
            if (detail != null)
            {
                foreach (string row in new[] { "Footprint", "MaterialsCost", "FuelCost", "BuildTime" })
                {
                    var stat = Find(detail, row); if (stat == null) continue;
                    Place(Find(stat, "Label") as RectTransform, 39, 3, 163, 32);
                    var value = Find(stat, "Value")?.GetComponent<TMP_Text>();
                    if (value != null)
                    {
                        Place(value.rectTransform, 220, 3, 108, 32);
                        value.alignment = TextAlignmentOptions.Center;
                        value.margin = new Vector4(4, 0, 4, 0);
                    }
                }
                foreach (string name in new[] { "Name", "Role", "Requirements" })
                {
                    var text = Find(detail, name)?.GetComponent<TMP_Text>();
                    if (text != null) text.margin = new Vector4(4, 2, 8, 2);
                }
            }
            Paint(view.PrimaryActionButton, "green");
            if (view.CloseButton != null) CenterIcon(view.CloseButton);
        }

        public static void ApplyPlacement(GameObject root)
        {
            var view = root.GetComponent<BuildPlacementConfirmationBarView>();
            if (view == null) throw new InvalidOperationException("Placement binding missing");
            Paint(view.ConfirmButton, "green");
            Paint(Ref<Button>(view, "cancelButton"), "neutral");
            Paint(Ref<Button>(view, "rotateButton"), "neutral");
            var portrait = Ref<Image>(view, "buildingPortrait");
            if (portrait != null)
            {
                // Runtime typed query supplies the real selected definition.
                portrait.sprite = null; portrait.enabled = false;
            }
        }

        public static void ApplyExchange(GameObject root)
        {
            var view = root.GetComponent<ResourceExchangePopupView>();
            if (view == null) throw new InvalidOperationException("Exchange binding missing");
            foreach (var card in view.StaticRecipeCards ?? Array.Empty<ResourceExchangeRecipeCardView>())
            {
                if (card == null) continue;
                var icon = Find(card.transform, "IconPanel");
                if (icon != null)
                {
                    icon.gameObject.SetActive(true); Place(icon as RectTransform, 10, 18, 58, 58);
                    var thumb = Find(icon, "Thumbnail") as RectTransform;
                    if (thumb != null) { Stretch(thumb, 5); var image = thumb.GetComponent<Image>(); if (image != null) image.preserveAspect = true; }
                }
                // Reserve the checkmark column in both languages.
                Place(Find(card.transform, "Title") as RectTransform, 80, 8, 224, 35);
                Place(Find(card.transform, "Input") as RectTransform, 80, 42, 250, 24);
                Place(Find(card.transform, "Output") as RectTransform, 80, 68, 250, 24);
                foreach (var t in card.GetComponentsInChildren<TMP_Text>(true)) if (t.name is "Input" or "Output") { t.fontSize = 17; t.enableAutoSizing = false; t.textWrappingMode = TextWrappingModes.NoWrap; }
            }
            Paint(view.AmountDecreaseButton, "neutral"); Paint(view.AmountIncreaseButton, "neutral");
            Paint(view.FooterConfirmButton, "green"); Paint(view.ConfirmButton, "green");
            var minus = view.AmountDecreaseButton.transform as RectTransform;
            var plus = view.AmountIncreaseButton.transform as RectTransform;
            var amount = Ref<TMP_Text>(view, "detailAmountText");
            if (minus != null && plus != null && amount != null)
            {
                amount.gameObject.SetActive(true); amount.enabled = true; amount.color = White;
                amount.alignment = TextAlignmentOptions.Center; amount.fontSize = 36; amount.enableAutoSizing = false;
                minus.anchorMin = minus.anchorMax = new Vector2(.5f, 1); minus.pivot = new Vector2(.5f, 1); minus.anchoredPosition = new Vector2(-210, -118); minus.sizeDelta = new Vector2(80, 80);
                plus.anchorMin = plus.anchorMax = new Vector2(.5f, 1); plus.pivot = new Vector2(.5f, 1); plus.anchoredPosition = new Vector2(210, -118); plus.sizeDelta = new Vector2(80, 80);
                amount.rectTransform.anchorMin = amount.rectTransform.anchorMax = new Vector2(.5f, 1); amount.rectTransform.pivot = new Vector2(.5f, 1); amount.rectTransform.anchoredPosition = new Vector2(0, -118); amount.rectTransform.sizeDelta = new Vector2(320, 80);
                Gradient(amount.rectTransform, new Color32(23, 45, 60, 255), new Color32(4, 16, 26, 255), Line, 2).raycastTarget = false;
            }
            var warning = Ref<TMP_Text>(view, "detailInstructionText");
            if (warning != null) { warning.color = new Color32(247, 184, 30, 255); warning.fontSize = 23; }
            if (view.CloseButton != null) CenterIcon(view.CloseButton);
        }

        public static void ApplyPause(GameObject root)
        {
            var view = root.GetComponent<PauseOptionsV3PopupView>() ?? root.GetComponentInChildren<PauseOptionsV3PopupView>(true);
            if (view == null) throw new InvalidOperationException("Pause binding missing");
            Paint(view.ResumeButton, "green"); Paint(view.RestartButton, "neutral");
            Paint(view.SettingsButton, "blue"); Paint(view.HelpButton, "blue"); Paint(view.ExitButton, "red");
            CenterIcon(view.CloseButton);
            foreach (var rt in root.GetComponentsInChildren<RectTransform>(true)) if (rt.name.Contains("Star")) rt.gameObject.SetActive(false);
            // No authoritative autosave timestamp is projected for Campaign yet.
            var autosave = Find(root.transform, "AutosaveTime")?.GetComponent<TMP_Text>();
            if (autosave != null) autosave.text = "—";
        }

        public static void ApplySettings(GameObject root)
        {
            var view = root.GetComponent<SettingsPopupView>() ?? root.GetComponentInChildren<SettingsPopupView>(true);
            if (view != null) { Paint(view.ResetButton, "neutral"); Paint(view.ApplyButton, "green"); CenterIcon(view.CloseButton); }
            var header = Find(root.transform, "Header");
            var title = header != null ? Find(header, "TitleText")?.GetComponent<TMP_Text>() : null;
            if (title != null)
            {
                var rect = title.rectTransform;
                rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
                rect.offsetMin = new Vector2(160, 12); rect.offsetMax = new Vector2(-136, -12);
                title.margin = new Vector4(12, 0, 12, 0);
            }
            var rail = Find(root.transform, "SettingsTabRail");
            if (rail != null)
                foreach (var text in rail.GetComponentsInChildren<TMP_Text>(true))
                    text.margin = new Vector4(8, 2, 24, 2);
            foreach (var slider in root.GetComponentsInChildren<Slider>(true))
            {
                if (slider.handleRect != null)
                {
                    var thumb = slider.handleRect;
                    thumb.anchorMin = new Vector2(thumb.anchorMin.x, .5f); thumb.anchorMax = new Vector2(thumb.anchorMax.x, .5f);
                    thumb.pivot = new Vector2(.5f, .5f); thumb.anchoredPosition = new Vector2(thumb.anchoredPosition.x, 0);
                    thumb.sizeDelta = new Vector2(24, 38);
                    var image = slider.handleRect.GetComponent<Image>(); if (image != null) { image.sprite = null; image.color = White; }
                }
            }
        }

        static void AddStrings()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<GameLocalizationCatalog>(V3UiLocalizationCatalogBuilder.CatalogPath);
            var values = new[] {
                ("hud.build.place", "PLACE BUILDING", "قرار دادن ساختمان"),
                ("hud.build.aircraft", "AIRCRAFT", "هواگردها"),
                ("hud.commands.heading", "COMMANDS", "فرمان‌ها"),
                ("hud.pause.time", "CURRENT TIME", "زمان فعلی"),
                ("hud.build.preview_hint", "Select {0} to begin.", "برای شروع {0} را انتخاب کنید."),
                ("hud.passenger.empty", "No passengers aboard.", "هیچ مسافری سوار نیست."),
                ("hud.passenger.unload_all", "UNLOAD ALL", "پیاده کردن همه"),
                ("hud.passenger.unload", "UNLOAD", "پیاده کردن"),
                ("hud.passenger.capacity", "PASSENGERS {0}/{1}", "مسافران {0}/{1}"),
                ("hud.passenger.empty_hint", "Select infantry, then Board and tap this transport.", "پیاده‌نظام را انتخاب کنید؛ سپس سوار شدن را بزنید و این ترابری را انتخاب کنید.") };
            var locales = new List<GameLocaleTable>();
            foreach (var table in catalog.Locales)
            {
                var keys = new HashSet<string>(values.Select(v => v.Item1));
                var entries = table.Entries.Where(e => !keys.Contains(e.Key)).ToList();
                foreach (var value in values) entries.Add(new GameLocalizedStringRecord(value.Item1, table.LocaleCode == "fa-IR" ? value.Item3 : value.Item2));
                locales.Add(new GameLocaleTable(table.LocaleCode, table.DisplayName, table.ShortLabel, table.RightToLeft, table.FontAsset, entries));
            }
            catalog.Configure(catalog.SourceLocaleCode, locales); EditorUtility.SetDirty(catalog);
        }

        static void Paint(Button button, string tone)
        {
            if (button == null) return;
            Color top = new Color32(49, 68, 80, 255), bottom = new Color32(9, 23, 32, 255), border = Line;
            if (tone == "blue") { top = new Color32(14, 112, 171, 255); bottom = new Color32(2, 42, 67, 255); border = Cyan; }
            if (tone == "green") { top = new Color32(28, 155, 58, 255); bottom = new Color32(3, 65, 29, 255); border = new Color32(49, 210, 84, 255); }
            if (tone == "red") { top = new Color32(171, 46, 27, 255); bottom = new Color32(79, 13, 8, 255); border = new Color32(236, 74, 46, 255); }
            var image = button.GetComponent<Image>(); if (image != null) { image.sprite = null; image.color = Color.clear; }
            var gradient = Gradient(button.transform as RectTransform, top, bottom, border, 2);
            button.targetGraphic = gradient; gradient.raycastTarget = true;
            foreach (string name in new[] { "ActionFill", "V3GradientLayer" })
            {
                var previous = button.transform.Find(name); if (previous != null) previous.gameObject.SetActive(false);
            }
            button.transition = Selectable.Transition.ColorTint; button.spriteState = default;
            var colors = button.colors; colors.normalColor = Color.white; colors.disabledColor = Color.white; button.colors = colors;
            foreach (var t in button.GetComponentsInChildren<TMP_Text>(true)) { t.margin = new Vector4(6, 2, 6, 2); t.color = White; }
        }

        static V3GradientGraphic Gradient(RectTransform rect, Color top, Color bottom, Color border, float stroke)
        {
            var graphic = rect.GetComponent<V3GradientGraphic>();
            if (rect.GetComponent<TMP_Text>() != null)
            {
                var obsolete = rect.Find("ApprovedSurface");
                if (obsolete != null) UnityEngine.Object.DestroyImmediate(obsolete.gameObject);
                string name = rect.name + "ApprovedSurface";
                var sibling = rect.parent.Find(name) as RectTransform;
                if (sibling == null)
                {
                    var go = new GameObject(name, typeof(RectTransform), typeof(V3GradientGraphic));
                    go.layer = rect.gameObject.layer; go.transform.SetParent(rect.parent, false);
                    sibling = go.GetComponent<RectTransform>();
                }
                sibling.anchorMin = rect.anchorMin; sibling.anchorMax = rect.anchorMax; sibling.pivot = rect.pivot;
                sibling.anchoredPosition = rect.anchoredPosition; sibling.sizeDelta = rect.sizeDelta;
                sibling.SetAsFirstSibling();
                graphic = sibling.GetComponent<V3GradientGraphic>();
            }
            if (graphic == null && rect.GetComponent<Graphic>() != null)
            {
                var surface = rect.Find("ApprovedSurface") as RectTransform;
                if (surface == null)
                {
                    var go = new GameObject("ApprovedSurface", typeof(RectTransform), typeof(V3GradientGraphic));
                    go.layer = rect.gameObject.layer;
                    go.transform.SetParent(rect, false);
                    surface = go.GetComponent<RectTransform>();
                }
                surface.SetAsFirstSibling(); Stretch(surface, 0);
                graphic = surface.GetComponent<V3GradientGraphic>();
            }
            if (graphic == null) graphic = rect.gameObject.AddComponent<V3GradientGraphic>();
            graphic.Configure(top, bottom, border, stroke); graphic.SetMilitaryMaterial(MilitaryUiMaterialKind.Automatic); graphic.color = Color.white; return graphic;
        }
        static void Typography(TMP_Text text, float size)
        {
            text.fontSize = size; text.fontSizeMin = text.fontSizeMax = size; text.enableAutoSizing = false;
            var binding = text.GetComponent<V3LocalizedTextBindingView>();
            if (binding == null) return;
            var data = new SerializedObject(binding);
            data.FindProperty("minimumPresentationSize").floatValue = size;
            data.FindProperty("maximumPresentationSize").floatValue = size;
            data.ApplyModifiedPropertiesWithoutUndo();
        }

        static void CenterIcon(Button button)
        {
            if (button == null) return;
            var icon = Find(button.transform, "Icon") as RectTransform;
            if (icon == null) return;
            icon.anchorMin = icon.anchorMax = icon.pivot = new Vector2(.5f, .5f); icon.anchoredPosition = Vector2.zero;
            var image = icon.GetComponent<Image>(); if (image != null) image.preserveAspect = true;
        }
        static void Bar(Transform parent, string name, Vector2 size)
        {
            var child = parent.Find(name) as RectTransform;
            if (child == null) { var go = new GameObject(name, typeof(RectTransform), typeof(Image)); go.layer = parent.gameObject.layer; go.transform.SetParent(parent, false); child = go.GetComponent<RectTransform>(); }
            child.anchorMin = child.anchorMax = child.pivot = new Vector2(.5f, .5f); child.anchoredPosition = Vector2.zero; child.sizeDelta = size;
            var image = child.GetComponent<Image>(); image.sprite = null; image.color = White; image.raycastTarget = false;
        }
        static TMP_Text Text(Transform parent, string name, string value, float x, float y, float w, float h, float size, Color color, string key = null)
        {
            var child = parent.Find(name);
            var text = child != null ? child.GetComponent<TMP_Text>() : null;
            if (text == null) { var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI)); go.layer = parent.gameObject.layer; go.transform.SetParent(parent, false); text = go.GetComponent<TMP_Text>(); }
            Place(text.rectTransform, x, y, w, h); text.font = Font; text.fontSize = size; text.color = color; text.text = value; text.raycastTarget = false; text.textWrappingMode = TextWrappingModes.Normal;
            if (key != null) { var binding = text.GetComponent<V3LocalizedTextBindingView>() ?? text.gameObject.AddComponent<V3LocalizedTextBindingView>(); binding.Configure(key, value); }
            return text;
        }
        static Button MakeButton(Transform parent, string name, float x, float y, float w, float h, string label, float size, string tone)
        {
            var child = parent.Find(name); var rect = child as RectTransform;
            if (rect == null) { var go = new GameObject(name, typeof(RectTransform), typeof(Button)); go.layer = parent.gameObject.layer; go.transform.SetParent(parent, false); rect = go.GetComponent<RectTransform>(); }
            Place(rect, x, y, w, h); var button = rect.GetComponent<Button>() ?? rect.gameObject.AddComponent<Button>(); Paint(button, tone);
            var text = Text(rect, "Label", label, 0, 0, w, h, size, White); text.alignment = TextAlignmentOptions.Center; return button;
        }
        static T Ref<T>(UnityEngine.Object owner, string field) where T : UnityEngine.Object => new SerializedObject(owner).FindProperty(field)?.objectReferenceValue as T;
        static Transform Find(Transform root, string name) { if (root.name == name) return root; foreach (Transform child in root) { var found = Find(child, name); if (found != null) return found; } return null; }
        static string PathOf(Transform child, Transform root) => child == root ? child.name : PathOf(child.parent, root) + "/" + child.name;
        static void Place(RectTransform rect, float x, float y, float w, float h) { if (rect == null) return; rect.anchorMin = rect.anchorMax = new Vector2(0, 1); rect.pivot = new Vector2(0, 1); rect.anchoredPosition = new Vector2(x, -y); rect.sizeDelta = new Vector2(w, h); }
        static void Horizontal(RectTransform r, float left, float right, float top, float height) { if (r == null) return; r.anchorMin = new Vector2(0, 1); r.anchorMax = Vector2.one; r.pivot = new Vector2(0, 1); r.anchoredPosition = new Vector2(left, -top); r.sizeDelta = new Vector2(-left - right, height); }
        static void Bottom(RectTransform r, float left, float right, float bottom, float height) { if (r == null) return; r.anchorMin = Vector2.zero; r.anchorMax = new Vector2(1, 0); r.pivot = Vector2.zero; r.anchoredPosition = new Vector2(left, bottom); r.sizeDelta = new Vector2(-left - right, height); }
        static void Stretch(RectTransform r, float inset) { if (r == null) return; r.anchorMin = Vector2.zero; r.anchorMax = Vector2.one; r.offsetMin = Vector2.one * inset; r.offsetMax = Vector2.one * -inset; }
        static void StretchBetween(RectTransform r, float left, float right, float top, float bottom) { if (r == null) return; r.anchorMin = Vector2.zero; r.anchorMax = Vector2.one; r.offsetMin = new Vector2(left, bottom); r.offsetMax = new Vector2(-right, -top); }
        static void UpdateBase(GameObject root, RectTransform target)
        {
            foreach (var layout in root.GetComponentsInChildren<MainMenuV3SectionLayoutView>(true))
            {
                var data = new SerializedObject(layout);
                var targets = data.FindProperty("widthExpandedTargets"); var sizes = data.FindProperty("widthTargetBaseSizes");
                for (int i = 0; i < targets.arraySize && i < sizes.arraySize; i++) if (targets.GetArrayElementAtIndex(i).objectReferenceValue == target) sizes.GetArrayElementAtIndex(i).vector2Value = target.sizeDelta;
                foreach (var pair in new[] { ("rightAnchoredTargets", "rightTargetBasePositions"), ("centerAnchoredTargets", "centerTargetBasePositions") })
                {
                    var anchored = data.FindProperty(pair.Item1); var positions = data.FindProperty(pair.Item2);
                    for (int i = 0; i < anchored.arraySize && i < positions.arraySize; i++)
                        if (anchored.GetArrayElementAtIndex(i).objectReferenceValue == target)
                            positions.GetArrayElementAtIndex(i).vector2Value = target.anchoredPosition;
                }
                var horizontal = data.FindProperty("horizontalResponsiveTargets");
                var horizontalPositions = data.FindProperty("horizontalTargetBasePositions");
                for (int i = 0; i < horizontal.arraySize && i < horizontalPositions.arraySize; i++)
                    if (horizontal.GetArrayElementAtIndex(i).FindPropertyRelative("target").objectReferenceValue == target)
                        horizontalPositions.GetArrayElementAtIndex(i).vector2Value = target.anchoredPosition;
                var frames = data.FindProperty("menuFrameTargets");
                for (int i = 0; i < frames.arraySize; i++)
                {
                    var frame = frames.GetArrayElementAtIndex(i);
                    if (frame.FindPropertyRelative("Target").objectReferenceValue != target) continue;
                    frame.FindPropertyRelative("Position").vector2Value = target.anchoredPosition;
                    frame.FindPropertyRelative("Size").vector2Value = target.sizeDelta;
                }
                data.ApplyModifiedPropertiesWithoutUndo();
            }
        }
    }
}
#endif
