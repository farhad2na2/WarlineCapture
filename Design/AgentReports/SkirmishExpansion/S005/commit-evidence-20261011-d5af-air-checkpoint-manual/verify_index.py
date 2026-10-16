"""External evidence preparation; default writes only a uniquely named /private/tmp proposal.
Root may create the repository archive only with --write-reviewed-proposal and its exact SHA.
No Unity, Git, source edits, ambient logs, active-run capture or failed-proof promotion.
"""
from pathlib import Path
import argparse,ast,csv,gzip,hashlib,importlib.util,json,os,re
ROOT=Path('/Users/farhad/Projects/WarlineCapture')
PREFIX='Design/AgentReports/SkirmishExpansion/S005/'
REPORT=ROOT/PREFIX
SOURCE='d5afc88f225920451037f6e1c9f7e9d83dfc567e3bbdadfb5b6d65d9ee6774f4'
HISTORY=REPORT/'commit-evidence-20261011-final-normal-matrix/index.json'
HISTORY_SHA='0de7bcdc0bca2bf3f7d8a3728243eab27446bb0396c5510ea7b073486e90cf2b'
OUTPUT=REPORT/'commit-evidence-20261011-d5af-air-checkpoint-manual'
CREDENTIAL_PATTERNS={
 'privateKey':rb'-----BEGIN (?:RSA |EC |OPENSSH )?PRIVATE KEY-----',
 'githubToken':rb'\b(?:gh[pousr]_[A-Za-z0-9]{30,}|github_pat_[A-Za-z0-9_]{40,})\b',
 'apiKey':rb'\bsk-(?:proj-)?[A-Za-z0-9_-]{20,}\b',
 'awsAccessId':rb'\b(?:AKIA|ASIA)[A-Z0-9]{16}\b',
 'googleApiKey':rb'\bAIza[A-Za-z0-9_-]{30,}\b',
 'bearer':rb'(?i)\bbearer\s+[A-Za-z0-9._~+/-]{20,}'}
def sha(value):return hashlib.sha256(value).hexdigest()
def read(path):return json.loads(path.read_text())
def exact(path):return str(path) if not path.is_relative_to(ROOT) else str(path.relative_to(ROOT))
def native_ok(raw):
 assert raw['success'] is True and raw['data']['result']['success'] is True
 assert not any(str(x.get('severity')).lower()=='error' for x in raw['data']['result'].get('diagnostics',[]))
 return raw['data']['result'].get('result')
def idle(value):
 if 'data' in value:value=native_ok(value)
 assert isinstance(value,dict)
 for alternatives in [('playing','isPlaying','IsPlaying'),('changing','Changing'),('compiling','isCompiling','IsCompiling'),('updating','IsUpdating')]:
  key=next((k for k in alternatives if k in value),None);assert key and value[key] is False
 if 'qaRunning' in value:assert value['qaRunning'] is False
 if 'ValidationRunning' in value:assert value['ValidationRunning'] is False
 assert value.get('project')==str(ROOT/'Assets') or value.get('Project')==str(ROOT/'Assets') or value.get('projectPath')==str(ROOT)
def jsonl(value):return [json.loads(x) for x in value.decode().splitlines() if x.strip()]
def verify_runs(index,data):
 assert index['sourceHash']==SOURCE and index['readinessClaim'] is False
 assert len(index['dependencies'])==11 and index['failed1177Promoted'] is False
 helper={};exec(data('Tools/CI/audit_skirmish_native_contacts.py').decode(),helper)
 for run in index['runs']:
  n=run['id'];proof=json.loads(data(PREFIX+run['proofName']));assert proof==run['proof']
  assert proof['sourceHash']==SOURCE and proof['fullSourceUnchanged'] is True
  assert proof['actualWrapperExit']==proof['nativeBridgeExit']==(1 if n==1185 else 0)
  assert proof.get('playerReady') is False
  if n in (1177,1185):
   assert proof['result']=='Failed' and proof['errors'] and proof['countsForCurrentManualAcceptance'] is False
  else:assert proof['result'].startswith('Passed') and not proof['errors']
  manifests=[proof[k] for k in ('preManifest','preflightManifest','preDispatchManifest','postManifest') if k in proof]
  assert len(manifests)>=2 and len({data(PREFIX+p) for p in manifests})==1
  for p in manifests:
   fp=p.replace('-source-manifest.json','-independent-fingerprint.json')
   assert json.loads(data(PREFIX+fp))['sourceHash']==SOURCE
  log=data(PREFIX+proof['log']);assert sha(log)==proof['logSha256']
  assert int(data(PREFIX+proof['log']+'.exit'))==proof['nativeBridgeExit'] and int(data(PREFIX+run['wrapperReceipt']))==proof['actualWrapperExit']
  assert not re.search(rb'error CS\d+|Compilation failed',log)
  for marker in run['requiredMarkers']:assert marker.encode() in log,(n,marker)
  counted=[next(csv.reader([line])) for line in log.decode().splitlines()]
  assert sum(len(row)==20 and row[1]=='S005' for row in counted)==run['countedCsvRows']==0 # No fabricated countedCSV for scoped/manual runs.
  dispatch=json.loads(data(PREFIX+proof['log']+'.dispatch.json'));native_ok(dispatch)
  for p in run['idleEvidence']:idle(json.loads(data(PREFIX+p)))
  for p in run['nativeCompileAndLoad']:
   response=json.loads(data(PREFIX+p));native_ok(response)
   assert response['data']['target']['projectPath']==str(ROOT)
   assert response['data']['parameters']['file']==run['actualCompiledFixturePath'] and response['data']['parameters']['entry']==run['loadOnlyEntry']
   assert str(response['data']['parameters'].get('dry_run','false')).lower()==str(p.startswith('compile')).lower()
  fixture=proof.get('fixture','native1179-checkpoint-focused-fixture.cs.txt')
  assert sha(data(PREFIX+fixture))==proof['fixtureSha256']
  assert sha(data(run['runnerPath']))==run['executedRunnerSha256']
  if 'executedRunnerSha256' in proof:assert proof['executedRunnerSha256']==run['executedRunnerSha256']
  if n==1106:
   start=json.loads(data(PREFIX+'native1106-bilateral-airport-runner-start.json'))
   assert start['executedRunnerSha256']==run['executedRunnerSha256'] and start['fixtureSha256']==proof['fixtureSha256']
   rows=jsonl(data(PREFIX+proof['touchEvidence']));assert rows==proof['allActualTouchRows']
   assert sha(data(PREFIX+proof['touchEvidence']))==proof['touchSha256']
   assert all(x['unexpected']==x['interventions']==x['receiptViolations']==0 for x in rows)
   audit=helper['audit'](rows);assert json.loads(json.dumps(audit))==run['recordedContactAudit']
   assert audit['recordedContacts']==audit['completedContacts'] and audit['completedContacts']>0 and not audit['terminalCancellations']
   assert all(e['reason']=='Runtime contact lacks active command audit' for e in audit['errors'])
   assert audit['strictRecordedContacts'] is False and audit['completeMatchCertified'] is False
   assert any(not x['commandAuditActive'] for x in rows) # Early isolated setup contacts lack normal command audit; raw rejection retained.
   assert proof['paidFourAircraftCapacityCertified'] and proof['actualStandardAirCap2PerFactionConfigurationMeasured']
   assert proof['ownershipRelabeled'] is False and proof['geometryPricesCapsChanged'] is False
   assert proof['planeCargoCertified'] is False and proof['airCombatCertified'] is False and proof['normalMissionCertified'] is False
   assert jsonl(data(PREFIX+'native1106-bilateral-mixed-air.air.jsonl'))==proof['actualNativeAirRows']
  elif n==1179:
   assert proof['passedSuites']==4 and proof['declaredTechnicalCases']==74 and proof['normalPlayerJourneys']==0
   assert proof['fullX36GateClosed'] is False and proof['normalTenReplayCycles']==0
   for marker in proof['requiredSuiteMarkers']:assert marker.encode() in log
   assert sha(data(PREFIX+proof['rawNativeCensus']))==proof['rawCensusSha256']
   assert proof['runnerSha256']==run['executedRunnerSha256']
  elif n==1185:
   trace=proof['trace'];touch=jsonl(data(PREFIX+trace+'.touch.jsonl'))
   assert all(x['unexpected']==x['interventions']==x['receiptViolations']==0 for x in touch)
   audit=helper['audit'](touch);assert json.loads(json.dumps(audit))==proof['inputAudit']
   assert not audit['errors'] and audit['strictAllContactsEnded'] and audit['terminalContactsRecorded'] is False
   obs=jsonl(data(PREFIX+proof['observation']));assert sha(data(PREFIX+proof['observation']))==proof['observationSha256']
   assert any(x['stage']=='ObserverFailure' for x in obs)
   assert not any(x['stage']=='NativeMainMenuDestination' for x in obs)
   assert proof['terminal']['phase']=='Playing' and proof['terminal']['outcome']=='None'
   assert proof['nativePaidHeavyAPC']['materialsPaid']==280 and proof['nativePaidHeavyAPC']['reservation']==1001
   assert proof['normalInput'] is True and proof['fundingReadinessActorPoseDamageVisibilityOutcomeInjected'] is False
   assert proof['countsForCurrentManualAcceptance'] is False and proof['countsForAriaSeedBaseline'] is False
   assert run['nativeRequestedExitCode']==2 # Distinct from actual bridge/native receipt1; preserve both.
  else:
   trace=proof['trace'];assert re.fullmatch(r's005-aria-live-155926-en-[0-9a-f]{32}\.jsonl',trace)
   touch=jsonl(data(PREFIX+trace+'.touch.jsonl'))
   assert all(x['unexpected']==x['interventions']==x['receiptViolations']==0 for x in touch)
   audit=helper['audit'](touch);assert json.loads(json.dumps(audit))==proof['inputAudit']
   assert not audit['errors'] and audit['strictAllContactsEnded'] and audit['terminalContactsRecorded']
   obs=jsonl(data(PREFIX+proof['observation']));assert sha(data(PREFIX+proof['observation']))==proof['observationSha256']
   assert not any(x['stage']=='ObserverFailure' for x in obs)
   terminal=proof['terminal'];assert terminal['outcome']=='Victory' and terminal['phase']=='Finished' and terminal['enemyBaseHp']==0 and terminal['playerBaseHp']>0
   assert terminal['elapsed']==terminal['clockElapsed'] and terminal['clockPaused']==1 and terminal['simulationActive']==0
   assert proof['nativePaidHeavyAPC']['materialsPaid']==280 and proof['nativePaidHeavyAPC']['reservation']==1001
   assert proof['fundingReadinessActorPoseDamageVisibilityOutcomeInjected'] is False and proof['normalInput'] is True
   assert sha(data(PREFIX+proof['fixture']))==proof['fixtureSha256']
   for suffix in ('.terminal-result.png','.terminal-destination.png'):
    assert data(PREFIX+trace+suffix).startswith(b'\x89PNG\r\n\x1a\n')
   if n==1177:
    closure=[x for x in obs if x['stage']=='ClosedObserver'];assert len(closure)==1 and closure[0]['returnSeen'] is False
    assert not any(x['stage']=='NativeMainMenuDestination' for x in obs)
    diagnosis=json.loads(data(PREFIX+'delegation1177-readonly-mainmenu-binding-diagnosis.json'))
    assert diagnosis # preserved independent diagnosis; original evaluator remainsFailed
   else:
    closure=[x for x in obs if x['stage']=='ClosedObserver'];assert len(closure)==1 and all(closure[0][k] for k in ('setupSeen','bindingSeen','purposeSeen','activeControlSeen','resultSeen','returnSeen'))
    victory=[(i,x) for i,x in enumerate(obs) if x['stage']=='NativeRenderedResultText' and x['finished'] and x['frozenVictory'] and x['renderedVictoryWitnessed'] and x['outcome']=='Victory']
    returns=[(i,x) for i,x in enumerate(obs) if x['stage']=='NativeMainMenuDestination']
    assert victory and returns
    for i,x in returns:
     assert i>victory[0][0] and x['frame']>=victory[0][1]['frame'] and x['attempt']==victory[0][1]['attempt']
     assert all(x[k] for k in ('finished','frozenVictory','renderedVictoryWitnessed','presentedControl','renderedBody','unobscuredDestination','destinationSessionAbsent','exactMainMenuOwner','exactMainMenuTitle'))
     assert x['mainMenuIntent']=='OpenMenuRoute' and x['mainMenuRoute']=='QuickCustomSetup'
     assert x['controlPath'].endswith('/Card_Skirmish/Hotspot') and x['mainMenuTitlePath'].endswith('/Card_Skirmish/Title')
 for scan in index['credentialScans']:assert not any(scan['markerCounts'].values())
 return {'closedRuns':len(index['runs']),'failed1177Retained':True,'nativeCyclesOrReadinessClaimed':False}

archive=Path(__file__).resolve().parent
repository=archive.parents[4]
index=read(archive/'index.json')
for dep in index['dependencies']:
 path=repository/dep['index'];assert sha(path.read_bytes())==dep['indexSha256']
 for obj in read(path)['objects']:
  compressed=(path.parent/obj['object']).read_bytes();assert sha(compressed)==obj['gzipSha256'] and len(compressed)==obj['gzipBytes'];raw=gzip.decompress(compressed);assert sha(raw)==obj['sha256'] and len(raw)==obj['bytes']
rawobjects={}
for obj in index['objects']:
 compressed=(archive/obj['object']).read_bytes();assert sha(compressed)==obj['gzipSha256'] and len(compressed)==obj['gzipBytes'];raw=gzip.decompress(compressed);assert sha(raw)==obj['sha256'] and len(raw)==obj['bytes'];rawobjects[obj['sha256']]=raw
aliases={f['originalPath']:rawobjects[f['sha256']] for f in index['files']}
assert len(aliases)==len(index['files'])
print(json.dumps(dict(result='PassedOfflineExactLosslessClosedScope',aliases=len(aliases),historyIndexes=len(index['dependencies']),**verify_runs(index,lambda name:aliases[name]))))
