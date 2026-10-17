"""Offline-only successor. No Unity, gameplay, original evidence or readiness writes."""
import argparse,csv,hashlib,importlib.util,json,pathlib,re,subprocess,sys

ROOT=pathlib.Path('/Users/farhad/Projects/WarlineCapture')
REPORT=ROOT/'Design/AgentReports/SkirmishExpansion/S005'
BUNDLE=REPORT
sha=lambda value:hashlib.sha256(value).hexdigest()
read=lambda path:json.loads(path.read_text())
original=REPORT/'native1262-normal-manual-r2-recovery-failure.json'
failure=read(original)
assert failure['errors']==['Missing/malformed actual normal manual evidence: Actual runtime paid receipt facts changed']
assert failure['actualWrapperExit']==failure['nativeBridgeExit']==0
assert failure['fullSourceUnchanged'] and not failure['playerReady'] and not failure['realHumanDeviceAcceptance']
archivedRunner=REPORT/'native1262-executed-runner.py.txt'
assert sha(archivedRunner.read_bytes())==failure['executedRunnerSha256']=='2c203a14361e78975cdf0e57bd5415aa5b1f6511068250dde18313e7b4732f63'
for path,key in ((failure['log'],'logSha256'),(failure['fixture'],'fixtureSha256'),(failure['observation'],'observationSha256')):
    assert sha((REPORT/path).read_bytes())==failure[key]
manifestNames=[failure[k] for k in ('preManifest','preflightManifest','preDispatchManifest','postManifest')]
manifests=[read(REPORT/name) for name in manifestNames]
assert all(m==manifests[0] for m in manifests)
fp=read(REPORT/failure['postManifest'].replace('-source-manifest.json','-independent-fingerprint.json'))
h=fp['sourceHash'];assert h==failure['sourceHash']=='60de9fddb00f2c22e416b14082f60ad352e173f867515cfe18942f270b4046f9'
prep=read(REPORT/'native1262-offline-original-prepared.json')
assert prep['currentCandidateSourceHash']==h
assert len(prep['sourceContracts'])==failure['reviewedContractCount']==123
assert all((manifests[0]['files'][name] if name in manifests[0]['files'] else sha((ROOT/name).read_bytes()))==expected for name,expected in prep['sourceContracts'].items())
for tag in ('before-load','before-dispatch','after-close'):
    idle=read(REPORT/('native1262-'+tag+'-native-idle.json'))
    assert idle['success'] is True and idle['data']['success'] is True and idle['data']['result']['success'] is True
    assert idle['data']['target']['projectPath']==str(ROOT) and idle['data']['target']['port']==7801
    assert idle['data']['result']['result']=='False|False|False|False|False|'+str(ROOT/'Assets')
dry=read(REPORT/'compile1262-normal-manual-observer-dry-run2.json')
assert dry['success'] and dry['data']['success'] and dry['data']['result']['success']
assert not any(d.get('severity','').lower()=='error' for d in dry['data']['result']['diagnostics'])
assert dry['data']['parameters']['entry']=='S005NormalManualR2Recovery1262Observer.Run'
assert dry['data']['parameters']['dry_run']=='true' and dry['data']['target']['projectPath']==str(ROOT)
LOG=failure['log'];OBS=failure['observation'];ARCHIVE=failure['fixture']
METHOD='S005NormalManualR2Recovery1262Observer.RunValidation'
QUALIFIER=REPORT/'native1262-offline-original-fog-qualifier.py'
PAID_QUALIFIER=BUNDLE/'native1262-offline-paid-remap-qualifier.py'
FALLBACK='native1262-manual-fog-fallback.jsonl'
assert sha(QUALIFIER.read_bytes())==prep['fallbackQualifierSha256']
wrapper=int((REPORT/(LOG+'.wrapper-exit')).read_text())
native=int((REPORT/(LOG+'.exit')).read_text())
assert wrapper==native==0
same=True
# Execute every immutable original post-native gate, including those skipped when
# the paid qualifier raised. The only substituted module is the reviewed runtime
# producer remap oracle; there are no CLI, source-manifest or readiness mutations.
runnerText=archivedRunner.read_text()
start="text=(REPORT/LOG).read_text(errors='replace');errors=[]"
stop="proof={'result':'Passed' if not errors else 'Failed'"
assert runnerText.count(start)==runnerText.count(stop)==1
tail=runnerText[runnerText.index(start):runnerText.index(stop)]
assert 'subprocess.' not in tail and 'STATE.write' not in tail and 'cli_json(' not in tail
exec(compile(tail,str(archivedRunner)+':immutable-offline-gates','exec'),globals())
assert not errors,errors
proof={
 'result':'PassedScopedOfflineAuditedNative1262',
 'scope':failure['scope'],
 'qualification':'All original immutable post-native gates replayed offline; corrected producer remap oracle only.',
 'originalFailure':original.name,'originalFailureSha256':sha(original.read_bytes()),
 'originalExecutedRunnerSha256':sha(archivedRunner.read_bytes()),
 'offlineScriptSha256':sha(pathlib.Path(__file__).read_bytes()),
 'correctedPaidQualifierSha256':sha(PAID_QUALIFIER.read_bytes()),
 'executedOriginalGateTailSha256':sha(tail.encode()),
 'sourceHash':h,'fullSourceUnchanged':True,'sourceManifestNames':manifestNames,
 'sourceManifestSha256':{name:sha((REPORT/name).read_bytes()) for name in manifestNames},
 'actualWrapperExit':wrapper,'nativeBridgeExit':native,
 'log':LOG,'logSha256':sha((REPORT/LOG).read_bytes()),
 'observation':OBS,'observationSha256':sha((REPORT/OBS).read_bytes()),
 'actualResolvedSetup':binding,'trace':trace.name,'terminal':terminal,'inputAudit':audit,
 'nativePaidHeavyAPC':paid,'nativeRecoveredIdentity':recovered,
 'nativeResearchObservationCount':len(research),'nativeFogApproach':fogproof,
 'paidRecoveryLifecycle':paidproof,'countsForLivePaidActorRecovery':paidproof['countsForLivePaidActorRecovery'],
 'producerRemap':{'savedStableId':'building.ground_staging.1','savedRuntimeId':3,'recoveredReceiptRuntimeId':paidproof['firstRestoredReceipt']['producer'],'nativeRecoveryCompleted':True,'directPostRecoveryBuildingStableIdentityCensus':False,'basis':'Authenticated saved unique living producer, verified source-pinned reconstruction/preparation/remap/commit, and constant positive receipt producer after completed recovery.'},
 'rendererObservedNormalSpeed':True,'countsForCurrentManualAcceptance':True,
 'countsForAriaSeedBaseline':False,'normalInput':True,
 'fundingReadinessActorPoseDamageVisibilityOutcomeInjected':False,
 'errors':[], 'playerReady':False,'realHumanDeviceAcceptance':False,
 'preSaveBattleObservedLimitation':failure['preSaveBattleObservedLimitation'],
 'limitations':['Post-recovery producer stable building ID was not directly censused; attributed through verified native source path.','Does not establish overall player readiness, human/device acceptance, all-Touch ARIA, Stop/takeover or discrete Move/Hold.'],
 'newUnityCalls':0,'originalEvidenceModified':False,
}
evidenceNames=[LOG,LOG+'.exit',LOG+'.wrapper-exit',LOG+'.dispatch.json',OBS,ARCHIVE,archivedRunner.name,original.name,'native1262-normal-manual-r2-native-checkpoint.json',FALLBACK,trace.name,trace.name+'.touch.jsonl',trace.name+'.commands.jsonl']
proof['immutableEvidenceSha256']={name:sha((REPORT/name).read_bytes()) for name in evidenceNames}
parser=argparse.ArgumentParser(description=__doc__);parser.add_argument('--output',type=pathlib.Path,required=True);output=parser.parse_args().output
with output.open('x') as stream:stream.write(json.dumps(proof,indent=2)+'\n')
print(json.dumps({'result':proof['result'],'proof':str(output),'proofSha256':sha(output.read_bytes()),'source':h,'paidActorRecovery':proof['countsForLivePaidActorRecovery'],'errors':errors,'playerReady':False}))
