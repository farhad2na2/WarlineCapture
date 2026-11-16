"""Keep the S073 legacy catalog scope in every source-simulation manifest."""

import json
from pathlib import Path
import unittest


ROOT = Path(__file__).resolve().parents[4]
SIMULATION = ROOT / "Tools/CI/SkirmishSourceSimulation"
ADAPTER_ID = "SourceS073LegacyCatalogScope.cs"
ADAPTER_PATH = "Tools/CI/SkirmishSourceSimulation/Adapters/SourceS073LegacyCatalogScope.cs"
READ_SCOPE_PATH = "Tools/CI/SkirmishSourceSimulation/Adapters/454_SourceCampReadScope.cs"


class S073LegacyCatalogScopeManifestTests(unittest.TestCase):
    def test_normal_and_experimental_manifests_include_referenced_adapter_once(self):
        for manifest_name in ("source_manifest.plan.json", "source_manifest.s073-ai-experimental.plan.json"):
            with self.subTest(manifest=manifest_name):
                manifest = json.loads((SIMULATION / manifest_name).read_text(encoding="utf-8"))
                entries = manifest["sources"]
                by_id = [entry for entry in entries if entry.get("id") == ADAPTER_ID]
                self.assertEqual(len(by_id), 1)
                self.assertEqual(by_id[0].get("kind"), "finite_adapter")
                self.assertEqual(by_id[0].get("path"), ADAPTER_PATH)
                self.assertEqual(sum(entry.get("path") == ADAPTER_PATH for entry in entries), 1)
                read_scope = [entry for entry in entries if entry.get("path") == READ_SCOPE_PATH]
                self.assertEqual(len(read_scope), 1)

    def test_read_scope_reference_resolves_to_manifested_adapter(self):
        adapter = (ROOT / ADAPTER_PATH).read_text(encoding="utf-8")
        read_scope = (ROOT / READ_SCOPE_PATH).read_text(encoding="utf-8")
        self.assertIn("class SourceS073LegacyCatalogScope", adapter)
        self.assertIn("SourceS073LegacyCatalogScope.Current", read_scope)
        self.assertIn("SourceS073LegacyCatalogScope.Allows", read_scope)


if __name__ == "__main__":
    unittest.main()
