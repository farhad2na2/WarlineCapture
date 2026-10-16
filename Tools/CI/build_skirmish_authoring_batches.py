"""Build source-pinned logical authoring inputs for all120; never publish readiness."""
import argparse
import csv
from collections import Counter
import hashlib
import json
from pathlib import Path
import re
from itertools import product
import subprocess

PILOTS={'S026','S049','S050','S074','S097','S098'}
PROTOTYPES={'S001':0,'S025':1,'S073':3}
CODED={'S001','S002','S003','S004','S005','S025','S073'}
SIZES={'Standard','War','LargeWar'}


def sha(path):return hashlib.sha256(path.read_bytes()).hexdigest()


def rows(path):
    with path.open(encoding='utf-8-sig',newline='') as stream:return list(csv.DictReader(stream))


def validate(catalog,matrix):
    expected={f'S{i:03}' for i in range(1,121)}
    if len(catalog)!=120 or {r['scenario_id'] for r in catalog}!=expected:raise ValueError('Exact120 unique stable catalog IDs required')
    tuples={(r['map_id'],r['objective_id'],r['army_profile'],r['start_profile']) for r in catalog}
    if tuples!=set(product(('DB','CC','MP','IB','AP'),('BA','FC','BT','CE'),('G','A','C'),('F','E'))):raise ValueError('Exact120 catalog tuple product required')
    by_id={r['scenario_id']:r for r in catalog};keys=set()
    for row in matrix:
        key=(row['catalog_id'],row['size_id'])
        if key in keys or key[0] not in by_id or key[1] not in SIZES:raise ValueError('Duplicate/unknown canonical size vector')
        keys.add(key);entry=by_id[key[0]]
        if any(row[k]!=entry[k] for k in ('army_profile','start_profile','objective_id')):raise ValueError('Canonical vector/catalog tuple mismatch')
    if keys!={(key,size) for key in expected for size in SIZES}:raise ValueError('All360 exact size vectors required')


def read_field(text,key):
    found=re.findall(r'^  '+re.escape(key)+r': (.*)$',text,re.M)
    return found[0].strip() if len(found)==1 else None


def build(root,survey,pilot_config):
    roadmap=root/'Design/Roadmap/Skirmish_Expansion';catalog_path=roadmap/'SCENARIO_CATALOG.csv';matrix_path=roadmap/'INITIAL_SETUP_MATRIX.csv';queue_path=roadmap/'WORK_QUEUE_004_120.csv'
    catalog=rows(catalog_path);matrix=rows(matrix_path);validate(catalog,matrix);queue={r['catalog_id']:r for r in rows(queue_path)}
    native=json.loads(survey.read_bytes());pilot=json.loads(pilot_config.read_bytes());pilot_by_id={r['catalog_id']:r for r in pilot['candidates']}
    if set(pilot_by_id)!=PILOTS or pilot['survey_sha256']!=sha(survey):raise ValueError('Six pilot config/survey join differs')
    sources={}
    for row in native['sources']:
        for path,hash_key in [('sourcePath','sourceAssetSha256'),('gridPath','gridAssetSha256'),('surfacePath','surfaceAssetSha256')]:
            if sha(root/row[path])!=row[hash_key]:raise ValueError('Actual physical source drift: '+row[path])
        sources[row['group']]={k:row[k] for k in ('sourcePath','sourceAssetSha256','gridPath','gridAssetSha256','surfacePath','surfaceAssetSha256','operationMapId','sourceIdentityHash','contentHash','sourceSceneGuid','authoredSubSceneGuid')}
    if set(sources)!={'CC','AP','MP','IB'}:raise ValueError('Exact four measured existing Campaign source candidates required')
    # DB's generic candidate references the same existing Campaign physical source
    # as CC. Existing coded bindings below remain independently pinned unchanged.
    sources['DB']=dict(sources['CC']);sources['DB']['candidate_scope']='ExistingSharedCampaignPhysicalSourceOnly_DBObjectiveGeometryUnqualified'
    definition_files={p.parent.name:p for p in (root/'Assets/Game/Configs/SkirmishExpansion/Scenarios').glob('S*/SkirmishScenario_*.asset')}
    source_meta={re.search(r'^guid: (\w+)$',p.read_text(),re.M)[1]:p.with_suffix('') for p in (root/'Assets/Game/Configs/SkirmishExpansion/Scenarios').glob('S*/*.asset.meta') if re.search(r'^guid: (\w+)$',p.read_text(),re.M)}
    result=dict(schema_version=1,kind='UnqualifiedFullCatalogLogicalAuthoringInputs',catalog_sha256=sha(catalog_path),matrix_sha256=sha(matrix_path),work_queue_sha256=sha(queue_path),
        survey_path=str(survey.relative_to(root)),survey_sha256=sha(survey),pilot_config_path=str(pilot_config.relative_to(root)),pilot_config_sha256=sha(pilot_config),
        mission_count=120,canonical_size_vectors=360,native_qualification=False,player_ready=False,physical_map_changes_authorized=False,
        source_candidates=sources,missions=[],family_work_units=[],parallel_batches=[])
    packets={}
    for entry in catalog:
        sid=entry['scenario_id'];family=entry['map_id']+'_'+entry['objective_id'];packet=roadmap/'Scenarios'/(family+'.md')
        if not packet.is_file():raise ValueError('Actual packet missing '+str(packet))
        packets[family]=dict(path=packet.relative_to(root).as_posix(),sha256=sha(packet))
        vectors=[r for r in matrix if r['catalog_id']==sid]
        asset=definition_files.get(sid);protected=None
        if sid in CODED:
            protected=dict(policy='PreserveActualCodedBindingAndExistingGUIDs',legacy_runtime_index=PROTOTYPES.get(sid),definition_asset=None)
            if asset:
                text=asset.read_text();ref=read_field(text,'campaignMap');match=re.search(r'guid: (\w+)',ref or '')
                protected.update(definition_asset=asset.relative_to(root).as_posix(),definition_sha256=sha(asset),definition_meta_sha256=sha(Path(str(asset)+'.meta')))
                if match:
                    if match[1] not in source_meta:raise ValueError('Serialized coded source reference unresolved '+sid)
                    source=source_meta[match[1]]
                    protected.update(campaign_source_asset=source.relative_to(root).as_posix(),campaign_source_sha256=sha(source),campaign_source_guid=match[1],native_source_identity_census_required=True)
                else:
                    protected.update(campaign_source_asset=None,campaign_source_guid=None,native_source_identity_census_required=True,
                        source_binding_authority='Existing factory/runtime binding; serialized campaignMap field absent. Preserve, do not replace with the family candidate.')
        row=dict(catalog_id=sid,definition_id=queue.get(sid,{}).get('definition_id','skirmish.'+sid.lower()),scenario_setup_id=queue.get(sid,{}).get('scenario_setup_id','scenario.skirmish.'+sid.lower()),
            tuple={k:entry[k] for k in ('map_id','objective_id','army_profile','start_profile')},packet=packets[family],packet_section=sid.lower(),
            canonical_size_rows=vectors,canonical_size_rows_sha256=hashlib.sha256(json.dumps(vectors,sort_keys=True).encode()).hexdigest(),
            shared_source_candidate_group=entry['map_id'],source_selection_status='PreserveCodedBinding' if protected else 'CandidateExistingCampaignSource_NotFitCertified',
            protected_binding=protected,existing_definition_asset=asset.relative_to(root).as_posix() if asset else None,
            intended_new_definition_asset='Assets/Game/Configs/SkirmishExpansion/Scenarios/'+sid+'/SkirmishScenario_'+sid+'.asset',
            required_capabilities=queue.get(sid,{}).get('required_capabilities','').split(';') if queue.get(sid,{}).get('required_capabilities') else [],
            capability_authority='ExistingWorkQueue' if sid in queue else 'ProtectedPrototype_InspectExistingRuntimeAndPacket',
            required_logical_inputs=dict(view_profile='MissingPerSourceElevationRoleFocusReadabilityAndAircraftFacts',
                typed_objective_areas='NotRequiredForBA' if entry['objective_id']=='BA' else 'MissingExplicitSourceLayoutBoundTypedAreas',
                route_independence='Unqualified',ordinary_facility_and_all_size_force_fit='Unqualified',camera_scalars=None),
            pilot_candidate=pilot_by_id.get(sid),normal_mission_qualified=False,device_player_acceptance='Pending')
        result['missions'].append(row)
    for family in sorted(packets):
        mission_ids=[r['catalog_id'] for r in result['missions'] if r['tuple']['map_id']+'_'+r['tuple']['objective_id']==family]
        if len(mission_ids)!=6:raise ValueError('Each of20 family authoring units must contain6 catalog entries')
        result['family_work_units'].append(dict(family_id=family,packet=packets[family],catalog_ids=mission_ids,
            editable_output_scope='Design/AgentReports/SkirmishExpansion/Full120/LogicalCandidates/'+family,
            task='Author actual source-bound logical pads/routes/typed objective areas and per-role view requirements; no guessed coordinates or acceptance',
            native_gate='Root-owned disposable materialization, all-size native placement/circulation/route joins; then normal missions',ready=False))
    result['parallel_batches']=[dict(id='pilot-ba-ground-six',catalog_ids=sorted(PILOTS),work='Consume measured v2 configs; iterate from actual materialization failures'),
        dict(id='ground-objective-thirty',catalog_ids=[r['catalog_id'] for r in result['missions'] if r['tuple']['army_profile']=='G' and r['tuple']['objective_id']!='BA'],work='Typed FC/BT/CE source areas, original roles, routes and view intents on existing sources; shared native controls do not qualify missions'),
        dict(id='air-combined-eighty',catalog_ids=[r['catalog_id'] for r in result['missions'] if r['tuple']['army_profile'] in ('A','C')],work='Concurrent source/producer/aircraft lifecycle and role-view metadata; retain unsupported runway/paid-flight/cargo gates'),
        dict(id='protected-ba-ground-four',catalog_ids=[r['catalog_id'] for r in result['missions'] if r['tuple']['army_profile']=='G' and r['tuple']['objective_id']=='BA' and r['catalog_id'] not in PILOTS],work='Preserve existing bindings; new shared-candidate regression/qualification separately')]
    if sum(len(v['catalog_ids']) for v in result['parallel_batches'])!=120:raise ValueError('Batches must partition120 entries')
    result['coverage_workload']=dict(regular_baseline_slots=720,family_stress_slots_before_valid_overlap=1200,
        additional_exposed_difficulty_wins=360,additional_exposed_size_locale_wins=480,throughput_measured=False)
    return result


def write_family_inputs(result,directory,root):
    directory=directory.resolve()
    if not directory.is_relative_to(root.resolve()) or directory.is_relative_to((root/'Assets').resolve()):raise ValueError('Authoring inputs remain inside workspace and outside imported Assets')
    directory.mkdir(parents=True,exist_ok=False)
    index_hash=hashlib.sha256(json.dumps(result,sort_keys=True,separators=(',',':')).encode()).hexdigest()
    receipts=[]
    for work in result['family_work_units']:
        missions=[r for r in result['missions'] if r['catalog_id'] in work['catalog_ids']]
        group=work['family_id'].split('_')[0]
        document=dict(schema_version=1,kind='UnqualifiedSourceBoundFamilyLogicalInputs',full_index_logical_sha256=index_hash,
            catalog_sha256=result['catalog_sha256'],matrix_sha256=result['matrix_sha256'],packet=work['packet'],
            source_candidate=result['source_candidates'][group],family=work['family_id'],missions=missions,
            actual_authored_objective_geometry=None,per_source_readable_view_profiles=None,
            source_fit_certified=False,native_qualification=False,player_ready=False)
        path=directory/(work['family_id']+'.json')
        with path.open('x',encoding='utf-8') as output:json.dump(document,output,indent=2);output.write('\n')
        receipts.append(dict(family=work['family_id'],path=path.relative_to(root).as_posix(),sha256=sha(path),mission_count=len(missions)))
    with (directory/'manifest.json').open('x',encoding='utf-8') as output:json.dump(dict(schema_version=1,families=receipts,files=20,missions=120,ready=False),output,indent=2);output.write('\n')
    return receipts


if __name__=='__main__':
    parser=argparse.ArgumentParser(description=__doc__);parser.add_argument('--survey',required=True,type=Path);parser.add_argument('--pilot-config',required=True,type=Path);parser.add_argument('--output',required=True,type=Path);parser.add_argument('--family-directory',type=Path);args=parser.parse_args();root=Path(__file__).resolve().parents[2]
    if not args.output.resolve().is_relative_to(root) or args.output.resolve().is_relative_to(root/'Assets'):raise ValueError('Authoring index must remain outside imported Assets and inside workspace')
    result=build(root,args.survey.resolve(),args.pilot_config.resolve());args.output.parent.mkdir(parents=True,exist_ok=True)
    with args.output.open('x',encoding='utf-8') as output:json.dump(result,output,indent=2);output.write('\n')
    if args.family_directory:write_family_inputs(result,args.family_directory,root)
    print(json.dumps(dict(output=str(args.output),missions=len(result['missions']),vectors=sum(len(v['canonical_size_rows']) for v in result['missions']),family_units=len(result['family_work_units']),qualified=False)))
