"""Exact S002 eighteen-building placement search from captured causal before-inputs.

Native origins, footprints, factory XYZ and first-candidate geometry are assertions.
Mask state transitions, active catalog selection and complete physical startup remain pending.
This executable is separate from the actor/session runner and requires no Unity binaries.
"""
import argparse
import copy
import hashlib
import json
from pathlib import Path
import struct
import stat
import subprocess
import sys
from xml.sax.saxutils import escape
sys.dont_write_bytecode = True
ROOT = Path(__file__).resolve().parents[2]
PROGRAM = 'Tools/CI/SkirmishOfflineSim/StartupBuildingPlacementProgram.cs'
MAXIMUM_FILE_BYTES = 128 * 1024 * 1024
MAXIMUM_ARTIFACT_BYTES = 512 * 1024 * 1024
EXPECTED_POLICY_SHA256 = {'Assets/Game/Scripts/Systems/BuildingRuntimeSpawnCompositionSystemHelper.InitialPlacement.cs': '731015a4126399f6f6a8378558a589d0286234e17cf27eb0c6927544f4379f4c', 'Assets/Game/Scripts/Systems/BuildingPlacementValidationUtilitySystemHelper.cs': 'b2dbd69269a9bbd34e025885b8869ca0ae50a92800fb5c77ac7af33f4056f4d5', 'Assets/Game/Scripts/Systems/BuildingPlacementValidationUtilitySystemHelper.RoadValidation.cs': 'be354fd2f39f1446c4acbdc3c39f532079726aadee74e821bb2761d6faafad22', 'Assets/Game/Scripts/Systems/BuildingPlacementInvalidCellCacheCompositionSystemHelper.RoadValidation.cs': 'b649dd21863b8a70d37ec362bae285d8f7409fd4f77f82cf78b65e87d0284547', 'Assets/Game/Scripts/Systems/BuildingRuntimeProcessingCompositionSystemHelper.cs': '69eeaa2da75e42c7327c30339a0f67e7dd89da84e0c37082ad10ebe05d29f7e8', 'Assets/Game/Scripts/Systems/BuildingRuntimeQueryCompositionSystemHelper.cs': 'f9be3ad78daf09328a8ff5679d365fae16564a5954bc733deb9fcbfe38999bdd', 'Assets/Game/Scripts/Systems/BuildingPlacementGridCameraSystemHelper.cs': 'b987e4b6374bd43c365637d20d1d119b145e732914ba65dd557843806133e3dc', 'Assets/Game/Scripts/Systems/BuildingRunwaySystem.cs': 'ae801035c1fdf2200e9ef824adbeefbc7760373602fd310359c66e672f189e1c', 'Assets/Game/Scripts/Components/UnitFootprintUtility.cs': 'af2205647cd3bf6d99df509af5525840e60bda99659b3e0ffa17bc24e4303599', 'Assets/Game/Scripts/Systems/BuildingPlacementAuthoredBuildingCache.cs': '879e07078701f2ab0c7f0ffd461b59c18c1791957b49672a772c15394f7dc8f9', 'Assets/Game/Scripts/Systems/BuildingPlacementAuthoredRoadCache.cs': '706092eea13e0be6a5085e531c3693aa64fad8a211a501144e2a921ed498bc6b', 'Assets/Game/Scripts/Systems/BuildingBarrierUtilitySystemHelper.cs': '997ab7d420bd5d89d5786a57435ef6c12927486ffc4d1f9054447258c64df98d'}


def need(ok, why):
    if not ok: raise ValueError(why)


def regular_file(path, maximum=MAXIMUM_FILE_BYTES):
    info = path.lstat()
    need(stat.S_ISREG(info.st_mode) and not path.is_symlink() and info.st_size <= maximum, "Bounded regular file required: " + str(path))
    return info.st_size


def sha(path):
    regular_file(path)
    digest = hashlib.sha256()
    with path.open("rb") as stream:
        for block in iter(lambda: stream.read(1024 * 1024), b""): digest.update(block)
    return digest.hexdigest()


def closure(project, pins):
    for relative, expected in pins.items():
        need(sha(project / relative) == expected, 'Pinned placement source changed: ' + relative)


def extract(project, path, signature):
    text = (project / path).read_text()
    need(text.count(signature) == 1, 'Ambiguous native method signature: ' + signature)
    start = text.index(signature); brace = text.index('{', start); depth = 1; end = brace + 1
    # Extraction preserves original text, including interpolation braces. These
    # pinned methods have balanced braces inside their diagnostic interpolations.
    while depth:
        depth += (text[end] == '{') - (text[end] == '}'); end += 1
    return text[start:end]


def build(project, output):
    pins = dict(EXPECTED_POLICY_SHA256)
    pins[PROGRAM] = sha(project / PROGRAM)
    pins['Tools/CI/skirmish_s002_building_placement_replay.py'] = sha(Path(__file__))
    closure(project, pins)
    policy = 'using System; using System.Collections.Generic; using Unity.Entities; using UnityEngine; using Game.Components;\nnamespace Game.Runtime { internal sealed partial class BuildingRuntimeSpawnCompositionSystemHelper {\n'
    policy += extract(project, 'Assets/Game/Scripts/Systems/BuildingRuntimeSpawnCompositionSystemHelper.InitialPlacement.cs', 'public bool TryFindValidInitialBuildingOrigin(')
    # All diagnostic calls are unreachable under the checked normal S002
    # RequirePreferredOrigin=0 path. No search/validation expressions are replaced.
    policy += '\nstatic bool IsTrustPlacementDiagnostic()=>false; static bool IsCitywidePlacementDiagnostic()=>false; static void LogTrustPlacementCells(params object[] x){} static void LogCitywidePlacementCells(params object[] x){} static void SurveyCitywideReserveCandidates(params object[] x){}\n}\npublic sealed partial class BuildingPlacementValidationUtilitySystemHelper {\n'
    for signature in ('public static bool IsFootprintInsideGrid(', 'public static bool HasCachedInvalidCellInFootprint(', 'private static bool IsRuntimeBlockerCell('):
        policy += extract(project, 'Assets/Game/Scripts/Systems/BuildingPlacementValidationUtilitySystemHelper.cs', signature) + '\n'
    policy += '}\ninternal static class BuildingBarrierUtilitySystemHelper {\n' + extract(project, 'Assets/Game/Scripts/Systems/BuildingBarrierUtilitySystemHelper.cs', 'public static bool IsWallGateDefinition(BuildingDefinition definition)') + '\n}\ninternal class BuildingPlacementGridCameraSystemHelper {\n' + extract(project, 'Assets/Game/Scripts/Systems/BuildingPlacementGridCameraSystemHelper.cs', 'public Vector3 GetFootprintCenter(') + '\n}}\n'
    (output / 'ExtractedPolicy.cs').write_text(policy)
    (output / 'NuGet.Config').write_text('<configuration><packageSources><clear /></packageSources></configuration>')
    sources = [project / PROGRAM, output / 'ExtractedPolicy.cs', project / 'Assets/Game/Scripts/Systems/BuildingPlacementValidationUtilitySystemHelper.RoadValidation.cs']
    xml = '<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net8.0</TargetFramework><EnableDefaultCompileItems>false</EnableDefaultCompileItems><NuGetAudit>false</NuGetAudit></PropertyGroup><ItemGroup>'
    xml += ''.join('<Compile Include="' + escape(str(source), {'"': '&quot;'}) + '" />' for source in sources) + '</ItemGroup></Project>'
    (output / 'Placement.csproj').write_text(xml)
    result = subprocess.run(['dotnet', 'build', str(output / 'Placement.csproj'), '--configfile', str(output / 'NuGet.Config'), '--nologo'], capture_output=True, text=True, timeout=120)
    (output / 'build.log').write_text(result.stdout + result.stderr)
    need(result.returncode == 0, 'Building placement compile failed; see build.log')
    closure(project, pins)
    return pins, output / 'bin/Debug/net8.0/Placement.dll'


def invoke(executable, journal, artifacts, output, label, synthetic=False):
    command = ['dotnet', str(executable), str(journal), str(artifacts)]
    if synthetic: command += ['--synthetic-first']
    result = subprocess.run(command, capture_output=True, text=True, timeout=120)
    (output / (label + '.stdout.json')).write_text(result.stdout)
    (output / (label + '.stderr.log')).write_text(result.stderr)
    return result


def protocol_controls(executable, data, artifacts, output, baseline):
    fixtures = output / 'protocol'; fixtures.mkdir()
    mutations = {
        'native-origin-assertion': lambda d: d['transactions'][0]['factory_inputs']['origin_assertion'].__setitem__(0, 1),
        'native-footprint-assertion': lambda d: d['transactions'][0]['placement_footprint_assertion'].__setitem__(0, 1),
        'first-candidate-assertion': lambda d: d['transactions'][0]['original_placement_inputs']['first_validated_candidate_assertion'].__setitem__(0, 1),
        'artifact-hash': lambda d: d['transactions'][0]['masks_before'][0].__setitem__('sha256', '0' * 64),
        'artifact-length': lambda d: d['transactions'][0]['masks_before'][0].__setitem__('bytes', 1),
        'artifact-path-traversal': lambda d: d['transactions'][0]['masks_before'][0].__setitem__('artifact', '../building-startup.json'),
        'missing-original-cache': lambda d: d['transactions'][0].__setitem__('original_placement_cache_inputs_complete', False),
        'unsupported-runway': lambda d: d['transactions'][0]['selected_request_definition'].__setitem__('HasRunway', True),
        'unsupported-rotation': lambda d: d['transactions'][0]['request'].__setitem__('RotateVertical', 1),
        'missing-occupancy': lambda d: d['transactions'][0].pop('pre_placement_live_occupancy'),
        'incomplete-eighteen': lambda d: d['transactions'].pop(),
        'grid-width-bound': lambda d: d['transactions'][0]['grid'].__setitem__('Width', 2049),
        'grid-height-bound': lambda d: d['transactions'][0]['grid'].__setitem__('Height', 1025),
        'factory-xyz-assertion': lambda d: d['transactions'][0]['factory_result_assertion']['components']['LocalTransform']['Position'].__setitem__('y', 1),
    }
    controls = []
    for name, mutation in mutations.items():
        changed = copy.deepcopy(data); mutation(changed); path = fixtures / (name + '.json'); path.write_text(json.dumps(changed))
        result = invoke(executable, path, artifacts, output, name)
        need(result.returncode != 0, 'Protocol control incorrectly accepted: ' + name)
        controls.append(dict(name=name, passed=True))
    changed = copy.deepcopy(data)
    for row in changed['transactions']:
        row['request']['ActualOrigin'] = dict(x=-999999, y=-999999)
        row['request']['ActualFootprint'] = dict(x=999999, y=999999)
        row['request']['BuildingRuntimeId'] = -999999
    path = fixtures / 'pending-result-independence.json'; path.write_text(json.dumps(changed))
    result = invoke(executable, path, artifacts, output, 'pending-result-independence')
    need(result.returncode == 0 and json.loads(result.stdout) == baseline, 'Pending native result slot leaked into derivation')
    controls.append(dict(name='pending-result-independence', passed=True))
    security = output / 'security'; security.mkdir()
    linked = security / 'artifacts'; linked.mkdir()
    descriptors = {}
    def collect(value):
        if isinstance(value, dict):
            if all(k in value for k in ('artifact', 'bytes', 'encoding', 'sha256')): descriptors[value['artifact']] = value
            for nested in value.values(): collect(nested)
        elif isinstance(value, list):
            for nested in value: collect(nested)
    collect(data)
    chosen = data['transactions'][0]['masks_before'][0]['artifact']
    # The selected first descriptor must fail before any later artifact opens.
    (linked / chosen).symlink_to(artifacts / chosen)
    path = security / 'journal.json'; path.write_text(json.dumps(data))
    def rejected(label, journal, root, message):
        result = invoke(executable, journal, root, output, label)
        need(result.returncode != 0 and message in result.stderr, 'File control incorrectly accepted: ' + label)
        controls.append(dict(name=label, passed=True))
    rejected('symlink-artifact', path, linked, 'Bounded regular file required')
    try: artifact_closure(data, linked)
    except ValueError: pass
    else: raise ValueError('Python symlink artifact incorrectly accepted')
    (linked / chosen).unlink()
    with (linked / chosen).open('wb') as stream: stream.truncate(MAXIMUM_FILE_BYTES + 1)
    changed = copy.deepcopy(data)
    for row in changed['transactions']:
        for descriptor in row['masks_before']:
            if descriptor['artifact'] == chosen: descriptor['bytes'] = MAXIMUM_FILE_BYTES + 1
    path.write_text(json.dumps(changed))
    rejected('oversize-artifact-before-read', path, linked, 'Artifact byte bound exceeded')
    try: artifact_closure(changed, linked)
    except ValueError: pass
    else: raise ValueError('Python oversize artifact incorrectly accepted')
    huge = security / 'oversize-journal.json'
    with huge.open('wb') as stream: stream.truncate(MAXIMUM_FILE_BYTES + 1)
    rejected('oversize-journal-before-read', huge, artifacts, 'Bounded regular file required')
    try: regular_file(huge)
    except ValueError: pass
    else: raise ValueError('Python oversize journal incorrectly accepted')
    symlink = security / 'symlink-journal.json'; symlink.symlink_to(path)
    rejected('symlink-journal', symlink, artifacts, 'Bounded regular file required')
    try: regular_file(symlink)
    except ValueError: pass
    else: raise ValueError('Python symlink journal incorrectly accepted')
    return controls


def synthetic_controls(executable, real, output):
    fixtures = output / "synthetic"; fixtures.mkdir()
    def artifact(name,data,encoding):
     (fixtures/name).write_bytes(data);return {'artifact':name,'bytes':len(data),'encoding':encoding,'sha256':hashlib.sha256(data).hexdigest()}
    zero=artifact('zero.u8',bytes(36),'u8')
    def mask(role):return dict(zero,role=role)
    def fixture(entity=None,preferred=[2,2],expected=[2,2],first=None):
     d=copy.deepcopy(real);d['transactions']=d['transactions'][:1];t=d['transactions'][0];
     t.pop('factory_center_inputs',None);t.pop('factory_result_assertion',None);t.pop('placement_result_assertion',None);t['ordinal']=0;t['request']['PreferredOrigin']={'x':preferred[0],'y':preferred[1]};t['grid']['Width']=6;t['grid']['Height']=6
     t['selected_request_definition']['FootprintCells']=[1,1];t['factory_inputs']['selected_definition']['FootprintCells']=[1,1];t['placement_footprint_assertion']=[1,1]
     t['masks_before']=[mask(role) for role in ['blocked_before','grid_roads_before','authored_buildings','authored_roads_and_sidewalks','water','sidewalks']]
     t['existing_runtime_buildings_before']=[]
     t['original_placement_inputs']={'has_invalid_prefix':False,'prefix_width':0,'prefix_height':0,'masks':[mask(role) for role in ['original_authored_buildings','original_authored_roads_and_sidewalks','original_water','original_startup_roads','runtime_blocker_exceptions']],'first_validated_candidate_assertion':first or preferred,'footprint_assertion':[1,1],'effective_rect_assertion':[*(first or preferred),1,1]}
     t['factory_inputs']['origin_assertion']=expected
     occupancy={'schema_version':1,'query':'All(UnitGrid,UnitFootprint);Options=Default','production_query_supplied':False,'order':'native_chunk_then_entity_unsorted','grid_width':6,'grid_height':6,'complete':True,'count':int(entity is not None),'entities':[] if entity is None else [entity]}
     t['pre_placement_live_occupancy']=artifact('occupancy.json',(json.dumps(occupancy)+'\n').encode(),'utf8-json')
     return d
    base={'ordinal':0,'chunk_ordinal':0,'index_in_chunk':0,'entity':{'index':1,'version':1},'has_unit_grid':True,'unit_grid_cell':[2,2],'has_unit_footprint':True,'unit_footprint_size':[1,1],'has_prefab':False,'has_static_grid_blocker':False,'has_runtime_building_combat_tag':False,'has_operation_map_building':False,'has_runtime_building_combat_info':False,'has_unit_health':True,'unit_health':{'current':0,'max':100},'has_operation_map_building_destroyed':False}
    cases=[]
    def run(name,d,accept=True):
     p=fixtures/(name+'.json');p.write_text(json.dumps(d));r=subprocess.run(['dotnet',str(executable),str(p),str(fixtures),'--synthetic-first'],capture_output=True,text=True)
     (fixtures/(name+'.stdout.json')).write_text(r.stdout);(fixtures/(name+'.stderr.log')).write_text(r.stderr)
     if (r.returncode==0)!=accept:raise RuntimeError(name+': '+r.stderr)
     cases.append({'name':name,'passed':True,'expected_accept':accept})
    run('empty-preferred',fixture())
    run('live-dead-health-still-blocks-and-ring-order',fixture(base,expected=[1,1]))
    e=copy.deepcopy(base);e['has_static_grid_blocker']=True;run('static-nonoperation-skipped',fixture(e))
    e=copy.deepcopy(base);e['has_runtime_building_combat_tag']=True;run('runtime-proxy-skipped',fixture(e))
    e=copy.deepcopy(base);e['has_static_grid_blocker']=True;e['has_operation_map_building']=True;run('operation-static-exception-blocks',fixture(e,expected=[1,1]))
    e=copy.deepcopy(base);e['has_runtime_building_combat_tag']=True;e['has_operation_map_building']=True;run('operation-runtime-tag-exception-blocks',fixture(e,expected=[1,1]))
    e=copy.deepcopy(base);e['has_operation_map_building']=True;e['has_runtime_building_combat_info']=True;e['runtime_building_combat_info']={'origin_cell':[0,0],'footprint_cells':[1,1]};run('operation-info-origin-overrides-unit-center',fixture(e))
    e=copy.deepcopy(base);e['unit_footprint_size']=[2,2];run('even-footprint-min-policy',fixture(e,preferred=[1,1],expected=[1,1]))
    e=copy.deepcopy(base);e['unit_footprint_size']=[0,-2];run('footprint-clamp-min-one',fixture(e,expected=[1,1]))
    run('preferred-clamp-before-search',fixture(preferred=[99,99],expected=[5,5],first=[5,5]))
    d=fixture();d['transactions'][0]['existing_runtime_buildings_before']=[{'runtime_id':77,'is_destroyed':True,'origin':[2,2],'definition':d['transactions'][0]['selected_request_definition']}];run('destroyed-runtime-building-skipped',d)
    d=fixture();d['transactions'][0]['existing_runtime_buildings_before']=[{'runtime_id':77,'is_destroyed':False,'origin':[2,2],'definition':d['transactions'][0]['selected_request_definition']}];d['transactions'][0]['factory_inputs']['origin_assertion']=[1,1];run('live-runtime-building-blocks',d)
    # Prefix-enabled route must short-circuit before original validation, while fallback
    # blockers honor runtime exceptions without permitting roads.
    import struct
    d=fixture(expected=[1,1],first=[1,1]);prefix=[0]*49
    for y in range(1,7):
     for x in range(1,7):prefix[y*7+x]=int(x>2 and y>2)
    o=d['transactions'][0]['original_placement_inputs'];o.update(has_invalid_prefix=True,prefix_width=7,prefix_height=7,invalid_prefix=artifact('prefix.i32',struct.pack('<49i',*prefix),'i32-le'));run('prefix-cached-short-circuit',d)
    for name,exception,road,expected in [('static-blocker-reject',False,False,[1,1]),('runtime-blocker-exception',True,False,[2,2]),('roads-still-reject-runtime-exception',True,True,[1,1])]:
     d=fixture(expected=expected);bits=bytearray(36);bits[2*6+2]=1;blocked=artifact('blocked.u8',bytes(bits),'u8')
     for a in d['transactions'][0]['masks_before']:
      if a['role']=='blocked_before' or road and a['role']=='grid_roads_before':a.update(blocked)
     if exception:
      for a in d['transactions'][0]['original_placement_inputs']['masks']:
       if a['role']=='runtime_blocker_exceptions':a.update(blocked)
     run(name,d)
    return cases


def artifact_closure(data, root):
    pins = {}
    total_bytes = 0
    def visit(value):
        nonlocal total_bytes
        if isinstance(value, dict):
            if all(key in value for key in ('artifact', 'bytes', 'encoding', 'sha256')):
                name = value['artifact']
                need(isinstance(name, str) and name not in ('.', '..') and '/' not in name and '\\' not in name and Path(name).name == name, 'Artifact basename required')
                path = root / name
                need(isinstance(value['bytes'], int) and 0 <= value['bytes'] <= MAXIMUM_FILE_BYTES, 'Artifact byte bound exceeded')
                need(path.parent.resolve() == root.resolve(), 'Artifact escaped root')
                size = regular_file(path)
                if name not in pins:
                    total_bytes += size
                    need(total_bytes <= MAXIMUM_ARTIFACT_BYTES, 'Total artifact byte bound exceeded')
                need(size == value['bytes'] and sha(path) == value['sha256'], 'Artifact closure differs: ' + name)
                if name in pins: need(pins[name] == value['sha256'], 'Conflicting artifact identity')
                pins[name] = value['sha256']
            for nested in value.values(): visit(nested)
        elif isinstance(value, list):
            for nested in value: visit(nested)
    visit(data)
    return pins


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--capture', required=True, type=Path)
    parser.add_argument('--output-dir', required=True, type=Path)
    parser.add_argument('--project', type=Path, default=ROOT)
    parser.add_argument('--controls', action='store_true')
    args = parser.parse_args()
    regular_file(args.capture.absolute())
    project, capture, output = args.project.resolve(), args.capture.resolve(), args.output_dir.resolve()
    need(capture.is_file(), 'Sealed physical building journal required')
    need(not output.exists(), 'Fresh output directory required; preserve earlier evidence')
    output.mkdir(parents=True)
    capture_hash = sha(capture)
    data = json.loads(capture.read_text())
    artifacts = artifact_closure(data, capture.parent)
    pins, executable = build(project, output)
    result = invoke(executable, capture, capture.parent, output, 'placements')
    need(result.returncode == 0, 'Eighteen strict placement assertions failed; see placements.stderr.log')
    baseline = json.loads(result.stdout)
    controls = protocol_controls(executable, data, capture.parent, output, baseline) if args.controls else []
    synthetic = synthetic_controls(executable, data, output) if args.controls else []
    closure(project, pins)
    need(sha(capture) == capture_hash, 'Capture changed while replay ran')
    need(artifact_closure(data, capture.parent) == artifacts, 'Artifacts changed while replay ran')
    report = dict(passed=True, scope=baseline['scope'], capture=str(capture), capture_sha256=capture_hash,
        placements=baseline['placements'], source_pins=pins, artifact_sha256=artifacts, protocol_controls=controls, synthetic_controls=synthetic,
        active_catalog_selection_derived=False, mask_transitions_derived=False,
        final_post_roster_building_grounding_derived=False, full_physical_startup_derived=False,
        full_mission_initialized=False, player_ready=False)
    (output / 'result.json').write_text(json.dumps(report, indent=2) + '\n')
    print(json.dumps(dict(passed=True, placements=len(report['placements']), protocol_controls=len(controls),
        synthetic_controls=len(synthetic), result=str(output / 'result.json'))))


if __name__ == '__main__':
    main()
