"""Create legacy job proposals from SHA-bound actual Editor preset metadata. No dispatch/claim/qualification."""
import argparse
import hashlib
import json
from pathlib import Path
import sys

# During draft validation this imports the explicitly projected queue beside this file.
sys.path.append(str(Path(__file__).resolve().parents[2]))
from skirmish_production_queue import fingerprint, preflight, write_new
from skirmish_production_matrix_authority import SamplingAuthority
from s005_windows_source_snapshot import candidate_hash

BINDINGS={'S001':(0,'SkirmishBaseAssault'),'S025':(1,'SkirmishCityCrossroads'),'S073':(3,'SkirmishIndustrialBasin')}
def checked(path, expected):
    raw=Path(path).read_bytes()
    if hashlib.sha256(raw).hexdigest()!=expected:raise ValueError('SHA-bound input changed: '+str(path))
    def unique(items):
        result={}
        for k,v in items:
            if k in result:raise ValueError('Duplicate JSON property: '+k)
            result[k]=v
        return result
    return json.loads(raw.decode('utf-8-sig'),object_pairs_hook=unique)

def prepare(project,plan_path,plan_sha,metadata_path,metadata_sha,snapshot_path,snapshot_sha,output):
    project=Path(project).resolve(); output=Path(output).resolve()
    if output.is_relative_to(project/'Assets'):raise ValueError('No imported Assets output')
    authority=SamplingAuthority(project,Path(plan_path),plan_sha)
    plan=checked(plan_path,plan_sha);metadata=checked(metadata_path,metadata_sha);snapshot=checked(snapshot_path,snapshot_sha)
    if set(metadata)!={'schema_version','sampling_plan_sha256','rows','native_jobs','queue_success_enabled'} or metadata['schema_version']!=1 or metadata['sampling_plan_sha256']!=plan_sha or metadata['native_jobs']!=0 or metadata['queue_success_enabled'] is not False:raise ValueError('Actual legacy metadata envelope differs')
    if set(snapshot)!={'source_manifest','candidate_sha256','build_manifest','build_sha256'} or not snapshot['source_manifest'] or not snapshot['build_manifest'] or candidate_hash(snapshot['source_manifest'])!=snapshot['candidate_sha256'] or fingerprint(snapshot['build_manifest'])!=snapshot['build_sha256']:raise ValueError('Complete native candidate/DLL snapshot required')
    expected=[r for r in plan['slots'] if r['catalog_id'] in BINDINGS]
    rows=metadata['rows']
    if not expected or len(rows)!=len(expected) or [r.get('sampling_slot') for r in rows]!=expected:raise ValueError('Every protected canonical slot must have one ordered actual metadata row')
    jobs=[]
    for n,row in enumerate(rows):
        if set(row)!={'kind','sampling_slot','native_setup_identity','setup_sha256','content_sha256','live_session_observed'} or row['kind']!='LegacyPresetExpectedNativeMetadata' or row['live_session_observed'] is not False:raise ValueError('Unobserved metadata preparation cannot masquerade as live run')
        slot=row['sampling_slot'];identity=row['native_setup_identity'];catalog=slot['catalog_id'];index,resource=BINDINGS[catalog]
        if identity.get('identity_kind')!='SkirmishLegacyPresetNativeIdentity' or identity.get('catalog_id')!=catalog or identity.get('scenario_index')!=index or identity.get('preset_resource')!=resource or identity.get('seed')!=slot['seed'] or identity.get('locale')!=slot['locale'] or any(k in identity for k in ('definition_id','definition_version','native_setup_hash_uint32')):raise ValueError('Actual legacy preset/configuration binding differs')
        if identity.get('native_configuration',{}).get('ScenarioIndex')!=index or identity['native_configuration'].get('MapSeed')!=slot['seed']:raise ValueError('Native QuickConfig differs from canonical slot')
        content=dict(identity);content.pop('seed');content.pop('locale');content.pop('native_configuration');content['identity_kind']='SkirmishLegacyPresetNativeContentIdentity'
        if fingerprint(identity)!=row['setup_sha256'] or fingerprint(content)!=row['content_sha256']:raise ValueError('Actual metadata identity digest differs')
        packet=authority.packet(authority.catalog[catalog])
        job=dict(schema_version=2,job_id=f'legacy-{catalog}-{slot["seed"]}-{slot["locale"]}-{n}',catalog_id=catalog,legacy_preset_index=index,legacy_preset_resource=resource,legacy_preset_sha256=identity['preset_asset_sha256'],candidate_sha256=snapshot['candidate_sha256'],build_sha256=snapshot['build_sha256'],setup_sha256=row['setup_sha256'],content_sha256=row['content_sha256'],source_manifest=snapshot['source_manifest'],packet_path=packet,packet_sha256=authority.pins[packet],seed=slot['seed'],locale=slot['locale'],size='Standard',difficulty='Regular',control_mode='aria',required_stages=['compile','live_api','source_fit','normal_input','terminal','return'],timeout_seconds=3600)
        preflight(job,project,authority);jobs.append(job)
    # Finish all validation before creating any output. Original queue owns enqueue/claim.
    output.mkdir(parents=True,exist_ok=False)
    for job in jobs:write_new(output/(job['job_id']+'.json'),job)
    write_new(output/'metadata-provenance.json',dict(metadata_sha256=metadata_sha,snapshot_sha256=snapshot_sha,plan_sha256=plan_sha,jobs=len(jobs),native_jobs=0,sampling_coverage_credit=0))
    return jobs

if __name__=='__main__':
    parser=argparse.ArgumentParser(description=__doc__)
    for name in ('project','plan','metadata','snapshot','output'):parser.add_argument('--'+name,type=Path,required=True)
    for name in ('plan-sha','metadata-sha','snapshot-sha'):parser.add_argument('--'+name,required=True)
    a=parser.parse_args();jobs=prepare(a.project,a.plan,a.plan_sha,a.metadata,a.metadata_sha,a.snapshot,a.snapshot_sha,a.output)
    print('[LegacyProductionJobs] result=PreparedUnqualified jobs='+str(len(jobs))+' nativeJobs=0 samplingCoverageCredit=0')
