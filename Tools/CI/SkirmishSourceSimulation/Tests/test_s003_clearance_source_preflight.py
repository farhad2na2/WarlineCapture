import hashlib
import importlib.util
import json
from pathlib import Path
import sys
import tempfile
import types
import unittest
from unittest.mock import patch


ROOT = Path(__file__).resolve().parents[4]
sys.path.insert(0, str(ROOT / "Tools/CI"))
def _no_duplicates(pairs):
    value = {}
    for key, item in pairs:
        if key in value:
            raise ValueError("duplicate JSON key")
        value[key] = item
    return value

syntax = types.SimpleNamespace(no_duplicates=_no_duplicates)
owned = types.ModuleType("skirmish_owned_source")
owned.require = lambda name: syntax
profiles = types.ModuleType("skirmish_source_profiles")
profiles.require_profile = lambda *args: None
build = types.ModuleType("skirmish_source_build")
def _need(condition, message):
    if not condition:
        raise ValueError(message)
build.need = _need
build.sha = lambda raw: hashlib.sha256(raw).hexdigest()
build.pin = lambda path: {"path": str(Path(path).resolve()), "sha256": hashlib.sha256(Path(path).read_bytes()).hexdigest(), "bytes": Path(path).stat().st_size}
sys.modules.update({"skirmish_owned_source": owned, "skirmish_source_profiles": profiles, "skirmish_source_build": build})
SCRIPT = ROOT / "Tools/CI/skirmish_source_smoke.py"
SPEC = importlib.util.spec_from_file_location("s003_smoke_preflight", SCRIPT)
SMOKE = importlib.util.module_from_spec(SPEC)
SPEC.loader.exec_module(SMOKE)


class S003ClearanceSourcePreflightTests(unittest.TestCase):
    def setUp(self):
        self.temp = tempfile.TemporaryDirectory()
        self.root = Path(self.temp.name)
        self.source = self.root / "source.json"
        self.scenes = [
            "Assets/Game/Scenes/Match/MatchRuntimeSubScene.unity",
            "Assets/Game/Scenes/OperationMaps/Skirmish/Candidates/opmap_skirmish_desert_base_01_entity_presentation_dense_city_candidate.unity",
            "Assets/Game/GeneratedOperationMaps/RuntimeBinding/opmap.skirmish.desert_base_01/opmap_skirmish_desert_base_01_runtime.unity",
        ]
        self.source.write_text(json.dumps({"kind": "AuthoredSkirmishReferenceData", "source_scenes": self.scenes}), encoding="utf-8")
        self.sha = hashlib.sha256(self.source.read_bytes()).hexdigest()
        self.projection = self.root / "projection.json"

    def tearDown(self):
        self.temp.cleanup()

    def bundle(self, embedded_path, descriptor=None):
        self.projection.write_text(json.dumps({
            "schema_version": 1,
            "kind": "GeneratedSourceStaticClearanceProjection",
            "source_sha256": self.sha,
            "source_path": str(embedded_path),
            "source_record": {
                "authority": "OriginalRenderBoundsPresentation.CollectStaticBounds_SourceBakedDomain",
                "projection_source": "Unity.Rendering.RenderBoundsUpdateSystem.BoundsJob:AABB.Transform(LocalToWorld,RenderBounds)",
                "grid_width": 2048, "grid_height": 1024, "grid_origin": [0.0, 0.0, 0.0], "cell_size": 1.0,
                "query_rect": [-118.0, -118.0, 2284.0, 1260.0], "minimum_extent": 0.25, "maximum_extent": 20.0,
                "source_search_radius_cells": 96, "source_padding_cells": 2, "bounds": [{}] * 48457,
            },
        }), encoding="utf-8")
        value = {"static_clearance": {"path": str(self.projection), "sha256": hashlib.sha256(self.projection.read_bytes()).hexdigest()}}
        if descriptor is not None:
            value["static_clearance_source_artifact"] = descriptor
        return value

    def test_legacy_embedded_source_path_is_accepted_when_bytes_match(self):
        bundle = self.bundle(self.source)
        with patch.object(SMOKE, "S003_CLEARANCE_SOURCE_SHA", self.sha), patch.object(SMOKE, "S003_CLEARANCE_SOURCE_BYTES", self.source.stat().st_size):
            result = SMOKE._s003_clearance_source(bundle)
        self.assertEqual(result["sha256"], self.sha)
        self.assertEqual(result["bytes"], self.source.stat().st_size)

    def test_legacy_missing_embedded_source_fails_preflight(self):
        bundle = self.bundle(self.root / "missing-source.json")
        with patch.object(SMOKE, "S003_CLEARANCE_SOURCE_SHA", self.sha), patch.object(SMOKE, "S003_CLEARANCE_SOURCE_BYTES", self.source.stat().st_size):
            with self.assertRaisesRegex((ValueError, FileNotFoundError, OSError), ".*"):
                SMOKE._s003_clearance_source(bundle)

    def test_explicit_descriptor_resolves_source_when_embedded_path_is_stale(self):
        bundle = self.bundle(self.root / "stale-private-tmp.json", {
            "path": str(self.source), "sha256": self.sha, "bytes": self.source.stat().st_size,
        })
        with patch.object(SMOKE, "S003_CLEARANCE_SOURCE_SHA", self.sha), patch.object(SMOKE, "S003_CLEARANCE_SOURCE_BYTES", self.source.stat().st_size):
            result = SMOKE._s003_clearance_source(bundle)
        self.assertEqual(result["path"], str(self.source.resolve()))

    def test_explicit_descriptor_with_wrong_hash_is_rejected(self):
        bundle = self.bundle(self.source, {
            "path": str(self.source), "sha256": "0" * 64, "bytes": self.source.stat().st_size,
        })
        with patch.object(SMOKE, "S003_CLEARANCE_SOURCE_SHA", self.sha), patch.object(SMOKE, "S003_CLEARANCE_SOURCE_BYTES", self.source.stat().st_size):
            with self.assertRaisesRegex(ValueError, ".*"):
                SMOKE._s003_clearance_source(bundle)

    def test_noncanonical_source_scene_identity_is_rejected_even_when_pinned(self):
        wrong = self.root / "wrong-source.json"
        wrong.write_text(json.dumps({"kind": "AuthoredSkirmishReferenceData", "source_scenes": ["Assets/OtherMap.unity"]}), encoding="utf-8")
        digest = hashlib.sha256(wrong.read_bytes()).hexdigest()
        self.sha = digest
        bundle = self.bundle(wrong, {"path": str(wrong), "sha256": digest, "bytes": wrong.stat().st_size})
        with patch.object(SMOKE, "S003_CLEARANCE_SOURCE_SHA", digest), patch.object(SMOKE, "S003_CLEARANCE_SOURCE_BYTES", wrong.stat().st_size):
            with self.assertRaisesRegex(ValueError, "canonical map scene identity"):
                SMOKE._s003_clearance_source(bundle)


if __name__ == "__main__":
    unittest.main()
