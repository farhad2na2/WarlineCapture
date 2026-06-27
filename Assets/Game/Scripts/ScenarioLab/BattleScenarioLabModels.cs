using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using UnityEngine;

public enum BattleScenarioSupportMode
{
    None = 0,
    RadarNear = 1,
    RadarFar = 2,
    Satellite = 3,
    Combined = 4
}

public enum BattleScenarioIncomingThreatKind
{
    GroundMissile = 0,
    Jet = 1,
    Drone = 2
}

public enum BattleScenarioExpectedOutcome
{
    Baseline = 0,
    MustIntercept = 1,
    MayFail = 2,
    MustImproveOrMatchBaseline = 3
}

public enum BattleScenarioFailureReason
{
    None = 0,
    InvalidSetup = 1,
    NoDetection = 2,
    NoTracking = 3,
    NoLock = 4,
    NoLaunch = 5,
    InterceptorTimeout = 6,
    IncomingThreatImpactedTarget = 7,
    TargetEntityMissing = 8,
    MetricsComparisonFailed = 9
}

public enum BattleScenarioStepOutcome
{
    Continue = 0,
    Complete = 1,
    Failed = 2
}

public delegate BattleScenarioStepOutcome BattleScenarioVariantStep(
    BattleScenarioFixedStepState state,
    BattleScenarioMetrics metrics);

[Serializable]
public readonly struct BattleScenarioFixedStepState
{
    public BattleScenarioFixedStepState(int frame, float timeSeconds, float fixedDeltaTime, bool isFinalFrame)
    {
        Frame = frame;
        TimeSeconds = timeSeconds;
        FixedDeltaTime = fixedDeltaTime;
        IsFinalFrame = isFinalFrame;
    }

    public int Frame { get; }
    public float TimeSeconds { get; }
    public float FixedDeltaTime { get; }
    public bool IsFinalFrame { get; }
}

public enum BattleScenarioCameraPreset
{
    Default = 0,
    AirDefenseSideView = 1,
    AirDefenseTopDown = 2,
    MissileInterceptCloseup = 3
}

[Serializable]
public struct BattleScenarioVariant
{
    public string VariantId;
    public string Label;
    public BattleScenarioSupportMode SupportMode;
    public BattleScenarioIncomingThreatKind IncomingThreatKind;
    public float IncomingThreatSpeedMultiplier;
    public float IncomingThreatStartDistance;
    public float IncomingThreatAltitude;
    public int LauncherCount;
    public float RadarDistanceFromLauncher;
    public BattleScenarioExpectedOutcome ExpectedOutcome;

    public static BattleScenarioVariant CreateDefault(string variantId, BattleScenarioSupportMode supportMode)
    {
        return new BattleScenarioVariant
        {
            VariantId = variantId,
            Label = variantId,
            SupportMode = supportMode,
            IncomingThreatKind = BattleScenarioIncomingThreatKind.GroundMissile,
            IncomingThreatSpeedMultiplier = 1f,
            IncomingThreatStartDistance = 170f,
            IncomingThreatAltitude = 12f,
            LauncherCount = 1,
            RadarDistanceFromLauncher = supportMode == BattleScenarioSupportMode.RadarNear ? 8f : 0f,
            ExpectedOutcome = supportMode == BattleScenarioSupportMode.None
                ? BattleScenarioExpectedOutcome.Baseline
                : BattleScenarioExpectedOutcome.MustImproveOrMatchBaseline
        };
    }
}

[Serializable]
public struct BattleScenarioSpawnEntry
{
    public string SourcePrefabKey;
    public ScriptableObject ConfigAsset;
    public byte FactionId;
    public Vector3 WorldPosition;
    public Vector3 WorldRotationEuler;
    public int InitialHealth;
    public string InitialCommandState;
}

[Serializable]
public struct BattleScenarioSuccessCriteria
{
    public bool RequireDetection;
    public bool RequireLaunch;
    public bool RequireInterceptForSupportedNormal;
    public bool RequireSupportedVariantImprovesOrMatchesBaseline;
    public float MaxSupportedDetectionDelaySeconds;
    public float MaxSupportedLockDelaySeconds;

    public static BattleScenarioSuccessCriteria Default => new()
    {
        RequireDetection = true,
        RequireLaunch = true,
        RequireInterceptForSupportedNormal = true,
        RequireSupportedVariantImprovesOrMatchesBaseline = true,
        MaxSupportedDetectionDelaySeconds = 0f,
        MaxSupportedLockDelaySeconds = 0f
    };
}

[Serializable]
public sealed class BattleScenarioMetrics
{
    public string ScenarioId;
    public string VariantId;
    public int Seed;
    public float DurationSeconds;
    public int Frames;
    public bool Detected;
    public float DetectionTimeSeconds = -1f;
    public bool TrackingStarted;
    public float TrackingStartTimeSeconds = -1f;
    public bool Locked;
    public float LockTimeSeconds = -1f;
    public bool InterceptorLaunched;
    public float LaunchTimeSeconds = -1f;
    public bool Intercepted;
    public float InterceptTimeSeconds = -1f;
    public bool IncomingThreatImpacted;
    public float IncomingThreatImpactTimeSeconds = -1f;
    public float IncomingThreatDistanceAtDetection = -1f;
    public float InterceptDistanceFromDefendedTarget = -1f;
    public float ClosestInterceptorDistanceToThreat = -1f;
    public float LauncherEffectiveRange;
    public float LauncherEffectiveLockSeconds;
    public float LauncherEffectiveTrackingQuality;
    public float LauncherEffectiveTurnRateDegreesPerSecond;
    public bool RadarProviderUsed;
    public bool SatelliteProviderUsed;
    public BattleScenarioFailureReason FailureReason;
}

[Serializable]
public sealed class BattleScenarioComparison
{
    public string BaselineVariantId;
    public string SupportedVariantId;
    public bool RadarImprovedDetectionTime;
    public bool RadarImprovedLockTime;
    public bool RadarImprovedOrMatchedOutcome;
    public float DetectionTimeDeltaSeconds;
    public float LockTimeDeltaSeconds;
}

[Serializable]
public sealed class BattleScenarioResult
{
    public string ScenarioId;
    public string GeneratedAtUtc;
    public float FixedDeltaTime;
    public BattleScenarioMetrics[] Variants = Array.Empty<BattleScenarioMetrics>();
    public BattleScenarioComparison[] Comparisons = Array.Empty<BattleScenarioComparison>();
    public bool Passed;
    public BattleScenarioFailureReason FailureReason;
}

public static class BattleScenarioResultComparison
{
    public static BattleScenarioComparison CompareRadarSupport(
        BattleScenarioMetrics baseline,
        BattleScenarioMetrics supported)
    {
        float detectionDelta = ResolveDelta(baseline.DetectionTimeSeconds, supported.DetectionTimeSeconds);
        float lockDelta = ResolveDelta(baseline.LockTimeSeconds, supported.LockTimeSeconds);
        bool outcomeImprovedOrMatched = supported.Intercepted ||
                                        (!baseline.Intercepted && IsCloserOrEqual(supported, baseline));

        return new BattleScenarioComparison
        {
            BaselineVariantId = baseline.VariantId,
            SupportedVariantId = supported.VariantId,
            DetectionTimeDeltaSeconds = detectionDelta,
            LockTimeDeltaSeconds = lockDelta,
            RadarImprovedDetectionTime = supported.Detected && (!baseline.Detected || detectionDelta <= 0f),
            RadarImprovedLockTime = supported.Locked && (!baseline.Locked || lockDelta <= 0f),
            RadarImprovedOrMatchedOutcome = outcomeImprovedOrMatched
        };
    }

    private static float ResolveDelta(float baselineValue, float supportedValue)
    {
        if (baselineValue < 0f || supportedValue < 0f)
            return supportedValue - baselineValue;
        return supportedValue - baselineValue;
    }

    private static bool IsCloserOrEqual(BattleScenarioMetrics supported, BattleScenarioMetrics baseline)
    {
        if (supported.ClosestInterceptorDistanceToThreat >= 0f && baseline.ClosestInterceptorDistanceToThreat >= 0f)
            return supported.ClosestInterceptorDistanceToThreat <= baseline.ClosestInterceptorDistanceToThreat;

        if (supported.InterceptDistanceFromDefendedTarget >= 0f && baseline.InterceptDistanceFromDefendedTarget >= 0f)
            return supported.InterceptDistanceFromDefendedTarget >= baseline.InterceptDistanceFromDefendedTarget;

        return supported.FailureReason == BattleScenarioFailureReason.None ||
               baseline.FailureReason != BattleScenarioFailureReason.None;
    }
}

public static class BattleScenarioFixedStepRunner
{
    public static BattleScenarioResult RunScenario(
        BattleScenarioDefinition definition,
        IReadOnlyList<BattleScenarioVariantStep> steps)
    {
        if (definition == null)
            throw new ArgumentNullException(nameof(definition));

        if (steps == null)
            throw new ArgumentNullException(nameof(steps));

        BattleScenarioVariant[] variants = definition.ScenarioVariants;
        if (steps.Count != variants.Length)
            throw new ArgumentException("Step count must match scenario variant count.", nameof(steps));

        var result = new BattleScenarioResult
        {
            ScenarioId = definition.ScenarioId,
            GeneratedAtUtc = DateTime.UtcNow.ToString("O"),
            FixedDeltaTime = definition.FixedDeltaTime,
            Variants = new BattleScenarioMetrics[variants.Length],
            Comparisons = Array.Empty<BattleScenarioComparison>()
        };

        for (int i = 0; i < variants.Length; i++)
        {
            result.Variants[i] = RunVariant(
                definition.ScenarioId,
                variants[i],
                definition.RandomSeed,
                definition.FixedDeltaTime,
                definition.MaxDurationSeconds,
                steps[i]);
        }

        result.Passed = EvaluateScenarioPass(result.Variants);
        return result;
    }

    public static BattleScenarioMetrics RunVariant(
        string scenarioId,
        BattleScenarioVariant variant,
        int seed,
        float fixedDeltaTime,
        float maxDurationSeconds,
        BattleScenarioVariantStep step)
    {
        if (step == null)
            throw new ArgumentNullException(nameof(step));

        float safeFixedDelta = Mathf.Max(0.001f, fixedDeltaTime);
        float safeMaxDuration = Mathf.Max(safeFixedDelta, maxDurationSeconds);
        int maxFrames = Mathf.Max(1, Mathf.CeilToInt(safeMaxDuration / safeFixedDelta));
        var metrics = new BattleScenarioMetrics
        {
            ScenarioId = scenarioId,
            VariantId = variant.VariantId,
            Seed = seed,
            FailureReason = BattleScenarioFailureReason.None
        };

        for (int frame = 0; frame < maxFrames; frame++)
        {
            var state = new BattleScenarioFixedStepState(
                frame,
                frame * safeFixedDelta,
                safeFixedDelta,
                frame == maxFrames - 1);

            metrics.Frames = frame + 1;
            metrics.DurationSeconds = state.TimeSeconds;

            BattleScenarioStepOutcome outcome = step(state, metrics);
            if (outcome == BattleScenarioStepOutcome.Complete)
                return metrics;

            if (outcome == BattleScenarioStepOutcome.Failed)
            {
                if (metrics.FailureReason == BattleScenarioFailureReason.None)
                    metrics.FailureReason = BattleScenarioFailureReason.InvalidSetup;

                return metrics;
            }
        }

        metrics.DurationSeconds = maxFrames * safeFixedDelta;
        if (!metrics.Intercepted &&
            !metrics.IncomingThreatImpacted &&
            metrics.FailureReason == BattleScenarioFailureReason.None)
        {
            metrics.FailureReason = BattleScenarioFailureReason.InterceptorTimeout;
        }

        return metrics;
    }

    private static bool EvaluateScenarioPass(IReadOnlyList<BattleScenarioMetrics> variants)
    {
        for (int i = 0; i < variants.Count; i++)
        {
            BattleScenarioMetrics metrics = variants[i];
            if (metrics.FailureReason != BattleScenarioFailureReason.None && !metrics.Intercepted)
                return false;
        }

        return true;
    }
}

public static class BattleScenarioReportJson
{
    public static string ToJson(BattleScenarioResult result, bool prettyPrint = true)
    {
        if (result == null)
            return "{}";

        var builder = new StringBuilder(4096);
        JsonWriter writer = new(builder, prettyPrint);
        writer.BeginObject();
        writer.WriteString("ScenarioId", result.ScenarioId);
        writer.WriteString("GeneratedAtUtc", result.GeneratedAtUtc);
        writer.WriteNumber("FixedDeltaTime", result.FixedDeltaTime);
        writer.WritePropertyName("Variants");
        writer.BeginArray();
        for (int i = 0; i < result.Variants.Length; i++)
        {
            if (i > 0)
                writer.WriteArraySeparator();
            WriteMetrics(writer, result.Variants[i]);
        }
        writer.EndArray();
        writer.WritePropertyName("Comparisons");
        writer.BeginArray();
        for (int i = 0; i < result.Comparisons.Length; i++)
        {
            if (i > 0)
                writer.WriteArraySeparator();
            WriteComparison(writer, result.Comparisons[i]);
        }
        writer.EndArray();
        writer.WriteBool("Passed", result.Passed);
        writer.WriteString("FailureReason", result.FailureReason.ToString());
        writer.EndObject();
        return builder.ToString();
    }

    private static void WriteMetrics(JsonWriter writer, BattleScenarioMetrics metrics)
    {
        writer.BeginObject();
        writer.WriteString("ScenarioId", metrics.ScenarioId);
        writer.WriteString("VariantId", metrics.VariantId);
        writer.WriteNumber("Seed", metrics.Seed);
        writer.WriteNumber("DurationSeconds", metrics.DurationSeconds);
        writer.WriteNumber("Frames", metrics.Frames);
        writer.WriteBool("Detected", metrics.Detected);
        writer.WriteNumber("DetectionTimeSeconds", metrics.DetectionTimeSeconds);
        writer.WriteBool("TrackingStarted", metrics.TrackingStarted);
        writer.WriteNumber("TrackingStartTimeSeconds", metrics.TrackingStartTimeSeconds);
        writer.WriteBool("Locked", metrics.Locked);
        writer.WriteNumber("LockTimeSeconds", metrics.LockTimeSeconds);
        writer.WriteBool("InterceptorLaunched", metrics.InterceptorLaunched);
        writer.WriteNumber("LaunchTimeSeconds", metrics.LaunchTimeSeconds);
        writer.WriteBool("Intercepted", metrics.Intercepted);
        writer.WriteNumber("InterceptTimeSeconds", metrics.InterceptTimeSeconds);
        writer.WriteBool("IncomingThreatImpacted", metrics.IncomingThreatImpacted);
        writer.WriteNumber("IncomingThreatImpactTimeSeconds", metrics.IncomingThreatImpactTimeSeconds);
        writer.WriteNumber("IncomingThreatDistanceAtDetection", metrics.IncomingThreatDistanceAtDetection);
        writer.WriteNumber("InterceptDistanceFromDefendedTarget", metrics.InterceptDistanceFromDefendedTarget);
        writer.WriteNumber("ClosestInterceptorDistanceToThreat", metrics.ClosestInterceptorDistanceToThreat);
        writer.WriteNumber("LauncherEffectiveRange", metrics.LauncherEffectiveRange);
        writer.WriteNumber("LauncherEffectiveLockSeconds", metrics.LauncherEffectiveLockSeconds);
        writer.WriteNumber("LauncherEffectiveTrackingQuality", metrics.LauncherEffectiveTrackingQuality);
        writer.WriteNumber("LauncherEffectiveTurnRateDegreesPerSecond", metrics.LauncherEffectiveTurnRateDegreesPerSecond);
        writer.WriteBool("RadarProviderUsed", metrics.RadarProviderUsed);
        writer.WriteBool("SatelliteProviderUsed", metrics.SatelliteProviderUsed);
        writer.WriteString("FailureReason", metrics.FailureReason.ToString());
        writer.EndObject();
    }

    private static void WriteComparison(JsonWriter writer, BattleScenarioComparison comparison)
    {
        writer.BeginObject();
        writer.WriteString("BaselineVariantId", comparison.BaselineVariantId);
        writer.WriteString("SupportedVariantId", comparison.SupportedVariantId);
        writer.WriteBool("RadarImprovedDetectionTime", comparison.RadarImprovedDetectionTime);
        writer.WriteBool("RadarImprovedLockTime", comparison.RadarImprovedLockTime);
        writer.WriteBool("RadarImprovedOrMatchedOutcome", comparison.RadarImprovedOrMatchedOutcome);
        writer.WriteNumber("DetectionTimeDeltaSeconds", comparison.DetectionTimeDeltaSeconds);
        writer.WriteNumber("LockTimeDeltaSeconds", comparison.LockTimeDeltaSeconds);
        writer.EndObject();
    }

    private readonly struct JsonWriter
    {
        private readonly StringBuilder _builder;
        private readonly bool _prettyPrint;
        private readonly string _indent;
        private readonly int _depth;

        public JsonWriter(StringBuilder builder, bool prettyPrint, string indent = "    ", int depth = 0)
        {
            _builder = builder;
            _prettyPrint = prettyPrint;
            _indent = indent;
            _depth = depth;
        }

        public void BeginObject()
        {
            _builder.Append('{');
            NewLine();
        }

        public void EndObject()
        {
            NewLineBeforeClose();
            _builder.Append('}');
        }

        public void BeginArray()
        {
            _builder.Append('[');
            NewLine();
        }

        public void EndArray()
        {
            NewLineBeforeClose();
            _builder.Append(']');
        }

        public void WriteArraySeparator()
        {
            _builder.Append(',');
            NewLine();
        }

        public void WritePropertyName(string name)
        {
            WriteCommaIfNeeded();
            WriteIndent();
            WriteEscaped(name);
            _builder.Append(_prettyPrint ? ": " : ":");
        }

        public void WriteString(string name, string value)
        {
            WritePropertyName(name);
            WriteEscaped(value ?? string.Empty);
        }

        public void WriteNumber(string name, int value)
        {
            WritePropertyName(name);
            _builder.Append(value.ToString(CultureInfo.InvariantCulture));
        }

        public void WriteNumber(string name, float value)
        {
            WritePropertyName(name);
            _builder.Append(value.ToString("R", CultureInfo.InvariantCulture));
        }

        public void WriteBool(string name, bool value)
        {
            WritePropertyName(name);
            _builder.Append(value ? "true" : "false");
        }

        private void WriteCommaIfNeeded()
        {
            if (_builder.Length == 0)
                return;

            char last = _builder[_builder.Length - 1];
            if (last != '{' && last != '[' && last != '\n' && last != ',')
            {
                _builder.Append(',');
                NewLine();
            }
        }

        private void NewLine()
        {
            if (_prettyPrint)
                _builder.Append('\n');
        }

        private void NewLineBeforeClose()
        {
            if (!_prettyPrint)
                return;

            if (_builder.Length > 0 && _builder[_builder.Length - 1] == '\n')
            {
                for (int i = 0; i < _depth; i++)
                    _builder.Append(_indent);
            }
            else
            {
                _builder.Append('\n');
                for (int i = 0; i < _depth; i++)
                    _builder.Append(_indent);
            }
        }

        private void WriteIndent()
        {
            if (!_prettyPrint)
                return;

            for (int i = 0; i < _depth + 1; i++)
                _builder.Append(_indent);
        }

        private void WriteEscaped(string value)
        {
            _builder.Append('"');
            for (int i = 0; i < value.Length; i++)
            {
                char c = value[i];
                switch (c)
                {
                    case '\\':
                        _builder.Append("\\\\");
                        break;
                    case '"':
                        _builder.Append("\\\"");
                        break;
                    case '\n':
                        _builder.Append("\\n");
                        break;
                    case '\r':
                        _builder.Append("\\r");
                        break;
                    case '\t':
                        _builder.Append("\\t");
                        break;
                    default:
                        _builder.Append(c);
                        break;
                }
            }
            _builder.Append('"');
        }
    }
}
