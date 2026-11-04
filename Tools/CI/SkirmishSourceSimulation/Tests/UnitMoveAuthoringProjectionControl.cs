using System;
using System.Globalization;
using Game.Components;

internal static class UnitMoveAuthoringProjectionControl
{
    private static int Main(string[] args)
    {
        if (args.Length != 9)
            return Fail("expected four float fields and five exact movement fields");

        float speed = Parse(args[0]);
        float walk = Parse(args[1]);
        float road = Parse(args[2]);
        float arrive = Parse(args[3]);
        UnitMoveAuthoringProjection authored = UnitMoveAuthoringProjection.FromAuthoredValues(speed, walk, road, arrive);
        Require(authored.Speed == speed, "authored speed preserved");
        Require(authored.WalkSpeed == walk, "authored rifle walk speed preserved when below max speed");
        Require(authored.RoadSpeedMultiplier == road, "authored road multiplier preserved");
        Require(authored.ArriveDistance == arrive, "authored arrive distance preserved");

        int footprintX = int.Parse(args[4], CultureInfo.InvariantCulture);
        int footprintY = int.Parse(args[5], CultureInfo.InvariantCulture);
        bool autoFootprint = bool.Parse(args[6]);
        bool vehicle = bool.Parse(args[7]);
        bool idleWander = bool.Parse(args[8]);
        UnitMoveAuthoringProjection movement = UnitMoveAuthoringProjection.FromAuthoredMovementWithoutModelBounds(
            speed, walk, road, arrive, footprintX, footprintY, autoFootprint, vehicle, idleWander);
        Require(movement.HasMovementSlice, "complete movement slice projected");
        Require(movement.Footprint.Size.x == Math.Max(1, footprintX) && movement.Footprint.Size.y == Math.Max(1, footprintY),
            "authored nonvehicle footprint projected with original minimum");
        Require(movement.MovementBehavior.AllowIdleWander == (idleWander ? 1 : 0) &&
            movement.MovementBehavior.UsesVehicleMotion == (vehicle ? 1 : 0), "authored movement flags projected");
        Require(movement.VehicleMovement.TurnSpeedDegrees == 720f && movement.VehicleMovement.Acceleration == 999f &&
            movement.VehicleMovement.Braking == 999f && movement.VehicleMovement.RearPivotOffset == 0f,
            "original nonvehicle motion values projected");
        Require(movement.VehicleKinematics.CurrentSpeed == 0f && movement.VehicleKinematics.StallSeconds == 0f,
            "original initial vehicle kinematics projected");

        Check(-2f, -3f, 1.25f, 0.2f, -2f, 0.01f, "negative speed and walk clamp");
        Check(0.005f, 2f, 0.8f, -1f, 0.005f, 0.01f, "subminimum speed keeps raw speed");
        Check(3f, 8f, 1.4f, 0.05f, 3f, 3f, "walk speed capped at speed");
        Check(4f, 0f, 1f, 0f, 4f, 0.01f, "zero walk gets minimum");

        UnitSurfaceAuthoringProjectionControl.Verify();
        Console.WriteLine("[UnitMoveAndSurfaceAuthoringProjectionControl] result=Passed");
        return 0;
    }

    private static float Parse(string value) => float.Parse(value, CultureInfo.InvariantCulture);

    private static void Check(float speed, float walk, float road, float arrive, float expectedSpeed, float expectedWalk, string label)
    {
        UnitMoveAuthoringProjection actual = UnitMoveAuthoringProjection.FromAuthoredValues(speed, walk, road, arrive);
        Require(actual.Speed == expectedSpeed, label + " speed");
        Require(actual.WalkSpeed == expectedWalk, label + " walk");
        Require(actual.RoadSpeedMultiplier == road, label + " road pass-through");
        Require(actual.ArriveDistance == arrive, label + " arrive pass-through");
    }

    private static void Require(bool condition, string label)
    {
        if (!condition)
            throw new InvalidOperationException("assertion failed: " + label);
    }

    private static int Fail(string message)
    {
        Console.Error.WriteLine("[UnitMoveAuthoringProjectionControl] result=Failed " + message);
        return 1;
    }
}
