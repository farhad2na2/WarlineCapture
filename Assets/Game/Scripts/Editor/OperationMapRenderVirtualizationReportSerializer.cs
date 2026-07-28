using System;
using System.Collections.Generic;
using System.IO;
using Game.Configs;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Game.Editor
{
    internal sealed class OperationMapRenderVirtualizationReportDocument
    {
        internal int SchemaVersion { get; set; }
        internal string OperationMapId { get; set; }
        internal string ContentHash { get; set; }
        internal OperationMapRenderResidencyMode ResidencyMode { get; set; }
        internal int ResidentRenderRows { get; set; }
        internal int VirtualizedLogicalRows { get; set; }
        internal int PrototypeCount { get; set; }
        internal int PartCount { get; set; }
        internal int PlacementCount { get; set; }
        internal int CellCount { get; set; }
        internal int PolicyBucketCount { get; set; }
        internal int TotalSlotCount { get; set; }
        internal long PackedDatabaseBytes { get; set; }
        internal int SourceRowsRemoved { get; set; }
        internal int ExcludedRowCount { get; set; }
        internal int SourceHierarchyObjectCount { get; set; }
        internal OperationMapRenderCapacitySweepResult[] CapacityByPolicy { get; set; }
    }

    internal static class OperationMapRenderVirtualizationReportSerializer
    {
        internal const string ReportSchema =
            "warline.operation-map.render-virtualization";
        internal const int ReportSchemaVersion = 1;

        private static readonly string[] RootProperties =
        {
            "schema",
            "schemaVersion",
            "operationMapId",
            "contentHash",
            "residencyMode",
            "metrics",
            "capacityByPolicy"
        };

        private static readonly string[] MetricProperties =
        {
            "residentRenderRows",
            "virtualizedLogicalRows",
            "prototypeCount",
            "partCount",
            "placementCount",
            "cellCount",
            "policyBucketCount",
            "totalSlotCount",
            "packedDatabaseBytes",
            "sourceRowsRemoved",
            "excludedRowCount",
            "sourceHierarchyObjectCount"
        };

        private static readonly string[] CapacityProperties =
        {
            "bucket",
            "layer",
            "renderingLayerMask",
            "motionVectorMode",
            "shadowFlags",
            "sweepSampleCount",
            "peakRequiredPartRows",
            "capacity",
            "headroomCount"
        };

        internal static bool TrySerialize(
            OperationMapRenderVirtualizationReportDocument document,
            out string json,
            out string error)
        {
            json = null;
            if (!TryValidate(document, out error))
                return false;

            JObject root = new()
            {
                ["schema"] = ReportSchema,
                ["schemaVersion"] = document.SchemaVersion,
                ["operationMapId"] = document.OperationMapId,
                ["contentHash"] = document.ContentHash,
                ["residencyMode"] = (int)document.ResidencyMode,
                ["metrics"] = new JObject
                {
                    ["residentRenderRows"] = document.ResidentRenderRows,
                    ["virtualizedLogicalRows"] = document.VirtualizedLogicalRows,
                    ["prototypeCount"] = document.PrototypeCount,
                    ["partCount"] = document.PartCount,
                    ["placementCount"] = document.PlacementCount,
                    ["cellCount"] = document.CellCount,
                    ["policyBucketCount"] = document.PolicyBucketCount,
                    ["totalSlotCount"] = document.TotalSlotCount,
                    ["packedDatabaseBytes"] = document.PackedDatabaseBytes,
                    ["sourceRowsRemoved"] = document.SourceRowsRemoved,
                    ["excludedRowCount"] = document.ExcludedRowCount,
                    ["sourceHierarchyObjectCount"] = document.SourceHierarchyObjectCount
                }
            };

            JArray capacities = new();
            for (int index = 0; index < document.CapacityByPolicy.Length; index++)
            {
                OperationMapRenderCapacitySweepResult result =
                    document.CapacityByPolicy[index];
                capacities.Add(new JObject
                {
                    ["bucket"] = (int)result.Policy.Bucket,
                    ["layer"] = result.Policy.Layer,
                    ["renderingLayerMask"] = result.Policy.RenderingLayerMask,
                    ["motionVectorMode"] = (int)result.Policy.MotionVectorMode,
                    ["shadowFlags"] = (int)result.Policy.ShadowFlags,
                    ["sweepSampleCount"] = result.SweepSampleCount,
                    ["peakRequiredPartRows"] = result.PeakRequiredPartRows,
                    ["capacity"] = result.Capacity,
                    ["headroomCount"] = result.HeadroomCount
                });
            }

            root["capacityByPolicy"] = capacities;
            json = root.ToString(Formatting.Indented) + "\n";
            error = null;
            return true;
        }

        internal static bool TryDeserialize(
            string json,
            out OperationMapRenderVirtualizationReportDocument document,
            out string error)
        {
            document = null;
            if (string.IsNullOrWhiteSpace(json))
            {
                error = "Render-virtualization report JSON is required.";
                return false;
            }

            try
            {
                JObject root;
                using (StringReader stringReader = new(json))
                using (JsonTextReader jsonReader = new(stringReader))
                {
                    root = JObject.Load(
                        jsonReader,
                        new JsonLoadSettings
                        {
                            DuplicatePropertyNameHandling =
                                DuplicatePropertyNameHandling.Error
                        });
                }

                RequireExactProperties(root, RootProperties, "$");
                string schema = RequireString(root, "schema", "$");
                if (!string.Equals(schema, ReportSchema, StringComparison.Ordinal))
                    throw new InvalidDataException($"$.schema must equal '{ReportSchema}'.");

                JObject metrics = RequireObject(root, "metrics", "$");
                RequireExactProperties(metrics, MetricProperties, "$.metrics");
                JArray capacityArray = RequireArray(root, "capacityByPolicy", "$");
                OperationMapRenderCapacitySweepResult[] capacities =
                    new OperationMapRenderCapacitySweepResult[capacityArray.Count];
                for (int index = 0; index < capacityArray.Count; index++)
                {
                    string path = $"$.capacityByPolicy[{index}]";
                    if (capacityArray[index] is not JObject capacity)
                        throw new InvalidDataException($"{path} must be an object.");

                    RequireExactProperties(capacity, CapacityProperties, path);
                    OperationMapRenderPolicyKey policy = new(
                        (Game.Components.OperationMapRenderPolicyBucket)
                            RequireByte(capacity, "bucket", path),
                        RequireInt(capacity, "layer", path),
                        RequireUInt(capacity, "renderingLayerMask", path),
                        (OperationMapRenderMotionVectorMode)
                            RequireByte(capacity, "motionVectorMode", path),
                        (Game.Components.OperationMapRenderShadowFlags)
                            RequireByte(capacity, "shadowFlags", path));
                    capacities[index] = new OperationMapRenderCapacitySweepResult(
                        policy,
                        RequireInt(capacity, "sweepSampleCount", path),
                        RequireInt(capacity, "peakRequiredPartRows", path),
                        RequireInt(capacity, "capacity", path),
                        RequireInt(capacity, "headroomCount", path));
                }

                document = new OperationMapRenderVirtualizationReportDocument
                {
                    SchemaVersion = RequireInt(root, "schemaVersion", "$"),
                    OperationMapId = RequireString(root, "operationMapId", "$"),
                    ContentHash = RequireString(root, "contentHash", "$"),
                    ResidencyMode = (OperationMapRenderResidencyMode)
                        RequireByte(root, "residencyMode", "$"),
                    ResidentRenderRows =
                        RequireInt(metrics, "residentRenderRows", "$.metrics"),
                    VirtualizedLogicalRows =
                        RequireInt(metrics, "virtualizedLogicalRows", "$.metrics"),
                    PrototypeCount = RequireInt(metrics, "prototypeCount", "$.metrics"),
                    PartCount = RequireInt(metrics, "partCount", "$.metrics"),
                    PlacementCount = RequireInt(metrics, "placementCount", "$.metrics"),
                    CellCount = RequireInt(metrics, "cellCount", "$.metrics"),
                    PolicyBucketCount =
                        RequireInt(metrics, "policyBucketCount", "$.metrics"),
                    TotalSlotCount = RequireInt(metrics, "totalSlotCount", "$.metrics"),
                    PackedDatabaseBytes =
                        RequireLong(metrics, "packedDatabaseBytes", "$.metrics"),
                    SourceRowsRemoved =
                        RequireInt(metrics, "sourceRowsRemoved", "$.metrics"),
                    ExcludedRowCount =
                        RequireInt(metrics, "excludedRowCount", "$.metrics"),
                    SourceHierarchyObjectCount =
                        RequireInt(metrics, "sourceHierarchyObjectCount", "$.metrics"),
                    CapacityByPolicy = capacities
                };

                if (!TryValidate(document, out error))
                {
                    document = null;
                    return false;
                }

                error = null;
                return true;
            }
            catch (Exception exception) when (
                exception is JsonException ||
                exception is InvalidDataException ||
                exception is OverflowException)
            {
                document = null;
                error = exception.Message;
                return false;
            }
        }

        internal static bool TryValidate(
            OperationMapRenderVirtualizationReportDocument document,
            out string error)
        {
            if (document == null)
            {
                error = "Render-virtualization report document is required.";
                return false;
            }

            if (document.SchemaVersion != ReportSchemaVersion)
            {
                error =
                    $"Report schema version must be {ReportSchemaVersion}, " +
                    $"but was {document.SchemaVersion}.";
                return false;
            }

            if (string.IsNullOrWhiteSpace(document.OperationMapId) ||
                !string.Equals(
                    document.OperationMapId,
                    document.OperationMapId.Trim(),
                    StringComparison.Ordinal))
            {
                error = "Operation-map id must be nonempty and trimmed.";
                return false;
            }

            if (!IsLowerHexHash(document.ContentHash))
            {
                error = "Content hash must be exactly 64 lowercase hexadecimal characters.";
                return false;
            }

            if (document.ResidencyMode !=
                OperationMapRenderResidencyMode.VirtualizedProxyPool)
            {
                error = "Virtualization report requires VirtualizedProxyPool residency mode.";
                return false;
            }

            if (!RequireNonnegative(document.ResidentRenderRows, "residentRenderRows", out error) ||
                !RequirePositive(
                    document.VirtualizedLogicalRows,
                    "virtualizedLogicalRows",
                    out error) ||
                !RequirePositive(document.PrototypeCount, "prototypeCount", out error) ||
                !RequirePositive(document.PartCount, "partCount", out error) ||
                !RequirePositive(document.PlacementCount, "placementCount", out error) ||
                !RequirePositive(document.CellCount, "cellCount", out error) ||
                !RequirePositive(document.PolicyBucketCount, "policyBucketCount", out error) ||
                !RequirePositive(document.TotalSlotCount, "totalSlotCount", out error) ||
                !RequirePositive(document.PackedDatabaseBytes, "packedDatabaseBytes", out error) ||
                !RequireNonnegative(document.SourceRowsRemoved, "sourceRowsRemoved", out error) ||
                !RequireNonnegative(document.ExcludedRowCount, "excludedRowCount", out error) ||
                !RequireNonnegative(
                    document.SourceHierarchyObjectCount,
                    "sourceHierarchyObjectCount",
                    out error))
            {
                return false;
            }

            if (document.CapacityByPolicy == null ||
                document.CapacityByPolicy.Length != document.PolicyBucketCount)
            {
                error =
                    "capacityByPolicy length must exactly match policyBucketCount.";
                return false;
            }

            long totalSlots = 0;
            int expectedSweepSamples = -1;
            HashSet<OperationMapRenderPolicyKey> uniquePolicies = new();
            OperationMapRenderPolicyKey previousPolicy = default;
            for (int index = 0; index < document.CapacityByPolicy.Length; index++)
            {
                OperationMapRenderCapacitySweepResult result =
                    document.CapacityByPolicy[index];
                if (!OperationMapRenderPolicyClassifier.TryValidate(
                        result.Policy,
                        out string policyError))
                {
                    error = $"capacityByPolicy[{index}] has invalid policy: {policyError}";
                    return false;
                }

                if (!uniquePolicies.Add(result.Policy))
                {
                    error = $"capacityByPolicy[{index}] repeats a policy key.";
                    return false;
                }

                if (index > 0 &&
                    OperationMapRenderCapacitySweep.ComparePolicies(
                        previousPolicy,
                        result.Policy) >= 0)
                {
                    error = "capacityByPolicy must be strictly sorted by complete policy key.";
                    return false;
                }

                if (result.SweepSampleCount <= 0)
                {
                    error = $"capacityByPolicy[{index}].sweepSampleCount must be positive.";
                    return false;
                }

                if (expectedSweepSamples < 0)
                    expectedSweepSamples = result.SweepSampleCount;
                else if (result.SweepSampleCount != expectedSweepSamples)
                {
                    error = "Every capacity policy must report the same sweep sample count.";
                    return false;
                }

                if (result.PeakRequiredPartRows <= 0)
                {
                    error =
                        $"capacityByPolicy[{index}].peakRequiredPartRows must be positive.";
                    return false;
                }

                if (!OperationMapRenderCapacitySweep.TryCalculateCapacity(
                        result.PeakRequiredPartRows,
                        out int expectedCapacity,
                        out int expectedHeadroom) ||
                    result.Capacity != expectedCapacity ||
                    result.HeadroomCount != expectedHeadroom)
                {
                    error =
                        $"capacityByPolicy[{index}] does not match exact 20% headroom.";
                    return false;
                }

                totalSlots += result.Capacity;
                if (totalSlots > int.MaxValue)
                {
                    error = "capacityByPolicy total slots exceed Int32.";
                    return false;
                }

                previousPolicy = result.Policy;
            }

            if (totalSlots != document.TotalSlotCount)
            {
                error =
                    $"totalSlotCount {document.TotalSlotCount} does not match " +
                    $"capacity sum {totalSlots}.";
                return false;
            }

            error = null;
            return true;
        }

        private static void RequireExactProperties(
            JObject value,
            IReadOnlyList<string> expected,
            string path)
        {
            HashSet<string> remaining = new(expected, StringComparer.Ordinal);
            foreach (JProperty property in value.Properties())
            {
                if (!remaining.Remove(property.Name))
                    throw new InvalidDataException($"{path} has unknown property '{property.Name}'.");
            }

            foreach (string missing in remaining)
                throw new InvalidDataException($"{path} is missing required property '{missing}'.");
        }

        private static JObject RequireObject(JObject parent, string name, string path)
        {
            if (parent[name] is not JObject value)
                throw new InvalidDataException($"{path}.{name} must be an object.");
            return value;
        }

        private static JArray RequireArray(JObject parent, string name, string path)
        {
            if (parent[name] is not JArray value)
                throw new InvalidDataException($"{path}.{name} must be an array.");
            return value;
        }

        private static string RequireString(JObject parent, string name, string path)
        {
            JToken token = parent[name];
            if (token == null || token.Type != JTokenType.String)
                throw new InvalidDataException($"{path}.{name} must be a string.");
            return token.Value<string>();
        }

        private static int RequireInt(JObject parent, string name, string path)
        {
            long value = RequireLong(parent, name, path);
            if (value < int.MinValue || value > int.MaxValue)
                throw new InvalidDataException($"{path}.{name} must fit Int32.");
            return (int)value;
        }

        private static byte RequireByte(JObject parent, string name, string path)
        {
            long value = RequireLong(parent, name, path);
            if (value < byte.MinValue || value > byte.MaxValue)
                throw new InvalidDataException($"{path}.{name} must fit byte.");
            return (byte)value;
        }

        private static uint RequireUInt(JObject parent, string name, string path)
        {
            long value = RequireLong(parent, name, path);
            if (value < uint.MinValue || value > uint.MaxValue)
                throw new InvalidDataException($"{path}.{name} must fit UInt32.");
            return (uint)value;
        }

        private static long RequireLong(JObject parent, string name, string path)
        {
            JToken token = parent[name];
            if (token == null || token.Type != JTokenType.Integer)
                throw new InvalidDataException($"{path}.{name} must be an integer.");
            return token.Value<long>();
        }

        private static bool RequirePositive(long value, string name, out string error)
        {
            if (value <= 0)
            {
                error = $"{name} must be positive, but was {value}.";
                return false;
            }

            error = null;
            return true;
        }

        private static bool RequireNonnegative(int value, string name, out string error)
        {
            if (value < 0)
            {
                error = $"{name} must be nonnegative, but was {value}.";
                return false;
            }

            error = null;
            return true;
        }

        private static bool IsLowerHexHash(string value)
        {
            if (string.IsNullOrEmpty(value) || value.Length != 64)
                return false;

            for (int index = 0; index < value.Length; index++)
            {
                char character = value[index];
                if ((character < '0' || character > '9') &&
                    (character < 'a' || character > 'f'))
                {
                    return false;
                }
            }

            return true;
        }
    }
}
