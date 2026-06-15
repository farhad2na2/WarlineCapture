using System.Collections.Generic;
using Unity.Entities;

public sealed class SelectionStateSystem
{
    public Entity FocusedUnit { get; private set; } = Entity.Null;
    public List<Entity> CachedSelectedMoveEntities { get; } = new();
    public string LastSelectionLifecycleDebug { get; private set; } = "none";

    public void SetFocusedUnit(Entity entity)
    {
        FocusedUnit = entity;
    }

    public void ClearFocusedUnit()
    {
        FocusedUnit = Entity.Null;
    }

    public void ClearSelectedMoveCache()
    {
        CachedSelectedMoveEntities.Clear();
    }

    public void RecordSelectionLifecycleDebug(string message)
    {
        LastSelectionLifecycleDebug = message ?? "none";
    }

    public void CacheSelectedMoveEntities(EntityManager entityManager, IReadOnlyList<Entity> entities)
    {
        CachedSelectedMoveEntities.Clear();
        if (entities == null)
            return;

        for (int i = 0; i < entities.Count; i++)
            CacheSelectedMoveEntity(entityManager, entities[i]);
    }

    public void CacheSelectedMoveEntity(EntityManager entityManager, Entity entity)
    {
        if (!IsCacheableSelectedMoveEntity(entityManager, entity))
            return;
        if (CachedSelectedMoveEntities.Contains(entity))
            return;

        CachedSelectedMoveEntities.Add(entity);
    }

    public static bool IsCacheableSelectedMoveEntity(EntityManager entityManager, Entity entity)
    {
        return entityManager.Exists(entity) &&
               entityManager.HasComponent<Faction>(entity) &&
               FactionIdentity.IsPlayerControlled(entityManager.GetComponentData<Faction>(entity).Id) &&
               entityManager.HasComponent<UnitGrid>(entity) &&
               entityManager.HasComponent<UnitMove>(entity) &&
               !entityManager.HasComponent<Disabled>(entity) &&
               !entityManager.HasComponent<UnitTransportPassenger>(entity);
    }
}
