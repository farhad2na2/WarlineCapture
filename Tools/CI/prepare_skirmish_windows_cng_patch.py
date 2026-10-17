"""Generate reviewable CNG+focused-validation patch outside Assets; never apply it."""
import difflib
import hashlib
import json
from pathlib import Path


def prepare(project):
    source_name = "Assets/Game/Scripts/Editor/Skirmish/SkirmishCandidateSourceHash.cs"
    test_name = "Assets/Game/Scripts/Editor/Skirmish/SkirmishCandidateSourceHashWindowsValidation.cs"
    drafts = project / "Tools/CI/SkirmishProductionDrafts"
    original_bytes = (project / source_name).read_bytes()
    original = original_bytes.decode("utf-8")
    newline = "\r\n" if "\r\n" in original else "\n"
    marker = "#else" + newline + "            private readonly SHA256 hash = SHA256.Create();"
    if original.count(marker) != 1:
        raise ValueError("Original fallback seam changed; re-review source before preparing")
    branch = (drafts / "SkirmishCandidateSourceHash.WindowsCng.branch.cs.txt").read_text(encoding="utf-8").rstrip().replace("\n", newline) + newline
    if original.count(branch) == 1:
        record = dict(result="AlreadyIntegratedExactReviewedBranch", source=source_name,
            current_source_sha256=hashlib.sha256(original_bytes).hexdigest(),
            focused_validation_exists=(project / test_name).is_file(), native_run_performed=False,
            action="No patch generated or applied; preserve existing imported branch and source freeze")
        print(json.dumps(record, indent=2))
        return
    if "#elif UNITY_EDITOR_WIN" in original:
        raise ValueError("Existing Windows branch differs from reviewed draft; re-review rather than reinserting")
    proposed = original.replace(marker, branch + marker)
    if proposed.replace(branch, "", 1).encode("utf-8") != original_bytes:
        raise ValueError("Original source bytes did not survive exact branch removal")
    test = (drafts / "SkirmishCandidateSourceHashWindowsValidation.cs.txt").read_text(encoding="utf-8")
    patch = "".join(difflib.unified_diff(original.replace("\r\n", "\n").splitlines(True), proposed.replace("\r\n", "\n").splitlines(True), fromfile="a/"+source_name, tofile="b/"+source_name))
    patch += "".join(difflib.unified_diff([], test.splitlines(True), fromfile="/dev/null", tofile="b/"+test_name))
    output = drafts / "SkirmishCandidateSourceHash.WindowsCng.integration.patch"
    output.write_text(patch, encoding="utf-8", newline="\n")
    record = dict(result="PreparedOutsideAssetsOnly", source=source_name, focusedValidation=test_name,
        originalSourceSha256=hashlib.sha256(original_bytes).hexdigest(), proposedSourceSha256=hashlib.sha256(proposed.encode("utf-8")).hexdigest(),
        patchSha256=hashlib.sha256(output.read_bytes()).hexdigest(), preservedOriginalBranches=True,
        executeMethod="Game.Tests.Editor.SkirmishCandidateSourceHashWindowsValidation.RunFocusedValidation",
        requiredPassMarker="[SkirmishCandidateSourceHashWindowsValidation] result=Passed",
        timeoutSeconds=180, nativeRunPerformed=False,
        integrationGate="Current wrapper must exit before importing source/test; root owns integration and native runs")
    (drafts / "SkirmishCandidateSourceHash.WindowsCng.integration.json").write_text(json.dumps(record, indent=2)+"\n", encoding="utf-8")
    harness_name = "Assets/Game/Scripts/Editor/Skirmish/SkirmishS002AriaRunHarness.cs"
    harness = (project / harness_name).read_text(encoding="utf-8")
    replacements = [
        ("        private static System.Threading.Tasks.Task<string> finishSourceHash;",
         "        private static System.Threading.Tasks.Task<string> finishSourceHash;\n        private static System.Diagnostics.Stopwatch finishFingerprintTimer;"),
        ('            SessionState.SetString(CandidateHashKey,\n                SkirmishCandidateSourceHash.Compute(Path.GetFullPath(Path.Combine(Application.dataPath, ".."))));',
         '            Debug.Log("[SkirmishCandidateFingerprint] phase=Start owner=LaunchCatalog");\n            var launchFingerprintTimer = System.Diagnostics.Stopwatch.StartNew();\n            string launchFingerprint = SkirmishCandidateSourceHash.Compute(Path.GetFullPath(Path.Combine(Application.dataPath, "..")));\n            launchFingerprintTimer.Stop();\n            Debug.Log("[SkirmishCandidateFingerprint] phase=Completed owner=LaunchCatalog elapsedMilliseconds=" + launchFingerprintTimer.Elapsed.TotalMilliseconds.ToString("R", CultureInfo.InvariantCulture) + " sha256=" + launchFingerprint);\n            SessionState.SetString(CandidateHashKey, launchFingerprint);'),
        ('            finishSourceHash = System.Threading.Tasks.Task.Run(() => SkirmishCandidateSourceHash.Compute(root));',
         '            Debug.Log("[SkirmishCandidateFingerprint] phase=Start owner=Finish");\n            finishFingerprintTimer = System.Diagnostics.Stopwatch.StartNew();\n            finishSourceHash = System.Threading.Tasks.Task.Run(() => SkirmishCandidateSourceHash.Compute(root));'),
        ('            finishSourceHash = null;\n            if (completed.IsFaulted || completed.IsCanceled)',
         '            finishSourceHash = null;\n            finishFingerprintTimer?.Stop();\n            Debug.Log("[SkirmishCandidateFingerprint] phase=Completed owner=Finish elapsedMilliseconds=" + (finishFingerprintTimer?.Elapsed.TotalMilliseconds ?? -1d).ToString("R", CultureInfo.InvariantCulture) + " taskStatus=" + completed.Status);\n            finishFingerprintTimer = null;\n            if (completed.IsFaulted || completed.IsCanceled)')]
    instrumented = harness
    for old, new in replacements:
        if instrumented.count(old) != 1:
            raise ValueError("Optional harness phase seam changed; preserve source and re-review")
        instrumented = instrumented.replace(old, new)
    phase_patch = "".join(difflib.unified_diff(harness.splitlines(True), instrumented.splitlines(True), fromfile="a/"+harness_name, tofile="b/"+harness_name))
    (drafts / "SkirmishS002AriaRunHarness.FingerprintPhases.optional.patch").write_text(phase_patch, encoding="utf-8", newline="\n")
    print(json.dumps(record, indent=2))


if __name__ == "__main__":
    prepare(Path(__file__).resolve().parents[2])
