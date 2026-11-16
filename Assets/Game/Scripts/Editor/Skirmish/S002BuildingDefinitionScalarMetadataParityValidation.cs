using System;
using System.Linq;
using System.Reflection;
using Game.Authoring;
using Game.Components;
using Game.Configs;
using UnityEditor;
using UnityEngine;

namespace Game.Editor.Skirmish
{
    /// <summary>
    /// Focused parity control for the shared managed building metadata scalar policy.
    /// Uses transient objects only; it creates no scene, ECS World, or baked entity.
    /// </summary>
    public static class S002BuildingDefinitionScalarMetadataParityValidation
    {
        public static void RunFocusedValidation()
        {
            ValidateCase("no-config-negative-fields", false, -3, new Vector2Int(-5, 0), -9, -10,
                float.NegativeInfinity, 0, 0, 0, 0f);
            ValidateCase("no-config-duration-floor", false, 4, new Vector2Int(2, 3), 5, 6,
                0.001f, 0, 0, 0, 30f);
            ValidateCase("configured-values-win", true, 11, new Vector2Int(28, 15), 12, 13, 17f,
                1200, 40000, 90, 30f);
            ValidateCase("configured-negative-material-and-nan-duration", true, 11, new Vector2Int(3, 4),
                12, 13, 17f, 700, 22000, -5, float.NaN);
            ValidateCase("configured-positive-infinity-duration", true, 11, new Vector2Int(5, 6),
                12, 13, 17f, 701, 22001, 7, float.PositiveInfinity);
            ValidateCase("configured-negative-infinity-duration", true, 11, new Vector2Int(7, 8),
                12, 13, 17f, 702, 22002, 8, float.NegativeInfinity);
            ValidateCase("no-config-nan-duration", false, -4, new Vector2Int(9, 10), -3, -2,
                float.NaN, 901, 902, 903, 19f);

            Debug.Log("[S002BuildingDefinitionScalarMetadataParity] result=Passed cases=7 scope=TransientManagedMetadataOnly noScene=true noWorld=true noBaker=true sourceFitVerified=false missionReady=false configPriceNonpositiveNotTested=true");
        }

        private static void ValidateCase(string name, bool hasConfig, int prefabHealth, Vector2Int footprint,
            int prefabPrice, int prefabMaterials, float prefabDuration, int configHealth, int configPrice,
            int configMaterials, float configDuration)
        {
            GameObject gameObject = null;
            BuildingDefinitionAuthoringConfig config = null;
            try
            {
                gameObject = new GameObject("S002ScalarMetadata_" + name);
                var authoring = gameObject.AddComponent<BuildingDefinitionAuthoring>();
                SetAuthoring(authoring, prefabHealth, footprint, prefabPrice, prefabMaterials, prefabDuration);
                if (hasConfig)
                {
                    config = ScriptableObject.CreateInstance<BuildingDefinitionAuthoringPrefabConfigAsset>();
                    SetConfig(config, configHealth, configPrice, configMaterials, configDuration);
                    SetObjectReference(authoring, "config", config);
                }

                BuildingDefinitionAuthoringScalarSource source = authoring.CaptureScalarMetadataSource();
                BuildingDefinitionAuthoringScalarMetadata projected = BuildingDefinitionAuthoringScalarProjection.Project(source);
                object metadata = InvokeOriginalMetadataOwner(gameObject);
                Vector2Int actualFootprint = (Vector2Int)Field(metadata, "FootprintCells");

                Require((int)Field(metadata, "MaxHealth") == projected.MaxHealth &&
                        actualFootprint.x == projected.FootprintWidth && actualFootprint.y == projected.FootprintDepth &&
                        (int)Field(metadata, "Price") == projected.Price &&
                        (int)Field(metadata, "MaterialsCost") == projected.MaterialsCost &&
                        SameFloat((float)Field(metadata, "ProductionDurationSeconds"), projected.ProductionDurationSeconds),
                    "Original metadata owner differs from shared scalar projection: " + name);

                // The helper has applied the current config. These are the original
                // getters whose behavior this projection must preserve.
                Require((int)Field(metadata, "MaxHealth") == authoring.ConfiguredMaxHealth &&
                        actualFootprint == authoring.ConfiguredFootprintCells &&
                        (int)Field(metadata, "Price") == authoring.ConfiguredPrice &&
                        (int)Field(metadata, "MaterialsCost") == authoring.ConfiguredMaterialsCost &&
                        SameFloat((float)Field(metadata, "ProductionDurationSeconds"),
                            authoring.ConfiguredProductionDurationSeconds),
                    "Original configured getters differ from metadata result: " + name);
            }
            finally
            {
                if (gameObject != null) UnityEngine.Object.DestroyImmediate(gameObject);
                if (config != null) UnityEngine.Object.DestroyImmediate(config);
            }
        }

        private static object InvokeOriginalMetadataOwner(GameObject prefab)
        {
            Assembly assembly = AppDomain.CurrentDomain.GetAssemblies()
                .SingleOrDefault(item => item.GetName().Name == "Game.Composition");
            Require(assembly != null, "Original Game.Composition assembly must be loaded");
            Type owner = assembly.GetType("Game.Composition.BuildingDefinitionAuthoringMetadataPrefabSystemHelper");
            Require(owner != null, "Original building metadata owner type must be loaded");
            MethodInfo method = owner.GetMethod("TryGetBuildingDefinitionMetadata",
                BindingFlags.Public | BindingFlags.Static);
            Require(method != null, "Original TryGetBuildingDefinitionMetadata method must be loaded");
            object[] args = { prefab, null };
            Require((bool)method.Invoke(null, args), "Original metadata owner rejected transient authoring: " + prefab.name);
            Require(args[1] != null, "Original metadata owner must return its scalar metadata struct");
            return args[1];
        }

        private static object Field(object value, string name)
        {
            FieldInfo field = value.GetType().GetField(name, BindingFlags.Instance | BindingFlags.Public);
            Require(field != null, "Original metadata field missing: " + name);
            return field.GetValue(value);
        }

        private static void SetAuthoring(BuildingDefinitionAuthoring authoring, int maxHealth,
            Vector2Int footprint, int price, int materialsCost, float duration)
        {
            var serialized = new SerializedObject(authoring);
            serialized.FindProperty("maxHealth").intValue = maxHealth;
            serialized.FindProperty("footprintCells").vector2IntValue = footprint;
            serialized.FindProperty("price").intValue = price;
            serialized.FindProperty("materialsCost").intValue = materialsCost;
            serialized.FindProperty("productionDurationSeconds").floatValue = duration;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetConfig(BuildingDefinitionAuthoringConfig config, int maxHealth,
            int price, int materialsCost, float duration)
        {
            var serialized = new SerializedObject(config);
            serialized.FindProperty("maxHealth").intValue = maxHealth;
            serialized.FindProperty("price").intValue = price;
            serialized.FindProperty("materialsCost").intValue = materialsCost;
            serialized.FindProperty("productionDurationSeconds").floatValue = duration;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetObjectReference(BuildingDefinitionAuthoring authoring, string propertyName,
            UnityEngine.Object value)
        {
            var serialized = new SerializedObject(authoring);
            serialized.FindProperty(propertyName).objectReferenceValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static bool SameFloat(float left, float right)
        {
            if (float.IsNaN(left) || float.IsNaN(right)) return float.IsNaN(left) && float.IsNaN(right);
            if (float.IsInfinity(left) || float.IsInfinity(right)) return left.Equals(right);
            return left.Equals(right);
        }

        private static void Require(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
        }
    }
}
