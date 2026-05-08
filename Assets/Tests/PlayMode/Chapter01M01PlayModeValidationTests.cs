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
        await WaitForMissionSpriteRenderers(context);
        AssertM01ProductionPlayerVisibleState(context, "Quick Custom public launch");
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
        await WaitForMissionSpriteRenderers(context);
        AssertM01ProductionPlayerVisibleState(context, "Campaign public launch");
        CapturePlayerView(context, uiBootstrap.AppCanvasInstance, "campaign-public-m01");
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

    private static async Task WaitForMissionSpriteRenderers(M01SceneContext context)
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
        if (!em.Exists(entity) || !em.HasComponent<MissionRuntimeSpriteRendererRuntime>(entity))
            return false;

        MissionRuntimeSpriteRendererRuntime runtime = em.GetComponentObject<MissionRuntimeSpriteRendererRuntime>(entity);
        return runtime.Renderer != null &&
            runtime.Renderer.enabled &&
            runtime.Renderer.sprite != null &&
            runtime.Renderer.gameObject.activeInHierarchy;
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
            None = new[] { ComponentType.ReadOnly<DisableRendering>() }
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
            None = new[] { ComponentType.ReadOnly<DisableRendering>() }
        });
        Assert.AreEqual(0, query.CalculateEntityCount(), $"{entryPath}: legacy ECS mesh renderers must be suppressed so the first visible state is the 2D/isometric production slice.");
    }

    private static void AssertMissionRendererVisible(EntityManager em, Entity entity, string label)
    {
        Assert.IsTrue(em.HasComponent<MissionRuntimeSpriteRendererRuntime>(entity), $"{label} should have a runtime sprite renderer component.");
        MissionRuntimeSpriteRendererRuntime runtime = em.GetComponentObject<MissionRuntimeSpriteRendererRuntime>(entity);
        Assert.NotNull(runtime.Renderer, $"{label} should expose a sprite renderer reference.");
        Assert.IsTrue(runtime.Renderer.enabled, $"{label} sprite renderer should be enabled.");
        Assert.NotNull(runtime.Renderer.sprite, $"{label} sprite renderer should have a sprite.");
        Assert.IsTrue(runtime.Renderer.gameObject.activeInHierarchy, $"{label} sprite renderer GameObject should be active in hierarchy.");
        StringAssert.StartsWith("M01Sprite_", runtime.Renderer.gameObject.name, $"{label} should use the M01 sprite renderer object.");
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

    private static void CapturePlayerViewAtResolution(M01SceneContext context, GameObject appCanvas, string capturePath, int width, int height)
    {
        if (File.Exists(capturePath))
            File.Delete(capturePath);

        Camera camera = context.Bootstrap.WorldCamera;
        RenderTexture previousTarget = camera.targetTexture;
        RenderTexture previousActive = RenderTexture.active;
        RenderTexture renderTexture = new(width, height, 24, RenderTextureFormat.ARGB32);
        Texture2D texture = new(width, height, TextureFormat.RGBA32, false);
        try
        {
            camera.targetTexture = renderTexture;
            camera.aspect = width / (float)height;
            context.Bootstrap.ApplyM01ProductionCameraPoseForCurrentAspect();
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
            Object.DestroyImmediate(texture);
            renderTexture.Release();
            Object.DestroyImmediate(renderTexture);
        }

        Assert.IsTrue(File.Exists(capturePath), $"Expected public launch capture at {capturePath}.");
        Assert.Greater(new FileInfo(capturePath).Length, 1024, $"Expected non-empty public launch capture at {capturePath}.");
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

    private static void InvokeAttackOrder(Entity player, Entity enemy, EntityManager em)
    {
        LocalTransform enemyTransform = em.GetComponentData<LocalTransform>(enemy);
        int2 enemyCell = em.GetComponentData<UnitGrid>(enemy).Cell;
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
