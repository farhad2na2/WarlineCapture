"""Focused checks for the explicitly selected S026 CityCrossroads alias."""
import hashlib
import importlib.util
import json
import shutil
import unittest
from pathlib import Path


ROOT = Path(__file__).resolve().parents[3]
RUNNER_PATH = ROOT / "Tools/CI/run_skirmish_logical_binding_author.py"
RUNNER_SPEC = importlib.util.spec_from_file_location("logical_binding_author", RUNNER_PATH)
RUNNER = importlib.util.module_from_spec(RUNNER_SPEC)
RUNNER_SPEC.loader.exec_module(RUNNER)

ALIAS_REL = Path(RUNNER.S026_ALIAS)
SOURCE_REL = Path(RUNNER.S026_SOURCE)
PARSER_REL = Path(RUNNER.SOURCE_PARSER)
DRAFT_PATH = ROOT / "Logs/SkirmishProduction/s026-alias-unbound-author-draft-20261116-r4/draft.json"
LAYOUT_PATH = ROOT / "Logs/SkirmishProduction/s026-native-shape-draft-20261116-r4/layout.json"


def _sha(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


class ExplicitS026AliasTests(unittest.TestCase):
    def setUp(self):
        self.draft = json.loads(DRAFT_PATH.read_text())
        self.layout = json.loads(LAYOUT_PATH.read_text())
        self.alias = ROOT / ALIAS_REL
        self.alias_sha = _sha(self.alias)

    def test_current_s026_draft_and_native_shape_r4_alias_match(self):
        pins = RUNNER.validate_explicit_s026_alias(
            self.draft, self.layout, ROOT, self.alias, self.alias_sha
        )
        self.assertEqual(pins[str(self.alias.resolve())], self.alias_sha)
        self.assertIn(str((ROOT / SOURCE_REL).resolve()), pins)
        self.assertIn(str((ROOT / PARSER_REL).resolve()), pins)

    def test_wrong_layout_identity_is_rejected(self):
        layout = dict(self.layout, operationMapId="opmap.skirmish.not_city_crossroads")
        with self.assertRaises((ValueError, RuntimeError)):
            RUNNER.validate_explicit_s026_alias(self.draft, layout, ROOT, self.alias, self.alias_sha)

    def test_wrong_campaign_source_path_is_rejected(self):
        draft = dict(self.draft, campaign_source_path="Assets/Game/Configs/OperationMaps/OperationMap_Compatibility_DesertBase01.asset")
        with self.assertRaises((ValueError, RuntimeError)):
            RUNNER.validate_explicit_s026_alias(draft, self.layout, ROOT, self.alias, self.alias_sha)

    def test_wrong_alias_hash_is_rejected(self):
        with self.assertRaises((ValueError, RuntimeError)):
            RUNNER.validate_explicit_s026_alias(self.draft, self.layout, ROOT, self.alias, "0" * 64)

    def test_tampered_alias_content_is_rejected_in_isolated_temp_repository(self):
        # A private repository-shaped fixture keeps the checked-in pinned asset untouched.
        import tempfile
        with tempfile.TemporaryDirectory() as temp:
            root = Path(temp)
            for relative in (ALIAS_REL, SOURCE_REL, PARSER_REL, Path(str(ALIAS_REL) + ".meta"), Path(str(SOURCE_REL) + ".meta"), Path("Assets/Game/Scripts/Configs/OperationMapDefinition.cs.meta")):
                destination = root / relative
                destination.parent.mkdir(parents=True, exist_ok=True)
                shutil.copyfile(ROOT / relative, destination)
            alias = root / ALIAS_REL
            content = alias.read_text()
            content = content.replace(
                "sourceContentHash: 2713962f0faa2dae49805e1b7e3a1673199a2cca915334d11421b354cd8f591c",
                "sourceContentHash: " + "0" * 64,
                1,
            )
            alias.write_text(content)
            with self.assertRaises((ValueError, RuntimeError)):
                RUNNER.validate_explicit_s026_alias(self.draft, self.layout, root, alias, _sha(alias))


if __name__ == "__main__":
    unittest.main()
