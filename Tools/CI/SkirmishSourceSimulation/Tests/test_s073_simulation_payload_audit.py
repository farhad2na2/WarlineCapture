import json
import shutil
import tempfile
import unittest
from pathlib import Path

from audit_s073_streamed_source import SOURCE_PINS, audit_simulation_payload, digest


REPO = Path(__file__).resolve().parents[4]
ARTIFACT = REPO / 'Logs/SkirmishProduction/s073-streamed-source-capture-20261101-r4'


class S073SimulationPayloadAuditTests(unittest.TestCase):
    def setUp(self):
        self.temp = tempfile.TemporaryDirectory()
        self.root = Path(self.temp.name)
        self.capture = self.root / 'source-world.json'
        shutil.copyfile(ARTIFACT / 'source-world.json', self.capture)
        self.receipt = json.loads((ARTIFACT / 'receipt.json').read_text())
        self.receipt['capture_sha256'] = digest(self.capture)
        for relative in SOURCE_PINS:
            source = self.root / relative
            source.parent.mkdir(parents=True, exist_ok=True)
            shutil.copyfile(REPO / relative, source)

    def tearDown(self):
        self.temp.cleanup()

    def write_capture(self, data):
        self.capture.write_text(json.dumps(data, separators=(',', ':')))
        self.receipt['capture_sha256'] = digest(self.capture)

    def test_actual_r4_capture_classifies_only_captured_references(self):
        result = audit_simulation_payload(self.capture, self.receipt, self.root)
        self.assertTrue(result['captured_unsupported_refs_classified_as_presentation'])
        self.assertFalse(result['simulation_payload_complete'])
        self.assertFalse(result['full_source_authority_closure_complete'])
        self.assertFalse(result['mission_ready'])
        self.assertEqual(result['unresolved_presentation_reference_count'], 71)

    def test_unknown_gameplay_field_is_rejected(self):
        data = json.loads(self.capture.read_text())
        owner = next(row for row in data['entities']
                     if 'Game.Components.UnitHealth' in row['components'])
        component = owner['components']['Game.Components.UnitHealth']
        component['value']['fields']['Game.Components.UnitHealth::Current'] = {
            'codec': 'unsupported-v1', 'type': 'Unity.Entities.UnityObjectRef`1[[UnityEngine.GameObject]]',
            'reason': 'unity_object_ref_requires_bound_source_identity'}
        data['unsupported_values'].append({
            'path': owner['id'] + '/Game.Components.UnitHealth/Current',
            'reason': 'unity_object_ref_requires_bound_source_identity',
            'type': 'Unity.Entities.UnityObjectRef`1[[UnityEngine.GameObject]]'})
        self.write_capture(data)
        with self.assertRaisesRegex(ValueError, 'Unknown or gameplay-relevant unresolved field'):
            audit_simulation_payload(self.capture, self.receipt, self.root)

    def test_source_drift_is_rejected(self):
        path = self.root / 'Assets/Game/Scripts/Systems/UnitAttackSystem.cs'
        path.write_bytes(path.read_bytes() + b'// drift\n')
        with self.assertRaisesRegex(ValueError, 'Reviewed source drift'):
            audit_simulation_payload(self.capture, self.receipt, self.root)


if __name__ == '__main__':
    unittest.main()
