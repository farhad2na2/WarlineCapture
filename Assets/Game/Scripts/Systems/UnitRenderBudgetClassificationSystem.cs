using Unity.Collections;
using Unity.Entities;

public readonly struct UnitRenderBudgetClassificationSystem
{
    public bool IsCharacterUnit(EntityManager em, Entity unit)
    {
        if (em.HasComponent<UnitMovementBehavior>(unit) &&
            em.GetComponentData<UnitMovementBehavior>(unit).UsesVehicleMotion != 0)
        {
            return false;
        }

        if (!em.HasComponent<UnitSourcePrefabKey>(unit))
            return false;

        FixedString64Bytes key = em.GetComponentData<UnitSourcePrefabKey>(unit).Value;
        return key.ToString().StartsWith("Unit_Chr_", System.StringComparison.Ordinal);
    }
}
