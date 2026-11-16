#!/usr/bin/env python3
"""Use the exact original intro-lock rule in the shell-owner diagnostic host."""
import argparse
import hashlib
import json
from pathlib import Path

ROOT = Path(__file__).resolve().parents[5]
TEMPLATE = ROOT / 'Tools/CI/SkirmishSourceSimulation/Templates/471_SourceS002CatalogPublicFacts.cs.template'
ORIGINAL = ROOT / 'Assets/Game/Scripts/UI/Shell/Ecs/UiShellEcsGateway.MissionHudRestrictions.cs'
GUARD = '''  // Original IsSkirmishIntroInputLocked returns false at zero transitions.
  // A present transition requires original UiShellState/transition authority, never default false.
  using(var q=em.CreateEntityQuery(typeof(MatchIntroTransitionComponent)))if(q.CalculateEntityCount()!=0)throw new NotSupportedException("Active MatchIntroTransitionComponent requires original source UI state/lock read");
'''
OLD_BINDING = 'throw new NotSupportedException("Active source intro boundary requires UI shell state authority");'


def main():
    p = argparse.ArgumentParser(description=__doc__)
    p.add_argument('--manifest', type=Path, required=True); p.add_argument('--output', type=Path, required=True)
    args = p.parse_args(); out = args.output.resolve()
    if not out.is_relative_to(ROOT):
        raise ValueError('Output must be in repository')
    manifest = json.loads(args.manifest.read_bytes()); source = TEMPLATE.read_text(); original = ORIGINAL.read_text()
    if source.count(GUARD) != 1 or 'return !em.HasComponent<UiShellStateComponent>(boundary) ||' not in original:
        raise ValueError('Exact original source intro rule and adapter guard required')
    rows = [r for r in manifest['sources'] if r.get('output') == '471_SourceS002CatalogPublicFacts.cs']
    if len(rows) != 1:
        raise ValueError('Unique source catalog facts entry required')
    members = [r for r in rows[0]['members'] if r.get('id') == 'intro_guard']
    if len(members) != 1 or len(members[0].get('bindings', [])) != 1 or members[0]['bindings'][0].get('to') != OLD_BINDING:
        raise ValueError('Exact prior unsupported intro seam required')
    out.mkdir(parents=True, exist_ok=False)
    template = out / 'SourceCatalogOriginalIntro.cs.template'
    template.write_text(source.replace(GUARD, '  // Original intro rule below reads the actual source shell and transition components.\n'))
    rows[0]['template'] = str(template.relative_to(ROOT))
    members[0]['bindings'] = []
    rows[0]['scope'] = 'Original current restriction and intro-lock bodies; same-world source shell state, no native UI or readiness claim.'
    manifest_path = out / 'source-manifest.json'
    manifest_path.write_text(json.dumps(manifest, indent=2) + '\n')
    h = lambda path: hashlib.sha256(path.read_bytes()).hexdigest()
    proof = {'status': 'PreparedOriginalIntroRule', 'original_source': {'path': str(ORIGINAL), 'sha256': h(ORIGINAL)},
             'base_template': {'path': str(TEMPLATE), 'sha256': h(TEMPLATE)},
             'derived_template': {'path': str(template), 'sha256': h(template)},
             'manifest': {'path': str(manifest_path), 'sha256': h(manifest_path)},
             'scope': 'Original rule extraction only; experimental source host, no readiness claim', 'mission_ready': False}
    (out / 'preparation.json').write_text(json.dumps(proof, indent=2) + '\n')
    print(json.dumps(proof))


if __name__ == '__main__':
    main()
