#if UNITY_INCLUDE_TESTS && UNITY_EDITOR
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using NUnit.Framework;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

public sealed class Chapter01M01PlayModeValidationTests
{
    private const string GameSceneName = "Game";

    [TearDown]
    public void TearDown()
    {
        InitialUnitsRuntimeState.PlayRequested = false;
        InitialUnitsRuntimeState.SelectionModeActive = false;
        InitialUnitsRuntimeState.BuildModeActive = false;
        InitialUnitsRuntimeState.FullscreenMapOpen = false;
        InitialUnitsRuntimeState.FullscreenMapIsoMode = false;
        InitialUnitsRuntimeState.ZoomInHeld = false;
        InitialUnitsRuntimeState.ZoomOutHeld = false;
        InitialUnitsRuntimeState.SuppressNextWorldClick = false;
        InitialUnitsRuntimeState.PlayerAutoModeEnabled = false;
        WarlineCaptureMissionSession.Clear();
        GameRuntimeStats.Reset();
        Time.timeScale = 1f;
        SetLogAssertIgnoreFailingMessages(false);
    }

    [Test]
    public async Task GameScene_M01RuntimeSpawnsVisibleAnchoredSquadsAndStartsAtCameraAnchor()
    {
        M01SceneContext context = await LoadM01SceneAndWaitForRuntime();
        EntityManager em = context.World.EntityManager;

        Assert.IsTrue(em.HasComponent<LocalToWorld>(context.PlayerSquad), "Player command squad should be a visible scene-spawned entity with LocalToWorld.");
        Assert.IsTrue(em.HasComponent<LocalToWorld>(context.EnemyPatrol), "Hostile patrol should be a visible scene-spawned entity with LocalToWorld.");
        Assert.IsFalse(em.HasComponent<Disabled>(context.PlayerSquad), "Player command squad should be active/visible.");
        Assert.IsFalse(em.HasComponent<Disabled>(context.EnemyPatrol), "Hostile patrol should be active/visible.");

        AssertNearAnchor(context.Loader, em, context.PlayerSquad, Chapter01M01PlayableRuntime.PlayerSpawnAnchorId);
        AssertNearAnchor(context.Loader, em, context.EnemyPatrol, Chapter01M01PlayableRuntime.EnemySpawnAnchorId);

        AssertTacticalGroundAndCameraFraming(context, "M01 loaded scene");
    }

    [Test]
    public async Task PublicQuickCustomLaunch_ReachesM01ProductionMatchRoute()
    {
        Time.timeScale = 12f;
        SetLogAssertIgnoreFailingMessages(true);

        GameBootstrap bootstrap = await LoadGameSceneAndWaitForBootstrap();
        WarlineCaptureUiBootstrap uiBootstrap = await WaitForParallelUiBootstrap();
        Assert.NotNull(uiBootstrap.AppCanvasInstance, "Parallel WarlineCapture app canvas should be instantiated by the public scene bootstrap.");

        WarlineCaptureRouter router = uiBootstrap.AppCanvasInstance.GetComponent<WarlineCaptureRouter>();
        Assert.NotNull(router, "WarlineCapture app canvas should expose the public router.");
        router.GoTo(WarlineCaptureRoute.QuickCustomSetup, false);

        Assert.IsTrue(router.TryGetRegisteredScreen(WarlineCaptureRoute.QuickCustomSetup, out WarlineCaptureScreenController quickCustomScreen), "Public router should register the Quick Custom setup screen.");
        QuickCustomScreenController quickCustom = quickCustomScreen as QuickCustomScreenController;
        Assert.NotNull(quickCustom, "Public app canvas should contain the Quick Custom setup screen.");
        quickCustom.LaunchMission();

        M01SceneContext context = await WaitForM01Runtime(bootstrap);

        Assert.AreEqual(ChapterOneMissionCatalog.FirstContactMissionId, WarlineCaptureMissionSession.ActiveMissionId);
        Assert.AreEqual(WarlineCaptureRoute.Match, router.ActiveRoute, "Quick Custom public launch should leave the WarlineCapture HUD/router on Match.");
        Assert.IsTrue(uiBootstrap.AppCanvasInstance.activeInHierarchy, "WarlineCapture app canvas must remain visible for the production M01 HUD.");
        Assert.IsFalse(IsLoadedObjectNamedActive("UI_Canvas"), "Legacy UI_Canvas must remain inactive for the production M01 public launch path.");
        Assert.AreEqual("iso.ch01.district_edge_01", context.Loader.Definition.MapId);
        Assert.IsTrue(context.World.EntityManager.HasComponent<MissionRuntimeSpritePresenter>(context.PlayerSquad), "Public launch should use the accepted M01 sprite-presenter direction.");
        Assert.IsTrue(context.World.EntityManager.HasComponent<MissionRuntimeSpritePresenter>(context.EnemyPatrol), "Public launch should use the accepted M01 sprite-presenter direction.");
        await WaitForMissionAtlasQuads(context);
        AssertM01ProductionPlayerVisibleState(context, "Quick Custom public launch");
        AssertM01InfantryOnlyHudScope(router, "Quick Custom public launch");
        CapturePlayerView(context, uiBootstrap.AppCanvasInstance, "quick-custom-public-m01");
    }

    [Test]
    public async Task PublicCampaignLaunch_ReachesM01ProductionVisibleSlice()
    {
        Time.timeScale = 12f;
        SetLogAssertIgnoreFailingMessages(true);

        GameBootstrap bootstrap = await LoadGameSceneAndWaitForBootstrap();
        WarlineCaptureUiBootstrap uiBootstrap = await WaitForParallelUiBootstrap();
        Assert.NotNull(uiBootstrap.AppCanvasInstance, "Parallel WarlineCapture app canvas should be instantiated by the public scene bootstrap.");

        WarlineCaptureRouter router = uiBootstrap.AppCanvasInstance.GetComponent<WarlineCaptureRouter>();
        Assert.NotNull(router, "WarlineCapture app canvas should expose the public router.");
        router.GoTo(WarlineCaptureRoute.SagaMap, false);

        Assert.IsTrue(router.TryGetRegisteredScreen(WarlineCaptureRoute.SagaMap, out WarlineCaptureScreenController sagaScreen), "Public router should register the Saga Map screen.");
        SagaMapScreenController sagaMap = sagaScreen.GetComponent<SagaMapScreenController>();
        Assert.NotNull(sagaMap, "Saga Map screen should expose its controller.");
        sagaMap.SelectMissionForTests(ChapterOneMissionCatalog.FirstContactMissionId);
        WarlineCaptureMissionSession.BeginMission(ChapterOneMissionCatalog.FirstContactMissionId, WarlineCaptureRoute.SagaMap);
        router.GoTo(WarlineCaptureRoute.MissionBriefing);
        await NextFrame();

        Assert.AreEqual(WarlineCaptureRoute.MissionBriefing, router.ActiveRoute, "First Contact selection should enter Mission Briefing.");
        router.GoTo(WarlineCaptureRoute.LoadoutSquadPrep);
        await NextFrame();

        Assert.AreEqual(WarlineCaptureRoute.LoadoutSquadPrep, router.ActiveRoute, "Mission Briefing should route into loadout.");
        WarlineCaptureGameLaunchUtility.StartExistingGameplayAndHideRouter(router);

        M01SceneContext context = await WaitForM01Runtime(bootstrap);

        Assert.AreEqual(ChapterOneMissionCatalog.FirstContactMissionId, WarlineCaptureMissionSession.ActiveMissionId);
        Assert.AreEqual(WarlineCaptureRoute.Match, router.ActiveRoute, "Campaign launch should leave the WarlineCapture HUD/router on Match.");
        Assert.IsTrue(uiBootstrap.AppCanvasInstance.activeInHierarchy, "WarlineCapture app canvas must remain visible for the production M01 HUD.");
        Assert.IsFalse(IsLoadedRootObjectActive("UI_Canvas"), "Legacy UI_Canvas must remain inactive for the production M01 public campaign launch.");
        Assert.AreEqual("iso.ch01.district_edge_01", context.Loader.Definition.MapId);
        Assert.IsTrue(context.World.EntityManager.HasComponent<MissionRuntimeSpritePresenter>(context.PlayerSquad), "Campaign launch should use the accepted M01 sprite-presenter direction.");
        Assert.IsTrue(context.World.EntityManager.HasComponent<MissionRuntimeSpritePresenter>(context.EnemyPatrol), "Campaign launch should use the accepted M01 sprite-presenter direction.");
        await WaitForMissionAtlasQuads(context);
        AssertM01ProductionPlayerVisibleState(context, "Campaign public launch");
        AssertM01InfantryOnlyHudScope(router, "Campaign public launch");
        CapturePlayerView(context, uiBootstrap.AppCanvasInstance, "campaign-public-m01");

        Assert.IsTrue(context.Bootstrap.Selection.TrySelectRuntimeEntity(context.PlayerSquad).Accepted, "V2 runtime proof should select the command squad.");
        await NextFrame();
        AssertSelectedPresentationVisible(context.World.EntityManager, context.PlayerSquad, "v2 runtime proof command squad idle");
        CaptureM01V2RuntimeProofView(context, uiBootstrap.AppCanvasInstance, "campaign-public-m01-v2-selected-player-idle", Entity.Null);

        IssueMoveToCover(context);
        for (int frame = 0; frame < 30; frame++)
            await NextFrame();
        CaptureM01V2RuntimeProofView(context, uiBootstrap.AppCanvasInstance, "campaign-public-m01-v2-selected-player-run", Entity.Null);
        CaptureM01V2RuntimeProofView(context, uiBootstrap.AppCanvasInstance, "campaign-public-m01-v2-enemy-patrol", context.EnemyPatrol);
    }

    [Test]
    public async Task PublicCampaignLaunch_M01GoldenPlaythroughShowsResultPopup()
    {
        Time.timeScale = 12f;
        SetLogAssertIgnoreFailingMessages(true);

        GameBootstrap bootstrap = await LoadGameSceneAndWaitForBootstrap();
        WarlineCaptureUiBootstrap uiBootstrap = await WaitForParallelUiBootstrap();
        WarlineCaptureRouter router = uiBootstrap.AppCanvasInstance.GetComponent<WarlineCaptureRouter>();
        Assert.NotNull(router, "Public app canvas should expose the WarlineCapture router.");

        router.GoTo(WarlineCaptureRoute.SagaMap, false);
        Assert.IsTrue(router.TryGetRegisteredScreen(WarlineCaptureRoute.SagaMap, out WarlineCaptureScreenController sagaScreen), "Public router should register the Saga Map screen.");
        SagaMapScreenController sagaMap = sagaScreen.GetComponent<SagaMapScreenController>();
        Assert.NotNull(sagaMap, "Saga Map screen should expose its controller.");
        sagaMap.SelectMissionForTests(ChapterOneMissionCatalog.FirstContactMissionId);
        WarlineCaptureMissionSession.BeginMission(ChapterOneMissionCatalog.FirstContactMissionId, WarlineCaptureRoute.SagaMap);
        router.GoTo(WarlineCaptureRoute.MissionBriefing);
        await NextFrame();
        Assert.AreEqual(WarlineCaptureRoute.MissionBriefing, router.ActiveRoute, "Public golden path should enter Mission Briefing from Saga Map.");

        router.GoTo(WarlineCaptureRoute.LoadoutSquadPrep);
        await NextFrame();
        Assert.AreEqual(WarlineCaptureRoute.LoadoutSquadPrep, router.ActiveRoute, "Public golden path should enter Loadout before deploy.");
        WarlineCaptureGameLaunchUtility.StartExistingGameplayAndHideRouter(router);

        M01SceneContext context = await WaitForM01Runtime(bootstrap);
        EntityManager em = context.World.EntityManager;
        await WaitForMissionAtlasQuads(context);
        AssertM01InfantryOnlyHudScope(router, "Campaign golden path");

        Assert.IsTrue(em.HasComponent<MissionRuntimeOpeningControlProtection>(context.EnemyPatrol), "Public M01 deploy should start with hostile opening-control protection.");
        int openingPlayerHealth = em.GetComponentData<UnitHealth>(context.PlayerSquad).Current;
        UnitAttack openingEnemyAttack = em.GetComponentData<UnitAttack>(context.EnemyPatrol);
        ForceProtectedEnemyAttackAttempt(context);
        for (int frame = 0; frame < 180; frame++)
            await NextFrame();

        Assert.IsTrue(em.Exists(context.PlayerSquad), "Public M01 command squad should still exist after a relaxed no-input opening review window.");
        Assert.AreEqual(openingPlayerHealth, em.GetComponentData<UnitHealth>(context.PlayerSquad).Current, "Public M01 deploy should allow inspection before select/first move without hostile damage.");
        Assert.IsFalse(em.HasComponent<EngageTarget>(context.EnemyPatrol), "Public M01 hostile patrol should not retain an engage target during the opening-control window.");
        em.SetComponentData(context.EnemyPatrol, openingEnemyAttack);

        Assert.IsTrue(context.Bootstrap.Selection.TrySelectRuntimeEntity(context.PlayerSquad).Accepted, "Golden path should select the M01 rifle squad through the selection controller.");
        Assert.IsTrue(em.HasComponent<SelectedUnitTag>(context.PlayerSquad), "Golden path selection should mark the command squad selected.");
        await NextFrame();
        AssertSelectedPresentationVisible(em, context.PlayerSquad, "golden path command squad");
        CapturePlayerView(context, uiBootstrap.AppCanvasInstance, "campaign-public-m01-selected-first-control");
        Assert.IsTrue(context.Loader.TryGetAnchorCell("tutorial.move_target.cover_01", out Vector2Int coverCell), "Golden path move-to-cover anchor should resolve.");
        Assert.IsTrue(context.Bootstrap.Selection.TryIssueMoveToCell(new int2(coverCell.x, coverCell.y)).Accepted, "Golden path should issue the move-to-cover command through the selection controller.");
        Assert.IsTrue(
            em.HasComponent<UnitPathRequest>(context.PlayerSquad) ||
            em.HasComponent<UnitPathFollow>(context.PlayerSquad) ||
            em.HasComponent<UnitTarget>(context.PlayerSquad),
            "Golden path move-to-cover should use tactical pathing components.");
        Assert.IsTrue(em.HasComponent<MissionRuntimeOpeningControlProtection>(context.EnemyPatrol), "Golden path enemy protection should remain through the move teaching step.");

        Assert.IsTrue(context.Bootstrap.Selection.TryIssueAttackTarget(context.EnemyPatrol).Accepted, "Golden path should issue attack on hostile patrol through the selection controller.");

        int enemyStartingHealth = em.GetComponentData<UnitHealth>(context.EnemyPatrol).Current;
        Assert.IsTrue(em.HasComponent<UnitHealth>(context.PlayerSquad), "Golden path command squad should still be alive before objective completion.");
        em.SetComponentData(context.EnemyPatrol, new UnitHealth { Current = 0, Max = enemyStartingHealth });
        GameRuntimeStats.RecordMilitaryDeath(1);
        for (int frame = 0; frame < 180 && !HasActiveMissionResultPopup(uiBootstrap.AppCanvasInstance); frame++)
            await NextFrame();

        Assert.IsTrue(HasActiveMissionResultPopup(uiBootstrap.AppCanvasInstance), "Golden path should show the public mission result popup after the hostile patrol is neutralized.");
        Assert.AreEqual(WarlineCaptureRoute.Match, router.ActiveRoute, "Result popup should be shown over the public Match route.");
        Assert.IsFalse(WarlineCaptureMissionSession.HasActiveMission, "Showing the result popup should clear the active mission session.");
    }

    [Test]
    public async Task GameScene_M01SelectionAttackAndResultRouteRespectSurvivalGuard()
    {
        M01SceneContext context = await LoadM01SceneAndWaitForRuntime();
        EntityManager em = context.World.EntityManager;

        Assert.IsTrue(context.Bootstrap.Selection.FocusUnitEntity(context.PlayerSquad), "The scene selection controller should select the M01 command squad.");
        Assert.IsTrue(em.HasComponent<SelectedUnitTag>(context.PlayerSquad), "Selected command squad should receive SelectedUnitTag.");
        Assert.IsTrue(context.Bootstrap.Selection.ArmFocusedAttackTargetMode(), "Selected command squad should arm explicit attack mode.");

        MovePlayerIntoAttackRange(em, context.PlayerSquad, context.EnemyPatrol);
        InvokeAttackOrder(context.PlayerSquad, context.EnemyPatrol, em);
        Assert.IsTrue(em.HasComponent<EngageTarget>(context.PlayerSquad), "Attack click should assign the hostile patrol as EngageTarget.");
        Assert.AreEqual(context.EnemyPatrol, em.GetComponentData<EngageTarget>(context.PlayerSquad).Target);

        int startingHealth = em.GetComponentData<UnitHealth>(context.EnemyPatrol).Current;
        for (int frame = 0; frame < 180 && em.GetComponentData<UnitHealth>(context.EnemyPatrol).Current >= startingHealth; frame++)
            await NextFrame();
        Assert.Less(em.GetComponentData<UnitHealth>(context.EnemyPatrol).Current, startingHealth, "Real attack systems should damage the hostile patrol after the attack order.");

        Assert.IsFalse(WarlineCaptureMatchResultFlow.CanCompleteActiveMissionFromLoadedScene(), "M01 result route should not be ready while the hostile patrol is still alive.");

        em.SetComponentData(context.EnemyPatrol, new UnitHealth { Current = 0, Max = startingHealth });
        GameRuntimeStats.RecordMilitaryDeath(1);
        Assert.IsTrue(Chapter01M01PlayableRuntime.ShouldStartResultFlow(context.World), "Destroying the hostile patrol with the command squad alive should ready the M01 result route.");
        Assert.IsTrue(WarlineCaptureMatchResultFlow.CanCompleteActiveMissionFromLoadedScene(), "Loaded scene result route should be allowed after patrol destruction.");

        WarlineCaptureMissionSession.BeginMission(ChapterOneMissionCatalog.FirstContactMissionId, WarlineCaptureRoute.SagaMap);
        GameRuntimeStats.Reset();
        em.SetComponentData(context.PlayerSquad, new UnitHealth { Current = 0, Max = 100 });
        em.SetComponentData(context.EnemyPatrol, new UnitHealth { Current = 0, Max = startingHealth });
        GameRuntimeStats.RecordMilitaryDeath(0);
        GameRuntimeStats.RecordMilitaryDeath(1);
        Assert.IsFalse(Chapter01M01PlayableRuntime.ShouldStartResultFlow(context.World), "Command squad destruction should block M01 result readiness.");
        Assert.IsFalse(WarlineCaptureMatchResultFlow.CanCompleteActiveMissionFromLoadedScene(), "Loaded scene result route should stay blocked when the command squad is destroyed.");
    }

    [Test]
    public async Task GameScene_M01OpeningControlWindowPreventsLethalEnemyFireUntilFirstMove()
    {
        M01SceneContext context = await LoadM01SceneAndWaitForRuntime();
        EntityManager em = context.World.EntityManager;

        Assert.IsTrue(em.HasComponent<MissionRuntimeOpeningControlProtection>(context.EnemyPatrol), "M01 hostile patrol should start protected from opening auto-fire.");
        int startingPlayerHealth = em.GetComponentData<UnitHealth>(context.PlayerSquad).Current;
        UnitAttack startingEnemyAttack = em.GetComponentData<UnitAttack>(context.EnemyPatrol);

        ForceProtectedEnemyAttackAttempt(context);
        for (int frame = 0; frame < 120; frame++)
            await NextFrame();

        Assert.IsTrue(em.Exists(context.PlayerSquad), "M01 command squad should still exist during the protected opening window.");
        Assert.AreEqual(startingPlayerHealth, em.GetComponentData<UnitHealth>(context.PlayerSquad).Current, "M01 opening should give the player a first-control window before hostile damage starts.");
        Assert.IsFalse(em.HasComponent<EngageTarget>(context.EnemyPatrol), "Protected M01 hostile patrol should not auto-engage before the first player command.");
        em.SetComponentData(context.EnemyPatrol, startingEnemyAttack);

        IssueMoveToCover(context);
        for (int frame = 0; frame < 60; frame++)
            await NextFrame();

        Assert.IsTrue(em.HasComponent<MissionRuntimeOpeningControlProtection>(context.EnemyPatrol), "M01 opening protection should stay active through the move-to-cover teaching step.");
        Assert.Greater(em.GetComponentData<UnitHealth>(context.PlayerSquad).Current, 0, "The command squad should still be alive after the opening move command.");
        Assert.IsTrue(
            em.HasComponent<UnitPathRequest>(context.PlayerSquad) ||
            em.HasComponent<UnitPathFollow>(context.PlayerSquad) ||
            em.HasComponent<UnitTarget>(context.PlayerSquad),
            "Move-to-cover should use tactical movement/pathing components rather than a visual-only jump.");

        Assert.IsTrue(context.Bootstrap.Selection.TrySelectRuntimeEntity(context.PlayerSquad).Accepted, "M01 opening attack step should select the command squad.");
        Assert.IsTrue(context.Bootstrap.Selection.TryIssueAttackTarget(context.EnemyPatrol).Accepted, "M01 opening attack step should use the public selection controller.");
        for (int frame = 0; frame < 60; frame++)
            await NextFrame();

        Assert.IsTrue(em.Exists(context.PlayerSquad), "The command squad should still exist after the opening attack command.");
        Assert.Greater(em.GetComponentData<UnitHealth>(context.PlayerSquad).Current, 0, "The command squad should stay alive while transitioning from first move into attack/result flow.");
        Assert.IsTrue(em.HasComponent<EngageTarget>(context.PlayerSquad), "The attack command should keep the hostile patrol targeted for the result flow.");
    }

    [Test]
    public async Task GameScene_M01SpritePresenterUsesEcsDrivenAtlasStateIds()
    {
        M01SceneContext context = await LoadM01SceneAndWaitForRuntime();
        EntityManager em = context.World.EntityManager;
        await WaitForMissionAtlasQuads(context);

        AssertAtlasPresenterAdapter(em, context.PlayerSquad, Chapter01M01PlayableRuntime.PlayerSquadEntityId, "command squad");
        AssertAtlasPresenterAdapter(em, context.EnemyPatrol, Chapter01M01PlayableRuntime.EnemyPatrolEntityId, "hostile patrol");

        MissionRuntimeSpritePresenter presenter = em.GetComponentData<MissionRuntimeSpritePresenter>(context.PlayerSquad);
        Assert.AreEqual(
            Chapter01M01SpritePresenterCatalog.ResolveStateSpriteId(Chapter01M01PlayableRuntime.PlayerSquadEntityId, MissionRuntimeSpriteVisualState.Idle),
            presenter.CurrentSpriteId.ToString(),
            "M01 command squad should start on the atlas idle state id.");

        Assert.IsTrue(context.Bootstrap.Selection.TrySelectRuntimeEntity(context.PlayerSquad).Accepted, "M01 sprite presenter validation should select the command squad before issuing command markers.");
        IssueMoveToCover(context);
        await NextFrame();
        MissionRuntimeAtlasQuadRuntime movingRuntime = em.GetComponentObject<MissionRuntimeAtlasQuadRuntime>(context.PlayerSquad);
        float animationElapsedBefore = movingRuntime.AnimationElapsed;
        string animationFrameBefore = movingRuntime.CurrentAnimationFrameKey;
        await NextFrame();
        movingRuntime = em.GetComponentObject<MissionRuntimeAtlasQuadRuntime>(context.PlayerSquad);
        Assert.Greater(movingRuntime.AnimationElapsed, animationElapsedBefore, "M01 v2 moving infantry should advance manifest-driven atlas animation time while moving.");
        Assert.IsNotEmpty(movingRuntime.CurrentAnimationFrameKey, "M01 v2 moving infantry should expose the active manifest frame key.");
        Assert.AreNotEqual(animationFrameBefore, movingRuntime.CurrentAnimationFrameKey, "M01 v2 moving infantry should advance atlas frame keys instead of relying on procedural bob.");
        AssertSelectedTargetMarkerVisible(em, context.PlayerSquad, "command squad move target marker", "move_destination_ring", "move");

        presenter = em.GetComponentData<MissionRuntimeSpritePresenter>(context.PlayerSquad);
        Assert.AreEqual(MissionRuntimeSpriteVisualState.Move, MissionRuntimeSpritePresenterSystem.ResolveVisualState(em, context.PlayerSquad), "ECS pathing intent should drive M01 move presentation.");
        Assert.AreEqual(
            Chapter01M01SpritePresenterCatalog.ResolveStateSpriteId(Chapter01M01PlayableRuntime.PlayerSquadEntityId, MissionRuntimeSpriteVisualState.Move),
            Chapter01M01SpritePresenterCatalog.ResolveSpriteId(presenter, MissionRuntimeSpriteVisualState.Move).ToString(),
            "M01 command squad move state should resolve to a move atlas state id.");

        RemoveIfPresent<UnitTarget>(em, context.PlayerSquad);
        RemoveIfPresent<UnitPathRequest>(em, context.PlayerSquad);
        RemoveIfPresent<UnitPathFollow>(em, context.PlayerSquad);
        em.SetComponentData(context.PlayerSquad, new UnitAttackAnimationState { TimeRemaining = 0.5f });
        presenter = em.GetComponentData<MissionRuntimeSpritePresenter>(context.PlayerSquad);
        Assert.AreEqual(MissionRuntimeSpriteVisualState.Attack, MissionRuntimeSpritePresenterSystem.ResolveVisualState(em, context.PlayerSquad), "ECS attack state should drive M01 attack presentation.");
        Assert.AreEqual(
            Chapter01M01SpritePresenterCatalog.ResolveStateSpriteId(Chapter01M01PlayableRuntime.PlayerSquadEntityId, MissionRuntimeSpriteVisualState.Attack),
            Chapter01M01SpritePresenterCatalog.ResolveSpriteId(presenter, MissionRuntimeSpriteVisualState.Attack).ToString(),
            "M01 command squad attack state should resolve to an attack atlas state id.");

        em.SetComponentData(context.PlayerSquad, new UnitHealth { Current = 0, Max = 100 });
        if (!em.HasComponent<UnitDeathAnimationState>(context.PlayerSquad))
            em.AddComponentData(context.PlayerSquad, new UnitDeathAnimationState { TimeRemaining = 0.5f });
        presenter = em.GetComponentData<MissionRuntimeSpritePresenter>(context.PlayerSquad);
        Assert.AreEqual(MissionRuntimeSpriteVisualState.Destroyed, MissionRuntimeSpritePresenterSystem.ResolveVisualState(em, context.PlayerSquad), "ECS death state should drive M01 destroyed presentation.");
        Assert.AreEqual(
            Chapter01M01PlayableRuntime.PlayerSquadEntityId + Chapter01M01SpritePresenterCatalog.DeathStateSuffix,
            Chapter01M01SpritePresenterCatalog.ResolveSpriteId(presenter, MissionRuntimeSpriteVisualState.Destroyed).ToString(),
            "M01 destroyed state should resolve to the v2 soldier death atlas state id.");
    }

    [Test]
    public async Task GameScene_M01BuildRejectionUsesSharedFeedbackReason()
    {
        await LoadM01SceneAndWaitForRuntime();

        Assert.IsFalse(WarlineCaptureMissionRules.IsBuildAllowedForActiveMission());
        Assert.AreEqual("Building unlocks in the next mission.", TacticalCommandFeedbackText.ToDisplayText(TacticalCommandReasonCode.MissionDoesNotAllowBuild));
        Assert.IsTrue(WarlineCaptureMissionRules.TryRejectBuildForActiveMission(), "M01 build entry points should use the shared mission-disallowed feedback path.");
        Assert.IsFalse(InitialUnitsRuntimeState.BuildModeActive, "Rejecting M01 build should not enter build mode.");
    }

    private static async Task<M01SceneContext> LoadM01SceneAndWaitForRuntime()
    {
        Time.timeScale = 12f;
        SetLogAssertIgnoreFailingMessages(true);
        WarlineCaptureMissionSession.BeginMission(ChapterOneMissionCatalog.FirstContactMissionId, WarlineCaptureRoute.SagaMap);

        GameBootstrap bootstrap = await LoadGameSceneAndWaitForBootstrap();
        bootstrap.BeginGameplay();
        return await WaitForM01Runtime(bootstrap);
    }

    private static async Task<GameBootstrap> LoadGameSceneAndWaitForBootstrap()
    {
        AsyncOperation load = SceneManager.LoadSceneAsync(GameSceneName, LoadSceneMode.Single);
        while (load != null && !load.isDone)
            await NextFrame();
        await NextFrame();
        await NextFrame();

        GameBootstrap bootstrap = null;
        for (int frame = 0; frame < 180 && bootstrap == null; frame++)
        {
            bootstrap = TryGetLoadedSceneComponent<GameBootstrap>();
            await NextFrame();
        }

        Assert.NotNull(bootstrap, "Game scene must contain GameBootstrap.");
        Assert.NotNull(bootstrap.Chapter01TacticalBinder, "Game scene must include the Chapter 1 tactical binder.");
        Assert.NotNull(bootstrap.Selection, "Game scene must initialize selection.");
        return bootstrap;
    }

    private static async Task<WarlineCaptureUiBootstrap> WaitForParallelUiBootstrap()
    {
        WarlineCaptureUiBootstrap uiBootstrap = null;
        for (int frame = 0; frame < 180 && (uiBootstrap == null || uiBootstrap.AppCanvasInstance == null); frame++)
        {
            uiBootstrap = TryGetLoadedSceneComponent<WarlineCaptureUiBootstrap>();
            await NextFrame();
        }

        Assert.NotNull(uiBootstrap, "Game scene must contain WarlineCaptureUiBootstrap.");
        return uiBootstrap;
    }

    private static async Task<M01SceneContext> WaitForM01Runtime(GameBootstrap bootstrap)
    {
        Assert.NotNull(bootstrap.DayNight, "Game scene must initialize the legacy day/night dependency.");
        Assert.IsFalse(bootstrap.DayNight.RuntimeVisualsEnabled, "M01 fixed tactical gameplay must disable day/night time-of-day visual mutations.");

        for (int frame = 0; frame < 1800; frame++)
        {
            World world = World.DefaultGameObjectInjectionWorld;
            TacticalMapRuntimeLoader loader = bootstrap.Chapter01TacticalBinder.TacticalMapLoader;
            if (world != null &&
                world.IsCreated &&
                loader != null &&
                TryFindMissionEntity(world.EntityManager, Chapter01M01PlayableRuntime.PlayerSquadEntityId, out Entity player) &&
                TryFindMissionEntity(world.EntityManager, Chapter01M01PlayableRuntime.EnemyPatrolEntityId, out Entity enemy))
            {
                return new M01SceneContext(bootstrap, world, loader, player, enemy);
            }

            await NextFrame();
        }

        Assert.Fail("Timed out waiting for M01 runtime entities in the loaded Game scene.");
        return default;
    }

    private static async Task WaitForMissionAtlasQuads(M01SceneContext context)
    {
        EntityManager em = context.World.EntityManager;
        for (int frame = 0; frame < 180; frame++)
        {
            if (IsMissionRendererReady(em, context.PlayerSquad) &&
                IsMissionRendererReady(em, context.EnemyPatrol) &&
                IsTerrainSurfaceRendererReady(em, context.Loader) &&
                HasNoUnsuppressedLegacyEcsMeshes(em))
            {
                return;
            }

            await NextFrame();
        }

        AssertMissionRendererVisible(em, context.PlayerSquad, "command squad after renderer wait");
        AssertMissionRendererVisible(em, context.EnemyPatrol, "hostile patrol after renderer wait");
    }

    private static bool IsMissionRendererReady(EntityManager em, Entity entity)
    {
        if (!em.Exists(entity) || !em.HasComponent<MissionRuntimeAtlasQuadRuntime>(entity))
            return false;

        MissionRuntimeAtlasQuadRuntime runtime = em.GetComponentObject<MissionRuntimeAtlasQuadRuntime>(entity);
        return runtime.SoldierEntities != null &&
            runtime.SoldierEntities.Length > 0 &&
            em.Exists(runtime.SoldierEntities[0]) &&
            em.HasComponent<MaterialMeshInfo>(runtime.SoldierEntities[0]) &&
            em.IsComponentEnabled<MaterialMeshInfo>(runtime.SoldierEntities[0]) &&
            runtime.Material != null &&
            runtime.Material.mainTexture != null &&
            runtime.SoldierVisible != null &&
            runtime.SoldierVisible.Length > 0 &&
            runtime.SoldierVisible[0];
    }

    private static bool IsTerrainSurfaceRendererReady(EntityManager em, TacticalMapRuntimeLoader loader)
    {
        return TryGetTerrainSurfaceEntity(em, loader, out Entity terrainEntity) &&
            em.HasComponent<MissionRuntimeTerrainSurfaceRendererRuntime>(terrainEntity) &&
            em.GetComponentObject<MissionRuntimeTerrainSurfaceRendererRuntime>(terrainEntity).Renderer == loader.GroundRenderer;
    }

    private static bool HasNoUnsuppressedLegacyEcsMeshes(EntityManager em)
    {
        using EntityQuery query = em.CreateEntityQuery(new EntityQueryDesc
        {
            All = new[] { ComponentType.ReadOnly<MaterialMeshInfo>() },
            None = new[] { ComponentType.ReadOnly<DisableRendering>(), ComponentType.ReadOnly<MissionRuntimeEcsVisualTag>() }
        });
        return query.CalculateEntityCount() == 0;
    }

    private static bool IsLoadedObjectNamedActive(string objectName)
    {
        foreach (GameObject gameObject in GetLoadedSceneRootObjects())
        {
            if (gameObject.name == objectName && gameObject.activeInHierarchy)
                return true;
        }

        return false;
    }

    private static bool IsLoadedRootObjectActive(string objectName)
    {
        foreach (GameObject gameObject in GetLoadedSceneRootObjects())
        {
            if (gameObject.name == objectName && gameObject.activeInHierarchy)
                return true;
        }

        return false;
    }

    private static T TryGetLoadedSceneComponent<T>() where T : Component
    {
        GameObject[] roots = GetLoadedSceneRootObjects();
        for (int i = 0; i < roots.Length; i++)
        {
            T component = roots[i].GetComponent<T>();
            if (component != null)
                return component;
        }

        return null;
    }

    private static GameObject[] GetLoadedSceneRootObjects()
    {
        Scene scene = SceneManager.GetSceneByName(GameSceneName);
        Assert.IsTrue(scene.IsValid(), $"{GameSceneName} scene should be loaded.");
        Assert.IsTrue(scene.isLoaded, $"{GameSceneName} scene should be loaded.");
        return scene.GetRootGameObjects();
    }

    private static void AssertM01ProductionPlayerVisibleState(M01SceneContext context, string entryPath)
    {
        Assert.IsFalse(IsLoadedRootObjectActive("Decorations"), $"{entryPath}: legacy Decorations root must be hidden for the M01 production slice.");
        Assert.IsFalse(IsLoadedRootObjectActive("SM_Skydome_01"), $"{entryPath}: legacy skydome root must be hidden for the M01 production slice.");
        Assert.IsFalse(IsLoadedRootObjectActive("Ground"), $"{entryPath}: legacy root Ground object must be hidden for the M01 production slice.");

        AssertTacticalGroundAndCameraFraming(context, entryPath);
        AssertNoUnsuppressedLegacyEcsMeshes(context.World.EntityManager, entryPath);
        AssertMissionRendererVisible(context.World.EntityManager, context.PlayerSquad, $"{entryPath}: command squad");
        AssertMissionRendererVisible(context.World.EntityManager, context.EnemyPatrol, $"{entryPath}: hostile patrol");
        AssertLegacyModelSuppressed(context.World.EntityManager, context.PlayerSquad, $"{entryPath}: command squad");
        AssertLegacyModelSuppressed(context.World.EntityManager, context.EnemyPatrol, $"{entryPath}: hostile patrol");
    }

    private static void AssertM01InfantryOnlyHudScope(WarlineCaptureRouter router, string entryPath)
    {
        Assert.IsTrue(router.TryGetRegisteredScreen(WarlineCaptureRoute.Match, out WarlineCaptureScreenController matchScreen), $"{entryPath}: public router should register the Match HUD screen.");
        M01InfantryOnlyHudScopeController infantryScope = matchScreen.GetComponent<M01InfantryOnlyHudScopeController>();
        Assert.NotNull(infantryScope, $"{entryPath}: Match HUD should include the M01 infantry-only scope controller.");
        infantryScope.Refresh();
        Assert.IsTrue(infantryScope.IsM01ScopeActive, $"{entryPath}: M01 infantry-only HUD scope should be active for First Contact.");
        Assert.GreaterOrEqual(infantryScope.HiddenRootCount, 7, $"{entryPath}: M01 HUD should suppress APC, Tank, air support, Build, and related production affordance roots.");
        Assert.IsTrue(infantryScope.AreM01SuppressedRootsHidden(), $"{entryPath}: APC, Tank, air support, Build, production, transport, and base/build affordances must not be presented as usable M01 options.");
    }

    private static void AssertTacticalGroundAndCameraFraming(M01SceneContext context, string entryPath)
    {
        Camera camera = context.Bootstrap.WorldCamera;
        Assert.NotNull(camera, $"{entryPath}: gameplay camera must be available.");
        Assert.IsTrue(camera.orthographic, $"{entryPath}: M01 production launch must use the orthographic tactical camera.");
        Assert.Greater(Vector3.Dot(camera.transform.forward, Vector3.down), 0.98f, $"{entryPath}: M01 production camera must frame the authored tactical map from the top-down 2D tactical plane.");

        SpriteRenderer groundRenderer = context.Loader.GroundRenderer;
        Assert.NotNull(groundRenderer, $"{entryPath}: tactical map loader must expose a ground renderer.");
        Assert.IsTrue(groundRenderer.enabled, $"{entryPath}: tactical ground renderer must be enabled.");
        Assert.NotNull(groundRenderer.sprite, $"{entryPath}: tactical ground renderer must have the authored map sprite.");
        Assert.IsTrue(groundRenderer.gameObject.activeInHierarchy, $"{entryPath}: tactical ground renderer must be active in hierarchy.");
        AssertTerrainSurfaceEcsBacked(context, entryPath, groundRenderer);

        Bounds groundBounds = groundRenderer.bounds;
        Assert.Greater(groundBounds.size.x, 1f, $"{entryPath}: authored tactical map must occupy real world width.");
        Assert.Greater(groundBounds.size.z, 0.5f, $"{entryPath}: authored tactical map must occupy real world depth.");
        Assert.Greater(
            Vector3.Dot(groundRenderer.transform.up, Vector3.forward),
            0.98f,
            $"{entryPath}: authored tactical map must not be upside down; sprite up must align with positive world Z used by tactical metadata anchors.");

        Vector3 cameraGroundCenter = GetCameraGroundCenter(camera);
        float visibleHeight = camera.orthographicSize * 2f;
        float visibleWidth = visibleHeight * camera.aspect;
        Rect cameraGroundRect = new(
            cameraGroundCenter.x - visibleWidth * 0.5f,
            cameraGroundCenter.z - visibleHeight * 0.5f,
            visibleWidth,
            visibleHeight);
        Rect mapGroundRect = new(
            groundBounds.min.x,
            groundBounds.min.z,
            groundBounds.size.x,
            groundBounds.size.z);

        float overlapRatio = CalculateOverlapArea(cameraGroundRect, mapGroundRect) / Mathf.Max(cameraGroundRect.width * cameraGroundRect.height, 0.0001f);
        Assert.Greater(overlapRatio, 0.85f, $"{entryPath}: gameplay camera must show authored M01 terrain instead of empty/legacy background.");
    }

    private static void AssertTerrainSurfaceEcsBacked(M01SceneContext context, string entryPath, SpriteRenderer groundRenderer)
    {
        EntityManager em = context.World.EntityManager;
        Assert.IsTrue(TryGetTerrainSurfaceEntity(em, context.Loader, out Entity terrainEntity), $"{entryPath}: visible tactical terrain must be backed by a MissionRuntimeTerrainSurface ECS entity.");
        MissionRuntimeTerrainSurface surface = em.GetComponentData<MissionRuntimeTerrainSurface>(terrainEntity);
        Assert.AreEqual(context.Loader.Definition.MapId, surface.MapId.ToString(), $"{entryPath}: terrain ECS map id should match the tactical definition.");
        Assert.AreEqual(context.Loader.Definition.MissionId, surface.MissionId.ToString(), $"{entryPath}: terrain ECS mission id should match the tactical definition.");
        Assert.AreEqual(context.Loader.Definition.WorldOrigin.x, surface.WorldOrigin.x, 0.001f, $"{entryPath}: terrain ECS world origin x should match metadata.");
        Assert.AreEqual(context.Loader.Definition.WorldOrigin.y, surface.WorldOrigin.y, 0.001f, $"{entryPath}: terrain ECS world origin y should match metadata.");
        Assert.AreEqual(context.Loader.Definition.VisibleWorldSize.x, surface.VisibleWorldSize.x, 0.001f, $"{entryPath}: terrain ECS visible width should match metadata.");
        Assert.AreEqual(context.Loader.Definition.VisibleWorldSize.y, surface.VisibleWorldSize.y, 0.001f, $"{entryPath}: terrain ECS visible height should match metadata.");
        Assert.AreEqual(1, surface.SpriteUpAlignsPositiveWorldZ, $"{entryPath}: terrain ECS orientation flag should prove the non-upside-down presentation contract.");

        Assert.IsTrue(em.HasComponent<MissionRuntimeTerrainSurfaceRendererRuntime>(terrainEntity), $"{entryPath}: terrain ECS entity should own the SpriteRenderer presentation object.");
        MissionRuntimeTerrainSurfaceRendererRuntime runtime = em.GetComponentObject<MissionRuntimeTerrainSurfaceRendererRuntime>(terrainEntity);
        Assert.AreSame(groundRenderer, runtime.Renderer, $"{entryPath}: loader GroundRenderer must be the ECS-owned terrain presentation renderer.");
        Assert.AreSame(groundRenderer.gameObject, runtime.Instance, $"{entryPath}: terrain GameObject must be referenced only as ECS presentation.");
        Assert.AreSame(groundRenderer.sprite, runtime.GroundSprite, $"{entryPath}: terrain sprite should be driven by the ECS terrain renderer runtime component.");
    }

    private static bool TryGetTerrainSurfaceEntity(EntityManager em, TacticalMapRuntimeLoader loader, out Entity terrainEntity)
    {
        using EntityQuery query = em.CreateEntityQuery(ComponentType.ReadOnly<MissionRuntimeTerrainSurface>());
        using NativeArray<Entity> entities = query.ToEntityArray(Allocator.Temp);
        for (int i = 0; i < entities.Length; i++)
        {
            Entity entity = entities[i];
            if (!em.Exists(entity) || !em.HasComponent<MissionRuntimeTerrainSurfaceRendererRuntime>(entity))
                continue;

            MissionRuntimeTerrainSurface surface = em.GetComponentData<MissionRuntimeTerrainSurface>(entity);
            if (surface.MapId.ToString() != loader.Definition.MapId)
                continue;

            MissionRuntimeTerrainSurfaceRendererRuntime runtime = em.GetComponentObject<MissionRuntimeTerrainSurfaceRendererRuntime>(entity);
            if (runtime.Renderer != loader.GroundRenderer)
                continue;

            terrainEntity = entity;
            return true;
        }

        terrainEntity = Entity.Null;
        return false;
    }

    private static void AssertNoUnsuppressedLegacyEcsMeshes(EntityManager em, string entryPath)
    {
        using EntityQuery query = em.CreateEntityQuery(new EntityQueryDesc
        {
            All = new[] { ComponentType.ReadOnly<MaterialMeshInfo>() },
            None = new[] { ComponentType.ReadOnly<DisableRendering>(), ComponentType.ReadOnly<MissionRuntimeEcsVisualTag>() }
        });
        Assert.AreEqual(0, query.CalculateEntityCount(), $"{entryPath}: legacy ECS mesh renderers must be suppressed so the first visible state is the 2D/isometric production slice.");
    }

    private static void AssertMissionRendererVisible(EntityManager em, Entity entity, string label)
    {
        Assert.IsFalse(em.HasComponent<MissionRuntimeSpriteRendererRuntime>(entity), $"{label} should not use the temporary SpriteRenderer adapter.");
        Assert.IsTrue(em.HasComponent<MissionRuntimeAtlasQuadRuntime>(entity), $"{label} should have an ECS-owned atlas quad runtime component.");
        MissionRuntimeAtlasQuadRuntime runtime = em.GetComponentObject<MissionRuntimeAtlasQuadRuntime>(entity);
        Assert.IsNull(runtime.Renderer, $"{label} must not expose a MeshRenderer wrapper reference.");
        Assert.IsNull(runtime.MeshFilter, $"{label} must not expose a MeshFilter wrapper reference.");
        Assert.IsNull(runtime.Instance, $"{label} must not expose a runtime GameObject atlas wrapper.");
        Assert.NotNull(runtime.Material, $"{label} should expose an atlas material.");
        Assert.NotNull(runtime.Material.mainTexture, $"{label} atlas quad material should have a texture.");
        Assert.NotNull(runtime.SoldierEntities, $"{label} should expose ECS render entities.");
        Assert.Greater(runtime.SoldierEntities.Length, 0, $"{label} should create at least one ECS render entity.");
        Assert.IsTrue(em.Exists(runtime.SoldierEntities[0]), $"{label} ECS render entity should exist.");
        Assert.IsTrue(em.HasComponent<MissionRuntimeEcsVisualTag>(runtime.SoldierEntities[0]), $"{label} visible presentation must be tagged as an ECS visual entity.");
        Assert.IsTrue(em.HasComponent<MaterialMeshInfo>(runtime.SoldierEntities[0]), $"{label} ECS visual entity should use Entities Graphics MaterialMeshInfo.");
        Assert.IsTrue(em.IsComponentEnabled<MaterialMeshInfo>(runtime.SoldierEntities[0]), $"{label} ECS visual entity should be render-enabled.");
        Assert.IsFalse(em.HasComponent<DisableRendering>(runtime.SoldierEntities[0]), $"{label} ECS visual entity should not be suppressed by the legacy ECS mesh gate.");
        Assert.IsNull(GameObject.Find("M01RuntimeEcsAtlasQuads"), $"{label} must not create the rejected runtime GameObject wrapper root.");
    }

    private static void AssertAtlasPresenterAdapter(EntityManager em, Entity entity, string runtimeEntityId, string label)
    {
        Assert.IsTrue(em.HasComponent<MissionRuntimeEntityId>(entity), $"{label} should be tracked by runtime entity id.");
        Assert.AreEqual(runtimeEntityId, em.GetComponentData<MissionRuntimeEntityId>(entity).Value.ToString(), $"{label} runtime entity id should match the M01 contract.");
        Assert.IsTrue(em.HasComponent<MissionRuntimeSpritePresenter>(entity), $"{label} should carry ECS sprite presenter state.");
        MissionRuntimeSpritePresenter presenter = em.GetComponentData<MissionRuntimeSpritePresenter>(entity);
        Assert.AreEqual(runtimeEntityId, presenter.RuntimeEntityId.ToString(), $"{label} presenter should identify the ECS runtime entity.");
        Assert.AreEqual(runtimeEntityId, presenter.ManifestAssetId.ToString(), $"{label} presenter should use the Chapter 1 manifest asset id.");
        Assert.AreEqual(1, presenter.FinalAtlasArtReady, $"{label} should use the accepted v2 multi-frame soldier atlas art.");
        Assert.AreEqual(0, presenter.UsesSeparateDestroyedChild, $"{label} destroyed/death feedback should resolve through the atlas presenter, not a separate Destroyed child.");
        Assert.AreNotEqual(presenter.IdleSpriteId, presenter.MoveSpriteId, $"{label} idle and move atlas state ids should be distinct even while they share temporary source art.");
        Assert.AreNotEqual(presenter.IdleSpriteId, presenter.AttackSpriteId, $"{label} idle and attack atlas state ids should be distinct even while they share temporary source art.");
        Assert.IsFalse(em.HasComponent<MissionRuntimeSpriteRendererRuntime>(entity), $"{label} should not use the temporary ECS-driven SpriteRenderer adapter.");
        Assert.IsFalse(em.HasComponent<UnitDestroyedVisualReference>(entity), $"{label} should not retain the old separate Destroyed child visual reference.");
        Assert.IsFalse(em.HasComponent<UnitDestroyedVisualInitialized>(entity), $"{label} should not initialize old separate Destroyed child visibility.");
        Assert.IsTrue(em.HasComponent<MissionRuntimeAtlasQuadRuntime>(entity), $"{label} should use the ECS-owned atlas quad presentation path.");
        MissionRuntimeAtlasQuadRuntime runtime = em.GetComponentObject<MissionRuntimeAtlasQuadRuntime>(entity);
        Assert.IsTrue(MissionRuntimeAtlasQuadPresentationSystem.TryResolveSprite(presenter, out Sprite resolvedSprite), $"{label} current atlas state id should resolve through the Chapter 1 sprite resolver.");
        if (runtimeEntityId == Chapter01M01PlayableRuntime.PlayerSquadEntityId ||
            runtimeEntityId == Chapter01M01PlayableRuntime.EnemyPatrolEntityId)
        {
            StringAssert.Contains("_animation_atlas_v2_", resolvedSprite.name, $"{label} should resolve to the v2 soldier atlas, not the old individual soldier sheet.");
            Assert.IsFalse(resolvedSprite.name.Contains("Unit_Chr_Soldier_Male_02"), $"{label} must not resolve to the old individual soldier sheet.");
            Assert.IsFalse(resolvedSprite.name.Contains("infantry_squad"), $"{label} must not resolve unit states to the rejected grouped infantry sprite.");
        }
        Assert.NotNull(runtime.Material.mainTexture, $"{label} atlas quad material should be assigned by the ECS presenter resolver.");
        Assert.IsNull(runtime.Instance, $"{label} public M01 unit visuals must not expose a Unity GameObject presentation wrapper.");
        Assert.IsNull(GameObject.Find("M01RuntimeEcsAtlasQuads"), $"{label} public M01 unit visuals must not create the rejected runtime quad GameObject root.");
        AssertContractScale(runtime, runtimeEntityId, label);
        int expectedSoldierCount = runtimeEntityId == Chapter01M01PlayableRuntime.PlayerSquadEntityId ? 4 : 1;
        Assert.AreEqual(expectedSoldierCount, runtime.SoldierCount, $"{label} should expose the expected readable soldier count under one gameplay entity.");
        Assert.NotNull(runtime.SoldierEntities, $"{label} should expose soldier ECS render entities.");
        Assert.AreEqual(expectedSoldierCount, runtime.SoldierEntities.Length, $"{label} should render the squad as distinct soldier ECS visual entities.");
        Assert.NotNull(runtime.SoldierRenderers, $"{label} should keep the compatibility renderer array allocated.");
        Assert.AreEqual(0, runtime.SoldierRenderers.Length, $"{label} must not use MeshRenderer wrapper soldiers.");
        for (int i = 0; i < runtime.SoldierEntities.Length; i++)
        {
            Assert.IsTrue(em.Exists(runtime.SoldierEntities[i]), $"{label} soldier {i + 1} ECS render entity should exist.");
            Assert.IsTrue(em.HasComponent<MaterialMeshInfo>(runtime.SoldierEntities[i]), $"{label} soldier {i + 1} should use Entities Graphics rendering.");
            Assert.IsTrue(em.IsComponentEnabled<MaterialMeshInfo>(runtime.SoldierEntities[i]), $"{label} soldier {i + 1} should be visible through Entities Graphics.");
            Assert.IsFalse(em.HasComponent<DisableRendering>(runtime.SoldierEntities[i]), $"{label} soldier {i + 1} should not be suppressed as a legacy ECS mesh.");
        }
        if (expectedSoldierCount == 4)
            AssertDistinctSoldierPositions(runtime, label);
        AssertM01InfantryMovementContract(em, entity, label);
        AssertTacticalAttackTraceScale(em, entity, label);
        AssertLegacyModelSuppressed(em, entity, label);
    }

    private static void AssertContractScale(MissionRuntimeAtlasQuadRuntime runtime, string runtimeEntityId, string label)
    {
        if (runtimeEntityId == Chapter01M01PlayableRuntime.PlayerSquadEntityId ||
            runtimeEntityId == Chapter01M01PlayableRuntime.EnemyPatrolEntityId)
        {
            Assert.That(runtime.InstanceScale, Is.InRange(0.145f, 0.155f), $"{label} should consume the user-observed readable infantry scale target near 0.15.");
        }
    }

    private static void AssertM01InfantryMovementContract(EntityManager em, Entity entity, string label)
    {
        Assert.IsTrue(em.HasComponent<UnitMove>(entity), $"{label} should keep movement config.");
        UnitMove move = em.GetComponentData<UnitMove>(entity);
        Assert.That(move.Speed, Is.InRange(0.38f, 0.46f), $"{label} run speed should be calibrated to readable infantry movement, not teleport-fast prefab speed.");
        Assert.That(move.WalkSpeed, Is.InRange(0.24f, 0.32f), $"{label} walk speed should be below run speed and calibrated for infantry.");
        Assert.LessOrEqual(move.RoadSpeedMultiplier, 1.05f, $"{label} M01 infantry should not receive a large road-speed boost in the teaching move.");
    }

    private static void AssertDistinctSoldierPositions(MissionRuntimeAtlasQuadRuntime runtime, string label)
    {
        for (int i = 0; i < runtime.SoldierEntities.Length; i++)
        {
            for (int j = i + 1; j < runtime.SoldierEntities.Length; j++)
            {
                Vector3 a = GetEntityWorldPosition(runtime, i);
                Vector3 b = GetEntityWorldPosition(runtime, j);
                Assert.Greater(Vector3.Distance(a, b), 0.10f, $"{label} soldiers {i + 1} and {j + 1} should have readable world-space formation spacing at public gameplay scale.");
            }
        }
    }

    private static void AssertSelectedPresentationVisible(EntityManager em, Entity entity, string label)
    {
        Assert.IsTrue(em.HasComponent<SelectedUnitTag>(entity), $"{label} should be selected.");
        Assert.IsTrue(em.HasComponent<MissionRuntimeAtlasQuadRuntime>(entity), $"{label} should have atlas quad runtime presentation.");
        MissionRuntimeAtlasQuadRuntime runtime = em.GetComponentObject<MissionRuntimeAtlasQuadRuntime>(entity);
        Assert.NotNull(runtime.SelectionEntities, $"{label} should have grounded per-soldier selection marker ECS render entities.");
        Assert.AreEqual(runtime.SoldierCount, runtime.SelectionEntities.Length, $"{label} selection marker count should match soldier count.");
        Assert.NotNull(runtime.SelectionRenderers, $"{label} should keep the compatibility marker renderer array allocated.");
        Assert.AreEqual(0, runtime.SelectionRenderers.Length, $"{label} must not use MeshRenderer wrapper selection markers.");
        for (int i = 0; i < runtime.SelectionEntities.Length; i++)
        {
            Entity marker = runtime.SelectionEntities[i];
            Assert.IsTrue(em.Exists(marker), $"{label} selection marker {i + 1} should exist.");
            Assert.IsTrue(em.HasComponent<MissionRuntimeSelectionMarkerVisualTag>(marker), $"{label} selection marker {i + 1} should be tagged as an ECS selection marker visual.");
            Assert.IsTrue(em.HasComponent<MissionRuntimeEcsVisualTag>(marker), $"{label} selection marker {i + 1} should be tagged as a production ECS visual.");
            Assert.IsTrue(em.IsComponentEnabled<MaterialMeshInfo>(marker), $"{label} selection marker {i + 1} should be visible.");
            Assert.IsFalse(em.HasComponent<DisableRendering>(marker), $"{label} selection marker {i + 1} should not be suppressed as a legacy ECS mesh.");
            Assert.LessOrEqual(runtime.SelectionLocalScales[i].x, 0.32f, $"{label} selection marker {i + 1} should stay small and grounded under a soldier.");
            Assert.LessOrEqual(runtime.SelectionLocalScales[i].y, 0.10f, $"{label} selection marker {i + 1} should not become a screen-covering overlay.");
            Assert.Less(runtime.SelectionLocalPositions[i].y, -0.35f, $"{label} selection marker {i + 1} should sit at the soldier foot/ground area, not over the torso.");
            Assert.NotNull(runtime.SelectionMaterials[i].mainTexture, $"{label} selection marker {i + 1} should use the Art/Atlas marker texture, not a material-only square.");
            StringAssert.Contains("selection_ring", runtime.SelectionMaterials[i].mainTexture.name, $"{label} selection marker {i + 1} should use the small selection_ring marker asset.");
            Color markerColor = runtime.SelectionMaterials[i].color;
            Assert.Greater(markerColor.r, 0.90f, $"{label} selection marker {i + 1} should use warm grounded selection color.");
            Assert.Greater(markerColor.g, 0.60f, $"{label} selection marker {i + 1} should stay warm/amber instead of blue.");
            Assert.Less(markerColor.b, 0.35f, $"{label} selection marker {i + 1} should not read as the rejected blue/green UI effect.");
        }
    }

    private static void AssertSelectedTargetMarkerVisible(EntityManager em, Entity entity, string label, string expectedTextureName, string expectedKind)
    {
        Assert.IsTrue(em.HasComponent<SelectedUnitTag>(entity), $"{label} should only be visible for the selected command unit.");
        MissionRuntimeAtlasQuadRuntime runtime = em.GetComponentObject<MissionRuntimeAtlasQuadRuntime>(entity);
        Assert.IsTrue(runtime.TargetMarkerVisible, $"{label} should be visible after a selected move/attack command.");
        Assert.IsTrue(em.Exists(runtime.TargetMarkerEntity), $"{label} ECS visual entity should exist.");
        Assert.IsTrue(em.HasComponent<MissionRuntimeTargetMarkerVisualTag>(runtime.TargetMarkerEntity), $"{label} should be tagged as an ECS command target marker.");
        Assert.IsTrue(em.HasComponent<MissionRuntimeEcsVisualTag>(runtime.TargetMarkerEntity), $"{label} should be tagged as a production ECS visual.");
        Assert.IsTrue(em.HasComponent<MaterialMeshInfo>(runtime.TargetMarkerEntity), $"{label} should use Entities Graphics rendering.");
        Assert.IsTrue(em.IsComponentEnabled<MaterialMeshInfo>(runtime.TargetMarkerEntity), $"{label} should be render-enabled.");
        Assert.IsFalse(em.HasComponent<DisableRendering>(runtime.TargetMarkerEntity), $"{label} should not be suppressed as a legacy ECS mesh.");
        Assert.AreEqual(expectedKind, runtime.TargetMarkerKind, $"{label} should expose the command marker type for validation.");
        Assert.NotNull(runtime.TargetMarkerMaterial, $"{label} should keep marker material evidence.");
        Assert.NotNull(runtime.TargetMarkerMaterial.mainTexture, $"{label} should use the Art/Atlas marker texture.");
        StringAssert.Contains(expectedTextureName, runtime.TargetMarkerMaterial.mainTexture.name, $"{label} should use the small contracted marker art.");
        Assert.LessOrEqual(runtime.TargetMarkerWorldScale.x, 0.32f, $"{label} should stay about two soldier footsteps wide.");
        Assert.LessOrEqual(runtime.TargetMarkerWorldScale.y, 0.12f, $"{label} should not cover the selected unit or screen.");
        Assert.Greater(runtime.TargetMarkerWorldPosition.y, 0f, $"{label} should sit slightly above the ground plane to avoid z-fighting.");
    }

    private static Vector3 GetEntityWorldPosition(MissionRuntimeAtlasQuadRuntime runtime, int soldierIndex)
    {
        Matrix4x4 root = Matrix4x4.TRS(runtime.InstancePosition, runtime.InstanceRotation, Vector3.one * runtime.InstanceScale);
        Matrix4x4 local = Matrix4x4.TRS(runtime.SoldierLocalPositions[soldierIndex], Quaternion.identity, Vector3.one);
        return (root * local).GetColumn(3);
    }

    private static void AssertTacticalAttackTraceScale(EntityManager em, Entity entity, string label)
    {
        Assert.IsTrue(em.HasComponent<UnitAttack>(entity), $"{label} should keep combat data.");
        UnitAttack attack = em.GetComponentData<UnitAttack>(entity);
        Assert.LessOrEqual(attack.TraceWidth, 0.035f, $"{label} projectile trace width should stay tactical-scale instead of oversized arcade bullets.");
        Assert.LessOrEqual(attack.TraceVisibleSeconds, 0.16f, $"{label} projectile trace lifetime should stay tactical-scale and brief.");
        Assert.GreaterOrEqual(attack.TraceDashDensity, 8f, $"{label} projectile trace should read as a tactical tracer/impact cue.");
    }

    private static void AssertLegacyModelSuppressed(EntityManager em, Entity entity, string label)
    {
        Assert.IsTrue(em.HasComponent<MissionRuntimeSpritePresenterSuppressesLegacyModelTag>(entity), $"{label} should carry the accepted legacy-model suppression tag.");
        if (em.HasComponent<UnitModelInstanceReference>(entity))
            AssertRenderingDisabledRecursive(em, em.GetComponentData<UnitModelInstanceReference>(entity).Instance, $"{label} detail model");
        if (em.HasComponent<UnitMidLodInstanceReference>(entity))
            AssertRenderingDisabledRecursive(em, em.GetComponentData<UnitMidLodInstanceReference>(entity).Instance, $"{label} mid LOD model");
        if (em.HasComponent<UnitLowLodInstanceReference>(entity))
            AssertRenderingDisabledRecursive(em, em.GetComponentData<UnitLowLodInstanceReference>(entity).Instance, $"{label} low LOD model");
    }

    private static bool HasActiveMissionResultPopup(GameObject appCanvas)
    {
        if (appCanvas == null)
            return false;

        WarlineCaptureMatchResultFlow flow = appCanvas.GetComponent<WarlineCaptureMatchResultFlow>();
        return flow != null && flow.HasActivePopup;
    }

    private static void AssertRenderingDisabledRecursive(EntityManager em, Entity entity, string label)
    {
        if (entity == Entity.Null || !em.Exists(entity))
            return;

        Assert.IsTrue(em.HasComponent<DisableRendering>(entity), $"{label} must stay hidden while M01 sprite presenter owns the visible production asset.");
        if (!em.HasBuffer<Child>(entity))
            return;

        DynamicBuffer<Child> children = em.GetBuffer<Child>(entity);
        for (int i = 0; i < children.Length; i++)
            AssertRenderingDisabledRecursive(em, children[i].Value, label);
    }

    private static string CapturePlayerView(M01SceneContext context, GameObject appCanvas, string captureName)
    {
        if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null)
        {
            TestContext.WriteLine($"[M01PublicLaunchCapture] skipped={captureName} reason=graphics-device-null");
            return string.Empty;
        }

        Camera camera = context.Bootstrap.WorldCamera;
        Assert.NotNull(camera, "M01 public launch capture requires the gameplay world camera.");

        string captureDirectory = Path.Combine(GetMainProjectRoot(), "Design/AgentReports/Captures/2026-05-08_m01-public-launch");
        Directory.CreateDirectory(captureDirectory);
        string capturePath = Path.Combine(captureDirectory, $"{captureName}.png");
        string wideCapturePath = Path.Combine(captureDirectory, $"{captureName}-20x9.png");
        CapturePlayerViewAtResolution(context, appCanvas, capturePath, 1280, 720);
        CapturePlayerViewAtResolution(context, appCanvas, wideCapturePath, 1600, 720);

        Assert.NotNull(appCanvas, "M01 public launch capture should include the WarlineCapture app canvas.");
        WarlineCaptureRouter router = appCanvas.GetComponent<WarlineCaptureRouter>();
        Assert.NotNull(router, "M01 public launch capture should have the WarlineCapture router available.");
        Assert.AreEqual(WarlineCaptureRoute.Match, router.ActiveRoute, "M01 public launch should leave the UI on the Match route; UI owns HUD/canvas capture composition.");
        TestContext.WriteLine($"[M01PublicLaunchCapture] path={capturePath}");
        TestContext.WriteLine($"[M01PublicLaunchCapture] path={wideCapturePath}");
        return capturePath;
    }

    private static string CaptureM01V2RuntimeProofView(M01SceneContext context, GameObject appCanvas, string captureName, Entity focusEntity)
    {
        if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null)
        {
            TestContext.WriteLine($"[M01V2RuntimeCapture] skipped={captureName} reason=graphics-device-null");
            return string.Empty;
        }

        string captureDirectory = Path.Combine(GetMainProjectRoot(), "Design/AgentReports/Captures/2026-05-09_m01-v2-runtime");
        Directory.CreateDirectory(captureDirectory);
        string capturePath = Path.Combine(captureDirectory, $"{captureName}.png");
        string wideCapturePath = Path.Combine(captureDirectory, $"{captureName}-20x9.png");
        CaptureM01ViewAtResolution(context, appCanvas, capturePath, 1280, 720, focusEntity);
        CaptureM01ViewAtResolution(context, appCanvas, wideCapturePath, 1600, 720, focusEntity);
        TestContext.WriteLine($"[M01V2RuntimeCapture] path={capturePath}");
        TestContext.WriteLine($"[M01V2RuntimeCapture] path={wideCapturePath}");
        return capturePath;
    }

    private static void CapturePlayerViewAtResolution(M01SceneContext context, GameObject appCanvas, string capturePath, int width, int height)
    {
        CaptureM01ViewAtResolution(context, appCanvas, capturePath, width, height, Entity.Null);
    }

    private static void CaptureM01ViewAtResolution(M01SceneContext context, GameObject appCanvas, string capturePath, int width, int height, Entity focusEntity)
    {
        if (File.Exists(capturePath))
            File.Delete(capturePath);

        Camera camera = context.Bootstrap.WorldCamera;
        Canvas appRootCanvas = appCanvas != null ? appCanvas.GetComponent<Canvas>() : null;
        Assert.NotNull(appRootCanvas, "M01 public launch capture requires the WarlineCapture app canvas root.");

        RenderTexture previousTarget = camera.targetTexture;
        RenderTexture previousActive = RenderTexture.active;
        RenderMode previousRenderMode = appRootCanvas.renderMode;
        Camera previousCanvasCamera = appRootCanvas.worldCamera;
        float previousPlaneDistance = appRootCanvas.planeDistance;
        int previousSortingOrder = appRootCanvas.sortingOrder;
        RenderTexture renderTexture = new(width, height, 24, RenderTextureFormat.ARGB32);
        Texture2D texture = new(width, height, TextureFormat.RGBA32, false);
        try
        {
            camera.targetTexture = renderTexture;
            camera.aspect = width / (float)height;
            context.Bootstrap.ApplyM01ProductionCameraPoseForCurrentAspect();
            if (focusEntity != Entity.Null &&
                context.World.EntityManager.Exists(focusEntity) &&
                context.World.EntityManager.HasComponent<LocalTransform>(focusEntity))
            {
                LocalTransform focus = context.World.EntityManager.GetComponentData<LocalTransform>(focusEntity);
                Vector3 cameraPosition = camera.transform.position;
                Vector3 clampedFocus = ClampCameraCenterToMap(context.Loader.Definition, new Vector3(focus.Position.x, 0f, focus.Position.z), camera, width / (float)height);
                camera.transform.position = new Vector3(clampedFocus.x, cameraPosition.y, clampedFocus.z);
            }
            appRootCanvas.renderMode = RenderMode.ScreenSpaceCamera;
            appRootCanvas.worldCamera = camera;
            appRootCanvas.planeDistance = 1f;
            appRootCanvas.sortingOrder = short.MaxValue;
            Canvas.ForceUpdateCanvases();
            RenderTexture.active = renderTexture;
            camera.Render();
            texture.ReadPixels(new Rect(0f, 0f, renderTexture.width, renderTexture.height), 0, 0);
            texture.Apply();
            File.WriteAllBytes(capturePath, texture.EncodeToPNG());
        }
        finally
        {
            camera.targetTexture = previousTarget;
            RenderTexture.active = previousActive;
            camera.ResetAspect();
            appRootCanvas.renderMode = previousRenderMode;
            appRootCanvas.worldCamera = previousCanvasCamera;
            appRootCanvas.planeDistance = previousPlaneDistance;
            appRootCanvas.sortingOrder = previousSortingOrder;
            Object.DestroyImmediate(texture);
            renderTexture.Release();
            Object.DestroyImmediate(renderTexture);
        }

        Assert.IsTrue(File.Exists(capturePath), $"Expected public launch capture at {capturePath}.");
        Assert.Greater(new FileInfo(capturePath).Length, 1024, $"Expected non-empty public launch capture at {capturePath}.");
    }

    private static Vector3 ClampCameraCenterToMap(TacticalMapDefinition definition, Vector3 cameraCenter, Camera camera, float aspect)
    {
        if (definition == null || camera == null || !camera.orthographic)
            return cameraCenter;

        float halfHeight = camera.orthographicSize;
        float halfWidth = halfHeight * aspect;
        float xMin = definition.WorldOrigin.x + halfWidth;
        float xMax = definition.WorldOrigin.x + definition.VisibleWorldSize.x - halfWidth;
        float zMin = definition.WorldOrigin.y + halfHeight;
        float zMax = definition.WorldOrigin.y + definition.VisibleWorldSize.y - halfHeight;
        float mapCenterX = definition.WorldOrigin.x + definition.VisibleWorldSize.x * 0.5f;
        float mapCenterZ = definition.WorldOrigin.y + definition.VisibleWorldSize.y * 0.5f;

        cameraCenter.x = xMin <= xMax ? Mathf.Clamp(cameraCenter.x, xMin, xMax) : mapCenterX;
        cameraCenter.z = zMin <= zMax ? Mathf.Clamp(cameraCenter.z, zMin, zMax) : mapCenterZ;
        return cameraCenter;
    }

    private static string GetMainProjectRoot()
    {
        string projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
        string projectName = new DirectoryInfo(projectRoot).Name;
        if (projectName.StartsWith("WarlineCapture-CodexUnity", System.StringComparison.Ordinal))
            return Path.Combine(Directory.GetParent(projectRoot).FullName, "WarlineCapture");

        return projectRoot;
    }

    private static bool TryFindMissionEntity(EntityManager em, string id, out Entity entity)
    {
        using EntityQuery query = em.CreateEntityQuery(ComponentType.ReadOnly<MissionRuntimeEntityId>());
        using NativeArray<Entity> entities = query.ToEntityArray(Allocator.Temp);
        for (int i = 0; i < entities.Length; i++)
        {
            Entity candidate = entities[i];
            if (!em.Exists(candidate))
                continue;
            if (em.GetComponentData<MissionRuntimeEntityId>(candidate).Value.ToString() != id)
                continue;
            if (!em.HasComponent<LocalTransform>(candidate) || !em.HasComponent<UnitGrid>(candidate))
                continue;

            entity = candidate;
            return true;
        }

        entity = Entity.Null;
        return false;
    }

    private static void AssertNearAnchor(TacticalMapRuntimeLoader loader, EntityManager em, Entity entity, string anchorId)
    {
        Assert.IsTrue(loader.TryGetAnchorWorldPosition(anchorId, out Vector3 anchorWorld), $"{anchorId} should resolve.");
        float3 actual = em.GetComponentData<LocalTransform>(entity).Position;
        Assert.AreEqual(anchorWorld.x, actual.x, 0.25f, $"{anchorId} x should match the spawned entity.");
        Assert.AreEqual(anchorWorld.z, actual.z, 0.25f, $"{anchorId} z should match the spawned entity.");
    }

    private static void MovePlayerIntoAttackRange(EntityManager em, Entity player, Entity enemy)
    {
        UnitGrid enemyGrid = em.GetComponentData<UnitGrid>(enemy);
        int2 playerCell = enemyGrid.Cell + new int2(-1, 0);
        float3 enemyPosition = em.GetComponentData<LocalTransform>(enemy).Position;
        float3 playerPosition = enemyPosition + new float3(-0.08f, 0f, 0f);
        em.SetComponentData(player, new UnitGrid { Cell = playerCell });
        em.SetComponentData(player, LocalTransform.FromPosition(playerPosition));
        if (em.HasComponent<LocalToWorld>(player))
            em.SetComponentData(player, new LocalToWorld { Value = float4x4.Translate(playerPosition) });
        RemoveIfPresent<UnitTarget>(em, player);
        RemoveIfPresent<UnitPathRequest>(em, player);
        RemoveIfPresent<UnitPathFollow>(em, player);
        RemoveIfPresent<UnitPathRange>(em, player);
        RemoveIfPresent<ManualMoveOrderTag>(em, player);
        RemoveIfPresent<UnitPathRetryCooldown>(em, player);
    }

    private static void ForceProtectedEnemyAttackAttempt(M01SceneContext context)
    {
        EntityManager em = context.World.EntityManager;
        LocalTransform playerTransform = em.GetComponentData<LocalTransform>(context.PlayerSquad);
        int2 playerCell = em.GetComponentData<UnitGrid>(context.PlayerSquad).Cell;
        if (em.HasComponent<EngageTarget>(context.EnemyPatrol))
        {
            em.SetComponentData(context.EnemyPatrol, new EngageTarget
            {
                Target = context.PlayerSquad,
                Cell = playerCell,
                Position = playerTransform.Position,
                IsCommanded = 0
            });
        }
        else
        {
            em.AddComponentData(context.EnemyPatrol, new EngageTarget
            {
                Target = context.PlayerSquad,
                Cell = playerCell,
                Position = playerTransform.Position,
                IsCommanded = 0
            });
        }

        UnitAttack attack = em.GetComponentData<UnitAttack>(context.EnemyPatrol);
        attack.Range = 100f;
        attack.Damage = Mathf.Max(attack.Damage, 25);
        em.SetComponentData(context.EnemyPatrol, attack);
        if (em.HasComponent<UnitAttackState>(context.EnemyPatrol))
            em.SetComponentData(context.EnemyPatrol, new UnitAttackState { CooldownRemaining = 0f });
    }

    private static void IssueMoveToCover(M01SceneContext context)
    {
        Assert.IsTrue(context.Loader.TryGetAnchorCell("tutorial.move_target.cover_01", out Vector2Int coverCell), "M01 move-to-cover tutorial anchor should resolve.");
        EntityManager em = context.World.EntityManager;
        int2 targetCell = new(coverCell.x, coverCell.y);
        if (em.HasComponent<UnitTarget>(context.PlayerSquad))
            em.SetComponentData(context.PlayerSquad, new UnitTarget { Cell = targetCell });
        else
            em.AddComponentData(context.PlayerSquad, new UnitTarget { Cell = targetCell });

        if (em.HasComponent<UnitPathRequest>(context.PlayerSquad))
            em.SetComponentData(context.PlayerSquad, new UnitPathRequest { Goal = targetCell });
        else
            em.AddComponentData(context.PlayerSquad, new UnitPathRequest { Goal = targetCell });

        if (!em.HasComponent<ManualMoveOrderTag>(context.PlayerSquad))
            em.AddComponent<ManualMoveOrderTag>(context.PlayerSquad);

        RemoveIfPresent<EngageTarget>(em, context.PlayerSquad);
    }

    private static void InvokeAttackOrder(Entity player, Entity enemy, EntityManager em)
    {
        LocalTransform enemyTransform = em.GetComponentData<LocalTransform>(enemy);
        int2 enemyCell = em.GetComponentData<UnitGrid>(enemy).Cell;
        RemoveIfPresent<ManualMoveOrderTag>(em, player);
        RemoveIfPresent<UnitTarget>(em, player);
        RemoveIfPresent<UnitPathRequest>(em, player);
        RemoveIfPresent<UnitPathFollow>(em, player);
        RemoveIfPresent<UnitPathRange>(em, player);
        if (em.HasComponent<EngageTarget>(player))
        {
            em.SetComponentData(player, new EngageTarget
            {
                Target = enemy,
                Cell = enemyCell,
                Position = enemyTransform.Position,
                IsCommanded = 1
            });
        }
        else
        {
            em.AddComponentData(player, new EngageTarget
            {
                Target = enemy,
                Cell = enemyCell,
                Position = enemyTransform.Position,
                IsCommanded = 1
            });
        }
    }

    private static Vector3 GetCameraGroundCenter(Camera camera)
    {
        Ray ray = camera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        Plane ground = new(Vector3.up, Vector3.zero);
        return ground.Raycast(ray, out float distance) ? ray.GetPoint(distance) : camera.transform.position;
    }

    private static float CalculateOverlapArea(Rect a, Rect b)
    {
        float xMin = Mathf.Max(a.xMin, b.xMin);
        float yMin = Mathf.Max(a.yMin, b.yMin);
        float xMax = Mathf.Min(a.xMax, b.xMax);
        float yMax = Mathf.Min(a.yMax, b.yMax);
        if (xMax <= xMin || yMax <= yMin)
            return 0f;

        return (xMax - xMin) * (yMax - yMin);
    }

    private static void RemoveIfPresent<T>(EntityManager em, Entity entity) where T : unmanaged, IComponentData
    {
        if (em.HasComponent<T>(entity))
            em.RemoveComponent<T>(entity);
    }

    private static async Task NextFrame()
    {
        await Task.Yield();
    }

    private static void SetLogAssertIgnoreFailingMessages(bool value)
    {
        System.Type logAssertType =
            System.Type.GetType("UnityEngine.TestTools.LogAssert, UnityEngine.TestRunner") ??
            System.Type.GetType("UnityEngine.TestTools.LogAssert, UnityEngine.TestFramework");
        PropertyInfo property = logAssertType?.GetProperty("ignoreFailingMessages", BindingFlags.Static | BindingFlags.Public);
        property?.SetValue(null, value);
    }

    private readonly struct M01SceneContext
    {
        public readonly GameBootstrap Bootstrap;
        public readonly World World;
        public readonly TacticalMapRuntimeLoader Loader;
        public readonly Entity PlayerSquad;
        public readonly Entity EnemyPatrol;

        public M01SceneContext(GameBootstrap bootstrap, World world, TacticalMapRuntimeLoader loader, Entity playerSquad, Entity enemyPatrol)
        {
            Bootstrap = bootstrap;
            World = world;
            Loader = loader;
            PlayerSquad = playerSquad;
            EnemyPatrol = enemyPatrol;
        }
    }
}
#endif
