"""Batch capture keeps one epoch and the original full merge verification."""
import tempfile
import unittest
from pathlib import Path
from unittest import mock
import skirmish_source_build as build
from skirmish_source_manifest import UnsupportedSourceShape


class SourcePinBatchTests(unittest.TestCase):
    def test_mixed_duplicate_epochs_reject_before_merge(self):
        with tempfile.TemporaryDirectory() as directory:
            p=Path(directory)/'same';p.write_bytes(b'old');original_pin=build.pin;calls=0
            def capture(path):
                nonlocal calls
                result=original_pin(path);calls+=1
                if calls==1:p.write_bytes(b'new')
                return result
            with mock.patch.object(build,'pin',side_effect=capture),mock.patch.object(build,'merge_pins',wraps=build.merge_pins) as merge:
                with self.assertRaises(UnsupportedSourceShape):build.merge_file_pins({},[p,p])
            merge.assert_not_called()

    def test_bound_file_drift_during_capture_rejected_by_full_merge(self):
        with tempfile.TemporaryDirectory() as directory:
            old=Path(directory)/'old';new=Path(directory)/'new';old.write_bytes(b'bound');new.write_bytes(b'new')
            entry=build.pin(old);pins={entry['path']:entry['sha256']};original_pin=build.pin
            def capture(path):
                result=original_pin(path);old.write_bytes(b'drift');return result
            with mock.patch.object(build,'pin',side_effect=capture),mock.patch.object(build,'verify',wraps=build.verify) as verify:
                with self.assertRaises(UnsupportedSourceShape):build.merge_file_pins(pins,[new])
            verify.assert_called_once()

    def test_newly_captured_drift_before_merge_rejected(self):
        with tempfile.TemporaryDirectory() as directory:
            first=Path(directory)/'first';last=Path(directory)/'last';first.write_bytes(b'first');last.write_bytes(b'last');original_pin=build.pin
            def capture(path):
                result=original_pin(path)
                if Path(path)==last:first.write_bytes(b'drift')
                return result
            pins={}
            with mock.patch.object(build,'pin',side_effect=capture):
                with self.assertRaises(UnsupportedSourceShape):build.merge_file_pins(pins,[first,last])
            self.assertNotIn(str(first.resolve()),pins)

    def test_equivalent_duplicates_keep_one_canonical_epoch(self):
        with tempfile.TemporaryDirectory() as directory:
            p=Path(directory)/'same';p.write_bytes(b'stable');alias=p.parent/'.'/p.name;pins={}
            build.merge_file_pins(pins,[p,alias,p.resolve()])
            self.assertEqual(pins,{str(p.resolve()):build.sha(b'stable')})

    def test_many_files_capture_all_exact_pins_with_one_full_verify(self):
        with tempfile.TemporaryDirectory() as directory:
            paths=[]
            for i in range(100):
                p=Path(directory)/str(i);p.write_bytes(('value-'+str(i)).encode());paths.append(p)
            expected={build.pin(p)['path']:build.pin(p)['sha256']for p in paths};pins={}
            with mock.patch.object(build,'merge_pins',wraps=build.merge_pins) as merge,mock.patch.object(build,'verify',wraps=build.verify) as verify,mock.patch.object(build,'pin',wraps=build.pin) as pin:
                build.merge_file_pins(pins,paths)
            merge.assert_called_once();verify.assert_called_once();self.assertEqual(pin.call_count,len(paths));self.assertEqual(pins,expected)


if __name__=='__main__':unittest.main()
