
from pathlib import Path
import gzip, hashlib, json
archive = Path(__file__).resolve().parent
repository = archive.parents[4]
index = json.loads((archive / 'index.json').read_text())
def sha(value): return hashlib.sha256(value).hexdigest()
for dependency in index['dependencies']:
    assert sha((repository / dependency['index']).read_bytes()) == dependency['indexSha256']
objects = {}
for obj in index['objects']:
    compressed = (archive / obj['object']).read_bytes()
    assert sha(compressed) == obj['gzipSha256'] and len(compressed) == obj['gzipBytes']
    original = gzip.decompress(compressed)
    assert sha(original) == obj['sha256'] and len(original) == obj['bytes']
    assert obj['sha256'] not in objects
    objects[obj['sha256']] = original
alias_bytes = {}
for file in index['files']:
    assert file['originalPath'] not in alias_bytes
    original = objects[file['sha256']]
    assert sha(original) == file['sha256'] and len(original) == file['bytes']
    alias_bytes[file['originalPath']] = original
def data(name): return alias_bytes[name]


PREFIX = 'Design/AgentReports/SkirmishExpansion/S005/'
SOURCE = 'f788e01e25897c16ae470e4e4d9d687a574c35eca934aea9aa25ed9f72254893'
assert index['readinessClaim'] is False and index['normalWins'] == 1
assert len(index['dependencies']) == 9
assert len(index['files']) == index['statistics']['selectedExactAliases'] == 62
assert len(index['objects']) == index['statistics']['uniqueObjects'] == 52
for scan in index['credentialScans']:
    assert not any(scan['markerCounts'].values())
    assert sha(data(PREFIX + scan['log'])) == scan['sha256']
proof = json.loads(data(PREFIX + 'native1167-normal-aria-pass.json'))
assert proof == index['run']['proof']
assert proof['result'] == 'Passed' and not proof['errors']
assert proof['sourceHash'] == SOURCE and proof['fullSourceUnchanged'] is True
assert proof['normalInput'] is True and proof['normalSpeedCertified'] is True
assert proof['fundingReadinessActorPoseDamageVisibilityOutcomeInjected'] is False
assert proof['countedWinCertified'] is True and proof['countsForAriaSeedBaseline'] is True
assert proof['playerReady'] is False and proof['realHumanDeviceAcceptance'] is False
source_aliases = index['run']['fullSourceAliasNames']
assert len(source_aliases) == 4
assert len({data(PREFIX + name) for name in source_aliases}) == 1
for alias in source_aliases:
    assert json.loads(data(PREFIX + alias.replace('-source-manifest.json','-independent-fingerprint.json')))['sourceHash'] == SOURCE
log = data(PREFIX + proof['log'])
assert sha(log) == proof['logSha256']
assert int(data(PREFIX + proof['log'] + '.exit')) == int(data(PREFIX + proof['log'] + '.wrapper-exit')) == 0
assert sha(data(PREFIX + proof['fixture'])) == proof['fixtureSha256']
assert sha(data('/private/tmp/s005-run-normal-aria-1167.py')) == proof['executedRunnerSha256']
for marker in [b'[SkirmishTerminalProbe] frozen=1 returnInput=Touch target=ADJUST SETUP',b'[SkirmishTerminalProbe] result=Passed reason=frozenResultAnd:ADJUST SETUP',b'[SkirmishS002AriaRun] appended=1 countedWin=1 error=',b'[SkirmishValidationExit] result=Requested code=0',b'[ExistingEditorValidation] result=Passed executeMethod=S005NormalAria1167Observer.RunValidation']:
    assert marker in log
row = proof['nativeCountedRow']
assert row[3] == SOURCE and row[5:9] == ['Standard','Regular','104734','fa-IR']
assert row[13:16] == ['Victory','MainBaseDestroyed','232.944']
assert row[16:18] == ['0','0']
assert data(str(Path(row[-2]))) == data(PREFIX + proof['trace'])
assert data(str(Path(row[-1]))) != log
resolved = proof['actualResolvedSetup']
assert resolved['catalog'] == 'S005' and resolved['definition'] == 'skirmish.s005'
assert resolved['size'] == 'Standard' and resolved['difficulty'] == 'Regular'
assert resolved['army'] == 'CombinedArms' and resolved['start'] == 'FieldBase'
assert resolved['locale'] == 'fa-IR' and resolved['seed'] == 104734
assert resolved['sharedFog'] is True and resolved['developmentFullVision'] is False
terminal = proof['terminal']
assert terminal['phase'] == 'Finished' and terminal['outcome'] == 'Victory'
assert terminal['elapsed'] == terminal['clockElapsed'] == 232.944
assert terminal['clockPaused'] == 1 and terminal['simulationActive'] == 0
assert terminal['playerBaseHp'] == 800 and terminal['enemyBaseHp'] == 0
trace = [json.loads(line) for line in data(PREFIX + proof['trace']).splitlines() if line.strip()]
finished = [row for row in trace if row.get('phase') == 'Finished']
assert len(finished) >= 2
assert all(row['elapsed'] == row['clockElapsed'] == 232.944 and row['outcome'] == 'Victory' and row['clockPaused'] == 1 and row['simulationActive'] == 0 and row['playerBaseHp'] == 800 and row['enemyBaseHp'] == 0 for row in finished)
audit = proof['inputAudit']
assert audit['recordedContacts'] == audit['completedContacts'] == 62 and not audit['errors']
assert audit['phases'] == {'Menu':9,'Runtime':52,'Terminal':1}
assert audit['strictRecordedContacts'] and audit['strictAllContactsEnded'] and audit['terminalContactsRecorded']
touch = [json.loads(line) for line in data(PREFIX + proof['trace'] + '.touch.jsonl').splitlines() if line.strip()]
assert touch and all(row['receiptViolations'] == row['unexpected'] == row['interventions'] == 0 for row in touch)
ended = [row for row in touch if row['kind'] == 'Ended']
assert len(ended) == 62 and sum(row['phase'] == 'Terminal' for row in ended) == 1
observation_bytes = data(PREFIX + proof['observation'])
assert sha(observation_bytes) == proof['observationSha256']
observations = [json.loads(line) for line in observation_bytes.splitlines() if line.strip()]
assert any(row['stage'] == 'NormalDriverFlags' and row['normalDriverFlags'] is True and row['timeScale'] == 1 for row in observations)
assert any(row['stage'] == 'NativeSetupBriefing' and row['renderedTitle'] and row['renderedObjective'] and row['localizedObjectiveCopy'] for row in observations)
assert any(row['stage'] == 'NativeHudPurpose' and row['renderedTitle'] and row['renderedBody'] for row in observations)
assert any(row['stage'] == 'NativeAriaControl' and row['presentedControl'] and row['ariaActive'] and row['localizedStopCopy'] for row in observations)
assert any(row['stage'] == 'NativeRenderedResultText' and row['localizedVictoryCopy'] and row['renderedTitle'] and row['frozenVictory'] and row['outcome'] == 'Victory' for row in observations)
assert any(row['stage'] == 'NativeRenderedResultText' and row['title'] == 'ﺗﻨﻈﯿﻢ ﻧﺒﺮﺩ' and row['renderedTitle'] and row['frozenVictory'] for row in observations)
assert any(row['stage'] == 'NativeAdjustSetupDestination' and row['renderedTitle'] and row['renderedObjective'] and row['renderedVictoryWitnessed'] and row['localizedObjectiveCopy'] for row in observations)
closed = [row for row in observations if row['stage'] == 'ClosedObserver']
assert len(closed) == 1 and all(closed[0][key] for key in ['setupSeen','bindingSeen','purposeSeen','activeControlSeen','resultSeen','returnSeen'])
response = json.loads(data(PREFIX + 'compile1167-caption-owner-reviewed-dry-run.json'))
assert response['success'] and response['data']['result']['success']
assert len(response['data']['result']['diagnostics']) == 10
assert all(item['severity'] == 'warning' and item['id'] == 'CS0618' for item in response['data']['result']['diagnostics'])
response = json.loads(data(PREFIX + 'load1167-normal-aria-observer.json'))
assert response['success'] and response['data']['result']['success']
assert len(response['data']['result']['diagnostics']) == 10
assert all(item['severity'] == 'warning' and item['id'] == 'CS0618' for item in response['data']['result']['diagnostics'])
assert response['data']['parameters']['file'] == '/private/tmp/S005NormalAria1167Observer.cs'

assert index['currentFinalSourceNormalWins'] == 0
start = json.loads(data(PREFIX + 'native1167-runner-start.json'))
assert start['explicitHeavyLaneOwner'] == 'main1167' and start['normalInput']
assert start['fixtureSha256'] == proof['fixtureSha256'] and start['executedRunnerSha256'] == proof['executedRunnerSha256']
assert start['matchResourcesPosesDamageVisibilityClocksOutcomeWrittenByAdapter'] is False
assert start['method'] == 'S005NormalAria1167Observer.RunValidation'
dispatch = json.loads(data(PREFIX + proof['log'] + '.dispatch.json'))
assert dispatch['success'] and dispatch['data']['result']['success'] and dispatch['data']['result']['result'] == 'scheduled'
for name in ['native1167-before-load-native-idle.json','native1167-before-dispatch-native-idle.json','native1167-after-close-native-idle.json']:
    idle = json.loads(data(PREFIX + name))
    assert idle['success'] and idle['data']['result']['success']
    state = idle['data']['result']['result']
    assert all(state[key] is False for key in ['playing','changing','compiling','updating'])
    assert state['project'] == '/Users/farhad/Projects/WarlineCapture/Assets'
for suffix in ['.setup.png','.opening.png','.menu-selection-diagnostic.png','.terminal-result.png','.terminal-destination.png']:
    assert data(PREFIX + proof['trace'] + suffix).startswith(b'\x89PNG\r\n\x1a\n')
viewport_rows = [row for row in observations if row['stage'] == 'RenderedViewportContext']
assert len(viewport_rows) == 3
assert all(row['nativeViewportMatched'] and row['screenWidth'] == row['canvasWidth'] == 2400 and row['screenHeight'] == row['canvasHeight'] == 1080 for row in viewport_rows)
for role in ['SetupTitle','SetupObjective','AdjustSetupTitle','AdjustSetupObjective']:
    assert any(row['stage'] == 'NativeBoundText' and row['role'] == role and row['exactBoundReference'] and row['renderedBody'] and row['activeText'] and not row['culledText'] and row['visibleGlyphs'] > 0 and row['safeAreaOverlap'] for row in observations)
helper = index['originalSourceManifestHelper']
assert sha(data(helper['path'])) == helper['sha256']
prepared = json.loads(data(PREFIX + 'delegation-normal-aria1167-prepared.json'))
assert prepared['fixtureSha256'] == proof['fixtureSha256'] and prepared['runnerSha256'] == proof['executedRunnerSha256']
assert prepared['sourceManifestHelperSha256'] == helper['sha256']

CORE_SOURCE = 'd5afc88f225920451037f6e1c9f7e9d83dfc567e3bbdadfb5b6d65d9ee6774f4'
core = json.loads(data(PREFIX + 'native1175-focused-current-proof.json'))
assert core == index['coreRun']['proof'] and core['result'] == 'Passed'
assert core['sourceHash'] == CORE_SOURCE and core['sourceUnchanged']
assert core['actualWrapperExit'] == core['nativeBridgeExit'] == 0
assert core['actualSuites'] == 16 and core['actualPlannerCases'] == 111 and core['actualMoveCommandTests'] == 26
assert core['nativeMatchCount'] == 0 and core['playerReady'] is False
assert core['compilerErrors'] == core['cleanupWarnings'] == []
core_aliases = index['coreRun']['fullSourceAliasNames']
assert len(core_aliases) == 2 and len({data(PREFIX + name) for name in core_aliases}) == 1
for alias in core_aliases:
    assert json.loads(data(PREFIX + alias.replace('-source-manifest.json','-independent-fingerprint.json')))['sourceHash'] == CORE_SOURCE
core_log = data(PREFIX + core['log'])
assert core['requiredPassMarker'].encode() in core_log and core['requiredPassMarkerPresent']
for marker in [b'[AriaSkirmishPlanValidation] result=Passed cases=111', b'[UnitMoveOrderFocusedValidation] result=Passed tests=26', b'[SkirmishSharedFogTests] result=Passed cases=19',b'[SkirmishFogLedgerTests] result=Passed cases=9',b'[SkirmishFogConsumerTests] result=Passed cases=13',b'[SkirmishFogVfxTests] result=Passed cases=8',b'[AssistantGuidanceLifecycleTests] result=Passed cases=2',b'[ExistingEditorValidation] result=Passed executeMethod=S005IntegratedTechnical1175.RunValidation']:
    assert marker in core_log
assert int(data(PREFIX + core['log'] + '.exit')) == int(data(PREFIX + core['log'] + '.wrapper-exit')) == 0
poll = json.loads(data(PREFIX + 'native1175-compilation-poll2.json'))
assert poll['success'] is False and poll['data'] is None
assert len(poll['errors']) == 1 and poll['errors'][0]['code'] == 'COMMAND_FAILED'
assert 'Connection reset by server' in poll['errors'][0]['message']
compilation = json.loads(data(PREFIX + 'native1175-current-compilation.json'))
assert compilation['success'] and compilation['data']['success']
state = json.loads(compilation['data']['result'])
assert state == {'status':'completed','failed':False,'errors':[]}
quality_failed = json.loads(data(PREFIX + 'native1175-live-mobile-quality.json'))
assert quality_failed['success'] is False and quality_failed['data'] is None
assert len(quality_failed['errors']) == 1 and 'Main thread operation timed out after 5000ms' in quality_failed['errors'][0]['message']
poll_connect = json.loads(data(PREFIX + 'native1175-compilation-poll3.json'))
assert poll_connect['success'] is False and poll_connect['data'] is None
assert len(poll_connect['errors']) == 1 and 'Cannot connect to Unity Editor Pipeline server' in poll_connect['errors'][0]['message']
quality = json.loads(data(PREFIX + 'native1175-native-idle-quality-recheck1.json'))
assert quality['success'] and quality['data']['result']['success']
quality_state = quality['data']['result']['result']
assert quality_state['quality'] == 'Mobile' and quality_state['msaa'] == 0
assert all(quality_state[key] is False for key in ['playing','changing','compiling','updating','running'])
assert quality_state['project'] == '/Users/farhad/Projects/WarlineCapture/Assets'
for name in ['compile1175-integrated-reviewed-dry-run.json','load1175-integrated-reviewed.json']:
    response = json.loads(data(PREFIX + name))
    assert response['success'] and response['data']['result']['success']
    assert response['data']['result']['diagnostics'] == []
    assert response['data']['parameters']['file'] == index['coreRun']['fixturePath']
assert sha(data(index['coreRun']['fixturePath'])) == index['coreRun']['fixtureSha256']

print('[S005Normal1167Core1175Evidence] result=Passed runs=2 aliases=62 objects=52 normalWins=1 currentFinalSourceNormalWins=0 playerReady=0')
