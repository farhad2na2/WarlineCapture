using Game.Components;
using Game.Runtime;
#if UNITY_INCLUDE_TESTS && UNITY_EDITOR
using NUnit.Framework;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;

public sealed class DynamicOccupancyRebuildSystemTests
{
    [MenuItem("Game/Validation/Run Dynamic Occupancy Focused")]
    public static void RunFocusedValidation()
    {
        try
        {
            var tests = new DynamicOccupancyRebuildSystemTests();
            tests.StationaryUnitKeepsOccupancyAcrossUpdates();
            tests.MovingOneUnitKeepsStationaryNeighborOccupied();
            tests.EqualCountReplacementReconcilesOccupancy();
            tests.SameSizeReplacementGridRebuildsStationaryOccupancy();
            tests.InitialRebuildMarksUnitFootprint();
            tests.ChangedUnitGridMovesOccupancy();
            tests.DeathAnimationStateRemovesUnitFootprintFromOccupancy();
            Debug.Log("[DynamicOccupancyRebuildFocusedValidation] result=Passed tests=7");
            ValidationExit.Exit(0);
        }
        catch (System.Exception exception)
        {
            Debug.LogException(exception);
            Debug.LogError("[DynamicOccupancyRebuildFocusedValidation] result=Failed");
            ValidationExit.Exit(1);
        }
    }

    [Test]
    public void InitialRebuildMarksUnitFootprint()
    {
        using World world = new("DynamicOccupancyRebuildSystemTests");
        EntityManager em = world.EntityManager;
        Entity gridEntity = CreateGrid(em);
        Entity unit = em.CreateEntity(typeof(UnitGrid), typeof(UnitFootprint));
        em.SetComponentData(unit, new UnitGrid { Cell = new int2(2, 2) });
        em.SetComponentData(unit, new UnitFootprint { Size = new int2(1, 1) });

        SystemHandle system = world.CreateSystem<DynamicOccupancyRebuildSystem>();
        system.Update(world.Unmanaged);
        em.CompleteAllTrackedJobs();

        DynamicOccupancyComponent occupancy = em.GetComponentData<DynamicOccupancyComponent>(gridEntity);
        Assert.IsTrue(occupancy.Occupied.IsSet(GridUtils.CellToIndex(new int2(2, 2), 6)));
        DisposeOccupancy(em, gridEntity);
    }

    [Test]
    public void ChangedUnitGridMovesOccupancy()
    {
        using World world = new("DynamicOccupancyRebuildSystemTests");
        EntityManager em = world.EntityManager;
        Entity gridEntity = CreateGrid(em);
        Entity unit = em.CreateEntity(typeof(UnitGrid), typeof(UnitFootprint));
        em.SetComponentData(unit, new UnitGrid { Cell = new int2(1, 1) });
        em.SetComponentData(unit, new UnitFootprint { Size = new int2(1, 1) });

        SystemHandle system = world.CreateSystem<DynamicOccupancyRebuildSystem>();
        system.Update(world.Unmanaged);
        em.CompleteAllTrackedJobs();

        em.SetComponentData(unit, new UnitGrid { Cell = new int2(4, 3) });
        system.Update(world.Unmanaged);
        em.CompleteAllTrackedJobs();

        DynamicOccupancyComponent occupancy = em.GetComponentData<DynamicOccupancyComponent>(gridEntity);
        Assert.IsFalse(occupancy.Occupied.IsSet(GridUtils.CellToIndex(new int2(1, 1), 6)));
        Assert.IsTrue(occupancy.Occupied.IsSet(GridUtils.CellToIndex(new int2(4, 3), 6)));
        DisposeOccupancy(em, gridEntity);
    }

    [Test]
    public void DeathAnimationStateRemovesUnitFootprintFromOccupancy()
    {
        using World world = new(nameof(DeathAnimationStateRemovesUnitFootprintFromOccupancy));
        EntityManager em = world.EntityManager;
        Entity gridEntity = CreateGrid(em);
        Entity unit = em.CreateEntity(typeof(UnitGrid), typeof(UnitFootprint));
        int2 cell = new(2, 2);
        em.SetComponentData(unit, new UnitGrid { Cell = cell });
        em.SetComponentData(unit, new UnitFootprint { Size = new int2(1, 1) });

        SystemHandle system = world.CreateSystem<DynamicOccupancyRebuildSystem>();
        system.Update(world.Unmanaged);
        em.CompleteAllTrackedJobs();
        Assert.IsTrue(em.GetComponentData<DynamicOccupancyComponent>(gridEntity).Occupied.IsSet(
            GridUtils.CellToIndex(cell, 6)));

        em.AddComponentData(unit, new UnitDeathAnimationComponent { PoseFrozen = 1 });
        system.Update(world.Unmanaged);
        em.CompleteAllTrackedJobs();

        Assert.IsFalse(em.GetComponentData<DynamicOccupancyComponent>(gridEntity).Occupied.IsSet(
            GridUtils.CellToIndex(cell, 6)),
            "A retained corpse must not block pathfinding occupancy.");
        DisposeOccupancy(em, gridEntity);
    }

    [Test]
    public void StationaryUnitKeepsOccupancyAcrossUpdates()
    {
        using World world = new(nameof(StationaryUnitKeepsOccupancyAcrossUpdates));
        EntityManager em = world.EntityManager;
        Entity grid = CreateGrid(em);
        try
        {
            CreateUnit(em, new int2(2, 2));
            SystemHandle system = world.CreateSystem<DynamicOccupancyRebuildSystem>();
            for (int tick = 0; tick < 4; tick++)
            {
                system.Update(world.Unmanaged);
                em.CompleteAllTrackedJobs();
                AssertOccupied(em, grid, new int2(2, 2), true,
                    "A stationary live unit must not disappear from occupancy when no UnitGrid changes occur.");
            }
        }
        finally { DisposeOccupancy(em, grid); }
    }

    [Test]
    public void MovingOneUnitKeepsStationaryNeighborOccupied()
    {
        using World world = new(nameof(MovingOneUnitKeepsStationaryNeighborOccupied));
        EntityManager em = world.EntityManager;
        Entity grid = CreateGrid(em);
        try
        {
            Entity mover = CreateUnit(em, new int2(1, 1));
            CreateUnit(em, new int2(4, 4));
            SystemHandle system = world.CreateSystem<DynamicOccupancyRebuildSystem>();
            system.Update(world.Unmanaged);
            em.SetComponentData(mover, new UnitGrid { Cell = new int2(1, 4) });
            system.Update(world.Unmanaged);
            em.CompleteAllTrackedJobs();
            AssertOccupied(em, grid, new int2(1, 1), false, "The moved unit must release its old cell.");
            AssertOccupied(em, grid, new int2(1, 4), true, "The moved unit must occupy its new cell.");
            AssertOccupied(em, grid, new int2(4, 4), true,
                "A changed-unit filter must not turn stationary live neighbors into removed entities.");
        }
        finally { DisposeOccupancy(em, grid); }
    }

    [Test]
    public void EqualCountReplacementReconcilesOccupancy()
    {
        using World world = new(nameof(EqualCountReplacementReconcilesOccupancy));
        EntityManager em = world.EntityManager;
        Entity grid = CreateGrid(em);
        try
        {
            Entity removed = CreateUnit(em, new int2(1, 1));
            CreateUnit(em, new int2(4, 4));
            SystemHandle system = world.CreateSystem<DynamicOccupancyRebuildSystem>();
            system.Update(world.Unmanaged);
            em.DestroyEntity(removed);
            CreateUnit(em, new int2(3, 1));
            system.Update(world.Unmanaged);
            em.CompleteAllTrackedJobs();
            AssertOccupied(em, grid, new int2(1, 1), false, "An equal-count replacement must release the removed actor's cell.");
            AssertOccupied(em, grid, new int2(3, 1), true, "A new actor must be tracked even when total unit count is unchanged.");
            AssertOccupied(em, grid, new int2(4, 4), true, "Replacing one actor must preserve its stationary neighbor.");
        }
        finally { DisposeOccupancy(em, grid); }
    }

    [Test]
    public void SameSizeReplacementGridRebuildsStationaryOccupancy()
    {
        using World world = new(nameof(SameSizeReplacementGridRebuildsStationaryOccupancy));
        EntityManager em = world.EntityManager;
        Entity grid = CreateGrid(em);
        try
        {
            CreateUnit(em, new int2(2, 2));
            SystemHandle system = world.CreateSystem<DynamicOccupancyRebuildSystem>();
            system.Update(world.Unmanaged);
            DisposeOccupancy(em, grid);
            em.DestroyEntity(grid);
            grid = CreateGrid(em);
            system.Update(world.Unmanaged);
            em.CompleteAllTrackedJobs();
            AssertOccupied(em, grid, new int2(2, 2), true,
                "A same-size replacement grid must rebuild occupancy for retained stationary actors.");
        }
        finally { DisposeOccupancy(em, grid); }
    }

    private static Entity CreateUnit(EntityManager em, int2 cell)
    {
        Entity unit = em.CreateEntity(typeof(UnitGrid), typeof(UnitFootprint));
        em.SetComponentData(unit, new UnitGrid { Cell = cell });
        em.SetComponentData(unit, new UnitFootprint { Size = new int2(1, 1) });
        return unit;
    }

    private static void AssertOccupied(EntityManager em, Entity grid, int2 cell, bool expected, string reason)
    {
        Assert.AreEqual(expected, em.GetComponentData<DynamicOccupancyComponent>(grid).Occupied.IsSet(
            GridUtils.CellToIndex(cell, 6)), reason);
    }

    private static Entity CreateGrid(EntityManager em)
    {
        Entity entity = em.CreateEntity(typeof(GridConfig), typeof(DynamicOccupancyComponent));
        em.SetComponentData(entity, new GridConfig
        {
            Width = 6,
            Height = 6,
            CellSize = 1f,
            Origin = float3.zero
        });
        em.SetComponentData(entity, new DynamicOccupancyComponent
        {
            GridSize = 36,
            Occupied = new NativeBitArray(36, Allocator.Persistent, NativeArrayOptions.ClearMemory)
        });
        return entity;
    }

    private static void DisposeOccupancy(EntityManager em, Entity gridEntity)
    {
        if (!em.Exists(gridEntity) || !em.HasComponent<DynamicOccupancyComponent>(gridEntity))
            return;

        DynamicOccupancyComponent occupancy = em.GetComponentData<DynamicOccupancyComponent>(gridEntity);
        if (occupancy.Occupied.IsCreated)
            occupancy.Occupied.Dispose();
        em.SetComponentData(gridEntity, default(DynamicOccupancyComponent));
    }
}
#endif
