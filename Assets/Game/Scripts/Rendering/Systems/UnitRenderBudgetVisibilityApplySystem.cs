using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;

public readonly struct UnitRenderBudgetVisibilityApplySystem
{
    public readonly struct Result
    {
        public readonly int Shown;
        public readonly int Hidden;

        public Result(int shown, int hidden)
        {
            Shown = shown;
            Hidden = hidden;
        }
    }

    public Result Apply(
        EntityManager em,
        EntityCommandBuffer renderStateEcb,
        NativeList<Entity> unitsToShowDetailed,
        NativeList<Entity> unitsToShowFarImpostor,
        NativeList<Entity> entitiesToShow,
        NativeList<Entity> entitiesToHide)
    {
        int shown = 0;
        int hidden = 0;
        using NativeHashSet<Entity> farImpostorUnitRequests = new(math.max(1, unitsToShowFarImpostor.Length), Allocator.Temp);
        using NativeHashSet<Entity> hiddenEntityRequests = new(math.max(1, entitiesToHide.Length), Allocator.Temp);
        using NativeHashSet<Entity> scheduledCulledUnitAdds = new(math.max(1, unitsToShowFarImpostor.Length), Allocator.Temp);
        using NativeHashSet<Entity> scheduledCulledUnitRemoves = new(math.max(1, unitsToShowDetailed.Length), Allocator.Temp);
        using NativeHashSet<Entity> scheduledDisableRenderingAdds = new(math.max(1, entitiesToHide.Length), Allocator.Temp);
        using NativeHashSet<Entity> scheduledDisableRenderingRemoves = new(math.max(1, entitiesToShow.Length), Allocator.Temp);
        using NativeHashSet<Entity> scheduledDisabledRemoves = new(math.max(1, entitiesToShow.Length), Allocator.Temp);
        using NativeHashSet<Entity> scheduledCulledTagAdds = new(math.max(1, entitiesToHide.Length), Allocator.Temp);
        using NativeHashSet<Entity> scheduledCulledTagRemoves = new(math.max(1, entitiesToShow.Length), Allocator.Temp);

        for (int i = 0; i < unitsToShowFarImpostor.Length; i++)
            farImpostorUnitRequests.Add(unitsToShowFarImpostor[i]);

        for (int i = 0; i < entitiesToHide.Length; i++)
            hiddenEntityRequests.Add(entitiesToHide[i]);

        for (int i = 0; i < unitsToShowDetailed.Length; i++)
        {
            Entity unit = unitsToShowDetailed[i];
            if (farImpostorUnitRequests.Contains(unit))
                continue;

            if (em.Exists(unit) &&
                em.HasComponent<UnitRenderBudgetCulledUnitTag>(unit) &&
                scheduledCulledUnitRemoves.Add(unit))
            {
                renderStateEcb.RemoveComponent<UnitRenderBudgetCulledUnitTag>(unit);
            }
        }

        for (int i = 0; i < unitsToShowFarImpostor.Length; i++)
        {
            Entity unit = unitsToShowFarImpostor[i];
            if (em.Exists(unit) &&
                !em.HasComponent<UnitRenderBudgetCulledUnitTag>(unit) &&
                scheduledCulledUnitAdds.Add(unit))
            {
                renderStateEcb.AddComponent<UnitRenderBudgetCulledUnitTag>(unit);
            }
        }

        for (int i = 0; i < entitiesToShow.Length; i++)
        {
            Entity entity = entitiesToShow[i];
            if (!em.Exists(entity) || hiddenEntityRequests.Contains(entity))
                continue;

            if (em.HasComponent<Disabled>(entity) && scheduledDisabledRemoves.Add(entity))
                renderStateEcb.RemoveComponent<Disabled>(entity);
            if (em.HasComponent<DisableRendering>(entity) && scheduledDisableRenderingRemoves.Add(entity))
                renderStateEcb.RemoveComponent<DisableRendering>(entity);
            if (em.HasComponent<UnitRenderBudgetCulledTag>(entity) && scheduledCulledTagRemoves.Add(entity))
                renderStateEcb.RemoveComponent<UnitRenderBudgetCulledTag>(entity);
            shown++;
        }

        for (int i = 0; i < entitiesToHide.Length; i++)
        {
            Entity entity = entitiesToHide[i];
            if (!em.Exists(entity))
                continue;

            if (!em.HasComponent<DisableRendering>(entity) && scheduledDisableRenderingAdds.Add(entity))
                renderStateEcb.AddComponent<DisableRendering>(entity);
            if (!em.HasComponent<UnitRenderBudgetCulledTag>(entity) && scheduledCulledTagAdds.Add(entity))
                renderStateEcb.AddComponent<UnitRenderBudgetCulledTag>(entity);
            hidden++;
        }

        renderStateEcb.Playback(em);
        renderStateEcb.Dispose();
        return new Result(shown, hidden);
    }
}
