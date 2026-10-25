"""Finite current-source S002 fallback binding. No World or caller readiness certificate."""
from pathlib import Path
import argparse,hashlib,json,re,subprocess,tempfile,importlib.util
HERE=Path(__file__).resolve().parent
SOURCES=['Assets/Game/Scripts/Configs/OperationMapCatalogConfig.cs','Assets/Game/Scripts/Configs/SkirmishPresetConfig.cs','Assets/Game/Scripts/Composition/SkirmishLaunchProjection.cs','Assets/Game/Scripts/Composition/MatchSceneView.OperationMapLaunch.cs','Assets/Game/Scripts/Composition/MatchSceneView.OperationMapLifecycle.cs','Assets/Game/Scripts/Runtime/Skirmish/SkirmishS004Environment.cs','Assets/Game/Scripts/Composition/MatchSceneView.cs.meta','Assets/Game/Scripts/Configs/SkirmishPresetConfig.cs.meta','Assets/Game/Scripts/Configs/OperationMapCatalogConfig.cs.meta','Assets/Game/Scripts/Configs/OperationMapCatalogEntryConfig.cs','Assets/Game/Scripts/Composition/OperationMapDenseCityCandidateRuntimeOverride.cs']
ASSETS=['Assets/Game/Scenes/Match.unity','Assets/Game/Resources/SkirmishBaseAssault.asset','Assets/Game/Configs/OperationMaps/OperationMapCatalog_Compatibility.asset','Assets/Game/Configs/OperationMaps/OperationMap_Compatibility_DesertBase01.asset']
sha=lambda b:hashlib.sha256(b).hexdigest()
def need(v,why):
 if not v:raise ValueError(why)
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
 template=text(HERE/'helpers/SourceSelectedOperationMap.cs.template');values={'FallbackMissionId':constants['MissionId'],'FallbackScenarioId':constants['ScenarioId'],'FallbackMapId':constants['OperationMapId'],'PresetResource':'SkirmishBaseAssault','PresetPath':ASSETS[1],'PresetSha':assetPins[ASSETS[1]],'CatalogPath':ASSETS[2],'CatalogSha':assetPins[ASSETS[2]],'MapPath':ASSETS[3],'MapSha':assetPins[ASSETS[3]],'MapGuid':mapGuid};decl='\n'.join(' const string '+k+'='+json.dumps(v)+';'for k,v in values.items());pairs=','.join('{'+json.dumps(k)+','+json.dumps(v)+'}'for k,v in assetPins.items());code=template.replace('@@CONSTANTS@@',decl).replace('@@ASSETS@@',pairs).replace('@@NATIVE_RESOLVE@@',native).replace('@@NATIVE_VALIDATE@@',validate);need('@@'not in code,'Unresolved source template');(out/'SourceSelectedOperationMap.cs').write_text(code);(out/'SourceSelectedMapCatalogEntries.cs').write_text(src[SOURCES[9]]);(out/'source-config-graph.json').write_text(json.dumps(base,indent=2,ensure_ascii=False)+'\n')
 if initialization:
  initial=json.loads(read(Path(initialization)));initial['source_config_graph']={'path':str(out/'source-config-graph.json'),'sha256':sha((out/'source-config-graph.json').read_bytes())};initial['selected_operation_map']={'path':str(data/ASSETS[3]),'sha256':assetPins[ASSETS[3]]};(out/'initialization.json').write_text(json.dumps(initial,indent=2)+'\n')
 need(all(sha(Path(p).read_bytes())==v for p,v in pins.items()),'Source/helper drift before output proof');proof={'generator_inputs':pins,'helper_pins':helpers,'native_method':{'path':SOURCES[0],'signature':signature,'body_sha256':sha(native.encode())},'actual_selection':values,'graph_nodes':len(base['nodes']),'scope':'Current actual S002 null preset/environment → Match catalog → native TryResolve. No world/readiness.','outputs':{str(p):sha(p.read_bytes())for p in out.iterdir()if p.is_file()}};(out/'proof.json').write_text(json.dumps(proof,indent=2)+'\n');return proof

def generate_sources(repo,output):
 """Source resolver API. Existing directory allowed; never replaces generated files."""
 repo=Path(repo).resolve();out=Path(output).absolute();out.mkdir(parents=True,exist_ok=True);pins={}
 def raw(p):
  p=p.resolve();b=p.read_bytes();need(len(b)<=4*1024*1024,'Bounded recipe input');v=pins.setdefault(str(p),sha(b));need(v==sha(b),'Recipe input changed');return b
 def text(p):return raw(p).decode('utf-8').replace('\r\n','\n')
 src={p:text(repo/p)for p in SOURCES};helpers={str(p):sha(raw(p))for p in(HERE/'helpers').iterdir()if p.is_file()}
 spec=importlib.util.spec_from_file_location('syntax',HERE/'helpers/source_syntax.py');syntax=importlib.util.module_from_spec(spec);spec.loader.exec_module(syntax);catalog=src[SOURCES[0]];a,b=syntax.method_span(catalog,'public bool TryResolve(');resolve=catalog[a:b];a,b=syntax.method_span(catalog,'public bool TryValidate(');validate=catalog[a:b]
 need(re.search(r'public\s+OperationMapDefinition\s+operationMap\s*;',src[SOURCES[1]])!=None,'Preset null default changed')
 need('var preset = SkirmishPresetConfig.Load(skirmish.ScenarioIndex);'in src[SOURCES[3]]and'SkirmishS004Environment.LoadMapForSession(entityManager) ?? (preset != null ? preset.operationMap : null)'in src[SOURCES[3]],'Preset precedence changed')
 need('SkirmishLaunchProjection.MissionId, SkirmishLaunchProjection.ScenarioId,\n                        SkirmishLaunchProjection.OperationMapId,'in src[SOURCES[3]],'Fallback source call changed')
 need('return IsActive(em) ? Resources.Load<OperationMapDefinition>("SkirmishS004OperationMap") : null;'in src[SOURCES[5]]and'Setup?.CatalogId == "S004"'in src[SOURCES[5]]and'definition.CampaignMap != null)'in src[SOURCES[5]],'Environment source branch changed')
 need('denseCityCandidateRuntimeOverride.TryResolve(\n                ResolveOperationMapCatalog(),'in src[SOURCES[4]]and'if (!productionCatalog.TryValidate(out error))'in src[SOURCES[10]]and'if (!productionCatalog.TryResolve(operationMapId, out definition))'in src[SOURCES[10]],'Catalog source branch changed')
 contract=json.loads(text(HERE/'helpers/source-data-contract.json'));values=dict(contract['selection'])
 for native,field in [('MissionId','FallbackMissionId'),('ScenarioId','FallbackScenarioId'),('OperationMapId','FallbackMapId')]:
  v=re.findall(r'public const string '+native+r'\s*=\s*"([^"\n]+)";',src[SOURCES[2]]);need(len(v)==1,'Unique native launch constant');values[field]=v[0]
 v=re.findall(r'public const string ResourceName\s*=\s*"([^"\n]+)";',src[SOURCES[1]]);need(len(v)==1,'Unique base resource');values['PresetResource']=v[0]
 values.update({'PresetOperationMapJson':json.dumps(contract['preset_operation_map_value'],separators=(',',':')),'CatalogDefinitionsJson':json.dumps(contract['catalog_definitions'],separators=(',',':')),'CatalogEntriesJson':json.dumps(contract['catalog_entries'],separators=(',',':'))})
 decl='\n'.join(' const string '+k+'='+json.dumps(v)+';'for k,v in values.items())+'\n const bool PresetOperationMapPresent='+str(contract['preset_operation_map_present']).lower()+';'
 pairs=','.join('{'+json.dumps(k)+','+json.dumps(v)+'}'for k,v in contract['asset_pins'].items());code=text(HERE/'helpers/SourceSelectedOperationMap.cs.template').replace('@@CONSTANTS@@',decl).replace('@@ASSETS@@',pairs).replace('@@NATIVE_RESOLVE@@',resolve).replace('@@NATIVE_VALIDATE@@',validate);need('@@'not in code,'Unbound template')
 outputs={'SourceSelectedOperationMap.cs':code,'SourceSelectedMapCatalogEntries.cs':src[SOURCES[9]]};need(all(not(out/n).exists()for n in outputs),'Existing generated destination rejected');need(all(sha(Path(p).read_bytes())==v for p,v in pins.items()),'Source/helper drift before emission')
 for name,body in outputs.items():(out/name).write_text(body)
 need(all(sha(Path(p).read_bytes())==v for p,v in pins.items()),'Source/helper drift after emission');proof={'generator_inputs':pins,'helper_pins':helpers,'native_methods':{'TryValidate':sha(validate.encode()),'TryResolve':sha(resolve.encode())},'outputs':{str(out/n):sha((out/n).read_bytes())for n in outputs},'asset_metadata_contract':contract['asset_pins'],'scope':'Two current native catalog methods, original entry structs and finite source-bound selection adapter. No World.'};(out/'source-extraction.json').write_text(json.dumps(proof,indent=2)+'\n');return proof
if __name__=='__main__':
 p=argparse.ArgumentParser();p.add_argument('--repo',required=True);p.add_argument('--data-repo');p.add_argument('--output',required=True);p.add_argument('--graph',required=True);p.add_argument('--initialization');a=p.parse_args();r=generate(a.repo,a.output,a.data_repo,a.graph,a.initialization);print(json.dumps({'status':'Generated','nodes':r['graph_nodes'],'native_method':r['native_method']['body_sha256']}))
