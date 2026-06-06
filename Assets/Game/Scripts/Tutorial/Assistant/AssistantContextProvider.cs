using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

public sealed class AssistantContextProvider
{
    private readonly World _world;
    private readonly BattleHudRuntimeFeedbackView _runtimeFeedbackView;
    private readonly WarlineCaptureRouter _router;
    private readonly WarlineCaptureMatchResultFlow _resultFlow;
    private readonly MatchObjectivePanelSystem _objectivePanel;
    private readonly RuntimeGameplayStateSystem _runtimeGameplayStateSystem = new();

    public AssistantContextProvider()
    {
    }

    public AssistantContextProvider(
        World world,
        BattleHudRuntimeFeedbackView runtimeFeedbackView,
        WarlineCaptureRouter router = null,
        WarlineCaptureMatchResultFlow resultFlow = null,
        MatchObjectivePanelSystem objectivePanel = null)
    {
        _world = world;
        _runtimeFeedbackView = runtimeFeedbackView;
        _router = router;
        _resultFlow = resultFlow;
        _objectivePanel = objectivePanel;
    }

    public AssistantContext BuildContext(TutorialSessionState sessionState = null)
    {
        World world = ResolveWorld();
        BattleHudRuntimeFeedbackView view = _runtimeFeedbackView ?? BattleHudRuntimeFeedbackSystem.ResolveActiveView();
        WarlineCaptureRouter router = _router ?? ResolveActiveRouter();
        WarlineCaptureMatchResultFlow resultFlow = _resultFlow ?? ResolveResultFlow();
        MatchObjectivePanelSystem objectivePanel = _objectivePanel ?? ResolveObjectivePanel();
        bool playRequested = _runtimeGameplayStateSystem.PlayRequested;

        var context = new AssistantContext
        {
            ActiveRoute = ResolveActiveRoute(router, playRequested),
            MissionId = WarlineCaptureMissionSession.ActiveMissionId,
            ScenarioSetupId = WarlineCaptureMissionSession.ActiveScenarioSetupId,
            LevelId = WarlineCaptureMissionSession.ActiveLevelId,
            IsoMapId = WarlineCaptureMissionSession.ActiveIsoMapId,
            MapPreviewArtId = WarlineCaptureMissionSession.ActiveMapPreviewArtId,
            MinimapArtId = WarlineCaptureMissionSession.ActiveMinimapArtId,
            IsMatchOverlayActive = ResolveMatchOverlayActive(router, playRequested),
            ObjectivePanelVisible = ResolveObjectivePanelVisible(objectivePanel, router, playRequested),
            ResultPopupVisible = resultFlow != null && resultFlow.HasActivePopup,
            AssistanceLevel = "FullGuidance",
            AssistantMuted = false,
            CurrentControlOwnerState = ResolveControlOwnerState(sessionState)
        };

        ApplyRuntimeEntityState(context, sessionState, world);
        ApplyLatestCommandResult(context, sessionState, view);
        return context;
    }

    private void ApplyRuntimeEntityState(
        AssistantContext context,
        TutorialSessionState sessionState,
        World world)
    {
        bool hasWorld = world != null && world.IsCreated;
        bool activeM01 = context.IsM01Active && Chapter01M01PlayableRuntime.IsActiveMission();

        context.MoveTargetAvailable = activeM01;

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
            IsMoveCommandAccepted(em, playerSquad);
        context.AttackCommandAccepted = (sessionState != null && sessionState.M01AttackCommandAccepted) ||
            IsAttackCommandAccepted(em, playerSquad, enemyPatrol);
        context.M01ResultExplained = sessionState != null && sessionState.M01ResultExplained;
        context.TypedCommandHooksAvailable = activeM01 &&
            M01AssistantCommandRuntime.HasTypedCommandHooks(world) &&
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
        BattleHudRuntimeFeedbackView view)
    {
        BattleHudRuntimeFeedbackState state = BattleHudRuntimeFeedbackSystem.GetState(view);
        context.CurrentCommandMode = state.CurrentCommandMode;

        if (!state.HasLastCommandResult)
        {
            context.LastCommandResultAccepted = true;
            context.LastCommandReasonCode = TacticalCommandReasonCode.None;
            context.LastCommandReasonText = string.Empty;
            return;
        }

        TacticalCommandResult result = state.LastCommandResult;
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

    private static MatchObjectivePanelSystem ResolveObjectivePanel()
    {
        MatchObjectivePanelSystem[] panels = Resources.FindObjectsOfTypeAll<MatchObjectivePanelSystem>();
        for (int i = 0; i < panels.Length; i++)
        {
            MatchObjectivePanelSystem panel = panels[i];
            if (panel != null && panel.gameObject.scene.IsValid())
                return panel;
        }

        return null;
    }

    private static string ResolveActiveRoute(WarlineCaptureRouter router, bool playRequested)
    {
        if (router != null && router.HasActiveRoute)
            return router.ActiveRoute.ToString();

        return playRequested ? WarlineCaptureRoute.Match.ToString() : string.Empty;
    }

    private static bool ResolveMatchOverlayActive(WarlineCaptureRouter router, bool playRequested)
    {
        return playRequested ||
            router != null && router.HasActiveRoute && router.ActiveRoute == WarlineCaptureRoute.Match;
    }

    private static bool ResolveObjectivePanelVisible(MatchObjectivePanelSystem objectivePanel, WarlineCaptureRouter router, bool playRequested)
    {
        if (objectivePanel != null)
            return objectivePanel.gameObject.activeInHierarchy;

        return ResolveMatchOverlayActive(router, playRequested) && WarlineCaptureMissionSession.HasActiveMission;
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
            FactionIdentitySystem.IsPlayerControlled(em.GetComponentData<Faction>(entity).Id) &&
            em.HasComponent<UnitMove>(entity);
    }

    private static bool IsAttackableEnemy(EntityManager em, Entity entity)
    {
        return IsAlive(em, entity) &&
            em.HasComponent<Faction>(entity) &&
            FactionIdentitySystem.IsHostileToPlayer(em.GetComponentData<Faction>(entity).Id) &&
            em.HasComponent<LocalTransform>(entity);
    }

    private static bool IsMoveCommandAccepted(EntityManager em, Entity playerSquad)
    {
        if (!IsExisting(em, playerSquad))
            return false;

        int2 expected = Chapter01M01PlayableRuntime.GetMoveToCoverCell();
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
