using System;
using Game.Components;
using Unity.Transforms;
using Unity.Mathematics;

// Source-linked original production utility. This adapter owns no collision,
// grounding, orders, occupancy or scheduler policy, and never fills absent fields.
internal struct CapturedVehicleState
{
    internal string Id;
    internal byte Role,Category,Faction;
    internal UnitFootprint Footprint;
    internal UnitMovementBehavior Behavior;
    internal UnitVehicleMovement Movement;
    internal UnitVehicleKinematics Kinematics;
    internal LocalTransform Transform;
    internal float CapturedSpeed,CapturedWalkSpeed,CapturedRoadMultiplier;
}
internal readonly struct ExplicitVehicleStep
{
    internal readonly float3 Direction;
    internal readonly float TurnThresholdRadians,MaxSpeed,DeltaTime,MaxDistance;
    internal ExplicitVehicleStep(float3 direction,float threshold,float speed,float delta,float distance)
    {Direction=direction;TurnThresholdRadians=threshold;MaxSpeed=speed;DeltaTime=delta;MaxDistance=distance;}
}
internal static class CapturedVehicleSteering
{
    static void Need(bool ok,string message){if(!ok)throw new ArgumentException(message);}
    static bool Finite(float f)=>float.IsFinite(f);
    internal static bool Step(ref CapturedVehicleState state,in ExplicitVehicleStep input)
    {
        Need(state.Footprint.Size.x>=1&&state.Footprint.Size.y>=1&&state.Footprint.Size.x<=64&&state.Footprint.Size.y<=64,"Unsupported vehicle footprint");
        Need(UnitVehicleMovementUtility.IsVehicle(state.Footprint,state.Behavior),"Actual vehicle category required; no infantry coercion");
        var pose=state.Transform;var m=state.Movement;
        Need(math.all(math.isfinite(pose.Position))&&math.all(math.isfinite(pose.Rotation.value))&&Finite(pose.Scale)&&pose.Scale>0,"Explicit finite pose required");
        Need(Finite(m.TurnSpeedDegrees)&&m.TurnSpeedDegrees>=0&&Finite(m.Acceleration)&&Finite(m.Braking)&&Finite(m.RearPivotOffset)&&Finite(state.Kinematics.CurrentSpeed)&&Finite(state.Kinematics.StallSeconds),"Explicit captured vehicle tuning/kinematics required");
        Need(math.all(math.isfinite(input.Direction))&&Finite(input.TurnThresholdRadians)&&input.TurnThresholdRadians>=0&&input.TurnThresholdRadians<=MathF.PI&&Finite(input.MaxSpeed)&&input.MaxSpeed>=0&&input.MaxSpeed<=1000&&Finite(input.DeltaTime)&&input.DeltaTime>=0&&input.DeltaTime<=1&&Finite(input.MaxDistance)&&input.MaxDistance>=0&&input.MaxDistance<=10000,"Unsupported explicit steering input");
        // Preserve the original behavior exactly, including turn-in-place, deadband,
        // immediate CurrentSpeed assignment, and zero-distance return semantics.
        // Acceleration/braking/pivot fields are carried unchanged: the original
        // utility does not consume them. Do not invent a different vehicle model.
        return UnitVehicleMovementUtility.MoveVehicle(ref state.Transform,ref state.Kinematics,state.Movement,input.Direction,input.TurnThresholdRadians,input.MaxSpeed,input.DeltaTime,input.MaxDistance);
    }
}
