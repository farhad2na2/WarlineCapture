using System;
using Game.Components;
using Game.Runtime;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

internal static class SourceGuardTowerControlFixture
{
    static void Need(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
    static void Reject(EntityManager em, string label)
    {
        try { SourceGuardTowerControl.Read(em); }
        catch (NotSupportedException) { return; }
        throw new InvalidOperationException("Expected rejection: " + label);
    }

    public static void Main()
    {
        var em = new EntityManager();
        int next = 1;
        em.LogicalAllocatorAdmitted = true;
        em.AllocateLogicalIdentity = () => new Entity { Index = next++, Version = 1 };
        Reject(em, "missing publication");
        var root = em.CreateEntity();
        em.AddComponentData(root, new BuildingRuntimeStateTag());
        var rows = em.AddBuffer<BuildingConfiguredSpawnableReadModel>(root);
        var tower = new BuildingConfiguredSpawnableReadModel {
            BuildingId = new FixedString128Bytes("building_guardtower"),
            DisplayName = new FixedString128Bytes("Guard Tower"),
            CanRequest = 1, MaterialsCost = 50, Price = 22000, FootprintCells = new int2(3, 4)
        };
        rows.Add(tower);
        Reject(em, "missing source authority marker");
        em.AddComponentData(root, new SourceConfiguredBuildingFactsKnown());
        var read = SourceGuardTowerControl.Read(em);
        Need((bool)read["can_request"] && (int)read["materials_cost"] == 50 &&
            (int)read["price"] == 22000 && !(bool)read["placement_command_bound"], "exact published row");
        rows.Add(tower);
        Reject(em, "duplicate guard tower");
        rows.RemoveAt(1);
        tower.FootprintCells = new int2(0, 4);
        rows[0] = tower;
        Reject(em, "invalid footprint");
        tower.FootprintCells = new int2(3, 4);
        tower.CanRequest = 2;
        rows[0] = tower;
        Reject(em, "invalid request flag");
        Console.WriteLine("[SourceGuardTowerControlFixture] result=Passed cases=5 scope=SyntheticOnly");
    }
}
