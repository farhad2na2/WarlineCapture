import hashlib
import json
import tempfile
import unittest
from pathlib import Path
from unittest.mock import patch

import verify_s004_merged_source_epoch as verifier


class MergedSourceEpochTests(unittest.TestCase):
    def setUp(self):
        self.tmp=tempfile.TemporaryDirectory();self.addCleanup(self.tmp.cleanup)
        self.root=Path(self.tmp.name).resolve();self.evidence=self.root/'evidence.json'
        self.evidence.write_text(json.dumps({'runtime_admission':False,'mission_ready':False}))
        self.bundle=self.root/'bundle.json'
        self.value={'kind':'S004SourceDiagnosticInputBundle','mission_id':'S004','scope':'SourceHostDiagnosticOnly',
                    'qualification':{'mission_ready':False},'inputs':{'a':1},
                    'evidence':{'path':str(self.evidence),'sha256':self.sha(self.evidence)}}
        self.bundle.write_text(json.dumps(self.value))
        self.output=self.root/'reconstruction'

    @staticmethod
    def sha(path):return hashlib.sha256(path.read_bytes()).hexdigest()

    def rebuild(self,output):
        output.mkdir(); evidence=output/'input-evidence.json';evidence.write_bytes(self.evidence.read_bytes())
        bundle=output/'bundle.json';value=dict(self.value);value['evidence']={'path':str(evidence),'sha256':self.sha(evidence)}
        bundle.write_text(json.dumps(value))
        return {'bundle':{'path':str(bundle)},'evidence':{'path':str(evidence)}}

    def test_exact_rebuild_remains_unqualified(self):
        with patch.object(verifier.epoch,'ROOT',self.root),patch.object(verifier.epoch,'build',side_effect=self.rebuild):
            result=verifier.verify(self.bundle,self.sha(self.bundle),self.output)
        self.assertFalse(result['runtime_admission']);self.assertFalse(result['mission_ready'])

    def test_changed_bundle_or_promoted_scope_rejected(self):
        with patch.object(verifier.epoch,'ROOT',self.root):
            with self.assertRaisesRegex(ValueError,'SHA-256'):
                verifier.verify(self.bundle,'0'*64,self.output)
            self.value['qualification']['mission_ready']=True;self.bundle.write_text(json.dumps(self.value))
            with self.assertRaisesRegex(ValueError,'unqualified'):
                verifier.verify(self.bundle,self.sha(self.bundle),self.output)

    def test_changed_evidence_and_reconstructed_input_rejected(self):
        with patch.object(verifier.epoch,'ROOT',self.root),patch.object(verifier.epoch,'build',side_effect=self.rebuild):
            self.evidence.write_text('{}')
            with self.assertRaisesRegex(ValueError,'input evidence'):
                verifier.verify(self.bundle,self.sha(self.bundle),self.output)
            self.evidence.write_text(json.dumps({'runtime_admission':False,'mission_ready':False}))
            def changed(output):
                result=self.rebuild(output); path=Path(result['bundle']['path']); data=json.loads(path.read_text());data['inputs']['a']=2;path.write_text(json.dumps(data));return result
            with patch.object(verifier.epoch,'build',side_effect=changed):
                with self.assertRaisesRegex(ValueError,'independently reconstructed'):
                    verifier.verify(self.bundle,self.sha(self.bundle),self.output)

    def test_symlink_alias_rejected(self):
        alias=self.root/'alias.json';alias.symlink_to(self.bundle)
        with patch.object(verifier.epoch,'ROOT',self.root):
            with self.assertRaisesRegex(ValueError,'without symlinks'):
                verifier.verify(alias,self.sha(self.bundle),self.output)


if __name__=='__main__':unittest.main()
