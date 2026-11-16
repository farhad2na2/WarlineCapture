using System;
using System.Linq;
using System.Reflection;
using Game.Authoring;
using Game.Components;
using Game.Configs;
using UnityEngine;

namespace Game.Editor.Skirmish
{
    /// <summary>Transient parity control for the shared unit production metadata scalar policy.</summary>
    public static class S002UnitProductionMetadataScalarParityValidation
    {
        public static void RunFocusedValidation()
        {
            ValidateCase("no-config-floors-null-transport", false,
                0.001f, float.NegativeInfinity, float.PositiveInfinity, 0, new Vector2Int(0, -2),
                true, true, false, null,
                false, false, false, 60f, 5f, 4f, 1);

            var transport = new GameObject("S002UnitMetadataTransportIdentity");
            try
            {
                ValidateCase("config-bool-precedence-nan-values", true,
                    float.NaN, float.PositiveInfinity, float.NegativeInfinity, -2, new Vector2Int(3, 4),
                    false, false, true, transport,
                    true, true, false, 123f, 14f, 12f, 7);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(transport);
            }

            UnityEngine.Debug.Log("[S002UnitProductionMetadataScalarParity] result=Passed cases=2 scope=TransientManagedMetadataOnly noApplyConfig=true noScene=true noWorld=true noBaker=true nullableTransportIdentityPreserved=true sourceFitVerified=false missionReady=false");
        }

        private static void ValidateCase(string name, bool hasConfig,
            float duration, float arrival, float hold, int maxConcurrent, Vector2Int footprint,
            bool prefabAir, bool prefabRequiresRunway, bool prefabUsesLanding,
            GameObject transport, bool configAir, bool configRequiresRunway, bool configUsesLanding,
            float configDuration, float configArrival, float configHold, int configConcurrent)
        {
            GameObject root = null;
            UnitGridAuthoringConfig config = null;
            try
            {
                root = new GameObject("S002UnitMetadata_" + name);
                UnitGridAuthoring authoring = root.AddComponent<UnitGridAuthoring>();
                if (hasConfig)
                {
                    config = ScriptableObject.CreateInstance<UnitGridAuthoringConfig>();
                    SetField(config, "isAirUnit", configAir);
                    SetField(config, "productionTransportRequiresAirportRunway", configRequiresRunway);
                    SetField(config, "productionTransportUsesRunwayLanding", configUsesLanding);
                    SetField(config, "productionDurationSeconds", configDuration);
                    SetField(config, "productionTransportArrivalSeconds", configArrival);
                    SetField(config, "productionTransportHoldForNextReadySeconds", configHold);
                    SetField(config, "productionTransportMaxConcurrent", configConcurrent);
                    SetField(authoring, "config", config);
                }

                // Set the component fields after assigning config so this control can
                // prove the original owner does not call ApplyConfigIfAvailable.
                SetField(authoring, "productionDurationSeconds", duration);
                SetField(authoring, "productionTransportArrivalSeconds", arrival);
                SetField(authoring, "productionTransportHoldForNextReadySeconds", hold);
                SetField(authoring, "productionTransportMaxConcurrent", maxConcurrent);
                SetField(authoring, "footprintCells", footprint);
                SetField(authoring, "isAirUnit", prefabAir);
                SetField(authoring, "productionTransportRequiresAirportRunway", prefabRequiresRunway);
                SetField(authoring, "productionTransportUsesRunwayLanding", prefabUsesLanding);
                SetField(authoring, "productionTransportPrefab", transport);

                UnitProductionMetadataScalarValues projected =
                    UnitProductionMetadataScalarPolicy.Project(authoring.CaptureProductionMetadataScalarSource());
                object metadata = InvokeOriginalMetadataOwner(root);
                Require((bool)Field(metadata, "IsAirUnit") == projected.IsAirUnit &&
                        (bool)Field(metadata, "ProductionTransportRequiresAirportRunway") == projected.ProductionTransportRequiresAirportRunway &&
                        (bool)Field(metadata, "ProductionTransportUsesRunwayLanding") == projected.ProductionTransportUsesRunwayLanding &&
                        SameFloat((float)Field(metadata, "ProductionDurationSeconds"), projected.ProductionDurationSeconds) &&
                        SameFloat((float)Field(metadata, "ProductionTransportArrivalSeconds"), projected.ProductionTransportArrivalSeconds) &&
                        SameFloat((float)Field(metadata, "ProductionTransportHoldForNextReadySeconds"), projected.ProductionTransportHoldForNextReadySeconds) &&
                        (int)Field(metadata, "ProductionTransportMaxConcurrent") == projected.ProductionTransportMaxConcurrent,
                    "Original unit metadata owner differs from shared scalar projection: " + name);

                Vector2Int actualFootprint = (Vector2Int)Field(metadata, "FootprintCells");
                Require(actualFootprint.x == projected.FootprintWidth && actualFootprint.y == projected.FootprintDepth,
                    "Original unit footprint differs from unmodified source footprint: " + name);
                Require(ReferenceEquals(Field(metadata, "ProductionTransportPrefab"), transport),
                    "Original owner must preserve the exact nullable transport GameObject reference: " + name);
                Require((bool)Field(metadata, "IsAirUnit") == authoring.IsAirUnit &&
                        (bool)Field(metadata, "ProductionTransportRequiresAirportRunway") == authoring.ProductionTransportRequiresAirportRunway &&
                        (bool)Field(metadata, "ProductionTransportUsesRunwayLanding") == authoring.ProductionTransportUsesRunwayLanding &&
                        SameFloat((float)Field(metadata, "ProductionDurationSeconds"), authoring.ProductionDurationSeconds) &&
                        SameFloat((float)Field(metadata, "ProductionTransportArrivalSeconds"), authoring.ProductionTransportArrivalSeconds) &&
                        SameFloat((float)Field(metadata, "ProductionTransportHoldForNextReadySeconds"), authoring.ProductionTransportHoldForNextReadySeconds) &&
                        (int)Field(metadata, "ProductionTransportMaxConcurrent") == authoring.ProductionTransportMaxConcurrent,
                    "Original metadata getters changed during policy projection: " + name);
                Require(actualFootprint == authoring.GetConfiguredFootprintCells(),
                    "Original owner footprint getter changed during policy projection: " + name);
            }
            finally
            {
                if (root != null) UnityEngine.Object.DestroyImmediate(root);
                if (config != null) UnityEngine.Object.DestroyImmediate(config);
            }
        }

        private static object InvokeOriginalMetadataOwner(GameObject prefab)
        {
            Assembly assembly = AppDomain.CurrentDomain.GetAssemblies()
                .SingleOrDefault(item => item.GetName().Name == "Game.Composition");
            Require(assembly != null, "Original Game.Composition assembly must be loaded");
            Type owner = assembly.GetType("Game.Composition.BuildingProductionUnitMetadataPrefabSystemHelper");
            Require(owner != null, "Original unit production metadata owner must be loaded");
            MethodInfo method = owner.GetMethod("TryGetMetadata", BindingFlags.Public | BindingFlags.Static);
            Require(method != null, "Original TryGetMetadata method must be loaded");
            object[] args = { prefab, null };
            Require((bool)method.Invoke(null, args), "Original metadata owner rejected transient UnitGridAuthoring");
            Require(args[1] != null, "Original metadata owner must return its metadata struct");
            return args[1];
        }

        private static object Field(object value, string name)
        {
            FieldInfo field = value.GetType().GetField(name, BindingFlags.Instance | BindingFlags.Public);
            Require(field != null, "Original unit metadata field missing: " + name);
            return field.GetValue(value);
        }

        private static void SetField(object value, string name, object fieldValue)
        {
            FieldInfo field = value.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
            Require(field != null, "Serialized unit authoring field missing: " + name);
            field.SetValue(value, fieldValue);
        }

        private static bool SameFloat(float left, float right)
        {
            return BitConverter.ToInt32(BitConverter.GetBytes(left), 0) ==
                   BitConverter.ToInt32(BitConverter.GetBytes(right), 0);
        }

        private static void Require(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
        }
    }
}
