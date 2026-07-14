using Game.Components;
using Unity.Entities;
using Unity.Mathematics;

namespace Game.Runtime
{
    internal static class MaterialFabricationTelemetrySystemHelper
    {
        internal static void Accumulate(
            ComponentLookup<FactionMaterialFabricationTelemetryComponent> telemetryLookup,
            Entity entity,
            in MaterialFabricationComponent fabrication,
            in MaterialFabricationSystem.TickResult result,
            float deltaTime)
        {
            if (!telemetryLookup.HasComponent(entity))
                return;

            FactionMaterialFabricationTelemetryComponent telemetry = telemetryLookup[entity];
            Accumulate(
                ref telemetry,
                result,
                deltaTime,
                fabrication.Status,
                fabrication.BlockReason);
            telemetryLookup[entity] = telemetry;
        }

        internal static void Accumulate(
            ref FactionMaterialFabricationTelemetryComponent telemetry,
            in MaterialFabricationSystem.TickResult result,
            float deltaTime,
            MaterialFabricationStatusCode status,
            MaterialFabricationBlockReasonCode blockReason)
        {
            float safeDeltaTime = math.isfinite(deltaTime) ? math.max(0f, deltaTime) : 0f;
            float activeSeconds = math.clamp(result.ActiveSeconds, 0f, safeDeltaTime);
            float blockedSeconds = safeDeltaTime - activeSeconds;
            bool changed = false;
            if (activeSeconds > 0f)
            {
                telemetry.ActiveSeconds = SaturatingAdd(telemetry.ActiveSeconds, activeSeconds);
                changed = true;
            }

            if (blockedSeconds > 0f && status != MaterialFabricationStatusCode.Producing)
            {
                switch (blockReason)
                {
                    case MaterialFabricationBlockReasonCode.NoOilInput:
                        telemetry.NoOilInputBlockedSeconds =
                            SaturatingAdd(telemetry.NoOilInputBlockedSeconds, blockedSeconds);
                        changed = true;
                        break;
                    case MaterialFabricationBlockReasonCode.MaterialsCapacityFull:
                        telemetry.MaterialsCapacityFullBlockedSeconds =
                            SaturatingAdd(telemetry.MaterialsCapacityFullBlockedSeconds, blockedSeconds);
                        changed = true;
                        break;
                    case MaterialFabricationBlockReasonCode.NoOilRoute:
                        telemetry.NoOilRouteBlockedSeconds =
                            SaturatingAdd(telemetry.NoOilRouteBlockedSeconds, blockedSeconds);
                        changed = true;
                        break;
                    case MaterialFabricationBlockReasonCode.ProductionDisabled:
                        telemetry.ProductionDisabledSeconds =
                            SaturatingAdd(telemetry.ProductionDisabledSeconds, blockedSeconds);
                        changed = true;
                        break;
                    case MaterialFabricationBlockReasonCode.BuildingDisabled:
                        telemetry.BuildingDisabledSeconds =
                            SaturatingAdd(telemetry.BuildingDisabledSeconds, blockedSeconds);
                        changed = true;
                        break;
                }
            }

            if (changed)
                IncrementVersion(ref telemetry.Version);
        }

        private static float SaturatingAdd(float value, float amount)
        {
            if (!math.isfinite(value) || value < 0f)
                value = 0f;
            if (!math.isfinite(amount) || amount <= 0f)
                return value;
            return value >= float.MaxValue - amount ? float.MaxValue : value + amount;
        }

        private static void IncrementVersion(ref uint version)
        {
            version = version == uint.MaxValue ? 1u : version + 1u;
        }
    }
}
