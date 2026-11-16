using System;
using System.Collections.Generic;
using Game.Components;
using Game.Runtime;
using Unity.Entities;
using Unity.Mathematics;

internal static class S005RuntimeOverlapFixture
{
    static void Need(bool condition, string label) { if (!condition) throw new InvalidOperationException(label); }
    public static void Main()
    {
        var em = new EntityManager();
        int next = 1;
        em.LogicalAllocatorAdmitted = true;
        em.AllocateLogicalIdentity = () => new Entity { Index = next++, Version = 1 };
        var buildings = new Dictionary<int, RuntimeBuildingEntity>();
        var overlap = new S005RuntimeOverlapOwner(em, buildings);
        var unitRect = new RectInt(10, 10, 1, 1);
        Need(!overlap.Overlaps(unitRect), "empty world");
        var unit = em.CreateEntity();
        em.AddComponentData(unit, new UnitGrid { Cell = new int2(10, 10) });
        em.AddComponentData(unit, new UnitFootprint { Size = new int2(1, 1) });
        Need(overlap.OverlapsUnits(unitRect), "live unit");
        Need(!overlap.OverlapsUnits(new RectInt(20, 20, 1, 1)), "unit outside rectangle");
        em.AddComponentData(unit, new StaticGridBlocker());
        Need(!overlap.OverlapsUnits(unitRect), "static non-map blocker excluded");
        em.RemoveComponent<StaticGridBlocker>(unit);
        em.AddComponentData(unit, new RuntimeBuildingCombatTag());
        Need(!overlap.OverlapsUnits(unitRect), "runtime building proxy excluded");
        em.RemoveComponent<RuntimeBuildingCombatTag>(unit);
        em.AddComponentData(unit, new OperationMapBuildingComponent());
        em.AddComponentData(unit, new RuntimeBuildingCombatInfo { OriginCell = new int2(30, 30), FootprintCells = new int2(2, 3) });
        Need(!overlap.OverlapsUnits(unitRect) && overlap.OverlapsUnits(new RectInt(31, 32, 1, 1)), "map building uses original footprint");
        var definition = new RuntimeDefinition { SourceMetadataComplete = true, FootprintCells = new Vector2Int(3, 4) };
        buildings.Add(1, new RuntimeBuildingEntity { Definition = definition, OriginCell = new Vector2Int(50, 50) });
        Need(overlap.OverlapsBuildings(new RectInt(52, 53, 1, 1)), "registered ordinary building");
        buildings[1].IsDestroyed = true;
        Need(!overlap.OverlapsBuildings(new RectInt(52, 53, 1, 1)), "destroyed building excluded");
        buildings[1].IsDestroyed = false;
        definition.HasRunway = true;
        try { overlap.OverlapsBuildings(new RectInt(52, 53, 1, 1)); throw new InvalidOperationException("runway must fail closed"); }
        catch (NotSupportedException) { }
        Console.WriteLine("[S005RuntimeOverlapFixture] result=Passed cases=9 scope=SyntheticOnly");
    }
}
