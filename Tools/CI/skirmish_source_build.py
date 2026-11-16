"""One-entry source-only S002 build/run plumbing. Never launches Unity or a player."""
from __future__ import annotations
import hashlib,json,os,platform,re,shutil,subprocess,sys,time,importlib.util,math,types,struct
from pathlib import Path
from skirmish_source_manifest import resolve_manifest,need,sha,UnsupportedSourceShape
import skirmish_owned_source as owned
from skirmish_source_profiles import require_profile # admitted first by owned.load_cli
sys.dont_write_bytecode=True

S073_WATER_CAPTURE_REVISIONS={
 'Logs/SkirmishProduction/s073-native-water-mask-20261101-r1':('f6cc82e8589fb5dd267c92f64c292eb2786c21f87ff6b7c1b6527c061c52abf9','587f910720e0958b8b1dfee7a1e9ec683a3b15fc878299c3528929e22a603102','5647f05ec18958947d32874eeb788fa396a05d0bab7c1b71f112ceb7e9b31eee'),
 'Logs/SkirmishProduction/s073-native-startup-playing-water-20261101-r5':('d79405175f6bc8c526fe1198a5e2eb93bcf10e135e3088a6e685e3f2a77540af','107e26f463fa10228f2f421d1eaa184f83f62262fc377272f5eb62ce1062b171','5647f05ec18958947d32874eeb788fa396a05d0bab7c1b71f112ceb7e9b31eee'),
 'Logs/SkirmishProduction/s073-native-startup-playing-water-20261102-r1':('e5963e2f4d010c1dfcebcd5a92272858af9a3542eca89965f99cc835baa0bf3e','589cf99395fb17b2d1c9d75807a65eedfc7e0651ff84bdd0bea44b156d33c4c2','5647f05ec18958947d32874eeb788fa396a05d0bab7c1b71f112ceb7e9b31eee'),
 'Logs/SkirmishProduction/s073-loaded-source-fit-20261102-r2':('ba28b7dc90e6db335686df62b64c1ef6246a056cfaa1648f94ca4cf5a23f561a','165c1efdc74e6c5ab00e5a9cd5a90504ef0a32e26d1a538b2d585d48d95fad3f','5647f05ec18958947d32874eeb788fa396a05d0bab7c1b71f112ceb7e9b31eee'),
 'Logs/SkirmishProduction/s073-native-playing-occupancy-20261102-r3-finalized-r2':('5593b9b8e81a895750aea55aa93666fe75b5aa8e6c65e771334394d6ea6d7d64','d33dedfd5c85d8263a84f70262399315416713c041a1b0f3d7bab6fadb4852cc','5647f05ec18958947d32874eeb788fa396a05d0bab7c1b71f112ceb7e9b31eee'),
}
S073_WATER_PARITY_REVISIONS={
 'Logs/SkirmishProduction/s073-native-startup-playing-water-20261101-r2':('59cb4faaef863cc56f9de5107945bf5c057f21a56980de5caf1341cbfa0a81aa',False),
 'Logs/SkirmishProduction/s073-native-startup-playing-water-20261101-r5':('623f886f91605bab63c3190383b843aeb7ba114d7aaeb24a5acef9cd8078a966',True),
 'Logs/SkirmishProduction/s073-native-startup-playing-water-20261102-r1':('a277dc72fc56b7ade4c2a4232a91ecce1103ca70fdeb1b1ba60a8e57dd279bd2',True),
 'Logs/SkirmishProduction/s073-loaded-source-fit-20261102-r2':('a1991821818bf53b31d66c7773ef2a68e10645ed9af744236266e0c04031d8c1',True),
 'Logs/SkirmishProduction/s073-native-playing-occupancy-20261102-r3-finalized-r2':('ad2dc2aa0bd7c4009628ae1ec53885a713feef43a9abcdb43d3da069eb6e1e4a',True),
}
S002_FOCUSED_PREFAB_FAILED_CAPTURE_SHA='31b939d930cf5d9e6d8eac1b3325e7272756cedac916fb8d36a17734954a011e'
S049_METADATA_ONLY_BUNDLE_PATH='/Volumes/T7/Projects/WarlineCapture/Logs/SkirmishProduction/Offline/S049MetadataOnlyRecapture_20261102/Projection-17/data-bundle.json'
S049_METADATA_ONLY_BUNDLE_SHA='098132b31b89f053baa97b900b92bb32dc6050e934adad11fa9ae6dbaece171e'
S049_METADATA_ONLY_INITIALIZATION_PATH='/Volumes/T7/Projects/WarlineCapture/Logs/SkirmishProduction/Offline/S049MetadataOnlyRecapture_20261102/Projection-17/initialization.json'
S049_METADATA_ONLY_INITIALIZATION_SHA='562f98cfd275da945c32cedfb66f6f69a27819721d6f721c46a1422651b4b965'
S049_METADATA_ONLY_EPOCH_PATH='/Volumes/T7/Projects/WarlineCapture/Logs/SkirmishProduction/Offline/S049MetadataOnlyRecapture_20261102/Projection-17/metadata-epoch-context.json'
S049_METADATA_ONLY_EPOCH_SHA='f50e16deb9a6e780f3fbd9c3b1b6ad592fa21ffd18daecf1eeddf9fce756669f'
S049_METADATA_ONLY_QUALIFICATION_PATH='/Volumes/T7/Projects/WarlineCapture/Logs/SkirmishProduction/Offline/S049MetadataOnlyRecapture_20261102/Projection-17/configuration-qualification.json'
S049_METADATA_ONLY_QUALIFICATION_SHA='e0504d33bd5c553049ad67e8ef847be467622afc919b6b17e67961296a697c13'
S049_METADATA_ONLY_FIXED_MAP_PATH='/Volumes/T7/Projects/WarlineCapture/Logs/SkirmishProduction/Offline/S049MetadataOnlyRecapture_20261102/Projection-17/fixed-map-context.json'
S049_METADATA_ONLY_FIXED_MAP_SHA='6c7f61b3231a1d23ac1262d0c0ce0dff00eac43485984e4f8224843a8da22f38'
S049_METADATA_ONLY_NATIVE_READBACK_SHA='6d90b4576abd359dc09eda7cd05108be5212bcf286283e7cec867a56f85ddae3'
S049_METADATA_ONLY_SOURCE_DEFINITION_SHA='181418e5b316d8a5625913d0251e5f5a9a83ab81f56baec49e4adcb2bb89e3b4'
S049_METADATA_ONLY_CAMERA_PROFILE_SHA='0357aacf68cd3a96a8d715775dd838b4d4b32d60cf320caba7810fe0cb12299e'
S049_METADATA_ONLY_CAMERA_VALUES_SHA='cb5a40d3aa7b5b125eb509257045e4447baae969f5b325d6b7429449eab61e79'
S049_METADATA_ONLY_CAMERA_MOTION_SHA='18a63db2b9a6d7c39e9898efe5825295143c4b6de4ce714243e51886f12e4765'
S049_METADATA_ONLY_SOURCE_REFERENCE_SHA='83aec3d79c2ac069527c438be1056df9639e40f325b4a3a3c479e090996dd70f'
S049_METADATA_ONLY_TEMPLATE_SLICE_SHA='4f548f40239059a37cfefcab777413cb49dfbbfce7c04757016574382aacb0e2'
S049_DIAGNOSTIC_PREFLIGHT_KIND='S049NativeDiagnosticPreflightEvidenceV1'
S049_DIAGNOSTIC_CONTEXT_KIND='S049Projection17NativeDiagnosticV1'
S049_PHYSICAL_WATER_MARKER='[S049PhysicalWaterDiagnostic] active_session_equivalence=0 native_water_qualified=0 source_fit_certified=0'
S049_CAMERA_MARKER='[S049CameraDiagnostic] native_camera_qualified=0 motion_reference_passed=0 exact_mismatch=Consumed camera state=124 pose=0 phase=124 detail=Native value reference vector mismatch actual=float3(1644.26f, 14f, 670.651f) expected=[1644.1658935546875,14.0,670.61224365234375] tolerance=0.025'
S073_WATER_SOURCE_PIN_PATHS={
 'Assets/Game/Scripts/Editor/Skirmish/S073NativeWaterMaskSnapshot.cs','Assets/Game/Scripts/Components/GridComponents.cs',
 'Assets/Game/Scripts/Components/InitialUnitsSpawnComponents.cs','Assets/Game/Scripts/Components/OperationMapBuildingComponents.cs',
 'Assets/Game/Scripts/Systems/StaticGridBlockerUpdateSystem.cs','Assets/Game/Scripts/Systems/InitialUnitsSpawnSystem.cs',
 'Assets/Game/Scripts/Systems/DynamicOccupancyRebuildSystem.cs','Assets/Game/Scripts/Systems/GridOccupancyKernel.cs',
 'Assets/Game/Scripts/Systems/SkirmishWaterPlacement.cs','Assets/Game/Scripts/Systems/BuildingPlacementAuthoredRoadCache.cs',
 'Assets/Game/Scripts/Rendering/RenderBoundsPresentation.cs','Assets/Game/Scripts/Rendering/Contracts/IRenderBoundsPresentation.cs',
 'Assets/Game/Configs/Skirmish/IndustrialBasin/OperationMap_IndustrialBasin.asset','Assets/Game/Scenes/Match.unity',
 'Assets/Game/GeneratedOperationMaps/RuntimeBinding/opmap.skirmish.desert_base_01/Candidates/opmap_skirmish_desert_base_01_dense_city_entity_scene_runtime.unity',
 'Assets/Game/GeneratedOperationMaps/RuntimeBinding/opmap.skirmish.desert_base_01/Candidates/opmap_skirmish_desert_base_01_dense_city_entity_scene_runtime.unity.meta',
 'Assets/Game/Scenes/OperationMaps/Skirmish/Candidates/opmap_skirmish_desert_base_01_entity_presentation_dense_city_candidate.unity',
 'Assets/Game/Scenes/OperationMaps/Skirmish/Candidates/opmap_skirmish_desert_base_01_entity_presentation_dense_city_candidate.unity.meta',
}

def _select_s073_exact_revision(repo, receipt_path, revisions):
 receipt_descriptor=Path(receipt_path).resolve(strict=True)
 for relative,metadata in revisions.items():
  candidate=(Path(repo)/relative).resolve(strict=True)
  if receipt_descriptor==candidate/('water-receipt.json' if len(metadata)==3 else 'joint-receipt.json'):
   return candidate,metadata
 return None

def pin(path):
 p=Path(path).resolve(strict=True);raw=p.read_bytes();return {'path':str(p),'sha256':sha(raw),'bytes':len(raw)}
def source_mission_scene_pin(data_repo):
 # Match.unity is also road-source data for the S074 no-camera diagnostic.
 return pin(Path(data_repo)/'Assets/Game/Scenes/Match.unity')
def verify(pins):
 for path,expected in pins.items():need(sha(Path(path).read_bytes())==expected,'Input changed: '+path)
def merge_pins(pins,other):
 verify(pins)
 for name,expected in other.items():
  key=str(Path(name).resolve(strict=True))
  need(key not in pins or pins[key]==expected,'Conflicting source/runtime epoch: '+key)
  need(sha(Path(key).read_bytes())==expected,'Input changed before merge: '+key)
  pins[key]=expected
def merge_file_pins(pins,paths):
 added={}
 for path in paths:
  entry=pin(path);key=entry['path'];expected=entry['sha256']
  need(key not in added or added[key]==expected,'Conflicting captured source/runtime epoch: '+key)
  added[key]=expected
 merge_pins(pins,added)
def read_json_pinned(path):
 p=Path(path).resolve(strict=True);raw=p.read_bytes();return json.loads(raw),{'path':str(p),'sha256':sha(raw),'bytes':len(raw)}
def _safe_source_json(path,limit=64*1024*1024):
 p=Path(path)
 need(p.is_absolute()and not p.is_symlink()and p.is_file(),'Canonical source provenance JSON required')
 size=p.stat().st_size;need(0<size<=limit,'Bounded source provenance JSON required')
 raw=p.read_bytes();need(len(raw)==size,'Source provenance JSON changed while read')
 def pairs(rows):
  value={}
  for key,item in rows:
   need(key not in value,'Duplicate source provenance JSON key: '+key);value[key]=item
  return value
 def constant(value):raise UnsupportedSourceShape('Nonfinite source provenance JSON constant: '+value)
 try:value=json.loads(raw,object_pairs_hook=pairs,parse_constant=constant)
 except (json.JSONDecodeError,UnicodeError)as error:raise UnsupportedSourceShape('Malformed source provenance JSON: '+str(p))from error
 need(type(value)is dict,'Source provenance JSON object required');return value,hashlib.sha256(raw).hexdigest(),size
def _source_provenance_pin(path,digest,*maps):
 need(type(path)is str and Path(path).is_absolute()and type(digest)is str and re.fullmatch('[0-9a-f]{64}',digest),'Exact source provenance pin required')
 for pins in maps:need(type(pins)is dict and pins.get(path)==digest,'Source provenance pin missing/conflicts: '+path)
 p=Path(path);need(not p.is_symlink()and p.is_file(),'Pinned source provenance file required: '+path)
 size=p.stat().st_size;need(0<size<=64*1024*1024,'Pinned source provenance file exceeds bound: '+path)
 need(sha(p.read_bytes())==digest,'Pinned source provenance drift: '+path)
def _same_source_json(left,right):
 return json.dumps(left,sort_keys=True,separators=(',',':'),ensure_ascii=False)==json.dumps(right,sort_keys=True,separators=(',',':'),ensure_ascii=False)
def source_build_provenance(receipt):
 """Validate new resolution proof; old build receipts remain runnable but diagnostic-only."""
 if type(receipt)is not dict:return {'trusted':False,'experimental':False,'canonical':False,'reason':'MalformedBuildReceipt'}
 has_resolution='source_resolution_pin'in receipt;has_canonical='canonical_source_manifest'in receipt
 if not has_resolution and not has_canonical:return {'trusted':False,'experimental':False,'canonical':False,'reason':'LegacyBuildReceiptWithoutResolutionProof'}
 if not has_resolution or not has_canonical:return {'trusted':False,'experimental':False,'canonical':False,'reason':'IncompleteNewBuildProvenance'}
 pins=receipt.get('source_and_reference_pins');need(type(pins)is dict,'Build source pin map required')
 resolution_pin=receipt['source_resolution_pin'];manifest=receipt.get('source_manifest');canonical=receipt['canonical_source_manifest']
 for descriptor,label in ((resolution_pin,'resolution'),(manifest,'manifest'),(canonical,'canonical manifest')):
  need(type(descriptor)is dict and set(descriptor)=={'path','sha256','bytes'} and type(descriptor['path'])is str and type(descriptor['sha256'])is str and type(descriptor['bytes'])is int and Path(descriptor['path']).is_absolute(),'Exact build '+label+' descriptor required')
 package_root=receipt.get('package_root');need(type(package_root)is str and Path(package_root).is_absolute()and Path(package_root).is_dir(),'Build package root required for canonical manifest selection')
 expected_canonical=canonical_source_manifest(Path(package_root),receipt.get('mission')).resolve(strict=True)
 need(Path(canonical['path']).resolve(strict=True)==expected_canonical,'Canonical manifest path differs from selected source lane')
 _source_provenance_pin(resolution_pin['path'],resolution_pin['sha256'],pins)
 need(Path(resolution_pin['path']).stat().st_size==resolution_pin['bytes'],'Source resolution byte count differs')
 _source_provenance_pin(manifest['path'],manifest['sha256'],pins)
 _source_provenance_pin(canonical['path'],canonical['sha256'],pins)
 need(Path(manifest['path']).stat().st_size==manifest['bytes']and Path(canonical['path']).stat().st_size==canonical['bytes'],'Manifest byte count differs')
 resolution,resolution_sha,resolution_size=_safe_source_json(Path(resolution_pin['path']))
 need(resolution_sha==resolution_pin['sha256']and resolution_size==resolution_pin['bytes'],'Source resolution descriptor differs')
 need(receipt.get('source_resolution')==resolution_pin['path'],'Source resolution path differs from bound descriptor')
 need(resolution.get('schema_version')==1 and resolution.get('kind')=='SkirmishSourceResolution'and resolution.get('status')=='Resolved'and resolution.get('unresolved')==[],'Resolved source resolution identity required')
 resolution_pins=resolution.get('pins');rows=resolution.get('resolved')
 need(type(resolution_pins)is dict and type(rows)is list,'Resolver source pins and rows required')
 _source_provenance_pin(manifest['path'],manifest['sha256'],resolution_pins)
 manifest_data,manifest_sha,manifest_size=_safe_source_json(Path(manifest['path']))
 need(manifest_sha==manifest['sha256']and manifest_size==manifest['bytes'],'Pinned source manifest descriptor differs')
 mission=receipt.get('mission');need(type(mission)is str,'Build mission identity required')
 expected_backend=owned.require('skirmish_source_profiles').runtime_backend(mission)
 need(receipt.get('runtime_backend')==expected_backend and resolution.get('runtime_backend')==expected_backend,'Build/resolution runtime backend differs from admitted mission profile')
 need(_same_source_json(resolution.get('profile'),manifest_data.get('profile')),'Resolved source profile differs from pinned manifest profile')
 expected_profile=({'runtime_backend':'TypedGroundS026V1','numeric_runtime':'InstalledMono','qualification_lineage':{'mission':'S026','seed':104755}}
  if expected_backend=='TypedGroundS026V1'else {'mission':'S002','seed':104731,'numeric_runtime':'InstalledMono'})
 need(_same_source_json(manifest_data.get('profile'),expected_profile),'Pinned manifest profile differs from resolver backend profile')
 s073_variant={'mission':'S073','seed':104729,'runtime_backend':'LegacyS073OwnerV1','scope':'LegacyStartupAndQueueOnly'}
 variants=manifest_data.get('source_profile_variants',[])
 if expected_backend=='LegacyS073OwnerV1':need(_same_source_json(variants,[s073_variant]),'Exact bounded S073 source profile variant required')
 else:need(_same_source_json(variants,[])or _same_source_json(variants,[s073_variant]),'Unsupported source profile variant')
 experimental=[];generator_outputs=[];generated_root=(Path(resolution_pin['path']).parent/'generated').resolve()
 for row in rows:
  need(type(row)is dict and row.get('kind')in('native_direct','finite_adapter','current_original'),'Known resolved source row kind required')
  proof=row.get('proof')
  if proof is None and row.get('kind')in('native_direct','finite_adapter'):continue
  need(type(proof)is list,'Resolved current source proof row required')
  if row.get('kind')=='current_original':need(proof,'Current original source proof required')
  for item in proof:
   if type(item)is not dict or 'generation_proof'not in item:continue
   detail=item['generation_proof'];need(type(detail)is dict,'Generator proof object required')
   if detail.get('experimental_only')is not True:continue
   generator=item.get('generator');generator_sha=item.get('generator_sha256')
   _source_provenance_pin(generator,generator_sha,pins,resolution_pins)
   source_pins=detail.get('source_pins');outputs=detail.get('outputs')
   need(type(source_pins)is dict and bool(source_pins)and type(outputs)is dict and bool(outputs),'Experimental source/output closure required')
   for source_path,source_sha in source_pins.items():_source_provenance_pin(source_path,source_sha,pins,resolution_pins)
   generated=row.get('generated');generated_sha=row.get('sha256');generated_path=row.get('path')
   need(type(generated)is str and Path(generated).name==generated and generated in outputs,'Named generated output required')
   need(type(outputs[generated])is str and outputs[generated]==generated_sha,'Generated output proof digest differs')
   need(type(generated_path)is str and Path(generated_path).is_absolute(),'Absolute generated output path required')
   actual_generated=Path(generated_path).resolve(strict=True)
   need(actual_generated.is_relative_to(generated_root),'Generated source escaped isolated output directory')
   _source_provenance_pin(generated_path,generated_sha,pins)
   generator_outputs.append((generator,generator_sha,generated,generated_sha))
   experimental.append({'generator':generator,'generator_sha256':generator_sha,'output':generated,'output_sha256':generated_sha})
 for row in rows:
  for item in row.get('proof',[]):
   detail=item.get('generation_proof')if type(item)is dict else None
   if type(detail)is not dict or detail.get('experimental_only')is not True:continue
   generator=item.get('generator');generator_sha=item.get('generator_sha256');outputs=detail.get('outputs')
   for output,output_sha in outputs.items():
    matches=[entry for entry in generator_outputs if entry==(generator,generator_sha,output,output_sha)]
    need(len(matches)==1,'Experimental generated output closure incomplete/ambiguous')
 canonical_match=manifest['sha256']==canonical['sha256']
 return {'trusted':True,'experimental':bool(experimental),'canonical':canonical_match,
         'reason':'ExperimentalGenerator'if experimental else 'CanonicalSourceManifest'if canonical_match else 'AlternateManifest',
         'runtime_configuration_mode':receipt.get('runtime_configuration_mode'),
         'manifest_sha256':manifest['sha256'],'canonical_manifest_sha256':canonical['sha256'],
         'resolution_pin':dict(resolution_pin),'experimental_proofs':experimental}
def setup_matrix_provenance(initial,data_repo):
 """Compare the exact consumed matrix bytes to this data checkout's packaged matrix."""
 try:
  descriptor=initial['setup_matrix']
  if type(descriptor)is not dict or set(descriptor)!={'path','sha256'}:raise ValueError('Exact setup matrix descriptor required')
  candidate=Path(descriptor['path']); packaged=Path(data_repo)/'Assets/Game/Resources/SkirmishExpansion/InitialSetupMatrix.csv'
  if not candidate.is_file() or not packaged.is_file() or candidate.stat().st_size>4*1024*1024 or packaged.stat().st_size>4*1024*1024:raise ValueError('Bounded setup matrices required')
  actual=sha(candidate.read_bytes()); canonical=sha(packaged.read_bytes())
  if actual!=descriptor['sha256']:raise ValueError('Consumed setup matrix hash differs from descriptor')
  return {'canonical':actual==canonical,'matrix_sha256':actual,'packaged_matrix_sha256':canonical}
 except (KeyError,TypeError,ValueError,OSError) as error:
  return {'canonical':False,'error':str(error)}

def stable_setup_matrix_provenance(before,after):
 """Qualification requires the same verified canonical bytes at both boundaries."""
 stable=(before.get('canonical') is True and after.get('canonical') is True and
         before.get('matrix_sha256')==after.get('matrix_sha256') and
         before.get('packaged_matrix_sha256')==after.get('packaged_matrix_sha256'))
 return {'canonical':stable,'before':before,'after':after}

def classify_experimental_source_run(initial,build_provenance,result,matrix_provenance=None):
 """Demote experiments and any unverified/legacy source build; preserve terminal observation."""
 qualification=(type(initial)is dict and ('metadata_compiler_context'in initial or 'configuration_qualification'in initial))
 layout_replay=(type(initial)is dict and 'layout_json_replay'in initial)
 resource_bindings=(type(initial)is dict and 'source_resource_bindings'in initial)
 expanded_diagnostic=(type(initial)is dict and 'source_expanded_diagnostic'in initial)
 external_matrix=matrix_provenance is not None and matrix_provenance.get('canonical') is not True
 external=qualification or layout_replay or resource_bindings or expanded_diagnostic or external_matrix
 if type(build_provenance)is not dict:build_provenance={'trusted':False,'experimental':False,'canonical':False,'reason':'MissingBuildProvenance'}
 demote=external or not build_provenance.get('trusted')or build_provenance.get('experimental')or not build_provenance.get('canonical')
 if not demote:return result
 result.update(diagnostic_only=True,validation_passed=False,camera_qualified=False,motion_reference_passed=False,
  active_mission_water_qualified=False,placement_qualified=False,runtime_presentation_qualified=False,
  source_fit_certified=False,native_acceptance=False,mission_ready=False,qualified_source_mission=False)
 if result.get('status')=='SourceOutcomeObserved':result['status']='DiagnosticSourceOutcomeObserved'
 result['source_provenance_classification']={'class':'ExternalConfigurationQualification'if qualification else 'ExternalLayoutJsonReplay'if layout_replay else 'ExternalSourceResourceBindings'if resource_bindings else 'ExpandedSourceDiagnostic'if expanded_diagnostic else 'ExternalOrUnverifiedSetupMatrix'if external_matrix else build_provenance.get('reason','UntrustedBuildProvenance'),
  'trusted_build_provenance':build_provenance.get('trusted')is True,'external_configuration_qualification':qualification,'external_layout_json_replay':layout_replay,'external_source_resource_bindings':resource_bindings,'expanded_source_diagnostic':expanded_diagnostic,
  'canonical_manifest':build_provenance.get('canonical')is True,'experimental_generator_proof':build_provenance.get('experimental')is True,
  'proof_error':build_provenance.get('error'),'setup_matrix_provenance':matrix_provenance}
 result['scope']='Source-only experimental, legacy, or untrusted alternate build; mission validation and readiness are not established.'
 return result

def source_camera_policy_selection(inputs,initial,build_provenance,max_ticks,build_pin):
 """Admit a bounded S074 logical-delivery source diagnostic without camera observations."""
 policy=inputs.get('source_camera_policy')
 if policy is None:return None
 fields={'schema_version','kind','mission','mode','configuration_qualification'}
 need(type(policy)is dict and set(policy)==fields and type(policy.get('schema_version'))is int and policy['schema_version']==1 and policy.get('kind')=='SkirmishSourceCameraPolicy' and policy.get('mission')=='S074' and policy.get('mode')=='NoCameraLogicalDeliveryDiagnostic','Exact opt-in S074 no-camera policy required')
 need(inputs.get('mission')=='S074' and initial.get('mission_id')=='S074','No-camera source policy is S074-only')
 need(type(build_provenance)is dict and build_provenance.get('trusted')is True and build_provenance.get('experimental')is True and build_provenance.get('canonical')is False,'No-camera source policy requires trusted experimental build provenance')
 need(type(max_ticks)is int and 0<max_ticks<=72000,'No-camera source policy requires a bounded tick count')
 qualification=initial.get('configuration_qualification')
 need(type(qualification)is dict and _same_source_json(policy['configuration_qualification'],qualification),'No-camera source policy must bind the exact initialization configuration qualification')
 for name in ('mission_scene','camera_profile','camera_values','camera_motion'):need(name not in inputs,'No-camera policy rejects supplied camera input: '+name)
 need(type(build_provenance.get('experimental_proofs'))is list and bool(build_provenance['experimental_proofs']),'No-camera source policy requires validated experimental generator closure')
 return {'schema_version':1,'kind':'SkirmishSourceCameraPolicy','mission':'S074','mode':'NoCameraLogicalDeliveryDiagnostic','configuration_qualification':qualification,'source_build_receipt':dict(build_pin),'build_provenance':{'trusted':True,'experimental':True,'canonical':False,'reason':build_provenance.get('reason'),'manifest_sha256':build_provenance.get('manifest_sha256'),'resolution_pin':build_provenance.get('resolution_pin'),'experimental_proofs':build_provenance.get('experimental_proofs')}}

def s003_source_diagnostic_selection(receipt,inputs,initial,build_provenance,environ):
 """Pin an explicitly configured source-only selection or reject ignored overrides."""
 path=environ.get('WARLINE_SOURCE_DIAGNOSTIC_CONFIG')
 expected=environ.get('WARLINE_SOURCE_DIAGNOSTIC_CONFIG_SHA256')
 if not path and not expected:return None
 need(bool(path)and bool(expected),'Source diagnostic configuration and SHA256 must be supplied together')
 need(inputs.get('mission')=='S003'and initial.get('mission_id')=='S003','Source diagnostic selection is S003-only')
 need(isinstance(path,str)and Path(path).is_absolute()and not Path(path).is_symlink(),'Absolute regular diagnostic configuration required')
 need(isinstance(expected,str)and re.fullmatch(r'[0-9a-f]{64}',expected) is not None,'Exact lowercase diagnostic SHA256 required')
 config,digest,size=_safe_source_json(path,65536)
 need(str(Path(path).resolve(strict=True))==path and digest==expected,'Diagnostic configuration path or bytes changed')
 descriptor={'path':path,'sha256':digest,'bytes':size}
 manifest_desc=receipt.get('source_manifest');need(type(manifest_desc)is dict and isinstance(manifest_desc.get('path'),str),'Pinned source manifest required')
 manifest,manifest_pin=read_json_pinned(manifest_desc['path']);need(manifest_pin['sha256']==manifest_desc.get('sha256'),'Diagnostic build manifest changed')
 need('WARLINE_S003_SHARED_SELECTION'in manifest.get('compiler_defines',[]),'Build lacks source diagnostic selection support')
 rsp_desc=receipt.get('compiler_response');need(type(rsp_desc)is dict and isinstance(rsp_desc.get('path'),str),'Pinned compiler response required')
 rsp_path=Path(rsp_desc['path']);raw=rsp_path.read_bytes();need(sha(raw)==rsp_desc.get('sha256'),'Diagnostic compiler response changed')
 defines=[json.loads(line)for line in raw.decode().splitlines()if line.strip()and json.loads(line).startswith('-define:')]
 need(len(defines)==1 and 'WARLINE_S003_SHARED_SELECTION'in defines[0][len('-define:'):].split(','),'Compiled diagnostic feature flag required')
 need(type(build_provenance)is dict and build_provenance.get('trusted')is True and (build_provenance.get('experimental')is True or build_provenance.get('canonical')is not True),'Diagnostic selection requires a trusted experimental or alternate build')
 fields={'schema_version','kind','mission_id','seed','size_id','selected_difficulty','capture_difficulty','capture_qualification','native_parity','runtime_admission','mission_ready'}
 sel=initial.get('selection',{})
 need(type(sel)is dict and type(initial.get('configuration_qualification'))is dict and type(config)is dict and set(config)==fields and type(config.get('schema_version'))is int and config['schema_version']==1 and config.get('kind')=='S003SourceDiagnosticSelection' and config.get('mission_id')=='S003' and type(config.get('seed'))is int and config['seed']==sel.get('seed') and type(config.get('size_id'))is int and config['size_id']==sel.get('size_id') and sel.get('difficulty_id')==2 and type(config.get('selected_difficulty'))is str and config['selected_difficulty']in {'Recruit','Veteran','Commander'} and config.get('capture_difficulty')=='Regular' and config.get('capture_qualification')==initial.get('configuration_qualification') and all(config.get(k)is False for k in ('native_parity','runtime_admission','mission_ready')),'Exact selected difficulty and Regular capture identity required')
 return {'path':descriptor['path'],'sha256':descriptor['sha256'],'bytes':descriptor['bytes'],'selected_difficulty':config['selected_difficulty'],'capture_difficulty':'Regular'}
def write(path,value):
 p=Path(path);temporary=p.with_name(p.name+'.tmp');temporary.write_text(json.dumps(value,indent=2)+'\n');os.replace(temporary,p)
def runtime_paths(repo):
 need(platform.system()=='Darwin','Only the qualified installed macOS Mono numeric lane is currently supported')
 version=re.search(r'm_EditorVersion:\s*(\S+)',(repo/'ProjectSettings/ProjectVersion.txt').read_text()).group(1)
 contents=Path('/Applications/Unity/Hub/Editor')/version/'Unity.app/Contents';script=contents/'Resources/Scripting';engine=script/'Managed/UnityEngine';mono=script/'MonoBleedingEdge/bin/mono'
 dot=Path(shutil.which('dotnet')or'').resolve(strict=True);sdks=list((dot.parent/'sdk').glob('*/Roslyn/bincore/csc.dll'));need(bool(sdks),'Installed Roslyn compiler required')
 def key(p):return tuple(int(x)for x in p.parents[2].name.split('.')if x.isdigit())
 csc=sorted(sdks,key=key)[-1];newtons=list((repo/'Library/PackageCache').glob('com.unity.nuget.newtonsoft-json@*/Runtime/Newtonsoft.Json.dll'));need(len(newtons)==1,'One resolved current Newtonsoft assembly required')
 refs=[repo/'Library/ScriptAssemblies/Unity.Addressables.dll',repo/'Library/ScriptAssemblies/Unity.ResourceManager.dll',script/'NetStandard/ref/2.1.0/netstandard.dll',script/'NetStandard/compat/2.1.0/shims/netfx/mscorlib.dll',engine/'UnityEngine.MathematicsModule.dll',engine/'UnityEngine.SharedInternalsModule.dll',newtons[0]]
 core=engine/'UnityEngine.CoreModule.dll'
 for p in refs+[core,mono,dot,csc]:need(p.is_file(),'Required installed build/runtime file missing: '+str(p))
 return {'contents':contents,'script':script,'engine':engine,'mono':mono,'dotnet':dot,'csc':csc,'refs':refs,'core':core,'newtonsoft':newtons[0]}
def _generate_navigation_identity(tool,inputs,output,pins):
 # Execute exactly the source bytes hashed/pinned here; never import cached .pyc.
 tool=Path(tool).resolve(strict=True);raw=tool.read_bytes();digest=sha(raw)
 merge_pins(pins,{str(tool):digest});need(tool.read_bytes()==raw,'Identity generator changed before execution')
 module=types.ModuleType('current_navigation_facts_identity');module.__file__=str(tool)
 exec(compile(raw,str(tool),'exec'),module.__dict__)
 need(tool.read_bytes()==raw,'Identity generator changed during execution')
 result=module.generate(inputs,output)
 need(tool.read_bytes()==raw,'Identity generator changed during generation');verify(pins)
 return result
def canonical_source_manifest(package,mission):
 backend=owned.require('skirmish_source_profiles').runtime_backend(mission)
 # S003 is admitted only by its explicit, reviewed source-only qualification.
 # Keep its dedicated manifest identity even though it shares the typed-ground backend.
 name='source_manifest.s003.plan.json'if mission=='S003'else'source_manifest.s003derived.plan.json'if owned.require('skirmish_source_profiles').uses_s003_derived_factory(mission)else'source_manifest.s026.plan.json'if backend=='TypedGroundS026V1'else'source_manifest.plan.json'
 return Path(package)/'Tools/CI/SkirmishSourceSimulation'/name

def build(repo,package,manifest,output,mission='S002',seed=104731):
 repo=Path(repo).resolve(strict=True);package=Path(package).resolve(strict=True);out=Path(output).absolute();need(not out.exists(),'Fresh isolated build output required');out.mkdir(parents=True)
 build_start=time.monotonic()
 result=resolve_manifest(repo,package,manifest,out/'source',mission,seed)
 need(not result['unresolved'],'Unmaterialized source recipes: '+', '.join(x['id']for x in result['unresolved']))
 resolved_at=time.monotonic()
 tools=runtime_paths(repo);placement_root=package/'Tools/CI/SkirmishSourceSimulation/Placement';module=owned.load_placement(placement_root);placement=module.build(repo,tools['engine'],out/'placement')
 placement_at=time.monotonic()
 pins=dict(result['pins']);merge_pins(pins,owned.pins());verify(pins);merge_pins(pins,{x['path']:x['sha256']for x in result['resolved']});merge_pins(pins,placement['source_and_reference_pins']);code=out/'SourceSimulation.exe';sources=[Path(x['path'])for x in result['resolved']]
 identity_tool=package/'Tools/CI/generate_navigation_facts_identity.py';identity_output=out/'source/generated/SourceNavigationFactsIdentity.cs';identity_inputs=out/'navigation-facts-sources.rsp'
 identity_inputs.write_text('\n'.join(json.dumps(str(x))for x in sources)+'\n')
 _generate_navigation_identity(identity_tool,identity_inputs,identity_output,pins);sources.append(identity_output)
 for identity_input in [identity_tool,identity_inputs,identity_output,identity_output.with_suffix('.json')]:
  identity_pin=pin(identity_input);merge_pins(pins,{identity_pin['path']:identity_pin['sha256']})
 mono_native=tools['mono'].parent.parent/'lib/libmono-native.dylib'
 runtime_support=[tools['script']/'MonoBleedingEdge/etc/mono/config',tools['contents']/'Frameworks/libMonoPosixHelper.dylib']
 if mono_native.is_file():runtime_support.append(mono_native)
 merge_file_pins(pins,sources+[tools['mono'],tools['dotnet'],tools['csc'],tools['core']]+tools['refs']+[Path(placement['dll']),Path(__file__)]+([repo/'Tools/CI/run_skirmish_source_simulation.py']if mission=='S049'else[])+[repo/'ProjectSettings/ProjectVersion.txt']+runtime_support)
 data,manifest_pin=read_json_pinned(manifest);merge_pins(pins,{manifest_pin['path']:manifest_pin['sha256']});canonical_manifest_pin=pin(canonical_source_manifest(package,mission));merge_pins(pins,{canonical_manifest_pin['path']:canonical_manifest_pin['sha256']});resolution_pin=pin(out/'source/resolution.json');merge_pins(pins,{resolution_pin['path']:resolution_pin['sha256']});defines=data.get('compiler_defines',[]);need(all(re.fullmatch(r'[A-Za-z_]\w*',x)and not x.startswith('UNITY_EDITOR')for x in defines),'Explicit non-Editor compiler profile required')
 args=['-nologo','-unsafe+','-nostdlib+','-langversion:latest','-target:exe','-main:SourceSimulationProgram','-out:'+str(code)]+(['-define:'+','.join(defines)]if defines else[])+['-r:'+str(p)for p in tools['refs']]+['-r:ActualCore='+str(tools['core']),'-r:'+placement['dll']]+[str(p)for p in sources]
 rsp=out/'compile.rsp';rsp.write_text('\n'.join(json.dumps(x)for x in args)+'\n');inputs_at=time.monotonic();verify(pins);start=time.monotonic();r=subprocess.run([str(tools['dotnet']),str(tools['csc']),'@'+str(rsp)],capture_output=True,text=True,timeout=120);compiler_at=time.monotonic();(out/'compile.log').write_text(r.stdout+r.stderr);verify(pins);verified_at=time.monotonic()
 receipt={'runtime_configuration_mode':owned.require('skirmish_source_profiles').runtime_configuration_mode(mission),'runtime_backend':result['runtime_backend'],'schema_version':1,'kind':'SkirmishSourceSimulationBuild','mission':mission,'seed':seed,'repository_root':str(repo),'package_root':str(package),'status':'Passed'if r.returncode==0 else'Failed','compile_exit':r.returncode,'compile_wall_seconds':time.monotonic()-start,'source_count':len(sources),'source_and_reference_pins':pins,'source_manifest':manifest_pin,'canonical_source_manifest':canonical_manifest_pin,'source_resolution':resolution_pin['path'],'source_resolution_pin':resolution_pin,'placement_build':str(out/'placement/build.json'),'compiler_response':pin(rsp),'runtime_selection':{k:str(tools[k])for k in ['contents','script','engine','mono','newtonsoft']},'numeric_runtime':'InstalledMono --optimize=-float32','native_or_player_launched':False,'scope':'Current source plus explicit finite engine/data adapters. No native UI/render, seed-only determinism or mission outcome claim from compilation.'}
 if mission=='S002' and owned.require('skirmish_source_profiles').derived() is not None:receipt['s002_derived_capability']='RuntimePinnedS002DerivedV1'
 if owned.require('skirmish_source_profiles').runtime_backend(mission)=='TypedGroundS026V1':
  readset=out/'source/compiled-transform-readset.json';sidecar=Path(str(code)+'.transform-readset.json');sidecar.write_bytes(readset.read_bytes());receipt['transform_readset']=pin(sidecar);merge_file_pins(pins,[readset,sidecar])
 if r.returncode==0:receipt['executable']=pin(code)
 receipt['build_stage_wall_seconds']={'source_resolution':resolved_at-build_start,'placement_build':placement_at-resolved_at,'input_preparation_and_pin_merges':inputs_at-placement_at,'pre_compile_verify':start-inputs_at,'compiler_process':compiler_at-start,'compile_log_and_post_verify':verified_at-compiler_at,'post_compile_finalization':time.monotonic()-verified_at,'total_before_receipt_write':time.monotonic()-build_start}
 write(out/'build.json',receipt);need(r.returncode==0,'Current source compilation failed; see '+str(out/'compile.log'));return receipt

def prepare_mono_config(tools,out):
 installed=tools['script']/'MonoBleedingEdge/etc/mono/config';raw=installed.read_bytes();text=raw.decode('utf-8');helper=tools['contents']/'Frameworks/libMonoPosixHelper.dylib';need(helper.is_file(),'Actual installed MonoPosixHelper required')
 text=text.replace('target="libMonoPosixHelper.dylib"','target="'+str(helper)+'"');text=text.replace('target="MonoPosixHelper"','target="'+str(helper)+'"');path=out/'mono-config.xml';path.write_text(text);return path,{str(installed):sha(raw),str(helper):sha(helper.read_bytes())}
def metadata_origin_pins(initial,repo,receipt):
 descriptor=initial.get('metadata_source_origin')
 if descriptor is None:return {}
 def exact_descriptor(value,maximum=1024*1024):
  need(isinstance(value,dict)and set(value)=={'path','sha256'}and all(isinstance(value[k],str)for k in value),'Exact native origin descriptor required')
  path=Path(value['path']);need(path.is_absolute()and path.is_file()and not path.is_symlink(),'Regular absolute origin descriptor required')
  need(0<path.stat().st_size<=maximum,'Bounded origin descriptor required');return path
 path=exact_descriptor(descriptor);origin_pin=pin(path);origin=json.loads(path.read_bytes(),object_pairs_hook=owned.require('skirmish_source_syntax').no_duplicates);need(origin_pin['sha256']==descriptor['sha256'],'Metadata origin descriptor changed')
 need(set(origin)=={'schema_version','kind','native_metadata','source_root','data_root','input_pins'}and type(origin['schema_version'])is int and origin['schema_version']==1 and origin['kind']=='QualifiedNativeMetadataOrigin','Exact native metadata origin schema required')
 need(origin['native_metadata']==initial.get('metadata_compatibility'),'Native origin artifact binding differs')
 native_path=exact_descriptor(origin['native_metadata'],64*1024*1024);native,native_pin=read_json_pinned(native_path);need(native_pin['sha256']==origin['native_metadata']['sha256'],'Native origin artifact changed')
 roots=[]
 for name in ['source_root','data_root']:
  root=Path(origin[name]);need(root.is_absolute()and root.is_dir(),'Explicit native origin root required')
  need(all(not x.is_symlink()for x in [root,*root.parents]),'Symlink origin root forbidden');roots.append(root.resolve(strict=True))
 pins={origin_pin['path']:origin_pin['sha256'],native_pin['path']:native_pin['sha256']};declared=origin['input_pins'];need(isinstance(declared,dict)and 68<=len(declared)<=8192,'Bounded origin file closure required')
 native_pins=native['source_pins']
 for name,expected in declared.items():
  f=Path(name);need(f.is_absolute()and f.is_file()and not f.is_symlink(),'Regular native origin input required');canonical=f.resolve(strict=True);matches=[str(canonical.relative_to(root))for root in roots if canonical.is_relative_to(root)];need(matches and all(native_pins.get(rel)==expected for rel in matches),'Origin file differs from captured native pin');entry=pin(f);need(entry['sha256']==expected,'Native origin input changed');pins[entry['path']]=expected
 response=receipt['compiler_response'];response_path=Path(response['path']);need(pin(response_path)['sha256']==response['sha256'],'Actual compiler response changed');arguments=[json.loads(line)for line in response_path.read_text().splitlines()if line.strip()];reader_paths=[name for name in arguments if isinstance(name,str)and Path(name).is_absolute()and Path(name).name=='SourceMetadataCompatibility.cs'];need(len(reader_paths)==1,'One actual compiled metadata reader required');reader=pin(Path(reader_paths[0]));need(receipt['source_and_reference_pins'].get(reader['path'])==reader['sha256'],'Compiled metadata reader receipt pin differs');pins[reader['path']]=reader['sha256'];pins[str(response_path)]=response['sha256'];text=Path(reader_paths[0]).read_text()
 def literals(name):
  matches=re.findall(r'\b'+name+r'\s*=\s*\{([^}]+)\}',text);need(len(matches)==1,'One literal metadata dependency list required');body=matches[0];values=re.findall(r'"([^"\n]+)"',body);need(not re.sub(r'"[^"\n]+"|[\s,]','',body),'Only literal dependency paths admitted');return values
 factories=literals('FactorySources');owners=literals('OwnerDefinitions');need(len(factories)==27 and len(owners)==6,'Exact current metadata consumer domain required')
 expected_origin={}
 def consume(root,rel,expected):
  f=root/rel;entry=pin(f);need(entry['sha256']==expected,'Metadata dependency changed: '+str(f));pins[entry['path']]=expected
 for suffix in factories+owners:
  rel='Assets/Game/Scripts/'+suffix
  for item in [rel,rel+'.meta']:
   expected=native_pins[item];expected_origin[str(roots[0]/item)]=expected
   current_expected=expected
   if item=='Assets/Game/Scripts/Composition/Game.Composition.asmdef':
    current=pin(repo/item)['sha256'];need(current==expected or(current=='8f41d8cd74821639e2628fb77ad32fdd89db88111af3af800ebb16d109753cfe'and expected=='b6db33e0e266aea3c4f63a70677b92a70188f101ad149b91560f897e466d3c6b'),'Unqualified current Composition packaging definition');current_expected=current
   consume(repo,item,current_expected)
 for rel in ['Packages/manifest.json','Packages/packages-lock.json']:
  expected_origin[str(roots[0]/rel)]=native_pins[rel];consume(repo,rel,native_pins[rel])
 for rel,expected in native_pins.items():
  if rel.startswith('Assets/')and not rel.startswith('Assets/Game/Scripts/')and not rel.endswith(('.cs','.cs.meta')):expected_origin[str(roots[1]/rel)]=expected
 need(declared==expected_origin,'Native origin consumed closure mismatch')
 return pins

def require_s026_inactive_diagnostic_contract(initial,inputs):
 placement=initial.get('placement_domain') if isinstance(initial,dict) else None
 diagnostic=placement.get('s026_inactive_diagnostic') if isinstance(placement,dict) else None
 if diagnostic is None:return None
 need(inputs.get('mission')=='S026' and set(diagnostic)=={'path','sha256','source_root','external_inputs'} and diagnostic['sha256']=='fa0d0dedd774ba7648ee2cedaa872f506339878bff45429abadf9585157e2710' and initial['navigation_reference']=={k:diagnostic[k] for k in ('path','sha256')} and inputs['static_clearance']==initial['navigation_reference'],'Exact S026 inactive capture identity required')
 origin=Path(diagnostic['source_root'])
 need(origin.is_absolute() and origin.is_dir() and all(not p.is_symlink() for p in [origin,*origin.parents]),'Canonical captured source root required')
 return diagnostic,origin

def require_s026_declared_candidate(initial,inputs):
 candidate=initial.get('s026_declared_pad_candidate')
 declared_metadata=initial.get('metadata_compatibility',{}).get('sha256')=='0c8c592b13e04ab49194dd076a287cbdd6db8ca486d06b8930907df97f2b6dbb'
 if candidate is None:
  need(not declared_metadata,'Declared metadata requires the exact diagnostic candidate')
  return None
 derived=initial.get('metadata_compiler_context') is not None and initial.get('configuration_qualification')==owned.require('skirmish_source_profiles').configuration_qualification() and initial.get('configuration_qualification') is not None
 native=declared_metadata and initial.get('mission_logical_binding',{}).get('sha256')=='d44197811ecac5ccc94357bf6fc974abdd3beb3c9eb3c73337f88c32e4018e70'
 need(type(candidate)is dict and set(candidate)=={'path','sha256'} and candidate['sha256']=='0a412bf51e718e3fba00d1a89f3699a9b8959c200933eb8d20c097fc0db86f61' and inputs.get('mission')=='S026' and initial.get('mission_id')=='S026' and initial.get('selection')=={'difficulty_id':2,'size_id':1,'seed':104755} and (native or derived) and require_s026_inactive_diagnostic_contract(initial,inputs) is not None,'Exact one-tuple diagnostic candidate and source context required')
 return candidate

def verify_s026_project_pin(root_path,relative,digest,descriptor):
 rel=Path(relative);need(not rel.is_absolute() and rel.parts and rel.parts[0] in ('Assets','Packages','ProjectSettings','Library') and all(x not in ('.','..') for x in rel.parts),'Canonical captured source-relative path required')
 path=root_path/rel;need(all(not q.is_symlink() for q in [path,*path.parents] if q!=root_path.parent),'Captured source symlink forbidden');descriptor({'path':str(path),'sha256':digest},512*1024*1024);return path

def pin_reference_template_runtime_inputs(initial,diagnostic,descriptor):
 if diagnostic is None:
  row=initial['reference_templates']
  for stem in ['source_reference','projection_manifest']:descriptor({'path':row[stem+'_path'],'sha256':row[stem+'_sha256']})

def diagnostic_descriptor_input_pins(initial,key,maximum):
 if type(initial)is not dict or key not in initial:return {}
 row=initial[key]
 need(type(row)is dict and set(row)=={'path','sha256'} and type(row['path'])is str and type(row['sha256'])is str and re.fullmatch('[0-9a-f]{64}',row['sha256']) is not None,'Exact '+key+' descriptor required')
 path=Path(row['path']);need(path.is_absolute() and path.is_file() and not path.is_symlink() and str(path.resolve(strict=True))==str(path) and 0<path.stat().st_size<=maximum,'Canonical bounded '+key+' input required')
 entry=pin(path);need(entry['sha256']==row['sha256'],key+' input hash differs')
 return {entry['path']:entry['sha256']}

def layout_replay_input_pins(initial):
 """Pin optional diagnostic replay bytes; presence never grants qualification."""
 return diagnostic_descriptor_input_pins(initial,'layout_json_replay',4*1024*1024)

def source_resource_binding_input_pins(initial):
 """Pin optional diagnostic source-resource configuration bytes."""
 return diagnostic_descriptor_input_pins(initial,'source_resource_bindings',1024*1024)

def _s074_origin_owner_setup_matches(initial,context):
 """Admit the retained origin replay only when its compiler inputs remain exact."""
 try:
  compiler=initial['metadata_compiler_context']
  if compiler['sha256']!='254fa7f2cf9bb76a0cf8e26e73f5cbcfd2118e06f14d5b56a84b0004f2911d6c':return False
  directory=Path(compiler['path']).resolve(strict=True).parent
  author,author_pin=read_json_pinned(directory/'authoring-request.json')
  result,result_pin=read_json_pinned(directory/'authoring-result.json')
  if result_pin['sha256']!='2230a1b0b3f199561ca75bcff7c960c25cef06b141faa4903e26c55332d72ef9':return False
  target=context['target_context'];matrix=author['setup_matrix'];pins=result['input_pins']
  if not isinstance(matrix,dict) or set(matrix)!={'path','sha256'} or initial.get('setup_matrix')!=matrix:return False
  if pin(Path(matrix['path']))['sha256']!=matrix['sha256']:return False
  if pins.get(author_pin['path'],{}).get('sha256')!=author_pin['sha256'] or pins.get(matrix['path'],{}).get('sha256')!=matrix['sha256']:return False
  return result.get('status')=='ConfigurationCompiled' and result.get('compiler_accepted') is True and result.get('world_allocated') is False and \
   all(result.get(k)==target.get(k) for k in ('catalog_id','seed','source_content_hash','source_fingerprint','layout_fingerprint')) and \
   result.get('resolved_setup',{}).get('setup_hash')==target.get('setup_hash')==1716123768
 except (OSError,ValueError,KeyError,TypeError,IndexError):return False

def _s074_candidate_owner_setup_matches(initial,context):
 """Match an exact independently compiled source candidate, never native readiness."""
 try:
  compiler=initial['metadata_compiler_context'];target=context['target_context']
  candidates={
   'c0f9d1e6c01467826463f9458cf92e8f8125ad7fd4e5bf66a208e0bf63095b91':(1815748444,'s074-six-metre-bound-author-20261114-r1','e0a7362cbd1999107e8b530976029a2e32cc8ba7ef4dff98d16b05c2f9c868c3','b411ac76e68ae4e9e3e82d299102826a3af948d727c366450c644a2e41e25cf6'),
   'd54c71cf26affe91826a7fdcf6c76d1f505f5b54cddb867869b16406c6b89cfb':(1559121027,'s074-eight-metre-bound-author-20261114-r1','8d5f84b409c71a4f11783d128c8a6f377482995fac885b7111b7a2f8528fb09e','c77947074d3504db73791afe2562f4e8419c1bc22c8b74256dff3a4bf957429c'),
   '1256a8ce7dc04b144997592f7e0d87d549b22087285bdb2d46e334513c0cc374':(3241215135,'s074-ten-metre-player-vehicle-authoring-20261114-r2','078b8bbd23b0de1f50d3a4570b57067521a7bc2a4a3592075a3e9c1ef0884d6b','11b356ed9b1e5ca49729a1ea98b614c0dd831ebeaa9fc2281b9b1c7ab0bc52e4')}
  selected=candidates.get(compiler['sha256'])
  if selected is None or target.get('setup_hash')!=selected[0]:return False
  setup_hash,directory,request_sha,result_sha=selected
  owner=Path(__file__).resolve().parents[2]/'T Logs/SkirmishProduction'/directory
  author,author_pin=read_json_pinned(owner/'authoring-request.json');result,result_pin=read_json_pinned(owner/'authoring-result.json')
  if author_pin['sha256']!=request_sha or result_pin['sha256']!=result_sha:return False
  matrix=author['setup_matrix'];pins=result['input_pins']
  return isinstance(matrix,dict) and set(matrix)=={'path','sha256'} and initial.get('setup_matrix')==matrix and pin(Path(matrix['path']))['sha256']==matrix['sha256'] and \
   pins.get(author_pin['path'],{}).get('sha256')==author_pin['sha256'] and pins.get(matrix['path'],{}).get('sha256')==matrix['sha256'] and \
   result.get('status')=='ConfigurationCompiled' and result.get('compiler_accepted') is True and result.get('world_allocated') is False and \
   all(result.get(k)==target.get(k) for k in ('catalog_id','seed','source_content_hash','source_fingerprint','layout_fingerprint')) and result.get('resolved_setup',{}).get('setup_hash')==setup_hash
 except (OSError,ValueError,KeyError,TypeError,IndexError):return False

def _s074_receipt_candidate_owner_matches(initial,context):
 """Check the compiler identity before the full source-owner receipt join."""
 try:
  refs=initial['s074_candidate_author'];target=context['target_context']
  if not isinstance(refs,dict) or set(refs)!={'request','result'}:return False
  author,ap=read_json_pinned(refs['request']['path']);result,rp=read_json_pinned(refs['result']['path'])
  if any(not isinstance(refs[k],dict) or set(refs[k])!={'path','sha256'} for k in ('request','result')):return False
  if ap['sha256']!=refs['request']['sha256'] or rp['sha256']!=refs['result']['sha256']:return False
  matrix=author['setup_matrix'];pins=result['input_pins']
  return author.get('kind')=='SkirmishMissionAuthoringPreflight' and author.get('catalog_id')=='S074' and author.get('seed')==104803 and \
   author.get('size')=='Standard' and author.get('difficulty')=='Regular' and author.get('graph')==initial.get('source_config_graph') and \
   author.get('layout')==initial.get('typed_mission_configuration',{}).get('layout') and author.get('setup_matrix')==initial.get('setup_matrix') and \
   pin(Path(matrix['path']))['sha256']==matrix['sha256'] and pins.get(ap['path'],{}).get('sha256')==ap['sha256'] and \
   pins.get(matrix['path'],{}).get('sha256')==matrix['sha256'] and result.get('status')=='ConfigurationCompiled' and \
   result.get('compiler_accepted') is True and all(result.get(k) is False for k in ('world_allocated','source_fit_verified','mission_ready','native_acceptance')) and \
   all(result.get(k)==target.get(k) for k in ('catalog_id','seed','source_content_hash','source_fingerprint','layout_fingerprint')) and \
   result.get('resolved_setup',{}).get('setup_hash')==target.get('setup_hash')
 except (OSError,ValueError,KeyError,TypeError,IndexError):return False

def _s074_qualified_external_context(initial):
 if not isinstance(initial,dict) or initial.get('mission_id')!='S074' or initial.get('source_epoch')!='OriginalCompilerDerivedNativeFactoryContext/v1':return False
 compiler=initial.get('metadata_compiler_context');qualification=initial.get('configuration_qualification')
 if not isinstance(compiler,dict) or set(compiler)!={'path','sha256'} or not isinstance(qualification,dict) or set(qualification)!={'path','sha256'}:return False
 try:
  context,context_pin=read_json_pinned(compiler['path']);qual,qual_pin=read_json_pinned(qualification['path'])
 except (OSError,ValueError,KeyError,TypeError):return False
 target=context.get('target_context',{})
 return context_pin['sha256']==compiler['sha256'] and qual_pin['sha256']==qualification['sha256'] and \
  context.get('kind')=='OriginalCompilerDerivedNativeFactoryContext' and context.get('authority')=='ActualSourceCompilerContext_ImmutableNativeFactoryFacts_NotNativeS050' and \
  target.get('catalog_id')=='S074' and target.get('seed')==104803 and (target.get('setup_hash')==1298808707 or _s074_origin_owner_setup_matches(initial,context) or _s074_candidate_owner_setup_matches(initial,context) or _s074_receipt_candidate_owner_matches(initial,context)) and target.get('operation_map_id')=='opmap.skirmish.industrial_basin' and \
  qual=={'schema_version':1,'kind':'ExperimentalGroundConfigurationQualification','runtime_backend':'TypedGroundS026V1',
         'compiler_context':compiler,'scope':'OriginalCompilerAndQualifiedFactoryInputs_SourceOnly','native_context_qualified':False,'mission_ready':False}

def _s074_owner_setup_matrix_matches(initial):
 """Require the S074 matrix selected by the initialization to be the one compiled by its owner."""
 if not isinstance(initial,dict) or not isinstance(initial.get('metadata_compiler_context'),dict):return False
 compiler=initial['metadata_compiler_context']
 try:
  context,context_pin=read_json_pinned(compiler['path'])
  if context_pin['sha256']!=compiler['sha256']:return False
  target=context.get('target_context',{})
  if target.get('setup_hash')==1716123768:return _s074_origin_owner_setup_matches(initial,context)
  if target.get('setup_hash') in (1815748444,1559121027,3241215135):return _s074_candidate_owner_setup_matches(initial,context)
  if _s074_receipt_candidate_owner_matches(initial,context):return True
  if target.get('catalog_id')!='S074' or target.get('seed')!=104803 or target.get('setup_hash')!=1298808707:return False
  owner_root=Path(compiler['path']).resolve(strict=True).parents[2]
  author,author_pin=read_json_pinned(owner_root/'exporter/s074/authoring-request.json')
  receipt,receipt_pin=read_json_pinned(owner_root/'candidate/receipt.json')
  owner_matrix=author.get('setup_matrix')
  if not isinstance(owner_matrix,dict) or set(owner_matrix)!={'path','sha256'}:return False
  matrix_path=Path(owner_matrix['path']).resolve(strict=True)
  matrix_pin=pin(matrix_path)
  if matrix_pin['sha256']!=owner_matrix['sha256']:return False
  source_inputs=receipt.get('source_inputs_sha256')
  return receipt.get('status')=='PassedSourceOwnerCompile' and isinstance(source_inputs,dict) and \
   source_inputs.get(owner_matrix['path'])==owner_matrix['sha256'] and initial.get('setup_matrix')==owner_matrix
 except (OSError,ValueError,KeyError,TypeError,IndexError):return False

def _s074_no_camera_policy_matches(initial,inputs,policy,build_pin):
 if not _s074_qualified_external_context(initial) or not isinstance(inputs,dict) or not isinstance(policy,dict):return False
 if set(policy)!={'schema_version','kind','mission','mode','configuration_qualification','source_build_receipt','build_provenance'}:return False
 if policy.get('schema_version')!=1 or policy.get('kind')!='SkirmishSourceCameraPolicy' or policy.get('mission')!='S074' or policy.get('mode')!='NoCameraLogicalDeliveryDiagnostic':return False
 if policy.get('configuration_qualification')!=initial.get('configuration_qualification'):return False
 if not isinstance(build_pin,dict) or policy.get('source_build_receipt')!=build_pin:return False
 if any(key in inputs for key in ('mission_scene','camera_profile','camera_values','camera_motion')):return False
 provenance=policy.get('build_provenance')
 return isinstance(provenance,dict) and provenance.get('trusted') is True and provenance.get('experimental') is True and provenance.get('canonical') is False and bool(provenance.get('experimental_proofs'))

def _s074_verified_alias_join(initial,inputs,package,join_descriptor,build_pin,repo):
 if not _s074_qualified_external_context(initial) or not isinstance(inputs,dict) or not isinstance(package,dict):return False
 package_descriptor=inputs.get('source_derived_map')
 if not isinstance(package_descriptor,dict) or set(package_descriptor)!={'path','sha256'}:return False
 if not isinstance(join_descriptor,dict) or not isinstance(join_descriptor.get('path'),str) or not isinstance(join_descriptor.get('sha256'),str):return False
 logical=initial.get('static_map');physical=package.get('static_map');graph=initial.get('source_config_graph')
 if not isinstance(logical,dict) or not isinstance(physical,dict) or not isinstance(graph,dict):return False
 if logical.get('operation_map_id')!='opmap.skirmish.industrial_basin' or physical.get('operation_map_id')!='opmap.skirmish.desert_base_01':return False
 if any(logical.get(key)!=physical.get(key) for key in ('grid_config','surface_data')):return False
 try:
  join,join_pin=read_json_pinned(join_descriptor['path'])
 except (OSError,ValueError,KeyError,TypeError):return False
 result=join.get('result',{})
 root=Path(repo).resolve(strict=True)
 def relative_definition(row):
  descriptor=row.get('definition') if isinstance(row,dict) else None
  if not isinstance(descriptor,dict) or not isinstance(descriptor.get('path'),str):return None
  try:path=Path(descriptor['path']).resolve(strict=True)
  except (OSError,RuntimeError):return None
  return str(path.relative_to(root)) if path.is_relative_to(root) else None
 logical_path=relative_definition(logical);physical_path=relative_definition(physical)
 if logical_path is None or physical_path is None:return False
 return join_pin.get('sha256')==join_descriptor['sha256'] and join.get('kind')=='OriginalOperationMapContractSourceOnlyReceipt' and \
  join.get('status')=='Passed' and join.get('world_initialized') is False and join.get('mission_ready') is False and join.get('source_fit_certified') is False and \
  join.get('source_build',{}).get('path')==build_pin.get('path') and join.get('source_build',{}).get('sha256')==build_pin.get('sha256') and \
  join.get('source_package',{}).get('path')==package_descriptor.get('path') and join.get('source_package',{}).get('sha256')==package_descriptor.get('sha256') and \
  join.get('source_graph',{}).get('path')==graph.get('path') and join.get('source_graph',{}).get('sha256')==graph.get('sha256') and \
  join.get('logical_source',{}).get('path')==logical_path and join.get('logical_source',{}).get('sha256')==logical.get('definition',{}).get('sha256') and \
  join.get('physical_source',{}).get('path')==physical_path and join.get('physical_source',{}).get('sha256')==physical.get('definition',{}).get('sha256') and \
  result.get('status')=='Passed' and result.get('original_public_validator')=='OperationMapContractValidation.TryValidate' and \
  result.get('logical_map_id')==logical.get('operation_map_id') and result.get('physical_source_map_id')==physical.get('operation_map_id') and result.get('changed_binding_rejected') is True

def _s074_needs_original_owner_mask_capture(initial,diagnostic,alias_join_verified):
 return initial.get('metadata_compiler_context') is not None and diagnostic is None and not alias_join_verified

def _s074_needs_original_placement_inputs(diagnostic,alias_join_verified):
 return diagnostic is None and not alias_join_verified

def _source_derived_map_exact_selection(initial,package,preflight):
 """Admit a preflighted package only when it is the exact selected physical map."""
 if not isinstance(initial,dict) or not isinstance(package,dict) or not isinstance(preflight,dict):return False
 if package.get('kind')!='SourceDerivedCampaignMapPackage' or package.get('schema_version')!=1:return False
 if any(package.get(k) is not False for k in ('mission_initialized','mission_ready','source_fit_certified','live_occupancy_measured')):return False
 if preflight.get('kind')!='SourceDerivedCampaignMapPackage' or preflight.get('status')!='SourceOnlyPreflightPassed' or preflight.get('world_created') is not False:return False
 if any(preflight.get(k) is not False for k in ('mission_initialized','mission_ready','source_fit_certified','live_occupancy_measured','active_water_qualified')):return False
 static_map=initial.get('static_map')
 if not isinstance(static_map,dict) or package.get('static_map')!=static_map:return False
 selected=initial.get('selected_operation_map')
 if selected!=static_map.get('definition'):return False
 operation_map_id=static_map.get('operation_map_id')
 if not isinstance(operation_map_id,str) or package.get('static_map',{}).get('operation_map_id')!=operation_map_id or preflight.get('operation_map_id')!=operation_map_id:return False
 frame=package.get('frame')
 if not isinstance(frame,dict) or type(frame.get('width')) is not int or frame['width']<=0 or type(frame.get('height')) is not int or frame['height']<=0:return False
 origin=frame.get('origin')
 if not isinstance(origin,list) or len(origin)!=3 or any(type(v) not in (int,float) for v in origin) or any(not math.isfinite(v) for v in origin):return False
 if type(frame.get('cell_size')) not in (int,float) or not math.isfinite(frame['cell_size']) or frame['cell_size']<=0:return False
 if preflight.get('frame')!=frame:return False
 for key in ('capture','water_descriptor','clearance_descriptor'):
  row=package.get(key)
  if not isinstance(row,dict) or set(row)!={'path','sha256'} or not isinstance(row['path'],str) or not Path(row['path']).is_absolute() or not re.fullmatch(r'[0-9a-f]{64}',row['sha256']):return False
 return True

def s050_source_diagnostic_death_pins(initial,repo):
 """Validate and pin the bounded, explicitly non-admitted S050 death projection."""
 need(isinstance(initial,dict) and initial.get('mission_id')=='S050','S050 diagnostic initialization required')
 container=initial.get('death_mechanical_inputs')
 need(isinstance(container,dict) and set(container)=={'s050_source_diagnostic'},'Exact nested S050 death diagnostic descriptor required')
 diagnostic=container['s050_source_diagnostic']
 need(isinstance(diagnostic,dict) and set(diagnostic)=={'projection','receipt'},'Exact S050 projection/receipt pair required')
 projection_desc,receipt_desc=diagnostic['projection'],diagnostic['receipt']
 def bounded_pin(row,maximum):
  need(isinstance(row,dict) and set(row)=={'path','sha256'} and isinstance(row['path'],str) and isinstance(row['sha256'],str) and re.fullmatch(r'[0-9a-f]{64}',row['sha256']),'Exact S050 diagnostic file descriptor required')
  path=Path(row['path']);need(path.is_absolute() and not path.is_symlink() and path.is_file() and path.stat().st_size<=maximum,'Bounded absolute regular S050 diagnostic input required')
  actual=pin(path);need(actual['path']==row['path'] and actual['sha256']==row['sha256'],'S050 diagnostic input pin mismatch: '+str(path));return path
 projection_path=bounded_pin(projection_desc,1024*1024)
 receipt_path=bounded_pin(receipt_desc,1024*1024)
 try:
  receipt=json.loads(receipt_path.read_bytes())
  projector_path=Path(repo)/'Tools/CI/SkirmishSourceSimulation/Recipes/project_s050_death_mechanical_readset.py'
  spec=importlib.util.spec_from_file_location('_s050_death_projection_runtime_validator',projector_path)
  need(spec is not None and spec.loader is not None,'S050 source projection validator unavailable')
  validator=importlib.util.module_from_spec(spec);spec.loader.exec_module(validator)
  source_capture=receipt.get('source_capture',{});source_audit=receipt.get('source_audit',{});keys=receipt.get('prefab_key_set',{})
  capture_root=Path(receipt.get('capture_root',''))
  need(capture_root.is_absolute() and capture_root.resolve(strict=True)==Path(repo).resolve(strict=True),'S050 capture root differs from selected source repository')
  expected_projection,expected_receipt=validator.make_projection(source_capture.get('path'),source_audit.get('path'),keys.get('path'),capture_root)
  need(projection_path.read_bytes()==expected_projection,'S050 compact projection differs from current pinned original-owner source')
  expected_receipt['projection']=projection_desc
  need(receipt==expected_receipt,'S050 projection receipt differs from recomputed original-owner evidence')
  capture_path=Path(source_capture['path']);audit_path=Path(source_audit['path']);keys_path=Path(keys['path'])
  capture=json.loads(capture_path.read_bytes())
  log_path=Path(receipt['source_audit']['path'])
  audit=json.loads(audit_path.read_bytes());log_path=Path(audit['unity_log']['path'])
  log_pin=pin(log_path);need(log_pin['sha256']==audit['unity_log']['sha256'] and log_path.stat().st_size<=32*1024*1024,'Current pinned S050 Unity owner log required')
  need(capture_path.stat().st_size<=4*1024*1024 and audit_path.stat().st_size<=1024*1024 and keys_path.stat().st_size<=4096,'S050 source audit inputs exceed bounded diagnostic limits')
  pins={}
  for path in (projection_path,receipt_path,capture_path,audit_path,keys_path,log_path):
   entry=pin(path);pins[entry['path']]=entry['sha256']
  for name,digest in capture.get('source_pins',{}).items():
   path=Path(name) if Path(name).is_absolute() else capture_root.joinpath(*name.split('/'))
   entry=pin(path);need(entry['sha256']==digest,'S050 original-owner source pin changed: '+name);pins[entry['path']]=digest
  return pins
 except (OSError,KeyError,TypeError,json.JSONDecodeError,ValueError) as error:
  if isinstance(error,UnsupportedSourceShape):raise
  raise UnsupportedSourceShape('Invalid S050 source diagnostic death projection: '+str(error)) from error

def runtime_input_pins(initial,inputs,repo,receipt,data_repo,source_derived_map=None,source_derived_join=None,build_pin=None,source_camera_policy=None,source_derived_preflight=None):
 if owned.require('skirmish_source_profiles').runtime_backend(inputs['mission'])!='TypedGroundS026V1':return {}
 pins={}
 def descriptor(row,maximum=256*1024*1024):
  need(isinstance(row,dict)and isinstance(row.get('path'),str)and isinstance(row.get('sha256'),str),'Required S026 input descriptor')
  f=Path(row['path']);need(f.is_absolute()and f.is_file()and f.stat().st_size<=maximum,'Bounded absolute S026 input required');entry=pin(f);need(entry['sha256']==row['sha256'],'S026 input changed: '+str(f));pins[entry['path']]=entry['sha256'];return f
 runtime=inputs.get('runtime_data_inputs');need(isinstance(runtime,dict)and set(runtime)=={'schema_version','kind','data_root','data_files','source_files'}and type(runtime['schema_version'])is int and runtime['schema_version']==1 and runtime['kind']=='CurrentRoadSceneRuntimeInputs','Exact current road input contract required')
 need(Path(runtime['data_root']).is_absolute()and Path(runtime['data_root']).resolve(strict=True)==data_repo,'Road data root differs from actual selected runtime root')
 datafiles={'Assets/Game/Configs/Scene/Game_RoadBuild_Config.asset','Assets/Game/Configs/Scene/Game_RoadBuild_Config.asset.meta','Assets/Game/Configs/Scene/Game_RuntimeCitySpawner_Config.asset','Assets/Game/Configs/Scene/Game_RuntimeCitySpawner_Config.asset.meta'}
 sourcefiles={'Assets/Game/Scripts/Configs/Scene/RoadBuildSystemSceneConfigAsset.cs.meta','Assets/Game/Scripts/Configs/Scene/RuntimeCitySpawnerSystemSceneConfigAsset.cs.meta'}
 need(set(runtime['data_files'])==datafiles and set(runtime['source_files'])==sourcefiles,'Exact road runtime/current-source closure required')
 for root,rows in [(data_repo,runtime['data_files']),(repo,runtime['source_files'])]:
  for rel,digest in rows.items():descriptor({'path':str(root/rel),'sha256':digest},4*1024*1024)
 alias_join_verified=_s074_verified_alias_join(initial,inputs,source_derived_map,source_derived_join,build_pin,repo)
 exact_source_map=_source_derived_map_exact_selection(initial,source_derived_map,source_derived_preflight)
 no_camera_verified=_s074_no_camera_policy_matches(initial,inputs,source_camera_policy,build_pin)
 no_native_metadata_context=_s074_qualified_external_context(initial) and initial.get('metadata_compatibility') is None and initial.get('metadata_source_origin') is None
 if _s074_qualified_external_context(initial):need(_s074_owner_setup_matrix_matches(initial),'S074 setup matrix differs from exact owner-compiled authoring request')
 if initial.get('metadata_compiler_context',{}).get('sha256')=='254fa7f2cf9bb76a0cf8e26e73f5cbcfd2118e06f14d5b56a84b0004f2911d6c':
  owner_dir=Path(initial['metadata_compiler_context']['path']).parent
  descriptor({'path':str(owner_dir/'authoring-request.json'),'sha256':'cb72aae80c1edca2dbc2a0d1cd54f0427d22ab311660cd07c64211bb6f66ce63'})
  descriptor({'path':str(owner_dir/'authoring-result.json'),'sha256':'2230a1b0b3f199561ca75bcff7c960c25cef06b141faa4903e26c55332d72ef9'})
 if initial.get('metadata_compiler_context',{}).get('sha256')=='c0f9d1e6c01467826463f9458cf92e8f8125ad7fd4e5bf66a208e0bf63095b91':
  owner_dir=Path(repo)/'T Logs/SkirmishProduction/s074-six-metre-bound-author-20261114-r1'
  descriptor({'path':str(owner_dir/'authoring-request.json'),'sha256':'e0a7362cbd1999107e8b530976029a2e32cc8ba7ef4dff98d16b05c2f9c868c3'})
  descriptor({'path':str(owner_dir/'authoring-result.json'),'sha256':'b411ac76e68ae4e9e3e82d299102826a3af948d727c366450c644a2e41e25cf6'})
  admission_desc=inputs.get('s074_source_owner_admission');descriptor(admission_desc,4*1024*1024)
  validator=owned.load_s074_six_metre_validation(Path(repo)/'Tools/CI')
  owner_pins=validator.validate_s074_six_metre_owner_inputs(initial,inputs,repo)
  merge_pins(pins,owner_pins)
 if initial.get('metadata_compiler_context',{}).get('sha256')=='d54c71cf26affe91826a7fdcf6c76d1f505f5b54cddb867869b16406c6b89cfb':
  owner_dir=Path(repo)/'T Logs/SkirmishProduction/s074-eight-metre-bound-author-20261114-r1'
  descriptor({'path':str(owner_dir/'authoring-request.json'),'sha256':'8d5f84b409c71a4f11783d128c8a6f377482995fac885b7111b7a2f8528fb09e'})
  descriptor({'path':str(owner_dir/'authoring-result.json'),'sha256':'c77947074d3504db73791afe2562f4e8419c1bc22c8b74256dff3a4bf957429c'})
  descriptor(inputs.get('s074_source_owner_admission'),4*1024*1024)
  validator=owned.load_s074_eight_metre_validation(Path(repo)/'Tools/CI')
  owner_pins=validator.validate_s074_eight_metre_owner_inputs(initial,inputs,repo)
  merge_pins(pins,owner_pins)
 if initial.get('metadata_compiler_context',{}).get('sha256')=='1256a8ce7dc04b144997592f7e0d87d549b22087285bdb2d46e334513c0cc374':
  owner_dir=Path(repo)/'T Logs/SkirmishProduction/s074-ten-metre-player-vehicle-authoring-20261114-r2'
  descriptor({'path':str(owner_dir/'authoring-request.json'),'sha256':'078b8bbd23b0de1f50d3a4570b57067521a7bc2a4a3592075a3e9c1ef0884d6b'})
  descriptor({'path':str(owner_dir/'authoring-result.json'),'sha256':'11b356ed9b1e5ca49729a1ea98b614c0dd831ebeaa9fc2281b9b1c7ab0bc52e4'})
  descriptor(inputs.get('s074_source_owner_admission'),4*1024*1024)
  validator=owned.load_s074_ten_metre_validation(Path(repo)/'Tools/CI')
  owner_pins=validator.validate_s074_ten_metre_owner_inputs(initial,inputs,repo)
  merge_pins(pins,owner_pins)
 if 's074_candidate_author' in initial:
  author_refs=initial['s074_candidate_author']
  need(isinstance(author_refs,dict) and set(author_refs)=={'request','result'},'Exact S074 candidate author descriptors required')
  for entry in author_refs.values():descriptor(entry,16*1024*1024)
  descriptor(inputs.get('s074_source_owner_admission'),4*1024*1024)
  need(isinstance(build_pin,dict) and set(('path','sha256')).issubset(build_pin),'Exact selected source build required')
  validator=owned.load_s074_source_owner_receipt_validation(Path(repo)/'Tools/CI')
  owner_pins=validator.validate_s074_source_owner_receipt_v1(initial,inputs,repo,{k:build_pin[k] for k in ('path','sha256')})
  merge_pins(pins,owner_pins)
 for key in ['source_config_graph','setup_matrix','canonical_battle_catalog','selected_operation_map','building_definitions','building_placement_config','reference_templates','navigation_reference','resource_preset','unit_production_metadata','building_request_metadata','metadata_compatibility','engineering_camera_profile','camera_engine_adapter','operation_map_rotation_reference','mission_logical_binding','death_mechanical_inputs','command_text_data']:
  if key=='navigation_reference' and (alias_join_verified or exact_source_map):continue
  if key=='navigation_reference':need(key in initial,'Verified source-map alias join or original navigation reference required')
  if key in ('engineering_camera_profile','camera_engine_adapter') and no_camera_verified:continue
  if key=='metadata_compatibility' and no_native_metadata_context:continue
  if inputs['mission']=='S003' and key in ('camera_engine_adapter','mission_logical_binding') and key not in initial:continue
  if key=='death_mechanical_inputs' and inputs['mission']=='S050':
   merge_pins(pins,s050_source_diagnostic_death_pins(initial,repo))
  else:descriptor(initial[key])
 s026_contract=require_s026_inactive_diagnostic_contract(initial,inputs)
 candidate=require_s026_declared_candidate(initial,inputs)
 if candidate is not None:descriptor(candidate,1024*1024)
 clearance=initial.get('s026_drop_clearance')
 if clearance is not None:
  need(candidate is not None and type(clearance)is dict and set(clearance)=={'path','sha256'} and clearance['sha256']=='1d6513b4e09435ad42b54a8ec29704e590451ad7777dde1449d0d716058b0deb','Exact inactive S026 source clearance projection required')
  descriptor(clearance,8*1024*1024)
 diagnostic,origin=s026_contract if s026_contract is not None else (None,None)
 if diagnostic is not None:
  physical,_=read_json_pinned(descriptor(diagnostic));need(physical.get('worldDisposed')is True and physical.get('sourceFitCertified')is False and physical.get('missionReady')is False and physical.get('missionInitialized')is False and physical.get('sourceSha256')=='68a631ca108e01c9a17839d2359e409226cdb009193f68ba910fa6f0f0a082b8' and physical.get('headlessPlacementInputs',{}).get('waterPolicyScope')=='OriginalInactiveStreamingFixture_NullNoOpFalse_NotPhysicalWaterClear','Exact inactive source authority required')
  ext=diagnostic['external_inputs'];root=physical['capturedProjectRoot'];expected={name:digest for name,digest in physical['readPins'].items() if Path(name).is_absolute() and not name.startswith(root+'/')};need(isinstance(ext,dict) and set(ext)==set(expected) and len(ext)==4,'Exact external capture authority closure required')
  for name,digest in expected.items():need(ext[name].get('sha256')==digest,'External capture pin differs');descriptor(ext[name],512*1024*1024)
  def project_pin(root_path,relative,digest):return verify_s026_project_pin(root_path,relative,digest,descriptor)
  for name,digest in physical['readPins'].items():
   if Path(name).is_absolute():
    if not name.startswith(root+'/'):continue
    relative=name[len(root)+1:]
   else:relative=name
   if relative.startswith('Library/ScriptAssemblies/') and relative.endswith('.dll'):continue # Historical captured producers, not live runtime inputs.
   project_pin(origin,relative,digest)
  def captured_source_pins(path,root_path):
   artifact,_=read_json_pinned(path);capture_pins=artifact.get('source_pins');need(isinstance(capture_pins,dict) and len(capture_pins)>=7755,'Complete native source pin set required');external=0;producer=None
   for name,digest in capture_pins.items():
    if Path(name).is_absolute():
     marker=next((x for x in ('/Assets/','/Packages/','/Library/PackageCache/') if x in name),None)
     if marker is None:
      need(name.endswith('/s026-unit-keys.json') and digest=='2e207fab1547fd55d56ddbb9919c73da4c942ccf2b85dd6d8619a5cf6bc481f7','Unexpected external prefab key origin');external+=1;continue
     at=name.index(marker);prefix=name[:at];need(producer is None or producer==prefix,'Capture project origin differs');producer=prefix;relative=name[at+1:]
    else:relative=name
    project_pin(root_path,relative,digest)
   need(external==1 and producer is not None,'Exact captured key/project origin required')
  captured_source_pins(initial['reference_templates']['path'],origin)
  captured_source_pins(initial['death_mechanical_inputs']['native_path'],Path(initial['death_mechanical_inputs']['capture_root']))
  reference=initial['reference_templates'];need(set(reference)=={'path','sha256'} and reference['sha256']=='3aa03b05a979e5c78bd51c01dc2b17f9bf8fff41d79828035a4d3eae3a580519','Exact diagnostic prefab capture required')
  death=initial['death_mechanical_inputs'];need(set(death)=={'path','sha256','native_path','native_sha256','capture_root'} and death['sha256']=='b8dfa566c3ca35a809abf9e716142bc78902128a5b3ca06c3f6440f129e95e98' and death['native_sha256']=='8a8a5bafd860d512b77500205b987233555209d35c5465a72f2a176ae3feb9ec','Exact diagnostic death capture required');descriptor({'path':death['native_path'],'sha256':death['native_sha256']})
 if initial.get('metadata_compiler_context')is not None:
  descriptor(initial['metadata_compiler_context']);descriptor(initial['configuration_qualification'])
  need(initial['configuration_qualification']==owned.require('skirmish_source_profiles').configuration_qualification(),'Explicit active configuration qualification differs')
  qualification,_=read_json_pinned(initial['configuration_qualification']['path'])
  refreshed=inputs['mission']=='S003' and qualification.get('kind') in ('S003RefreshedSourceOnlyQualificationEvidence','S003IndependentCurrentSourceEvidencePackage')
  need((initial['configuration_qualification']==initial['metadata_compiler_context']) if refreshed else (qualification.get('compiler_context')==initial['metadata_compiler_context']),'Qualified compiler descriptor differs')
 rotation,_=read_json_pinned(initial['operation_map_rotation_reference']['path'])
 if rotation.get('kind')=='SourceQualifiedOperationMapRotationReferenceSet':
  descriptor(rotation['base_reference']);descriptor(rotation['supplementary_reference'])
 for key in ['definition','grid_config','surface_data']:descriptor(initial['static_map'][key])
 if inputs['mission']=='S003':
  placement=initial['placement_domain'];need(placement.get('kind')=='S003PhysicalSnapshotDiagnostic' and placement.get('active_mission_water_qualified') is False,'Exact unqualified S003 physical snapshot required')
  for key in ['packed','geometry_reference','water_reference','original_reference']:descriptor(placement[key])
  packed,_=read_json_pinned(placement['packed']['path']);geometry,_=read_json_pinned(placement['geometry_reference']['path']);water,_=read_json_pinned(placement['water_reference']['path']);need(packed.get('operation_map_id')=='opmap.skirmish.desert_base_01' and packed.get('source_asset_sha256')==placement['source_bake_sha256'] and geometry.get('mesh_database_sha256')==placement['source_bake_sha256'] and water.get('mesh_database_sha256')==placement['source_bake_sha256'] and geometry.get('source_reference_sha256')==placement['original_reference']['sha256'],'S003 snapshot provenance mismatch')
  descriptor({'path':packed['source_asset_path'],'sha256':packed['source_asset_sha256']},64*1024*1024)
 elif _s074_needs_original_owner_mask_capture(initial,diagnostic,alias_join_verified) and not exact_source_map:
  descriptor(initial['placement_domain']['original_owner_masks'])
  fixed,_=read_json_pinned(initial['navigation_reference']['path']);need(fixed.get('kind')=='OriginalCapturedFixedMapCompilerContext'and fixed.get('metadata_compiler_context')==initial['metadata_compiler_context']and fixed.get('configuration_qualification')==initial['configuration_qualification'],'Explicit same compiler/fixed-map data binding required');native_path=descriptor(fixed['native_readback']);native,_=read_json_pinned(native_path);need(native.get('result')=='Collected'and native.get('worldDisposed')is True,'Actual disposed native source read required')
  masks,_=read_json_pinned(initial['placement_domain']['original_owner_masks']['path'])
  capture=initial['placement_domain']['captured_asset_origin'];need(set(capture)=={'source_root','source_definition_path','source_definition_sha256'}and capture['source_definition_sha256']==initial['static_map']['definition']['sha256'],'Explicit captured asset origin identity required');capture_root=Path(capture['source_root']);need(capture_root.is_absolute()and capture_root.is_dir()and all(not p.is_symlink()for p in[capture_root,*capture_root.parents]),'Canonical captured asset source root required')
  for root in [native,masks]:
   for path,digest in root['readPins'].items():
    if path.startswith('Assets/'):
     relative=Path(path);need(all(part not in {'','..','.'}for part in relative.parts),'Canonical captured asset relative path required');path=str(capture_root/relative)
    elif path.startswith('Packages/'):
     locator=root['packageAssetLocators'][path];need(locator['logicalPath']==path and locator['sha256']==digest,'Registered package locator differs');path=locator['resolvedPhysicalPath']
    if any(segment in path for segment in ['/Assets/','/Packages/','/Library/PackageCache/'])and not path.endswith(('.cs','.cs.meta','.asmdef','.asmdef.meta','.dll','.pdb')):descriptor({'path':path,'sha256':digest},512*1024*1024)
  camera,_=read_json_pinned(initial['engineering_camera_profile']['path']);descriptor(camera['native_function_profile'])
 elif exact_source_map or not _s074_needs_original_placement_inputs(diagnostic,alias_join_verified):
  pass # Exact source-only S026 capture validated above; active water remains unqualified.
 else:
  placement=initial.get('placement_domain')
  need(isinstance(placement,dict) and all(key in placement for key in ['packed','water_reference','geometry_reference']),'Original placement inputs required')
  for key in ['packed','water_reference','geometry_reference']:descriptor(placement[key])
 if inputs['mission']!='S003':descriptor(initial['typed_mission_configuration']['layout'])
 graph,_=read_json_pinned(initial['source_config_graph']['path'])
 if inputs['mission']=='S003':need(graph.get('authority')=='AuthoredConfigAssets' and graph.get('world_initialized') is False and graph.get('native_world_state_used') is False and isinstance(graph.get('source_pins'),dict) and graph['source_pins'],'Exact S003 authored config graph required')
 for f,digest in graph['source_pins'].items():descriptor({'path':f,'sha256':digest},4*1024*1024)
 pin_reference_template_runtime_inputs(initial,diagnostic,descriptor)
 need(initial['unit_production_metadata']==inputs['unit_metadata'],'S026 unit metadata descriptor differs')
 text,_=read_json_pinned(initial['command_text_data']['path']);source=inputs['command_text_sources'];need(set(source)=={'captured_root','current_root','input_pins','artifact_sha256'}and source['artifact_sha256']==initial['command_text_data']['sha256'],'Exact original text source manifest required')
 captured=Path(source['captured_root']);current=Path(source['current_root']);need(captured.is_absolute()and current.is_absolute()and current.is_dir(),'Explicit text source roots required')
 expected={str(current/Path(name).relative_to(captured)):digest for name,digest in text['source_pins'].items()};need(source['input_pins']==expected,'Original text source closure differs')
 for f,digest in expected.items():descriptor({'path':f,'sha256':digest})
 return pins

def classify_s002_source_diagnostic(initial,result):
 """Never promote finite S002 original-owner diagnostic inputs to validation."""
 placement=initial.get('placement_domain') if isinstance(initial,dict) else None
 reference=initial.get('reference_templates') if isinstance(initial,dict) else None
 focused_capture=initial.get('mission_id')=='S002' and isinstance(reference,dict) and reference.get('source_reference_sha256')==S002_FOCUSED_PREFAB_FAILED_CAPTURE_SHA
 diagnostic=initial.get('mission_id')=='S002' and (isinstance(placement,dict) and 's002_original_owner_diagnostic' in placement or focused_capture)
 if not diagnostic:return result
 result['diagnostic_only']=True
 result['validation_passed']=False
 result['camera_qualified']=False
 result['active_mission_water_qualified']=False
 result['runtime_presentation_qualified']=False
 result['source_fit_certified']=False
 result['native_acceptance']=False
 result['mission_ready']=False
 result['qualified_source_mission']=False
 if focused_capture:
  result['capture_validation_status']='Failed'
  result['capture_pass_marker_present']=False
  result['source_capture_validation']={'capture_validation_status':'Failed','capture_pass_marker_present':False,'capture_sha256':S002_FOCUSED_PREFAB_FAILED_CAPTURE_SHA,'unsupported_presentation_omission_count':14,'scope':'Existing importer PresentationOnly whitelist only; capture exporter validation failed'}
 if result.get('status')=='SourceOutcomeObserved':result['status']='DiagnosticSourceOutcomeObserved'
 result['scope']='Source-only S002 diagnostic outcome; capture validation, active water/presentation, source fit, native input and player acceptance remain unqualified.'
 return result

def classify_s026_inactive_diagnostic(initial,result):
 placement=initial.get('placement_domain') if isinstance(initial,dict) else None
 if initial.get('mission_id')!='S026' or not isinstance(placement,dict) or 's026_inactive_diagnostic' not in placement:return result
 result.update(diagnostic_only=True,validation_passed=False,active_mission_water_qualified=False,source_fit_certified=False,native_acceptance=False,mission_ready=False)
 if result.get('status')=='SourceOutcomeObserved':result['status']='DiagnosticSourceOutcomeObserved'
 result['scope']='S026 source-only inactive-water diagnostic; active water, source fit, native input and player acceptance remain unqualified.'
 return result

def classify_s003_source_diagnostic(initial,result):
 placement=initial.get('placement_domain') if isinstance(initial,dict) else None
 if initial.get('mission_id')!='S003' or not isinstance(placement,dict) or placement.get('kind')!='S003PhysicalSnapshotDiagnostic':return result
 result.update(diagnostic_only=True,validation_passed=False,camera_qualified=False,active_mission_water_qualified=False,placement_qualified=False,runtime_presentation_qualified=False,source_fit_certified=False,native_acceptance=False,mission_ready=False,native_aria_planner_parity_proven=False)
 if result.get('status')=='SourceOutcomeObserved':result['status']='DiagnosticSourceOutcomeObserved'
 result['scope']='S003 source-only physical and camera diagnostic; native ARIA planner parity, active water, placement, native input and player acceptance remain unqualified.'
 return result

def _s049_exact_descriptor(actual,path,digest):
 return isinstance(actual,dict) and actual.get('path')==path and actual.get('sha256')==digest

def _s049_expected_identity():
 return {
  'kind':S049_DIAGNOSTIC_CONTEXT_KIND,'mission':'S049','seed':104734,
  'data_bundle':{'path':S049_METADATA_ONLY_BUNDLE_PATH,'sha256':S049_METADATA_ONLY_BUNDLE_SHA},
  'initialization':{'path':S049_METADATA_ONLY_INITIALIZATION_PATH,'sha256':S049_METADATA_ONLY_INITIALIZATION_SHA},
  'metadata_epoch':{'path':S049_METADATA_ONLY_EPOCH_PATH,'sha256':S049_METADATA_ONLY_EPOCH_SHA},
  'configuration_qualification':{'path':S049_METADATA_ONLY_QUALIFICATION_PATH,'sha256':S049_METADATA_ONLY_QUALIFICATION_SHA},
  'fixed_map':{'path':S049_METADATA_ONLY_FIXED_MAP_PATH,'sha256':S049_METADATA_ONLY_FIXED_MAP_SHA},
  'native_readback_sha256':S049_METADATA_ONLY_NATIVE_READBACK_SHA,
  'source_definition_sha256':S049_METADATA_ONLY_SOURCE_DEFINITION_SHA,
  'camera_profile_sha256':S049_METADATA_ONLY_CAMERA_PROFILE_SHA,
  'camera_values_sha256':S049_METADATA_ONLY_CAMERA_VALUES_SHA,
  'camera_motion_sha256':S049_METADATA_ONLY_CAMERA_MOTION_SHA,
  'source_reference_sha256':S049_METADATA_ONLY_SOURCE_REFERENCE_SHA,
  'template_slice_sha256':S049_METADATA_ONLY_TEMPLATE_SLICE_SHA,
 }

def s049_projection17_diagnostic_identity(initial,inputs,bundle_pin):
 """Identify only the pinned Projection-17 metadata epoch with false runtime gates."""
 if not isinstance(initial,dict) or not isinstance(inputs,dict):return None
 if initial.get('mission_id')!='S049' or initial.get('source_epoch')!='OriginalCompilerDerivedNativeFactoryContext/v1':return None
 expected_profile={'identity_backend':'FreshLogicalEntitiesV1','system_initialization':'OriginalOnCreate','future_native_inputs':False}
 if initial.get('world_profile')!=expected_profile:return None
 expected=_s049_expected_identity()
 if inputs.get('mission')!='S049' or inputs.get('seed')!=104734:return None
 if not _s049_exact_descriptor(bundle_pin,expected['data_bundle']['path'],expected['data_bundle']['sha256']):return None
 if not _s049_exact_descriptor(inputs.get('initialization'),expected['initialization']['path'],expected['initialization']['sha256']):return None
 if not _s049_exact_descriptor(initial.get('metadata_compiler_context'),expected['metadata_epoch']['path'],expected['metadata_epoch']['sha256']):return None
 if not _s049_exact_descriptor(initial.get('configuration_qualification'),expected['configuration_qualification']['path'],expected['configuration_qualification']['sha256']):return None
 if not _s049_exact_descriptor(inputs.get('static_clearance'),expected['fixed_map']['path'],expected['fixed_map']['sha256']):return None
 placement=initial.get('placement_domain',{});origin=placement.get('captured_asset_origin',{})
 if not _s049_exact_descriptor(placement.get('original_owner_masks'),'/Volumes/T7/Projects/WarlineCapture/Logs/private-candidates/s049-physical-export-qualified-20261028-r1/project/Logs/SkirmishProduction/s049-armorbreak-placement-read-domain-20261028-r1.json',S049_METADATA_ONLY_NATIVE_READBACK_SHA):return None
 if origin.get('source_root')!='/Volumes/T7/Projects/WarlineCapture/Logs/private-candidates/s049-physical-export-qualified-20261028-r1/project' or origin.get('source_definition_path')!='Assets/Game/GeneratedOperationMaps/CH04M05ArmorBreak/Definition.asset' or origin.get('source_definition_sha256')!=S049_METADATA_ONLY_SOURCE_DEFINITION_SHA:return None
 if initial.get('static_map',{}).get('definition',{}).get('sha256')!=S049_METADATA_ONLY_SOURCE_DEFINITION_SHA:return None
 if initial.get('engineering_camera_profile',{}).get('sha256')!=S049_METADATA_ONLY_CAMERA_PROFILE_SHA:return None
 if inputs.get('camera_values',{}).get('sha256')!=S049_METADATA_ONLY_CAMERA_VALUES_SHA or inputs.get('camera_motion',{}).get('sha256')!=S049_METADATA_ONLY_CAMERA_MOTION_SHA:return None
 templates=initial.get('reference_templates',{})
 if templates.get('source_reference_sha256')!=S049_METADATA_ONLY_SOURCE_REFERENCE_SHA or templates.get('sha256')!=S049_METADATA_ONLY_TEMPLATE_SLICE_SHA:return None
 return expected

def _s049_preflight_matches(descriptor,identity):
 if not isinstance(descriptor,dict) or type(descriptor.get('path'))is not str or type(descriptor.get('sha256'))is not str:return None
 try:
  path=Path(descriptor['path']).resolve(strict=True);raw=path.read_bytes()
  if sha(raw)!=descriptor['sha256']:return None
  document=json.loads(raw)
  if document.get('schema_version')!=1 or document.get('kind')!=S049_DIAGNOSTIC_PREFLIGHT_KIND or document.get('mission')!='S049' or document.get('seed')!=104734 or document.get('identity')!=identity:return None
  if not _s049_exact_descriptor(document.get('data_bundle'),identity['data_bundle']['path'],identity['data_bundle']['sha256']):return None
  native=document.get('native_data');selected=native.get('selected_runtime_map_preflight')if isinstance(native,dict)else None
  if not isinstance(native,dict) or not isinstance(selected,dict):return None
  if native.get('status')!='Passed' or native.get('mission')!='S049' or native.get('compiler_hash')!=971644661 or native.get('world_allocated')is not False or native.get('entity_ids_allocated')!=0:return None
  if native.get('metadata_source_and_current_origins_checked')is not True or native.get('fresh_storage_authority_created_unclaimed')is not True:return None
  if selected.get('s049_physical_water_diagnostic')is not True or selected.get('active_mission_water_qualified')is not False or selected.get('source_fit_certified')is not False or selected.get('native_camera_context_qualified')is not False:return None
  if selected.get('actual_camera_profile_sha256')!=S049_METADATA_ONLY_CAMERA_PROFILE_SHA:return None
  scope=native.get('configuration_qualification_scope',{}).get('selected_context',{})
  if scope.get('catalog_id')!='S049' or scope.get('seed')!=104734 or scope.get('setup_hash')!=971644661:return None
  markers=native.get('s049_diagnostic_markers',{});water=markers.get('physical_water');camera=markers.get('camera');native_log=markers.get('native_preflight_log')
  if water!=S049_PHYSICAL_WATER_MARKER or camera!=S049_CAMERA_MARKER:return None
  if not isinstance(native_log,dict) or type(native_log.get('path'))is not str or type(native_log.get('sha256'))is not str:return None
  log_path=Path(native_log['path']).resolve(strict=True);log_raw=log_path.read_bytes()
  if sha(log_raw)!=native_log['sha256']:return None
  lines=log_raw.decode('utf-8',errors='replace').splitlines()
  if lines.count(S049_PHYSICAL_WATER_MARKER)!=1 or lines.count(S049_CAMERA_MARKER)!=1:return None
  prefix='[SourceRunPreflight] ';payload=[line[len(prefix):]for line in lines if line.startswith(prefix)]
  if len(payload)!=1:return None
  preflight=json.loads(payload[0]);runtime=preflight.get('selected_runtime_map_preflight',{})
  if preflight.get('status')!='Passed' or preflight.get('world_allocated')is not False or preflight.get('entity_ids_allocated')!=0:return None
  if runtime.get('s049_physical_water_diagnostic')is not True or runtime.get('active_mission_water_qualified')is not False or runtime.get('source_fit_certified')is not False or runtime.get('native_camera_context_qualified')is not False:return None
  process_desc=markers.get('native_preflight_process');request_desc=markers.get('native_preflight_request');pins_desc=markers.get('native_preflight_input_pins');build_desc=document.get('build_receipt')
  for item in (process_desc,request_desc,pins_desc,build_desc):
   if not isinstance(item,dict) or type(item.get('path'))is not str or type(item.get('sha256'))is not str:return None
   item_path=Path(item['path']).resolve(strict=True)
   if sha(item_path.read_bytes())!=item['sha256']:return None
  process=json.loads(Path(process_desc['path']).read_bytes());argv=process.get('argv')
  if process.get('process_exit')!=0 or process.get('timed_out')is not False or not isinstance(argv,list) or len(argv)<7 or argv[-2]!='--source-run-preflight' or argv[-1]!=request_desc['path']:return None
  request=json.loads(Path(request_desc['path']).read_bytes())
  if request.get('kind')!='SkirmishSourceSimulationRun' or request.get('mission')!='S049' or request.get('seed')!=104734 or request.get('source_repository_root')!='/Volumes/T7/Projects/WarlineCapture':return None
  if not _s049_exact_descriptor(request.get('initialization'),S049_METADATA_ONLY_INITIALIZATION_PATH,S049_METADATA_ONLY_INITIALIZATION_SHA) or not _s049_exact_descriptor(request.get('static_clearance'),S049_METADATA_ONLY_FIXED_MAP_PATH,S049_METADATA_ONLY_FIXED_MAP_SHA):return None
  build=json.loads(Path(build_desc['path']).read_bytes());
  if build.get('kind')!='SkirmishSourceSimulationBuild' or build.get('status')!='Passed' or build.get('mission')!='S049' or build.get('seed')!=104734 or argv[-3]!=build.get('executable',{}).get('path'):return None
  input_pins=json.loads(Path(pins_desc['path']).read_bytes())
  driver_paths=[Path('/Volumes/T7/Projects/WarlineCapture/Tools/CI/skirmish_source_build.py'),Path('/Volumes/T7/Projects/WarlineCapture/Tools/CI/run_skirmish_source_simulation.py')]
  for driver in driver_paths:
   resolved=driver.resolve(strict=True);driver_hash=sha(resolved.read_bytes());driver_path=str(resolved)
   if input_pins.get(driver_path)!=driver_hash or build.get('source_and_reference_pins',{}).get(driver_path)!=driver_hash:return None
  for item in [S049_METADATA_ONLY_INITIALIZATION_PATH,S049_METADATA_ONLY_FIXED_MAP_PATH]:
   expected_hash=S049_METADATA_ONLY_INITIALIZATION_SHA if item==S049_METADATA_ONLY_INITIALIZATION_PATH else S049_METADATA_ONLY_FIXED_MAP_SHA
   if input_pins.get(item)!=expected_hash:return None
  return {'preflight_evidence':{'path':str(path),'sha256':descriptor['sha256']},'native_preflight_log':{'path':str(log_path),'sha256':native_log['sha256']},'native_preflight_process':{'path':str(Path(process_desc['path']).resolve()),'sha256':process_desc['sha256']},'native_preflight_request':{'path':str(Path(request_desc['path']).resolve()),'sha256':request_desc['sha256']},'native_preflight_input_pins':{'path':str(Path(pins_desc['path']).resolve()),'sha256':pins_desc['sha256']},'build_receipt':{'path':str(Path(build_desc['path']).resolve()),'sha256':build_desc['sha256']},'physical_water_marker':water,'camera_marker':camera}
 except (OSError,ValueError,TypeError,KeyError,json.JSONDecodeError):return None

def validate_s049_diagnostic_receipt(receipt):
 """Reverify the immutable preflight and worker markers before batch classification."""
 if not isinstance(receipt,dict) or receipt.get('mission')!='S049':return False
 context=receipt.get('s049_diagnostic_context')
 if not isinstance(context,dict) or context.get('kind')!=S049_DIAGNOSTIC_CONTEXT_KIND:return False
 identity=context.get('identity');expected_bundle=identity.get('data_bundle')if isinstance(identity,dict)else None
 if not isinstance(expected_bundle,dict) or not _s049_exact_descriptor(receipt.get('data_bundle'),expected_bundle.get('path'),expected_bundle.get('sha256')):return False
 expected_values=s049_projection17_diagnostic_identity_from_receipt(receipt)
 if expected_values is None or identity!=expected_values:return False
 evidence=context.get('preflight_evidence');verified=_s049_preflight_matches(evidence,identity)
 if verified is None:return False
 for key in ('native_preflight_log','native_preflight_process','native_preflight_request','native_preflight_input_pins','build_receipt'):
  if context.get(key)!=verified[key]:return False
 if context.get('physical_water_marker')!=S049_PHYSICAL_WATER_MARKER or context.get('camera_marker')!=S049_CAMERA_MARKER:return False
 worker=context.get('worker_process_log')
 try:
  if not isinstance(worker,dict) or type(worker.get('path'))is not str or type(worker.get('sha256'))is not str:return False
  worker_path=Path(worker['path']).resolve(strict=True);worker_raw=worker_path.read_bytes()
  if sha(worker_raw)!=worker['sha256']:return False
  worker_lines=worker_raw.decode('utf-8',errors='replace').splitlines()
  if worker_lines.count(S049_PHYSICAL_WATER_MARKER)!=1 or worker_lines.count(S049_CAMERA_MARKER)!=1:return False
 except (OSError,ValueError,TypeError):return False
 pins=receipt.get('input_and_runtime_pins',{})
 for driver in (Path('/Volumes/T7/Projects/WarlineCapture/Tools/CI/skirmish_source_build.py'),Path('/Volumes/T7/Projects/WarlineCapture/Tools/CI/run_skirmish_source_simulation.py')):
  try:
   path=driver.resolve(strict=True);digest=sha(path.read_bytes())
   if pins.get(str(path))!=digest:return False
  except OSError:return False
 for key in ('preflight_evidence','native_preflight_log','native_preflight_process','native_preflight_request','native_preflight_input_pins','build_receipt'):
  if pins.get(verified[key]['path'])!=verified[key]['sha256']:return False
 flags=context.get('runner_flags')
 return isinstance(flags,dict) and flags.get('physical_water')is True and flags.get('camera')is True and context.get('evidence_valid')is True

def s049_projection17_diagnostic_identity_from_receipt(receipt):
 context=receipt.get('s049_diagnostic_context',{});identity=context.get('identity')if isinstance(context,dict)else None
 return identity if isinstance(identity,dict) and identity==_s049_expected_identity() else None

def classify_s049_source_diagnostic(initial,inputs,bundle_pin,result,preflight_evidence,worker_log_path):
 # A copied bundle or initialization must not escape the diagnostic gate merely
 # because its path changed. Noncanonical aliases fail identity below.
 metadata=initial.get('metadata_compiler_context') if isinstance(initial,dict) else None
 projection_path=bool(isinstance(inputs,dict) and inputs.get('mission')=='S049' and (
  isinstance(bundle_pin,dict) and (bundle_pin.get('path')==S049_METADATA_ONLY_BUNDLE_PATH or bundle_pin.get('sha256')==S049_METADATA_ONLY_BUNDLE_SHA)
  or isinstance(metadata,dict) and metadata.get('sha256')==S049_METADATA_ONLY_EPOCH_SHA))
 if not projection_path:return result
 identity=s049_projection17_diagnostic_identity(initial,inputs,bundle_pin)
 result.update(diagnostic_only=True,validation_passed=False,camera_qualified=False,motion_reference_passed=False,active_mission_water_qualified=False,placement_qualified=False,source_fit_certified=False,runtime_presentation_qualified=False,native_acceptance=False,mission_ready=False,qualified_source_mission=False)
 if identity is None:
  result.update(status='Failed',full_outcome_observed=False,diagnostic_prefix_completed=False)
  result['diagnostic_evidence_error']='Projection-17 S049 input identity did not match its pinned mission, epoch, fixed map, or camera context'
  return result
 preflight=_s049_preflight_matches(preflight_evidence,identity)
 try:worker_pin=pin(worker_log_path)
 except (OSError,ValueError):worker_pin=None
 worker_valid=False
 if worker_pin is not None:
  lines=Path(worker_pin['path']).read_text(errors='replace').splitlines()
  worker_valid=lines.count(S049_PHYSICAL_WATER_MARKER)==1 and lines.count(S049_CAMERA_MARKER)==1
 flags={'physical_water':os.environ.get('WARLINE_S049_PHYSICAL_WATER_DIAGNOSTIC')=='1','camera':os.environ.get('WARLINE_S049_CAMERA_DIAGNOSTIC')=='1'}
 evidence={'kind':S049_DIAGNOSTIC_CONTEXT_KIND,'identity':identity,'runner_flags':flags,'evidence_valid':bool(preflight and worker_valid and flags['physical_water'] and flags['camera']),'preflight_evidence':preflight['preflight_evidence']if preflight else {'path':preflight_evidence.get('path'),'sha256':preflight_evidence.get('sha256')}if isinstance(preflight_evidence,dict)else None,'native_preflight_log':preflight['native_preflight_log']if preflight else None,'native_preflight_process':preflight['native_preflight_process']if preflight else None,'native_preflight_request':preflight['native_preflight_request']if preflight else None,'native_preflight_input_pins':preflight['native_preflight_input_pins']if preflight else None,'build_receipt':preflight['build_receipt']if preflight else None,'physical_water_marker':preflight['physical_water_marker']if preflight else None,'camera_marker':preflight['camera_marker']if preflight else None,'worker_process_log':worker_pin}
 result['s049_diagnostic_context']=evidence
 if not evidence['evidence_valid']:
  result.update(status='Failed',full_outcome_observed=False,diagnostic_prefix_completed=False)
  result['diagnostic_evidence_error']='Exact consumed Projection-17 preflight/worker flags and markers are required'
  return result
 if result.get('status')=='SourceOutcomeObserved':result['status']='DiagnosticSourceOutcomeObserved'
 result['scope']='S049 source-only outcome from Projection-17 metadata-epoch diagnostic; active water, camera, source fit, player and device readiness remain unqualified.'
 return result

def _validate_s073_native_water_capture(repo,inputs,pins):
 selected=_select_s073_exact_revision(repo,inputs['native_water_capture_receipt']['path'],S073_WATER_CAPTURE_REVISIONS)
 artifact,revision_hashes=selected if selected is not None else (None,None)
 need(artifact is not None,'Only exact qualified S073 current-source water-capture revisions are admitted')
 current_r1=artifact.name=='s073-native-startup-playing-water-20261102-r1'
 current_r2=artifact.name=='s073-loaded-source-fit-20261102-r2'
 current_r3=artifact.name=='s073-native-playing-occupancy-20261102-r3-finalized-r2'
 expected_paths={
  'native_water_capture_receipt':artifact/'water-receipt.json',
  'native_water_capture_snapshot':Path('/private/tmp/s073-water-current-20261102-r1-playing.json') if current_r1 else artifact/'snapshot.json',
  'native_water_capture_mask':Path('/private/tmp/s073-water-current-20261102-r1-playing.water.bin') if current_r1 else artifact/'snapshot.water.bin',
 }
 for name,path in expected_paths.items():
  need(Path(inputs[name]['path'])==path.resolve(strict=True),'Only the qualified S073 native-water artifact path is admitted: '+name)
 receipt_path=expected_paths['native_water_capture_receipt'];snapshot_path=expected_paths['native_water_capture_snapshot'];mask_path=expected_paths['native_water_capture_mask'];log_path=artifact/'editor.log'
 water_receipt,receipt_pin=read_json_pinned(receipt_path);snapshot,snapshot_pin=read_json_pinned(snapshot_path);log_pin=pin(log_path);mask_pin=pin(mask_path)
 need(receipt_pin['sha256']==inputs['native_water_capture_receipt']['sha256'] and snapshot_pin['sha256']==inputs['native_water_capture_snapshot']['sha256'] and mask_pin['sha256']==inputs['native_water_capture_mask']['sha256'],'S073 native-water descriptor changed')
 need((receipt_pin['sha256'],snapshot_pin['sha256'],mask_pin['sha256'])==revision_hashes,'Exact qualified S073 native-water revision hashes required')
 need(water_receipt.get('kind')=='S073NativeWaterMaskCaptureReceipt' and water_receipt.get('schema_version')==1 and water_receipt.get('validation_status')=='Passed' and water_receipt.get('wrapper_exit_code')==0 and water_receipt.get('required_marker_present') is True and water_receipt.get('scope')=='Same-session original water-mask observation; footprint query count is separate and this receipt is not readiness evidence','Qualified checked-wrapper S073 water capture receipt required')
 marker_pattern=re.compile(rb'^\[S073NativeWaterMask\] result=Recorded cells=(\d+) waterCells=(\d+) path=(\S+)$')
 log=log_path.read_bytes();markers=[marker_pattern.fullmatch(line) for line in log.splitlines() if line.startswith(b'[S073NativeWaterMask]')]
 need(len(markers)==1 and markers[0] is not None and water_receipt.get('editor_log_sha256')==log_pin['sha256'],'Exact successful S073 water marker and editor log hash required')
 marker_cells,marker_water,marker_path=markers[0].groups()
 captured_snapshot=Path(marker_path.decode());captured_mask=Path(captured_snapshot.with_suffix('.water.bin'))
 need(captured_snapshot.is_absolute() and str(captured_snapshot).startswith('/private/tmp/') and captured_snapshot.suffix=='.json','S073 water marker must identify the finalized private-temp observation')
 if current_r2 or current_r3:need(water_receipt.get('original_observation_path')==str(captured_snapshot.resolve(strict=True)) and water_receipt.get('original_observation_sha256')==sha(captured_snapshot.read_bytes()),'Final water receipt must bind the exact original Editor observation marker path/hash')
 if current_r2:
  wrapper_exit=None;wrapper_pin=None
  need(water_receipt.get('wrapper_exit_code')==0 and water_receipt.get('required_marker_present') is True,'Final native water receipt must bind wrapper success and marker')
 else:
  wrapper_exit,wrapper_pin=read_json_pinned(artifact/'wrapper-exit.json');need(type(wrapper_exit.get('exit_code')) is int and wrapper_exit['exit_code']==0,'Actual S073 water wrapper exit record required')
 if current_r1 or current_r3:
  wrapper_provenance_path=artifact/'wrapper-exit-provenance.json';wrapper_provenance,wrapper_provenance_pin=read_json_pinned(wrapper_provenance_path)
  need(wrapper_provenance.get('kind')=='S073WrapperExitMaterializationReceipt' and wrapper_provenance.get('status')=='Passed' and wrapper_provenance.get('wrapper_exit_path')==str((artifact/'wrapper-exit.json').resolve()) and wrapper_provenance.get('wrapper_exit_sha256')==wrapper_pin['sha256'] and wrapper_provenance.get('wrapper_exit_code')==0 and wrapper_provenance.get('water_receipt_path')==str(receipt_path.resolve()) and wrapper_provenance.get('water_receipt_sha256')==receipt_pin['sha256'] and wrapper_provenance.get('editor_log_path')==str(log_path.resolve()) and wrapper_provenance.get('editor_log_sha256')==log_pin['sha256'],'Current S073 wrapper exit materialization must be receipt-bound')
  joint_receipt_path=artifact/'joint-receipt.json';joint_receipt,joint_receipt_pin=read_json_pinned(joint_receipt_path)
  need(wrapper_provenance.get('joint_receipt_path')==str(joint_receipt_path.resolve()) and wrapper_provenance.get('joint_receipt_sha256')==joint_receipt_pin['sha256'] and joint_receipt.get('wrapper_exit_code')==0,'Current S073 wrapper exit provenance must bind the passing joint receipt')
 expected_identity='2a2a791e8292a4f458bd603ceb86598aa0ba2ca82db41323bf5bf7c748ec6900';expected_content='2713962f0faa2dae49805e1b7e3a1673199a2cca915334d11421b354cd8f591c';map_id='opmap.skirmish.industrial_basin';provider='Game.Rendering.RenderBoundsPresentation+Reader'
 need(snapshot.get('kind')=='S073NativeWaterMaskObservation' and snapshot.get('schema_version')==1 and snapshot.get('scope')=='SameSessionOriginalSkirmishWaterPlacementCreateMask_NotReadinessOrMissionAcceptance' and snapshot.get('authority')=='OriginalSkirmishWaterPlacement.CreateMask_OriginalRenderBoundsGatewayProvider' and snapshot.get('scenario_index')==3 and snapshot.get('seed')==104729 and snapshot.get('phase')=='Playing' and re.fullmatch('[0-9a-f]{32}',snapshot.get('session_id','')) and snapshot.get('operation_map_id')==map_id and snapshot.get('source_identity_hash')==expected_identity and snapshot.get('source_content_hash')==expected_content and snapshot.get('physical_source_validated') is True,'Exact same-session S073 source water observation required')
 need(all(water_receipt.get(key)==snapshot.get(source_key) for key,source_key in [('session_id','session_id'),('world_name','world_name'),('world_sequence','world_sequence'),('operation_map_id','operation_map_id'),('source_identity_hash','source_identity_hash'),('source_content_hash','source_content_hash')]),'S073 water receipt and observation identity differ')
 grid=snapshot.get('grid');need(isinstance(grid,dict) and grid.get('width')==2048 and grid.get('height')==1024 and type(grid.get('cell_size'))in(int,float) and not isinstance(grid.get('cell_size'),bool) and grid.get('cell_size')==1 and grid.get('origin')==[0,0,0] and water_receipt.get('grid')==grid,'Exact S073 2048x1024 grid frame required')
 provider_type=snapshot.get('provider_type');provider_path=snapshot.get('provider_assembly_path');provider_hash=snapshot.get('provider_assembly_sha256')
 need(isinstance(provider_type,str) and provider_type.startswith(provider+',') and isinstance(provider_path,str) and Path(provider_path).is_absolute() and re.fullmatch('[0-9a-f]{64}',provider_hash or '') and water_receipt.get('provider_type')==provider_type and water_receipt.get('provider_assembly_path')==provider_path and water_receipt.get('provider_assembly_sha256')==provider_hash,'Original S073 RenderBoundsPresentation provider binding required')
 provider_file=Path(provider_path).resolve(strict=True);need(provider_file.is_file() and provider_file.is_relative_to(repo) and sha(provider_file.read_bytes())==provider_hash,'Captured S073 provider assembly differs from current checkout')
 mask=mask_path.read_bytes();mask_cells=2048*1024;expected_snapshot_mask_path=mask_path if current_r2 or current_r3 else captured_mask;need(Path(snapshot.get('mask_path','')).is_absolute() and Path(snapshot.get('mask_path','')).resolve(strict=True)==expected_snapshot_mask_path.resolve(strict=True) and snapshot.get('mask_encoding')=='uint8-bool01-row-major-xfast' and water_receipt.get('mask_encoding')=='uint8-bool01-row-major-xfast' and snapshot.get('mask_cells')==mask_cells and len(mask)==mask_cells and all(value in (0,1) for value in mask),'Complete S073 bool01 row-major mask required')
 mask_hash=sha(mask);water_cells=sum(mask)
 need(snapshot.get('mask_sha256')==mask_hash and snapshot.get('water_cells')==water_cells and water_receipt.get('mask_sha256')==mask_hash and water_receipt.get('mask_cells')==mask_cells and water_receipt.get('water_cells')==water_cells and int(marker_cells)==mask_cells and int(marker_water)==water_cells,'S073 water mask bytes, hashes and marker counts differ')
 footprint_count=snapshot.get('water_footprint_count');need(type(footprint_count)is int and 0<=footprint_count<=100000 and water_receipt.get('water_footprint_count')==footprint_count,'Separate bounded S073 water footprint query count required')
 before=snapshot.get('entity_order_version_before');after=snapshot.get('entity_order_version_after');need(type(before)is int and type(after)is int and before==after and water_receipt.get('entity_order_version')==before and snapshot.get('provider_reference_unchanged') is True,'S073 water capture changed ECS structure or provider reference')
 source_pins=snapshot.get('source_pins_sha256');need(isinstance(source_pins,dict) and source_pins==water_receipt.get('source_pins_sha256'),'Matching S073 water observation and receipt source pins required')
 expected_sources=S073_WATER_SOURCE_PIN_PATHS
 need(set(source_pins)==expected_sources,'Exact S073 water-capture source pin set required')
 capture_pins={}
 for relative,digest in source_pins.items():
  source=(repo/relative).resolve(strict=True);need(not Path(relative).is_absolute() and source.is_relative_to(repo) and re.fullmatch('[0-9a-f]{64}',digest or '') and sha(source.read_bytes())==digest,'S073 water capture source changed: '+relative);capture_pins[str(source)]=digest
 if current_r3:
  original=json.loads(captured_snapshot.read_text(encoding='utf-8'));passive=original.get('playing_passive_input_observation');normalized_passive=snapshot.get('playing_passive_input_observation');need(isinstance(passive,dict) and isinstance(normalized_passive,dict) and passive.get('observation_name')=='playing_passive_input_observation' and passive.get('phase')=='Playing' and passive.get('session_id')==snapshot.get('session_id') and passive.get('input_injected') is False and passive.get('gameplay_state_mutated') is False,'Exact passive same-session native blocker observation required')
  original_mask_path=Path(original.get('mask_path','')).resolve(strict=True);need(original_mask_path==captured_mask.resolve(strict=True) and sha(original_mask_path.read_bytes())==mask_hash,'Original marker mask path/hash differs from finalized native mask')
  original_raw=passive.get('raw_blocker_occupancy_snapshot') or {};normalized_raw=normalized_passive.get('raw_blocker_occupancy_snapshot') or {};need(original_raw.get('kind')=='S073BlockerOccupancyRawSnapshot' and original_raw.get('schema_version')==1 and original_raw.get('status')=='Captured' and original_raw.get('producer')=='native' and original_raw.get('observation')=='playing_passive_input_observation' and original_raw.get('native_session_id')==snapshot.get('session_id') and original_raw.get('source_map_id')==map_id and original_raw.get('source_campaign_map_id')=='opmap.skirmish.desert_base_01' and original_raw.get('source_identity_hash')==expected_identity and original_raw.get('source_content_hash')==expected_content and original_raw.get('native_source_pins_sha256')==source_pins and original_raw.get('source_host_state_transferred') is False and original_raw.get('campaign_state_transferred') is False and original_raw.get('readiness_claimed') is False,'Exact native blocker receipt identity and non-admission scope required')
  original_raw_path=Path(original_raw.get('raw_path','')).resolve(strict=True);normalized_raw_path=Path(normalized_raw.get('raw_path','')).resolve(strict=True);original_sidecar_path=Path(str(original_raw_path)+'.json');normalized_sidecar_path=Path(str(normalized_raw_path)+'.json');raw_capture=water_receipt.get('blocker_occupancy_raw_capture') or {};need(original_raw_path==captured_snapshot.with_suffix('.blocker-occupancy.bin').resolve(strict=True) and normalized_raw_path==(artifact/'blocker-occupancy.bin').resolve(strict=True) and normalized_raw_path==Path((raw_capture.get('raw') or {}).get('path','')).resolve(strict=True) and sha(original_raw_path.read_bytes())==original_raw.get('raw_sha256')==normalized_raw.get('raw_sha256') and sha(normalized_raw_path.read_bytes())==normalized_raw.get('raw_sha256') and original_raw_path.read_bytes()==normalized_raw_path.read_bytes(),'Original and finalized passive blocker raw payloads differ')
  original_sidecar=json.loads(original_sidecar_path.read_text(encoding='utf-8'));normalized_sidecar=json.loads(normalized_sidecar_path.read_text(encoding='utf-8'));need(original_sidecar==original_raw and normalized_sidecar==normalized_raw and raw_capture.get('observation')=='playing_passive_input_observation' and (raw_capture.get('raw') or {}).get('sha256')==normalized_raw.get('raw_sha256') and (raw_capture.get('receipt') or {}).get('sha256')==sha(normalized_sidecar_path.read_bytes()),'Original and finalized blocker sidecars are not exactly bound')
  raw=normalized_raw_path.read_bytes();need(len(raw)>=60 and raw[:8]==b'S073BOC1','Native blocker raw v1 schema magic required');width,height,cell_size,ox,oy,oz,epoch,applied_epoch,revision,cells=struct.unpack_from('<IIffffQQQI',raw,8);bit_bytes=(cells+7)//8;expected_len=60+bit_bytes+cells*4+cells+bit_bytes;need((width,height,cells)==(2048,1024,2048*1024) and cell_size==1 and [ox,oy,oz]==[0,0,0] and len(raw)==expected_len and epoch==applied_epoch and epoch>0 and revision>0 and normalized_raw.get('width')==width and normalized_raw.get('height')==height and normalized_raw.get('cell_size')==cell_size and normalized_raw.get('origin')==[ox,oy,oz] and normalized_raw.get('storage_epoch')==str(epoch) and normalized_raw.get('applied_storage_epoch')==str(applied_epoch) and normalized_raw.get('applied_revision')==str(revision),'Committed S073 blocker raw header/frame/epoch required')
  blocked=raw[60:60+bit_bytes];counts=struct.unpack_from('<%di'%cells,raw,60+bit_bytes);occupied_start=60+bit_bytes+cells*4+cells;occupied=raw[occupied_start:occupied_start+bit_bytes];need(all(c>=0 for c in counts) and all(((blocked[i>>3]>>(i&7))&1)==(c>0) for i,c in enumerate(counts)),'S073 blocker raw mask/count consistency required')
  if cells&7:need((blocked[-1]>>(cells&7))==0 and (occupied[-1]>>(cells&7))==0,'S073 blocker raw bitset padding must be zero')
  projection=json.loads(json.dumps(original));projection['mask_path']=snapshot.get('mask_path');projection['playing_passive_input_observation']['raw_blocker_occupancy_snapshot']['raw_path']=normalized_raw.get('raw_path');need(projection==snapshot,'Finalized S073 observation differs beyond exact mask/raw path normalization')
 false_fields=('campaign_gameplay_state_copied','mission_ready','source_fit_certified','normal_input_playthrough','player_readiness')
 need(all(snapshot.get(field) is False and water_receipt.get(field) is False for field in false_fields),'S073 water evidence must not claim gameplay readiness')
 expected_observation_hash=water_receipt.get('normalized_observation_sha256') if current_r2 else water_receipt.get('observation_sha256')
 need(expected_observation_hash==snapshot_pin['sha256'],'S073 water observation hash differs from qualified receipt')
 artifact_pins={receipt_pin['path']:receipt_pin['sha256'],snapshot_pin['path']:snapshot_pin['sha256'],mask_pin['path']:mask_pin['sha256'],log_pin['path']:log_pin['sha256'],str(provider_file):provider_hash}
 if wrapper_pin is not None:artifact_pins[wrapper_pin['path']]=wrapper_pin['sha256']
 if current_r1 or current_r3:
  artifact_pins[wrapper_provenance_pin['path']]=wrapper_provenance_pin['sha256'];artifact_pins[joint_receipt_pin['path']]=joint_receipt_pin['sha256']
 if current_r3:
  for evidence_path in (captured_snapshot,captured_mask,original_raw_path,original_sidecar_path,normalized_raw_path,normalized_sidecar_path):
   entry=pin(evidence_path);artifact_pins[entry['path']]=entry['sha256']
 merge_pins(pins,capture_pins);merge_pins(pins,artifact_pins)
 verify(pins)
 return {name:{'path':str(path.resolve(strict=True)),'sha256':pin(path)['sha256']} for name,path in expected_paths.items()}|{'native_water_capture_log':{'path':str(log_path.resolve(strict=True)),'sha256':log_pin['sha256']}}

def _validate_s073_native_water_startup_parity(repo,inputs,pins,water_capture):
 selected=_select_s073_exact_revision(repo,inputs['native_water_startup_parity_receipt']['path'],S073_WATER_PARITY_REVISIONS)
 artifact,revision_info=selected if selected is not None else (None,None)
 revision_hash,current_revision=revision_info if revision_info is not None else (None,False)
 need(artifact is not None,'Only exact qualified S073 joint startup/Playing water revisions are admitted')
 receipt_path=artifact/'joint-receipt.json'
 need(water_capture is not None,'S073 startup/Playing water parity requires a qualified native-water capture')
 need(current_revision or water_capture['native_water_capture_receipt']['path']==str((repo/'Logs/SkirmishProduction/s073-native-water-mask-20261101-r1/water-receipt.json').resolve(strict=True)),
      'Historical S073 joint water parity may only pair with its historical r1 capture')
 current_r1=artifact.name=='s073-native-startup-playing-water-20261102-r1'
 current_r2=artifact.name=='s073-loaded-source-fit-20261102-r2'
 current_r3=artifact.name=='s073-native-playing-occupancy-20261102-r3-finalized-r2'
 expected={
  'startup_observation':Path('/private/tmp/s073-water-current-20261102-r1-start.json') if current_r1 else artifact/('startup-snapshot.json' if current_revision else 'startup-snapshot.json'),
  'startup_mask':Path('/private/tmp/s073-water-current-20261102-r1-start.water.bin') if current_r1 else artifact/('startup-snapshot.water.bin' if current_revision else 'startup-mask.bin'),
  'playing_observation':Path('/private/tmp/s073-water-current-20261102-r1-playing.json') if current_r1 else artifact/('snapshot.json' if current_revision else 'playing-snapshot.json'),
  'playing_mask':Path('/private/tmp/s073-water-current-20261102-r1-playing.water.bin') if current_r1 else artifact/('snapshot.water.bin' if current_revision else 'playing-mask.bin'),
  'editor_log':artifact/'editor.log',
  'wrapper_exit':artifact/'wrapper-exit.json',
 }
 if current_r3:
  expected.update({'startup_observation':Path('/private/tmp/s073-native-playing-occupancy-20261102-r3-start.json'),'startup_mask':Path('/private/tmp/s073-native-playing-occupancy-20261102-r3-start.water.bin'),'playing_observation':Path('/private/tmp/s073-native-playing-occupancy-20261102-r3-playing.json'),'playing_mask':Path('/private/tmp/s073-native-playing-occupancy-20261102-r3-playing.water.bin')})
 if current_r2:
  expected.update({'startup_observation':Path('/private/tmp/s073-loaded-source-fit-20261102-r2-start.json'),'startup_mask':Path('/private/tmp/s073-loaded-source-fit-20261102-r2-start.water.bin'),'playing_observation':Path('/private/tmp/s073-loaded-source-fit-20261102-r2-playing.json'),'playing_mask':Path('/private/tmp/s073-loaded-source-fit-20261102-r2-playing.water.bin'),'wrapper_exit':None})
 verify(pins)
 receipt,receipt_pin=read_json_pinned(receipt_path)
 need(receipt_pin['sha256']==inputs['native_water_startup_parity_receipt']['sha256'],'S073 joint water receipt descriptor changed')
 need(receipt_pin['sha256']==revision_hash,'Exact qualified S073 joint startup/Playing water receipt hash required')
 observations={};observation_pins={};mask_bytes={};mask_pins={}
 for key in ('startup_observation','playing_observation'):
  observations[key],observation_pins[key]=read_json_pinned(expected[key])
 for key in ('startup_mask','playing_mask'):
  mask_pins[key]=pin(expected[key]);mask_bytes[key]=expected[key].read_bytes()
 log_pin=pin(expected['editor_log'])
 if expected['wrapper_exit'] is None:wrapper=None;wrapper_pin=None
 else:wrapper,wrapper_pin=read_json_pinned(expected['wrapper_exit'])
 startup=observations['startup_observation'];playing=observations['playing_observation']
 startup_capture=receipt.get('startup_capture');playing_capture=receipt.get('playing_capture')
 need(receipt.get('kind')=='S073JointNativeWaterMaskComparison' and receipt.get('schema_version')==1 and receipt.get('evidence_validation')=='Passed' and receipt.get('parity_status')=='Match' and receipt.get('masks_match') is True and receipt.get('differing_cells')==0 and receipt.get('wrapper_exit_code')==0 and receipt.get('required_markers_present') is True,'Exact successful S073 startup/Playing mask parity receipt required')
 if wrapper is not None:need(type(wrapper.get('exit_code')) is int and wrapper['exit_code']==0,'Joint S073 water wrapper exit must be zero')
 else:need(receipt.get('wrapper_exit_path') is None and receipt.get('wrapper_exit_code')==0 and receipt.get('required_markers_present') is True,'Final joint water receipt must bind wrapper success and markers without an external wrapper artifact')
 if current_r1 or current_r3:
  wrapper_provenance_path=artifact/'wrapper-exit-provenance.json';wrapper_provenance,wrapper_provenance_pin=read_json_pinned(wrapper_provenance_path)
 if current_r1 or current_r2 or current_r3:
  water_receipt_path=artifact/'water-receipt.json';water_receipt,water_receipt_pin=read_json_pinned(water_receipt_path)
 if current_r1 or current_r3:
  need(wrapper_provenance.get('kind')=='S073WrapperExitMaterializationReceipt' and wrapper_provenance.get('status')=='Passed' and wrapper_provenance.get('wrapper_exit_path')==str(expected['wrapper_exit'].resolve()) and wrapper_provenance.get('wrapper_exit_sha256')==wrapper_pin['sha256'] and wrapper_provenance.get('wrapper_exit_code')==0 and wrapper_provenance.get('joint_receipt_path')==str(receipt_path.resolve()) and wrapper_provenance.get('joint_receipt_sha256')==receipt_pin['sha256'] and wrapper_provenance.get('water_receipt_path')==str(water_receipt_path.resolve()) and wrapper_provenance.get('water_receipt_sha256')==water_receipt_pin['sha256'],'Current S073 wrapper exit must be bound to both strict water receipts')
 need(isinstance(startup_capture,dict) and isinstance(playing_capture,dict),'Joint S073 receipt capture descriptors required')
 need(startup.get('kind')=='S073NativeStartupWaterMaskObservation' and startup.get('schema_version')==1 and startup.get('scope')=='OriginalPreparingPhasePlacementCallback_CachedRoadOwnerWaterMask_NotReadinessOrMissionAcceptance' and startup.get('authority')=='S002PhysicalStartupObservation.OriginalPlacementFacts.Roads.GetWater' and startup.get('phase')=='Preparing' and startup.get('request_id') is None and startup.get('request_binding_available') is False and startup.get('request_binding_reason')=='OriginalPlacementFacts does not expose BuildingRuntimeSpawnRequest identity','Exact request-free S073 Preparing water observation required')
 need(playing.get('kind')=='S073NativeWaterMaskObservation' and playing.get('schema_version')==1 and playing.get('scope')=='SameSessionOriginalSkirmishWaterPlacementCreateMask_NotReadinessOrMissionAcceptance' and playing.get('authority')=='OriginalSkirmishWaterPlacement.CreateMask_OriginalRenderBoundsGatewayProvider' and playing.get('phase')=='Playing','Exact S073 Playing original-owner water observation required')
 for field in ('unity_version','scenario_index','seed','operation_map_id'):
  need(startup.get(field)==playing.get(field),'S073 startup/Playing capture identity differs: '+field)
 for field in ('world_name','world_sequence','session_id','source_identity_hash','source_content_hash'):
  need(startup.get(field)==playing.get(field)==receipt.get(field),'S073 joint receipt/capture identity differs: '+field)
 need(startup.get('scenario_index')==3 and startup.get('seed')==104729 and startup.get('operation_map_id')=='opmap.skirmish.industrial_basin' and startup.get('source_identity_hash')=='2a2a791e8292a4f458bd603ceb86598aa0ba2ca82db41323bf5bf7c748ec6900' and startup.get('source_content_hash')=='2713962f0faa2dae49805e1b7e3a1673199a2cca915334d11421b354cd8f591c' and startup.get('physical_source_validated') is True and playing.get('physical_source_validated') is True,'Exact validated S073 physical-source identity required')
 grid=startup.get('grid');need(isinstance(grid,dict) and grid==playing.get('grid')==receipt.get('grid') and grid.get('width')==2048 and grid.get('height')==1024 and grid.get('cell_size')==1 and grid.get('origin')==[0,0,0],'Exact shared S073 water grid frame required')
 provider='Game.Rendering.RenderBoundsPresentation+Reader';provider_path=startup.get('provider_assembly_path');provider_hash=startup.get('provider_assembly_sha256')
 need(isinstance(startup.get('provider_type'),str) and startup['provider_type'].startswith(provider+',') and startup.get('provider_type')==playing.get('provider_type') and provider_path==playing.get('provider_assembly_path') and provider_hash==playing.get('provider_assembly_sha256') and re.fullmatch('[0-9a-f]{64}',provider_hash or ''),'Same original S073 water provider binding required')
 provider_file=Path(provider_path).resolve(strict=True);need(provider_file.is_file() and provider_file.is_relative_to(repo) and sha(provider_file.read_bytes())==provider_hash,'S073 joint water provider assembly changed')
 capture_paths={
  'startup_observation':startup_capture.get('observation_path'),
  'startup_mask':startup_capture.get('mask_path'),
  'playing_observation':playing_capture.get('observation_path'),
  'playing_mask':playing_capture.get('mask_path'),
 }
 for key,raw_path in capture_paths.items():
  need(isinstance(raw_path,str) and Path(raw_path).is_absolute() and raw_path.startswith('/private/tmp/'),'Joint receipt must preserve each original private-temp capture path')
  expected_sha=(startup_capture if key.startswith('startup') else playing_capture).get('observation_sha256' if key.endswith('observation') else 'mask_sha256')
  need(re.fullmatch('[0-9a-f]{64}',expected_sha or ''),'Original S073 water capture hash descriptor required: '+key)
  durable=expected[key]
  durable_sha=observation_pins[key]['sha256'] if key.endswith('observation') else mask_pins[key]['sha256']
  need(durable_sha==expected_sha,'Durable S073 water copy differs from original capture: '+key)
  need(startup.get('mask_path')==startup_capture.get('mask_path') and playing.get('mask_path')==playing_capture.get('mask_path'),'Joint receipt mask path differs from original snapshot')
 need(startup_capture.get('observation_sha256')==observation_pins['startup_observation']['sha256'] and playing_capture.get('observation_sha256')==observation_pins['playing_observation']['sha256'],'S073 joint receipt observation hash differs from durable snapshots')
 mask_sha={key:sha(value) for key,value in mask_bytes.items()}
 need(all(len(mask_bytes[key])==2048*1024 and all(byte in (0,1) for byte in mask_bytes[key]) for key in mask_bytes),'Complete S073 uint8 bool01 startup and Playing masks required')
 need(mask_sha['startup_mask']==startup_capture.get('mask_sha256') and mask_sha['playing_mask']==playing_capture.get('mask_sha256') and mask_sha['startup_mask']==mask_sha['playing_mask'] and mask_bytes['startup_mask']==mask_bytes['playing_mask'],'S073 startup and Playing native masks must match byte-for-byte')
 need(mask_sha['playing_mask']==water_capture['native_water_capture_mask']['sha256'],'S073 Playing mask must equal the selected qualified native-water mask')
 selected_snapshot,_=read_json_pinned(inputs['native_water_capture_snapshot']['path'])
 selected_receipt,_=read_json_pinned(inputs['native_water_capture_receipt']['path'])
 need(selected_snapshot.get('mask_sha256')==mask_sha['playing_mask']==selected_receipt.get('mask_sha256'),'Joint startup/Playing mask differs from selected native-water reference')
 need(receipt.get('session_id')==startup.get('session_id') and receipt.get('world_name')==startup.get('world_name') and receipt.get('world_sequence')==startup.get('world_sequence') and receipt.get('source_identity_hash')==startup.get('source_identity_hash') and receipt.get('source_content_hash')==startup.get('source_content_hash'),'S073 joint receipt same-world/session source identity differs')
 log=expected['editor_log'].read_bytes()
 need(receipt.get('editor_log_path')==str(expected['editor_log'].resolve(strict=True)) and receipt.get('editor_log_sha256')==log_pin['sha256'],'S073 joint receipt editor log binding differs')
 startup_marker=re.compile(rb'^\[S073NativeStartupWaterMask\] result=Recorded frame=(\d+) cells=(\d+) waterCells=(\d+) path=(\S+)$')
 playing_marker=re.compile(rb'^\[S073NativeWaterMask\] result=Recorded cells=(\d+) waterCells=(\d+) path=(\S+)$')
 startup_rows=[startup_marker.fullmatch(line)for line in log.splitlines()if line.startswith(b'[S073NativeStartupWaterMask]')]
 playing_rows=[playing_marker.fullmatch(line)for line in log.splitlines()if line.startswith(b'[S073NativeWaterMask]')]
 need(len(startup_rows)==1 and startup_rows[0] is not None and len(playing_rows)==1 and playing_rows[0] is not None,'Exact S073 startup and Playing water markers required')
 frame,cells,water_cells,marker_path=startup_rows[0].groups();need(int(frame)==startup.get('frame') and int(cells)==2048*1024 and int(water_cells)==startup.get('water_cells') and marker_path.decode()==startup_capture.get('observation_path'),'Startup water marker must bind the original Preparing capture')
 cells,water_cells,marker_path=playing_rows[0].groups();need(int(cells)==2048*1024 and int(water_cells)==playing.get('water_cells') and marker_path.decode()==playing_capture.get('observation_path'),'Playing water marker must bind the original Playing capture')
 need(startup.get('water_cells')==playing.get('water_cells')==sum(mask_bytes['startup_mask'])==0 and receipt.get('differing_cells')==0,'S073 native water parity cell counts differ')
 need(startup.get('mask_encoding')=='uint8-bool01-row-major-xfast' and playing.get('mask_encoding')=='uint8-bool01-row-major-xfast' and startup.get('mask_cells')==playing.get('mask_cells')==2048*1024 and startup.get('mask_sha256')==playing.get('mask_sha256')==mask_sha['startup_mask'],'S073 joint mask encoding, count, or hash differs')
 false_fields=('campaign_gameplay_state_copied','source_fit_certified','mission_ready','normal_input_playthrough','player_readiness')
 need(all(startup.get(field) is False and playing.get(field) is False and receipt.get(field) is False for field in false_fields),'S073 joint water evidence cannot claim readiness or source fit')
 shared=receipt.get('shared_source_pins_sha256');startup_pins=startup.get('source_pins_sha256');playing_pins=playing.get('source_pins_sha256')
 need(isinstance(shared,dict) and isinstance(startup_pins,dict) and isinstance(playing_pins,dict),'S073 joint source pins required')
 expected_startup={'Assets/Game/Scripts/Editor/Skirmish/S073NativeStartupWaterMaskSnapshot.cs','Assets/Game/Scripts/Systems/S002PhysicalStartupObservation.cs','Assets/Game/Scripts/Systems/BuildingPlacementInvalidCellCacheCompositionSystemHelper.RoadValidation.cs','Assets/Game/Scripts/Systems/BuildingRuntimeProcessingCompositionSystemHelper.cs','Assets/Game/Scripts/Systems/BuildingPlacementAuthoredRoadCache.cs','Assets/Game/Scripts/Systems/BuildingPlacementAuthoredBuildingCache.cs','Assets/Game/Scripts/Systems/SkirmishWaterPlacement.cs','Assets/Game/Scripts/Rendering/RenderBoundsPresentation.cs','Assets/Game/Scripts/Rendering/Contracts/IRenderBoundsPresentation.cs','Assets/Game/Configs/Skirmish/IndustrialBasin/OperationMap_IndustrialBasin.asset','Assets/Game/Scenes/Match.unity','Assets/Game/GeneratedOperationMaps/RuntimeBinding/opmap.skirmish.desert_base_01/Candidates/opmap_skirmish_desert_base_01_dense_city_entity_scene_runtime.unity'}
 expected_playing={'Assets/Game/Scripts/Editor/Skirmish/S073NativeWaterMaskSnapshot.cs','Assets/Game/Scripts/Components/GridComponents.cs','Assets/Game/Scripts/Components/InitialUnitsSpawnComponents.cs','Assets/Game/Scripts/Components/OperationMapBuildingComponents.cs','Assets/Game/Scripts/Systems/StaticGridBlockerUpdateSystem.cs','Assets/Game/Scripts/Systems/InitialUnitsSpawnSystem.cs','Assets/Game/Scripts/Systems/DynamicOccupancyRebuildSystem.cs','Assets/Game/Scripts/Systems/GridOccupancyKernel.cs','Assets/Game/Scripts/Systems/SkirmishWaterPlacement.cs','Assets/Game/Scripts/Systems/BuildingPlacementAuthoredRoadCache.cs','Assets/Game/Scripts/Rendering/RenderBoundsPresentation.cs','Assets/Game/Scripts/Rendering/Contracts/IRenderBoundsPresentation.cs','Assets/Game/Configs/Skirmish/IndustrialBasin/OperationMap_IndustrialBasin.asset','Assets/Game/Scenes/Match.unity','Assets/Game/GeneratedOperationMaps/RuntimeBinding/opmap.skirmish.desert_base_01/Candidates/opmap_skirmish_desert_base_01_dense_city_entity_scene_runtime.unity','Assets/Game/GeneratedOperationMaps/RuntimeBinding/opmap.skirmish.desert_base_01/Candidates/opmap_skirmish_desert_base_01_dense_city_entity_scene_runtime.unity.meta','Assets/Game/Scenes/OperationMaps/Skirmish/Candidates/opmap_skirmish_desert_base_01_entity_presentation_dense_city_candidate.unity','Assets/Game/Scenes/OperationMaps/Skirmish/Candidates/opmap_skirmish_desert_base_01_entity_presentation_dense_city_candidate.unity.meta'}
 common=expected_startup&expected_playing;need(set(startup_pins)==expected_startup and set(playing_pins)==expected_playing and set(shared)==common,'Exact complete S073 startup, Playing, and shared source pin sets required')
 loaded=playing.get('loaded_campaign_source_fit_observation');scene_rel='Assets/Game/Scenes/OperationMaps/Skirmish/Candidates/opmap_skirmish_desert_base_01_entity_presentation_dense_city_candidate.unity';loaded_blockers=loaded.get('blocker_contributors') if isinstance(loaded,dict) else None;loaded_buildings=loaded.get('building_entities') if isinstance(loaded,dict) else None;loaded_completeness=loaded.get('capture_completeness') if isinstance(loaded,dict) else None
 expected_identity='2a2a791e8292a4f458bd603ceb86598aa0ba2ca82db41323bf5bf7c748ec6900';expected_content='2713962f0faa2dae49805e1b7e3a1673199a2cca915334d11421b354cd8f591c';map_id='opmap.skirmish.industrial_basin'
 source_scene_file=(repo/scene_rel).resolve(strict=True);source_scene_meta=Path(str(source_scene_file)+'.meta')
 need(isinstance(loaded,dict) and water_receipt.get('loaded_campaign_source_fit_observation')==loaded and loaded.get('kind')=='S073SameSessionLoadedCampaignSourceFitObservation' and loaded.get('scope')=='SamePlayingWorld_LoadedCampaignSceneOwnerAndSpawnInputs_DiagnosticOnly' and loaded.get('session_id')==playing.get('session_id') and loaded.get('phase')=='Playing' and loaded.get('world_name')==playing.get('world_name') and loaded.get('world_sequence')==playing.get('world_sequence') and loaded.get('logical_map_id')==map_id and loaded.get('campaign_source_map_id')=='opmap.skirmish.desert_base_01' and loaded.get('source_identity_hash')==expected_identity and loaded.get('source_content_hash')==expected_content and loaded.get('source_scene_guid')=='c00140f2e94a04c3084c8dcb0c18cbd0' and loaded.get('source_scene_path')==scene_rel and loaded.get('scene_loaded') is True and loaded.get('grid_owned_by_campaign_scene') is True and loaded.get('source_scene_sha256')==sha(source_scene_file.read_bytes())==playing_pins.get(scene_rel) and loaded.get('source_scene_meta_sha256')==sha(source_scene_meta.read_bytes())==playing_pins.get(scene_rel+'.meta') and 'guid: '+loaded.get('source_scene_guid','') in source_scene_meta.read_text() and loaded.get('s073_config_sha256')==playing_pins.get('Assets/Game/Configs/Skirmish/IndustrialBasin/OperationMap_IndustrialBasin.asset') and loaded.get('blocker_owner_source_sha256')==playing_pins.get('Assets/Game/Scripts/Systems/StaticGridBlockerUpdateSystem.cs') and isinstance(loaded_blockers,list) and loaded.get('blocker_contributor_query_count')==len(loaded_blockers) and loaded.get('blocker_contributors_owned_by_campaign_scene')==sum(row.get('bound_campaign_scene_owned') is True for row in loaded_blockers) and isinstance(loaded_buildings,list) and loaded.get('building_owner_query_count')==len(loaded_buildings) and loaded.get('building_entities_owned_by_campaign_scene')==sum(row.get('bound_campaign_scene_owned') is True for row in loaded_buildings) and isinstance(loaded.get('original_initial_unit_spawn_cells'),list) and isinstance(loaded.get('original_initial_building_spawn_inputs'),list) and type(loaded.get('original_initial_spawn_plan_available')) is bool and isinstance(loaded_completeness,dict) and loaded_completeness.get('loaded_campaign_scene') is True and loaded_completeness.get('active_grid_scene_ownership') is True and loaded_completeness.get('blocker_owner_inputs') is (len(loaded_blockers)>0) and loaded_completeness.get('building_owner_inputs') is (len(loaded_buildings)>0) and loaded_completeness.get('original_initial_spawn_plan') is loaded.get('original_initial_spawn_plan_available') and loaded.get('source_fit_certified') is False and loaded.get('physical_source_admission_changed') is False and loaded.get('scheduler_admitted') is False and loaded.get('mission_ready') is False and loaded.get('player_readiness') is False,'Exact same-session loaded Campaign source-fit diagnostic required without admission/readiness claims')
 capture_pins={}
 for relative in sorted(expected_startup|expected_playing):
  digest=startup_pins[relative] if relative in startup_pins else playing_pins[relative]
  source=(repo/relative).resolve(strict=True);need(not Path(relative).is_absolute() and source.is_relative_to(repo) and re.fullmatch('[0-9a-f]{64}',digest or '') and (relative not in startup_pins or startup_pins[relative]==digest) and (relative not in playing_pins or playing_pins[relative]==digest) and (relative not in shared or shared[relative]==digest) and sha(source.read_bytes())==digest,'S073 joint source pin changed: '+relative);capture_pins[str(source)]=digest
 artifact_pins={receipt_pin['path']:receipt_pin['sha256'],log_pin['path']:log_pin['sha256'],provider_file.as_posix():provider_hash}
 if wrapper_pin is not None:artifact_pins[wrapper_pin['path']]=wrapper_pin['sha256']
 if current_r1 or current_r3:artifact_pins[wrapper_provenance_pin['path']]=wrapper_provenance_pin['sha256']
 if current_r1 or current_r2 or current_r3:artifact_pins[water_receipt_pin['path']]=water_receipt_pin['sha256']
 for key in ('startup_observation','playing_observation'):artifact_pins[observation_pins[key]['path']]=observation_pins[key]['sha256']
 for key in ('startup_mask','playing_mask'):artifact_pins[mask_pins[key]['path']]=mask_pins[key]['sha256']
 artifact_pins[water_capture['native_water_capture_mask']['path']]=mask_sha['playing_mask']
 merge_pins(pins,capture_pins);merge_pins(pins,artifact_pins);verify(pins)
 return {'receipt':{'path':receipt_pin['path'],'sha256':receipt_pin['sha256']},'startup_observation':observation_pins['startup_observation'],'startup_mask':mask_pins['startup_mask'],'playing_observation':observation_pins['playing_observation'],'playing_mask':mask_pins['playing_mask'],'editor_log':log_pin,'wrapper_exit':wrapper_pin,'parity_status':'Match','masks_match':True,'differing_cells':0,'mask_sha256':mask_sha['playing_mask'],'session_id':startup['session_id'],'source_pins_sha256':shared}


def s073_source_host_diagnostic_expected(result,code,timed_out,admission_requested,no_player_policy_requested=False):
 admission=result.get('source_host_diagnostic_gameplay_admission') or {}
 scheduler=result.get('source_host_original_tactical_owner_scheduler_tick') or {}
 aria=result.get('source_host_aria_policy_step') or {}
 ticks=scheduler.get('ticks_executed');budget=scheduler.get('tick_budget')
 steps=aria.get('step_count');last=aria.get('last_step') or {}
 counts=scheduler.get('owner_update_counts') or {}
 common=bool(admission_requested and not timed_out and code==2 and
  result.get('source_host_diagnostic_activation') is True and admission.get('opt_in') is True and
  admission.get('shared_start_completion_predicate_passed') is True and
  admission.get('physical_source_validated_before') is False and admission.get('physical_source_validated_after') is False and
  result.get('operation_map_physical_source_validated') is False and result.get('world_initialized') is False and
  type(ticks) is int and ticks>0 and
  scheduler.get('status')=='RecurringOriginalOwnerTicksExecuted' and result.get('scheduler_tick_executed') is True and
  counts.get('AIEconomySystem')==ticks and counts.get('AISquadSystem')==ticks and counts.get('SkirmishRulesSystem')==ticks and
  aria.get('kind')=='S073LegacySharedAriaRecurringPolicySteps' and type(steps) is int and
  type(aria.get('accepted_action_count')) is int and aria.get('readiness_claimed') is False and
  aria.get('mission_outcome_claimed') is False and
  result.get('mission_ready') is False and result.get('map_blocker_parity_verified') is False)
 if not common:return False
 if type(budget) is int:
  return bool(1<=ticks<=3600 and ticks<=budget and
   result.get('status')=='DiagnosticPlayingNativeReadinessUnverified' and
   result.get('session_phase')=='Playing' and result.get('session_outcome')=='None' and
   ((not no_player_policy_requested and steps==ticks and type(last.get('action')) is str and
      aria.get('no_player_policy_diagnostic') is not True and scheduler.get('no_player_policy_diagnostic') is not True) or
    (no_player_policy_requested and steps==0 and aria.get('no_player_policy_diagnostic') is True and
      aria.get('planner_route')=='NoPlayerPolicy_ExplicitSourceDiagnostic' and
      aria.get('accepted_action_count')==0 and scheduler.get('no_player_policy_diagnostic') is True and
      scheduler.get('aria_steps')==[] and scheduler.get('aria_actions_accepted')==0 and
      scheduler.get('aria_failure') is None and result.get('native_player_input_observed') is False)) and
   result.get('original_legacy_rules_diagnostic_playing_step_passed') is True)
 if budget!='until_original_terminal_outcome':return False
 outcome=result.get('session_outcome')
 actual_aria_steps=scheduler.get('aria_steps')
 # Shared ARIA may stop naturally before the original owner scheduler reaches
 # its terminal state. Accept that observation only when both recorded ARIA
 # streams agree exactly and the original production owner corroborates the
 # same terminal state. This remains diagnostic evidence, never validation.
 runtime=scheduler.get('original_production_runtime') or {}
 stopped_terminal=bool(
  scheduler.get('aria_stopped') is True and scheduler.get('aria_failure') is None and
  type(steps) is int and 0<steps<=ticks and
  isinstance(actual_aria_steps,list) and len(actual_aria_steps)==steps and
  type(scheduler.get('session_id')) is str and bool(scheduler.get('session_id')) and
  result.get('session_id')==scheduler.get('session_id') and
  last.get('stopped') is True and actual_aria_steps[-1]==last and
  last.get('frame')==steps and scheduler.get('aria_actions_accepted')==aria.get('accepted_action_count') and
  runtime.get('source_host_terminal_observed') is True and
  runtime.get('source_host_terminal_phase')=='Finished' and
  runtime.get('source_host_terminal_outcome')==outcome)
 if stopped_terminal:
  return bool(result.get('status')=='DiagnosticTerminalOutcomeNativeReadinessUnverified' and
   result.get('session_phase')=='Finished' and outcome in {'Victory','Defeat','Draw'} and
   scheduler.get('terminal_phase')=='Finished' and scheduler.get('terminal_outcome')==outcome and
   ticks>0 and type(budget) is str and
   all(counts.get(owner)==ticks for owner in ('AIEconomySystem','AISquadSystem','SkirmishRulesSystem')))
 return bool(result.get('status')=='DiagnosticTerminalOutcomeNativeReadinessUnverified' and
  result.get('session_phase')=='Finished' and outcome in {'Victory','Defeat','Draw'} and
  scheduler.get('terminal_phase')=='Finished' and scheduler.get('terminal_outcome')==outcome and
  isinstance(actual_aria_steps,list) and len(actual_aria_steps)==steps and
  0<=steps<=ticks and ticks-steps in (0,1) and
  (steps==0 and aria.get('last_step') is None or steps>0 and type(last.get('action')) is str))

def s073_source_host_diagnostic_receipt_fields(result,observed):
 scheduler=result.get('source_host_original_tactical_owner_scheduler_tick') or {}
 budget=scheduler.get('tick_budget')
 outcome=result.get('session_outcome')
 terminal=bool(observed and budget=='until_original_terminal_outcome' and
  result.get('status')=='DiagnosticTerminalOutcomeNativeReadinessUnverified' and
  result.get('session_phase')=='Finished' and outcome in {'Victory','Defeat','Draw'} and
  scheduler.get('terminal_phase')=='Finished' and scheduler.get('terminal_outcome')==outcome)
 return {'validation_passed':False,'full_outcome_observed':terminal,
  'diagnostic_prefix_completed':bool(observed and type(budget) is int),
  'terminal_phase_observed':terminal,'terminal_outcome_observed':outcome if terminal else None,
  'mission_ready':False}

def s073_worker_timing_receipt_fields(result):
 scheduler=result.get('source_host_original_tactical_owner_scheduler_tick') or {}
 return {'worker_setup_wall_seconds':result.get('worker_setup_wall_seconds'),
  'worker_setup_timing_status':result.get('worker_setup_timing_status'),
  'worker_setup_timing_scope':result.get('worker_setup_timing_scope'),
  'simulation_advance_wall_seconds':result.get('simulation_advance_wall_seconds'),
  'simulation_advance_timing_status':result.get('simulation_advance_timing_status') or scheduler.get('simulation_advance_timing_status'),
  'simulation_advance_timing_scope':result.get('simulation_advance_timing_scope') or scheduler.get('simulation_advance_timing_scope'),
  'worker_total_wall_seconds':result.get('worker_total_wall_seconds'),
  'worker_total_timing_scope':result.get('worker_total_timing_scope')}

def s073_record_postrun_integrity_failure(out,error,executable,code,timed_out):
 out=Path(out)
 def observed_file(path):
  path=Path(path)
  if not path.is_file():return None
  return pin(path)
 binary=Path(executable['path'])
 failure={'schema_version':1,'kind':'S073PostRunIntegrityFailure',
  'status':'PostRunIntegrityVerificationFailed','error_type':type(error).__name__,'error_message':str(error),
  'worker_exit':code,'timed_out':timed_out,'expected_executable':executable,
  'executable_observed_after_worker':observed_file(binary),
  'worker_result_observed':observed_file(out/'result.json'),
  'worker_log_observed':observed_file(out/'process.log'),
  'worker_result_classified':False,'validation_passed':False,'mission_ready':False}
 write(out/'postrun-integrity-failure.json',failure)
 return failure

def s073_preparing_placement_scope_matches(result, historical_requested):
 if type(result) is not dict or type(historical_requested) is not bool:
  return False
 if historical_requested:
  return (result.get('historical_input_diagnostic') is True and
   result.get('historical_input_current_source_equivalence') is False and
   result.get('placement_input_scope') == 'HistoricalCapturedCampaignSource_CurrentSourceEquivalenceUnverified')
 return (result.get('historical_input_diagnostic') is False and
  result.get('placement_input_scope') == 'CapturedCampaignSource_NotLoadedSceneOrSourceFit')

def s073_natural_terminal_diagnostic_completed(receipt):
 return bool(type(receipt)is dict and receipt.get('mission')=='S073' and
  receipt.get('historical_input_diagnostic_requested') is not True and
  receipt.get('status')=='DiagnosticTerminalOutcomeNativeReadinessUnverified' and
  receipt.get('process_exit')==2 and receipt.get('timed_out') is False and
  receipt.get('source_host_diagnostic_observed') is True and
  receipt.get('full_outcome_observed') is True and receipt.get('diagnostic_prefix_completed') is False and
  receipt.get('terminal_phase_observed') is True and
  receipt.get('terminal_outcome_observed') in {'Victory','Defeat','Draw'} and
  receipt.get('validation_passed') is False and receipt.get('mission_ready') is False and
  receipt.get('map_blocker_parity_verified') is False)

def _validate_s073_source_transport_diagnostic(s073_source_diagnostic,s073_source_host_diagnostic_admission,s073_source_transport_diagnostic):
 need(type(s073_source_transport_diagnostic)is bool,'Explicit S073 source transport diagnostic selection required')
 if s073_source_transport_diagnostic:
  need(s073_source_diagnostic is True and s073_source_host_diagnostic_admission is True,'S073 source transport diagnostic requires both current-source and source-host diagnostic admissions')

def _validate_s073_ui_shell_state_owner_diagnostic(selected,receipt):
 need(type(selected)is bool,'Explicit S073 UI shell owner diagnostic selection required')
 if not selected:return
 manifest=receipt.get('source_manifest')
 need(type(manifest)is dict and set(manifest)=={'path','sha256','bytes'}and type(manifest['bytes'])is int and manifest['bytes']>=0,'Pinned derived S073 UI shell owner source manifest descriptor required')
 path=Path(manifest['path']).resolve(strict=True);need(path.is_file()and not path.is_symlink(),'Derived S073 UI shell owner source manifest file required')
 raw=path.read_bytes();need(len(raw)==manifest['bytes']and sha(raw)==manifest['sha256'],'Derived S073 UI shell owner source manifest changed')
 value=json.loads(raw)
 need('WARLINE_SOURCE_S073_UI_SHELL_OWNER_PROFILE'in value.get('compiler_defines',[]),'S073 UI shell owner diagnostic requires its derived source profile define')
 profile=value.get('experimental_source_profile',{})
 need(profile.get('id')=='S073UiShellStateOwnerOnCreateV1_NonNative'and 'Diagnostic-only'in profile.get('scope',''),'Exact diagnostic-only non-native S073 UI shell owner profile required')

def _run_s073_legacy_bundle(repo,receipt,build_pin,bundle,bundle_pin,out,timeout,max_ticks=None,s073_source_diagnostic=False,s073_source_host_diagnostic_admission=False,s073_source_transport_diagnostic=False,data_repo=None,s073_ui_shell_state_owner_diagnostic=False,s073_historical_input_diagnostic=False):
 required={'schema_version','kind','mission','seed','source_repository_root','asset_repository_root','prefab_capture','source_identity','legacy_config','launch_capture','base_graph','selected_map','operation_map_rotation_reference','campaign_physical','campaign_water','campaign_startup','authored_mask','mission_scene','production_prefabs','building_definitions','building_placement_config'}
 optional={'capture_relocation','streamed_physical_blocker_capture','native_physical_road_mask','native_physical_sidewalk_mask','native_physical_road_receipt','native_configured_catalog_receipt','native_configured_unit_receipt','native_ai_snapshot_receipt','native_ai_source_graph','native_exchange_source_graph','native_scene_binding_receipt','native_water_capture_receipt','native_water_capture_snapshot','native_water_capture_mask','native_water_startup_parity_receipt'}
 need(required<=set(bundle) and set(bundle)<=required|optional,'Exact S073 legacy bundle fields required')
 water_capture_fields={'native_water_capture_receipt','native_water_capture_snapshot','native_water_capture_mask'}
 need(not(set(bundle)&water_capture_fields) or water_capture_fields<=set(bundle),'All three S073 native water capture descriptors required together')
 road_inputs={'native_physical_road_mask','native_physical_sidewalk_mask','native_physical_road_receipt'}
 need(not(set(bundle)&road_inputs) or road_inputs<=set(bundle),'All three S073 physical road descriptors required together')
 need(bundle['schema_version']==1 and bundle['kind']=='S073LegacyStartupBundle'and bundle['mission']=='S073'and bundle['seed']==104729,'Exact finite S073 startup bundle required')
 need(receipt.get('mission')=='S073'and receipt.get('seed')==104729 and receipt.get('runtime_backend')=='LegacyS073OwnerV1','Matching S073 owner build required')
 need(Path(bundle['source_repository_root']).resolve(strict=True)==repo,'S073 source root differs from built checkout')
 asset_repo=Path(bundle['asset_repository_root']).resolve(strict=True);need(asset_repo==repo,'S073 assets must resolve from the pinned source checkout')
 inputs={};pins=dict(receipt['source_and_reference_pins']);merge_pins(pins,owned.pins())
 for name in ['prefab_capture','source_identity','legacy_config','launch_capture','base_graph','selected_map','operation_map_rotation_reference','campaign_physical','campaign_water','campaign_startup','authored_mask','mission_scene','production_prefabs','building_definitions','building_placement_config']+[name for name in ('capture_relocation','streamed_physical_blocker_capture','native_physical_road_mask','native_physical_sidewalk_mask','native_physical_road_receipt','native_configured_catalog_receipt','native_configured_unit_receipt','native_ai_snapshot_receipt','native_ai_source_graph','native_exchange_source_graph','native_scene_binding_receipt','native_water_capture_receipt','native_water_capture_snapshot','native_water_capture_mask','native_water_startup_parity_receipt') if name in bundle]:
  row=bundle[name];need(type(row)is dict and set(row)=={'path','sha256'}and type(row['path'])is str and re.fullmatch('[0-9a-f]{64}',row['sha256']),'Exact S073 input descriptor required: '+name)
  path=Path(row['path']).resolve(strict=True);need(path.is_file()and not path.is_symlink()and sha(path.read_bytes())==row['sha256'],'S073 bundle input changed: '+name)
  inputs[name]={'path':str(path),'sha256':row['sha256']};pins[str(path)]=row['sha256']
 need(type(s073_source_diagnostic)is bool,'Explicit S073 source-AI diagnostic selection required')
 need(type(s073_source_host_diagnostic_admission)is bool,'Explicit S073 source-host diagnostic admission selection required')
 need(type(s073_historical_input_diagnostic)is bool,'Explicit S073 historical input diagnostic selection required')
 need(not s073_historical_input_diagnostic or (s073_source_diagnostic and (s073_source_host_diagnostic_admission or type(max_ticks)is int and 0<max_ticks<=3600) and (max_ticks is None or type(max_ticks)is int and 0<max_ticks<=3600) and 'capture_relocation' in inputs),'S073 historical input requires current-source, bounded preparing or admitted source-host, and preserved source receipt')
 _validate_s073_source_transport_diagnostic(s073_source_diagnostic,s073_source_host_diagnostic_admission,s073_source_transport_diagnostic)
 _validate_s073_ui_shell_state_owner_diagnostic(s073_ui_shell_state_owner_diagnostic,receipt)
 water_capture=None
 if water_capture_fields<=set(inputs) and not s073_source_diagnostic:
  water_capture=_validate_s073_native_water_capture(repo,inputs,pins)
 water_startup_parity=None
 if 'native_water_startup_parity_receipt' in inputs and water_capture is not None and not s073_source_diagnostic:
  water_startup_parity=_validate_s073_native_water_startup_parity(repo,inputs,pins,water_capture)
 if 'native_scene_binding_receipt' in inputs:
  qualified=repo/'Logs/SkirmishProduction/s073-native-entity-scene-binding-20261101-r2/receipt-strict-r3.json'
  receipt_path=Path(inputs['native_scene_binding_receipt']['path'])
  need(receipt_path==qualified.resolve(strict=True),'Only the qualified S073 strict-r3 scene-binding receipt is admitted')
  need(inputs['native_scene_binding_receipt']['sha256']=='594bc068ae821c2858307124c11ee058a1b4e02a500988928bad044aabe31904','Exact qualified S073 scene-binding receipt digest required')
  binding_receipt,binding_receipt_pin=read_json_pinned(receipt_path)
  sibling=receipt_path.parent;observation_path=sibling/'observation.json';editor_log_path=sibling/'editor.log';wrapper_exit_path=sibling/'wrapper-exit.json'
  observation,observation_pin=read_json_pinned(observation_path);editor_log_pin=pin(editor_log_path);wrapper_exit,wrapper_exit_pin=read_json_pinned(wrapper_exit_path)
  need(wrapper_exit.get('exit_code')==0 and binding_receipt.get('unity_exit_code')==0,'Qualified scene-binding wrapper and Unity exits must be zero')
  marker=b'[S073NativeEntitySceneBinding] result=Recorded scope=actual_binding_outcome '
  need(marker in editor_log_path.read_bytes() and binding_receipt.get('unity_log_sha256')==editor_log_pin['sha256'],'S073 scene-binding editor log hash or Recorded marker mismatch')
  need(binding_receipt.get('schema_version')==1 and binding_receipt.get('kind')=='S073NativeEntitySceneBindingFinalization' and binding_receipt.get('recorded_observation')=='Recorded' and binding_receipt.get('observation_sha256')==observation_pin['sha256'],'Qualified strict-r3 S073 scene-binding receipt and durable observation required')
  need(binding_receipt.get('observation_path')=='/private/tmp/warline-s073-entity-binding-20261101-r2.json','Unexpected ephemeral observation identity in strict-r3 receipt')
  need(observation.get('schema_version')==1 and observation.get('kind')=='S073NativeEntitySceneBindingObservation' and observation.get('scope')=='ActualMatchSceneViewTryReadEntitySceneGridBindingOutcome_NotReadinessOrMissionAcceptance' and observation.get('scenario_index')==3 and observation.get('seed')==104729 and observation.get('phase')=='Playing' and observation.get('session_id')==binding_receipt.get('session_id') and observation.get('binding_read_succeeded') is True,'Exact actual S073 entity scene-binding outcome required')
  need(observation.get('mission_ready') is False and observation.get('source_fit_certified') is False and observation.get('normal_input_playthrough') is False and observation.get('player_readiness') is False and binding_receipt.get('mission_ready') is False and binding_receipt.get('source_fit_certified') is False and binding_receipt.get('normal_input_playthrough') is False and binding_receipt.get('player_readiness') is False,'Scene-binding evidence cannot assert readiness or source fit')
  binding_sources=observation.get('source_pins_sha256');receipt_sources=binding_receipt.get('source_pins_sha256')
  need(isinstance(binding_sources,dict) and len(binding_sources)>=5 and binding_sources==receipt_sources,'Complete matching scene-binding source pin set required')
  binding_pins={observation_pin['path']:observation_pin['sha256'],editor_log_pin['path']:editor_log_pin['sha256'],wrapper_exit_pin['path']:wrapper_exit_pin['sha256'],binding_receipt_pin['path']:binding_receipt_pin['sha256']}
  for relative,expected in binding_sources.items():
   source=(repo/relative).resolve(strict=True);need(not Path(relative).is_absolute() and source.is_relative_to(repo) and re.fullmatch('[0-9a-f]{64}',expected or '') and sha(source.read_bytes())==expected,'S073 scene-binding source dependency changed: '+str(relative));binding_pins[str(source)]=expected
  merge_pins(pins,binding_pins)
 need(('native_ai_snapshot_receipt' in inputs)==('native_ai_source_graph' in inputs),'Native AI snapshot receipt and authored source graph must be supplied together')
 need('native_exchange_source_graph' not in inputs or 'native_ai_source_graph' in inputs and 'native_ai_snapshot_receipt' in inputs,'Native exchange source graph requires the qualified native AI graph and receipt')
 if 'native_ai_snapshot_receipt' in inputs and not s073_source_diagnostic:
  ai_receipt_path=Path(inputs['native_ai_snapshot_receipt']['path']);ai_receipt=json.loads(ai_receipt_path.read_bytes())
  need(ai_receipt.get('kind')=='S073NativeAIStartupSnapshotReceipt' and ai_receipt.get('schema_version')==1 and ai_receipt.get('validation_status')=='Passed' and ai_receipt.get('wrapper_exit_code')==0 and ai_receipt.get('required_marker_present') is True and ai_receipt.get('normal_input_playthrough') is False and ai_receipt.get('mission_ready') is False,'Qualified native S073 AI startup snapshot receipt required')
  snapshot_path=ai_receipt_path.parent/'snapshot.json';ai_log_path=ai_receipt_path.parent/'editor.log';snapshot,snapshot_pin=read_json_pinned(snapshot_path);ai_log_pin=pin(ai_log_path)
  need(snapshot_pin['sha256']==ai_receipt.get('snapshot_sha256') and ai_log_pin['sha256']==ai_receipt.get('editor_log_sha256') and b'[S073NativeAIStartupSnapshot] result=Passed scope=instrumented_startup ' in ai_log_path.read_bytes(),'Native AI snapshot, log or marker changed')
  need(snapshot.get('kind')=='S073NativeAIStartupSnapshot' and snapshot.get('schema_version')==1 and snapshot.get('scope')=='OriginalS073InstrumentedStartupSnapshot_NotNormalInputOrReadiness' and snapshot.get('scenario_index')==3 and snapshot.get('seed')==104729 and snapshot.get('phase')=='Playing' and snapshot.get('map_id')=='opmap.skirmish.industrial_basin' and snapshot.get('source_map_id')=='opmap.skirmish.desert_base_01' and snapshot.get('source_identity_hash')=='2a2a791e8292a4f458bd603ceb86598aa0ba2ca82db41323bf5bf7c748ec6900' and snapshot.get('session_id')==ai_receipt.get('session_id') and snapshot.get('gameplay_start_complete') is True and snapshot.get('gameplay_start_failed') is False and snapshot.get('normal_input_playthrough') is False and snapshot.get('player_readiness') is False,'Exact instrumented native S073 AI startup observation required')
  ai_source_pins=snapshot.get('source_pins_sha256');need(isinstance(ai_source_pins,dict) and len(ai_source_pins)==ai_receipt.get('source_pin_count') and len(ai_source_pins)>=20,'Complete native AI source pin set required')
  ai_pins={snapshot_pin['path']:snapshot_pin['sha256'],ai_log_pin['path']:ai_log_pin['sha256']}
  for relative,expected in ai_source_pins.items():
   source=(repo/relative).resolve(strict=True);need(not Path(relative).is_absolute() and source.is_relative_to(repo) and re.fullmatch('[0-9a-f]{64}',expected or '') and sha(source.read_bytes())==expected,'Native AI source dependency changed: '+str(relative));ai_pins[str(source)]=expected
  graph_path=Path(inputs['native_ai_source_graph']['path']);graph,graph_pin=read_json_pinned(graph_path)
  need(graph_pin['sha256']=='2f666edfd309232aa78fa42bb6ada68ebbf57600a0ddbd89ada7046437a72873','Exact qualified S073 authored AI graph required')
  need(graph_pin['sha256']==inputs['native_ai_source_graph']['sha256'] and graph.get('schema_version')==1 and graph.get('authority')=='AuthoredConfigAssets' and graph.get('compiled_setup') is False and graph.get('world_initialized') is False and graph.get('native_world_state_used') is False and graph.get('preset_path')=='Assets/Game/Resources/SkirmishIndustrialBasin.asset','Exact authored S073 AI source graph required')
  graph_sources=graph.get('source_pins');need(isinstance(graph_sources,dict) and len(graph_sources)>=12,'Complete authored AI graph source pins required')
  required_ai_sources={'Assets/Game/Resources/SkirmishIndustrialBasin.asset','Assets/Game/Resources/SkirmishIndustrialBasin.asset.meta','Assets/Game/Configs/Skirmish/Enemy.asset','Assets/Game/Configs/Skirmish/Enemy.asset.meta','Assets/Game/Configs/Skirmish/Player.asset','Assets/Game/Configs/Skirmish/Player.asset.meta','Assets/Game/Configs/Skirmish/Plan.asset','Assets/Game/Configs/Skirmish/Plan.asset.meta','Assets/Game/Scripts/Configs/SkirmishPresetConfig.cs.meta','Assets/Game/Scripts/Configs/GameplayConfigModels.cs.meta','Assets/Game/Scripts/Configs/AIPlanEntryStartupConfig.cs.meta','Tools/CI/SkirmishSourceSimulation/Recipes/selected_operation_map/helpers/read_source_configs.rb','Tools/CI/SkirmishSourceSimulation/Fixtures/S073/retained/provenance.json'}
  need({str((repo/name).resolve()) for name in required_ai_sources}<=set(graph_sources),'Authored AI graph dependency closure incomplete')
  for name,expected in graph_sources.items():
   source=Path(name).resolve(strict=True);need(Path(name).is_absolute() and source.is_relative_to(repo) and re.fullmatch('[0-9a-f]{64}',expected or '') and sha(source.read_bytes())==expected,'Authored AI graph source changed: '+str(name));relative=source.relative_to(repo).as_posix();need(relative not in ai_source_pins or ai_source_pins[relative]==expected,'Native AI observation and authored graph source pins differ: '+relative);ai_pins[str(source)]=expected
  nodes=graph.get('nodes');need(isinstance(nodes,list) and [row.get('source_path') for row in nodes]==['Assets/Game/Configs/Skirmish/Enemy.asset','Assets/Game/Configs/Skirmish/Player.asset','Assets/Game/Configs/Skirmish/Plan.asset'],'Exact authored AI controller and plan order required')
  need(graph.get('preset_ai_controllers')==[{'fileID':11400000,'guid':nodes[0].get('asset_guid'),'type':2},{'fileID':11400000,'guid':nodes[1].get('asset_guid'),'type':2}] and graph.get('preset_ai_plan')=={'fileID':11400000,'guid':nodes[2].get('asset_guid'),'type':2},'Authored AI preset references differ from graph nodes')
  ai_pins[graph_pin['path']]=graph_pin['sha256'];merge_pins(pins,ai_pins)
 if 'native_exchange_source_graph' in inputs and not s073_source_diagnostic:
  exchange_path=Path(inputs['native_exchange_source_graph']['path']).resolve(strict=True)
  need(exchange_path.is_relative_to(repo),'Native exchange graph must be inside the repository')
  exchange,exchange_pin=read_json_pinned(exchange_path)
  need(exchange_pin['sha256']=='cf51352a9046d8680cc8611b6d253fc812568b4169163afcb85e375ba7954427' and exchange_pin['sha256']==inputs['native_exchange_source_graph']['sha256'],'Exact qualified native exchange graph required')
  exchange_sources=exchange.get('source_pins');need(isinstance(exchange_sources,dict) and len(exchange_sources)>0,'Native exchange graph source pins required')
  exchange_pins={exchange_pin['path']:exchange_pin['sha256']}
  for name,expected in exchange_sources.items():
   raw_source=Path(name);source=(raw_source if raw_source.is_absolute() else repo/raw_source).resolve(strict=True)
   need(source.is_relative_to(repo) and re.fullmatch('[0-9a-f]{64}',expected or '') and sha(source.read_bytes())==expected,'Native exchange graph source changed or escapes the repository: '+str(name))
   exchange_pins[str(source)]=expected
  merge_pins(pins,exchange_pins)
 if 'native_configured_catalog_receipt' in inputs and not s073_source_diagnostic:
  metadata_path=Path(inputs['native_configured_catalog_receipt']['path']);metadata=json.loads(metadata_path.read_bytes())
  need(metadata.get('kind')=='S073ConfiguredCatalogMetadataReceipt' and metadata.get('schema_version')==1 and metadata.get('validation_status')=='Passed' and metadata.get('wrapper_exit_code')==0 and metadata.get('required_marker_present') is True and metadata.get('mission_ready') is False,'Qualified S073 authored metadata receipt required')
  catalog_path=metadata_path.parent/'catalog.json';log_path=metadata_path.parent/'editor.log'
  catalog,catalog_pin=read_json_pinned(catalog_path);log_pin=pin(log_path)
  need(catalog_pin['sha256']==metadata.get('catalog_sha256') and log_pin['sha256']==metadata.get('editor_log_sha256') and b'[S073CatalogMetadataExport] result=Passed entries=6 ' in log_path.read_bytes(),'S073 authored metadata capture or marker changed')
  source_pins=catalog.get('source_dependencies_sha256');need(isinstance(source_pins,dict) and len(source_pins)==metadata.get('source_pin_count') and len(source_pins)>=6,'Complete S073 metadata source pins required')
  metadata_pins={catalog_pin['path']:catalog_pin['sha256'],log_pin['path']:log_pin['sha256']}
  for relative,expected in source_pins.items():
   source=(repo/relative).resolve(strict=True);need(not Path(relative).is_absolute() and source.is_relative_to(repo) and re.fullmatch('[0-9a-f]{64}',expected or ''),'Valid in-repository metadata dependency required');metadata_pins[str(source)]=expected
  merge_pins(pins,metadata_pins)
 if 'native_configured_unit_receipt' in inputs and not s073_source_diagnostic:
  need('native_configured_catalog_receipt' in inputs,'Qualified building metadata required before captured unit publication')
  unit_path=Path(inputs['native_configured_unit_receipt']['path']);unit_receipt=json.loads(unit_path.read_bytes())
  need(unit_receipt.get('kind')=='S073ConfiguredUnitCatalogReceipt' and unit_receipt.get('schema_version')==1 and unit_receipt.get('validation_status')=='Passed' and unit_receipt.get('wrapper_exit_code')==0 and unit_receipt.get('required_marker_present') is True and unit_receipt.get('mission_ready') is False,'Qualified S073 configured-unit receipt required')
  unit_catalog,unit_catalog_pin=read_json_pinned(unit_path.parent/'unit-catalog.json');unit_log=unit_path.parent/'editor.log';unit_log_pin=pin(unit_log)
  need(unit_catalog_pin['sha256']==unit_receipt.get('catalog_sha256') and unit_log_pin['sha256']==unit_receipt.get('editor_log_sha256') and b'[S073UnitCatalogExport] result=Passed entries=3 ' in unit_log.read_bytes(),'S073 unit capture or marker changed')
  unit_source_pins=unit_catalog.get('source_dependencies_sha256');need(isinstance(unit_source_pins,dict) and len(unit_source_pins)==unit_receipt.get('source_pin_count') and len(unit_source_pins)>=6,'Complete S073 unit metadata source pins required')
  unit_pins={unit_catalog_pin['path']:unit_catalog_pin['sha256'],unit_log_pin['path']:unit_log_pin['sha256']}
  for relative,expected in unit_source_pins.items():
   source=(repo/relative).resolve(strict=True);need(not Path(relative).is_absolute() and source.is_relative_to(repo) and re.fullmatch('[0-9a-f]{64}',expected or ''),'Valid in-repository unit metadata dependency required');unit_pins[str(source)]=expected
  merge_pins(pins,unit_pins)
 source=json.loads(Path(inputs['legacy_config']['path']).read_text());identity=json.loads(Path(inputs['source_identity']['path']).read_text());launch=json.loads(Path(inputs['launch_capture']['path']).read_text())
 need(source.get('kind')=='S073LegacyInitialSpawnExternalData'and source.get('mission')=='S073','Exact current S073 readback required')
 artifacts=source.get('source_artifacts',{});need(artifacts.get('name_readback_sha256')==inputs['source_identity']['sha256']and artifacts.get('prefab_capture_sha256')==inputs['prefab_capture']['sha256'],'S073 readback lineage differs from input descriptors')
 need(identity.get('kind')=='S073LegacyPrefabSourceNameReadback'and identity.get('mission')=='S073'and identity.get('unity_world_allocated')is False and identity.get('mission_world_allocated')is False,'Exact source-only S073 asset identity readback required')
 need(launch.get('kind')=='S073LiveOriginalAuthoredBuildingMask'and launch.get('scenario_index')==3 and launch.get('seed')==104729 and launch.get('selected_source_path')=='Assets/Game/Configs/Skirmish/IndustrialBasin/OperationMap_IndustrialBasin.asset','Exact retained S073 native launch capture required')
 map_rel='Assets/Game/Configs/Skirmish/IndustrialBasin/OperationMap_IndustrialBasin.asset';map_pins=json.loads(Path(inputs['prefab_capture']['path']).read_text()).get('source_pins',{})
 need(inputs['selected_map']['path']==str((asset_repo/map_rel).resolve())and map_pins.get(map_rel)==inputs['selected_map']['sha256'],'Selected S073 map must match retained prefab-capture source identity')
 for rel,digest in artifacts.get('configuration_asset_sha256',{}).items():
  path=(asset_repo/rel).resolve(strict=True);need(path.is_relative_to(asset_repo)and sha(path.read_bytes())==digest,'S073 authored configuration source changed: '+rel);pins[str(path)]=digest
 executable=receipt['executable'];need(sha(Path(executable['path']).read_bytes())==executable['sha256'],'S073 executable changed')
 tools={k:Path(v).resolve(strict=True)for k,v in receipt['runtime_selection'].items()};config,extra=prepare_mono_config(tools,out);merge_pins(pins,extra)
 graph_generator=repo/'Tools/CI/SkirmishSourceSimulation/Tests/generate_s073_operation_map_graph.py';graph_generator_pin=pin(graph_generator);graph_out=out/'selected-operation-map-graph'
 generated=subprocess.run([sys.executable,str(graph_generator),'--repo',str(repo),'--base-graph',inputs['base_graph']['path'],'--base-graph-sha256',inputs['base_graph']['sha256'],'--selected-map-sha256',inputs['selected_map']['sha256'],'--output',str(graph_out)],capture_output=True,text=True,timeout=60)
 (out/'selected-operation-map-graph-generation.log').write_text(generated.stdout+generated.stderr);need(generated.returncode==0,'S073 selected OperationMap source graph generation failed')
 graph_result=json.loads((graph_out/'result.json').read_text());need(graph_result.get('kind')=='S073SelectedOperationMapSourceGraphExtension'and graph_result.get('status')=='Passed'and graph_result.get('selected_map',{}).get('sha256')==inputs['selected_map']['sha256'],'S073 selected OperationMap source graph identity mismatch')
 graph_descriptor=graph_result['output_graph'];need(sha(Path(graph_descriptor['path']).read_bytes())==graph_descriptor['sha256'],'Generated S073 source graph changed')
 request={'runtime_backend':'LegacyS073OwnerV1','schema_version':1,'kind':'SkirmishSourceSimulationRun','mission':'S073','seed':104729,'repository_root':str(repo),'source_repository_root':str(repo),'output_directory':str(out),'runtime_config':pin(config),'s073_bundle':bundle_pin,'selected_operation_map_graph':graph_descriptor,'source_build_receipt':build_pin,'source_executable':executable,'s073_source_mode':('current-source-unverified-native-parity' if s073_source_diagnostic else 'qualified-native-observation'),'s073_source_host_diagnostic_admission':s073_source_host_diagnostic_admission,'s073_ui_shell_state_owner_diagnostic':s073_ui_shell_state_owner_diagnostic}
 if s073_historical_input_diagnostic:request['s073_historical_input_diagnostic']=True
 if s073_source_transport_diagnostic:request['source_host_transport_diagnostic']=True
 if max_ticks is not None:need(type(max_ticks)is int and 0<max_ticks<=3600,'Bounded S073 scheduler tick budget required');request['max_ticks']=max_ticks
 ai_graph_generator=repo/'Tools/CI/SkirmishSourceSimulation/Tests/generate_s073_ai_source_graph.py';ai_graph_generator_pin=pin(ai_graph_generator);ai_graph_out=out/'current-authored-ai-source-graph'
 ai_graph_run=subprocess.run([sys.executable,str(ai_graph_generator),'--repo',str(repo),'--output',str(ai_graph_out)],capture_output=True,text=True,timeout=60)
 (out/'current-authored-ai-source-graph-generation.log').write_text(ai_graph_run.stdout+ai_graph_run.stderr);need(ai_graph_run.returncode==0,'S073 current authored AI source graph generation failed')
 ai_graph_result=json.loads(ai_graph_run.stdout);need(ai_graph_result.get('status')=='Passed','S073 current authored AI graph did not pass source readback')
 ai_graph_path=ai_graph_out/'source-ai-config-graph.json';ai_graph_descriptor=pin(ai_graph_path);need(ai_graph_result.get('graph_sha256')==ai_graph_descriptor['sha256'],'S073 current authored AI graph identity mismatch')
 request['s073_current_authored_ai_source_graph']=ai_graph_descriptor
 if water_capture is not None:request['native_water_capture']=water_capture
 pins[str(graph_generator.resolve())]=graph_generator_pin['sha256'];pins[graph_descriptor['path']]=graph_descriptor['sha256'];pins[str(ai_graph_generator.resolve())]=ai_graph_generator_pin['sha256'];pins[str(ai_graph_path.resolve())]=ai_graph_descriptor['sha256']
 write(out/'run-request.json',request);merge_pins(pins,{build_pin['path']:build_pin['sha256'],bundle_pin['path']:bundle_pin['sha256'],executable['path']:executable['sha256'],str((out/'run-request.json').resolve()):sha((out/'run-request.json').read_bytes())});verify(pins)
 env=os.environ.copy();env.pop('WARLINE_S073_SOURCE_TRANSPORT_DIAGNOSTIC',None);env.pop('WARLINE_S073_UI_SHELL_OWNER_DIAGNOSTIC',None)
 if s073_source_transport_diagnostic:env['WARLINE_S073_SOURCE_TRANSPORT_DIAGNOSTIC']='1'
 if s073_ui_shell_state_owner_diagnostic:env['WARLINE_S073_UI_SHELL_OWNER_DIAGNOSTIC']='1'
 env['MONO_PATH']=os.pathsep.join([str(repo/'Library/ScriptAssemblies'),str(tools['engine']),str(tools['newtonsoft'].parent),str(Path(receipt['placement_build']).parent)])
 start=time.monotonic();timed_out=False;code=None
 with (out/'process.log').open('w')as log:
  process=subprocess.Popen([str(tools['mono']),'--config',str(config),'--optimize=-float32',executable['path'],str(out/'run-request.json')],env=env,stdout=log,stderr=subprocess.STDOUT)
  try:code=process.wait(timeout=timeout)
  except subprocess.TimeoutExpired:timed_out=True
  finally:
   if process.poll()is None:
    process.terminate()
    try:process.wait(timeout=5)
    except subprocess.TimeoutExpired:process.kill();process.wait(timeout=5)
   code=process.returncode
 try:
  verify(pins)
 except Exception as error:
  s073_record_postrun_integrity_failure(out,error,executable,code,timed_out)
  raise
 result=json.loads((out/'result.json').read_text())if(out/'result.json').is_file()else{}
 if s073_historical_input_diagnostic and result.get('status') not in (None,'Failed'):
  need(result.get('historical_input_diagnostic') is True and result.get('historical_input_current_source_equivalence') is False and result.get('map_blocker_parity_verified') is False and result.get('mission_ready') is False,'Historical S073 result must remain explicitly unqualified')
 owner_admitted=result.get('ui_shell_state_owner_diagnostic_admitted')is True
 need(owner_admitted is s073_ui_shell_state_owner_diagnostic,'S073 UI shell owner diagnostic admission differs from the explicit run selection')
 if owner_admitted:
  owner=result.get('ui_shell_state_owner_initialization')
  need(type(owner)is dict and owner.get('kind')=='S073RetainedSourceUiShellOwnerInitialization'and owner.get('owner')=='UiShellStateSystem.OnCreate/EnsureBoundary'and owner.get('gateway_registration_invoked')is False and owner.get('flow_or_owner_update_invoked')is False and owner.get('unity_native_runtime')is False and owner.get('player_or_ui_readiness')is False,'Exact source-only original S073 shell owner initialization readback required')
 strict_expected=not timed_out and code==2 and result.get('status')=='PreparingPhysicalSourceRequired'and result.get('original_metadata_compiler_passed')is True and result.get('rotation_reference_sha256')==inputs['operation_map_rotation_reference']['sha256'] and result.get('session_phase')=='Preparing' and result.get('original_preparemap_before_startup')is True and result.get('preparation_scenery_scope')=='SourceHostWithoutLoadedPhysicalScene'and result.get('preset_readback_bound_to_queued_entity_manager')is True and result.get('economy_owner_projection_matched')is True and result.get('economy_owner_applied')is False and result.get('selected_operation_map_source_materialized')is True and result.get('operation_map_id')=='opmap.skirmish.industrial_basin'and result.get('operation_map_content_hash')=='cbbaa8366cbfb5e0ad9bc6bf70a98b48503df10e3d0c5c45757295af841a7a6d'and result.get('operation_map_source_identity_hash')=='2a2a791e8292a4f458bd603ceb86598aa0ba2ca82db41323bf5bf7c748ec6900'and result.get('source_map_anchor_count')==2 and result.get('source_map_camera_count')==1 and result.get('world_initialized')is False and result.get('operation_map_published')is True and result.get('operation_map_publication_scope')=='MetadataOnly_PhysicalSourceNotValidated' and result.get('operation_map_physical_source_validated')is False and result.get('original_logical_reuse_identity_matched')is True and result.get('mismatched_mission_rejected')is True and result.get('mismatched_scenario_rejected')is True and result.get('source_grid_baked')is True and result.get('source_surface_decoded')is True and result.get('original_initial_road_sync_executed')is True and s073_preparing_placement_scope_matches(result,s073_historical_input_diagnostic) and result.get('captured_water_footprints')==194 and result.get('source_host_start_transition_invoked')is True and result.get('native_player_input_observed')is False and result.get('original_building_requests_queued')==10 and result.get('building_and_unit_startup_diagnostic',{}).get('status')=='Passed' and result.get('building_and_unit_startup_diagnostic',{}).get('registered_buildings')==10 and result.get('building_and_unit_startup_diagnostic',{}).get('units_spawned')is True and result.get('building_and_unit_startup_diagnostic',{}).get('initial_spawn_completion_marked')is True and result.get('building_and_unit_startup_diagnostic',{}).get('same_world_host_retained')is True and result.get('original_legacy_rules_preparing_step_passed')is True and result.get('scheduler_tick_executed')is False and result.get('initial_spawn_owner_step',{}).get('owner_update_invoked')is True and result.get('initial_spawn_owner_step',{}).get('owner_marked_completion')is False and result.get('map_blocker_parity_status') in {'unverified','qualified-native-observation'} and type(result.get('map_blocker_parity_verified')) is bool and result.get('map_blocker_parity_verified') is (result.get('map_blocker_parity_status')=='qualified-native-observation') and result.get('ai_startup_mode')==('OriginalCurrentAuthoredConfigOwner_NativeParityUnverified' if s073_source_diagnostic else 'QualifiedNativeObservationBound' if 'native_ai_snapshot_receipt' in inputs else 'NotInvoked_NoAIObservationSupplied') and result.get('mission_ready')is False
 diagnostic_expected=s073_source_host_diagnostic_expected(result,code,timed_out,s073_source_host_diagnostic_admission,os.environ.get('WARLINE_S073_NO_PLAYER_POLICY_DIAGNOSTIC')=='1')
 expected=diagnostic_expected if s073_source_host_diagnostic_admission else strict_expected
 source_host_diagnostic_observed=bool(diagnostic_expected)
 diagnostic_fields=s073_source_host_diagnostic_receipt_fields(result,diagnostic_expected)
 receipt_out={'schema_version':1,'kind':'SkirmishSourceSimulationRunReceipt','mission':'S073','seed':104729,'source_repository_root':str(Path(repo).resolve()),'data_repository_root':str(Path(data_repo if data_repo is not None else repo).resolve()),'status':result.get('status','Failed'),'process_exit':code,'timed_out':timed_out,'wall_seconds':time.monotonic()-start,'wall_seconds_scope':'CLI elapsed from immediately before worker process-log open/launch through result pin verification and receipt assembly; excludes build and earlier run-input preparation','historical_input_diagnostic_requested':s073_historical_input_diagnostic,'source_diagnostic_classification':('ExplicitS073HistoricalCampaignInputDiagnostic' if s073_historical_input_diagnostic else 'ExplicitS073UiShellStateOwnerDiagnostic' if s073_ui_shell_state_owner_diagnostic else 'ExplicitS073SourceHostTransportDiagnostic' if s073_source_transport_diagnostic else 'S073SourceHostDiagnosticAdmission' if s073_source_host_diagnostic_admission else 'S073CurrentSourceDiagnostic' if s073_source_diagnostic else 'QualifiedNativeObservation'),'source_transport_diagnostic_requested':s073_source_transport_diagnostic,'ui_shell_state_owner_diagnostic_requested':s073_ui_shell_state_owner_diagnostic,'ui_shell_state_owner_diagnostic_admitted':owner_admitted,'ui_shell_state_owner_initialization':result.get('ui_shell_state_owner_initialization'),'validation_passed':diagnostic_fields['validation_passed'],'full_outcome_observed':diagnostic_fields['full_outcome_observed'],'diagnostic_prefix_completed':diagnostic_fields['diagnostic_prefix_completed'],'terminal_phase_observed':diagnostic_fields['terminal_phase_observed'],'terminal_outcome_observed':diagnostic_fields['terminal_outcome_observed'],'mission_ready':diagnostic_fields['mission_ready'],'preparing_session_observed':result.get('session_phase')=='Preparing','source_host_diagnostic_observed':source_host_diagnostic_observed,'map_blocker_parity_status':result.get('map_blocker_parity_status'),'map_blocker_parity_verified':result.get('map_blocker_parity_verified'),'ai_startup_mode':result.get('ai_startup_mode'),'blocking_gate':result.get('blocking_gate'),'build_receipt':build_pin,'data_bundle':bundle_pin,'native_water_capture':water_capture,'native_water_startup_parity_receipt':water_startup_parity,'worker_result':pin(out/'result.json')if(out/'result.json').is_file()else None,**s073_worker_timing_receipt_fields(result)}
 write(out/'run-receipt.json',receipt_out);need(expected,'S073 worker did not reach the explicit preparing/physical-source-required gate; see '+str(out/'process.log'));return receipt_out

def _s074_alias_runtime_admitted(repo,initial,inputs,package,build_provenance,max_ticks,build_pin,selected_policy):
 """Allow only the proven S074 Industrial Basin/Desert Base no-camera diagnostic alias."""
 if not _s074_qualified_external_context(initial) or not isinstance(inputs,dict) or inputs.get('mission')!='S074' or initial.get('mission_id')!='S074':return False
 if type(max_ticks)is not int or not 0<max_ticks<=72000:return False
 try:expected_policy=source_camera_policy_selection(inputs,initial,build_provenance,max_ticks,build_pin)
 except (ValueError,TypeError,KeyError):return False
 if expected_policy is None or selected_policy!=expected_policy or not _s074_no_camera_policy_matches(initial,inputs,selected_policy,build_pin):return False
 descriptor=inputs.get('source_derived_map');root=Path(repo).resolve(strict=True)
 expected_package=root/'T Logs/SkirmishProduction/s026-source-map-bundle-20261107-r2/map-package.json'
 try:expected_pin=pin(expected_package)
 except (OSError,ValueError):return False
 logical=initial.get('static_map');physical=package.get('static_map') if isinstance(package,dict) else None
 return descriptor=={'path':expected_pin['path'],'sha256':'233a93bc7738b40f97beecf001f31cdd9d169cdc42e011f45ab7483c61d723b1'} and \
  expected_pin['sha256']==descriptor['sha256'] and isinstance(logical,dict) and isinstance(physical,dict) and \
  logical.get('operation_map_id')=='opmap.skirmish.industrial_basin' and physical.get('operation_map_id')=='opmap.skirmish.desert_base_01' and \
  logical.get('definition')==initial.get('selected_operation_map') and \
  logical.get('definition',{}).get('path')==str((root/'Assets/Game/Configs/Skirmish/IndustrialBasin/OperationMap_IndustrialBasin.asset').resolve(strict=True)) and \
  logical.get('definition',{}).get('sha256')=='b145eae26821fa3ee559de12473354813c83d0a4c1997159086c20654ea0112a' and \
  all(logical.get(key)==physical.get(key) for key in ('grid_config','surface_data'))

def prepare_source_derived_map_join(repo,initial,package,package_pin,build_pin,out,prepare_only,allow_runtime_alias=False):
 """Preflight a captured physical map; only the explicitly qualified S074 alias may continue to runtime."""
 if package.get('static_map')!=initial.get('static_map'):
  need(prepare_only or allow_runtime_alias,'Logical-to-physical source map join is currently prepare-only')
 package_reader=owned.load_source_map_package(repo/'Tools/CI')
 preflight=package_reader.preflight_package(package_pin['path'])
 if package.get('static_map')==initial.get('static_map'):
  return preflight,None,{}
 logical=initial.get('static_map');physical=package.get('static_map');graph=initial.get('source_config_graph')
 need(type(logical)is dict and type(physical)is dict and type(graph)is dict,'Pinned logical map, physical map and source graph required')
 for key in ('grid_config','surface_data'):
  need(logical.get(key)==physical.get(key),'Logical and physical map input differs: '+key)
 logical_def=logical.get('definition');physical_def=physical.get('definition')
 need(type(logical_def)is dict and type(physical_def)is dict and initial.get('selected_operation_map')==logical_def,'Selected logical map definition must match initialization')
 root=Path(repo).resolve(strict=True)
 def relative(descriptor):
  need(type(descriptor)is dict and isinstance(descriptor.get('path'),str)and isinstance(descriptor.get('sha256'),str),'Pinned map definition required')
  path=Path(descriptor['path']).resolve(strict=True);need(path.is_relative_to(root),'Map definition must be repository-owned')
  need(sha(path.read_bytes())==descriptor['sha256'],'Map definition pin changed')
  return str(path.relative_to(root))
 logical_path=relative(logical_def);physical_path=relative(physical_def)
 need(type(graph)is dict and isinstance(graph.get('path'),str)and isinstance(graph.get('sha256'),str),'Pinned authored source graph required')
 graph_path=Path(graph['path']).resolve(strict=True);need(sha(graph_path.read_bytes())==graph['sha256'],'Authored source graph pin changed')
 wrapper=root/'Tools/CI/SkirmishSourceSimulation/Recipes/source_map_package/run_operation_map_contract_source_host.py'
 wrapper_pin=pin(wrapper);join_out=Path(out)/'source-derived-map-logical-join'
 physical_preflight=Path(out)/'source-derived-map-physical-preflight.json';write(physical_preflight,preflight);physical_preflight_pin=pin(physical_preflight)
 command=[sys.executable,str(wrapper),'--repo',str(root),'--graph',str(graph_path),'--graph-sha256',graph['sha256'],'--build',build_pin['path'],'--build-sha256',build_pin['sha256'],'--package',package_pin['path'],'--package-sha256',package_pin['sha256'],'--package-preflight',physical_preflight_pin['path'],'--package-preflight-sha256',physical_preflight_pin['sha256'],'--logical',logical_path,'--physical',physical_path,'--output',str(join_out)]
 completed=subprocess.run(command,capture_output=True,text=True,timeout=900)
 (Path(out)/'source-derived-map-logical-join-console.log').write_text(completed.stdout+completed.stderr)
 need(completed.returncode==0 and '[OriginalOperationMapContract] result=Passed' in completed.stdout,'Original logical-to-physical map validation failed; see join evidence')
 need(pin(wrapper)['sha256']==wrapper_pin['sha256'],'Logical map join code changed during execution')
 join,join_pin=read_json_pinned(join_out/'receipt.json');preflight,preflight_pin=read_json_pinned(join_out/'package-preflight.json')
 result=join.get('result',{})
 need(join.get('kind')=='OriginalOperationMapContractSourceOnlyReceipt'and join.get('status')=='Passed'and join.get('world_initialized')is False and join.get('mission_ready')is False and join.get('source_fit_certified')is False,'Unqualified original map contract receipt required')
 need(join.get('source_package',{}).get('sha256')==package_pin['sha256'] and join.get('source_graph',{}).get('sha256')==graph['sha256'] and join.get('source_build',{}).get('sha256')==build_pin['sha256'],'Map join source pins differ')
 need(join.get('logical_source',{}).get('path')==logical_path and join.get('logical_source',{}).get('sha256')==logical_def['sha256'] and join.get('physical_source',{}).get('path')==physical_path and join.get('physical_source',{}).get('sha256')==physical_def['sha256'],'Map join definition pins differ')
 need(result.get('logical_map_id')==logical.get('operation_map_id') and result.get('physical_source_map_id')==physical.get('operation_map_id') and result.get('changed_binding_rejected')is True and result.get('world_initialized')is False,'Actual logical/physical map validation and negative control required')
 need(preflight.get('status')=='SourceOnlyPreflightPassed'and preflight.get('world_created')is False,'Source package preflight required')
 extra={wrapper_pin['path']:wrapper_pin['sha256'],join_pin['path']:join_pin['sha256'],preflight_pin['path']:preflight_pin['sha256'],physical_preflight_pin['path']:physical_preflight_pin['sha256'],str(graph_path):graph['sha256']}
 merge_pins(extra,join.get('additional_source_pins',{}))
 graph_data,graph_pin=read_json_pinned(graph_path);need(graph_pin['sha256']==graph['sha256'] and isinstance(graph_data.get('source_pins'),dict),'Pinned authored graph source closure required')
 merge_pins(extra,graph_data['source_pins'])
 return preflight,join_pin,extra

def run(repo,build_receipt,bundle,output,max_ticks=None,timeout=1800,data_repo=None,prepare_only=False,diagnostic_preflight=None,s073_source_diagnostic=False,s073_source_host_diagnostic_admission=False,s073_source_transport_diagnostic=False,s073_ui_shell_state_owner_diagnostic=False,s002_historical_input_only=False,s002_current_source_input_package=None,s073_historical_input_diagnostic=False):
 repo=Path(repo).resolve(strict=True);data_repo=Path(data_repo).resolve(strict=True)if data_repo is not None else repo;out=Path(output).absolute();need(math.isfinite(timeout)and timeout>0,'Finite positive owned-process timeout required');need(not out.exists(),'Fresh isolated run output required');out.mkdir(parents=True);receipt,build_pin=read_json_pinned(build_receipt);inputs,bundle_pin=read_json_pinned(bundle);need(receipt.get('status')=='Passed','Qualified supported build required');need(str(repo)==receipt.get('repository_root'),'Run repository differs from built source root')
 if inputs.get('mission')=='S074' and 's074_source_owner_admission' in inputs:
  validator=owned.load_s074_source_owner_receipt_validation(repo/'Tools/CI');validator.preflight_s074_selected_build(inputs,repo,{k:build_pin[k]for k in ('path','sha256')})
 verify(receipt['source_and_reference_pins']);exe=receipt['executable'];need(sha(Path(exe['path']).read_bytes())==exe['sha256'],'Built image changed')
 if receipt.get('runtime_backend')=='LegacyS073OwnerV1':
  need(not prepare_only,'S073 prepare-only is unsupported; no worker will be launched')
  need(not os.environ.get('WARLINE_SOURCE_DIAGNOSTIC_CONFIG')and not os.environ.get('WARLINE_SOURCE_DIAGNOSTIC_CONFIG_SHA256'),'S003 source diagnostic configuration is not supported by the legacy host')
  return _run_s073_legacy_bundle(repo,receipt,build_pin,inputs,bundle_pin,out,timeout,max_ticks,s073_source_diagnostic,s073_source_host_diagnostic_admission,s073_source_transport_diagnostic,data_repo,s073_ui_shell_state_owner_diagnostic,s073_historical_input_diagnostic)
 need(not s073_historical_input_diagnostic,'S073 historical input diagnostic requires S073 legacy owner');need(not s073_source_transport_diagnostic,'S073 source transport diagnostic is valid only for the S073 legacy owner');need(not s002_historical_input_only or inputs.get('mission')=='S002','S002 historical-input diagnostic is valid only for S002');need(not s002_historical_input_only or max_ticks is not None,'S002 historical-input diagnostic requires an explicit bounded --max-ticks');need(s002_current_source_input_package is None or s002_historical_input_only and inputs.get('mission')=='S002' and max_ticks is not None,'Current-source package requires explicit bounded S002 historical-input diagnostic')
 generic_host=owned.require('run_skirmish_source_batch').is_external_build(receipt)
 bound=owned.require('skirmish_source_profiles').derived()
 reusable=bound is not None and receipt.get('s002_derived_capability')=='RuntimePinnedS002DerivedV1' and receipt.get('mission')=='S002'
 if not generic_host and not reusable:require_profile(receipt.get('mission'),receipt.get('seed'))
 tools={k:Path(v).resolve(strict=True)for k,v in receipt['runtime_selection'].items()};need(set(tools)=={'contents','script','engine','mono','newtonsoft'},'Build-recorded runtime paths required');need(str(tools['mono'])in receipt['source_and_reference_pins']and str(tools['newtonsoft'])in receipt['source_and_reference_pins'],'Unbound runtime selection');config,extra=prepare_mono_config(tools,out);need(inputs.get('schema_version')==1 and (generic_host or reusable and inputs.get('mission')=='S002' and inputs.get('seed')==bound[0]['seed'] or(inputs.get('mission')==receipt.get('mission')and inputs.get('seed')==receipt.get('seed'))),f"Explicit matching {receipt.get('mission')}/{receipt.get('seed')} data bundle required")
 initial,initial_pin=read_json_pinned(inputs['initialization']['path']);need(initial_pin['sha256']==inputs['initialization']['sha256'],'Initialization pin mismatch');selection=initial['selection'];owned.require('skirmish_source_profiles').require_initial_qualification_pair(initial);need(initial.get('mission_id')==inputs['mission']and selection.get('seed')==inputs['seed'],'Initialization identity mismatch');require_profile(initial['mission_id'],selection['seed'],selection['difficulty_id'],selection['size_id'])
 current_package=None;current_package_provenance=None;historical_placement_binding=None
 if s002_current_source_input_package is not None:
  import s002_current_source_diagnostic_package as current_package_reader
  current_package=current_package_reader.load_current_s002_package(s002_current_source_input_package['path'],s002_current_source_input_package['sha256'],repo)
  need(current_package['value']['seed']==inputs['seed']==receipt['seed']==104731 and current_package['value']['setup_hash']==3657809016,'Current S002 package/build/bundle target mismatch')
  if s002_historical_input_only:
   need(data_repo==repo,'Current S002 historical package derivation requires the trusted source/data repository to be the same root')
   historical_placement_binding=current_package_reader.load_s002_historical_placement_binding(data_repo)
  initial,current_package_provenance=current_package_reader.derive_current_s002_initialization(initial,inputs['initialization']['path'],initial_pin['sha256'],current_package,historical_placement_binding)
  if historical_placement_binding is not None:
   current_package_provenance=dict(current_package_provenance)
   current_package_provenance['historical_placement_derivation']=current_package_reader.make_s002_historical_placement_derivation(historical_placement_binding)
  selection=initial['selection'];need(selection['seed']==104731,'Derived current-source seed mismatch')
 try:build_provenance=source_build_provenance(receipt)
 except (ValueError,OSError,KeyError,TypeError)as error:build_provenance={'trusted':False,'experimental':False,'canonical':False,'reason':'InvalidBuildProvenance','error':str(error)}
 selected_source_diagnostic=s003_source_diagnostic_selection(receipt,inputs,initial,build_provenance,os.environ)
 selected_source_camera_policy=source_camera_policy_selection(inputs,initial,build_provenance,max_ticks,build_pin)
 source_derived_map=None;source_derived_preflight=None;source_derived_join=None;source_derived_join_pins={}
 if 'source_derived_map' in inputs:
  descriptor=inputs['source_derived_map'];need(isinstance(descriptor,dict)and set(descriptor)=={'path','sha256'}and isinstance(descriptor['path'],str)and Path(descriptor['path']).is_absolute(),'Exact source-derived map descriptor required')
  need(max_ticks is not None and build_provenance.get('trusted')is True and (prepare_only or build_provenance.get('canonical')is False),'Bounded, trusted noncanonical source build required for source-derived map execution')
  source_derived_map,source_derived_sha,source_derived_size=_safe_source_json(descriptor['path']);source_derived_pin={'path':str(Path(descriptor['path']).resolve(strict=True)),'sha256':source_derived_sha,'bytes':source_derived_size};need(source_derived_pin['sha256']==descriptor['sha256'],'Source-derived map descriptor hash changed')
  allow_runtime_alias=_s074_alias_runtime_admitted(repo,initial,inputs,source_derived_map,build_provenance,max_ticks,build_pin,selected_source_camera_policy)
  source_derived_preflight,source_derived_join,source_derived_join_pins=prepare_source_derived_map_join(repo,initial,source_derived_map,source_derived_pin,build_pin,out,prepare_only,allow_runtime_alias)
  need(source_derived_preflight.get('status')=='SourceOnlyPreflightPassed'and source_derived_preflight.get('world_created')is False,'Source-derived map preflight required before world')
 bound=owned.require('skirmish_source_profiles').derived()
 if bound is not None:owned.require('s002_derived_bootstrap').admit_initial(initial,*bound)
 expected_backend=owned.require('skirmish_source_profiles').runtime_backend(inputs['mission'])
 owned.require('run_skirmish_source_batch').require_build_target(receipt,inputs['mission'],inputs['seed'],expected_backend,bound)
 need(inputs.get('runtime_backend', 'LegacyGroundS002V1')==expected_backend,'Bundle/build runtime backend mismatch')
 origin_pins=metadata_origin_pins(initial,repo,receipt)
 if source_derived_map is not None:
  source_map_pins={}
  for row in source_derived_preflight['verified_inputs']:
   need(row['path'] not in source_map_pins or source_map_pins[row['path']]==row['sha256'],'Conflicting source-derived input pins')
   source_map_pins[row['path']]=row['sha256']
  source_map_pins[source_derived_pin['path']]=source_derived_pin['sha256'];merge_pins(origin_pins,source_map_pins)
  merge_pins(origin_pins,source_derived_join_pins)
 merge_pins(origin_pins,layout_replay_input_pins(initial))
 merge_pins(origin_pins,source_resource_binding_input_pins(initial))
 if bound is not None:merge_pins(origin_pins,{bound[1]['path']:bound[1]['sha256']})
 merge_pins(origin_pins,runtime_input_pins(initial,inputs,repo,receipt,data_repo,source_derived_map,source_derived_join,build_pin,selected_source_camera_policy,source_derived_preflight))
 matrix_provenance_before=setup_matrix_provenance(initial,data_repo)
 request={'runtime_backend':expected_backend,'schema_version':1,'kind':'SkirmishSourceSimulationRun','mission':inputs['mission'],'seed':inputs['seed'],'repository_root':str(data_repo),'source_repository_root':str(repo),'output_directory':str(out),'runtime_config':pin(config)}
 if selected_source_camera_policy is not None:request['source_camera_policy']=selected_source_camera_policy
 if source_derived_map is not None:
  request['source_derived_map']={'path':source_derived_pin['path'],'sha256':source_derived_pin['sha256']}
  write(out/'source-derived-map-preflight.json',source_derived_preflight)
  if source_derived_join is not None:request['source_derived_map_logical_join']={key:source_derived_join[key] for key in ('path','sha256')}
 if current_package is not None:
  derived_path=out/'current-source-derived-initialization.json';write(derived_path,initial);derived_pin=pin(derived_path)
  request['initialization']=derived_pin;request['unit_metadata']=current_package['unit_production_metadata'];request['s002_current_source_diagnostic_package']={'path':current_package['path'],'sha256':current_package['sha256']};request['authored_reference_package']=current_package['authored_reference_package']
 historical_descriptor=None
 if s002_historical_input_only:
  if historical_placement_binding is not None:
   historical_descriptor=historical_placement_binding['runtime_descriptor']
   merge_pins(origin_pins,historical_placement_binding['source_pins'])
  else:
   fixed=(('physical','Logs/SkirmishProduction/s073-source-inputs-20261101-r1/campaign-physical-source.json','8407889e4aff5c1f731c429fbae0063c5b6d99e1f7f51ab5c5c5bb0be29fa0b8'),('water','Logs/SkirmishProduction/s073-source-inputs-20261101-r1/campaign-water-capture.json','acb0711c4e7046b70a696ee8fa935eef8a88c00d4dc23929ccb2b7812fcc15f5'),('startup','Logs/SkirmishProduction/s073-source-inputs-20261101-r1/campaign-startup-observation.json','ceed295e16b473fbcba7444c6daa6347b5940c39dd6e80496321cb479c554c03'),('preservation','Logs/SkirmishProduction/s073-captured-source-preservation-20261101-r1/receipt.json','68b2a130fe08e3f952b1a8c1c0603b082ec38c7feb8b4581e1153cba7665e297'))
   historical_descriptor={'kind':'S002HistoricalInputOnlyDiagnostic'}
   for key,relative,digest in fixed:
    path=(data_repo/relative).resolve(strict=True);item=pin(path);need(item['sha256']==digest,'Retained S002 historical input/preservation hash mismatch: '+key);historical_descriptor[key]={'path':str(path),'sha256':digest};merge_pins(origin_pins,{str(path):digest})
  request['s002_historical_input_diagnostic']=historical_descriptor
 camera_keys={'mission_scene','camera_profile','camera_values','camera_motion'}
 # Match.unity remains an actual source-data input in the no-camera diagnostic
 # lane: SourceFreshInitialRoadOwner reads authored road data from this scene.
 # The opt-in only removes camera descriptors; callers cannot override this pin.
 request['mission_scene']=source_mission_scene_pin(data_repo)
 if selected_source_camera_policy is None:
  request['camera_profile']=inputs['camera_profile']if expected_backend=='TypedGroundS026V1'else pin(data_repo/'Assets/Game/Resources/SkirmishMissionViewProfile_S002.asset')
  if expected_backend=='TypedGroundS026V1':need(initial.get('engineering_camera_profile')==request['camera_profile'],'S026 current engineering camera descriptor differs')
 for key in ['initialization','unit_metadata','static_clearance']+([] if selected_source_camera_policy is not None else ['camera_values','camera_motion']):
  if current_package is not None and key in {'initialization','unit_metadata'}:continue
  if key in camera_keys and selected_source_camera_policy is not None:continue
  row=inputs[key];p=Path(row['path']).resolve(strict=True);need(sha(p.read_bytes())==row['sha256'],'Bundle input changed: '+key);request[key]={'path':str(p),'sha256':row['sha256']}
 if inputs['mission']=='S003':
  # S003 historical physical-snapshot bridge only; this is not the common map contract.
  projection,_=read_json_pinned(inputs['static_clearance']['path']);need(projection.get('schema_version')==1 and projection.get('kind')=='GeneratedSourceStaticClearanceProjection' and projection.get('source_sha256')=='5a0993af26be09c8605a883e842ff802c67ae56477b57e9513c2b11b3827b010','Exact S003 clearance projection/source identity required')
  record=projection.get('source_record');need(isinstance(record,dict)and record.get('authority')=='OriginalRenderBoundsPresentation.CollectStaticBounds_SourceBakedDomain' and record.get('projection_source')=='Unity.Rendering.RenderBoundsUpdateSystem.BoundsJob:AABB.Transform(LocalToWorld,RenderBounds)' and record.get('grid_width')==2048 and record.get('grid_height')==1024 and record.get('grid_origin')==[0,0,0] and record.get('cell_size')==1 and record.get('query_rect')==[-118,-118,2284,1260] and record.get('minimum_extent')==.25 and record.get('maximum_extent')==20 and record.get('source_search_radius_cells')==96 and record.get('source_padding_cells')==2 and isinstance(record.get('bounds'),list)and len(record['bounds'])==48457,'S003 captured physical source identity/frame differs')
  source=inputs.get('static_clearance_source_artifact')
  if source is None:source={'path':projection.get('source_path'),'sha256':projection.get('source_sha256'),'bytes':86684375}
  need(isinstance(source,dict)and set(source)=={'path','sha256','bytes'},'Exact S003 clearance source artifact descriptor required')
  need(isinstance(source['path'],str)and Path(source['path']).is_absolute(),'Absolute S003 clearance source artifact path required')
  source_path=Path(source['path']).resolve(strict=True);source_size=source_path.stat().st_size
  need(not Path(source['path']).is_symlink() and source_path.is_file() and type(source['bytes'])is int and source['bytes']==source_size==86684375 and source['sha256']==projection['source_sha256'],'S003 clearance source descriptor/size changed')
  source_raw=source_path.read_bytes();need(sha(source_raw)==source['sha256'],'S003 clearance source bytes/hash changed');source_doc=json.loads(source_raw)
  need(source_doc.get('kind')=='AuthoredSkirmishReferenceData' and source_doc.get('source_scenes')==['Assets/Game/Scenes/Match/MatchRuntimeSubScene.unity','Assets/Game/Scenes/OperationMaps/Skirmish/Candidates/opmap_skirmish_desert_base_01_entity_presentation_dense_city_candidate.unity','Assets/Game/GeneratedOperationMaps/RuntimeBinding/opmap.skirmish.desert_base_01/opmap_skirmish_desert_base_01_runtime.unity'],'S003 canonical map scene identity differs')
  request['static_clearance_source_artifact']={'path':str(source_path),'sha256':source['sha256'],'bytes':source_size}
 if selected_source_diagnostic is not None:request['source_diagnostic_selection']={k:selected_source_diagnostic[k]for k in ('path','sha256','bytes')}
 if max_ticks is not None:need(0<max_ticks<=72000,'Bounded max_ticks required');request['max_ticks']=max_ticks
 write(out/'run-request.json',request);env=os.environ.copy();env['MONO_PATH']=os.pathsep.join([str(repo/'Library/ScriptAssemblies'),str(tools['engine']),str(tools['newtonsoft'].parent),str(Path(receipt['placement_build']).parent)]);pins=dict(receipt['source_and_reference_pins']);merge_pins(pins,owned.pins());merge_pins(pins,extra);merge_pins(pins,{build_pin['path']:build_pin['sha256'],bundle_pin['path']:bundle_pin['sha256'],exe['path']:exe['sha256']});merge_pins(pins,{v['path']:v['sha256']for k,v in request.items()if isinstance(v,dict)and'path'in v});runtime_driver_paths=[repo/'Tools/CI/skirmish_source_build.py',repo/'Tools/CI/run_skirmish_source_simulation.py']+([repo/'Tools/CI/s002_current_source_diagnostic_package.py']if current_package is not None else[])if inputs['mission']=='S049' or s002_historical_input_only else[];runtime_driver_pins={str(p.resolve(strict=True)):sha(p.read_bytes())for p in runtime_driver_paths};
 if inputs['mission']=='S049':need(all(receipt['source_and_reference_pins'].get(p)==h for p,h in runtime_driver_pins.items()),'Build receipt must pin current source runner and CLI')
 merge_pins(pins,runtime_driver_pins);preflight_descriptor=None
 if diagnostic_preflight is not None:
  preflight_path=Path(diagnostic_preflight['path']).resolve(strict=True);preflight_raw=preflight_path.read_bytes();need(sha(preflight_raw)==diagnostic_preflight['sha256'],'S049 diagnostic preflight evidence changed');preflight_doc=json.loads(preflight_raw);preflight_descriptor={'path':str(preflight_path),'sha256':sha(preflight_raw)};merge_pins(pins,{str(preflight_path):sha(preflight_raw)})
  native_markers=preflight_doc.get('native_data',{}).get('s049_diagnostic_markers',{})
  for key in ('native_preflight_log','native_preflight_process','native_preflight_request','native_preflight_input_pins'):
   item=native_markers.get(key)
   if isinstance(item,dict)and isinstance(item.get('path'),str)and isinstance(item.get('sha256'),str):merge_pins(pins,{item['path']:item['sha256']})
  build_desc=preflight_doc.get('build_receipt')
  if isinstance(build_desc,dict)and isinstance(build_desc.get('path'),str)and isinstance(build_desc.get('sha256'),str):merge_pins(pins,{build_desc['path']:build_desc['sha256']})
 if current_package is not None:merge_pins(pins,current_package['source_pins'])
 # Add the large already-validated capture closure last. Every descriptor is
 # still checked on insertion and the complete set is checked before launch.
 merge_pins(pins,origin_pins)
 verify(pins)
 if prepare_only:
  write(out/'input-pins.json',pins);prepared={'status':'PreparedOnly','world_allocated':False,'run_request':pin(out/'run-request.json'),'input_pins':pin(out/'input-pins.json'),'runtime_backend':expected_backend,'build_receipt':build_pin,'data_bundle':bundle_pin,'current_source_package_provenance':current_package_provenance};write(out/'prepared-run.json',prepared);return prepared
 start=time.monotonic();returncode=None;timed_out=False
 with (out/'process.log').open('w')as log:
  process=subprocess.Popen([str(tools['mono']),'--config',str(config),'--optimize=-float32',exe['path'],str(out/'run-request.json')],env=env,stdout=log,stderr=subprocess.STDOUT)
  try:returncode=process.wait(timeout=timeout)
  except subprocess.TimeoutExpired:timed_out=True
  finally:
   if process.poll()is None:
    process.terminate()
    try:process.wait(timeout=5)
    except subprocess.TimeoutExpired:process.kill();process.wait(timeout=5)
  returncode=process.returncode
 verify(pins)
 if current_package is not None:current_package_reader.verify_current_s002_package_inputs(current_package['source_pins'])
 raw=json.loads((out/'result.json').read_text())if(out/'result.json').is_file()else None
 result={'schema_version':1,'kind':'SkirmishSourceSimulationRunReceipt','mission':inputs['mission'],'seed':inputs['seed'],'source_repository_root':str(repo),'data_repository_root':str(data_repo),'process_exit':returncode,'timed_out':timed_out,'wall_seconds':time.monotonic()-start,'build_receipt':build_pin,'data_bundle':bundle_pin,'input_and_runtime_pins':pins,'source_result':raw,'status':'SourceOutcomeObserved'if not timed_out and returncode==0 and raw and raw.get('status')=='SourceTerminal'and raw.get('source_outcome')in {'Victory','Defeat'}else'BoundedPrefix'if not timed_out and returncode==0 and raw and raw.get('status')=='BoundedPrefix'and max_ticks is not None else'Failed','scope':'Source gameplay outcome or explicit bounded prefix; native normal-input/UI/render/result-save/return acceptance is separate. Real wall-duration path budgeting is preserved; fixed seed does not establish deterministic scheduling.'}
 result['repository_data_scope']='CURRENT_REPOSITORY_ASSET_SNAPSHOT_PREWORLD_AND_POSTRUN; not historical asset equality'
 result['runtime_identity_scope']='Common before/after loaded files and MVIDs unchanged; late-only modules listed with post-load hashes, not pre-load equality'
 result.update(full_outcome_observed=result['status']=='SourceOutcomeObserved',expected_outcome='Victory',validation_passed=result['status']=='SourceOutcomeObserved'and raw.get('source_outcome')=='Victory',diagnostic_prefix_completed=result['status']=='BoundedPrefix',native_acceptance=False)
 classify_s049_source_diagnostic(initial,inputs,bundle_pin,result,preflight_descriptor,out/'process.log')
 classify_s002_source_diagnostic(initial,result)
 classify_s026_inactive_diagnostic(initial,result)
 classify_s003_source_diagnostic(initial,result)
 if s002_historical_input_only:
  result.update(diagnostic_only=True,source_diagnostic_classification='S002HistoricalInputOnlyDiagnostic_Unqualified',validation_passed=False,native_acceptance=False,mission_ready=False,source_fit_certified=False,camera_qualified=False,active_mission_water_qualified=False,runtime_presentation_qualified=False,expected_outcome=None,scope='Historical-input-only source-host diagnostic; outcome, current-source equivalence, fit, readiness, native input and victory validation remain unqualified.')
  result['historical_input_classification']=raw.get('historical_input_classification') if isinstance(raw,dict) else None
 if current_package is not None:
  expected_package={'path':current_package['path'],'sha256':current_package['sha256']}
  worker_package=raw.get('s002_current_source_diagnostic_package') if isinstance(raw,dict) else None
  package_bound=worker_package==expected_package
  result.update(diagnostic_only=True,source_diagnostic_classification='S002CurrentOwnerInputsHistoricalPhysicalDiagnostic_Unqualified',validation_passed=False,native_acceptance=False,mission_ready=False,source_fit_certified=False,camera_qualified=False,active_mission_water_qualified=False,runtime_presentation_qualified=False,expected_outcome=None,scope='Current owner metadata plus explicitly historical physical inputs; diagnostic only and never validated victory.',s002_current_source_diagnostic_package=expected_package,s002_current_source_diagnostic_package_bound_by_worker=package_bound,current_source_package_provenance=current_package_provenance)
  if not package_bound:result.update(status='Failed',full_outcome_observed=False,diagnostic_prefix_completed=False,package_binding_failure='Worker result omitted or mismatched exact current-source package descriptor')
 if bound is not None:result=owned.require('s002_derived_bootstrap').classify(result)
 matrix_provenance_after=setup_matrix_provenance(initial,data_repo)
 classify_experimental_source_run(initial,build_provenance,result,stable_setup_matrix_provenance(matrix_provenance_before,matrix_provenance_after))
 if selected_source_camera_policy is not None:
  policy_readback=None
  if (out/'source-camera-policy.json').is_file():
   try:policy_readback=json.loads((out/'source-camera-policy.json').read_text())
   except (OSError,json.JSONDecodeError):policy_readback=None
  death_readback=raw.get('death_readset_session') if isinstance(raw,dict) else None
  worker_policy_ok=(isinstance(raw,dict) and raw.get('source_camera_mode')=='NoCameraLogicalDeliveryDiagnostic' and raw.get('source_camera_absent') is True and raw.get('native_provenance_attached') is False and raw.get('player_readiness') is False and raw.get('full_mission_ready') is False and isinstance(raw.get('delivery_basis_session'),dict) and raw['delivery_basis_session'].get('source_only') is True and raw['delivery_basis_session'].get('mission_ready') is False and isinstance(death_readback,dict) and death_readback.get('kind')=='S074NoCameraDeathReadsetQualification' and death_readback.get('raw_sha256')=='5e1bf6de50b045dd6f5a380a63aa1dcfcfcba2c13d33a3fcf4eb3d94374f3664' and death_readback.get('qualified_prefab_count')==16 and death_readback.get('source_only') is True and death_readback.get('mission_ready') is False)
  policy_file_ok=(isinstance(policy_readback,dict) and policy_readback.get('kind')=='S074NoCameraLogicalDeliveryDiagnostic' and policy_readback.get('source_camera_provider_bound') is False and policy_readback.get('runtime_camera_snapshot_entities')==0 and policy_readback.get('original_death_owner_phase_runs') is True and policy_readback.get('death_readset_qualified') is True and policy_readback.get('death_readset_sha256')=='5e1bf6de50b045dd6f5a380a63aa1dcfcfcba2c13d33a3fcf4eb3d94374f3664' and policy_readback.get('qualified_prefab_count')==16 and policy_readback.get('death_readset_source_only') is True and policy_readback.get('native_provenance_attached') is False and policy_readback.get('native_readiness') is False and policy_readback.get('player_readiness') is False and policy_readback.get('mission_ready') is False)
  result.update(diagnostic_only=True,source_camera_policy=selected_source_camera_policy,source_camera_policy_readback=policy_readback,source_camera_policy_bound=worker_policy_ok and policy_file_ok,source_diagnostic_classification='S074NoCameraLogicalDeliveryDiagnostic',validation_passed=False,full_outcome_observed=False,diagnostic_prefix_completed=result.get('status')=='BoundedPrefix',native_acceptance=False,mission_ready=False,player_readiness=False,native_provenance_attached=False,camera_qualified=False,motion_reference_passed=False,runtime_presentation_qualified=False,source_fit_certified=False,expected_outcome=None,scope='Bounded experimental source-only S074 diagnostic with no numeric camera inputs; original owner semantics run, while camera, native, player and mission readiness remain unqualified.')
  if not worker_policy_ok or not policy_file_ok:result.update(status='Failed',source_camera_policy_bound=False,source_camera_policy_failure='Worker did not confirm the exact no-camera source policy and session logical-basis readback')
  elif result.get('status')=='SourceOutcomeObserved':result['status']='DiagnosticSourceOutcomeObserved'
 if source_derived_map is not None:
  observed=raw.get('source_derived_map') if isinstance(raw,dict) else None
  expected_descriptor={'path':source_derived_pin['path'],'sha256':source_derived_pin['sha256']}
  hashes=source_derived_preflight['source_mask_sha256']
  bound=isinstance(observed,dict)and observed.get('descriptor')==expected_descriptor and observed.get('installed_water_sha256')==source_derived_preflight['source_derived_water_mask_sha256'] and observed.get('installed_buildings_sha256')==hashes['authoredBuildings'] and observed.get('installed_roads_sha256')==hashes['authoredRoadsIncludingSidewalks'] and observed.get('installed_sidewalks_sha256')==hashes['sidewalks'] and observed.get('installed_navigation_count')==source_derived_preflight['navigation_blocker_count'] and observed.get('clearance_descriptor_sha256')==source_derived_map['clearance_descriptor']['sha256'] and isinstance(observed.get('navigation_rows_sha256'),str) and re.fullmatch(r'[0-9a-f]{64}',observed['navigation_rows_sha256']) is not None and observed.get('source_only')is True and observed.get('active_water_qualified')is False and observed.get('source_fit_certified')is False and observed.get('mission_ready')is False
  result.update(source_derived_map=expected_descriptor,source_derived_map_bound=bound,diagnostic_only=True,validation_passed=False,native_acceptance=False,mission_ready=False,source_fit_certified=False,active_mission_water_qualified=False,expected_outcome=None,scope='Source-derived captured map diagnostic; active water, source fit, native and player readiness remain unqualified.')
  if result.get('status')=='SourceOutcomeObserved':result['status']='DiagnosticSourceOutcomeObserved'
  if not bound:result.update(status='Failed',full_outcome_observed=False,diagnostic_prefix_completed=False,source_derived_map_binding_failure='Worker omitted or mismatched installed source-derived map and water digest')
 if selected_source_diagnostic is not None:
  result.update(source_diagnostic_selection=selected_source_diagnostic,diagnostic_only=True,validation_passed=False,native_acceptance=False,mission_ready=False,source_fit_certified=False,camera_qualified=False,active_mission_water_qualified=False,runtime_presentation_qualified=False)
  if result.get('status')=='SourceOutcomeObserved':result['status']='DiagnosticSourceOutcomeObserved'
 result['source_build_provenance']=build_provenance
 missing_manifests=[name for name in ['runtime-before-world.json','runtime-after-outcome.json']if not(out/name).is_file()]
 if missing_manifests:
  result.update(status='Failed',full_outcome_observed=False,validation_passed=False,diagnostic_prefix_completed=False)
  result['runtime_manifest_failure']={'reason':'Actual loaded-runtime manifests missing','missing_files':missing_manifests}
  write(out/'run-receipt.json',result)
  if timed_out or returncode!=0:return result
 need((out/'runtime-before-world.json').is_file()and(out/'runtime-after-outcome.json').is_file(),'Actual loaded-runtime manifests missing');write(out/'run-receipt.json',result);return result
