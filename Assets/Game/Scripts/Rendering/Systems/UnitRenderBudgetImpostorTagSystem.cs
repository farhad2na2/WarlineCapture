using Unity.Collections;
using Unity.Entities;

public readonly struct UnitRenderBudgetImpostorTag
{
    public void CollectUnitImpostorTagRequest(
        EntityManager em,
        Entity unit,
        bool shouldShowFar,
        NativeList<Entity> unitsToShowDetailed,
        NativeList<Entity> unitsToShowFarImpostor,
        ref int changed)
    {
        bool farImpostor = em.HasComponent<UnitRenderBudgetCulledUnitTag>(unit);
        if (!shouldShowFar && farImpostor)
        {
            unitsToShowDetailed.Add(unit);
            changed++;
        }
        else if (shouldShowFar && !farImpostor)
        {
            unitsToShowFarImpostor.Add(unit);
            changed++;
        }
    }

    public void CollectUnitImpostorTagRequest(
        Entity unit,
        bool shouldShowFar,
        ComponentLookup<UnitRenderBudgetCulledUnitTag> culledUnitLookup,
        NativeList<Entity> unitsToShowDetailed,
        NativeList<Entity> unitsToShowFarImpostor,
        ref int changed)
    {
        bool farImpostor = culledUnitLookup.HasComponent(unit);
        if (!shouldShowFar && farImpostor)
        {
            unitsToShowDetailed.Add(unit);
            changed++;
        }
        else if (shouldShowFar && !farImpostor)
        {
            unitsToShowFarImpostor.Add(unit);
            changed++;
        }
    }
}
