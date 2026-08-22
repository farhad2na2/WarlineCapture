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
            tests.InitialRebuildMarksUnitFootprint();
            tests.ChangedUnitGridMovesOccupancy();
            tests.DeathAnimationStateRemovesUnitFootprintFromOccupancy();
            Debug.Log("[DynamicOccupancyRebuildFocusedValidation] result=Passed tests=3");
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
