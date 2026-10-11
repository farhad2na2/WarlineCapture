
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
OLD = 'e17a324ececad00596682b1b9820ec7bf549d489537de3417f304f20077df098'
NEW = 'ca84597a2ac1992ba7c5aaa60653155aaa31ed1e6c0fda7bc28591f83910fda7'
assert index['readinessClaim'] is False and index['normalWins'] == 0
assert len(index['dependencies']) == 5
assert len(index['files']) == index['statistics']['selectedExactAliases'] == 61
assert len(index['objects']) == index['statistics']['uniqueObjects'] == 54
for scan in index['credentialScans']:
    assert not any(scan['markerCounts'].values())
    assert sha(data(PREFIX + scan['log'])) == scan['sha256']
proof = json.loads(data(PREFIX + 'native1158-normal-aria-failure.json'))
assert proof == index['run']['proof']
assert proof['sourceHash'] == OLD and proof['fullSourceUnchanged'] is True
assert proof['result'] == 'Failed' and proof['playerReady'] is False
assert proof['normalInput'] is True and proof['fundingReadinessActorPoseDamageVisibilityOutcomeInjected'] is False
assert proof['countedWinCertified'] is False and proof['countsForAriaSeedBaseline'] is False
source_aliases = index['run']['fullSourceAliasNames']
assert len({data(PREFIX + name) for name in source_aliases}) == 1
for alias in source_aliases:
    assert json.loads(data(PREFIX + alias.replace('-source-manifest.json','-independent-fingerprint.json')))['sourceHash'] == OLD
log = data(PREFIX + proof['log'])
assert sha(log) == proof['logSha256']
assert int(data(PREFIX + proof['log'] + '.exit')) == int(data(PREFIX + proof['log'] + '.wrapper-exit')) == 1
assert sha(data(PREFIX + proof['fixture'])) == proof['fixtureSha256']
assert sha(data('/private/tmp/s005-run-normal-aria-1158.py')) == proof['executedRunnerSha256']
row = proof['nativeCountedRow']
assert row[3] == OLD and row[13:16] == ['Abort','ariaStopped:Blocked','404.205']
assert data(str(Path(row[-2]))) == data(PREFIX + proof['trace'])
assert data(str(Path(row[-1]))) != log
assert proof['inputAudit']['recordedContacts'] == proof['inputAudit']['completedContacts'] == 201
assert not proof['inputAudit']['errors']
touch = [json.loads(line) for line in data(PREFIX + proof['trace'] + '.touch.jsonl').splitlines() if line.strip()]
assert touch and all(r['receiptViolations'] == r['unexpected'] == r['interventions'] == 0 for r in touch)
ended = [r for r in touch if r['kind'] == 'Ended']
assert len(ended) == 201
reader = json.loads(data('/private/tmp/S005AriaApproachReadOnly1153.manifest.json'))
assert sha(data(reader['readerPath'])) == reader['readerSha256']
for snapshot in index['readonlySnapshots']:
    raw = json.loads(data(PREFIX + snapshot['path']))
    assert raw['success'] and raw['data']['result']['success']
    assert raw['data']['parameters']['file'] == reader['readerPath']
    observed = raw['data']['result']['result']
    assert observed['active'] and observed['expanded'] and observed['seed'] == 104734
    assert observed['worldFrame'] == snapshot['actualNativeFrame']
    assert all(observed[k] is False for k in ['gameplayOrPresentationWrites','callbacksRegistered','hiddenActorReads'])
    first = [g for g in observed['groups'] if g['groupId'] in [1,2]]
    assert first == snapshot['firstTwoRifleGroups'] and len(first) == 2
    assert all(g['role'] == 'Rifle' and g['lastOrder'] == 'Attack' for g in first)
failed_reader = json.loads(data(PREFIX + 'native1158-friendly-qualification1161-drycompile.json'))
inner = failed_reader['data']['result']
assert failed_reader['success'] and inner['success'] is False and inner['executeMs'] == 0
assert any(d['id'] == 'CS0246' and 'SkirmishSharedSupplyInitialized' in d['message'] for d in inner['diagnostics'])
assert failed_reader['data']['parameters']['dry_run'] == 'true'
reader1161 = json.loads(data('/private/tmp/S005FriendlyCommandQualificationReadOnly1161.manifest.json'))
assert sha(data(reader1161['readerPath'])) == reader1161['readerSha256'] == index['readonly1161Failure']['readerOriginalSha256']
core = json.loads(data(PREFIX + 'native1163-focused-current-proof.json'))
assert core['result'] == 'Passed' and core['sourceHash'] == NEW and core['sourceUnchanged'] is True
assert core['suites'] == 16 and core['plannerCases'] == 111 and core['moveCommandTests'] == 26
assert core['nativeMatchCount'] == 0 and core['playerReady'] is False
new_source = data(PREFIX + core['preSourceManifest'])
assert new_source == data(PREFIX + core['postSourceManifest'])
for name in [core['preSourceManifest'],core['postSourceManifest']]:
    assert json.loads(data(PREFIX + name.replace('-source-manifest.json','-independent-fingerprint.json')))['sourceHash'] == NEW
core_log = data(PREFIX + core['log'])
assert sha(core_log) == index['core1163']['logSha256']
assert int(data(PREFIX + core['log'] + '.exit')) == int(data(PREFIX + core['log'] + '.wrapper-exit')) == 0
assert core['requiredPassMarker'].encode() in core_log
assert b'[AriaSkirmishPlanValidation] result=Passed cases=111' in core_log
assert b'[UnitMoveOrderFocusedValidation] result=Passed tests=26' in core_log
assert b'[ExistingEditorValidation] result=Passed executeMethod=S005IntegratedTechnical1163.RunValidation' in core_log
compilation = json.loads(data(PREFIX + 'native1163-current-compilation.json'))
actual_compile = json.loads(compilation['data']['result'])
assert compilation['success'] and actual_compile['failed'] is False and actual_compile['errors'] == []
for name in ['compile1163-reviewed-dry-run.json','load1163-reviewed-integrated.json']:
    response = json.loads(data(PREFIX + name))
    assert response['success'] and response['data']['result']['success']
    assert response['data']['result']['diagnostics'] == []
    assert response['data']['parameters']['file'] == '/private/tmp/S005IntegratedTechnical1163.cs'
mobile = json.loads(data(PREFIX + 'native1163-live-mobile-quality.json'))
assert mobile['success'] and mobile['data']['result']['success']
assert mobile['data']['result']['result'] == {'quality':'Mobile','msaa':0}
reviewed = json.loads(data(index['predicateManifest']))
assert sha(data(index['predicatePatch'])) == reviewed['patchSha256']
old = json.loads(data(PREFIX + proof['preManifest']))
new = json.loads(new_source)
assert old['materials'] == new['materials'] and old['files'].keys() == new['files'].keys()
changed = [path for path in old['files'] if old['files'][path] != new['files'][path]]
assert set(changed) == set(index['actualSourceDeltaFiles']) == {f['path'] for f in reviewed['files']}
for source in index['sourceSnapshots']:
    assert sha(data(source['snapshot'])) == source['stagedSha256'] == new['files'][source['productionPath']]
    assert source['baseSha256'] == old['files'][source['productionPath']]
assert len(index['sourceSnapshots']) == 2

print('[S005Aria1158Core1163SupplementEvidence] result=Passed runs=2 aliases=61 objects=54 suites=16 moveTests=26 ariaCases=111 normalWins=0 playerReady=0')
