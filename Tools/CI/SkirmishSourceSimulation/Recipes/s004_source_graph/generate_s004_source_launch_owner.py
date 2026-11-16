#!/usr/bin/env python3
"""Extract the original launch owner with a null-only visual registry boundary."""
import argparse
import hashlib
from pathlib import Path

ROOT = Path(__file__).resolve().parents[5]
SOURCE = ROOT / 'Assets/Game/Scripts/Composition/Skirmish/SkirmishExpandedLaunchProjection.cs'

def main():
    parser=argparse.ArgumentParser();parser.add_argument('--output',required=True);parser.add_argument('--source-sha256',required=True);args=parser.parse_args()
    raw=SOURCE.read_bytes()
    if hashlib.sha256(raw).hexdigest()!=args.source_sha256:raise ValueError('Exact original launch owner source required')
    text=raw.decode('utf-8')
    replacements=[('UnitPrefabRegistryAuthoringConfig registry = null','object registry = null'),
      ('            if (em == default || payload == null || setup == null || payload.IsLegacy)',
       '            if (registry != null) throw new NotSupportedException("Source-only launch requires an absent managed visual registry");\n            if (em == default || payload == null || setup == null || payload.IsLegacy)'),
      ('            if (registry != null)\n                SkirmishVisualSpawnService.BindSceneRegistry(em, entity, registry);','')]
    for old,new in replacements:
        if text.count(old)!=1:raise ValueError('Original launch owner extraction anchor changed')
        text=text.replace(old,new)
    output=Path(args.output)
    with output.open('x') as f:f.write(text)
    print('[S004SourceLaunchOwnerExtraction] result=Passed nullVisualRegistryOnly=1 nativeInput=0')

if __name__=='__main__':main()
