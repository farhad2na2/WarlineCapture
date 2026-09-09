using Unity.Mathematics;
using Game.Components;

namespace Game.Runtime
{
    public partial struct UnitAirMovementSystem
    {
        private static void ObserveVtolFlight(ref UnitAirComponent state, float height, float groundY, bool inTransit)
        {
            // VTOL flight bypasses runway lift-off. Publish observed height for transport and mission readers.
            if (state.UsesRunway == 0 && !inTransit &&
                height > groundY + math.max(.25f, TransportBoardingData.AirBoardingGroundedHeightTolerance))
                state.Airborne = 1;
        }

        private static float ResolveFixedWingCruiseY(
            ref UnitAirComponent state,
            MapSurfaceComponent surface,
            bool hasSurface,
            in GridConfig grid,
            float3 currentWorld,
            float3 targetWorld,
            float speed,
            float deltaTime,
            float fallbackGroundY,
            float cruiseHeight,
            float currentY)
        {
            float rawCruiseY = ResolveAirCruiseY(
                surface,
                hasSurface,
                grid,
                currentWorld,
                targetWorld,
                speed,
                deltaTime,
                fallbackGroundY,
                cruiseHeight,
                true);
            float minimumCruiseY = fallbackGroundY + ResolveAirClearance(cruiseHeight, true);
            rawCruiseY = math.max(rawCruiseY, minimumCruiseY);

            if (state.FixedWingCruiseYInitialized == 0)
            {
                state.FixedWingCruiseY = math.max(rawCruiseY, currentY);
                state.FixedWingCruiseYInitialized = 1;
                return state.FixedWingCruiseY;
            }

            float currentCruiseY = math.max(state.FixedWingCruiseY, minimumCruiseY);
            float deadband = math.max(
                FixedWingCruiseHeightDeadbandMin,
                math.max(0.01f, grid.CellSize) * FixedWingCruiseHeightDeadbandCells);
            float targetCruiseY = currentCruiseY;
            if (rawCruiseY > currentCruiseY + deadband)
                targetCruiseY = rawCruiseY;
            else if (rawCruiseY < currentCruiseY - deadband)
                targetCruiseY = math.max(rawCruiseY, minimumCruiseY);

            float cruiseDelta = targetCruiseY - currentCruiseY;
            if (math.abs(cruiseDelta) > 1e-4f)
            {
                float climbRate = math.max(FixedWingCruiseHeightMinClimbRate, math.max(0.01f, speed) * FixedWingCruiseHeightClimbSpeedMultiplier);
                float descentRate = math.max(FixedWingCruiseHeightMinDescentRate, math.max(0.01f, speed) * FixedWingCruiseHeightDescentSpeedMultiplier);
                float maxStep = (cruiseDelta > 0f ? climbRate : descentRate) * math.max(0f, deltaTime);
                currentCruiseY += math.clamp(cruiseDelta, -maxStep, maxStep);
            }

            state.FixedWingCruiseY = math.max(currentCruiseY, minimumCruiseY);
            return state.FixedWingCruiseY;
        }

        private static void ResetFixedWingCruiseState(ref UnitAirComponent state)
        {
            state.FixedWingCruiseY = 0f;
            state.FixedWingCruiseYInitialized = 0;
        }

    }
}
