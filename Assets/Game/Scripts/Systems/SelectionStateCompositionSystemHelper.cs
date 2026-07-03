using System.Collections.Generic;
using Unity.Entities;
using Game.Components;

namespace Game.Runtime
{
    public sealed class SelectionStateCompositionSystemHelper
    {
        private readonly List<Entity> _pendingSelectedMoveEntities = new();

        public Entity FocusedUnit { get; private set; } = Entity.Null;
        public List<Entity> CachedSelectedMoveEntities { get; } = new();
        public int SelectionVersion { get; private set; }
        public string LastSelectionLifecycleDebug { get; private set; } = "none";

        public void SetFocusedUnit(Entity entity)
        {
            if (FocusedUnit == entity)
                return;

            FocusedUnit = entity;
            BumpSelectionVersion();
        }

        public void ClearFocusedUnit()
        {
            if (FocusedUnit == Entity.Null)
                return;

            FocusedUnit = Entity.Null;
            BumpSelectionVersion();
        }

        public void ClearSelectedMoveCache()
        {
            if (CachedSelectedMoveEntities.Count == 0)
                return;

            CachedSelectedMoveEntities.Clear();
            BumpSelectionVersion();
        }

        public void RecordSelectionLifecycleDebug(string message)
        {
            LastSelectionLifecycleDebug = message ?? "none";
        }

        public void CacheSelectedMoveEntities(EntityManager entityManager, IReadOnlyList<Entity> entities)
        {
            _pendingSelectedMoveEntities.Clear();
            if (entities != null)
            {
                for (int i = 0; i < entities.Count; i++)
                    CacheSelectedMoveEntity(entityManager, entities[i], _pendingSelectedMoveEntities);
            }

            if (ListsMatch(CachedSelectedMoveEntities, _pendingSelectedMoveEntities))
            {
                _pendingSelectedMoveEntities.Clear();
                return;
            }

            CachedSelectedMoveEntities.Clear();
            CachedSelectedMoveEntities.AddRange(_pendingSelectedMoveEntities);
            _pendingSelectedMoveEntities.Clear();
            BumpSelectionVersion();
        }

        public void CacheSelectedMoveEntity(EntityManager entityManager, Entity entity)
        {
            if (!IsCacheableSelectedMoveEntity(entityManager, entity))
                return;
            if (CachedSelectedMoveEntities.Contains(entity))
                return;

            CachedSelectedMoveEntities.Add(entity);
            BumpSelectionVersion();
        }

        private static void CacheSelectedMoveEntity(EntityManager entityManager, Entity entity, List<Entity> target)
        {
            if (!IsCacheableSelectedMoveEntity(entityManager, entity))
                return;
            if (target.Contains(entity))
                return;

            target.Add(entity);
        }

        private static bool ListsMatch(List<Entity> left, List<Entity> right)
        {
            if (left.Count != right.Count)
                return false;

            for (int i = 0; i < left.Count; i++)
            {
                if (left[i] != right[i])
                    return false;
            }

            return true;
        }

        private void BumpSelectionVersion()
        {
            unchecked
            {
                SelectionVersion++;
            }
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
}
