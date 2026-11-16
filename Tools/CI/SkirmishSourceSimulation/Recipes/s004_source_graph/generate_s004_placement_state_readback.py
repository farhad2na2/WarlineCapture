#!/usr/bin/env python3
"""Add read-only placement-state evidence to an explicitly pinned S004 host."""
import argparse
import hashlib
import json
from pathlib import Path

ANCHOR = '            var prefabRegistry=em.CreateEntity(); var prefabEntries=em.AddBuffer<UnitPrefabRegistryEntry>(prefabRegistry);'
INSTRUMENT = r'''            // Read only: after original building placement and blocker owner, before actor spawn.
            var placementRows = new JArray();
            foreach (var item in registry.OrderBy(x => x.Key)) {
                var b = item.Value;
                placementRows.Add(new JObject { ["runtime_id"] = item.Key,
                    ["origin_x"] = b.OriginCell.x, ["origin_z"] = b.OriginCell.y,
                    ["footprint_x"] = b.Definition.FootprintCells.x, ["footprint_z"] = b.Definition.FootprintCells.y });
            }
            byte[] blockerBits = new byte[(count + 7) / 8], occupancyBits = new byte[(count + 7) / 8];
            var currentBlockers = em.GetComponentData<DynamicBlockerComponent>(gridEntity).Blocked;
            var currentOccupancy = em.GetComponentData<DynamicOccupancyComponent>(gridEntity).Occupied;
            int blockerCount = 0, occupancyCount = 0;
            for (int index = 0; index < count; index++) {
                if (currentBlockers.IsSet(index)) { blockerBits[index >> 3] |= (byte)(1 << (index & 7)); blockerCount++; }
                if (currentOccupancy.IsSet(index)) { occupancyBits[index >> 3] |= (byte)(1 << (index & 7)); occupancyCount++; }
            }
            string Digest(byte[] bytes) { using var algorithm = System.Security.Cryptography.SHA256.Create();
                return BitConverter.ToString(algorithm.ComputeHash(bytes)).Replace("-", "").ToLowerInvariant(); }
            var placementState = new JObject { ["kind"] = "S004SourcePlacementStateReadback",
                ["scope"] = "AfterOriginalBuildingPlacementAndBlockerUpdate_BeforeActorSpawn",
                ["registry"] = placementRows, ["blocker_count"] = blockerCount, ["blocker_sha256"] = Digest(blockerBits),
                ["occupancy_count"] = occupancyCount, ["occupancy_sha256"] = Digest(occupancyBits),
                ["occupancy_owner_update_asserted"] = false, ["native_parity"] = false, ["mission_ready"] = false };
            string stateFile = Path.Combine(Path.GetDirectoryName(args[4]), "placement-state.json");
            using (var stateStream = new FileStream(stateFile, FileMode.CreateNew, FileAccess.Write))
            using (var stateWriter = new StreamWriter(stateStream)) stateWriter.Write(placementState.ToString() + "\n");
'''

def sha(path): return hashlib.sha256(path.read_bytes()).hexdigest()

def generate(source, expected, output):
    if not source.is_absolute() or not source.is_file() or sha(source) != expected:
        raise ValueError('Exact absolute source projection required')
    text = source.read_text()
    if text.count(ANCHOR) != 1: raise ValueError('Exact post-placement owner anchor required')
    output.mkdir(parents=True, exist_ok=False)
    target = output / 'S004PlacementStateReadback.cs'
    with target.open('x') as stream: stream.write(text.replace(ANCHOR, INSTRUMENT + ANCHOR, 1))
    if sha(source) != expected: raise ValueError('Source changed during projection')
    receipt = {'kind':'S004PlacementStateReadbackProjection','status':'GeneratedSourceOnly',
               'source':{'path':str(source),'sha256':expected},'projection':{'path':str(target),'sha256':sha(target)},
               'scope':'Read-only post-placement registry and grid state; no native or occupancy-owner parity',
               'world_initialized':False,'runtime_admission':False,'mission_ready':False}
    with (output/'receipt.json').open('x') as stream: json.dump(receipt,stream,indent=2);stream.write('\n')
    return receipt

if __name__=='__main__':
    parser=argparse.ArgumentParser(description=__doc__);parser.add_argument('--source',type=Path,required=True)
    parser.add_argument('--source-sha256',required=True);parser.add_argument('--output',type=Path,required=True);args=parser.parse_args()
    print(json.dumps(generate(args.source,args.source_sha256,args.output.resolve())))
