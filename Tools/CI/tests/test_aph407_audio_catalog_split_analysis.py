import importlib.util
import unittest
import json
import subprocess
import tempfile
import tarfile
import io
from pathlib import Path


MODULE_PATH = Path(__file__).resolve().parents[1] / "aph407_audio_catalog_split_analysis.py"
SPEC = importlib.util.spec_from_file_location("aph407", MODULE_PATH)
APH407 = importlib.util.module_from_spec(SPEC)
SPEC.loader.exec_module(APH407)


class Aph407AudioCatalogSplitAnalysisTests(unittest.TestCase):
    # This analysis is bound to the 234-clip July capture, not later mission catalogs.
    HISTORICAL_COMMIT = "2653d43ea1acd05762a84dd8330cfb7e2b6ddbca"

    @classmethod
    def setUpClass(cls):
        cls.temporary = tempfile.TemporaryDirectory(prefix="warline-aph407-historical-")
        cls.snapshot = Path(cls.temporary.name)
        names = ["INVENTORY_PATH", "MENU_CAPTURE_PATH", "MATCH_CAPTURE_PATH", "SOURCE_CATALOG_PATH",
                 "IMPORT_PROFILES_PATH", "SERIALIZED_CATALOG_PATH", "MENU_SCENE_PATH", "RUNTIME_VIEW_PATH",
                 "BRIDGE_PATH", "SCENE_LIFECYCLE_PATH"]
        paths = [getattr(APH407, name).relative_to(APH407.ROOT).as_posix() for name in names]
        inventory = json.loads(subprocess.run(["git", "show", cls.HISTORICAL_COMMIT + ":" + paths[0]],
            cwd=APH407.ROOT, check=True, capture_output=True).stdout)
        paths += [clip["assetPath"] + ".meta" for clip in inventory["catalogAudioClips"] if clip.get("busIds") == ["Voice"]]
        archive = subprocess.run(["git", "archive", cls.HISTORICAL_COMMIT, "--", *paths],
            cwd=APH407.ROOT, check=True, capture_output=True).stdout
        with tarfile.open(fileobj=io.BytesIO(archive)) as files:
            for member in files.getmembers():
                if not member.isfile():
                    continue
                target = cls.snapshot / member.name
                if not target.resolve().is_relative_to(cls.snapshot.resolve()):
                    raise ValueError("Historical fixture path escapes its temporary root")
                target.parent.mkdir(parents=True, exist_ok=True)
                target.write_bytes(files.extractfile(member).read())

    @classmethod
    def tearDownClass(cls):
        cls.temporary.cleanup()

    def test_historical_analyzer_rejects_a_changed_current_catalog(self):
        with self.assertRaisesRegex(ValueError, "Source catalog must contain exactly 234 events"):
            APH407.build_report()

    def test_partition_policy_is_exhaustive_for_expected_domains(self):
        self.assertEqual("Core/Menu", APH407.partition_for("UI", ["UI.Button.Primary.Click"]))
        self.assertEqual("Core/Menu", APH407.partition_for("Music", ["Music.Menu.Loop"]))
        self.assertEqual("Match", APH407.partition_for("Music", ["Music.Match.CalmLoop"]))
        self.assertEqual("Match", APH407.partition_for("SFX", ["Gameplay.Weapon.Rifle"]))
        self.assertEqual("Voice", APH407.partition_for("Voice", ["VO.ARIA.Settings.Opened"]))

    def test_inventory_partitions_cover_catalog_once(self):
        report = APH407.build_report(self.snapshot)
        self.assertEqual(234, sum(row["clipCount"] for row in report["catalogPartition"]))
        self.assertEqual(["Core/Menu", "Match", "Voice"], [row["partition"] for row in report["catalogPartition"]])

    def test_measured_partition_totals_equal_capture_total(self):
        report = APH407.build_report(self.snapshot)
        menu = report["measuredResidency"]["menuBeforePlayback"]
        self.assertEqual(menu["catalogRuntimeMemoryBytes"], sum(row["runtimeMemoryBytes"] for row in menu["partitions"]))
        self.assertEqual(menu["loadedClipCount"], sum(row["loadedClipCount"] for row in menu["partitions"]))

    def test_recommendation_remains_gated_on_full_policy_residency(self):
        report = APH407.build_report(self.snapshot)
        self.assertEqual("DECLINE_OPENING_IMPLEMENTATION_NOW", report["recommendation"])
        self.assertEqual(8, report["importerEvidence"]["pilotImporterAppliedCount"])
        self.assertEqual(163, report["importerEvidence"]["fullVoicePolicyAppliedCount"])
        self.assertEqual(0, report["importerEvidence"]["remainingVoiceDecompressPreloadCount"])
        self.assertTrue(report["importerEvidence"]["pilotAndroidMeasurementAvailable"])
        self.assertTrue(report["importerEvidence"]["fullVoicePolicyApplied"])

    def test_report_is_deterministic(self):
        self.assertEqual(APH407.build_report(self.snapshot), APH407.build_report(self.snapshot))
        report = APH407.build_report(self.snapshot)
        self.assertEqual(APH407.render_markdown(report), APH407.render_markdown(report))

    def test_importer_state_parser_reads_unity_audio_meta_contract(self):
        meta = """  defaultSettings:\n    loadType: 1\n    preloadAudioData: 0\n  platformSettingOverrides: {}\n  loadInBackground: 1\n"""
        self.assertEqual((1, 0, 1), APH407.audio_importer_state(meta))


if __name__ == "__main__":
    unittest.main()
