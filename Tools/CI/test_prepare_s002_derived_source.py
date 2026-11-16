import hashlib
import json
import os
from pathlib import Path
import subprocess
import tempfile
import time
import unittest

ROOT=Path(__file__).resolve().parents[2]
BUILD=os.environ.get('S002_PREP_BUILD');BUNDLE=os.environ.get('S002_PREP_BUNDLE')

@unittest.skipUnless(BUILD and BUNDLE,'set S002_PREP_BUILD and S002_PREP_BUNDLE to retained current S002 inputs')
class PreparationControls(unittest.TestCase):
 def setUp(self):
  self.temp=tempfile.TemporaryDirectory(dir='/private/tmp');self.root=Path(self.temp.name);self.bundle=json.loads(Path(BUNDLE).read_text());self.initial=json.loads(Path(self.bundle['initialization']['path']).read_text())
 def tearDown(self):self.temp.cleanup()
 def command(self,bundle,seed,out):return ['python3',str(ROOT/'Tools/CI/prepare_s002_derived_source.py'),'--build-receipt',BUILD,'--data-bundle',str(bundle),'--seed',str(seed),'--output',str(out)]
 def modified_bundle(self,initial):
  p=self.root/'initialization.json';p.write_text(json.dumps(initial));b=dict(self.bundle);b['initialization']={'path':str(p),'sha256':hashlib.sha256(p.read_bytes()).hexdigest()};q=self.root/'bundle.json';q.write_text(json.dumps(b));return q
 def test_actual_preparation_and_output_binding(self):
  out=self.root/'success';r=subprocess.run(self.command(BUNDLE,87322,out),capture_output=True,text=True,timeout=120);self.assertEqual(r.returncode,0,r.stdout+r.stderr)
  d=out/'data';initial=json.loads((d/'initialization.json').read_text());bundle=json.loads((d/'data-bundle.json').read_text());self.assertEqual(initial['s002_derived_seed']['path'],str(d/'qualification.json'));self.assertEqual(bundle['initialization']['path'],str(d/'initialization.json'));self.assertTrue((d/'preparation-receipt.json').is_file());self.assertFalse((out/'.staging').exists())
 def test_wrong_graph_and_matrix(self):
  for key in ('source_config_graph','setup_matrix'):
   with self.subTest(key=key):
    initial=json.loads(json.dumps(self.initial));initial[key]['sha256']='0'*64;out=self.root/(key+'-out');r=subprocess.run(self.command(self.modified_bundle(initial),87322,out),capture_output=True,text=True,timeout=120);self.assertNotEqual(r.returncode,0);self.assertFalse((out/'data').exists())
 def test_seed_and_existing_output(self):
  for seed in (1,104731,2147483648):
   out=self.root/str(seed);r=subprocess.run(self.command(BUNDLE,seed,out),capture_output=True,text=True,timeout=120);self.assertNotEqual(r.returncode,0);self.assertFalse((out/'data').exists())
  out=self.root/'existing';out.mkdir();r=subprocess.run(self.command(BUNDLE,87322,out),capture_output=True,text=True,timeout=120);self.assertNotEqual(r.returncode,0);self.assertFalse((out/'data').exists())
 def test_graph_drift_during_preparation(self):
  initial=json.loads(json.dumps(self.initial));graph=self.root/'graph.json';graph.write_bytes(Path(initial['source_config_graph']['path']).read_bytes());initial['source_config_graph']={'path':str(graph),'sha256':hashlib.sha256(graph.read_bytes()).hexdigest()};out=self.root/'drift';p=subprocess.Popen(self.command(self.modified_bundle(initial),87322,out),stdout=subprocess.PIPE,stderr=subprocess.STDOUT,text=True)
  for _ in range(400):
   if (out/'build/compile.log').exists() or p.poll() is not None:break
   time.sleep(.025)
  with graph.open('a') as f:f.write(' ')
  output=p.communicate(timeout=120)[0];self.assertNotEqual(p.returncode,0,output);self.assertFalse((out/'data').exists())

if __name__=='__main__':unittest.main()
