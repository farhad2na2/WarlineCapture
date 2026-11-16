"""Per-call indexed child scan comparison for the exact S026 host source."""
import hashlib
from pathlib import Path

HERE = Path(__file__).resolve().parent
VERIFIED = HERE / 'generate_verified.py'
SOURCE = 'Tools/CI/SkirmishSourceSimulation/Adapters/S026/extra-614-SourceMissionTransformBridge.cs'


def generate(repo, output):
    raw = VERIFIED.read_bytes()
    module = {'__file__': str(VERIFIED), '__name__': 's026_verified_transform_generator'}
    exec(compile(raw, str(VERIFIED), 'exec'), module)
    module['base']['SOURCE'] = SOURCE
    result = module['generate'](repo, output)
    if VERIFIED.read_bytes() != raw:
        raise ValueError('Verified generator changed during S026 generation')
    result['source_pins'][str(VERIFIED)] = hashlib.sha256(raw).hexdigest()
    result['scope'] = 'S026 same-host per-call original-versus-indexed control; no native or readiness claim'
    return result
