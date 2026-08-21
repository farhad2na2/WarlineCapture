#if UNITY_INCLUDE_TESTS
using System;
using System.IO;
using System.Text;
using Game.Components;
using Game.Missions.Contracts;
using Game.Rendering;
using Game.Runtime;
using Game.Tactical.Contracts;
using Game.UI.Runtime;
using NUnit.Framework;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEditor;
using UnityEngine;

public sealed class M01GuidedMoveRouteTests
{
    [Test]
    public void GuidedMoveUsesValidatedStreetCellsWithoutPathRequest()
    {
        using World world = new(nameof(GuidedMoveUsesValidatedStreetCellsWithoutPathRequest));
        EntityManager entityManager = world.EntityManager;
        BlobAssetReference<OperationMapBlob> mapBlob = default;
        NativeBitArray blocked = default;
        NativeBitArray occupied = default;
        NativeArray<byte> friendlyPass = default;
        NativeList<int2> pathPool = default;
        try
        {
            const int width = 40;
            const int height = 40;
            GridConfig grid = new() { Width = width, Height = height, CellSize = 1f };
            FixedString64Bytes session = new("m01-guided-route");
            Entity runtime = entityManager.CreateEntity(typeof(CampaignMissionRuntimeComponent));
            entityManager.SetComponentData(runtime, new CampaignMissionRuntimeComponent
            {
                MissionId = new FixedString64Bytes("saga.ch01.m01.first_contact"),
                SessionToken = session,
                Phase = MissionPhaseKind.InteractiveBrief,
                Outcome = MissionOutcomeKind.None
            });

            using (BlobBuilder builder = new(Allocator.Temp))
            {
                ref OperationMapBlob map = ref builder.ConstructRoot<OperationMapBlob>();
                BlobBuilderArray<OperationMapAnchorBlob> anchors = builder.Allocate(ref map.Anchors, 1);
                anchors[0] = new OperationMapAnchorBlob
                {
                    Id = new FixedString64Bytes("anchor.ch01.m01.move_target"),
                    Position = GridUtils.CellToWorldCenter(grid, new int2(10, 20)),
                    Radius = 3f
                };
                mapBlob = builder.CreateBlobAssetReference<OperationMapBlob>(Allocator.Persistent);
            }
            Entity metadata = entityManager.CreateEntity(typeof(OperationMapMetadataComponent));
            entityManager.SetComponentData(metadata, new OperationMapMetadataComponent { Blob = mapBlob });

            blocked = new NativeBitArray(width * height, Allocator.Persistent, NativeArrayOptions.ClearMemory);
            occupied = new NativeBitArray(width * height, Allocator.Persistent, NativeArrayOptions.ClearMemory);
            friendlyPass = new NativeArray<byte>(width * height, Allocator.Persistent, NativeArrayOptions.ClearMemory);
            pathPool = new NativeList<int2>(Allocator.Persistent);
            Entity gridEntity = entityManager.CreateEntity(
                typeof(GridConfig), typeof(DynamicBlockerComponent), typeof(DynamicOccupancyComponent),
                typeof(PathPoolComponent));
            entityManager.SetComponentData(gridEntity, grid);
            entityManager.SetComponentData(gridEntity, new DynamicBlockerComponent
            {
                GridSize = width * height,
                Blocked = blocked,
                FriendlyPassFactionIds = friendlyPass
            });
            entityManager.SetComponentData(gridEntity, new DynamicOccupancyComponent
            {
                GridSize = width * height,
                Occupied = occupied
            });
            entityManager.SetComponentData(gridEntity, new PathPoolComponent { Cells = pathPool });
            entityManager.AddBuffer<GridWalkable>(gridEntity);
            entityManager.AddBuffer<GridRoad>(gridEntity);
            entityManager.AddBuffer<GridRoadSidewalk>(gridEntity);
            entityManager.AddBuffer<GridRoadDirt>(gridEntity);
            DynamicBuffer<GridWalkable> walkableBuffer = entityManager.GetBuffer<GridWalkable>(gridEntity);
            DynamicBuffer<GridRoad> roadBuffer = entityManager.GetBuffer<GridRoad>(gridEntity);
            DynamicBuffer<GridRoadSidewalk> sidewalkBuffer = entityManager.GetBuffer<GridRoadSidewalk>(gridEntity);
            DynamicBuffer<GridRoadDirt> dirtBuffer = entityManager.GetBuffer<GridRoadDirt>(gridEntity);
            walkableBuffer.ResizeUninitialized(width * height);
            roadBuffer.ResizeUninitialized(width * height);
            sidewalkBuffer.ResizeUninitialized(width * height);
            dirtBuffer.ResizeUninitialized(width * height);
            for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
            {
                int index = GridUtils.CellToIndex(new int2(x, y), width);
                walkableBuffer[index] = new GridWalkable { Value = 1 };
                // Reproduce the shared dense-city source's sparse road-classification seam.
                // The authored M01 avenue remains visually continuous through this row.
                roadBuffer[index] = new GridRoad { Value = (byte)(x >= 7 && x <= 13 && y != 15 ? 1 : 0) };
                sidewalkBuffer[index] = default;
                dirtBuffer[index] = default;
            }

            Entity unit = entityManager.CreateEntity(
                typeof(CampaignMissionUnitRoleComponent), typeof(UnitGrid), typeof(UnitFootprint), typeof(Faction),
                typeof(UnitMove), typeof(UnitHealth));
            entityManager.SetComponentData(unit, new CampaignMissionUnitRoleComponent
            {
                MissionRoleId = new FixedString64Bytes("role.friendly.command_squad"),
                SessionToken = session
            });
            entityManager.SetComponentData(unit, new UnitGrid { Cell = new int2(10, 10) });
            entityManager.SetComponentData(unit, new UnitFootprint { Size = new int2(1, 1) });
            entityManager.SetComponentData(unit, new Faction { Id = FactionIdentity.PlayerFactionId });
            entityManager.SetComponentData(unit, new UnitHealth { Current = 100, Max = 100 });
            Entity secondUnit = entityManager.CreateEntity(
                typeof(CampaignMissionUnitRoleComponent), typeof(UnitGrid), typeof(UnitFootprint), typeof(Faction),
                typeof(UnitMove), typeof(UnitHealth));
            entityManager.SetComponentData(secondUnit, new CampaignMissionUnitRoleComponent
            {
                MissionRoleId = new FixedString64Bytes("role.friendly.command_squad"),
                SessionToken = session
            });
            entityManager.SetComponentData(secondUnit, new UnitGrid { Cell = new int2(10, 11) });
            entityManager.SetComponentData(secondUnit, new UnitFootprint { Size = new int2(1, 1) });
            entityManager.SetComponentData(secondUnit, new Faction { Id = FactionIdentity.PlayerFactionId });
            entityManager.SetComponentData(secondUnit, new UnitHealth { Current = 100, Max = 100 });

            Entity thirdUnit = CreateGuidedUnit(entityManager, session, new int2(9, 9));

            int2 goal = new(10, 20);
            Assert.That(CampaignMissionGuidedMoveRouteUtility.TryCreateContext(
                entityManager, grid, goal, out CampaignMissionGuidedMoveRouteUtility.Context context), Is.True);
            Assert.That(CampaignMissionGuidedMoveRouteUtility.IsGuidedMovePhaseActive(entityManager), Is.True,
                "The UI-unlock transition frame must already be owned by the four-soldier authored move.");
            CampaignMissionRuntimeComponent movePhaseRuntime =
                entityManager.GetComponentData<CampaignMissionRuntimeComponent>(runtime);
            movePhaseRuntime.Phase = MissionPhaseKind.FindSquad;
            entityManager.SetComponentData(runtime, movePhaseRuntime);
            Assert.That(CampaignMissionGuidedMoveRouteUtility.TryCreateContext(
                entityManager, grid, goal, out _), Is.True,
                "The selection phase must keep the authored route active.");
            movePhaseRuntime.Phase = MissionPhaseKind.MoveToCover;
            entityManager.SetComponentData(runtime, movePhaseRuntime);
            Assert.That(CampaignMissionGuidedMoveRouteUtility.TryCreateContext(
                entityManager, grid, goal, out _), Is.True,
                "The authored route must remain active after mission-phase projection catches up.");
            Assert.That(CampaignMissionGuidedMoveRouteUtility.TryCreateContext(
                entityManager, grid, new int2(2, 2), out CampaignMissionGuidedMoveRouteUtility.Context snappedContext), Is.True,
                "A guided M01 move must not fall through to a partial ordinary selection when the click misses the marker edge.");
            Assert.That(snappedContext.TargetCell, Is.EqualTo(goal));
            using (NativeList<Entity> partialSquad = new(Allocator.Temp))
            {
                Assert.That(CampaignMissionGuidedMoveRouteUtility.TryCollectFullFriendlySquad(
                    entityManager, context, partialSquad), Is.False,
                    "M01 must reject a partial three-soldier recovery instead of accepting a detouring fallback.");
                Assert.That(partialSquad.Length, Is.EqualTo(3));
            }

            Entity fourthUnit = CreateGuidedUnit(entityManager, session, new int2(11, 12));
            using (NativeList<Entity> fullSquad = new(Allocator.Temp))
            {
                Assert.That(CampaignMissionGuidedMoveRouteUtility.TryCollectFullFriendlySquad(
                    entityManager, context, fullSquad), Is.True);
                Assert.That(fullSquad.Length, Is.EqualTo(4),
                    "The guided command must recover the full mission squad even if a cached selection omitted one soldier.");
            }
            using NativeArray<Entity> formation = new(
                new[] { unit, secondUnit, thirdUnit, fourthUnit }, Allocator.Temp);
            var formationGoals = new int2[formation.Length];
            var moveOrderSystem = new UnitMoveOrderSystem();
            Assert.That(CampaignMissionGuidedMoveRouteUtility.TryResolveStreetFormationGoals(
                entityManager,
                gridEntity,
                grid,
                moveOrderSystem,
                formation,
                entityManager.GetBuffer<GridWalkable>(gridEntity).AsNativeArray(),
                blocked,
                friendlyPass,
                occupied,
                moveOrderSystem.BuildSelectedCurrentFootprintCells(entityManager, grid, formation),
                default,
                context,
                formationGoals), Is.True,
                "All four tutorial soldiers must reserve a direct street goal before any order is issued.");
            Assert.That(new System.Collections.Generic.HashSet<int2>(formationGoals).Count, Is.EqualTo(4),
                "Every tutorial soldier needs a distinct destination so none is rejected or left behind.");
            var lateralOffsets = new System.Collections.Generic.HashSet<int>();
            for (int index = 0; index < formationGoals.Length; index++)
            {
                Assert.That(formationGoals[index].y, Is.EqualTo(context.TargetCell.y),
                    "The four soldiers must finish on one readable firing line across the road.");
                lateralOffsets.Add(formationGoals[index].x - context.TargetCell.x);
            }
            Assert.That(lateralOffsets.SetEquals(new[] { -3, -1, 1, 3 }), Is.True,
                "The authored formation must expose all four soldiers instead of hiding one in a single-file column.");

            entityManager.AddComponent<SelectedUnitTag>(unit);
            using (EntityQuery selectedMoveQuery = entityManager.CreateEntityQuery(
                       ComponentType.ReadOnly<SelectedUnitTag>(),
                       ComponentType.ReadOnly<UnitGrid>(),
                       ComponentType.ReadOnly<UnitMove>()))
            using (EntityQuery gridQuery = entityManager.CreateEntityQuery(
                       ComponentType.ReadOnly<GridConfig>(),
                       ComponentType.ReadOnly<GridWalkable>(),
                       ComponentType.ReadOnly<DynamicBlockerComponent>(),
                       ComponentType.ReadOnly<DynamicOccupancyComponent>()))
            using (EntityQuery surfaceQuery = entityManager.CreateEntityQuery(
                       ComponentType.ReadOnly<MapSurfaceComponent>()))
            {
                movePhaseRuntime.Phase = MissionPhaseKind.InteractiveBrief;
                entityManager.SetComponentData(runtime, movePhaseRuntime);
                var selectedMoveSystem = new SelectedMoveOrderCommandSystem();
                SelectedMoveOrderCommandSystem.Result guidedManualResult =
                    selectedMoveSystem.TryIssueMoveOrderToCell(
                        entityManager,
                        selectedMoveQuery,
                        gridQuery,
                        surfaceQuery,
                        moveOrderSystem,
                        new int2(2, 2),
                        new Vector3(2.5f, 0f, 2.5f),
                        currentFrame: 98);
                Assert.That(guidedManualResult.CommandResult.Accepted, Is.True,
                    "Show Me followed by Move must route through the same full-squad operation as Do It.");
                Assert.That(guidedManualResult.MarkerCell, Is.EqualTo(goal),
                    "The accepted marker must acknowledge the authored destination, not the raw click.");
                Assert.That(entityManager.HasComponent<CampaignMissionGuidedMoveInProgressTag>(unit), Is.True);
                Assert.That(entityManager.HasComponent<CampaignMissionGuidedMoveInProgressTag>(secondUnit), Is.True);
                Assert.That(entityManager.HasComponent<CampaignMissionGuidedMoveInProgressTag>(thirdUnit), Is.True);
                Assert.That(entityManager.HasComponent<CampaignMissionGuidedMoveInProgressTag>(fourthUnit), Is.True);
            }

            movePhaseRuntime.Phase = MissionPhaseKind.MoveToCover;
            entityManager.SetComponentData(runtime, movePhaseRuntime);
            int ariaDoItRequest = UnitMoveOrderRequestSystem.EnqueueCampaignGuidedSquadMoveOrder(
                entityManager,
                unit,
                goal,
                currentFrame: 99);
            UnitMoveOrderRequestSystem.ProcessPendingRequests(entityManager);
            Assert.That(UnitMoveOrderRequestSystem.TryGetResult(
                entityManager,
                ariaDoItRequest,
                out UnitMoveOrderResultElement ariaDoItResult), Is.True);
            Assert.That(ariaDoItResult.Issued, Is.EqualTo(1),
                "ARIA Do It must be one accepted squad request, not sequential single-soldier moves.");
            Assert.That(entityManager.HasComponent<CampaignMissionGuidedMoveInProgressTag>(unit), Is.True);
            Assert.That(entityManager.HasComponent<CampaignMissionGuidedMoveInProgressTag>(secondUnit), Is.True);
            Assert.That(entityManager.HasComponent<CampaignMissionGuidedMoveInProgressTag>(thirdUnit), Is.True);
            Assert.That(entityManager.HasComponent<CampaignMissionGuidedMoveInProgressTag>(fourthUnit), Is.True,
                "The fourth soldier must receive its route in the same Do It request as the first three.");

            Assert.That(CampaignMissionGuidedMoveRouteUtility.TryIssueStreetRoute(
                entityManager,
                gridEntity,
                grid,
                moveOrderSystem,
                unit,
                formationGoals[0],
                context,
                100,
                out UnitMoveOrderSystem.MoveOrderCommandResult result), Is.True);
            Assert.That(result.Issued, Is.True);
            Assert.That(entityManager.HasComponent<UnitPathRequest>(unit), Is.False,
                "The authored tutorial move must not enter the general city A* pathfinder.");
            Assert.That(entityManager.HasComponent<CampaignMissionGuidedMoveInProgressTag>(unit), Is.True,
                "The authored tutorial corridor must remain authoritative until this soldier arrives.");
            AssertStoredDirectStreetRoute(
                entityManager, gridEntity, unit, new int2(10, 10), formationGoals[0]);

            Assert.That(CampaignMissionGuidedMoveRouteUtility.TryIssueStreetRoute(
                entityManager,
                gridEntity,
                grid,
                moveOrderSystem,
                secondUnit,
                formationGoals[1],
                context,
                100,
                out UnitMoveOrderSystem.MoveOrderCommandResult secondResult), Is.True,
                "A structural change on the first soldier must not invalidate the next soldier's route.");
            Assert.That(secondResult.Issued, Is.True);
            Assert.That(entityManager.HasComponent<UnitPathRequest>(secondUnit), Is.False);
            AssertStoredDirectStreetRoute(
                entityManager, gridEntity, secondUnit, new int2(10, 11), formationGoals[1]);

            AssertDirectStreetRoute(entityManager, gridEntity, grid, moveOrderSystem, thirdUnit, formationGoals[2], context);
            AssertDirectStreetRoute(entityManager, gridEntity, grid, moveOrderSystem, fourthUnit, formationGoals[3], context);
            Assert.That(entityManager.HasComponent<CampaignMissionGuidedMoveInProgressTag>(secondUnit), Is.True);
            Assert.That(entityManager.HasComponent<CampaignMissionGuidedMoveInProgressTag>(thirdUnit), Is.True);
            Assert.That(entityManager.HasComponent<CampaignMissionGuidedMoveInProgressTag>(fourthUnit), Is.True);
            Assert.That(CampaignMissionRuntimeProgressUtility.AllAliveFriendliesReachedMoveTarget(4, 3), Is.False,
                "The tutorial must not advance while the fourth living soldier is still behind.");
            Assert.That(CampaignMissionRuntimeProgressUtility.AllAliveFriendliesReachedMoveTarget(4, 4), Is.True);
            Assert.That(UnitGridMoveJob.ShouldBlockPathCell(true, true), Is.False,
                "Shared-city occupancy must not repath a soldier off the one authored tutorial corridor.");
            Assert.That(UnitGridMoveJob.ShouldUseGroupedManualStop(false, true, true, false), Is.False,
                "Guided soldiers must reach their distinct reserved cells instead of stopping early behind the squad.");
        }
        finally
        {
            if (mapBlob.IsCreated) mapBlob.Dispose();
            if (blocked.IsCreated) blocked.Dispose();
            if (occupied.IsCreated) occupied.Dispose();
            if (friendlyPass.IsCreated) friendlyPass.Dispose();
            if (pathPool.IsCreated) pathPool.Dispose();
        }
    }

    [Test]
    public void InfantrySelectionOutlineResolutionIsCachedWhenNoOutlineCanBeCreated()
    {
        using World world = new(nameof(InfantrySelectionOutlineResolutionIsCachedWhenNoOutlineCanBeCreated));
        EntityManager entityManager = world.EntityManager;
        Entity marker = entityManager.CreateEntity();
        Entity unit = entityManager.CreateEntity(typeof(SelectedUnitTag));
        entityManager.AddComponentData(unit, new UnitSelectionMarkerInstanceReference { Instance = marker });
        Entity staleRectangle = entityManager.CreateEntity(
            typeof(Unity.Transforms.LocalTransform), typeof(SelectionObjectOutlineTag));
        entityManager.AddBuffer<SelectionObjectOutlineInstanceElement>(marker).Add(
            new SelectionObjectOutlineInstanceElement { Value = staleRectangle });
        entityManager.AddComponent<SelectionObjectOutlineResolvedTag>(marker);
        UnitSelectionObjectOutlinePresentationSystem system =
            world.GetOrCreateSystemManaged<UnitSelectionObjectOutlinePresentationSystem>();

        system.Update();
        Assert.That(entityManager.HasBuffer<SelectionObjectOutlineInstanceElement>(marker), Is.True);
        Assert.That(entityManager.GetBuffer<SelectionObjectOutlineInstanceElement>(marker).Length, Is.Zero);
        Assert.That(entityManager.Exists(staleRectangle), Is.False,
            "Infantry must remove the stale rectangular object outline and keep only its circular ground marker.");
        Assert.That(entityManager.HasComponent<SelectionObjectOutlineResolvedTag>(marker), Is.True,
            "An intentionally empty infantry outline must be cached instead of rescanning the city next frame.");

        system.Update();
        Assert.That(entityManager.HasComponent<SelectionObjectOutlineResolvedTag>(marker), Is.True);
    }

    [Test]
    public void FirstContactSkipsUnrelatedBuildingSimulation()
    {
        using World world = new(nameof(FirstContactSkipsUnrelatedBuildingSimulation));
        Entity mission = world.EntityManager.CreateEntity(typeof(CampaignMissionRuntimeComponent));
        world.EntityManager.SetComponentData(mission, new CampaignMissionRuntimeComponent
        {
            MissionId = new FixedString64Bytes("saga.ch01.m01.first_contact"),
            Phase = MissionPhaseKind.MoveToCover
        });
        Assert.That(GameplayRuntimeUpdateCompositionSystemHelper.ShouldSkipBuildingSimulation(world), Is.True,
            "M01 has no building, production, or economy gameplay and must not run their expensive simulation tick.");

        CampaignMissionRuntimeComponent runtime = world.EntityManager.GetComponentData<CampaignMissionRuntimeComponent>(mission);
        runtime.MissionId = new FixedString64Bytes("saga.ch01.m02.placeholder");
        world.EntityManager.SetComponentData(mission, runtime);
        Assert.That(GameplayRuntimeUpdateCompositionSystemHelper.ShouldSkipBuildingSimulation(world), Is.False,
            "The campaign-only performance policy must preserve every other mode's building runtime.");
    }

    [Test]
    public void AttackCanArmBeforeTargetSelectionWhileAutoEngagementIsHeld()
    {
        using World world = new(nameof(AttackCanArmBeforeTargetSelectionWhileAutoEngagementIsHeld));
        EntityManager entityManager = world.EntityManager;
        Entity command = entityManager.CreateEntity(typeof(RtsSelectionInputStateComponent));
        entityManager.AddBuffer<RtsSelectionCommandIntentRequestElement>(command).Add(
            new RtsSelectionCommandIntentRequestElement
            {
                Kind = RtsSelectionCommandIntentKind.EnterAttackTargetMode,
                RequestId = 1,
                Frame = 10
            });
        Entity runtime = entityManager.CreateEntity(typeof(RuntimeGameplayStateComponent));
        entityManager.SetComponentData(runtime, new RuntimeGameplayStateComponent
        {
            SelectionModeActive = 1
        });
        Entity soldier = entityManager.CreateEntity(
            typeof(SelectedUnitTag), typeof(Faction), typeof(UnitMove), typeof(UnitCombat),
            typeof(UnitAttack), typeof(UnitHealth), typeof(Unity.Transforms.LocalTransform));
        entityManager.SetComponentData(soldier, new Faction { Id = FactionIdentity.PlayerFactionId });
        UnitCombat combat = new() { CanAttack = 1, AutoEngage = 1 };
        CampaignMissionPatrolOrderSystem.ApplyTutorialCombatPolicy(ref combat, releaseCombat: false);
        entityManager.SetComponentData(soldier, combat);
        entityManager.SetComponentData(soldier, new UnitHealth { Current = 100, Max = 100 });

        Assert.That(combat.CanAttack, Is.EqualTo(1),
            "The tutorial hold must preserve manual attack capability.");
        Assert.That(combat.AutoEngage, Is.Zero,
            "The tutorial hold must suppress only autonomous target acquisition.");
        Assert.That(RtsSelectionAttackTargetModeCommandSystem.ProcessPendingRequests(
            entityManager,
            currentFrame: 10,
            out bool accepted,
            out bool airDefenseAutoEngageOnly,
            out TacticalCommandReasonCode rejectionReason), Is.True);
        Assert.That(accepted, Is.True,
            "Pressing Attack must arm target-selection mode before any target is validated.");
        Assert.That(airDefenseAutoEngageOnly, Is.False);
        Assert.That(rejectionReason, Is.EqualTo(TacticalCommandReasonCode.None));
        RtsSelectionInputStateComponent input =
            entityManager.GetComponentData<RtsSelectionInputStateComponent>(command);
        Assert.That((TacticalCommandMode)input.ActiveCommandMode, Is.EqualTo(TacticalCommandMode.Attack));
        Assert.That(input.ActiveCommandModeRequiresWorldTarget, Is.EqualTo(1));
    }

    [Test]
    public void GuidedArrivalAdvancesAndCommandedAttackSurvivesPreEngageHold()
    {
        Assert.That(CampaignMissionRuntimeSystem.IsRosterProjectionReady(
                expectedCount: 4, observedCount: 0, phase: MissionPhaseKind.FindSquad), Is.False,
            "A spawn frame before health initialization must not settle a false squad defeat.");
        Assert.That(CampaignMissionRuntimeSystem.IsRosterProjectionReady(
                expectedCount: 4, observedCount: 3, phase: MissionPhaseKind.MoveToCover), Is.False,
            "A partial pre-combat roster must wait for the authored fourth soldier.");
        Assert.That(CampaignMissionRuntimeSystem.IsRosterProjectionReady(
                expectedCount: 4, observedCount: 4, phase: MissionPhaseKind.MoveToCover), Is.True);
        Assert.That(CampaignMissionRuntimeSystem.IsRosterProjectionReady(
                expectedCount: 4, observedCount: 0, phase: MissionPhaseKind.Engage), Is.True,
            "Once combat begins, removed entities represent real losses and must remain authoritative.");

        CampaignMissionRuntimeComponent initializingMove = new()
        {
            Version = 4,
            SourceVersion = 1,
            MissionId = new FixedString64Bytes("saga.ch01.m01.first_contact"),
            ScenarioId = new FixedString64Bytes("scenario.ch01.m01.first_contact"),
            OperationMapId = new FixedString64Bytes("opmap.ch01.district_edge_01"),
            SessionToken = new FixedString64Bytes("m01-initializing-roster"),
            AttemptOrdinal = 1,
            DeterministicSeed = 1234,
            Phase = MissionPhaseKind.MoveToCover,
            Outcome = MissionOutcomeKind.None,
            LaunchOrigin = MissionLaunchOriginKind.CampaignOperations
        };
        CampaignMissionAttemptFactsComponent initializingFacts = new()
        {
            CommandSquadSpawned = 1,
            CommandSquadAlive = 0,
            SquadLossCount = 0
        };
        Assert.That(CampaignMissionRuntimeSystem.TryEvaluate(
                in initializingMove, in initializingFacts, commandSquadSelected: true, out _), Is.False,
            "A transient zero-alive initialization frame without a recorded loss cannot settle defeat.");
        initializingFacts.SquadLossCount = 4;
        Assert.That(CampaignMissionRuntimeSystem.TryEvaluate(
                in initializingMove, in initializingFacts, commandSquadSelected: true,
                out CampaignMissionRuntimeComponent defeated), Is.True);
        Assert.That(defeated.Outcome, Is.EqualTo(MissionOutcomeKind.Defeat),
            "A real four-soldier loss must still settle defeat.");

        Assert.That(CampaignMissionRuntimeProgressUtility.IsAtMoveTarget(
                new float3(100f, 0f, 100f),
                hasUnitGrid: true,
                unitCell: new int2(21, 20),
                targetWorld: new float3(20.1f, 0f, 20.1f),
                targetRadius: 0.25f,
                hasGrid: true,
                targetCell: new int2(20, 20),
                targetRadiusCells: 1),
            Is.True,
            "Reaching an accepted guided formation cell must advance past Move even when the anchor is not cell-aligned.");

        EngageTarget automatic = new() { IsCommanded = 0 };
        EngageTarget commanded = new() { IsCommanded = 1 };
        Assert.That(CampaignMissionPatrolOrderSystem.ShouldRemovePreEngageTarget(
                automatic, FactionIdentity.PlayerFactionId), Is.True,
            "Automatic combat must remain held during the tutorial.");
        Assert.That(CampaignMissionPatrolOrderSystem.ShouldRemovePreEngageTarget(
                commanded, FactionIdentity.PlayerFactionId), Is.False,
            "The explicit enemy click must survive until the mission writer observes it and enters Engage.");
        Assert.That(CampaignMissionPatrolOrderSystem.ShouldRemovePreEngageTarget(commanded, 2), Is.True,
            "A hostile AI order must not be mistaken for the player's confirming attack input.");

        UnitCombat combat = new() { CanAttack = 1, AutoEngage = 0 };
        Assert.That(CampaignMissionPatrolOrderSystem.ShouldReleaseCombat(MissionPhaseKind.ConfirmThreat), Is.False);
        Assert.That(CampaignMissionPatrolOrderSystem.ShouldReleaseCombat(MissionPhaseKind.Engage), Is.True);
        Assert.That(CampaignMissionPatrolOrderSystem.ShouldIssuePatrolRoute(MissionPhaseKind.MoveToCover), Is.False,
            "The civic-hall patrol must hold while the player learns the move flow.");
        Assert.That(CampaignMissionPatrolOrderSystem.ShouldIssuePatrolRoute(MissionPhaseKind.ConfirmThreat), Is.False,
            "The enemies must remain staged until the player confirms an attack target.");
        Assert.That(CampaignMissionPatrolOrderSystem.ShouldIssuePatrolRoute(MissionPhaseKind.Engage), Is.True,
            "The patrol route may release only after the explicit attack advances the mission.");
        CampaignMissionPatrolOrderSystem.ApplyTutorialCombatPolicy(
            ref combat,
            CampaignMissionPatrolOrderSystem.ShouldReleaseCombat(MissionPhaseKind.Engage));
        Assert.That(combat.AutoEngage, Is.EqualTo(1),
            "Engage must continuously release auto-acquisition so the squad attacks the rest of the nearby patrol after its first target dies.");
    }

    [Test]
    public void InfantryMarkerUsesBakedVariantReferencesToHideEveryRectangle()
    {
        using World world = new(nameof(InfantryMarkerUsesBakedVariantReferencesToHideEveryRectangle));
        EntityManager entityManager = world.EntityManager;
        Entity ring = entityManager.CreateEntity(typeof(LocalTransform));
        Entity fill = entityManager.CreateEntity(typeof(LocalTransform));
        Entity brackets = entityManager.CreateEntity(typeof(LocalTransform));
        Entity frame = entityManager.CreateEntity(typeof(LocalTransform));
        entityManager.SetComponentData(ring, LocalTransform.FromScale(1f));
        entityManager.SetComponentData(fill, LocalTransform.FromScale(1f));
        entityManager.SetComponentData(brackets, LocalTransform.FromScale(1f));
        entityManager.SetComponentData(frame, LocalTransform.FromScale(1f));
        Entity marker = entityManager.CreateEntity(typeof(SelectionMarkerVariantVisuals));
        entityManager.SetComponentData(marker, new SelectionMarkerVariantVisuals
        {
            InfantryGroundRing = ring,
            VehicleFootprintFill = fill,
            VehicleCornerBrackets = brackets,
            VehicleBoundsFrame = frame
        });

        UnitSelectionMarkerSystem.ApplyVehicleVariantVisibility(
            entityManager, marker, usesVehicleMarker: false, isAirUnit: false);

        Assert.That(entityManager.GetComponentData<LocalTransform>(ring).Scale, Is.EqualTo(1f));
        Assert.That(entityManager.GetComponentData<LocalTransform>(fill).Scale, Is.Zero);
        Assert.That(entityManager.GetComponentData<LocalTransform>(brackets).Scale, Is.Zero);
        Assert.That(entityManager.GetComponentData<LocalTransform>(frame).Scale, Is.Zero);
    }

    [Test]
    public void CommandedSquadAttackRedistributesAcrossEverySurvivingPatrolEnemy()
    {
        using World world = new(nameof(CommandedSquadAttackRedistributesAcrossEverySurvivingPatrolEnemy));
        EntityManager entityManager = world.EntityManager;
        FixedString64Bytes session = new("m01-commanded-attack-continuation");
        Entity runtime = entityManager.CreateEntity(typeof(CampaignMissionRuntimeComponent));
        entityManager.SetComponentData(runtime, new CampaignMissionRuntimeComponent
        {
            MissionId = new FixedString64Bytes("saga.ch01.m01.first_contact"),
            SessionToken = session,
            Phase = MissionPhaseKind.ConfirmThreat,
            Outcome = MissionOutcomeKind.None
        });
        Entity defeated = CreateMissionCombatant(entityManager, session, factionId: 2, health: 0, new float3(6f, 0f, 0f));
        Entity hostileA = CreateMissionCombatant(entityManager, session, factionId: 2, health: 100, new float3(8f, 0f, -1f));
        Entity hostileB = CreateMissionCombatant(entityManager, session, factionId: 2, health: 100, new float3(8f, 0f, 1f));
        Entity[] squad = new Entity[4];
        for (int index = 0; index < squad.Length; index++)
        {
            squad[index] = CreateMissionCombatant(
                entityManager,
                session,
                FactionIdentity.PlayerFactionId,
                health: 100,
                new float3(index, 0f, index % 2));
            entityManager.AddComponentData(squad[index], new EngageTarget
            {
                Target = defeated,
                IsCommanded = 1
            });
        }

        Assert.That(CampaignMissionGroupAttackUtility.TryContinueActiveMissionSquadAttack(entityManager), Is.True,
            "ARIA Do It must immediately issue one commanded attack across the whole active M01 squad.");

        int assignedA = 0;
        int assignedB = 0;
        foreach (Entity friendly in squad)
        {
            EngageTarget continuation = entityManager.GetComponentData<EngageTarget>(friendly);
            Assert.That(continuation.Target, Is.Not.EqualTo(defeated));
            Assert.That(continuation.IsCommanded, Is.EqualTo(1));
            if (continuation.Target == hostileA) assignedA++;
            if (continuation.Target == hostileB) assignedB++;
        }
        Assert.That(assignedA, Is.EqualTo(2));
        Assert.That(assignedB, Is.EqualTo(2));
    }

    [Test]
    public void ThirdPatrolDeathSettlesMissionVictoryInTheSameRuntimeUpdate()
    {
        CampaignMissionRuntimeComponent runtime = new()
        {
            Version = 10,
            SourceVersion = 1,
            MissionId = new FixedString64Bytes("saga.ch01.m01.first_contact"),
            ScenarioId = new FixedString64Bytes("scenario.ch01.m01.first_contact"),
            OperationMapId = new FixedString64Bytes("opmap.ch01.district_edge_01"),
            SessionToken = new FixedString64Bytes("m01-third-death-victory"),
            AttemptOrdinal = 1,
            DeterministicSeed = 1234,
            Phase = MissionPhaseKind.Engage,
            Outcome = MissionOutcomeKind.None,
            LaunchOrigin = MissionLaunchOriginKind.CampaignOperations
        };
        CampaignMissionAttemptFactsComponent facts = new()
        {
            CommandSquadSpawned = 1,
            CommandSquadAlive = 1,
            HostileTotalCount = 3,
            HostileDefeatedCount = 3
        };

        Assert.That(CampaignMissionRuntimeProgressUtility.TryEvaluateSettled(
            in runtime, in facts, commandSquadSelected: true, out CampaignMissionRuntimeComponent result), Is.True);
        Assert.That(result.Phase, Is.EqualTo(MissionPhaseKind.Result));
        Assert.That(result.Outcome, Is.EqualTo(MissionOutcomeKind.Victory));
        Assert.That(result.ReturnDestination, Is.EqualTo(MissionReturnDestinationKind.CampaignOperations));
        Assert.That(result.Version, Is.EqualTo(runtime.Version + 2),
            "The mission must retain both authored transitions while exposing the victory atomically.");
    }

    [Test]
    public void ResultPresentationPrefabOwnsConfiguredBinder()
    {
        const string appCanvasPath = "Assets/Game/Prefabs/UI/Shell/UIShellAppCanvas.prefab";
        GameObject appCanvas = AssetDatabase.LoadAssetAtPath<GameObject>(appCanvasPath);

        Assert.That(appCanvas, Is.Not.Null);
        Assert.That(appCanvas.GetComponent<CampaignMissionHudResultBinder>(), Is.Not.Null,
            "The app canvas prefab must own the result binder instead of growing the guarded shell bridge.");
    }

    [Test]
    public void ResultPopupStretchesInsideEverySupportedScreen()
    {
        const string resultPath = "Assets/Game/Prefabs/UI/Popups/MissionResultPopup.prefab";
        GameObject resultPopup = AssetDatabase.LoadAssetAtPath<GameObject>(resultPath);
        RectTransform rect = resultPopup != null ? resultPopup.GetComponent<RectTransform>() : null;

        Assert.That(rect, Is.Not.Null);
        Assert.That(rect.anchorMin, Is.EqualTo(Vector2.zero));
        Assert.That(rect.anchorMax, Is.EqualTo(Vector2.one));
        Assert.That(rect.anchoredPosition, Is.EqualTo(Vector2.zero));
        Assert.That(rect.sizeDelta, Is.EqualTo(Vector2.zero));
        Assert.That(rect.localScale, Is.EqualTo(Vector3.one));
    }

    private static Entity CreateMissionCombatant(
        EntityManager entityManager,
        FixedString64Bytes session,
        byte factionId,
        int health,
        float3 position)
    {
        Entity entity = entityManager.CreateEntity(
            typeof(CampaignMissionUnitRoleComponent),
            typeof(Faction),
            typeof(UnitHealth),
            typeof(UnitCombat),
            typeof(LocalTransform),
            typeof(UnitGrid));
        entityManager.SetComponentData(entity, new CampaignMissionUnitRoleComponent { SessionToken = session });
        entityManager.SetComponentData(entity, new Faction { Id = factionId });
        entityManager.SetComponentData(entity, new UnitHealth { Current = health, Max = 100 });
        entityManager.SetComponentData(entity, new UnitCombat { CanAttack = 1, AutoEngage = 1 });
        entityManager.SetComponentData(entity, LocalTransform.FromPosition(position));
        entityManager.SetComponentData(entity, new UnitGrid { Cell = new int2((int)position.x, (int)position.z) });
        return entity;
    }

    private static Entity CreateGuidedUnit(
        EntityManager entityManager,
        FixedString64Bytes session,
        int2 cell)
    {
        Entity unit = entityManager.CreateEntity(
            typeof(CampaignMissionUnitRoleComponent), typeof(UnitGrid), typeof(UnitFootprint), typeof(Faction),
            typeof(UnitMove), typeof(UnitHealth));
        entityManager.SetComponentData(unit, new CampaignMissionUnitRoleComponent
        {
            MissionRoleId = new FixedString64Bytes("role.friendly.command_squad"),
            SessionToken = session
        });
        entityManager.SetComponentData(unit, new UnitGrid { Cell = cell });
        entityManager.SetComponentData(unit, new UnitFootprint { Size = new int2(1, 1) });
        entityManager.SetComponentData(unit, new Faction { Id = FactionIdentity.PlayerFactionId });
        entityManager.SetComponentData(unit, new UnitHealth { Current = 100, Max = 100 });
        return unit;
    }

    private static void AssertDirectStreetRoute(
        EntityManager entityManager,
        Entity gridEntity,
        in GridConfig grid,
        UnitMoveOrderSystem moveOrderSystem,
        Entity unit,
        int2 goal,
        in CampaignMissionGuidedMoveRouteUtility.Context context)
    {
        int2 start = entityManager.GetComponentData<UnitGrid>(unit).Cell;
        Assert.That(CampaignMissionGuidedMoveRouteUtility.TryIssueStreetRoute(
            entityManager, gridEntity, grid, moveOrderSystem, unit, goal, context, 100,
            out UnitMoveOrderSystem.MoveOrderCommandResult result), Is.True);
        Assert.That(result.Issued, Is.True);
        Assert.That(entityManager.HasComponent<UnitPathRequest>(unit), Is.False);
        AssertStoredDirectStreetRoute(entityManager, gridEntity, unit, start, goal);
    }

    private static void AssertStoredDirectStreetRoute(
        EntityManager entityManager,
        Entity gridEntity,
        Entity unit,
        int2 start,
        int2 goal)
    {
        UnitPathRange range = entityManager.GetComponentData<UnitPathRange>(unit);
        NativeList<int2> cells = entityManager.GetComponentData<PathPoolComponent>(gridEntity).Cells;
        int previousDistance = math.csum(math.abs(goal - start));
        for (int index = range.Start; index < range.Start + range.Length; index++)
        {
            int distance = math.csum(math.abs(goal - cells[index]));
            Assert.That(distance, Is.LessThan(previousDistance),
                "The authored tutorial route must progress directly toward its target without a detour.");
            previousDistance = distance;
        }
        Assert.That(cells[range.Start + range.Length - 1], Is.EqualTo(goal));
    }
}

internal static class M01OwnerFeedbackValidation
{
    private const string Marker = "[M01OwnerFeedbackValidation] result=Passed tests=9";

    [MenuItem("Game/Missions/M01/Log Live Owner Feedback State _F9")]
    public static void LogLiveState()
    {
        World world = World.DefaultGameObjectInjectionWorld;
        if (world == null || !world.IsCreated)
        {
            Debug.LogError("[M01LiveState] world=Unavailable");
            return;
        }

        EntityManager entityManager = world.EntityManager;
        using EntityQuery rootQuery = entityManager.CreateEntityQuery(
            ComponentType.ReadOnly<CampaignMissionRootComponent>());
        if (rootQuery.CalculateEntityCount() != 1)
        {
            Debug.LogError($"[M01LiveState] rootCount={rootQuery.CalculateEntityCount()}");
            return;
        }

        Entity root = rootQuery.GetSingletonEntity();
        CampaignMissionRuntimeComponent runtime =
            entityManager.GetComponentData<CampaignMissionRuntimeComponent>(root);
        CampaignMissionAttemptFactsComponent facts =
            entityManager.GetComponentData<CampaignMissionAttemptFactsComponent>(root);
        bool hasResult = entityManager.HasComponent<CampaignMissionResultComponent>(root);
        CampaignMissionResultComponent result = hasResult
            ? entityManager.GetComponentData<CampaignMissionResultComponent>(root)
            : default;
        int settlementCount = entityManager.HasBuffer<CampaignMissionSettlementResultElement>(root)
            ? entityManager.GetBuffer<CampaignMissionSettlementResultElement>(root, true).Length
            : -1;
        CampaignMissionSettlementResultElement settlement = settlementCount > 0
            ? entityManager.GetBuffer<CampaignMissionSettlementResultElement>(root, true)[settlementCount - 1]
            : default;

        int friendlyEntities = 0;
        int friendlyAlive = 0;
        StringBuilder friendlyState = new();
        using (EntityQuery friendlyQuery = entityManager.CreateEntityQuery(
                   ComponentType.ReadOnly<CampaignMissionUnitRoleComponent>(),
                   ComponentType.ReadOnly<Faction>(),
                   ComponentType.ReadOnly<UnitHealth>(),
                   ComponentType.ReadOnly<UnitGrid>()))
        using (NativeArray<Entity> friendlies = friendlyQuery.ToEntityArray(Allocator.Temp))
        {
            for (int index = 0; index < friendlies.Length; index++)
            {
                Entity entity = friendlies[index];
                CampaignMissionUnitRoleComponent role =
                    entityManager.GetComponentData<CampaignMissionUnitRoleComponent>(entity);
                Faction faction = entityManager.GetComponentData<Faction>(entity);
                if (!role.SessionToken.Equals(runtime.SessionToken) ||
                    !FactionIdentity.IsPlayerControlled(faction.Id))
                    continue;
                UnitHealth unitHealth = entityManager.GetComponentData<UnitHealth>(entity);
                UnitGrid unitGrid = entityManager.GetComponentData<UnitGrid>(entity);
                friendlyEntities++;
                if (unitHealth.Current > 0) friendlyAlive++;
                if (friendlyState.Length > 0) friendlyState.Append(';');
                friendlyState.Append(entity.Index).Append('@').Append(unitGrid.Cell)
                    .Append(" hp=").Append(unitHealth.Current)
                    .Append(" guided=").Append(
                        entityManager.HasComponent<CampaignMissionGuidedMoveInProgressTag>(entity) ? 1 : 0);
            }
        }

        Debug.Log(
            $"[M01LiveState] phase={runtime.Phase} outcome={runtime.Outcome} " +
            $"runtimeVersion={runtime.Version} squadAlive={facts.CommandSquadAlive} " +
            $"friendlyEntities={friendlyEntities} friendlyAlive={friendlyAlive} " +
            $"squadLosses={facts.SquadLossCount} friendlyState={friendlyState} " +
            $"hostiles={facts.HostileDefeatedCount}/{facts.HostileTotalCount} " +
            $"hasResult={(hasResult ? 1 : 0)} resultVersion={result.SourceVersion} " +
            $"resultOutcome={result.Outcome} runKind={runtime.RunKind} launchOrigin={runtime.LaunchOrigin} " +
            $"runtimeReturn={runtime.ReturnDestination} resultReturn={result.ReturnDestination} " +
            $"settlements={settlementCount} settlementVersion={settlement.SourceVersion} " +
            $"settlementAccepted={settlement.Accepted} settlementReason={settlement.ReasonCode}");
    }

    [MenuItem("Game/Missions/M01/Validate Owner Feedback Fixes _F8")]
    public static void RunFromMenu()
    {
        int passed = 0;
        try
        {
            M01FirstContactAnchorTests.RunFocusedValidation();
            Game.Editor.M01FirstContactConfigBuilder.RefreshOperationMapCatalogContentPack();
            passed++;
            RunFocused(ProductionSourceGrowthArchitectureTests.RunFocusedValidation,
                nameof(ProductionSourceGrowthArchitectureTests));
            passed++;
            new M01GuidedMoveRouteTests().GuidedMoveUsesValidatedStreetCellsWithoutPathRequest();
            new M01GuidedMoveRouteTests().InfantrySelectionOutlineResolutionIsCachedWhenNoOutlineCanBeCreated();
            new M01GuidedMoveRouteTests().FirstContactSkipsUnrelatedBuildingSimulation();
            new M01GuidedMoveRouteTests().AttackCanArmBeforeTargetSelectionWhileAutoEngagementIsHeld();
            new M01GuidedMoveRouteTests().GuidedArrivalAdvancesAndCommandedAttackSurvivesPreEngageHold();
            new M01GuidedMoveRouteTests().InfantryMarkerUsesBakedVariantReferencesToHideEveryRectangle();
            new M01GuidedMoveRouteTests().CommandedSquadAttackRedistributesAcrossEverySurvivingPatrolEnemy();
            new M01GuidedMoveRouteTests().ThirdPatrolDeathSettlesMissionVictoryInTheSameRuntimeUpdate();
            new M01GuidedMoveRouteTests().ResultPresentationPrefabOwnsConfiguredBinder();
            new M01GuidedMoveRouteTests().ResultPopupStretchesInsideEverySupportedScreen();
            passed++;
            RunFocused(AssistantCommandIntentSystemTests.RunFocusedValidation, "ARIA camera/command intent");
            passed++;
            RunFocused(M01FirstContactGuidanceTests.RunFocusedValidation, "M01 guidance projection");
            passed++;
            RunFocused(M01FirstContactHudRestrictionTests.RunFocusedValidation, "M01 disabled/grayscale HUD");
            passed++;

            var assistantUi = new MatchHudAssistantUiSystemHelperTests();
            try
            {
                assistantUi.GuidedCommandHighlight_PointsAtCommandBeforeWorldTarget();
            }
            finally
            {
                assistantUi.TearDown();
            }
            var selection = new RtsSelectionInputSystemTests();
            selection.SetUp();
            try
            {
                selection.AttackTargetLookup_SnapsAriaAttackPreviewToHighlightedEnemy();
                selection.PointerTargetCommandSystem_UsesBoundaryPassForResolvedCommandTargets();
            }
            finally
            {
                selection.TearDown();
            }
            passed++;
            Debug.Log(Marker);
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            Debug.LogError($"[M01OwnerFeedbackValidation] result=Failed passed={passed}");
        }
    }

    [MenuItem("Game/Missions/M01/Capture Live Owner Feedback _F10")]
    public static void CaptureLiveState()
    {
        string directory = Path.GetFullPath("Temp/AgentQa");
        Directory.CreateDirectory(directory);
        string path = Path.Combine(directory, "m01-live-owner-feedback.png");
        ScreenCapture.CaptureScreenshot(path);
        Debug.Log($"[M01LiveCapture] requested={path}");
    }

    private static void RunFocused(Action validation, string label)
    {
        ValidationExit.ClearLastExitCode();
        using (ValidationExit.SuppressProcessExit())
            validation();
        if (ValidationExit.LastExitCode != 0)
            throw new InvalidOperationException($"{label} failed with exit code {ValidationExit.LastExitCode}.");
    }
}
#endif
