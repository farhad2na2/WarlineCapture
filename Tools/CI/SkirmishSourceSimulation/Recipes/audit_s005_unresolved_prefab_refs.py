#!/usr/bin/env python3
"""Record unresolved S005 prefab leaves and relevant original owner reads."""
import argparse
import collections
import hashlib
import json
from pathlib import Path

ROOT=Path(__file__).resolve().parents[4]
CAPTURE=ROOT/'T Logs/SkirmishProduction/s005-focused-prefab-capture-20261105-T1/prefab-reference-data.failed.json'
AIR=ROOT/'Assets/Game/Scripts/Systems/AirMissileLauncherSystems.cs'
GROUND=ROOT/'Assets/Game/Scripts/Systems/GroundMissileLauncherSystems.cs'
ATTACK=ROOT/'Assets/Game/Scripts/Systems/UnitAttackSystem.cs'
IMPORTER=ROOT/'Tools/CI/SkirmishSourceSimulation/Adapters/305_GeneratedReferenceTemplateImporter.cs'

def sha(path):return hashlib.sha256(path.read_bytes()).hexdigest()
def audit(out):
    paths=(CAPTURE,AIR,GROUND,ATTACK,IMPORTER,Path(__file__).resolve());pins={str(p):sha(p) for p in paths}
    doc=json.loads(CAPTURE.read_text());rows=doc['unsupported_values']
    if doc.get('replay_ready') is not False or len(rows)!=29:raise ValueError('Exact failed capture and unresolved ledger required')
    counts=collections.Counter()
    for row in rows:
        parts=row['path'].split('/')
        if len(parts)!=3 or row['reason']!='unity_object_ref_requires_bound_source_identity' or not row['type'].startswith('Unity.Entities.UnityObjectRef`1[[UnityEngine.GameObject,'):
            raise ValueError('Unreviewed unresolved source value')
        counts[parts[1]+'/'+parts[2]]+=1
    air=AIR.read_text();ground=GROUND.read_text();attack=ATTACK.read_text();importer=IMPORTER.read_text()
    start=air.index('em.HasComponent<AirMissileLauncherVfxReferenceComponent>(launcherEntity)')
    end=air.index('state.Phase = (byte)AirMissileLauncherPhase.Reloading;',start)
    branch=air[start:end]
    if 'ecb.AddComponent(projectileEntity, new AirMissileProjectileTrailComponent' not in branch:
        raise ValueError('Original air owner branch changed')
    if 'em.HasComponent<GroundMissileLauncherVfxReferenceComponent>(launcherEntity)' not in ground or 'em.HasComponent<UnitMuzzleFlashVfxReference>(source)' not in attack:
        raise ValueError('Original component read sites changed')
    if 'if(PresentationOnly.Contains(property.Name)){omitted++;continue;}' not in importer:
        raise ValueError('Current importer omission branch changed')
    for path,digest in pins.items():
        if sha(Path(path))!=digest:raise ValueError('Source changed: '+path)
    out.mkdir(parents=True,exist_ok=False)
    receipt={'status':'ReadSitesObserved','capture_remains_failed':True,'unresolved_leaf_count':len(rows),
      'unresolved_fields':dict(sorted(counts.items())),'source_pins':pins,
      'air_owner_reference_branch_adds_projectile_trail_component':True,
      'current_importer_omits_entire_vfx_components':True,
      'scope':'Static read-site audit; does not prove gameplay impact, source/native parity or readiness',
      'world_initialized':False,'mission_ready':False}
    (out/'receipt.json').write_text(json.dumps(receipt,indent=2)+'\n');print(json.dumps(receipt))

if __name__=='__main__':
    p=argparse.ArgumentParser();p.add_argument('--output',type=Path,required=True);audit(p.parse_args().output.resolve())
