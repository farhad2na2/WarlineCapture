"""Read-only native mission export integrity, static observations, and catalog coverage."""
from __future__ import annotations
import argparse
from collections import Counter, deque
import hashlib
import json
import math
import ntpath
from pathlib import Path
import re
import sys
import time
sys.dont_write_bytecode=True
from skirmish_preflight_doctor import strict_json, stamp, LFS_HEADER
from skirmish_catalog_diagnostics import Diagnostics, CATALOG, MATRIX

EXPECTED_IDS=tuple(f"S{i:03d}" for i in range(1,121))
MAX_JSON_BYTES=128*1024*1024
MAX_ARTIFACT_BYTES=256*1024*1024
MAX_CELLS=16*1024*1024
MAX_TOTAL_HASH_BYTES=16*1024*1024*1024
SHA=re.compile(r"[a-f0-9]{64}\Z")


def safe_relative(root,value):
    if not isinstance(value,str) or not value or "\\" in value or ":" in value or ntpath.isabs(value):raise ValueError("Expected portable relative path")
    rel=Path(value)
    if rel.is_absolute() or any(p in ("",".","..") for p in value.split("/")):raise ValueError("Unsafe relative path")
    target=(root/rel).resolve()
    if not target.is_relative_to(root):raise ValueError("Path escapes its pinned root")
    return target


def document(path):
    with Path(path).open('rb') as stream:raw=stream.read(MAX_JSON_BYTES+1)
    if len(raw)>MAX_JSON_BYTES:raise ValueError("Export JSON exceeds bounded parse limit")
    value=strict_json(raw)
    if not isinstance(value,dict):raise ValueError("JSON object required")
    return value


class Inspection:
    def __init__(self,project,bundle):
        self.project=Path(project).resolve();self.bundle=Path(bundle).resolve();self.findings=[];self.hashes={};self.total_hash_bytes=0;self.checked_artifacts={};self.map_results={};self.topology_cells=0;self.package_roots={}
    def issue(self,code,message,*,scope='export',source='',field='',catalog_id='',vector='',severity='integrity'):
        self.findings.append(dict(severity=severity,code=code,scope=scope,source_path=str(source),field=field,catalog_id=catalog_id,vector=vector,message=str(message)))
    def digest(self,path):
        path=Path(path)
        if path in self.hashes:return self.hashes[path]
        before=stamp(path)
        if not path.is_file():raise ValueError("Expected regular file")
        self.total_hash_bytes+=before[2]
        if self.total_hash_bytes>MAX_TOTAL_HASH_BYTES:raise ValueError("Aggregate source hash work exceeds bounded limit")
        h=hashlib.sha256();head=b''
        with path.open('rb') as stream:
            for block in iter(lambda:stream.read(1024*1024),b''):
                if not head:head=block[:128]
                h.update(block)
        if stamp(path)!=before:raise ValueError("File changed while hashing")
        result=dict(sha256=h.hexdigest(),bytes=before[2],lfs=head.startswith(LFS_HEADER),stamp=before);self.hashes[path]=result;return result
    def read_document(self,path):
        path=Path(path);record=self.digest(path)
        if record['bytes']>MAX_JSON_BYTES:raise ValueError('JSON exceeds bounded parse limit')
        raw=path.read_bytes()
        if hashlib.sha256(raw).hexdigest()!=record['sha256'] or stamp(path)!=record['stamp']:raise ValueError('Parsed JSON differs from pinned bytes')
        value=strict_json(raw)
        if not isinstance(value,dict):raise ValueError('JSON object required')
        return value
    def recheck_stamps(self):
        for path,record in self.hashes.items():
            try:
                if stamp(path)!=record['stamp']:self.issue('changed_during_inspection','Input changed during inspection',source=path)
            except OSError as error:self.issue('changed_during_inspection',error,source=path)
    def verify_file(self,root,path,digest,size=None):
        if not isinstance(digest,str) or not SHA.fullmatch(digest):raise ValueError("Lowercase SHA256 required")
        target=safe_relative(root,path);record=self.digest(target)
        if record['sha256']!=digest:raise ValueError("Pinned bytes changed; regenerate export")
        if size is not None and (type(size) is not int or size<0 or size!=record['bytes']):raise ValueError("Pinned byte length differs")
        return target,record
    @property
    def integrity_ok(self):return not any(f['severity']=='integrity' for f in self.findings)


def components(mask,width,height):
    """Four-neighbour static-mask topology only; not a native routing/search simulation."""
    from array import array
    labels=array('i',[-1])*len(mask);sizes=[]
    for start,open_cell in enumerate(mask):
        if not open_cell or labels[start]>=0:continue
        label=len(sizes);labels[start]=label;pending=deque([start]);count=0
        while pending:
            at=pending.popleft();count+=1;x=at%width
            for other in ((at-1 if x else -1),(at+1 if x+1<width else -1),at-width,at+width):
                if 0<=other<len(mask) and mask[other] and labels[other]<0:labels[other]=label;pending.append(other)
        sizes.append(count)
    return labels,sizes

MARKER='[SkirmishOfflineDataExporter] result=Passed'
PRODUCER='Game.Editor.SkirmishOfflineDataExporter.Export'
SIZES=('Standard','War','LargeWar')
DIFFICULTIES=('Recruit','Regular','Veteran','Commander')
VARIANTS=tuple((s,d) for s in SIZES for d in DIFFICULTIES)
DRAFT_ROLES={'infantry_anchor','vehicle_anchor','air_anchor','structure_anchor','objective_point','route_waypoint','logical_anchor'}
ARTIFACT_FIELDS={'walkable_artifact':1,'infantry_surface_artifact':1,'cells_artifact':8,
                 'samples_artifact':47,'connections_artifact':19,'compact_samples_artifact':12}
REPLAY_GAPS=('Runtime actor initialization/statistics and final spawn placement are not exported',
 'Runtime static blockers/openings/roads, live occupancy and fog/session state are not captured',
 'Production/economy, player controls and full mission world-system order are not simulated',
 'Normal-input ARIA, result/return, visual and device acceptance remain separate gates')


def array(value,field,limit=100000):
    if not isinstance(value,list) or len(value)>limit:raise ValueError(field+' requires bounded array')
    return value


def object_value(value,field):
    if not isinstance(value,dict):raise ValueError(field+' requires object')
    return value


def finite(value):
    return type(value) in (int,float) and math.isfinite(value)


def keyed(rows,key,limit):
    result={}
    for row in array(rows,key,limit):
        object_value(row,key);identity=row.get(key)
        if not isinstance(identity,str) or not identity or identity in result:raise ValueError('Missing/duplicate '+key+': '+str(identity))
        result[identity]=row
    return result


def require_pin_closure(ins,manifest,pins):
    # Mirror the exporter's source-policy closure, detecting newly added policy files too.
    required={CATALOG,MATRIX,'Packages/manifest.json','Packages/packages-lock.json','Assets/Tests/Editor/SkirmishOfflineDataExportTests.cs','Assets/Tests/Editor/SkirmishOfflineCampaignReferencesTests.cs'}
    for root,pattern in (('Assets/Game/Scripts','*.cs'),('Assets/Game/Scripts','*.asmdef'),('ProjectSettings','*')):
        folder=ins.project/root
        required.update(p.relative_to(ins.project).as_posix() for p in (folder.rglob(pattern) if root.startswith('Assets') else folder.glob(pattern)) if p.is_file())
    required.update(p.relative_to(ins.project).as_posix() for p in (ins.project/'Assets').rglob('*') if p.is_file() and '/Resources/' in p.as_posix() and p.suffix!='.meta' and not any(part.startswith('.') for part in p.relative_to(ins.project).parts))
    required.update(p.relative_to(ins.project).as_posix() for p in (ins.project/'Design').rglob('*.csv') if 'skirmish' in str(p).lower())
    for field in ('menu_path','campaign_catalog_path','campaign_map_catalog_path','battle_catalog_path','expansion_catalog_path','matrix_path'):
        value=manifest.get(field)
        if not isinstance(value,str) or not value:ins.issue('authority_reference_missing','Required authority reference missing',field=field)
        else:required.add(value)
    required.update({'Assets/Game/Scripts/Editor/Skirmish/SkirmishOfflineDataExporter.cs',
                     'Assets/Game/Scripts/Editor/Skirmish/SkirmishOfflineDataExporter.Maps.cs',
                     'Assets/Game/Scripts/Editor/Skirmish/SkirmishOfflineExportData.cs'})
    for path in sorted(required-set(pins)):ins.issue('source_pin_missing','Required authority/policy source was not pinned',source=path)
    for path in list(pins):
        if not path.endswith('.meta') and safe_relative(ins.project,path+'.meta').is_file() and path+'.meta' not in pins:
            ins.issue('meta_pin_missing','Current asset identity metadata not pinned',source=path+'.meta')



def source_path(ins,path,pins):
    direct=safe_relative(ins.project,path)
    if direct.exists() or not path.startswith('Packages/') or path.count('/')<2:return direct
    _,name,relative=path.split('/',2)
    if not re.fullmatch(r'[a-z0-9][a-z0-9._-]*',name):raise ValueError('Invalid virtual package name')
    if name not in ins.package_roots:
        lockpin=pins.get('Packages/packages-lock.json',{})
        ins.verify_file(ins.project,'Packages/packages-lock.json',lockpin.get('sha256'),lockpin.get('bytes'))
        lock=ins.read_document(ins.project/'Packages/packages-lock.json');entry=lock.get('dependencies',{}).get(name)
        if not isinstance(entry,dict) or entry.get('source') not in ('builtin','registry'):raise ValueError('Unresolved virtual package requires a locked builtin/registry package')
        candidates=[]
        for folder in sorted((ins.project/'Library/PackageCache').glob(name+'@*')):
            root=safe_relative(ins.project,folder.relative_to(ins.project).as_posix());manifest=root/'package.json'
            if not manifest.is_file():continue
            package=ins.read_document(manifest)
            if package.get('name')==name and package.get('version')==entry.get('version'):candidates.append(root)
        if len(candidates)!=1:raise ValueError('Exactly one installed lockfile-matching package cache required; no download or ambiguous fallback')
        ins.package_roots[name]=candidates[0]
    return safe_relative(ins.package_roots[name],relative)

def inspect_sources(ins,manifest):
    pins=keyed(manifest.get('source_pins'),'path',50000)
    require_pin_closure(ins,manifest,pins)
    for path,pin in pins.items():
        try:
            status=pin.get('status');target=source_path(ins,path,pins)
            if status not in ('complete','missing','empty','lfs_pointer'):raise ValueError('Unknown source status')
            if status=='missing':
                if target.exists() or pin.get('sha256') not in ('',None) or pin.get('bytes')!=0:raise ValueError('Missing source status differs from current source')
            else:
                target,actual=ins.verify_file(ins.project,target.relative_to(ins.project).as_posix(),pin.get('sha256'),pin.get('bytes'))
                actual_status='lfs_pointer' if actual['lfs'] else ('empty' if actual['bytes']==0 else 'complete')
                if actual_status!=status:raise ValueError('Source payload status differs')
            if status!='complete':ins.issue('source_'+status,'Export accurately records unavailable source payload',source=path,severity='blocker')
            guid=pin.get('guid')
            if guid and not re.fullmatch(r'[a-f0-9]{32}',guid):raise ValueError('Invalid asset GUID')
            if guid and target.is_file() and target.suffix!='.meta':
                meta=source_path(ins,path+'.meta',pins)
                if meta.is_file():
                    if path+'.meta' not in pins:raise ValueError('Asset identity metadata not pinned')
                    found=re.search(rb'^guid: ([a-f0-9]{32})\s*$',meta.read_bytes(),re.M)
                    if not found or found.group(1).decode()!=guid:raise ValueError('Current metadata GUID differs')
        except (OSError,ValueError,TypeError) as exc:ins.issue('source_drift',exc,source=path)
    selection=manifest.get('catalog_selection');scope=manifest.get('compiler_catalog_scope')
    if (selection,scope) not in (('DefaultPackaged','PackagedRuntimeResources'),('ExplicitCandidateCatalog','ExistingDisposableEditorValidationCatalog')):ins.issue('catalog_selection','Unknown catalog/compile selection scope')
    selected=pins.get(manifest.get('expansion_catalog_path'),{})
    if not re.fullmatch(r'[a-f0-9]{32}',manifest.get('selected_catalog_guid') or '') or manifest['selected_catalog_guid']!=selected.get('guid') or type(manifest.get('selected_catalog_local_id'))is not int or manifest['selected_catalog_local_id']<=0:ins.issue('catalog_identity','Selected catalog GUID/local identity does not match pinned asset')
    return pins


def inspect_artifacts(ins,manifest):
    artifacts=keyed(manifest.get('artifacts'),'path',5000)
    for path,a in artifacts.items():
        try:
            count,stride,size=a.get('count'),a.get('stride'),a.get('bytes')
            if type(count)is not int or type(stride)is not int or count<0 or stride<=0 or count*stride!=size or size>MAX_ARTIFACT_BYTES:raise ValueError('Invalid artifact count/stride/size')
            if not isinstance(a.get('encoding'),str) or not a['encoding']:raise ValueError('Explicit artifact encoding required')
            target,actual=ins.verify_file(ins.bundle,path,a.get('sha256'),size)
            if actual['lfs']:raise ValueError('Artifact is an LFS pointer')
            if stride==47:
                import struct
                if not a['encoding'].startswith('le:i32 cell_x,i32 cell_z,i32 surface_id,i32 layer_id,f32 height,'):raise ValueError('Unknown surface sample schema')
                with target.open('rb') as stream:
                    for _ in range(count):
                        row=stream.read(47)
                        if not all(math.isfinite(v) for v in struct.unpack_from('<5f',row,16)):raise ValueError('Nonfinite decoded surface sample')
            ins.checked_artifacts[path]=(a,target)
        except (OSError,ValueError,TypeError) as exc:ins.issue('artifact_integrity',exc,source=path)
    return artifacts


def check_reference(ins,pins,path,field,scope):
    if path and path not in pins:ins.issue('reference_unpinned','Referenced source is not pinned',source=path,field=field,scope=scope)


def analyze_map(ins,m,pins,campaign,maps):
    for flag in ('authored_grid_available','decoded_surface_available','surface_grid_matches','campaign_physical_source_member'):
        if type(m.get(flag))is not bool:raise ValueError('Explicit map boolean required: '+flag)
    key=m['key'];source=m.get('definition_path','');result={'key':key,'source_path':source,'scene_guid':m.get('source_scene_guid'),
        'authored_grid':False,'surface_admission':False,'mask_topology':False,'scope':'Authored grid + native infantry cell admission only; no layered edge or runtime connectivity proof'}
    for field in ('definition_path','source_scene_path','grid_path','surface_path'):
        check_reference(ins,pins,m.get(field),field,'map:'+key)
    members=[r for r in campaign if not r.get('reasons') and r.get('scene_guid') and r['scene_guid']==m.get('source_scene_guid')]
    physical=[]
    for row in members:
        target=next((v for v in maps.values() if v.get('definition_path')==row.get('map_path')),None)
        if target and target.get('grid_path') and target.get('surface_path') and target.get('grid_path')==m.get('grid_path') and target.get('surface_path')==m.get('surface_path'):physical.append(row)
    if type(m.get('campaign_physical_source_member'))is not bool or m['campaign_physical_source_member']!=bool(physical):
        ins.issue('campaign_membership_mismatch','Physical Campaign membership differs from exact authored Menu rows',scope='map:'+key,source=source)
    if set(m.get('campaign_mission_ids') or [])!={r['mission_id'] for r in members}:
        ins.issue('campaign_membership_mismatch','Campaign mission IDs disagree',scope='map:'+key,source=source)
    result['campaign_physical_source_member']=bool(physical);result['campaign_scene_member']=bool(members)
    for reason in m.get('reasons') or []:ins.issue('map_export_blocker',reason,scope='map:'+key,source=source,severity='blocker')
    for field,stride in ARTIFACT_FIELDS.items():
        path=m.get(field)
        if path:
            entry=ins.checked_artifacts.get(path)
            if not entry or entry[0]['stride']!=stride:ins.issue('map_artifact_reference','Missing, invalid or wrong-stride map artifact',scope='map:'+key,source=path,field=field)
    if m.get('decoded_surface_available') or m.get('surface_grid_matches'):
        sw,sh=m.get('surface_width'),m.get('surface_height')
        if type(sw)is not int or type(sh)is not int or sw<=0 or sh<=0 or sw*sh>MAX_CELLS:raise ValueError('Invalid complete surface dimensions')
        if not all(finite(m.get(k)) for k in ('surface_origin_x','surface_origin_y','surface_origin_z','surface_cell_size')) or m['surface_cell_size']<=0:raise ValueError('Invalid surface frame')
        expected_match=bool(m.get('authored_grid_available')) and all(m.get(a)==m.get(b) for a,b in (('width','surface_width'),('height','surface_height'),('origin_x','surface_origin_x'),('origin_y','surface_origin_y'),('origin_z','surface_origin_z'),('cell_size','surface_cell_size')))
        if expected_match!=m['surface_grid_matches']:raise ValueError('Surface/grid match flag differs from actual frames')
    if m.get('decoded_surface_available'):
        for field in ('cells_artifact','samples_artifact','connections_artifact','compact_samples_artifact','infantry_surface_artifact'):
            if m.get(field) not in ins.checked_artifacts:raise ValueError('Decoded surface requires all complete binary tables')
        counts={field:ins.checked_artifacts[m[field]][0]['count'] for field in ('cells_artifact','samples_artifact','connections_artifact','compact_samples_artifact','infantry_surface_artifact')}
        if counts['infantry_surface_artifact']!=sw*sh:raise ValueError('Surface admission dimensions disagree')
        encoding=m.get('runtime_encoding')
        if type(encoding)is not int or encoding not in (0,1):raise ValueError('Unknown runtime surface encoding')
        if encoding==0:
            if counts['cells_artifact']!=sw*sh or counts['compact_samples_artifact']!=0:raise ValueError('Full surface encoding requires complete cell table and no compact samples')
        else:
            if counts['compact_samples_artifact']!=sw*sh or any(counts[field]!=0 for field in ('cells_artifact','samples_artifact','connections_artifact')):raise ValueError('Compact encoding requires exactly one sample per cell and empty full tables')
            if not finite(m.get('compact_min_height')) or not finite(m.get('compact_height_step')):raise ValueError('Nonfinite compact height metadata')
    elif m['surface_grid_matches']:
        ins.issue('surface_decode_unavailable','Surface asset frame matches grid but no decoded runtime blob is available',scope='map:'+key,source=m.get('surface_path',''),severity='blocker')
    if not m.get('authored_grid_available'):return result
    try:
        w,h=m.get('width'),m.get('height')
        if type(w)is not int or type(h)is not int or w<=0 or h<=0 or w*h>MAX_CELLS:raise ValueError('Grid dimensions exceed bounded full-grid profile')
        if not all(finite(m.get(k)) for k in ('origin_x','origin_y','origin_z','cell_size')) or m['cell_size']<=0:raise ValueError('Invalid coordinate frame')
        a,path=ins.checked_artifacts[m['walkable_artifact']]
        if a['count']!=w*h or a['stride']!=1 or a['encoding']!='u8 row-major x+z*width; authored GridAuthoringConfig only':raise ValueError('Full authored-grid encoding/count required')
        walk=path.read_bytes()
        if any(v>1 for v in walk):raise ValueError('Authored walkability must be binary')
        result.update(authored_grid=True,width=w,height=h,cells=w*h,walkable_cells=sum(walk),_walk=walk)
        if m.get('surface_grid_matches') and m.get('decoded_surface_available'):
            a,path=ins.checked_artifacts[m['infantry_surface_artifact']]
            if a['count']!=w*h or a['stride']!=1 or a['encoding']!='u8 row-major surface dimensions; MapSurfaceTraversalValidation.CanTraverse Infantry':raise ValueError('Full native infantry admission required')
            surface=path.read_bytes()
            if any(v>1 for v in surface):raise ValueError('Surface admission must be binary')
            cachekey=(m['walkable_artifact'],m['infantry_surface_artifact'],w,h)
            cached=ins.map_results.get(cachekey)
            if cached is None:
                ins.topology_cells+=w*h
                if ins.topology_cells>32*1024*1024:raise ValueError('Aggregate topology work exceeds 32 million cells; no partial topology claim')
                mask=bytes(a&b for a,b in zip(walk,surface));labels,sizes=components(mask,w,h)
                cached=(surface,labels,sizes);ins.map_results[cachekey]=cached
            surface,labels,sizes=cached
            result.update(surface_admission=True,mask_topology=True,surface_admitted_cells=sum(surface),joint_admitted_cells=sum(sizes),
                          mask_component_count=len(sizes),largest_mask_component=max(sizes,default=0),_surface=surface,_labels=labels)
    except (OSError,KeyError,ValueError,TypeError) as exc:ins.issue('map_grid_integrity',exc,scope='map:'+key,source=source)
    return result



def read_bounds(m,name):
    bounds=(m.get('metadata') or {}).get('bounds') or {};low=bounds.get(name+'Min');high=bounds.get(name+'Max')
    if not isinstance(low,dict) or not isinstance(high,dict):return None
    if not all(finite(v.get(c)) for v in (low,high) for c in ('x','z')) or low['x']>=high['x'] or low['z']>=high['z']:return None
    return low['x'],low['z'],high['x'],high['z']


def world_bounds_observation(m,x,z):
    result={}
    for name in ('world','playable'):
        bounds=read_bounds(m,name);result[name]=None if bounds is None else bounds[0]<=x<=bounds[2] and bounds[1]<=z<=bounds[3]
    return result


def cell_bounds_observation(m,x,z):
    # Real-number geometric cell envelope. Deliberately not a reimplementation of
    # Unity rounded float centers, footprint clearance, or runtime bounds policy.
    lowx=m['origin_x']+x*m['cell_size'];lowz=m['origin_z']+z*m['cell_size']
    highx=lowx+m['cell_size'];highz=lowz+m['cell_size'];result={}
    for name in ('world','playable'):
        bounds=read_bounds(m,name);result[name]=None if bounds is None else bounds[0]<=lowx and highx<=bounds[2] and bounds[1]<=lowz and highz<=bounds[3]
    return result

def point_observation(ins,p,m,analysis,mission_id,field,vector=''):
    object_value(p,field)
    result={'field':field,'id':p.get('id'),'role':p.get('role'),'coordinate_source':p.get('coordinate_source'),
            'world':[p.get('world_x'),p.get('world_z')],'cell':None,'in_bounds':None,'authored_walkable':None,'native_infantry_cell_admitted':None,'mask_component':None}
    if not finite(p.get('world_x')) or not finite(p.get('world_z')):raise ValueError('Nonfinite/invalid projected point')
    if type(p.get('has_cell'))is not bool or type(p.get('in_bounds'))is not bool:raise ValueError('Explicit cell projection flags required')
    if not p['has_cell']:
        ins.issue('point_unprojected','Native world/cell projection unavailable; authored coordinates not reinterpreted',scope='mission',catalog_id=mission_id,vector=vector,field=field,severity='blocker')
        return result
    x,z=p.get('cell_x'),p.get('cell_z')
    if type(x)is not int or type(z)is not int or not m or not analysis.get('authored_grid'):raise ValueError('Projected cell requires an exported complete grid')
    inside=0<=x<m['width'] and 0<=z<m['height']
    if inside!=p['in_bounds']:raise ValueError('Native cell/bounds flags disagree')
    result.update(cell=[x,z],in_bounds=inside)
    result['geometric_world_bounds']=world_bounds_observation(m,p['world_x'],p['world_z'])
    if any(v is False for v in result['geometric_world_bounds'].values()):ins.issue('point_outside_map_bounds','Desired point exceeds actual world/playable rectangle',scope='mission',catalog_id=mission_id,vector=vector,field=field,source=m.get('definition_path',''),severity='blocker')
    if not inside:
        ins.issue('point_out_of_grid','Authored desired point lies outside actual grid',scope='mission',catalog_id=mission_id,vector=vector,field=field,source=m.get('grid_path',''),severity='blocker')
    else:
        index=x+z*m['width'];result['authored_walkable']=bool(analysis['_walk'][index])
        if analysis.get('surface_admission'):
            result['native_infantry_cell_admitted']=bool(analysis['_surface'][index]);label=analysis['_labels'][index]
            result['mask_component']=label if label>=0 else None
    return result


def inspect_mission(ins,mission,canonical,maps,analyses,pins):
    sid=mission['canonical_id'];mapkey=mission.get('map_key');m=maps.get(mapkey);analysis=analyses.get(mapkey,{})
    source=(mission.get('definition_refs') or [''])[0]
    result={'canonical_id':sid,'canonical':canonical,'definition_refs':mission.get('definition_refs'), 'map_key':mapkey,
            'binding_state':'ExplicitCampaignMap' if m else ('LegacyRuntimeBindingNotCaptured' if mission.get('definition_refs') else 'DefinitionNotExported'),
            'points':[],'routes':[],'variants':[],'full_mission_simulated':False,'player_ready':False}
    if mapkey and not m:ins.issue('map_reference_missing','Mission references an absent map',catalog_id=sid,source=source)
    for path in array(mission.get('definition_refs'),'definition_refs',100):check_reference(ins,pins,path,'definition_refs','mission:'+sid)
    for reason in array(mission.get('reasons'),'reasons',1000):ins.issue('mission_export_blocker',reason,catalog_id=sid,source=source,severity='blocker')
    for field in ('objective_json','logical_binding_json'):
        value=mission.get(field)
        if value:
            if not isinstance(value,str):raise ValueError('Nested native JSON must be a string')
            strict_json(value.encode("utf-8"))
    anchors={}
    for kind in ('anchors','pads'):
        for i,entry in enumerate(array(mission.get(kind),kind,10000)):
            field=f'{kind}[{i}]';object_value(entry,field);authored=object_value(entry.get('authored'),field+'.authored')
            point=point_observation(ins,entry.get('projected'),m,analysis,sid,field);result['points'].append(point)
            if kind=='anchors':
                aid=authored.get('AnchorId')
                if not isinstance(aid,str) or not aid or aid in anchors:raise ValueError('Missing/duplicate anchor ID')
                anchors[aid]=point
    for route in array(mission.get('routes'),'routes',10000):
        object_value(route,'route');rid=route.get('RouteId');waypoints=array(route.get('WaypointAnchorIds'),'WaypointAnchorIds',10000)
        route_result={'route_id':rid,'waypoint_anchor_ids':waypoints,'segments':[],'scope':'Four-neighbour joint cell-mask topology; no layer/edge routing or vehicle/air certification'}
        for aid in waypoints:
            if aid not in anchors:ins.issue('route_anchor_missing','Unknown waypoint anchor: '+str(aid),catalog_id=sid,source=source,field='route:'+str(rid),severity='blocker')
        for first,second in zip(waypoints,waypoints[1:]):
            a,b=anchors.get(first),anchors.get(second);same=None
            if a and b and a['mask_component'] is not None and b['mask_component'] is not None:same=a['mask_component']==b['mask_component']
            route_result['segments'].append({'from':first,'to':second,'same_four_neighbour_mask_component':same})
        result['routes'].append(route_result)
    variants={}
    for v in array(mission.get('variants'),'variants',12):
        object_value(v,'variant');key=(v.get('size'),v.get('difficulty'))
        if key not in VARIANTS or key in variants:raise ValueError('Unknown/duplicate size/difficulty vector')
        variants[key]=v
    for size,difficulty in VARIANTS:
        key=(size,difficulty);vector=size+'/'+difficulty;v=variants.get(key)
        if v is None:
            ins.issue('variant_missing','Expected one of twelve size/difficulty vectors',catalog_id=sid,vector=vector)
            continue
        for flag in ('compile_success','static_budget_available','static_budget_exact'):
            if type(v.get(flag))is not bool:raise ValueError('Explicit variant boolean required: '+flag)
        if type(v.get('seed'))is not int or v['seed']!=0:raise ValueError('Schema v1 exports exact seed 0 only')
        forces=array(v.get('forces'),'forces',10000);structures=array(v.get('structures'),'structures',10000)
        points=[]
        for field,entries in (('desired_force_positions',forces),('desired_structure_positions',structures)):
            positions=array(v.get(field),field,10000)
            if len(entries)!=len(positions):raise ValueError('Desired-position count differs from resolved entries')
            for i,p in enumerate(positions):points.append(point_observation(ins,p,m,analysis,sid,f'{field}[{i}]',vector))
        for i,p in enumerate(array(v.get('objective_area_points',[]),'objective_area_points',10000)):
            points.append(point_observation(ins,p,m,analysis,sid,f'objective_area_points[{i}]',vector))
        roles=[]
        for force in forces:
            object_value(force,'force');role=force.get('RoleKind');qty=force.get('Quantity')
            if type(role)is not int or not 0<=role<=23 or type(qty)is not int or qty<0:raise ValueError('Invalid resolved role/quantity')
            check_reference(ins,pins,force.get('SourceConfigPath'),'SourceConfigPath','mission:'+sid)
            roles.append(role)
        blockers=list(REPLAY_GAPS)
        if not v['compile_success']:blockers.append('Native setup compile did not succeed')
        if not m:blockers.append('Explicit Campaign source/grid binding not captured')
        elif not m.get('campaign_physical_source_member'):blockers.append('Physical source is outside authored Menu Campaign membership')
        if not analysis.get('surface_admission'):blockers.append('Complete matching grid/native infantry surface admission unavailable')
        if m and (m.get('width',0)>64 or m.get('height',0)>64):blockers.append('Current bounded world replay supports at most 64x64; no crop authorized')
        unsupported=sorted(set(r for r in roles if r not in (1,2,3,4,5)))
        if unsupported:blockers.append('Unsupported world replay role kinds: '+','.join(map(str,unsupported)))
        if canonical.get('objective_id')!='BA':blockers.append('Current world replay lacks this complete objective collector: '+str(canonical.get('objective_id')))
        result['variants'].append({'size':size,'difficulty':difficulty,'seed':0,'compile_success':v['compile_success'],
            'compile_reasons':array(v.get('compile_reasons'),'compile_reasons',10000),'static_budget_available':v['static_budget_available'],
            'static_budget_exact':v['static_budget_exact'],'static_budget_errors':array(v.get('static_budget_errors'),'static_budget_errors',10000),
            'force_entries':len(forces),'force_quantity':sum(f['Quantity'] for f in forces),'structure_entries':len(structures),
            'role_kinds':sorted(set(roles)),'desired_points':points,'replay_eligible':False,'replay_blockers':blockers})
    return result



def exact_keys(value,keys,field):
    object_value(value,field)
    if set(value)!=set(keys):raise ValueError(field+' has missing or unknown fields')


def inspect_draft(ins,draft,manifest_sha,maps,analyses):
    exact_keys(draft,('schema_version','manifest_sha256','missions'),'draft')
    if type(draft['schema_version'])is not int or draft['schema_version']!=1 or draft['manifest_sha256']!=manifest_sha:raise ValueError('Draft must name exact schema and current export hash')
    result={'scope':'DraftPlacementPrevalidation','role_vocabulary':'StaticProbeRoleV1: analysis purposes, not native mission RoleId certification','native_compiled_setup':False,'runtime_spawn_validated':False,'player_ready':False,'missions':[]}
    seen=set();totalpoints=0;draft_start=len(ins.findings)
    for entry in array(draft['missions'],'draft.missions',120):
        exact_keys(entry,('canonical_id','map_key','points','routes'),'draft mission');sid=entry['canonical_id'];key=entry['map_key']
        if sid not in EXPECTED_IDS or sid in seen:raise ValueError('Draft requires unique canonical mission IDs')
        seen.add(sid);m=maps.get(key);analysis=analyses.get(key,{})
        if not m or read_bounds(m,'world') is None or read_bounds(m,'playable') is None or not analysis.get('campaign_physical_source_member') or not analysis.get('authored_grid') or not analysis.get('surface_admission'):raise ValueError('Draft requires exact exported Campaign-member map with complete matching grid/surface')
        points={};out={'canonical_id':sid,'map_key':key,'source_path':m['definition_path'],'points':[],'routes':[]}
        for p in array(entry['points'],'draft.points',4096):
            exact_keys(p,('id','role','cell'),'draft point');pid=p['id'];role=p['role'];cell=p['cell'];totalpoints+=1
            if totalpoints>65536:raise ValueError('Aggregate draft points exceed65536')
            if not isinstance(pid,str) or not pid or len(pid)>256 or pid in points or role not in DRAFT_ROLES:raise ValueError('Draft point requires unique ID and explicit role')
            if not isinstance(cell,list) or len(cell)!=2 or any(type(v)is not int or abs(v)>2147483647 for v in cell):raise ValueError('Draft cell must contain two bounded integers')
            x,z=cell;inside=0<=x<m['width'] and 0<=z<m['height'];label=None
            observation={'id':pid,'role':role,'cell':cell,'in_bounds':inside,'authored_walkable':None,'native_infantry_cell_admitted':None,'mask_component':None}
            if inside:
                at=x+z*m['width'];label=analysis['_labels'][at];observation.update(authored_walkable=bool(analysis['_walk'][at]),native_infantry_cell_admitted=bool(analysis['_surface'][at]),mask_component=label if label>=0 else None)
            else:ins.issue('draft_point_out_of_grid','Draft cell outside actual full grid',scope='draft',catalog_id=sid,source=m['grid_path'],field=pid,severity='blocker')
            if inside and role=='infantry_anchor' and (not observation['authored_walkable'] or not observation['native_infantry_cell_admitted']):
                ins.issue('draft_infantry_cell_rejected','Authored walkability/native single-cell admission rejects this draft infantry anchor; final spawn placement not tested',scope='draft',catalog_id=sid,source=m['grid_path'],field=pid,severity='blocker')
            observation['geometric_cell_envelope']=cell_bounds_observation(m,x,z)
            if any(value is False for value in observation['geometric_cell_envelope'].values()):ins.issue('draft_cell_outside_map_bounds','Cell envelope exceeds exported world/playable rectangle; conservative geometry test, no footprint legality claim',scope='draft',catalog_id=sid,source=m['definition_path'],field=pid,severity='blocker')
            points[pid]=observation;out['points'].append(observation)
        routes=set()
        for route in array(entry['routes'],'draft.routes',4096):
            exact_keys(route,('id','waypoints'),'draft route');rid=route['id'];waypoints=array(route['waypoints'],'draft waypoints',4096)
            if not isinstance(rid,str) or not rid or rid in routes or len(waypoints)<2 or any(not isinstance(v,str) or v not in points for v in waypoints):raise ValueError('Draft route requires unique ID and at least two existing point IDs')
            routes.add(rid);segments=[]
            for a,b in zip(waypoints,waypoints[1:]):
                first,last=points[a]['mask_component'],points[b]['mask_component']
                same=None if first is None or last is None else first==last
                infantry_probe=points[a]['role']==points[b]['role']=='infantry_anchor'
                segments.append({'from':a,'to':b,'same_four_neighbour_mask_component':same,'analytical_infantry_probe':infantry_probe})
                if infantry_probe and same is not True:ins.issue('draft_infantry_mask_segment_rejected','Explicit infantry-anchor pair has unadmitted endpoint or disconnected four-neighbour cell masks; not a native pathfinding outcome',scope='draft',catalog_id=sid,source=m['grid_path'],field=rid+':'+a+'->'+b,severity='blocker')
            out['routes'].append({'id':rid,'segments':segments,'scope':'Four-neighbour mask topology only; excludes layered edges, diagonal rules, footprint and runtime blockers'})
        result['missions'].append(out)
    allpoints=[p for mission in result['missions'] for p in mission['points']]
    allsegments=[segment for mission in result['missions'] for route in mission['routes'] for segment in route['segments']]
    issues=ins.findings[draft_start:]
    result['summary']={'missions':len(result['missions']),'points':len(allpoints),'segments':len(allsegments),
        'outside_grid':sum(not p['in_bounds'] for p in allpoints),'outside_world_or_playable_cell_envelope':sum(any(v is False for v in p['geometric_cell_envelope'].values()) for p in allpoints),
        'rejected_infantry_anchor_cells':sum(p['role']=='infantry_anchor' and p['in_bounds'] and (not p['authored_walkable'] or not p['native_infantry_cell_admitted']) for p in allpoints),
        'disconnected_mask_segments_observed':sum(p['same_four_neighbour_mask_component'] is False for p in allsegments),
        'unresolved_mask_segments_observed':sum(p['same_four_neighbour_mask_component'] is None for p in allsegments),
        'static_probe_findings':len(issues)}
    result['findings']=issues
    return result

def inspect(project,bundle,native_receipt=None,require_native=False,draft=None):
    ins=Inspection(project,bundle);manifest={};reports=[];analyses={};native_verified=False;manifest_sha=None;draft_result=None
    try:
        manifest_path=ins.bundle/'manifest.json';manifest=ins.read_document(manifest_path);manifest_sha=ins.digest(manifest_path)['sha256']
        receipt=ins.read_document(ins.bundle/'receipt.json');ins.digest(ins.bundle/'receipt.json')
        if manifest.get('schema_version')!=1 or manifest.get('producer')!=PRODUCER or manifest.get('required_pass_marker')!=MARKER:raise ValueError('Unsupported producer/schema/pass marker')
        if receipt.get('schema_version')!=1 or receipt.get('manifest_sha256')!=manifest_sha or receipt.get('source_pins_unchanged')is not True or receipt.get('required_pass_marker')!=MARKER:raise ValueError('Native export integrity receipt mismatch')
        if native_receipt:
            proof=ins.read_document(native_receipt);ins.digest(Path(native_receipt))
            if proof.get('schema_version')!=1 or proof.get('manifest_sha256')!=manifest_sha or type(proof.get('wrapper_exit'))is not int or proof.get('wrapper_exit')!=0 or proof.get('reaped')is not True or proof.get('required_pass_marker')!=MARKER:raise ValueError('Verified native run receipt mismatch')
            log,record=ins.verify_file(ins.project,proof.get('native_log_path'),proof.get('native_log_sha256'))
            if record['bytes']>MAX_ARTIFACT_BYTES:raise ValueError('Native log exceeds bounded read limit')
            raw=log.read_bytes()
            if hashlib.sha256(raw).hexdigest()!=record['sha256'] or MARKER.encode() not in raw:raise ValueError('Native log changed or missing fixed export pass marker')
            native_verified=True
        if require_native and not native_verified:ins.issue('native_run_unverified','Verified wrapper receipt required')
        for reason in array(manifest.get('reasons'),'reasons',10000):ins.issue('export_coverage_blocker',reason,severity='blocker')
        pins=inspect_sources(ins,manifest);inspect_artifacts(ins,manifest)
        diagnostic=Diagnostics(ins.project);canonical_result=diagnostic.canonical(ins.project/CATALOG,ins.project/MATRIX)
        for error in diagnostic.errors:ins.issue('canonical_'+error['code'],error['message'],source=error['source'],field=error['location'])
        canonical={r['scenario_id']:r for r in canonical_result[0]} if canonical_result else {}
        campaign=array(manifest.get('campaign_rows'),'campaign_rows',10000)
        for row in campaign:
            object_value(row,'campaign_row')
            for field in ('definition_path','scenario_path','map_path'):check_reference(ins,pins,row.get(field),field,'Campaign:'+str(row.get('mission_id')))
        maps=keyed(manifest.get('maps'),'key',1000)
        for key,m in maps.items():
            try:
                analyses[key]=analyze_map(ins,m,pins,campaign,maps)
                analyses[key]['typed_map_anchors']=[point_observation(ins,p,m,analyses[key],'','map:'+key+'/anchor:'+str(p.get('id'))) for p in array(m.get('map_anchors',[]),'map_anchors',10000)]
            except (OSError,ValueError,TypeError,KeyError) as exc:ins.issue('map_schema',exc,scope='map:'+key)
        missions=keyed(manifest.get('missions'),'canonical_id',120)
        for unknown in sorted(set(missions)-set(EXPECTED_IDS)):ins.issue('unknown_mission',unknown)
        for sid in EXPECTED_IDS:
            if sid not in missions:
                ins.issue('mission_missing','All120 canonical IDs must have explicit records',catalog_id=sid)
                reports.append({'canonical_id':sid,'binding_state':'RecordMissing','variants':[],'replay_eligible':False});continue
            try:reports.append(inspect_mission(ins,missions[sid],canonical.get(sid,{}),maps,analyses,pins))
            except (OSError,ValueError,TypeError,KeyError) as exc:
                ins.issue('mission_schema',exc,catalog_id=sid)
                reports.append({'canonical_id':sid,'binding_state':'InvalidRecord','variants':[],'replay_eligible':False})
    except (OSError,ValueError,TypeError,KeyError) as exc:ins.issue('export_integrity',exc)
    if draft is not None and ins.integrity_ok:
        try:draft_result=inspect_draft(ins,ins.read_document(draft),manifest_sha,maps,analyses)
        except (OSError,ValueError,TypeError,KeyError) as exc:ins.issue('draft_schema',exc,scope='draft')
    ins.recheck_stamps()
    variants=[v for m in reports for v in m.get('variants',[])]
    publicmaps=[{k:v for k,v in a.items() if not k.startswith('_')} for a in analyses.values()]
    campaign=manifest.get('campaign_rows') or []
    return {'schema_version':1,'scope':'AuthoredMenuBindings_StaticPrevalidationOnly','catalog_selection':manifest.get('catalog_selection'),'compiler_catalog_scope':manifest.get('compiler_catalog_scope'),'selected_catalog_guid':manifest.get('selected_catalog_guid'),'selected_catalog_local_id':manifest.get('selected_catalog_local_id'),'manifest_sha256':manifest_sha,
            'integrity_valid':ins.integrity_ok,'native_run_verified':native_verified,'full_mission_simulated':False,'player_ready':False,
            'coverage':{'canonical_expected':120,'canonical_records':len(reports),'variant_expected':1440,'variant_records':len(variants),
                'compiled_variants':sum(v['compile_success'] for v in variants),'static_budget_variants':sum(v['static_budget_available'] for v in variants),
                'campaign_rows':len(campaign),'campaign_physical_scene_guids':len({r.get('scene_guid') for r in campaign if isinstance(r,dict) and r.get('scene_guid') and not r.get('reasons')}),
                'map_records':len(publicmaps),'complete_authored_grids':sum(m['authored_grid'] for m in publicmaps),
                'resolved_virtual_packages':{name:root.relative_to(ins.project).as_posix() for name,root in sorted(ins.package_roots.items())},'hashed_input_files':len(ins.hashes),'hashed_input_bytes':ins.total_hash_bytes,'matching_surface_maps':sum(m['surface_admission'] for m in publicmaps),'cached_mask_topologies':len(ins.map_results),
                'explicit_mission_map_bindings':sum(m.get('binding_state')=='ExplicitCampaignMap' for m in reports),'replay_eligible_variants':0},
            'findings':ins.findings,'maps':publicmaps,'missions':reports,'draft':draft_result}



def safe_output(project,bundle,output,inputs):
    import tempfile
    target=Path(output).resolve();project=Path(project).resolve();bundle=Path(bundle).resolve()
    roots=[(project/'Logs').resolve(),Path(tempfile.gettempdir()).resolve(),Path('/private/tmp').resolve()]
    if not any(target.is_relative_to(root) and target!=root for root in roots):raise ValueError('Output must be a new JSON file under project Logs or system temporary directory')
    if target.is_relative_to(bundle) or target.exists() or target.suffix.lower()!='.json' or target in {Path(p).resolve() for p in inputs if p}:raise ValueError('Output must not replace export, proof, draft or existing files')
    for folder in ('Assets','Packages','ProjectSettings','Design'):
        if target.is_relative_to((project/folder).resolve()):raise ValueError('Output cannot modify project source')
    return target

def main(argv=None):
    parser=argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--project',type=Path,default=Path(__file__).resolve().parents[2]);parser.add_argument('--bundle',type=Path,required=True)
    parser.add_argument('--draft',type=Path,help='Strict cell-coordinate logical draft probes; no source/budget overrides');parser.add_argument('--native-receipt',type=Path);parser.add_argument('--require-native-verified',action='store_true');parser.add_argument('--output',type=Path,required=True)
    args=parser.parse_args(argv)
    try:args.output=safe_output(args.project,args.bundle,args.output,(args.native_receipt,args.draft))
    except ValueError as exc:parser.error(str(exc))
    started=time.perf_counter();report=inspect(args.project,args.bundle,args.native_receipt,args.require_native_verified,args.draft)
    args.output.parent.mkdir(parents=True,exist_ok=True);
    with args.output.open('x',encoding='utf-8') as stream:stream.write(json.dumps(report,indent=2,sort_keys=True,allow_nan=False)+'\n')
    c=report['coverage'];print(f"integrity={'valid' if report['integrity_valid'] else 'FAILED'} native_verified={report['native_run_verified']} missions={c['canonical_records']}/120 variants={c['variant_records']}/1440 compiled={c['compiled_variants']} Campaign_scenes={c['campaign_physical_scene_guids']} explicit_bindings={c['explicit_mission_map_bindings']} replay_eligible=0 elapsed={time.perf_counter()-started:.3f}s")
    if report['draft'] is not None:print('Draft-only observations: '+json.dumps(report['draft']['summary'],sort_keys=True))
    print(f"Static authored prevalidation only; placement/source blockers={sum(f['severity']=='blocker' for f in report['findings'])}; no mission readiness.")
    return 0 if report['integrity_valid'] else 2

if __name__=='__main__':raise SystemExit(main())
