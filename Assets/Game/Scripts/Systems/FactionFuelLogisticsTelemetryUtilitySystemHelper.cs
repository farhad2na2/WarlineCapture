using Unity.Mathematics;
using Game.Components;

namespace Game.Runtime
{
    internal static class FactionFuelLogisticsTelemetryUtilitySystemHelper
    {
        internal static bool RecordRouteAssignment(
            ref FactionFuelLogisticsTelemetryComponent telemetry,
            bool isReassignment)
        {
            if (isReassignment)
                telemetry.TrayRouteReassignmentCount = SaturatingIncrement(telemetry.TrayRouteReassignmentCount);
            else
                telemetry.TrayRouteAssignmentCount = SaturatingIncrement(telemetry.TrayRouteAssignmentCount);

            IncrementVersion(ref telemetry.Version);
            return true;
        }

        internal static bool RecordRouteFailure(ref FactionFuelLogisticsTelemetryComponent telemetry)
        {
            telemetry.TrayRouteFailureCount = SaturatingIncrement(telemetry.TrayRouteFailureCount);
            IncrementVersion(ref telemetry.Version);
            return true;
        }

        internal static bool RecordOilDelivery(
            ref FactionFuelLogisticsTelemetryComponent telemetry,
            float deliveredBarrels,
            bool isFabricationDepot,
            bool isRefinery)
        {
            if (!math.isfinite(deliveredBarrels) || deliveredBarrels <= 0f || (!isFabricationDepot && !isRefinery))
                return false;

            if (isFabricationDepot)
            {
                telemetry.OilDeliveredToFabricationDepots =
                    SaturatingAdd(telemetry.OilDeliveredToFabricationDepots, deliveredBarrels);
            }
            else
            {
                telemetry.OilDeliveredToRefineries =
                    SaturatingAdd(telemetry.OilDeliveredToRefineries, deliveredBarrels);
            }

            IncrementVersion(ref telemetry.Version);
            return true;
        }

        private static int SaturatingIncrement(int value)
        {
            return value >= int.MaxValue ? int.MaxValue : math.max(0, value) + 1;
        }

        private static float SaturatingAdd(float value, float amount)
        {
            if (!math.isfinite(value) || value < 0f)
                value = 0f;

            float sum = value + amount;
            return math.isfinite(sum) ? sum : float.MaxValue;
        }

        private static void IncrementVersion(ref uint version)
        {
            version = version == uint.MaxValue ? 1u : version + 1u;
        }
    }
}
