from pathlib import Path
import hashlib,json,importlib.util
PATHS={'bootstrap':'Assets/Game/Scripts/Composition/OperationMapRuntimeBootstrapSceneSystemHelper.cs','root':'Assets/Game/Scripts/Composition/OperationMapRuntimeRootContract.cs','readiness':'Assets/Game/Scripts/Composition/OperationMapRuntimeReadinessProjection.cs','reuse':'Assets/Game/Scripts/Composition/CampaignMissionOperationMapReuseUtility.cs','initial':'Assets/Game/Scripts/Composition/MatchSceneView.OperationMapLifecycle.cs','campaign_dtos':'Assets/Game/Scripts/Components/CampaignMissionComponents.cs'}
def generate(repo,output):
 repo=Path(repo);out=Path(output);out.mkdir(parents=True,exist_ok=True);raw={k:(repo/v).read_bytes()for k,v in PATHS.items()};texts={k:b.decode().replace('\r\n','\n')for k,b in raw.items()}
 import types
 helper=Path(__file__).with_name('regenerate_config_metadata.py');helper_bytes=helper.read_bytes();lex=types.ModuleType('root_source_lexical');lex.__file__=str(helper);exec(compile(helper_bytes,str(helper),'exec'),lex.__dict__)
 def exact(text,old,new,count=1):
  if text.count(old)!=count:raise ValueError('Unsupported native root binding: '+old)
  return text.replace(old,new,count)
 boot=texts['bootstrap'];mask=lex.mask(boot);marker='        public bool TryPublish(\n            OperationMapCatalogConfig catalog,';start=boot.find(marker)
 if start<0 or boot.count(marker)!=1:raise ValueError('Unique native catalog overload required')
 begin=mask.find('{',start);end=begin+1;depth=1
 while depth:
  if mask[end]=='{':depth+=1
  elif mask[end]=='}':depth-=1
  end+=1
 omitted=boot[start:end];boot=boot[:start]+boot[end:]
 boot=exact(boot,'private readonly World createdWorld;','private readonly World createdWorld;\n        private readonly EntityManager sourceEntityManager;\n        private readonly SourceOperationMapRotationReference rotationReference;')
 boot=exact(boot,'OperationMapRuntimeBootstrapSceneSystemHelper(World world)','OperationMapRuntimeBootstrapSceneSystemHelper(EntityManager manager, SourceOperationMapRotationReference reference)')
 boot=exact(boot,'createdWorld = world;','sourceEntityManager = manager;\n            createdWorld = manager?.World;\n            rotationReference = reference;')
 boot=exact(boot,'definition.TryCreatePersistentMetadataBlob(\n                    out BlobAssetReference','definition.TryCreatePersistentMetadataBlob(\n                    rotationReference,\n                    out BlobAssetReference')
 boot=exact(boot,'ToFloat3(UnityEngine.Vector3 value)','ToFloat3(ActualCore::UnityEngine.Vector3 value)')
 boot=exact(boot,'entityManager = createdWorld.EntityManager;','entityManager = sourceEntityManager;')
 boot='extern alias ActualCore;\n'+boot
 root=exact(texts['root'],'where T : unmanaged, IComponentData','where T : struct, IComponentData')
 reuse=lex.member(texts['reuse'],'TryReuse');cut=reuse.find('\n            Entity mapRoot = mapQuery.GetSingletonEntity();')
 if cut<0 or reuse[:cut].count('CreateEntityQuery(')!=2 or reuse[:cut].count('if (mapQuery.CalculateEntityCount() != 1)\n                return false;')!=1:raise ValueError('Unsupported native initial reuse prefix')
 prefix=reuse[:cut];reuse=prefix+'\n            throw new System.NotSupportedException("Existing operation-map root reuse/launch continuation is outside the fresh initial-root profile.");\n        }'
 reuse='using Game.Components;using Game.Configs;using Unity.Entities;namespace Game.Composition{internal static class CampaignMissionOperationMapReuseUtility{\n'+reuse+'\n}}'
 initial=lex.member(texts['initial'],'ResolveInitialOperationMapReadiness');initial='using Game.Components;namespace Game.Composition{internal static class SourceOperationMapInitialReadiness{\n'+initial+'\n}}'
 dto=texts['campaign_dtos'];marker='    public struct CampaignMissionLaunchRequestElement : IBufferElementData';start=dto.index(marker);masked=lex.mask(dto);begin=masked.index('{',start);end=masked.index('}',begin)+1;dto='using Game.Missions.Contracts;using Game.Narrative.Contracts;using Unity.Collections;using Unity.Entities;namespace Game.Components{[InternalBufferCapacity(2)]\n'+dto[start:end]+'\n}'
 files={'SourceOperationMapRuntimeBootstrapSceneSystemHelper.cs':boot,'SourceOperationMapRuntimeRootContract.cs':root,'SourceOperationMapRuntimeReadinessProjection.cs':texts['readiness'],'SourceInitialOperationMapReuse.cs':reuse,'SourceOperationMapInitialReadiness.cs':initial,'SourceCampaignMissionLaunchRequestElement.cs':dto}
 if any((repo/PATHS[k]).read_bytes()!=b for k,b in raw.items()):raise ValueError('Root native input drift before emission')
 for name,value in files.items():(out/name).write_text(value)
 proof={'kind':'CurrentOriginalFreshOperationMapRootBootstrap','source_pins':{str(repo/PATHS[k]):hashlib.sha256(b).hexdigest()for k,b in raw.items()},'outputs':{k:hashlib.sha256(v.encode()).hexdigest()for k,v in files.items()},'bindings':['Original World field bound to SAME source manager.World; one EntityManager property read bound to same readonly manager, no new World/ECS API','Original factory explicit finite native reference argument','Existing RootContract storage unmanaged→struct constraint, no body policy changes','Exact original reuse prefix and BOTH queries; count!=1 originalfalse, count==1 consumed continuation explicit unsupported','Original metadata-only readiness resolver, no scene-ready invention'],'reuse_prefix_sha256':hashlib.sha256(prefix.encode()).hexdigest(),'omitted_catalog_overload_sha256':hashlib.sha256(omitted.encode()).hexdigest(),'scope':'Fresh initial metadata-root producer only. Existing-root launch/reuse continuation and catalog overload unsupported. No native root absence/world admission.'};(out/'source-extraction.json').write_text(json.dumps(proof,indent=2)+'\n')
 if any((repo/PATHS[k]).read_bytes()!=b for k,b in raw.items()) or helper.read_bytes()!=helper_bytes:raise ValueError('Root native/helper input drift after emission')
 return proof
if __name__=='__main__':generate(str(Path(__file__).resolve().parent/'native-source'),Path(__file__).resolve().parent/'generated')
