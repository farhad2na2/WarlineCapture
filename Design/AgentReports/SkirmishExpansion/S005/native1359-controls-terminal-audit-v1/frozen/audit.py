"""Immutable offline scope audit. No Unity, subprocess, gameplay writes or original proof replacement."""
import ast,copy,hashlib,importlib.util,json,math,pathlib,re
BASE=pathlib.Path(__file__).resolve().parent
ROOT=pathlib.Path('/Users/farhad/Projects/WarlineCapture')
REPORT=ROOT/'Design/AgentReports/SkirmishExpansion/S005'
SOURCE='2df3b52828c15c0c82d6f31e79aac04a246ff4aef7805c9be0e40cfbb55ac474'
METHOD='S005NormalStopManual1359Observer.RunValidation'
LOG='native-1359-normal-stop-manual-en104734.log'
def sha(p):return hashlib.sha256(pathlib.Path(p).read_bytes()).hexdigest()
def read(p):return json.loads(pathlib.Path(p).read_text())
def load(name):
 file=BASE/('executed-'+name+'.py');s=importlib.util.spec_from_file_location(name.replace('-','_'),file);m=importlib.util.module_from_spec(s);s.loader.exec_module(m);return m

def native(d,entry,file,result=None,dry=False):
 data=d.get('data',{});inner=data.get('result',{});params=data.get('parameters',{})
 assert d.get('success') is True and data.get('success') is True and inner.get('success') is True
 assert data.get('target',{}).get('projectPath')==str(ROOT) and data['target'].get('port')==7801
 assert params.get('entry')==entry and params.get('file')==str(file) and params.get('timeout_ms')=='30000'
 assert not d.get('errors') and not any(str(x.get('severity','')).lower()=='error' for x in inner.get('diagnostics',[]))
 if result is not None:assert inner.get('result')==result
 if dry:assert params.get('dry_run')=='true'

def raw_data():
 pins=read(BASE/'evidence-pins.json');failure=read(pins['originalFailurePath']);trace=pathlib.Path(failure['trace'])
 paths=[REPORT/'native1359-normal-stop-manual-observation.jsonl',REPORT/'native1359-stop-manual-controls.jsonl',REPORT/'native1359-manual-fog-fallback.jsonl',REPORT/'native1359-manual-home-defense.jsonl',trace,pathlib.Path(str(trace)+'.touch.jsonl'),pathlib.Path(str(trace)+'.commands.jsonl')]
 return {'rows':{str(p):[json.loads(x) for x in p.read_text().splitlines() if x.strip()] for p in paths},'failure':failure,'log':(REPORT/LOG).read_text(),'wrapper':int((REPORT/(LOG+'.wrapper-exit')).read_text()),'native':int((REPORT/(LOG+'.exit')).read_text()),'idle':read(REPORT/'native1359-after-close-idle.json'),'dispatch':read(REPORT/(LOG+'.dispatch.json')),'load':read(REPORT/'load1359-stop-manual.json'),'dry':read(REPORT/'compile1359-stop-manual-reviewed-dry-run.json'),'start':read(REPORT/'native1359-runner-start.json'),'manifests':[read(REPORT/failure[k]) for k in ('preManifest','preflightManifest','preDispatchManifest','postManifest')],'fingerprints':[read(REPORT/failure[k].replace('-source-manifest.json','-independent-fingerprint.json')) for k in ('preManifest','preflightManifest','preDispatchManifest','postManifest')]}

def qualify(data):
 failure=data['failure'];pins=read(BASE/'evidence-pins.json');pkg=pathlib.Path(pins['executedPackage']);attempt=pins['attempt']
 assert failure['result']=='Failed' and failure['errors']==pins['exactOriginalErrors']==['Missing/malformed actual proof ValueError: Fallback companion failed or QA lease unclosed']
 assert failure['sourceHash']==SOURCE and failure['fullSourceUnchanged'] is True and failure['gameplayStateInjected'] is False
 assert data['wrapper']==data['native']==failure['wrapperExit']==failure['nativeExit']==0
 assert all(x==data['manifests'][0] for x in data['manifests']) and all(x['sourceHash']==SOURCE for x in data['fingerprints'])
 start=data['start'];assert start['explicitHeavyLaneOwner']=='main1359' and start['method']==METHOD and start['normalInput'] is True and start['plannedStop'] is True and start['gameplayStateInjected'] is False and start['unattendedAriaSeedWin'] is False and start['sourceHash']==SOURCE
 assert start['fixtureSha256']==failure['fixtureSha256']==sha(BASE/'executed-S005NormalStopManual1359Observer.cs')
 assert start['runnerSha256']==failure['runnerSha256']==sha(BASE/'executed-runner.py')
 native(data['idle'],'S005NormalStopIdle1359.Run',pkg/'S005NormalStopIdle1359.cs','False|False|False|False|False|'+str(ROOT/'Assets'))
 native(data['dispatch'],'WarlineExistingValidationDispatch.Run',REPORT/(LOG+'.dispatch.cs'),'scheduled')
 native(data['load'],'S005NormalStopManual1359Observer.Run',pkg/'S005NormalStopManual1359Observer.cs')
 native(data['dry'],'S005NormalStopManual1359Observer.Run',pkg/'S005NormalStopManual1359Observer.cs',dry=True)
 # Re-execute the exact original read-only collector AST, including ALL global gates
 # and the original failing fog qualifier. No dispatch/write/owner cleanup nodes selected.
 tree=ast.parse((BASE/'executed-runner.py').read_text());main=next(x for x in tree.body if isinstance(x,ast.FunctionDef) and x.name=='main')
 begin=next(i for i,x in enumerate(main.body) if isinstance(x,ast.Assign) and isinstance(x.targets[0],ast.Name) and x.targets[0].id=='text')
 end=next(i for i,x in enumerate(main.body[begin:],begin) if isinstance(x,ast.Try))
 nodes=main.body[begin:end+1]
 assert isinstance(nodes[-1],ast.Try) and len(nodes[-1].handlers)==1
 # The single direct disk log-read is replaced by supplied immutable/in-memory text
 # for mutation controls; all remaining collector and pure qualifiers stay unchanged.
 ns={'ROOT':ROOT,'REPORT':REPORT,'LOG':LOG,'CONTROLS':'native1359-stop-manual-controls.jsonl','OBS':'native1359-normal-stop-manual-observation.jsonl','FOG':'native1359-manual-fog-fallback.jsonl','METHOD':METHOD,'wrapper':data['wrapper'],'native':data['native'],'same':True,'errors':[],'re':re,'pathlib':pathlib,'json':json,'importlib':importlib,'load':load,'rows':lambda p:copy.deepcopy(data['rows'][str(p)]),'text':data['log']}
 exec(compile(ast.fix_missing_locations(ast.Module(body=nodes[1:],type_ignores=[])),'immutable-executed1359-collector','exec'),ns)
 assert ns['errors']==pins['exactOriginalErrors'],ns['errors']
 assert ns['binding']==failure['actualResolvedSetup'] and ns['terminal']==failure['terminal'] and ns['audit']==failure['inputAudit'] and ns['controlProof']==failure['controlProof'] and ns['visualProof']==failure['visualProof']
 assert ns['binding']['attempt']==attempt and ns['fogProof'] is None
 controls=data['rows'][str(REPORT/'native1359-stop-manual-controls.jsonl')];fog=data['rows'][str(REPORT/'native1359-manual-fog-fallback.jsonl')];home=data['rows'][str(REPORT/'native1359-manual-home-defense.jsonl')]
 def one(rows,stage):
  found=[x for x in rows if x.get('stage')==stage];assert len(found)==1,(stage,len(found));return found[0]
 passed=one(controls,'ControlsPassed');failed=one(fog,'Failure');closed=one(fog,'Closed');hclosed=one(home,'Closed')
 assert passed['frame']==2585 and failed['frame']==5377 and passed['frame']<failed['frame']
 assert failed['attempt']==attempt and failed['round']==4 and failed['candidate']==3 and failed['reason'].startswith('System.InvalidOperationException: All3 public ground approach candidates rejected\n')
 assert closed['restoredExact'] is True and closed['reason']=='NativeClosed;Failed:'+failed['reason'] and closed['attempt']==attempt
 # Every lease must relinquish the same QA due field to its exact original value,
 # including failed round4. Closed frame1 is post-Play teardown, never progress.
 starts=[x for x in fog if x['stage']=='LeaseStarted'];restores=[x for x in fog if x['stage']=='LeaseRestored']
 assert len(starts)==len(restores)==4 and [x['round'] for x in starts]==[1,2,3,4]
 for a in starts:
  rr=[x for x in restores if x['round']==a['round']];assert len(rr)==1;b=rr[0]
  assert a['attempt']==b['attempt']==attempt and b['frame']>=a['frame'] and b['sameDriver'] is True and b['restoredExact'] is True
  assert math.isfinite(a['originalDue']) and b['originalDue']==a['originalDue']
 assert restores[-1]['frame']==failed['frame'] and restores[-1]['reason']=='CompanionFailure'
 rejected=[x for x in fog if x['stage']=='RejectedApproach'];assert len(rejected)==3 and [x['candidate'] for x in rejected]==[0,1,2] and all(x['round']==4 and x['attempt']==attempt and x['receipt']==0 for x in rejected)
 assert hclosed['stage']=='Closed' and hclosed['reason']=='NativeClosed' and hclosed['restoredExact'] is True and hclosed['nativePumpDisabled'] is False and hclosed['attempt']==attempt
 assert not any(x['stage']=='Failure' for x in home)
 # Prefix-only handoff check: exclude no tactical row, but use source-native
 # post-Play closure separately because Time.frameCount resets after teardown.
 handoff=load('handoff_qualifier').qualify_handoff(controls,[x for x in home if x['stage']!='Closed'],attempt)
 assert ns['audit']['recordedContacts']==ns['audit']['completedContacts']==145 and ns['audit']['terminalCancellations']==[] and ns['audit']['strictAllContactsEnded'] is True
 return {'result':'PassedScopedOfflineNative1359ControlsAndTerminalWitness','sourceHash':SOURCE,'attempt':attempt,'scope':'Deliberate ARIAStop followed by paired native Move/Hold/Pause/Resume/replacementMove controls and later genuine same-attempt Victory/rendered MainMenu. Overall run/companion remains Failed.','controlProof':ns['controlProof'],'visualProof':ns['visualProof'],'inputAudit':ns['audit'],'nativeTerminal':ns['terminal'],'handoffPrefixProof':handoff,'fallbackFailure':{'frame':failed['frame'],'round':4,'reason':failed['reason'],'allFourQaLeasesRestoredExact':True,'fallbackQualified':False},'homeDefenseEffectQualified':False,'wrapperExit':0,'nativeExit':0,'fullSourceUnchanged':True,'originalFailurePreserved':True,'countsForNormalStopManualControls':True,'sameAttemptVictoryResultMainMenuWitness':True,'countsForCompleteOriginalFixture':False,'countsForAriaSeedBaseline':False,'countsForPhysicalTakeover':False,'fogFallbackQualified':False,'normalMissionReadinessQualified':False,'playerReady':False,'humanDeviceAcceptance':False,'nativeReplayed':False,'gameplayStateInjected':False}

def audit():
 pins=read(BASE/'evidence-pins.json')
 for path,v in pins['references'].items():assert pathlib.Path(path).is_file() and sha(path)==v['sha256'],path
 for n in ['runner.py','qualifier.py','fog-qualifier.py','handoff_qualifier.py','home-defense-qualifier.py','S005NormalStopManual1359Observer.cs','S005NormalStopIdle1359.cs']:
  assert sha(BASE/('executed-'+n))==pins['references'][str(pathlib.Path(pins['executedPackage'])/n)]['sha256']
 result=qualify(raw_data());result['evidenceReferences']=pins['references'];result['originalFailurePath']=pins['originalFailurePath'];result['originalFailureSha256']=pins['references'][pins['originalFailurePath']]['sha256'];return result
if __name__=='__main__':
 result=audit()
 with (BASE/'qualified-scoped-native1359-proof.json').open('x') as f:json.dump(result,f,indent=2);f.write('\n')
 print(json.dumps({k:result[k] for k in ('result','sourceHash','attempt','wrapperExit','nativeExit','countsForNormalStopManualControls','fogFallbackQualified','normalMissionReadinessQualified')}))
