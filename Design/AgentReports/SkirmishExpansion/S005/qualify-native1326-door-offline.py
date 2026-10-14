"""Offline supplement only. Preserves original native1326 failure; no Editor/source/state mutation."""
import ast,hashlib,importlib.util,json,pathlib,re
ROOT=pathlib.Path('/Users/farhad/Projects/WarlineCapture');REPORT=ROOT/'Design/AgentReports/SkirmishExpansion/S005';BUNDLE=pathlib.Path('/private/tmp/s005-plane-door1326')
FAILURE=REPORT/'native1326-plane-door-failure.json';EXPECTED_SOURCE='c69396566ba9875bb46514d07a9b1972d717debe59abb538c7fb6100d85098d0';METHOD='S005PaidTransportPlane1326.RunValidation'
def sha(p):return hashlib.sha256(p.read_bytes()).hexdigest()
def read(p):return json.loads(p.read_text())
def check(v,msg):
 if not v:raise ValueError(msg)
def verify_closure(text,receipt,wrapper,idle):
 check(text.splitlines()[0]=='[ExistingEditorValidation] project='+str(ROOT/'Assets')+' executeMethod='+METHOD,'Exact log ownership header')
 passed='[ExistingEditorValidation] result=Passed executeMethod='+METHOD
 check(text.splitlines().count(passed)==1,'Exact owned single completion marker')
 check('[ExistingEditorValidation] result=Failed' not in text,'No native bridge failure')
 cleanup='[S005TransportSupportCleanup] result=Passed attemptsRemoved=1'
 check(text.splitlines().count(cleanup)==1 and text.index(cleanup)<text.index(passed),'Actual own cleanup before completion')
 check(receipt.strip()=='0' and wrapper.strip()=='0','Exact native/wrapper0 receipts')
 check(not re.search(r'\[SkirmishValidationExit\] result=Requested code=(?!0(?:\s|$))\d+',text),'Any recorded harness exit must be0')
 data=idle.get('data',{});inner=data.get('result',{})
 check(idle.get('success') is True and data.get('success') is True and inner.get('success') is True,'Actual idle nested success')
 check(data.get('target',{}).get('projectPath')==str(ROOT),'Exact idle target')
 check(inner.get('result')=='False|False|False|False|False|'+str(ROOT/'Assets'),'Actual native after-close idle flags')

def main(output):
 d=read(FAILURE);check(d['result']=='Failed' and d['errors']==['Missing native marker [SkirmishValidationExit] result=Requested code=0'],'Original failure has exactly only nonuniversal marker discrepancy')
 check(d['sourceHash']==EXPECTED_SOURCE and d['fullSourceUnchanged'] is True and d['wrapperExit']==d['nativeExit']==0,'Original run source/native facts')
 m=read(BUNDLE/'manifest.json')
 for n,h in m['files'].items():check(sha(BUNDLE/n)==h,'Frozen dependency '+n)
 check(d['fixtureSha256']==sha(BUNDLE/'S005PlaneDoorDiagnostic1326.cs') and d['runnerSha256']==sha(BUNDLE/'runner.py'),'Exact executed frozen fixture/runner')
 check(sha(REPORT/'native1326-fixture.cs.txt')==d['fixtureSha256'],'Exact archived native fixture')
 log=REPORT/d['log'];check(sha(log)==d['logSha256'],'Raw log checksum')
 census=REPORT/d['censusPath'];touch=REPORT/d['touchPath'];check(sha(census)==d['censusSha256'] and sha(touch)==d['touchSha256'],'Raw census/Touch checksums')
 idlePath=REPORT/'native1326-after-close-idle.json';text=log.read_text();verify_closure(text,pathlib.Path(str(log)+'.exit').read_text(),pathlib.Path(str(log)+'.wrapper-exit').read_text(),read(idlePath))
 evidence=[FAILURE,log,pathlib.Path(str(log)+'.exit'),pathlib.Path(str(log)+'.wrapper-exit'),idlePath,census,touch,REPORT/'native1326-fixture.cs.txt',BUNDLE/'manifest.json',BUNDLE/'runner.py',BUNDLE/'qualifier.py']
 manifests=[]
 for key in ('preManifest','preflightManifest','preDispatchManifest','postManifest'):
  p=REPORT/d[key];manifests.append(read(p));evidence.append(p);finger=REPORT/p.name.replace('-source-manifest.json','-independent-fingerprint.json');check(read(finger)['sourceHash']==EXPECTED_SOURCE,'Exact original source hash '+finger.name);evidence.append(finger)
 check(all(v==manifests[0] for v in manifests),'Full original pre/dispatch/post equality')
 asttree=ast.parse((BUNDLE/'runner.py').read_text());markers=next(ast.literal_eval(n.value) for n in asttree.body if isinstance(n,ast.Assign) and any(isinstance(t,ast.Name) and t.id=='MARKERS' for t in n.targets))
 for marker in markers:
  if marker!='[SkirmishValidationExit] result=Requested code=0':check(marker in text,'All original applicable markers '+marker)
 check(not re.search(r'error CS\d+|Compilation failed|\[S005PlaneDoorDiagnostic1326\] result=Failed',text),'No original fixture/compiler error')
 rr=[json.loads(l) for l in touch.read_text().splitlines() if l.strip()];check(rr and all(all(r.get(k)==0 for k in ('unexpected','interventions','receiptViolations')) for r in rr),'All native raw Touch violations0')
 check(rr[-1]['completed']==rr[-1]['dispatched'] and rr[-1]['completed']>0,'Actual final matched contacts')
 counts=re.search(r'\[S005TransportSupportInput\] dispatched=(\d+) completed=(\d+) acceptedCommands=(\d+) violations=(\d+)',text);check(counts and int(counts[1])>0 and counts[1]==counts[2] and int(counts[3])>=0 and int(counts[4])==0,'Original native input audit')
 grouped={}
 for r in rr:grouped.setdefault((r['device'],r['generation']),[]).append(r)
 for key,values in grouped.items():
  phases=[r['kind'] for r in values if r['kind'] in ('Dispatched','Began','Ended','Canceled','PhysicalTakeover')];check(len(phases)%3==0 and all(phases[i:i+3]==['Dispatched','Began','Ended'] for i in range(0,len(phases),3)),'Every native contact actually paired '+str(key))
 for role in ('Rifle','ApcFast'):check(re.search(r'\[S005TransportNativeMoveReceipt\] role='+role+r' receipt=[1-9]\d* verified=1 displacementOrigin=AfterAcceptedMove',text),'Earlier actual native paid movement '+role)
 rows=[json.loads(l) for l in census.read_text().splitlines() if l.strip()];spec=importlib.util.spec_from_file_location('frozen1326qualifier',BUNDLE/'qualifier.py');mod=importlib.util.module_from_spec(spec);spec.loader.exec_module(mod);diagnostic=mod.qualify(rows);check(json.loads(json.dumps(diagnostic))==d['diagnostic'],'All original Door diagnostic assertions reproduced')
 dry=REPORT/d['dryCompileProof'];check(sha(dry)==d['dryCompileProofSha256'],'Original dry proof checksum');evidence.append(dry)
 proof={'result':'PassedReadOnlyPaidPlaneDoorCensusOfflineSupplement','originalFailure':FAILURE.name,'originalFailureSha256':sha(FAILURE),'sourceHash':EXPECTED_SOURCE,'fullSourceUnchanged':True,'wrapperExit':0,'nativeExit':0,'scope':'Read-only actual paid actor/producer/Door/prefab capture only. Original failure retained; no native replay.','reason':'Task completion closes ExistingEditorValidation capture before asynchronously scheduled harness FinishQuitAfterPlayMode can log SkirmishValidationExit. Harness Requested marker is not universal Task closure evidence.','closureGate':'Exact project/method ownership header + actual own cleanup + exact native ExistingEditorValidation Passed + numeric native/wrapper0 + actual primitive idle + full source equality. No fabricated marker.','missingHarnessExitMarker':True,'allOriginalApplicableChecksReplayed':True,'diagnostic':diagnostic,'rawEvidence':{str(p):sha(p) for p in evidence},'scriptSha256':sha(pathlib.Path(__file__)),'freshBakePerformed':False,'alignmentCertified':False,'boardingCertified':False,'cargoLifecycleCertified':False,'checkpointPerformed':False,'normalMissionCertified':False,'playerReady':False,'humanDeviceAcceptance':False,'native1322CargoFailureRemainsFailed':True,'limitation':'Current loaded source metadata mismatch localized before paid clone; no new fresh/incremental bake or causal cache proof. Door graph/matrices observed without force-refresh.'}
 with output.open('x') as f:json.dump(proof,f,indent=2);f.write('\n')
 print(json.dumps({'result':proof['result'],'proof':str(output),'sha256':sha(output),'playerReady':False}))
if __name__=='__main__':main(REPORT/'native1326-plane-door-offline-qualified.json')
