#if UNITY_INCLUDE_TESTS && UNITY_EDITOR
using System;
using Unity.Core;
using NUnit.Framework;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Graphics;
using Unity.Mathematics;
using Unity.Rendering;
using UnityEditor;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.Rendering;
using SnivelerCode.GpuAnimation.Scripts.Components;

public sealed class VehicleVisualAdornmentsSystemTests
{
    private const float ExpectedVehicleFallbackMarkerScaleX = 3.024194f;
    private const float ExpectedVehicleFallbackMarkerScaleZ = 3.04878f;
    private const float ExpectedVehicleMeshMarkerScaleX = 1.306452f;
    private const float ExpectedVehicleMeshMarkerScaleZ = 1.97561f;

    public static void RunFocusedValidation()
    {
        try
        {
            var tests = new VehicleVisualAdornmentsSystemTests();
            tests.VehicleVisualPrefabReferenceBackfillCopiesMarkerReferenceFromSourcePrefab();
            tests.VehicleVisualPrefabReferenceBackfillUsesSharedMarkerWhenSourcePrefabIsStale();
            tests.UnitVisualPrefabReferenceBackfillCopiesMarkerAndHealthReferencesForCharacterUnit();
            tests.UnitSelectionMarkerSystemCreatesAndRetainsMarkersPerSelectedVehicle();
            tests.UnitSelectionMarkerSystemSizesVehicleMarkerFromMeshBoundsWhenAvailable();
            tests.UnitSelectionMarkerSystemCreatesMarkerForSelectedCharacterUnit();
            tests.UnitSelectionMarkerSystemSplitsReferenceMarkerPrefabForVehiclesAndInfantry();
            tests.UnitSelectionMarkerSystemCreatesEcsObjectOutlinesForSelectedVehicleAndCharacterRenderChildren();
            tests.UnitSelectionMarkerSystemKeepsAirVehicleObjectOutlineWhileGroundMarkerIsHidden();
            tests.UnitSelectionMarkerSystemOutlinesReferencedAirVehicleVisualRoot();
            tests.UnitSelectionMarkerSystemCreatesSafeSelectionVolumeForGpuAnimatedCharacterWithoutBindPoseOverlay();
            tests.UnitSelectionMarkerSystemHidesMarkersForTransportedCharactersButKeepsCulledSelectedCharactersVisible();
            tests.SelectionMarkerVisibilitySystemTogglesVisualChildScaleFromSelectionState();
            tests.UnitFactionTintTargetBackfillIgnoresSelectionObjectOutlines();
            tests.UnitRuntimeHealthBarSystemCreatesAndRetainsRuntimeHealthBar();
            tests.UnitRuntimeHealthBarSystemCreatesHealthBarForDamagedCharacterUnit();
            tests.UnitRuntimeHealthBarSystemRetainsAndHidesBarsForTransportedOrImpostorOnlyCharacters();
            tests.UnitDestroyedVisualSystemInitializesAliveAndDestroyedChildScales();
            tests.UnitHealthBarSystemExpiresRecentDamageVisibilityWithEcb();
            Debug.Log("[VehicleVisualAdornmentsFocusedValidation] result=Passed tests=19");
            EditorApplication.Exit(0);
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            Debug.LogError("[VehicleVisualAdornmentsFocusedValidation] result=Failed");
            EditorApplication.Exit(1);
        }
    }

    [Test]
    public void VehicleVisualPrefabReferenceBackfillCopiesMarkerReferenceFromSourcePrefab()
    {
        using var world = new World(nameof(VehicleVisualPrefabReferenceBackfillCopiesMarkerReferenceFromSourcePrefab));
        EntityManager em = world.EntityManager;
        Entity markerPrefab = CreateVisualPrefab(em);
        Entity healthBarPrefab = CreateHealthBarPrefab(em);
        Entity destroyedVisualPrefab = CreateVisualPrefab(em);
        Entity sourcePrefab = CreateVehiclePrefab(em, "Unit_Veh_Tank_USA");
        em.AddComponentData(sourcePrefab, new UnitSelectionMarkerPrefabReference { Prefab = markerPrefab });
        em.AddComponentData(sourcePrefab, new UnitHealthBarPrefabReference { Prefab = healthBarPrefab });
        em.AddComponentData(sourcePrefab, new VehicleDestroyedVisualPrefabReference { Prefab = destroyedVisualPrefab });

        Entity liveVehicle = CreateVehicle(em, health: 100);
        em.AddComponentData(liveVehicle, new UnitSourcePrefabKey { Value = new FixedString64Bytes("Unit_Veh_Tank_USA") });
        em.AddComponent<SelectedUnitTag>(liveVehicle);

        SystemHandle backfillSystem = world.CreateSystem<UnitVisualPrefabReferenceBackfillSystem>();
        backfillSystem.Update(world.Unmanaged);

        Assert.IsTrue(em.HasComponent<UnitSelectionMarkerPrefabReference>(liveVehicle));
        Assert.AreEqual(markerPrefab, em.GetComponentData<UnitSelectionMarkerPrefabReference>(liveVehicle).Prefab);
        Assert.IsTrue(em.HasComponent<UnitHealthBarPrefabReference>(liveVehicle));
        Assert.AreEqual(healthBarPrefab, em.GetComponentData<UnitHealthBarPrefabReference>(liveVehicle).Prefab);
        Assert.IsTrue(em.HasComponent<VehicleDestroyedVisualPrefabReference>(liveVehicle));
        Assert.AreEqual(destroyedVisualPrefab, em.GetComponentData<VehicleDestroyedVisualPrefabReference>(liveVehicle).Prefab);
        Assert.IsTrue(em.HasComponent<UnitVisualPrefabReferencesBackfilledTag>(liveVehicle));

        SystemHandle markerSystem = world.CreateSystem<UnitSelectionMarkerSystem>();
        markerSystem.Update(world.Unmanaged);

        Assert.IsTrue(em.HasComponent<UnitSelectionMarkerInstanceReference>(liveVehicle));
    }

    [Test]
    public void VehicleVisualPrefabReferenceBackfillUsesSharedMarkerWhenSourcePrefabIsStale()
    {
        using var world = new World(nameof(VehicleVisualPrefabReferenceBackfillUsesSharedMarkerWhenSourcePrefabIsStale));
        EntityManager em = world.EntityManager;
        Entity markerPrefab = CreateVisualPrefab(em);
        Entity sharedVehiclePrefab = CreateVehiclePrefab(em, "Unit_Veh");
        Entity staleSourcePrefab = CreateVehiclePrefab(em, "Unit_Veh_Helicopter_Attack");
        em.AddComponentData(sharedVehiclePrefab, new UnitSelectionMarkerPrefabReference { Prefab = markerPrefab });

        Entity liveVehicle = CreateVehicle(em, health: 100);
        em.AddComponentData(liveVehicle, new UnitSourcePrefabKey { Value = new FixedString64Bytes("Unit_Veh_Helicopter_Attack") });
        em.AddComponent<SelectedUnitTag>(liveVehicle);

        SystemHandle backfillSystem = world.CreateSystem<UnitVisualPrefabReferenceBackfillSystem>();
        backfillSystem.Update(world.Unmanaged);

        Assert.IsTrue(em.HasComponent<UnitSelectionMarkerPrefabReference>(liveVehicle));
        Assert.AreEqual(markerPrefab, em.GetComponentData<UnitSelectionMarkerPrefabReference>(liveVehicle).Prefab);
        Assert.IsTrue(em.HasComponent<UnitVisualPrefabReferencesBackfilledTag>(liveVehicle));

        SystemHandle markerSystem = world.CreateSystem<UnitSelectionMarkerSystem>();
        markerSystem.Update(world.Unmanaged);

        Assert.IsTrue(em.HasComponent<UnitSelectionMarkerInstanceReference>(liveVehicle));
        Assert.IsFalse(em.HasComponent<VehicleDestroyedVisualPrefabReference>(liveVehicle), "Destroyed visuals must stay source-specific, not use the shared marker fallback.");
        Assert.IsTrue(em.Exists(staleSourcePrefab));
    }

    [Test]
    public void UnitVisualPrefabReferenceBackfillCopiesMarkerAndHealthReferencesForCharacterUnit()
    {
        using var world = new World(nameof(UnitVisualPrefabReferenceBackfillCopiesMarkerAndHealthReferencesForCharacterUnit));
        EntityManager em = world.EntityManager;
        Entity markerPrefab = CreateVisualPrefab(em);
        Entity healthBarPrefab = CreateHealthBarPrefab(em);
        Entity sourcePrefab = CreateCharacterPrefab(em, "Unit_Rifleman");
        em.AddComponentData(sourcePrefab, new UnitSelectionMarkerPrefabReference { Prefab = markerPrefab });
        em.AddComponentData(sourcePrefab, new UnitHealthBarPrefabReference { Prefab = healthBarPrefab });

        Entity liveCharacter = CreateCharacter(em, health: 100);
        em.AddComponentData(liveCharacter, new UnitSourcePrefabKey { Value = new FixedString64Bytes("Unit_Rifleman") });

        SystemHandle backfillSystem = world.CreateSystem<UnitVisualPrefabReferenceBackfillSystem>();
        backfillSystem.Update(world.Unmanaged);

        Assert.IsTrue(em.HasComponent<UnitSelectionMarkerPrefabReference>(liveCharacter));
        Assert.AreEqual(markerPrefab, em.GetComponentData<UnitSelectionMarkerPrefabReference>(liveCharacter).Prefab);
        Assert.IsTrue(em.HasComponent<UnitHealthBarPrefabReference>(liveCharacter));
        Assert.AreEqual(healthBarPrefab, em.GetComponentData<UnitHealthBarPrefabReference>(liveCharacter).Prefab);
        Assert.IsFalse(em.HasComponent<VehicleDestroyedVisualPrefabReference>(liveCharacter));
        Assert.IsTrue(em.HasComponent<UnitVisualPrefabReferencesBackfilledTag>(liveCharacter));
    }

    [Test]
    public void UnitSelectionMarkerSystemCreatesAndRetainsMarkersPerSelectedVehicle()
    {
        using var world = new World(nameof(UnitSelectionMarkerSystemCreatesAndRetainsMarkersPerSelectedVehicle));
        EntityManager em = world.EntityManager;
        Entity markerPrefab = CreateVisualPrefab(em);
        Entity firstVehicle = CreateVehicle(em, health: 100);
        Entity secondVehicle = CreateVehicle(em, health: 100);
        em.AddComponentData(firstVehicle, new UnitSelectionMarkerPrefabReference { Prefab = markerPrefab });
        em.AddComponentData(secondVehicle, new UnitSelectionMarkerPrefabReference { Prefab = markerPrefab });
        em.AddComponent<SelectedUnitTag>(firstVehicle);
        em.AddComponent<SelectedUnitTag>(secondVehicle);

        SystemHandle system = world.CreateSystem<UnitSelectionMarkerSystem>();
        SystemHandle visibilitySystem = world.CreateSystem<SelectionMarkerVisibilitySystem>();
        system.Update(world.Unmanaged);

        Assert.IsTrue(em.HasComponent<UnitSelectionMarkerInstanceReference>(firstVehicle));
        Assert.IsTrue(em.HasComponent<UnitSelectionMarkerInstanceReference>(secondVehicle));
        Entity firstMarker = em.GetComponentData<UnitSelectionMarkerInstanceReference>(firstVehicle).Instance;
        Entity secondMarker = em.GetComponentData<UnitSelectionMarkerInstanceReference>(secondVehicle).Instance;
        Assert.AreNotEqual(firstMarker, secondMarker, "Each selected vehicle needs its own marker instance.");
        Assert.AreEqual(firstVehicle, em.GetComponentData<Parent>(firstMarker).Value);
        Assert.AreEqual(secondVehicle, em.GetComponentData<Parent>(secondMarker).Value);
        Assert.AreEqual(1f, em.GetComponentData<LocalTransform>(firstMarker).Scale, 0.001f);
        Assert.AreEqual(0.12f, em.GetComponentData<LocalTransform>(firstMarker).Position.y, 0.001f);
        Assert.IsTrue(em.HasComponent<SelectionMarkerTag>(firstMarker));
        SelectionMarkerVisualChild firstVisualChild = em.GetComponentData<SelectionMarkerVisualChild>(firstMarker);
        Assert.AreEqual(ExpectedVehicleFallbackMarkerScaleZ, firstVisualChild.VisibleScale, 0.001f);
        Assert.AreEqual(ExpectedVehicleFallbackMarkerScaleX, firstVisualChild.VisibleScaleX, 0.001f);
        Assert.AreEqual(ExpectedVehicleFallbackMarkerScaleZ, firstVisualChild.VisibleScaleZ, 0.001f);
        Assert.IsTrue(em.HasComponent<PostTransformMatrix>(firstMarker));
        AssertPostTransformScale(em, firstMarker, ExpectedVehicleFallbackMarkerScaleX, 1f, ExpectedVehicleFallbackMarkerScaleZ);

        em.RemoveComponent<SelectedUnitTag>(firstVehicle);
        system.Update(world.Unmanaged);
        visibilitySystem.Update(world.Unmanaged);
        em.CompleteAllTrackedJobs();

        Assert.IsTrue(em.HasComponent<UnitSelectionMarkerInstanceReference>(firstVehicle));
        Assert.IsTrue(em.Exists(firstMarker), "Deselecting one vehicle should retain its marker entity and hide it through the visibility system.");
        Assert.AreEqual(0f, em.GetComponentData<LocalTransform>(firstMarker).Scale, 0.001f);
        Assert.IsTrue(em.HasComponent<UnitSelectionMarkerInstanceReference>(secondVehicle));
        Assert.IsTrue(em.Exists(secondMarker));
        Assert.AreEqual(1f, em.GetComponentData<LocalTransform>(secondMarker).Scale, 0.001f);
        AssertPostTransformScale(em, secondMarker, ExpectedVehicleFallbackMarkerScaleX, 1f, ExpectedVehicleFallbackMarkerScaleZ);
    }

    [Test]
    public void UnitSelectionMarkerSystemSizesVehicleMarkerFromMeshBoundsWhenAvailable()
    {
        using var world = new World(nameof(UnitSelectionMarkerSystemSizesVehicleMarkerFromMeshBoundsWhenAvailable));
        EntityManager em = world.EntityManager;
        Entity markerPrefab = CreateVisualPrefab(em);
        Entity vehicle = CreateVehicle(em, health: 100);
        Entity renderer = CreateRenderableChild(em, vehicle, "CompactVehicleBody", 1.5f);
        em.SetComponentData(renderer, new Unity.Rendering.RenderBounds
        {
            Value = new AABB
            {
                Center = float3.zero,
                Extents = new float3(64f, 64f, 64f)
            }
        });
        em.AddComponentData(vehicle, new UnitSelectionMarkerPrefabReference { Prefab = markerPrefab });
        em.AddComponent<SelectedUnitTag>(vehicle);

        SystemHandle system = world.CreateSystem<UnitSelectionMarkerSystem>();
        system.Update(world.Unmanaged);

        Assert.IsTrue(em.HasComponent<UnitSelectionMarkerInstanceReference>(vehicle));
        Entity marker = em.GetComponentData<UnitSelectionMarkerInstanceReference>(vehicle).Instance;
        SelectionMarkerVisualChild visualChild = em.GetComponentData<SelectionMarkerVisualChild>(marker);
        Assert.AreEqual(ExpectedVehicleMeshMarkerScaleZ, visualChild.VisibleScale, 0.001f);
        Assert.AreEqual(ExpectedVehicleMeshMarkerScaleX, visualChild.VisibleScaleX, 0.001f);
        Assert.AreEqual(ExpectedVehicleMeshMarkerScaleZ, visualChild.VisibleScaleZ, 0.001f);
        AssertPostTransformScale(em, marker, ExpectedVehicleMeshMarkerScaleX, 1f, ExpectedVehicleMeshMarkerScaleZ);
    }

    [Test]
    public void UnitSelectionMarkerSystemCreatesMarkerForSelectedCharacterUnit()
    {
        using var world = new World(nameof(UnitSelectionMarkerSystemCreatesMarkerForSelectedCharacterUnit));
        EntityManager em = world.EntityManager;
        Entity markerPrefab = CreateVisualPrefab(em);
        Entity character = CreateCharacter(em, health: 100);
        em.AddComponentData(character, new UnitSelectionMarkerPrefabReference { Prefab = markerPrefab });
        em.AddComponent<SelectedUnitTag>(character);

        SystemHandle system = world.CreateSystem<UnitSelectionMarkerSystem>();
        system.Update(world.Unmanaged);

        Assert.IsTrue(em.HasComponent<UnitSelectionMarkerInstanceReference>(character));
        Entity marker = em.GetComponentData<UnitSelectionMarkerInstanceReference>(character).Instance;
        Assert.AreEqual(character, em.GetComponentData<Parent>(marker).Value);
        Assert.AreEqual(0.12f, em.GetComponentData<LocalTransform>(marker).Position.y, 0.001f);
        Assert.AreEqual(1.35f, em.GetComponentData<SelectionMarkerVisualChild>(marker).VisibleScale, 0.001f);
        Assert.IsTrue(em.HasComponent<PostTransformMatrix>(marker));
        AssertPostTransformScale(em, marker, 1.35f, 1f, 1.35f);
    }

    [Test]
    public void UnitSelectionMarkerSystemSplitsReferenceMarkerPrefabForVehiclesAndInfantry()
    {
        using var world = new World(nameof(UnitSelectionMarkerSystemSplitsReferenceMarkerPrefabForVehiclesAndInfantry));
        EntityManager em = world.EntityManager;
        Entity markerPrefab = CreateReferenceSelectionMarkerPrefab(em);
        Entity vehicle = CreateVehicle(em, health: 100);
        Entity infantry = CreateCharacter(em, health: 100);
        em.AddComponentData(vehicle, new UnitSelectionMarkerPrefabReference { Prefab = markerPrefab });
        em.AddComponentData(infantry, new UnitSelectionMarkerPrefabReference { Prefab = markerPrefab });
        em.AddComponent<SelectedUnitTag>(vehicle);
        em.AddComponent<SelectedUnitTag>(infantry);

        SystemHandle system = world.CreateSystem<UnitSelectionMarkerSystem>();
        system.Update(world.Unmanaged);

        Entity vehicleMarker = em.GetComponentData<UnitSelectionMarkerInstanceReference>(vehicle).Instance;
        Entity vehicleModel = FindLinkedEntityByName(em, vehicleMarker, "Model");
        Entity vehicleInfantryRing = FindLinkedEntityByName(em, vehicleMarker, "InfantryGroundRing");
        Entity vehicleFrame = FindLinkedEntityByName(em, vehicleMarker, "VehicleBoundsFrame");
        Assert.IsTrue(em.HasComponent<PostTransformMatrix>(vehicleModel));
        AssertPostTransformScale(em, vehicleModel, ExpectedVehicleFallbackMarkerScaleX, 1f, ExpectedVehicleFallbackMarkerScaleZ);
        Assert.AreEqual(0f, em.GetComponentData<LocalTransform>(vehicleInfantryRing).Scale, 0.001f);
        Assert.AreEqual(1f, em.GetComponentData<LocalTransform>(vehicleFrame).Scale, 0.001f);

        Entity infantryMarker = em.GetComponentData<UnitSelectionMarkerInstanceReference>(infantry).Instance;
        Entity infantryModel = FindLinkedEntityByName(em, infantryMarker, "Model");
        Entity infantryRing = FindLinkedEntityByName(em, infantryMarker, "InfantryGroundRing");
        Entity infantryVehicleFrame = FindLinkedEntityByName(em, infantryMarker, "VehicleBoundsFrame");
        Assert.IsTrue(em.HasComponent<PostTransformMatrix>(infantryModel));
        AssertPostTransformScale(em, infantryModel, 1.35f, 1f, 1.35f);
        Assert.AreEqual(1f, em.GetComponentData<LocalTransform>(infantryRing).Scale, 0.001f);
        Assert.AreEqual(0f, em.GetComponentData<LocalTransform>(infantryVehicleFrame).Scale, 0.001f);
    }

    [Test]
    public void UnitSelectionMarkerSystemCreatesEcsObjectOutlinesForSelectedVehicleAndCharacterRenderChildren()
    {
        using var world = new World(nameof(UnitSelectionMarkerSystemCreatesEcsObjectOutlinesForSelectedVehicleAndCharacterRenderChildren));
        EntityManager em = world.EntityManager;
        Entity markerPrefab = CreateVisualPrefab(em);
        Entity vehicle = CreateVehicle(em, health: 100);
        Entity character = CreateCharacter(em, health: 100);
        Entity vehicleRenderer = CreateRenderableChild(em, vehicle, "VehicleBody", 1.5f);
        Entity characterRenderer = CreateRenderableChild(em, character, "CharacterBody", 0.8f);
        em.AddComponentData(vehicle, new UnitSelectionMarkerPrefabReference { Prefab = markerPrefab });
        em.AddComponentData(character, new UnitSelectionMarkerPrefabReference { Prefab = markerPrefab });
        em.AddComponent<SelectedUnitTag>(vehicle);
        em.AddComponent<SelectedUnitTag>(character);

        SystemHandle system = world.CreateSystem<UnitSelectionMarkerSystem>();
        SystemHandle visibilitySystem = world.CreateSystem<SelectionMarkerVisibilitySystem>();
        system.Update(world.Unmanaged);

        Entity vehicleOutline = AssertSelectionObjectOutline(em, vehicle, vehicleRenderer, "Vehicle");
        Entity characterOutline = AssertSelectionObjectOutline(em, character, characterRenderer, "Character");
        Assert.AreEqual(1.5f, em.GetComponentData<LocalTransform>(vehicleOutline).Scale, 0.001f);
        Assert.AreEqual(0.8f, em.GetComponentData<LocalTransform>(characterOutline).Scale, 0.001f);

        em.RemoveComponent<SelectedUnitTag>(vehicle);
        visibilitySystem.Update(world.Unmanaged);
        em.CompleteAllTrackedJobs();

        Assert.AreEqual(0f, em.GetComponentData<LocalTransform>(vehicleOutline).Scale, 0.001f);
        Assert.AreEqual(0.8f, em.GetComponentData<LocalTransform>(characterOutline).Scale, 0.001f);

        em.SetComponentData(character, new UnitHealth { Current = 0, Max = 100 });
        system.Update(world.Unmanaged);

        Assert.IsFalse(em.HasComponent<UnitSelectionMarkerInstanceReference>(character));
        Assert.IsFalse(em.Exists(characterOutline), "Destroying a selected unit marker must also destroy ECS selection-object outlines that parent outside the marker tree.");
    }

    [Test]
    public void UnitSelectionMarkerSystemKeepsAirVehicleObjectOutlineWhileGroundMarkerIsHidden()
    {
        using var world = new World(nameof(UnitSelectionMarkerSystemKeepsAirVehicleObjectOutlineWhileGroundMarkerIsHidden));
        EntityManager em = world.EntityManager;
        Entity markerPrefab = CreateReferenceSelectionMarkerPrefab(em);
        Entity aircraft = CreateVehicle(em, health: 100);
        Entity aircraftRenderer = CreateRenderableChild(em, aircraft, "TransportPlaneBody", 1.2f);
        em.AddComponentData(aircraft, new UnitAirMovement
        {
            CruiseHeight = 55f,
            RunwayTaxiSpeed = 12f
        });
        em.AddComponentData(aircraft, new UnitSelectionMarkerPrefabReference { Prefab = markerPrefab });
        em.AddComponent<SelectedUnitTag>(aircraft);

        SystemHandle system = world.CreateSystem<UnitSelectionMarkerSystem>();
        SystemHandle visibilitySystem = world.CreateSystem<SelectionMarkerVisibilitySystem>();
        system.Update(world.Unmanaged);
        visibilitySystem.Update(world.Unmanaged);
        em.CompleteAllTrackedJobs();

        Assert.IsTrue(em.HasComponent<UnitSelectionMarkerInstanceReference>(aircraft));
        Entity marker = em.GetComponentData<UnitSelectionMarkerInstanceReference>(aircraft).Instance;
        Entity markerModel = FindLinkedEntityByName(em, marker, "Model");
        Entity vehicleFrame = FindLinkedEntityByName(em, marker, "VehicleBoundsFrame");
        Assert.AreEqual(0f, em.GetComponentData<LocalTransform>(markerModel).Scale, 0.001f, "Air vehicles should not show the ground marker model.");
        Assert.AreEqual(0f, em.GetComponentData<LocalTransform>(vehicleFrame).Scale, 0.001f, "Air vehicles should not show the under-vehicle rectangle.");

        Entity outline = AssertSelectionObjectOutline(em, aircraft, aircraftRenderer, "Vehicle");
        Assert.AreEqual(1.2f, em.GetComponentData<LocalTransform>(outline).Scale, 0.001f, "Air vehicles still need a visible selected-body outline.");
    }

    [Test]
    public void UnitSelectionMarkerSystemOutlinesReferencedAirVehicleVisualRoot()
    {
        using var world = new World(nameof(UnitSelectionMarkerSystemOutlinesReferencedAirVehicleVisualRoot));
        EntityManager em = world.EntityManager;
        Entity markerPrefab = CreateReferenceSelectionMarkerPrefab(em);
        Entity aircraft = CreateVehicle(em, health: 100);
        Entity visualRoot = CreateVisualInstance(em);
        em.SetName(visualRoot, "Unit_Veh_Plane_Transport_Model");
        Entity aircraftRenderer = CreateRenderableChild(em, visualRoot, "TransportPlaneBody", 1.35f);
        em.AddComponentData(aircraft, new UnitDetailedVisualReference { Root = visualRoot });
        em.AddComponentData(aircraft, new UnitAirMovement
        {
            CruiseHeight = 55f,
            RunwayTaxiSpeed = 12f
        });
        em.AddComponentData(aircraft, new UnitSelectionMarkerPrefabReference { Prefab = markerPrefab });
        em.AddComponent<SelectedUnitTag>(aircraft);

        SystemHandle system = world.CreateSystem<UnitSelectionMarkerSystem>();
        SystemHandle visibilitySystem = world.CreateSystem<SelectionMarkerVisibilitySystem>();
        system.Update(world.Unmanaged);
        visibilitySystem.Update(world.Unmanaged);
        em.CompleteAllTrackedJobs();

        Assert.IsTrue(em.HasComponent<UnitSelectionMarkerInstanceReference>(aircraft));
        Entity marker = em.GetComponentData<UnitSelectionMarkerInstanceReference>(aircraft).Instance;
        Entity markerModel = FindLinkedEntityByName(em, marker, "Model");
        Entity vehicleFrame = FindLinkedEntityByName(em, marker, "VehicleBoundsFrame");
        Assert.AreEqual(0f, em.GetComponentData<LocalTransform>(markerModel).Scale, 0.001f);
        Assert.AreEqual(0f, em.GetComponentData<LocalTransform>(vehicleFrame).Scale, 0.001f);

        Entity outline = AssertSelectionObjectOutline(em, aircraft, aircraftRenderer, "Vehicle", visualRoot);
        Assert.AreEqual(1.35f, em.GetComponentData<LocalTransform>(outline).Scale, 0.001f);
    }

    [Test]
    public void UnitSelectionMarkerSystemCreatesSafeSelectionVolumeForGpuAnimatedCharacterWithoutBindPoseOverlay()
    {
        using var world = new World(nameof(UnitSelectionMarkerSystemCreatesSafeSelectionVolumeForGpuAnimatedCharacterWithoutBindPoseOverlay));
        EntityManager em = world.EntityManager;
        Entity markerPrefab = CreateVisualPrefab(em);
        Entity character = CreateCharacter(em, health: 100);
        Entity renderer = CreateRenderableChild(em, character, "GpuAnimatedCharacterBody", 1f);
        em.AddComponentData(character, new UnitSelectionMarkerPrefabReference { Prefab = markerPrefab });
        em.AddComponent<SelectedUnitTag>(character);
        em.AddComponentData(character, new MaterialAnimationIndex { Value = 1 });
        em.AddComponentData(character, new MaterialAnimationData
        {
            AnimationIndex = 1,
            TransitionIndex = 1,
            Time = 0f,
            TransitionTime = 0f,
            RenderConfig = new float3(1f, 2f, 0.5f)
        });
        em.AddComponentData(character, new MaterialAnimatorLink { Value = Entity.Null });
        em.AddComponentData(renderer, new MeshLODComponent
        {
            Group = character,
            ParentGroup = character,
            LODMask = 1
        });
        em.AddComponentData(renderer, new MaterialPropertyShowModel { Value = 1f });
        em.AddComponentData(renderer, new MaterialPropertyRenderPixel { Value = new float3(1f, 2f, 0.5f) });
        em.AddComponentData(renderer, new MaterialPropertyAlphaEnabled { Value = 1f });
        em.AddComponent<MaterialAlphaCompleteTag>(renderer);
        em.SetComponentData(renderer, new Unity.Rendering.RenderBounds
        {
            Value = new AABB
            {
                Center = float3.zero,
                Extents = new float3(24f, 16f, 30f)
            }
        });

        SystemHandle system = world.CreateSystem<UnitSelectionMarkerSystem>();
        system.Update(world.Unmanaged);

        Assert.IsTrue(em.HasComponent<UnitSelectionMarkerInstanceReference>(character));
        Entity marker = em.GetComponentData<UnitSelectionMarkerInstanceReference>(character).Instance;
        Assert.IsTrue(em.Exists(marker));
        Assert.IsTrue(em.HasBuffer<SelectionObjectOutlineInstanceElement>(marker));
        AssertSafeGpuAnimatedSelectionVolume(em, character, renderer);
    }

    [Test]
    public void UnitSelectionMarkerSystemHidesMarkersForTransportedCharactersButKeepsCulledSelectedCharactersVisible()
    {
        using var world = new World(nameof(UnitSelectionMarkerSystemHidesMarkersForTransportedCharactersButKeepsCulledSelectedCharactersVisible));
        EntityManager em = world.EntityManager;
        Entity markerPrefab = CreateVisualPrefab(em);
        Entity character = CreateCharacter(em, health: 100);
        em.AddComponentData(character, new UnitSelectionMarkerPrefabReference { Prefab = markerPrefab });
        em.AddComponent<SelectedUnitTag>(character);

        SystemHandle system = world.CreateSystem<UnitSelectionMarkerSystem>();
        SystemHandle visibilitySystem = world.CreateSystem<SelectionMarkerVisibilitySystem>();
        system.Update(world.Unmanaged);

        Assert.IsTrue(em.HasComponent<UnitSelectionMarkerInstanceReference>(character));
        Entity marker = em.GetComponentData<UnitSelectionMarkerInstanceReference>(character).Instance;

        em.AddComponentData(character, new UnitTransportPassenger { Transport = Entity.Null });
        system.Update(world.Unmanaged);
        visibilitySystem.Update(world.Unmanaged);
        em.CompleteAllTrackedJobs();

        Assert.IsTrue(em.HasComponent<UnitSelectionMarkerInstanceReference>(character));
        Assert.IsTrue(em.Exists(marker), "Transported characters should retain their marker instance and hide it while onboard.");
        Assert.AreEqual(0f, em.GetComponentData<LocalTransform>(marker).Scale, 0.001f);

        em.RemoveComponent<UnitTransportPassenger>(character);
        em.AddComponent<UnitRenderBudgetCulledUnitTag>(character);
        system.Update(world.Unmanaged);
        visibilitySystem.Update(world.Unmanaged);
        em.CompleteAllTrackedJobs();

        Assert.IsTrue(em.HasComponent<UnitSelectionMarkerInstanceReference>(character));
        Assert.AreEqual(1f, em.GetComponentData<LocalTransform>(marker).Scale, 0.001f);
        AssertPostTransformScale(em, marker, 1.35f, 1f, 1.35f);
    }

    [Test]
    public void SelectionMarkerVisibilitySystemTogglesVisualChildScaleFromSelectionState()
    {
        using var world = new World(nameof(SelectionMarkerVisibilitySystemTogglesVisualChildScaleFromSelectionState));
        EntityManager em = world.EntityManager;
        Entity unit = CreateVehicle(em, health: 100);
        em.AddComponent<SelectedUnitTag>(unit);

        Entity visualChild = CreateVisualInstance(em);
        em.AddComponentData(visualChild, new PostTransformMatrix { Value = float4x4.identity });
        Entity marker = CreateVisualInstance(em);
        em.AddComponentData(marker, new Parent { Value = unit });
        em.AddComponent<SelectionMarkerTag>(marker);
        em.AddComponentData(marker, new SelectionMarkerVisualChild
        {
            Value = visualChild,
            VisibleScale = 4.05f
        });

        SystemHandle system = world.CreateSystem<SelectionMarkerVisibilitySystem>();
        system.Update(world.Unmanaged);
        em.CompleteAllTrackedJobs();

        Assert.AreEqual(1f, em.GetComponentData<LocalTransform>(marker).Scale, 0.001f);
        Assert.AreEqual(1f, em.GetComponentData<LocalTransform>(visualChild).Scale, 0.001f);
        AssertPostTransformScale(em, visualChild, 4.05f, 1f, 4.05f);

        em.RemoveComponent<SelectedUnitTag>(unit);
        system.Update(world.Unmanaged);
        em.CompleteAllTrackedJobs();

        Assert.AreEqual(1f, em.GetComponentData<LocalTransform>(marker).Scale, 0.001f);
        Assert.AreEqual(0f, em.GetComponentData<LocalTransform>(visualChild).Scale, 0.001f);
        AssertPostTransformScale(em, visualChild, 0f, 1f, 0f);
    }

    [Test]
    public void UnitFactionTintTargetBackfillIgnoresSelectionObjectOutlines()
    {
        using var world = new World(nameof(UnitFactionTintTargetBackfillIgnoresSelectionObjectOutlines));
        EntityManager em = world.EntityManager;
        Entity character = CreateCharacter(em, health: 100);
        em.AddComponentData(character, new UnitGrid());
        em.AddComponentData(character, new UnitSourcePrefabKey { Value = new FixedString64Bytes("Unit_Chr_Soldier") });
        Entity renderer = CreateRenderableChild(em, character, "CharacterBody", 1f);
        Entity outline = CreateRenderableChild(em, character, "CharacterSelectionOutline", 1f);
        em.AddComponent<SelectionObjectOutlineTag>(outline);

        SystemHandle backfill = world.CreateSystem<UnitFactionTintTargetBackfillSystem>();
        backfill.Update(world.Unmanaged);

        Assert.IsTrue(em.HasComponent<FactionTintTarget>(renderer));
        Assert.IsTrue(em.HasComponent<FactionTintColor>(renderer));
        Assert.IsFalse(em.HasComponent<FactionTintTarget>(outline), "Selection-object outlines keep their authored cyan material and must not become faction tint targets.");
        Assert.IsFalse(em.HasComponent<FactionTintColor>(outline));
    }

    [Test]
    public void UnitRuntimeHealthBarSystemCreatesAndRetainsRuntimeHealthBar()
    {
        using var world = new World(nameof(UnitRuntimeHealthBarSystemCreatesAndRetainsRuntimeHealthBar));
        EntityManager em = world.EntityManager;
        Entity healthBarPrefab = CreateHealthBarPrefab(em);
        Entity vehicle = CreateVehicle(em, health: 60);
        em.AddComponentData(vehicle, new UnitHealthBarPrefabReference { Prefab = healthBarPrefab });
        em.AddComponentData(vehicle, new RecentDamageHealthBarVisibility { TimeRemaining = 1f });

        SystemHandle system = world.CreateSystem<UnitRuntimeHealthBarSystem>();
        world.CreateSystem<EndSimulationEntityCommandBufferSystem>();
        SystemHandle visibilitySystem = world.CreateSystem<UnitHealthBarSystem>();
        system.Update(world.Unmanaged);

        Assert.IsTrue(em.HasComponent<UnitHealthBarInstanceReference>(vehicle));
        Entity healthBar = em.GetComponentData<UnitHealthBarInstanceReference>(vehicle).Instance;
        Assert.AreEqual(vehicle, em.GetComponentData<Parent>(healthBar).Value);
        Assert.IsTrue(em.HasComponent<HealthBarFill>(healthBar));

        em.RemoveComponent<RecentDamageHealthBarVisibility>(vehicle);
        system.Update(world.Unmanaged);
        visibilitySystem.Update(world.Unmanaged);
        em.CompleteAllTrackedJobs();

        Assert.IsTrue(em.HasComponent<UnitHealthBarInstanceReference>(vehicle));
        Assert.IsTrue(em.Exists(healthBar), "Expired health-bar feedback should retain the instance and hide it for reuse.");
        Assert.AreEqual(0f, em.GetComponentData<LocalTransform>(healthBar).Scale, 0.001f);
    }

    [Test]
    public void UnitRuntimeHealthBarSystemCreatesHealthBarForDamagedCharacterUnit()
    {
        using var world = new World(nameof(UnitRuntimeHealthBarSystemCreatesHealthBarForDamagedCharacterUnit));
        EntityManager em = world.EntityManager;
        Entity healthBarPrefab = CreateHealthBarPrefab(em);
        Entity character = CreateCharacter(em, health: 60);
        em.AddComponentData(character, new UnitHealthBarPrefabReference { Prefab = healthBarPrefab });
        em.AddComponentData(character, new RecentDamageHealthBarVisibility { TimeRemaining = 1f });

        SystemHandle system = world.CreateSystem<UnitRuntimeHealthBarSystem>();
        system.Update(world.Unmanaged);

        Assert.IsTrue(em.HasComponent<UnitHealthBarInstanceReference>(character));
        Entity healthBar = em.GetComponentData<UnitHealthBarInstanceReference>(character).Instance;
        Assert.AreEqual(character, em.GetComponentData<Parent>(healthBar).Value);
    }

    [Test]
    public void UnitRuntimeHealthBarSystemRetainsAndHidesBarsForTransportedOrImpostorOnlyCharacters()
    {
        using var world = new World(nameof(UnitRuntimeHealthBarSystemRetainsAndHidesBarsForTransportedOrImpostorOnlyCharacters));
        EntityManager em = world.EntityManager;
        Entity healthBarPrefab = CreateHealthBarPrefab(em);
        Entity character = CreateCharacter(em, health: 60);
        em.AddComponentData(character, new UnitHealthBarPrefabReference { Prefab = healthBarPrefab });
        em.AddComponentData(character, new RecentDamageHealthBarVisibility { TimeRemaining = 1f });

        SystemHandle system = world.CreateSystem<UnitRuntimeHealthBarSystem>();
        world.CreateSystem<EndSimulationEntityCommandBufferSystem>();
        SystemHandle visibilitySystem = world.CreateSystem<UnitHealthBarSystem>();
        system.Update(world.Unmanaged);

        Assert.IsTrue(em.HasComponent<UnitHealthBarInstanceReference>(character));
        Entity healthBar = em.GetComponentData<UnitHealthBarInstanceReference>(character).Instance;

        em.AddComponentData(character, new UnitTransportPassenger { Transport = Entity.Null });
        system.Update(world.Unmanaged);
        visibilitySystem.Update(world.Unmanaged);
        em.CompleteAllTrackedJobs();

        Assert.IsTrue(em.HasComponent<UnitHealthBarInstanceReference>(character));
        Assert.IsTrue(em.Exists(healthBar), "Transported characters should retain health bars and hide them while onboard.");
        Assert.AreEqual(0f, em.GetComponentData<LocalTransform>(healthBar).Scale, 0.001f);

        em.RemoveComponent<UnitTransportPassenger>(character);
        em.AddComponent<UnitRenderBudgetCulledUnitTag>(character);
        system.Update(world.Unmanaged);
        visibilitySystem.Update(world.Unmanaged);
        em.CompleteAllTrackedJobs();

        Assert.IsTrue(em.HasComponent<UnitHealthBarInstanceReference>(character));
        Assert.IsTrue(em.Exists(healthBar));
        Assert.AreEqual(0f, em.GetComponentData<LocalTransform>(healthBar).Scale, 0.001f);
    }

    [Test]
    public void UnitDestroyedVisualSystemInitializesAliveAndDestroyedChildScales()
    {
        using var world = new World(nameof(UnitDestroyedVisualSystemInitializesAliveAndDestroyedChildScales));
        EntityManager em = world.EntityManager;
        Entity aliveVisual = CreateVisualInstance(em);
        Entity destroyedVisual = CreateVisualInstance(em);
        Entity unit = CreateVehicle(em, health: 100);
        em.AddComponentData(unit, new UnitDestroyedVisualReference
        {
            AliveVisual = aliveVisual,
            DestroyedVisual = destroyedVisual,
            AliveVisibleScale = 1.75f,
            DestroyedVisibleScale = 1.25f
        });

        SystemHandle endSimulationEcbSystem = world.CreateSystem<EndSimulationEntityCommandBufferSystem>();
        SystemHandle system = world.CreateSystem<UnitDestroyedVisualSystem>();
        system.Update(world.Unmanaged);
        endSimulationEcbSystem.Update(world.Unmanaged);

        Assert.IsTrue(em.HasComponent<UnitDestroyedVisualInitialized>(unit));
        Assert.AreEqual(1.75f, em.GetComponentData<LocalTransform>(aliveVisual).Scale, 0.001f);
        Assert.AreEqual(0f, em.GetComponentData<LocalTransform>(destroyedVisual).Scale, 0.001f);
    }

    [Test]
    public void UnitHealthBarSystemExpiresRecentDamageVisibilityWithEcb()
    {
        using var world = new World(nameof(UnitHealthBarSystemExpiresRecentDamageVisibilityWithEcb));
        world.SetTime(new TimeData(1d, 0.1f));
        EntityManager em = world.EntityManager;
        Entity unit = CreateCharacter(em, health: 50);
        em.AddComponentData(unit, new RecentDamageHealthBarVisibility { TimeRemaining = 0.05f });
        Entity healthBar = CreateVisualInstance(em);
        em.AddComponentData(healthBar, new Parent { Value = unit });
        em.AddComponentData(healthBar, new HealthBarFill { Value = 1f });

        SystemHandle endSimulationEcbSystem = world.CreateSystem<EndSimulationEntityCommandBufferSystem>();
        SystemHandle system = world.CreateSystem<UnitHealthBarSystem>();
        system.Update(world.Unmanaged);
        endSimulationEcbSystem.Update(world.Unmanaged);
        em.CompleteAllTrackedJobs();

        Assert.IsFalse(em.HasComponent<RecentDamageHealthBarVisibility>(unit));
        Assert.AreEqual(0f, em.GetComponentData<LocalTransform>(healthBar).Scale, 0.001f);
        Assert.AreEqual(0.5f, em.GetComponentData<HealthBarFill>(healthBar).Value, 0.001f);
    }

    [Test]
    public void FactionVisualSystemAppliesFactionTintToCharacterModelTargets()
    {
        using var world = new World(nameof(FactionVisualSystemAppliesFactionTintToCharacterModelTargets));
        EntityManager em = world.EntityManager;
        Entity config = em.CreateEntity(typeof(FactionVisualConfig));
        em.SetComponentData(config, new FactionVisualConfig
        {
            NeutralColor = new float4(0.5f, 0.5f, 0.5f, 1f),
            PlayerColor = new float4(0.1f, 0.7f, 1f, 1f),
            EnemyColor = new float4(1f, 0.2f, 0.1f, 1f)
        });

        Entity character = CreateCharacter(em, health: 100);
        em.AddComponentData(character, new Faction { Id = 1 });
        Entity renderer = em.CreateEntity(
            typeof(FactionTintTarget),
            typeof(FactionTintColor),
            typeof(FactionSnivelerBaseColor),
            typeof(Parent));
        em.SetComponentData(renderer, new FactionTintColor { Value = new float4(1f, 1f, 1f, 1f) });
        em.SetComponentData(renderer, new FactionSnivelerBaseColor { Value = new float4(1f, 1f, 1f, 1f) });
        em.SetComponentData(renderer, new Parent { Value = character });

        SystemHandle system = world.CreateSystem<FactionVisualSystem>();
        system.Update(world.Unmanaged);
        em.CompleteAllTrackedJobs();

        Assert.AreEqual(new float4(0.1f, 0.7f, 1f, 1f), em.GetComponentData<FactionTintColor>(renderer).Value);
        Assert.AreEqual(new float4(0.1f, 0.7f, 1f, 1f), em.GetComponentData<FactionSnivelerBaseColor>(renderer).Value);

        em.SetComponentData(character, new Faction { Id = 2 });
        system.Update(world.Unmanaged);
        em.CompleteAllTrackedJobs();

        Assert.AreEqual(new float4(1f, 0.2f, 0.1f, 1f), em.GetComponentData<FactionTintColor>(renderer).Value);
        Assert.AreEqual(new float4(1f, 0.2f, 0.1f, 1f), em.GetComponentData<FactionSnivelerBaseColor>(renderer).Value);

        em.SetComponentData(character, new Faction { Id = 0 });
        system.Update(world.Unmanaged);
        em.CompleteAllTrackedJobs();

        Assert.AreEqual(new float4(0.5f, 0.5f, 0.5f, 1f), em.GetComponentData<FactionTintColor>(renderer).Value);
        Assert.AreEqual(new float4(0.5f, 0.5f, 0.5f, 1f), em.GetComponentData<FactionSnivelerBaseColor>(renderer).Value);
    }

    [Test]
    public void UnitFactionTintTargetBackfillFindsDeepCharacterRenderHierarchy()
    {
        using var world = new World(nameof(UnitFactionTintTargetBackfillFindsDeepCharacterRenderHierarchy));
        EntityManager em = world.EntityManager;
        Entity config = em.CreateEntity(typeof(FactionVisualConfig));
        em.SetComponentData(config, new FactionVisualConfig
        {
            NeutralColor = new float4(0.5f, 0.5f, 0.5f, 1f),
            PlayerColor = new float4(0.1f, 0.7f, 1f, 1f),
            EnemyColor = new float4(1f, 0.2f, 0.1f, 1f)
        });

        Entity character = CreateCharacter(em, health: 100);
        em.AddComponentData(character, new Faction { Id = 1 });
        em.AddComponentData(character, new UnitGrid());
        em.AddComponentData(character, new UnitSourcePrefabKey { Value = new FixedString64Bytes("Unit_Chr_Soldier_Male_02_Alt_04") });

        Entity parent = character;
        for (int i = 0; i < 32; i++)
        {
            Entity bone = em.CreateEntity(typeof(Parent));
            em.SetComponentData(bone, new Parent { Value = parent });
            parent = bone;
        }

        Entity renderer = em.CreateEntity(typeof(Parent), typeof(MaterialMeshInfo));
        em.SetComponentData(renderer, new Parent { Value = parent });

        SystemHandle backfill = world.CreateSystem<UnitFactionTintTargetBackfillSystem>();
        backfill.Update(world.Unmanaged);

        Assert.IsTrue(em.HasComponent<FactionTintTarget>(renderer));
        Assert.IsTrue(em.HasComponent<FactionTintColor>(renderer));
        Assert.IsTrue(em.HasComponent<FactionSnivelerBaseColor>(renderer));

        SystemHandle factionVisual = world.CreateSystem<FactionVisualSystem>();
        factionVisual.Update(world.Unmanaged);
        em.CompleteAllTrackedJobs();

        Assert.AreEqual(new float4(0.1f, 0.7f, 1f, 1f), em.GetComponentData<FactionTintColor>(renderer).Value);
        Assert.AreEqual(new float4(0.1f, 0.7f, 1f, 1f), em.GetComponentData<FactionSnivelerBaseColor>(renderer).Value);
    }

    [Test]
    public void VehicleDestroyedVisualSystemSpawnsDestroyedVisualAndCleansRuntimeAdornments()
    {
        using var world = new World(nameof(VehicleDestroyedVisualSystemSpawnsDestroyedVisualAndCleansRuntimeAdornments));
        EntityManager em = world.EntityManager;
        Entity destroyedVisualPrefab = CreateVisualPrefab(em);
        Entity marker = CreateVisualInstance(em);
        Entity healthBar = CreateVisualInstance(em);
        Entity aliveVisual = CreateVisualInstance(em);
        Entity vehicle = CreateVehicle(em, health: 0);
        em.AddComponentData(vehicle, new VehicleDestroyedVisualPrefabReference { Prefab = destroyedVisualPrefab });
        em.AddComponentData(vehicle, new VehicleDestroyedVisualSpawnRequest());
        em.AddComponentData(vehicle, new UnitSelectionMarkerInstanceReference { Instance = marker });
        em.AddComponentData(vehicle, new UnitHealthBarInstanceReference { Instance = healthBar });
        em.AddComponentData(vehicle, new UnitDetailedVisualReference { Root = aliveVisual });

        SystemHandle system = world.CreateSystem<VehicleDestroyedVisualSystem>();
        system.Update(world.Unmanaged);

        Assert.IsFalse(em.HasComponent<VehicleDestroyedVisualSpawnRequest>(vehicle));
        Assert.IsFalse(em.HasComponent<UnitSelectionMarkerInstanceReference>(vehicle));
        Assert.IsFalse(em.HasComponent<UnitHealthBarInstanceReference>(vehicle));
        Assert.IsFalse(em.Exists(marker));
        Assert.IsFalse(em.Exists(healthBar));
        Assert.AreEqual(0f, em.GetComponentData<LocalTransform>(aliveVisual).Scale);
        Assert.IsTrue(em.HasComponent<VehicleDestroyedVisualInstanceReference>(vehicle));
        Entity destroyedVisual = em.GetComponentData<VehicleDestroyedVisualInstanceReference>(vehicle).Instance;
        Assert.AreEqual(vehicle, em.GetComponentData<Parent>(destroyedVisual).Value);
    }

    private static Entity CreateVehicle(EntityManager em, int health)
    {
        Entity entity = em.CreateEntity(
            typeof(UnitMovementBehavior),
            typeof(UnitHealth),
            typeof(UnitFootprint),
            typeof(LocalTransform));
        em.SetComponentData(entity, new UnitMovementBehavior { UsesVehicleMotion = 1 });
        em.SetComponentData(entity, new UnitHealth { Current = health, Max = 100 });
        em.SetComponentData(entity, new UnitFootprint { Size = new int2(3, 2) });
        em.SetComponentData(entity, LocalTransform.Identity);
        return entity;
    }

    private static void AssertPostTransformScale(EntityManager em, Entity entity, float expectedX, float expectedY, float expectedZ)
    {
        Assert.IsTrue(em.HasComponent<PostTransformMatrix>(entity), $"{entity} must use non-uniform marker scale.");
        float4x4 matrix = em.GetComponentData<PostTransformMatrix>(entity).Value;
        Assert.AreEqual(expectedX, matrix.c0.x, 0.001f);
        Assert.AreEqual(expectedY, matrix.c1.y, 0.001f);
        Assert.AreEqual(expectedZ, matrix.c2.z, 0.001f);
    }

    private static Entity CreateCharacter(EntityManager em, int health)
    {
        Entity entity = em.CreateEntity(
            typeof(UnitMovementBehavior),
            typeof(UnitHealth),
            typeof(UnitFootprint),
            typeof(LocalTransform));
        em.SetComponentData(entity, new UnitMovementBehavior { UsesVehicleMotion = 0 });
        em.SetComponentData(entity, new UnitHealth { Current = health, Max = 100 });
        em.SetComponentData(entity, new UnitFootprint { Size = new int2(1, 1) });
        em.SetComponentData(entity, LocalTransform.Identity);
        return entity;
    }

    private static Entity AssertSafeGpuAnimatedSelectionVolume(EntityManager em, Entity unit, Entity sourceRenderer)
    {
        Assert.IsTrue(em.HasComponent<UnitSelectionMarkerInstanceReference>(unit));
        Entity marker = em.GetComponentData<UnitSelectionMarkerInstanceReference>(unit).Instance;
        Assert.IsTrue(em.HasBuffer<SelectionObjectOutlineInstanceElement>(marker), "GPU-animated selected units must own a safe selection volume from their marker instance.");
        DynamicBuffer<SelectionObjectOutlineInstanceElement> outlines = em.GetBuffer<SelectionObjectOutlineInstanceElement>(marker);
        Assert.Greater(outlines.Length, 0);

        Entity volume = outlines[0].Value;
        Assert.IsTrue(em.Exists(volume));
        Assert.IsTrue(em.HasComponent<SelectionObjectOutlineTag>(volume));
        Assert.AreEqual(unit, em.GetComponentData<SelectionMarkerOwner>(volume).Value);
        Assert.AreEqual(unit, em.GetComponentData<Parent>(volume).Value);
        Assert.AreEqual(LocalTransform.Identity.Position, em.GetComponentData<LocalTransform>(volume).Position);
        Assert.IsTrue(em.HasComponent<PostTransformMatrix>(volume));
        float4x4 volumeScale = em.GetComponentData<PostTransformMatrix>(volume).Value;
        Assert.LessOrEqual(volumeScale.c0.x, 0.86f, "Oversized animated renderer bounds must not inflate the soldier selection volume.");
        Assert.LessOrEqual(volumeScale.c1.y, 2.05f, "Oversized animated renderer bounds must not inflate the soldier selection volume.");
        Assert.LessOrEqual(volumeScale.c2.z, 0.86f, "Oversized animated renderer bounds must not inflate the soldier selection volume.");
        Assert.GreaterOrEqual(volumeScale.c0.x, 0.56f);
        Assert.GreaterOrEqual(volumeScale.c1.y, 1.2f);
        Assert.GreaterOrEqual(volumeScale.c2.z, 0.56f);
        Assert.IsFalse(em.HasComponent<MeshLODComponent>(volume), "GPU-animated soldiers must not use duplicated render mesh outlines by default.");
        Assert.IsFalse(em.HasComponent<MaterialPropertyRenderPixel>(volume));
        Assert.IsFalse(em.HasComponent<MaterialPropertyShowModel>(volume));
        Assert.IsFalse(em.HasComponent<MaterialPropertyAlphaEnabled>(volume));

        RenderMeshArray renderMeshArray = em.GetSharedComponentManaged<RenderMeshArray>(volume);
        MaterialMeshInfo materialMeshInfo = em.GetComponentData<MaterialMeshInfo>(volume);
        Mesh mesh = renderMeshArray.GetMesh(materialMeshInfo);
        Assert.IsNotNull(mesh);
        Assert.Greater(mesh.vertexCount, 0);
        Assert.IsTrue(mesh.HasVertexAttribute(VertexAttribute.TexCoord0), "Safe soldier selection volume must provide UVs because SelectionHologram uses UVs for visibility.");
        Assert.IsTrue(mesh.HasVertexAttribute(VertexAttribute.Color), "Safe soldier selection volume must provide vertex color for SelectionHologram modulation.");
        Material material = renderMeshArray.GetMaterial(materialMeshInfo);
        Assert.IsNotNull(material);
        Assert.IsTrue(material.enableInstancing);
        Assert.AreEqual("WarlineCapture/Markers/SelectionHologram", material.shader.name);
        StringAssert.Contains("CharacterSelectionVolume", em.GetName(volume));
        Assert.IsTrue(em.HasComponent<RenderFilterSettings>(volume));
        RenderFilterSettings settings = em.GetSharedComponentManaged<RenderFilterSettings>(volume);
        Assert.AreEqual(ShadowCastingMode.Off, settings.ShadowCastingMode);
        Assert.IsFalse(settings.ReceiveShadows);
        Assert.AreEqual(MotionVectorGenerationMode.ForceNoMotion, settings.MotionMode);
        Assert.AreEqual(7, settings.Layer);
        Assert.AreEqual(0x00000004u, settings.RenderingLayerMask);
        Assert.IsTrue(em.HasComponent<Unity.Rendering.RenderBounds>(sourceRenderer));
        return volume;
    }

    private static Entity AssertSelectionObjectOutline(EntityManager em, Entity unit, Entity sourceRenderer, string expectedKind, Entity expectedParent = default)
    {
        Assert.IsTrue(em.HasComponent<UnitSelectionMarkerInstanceReference>(unit));
        Entity marker = em.GetComponentData<UnitSelectionMarkerInstanceReference>(unit).Instance;
        Assert.IsTrue(em.HasBuffer<SelectionObjectOutlineInstanceElement>(marker), "Selected units with ECS render children must own outline entities from their marker instance.");
        DynamicBuffer<SelectionObjectOutlineInstanceElement> outlines = em.GetBuffer<SelectionObjectOutlineInstanceElement>(marker);
        Assert.Greater(outlines.Length, 0);

        Entity outline = outlines[0].Value;
        Assert.IsTrue(em.Exists(outline));
        Assert.IsTrue(em.HasComponent<SelectionObjectOutlineTag>(outline));
        Assert.AreEqual(unit, em.GetComponentData<SelectionMarkerOwner>(outline).Value);
        Assert.AreEqual(expectedParent == Entity.Null ? unit : expectedParent, em.GetComponentData<Parent>(outline).Value);
        Assert.AreEqual(em.GetComponentData<LocalTransform>(sourceRenderer).Position, em.GetComponentData<LocalTransform>(outline).Position);
        Assert.AreEqual(em.GetComponentData<LocalTransform>(sourceRenderer).Rotation, em.GetComponentData<LocalTransform>(outline).Rotation);
        Assert.IsTrue(em.HasComponent<MaterialMeshInfo>(outline));
        Assert.IsTrue(em.HasComponent<Unity.Rendering.RenderBounds>(outline));

        RenderMeshArray renderMeshArray = em.GetSharedComponentManaged<RenderMeshArray>(outline);
        Material material = renderMeshArray.GetMaterial(em.GetComponentData<MaterialMeshInfo>(outline));
        Assert.IsNotNull(material);
        Assert.IsTrue(material.enableInstancing);
        Assert.AreEqual("WarlineCapture/Markers/SelectionObjectOutline", material.shader.name);
        StringAssert.Contains(expectedKind, em.GetName(outline));

        Assert.IsTrue(em.HasComponent<RenderFilterSettings>(outline));
        RenderFilterSettings settings = em.GetSharedComponentManaged<RenderFilterSettings>(outline);
        Assert.AreEqual(ShadowCastingMode.Off, settings.ShadowCastingMode);
        Assert.IsFalse(settings.ReceiveShadows);
        Assert.AreEqual(MotionVectorGenerationMode.ForceNoMotion, settings.MotionMode);
        Assert.AreEqual(7, settings.Layer);
        Assert.AreEqual(0x00000004u, settings.RenderingLayerMask);
        return outline;
    }

    private static Entity CreateRenderableChild(EntityManager em, Entity parent, string name, float scale)
    {
        Entity entity = em.CreateEntity(
            typeof(Parent),
            typeof(LocalTransform),
            typeof(MaterialMeshInfo),
            typeof(Unity.Rendering.RenderBounds));
        em.SetName(entity, name);
        em.SetComponentData(entity, new Parent { Value = parent });
        em.SetComponentData(entity, LocalTransform.FromPositionRotationScale(new float3(0.1f, 0.2f, 0.3f), quaternion.identity, scale));
        em.SetComponentData(entity, MaterialMeshInfo.FromRenderMeshArrayIndices(0, 0));
        em.SetComponentData(entity, new Unity.Rendering.RenderBounds
        {
            Value = new AABB
            {
                Center = float3.zero,
                Extents = new float3(0.5f, 0.6f, 0.7f)
            }
        });
        em.AddSharedComponentManaged(entity, new RenderMeshArray(
            new[] { CreateUnitTestMaterial(name + "_Material") },
            new[] { CreateUnitTestMesh(name + "_Mesh") }));
        em.AddSharedComponentManaged(entity, new RenderFilterSettings
        {
            Layer = 7,
            RenderingLayerMask = 0x00000004u,
            MotionMode = MotionVectorGenerationMode.Camera,
            ShadowCastingMode = ShadowCastingMode.On,
            ReceiveShadows = true,
            StaticShadowCaster = false
        });
        return entity;
    }

    private static Material CreateUnitTestMaterial(string name)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Unlit") ??
                        Shader.Find("Unlit/Color") ??
                        Shader.Find("Standard");
        return new Material(shader)
        {
            name = name,
            hideFlags = HideFlags.HideAndDontSave
        };
    }

    private static Mesh CreateUnitTestMesh(string name)
    {
        Mesh mesh = new()
        {
            name = name,
            hideFlags = HideFlags.HideAndDontSave
        };
        mesh.vertices = new[]
        {
            new Vector3(-0.5f, 0f, -0.5f),
            new Vector3(0.5f, 0f, -0.5f),
            new Vector3(0f, 1f, 0.5f),
            new Vector3(0f, 0f, 0.5f)
        };
        mesh.triangles = new[] { 0, 2, 1, 0, 1, 3, 1, 2, 3, 2, 0, 3 };
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        return mesh;
    }

    private static Entity CreateVehiclePrefab(EntityManager em, string sourceKey)
    {
        Entity entity = em.CreateEntity(
            typeof(Prefab),
            typeof(UnitMovementBehavior),
            typeof(UnitSourcePrefabKey),
            typeof(LocalTransform));
        em.SetComponentData(entity, new UnitMovementBehavior { UsesVehicleMotion = 1 });
        em.SetComponentData(entity, new UnitSourcePrefabKey { Value = new FixedString64Bytes(sourceKey) });
        em.SetComponentData(entity, LocalTransform.Identity);
        return entity;
    }

    private static Entity CreateCharacterPrefab(EntityManager em, string sourceKey)
    {
        Entity entity = em.CreateEntity(
            typeof(Prefab),
            typeof(UnitMovementBehavior),
            typeof(UnitSourcePrefabKey),
            typeof(LocalTransform));
        em.SetComponentData(entity, new UnitMovementBehavior { UsesVehicleMotion = 0 });
        em.SetComponentData(entity, new UnitSourcePrefabKey { Value = new FixedString64Bytes(sourceKey) });
        em.SetComponentData(entity, LocalTransform.Identity);
        return entity;
    }

    private static Entity CreateHealthBarPrefab(EntityManager em)
    {
        Entity entity = CreateVisualPrefab(em);
        em.AddComponentData(entity, new HealthBarFill { Value = 1f });
        return entity;
    }

    private static Entity CreateReferenceSelectionMarkerPrefab(EntityManager em)
    {
        Entity root = em.CreateEntity(typeof(Prefab), typeof(LocalTransform), typeof(SelectionMarkerTag));
        Entity model = em.CreateEntity(typeof(Prefab), typeof(Parent), typeof(LocalTransform));
        Entity infantryRing = em.CreateEntity(typeof(Prefab), typeof(Parent), typeof(LocalTransform));
        Entity vehicleFrame = em.CreateEntity(typeof(Prefab), typeof(Parent), typeof(LocalTransform));

        em.SetName(root, "VehicleSelectionMarker");
        em.SetName(model, "Model");
        em.SetName(infantryRing, "InfantryGroundRing");
        em.SetName(vehicleFrame, "VehicleBoundsFrame");
        em.SetComponentData(root, LocalTransform.Identity);
        em.SetComponentData(model, LocalTransform.Identity);
        em.SetComponentData(infantryRing, LocalTransform.Identity);
        em.SetComponentData(vehicleFrame, LocalTransform.Identity);
        em.SetComponentData(model, new Parent { Value = root });
        em.SetComponentData(infantryRing, new Parent { Value = model });
        em.SetComponentData(vehicleFrame, new Parent { Value = model });
        em.AddComponentData(root, new SelectionMarkerVisualChild
        {
            Value = model,
            VisibleScale = 1f
        });

        DynamicBuffer<LinkedEntityGroup> linked = em.AddBuffer<LinkedEntityGroup>(root);
        linked.Add(new LinkedEntityGroup { Value = root });
        linked.Add(new LinkedEntityGroup { Value = model });
        linked.Add(new LinkedEntityGroup { Value = infantryRing });
        linked.Add(new LinkedEntityGroup { Value = vehicleFrame });
        return root;
    }

    private static Entity FindLinkedEntityByName(EntityManager em, Entity root, string name)
    {
        Assert.IsTrue(em.HasBuffer<LinkedEntityGroup>(root), $"{root} must have a linked entity group.");
        DynamicBuffer<LinkedEntityGroup> linked = em.GetBuffer<LinkedEntityGroup>(root);
        for (int i = 0; i < linked.Length; i++)
        {
            Entity entity = linked[i].Value;
            if (entity != Entity.Null && em.Exists(entity) && em.GetName(entity) == name)
                return entity;
        }

        Assert.Fail($"Could not find linked entity named {name}.");
        return Entity.Null;
    }

    private static Entity CreateVisualPrefab(EntityManager em)
    {
        Entity entity = em.CreateEntity(typeof(Prefab), typeof(LocalTransform));
        em.SetComponentData(entity, LocalTransform.Identity);
        return entity;
    }

    private static Entity CreateVisualInstance(EntityManager em)
    {
        Entity entity = em.CreateEntity(typeof(LocalTransform));
        em.SetComponentData(entity, LocalTransform.FromPositionRotationScale(float3.zero, quaternion.identity, 1f));
        return entity;
    }
}
#endif
