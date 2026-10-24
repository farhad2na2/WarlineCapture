"""Focused non-Unity guards for the portable predicate runner."""
import importlib.util
import os
from pathlib import Path
import subprocess
import sys
import tempfile
import unittest
from unittest.mock import patch

sys.dont_write_bytecode=True

spec=importlib.util.spec_from_file_location("logical_layout_runner",Path(__file__).with_name("run.py"))
runner=importlib.util.module_from_spec(spec)
spec.loader.exec_module(runner)

class RunnerInputGuards(unittest.TestCase):
    def test_regular_file(self):
        with tempfile.TemporaryDirectory() as directory:
            path=Path(directory)/"source";path.write_bytes(b"actual source")
            self.assertEqual(runner.read_regular(path),b"actual source")
    @unittest.skipUnless(hasattr(os,"mkfifo"),"OS has no FIFO fixture")
    def test_real_fifo_rejected_before_open(self):
        with tempfile.TemporaryDirectory() as directory:
            path=Path(directory)/"fifo";os.mkfifo(path)
            with patch.object(runner.os,"open",side_effect=AssertionError("FIFO opened")) as opened:
                with self.assertRaisesRegex(RuntimeError,"regular file"):runner.read_regular(path)
                opened.assert_not_called()
    def test_sparse_oversize_rejected_before_open(self):
        with tempfile.TemporaryDirectory() as directory:
            path=Path(directory)/"large"
            with path.open("wb") as stream:stream.truncate(runner.FILE_LIMIT+1)
            with patch.object(runner.os,"open",side_effect=AssertionError("Oversize opened")) as opened:
                with self.assertRaisesRegex(RuntimeError,"regular file"):runner.read_regular(path)
                opened.assert_not_called()
    def test_leaf_symlink_rejected(self):
        with tempfile.TemporaryDirectory() as directory:
            path=Path(directory)/"source";path.write_bytes(b"source")
            link=Path(directory)/"link"
            try:link.symlink_to(path)
            except OSError as error:self.skipTest(str(error))
            with self.assertRaisesRegex(RuntimeError,"regular file"):runner.read_regular(link)
    def test_oversize_git_blob_not_read(self):
        with patch.object(runner.subprocess,"check_output",return_value=str(runner.FILE_LIMIT+1).encode()) as command:
            with self.assertRaisesRegex(RuntimeError,"source bound"):runner.read_git(Path.cwd(),"base","source.cs")
            self.assertEqual(command.call_count,1)
    def test_sdk_timeout_partial_logs_preserved(self):
        with tempfile.TemporaryDirectory() as directory:
            output=Path(directory)
            error=subprocess.TimeoutExpired(["dotnet","--version"],30,output=b"partial version",stderr=b"partial error")
            with patch.object(runner.subprocess,"run",side_effect=error):
                with self.assertRaisesRegex(RuntimeError,"partial logs preserved"):runner.query_sdk_version("dotnet",{},output)
            self.assertEqual((output/"sdk-version.stdout.log").read_bytes(),b"partial version")
            self.assertEqual((output/"sdk-version.stderr.log").read_bytes(),b"partial error")

if __name__=="__main__":unittest.main(verbosity=2)
