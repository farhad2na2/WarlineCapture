#!/usr/bin/env python3
"""Prepare an opt-in S004 data-only context admission candidate.

The original compiler, catalog-row/session matching, dispatch and policy bodies
are untouched. This generated overlay is not runtime/native admission.
"""
import argparse
import hashlib
import json
from pathlib import Path

ROOT = Path(__file__).resolve().parents[5]
INPUTS = ["Assets/Game/Scripts/Simulation/GroundMissionContext.cs",
          "Assets/Game/Scripts/Runtime/Skirmish/GroundMissionContextFactory.cs"]


def sha(path): return hashlib.sha256(path.read_bytes()).hexdigest()


def change_once(text, old, new):
    if text.count(old) != 1: raise ValueError("Exact source identity guard changed")
    return text.replace(old, new)


def generate(output):
    sources = [ROOT / name for name in INPUTS]
    original = [p.read_text() for p in sources]
    context_old = 'bool codedS003Air=CatalogId=="S003"&&DefinitionId=="skirmish.s003"&&ArmyProfileId==SkirmishArmyProfileId.AirMobile&&StartPackageId==SkirmishStartPackageId.FieldBase&&Readiness==SkirmishReadinessStage.Field;'
    context_new = context_old[:-1] + ' || (CatalogId=="S004"&&DefinitionId=="skirmish.s004"&&PlayableScenarioIndex==6&&ArmyProfileId==SkirmishArmyProfileId.AirMobile&&StartPackageId==SkirmishStartPackageId.EstablishedBase&&Readiness==SkirmishReadinessStage.Established);'
    factory_old = '''bool codedS003Air = setup.CatalogId == "S003" && setup.DefinitionId == "skirmish.s003" &&
                setup.ArmyProfileId == SkirmishArmyProfileId.AirMobile && setup.StartPackageId == SkirmishStartPackageId.FieldBase &&
                setup.Readiness == SkirmishReadinessStage.Field && setup.EnemyPolicy == null &&
                setup.LayoutBindingFingerprint == string.Empty && setup.MissionLogicalBindingFingerprint == string.Empty;'''
    factory_new = factory_old[:-1] + ''' ||
                (setup.CatalogId == "S004" && setup.DefinitionId == "skirmish.s004" && playableScenarioIndex == 6 &&
                setup.ArmyProfileId == SkirmishArmyProfileId.AirMobile && setup.StartPackageId == SkirmishStartPackageId.EstablishedBase &&
                setup.Readiness == SkirmishReadinessStage.Established && setup.EnemyPolicy == null &&
                setup.LayoutBindingFingerprint == string.Empty && setup.MissionLogicalBindingFingerprint == string.Empty);'''
    changed = [change_once(original[0], context_old, context_new), change_once(original[1], factory_old, factory_new)]
    output.mkdir(parents=True, exist_ok=False)
    files = []
    for path, text in zip(sources, changed):
        target = output / path.name; target.write_text(text); files.append(target)
    receipt = {"status": "Prepared", "kind": "S004LogicalContextSourceCandidate", "scope": "IdentityGuardOnly_OptInSourceDiagnostic",
        "source_sha256": {str(p): sha(p) for p in sources}, "output_sha256": {str(p): sha(p) for p in files},
        "original_compiler_catalog_and_session_guards_preserved": True, "policy_parity_proven": False,
        "native_parity": False, "mission_ready": False}
    (output / "receipt.json").write_text(json.dumps(receipt, indent=2) + "\n")
    return receipt


if __name__ == "__main__":
    parser=argparse.ArgumentParser(); parser.add_argument("--output",type=Path,required=True)
    print(json.dumps(generate(parser.parse_args().output.resolve())))
