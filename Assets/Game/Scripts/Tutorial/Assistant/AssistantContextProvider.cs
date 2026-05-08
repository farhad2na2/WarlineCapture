using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

public sealed class AssistantContextProvider
{
    private readonly World _world;
    private readonly TacticalMapRuntimeLoader _loader;
    private readonly RTSSelectionSystem _selectionSystem;
    private readonly BattleHudGameplayBridge _gameplayBridge;
    private readonly WarlineCaptureRouter _router;
    private readonly WarlineCaptureMatchResultFlow _resultFlow;
    private readonly MatchObjectivePanelController _objectivePanel;

    public AssistantContextProvider()
    {
    }

    public AssistantContextProvider(
        World world,
        TacticalMapRuntimeLoader loader,
        RTSSelectionSystem selectionSystem,
        BattleHudGameplayBridge gameplayBridge,
        WarlineCaptureRouter router = null,
        WarlineCaptureMatchResultFlow resultFlow = null,
        MatchObjectivePanelController objectivePanel = null)
    {
        _world = world;
        _loader = loader;
        _selectionSystem = selectionSystem;
        _gameplayBridge = gameplayBridge;
        _router = router;
        _resultFlow = resultFlow;
        _objectivePanel = objectivePanel;
    }

    public AssistantContext BuildContext(TutorialSessionState sessionState = null)
    {
        World world = ResolveWorld();
        TacticalMapRuntimeLoader loader = ResolveLoader();
        RTSSelectionSystem selectionSystem = _selectionSystem ?? RTSSelectionSystem.Instance;
        BattleHudGameplayBridge bridge = _gameplayBridge ?? BattleHudGameplayBridge.ResolveActive();
        WarlineCaptureRouter router = _router ?? ResolveActiveRouter();
        WarlineCaptureMatchResultFlow resultFlow = _resultFlow ?? ResolveResultFlow();
        MatchObjectivePanelController objectivePanel = _objectivePanel ?? ResolveObjectivePanel();

        var context = new AssistantContext
        {
            ActiveRoute = ResolveActiveRoute(router),
            MissionId = WarlineCaptureMissionSession.ActiveMissionId,
            ScenarioSetupId = WarlineCaptureMissionSession.ActiveScenarioSetupId,
            LevelId = WarlineCaptureMissionSession.ActiveLevelId,
            IsoMapId = WarlineCaptureMissionSession.ActiveIsoMapId,
            MapPreviewArtId = WarlineCaptureMissionSession.ActiveMapPreviewArtId,
            MinimapArtId = WarlineCaptureMissionSession.ActiveMinimapArtId,
            IsMatchOverlayActive = ResolveMatchOverlayActive(router),
            ObjectivePanelVisible = ResolveObjectivePanelVisible(objectivePanel, router),
            ResultPopupVisible = resultFlow != null && resultFlow.HasActivePopup,
            AssistanceLevel = "FullGuidance",
            AssistantMuted = false,
            CurrentControlOwnerState = ResolveControlOwnerState(sessionState)
        };

        ApplyRuntimeEntityState(context, sessionState, world, loader, selectionSystem);
        ApplyLatestCommandResult(context, sessionState, bridge);
        return context;
    }

    private void ApplyRuntimeEntityState(
        AssistantContext context,
        TutorialSessionState sessionState,
        World world,
        TacticalMapRuntimeLoader loader,
        RTSSelectionSystem selectionSystem)
    {
        bool hasWorld = world != null && world.IsCreated;
        bool hasLoader = loader != null && loader.Definition != null;
        bool activeM01 = context.IsM01Active && Chapter01M01PlayableRuntime.IsActiveMission();

        context.MoveTargetAvailable = activeM01 &&
            hasLoader &&
            loader.TryGetAnchorCell(M01AssistantIds.MoveTargetAnchorId, out _);

        if (!hasWorld)
        {
            context.TypedCommandHooksAvailable = false;
            return;
        }

        EntityManager em = world.EntityManager;
        Entity playerSquad = ResolveMissionEntity(em, M01AssistantIds.PlayerSquadEntityId);
        Entity enemyPatrol = ResolveMissionEntity(em, M01AssistantIds.EnemyPatrolEntityId);

        bool squadSpawned = IsExisting(em, playerSquad);
        bool squadAlive = IsAlive(em, playerSquad);
        bool squadCommandable = IsCommandablePlayerSquad(em, playerSquad);
        bool squadSelected = squadSpawned && em.HasComponent<SelectedUnitTag>(playerSquad);

        bool patrolSpawned = IsExisting(em, enemyPatrol);
        bool patrolAlive = IsAlive(em, enemyPatrol);
        bool patrolAttackable = IsAttackableEnemy(em, enemyPatrol);

        context.CommandSquadSpawned = squadSpawned;
        context.CommandSquadAlive = squadAlive;
        context.CommandSquadSelected = squadSelected;
        context.EnemyPatrolSpawned = patrolSpawned;
        context.EnemyPatrolDestroyed = patrolSpawned && !patrolAlive;
        context.EnemyPatrolVisible = patrolAttackable;
        context.MoveCommandAccepted = (sessionState != null && sessionState.M01MoveCommandAccepted) ||
            IsMoveCommandAccepted(em, playerSquad, loader);
        context.AttackCommandAccepted = (sessionState != null && sessionState.M01AttackCommandAccepted) ||
            IsAttackCommandAccepted(em, playerSquad, enemyPatrol);
        context.M01ResultExplained = sessionState != null && sessionState.M01ResultExplained;
        context.TypedCommandHooksAvailable = activeM01 &&
            selectionSystem != null &&
            squadCommandable &&
            context.MoveTargetAvailable &&
            patrolSpawned;

        if (sessionState == null)
            return;

        if (context.MoveCommandAccepted)
            sessionState.MarkM01MoveCommandAccepted();
        if (context.AttackCommandAccepted || context.EnemyPatrolDestroyed)
            sessionState.MarkM01AttackCommandAccepted();
    }

    private static void ApplyLatestCommandResult(
        AssistantContext context,
        TutorialSessionState sessionState,
        BattleHudGameplayBridge bridge)
    {
        context.CurrentCommandMode = bridge != null ? bridge.CurrentCommandMode : TacticalCommandMode.None;

        if (bridge == null || !bridge.HasLastCommandResult)
        {
            context.LastCommandResultAccepted = true;
            context.LastCommandReasonCode = TacticalCommandReasonCode.None;
            context.LastCommandReasonText = string.Empty;
            return;
        }

        TacticalCommandResult result = bridge.LastCommandResult;
        context.LastCommandResultAccepted = result.Accepted;
        context.LastCommandReasonCode = result.ReasonCode;
        context.LastCommandReasonText = !string.IsNullOrWhiteSpace(result.Message)
            ? result.Message
            : TacticalCommandFeedbackText.ToDisplayText(result.ReasonCode);

        if (!result.Accepted)
            sessionState?.RecordRejectedCommand(result.ReasonCode, ResolveCurrentStepId(context, sessionState));
    }

    private World ResolveWorld()
    {
        return _world ?? World.DefaultGameObjectInjectionWorld;
    }

    private TacticalMapRuntimeLoader ResolveLoader()
    {
        if (_loader != null)
            return _loader;

        TacticalMapRuntimeLoader[] loaders = Resources.FindObjectsOfTypeAll<TacticalMapRuntimeLoader>();
        for (int i = 0; i < loaders.Length; i++)
        {
            TacticalMapRuntimeLoader loader = loaders[i];
            if (loader != null && loader.gameObject.scene.IsValid())
                return loader;
        }

        return null;
    }

    private static WarlineCaptureRouter ResolveActiveRouter()
    {
        WarlineCaptureRouter[] routers = Resources.FindObjectsOfTypeAll<WarlineCaptureRouter>();
        for (int i = 0; i < routers.Length; i++)
        {
            WarlineCaptureRouter router = routers[i];
            if (router != null && router.gameObject.scene.IsValid() && router.HasActiveRoute)
                return router;
        }

        return null;
    }

    private static WarlineCaptureMatchResultFlow ResolveResultFlow()
    {
        WarlineCaptureMatchResultFlow[] flows = Resources.FindObjectsOfTypeAll<WarlineCaptureMatchResultFlow>();
        for (int i = 0; i < flows.Length; i++)
        {
            WarlineCaptureMatchResultFlow flow = flows[i];
            if (flow != null && flow.gameObject.scene.IsValid())
                return flow;
        }

        return null;
    }

    private static MatchObjectivePanelController ResolveObjectivePanel()
    {
        MatchObjectivePanelController[] panels = Resources.FindObjectsOfTypeAll<MatchObjectivePanelController>();
        for (int i = 0; i < panels.Length; i++)
        {
            MatchObjectivePanelController panel = panels[i];
            if (panel != null && panel.gameObject.scene.IsValid())
                return panel;
        }

        return null;
    }

    private static string ResolveActiveRoute(WarlineCaptureRouter router)
    {
        if (router != null && router.HasActiveRoute)
            return router.ActiveRoute.ToString();

        return InitialUnitsRuntimeState.PlayRequested ? WarlineCaptureRoute.Match.ToString() : string.Empty;
    }

    private static bool ResolveMatchOverlayActive(WarlineCaptureRouter router)
    {
        return InitialUnitsRuntimeState.PlayRequested ||
            router != null && router.HasActiveRoute && router.ActiveRoute == WarlineCaptureRoute.Match;
    }

    private static bool ResolveObjectivePanelVisible(MatchObjectivePanelController objectivePanel, WarlineCaptureRouter router)
    {
        if (objectivePanel != null)
            return objectivePanel.gameObject.activeInHierarchy;

        return ResolveMatchOverlayActive(router) && WarlineCaptureMissionSession.HasActiveMission;
    }

    private static AssistantControlOwnerState ResolveControlOwnerState(TutorialSessionState sessionState)
    {
        if (sessionState == null)
            return AssistantControlOwnerState.Player;
        if (!string.IsNullOrEmpty(sessionState.ActiveTakeoverIntentId))
            return AssistantControlOwnerState.AssistantTakeover;
        if (!string.IsNullOrEmpty(sessionState.ActivePreviewIntentId))
            return AssistantControlOwnerState.AssistantPreview;
        if (!string.IsNullOrEmpty(sessionState.ActiveRecommendationId))
            return AssistantControlOwnerState.Guided;

        return AssistantControlOwnerState.Player;
    }

    private static Entity ResolveMissionEntity(EntityManager em, string runtimeEntityId)
    {
        if (string.IsNullOrWhiteSpace(runtimeEntityId))
            return Entity.Null;

        using EntityQuery query = em.CreateEntityQuery(ComponentType.ReadOnly<MissionRuntimeEntityId>());
        using NativeArray<Entity> entities = query.ToEntityArray(Allocator.Temp);
        for (int i = 0; i < entities.Length; i++)
        {
            Entity entity = entities[i];
            if (!em.Exists(entity))
                continue;

            MissionRuntimeEntityId id = em.GetComponentData<MissionRuntimeEntityId>(entity);
            if (id.Value.ToString() == runtimeEntityId)
                return entity;
        }

        return Entity.Null;
    }

    private static bool IsExisting(EntityManager em, Entity entity)
    {
        return entity != Entity.Null && em.Exists(entity);
    }

    private static bool IsAlive(EntityManager em, Entity entity)
    {
        return IsExisting(em, entity) &&
            (!em.HasComponent<UnitHealth>(entity) || em.GetComponentData<UnitHealth>(entity).Current > 0);
    }

    private static bool IsCommandablePlayerSquad(EntityManager em, Entity entity)
    {
        return IsAlive(em, entity) &&
            em.HasComponent<Faction>(entity) &&
            em.GetComponentData<Faction>(entity).Id == 0 &&
            em.HasComponent<UnitMove>(entity);
    }

    private static bool IsAttackableEnemy(EntityManager em, Entity entity)
    {
        return IsAlive(em, entity) &&
            em.HasComponent<Faction>(entity) &&
            em.GetComponentData<Faction>(entity).Id != 0 &&
            em.HasComponent<LocalTransform>(entity);
    }

    private static bool IsMoveCommandAccepted(EntityManager em, Entity playerSquad, TacticalMapRuntimeLoader loader)
    {
        if (!IsExisting(em, playerSquad) ||
            loader == null ||
            !loader.TryGetAnchorCell(M01AssistantIds.MoveTargetAnchorId, out Vector2Int coverCell))
        {
            return false;
        }

        int2 expected = new(coverCell.x, coverCell.y);
        return em.HasComponent<UnitTarget>(playerSquad) && em.GetComponentData<UnitTarget>(playerSquad).Cell.Equals(expected) ||
            em.HasComponent<UnitPathRequest>(playerSquad) && em.GetComponentData<UnitPathRequest>(playerSquad).Goal.Equals(expected);
    }

    private static bool IsAttackCommandAccepted(EntityManager em, Entity playerSquad, Entity enemyPatrol)
    {
        if (!IsExisting(em, playerSquad) || !IsExisting(em, enemyPatrol) || !em.HasComponent<EngageTarget>(playerSquad))
            return false;

        EngageTarget engageTarget = em.GetComponentData<EngageTarget>(playerSquad);
        return engageTarget.Target == enemyPatrol && engageTarget.IsCommanded != 0;
    }

    private static string ResolveCurrentStepId(AssistantContext context, TutorialSessionState sessionState)
    {
        if (context.CommandSquadSpawned && !context.CommandSquadSelected)
            return M01AssistantIds.SelectSquadStepId;
        if (context.CommandSquadSelected && context.MoveTargetAvailable && sessionState != null && !sessionState.IsStepCompleted(M01AssistantIds.MoveStepId))
            return M01AssistantIds.MoveStepId;
        if (context.EnemyPatrolSpawned && context.EnemyPatrolVisible && !context.EnemyPatrolDestroyed)
            return M01AssistantIds.AttackStepId;
        if (context.ResultPopupVisible || context.EnemyPatrolDestroyed)
            return M01AssistantIds.CompleteStepId;

        return M01AssistantIds.ObjectivesStepId;
    }
}
