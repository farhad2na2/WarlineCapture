using System;
using Game.Components;
using UnityEngine;

internal static class S073HistoricalProductionScalarControl
{
    static int Bits(float value) => BitConverter.ToInt32(BitConverter.GetBytes(value), 0);
    public static int Main()
    {
        try
        {
            int cases = 0;
            float[] values = { -10f, 0f, 0.009f, 0.01f, 5f };
            int[] counts = { -1, 0, 1, 4 };
            for (int mask = 0; mask < 128; mask++)
            foreach (float duration in values) foreach (float arrival in values) foreach (float hold in values)
            foreach (int concurrent in counts)
            {
                var owner = new HistoricalProductionScalarOwner {
                    config = (mask & 1) == 0 ? null : new HistoricalProductionScalarOwner.Config {
                        IsAirUnit = (mask & 2) != 0,
                        ProductionTransportRequiresAirportRunway = (mask & 4) != 0,
                        ProductionTransportUsesRunwayLanding = (mask & 8) != 0 },
                    isAirUnit = (mask & 16) != 0,
                    productionTransportRequiresAirportRunway = (mask & 32) != 0,
                    productionTransportUsesRunwayLanding = (mask & 64) != 0,
                    productionDurationSeconds = duration, productionTransportArrivalSeconds = arrival,
                    productionTransportHoldForNextReadySeconds = hold, productionTransportMaxConcurrent = concurrent,
                    footprintCells = new Vector2Int(3, 4)
                };
                UnitProductionMetadataScalarValues projected = UnitProductionMetadataScalarPolicy.Project(owner.CaptureProductionMetadataScalarSource());
                var footprint = owner.GetConfiguredFootprintCells();
                if (Bits(owner.ProductionDurationSeconds) != Bits(projected.ProductionDurationSeconds) ||
                    Bits(owner.ProductionTransportArrivalSeconds) != Bits(projected.ProductionTransportArrivalSeconds) ||
                    Bits(owner.ProductionTransportHoldForNextReadySeconds) != Bits(projected.ProductionTransportHoldForNextReadySeconds) ||
                    owner.ProductionTransportMaxConcurrent != projected.ProductionTransportMaxConcurrent ||
                    owner.IsAirUnit != projected.IsAirUnit ||
                    owner.ProductionTransportRequiresAirportRunway != projected.ProductionTransportRequiresAirportRunway ||
                    owner.ProductionTransportUsesRunwayLanding != projected.ProductionTransportUsesRunwayLanding ||
                    footprint.x != projected.FootprintWidth || footprint.y != projected.FootprintDepth)
                    throw new InvalidOperationException("Historical/current production scalar mismatch at case " + cases);
                cases++;
            }
            Console.WriteLine($"[S073HistoricalProductionScalarControl] result=Passed cases={cases} fixtureOnly=1 lifecycleParity=0 readiness=0");
            return 0;
        }
        catch (Exception error) { Console.Error.WriteLine(error); return 1; }
    }
}
