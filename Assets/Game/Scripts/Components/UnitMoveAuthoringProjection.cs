using Unity.Mathematics;

namespace Game.Components
{
    /// <summary>
    /// Data-only projection of the UnitMove, footprint, movement behavior,
    /// vehicle motion, and initial vehicle kinematics authored by UnitGridAuthoring.
    /// Shared by the game baker and source host; use Unity Mathematics directly
    /// to retain its exact floating point min/max and ceil behavior.
    /// </summary>
    public readonly struct UnitMoveAuthoringProjection
    {
        public readonly float Speed;
        public readonly float WalkSpeed;
        public readonly float RoadSpeedMultiplier;
        public readonly float ArriveDistance;
        public readonly UnitFootprint Footprint;
        public readonly UnitMovementBehavior MovementBehavior;
        public readonly UnitVehicleMovement VehicleMovement;
        public readonly UnitVehicleKinematics VehicleKinematics;
        public readonly bool HasMovementSlice;

        private UnitMoveAuthoringProjection(float speed, float walkSpeed, float roadSpeedMultiplier, float arriveDistance,
            UnitFootprint footprint = default, UnitMovementBehavior movementBehavior = default,
            UnitVehicleMovement vehicleMovement = default, UnitVehicleKinematics vehicleKinematics = default,
            bool hasMovementSlice = false)
        {
            Speed = speed;
            WalkSpeed = walkSpeed;
            RoadSpeedMultiplier = roadSpeedMultiplier;
            ArriveDistance = arriveDistance;
            Footprint = footprint;
            MovementBehavior = movementBehavior;
            VehicleMovement = vehicleMovement;
            VehicleKinematics = vehicleKinematics;
            HasMovementSlice = hasMovementSlice;
        }

        public static UnitMoveAuthoringProjection FromAuthoredValues(
            float speed,
            float walkSpeed,
            float roadSpeedMultiplier,
            float arriveDistance)
        {
            // Mirrors math.min(math.max(0.01f, walkSpeed), math.max(0.01f, speed))
            // from UnitGridAuthoring.UnitGridBaker. Preserve the authored values
            // for every other field, including out-of-range values.
            float projectedWalkSpeed = math.min(math.max(0.01f, walkSpeed), math.max(0.01f, speed));
            return new UnitMoveAuthoringProjection(speed, projectedWalkSpeed, roadSpeedMultiplier, arriveDistance);
        }

        public static UnitMoveAuthoringProjection FromAuthoredMovement(
            float speed, float walkSpeed, float roadSpeedMultiplier, float arriveDistance,
            int configuredFootprintX, int configuredFootprintY, bool autoCalculateFootprint,
            bool usesVehicleMotion, bool allowIdleWander, bool hasModelBounds, float modelSizeX, float modelSizeZ)
            => FromAuthoredMovementCore(speed, walkSpeed, roadSpeedMultiplier, arriveDistance,
                configuredFootprintX, configuredFootprintY, autoCalculateFootprint, usesVehicleMotion,
                allowIdleWander, hasModelBounds ? new float2(modelSizeX, modelSizeZ) : (float2?)null);

        public static UnitMoveAuthoringProjection FromAuthoredMovementWithoutModelBounds(
            float speed, float walkSpeed, float roadSpeedMultiplier, float arriveDistance,
            int configuredFootprintX, int configuredFootprintY, bool autoCalculateFootprint,
            bool usesVehicleMotion, bool allowIdleWander)
        {
            if (usesVehicleMotion)
                throw new System.ArgumentException("Vehicle movement projection requires actual model-bounds inputs", nameof(usesVehicleMotion));
            return FromAuthoredMovementCore(speed, walkSpeed, roadSpeedMultiplier, arriveDistance,
                configuredFootprintX, configuredFootprintY, autoCalculateFootprint, false, allowIdleWander, null);
        }

        private static UnitMoveAuthoringProjection FromAuthoredMovementCore(
            float speed, float walkSpeed, float roadSpeedMultiplier, float arriveDistance,
            int configuredFootprintX, int configuredFootprintY, bool autoCalculateFootprint,
            bool usesVehicleMotion, bool allowIdleWander, float2? modelSizeXZ)
        {
            int configuredX = math.max(1, configuredFootprintX);
            int configuredY = math.max(1, configuredFootprintY);
            int footprintX = configuredX;
            int footprintY = configuredY;
            if (usesVehicleMotion && configuredX <= 1 && configuredY <= 1 && modelSizeXZ.HasValue)
            {
                int modelFootprintX = math.max(1, (int)math.ceil(modelSizeXZ.Value.x));
                int modelFootprintY = math.max(1, (int)math.ceil(modelSizeXZ.Value.y));
                if (autoCalculateFootprint)
                {
                    footprintX = modelFootprintX;
                    footprintY = modelFootprintY;
                }
            }

            float modelLength = modelSizeXZ.HasValue
                ? math.max(modelSizeXZ.Value.x, modelSizeXZ.Value.y)
                : math.max(footprintX, footprintY);
            float turnSpeed = usesVehicleMotion ? 180f : 720f;
            float acceleration = usesVehicleMotion ? math.max(6f, modelLength * 3f) : 999f;
            float braking = usesVehicleMotion ? math.max(8f, modelLength * 4f) : 999f;
            float rearPivotOffset = usesVehicleMotion ? math.max(0.35f, modelLength * 0.22f) : 0f;
            UnitMoveAuthoringProjection move = FromAuthoredValues(speed, walkSpeed, roadSpeedMultiplier, arriveDistance);
            return new UnitMoveAuthoringProjection(move.Speed, move.WalkSpeed, move.RoadSpeedMultiplier,
                move.ArriveDistance,
                new UnitFootprint { Size = new int2(footprintX, footprintY) },
                new UnitMovementBehavior
                {
                    AllowIdleWander = (byte)(allowIdleWander ? 1 : 0),
                    UsesVehicleMotion = (byte)(usesVehicleMotion ? 1 : 0)
                },
                new UnitVehicleMovement
                {
                    TurnSpeedDegrees = turnSpeed,
                    Acceleration = acceleration,
                    Braking = braking,
                    RearPivotOffset = rearPivotOffset
                },
                new UnitVehicleKinematics { CurrentSpeed = 0f, StallSeconds = 0f }, true);
        }
    }

    /// <summary>
    /// Exact initial UnitIdleWanderComponent state emitted by UnitGridAuthoring.
    /// Shared so source fixtures retain the authored name hash expression.
    /// </summary>
    public static class UnitIdleWanderAuthoringProjection
    {
        public static UnitIdleWanderComponent FromAuthoredIdentity(int footprintX, int footprintY, string authoredName)
        {
            if (authoredName == null)
                throw new System.ArgumentNullException(nameof(authoredName));

            return new UnitIdleWanderComponent
            {
                RandomState = math.max(1u, math.hash(new int4(footprintX, footprintY, authoredName.GetHashCode(), 0x45d9f3b))),
                RetrySeconds = 0f,
                CurrentIdleDelaySeconds = 0f
            };
        }
    }

    /// <summary>Exact initial animation timing values emitted by UnitGridAuthoring.</summary>
    public static class UnitAnimationAuthoringProjection
    {
        public static UnitAnimationSettings FromConfiguredValues(
            float idleDelayMinSeconds, float idleDelayMaxSeconds,
            float idleWanderDistanceMin, float idleWanderDistanceMax,
            float attackAnimationSeconds, float deathAnimationSeconds)
        {
            return new UnitAnimationSettings
            {
                IdleDelayMinSeconds = math.max(0f, idleDelayMinSeconds),
                IdleDelayMaxSeconds = math.max(idleDelayMinSeconds, idleDelayMaxSeconds),
                IdleWanderDistanceMin = math.max(0f, idleWanderDistanceMin),
                IdleWanderDistanceMax = math.max(idleWanderDistanceMin, idleWanderDistanceMax),
                AttackAnimationSeconds = math.max(0.01f, attackAnimationSeconds),
                DeathAnimationSeconds = math.max(0.01f, deathAnimationSeconds)
            };
        }
    }
}
