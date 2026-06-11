using NUnit.Framework;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;
using Unity.Transforms;
using UnityEngine;

public sealed partial class UnitRenderBudgetSystemTests
{
    public static void RunFocusedValidation()
    {
        try
        {
            var tests = new UnitRenderBudgetSystemTests();
            tests.BudgetBandsRespectDetailedMidAndLowCaps();
            tests.MovingVisibleCharactersUseDetailedModelPath();
            tests.MovingVisibleCharactersFallbackToDetailWhenMeshLodIsNotAnimatable();
            tests.IdleDistantVisibleCharactersStayOnDetailedModelPath();
            tests.CharacterRenderPolicyForcesDetailedModelPath();
            tests.UnselectedEnemyBeyondImpostorThresholdUsesFarVisual();
            tests.MissingMeshLodInstanceKeepsDetailVisibleUntilReady();
            tests.VisualStateTransitionKeepsCurrentVisualUntilStableOrForced();
            tests.VisibilityApplyAddsAndRemovesRenderTags();
            tests.ReadinessSystemAddsReadyTagForRenderableVisual();
            tests.RenderSafetyPatchesBoundsAndAddsSafetyTag();
            tests.CharacterImpostorsScaleUpAtHighTacticalCameraHeight();
            tests.HighCameraCharacterImpostorsFaceCameraPlane();
            tests.CharacterSourceKeyPrefixCheckDoesNotAllocate();
            Debug.Log("[UnitRenderBudgetFocusedValidation] result=Passed tests=14");
        }
        catch (System.Exception ex)
        {
            Debug.LogException(ex);
            Debug.LogError("[UnitRenderBudgetFocusedValidation] result=Failed");
            throw;
        }
    }

    [Test]
    public void BudgetBandsRespectDetailedMidAndLowCaps()
    {
        var distances = new NativeList<UnitRenderBudgetDistanceSystem.UnitDistance>(Allocator.Temp);
        try
        {
            for (int i = 0; i < 7; i++)
            {
                distances.Add(new UnitRenderBudgetDistanceSystem.UnitDistance
                {
                    Unit = TestEntity(i + 1),
                    DistanceSq = i < 2 ? 9f : 100f,
                    Priority = (byte)i,
                    Visible = 1
                });
            }

            UnitRenderBudgetBandSystem.Plan plan = new UnitRenderBudgetBandSystem().Create(
                distances,
                maxDetailedUnits: 2,
                maxMidLodUnits: 2,
                maxLowLodUnits: 2,
                alwaysDetailedDistanceSq: 25f,
                Allocator.Temp);
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
    public void MovingVisibleCharactersUseDetailedModelPath()
    {
        var policy = new UnitRenderBudgetCharacterPolicySystem();
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

        Assert.AreEqual(UnitRenderVisualKind.Detail, visual);
    }

    [Test]
    public void MovingVisibleCharactersFallbackToDetailWhenMeshLodIsNotAnimatable()
    {
        var policy = new UnitRenderBudgetCharacterPolicySystem();
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
    public void IdleDistantVisibleCharactersStayOnDetailedModelPath()
    {
        var policy = new UnitRenderBudgetCharacterPolicySystem();
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

        Assert.AreEqual(UnitRenderVisualKind.Detail, visual);
    }

    [Test]
    public void CharacterRenderPolicyForcesDetailedModelPath()
    {
        var policy = new UnitRenderBudgetCharacterPolicySystem();
        Assert.IsTrue(policy.ShouldForceCharacterDetailVisual(true));
        Assert.IsFalse(policy.ShouldForceCharacterDetailVisual(false));
    }

    [Test]
    public void UnselectedEnemyBeyondImpostorThresholdUsesFarVisual()
    {
        using var world = new World(nameof(UnselectedEnemyBeyondImpostorThresholdUsesFarVisual));
        using var ecb = new EntityCommandBuffer(Allocator.Temp);
        using var readyTaggedThisFrame = new NativeHashSet<Entity>(1, Allocator.Temp);

        UnitRenderBudgetVisualPlanSystem.Result result = new UnitRenderBudgetVisualPlanSystem().CreateDesiredVisualPlan(
            world.EntityManager,
            ecb,
            readyTaggedThisFrame,
            default,
            new UnitRenderBudgetVisualPlanSystem.Request
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
                MidBand = true,
                LowBand = true
            },
            new UnitRenderBudgetCharacterPolicySystem(),
            new UnitRenderBudgetReadinessSystem(),
            new UnitRenderBudgetAnimationReadinessSystem(),
            new UnitRenderBudgetRenderableQuerySystem());

        Assert.AreEqual(UnitRenderVisualKind.Far, result.DesiredVisual);
        Assert.IsTrue(result.ShouldShowFar);
        Assert.IsFalse(result.ShouldShowDetail);
        Assert.IsFalse(result.ShouldShowMid);
        Assert.IsFalse(result.ShouldShowLow);
    }

    [Test]
    public void MissingMeshLodInstanceKeepsDetailVisibleUntilReady()
    {
        using var world = new World(nameof(MissingMeshLodInstanceKeepsDetailVisibleUntilReady));
        using var ecb = new EntityCommandBuffer(Allocator.Temp);
        using var readyTaggedThisFrame = new NativeHashSet<Entity>(1, Allocator.Temp);

        UnitRenderBudgetVisualPlanSystem.Result result = new UnitRenderBudgetVisualPlanSystem().CreateDesiredVisualPlan(
            world.EntityManager,
            ecb,
            readyTaggedThisFrame,
            default,
            new UnitRenderBudgetVisualPlanSystem.Request
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
            new UnitRenderBudgetCharacterPolicySystem(),
            new UnitRenderBudgetReadinessSystem(),
            new UnitRenderBudgetAnimationReadinessSystem(),
            new UnitRenderBudgetRenderableQuerySystem());

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
        var visualStateSystem = new UnitRenderBudgetVisualStateSystem();

        int visualStateChanges = 0;
        int visualStatePending = 0;
        int visualTransitionsCommitted = 0;
        using (var ecb = new EntityCommandBuffer(Allocator.Temp))
        {
            UnitRenderVisualKind initialVisual = visualStateSystem.ResolveStableUnitRenderVisualState(
                em,
                ecb,
                unit,
                UnitRenderVisualKind.Mid,
                forceImmediate: false,
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
                em,
                ecb,
                unit,
                UnitRenderVisualKind.Low,
                forceImmediate: false,
                ref visualStateChanges,
                ref visualStatePending,
                ref visualTransitionsCommitted);

            Assert.AreEqual(UnitRenderVisualKind.Mid, pendingVisual);
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
                em,
                ecb,
                unit,
                UnitRenderVisualKind.Detail,
                forceImmediate: true,
                ref visualStateChanges,
                ref visualStatePending,
                ref visualTransitionsCommitted);

            Assert.AreEqual(UnitRenderVisualKind.Detail, forcedVisual);
        }

        state = em.GetComponentData<UnitRenderVisualComponent>(unit);
        Assert.AreEqual((byte)UnitRenderVisualKind.Detail, state.Current);
        Assert.AreEqual((byte)UnitRenderVisualKind.Detail, state.Desired);
        Assert.AreEqual(1, visualStateChanges);
        Assert.AreEqual(0, visualStatePending);
        Assert.AreEqual(1, visualTransitionsCommitted);
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

        UnitRenderBudgetVisibilityApplySystem.Result result = new UnitRenderBudgetVisibilityApplySystem().Apply(
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
            bool ready = new UnitRenderBudgetReadinessSystem().IsVisualReadyForExclusiveDisplay(
                em,
                ecb,
                readyTaggedThisFrame,
                root,
                childLookup,
                new UnitRenderBudgetAnimationReadinessSystem(),
                new UnitRenderBudgetRenderableQuerySystem());

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
            int patched = new UnitRenderBudgetRenderSafetySystem().EnsureRenderSafetyRecursiveOnce(
                em,
                ecb,
                safetyTaggedThisFrame,
                root,
                childLookup,
                new UnitRenderBudgetLodReferenceSystem());

            Assert.AreEqual(1, patched);
            Assert.IsTrue(safetyTaggedThisFrame.Contains(root));
            RenderBounds bounds = em.GetComponentData<RenderBounds>(root);
            Assert.AreEqual(new float3(64f, 64f, 64f), bounds.Value.Extents);
            ecb.Playback(em);
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
            int patchedAgain = new UnitRenderBudgetRenderSafetySystem().EnsureRenderSafetyRecursiveOnce(
                em,
                secondEcb,
                secondFrameTagged,
                root,
                childLookup,
                new UnitRenderBudgetLodReferenceSystem());

            Assert.AreEqual(0, patchedAgain);
            Assert.IsFalse(secondFrameTagged.Contains(root));
        }
        finally
        {
            secondEcb.Dispose();
        }
    }

    [Test]
    public void CharacterImpostorsScaleUpAtHighTacticalCameraHeight()
    {
        Assert.AreEqual(1f, UnitImpostorRenderSystem.ResolveCharacterTacticalScale(80f), 0.001f);
        Assert.AreEqual(16f, UnitImpostorRenderSystem.ResolveCharacterTacticalScale(200f), 0.001f);
    }

    [Test]
    public void HighCameraCharacterImpostorsFaceCameraPlane()
    {
        Quaternion cameraRotation = Quaternion.Euler(70f, 35f, 0f);
        Quaternion characterRotation = UnitImpostorRenderSystem.ResolveBillboardRotation(
            true,
            Vector3.zero,
            new Vector3(0f, 200f, 0f),
            cameraRotation);
        Quaternion vehicleRotation = UnitImpostorRenderSystem.ResolveBillboardRotation(
            false,
            Vector3.zero,
            new Vector3(0f, 200f, 0f),
            cameraRotation);

        Vector3 expectedCharacterForward = -(cameraRotation * Vector3.forward);
        Assert.Less(Vector3.Angle(expectedCharacterForward, characterRotation * Vector3.forward), 0.1f);
        Assert.Less(Vector3.Angle(Vector3.forward, vehicleRotation * Vector3.forward), 0.1f);
    }

    [Test]
    public void CharacterSourceKeyPrefixCheckDoesNotAllocate()
    {
        FixedString64Bytes character = new("Unit_Chr_Rifleman");
        FixedString64Bytes vehicle = new("Unit_Veh_APC");

        _ = UnitImpostorRenderSystem.HasCharacterUnitPrefix(character);
        _ = UnitImpostorRenderSystem.HasCharacterUnitPrefix(vehicle);

        bool allMatched = false;
        Assert.That(() =>
        {
            bool result = true;
            for (int i = 0; i < 4096; i++)
            {
                result &= UnitImpostorRenderSystem.HasCharacterUnitPrefix(character);
                result &= !UnitImpostorRenderSystem.HasCharacterUnitPrefix(vehicle);
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

    private sealed partial class UnitRenderBudgetTestLookupSystem : SystemBase
    {
        public BufferLookup<Child> GetChildLookup()
        {
            return GetBufferLookup<Child>(true);
        }

        protected override void OnUpdate()
        {
        }
    }
}
