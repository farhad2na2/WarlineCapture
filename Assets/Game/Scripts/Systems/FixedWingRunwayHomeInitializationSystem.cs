using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using Game.Components;

namespace Game.Runtime
{
    [UpdateBefore(typeof(UnitAirMovementSystem))]
    public partial struct FixedWingRunwayHomeInitializationSystem : ISystem
    {
        private EntityQuery _boundaryQuery;

        public void OnCreate(ref SystemState state)
        {
            _boundaryQuery = state.GetEntityQuery(
                ComponentType.ReadOnly<BuildingRuntimeStateTag>(),
                ComponentType.ReadOnly<BuildingFactionRunwayReadModel>());

            state.RequireForUpdate(_boundaryQuery);
            state.RequireForUpdate<UnitAirMovement>();
        }

        public void OnUpdate(ref SystemState state)
        {
            Entity boundaryEntity = _boundaryQuery.GetSingletonEntity();
            if (!state.EntityManager.HasBuffer<BuildingFactionRunwayReadModel>(boundaryEntity))
                return;

            DynamicBuffer<BuildingFactionRunwayReadModel> runways =
                state.EntityManager.GetBuffer<BuildingFactionRunwayReadModel>(boundaryEntity, true);
            if (runways.Length == 0)
                return;

            foreach (var (airState, transform, grid, faction, sourceKey) in SystemAPI
                         .Query<RefRW<UnitAirComponent>, RefRO<LocalTransform>, RefRO<UnitGrid>, RefRO<Faction>, RefRO<UnitSourcePrefabKey>>()
                         .WithAll<UnitAirMovement, UnitMove>()
                         .WithNone<UnitDeathAnimationComponent, Disabled, UnitSpawnTransitTag>())
            {
                if (!ShouldInitializeRunwayHome(airState.ValueRO, sourceKey.ValueRO.Value))
                    continue;

                if (!TryFindNearestRunway(runways, faction.ValueRO.Id, transform.ValueRO.Position, out BuildingFactionRunwayReadModel runway))
                    continue;

                ResolveRunwayThresholdsForUnit(
                    runway,
                    transform.ValueRO.Position,
                    out float3 takeoffPosition,
                    out int2 takeoffCell,
                    out float3 landingPosition,
                    out int2 landingCell);

                float3 homePosition = airState.ValueRO.HomeInitialized != 0
                    ? airState.ValueRO.HomePosition
                    : transform.ValueRO.Position;
                int2 homeCell = airState.ValueRO.HomeInitialized != 0
                    ? airState.ValueRO.HomeCell
                    : grid.ValueRO.Cell;

                UnitAirComponent initialized = airState.ValueRO;
                initialized.HomePosition = homePosition;
                initialized.HomeCell = homeCell;
                initialized.HomeInitialized = 1;
                initialized.ReturningHome = 0;
                initialized.Airborne = 0;
                initialized.UsesRunway = 1;
                initialized.TakeoffRolling = 0;
                initialized.LandingRolling = 0;
                initialized.AttackRunActive = 0;
                initialized.ReturnApproachInitialized = 0;
                initialized.RunwayTakeoffPosition = takeoffPosition;
                initialized.RunwayTakeoffCell = takeoffCell;
                initialized.RunwayLandingPosition = landingPosition;
                initialized.RunwayLandingCell = landingCell;
                initialized.AttackRunExitPosition = default;
                airState.ValueRW = initialized;
            }
        }

        private static bool ShouldInitializeRunwayHome(
            UnitAirComponent airState,
            FixedString64Bytes sourceKey)
        {
            if (!FixedWingRunwayUnitUtility.IsFixedWingRunwayUnit(sourceKey))
                return false;

            if (airState.Airborne != 0 ||
                airState.ReturningHome != 0 ||
                airState.TakeoffRolling != 0 ||
                airState.LandingRolling != 0 ||
                airState.AttackRunActive != 0 ||
                airState.ReturnApproachInitialized != 0)
            {
                return false;
            }

            return true;
        }

        private static void ResolveRunwayThresholdsForUnit(
            BuildingFactionRunwayReadModel runway,
            float3 unitPosition,
            out float3 takeoffPosition,
            out int2 takeoffCell,
            out float3 landingPosition,
            out int2 landingCell)
        {
            float3 toTakeoff = runway.TakeoffPosition - unitPosition;
            float3 toLanding = runway.LandingPosition - unitPosition;
            toTakeoff.y = 0f;
            toLanding.y = 0f;

            if (math.lengthsq(toLanding) < math.lengthsq(toTakeoff))
            {
                takeoffPosition = runway.LandingPosition;
                takeoffCell = runway.LandingCell;
                landingPosition = runway.TakeoffPosition;
                landingCell = runway.TakeoffCell;
                return;
            }

            takeoffPosition = runway.TakeoffPosition;
            takeoffCell = runway.TakeoffCell;
            landingPosition = runway.LandingPosition;
            landingCell = runway.LandingCell;
        }

        private static bool TryFindNearestRunway(
            DynamicBuffer<BuildingFactionRunwayReadModel> runways,
            byte factionId,
            float3 position,
            out BuildingFactionRunwayReadModel nearestRunway)
        {
            nearestRunway = default;
            float bestDistanceSq = float.MaxValue;
            bool found = false;
            for (int i = 0; i < runways.Length; i++)
            {
                BuildingFactionRunwayReadModel runway = runways[i];
                if (runway.FactionId != factionId)
                    continue;

                float3 delta = runway.Center - position;
                delta.y = 0f;
                float distanceSq = math.lengthsq(delta);
                if (found && distanceSq >= bestDistanceSq)
                    continue;

                nearestRunway = runway;
                bestDistanceSq = distanceSq;
                found = true;
            }

            return found;
        }

    }

    internal static class FixedWingRunwayUnitUtility
    {
        public static bool IsFixedWingRunwayUnit(FixedString64Bytes sourceKey)
        {
            if (sourceKey.Length == 0)
                return false;

            if (ContainsIgnoreCase(sourceKey, "Helicopter") ||
                ContainsIgnoreCase(sourceKey, "Heli"))
            {
                return false;
            }

            return ContainsIgnoreCase(sourceKey, "Jet") ||
                   ContainsIgnoreCase(sourceKey, "Drone") ||
                   ContainsIgnoreCase(sourceKey, "Plane") ||
                   ContainsIgnoreCase(sourceKey, "FixedWing");
        }

        private static bool ContainsIgnoreCase(FixedString64Bytes value, string needle)
        {
            int valueLength = value.Length;
            int needleLength = needle.Length;
            if (needleLength == 0 || valueLength < needleLength)
                return false;

            for (int start = 0; start <= valueLength - needleLength; start++)
            {
                bool matches = true;
                for (int offset = 0; offset < needleLength; offset++)
                {
                    if (ToLowerAscii(value[start + offset]) == ToLowerAscii((byte)needle[offset]))
                        continue;

                    matches = false;
                    break;
                }

                if (matches)
                    return true;
            }

            return false;
        }

        private static byte ToLowerAscii(byte value)
        {
            return value is >= (byte)'A' and <= (byte)'Z'
                ? (byte)(value + ((byte)'a' - (byte)'A'))
                : value;
        }
    }
}
