#!/usr/bin/env python3
"""Prepare an S004-only source compilation manifest with no S002 package admission."""
import argparse
import hashlib
import json
from pathlib import Path

ROOT = Path(__file__).resolve().parents[5]
BASE = ROOT / 'T Logs/SkirmishProduction/s002-current-merged-matrix-manifest-20261105-T1/source-manifest.json'
TARGET = 'S002CurrentSelectedMapGraphBuildAttestation.cs'
DEFINE = 'S002_CURRENT_PACKAGE_DIAGNOSTIC'

def sha(path): return hashlib.sha256(path.read_bytes()).hexdigest()

def prepare(output):
    raw = BASE.read_bytes(); manifest = json.loads(raw); sources = manifest['sources']; defines = manifest['compiler_defines']
    if sum(row.get('id') == TARGET for row in sources) != 1 or defines.count(DEFINE) != 1:
        raise ValueError('Exact S002 package attestation seam required')
    manifest['sources'] = [row for row in sources if row.get('id') != TARGET]
    manifest['compiler_defines'] = [value for value in defines if value != DEFINE]
    output.mkdir(parents=True, exist_ok=False)
    destination = output / 'source-manifest.json'
    with destination.open('x') as stream: json.dump(manifest, stream, indent=2); stream.write('\n')
    if BASE.read_bytes() != raw: raise ValueError('Base manifest changed while deriving')
    proof = {'kind':'S004UnqualifiedSourceManifestDerivation', 'status':'PreparedDiagnosticOnly',
             'base_manifest':{'path':str(BASE),'sha256':hashlib.sha256(raw).hexdigest()},
             'manifest':{'path':str(destination),'sha256':sha(destination)},
             'removed_s002_only_generator':TARGET,'removed_s002_only_define':DEFINE,
             'purpose':'Compile S004 diagnostic owners without asserting S002 capture qualification',
             'runtime_admission':False,'native_context_qualified':False,'mission_ready':False}
    with (output / 'derivation.json').open('x') as stream: json.dump(proof, stream, indent=2); stream.write('\n')
    return proof

if __name__ == '__main__':
    parser = argparse.ArgumentParser(description=__doc__); parser.add_argument('--output', type=Path, required=True)
    print(json.dumps(prepare(parser.parse_args().output.resolve())))
