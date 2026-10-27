"""Fast header-shape controls only; no native provenance or gameplay qualification."""
import copy
import json
import sys
import tempfile
import unittest
from pathlib import Path
from unittest.mock import patch

sys.path.insert(0, str(Path(__file__).resolve().parents[1]))
import skirmish_source_build as m


class DerivedProjectionHeaderPrecheckTests(unittest.TestCase):
    def setUp(self):
        self.temp = tempfile.TemporaryDirectory()
        self.addCleanup(self.temp.cleanup)
        self.root = Path(self.temp.name)
        self.initial = {"metadata_compiler_context": {"path": "context-not-read-here"}}
        self.headers = {}
        for name, kind in [
            ("building_definitions", "CompilerContextDerivedSkirmishBuildingDefinitions"),
            ("unit_production_metadata", "CompilerContextDerivedSkirmishUnitProductionMetadata"),
            ("building_request_metadata", "CompilerContextDerivedConfiguredBuildingMetadata"),
        ]:
            self.headers[name] = {
                "schema_version": 3, "kind": kind,
                "authority": "OriginalCompilerContext_ImmutableNativeFactoryFacts_NotNativeS050",
                "native_origin": {
                    "kind": "NativeS026LogicalBoundMetadataQualification",
                    "artifact_sha256": "da1383237b7ec9bb0607596be496edc40d6a1cd9c15c12901d7dbee9731b5f37",
                },
            }
        self.save()

    def save(self):
        for name, header in self.headers.items():
            path = self.root / (name + ".json")
            path.write_text(json.dumps(header))
            self.initial[name] = m.pin(path)

    def assert_bad_path(self, name, field):
        with self.assertRaises(ValueError) as error:
            m.derived_projection_header_pins(self.initial)
        self.assertIn("$." + name + ".native_origin" + field + ":", str(error.exception))

    def test_current_three_header_shapes_are_pinned_without_qualification(self):
        pins = m.derived_projection_header_pins(self.initial)
        self.assertEqual(pins, {v["path"]: v["sha256"] for k, v in self.initial.items() if k in self.headers})

    def test_each_missing_kind_reports_its_exact_path(self):
        originals = copy.deepcopy(self.headers)
        for name in self.headers:
            with self.subTest(name=name):
                self.headers = copy.deepcopy(originals)
                del self.headers[name]["native_origin"]["kind"]
                self.save()
                self.assert_bad_path(name, ".kind")

    def test_each_missing_artifact_hash_reports_its_exact_path(self):
        originals = copy.deepcopy(self.headers)
        for name in self.headers:
            with self.subTest(name=name):
                self.headers = copy.deepcopy(originals)
                del self.headers[name]["native_origin"]["artifact_sha256"]
                self.save()
                self.assert_bad_path(name, ".artifact_sha256")

    def test_invalid_origin_kinds_and_hash_shapes_reject(self):
        original = copy.deepcopy(self.headers)
        name = "building_definitions"
        for field, values in [("kind", [None, 7, "", "  "]),
                              ("artifact_sha256", [None, True, "bad", "A" * 64, "a" * 63])]:
            for value in values:
                with self.subTest(field=field, value=value):
                    self.headers = copy.deepcopy(original)
                    self.headers[name]["native_origin"][field] = value
                    self.save()
                    self.assert_bad_path(name, "." + field)

    def test_missing_origin_object_reports_its_path(self):
        del self.headers["building_definitions"]["native_origin"]
        self.save()
        self.assert_bad_path("building_definitions", "")

    def test_legacy_absence_does_not_read_or_require_new_headers(self):
        del self.initial["metadata_compiler_context"]
        with patch.object(m, "read_json_pinned", side_effect=AssertionError("legacy headers must not be read")):
            self.assertEqual(m.derived_projection_header_pins(self.initial), {})

    def test_unsupplied_headers_do_not_gain_a_new_requirement(self):
        self.assertEqual(m.derived_projection_header_pins({"metadata_compiler_context": {}}), {})

    def test_legacy_or_unsupported_header_is_left_to_original_reader(self):
        for header in self.headers.values():
            header["schema_version"] = 2
            del header["native_origin"]
        self.save()
        self.assertEqual(len(m.derived_projection_header_pins(self.initial)), 3)

    def test_descriptor_hash_drift_still_fails_before_shape_check(self):
        self.initial["building_definitions"]["sha256"] = "0" * 64
        with self.assertRaisesRegex(ValueError, "Input changed: initialization.building_definitions"):
            m.derived_projection_header_pins(self.initial)

    def test_semantic_origin_equality_remains_original_csharp_authority(self):
        self.headers["building_definitions"]["native_origin"]["kind"] = "NonemptyButUnqualifiedKind"
        self.headers["building_definitions"]["native_origin"]["artifact_sha256"] = "a" * 64
        self.save()
        self.assertEqual(len(m.derived_projection_header_pins(self.initial)), 3)
