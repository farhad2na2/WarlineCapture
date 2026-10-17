"""Draft source-pinned logical objective geometry; never measurement/readiness evidence."""
from pathlib import Path
import copy,hashlib,json,re

ROOT=Path(__file__).resolve().parents[2]
INDEX=Path('Design/AgentReports/SkirmishExpansion/Full120/source-fit-authoring-batches-20261015-v2.json')
MANIFEST=Path('Design/AgentReports/SkirmishExpansion/Full120/LogicalInputs/20261015-01/manifest.json')
PILOTS=Path('Design/AgentReports/SkirmishExpansion/GroundPilots/measured-pad-bounds-proposals-survey11-v2.json')
SURVEY=Path('Logs/SkirmishProduction/ground-source-fit-20261015-11.json')

def load(path):return json.loads((ROOT/path).read_text(encoding='utf-8-sig'))
def pin(path):return dict(path=path.as_posix(),sha256=hashlib.sha256((ROOT/path).read_bytes()).hexdigest())

def routes_from_template(path):
    text=(ROOT/path).read_text(encoding='utf-8-sig')
    block=text.split('  routes:\n',1)[1].split('  pads:\n',1)[0]
    result=[]
    for part in block.split('  - RouteId: ')[1:]:
        lines=part.splitlines();waypoints=[];row={'RouteId':lines[0]}
        for line in lines[1:]:
            if line.startswith('    - '):waypoints.append(line[6:])
            elif ': ' in line:
                key,value=line.strip().split(': ',1)
                row[key]=float(value) if '.' in value else int(value)
        row['WaypointAnchorIds']=waypoints;result.append(row)
    if len(result)!=3:raise ValueError('Exact existing three-route template required')
    return result

def authored_area(sid,role,points):
    return dict(StableAreaId=f'{sid.lower()}.objective.{role}',Role=role,Shape=1,
                Points=[dict(WorldX=points[0],WorldZ=points[1]),dict(WorldX=points[2],WorldZ=points[3])])

def build():
    index=load(INDEX);survey={r['group']:r for r in load(SURVEY)['sources']};pilots=load(PILOTS)['candidates']
    ids=set(next(r['catalog_ids'] for r in index['parallel_batches'] if r['id']=='ground-objective-thirty'))
    result=dict(schema_version=1,kind='UnqualifiedGroundObjective30Authoring',mission_count=30,canonical_size_vectors=90,
        authority=dict(full_index=pin(INDEX),logical_inputs_manifest=pin(MANIFEST),pilot_candidates=pin(PILOTS),source_survey=pin(SURVEY)),
        protected_catalog_ids=sorted(r['catalog_id'] for r in index['missions'] if r.get('protected_binding')),
        protected_coded_bindings=['S002','S003','S004','S005'],missions=[])
    for original in index['missions']:
        if original['catalog_id'] not in ids:continue
        mission={key:copy.deepcopy(original[key]) for key in ('catalog_id','definition_id','scenario_setup_id','tuple','packet','canonical_size_rows','canonical_size_rows_sha256')}
        sid=mission['catalog_id'];kind=mission['tuple']['objective_id'];group=mission['tuple']['map_id'];group='CC' if group=='DB' else group
        source=survey[group];candidates=[v for v in pilots if v['group']==group]
        start=mission['tuple']['start_profile']
        candidate=next((v for v in candidates if next(r for r in index['missions'] if r['catalog_id']==v['catalog_id'])['tuple']['start_profile']==start),candidates[0])
        candidate=copy.deepcopy(candidate);template_pilot=candidate['catalog_id'];candidate['catalog_id']=sid
        candidate['canonical_budget_rows_sha256']=mission['canonical_size_rows_sha256']
        if 'projectedAnchors' in source:
            anchors={r['AnchorId']:r['projectedSourceXZ'] for r in source['projectedAnchors']}
            routes=routes_from_template(Path(source['layoutTemplatePath']))
        else:
            serialized=source['candidateLayoutSerialized'];anchors={r['AnchorId']:[r['WorldX'],r['WorldZ']] for r in serialized['anchors']};routes=copy.deepcopy(serialized['routes'])
        def center(route):return anchors[route['WaypointAnchorIds'][len(route['WaypointAnchorIds'])//2]]
        areas=[];origins=[]
        if kind=='FC':
            for role,route in enumerate(routes,1):
                x,z=center(route);areas.append(authored_area(sid,role,[x-16,z-16,x+16,z+16]));origins.append(dict(role=role,route_id=route['RouteId'],anchor_id=route['WaypointAnchorIds'][len(route['WaypointAnchorIds'])//2],design_half_extent_metres=16))
        else:
            rally=next(p for p in candidate['typed_pad_world_axis_candidates'] if p['kind']==5 and p['faction']==2)
            if kind=='BT':
                for role,route in zip((4,5),routes[1:]):
                    x,z=center(route);half=route['WidthMetres']*.5
                    areas.append(authored_area(sid,role,[x-half,z-half,x+half,z+half]));origins.append(dict(role=role,route_id=route['RouteId'],anchor_id=route['WaypointAnchorIds'][len(route['WaypointAnchorIds'])//2],design_half_extent_metres=half))
                role=6
            else:role=7
            areas.append(authored_area(sid,role,rally['world_rect']));origins.append(dict(role=role,pad_id=rally['pad_id'],faction=2,scope='Authored reuse of existing opponent-side rally footprint; tactical destination/exit safety not established'))
        bounds=candidate['proposed_logical_world_rect']
        for area in areas:
            low,high=area['Points'];bounds=[min(bounds[0],low['WorldX']),min(bounds[1],low['WorldZ']),max(bounds[2],high['WorldX']),max(bounds[3],high['WorldZ'])]
        candidate['proposed_logical_world_rect']=bounds
        physical=json.loads(source['serializedMetadata'])['MonoBehaviour']['bounds']
        if not (bounds[0]>=physical['playableMin']['x'] and bounds[1]>=physical['playableMin']['z'] and bounds[2]<=physical['playableMax']['x'] and bounds[3]<=physical['playableMax']['z']):raise ValueError('Authored candidate escapes actual source: '+sid)
        mission.update(source_candidate=copy.deepcopy(index['source_candidates'][mission['tuple']['map_id']]),collector_source_group=group,
            layout_input=dict(pilot_candidate=candidate,template_pilot_catalog_id=template_pilot,template_group=group,retained_routes=routes,
                authority_scope='Authored logical candidate from pinned source anchors and pilot engineering pad proposals; no new terrain or native clearance proof'),
            typed_objective_areas=dict(status='AuthoredCandidate_UnmeasuredSourceFit',areas=areas,measurement=None,derivation=origins,
                geometry_scope='Explicit authoring choices, not observations; FC initial16m design half extent; BT existing route-width capture points; exit/destination existing rally rectangle. Walls, separation, dwell, tower fire, full armies and legal approaches pending.'),
            view_intent=dict(required_roles=['Infantry','GroundVehicle','TransportHelicopter','ReconDrone']+(['ConvoyLogic'] if kind=='CE' else []),
                source_elevation_min=source['surfaceHeightMin'],source_elevation_max=source['surfaceHeightMax'],scalars=None,status='MissingPerMissionOpeningZoomAndPermittedAircraftNativeFraming'),
            pending_gates=['Authored area terrain/wall/no-build/source-fit','All three canonical facility and army placements','Distinct legal heavy-vehicle routes and holding pockets','Service approach and paid repair','Helicopter/recon production, return and role-specific readable framing','Per-mission view values and native opening/minimum/maximum PNG proof','Exact measured deployment and typed-area evidence','Normal input ARIA/result/return/checkpoint and final-candidate qualification'],publication_enabled=False)
        result['missions'].append(mission)
    return result

if __name__=='__main__':
    output=ROOT/'Tools/CI/SkirmishProductionDrafts/GroundObjective30/ground-objective-thirty.authoring.json'
    output.parent.mkdir(parents=True,exist_ok=True);document=build();output.write_text(json.dumps(document,indent=2)+'\n',encoding='utf-8',newline='\n')
    print(json.dumps(dict(path=output.relative_to(ROOT).as_posix(),sha256=pin(output.relative_to(ROOT))['sha256'],missions=len(document['missions']),publication_enabled=False)))
