from pathlib import Path
import gzip,hashlib,json,runpy
root=Path(__file__).resolve().parent
idx=json.loads((root/'index.json').read_text())
base=root.parent/idx['requiresBaseDirectory']
def sha(b):return hashlib.sha256(b).hexdigest()
assert sha((base/'index.json').read_bytes())==idx['baseIndexSha256']
runpy.run_path(str(base/'verify_index.py'))
objects={o['sha256']:o for o in idx['objects']}
for digest,o in objects.items():
 c=(root/o['object']).read_bytes(); assert sha(c)==o['gzipSha256'] and len(c)==o['gzipBytes']
 b=gzip.decompress(c); assert sha(b)==digest and len(b)==o['bytes']
files={f['originalPath']:f for f in json.loads((base/'index.json').read_text())['files']+idx['files']}
def data(name):
 f=files[name]
 if f['sha256'] in objects:return gzip.decompress((root/objects[f['sha256']]['object']).read_bytes())
 return gzip.decompress((base/f['object']).read_bytes())
prefix='Design/AgentReports/SkirmishExpansion/S005/'
for r in idx['runs']:
 assert data(prefix+r['preSourceManifest'])==data(prefix+r['postSourceManifest'])
 proof=json.loads(data(r['proofPath']))
 log=data(prefix+proof['log']).decode()
 assert int(data(prefix+proof['log']+'.exit'))==r['nativeExit']
 assert int(data(prefix+proof['log']+'.wrapper-exit'))==r['wrapperExit']
 if r['run']=='1136':
  assert r['result']=='Passed' and r['nativeExit']==r['wrapperExit']==0
  assert proof['requiredPassMarker'] in log and not proof['compilerErrors']
 else:
  assert r['result']=='Failed' and r['nativeExit']==r['wrapperExit']==1
  for c in proof['checkpointArchives']:assert sha(data(prefix+c['path']))==c['sha256']
print('[S005SelectedEvidenceSupplement] result=Passed aliases=%d shared=%d new=%d closedSourcePairs=2 technicalSuites=14 normalJourneys=0 playerReady=0' % (len(idx['files']),idx['statistics']['sharedBaseObjects'],idx['statistics']['newObjects']))
