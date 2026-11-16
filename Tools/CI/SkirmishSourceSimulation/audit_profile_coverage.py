"""Report planned Skirmish mission IDs against configured source profiles.

This inspects only the default checked-in base profile manifest. It does not
assess external qualification contexts, S002's derived seed, or execution
evidence. Missing base-manifest entries do not establish missing external
profiles or source runs.
"""

import json
from pathlib import Path


ROOT = Path(__file__).resolve().parents[3]
SIMULATION = ROOT / "Tools/CI/SkirmishSourceSimulation"
PLAN = ROOT / "Design/MapVariants/FUTURE_CONTENT_MAP_PLAN.md"


def planned_mission_ids():
    # The plan documents 114 uncoded IDs plus six preserved coded/prototype IDs.
    ranges = ((5, 24), (26, 72), (74, 120))
    uncoded = {f"S{i:03d}" for start, end in ranges for i in range(start, end + 1)}
    preserved = {"S001", "S002", "S003", "S004", "S025", "S073"}
    return sorted(uncoded | preserved)


def compress_ids(mission_ids):
    numbers = sorted(int(mission_id[1:]) for mission_id in mission_ids)
    if not numbers:
        return []
    ranges = []
    start = previous = numbers[0]
    for number in numbers[1:]:
        if number == previous + 1:
            previous = number
            continue
        ranges.append((start, previous))
        start = previous = number
    ranges.append((start, previous))
    return [f"S{start:03d}" if start == end else f"S{start:03d}-S{end:03d}" for start, end in ranges]


def build_report():
    config = json.loads((SIMULATION / "admitted_profiles.json").read_text(encoding="utf-8"))
    profiles = config["profiles"]
    ids = planned_mission_ids()
    by_id = {
        mission_id: [profile for profile in profiles if profile["mission_id"] == mission_id]
        for mission_id in ids
    }
    configured_ids = sorted(mission_id for mission_id, rows in by_id.items() if rows)
    return {
        "schema_version": 1,
        "classification": "configuration_census_only",
        "mission_id_basis": "Hardcoded ID ranges matching the documented plan; the plan is not parsed",
        "profile_manifest_scope": "default_checked_in_base_manifest_only",
        "profile_source": "Tools/CI/SkirmishSourceSimulation/admitted_profiles.json",
        "planned_mission_count": len(ids),
        "configured_profile_entry_count": len(profiles),
        "configured_profile_seed_case_count": sum(len(p.get("seeds", [])) for p in profiles),
        "configured_mission_ids": configured_ids,
        "configured_mission_count": len(configured_ids),
        "missions_without_configured_profile_ranges": compress_ids(
            [mission_id for mission_id in ids if mission_id not in configured_ids]
        ),
        "configured_profiles": [
            {
                "mission_id": mission_id,
                "entry_count": len(by_id[mission_id]),
                "profile_scope_values": sorted({p.get("scope", "unspecified") for p in by_id[mission_id]}),
                "runtime_backend_values": sorted({p.get("runtime_backend", "default") for p in by_id[mission_id]}),
            }
            for mission_id in configured_ids
        ],
        "unconfigured_mission_profile_reason": (
            "No entry in the checked-in base manifest; external qualification contexts and recorded runs are not assessed"
        ),
        "external_qualification_contexts_assessment": "not_assessed",
        "s002_derived_seed_assessment": "not_assessed",
        "execution_evidence_assessment": "not_assessed",
    }


if __name__ == "__main__":
    print(json.dumps(build_report(), indent=2, sort_keys=True))
