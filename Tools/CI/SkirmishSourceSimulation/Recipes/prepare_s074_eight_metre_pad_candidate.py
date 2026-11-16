#!/usr/bin/env python3
"""Prepare a separate, unqualified eight-metre S074 spawn-pad layout candidate."""
import argparse, hashlib, json
from pathlib import Path
ROOT=Path(__file__).resolve().parents[4]
BASE=ROOT/'T Logs/SkirmishProduction/s074-provisional-six-metre-spawn-pads-20261113-r1'
OBS=ROOT/'T Logs/SkirmishProduction/s074-six-metre-live-grid-observer-run-20261114-r1/process.log'
PADS={'s074.provisional.spawn.infantry.player.pad','s074.provisional.spawn.infantry.enemy.pad','s074.provisional.spawn.vehicle.player.pad','s074.provisional.spawn.vehicle.enemy.pad'}
def sha(p):return hashlib.sha256(Path(p).read_bytes()).hexdigest()
def main():
 p=argparse.ArgumentParser(description=__doc__);p.add_argument('--output',type=Path,required=True);a=p.parse_args();out=a.output.resolve()
 if not out.is_relative_to(ROOT) or out.exists():raise ValueError('Fresh repository-owned output required')
 source=BASE/'layout.json';base_receipt=BASE/'receipt.json';obs=OBS
 r=json.loads(base_receipt.read_bytes());layout=json.loads(source.read_bytes());lines=obs.read_text().splitlines();markers=[x for x in lines if x.startswith('[S074SixMetreLiveGridObserver] ')]
 if len(markers)!=1:raise ValueError('Exact fresh live-grid observer marker required')
 diagnostic=json.loads(markers[0].split(' ',1)[1])
 if diagnostic.get('kind')!='S074SixMetreLiveGridSurfaceObservation' or diagnostic.get('stage')!='Immediately before first original opening unit placement after authored buildings':raise ValueError('Candidate-specific original-owner observation required')
 rows=[x for x in layout['pads'] if x['PadId'] in PADS]
 if len(rows)!=4:raise ValueError('Four exact spawn pads required')
 changes=[]
 for pad in rows:
  old=[pad['WidthMetres'],pad['DepthMetres']];pad['WidthMetres']=8;pad['DepthMetres']=8
  changes.append({'pad':pad['PadId'],'old_metres':old,'new_metres':[8,8],'center':[pad['CenterX'],pad['CenterZ']]})
 layout['contentHash']='unqualified.s074.source-origin-eight-metre-pads.v1'
 out.mkdir(parents=True);lp=out/'layout.json';lp.write_text(json.dumps(layout,indent=2)+'\n')
 receipt={'kind':'S074ProvisionalSpawnPadCandidate','status':'PreparedUnqualified','base_layout':r['base_layout'],'parent_candidate':{'path':str(source.resolve()),'sha256':sha(source)},'source_grid_diagnostic':{'path':str(obs.resolve()),'sha256':sha(obs)},'layout':{'path':str(lp.resolve()),'sha256':sha(lp)},'changes':changes,'reason':'Separate 8m extent candidate following original-owner surface traversal observation; no placement or source-fit claim.','source_fit_certified':False,'world_initialized':False,'mission_ready':False,'native_parity':False,'runtime_admission':False}
 (out/'receipt.json').write_text(json.dumps(receipt,indent=2)+'\n');print(json.dumps({'status':receipt['status'],'layout':str(lp),'sha256':sha(lp),'mission_ready':False}))
if __name__=='__main__':main()
