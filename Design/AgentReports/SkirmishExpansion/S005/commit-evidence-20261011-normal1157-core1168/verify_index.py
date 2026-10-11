
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
assert index['readinessClaim'] is False and index['normalWins'] == 1
assert len(index['dependencies']) == 8
assert len(index['files']) == index['statistics']['selectedExactAliases'] == 60
assert len(index['objects']) == index['statistics']['uniqueObjects'] == 50
for scan in index['credentialScans']:
    assert not any(scan['markerCounts'].values())
    assert sha(data(PREFIX + scan['log'])) == scan['sha256']
proof = json.loads(data(PREFIX + 'native1157-normal-aria-pass.json'))
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
assert sha(data('/private/tmp/s005-run-normal-aria-1157.py')) == proof['executedRunnerSha256']
for marker in [b'[SkirmishTerminalProbe] frozen=1 returnInput=Touch target=ADJUST SETUP',b'[SkirmishTerminalProbe] result=Passed reason=frozenResultAnd:ADJUST SETUP',b'[SkirmishS002AriaRun] appended=1 countedWin=1 error=',b'[SkirmishValidationExit] result=Requested code=0',b'[ExistingEditorValidation] result=Passed executeMethod=S005NormalAria1157Observer.RunValidation']:
    assert marker in log
row = proof['nativeCountedRow']
assert row[3] == SOURCE and row[5:9] == ['Standard','Regular','130368','en']
assert row[13:16] == ['Victory','MainBaseDestroyed','234.669']
assert row[16:18] == ['0','0']
assert data(str(Path(row[-2]))) == data(PREFIX + proof['trace'])
assert data(str(Path(row[-1]))) != log
resolved = proof['actualResolvedSetup']
assert resolved['catalog'] == 'S005' and resolved['definition'] == 'skirmish.s005'
assert resolved['size'] == 'Standard' and resolved['difficulty'] == 'Regular'
assert resolved['army'] == 'CombinedArms' and resolved['start'] == 'FieldBase'
assert resolved['locale'] == 'en' and resolved['seed'] == 130368
assert resolved['sharedFog'] is True and resolved['developmentFullVision'] is False
terminal = proof['terminal']
assert terminal['phase'] == 'Finished' and terminal['outcome'] == 'Victory'
assert terminal['elapsed'] == terminal['clockElapsed'] == 234.669
assert terminal['clockPaused'] == 1 and terminal['simulationActive'] == 0
assert terminal['playerBaseHp'] == 800 and terminal['enemyBaseHp'] == 0
trace = [json.loads(line) for line in data(PREFIX + proof['trace']).splitlines() if line.strip()]
finished = [row for row in trace if row.get('phase') == 'Finished']
assert len(finished) >= 2
assert all(row['elapsed'] == row['clockElapsed'] == 234.669 and row['outcome'] == 'Victory' and row['clockPaused'] == 1 and row['simulationActive'] == 0 and row['playerBaseHp'] == 800 and row['enemyBaseHp'] == 0 for row in finished)
audit = proof['inputAudit']
assert audit['recordedContacts'] == audit['completedContacts'] == 67 and not audit['errors']
assert audit['phases'] == {'Menu':9,'Runtime':57,'Terminal':1}
assert audit['strictRecordedContacts'] and audit['strictAllContactsEnded'] and audit['terminalContactsRecorded']
touch = [json.loads(line) for line in data(PREFIX + proof['trace'] + '.touch.jsonl').splitlines() if line.strip()]
assert touch and all(row['receiptViolations'] == row['unexpected'] == row['interventions'] == 0 for row in touch)
ended = [row for row in touch if row['kind'] == 'Ended']
assert len(ended) == 67 and sum(row['phase'] == 'Terminal' for row in ended) == 1
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
response = json.loads(data(PREFIX + 'compile1157-strict-result-normal-dry-run.json'))
assert response['success'] and response['data']['result']['success']
assert len(response['data']['result']['diagnostics']) == 10
assert all(item['severity'] == 'warning' and item['id'] == 'CS0618' for item in response['data']['result']['diagnostics'])
response = json.loads(data(PREFIX + 'load1157-normal-aria-observer.json'))
assert response['success'] and response['data']['result']['success']
assert len(response['data']['result']['diagnostics']) == 10
assert all(item['severity'] == 'warning' and item['id'] == 'CS0618' for item in response['data']['result']['diagnostics'])
assert response['data']['parameters']['file'] == '/private/tmp/S005NormalAria1157Observer.cs'
start = json.loads(data(PREFIX + 'native1157-runner-start.json'))
assert start['explicitHeavyLaneOwner'] == 'main1157' and start['normalInput']
assert start['fixtureSha256'] == proof['fixtureSha256'] and start['executedRunnerSha256'] == proof['executedRunnerSha256']
assert start['matchResourcesPosesDamageVisibilityClocksOutcomeWrittenByAdapter'] is False
for name in ['native1157-before-load-native-idle.json','native1157-before-dispatch-native-idle.json','native1157-after-close-native-idle.json']:
    idle = json.loads(data(PREFIX + name))
    assert idle['success'] and idle['data']['result']['success']
    state = idle['data']['result']['result']
    assert all(state[key] is False for key in ['playing','changing','compiling','updating'])
    assert state['project'] == '/Users/farhad/Projects/WarlineCapture/Assets'
for suffix in ['.setup.png','.opening.png','.menu-selection-diagnostic.png','.terminal-result.png','.terminal-destination.png']:
    assert data(PREFIX + proof['trace'] + suffix).startswith(b'\x89PNG\r\n\x1a\n')

CORE_SOURCE = 'f788e01e25897c16ae470e4e4d9d687a574c35eca934aea9aa25ed9f72254893'
core = json.loads(data(PREFIX + 'native1168-focused-current-proof.json'))
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
for marker in [b'[AriaSkirmishPlanValidation] result=Passed cases=111', b'[UnitMoveOrderFocusedValidation] result=Passed tests=26', b'[SkirmishSharedFogTests] result=Passed cases=19',b'[SkirmishFogLedgerTests] result=Passed cases=9',b'[SkirmishFogConsumerTests] result=Passed cases=13',b'[SkirmishFogVfxTests] result=Passed cases=8',b'[AssistantGuidanceLifecycleTests] result=Passed cases=2',b'[ExistingEditorValidation] result=Passed executeMethod=S005IntegratedTechnical1168.RunValidation']:
    assert marker in core_log
assert int(data(PREFIX + core['log'] + '.exit')) == int(data(PREFIX + core['log'] + '.wrapper-exit')) == 0
poll = json.loads(data(PREFIX + 'native1168-compilation-poll2.json'))
assert poll['success'] is False and poll['data'] is None
assert len(poll['errors']) == 1 and poll['errors'][0]['code'] == 'COMMAND_FAILED'
assert 'Connection reset by server' in poll['errors'][0]['message']
compilation = json.loads(data(PREFIX + 'native1168-current-compilation.json'))
assert compilation['success'] and compilation['data']['success']
state = json.loads(compilation['data']['result'])
assert state == {'status':'completed','failed':False,'errors':[]}
quality = json.loads(data(PREFIX + 'native1168-live-mobile-quality.json'))
assert quality['success'] and quality['data']['result']['success']
assert quality['data']['result']['result'] == {'quality':'Mobile','msaa':0}
for name in ['compile1168-integrated-reviewed-dry-run.json','load1168-integrated-reviewed.json']:
    response = json.loads(data(PREFIX + name))
    assert response['success'] and response['data']['result']['success']
    assert response['data']['result']['diagnostics'] == []
    assert response['data']['parameters']['file'] == index['coreRun']['fixturePath']
assert sha(data(index['coreRun']['fixturePath'])) == index['coreRun']['fixtureSha256']
patch = index['sourcePatch']
original = data(patch['originalSnapshot']); candidate = data(patch['newSnapshot'])
assert sha(original) == patch['originalSha256'] and sha(candidate) == patch['proposedAndActualSha256']
assert sha(data(patch['patch'])) == patch['patchSha256']
old_source = json.loads(data(PREFIX + source_aliases[-1]))
new_source = json.loads(data(PREFIX + core_aliases[0]))
assert old_source['materials'] == new_source['materials']
changed = [name for name in set(old_source['files']) | set(new_source['files']) if old_source['files'].get(name) != new_source['files'].get(name)]
assert changed == [patch['source']]
assert old_source['files'][patch['source']] == sha(original) and new_source['files'][patch['source']] == sha(candidate)
start = candidate.index(b'                    else if (selected.ScenarioId == "S005")')
end = candidate.index(b'                    else if (!string.IsNullOrEmpty(titleKey))', start)
reversed_source = candidate[:start] + candidate[start:end].replace(b'                    {\n',b'',1).replace(b'                        mapNameText.overflowMode = TextOverflowModes.Overflow;\n',b'',1).replace(b'                    }\n',b'',1) + candidate[end:]
assert reversed_source == original
allowed_1167 = {'/private/tmp/s005-title-persian-overflow1167-manifest.json','/private/tmp/s005-title-persian-overflow1167.patch'}
assert all('1167' not in file['originalPath'] or file['originalPath'] in allowed_1167 for file in index['files'])

print('[S005Normal1157Core1168Evidence] result=Passed runs=2 aliases=60 objects=50 normalWins=1 currentSourceNormalWins=0 playerReady=0')
