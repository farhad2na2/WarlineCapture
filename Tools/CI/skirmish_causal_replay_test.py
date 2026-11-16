"""Focused admission controls for the retained causal CLI boundary."""
from pathlib import Path
import json
import hashlib
import contextlib
import io
import tempfile
import unittest
from unittest.mock import patch

import skirmish_causal_replay as cli
from skirmish_causal_io import parse, read


class CausalBoundaryControls(unittest.TestCase):
    def setUp(self):
        self.temp = tempfile.TemporaryDirectory()
        self.addCleanup(self.temp.cleanup)
        self.root = Path(self.temp.name).resolve()

    def put(self, name, value):
        path = self.root/name
        path.parent.mkdir(parents=True, exist_ok=True)
        path.write_text(json.dumps(value))
        return path

    def receipt(self, **changes):
        source = self.root/'source.cs'
        source.write_text('original source')
        baseline = self.put('baseline.json', {'pins': {str(source): cli.digest(source)}})
        trace, side = self.put('forward-interval.json', {'legacy': 'test only'}), self.put('continuous-causal-window-v8.json', {'window': 'test only'})
        receipt = dict(wrapperReaped=True, ownedTreeGone=True, strictOperationalControlGate=True,
                       dataCaptureGate=True, wrapperExitCode=0, baseline=str(baseline),
                       baselineSha256=cli.digest(baseline), traceSha256=cli.digest(trace), sidecarSha256=cli.digest(side))
        receipt.update(changes)
        path = self.put('receipt.json', receipt)
        return path, trace, source

    def test_live_or_failed_native_cannot_be_admitted(self):
        for change in ({'wrapperReaped': False}, {'ownedTreeGone': False}, {'wrapperExitCode': 2}, {'wrapperExitCode': False}):
            path, trace, _ = self.receipt(**change)
            with self.subTest(change=change), self.assertRaises(ValueError):
                cli.native_admission(self.root, trace, path, cli.digest(path))

    def test_changed_captured_source_rejected(self):
        path, trace, source = self.receipt()
        source.write_text('changed source')
        with self.assertRaisesRegex(ValueError, 'Captured native source changed'):
            cli.native_admission(self.root, trace, path, cli.digest(path))

    def test_native_artifact_hash_mismatch_rejected(self):
        path, trace, _ = self.receipt()
        trace.write_text('{"legacy":"changed"}')
        with self.assertRaisesRegex(ValueError, 'artifact pins differ'):
            cli.native_admission(self.root, trace, path, cli.digest(path))

    def test_imported_output_rejected_before_creation(self):
        for name in ('Assets', 'Packages', 'ProjectSettings', '.git'):
            output = self.root/name/'must-not-exist'
            with self.subTest(name=name), self.assertRaises(ValueError):
                cli.unique_output(self.root, output)
            self.assertFalse(output.exists())

    def test_existing_output_preserved(self):
        output = self.root/'existing'
        output.mkdir()
        marker = output/'evidence.json'
        marker.write_text('preserve')
        with self.assertRaises(FileExistsError):
            cli.unique_output(self.root, output)
        self.assertEqual(marker.read_text(), 'preserve')

    def test_real_symlink_ancestor_and_json_duplicate_rejected(self):
        actual = self.root/'actual'
        actual.mkdir()
        file = actual/'file.json'
        file.write_text('{}')
        alias = self.root/'alias'
        alias.symlink_to(actual, target_is_directory=True)
        with self.assertRaisesRegex(ValueError, 'Symlink'):
            read(alias/'file.json', 32)
        with self.assertRaisesRegex(ValueError, 'symlink'):
            cli.digest(alias/'file.json')
        with self.assertRaisesRegex(ValueError, 'Duplicate JSON key'):
            parse(b'{"same":1,"same":2}')

    def test_streamed_source_hash_and_size_bound(self):
        path = self.root/'large-source'
        raw = b'original source\n'*10000
        path.write_bytes(raw)
        self.assertEqual(cli.digest(path), hashlib.sha256(raw).hexdigest())
        with self.assertRaisesRegex(ValueError, 'bounded regular hash input'):
            cli.digest(path, len(raw)-1)

    def test_terminal_profiles_required(self):
        good = dict(result='ComputedCausalPrefixThroughOriginalRootExit_FinalStorageAndMaterialBankMatched',
                    computed_terminal_material_matched=True, bank_instances=1, bank_reseeds=0,
                    terminal_storage_assertions=51, terminal_profile_assertions=4)
        cli.computed_terminal_admission(good)
        for count in (None, 3, True):
            changed = dict(good, terminal_profile_assertions=count)
            with self.subTest(count=count), self.assertRaisesRegex(ValueError, 'terminal_profile_assertions'):
                cli.computed_terminal_admission(changed)

    def test_unsupported_runtime_fails_before_output_creation(self):
        output = self.root/'not-created'
        args = ['run', '--project', str(self.root), '--config', 'unused', '--trace', 'unused',
                '--trace-sha256', 'unused', '--native-receipt', 'unused', '--native-receipt-sha256', 'unused',
                '--output', str(output)]
        with patch.object(cli.sys, 'platform', 'win32'), contextlib.redirect_stdout(io.StringIO()) as text:
            self.assertEqual(cli.main(args), 1)
        self.assertIn('qualified only', text.getvalue())
        self.assertFalse(output.exists())


if __name__ == '__main__':
    unittest.main()
