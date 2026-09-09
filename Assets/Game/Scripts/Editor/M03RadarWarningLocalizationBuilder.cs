using System;
using System.Collections.Generic;
using System.Linq;
using Game.Configs;
using UnityEditor;
using UnityEngine;

namespace Game.Editor
{
    public static class M03RadarWarningLocalizationBuilder
    {
        public static void Import()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<GameLocalizationCatalog>(V3UiLocalizationCatalogBuilder.CatalogPath);
            if (catalog == null) throw new InvalidOperationException("The central localization catalog is missing.");
            var tables = new List<GameLocaleTable>();
            foreach (GameLocaleTable locale in catalog.Locales)
            {
                if (locale.LocaleCode != "en" && locale.LocaleCode != "fa-IR") { tables.Add(locale); continue; }
                bool persian = locale.LocaleCode == "fa-IR";
                var entries = locale.Entries.ToDictionary(item => item.Key, item => item.Value, StringComparer.Ordinal);
                foreach (var item in M03RadarWarningUiCopyCatalog.Entries) entries[item.Key] = persian ? item.Persian : item.English;
                foreach (var item in M03RadarWarningGuideCopyCatalog.Entries) entries[item.Key] = persian ? item.Persian : item.English;
                for(int i=0;i<M03RadarWarningTutorialCopyCatalog.Steps.Length;i++)
                {
                    var step=M03RadarWarningTutorialCopyCatalog.Steps[i];
                    entries["mission.m03.tutorial."+(i+1)+".title"]=persian ? step.PersianTitle : step.Title;
                    entries["mission.m03.tutorial."+(i+1)+".body"]=persian ? step.PersianBody : step.Body;
                }
                foreach (M03NarrativeLine line in M03RadarWarningCopyCatalog.Brief.Concat(M03RadarWarningCopyCatalog.Comms).Concat(M03RadarWarningCopyCatalog.Debrief).Concat(M03RadarWarningCopyCatalog.DebriefOutcomes))
                    entries[line.Key] = persian ? line.Persian : line.English;
                tables.Add(new GameLocaleTable(locale.LocaleCode, locale.DisplayName, locale.ShortLabel, locale.RightToLeft,
                    locale.FontAsset, entries.OrderBy(item => item.Key, StringComparer.Ordinal)
                        .Select(item => new GameLocalizedStringRecord(item.Key, item.Value))));
            }
            catalog.Configure(catalog.SourceLocaleCode, tables);
            EditorUtility.SetDirty(catalog);
            AssetDatabase.SaveAssets();
            Debug.Log("[M03RadarWarningLocalizationBuilder] result=Passed central tables updated; unrelated keys and prefab bindings preserved");
        }
    }
}
