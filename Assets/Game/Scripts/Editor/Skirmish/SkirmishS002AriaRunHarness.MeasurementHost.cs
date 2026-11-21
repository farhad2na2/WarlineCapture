#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using Game.Configs;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Game.Editor
{
    public static partial class SkirmishS002AriaRunHarness
    {
        private const string MeasurementCatalogVariable = "WARLINE_SKIRMISH_CATALOG";
        private const string StagedArmorBreakHostPath =
            "Assets/Game/Configs/SkirmishExpansion/StagedS049/SkirmishScenario_S049.asset";
        private static OperationMapDefinition measurementSource;
        private static SkirmishExpansionCatalogConfig measurementCatalog;
        private static IDisposable measurementCatalogScope;
        private static bool measurementHostSelected;

        private static JObject ReadMeasurementDraft(string path, out string fullPath)
        {
            string project = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            fullPath = Path.GetFullPath(Path.IsPathRooted(path) ? path : Path.Combine(project, path));
            return JObject.Parse(File.ReadAllText(fullPath));
        }

        private static void RunMeasurementHost(JObject draft, Action launch)
        {
            string catalog = Environment.GetEnvironmentVariable(MeasurementCatalogVariable);
            if (draft == null)
            {
                if (string.IsNullOrEmpty(catalog))
                    throw new InvalidOperationException("Set WARLINE_SKIRMISH_CATALOG or WARLINE_MISSION_DRAFT for a measurement probe.");
                launch();
                return;
            }
            if (EditorApplication.isPlayingOrWillChangePlaymode || measurementSource != null)
                throw new InvalidOperationException("A draft source probe requires idle Edit Mode.");
            string sourcePath = (string)draft["campaign_source"];
            var source = string.IsNullOrEmpty(sourcePath) ? null : AssetDatabase.LoadAssetAtPath<OperationMapDefinition>(sourcePath);
            if (source == null)
                throw new InvalidOperationException("Invalid draft campaign_source: " + sourcePath);

            measurementSource = source;
            try
            {
                if (string.IsNullOrEmpty(catalog))
                {
                    catalog = SelectMeasurementHost(source);
                    Environment.SetEnvironmentVariable(MeasurementCatalogVariable, catalog);
                    measurementHostSelected = true;
                }
                EditorApplication.playModeStateChanged += ReleaseMeasurementHostAfterPlay;
                Debug.Log("[SkirmishMeasurementHost] catalog=" + catalog + " source=" +
                    source.OperationMapId + " staged=" + (measurementCatalog != null ? 1 : 0));
                launch();
            }
            catch
            {
                ReleaseMeasurementHost();
                throw;
            }
        }

        private static string SelectMeasurementHost(OperationMapDefinition source)
        {
            var shipped = SkirmishExpansionCatalogConfig.Load();
            if (shipped == null)
                throw new InvalidOperationException("Packaged Skirmish expansion catalog missing.");
            var registered = shipped.Definitions
                .Where(d => d != null && d.CampaignMap == source && IsGenericAriaCatalog(d.CatalogId) &&
                    !SkirmishMissionDispatch.TryGetLegacyIndex(d.CatalogId, out _))
                .OrderBy(d => d.CatalogId, StringComparer.Ordinal)
                .FirstOrDefault();
            if (registered != null) return registered.CatalogId;

            var staged = AssetDatabase.LoadAssetAtPath<SkirmishScenarioDefinitionConfig>(StagedArmorBreakHostPath);
            if (staged == null || staged.CampaignMap != source)
                throw new InvalidOperationException("No measurement host for draft campaign_source " + source.OperationMapId);

            measurementCatalog = Object.Instantiate(shipped);
            measurementCatalog.hideFlags = HideFlags.HideAndDontSave;
            using var fields = new SerializedObject(measurementCatalog);
            var definitions = fields.FindProperty("definitions");
            int index = Array.FindIndex(shipped.Definitions.ToArray(), d => d != null && d.CatalogId == staged.CatalogId);
            if (index < 0)
            {
                index = definitions.arraySize;
                definitions.arraySize = index + 1;
            }
            definitions.GetArrayElementAtIndex(index).objectReferenceValue = staged;
            fields.ApplyModifiedPropertiesWithoutUndo();
            measurementCatalogScope = SkirmishExpansionCatalogConfig.UseEditorValidationCatalog(measurementCatalog);
            return staged.CatalogId;
        }

        private static void RequireMeasurementSource(string actual)
        {
            if (measurementSource != null && measurementSource.OperationMapId != actual)
                throw new InvalidOperationException("Loaded map " + actual +
                    " differs from the draft's campaign_source " + measurementSource.OperationMapId);
        }

        private static void ReleaseMeasurementHostAfterPlay(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.EnteredEditMode) ReleaseMeasurementHost();
        }

        private static void ReleaseMeasurementHost()
        {
            EditorApplication.playModeStateChanged -= ReleaseMeasurementHostAfterPlay;
            if (measurementHostSelected)
            {
                DisposeHarnessGenericLibrary();
                Environment.SetEnvironmentVariable(MeasurementCatalogVariable, null);
                measurementHostSelected = false;
            }
            measurementCatalogScope?.Dispose();
            measurementCatalogScope = null;
            if (measurementCatalog != null) Object.DestroyImmediate(measurementCatalog);
            measurementCatalog = null;
            measurementSource = null;
        }
    }
}
#endif
