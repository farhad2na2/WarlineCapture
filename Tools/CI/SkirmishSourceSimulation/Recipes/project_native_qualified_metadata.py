"""Project exact qualified immutable native facts; never initialize a World."""
import argparse,copy,hashlib,json,re
from pathlib import Path
NATIVE_SHA='06ff2cf07f069988a04f9e1740cbc9d29461207014b566fe3f2845c205e7be17'
QUALIFIED={NATIVE_SHA:('NativeS026MetadataQualification',None),'6a55787a770c82623030907518e87343fad72b5c35d91ae293a5199128ba986a':('NativeS026LogicalBoundMetadataQualification','a6f7e1040b97c5daf76f86a9a81fcb48101863698c5f1593a2f6b00d1e70bf18'),'da1383237b7ec9bb0607596be496edc40d6a1cd9c15c12901d7dbee9731b5f37':('NativeS026LogicalBoundMetadataQualification','d0604a13ee34f00c19beb4b03be02850d5aa765592e4e33c0be3688dacd60443')}
def project(native_path,bridge_path,output):
 native_path=Path(native_path).resolve(strict=True);raw=native_path.read_bytes()
 native_sha=hashlib.sha256(raw).hexdigest()
 if native_sha not in QUALIFIED:raise ValueError('Exact passed native artifact required')
 expected_kind,expected_logical=QUALIFIED[native_sha]
 n=json.loads(raw)
 if n['kind']!=expected_kind or len(n['contexts'])!=12 or n['full_mission_initialized'] or n['runtime_state_transferred'] or n['public_admission_changed'] or (n.get('logical_binding_reference_sha256')!=expected_logical if expected_logical is not None else n['opening_placement_transferred']):raise ValueError('Exact metadata-only native context domain required')
 bridge_raw=Path(bridge_path).read_bytes();body=re.search(r'FactorySources=\{(.*?)\};',bridge_raw.decode(),re.S).group(1);rels=['Assets/Game/Scripts/'+s for s in re.findall(r'"([^"]+)"',body)];subset={k:n['source_pins'][k]for rel in rels for k in [rel,rel+'.meta']}
 out=Path(output).absolute()
 if out.exists():raise ValueError('Fresh isolated projection directory required')
 out.mkdir(parents=True);rows=[]
 def write(path,value):
  path.write_text(json.dumps(value,indent=2)+'\n');return {'path':str(path),'sha256':hashlib.sha256(path.read_bytes()).hexdigest()}
 for c in n['contexts']:
  m=c['metadata'];folder=out/(c['size']+'-'+c['difficulty']);folder.mkdir();origin={k:copy.deepcopy(c[k])for k in ['catalog_id','size','difficulty','seed','setup_hash','metadata_key_sha256']};origin.update(kind=n['kind'],artifact_sha256=native_sha)
  common={'schema_version':2,'authority':'OriginalNativeMetadataFactories_NotMissionState','catalog_id':c['catalog_id'],'setup_hash':c['setup_hash'],'seed':c['seed'],'native_origin':origin,'unsupported_values':[],'full_mission_initialized':False,'native_visual_readiness':False}
  d=copy.deepcopy(common);d.update(kind='NativeQualifiedSkirmishBuildingDefinitions',definitions=[{'key':r['prefab']['key'],'prefab':r['prefab'],'fields':r['fields']}for r in m['buildings']],source_pins=n['source_pins']);dd=write(folder/'building-definitions.json',d)
  u=copy.deepcopy(common);u.update(kind='NativeQualifiedSkirmishUnitProductionMetadata',baked_actor_footprint_substituted=False,runtime_camera_basis_exported=False,source_pins=n['source_pins'],transports=m['transports'],units=[dict(copy.deepcopy(r),key=r['prefab']['key'])for r in m['units']],ui_catalog_runtime_policy_sampled=False);ud=write(folder/'unit-production-metadata.json',u)
  s=copy.deepcopy(common);s.update(kind='NativeQualifiedConfiguredBuildingMetadata',reference=dd,source_pins=subset,rows=[{'definition_index':r['index'],'key':r['prefab']['key'],'prefab':r['prefab'],'display_name':r['configured_entry']['fields']['DisplayName']['value'],'can_request':r['configured_entry']['fields']['CanRequest']['value'],'construction_catalog_index':None if r['construction_index']<0 else r['construction_index'],'scene_catalog_index':None if r['scene_index']<0 else r['scene_index']}for r in m['buildings']]);sd=write(folder/'configured-metadata.json',s)
  rows.append({'selection':{k:c[k]for k in ['catalog_id','size','difficulty','seed','setup_hash']},'metadata_compatibility':{'path':str(native_path),'sha256':native_sha},'building_definitions':dd,'unit_production_metadata':ud,'building_request_metadata':sd,'building_count':len(m['buildings']),'unit_count':len(m['units'])})
 result={'kind':'NativeQualifiedImmutableMetadataProjectionIndex','native_artifact_sha256':native_sha,'contexts':rows,'runtime_admission':False,'positions_or_state_transferred':False};write(out/'projection-index.json',result)
 if native_path.read_bytes()!=raw or Path(bridge_path).read_bytes()!=bridge_raw:raise ValueError('Input changed during projection')
 return result


def project_declared(compiler_result,physical,graph,layout,current_assets,function_set,camera_function_profile,initialization_template,bundle_template,bridge_path,output,captured_root_override=None):
 """Project ACTUAL original compiler output into existing declared schemas; no World/admission.

 Inputs are explicit pinned descriptors. Shared templates supply immutable native/input
 descriptors only; target, binding, profile, source and layout come from actual owners.
 The normal collector/C# validators remain the admission authority.
 """
 def pinned(d,bound,decode=True):
  if not isinstance(d,dict)or set(d)!={'path','sha256'}:raise ValueError('Exact descriptor required')
  p=Path(d['path']).resolve(strict=True);raw=p.read_bytes()
  if not 0<len(raw)<=bound or hashlib.sha256(raw).hexdigest()!=d['sha256']:raise ValueError('Pinned input differs: '+str(p))
  return json.loads(raw)if decode else raw
 observed=pinned(compiler_result,32*1024*1024);actual=observed['actual_original_compiler'];c=actual['actual_derived_factory_context']
 roundtrip=actual.get('actual_binding_roundtrip_same_setup_and_compiled_anchors',actual.get('actual_binding_roundtrip_same_setup_and20anchors'));compiled=actual.get('compiled_structures')
 if actual['original_complete_compiler_passed']is not True or roundtrip is not True or not isinstance(compiled,list)or not compiled or not isinstance(c,dict):raise ValueError('Actual original compiler/roundtrip/compiled structures/context required')
 if 'actual_binding_roundtrip_same_setup_and_compiled_anchors'in actual and 'actual_binding_roundtrip_same_setup_and20anchors'in actual and actual['actual_binding_roundtrip_same_setup_and20anchors']is not True:raise ValueError('Conflicting original compiler roundtrip declarations')
 target=c['target_context'];portable=actual['actual_portable_binding_fields']
 if len(portable['Variants'])!=1:raise ValueError('One actual selected compiler variant required')
 native_ref=c['native_factory_reference'];n=pinned(native_ref,64*1024*1024)
 # Existing qualified ORIGINAL native factory epoch; no new digest/catalog admission.
 if native_ref['sha256']not in QUALIFIED or n['kind']!=QUALIFIED[native_ref['sha256']][0]:raise ValueError('Existing qualified native factory required')
 raw=pinned(physical,256*1024*1024);config=pinned(graph,32*1024*1024);layout_value=pinned(layout,4*1024*1024);init=copy.deepcopy(pinned(initialization_template,4*1024*1024));bundle=copy.deepcopy(pinned(bundle_template,4*1024*1024));pinned(function_set,4*1024*1024);pinned(camera_function_profile,4*1024*1024)
 if raw['result']!='Collected'or any(raw[x]for x in ['sourceFitCertified','missionReady','missionInitialized']):raise ValueError('Original physical-only source required')
 if raw['operationMapId']!=target['operation_map_id']or layout_value['operationMapId']!=target['operation_map_id']:raise ValueError('Actual source/layout target differs')
 for key in ['definition','grid','surface']:
  d=current_assets[key];pinned({k:d[k]for k in ['path','sha256']},16*1024*1024,False)
  if not any(v['source_path']==d['source_path']and v['source_sha256']==d['sha256']for v in config['nodes']):raise ValueError('Current graph asset differs: '+key)
 if current_assets['definition']['sha256']!=raw['sourceSha256']:raise ValueError('Captured/current source differs')
 out=Path(output).absolute()
 if out.exists():raise ValueError('Fresh isolated projection directory required')
 out.mkdir(parents=True)
 def write(name,v):
  p=out/name;p.write_text(json.dumps(v,indent=2)+'\n');return {'path':str(p),'sha256':hashlib.sha256(p.read_bytes()).hexdigest()}
 cd=write('actual-derived-factory-context.json',c);qd=write('configuration-qualification.json',{'schema_version':1,'kind':'ExperimentalGroundConfigurationQualification','runtime_backend':'TypedGroundS026V1','compiler_context':cd,'scope':'OriginalCompilerAndQualifiedFactoryInputs_SourceOnly','native_context_qualified':False,'mission_ready':False})
 origin={k:copy.deepcopy(c['native_origin'][k])for k in ['catalog_id','size','difficulty','seed','setup_hash','metadata_key_sha256']};origin.update(kind=n['kind'],artifact_sha256=native_ref['sha256'])
 common={'schema_version':3,'authority':'OriginalCompilerContext_ImmutableNativeFactoryFacts_NotNativeS050','catalog_id':target['catalog_id'],'setup_hash':target['setup_hash'],'seed':target['seed'],'native_origin':origin,'metadata_compiler_context':cd,'unsupported_values':[],'full_mission_initialized':False,'native_visual_readiness':False,'native_S050_capture':False};m=c['derived_metadata']
 # Same existing project() factory projections; only the declared envelope/target differs.
 bridge_raw=Path(bridge_path).read_bytes();body=re.search(r'FactorySources=\{(.*?)\};',bridge_raw.decode(),re.S).group(1);rels=['Assets/Game/Scripts/'+s for s in re.findall(r'"([^"]+)"',body)];subset={k:n['source_pins'][k]for rel in rels for k in [rel,rel+'.meta']}
 d=copy.deepcopy(common);d.update(kind='CompilerContextDerivedSkirmishBuildingDefinitions',definitions=[{'key':r['prefab']['key'],'prefab':r['prefab'],'fields':r['fields']}for r in m['buildings']],source_pins=n['source_pins']);dd=write('building-definitions.json',d)
 u=copy.deepcopy(common);u.update(kind='CompilerContextDerivedSkirmishUnitProductionMetadata',baked_actor_footprint_substituted=False,runtime_camera_basis_exported=False,source_pins=n['source_pins'],transports=m['transports'],units=[dict(copy.deepcopy(r),key=r['prefab']['key'])for r in m['units']],ui_catalog_runtime_policy_sampled=False);ud=write('unit-production-metadata.json',u)
 s=copy.deepcopy(common);s.update(kind='CompilerContextDerivedConfiguredBuildingMetadata',reference=dd,source_pins=subset,rows=[{'definition_index':r['index'],'key':r['prefab']['key'],'prefab':r['prefab'],'display_name':r['configured_entry']['fields']['DisplayName']['value'],'can_request':r['configured_entry']['fields']['CanRequest']['value'],'construction_catalog_index':None if r['construction_index']<0 else r['construction_index'],'scene_catalog_index':None if r['scene_index']<0 else r['scene_index']}for r in m['buildings']]);sd=write('configured-metadata.json',s)
 ld=write('layout.logical.json',layout_value);bd=write('declared-logical-binding.json',{'schema_version':1,'kind':'OriginalCompilerDeclaredLogicalBinding','metadata_compiler_context':cd,'target_context':target,'portable_binding':{'schema_version':1,'kind':'DeclaredOriginalLogicalBindingFieldsWithInlineViews','float_encoding':'IEEE754_float32_hex_bits','unity_object_ids_exported':False,'fields':portable},'native_capture':False,'source_fit_certified':False,'mission_ready':False})
 pd=write('engineering-camera-context.json',{'schema_version':1,'kind':'OriginalCompilerDerivedEngineeringCameraContext','metadata_compiler_context':cd,'target_context':target,'profile_fields':portable['Variants'][0]['View'],'native_function_profile':camera_function_profile,'native_camera_context_qualified':False,'source_profile_derived':True})
 fixed=raw['fixedMapReadDomain'];dto=raw['headlessPlacementInputs'];assets=copy.deepcopy(current_assets);assets.update(config_graph=graph,layout_fields=ld)
 contract=write('capture-contract.json',{'schema_version':1,'kind':'QualifiedOriginalFixedMapCaptureContract','native_readback':physical,'fixed_map_kind':fixed['kind'],'source_definition_sha256':raw['sourceSha256'],'operation_map_id':raw['operationMapId'],'frame':{'width':dto['gridWidth'],'height':dto['gridHeight'],'cell_size':dto['gridCellSize'],'origin':dto['gridOrigin'],'source_grid_sha256':current_assets['grid']['sha256']},'navigation_rows':len(fixed['authoredNavigationBlockers']),'drop_rows':len(fixed['dropClearanceBounds']['bounds']),'loaded_runtime_assemblies':raw['loadedRuntimeAssemblies'],'current_assets':assets,'source_fit_certified':False,'native_mission_context_qualified':False,'runtime_admitted':False})
 fd=write('fixed-map-context.json',{'schema_version':1,'kind':'OriginalCapturedFixedMapCompilerContext','metadata_compiler_context':cd,'configuration_qualification':qd,'target_context':target,'native_readback':physical,'native_mission_context_qualified':False,'runtime_admitted':False,'capture_contract':contract})
 plain=lambda k:{x:current_assets[k][x]for x in ['path','sha256']}
 init.update(mission_id=target['catalog_id'],source_config_graph=graph,selection={'difficulty_id':portable['Variants'][0]['Difficulty'],'size_id':portable['Variants'][0]['Size'],'seed':target['seed']},static_map={'operation_map_id':target['operation_map_id'],'definition':plain('definition'),'grid_config':plain('grid'),'surface_data':plain('surface')},building_definitions=dd,building_request_metadata=sd,unit_production_metadata=ud,navigation_reference=fd,selected_operation_map=plain('definition'),mission_logical_binding=bd,metadata_compiler_context=cd,engineering_camera_profile=pd,configuration_qualification=qd,operation_map_rotation_reference=function_set,source_changes=[])
 if captured_root_override is not None:
  capture_root=Path(captured_root_override);need_root=capture_root.is_absolute() and capture_root.is_dir() and not any(x.is_symlink() for x in [capture_root,*capture_root.parents]);
  if not need_root:raise ValueError('Canonical non-symlink captured root required')
  captured_root_override=str(capture_root.resolve(strict=True))
 init['placement_domain']={'original_owner_masks':physical,'captured_asset_origin':{'source_root':captured_root_override if captured_root_override is not None else raw['capturedProjectRoot'],'source_definition_path':current_assets['definition']['source_path'],'source_definition_sha256':raw['sourceSha256']}};init['typed_mission_configuration']=dict(init['typed_mission_configuration'],campaign_source_path=current_assets['definition']['source_path'],layout=ld);ide=write('initialization.json',init)
 bundle.update(mission=target['catalog_id'],seed=target['seed'],initialization=ide,unit_metadata=ud,static_clearance=fd,camera_profile=pd,scope='ActualOriginalCompilerContextAndImmutableNativeFacts_PhysicalSourceOnly_NotNativeMissionAcceptance');write('data-bundle.json',bundle)
 result={'kind':'DeclaredOriginalCompilerInputProjection','target_context':target,'actual_compiler_result':compiler_result,'native_factory_reference':native_ref,'physical_capture':physical,'rotation_function_set':function_set,'context_root':c['current_factory_source_root'],'source_fit_certified':False,'mission_ready':False,'runtime_admission':False,'world_allocated':False};write('projection-index.json',result);return result

if __name__=='__main__':
 p=argparse.ArgumentParser();p.add_argument('--native',required=True);p.add_argument('--bridge',required=True);p.add_argument('--output',required=True);a=p.parse_args();r=project(a.native,a.bridge,a.output);print('Projected exact contexts='+str(len(r['contexts'])))
