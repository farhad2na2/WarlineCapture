using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Xml;
using Process = System.Diagnostics.Process;
using ProcessStartInfo = System.Diagnostics.ProcessStartInfo;
#if UNITY_INCLUDE_TESTS && UNITY_EDITOR
using NUnit.Framework;
using UnityEngine;
#endif

public sealed class ProductionSourceGrowthArchitectureTests
{
    private const string BaselinePath = "Design/Architecture/production_source_growth_baseline.md";
    private const string PostHardeningGuardrailPath =
        "Design/Architecture/post_hardening_source_responsibility_guardrails.json";
    private const string PostHardeningGuardrailContractId = "post-hardening-source-responsibility-v1";
    private const int PostHardeningGuardrailEntryCount = 2;
    private const string TrackerPath =
        "Design/Architecture/architecture_performance_hardening_implementation_tracker.md";
    private const string PostHardeningTrackerPath =
        "Design/Architecture/post_hardening_architecture_maturity_tracker.md";
    private const string FeatureReadinessTrackerPath =
        "Design/Architecture/am025_feature_readiness_architecture_closeout_tracker.md";
    private const string ProductionRoot = "Assets/Game/Scripts";
    private const string EditorPathSegment = "Editor";
    private const string SystemHelperSuffix = "SystemHelper.cs";
    private const string SystemHelperScope = "system-helper";
    private const string SystemHelperGrowthScope = "system-helper-growth";
    private const string ProductionReviewScope = "production-over-500-review";
    private const string StrictNoGrowthScope = "production-over-1000-growth";
    private const string ProductionPathRecreationScope = "production-path-recreation";
    private const string SystemHelperRecreationScope = "system-helper-recreation";
    private const string BaselineCommit = "9280ead856fd0bf117fdb3601cc2216c3a35e0f4";
    private const int ReviewThresholdLines = 500;
    private const int StrictNoGrowthThresholdLines = 1000;
    private const int FrozenSystemHelperPathCount = 265;
    private const int ReviewedProductionFileCount = 108;
    private const int StrictNoGrowthFileCount = 27;
    private const string FrozenSystemHelperCeilingsSha256 =
        "18529b2c77b9a2823ea6bd23d3f774088d882f3eab8a28205b0d3ce7618780af";
    private const string ReviewedProductionCeilingsSha256 =
        "893d7bca1ca648334e0afb7e2656bdada0db054c67e62b324a0e625cb9ed36db";
    private const string ManifestStartMarker = "<!-- production-source-growth-manifest:start -->";
    private const string ManifestEndMarker = "<!-- production-source-growth-manifest:end -->";

    public static readonly string FocusedRunnerMarker =
        "[ProductionSourceGrowthArchitectureValidation] result=Passed tests=17";
    public static readonly string PostHardeningGuardrailFocusedRunnerMarker =
        "[PostHardeningSourceGuardrailValidation] result=Passed tests=2";
    public static readonly string PostHardeningAuthorizationFocusedRunnerMarker =
        "[PostHardeningSourceAuthorizationValidation] result=Passed tests=1";

    private static readonly StringComparer PathIdentityComparer = StringComparer.OrdinalIgnoreCase;

    private static readonly Regex TrackerTaskIdRegex = new(
        @"^APH-[0-9]{3}$",
        RegexOptions.CultureInvariant);

    private static readonly Regex DecisionIdRegex = new(
        @"^D-[0-9]{3}$",
        RegexOptions.CultureInvariant);

    private static readonly Regex ActiveOrCompletedTrackerTaskRegex = new(
        @"^[ \t]*- \[(?:[xX]|~)\] `(?<id>APH-[0-9]{3})`",
        RegexOptions.CultureInvariant | RegexOptions.Multiline);

#if UNITY_INCLUDE_TESTS && UNITY_EDITOR
    public static void RunFocusedValidation()
    {
        try
        {
            var tests = new ProductionSourceGrowthArchitectureTests();
            tests.BaselineManifestHasExpectedFrozenInventories();
            tests.NewSystemHelperPathsRequireApprovedException();
            tests.ProductionFilesAboveFiveHundredLinesRequireReview();
            tests.ExistingFilesAboveOneThousandLinesDoNotGrow();
            tests.AllBaselinedPathsRespectRatchetedLineAndByteCeilings();
            tests.ApprovedExceptionsUseExactTrackedAuthorization();
            tests.ExactDecisionAuthorizationRejectsMismatchedTuples();
            tests.HistoricalPolicyRejectsReservedGrowthAndRecreation();
            tests.JenkinsEditModeGateFailsClosed();
            tests.JenkinsCheckoutProvidesGuardInputsAndHistory();
            tests.JenkinsResultContractRejectsIncompleteRuns();
            tests.BinaryNumstatRecordsFailClosed();
            tests.DuplicateJsonPropertiesFailClosedAtEveryLevel();
            tests.WindowsPathIdentityIsCaseInsensitiveButSpellingStable();
            tests.ByteCeilingRejectsPhysicalLineMinificationBypass();
            tests.PostHardeningGuardrailContractHasExpectedRatchets();
            tests.PostHardeningGuardedSourcesStayBoundedAndNarrow();
            Debug.Log(FocusedRunnerMarker);
            ValidationExit.Exit(0);
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            Debug.LogError("[ProductionSourceGrowthArchitectureValidation] result=Failed");
            ValidationExit.Exit(1);
        }
    }

    public static void RunPostHardeningGuardrailFocusedValidation()
    {
        try
        {
            var tests = new ProductionSourceGrowthArchitectureTests();
            tests.PostHardeningGuardrailContractHasExpectedRatchets();
            tests.PostHardeningGuardedSourcesStayBoundedAndNarrow();
            Debug.Log(PostHardeningGuardrailFocusedRunnerMarker);
            ValidationExit.Exit(0);
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            Debug.LogError("[PostHardeningSourceGuardrailValidation] result=Failed");
            ValidationExit.Exit(1);
        }
    }

    public static void RunPostHardeningAuthorizationFocusedValidation()
    {
        try
        {
            var tests = new ProductionSourceGrowthArchitectureTests();
            tests.PostHardeningGuardrailContractHasExpectedRatchets();
            Debug.Log(PostHardeningAuthorizationFocusedRunnerMarker);
            ValidationExit.Exit(0);
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            Debug.LogError("[PostHardeningSourceAuthorizationValidation] result=Failed");
            ValidationExit.Exit(1);
        }
    }
#endif

#if UNITY_INCLUDE_TESTS && UNITY_EDITOR
    [Test]
#endif
    public void BaselineManifestHasExpectedFrozenInventories()
    {
        BaselineManifest manifest = LoadManifest();

        Require(manifest.SchemaVersion == 3, "The production growth baseline schemaVersion must remain 3.");
        Require(manifest.BaselineCommit == BaselineCommit, $"baselineCommit must remain `{BaselineCommit}`.");
        Require(manifest.ProductionRoot == ProductionRoot, $"productionRoot must be `{ProductionRoot}`.");
        Require(
            manifest.ProductionEditorPathSegment == EditorPathSegment,
            $"productionEditorPathSegment must be `{EditorPathSegment}`.");
        Require(manifest.HelperSuffix == SystemHelperSuffix, $"helperSuffix must be `{SystemHelperSuffix}`.");
        Require(
            manifest.ReviewThresholdLines == ReviewThresholdLines,
            $"reviewThresholdLines must remain {ReviewThresholdLines}.");
        Require(
            manifest.StrictNoGrowthThresholdLines == StrictNoGrowthThresholdLines,
            $"strictNoGrowthThresholdLines must remain {StrictNoGrowthThresholdLines}.");
        Require(manifest.FrozenSystemHelpers != null, "frozenSystemHelpers is required.");
        Require(manifest.ProductionFilesOver500 != null, "productionFilesOver500 is required.");
        Require(manifest.ApprovedExceptions != null, "approvedExceptions must be a structured array.");

        Require(
            manifest.FrozenSystemHelpers.Count == FrozenSystemHelperPathCount,
            $"The APH-701 baseline must contain exactly {FrozenSystemHelperPathCount} frozen helper paths; " +
            $"found {manifest.FrozenSystemHelpers.Count}.");
        Require(
            manifest.ProductionFilesOver500.Count == ReviewedProductionFileCount,
            $"The APH-702 baseline must contain exactly {ReviewedProductionFileCount} reviewed files; " +
            $"found {manifest.ProductionFilesOver500.Count}.");
        Require(
            IsOrdinallySorted(manifest.FrozenSystemHelpers.Select(entry => entry.Path).ToList()),
            "frozenSystemHelpers must remain in ordinal path order.");
        Require(
            ComputeLineHash(manifest.FrozenSystemHelpers.Select(
                entry => entry.Path + "\t" + entry.BaselineLines + "\t" + entry.BaselineBytes)) ==
            FrozenSystemHelperCeilingsSha256,
            "The immutable APH-701 helper path/line/byte ceiling set changed.");

        var frozenPaths = new HashSet<string>(PathIdentityComparer);
        foreach (FrozenSystemHelperBaseline entry in manifest.FrozenSystemHelpers)
        {
            Require(entry != null, "frozenSystemHelpers cannot contain null entries.");
            RequireExactProjectSourcePath(entry.Path, "frozen helper path");
            Require(entry.Path.EndsWith(SystemHelperSuffix, StringComparison.Ordinal), $"`{entry.Path}` is not a *SystemHelper.cs path.");
            Require(entry.BaselineLines > 0, $"`{entry.Path}` requires a positive baselineLines ceiling.");
            Require(entry.BaselineBytes > 0, $"`{entry.Path}` requires a positive baselineBytes ceiling.");
            Require(frozenPaths.Add(entry.Path), $"Case-insensitive duplicate frozen helper path: `{entry.Path}`.");
        }

        var reviewedPaths = new HashSet<string>(PathIdentityComparer);
        string previousPath = null;
        int strictCount = 0;
        foreach (ProductionFileBaseline entry in manifest.ProductionFilesOver500)
        {
            Require(entry != null, "productionFilesOver500 cannot contain null entries.");
            RequireExactProjectSourcePath(entry.Path, "production baseline path");
            Require(!HasPathSegment(entry.Path, EditorPathSegment), $"Editor source is outside production scope: `{entry.Path}`.");
            Require(entry.BaselineLines > ReviewThresholdLines, $"`{entry.Path}` is not above the 500-line review threshold.");
            Require(entry.BaselineBytes > 0, $"`{entry.Path}` requires a positive baselineBytes ceiling.");
            Require(
                entry.StrictNoGrowth == (entry.BaselineLines > StrictNoGrowthThresholdLines),
                $"`{entry.Path}` has an inconsistent strictNoGrowth value for {entry.BaselineLines} lines.");
            Require(reviewedPaths.Add(entry.Path), $"Case-insensitive duplicate production baseline path: `{entry.Path}`.");
            Require(
                previousPath == null || string.CompareOrdinal(previousPath, entry.Path) < 0,
                "productionFilesOver500 must remain in ordinal path order.");
            previousPath = entry.Path;
            if (entry.StrictNoGrowth)
                strictCount++;
        }

        Require(
            strictCount == StrictNoGrowthFileCount,
            $"The APH-702 strict no-growth set must contain exactly {StrictNoGrowthFileCount} files; found {strictCount}.");
        Require(
            ComputeLineHash(
                manifest.ProductionFilesOver500.Select(
                    entry => entry.Path + "\t" + entry.BaselineLines + "\t" + entry.BaselineBytes)) ==
            ReviewedProductionCeilingsSha256,
            "The immutable APH-702 path/line/byte ceiling set changed. All 108 reviewed ceilings are frozen; " +
            "growth requires an approved exception rather than a baseline edit.");
        RequireBaselineRepositorySpelling(manifest);
    }

#if UNITY_INCLUDE_TESTS && UNITY_EDITOR
    [Test]
#endif
    public void NewSystemHelperPathsRequireApprovedException()
    {
        BaselineManifest manifest = LoadManifest();
        Dictionary<string, FrozenSystemHelperBaseline> frozenPaths = manifest.FrozenSystemHelpers
            .ToDictionary(entry => entry.Path, entry => entry, PathIdentityComparer);
        List<SourceFile> helpers = EnumerateFiles(ProductionRoot, "*" + SystemHelperSuffix, includeEditorPaths: true);
        var violations = new List<string>();

        foreach (SourceFile helper in helpers)
        {
            if (frozenPaths.TryGetValue(helper.Path, out FrozenSystemHelperBaseline frozen))
            {
                RequireRepositorySpelling(frozen.Path, helper.Path, "frozen helper");
                continue;
            }
            if (HasBoundException(manifest, helper, SystemHelperScope))
                continue;

            violations.Add($"{helper.Path} ({helper.LineCount} lines, {helper.ByteCount} bytes)");
        }

        Require(
            violations.Count == 0,
            "New *SystemHelper.cs and *CompositionSystemHelper.cs paths require an exact `system-helper` " +
            "approved exception. Violations:\n" + string.Join("\n", violations));
    }

#if UNITY_INCLUDE_TESTS && UNITY_EDITOR
    [Test]
#endif
    public void ProductionFilesAboveFiveHundredLinesRequireReview()
    {
        BaselineManifest manifest = LoadManifest();
        Dictionary<string, SourceHistoryState> history = BuildSourceHistory(manifest);
        Dictionary<string, ProductionFileBaseline> reviewed = manifest.ProductionFilesOver500
            .ToDictionary(entry => entry.Path, entry => entry, PathIdentityComparer);
        List<SourceFile> productionFiles = EnumerateFiles(ProductionRoot, "*.cs", includeEditorPaths: false);
        var violations = new List<string>();

        foreach (SourceFile source in productionFiles)
        {
            if (source.LineCount <= ReviewThresholdLines)
                continue;

            if (!reviewed.TryGetValue(source.Path, out ProductionFileBaseline entry))
            {
                if (!HasBoundException(manifest, source, ProductionReviewScope))
                    violations.Add($"unreviewed {source.Path} ({source.LineCount} lines, {source.ByteCount} bytes)");
                continue;
            }

            RequireRepositorySpelling(entry.Path, source.Path, "reviewed production source");
            SourceHistoryState state = history[entry.Path];
            if (source.LineCount <= state.MinimumPositiveLines && source.ByteCount <= state.MinimumPositiveBytes)
                continue;

            string requiredScope = entry.StrictNoGrowth ? StrictNoGrowthScope : ProductionReviewScope;
            if (!HasBoundException(manifest, source, requiredScope))
            {
                violations.Add(
                    $"grew {source.Path} (lines {state.MinimumPositiveLines} -> {source.LineCount}, " +
                    $"bytes {state.MinimumPositiveBytes} -> {source.ByteCount}; " +
                    $"requires {requiredScope})");
            }
        }

        Require(
            violations.Count == 0,
            "Production files above 500 lines must be baselined at their reviewed size or covered by an " +
            "exact line-bounded approved exception. Violations:\n" + string.Join("\n", violations));
    }

#if UNITY_INCLUDE_TESTS && UNITY_EDITOR
    [Test]
#endif
    public void ExistingFilesAboveOneThousandLinesDoNotGrow()
    {
        BaselineManifest manifest = LoadManifest();
        Dictionary<string, SourceHistoryState> history = BuildSourceHistory(manifest);
        var violations = new List<string>();

        foreach (ProductionFileBaseline entry in manifest.ProductionFilesOver500.Where(item => item.StrictNoGrowth))
        {
            if (!File.Exists(entry.Path))
                continue;

            SourceFile current = MeasureFile(entry.Path);
            SourceHistoryState state = history[entry.Path];
            if (current.LineCount <= state.MinimumPositiveLines && current.ByteCount <= state.MinimumPositiveBytes)
                continue;
            if (HasBoundException(manifest, current, StrictNoGrowthScope))
                continue;

            violations.Add(
                $"{entry.Path} (lines {state.MinimumPositiveLines} -> {current.LineCount}, " +
                $"bytes {state.MinimumPositiveBytes} -> {current.ByteCount})");
        }

        Require(
            violations.Count == 0,
            "The 27 production files frozen above 1,000 lines have strict no-growth ceilings. " +
            "Growth requires an exact `production-over-1000-growth` exception. Violations:\n" +
            string.Join("\n", violations));
    }

#if UNITY_INCLUDE_TESTS && UNITY_EDITOR
    [Test]
#endif
    public void AllBaselinedPathsRespectRatchetedLineAndByteCeilings()
    {
        BaselineManifest manifest = LoadManifest();
        Dictionary<string, SourceHistoryState> history = BuildSourceHistory(manifest);
        Dictionary<string, SourceFile> production = EnumerateFiles(ProductionRoot, "*.cs", includeEditorPaths: false)
            .ToDictionary(entry => entry.Path, entry => entry, PathIdentityComparer);
        Dictionary<string, SourceFile> helpers = EnumerateFiles(ProductionRoot, "*" + SystemHelperSuffix, includeEditorPaths: true)
            .ToDictionary(entry => entry.Path, entry => entry, PathIdentityComparer);
        var violations = new List<string>();

        foreach (ProductionFileBaseline entry in manifest.ProductionFilesOver500)
        {
            SourceHistoryState state = history[entry.Path];
            if (!production.TryGetValue(entry.Path, out SourceFile current))
                continue;
            RequireRepositorySpelling(entry.Path, current.Path, "reviewed production source");
            if (state.WasRecreated && !HasBoundException(manifest, current, ProductionPathRecreationScope))
            {
                violations.Add($"recreated reviewed path {entry.Path}");
            }

            string growthScope = entry.StrictNoGrowth ? StrictNoGrowthScope : ProductionReviewScope;
            if ((current.LineCount > state.MinimumPositiveLines || current.ByteCount > state.MinimumPositiveBytes) &&
                !HasBoundException(manifest, current, growthScope))
            {
                violations.Add(
                    $"reviewed path regrew {entry.Path} (lines {state.MinimumPositiveLines} -> {current.LineCount}, " +
                    $"bytes {state.MinimumPositiveBytes} -> {current.ByteCount}; requires {growthScope})");
            }
        }

        foreach (FrozenSystemHelperBaseline helper in manifest.FrozenSystemHelpers)
        {
            SourceHistoryState state = history[helper.Path];
            if (!helpers.TryGetValue(helper.Path, out SourceFile current))
                continue;
            RequireRepositorySpelling(helper.Path, current.Path, "frozen helper");
            if (state.WasRecreated && !HasBoundException(manifest, current, SystemHelperRecreationScope))
            {
                violations.Add($"recreated frozen helper path {helper.Path}");
            }

            if ((current.LineCount > state.MinimumPositiveLines || current.ByteCount > state.MinimumPositiveBytes) &&
                !HasBoundException(manifest, current, SystemHelperGrowthScope))
            {
                violations.Add(
                    $"helper path regrew {helper.Path} (lines {state.MinimumPositiveLines} -> {current.LineCount}, " +
                    $"bytes {state.MinimumPositiveBytes} -> {current.ByteCount}; requires {SystemHelperGrowthScope})");
            }
        }

        Require(
            violations.Count == 0,
            "Every baseline path remains governed after shrinking below 500 lines. Committed line/byte shrinkage " +
            "ratchets future ceilings and deletion retires the path. Violations:\n" +
            string.Join("\n", violations));
    }

#if UNITY_INCLUDE_TESTS && UNITY_EDITOR
    [Test]
#endif
    public void ApprovedExceptionsUseExactTrackedAuthorization()
    {
        BaselineManifest manifest = LoadManifest();
        Require(File.Exists(TrackerPath), $"Architecture/performance tracker is missing at `{TrackerPath}`.");
        string tracker = File.ReadAllText(TrackerPath);
        HashSet<string> activeOrCompletedTasks = ExtractIds(ActiveOrCompletedTrackerTaskRegex, tracker);
        Dictionary<string, string> detailedDecisionRows = ExtractDetailedDecisionRows(tracker);
        Dictionary<string, ProductionFileBaseline> productionBaselines = manifest.ProductionFilesOver500
            .ToDictionary(entry => entry.Path, entry => entry, PathIdentityComparer);
        Dictionary<string, SourceHistoryState> history = BuildSourceHistory(manifest);
        Dictionary<string, FrozenSystemHelperBaseline> frozenHelpers = manifest.FrozenSystemHelpers
            .ToDictionary(entry => entry.Path, entry => entry, PathIdentityComparer);
        Dictionary<string, SourceFile> currentSources = EnumerateFiles(ProductionRoot, "*.cs", includeEditorPaths: true)
            .ToDictionary(entry => entry.Path, entry => entry, PathIdentityComparer);
        var uniqueAuthorizations = new HashSet<string>(PathIdentityComparer);
        var uniqueDecisionAuthorizations = new HashSet<string>(StringComparer.Ordinal);

        foreach (ApprovedException exception in manifest.ApprovedExceptions)
        {
            Require(exception != null, "approvedExceptions cannot contain null entries.");
            RequireExactProjectSourcePath(exception.Path, "approved exception path");
            Require(
                uniqueAuthorizations.Add(exception.Path + "|" + exception.Scope),
                $"Duplicate approved exception path/scope: `{exception.Path}` / `{exception.Scope}`.");
            Require(
                TrackerTaskIdRegex.IsMatch(exception.TrackerTaskId ?? string.Empty),
                $"Exception for `{exception.Path}` requires one exact APH-nnn trackerTaskId.");
            Require(
                activeOrCompletedTasks.Contains(exception.TrackerTaskId),
                $"Exception for `{exception.Path}` cites `{exception.TrackerTaskId}`, which is not active [~] or completed [x].");
            Require(
                DecisionIdRegex.IsMatch(exception.DecisionId ?? string.Empty),
                $"Exception for `{exception.Path}` requires one exact D-nnn decisionId.");
            Require(
                uniqueDecisionAuthorizations.Add(exception.DecisionId),
                $"Decision `{exception.DecisionId}` must authorize exactly one source-growth exception.");
            Require(
                detailedDecisionRows.TryGetValue(exception.DecisionId, out string detailedDecisionRow),
                $"Exception for `{exception.Path}` cites `{exception.DecisionId}`, which has no unique detailed Decision Log row.");
            Require(exception.MaxLines > 0, $"Exception for `{exception.Path}` requires a positive maxLines ceiling.");
            Require(exception.MaxBytes > 0, $"Exception for `{exception.Path}` requires a positive maxBytes ceiling.");
            Require(currentSources.TryGetValue(exception.Path, out SourceFile current), $"Exception path does not exist: `{exception.Path}`.");
            RequireRepositorySpelling(exception.Path, current.Path, "approved exception");
            bool supersededByPostHardeningAuthorization =
                TryGetPostHardeningGrowthAuthorization(
                    exception.Path,
                    exception.Scope,
                    out PostHardeningGrowthAuthorization replacement) &&
                current.LineCount <= replacement.MaxLines &&
                current.ByteCount <= replacement.MaxBytes &&
                replacement.MaxLines >= exception.MaxLines &&
                replacement.MaxBytes >= exception.MaxBytes;
            Require(
                (current.LineCount <= exception.MaxLines && current.ByteCount <= exception.MaxBytes) ||
                supersededByPostHardeningAuthorization,
                $"Exception for `{exception.Path}` caps {exception.MaxLines} lines/{exception.MaxBytes} bytes but " +
                $"the source has {current.LineCount} lines/{current.ByteCount} bytes.");
            string decisionMarker = BuildExceptionDecisionMarker(exception);
            RequireSingleDecisionAuthorization(
                detailedDecisionRows,
                exception.DecisionId,
                detailedDecisionRow,
                decisionMarker);

            if (exception.Scope == SystemHelperScope)
            {
                Require(
                    exception.Path.EndsWith(SystemHelperSuffix, StringComparison.Ordinal),
                    $"`system-helper` exception path must end in {SystemHelperSuffix}: `{exception.Path}`.");
                Require(
                    !frozenHelpers.ContainsKey(exception.Path),
                    $"`{exception.Path}` is already frozen and does not need a system-helper exception.");
            }
            else if (exception.Scope == SystemHelperGrowthScope)
            {
                Require(
                    frozenHelpers.ContainsKey(exception.Path),
                    $"Helper growth exception path is not frozen: `{exception.Path}`.");
                SourceHistoryState state = history[exception.Path];
                Require(
                    current.LineCount > state.MinimumPositiveLines || current.ByteCount > state.MinimumPositiveBytes,
                    $"Helper growth exception for `{exception.Path}` is unused.");
            }
            else if (exception.Scope == ProductionReviewScope)
            {
                Require(
                    !HasPathSegment(exception.Path, EditorPathSegment),
                    $"`production-over-500-review` cannot authorize Editor source: `{exception.Path}`.");
                Require(
                    exception.MaxLines > ReviewThresholdLines,
                    $"Review exception for `{exception.Path}` must cap a size above 500 lines.");
                Require(
                    current.LineCount > ReviewThresholdLines || productionBaselines.ContainsKey(exception.Path),
                    $"Review exception for `{exception.Path}` is unused because the file is not above 500 lines.");
                if (productionBaselines.TryGetValue(exception.Path, out ProductionFileBaseline reviewedEntry))
                {
                    Require(
                        !reviewedEntry.StrictNoGrowth,
                        $"`{exception.Path}` requires `{StrictNoGrowthScope}`, not `{ProductionReviewScope}`.");
                    SourceHistoryState state = history[exception.Path];
                    bool supersededByPostHardeningRatchet =
                        TryGetPostHardeningGuardrail(exception.Path, out PostHardeningSourceGuardrail guardrail) &&
                        guardrail.MaxLines <= state.MinimumPositiveLines &&
                        guardrail.MaxBytes <= state.MinimumPositiveBytes;
                    Require(
                        current.LineCount > state.MinimumPositiveLines ||
                        current.ByteCount > state.MinimumPositiveBytes ||
                        supersededByPostHardeningRatchet,
                        $"Review exception for `{exception.Path}` is unused at " +
                        $"{current.LineCount}/{state.MinimumPositiveLines} lines and " +
                        $"{current.ByteCount}/{state.MinimumPositiveBytes} bytes.");
                }
            }
            else if (exception.Scope == StrictNoGrowthScope)
            {
                Require(
                    productionBaselines.TryGetValue(exception.Path, out ProductionFileBaseline entry) &&
                    entry.StrictNoGrowth,
                    $"Strict growth exception path is not one of the 27 frozen files: `{exception.Path}`.");
                Require(
                    exception.MaxLines > history[exception.Path].MinimumPositiveLines ||
                    exception.MaxBytes > history[exception.Path].MinimumPositiveBytes,
                    $"Strict growth exception for `{exception.Path}` must exceed an effective historical ceiling.");
                SourceHistoryState state = history[exception.Path];
                Require(
                    current.LineCount > state.MinimumPositiveLines || current.ByteCount > state.MinimumPositiveBytes,
                    $"Strict growth exception for `{exception.Path}` is unused.");
            }
            else if (exception.Scope == ProductionPathRecreationScope)
            {
                Require(
                    productionBaselines.ContainsKey(exception.Path),
                    $"Production recreation exception path is not a reviewed baseline: `{exception.Path}`.");
                Require(
                    history[exception.Path].WasRecreated,
                    $"Production recreation exception for `{exception.Path}` is unused because history has no recreation.");
            }
            else if (exception.Scope == SystemHelperRecreationScope)
            {
                Require(
                    frozenHelpers.ContainsKey(exception.Path),
                    $"Helper recreation exception path is not frozen: `{exception.Path}`.");
                Require(
                    history[exception.Path].WasRecreated,
                    $"Helper recreation exception for `{exception.Path}` is unused because history has no recreation.");
            }
            else
            {
                throw new InvalidDataException(
                    $"Exception for `{exception.Path}` has unsupported scope `{exception.Scope}`. " +
                    $"Allowed: {SystemHelperScope}, {SystemHelperGrowthScope}, {ProductionReviewScope}, {StrictNoGrowthScope}, " +
                    $"{ProductionPathRecreationScope}, {SystemHelperRecreationScope}.");
            }
        }
    }

#if UNITY_INCLUDE_TESTS && UNITY_EDITOR
    [Test]
#endif
    public void ExactDecisionAuthorizationRejectsMismatchedTuples()
    {
        var approved = new ApprovedException
        {
            Path = "Assets/Game/Scripts/ExampleSystemHelper.cs",
            Scope = SystemHelperScope,
            MaxLines = 640,
            MaxBytes = 64000,
            TrackerTaskId = "APH-999",
            DecisionId = "D-999"
        };
        string exactMarker = BuildExceptionDecisionMarker(approved);
        string detailedRow = $"| 2026-07-10 | `D-999` | Approve `{approved.Path}` | {exactMarker} | Focused evidence |";
        Dictionary<string, string> rows = ExtractDetailedDecisionRows(BuildCanonicalDecisionLog(detailedRow));
        Require(rows.TryGetValue("D-999", out string row), "The canonical detailed Decision Log row was not parsed.");
        Require(CountOccurrences(row, exactMarker) == 1, "The exact authorization tuple must match once.");

        var mismatches = new[]
        {
            new ApprovedException { Path = approved.Path + ".Other", Scope = approved.Scope, MaxLines = approved.MaxLines, MaxBytes = approved.MaxBytes, TrackerTaskId = approved.TrackerTaskId },
            new ApprovedException { Path = approved.Path, Scope = ProductionReviewScope, MaxLines = approved.MaxLines, MaxBytes = approved.MaxBytes, TrackerTaskId = approved.TrackerTaskId },
            new ApprovedException { Path = approved.Path, Scope = approved.Scope, MaxLines = approved.MaxLines + 1, MaxBytes = approved.MaxBytes, TrackerTaskId = approved.TrackerTaskId },
            new ApprovedException { Path = approved.Path, Scope = approved.Scope, MaxLines = approved.MaxLines, MaxBytes = approved.MaxBytes + 1, TrackerTaskId = approved.TrackerTaskId },
            new ApprovedException { Path = approved.Path, Scope = approved.Scope, MaxLines = approved.MaxLines, MaxBytes = approved.MaxBytes, TrackerTaskId = "APH-998" }
        };
        foreach (ApprovedException mismatch in mismatches)
        {
            Require(
                CountOccurrences(row, BuildExceptionDecisionMarker(mismatch)) == 0,
                "A mismatched path, scope, ceiling, or task must not inherit authorization.");
        }

        Require(
            ExtractDetailedDecisionRows(BuildCanonicalDecisionLog(detailedRow, $"Outside {exactMarker}"))["D-999"] == detailedRow,
            "A marker outside the canonical Decision Log section must be ignored.");

        string reusedRow = $"| 2026-07-10 | `D-998` | Duplicate | {exactMarker} | Evidence |";
        Dictionary<string, string> reusedRows = ExtractDetailedDecisionRows(BuildCanonicalDecisionLog(detailedRow + "\n" + reusedRow));
        ExpectInvalid(
            () => RequireSingleDecisionAuthorization(reusedRows, "D-999", reusedRows["D-999"], exactMarker),
            "exactly once");
        ExpectInvalid(
            () => ExtractDetailedDecisionRows(BuildCanonicalDecisionLog(
                "| 2026-07-10 | `D-997` | Decision | Reason | Evidence | Extra |")),
            "five columns");
        ExpectInvalid(
            () => ExtractDetailedDecisionRows(
                BuildCanonicalDecisionLog(detailedRow).Replace("## Decision Log", "### Decision Log")),
            "canonical Decision Log");
    }

#if UNITY_INCLUDE_TESTS && UNITY_EDITOR
    [Test]
#endif
    public void HistoricalPolicyRejectsReservedGrowthAndRecreation()
    {
        var shrunk = new SourceHistoryState("Assets/Game/Scripts/Example.cs", 828, 82000);
        shrunk.ApplySnapshot(400, 40000);
        Require(shrunk.MinimumPositiveLines == 400, "A committed shrink below 500 must ratchet the future ceiling.");
        Require(450 > shrunk.MinimumPositiveLines, "Regrowth below 500 must not consume an old reservation.");

        var recreated = new SourceHistoryState("Assets/Game/Scripts/Example.cs", 828, 82000);
        recreated.RecordDeletion();
        recreated.RecordCreation();
        recreated.ApplySnapshot(640, 60000);
        Require(recreated.WasRecreated, "A same-path creation after deletion must be recognized as recreation.");
    }

#if UNITY_INCLUDE_TESTS && UNITY_EDITOR
    [Test]
#endif
    public void JenkinsEditModeGateFailsClosed()
    {
        const string jenkinsPath = "Jenkinsfile.groovy";
        Require(File.Exists(jenkinsPath), $"Jenkins pipeline is missing at `{jenkinsPath}`.");
        string jenkins = File.ReadAllText(jenkinsPath);
        string editModeStage = ExtractTextBetween(
            jenkins,
            "stage('Run Unity EditMode Smoke Tests')",
            "stage('Build Windows')");

        Require(
            editModeStage.Contains("$ErrorActionPreference = \"Stop\"", StringComparison.Ordinal),
            "The EditMode gate must use terminating PowerShell errors.");
        Require(
            editModeStage.Contains("throw \"[BuildGate] EditMode test results were not created.", StringComparison.Ordinal),
            "Missing EditMode results must throw and fail Jenkins.");
        Require(
            editModeStage.Contains("throw \"[BuildGate] EditMode tests failed with exit code", StringComparison.Ordinal),
            "A nonzero Unity EditMode exit code must throw and fail Jenkins.");
        Require(
            editModeStage.Contains("$editModeFailed -gt 0", StringComparison.Ordinal) &&
            editModeStage.Contains("$editModeResult -ne \"Passed\"", StringComparison.Ordinal) &&
            editModeStage.Contains("HasAttribute($requiredAttribute)", StringComparison.Ordinal) &&
            editModeStage.Contains("$editModeTotal -le 0", StringComparison.Ordinal) &&
            editModeStage.Contains("$editModeAccounted -ne $editModeTotal", StringComparison.Ordinal) &&
            editModeStage.Contains("$editModeSerializedCases -ne $editModeTestCaseCount", StringComparison.Ordinal),
            "The EditMode XML attributes, nonzero discovery, result, and completeness must be checked explicitly.");
        Require(
            !Regex.IsMatch(
                editModeStage,
                @"EditMode tests FAILED[^\r\n]*allowed to continue|Continuing build and deployment",
                RegexOptions.CultureInvariant | RegexOptions.IgnoreCase),
            "The EditMode gate must not allow a failed result to continue the build.");
    }

#if UNITY_INCLUDE_TESTS && UNITY_EDITOR
    [Test]
#endif
    public void JenkinsCheckoutProvidesGuardInputsAndHistory()
    {
        string jenkins = File.ReadAllText("Jenkinsfile.groovy");
        string checkout = ExtractTextBetween(jenkins, "stage('Checkout Unity Project')", "stage('Resolve Unity Editor')");
        Require(!checkout.Contains("--depth 1", StringComparison.Ordinal), "Jenkins checkout must not truncate baseline history.");
        Require(
            !checkout.Contains("--filter=blob:none", StringComparison.Ordinal),
            "Jenkins must clone baseline blobs while repository credentials are available.");
        Require(
            checkout.Contains("git sparse-checkout set Assets Packages ProjectSettings Tools Design", StringComparison.Ordinal),
            "Jenkins sparse checkout must include Design.");
        Require(
            checkout.Contains($"git cat-file -e {BaselineCommit}", StringComparison.Ordinal) &&
            checkout.Contains($"git merge-base --is-ancestor {BaselineCommit} HEAD", StringComparison.Ordinal),
            "Jenkins must verify the immutable baseline commit and ancestry.");
        string normalizedCheckout = checkout.Replace("\\\\", "\\");
        Require(
            normalizedCheckout.Contains(BaselinePath.Replace('/', '\\'), StringComparison.Ordinal) &&
            normalizedCheckout.Contains(TrackerPath.Replace('/', '\\'), StringComparison.Ordinal),
            "Jenkins must verify both APH-701/702 Design inputs exist.");

        RequireStageRunsUnconditionally(jenkins, "stage('Resolve Unity Editor')", "stage('Run Unity EditMode Smoke Tests')");
        RequireStageRunsUnconditionally(jenkins, "stage('Run Unity EditMode Smoke Tests')", "stage('Build Windows')");
    }

#if UNITY_INCLUDE_TESTS && UNITY_EDITOR
    [Test]
#endif
    public void JenkinsResultContractRejectsIncompleteRuns()
    {
        const string valid = "<test-run testcasecount=\"2\" result=\"Passed\" total=\"2\" passed=\"2\" " +
                             "failed=\"0\" inconclusive=\"0\" skipped=\"0\" start-time=\"a\" end-time=\"b\">" +
                             "<test-suite><test-case/><test-case/></test-suite></test-run>";
        ValidateNUnitTestRunContract(valid);
        ExpectInvalid(() => ValidateNUnitTestRunContract(valid.Replace(" failed=\"0\"", string.Empty)), "failed");
        ExpectInvalid(() => ValidateNUnitTestRunContract(valid.Replace("total=\"2\"", "total=\"0\"")), "at least one");
        ExpectInvalid(() => ValidateNUnitTestRunContract(valid.Replace("passed=\"2\"", "passed=\"1\"")), "incomplete");
        ExpectInvalid(() => ValidateNUnitTestRunContract(valid.Replace(" end-time=\"b\"", string.Empty)), "end-time");
        ExpectInvalid(() => ValidateNUnitTestRunContract(valid.Replace("<test-case/><test-case/>", "<test-case/>")), "serialized");
        ExpectInvalid(() => ValidateNUnitTestRunContract("<test-run"), "XML");
    }

#if UNITY_INCLUDE_TESTS && UNITY_EDITOR
    [Test]
#endif
    public void BinaryNumstatRecordsFailClosed()
    {
        ValidateNumstatRecords("__COMMIT__abc\n1\t2\tAssets/Game/Scripts/Example.cs\n");
        ExpectInvalid(
            () => ValidateNumstatRecords("__COMMIT__abc\n-\t-\tAssets/Game/Scripts/Binary.cs\n"),
            "binary");
        ExpectInvalid(() => ValidateNumstatRecords("__COMMIT__abc\nmalformed\n"), "Malformed");
    }

#if UNITY_INCLUDE_TESTS && UNITY_EDITOR
    [Test]
#endif
    public void DuplicateJsonPropertiesFailClosedAtEveryLevel()
    {
        RejectDuplicateJsonProperties("{\"schemaVersion\":3}");
        ExpectInvalid(() => RejectDuplicateJsonProperties("{\"schemaVersion\":3,\"schemaVersion\":3}"), "Duplicate JSON property");
        ExpectInvalid(() => RejectDuplicateJsonProperties("{\"entry\":{\"path\":\"a\",\"path\":\"b\"}}"), "Duplicate JSON property");
        ExpectInvalid(() => RejectDuplicateJsonProperties("{\"items\":[{\"scope\":\"a\",\"scope\":\"b\"}]}"), "Duplicate JSON property");
    }

#if UNITY_INCLUDE_TESTS && UNITY_EDITOR
    [Test]
#endif
    public void WindowsPathIdentityIsCaseInsensitiveButSpellingStable()
    {
        var identities = new HashSet<string>(PathIdentityComparer) { "Assets/Game/Scripts/Example.cs" };
        Require(!identities.Add("assets/game/scripts/example.cs"), "Windows path identities must compare case-insensitively.");
        ExpectInvalid(
            () => RequireRepositorySpelling("Assets/Game/Scripts/Example.cs", "assets/game/scripts/example.cs", "test"),
            "spelling");
        RequireRepositorySpelling("Assets/Game/Scripts/Example.cs", "Assets/Game/Scripts/Example.cs", "test");
    }

#if UNITY_INCLUDE_TESTS && UNITY_EDITOR
    [Test]
#endif
    public void ByteCeilingRejectsPhysicalLineMinificationBypass()
    {
        var state = new SourceHistoryState("Assets/Game/Scripts/Example.cs", 800, 80000);
        state.ApplySnapshot(400, 40000);
        Require(
            ExceedsHistoricalCeiling(new SourceFile(state.RepositoryPath, 350, 45000), state),
            "Fewer physical lines must not bypass a ratcheted byte ceiling.");
    }

#if UNITY_INCLUDE_TESTS && UNITY_EDITOR
    [Test]
#endif
    public void PostHardeningGuardrailContractHasExpectedRatchets()
    {
        PostHardeningGuardrailContract contract = LoadPostHardeningGuardrails();
        Require(contract.SchemaVersion == 1, "The post-hardening guardrail schemaVersion must remain 1.");
        Require(
            contract.ContractId == PostHardeningGuardrailContractId,
            $"The post-hardening guardrail contractId must remain `{PostHardeningGuardrailContractId}`.");
        Require(contract.Entries != null, "The post-hardening guardrail entries are required.");
        Require(contract.ReplacementOwnerBoundary != null, "The replacement-owner boundary is required.");
        Require(contract.GrowthAuthorizations != null, "Post-hardening growth authorizations are required.");
        Require(
            contract.GrowthAuthorizations.Count == 6,
            "Exactly six accepted post-hardening growth authorizations are required.");
        Require(
            contract.Entries.Count == PostHardeningGuardrailEntryCount,
            $"The post-hardening guardrail must contain exactly {PostHardeningGuardrailEntryCount} entries.");
        ValidateExpectedReplacementOwnerBoundary(contract.ReplacementOwnerBoundary);

        List<PostHardeningSourceGuardrail> expected = CreateExpectedPostHardeningGuardrails();
        for (int index = 0; index < expected.Count; index++)
        {
            PostHardeningSourceGuardrail actual = contract.Entries[index];
            PostHardeningSourceGuardrail frozen = expected[index];
            Require(actual != null, "Post-hardening guardrail entries cannot be null.");
            RequireExactProjectSourcePath(actual.Path, "post-hardening guardrail path");
            Require(actual.Path == frozen.Path, $"Unexpected post-hardening guardrail path `{actual.Path}`.");
            Require(actual.SourceSha256 == frozen.SourceSha256, $"`{actual.Path}` sourceSha256 changed.");
            Require(actual.MaxLines == frozen.MaxLines, $"`{actual.Path}` maxLines must remain {frozen.MaxLines}.");
            Require(actual.MaxBytes == frozen.MaxBytes, $"`{actual.Path}` maxBytes must remain {frozen.MaxBytes}.");
            Require(
                actual.MaxResponsibilityDomainSymbolOccurrences == frozen.MaxResponsibilityDomainSymbolOccurrences,
                $"`{actual.Path}` maxResponsibilityDomainSymbolOccurrences must remain " +
                $"{frozen.MaxResponsibilityDomainSymbolOccurrences}.");
            Require(actual.MaxStateSlots == frozen.MaxStateSlots, $"`{actual.Path}` maxStateSlots must remain {frozen.MaxStateSlots}.");
            RequireExactSequence(actual.Responsibilities, frozen.Responsibilities, actual.Path, "responsibilities");
            RequireExactSequence(actual.RequiredSymbols, frozen.RequiredSymbols, actual.Path, "requiredSymbols");
            RequireExactSequence(actual.ForbiddenSymbols, frozen.ForbiddenSymbols, actual.Path, "forbiddenSymbols");
            RequireExactSequence(
                actual.ResponsibilitySignatureSymbols,
                frozen.ResponsibilitySignatureSymbols,
                actual.Path,
                "responsibilitySignatureSymbols");
            Require(
                actual.ResponsibilitySignatureMatchThreshold == frozen.ResponsibilitySignatureMatchThreshold,
                $"`{actual.Path}` responsibilitySignatureMatchThreshold must remain " +
                $"{frozen.ResponsibilitySignatureMatchThreshold}.");
        }

        ValidateExpectedPostHardeningGrowthAuthorizations(contract.GrowthAuthorizations);

        BaselineManifest legacy = LoadManifest();
        ApprovedException superseded = legacy.ApprovedExceptions.Single(exception =>
            exception.Path == "Assets/Game/Scripts/UI/Shell/UIShellContentView.cs" &&
            exception.Scope == ProductionReviewScope);
        PostHardeningSourceGuardrail shellGuardrail = contract.Entries.Single(entry => entry.Path == superseded.Path);
        Require(
            shellGuardrail.MaxLines < superseded.MaxLines && shellGuardrail.MaxBytes < superseded.MaxBytes,
            "The AM-016 shell ratchet must remain stricter than the retained historical authorization.");
    }

#if UNITY_INCLUDE_TESTS && UNITY_EDITOR
    [Test]
#endif
    public void PostHardeningGuardedSourcesStayBoundedAndNarrow()
    {
        PostHardeningGuardrailContract contract = LoadPostHardeningGuardrails();
        List<SourceFile> productionSources = EnumerateFiles(ProductionRoot, "*.cs", includeEditorPaths: false);
        var violations = new List<string>();

        foreach (PostHardeningSourceGuardrail guardrail in contract.Entries)
        {
            if (!File.Exists(guardrail.Path))
            {
                violations.Add($"missing guarded source {guardrail.Path}");
                continue;
            }

            SourceFile source = MeasureFile(guardrail.Path);
            string sourceSha256 = ComputeFileSha256(guardrail.Path);
            if (!string.Equals(sourceSha256, guardrail.SourceSha256, StringComparison.Ordinal))
            {
                violations.Add(
                    $"guarded source identity changed {guardrail.Path} " +
                    $"({sourceSha256} != {guardrail.SourceSha256})");
            }
            if (source.LineCount > guardrail.MaxLines || source.ByteCount > guardrail.MaxBytes)
            {
                violations.Add(
                    $"regrew {guardrail.Path} (lines {source.LineCount}/{guardrail.MaxLines}, " +
                    $"bytes {source.ByteCount}/{guardrail.MaxBytes})");
            }

            string content = File.ReadAllText(guardrail.Path);
            int domainSymbolOccurrences = CountOccurrences(
                content,
                contract.ReplacementOwnerBoundary.DomainSymbol);
            if (domainSymbolOccurrences > guardrail.MaxResponsibilityDomainSymbolOccurrences)
            {
                violations.Add(
                    $"{guardrail.Path} grew its `{contract.ReplacementOwnerBoundary.DomainSymbol}` responsibility " +
                    $"surface ({domainSymbolOccurrences}/{guardrail.MaxResponsibilityDomainSymbolOccurrences})");
            }
            foreach (string required in guardrail.RequiredSymbols)
            {
                if (!content.Contains(required, StringComparison.Ordinal))
                    violations.Add($"{guardrail.Path} lost required responsibility symbol `{required}`");
            }

            foreach (string forbidden in guardrail.ForbiddenSymbols)
            {
                if (content.Contains(forbidden, StringComparison.Ordinal))
                    violations.Add($"{guardrail.Path} regained forbidden responsibility symbol `{forbidden}`");
            }

            if (guardrail.ResponsibilitySignatureMatchThreshold <= 0)
                continue;

            int ownerMatches = CountContainedSymbols(content, guardrail.ResponsibilitySignatureSymbols);
            if (ownerMatches < guardrail.ResponsibilitySignatureMatchThreshold)
            {
                violations.Add(
                    $"{guardrail.Path} no longer owns its declared responsibility signature " +
                    $"({ownerMatches}/{guardrail.ResponsibilitySignatureMatchThreshold})");
            }

            foreach (SourceFile candidate in productionSources)
            {
                if (PathIdentityComparer.Equals(candidate.Path, guardrail.Path))
                    continue;

                string candidateContent = File.ReadAllText(candidate.Path);
                int matches = CountContainedSymbols(candidateContent, guardrail.ResponsibilitySignatureSymbols);
                if (matches >= guardrail.ResponsibilitySignatureMatchThreshold)
                {
                    violations.Add(
                        $"equivalent responsibility owner {candidate.Path} matches {matches}/" +
                        $"{guardrail.ResponsibilitySignatureSymbols.Count} symbols governed by {guardrail.Path}");
                }
            }
        }

        HashSet<string> allowedReplacementOwners = new(
            contract.ReplacementOwnerBoundary.AllowedOwnerPaths,
            PathIdentityComparer);
        string baselineTree = RunGit(
            $"-c core.quotepath=false ls-tree -r --name-only {contract.ReplacementOwnerBoundary.BaselineCommit} -- " +
            contract.ReplacementOwnerBoundary.Root,
            allowEmptyOutput: false);
        HashSet<string> baselinePaths = SplitRawLines(baselineTree)
            .Select(NormalizePath)
            .ToHashSet(PathIdentityComparer);
        string changedProduction = RunGit(
            $"-c core.quotepath=false diff --name-only {contract.ReplacementOwnerBoundary.BaselineCommit} -- " +
            contract.ReplacementOwnerBoundary.Root,
            allowEmptyOutput: true);
        HashSet<string> changedProductionPaths = SplitRawLines(changedProduction)
            .Select(NormalizePath)
            .ToHashSet(PathIdentityComparer);
        foreach (SourceFile candidate in productionSources.Where(source =>
            source.Path.StartsWith(contract.ReplacementOwnerBoundary.Root + "/", StringComparison.Ordinal)))
        {
            bool existedAtBaseline = baselinePaths.Contains(candidate.Path);
            if (existedAtBaseline && !changedProductionPaths.Contains(candidate.Path))
                continue;

            string candidateContent = File.ReadAllText(candidate.Path);
            int currentLifecycleMatches = CountContainedSymbols(
                candidateContent,
                contract.ReplacementOwnerBoundary.ManagedLifecycleSymbols);
            int currentLifecycleOccurrences = CountTotalOccurrences(
                candidateContent,
                contract.ReplacementOwnerBoundary.ManagedLifecycleSymbols);
            int currentGenericLifecycleMatches = CountContainedSymbols(
                candidateContent,
                contract.ReplacementOwnerBoundary.GenericLifecycleAnchorSymbols);
            int currentGenericLifecycleOccurrences = CountTotalOccurrences(
                candidateContent,
                contract.ReplacementOwnerBoundary.GenericLifecycleAnchorSymbols);
            int currentDomainOccurrences = CountOccurrences(
                candidateContent,
                contract.ReplacementOwnerBoundary.DomainSymbol);
            bool currentDomainOwner =
                candidateContent.Contains(contract.ReplacementOwnerBoundary.DomainSymbol, StringComparison.Ordinal) &&
                currentLifecycleMatches >= contract.ReplacementOwnerBoundary.ManagedLifecycleMatchThreshold;
            bool currentGenericOwner =
                currentGenericLifecycleMatches >= contract.ReplacementOwnerBoundary.GenericLifecycleMatchThreshold;
            if ((!currentDomainOwner && !currentGenericOwner) || allowedReplacementOwners.Contains(candidate.Path))
                continue;

            int baselineLifecycleMatches = 0;
            int baselineLifecycleOccurrences = 0;
            int baselineGenericLifecycleMatches = 0;
            int baselineGenericLifecycleOccurrences = 0;
            int baselineDomainOccurrences = 0;
            bool baselineDomainOwner = false;
            bool baselineGenericOwner = false;
            SourceFile baselineSource = new(candidate.Path, 0, 0);
            if (existedAtBaseline)
            {
                byte[] baselineBytes = RunGitBytes(
                    $"show {contract.ReplacementOwnerBoundary.BaselineCommit}:{candidate.Path}");
                string baselineContent = Encoding.UTF8.GetString(baselineBytes);
                baselineSource = MeasureContent(candidate.Path, baselineBytes);
                baselineLifecycleMatches = CountContainedSymbols(
                    baselineContent,
                    contract.ReplacementOwnerBoundary.ManagedLifecycleSymbols);
                baselineLifecycleOccurrences = CountTotalOccurrences(
                    baselineContent,
                    contract.ReplacementOwnerBoundary.ManagedLifecycleSymbols);
                baselineGenericLifecycleMatches = CountContainedSymbols(
                    baselineContent,
                    contract.ReplacementOwnerBoundary.GenericLifecycleAnchorSymbols);
                baselineGenericLifecycleOccurrences = CountTotalOccurrences(
                    baselineContent,
                    contract.ReplacementOwnerBoundary.GenericLifecycleAnchorSymbols);
                baselineDomainOccurrences = CountOccurrences(
                    baselineContent,
                    contract.ReplacementOwnerBoundary.DomainSymbol);
                baselineDomainOwner =
                    baselineContent.Contains(
                        contract.ReplacementOwnerBoundary.DomainSymbol,
                        StringComparison.Ordinal) &&
                    baselineLifecycleMatches >= contract.ReplacementOwnerBoundary.ManagedLifecycleMatchThreshold;
                baselineGenericOwner =
                    baselineGenericLifecycleMatches >= contract.ReplacementOwnerBoundary.GenericLifecycleMatchThreshold;
            }

            bool grewDomainOwnership = currentDomainOwner &&
                (!baselineDomainOwner ||
                 currentLifecycleMatches > baselineLifecycleMatches ||
                 currentDomainOccurrences > baselineDomainOccurrences);
            bool grewGenericOwnership = currentGenericOwner &&
                (!baselineGenericOwner ||
                 currentGenericLifecycleMatches > baselineGenericLifecycleMatches ||
                 currentGenericLifecycleOccurrences > baselineGenericLifecycleOccurrences ||
                 candidate.LineCount > baselineSource.LineCount ||
                 candidate.ByteCount > baselineSource.ByteCount);
            if (grewDomainOwnership || grewGenericOwnership)
            {
                violations.Add(
                    $"replacement owner {candidate.Path} grew managed lifecycle ownership " +
                    $"(symbols {baselineLifecycleMatches}->{currentLifecycleMatches}, " +
                    $"occurrences {baselineLifecycleOccurrences}->{currentLifecycleOccurrences}, " +
                    $"genericSymbols {baselineGenericLifecycleMatches}->{currentGenericLifecycleMatches}, " +
                    $"genericOccurrences {baselineGenericLifecycleOccurrences}->{currentGenericLifecycleOccurrences}, " +
                    $"domain {baselineDomainOccurrences}->{currentDomainOccurrences}, " +
                    $"lines {baselineSource.LineCount}->{candidate.LineCount}, " +
                    $"bytes {baselineSource.ByteCount}->{candidate.ByteCount})");
            }
        }

        Require(
            violations.Count == 0,
            "Post-hardening source and responsibility ratchets failed:\n" + string.Join("\n", violations));
    }

#if PRODUCTION_SOURCE_GROWTH_STANDALONE
    public static int Main(string[] args)
    {
        try
        {
            if (args.Length == 1)
                Directory.SetCurrentDirectory(Path.GetFullPath(args[0]));
            else if (args.Length != 0)
                throw new ArgumentException("Expected zero arguments or one project-root path.");

            var tests = new ProductionSourceGrowthArchitectureTests();
            tests.BaselineManifestHasExpectedFrozenInventories();
            tests.NewSystemHelperPathsRequireApprovedException();
            tests.ProductionFilesAboveFiveHundredLinesRequireReview();
            tests.ExistingFilesAboveOneThousandLinesDoNotGrow();
            tests.AllBaselinedPathsRespectRatchetedLineAndByteCeilings();
            tests.ApprovedExceptionsUseExactTrackedAuthorization();
            tests.ExactDecisionAuthorizationRejectsMismatchedTuples();
            tests.HistoricalPolicyRejectsReservedGrowthAndRecreation();
            tests.JenkinsEditModeGateFailsClosed();
            tests.JenkinsCheckoutProvidesGuardInputsAndHistory();
            tests.JenkinsResultContractRejectsIncompleteRuns();
            tests.BinaryNumstatRecordsFailClosed();
            tests.DuplicateJsonPropertiesFailClosedAtEveryLevel();
            tests.WindowsPathIdentityIsCaseInsensitiveButSpellingStable();
            tests.ByteCeilingRejectsPhysicalLineMinificationBypass();
            tests.PostHardeningGuardrailContractHasExpectedRatchets();
            tests.PostHardeningGuardedSourcesStayBoundedAndNarrow();
            Console.WriteLine(FocusedRunnerMarker);
            return 0;
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine(exception);
            Console.Error.WriteLine("[ProductionSourceGrowthArchitectureValidation] result=Failed");
            return 1;
        }
    }
#endif

    private static BaselineManifest LoadManifest()
    {
        string json = ReadManifestJson();
        ValidateJsonShape(json);
        BaselineManifest manifest;
        try
        {
            manifest = JsonSerializer.Deserialize<BaselineManifest>(json);
        }
        catch (JsonException exception)
        {
            throw new InvalidDataException($"Invalid JSON in `{BaselinePath}`.", exception);
        }

        Require(manifest != null, $"`{BaselinePath}` deserialized to null.");
        return manifest;
    }

    private static PostHardeningGuardrailContract LoadPostHardeningGuardrails()
    {
        Require(
            File.Exists(PostHardeningGuardrailPath),
            $"Post-hardening source responsibility guardrails are missing at `{PostHardeningGuardrailPath}`.");
        string json = File.ReadAllText(PostHardeningGuardrailPath);
        RejectDuplicateJsonProperties(json);
        ValidatePostHardeningGuardrailJsonShape(json);
        try
        {
            PostHardeningGuardrailContract contract = JsonSerializer.Deserialize<PostHardeningGuardrailContract>(json);
            Require(contract != null, $"`{PostHardeningGuardrailPath}` deserialized to null.");
            return contract;
        }
        catch (JsonException exception)
        {
            throw new InvalidDataException($"Invalid JSON in `{PostHardeningGuardrailPath}`.", exception);
        }
    }

    private static void ValidatePostHardeningGuardrailJsonShape(string json)
    {
        using JsonDocument document = JsonDocument.Parse(json);
        JsonElement root = document.RootElement;
        Require(root.ValueKind == JsonValueKind.Object, "The post-hardening guardrail root must be an object.");
        RequireExactProperties(
            root,
            "schemaVersion",
            "contractId",
            "replacementOwnerBoundary",
            "growthAuthorizations",
            "entries");
        JsonElement boundary = root.GetProperty("replacementOwnerBoundary");
        Require(boundary.ValueKind == JsonValueKind.Object, "replacementOwnerBoundary must be an object.");
        RequireExactProperties(
            boundary,
            "root",
            "baselineCommit",
            "domainSymbol",
            "managedLifecycleSymbols",
            "managedLifecycleMatchThreshold",
            "genericLifecycleMatchThreshold",
            "genericLifecycleAnchorSymbols",
            "allowedOwnerPaths");
        JsonElement growthAuthorizations = root.GetProperty("growthAuthorizations");
        Require(
            growthAuthorizations.ValueKind == JsonValueKind.Array,
            "Post-hardening growthAuthorizations must be an array.");
        foreach (JsonElement authorization in growthAuthorizations.EnumerateArray())
        {
            Require(
                authorization.ValueKind == JsonValueKind.Object,
                "Every post-hardening growth authorization must be an object.");
            RequireExactProperties(
                authorization,
                "path",
                "trackerTaskId",
                "acceptedCommit",
                "maxLines",
                "maxBytes",
                "scope");
        }
        JsonElement entries = root.GetProperty("entries");
        Require(entries.ValueKind == JsonValueKind.Array, "Post-hardening guardrail entries must be an array.");
        foreach (JsonElement entry in entries.EnumerateArray())
        {
            Require(entry.ValueKind == JsonValueKind.Object, "Every post-hardening guardrail entry must be an object.");
            RequireExactProperties(
                entry,
                "path",
                "sourceSha256",
                "maxLines",
                "maxBytes",
                "maxResponsibilityDomainSymbolOccurrences",
                "maxStateSlots",
                "responsibilities",
                "requiredSymbols",
                "forbiddenSymbols",
                "responsibilitySignatureSymbols",
                "responsibilitySignatureMatchThreshold");
        }
    }

    private static List<PostHardeningSourceGuardrail> CreateExpectedPostHardeningGuardrails()
    {
        return new List<PostHardeningSourceGuardrail>
        {
            new()
            {
                Path = "Assets/Game/Scripts/UI/Shell/ResourceExchangeShellBinding.cs",
                SourceSha256 = "640758d7b1562455285ee8da14b8da38fd9c31102397e1267eafa90328d7ee07",
                MaxLines = 94,
                MaxBytes = 3160,
                MaxResponsibilityDomainSymbolOccurrences = 10,
                MaxStateSlots = 4,
                Responsibilities = new List<string>
                {
                    "resource-exchange-popup-instance",
                    "resource-exchange-popup-view-binding",
                    "resource-exchange-close-listener-lifecycle",
                    "resource-exchange-region-reset"
                },
                RequiredSymbols = new List<string>
                {
                    "internal sealed class ResourceExchangeShellBinding",
                    "private GameObject _instance;",
                    "private ResourceExchangePopupView _view;",
                    "private Button _closeButton;",
                    "private UnityAction _closeListener;",
                    "public GameObject Install(",
                    "public void Close(",
                    "public void ResetForRegionClear(",
                    "public void RebindMainMenuPlayUi("
                },
                ForbiddenSymbols = new List<string>
                {
                    "UiShellRuntimeGateway",
                    "UiActionKind",
                    "SetPopupRegion",
                    "InstallFullMap",
                    "InstallSettings",
                    "BuildDrawer",
                    "SystemBase",
                    "ISystem",
                    "World.DefaultGameObjectInjectionWorld",
                    "ServiceLocator"
                },
                ResponsibilitySignatureSymbols = new List<string>
                {
                    "ResourceExchangePopupView",
                    "BindResourceExchangePopup",
                    "UIPopupMotionView",
                    "UIShellRegionId.PopupLayer",
                    "DestroyRegionObject"
                },
                ResponsibilitySignatureMatchThreshold = 4
            },
            new()
            {
                Path = "Assets/Game/Scripts/UI/Shell/UIShellContentView.cs",
                SourceSha256 = "a729b621b4ae140807469c336e6277c6cfaaa81aac5dfe2fcd3c9b3e97d8d7a9",
                MaxLines = 898,
                MaxBytes = 38807,
                MaxResponsibilityDomainSymbolOccurrences = 11,
                MaxStateSlots = 49,
                Responsibilities = new List<string>
                {
                    "ui-route-interpretation",
                    "typed-ui-action-enqueue",
                    "main-menu-play-ui-authority",
                    "popup-region-mutation",
                    "content-versioning"
                },
                RequiredSymbols = new List<string>
                {
                    "private readonly ResourceExchangeShellBinding _resourceExchangeShellBinding = new();",
                    "_resourceExchangeShellBinding.Install(",
                    "_resourceExchangeShellBinding.Close(",
                    "_resourceExchangeShellBinding.ResetForRegionClear(",
                    "_resourceExchangeShellBinding.RebindMainMenuPlayUi(",
                    "UiShellRuntimeGateway.TryEnqueueUiAction(UiActionKind.CloseResourceExchange, 0);"
                },
                ForbiddenSymbols = new List<string>
                {
                    "_resourceExchangePopupInstance",
                    "_resourceExchangePopupView",
                    "_resourceExchangePopupCloseButton",
                    "_resourceExchangePopupCloseButtonListener",
                    "ResourceExchangePopupView",
                    "BindResourceExchangePopup",
                    "MatchHudLargeTacticalPopup.ResourceExchange"
                },
                ResponsibilitySignatureSymbols = new List<string>(),
                ResponsibilitySignatureMatchThreshold = 0
            }
        };
    }

    private static void RequireExactSequence(
        IReadOnlyList<string> actual,
        IReadOnlyList<string> expected,
        string path,
        string fieldName)
    {
        Require(actual != null, $"`{path}` {fieldName} is required.");
        Require(
            actual.SequenceEqual(expected, StringComparer.Ordinal),
            $"`{path}` {fieldName} must remain the frozen ordered contract.");
    }

    private static void ValidateExpectedReplacementOwnerBoundary(
        PostHardeningReplacementOwnerBoundary boundary)
    {
        Require(boundary.Root == "Assets/Game/Scripts", "Replacement-owner root changed.");
        Require(
            boundary.BaselineCommit == "5f24549a18199db04921bb0ad6adf075872b03ff",
            "Replacement-owner baseline commit changed.");
        Require(boundary.DomainSymbol == "ResourceExchange", "Replacement-owner domain symbol changed.");
        Require(
            boundary.ManagedLifecycleMatchThreshold == 3,
            "Replacement-owner managed lifecycle threshold must remain 3.");
        Require(
            boundary.GenericLifecycleMatchThreshold == 3,
            "Replacement-owner generic lifecycle threshold must remain 3.");
        RequireExactSequence(
            boundary.ManagedLifecycleSymbols,
            new[]
            {
                "using UnityEngine;",
                "GameObject",
                "UnityEngine.UI",
                "Button",
                "UnityAction",
                "UIPopupMotionView",
                "UIShellRegionId.PopupLayer",
                "DestroyRegionObject",
                "InstallRoot(",
                "BindResourceExchangePopup"
            },
            boundary.Root,
            "managedLifecycleSymbols");
        RequireExactSequence(
            boundary.GenericLifecycleAnchorSymbols,
            new[]
            {
                "UnityAction",
                "UIPopupMotionView",
                "UIShellRegionId.PopupLayer",
                "DestroyRegionObject",
                "InstallRoot(",
                "BindResourceExchangePopup"
            },
            boundary.Root,
            "genericLifecycleAnchorSymbols");
        RequireExactSequence(
            boundary.AllowedOwnerPaths,
            Array.Empty<string>(),
            boundary.Root,
            "allowedOwnerPaths");
    }

    private static void ValidateExpectedPostHardeningGrowthAuthorizations(
        IReadOnlyList<PostHardeningGrowthAuthorization> authorizations)
    {
        Require(File.Exists(PostHardeningTrackerPath), $"Missing `{PostHardeningTrackerPath}`.");
        Require(File.Exists(FeatureReadinessTrackerPath), $"Missing `{FeatureReadinessTrackerPath}`.");
        string postHardeningTracker = File.ReadAllText(PostHardeningTrackerPath);
        string featureReadinessTracker = File.ReadAllText(FeatureReadinessTrackerPath);
        var expected = new[]
        {
            new PostHardeningGrowthAuthorization
            {
                Path = "Assets/Game/Scripts/Composition/OperationMapSceneLoadingSceneSystemHelper.cs",
                TrackerTaskId = "AMFR-004",
                AcceptedCommit = "e92f16815ff871e8ba0a04481a8e2abd28551d2b",
                MaxLines = 465,
                MaxBytes = 16734,
                Scope = SystemHelperScope
            },
            new PostHardeningGrowthAuthorization
            {
                Path = "Assets/Game/Scripts/Composition/OperationMapSceneReferenceSceneSystemHelper.cs",
                TrackerTaskId = "AMFR-007",
                AcceptedCommit = "18ebc842961b31e33239f9c510c613a25902b89e",
                MaxLines = 67,
                MaxBytes = 2223,
                Scope = SystemHelperScope
            },
            new PostHardeningGrowthAuthorization
            {
                Path = "Assets/Game/Scripts/Systems/BuildingResourceHaulerBridgeCompositionSystemHelper.cs",
                TrackerTaskId = "AM-013",
                AcceptedCommit = "664ae7fa4544699faad7da01b11db60434e39088",
                MaxLines = 1574,
                MaxBytes = 73364,
                Scope = StrictNoGrowthScope
            },
            new PostHardeningGrowthAuthorization
            {
                Path = "Assets/Game/Scripts/Systems/BuildingResourceHaulerBridgeCompositionSystemHelper.cs",
                TrackerTaskId = "AM-013",
                AcceptedCommit = "664ae7fa4544699faad7da01b11db60434e39088",
                MaxLines = 1574,
                MaxBytes = 73364,
                Scope = SystemHelperGrowthScope
            },
            new PostHardeningGrowthAuthorization
            {
                Path = "Assets/Game/Scripts/Systems/FactionResourceCompositionSystemHelper.cs",
                TrackerTaskId = "AM-013",
                AcceptedCommit = "664ae7fa4544699faad7da01b11db60434e39088",
                MaxLines = 865,
                MaxBytes = 34274,
                Scope = ProductionReviewScope
            },
            new PostHardeningGrowthAuthorization
            {
                Path = "Assets/Game/Scripts/Systems/FactionResourceCompositionSystemHelper.cs",
                TrackerTaskId = "AM-013",
                AcceptedCommit = "664ae7fa4544699faad7da01b11db60434e39088",
                MaxLines = 865,
                MaxBytes = 34274,
                Scope = SystemHelperGrowthScope
            }
        };

        for (int index = 0; index < expected.Length; index++)
        {
            PostHardeningGrowthAuthorization actual = authorizations[index];
            PostHardeningGrowthAuthorization frozen = expected[index];
            Require(actual != null, "Post-hardening growth authorizations cannot contain null entries.");
            RequireExactProjectSourcePath(actual.Path, "post-hardening growth authorization path");
            Require(actual.Path == frozen.Path, $"Unexpected post-hardening growth path `{actual.Path}`.");
            Require(
                actual.TrackerTaskId == frozen.TrackerTaskId,
                $"`{actual.Path}` must remain bound to `{frozen.TrackerTaskId}`.");
            Require(actual.AcceptedCommit == frozen.AcceptedCommit, $"`{actual.Path}` acceptedCommit changed.");
            Require(actual.MaxLines == frozen.MaxLines, $"`{actual.Path}` maxLines must remain {frozen.MaxLines}.");
            Require(actual.MaxBytes == frozen.MaxBytes, $"`{actual.Path}` maxBytes must remain {frozen.MaxBytes}.");
            Require(actual.Scope == frozen.Scope, $"`{actual.Path}` scope must remain `{frozen.Scope}`.");
            string tracker = actual.TrackerTaskId.StartsWith("AMFR-", StringComparison.Ordinal)
                ? featureReadinessTracker
                : postHardeningTracker;
            Require(
                tracker.Contains($"- [x] `{actual.TrackerTaskId}`", StringComparison.Ordinal),
                $"`{actual.TrackerTaskId}` is not complete in its owning tracker.");
            Require(
                tracker.Contains(actual.AcceptedCommit, StringComparison.Ordinal),
                $"The tracker does not bind `{actual.TrackerTaskId}` to `{actual.AcceptedCommit}`.");

            SourceFile accepted = MeasureContent(
                actual.Path,
                RunGitBytes($"show {actual.AcceptedCommit}:{actual.Path}"));
            Require(
                accepted.LineCount == actual.MaxLines && accepted.ByteCount == actual.MaxBytes,
                $"`{actual.Path}` authorization must exactly match its accepted commit blob.");
        }
    }

    private static int CountContainedSymbols(string content, IReadOnlyList<string> symbols)
    {
        return symbols.Count(symbol => content.Contains(symbol, StringComparison.Ordinal));
    }

    private static string ComputeFileSha256(string path)
    {
        using SHA256 sha256 = SHA256.Create();
        return BitConverter.ToString(sha256.ComputeHash(File.ReadAllBytes(path)))
            .Replace("-", string.Empty)
            .ToLowerInvariant();
    }

    private static string ReadManifestJson()
    {
        Require(File.Exists(BaselinePath), $"Production source growth baseline is missing at `{BaselinePath}`.");
        string markdown = File.ReadAllText(BaselinePath);
        int markerStart = markdown.IndexOf(ManifestStartMarker, StringComparison.Ordinal);
        int markerEnd = markdown.IndexOf(ManifestEndMarker, StringComparison.Ordinal);
        Require(markerStart >= 0 && markerEnd > markerStart, "The baseline manifest markers are missing or out of order.");
        Require(
            markdown.LastIndexOf(ManifestStartMarker, StringComparison.Ordinal) == markerStart &&
            markdown.LastIndexOf(ManifestEndMarker, StringComparison.Ordinal) == markerEnd,
            "The baseline must contain exactly one manifest block.");

        int fenceStart = markdown.IndexOf("```json", markerStart + ManifestStartMarker.Length, StringComparison.Ordinal);
        Require(fenceStart >= 0 && fenceStart < markerEnd, "The manifest block must open with a ```json fence.");
        int jsonStart = markdown.IndexOf('\n', fenceStart);
        int fenceEnd = jsonStart < 0 ? -1 : markdown.IndexOf("\n```", jsonStart, StringComparison.Ordinal);
        Require(jsonStart >= 0 && fenceEnd > jsonStart && fenceEnd < markerEnd, "The manifest JSON fence is incomplete.");
        return markdown.Substring(jsonStart + 1, fenceEnd - jsonStart - 1);
    }

    private static void ValidateJsonShape(string json)
    {
        RejectDuplicateJsonProperties(json);
        using JsonDocument document = JsonDocument.Parse(json);
        JsonElement root = document.RootElement;
        Require(root.ValueKind == JsonValueKind.Object, "The baseline manifest root must be an object.");
        RequireExactProperties(
            root,
            "schemaVersion",
            "baselineCommit",
            "productionRoot",
            "productionEditorPathSegment",
            "helperSuffix",
            "reviewThresholdLines",
            "strictNoGrowthThresholdLines",
            "frozenSystemHelpers",
            "productionFilesOver500",
            "approvedExceptions");

        JsonElement helpers = root.GetProperty("frozenSystemHelpers");
        Require(helpers.ValueKind == JsonValueKind.Array, "frozenSystemHelpers must be an array.");
        foreach (JsonElement helper in helpers.EnumerateArray())
        {
            Require(helper.ValueKind == JsonValueKind.Object, "Every frozen helper baseline must be an object.");
            RequireExactProperties(helper, "path", "baselineLines", "baselineBytes");
        }

        JsonElement productionFiles = root.GetProperty("productionFilesOver500");
        Require(productionFiles.ValueKind == JsonValueKind.Array, "productionFilesOver500 must be an array.");
        foreach (JsonElement productionFile in productionFiles.EnumerateArray())
        {
            Require(productionFile.ValueKind == JsonValueKind.Object, "Every production baseline must be an object.");
            RequireExactProperties(productionFile, "path", "baselineLines", "baselineBytes", "strictNoGrowth");
        }

        JsonElement approvedExceptions = root.GetProperty("approvedExceptions");
        Require(approvedExceptions.ValueKind == JsonValueKind.Array, "approvedExceptions must be an array.");
        foreach (JsonElement exception in approvedExceptions.EnumerateArray())
        {
            Require(exception.ValueKind == JsonValueKind.Object, "Every approved exception must be an object.");
            RequireExactProperties(exception, "path", "trackerTaskId", "decisionId", "maxLines", "maxBytes", "scope");
        }
    }

    private static void RequireExactProperties(JsonElement element, params string[] expectedProperties)
    {
        var expected = new HashSet<string>(expectedProperties, StringComparer.Ordinal);
        List<string> names = element.EnumerateObject().Select(property => property.Name).ToList();
        string duplicate = names.GroupBy(name => name, StringComparer.Ordinal)
            .Where(group => group.Count() > 1)
            .Select(group => group.Key)
            .FirstOrDefault();
        Require(duplicate == null, $"Duplicate JSON property `{duplicate}` is forbidden.");
        var actual = new HashSet<string>(names, StringComparer.Ordinal);
        if (expected.SetEquals(actual))
            return;

        string missing = string.Join(", ", expected.Except(actual, StringComparer.Ordinal).OrderBy(value => value, StringComparer.Ordinal));
        string unknown = string.Join(", ", actual.Except(expected, StringComparer.Ordinal).OrderBy(value => value, StringComparer.Ordinal));
        throw new InvalidDataException($"Manifest object schema mismatch. Missing=[{missing}] Unknown=[{unknown}].");
    }

    private static List<SourceFile> EnumerateFiles(string root, string pattern, bool includeEditorPaths)
    {
        Require(Directory.Exists(root), $"Source root is missing at `{root}`.");
        List<SourceFile> files = Directory.GetFiles(root, pattern, SearchOption.AllDirectories)
            .Select(NormalizePath)
            .Where(path => includeEditorPaths || !HasPathSegment(path, EditorPathSegment))
            .OrderBy(path => path, StringComparer.Ordinal)
            .Select(MeasureFile)
            .ToList();
        Require(
            files.Select(file => file.Path).Distinct(PathIdentityComparer).Count() == files.Count,
            $"`{root}` contains case-insensitive duplicate source path identities.");
        return files;
    }

    private static bool HasBoundException(BaselineManifest manifest, SourceFile current, string requiredScope)
    {
        if (TryGetPostHardeningGuardrail(current.Path, out PostHardeningSourceGuardrail guardrail) &&
            (current.LineCount > guardrail.MaxLines || current.ByteCount > guardrail.MaxBytes))
        {
            return false;
        }

        if (manifest.ApprovedExceptions.Any(exception =>
            exception != null &&
            PathIdentityComparer.Equals(exception.Path, current.Path) &&
            string.Equals(exception.Scope, requiredScope, StringComparison.Ordinal) &&
            current.LineCount <= exception.MaxLines &&
            current.ByteCount <= exception.MaxBytes))
        {
            return true;
        }

        PostHardeningGuardrailContract contract = LoadPostHardeningGuardrails();
        return contract.GrowthAuthorizations.Any(authorization =>
            authorization != null &&
            PathIdentityComparer.Equals(authorization.Path, current.Path) &&
            string.Equals(authorization.Scope, requiredScope, StringComparison.Ordinal) &&
            current.LineCount <= authorization.MaxLines &&
            current.ByteCount <= authorization.MaxBytes);
    }

    private static bool TryGetPostHardeningGuardrail(
        string path,
        out PostHardeningSourceGuardrail guardrail)
    {
        guardrail = LoadPostHardeningGuardrails().Entries.FirstOrDefault(entry =>
            entry != null && PathIdentityComparer.Equals(entry.Path, path));
        return guardrail != null;
    }

    private static bool TryGetPostHardeningGrowthAuthorization(
        string path,
        string scope,
        out PostHardeningGrowthAuthorization authorization)
    {
        authorization = LoadPostHardeningGuardrails().GrowthAuthorizations.FirstOrDefault(entry =>
            entry != null &&
            PathIdentityComparer.Equals(entry.Path, path) &&
            string.Equals(entry.Scope, scope, StringComparison.Ordinal));
        return authorization != null;
    }

    private static Dictionary<string, SourceHistoryState> BuildSourceHistory(BaselineManifest manifest)
    {
        var history = new Dictionary<string, SourceHistoryState>(PathIdentityComparer);
        foreach (ProductionFileBaseline entry in manifest.ProductionFilesOver500)
            AddHistoryBaseline(history, entry.Path, entry.BaselineLines, entry.BaselineBytes);
        foreach (FrozenSystemHelperBaseline helper in manifest.FrozenSystemHelpers)
            AddHistoryBaseline(history, helper.Path, helper.BaselineLines, helper.BaselineBytes);

        RunGit($"merge-base --is-ancestor {manifest.BaselineCommit} HEAD", allowEmptyOutput: true);
        string numstat = RunGit(
            $"-c core.quotepath=false log --first-parent --reverse --diff-merges=first-parent " +
            $"--format=__COMMIT__%H --numstat --no-renames {manifest.BaselineCommit}..HEAD -- {ProductionRoot}",
            allowEmptyOutput: true);
        ValidateNumstatRecords(numstat);

        string pathStatus = RunGit(
            $"-c core.quotepath=false log --first-parent --reverse --diff-merges=first-parent " +
            $"--format=__COMMIT__%H --name-status --diff-filter=AMD --no-renames " +
            $"{manifest.BaselineCommit}..HEAD -- {ProductionRoot}",
            allowEmptyOutput: true);
        string currentCommit = null;
        foreach (string line in SplitRawLines(pathStatus))
        {
            if (line.StartsWith("__COMMIT__", StringComparison.Ordinal))
            {
                currentCommit = line.Substring("__COMMIT__".Length);
                continue;
            }

            string[] fields = line.Split('\t');
            Require(fields.Length == 2 && currentCommit != null, $"Malformed git name-status record: `{line}`.");
            string status = fields[0];
            string path = NormalizePath(fields[1]);
            Require(status == "A" || status == "M" || status == "D", $"Unsupported git path status `{status}` for `{path}`.");
            if (!history.TryGetValue(path, out SourceHistoryState state))
                continue;
            RequireRepositorySpelling(state.RepositoryPath, path, "git history");
            if (status == "D")
            {
                state.RecordDeletion();
                continue;
            }
            if (status == "A")
                state.RecordCreation();
            SourceFile snapshot = MeasureContent(path, RunGitBytes($"show {currentCommit}:{path}"));
            state.ApplySnapshot(snapshot.LineCount, snapshot.ByteCount);
        }

        return history;
    }

    private static void AddHistoryBaseline(
        IDictionary<string, SourceHistoryState> history,
        string path,
        int baselineLines,
        int baselineBytes)
    {
        if (history.TryGetValue(path, out SourceHistoryState existing))
        {
            RequireRepositorySpelling(existing.RepositoryPath, path, "overlapping baseline");
            Require(
                existing.MinimumPositiveLines == baselineLines && existing.MinimumPositiveBytes == baselineBytes,
                $"Overlapping helper/production baseline measurements disagree for `{path}`.");
            return;
        }

        history.Add(path, new SourceHistoryState(path, baselineLines, baselineBytes));
    }

    private static string RunGit(string arguments, bool allowEmptyOutput)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = "git",
            Arguments = arguments,
            WorkingDirectory = Directory.GetCurrentDirectory(),
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };
        using Process process = Process.Start(startInfo);
        Require(process != null, "Unable to start git for source-growth history validation.");
        string output = process.StandardOutput.ReadToEnd();
        string error = process.StandardError.ReadToEnd();
        process.WaitForExit();
        Require(
            process.ExitCode == 0,
            $"Git history validation failed ({process.ExitCode}) for `git {arguments}`: {error.Trim()}");
        Require(allowEmptyOutput || !string.IsNullOrWhiteSpace(output), $"Git returned no output for `git {arguments}`.");
        return output;
    }

    private static byte[] RunGitBytes(string arguments)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = "git",
            Arguments = arguments,
            WorkingDirectory = Directory.GetCurrentDirectory(),
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };
        using Process process = Process.Start(startInfo);
        Require(process != null, "Unable to start git for blob history validation.");
        using var output = new MemoryStream();
        process.StandardOutput.BaseStream.CopyTo(output);
        string error = process.StandardError.ReadToEnd();
        process.WaitForExit();
        Require(process.ExitCode == 0, $"Git blob validation failed for `git {arguments}`: {error.Trim()}");
        return output.ToArray();
    }

    private static IEnumerable<string> SplitRawLines(string value)
    {
        return value.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);
    }

    private static void ValidateNumstatRecords(string numstat)
    {
        foreach (string line in SplitRawLines(numstat))
        {
            if (line.StartsWith("__COMMIT__", StringComparison.Ordinal))
                continue;
            string[] fields = line.Split('\t');
            Require(fields.Length == 3, $"Malformed git numstat record: `{line}`.");
            Require(
                fields[0] != "-" && fields[1] != "-",
                $"Git numstat reported binary '-' counts for `{fields[2]}`; source-growth validation fails closed.");
            Require(
                int.TryParse(fields[0], out _) && int.TryParse(fields[1], out _),
                $"Malformed git numstat counts for `{fields[2]}`.");
        }
    }

    private static Dictionary<string, string> ExtractDetailedDecisionRows(string tracker)
    {
        MatchCollection headingMatches = Regex.Matches(
            tracker,
            "^## Decision Log\\r?$",
            RegexOptions.CultureInvariant | RegexOptions.Multiline);
        Require(headingMatches.Count == 1, "The tracker must contain exactly one canonical Decision Log section.");
        int headingStart = headingMatches[0].Index;
        int contentStart = headingStart + headingMatches[0].Length;
        Match nextSection = Regex.Match(
            tracker.Substring(contentStart),
            "^## [^\\r\\n]+\\r?$",
            RegexOptions.CultureInvariant | RegexOptions.Multiline);
        int sectionEnd = nextSection.Success ? contentStart + nextSection.Index : -1;
        string section = sectionEnd < 0
            ? tracker.Substring(headingStart)
            : tracker.Substring(headingStart, sectionEnd - headingStart);
        const string header = "| Date | Decision ID | Decision | Reason | Evidence/approval |";
        const string separator = "|---|---|---|---|---|";
        int tableStart = section.IndexOf(header, StringComparison.Ordinal);
        Require(tableStart >= 0, "The canonical five-column Decision Log header is missing.");
        string[] lines = section.Substring(tableStart).Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);
        Require(lines.Length >= 2 && lines[0] == header && lines[1] == separator, "The canonical Decision Log table header is malformed.");

        var rows = new Dictionary<string, string>(StringComparer.Ordinal);
        for (int index = 2; index < lines.Length; index++)
        {
            string line = lines[index];
            if (!line.StartsWith("|", StringComparison.Ordinal))
                break;
            string[] cells = SplitMarkdownTableRow(line);
            Require(cells.Length == 5, $"Decision Log row must contain exactly five columns: `{line}`.");
            Require(Regex.IsMatch(cells[0], @"^[0-9]{4}-[0-9]{2}-[0-9]{2}$"), $"Decision Log date is malformed: `{cells[0]}`.");
            Match idMatch = Regex.Match(cells[1], @"^`(?<id>D-[0-9]{3})`$", RegexOptions.CultureInvariant);
            if (!idMatch.Success)
            {
                Require(
                    line.IndexOf("`source-growth-exception(", StringComparison.Ordinal) < 0,
                    "Source-growth authorization markers require one exact D-nnn Decision Log row.");
                continue;
            }
            string id = idMatch.Groups["id"].Value;
            Require(rows.TryAdd(id, line), $"Detailed Decision Log contains duplicate row `{id}`.");
        }

        return rows;
    }

    private static string[] SplitMarkdownTableRow(string line)
    {
        Require(line.StartsWith("|", StringComparison.Ordinal) && line.EndsWith("|", StringComparison.Ordinal),
            $"Malformed Markdown table row: `{line}`.");
        return line.Substring(1, line.Length - 2).Split('|').Select(cell => cell.Trim()).ToArray();
    }

    private static string BuildExceptionDecisionMarker(ApprovedException exception)
    {
        return $"`source-growth-exception(path={exception.Path};scope={exception.Scope};" +
               $"maxLines={exception.MaxLines};maxBytes={exception.MaxBytes};task={exception.TrackerTaskId})`";
    }

    private static void RequireSingleDecisionAuthorization(
        IReadOnlyDictionary<string, string> rows,
        string decisionId,
        string citedRow,
        string marker)
    {
        int matches = rows.Values.Sum(row => CountOccurrences(row, marker));
        Require(
            matches == 1 && CountOccurrences(citedRow, marker) == 1,
            $"Authorization marker must appear exactly once in canonical Decision Log row `{decisionId}`.");
    }

    private static string BuildCanonicalDecisionLog(string rows, string prefix = null)
    {
        return (prefix == null ? string.Empty : prefix + "\n") +
               "## Decision Log\n\n" +
               "| Date | Decision ID | Decision | Reason | Evidence/approval |\n" +
               "|---|---|---|---|---|\n" + rows + "\n";
    }

    private static void RequireStageRunsUnconditionally(string jenkins, string startMarker, string endMarker)
    {
        string stage = ExtractTextBetween(jenkins, startMarker, endMarker);
        int stepsStart = stage.IndexOf("steps {", StringComparison.Ordinal);
        Require(stepsStart >= 0, $"Jenkins stage `{startMarker}` has no steps block.");
        Require(
            stage.Substring(0, stepsStart).IndexOf("when {", StringComparison.Ordinal) < 0,
            $"Jenkins stage `{startMarker}` must run even when no build target is selected.");
    }

    private static void ValidateNUnitTestRunContract(string xml)
    {
        var document = new XmlDocument();
        try
        {
            document.LoadXml(xml);
        }
        catch (XmlException exception)
        {
            throw new InvalidDataException("Invalid NUnit XML.", exception);
        }

        XmlElement run = document.DocumentElement;
        Require(run != null && run.Name == "test-run", "NUnit XML requires a test-run root.");
        string[] required = { "result", "failed", "total", "testcasecount", "passed", "inconclusive", "skipped", "start-time", "end-time" };
        foreach (string attribute in required)
            Require(run.HasAttribute(attribute), $"NUnit test-run is missing required attribute `{attribute}`.");
        Require(int.TryParse(run.GetAttribute("total"), out int total), "NUnit total must be an integer.");
        Require(int.TryParse(run.GetAttribute("testcasecount"), out int cases), "NUnit testcasecount must be an integer.");
        Require(int.TryParse(run.GetAttribute("passed"), out int passed), "NUnit passed must be an integer.");
        Require(int.TryParse(run.GetAttribute("failed"), out int failed), "NUnit failed must be an integer.");
        Require(int.TryParse(run.GetAttribute("inconclusive"), out int inconclusive), "NUnit inconclusive must be an integer.");
        Require(int.TryParse(run.GetAttribute("skipped"), out int skipped), "NUnit skipped must be an integer.");
        Require(total > 0 && cases > 0, "NUnit must discover at least one test.");
        Require(total == cases && passed + failed + inconclusive + skipped == total, "NUnit results are incomplete.");
        Require(run.SelectNodes(".//test-case").Count == cases, "NUnit serialized test cases are incomplete.");
        Require(run.GetAttribute("result") == "Passed" && failed == 0, "NUnit results did not pass.");
    }

    private static void ExpectInvalid(Action action, string expectedMessageFragment)
    {
        try
        {
            action();
        }
        catch (InvalidDataException exception)
        {
            Require(
                exception.Message.IndexOf(expectedMessageFragment, StringComparison.OrdinalIgnoreCase) >= 0,
                $"Expected failure containing `{expectedMessageFragment}`, got `{exception.Message}`.");
            return;
        }

        throw new InvalidDataException($"Expected validation failure containing `{expectedMessageFragment}`.");
    }

    private static string ExtractTextBetween(string contents, string startMarker, string endMarker)
    {
        int start = contents.IndexOf(startMarker, StringComparison.Ordinal);
        int end = start < 0 ? -1 : contents.IndexOf(endMarker, start + startMarker.Length, StringComparison.Ordinal);
        Require(start >= 0 && end > start, $"Could not find `{startMarker}` before `{endMarker}`.");
        return contents.Substring(start, end - start);
    }

    private static int CountOccurrences(string contents, string value)
    {
        int count = 0;
        int searchStart = 0;
        while (true)
        {
            int found = contents.IndexOf(value, searchStart, StringComparison.Ordinal);
            if (found < 0)
                return count;
            count++;
            searchStart = found + value.Length;
        }
    }

    private static int CountTotalOccurrences(string contents, IEnumerable<string> values)
    {
        return values.Sum(value => CountOccurrences(contents, value));
    }

    private static HashSet<string> ExtractIds(Regex regex, string contents)
    {
        return regex.Matches(contents)
            .Cast<Match>()
            .Select(match => match.Groups["id"].Value)
            .ToHashSet(StringComparer.Ordinal);
    }

    private static void RejectDuplicateJsonProperties(string json)
    {
        using JsonDocument document = JsonDocument.Parse(json);
        RejectDuplicateJsonProperties(document.RootElement, "$manifest");
    }

    private static void RejectDuplicateJsonProperties(JsonElement element, string location)
    {
        if (element.ValueKind == JsonValueKind.Object)
        {
            var names = new HashSet<string>(StringComparer.Ordinal);
            foreach (JsonProperty property in element.EnumerateObject())
            {
                Require(names.Add(property.Name), $"Duplicate JSON property `{property.Name}` at `{location}`.");
                RejectDuplicateJsonProperties(property.Value, location + "." + property.Name);
            }
        }
        else if (element.ValueKind == JsonValueKind.Array)
        {
            int index = 0;
            foreach (JsonElement item in element.EnumerateArray())
                RejectDuplicateJsonProperties(item, location + "[" + index++ + "]");
        }
    }

    private static SourceFile MeasureFile(string path)
    {
        return MeasureContent(path, File.ReadAllBytes(path));
    }

    private static SourceFile MeasureContent(string path, byte[] rawBytes)
    {
        string normalized = Encoding.UTF8.GetString(rawBytes)
            .Replace("\r\n", "\n")
            .Replace('\r', '\n');
        int lines = normalized.Length == 0
            ? 0
            : normalized.Count(value => value == '\n') + (normalized.EndsWith("\n", StringComparison.Ordinal) ? 0 : 1);
        return new SourceFile(path, lines, Encoding.UTF8.GetByteCount(normalized));
    }

    private static bool ExceedsHistoricalCeiling(SourceFile current, SourceHistoryState state)
    {
        return current.LineCount > state.MinimumPositiveLines || current.ByteCount > state.MinimumPositiveBytes;
    }

    private static void RequireBaselineRepositorySpelling(BaselineManifest manifest)
    {
        string tree = RunGit(
            $"-c core.quotepath=false ls-tree -r --name-only {manifest.BaselineCommit} -- {ProductionRoot}",
            allowEmptyOutput: false);
        var repositoryPaths = new Dictionary<string, string>(PathIdentityComparer);
        foreach (string rawPath in SplitRawLines(tree))
        {
            string path = NormalizePath(rawPath);
            Require(repositoryPaths.TryAdd(path, path), $"Baseline tree has case-insensitive duplicate path `{path}`.");
        }

        var expectedMeasurements = new Dictionary<string, SourceFile>(PathIdentityComparer);
        foreach (FrozenSystemHelperBaseline helper in manifest.FrozenSystemHelpers)
            AddExpectedBaselineMeasurement(expectedMeasurements, helper.Path, helper.BaselineLines, helper.BaselineBytes);
        foreach (ProductionFileBaseline production in manifest.ProductionFilesOver500)
            AddExpectedBaselineMeasurement(expectedMeasurements, production.Path, production.BaselineLines, production.BaselineBytes);

        foreach (SourceFile expected in expectedMeasurements.Values)
        {
            Require(repositoryPaths.TryGetValue(expected.Path, out string repositoryPath), $"Baseline path is absent from Git: `{expected.Path}`.");
            RequireRepositorySpelling(expected.Path, repositoryPath, "baseline manifest");
            SourceFile blob = MeasureContent(expected.Path, RunGitBytes($"show {manifest.BaselineCommit}:{expected.Path}"));
            Require(
                blob.LineCount == expected.LineCount && blob.ByteCount == expected.ByteCount,
                $"Baseline Git measurement mismatch for `{expected.Path}`.");
        }
    }

    private static void AddExpectedBaselineMeasurement(
        IDictionary<string, SourceFile> expected,
        string path,
        int lines,
        int bytes)
    {
        if (expected.TryGetValue(path, out SourceFile existing))
        {
            RequireRepositorySpelling(existing.Path, path, "overlapping baseline");
            Require(existing.LineCount == lines && existing.ByteCount == bytes,
                $"Overlapping baseline measurement mismatch for `{path}`.");
            return;
        }

        expected.Add(path, new SourceFile(path, lines, bytes));
    }

    private static void RequireRepositorySpelling(string expected, string actual, string context)
    {
        Require(PathIdentityComparer.Equals(expected, actual), $"{context} path identity mismatch: `{expected}` vs `{actual}`.");
        Require(string.Equals(expected, actual, StringComparison.Ordinal),
            $"{context} must preserve repository path spelling: expected `{expected}`, found `{actual}`.");
    }

    private static string ComputeLineHash(IEnumerable<string> values)
    {
        string canonical = string.Concat(values.Select(value => value + "\n"));
        using SHA256 sha256 = SHA256.Create();
        byte[] hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(canonical));
        return BitConverter.ToString(hash).Replace("-", string.Empty).ToLowerInvariant();
    }

    private static string NormalizePath(string path)
    {
        return path.Replace('\\', '/');
    }

    private static bool HasPathSegment(string path, string segment)
    {
        return path.Split('/').Any(value => string.Equals(value, segment, StringComparison.Ordinal));
    }

    private static bool IsOrdinallySorted(IReadOnlyList<string> values)
    {
        for (int index = 1; index < values.Count; index++)
        {
            if (string.CompareOrdinal(values[index - 1], values[index]) >= 0)
                return false;
        }

        return true;
    }

    private static void RequireExactProjectSourcePath(string path, string fieldName)
    {
        Require(!string.IsNullOrWhiteSpace(path), $"Every {fieldName} is required.");
        Require(!Path.IsPathRooted(path), $"Every {fieldName} must be project-relative: `{path}`.");
        Require(path.IndexOf('\\') < 0, $"Every {fieldName} must use forward slashes: `{path}`.");
        Require(path.IndexOfAny(new[] { '*', '?' }) < 0, $"Every {fieldName} must be exact, without globs: `{path}`.");
        Require(
            path.StartsWith(ProductionRoot + "/", StringComparison.Ordinal),
            $"Every {fieldName} must be below `{ProductionRoot}`: `{path}`.");
        Require(path.EndsWith(".cs", StringComparison.Ordinal), $"Every {fieldName} must name a C# file: `{path}`.");
        Require(
            path.Split('/').All(segment => segment.Length > 0 && segment != "." && segment != ".."),
            $"Every {fieldName} must be normalized and traversal-free: `{path}`.");
    }

    private static void Require(bool condition, string message)
    {
        if (!condition)
            throw new InvalidDataException(message);
    }

    private sealed class SourceFile
    {
        public SourceFile(string path, int lineCount, int byteCount)
        {
            Path = path;
            LineCount = lineCount;
            ByteCount = byteCount;
        }

        public string Path { get; }
        public int LineCount { get; }
        public int ByteCount { get; }
    }

    public sealed class BaselineManifest
    {
        [JsonPropertyName("schemaVersion")]
        public int SchemaVersion { get; set; }

        [JsonPropertyName("baselineCommit")]
        public string BaselineCommit { get; set; }

        [JsonPropertyName("productionRoot")]
        public string ProductionRoot { get; set; }

        [JsonPropertyName("productionEditorPathSegment")]
        public string ProductionEditorPathSegment { get; set; }

        [JsonPropertyName("helperSuffix")]
        public string HelperSuffix { get; set; }

        [JsonPropertyName("reviewThresholdLines")]
        public int ReviewThresholdLines { get; set; }

        [JsonPropertyName("strictNoGrowthThresholdLines")]
        public int StrictNoGrowthThresholdLines { get; set; }

        [JsonPropertyName("frozenSystemHelpers")]
        public List<FrozenSystemHelperBaseline> FrozenSystemHelpers { get; set; }

        [JsonPropertyName("productionFilesOver500")]
        public List<ProductionFileBaseline> ProductionFilesOver500 { get; set; }

        [JsonPropertyName("approvedExceptions")]
        public List<ApprovedException> ApprovedExceptions { get; set; }
    }

    public sealed class FrozenSystemHelperBaseline
    {
        [JsonPropertyName("path")]
        public string Path { get; set; }

        [JsonPropertyName("baselineLines")]
        public int BaselineLines { get; set; }

        [JsonPropertyName("baselineBytes")]
        public int BaselineBytes { get; set; }
    }

    public sealed class ProductionFileBaseline
    {
        [JsonPropertyName("path")]
        public string Path { get; set; }

        [JsonPropertyName("baselineLines")]
        public int BaselineLines { get; set; }

        [JsonPropertyName("baselineBytes")]
        public int BaselineBytes { get; set; }

        [JsonPropertyName("strictNoGrowth")]
        public bool StrictNoGrowth { get; set; }
    }

    public sealed class ApprovedException
    {
        [JsonPropertyName("path")]
        public string Path { get; set; }

        [JsonPropertyName("trackerTaskId")]
        public string TrackerTaskId { get; set; }

        [JsonPropertyName("decisionId")]
        public string DecisionId { get; set; }

        [JsonPropertyName("maxLines")]
        public int MaxLines { get; set; }

        [JsonPropertyName("maxBytes")]
        public int MaxBytes { get; set; }

        [JsonPropertyName("scope")]
        public string Scope { get; set; }
    }

    public sealed class PostHardeningGuardrailContract
    {
        [JsonPropertyName("schemaVersion")]
        public int SchemaVersion { get; set; }

        [JsonPropertyName("contractId")]
        public string ContractId { get; set; }

        [JsonPropertyName("replacementOwnerBoundary")]
        public PostHardeningReplacementOwnerBoundary ReplacementOwnerBoundary { get; set; }

        [JsonPropertyName("growthAuthorizations")]
        public List<PostHardeningGrowthAuthorization> GrowthAuthorizations { get; set; }

        [JsonPropertyName("entries")]
        public List<PostHardeningSourceGuardrail> Entries { get; set; }
    }

    public sealed class PostHardeningReplacementOwnerBoundary
    {
        [JsonPropertyName("root")]
        public string Root { get; set; }

        [JsonPropertyName("baselineCommit")]
        public string BaselineCommit { get; set; }

        [JsonPropertyName("domainSymbol")]
        public string DomainSymbol { get; set; }

        [JsonPropertyName("managedLifecycleSymbols")]
        public List<string> ManagedLifecycleSymbols { get; set; }

        [JsonPropertyName("managedLifecycleMatchThreshold")]
        public int ManagedLifecycleMatchThreshold { get; set; }

        [JsonPropertyName("genericLifecycleMatchThreshold")]
        public int GenericLifecycleMatchThreshold { get; set; }

        [JsonPropertyName("genericLifecycleAnchorSymbols")]
        public List<string> GenericLifecycleAnchorSymbols { get; set; }

        [JsonPropertyName("allowedOwnerPaths")]
        public List<string> AllowedOwnerPaths { get; set; }
    }

    public sealed class PostHardeningGrowthAuthorization
    {
        [JsonPropertyName("path")]
        public string Path { get; set; }

        [JsonPropertyName("trackerTaskId")]
        public string TrackerTaskId { get; set; }

        [JsonPropertyName("acceptedCommit")]
        public string AcceptedCommit { get; set; }

        [JsonPropertyName("maxLines")]
        public int MaxLines { get; set; }

        [JsonPropertyName("maxBytes")]
        public int MaxBytes { get; set; }

        [JsonPropertyName("scope")]
        public string Scope { get; set; }
    }

    public sealed class PostHardeningSourceGuardrail
    {
        [JsonPropertyName("path")]
        public string Path { get; set; }

        [JsonPropertyName("sourceSha256")]
        public string SourceSha256 { get; set; }

        [JsonPropertyName("maxLines")]
        public int MaxLines { get; set; }

        [JsonPropertyName("maxBytes")]
        public int MaxBytes { get; set; }

        [JsonPropertyName("maxResponsibilityDomainSymbolOccurrences")]
        public int MaxResponsibilityDomainSymbolOccurrences { get; set; }

        [JsonPropertyName("maxStateSlots")]
        public int MaxStateSlots { get; set; }

        [JsonPropertyName("responsibilities")]
        public List<string> Responsibilities { get; set; }

        [JsonPropertyName("requiredSymbols")]
        public List<string> RequiredSymbols { get; set; }

        [JsonPropertyName("forbiddenSymbols")]
        public List<string> ForbiddenSymbols { get; set; }

        [JsonPropertyName("responsibilitySignatureSymbols")]
        public List<string> ResponsibilitySignatureSymbols { get; set; }

        [JsonPropertyName("responsibilitySignatureMatchThreshold")]
        public int ResponsibilitySignatureMatchThreshold { get; set; }
    }

    private sealed class SourceHistoryState
    {
        public SourceHistoryState(string repositoryPath, int initialLines, int initialBytes)
        {
            Require(initialLines > 0, "Historical source state requires a positive initial line count.");
            Require(initialBytes > 0, "Historical source state requires a positive initial byte count.");
            RepositoryPath = repositoryPath;
            CurrentLines = initialLines;
            CurrentBytes = initialBytes;
            MinimumPositiveLines = initialLines;
            MinimumPositiveBytes = initialBytes;
        }

        public string RepositoryPath { get; }
        public int CurrentLines { get; private set; }
        public int CurrentBytes { get; private set; }
        public int MinimumPositiveLines { get; private set; }
        public int MinimumPositiveBytes { get; private set; }
        public bool WasDeleted { get; private set; }
        public bool WasRecreated { get; private set; }

        public void ApplySnapshot(int nextLines, int nextBytes)
        {
            Require(nextLines > 0 && nextBytes > 0, $"Historical source snapshot for `{RepositoryPath}` must be positive.");
            if (CurrentLines == 0 && WasDeleted)
                WasRecreated = true;
            CurrentLines = nextLines;
            CurrentBytes = nextBytes;
            MinimumPositiveLines = Math.Min(MinimumPositiveLines, nextLines);
            MinimumPositiveBytes = Math.Min(MinimumPositiveBytes, nextBytes);
        }

        public void RecordDeletion()
        {
            WasDeleted = true;
            CurrentLines = 0;
            CurrentBytes = 0;
        }

        public void RecordCreation()
        {
            if (WasDeleted)
                WasRecreated = true;
        }
    }
}
