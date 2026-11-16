"""Source-reference controls; these do not establish a native match result."""
import json
from pathlib import Path
import shutil
import subprocess
import sys
import tempfile
import unittest

ROOT = Path(__file__).resolve().parents[4]
SCRIPT = ROOT / "Tools/CI/SkirmishSourceSimulation/Tests/generate_s073_ai_source_graph.py"
INPUTS = [
    "Assets/Game/Resources/SkirmishIndustrialBasin.asset",
    "Assets/Game/Configs/Skirmish/Enemy.asset",
    "Assets/Game/Configs/Skirmish/Player.asset",
    "Assets/Game/Configs/Skirmish/Plan.asset",
]
OTHER = ["Tools/CI/SkirmishSourceSimulation/Recipes/selected_operation_map/helpers/read_source_configs.rb",
         "Tools/CI/SkirmishSourceSimulation/Fixtures/S073/retained/provenance.json",
         "Assets/Game/Scripts/Configs/SkirmishPresetConfig.cs.meta",
         "Assets/Game/Scripts/Configs/GameplayConfigModels.cs.meta",
         "Assets/Game/Scripts/Configs/AIPlanEntryStartupConfig.cs.meta"]


class S073AISourceGraphTests(unittest.TestCase):
    def setUp(self):
        self.tmp = tempfile.TemporaryDirectory()
        self.root = Path(self.tmp.name)
        for name in INPUTS + [x + ".meta" for x in INPUTS] + OTHER:
            target = self.root / name
            target.parent.mkdir(parents=True, exist_ok=True)
            shutil.copyfile(ROOT / name, target)

    def tearDown(self):
        self.tmp.cleanup()

    def run_generator(self):
        return subprocess.run([sys.executable, str(SCRIPT), "--repo", str(self.root), "--output", str(self.root / "out")],
                              text=True, capture_output=True, timeout=30)

    def test_exact_preset_order_and_enemy_source(self):
        result = self.run_generator()
        self.assertEqual(result.returncode, 0, result.stderr)
        graph = json.loads((self.root / "out/source-ai-config-graph.json").read_text())
        self.assertEqual([x["source_path"] for x in graph["nodes"]], INPUTS[1:])
        self.assertEqual(graph["nodes"][0]["fields"]["preferredBuildingIds"], ["Building_GuardTower"])
        self.assertFalse(graph["world_initialized"])

    def test_foreign_reference_fails_closed(self):
        preset = self.root / INPUTS[0]
        data = preset.read_text()
        self.assertEqual(data.count("c3d7226b0272d44dfa219e9185a76087"), 1)
        preset.write_text(data.replace("c3d7226b0272d44dfa219e9185a76087", "0" * 32))
        result = self.run_generator()
        self.assertNotEqual(result.returncode, 0)
        self.assertIn("Preset AI references", result.stderr)
        self.assertFalse((self.root / "out").exists())


if __name__ == "__main__":
    unittest.main()
