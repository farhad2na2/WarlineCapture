"""1359 native public S005 ARIAStop/manual MoveHold normal journey. Escalated root launch required."""
import argparse,hashlib,importlib.util,json,pathlib,re,subprocess,sys
ROOT=pathlib.Path('/Users/farhad/Projects/WarlineCapture');REPORT=ROOT/'Design/AgentReports/SkirmishExpansion/S005';P=pathlib.Path(__file__).parent
STATE=REPORT/'TECHNICAL_READINESS_CURRENT.json';PREP=P/'prepared.json';HELPER=pathlib.Path('/private/tmp/warline-s005-source-manifest.py');FIXTURE=P/'S005NormalStopManual1359Observer.cs';IDLE=P/'S005NormalStopIdle1359.cs'
LOG='native-1359-normal-stop-manual-en104734.log';METHOD='S005NormalStopManual1359Observer.RunValidation';OBS='native1359-normal-stop-manual-observation.jsonl';CONTROLS='native1359-stop-manual-controls.jsonl';FOG='native1359-manual-fog-fallback.jsonl'
def sha(p):return hashlib.sha256(p.read_bytes()).hexdigest()
def read(p):return json.loads(p.read_text())
def new_json(p,d):
 with p.open('x') as f:json.dump(d,f,indent=2);f.write('\n')
def load(name):
 s=importlib.util.spec_from_file_location(name,P/(name+'.py'));m=importlib.util.module_from_spec(s);s.loader.exec_module(m);return m

def rows(p):
 if not p.is_file():raise ValueError('Missing native evidence '+p.name)
 return [json.loads(l) for l in p.read_text().splitlines() if l.strip()]
def manifest(prefix):
 for suffix in ('-source-manifest.json','-independent-fingerprint.json'):
  if (REPORT/(prefix+suffix)).exists():raise RuntimeError('Preserve existing source evidence '+prefix)
 subprocess.run(['rtk','proxy','python3',str(HELPER),prefix],cwd=ROOT,check=True)
 return prefix+'-source-manifest.json'
def cli(args,out):
 r=subprocess.run(['rtk','proxy','unity','command',*args,'--project-path',str(ROOT),'--format','json'],cwd=ROOT,capture_output=True,text=True)
 with out.open('x') as f:f.write(r.stdout)
 if r.stderr:new_json(out.with_name(out.name+'.stderr.json'),{'stderr':r.stderr,'exit':r.returncode})
 d=json.loads(r.stdout)
 if r.returncode or d.get('success') is not True or d.get('data',{}).get('success') is not True or d.get('data',{}).get('result',{}).get('success') is not True or d.get('data',{}).get('target',{}).get('projectPath')!=str(ROOT):raise RuntimeError('Native CLI outer/nested failure '+out.name)
 return d['data']['result'].get('result')
def idle(tag):
 v=cli(['run_script','--file',str(IDLE),'--entry','S005NormalStopIdle1359.Run','--timeout_ms','30000'],REPORT/('native1359-'+tag+'-idle.json'))
 if v!='False|False|False|False|False|'+str(ROOT/'Assets'):raise RuntimeError('Main native idle ownership differs '+repr(v))
def owner_idle():
 if read(STATE).get('activeRun') is not None:raise RuntimeError('Another main lane is owned')
def verify_dry(d):
 data=d.get('data',{});inner=data.get('result',{});params=data.get('parameters',{})
 if d.get('success') is not True or data.get('success') is not True or inner.get('success') is not True or any(str(v.get('severity','')).lower()=='error' for v in inner.get('diagnostics',[])) or params.get('file')!=str(FIXTURE) or params.get('entry')!='S005NormalStopManual1359Observer.Run' or params.get('dry_run') not in ('true',True) or data.get('target',{}).get('projectPath')!=str(ROOT):raise RuntimeError('Exact1359 current fixture native dry success/parameters/target not proven')

def main():
 ap=argparse.ArgumentParser(description=__doc__);ap.add_argument('pre_manifest');ap.add_argument('--heavy-lane-owner',required=True,choices=['main1359']);ap.add_argument('--dry-compile-proof',required=True);a=ap.parse_args();prep=read(PREP)
 if prep.get('nativeDispatchPermitted') is not True:raise RuntimeError('1359 prepared only; root must authorize a preserved runner/prep successor before dispatch')
 owner_idle()
 dryPath=pathlib.Path(a.dry_compile_proof)
 if dryPath.parent!=REPORT or not dryPath.name.startswith('compile1359-'):raise RuntimeError('Exact compile1359 native dry-proof artifact required')
 verify_dry(read(dryPath))
 if pathlib.Path(a.pre_manifest).name!=a.pre_manifest or not (REPORT/a.pre_manifest).is_file():raise RuntimeError('Existing source-manifest basename required')
 for rel,h in prep['files'].items():
  if sha(P/rel)!=h:raise RuntimeError('Frozen external bytes changed '+rel)
 if sha(HELPER)!=prep['sourceManifestHelperSha256']:raise RuntimeError('Source helper bytes changed')
 for rel,h in prep['sourceContracts'].items():
  if sha(ROOT/rel)!=h:raise RuntimeError('Reviewed current source changed '+rel)
 if any(re.match(r'(?:native-?|load|final-|compile)1359(?:\D|$)',x.name) for x in REPORT.iterdir() if x!=dryPath):raise RuntimeError('Preserve previous1359 artifacts')
 pre=manifest('final-1359-normal-stop-preflight')
 if read(REPORT/a.pre_manifest)!=read(REPORT/pre) or read(REPORT/pre.replace('-source-manifest.json','-independent-fingerprint.json'))['sourceHash']!=prep['sourceHash']:raise RuntimeError('Current full source differs; native not called')
 try:idle('before-load')
 except Exception as e:
  new_json(REPORT/'native1359-preflight-failure.json',{'result':'Failed','stage':'NativeIdlePreflight','error':str(e),'nativeMissionDispatched':False,'preManifest':pre});raise
 with (REPORT/'native1359-observer.cs.txt').open('xb') as f:f.write(FIXTURE.read_bytes())
 cli(['run_script','--file',str(FIXTURE),'--entry','S005NormalStopManual1359Observer.Run','--timeout_ms','30000'],REPORT/'load1359-stop-manual.json')
 dispatch=manifest('final-1359-normal-stop-pre-dispatch')
 if read(REPORT/a.pre_manifest)!=read(REPORT/dispatch):raise RuntimeError('Source changed before dispatch')
 idle('before-dispatch');owner_idle();s=read(STATE);s.update(activeRun='native1359',activeLog=LOG,activeScope=METHOD,activePreManifest=a.pre_manifest);STATE.write_text(json.dumps(s,indent=2)+'\n')
 new_json(REPORT/'native1359-runner-start.json',{'explicitHeavyLaneOwner':a.heavy_lane_owner,'sourceHash':prep['sourceHash'],'preManifest':a.pre_manifest,'preflightManifest':pre,'preDispatchManifest':dispatch,'method':METHOD,'fixtureSha256':sha(FIXTURE),'runnerSha256':sha(pathlib.Path(__file__)),'normalInput':True,'plannedStop':True,'unattendedAriaSeedWin':False,'gameplayStateInjected':False})
 wrapper=subprocess.run(['rtk','proxy','Tools/CI/invoke_unity_macos.sh','--project',str(ROOT),'--reuse','--timeout','1800','--log',str(REPORT/LOG),'--','-executeMethod',METHOD],cwd=ROOT).returncode
 with (REPORT/(LOG+'.wrapper-exit')).open('x') as f:f.write(str(wrapper)+'\n')
 receipt=REPORT/(LOG+'.exit');native=None
 if receipt.is_file() and re.fullmatch(r'-?\d+\s*',receipt.read_text()):native=int(receipt.read_text())
 if native is None:
  new_json(REPORT/'native1359-unclosed-ownership.json',{'result':'Unclosed','reason':'Missing/malformed native exit receipt','wrapperExit':wrapper,'nativeExit':native,'ownershipPreserved':True,'playerReady':False});return 4
 try:idle('after-close')
 except Exception as e:
  new_json(REPORT/'native1359-unclosed-ownership.json',{'result':'Unclosed','reason':str(e),'wrapperExit':wrapper,'nativeExit':native,'ownershipPreserved':True,'playerReady':False});return 4
 post=manifest('final-1359-normal-stop-post');same=read(REPORT/a.pre_manifest)==read(REPORT/pre)==read(REPORT/dispatch)==read(REPORT/post);errors=[]
 text=(REPORT/LOG).read_text(errors='replace') if (REPORT/LOG).is_file() else ''
 if wrapper!=0 or native!=0:errors.append('Actual wrapper/native failure '+str(wrapper)+'/'+str(native))
 if not same:errors.append('Full source/material manifest changed')
 for marker in ['[ExistingEditorValidation] result=Passed executeMethod='+METHOD,'[S005CandidateAvailability] fixture=EditorMemoryOnly publication=InProgress shippedCatalogModified=0','[S004MenuProfileFixture] chapter1Completed=5 progressionGate=Preserved isolatedProfile=1','[S004MenuJourney] result=Passed input=Touch controls=Skirmish,Scroll,S005,Launch seedFixture=104734 nativeSessionObserved=1','[S004AriaConsent] result=Passed input=Touch','[SkirmishS002AriaRun] watchStarted=1 seed=104734 locale=en','[S004ManualReceiptAudit] result=Passed measured=1','[S004ManualNative] result=Passed outcome=Victory','[SkirmishTerminalProbe] result=Passed reason=frozenResultAnd:MAIN MENU','[SkirmishValidationExit] result=Requested code=0']:
  if marker not in text:errors.append('Missing native marker '+marker)
 if re.search(r'error CS\d+|Compilation failed|observerFailure=1',text):errors.append('Native compiler/observer failure')
 binding=terminal=audit=controlProof=visualProof=fogProof=homeProof=handoffProof=None;trace=None
 try:
  obs=rows(REPORT/OBS);bindings=[r for r in obs if r.get('stage')=='ActualResolvedBinding']
  if len(bindings)!=1:raise ValueError('Exactly one native resolved binding required')
  binding=bindings[0]
  expected=dict(catalog='S005',definition='skirmish.s005',seed=104734,size='Standard',difficulty='Regular',army='CombinedArms',start='FieldBase',readiness='Field',readinessValue=1,deadline=1080,objective='BaseAssault',sharedFog=True,developmentFullVision=False,locale='en')
  if any(binding.get(k)!=v for k,v in expected.items()) or not binding.get('attempt'):raise ValueError('Actual public normal setup differs')
  if not any(r.get('stage')=='NormalDriverFlags' and r.get('normalDriverFlags') is True for r in obs):raise ValueError('Public launch flags not verified')
  trace=pathlib.Path(binding['tracePath'])
  if trace.parent!=REPORT or not re.fullmatch(r's005-aria-live-104734-en-[0-9a-f]{32}\.jsonl',trace.name):raise ValueError('Unique current native trace differs')
  actual=rows(trace)
  if not actual:raise ValueError('Native trace is empty; failure evidence retained')
  terminal=actual[-1]
  if terminal.get('phase')!='Finished' or terminal.get('outcome')!='Victory' or terminal.get('clockPaused')!=1 or terminal.get('simulationActive')!=0 or not 0<terminal.get('elapsed',0)<=1080:raise ValueError('Native terminal Victory missing')
  touch=rows(pathlib.Path(str(trace)+'.touch.jsonl'))
  if not touch or any(any(r.get(k)!=0 for k in ('unexpected','interventions','receiptViolations')) for r in touch):raise ValueError('Raw native touch stream violation')
  spec=importlib.util.spec_from_file_location('native_contacts',ROOT/'Tools/CI/audit_skirmish_native_contacts.py');contacts=importlib.util.module_from_spec(spec);spec.loader.exec_module(contacts);audit=contacts.audit(touch)
  if audit['errors'] or not all(audit[k] for k in ('strictRecordedContacts','nativeMenuContactsRecorded','terminalContactsRecorded')):raise ValueError('Native strict paired Menu/Runtime/Manual/Terminal contacts failed')
  commands=rows(pathlib.Path(str(trace)+'.commands.jsonl'))
  if not commands or any(c.get('verified') is not True or not c.get('receipt') for c in commands):raise ValueError('Actual accepted native receipt violation')
  q=load('qualifier');controlProof=q.qualify_controls(rows(REPORT/CONTROLS),commands,binding['attempt']);visualProof=q.qualify_visual(obs,binding['attempt'])
  fogProof=load('fog-qualifier').qualify(rows(REPORT/FOG),commands,binding['attempt'],terminal_proof={'renderedVictoryPassed':True,'sameAttemptReturnPassed':True,'strictContactAudit':True})
  homeRows=rows(REPORT/'native1359-manual-home-defense.jsonl');homeProof=load('home-defense-qualifier').qualify(homeRows,binding['attempt']);handoffProof=load('handoff_qualifier').qualify_handoff(rows(REPORT/CONTROLS),homeRows,binding['attempt'])
 except Exception as e:errors.append('Missing/malformed actual proof '+type(e).__name__+': '+str(e))
 # Always preserve a structured lossless failure envelope after actual closure, even no trace.
 proof={'result':'Passed' if not errors else 'Failed','scope':'Normal native public ARIAStart/Stop→manual MoveHoldPauseResumeMove→Victory/renderedMainMenu; no physical takeover or unattendedARIA baseline claim','sourceHash':prep['sourceHash'],'fullSourceUnchanged':same,'preManifest':a.pre_manifest,'preflightManifest':pre,'preDispatchManifest':dispatch,'postManifest':post,'wrapperExit':wrapper,'nativeExit':native,'log':LOG,'logSha256':sha(REPORT/LOG) if (REPORT/LOG).is_file() else None,'fixtureSha256':sha(FIXTURE),'runnerSha256':sha(pathlib.Path(__file__)),'actualResolvedSetup':binding,'trace':str(trace) if trace else None,'terminal':terminal,'inputAudit':audit,'controlProof':controlProof,'visualProof':visualProof,'fogProof':fogProof,'homeDefenseProof':homeProof,'handoffProof':handoffProof,'errors':errors,'countsForAriaSeedBaseline':False,'countsForPhysicalTakeover':False,'countsForNormalStopManualControls':not errors,'gameplayStateInjected':False,'playerReady':False,'humanDeviceAcceptance':False}
 new_json(REPORT/('native1359-normal-stop-manual-'+('failure' if errors else 'pass')+'.json'),proof)
 s=read(STATE)
 if s.get('activeRun')!='native1359':raise RuntimeError('Main owner changed; do not clear unrelated lane')
 s.update(activeRun=None,activeLog=None,activeScope=None,activePreManifest=None,latestNormalStopManual1359='native1359-normal-stop-manual-'+('failure' if errors else 'pass')+'.json');STATE.write_text(json.dumps(s,indent=2)+'\n')
 print(json.dumps({'result':proof['result'],'errors':errors,'wrapperExit':wrapper,'nativeExit':native,'playerReady':False}));return 1 if errors else 0
if __name__=='__main__':sys.exit(main())
