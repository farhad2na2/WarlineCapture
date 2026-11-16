#!/usr/bin/env python3
"""Generate the exact opt-in combined-history-scan candidate owner source."""
from __future__ import annotations

import hashlib
from pathlib import Path

SOURCE = "Assets/Game/Scripts/Systems/StaticGridBlockerUpdateSystem.cs"
SOURCE_SHA256 = "3041ad89db5d94881fe8c8e2e0ac7e6471793679c9a4e5c6fe4c70c2a489b306"
OUTPUT = "StaticGridBlockerUpdateSystem.Candidate.cs"
BEGIN = """            // History is meaningful only for this exact grid entity/version and storage epoch.
            // Scan the same existing/removal domains, without sorting or filtering foreign inputs.
            foreach (var prev in SystemAPI.Query<RefRO<StaticBlockerPrevBounds>>()
                         .WithAll<StaticGridBlocker, UnitGrid, GridBlockerSize>())
            {
                var previous = prev.ValueRO;
                if (!StaticBlockerKernel.SameOwner(Identity(previous.AppliedGrid,previous.StorageEpoch),Identity(gridEntity,owner.StorageEpoch))) rebuild = true;
            }
            foreach (var prev in SystemAPI.Query<RefRO<StaticBlockerPrevBounds>>().WithNone<StaticGridBlocker>())
            {
                var previous = prev.ValueRO;
                if (!StaticBlockerKernel.SameOwner(Identity(previous.AppliedGrid,previous.StorageEpoch),Identity(gridEntity,owner.StorageEpoch))) rebuild = true;
            }
"""
CANDIDATE = """            // Opt-in candidate: exact union of the original owner-history query domains.
            var staticBlockerLookup = SystemAPI.GetComponentLookup<StaticGridBlocker>(true);
            var unitGridHistoryLookup = SystemAPI.GetComponentLookup<UnitGrid>(true);
            var sizeHistoryLookup = SystemAPI.GetComponentLookup<GridBlockerSize>(true);
            foreach (var (prev, entity) in SystemAPI.Query<RefRO<StaticBlockerPrevBounds>>().WithEntityAccess())
            {
                bool hasStaticBlocker = staticBlockerLookup.HasComponent(entity);
                if (hasStaticBlocker &&
                    (!unitGridHistoryLookup.HasComponent(entity) || !sizeHistoryLookup.HasComponent(entity)))
                    continue;
                var previous = prev.ValueRO;
                if (!StaticBlockerKernel.SameOwner(Identity(previous.AppliedGrid,previous.StorageEpoch),Identity(gridEntity,owner.StorageEpoch))) rebuild = true;
            }
"""


def sha(raw: bytes) -> str:
    return hashlib.sha256(raw).hexdigest()


def generate(repo: Path, output: Path) -> dict:
    repo = Path(repo).resolve(strict=True)
    source_path = repo / SOURCE
    raw = source_path.read_bytes()
    digest = sha(raw)
    if digest != SOURCE_SHA256:
        raise ValueError("Pinned production owner source hash mismatch")
    text = raw.decode("utf-8")
    if text.count(BEGIN) != 1:
        raise ValueError("Exact audited two-scan owner block required")
    candidate = text.replace(BEGIN, CANDIDATE, 1)
    if candidate == text:
        raise ValueError("Candidate transform made no change")
    output = Path(output)
    output.mkdir(parents=True, exist_ok=False)
    target = output / OUTPUT
    with target.open("w", encoding="utf-8", newline="\n") as stream:
        stream.write(candidate)
    generated = target.read_bytes()
    generator = Path(__file__).resolve(strict=True)
    return {
        "source_pins": {str(source_path): digest, str(generator): sha(generator.read_bytes())},
        "experimental_only": True,
        "scope": "Only preflight owner-history scans are combined; original candidate source remains the semantic baseline",
        "candidate_filter": "!has StaticGridBlocker || (has UnitGrid && has GridBlockerSize)",
        "same_owner_check_preserved": True,
        "mismatch_preflight_before_rebuild_clear_and_incremental_apply": True,
        "new_existing_removed_and_opening_phases_byte_identical": True,
        "candidate_sha256": sha(generated),
        "output": OUTPUT,
        "compiled_or_executed": False,
        "performance_claimed": False,
    }
