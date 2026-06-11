using Unity.Collections;
using Unity.Entities;
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

        for (int i = 0; i < unitsToShowDetailed.Length; i++)
        {
            Entity unit = unitsToShowDetailed[i];
            if (em.Exists(unit) && em.HasComponent<UnitRenderBudgetCulledUnitTag>(unit))
                em.RemoveComponent<UnitRenderBudgetCulledUnitTag>(unit);
        }

        for (int i = 0; i < unitsToShowFarImpostor.Length; i++)
        {
            Entity unit = unitsToShowFarImpostor[i];
            if (em.Exists(unit) && !em.HasComponent<UnitRenderBudgetCulledUnitTag>(unit))
                em.AddComponent<UnitRenderBudgetCulledUnitTag>(unit);
        }

        for (int i = 0; i < entitiesToShow.Length; i++)
        {
            Entity entity = entitiesToShow[i];
            if (!em.Exists(entity))
                continue;

            if (em.HasComponent<Disabled>(entity))
                em.RemoveComponent<Disabled>(entity);
            if (em.HasComponent<DisableRendering>(entity))
                em.RemoveComponent<DisableRendering>(entity);
            if (em.HasComponent<UnitRenderBudgetCulledTag>(entity))
                em.RemoveComponent<UnitRenderBudgetCulledTag>(entity);
            shown++;
        }

        for (int i = 0; i < entitiesToHide.Length; i++)
        {
            Entity entity = entitiesToHide[i];
            if (!em.Exists(entity))
                continue;

            if (!em.HasComponent<DisableRendering>(entity))
                em.AddComponent<DisableRendering>(entity);
            if (!em.HasComponent<UnitRenderBudgetCulledTag>(entity))
                em.AddComponent<UnitRenderBudgetCulledTag>(entity);
            hidden++;
        }

        renderStateEcb.Playback(em);
        renderStateEcb.Dispose();
        return new Result(shown, hidden);
    }
}
