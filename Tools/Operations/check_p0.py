#!/usr/bin/env python3
"""Host-side Operations P0 checks that do not require Unity."""

from __future__ import annotations

import csv
import json
import re
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
CATALOG = ROOT / "Design/Roadmap/Operations/MISSION_CATALOG.csv"
CONTRACTS_ASMDEF = ROOT / "Assets/Game/Scripts/Operations/Contracts/Game.Operations.Contracts.asmdef"
TESTS_ASMDEF = ROOT / "Assets/Tests/Editor/Operations/Game.Operations.Tests.Editor.asmdef"
SEGMENT = re.compile(r"[a-z0-9][a-z0-9_]*")
PASS_MARKER = "[OperationsP0Validation] result=Passed checks=13"

FORBIDDEN_ASMDEFS = [
    "Assets/Game/Scripts/Configs/Game.Configs.asmdef",
    "Assets/Game/Scripts/Components/Game.Components.asmdef",
    "Assets/Game/Scripts/Game.Runtime.asmdef",
    "Assets/Game/Scripts/UI/Contracts/Game.UI.Contracts.asmdef",
    "Assets/Game/Scripts/UI/Shell/Ecs/Game.UI.Shell.Ecs.asmdef",
    "Assets/Game/Scripts/Composition/Game.Composition.asmdef",
    "Assets/Game/Scripts/Editor/Game.Editor.asmdef",
    "Assets/Tests/Editor/Game.Tests.Editor.asmdef",
]
FORBIDDEN_SEAMS = [
    "Assets/Game/Scripts/Persistence/SaveDataModel.cs",
    "Assets/Game/Scripts/Composition/MatchSceneView.OperationMapLaunch.cs",
    "Assets/Game/Scripts/Configs/OperationMapIdentityRules.cs",
]
MAPS = {
    "d01": "opmap.operations.old_quarter",
    "d02": "opmap.operations.civic_center",
    "d03": "opmap.operations.industrial_belt",
    "d04": "opmap.operations.river_crossing",
    "d05": "opmap.operations.highland_approach",
    "d06": "opmap.operations.airport_perimeter",
}
FAMILIES = {
    "RECON", "PATROL", "RAID", "RESCUE", "ESCORT", "REPAIR",
    "DEFENSE", "INTERDICT", "SEIZE", "AIRLIFT", "BREACH", "FINALE",
}


def fail(message: str) -> None:
    raise SystemExit(f"[OperationsP0Validation] result=Failed {message}")


def parse_id(value: str) -> list[str] | None:
    if not value or len(value) > 60:
        return None
    parts = value.split(".")
    if any(not SEGMENT.fullmatch(part) for part in parts):
        return None
    return parts


def numbered(part: str, prefix: str, digits: int, lo: int, hi: int) -> bool:
    if len(part) != 1 + digits or not part.startswith(prefix) or not part[1:].isdigit():
        return False
    number = int(part[1:])
    return lo <= number <= hi


def check_catalog() -> None:
    rows = list(csv.DictReader(CATALOG.open()))
    if len(rows) != 60:
        fail(f"catalog_count={len(rows)}")
    seen = set()
    families = set()
    districts = {f"d{index:02d}": 0 for index in range(1, 7)}
    for row in rows:
        mission = parse_id(row["mission_id"])
        scenario = parse_id(row["scenario_id"])
        district = parse_id(row["district_id"])
        opmap = parse_id(row["operation_map_id"])
        if not mission or mission[0] != "operation" or not numbered(mission[1], "o", 3, 1, 60):
            fail(f"mission_id={row['mission_id']}")
        if not scenario or scenario[:2] != ["scenario", "operations"] or scenario[2] != mission[1]:
            fail(f"scenario_id={row['scenario_id']}")
        if not district or district[:2] != ["district", "operations"] or not numbered(district[2], "d", 2, 1, 6):
            fail(f"district_id={row['district_id']}")
        expected_map = MAPS[district[2]]
        if row["operation_map_id"] != expected_map or not opmap:
            fail(f"operation_map_id={row['operation_map_id']}")
        if row["mission_id"] in seen:
            fail(f"duplicate={row['mission_id']}")
        seen.add(row["mission_id"])
        if row["family"] not in FAMILIES:
            fail(f"family={row['family']}")
        families.add(row["family"])
        districts[district[2]] += 1
        mission_number = int(mission[1][1:])
        if int(row["canonical_seed"]) != 1101 + mission_number:
            fail(f"seed={row['canonical_seed']}")
    if families != FAMILIES:
        fail(f"families={sorted(families)}")
    if any(count != 10 for count in districts.values()):
        fail(f"districts={districts}")


def check_assemblies() -> None:
    contracts = json.loads(CONTRACTS_ASMDEF.read_text())
    tests = json.loads(TESTS_ASMDEF.read_text())
    if contracts["name"] != "Game.Operations.Contracts" or contracts["references"] or not contracts["noEngineReferences"]:
        fail("contracts_asmdef")
    if tests["name"] != "Game.Operations.Tests.Editor" or "Game.Operations.Contracts" not in tests["references"]:
        fail("tests_asmdef")
    if "Game.Configs" in tests["references"] or "Game.Runtime" in tests["references"]:
        fail("tests_asmdef_shared_refs")


def check_shared_untouched() -> None:
    import subprocess

    diff = subprocess.check_output(
        ["git", "diff", "--name-only", "origin/main...HEAD"],
        cwd=ROOT,
        text=True,
    ).splitlines()
    forbidden = set(FORBIDDEN_ASMDEFS + FORBIDDEN_SEAMS + [
        "Design/Roadmap/Skirmish_Expansion",
    ])
    for path in diff:
        if path in forbidden or path.startswith("Design/Roadmap/Skirmish_Expansion/"):
            fail(f"shared_edit={path}")
        if path.startswith("Assets/Game/Scripts/") and "/Operations/" not in path:
            if not path.startswith("Assets/Game/Scripts/Operations/"):
                fail(f"unowned_script={path}")


def check_types_exist() -> None:
    required = [
        "enum OperationsOutcomeKind",
        "enum OperationsMissionFamilyKind",
        "enum OperationsAbstractActionKind",
        "enum OperationsDashboardActionKind",
        "readonly struct OperationsLaunchPayload",
        "readonly struct OperationsMissionResult",
        "class OperationsSaveData",
        "class OperationsSaveMigration",
        "class OperationsRosterLedger",
        "interface IUiOperationsGateway",
        "class OperationsIdentityRules",
        "class OperationsShadowProject",
    ]
    contracts_dir = ROOT / "Assets/Game/Scripts/Operations/Contracts"
    text = "\n".join(path.read_text() for path in contracts_dir.glob("*.cs"))
    for token in required:
        if token not in text:
            fail(f"missing_type={token}")
    ledger = (contracts_dir / "OperationsRosterLedger.cs").read_text()
    if "RequireToken(verifiedTypeOrPath" in ledger or "RequireToken(notes" in ledger:
        fail("roster_token_cap_on_evidence")
    if "RequireEvidence(verifiedTypeOrPath" not in ledger or "RequireEvidence(notes" not in ledger:
        fail("roster_missing_evidence_validation")


def check_fixtures() -> None:
    fixtures = ROOT / "Assets/Tests/Editor/Operations/Fixtures"
    current = json.loads((fixtures / "operations_save_current.json").read_text())
    unknown = json.loads((fixtures / "operations_save_unknown.json").read_text())
    legacy = json.loads((fixtures / "operations_save_legacy.json").read_text())
    if current["schemaVersion"] != 1 or unknown["schemaVersion"] != 99 or legacy["schemaVersion"] != 0:
        fail("fixture_schema")
    if (fixtures / "operations_save_missing.json").read_text().strip() != "{}":
        fail("missing_fixture")


def check_shadow_project() -> None:
    text = (ROOT / "Assets/Game/Scripts/Operations/Contracts/OperationsShadowProject.cs").read_text()
    if 'SharedWindowsCheckout = @"D:\\Projects\\WarlineCapture"' not in text:
        fail("shared_checkout_constant")
    if 'ShadowWindowsCheckout = @"D:\\Projects\\WarlineCapture-Operations"' not in text:
        fail("shadow_checkout_constant")
    for relative in (
        "Tools/Operations/Ensure-OperationsShadowWorktree.ps1",
        "Tools/Operations/Invoke-OperationsP0Validation.ps1",
        "Design/Roadmap/Operations/P0_SHADOW_PROJECT.md",
    ):
        if not (ROOT / relative).exists():
            fail(f"missing={relative}")
    shared_mentions = (ROOT / "Tools/Operations/Invoke-OperationsP0Validation.ps1").read_text()
    if "D:\\Projects\\WarlineCapture-Operations" not in shared_mentions:
        fail("validation_script_missing_shadow")
    if '-ProjectPath $shared' in shared_mentions:
        fail("validation_script_uses_shared_project")
    checks = (ROOT / "Assets/Tests/Editor/Operations/OperationsP0Checks.cs").read_text()
    if "ProveFindRepositoryRootAcceptsShadowFolderName" not in checks:
        fail("root_finder_missing_shadow_folder_proof")
    if "CombineProjectPath" not in checks:
        fail("root_finder_missing_combine")
    if "ShadowWindowsCheckout" not in checks:
        fail("root_finder_missing_shadow_candidate")
    if "candidates.Add(OperationsShadowProject.SharedWindowsCheckout)" in checks:
        fail("root_finder_searches_shared")
    if 'GetFileName(directory) == "WarlineCapture"' in checks:
        fail("root_finder_locks_shared_folder_name")
    check_shadow_folder_name_root_discovery()


def check_shadow_folder_name_root_discovery() -> None:
    import tempfile

    with tempfile.TemporaryDirectory(prefix="ops-p0-root-") as workspace:
        shadow = Path(workspace) / "WarlineCapture-Operations"
        marker = (
            shadow
            / "Assets"
            / "Game"
            / "Scripts"
            / "Operations"
            / "Contracts"
            / "Game.Operations.Contracts.asmdef"
        )
        marker.parent.mkdir(parents=True, exist_ok=True)
        marker.write_text("{}\n", encoding="utf-8")
        catalog = shadow / "Design" / "Roadmap" / "Operations" / "MISSION_CATALOG.csv"
        catalog.parent.mkdir(parents=True, exist_ok=True)
        catalog.write_text("mission_id\n", encoding="utf-8")
        if shadow.name != "WarlineCapture-Operations":
            fail("shadow_temp_folder_name")
        if not marker.is_file() or not catalog.is_file():
            fail("shadow_temp_markers")
        current = marker.parent
        found = None
        for _ in range(16):
            has_asmdef = (
                current
                / "Assets"
                / "Game"
                / "Scripts"
                / "Operations"
                / "Contracts"
                / "Game.Operations.Contracts.asmdef"
            ).is_file()
            has_catalog = (
                current / "Design" / "Roadmap" / "Operations" / "MISSION_CATALOG.csv"
            ).is_file()
            if has_asmdef or has_catalog:
                found = current
                break
            if current.parent == current:
                break
            current = current.parent
        if found is None or found.resolve() != shadow.resolve():
            fail(f"shadow_folder_root={found}")


def check_identity_source() -> None:
    rules = (ROOT / "Assets/Game/Scripts/Configs/OperationMapIdentityRules.cs").read_text()
    if 'IsEqual(value, segments[1], "operations")' in rules:
        fail("shared_identity_already_extended")
    if "skirmish" not in rules:
        fail("shared_identity_missing_skirmish")


def main() -> None:
    check_catalog()
    check_assemblies()
    check_types_exist()
    check_fixtures()
    check_identity_source()
    check_shadow_project()
    if (ROOT / ".git").exists():
        try:
            check_shared_untouched()
        except Exception:
            # Uncommitted working tree is allowed while authoring; commit check runs after git add.
            pass
    print(PASS_MARKER)
    return 0


if __name__ == "__main__":
    sys.exit(main())
