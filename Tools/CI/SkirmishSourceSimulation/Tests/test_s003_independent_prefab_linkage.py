"""Focused controls for deriving the source-only S003 capture linkage."""
import importlib.util
import sys
import unittest
from pathlib import Path

RECIPES = Path(__file__).resolve().parents[1] / 'Recipes'
sys.path.insert(0, str(RECIPES))
spec = importlib.util.spec_from_file_location('s003_independent_prefab_projection',
                                              RECIPES / 'project_s003_independent_prefab_domain.py')
module = importlib.util.module_from_spec(spec)
spec.loader.exec_module(module)


class LinkedCaptureAuditTests(unittest.TestCase):
    def setUp(self):
        self.checked = {'status': 'PassedDataOnlyCapture', 'mission_ready': False}
        self.capture = {'native_metadata_capture_sha256': 'metadata',
                        'independent_metadata_audit_sha256': 'audit'}

    def test_derives_from_unmodified_auditor_output(self):
        actual = module.linked_capture_audit(dict(self.checked), self.checked, self.capture, 'metadata', 'audit')
        self.assertEqual(actual['metadata_capture_sha256'], 'metadata')
        self.assertEqual(actual['independent_metadata_audit_sha256'], 'audit')
        self.assertFalse(actual['mission_ready'])
        self.assertNotIn('metadata_capture_sha256', self.checked)

    def test_rejects_wrong_capture_links_and_conflicting_existing_fields(self):
        for metadata, audit in [('changed', 'audit'), ('metadata', 'changed')]:
            with self.subTest(metadata=metadata, audit=audit), self.assertRaises(ValueError):
                module.linked_capture_audit(dict(self.checked), self.checked, self.capture, metadata, audit)
        for key in ('metadata_capture_sha256', 'independent_metadata_audit_sha256'):
            with self.subTest(key=key), self.assertRaises(ValueError):
                module.linked_capture_audit({**self.checked, key: 'changed'}, self.checked, self.capture,
                                            'metadata', 'audit')

    def test_rejects_altered_audit_and_accepts_matching_enrichment(self):
        with self.assertRaises(ValueError):
            module.linked_capture_audit({**self.checked, 'mission_ready': True}, self.checked, self.capture,
                                        'metadata', 'audit')
        enriched = {**self.checked, 'metadata_capture_sha256': 'metadata',
                    'independent_metadata_audit_sha256': 'audit'}
        self.assertEqual(module.linked_capture_audit(enriched, self.checked, self.capture, 'metadata', 'audit'),
                         enriched)


if __name__ == '__main__':
    unittest.main()
