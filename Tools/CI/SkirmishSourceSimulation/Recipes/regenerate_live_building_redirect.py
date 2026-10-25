"""Complete current original redirect owner; finite logical query snapshot binding only."""
from pathlib import Path
import hashlib,json
SOURCE='Assets/Game/Scripts/Systems/BuildingPlacementRedirectCompositionSystemHelper.cs'
QUERY='Assets/Game/Scripts/Systems/BuildingGameplayEcsQueryCompositionSystemHelper.cs'
def generate(repo,output):
 repo=Path(repo);output=Path(output);output.mkdir(parents=True,exist_ok=True);raw=(repo/SOURCE).read_bytes();query=(repo/QUERY).read_bytes();text=raw.decode('utf8').replace('\r\n','\n');q=query.decode('utf8').replace('\r\n','\n')
 if q.count('_redirectQuery = em.CreateEntityQuery(R<UnitMove>(), R<UnitGrid>(), R<LocalTransform>());')!=1:raise ValueError('Original redirect query shape changed')
 a=text.index('            EntityTypeHandle entityType = em.GetEntityTypeHandle();');b=text.index('\n            try',a);old=text[a:b]
 if old.count('redirectUnitsQuery.ToArchetypeChunkArray(Allocator.Temp)')!=1 or old.count('units.AddRange(entities);')!=1 or 'chunks[chunkIndex].GetNativeArray(entityType)'not in old:raise ValueError('Original chunk/entity collection shape changed')
 text=text[:a]+'            using NativeArray<Entity> units = redirectUnitsQuery.ToEntityArray(Allocator.Temp);\n'+text[b:]
 if (repo/SOURCE).read_bytes()!=raw or(repo/QUERY).read_bytes()!=query:raise ValueError('Current redirect native source drift')
 (output/'BuildingPlacementRedirectCompositionSystemHelper.cs').write_text(text)
 proof={'source_inputs':{str(repo/SOURCE):hashlib.sha256(raw).hexdigest(),str(repo/QUERY):hashlib.sha256(query).hexdigest()},'original_entity_collection_sha256':hashlib.sha256(old.encode()).hexdigest(),'bindings':['Complete current native owner and every redirect/perimeter/path/command body retained; only archetype chunk entity flattening replaced with one allocated finite host query snapshot.','Exact original query UnitMove+UnitGrid+LocalTransform, default enabled/prefab filtering. Actual host query ordering retained; no native archetype iteration parity claim.','No no-op redirect callback, new engine API or generic framework.']};(output/'source-extraction.json').write_text(json.dumps(proof,indent=2));return proof
