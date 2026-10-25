"""Finite current generator byte/epoch execution controls; no gameplay fixtures."""
import os
import py_compile
import tempfile
import unittest
from pathlib import Path
from unittest import mock
import skirmish_source_build as build
from skirmish_source_manifest import UnsupportedSourceShape


class NavigationIdentityExecutionTests(unittest.TestCase):
    def test_hashes_and_executes_same_fresh_bytes_despite_valid_stale_pyc(self):
        with tempfile.TemporaryDirectory() as directory:
            root=Path(directory);tool=root/'generator.py';out=root/'identity.cs'
            template="from pathlib import Path\nVALUE='OLD'\ndef generate(inputs,output):\n Path(output).write_text(VALUE)\n return (__file__,VALUE)\n"
            tool.write_text(template);stamp=tool.stat();py_compile.compile(str(tool),doraise=True)
            fresh=template.replace("'OLD'","'NEW'").encode();tool.write_bytes(fresh);os.utime(tool,ns=(stamp.st_atime_ns,stamp.st_mtime_ns))
            pins={};original_compile=compile
            with mock.patch('builtins.compile',wraps=original_compile) as compiler:
                result=build._generate_navigation_identity(tool,root/'sources.rsp',out,pins)
            self.assertEqual(result,(str(tool.resolve()),'NEW'));self.assertEqual(out.read_text(),'NEW')
            self.assertEqual(compiler.call_args.args[0],fresh)
            self.assertEqual(pins[str(tool.resolve())],build.sha(fresh))

    def test_execution_source_drift_rejects_before_generator(self):
        with tempfile.TemporaryDirectory() as directory:
            root=Path(directory);tool=root/'generator.py';out=root/'identity.cs'
            tool.write_text("from pathlib import Path\nPath(__file__).write_text('# changed')\ndef generate(inputs,output):\n Path(output).write_text('must not run')\n")
            with self.assertRaises(UnsupportedSourceShape):
                build._generate_navigation_identity(tool,root/'sources.rsp',out,{})
            self.assertFalse(out.exists())

    def test_generation_source_drift_rejects_before_build(self):
        with tempfile.TemporaryDirectory() as directory:
            root=Path(directory);tool=root/'generator.py';out=root/'identity.cs'
            tool.write_text("from pathlib import Path\ndef generate(inputs,output):\n Path(__file__).write_text('# changed')\n return {}\n")
            with self.assertRaises(UnsupportedSourceShape):
                build._generate_navigation_identity(tool,root/'sources.rsp',out,{})


if __name__=='__main__':
    unittest.main()
