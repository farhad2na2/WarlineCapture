using NUnit.Framework;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using Game.Components;
using Game.Configs;
using Game.Runtime;
using Game.UI.Contracts;

public sealed class MapVehiclePlacementStartupCompletionTests
{
    public static void RunFocusedValidation()
    {
        var tests = new MapVehiclePlacementStartupCompletionTests();
        tests.EmptyPlacementConfigCompletesAfterAuthoringRootIsHidden();
        tests.PlayerPlacementFindsNeutralAuthoredVehicleForOwnershipAdoption();
        tests.EntityPresentationVehicleUsesStablePlacementIdentityBeforeTransformDistance();
        tests.EntityPresentationTransportUsesStableIdentityAndTransportRosterSelection();
        tests.AdoptionDoesNotClaimSpawnedOrDistantVehicles();
        UnityEngine.Debug.Log("[MapVehiclePlacementStartupCompletionValidation] result=Passed tests=5");
    }

    [Test]
    public void EmptyPlacementConfigCompletesAfterAuthoringRootIsHidden()
    {
        MapVehiclePlacementConfig config = ScriptableObject.CreateInstance<MapVehiclePlacementConfig>();
        GameObject root = new("Vehicles");
        World world = new("MapVehiclePlacementStartupCompletionTests");
        try
        {
            var unitPrefabContext = new RuntimeUnitPrefabSystem.Context(
                spawnPrefabSystem: default,
                tryGetEntityManager: TryGetEntityManager,
                ensureEntityQueries: null,
                createSpawnPrefabContext: null);
            MapVehiclePlacementSpawnPrefabSystemHelper system = new();
            var context = new MapVehiclePlacementSpawnPrefabSystemHelper.Context(
                config,
                root.transform,
                unitPrefabSystem: default,
                unitPrefabContext,
                tryGetGridData: null,
                logWarning: null);

            Assert.IsFalse(system.IsCompleteFor(config, root.transform));

            system.Update(context);
            Assert.IsTrue(system.IsCompleteFor(config, root.transform));
            Assert.IsFalse(root.activeSelf);
        }
        finally
        {
            world.Dispose();
            Object.DestroyImmediate(root);
            Object.DestroyImmediate(config);
        }

        bool TryGetEntityManager(out EntityManager entityManager)
        {
            entityManager = world.EntityManager;
            return true;
        }
    }

    [Test]
    public void PlayerPlacementFindsNeutralAuthoredVehicleForOwnershipAdoption()
    {
        using World world = new("MapVehiclePlacementAuthoredAdoptionTests");
        EntityManager em = world.EntityManager;
        float3 position = new(842f, 1f, 378f);
        Entity authored = CreateVehicle(em, position, FactionIdentity.NeutralFactionId, Entity.Null);
        MapVehiclePlacementConfigEntry placement = CreatePlacement(position, FactionIdentity.PlayerFactionId);

        Assert.IsTrue(MapVehiclePlacementSpawnPrefabSystemHelper.TryFindAuthoredVehicleEntity(
            em,
            -1,
            placement,
            default,
            out Entity resolved));
        Assert.AreEqual(authored, resolved);

        Entity prefab = em.CreateEntity();
        using (EntityCommandBuffer ecb = new(Allocator.Temp))
        {
            MapVehiclePlacementSpawnPrefabSystemHelper.ConfigureAdoptedVehicle(
                em,
                ecb,
                resolved,
                prefab,
                placement.FactionId,
                new int2(21, 9),
                position);
            ecb.Playback(em);
        }

        Assert.AreEqual(FactionIdentity.PlayerFactionId, em.GetComponentData<Faction>(resolved).Id);
        Assert.AreEqual(prefab, em.GetComponentData<UnitRespawnPrefab>(resolved).Prefab);
        Assert.AreEqual(new int2(21, 9), em.GetComponentData<UnitGrid>(resolved).Cell);
    }

    [Test]
    public void EntityPresentationVehicleUsesStablePlacementIdentityBeforeTransformDistance()
    {
        using World world = new("MapVehiclePlacementStableIdentityAdoptionTests");
        EntityManager em = world.EntityManager;
        float3 configuredPosition = new(842f, 1f, 378f);
        Entity authored = CreateVehicle(
            em,
            configuredPosition + new float3(6f, 0f, 0f),
            FactionIdentity.NeutralFactionId,
            Entity.Null);
        Entity visualRoot = em.CreateEntity(typeof(OperationMapEntityPresentationIdentity));
        em.SetComponentData(visualRoot, new OperationMapEntityPresentationIdentity
        {
            OperationMapId = new FixedString128Bytes("opmap.skirmish.desert_base_01"),
            SourceGlobalObjectId = new FixedString128Bytes("GlobalObjectId_Vehicle_17"),
            Role = 1,
            PlacementIndex = 17
        });
        em.AddComponent<OperationMapAuthoredVehiclePresentation>(authored);
        em.AddComponentData(authored, new UnitDetailedVisualReference { Root = visualRoot });
        MapVehiclePlacementConfigEntry placement = CreatePlacement(
            configuredPosition,
            FactionIdentity.PlayerFactionId);

        Assert.IsTrue(MapVehiclePlacementSpawnPrefabSystemHelper.TryFindAuthoredVehicleEntity(
            em,
            17,
            placement,
            default,
            out Entity resolved));
        Assert.AreEqual(authored, resolved);

        Entity prefab = em.CreateEntity();
        using (EntityCommandBuffer ecb = new(Allocator.Temp))
        {
            MapVehiclePlacementSpawnPrefabSystemHelper.ConfigureAdoptedVehicle(
                em,
                ecb,
                resolved,
                prefab,
                placement.FactionId,
                new int2(21, 9),
                configuredPosition);
            ecb.Playback(em);
        }
        em.AddComponentData(authored, new UnitSourcePrefabKey
        {
            Value = new FixedString64Bytes("Unit_Veh_Tank_USA")
        });
        em.AddComponentData(authored, new LocalToWorld
        {
            Value = float4x4.Translate(configuredPosition)
        });

        var selection = new MatchHudSquadTraySelectionUiSystemHelper();
        var view = new TestSquadTrayView();
        var selectionState = new SelectionStateCompositionSystemHelper();
        var focusedLifecycle = new FocusedUnitLifecycleCompositionSystemHelper();
        var context = new MatchHudSquadTraySelectionUiSystemHelper.Context(
            worldCamera: null,
            TryGetEntityManager,
            ensureSelectionDependencies: _ => { },
            clearCurrentSelection: (_, _) => { },
            clearSelectedBuilding: () => { },
            applyHudSelection: (_, _) => { },
            applyHudSquadSelection: _ => { },
            logSelectionDiagnostic: null,
            selectionState,
            focusedLifecycle);
        selection.SelectSlot(context, view, MatchHudSquadTraySlot.CombatVehicles);
        Assert.AreEqual(MatchHudSquadTraySlot.CombatVehicles, view.SelectedSlot);
        Assert.IsTrue(em.HasComponent<SelectedUnitTag>(authored));

        Assert.IsFalse(MapVehiclePlacementSpawnPrefabSystemHelper.TryFindAuthoredVehicleEntity(
            em,
            16,
            placement,
            default,
            out _), "A migrated vehicle must not fall back to proximity when its stable identity belongs to another placement.");

        bool TryGetEntityManager(out EntityManager entityManager)
        {
            entityManager = em;
            return true;
        }
    }

    [Test]
    public void AdoptionDoesNotClaimSpawnedOrDistantVehicles()
    {
        using World world = new("MapVehiclePlacementAuthoredAdoptionGuardTests");
        EntityManager em = world.EntityManager;
        float3 target = new(842f, 1f, 378f);
        Entity prefab = em.CreateEntity();
        CreateVehicle(em, target, FactionIdentity.PlayerFactionId, prefab);
        CreateVehicle(em, target + new float3(3f, 0f, 0f), FactionIdentity.NeutralFactionId, Entity.Null);
        MapVehiclePlacementConfigEntry placement = CreatePlacement(target, FactionIdentity.PlayerFactionId);

        Assert.IsFalse(MapVehiclePlacementSpawnPrefabSystemHelper.TryFindAuthoredVehicleEntity(
            em,
            -1,
            placement,
            default,
            out _));
    }

    [Test]
    public void EntityPresentationTransportUsesStableIdentityAndTransportRosterSelection()
    {
        using World world = new("MapVehiclePlacementTransportStableIdentityTests");
        EntityManager em = world.EntityManager;
        float3 configuredPosition = new(790f, 1f, 426f);
        Entity transport = CreateVehicle(
            em,
            configuredPosition + new float3(-8f, 0f, 4f),
            FactionIdentity.NeutralFactionId,
            Entity.Null);
        Entity visualRoot = em.CreateEntity(typeof(OperationMapEntityPresentationIdentity));
        em.SetComponentData(visualRoot, new OperationMapEntityPresentationIdentity
        {
            OperationMapId = new FixedString128Bytes("opmap.skirmish.desert_base_01"),
            SourceGlobalObjectId = new FixedString128Bytes("GlobalObjectId_Vehicle_18"),
            Role = 1,
            PlacementIndex = 18
        });
        em.AddComponent<OperationMapAuthoredVehiclePresentation>(transport);
        em.AddComponentData(transport, new UnitDetailedVisualReference { Root = visualRoot });
        MapVehiclePlacementConfigEntry placement = CreatePlacement(
            configuredPosition,
            FactionIdentity.PlayerFactionId);

        Assert.IsTrue(MapVehiclePlacementSpawnPrefabSystemHelper.TryFindAuthoredVehicleEntity(
            em,
            18,
            placement,
            default,
            out Entity resolved));
        Assert.AreEqual(transport, resolved);

        Entity prefab = em.CreateEntity();
        using (EntityCommandBuffer ecb = new(Allocator.Temp))
        {
            MapVehiclePlacementSpawnPrefabSystemHelper.ConfigureAdoptedVehicle(
                em,
                ecb,
                resolved,
                prefab,
                placement.FactionId,
                new int2(19, 11),
                configuredPosition);
            ecb.Playback(em);
        }
        em.AddComponentData(transport, new UnitSourcePrefabKey
        {
            Value = new FixedString64Bytes("Unit_Veh_Helicopter_Transport")
        });
        em.AddComponent<UnitAirMovement>(transport);
        em.AddComponentData(transport, new UnitTransportCapacity { SoldierCapacity = 6 });
        em.AddComponentData(transport, new LocalToWorld
        {
            Value = float4x4.Translate(configuredPosition)
        });

        var selection = new MatchHudSquadTraySelectionUiSystemHelper();
        var view = new TestSquadTrayView();
        var selectionState = new SelectionStateCompositionSystemHelper();
        var focusedLifecycle = new FocusedUnitLifecycleCompositionSystemHelper();
        var context = new MatchHudSquadTraySelectionUiSystemHelper.Context(
            worldCamera: null,
            TryGetEntityManager,
            ensureSelectionDependencies: _ => { },
            clearCurrentSelection: (_, _) => { },
            clearSelectedBuilding: () => { },
            applyHudSelection: (_, _) => { },
            applyHudSquadSelection: _ => { },
            logSelectionDiagnostic: null,
            selectionState,
            focusedLifecycle);
        selection.SelectSlot(context, view, MatchHudSquadTraySlot.Transport);

        Assert.AreEqual(MatchHudSquadTraySlot.Transport, view.SelectedSlot);
        Assert.IsTrue(em.HasComponent<SelectedUnitTag>(transport));

        bool TryGetEntityManager(out EntityManager entityManager)
        {
            entityManager = em;
            return true;
        }
    }

    private static Entity CreateVehicle(
        EntityManager em,
        float3 position,
        byte faction,
        Entity respawnPrefab)
    {
        Entity entity = em.CreateEntity(
            typeof(Faction),
            typeof(UnitGrid),
            typeof(UnitMove),
            typeof(UnitMovementBehavior),
            typeof(UnitRespawnPrefab),
            typeof(LocalTransform));
        em.SetComponentData(entity, new Faction { Id = faction });
        em.SetComponentData(entity, new UnitMovementBehavior { UsesVehicleMotion = 1 });
        em.SetComponentData(entity, new UnitRespawnPrefab { Prefab = respawnPrefab });
        em.SetComponentData(entity, LocalTransform.FromPosition(position));
        return entity;
    }

    private static MapVehiclePlacementConfigEntry CreatePlacement(float3 position, byte faction)
    {
        return new MapVehiclePlacementConfigEntry(
            "Map/Vehicles/Tank",
            "Unit_Veh_Tank_USA",
            null,
            faction,
            position,
            position,
            Vector3.zero,
            Vector3.one);
    }

    private sealed class TestSquadTrayView : IMatchHudSquadTrayView
    {
        public MatchHudSquadTraySlot SelectedSlot { get; private set; } = MatchHudSquadTraySlot.None;

        public void Bind(System.Action<MatchHudSquadTraySlot> cardClicked) { }
        public void ClearActiveSlot() => SelectedSlot = MatchHudSquadTraySlot.None;
        public bool ContainsScreenPoint(Vector2 screenPosition) => false;
        public void FlashDisabled(MatchHudSquadTraySlot slot) { }
        public void SetSelectedSlot(MatchHudSquadTraySlot selectedSlot) => SelectedSlot = selectedSlot;
        public bool TryGetPortraitSprite(MatchHudSquadTraySlot slot, out Sprite sprite)
        {
            sprite = null;
            return false;
        }
        public void Unbind() { }
    }
}
