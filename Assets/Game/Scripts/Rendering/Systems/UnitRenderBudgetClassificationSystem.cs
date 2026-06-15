using Unity.Collections;
using Unity.Entities;

public readonly struct UnitRenderBudgetClassification
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
        return UnitImpostorVisualUtility.HasCharacterUnitPrefix(key);
    }

    public bool IsCharacterUnit(
        Entity unit,
        ComponentLookup<UnitMovementBehavior> movementBehaviorLookup,
        ComponentLookup<UnitSourcePrefabKey> sourcePrefabKeyLookup)
    {
        if (movementBehaviorLookup.HasComponent(unit) &&
            movementBehaviorLookup[unit].UsesVehicleMotion != 0)
        {
            return false;
        }

        if (!sourcePrefabKeyLookup.HasComponent(unit))
            return false;

        FixedString64Bytes key = sourcePrefabKeyLookup[unit].Value;
        return UnitImpostorVisualUtility.HasCharacterUnitPrefix(key);
    }
}
