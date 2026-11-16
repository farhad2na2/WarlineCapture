using System;
using Game.Components;
using Game.UI.Contracts;
using Game.UI.Shell.Contracts.Ecs;
using Game.UI.Shell.Ecs;
using Unity.Entities;

internal static class S073LegacyTrayProjectionFixture
{
    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }

    private static Entity AddUnit(EntityManager em, Entity session, int faction, byte slot, int current, int max)
    {
        var entity = em.CreateEntity(typeof(SkirmishSquadMember), typeof(UnitHealth), typeof(SkirmishAttemptOwnedComponent));
        em.SetComponentData(entity, new SkirmishSquadMember { Slot = slot });
        em.SetComponentData(entity, new UnitHealth { Current = current, Max = max });
        em.SetComponentData(entity, new SkirmishAttemptOwnedComponent
        {
            SessionId = em.GetComponentData<SkirmishExpandedSessionComponent>(session).SessionId,
            StableObjectId = new Unity.Collections.FixedString64Bytes("fixture-" + entity.Index),
            FactionId = (byte)faction
        });
        return entity;
    }

    public static void Main()
    {
        var em = new EntityManager();
        int nextEntity = 1;
        em.LogicalAllocatorAdmitted = true;
        em.AllocateLogicalIdentity = () => new Entity { Index = nextEntity++, Version = 1 };
        var session = em.CreateEntity(typeof(SkirmishExpandedSessionComponent));
        em.SetComponentData(session, new SkirmishExpandedSessionComponent
        {
            SessionId = new Unity.Collections.FixedString64Bytes("S073-fixture"),
            IsLegacy = 1
        });
        var boundary = em.CreateEntity(typeof(UiMatchHudSquadTrayStateComponent));
        em.SetComponentData(boundary, new UiMatchHudSquadTrayStateComponent
        {
            SelectedSlot = MatchHudSquadTraySlot.CombatVehicles
        });

        // Mixed factions intentionally share the legacy slot/health query shape.
        AddUnit(em, session, 1, 0, 50, 100);
        AddUnit(em, session, 2, 0, 50, 50);
        AddUnit(em, session, 1, 1, 25, 100);
        AddUnit(em, session, 1, 1, 0, 100); // dead member is skipped
        AddUnit(em, session, 2, 4, 75, 100);
        AddUnit(em, session, 1, 5, 100, 100); // out-of-range slot is skipped

        LegacyTrayProjectionHost.Bind(em, boundary, session);
        Require(LegacyTrayProjectionHost.TryReadMatchHudSquadTray(out var tray),
            "Original tray projection did not return for the fixture boundary");
        var c0 = tray.GetCard(0);
        var c1 = tray.GetCard(1);
        var c2 = tray.GetCard(2);
        var c4 = tray.GetCard(4);
        Require(c0.Visible && c0.Title.EndsWith("(2)") && c0.HealthText == "100/150" &&
                Math.Abs(c0.Health01 - (100f / 150f)) < 0.0001f,
            "Legacy slot 0 must aggregate both factions and sum current/max health");
        Require(c1.Visible && c1.Title.EndsWith("(1)") && c1.HealthText == "25/100" &&
                Math.Abs(c1.Health01 - 0.25f) < 0.0001f,
            "Legacy slot 1 must skip zero-health entities and preserve fractional health");
        Require(c2.Visible && c2.Title.EndsWith("(0)") && c2.HealthText == "0/0" && c2.Health01 == 0f,
            "Legacy empty slots remain visible but contain no available health");
        Require(c4.Visible && c4.Title.EndsWith("(1)") && c4.HealthText == "75/100" &&
                Math.Abs(c4.Health01 - 0.75f) < 0.0001f,
            "Legacy slot 4 must preserve its surviving member health");
        Require(!tray.GetCard(5).Visible,
            "Legacy tray projection exposes exactly five slots");

        Console.WriteLine("[S073LegacyTrayProjectionFixture] result=Passed path=legacy_ecs_query mixedFactionSlot0Alive=2 deadSkipped=1 outOfRangeSkipped=1 emptyCardsVisible=1 cards=5 ui=stubbed_data_boundary=1 gameplay=0 native=0");
    }
}
