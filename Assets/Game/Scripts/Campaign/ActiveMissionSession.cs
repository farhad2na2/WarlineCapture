using System;
using Unity.Collections;
using Unity.Entities;

public sealed class ActiveMissionSession
{
    private readonly World _world;

    public ActiveMissionSession(World world = null)
    {
        _world = world;
    }

    public MissionConfig ActiveMission => TryGetActiveMission(out MissionConfig mission) ? mission : null;
    public WarlineCaptureRoute ReturnRoute => TryGetSession(out ActiveMissionSessionComponent session)
        ? (WarlineCaptureRoute)session.ReturnRoute
        : WarlineCaptureRoute.SagaMap;
    public bool HasActiveMission => ActiveMission != null;
    public string ActiveMissionId => ActiveMission?.MissionId ?? string.Empty;
    public string ActiveScenarioSetupId => ActiveMission?.ScenarioSetupId ?? string.Empty;
    public string ActiveLevelId => ActiveMission?.LevelId ?? string.Empty;
    public string ActiveIsoMapId => ActiveMission?.IsoMapId ?? string.Empty;
    public string ActiveMapPreviewArtId => ActiveMission?.MapPreviewArtId ?? string.Empty;
    public string ActiveMinimapArtId => ActiveMission?.MinimapArtId ?? string.Empty;

    public void BeginMission(string missionId, WarlineCaptureRoute returnRoute)
    {
        MissionConfig mission = ChapterOneMissionCatalog.GetMission(missionId);
        EntityManager em = ResolveEntityManager();
        Entity sessionEntity = ResolveOrCreateSessionEntity(em);
        em.SetComponentData(sessionEntity, new ActiveMissionSessionComponent
        {
            MissionId = new FixedString128Bytes(mission.MissionId),
            ReturnRoute = (int)returnRoute
        });
    }

#if UNITY_EDITOR
    public void BeginMissionForTests(MissionConfig mission, WarlineCaptureRoute returnRoute)
    {
        if (mission == null)
            throw new ArgumentNullException(nameof(mission));

        EntityManager em = ResolveEntityManager();
        Entity sessionEntity = ResolveOrCreateSessionEntity(em);
        em.SetComponentData(sessionEntity, new ActiveMissionSessionComponent
        {
            MissionId = new FixedString128Bytes(mission.MissionId),
            ReturnRoute = (int)returnRoute
        });
    }
#endif

    public void Clear()
    {
        if (!TryResolveEntityManager(out EntityManager em))
            return;

        using EntityQuery query = em.CreateEntityQuery(ComponentType.ReadOnly<ActiveMissionSessionComponent>());
        using NativeArray<Entity> entities = query.ToEntityArray(Allocator.Temp);
        for (int i = 0; i < entities.Length; i++)
            em.DestroyEntity(entities[i]);
    }

    public MissionResultData BuildCurrentResult(GameRuntimeStats.Snapshot snapshot)
    {
        MissionConfig mission = ActiveMission;
        if (mission == null)
            throw new InvalidOperationException("No active mission session is available.");

        return MissionResultBuilder.Build(mission, snapshot);
    }

    public MissionResultData CompleteCurrentMission(GameRuntimeStats.Snapshot snapshot)
    {
        MissionResultData result = BuildCurrentResult(snapshot);
        SagaProgressStore.ApplyMissionResult(result);
        return result;
    }

    public bool TryGetActiveMission(out MissionConfig mission)
    {
        mission = null;
        if (!TryGetSession(out ActiveMissionSessionComponent session))
            return false;

        string missionId = session.MissionId.ToString();
        if (string.IsNullOrWhiteSpace(missionId))
            return false;

        mission = ChapterOneMissionCatalog.GetMission(missionId);
        return mission != null;
    }

    private bool TryGetSession(out ActiveMissionSessionComponent session)
    {
        session = default;
        if (!TryResolveEntityManager(out EntityManager em))
            return false;

        using EntityQuery query = em.CreateEntityQuery(ComponentType.ReadOnly<ActiveMissionSessionComponent>());
        if (query.IsEmptyIgnoreFilter)
            return false;

        Entity entity = query.GetSingletonEntity();
        session = em.GetComponentData<ActiveMissionSessionComponent>(entity);
        return true;
    }

    private Entity ResolveOrCreateSessionEntity(EntityManager em)
    {
        using EntityQuery query = em.CreateEntityQuery(ComponentType.ReadWrite<ActiveMissionSessionComponent>());
        if (!query.IsEmptyIgnoreFilter)
            return query.GetSingletonEntity();

        Entity entity = em.CreateEntity(typeof(ActiveMissionSessionComponent));
        em.SetName(entity, "ActiveMissionSession");
        return entity;
    }

    private EntityManager ResolveEntityManager()
    {
        if (TryResolveEntityManager(out EntityManager em))
            return em;

        World world = new("ActiveMissionSessionWorld");
        World.DefaultGameObjectInjectionWorld = world;
        return world.EntityManager;
    }

    private bool TryResolveEntityManager(out EntityManager em)
    {
        World world = _world ?? World.DefaultGameObjectInjectionWorld;
        if (world == null || !world.IsCreated)
        {
            em = default;
            return false;
        }

        em = world.EntityManager;
        return true;
    }
}
