from pathlib import Path
import gzip,hashlib,json
root=Path(__file__).resolve().parent
idx=json.loads((root/'index.json').read_text())
def sha(b):return hashlib.sha256(b).hexdigest()
for dep in idx['dependencies']:assert sha((root.parent/dep['directory']/'index.json').read_bytes())==dep['indexSha256']
objects={o['sha256']:o for o in idx['objects']}
for digest,o in objects.items():
 c=(root/o['object']).read_bytes();assert sha(c)==o['gzipSha256'] and len(c)==o['gzipBytes']
 b=gzip.decompress(c);assert sha(b)==digest and len(b)==o['bytes']
files={f['originalPath']:f for f in idx['files']}
for dep in idx['dependencies']:
 base=root.parent/dep['directory']; prior=json.loads((base/'index.json').read_text())
 for f in prior['files']:
  if f['originalPath'] not in files:
   files[f['originalPath']]=dict(f,resolvedObject=str((base/f['object']).resolve()))
def data(name):
 f=files[name];path=Path(f['resolvedObject']) if 'resolvedObject' in f else root/objects[f['sha256']]['object'];return gzip.decompress(path.read_bytes())
prefix='Design/AgentReports/SkirmishExpansion/S005/'
for r in idx['runs']:
 assert data(prefix+r['pre'])==data(prefix+r['post'])
 p=json.loads(data(r['proofPath']));log=data(prefix+r['log'])
 assert sha(log)==p['logSha256'] and p['fullSourceUnchanged']
 assert int(data(prefix+r['log']+'.exit'))==r['nativeExit']
 assert int(data(prefix+r['log']+'.wrapper-exit'))==r['wrapperExit']
 assert sha(data(prefix+p['fixture']))==p['fixtureSha256']
 if r['run'] in ('1140','1144'):assert not p['errors'] and '[ExistingEditorValidation] result=Passed' in log.decode()
 else:assert p['result']=='Failed' and p['errors']
 for c in p.get('checkpointArchives',[]):assert sha(data(prefix+c['path']))==c['sha256']
 if r['run']=='1144':assert len(p['actualTypedMarkerCensus'])==8 and all(x['passed'] for x in p['actualTypedMarkerCensus'])
assert not idx['readiness']['playerReady']
print('[S005ClosedRecoveryReconEvidence] result=Passed sourcePairs=4 normalMissions=0 playerReady=0')
