#!/usr/bin/env python3
"""Project the original AirSelf completion branch onto an explicit host boundary.

This does not admit aircraft queues or claim delivery parity. Every substitution
is checked against the current original branch before a projection is written.
"""
import argparse
import hashlib
from pathlib import Path

ROOT = Path(__file__).resolve().parents[5]
SOURCE = ROOT / "Assets/Game/Scripts/Systems/BuildingProductionTransportPresentationSystemHelper.cs"


def generate(output):
    source = SOURCE.read_text()
    marker = "if (transport.Mode == ProductionTransportMode.AirSelf)"
    method = source.index("private bool TryCompleteSelfArrival(")
    start = source.index(marker, method)
    begin = source.index("{", start)
    depth = 1
    end = begin + 1
    while depth:
        depth += (source[end] == "{") - (source[end] == "}")
        end += 1
    branch = source[begin + 1:end - 1]
    bindings = [
        ("RuntimeBuildingEntity.PendingProduction readyAirPending = context.ProductionSystem.FindNextReadyTransportPending(building.PendingProductions, transport.Prefab, now)", "SourcePendingProduction readyAirPending = view.FindReady(building, prefab, now)"),
        ("ResolveProductionGroundGoalCell(context, transport.TouchdownPosition)", "view.ResolveCell(touchdown)"),
        ("TrySpawnPlayerUnitNearBuilding(context, building, readyAirPending.ProductionIndex, readyAirPending.ReservedProductionSlotIndex, transport.TouchdownPosition, airCell, ref randomState, readyAirPending)", "view.TrySpawn(building, readyAirPending.ProductionIndex, readyAirPending.ReservedProductionSlotIndex, touchdown, airCell, ref randomState, readyAirPending)"),
        ("context.ProductionSystem.RemovePendingProduction(building.PendingProductions, readyAirPending)", "view.Remove(building, readyAirPending)"),
        ("context.ProductionSystem.RebuildPendingProductionTimeline(building.PendingProductions, now, preserveActiveProgress: false)", "view.Rebuild(building, now, false)"),
        ("AlignNewestProducedUnitRotation(context, building, transport.Transform.forward)", "view.Align(building, forward)"),
        ("context.ProductionSystem.ReleasePendingProduction(readyAirPending)", "view.Release(readyAirPending)"),
        ("DestroyTransport(building, transport)", "view.Destroy(building)"),
    ]
    for old, new in bindings:
        if branch.count(old) != 1:
            raise ValueError("original branch changed: " + old)
        branch = branch.replace(old, new)
    result = '''using Unity.Mathematics;
namespace Game.Runtime {
internal interface ISourceAirSelfCompletionView {
 SourcePendingProduction FindReady(RuntimeBuildingEntity building, SourceUnitPrefab prefab, float now);
 int2 ResolveCell(float3 touchdown);
 bool TrySpawn(RuntimeBuildingEntity building, int index, int slot, float3 touchdown, int2 cell, ref uint random, SourcePendingProduction pending);
 bool Remove(RuntimeBuildingEntity building, SourcePendingProduction pending);
 void Rebuild(RuntimeBuildingEntity building, float now, bool preserve);
 void Align(RuntimeBuildingEntity building, float3 forward);
 void Release(SourcePendingProduction pending);
 void Destroy(RuntimeBuildingEntity building);
}
internal static class SourceAirSelfCompletionProjection {
 internal static bool Complete(ISourceAirSelfCompletionView view, RuntimeBuildingEntity building, SourceUnitPrefab prefab, float3 touchdown, float3 forward, float now, ref uint randomState) {
''' + branch + '''
 }
}}
'''
    output.write_text(result)
    return hashlib.sha256(source.encode()).hexdigest(), hashlib.sha256(result.encode()).hexdigest()


if __name__ == "__main__":
    parser = argparse.ArgumentParser()
    parser.add_argument("output", type=Path)
    args = parser.parse_args()
    original, projected = generate(args.output)
    print("original_sha256=" + original + " projection_sha256=" + projected)
