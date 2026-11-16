#!/usr/bin/env python3
"""Extract pinned Campaign placement inputs for S005 diagnostics, without admission."""
import argparse
import base64
import hashlib
import json
from pathlib import Path

ROOT=Path(__file__).resolve().parents[4]
AUDIT=ROOT/'T Logs/SkirmishProduction/s005-campaign-source-reuse-audit-20261105-T1/receipt.json'
BUNDLE=ROOT/'Logs/SkirmishProduction/s073-source-inputs-20261101-r1/data-bundle-r4.json'
NAMES=('gridWalkable','gridRoad','authoredBuildings','authoredRoadsIncludingSidewalks','sidewalks','startupRoads',
       'primarySurfacePresent','primaryHeightBits','primarySlopeBits','sourceInvalidPrefix')
def sha_bytes(data):return hashlib.sha256(data).hexdigest()
def sha(path):return sha_bytes(path.read_bytes())
def need(value,message):
    if not value:raise ValueError(message)
def extract(out):
    audit=json.loads(AUDIT.read_text());need(audit['status']=='MatchedLimitedCampaignInputIdentity' and not audit['mission_ready'],'S005 source identity audit required')
    bundle=json.loads(BUNDLE.read_text());desc=bundle['campaign_physical'];capture=Path(desc['path']);raw=capture.read_bytes()
    need(sha_bytes(raw)==desc['sha256']==audit['campaign_capture_sha256'],'Exact retained physical capture required')
    source=json.loads(raw);data=source['headlessPlacementInputs'];domain=source['fixedMapReadDomain']
    need(data['operationMapId']==audit['campaign_source_id'] and data['sourceDefinitionSha256']==audit['campaign_source_sha256'] and
         data['gridWidth']==2048 and data['gridHeight']==1024 and data['gridCellSize']==1 and data['gridOrigin']==[0,0,0] and
         not data['liveOccupancyMeasured'] and not data['sourceFitCertified'] and not data['missionReady'] and
         data['water']['present'] is False and data['skirmishMatchQueryCount']==0 and
         domain['actorOrMissionStateExported'] is False and len(domain['authoredNavigationBlockers'])==4954,
         'Captured input scope or physical source differs')
    blobs={}
    for name in NAMES:
        row=data[name];value=base64.b64decode(row['base64'],validate=True)
        need(len(value)==row['byteCount'] and sha_bytes(value)==row['sha256'],'Captured input bytes differ: '+name)
        if name in NAMES[:7]:need(row['count']==2048*1024 and len(value)==2048*1024 and all(v in (0,1) for v in value),'Invalid binary cell mask: '+name)
        blobs[name]=value
    need(len(blobs['primaryHeightBits'])==len(blobs['primarySlopeBits'])==2048*1024*4 and
         len(blobs['sourceInvalidPrefix'])==2049*1025*4,'Captured numeric input lengths differ')
    actual=Path(audit['campaign_source_asset']);need(actual.is_file() and sha(actual)==audit['campaign_source_sha256'],'Campaign source asset changed')
    need(sha(capture)==audit['campaign_capture_sha256'],'Campaign capture changed during extraction')
    out.mkdir(parents=True,exist_ok=False);artifacts={}
    for name,value in blobs.items():
        p=out/(name+'.bin');p.write_bytes(value);artifacts[name]={'path':str(p),'sha256':sha_bytes(value),'bytes':len(value),'encoding':data[name]['encoding']}
    receipt={'status':'ExtractedSourceInputsOnly','campaign_capture_sha256':audit['campaign_capture_sha256'],
      'campaign_source_sha256':audit['campaign_source_sha256'],'s005_logical_map_id':audit['logical_map_id'],
      'grid':[2048,1024,1],'artifacts':artifacts,'authored_navigation_blocker_rows':4954,
      'water':'NotCapturedForActiveMission','live_occupancy_measured':False,'capture_transitive_pins_verified':False,
      'loaded_scene_validated':False,'terrain_fit_validated':False,'placement_attempted':False,'mission_ready':False}
    (out/'receipt.json').write_text(json.dumps(receipt,indent=2)+'\n');print(json.dumps({'status':receipt['status'],'artifacts':len(artifacts),'water':receipt['water'],'mission_ready':False}))
if __name__=='__main__':
    p=argparse.ArgumentParser();p.add_argument('--output',required=True,type=Path);extract(p.parse_args().output.resolve())
