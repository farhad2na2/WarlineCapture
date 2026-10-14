using System;
using System.Linq;
using Game.Configs;
using Game.Components;
using Game.Runtime;
using Game.Skirmish.Contracts;
using NUnit.Framework;
using UnityEngine;
using Unity.Entities;
using Unity.Rendering;
using Game.Rendering;

namespace Game.Tests.Editor
{
    public sealed class SkirmishCombinedArmsTests
    {
        [Test]
        public void CombinedRosterPreservesArmorAndAirWithPaidReadiness()
        {
            var army = SkirmishArmyProfileConfig.ResolveCached(SkirmishArmyProfileId.CombinedArms);
            var overlays = SkirmishRoleOverlayCatalog.CreateCombinedArmsSlice();
            Assert.AreEqual(22, overlays.Length);
            Assert.AreEqual(22, overlays.Select(r => r.RoleId).Distinct().Count());
            Assert.IsTrue(army.Allows(SkirmishRoleIds.Tank));
            Assert.IsTrue(army.Allows(SkirmishRoleIds.ApcHeavy));
            Assert.IsTrue(army.Allows(SkirmishRoleIds.Siege));
            Assert.IsTrue(army.Allows(SkirmishRoleIds.Fighter));
            Assert.AreEqual(SkirmishReadinessStage.Field, SkirmishProductionEligibility.RequiredReadiness(SkirmishRoleKind.AntiAir));
            Assert.AreEqual(SkirmishReadinessStage.Established, SkirmishProductionEligibility.RequiredReadiness(SkirmishRoleKind.Tank));
            Assert.AreEqual(SkirmishReadinessStage.Established, SkirmishProductionEligibility.RequiredReadiness(SkirmishRoleKind.AttackHeliLight));
            Assert.AreEqual(SkirmishReadinessStage.FullArsenal, SkirmishProductionEligibility.RequiredReadiness(SkirmishRoleKind.Siege));
            Assert.IsTrue(SkirmishRoleOverlayCatalog.TryGet(overlays, SkirmishRoleKind.ApcHeavy, out var heavy));
            Assert.AreEqual(280, heavy.MaterialsCost);
            Assert.IsTrue(SkirmishRoleOverlayCatalog.TryGet(overlays, SkirmishRoleKind.Siege, out var siege));
            Assert.AreEqual(420, siege.MaterialsCost);
            Assert.AreEqual(SkirmishTargetDomain.Ground | SkirmishTargetDomain.Structure, siege.TargetDomains);
        }

        [Test]
        public void S005AllSizesAndDifficultiesPreserveMatrixAndFieldStart()
        {
            Assert.IsTrue(SkirmishSetupMatrixTable.TryLoadPackaged(out var matrix, out var error), error);
            var authored = SkirmishExpansionCatalogConfig.Load().ToAuthoredSet();
            var definition = authored.DefinitionS005;
            Assert.IsNotNull(definition);
            foreach (var size in new[] { SkirmishSizeId.Standard, SkirmishSizeId.War, SkirmishSizeId.LargeWar })
            foreach (var difficulty in new[] { SkirmishDifficultyId.Recruit, SkirmishDifficultyId.Regular, SkirmishDifficultyId.Veteran, SkirmishDifficultyId.Commander })
            {
                Assert.IsTrue(SkirmishSetupCompiler.TryCompile(definition, difficulty, size, 104734,
                    new SkirmishContentManifest { RequiredFeatureIds = definition.RequiredFeatureIds }, matrix,
                    out var setup, out var reasons), string.Join(";", reasons));
                Assert.IsTrue(SkirmishSetupMatrixTable.TryFind(matrix, "S005", size, out var row));
                Assert.AreEqual(SkirmishArmyProfileId.CombinedArms, setup.ArmyProfileId);
                Assert.AreEqual(SkirmishStartPackageId.FieldBase, setup.StartPackageId);
                Assert.AreEqual(1, (int)setup.Readiness);
                Assert.AreEqual(7, setup.Structures.Count(s => s.FactionId == 1));
                Assert.AreEqual(7, setup.Structures.Count(s => s.FactionId == 2));
                Assert.IsFalse(setup.Structures.Any(s => s.StructureId == SkirmishStructureIds.Helipad));
                Assert.IsFalse(setup.Forces.Any(f => f.RoleKind == SkirmishRoleKind.Tank || f.RoleKind == SkirmishRoleKind.AttackHeli));
                Assert.AreEqual(size == SkirmishSizeId.Standard ? 450 : size == SkirmishSizeId.War ? 675 : 900, setup.MaterialsEach);
                Assert.AreEqual(size == SkirmishSizeId.Standard ? 120 : size == SkirmishSizeId.War ? 180 : 240, setup.OilEach);
                Assert.AreEqual(size == SkirmishSizeId.Standard ? 350 : size == SkirmishSizeId.War ? 525 : 700, setup.UsableFuelEach);
                Assert.AreEqual(size == SkirmishSizeId.Standard ? 1080 : size == SkirmishSizeId.War ? 1500 : 1800, setup.DeadlineSeconds);
                foreach (byte faction in new byte[] { 1, 2 })
                {
                    int support = faction == 1 ? row.PlayerLogisticsSupport : row.EnemyLogisticsSupport;
                    Assert.AreEqual(1, setup.Forces.Where(f => f.FactionId == faction && f.RoleKind == SkirmishRoleKind.Tanker).Sum(f => f.Quantity));
                    Assert.AreEqual(support - 1, setup.Forces.Where(f => f.FactionId == faction && f.RoleKind == SkirmishRoleKind.LogisticsTruck).Sum(f => f.Quantity));
                    Assert.IsTrue(setup.Forces.Where(f => f.FactionId == faction &&
                        (f.RoleKind == SkirmishRoleKind.Tanker || f.RoleKind == SkirmishRoleKind.LogisticsTruck))
                        .All(f => f.SupplyCost == 0 && !string.IsNullOrEmpty(f.RuntimePrefabKey)));
                    foreach (var force in setup.Forces.Where(f => f.FactionId == faction &&
                        (SkirmishRoleIds.Category(f.RoleKind) == SkirmishPopulationCategory.Ground ||
                         SkirmishRoleIds.Category(f.RoleKind) == SkirmishPopulationCategory.LogisticsSupport)))
                    {
                        // Starting logistics must not be exposed ahead of HQ.
                        // Native footprint/path legality is a separate Play gate.
                        if (faction == 1) Assert.Less(force.SpawnWorldX, setup.PlayerBaseWorldX);
                        else Assert.Greater(force.SpawnWorldX, setup.EnemyBaseWorldX);
                    }
                }
                Assert.AreEqual("obj.s005.base.player", setup.PlayerBaseObjectId);
                Assert.AreEqual("obj.s005.base.enemy", setup.EnemyBaseObjectId);
                Assert.AreEqual(22, setup.RoleOverlays.Length);
                {
                    Assert.IsTrue(setup.MeasuredLayoutBound, "A new mission must bind its measured Campaign placement frame.");
                    float dx = setup.PlayerBaseWorldX - setup.EnemyBaseWorldX;
                    float dz = setup.PlayerBaseWorldZ - setup.EnemyBaseWorldZ;
                    Assert.Greater(dx * dx + dz * dz, 40000f, "Both bases must be separated in Campaign world coordinates.");
                    Assert.Greater(setup.PlayerBaseWorldX, 400f);
                }
            }
        }

        [Test]
        public void S005UsesExistingCampaignSourceWithoutPublishingByAssetExistence()
        {
            var catalog = SkirmishExpansionCatalogConfig.Load();
            Assert.IsTrue(catalog.TryGetDefinition("S005", out var definition));
            Assert.IsNotNull(definition.CampaignMap);
            Assert.IsTrue(definition.CampaignMap.TryValidateMetadata(out var error), error);
            Assert.IsTrue(definition.CampaignMap.TryValidateLocalContentReferences(out error), error);
            Assert.AreEqual(definition.CampaignMap.ContentHash, definition.MapLayout.WorldBinding.SourceHash);
            Assert.IsTrue(catalog.ToAuthoredSet().Publication.TryGet("S005", out var publication));
            Assert.AreEqual(SkirmishPublicationStatus.InProgress, publication.Status);
            Assert.IsTrue(SkirmishBattleCatalogConfig.Load().TryGet("S005", out var entry));
            Assert.IsFalse(entry.IsPlayable);
            var quick = QuickGameConfig.Defaults;
            quick.ScenarioIndex = SkirmishPresetConfig.DesertBaseCombinedArmsFieldScenarioIndex;
            Assert.AreEqual(7, quick.NormalizeForBaseAssault().ScenarioIndex);
        }

        [Test]
        public void S005PublicationRequiresCompleteBaselineAndCombinedArmsEvidence()
        {
            var catalog = SkirmishExpansionCatalogConfig.Load();
            Assert.IsTrue(catalog.TryGetDefinition("S005", out var definition));
            Assert.IsTrue(catalog.ToAuthoredSet().Publication.TryGet("S005", out var row));
            Assert.IsTrue(SkirmishSetupMatrixTable.TryLoadPackaged(out var matrix, out var error), error);
            Assert.IsTrue(SkirmishSetupCompiler.TryCompile(definition, SkirmishDifficultyId.Regular,
                SkirmishSizeId.Standard, 104734,
                new SkirmishContentManifest { RequiredFeatureIds = definition.RequiredFeatureIds }, matrix,
                out var setup, out var compileReasons), string.Join(";", compileReasons));
            var evidence = new SkirmishPublicationEvidence {
                CatalogId = setup.CatalogId, DefinitionId = setup.DefinitionId,
                ContentHash = setup.ContentHash, SetupHash = setup.SetupHash,
                DifficultyId = setup.DifficultyId, SizeId = setup.SizeId,
                ManualWin = true, AriaMatrix = true, EdgeFixtures = true,
                RecoveryEvidence = true, DeviceEvidence = true, AssetExists = true
            };
            // A completed baseline cannot stand in for either Combined Arms gate.
            foreach (var pair in new[] { (false, false), (true, false), (false, true) })
            {
                evidence.RoleLifecycleEvidence = pair.Item1;
                evidence.AircraftCapacityEvidence = pair.Item2;
                Assert.IsTrue(SkirmishPublicationValidator.TryEvaluate(row, definition, setup,
                    in evidence, out var status, out var reasons));
                Assert.AreEqual(SkirmishPublicationStatus.InProgress, status);
                Assert.IsTrue(reasons.Any(r => r.Field == "combinedArms"));
            }
            evidence.RoleLifecycleEvidence = evidence.AircraftCapacityEvidence = true;
            evidence.DeviceEvidence = false;
            Assert.IsTrue(SkirmishPublicationValidator.TryEvaluate(row, definition, setup,
                in evidence, out var missingDevice, out _));
            Assert.AreEqual(SkirmishPublicationStatus.InProgress, missingDevice);
            evidence.DeviceEvidence = true;
            Assert.IsTrue(SkirmishPublicationValidator.TryEvaluate(row, definition, setup,
                in evidence, out var accepted, out var acceptedReasons));
            Assert.AreEqual(SkirmishPublicationStatus.Playable, accepted);
            Assert.IsEmpty(acceptedReasons);
            Assert.IsNotNull(setup.EnemyPolicy);
            Assert.AreEqual(definition.ContentHash, setup.EnemyPolicy.SourceContentHash);
            Assert.AreEqual(definition.ContentHash + "/enemy-policy-v1/" + setup.EnemyPolicy.Fingerprint, setup.ContentHash);
            string compiledContentHash = setup.ContentHash;
            setup.ContentHash = definition.ContentHash;
            evidence.ContentHash = setup.ContentHash;
            Assert.IsFalse(SkirmishPublicationValidator.TryEvaluate(row, definition, setup,
                in evidence, out _, out var policylessReasons));
            Assert.IsTrue(policylessReasons.Any(r => r.Field == "contentHash"),
                "A policy-bearing setup cannot publish under the source-only content identity.");
            setup.ContentHash = compiledContentHash;
            evidence.ContentHash = compiledContentHash;
            // This exercises evaluation only; it must never publish the packaged row.
            Assert.AreEqual(SkirmishPublicationStatus.InProgress, row.Status);
            evidence.SetupHash ^= 1;
            Assert.IsFalse(SkirmishPublicationValidator.TryEvaluate(row, definition, setup,
                in evidence, out var mismatched, out _));
            Assert.AreEqual(SkirmishPublicationStatus.InProgress, mismatched);
        }

        [Test]
        public void NativeValidationRejectsAnIncompleteRenderDatabase()
        {
            using var world = new World("S005RenderingReadiness");
            var em = world.EntityManager;
            Assert.IsTrue(Game.Editor.SkirmishS002AriaRunHarness.NativeRenderingIsReady(em));
            var database = em.CreateEntity(typeof(OperationMapRenderDatabaseComponent));
            Assert.IsFalse(Game.Editor.SkirmishS002AriaRunHarness.NativeRenderingIsReady(em));
            em.AddComponent<OperationMapRenderPackedReadinessComponent>(database);
            em.AddComponent<OperationMapRenderVirtualizationStateComponent>(database);
            Assert.IsFalse(Game.Editor.SkirmishS002AriaRunHarness.NativeRenderingIsReady(em));
            em.SetComponentData(database, new OperationMapRenderVirtualizationStateComponent { Initialized = 1 });
            Assert.IsFalse(Game.Editor.SkirmishS002AriaRunHarness.NativeRenderingIsReady(em), "Initialized allocation is not a rendered view.");
            em.AddComponentData(database, new OperationMapRenderVirtualizationMetricsComponent { Capacity = 4, EnabledSlotCount = 2 });
            var rendered = new OperationMapRenderVirtualizationStateComponent
                { Initialized = 1, InitialViewApplied = 1, RebuildCount = 1, ActiveSlotCount = 2 };
            em.SetComponentData(database, rendered);
            Assert.IsTrue(Game.Editor.SkirmishS002AriaRunHarness.NativeRenderingIsReady(em));
            rendered.OverflowCount = 1; em.SetComponentData(database, rendered);
            Assert.IsFalse(Game.Editor.SkirmishS002AriaRunHarness.NativeRenderingIsReady(em), "Overflow must fail native validation.");
            rendered.OverflowCount = 0; rendered.ActiveSlotCount = 0; em.SetComponentData(database, rendered);
            em.SetComponentData(database, new OperationMapRenderVirtualizationMetricsComponent { Capacity = 4 });
            Assert.IsFalse(Game.Editor.SkirmishS002AriaRunHarness.NativeRenderingIsReady(em), "An empty packed view must fail native validation.");
        }

        [Test]
        public void NativeStartupRenderGateAcceptsFirstCompleteViewThenIgnoresCameraEmptyView()
        {
            using var world = new World("S005RenderingStartupLatch");
            var em = world.EntityManager;
            var database = em.CreateEntity(typeof(OperationMapRenderDatabaseComponent),
                typeof(OperationMapRenderPackedReadinessComponent),
                typeof(OperationMapRenderVirtualizationStateComponent),
                typeof(OperationMapRenderVirtualizationMetricsComponent));
            bool openingViewAccepted = false;
            Assert.IsFalse(Game.Editor.SkirmishS002AriaRunHarness.NativeRenderingStartupGateAllows(em, ref openingViewAccepted));
            Assert.IsFalse(openingViewAccepted, "Incomplete opening view cannot set the latch.");

            em.SetComponentData(database, new OperationMapRenderVirtualizationStateComponent
                { Initialized = 1, InitialViewApplied = 1, RebuildCount = 1, ActiveSlotCount = 2 });
            em.SetComponentData(database, new OperationMapRenderVirtualizationMetricsComponent
                { Capacity = 4, EnabledSlotCount = 2 });
            Assert.IsTrue(Game.Editor.SkirmishS002AriaRunHarness.NativeRenderingStartupGateAllows(em, ref openingViewAccepted));
            Assert.IsTrue(openingViewAccepted);

            // A later camera-dependent view can be empty without undoing opening initialization.
            em.SetComponentData(database, new OperationMapRenderVirtualizationStateComponent
                { Initialized = 1, InitialViewApplied = 1, RebuildCount = 2, ActiveSlotCount = 0 });
            em.SetComponentData(database, new OperationMapRenderVirtualizationMetricsComponent { Capacity = 4 });
            Assert.IsFalse(Game.Editor.SkirmishS002AriaRunHarness.NativeRenderingIsReady(em));
            Assert.IsTrue(Game.Editor.SkirmishS002AriaRunHarness.NativeRenderingStartupGateAllows(em, ref openingViewAccepted));
            bool freshRun = false;
            Assert.IsFalse(Game.Editor.SkirmishS002AriaRunHarness.NativeRenderingStartupGateAllows(em, ref freshRun),
                "A new run must reject the same incomplete view.");
            Assert.IsFalse(freshRun);
        }

        [Test]
        public void InvalidatedCandidateCannotCountItsFinishedVictory()
        {
            var facts = new SkirmishS002AriaTerminalFacts
            {
                MatchFinished = true, MatchOutcome = "Victory", Aborted = true,
                EndReason = "candidateSourceChangedDuringRun", NormalSpeed = true,
                InputViolations = 0, HumanInterventions = 0
            };
            Assert.IsTrue(SkirmishS002AriaRunLog.TryAcceptTerminal(in facts, out var result, out _));
            Assert.AreEqual(SkirmishS002AriaRunLog.ResultAbort, result);
            Assert.IsFalse(SkirmishS002AriaRunLog.IsCountedAriaWin(in facts, "Victory"));
        }

        [Test]
        public void FreshBakePackedRenderPartsRetainExactSubmeshOwnership()
        {
            using var world = new World("S005PackedCampaignScenery");
            var em = world.EntityManager;
            var mesh = new Mesh { subMeshCount = 2 };
            var material = new Material(Shader.Find("Hidden/InternalErrorShader"));
            try
            {
                var entity = em.CreateEntity(typeof(MaterialMeshInfo));
                em.AddSharedComponentManaged(entity, new RenderMeshArray(new[] { material }, new[] { mesh },
                    new[] { new MaterialMeshIndex { MaterialIndex = 0, MeshIndex = 0, SubMeshIndex = 0 },
                        new MaterialMeshIndex { MaterialIndex = 0, MeshIndex = 0, SubMeshIndex = 1 } }));
                em.SetComponentData(entity, MaterialMeshInfo.FromMaterialMeshIndexRange(0, 2));
                var rows = new[] {
                    new OperationMapRenderEligibleSourceRowBakingComponent { Mesh = mesh, Material = material, SubMeshIndex = 0 },
                    new OperationMapRenderEligibleSourceRowBakingComponent { Mesh = mesh, Material = material, SubMeshIndex = 1 }
                };
                Assert.IsFalse(em.HasComponent<RenderMeshUnmanaged>(entity));
                var parts = OperationMapRenderVirtualizationBakingSystem.RequireExactRenderParts(em, entity, rows);
                Assert.AreEqual(2, parts.Count);
                Assert.AreNotEqual(parts[0], parts[1]);
                Assert.Throws<InvalidOperationException>(() =>
                    OperationMapRenderVirtualizationBakingSystem.RequireExactRenderParts(em, entity, new[] { rows[0] }));
                em.SetComponentData(entity, MaterialMeshInfo.FromRenderMeshArrayIndices(0, 0, 1));
                Assert.AreEqual(parts[1], OperationMapRenderVirtualizationBakingSystem.RequireExactRenderParts(em, entity, rows)[0]);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(material);
                UnityEngine.Object.DestroyImmediate(mesh);
            }
        }

        [Test]
        public void SiegeNativeWeaponUsesMissionDamageAndRange()
        {
            using var world = new World("S005SiegeWeaponBinding");
            var em = world.EntityManager;
            var unit = em.CreateEntity(typeof(GroundMissileLauncherComponent), typeof(UnitAttack));
            em.SetComponentData(unit, new GroundMissileLauncherComponent
            { Damage = 650, MinRange = 120f, MaxRange = 600f, PrepareSeconds = 3f, ReloadSeconds = 8f });
            Assert.IsTrue(SkirmishRoleOverlayCatalog.TryGet(SkirmishRoleOverlayCatalog.CreateCombinedArmsSlice(),
                SkirmishRoleKind.Siege, out var overlay));
            SkirmishSharedCombatBinding.Bind(em, unit, overlay);
            var weapon = em.GetComponentData<GroundMissileLauncherComponent>(unit);
            Assert.AreEqual(overlay.Damage, weapon.Damage);
            Assert.AreEqual(overlay.RangeWorld, weapon.MaxRange);
            Assert.Less(weapon.MinRange, weapon.MaxRange);
            Assert.AreEqual(3f, weapon.PrepareSeconds);
            Assert.AreEqual(8f, weapon.ReloadSeconds);
            Assert.AreEqual(overlay.Damage, em.GetComponentData<UnitAttack>(unit).Damage);
        }

        [Test]
        public void EditorCandidateCatalogIsScopedAndDoesNotUnlockPackagedEntry()
        {
            var packaged = SkirmishBattleCatalogConfig.Load();
            var candidate = ScriptableObject.CreateInstance<SkirmishBattleCatalogConfig>();
            try
            {
                using (SkirmishBattleCatalogConfig.UseEditorValidationCatalog(candidate))
                {
                    Assert.AreSame(candidate, SkirmishBattleCatalogConfig.Load());
                    Assert.Throws<InvalidOperationException>(() =>
                        SkirmishBattleCatalogConfig.UseEditorValidationCatalog(candidate));
                }
                Assert.AreSame(packaged, SkirmishBattleCatalogConfig.Load());
                Assert.IsTrue(packaged.TryGet("S005", out var entry));
                Assert.IsFalse(entry.IsPlayable);
            }
            finally { UnityEngine.Object.DestroyImmediate(candidate); }
        }

        [Test]
        public void MeasuredBindingFollowsTheLogicalLayoutRatherThanAnIdAllowlist()
        {
            var setup = new SkirmishResolvedSetup
            {
                CatalogId = "S006", OperationMapId = SkirmishMapLayoutBuilder.DesertBaseMapId,
                LayoutId = SkirmishMapLayoutBuilder.DesertBaseLayoutId,
                DifficultyId = SkirmishDifficultyId.Regular, SizeId = SkirmishSizeId.Standard
            };
            Assert.IsTrue(SkirmishMapLayoutBuilder.ShouldBindRegularStandard(setup));
            setup.LayoutId = "layout.skirmish.cc.ba";
            Assert.IsFalse(SkirmishMapLayoutBuilder.ShouldBindRegularStandard(setup));
            setup.LayoutId = SkirmishMapLayoutBuilder.DesertBaseLayoutId;
            setup.CatalogId = "custom";
            Assert.IsFalse(SkirmishMapLayoutBuilder.ShouldBindRegularStandard(setup));
        }

        [Test]
        public void InitialPlacementIgnoresUnrelatedEntityAllocationOrder()
        {
            foreach (int seed in new[] { 104734, 130368, 155926 })
            {
                var baseline = PlaceInitialVehicle(seed, 0);
                foreach (int noise in new[] { 32, 128 })
                    Assert.AreEqual(baseline, PlaceInitialVehicle(seed, noise),
                        "Unrelated entity allocations changed starting placement for seed " + seed);
            }
        }

        [Test]
        public void InitialPlacementRejectsCoarseGridCellsWithoutTraversableSurface()
        {
            foreach (bool vehicle in new[] { false, true })
                PlaceInitialVehicle(130368, 0, true, false, vehicle);
            PlaceInitialVehicle(130368, 0, true, true);
        }

        private static Unity.Mathematics.int2 PlaceInitialVehicle(int seed, int noise,
            bool validateSurface = false, bool allSurfaceBlocked = false, bool vehicle = true)
        {
            using var world = new World("initial-placement-allocation-" + noise);
            var em = world.EntityManager;
            const int width = 64;
            using var blocked = new Unity.Collections.NativeBitArray(width * width, Unity.Collections.Allocator.Temp);
            using var occupied = new Unity.Collections.NativeBitArray(width * width, Unity.Collections.Allocator.Temp);
            if (!validateSurface) blocked.Set(32 * width + 32, true); // Force the real fallback search.
            var grid = em.CreateEntity(typeof(GridConfig));
            em.SetComponentData(grid, new GridConfig { Width = width, Height = width, CellSize = 1 });
            em.AddComponentData(grid, new DynamicBlockerComponent { Blocked = blocked });
            em.AddComponentData(grid, new DynamicOccupancyComponent { Occupied = occupied });
            var walkable = em.AddBuffer<GridWalkable>(grid);
            walkable.ResizeUninitialized(width * width);
            for (int i = 0; i < walkable.Length; i++) walkable[i] = new GridWalkable { Value = 1 };
            using var surfaceBlob = CreatePlacementSurface(width, allSurfaceBlocked);
            if (validateSurface)
            {
                em.CreateEntity(typeof(SkirmishMatchState));
                var surfaceEntity = em.CreateEntity(typeof(MapSurfaceComponent));
                em.SetComponentData(surfaceEntity, new MapSurfaceComponent
                {
                    SurfaceBlob = surfaceBlob, Dimensions = new Unity.Mathematics.int2(width),
                    CellSize = 1, HasSurfaceData = 1
                });
            }
            for (int i = 0; i < noise; i++) em.CreateEntity();
            var actor = em.CreateEntity(typeof(UnitFootprint), typeof(SkirmishAttemptOwnedComponent));
            em.SetComponentData(actor, new UnitFootprint { Size = new Unity.Mathematics.int2(vehicle ? 3 : 1) });
            em.AddComponentData(actor, new UnitMovementBehavior { UsesVehicleMotion = (byte)(vehicle ? 1 : 0) });
            em.SetComponentData(actor, new SkirmishAttemptOwnedComponent
            {
                FactionId = 1, SessionId = new Unity.Collections.FixedString64Bytes("placement-fixture"),
                StableObjectId = new Unity.Collections.FixedString64Bytes("role.tanker.1.8.0")
            });
            var method = typeof(SkirmishScenarioSpawnSystem).GetMethod("TryPlaceOnMap",
                System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
            Assert.IsNotNull(method);
            // Accept the pre-fix signature to reproduce allocation dependence,
            // then exercise the same native placement path with the mission seed.
            object[] args = method.GetParameters().Length == 3
                ? new object[] { em, actor, new Vector3(32.5f, 0, 32.5f) }
                : new object[] { em, actor, new Vector3(32.5f, 0, 32.5f), seed };
            bool placed = (bool)method.Invoke(null, args);
            if (allSurfaceBlocked)
            {
                Assert.IsFalse(placed, "A clear compatibility grid cannot authorize an impassable native surface.");
                Assert.IsFalse(em.HasComponent<UnitGrid>(actor));
                return default;
            }
            Assert.IsTrue(placed);
            var cell = em.GetComponentData<UnitGrid>(actor).Cell;
            Assert.AreNotEqual(new Unity.Mathematics.int2(32, 32), cell);
            if (validateSurface)
            {
                using var surfaces = em.CreateEntityQuery(typeof(MapSurfaceComponent));
                Assert.IsTrue(new Game.Runtime.Pathfinding.MapSurfaceTraversalValidation().CanTraverseFootprint(
                    surfaces.GetSingleton<MapSurfaceComponent>(),
                    1, em.GetComponentData<GridConfig>(grid), cell, em.GetComponentData<UnitFootprint>(actor).Size, vehicle));
            }
            return cell;
        }

        private static BlobAssetReference<MapSurfaceBlob> CreatePlacementSurface(int width, bool allBlocked)
        {
            using var builder = new BlobBuilder(Unity.Collections.Allocator.Temp);
            ref var root = ref builder.ConstructRoot<MapSurfaceBlob>();
            root.CellSize = 1;
            root.Dimensions = new Unity.Mathematics.int2(width);
            root.RuntimeEncoding = MapSurfaceRuntimeEncoding.Full;
            var cells = builder.Allocate(ref root.Cells, width * width);
            var samples = builder.Allocate(ref root.Samples, width * width);
            builder.Allocate(ref root.Connections, 0);
            builder.Allocate(ref root.CompactSamples, 0);
            for (int i = 0; i < cells.Length; i++)
            {
                cells[i] = new MapSurfaceCell { FirstSurfaceIndex = i, SurfaceCount = 1 };
                bool blocked = allBlocked || i == 32 * width + 32;
                samples[i] = new MapSurfaceSample
                {
                    Cell = new Unity.Mathematics.int2(i % width, i / width),
                    SurfaceId = i + 1, SurfaceType = blocked ? MapSurfaceType.Blocked : MapSurfaceType.Terrain,
                    MovementMask = blocked ? MapSurfaceMovementMask.None : MapSurfaceMovementMask.AllGroundUnits,
                    Normal = Unity.Mathematics.math.up()
                };
            }
            return builder.CreateBlobAssetReference<MapSurfaceBlob>(Unity.Collections.Allocator.Persistent);
        }

        public static void RunInitialPlacementValidation()
        {
            var suite = new SkirmishCombinedArmsTests();
            suite.InitialPlacementIgnoresUnrelatedEntityAllocationOrder();
            suite.InitialPlacementRejectsCoarseGridCellsWithoutTraversableSurface();
            SkirmishNativeCheckpointActorSpawnTests.RunFocusedValidation();
            Debug.Log("[SkirmishInitialPlacementTests] result=Passed seeds=3 allocationLayouts=3 nativeSurfaceCases=3 checkpointSpawnTests=2 nativeMissionAcceptance=Pending");
        }

        public static void RebuildAndRunFocusedValidation()
        {
            string scenarioPath = Game.Editor.SkirmishS005DefinitionBuilder.Folder + "/SkirmishScenario_S005.asset";
            string before = System.IO.File.ReadAllText(scenarioPath);
            Game.Editor.SkirmishS005DefinitionBuilder.Build();
            Assert.That(System.IO.File.ReadAllText(scenarioPath), Is.EqualTo(before),
                "Rebuilding S005 must preserve its authored scenario defaults and gameplay budgets.");
            Debug.Log("[S005RebuildPreservation] result=Passed scenarioBytesUnchanged=1");
            RunFocusedValidation();
        }

        public static void RunFocusedValidation()
        {
            var suite = new SkirmishCombinedArmsTests();
            suite.CombinedRosterPreservesArmorAndAirWithPaidReadiness();
            suite.NativeValidationRejectsAnIncompleteRenderDatabase();
            suite.NativeStartupRenderGateAcceptsFirstCompleteViewThenIgnoresCameraEmptyView();
            suite.InvalidatedCandidateCannotCountItsFinishedVictory();
            suite.FreshBakePackedRenderPartsRetainExactSubmeshOwnership();
            suite.SiegeNativeWeaponUsesMissionDamageAndRange();
            suite.EditorCandidateCatalogIsScopedAndDoesNotUnlockPackagedEntry();
            suite.MeasuredBindingFollowsTheLogicalLayoutRatherThanAnIdAllowlist();
            suite.S005AllSizesAndDifficultiesPreserveMatrixAndFieldStart();
            suite.S005UsesExistingCampaignSourceWithoutPublishingByAssetExistence();
            suite.S005PublicationRequiresCompleteBaselineAndCombinedArmsEvidence();
            SkirmishExpandedDefinitionTests.RunFocusedValidation();
            Debug.Log("[SkirmishCombinedArmsTests] result=Passed matrixConfigurations=12 existingMissionRegressions=Passed publication=InProgress");
        }

        public static void RunNativeRenderingStartupGateFocusedValidation()
        {
            var suite = new SkirmishCombinedArmsTests();
            suite.NativeValidationRejectsAnIncompleteRenderDatabase();
            suite.NativeStartupRenderGateAcceptsFirstCompleteViewThenIgnoresCameraEmptyView();
            Debug.Log("[SkirmishNativeRenderingStartupGateTests] result=Passed cases=2");
        }
    }
}
