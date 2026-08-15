using SnivelerCode.GpuAnimation.Scripts.Components;
using NUnit.Framework;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Graphics;
using Unity.Mathematics;
using Unity.Rendering;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.Rendering;
using Game.Rendering.Contracts;
using Game.Components;
using Game.Rendering;

public sealed partial class UnitRenderBudgetSystemTests
{
    private const int DenseOperationMapRenderChildCount = 70710;
    private const int Vrp054SteadyStateSampleCount = 300;

    public static void RunFocusedValidation()
    {
        try
        {
            var tests = new UnitRenderBudgetSystemTests();
            tests.BudgetBandsRespectDetailedMidAndLowCaps();
            tests.DistanceSortOrdersByPriorityThenDistance();
            tests.DistanceCollectScoresVisibleUnitsAndSkipsPassengers();
            tests.DistanceCollectWithNullCameraClearsOutputWithoutThrowing();
            tests.CharacterClassificationUsesCachedLookups();
            tests.LodReferenceResolutionUsesCachedLookups();
            tests.RenderableQueryUsesCachedLookups();
            tests.MovingVisibleCharactersUseAnimatableMeshLodPath();
            tests.MovingVisibleCharactersFallbackToDetailWhenMeshLodIsNotAnimatable();
            tests.IdleDistantVisibleCharactersUseFarImpostorPath();
            tests.NearVisibleCharactersUseDetailedModelPath();
            tests.CharacterRenderPolicyDoesNotGloballyForceDetailedModelPath();
            tests.ImpostorTagRequestUsesCachedLookup();
            tests.UnselectedEnemyBeyondImpostorThresholdUsesFarImpostorVisual();
            tests.AirVehiclesStayDetailedBeyondLodAndImpostorThresholds();
            tests.AirUnitDecisionForcesDetailedRootsWhenBudgetWouldUseLow();
            tests.SelectedVehicleForcesImmediateDetailedVisual();
            tests.StableBudgetDoesNotSkipSelectedUnitsAndSelectionChanges();
            tests.CameraMotionBudgetRespectsThreeFrameThrottle();
            tests.SelectedVehicleReappliesDetailRootsWhenVisualStateAlreadyDetail();
            tests.MissingMeshLodInstanceKeepsDetailVisibleUntilReady();
            tests.VisualStateTransitionKeepsCurrentVisualUntilStableOrForced();
            tests.VisibilityChangeCollectionUsesCachedLookups();
            tests.VisibilityApplyAddsAndRemovesRenderTags();
            tests.VisibilityApplyUsesCachedLookups();
            tests.ReadinessSystemAddsReadyTagForRenderableVisual();
            tests.ReadinessSystemUsesCachedLookups();
            tests.RenderSafetyPatchesBoundsAndAddsSafetyTag();
            tests.RenderSafetyUsesCachedLookups();
            tests.MassRenderSettingsPatchesUnitRenderChildren();
            tests.MassRenderSettingsPatchesRenderFiltersAfterLookupWork();
            tests.MassRenderSettingsRetriesUnitUntilVisualReferenceExists();
            tests.MassRenderSettingsClassifiesOperationMapParentWithoutRetry();
            tests.MassRenderSettingsClassifiesOperationMapAncestorAboveIncompleteUnitParent();
            tests.MassRenderSettingsClassifiesPackedResidentRowWithoutHierarchyIdentity();
            tests.DiagnosticLogFlushClearsQueuedMessages();
            tests.CharacterImpostorsScaleUpAtHighTacticalCameraHeight();
            tests.HighCameraCharacterImpostorsFaceCameraPlane();
            tests.SourceKeyPrefixChecksDoNotAllocate();
            Debug.Log("[UnitRenderBudgetFocusedValidation] result=Passed tests=38");
        }
        catch (System.Exception ex)
        {
            Debug.LogException(ex);
            Debug.LogError("[UnitRenderBudgetFocusedValidation] result=Failed");
            throw;
        }
    }

    public static void RunSelectedUnitImpostorValidation()
    {
        try
        {
            new UnitRenderBudgetSystemTests()
                .SelectedVisibleCharacterNeverUsesFarImpostorAtZoomedOutDistance();
            Debug.Log("[SelectedUnitImpostorValidation] result=Passed tests=1");
        }
        catch (System.Exception exception)
        {
            Debug.LogException(exception);
            Debug.LogError("[SelectedUnitImpostorValidation] result=Failed");
            throw;
        }
    }

    public static void RunVrp054SteadyStateValidation()
    {
        try
        {
            var tests = new UnitRenderBudgetSystemTests();
            double p95Milliseconds =
                tests.DenseOperationMapClassification_DrainsThenMeets120HzCpuBudget();
            Debug.Log(
                "[VRP054WindowsSteadyState] result=Passed " +
                $"renderChildren={DenseOperationMapRenderChildCount} " +
                $"samples={Vrp054SteadyStateSampleCount} " +
                $"p95Ms={p95Milliseconds:F4} budgetMs=8.3333");
            ValidationExit.Exit(0);
        }
        catch (System.Exception exception)
        {
            Debug.LogException(exception);
            Debug.LogError("[VRP054WindowsSteadyState] result=Failed");
            ValidationExit.Exit(1);
        }
    }

    [Test]
    public void BudgetBandsRespectDetailedMidAndLowCaps()
    {
        var distances = new NativeList<UnitRenderBudgetDistance.UnitDistance>(Allocator.TempJob);
        try
        {
            for (int i = 0; i < 7; i++)
            {
                distances.Add(new UnitRenderBudgetDistance.UnitDistance
                {
                    Unit = TestEntity(i + 1),
                    DistanceSq = i < 2 ? 9f : 100f,
                    Priority = (byte)i,
                    Visible = 1
                });
            }

            UnitRenderBudgetBand.Plan plan = new UnitRenderBudgetBand().Create(
                distances,
                maxDetailedUnits: 2,
                maxMidLodUnits: 2,
                maxLowLodUnits: 2,
                alwaysDetailedDistanceSq: 25f,
                Allocator.TempJob);
            try
            {
                Assert.AreEqual(2, plan.DetailedCount);
                Assert.AreEqual(2, plan.MidCount);
                Assert.AreEqual(2, plan.LowCount);
                Assert.IsTrue(plan.DetailedUnits.Contains(TestEntity(1)));
                Assert.IsTrue(plan.DetailedUnits.Contains(TestEntity(2)));
                Assert.IsTrue(plan.MidLodUnits.Contains(TestEntity(3)));
                Assert.IsTrue(plan.MidLodUnits.Contains(TestEntity(4)));
                Assert.IsTrue(plan.LowLodUnits.Contains(TestEntity(5)));
                Assert.IsTrue(plan.LowLodUnits.Contains(TestEntity(6)));
                Assert.IsFalse(plan.DetailedUnits.Contains(TestEntity(7)));
                Assert.IsFalse(plan.MidLodUnits.Contains(TestEntity(7)));
                Assert.IsFalse(plan.LowLodUnits.Contains(TestEntity(7)));
            }
            finally
            {
                plan.Dispose();
            }
        }
        finally
        {
            distances.Dispose();
        }
    }

    [Test]
    public void DistanceSortOrdersByPriorityThenDistance()
    {
        var distances = new NativeList<UnitRenderBudgetDistance.UnitDistance>(Allocator.TempJob);
        try
        {
            distances.Add(new UnitRenderBudgetDistance.UnitDistance { Unit = TestEntity(1), Priority = 2, DistanceSq = 1f });
            distances.Add(new UnitRenderBudgetDistance.UnitDistance { Unit = TestEntity(2), Priority = 0, DistanceSq = 9f });
            distances.Add(new UnitRenderBudgetDistance.UnitDistance { Unit = TestEntity(3), Priority = 0, DistanceSq = 4f });
            distances.Add(new UnitRenderBudgetDistance.UnitDistance { Unit = TestEntity(4), Priority = 1, DistanceSq = 2f });

            new UnitRenderBudgetSort().Sort(distances);

            Assert.AreEqual(TestEntity(3), distances[0].Unit);
            Assert.AreEqual(TestEntity(2), distances[1].Unit);
            Assert.AreEqual(TestEntity(4), distances[2].Unit);
            Assert.AreEqual(TestEntity(1), distances[3].Unit);
        }
        finally
        {
            distances.Dispose();
        }
    }

    [Test]
    public void DistanceCollectScoresVisibleUnitsAndSkipsPassengers()
    {
        using var world = new World(nameof(DistanceCollectScoresVisibleUnitsAndSkipsPassengers));
        EntityManager em = world.EntityManager;
        GameObject cameraObject = new("UnitRenderBudgetDistanceTestCamera");
        try
        {
            Camera camera = cameraObject.AddComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = 8f;
            camera.nearClipPlane = 0.1f;
            camera.farClipPlane = 100f;
            cameraObject.transform.position = new Vector3(0f, 10f, -10f);
            cameraObject.transform.rotation = Quaternion.LookRotation(Vector3.zero - cameraObject.transform.position, Vector3.up);

            Entity visible = em.CreateEntity(typeof(UnitHealth), typeof(LocalTransform));
            em.SetComponentData(visible, new UnitHealth { Current = 100, Max = 100 });
            em.SetComponentData(visible, LocalTransform.FromPosition(float3.zero));

            Entity passenger = em.CreateEntity(typeof(UnitHealth), typeof(LocalTransform), typeof(UnitTransportPassenger));
            em.SetComponentData(passenger, new UnitHealth { Current = 100, Max = 100 });
            em.SetComponentData(passenger, LocalTransform.FromPosition(new float3(1f, 0f, 0f)));
            em.SetComponentData(passenger, new UnitTransportPassenger { Transport = Entity.Null });

            using var distances = new NativeList<UnitRenderBudgetDistance.UnitDistance>(2, Allocator.TempJob);
            UnitRenderBudgetDistanceTestSystem system = world.GetOrCreateSystemManaged<UnitRenderBudgetDistanceTestSystem>();
            system.Camera = CreateCameraSnapshot(camera);
            system.Distances = distances;
            system.Update();

            Assert.AreEqual(1, distances.Length);
            UnitRenderBudgetDistance.UnitDistance distance = distances[0];
            Assert.AreEqual(visible, distance.Unit);
            Assert.AreEqual(1, distance.Visible);
            Assert.AreEqual(0, distance.ScreenEdge);
            Assert.AreEqual(0, distance.Priority);
            Assert.AreEqual(math.distancesq(float3.zero, (float3)cameraObject.transform.position), distance.DistanceSq, 0.001f);
        }
        finally
        {
            Object.DestroyImmediate(cameraObject);
        }
    }

    [Test]
    public void DistanceCollectWithNullCameraClearsOutputWithoutThrowing()
    {
        using var world = new World(nameof(DistanceCollectWithNullCameraClearsOutputWithoutThrowing));
        UnitRenderBudgetTestLookupSystem lookupSystem = world.GetOrCreateSystemManaged<UnitRenderBudgetTestLookupSystem>();
        using var units = new NativeArray<Entity>(0, Allocator.TempJob);
        using var transforms = new NativeArray<LocalTransform>(0, Allocator.TempJob);
        using var distances = new NativeList<UnitRenderBudgetDistance.UnitDistance>(1, Allocator.TempJob);
        distances.Add(new UnitRenderBudgetDistance.UnitDistance { Unit = TestEntity(10), DistanceSq = 25f });

        Assert.DoesNotThrow(() => new UnitRenderBudgetDistance().Collect(
            default,
            units,
            transforms,
            distances,
            lookupSystem.GetPassengerLookup(),
            lookupSystem.GetEntityStorageInfoLookupForTests(),
            alwaysDetailedDistanceSq: 18f * 18f,
            viewportPadding: 0.35f,
            edgeSafetyMargin: 0.18f));

        Assert.AreEqual(0, distances.Length);
    }

    [Test]
    public void MassRenderSettingsPatchesUnitRenderChildren()
    {
        using var world = new World(nameof(MassRenderSettingsPatchesUnitRenderChildren));
        EntityManager em = world.EntityManager;
        Entity unit = em.CreateEntity(
            typeof(UnitGrid),
            typeof(Faction),
            typeof(UnitModelInstanceReference));
        em.SetComponentData(unit, new UnitGrid { Cell = int2.zero });
        em.SetComponentData(unit, new Faction { Id = 1 });
        Entity lodGroup = em.CreateEntity(typeof(MeshLODGroupComponent));
        em.SetComponentData(lodGroup, new MeshLODGroupComponent
        {
            ParentMask = 1,
            LODDistances0 = new float4(1f),
            LODDistances1 = new float4(2f)
        });
        Entity renderChild = em.CreateEntity(typeof(Parent), typeof(RenderBounds), typeof(MeshLODComponent));
        em.SetComponentData(renderChild, new Parent { Value = unit });
        em.SetComponentData(renderChild, new RenderBounds
        {
            Value = new AABB { Center = float3.zero, Extents = new float3(1f, 2f, 3f) }
        });
        em.SetComponentData(renderChild, new MeshLODComponent
        {
            Group = lodGroup,
            ParentGroup = Entity.Null,
            LODMask = 1
        });
        SystemHandle system = world.CreateSystem<UnitMassRenderSettingsSystem>();

        system.Update(world.Unmanaged);

        Assert.IsTrue(em.HasComponent<UnitMassRenderSettingsApplied>(renderChild));
        Assert.IsTrue(em.HasComponent<FactionTintTarget>(renderChild));
        Assert.IsTrue(em.HasComponent<FactionTintColor>(renderChild));
        Assert.IsTrue(em.HasComponent<FactionSnivelerBaseColor>(renderChild));
        RenderBounds bounds = em.GetComponentData<RenderBounds>(renderChild);
        Assert.AreEqual(new float3(64f, 64f, 64f), bounds.Value.Extents);
        MeshLODComponent meshLod = em.GetComponentData<MeshLODComponent>(renderChild);
        Assert.AreEqual(0xFF, meshLod.LODMask);
        MeshLODGroupComponent patchedGroup = em.GetComponentData<MeshLODGroupComponent>(lodGroup);
        Assert.AreEqual(0xFF, patchedGroup.ParentMask);
        Assert.AreEqual(new float4(1048576f), patchedGroup.LODDistances0);
        Assert.AreEqual(new float4(1048576f), patchedGroup.LODDistances1);
    }

    [Test]
    public void MassRenderSettingsPatchesRenderFiltersAfterLookupWork()
    {
        using var world = new World(nameof(MassRenderSettingsPatchesRenderFiltersAfterLookupWork));
        EntityManager em = world.EntityManager;
        Entity unit = em.CreateEntity(
            typeof(UnitGrid),
            typeof(Faction),
            typeof(UnitModelInstanceReference));
        em.SetComponentData(unit, new UnitGrid { Cell = int2.zero });
        em.SetComponentData(unit, new Faction { Id = 1 });
        Entity firstGroup = CreateLodGroup(em);
        Entity secondGroup = CreateLodGroup(em);
        Entity firstRenderChild = CreateRenderChildWithFilter(em, unit, firstGroup);
        Entity secondRenderChild = CreateRenderChildWithFilter(em, unit, secondGroup);
        SystemHandle system = world.CreateSystem<UnitMassRenderSettingsSystem>();

        Assert.DoesNotThrow(() => system.Update(world.Unmanaged));

        AssertPatchedRenderChild(em, firstRenderChild, firstGroup);
        AssertPatchedRenderChild(em, secondRenderChild, secondGroup);
    }

    [Test]
    public void MassRenderSettingsRetriesUnitUntilVisualReferenceExists()
    {
        using var world = new World(nameof(MassRenderSettingsRetriesUnitUntilVisualReferenceExists));
        EntityManager em = world.EntityManager;
        Entity unit = em.CreateEntity(typeof(UnitGrid), typeof(Faction));
        em.SetComponentData(unit, new UnitGrid { Cell = int2.zero });
        em.SetComponentData(unit, new Faction { Id = 1 });
        Entity lodGroup = CreateLodGroup(em);
        Entity renderChild = CreateRenderChildWithFilter(em, unit, lodGroup);
        SystemHandle system = world.CreateSystem<UnitMassRenderSettingsSystem>();

        system.Update(world.Unmanaged);

        Assert.IsFalse(em.HasComponent<UnitMassRenderSettingsApplied>(renderChild));
        Assert.AreEqual(
            new float3(1f, 2f, 3f),
            em.GetComponentData<RenderBounds>(renderChild).Value.Extents);

        em.AddComponent<UnitModelInstanceReference>(unit);
        system.Update(world.Unmanaged);

        AssertPatchedRenderChild(em, renderChild, lodGroup);
    }

    [Test]
    public void MassRenderSettingsClassifiesOperationMapParentWithoutRetry()
    {
        using var world =
            new World(nameof(MassRenderSettingsClassifiesOperationMapParentWithoutRetry));
        EntityManager em = world.EntityManager;
        Entity operationMapParent = em.CreateEntity(
            typeof(UnitGrid),
            typeof(Faction),
            typeof(OperationMapEntityPresentationIdentity));
        Entity lodGroup = CreateLodGroup(em);
        Entity renderChild =
            CreateRenderChildWithFilter(em, operationMapParent, lodGroup);
        SystemHandle system = world.CreateSystem<UnitMassRenderSettingsSystem>();

        system.Update(world.Unmanaged);
        system.Update(world.Unmanaged);

        Assert.IsTrue(
            em.HasComponent<UnitMassRenderSettingsApplied>(renderChild),
            "An operation-map render child must be terminally classified instead of retried.");
        Assert.IsFalse(em.HasComponent<FactionTintTarget>(renderChild));
        Assert.AreEqual(
            new float3(1f, 2f, 3f),
            em.GetComponentData<RenderBounds>(renderChild).Value.Extents);
    }

    [Test]
    public void MassRenderSettingsClassifiesOperationMapAncestorAboveIncompleteUnitParent()
    {
        using var world =
            new World(nameof(MassRenderSettingsClassifiesOperationMapAncestorAboveIncompleteUnitParent));
        EntityManager em = world.EntityManager;
        Entity operationMapRoot =
            em.CreateEntity(typeof(OperationMapEntityPresentationIdentity));
        Entity incompleteUnitParent =
            em.CreateEntity(typeof(Parent), typeof(UnitGrid), typeof(Faction));
        em.SetComponentData(
            incompleteUnitParent,
            new Parent { Value = operationMapRoot });
        Entity lodGroup = CreateLodGroup(em);
        Entity renderChild =
            CreateRenderChildWithFilter(em, incompleteUnitParent, lodGroup);
        SystemHandle system = world.CreateSystem<UnitMassRenderSettingsSystem>();

        system.Update(world.Unmanaged);
        system.Update(world.Unmanaged);

        Assert.IsTrue(
            em.HasComponent<UnitMassRenderSettingsApplied>(renderChild),
            "An incomplete unit-like map parent must keep walking to its terminal operation-map ancestor.");
        Assert.IsFalse(em.HasComponent<FactionTintTarget>(renderChild));
        Assert.AreEqual(
            new float3(1f, 2f, 3f),
            em.GetComponentData<RenderBounds>(renderChild).Value.Extents);
    }

    [Test]
    public void MassRenderSettingsClassifiesPackedResidentRowWithoutHierarchyIdentity()
    {
        using var world =
            new World(nameof(MassRenderSettingsClassifiesPackedResidentRowWithoutHierarchyIdentity));
        EntityManager em = world.EntityManager;
        Entity incompleteUnitParent = em.CreateEntity(typeof(UnitGrid), typeof(Faction));
        Entity lodGroup = CreateLodGroup(em);
        Entity renderChild =
            CreateRenderChildWithFilter(em, incompleteUnitParent, lodGroup);
        Entity database = em.CreateEntity();
        em.AddBuffer<OperationMapRenderResidentSourceRowComponent>(database).Add(
            new OperationMapRenderResidentSourceRowComponent
            {
                RenderEntity = renderChild
            });
        SystemHandle system = world.CreateSystem<UnitMassRenderSettingsSystem>();

        system.Update(world.Unmanaged);

        Assert.IsTrue(
            em.HasComponent<UnitMassRenderSettingsApplied>(renderChild),
            "The exact packed resident-row contract must classify a renderer without relying on transform ancestry.");
        Assert.IsFalse(em.HasComponent<FactionTintTarget>(renderChild));
        Assert.AreEqual(
            new float3(1f, 2f, 3f),
            em.GetComponentData<RenderBounds>(renderChild).Value.Extents);
    }

    public double DenseOperationMapClassification_DrainsThenMeets120HzCpuBudget()
    {
        using var world =
            new World(nameof(DenseOperationMapClassification_DrainsThenMeets120HzCpuBudget));
        EntityManager em = world.EntityManager;
        Entity operationMapParent = em.CreateEntity(typeof(UnitGrid), typeof(Faction));
        EntityArchetype renderArchetype = em.CreateArchetype(
            typeof(Parent),
            typeof(RenderBounds),
            typeof(MaterialMeshInfo));
        using var renderChildren =
            new NativeArray<Entity>(DenseOperationMapRenderChildCount, Allocator.Temp);
        em.CreateEntity(renderArchetype, renderChildren);
        Entity database = em.CreateEntity();
        DynamicBuffer<OperationMapRenderResidentSourceRowComponent> residentRows =
            em.AddBuffer<OperationMapRenderResidentSourceRowComponent>(database);
        for (int i = 0; i < renderChildren.Length; i++)
        {
            em.SetComponentData(renderChildren[i], new Parent { Value = operationMapParent });
            residentRows.Add(new OperationMapRenderResidentSourceRowComponent
            {
                RenderEntity = renderChildren[i]
            });
        }

        SystemHandle massRender =
            world.CreateSystem<UnitMassRenderSettingsSystem>();
        SystemHandle factionTint =
            world.CreateSystem<UnitFactionTintTargetBackfillSystem>();
        int drainFrameLimit =
            (DenseOperationMapRenderChildCount + 11999) / 12000 + 1;
        for (int frame = 0; frame < drainFrameLimit; frame++)
        {
            massRender.Update(world.Unmanaged);
            factionTint.Update(world.Unmanaged);
        }

        using EntityQuery remaining = em.CreateEntityQuery(new EntityQueryDesc
        {
            All = new[]
            {
                ComponentType.ReadOnly<Parent>(),
                ComponentType.ReadOnly<RenderBounds>()
            },
            None = new[]
            {
                ComponentType.ReadOnly<UnitMassRenderSettingsApplied>()
            }
        });
        Assert.AreEqual(
            0,
            remaining.CalculateEntityCount(),
            "The exact dense resident render-child count must drain within the bounded pass count.");

        var samples = new double[Vrp054SteadyStateSampleCount];
        var stopwatch = new System.Diagnostics.Stopwatch();
        for (int sample = 0; sample < samples.Length; sample++)
        {
            stopwatch.Restart();
            massRender.Update(world.Unmanaged);
            factionTint.Update(world.Unmanaged);
            stopwatch.Stop();
            samples[sample] = stopwatch.Elapsed.TotalMilliseconds;
        }

        System.Array.Sort(samples);
        int p95Index = (int)System.Math.Ceiling(samples.Length * 0.95d) - 1;
        double p95Milliseconds = samples[p95Index];
        Assert.That(
            p95Milliseconds,
            Is.LessThan(1000d / 120d),
            "The corrected stationary CPU owners must fit a 120-Hz Editor CPU budget.");
        return p95Milliseconds;
    }

    private static Entity CreateLodGroup(EntityManager em)
    {
        Entity lodGroup = em.CreateEntity(typeof(MeshLODGroupComponent));
        em.SetComponentData(lodGroup, new MeshLODGroupComponent
        {
            ParentMask = 1,
            LODDistances0 = new float4(1f),
            LODDistances1 = new float4(2f)
        });
        return lodGroup;
    }

    private static Entity CreateRenderChildWithFilter(EntityManager em, Entity unit, Entity lodGroup)
    {
        Entity renderChild = em.CreateEntity(typeof(Parent), typeof(RenderBounds), typeof(MeshLODComponent));
        em.SetComponentData(renderChild, new Parent { Value = unit });
        em.SetComponentData(renderChild, new RenderBounds
        {
            Value = new AABB { Center = float3.zero, Extents = new float3(1f, 2f, 3f) }
        });
        em.SetComponentData(renderChild, new MeshLODComponent
        {
            Group = lodGroup,
            ParentGroup = Entity.Null,
            LODMask = 1
        });
        em.AddSharedComponentManaged(renderChild, new RenderFilterSettings
        {
            ShadowCastingMode = ShadowCastingMode.Off,
            ReceiveShadows = false,
            StaticShadowCaster = true
        });
        return renderChild;
    }

    private static void AssertPatchedRenderChild(EntityManager em, Entity renderChild, Entity lodGroup)
    {
        Assert.IsTrue(em.HasComponent<UnitMassRenderSettingsApplied>(renderChild));
        Assert.AreEqual(new float3(64f, 64f, 64f), em.GetComponentData<RenderBounds>(renderChild).Value.Extents);
        Assert.AreEqual(0xFF, em.GetComponentData<MeshLODComponent>(renderChild).LODMask);
        MeshLODGroupComponent patchedGroup = em.GetComponentData<MeshLODGroupComponent>(lodGroup);
        Assert.AreEqual(0xFF, patchedGroup.ParentMask);
        Assert.AreEqual(new float4(1048576f), patchedGroup.LODDistances0);
        Assert.AreEqual(new float4(1048576f), patchedGroup.LODDistances1);
        RenderFilterSettings settings = em.GetSharedComponentManaged<RenderFilterSettings>(renderChild);
        Assert.AreEqual(ShadowCastingMode.On, settings.ShadowCastingMode);
        Assert.IsTrue(settings.ReceiveShadows);
        Assert.IsFalse(settings.StaticShadowCaster);
    }

    [Test]
    public void DiagnosticLogFlushClearsQueuedMessages()
    {
        using var world = new World(nameof(DiagnosticLogFlushClearsQueuedMessages));
        EntityManager em = world.EntityManager;
        Entity queue = em.CreateEntity(typeof(UnitRenderBudgetDiagnosticLogQueueComponent));
        DynamicBuffer<UnitRenderBudgetDiagnosticLogComponent> logs = em.AddBuffer<UnitRenderBudgetDiagnosticLogComponent>(queue);
        logs.Add(new UnitRenderBudgetDiagnosticLogComponent
        {
            Message = new FixedString4096Bytes("Unit render budget diagnostic test message."),
            Severity = UnitRenderBudgetDiagnosticLogComponent.LogSeverity
        });
        SystemHandle system = world.CreateSystem<UnitRenderBudgetDiagnosticLogFlushSystem>();

        system.Update(world.Unmanaged);

        Assert.AreEqual(0, em.GetBuffer<UnitRenderBudgetDiagnosticLogComponent>(queue).Length);
    }

    [Test]
    public void CharacterClassificationUsesCachedLookups()
    {
        using var world = new World(nameof(CharacterClassificationUsesCachedLookups));
        EntityManager em = world.EntityManager;
        Entity soldier = em.CreateEntity(typeof(UnitMovementBehavior), typeof(UnitSourcePrefabKey));
        em.SetComponentData(soldier, new UnitMovementBehavior { UsesVehicleMotion = 0 });
        em.SetComponentData(soldier, new UnitSourcePrefabKey { Value = new FixedString64Bytes("Unit_Chr_Soldier_Male_01") });
        Entity vehicleNamedLikeCharacter = em.CreateEntity(typeof(UnitMovementBehavior), typeof(UnitSourcePrefabKey));
        em.SetComponentData(vehicleNamedLikeCharacter, new UnitMovementBehavior { UsesVehicleMotion = 1 });
        em.SetComponentData(vehicleNamedLikeCharacter, new UnitSourcePrefabKey { Value = new FixedString64Bytes("Unit_Chr_Soldier_Male_01") });
        Entity missingSource = em.CreateEntity(typeof(UnitMovementBehavior));
        em.SetComponentData(missingSource, new UnitMovementBehavior { UsesVehicleMotion = 0 });
        UnitRenderBudgetTestLookupSystem lookupSystem = world.GetOrCreateSystemManaged<UnitRenderBudgetTestLookupSystem>();
        var movementLookup = lookupSystem.GetMovementBehaviorLookup();
        var sourceLookup = lookupSystem.GetSourcePrefabKeyLookup();
        var classification = new UnitRenderBudgetClassification();

        Assert.IsTrue(classification.IsCharacterUnit(soldier, movementLookup, sourceLookup));
        Assert.IsFalse(classification.IsCharacterUnit(vehicleNamedLikeCharacter, movementLookup, sourceLookup));
        Assert.IsFalse(classification.IsCharacterUnit(missingSource, movementLookup, sourceLookup));
    }

    [Test]
    public void LodReferenceResolutionUsesCachedLookups()
    {
        using var world = new World(nameof(LodReferenceResolutionUsesCachedLookups));
        EntityManager em = world.EntityManager;
        Entity unit = em.CreateEntity();
        Entity detailRoot = em.CreateEntity();
        Entity midPrefab = em.CreateEntity();
        Entity midRoot = em.CreateEntity();
        Entity lowPrefab = em.CreateEntity();
        em.AddComponentData(unit, new UnitDetailedVisualReference { Root = detailRoot });
        em.AddComponentData(unit, new UnitMidLodPrefabReference { Prefab = midPrefab });
        em.AddComponentData(unit, new UnitMidLodInstanceReference { Instance = midRoot });
        em.AddComponentData(unit, new UnitLowLodPrefabReference { Prefab = lowPrefab });
        UnitRenderBudgetTestLookupSystem lookupSystem = world.GetOrCreateSystemManaged<UnitRenderBudgetTestLookupSystem>();

        UnitRenderBudgetLodReferences.UnitReferences references =
            new UnitRenderBudgetLodReferences().ResolveUnitReferences(
                unit,
                lookupSystem.GetLodReferenceLookups());

        Assert.IsTrue(references.HasDetailRoot);
        Assert.AreEqual(detailRoot, references.DetailRoot);
        Assert.IsTrue(references.HasMidLodPrefab);
        Assert.IsTrue(references.HasMidLodInstance);
        Assert.AreEqual(midRoot, references.MidRoot);
        Assert.IsTrue(references.HasLowLodPrefab);
        Assert.IsFalse(references.HasLowLodInstance);
        Assert.AreEqual(Entity.Null, references.LowRoot);
        Assert.IsTrue(references.HasAnyMeshLodPrefab);
        Assert.IsTrue(references.HasAnyMeshLodInstance);
    }

    [Test]
    public void RenderableQueryUsesCachedLookups()
    {
        using var world = new World(nameof(RenderableQueryUsesCachedLookups));
        EntityManager em = world.EntityManager;
        Entity root = em.CreateEntity();
        em.AddBuffer<Child>(root);
        Entity visibleChild = CreateRenderableEntity(em, new float3(1f));
        Entity hiddenChild = CreateRenderableEntity(em, new float3(1f));
        em.AddComponent<DisableRendering>(hiddenChild);
        Entity safeLod = em.CreateEntity(typeof(UnitSafeVisibleCharacterLodTag));
        DynamicBuffer<Child> rootChildren = em.GetBuffer<Child>(root);
        rootChildren.Add(new Child { Value = visibleChild });
        rootChildren.Add(new Child { Value = hiddenChild });
        UnitRenderBudgetTestLookupSystem lookupSystem = world.GetOrCreateSystemManaged<UnitRenderBudgetTestLookupSystem>();
        BufferLookup<Child> childLookup = lookupSystem.GetChildLookup();
        UnitRenderBudgetRenderableState.Lookups lookups = lookupSystem.GetRenderableQueryLookups();
        var system = new UnitRenderBudgetRenderableState();

        Assert.IsTrue(system.HasRenderableRecursive(root, childLookup, lookups));
        Assert.IsTrue(system.IsRenderableVisibleRecursive(root, childLookup, lookups));
        Assert.IsFalse(system.IsRenderableVisibleRecursive(hiddenChild, childLookup, lookups));
        Assert.IsTrue(system.IsSafeVisibleCharacterLod(safeLod, lookups));
        Assert.IsFalse(system.IsSafeVisibleCharacterLod(visibleChild, lookups));
    }

    [Test]
    public void MovingVisibleCharactersUseAnimatableMeshLodPath()
    {
        var policy = new UnitRenderBudgetCharacterPolicy();
        UnitRenderVisualKind visual = policy.ResolveVisibleCharacterVisualKind(
            movingVisibleCharacter: true,
            forceDetailNearVisible: false,
            forceDetailByBudget: false,
            farEnoughForImpostor: true,
            lowEnoughForSafeLow: true,
            hasSafeMid: true,
            midRootAnimatable: true,
            hasSafeLow: true,
            lowRootAnimatable: true);

        Assert.AreEqual(UnitRenderVisualKind.Mid, visual);
    }

    [Test]
    public void MovingVisibleCharactersFallbackToDetailWhenMeshLodIsNotAnimatable()
    {
        var policy = new UnitRenderBudgetCharacterPolicy();
        UnitRenderVisualKind visual = policy.ResolveVisibleCharacterVisualKind(
            movingVisibleCharacter: true,
            forceDetailNearVisible: false,
            forceDetailByBudget: false,
            farEnoughForImpostor: true,
            lowEnoughForSafeLow: true,
            hasSafeMid: true,
            midRootAnimatable: false,
            hasSafeLow: true,
            lowRootAnimatable: false);

        Assert.AreEqual(UnitRenderVisualKind.Detail, visual);
    }

    [Test]
    public void IdleDistantVisibleCharactersUseFarImpostorPath()
    {
        var policy = new UnitRenderBudgetCharacterPolicy();
        UnitRenderVisualKind visual = policy.ResolveVisibleCharacterVisualKind(
            movingVisibleCharacter: false,
            forceDetailNearVisible: false,
            forceDetailByBudget: false,
            farEnoughForImpostor: true,
            lowEnoughForSafeLow: true,
            hasSafeMid: true,
            midRootAnimatable: true,
            hasSafeLow: true,
            lowRootAnimatable: true);

        Assert.AreEqual(UnitRenderVisualKind.Far, visual);
    }

    [Test]
    public void NearVisibleCharactersUseDetailedModelPath()
    {
        var policy = new UnitRenderBudgetCharacterPolicy();
        UnitRenderVisualKind visual = policy.ResolveVisibleCharacterVisualKind(
            movingVisibleCharacter: false,
            forceDetailNearVisible: true,
            forceDetailByBudget: false,
            farEnoughForImpostor: true,
            lowEnoughForSafeLow: true,
            hasSafeMid: true,
            midRootAnimatable: true,
            hasSafeLow: true,
            lowRootAnimatable: true);

        Assert.AreEqual(UnitRenderVisualKind.Detail, visual);
    }

    [Test]
    public void CharacterRenderPolicyDoesNotGloballyForceDetailedModelPath()
    {
        var policy = new UnitRenderBudgetCharacterPolicy();
        Assert.IsFalse(policy.ShouldForceCharacterDetailVisual(true));
        Assert.IsFalse(policy.ShouldForceCharacterDetailVisual(false));
    }

    [Test]
    public void ImpostorTagRequestUsesCachedLookup()
    {
        using var world = new World(nameof(ImpostorTagRequestUsesCachedLookup));
        EntityManager em = world.EntityManager;
        Entity farUnit = em.CreateEntity();
        Entity detailedUnit = em.CreateEntity(typeof(UnitRenderBudgetCulledUnitTag));
        UnitRenderBudgetTestLookupSystem lookupSystem = world.GetOrCreateSystemManaged<UnitRenderBudgetTestLookupSystem>();
        var culledLookup = lookupSystem.GetCulledUnitLookup();
        using var unitsToShowDetailed = new NativeList<Entity>(Allocator.Temp);
        using var unitsToShowFarImpostor = new NativeList<Entity>(Allocator.Temp);
        int changed = 0;
        var system = new UnitRenderBudgetImpostorTag();

        system.CollectUnitImpostorTagRequest(
            farUnit,
            shouldShowFar: true,
            culledLookup,
            unitsToShowDetailed,
            unitsToShowFarImpostor,
            ref changed);
        system.CollectUnitImpostorTagRequest(
            detailedUnit,
            shouldShowFar: false,
            culledLookup,
            unitsToShowDetailed,
            unitsToShowFarImpostor,
            ref changed);

        Assert.AreEqual(2, changed);
        Assert.AreEqual(farUnit, unitsToShowFarImpostor[0]);
        Assert.AreEqual(detailedUnit, unitsToShowDetailed[0]);
    }

    [Test]
    public void UnselectedEnemyBeyondImpostorThresholdUsesFarImpostorVisual()
    {
        using var world = new World(nameof(UnselectedEnemyBeyondImpostorThresholdUsesFarImpostorVisual));
        using var ecb = new EntityCommandBuffer(Allocator.Temp);
        using var readyTaggedThisFrame = new NativeHashSet<Entity>(1, Allocator.Temp);

        UnitRenderBudgetVisualPlan.Result result = new UnitRenderBudgetVisualPlan().CreateDesiredVisualPlan(
            world.EntityManager,
            ecb,
            readyTaggedThisFrame,
            default,
            new UnitRenderBudgetVisualPlan.Request
            {
                Unit = TestEntity(1),
                IsEnemyUnit = true,
                IsSelectedUnit = false,
                DistanceSq = 29f * 29f,
                EnemyImpostorDistanceSq = 28f * 28f,
                EnemyLowLodDistanceSq = 20f * 20f,
                EnemyAlwaysDetailedDistanceSq = 14f * 14f,
                AlwaysDetailedDistanceSq = 18f * 18f,
                VisibleCharacterLowDistanceSq = 32f * 32f,
                VisibleCharacterImpostorNearDistance = 48f,
                VisibleCharacterImpostorFarDistance = 48f,
                HasMidLodInstance = true,
                HasLowLodInstance = true,
                MidBand = false,
                LowBand = true
            },
            new UnitRenderBudgetCharacterPolicy(),
            new UnitRenderBudgetReadiness(),
            new UnitRenderBudgetAnimationReadiness(),
            new UnitRenderBudgetRenderableState());

        Assert.AreEqual(UnitRenderVisualKind.Far, result.DesiredVisual);
        Assert.IsTrue(result.ShouldShowFar);
        Assert.IsFalse(result.ShouldShowDetail);
        Assert.IsFalse(result.ShouldShowMid);
        Assert.IsFalse(result.ShouldShowLow);
    }

    [Test]
    public void SelectedVisibleCharacterNeverUsesFarImpostorAtZoomedOutDistance()
    {
        using var world = new World(nameof(SelectedVisibleCharacterNeverUsesFarImpostorAtZoomedOutDistance));
        using var ecb = new EntityCommandBuffer(Allocator.Temp);
        using var readyTaggedThisFrame = new NativeHashSet<Entity>(1, Allocator.Temp);

        UnitRenderBudgetVisualPlan.Result result = new UnitRenderBudgetVisualPlan().CreateDesiredVisualPlan(
            world.EntityManager,
            ecb,
            readyTaggedThisFrame,
            default,
            new UnitRenderBudgetVisualPlan.Request
            {
                Unit = TestEntity(2),
                IsCharacter = true,
                IsSelectedUnit = true,
                Visible = 1,
                DistanceSq = 100f * 100f,
                LowBand = true,
                HasLowLodInstance = true,
                LowRootSafe = true,
                LowRootAnimatable = true,
                AlwaysDetailedDistanceSq = 18f * 18f,
                VisibleCharacterLowDistanceSq = 32f * 32f,
                VisibleCharacterImpostorNearDistance = 48f,
                VisibleCharacterImpostorFarDistance = 48f
            },
            new UnitRenderBudgetCharacterPolicy(),
            new UnitRenderBudgetReadiness(),
            new UnitRenderBudgetAnimationReadiness(),
            new UnitRenderBudgetRenderableState());

        Assert.AreEqual(UnitRenderVisualKind.Detail, result.DesiredVisual);
        Assert.IsTrue(result.ShouldShowDetail);
        Assert.IsFalse(result.ShouldShowFar);
    }

    [Test]
    public void AirVehiclesStayDetailedBeyondLodAndImpostorThresholds()
    {
        using var world = new World(nameof(AirVehiclesStayDetailedBeyondLodAndImpostorThresholds));
        using var ecb = new EntityCommandBuffer(Allocator.Temp);
        using var readyTaggedThisFrame = new NativeHashSet<Entity>(1, Allocator.Temp);

        UnitRenderBudgetVisualPlan.Result result = new UnitRenderBudgetVisualPlan().CreateDesiredVisualPlan(
            world.EntityManager,
            ecb,
            readyTaggedThisFrame,
            default,
            new UnitRenderBudgetVisualPlan.Request
            {
                Unit = TestEntity(4),
                DetailedBand = false,
                MidBand = false,
                LowBand = true,
                HasMidLodInstance = true,
                HasLowLodInstance = true,
                IsCharacter = false,
                IsAirUnit = true,
                IsEnemyUnit = true,
                IsSelectedUnit = false,
                Visible = 1,
                DistanceSq = 10000f,
                AlwaysDetailedDistanceSq = 18f * 18f,
                EnemyAlwaysDetailedDistanceSq = 14f * 14f,
                EnemyLowLodDistanceSq = 20f * 20f,
                EnemyImpostorDistanceSq = 28f * 28f,
                VisibleCharacterLowDistanceSq = 32f * 32f,
                VisibleCharacterImpostorNearDistance = 48f,
                VisibleCharacterImpostorFarDistance = 48f
            },
            new UnitRenderBudgetCharacterPolicy(),
            new UnitRenderBudgetReadiness(),
            new UnitRenderBudgetAnimationReadiness(),
            new UnitRenderBudgetRenderableState());

        Assert.AreEqual(UnitRenderVisualKind.Detail, result.DesiredVisual);
        Assert.IsTrue(result.ShouldShowDetail);
        Assert.IsFalse(result.ShouldShowMid);
        Assert.IsFalse(result.ShouldShowLow);
        Assert.IsFalse(result.ShouldShowFar);
        Assert.IsTrue(result.ForceImmediateDetailVisual);
    }

    [Test]
    public void AirUnitDecisionForcesDetailedRootsWhenBudgetWouldUseLow()
    {
        using var world = new World(nameof(AirUnitDecisionForcesDetailedRootsWhenBudgetWouldUseLow));
        EntityManager em = world.EntityManager;
        Entity detailRoot = em.CreateEntity(
            typeof(Disabled),
            typeof(DisableRendering),
            typeof(UnitRenderBudgetCulledTag));
        Entity midRoot = em.CreateEntity();
        Entity lowRoot = em.CreateEntity();
        Entity unit = em.CreateEntity(
            typeof(UnitMovementBehavior),
            typeof(UnitAirMovement),
            typeof(Faction),
            typeof(UnitDetailedVisualReference),
            typeof(UnitMidLodInstanceReference),
            typeof(UnitLowLodInstanceReference),
            typeof(UnitRenderVisualComponent));
        em.SetComponentData(unit, new UnitMovementBehavior { UsesVehicleMotion = 1 });
        em.SetComponentData(unit, new UnitAirMovement { CruiseHeight = 8f, RunwayTaxiSpeed = 4f });
        em.SetComponentData(unit, new Faction { Id = FactionIdentity.EnemyFactionId });
        em.SetComponentData(unit, new UnitDetailedVisualReference { Root = detailRoot });
        em.SetComponentData(unit, new UnitMidLodInstanceReference { Instance = midRoot });
        em.SetComponentData(unit, new UnitLowLodInstanceReference { Instance = lowRoot });
        em.SetComponentData(unit, new UnitRenderVisualComponent
        {
            Current = (byte)UnitRenderVisualKind.Low,
            Desired = (byte)UnitRenderVisualKind.Low,
            LastChangedFrame = 20
        });

        UnitRenderBudgetTestLookupSystem lookupSystem = world.GetOrCreateSystemManaged<UnitRenderBudgetTestLookupSystem>();
        using var ecb = new EntityCommandBuffer(Allocator.Temp);
        using var safetyTaggedThisFrame = new NativeHashSet<Entity>(4, Allocator.Temp);
        using var readyTaggedThisFrame = new NativeHashSet<Entity>(4, Allocator.Temp);
        using var distances = new NativeList<UnitRenderBudgetDistance.UnitDistance>(Allocator.Temp);
        using var detailedUnits = new NativeHashSet<Entity>(1, Allocator.Temp);
        using var midLodUnits = new NativeHashSet<Entity>(1, Allocator.Temp);
        using var lowLodUnits = new NativeHashSet<Entity>(1, Allocator.Temp);
        using var entitiesToShow = new NativeList<Entity>(Allocator.Temp);
        using var entitiesToHide = new NativeList<Entity>(Allocator.Temp);
        using var unitsToShowDetailed = new NativeList<Entity>(Allocator.Temp);
        using var unitsToShowFarImpostor = new NativeList<Entity>(Allocator.Temp);
        distances.Add(new UnitRenderBudgetDistance.UnitDistance
        {
            Unit = unit,
            DistanceSq = 10000f,
            Priority = 0,
            Visible = 1
        });
        lowLodUnits.Add(unit);

        var context = new UnitRenderBudgetDecision.Context
        {
            RenderStateEcb = ecb,
            SafetyTaggedThisFrame = safetyTaggedThisFrame,
            ReadyTaggedThisFrame = readyTaggedThisFrame,
            ChildLookup = lookupSystem.GetChildLookup(),
            AnimationIndexLookup = lookupSystem.GetMaterialAnimationIndexLookup(),
            MoveVisualLookup = lookupSystem.GetMoveVisualLookup(),
            MovementBehaviorLookup = lookupSystem.GetMovementBehaviorLookup(),
            AirMovementLookup = lookupSystem.GetAirMovementLookup(),
            HealthLookup = lookupSystem.GetHealthLookup(),
            SourcePrefabKeyLookup = lookupSystem.GetSourcePrefabKeyLookup(),
            FactionLookup = lookupSystem.GetFactionLookup(),
            SelectedLookup = lookupSystem.GetSelectedLookup(),
            VisualStateLookup = lookupSystem.GetVisualStateLookup(),
            CulledUnitLookup = lookupSystem.GetCulledUnitLookup(),
            EntityStorageInfoLookup = lookupSystem.GetEntityStorageInfoLookupForTests(),
            DisabledLookup = lookupSystem.GetDisabledLookup(),
            DisableRenderingLookup = lookupSystem.GetDisableRenderingLookup(),
            CulledTagLookup = lookupSystem.GetCulledTagLookup(),
            Distances = distances,
            DetailedUnits = detailedUnits,
            MidLodUnits = midLodUnits,
            LowLodUnits = lowLodUnits,
            EntitiesToShow = entitiesToShow,
            EntitiesToHide = entitiesToHide,
            UnitsToShowDetailed = unitsToShowDetailed,
            UnitsToShowFarImpostor = unitsToShowFarImpostor,
            AlwaysDetailedDistanceSq = 18f * 18f,
            EnemyAlwaysDetailedDistanceSq = 14f * 14f,
            EnemyLowLodDistanceSq = 20f * 20f,
            EnemyImpostorDistanceSq = 28f * 28f,
            VisibleCharacterLowDistanceSq = 32f * 32f,
            VisibleCharacterImpostorNearDistance = 48f,
            VisibleCharacterImpostorFarDistance = 48f,
            ClassificationSystem = new UnitRenderBudgetClassification(),
            CharacterPolicySystem = new UnitRenderBudgetCharacterPolicy(),
            LodReferenceSystem = new UnitRenderBudgetLodReferences(),
            LodReferenceLookups = lookupSystem.GetLodReferenceLookups(),
            AnimationReadinessSystem = new UnitRenderBudgetAnimationReadiness(),
            AnimationReadinessLookups = lookupSystem.GetAnimationReadinessLookups(),
            RenderableQuerySystem = new UnitRenderBudgetRenderableState(),
            RenderableQueryLookups = lookupSystem.GetRenderableQueryLookups(),
            VisualStateSystem = new UnitRenderBudgetVisualState(),
            ReadinessSystem = new UnitRenderBudgetReadiness(),
            ReadinessLookups = lookupSystem.GetReadinessLookups(),
            RenderSafetySystem = new UnitRenderBudgetRenderSafety(),
            RenderSafetyLookups = lookupSystem.GetRenderSafetyLookups(),
            VisualPlanSystem = new UnitRenderBudgetVisualPlan(),
            VisibilityChangeSystem = new UnitRenderBudgetVisibilityChange(),
            ImpostorTagSystem = new UnitRenderBudgetImpostorTag(),
            CurrentFrame = 30
        };

        UnitRenderBudgetDecision.Result result = new UnitRenderBudgetDecision().Process(ref context);

        Assert.AreEqual(3, result.Changed);
        Assert.AreEqual(1, entitiesToShow.Length);
        Assert.AreEqual(detailRoot, entitiesToShow[0]);
        Assert.AreEqual(2, entitiesToHide.Length);
        Assert.IsTrue(entitiesToHide.Contains(midRoot));
        Assert.IsTrue(entitiesToHide.Contains(lowRoot));
        Assert.AreEqual(0, unitsToShowFarImpostor.Length);
        Assert.AreEqual(0, result.FarCount);
    }

    [Test]
    public void SelectedVehicleForcesImmediateDetailedVisual()
    {
        using var world = new World(nameof(SelectedVehicleForcesImmediateDetailedVisual));
        using var ecb = new EntityCommandBuffer(Allocator.Temp);
        using var readyTaggedThisFrame = new NativeHashSet<Entity>(1, Allocator.Temp);
        EntityManager em = world.EntityManager;

        UnitRenderBudgetVisualPlan.Result result = new UnitRenderBudgetVisualPlan().CreateDesiredVisualPlan(
            em,
            ecb,
            readyTaggedThisFrame,
            default,
            new UnitRenderBudgetVisualPlan.Request
            {
                Unit = TestEntity(3),
                DetailedBand = false,
                MidBand = true,
                LowBand = false,
                HasMidLodInstance = true,
                HasLowLodInstance = true,
                IsCharacter = false,
                IsEnemyUnit = false,
                IsSelectedUnit = true,
                Visible = 1,
                DistanceSq = 10000f,
                AlwaysDetailedDistanceSq = 18f * 18f,
                EnemyAlwaysDetailedDistanceSq = 14f * 14f,
                EnemyLowLodDistanceSq = 20f * 20f,
                EnemyImpostorDistanceSq = 28f * 28f,
                VisibleCharacterLowDistanceSq = 32f * 32f,
                VisibleCharacterImpostorNearDistance = 48f,
                VisibleCharacterImpostorFarDistance = 48f
            },
            new UnitRenderBudgetCharacterPolicy(),
            new UnitRenderBudgetReadiness(),
            new UnitRenderBudgetAnimationReadiness(),
            new UnitRenderBudgetRenderableState());

        Assert.AreEqual(UnitRenderVisualKind.Detail, result.DesiredVisual);
        Assert.IsTrue(result.ShouldShowDetail);
        Assert.IsFalse(result.ShouldShowMid);
        Assert.IsFalse(result.ShouldShowLow);
        Assert.IsFalse(result.ShouldShowFar);
        Assert.IsTrue(result.ForceImmediateDetailVisual);
    }

    [Test]
    public void StableBudgetDoesNotSkipSelectedUnitsAndSelectionChanges()
    {
        var schedule = new UnitRenderBudgetSchedule();
        schedule.ScheduleNextUpdate(cameraMotionActive: false, frame: 10, updateIntervalFrames: 10);
        schedule.RecordBudgetStability(
            currentUnitCount: 12,
            currentSelectedUnitCount: 0,
            currentSelectedUnitHash: 0,
            budgetStable: true);

        Assert.IsTrue(schedule.ShouldSkipStableBudget(
            cameraMotionActive: false,
            currentUnitCount: 12,
            currentSelectedUnitCount: 0,
            currentSelectedUnitHash: 0));
        Assert.IsTrue(schedule.ShouldSkipUpdateFrame(
            cameraMotionActive: false,
            frame: 15,
            currentSelectedUnitCount: 0,
            currentSelectedUnitHash: 0));
        Assert.IsFalse(schedule.ShouldSkipStableBudget(
            cameraMotionActive: false,
            currentUnitCount: 12,
            currentSelectedUnitCount: 1,
            currentSelectedUnitHash: 3075));
        Assert.IsFalse(schedule.ShouldSkipUpdateFrame(
            cameraMotionActive: false,
            frame: 15,
            currentSelectedUnitCount: 1,
            currentSelectedUnitHash: 3075));

        schedule.RecordBudgetStability(
            currentUnitCount: 12,
            currentSelectedUnitCount: 1,
            currentSelectedUnitHash: 3075,
            budgetStable: true);
        schedule.ScheduleNextUpdate(cameraMotionActive: false, frame: 20, updateIntervalFrames: 10);

        Assert.IsFalse(schedule.ShouldSkipStableBudget(
            cameraMotionActive: false,
            currentUnitCount: 12,
            currentSelectedUnitCount: 1,
            currentSelectedUnitHash: 3075));
        Assert.IsTrue(schedule.ShouldSkipUpdateFrame(
            cameraMotionActive: false,
            frame: 25,
            currentSelectedUnitCount: 1,
            currentSelectedUnitHash: 3075));
        Assert.IsFalse(schedule.ShouldSkipUpdateFrame(
            cameraMotionActive: false,
            frame: 25,
            currentSelectedUnitCount: 1,
            currentSelectedUnitHash: 4096));
    }

    [Test]
    public void CameraMotionBudgetRespectsThreeFrameThrottle()
    {
        var schedule = new UnitRenderBudgetSchedule();
        schedule.MarkCameraMotion(frame: 10, settleFrames: 8);
        Assert.IsFalse(schedule.ShouldSkipUpdateFrame(
            cameraMotionActive: true,
            frame: 10,
            currentSelectedUnitCount: 0,
            currentSelectedUnitHash: 0));

        schedule.ScheduleNextUpdate(cameraMotionActive: true, frame: 10, updateIntervalFrames: 10);
        schedule.MarkCameraMotion(frame: 11, settleFrames: 8);
        Assert.IsTrue(schedule.ShouldSkipUpdateFrame(
            cameraMotionActive: true,
            frame: 11,
            currentSelectedUnitCount: 0,
            currentSelectedUnitHash: 0));
        Assert.IsTrue(schedule.ShouldSkipUpdateFrame(
            cameraMotionActive: true,
            frame: 12,
            currentSelectedUnitCount: 0,
            currentSelectedUnitHash: 0));
        Assert.IsFalse(schedule.ShouldSkipUpdateFrame(
            cameraMotionActive: true,
            frame: 13,
            currentSelectedUnitCount: 0,
            currentSelectedUnitHash: 0));
    }

    [Test]
    public void SelectedVehicleReappliesDetailRootsWhenVisualStateAlreadyDetail()
    {
        using var world = new World(nameof(SelectedVehicleReappliesDetailRootsWhenVisualStateAlreadyDetail));
        EntityManager em = world.EntityManager;
        Entity detailRoot = em.CreateEntity();
        Entity midRoot = em.CreateEntity();
        Entity lowRoot = em.CreateEntity();
        Entity unit = em.CreateEntity(
            typeof(UnitMovementBehavior),
            typeof(SelectedUnitTag),
            typeof(UnitDetailedVisualReference),
            typeof(UnitMidLodInstanceReference),
            typeof(UnitLowLodInstanceReference),
            typeof(UnitRenderVisualComponent));
        em.SetComponentData(unit, new UnitMovementBehavior { UsesVehicleMotion = 1 });
        em.SetComponentData(unit, new UnitDetailedVisualReference { Root = detailRoot });
        em.SetComponentData(unit, new UnitMidLodInstanceReference { Instance = midRoot });
        em.SetComponentData(unit, new UnitLowLodInstanceReference { Instance = lowRoot });
        em.SetComponentData(unit, new UnitRenderVisualComponent
        {
            Current = (byte)UnitRenderVisualKind.Detail,
            Desired = (byte)UnitRenderVisualKind.Detail,
            LastChangedFrame = 20
        });

        UnitRenderBudgetTestLookupSystem lookupSystem = world.GetOrCreateSystemManaged<UnitRenderBudgetTestLookupSystem>();
        using var ecb = new EntityCommandBuffer(Allocator.Temp);
        using var safetyTaggedThisFrame = new NativeHashSet<Entity>(4, Allocator.Temp);
        using var readyTaggedThisFrame = new NativeHashSet<Entity>(4, Allocator.Temp);
        using var distances = new NativeList<UnitRenderBudgetDistance.UnitDistance>(Allocator.Temp);
        using var detailedUnits = new NativeHashSet<Entity>(1, Allocator.Temp);
        using var midLodUnits = new NativeHashSet<Entity>(1, Allocator.Temp);
        using var lowLodUnits = new NativeHashSet<Entity>(1, Allocator.Temp);
        using var entitiesToShow = new NativeList<Entity>(Allocator.Temp);
        using var entitiesToHide = new NativeList<Entity>(Allocator.Temp);
        using var unitsToShowDetailed = new NativeList<Entity>(Allocator.Temp);
        using var unitsToShowFarImpostor = new NativeList<Entity>(Allocator.Temp);
        distances.Add(new UnitRenderBudgetDistance.UnitDistance
        {
            Unit = unit,
            DistanceSq = 10000f,
            Priority = 0,
            Visible = 1
        });
        midLodUnits.Add(unit);
        lowLodUnits.Add(unit);

        var context = new UnitRenderBudgetDecision.Context
        {
            RenderStateEcb = ecb,
            SafetyTaggedThisFrame = safetyTaggedThisFrame,
            ReadyTaggedThisFrame = readyTaggedThisFrame,
            ChildLookup = lookupSystem.GetChildLookup(),
            AnimationIndexLookup = lookupSystem.GetMaterialAnimationIndexLookup(),
            MoveVisualLookup = lookupSystem.GetMoveVisualLookup(),
            MovementBehaviorLookup = lookupSystem.GetMovementBehaviorLookup(),
            AirMovementLookup = lookupSystem.GetAirMovementLookup(),
            HealthLookup = lookupSystem.GetHealthLookup(),
            SourcePrefabKeyLookup = lookupSystem.GetSourcePrefabKeyLookup(),
            FactionLookup = lookupSystem.GetFactionLookup(),
            SelectedLookup = lookupSystem.GetSelectedLookup(),
            VisualStateLookup = lookupSystem.GetVisualStateLookup(),
            CulledUnitLookup = lookupSystem.GetCulledUnitLookup(),
            EntityStorageInfoLookup = lookupSystem.GetEntityStorageInfoLookupForTests(),
            DisabledLookup = lookupSystem.GetDisabledLookup(),
            DisableRenderingLookup = lookupSystem.GetDisableRenderingLookup(),
            CulledTagLookup = lookupSystem.GetCulledTagLookup(),
            Distances = distances,
            DetailedUnits = detailedUnits,
            MidLodUnits = midLodUnits,
            LowLodUnits = lowLodUnits,
            EntitiesToShow = entitiesToShow,
            EntitiesToHide = entitiesToHide,
            UnitsToShowDetailed = unitsToShowDetailed,
            UnitsToShowFarImpostor = unitsToShowFarImpostor,
            AlwaysDetailedDistanceSq = 18f * 18f,
            EnemyAlwaysDetailedDistanceSq = 14f * 14f,
            EnemyLowLodDistanceSq = 20f * 20f,
            EnemyImpostorDistanceSq = 28f * 28f,
            VisibleCharacterLowDistanceSq = 32f * 32f,
            VisibleCharacterImpostorNearDistance = 48f,
            VisibleCharacterImpostorFarDistance = 48f,
            ClassificationSystem = new UnitRenderBudgetClassification(),
            CharacterPolicySystem = new UnitRenderBudgetCharacterPolicy(),
            LodReferenceSystem = new UnitRenderBudgetLodReferences(),
            LodReferenceLookups = lookupSystem.GetLodReferenceLookups(),
            AnimationReadinessSystem = new UnitRenderBudgetAnimationReadiness(),
            AnimationReadinessLookups = lookupSystem.GetAnimationReadinessLookups(),
            RenderableQuerySystem = new UnitRenderBudgetRenderableState(),
            RenderableQueryLookups = lookupSystem.GetRenderableQueryLookups(),
            VisualStateSystem = new UnitRenderBudgetVisualState(),
            ReadinessSystem = new UnitRenderBudgetReadiness(),
            ReadinessLookups = lookupSystem.GetReadinessLookups(),
            RenderSafetySystem = new UnitRenderBudgetRenderSafety(),
            RenderSafetyLookups = lookupSystem.GetRenderSafetyLookups(),
            VisualPlanSystem = new UnitRenderBudgetVisualPlan(),
            VisibilityChangeSystem = new UnitRenderBudgetVisibilityChange(),
            ImpostorTagSystem = new UnitRenderBudgetImpostorTag(),
            CurrentFrame = 30
        };

        UnitRenderBudgetDecision.Result result = new UnitRenderBudgetDecision().Process(ref context);

        Assert.AreEqual(2, result.Changed);
        Assert.AreEqual(2, entitiesToHide.Length);
        Assert.IsTrue(entitiesToHide.Contains(midRoot));
        Assert.IsTrue(entitiesToHide.Contains(lowRoot));
        Assert.AreEqual(0, entitiesToShow.Length);
    }

    [Test]
    public void MissingMeshLodInstanceKeepsDetailVisibleUntilReady()
    {
        using var world = new World(nameof(MissingMeshLodInstanceKeepsDetailVisibleUntilReady));
        using var ecb = new EntityCommandBuffer(Allocator.Temp);
        using var readyTaggedThisFrame = new NativeHashSet<Entity>(1, Allocator.Temp);

        UnitRenderBudgetVisualPlan.Result result = new UnitRenderBudgetVisualPlan().CreateDesiredVisualPlan(
            world.EntityManager,
            ecb,
            readyTaggedThisFrame,
            default,
            new UnitRenderBudgetVisualPlan.Request
            {
                Unit = TestEntity(2),
                DetailedBand = false,
                MidBand = true,
                LowBand = false,
                HasMidLodInstance = true,
                HasLowLodInstance = false,
                HasAnyMeshLodPrefab = true,
                HasAnyMeshLodInstance = false,
                AlwaysDetailedDistanceSq = 18f * 18f,
                EnemyAlwaysDetailedDistanceSq = 14f * 14f,
                EnemyLowLodDistanceSq = 20f * 20f,
                EnemyImpostorDistanceSq = 28f * 28f,
                VisibleCharacterLowDistanceSq = 32f * 32f,
                VisibleCharacterImpostorNearDistance = 48f,
                VisibleCharacterImpostorFarDistance = 48f
            },
            new UnitRenderBudgetCharacterPolicy(),
            new UnitRenderBudgetReadiness(),
            new UnitRenderBudgetAnimationReadiness(),
            new UnitRenderBudgetRenderableState());

        Assert.AreEqual(UnitRenderVisualKind.Detail, result.DesiredVisual);
        Assert.IsTrue(result.ShouldShowDetail);
        Assert.IsTrue(result.ForceImmediateDetailVisual);
        Assert.IsFalse(result.ShouldShowMid);
        Assert.IsFalse(result.ShouldShowLow);
        Assert.IsFalse(result.ShouldShowFar);
    }

    [Test]
    public void VisualStateTransitionKeepsCurrentVisualUntilStableOrForced()
    {
        using var world = new World(nameof(VisualStateTransitionKeepsCurrentVisualUntilStableOrForced));
        EntityManager em = world.EntityManager;
        Entity unit = em.CreateEntity();
        UnitRenderBudgetTestLookupSystem lookupSystem = world.GetOrCreateSystemManaged<UnitRenderBudgetTestLookupSystem>();
        var visualStateSystem = new UnitRenderBudgetVisualState();

        int visualStateChanges = 0;
        int visualStatePending = 0;
        int visualTransitionsCommitted = 0;
        using (var ecb = new EntityCommandBuffer(Allocator.Temp))
        {
            UnitRenderVisualKind initialVisual = visualStateSystem.ResolveStableUnitRenderVisualState(
                lookupSystem.GetVisualStateLookup(),
                ecb,
                unit,
                UnitRenderVisualKind.Mid,
                forceImmediate: false,
                currentFrame: 10,
                ref visualStateChanges,
                ref visualStatePending,
                ref visualTransitionsCommitted);

            Assert.AreEqual(UnitRenderVisualKind.Mid, initialVisual);
            ecb.Playback(em);
        }

        UnitRenderVisualComponent state = em.GetComponentData<UnitRenderVisualComponent>(unit);
        Assert.AreEqual((byte)UnitRenderVisualKind.Mid, state.Current);
        Assert.AreEqual((byte)UnitRenderVisualKind.Mid, state.Desired);

        visualStateChanges = 0;
        visualStatePending = 0;
        visualTransitionsCommitted = 0;
        using (var ecb = new EntityCommandBuffer(Allocator.Temp))
        {
            UnitRenderVisualKind pendingVisual = visualStateSystem.ResolveStableUnitRenderVisualState(
                lookupSystem.GetVisualStateLookup(),
                ecb,
                unit,
                UnitRenderVisualKind.Low,
                forceImmediate: false,
                currentFrame: 11,
                ref visualStateChanges,
                ref visualStatePending,
                ref visualTransitionsCommitted);

            Assert.AreEqual(UnitRenderVisualKind.Mid, pendingVisual);
            ecb.Playback(em);
        }

        state = em.GetComponentData<UnitRenderVisualComponent>(unit);
        Assert.AreEqual((byte)UnitRenderVisualKind.Mid, state.Current);
        Assert.AreEqual((byte)UnitRenderVisualKind.Low, state.Desired);
        Assert.AreEqual(1, visualStateChanges);
        Assert.AreEqual(1, visualStatePending);
        Assert.AreEqual(0, visualTransitionsCommitted);

        visualStateChanges = 0;
        visualStatePending = 0;
        visualTransitionsCommitted = 0;
        using (var ecb = new EntityCommandBuffer(Allocator.Temp))
        {
            UnitRenderVisualKind forcedVisual = visualStateSystem.ResolveStableUnitRenderVisualState(
                lookupSystem.GetVisualStateLookup(),
                ecb,
                unit,
                UnitRenderVisualKind.Detail,
                forceImmediate: true,
                currentFrame: 12,
                ref visualStateChanges,
                ref visualStatePending,
                ref visualTransitionsCommitted);

            Assert.AreEqual(UnitRenderVisualKind.Detail, forcedVisual);
            ecb.Playback(em);
        }

        state = em.GetComponentData<UnitRenderVisualComponent>(unit);
        Assert.AreEqual((byte)UnitRenderVisualKind.Detail, state.Current);
        Assert.AreEqual((byte)UnitRenderVisualKind.Detail, state.Desired);
        Assert.AreEqual(1, visualStateChanges);
        Assert.AreEqual(0, visualStatePending);
        Assert.AreEqual(1, visualTransitionsCommitted);
    }

    [Test]
    public void VisibilityChangeCollectionUsesCachedLookups()
    {
        using var world = new World(nameof(VisibilityChangeCollectionUsesCachedLookups));
        EntityManager em = world.EntityManager;
        Entity root = em.CreateEntity();
        DynamicBuffer<Child> rootChildren = em.AddBuffer<Child>(root);
        Entity hiddenChild = em.CreateEntity(typeof(Disabled), typeof(UnitRenderBudgetCulledTag));
        Entity visibleChild = em.CreateEntity();
        rootChildren.Add(new Child { Value = hiddenChild });
        rootChildren.Add(new Child { Value = visibleChild });
        UnitRenderBudgetTestLookupSystem lookupSystem = world.GetOrCreateSystemManaged<UnitRenderBudgetTestLookupSystem>();
        BufferLookup<Child> childLookup = lookupSystem.GetChildLookup();
        EntityStorageInfoLookup storageLookup = lookupSystem.GetEntityStorageInfoLookupForTests();
        var disabledLookup = lookupSystem.GetDisabledLookup();
        var disableRenderingLookup = lookupSystem.GetDisableRenderingLookup();
        var culledTagLookup = lookupSystem.GetCulledTagLookup();
        using var entitiesToShow = new NativeList<Entity>(Allocator.Temp);
        using var entitiesToHide = new NativeList<Entity>(Allocator.Temp);
        int changed = 0;
        var system = new UnitRenderBudgetVisibilityChange();

        system.CollectRenderVisibilityChanges(
            root,
            visible: true,
            childLookup,
            storageLookup,
            disabledLookup,
            disableRenderingLookup,
            culledTagLookup,
            entitiesToShow,
            entitiesToHide,
            ref changed);
        system.CollectRenderVisibilityChangesRecursive(
            visibleChild,
            visible: false,
            childLookup,
            storageLookup,
            disabledLookup,
            disableRenderingLookup,
            culledTagLookup,
            entitiesToShow,
            entitiesToHide,
            ref changed);

        Assert.AreEqual(2, changed);
        Assert.AreEqual(hiddenChild, entitiesToShow[0]);
        Assert.AreEqual(visibleChild, entitiesToHide[0]);
    }

    [Test]
    public void VisibilityApplyAddsAndRemovesRenderTags()
    {
        using var world = new World(nameof(VisibilityApplyAddsAndRemovesRenderTags));
        EntityManager em = world.EntityManager;
        Entity detailedUnit = em.CreateEntity(typeof(UnitRenderBudgetCulledUnitTag));
        Entity farUnit = em.CreateEntity();
        Entity entityToShow = em.CreateEntity(
            typeof(Disabled),
            typeof(DisableRendering),
            typeof(UnitRenderBudgetCulledTag));
        Entity entityToHide = em.CreateEntity();

        using var unitsToShowDetailed = new NativeList<Entity>(Allocator.Temp);
        using var unitsToShowFarImpostor = new NativeList<Entity>(Allocator.Temp);
        using var entitiesToShow = new NativeList<Entity>(Allocator.Temp);
        using var entitiesToHide = new NativeList<Entity>(Allocator.Temp);
        unitsToShowDetailed.Add(detailedUnit);
        unitsToShowFarImpostor.Add(farUnit);
        entitiesToShow.Add(entityToShow);
        entitiesToHide.Add(entityToHide);

        UnitRenderBudgetVisibilityApply.Result result = new UnitRenderBudgetVisibilityApply().Apply(
            em,
            new EntityCommandBuffer(Allocator.Temp),
            unitsToShowDetailed,
            unitsToShowFarImpostor,
            entitiesToShow,
            entitiesToHide);

        Assert.AreEqual(1, result.Shown);
        Assert.AreEqual(1, result.Hidden);
        Assert.IsFalse(em.HasComponent<UnitRenderBudgetCulledUnitTag>(detailedUnit));
        Assert.IsTrue(em.HasComponent<UnitRenderBudgetCulledUnitTag>(farUnit));
        Assert.IsFalse(em.HasComponent<Disabled>(entityToShow));
        Assert.IsFalse(em.HasComponent<DisableRendering>(entityToShow));
        Assert.IsFalse(em.HasComponent<UnitRenderBudgetCulledTag>(entityToShow));
        Assert.IsTrue(em.HasComponent<DisableRendering>(entityToHide));
        Assert.IsTrue(em.HasComponent<UnitRenderBudgetCulledTag>(entityToHide));
    }

    [Test]
    public void VisibilityApplyUsesCachedLookups()
    {
        using var world = new World(nameof(VisibilityApplyUsesCachedLookups));
        EntityManager em = world.EntityManager;
        Entity detailedUnit = em.CreateEntity(typeof(UnitRenderBudgetCulledUnitTag));
        Entity farUnit = em.CreateEntity();
        Entity entityToShow = em.CreateEntity(
            typeof(Disabled),
            typeof(DisableRendering),
            typeof(UnitRenderBudgetCulledTag));
        Entity entityToHide = em.CreateEntity();
        UnitRenderBudgetTestLookupSystem lookupSystem = world.GetOrCreateSystemManaged<UnitRenderBudgetTestLookupSystem>();

        using var unitsToShowDetailed = new NativeList<Entity>(Allocator.Temp);
        using var unitsToShowFarImpostor = new NativeList<Entity>(Allocator.Temp);
        using var entitiesToShow = new NativeList<Entity>(Allocator.Temp);
        using var entitiesToHide = new NativeList<Entity>(Allocator.Temp);
        unitsToShowDetailed.Add(detailedUnit);
        unitsToShowFarImpostor.Add(farUnit);
        entitiesToShow.Add(entityToShow);
        entitiesToHide.Add(entityToHide);

        UnitRenderBudgetVisibilityApply.Result result = new UnitRenderBudgetVisibilityApply().Apply(
            em,
            new EntityCommandBuffer(Allocator.Temp),
            unitsToShowDetailed,
            unitsToShowFarImpostor,
            entitiesToShow,
            entitiesToHide,
            lookupSystem.GetVisibilityApplyLookups());

        Assert.AreEqual(1, result.Shown);
        Assert.AreEqual(1, result.Hidden);
        Assert.IsFalse(em.HasComponent<UnitRenderBudgetCulledUnitTag>(detailedUnit));
        Assert.IsTrue(em.HasComponent<UnitRenderBudgetCulledUnitTag>(farUnit));
        Assert.IsFalse(em.HasComponent<Disabled>(entityToShow));
        Assert.IsFalse(em.HasComponent<DisableRendering>(entityToShow));
        Assert.IsFalse(em.HasComponent<UnitRenderBudgetCulledTag>(entityToShow));
        Assert.IsTrue(em.HasComponent<DisableRendering>(entityToHide));
        Assert.IsTrue(em.HasComponent<UnitRenderBudgetCulledTag>(entityToHide));
    }

    [Test]
    public void ReadinessSystemAddsReadyTagForRenderableVisual()
    {
        using var world = new World(nameof(ReadinessSystemAddsReadyTagForRenderableVisual));
        EntityManager em = world.EntityManager;
        Entity root = CreateRenderableEntity(em, new float3(1f));
        UnitRenderBudgetTestLookupSystem lookupSystem = world.GetOrCreateSystemManaged<UnitRenderBudgetTestLookupSystem>();
        BufferLookup<Child> childLookup = lookupSystem.GetChildLookup();
        using var readyTaggedThisFrame = new NativeHashSet<Entity>(1, Allocator.Temp);
        var ecb = new EntityCommandBuffer(Allocator.Temp);
        try
        {
            bool ready = new UnitRenderBudgetReadiness().IsVisualReadyForExclusiveDisplay(
                em,
                ecb,
                readyTaggedThisFrame,
                root,
                childLookup,
                new UnitRenderBudgetAnimationReadiness(),
                new UnitRenderBudgetRenderableState());

            Assert.IsTrue(ready);
            Assert.IsTrue(readyTaggedThisFrame.Contains(root));
            Assert.IsFalse(em.HasComponent<UnitRenderVisualReadyTag>(root));
            ecb.Playback(em);
            Assert.IsTrue(em.HasComponent<UnitRenderVisualReadyTag>(root));
        }
        finally
        {
            ecb.Dispose();
        }
    }

    [Test]
    public void ReadinessSystemUsesCachedLookups()
    {
        using var world = new World(nameof(ReadinessSystemUsesCachedLookups));
        EntityManager em = world.EntityManager;
        Entity root = CreateRenderableEntity(em, new float3(1f));
        UnitRenderBudgetTestLookupSystem lookupSystem = world.GetOrCreateSystemManaged<UnitRenderBudgetTestLookupSystem>();
        BufferLookup<Child> childLookup = lookupSystem.GetChildLookup();
        using var readyTaggedThisFrame = new NativeHashSet<Entity>(1, Allocator.Temp);
        var ecb = new EntityCommandBuffer(Allocator.Temp);
        try
        {
            bool ready = new UnitRenderBudgetReadiness().IsVisualReadyForExclusiveDisplay(
                ecb,
                readyTaggedThisFrame,
                root,
                childLookup,
                new UnitRenderBudgetAnimationReadiness(),
                new UnitRenderBudgetRenderableState(),
                lookupSystem.GetReadinessLookups(),
                default,
                lookupSystem.GetRenderableQueryLookups());

            Assert.IsTrue(ready);
            Assert.IsTrue(readyTaggedThisFrame.Contains(root));
            Assert.IsFalse(em.HasComponent<UnitRenderVisualReadyTag>(root));
            ecb.Playback(em);
            Assert.IsTrue(em.HasComponent<UnitRenderVisualReadyTag>(root));
        }
        finally
        {
            ecb.Dispose();
        }
    }

    [Test]
    public void RenderSafetyPatchesBoundsAndAddsSafetyTag()
    {
        using var world = new World(nameof(RenderSafetyPatchesBoundsAndAddsSafetyTag));
        EntityManager em = world.EntityManager;
        Entity root = CreateRenderableEntity(em, new float3(1f, 2f, 3f));
        UnitRenderBudgetTestLookupSystem lookupSystem = world.GetOrCreateSystemManaged<UnitRenderBudgetTestLookupSystem>();
        BufferLookup<Child> childLookup = lookupSystem.GetChildLookup();
        using var safetyTaggedThisFrame = new NativeHashSet<Entity>(1, Allocator.Temp);
        var ecb = new EntityCommandBuffer(Allocator.Temp);
        try
        {
            int patched = new UnitRenderBudgetRenderSafety().EnsureRenderSafetyRecursiveOnce(
                em,
                ecb,
                safetyTaggedThisFrame,
                root,
                childLookup,
                new UnitRenderBudgetLodReferences());

            Assert.AreEqual(1, patched);
            Assert.IsTrue(safetyTaggedThisFrame.Contains(root));
            ecb.Playback(em);
            RenderBounds bounds = em.GetComponentData<RenderBounds>(root);
            Assert.AreEqual(new float3(64f, 64f, 64f), bounds.Value.Extents);
            Assert.IsTrue(em.HasComponent<UnitRenderSafetyPatchedTag>(root));
        }
        finally
        {
            ecb.Dispose();
        }

        using var secondFrameTagged = new NativeHashSet<Entity>(1, Allocator.Temp);
        var secondEcb = new EntityCommandBuffer(Allocator.Temp);
        try
        {
            int patchedAgain = new UnitRenderBudgetRenderSafety().EnsureRenderSafetyRecursiveOnce(
                em,
                secondEcb,
                secondFrameTagged,
                root,
                childLookup,
                new UnitRenderBudgetLodReferences());

            Assert.AreEqual(0, patchedAgain);
            Assert.IsFalse(secondFrameTagged.Contains(root));
        }
        finally
        {
            secondEcb.Dispose();
        }
    }

    [Test]
    public void RenderSafetyUsesCachedLookups()
    {
        using var world = new World(nameof(RenderSafetyUsesCachedLookups));
        EntityManager em = world.EntityManager;
        Entity root = CreateRenderableEntity(em, new float3(1f, 2f, 3f));
        UnitRenderBudgetTestLookupSystem lookupSystem = world.GetOrCreateSystemManaged<UnitRenderBudgetTestLookupSystem>();
        BufferLookup<Child> childLookup = lookupSystem.GetChildLookup();
        using var safetyTaggedThisFrame = new NativeHashSet<Entity>(1, Allocator.Temp);
        var ecb = new EntityCommandBuffer(Allocator.Temp);
        try
        {
            int patched = new UnitRenderBudgetRenderSafety().EnsureRenderSafetyRecursiveOnce(
                ecb,
                safetyTaggedThisFrame,
                root,
                childLookup,
                lookupSystem.GetRenderSafetyLookups());

            Assert.AreEqual(1, patched);
            Assert.IsTrue(safetyTaggedThisFrame.Contains(root));
            ecb.Playback(em);
            RenderBounds bounds = em.GetComponentData<RenderBounds>(root);
            Assert.AreEqual(new float3(64f, 64f, 64f), bounds.Value.Extents);
            Assert.IsTrue(em.HasComponent<UnitRenderSafetyPatchedTag>(root));
        }
        finally
        {
            ecb.Dispose();
        }
    }

    [Test]
    public void CharacterImpostorsScaleUpAtHighTacticalCameraHeight()
    {
        Assert.AreEqual(1f, UnitImpostorVisualUtility.ResolveCharacterTacticalScale(80f), 0.001f);
        Assert.AreEqual(16f, UnitImpostorVisualUtility.ResolveCharacterTacticalScale(200f), 0.001f);
    }

    [Test]
    public void HighCameraCharacterImpostorsFaceCameraPlane()
    {
        Quaternion cameraRotation = Quaternion.Euler(70f, 35f, 0f);
        Quaternion characterRotation = UnitImpostorVisualUtility.ResolveBillboardRotation(
            true,
            Vector3.zero,
            new Vector3(0f, 200f, 0f),
            cameraRotation);
        Quaternion vehicleRotation = UnitImpostorVisualUtility.ResolveBillboardRotation(
            false,
            Vector3.zero,
            new Vector3(0f, 200f, 0f),
            cameraRotation);

        Vector3 expectedCharacterForward = -(cameraRotation * Vector3.forward);
        Assert.Less(Vector3.Angle(expectedCharacterForward, characterRotation * Vector3.forward), 0.1f);
        Assert.Less(Vector3.Angle(Vector3.forward, vehicleRotation * Vector3.forward), 0.1f);
    }

    [Test]
    public void SourceKeyPrefixChecksDoNotAllocate()
    {
        FixedString64Bytes character = new("Unit_Chr_Rifleman");
        FixedString64Bytes vehicle = new("Unit_Veh_APC");
        FixedString64Bytes building = new("Building_Airport");

        _ = UnitImpostorVisualUtility.HasUnitPrefix(character);
        _ = UnitImpostorVisualUtility.HasUnitPrefix(vehicle);
        _ = UnitImpostorVisualUtility.HasUnitPrefix(building);
        _ = UnitImpostorVisualUtility.HasCharacterUnitPrefix(character);
        _ = UnitImpostorVisualUtility.HasCharacterUnitPrefix(vehicle);
        _ = UnitImpostorVisualUtility.HasCharacterUnitPrefix(building);

        bool allMatched = false;
        Assert.That(() =>
        {
            bool result = true;
            for (int i = 0; i < 4096; i++)
            {
                result &= UnitImpostorVisualUtility.HasUnitPrefix(character);
                result &= UnitImpostorVisualUtility.HasUnitPrefix(vehicle);
                result &= !UnitImpostorVisualUtility.HasUnitPrefix(building);
                result &= UnitImpostorVisualUtility.HasCharacterUnitPrefix(character);
                result &= !UnitImpostorVisualUtility.HasCharacterUnitPrefix(vehicle);
                result &= !UnitImpostorVisualUtility.HasCharacterUnitPrefix(building);
            }

            allMatched = result;
        }, new NUnit.Framework.Constraints.NotConstraint(
            UnityEngine.TestTools.Constraints.Is.AllocatingGCMemory()));

        Assert.IsTrue(allMatched);
    }

    private static Entity TestEntity(int index)
    {
        return new Entity { Index = index, Version = 1 };
    }

    private static Entity CreateRenderableEntity(EntityManager em, float3 extents)
    {
        Entity entity = em.CreateEntity(typeof(RenderBounds));
        em.SetComponentData(entity, new RenderBounds
        {
            Value = new AABB { Center = float3.zero, Extents = extents }
        });
        return entity;
    }

    [DisableAutoCreation]
    private sealed partial class UnitRenderBudgetTestLookupSystem : SystemBase
    {
        public BufferLookup<Child> GetChildLookup()
        {
            return GetBufferLookup<Child>(true);
        }

        public ComponentLookup<UnitMovementBehavior> GetMovementBehaviorLookup()
        {
            return GetComponentLookup<UnitMovementBehavior>(true);
        }

        public ComponentLookup<UnitAirMovement> GetAirMovementLookup()
        {
            return GetComponentLookup<UnitAirMovement>(true);
        }

        public ComponentLookup<UnitSourcePrefabKey> GetSourcePrefabKeyLookup()
        {
            return GetComponentLookup<UnitSourcePrefabKey>(true);
        }

        public ComponentLookup<Faction> GetFactionLookup()
        {
            return GetComponentLookup<Faction>(true);
        }

        public ComponentLookup<SelectedUnitTag> GetSelectedLookup()
        {
            return GetComponentLookup<SelectedUnitTag>(true);
        }

        public ComponentLookup<MaterialAnimationIndex> GetMaterialAnimationIndexLookup()
        {
            return GetComponentLookup<MaterialAnimationIndex>(true);
        }

        public ComponentLookup<UnitMoveVisualComponent> GetMoveVisualLookup()
        {
            return GetComponentLookup<UnitMoveVisualComponent>(true);
        }

        public ComponentLookup<UnitHealth> GetHealthLookup()
        {
            return GetComponentLookup<UnitHealth>(true);
        }

        public ComponentLookup<UnitRenderBudgetCulledUnitTag> GetCulledUnitLookup()
        {
            return GetComponentLookup<UnitRenderBudgetCulledUnitTag>(true);
        }

        public ComponentLookup<UnitTransportPassenger> GetPassengerLookup()
        {
            return GetComponentLookup<UnitTransportPassenger>(true);
        }

        public ComponentLookup<UnitRenderVisualComponent> GetVisualStateLookup()
        {
            return GetComponentLookup<UnitRenderVisualComponent>(true);
        }

        public UnitRenderBudgetLodReferences.Lookups GetLodReferenceLookups()
        {
            return new UnitRenderBudgetLodReferences.Lookups
            {
                DetailedVisualReferenceLookup = GetComponentLookup<UnitDetailedVisualReference>(true),
                MidLodPrefabReferenceLookup = GetComponentLookup<UnitMidLodPrefabReference>(true),
                MidLodInstanceReferenceLookup = GetComponentLookup<UnitMidLodInstanceReference>(true),
                LowLodPrefabReferenceLookup = GetComponentLookup<UnitLowLodPrefabReference>(true),
                LowLodInstanceReferenceLookup = GetComponentLookup<UnitLowLodInstanceReference>(true)
            };
        }

        public UnitRenderBudgetRenderableState.Lookups GetRenderableQueryLookups()
        {
            EntityQuery renderableEntityQuery = GetEntityQuery(new EntityQueryDesc
            {
                Any = new[]
                {
                    ComponentType.ReadOnly<RenderFilterSettings>(),
                    ComponentType.ReadOnly<RenderBounds>()
                }
            });

            return new UnitRenderBudgetRenderableState.Lookups
            {
                EntityStorageInfoLookup = GetEntityStorageInfoLookup(),
                RenderableEntityMask = renderableEntityQuery.GetEntityQueryMask(),
                DisabledLookup = GetComponentLookup<Disabled>(true),
                DisableRenderingLookup = GetComponentLookup<DisableRendering>(true),
                CulledTagLookup = GetComponentLookup<UnitRenderBudgetCulledTag>(true),
                SafeVisibleCharacterLodLookup = GetComponentLookup<UnitSafeVisibleCharacterLodTag>(true)
            };
        }

        public UnitRenderBudgetReadiness.Lookups GetReadinessLookups()
        {
            return new UnitRenderBudgetReadiness.Lookups
            {
                EntityStorageInfoLookup = GetEntityStorageInfoLookup(),
                VisualReadyLookup = GetComponentLookup<UnitRenderVisualReadyTag>(true)
            };
        }

        public UnitRenderBudgetAnimationReadiness.Lookups GetAnimationReadinessLookups()
        {
            return new UnitRenderBudgetAnimationReadiness.Lookups
            {
                MeshLodLookup = GetComponentLookup<MeshLODComponent>(true),
                MaterialAlphaCompleteLookup = GetComponentLookup<MaterialAlphaCompleteTag>(true),
                HasGpuAnimationMaterialLookups = 1
            };
        }

        public UnitRenderBudgetVisibilityApply.Lookups GetVisibilityApplyLookups()
        {
            return new UnitRenderBudgetVisibilityApply.Lookups
            {
                EntityStorageInfoLookup = GetEntityStorageInfoLookup(),
                CulledUnitLookup = GetComponentLookup<UnitRenderBudgetCulledUnitTag>(true),
                DisabledLookup = GetComponentLookup<Disabled>(true),
                DisableRenderingLookup = GetComponentLookup<DisableRendering>(true),
                CulledTagLookup = GetComponentLookup<UnitRenderBudgetCulledTag>(true)
            };
        }

        public UnitRenderBudgetRenderSafety.Lookups GetRenderSafetyLookups()
        {
            return new UnitRenderBudgetRenderSafety.Lookups
            {
                EntityStorageInfoLookup = GetEntityStorageInfoLookup(),
                SafetyPatchedLookup = GetComponentLookup<UnitRenderSafetyPatchedTag>(true),
                RenderBoundsLookup = GetComponentLookup<RenderBounds>(true),
                MeshLodLookup = GetComponentLookup<MeshLODComponent>(true),
                MeshLodGroupLookup = GetComponentLookup<MeshLODGroupComponent>(true)
            };
        }

        public EntityStorageInfoLookup GetEntityStorageInfoLookupForTests()
        {
            return GetEntityStorageInfoLookup();
        }

        public ComponentLookup<Disabled> GetDisabledLookup()
        {
            return GetComponentLookup<Disabled>(true);
        }

        public ComponentLookup<DisableRendering> GetDisableRenderingLookup()
        {
            return GetComponentLookup<DisableRendering>(true);
        }

        public ComponentLookup<UnitRenderBudgetCulledTag> GetCulledTagLookup()
        {
            return GetComponentLookup<UnitRenderBudgetCulledTag>(true);
        }

        protected override void OnUpdate()
        {
        }
    }

    [DisableAutoCreation]
    private sealed partial class UnitRenderBudgetDistanceTestSystem : SystemBase
    {
        public RuntimeCameraSnapshotComponent Camera;
        public NativeList<UnitRenderBudgetDistance.UnitDistance> Distances;

        protected override void OnUpdate()
        {
            EntityQuery query = GetEntityQuery(
                ComponentType.ReadOnly<UnitHealth>(),
                ComponentType.ReadOnly<LocalTransform>());
            using NativeArray<Entity> units = query.ToEntityArray(Allocator.TempJob);
            using NativeArray<LocalTransform> transforms = query.ToComponentDataArray<LocalTransform>(Allocator.TempJob);
            new UnitRenderBudgetDistance().Collect(
                Camera,
                units,
                transforms,
                Distances,
                GetComponentLookup<UnitTransportPassenger>(true),
                GetEntityStorageInfoLookup(),
                alwaysDetailedDistanceSq: 18f * 18f,
                viewportPadding: 0.35f,
                edgeSafetyMargin: 0.18f);
        }
    }

    private static RuntimeCameraSnapshotComponent CreateCameraSnapshot(Camera camera)
    {
        float4x4 worldToCamera = ToFloat4x4(camera.worldToCameraMatrix);
        float4x4 projection = ToFloat4x4(camera.projectionMatrix);
        return new RuntimeCameraSnapshotComponent
        {
            IsValid = 1,
            Position = camera.transform.position,
            Rotation = camera.transform.rotation,
            WorldToCamera = worldToCamera,
            Projection = projection,
            ViewProjection = math.mul(projection, worldToCamera)
        };
    }

    private static float4x4 ToFloat4x4(Matrix4x4 value)
    {
        return new float4x4(
            new float4(value.m00, value.m10, value.m20, value.m30),
            new float4(value.m01, value.m11, value.m21, value.m31),
            new float4(value.m02, value.m12, value.m22, value.m32),
            new float4(value.m03, value.m13, value.m23, value.m33));
    }
}
