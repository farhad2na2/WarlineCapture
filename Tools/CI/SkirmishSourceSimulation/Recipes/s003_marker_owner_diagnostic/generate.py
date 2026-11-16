"""Extract the current marker collector into an opt-in S003 source-world census."""
import hashlib
from pathlib import Path

OWNER = 'Assets/Game/Scripts/Systems/MatchHudMinimapMarkerSystem.cs'
SHIM = 'Tools/CI/SkirmishSourceSimulation/Adapters/S026/384-403_DefenseLogicalApis.cs'
BRIDGE = 'Tools/CI/SkirmishSourceSimulation/Adapters/SourceS003AirOpeningController.cs'
SUPPORT = 'Tools/CI/SkirmishSourceSimulation/Recipes/s003_marker_owner_diagnostic/support.cs'


def generate(repo, output):
    repo = Path(repo).resolve(strict=True)
    paths = [repo / x for x in (OWNER, SHIM, BRIDGE, SUPPORT)]
    raw = [p.read_bytes() for p in paths]
    original = raw[0].decode()

    def part(marker):
        start = original.index(marker)
        opening = original.index('{', start)
        depth = 0
        for i in range(opening, len(original)):
            if original[i] == '{': depth += 1
            elif original[i] == '}':
                depth -= 1
                if not depth: return original[start:i + 1]
        raise ValueError('Unclosed original marker owner')

    job = part('private partial struct CollectMarkersJob : IJobEntity')
    job = job.replace('private partial struct CollectMarkersJob : IJobEntity', 'internal struct CollectMarkersJob')
    if job.count('private void Execute(') != 1: raise ValueError('Original marker execute seam changed')
    job = job.replace('private void Execute(', 'internal void Execute(')
    key = part('private struct ContactKey : IEquatable<ContactKey>').replace('private struct ContactKey', 'internal struct ContactKey')
    extra = []
    for name, suffix in [('PruneDeadMarkersJob', 'IJob'), ('PublishMarkersJob', 'IJob'),
                         ('CollectFrozenLedgerMarkersJob', 'IJobEntity'), ('CollectScanIntelMarkersJob', 'IJobEntity')]:
        marker = ('private partial struct ' if suffix == 'IJobEntity' else 'private struct ') + name + ' : ' + suffix
        item = part(marker).replace(marker, 'internal struct ' + name)
        if suffix == 'IJobEntity':
            if item.count('private void Execute(') != 1: raise ValueError('Original marker job seam changed: ' + name)
            item = item.replace('private void Execute(', 'internal void Execute(')
        extra.append(item)
    collector = ('using System; using Unity.Collections; using Unity.Entities; using Unity.Mathematics; '
                 'using Unity.Transforms; using Game.Components; using Game.Skirmish.Contracts; '
                 'namespace Game.Runtime { internal static class OriginalMarkerCollectorProbe { '
                 'const byte CollectPlayerMarkers=1, CollectEnemyMarkers=2; ' + job + '\n' + key + '\n' +
                 '\n'.join(extra) + '\n}}\n')
    shim = raw[1].decode()
    anchor = '        public void Dispose() { entries?.Clear(); }'
    if shim.count(anchor) != 1: raise ValueError('Source set seam changed')
    shim = shim.replace(anchor, '        public bool Contains(T value) { if (entries == null) throw new InvalidOperationException("Uncreated defense set"); return entries.Contains(value); }\n' + anchor)
    bridge = raw[2].decode()
    field_anchor = '        readonly EntityManager em;'
    if bridge.count(field_anchor) != 1: raise ValueError('S003 owner field seam changed')
    bridge = bridge.replace(field_anchor, '        SourceS003MarkerOwnerCensus markerCensus;\n' + field_anchor)
    anchor = '            var view = SkirmishAriaPublicProjection.FromSession(em, session, army);'
    if bridge.count(anchor) != 1: raise ValueError('S003 observation seam changed')
    bridge = bridge.replace(anchor, '            if (Environment.GetEnvironmentVariable("WARLINE_S003_ORIGINAL_MARKER_CENSUS") == "1") {\n                if (markerCensus == null) markerCensus = new SourceS003MarkerOwnerCensus(em, session);\n                markerCensus.Write(tick, frame, time);\n            }\n' + anchor)
    anchor = '            contacts.UpdateAfterFog(frame, time);'
    if bridge.count(anchor) != 1: raise ValueError('S003 public contact seam changed')
    bridge = bridge.replace(anchor, anchor + '\n            if (markerCensus != null) markerCensus.ComparePlayers(contacts.ReadSnapshot(), tick, frame);')
    output = Path(output); output.mkdir(parents=True, exist_ok=False)
    values = {'OriginalMarkerCollector.cs': collector, 'SourceMarkerCensus.cs': raw[3].decode(),
              'MarkerSetShim.cs': shim, 'SourceS003MarkerCensusBridge.cs': bridge}
    for name, data in values.items(): (output / name).write_text(data)
    if any(p.read_bytes() != data for p, data in zip(paths, raw)): raise ValueError('Source changed during generation')
    return {'source_pins': {str(p): hashlib.sha256(data).hexdigest() for p, data in zip(paths, raw)},
            'outputs': {name: hashlib.sha256(data.encode()).hexdigest() for name, data in values.items()},
            'experimental_only': True, 'scope': 'Original collector same-world census; no scheduler or rendered presentation claim'}
