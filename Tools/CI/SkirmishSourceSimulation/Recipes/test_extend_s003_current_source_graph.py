import importlib.util
from pathlib import Path
import tempfile
import unittest


MODULE_PATH = Path(__file__).with_name("extend_s003_current_source_graph.py")
SPEC = importlib.util.spec_from_file_location("s003_source_graph_extension_producer", MODULE_PATH)
PRODUCER = importlib.util.module_from_spec(SPEC)
SPEC.loader.exec_module(PRODUCER)


class S003CurrentSourceGraphExtensionProducerTests(unittest.TestCase):
    def test_current_inputs_reproduce_exact_graph_identities_source_only(self):
        root = PRODUCER.ROOT.resolve()
        base_path, base = PRODUCER.load_base(root, root / PRODUCER.BASE_REL)
        build_path = PRODUCER.under_root(root, root / PRODUCER.BUILD_REL)
        target = root / "T Logs/SkirmishProduction/s003-producer-test-output"
        selection_path = target / "source-selection/source-config-graph-with-selected-source-pins.json"
        grid_path = target / "grid-surface/source-config-graph-with-grid-surface-nodes.json"
        selection, selection_bytes, selection_receipt = PRODUCER.selection_extension(
            root, base_path, base, selection_path, build_path
        )
        grid, grid_bytes, grid_receipt = PRODUCER.grid_surface_extension(
            root, selection_path, selection, grid_path
        )
        self.assertEqual(PRODUCER.sha(selection_bytes),
                         "e498aa3cd7415419541c1a22dc1543fc66fe930c7c6bc17cae984e37de031c2e")
        self.assertEqual(PRODUCER.sha(grid_bytes),
                         "34194993cb3430293923140addf3d19d6222e86a675fb82a2fe06b83f84eaf9f")
        self.assertEqual((len(selection["nodes"]), len(selection["source_pins"])), (35, 143))
        self.assertEqual((len(grid["nodes"]), len(grid["source_pins"])), (37, 147))
        self.assertFalse(selection.get("world_initialized", True))
        self.assertFalse(grid.get("world_initialized", True))
        self.assertEqual(selection_receipt["status"], "PassedStrictSourceEvidenceExtension")
        self.assertEqual(grid_receipt["status"], "PassedStrictSourceOnlyExtension")
        self.assertEqual(selection_receipt["producer"], {
            "path": PRODUCER.PRODUCER_REL.as_posix(),
            "sha256": PRODUCER.sha_file(root / PRODUCER.PRODUCER_REL),
        })
        self.assertEqual(grid_receipt["producer"], selection_receipt["producer"])
        self.assertEqual(selection_receipt["output_graph"]["path"], selection_path.relative_to(root).as_posix())
        self.assertEqual(grid_receipt["output_graph"]["path"], grid_path.relative_to(root).as_posix())

    def test_tampered_source_bytes_are_rejected(self):
        raw = b"actual source"
        with self.assertRaisesRegex(ValueError, "source byte hash mismatch"):
            PRODUCER.require_hash(raw + b"tamper", PRODUCER.sha(raw), "fixture")

    def test_duplicate_required_asset_path_is_rejected(self):
        pairs = [("Assets/duplicate.asset", "0" * 64)] * 8
        with self.assertRaisesRegex(ValueError, "unique generated selector"):
            PRODUCER.required_asset_map(pairs)

    def test_script_class_mismatch_is_rejected(self):
        with tempfile.TemporaryDirectory(prefix="s003-script-class-test-") as temp:
            script = Path(temp) / "WrongGrid.cs"
            script.write_text("namespace Game.Configs { public sealed class MapSurfaceDataAsset {} }",
                              encoding="utf-8")
            with self.assertRaisesRegex(ValueError, "declare the expected Game.Configs type"):
                PRODUCER.verify_declared_config_class(script, "GridAuthoringSceneConfigAsset")

    def test_class_in_wrong_namespace_is_rejected(self):
        with tempfile.TemporaryDirectory(prefix="s003-script-namespace-test-") as temp:
            script = Path(temp) / "GridAuthoringSceneConfigAsset.cs"
            script.write_text(
                "namespace Game.Configs { } namespace Other { public class GridAuthoringSceneConfigAsset {} }",
                encoding="utf-8")
            with self.assertRaisesRegex(ValueError, "declare the expected Game.Configs type"):
                PRODUCER.verify_declared_config_class(script, "GridAuthoringSceneConfigAsset")

    def test_actual_grid_monoscript_link_cannot_resolve_to_surface_class(self):
        root = PRODUCER.ROOT.resolve()
        helpers = PRODUCER.load_graph_helpers(root)
        asset = root / PRODUCER.PHYSICAL_ASSETS[0]
        parsed = PRODUCER.strict_read(root, [asset])
        mutated = __import__("copy").deepcopy(parsed)
        surface_meta = root / "Assets/Game/Scripts/Configs/MapSurfaceDataAsset.cs.meta"
        mutated[str(asset)]["MonoBehaviour"]["m_Script"]["guid"] = PRODUCER.meta_guid(surface_meta)
        with self.assertRaisesRegex(ValueError, "serialized script/class type mismatch"):
            PRODUCER.make_node(root, helpers, asset, mutated,
                               expect_type="GridAuthoringSceneConfigAsset")

    def test_malformed_packed_surface_payload_is_rejected(self):
        with self.assertRaisesRegex(ValueError, "hexadecimal MapSurfaceDataAsset payload"):
            PRODUCER.require_surface_payload("NOT_HEX")

    def test_duplicate_guid_target_is_rejected(self):
        helpers = PRODUCER.load_graph_helpers(PRODUCER.ROOT)
        with tempfile.TemporaryDirectory(prefix="s003-duplicate-guid-test-") as temp:
            assets = Path(temp) / "Assets"
            assets.mkdir()
            guid = "a" * 32
            for name in ("a", "b"):
                (assets / f"{name}.asset").write_bytes(b"%YAML 1.1\n--- !u!114 &11400000\nMonoBehaviour: {}\n")
                (assets / f"{name}.asset.meta").write_text(f"guid: {guid}\n", encoding="utf-8")
            with self.assertRaisesRegex(ValueError, "resolve uniquely"):
                PRODUCER.one_guid_target(helpers, assets, guid)

    def test_lexical_and_symlink_escape_is_rejected(self):
        with tempfile.TemporaryDirectory(prefix="s003-path-escape-test-") as temp:
            root = Path(temp)
            assets = root / "Assets"
            assets.mkdir()
            outside = root / "outside.asset"
            outside.write_text("outside", encoding="utf-8")
            link = assets / "linked.asset"
            link.symlink_to(outside)
            with self.assertRaisesRegex(ValueError, "lexical path escape|symlink source path"):
                PRODUCER.under_root(root, link)


if __name__ == "__main__":
    unittest.main()
