import importlib.util
import json
import tempfile
import unittest
from pathlib import Path

SCRIPT = Path(__file__).with_name("generate_s002_authored_config_graph.py")
SPEC = importlib.util.spec_from_file_location("s002_authored_graph", SCRIPT)
MODULE = importlib.util.module_from_spec(SPEC)
assert SPEC and SPEC.loader
SPEC.loader.exec_module(MODULE)
ROOT = SCRIPT.resolve().parents[5]


class S002AuthoredConfigGraphTests(unittest.TestCase):
    def test_current_yaml_graph_and_physical_source_targets_are_pinned_without_world(self):
        with tempfile.TemporaryDirectory(prefix="s002-authored-graph-") as temp:
            output = Path(temp) / "graph"
            proof = MODULE.generate(ROOT, output)
            graph = json.loads(Path(proof["graph"]["path"]).read_text())

            self.assertEqual("S002CurrentAuthoredConfigGraph", proof["kind"])
            self.assertEqual("Tools/CI/SkirmishSourceSimulation/Recipes/s002_source_graph/generate_s002_authored_config_graph.py",
                             proof["generator"]["path"])
            self.assertEqual(MODULE.sha((ROOT / proof["generator"]["path"]).read_bytes()),
                             proof["generator"]["sha256"])
            self.assertTrue(proof["generator"]["unchanged_before_and_after_generation"])
            self.assertEqual("AuthoredConfigAssets", graph["authority"])
            self.assertFalse(graph["compiled_setup"])
            self.assertFalse(graph["world_initialized"])
            self.assertFalse(proof["mission_ready"])
            self.assertTrue(any(row["source_path"] == MODULE.SCENARIO for row in graph["nodes"]))
            map_row = next(row for row in graph["nodes"] if row["source_path"] == MODULE.S002_MAP)
            self.assertEqual("opmap.skirmish.desert_base_01", map_row["fields"]["operationMapId"])
            self.assertEqual("Assets/Game/Resources/SkirmishExpansionCatalog.asset", graph["catalog_path"])
            providers = graph["provider_registry_inputs"]
            self.assertEqual("S002SerializedProviderRegistryInputs", providers["kind"])
            self.assertIn("no importer execution", providers["scope"])
            self.assertEqual("Assets/Game/Resources/SkirmishBaseAssault.asset", providers["preset"]["path"])
            self.assertEqual("Assets/Game/Scenes/Match.unity", providers["match_scene"]["path"])
            self.assertTrue(providers["match_scene"]["game_object_component_link_verified"])
            self.assertTrue(providers["match_scene"]["building_placement_owner_usage_verified"])
            self.assertEqual("Assets/Game/Configs/Skirmish/Construction.asset",
                             providers["baseline_building_placement"]["path"])
            self.assertEqual("Assets/Game/Configs/Scene/Game_BuildingPlacement_Config.asset",
                             providers["scene_building_placement"]["path"])
            self.assertEqual("Assets/Game/Configs/Skirmish/UnitRegistry.asset",
                             providers["baseline_unit_registry"]["path"])
            self.assertEqual("Assets/Game/Configs/Scene/Game_UnitPrefabRegistry_Config.asset",
                             providers["scene_unit_registry"]["path"])
            self.assertIn("baseline_units", providers["ordered_rows"])
            self.assertIn("scene_units", providers["ordered_rows"])
            for rows in providers["ordered_rows"].values():
                self.assertTrue(rows)
                self.assertEqual(list(range(len(rows))), [row["index"] for row in rows])
                self.assertTrue(all(row["guid"] and row["file_id"] != 0 and row["type"] == 3 for row in rows))
                for row in rows:
                    self.assertEqual(row["prefab_sha256"], MODULE.sha((ROOT / row["path"]).read_bytes()))
                    self.assertEqual(row["meta_sha256"], MODULE.sha((ROOT / (row["path"] + ".meta")).read_bytes()))
            self.assertFalse(graph["compiled_setup"])
            self.assertFalse(graph["world_initialized"])
            for target in proof["geometry_source_targets"].values():
                path = ROOT / target["path"]
                self.assertTrue(path.is_file())
                self.assertEqual(target["sha256"], MODULE.sha(path.read_bytes()))
            for path, expected in graph["source_pins"].items():
                self.assertEqual(expected, MODULE.sha(Path(path).read_bytes()), path)

    def test_tampered_or_missing_map_guid_target_is_rejected(self):
        h = MODULE._generic()
        with tempfile.TemporaryDirectory(prefix="s002-missing-map-guid-") as temp:
            assets = Path(temp) / "Assets"
            assets.mkdir()
            source = assets / "source.asset"
            source.write_text("%YAML 1.1\n--- !u!114 &11400000\nMonoBehaviour: {}\n")
            (assets / "source.asset.meta").write_text("guid: " + "a" * 32 + "\n")
            with self.assertRaisesRegex(ValueError, "GUID must resolve to exactly one current asset"):
                MODULE.unique_guid_target(h, assets, "b" * 32, "map.surfaceMetadata")
            (assets / "duplicate.asset.meta").write_text("guid: " + "a" * 32 + "\n")
            with self.assertRaisesRegex(ValueError, "GUID must resolve to exactly one current asset"):
                MODULE.unique_guid_target(h, assets, "a" * 32, "map.gridMetadata")

    def test_duplicate_unity_document_anchor_is_rejected(self):
        h = MODULE._generic()
        with tempfile.TemporaryDirectory(prefix="s002-duplicate-anchor-") as temp:
            asset = Path(temp) / "map.asset"
            asset.write_text("%YAML 1.1\n--- !u!114 &11400000\nA: 1\n--- !u!114 &11400000\nB: 2\n")
            with self.assertRaisesRegex(ValueError, "must identify exactly one YAML document"):
                MODULE.require_unique_anchor(h, asset, 11400000, "map.operationMap")

    def test_match_scene_binding_rejects_missing_duplicate_or_wrong_script(self):
        guid = "a" * 32
        component = {"m_Script": {"fileID": 11500000, "guid": guid, "type": 3},
                     "buildingPlacementConfig": {"fileID": 11400000, "guid": "b" * 32, "type": 2}}
        self.assertEqual(114, MODULE.select_scene_component([(114, component)], guid)[0])
        with self.assertRaisesRegex(ValueError, "exactly one GUID-bound"):
            MODULE.select_scene_component([], guid)
        with self.assertRaisesRegex(ValueError, "exactly one GUID-bound"):
            MODULE.select_scene_component([(114, component), (221, component)], guid)
        wrong = {"m_Script": {"fileID": 11500000, "guid": "c" * 32, "type": 3}}
        with self.assertRaisesRegex(ValueError, "exactly one GUID-bound"):
            MODULE.select_scene_component([(114, wrong)], guid)

    def test_match_scene_gameobject_component_link_is_exact(self):
        component = {"m_GameObject": {"fileID": 7}}
        self.assertEqual(7, MODULE.require_scene_game_object_link(
            9, component, {7: {"m_Component": [{"component": {"fileID": 9}}]}}))
        with self.assertRaisesRegex(ValueError, "must link exactly once"):
            MODULE.require_scene_game_object_link(9, component, {7: {"m_Component": []}})
        with self.assertRaisesRegex(ValueError, "must link exactly once"):
            MODULE.require_scene_game_object_link(9, component,
                {7: {"m_Component": [{"component": {"fileID": 9}}, {"component": {"fileID": 9}}]}})
        with self.assertRaisesRegex(ValueError, "must resolve uniquely"):
            MODULE.require_scene_game_object_link(9, component, {})

    def test_provider_reference_rejects_null_malformed_or_wrong_asset_type(self):
        with self.assertRaisesRegex(ValueError, "Exact non-null ScriptableObject reference"):
            MODULE.exact_asset_reference({"fileID": 0, "guid": "a" * 32, "type": 2}, "null")
        with self.assertRaisesRegex(ValueError, "Exact non-null ScriptableObject reference"):
            MODULE.exact_asset_reference({"fileID": 11400000, "guid": "a" * 32, "type": 3}, "wrong-type")
        with self.assertRaisesRegex(ValueError, "Exact non-null ScriptableObject reference"):
            MODULE.exact_asset_reference({"fileID": 11400000, "guid": "a" * 32, "type": 2, "extra": 1}, "extra")

    def test_crlf_duplicate_guid_targets_are_ambiguous(self):
        with tempfile.TemporaryDirectory(prefix="s002-guid-index-") as temp:
            assets = Path(temp) / "Assets"
            assets.mkdir()
            (assets / "a.asset.meta").write_bytes(b"guid: " + b"a" * 32 + b"\r\n")
            (assets / "b.asset.meta").write_bytes(b"guid: " + b"a" * 32 + b"\r\n")
            cache = MODULE.scan_current_meta_guids(assets, 4096)
            with self.assertRaisesRegex(ValueError, "exactly one current asset"):
                MODULE.cached_guid_target(None, assets, "a" * 32, "duplicate", cache)


if __name__ == "__main__":
    unittest.main()
