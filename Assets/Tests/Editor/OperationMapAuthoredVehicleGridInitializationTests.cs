using Game.Components;
using Game.Runtime;
using NUnit.Framework;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

public sealed class OperationMapAuthoredVehicleGridInitializationTests
{
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
