from __future__ import annotations

import copy
import hashlib
import importlib.util
import json
from pathlib import Path
import tempfile
import unittest


ROOT = Path(__file__).resolve().parents[5]
HERE = Path(__file__).resolve().parent
BASE_GRAPH = ROOT / "T Logs/SkirmishProduction/s002-source-registries-graph-20261104-T3/source-config-graph.json"
BASE_PROOF = ROOT / "T Logs/SkirmishProduction/s002-source-registries-graph-20261104-T3/graph-proof.json"
PREPARER = HERE.parent / "selected_operation_map" / "prepare_inputs.py"
GRID_OWNER = HERE / "run_s002_pinned_physical_input_projection.py"
GRAPH_ATTESTATION_OWNER = HERE / "generate_s002_selected_map_graph_attestation.py"
GRID_PATH = "Assets/Game/Configs/Scene/MatchSubScene_GridAuthoring_Config.asset"
SURFACE_PATH = "Assets/Game/Data/MapSurfaces/Match_Map_MapSurfaceData.asset"


def module(path: Path, name: str):
    spec = importlib.util.spec_from_file_location(name, path)
    assert spec is not None and spec.loader is not None
    value = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(value)
    return value


def digest(raw: bytes) -> str:
    return hashlib.sha256(raw).hexdigest()


class SelectedMapGridGraphExtensionTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.base = json.loads(BASE_GRAPH.read_text(encoding="utf-8"))
        cls.base_proof = json.loads(BASE_PROOF.read_text(encoding="utf-8"))
        cls.base_sha = digest(BASE_GRAPH.read_bytes())
        cls.base_proof_sha = digest(BASE_PROOF.read_bytes())
        preparer = module(PREPARER, "s002_test_map_preparer")
        with tempfile.TemporaryDirectory(prefix="s002-grid-extension-test-") as temp:
            output = Path(temp) / "prepared"
            cls.selected_map_proof = preparer.generate(ROOT, output, ROOT, BASE_GRAPH, None)
            cls.selected_map_graph_bytes = (output / "source-config-graph.json").read_bytes()
            cls.selected_map_graph = json.loads(cls.selected_map_graph_bytes)
        cls.owner = module(GRID_OWNER, "s002_test_grid_owner")
        cls.parent_sha = digest(cls.selected_map_graph_bytes)

    def extend(self, graph=None, proof=None, **kwargs):
        return self.owner.add_grid_node(
            ROOT, copy.deepcopy(graph or self.selected_map_graph),
            copy.deepcopy(proof or self.base_proof),
            base_graph_path=BASE_GRAPH,
            base_graph_sha256=self.base_sha,
            parent_graph_sha256=self.parent_sha,
            base_graph_proof_sha256=self.base_proof_sha,
            **kwargs)

    def test_parser_adds_exact_initialize_grid_node_and_pins(self):
        result = self.extend()
        rows = [row for row in result["nodes"] if row.get("source_path") == GRID_PATH]
        self.assertEqual(1, len(rows))
        self.assertEqual("GridAuthoringSceneConfigAsset", rows[0]["source_type"])
        self.assertEqual(self.owner.GRID_GUID, rows[0]["asset_guid"])
        self.assertEqual(self.owner.GRID_SHA, rows[0]["source_sha256"])
        self.assertEqual(1, len([row for row in result["nodes"] if row.get("source_path") == SURFACE_PATH]))
        self.assertEqual(len(self.base["nodes"]) + 3, len(result["nodes"]))
        extension = result["source_graph_extension"]
        self.assertEqual(self.base_sha, extension["base_graph_sha256"])
        self.assertEqual(self.base_proof_sha, extension["base_graph_proof_sha256"])
        self.assertEqual(self.parent_sha, extension["parent_graph_sha256"])
        self.assertEqual("a101000000000000000000000000000b", extension["added_script_guid"])
        for path in (GRID_PATH, GRID_PATH + ".meta", extension["added_script_path"],
                     extension["added_script_path"] + ".meta"):
            absolute = str(ROOT / path)
            self.assertEqual(digest((ROOT / path).read_bytes()), result["source_pins"][absolute])

    def test_grid_target_guid_mismatch_rejected(self):
        altered = copy.deepcopy(self.base_proof)
        altered["geometry_source_targets"]["gridMetadata"]["guid"] = "0" * 32
        with self.assertRaises(ValueError):
            self.extend(proof=altered)

    def test_unrelated_base_proof_rejected(self):
        altered = copy.deepcopy(self.base_proof)
        altered["graph"]["sha256"] = "0" * 64
        with self.assertRaises(ValueError):
            self.extend(proof=altered)

    def test_generator_recipe_proof_has_no_ephemeral_output_pins(self):
        generator = module(GRAPH_ATTESTATION_OWNER, "s002_test_grid_attestation_owner")
        with tempfile.TemporaryDirectory(prefix="s002-grid-recipe-lifecycle-") as temp:
            recipe_root = Path(temp).resolve()
            detail = generator.generate(ROOT, recipe_root / "generated")
            self.assertTrue(detail["derived_package_path"].startswith(str(ROOT)))
            self.assertNotIn("attestation_source_path", detail)
            self.assertTrue(detail["attestation_source_sha256"])
            self.assertTrue(all(Path(path).is_relative_to(ROOT) for path in detail["source_pins"]))
            self.assertFalse(any(str(recipe_root) in path for path in detail["source_pins"]))
            self.assertNotIn(str(recipe_root), json.dumps(detail))

    def test_duplicate_grid_node_rejected(self):
        altered = copy.deepcopy(self.selected_map_graph)
        altered["nodes"].append({"asset_guid": self.owner.GRID_GUID, "source_path": GRID_PATH})
        with self.assertRaises(ValueError):
            self.extend(graph=altered)

    def test_conflicting_grid_source_pin_rejected(self):
        altered = copy.deepcopy(self.selected_map_graph)
        altered["source_pins"][str(ROOT / GRID_PATH)] = "0" * 64
        with self.assertRaises(ValueError):
            self.extend(graph=altered)


if __name__ == "__main__":
    unittest.main()
