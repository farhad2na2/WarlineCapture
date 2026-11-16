"""Create an experimental census-only derivative of the S003 shared-air manifest."""
import argparse
import json
from pathlib import Path

p = argparse.ArgumentParser(description=__doc__)
p.add_argument('--manifest', type=Path, required=True)
p.add_argument('--output', type=Path, required=True)
a = p.parse_args()
d = json.loads(a.manifest.read_bytes())
generator = 'Tools/CI/SkirmishSourceSimulation/Recipes/s003_marker_owner_diagnostic/generate.py'
inputs = ['Assets/Game/Scripts/Systems/MatchHudMinimapMarkerSystem.cs',
          'Tools/CI/SkirmishSourceSimulation/Adapters/S026/384-403_DefenseLogicalApis.cs',
          'Tools/CI/SkirmishSourceSimulation/Adapters/SourceS003AirOpeningController.cs',
          'Tools/CI/SkirmishSourceSimulation/Recipes/s003_marker_owner_diagnostic/support.cs']

def generated(row, filename):
    row.pop('path', None)
    row.update(kind='current_original', generator=generator, generator_inputs=inputs,
               generator_output=filename, output=filename,
               scope='Experimental original marker collector census; no scheduler or presentation parity')

for name, filename in [('S026_403_DefenseLogicalApis.cs', 'MarkerSetShim.cs'),
                       ('SourceS003AirOpeningController.cs', 'SourceS003MarkerCensusBridge.cs')]:
    rows = [x for x in d['sources'] if x.get('id') == name]
    if len(rows) != 1 or rows[0].get('kind') != 'finite_adapter': raise ValueError('Unexpected source seam: ' + name)
    generated(rows[0], filename)
for filename in ('OriginalMarkerCollector.cs', 'SourceMarkerCensus.cs'):
    row = {'id': filename, 'role': 'host_glue', 'contains_original_gameplay': False}
    generated(row, filename)
    d['sources'].append(row)
d['sources'].append({'id': 'OriginalMarkerComponent.cs', 'kind': 'native_direct',
                     'path': 'Assets/Game/Scripts/Components/MatchHudMinimapMarkerComponents.cs',
                     'require_declarations': ['public struct MatchHudMinimapMarkerElement']})
a.output.parent.mkdir(parents=True, exist_ok=True)
with a.output.open('x') as stream:
    json.dump(d, stream, indent=2)
    stream.write('\n')
print('[S003MarkerCensusManifest] result=Prepared experimental=1')
