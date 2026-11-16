import json
from pathlib import Path
import shutil
import subprocess
import tempfile
import unittest

ROOT = Path(__file__).resolve().parents[4]
GENERATOR = ROOT / 'Tools/CI/SkirmishSourceSimulation/Tests/generate_s073_exchange_source_graph.py'
PATHS = [
    'Assets/Game/Scenes/Match.unity',
    'Assets/Game/Configs/Scene/Game_ResourceExchange_Config.asset',
    'Assets/Game/Configs/Scene/Game_ResourceExchange_Config.asset.meta',
    'Assets/Game/Scripts/Configs/ResourceExchangeConfigModels.cs.meta',
    'Assets/Game/Scripts/Composition/MatchSceneView.cs.meta',
    'Tools/CI/SkirmishSourceSimulation/Recipes/selected_operation_map/helpers/read_source_configs.rb',
    'Tools/CI/SkirmishSourceSimulation/Fixtures/S073/retained/provenance.json',
    'Tools/CI/SkirmishSourceSimulation/Tests/generate_s073_exchange_source_graph.py',
]


class ExchangeGraphTest(unittest.TestCase):
    def setUp(self):
        self.temp = tempfile.TemporaryDirectory()
        self.root = Path(self.temp.name)
        for relative in PATHS:
            target = self.root / relative
            target.parent.mkdir(parents=True, exist_ok=True)
            shutil.copyfile(ROOT / relative, target)

    def tearDown(self):
        self.temp.cleanup()

    def generate(self):
        return subprocess.run(['python3', '-B', str(GENERATOR), '--repo', str(self.root),
                               '--output', str(self.root / 'out')], capture_output=True, text=True)

    def test_scene_bound_graph_contains_actual_legacy_gate(self):
        result = self.generate()
        self.assertEqual(result.returncode, 0, result.stderr)
        graph = json.loads((self.root / 'out/source-exchange-config-graph.json').read_text())
        fields = graph['nodes'][0]['fields']
        self.assertEqual(len(fields['recipes']), 5)
        gate = next(row for row in fields['scenarioGates'] if row['scenarioTag'] == 'custom.skirmish.legacy')
        self.assertEqual((gate['exchangeEnabled'], gate['allowAiExchange']), (1, 0))
        self.assertFalse(graph['world_initialized'])

    def test_changed_scene_reference_is_rejected(self):
        path = self.root / PATHS[0]
        path.write_text(path.read_text().replace('58803fa3f1c245a6822e896daeb5cc8a', '0' * 32))
        self.assertNotEqual(self.generate().returncode, 0)
        self.assertFalse((self.root / 'out').exists())

    def test_changed_script_identity_is_rejected(self):
        path = self.root / PATHS[3]
        path.write_text(path.read_text().replace('9242380c1e674cc5b0a925b70e88cbfc', '0' * 32))
        self.assertNotEqual(self.generate().returncode, 0)
        self.assertFalse((self.root / 'out').exists())

    def test_changed_scene_view_identity_is_rejected(self):
        path = self.root / PATHS[4]
        path.write_text(path.read_text().replace('7545eea47de94ef09228cf74cbc6e07a', '0' * 32))
        self.assertNotEqual(self.generate().returncode, 0)
        self.assertFalse((self.root / 'out').exists())

    def test_changed_repository_generator_is_rejected(self):
        path = self.root / PATHS[-1]
        path.write_text(path.read_text() + '\n# changed\n')
        self.assertNotEqual(self.generate().returncode, 0)
        self.assertFalse((self.root / 'out').exists())


if __name__ == '__main__':
    unittest.main()
