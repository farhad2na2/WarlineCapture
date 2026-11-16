"""Source-only S003 shared-selection diagnostic projection of the current enemy owner."""
from pathlib import Path
import hashlib
import importlib.util


def generate(repo, output):
    repo, output = Path(repo), Path(output)
    base = repo / 'Tools/CI/SkirmishSourceSimulation/Recipes/regenerate_enemy_owner.py'
    raw = base.read_bytes()
    spec = importlib.util.spec_from_file_location('s003_shared_base_enemy_owner', base)
    if spec is None or spec.loader is None:
        raise ValueError('Current enemy owner generator required')
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    proof = module.generate(repo, output)
    path = output / 'SkirmishEnemyStrategySystem.source.cs'
    text = path.read_text()
    old = 'setup.SetupHash == 2394754492u && setup.ArmyProfileId == SkirmishArmyProfileId.AirMobile'
    new = '(setup.SetupHash == 2394754492u || SourcePaidCatalogSetupGuard.IsSelectedSourceDiagnostic(setup)) && setup.ArmyProfileId == SkirmishArmyProfileId.AirMobile'
    if text.count(old) != 1:
        raise ValueError('Exact current S003 source admission guard required')
    text = text.replace(old, new)
    path.write_text(text)
    if base.read_bytes() != raw:
        raise ValueError('Enemy owner generator changed during extraction')
    digest = hashlib.sha256(text.encode()).hexdigest()
    return {
        'kind': 'S003SharedSelectionEnemySourceProjection',
        'experimental_only': True,
        'source_pins': {str(base): hashlib.sha256(raw).hexdigest(), **proof['source_inputs']},
        'outputs': {'404_SkirmishEnemyStrategySystem.source.cs': digest},
        'base_owner_proof': proof,
        'scope': 'Source diagnostic only; retained original enemy owner and exact bound setup guard',
        'runtime_admission': False,
        'mission_ready': False,
    }
