"""Focused adversarial checks for the receipt-driven S074 admission helper."""
import copy
import hashlib
import importlib.util
import json
from pathlib import Path
import tempfile
import unittest
from unittest.mock import patch


ROOT = Path(__file__).resolve().parents[3]
HELPER = ROOT / 'Tools/CI/SkirmishSourceSimulation/Recipes/validate_s074_source_owner_receipt_v1.py'
RECEIPT_DIR = ROOT / 'T Logs/SkirmishProduction/s074-both-vehicle-generic-source-only-bundle-20261114-r2'
spec = importlib.util.spec_from_file_location('s074_receipt_v1', HELPER)
validator = importlib.util.module_from_spec(spec)
spec.loader.exec_module(validator)


class S074SourceOwnerReceiptV1Tests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls._temps = []
        cls.bundle = json.loads((RECEIPT_DIR / 'data-bundle.json').read_text())
        original_initial = json.loads(Path(cls.bundle['initialization']['path']).read_text())
        cls.admission = json.loads(Path(cls.bundle['s074_source_owner_admission']['path']).read_text())
        cls.initial, cls.bundle, cls.admission = cls.make_joined_fixture(
            original_initial, cls.bundle, cls.admission)
        cls.selected_build = cls.admission['inputs']['candidate_source_build']

    @classmethod
    def make_joined_fixture(cls, initial, bundle, admission, bad_author=None):
        """Build a noncircular init/receipt/bundle chain from pinned source inputs."""
        temp = tempfile.TemporaryDirectory(prefix='s074-owner-receipt-test-', dir=ROOT)
        cls._temps.append(temp)
        directory = Path(temp.name)
        initial = copy.deepcopy(initial)
        bundle = copy.deepcopy(bundle)
        admission = copy.deepcopy(admission)
        initial['s074_candidate_author'] = copy.deepcopy(bad_author or {
            'request': admission['inputs']['author_request'],
            'result': admission['inputs']['author_result']})
        initial_path = directory / 'initialization.json'
        initial_path.write_text(json.dumps(initial, separators=(',', ':')) + '\n')
        initial_pin = {'path': str(initial_path),
            'sha256': hashlib.sha256(initial_path.read_bytes()).hexdigest()}
        admission['inputs']['initialization'] = initial_pin
        admission_path = directory / 'admission.json'
        admission_path.write_text(json.dumps(admission, separators=(',', ':')) + '\n')
        admission_pin = {'path': str(admission_path),
            'sha256': hashlib.sha256(admission_path.read_bytes()).hexdigest()}
        bundle['initialization'] = initial_pin
        bundle['s074_source_owner_admission'] = admission_pin
        return initial, bundle, admission

    def validate(self):
        return validator.validate_s074_source_owner_receipt_v1(
            self.initial, self.bundle, ROOT, self.selected_build)

    def preflight(self, bundle=None, selected_build=None):
        return validator.preflight_s074_selected_build(
            self.bundle if bundle is None else bundle, ROOT,
            self.selected_build if selected_build is None else selected_build)

    def test_selected_build_preflight_admits_matching_s074_owner_receipt(self):
        self.assertTrue(self.preflight())

    def test_selected_build_preflight_rejects_different_build(self):
        with self.assertRaises(ValueError):
            self.preflight(selected_build={'path': self.selected_build['path'],
                                          'sha256': '0' * 64})

    def test_selected_build_preflight_rejects_malformed_or_tampered_admission_descriptor(self):
        malformed = copy.deepcopy(self.bundle)
        malformed['s074_source_owner_admission'] = {'path': self.admission['inputs']['candidate_source_build']['path']}
        with self.assertRaises(ValueError):
            self.preflight(bundle=malformed)

        tampered = copy.deepcopy(self.bundle)
        tampered['s074_source_owner_admission']['sha256'] = '0' * 64
        with self.assertRaises(ValueError):
            self.preflight(bundle=tampered)

    def test_selected_build_preflight_does_not_admit_other_missions_or_missing_owner_receipt(self):
        other_mission = copy.deepcopy(self.bundle)
        other_mission['mission'] = 'S073'
        self.assertFalse(self.preflight(bundle=other_mission))

        no_owner_receipt = copy.deepcopy(self.bundle)
        no_owner_receipt.pop('s074_source_owner_admission')
        self.assertFalse(self.preflight(bundle=no_owner_receipt))

        initial_without_author = copy.deepcopy(self.initial)
        initial_without_author.pop('s074_candidate_author')
        temp = tempfile.TemporaryDirectory(prefix='s074-owner-no-author-', dir=ROOT)
        self._temps.append(temp)
        initial_path = Path(temp.name) / 'initialization.json'
        initial_path.write_text(json.dumps(initial_without_author, separators=(',', ':')) + '\n')
        bundle_without_author = copy.deepcopy(self.bundle)
        bundle_without_author['initialization'] = {
            'path': str(initial_path),
            'sha256': hashlib.sha256(initial_path.read_bytes()).hexdigest()}
        self.assertFalse(self.preflight(bundle=bundle_without_author))

    def reject_with_document_mutation(self, label, mutation, path_match=None):
        read = validator._read

        def altered(desc, read_label, root=None, allow_external=False):
            value, pin = read(desc, read_label, root, allow_external)
            if read_label == label and (path_match is None or path_match in desc.get('path', '')):
                value = copy.deepcopy(value)
                mutation(value)
            return value, pin

        with patch.object(validator, '_read', side_effect=altered):
            with self.assertRaises(ValueError):
                self.validate()

    def test_accepts_current_ten_metre_receipt(self):
        self.assertGreater(len(self.validate()), 8000)

    def test_rejects_forged_author_compilation_status(self):
        self.reject_with_document_mutation('author_result', lambda doc: doc.update(status='Failed'))

    def test_rejects_candidate_author_descriptor_mismatch(self):
        forged = copy.deepcopy(self.admission['inputs'])
        forged['author_result'] = {'path': forged['author_result']['path'], 'sha256': '0' * 64}
        initial, bundle, admission = self.make_joined_fixture(
            json.loads(Path(self.bundle['initialization']['path']).read_text()),
            self.bundle, self.admission,
            {'request': forged['author_request'], 'result': forged['author_result']})
        with self.assertRaises(ValueError):
            validator.validate_s074_source_owner_receipt_v1(
                initial, bundle, ROOT, admission['inputs']['candidate_source_build'])

    def test_rejects_raw_capture_relabel_or_pass_marker(self):
        self.reject_with_document_mutation('focused_prefab_proof',
            lambda doc: doc.update(raw_capture_result='Passed', raw_capture_pass_marker_present=True))

    def test_rejects_portable_binding_or_context_mismatch(self):
        self.reject_with_document_mutation('portable_binding_wrapper',
            lambda doc: doc.update(camera_framing_unobserved=False))

    def test_rejects_factory_projection_domain_mismatch(self):
        self.reject_with_document_mutation('factory projection artifact',
            lambda doc: doc.update(adversarial_projection_change=True), 'factory-metadata-epoch')

    def test_rejects_duplicate_json_properties(self):
        with tempfile.NamedTemporaryFile('w', dir=ROOT, suffix='.json', delete=False) as stream:
            stream.write('{"status":"Passed","status":"Failed"}')
            path = Path(stream.name)
        try:
            desc = {'path': str(path), 'sha256': hashlib.sha256(path.read_bytes()).hexdigest()}
            with self.assertRaises(ValueError):
                validator._read(desc, 'duplicate JSON test', ROOT)
        finally:
            path.unlink(missing_ok=True)

    @classmethod
    def tearDownClass(cls):
        for temp in cls._temps:
            temp.cleanup()


if __name__ == '__main__':
    unittest.main()
