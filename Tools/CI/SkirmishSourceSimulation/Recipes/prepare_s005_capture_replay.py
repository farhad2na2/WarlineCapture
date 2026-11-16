#!/usr/bin/env python3
"""Derive an S005 typed native-layout replay from the retained S004 harness."""
import hashlib
import json
from pathlib import Path
import argparse

ROOT = Path(__file__).resolve().parents[4]
BASE = ROOT / 'Tools/CI/SkirmishSourceSimulation/Recipes/s004_source_graph'
CAPTURE = ROOT / 'T Logs/SkirmishProduction/s005-layout-native-capture-20261105-T1'
INPUT = ROOT / 'T Logs/SkirmishProduction/s005-original-compiler-probe-inputs-20261105-T2'

def sha(path): return hashlib.sha256(path.read_bytes()).hexdigest()
def need(ok, message):
    if not ok: raise ValueError(message)

def prepare(out, export_experimental=False):
    receipt = json.loads((CAPTURE / 'receipt.json').read_text())
    log = CAPTURE / 'editor.log'
    need(receipt['status'] == 'Passed' and receipt['wrapper_exit'] == 0 and sha(log) == receipt['log_sha256'], 'Capture receipt/log mismatch')
    for name, digest in receipt['source_pins'].items(): need(sha(ROOT/name) == digest, 'Capture source changed: '+name)
    need(receipt['fingerprint'] == receipt['native_json_sha256'], 'Raw native fingerprint mismatch')
    sources = [BASE/'run_s004_authored_capture_replay.py', BASE/'S004LayoutCaptureReplaySourceBinding.cs.txt',
               BASE/'S004DiagnosticJsonUtilityAdapter.cs.txt', INPUT/'S005OriginalCompilerSourceBinding.cs.txt', Path(__file__).resolve()]
    pins = {str(p): sha(p) for p in sources}
    runner = sources[0].read_text().replace('S004','S005').replace('s004','s005').replace('104733','1')
    binding = sources[1].read_text().replace('S004','S005').replace('s004','s005').replace('104733','1')
    adapter = sources[2].read_text().replace('S004','S005').replace('s004','s005')
    start = binding.index('            const string operationMapPath = ')
    end = binding.index('            Type binder = ', start)
    # S005's authored operation-map asset has a distinct logical ID. The
    # original compiler probe uses the definition/catalog directly; avoid the
    # S004-only Resources binding and do not substitute a physical map.
    binding = binding[:start] + ('            UnityEngine.Resources.BindSourceResources(new Dictionary<string, object>\n'
                                '                { [SkirmishExpansionCatalogConfig.ResourceName] = catalog });\n') + binding[end:]
    anchor = '                    if (!matched) throw new InvalidDataException('
    need(binding.count(anchor) == 1, 'Compiler comparison anchor changed')
    binding = binding.replace(anchor, '                    Console.Error.WriteLine("[S005PolicySnapshot] policy size=" + size + " json=" + JsonConvert.SerializeObject(setup.EnemyPolicy));\n' + '                    Console.Error.WriteLine("[S005HashInputs] " + JsonConvert.SerializeObject(new { size = size.ToString(), setup.CatalogId, setup.DefinitionId, setup.SizeId, setup.DifficultyId, seed = Seed, setup.PlayerInfantry, setup.PlayerGround, playerTanks = setup.PlayerTankCount(), setup.MaterialsEach, setup.DeadlineSeconds, setup.LayoutBindingFingerprint, setup.MissionLogicalBindingFingerprint, setup.ObjectiveActorDefinitionFingerprint, enemyPolicyFingerprint = setup.EnemyPolicy == null ? null : setup.EnemyPolicy.Fingerprint, setup.MeasuredLayoutBound, sourceHash = definition.CampaignMap == null ? null : definition.CampaignMap.SourceIdentityHash, contentHash = definition.CampaignMap == null ? null : definition.CampaignMap.ContentHash }));\n' + anchor)
    need('definition.CatalogId != "S005"' in binding and 'JsonConvert.DeserializeObject(raw, projection)' in binding, 'Replay contract changed')
    need('seed") == 1' in runner, 'Native seed guard missing')
    if export_experimental:
        invocation = '[str(mono), str(out / "S005LayoutCaptureReplay.exe")'
        need(runner.count(invocation) == 1, 'Mono invocation anchor changed')
        runner = runner.replace(invocation, '[str(mono), "--optimize=-float32", str(out / "S005LayoutCaptureReplay.exe")')
        result_row = '["nativeSetupHash"] = expected, ["matched"] = true'
        need(binding.count(result_row) == 1, 'Original setup output anchor changed')
        binding = binding.replace(result_row, '["nativeSetupHash"] = expected, ["matched"] = true, ["compiled_setup"] = JObject.FromObject(setup)')
        binding = binding.replace('Exact authored S005 graph only; returns pinned native capture bytes; no serializer/general JsonUtility claim',
                                  'Experimental Mono --optimize=-float32 S005 source setup; no native gameplay or readiness claim')
    compat = {'schema':'s005-native-json-capture-receipt-v1','wrapperExit':0,
              'captureLog':str(log),'captureLogSha256':receipt['log_sha256'],'finalPassMarkerCount':1,
              'authoredS005':{'asset':'Assets/Game/Configs/SkirmishExpansion/Scenarios/S005/SkirmishScenario_S005.asset',
                              'format':'NativeJsonV1','fingerprint':receipt['fingerprint']},
              'sourcePinsVerified':receipt['source_pins'],
              'compilerRows':[{'size':r['size'],'outcome':r['outcome'],'difficulty':r['difficulty'],
                               'seed':int(r['seed']),'setupHash':r['setupHash']} for r in receipt['compiler_rows']]}
    for p,d in pins.items(): need(sha(Path(p)) == d, 'Replay source changed: '+p)
    out.mkdir(parents=True, exist_ok=False)
    (out/'run_s005_authored_capture_replay.py').write_text(runner)
    (out/'S005LayoutCaptureReplaySourceBinding.cs.txt').write_text(binding)
    (out/'S005DiagnosticJsonUtilityAdapter.cs.txt').write_text(adapter)
    (out/'S005OriginalCompilerSourceBinding.cs.txt').write_bytes(sources[3].read_bytes())
    (out/'native-receipt.json').write_text(json.dumps(compat,indent=2)+'\n')
    (out/'preparation.json').write_text(json.dumps({'status':'PreparedOnly','source_pins':pins,
        'native_receipt_sha256':sha(CAPTURE/'receipt.json'),'native_log_sha256':sha(log),
        'experimental_mono_float32_disabled':export_experimental,
        'world_initialized':False,'mission_ready':False},indent=2)+'\n')

if __name__ == '__main__':
    p=argparse.ArgumentParser();p.add_argument('--output',required=True,type=Path);p.add_argument('--export-experimental',action='store_true');a=p.parse_args();prepare(a.output.resolve(),a.export_experimental)
