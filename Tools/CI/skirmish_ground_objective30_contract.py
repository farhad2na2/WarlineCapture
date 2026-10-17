"""Preflight exact thirty-mission authoring input and source-world reuse plan.

No Unity, imported assets, source-fit certification or queue completion occurs.
"""
import argparse
from collections import defaultdict
import hashlib
import json
import math
from pathlib import Path
from build_skirmish_authoring_batches import rows,validate as validate_catalog

SOURCE_KEYS = ('sourcePath','sourceAssetSha256','gridPath','gridAssetSha256','surfacePath',
               'surfaceAssetSha256','operationMapId','sourceIdentityHash','contentHash',
               'sourceSceneGuid','authoredSubSceneGuid')
ROLES = {'FC': {1,2,3}, 'BT': {4,5,6}, 'CE': {7}}


def sha(raw): return hashlib.sha256(raw).hexdigest()


def load(path): return json.loads(path.read_text(encoding='utf-8-sig'))


def checked_file(pin,root):
    if not isinstance(pin,dict) or not isinstance(pin.get('path'),str):
        raise ValueError('Explicit input path/SHA pin required')
    path=(root/pin['path']).resolve()
    if not path.is_relative_to(root.resolve()) or sha(path.read_bytes())!=pin.get('sha256'):
        raise ValueError('Authority path or immutable SHA differs: '+str(pin.get('path')))
    return path


def validate(document,root):
    if (document.get('schema_version')!=1 or document.get('kind')!='UnqualifiedGroundObjective30Authoring' or
            document.get('mission_count')!=30 or document.get('canonical_size_vectors')!=90):
        raise ValueError('Exact version1 thirty-mission/ninety-vector envelope required')
    authority=document.get('authority',{})
    required=('full_index','logical_inputs_manifest','pilot_candidates','source_survey')
    paths={key:checked_file(authority.get(key),root) for key in required}
    index=load(paths['full_index']);manifest=load(paths['logical_inputs_manifest'])
    if (index.get('mission_count')!=120 or index.get('canonical_size_vectors')!=360 or
            manifest.get('files')!=20 or manifest.get('missions')!=120):
        raise ValueError('Actual complete Full120 index/family manifest required')
    roadmap=root/'Design/Roadmap/Skirmish_Expansion'
    catalog_path=roadmap/'SCENARIO_CATALOG.csv';matrix_path=roadmap/'INITIAL_SETUP_MATRIX.csv'
    catalog,matrix=rows(catalog_path),rows(matrix_path);validate_catalog(catalog,matrix)
    if index.get('catalog_sha256')!=sha(catalog_path.read_bytes()) or index.get('matrix_sha256')!=sha(matrix_path.read_bytes()):
        raise ValueError('Full120 index must join current canonical catalog/matrix bytes')
    canonical_ids={r['scenario_id'] for r in catalog if r['army_profile']=='G' and r['objective_id']!='BA'}
    if index.get('survey_sha256')!=sha(paths['source_survey'].read_bytes()) or index.get('pilot_config_sha256')!=sha(paths['pilot_candidates'].read_bytes()):
        raise ValueError('Exact index/survey/pilot authority join differs')
    for row in manifest['families']:checked_file(row,root)
    expected_ids=set(next(row['catalog_ids'] for row in index['parallel_batches'] if row['id']=='ground-objective-thirty'))
    if expected_ids!=canonical_ids:raise ValueError('Batch ID set differs from actual canonical catalog')
    expected={row['catalog_id']:row for row in index['missions'] if row['catalog_id'] in expected_ids}
    protected=sorted(row['catalog_id'] for row in index['missions'] if row.get('protected_binding'))
    if sorted(document.get('protected_catalog_ids',[]))!=protected:
        raise ValueError('Protected coded bindings must remain explicit and unchanged')
    missions=document.get('missions',[])
    if len(missions)!=30 or len({r.get('catalog_id') for r in missions})!=30 or {r.get('catalog_id') for r in missions}!=expected_ids:
        raise ValueError('Exact thirty unique ground FC/BT/CE catalog IDs required')
    groups=defaultdict(list)
    for mission in missions:
        sid=mission['catalog_id'];old=expected[sid]
        if mission.get('canonical_size_rows')!=[r for r in matrix if r['catalog_id']==sid]:
            raise ValueError('Current exact canonical matrix budget differs: '+sid)
        for key in ('definition_id','scenario_setup_id','tuple','packet','canonical_size_rows','canonical_size_rows_sha256'):
            if mission.get(key)!=old.get(key):raise ValueError('Canonical mission '+key+' differs: '+sid)
        if mission['tuple']['army_profile']!='G' or mission['tuple']['objective_id'] not in ROLES:
            raise ValueError('Ground objective tuple required')
        if mission.get('publication_enabled') is not False:
            raise ValueError('Unqualified authoring cannot enable publication')
        checked_file(mission['packet'],root)
        source=mission.get('source_candidate',{});original=index['source_candidates'][mission['tuple']['map_id']]
        if any(not source.get(key) or source.get(key)!=original.get(key) for key in SOURCE_KEYS):
            raise ValueError('Exact existing Campaign source candidate differs: '+sid)
        for path_key,hash_key in (('sourcePath','sourceAssetSha256'),('gridPath','gridAssetSha256'),('surfacePath','surfaceAssetSha256')):
            checked_file(dict(path=source[path_key],sha256=source[hash_key]),root)
        group=mission.get('collector_source_group')
        if group!=('CC' if mission['tuple']['map_id']=='DB' else mission['tuple']['map_id']):
            raise ValueError('Physical source alias differs')
        areas=mission.get('typed_objective_areas',{})
        if not isinstance(areas.get('status'),str) or areas.get('measurement') is not None:
            raise ValueError('Initial unmeasured typed-area status required; no invented proof')
        values=areas.get('areas')
        if not isinstance(values,list):raise ValueError('Explicit typed-area list required')
        if values:
            if {r.get('Role') for r in values}!=ROLES[mission['tuple']['objective_id']] or len(values)!=len(ROLES[mission['tuple']['objective_id']]):
                raise ValueError('Exact objective-specific typed area roles required')
            if len({r.get('StableAreaId') for r in values})!=len(values) or any(not isinstance(r.get('StableAreaId'),str) or not r['StableAreaId'] for r in values):
                raise ValueError('Exact distinct area identities required')
            for area in values:
                points=area.get('Points',[])
                if type(area.get('Role')) is not int or type(area.get('Shape')) is not int or not (
                        area['Shape']==1 and len(points)==2 or area['Shape']==2 and len(points)>=3):
                    raise ValueError('Typed rectangle or polygon points required')
                for point in points:
                    if any(type(point.get(key)) not in (int,float) or not math.isfinite(point[key]) for key in ('WorldX','WorldZ')):
                        raise ValueError('Finite actual world-coordinate points required')
        view=mission.get('view_intent',{})
        if view.get('scalars') is not None or not isinstance(view.get('required_roles'),list) or not isinstance(view.get('status'),str):
            raise ValueError('Per-role view requirements with unmeasured scalars required')
        if not isinstance(mission.get('pending_gates'),list) or not mission['pending_gates']:
            raise ValueError('Unresolved source/native/readability gates must remain explicit')
        key=tuple(source[k] for k in SOURCE_KEYS)
        groups[key].append(sid)
    if len(groups)!=4:raise ValueError('Exact four existing physical source groups required')
    return dict(schema_version=1,result='ValidatedUnqualifiedAuthoringInputs',missions=30,canonical_size_vectors=90,
        source_worlds_planned=len(groups),source_fit_certified=False,publication_enabled=False,
        input_scope='Authoring data only; native compiler/source/geometry/normal journey gates remain independent',
        authority_sha256={key:sha(path.read_bytes()) for key,path in paths.items()},
        source_world_groups=[dict(source=dict(zip(SOURCE_KEYS,key)),catalog_ids=sorted(ids)) for key,ids in sorted(groups.items())])


if __name__=='__main__':
    p=argparse.ArgumentParser(description=__doc__);p.add_argument('--input',type=Path,required=True);p.add_argument('--output',type=Path,required=True)
    a=p.parse_args();root=Path(__file__).resolve().parents[2];output=a.output.resolve()
    if not output.is_relative_to(root) or output.is_relative_to(root/'Assets'):raise ValueError('Evidence output must remain outside imported Assets')
    raw=a.input.read_bytes();report=validate(json.loads(raw.decode('utf-8-sig')),root);report['input_sha256']=sha(raw)
    output.parent.mkdir(parents=True,exist_ok=True)
    with output.open('x',encoding='utf-8') as stream:json.dump(report,stream,indent=2);stream.write('\n')
    print(json.dumps({key:report[key] for key in ('result','missions','canonical_size_vectors','source_worlds_planned','source_fit_certified')}))
