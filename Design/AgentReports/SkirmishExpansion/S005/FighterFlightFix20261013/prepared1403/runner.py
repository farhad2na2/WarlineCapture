"""1403 isolated actual paid AntiAir/Fighter effects versus natural paid Plane flight. Escalated root launch required."""
import argparse,hashlib,importlib.util,json,pathlib,re,subprocess,sys
ROOT=pathlib.Path('/Users/farhad/Projects/WarlineCapture');REPORT=ROOT/'Design/AgentReports/SkirmishExpansion/S005';P=pathlib.Path(__file__).parent
STATE=REPORT/'TECHNICAL_READINESS_CURRENT.json';PREP=P/'prepared.json';HELPER=pathlib.Path('/private/tmp/warline-s005-source-manifest.py');FIXTURE=P/'S005PaidEnemyAirEffects1403.cs';IDLE=P/'S005PaidAirIdle1403.cs'
LOG='native-1403-paid-anti-air-fighter-effects.log';METHOD='S005PaidEnemyAirEffects1403.RunValidation';TOUCH='native1403-bilateral-airport.touch.jsonl'
MARKERS=['[S005PaidEnemyAirEffects] result=Passed cases=2 antiAirKind=AirMissile fighterKind=DirectFire actualPaidEnemyPlane=1 naturalTargetTaxiTakeoff=1 targetPoseAirborneVisibilityInjected=0 nativeCombatDamage=1 normalMission=0 planeCargoCertified=0','[S005BilateralAirportPrerequisite] result=Passed owners=2 ','[S005BilateralAirportClosure] result=Passed paidAirportPrerequisiteOnly=0 aircraftCapacityCertified=0 airCombatCertified=1 normalMission=0','[S005BilateralAirportMovementCleanup] result=Passed attemptsRemoved=1','[S005BilateralAirportEnemyPreview] fullNativeValid=1 unrotated=1 playerConfirmNotIssued=1 cancelBeforeNativePlan=1','[S005BilateralAirportPlanSettlement] nextBuildIndex=1 nativeCompletionConsumed=1 temporaryControlRestored=1','[S005BilateralAirCapacityConfiguration] playerAirCap=2 enemyAirCap=2 playerAirLive=0 enemyAirLive=0 aircraftDelivered=0 capacityCertified=0','[ExistingEditorValidation] result=Passed executeMethod='+METHOD,'[SkirmishValidationExit] result=Requested code=0']

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
 if r.returncode or d.get('success') is not True or d.get('data',{}).get('success') is not True or d.get('data',{}).get('result',{}).get('success') is not True or d.get('data',{}).get('target',{}).get('projectPath')!=str(ROOT):raise RuntimeError('Native CLI exact target/outer/data/nested failure '+out.name)
 return d['data']['result'].get('result')
def idle(tag):
 v=cli(['run_script','--file',str(IDLE),'--entry','S005PaidAirIdle1403.Run','--timeout_ms','30000'],REPORT/('native1403-'+tag+'-idle.json'))
 if v!='False|False|False|False|False|'+str(ROOT/'Assets'):raise RuntimeError('Main native idle ownership differs '+repr(v))
def owner_idle():
 if read(STATE).get('activeRun') is not None:raise RuntimeError('Another main lane is owned')
def verify_dry(d):
 data=d.get('data',{});inner=data.get('result',{});params=data.get('parameters',{})
 if d.get('success') is not True or data.get('success') is not True or inner.get('success') is not True or any(str(v.get('severity','')).lower()=='error' for v in inner.get('diagnostics',[])) or params.get('file')!=str(FIXTURE) or params.get('entry')!='S005PaidEnemyAirEffects1403.Run' or params.get('dry_run') not in ('true',True) or data.get('target',{}).get('projectPath')!=str(ROOT):raise RuntimeError('Exact current fixture native dry success/parameters/target not proven')

def main():
 ap=argparse.ArgumentParser(description=__doc__);ap.add_argument('pre_manifest');ap.add_argument('--dry-compile-proof',required=True);ap.add_argument('--heavy-lane-owner',required=True,choices=['main1403']);a=ap.parse_args();prep=read(PREP)
 if prep.get('nativeDispatchPermitted') is not True or prep.get('sourceHashStatus')!='FinalReviewedMergedSource':raise RuntimeError('1403 draft source pins are provisional; root review and final repin required')
 owner_idle()
 dryPath=pathlib.Path(a.dry_compile_proof)
 if dryPath.parent!=REPORT or not dryPath.name.startswith('compile1403-'):raise RuntimeError('Exact archived compile1403 dry-proof path required')
 verify_dry(read(dryPath))
 if pathlib.Path(a.pre_manifest).name!=a.pre_manifest or not (REPORT/a.pre_manifest).is_file():raise RuntimeError('Existing source-manifest basename required')
 for dependency,h in prep['frozenInputDependencies'].items():
  if sha(pathlib.Path(dependency))!=h:raise RuntimeError('Immutable v2 source-neutral dependency changed '+dependency)
 for rel,h in prep['files'].items():
  if sha(P/rel)!=h:raise RuntimeError('Frozen external bytes changed '+rel)
 if sha(HELPER)!=prep['sourceManifestHelperSha256']:raise RuntimeError('Source helper bytes changed')
 for rel,h in prep['sourceContracts'].items():
  if sha(ROOT/rel)!=h:raise RuntimeError('Reviewed current source changed '+rel)
 if any(re.match(r'(?:native-?|load|final-|compile)1403(?:\D|$)',x.name) for x in REPORT.iterdir() if x!=dryPath):raise RuntimeError('Preserve existing1403 artifacts')
 pre=manifest('final-1403-paid-air-preflight')
 if read(REPORT/a.pre_manifest)!=read(REPORT/pre) or read(REPORT/pre.replace('-source-manifest.json','-independent-fingerprint.json'))['sourceHash']!=prep['sourceHash']:raise RuntimeError('Current full source differs; native not called')
 try:idle('before-load')
 except Exception as e:
  new_json(REPORT/'native1403-preflight-failure.json',{'result':'Failed','stage':'NativeIdlePreflight','error':str(e),'nativeMissionDispatched':False,'preManifest':pre});raise
 with (REPORT/'native1403-fixture.cs.txt').open('xb') as f:f.write(FIXTURE.read_bytes())
 cli(['run_script','--file',str(FIXTURE),'--entry','S005PaidEnemyAirEffects1403.Run','--timeout_ms','30000'],REPORT/'load1403-paid-air.json')
 dispatch=manifest('final-1403-paid-air-pre-dispatch')
 if read(REPORT/a.pre_manifest)!=read(REPORT/dispatch):raise RuntimeError('Source changed before dispatch')
 idle('before-dispatch');owner_idle();s=read(STATE);s.update(activeRun='native1403',activeLog=LOG,activeScope=METHOD,activePreManifest=a.pre_manifest);STATE.write_text(json.dumps(s,indent=2)+'\n')
 new_json(REPORT/'native1403-runner-start.json',{'explicitHeavyLaneOwner':a.heavy_lane_owner,'sourceHash':prep['sourceHash'],'preManifest':a.pre_manifest,'preflightManifest':pre,'preDispatchManifest':dispatch,'method':METHOD,'fixtureSha256':sha(FIXTURE),'runnerSha256':sha(pathlib.Path(__file__)),'dryCompileProof':dryPath.name,'dryCompileProofSha256':sha(dryPath),'normalMission':False,'fundingReadinessOpeningDeathIsolation':True,'paidNativeProduction':True,'targetPoseAirFlagsFogOrDamageInjected':False})
 wrapper=subprocess.run(['rtk','proxy','Tools/CI/invoke_unity_macos.sh','--project',str(ROOT),'--reuse','--timeout','1520','--log',str(REPORT/LOG),'--','-executeMethod',METHOD],cwd=ROOT).returncode
 with (REPORT/(LOG+'.wrapper-exit')).open('x') as f:f.write(str(wrapper)+'\n')
 receipt=REPORT/(LOG+'.exit');native=None
 if receipt.is_file() and re.fullmatch(r'-?\d+\s*',receipt.read_text()):native=int(receipt.read_text())
 if native is None:
  new_json(REPORT/'native1403-unclosed-ownership.json',{'result':'Unclosed','reason':'Missing/malformed native exit receipt','wrapperExit':wrapper,'nativeExit':native,'ownershipPreserved':True,'playerReady':False});return 4
 try:idle('after-close')
 except Exception as e:
  new_json(REPORT/'native1403-unclosed-ownership.json',{'result':'Unclosed','reason':str(e),'wrapperExit':wrapper,'nativeExit':native,'ownershipPreserved':True,'playerReady':False});return 4
 post=manifest('final-1403-paid-air-post');same=read(REPORT/a.pre_manifest)==read(REPORT/pre)==read(REPORT/dispatch)==read(REPORT/post);errors=[]
 text=(REPORT/LOG).read_text(errors='replace') if (REPORT/LOG).is_file() else ''
 if wrapper!=0 or native!=0:errors.append('Actual wrapper/native failure '+str(wrapper)+'/'+str(native))
 if not same:errors.append('Full source/material manifest changed')
 for marker in MARKERS:
  if marker not in text:errors.append('Missing native marker '+marker)
 if re.search(r'error CS\d+|Compilation failed|\[S005BilateralAirportClosure\] result=Failed',text):errors.append('Native compiler/fixture failure')
 touch=[];qualified=None;inputHealth=None
 try:
  inputHealth=load('input_health_qualifier').qualify(rows(REPORT/'native1403-first-touch-input-health.jsonl'))
  touch=rows(REPORT/TOUCH)
  if not touch or any(any(r.get(k)!=0 for k in ('unexpected','interventions','receiptViolations')) for r in touch):raise ValueError('Raw native touch stream violation')
  if touch[-1]['completed']!=touch[-1]['dispatched'] or touch[-1]['completed']<=0:raise ValueError('Native Touch final matched completion required')
  match=re.search(r'\[S005BilateralAirportMovementInput\] dispatched=(\d+) completed=(\d+) acceptedCommands=(\d+) violations=(\d+)',text)
  if not match or int(match[1])<1 or match[1]!=match[2] or int(match[4])!=0:raise ValueError('Native completion receipt audit failed')
  # Exact contacts, not just matching counters, even across per-case command-audit resets.
  grouped={}
  for r in touch:
   key=(r['device'],r['generation']);grouped.setdefault(key,[]).append(r)
  for k,rr in grouped.items():
   phases=[r['kind'] for r in rr if r['kind'] in ('Dispatched','Began','Ended','Canceled','PhysicalTakeover')]
   if len(phases)%3 or any(phases[i:i+3]!=['Dispatched','Began','Ended'] for i in range(0,len(phases),3)):raise ValueError('Native paired contact sequence failed '+str(k))
  inputs={name:rows(REPORT/file) for name,file in {'placement':'native1403-bilateral-airport.placement.jsonl','effects':'native1403-enemy-air-effects.jsonl','raw':'native1403-enemy-air-all-damage.jsonl','states':'native1403-enemy-air-state.jsonl','frames':'native1403-enemy-air-frame-census.jsonl','census':'native1403-source-census.jsonl','preparation':'native1403-fighter-public-preparation.jsonl','readiness':'native1403-fighter-native-readiness.jsonl','intercept':'native1403-fighter-intercept.jsonl','preframe':'native1403-fighter-route-preframe.jsonl'}.items()}
  inputs['touch']=touch
  qualified=load('qualifier').qualify(text,inputs)
  qualified['nativeRunwayBindings']=load('runway_qualifier').qualify(rows(REPORT/'native1403-runway-binding.jsonl'),text)
  qualified['mapFighterNativeReselection']=load('fighter_reselection_qualifier').qualify(inputs['preparation'])
  qualified['ownedPlacementFocusScopes']=load('focus_scope_qualifier').qualify(text)
 except Exception as e:errors.append('Missing/malformed native proof '+type(e).__name__+': '+str(e))
 guardPath=REPORT/'native1403-fighter-preparation-operands.jsonl';guardProof=None
 if guardPath.is_file():
  try:guardProof=load('fighter_guard_qualifier').qualify(rows(guardPath),require_pass=not errors,preparation=inputs['preparation'] if qualified is not None else None)
  except Exception as ex:errors.append('Actual preparation operand schema: '+str(ex))
 elif not errors:errors.append('Actual preparation operand census missing')
 cameraPath=REPORT/'native1403-fighter-camera-framing.jsonl';cameraProof=None
 if cameraPath.is_file() and not errors:
  try:
   source=next(json.loads(line[len('[S005EnemyAirPaidQueue] '):]) for line in text.splitlines() if line.startswith('[S005EnemyAirPaidQueue] ') and json.loads(line[len('[S005EnemyAirPaidQueue] '):])['role']=='Fighter')
   cameraProof=load('fighter_camera_qualifier').qualify(rows(cameraPath),source)
  except Exception as ex:errors.append('Actual public camera framing: '+str(ex))
 elif not errors:errors.append('Actual public camera framing missing')
 geometryProof=None;geometryDirectory=REPORT/'native1403-geometry'
 try:
  geometrySource=next(json.loads(line[len('[S005EnemyAirPaidQueue] '):]) for line in text.splitlines() if line.startswith('[S005EnemyAirPaidQueue] ') and json.loads(line[len('[S005EnemyAirPaidQueue] '):])['role']=='Fighter')
  actualOwners=re.search(r'\[S005BilateralAirportPrerequisite\] result=Passed owners=2 playerRuntimeId=(\d+) enemyRuntimeId=(\d+)',text)
  if actualOwners is None:raise ValueError('Actual paid Airport prerequisite identity absent')
  geometryProof=load('geometry_qualifier').qualify(rows(geometryDirectory/'native1366-geometry.jsonl'),read(geometryDirectory/'native1366-military-clearance.json'),[json.loads(line[len('[S005BilateralAirportReceipt] '):]) for line in text.splitlines() if line.startswith('[S005BilateralAirportReceipt] ')],geometrySource['attemptId'],int(actualOwners[1]),1)
 except Exception as ex:errors.append('Separate native geometry census/paid join: '+str(ex))
 proof={'result':'PassedScopedIsolatedActualPaidAntiAirFighterAirborneEffects' if not errors else 'Failed','sourceHash':prep['sourceHash'],'fullSourceUnchanged':same,'preManifest':a.pre_manifest,'preflightManifest':pre,'preDispatchManifest':dispatch,'postManifest':post,'wrapperExit':wrapper,'nativeExit':native,'log':LOG,'logSha256':sha(REPORT/LOG) if (REPORT/LOG).is_file() else None,'fixtureSha256':sha(FIXTURE),'runnerSha256':sha(pathlib.Path(__file__)),'dryCompileProof':dryPath.name,'dryCompileProofSha256':sha(dryPath),'inputRows':touch,'firstInputHealth':inputHealth,'nativeAttribution':qualified,'errors':errors,'geometryClearanceSupplement':geometryProof,'geometryReadOnlyObserver':True,'civilianVisualReviewCertified':False,'normalMission':False,'openingActorHealthZeroAndFinalDeathStimulus':True,'bothFactionsFundingAndReadinessInjected':True,'temporaryEnemyStrategyAndNativePlannerControl':True,'temporaryUnrelatedShooterSuppressionDisclosed':True,'geometryPricesCapsUnchanged':True,'enemyTargetMovePublicNativeAPIOutsideTouchAudit':True,'targetPoseAirFlagsFogHPDamageInjected':False,'fighterInitialAutoEngageZeroStimulusDisclosed':True,'fighterNativeRoleAutoEngageRefreshPreserved':True,'publicNativeCameraApiNotNormalTouchAcceptance':True,'fighterCameraFraming':cameraProof,'fighterCameraFramingSha256':sha(cameraPath) if cameraPath.is_file() else None,'paidEnemyNaturalTaxiTakeoff':not errors,'nativeRenderedLaunchFogAndImpactAirPaidAttribution':not errors,'antiAirEligibilityTiming':'ActualVisibleEligibleLockedNativeProjectileLaunchThenExactDeferredImpact','fighterEligibilityTiming':'ActualVisibleCurrentAndPriorImmediateHit','airCombatCertified':not errors,'fighterPreparationOperands':guardProof,'fighterPreparationOperandSha256':sha(guardPath) if guardPath.is_file() else None,'planeCargoCertified':False,'currentNativeEnemyEconomyOrchestrationCertified':False,'playerReady':False}
 new_json(REPORT/('native1403-paid-air-effects-'+('failure' if errors else 'pass')+'.json'),proof)
 s=read(STATE)
 if s.get('activeRun')!='native1403':raise RuntimeError('Main owner changed; do not clear unrelated lane')
 s.update(activeRun=None,activeLog=None,activeScope=None,activePreManifest=None,latestPaidAntiAirFighter1403='native1403-paid-air-effects-'+('failure' if errors else 'pass')+'.json');STATE.write_text(json.dumps(s,indent=2)+'\n')
 print(json.dumps({'result':proof['result'],'errors':errors,'wrapperExit':wrapper,'nativeExit':native,'playerReady':False}));return 1 if errors else 0
if __name__=='__main__':sys.exit(main())
