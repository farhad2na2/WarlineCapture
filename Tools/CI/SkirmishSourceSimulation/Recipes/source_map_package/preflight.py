#!/usr/bin/env python3
"""Fail-closed, source-only preflight for source-derived Campaign map packages."""
from __future__ import annotations
import argparse, base64, hashlib, json, math, struct, sys
from pathlib import Path

KIND = "SourceDerivedCampaignMapPackage"

def _fail(message): raise ValueError(message)
def _read(path):
    try: return Path(path).read_bytes()
    except OSError as e: _fail(f"cannot read {path}: {e}")
def _unique_object(pairs):
    result={}
    for k,v in pairs:
        if k in result: _fail(f"duplicate JSON property: {k}")
        result[k]=v
    return result
def _json(path):
    try: return json.loads(_read(path),object_pairs_hook=_unique_object)
    except (json.JSONDecodeError, UnicodeDecodeError) as e: _fail(f"invalid JSON {path}: {e}")
def _sha(path): return hashlib.sha256(_read(path)).hexdigest()
def _no_symlink(path):
    p=Path(path)
    if not p.is_absolute(): return False
    cur=Path(p.anchor)
    for part in p.parts[1:]:
        cur=cur/part
        if cur.is_symlink(): return False
    return True
def _validate_binary_mask(raw, meta, cells, label, owned=False):
    if not isinstance(meta,dict) or len(raw)!=cells or meta.get('byteCount')!=cells or meta.get('count')!=cells: _fail(f"mask length/count differs from frame: {label}")
    if owned and (meta.get('present') is not True or meta.get('ownerReady') is not True or meta.get('actualLength')!=cells or meta.get('expectedLength')!=cells): _fail(f"mask ownership/length evidence incomplete: {label}")
    if any(b not in (0,1) for b in raw): _fail(f"mask has non-bool byte: {label}")
    if hashlib.sha256(raw).hexdigest()!=meta.get('sha256'): _fail(f"mask hash mismatch: {label}")
def _validate_clearance_descriptor(descriptor, capture_pin, static_map, frame, water_descriptor, record):
    _pin(descriptor,'clearance descriptor')
    projection=_json(descriptor['path'])
    # Clearance projection is a separate schema with no mutable readiness claims.
    expected={'kind','schema_version','package_capture','static_map','frame','collector','water_descriptor','scope','live_occupancy_qualified','native_presentation_qualified','source_fit_certified','mission_ready'}
    if set(projection)!=expected: _fail('clearance projection keys differ from schema')
    if projection.get('kind')!='SourceDerivedCampaignMapDropClearanceProjection' or projection.get('schema_version')!=1: _fail('unsupported clearance projection kind/schema')
    if any(projection.get(k) is not False for k in ('live_occupancy_qualified','native_presentation_qualified','source_fit_certified','mission_ready')): _fail('clearance projection must remain explicitly unqualified')
    if projection.get('package_capture')!=capture_pin: _fail('clearance projection capture pin mismatch')
    expected_map={'operation_map_id':static_map['operation_map_id'],'definition':static_map['definition']}
    if projection.get('static_map')!=expected_map or projection.get('frame')!=frame: _fail('clearance projection static map/frame mismatch')
    if projection.get('water_descriptor')!=water_descriptor: _fail('clearance projection water descriptor mismatch')
    nested=[projection['package_capture'],projection['static_map']['definition'],projection['water_descriptor']]
    for i,pin in enumerate(nested): _pin(pin,f'clearance nested pin {i}')
    collector=projection.get('collector',{})
    if set(collector)!={'authority','query_rect','record_sha256','grid_width','grid_height','cell_size','grid_origin'} or collector.get('authority')!='OriginalRenderBoundsPresentation.CollectStaticBounds_SourceBakedDomain': _fail('clearance projection collector identity invalid')
    if (collector['grid_width'],collector['grid_height'],collector['cell_size'])!=(frame['width'],frame['height'],frame['cell_size']) or _finite_vec(collector['grid_origin'],3,'collector origin')!=_finite_vec(frame['origin'],3,'frame origin'): _fail('clearance projection collector frame mismatch')
    # Match the original owner's float32 evaluation order at both grid edges.
    f32=lambda x: struct.unpack('<f',struct.pack('<f',x))[0]
    safe=max(f32(.01),f32(frame['cell_size']))
    rect=_finite_vec(collector['query_rect'],4,'collector query rectangle')
    for axis,extent,origin in ((0,frame['width'],frame['origin'][0]),(1,frame['height'],frame['origin'][2])):
        low=f32(f32(origin)+f32(-98*safe))
        edge=f32(f32(origin)+f32((extent-1-98)*safe))
        high=f32(edge+f32(197*safe))
        if f32(rect[axis])>low or f32(f32(rect[axis])+f32(rect[axis+2]))<high: _fail('clearance collector query omits original expanded domain')
    digest=hashlib.sha256(json.dumps(record,sort_keys=True,separators=(',',':'),allow_nan=False).encode()).hexdigest()
    if collector.get('record_sha256')!=digest or collector.get('query_rect')!=record.get('query_rect'): _fail('clearance projection source record mismatch')
    return projection,nested
def _pin(d, label):
    if not isinstance(d, dict) or not isinstance(d.get('path'), str) or not isinstance(d.get('sha256'), str): _fail(f"{label}: expected path and sha256")
    path=Path(d['path'])
    if not _no_symlink(path) or not path.is_file(): _fail(f"{label}: expected absolute regular file without symlink: {path}")
    got = _sha(path)
    if got != d['sha256']: _fail(f"{label}: SHA-256 mismatch for {d['path']}")
    return got
def _finite_vec(v,n,label):
    if not isinstance(v,(list,tuple)) or len(v)!=n or any(not isinstance(x,(int,float)) or not math.isfinite(x) for x in v): _fail(f"{label}: expected {n} finite numbers")
    return [float(x) for x in v]
def _frame_equal(a,b):
    return (a.get('width')==b.get('width') and a.get('height')==b.get('height') and
            a.get('cell_size')==b.get('cell_size') and _finite_vec(a.get('origin'),3,'frame origin')==_finite_vec(b.get('origin'),3,'capture origin'))

def preflight_package(package):
    """Validate package and all pinned source material. Returns an unqualified report; never creates a world."""
    package_file=Path(package).absolute() if isinstance(package,(str,Path)) else None
    if package_file is not None and (not _no_symlink(package_file) or not package_file.is_file()): _fail('package must be an absolute regular file without symlink')
    p = _json(package_file) if package_file is not None else package
    if not isinstance(p,dict) or p.get('kind')!=KIND or p.get('schema_version')!=1: _fail('unsupported package kind/schema')
    required={'kind','schema_version','capture','source_root','external_inputs','static_map','frame','water_descriptor','clearance_descriptor','mission_initialized','mission_ready','source_fit_certified','live_occupancy_measured'}
    if set(p)!=required: _fail(f"package keys differ from schema: missing={sorted(required-set(p))} extra={sorted(set(p)-required)}")
    for flag in ('mission_initialized','mission_ready','source_fit_certified','live_occupancy_measured'):
        if p.get(flag) is not False: _fail(f"{flag} must be explicitly false for source-only package")
    cap_path=p.get('capture',{}).get('path'); _pin(p.get('capture'), 'capture')
    c=_json(cap_path); root=Path(p.get('source_root',''))
    if not _no_symlink(root) or not root.is_dir(): _fail('source_root must be an absolute directory without symlink')
    root=root.resolve()
    ext=p.get('external_inputs')
    if not isinstance(ext,dict): _fail('external_inputs must be keyed by the capture recorded pin path')
    external=ext
    pins=c.get('readPins')
    if not isinstance(pins,dict) or not pins: _fail('capture readPins missing')
    # Rebase captured project-absolute paths onto the declared relocated source root.
    verified=0; skipped=[]; resolved_inputs=[]
    for original,digest in pins.items():
        if not isinstance(digest,str) or len(digest)!=64: _fail(f"malformed source pin: {original}")
        captured_root=Path(c.get('capturedProjectRoot','')).resolve()
        if not Path(original).is_absolute(): rel=Path(original)
        else:
            try: rel=Path(original).relative_to(captured_root)
            except ValueError: rel=None
        candidate=(root/rel) if rel is not None else Path(original)
        if candidate.is_file() and _no_symlink(candidate):
            if _sha(candidate)!=digest: _fail(f"source pin mismatch: {original} -> {candidate}")
            verified+=1
            resolved_inputs.append({'path':str(candidate.absolute()),'sha256':digest})
        elif original in external:
            _pin(external[original],f"external source pin {original}")
            if external[original]['sha256']!=digest: _fail(f"external pin differs from capture: {original}")
            verified+=1
            resolved_inputs.append({'path':str(Path(external[original]['path']).absolute()),'sha256':digest})
        elif '/Library/ScriptAssemblies/' in original and original.endswith('.dll'):
            skipped.append(original)  # Historical producer binaries are not source closure.
        else: _fail(f"source pin unavailable: {original}")
    sm=p.get('static_map'); fm=p.get('frame')
    if not isinstance(sm,dict) or not isinstance(fm,dict): _fail('static_map/frame missing')
    d=sm.get('definition'); _pin(d,'static map definition')
    if sm.get('operation_map_id')!=c.get('operationMapId'): _fail('static map operation id differs from capture')
    mono=c.get('sourceMetadata',{}).get('MonoBehaviour',{})
    if mono.get('operationMapId')!=sm.get('operation_map_id'): _fail('selected map metadata operation id mismatch')
    identity=mono.get('sourceIdentityHash')
    if not identity or d.get('sha256')!=c.get('sourceSha256'): _fail('capture source identity/definition mismatch')
    cf=c['fixedMapReadDomain']['gridFrame']
    cframe={'width':cf['width'],'height':cf['height'],'cell_size':cf['cellSize'],'origin':[cf['origin'][k] for k in ('x','y','z')]}
    if not _frame_equal(fm,cframe): _fail('package frame differs from captured grid frame')
    cells=fm['width']*fm['height']
    hi=c['headlessPlacementInputs']
    if hi.get('operationMapId')!=sm['operation_map_id'] or hi.get('sourceDefinitionSha256')!=d['sha256']: _fail('mask source/map identity mismatch')
    masks=[]; mask_hashes={}
    for name in ('gridWalkable','gridRoad','authoredBuildings','authoredRoadsIncludingSidewalks','sidewalks'):
        m=hi.get(name)
        if not isinstance(m,dict) or not isinstance(m.get('base64'),str): _fail(f"required mask missing: {name}")
        try: raw=base64.b64decode(m['base64'],validate=True)
        except Exception: _fail(f"invalid base64 mask: {name}")
        expected_fields=('present','ownerReady','actualLength','expectedLength') if name in ('authoredBuildings','authoredRoadsIncludingSidewalks','sidewalks') else ()
        _validate_binary_mask(raw,m,cells,name,owned=bool(expected_fields))
        masks.append(name)
        mask_hashes[name]=hashlib.sha256(raw).hexdigest()
    dom=c['fixedMapReadDomain']; nav=dom.get('authoredNavigationBlockers')
    if not isinstance(nav,list) or not nav: _fail('authored navigation blockers missing')
    seen=set()
    for row in nav:
        if not isinstance(row,dict) or not isinstance(row.get('entity_enabled'),bool) or row.get('id') in seen: _fail('invalid/duplicate navigation blocker')
        seen.add(row['id']); comps=row.get('components',{})
        for typ,field in [('Game.Components.UnitGrid','Game.Components.UnitGrid::Cell'),('Game.Components.GridBlockerSize','Game.Components.GridBlockerSize::Size')]:
            comp=comps.get(typ,{})
            if comp.get('present') is not True: _fail(f"navigation component absent: {typ}")
            try:
                f=comp['value']['fields'][field]['fields']; xy=[int(f[f'Unity.Mathematics.int2::{axis}']['value']) for axis in ('x','y')]
            except Exception: _fail(f"navigation data malformed: {typ}")
            if field.endswith('Cell'):
                cell=xy
            else: size=xy
        if size[0]<=0 or size[1]<=0: _fail('navigation footprint has nonpositive dimensions')
        if comps.get('Game.Components.StaticGridBlocker',{}).get('present') is not True: _fail('navigation static blocker tag absent')
    clear=dom.get('dropClearanceBounds'); bounds=clear.get('bounds') if isinstance(clear,dict) else None
    if not isinstance(bounds,list) or not bounds: _fail('clearance bounds missing')
    if (clear.get('grid_width'),clear.get('grid_height'),clear.get('cell_size'))!=(fm['width'],fm['height'],fm['cell_size']): _fail('clearance frame mismatch')
    if _finite_vec(clear.get('grid_origin'),3,'clearance grid origin')!=_finite_vec(fm['origin'],3,'package grid origin'): _fail('clearance origin mismatch')
    rect=_finite_vec(clear.get('query_rect'),4,'clearance query_rect')
    if rect[2]<=0 or rect[3]<=0 or rect[0]>fm['origin'][0] or rect[1]>fm['origin'][2] or rect[0]+rect[2]<fm['origin'][0]+fm['width']*fm['cell_size'] or rect[1]+rect[3]<fm['origin'][2]+fm['height']*fm['cell_size']: _fail('clearance query does not cover the full map frame')
    for b in bounds:
        ce=_finite_vec(b.get('center'),3,'clearance center'); ex=_finite_vec(b.get('extents'),3,'clearance extents')
        if min(ex)<0 or ce[0]+ex[0]<rect[0] or ce[0]-ex[0]>=rect[0]+rect[2] or ce[2]+ex[2]<rect[1] or ce[2]-ex[2]>=rect[1]+rect[3]: _fail('clearance bound is invalid or outside collector query')
    def check_bits_vector(v,label):
        for axis in ('x','y','z'):
            word=v.get(axis+'Bits')
            if not isinstance(word,str) or len(word)!=8: _fail(f"{label}: exact raw float bits missing")
            try: raw=struct.unpack('<f',struct.pack('<I',int(word,16)))[0]
            except (ValueError,struct.error): _fail(f"{label}: invalid raw float bits")
            if not math.isfinite(raw) or struct.pack('<f',raw)!=struct.pack('<f',float(v[axis])): _fail(f"{label}: value/raw bits mismatch")
    for footprint in dom.get('waterFootprints',[]):
        for area in ('localBounds','worldBounds'):
            for vector in ('center','extents'): check_bits_vector(footprint[area][vector],f"water {area} {vector}")
    # Validate transform float words without erasing captured signed-zero bits.
    signed_zero=0
    for footprint in dom.get('waterFootprints',[]):
        for item in footprint.get('worldToLocalRowMajor',[]):
            bits=item.get('bits','')
            if len(bits)!=8: _fail('water transform raw f32 bits missing')
            try: decoded=struct.unpack('<f',struct.pack('<I',int(bits,16)))[0]
            except (ValueError,struct.error): _fail('invalid water transform raw bits')
            value=item.get('value')
            if not math.isfinite(decoded) or not isinstance(value,(int,float)) or not math.isfinite(value): _fail('nonfinite water transform')
            encoded=f"{struct.unpack('<I',struct.pack('<f',float(value)))[0]:08x}"
            if encoded!=bits:
                if float(value)==0.0 and bits.lower()=='80000000': signed_zero+=1
                else: _fail('water transform JSON value/raw bits mismatch')
    wd=p.get('water_descriptor'); _pin(wd,'water descriptor'); wr=_json(wd['path'])
    if wr.get('kind')!='SourceDerivedCampaignWaterGeometry' or wr.get('schema_version')!=1 or wr.get('active_mission_water_qualified') is not False or wr.get('source_fit_certified') is not False or wr.get('mission_ready') is not False: _fail('water descriptor must be source-derived and explicitly unqualified')
    if wr.get('source_map')!=sm or wr.get('frame')!=fm: _fail('water descriptor map/frame mismatch')
    if wr.get('operation_map_id')!=sm['operation_map_id'] or wr.get('capture')!=p['capture']: _fail('water descriptor capture/map identity mismatch')
    clearance_projection,clearance_nested=_validate_clearance_descriptor(p['clearance_descriptor'],p['capture'],sm,fm,wd,clear)
    nested_pins=[]
    for key in ('capture_provenance','kernel_build_proof','kernel_run_log','bounds_audit'):
        nested=wr.get(key)
        if not isinstance(nested,dict): _fail(f"water descriptor nested evidence missing: {key}")
        _pin(nested,f"water {key}"); nested_pins.append({'path':str(Path(nested['path']).resolve()),'sha256':nested['sha256']})
    water_bytes=wr.get('mask',{}).get('bytes')
    if water_bytes!=cells or not isinstance(wr.get('mask',{}).get('sha256'),str): _fail('water descriptor mask size/hash missing')
    water_mask_path=wr.get('mask',{}).get('path')
    if not isinstance(water_mask_path,str): _fail('water descriptor mask path missing')
    water_file=Path(water_mask_path)
    if not _no_symlink(water_file) or not water_file.is_file(): _fail('water mask must be an absolute regular file without symlink')
    mask=_read(water_file)
    water_meta={'sha256':wr['mask']['sha256'],'bytes':wr['mask']['bytes'],'byteCount':wr['mask']['bytes'],'count':wr['mask']['bytes']}
    _validate_binary_mask(mask,water_meta,cells,'source-derived water')
    if wr.get('footprint_count')!=len(dom.get('waterFootprints',[])): _fail('water descriptor footprint count mismatch')
    verified_inputs=resolved_inputs
    verified_inputs += [{'path':str(Path(p['capture']['path']).absolute()),'sha256':p['capture']['sha256']},{'path':str(Path(wd['path']).absolute()),'sha256':wd['sha256']},{'path':str(water_file.absolute()),'sha256':wr['mask']['sha256']}]+nested_pins
    verified_inputs += [{'path':str(Path(p['clearance_descriptor']['path']).absolute()),'sha256':p['clearance_descriptor']['sha256']}]+[{'path':str(Path(x['path']).absolute()),'sha256':x['sha256']} for x in clearance_nested]
    if package_file is not None: verified_inputs.append({'path':str(package_file),'sha256':_sha(package_file)})
    verified_inputs=list({x['path']:x for x in verified_inputs}.values())
    return {'status':'SourceOnlyPreflightPassed','kind':KIND,'schema_version':1,'operation_map_id':sm['operation_map_id'],'frame':fm,'verified_inputs':verified_inputs,'source_pins_verified':verified,'historical_producer_binaries_unavailable':len(skipped),'mask_count':len(masks),'source_mask_sha256':mask_hashes,'navigation_blocker_count':len(nav),'clearance_bounds_count':len(bounds),'water_footprint_count':len(dom.get('waterFootprints',[])),'signed_zero_decimal_loss_count':signed_zero,'source_derived_water_mask_sha256':wr['mask']['sha256'],'active_water_qualified':False,'mission_initialized':False,'mission_ready':False,'source_fit_certified':False,'live_occupancy_measured':False,'world_created':False}

def main(argv=None):
    ap=argparse.ArgumentParser(description=__doc__); ap.add_argument('--package',required=True); ap.add_argument('--output')
    a=ap.parse_args(argv)
    try: result=preflight_package(a.package)
    except (ValueError,KeyError,TypeError) as e:
        print(json.dumps({'status':'Rejected','error':str(e)},sort_keys=True),file=sys.stderr); return 2
    out=json.dumps(result,indent=2,sort_keys=True)+'\n'
    if a.output: Path(a.output).write_text(out)
    else:
        result.pop('verified_inputs',None)
        print(json.dumps(result,indent=2,sort_keys=True))
    return 0
if __name__=='__main__': raise SystemExit(main())
