import importlib.util
from pathlib import Path
import shutil
import tempfile
import unittest

CI = Path(__file__).resolve().parents[1]
spec = importlib.util.spec_from_file_location('skirmish_source_mission_preflight', CI / 'skirmish_source_mission_preflight.py')
preflight = importlib.util.module_from_spec(spec)
spec.loader.exec_module(preflight)
ROOT = CI.parents[1]

class SourceMissionPreflightTests(unittest.TestCase):
    def setUp(self):
        self.temp = tempfile.TemporaryDirectory()
        self.addCleanup(self.temp.cleanup)
        self.root = Path(self.temp.name)
        for path in [ROOT / 'Assets/Game/Configs/SkirmishExpansion/Scenarios/S003/SkirmishScenario_S003.asset', *[ROOT / ('Assets/Game/Configs/SkirmishExpansion/Shared/' + n + '.asset') for n in ('SkirmishObjective_BA', 'SkirmishArmy_AirMobile', 'SkirmishStart_Field', 'SkirmishLayout_DB_BA')]]:
            for source in (path, Path(str(path) + '.meta')):
                target = self.root / source.relative_to(ROOT)
                target.parent.mkdir(parents=True, exist_ok=True)
                shutil.copyfile(source, target)
        self.definition = self.root / 'Assets/Game/Configs/SkirmishExpansion/Scenarios/S003/SkirmishScenario_S003.asset'

    def test_identity_and_air_profile_do_not_admit_execution(self):
        result = preflight.inspect_s003(self.root)
        self.assertEqual((result['mission'], result['objective'], result['army_profile']), ('S003', 'BA', 'A'))
        self.assertFalse(result['execution_admitted'])
        self.assertFalse(result['world_allocated'])
        self.assertFalse(result['mission_ready'])
        self.assertEqual(len(result['source_pins']), 10)

    def test_rejects_relabelled_definition(self):
        self.definition.write_text(self.definition.read_text().replace('catalogId: S003', 'catalogId: S002'))
        with self.assertRaisesRegex(preflight.SourceMissionPreflightError, 'identity mismatch'):
            preflight.inspect_s003(self.root)

    def test_rejects_mismatched_army_reference(self):
        self.definition.write_text(self.definition.read_text().replace('c4e8a1b27d9f4e6a9c0d5f3b8a217604', '0' * 32))
        with self.assertRaisesRegex(preflight.SourceMissionPreflightError, 'reference mismatch'):
            preflight.inspect_s003(self.root)

if __name__ == '__main__':
    unittest.main()
