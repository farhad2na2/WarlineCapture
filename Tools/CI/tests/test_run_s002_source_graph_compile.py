import hashlib
import importlib.util
import json
import tempfile
import unittest
from pathlib import Path

SCRIPT = Path(__file__).resolve().parents[1] / "run_s002_source_graph_compile.py"
SPEC = importlib.util.spec_from_file_location("s002_source_graph_compile", SCRIPT)
MODULE = importlib.util.module_from_spec(SPEC)
assert SPEC and SPEC.loader
SPEC.loader.exec_module(MODULE)


def sha(path):
    return hashlib.sha256(Path(path).read_bytes()).hexdigest()


def result():
    return {
        "kind": "S002SourceGraphCompileResult", "mission": "S002", "seed": 104731,
        "status": "ConfigurationCompiled", "compiler_accepted": True,
        "operation_map_id": "opmap.skirmish.desert_base_01",
        "selected_map_source": "opmap.skirmish.desert_base_01",
        "source_graph_sha256": "a" * 64, "setup_matrix_sha256": "b" * 64,
        "compiler_reasons": [], "setup_hash": 123456,
        "world_allocated": False, "geometry_materialized": False,
        "source_fit_verified": False, "native_acceptance": False, "mission_ready": False,
    }


class S002SourceGraphCompileRunnerTests(unittest.TestCase):
    def test_only_source_compiler_outcome_with_false_qualification_is_admitted(self):
        self.assertTrue(MODULE.validate_result(result()))
        changed = result()
        changed["mission_ready"] = True
        self.assertFalse(MODULE.validate_result(changed))
        changed = result()
        changed["compiler_accepted"] = False
        self.assertFalse(MODULE.validate_result(changed))
        changed = result()
        changed["native_acceptance"] = True
        self.assertFalse(MODULE.validate_result(changed))

    def test_build_receipt_pin_drift_is_rejected(self):
        with tempfile.TemporaryDirectory(prefix="s002-compiler-build-pin-") as temp:
            root = Path(temp).resolve()
            source = root / "owner.cs"
            source.write_text("current source")
            manifest = root / "manifest.json"
            manifest.write_text("{}\n")
            executable = root / "SourceSimulation.exe"
            executable.write_bytes(b"compiled")
            response = root / "compile.rsp"
            response.write_text("-main:SourceSimulationProgram\n")
            receipt = {
                "status": "Passed", "mission": "S002", "seed": 104731,
                "repository_root": str(root),
                "source_and_reference_pins": {str(source): sha(source)},
                "executable": {"path": str(executable), "sha256": sha(executable)},
                "compiler_response": {"path": str(response), "sha256": sha(response)},
                "canonical_source_manifest": {"path": str(manifest), "sha256": sha(manifest)},
            }
            self.assertEqual(1, len(MODULE.verify_build_receipt(receipt, root)))
            source.write_text("changed source")
            with self.assertRaisesRegex(ValueError, "build source pin changed"):
                MODULE.verify_build_receipt(receipt, root)


if __name__ == "__main__":
    unittest.main()
