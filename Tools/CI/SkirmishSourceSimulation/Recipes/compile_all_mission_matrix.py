#!/usr/bin/env python3
"""Run the actual shared C# setup-matrix parser for all authored rows."""
import argparse
import csv
import hashlib
import io
import json
import os
import subprocess
from pathlib import Path

ROOT = Path(__file__).resolve().parents[4]
BASE = ROOT / 'T Logs/SkirmishProduction/s003-actual-importer-current-fixture-20261105-T7'
BUILD = ROOT / 'T Logs/SkirmishProduction/s003-current-prefab-import-build-20261104-T1/build.json'
MATRIX = ROOT / 'Assets/Game/Resources/SkirmishExpansion/InitialSetupMatrix.csv'
CATALOG = ROOT / 'Assets/Game/Resources/SkirmishBattleCatalog.asset'
INVENTORY = ROOT / 'T Logs/SkirmishProduction/all120-source-config-inventory-20261105-T2/inventory.json'
ORIGINAL_CONTROL = ROOT / 'Tools/CI/SkirmishSourceSimulation/Tests/S003RefreshedPrefabImportControl.cs'
MATRIX_OWNER = ROOT / 'Assets/Game/Scripts/Configs/Skirmish/SkirmishSetupMatrixTable.cs'
FIXTURE = r'''using System;using System.IO;using System.Collections.Generic;using Game.Configs;using Game.Skirmish.Contracts;using Newtonsoft.Json;using Newtonsoft.Json.Linq;
internal static class AllMissionMatrixOwnerControl {
 static void Main(string[] args){string text=File.ReadAllText(args[0]);
  if(!SkirmishSetupMatrixTable.TryLoadFromText(text,out var rows,out var error))throw new Exception("Original parser rejected matrix: "+error);
  if(rows.Count!=360)throw new Exception("Expected 360 original typed rows");
  var seen=new HashSet<string>();foreach(var row in rows){string key=row.CatalogId+":"+row.SizeId;if(!seen.Add(key))throw new Exception("Duplicate matrix identity "+key);}
  var sizes=new[]{SkirmishSizeId.Standard,SkirmishSizeId.War,SkirmishSizeId.LargeWar};
  for(int i=1;i<=120;i++){string id="S"+i.ToString("D3");foreach(var size in sizes)if(!SkirmishSetupMatrixTable.TryFind(rows,id,size,out var selected)||selected==null||selected.CatalogId!=id||selected.SizeId!=size)throw new Exception("Missing original typed row "+id+":"+size);}
  if(SkirmishSetupMatrixTable.TryLoadFromText("",out _,out _)||SkirmishSetupMatrixTable.TryLoadFromText("header\nS006,Standard",out _,out _))throw new Exception("Original invalid-data rejection failed");
  var exported=new JArray();foreach(var row in rows){var data=JObject.FromObject(row);data["SourceCodes"]=new JObject{["size_id"]=SkirmishIdParsing.ToCode(row.SizeId),["army_profile"]=SkirmishIdParsing.ToCode(row.ArmyProfile),["start_profile"]=SkirmishIdParsing.ToCode(row.StartProfile),["objective_id"]=SkirmishObjectiveIds.ToCode(row.ObjectiveKind)};exported.Add(data);}
  var report=new JObject{["kind"]="OriginalCSharpAllMissionSetupMatrix",["status"]="ParsedSourceDataOnly",["row_count"]=rows.Count,["runtime_admission"]=false,["mission_ready"]=false,["rows"]=exported};
  File.WriteAllText(args[1],report.ToString(Formatting.Indented));Console.WriteLine("[AllMissionMatrixOwner] result=Passed missions=120 rows=360 negatives=2 admission=0 readiness=0");}}
'''


def sha(path):
    h = hashlib.sha256()
    with path.open('rb') as f:
        for part in iter(lambda: f.read(1024 * 1024), b''):
            h.update(part)
    return h.hexdigest()


def main():
    p = argparse.ArgumentParser(description=__doc__); p.add_argument('--output', type=Path, required=True)
    out = p.parse_args().output.resolve()
    if not out.is_relative_to(ROOT):
        raise ValueError('Output must be inside repository')
    out.mkdir(parents=True, exist_ok=False)
    receipt = json.loads((BASE / 'receipt.json').read_bytes())
    if receipt.get('status') != 'PassedDisposableS003ImporterFixture':
        raise ValueError('Pinned original source closure required')
    pins = dict(receipt['source_pins'])
    for path in (BASE / 'receipt.json', BASE / 'compile.rsp', BUILD, MATRIX, CATALOG, MATRIX_OWNER, INVENTORY, Path(__file__).resolve()):
        pins[str(path)] = sha(path)
    def verify():
        for name, expected in pins.items():
            path = Path(name)
            if not path.is_file() or sha(path) != expected:
                raise ValueError('Source input changed: ' + name)
    verify()
    control = out / 'AllMissionMatrixOwnerControl.cs'; control.write_text(FIXTURE)
    args = [json.loads(line) for line in (BASE / 'compile.rsp').read_text().splitlines() if line.strip()]
    if args.count(str(ORIGINAL_CONTROL)) != 1 or args.count(str(MATRIX_OWNER)) != 1:
        raise ValueError('Exact original owner compile closure required')
    args = [str(control) if arg == str(ORIGINAL_CONTROL) else '-main:AllMissionMatrixOwnerControl' if arg.startswith('-main:') else '-out:' + str(out / 'control.exe') if arg.startswith('-out:') else arg for arg in args]
    rsp = out / 'compile.rsp'; rsp.write_text(''.join(json.dumps(arg) + '\n' for arg in args))
    csc = next(Path(name) for name in pins if name.endswith('/Roslyn/bincore/csc.dll'))
    compile_run = subprocess.run(['dotnet', str(csc), '@' + str(rsp)], capture_output=True, text=True, timeout=300)
    (out / 'compile.log').write_text(compile_run.stdout + compile_run.stderr)
    if compile_run.returncode:
        raise RuntimeError('Original matrix owner compilation failed')
    runtime = json.loads(BUILD.read_bytes())['runtime_selection']; env = os.environ.copy()
    env['MONO_PATH'] = os.pathsep.join((str(BUILD.parent / 'placement'), runtime['engine'], str(Path(runtime['newtonsoft']).parent), str(ROOT / 'Library/ScriptAssemblies'), str(out)))
    run = subprocess.run([runtime['mono'], str(out / 'control.exe'), str(MATRIX), str(out / 'matrix.json')], cwd=ROOT, env=env, capture_output=True, text=True, timeout=300)
    (out / 'run.log').write_text(run.stdout + run.stderr)
    marker = '[AllMissionMatrixOwner] result=Passed missions=120 rows=360 negatives=2 admission=0 readiness=0'
    if run.returncode or run.stdout.splitlines().count(marker) != 1:
        raise RuntimeError('Original matrix owner execution failed')
    typed = json.loads((out / 'matrix.json').read_bytes())['rows']
    serialized = list(csv.DictReader(io.StringIO(MATRIX.read_text(encoding='utf-8-sig'), newline='')))
    inventory = json.loads(INVENTORY.read_bytes())
    catalog = {m['catalog_id']: m['catalog'] for m in inventory['missions']}
    if inventory['input_sha256'].get(str(MATRIX)) != sha(MATRIX) or inventory['input_sha256'].get(str(CATALOG)) != sha(CATALOG) or len(catalog) != 120:
        raise ValueError('Current complete matrix inventory required')
    if len(typed) != len(serialized):
        raise ValueError('Typed and serialized matrix row counts differ')
    checked = 0
    for raw, row in zip(serialized, typed):
        normalized = {key.lower(): value for key, value in row.items()}
        for key, value in raw.items():
            if key in ('size_id', 'army_profile', 'start_profile', 'objective_id'):
                if row['SourceCodes'][key] != value:
                    raise ValueError('Original enum round trip differs: ' + raw['catalog_id'] + ' ' + key)
                checked += 1
                continue
            property_name = key.replace('_', '').lower()
            if property_name not in normalized:
                raise ValueError('Missing typed matrix property: ' + key)
            actual = normalized[property_name]
            expected = int(value) if isinstance(actual, int) and not isinstance(actual, bool) else value
            if actual != expected:
                raise ValueError('Typed matrix value differs: ' + raw['catalog_id'] + ' ' + key)
            checked += 1
        entry = catalog[raw['catalog_id']]
        if any(raw[matrix] != entry[code] for matrix, code in [('objective_id', 'ObjectiveId'), ('army_profile', 'ArmyProfile'), ('start_profile', 'StartProfile')]):
            raise ValueError('Catalog and typed row identity differ: ' + raw['catalog_id'])
    exported = json.loads((out / 'matrix.json').read_bytes())
    exported['catalog_identity_by_mission'] = catalog
    exported['scope'] = 'Original typed matrix owner joined to serialized catalog identity; no runtime definition or readiness admission'
    (out / 'matrix.json').write_text(json.dumps(exported, indent=2) + '\n')
    verify()
    result = {'status': 'PassedOriginalMatrixOwner', 'marker': marker, 'source_pins': pins,
              'matrix_sha256': sha(MATRIX), 'output_sha256': sha(out / 'matrix.json'), 'run_log_sha256': sha(out / 'run.log'), 'source_field_values_cross_checked': checked,
              'scope': 'Original typed C# setup matrix parser; source data only, no definition, compiler admission, world or mission readiness', 'mission_ready': False}
    (out / 'receipt.json').write_text(json.dumps(result, indent=2) + '\n'); print(marker)


if __name__ == '__main__':
    main()
