"""Offline contract controls only; synthetic identities cannot qualify native jobs."""
import copy
import hashlib
from pathlib import Path
import sys
import unittest
import tempfile
import json
ROOT=Path(__file__).resolve().parents[4]
sys.path.append(str(ROOT/'Tools/CI'))
import skirmish_production_queue as queue
import prepare_skirmish_legacy_production_jobs as builder
from s005_windows_source_snapshot import candidate_hash
from skirmish_production_matrix_authority import SamplingAuthority

class LegacyQueueControls(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        plan=ROOT/'Tools/CI/SkirmishProductionDrafts/ProductionBaselineBatchCandidateAdmission/baseline720.plan.json'
        cls.authority=SamplingAuthority(ROOT,plan,queue.digest(plan))
    def job(self,id):
        index,resource={'S001':(0,'SkirmishBaseAssault'),'S025':(1,'SkirmishCityCrossroads'),'S073':(3,'SkirmishIndustrialBasin')}[id]
        slot=next(v for v in self.authority.slots.values() if v['catalog_id']==id)
        packet=self.authority.packet(self.authority.catalog[id]);preset='Assets/Game/Resources/'+resource+'.asset'
        return dict(schema_version=2,job_id='synthetic-'+id,catalog_id=id,legacy_preset_index=index,legacy_preset_resource=resource,legacy_preset_sha256=queue.digest(ROOT/preset),candidate_sha256='1'*64,build_sha256='2'*64,setup_sha256='3'*64,content_sha256='4'*64,source_manifest=[dict(path=preset,sha256=queue.digest(ROOT/preset))],packet_path=packet,packet_sha256=queue.digest(ROOT/packet),seed=slot['seed'],locale=slot['locale'],size=slot['size'],difficulty=slot['difficulty'],control_mode='aria',required_stages=sorted(queue.STAGES),timeout_seconds=3600)
    def test_three_actual_preset_bindings_and_canonical_slots(self):
        for id in ('S001','S025','S073'):
            self.assertEqual(queue.preflight(self.job(id),ROOT,self.authority)['schema_version'],2)
    def test_wrong_preset_or_fabricated_expanded_identity_refused(self):
        job=self.job('S001')
        for key,value in [('catalog_id','S005'),('legacy_preset_index',1),('legacy_preset_index',True),('legacy_preset_resource','SkirmishIndustrialBasin'),('legacy_preset_sha256','a'*64),('seed',104729),('difficulty','Veteran'),('schema_version',1),('definition_id','skirmish.s001')]:
            bad=copy.deepcopy(job);bad[key]=value
            with self.assertRaises((ValueError,TypeError,KeyError)):queue.preflight(bad,ROOT,self.authority)
        fake=copy.deepcopy(job)
        for key in ('legacy_preset_index','legacy_preset_resource','legacy_preset_sha256'):fake.pop(key)
        fake.update(schema_version=1,definition_id='skirmish.s001',definition_version=1)
        with self.assertRaises(ValueError):queue.preflight(fake,ROOT,self.authority)
    def test_complete_authority_mandatory_and_no_qualification(self):
        with self.assertRaises(ValueError):queue.preflight(self.job('S073'),ROOT)
        with self.assertRaises(ValueError):queue.finish(ROOT,'unused','qualified')
    def test_builder_requires_exact_metadata_slots_hashes_and_native_kind(self):
        plan=self.authority.path
        rows=[]
        for slot in self.authority.slots.values():
            if slot['catalog_id'] not in builder.BINDINGS:continue
            index,resource=builder.BINDINGS[slot['catalog_id']]
            # Deliberately synthetic metadata. Native schema/config verification remains mandatory at actual dispatch.
            identity=dict(identity_kind='SkirmishLegacyPresetNativeIdentity',catalog_id=slot['catalog_id'],scenario_index=index,preset_resource=resource,preset_asset_sha256=queue.digest(ROOT/('Assets/Game/Resources/'+resource+'.asset')),seed=slot['seed'],locale=slot['locale'],native_configuration=dict(ScenarioIndex=index,MapSeed=slot['seed']))
            preset='Assets/Game/Resources/'+resource+'.asset'
            identity.update(schema_version=2,preset_asset_path=preset,preset_dependency_identity='0'*32,source_asset_path=preset,source_asset_sha256=queue.digest(ROOT/preset),source_content_identity='synthetic-contract-control',source_identity='synthetic-contract-control')
            # These are contract-only fixtures, not native source/configuration observations.
            identity['source_resolution']=dict(mode='PresetOperationMap',presetOperationMapPresent=True)
            if slot['catalog_id']=='S001':
                scene='Assets/Game/Scenes/Match.unity';catalog='Assets/Game/Configs/OperationMaps/OperationMapCatalog_Compatibility.asset'
                identity['source_resolution']=dict(mode='CompatibilityCatalogFallback',presetOperationMapPresent=False,operationMapId='opmap.skirmish.desert_base_01',matchScenePath=scene,matchSceneSha256=queue.digest(ROOT/scene),catalogPath=catalog,catalogSha256=queue.digest(ROOT/catalog))
            content=dict(identity);content.pop('seed');content.pop('locale');content.pop('native_configuration');content['identity_kind']='SkirmishLegacyPresetNativeContentIdentity'
            rows.append(dict(kind='LegacyPresetExpectedNativeMetadata',sampling_slot=slot,native_setup_identity=identity,setup_sha256=queue.fingerprint(identity),content_sha256=queue.fingerprint(content),live_session_observed=False))
        metadata=dict(schema_version=1,sampling_plan_sha256=self.authority.expected,rows=rows,native_jobs=0,queue_success_enabled=False)
        sources=self.job('S001')['source_manifest'];builds=[dict(path='Library/ScriptAssemblies/Synthetic.dll',sha256='a'*64)]
        snapshot=dict(source_manifest=sources,candidate_sha256=candidate_hash(sources),build_manifest=builds,build_sha256=queue.fingerprint(builds))
        scratch=ROOT/'Logs/SkirmishProduction/Offline';scratch.mkdir(parents=True,exist_ok=True)
        with tempfile.TemporaryDirectory(prefix='legacy-queue-controls-',dir=scratch) as temporary:
            t=Path(temporary);m=t/'metadata.json';s=t/'snapshot.json'
            def write(path,value):path.write_text(json.dumps(value),encoding='utf-8');return queue.digest(path)
            ss=write(s,snapshot);ms=write(m,metadata)
            jobs=builder.prepare(ROOT,plan,self.authority.expected,m,ms,s,ss,t/'valid')
            self.assertEqual(len(jobs),18);self.assertTrue(all(j['schema_version']==2 for j in jobs))
            for i,mutation in enumerate(('omitted','nativeclaim','changedseed','expanded','wronghash','oldschema','missingroute','wrongmode','missingscene','changedscene','missingcatalog','changedcatalog')):
                bad=copy.deepcopy(metadata)
                if mutation=='omitted':bad['rows'].pop()
                if mutation=='nativeclaim':bad['rows'][0]['live_session_observed']=True
                if mutation=='changedseed':bad['rows'][0]['native_setup_identity']['seed']+=1
                if mutation=='expanded':bad['rows'][0]['native_setup_identity']['definition_version']=1
                if mutation=='wronghash':bad['rows'][0]['setup_sha256']='f'*64
                identity=bad['rows'][0]['native_setup_identity']
                if mutation=='oldschema':identity['schema_version']=1
                if mutation=='missingroute':identity.pop('source_resolution')
                if mutation=='wrongmode':identity['source_resolution']['mode']='InventedFallback'
                if mutation=='missingscene':identity['source_resolution'].pop('matchScenePath')
                if mutation=='changedscene':identity['source_resolution']['matchSceneSha256']='f'*64
                if mutation=='missingcatalog':identity['source_resolution']['catalogPath']='Assets/Missing.asset'
                if mutation=='changedcatalog':identity['source_resolution']['catalogSha256']='f'*64

                digest=write(m,bad)
                with self.assertRaises(ValueError):builder.prepare(ROOT,plan,self.authority.expected,m,digest,s,ss,t/str(i))
                self.assertFalse((t/str(i)).exists())

if __name__=='__main__':unittest.main(verbosity=2)
