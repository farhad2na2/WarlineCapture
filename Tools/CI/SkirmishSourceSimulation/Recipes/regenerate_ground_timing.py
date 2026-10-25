"""Original catalog timing block/getter over the existing actual metadata resolver."""
from pathlib import Path
import hashlib,json,sys
sys.path.insert(0,str(Path(__file__).resolve().parents[2]))
from skirmish_source_syntax import method_span
SOURCE='Assets/Game/Scripts/Runtime/Skirmish/SkirmishProductionCatalog.cs'
def generate(repo,output):
 p=Path(repo)/SOURCE;raw=p.read_bytes();s=raw.decode('utf-8').replace('\r\n','\n');output=Path(output);output.mkdir(parents=True,exist_ok=True)
 sig='if (timingResolver != null && timingResolver(unit,out var timing) &&'
 if s.count(sig)!=1:raise ValueError('Unique native configured timing block required')
 a,b=method_span(s,sig);block=s[a:b]
 first='            var profile = SkirmishArmyProfileConfig.ResolveCached(setup.ArmyProfileId);'
 last='                    string producerKey = ProducerPrefabKey(overlay.Producer);'
 if s.count(first)!=1 or s.count(last)!=1:raise ValueError('Native catalog role timing scope changed')
 prefix=s[s.index(first):s.index(last)]
 sig2='public bool TryReadProductionDeliverySeconds(SkirmishRoleKind role, out float seconds) =>'
 if s.count(sig2)!=1:raise ValueError('Unique native timing getter required')
 a=s.index(sig2);end=s.index(';',a)+1;getter=s[a:end].replace('public bool TryReadProductionDeliverySeconds','private bool ReadNativeDeliverySeconds',1)
 text='using System;using System.Collections.Generic;using Game.Configs;using Game.Skirmish.Contracts;namespace Game.Runtime{public sealed partial class SkirmishProductionCatalog{private readonly Dictionary<SkirmishRoleKind,float> productionDeliverySeconds=new Dictionary<SkirmishRoleKind,float>();private bool deliveryTimingKnown;'
 text+='internal void BindSourceDeliveryMetadata(IReadOnlyDictionary<string,SourceUnitPrefab> units,BuildingProductionQueueCompositionSystemHelper.TryGetUnitProductionMetadataDelegate timingResolver){if(deliveryTimingKnown||units==null||timingResolver==null)throw new InvalidOperationException("Bind genuine source production timing once");var setup=Setup;var result=this;'+prefix
 text+='if(!units.TryGetValue(unitKey,out var unit))throw new NotSupportedException("Original configured role prefab missing: "+unitKey);'+block+'}deliveryTimingKnown=true;}'+getter
 text+='public bool TryReadProductionDeliverySeconds(SkirmishRoleKind role,out float seconds){if(!deliveryTimingKnown)throw new NotSupportedException("Actual configured production delivery timing not bound");return ReadNativeDeliverySeconds(role,out seconds);}public static string ProducerPrefabKey(SkirmishProducerKind producer)=>GeneratedProductionRecipeSource.ProducerPrefabKey(producer);}}'
 if p.read_bytes()!=raw:raise ValueError('Native timing source drift')
 with(output/'NativeProductionTiming.cs').open('w',encoding='utf-8',newline='')as f:f.write(text)
 proof={'source_inputs':{str(p):hashlib.sha256(raw).hexdigest()},'original_role_scope_sha256':hashlib.sha256(prefix.encode()).hexdigest(),'original_timing_block_sha256':hashlib.sha256(block.encode()).hexdigest(),'bindings':['Original current role filter/unit key/timing formula/getter; opaque source prefab type +canonical actual configured map/resolver only','Unknown unbound timing throws; native configured unavailable/invalid timing remains original false','Producer key forwards existing exact current-source helper; no prices/durations/camera inferred']};(output/'source-extraction.json').write_text(json.dumps(proof,indent=2));return proof
