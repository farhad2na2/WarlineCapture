#!/usr/bin/env python3
"""Propose rectangles from pinned S026 inactive masks with a source-owner surface prefilter; no placement claim."""
import argparse, base64, hashlib, json, math, sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[4]
BASE = ROOT / 'T Logs/SkirmishProduction/five-mission-pilot-20261106-T1'
CAPTURE = ROOT / 'Logs/private-candidates/s026-physical-project-r1/Logs/SkirmishProduction/s026-physical-input-r2.json'
CAPTURE_SHA = 'fa0d0dedd774ba7648ee2cedaa872f506339878bff45429abadf9585157e2710'

def sha(data): return hashlib.sha256(data).hexdigest()

def main():
    ap=argparse.ArgumentParser(description=__doc__);ap.add_argument('--output',type=Path,required=True);ap.add_argument('--radius',type=int,default=128);args=ap.parse_args()
    output=args.output.resolve();preferred=BASE/'s026-compiler-preferred-footprints-audit-r2/result.json';layout=ROOT/'Logs/SkirmishProduction/five-mission-pilot-s026-logical-metadata-20261106-T1/native-layout-fields.json'
    if not output.is_relative_to(ROOT) or output.exists() or not 0<=args.radius<=256 or sys.byteorder!='little':raise ValueError('Fresh repository output, bounded radius and little-endian required')
    owner=ROOT/'Assets/Game/Scripts/Systems/BuildingRuntimeCreationCompositionSystemHelper.cs';raw={str(p):p.read_bytes() for p in (CAPTURE,preferred,layout,owner,Path(__file__).resolve())};owner_bytes=raw[str(owner)];
    if sha(owner_bytes)!='d9c59df98aa777aa67d94e51bf2abcf2e23dc6dbe159746b4558896cb45e3c9d' or b'RuntimeBuildingMaxSurfaceHeightDelta = 0.5f' not in owner_bytes or b'RuntimeBuildingMaxSurfaceSlopeDegrees = 45f' not in owner_bytes:raise ValueError('Exact original owner surface limits required')
    c=json.loads(raw[str(CAPTURE)]);v=json.loads(raw[str(preferred)]);l=json.loads(raw[str(layout)])['MonoBehaviour'];x=c['headlessPlacementInputs']
    if sha(raw[str(CAPTURE)])!=CAPTURE_SHA or sha(raw[str(layout)])!='4dc7c5655f1c1062f99ebc0f1c2483004af40437ae65bf64c50260ce718c01aa' or c['result']!='Collected' or c['missionReady'] is not False or c['sourceFitCertified'] is not False or x['waterPolicyScope']!='OriginalInactiveStreamingFixture_NullNoOpFalse_NotPhysicalWaterClear' or [x['gridWidth'],x['gridHeight'],x['gridCellSize']]!=[2048,1024,1] or x['gridOrigin']!=[0,0,0] or v['kind']!='S026CompilerPreferredFootprintsAudit' or len(v['rows'])!=20 or v['placement_owner_invoked'] is not False:raise ValueError('Exact source-only inputs required')
    if sha(raw[str(preferred)])!='e5219b6671a75f0805b9897c1ce2d164d8d186cba93cc8de1211b58606e42cde' or v['author_sha256']!='f1f056440970b36032ff698dd9d346049a67b43c454e4439d9b8e5d34d928a9b' or v['definition_sha256']!='8e006cca1598cfd5f61ee4c489979eee3849399bbb772b825773fe121cb78045':raise ValueError('Exact native-bound compiler and definition inputs required')
    f=l['worldBinding'];bounds=c['sourceMetadata']['MonoBehaviour']['bounds'];lo=bounds['playableMin'];hi=bounds['playableMax']
    if [f[k] for k in ('ForwardX','ForwardZ','AcrossX','AcrossZ')]!=[1,0,0,1] or [l[k] for k in ('originX','originZ','worldWidthMetres','worldDepthMetres')]!=[0,0,2048,1024] or [lo['x'],lo['z'],hi['x'],hi['z']]!=[0,0,2048,1024]:raise ValueError('Exact axis-aligned source frame required')
    def decode(name,encoding,length):
        item=x[name];data=base64.b64decode(item['base64'],validate=True)
        if item['encoding']!=encoding or item['byteCount']!=length or len(data)!=length or sha(data)!=item['sha256']:raise ValueError('Captured '+name+' changed')
        return data
    prefix=memoryview(decode('sourceInvalidPrefix','int32-little-endian-row-major-xfast',2049*1025*4)).cast('i');present=decode('primarySurfacePresent','uint8-bool01-row-major-xfast',2048*1024);heights=memoryview(decode('primaryHeightBits','float32-rawbits-little-endian-row-major-xfast',2048*1024*4)).cast('f');slopes=memoryview(decode('primarySlopeBits','float32-rawbits-little-endian-row-major-xfast',2048*1024*4)).cast('f')
    def free(px,pz,w,h,reserved):
        if px<1 or pz<1 or px+w>=2048 or pz+h>=1024:return None
        if prefix[(pz+h)*2049+px+w]-prefix[pz*2049+px+w]-prefix[(pz+h)*2049+px]+prefix[pz*2049+px]:return None
        if any(px<rx+rw and px+w>rx and pz<rz+rh and pz+h>rz for rx,rz,rw,rh in reserved):return None
        low=float('inf');high=float('-inf');slope=float('-inf')
        for z in range(pz,pz+h):
            for xx in range(px,px+w):
                i=z*2048+xx; height=heights[i]; deg=slopes[i]
                if present[i]!=1 or not math.isfinite(height) or not math.isfinite(deg):return None
                low=min(low,height);high=max(high,height);slope=max(slope,deg)
        if high-low>0.5 or slope>45.0:return None
        return low,high,slope
    rows=[];reserved=[]
    for expected,row in enumerate(v['rows']):
        if row['index']!=expected or not row['inside_grid']:raise ValueError('Exact ordered compiler preferred footprints required')
        px,pz=row['preferred_origin'];w,h=row['footprint'];found=None
        for radius in range(args.radius+1):
            if found is not None:break
            for dz in range(-radius,radius+1):
                if found is not None:break
                for dx in range(-radius,radius+1):
                    if max(abs(dx),abs(dz))!=radius:continue
                    result=free(px+dx,pz+dz,w,h,reserved)
                    if result is not None:found=(px+dx,pz+dz,*result);break
        if found is not None:reserved.append((found[0],found[1],w,h))
        rows.append({'index':expected,'faction_id':row['faction_id'],'structure_id':row['structure_id'],'visual_key':row['visual_key'],'preferred_origin':[px,pz],'footprint':[w,h],'proposed_origin':list(found[:2]) if found else None,'height_min':found[2] if found else None,'height_max':found[3] if found else None,'max_slope':found[4] if found else None})
    if any(Path(p).read_bytes()!=data for p,data in raw.items()):raise ValueError('Input changed during proposal')
    result={'kind':'S026InactiveMaskFootprintSurfacePrefilterProposals','input_sha256':{p:sha(data) for p,data in raw.items()},'search_radius_cells':args.radius,'rows':rows,'candidate_rectangles_found':len(reserved),'structure_count':len(rows),'scope':'Heuristic nonoverlapping rectangles against retained inactive original prefix and finite primary surface samples only; source-pinned 0.5m/45-degree heuristic surface prefilter only; no original placement/surface owner invoked, active water, live occupancy, typed pads, native parity, source fit or readiness.','placement_owner_invoked':False,'source_fit_certified':False,'mission_ready':False};output.parent.mkdir(parents=True,exist_ok=True);output.write_text(json.dumps(result,indent=2)+'\n');print(json.dumps({'candidate_rectangles_found':len(reserved),'structure_count':len(rows),'mission_ready':False}))

if __name__=='__main__':main()
