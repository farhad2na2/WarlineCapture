using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using Game.Components;

internal static class ProjectionCheckpointControl
{
    private static void Need(bool value, string message)
    {
        if (!value) throw new InvalidOperationException(message);
    }

    private static void Run(Type control, string[] args)
    {
        MethodInfo main = control.GetMethod("Main", BindingFlags.Static | BindingFlags.NonPublic);
        object result = main.Invoke(null, main.GetParameters().Length == 0 ? null : new object[] { args });
        Need((int)result == 0, control.Name + " failed");
    }

    private static int Main(string[] args)
    {
        Need(args.Length == 14, "Expected nine movement and five combat getter inputs");
        Run(typeof(UnitMoveAuthoringProjectionControl), args[..9]);
        Run(typeof(UnitMoveAuthoringProjectionUnityMathControl), args[..4]);
        Run(typeof(UnitCombatHealthAuthoringProjectionControl), args[9..]);
        Run(typeof(UnitAttackAuthoringProjectionControl), Array.Empty<string>());

        var vehicle = UnitMoveAuthoringProjection.FromAuthoredMovement(
            10f, 2f, 1f, .05f, 1, 1, true, true, false, true, 4f, 6f);
        Need(vehicle.Footprint.Size.x == 4 && vehicle.Footprint.Size.y == 6,
            "Vehicle footprint must use supplied model dimensions");
        Need(vehicle.VehicleMovement.Acceleration == 18f && vehicle.VehicleMovement.Braking == 24f,
            "Vehicle kinematics must retain authored geometry scaling");
        var configured = UnitMoveAuthoringProjection.FromAuthoredMovement(
            10f, 2f, 1f, .05f, 2, 3, true, true, false, true, 4f, 6f);
        Need(configured.Footprint.Size.x == 2 && configured.Footprint.Size.y == 3,
            "Explicit nontrivial footprints must survive automatic footprint mode");
        bool rejected = false;
        try { UnitMoveAuthoringProjection.FromAuthoredMovementWithoutModelBounds(
            10f, 2f, 1f, .05f, 1, 1, true, true, false); }
        catch (ArgumentException) { rejected = true; }
        Need(rejected, "Vehicle-only boundary must reject absent model bounds");

        var prefab = BuildingDefinitionAuthoringScalarProjection.Project(
            new BuildingDefinitionAuthoringScalarSource(false, 90, 2, 3, 5, 7, 2f, 0, 0, 0, 0f));
        Need(prefab.MaxHealth == 90 && prefab.Price == 5 && prefab.MaterialsCost == 7 &&
            prefab.ProductionDurationSeconds == 2f && prefab.FootprintWidth == 2 && prefab.FootprintDepth == 3,
            "Building without config must retain its serialized scalar values");
        var config = BuildingDefinitionAuthoringScalarProjection.Project(
            new BuildingDefinitionAuthoringScalarSource(true, 90, 2, 3, 5, 7, 2f, 200, -7, 12, -4f));
        Need(config.MaxHealth == 200 && config.Price == 0 && config.MaterialsCost == 12 &&
            config.ProductionDurationSeconds == .01f && config.FootprintWidth == 2,
            "Configured building values must retain existing clamp and footprint policy");

        var raw = UnitProductionMetadataScalarPolicy.Project(new UnitProductionMetadataScalarSource(
            true, false, false, true, true, false, true, false, -1f, -2f, -3f, -4, 3, 2));
        Need(raw.IsAirUnit && !raw.ProductionTransportRequiresAirportRunway &&
            !raw.ProductionTransportUsesRunwayLanding && raw.ProductionDurationSeconds == .01f &&
            raw.ProductionTransportArrivalSeconds == .01f && raw.ProductionTransportHoldForNextReadySeconds == .01f &&
            raw.ProductionTransportMaxConcurrent == 1 && raw.FootprintWidth == 3 && raw.FootprintDepth == 2,
            "Unit raw input must preserve config selection and existing getter floors");
        var resolved = UnitProductionMetadataScalarPolicy.Project(new UnitProductionMetadataScalarSource(
            true, true, false, true, true, false, false, true, float.NaN, float.PositiveInfinity, -0f, 3, 2, 4));
        Need(!resolved.IsAirUnit && resolved.ProductionTransportRequiresAirportRunway &&
            !resolved.ProductionTransportUsesRunwayLanding && float.IsNaN(resolved.ProductionDurationSeconds) &&
            float.IsPositiveInfinity(resolved.ProductionTransportArrivalSeconds) &&
            BitConverter.SingleToInt32Bits(resolved.ProductionTransportHoldForNextReadySeconds) == unchecked((int)0x80000000),
            "Resolved getter snapshots must retain identity, nonfinite values and signed zero");

        Need(!RuntimeHelpers.IsReferenceOrContainsReferences<UnitMoveAuthoringProjection>() &&
            !RuntimeHelpers.IsReferenceOrContainsReferences<UnitSurfaceAuthoringProjection>() &&
            !RuntimeHelpers.IsReferenceOrContainsReferences<BuildingDefinitionAuthoringScalarSource>() &&
            !RuntimeHelpers.IsReferenceOrContainsReferences<BuildingDefinitionAuthoringScalarMetadata>() &&
            !RuntimeHelpers.IsReferenceOrContainsReferences<UnitProductionMetadataScalarSource>() &&
            !RuntimeHelpers.IsReferenceOrContainsReferences<UnitProductionMetadataScalarValues>(),
            "Portable data values must not contain managed references");
        Console.WriteLine("[SharedGameplayProjectionControls] result=Passed scope=CompiledAuthoringDataOnly nativeBurstExecution=NotTested");
        return 0;
    }
}
