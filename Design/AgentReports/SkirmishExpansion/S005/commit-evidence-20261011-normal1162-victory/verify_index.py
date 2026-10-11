
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
aliases = {}
for file in index['files']:
    assert file['originalPath'] not in aliases
    original = objects[file['sha256']]
    assert sha(original) == file['sha256'] and len(original) == file['bytes']
    aliases[file['originalPath']] = original
def data(name): return aliases[name]


PREFIX = 'Design/AgentReports/SkirmishExpansion/S005/'
SOURCE = 'ca84597a2ac1992ba7c5aaa60653155aaa31ed1e6c0fda7bc28591f83910fda7'
assert index['readinessClaim'] is False and index['normalWins'] == 1
assert len(index['dependencies']) == 6
assert len(index['files']) == index['statistics']['selectedExactAliases'] == 39
assert len(index['objects']) == index['statistics']['uniqueObjects'] == 33
scan = index['credentialScan']
assert not any(scan['markerCounts'].values())
assert sha(data(PREFIX + scan['log'])) == scan['sha256']
proof = json.loads(data(PREFIX + 'native1162-normal-aria-pass.json'))
assert proof == index['run']['proof']
assert proof['result'] == 'Passed' and not proof['errors']
assert proof['sourceHash'] == SOURCE and proof['fullSourceUnchanged'] is True
assert proof['normalInput'] is True and proof['normalSpeedCertified'] is True
assert proof['fundingReadinessActorPoseDamageVisibilityOutcomeInjected'] is False
assert proof['countedWinCertified'] is True and proof['countsForAriaSeedBaseline'] is True
assert proof['playerReady'] is False and proof['realHumanDeviceAcceptance'] is False
source_aliases = index['run']['fullSourceAliasNames']
assert len({data(PREFIX + name) for name in source_aliases}) == 1
for alias in source_aliases:
    assert json.loads(data(PREFIX + alias.replace('-source-manifest.json','-independent-fingerprint.json')))['sourceHash'] == SOURCE
log = data(PREFIX + proof['log'])
assert sha(log) == proof['logSha256']
assert int(data(PREFIX + proof['log'] + '.exit')) == int(data(PREFIX + proof['log'] + '.wrapper-exit')) == 0
assert sha(data(PREFIX + proof['fixture'])) == proof['fixtureSha256']
assert sha(data('/private/tmp/s005-run-normal-aria-1162.py')) == proof['executedRunnerSha256']
for marker in [b'[SkirmishTerminalProbe] frozen=1 returnInput=Touch target=ADJUST SETUP',b'[SkirmishTerminalProbe] result=Passed reason=frozenResultAnd:ADJUST SETUP',b'[SkirmishS002AriaRun] appended=1 countedWin=1 error=',b'[SkirmishValidationExit] result=Requested code=0',b'[ExistingEditorValidation] result=Passed executeMethod=S005NormalAria1162Observer.RunValidation']:
    assert marker in log
row = proof['nativeCountedRow']
assert row[3] == SOURCE and row[5:9] == ['Standard','Regular','104734','en']
assert row[13:16] == ['Victory','MainBaseDestroyed','233.56']
assert row[16:18] == ['0','0']
assert data(str(Path(row[-2]))) == data(PREFIX + proof['trace'])
assert data(str(Path(row[-1]))) != log
resolved = proof['actualResolvedSetup']
assert resolved['catalog'] == 'S005' and resolved['definition'] == 'skirmish.s005'
assert resolved['size'] == 'Standard' and resolved['difficulty'] == 'Regular'
assert resolved['army'] == 'CombinedArms' and resolved['start'] == 'FieldBase'
assert resolved['locale'] == 'en' and resolved['seed'] == 104734
assert resolved['sharedFog'] is True and resolved['developmentFullVision'] is False
terminal = proof['terminal']
assert terminal['phase'] == 'Finished' and terminal['outcome'] == 'Victory'
assert terminal['elapsed'] == terminal['clockElapsed'] == 233.56
assert terminal['clockPaused'] == 1 and terminal['simulationActive'] == 0
assert terminal['playerBaseHp'] == 800 and terminal['enemyBaseHp'] == 0
trace = [json.loads(line) for line in data(PREFIX + proof['trace']).splitlines() if line.strip()]
finished = [row for row in trace if row.get('phase') == 'Finished']
assert len(finished) >= 2
assert all(row['elapsed'] == row['clockElapsed'] == 233.56 and row['outcome'] == 'Victory' and row['clockPaused'] == 1 and row['simulationActive'] == 0 and row['playerBaseHp'] == 800 and row['enemyBaseHp'] == 0 for row in finished)
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
assert any(row['stage'] == 'NativeSetupBriefing' and 'S005' in row['title'] for row in observations)
assert any(row['stage'] == 'NativeHudPurpose' and row['renderedTitle'] and row['renderedBody'] for row in observations)
assert any(row['stage'] == 'NativeAriaControl' and row['presentedControl'] and row['controlText'] == 'STOP ARIA' for row in observations)
assert any(row['stage'] == 'NativeRenderedResultText' and row['title'] == 'VICTORY' and row['renderedTitle'] and row['frozenVictory'] and row['outcome'] == 'Victory' for row in observations)
assert any(row['stage'] == 'NativeRenderedResultText' and row['title'] == 'ADJUST SETUP' and row['renderedTitle'] and row['frozenVictory'] for row in observations)
assert any(row['stage'] == 'NativeAdjustSetupDestination' and row['renderedTitle'] and row['renderedObjective'] and row['renderedVictoryWitnessed'] and 'S005' in row['title'] for row in observations)
closed = [row for row in observations if row['stage'] == 'ClosedObserver']
assert len(closed) == 1 and all(closed[0][key] for key in ['setupSeen','bindingSeen','purposeSeen','activeControlSeen','resultSeen','returnSeen'])
response = json.loads(data(PREFIX + 'compile1162-post-result-reviewed-dry-run.json'))
assert response['success'] and response['data']['result']['success']
assert len(response['data']['result']['diagnostics']) == 10
assert all(item['severity'] == 'warning' and item['id'] == 'CS0618' for item in response['data']['result']['diagnostics'])
response = json.loads(data(PREFIX + 'load1162-normal-aria-observer.json'))
assert response['success'] and response['data']['result']['success']
assert len(response['data']['result']['diagnostics']) == 10
assert all(item['severity'] == 'warning' and item['id'] == 'CS0618' for item in response['data']['result']['diagnostics'])
assert response['data']['parameters']['file'] == '/private/tmp/S005NormalAria1162Observer.cs'
reader = json.loads(data('/private/tmp/S005AriaApproachReadOnly1153.manifest.json'))
assert sha(data(reader['readerPath'])) == reader['readerSha256'] == index['originalReadOnlyReaderSha256']
raw = json.loads(data(PREFIX + index['readOnlySnapshot']))
assert raw['success'] and raw['data']['result']['success']
assert raw['data']['parameters']['file'] == reader['readerPath']
readonly = raw['data']['result']['result']
assert all(readonly[key] is False for key in ['gameplayOrPresentationWrites','callbacksRegistered','hiddenActorReads'])

print('[S005Normal1162VictoryEvidence] result=Passed runs=1 aliases=39 objects=33 normalWins=1 playerReady=0')
