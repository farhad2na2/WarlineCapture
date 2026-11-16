#!/usr/bin/env python3
"""Focused schema and wiring controls for the optional S002 relocation receipt."""
import copy
import importlib.util
import tempfile
import unittest
from pathlib import Path

ROOT = Path(__file__).resolve().parents[4]
ADAPTER = ROOT / "Tools/CI/SkirmishSourceSimulation/Adapters/307_GeneratedS002StartingWorld.lifetime.cs"
PROJECTOR = ROOT / "Tools/CI/project_s002_static_clearance.py"
CLEARANCE_ADAPTER = ROOT / "Tools/CI/SkirmishSourceSimulation/Adapters/527_GeneratedS002DropClearance.cs"
CAPTURE = ROOT / "Logs/SkirmishProduction/s073-source-inputs-20261101-r1/campaign-physical-source.json"
SPEC = importlib.util.spec_from_file_location("project_s002_static_clearance", PROJECTOR)
PROJECTOR_MODULE = importlib.util.module_from_spec(SPEC)
SPEC.loader.exec_module(PROJECTOR_MODULE)


def admit_descriptor(value):
    """Mirror only the documented outer descriptor shape; C# loader owns receipt validation."""
    if not isinstance(value, dict) or set(value) not in (
        {"physical", "water", "startup"},
        {"physical", "water", "startup", "relocation"},
    ):
        raise ValueError("diagnostic fields")
    for key in ("physical", "water", "startup"):
        row = value[key]
        if not isinstance(row, dict) or set(row) != {"path", "sha256"}:
            raise ValueError(key)
        if not isinstance(row["path"], str) or not isinstance(row["sha256"], str):
            raise ValueError(key)
    if "relocation" in value:
        row = value["relocation"]
        if not isinstance(row, dict) or set(row) != {"path", "sha256"}:
            raise ValueError("relocation")
        if not isinstance(row["path"], str) or not Path(row["path"]).is_absolute():
            raise ValueError("relocation path")
        digest = row["sha256"]
        if not isinstance(digest, str) or len(digest) != 64 or any(c not in "0123456789abcdef" for c in digest):
            raise ValueError("relocation digest")


class DescriptorControls(unittest.TestCase):
    def setUp(self):
        self.base = {k: {"path": f"/capture/{k}.json", "sha256": "a" * 64}
                     for k in ("physical", "water", "startup")}
        self.receipt = Path("/capture/receipt.json")

    def test_legacy_three_descriptor_shape_remains_admitted(self):
        admit_descriptor(self.base)

    def test_exact_relocation_descriptor_admitted(self):
        value = dict(self.base)
        value["relocation"] = {"path": str(self.receipt), "sha256": "b" * 64}
        admit_descriptor(value)

    def test_extra_top_level_field_rejected(self):
        with self.assertRaises(ValueError):
            admit_descriptor({**self.base, "packed": {}})

    def test_malformed_relocation_fields_rejected(self):
        for row in ({"path": str(self.receipt)},
                    {"path": str(self.receipt), "sha256": "B" * 64},
                    {"path": "relative/receipt.json", "sha256": "b" * 64},
                    {"path": str(self.receipt), "sha256": "b" * 64, "extra": 1}):
            with self.subTest(row=row), self.assertRaises(ValueError):
                admit_descriptor({**self.base, "relocation": row})

    def test_adapter_keeps_legacy_call_and_forwards_both_receipt_pins(self):
        source = ADAPTER.read_text()
        self.assertIn("diagnostic.Count!=3&&diagnostic.Count!=4", source)
        self.assertIn("LoadS002SourceDiagnostic(", source)
        self.assertIn("selectedSourceRotationRoot,relocationPath,relocationSha", source)
        self.assertIn("diagnostic[\"relocation\"]", source)


class StaticClearanceProjectionControls(unittest.TestCase):
    def setUp(self):
        self.projection = PROJECTOR_MODULE.make_projection(CAPTURE)

    def test_projection_copies_complete_captured_record(self):
        PROJECTOR_MODULE.validate_projection(self.projection, CAPTURE)
        self.assertEqual(len(self.projection["source_record"]["bounds"]), 90865)
        self.assertFalse(self.projection["source_fit_certified"])
        self.assertFalse(self.projection["mission_ready"])

    def test_projection_rejects_changed_source_identity(self):
        changed = copy.deepcopy(self.projection)
        changed["source_sha256"] = "0" * 64
        with self.assertRaises(ValueError):
            PROJECTOR_MODULE.validate_projection(changed, CAPTURE)

    def test_projection_rejects_changed_bounds_or_readiness(self):
        changed = copy.deepcopy(self.projection)
        changed["source_record"]["bounds"].pop()
        with self.assertRaises(ValueError):
            PROJECTOR_MODULE.validate_projection(changed, CAPTURE)

    def test_projector_refuses_to_overwrite_existing_evidence(self):
        with tempfile.TemporaryDirectory() as directory:
            output = Path(directory) / "projection.json"
            sentinel = b"preserve existing evidence\n"
            output.write_bytes(sentinel)
            with self.assertRaises(FileExistsError):
                PROJECTOR_MODULE.write_projection(CAPTURE, output)
            self.assertEqual(output.read_bytes(), sentinel)

    def test_runtime_adapter_binds_projection_to_capture_and_exact_record(self):
        source = CLEARANCE_ADAPTER.read_text()
        self.assertIn('root["source_sha256"]=="8407889e4aff5c1f731c429fbae0063c5b6d99e1f7f51ab5c5c5bb0be29fa0b8"', source)
        self.assertIn('JToken.DeepEquals(record,native["fixedMapReadDomain"]?["dropClearanceBounds"])', source)
        self.assertIn('"CurrentS002SourceOnlyInactiveFixture_NoActiveProviderEquivalence"', source)
        self.assertIn('(bool?)root["source_fit_certified"]!=false', source)
        self.assertIn('(bool?)root["mission_ready"]!=false', source)
        changed = copy.deepcopy(self.projection)
        changed["mission_ready"] = True
        with self.assertRaises(ValueError):
            PROJECTOR_MODULE.validate_projection(changed, CAPTURE)


if __name__ == "__main__":
    unittest.main()
