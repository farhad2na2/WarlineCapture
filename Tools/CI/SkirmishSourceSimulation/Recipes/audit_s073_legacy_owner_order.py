"""Audit a candidate S073 legacy owner ordering against current source headers.

This reports constraints and one legal linearization only. It does not execute
systems or assert Unity's observed runtime order.
"""
import argparse
import importlib.util
import json
from pathlib import Path


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--repo", type=Path, required=True)
    parser.add_argument("--output", type=Path, required=True)
    args = parser.parse_args()
    repo = args.repo.resolve(strict=True)
    out = args.output.absolute()
    if out.exists():
        raise FileExistsError("Fresh owner-order audit output required")
    generator = repo / "Tools/CI/SkirmishSourceSimulation/Recipes/regenerate_owner_order.py"
    spec = importlib.util.spec_from_file_location("s073_owner_order_generator", generator)
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    module.PHASES = [*module.PHASES, ("legacy_rules", ["SkirmishRulesSystem"])]
    proof = module.generate(repo, out)
    data = json.loads((out / "source-owner-order-inputs.json").read_text())
    legacy = data["records"]["SkirmishRulesSystem"]
    expanded = data["records"]["SkirmishOutcomeSystem"]
    if not legacy["source"] or not legacy["order_last"] or not expanded["order_last"]:
        raise ValueError("Current legacy and expanded OrderLast metadata required")
    result = {"kind": "S073LegacyCandidateOwnerOrderAudit", "status": "Passed",
              "legacy_rules_source": legacy["source"], "legacy_rules_order_last": True,
              "expanded_outcome_order_last": True,
              "native_runtime_total_order_observed": False,
              "scheduler_executed": False, "mission_ready": False,
              "scope": "Header constraints and one legal candidate only",
              "candidate_phases": proof["phase_types"],
              "source_header_pins": proof["referenced_header_pins"]}
    (out / "result.json").write_text(json.dumps(result, indent=2) + "\n")
    print("[S073LegacyOwnerOrderAudit] result=Passed candidateOnly=1 runtimeOrderObserved=0 schedulerExecuted=0")


if __name__ == "__main__":
    main()
