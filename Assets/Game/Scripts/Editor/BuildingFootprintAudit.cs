using System;
using System.Text;
using Game.Authoring;
using Game.Composition;
using Game.Runtime;
using UnityEditor;
using UnityEngine;

namespace Game.Editor
{
    public static class BuildingFootprintAudit
    {
        public static void RepairAndValidate()
        {
            Repair();
            M03RadarWarningLocalizationBuilder.Import();
            Run();
        }

        private static void Repair()
        {
            foreach (var guid in AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/Game/Prefabs/Buildings" }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (path.Contains("/Destroyed/")) continue;
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab.GetComponent<BuildingDefinitionAuthoring>() == null || prefab.name == "Building_Road_Barrier" ||
                    !BuildingModelBounds.TryMeasure(prefab, out var bounds)) continue;
                var expected = BuildingModelBounds.EnclosingCells(bounds);
                if (prefab.GetComponent<BuildingDefinitionAuthoring>().ConfiguredFootprintCells == expected) continue;
                var contents = PrefabUtility.LoadPrefabContents(path);
                try
                {
                    var data = new SerializedObject(contents.GetComponent<BuildingDefinitionAuthoring>());
                    data.FindProperty("footprintCells").vector2IntValue = expected;
                    data.ApplyModifiedPropertiesWithoutUndo();
                    PrefabUtility.SaveAsPrefabAsset(contents, path);
                }
                finally { PrefabUtility.UnloadPrefabContents(contents); }
            }
            AssetDatabase.SaveAssets();
        }

        public static void Run()
        {
            var rows = new StringBuilder("Building\tConfigured\tRuntime\tModel bounds\n");
            var definitions = new BuildingDefinitionPrefabSystemHelper();
            definitions.ConfigureAuthoringMetadataResolvers(
                BuildingDefinitionAuthoringMetadataPrefabSystemHelper.TryGetBuildingDefinitionMetadata,
                BuildingDefinitionAuthoringMetadataPrefabSystemHelper.TryGetUnitDefinitionMetadata);
            int count = 0;
            foreach (var guid in AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/Game/Prefabs/Buildings" }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (path.Contains("/Destroyed/")) continue;
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                var authoring = prefab.GetComponent<BuildingDefinitionAuthoring>();
                if (authoring == null) continue;
                var definition = definitions.CreateRuntimeBuildingDefinition(prefab, prefab.name, "", Vector2Int.one, 1, new BuildingRunwaySystem());
                rows.AppendLine($"{prefab.name}\t{authoring.ConfiguredFootprintCells}\t{definition.FootprintCells}\t{definition.LocalBounds}");
                if (definition.HasLocalBounds && prefab.name != "Building_Road_Barrier")
                {
                    var expected = BuildingModelBounds.EnclosingCells(definition.LocalBounds);
                    if (definition.FootprintCells != expected || authoring.ConfiguredFootprintCells != expected)
                        throw new InvalidOperationException($"{prefab.name}: footprint must enclose the model with less than one cell of rounding.");
                }
                count++;
            }
            System.IO.File.WriteAllText("/private/tmp/warline-building-footprints.tsv", rows.ToString());
            Debug.Log($"[BuildingFootprintAudit] result=Passed count={count}\n{rows}");
        }
    }
}
