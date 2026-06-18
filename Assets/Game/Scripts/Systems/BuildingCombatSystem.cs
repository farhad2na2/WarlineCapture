using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

public sealed partial class BuildingCombatSystem : SystemBase
{
    private readonly List<int> _destroyedCleanupIdsScratch = new();

    protected override void OnCreate()
    {
        Enabled = false;
    }

    protected override void OnUpdate()
    {
    }

    public enum RuntimeCombatState : byte
    {
        Active = 0,
        MissingCombatEntity = 1,
        DeadCombatEntity = 2
    }

    public interface IRuntimeBuilding
    {
        int Id { get; }
        bool IsDestroyed { get; set; }
        float DestroyedCleanupAt { get; set; }
        Entity CombatEntity { get; set; }
        Entity BlockerEntity { get; set; }
    }

    public interface IRuntimeBuildingVisualState : IRuntimeBuilding
    {
        GameObject InstanceObject { get; }
        IReadOnlyList<Transform> AliveVisualRootTransforms { get; }
    }

    public delegate bool TryGetEntityManagerDelegate(out EntityManager entityManager);
    public delegate void BuildingAction<TBuilding>(TBuilding building)
        where TBuilding : class, IRuntimeBuildingVisualState;
    public delegate void BuildingIdAction(int buildingId);
    public delegate void ObjectAction(Object target);
    public delegate void TransformVisibilityAction(Transform target, bool visible);
    public delegate void LogAction(string message);

    public readonly struct Context<TBuilding>
        where TBuilding : class, IRuntimeBuildingVisualState
    {
        public readonly RuntimeBuildingCollection<TBuilding> RuntimeBuildingSystem;
        public readonly IReadOnlyDictionary<int, TBuilding> RuntimeBuildings;
        public readonly Dictionary<int, TBuilding> RuntimeBuildingMap;
        public readonly TryGetEntityManagerDelegate TryGetEntityManager;
        public readonly BuildingAction<TBuilding> RememberOpenBaseBreach;
        public readonly BuildingIdAction NotifyHomeBuildingDestroyed;
        public readonly BuildingDestroyedVisualSystem DestroyedVisualSystem;
        public readonly BuildingDestroyedVisualSystem.Context DestroyedVisualContext;
        public readonly ObjectAction DestroyObject;
        public readonly System.Action RefreshBuildingMarkerVisibility;
        public readonly System.Action NotifyStaticMinimapChanged;
        public readonly LogAction Log;
        public readonly bool EnableDestroyDiagnostics;

        public Context(
            RuntimeBuildingCollection<TBuilding> runtimeBuildingSystem,
            IReadOnlyDictionary<int, TBuilding> runtimeBuildings,
            TryGetEntityManagerDelegate tryGetEntityManager,
            BuildingAction<TBuilding> rememberOpenBaseBreach,
            BuildingIdAction notifyHomeBuildingDestroyed,
            BuildingDestroyedVisualSystem destroyedVisualSystem,
            BuildingDestroyedVisualSystem.Context destroyedVisualContext,
            ObjectAction destroyObject,
            System.Action refreshBuildingMarkerVisibility,
            System.Action notifyStaticMinimapChanged,
            LogAction log,
            bool enableDestroyDiagnostics)
        {
            RuntimeBuildingSystem = runtimeBuildingSystem;
            RuntimeBuildings = runtimeBuildings;
            RuntimeBuildingMap = runtimeBuildings as Dictionary<int, TBuilding>;
            TryGetEntityManager = tryGetEntityManager;
            RememberOpenBaseBreach = rememberOpenBaseBreach;
            NotifyHomeBuildingDestroyed = notifyHomeBuildingDestroyed;
            DestroyedVisualSystem = destroyedVisualSystem;
            DestroyedVisualContext = destroyedVisualContext;
            DestroyObject = destroyObject;
            RefreshBuildingMarkerVisibility = refreshBuildingMarkerVisibility;
            NotifyStaticMinimapChanged = notifyStaticMinimapChanged;
            Log = log;
            EnableDestroyDiagnostics = enableDestroyDiagnostics;
        }
    }

    public bool TryMarkDestroyed(IRuntimeBuilding building, float now, float destroyedLifetimeSeconds)
    {
        if (building == null || building.IsDestroyed)
            return false;

        building.IsDestroyed = true;
        building.DestroyedCleanupAt = now + Mathf.Max(0f, destroyedLifetimeSeconds);
        return true;
    }

    public List<int> CollectDestroyedCleanupIds<TBuilding>(IReadOnlyDictionary<int, TBuilding> buildings, float now)
        where TBuilding : class, IRuntimeBuilding
    {
        if (buildings == null || buildings.Count == 0)
            return null;

        if (buildings is Dictionary<int, TBuilding> buildingMap)
            return CollectDestroyedCleanupIds(buildingMap, now);

        List<int> cleanupIds = null;
        foreach (KeyValuePair<int, TBuilding> entry in buildings)
        {
            TBuilding building = entry.Value;
            if (building == null || !building.IsDestroyed || now < building.DestroyedCleanupAt)
                continue;

            cleanupIds ??= new List<int>();
            cleanupIds.Add(entry.Key);
        }

        return cleanupIds;
    }

    public List<int> CollectDestroyedCleanupIds<TBuilding>(Dictionary<int, TBuilding> buildings, float now)
        where TBuilding : class, IRuntimeBuilding
    {
        if (buildings == null || buildings.Count == 0)
            return null;

        List<int> cleanupIds = null;
        foreach (KeyValuePair<int, TBuilding> entry in buildings)
        {
            TBuilding building = entry.Value;
            if (building == null || !building.IsDestroyed || now < building.DestroyedCleanupAt)
                continue;

            cleanupIds ??= new List<int>();
            cleanupIds.Add(entry.Key);
        }

        return cleanupIds;
    }

    public RuntimeCombatState ResolveRuntimeCombatState(IRuntimeBuilding building, EntityManager entityManager)
    {
        if (building == null || building.IsDestroyed || building.CombatEntity == Entity.Null)
            return RuntimeCombatState.Active;

        if (!entityManager.Exists(building.CombatEntity))
            return RuntimeCombatState.MissingCombatEntity;

        if (!entityManager.HasComponent<UnitHealth>(building.CombatEntity))
            return RuntimeCombatState.Active;

        UnitHealth health = entityManager.GetComponentData<UnitHealth>(building.CombatEntity);
        return health.Current <= 0 ? RuntimeCombatState.DeadCombatEntity : RuntimeCombatState.Active;
    }

    public void DestroyBlockerEntity(IRuntimeBuilding building, EntityManager entityManager)
    {
        if (building == null)
            return;

        Entity blockerEntity = building.BlockerEntity;
        if (blockerEntity != Entity.Null && entityManager.Exists(blockerEntity))
            entityManager.DestroyEntity(blockerEntity);

        building.BlockerEntity = Entity.Null;
    }

    public bool DeleteBuilding<TBuilding>(Context<TBuilding> context, int buildingId, bool destroyVisual, float now, float destroyedLifetimeSeconds)
        where TBuilding : class, IRuntimeBuildingVisualState
    {
        if (context.RuntimeBuildingSystem == null ||
            !context.RuntimeBuildingSystem.TryGetBuilding(buildingId, out TBuilding building))
        {
            return false;
        }

        if (destroyVisual && BeginDestroyedBuildingState(context, building, now, destroyedLifetimeSeconds))
            return true;

        DestroyRuntimeBuildingEntities(context, building);

        if (destroyVisual)
            DestroyRuntimeBuildingObject(context, building.InstanceObject);

        context.RuntimeBuildingSystem.RemoveBuilding(buildingId);
        context.RefreshBuildingMarkerVisibility?.Invoke();
        context.NotifyStaticMinimapChanged?.Invoke();
        return true;
    }

    public void HandleRuntimeBuildingEntityDestroyed<TBuilding>(
        Context<TBuilding> context,
        int buildingId,
        Entity blockerEntity,
        GameObject buildingObject)
        where TBuilding : class, IRuntimeBuildingVisualState
    {
        if (context.RuntimeBuildingSystem != null &&
            context.RuntimeBuildingSystem.TryGetBuilding(buildingId, out TBuilding destroyedBuilding) &&
            destroyedBuilding != null &&
            destroyedBuilding.IsDestroyed)
        {
            DestroyEntity(context, blockerEntity);
            destroyedBuilding.CombatEntity = Entity.Null;
            destroyedBuilding.BlockerEntity = Entity.Null;
            return;
        }

        if (context.RuntimeBuildingSystem != null &&
            (context.RuntimeBuildingSystem.SelectedBuildingId == buildingId ||
             context.RuntimeBuildingSystem.ActiveBuildingId == buildingId))
        {
            context.RuntimeBuildingSystem.ClearSelection();
        }

        context.NotifyHomeBuildingDestroyed?.Invoke(buildingId);
        if (context.EnableDestroyDiagnostics)
            context.Log?.Invoke($"[BuildingDestroyed] runtimeEntity buildingId={buildingId}");

        DestroyEntity(context, blockerEntity);
        context.RuntimeBuildingSystem?.RemoveBuilding(buildingId);
        DestroyRuntimeBuildingObject(context, buildingObject);
        context.RefreshBuildingMarkerVisibility?.Invoke();
    }

    public void UpdateDestroyedBuildings<TBuilding>(Context<TBuilding> context, float now)
        where TBuilding : class, IRuntimeBuildingVisualState
    {
        _destroyedCleanupIdsScratch.Clear();
        if (context.RuntimeBuildingMap != null)
            CollectDestroyedCleanupIds(context.RuntimeBuildingMap, now, _destroyedCleanupIdsScratch);
        else
            CollectDestroyedCleanupIds(context.RuntimeBuildings, now, _destroyedCleanupIdsScratch);

        if (_destroyedCleanupIdsScratch.Count == 0)
            return;

        for (int i = 0; i < _destroyedCleanupIdsScratch.Count; i++)
            FinalizeDestroyedBuilding(context, _destroyedCleanupIdsScratch[i]);

        _destroyedCleanupIdsScratch.Clear();
    }

    private static void CollectDestroyedCleanupIds<TBuilding>(
        IReadOnlyDictionary<int, TBuilding> buildings,
        float now,
        List<int> cleanupIds)
        where TBuilding : class, IRuntimeBuilding
    {
        if (buildings == null || buildings.Count == 0 || cleanupIds == null)
            return;

        foreach (KeyValuePair<int, TBuilding> entry in buildings)
            AddDestroyedCleanupId(entry, now, cleanupIds);
    }

    private static void CollectDestroyedCleanupIds<TBuilding>(
        Dictionary<int, TBuilding> buildings,
        float now,
        List<int> cleanupIds)
        where TBuilding : class, IRuntimeBuilding
    {
        if (buildings == null || buildings.Count == 0 || cleanupIds == null)
            return;

        foreach (KeyValuePair<int, TBuilding> entry in buildings)
            AddDestroyedCleanupId(entry, now, cleanupIds);
    }

    private static void AddDestroyedCleanupId<TBuilding>(
        KeyValuePair<int, TBuilding> entry,
        float now,
        List<int> cleanupIds)
        where TBuilding : class, IRuntimeBuilding
    {
        TBuilding building = entry.Value;
        if (building == null || !building.IsDestroyed || now < building.DestroyedCleanupAt)
            return;

        cleanupIds.Add(entry.Key);
    }

    public void SyncDestroyedRuntimeBuildingCombatEntities<TBuilding>(Context<TBuilding> context, float now, float destroyedLifetimeSeconds)
        where TBuilding : class, IRuntimeBuildingVisualState
    {
        if (context.RuntimeBuildings == null ||
            context.RuntimeBuildings.Count == 0 ||
            context.TryGetEntityManager == null ||
            !context.TryGetEntityManager(out EntityManager em))
        {
            return;
        }

        if (context.RuntimeBuildingMap != null)
        {
            foreach (KeyValuePair<int, TBuilding> entry in context.RuntimeBuildingMap)
                SyncDestroyedRuntimeBuildingCombatEntity(context, entry.Value, em, now, destroyedLifetimeSeconds);
            return;
        }

        foreach (KeyValuePair<int, TBuilding> entry in context.RuntimeBuildings)
            SyncDestroyedRuntimeBuildingCombatEntity(context, entry.Value, em, now, destroyedLifetimeSeconds);
    }

    private void SyncDestroyedRuntimeBuildingCombatEntity<TBuilding>(
        Context<TBuilding> context,
        TBuilding building,
        EntityManager em,
        float now,
        float destroyedLifetimeSeconds)
        where TBuilding : class, IRuntimeBuildingVisualState
    {
        if (building == null || building.IsDestroyed || building.CombatEntity == Entity.Null)
            return;

        RuntimeCombatState combatState = ResolveRuntimeCombatState(building, em);
        if (combatState == RuntimeCombatState.MissingCombatEntity)
        {
            BeginDestroyedBuildingState(context, building, now, destroyedLifetimeSeconds);
            building.CombatEntity = Entity.Null;
            return;
        }

        if (combatState == RuntimeCombatState.DeadCombatEntity)
            BeginDestroyedBuildingState(context, building, now, destroyedLifetimeSeconds);
    }

    public bool BeginDestroyedBuildingState<TBuilding>(Context<TBuilding> context, TBuilding building, float now, float destroyedLifetimeSeconds)
        where TBuilding : class, IRuntimeBuildingVisualState
    {
        if (!TryMarkDestroyed(building, now, destroyedLifetimeSeconds))
            return false;

        context.NotifyHomeBuildingDestroyed?.Invoke(building.Id);
        context.RememberOpenBaseBreach?.Invoke(building);
        DestroyRuntimeBuildingBlockerEntity(context, building);

        if (context.RuntimeBuildingSystem != null &&
            (context.RuntimeBuildingSystem.SelectedBuildingId == building.Id ||
             context.RuntimeBuildingSystem.ActiveBuildingId == building.Id))
        {
            context.RuntimeBuildingSystem.ClearSelection();
        }

        if (building is RuntimeBuildingEntity runtimeBuilding)
            context.DestroyedVisualSystem?.BeginDestroyedVisual(context.DestroyedVisualContext, runtimeBuilding);
        context.RefreshBuildingMarkerVisibility?.Invoke();
        return true;
    }

    public void DestroyRuntimeBuildingBlockerEntity<TBuilding>(Context<TBuilding> context, TBuilding building)
        where TBuilding : class, IRuntimeBuildingVisualState
    {
        if (building == null)
            return;

        if (context.TryGetEntityManager == null || !context.TryGetEntityManager(out EntityManager em))
        {
            building.BlockerEntity = Entity.Null;
            return;
        }

        DestroyBlockerEntity(building, em);
    }

    public void FinalizeDestroyedBuilding<TBuilding>(Context<TBuilding> context, int buildingId)
        where TBuilding : class, IRuntimeBuildingVisualState
    {
        if (context.RuntimeBuildingSystem == null ||
            !context.RuntimeBuildingSystem.TryGetBuilding(buildingId, out TBuilding building))
        {
            return;
        }

        context.NotifyHomeBuildingDestroyed?.Invoke(buildingId);
        DestroyRuntimeBuildingEntities(context, building);
        context.RuntimeBuildingSystem.RemoveBuilding(buildingId);
        if (building is RuntimeBuildingEntity runtimeBuilding)
            context.DestroyedVisualSystem?.CleanupDestroyedVisual(context.DestroyedVisualContext, runtimeBuilding);
        DestroyRuntimeBuildingObject(context, building.InstanceObject);
        context.RefreshBuildingMarkerVisibility?.Invoke();
    }

    private static void DestroyRuntimeBuildingEntities<TBuilding>(Context<TBuilding> context, TBuilding building)
        where TBuilding : class, IRuntimeBuildingVisualState
    {
        if (building == null ||
            context.TryGetEntityManager == null ||
            !context.TryGetEntityManager(out EntityManager em))
        {
            return;
        }

        DestroyEntity(context, building.CombatEntity);
        DestroyEntity(context, building.BlockerEntity);
        building.CombatEntity = Entity.Null;
        building.BlockerEntity = Entity.Null;
    }

    private static void DestroyEntity<TBuilding>(Context<TBuilding> context, Entity entity)
        where TBuilding : class, IRuntimeBuildingVisualState
    {
        if (entity == Entity.Null ||
            context.TryGetEntityManager == null ||
            !context.TryGetEntityManager(out EntityManager em) ||
            !em.Exists(entity))
        {
            return;
        }

        em.DestroyEntity(entity);
    }

    private static void DestroyRuntimeBuildingObject<TBuilding>(Context<TBuilding> context, Object target)
        where TBuilding : class, IRuntimeBuildingVisualState
    {
        if (target == null)
            return;

        context.DestroyObject?.Invoke(target);
    }
}
