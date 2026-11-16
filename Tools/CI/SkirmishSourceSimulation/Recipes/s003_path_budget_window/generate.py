"""Pin and observe the original source budget around the current divergence window."""
import hashlib
import importlib.util
from pathlib import Path


def generate(repo, output):
    repo, output = Path(repo).resolve(strict=True), Path(output)
    base = repo / 'Tools/CI/SkirmishSourceSimulation/Recipes/s003_path_budget_observation/generate.py'
    raw = base.read_bytes()
    spec = importlib.util.spec_from_file_location('s003_path_budget_observation_base', base)
    if spec is None or spec.loader is None:
        raise ValueError('Current path budget observation generator required')
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    if module.OBSERVED.count('frameCount>=2150') != 1 or module.OBSERVED.count('frameCount<=2245') != 1:
        raise ValueError('Exact previous passive observation window required')
    module.OBSERVED = module.OBSERVED.replace('frameCount>=2150', 'frameCount>=1900').replace('frameCount<=2245', 'frameCount<=2010')
    proof = module.generate(repo, output)
    if base.read_bytes() != raw:
        raise ValueError('Base observation generator changed')
    rendered = (output / module.OUTPUT).read_bytes()
    return {
        'kind': 'S003CurrentPathBudgetObservationWindow',
        'experimental_only': True,
        'source_pins': {str(base): hashlib.sha256(raw).hexdigest(), **proof['source_pins']},
        'outputs': {'203_UnitPathfindingBudget.cs': hashlib.sha256(rendered).hexdigest()},
        'scope': 'Passive source-only frames 1900-2010; observations can perturb wall timing',
        'native_parity': False,
        'mission_ready': False,
    }
