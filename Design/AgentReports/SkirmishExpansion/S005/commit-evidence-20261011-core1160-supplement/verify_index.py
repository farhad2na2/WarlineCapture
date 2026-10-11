
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
FAILED = 'dd52b2c2b274a08e36e6b345ed81bb978a51b1c3e491ad00851ab2201a30c6d3'
PASSED = 'e17a324ececad00596682b1b9820ec7bf549d489537de3417f304f20077df098'
TEST = 'Assets/Tests/Editor/UnitMoveOrderSystemTests.cs'
assert index['readinessClaim'] is False
assert not any(index['credentialScan']['boundedMarkerCounts'].values())
assert sha(data(index['credentialScan']['path'])) == index['credentialScan']['sha256']
assert {r['run'] for r in index['runs']} == {'1159','1160'}
assert len(index['dependencies']) == 4
assert len(index['files']) == index['statistics']['selectedExactAliases'] == 28
assert len(index['objects']) == index['statistics']['uniqueObjects'] == 27
old_manifest = json.loads(data(PREFIX + 'final-1159-integrated-pre-source-manifest.json'))
new_bytes = data(PREFIX + 'final-1160-integrated-pre-source-manifest.json')
assert new_bytes == data(PREFIX + 'final-1160-integrated-post-source-manifest.json')
new_manifest = json.loads(new_bytes)
assert old_manifest['materials'] == new_manifest['materials']
assert old_manifest['files'].keys() == new_manifest['files'].keys()
assert [path for path in old_manifest['files'] if old_manifest['files'][path] != new_manifest['files'][path]] == [TEST]
for name, expected in [('final-1159-integrated-pre-independent-fingerprint.json',FAILED),('final-1160-integrated-pre-independent-fingerprint.json',PASSED),('final-1160-integrated-post-independent-fingerprint.json',PASSED)]:
    assert json.loads(data(PREFIX + name))['sourceHash'] == expected
failure = json.loads(data(PREFIX + 'native1159-r1-failed-compilation.json'))
assert failure['success'] and failure['data']['success']
failed_inner = json.loads(failure['data']['result'])
assert failed_inner['failed'] is True and len(failed_inner['errors']) == 1
assert 'CS1612' in failed_inner['errors'][0] and TEST in failed_inner['errors'][0]
assert index['runs'][0]['nativeFixtureNeverLoaded'] and index['runs'][0]['nativeFixtureNeverExecuted']
assert not any('native-1159-' in file['originalPath'] for file in index['files'])
assert b'S005IntegratedTechnical1159' in data('/private/tmp/S005IntegratedTechnical1159.cs')
compilation = json.loads(data(PREFIX + 'native1160-current-compilation.json'))
assert compilation['success'] and compilation['data']['success']
compiled_inner = json.loads(compilation['data']['result'])
assert compiled_inner['failed'] is False and compiled_inner['errors'] == []
for name in ['compile1160-reviewed-dry-run.json','load1160-reviewed-integrated.json']:
    response = json.loads(data(PREFIX + name))
    assert response['success'] and response['data']['success'] and response['data']['result']['success']
    assert response['data']['result']['diagnostics'] == []
    assert response['data']['parameters']['file'] == '/private/tmp/S005IntegratedTechnical1160.cs'
mobile = json.loads(data(PREFIX + 'native1160-live-mobile-quality.json'))
assert mobile['success'] and mobile['data']['result']['success']
assert mobile['data']['result']['result'] == {'quality':'Mobile','msaa':0}
proof = json.loads(data(PREFIX + 'native1160-focused-current-proof.json'))
log = data(PREFIX + proof['log'])
assert proof['result'] == 'Passed' and proof['sourceHash'] == PASSED and proof['sourceUnchanged'] is True
assert proof['playerReady'] is False and proof['nativeMatchCount'] == 0
assert proof['suites'] == 16 and proof['moveCommandTests'] == 26 and proof['plannerCases'] == 107
assert proof['requiredPassMarker'].encode() in log
assert b'[UnitMoveOrderFocusedValidation] result=Passed tests=26' in log
assert b'[AriaSkirmishPlanValidation] result=Passed cases=107' in log
assert b'[ExistingEditorValidation] result=Passed executeMethod=S005IntegratedTechnical1160.RunValidation' in log
assert sha(log) == index['runs'][1]['logSha256']
assert int(data(PREFIX + proof['log'] + '.exit')) == int(data(PREFIX + proof['log'] + '.wrapper-exit')) == 0
assert b'S005IntegratedTechnical1160' in data('/private/tmp/S005IntegratedTechnical1160.cs')
for source in index['sourceSnapshots']:
    assert sha(data(source['snapshot'])) == source['sha256'] == new_manifest['files'][source['productionPath']]
meta = json.loads(data(index['cs1612CorrectionMetadata']))
assert meta['failedSourceHash'] == FAILED and meta['passedSourceHash'] == PASSED
assert meta['rootCorrectedTestSha256'] == 'a8e40c6e7c458e7fa96d8dc0082d4eedda75242f1069e6bcb29826b79ed092f2'
assert meta['failedTestSha256'] == old_manifest['files'][TEST]
assert meta['rootCorrectedTestSha256'] == new_manifest['files'][TEST]
assert sha(data(meta['tinyPatchPath'])) == meta['tinyPatchSha256']
reviewed = json.loads(data(index['reviewedR1Manifest']))
assert sha(data(index['reviewedR1Patch'])) == reviewed['patchSha256']
assert sha(data(index['failedR1TestSnapshot'])) == reviewed['files'][1]['stagedSha256'] == old_manifest['files'][TEST]
assert reviewed['files'][0]['stagedSha256'] == old_manifest['files'][reviewed['files'][0]['path']] == new_manifest['files'][reviewed['files'][0]['path']]

print('[S005Core1160SupplementEvidence] result=Passed runs=2 aliases=28 objects=27 suites=16 moveTests=26 ariaCases=107 normalWins=0 playerReady=0')
