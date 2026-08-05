using Game.Components;
using Game.Editor;
using Game.Runtime;
using NUnit.Framework;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

public sealed class OperationMapAuthoredVehicleGridInitializationTests
{
    [Test]
    public void CandidateBakeOwnershipValidation_RequiresExactPlacementFactionParity()
    {
        using var world = new World(nameof(
            CandidateBakeOwnershipValidation_RequiresExactPlacementFactionParity));
        EntityManager entityManager = world.EntityManager;
        var expectedFactions = new byte[22];
        for (int placementIndex = 0; placementIndex < expectedFactions.Length; placementIndex++)
        {
            expectedFactions[placementIndex] = placementIndex == 9
                ? FactionIdentity.NeutralFactionId
                : FactionIdentity.PlayerFactionId;
            Entity vehicle = entityManager.CreateEntity(
                typeof(OperationMapAuthoredVehiclePresentation),
                typeof(Faction));
            entityManager.SetComponentData(vehicle, new OperationMapAuthoredVehiclePresentation
            {
                PlacementIndex = placementIndex,
                FactionId = expectedFactions[placementIndex]
            });
            entityManager.SetComponentData(vehicle, new Faction { Id = expectedFactions[placementIndex] });
        }

        Assert.That(
            OperationMapEntityPresentationCandidateBakeValidator.TryValidateVehicleOwnership(
                entityManager,
                expectedFactions,
                out string rejectionReason),
            Is.True,
            rejectionReason);

        using Unity.Collections.NativeArray<Entity> vehicles = entityManager.CreateEntityQuery(
                typeof(OperationMapAuthoredVehiclePresentation),
                typeof(Faction))
            .ToEntityArray(Unity.Collections.Allocator.Temp);
        Entity firstVehicle = vehicles[0];
        entityManager.SetComponentData(firstVehicle, new Faction { Id = FactionIdentity.NeutralFactionId });
        Assert.That(
            OperationMapEntityPresentationCandidateBakeValidator.TryValidateVehicleOwnership(
                entityManager,
                expectedFactions,
                out rejectionReason),
            Is.False);
        Assert.That(rejectionReason, Does.StartWith("vehicle-ownership-faction:"));
    }

    [Test]
    public void AuthoredVehicleDerivesGridCellWithoutChangingAcceptedTransform()
    {
        using var world = new World(nameof(
            AuthoredVehicleDerivesGridCellWithoutChangingAcceptedTransform));
        EntityManager entityManager = world.EntityManager;
        Entity gridEntity = entityManager.CreateEntity(typeof(GridConfig));
        entityManager.SetComponentData(gridEntity, new GridConfig
        {
            Width = 256,
            Height = 256,
            CellSize = 2f,
            Origin = new float3(-100f, 0f, -80f)
        });
        Entity vehicle = entityManager.CreateEntity(
            typeof(OperationMapAuthoredVehiclePresentation),
            typeof(UnitGrid),
            typeof(LocalTransform));
        LocalTransform acceptedTransform = LocalTransform.FromPositionRotationScale(
            new float3(37.25f, 4.5f, 63.75f),
            quaternion.RotateY(0.73f),
            1.2f);
        entityManager.SetComponentData(vehicle, new UnitGrid { Cell = int2.zero });
        entityManager.SetComponentData(vehicle, acceptedTransform);

        SystemHandle system = world.CreateSystem<UnitGridSnapSystem>();
        system.Update(world.Unmanaged);

        Assert.That(entityManager.HasComponent<UnitGridInitialized>(vehicle), Is.True);
        Assert.That(
            entityManager.GetComponentData<UnitGrid>(vehicle).Cell,
            Is.EqualTo(GridUtils.WorldToCell(
                entityManager.GetComponentData<GridConfig>(gridEntity),
                acceptedTransform.Position)));
        Assert.That(
            entityManager.GetComponentData<LocalTransform>(vehicle),
            Is.EqualTo(acceptedTransform));
    }

    [Test]
    public void SpawnedUnitStillSnapsTransformToOwnedGridCell()
    {
        using var world = new World(nameof(SpawnedUnitStillSnapsTransformToOwnedGridCell));
        EntityManager entityManager = world.EntityManager;
        Entity gridEntity = entityManager.CreateEntity(typeof(GridConfig));
        GridConfig grid = new()
        {
            Width = 64,
            Height = 64,
            CellSize = 2f,
            Origin = new float3(-10f, 0f, -20f)
        };
        entityManager.SetComponentData(gridEntity, grid);
        Entity unit = entityManager.CreateEntity(
            typeof(UnitGrid),
            typeof(LocalTransform));
        int2 cell = new(12, 17);
        entityManager.SetComponentData(unit, new UnitGrid { Cell = cell });
        entityManager.SetComponentData(
            unit,
            LocalTransform.FromPosition(new float3(100f, 8f, 100f)));

        SystemHandle system = world.CreateSystem<UnitGridSnapSystem>();
        system.Update(world.Unmanaged);

        Assert.That(entityManager.HasComponent<UnitGridInitialized>(unit), Is.True);
        Assert.That(
            entityManager.GetComponentData<LocalTransform>(unit).Position,
            Is.EqualTo(GridUtils.CellToWorldCenter(grid, cell)));
    }
}
