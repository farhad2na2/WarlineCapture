using System;
using Game.Components;
using Unity.Mathematics;
using UnityEngine;

internal static class S073UnitFootprintHistoricalDifferentialControl
{
    static int Bits(float x) => BitConverter.ToInt32(BitConverter.GetBytes(x), 0);
    public static int Main()
    {
        try
        {
            int cases = 0;
            int[] configured = { -1, 0, 1, 2, 4 };
            float[] sizes = { 0f, 0.1f, 0.999f, 1f, 1.001f, 2f, 2.001f, 10f };
            foreach (int x in configured) foreach (int y in configured)
            foreach (bool vehicle in new[] { false, true })
            foreach (bool automatic in new[] { false, true })
            foreach (bool hasBounds in new[] { false, true })
            foreach (float sx in sizes) foreach (float sz in sizes)
            {
                var authored = new HistoricalFootprintOwner.LegacyInputs {
                    footprintCells = new Vector2Int(x, y), UsesVehicleMotion = vehicle,
                    autoCalculateFootprint = automatic
                };
                var bounds = new HistoricalFootprintOwner.LegacyBounds { size = new float3(sx, 1f, sz) };
                int2 oldFootprint = HistoricalFootprintOwner.Footprint(authored, hasBounds, bounds);
                UnitVehicleMovement oldMovement = HistoricalFootprintOwner.Vehicle(authored, oldFootprint, hasBounds, bounds);
                var current = UnitMoveAuthoringProjection.FromAuthoredMovement(
                    4f, 2f, 1f, 0.1f, x, y, automatic, vehicle, false, hasBounds, sx, sz);
                if (oldFootprint.x != current.Footprint.Size.x || oldFootprint.y != current.Footprint.Size.y ||
                    Bits(oldMovement.TurnSpeedDegrees) != Bits(current.VehicleMovement.TurnSpeedDegrees) ||
                    Bits(oldMovement.Acceleration) != Bits(current.VehicleMovement.Acceleration) ||
                    Bits(oldMovement.Braking) != Bits(current.VehicleMovement.Braking) ||
                    Bits(oldMovement.RearPivotOffset) != Bits(current.VehicleMovement.RearPivotOffset))
                    throw new InvalidOperationException($"Differential mismatch config=({x},{y}) vehicle={vehicle} auto={automatic} bounds={hasBounds} size=({sx},{sz})");
                cases++;
            }
            Console.WriteLine($"[S073UnitFootprintHistoricalDifferentialControl] result=Passed cases={cases} fixtureOnly=1 sceneParity=0 readiness=0");
            return 0;
        }
        catch (Exception error) { Console.Error.WriteLine(error); return 1; }
    }
}
