"""Prepare a source-only S003 shared-air-opening host manifest."""
import argparse
import json
from pathlib import Path

p=argparse.ArgumentParser(description=__doc__);p.add_argument('--manifest',type=Path,required=True);p.add_argument('--output',type=Path,required=True);a=p.parse_args()
doc=json.loads(a.manifest.read_bytes());host='Tools/CI/SkirmishSourceSimulation/Adapters/493_GeneratedS002UnifiedAria.s003.cs';bridge='Tools/CI/SkirmishSourceSimulation/Adapters/SourceS003AirOpeningController.cs';generator='Tools/CI/SkirmishSourceSimulation/Recipes/s003_shared_air_opening/generate.py';output='SourceS003SharedAirOpeningWorld.cs'
rows=[r for r in doc['sources'] if r.get('id')=='493_GeneratedS002UnifiedAria.cs']
if len(rows)!=1 or rows[0].get('path')!=host or rows[0].get('kind')!='finite_adapter':raise ValueError('Exact source host manifest required')
row=rows[0];row.pop('path');row.update(kind='current_original',generator=generator,generator_inputs=[host,bridge],generator_output=output,output=output,scope='Experimental S003 shared gameplay air-opening bridge')
doc['sources'].append({'id':'SourceS003AirOpeningController.cs','kind':'finite_adapter','path':bridge,'role':'host_glue','contains_original_gameplay':False,'scope':'Partial original shared air-opening policy and paid owner; no native UI or full planner parity'})
a.output.parent.mkdir(parents=True,exist_ok=True)
with a.output.open('x') as f:json.dump(doc,f,indent=2);f.write('\n')
print('[S003SharedAirOpeningManifest] result=Prepared experimental=1')
