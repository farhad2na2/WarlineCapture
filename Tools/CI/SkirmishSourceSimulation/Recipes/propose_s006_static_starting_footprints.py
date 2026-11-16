#!/usr/bin/env python3
"""Propose S006 rectangles against captured inactive Campaign masks only."""
import argparse
import hashlib
import json
import math
import struct
import sys
from pathlib import Path

ROOT=Path(__file__).resolve().parents[4]
CANDIDATE=ROOT/'T Logs/SkirmishProduction/s006-canonical-v2-candidate-20261105-T7'
INPUT=ROOT/'T Logs/SkirmishProduction/s005-campaign-placement-inputs-20261105-T1'
FACTORY=ROOT/'Logs/SkirmishProduction/s005-production-factory-capture-20261105-T2/production-factory-runway-definitions.json'
FACTORY_SHA='2f4796f95a1ef0cfd16056bd1abf65656d7aabad19b7bb98d2ec4bf0b6ccdf91'
GRAPH=ROOT/'T Logs/SkirmishProduction/s005-original-compiler-probe-inputs-20261105-T2/source-config-graph.json'
GRAPH_SHA='b784ffc6b36260b8c403362b93a741a06c493ebdc09704ffd828e82e9ee64138'
LAYOUT=ROOT/'T Logs/SkirmishProduction/s005-layout-native-capture-20261105-T1/authored-layout-native.json'
LAYOUT_RECEIPT=LAYOUT.parent/'receipt.json'


def sha(data):return hashlib.sha256(data).hexdigest()


def main():
    p=argparse.ArgumentParser(description=__doc__);p.add_argument('--output',required=True,type=Path);p.add_argument('--candidate',type=Path,default=CANDIDATE);p.add_argument('--radius',type=int,default=128);a=p.parse_args();out=a.output.resolve();candidate=a.candidate.resolve(strict=True)
    if not out.is_relative_to(ROOT) or a.radius<0 or a.radius>256 or sys.byteorder!='little':raise ValueError('Bounded repository output and little-endian inputs required')
    if not candidate.is_relative_to(ROOT):raise ValueError('Candidate must be repository-owned')
    inputs=[candidate/'receipt.json',candidate/'result.json',INPUT/'receipt.json',FACTORY,GRAPH,LAYOUT,LAYOUT_RECEIPT,Path(__file__).resolve()];raw={str(f):f.read_bytes() for f in inputs}
    receipt=json.loads(raw[str(candidate/'receipt.json')]);result=json.loads(raw[str(candidate/'result.json')]);capture=json.loads(raw[str(INPUT/'receipt.json')])
    if receipt.get('status')!='CollectedSourceCandidate' or receipt.get('result_sha256')!=sha(raw[str(candidate/'result.json')]) or not result.get('candidate_only') or result.get('format_changed_for_candidate') is not True:raise ValueError('Exact experimental candidate receipt required')
    if capture.get('status')!='ExtractedSourceInputsOnly' or capture.get('grid')!=[2048,1024,1] or sha(raw[str(FACTORY)])!=FACTORY_SHA or sha(raw[str(GRAPH)])!=GRAPH_SHA:raise ValueError('Exact retained source inputs required')
    for name,digest in receipt['source_pins'].items():
        f=Path(name)
        if not f.is_file() or sha(f.read_bytes())!=digest:raise ValueError('Candidate source pin changed: '+name)
    names=('sourceInvalidPrefix','primarySurfacePresent','primaryHeightBits','primarySlopeBits')
    arrays={}
    for name in names:
        desc=capture['artifacts'][name];f=Path(desc['path']);data=f.read_bytes()
        if len(data)!=desc['bytes'] or sha(data)!=desc['sha256']:raise ValueError('Captured mask changed: '+name)
        raw[str(f)]=data;arrays[name]=data
    prefix=memoryview(arrays['sourceInvalidPrefix']).cast('i');present=arrays['primarySurfacePresent'];heights=memoryview(arrays['primaryHeightBits']).cast('f');slopes=memoryview(arrays['primarySlopeBits']).cast('f')
    if len(prefix)!=2049*1025 or len(present)!=2048*1024 or len(heights)!=len(present) or len(slopes)!=len(present):raise ValueError('Exact captured grid dimensions required')
    factory=json.loads(raw[str(FACTORY)]);footprints={}
    for item in factory['buildings']:
        fields=item['fields']['FootprintCells']['fields'];key=item['prefab']['key']
        if key in footprints:raise ValueError('Duplicate factory prefab')
        footprints[key]=(int(fields['UnityEngine.Vector2Int::m_X']['value']),int(fields['UnityEngine.Vector2Int::m_Y']['value']))
    graph=json.loads(raw[str(GRAPH)]);maps=[n for n in graph['nodes'] if n.get('source_path','').endswith('/S005/SkirmishOperationMap_S005.asset')]
    if len(maps)!=1:raise ValueError('Unique Campaign-bound source required')
    bounds=maps[0]['fields']['bounds'];lo=bounds['playableMin'];hi=bounds['playableMax']
    layout_bytes=raw[str(LAYOUT)];layout_receipt=json.loads(raw[str(LAYOUT_RECEIPT)])
    if not layout_bytes.endswith(b'\n') or layout_receipt.get('native_json_sha256')!=sha(layout_bytes[:-1]) or layout_receipt.get('status')!='Passed' or layout_receipt.get('wrapper_exit')!=0:raise ValueError('Exact native layout JSON required')
    layout=json.loads(layout_bytes);frame=layout['Frame']
    if (frame['ForwardX'],frame['ForwardZ'],frame['AcrossX'],frame['AcrossZ'])!=(1.0,0.0,0.0,1.0):raise ValueError('Exact axis-aligned layout frame required')
    frame_min_x=frame['OriginX'];frame_max_x=frame_min_x+frame['ForwardMetres'];frame_min_z=frame['OriginZ']-frame['AcrossMetres']*.5;frame_max_z=frame['OriginZ']+frame['AcrossMetres']*.5
    def free(x,z,w,h,reserved):
        if x<lo['x'] or z<lo['z'] or x+w>hi['x'] or z+h>hi['z'] or x<0 or z<0 or x+w>2048 or z+h>1024:return False
        if x-1<frame_min_x or z-1<frame_min_z or x+w+1>frame_max_x or z+h+1>frame_max_z:return False
        if prefix[(z+h)*2049+x+w]-prefix[z*2049+x+w]-prefix[(z+h)*2049+x]+prefix[z*2049+x]:return False
        if any(x<rx+rw and x+w>rx and z<rz+rh and z+h>rz for rx,rz,rw,rh in reserved):return False
        low=float('inf');high=float('-inf')
        for zz in range(z,z+h):
            for xx in range(x,x+w):
                i=zz*2048+xx;v=heights[i];s=slopes[i]
                if not present[i] or not math.isfinite(v) or not math.isfinite(s) or s>45:return False
                low=min(low,v);high=max(high,v)
                if high-low>0.5:return False
        return True
    reports=[]
    for row in result['rows']:
        if not row.get('compiled') or row.get('matrix_scalar_mismatches'):raise ValueError('Compiled candidate and matrix match required')
        structures=row['setup']['Structures'];bindings=row['source_pad_resolution'];reserved=[];proposals=[]
        if len(structures)!=len(bindings):raise ValueError('Exact structure correspondence required')
        for i,(structure,binding) in enumerate(zip(structures,bindings)):
            key=binding['visual_key'];w,h=footprints[key]
            if binding['structure_index']!=i or binding['structure_id']!=structure['StructureId'] or binding['faction_id']!=structure['FactionId'] or w<=0 or h<=0:raise ValueError('Original owner structure/footprint correspondence required')
            cx=math.floor(structure['SpawnWorldX']);cz=math.floor(structure['SpawnWorldZ']);px=cx-w//2;pz=cz-h//2;found=None
            for radius in range(a.radius+1):
                if found:break
                for dz in range(-radius,radius+1):
                    if found:break
                    for dx in range(-radius,radius+1):
                        if max(abs(dx),abs(dz))!=radius:continue
                        x,z=px+dx,pz+dz
                        if free(x,z,w,h,reserved):found=(x,z,w,h);break
            if found:reserved.append(found)
            proposals.append({'structure_index':i,'faction_id':structure['FactionId'],'structure_id':structure['StructureId'],'visual_key':key,
                              'preferred_origin':[px,pz],'footprint':[w,h],'proposed_origin':list(found[:2]) if found else None,
                              'search_radius_cells':a.radius,'source_mask_candidate_found':found is not None})
        reports.append({'size':row['size'],'setup_hash':row['setup_hash'],'proposals':proposals,'candidate_rectangles_found':len(reserved),'structure_count':len(structures)})
    if any(Path(name).read_bytes()!=data for name,data in raw.items()):raise ValueError('Evidence changed during audit')
    out.parent.mkdir(parents=True,exist_ok=False);report={'kind':'S006StaticFootprintProposals','scope':'Disposable search on retained inactive Campaign prefix, surface samples and captured axis-aligned logical frame; no original placement owner invoked, active water, live blockers, typed pad, loaded-scene or source-fit validation',
       'input_sha256':{name:sha(data) for name,data in raw.items()},'sizes':reports,'dynamic_blockers_checked':False,'active_water_checked':False,'original_placement_owner_invoked':False,'source_fit_certified':False,'mission_ready':False}
    out.write_text(json.dumps(report,indent=2)+'\n');print(json.dumps({'kind':report['kind'],'sizes':[{k:v for k,v in row.items() if k!='proposals'} for row in reports],'mission_ready':False}))


if __name__=='__main__':main()
