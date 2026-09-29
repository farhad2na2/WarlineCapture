using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using Game.Authoring;
using Game.Components;
using Game.Editor;
using Game.Editor.MapVariants;
using Game.Runtime;
using Game.Rendering;
using NUnit.Framework;
using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class MapVariantPreparationTests
{
    public static void RunFocusedValidation()
    {
        var tests = new MapVariantPreparationTests();
        try
        {
            tests.Identity_IsMapScopedCultureInvariantAndIndependentOfTraversal();
            tests.Surface_UsesBridgeOffsetAndRejectsWaterAndOutsideWithoutWrapping();
            tests.Footprint_RotatesAndConvertsAllCornersExactlyOnce();
            tests.Inventory_SeparatesDressingAndRetainsAllSevenBoundsFailures();
            tests.PrefabAudit_RejectsNestedAndAmbiguousAlternativeBranches();
            tests.Stripping_RequiresExplicitRequestAndRejectsMissingDatabase();
            tests.SavedSlice_BakesIndependentOwnersAndTransitionsTheirCompleteHierarchies();
            tests.SavedSlice_BakesIndependentOwnersAndTransitionsTheirCompleteHierarchies();
            var records = new DenseCityBuildingRecordFactoryTests();
            records.Create_ProducesLinkedStableFiveRecordGroup();
            records.Add_CommitsFactoryGroupAtomically();
            DenseCityBuildingPresentationRealizerTests.RunFocusedValidation();
            var attachments = new DenseCityBuildingAttachmentTransactionTests();
            attachments.TryCommitAndRealize_RetainsOnlyAcceptedAttachment(true, 3);
            attachments.TryCommitAndRealize_RetainsOnlyAcceptedAttachment(false, 2);
            attachments.TryCommitAndRealize_ExceptionRemovesAttachmentBeforeRethrow();
            attachments.TryCommitAndRealize_RejectsUnknownBuildingOwnerBeforeRealization();
            attachments.Context_AllocatesPersistentAttachmentRecordForRegisteredOwner();
            OperationMapBuildingDestructionSystemTests.RunFocusedValidation();
            using (ValidationExit.SuppressProcessExit())
            {
                ValidationExit.ClearLastExitCode();
                OperationMapRenderVirtualizationValidation.RunFocusedValidation();
                Assert.That(ValidationExit.LastExitCode, Is.EqualTo(0), "Virtualization regression suite failed.");
            }
            Debug.Log("[MapPreparationValidation] result=Passed checks=8 existingSourceRegressions=5 scope=InventoryAndBakedSlice");
        }
        catch (Exception e)
        {
            Debug.LogException(e); Debug.LogError("[MapPreparationValidation] result=Failed"); throw;
        }
    }

    [Test]
    public void Identity_IsMapScopedCultureInvariantAndIndependentOfTraversal()
    {
        var matrix = Matrix4x4.TRS(new Vector3(587.701f, -0.06f, 326.548f), Quaternion.Euler(0, 90, 0), Vector3.one);
        string guid = new string('a', 32);
        CultureInfo previous = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("en-US");
            string key = MapVariantPreparationSchema.PlacementKey("RefineryDistrict", guid, matrix);
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("de-DE");
            Assert.That(MapVariantPreparationSchema.PlacementKey("RefineryDistrict", guid, matrix), Is.EqualTo(key));
            _ = MapVariantPreparationSchema.PlacementKey("RefineryDistrict", new string('b', 32), Matrix4x4.identity);
            Assert.That(MapVariantPreparationSchema.PlacementKey("RefineryDistrict", guid, matrix), Is.EqualTo(key));
            Assert.That(MapVariantPreparationSchema.PlacementKey("AshLinePort", guid, matrix), Is.Not.EqualTo(key));
            matrix.m03 += 1;
            Assert.That(MapVariantPreparationSchema.PlacementKey("RefineryDistrict", guid, matrix), Is.Not.EqualTo(key));
        }
        finally { CultureInfo.CurrentCulture = previous; }
    }

    [Test]
    public void Surface_UsesBridgeOffsetAndRejectsWaterAndOutsideWithoutWrapping()
    {
        var b = new MapVariantBuilder("AshLinePort", new Rect(0, 0, 1400, 1200), new Rect(400, 300, 600, 400), 5, 20502);
        try
        {
            b.Height.Apply((x, z, h) => -4f); b.WaterLevel = -1f;
            b.AddBridge(MapVariantRoadKind.Asphalt, 0, new Vector2(660, 505), new Vector2(690, 505));
            Assert.That(MapVariantPreparationSchema.TrySurface(b, new Vector2(680, 505), out float h, out string kind), Is.True);
            Assert.That(h, Is.EqualTo(0.08f).Within(0.00001f)); Assert.That(kind, Is.EqualTo("BridgeDeck"));
            Assert.That(MapVariantPreparationSchema.TrySurface(b, new Vector2(680, 515), out _, out kind), Is.False);
            Assert.That(kind, Is.EqualTo("WaterExcluded"));
            b.AddRoad(MapVariantRoadKind.Asphalt, new Vector2(670, 525), new Vector2(690, 525));
            Assert.That(MapVariantPreparationSchema.TrySurface(b, new Vector2(680, 525), out _, out kind), Is.False);
            Assert.That(kind, Is.EqualTo("WaterExcluded"), "Roads must not override water errors.");
            Assert.That(MapVariantPreparationSchema.TrySurface(b, new Vector2(700, 1100), out _, out kind), Is.False);
            Assert.That(kind, Is.EqualTo("OutsidePlayable"));
            Assert.That(MapVariantPreparationSchema.TrySurface(b, new Vector2(1000, 505), out _, out _), Is.False);
        }
        finally { UnityEngine.Object.DestroyImmediate(b.Root.gameObject); }
    }

    [Test]
    public void Footprint_RotatesAndConvertsAllCornersExactlyOnce()
    {
        Vector3 offset = MapVariantPreparationSchema.Offset("Frontier");
        Vector2[] corners = MapVariantPreparationSchema.Footprint(new Vector2(200, 200), new Vector2(2, 5), 90, offset);
        Assert.That(corners.Min(p => p.x), Is.EqualTo(19f).Within(0.0001f));
        Assert.That(corners.Max(p => p.x), Is.EqualTo(29f).Within(0.0001f));
        Assert.That(corners.Min(p => p.y), Is.EqualTo(22f).Within(0.0001f));
        Assert.That(corners.Max(p => p.y), Is.EqualTo(26f).Within(0.0001f));
        Vector3 source = new(2010, 0, 640);
        Assert.That(source + offset - offset, Is.EqualTo(source));
        Assert.That(new Vector3(176, 0, 176) + offset, Is.EqualTo(Vector3.zero));
    }

    [Test]
    public void Inventory_SeparatesDressingAndRetainsAllSevenBoundsFailures()
    {
        string[] mapIds = { "RefineryDistrict", "CityEdgeAirfield", "AshLinePort", "Frontier" };
        int[] counts = { 11066, 11084, 9492, 25678 };
        string[] expectedFailures = { "SplitFront_Ridge", "Airlift_Helipad", "AirCorridor_Tower", "Anchor_CityGate",
            "RouteReopened_Hub", "SupplyYard_East", "Anchor_EastCheckpoint" };
        var failures = new System.Collections.Generic.List<string>();
        for (int i = 0; i < mapIds.Length; i++)
        {
            var m = JsonUtility.FromJson<MapPreparationManifest>(File.ReadAllText(
                MapVariantPreparationInventory.ReportRoot + "/" + mapIds[i] + "/inventory.json"));
            Assert.That(m.placements.Count, Is.EqualTo(counts[i]));
            Assert.That(m.placements.Select(p => p.stableKey).Distinct().Count(), Is.EqualTo(counts[i]));
            Assert.That(m.placements.Any(p => p.artReserved && p.category == "RenderOnlyProp"), Is.True);
            Assert.That(m.placements.Where(p => p.category == "RenderOnlyProp").All(p => !p.movementBlocked && !p.damageEligible), Is.True);
            Assert.That(m.placements.All(p => !p.targetEligible), Is.True, "Preparation must not grant mission targeting roles.");
            failures.AddRange(m.zones.Where(z => !z.entireFootprintInsidePlayable).Select(z => z.id));
            Assert.That(m.groundVertexHeights.Length, Is.EqualTo((m.heightCellsX + 1) * (m.heightCellsZ + 1)));
            if (mapIds[i] == "AshLinePort")
            {
                foreach (string id in new[] { "Anchor_MainBridge", "Anchor_SouthBridge" })
                    Assert.That(m.zones.Single(z => z.id == id).runtimeCenter.y, Is.EqualTo(0.08f).Within(0.0001f));
            }
        }
        Assert.That(failures, Is.EquivalentTo(expectedFailures));
    }

    [Test]
    public void PrefabAudit_RejectsNestedAndAmbiguousAlternativeBranches()
    {
        const string folder = "Assets/Tests/Editor/MapPreparationTemp";
        MapVariantBuilder.EnsureFolder(folder);
        var go = new GameObject("Nested");
        try
        {
            var wrap = new GameObject("Wrap"); wrap.transform.SetParent(go.transform, false);
            foreach (string name in new[] { "Model", "Destroyed" })
            {
                GameObject child = GameObject.CreatePrimitive(PrimitiveType.Cube);
                child.name = name; child.transform.SetParent(wrap.transform, false);
            }
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(go, folder + "/nested.prefab");
            Assert.That(MapVariantPreparationInventory.InspectPrefab(prefab).hierarchyStatus, Is.EqualTo("UnresolvedNestedAlternatives"));
            wrap.transform.GetChild(0).SetParent(go.transform, false);
            wrap.transform.GetChild(0).SetParent(go.transform, false);
            UnityEngine.Object.DestroyImmediate(wrap);
            var extra = new GameObject("Destroyed"); extra.transform.SetParent(go.transform, false);
            prefab = PrefabUtility.SaveAsPrefabAsset(go, folder + "/ambiguous.prefab");
            Assert.That(MapVariantPreparationInventory.InspectPrefab(prefab).hierarchyStatus, Is.EqualTo("UnresolvedAmbiguousBranches"));
        }
        finally { UnityEngine.Object.DestroyImmediate(go); AssetDatabase.DeleteAsset(folder); }
    }

    [Test]
    public void Stripping_RequiresExplicitRequestAndRejectsMissingDatabase()
    {
        using var world = new World("RefineryResidentStrippingIntentValidation");
        EntityManager em = world.EntityManager;
        Entity owner = em.CreateEntity(typeof(OperationMapVirtualizedBuildingOwnerBakingComponent), typeof(OperationMapBuildingPresentation));
        em.AddBuffer<OperationMapRenderSourceRowBakingComponent>(owner);
        // These same generic source markers must preserve resident geometry.
        Assert.DoesNotThrow(() => UpdateStripping(world));
        Entity request = em.CreateEntity(typeof(OperationMapRenderSourceStrippingRequestBakingComponent));
        Assert.That(() => UpdateStripping(world), Throws.InvalidOperationException.With.Message.Contains("exactly one render database"));
        em.DestroyEntity(request);
        em.AddComponent<OperationMapVirtualizedBuildingPresentationComponent>(owner);
        Assert.That(() => UpdateStripping(world), Throws.InvalidOperationException.With.Message.Contains("resident presentation only"));
    }

    private static void UpdateStripping(World world)
    {
        SystemHandle handle = world.CreateSystem<OperationMapRenderVirtualizationBakingSystem>();
        try
        {
            ref SystemState state = ref world.Unmanaged.ResolveSystemStateRef(handle);
            world.Unmanaged.GetUnsafeSystemRef<OperationMapRenderVirtualizationBakingSystem>(handle).OnUpdate(ref state);
            state.Dependency.Complete();
        }
        finally { world.DestroySystem(handle); }
    }

    [Test]
    public void SavedSlice_BakesIndependentOwnersAndTransitionsTheirCompleteHierarchies()
    {
        Scene scene = EditorSceneManager.OpenScene(MapVariantRefineryPreparationSlice.ScenePath, OpenSceneMode.Additive);
        using var world = new World("RefineryPreparationBakedSliceValidation");
        IDisposable blobs = null;
        try
        {
            GameObject root = scene.GetRootGameObjects().Single(r => r.name == "PreparedEntityPresentation");
            var authorings = root.GetComponentsInChildren<OperationMapBuildingAuthoring>(true);
            Assert.That(authorings.Length, Is.GreaterThanOrEqualTo(2));
            foreach (var a in authorings)
            {
                Assert.That(a.TryValidate(out string error), Is.True, error);
                Assert.That(DenseCityBuildingIntactVisualPolicy.TryValidateNormalized(a.IntactVisualRoot, out error), Is.True, error);
            }
            Bake(world, root, out blobs);
            EntityManager em = world.EntityManager;
            using EntityQuery query = em.CreateEntityQuery(typeof(OperationMapBuildingIdentity), typeof(OperationMapBuildingPresentation));
            using NativeArray<Entity> owners = query.ToEntityArray(Allocator.Temp);
            Assert.That(owners.Length, Is.EqualTo(authorings.Length));
            Assert.That(owners.Select(e => em.GetComponentData<OperationMapBuildingIdentity>(e).StableId.ToString()).Distinct().Count(), Is.EqualTo(owners.Length));
            Update(world);
            foreach (Entity owner in owners) RequireState(em, owner, false);
            Entity first = owners[0];
            int before = em.UniversalQuery.CalculateEntityCount();
            UnitHealth health = em.GetComponentData<UnitHealth>(first); health.Current = 0; em.SetComponentData(first, health);
            Update(world);
            RequireState(em, first, true);
            foreach (Entity owner in owners.Skip(1)) RequireState(em, owner, false);
            Assert.That(em.UniversalQuery.CalculateEntityCount(), Is.EqualTo(before));
            Assert.That(em.HasComponent<StaticGridBlocker>(first), Is.True);
            Assert.That(em.HasComponent<OperationMapVirtualizedBuildingPresentationComponent>(first), Is.False);
            Debug.Log($"[MapPreparationBakedSlice] result=Passed map=RefineryDistrict owners={owners.Length} independentDamage=Passed blockers=Retained reset=FreshBake");
        }
        finally { blobs?.Dispose(); EditorSceneManager.CloseScene(scene, true); }
    }

    internal static void RequireState(EntityManager em, Entity owner, bool destroyed)
    {
        var p = em.GetComponentData<OperationMapBuildingPresentation>(owner);
        Assert.That(p.State, Is.EqualTo(destroyed ? 1 : 0));
        Assert.That(em.GetComponentData<LocalTransform>(p.IntactVisualRoot).Scale, Is.EqualTo(destroyed ? 0 : 1).Within(0.00001f));
        Assert.That(em.GetComponentData<LocalTransform>(p.DestroyedVisualRoot).Scale, Is.EqualTo(destroyed ? 1 : 0).Within(0.00001f));
        Assert.That(em.IsComponentEnabled<OperationMapBuildingDestroyedComponent>(owner), Is.EqualTo(destroyed));
        Assert.That(em.GetComponentData<OperationMapBuildingComponent>(owner).BlockerPolicy,
            Is.EqualTo(OperationMapBuildingBlockerPolicy.RubbleRemainsBlocked));
    }

    internal static void Update(World world)
    {
        SystemHandle handle = world.CreateSystem<OperationMapBuildingDestructionSystem>();
        ref SystemState state = ref world.Unmanaged.ResolveSystemStateRef(handle);
        world.Unmanaged.GetUnsafeSystemRef<OperationMapBuildingDestructionSystem>(handle).OnUpdate(ref state);
        state.Dependency.Complete(); world.EntityManager.CompleteAllTrackedJobs(); world.DestroySystem(handle);
    }

    internal static void Bake(World world, GameObject root, out IDisposable lifetime)
    {
        Type utility = Type.GetType("Unity.Entities.BakingUtility, Unity.Entities.Hybrid", true);
        Type settingsType = Type.GetType("Unity.Entities.BakingSettings, Unity.Entities.Hybrid", true);
        Type storeType = Type.GetType("Unity.Entities.BlobAssetStore, Unity.Entities") ?? Type.GetType("Unity.Entities.BlobAssetStore, Unity.Entities.Hybrid", true);
        object store = Activator.CreateInstance(storeType, 128); lifetime = (IDisposable)store;
        object settings = Activator.CreateInstance(settingsType);
        settingsType.GetField("BakingFlags")?.SetValue(settings, Enum.Parse(utility.GetNestedType("BakingFlags"), "AssignName"));
        settingsType.GetProperty("BlobAssetStore")?.SetValue(settings, store);
        var errors = new System.Collections.Generic.List<string>();
        void Log(string message, string trace, LogType type)
        {
            if (type is LogType.Error or LogType.Exception or LogType.Assert) errors.Add(message);
        }
        Application.logMessageReceived += Log;
        try
        {
            utility.GetMethod("BakeGameObjects", BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public)
                .Invoke(null, new object[] { world, new[] { root }, settings });
            Assert.That(errors, Is.Empty, "Baking logged errors: " + string.Join("; ", errors));
        }
        finally { Application.logMessageReceived -= Log; }
    }
}
