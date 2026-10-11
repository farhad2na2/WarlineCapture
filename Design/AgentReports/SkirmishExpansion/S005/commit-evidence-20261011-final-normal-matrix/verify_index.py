from pathlib import Path
import csv,gzip,hashlib,json,re
PREFIX='Design/AgentReports/SkirmishExpansion/S005/'
SOURCE='d5afc88f225920451037f6e1c9f7e9d83dfc567e3bbdadfb5b6d65d9ee6774f4'
def sha(value):return hashlib.sha256(value).hexdigest()
def verify(index,data):
 assert index['readinessClaim'] is False and index['normalWins']==6 and index['sourceHash']==SOURCE
 assert len(index['dependencies'])==10 and len(index['runs'])==6
 assert len(index['files'])==index['statistics']['selectedExactAliases'] and len(index['objects'])==index['statistics']['uniqueObjects']
 assert len({f['originalPath'] for f in index['files']})==len(index['files'])
 for scan in index['credentialScans']:assert not any(scan['markerCounts'].values())
 agg=json.loads(data(PREFIX+'native-final-d5af-normal-aria-matrix.json'))
 assert agg['sourceHash']==SOURCE and agg['fullMatrixCertified'] is True and agg['currentPassedCases']==6 and agg['remainingCases']==0
 assert agg['playerReady'] is False and agg['technicallyReady'] is False and agg['realHumanDeviceAcceptance'] is False and agg['countsForAlternatingStartFamilyStress'] is False
 assert sha(data(PREFIX+agg['sourceManifest']))==agg['sourceManifestSha256']
 assert set(agg['individualProofSha256'])=={r['proofName'] for r in index['runs']}
 prepared_index=json.loads(data(PREFIX+'delegation-final-d5af-normal-matrix-prepared-index.json'))
 assert prepared_index['sourceHash']==SOURCE and prepared_index['playerReady'] is False
 # Prepared index remains historical preparation (its Pending text is not rewritten as executed proof).
 assert len(prepared_index['cases'])==6
 contacts_namespace={'__name__':'archived_native_contact_audit'}
 exec(data('Tools/CI/audit_skirmish_native_contacts.py').decode('utf-8'),contacts_namespace)
 pairs=set();attempts=set();traces=set();normal_contacts=0
 for run in index['runs']:
  n=run['id'];proof=json.loads(data(PREFIX+run['proofName']));assert proof==run['proof']
  assert sha(data(PREFIX+run['proofName']))==agg['individualProofSha256'][run['proofName']]
  assert proof['result']=='Passed' and not proof['errors'] and proof['sourceHash']==SOURCE and proof['fullSourceUnchanged'] is True
  assert proof['actualWrapperExit']==proof['nativeBridgeExit']==0 and proof['normalInput'] is True and proof['normalSpeedCertified'] is True
  assert proof['countedWinCertified'] is True and proof['countsForAriaSeedBaseline'] is True
  assert proof['fundingReadinessActorPoseDamageVisibilityOutcomeInjected'] is False and proof['playerReady'] is False and proof['realHumanDeviceAcceptance'] is False
  resolved=proof['actualResolvedSetup'];seed=resolved['seed'];locale=resolved['locale'];pair=(seed,locale)
  assert pair not in pairs;pairs.add(pair)
  for k,v in {'catalog':'S005','definition':'skirmish.s005','size':'Standard','difficulty':'Regular','army':'CombinedArms','start':'FieldBase','readiness':'Field','readinessValue':1,'deadline':1080,'objective':'BaseAssault','sharedFog':True,'developmentFullVision':False}.items():assert resolved[k]==v,(n,k)
  assert resolved['attempt'] and resolved['attempt'] not in attempts;attempts.add(resolved['attempt'])
  assert proof['trace'] not in traces;traces.add(proof['trace'])
  expected_locale='fa' if locale=='fa-IR' else 'en';assert re.fullmatch(fr's005-aria-live-{seed}-{expected_locale}-[0-9a-f]{{32}}\.jsonl',proof['trace'])
  aliases=[proof[k] for k in ('preManifest','preflightManifest','preDispatchManifest','postManifest')]
  assert len({data(PREFIX+a) for a in aliases})==1
  assert data(PREFIX+aliases[0])==data(PREFIX+agg['sourceManifest'])
  for a in aliases:assert json.loads(data(PREFIX+a.replace('-source-manifest.json','-independent-fingerprint.json')))['sourceHash']==SOURCE
  log=data(PREFIX+proof['log']);assert sha(log)==proof['logSha256']
  assert int(data(PREFIX+proof['log']+'.exit'))==int(data(PREFIX+proof['log']+'.wrapper-exit'))==0
  for marker in [b'[SkirmishTerminalProbe] frozen=1 returnInput=Touch target=ADJUST SETUP',b'[SkirmishTerminalProbe] result=Passed reason=frozenResultAnd:ADJUST SETUP',b'[SkirmishS002AriaRun] appended=1 countedWin=1 error=',b'[SkirmishValidationExit] result=Requested code=0',f'[ExistingEditorValidation] result=Passed executeMethod=S005NormalAria{n}Observer.RunValidation'.encode()]:assert marker in log,(n,marker)
  assert re.search(rb'\[SkirmishNormalSpeed\] latch=True .* counted=True',log)
  assert not re.search(rb'error CS\d+|Compilation failed',log)
  row=proof['nativeCountedRow'];assert row[3]==SOURCE and row[5:12]==['Standard','Regular',str(seed),locale,'Editor','aria','1'] and row[13]=='Victory' and row[16:18]==['0','0']
  counted=[]
  for line in log.decode('utf-8').splitlines():
   values=next(csv.reader([line]))
   if len(values)==20 and values[1]=='S005' and values[7]==str(seed) and values[8]==locale:counted.append(values)
  assert counted==[row]
  assert data(str(Path(row[-2])))==data(PREFIX+proof['trace'])
  assert b'[SkirmishS002AriaRun] appended=1 countedWin=1 error=' in data(str(Path(row[-1])))
  assert sha(data(PREFIX+proof['fixture']))==proof['fixtureSha256'] and sha(data(run['fixturePath']))==proof['fixtureSha256']
  assert sha(data(run['runnerPath']))==proof['executedRunnerSha256']
  prep=json.loads(data(PREFIX+f'delegation-normal-aria{n}-prepared.json'))
  assert prep['fixtureSha256']==proof['fixtureSha256'] and prep['runnerSha256']==proof['executedRunnerSha256']
  assert sha(data(prep['sourceManifestHelper']))==prep['sourceManifestHelperSha256']
  for suffix,entry,dry in [(f'compile{n}-final-d5af-normal-dry-run.json',f'S005NormalAria{n}Observer.Run',True),(f'load{n}-normal-aria-observer.json',f'S005NormalAria{n}Observer.Run',False)]:
   response=json.loads(data(PREFIX+suffix));assert response['success'] and response['data']['result']['success']
   assert not any(str(d['severity']).lower()=='error' for d in response['data']['result'].get('diagnostics',[]))
   assert response['data']['parameters']['file']==run['fixturePath'] and response['data']['parameters']['entry']==entry
   assert str(response['data']['parameters'].get('dry_run','false')).lower()==str(dry).lower()
   assert response['data']['target']['projectPath']=='/Users/farhad/Projects/WarlineCapture'
  dispatch=json.loads(data(PREFIX+proof['log']+'.dispatch.json'));assert dispatch['success'] and dispatch['data']['result']['success'] and dispatch['data']['result']['result']=='scheduled'
  start=json.loads(data(PREFIX+f'native{n}-runner-start.json'));assert start['explicitHeavyLaneOwner']==f'main{n}' and start['normalInput'] and start['matchResourcesPosesDamageVisibilityClocksOutcomeWrittenByAdapter'] is False
  assert start['fixtureSha256']==proof['fixtureSha256'] and start['executedRunnerSha256']==proof['executedRunnerSha256']
  for when in ('before-load','before-dispatch','after-close'):
   idle=json.loads(data(PREFIX+f'native{n}-{when}-native-idle.json'));assert idle['success'] and idle['data']['result']['success'];s=idle['data']['result']['result'];assert all(s[k] is False for k in ('playing','changing','compiling','updating')) and s['project']=='/Users/farhad/Projects/WarlineCapture/Assets'
  terminal=proof['terminal'];assert terminal['phase']=='Finished' and terminal['outcome']=='Victory' and terminal['clockPaused']==1 and terminal['simulationActive']==0
  assert 0<terminal['elapsed']<=1080 and terminal['elapsed']==terminal['clockElapsed'] and terminal['enemyBaseHp']==0 and terminal['playerBaseHp']>0
  trace=[json.loads(x) for x in data(PREFIX+proof['trace']).splitlines() if x.strip()];assert trace and trace[-1]==terminal
  finished=[x for x in trace if x.get('phase')=='Finished'];assert len(finished)>=2
  assert all(x['elapsed']==x['clockElapsed']==terminal['elapsed'] and x['outcome']=='Victory' and x['clockPaused']==1 and x['simulationActive']==0 and x['enemyBaseHp']==0 for x in finished)
  touch=[json.loads(x) for x in data(PREFIX+proof['trace']+'.touch.jsonl').splitlines() if x.strip()]
  actual_audit=contacts_namespace['audit'](touch);assert json.loads(json.dumps(actual_audit))==proof['inputAudit']
  assert actual_audit['strictRecordedContacts'] and actual_audit['nativeMenuContactsRecorded'] and actual_audit['terminalContactsRecorded'] and not actual_audit['errors']
  assert all(x['unexpected']==x['interventions']==x['receiptViolations']==0 for x in touch);normal_contacts+=actual_audit['completedContacts']
  commands=[json.loads(x) for x in data(PREFIX+proof['trace']+'.commands.jsonl').splitlines() if x.strip()];assert commands and all(x['verified'] is True and x['receipt'] for x in commands)
  observation_bytes=data(PREFIX+proof['observation']);assert sha(observation_bytes)==proof['observationSha256'];observations=[json.loads(x) for x in observation_bytes.splitlines() if x.strip()]
  assert not any(x['stage']=='ObserverFailure' for x in observations)
  assert any(x['stage']=='NormalDriverFlags' and x['normalDriverFlags'] is True for x in observations)
  assert any(x['stage']=='RenderedViewportContext' and x['nativeViewportMatched'] is True and x['samplingPhase']=='CanvasWillRenderCanvases' for x in observations)
  roles=['SetupTitle','SetupObjective','HudPurposeTitle','HudPurposeBody','ObjectiveStrip','AriaControl','ResultText','AdjustSetupTitle','AdjustSetupObjective']
  if locale=='fa-IR':roles+=['PlayerBaseCaption','EnemyBaseCaption','CurrentMatchClock']
  for role in roles:assert any(x['stage']=='NativeBoundText' and x['role']==role and x['exactBoundReference'] and x['samplingPhase']=='CanvasWillRenderCanvases' and x['activeText'] and not x['culledText'] and x['textAlpha']>.01 and x['ancestorAlpha']>.01 and x['visibleGlyphs']>0 and x['safeAreaOverlap'] and x['renderedBody'] for x in observations),(n,role)
  strips=[x for x in observations if x['stage']=='NativeObjectiveStrip' and x['ariaActive'] and not x['finished']]
  assert any(all(x[k] for k in ('renderedPlayerBase','renderedEnemyBase','renderedClock')) for x in strips)
  if locale=='fa-IR':assert any(all(x.get(k) is True for k in ('renderedPlayerBase','renderedEnemyBase','renderedClock','exactPlayerBinding','exactEnemyBinding','exactClockBinding','clockNumberPresentAndCurrent')) and x.get('expectedPlayerCaption')=='پایگاه شما' and x.get('expectedEnemyCaption')=='پایگاه دشمن' and x.get('nativeClockNumber')==x.get('modelClockNumber') and x.get('modelClockNumber') for x in strips)
  victory=[(i,x) for i,x in enumerate(observations) if x['stage']=='NativeRenderedResultText' and x['finished'] and x['frozenVictory'] and x['renderedTitle'] and x['renderedVictoryWitnessed'] and x['outcome']=='Victory' and x['attempt']==resolved['attempt'] and (x.get('localizedVictoryCopy') is True if locale=='fa-IR' else x['title'].upper()=='VICTORY')]
  returns=[(i,x) for i,x in enumerate(observations) if x['stage']=='NativeAdjustSetupDestination' and x['seed']==seed and x['renderedTitle'] and x['renderedObjective']]
  assert victory and returns and all(i>victory[0][0] and x['attempt']==resolved['attempt'] and x['finished'] and x['frozenVictory'] and x['renderedVictoryWitnessed'] and x['outcome']=='Victory' and x['frame']>=victory[0][1]['frame'] for i,x in returns)
  closure=[x for x in observations if x['stage']=='ClosedObserver'];assert len(closure)==1 and all(closure[0][k] for k in ('setupSeen','bindingSeen','purposeSeen','activeControlSeen','resultSeen','returnSeen'))
  for suffix in ('.setup.png','.opening.png','.menu-selection-diagnostic.png','.terminal-result.png','.terminal-destination.png'):assert data(PREFIX+proof['trace']+suffix).startswith(b'\x89PNG\r\n\x1a\n')
 assert pairs=={(s,l) for s in (104734,130368,155926) for l in ('en','fa-IR')}
 assert normal_contacts==index['statistics']['actualCompletedNativeContacts']
 core=json.loads(data(PREFIX+'native1175-focused-current-proof.json'));assert core['sourceHash']==SOURCE and core['result']=='Passed' and core['sourceUnchanged'] and core['actualWrapperExit']==core['nativeBridgeExit']==0
 assert core['actualSuites']==16 and core['actualPlannerCases']==111 and core['actualMoveCommandTests']==26 and core['nativeMatchCount']==0 and core['playerReady'] is False
 return {'runs':6,'completedNativeContacts':normal_contacts,'sourceHash':SOURCE,'uniqueSeedLocalePairs':6,'normalWins':6,'playerReady':False}

archive=Path(__file__).resolve().parent
repository=archive.parents[4]
index=json.loads((archive/'index.json').read_text())
checked_history_objects=set()
for dependency in index['dependencies']:
    path=repository/dependency['index'];assert sha(path.read_bytes())==dependency['indexSha256']
    historical=json.loads(path.read_text())
    for obj in historical['objects']:
        q=(path.parent/obj['object']).resolve()
        if q in checked_history_objects:continue
        checked_history_objects.add(q);compressed=q.read_bytes()
        assert sha(compressed)==obj['gzipSha256'] and len(compressed)==obj['gzipBytes']
        original=gzip.decompress(compressed);assert sha(original)==obj['sha256'] and len(original)==obj['bytes']
objects={}
for obj in index['objects']:
    compressed=(archive/obj['object']).read_bytes()
    assert sha(compressed)==obj['gzipSha256'] and len(compressed)==obj['gzipBytes']
    original=gzip.decompress(compressed);assert sha(original)==obj['sha256'] and len(original)==obj['bytes']
    assert obj['sha256'] not in objects;objects[obj['sha256']]=original
aliases={}
for file in index['files']:
    assert file['originalPath'] not in aliases
    value=objects[file['sha256']];assert len(value)==file['bytes']
    aliases[file['originalPath']]=value
result=verify(index,lambda name:aliases[name])
print(json.dumps(dict(result='PassedOfflineExactLosslessMatrix',historyIndexes=len(index['dependencies']),historyObjects=len(checked_history_objects),aliases=len(aliases),objects=len(objects),**result)))
