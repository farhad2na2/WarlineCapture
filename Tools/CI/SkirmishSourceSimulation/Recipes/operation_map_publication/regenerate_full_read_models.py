from pathlib import Path
import hashlib, json, re, types

INPUTS = {
    'owner': 'Assets/Game/Scripts/Systems/BuildingRuntimeProcessingCompositionSystemHelper.cs',
    'query': 'Assets/Game/Scripts/Systems/BuildingRuntimeReadModelCompositionSystemHelper.cs',
    'overlay': 'Assets/Game/Scripts/Systems/BuildingRuntimeSurfaceOverlaySystem.cs',
    'definition': 'Assets/Game/Scripts/Systems/BuildingDefinitionPrefabSystemHelper.cs',
    'runway': 'Assets/Game/Scripts/Systems/OperationMapRunwayReadModelUtility.cs',
    'signature': 'Assets/Game/Scripts/Systems/BuildingRuntimeSignatureUtility.cs',
}


def generate(repo, output):
    repo, output = Path(repo), Path(output)
    raw = {key: (repo / path).read_bytes() for key, path in INPUTS.items()}
    text = {key: value.decode().replace('\r\n', '\n') for key, value in raw.items()}
    helper = Path(__file__).with_name('regenerate_config_metadata.py')
    helper_raw = helper.read_bytes()
    lex = types.ModuleType('current_read_model_lexical')
    lex.__file__ = str(helper)
    exec(compile(helper_raw, str(helper), 'exec'), lex.__dict__)
    source = text['owner']
    names = set(re.findall(r'^\s*(?:private|internal|public)\s+(?:(?:static|readonly|override|sealed|async)\s+)*[\w<>\[\],.?]+\s+(\w+)\s*\(', lex.mask(source), re.M))
    selected = {'PublishReadModelIfDue'}
    while True:
        old = set(selected)
        for name in list(selected):
            body = lex.mask(lex.member(source, name))
            selected |= {other for other in names if re.search(r'(?<![.\w])' + re.escape(other) + r'\s*\(', body)}
        if selected == old:
            break
    methods = {name: lex.member(source, name) for name in sorted(selected)}
    # These two CURRENT native methods already belong to this same generated owner.
    # Their existing recipe supplies them. Do not duplicate their caches or bodies.
    supplied = {'PublishConfiguredSpawnablesReadModel', 'ResolveBoundaryId'}
    if not supplied <= selected:
        raise ValueError('Existing configured publisher/source identity methods changed')
    point = 'building.Instance.transform.TransformPoint(building.ProductionSpawnLocalPositions[i])'
    if methods['PublishFactionProductionSpawnPointsForBuilding'].count(point) != 1:
        raise ValueError('Original TransformPoint source boundary changed')
    methods['PublishFactionProductionSpawnPointsForBuilding'] = methods['PublishFactionProductionSpawnPointsForBuilding'].replace(point, 'sourceReadModelTransformPoint(building,building.ProductionSpawnLocalPositions[i])', 1)
    original_runway = methods['PublishFactionRunwayForBuilding']
    cut = original_runway.index('            if (!BuildingRunwaySystem.TryResolveRuntimeRunwayWorldData(')
    if '!building.Definition.HasRunway' not in original_runway[:cut]:
        raise ValueError('Original lazy no-runway guard changed')
    methods['PublishFactionRunwayForBuilding'] = original_runway[:cut] + '            throw new NotSupportedException("Actual positive runtime runway geometry provider is not bound");\n        }'
    body = '\n'.join(methods[name] for name in sorted(selected - supplied, key=lambda name: source.index(lex.member(source, name))))
    body = body.replace('BuildingRuntimeReadModelCompositionSystemHelper.Context', 'GeneratedProductionReadModelSource.Context')
    body = body.replace('BuildingRuntimeReadModelCompositionSystemHelper', 'SourceRuntimeBuildingCountReader')
    body = body.replace('BuildingDefinitionPrefabSystemHelper.GetProductionPrefab', 'SourceCampDefinitionReads.GetProductionPrefab')
    body = body.replace('BuildingDefinitionPrefabSystemHelper.GetProductionCount', 'SourceReadModelDefinitionStatics.GetProductionCount')
    body = body.replace('BuildingDefinitionPrefabSystemHelper', 'GeneratedConfiguredDefinitionSource')
    body = body.replace('BuildingDefinition', 'RuntimeDefinition')
    header = 'using static Game.Runtime.BuildingRuntimeBoundaryBuffers;using System;using System.Collections.Generic;using Unity.Collections;using Unity.Entities;using Unity.Mathematics;using UnityEngine;using Game.Components;using GameObject=Game.Runtime.SourceUnitPrefab;using RuntimeSignature=Game.Runtime.BuildingRuntimeSignatureUtility;namespace Game.Runtime{internal sealed partial class GeneratedRuntimeSpawnProcessor{\n'
    fields = []
    state_names = set(re.findall(r'\b_\w+', body)) - {'_boundaryIdsByPrefab', '_boundaryIdsByFallback', '_forcePublishNextUpdate'}
    for name in sorted(state_names):
        matches = re.findall(r'^\s*private[^\n]*\b' + re.escape(name) + r'\b[^\n]*;', source, re.M)
        if len(matches) != 1:
            raise ValueError('Unique original owner field required: ' + name)
        field = matches[0]
        if name == '_surfaceOverlaySystem':
            field = field.replace('BuildingRuntimeSurfaceOverlaySystem', 'SourceRuntimeSurfaceOverlay')
        fields.append(field)
    interval = re.findall(r'^\s*private const float PublishIntervalSeconds = [^;]+;', source, re.M)
    if len(interval) != 1:
        raise ValueError('Original publish interval required')
    def nested(marker):
        start = source.index(marker)
        mask = lex.mask(source)
        end = mask.index('{', start) + 1
        depth = 1
        while depth:
            depth += (mask[end] == '{') - (mask[end] == '}')
            end += 1
        return source[start:end]
    types_source = '\n'.join(nested(marker) for marker in ['private enum PublishPhase', 'private readonly struct ProductionSummaryKey', 'private struct ProductionSummaryCounts'])
    binding = '''
    Func<RuntimeBuildingEntity,Vector3,Vector3> sourceReadModelTransformPoint;
    internal void BindReadModelSourceFrame(Func<RuntimeBuildingEntity,Vector3,Vector3> transformPoint){if(sourceReadModelTransformPoint!=null||transformPoint==null)throw new InvalidOperationException("Actual producer frame binds once");sourceReadModelTransformPoint=transformPoint;}
    internal void PublishSourceReadModels(GeneratedConfiguredDefinitionSource definitions,SourceRuntimeBuildingCountReader query,GeneratedProductionReadModelSource.Context context,FactionResourceCompositionSystemHelper resources,EntityManager em,Entity boundary,IReadOnlyDictionary<int,RuntimeBuildingEntity> buildings,float now){if(sourceReadModelTransformPoint==null||!definitions.ReadModelUnitDomainKnown)throw new NotSupportedException("Complete actual configured source/read context required before cadence");PublishReadModelIfDue(definitions,query,context,resources,em,boundary,buildings,now);}
'''
    output.mkdir(parents=True, exist_ok=True)
    files = {'SourceFullReadModelCadence.cs': header + '\n'.join(interval + fields) + types_source + binding + body + '\n}}'}
    # The current owner has both two- and three-argument overloads. Select the
    # exact consumed signature, then use the same balanced lexical body reader.
    count_signature = re.findall(r'public int CountRuntimeBuildingsForFaction\(\s*Context context,\s*byte factionId\s*\)', text['query'])
    if len(count_signature) != 1:
        raise ValueError('Unique two-argument native faction count signature required')
    start = text['query'].index(count_signature[0])
    next_overload = text['query'].find('public int CountRuntimeBuildingsForFaction(', start + 1)
    count_part = text['query'][start:next_overload] if next_overload >= 0 else text['query'][start:]
    count = lex.member(count_part, 'CountRuntimeBuildingsForFaction') + '\n' + lex.member(text['query'], 'CountBuildingForFaction')
    count = count.replace('Context context', 'GeneratedProductionReadModelSource.Context context')
    files['SourceRuntimeBuildingCountReader.cs'] = 'using System.Collections.Generic;namespace Game.Runtime{internal sealed class SourceRuntimeBuildingCountReader{' + count + '}}'
    files['SourceReadModelDefinitionStatics.cs'] = 'namespace Game.Runtime{internal static class SourceReadModelDefinitionStatics{' + lex.member(text['definition'], 'GetProductionCount').replace('BuildingDefinition definition', 'RuntimeDefinition definition') + '}}'
    files['SourceFullReadModelCadence.cs'] = files['SourceFullReadModelCadence.cs'].replace('RuntimeBuildingEntity.PendingProduction', 'SourcePendingProduction')
    files['SourceOperationMapRunwayReadModelUtility.cs'] = text['runway']
    signature_body = text['signature']
    if signature_body.count('UnityEngine.Object unityObject') != 1 or signature_body.count('unityObject.GetEntityId().GetHashCode()') != 1:
        raise ValueError('Native object signature boundary changed')
    files['SourceBuildingRuntimeSignatureUtility.cs'] = signature_body.replace('UnityEngine.Object unityObject', 'SourceUnitPrefab unityObject').replace('unityObject.GetEntityId().GetHashCode()', 'System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(unityObject)')
    overlay = lex.member(text['overlay'], 'Publish')
    original_overlay = lex.member(text['overlay'], 'TryBuildRunwayOverlay')
    cut = original_overlay.index('            if (!BuildingRunwaySystem.TryResolveRuntimeRunwayWorldData(')
    if 'if (!definition.HasRunway)' not in original_overlay[:cut]:
        raise ValueError('Original surface overlay no-runway guard changed')
    prefix = original_overlay[:cut].replace('BuildingDefinition definition', 'RuntimeDefinition definition')
    overlay_method = prefix + '            throw new System.NotSupportedException("Actual positive runway surface geometry is not bound");\n        }'
    files['SourceRuntimeSurfaceOverlay.cs'] = 'using System.Collections.Generic;using Unity.Entities;using Game.Components;namespace Game.Runtime{internal sealed class SourceRuntimeSurfaceOverlay{' + overlay + overlay_method + lex.member(text['overlay'], 'EnsureBoundaryBuffer') + '}}'
    if any((repo / INPUTS[key]).read_bytes() != value for key, value in raw.items()) or helper.read_bytes() != helper_raw:
        raise ValueError('Native/helper drift before emission')
    for name, value in files.items():
        (output / name).write_text(value)
    proof = {'source_pins': {str(repo / INPUTS[key]): hashlib.sha256(value).hexdigest() for key, value in raw.items()}, 'helper_sha256': hashlib.sha256(helper_raw).hexdigest(), 'method_hashes': {name: hashlib.sha256(lex.member(source, name).encode()).hexdigest() for name in sorted(selected)}, 'supplied_by_existing_same_owner': sorted(supplied), 'outputs': {name: hashlib.sha256(value.encode()).hexdigest() for name, value in files.items()}, 'scope': 'Current source projection. No publication success until all actual readers are bound and all original callbacks complete.'}
    (output / 'source-extraction.json').write_text(json.dumps(proof, indent=2) + '\n')
    return proof
