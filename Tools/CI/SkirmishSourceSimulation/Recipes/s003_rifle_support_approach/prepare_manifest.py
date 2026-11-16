"""Prepare an opt-in diagnostic manifest without changing canonical admission."""
import argparse
import json
from pathlib import Path

GENERATOR = 'Tools/CI/SkirmishSourceSimulation/Recipes/s003_rifle_support_approach/generate.py'
SOURCE = 'Assets/Game/Scripts/Runtime/Skirmish/SkirmishPublicSelectedApproachProjection.cs'


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--manifest', type=Path, required=True)
    parser.add_argument('--output', type=Path, required=True)
    args = parser.parse_args()
    manifest = json.loads(args.manifest.read_text())
    rows = [row for row in manifest['sources'] if row.get('id') == 'current_selected_approach_owner']
    if len(rows) != 1 or rows[0].get('path') != SOURCE:
        raise ValueError('Exact original approach source row required')
    row = rows[0]
    row.update(kind='current_original', generator=GENERATOR, generator_inputs=[SOURCE],
               generator_output='S003RifleSupportApproach.cs', output='S003RifleSupportApproach.cs',
               scope='Experimental source-only rifle support approach; no native or readiness claim')
    row.pop('path')
    row.pop('require_declarations', None)
    args.output.parent.mkdir(parents=True, exist_ok=True)
    with args.output.open('x') as stream:
        json.dump(manifest, stream, indent=2)
        stream.write('\n')


if __name__ == '__main__':
    main()
