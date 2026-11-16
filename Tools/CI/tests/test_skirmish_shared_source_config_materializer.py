import json
import unittest
from pathlib import Path

ROOT = Path(__file__).resolve().parents[3]
CORE = ROOT / "Assets/Game/Scripts/Editor/Skirmish/AuthoredSourceConfigGraphMaterializer.cs"
GENERAL = ROOT / "Tools/CI/SkirmishSourceSimulation/Adapters/051_SourceConfigMaterializer.cs"
DECLARED = ROOT / "Tools/CI/SkirmishSourceSimulation/Adapters/DeclaredGround/051_SourceConfigMaterializer.cs"
UNITY_STUB = ROOT / "Tools/CI/SkirmishSourceSimulation/Adapters/052_SourceConfigMetadataRuntime.cs"
PLANS = sorted((ROOT / "Tools/CI/SkirmishSourceSimulation").glob("source_manifest*.plan.json"))


def allowlist(path):
    text = path.read_text()
    start = text.index("AllowedSourceTypes={") + len("AllowedSourceTypes={")
    end = text.index("};", start)
    return [item.strip().strip('"') for item in text[start:end].split(",")]


class SharedSourceConfigMaterializerTests(unittest.TestCase):
    def test_lane_wrappers_keep_separate_allowlists(self):
        general = allowlist(GENERAL)
        declared = allowlist(DECLARED)
        self.assertEqual(len(general), 31)
        self.assertEqual(len(declared), 21)
        self.assertTrue(set(declared).issubset(general))
        self.assertIn("SourceConfigMaterializer : AuthoredSourceConfigGraphMaterializer", GENERAL.read_text())
        self.assertIn("SourceConfigMaterializer : AuthoredSourceConfigGraphMaterializer", DECLARED.read_text())

    def test_shared_core_keeps_bounded_exact_pin_checks_and_unity_object_creation(self):
        text = CORE.read_text()
        self.assertIn("FileAttributes.ReparsePoint", text)
        self.assertIn("File.ReadAllBytes(path)", text)
        self.assertIn("bytes.LongLength!=info.Length", text)
        self.assertIn("hash.ComputeHash(bytes)", text)
        self.assertIn("ScriptableObject.CreateInstance(type)", text)
        self.assertNotIn("AutonomousWorld.PinnedFile", text)
        stub = UNITY_STUB.read_text()
        self.assertIn("ScriptableObject CreateInstance(Type type)", stub)

    def test_every_source_host_manifest_includes_one_shared_core_and_its_lane_wrapper(self):
        self.assertEqual(len(PLANS), 6)
        for path in PLANS:
            with self.subTest(path=path.name):
                rows = json.loads(path.read_text())["sources"]
                shared = [row for row in rows if row.get("path") == "Assets/Game/Scripts/Editor/Skirmish/AuthoredSourceConfigGraphMaterializer.cs"]
                wrappers = [row for row in rows if row.get("id") == "051_SourceConfigMaterializer.cs"]
                self.assertEqual(len(shared), 1)
                self.assertEqual(len(wrappers), 1)
                self.assertEqual(shared[0]["role"], "data_decoder")

    def test_army_profile_keeps_its_five_unity_serialized_field_markers(self):
        self.assertEqual(len(PLANS), 6)
        for path in PLANS:
            with self.subTest(path=path.name):
                rows = json.loads(path.read_text())["sources"]
                profile = next(row for row in rows if row.get("id") == "229_SkirmishArmyProfileConfig.metadata.cs")
                marker = next(binding for binding in profile["bindings"]
                              if binding.get("from") == "[SerializeField] ")
                self.assertEqual(marker.get("count"), 5)
                self.assertEqual(marker.get("to"), "[UnityEngine.SerializeField] ")


if __name__ == "__main__":
    unittest.main()
