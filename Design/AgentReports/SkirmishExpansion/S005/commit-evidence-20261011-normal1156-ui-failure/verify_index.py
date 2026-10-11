
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
SOURCE = 'ca84597a2ac1992ba7c5aaa60653155aaa31ed1e6c0fda7bc28591f83910fda7'
assert index['readinessClaim'] is False and index['normalWins'] == 0
assert len(index['dependencies']) == 7
assert len(index['files']) == index['statistics']['selectedExactAliases'] == 35
assert len(index['objects']) == index['statistics']['uniqueObjects'] == 29
scan = index['credentialScan']
assert not any(scan['markerCounts'].values())
assert sha(data(PREFIX + scan['log'])) == scan['sha256']
proof = json.loads(data(PREFIX + 'native1156-normal-aria-failure.json'))
assert proof == index['run']['proof']
EXPECTED_ERRORS = ['Strict actual native bound rendered text evidence missing: SetupTitle', 'Strict actual native bound rendered text evidence missing: AdjustSetupTitle', 'Actual setup objective/failure-rule copy missing', 'Actual rendered objectives/clock during ARIA missing', 'Actual rendered S005 AdjustSetup must follow exact native frozen/rendered Victory of same attempt', 'Read-only observer closure incomplete']
assert proof['result'] == 'Failed' and proof['errors'] == EXPECTED_ERRORS
assert index['run']['strictObserverErrors'] == EXPECTED_ERRORS
assert index['run']['strictObserverResult'] == 'Failed' and index['run']['acceptedNormalWins'] == 0
assert proof['sourceHash'] == SOURCE and proof['fullSourceUnchanged'] is True
assert proof['normalInput'] is True and proof['normalSpeedCertified'] is False
assert proof['fundingReadinessActorPoseDamageVisibilityOutcomeInjected'] is False
assert proof['countedWinCertified'] is False and proof['countsForAriaSeedBaseline'] is False
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
assert sha(data('/private/tmp/s005-run-normal-aria-1156.py')) == proof['executedRunnerSha256']
for marker in [b'[SkirmishTerminalProbe] frozen=1 returnInput=Touch target=ADJUST SETUP',b'[SkirmishTerminalProbe] result=Passed reason=frozenResultAnd:ADJUST SETUP',b'[SkirmishS002AriaRun] appended=1 countedWin=1 error=',b'[SkirmishValidationExit] result=Requested code=0',b'[ExistingEditorValidation] result=Passed executeMethod=S005NormalAria1156Observer.RunValidation']:
    assert marker in log
row = proof['nativeCountedRow']
assert row[3] == SOURCE and row[5:9] == ['Standard','Regular','104734','fa-IR']
assert row[13:16] == ['Victory','MainBaseDestroyed','232.584']
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
assert terminal['elapsed'] == terminal['clockElapsed'] == 232.584
assert terminal['clockPaused'] == 1 and terminal['simulationActive'] == 0
assert terminal['playerBaseHp'] == 800 and terminal['enemyBaseHp'] == 0
trace = [json.loads(line) for line in data(PREFIX + proof['trace']).splitlines() if line.strip()]
finished = [row for row in trace if row.get('phase') == 'Finished']
assert len(finished) >= 2
assert all(row['elapsed'] == row['clockElapsed'] == 232.584 and row['outcome'] == 'Victory' and row['clockPaused'] == 1 and row['simulationActive'] == 0 and row['playerBaseHp'] == 800 and row['enemyBaseHp'] == 0 for row in finished)
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
closed = [row for row in observations if row['stage'] == 'ClosedObserver']
assert len(closed) == 1
assert {key:closed[0][key] for key in ['setupSeen','bindingSeen','purposeSeen','activeControlSeen','resultSeen','returnSeen']} == {'setupSeen':False,'bindingSeen':True,'purposeSeen':True,'activeControlSeen':True,'resultSeen':True,'returnSeen':False}
assert any(row['stage'] == 'NativeRenderedResultText' and row['frozenVictory'] and row['outcome'] == 'Victory' for row in observations)
response = json.loads(data(PREFIX + 'compile1156-post-result-reviewed-dry-run.json'))
assert response['success'] and response['data']['result']['success']
assert len(response['data']['result']['diagnostics']) == 10
assert all(item['severity'] == 'warning' and item['id'] == 'CS0618' for item in response['data']['result']['diagnostics'])
response = json.loads(data(PREFIX + 'load1156-normal-aria-observer.json'))
assert response['success'] and response['data']['result']['success']
assert len(response['data']['result']['diagnostics']) == 10
assert all(item['severity'] == 'warning' and item['id'] == 'CS0618' for item in response['data']['result']['diagnostics'])
assert response['data']['parameters']['file'] == '/private/tmp/S005NormalAria1156Observer.cs'
start = json.loads(data(PREFIX + 'native1156-runner-start.json'))
assert start['explicitHeavyLaneOwner'] == 'main1156' and start['normalInput'] is True
assert start['matchResourcesPosesDamageVisibilityClocksOutcomeWrittenByAdapter'] is False
assert start['fixtureSha256'] == proof['fixtureSha256'] and start['executedRunnerSha256'] == proof['executedRunnerSha256']
assert start['method'] == 'S005NormalAria1156Observer.RunValidation'
dispatch = json.loads(data(PREFIX + proof['log'] + '.dispatch.json'))
assert dispatch['success'] and dispatch['data']['result']['success'] and dispatch['data']['result']['result'] == 'scheduled'
for name in ['native1156-before-load-native-idle.json','native1156-before-dispatch-native-idle.json','native1156-after-close-native-idle.json']:
    idle = json.loads(data(PREFIX + name))
    assert idle['success'] and idle['data']['result']['success']
    state = idle['data']['result']['result']
    assert all(state[key] is False for key in ['playing','changing','compiling','updating'])
    assert state['project'] == '/Users/farhad/Projects/WarlineCapture/Assets'
for suffix in ['.setup.png','.opening.png','.menu-selection-diagnostic.png','.terminal-result.png','.terminal-destination.png']:
    assert data(PREFIX + proof['trace'] + suffix).startswith(b'\x89PNG\r\n\x1a\n')
assert not any('1157' in file['originalPath'] for file in index['files'])

print('[S005Normal1156UiFailureEvidence] result=Passed runs=1 aliases=35 objects=29 acceptedNormalWins=0 gameplayVictories=1 playerReady=0')
