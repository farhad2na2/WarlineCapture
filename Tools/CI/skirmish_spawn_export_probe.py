"""Build explicit hypothetical-empty-overlay placement probes on verified full authored maps."""
from __future__ import annotations
import json
from pathlib import Path
import skirmish_mission_data as data


def summarize_placements(result, count):
    rows=result.get('results',[])
    placed=sum(1 for row in rows for actor in row.get('actors',[]) if actor.get('found') is True)
    complete=len(rows)==1 and rows[0].get('all_requested_cells_placed') is True and placed==count
    result.update(placed_count=placed,unplaced_count=count-placed,all_requested_cells_placed=complete)
    if not complete:
        result['result']='failed'
        result.setdefault('probe_failures',[]).append('Not all requested hypothetical placements succeeded')
    return result


def probe(build_path,bundle,native_receipt,map_key,anchor_id,count,output):
    import skirmish_spawn_replay as replay
    build_path=Path(build_path).resolve();bundle=Path(bundle).resolve();native_receipt=Path(native_receipt).resolve()
    build=data.document(build_path/'build.json');project=Path(build['project']).resolve()
    if type(count)is not int or not 1<=count<=120:raise ValueError('Explicit probe count1..120 required')
    report=data.inspect(project,bundle,native_receipt,True)
    if not report['integrity_valid'] or not report['native_run_verified']:raise ValueError('Fresh complete native export verification required; regenerate after source drift')
    manifest=data.document(bundle/'manifest.json');maps=[m for m in manifest['maps'] if m['key']==map_key]
    if len(maps)!=1:raise ValueError('Exact exported map key required')
    m=maps[0]
    if not m['campaign_physical_source_member'] or not m['authored_grid_available'] or not m['decoded_surface_available'] or not m['surface_grid_matches']:raise ValueError('Complete same-grid/surface actual Campaign source required')
    cells=m['width']*m['height']
    if cells*count>256*1024*1024:raise ValueError('Full-grid worst-case work exceeds profile; reduce count, never crop')
    anchors=[p for p in m['map_anchors'] if p['id']==anchor_id and p['has_cell'] and p['in_bounds']]
    if len(anchors)!=1:raise ValueError('Exact inbounds exported typed map anchor required')
    anchor=anchors[0];output=replay.new_output(project,output)
    (output/'export-prevalidation.json').write_text(json.dumps(report,indent=2,sort_keys=True)+'\n')
    artifacts=[]
    def artifact(identity,path,encoding):
        path=Path(path).resolve();artifacts.append(dict(id=identity,path=str(path),sha256=replay.sha(path),bytes=path.stat().st_size,encoding=encoding));return {'artifact':identity}
    walk=artifact('walkable',bundle/m['walkable_artifact'],'u8');surface=artifact('infantry-surface',bundle/m['infantry_surface_artifact'],'u8')
    assumptions=output/'hypothetical-empty-overlay';assumptions.mkdir()
    for name,raw in [('zero-u8.bin',bytes(cells)),('zero-i32.bin',bytes(cells*4)),('friendly-255.bin',bytes([255])*cells)]:
        (assumptions/name).write_bytes(raw)
    zero=artifact('hypothetical-zero',assumptions/'zero-u8.bin','u8');counts=artifact('hypothetical-counts',assumptions/'zero-i32.bin','i32-le');friendly=artifact('hypothetical-friendly',assumptions/'friendly-255.bin','u8')
    actors=[dict(id=f'probe.{i:03d}.1.0.0',mission_seed=17+i,requested_world=[anchor['world_x'],m['origin_y'],anchor['world_z']],footprint=[1,1],is_air=False,vehicle=False,surface_profile='native-infantry-1x1',initialization=None,typed_occupied=None,has_footprint_component=True) for i in range(count)]
    case=dict(id='real-map-admission-only',kind='placement_sequence',facts_scope='AuthoredMapAdmissionProbe',placement_mode='Untyped',grid=dict(Width=m['width'],Height=m['height'],OriginX=m['origin_x'],OriginY=m['origin_y'],OriginZ=m['origin_z'],CellSize=m['cell_size']),storage=dict(walkable=walk,blocked=zero,occupied=zero,reserved=zero,counts=counts,friendly_pass=friendly,friendly_pass_created=True),blocker_update=None,existing=[],actors=actors,surface_profiles=[dict(id='native-infantry-1x1',footprint=[1,1],vehicle=False,admission=surface,provenance='ExportedInfantryCellAdmission1x1')],has_match=True,surface_count=1,emit_arrays=False)
    input_path=output/'probe-input.json';input_path.write_text(json.dumps(dict(schema_version=1,artifacts=artifacts,cases=[case]),indent=2)+'\n')
    result=replay.run(build_path,input_path,output/'replay')
    summarize_placements(result,count)
    result.update(export_manifest_sha256=report['manifest_sha256'],export_native_receipt_sha256=replay.sha(native_receipt),map_key=map_key,anchor_id=anchor_id,full_grid_dimensions=[m['width'],m['height']],probe_count=count,
        fact_scope='Verified authored cell admission plus explicitly hypothetical empty runtime overlays',runtime_spawn_qualified=False,actual_prefab_initialization=False)
    (output/'probe-report.json').write_text(json.dumps(result,indent=2)+'\n')
    return result
