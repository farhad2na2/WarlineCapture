"""Read-only portable replay of the exact frozen1359 scope audit. Never invokes Unity."""
import hashlib,importlib.util,importlib.machinery,json,pathlib,types,sys
sys.dont_write_bytecode=True
BASE=pathlib.Path(__file__).resolve().parent
DEFAULT_REPO=BASE.parents[4] if len(BASE.parents)>4 else None
def sha(p):return hashlib.sha256(pathlib.Path(p).read_bytes()).hexdigest()
def read(p):return json.loads(pathlib.Path(p).read_text())
def verify(repo_root=None):
 repo=pathlib.Path(repo_root).resolve() if repo_root else DEFAULT_REPO
 assert repo is not None, "Explicit repository root required for relocated bundle"
 index=read(BASE/'portable-index.json');frozen=BASE/'frozen'
 assert sha(frozen/'manifest.json')==index['preservedFrozenManifestSha256']=='cfe842fa763fdfab767cfcf5a31d197c020b8a7192a331ffbcc52166e5ede8a2'
 manifest=read(frozen/'manifest.json')
 for name,item in manifest['files'].items():assert sha(frozen/name)==item['sha256'],name
 mapping=index['mapping']
 def resolve(p):
  key=str(p)
  if key in mapping:
   item=mapping[key];base=repo if item['storage']=='repo' else BASE
   relative=pathlib.Path(item['relative']);assert not relative.is_absolute() and '..' not in relative.parts
   dest=base/relative;assert dest.is_file() and sha(dest)==item['sha256'],key
   return dest
  dest=pathlib.Path(p)
  assert dest.resolve().is_relative_to(frozen.resolve()),key
  assert dest.name in manifest['files'] and sha(dest)==manifest['files'][dest.name]['sha256'],key
  return dest
 for key in mapping:resolve(key)
 spec=importlib.util.spec_from_file_location('frozen_native1359_audit',frozen/'audit.py');audit=importlib.util.module_from_spec(spec);spec.loader.exec_module(audit)
 original_spec=importlib.util.spec_from_file_location
 audit.importlib=types.SimpleNamespace(util=types.SimpleNamespace(spec_from_file_location=lambda name,p:original_spec(name,resolve(p),loader=importlib.machinery.SourceFileLoader(name,str(resolve(p)))),module_from_spec=importlib.util.module_from_spec))
 audit.read=lambda p:read(resolve(p));audit.sha=lambda p:sha(resolve(p))
 pins=read(frozen/'evidence-pins.json');failure=read(resolve(pins['originalFailurePath']));report=pathlib.Path(index['historicalProjectRoot'])/'Design/AgentReports/SkirmishExpansion/S005';trace=pathlib.Path(failure['trace']);log=failure['log']
 rowpaths=[report/'native1359-normal-stop-manual-observation.jsonl',report/'native1359-stop-manual-controls.jsonl',report/'native1359-manual-fog-fallback.jsonl',report/'native1359-manual-home-defense.jsonl',trace,pathlib.Path(str(trace)+'.touch.jsonl'),pathlib.Path(str(trace)+'.commands.jsonl')]
 data={'rows':{str(p):[json.loads(x) for x in resolve(p).read_text().splitlines() if x.strip()] for p in rowpaths},'failure':failure,'log':resolve(report/log).read_text(),'wrapper':int(resolve(report/(log+'.wrapper-exit')).read_text()),'native':int(resolve(report/(log+'.exit')).read_text()),'idle':read(resolve(report/'native1359-after-close-idle.json')),'dispatch':read(resolve(report/(log+'.dispatch.json'))),'load':read(resolve(report/'load1359-stop-manual.json')),'dry':read(resolve(report/'compile1359-stop-manual-reviewed-dry-run.json')),'start':read(resolve(report/'native1359-runner-start.json')),'manifests':[read(resolve(report/failure[k])) for k in ('preManifest','preflightManifest','preDispatchManifest','postManifest')],'fingerprints':[read(resolve(report/failure[k].replace('-source-manifest.json','-independent-fingerprint.json'))) for k in ('preManifest','preflightManifest','preDispatchManifest','postManifest')]}
 scoped=audit.qualify(data);expected=read(frozen/'qualified-scoped-native1359-proof.json')
 assert all(scoped[k]==expected[k] for k in scoped) and set(expected)-set(scoped)=={'evidenceReferences','originalFailurePath','originalFailureSha256'}
 assert expected['evidenceReferences']==pins['references'] and expected['originalFailurePath']==pins['originalFailurePath'] and expected['originalFailureSha256']==mapping[pins['originalFailurePath']]['sha256']
 assert failure['result']=='Failed' and scoped['countsForCompleteOriginalFixture'] is False and scoped['fogFallbackQualified'] is False and scoped['normalMissionReadinessQualified'] is False
 # Repeat selected global mutation controls through the mapped frozen functions.
 import copy
 mutations=[]
 for name,change in [('wrong-native-exit',lambda x:x.update(native=1)),('wrong-source-fingerprint',lambda x:x['fingerprints'][-1].update(sourceHash='0'*64)),('lost-fog-restore',lambda x:next(r for r in x['rows'][str(report/'native1359-manual-fog-fallback.jsonl')] if r['stage']=='LeaseRestored').update(restoredExact=False))]:
  mutant=copy.deepcopy(data);change(mutant)
  try:audit.qualify(mutant)
  except (AssertionError,ValueError):mutations.append(name)
  else:raise AssertionError('Portable mutant accepted '+name)
 return {'result':'PassedPortableReadOnlyNative1359ScopedReplay','sourceHash':scoped['sourceHash'],'attempt':scoped['attempt'],'referencesVerified':len(mapping),'frozenPackageByteExact':True,'scopedProofByteSha256':sha(frozen/'qualified-scoped-native1359-proof.json'),'originalFailureByteSha256':mapping[pins['originalFailurePath']]['sha256'],'originalFailureRemainsFailed':True,'controlsQualified':True,'sameAttemptVictoryResultMainMenuWitness':True,'fogFallbackQualified':False,'completeOriginalFixtureQualified':False,'normalMissionReadinessQualified':False,'playerReady':False,'negativePortableControls':mutations,'unityCalled':False,'nativeReplayed':False}
if __name__=='__main__':print(json.dumps(verify(),indent=2))
