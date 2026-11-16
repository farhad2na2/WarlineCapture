"""Contract checks for the read-only S073 aircraft dependency inventory."""
import hashlib
import json
from pathlib import Path
import unittest


ROOT = Path(__file__).resolve().parents[4]
SOURCE = ROOT / "Assets/Game/Scripts/Editor/Skirmish/S073AircraftDependencyClosureDiagnostic.cs"
REPORT = ROOT / "Logs/SkirmishProduction/s073-aircraft-geometry-drift-20261102-r1/report.json"
REPORT_SHA256 = "801eebd8adb5b36229afb9be89bf83b7fef2e213a92cb64adb5a229f2b57b6e2"


class S073AircraftDependencyClosureContractTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.source = SOURCE.read_text(encoding="utf-8")
        cls.report_bytes = REPORT.read_bytes()
        cls.report = json.loads(cls.report_bytes)

    def test_input_report_is_pinned_and_supplies_only_prefab_roots(self):
        self.assertEqual(hashlib.sha256(self.report_bytes).hexdigest(), REPORT_SHA256)
        self.assertIn(REPORT_SHA256, self.source)
        self.assertIn("Logs/SkirmishProduction/s073-aircraft-geometry-drift-20261102-r1/report.json", self.source)
        self.assertEqual(self.report["kind"], "S073AircraftGeometryDependencyDiagnostic")
        self.assertTrue(self.report["rotor_geometry"]["rows"])
        self.assertTrue(self.report["canonical_geometry"]["roles"])
        self.assertIn('row["installed"]?["source_path"]', self.source)
        self.assertIn('row["measured"]?["source_path"]', self.source)
        self.assertIn('comparison["installed"]?["path"]', self.source)
        self.assertIn('comparison["measured"]?["path"]', self.source)
        self.assertIn('path.EndsWith(".prefab", StringComparison.Ordinal)', self.source)

    def test_assetdatabase_closure_hashes_and_explicit_file_statuses_are_recorded(self):
        self.assertIn("AssetDatabase.GetDependencies(sourcePath, recursive: true)", self.source)
        self.assertIn("AssetDatabase.GetAssetDependencyHash(sourcePath)", self.source)
        self.assertIn('"closure_elapsed_ms"', self.source)
        self.assertIn('"elapsed_ms"', self.source)
        self.assertIn('"raw_bytes"', self.source)
        self.assertIn('"meta_bytes"', self.source)
        self.assertIn('"Unavailable_FileMissing"', self.source)
        self.assertIn('"Unavailable_ReadFailed"', self.source)
        self.assertIn('"Unavailable_QueryFailed"', self.source)
        self.assertIn("SHA256.Create()", self.source)

    def test_diagnostic_is_edit_mode_only_and_writes_only_fresh_private_tmp_receipt(self):
        self.assertIn("EditorApplication.isPlayingOrWillChangePlaymode", self.source)
        self.assertIn('"WARLINE_S073_AIRCRAFT_DEPENDENCY_CLOSURE_OUTPUT"', self.source)
        self.assertIn('path.StartsWith("/private/tmp/", StringComparison.Ordinal)', self.source)
        self.assertIn("FileMode.CreateNew", self.source)
        for forbidden in (
            "AssetDatabase.Refresh(",
            "AssetDatabase.SaveAssets(",
            "AssetDatabase.ImportAsset(",
            "EditorApplication.EnterPlaymode(",
            "EditorApplication.EnterPlayMode(",
            "World.DefaultGameObjectInjectionWorld",
            "EntityManager",
        ):
            self.assertNotIn(forbidden, self.source)


if __name__ == "__main__":
    unittest.main()
