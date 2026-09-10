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
    public static class MatchHudResourceTextRepair
    {
        public static void Build()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<GameLocalizationCatalog>(V3UiLocalizationCatalogBuilder.CatalogPath);
            var locales = catalog.Locales.Select(locale => new GameLocaleTable(locale.LocaleCode, locale.DisplayName,
                locale.ShortLabel, locale.RightToLeft, locale.FontAsset,
                locale.Entries.Select(entry => new GameLocalizedStringRecord(entry.Key,
                    locale.LocaleCode == "fa-IR" && (entry.Key == "ui.hud.fuel" || entry.Value == "سوخت") ? "بنزین" :
                    locale.LocaleCode == "fa-IR" && entry.Key.StartsWith("ui.hud.", StringComparison.Ordinal) ? entry.Value.Replace("سوخت", "بنزین") : entry.Value)))).ToArray();
            catalog.Configure(catalog.SourceLocaleCode, locales);
            EditorUtility.SetDirty(catalog);
            var root = PrefabUtility.LoadPrefabContents(MatchHudV3PrefabBuilder.PrefabPath);
            try
            {
                var fuel = root.GetComponentsInChildren<Transform>(true).First(t => t.name == "FuelSlot");
                var view = fuel.GetComponent<MatchHudResourceIconView>() ?? fuel.gameObject.AddComponent<MatchHudResourceIconView>();
                var data = new SerializedObject(view);
                data.FindProperty("icon").objectReferenceValue = fuel.Find("Icon").GetComponent<Image>();
                data.FindProperty("fuel").objectReferenceValue = fuel.Find("Icon").GetComponent<Image>().sprite;
                data.FindProperty("credits").objectReferenceValue = AssetDatabase.LoadAssetAtPath<V3UiArtCatalog>(V3UiFoundationBuilder.CatalogPath).CreditsIcon;
                data.ApplyModifiedPropertiesWithoutUndo();
                // The compact mission action labels inherited a 16px line box. The Persian
                // font needs the full button height to avoid TMP ellipsis hiding the label.
                foreach (var t in root.GetComponentsInChildren<TMP_Text>(true))
                {
                    if(t.name=="Subtitle"&&t.transform.parent?.parent?.name=="SelectedSquadPanel")
                        (t.GetComponent<V3LocalizedTextBindingView>()??t.gameObject.AddComponent<V3LocalizedTextBindingView>()).Configure("", "", true);
                    if(t.name=="HealthText"&&t.transform.parent?.name=="HealthPanel")
                    {
                        var binding=t.GetComponent<V3LocalizedTextBindingView>()??t.gameObject.AddComponent<V3LocalizedTextBindingView>();
                        binding.Configure("", "", true);
                        t.rectTransform.anchoredPosition=new Vector2(205,0);t.rectTransform.sizeDelta=new Vector2(145,31);
                        t.enableAutoSizing=true;t.fontSizeMin=12;t.fontSizeMax=18;
                        var frame=(RectTransform)t.transform.parent.Find("HealthFrame");frame.sizeDelta=new Vector2(195,frame.sizeDelta.y);
                        var fill=(RectTransform)t.transform.parent.Find("HealthFill");fill.sizeDelta=new Vector2(187,fill.sizeDelta.y);
                    }
                    if (t.transform.parent == null || !new[] { "FieldGuide", "ReadWarning", "SkipLesson", "ReturnWarningCamera" }.Contains(t.transform.parent.name)) continue;
                    var r = t.rectTransform;
                    r.anchorMin = Vector2.zero; r.anchorMax = Vector2.one;
                    r.offsetMin = new Vector2(6, 2); r.offsetMax = new Vector2(-6, -2);
                    t.enableAutoSizing = true; t.fontSizeMin = 12; t.fontSizeMax = 18;
                }
                PrefabUtility.SaveAsPrefabAsset(root, MatchHudV3PrefabBuilder.PrefabPath);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
            AssetDatabase.SaveAssets();
        }
    }
}
