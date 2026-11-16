"""Derive the S003 movement input from a freshly rechecked prefab capture."""
import argparse
import hashlib
import importlib.util
import json
import sys
from pathlib import Path


def sha(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


p = argparse.ArgumentParser(description=__doc__)
for name in ('repo', 'capture', 'audit', 'log', 'output'):
    p.add_argument('--' + name, type=Path, required=True)
a = p.parse_args()
repo, capture, audit, log = [x.resolve(strict=True) for x in (a.repo, a.capture, a.audit, a.log)]
output = a.output.absolute()
if not output.is_relative_to(repo) or output.exists():
    raise ValueError('Fresh repository-owned output required')
tests = repo/'Tools/CI/SkirmishSourceSimulation/Tests'
sys.path.insert(0, str(tests))
from finalize_s003_refreshed_prefab_capture import qualify
inputs = {str(x): sha(x) for x in (capture, audit, log, Path(__file__).resolve())}
checked = qualify(repo, capture, log, 0)
if json.loads(audit.read_bytes()) != checked or checked.get('status') != 'PassedDataOnlyCapture':
    raise ValueError('Exact independently rechecked source prefab capture required')
path = repo/'Tools/CI/SkirmishSourceSimulation/Recipes/unit_move_projection/run_unit_move_authoring_source_host.py'
inputs[str(path)] = sha(path)
spec = importlib.util.spec_from_file_location('s003_original_movement_source', path)
module = importlib.util.module_from_spec(spec)
spec.loader.exec_module(module)
raw = json.loads(capture.read_bytes())
output.mkdir(parents=True)
descriptor_path, descriptor = module.create_source_input(repo, output, raw)
if descriptor.get('kind') != 'StrictUnitMovementAuthoringSourceInput' or descriptor.get('root_key') != 'Unit_Chr_Soldier_Male_02_Alt_04':
    raise ValueError('Exact original movement source projection required')
if any(sha(Path(name)) != digest for name, digest in inputs.items()):
    raise ValueError('Source movement input evidence changed')
receipt = {'kind': 'S003CurrentMovementSourceInputPreparation', 'status': 'PreparedSourceOnly',
           'inputs_sha256': inputs, 'descriptor': {'path': str(descriptor_path), 'sha256': sha(descriptor_path)},
           'scope': 'Original source-derived input, pending importer fixture; no native parity or readiness',
           'native_parity': False, 'mission_ready': False}
(output/'receipt.json').write_text(json.dumps(receipt, indent=2) + '\n')
print(json.dumps({'status': receipt['status'], 'descriptor_sha256': receipt['descriptor']['sha256'], 'mission_ready': False}))
