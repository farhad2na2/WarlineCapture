#if UNITY_EDITOR

namespace Game.Editor
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using Newtonsoft.Json.Linq;
    using UnityEditor;
    using UnityEngine;

    public static class PerformanceProductBudgetValidator
    {
        public const string ConfigPath =
            "Design/Architecture/performance_regression_accepted_baseline.json";

        private const int ExpectedSchemaVersion = 2;
        private const double MaximumEditorP95Ms = 50d;
        private const double MaximumBaselineAndroidP95Ms = 33d;
        private const double MaximumRecommendedAndroidP95Ms = 33d;
        private const double MaximumHighEndAndroidP95Ms = 25d;
        private const long ExpectedGcBaselineBytes = 269482;
        private const long MaximumGcAcceptanceBudgetBytes = 1024;
        private const int ExpectedPeakMemoryMinimumMB = 1054;
        private const int ExpectedPeakMemoryMaximumMB = 1075;
        private const double MinimumPeakMemoryReductionPercent = 10d;

        private static readonly string[] FrameEvidence =
        {
            "exactCommit", "artifactSha256", "deviceProfile", "buildType", "qualityTier",
            "frameRateMode", "scenario", "warmupDuration", "sampleDuration", "averageFrameMs",
            "p95FrameMs", "p99FrameMs", "maximumFrameMs", "cpuTiming", "gpuTiming", "thermalState"
        };

        private static readonly string[] MemoryEvidence =
        {
            "exactCommit", "artifactSha256", "deviceProfile", "buildType", "scenario",
            "warmupDuration", "sampleDuration", "peakAllocatedMemoryMB", "monoMemoryMB",
            "sameDeviceComparison", "runtimeResidencyNotes"
        };

        private static readonly string[] PackageEvidence =
        {
            "exactCommit", "artifactSha256", "releaseBuildType", "buildTarget", "scriptingBackend",
            "targetArchitecture", "artifactBytes", "buildReportIncludedAssets"
        };

        private static readonly string[] InstalledSizeEvidence =
        {
            "exactCommit", "artifactSha256", "deviceProfile", "cleanInstallMethod", "installedBytes",
            "packageManagerEvidence"
        };

        private static readonly string[] StartupEvidence =
        {
            "exactCommit", "artifactSha256", "deviceProfile", "launchDefinition", "coldStartSamples",
            "warmStartSamples", "p50Ms", "p95Ms", "maximumMs"
        };

        private static readonly string[] VisualEvidence =
        {
            "exactCommit", "artifactSha256", "deviceProfile", "qualityTier", "frameRateMode",
            "sameCameraBeforeAfter", "gameplayZoomCapture", "maxZoomOutCapture", "nightCapture",
            "16:9Capture", "20:9Capture", "reviewerDecision"
        };

        [MenuItem("Game/Tools/Performance/Validate Product Budgets")]
        public static void Run()
        {
            try
            {
                ValidateFile();
                Debug.Log($"[PerformanceProductBudgetValidation] result=Passed config={ConfigPath}");
                ExitBatchMode(0);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                Debug.LogError($"[PerformanceProductBudgetValidation] result=Failed config={ConfigPath}");
                ExitBatchMode(1);
                throw;
            }
        }

        public static void ValidateFile()
        {
            if (!File.Exists(ConfigPath))
                throw new FileNotFoundException("Performance product budget config is missing.", ConfigPath);

            ValidateJson(File.ReadAllText(ConfigPath));
        }

        public static void ValidateJson(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
                throw new InvalidDataException("Performance product budget config is empty.");

            JObject root;
            try
            {
                root = JObject.Parse(json);
            }
            catch (Exception exception)
            {
                throw new InvalidDataException("Performance product budget config is not valid JSON.", exception);
            }

            var errors = new List<string>();
            ValidateRoot(root, errors);
            if (errors.Count > 0)
                throw new InvalidDataException(string.Join(Environment.NewLine, errors));
        }

        private static void ValidateRoot(JObject root, List<string> errors)
        {
            ValidateSchema(root, "$", errors,
                "acceptedBaselineVersion", "acceptedAtUtc", "source", "editorP95FrameBudgetMs",
                "currentThreadAllocatedBytesBudget", "minimumFrameCount", "minimumUnitCount",
                "minimumRuntimeBuildingCount", "minimumVisibleModelEstimate", "productBudgets");

            RequireInteger(root, "acceptedBaselineVersion", "$", errors, ExpectedSchemaVersion, ExpectedSchemaVersion);
            RequireNonEmptyString(root, "acceptedAtUtc", "$", errors);
            RequireNonEmptyString(root, "source", "$", errors);
            RequireNumber(root, "editorP95FrameBudgetMs", "$", errors, double.Epsilon, MaximumEditorP95Ms);
            RequireInteger(root, "currentThreadAllocatedBytesBudget", "$", errors, 0, 0);
            RequireInteger(root, "minimumFrameCount", "$", errors, 180, int.MaxValue);
            RequireInteger(root, "minimumUnitCount", "$", errors, 700, int.MaxValue);
            RequireInteger(root, "minimumRuntimeBuildingCount", "$", errors, 600, int.MaxValue);
            RequireInteger(root, "minimumVisibleModelEstimate", "$", errors, 40, int.MaxValue);

            JObject productBudgets = RequireObject(root, "productBudgets", "$", errors);
            if (productBudgets != null)
                ValidateProductBudgets(productBudgets, errors);
        }

        private static void ValidateProductBudgets(JObject productBudgets, List<string> errors)
        {
            const string path = "$.productBudgets";
            ValidateSchema(productBudgets, path, errors,
                "taskId", "baselineCommit", "status", "androidFrameP95AfterWarmup",
                "matchSteadyStateGc", "peakAllocatedMemory", "releaseEvidence");
            RequireString(productBudgets, "taskId", path, errors, "APH-009");
            RequireString(productBudgets, "baselineCommit", path, errors, "ba3da6704");
            RequireString(productBudgets, "status", path, errors, "frozen-initial");

            JObject frames = RequireObject(productBudgets, "androidFrameP95AfterWarmup", path, errors);
            JObject gc = RequireObject(productBudgets, "matchSteadyStateGc", path, errors);
            JObject memory = RequireObject(productBudgets, "peakAllocatedMemory", path, errors);
            JObject releaseEvidence = RequireObject(productBudgets, "releaseEvidence", path, errors);

            if (frames != null)
                ValidateFrameBudgets(frames, errors);
            if (gc != null)
                ValidateGcBudget(gc, errors);
            if (memory != null)
                ValidateMemoryBudget(memory, errors);
            if (releaseEvidence != null)
                ValidateReleaseEvidence(releaseEvidence, errors);
        }

        private static void ValidateFrameBudgets(JObject frames, List<string> errors)
        {
            const string path = "$.productBudgets.androidFrameP95AfterWarmup";
            ValidateSchema(frames, path, errors,
                "comparison", "unit", "baseline", "recommended", "highEnd", "requiredEvidence");
            RequireString(frames, "comparison", path, errors, "lessThan");
            RequireString(frames, "unit", path, errors, "ms");
            RequireNumber(frames, "baseline", path, errors, double.Epsilon, MaximumBaselineAndroidP95Ms);
            RequireNumber(frames, "recommended", path, errors, double.Epsilon, MaximumRecommendedAndroidP95Ms);
            RequireNumber(frames, "highEnd", path, errors, double.Epsilon, MaximumHighEndAndroidP95Ms);
            RequireEvidence(frames, "requiredEvidence", path, errors, FrameEvidence);
        }

        private static void ValidateGcBudget(JObject gc, List<string> errors)
        {
            const string path = "$.productBudgets.matchSteadyStateGc";
            ValidateSchema(gc, path, errors,
                "status", "baselineAllocatedBytes", "acceptanceBudgetBytes", "warmupFrames",
                "measuredFrames", "source");
            RequireString(gc, "status", path, errors, "red-baseline");
            RequireInteger(gc, "baselineAllocatedBytes", path, errors,
                ExpectedGcBaselineBytes, ExpectedGcBaselineBytes);
            RequireInteger(gc, "acceptanceBudgetBytes", path, errors, 0, MaximumGcAcceptanceBudgetBytes);
            RequireInteger(gc, "warmupFrames", path, errors, 180, int.MaxValue);
            RequireInteger(gc, "measuredFrames", path, errors, 300, int.MaxValue);
            RequireNonEmptyString(gc, "source", path, errors);
        }

        private static void ValidateMemoryBudget(JObject memory, List<string> errors)
        {
            const string path = "$.productBudgets.peakAllocatedMemory";
            ValidateSchema(memory, path, errors, "baseline", "target", "runtimeResidency", "requiredEvidence");

            JObject baseline = RequireObject(memory, "baseline", path, errors);
            JObject target = RequireObject(memory, "target", path, errors);
            JObject residency = RequireObject(memory, "runtimeResidency", path, errors);
            RequireEvidence(memory, "requiredEvidence", path, errors, MemoryEvidence);

            if (baseline != null)
            {
                const string baselinePath = path + ".baseline";
                ValidateSchema(baseline, baselinePath, errors,
                    "status", "metric", "unit", "minimum", "maximum", "deviceProfile", "source");
                RequireString(baseline, "status", baselinePath, errors, "validated-same-device");
                RequireString(baseline, "metric", baselinePath, errors, "peakAllocatedMemory");
                RequireString(baseline, "unit", baselinePath, errors, "MB");
                RequireInteger(baseline, "minimum", baselinePath, errors,
                    ExpectedPeakMemoryMinimumMB, ExpectedPeakMemoryMinimumMB);
                RequireInteger(baseline, "maximum", baselinePath, errors,
                    ExpectedPeakMemoryMaximumMB, ExpectedPeakMemoryMaximumMB);
                RequireNonEmptyString(baseline, "deviceProfile", baselinePath, errors);
                RequireNonEmptyString(baseline, "source", baselinePath, errors);
            }

            if (target != null)
            {
                const string targetPath = path + ".target";
                ValidateSchema(target, targetPath, errors,
                    "status", "comparison", "requiredReductionPercent", "sameDeviceRequired",
                    "absoluteReleaseLimitMB", "absoluteReleaseLimitStatus", "ownerTaskId");
                RequireString(target, "status", targetPath, errors, "relative-budget-active");
                RequireString(target, "comparison", targetPath, errors, "atLeastReductionPercent");
                RequireNumber(target, "requiredReductionPercent", targetPath, errors,
                    MinimumPeakMemoryReductionPercent, 100d);
                RequireBoolean(target, "sameDeviceRequired", targetPath, errors, true);
                RequireNull(target, "absoluteReleaseLimitMB", targetPath, errors);
                RequireString(target, "absoluteReleaseLimitStatus", targetPath, errors, "measurement-required");
                RequireString(target, "ownerTaskId", targetPath, errors, "APH-501");
            }

            if (residency != null)
            {
                const string residencyPath = path + ".runtimeResidency";
                ValidateSchema(residency, residencyPath, errors,
                    "status", "ownerTaskId", "inventorySource", "measurementBoundary");
                RequireString(residency, "status", residencyPath, errors, "uncertain-measurement-required");
                RequireString(residency, "ownerTaskId", residencyPath, errors, "APH-501");
                RequireNonEmptyString(residency, "inventorySource", residencyPath, errors);
                RequireNonEmptyString(residency, "measurementBoundary", residencyPath, errors);
            }
        }

        private static void ValidateReleaseEvidence(JObject releaseEvidence, List<string> errors)
        {
            const string path = "$.productBudgets.releaseEvidence";
            ValidateSchema(releaseEvidence, path, errors,
                "knownProfilerApkBaseline", "apk", "aab", "installedSize", "startupTime", "visualQuality");

            JObject knownApk = RequireObject(releaseEvidence, "knownProfilerApkBaseline", path, errors);
            JObject apk = RequireObject(releaseEvidence, "apk", path, errors);
            JObject aab = RequireObject(releaseEvidence, "aab", path, errors);
            JObject installed = RequireObject(releaseEvidence, "installedSize", path, errors);
            JObject startup = RequireObject(releaseEvidence, "startupTime", path, errors);
            JObject visual = RequireObject(releaseEvidence, "visualQuality", path, errors);

            if (knownApk != null)
            {
                const string knownPath = path + ".knownProfilerApkBaseline";
                ValidateSchema(knownApk, knownPath, errors,
                    "status", "observedApproximateRange", "source", "isReleaseLimit");
                RequireString(knownApk, "status", knownPath, errors,
                    "baseline-evidence-only-not-release-limit");
                RequireString(knownApk, "observedApproximateRange", knownPath, errors,
                    "443-471 MB depending on captured profiler build");
                RequireNonEmptyString(knownApk, "source", knownPath, errors);
                RequireBoolean(knownApk, "isReleaseLimit", knownPath, errors, false);
            }

            ValidatePackageEvidence(apk, path + ".apk", "APH-500", "APH-501", errors);
            ValidatePackageEvidence(aab, path + ".aab", "APH-500", "APH-501", errors);
            ValidateMeasurementEvidence(installed, path + ".installedSize", "releaseLimitBytes",
                "APH-501", InstalledSizeEvidence, errors);
            ValidateMeasurementEvidence(startup, path + ".startupTime", "p95LimitMs",
                "APH-803", StartupEvidence, errors);

            if (visual != null)
            {
                string visualPath = path + ".visualQuality";
                ValidateSchema(visual, visualPath, errors, "status", "ownerTaskId", "requiredEvidence");
                RequireString(visual, "status", visualPath, errors, "evidence-required");
                RequireString(visual, "ownerTaskId", visualPath, errors, "APH-809");
                RequireEvidence(visual, "requiredEvidence", visualPath, errors, VisualEvidence);
            }
        }

        private static void ValidatePackageEvidence(
            JObject evidence,
            string path,
            string measurementOwner,
            string budgetOwner,
            List<string> errors)
        {
            if (evidence == null)
                return;

            ValidateSchema(evidence, path, errors,
                "status", "releaseLimitBytes", "measurementOwnerTaskId", "budgetOwnerTaskId",
                "requiredEvidence");
            RequireString(evidence, "status", path, errors, "measurement-required");
            RequireNull(evidence, "releaseLimitBytes", path, errors);
            RequireString(evidence, "measurementOwnerTaskId", path, errors, measurementOwner);
            RequireString(evidence, "budgetOwnerTaskId", path, errors, budgetOwner);
            RequireEvidence(evidence, "requiredEvidence", path, errors, PackageEvidence);
        }

        private static void ValidateMeasurementEvidence(
            JObject evidence,
            string path,
            string limitProperty,
            string ownerTaskId,
            IReadOnlyCollection<string> requiredEvidence,
            List<string> errors)
        {
            if (evidence == null)
                return;

            ValidateSchema(evidence, path, errors, "status", limitProperty, "ownerTaskId", "requiredEvidence");
            RequireString(evidence, "status", path, errors, "measurement-required");
            RequireNull(evidence, limitProperty, path, errors);
            RequireString(evidence, "ownerTaskId", path, errors, ownerTaskId);
            RequireEvidence(evidence, "requiredEvidence", path, errors, requiredEvidence);
        }

        private static void ValidateSchema(
            JObject value,
            string path,
            List<string> errors,
            params string[] expectedProperties)
        {
            var expected = new HashSet<string>(expectedProperties, StringComparer.Ordinal);
            foreach (JProperty property in value.Properties())
            {
                if (!expected.Contains(property.Name))
                    errors.Add($"{path}.{property.Name} is not part of the tracked schema.");
            }

            foreach (string propertyName in expectedProperties)
            {
                if (value.Property(propertyName, StringComparison.Ordinal) == null)
                    errors.Add($"{path}.{propertyName} is required.");
            }
        }

        private static JObject RequireObject(JObject parent, string propertyName, string path, List<string> errors)
        {
            JToken token = parent[propertyName];
            if (token == null)
                return null;
            if (token.Type == JTokenType.Object)
                return (JObject)token;

            errors.Add($"{path}.{propertyName} must be an object.");
            return null;
        }

        private static void RequireString(
            JObject parent,
            string propertyName,
            string path,
            List<string> errors,
            string expected)
        {
            JToken token = parent[propertyName];
            if (token?.Type != JTokenType.String ||
                !string.Equals(token.Value<string>(), expected, StringComparison.Ordinal))
            {
                errors.Add($"{path}.{propertyName} must be '{expected}'.");
            }
        }

        private static void RequireNonEmptyString(
            JObject parent,
            string propertyName,
            string path,
            List<string> errors)
        {
            JToken token = parent[propertyName];
            if (token?.Type != JTokenType.String || string.IsNullOrWhiteSpace(token.Value<string>()))
                errors.Add($"{path}.{propertyName} must be a non-empty string.");
        }

        private static void RequireBoolean(
            JObject parent,
            string propertyName,
            string path,
            List<string> errors,
            bool expected)
        {
            JToken token = parent[propertyName];
            if (token?.Type != JTokenType.Boolean || token.Value<bool>() != expected)
                errors.Add($"{path}.{propertyName} must be {expected.ToString().ToLowerInvariant()}.");
        }

        private static void RequireNull(JObject parent, string propertyName, string path, List<string> errors)
        {
            JToken token = parent[propertyName];
            if (token == null)
                return;
            if (token.Type != JTokenType.Null)
                errors.Add($"{path}.{propertyName} must remain null while measurement is required.");
        }

        private static void RequireInteger(
            JObject parent,
            string propertyName,
            string path,
            List<string> errors,
            long minimum,
            long maximum)
        {
            JToken token = parent[propertyName];
            if (token?.Type != JTokenType.Integer)
            {
                errors.Add($"{path}.{propertyName} must be an integer.");
                return;
            }

            long value = token.Value<long>();
            if (value < minimum || value > maximum)
            {
                errors.Add($"{path}.{propertyName} must be in [{minimum.ToString(CultureInfo.InvariantCulture)}, " +
                           $"{maximum.ToString(CultureInfo.InvariantCulture)}].");
            }
        }

        private static void RequireNumber(
            JObject parent,
            string propertyName,
            string path,
            List<string> errors,
            double minimum,
            double maximum)
        {
            JToken token = parent[propertyName];
            if (token == null || (token.Type != JTokenType.Integer && token.Type != JTokenType.Float))
            {
                errors.Add($"{path}.{propertyName} must be numeric.");
                return;
            }

            double value = token.Value<double>();
            if (double.IsNaN(value) || double.IsInfinity(value) || value < minimum || value > maximum)
            {
                errors.Add($"{path}.{propertyName} must be in [{minimum.ToString(CultureInfo.InvariantCulture)}, " +
                           $"{maximum.ToString(CultureInfo.InvariantCulture)}].");
            }
        }

        private static void RequireEvidence(
            JObject parent,
            string propertyName,
            string path,
            List<string> errors,
            IReadOnlyCollection<string> requiredEvidence)
        {
            JToken token = parent[propertyName];
            if (token?.Type != JTokenType.Array)
            {
                errors.Add($"{path}.{propertyName} must be an array.");
                return;
            }

            var actual = new HashSet<string>(StringComparer.Ordinal);
            foreach (JToken item in (JArray)token)
            {
                if (item.Type != JTokenType.String || string.IsNullOrWhiteSpace(item.Value<string>()))
                {
                    errors.Add($"{path}.{propertyName} entries must be non-empty strings.");
                    continue;
                }

                string value = item.Value<string>();
                if (!actual.Add(value))
                    errors.Add($"{path}.{propertyName} contains duplicate evidence '{value}'.");
            }

            foreach (string requirement in requiredEvidence.Where(requirement => !actual.Contains(requirement)))
                errors.Add($"{path}.{propertyName} is missing required evidence '{requirement}'.");
        }

        private static void ExitBatchMode(int exitCode)
        {
            if (Application.isBatchMode)
                EditorApplication.Exit(exitCode);
        }
    }
}

#endif
