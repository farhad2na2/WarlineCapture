"""Expose a checked full copy in an experimental source-host buffer shim."""
import hashlib
from pathlib import Path

SHIM = 'Tools/CI/SkirmishSourceSimulation/Adapters/S026/058-059_AutonomousEcs.cs'
OUTPUT = 'SourceExperimentalPooledBuffer.cs'
SEAM = 'internal T[] Copy()=>rows.ToArray();'
COPY = 'internal void SourceCopyTo(T[] dest,int length){if(length!=rows.Count)throw new InvalidOperationException("Buffer changed during copy");rows.CopyTo(0,dest,0,length);}'


def generate(repo, output):
    path = Path(repo).resolve(strict=True) / SHIM
    raw = path.read_bytes(); original = raw.decode()
    if original.count(SEAM) != 1:
        raise ValueError('Exact source-host buffer seam required')
    generated = original.replace(SEAM, SEAM + COPY, 1).encode()
    if path.read_bytes() != raw:
        raise ValueError('Buffer shim changed during generation')
    out = Path(output); out.mkdir(parents=True, exist_ok=False)
    (out / OUTPUT).write_bytes(generated)
    return {'source_pins': {str(path): hashlib.sha256(raw).hexdigest()},
            'outputs': {OUTPUT: hashlib.sha256(generated).hexdigest()}, 'experimental_only': True,
            'scope': 'Source-host full buffer copy only'}
