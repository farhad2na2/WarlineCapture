"""Bounded controls for pin verification and owned-module verify/merge boundary checks."""
import argparse,hashlib,runpy,tempfile
from pathlib import Path
from unittest.mock import patch
p=argparse.ArgumentParser();p.add_argument('--ci-root',required=True);a=p.parse_args();ci=Path(a.ci_root).resolve();runpy.run_path(str(ci/'run_skirmish_source_simulation.py'),run_name='qualified_test_bootstrap')
import skirmish_owned_source as owned
b=owned.require('skirmish_source_build');real=owned.require
with tempfile.TemporaryDirectory(prefix='source-pin-controls-',dir='/private/tmp') as tmp:
 p=Path(tmp)/'input';p.write_bytes(b'original');good=hashlib.sha256(p.read_bytes()).hexdigest();pins={str(p):good};b.verify(pins);b.merge_pins(pins,{str(p):good})
 def reject(label,action):
  try:action()
  except Exception:return
  raise AssertionError(label+' accepted')
 reject('conflicting hash',lambda:b.merge_pins(pins,{str(p):'0'*64}))
 p.write_bytes(b'changed');reject('changed pin',lambda:b.verify(pins));p.write_bytes(b'original')
 calls=[]
 def require(name):
  calls.append(name)
  if len(calls)==2:raise ValueError('owned module changed during verification')
  return real(name)
 with patch.object(owned,'require',side_effect=require):reject('owned module changed during verification',lambda:b.verify(pins))
 assert len(calls)==2
 calls=[]
 def require_merge(name):
  calls.append(name)
  if len(calls)==4:raise ValueError('owned module changed during merge')
  return real(name)
 with patch.object(owned,'require',side_effect=require_merge):reject('owned module changed during merge',lambda:b.merge_pins(pins,{str(p):good}))
 assert len(calls)==4
print('[SourcePinVerificationControls] Passed matched, changed, conflict and owned-module verify/merge boundary checks')
