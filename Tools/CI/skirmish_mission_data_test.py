"""Synthetic integrity/adversarial controls; real native export tested separately."""
from __future__ import annotations
import copy,hashlib,json,shutil,struct,tempfile,unittest
from pathlib import Path
import skirmish_mission_data as tool

ROOT=Path(__file__).resolve().parents[2]

def dump(path,data):
    path.parent.mkdir(parents=True,exist_ok=True);path.write_text(json.dumps(data,allow_nan=False)+'\n')
def sha(path):return hashlib.sha256(path.read_bytes()).hexdigest()

class ExportFixture:
    def __init__(self,base):
        self.project=base/'project';self.bundle=base/'bundle';self.bundle.mkdir();self.project.mkdir()
        paths=[tool.CATALOG,tool.MATRIX,'Packages/manifest.json','Packages/packages-lock.json',
            'Assets/Game/Scripts/Editor/Skirmish/SkirmishOfflineDataExporter.cs',
            'Assets/Game/Scripts/Editor/Skirmish/SkirmishOfflineDataExporter.Maps.cs',
            'Assets/Game/Scripts/Editor/Skirmish/SkirmishOfflineExportData.cs',
            'Assets/Menu.unity','Assets/Campaign.asset','Assets/Maps.asset','Assets/Battle.asset','Assets/Expansion.asset',
            'Assets/Tests/Editor/SkirmishOfflineDataExportTests.cs','Assets/Tests/Editor/SkirmishOfflineCampaignReferencesTests.cs','Assets/Matrix.csv','Assets/Map.asset','Assets/Grid.asset','Assets/Surface.asset','Assets/Physical.unity','Assets/Definition.asset']
        for name in paths:
            p=self.project/name;p.parent.mkdir(parents=True,exist_ok=True)
            if name in (tool.CATALOG,tool.MATRIX):shutil.copyfile(ROOT/name,p)
            else:p.write_text('synthetic fixture source '+name+'\n')
        self.manifest={'schema_version':1,'producer':tool.PRODUCER,'required_pass_marker':tool.MARKER,
            'menu_path':'Assets/Menu.unity','campaign_catalog_path':'Assets/Campaign.asset','campaign_map_catalog_path':'Assets/Maps.asset',
            'battle_catalog_path':'Assets/Battle.asset','expansion_catalog_path':'Assets/Expansion.asset','matrix_path':'Assets/Matrix.csv',
            'catalog_selection':'DefaultPackaged','compiler_catalog_scope':'PackagedRuntimeResources','selected_catalog_guid':'c'*32,'selected_catalog_local_id':11400000,'reasons':[],'source_pins':[],'artifacts':[],'maps':[],'missions':[],'campaign_rows':[]}
        for name in paths:self.pin(name)
        meta=self.project/'Assets/Expansion.asset.meta';meta.write_text('guid: '+'c'*32+'\n');self.pin('Assets/Expansion.asset.meta')
        next(p for p in self.manifest['source_pins'] if p['path']=='Assets/Expansion.asset')['guid']='c'*32
        self.artifact('walk.u8',bytes([1]*16),16,1,'u8 row-major x+z*width; authored GridAuthoringConfig only')
        self.artifact('surface.u8',bytes([1]*16),16,1,'u8 row-major surface dimensions; MapSurfaceTraversalValidation.CanTraverse Infantry')
        for name,count,stride,encoding in [('cells.bin',16,8,'le:i32 first_surface_index,u16 surface_count,u16 inline_surface_index'),('samples.bin',0,47,'le:i32 cell_x,i32 cell_z,i32 surface_id,i32 layer_id,f32 height,rest'),('connections.bin',0,19,'le:fixture empty connections'),('compact.bin',0,12,'le:fixture compact')]:self.artifact(name,bytes(count*stride),count,stride,encoding)
        self.manifest['campaign_rows']=[dict(mission_id='campaign.actual',definition_path='Assets/Definition.asset',scenario_path='Assets/Definition.asset',operation_map_id='actual.map',map_path='Assets/Map.asset',scene_guid='a'*32,reasons=[])]
        self.manifest['maps']=[dict(metadata={'bounds':{k:{'x':0 if k.endswith('Min') else 4,'y':0,'z':0 if k.endswith('Min') else 4} for k in ('worldMin','worldMax','playableMin','playableMax')}},key='b'*32,definition_path='Assets/Map.asset',source_scene_path='Assets/Physical.unity',source_scene_guid='a'*32,
            grid_path='Assets/Grid.asset',surface_path='Assets/Surface.asset',campaign_mission_ids=['campaign.actual'],campaign_definition_paths=['Assets/Map.asset'],
            campaign_physical_source_member=True,reasons=[],width=4,height=4,origin_x=0,origin_y=0,origin_z=0,cell_size=1,
            runtime_encoding=0,surface_width=4,surface_height=4,surface_origin_x=0,surface_origin_y=0,surface_origin_z=0,surface_cell_size=1,cells_artifact='data/cells.bin',samples_artifact='data/samples.bin',connections_artifact='data/connections.bin',compact_samples_artifact='data/compact.bin',authored_grid_available=True,decoded_surface_available=True,surface_grid_matches=True,walkable_artifact='data/walk.u8',infantry_surface_artifact='data/surface.u8')]
        for sid in tool.EXPECTED_IDS:
            variants=[]
            for size,difficulty in tool.VARIANTS:
                variants.append(dict(size=size,difficulty=difficulty,seed=0,compile_success=False,static_budget_available=False,static_budget_exact=False,
                    compile_reasons=[dict(code='UnavailableInputs',field='definition',detail='Synthetic test only')],static_budget_errors=[],forces=[],structures=[],desired_force_positions=[],desired_structure_positions=[],objective_area_configs=[]))
            self.manifest['missions'].append(dict(canonical_id=sid,map_key=None,definition_id=None,objective_json=None,logical_binding_json=None,
                catalog_rows=[],definition_refs=[],reasons=['Synthetic missing definition'],anchors=[],pads=[],routes=[],variants=variants))
        self.save()
    def pin(self,name,status='complete'):
        path=self.project/name
        self.manifest['source_pins'].append(dict(path=name,guid='',sha256=sha(path) if path.exists() else '',bytes=path.stat().st_size if path.exists() else 0,status=status))
    def artifact(self,name,raw,count,stride,encoding):
        path=self.bundle/'data'/name;path.parent.mkdir(exist_ok=True);path.write_bytes(raw)
        self.manifest['artifacts'].append(dict(path='data/'+name,sha256=sha(path),bytes=len(raw),count=count,stride=stride,encoding=encoding))
    def save(self):
        dump(self.bundle/'manifest.json',self.manifest)
        dump(self.bundle/'receipt.json',dict(schema_version=1,manifest_sha256=sha(self.bundle/'manifest.json'),required_pass_marker=tool.MARKER,source_pins_unchanged=True))
    def inspect(self,**kwargs):return tool.inspect(self.project,self.bundle,**kwargs)
    def point(self,id,x,z):return dict(id=id,role='authored-role',coordinate_source='Native synthetic fixture',world_x=x+.5,world_z=z+.5,cell_x=x,cell_z=z,has_cell=True,in_bounds=0<=x<4 and 0<=z<4)

class MissionDataTests(unittest.TestCase):
    def setUp(self):self.temp=tempfile.TemporaryDirectory();self.fx=ExportFixture(Path(self.temp.name))
    def tearDown(self):self.temp.cleanup()
    def errors(self,r):return [f for f in r['findings'] if f['severity']=='integrity']
    def test_complete_120_coverage_separate_from_readiness_and_repeat(self):
        a=self.fx.inspect();self.assertTrue(a['integrity_valid'],self.errors(a));self.assertEqual(a,self.fx.inspect());self.assertEqual(a['coverage']['variant_records'],1440)
        self.assertEqual(a['coverage']['replay_eligible_variants'],0);self.assertFalse(a['native_run_verified']);self.assertFalse(a['player_ready'])
    def test_sources_drift_new_policy_missing_and_lfs(self):
        (self.fx.project/'Assets/Grid.asset').write_text('drift');self.assertFalse(self.fx.inspect()['integrity_valid'])
    def test_new_source_closure_and_missing_pin(self):
        p=self.fx.project/'Assets/Game/Scripts/Added.cs';p.write_text('added');r=self.fx.inspect();self.assertTrue(any(f['code']=='source_pin_missing' for f in r['findings']))
    def test_accurate_missing_lfs_are_blockers_drift_is_integrity(self):
        self.fx.pin('Assets/Missing.asset','missing');p=self.fx.project/'Assets/Pointer.asset';p.write_bytes(tool.LFS_HEADER+b'\noid sha256:' + b'0'*64+b'\nsize 123\n');self.fx.pin('Assets/Pointer.asset','lfs_pointer');self.fx.save()
        r=self.fx.inspect();self.assertTrue(r['integrity_valid'],self.errors(r));self.assertEqual(2,sum(f['code'] in ('source_missing','source_lfs_pointer') for f in r['findings']))
        p.write_text('payload now');self.assertFalse(self.fx.inspect()['integrity_valid'])
    def test_artifact_tampering_and_bad_descriptor(self):
        p=self.fx.bundle/'data/walk.u8';p.write_bytes(bytes([0]*16));self.assertFalse(self.fx.inspect()['integrity_valid'])
        self.fx.manifest['artifacts'][0]['sha256']=sha(p);self.fx.manifest['artifacts'][0]['count']=15;self.fx.save();self.assertFalse(self.fx.inspect()['integrity_valid'])
    def test_duplicate_ids_missing_vector_and_nonfinite_json(self):
        self.fx.manifest['missions'][0]['variants'].pop();self.fx.save();r=self.fx.inspect();self.assertFalse(r['integrity_valid']);self.assertEqual(1439,r['coverage']['variant_records'])
        p=self.fx.bundle/'manifest.json';p.write_text('{"x":NaN}');self.assertFalse(self.fx.inspect()['integrity_valid'])
        p.write_text('{"schema_version":1,"schema_version":1}');self.assertFalse(self.fx.inspect()['integrity_valid'])
    def test_typed_anchor_route_mask_topology_no_native_path_claim(self):
        m=self.fx.manifest['missions'][0];m['map_key']='b'*32;m['definition_refs']=['Assets/Definition.asset']
        m['anchors']=[dict(authored={'AnchorId':id},projected=self.fx.point(id,x,z)) for id,x,z in [('a',0,0),('b',3,3),('outside',4,4)]]
        m['routes']=[dict(RouteId='route',WaypointAnchorIds=['a','b','missing'])];self.fx.save();r=self.fx.inspect();self.assertTrue(r['integrity_valid'],self.errors(r))
        self.assertTrue(r['missions'][0]['routes'][0]['segments'][0]['same_four_neighbour_mask_component']);self.assertTrue(any(f['code']=='route_anchor_missing' for f in r['findings']))
        self.assertTrue(any(f['code']=='point_out_of_grid' for f in r['findings']));self.assertFalse(r['missions'][0]['variants'][0]['replay_eligible'])
    def test_bounds_mismatch_and_campaign_membership_fail_closed(self):
        m=self.fx.manifest['missions'][0];m['map_key']='b'*32;p=self.fx.point('a',5,5);p['in_bounds']=True;m['anchors']=[dict(authored={'AnchorId':'a'},projected=p)]
        self.fx.manifest['maps'][0]['campaign_mission_ids']=['invented'];self.fx.save();r=self.fx.inspect();self.assertFalse(r['integrity_valid']);self.assertTrue(any(f['code']=='campaign_membership_mismatch' for f in r['findings']))
    def test_nonfinite_binary_and_path_escape(self):
        self.fx.artifact('invalid-samples.bin',struct.pack('<4i5fBHH iH',0,0,0,0,float('nan'),0,1,0,0,0,1,0,0,0),1,47,'le:i32 cell_x,i32 cell_z,i32 surface_id,i32 layer_id,f32 height,rest');self.fx.save();self.assertFalse(self.fx.inspect()['integrity_valid'])
        for value in ('../outside','/absolute','a//b','a/./b','C:/Windows','a\\b'):
            with self.assertRaises(ValueError):tool.safe_relative(self.fx.project,value)
    def test_draft_reuses_frozen_map_and_rejects_override_identity_and_roles(self):
        draft=Path(self.temp.name)/'draft.json'
        value=dict(schema_version=1,manifest_sha256=sha(self.fx.bundle/'manifest.json'),missions=[dict(canonical_id='S001',map_key='b'*32,points=[dict(id='a',role='infantry_anchor',cell=[0,0]),dict(id='b',role='route_waypoint',cell=[3,3])],routes=[dict(id='r',waypoints=['a','b'])])]);dump(draft,value)
        r=self.fx.inspect(draft=draft);self.assertTrue(r['integrity_valid'],self.errors(r));self.assertTrue(r['draft']['missions'][0]['routes'][0]['segments'][0]['same_four_neighbour_mask_component']);self.assertFalse(r['draft']['native_compiled_setup'])
        for mutate in (lambda v:v['missions'][0].update(budget=123),lambda v:v['missions'][0]['points'][0].update(role='unknown'),lambda v:v.update(manifest_sha256='0'*64),lambda v:v['missions'][0].update(map_key='invented'),lambda v:v['missions'][0]['points'][0].update(cell=[True,0])):
            invalid=copy.deepcopy(value);mutate(invalid);dump(draft,invalid);self.assertFalse(self.fx.inspect(draft=draft)['integrity_valid'])
    def test_surface_frame_claim_and_binary_mask_bounds(self):
        self.fx.manifest['maps'][0]['surface_cell_size']=2;self.fx.save();self.assertFalse(self.fx.inspect()['integrity_valid'])
        self.fx.manifest['maps'][0]['surface_cell_size']=1
        p=self.fx.bundle/'data/walk.u8';p.write_bytes(bytes([2]*16));self.fx.manifest['artifacts'][0]['sha256']=sha(p);self.fx.save();self.assertFalse(self.fx.inspect()['integrity_valid'])
    def test_output_guard_and_new_assembly_policy(self):
        for p in (self.fx.bundle/'manifest.json',self.fx.project/'Assets/new.json',self.fx.project/'Design/new.json'):
            with self.assertRaises(ValueError):tool.safe_output(self.fx.project,self.fx.bundle,p,[])
        draft=Path(self.temp.name)/'draft.json'
        with self.assertRaises(ValueError):tool.safe_output(self.fx.project,self.fx.bundle,draft,[draft])
        self.assertEqual((Path(self.temp.name)/'report.json').resolve(),tool.safe_output(self.fx.project,self.fx.bundle,Path(self.temp.name)/'report.json',[]))
        (self.fx.project/'Assets/Game/Scripts/New.asmdef').write_text('{}')
        self.assertTrue(any(f['code']=='source_pin_missing' and f['source_path'].endswith('New.asmdef') for f in self.fx.inspect()['findings']))
    def test_draft_actual_map_bounds_are_distinct_from_grid(self):
        self.fx.manifest['maps'][0]['metadata']['bounds']['playableMax']['x']=2;self.fx.save()
        draft=Path(self.temp.name)/'draft.json';dump(draft,dict(schema_version=1,manifest_sha256=sha(self.fx.bundle/'manifest.json'),missions=[dict(canonical_id='S001',map_key='b'*32,points=[dict(id='p',role='logical_anchor',cell=[3,1])],routes=[])]))
        r=self.fx.inspect(draft=draft);self.assertTrue(r['integrity_valid'],self.errors(r));point=r['draft']['missions'][0]['points'][0];self.assertTrue(point['in_bounds']);self.assertFalse(point['geometric_cell_envelope']['playable'])
    def test_compact_encoding_and_honest_decode_failure(self):
        m=self.fx.manifest['maps'][0];m.update(runtime_encoding=1,compact_min_height=0,compact_height_step=.1)
        for a in self.fx.manifest['artifacts']:
            if a['path'] in ('data/cells.bin','data/compact.bin'):
                count=0 if a['path']=='data/cells.bin' else 16;p=self.fx.bundle/a['path'];p.write_bytes(bytes(count*a['stride']));a.update(count=count,bytes=p.stat().st_size,sha256=sha(p))
        self.fx.save();r=self.fx.inspect();self.assertTrue(r['integrity_valid'],self.errors(r));self.assertEqual(1,r['coverage']['matching_surface_maps'])
        m['runtime_encoding']=0;self.fx.save();self.assertFalse(self.fx.inspect()['integrity_valid'])
        m.update(decoded_surface_available=False,surface_grid_matches=True,reasons=['Native decode rejected payload'])
        for field in ('cells_artifact','samples_artifact','connections_artifact','compact_samples_artifact','infantry_surface_artifact'):m[field]=''
        self.fx.save();r=self.fx.inspect();self.assertTrue(r['integrity_valid'],self.errors(r));self.assertEqual(0,r['coverage']['matching_surface_maps']);self.assertTrue(any(f['code']=='surface_decode_unavailable' for f in r['findings']))
    def test_nested_native_json_and_virtual_package_resolution(self):
        self.fx.manifest['missions'][0]['objective_json']='{"kind":1}'
        self.fx.manifest['missions'][0]['logical_binding_json']='{"Variants":[]}'
        lock=self.fx.project/'Packages/packages-lock.json';dump(lock,{'dependencies':{'com.example.test':{'version':'1.2.3','source':'registry'}}})
        pin=next(p for p in self.fx.manifest['source_pins'] if p['path']=='Packages/packages-lock.json');pin.update(sha256=sha(lock),bytes=lock.stat().st_size)
        folder=self.fx.project/'Library/PackageCache/com.example.test@cache';folder.mkdir(parents=True);dump(folder/'package.json',dict(name='com.example.test',version='1.2.3'));payload=folder/'source.txt';payload.write_text('actual installed payload')
        self.fx.manifest['source_pins'].append(dict(path='Packages/com.example.test/source.txt',guid='',sha256=sha(payload),bytes=payload.stat().st_size,status='complete'));self.fx.save()
        r=self.fx.inspect();self.assertTrue(r['integrity_valid'],self.errors(r));self.assertIn('com.example.test',r['coverage']['resolved_virtual_packages'])
        payload.write_text('changed');self.assertFalse(self.fx.inspect()['integrity_valid']);payload.write_text('actual installed payload')
        dump(folder/'package.json',dict(name='com.example.test',version='1.2.4'));self.assertFalse(self.fx.inspect()['integrity_valid'])
        dump(folder/'package.json',dict(name='com.example.test',version='1.2.3'));other=folder.parent/'com.example.test@another';shutil.copytree(folder,other);self.assertFalse(self.fx.inspect()['integrity_valid']);shutil.rmtree(other)
        payload.unlink();self.assertFalse(self.fx.inspect()['integrity_valid'])
    def test_nested_native_json_rejects_malformed_and_unknown_types(self):
        for value in ('{"n":NaN}',{'not':'string'},'{"x":1,"x":2}'):
            self.fx.manifest['missions'][0]['objective_json']=value;self.fx.save();self.assertFalse(self.fx.inspect()['integrity_valid'])
    def test_verified_native_receipt_is_separate_and_exact(self):
        self.assertFalse(self.fx.inspect(require_native=True)['integrity_valid'])
        log=self.fx.project/'Logs/native.log';log.parent.mkdir();log.write_text(tool.MARKER+'\n');receipt=Path(self.temp.name)/'proof.json'
        proof=dict(schema_version=1,manifest_sha256=sha(self.fx.bundle/'manifest.json'),native_log_path='Logs/native.log',native_log_sha256=sha(log),wrapper_exit=0,reaped=True,required_pass_marker=tool.MARKER);dump(receipt,proof)
        r=self.fx.inspect(native_receipt=receipt,require_native=True);self.assertTrue(r['integrity_valid'],self.errors(r));self.assertTrue(r['native_run_verified'])
        proof['wrapper_exit']=False;dump(receipt,proof);self.assertFalse(self.fx.inspect(native_receipt=receipt)['integrity_valid'])

if __name__=='__main__':unittest.main(verbosity=2)
