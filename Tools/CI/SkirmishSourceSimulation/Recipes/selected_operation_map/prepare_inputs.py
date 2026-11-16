"""Data-only current S002 map-input preparation. No World or runtime admission."""
from pathlib import Path
import argparse,hashlib,json,re,subprocess,tempfile,importlib.util,types
HERE=Path(__file__).resolve().parent
_module_path=HERE/'generate_current.py'
_module_raw=_module_path.read_bytes()
_source=types.ModuleType('qualified_selected_source');_source.__file__=str(_module_path)
exec(compile(_module_raw,str(_module_path),'exec'),_source.__dict__)
SOURCES,ASSETS,sha,need,generate_sources=_source.SOURCES,_source.ASSETS,_source.sha,_source.need,_source.generate_sources

def generate(repo,output,data_repo=None,graph=None,initialization=None):
 repo=Path(repo).resolve();data=Path(data_repo or repo).resolve();out=Path(output).absolute();need(not out.exists(),'Fresh output required');out.mkdir(parents=True);pins={}
 def read(p):
  p=p.resolve();raw=p.read_bytes();need(len(raw)<=4*1024*1024,'Bounded source input');prior=pins.setdefault(str(p),sha(raw));need(prior==sha(raw),'Input changed');return raw
 def text(p):return read(p).decode('utf-8').replace('\r\n','\n')
 src={p:text(repo/p)for p in SOURCES};helpers={str(p):sha(read(p))for p in(HERE/'helpers').iterdir()if p.is_file()};
 spec=importlib.util.spec_from_file_location('syntax',HERE/'helpers/source_syntax.py');syntax=importlib.util.module_from_spec(spec);spec.loader.exec_module(syntax)
 signature='public bool TryResolve(';s=src[SOURCES[0]];need(s.count(signature)==1,'Unique original TryResolve');a,b=syntax.method_span(s,signature);native=s[a:b];va,vb=syntax.method_span(s,'public bool TryValidate(');validate=s[va:vb]
 presetSource=src[SOURCES[1]];need(re.search(r'public\s+OperationMapDefinition\s+operationMap\s*;',presetSource)!=None,'Native preset null default changed');need('SourcePresetResourceName('in presetSource,'Shared preset selector required')
 launch=src[SOURCES[3]];need('var preset = SkirmishPresetConfig.Load(skirmish.ScenarioIndex);'in launch and 'SkirmishS004Environment.LoadMapForSession(entityManager) ?? (preset != null ? preset.operationMap : null)'in launch,'Original preset precedence changed');need('SkirmishLaunchProjection.MissionId, SkirmishLaunchProjection.ScenarioId,\n                        SkirmishLaunchProjection.OperationMapId,'in launch,'Original fallback call changed')
 env=src[SOURCES[5]];need('return IsActive(em) ? Resources.Load<OperationMapDefinition>("SkirmishS004OperationMap") : null;'in env and 'Setup?.CatalogId == "S004"'in env and 'definition.CampaignMap != null)'in env,'Original environment branch changed');need('denseCityCandidateRuntimeOverride.TryResolve(\n                ResolveOperationMapCatalog(),'in src[SOURCES[4]],'Original scene catalog resolver changed');need('if (!productionCatalog.TryValidate(out error))'in src[SOURCES[10]]and'if (!productionCatalog.TryResolve(operationMapId, out definition))'in src[SOURCES[10]],'Original catalog validation/resolve order changed')
 constants={}
 for name in['MissionId','ScenarioId','OperationMapId']:
  matches=re.findall(r'public const string '+name+r'\s*=\s*"([^"\n]+)";',src[SOURCES[2]]);need(len(matches)==1,'Unique native launch constant');constants[name]=matches[0]
 def guid(meta):
  rows=re.findall(r'^guid: ([0-9a-f]{32})$',text(meta),re.M);need(len(rows)==1,'Unique GUID');return rows[0]
 def parse(paths):
  r=subprocess.run(['ruby',str(HERE/'helpers/read_source_configs.rb'),*[str(p)for p in paths]],capture_output=True,text=True,timeout=20);need(r.returncode==0,'Existing source YAML reader failed: '+r.stderr);return json.loads(r.stdout)
 rawAssets={p:read(data/p)for p in ASSETS};assetPins={p:sha(v)for p,v in rawAssets.items()}
 for p in ASSETS:
  meta=data/(p+'.meta');assetPins[p+'.meta']=sha(read(meta))
 # Select one Unity document; all YAML interpretation remains in the existing safe reader.
 scene=rawAssets[ASSETS[0]].decode();headers=list(re.finditer(r'^--- !u!\d+ &-?\d+.*$',scene,re.M));docs=[scene[m.start():headers[i+1].start()if i+1<len(headers)else len(scene)]for i,m in enumerate(headers)];chosen=[d for d in docs if re.search(r'^  operationMapCatalog:',d,re.M)];need(len(chosen)==1,'Unique actual scene catalog owner')
 with tempfile.TemporaryDirectory()as temp:
  doc=Path(temp)/'match-component.asset';doc.write_text(chosen[0]);sceneRow=parse([doc])[str(doc)]['MonoBehaviour']
 need(sceneRow['m_Script']=={'fileID':11500000,'guid':guid(repo/SOURCES[6]),'type':3},'Actual MatchSceneView component changed')
 parsed=parse([data/ASSETS[1],data/ASSETS[2]])
 preset=parsed[str(data/ASSETS[1])]['MonoBehaviour'];catalog=parsed[str(data/ASSETS[2])]['MonoBehaviour'];need(preset.get('operationMap')in[None,{'fileID':0}],'Actual preset supplies another map');need(preset['m_Script']=={'fileID':11500000,'guid':guid(repo/SOURCES[7]),'type':3},'Actual preset script changed')
 catalogGuid=guid(data/(ASSETS[2]+'.meta'));mapGuid=guid(data/(ASSETS[3]+'.meta'));need(sceneRow['operationMapCatalog']=={'fileID':11400000,'guid':catalogGuid,'type':2},'Actual scene catalog reference changed');need(catalog['m_Script']=={'fileID':11500000,'guid':guid(repo/SOURCES[8]),'type':3},'Actual catalog script changed');need(catalog['definitions']==[{'fileID':11400000,'guid':mapGuid,'type':2}],'Current finite catalog source domain changed')
 # Reuse the source graph producer for the typed OperationMapDefinition, including exact scalar spellings.
 need(graph is not None,'Existing actual source graph required');base=json.loads(read(Path(graph)));need(len(base['nodes'])<=60,'Config graph bound');legacyPath=data/ASSETS[3];legacy=parse([legacyPath])[str(legacyPath)];
 def node(path,row,typ):
  mb=row['MonoBehaviour'];return {'asset_guid':guid(data/(path+'.meta')),'source_type':typ,'source_path':path,'source_sha256':assetPins[path],'fields':{k:v for k,v in mb.items()if not k.startswith('m_')},'scalar_spellings':{k[len('MonoBehaviour.'):]:v for k,v in row['__source_scalar_spellings'].items()if k.startswith('MonoBehaviour.')}}
 additions=[node(ASSETS[1],parsed[str(data/ASSETS[1])],'SkirmishPresetConfig'),node(ASSETS[2],parsed[str(data/ASSETS[2])],'OperationMapCatalogConfig'),node(ASSETS[3],legacy,'OperationMapDefinition')]
 nodes={n['source_path']:n for n in base['nodes']}
 for n in additions:need(n['source_path']not in nodes or nodes[n['source_path']]==n,'Graph node drift');nodes[n['source_path']]=n
 base['nodes']=list(nodes.values());need(len(base['nodes'])<=64,'Config graph bound')
 for rel,digest in assetPins.items():base['source_pins'][str(data/rel)]=digest
 contract=json.loads(text(HERE/'helpers/source-data-contract.json'));need(assetPins==contract['asset_pins'],'Current assets differ from reviewed finite data contract; refresh its safe-parser transcript explicitly');generate_sources(repo,out);(out/'source-config-graph.json').write_text(json.dumps(base,indent=2,ensure_ascii=False)+'\n')
 if initialization:
  initial=json.loads(read(Path(initialization)));initial['source_config_graph']={'path':str(out/'source-config-graph.json'),'sha256':sha((out/'source-config-graph.json').read_bytes())};initial['selected_operation_map']={'path':str(data/ASSETS[3]),'sha256':assetPins[ASSETS[3]]};(out/'initialization.json').write_text(json.dumps(initial,indent=2)+'\n')
 need(all(sha(Path(p).read_bytes())==v for p,v in pins.items()),'Source/helper drift before output proof');proof={'generator_inputs':pins,'helper_pins':helpers,'native_method':{'path':SOURCES[0],'signature':signature,'body_sha256':sha(native.encode())},'actual_selection':contract['selection'],'graph_nodes':len(base['nodes']),'scope':'Current actual S002 null preset/environment → Match catalog → native TryResolve. No world/readiness.','outputs':{str(p):sha(p.read_bytes())for p in out.iterdir()if p.is_file()}};(out/'proof.json').write_text(json.dumps(proof,indent=2)+'\n');return proof

def main():
 p=argparse.ArgumentParser(description=__doc__)
 for name in ['repo','data-repo','output','graph','graph-sha','initialization','initialization-sha','bundle','bundle-sha','rotation-reference','rotation-reference-sha','building-request-metadata','building-request-metadata-sha']:p.add_argument('--'+name,required=True)
 a=p.parse_args();pins={str(_module_path):sha(_module_raw),str(Path(__file__).resolve()):sha(Path(__file__).read_bytes())}
 def pin(path,digest):
  path=Path(path).resolve();raw=path.read_bytes();need(sha(raw)==digest,'Explicit input hash differs: '+str(path));pins[str(path)]=digest;return json.loads(raw)
 graph=pin(a.graph,a.graph_sha);initial=pin(a.initialization,a.initialization_sha);bundle=pin(a.bundle,a.bundle_sha);rotation=pin(a.rotation_reference,a.rotation_reference_sha);buildings=pin(a.building_request_metadata,a.building_request_metadata_sha)
 need(initial.get('canonical_battle_catalog') is not None,'Canonical battle catalog descriptor required')
 need(initial.get('source_config_graph')=={'path':str(Path(a.graph).resolve()),'sha256':a.graph_sha},'Initialization must refer to supplied source graph')
 initial_descriptor=bundle.get('initialization',{});need(set(initial_descriptor)<= {'path','sha256','bytes'} and initial_descriptor.get('path')==str(Path(a.initialization).resolve()) and initial_descriptor.get('sha256')==a.initialization_sha and ('bytes'not in initial_descriptor or initial_descriptor['bytes']==Path(a.initialization).stat().st_size),'Bundle must refer to the supplied initial input')
 need(buildings.get('kind')=='CurrentSourceBuildingRequestMetadataSupplement','Qualified configured-building supplement required')
 proof=generate(a.repo,a.output,a.data_repo,a.graph,a.initialization);out=Path(a.output).absolute()
 candidate=types.ModuleType('source_verify');candidate.__file__=str(_module_path);exec(compile(_module_raw,str(_module_path),'exec'),candidate.__dict__)
 with tempfile.TemporaryDirectory(prefix='selected-map-source-verify-')as tmp:
  candidate.generate_sources(a.repo,tmp)
  for name in ['SourceSelectedOperationMap.cs','SourceSelectedMapCatalogEntries.cs']:
   need((out/name).read_bytes()==(Path(tmp)/name).read_bytes(),'Data preparation changed qualified source output: '+name)
 prepared=json.loads((out/'initialization.json').read_text());prepared['operation_map_rotation_reference']={'path':str(Path(a.rotation_reference).resolve()),'sha256':a.rotation_reference_sha};prepared['building_request_metadata']={'path':str(Path(a.building_request_metadata).resolve()),'sha256':a.building_request_metadata_sha}
 for key,value in initial.items():
  if key not in ['source_config_graph','selected_operation_map','operation_map_rotation_reference','building_request_metadata']:need(prepared.get(key)==value,'Existing initialization descriptor changed: '+key)
 (out/'initialization.json').write_text(json.dumps(prepared,indent=2)+'\n');newbundle=dict(bundle);newbundle['initialization']={'path':str(out/'initialization.json'),'sha256':sha((out/'initialization.json').read_bytes())};
 if 'bytes'in initial_descriptor:newbundle['initialization']['bytes']=(out/'initialization.json').stat().st_size
 (out/'data-bundle.json').write_text(json.dumps(newbundle,indent=2)+'\n')
 need(all(sha(Path(path).read_bytes())==digest for path,digest in pins.items()),'Explicit preparation input/helper drift')
 proof['explicit_input_pins']=pins;proof['generated_source_hashes']={name:sha((out/name).read_bytes())for name in ['SourceSelectedOperationMap.cs','SourceSelectedMapCatalogEntries.cs']};proof['prepared_outputs']={name:sha((out/name).read_bytes())for name in ['source-config-graph.json','initialization.json','data-bundle.json']};proof['runtime_or_world_started']=False;(out/'preparation-proof.json').write_text(json.dumps(proof,indent=2)+'\n');print(json.dumps({'status':'PreparedDataOnly','nodes':len(json.loads((out/'source-config-graph.json').read_text())['nodes']),'world_started':False}));return 0
if __name__=='__main__':raise SystemExit(main())
