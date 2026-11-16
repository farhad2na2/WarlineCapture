#!/usr/bin/env python3
"""Add bounded passive source-only observations around the original budget call."""
import hashlib
from pathlib import Path

SOURCE = "Assets/Game/Scripts/Systems/UnitPathfindingBudget.cs"
OUTPUT = "SourceS003ObservedPathBudget.cs"
CALL = "            PathRequestBudgetKernel.ReportCompletedJob(ref _state,pendingFrames,pendingWallTime,requestCount,manualRequestCount,vehicleLikeCount,pendingRequestBudget);"
OBSERVED = '''            bool sourceObserve=System.Environment.GetEnvironmentVariable("WARLINE_SOURCE_S003_PATH_BUDGET_OBSERVE")=="1" && UnityEngine.Time.frameCount>=2150 && UnityEngine.Time.frameCount<=2245;
            var sourceBefore=_state;
            PathRequestBudgetKernel.ReportCompletedJob(ref _state,pendingFrames,pendingWallTime,requestCount,manualRequestCount,vehicleLikeCount,pendingRequestBudget);
            if(sourceObserve)System.Console.WriteLine("[S003PathBudgetObservation] frame="+UnityEngine.Time.frameCount+" pendingFrames="+pendingFrames+" wall="+pendingWallTime.ToString("R",System.Globalization.CultureInfo.InvariantCulture)+" requests="+requestCount+" manual="+manualRequestCount+" vehicle="+vehicleLikeCount+" scheduledBudget="+pendingRequestBudget+" before="+sourceBefore.AdaptiveRequestsPerFrame+" beforeStable="+sourceBefore.StableOneFrameBatchCount+" after="+_state.AdaptiveRequestsPerFrame+" afterStable="+_state.StableOneFrameBatchCount);
'''


def transform(source):
    if source.count(CALL) != 1:
        raise ValueError("Exact original path budget call required")
    return source.replace(CALL, OBSERVED, 1)


def generate(repo, output):
    root = Path(repo).resolve(strict=True); path = root / SOURCE
    raw = path.read_bytes(); rendered = transform(raw.decode("utf-8")).encode("utf-8")
    if path.read_bytes() != raw:
        raise ValueError("Original path budget changed during generation")
    dest = Path(output); dest.mkdir(parents=True, exist_ok=False)
    (dest / OUTPUT).write_bytes(rendered)
    return {"source_pins": {str(path): hashlib.sha256(raw).hexdigest()},
            "outputs": {OUTPUT: hashlib.sha256(rendered).hexdigest()},
            "experimental_only": True,
            "scope": "Bounded passive source-only original budget observations; wall timing may be perturbed"}
