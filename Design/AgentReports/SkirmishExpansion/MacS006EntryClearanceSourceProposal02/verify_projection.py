"""Source/text-only verifier. No C# compiler, SDK, Unity, imports or source writes."""
from pathlib import Path
import difflib
import hashlib
import json
import re
import subprocess

HERE = Path(__file__).resolve().parent
ROOT = next(p for p in HERE.parents if (p / 'Assets').is_dir())
BASE = 'b4405e72ae823c4463a82d81ceb9703ed6d99174'
PREFIX = 'Assets/Game/Scripts/Editor/Skirmish/'
NAMES = ['FacilityPreferences', 'SimultaneousRefinement', 'PilotFacilityRelocation',
         'ReviewedEntryClearance', 'ReviewedEntryClearance.Controls']

def digest(data):
    return hashlib.sha256(data).hexdigest()

def source(name):
    return subprocess.check_output(['git', 'show', BASE + ':' + name], cwd=ROOT)

def apply(text, patch):
    """Strict contextual in-memory application of a single complete unified diff."""
    lines = text.splitlines(True)
    output = []
    cursor = 0
    chunks = patch.splitlines(True)
    assert chunks[0].startswith('--- ') and chunks[1].startswith('+++ ')
    index = 2
    while index < len(chunks):
        match = re.fullmatch(r'@@ -(\d+)(?:,(\d+))? \+(\d+)(?:,(\d+))? @@\n', chunks[index])
        assert match, chunks[index]
        old_start, old_count, new_start, new_count = (
            int(match[1]), int(match[2] or 1), int(match[3]), int(match[4] or 1))
        start = old_start - 1 if old_count else old_start
        assert cursor <= start
        output.extend(lines[cursor:start])
        cursor = start
        index += 1
        consumed = added = 0
        while index < len(chunks) and not chunks[index].startswith('@@ '):
            row = chunks[index]
            assert row[0] in ' +-'
            if row[0] in ' -':
                assert cursor < len(lines) and lines[cursor] == row[1:]
                cursor += 1
                consumed += 1
            if row[0] in ' +':
                output.append(row[1:])
                added += 1
            index += 1
        assert consumed == old_count and added == new_count
        assert len(output) == (new_start - 1 if new_count else new_start) + new_count
    output.extend(lines[cursor:])
    return ''.join(output)

def main():
    pins = json.loads((HERE / 'source-pins.json').read_text())
    assert pins['commit'] == BASE
    forward, inverse = [], []
    for index, part in enumerate(NAMES):
        name = PREFIX + 'SkirmishGroundSourceFitSurvey.' + part + '.cs'
        old = source(name) if index < 3 else b''
        new = (HERE / 'proposed' / (Path(name).name + '.txt')).read_bytes()
        assert b'\r' not in new
        if index < 3:
            pin = pins['files'][index]
            assert pin['path'] == name and pin['base_sha256'] == digest(old)
            assert pin['proposed_sha256'] == digest(new)
        patch = ''.join(difflib.unified_diff(old.decode().splitlines(True), new.decode().splitlines(True),
                      fromfile='a/' + name if old else '/dev/null', tofile='b/' + name, n=3))
        reverse = ''.join(difflib.unified_diff(new.decode().splitlines(True), old.decode().splitlines(True),
                        fromfile='a/' + name, tofile='b/' + name if old else '/dev/null', n=3))
        assert apply(old.decode(), patch).encode() == new
        assert apply(new.decode(), reverse).encode() == old
        forward.append(patch)
        inverse.append(reverse)
        print('PASS exact contextual forward/inverse bytes:', part)
    assert ''.join(forward) == (HERE / 'proposal.patch').read_text()
    assert ''.join(inverse) == (HERE / 'proposal.inverse.patch').read_text()
    assert ''.join(forward[:3]) == (HERE / 'hooks.patch').read_text()
    assert ''.join(inverse[:3]) == (HERE / 'hooks.inverse.patch').read_text()
    print('PASS all four aggregate patch files equal reconstructed patches')
    helper = (HERE / 'proposed/SkirmishGroundSourceFitSurvey.ReviewedEntryClearance.cs.txt').read_text()
    core = (HERE / 'proposed/SkirmishGroundSourceFitSurvey.SimultaneousRefinement.cs.txt').read_text()
    preferences = (HERE / 'proposed/SkirmishGroundSourceFitSurvey.FacilityPreferences.cs.txt').read_text()
    pilot = (HERE / 'proposed/SkirmishGroundSourceFitSurvey.PilotFacilityRelocation.cs.txt').read_text()
    controls = (HERE / 'proposed/SkirmishGroundSourceFitSurvey.ReviewedEntryClearance.Controls.cs.txt').read_text()
    assert 'static JObject reviewedEntryClearanceRequest;' in helper
    assert 'static ReviewedEntryOwner reviewedEntryOwner;' in helper
    assert 'static bool reviewedEntryDerivationActive;' in helper
    assert 'static bool reviewedEntryBaselineCaptureActive;' in helper
    assert helper.count('UseReviewedEntryClearanceRequest(') == 1
    assert helper.count('RefineReviewedEntryTwoPhase(') == 1
    assert 'Entry request cannot begin in pre-final preference search' in preferences
    assert 'strictCandidatePreparation:guard.PrepareImmutable' in helper
    assert 'activePhase="search";search=Phase();guard.CompletePhase(search);guard.EnterReplay();' in helper
    assert 'activePhase="replay";replay=Phase();guard.CompletePhase(replay);' in helper
    assert 'reviewedEntryOwner==null||strictCandidatePreparation!=null' in core
    assert core.index('if(strictCandidatePreparation!=null)', core.index('if(!discovery&&complete)')) < core.index('if(!TryApplyRefinementHulls(')
    assert core.index('if(reviewedEntryOwner!=null)', core.index('if(!discovery&&complete)')) < core.index('try{FinalizeRefinementAuthoring(')
    assert 'entryBaselineExistingGatesPassed' in helper and 'entryBaselineUnqualified' in helper
    assert 'completeFacilities' in helper and 'allActorsInsidePads' in helper
    assert 'facilities.Add(names[i],(JArray)derivedRow[' in helper
    assert 'EntryFields(envelope,"catalogId","pass","discoveryOnly","size","facts","preferenceCandidate","candidateLocalPass")' in helper
    assert 'JToken.DeepEquals(facts,selected)' in helper
    assert 'EntrySha(comparisonJson)==EntryPreFinalLayoutHash&&layoutJson==finalJson' in helper
    assert 'EntrySha(actual)==EntryFinalLayoutHash' in helper
    assert 'JsonUtility.FromJsonOverwrite' not in helper
    assert 'checkedRefusals==21' in controls and 'checkedRefusals==38' in controls
    original = source(PREFIX + 'SkirmishGroundSourceFitSurvey.PilotFacilityRelocation.cs').decode()
    assert pilot.replace('static void ProbePilotNormalFacilityRelocationCore(', 'static void ProbePilotNormalFacilityRelocation(') == original
    # Default inactive preference hook and all non-hook bytes must remain exact.
    original_pref = source(PREFIX + 'SkirmishGroundSourceFitSurvey.FacilityPreferences.cs').decode()
    pref_guard = '            Require(reviewedEntryClearanceRequest==null,"Entry request cannot begin in pre-final preference search; explicit two-phase owner required");\n'
    assert preferences.replace(pref_guard, '') == original_pref
    template = json.loads((HERE / 'request-template.disabled.json').read_text())
    assert template['enabled'] is False and template['nativeLaunchAuthorized'] is False
    assert template['preservedSnapshotSha256'] is None and template['sizeArtifacts'] is None
    witness = json.loads((HERE / 'phase-witness.pending.json').read_text())
    for key in ['actualDerivedLayoutJson', 'actualDerivedStrictSizes', 'actualBaselineExistingGatesPassed', 'actualSearch', 'actualReplay', 'compiler', 'nativeRun']:
        assert witness[key] is None
    print('PASS structural scope/call-order/envelope/hash checks ONLY; not C# behavior')
    manifest_path = HERE / 'manifest.json'
    if manifest_path.exists():
        manifest = json.loads(manifest_path.read_text())
        for item in manifest['files']:
            path = ROOT / item['path']
            payload = path.read_bytes()
            assert len(payload) == item['bytes'] and digest(payload) == item['sha256'], item['path']
        print('PASS manifest payload raw hashes')
    print('CSharpCompile=NOT_RUN; proposed C# refusals=38 NOT_RUN (original21 preserved NOT_RUN)')
    print('UnityNative=NOT_RUN; sourceFit=false; missionAcceptance=false; APK/device=NOT_RUN')

if __name__ == '__main__':
    main()
