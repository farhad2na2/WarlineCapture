using Unity.Mathematics;
using Unity.Transforms;

namespace Game.Components
{
    public static class UnitVehicleMovementUtility
    {
        public static bool IsVehicle(in UnitFootprint footprint, in UnitMovementBehavior behavior) =>
            behavior.UsesVehicleMotion != 0;

        public static float3 Forward(in quaternion rotation)
        {
            float3 forward = math.mul(rotation, new float3(0f, 0f, 1f));
            forward.y = 0f;
            return math.normalizesafe(forward, new float3(0f, 0f, 1f));
        }

        public static float SignedAngleY(float3 from, float3 to)
        {
            float3 a = math.normalizesafe(new float3(from.x, 0f, from.z), new float3(0f, 0f, 1f));
            float3 b = math.normalizesafe(new float3(to.x, 0f, to.z), new float3(0f, 0f, 1f));
            float crossY = a.z * b.x - a.x * b.z;
            float dot = math.clamp(math.dot(a, b), -1f, 1f);
            return math.atan2(crossY, dot);
        }

        public static quaternion RotateTowards(quaternion currentRotation, float3 targetDirection, float maxTurnRadians)
        {
            float3 currentForward = Forward(currentRotation);
            float angle = SignedAngleY(currentForward, targetDirection);
            if (math.abs(angle) <= math.radians(2.5f))
                return currentRotation;

            float clampedAngle = math.clamp(angle, -maxTurnRadians, maxTurnRadians);
            return math.mul(currentRotation, quaternion.RotateY(clampedAngle));
        }

        public static bool MoveVehicle(
            ref LocalTransform transform,
            ref UnitVehicleKinematics kinematics,
            in UnitVehicleMovement movement,
            float3 desiredDirection,
            float turnAngleThresholdRadians,
            float maxSpeed,
            float deltaTime,
            float maxDistance)
        {
            float turnRadians = math.radians(math.max(1f, movement.TurnSpeedDegrees)) * deltaTime;
            quaternion oldRotation = transform.Rotation;
            float3 oldForward = Forward(oldRotation);
            float angle = math.abs(SignedAngleY(oldForward, desiredDirection));
            quaternion newRotation = RotateTowards(oldRotation, desiredDirection, turnRadians);
            transform.Rotation = newRotation;

            // Tanks first turn in place, then drive straight forward.
            if (angle > turnAngleThresholdRadians)
            {
                kinematics.CurrentSpeed = 0f;
                return false;
            }

            float effectiveSpeed = maxSpeed;
            kinematics.CurrentSpeed = effectiveSpeed;
            float moveDistance = math.min(maxDistance, effectiveSpeed * deltaTime);
            transform.Position += Forward(newRotation) * moveDistance;
            return moveDistance > 0f;
        }
    }
}
