"""Current original configured-building selection and boundary publication over typed data."""
from pathlib import Path
import hashlib,json,sys
sys.path.insert(0,str(Path(__file__).resolve().parents[2]))
from skirmish_source_syntax import method_span
INPUTS=['Assets/Game/Scripts/Runtime/Skirmish/SkirmishProductionCatalog.cs','Assets/Game/Scripts/Systems/BuildingRuntimeProcessingCompositionSystemHelper.cs','Assets/Game/Scripts/Systems/BuildingRuntimeBoundaryCommands.cs','Assets/Game/Scripts/Runtime/Skirmish/SkirmishEnemyConstructionAdapter.cs','Assets/Game/Scripts/Systems/BuildingRuntimePublishCompositionSystemHelper.cs','Assets/Game/Scripts/Systems/BuildingPlacementRuntimeTickCompositionSystemHelper.cs']
def generate(repo,output):
 repo=Path(repo);output=Path(output);output.mkdir(parents=True,exist_ok=True);raw={x:(repo/x).read_bytes() for x in INPUTS};text={x:v.decode('utf8').replace('\r\n','\n')for x,v in raw.items()};proof={}
 def member(path,signature):
  s=text[path]
  if s.count(signature)!=1:raise ValueError('Unique native member required: '+signature)
  a,b=method_span(s,signature);v=s[a:b];proof[signature]=hashlib.sha256(v.encode()).hexdigest();return v
 if '(float)em.World.Time.ElapsedTime,' not in text[INPUTS[4]]:raise ValueError('Native boundary clock authority changed')
 tick=text[INPUTS[5]];start=tick.index('public void UpdateSimulation(')if 'public void UpdateSimulation('in tick else tick.index('public void Update(')
 body=tick[start:]
 if body.index('context.UpdateBuildingRuntimeState?.Invoke();')>body.index('BuildingProductionCadenceKernel.BeginPending'):raise ValueError('Native boundary-before-pending order changed')
 catalog=text[INPUTS[0]]
 first='                var availableBuildings = new Dictionary<string, GameObject>(StringComparer.Ordinal);'
 last='                var availableUnits = new Dictionary<string, GameObject>(StringComparer.Ordinal);'
 if catalog.count(first)!=1 or catalog.count(last)!=1:raise ValueError('Native configured building selection shape changed')
 selection=catalog[catalog.index(first):catalog.index(last)]
 start='                var profile = SkirmishArmyProfileConfig.ResolveCached(setup.ArmyProfileId);'
 end='                    result.registry.UnitSpawnPrefabs.Add(unit);'
 if catalog.count(start)!=1 or catalog.count(end)!=1:raise ValueError('Native producer membership shape changed')
 producers=catalog[catalog.index(start):catalog.index(end)]
 selection=selection.replace('GameObject','SourceUnitPrefab');producers=producers.replace('ProducerPrefabKey(overlay.Producer)','GeneratedProductionRecipeSource.ProducerPrefabKey(overlay.Producer)')
 head='using System;using System.Collections.Generic;using Game.Configs;using Game.Skirmish.Contracts;namespace Game.Runtime{internal static class SourceConfiguredBuildingSelection{internal static List<SourceUnitPrefab> Select(SkirmishResolvedSetup setup,SourceBuildingProvider baseline,SourceBuildingProvider scene,IReadOnlyDictionary<string,SourceUnitPrefab> availableUnits){if(setup.CatalogId=="S004")throw new NotSupportedException("S004 environment building contribution not admitted");var buildings=new List<SourceUnitPrefab>(baseline.Spawnables);'
 result=head+selection+producers+'}return buildings;}}internal sealed class SourceBuildingProvider{internal readonly IReadOnlyList<SourceUnitPrefab> Spawnables;internal SourceBuildingProvider(IReadOnlyList<SourceUnitPrefab> values){Spawnables=values??throw new ArgumentNullException(nameof(values));}}}'
 (output/'NativeConfiguredBuildingSelection.cs').write_text(result)
 publisher=member(INPUTS[1],'internal void PublishConfiguredSpawnablesReadModel(BuildingDefinitionPrefabSystemHelper definitionSystem, EntityManager em, Entity boundaryEntity)').replace('BuildingDefinitionPrefabSystemHelper definitionSystem','GeneratedConfiguredDefinitionSource definitionSystem').replace('out BuildingDefinition definition','out RuntimeDefinition definition')
 boundary=member(INPUTS[1],'private FixedString128Bytes ResolveBoundaryId(GameObject prefab, string fallback)').replace('GameObject prefab','SourceUnitPrefab prefab')
 convert=member(INPUTS[2],'public static FixedString128Bytes ToFixedString128(string value) =>') if False else 'private static FixedString128Bytes ToFixedString128(string value)=>new FixedString128Bytes(value ?? string.Empty);'
 if 'new(value ?? string.Empty);' not in text[INPUTS[2]]:raise ValueError('Original boundary fixed string conversion changed')
 prefix='using System;using System.Collections.Generic;using UnityEngine;using Game.Components;using Unity.Collections;using Unity.Entities;using Unity.Mathematics;namespace Game.Runtime{internal sealed partial class GeneratedRuntimeSpawnProcessor{readonly Dictionary<SourceUnitPrefab,FixedString128Bytes> _boundaryIdsByPrefab=new Dictionary<SourceUnitPrefab,FixedString128Bytes>();readonly Dictionary<string,FixedString128Bytes> _boundaryIdsByFallback=new Dictionary<string,FixedString128Bytes>();'
 (output/'NativeConfiguredBuildingPublisher.cs').write_text(prefix+publisher+boundary+convert+'}}')
 full=text[INPUTS[3]];sig='static bool TryConfigured(EntityManager em,string key,out BuildingConfiguredSpawnableReadModel row)';a,b=method_span(full,sig);part=full[a:b];brace=part.index('{');part=part[:brace+1]+'SourceConfiguredBuildingAdmission.Require(em);'+part[brace+1:];full=full[:a]+part+full[b:]
 (output/'SkirmishEnemyConstructionAdapter.cs').write_text(full)
 for x,v in raw.items():
  if (repo/x).read_bytes()!=v:raise ValueError('Native source changed during generation: '+x)
 record={'source_inputs':{str(repo/x):hashlib.sha256(v).hexdigest()for x,v in raw.items()},'original_members':proof,'selection_sha256':hashlib.sha256(selection.encode()).hexdigest(),'producer_membership_sha256':hashlib.sha256(producers.encode()).hexdigest(),'bindings':['Current source configured selection/producer order over explicitly witnessed relevant provider data; S004 excluded.','Current full publisher body: definition/prefab token types only. Original ID normalization/cache/string conversion preserved.','Current whole construction adapter; single host data-admission precondition at TryConfigured before original reads. No unavailable false for unknown data.']};(output/'source-extraction.json').write_text(json.dumps(record,indent=2));return record
