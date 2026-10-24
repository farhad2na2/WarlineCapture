"""Candidate scalar defaults for S002's eight existing source building configs.

Active native definition selection, physical placement, grants and versions are
not inferred. Observed r3 building component values are assertions only.
"""
import argparse
import copy
import hashlib
import json
import math
import os
import stat
from pathlib import Path
import re
import struct
import subprocess
import sys
from xml.sax.saxutils import escape
sys.dont_write_bytecode = True
ROOT = Path(__file__).resolve().parents[2]
CONFIG_CLASS = 'Assets/Game/Scripts/Configs/GameplayConfigModels.cs'
AUTHORING = 'Assets/Game/Scripts/Authorings/BuildingDefinitionAuthoring.cs'
CONFIG_SCRIPT = 'Assets/Game/Scripts/Configs/Prefabs/BuildingDefinitionAuthoringPrefabConfigAsset.cs'
BASE = 'Assets/Game/Prefabs/Buildings/Building.prefab'
KERNEL = 'Assets/Game/Scripts/Runtime/Skirmish/SkirmishActorInitializationKernel.cs'
CONTRACTS = 'Assets/Game/Scripts/Skirmish/Contracts/SkirmishRosterContracts.cs'
CATALOG = 'Assets/Game/Scripts/Configs/Skirmish/SkirmishRoleOverlayCatalog.cs'
FACILITY = 'Assets/Game/Scripts/Runtime/Skirmish/SkirmishFacilityEconomyProfile.cs'
INTERVAL = 'Assets/Game/Scripts/Runtime/Skirmish/SkirmishExpandedEngagementService.cs'
HELPER = 'Tools/CI/SkirmishOfflineSim/StartupBuildingDefaultsProgram.cs'
PAIRS = {
 'Building_Ammunition_Depot': ('Assets/Game/Prefabs/Buildings/Building_Ammunition_Depot.prefab','Assets/Game/Configs/Prefabs/Prefab_BuildingDefinition_Ammunition_Depot_Config.asset'),
 'Building_Barrack': ('Assets/Game/Prefabs/Buildings/Building_Barrack.prefab','Assets/Game/Configs/Prefabs/Prefab_BuildingDefinition_Building_Barrack_Config.asset'),
 'Building_Fuel_Bladder': ('Assets/Game/Prefabs/Buildings/Building_Fuel_Bladder.prefab','Assets/Game/Configs/Prefabs/Prefab_BuildingDefinition_Fuel_Bladder_Config.asset'),
 'Building_GroundStaging': ('Assets/Game/Prefabs/Skirmish/Building_GroundStaging.prefab','Assets/Game/Configs/Prefabs/Prefab_BuildingDefinition_GroundStaging_Config.asset'),
 'Building_GuardTower': ('Assets/Game/Prefabs/Buildings/Building_GuardTower.prefab','Assets/Game/Configs/Prefabs/Prefab_BuildingDefinition_GuardTower_Config.asset'),
 'Building_OilPump': ('Assets/Game/Prefabs/Buildings/Building_OilPump.prefab','Assets/Game/Configs/Prefabs/Prefab_BuildingDefinition_OilPump_Config.asset'),
 'Building_Refinery': ('Assets/Game/Prefabs/Buildings/Building_Refinery.prefab','Assets/Game/Configs/Prefabs/Prefab_BuildingDefinition_OilRefinery_Config.asset'),
 'Building_Satelite_Dish': ('Assets/Game/Prefabs/Buildings/Building_Satelite_Dish.prefab','Assets/Game/Configs/Prefabs/Prefab_BuildingDefinition_Building_Satelite_Dish_Config.asset'),
}
FIELDS = ('maxHealth oilStorageCapacity fuelStorageCapacity oilBarrelsPerDay fuelBarrelsPerDay materialFabricationEnabled materialFabricationOilConsumedPerCycle materialFabricationMaterialsOutputPerCycle materialFabricationCycleDurationSeconds materialFabricationOutputCapacityPolicy canAttack maxConcurrentAttacks attackRange attackCooldownSeconds attackDamage attackTraceColor attackTraceWidth attackTraceScrollSpeed attackTraceDashDensity attackTraceVisibleSeconds attackTracerEveryNthShot').split()
FLOAT_FIELDS = set(('Range CooldownSeconds OilBarrelsPerDay FuelBarrelsPerDay TraceWidth TraceScrollSpeed TraceDashDensity TraceVisibleSeconds x y z w').split())


def need(ok, message):
    if not ok: raise ValueError(message)


def sha(value):
    return hashlib.sha256(value).hexdigest()


class Sources:
    def __init__(self, project=ROOT, overrides=None):
        self.project, self.overrides, self.pins = Path(project).resolve(), overrides or {}, {}
    def read(self, name):
        path = self.project / name
        raw = path.read_bytes()
        need(not raw.startswith(b'version https://git-lfs'), 'LFS pointer source unavailable: ' + name)
        text = self.overrides.get(name, raw.decode())
        need(not text.startswith('version https://git-lfs'), 'LFS pointer source unavailable: ' + name)
        self.pins[name] = dict(sha256=sha(raw), used_sha256=sha(text.encode()), override=name in self.overrides)
        return text
    def guid(self, path):
        matches = re.findall(r'^guid: ([0-9a-f]{32})$', self.read(path + '.meta'), re.M)
        need(len(matches) == 1, 'Exact source/meta GUID required: ' + path)
        return matches[0]


def number(text, kind):
    if kind == 'bool':
        need(text in ('0', '1', 'true', 'false'), 'Unsupported bool default')
        return text in ('1', 'true')
    if kind in ('int', 'MaterialFabricationOutputCapacityPolicyCode'):
        need(re.fullmatch(r'-?[0-9]+', text) is not None, 'Unsupported integer/enum default')
        value = int(text); need(-2147483648 <= value <= 2147483647, 'Integer config outside int32'); return value
    if kind == 'Color':
        match = re.fullmatch(r'new\(([^)]+)\)', text)
        if match: parts = match[1].split(',')
        else:
            match = re.fullmatch(r'\{r: ([^,]+), g: ([^,]+), b: ([^,]+), a: ([^}]+)\}', text)
            need(match is not None, 'Unsupported source color default'); parts = match.groups()
        need(len(parts) == 4, 'Exact source color required')
        return [number(p.strip(), 'float') for p in parts]
    need(kind == 'float' and re.fullmatch(r'-?(?:[0-9]+(?:\.[0-9]*)?|\.[0-9]+)(?:[eE][+-]?[0-9]+)?f?', text), 'Unsupported scalar default expression')
    value = struct.unpack('<f', struct.pack('<f', float(text.rstrip('f'))))[0]
    need(math.isfinite(value), 'Nonfinite config scalar'); return value


def defaults(sources):
    body = sources.read(CONFIG_CLASS).split('public class BuildingDefinitionAuthoringConfig : ScriptableObject', 1)
    need(len(body) == 2, 'Building config class missing')
    declarations = re.findall(r'private\s+(int|float|bool|Color|MaterialFabricationOutputCapacityPolicyCode)\s+(\w+)\s*(?:=\s*([^;]+))?;', body[1].split('private void OnValidate()', 1)[0])
    result = {}
    for kind, name, initializer in declarations:
        need(name not in result, 'Ambiguous config field declaration')
        # Absent initializer is the C# language-defined typed zero, proved by the
        # exact field declaration; no inferred native answer/default is consulted.
        literal = initializer.strip() if initializer else ('false' if kind == 'bool' else '0')
        result[name] = (kind, literal, number(literal, kind))
    need(all(field in result for field in FIELDS), 'Missing authoritative C# config field/default declaration')
    return result


def config_inputs(sources):
    declared = defaults(sources)
    script_guid, authoring_guid, base_guid = sources.guid(CONFIG_SCRIPT), sources.guid(AUTHORING), sources.guid(BASE)
    script = sources.read(CONFIG_SCRIPT); sources.read(AUTHORING)
    need(re.search(r'sealed class BuildingDefinitionAuthoringPrefabConfigAsset\s*:\s*BuildingDefinitionAuthoringConfig\s*\{\s*\}',script) is not None,'Unsupported config script inheritance/body')
    base = sources.read(BASE)
    base_docs = re.split(r'(?m)^--- !u!114 &', base)[1:]
    authoring_ids = [d.split('\n', 1)[0] for d in base_docs if 'guid: ' + authoring_guid + ',' in d]
    need(len(authoring_ids) == 1 and authoring_ids[0].isdigit(), 'Exact base BuildingDefinitionAuthoring component required')
    result, provenance, config_guids, prefab_guids = [], [], set(), set()
    for key, (prefab, config) in PAIRS.items():
        prefab_guid, config_guid = sources.guid(prefab), sources.guid(config)
        need(prefab_guid not in prefab_guids and config_guid not in config_guids, 'Ambiguous candidate prefab/config GUID')
        prefab_guids.add(prefab_guid); config_guids.add(config_guid)
        text, serialized = sources.read(prefab), sources.read(config)
        need(serialized.count('--- !u!114 &11400000') == 1 and re.findall(r'm_Script: \{fileID: 11500000, guid: ([0-9a-f]{32}), type: 3\}', serialized) == [script_guid], 'Config script identity differs: ' + config)
        direct = re.findall(r'^  config: \{fileID: 11400000, guid: ([0-9a-f]{32}), type: 2\}', text, re.M)
        variant = re.findall(r'- target: \{fileID: ([0-9]+), guid: ([0-9a-f]{32}), type: 3\}\s+propertyPath: config\s+value:[^\n]*\n\s*objectReference: \{fileID: 11400000, guid: ([0-9a-f]{32}), type: 2\}', text)
        if key == 'Building_GroundStaging':
            need(len(re.findall(r'^  config:',text,re.M)) == 1 and not re.search(r'propertyPath: config(?:\s|$)',text) and direct == [config_guid] and not variant and text.count('guid: ' + authoring_guid + ',') == 1, 'Unsupported direct building authoring/config identity')
        else:
            need(not direct and len(re.findall(r'propertyPath: config(?:\s|$)',text)) == 1 and variant == [(authoring_ids[0], base_guid, config_guid)], 'Unsupported/ambiguous building prefab variant config: ' + key)
            need('m_SourcePrefab: {fileID: 100100000, guid: ' + base_guid + ', type: 3}' in text, 'Unsupported variant base')
        values, fields = {}, {}
        for name in FIELDS:
            kind, literal, default = declared[name]
            matches = re.findall(r'^  ' + re.escape(name) + r':([^\n]*)$', serialized, re.M)
            need(len(matches) <= 1, 'Ambiguous serialized config scalar: ' + name)
            values[name] = number(matches[0].strip(), kind) if matches else default
            fields[name] = dict(origin='serialized_config' if matches else 'CSharp_field_default', declaration_type=kind, default_literal=literal if not matches else None)
        result.append(dict(key=key, values=values))
        provenance.append(dict(key=key,prefab=prefab,prefab_guid=prefab_guid,config=config,config_guid=config_guid,config_script_guid=script_guid,authoring_script_guid=authoring_guid,fields=fields,active_selection_proven=False))
    return result, provenance


def source_constants(sources, size):
    text = sources.read(FACILITY)
    names = ('oilRate','materialInput','materialOutput','fuelInput','fuelOutput')
    initial = re.findall(r'int oilRate = (\d+), materialInput = (\d+), materialOutput = (\d+), fuelInput = (\d+), fuelOutput = (\d+);', text)
    need(len(initial) == 1, 'Exact facility default rates missing')
    rates = dict(zip(names, map(int, initial[0])))
    need(size in ('Standard','War','LargeWar'), 'Unsupported S002 facility size')
    if size != 'Standard':
        match = re.findall(r'if \(session.SizeId == SkirmishSizeId\.' + size + r'\)\s*\{ oilRate = (\d+); materialInput = (\d+); materialOutput = (\d+); fuelInput = (\d+); fuelOutput = (\d+); \}', text)
        need(len(match) == 1, 'Exact facility size rates missing'); rates = dict(zip(names, map(int, match[0])))
    interval = re.findall(r'public const float ShotIntervalSeconds\s*=\s*([0-9]+(?:\.[0-9]+)?f)\s*;', sources.read(INTERVAL))
    need(len(interval) == 1, 'Exact shared shot interval missing')
    for contract in ('PeriodSeconds = 60', 'FuelBatchSeconds = 30', 'recipe.CycleDurationSeconds = 30'):
        need(contract in text, 'Unsupported facility period/recipe source contract')
    catalog = sources.read(CATALOG); methods = []
    for name in ('BarracksStructure','WatchtowerStructure','GroundStagingStructure'):
        match = re.findall(r'public static SkirmishRoleOverlay ' + name + r'\(\) =>\s*new SkirmishRoleOverlay\s*\{[^{}]+\};', catalog)
        need(len(match) == 1, 'Exact supported source building overlay missing: ' + name); methods.append(match[0])
    return rates, 'internal static class CandidateBuildingOverlays { ' + '\n'.join(methods) + '}\ninternal static class CandidateBuildingConstants { internal const float ShotIntervalSeconds=' + interval[0] + '; }'


def derive(capture, output, sources):
    configs, provenance = config_inputs(sources)
    rates, generated = source_constants(sources, capture['size'])
    setup = capture['resolved_setup']; need(setup['CatalogId'] == 'S002' and len(setup['Structures']) == 18, 'Exact authored eighteen-structure S002 candidate required')
    output.mkdir(parents=True, exist_ok=False)
    source = output / 'SourceBuildingOverlays.cs'; source.write_text('using Game.Skirmish.Contracts;\n' + generated)
    inp = output / 'input.json'; inp.write_text(json.dumps(dict(configs=configs,facility_rates=rates,structures=setup['Structures'])))
    for path in (KERNEL, CONTRACTS, HELPER, 'Assets/Game/Scripts/Systems/BuildingRuntimeEntityCompositionSystemHelper.cs', 'Assets/Game/Scripts/Systems/BuildingDefinitionPrefabSystemHelper.cs', 'Assets/Game/Scripts/Composition/BuildingDefinitionAuthoringMetadataPrefabSystemHelper.cs', 'Assets/Game/Scripts/Runtime/Skirmish/SkirmishRosterProjectionSystem.cs', 'Assets/Game/Scripts/Runtime/Skirmish/SkirmishSharedCombatBinding.cs', 'Assets/Game/Scripts/Components/MaterialFabricationComponents.cs'):
        sources.read(path)
    recipe_source = sources.read('Assets/Game/Scripts/Components/MaterialFabricationComponents.cs')
    need(re.search(r'enum MaterialFabricationOutputCapacityPolicyCode : byte\s*\{\s*RequireFullCycleCapacity = 0\s*\}',recipe_source) is not None, 'Unsupported fabrication capacity-policy wire value')
    files = [sources.project / p for p in (KERNEL, CONTRACTS, HELPER)] + [source]
    items = ''.join('<Compile Include="' + escape(str(p), {'"':'&quot;'}) + '" />' for p in files)
    (output / 'Defaults.csproj').write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net8.0</TargetFramework><EnableDefaultCompileItems>false</EnableDefaultCompileItems><NuGetAudit>false</NuGetAudit></PropertyGroup><ItemGroup>' + items + '</ItemGroup></Project>')
    (output / 'NuGet.Config').write_text('<configuration><packageSources><clear /></packageSources></configuration>')
    proc = subprocess.run(['rtk','proxy','dotnet','build',str(output/'Defaults.csproj'),'--configfile',str(output/'NuGet.Config'),'-o',str(output/'bin')],capture_output=True,text=True,timeout=120)
    (output/'build.log').write_text(proc.stdout+proc.stderr); need(proc.returncode == 0, 'Building scalar helper build failed')
    proc = subprocess.run(['rtk','proxy','dotnet',str(output/'bin/Defaults.dll'),str(inp)],capture_output=True,text=True,timeout=120)
    (output/'derive.log').write_text(proc.stdout+proc.stderr); need(proc.returncode == 0, 'Building scalar derivation failed: ' + proc.stdout)
    result = json.loads(proc.stdout)
    result.update(source_provenance=provenance,source_pins=sources.pins,config_defaults_used=True,physical_startup_parity=False,active_definition_selection_proven=False)
    return result


def compare(derived, capture):
    actual = {a['stable_id']:a for a in capture['actors'] if a['is_structure']}
    need(len(actual) == len(derived['structures']) == 18, 'Complete eighteen-building assertion set required')
    def same(expected, observed, path):
        if expected is None: need(observed is None, path + ': component presence differs')
        elif isinstance(expected, dict):
            need(isinstance(observed, dict), path + ': object absent')
            for key,value in expected.items(): need(key in observed,path+': field absent '+key); same(value,observed[key],path+'.'+key)
        elif path.rsplit('.',1)[-1] in FLOAT_FIELDS:
            need(struct.pack('<f',expected) == struct.pack('<f',observed),path+': float32 bits differ')
        else: need(expected == observed,path+': scalar differs')
    for row in derived['structures']:
        need(row['id'] in actual,'Native building ID absent'); a = actual[row['id']]
        need(a['source_key'] == row['source_key'] and a['faction'] == row['faction'],'Candidate source-key/faction assertion differs')
        same(row['health'],a['components']['UnitHealth'],row['id']+'.health')
        same(row['storage'],a['building_storage'],row['id']+'.storage')
        same(row['unit_attack'],a['components']['UnitAttack'],row['id']+'.unit_attack')
        same(row['building_weapon'],a['building_weapon'],row['id']+'.building_weapon')
        same(row['target_policy'],a['target_policy'],row['id']+'.target_policy')
        need((row['fabrication_recipe'] is not None) == ('Game.Components.MaterialFabricationComponent' in a['component_types']),'Candidate fabrication presence differs')
    return dict(actors=18,health_caps_rates_weapon_policy_assertions=True,fabrication_presence_asserted=True,fabrication_recipe_fields_observed=False,active_definition_selection_proven=False,physical_startup_parity=False)


def controls(derived, capture, output):
    output.mkdir();checks=[]
    def check(label, action, error):
        directory=output/label;directory.mkdir()
        try:
            action(directory)
        except ValueError as exc:
            (directory/'error.log').write_text(str(exc)+'\n')
            need(error in str(exc),'Wrong control failure '+label+': '+str(exc))
            checks.append(dict(label=label,passed=True,expected_error=error,error=str(exc)));return
        raise ValueError('Control unexpectedly passed: '+label)
    def observed(label, change, error):
        def action(directory):
            bad=copy.deepcopy(capture);change(bad);compare(derived,bad)
        check(label,action,error)
    def actor(data,key):
        return next(a for a in data['actors'] if a['is_structure'] and a['source_key']==key)
    observed('observed-health',lambda d:actor(d,'Building_OilPump')['components']['UnitHealth'].update(Current=651),'health.Current')
    observed('observed-capacity',lambda d:actor(d,'Building_OilPump')['building_storage'].update(OilStorageCapacity=201),'storage.OilStorageCapacity')
    observed('observed-rate',lambda d:actor(d,'Building_OilPump')['building_storage'].update(OilBarrelsPerDay=121),'storage.OilBarrelsPerDay')
    observed('observed-weapon',lambda d:actor(d,'Building_GuardTower')['building_weapon'].update(Damage=11),'building_weapon.Damage')
    observed('observed-source-key',lambda d:actor(d,'Building_OilPump').update(source_key='native_answer_cannot_select_config'),'source-key/faction')
    observed('observed-fabrication-presence',lambda d:actor(d,'Building_Ammunition_Depot')['component_types'].remove('Game.Components.MaterialFabricationComponent'),'fabrication presence')
    def source(label,path,old,new,error):
        original=(ROOT/path).read_text();need(old in original,'Control fixture source missing '+label)
        text=original.replace(old,new,1)
        def action(directory):
            (directory/'source-override.txt').write_text(text)
            candidate=derive(capture,directory/'derive',Sources(overrides={path:text}));compare(candidate,capture)
        check(label,action,error)
    oil=PAIRS['Building_OilPump'][1]
    source('source-health-input',oil,'maxHealth: 650','maxHealth: 651','health.Current')
    source('blank-serialized-value',oil,'maxHealth: 650','maxHealth:','Unsupported integer')
    source('source-capacity-input',oil,'oilStorageCapacity: 200','oilStorageCapacity: 201','storage.OilStorageCapacity')
    source('source-facility-rate-input',FACILITY,'int oilRate = 120','int oilRate = 121','storage.OilBarrelsPerDay')
    source('source-overlay-input',CATALOG,'MaxHealth = 800','MaxHealth = 801','health.Current')
    source('missing-default-declaration',CONFIG_CLASS,'private int maxHealth = 500','private int unsupportedHealth = 500','Missing authoritative C#')
    source('unsupported-default-expression',CONFIG_CLASS,'private int maxHealth = 500','private int maxHealth = UnknownFactory()','Unsupported integer')
    source('wrong-config-script',oil,'guid: a2010000000000000000000000000004','guid: 00000000000000000000000000000000','Config script identity')
    prefab=PAIRS['Building_OilPump'][0];text=(ROOT/prefab).read_text();match=re.search(r'- target: \{fileID: [0-9]+, guid: [0-9a-f]{32}, type: 3\}\s+propertyPath: config\s+value:[^\n]*\n\s*objectReference: \{fileID: 11400000, guid: [0-9a-f]{32}, type: 2\}',text)
    need(match is not None,'Supported variant control absent')
    source('ambiguous-variant-config',prefab,match[0],match[0]+'\n    '+match[0],'Unsupported/ambiguous')
    source('unsupported-variant-target',prefab,match[0],re.sub(r'fileID: [0-9]+','fileID: 999',match[0],count=1),'Unsupported/ambiguous')
    source('missing-authoring-script-chain',AUTHORING+'.meta','guid: 8c3c1b346ef24d7ea4a0cb6f65d6c1c1','guid: 00000000000000000000000000000000','Exact base BuildingDefinitionAuthoring')
    def lfs(directory):
        config_inputs(Sources(overrides={oil:'version https://git-lfs.github.com/spec/v1\n'}))
    check('unavailable-lfs-source',lfs,'LFS pointer source unavailable')
    def clamped(directory):
        guard=PAIRS['Building_GuardTower'][1]
        overrides={oil:(ROOT/oil).read_text().replace('maxHealth: 650','maxHealth: -5').replace('oilStorageCapacity: 200','oilStorageCapacity: -5'),guard:(ROOT/guard).read_text().replace('attackTraceWidth: 0.14','attackTraceWidth: -1').replace('maxConcurrentAttacks: 1','maxConcurrentAttacks: 9')}
        candidate=derive(capture,directory/'derive',Sources(overrides=overrides));by_key={v['source_key']:v for v in candidate['structures']}
        need(by_key['Building_OilPump']['health']['Max']==1 and by_key['Building_OilPump']['storage']['OilStorageCapacity']==0,'Factory integer clamp control failed')
        weapon=by_key['Building_GuardTower']['building_weapon']
        need(struct.pack('<f',weapon['TraceWidth'])==struct.pack('<f',.01) and weapon['MaxConcurrentAttacks']==4,'Factory weapon clamp control failed')
        compare(candidate,capture)
    check('source-boundary-clamps',clamped,'health.Current')
    (output/'result.json').write_text(json.dumps(dict(result='Passed',controls=checks,physical_startup_parity=False),indent=2)+'\n')
    return checks

def unique_object(pairs):
    result={}
    for key,value in pairs:
        need(key not in result,'Duplicate capture JSON key: '+key);result[key]=value
    return result

def read_capture(path):
    """Reject special files before open; bound reads even if a regular file grows."""
    path = Path(path)
    maximum = 128 * 1024 * 1024
    before = path.lstat()
    need(stat.S_ISREG(before.st_mode) and not stat.S_ISLNK(before.st_mode),
         'Capture must be a non-symlink regular file')
    need(0 <= before.st_size <= maximum, 'Capture exceeds128MiB')
    # O_NONBLOCK prevents a racing replacement FIFO from blocking open, and
    # O_NOFOLLOW prevents following a replacement symlink. fstat rejects any
    # replacement/special file before any bytes are read.
    descriptor = os.open(path, os.O_RDONLY | os.O_NOFOLLOW | os.O_NONBLOCK)
    with os.fdopen(descriptor, 'rb') as stream:
        opened = os.fstat(stream.fileno())
        need(stat.S_ISREG(opened.st_mode) and
             (opened.st_dev, opened.st_ino) == (before.st_dev, before.st_ino),
             'Capture regular-file identity changed before read')
        need(0 <= opened.st_size <= maximum, 'Capture exceeds128MiB')
        raw = stream.read(maximum + 1)
        need(len(raw) <= maximum, 'Capture exceeds128MiB during bounded read')
        after = os.fstat(stream.fileno())
        need(after.st_size == opened.st_size == len(raw), 'Capture size changed during bounded read')
        return raw


def main():
    parser=argparse.ArgumentParser(description=__doc__);parser.add_argument('--capture',type=Path,required=True);parser.add_argument('--output',type=Path,required=True);parser.add_argument('--controls',action='store_true');args=parser.parse_args()
    output=args.output.resolve();output.mkdir(parents=True,exist_ok=False);capture_raw=read_capture(args.capture);capture_hash=sha(capture_raw);capture=json.loads(capture_raw,object_pairs_hook=unique_object);sources=Sources();sources.read('Tools/CI/skirmish_s002_building_defaults.py');result=derive(capture,output/'derive',sources);assertions=compare(result,capture)
    checks=controls(result,capture,output/'controls') if args.controls else []
    result.update(result='PassedCandidateScalarAssertions',controls=checks,assertions=assertions,capture_sha256=capture_hash,full_mission_initialized=False,player_ready=False)
    for name,pin in sources.pins.items():need(sha((ROOT/name).read_bytes())==pin['sha256'],'Source drift after scalar derivation: ' + name)
    need(sha(read_capture(args.capture))==capture_hash,'Capture drift after scalar assertions')
    (output/'result.json').write_text(json.dumps(result,indent=2)+'\n');print('[S002CandidateBuildingDefaults] result=Passed actors=18 active_selection_proven=false physical_startup_parity=false')
    return 0
if __name__=='__main__':raise SystemExit(main())
