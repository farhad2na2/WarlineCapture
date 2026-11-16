using Game.Components;
using Game.Runtime;
using Game.Skirmish.Contracts;
using Unity.Collections;
using Unity.Entities;

// Logical source command only: never a UI contact, pointer or native receipt.
internal static class SourceS004SelectedAircraftReturn
{
    internal static bool TryReadSelected(EntityManager em, Entity session, byte faction, out Entity actor)
    {
        actor = Entity.Null;
        if (em == null || faction != 1 || !em.Exists(session)) return false;
        using var selected = em.CreateEntityQuery(typeof(SelectedUnitTag));
        using var all = selected.ToEntityArray(Allocator.Temp);
        if (all.Length != 1) return false;
        var candidate = all[0];
        if (!SkirmishActorCommandAuthority.IsActiveOwnedActor(em, session, faction, candidate) ||
            !em.HasComponent<UnitAirMovement>(candidate) || !em.HasComponent<UnitAirComponent>(candidate) ||
            !em.HasComponent<SkirmishUnitRoleComponent>(candidate) ||
            em.GetComponentData<SkirmishUnitRoleComponent>(candidate).Category != SkirmishPopulationCategory.Air)
            return false;
        actor = candidate;
        return true;
    }

    internal static bool TryIssue(EntityManager em, Entity session, byte faction, out Entity actor)
    {
        if (!TryReadSelected(em, session, faction, out actor)) return false;
        if (UnitReturnToBaseCommandService.TryIssueOwned(em, session, faction, actor)) return true;
        actor = Entity.Null;
        return false;
    }
}
