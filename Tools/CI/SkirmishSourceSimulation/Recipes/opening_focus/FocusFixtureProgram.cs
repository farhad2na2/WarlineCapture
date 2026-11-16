using System;
using Game.Components;
using Game.Configs;
using Game.Runtime;
using Game.Composition.SourceHostProbe;
using Game.Skirmish.Contracts;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

internal static class FocusFixtureProgram
{
    private static int nextEntityIndex = 1;
    private static int passedGuards;

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }

    private static Entity Import(EntityManager manager)
    {
        var entity = new Entity { Index = nextEntityIndex++, Version = 1 };
        manager.ImportEntity(entity);
        return entity;
    }

    private static Entity AddRequest(EntityManager manager)
    {
        Entity entity = Import(manager);
        manager.AddComponentData(entity, new RuntimeCameraFocusRequestComponent());
        return entity;
    }

    private static Entity AddFocusActor(EntityManager manager, float3 position)
    {
        Entity entity = Import(manager);
        manager.AddComponentData(entity, LocalTransform.FromPosition(position));
        return entity;
    }

    private static void MissingEntityGuard()
    {
        var manager = new EntityManager();
        World fixtureWorld = manager.World;
        Require(fixtureWorld != null && fixtureWorld.IsCreated, "Explicit missing-entity fixture world unavailable.");
        Entity request = AddRequest(manager);
        var before = manager.GetComponentData<RuntimeCameraFocusRequestComponent>(request);
        var owner = new SkirmishPresentationSystemFocusCompileProbe { EntityManager = manager };
        bool result = owner.InvokeFocus(new Entity { Index = 60001, Version = 1 }, true,
            SkirmishPresetConfig.IndustrialBasinScenarioIndex, false, false);
        var after = manager.GetComponentData<RuntimeCameraFocusRequestComponent>(request);
        Require(!result, "Missing target entity must reject the focus request.");
        Require(before.Requested == after.Requested && before.UseExplicitPerspective == after.UseExplicitPerspective &&
            before.World.Equals(after.World) && before.Perspective.Equals(after.Perspective),
            "Missing target entity changed the request singleton.");
        passedGuards++;
    }

    private static void MissingRequestGuard()
    {
        var manager = new EntityManager();
        World fixtureWorld = manager.World;
        Require(fixtureWorld != null && fixtureWorld.IsCreated, "Explicit missing-request fixture world unavailable.");
        Entity target = AddFocusActor(manager, new float3(0, 0, 0));
        var owner = new SkirmishPresentationSystemFocusCompileProbe { EntityManager = manager };
        Require(!owner.InvokeFocus(target, true, SkirmishPresetConfig.IndustrialBasinScenarioIndex, false, false),
            "Missing request singleton must reject the focus request.");
        passedGuards++;
    }

    private static void DuplicateRequestGuard()
    {
        var manager = new EntityManager();
        World fixtureWorld = manager.World;
        Require(fixtureWorld != null && fixtureWorld.IsCreated, "Explicit duplicate-request fixture world unavailable.");
        Entity target = AddFocusActor(manager, new float3(0, 0, 0));
        AddRequest(manager);
        AddRequest(manager);
        var owner = new SkirmishPresentationSystemFocusCompileProbe { EntityManager = manager };
        Require(!owner.InvokeFocus(target, true, SkirmishPresetConfig.IndustrialBasinScenarioIndex, false, false),
            "Ambiguous request owners must reject the focus request.");
        passedGuards++;
    }

    private static void MissingTransformGuard()
    {
        var manager = new EntityManager();
        World fixtureWorld = manager.World;
        Require(fixtureWorld != null && fixtureWorld.IsCreated, "Explicit missing-transform fixture world unavailable.");
        Entity target = Import(manager);
        Entity request = AddRequest(manager);
        var before = manager.GetComponentData<RuntimeCameraFocusRequestComponent>(request);
        var owner = new SkirmishPresentationSystemFocusCompileProbe { EntityManager = manager };
        Require(!owner.InvokeFocus(target, true, 3, false, false),
            "A source-census target without LocalTransform must reject the focus request.");
        var after = manager.GetComponentData<RuntimeCameraFocusRequestComponent>(request);
        Require(before.Requested == after.Requested && before.UseExplicitPerspective == after.UseExplicitPerspective &&
            before.World.Equals(after.World) && before.Perspective.Equals(after.Perspective),
            "Missing LocalTransform changed the request singleton.");
        passedGuards++;
    }

    private static void PositiveAuthoredArmyFocus(bool fixtureMobilePlatform)
    {
        SourceHostFixturePlatform.IsMobile = fixtureMobilePlatform;
        var manager = new EntityManager();
        World fixtureWorld = manager.World;
        Require(fixtureWorld != null && fixtureWorld.IsCreated, "Explicit positive fixture world unavailable.");
        AddRequest(manager);

        const string fixtureSession = "S073-focus-explicit-fixture";
        const byte fixtureFaction = 1;
        Entity baseActor = AddFocusActor(manager, new float3(0, 0, 0));
        manager.AddComponentData(baseActor, new SkirmishAttemptOwnedComponent
        {
            SessionId = fixtureSession,
            StableObjectId = "fixture-player-base",
            FactionId = fixtureFaction,
            IsStructure = 1
        });
        manager.AddComponentData(baseActor, new SkirmishVisualSpawnedComponent { Spawned = 1 });
        manager.AddComponentData(baseActor, new SkirmishUnitRoleComponent
        {
            Role = SkirmishRoleKind.Rifle,
            Category = SkirmishPopulationCategory.Infantry
        });
        manager.AddComponentData(baseActor, new UnitHealth { Current = 100, Max = 100 });

        Entity nearbyRifle = AddFocusActor(manager, new float3(10, 0, 10));
        manager.AddComponentData(nearbyRifle, new SkirmishAttemptOwnedComponent
        {
            SessionId = fixtureSession,
            StableObjectId = "fixture-player-rifle",
            FactionId = fixtureFaction
        });
        manager.AddComponentData(nearbyRifle, new SkirmishUnitRoleComponent
        {
            Role = SkirmishRoleKind.Rifle,
            Category = SkirmishPopulationCategory.Infantry
        });
        manager.AddComponentData(nearbyRifle, new UnitHealth { Current = 100, Max = 100 });

        var owner = new SkirmishPresentationSystemFocusCompileProbe { EntityManager = manager };
        Require(owner.InvokeFocus(baseActor, true, SkirmishPresetConfig.IndustrialBasinScenarioIndex, false, false),
            "Authored player base and nearby infantry fixture should produce an opening focus.");
        using (var requestQuery = manager.CreateEntityQuery(typeof(RuntimeCameraFocusRequestComponent)))
        {
            Require(requestQuery.CalculateEntityCount() == 1, "Positive fixture lost unique request ownership.");
            var request = requestQuery.GetSingleton<RuntimeCameraFocusRequestComponent>();
            Require(request.Requested == 1 && request.UseExplicitPerspective == 1,
                "Positive fixture did not queue an explicit focus request.");
            Require(math.distance(request.World, new float3(5, 0, 5)) < .001f,
                "Positive fixture focus center did not match authored base plus nearby actor bounds.");
            float expectedHeight = fixtureMobilePlatform ? 40f : 60f;
            Require(math.abs(request.Perspective.x - expectedHeight) < .001f,
                "Positive fixture opening height did not match the caller-supplied mobile-limit fact.");
        }
    }

    private static void PositiveSourceCensusBaseFocus(bool fixtureMobilePlatform)
    {
        SourceHostFixturePlatform.IsMobile = fixtureMobilePlatform;
        var manager = new EntityManager();
        World fixtureWorld = manager.World;
        Require(fixtureWorld != null && fixtureWorld.IsCreated, "Explicit source-census fixture world unavailable.");
        Entity requestEntity = AddRequest(manager);
        Entity censusBase = AddFocusActor(manager, new float3(7, 2, -4));
        Require(!manager.HasComponent<SkirmishAttemptOwnedComponent>(censusBase) &&
            !manager.HasComponent<SkirmishVisualSpawnedComponent>(censusBase),
            "Source-census base fixture must remain non-attempt-owned and have no visual-spawn tag.");

        var owner = new SkirmishPresentationSystemFocusCompileProbe { EntityManager = manager };
        Require(owner.InvokeFocus(censusBase, true, 3, false, false),
            "Scenario 3 source-census base with LocalTransform should produce an opening focus.");
        using (var requestQuery = manager.CreateEntityQuery(typeof(RuntimeCameraFocusRequestComponent)))
        {
            Require(requestQuery.CalculateEntityCount() == 1 && requestQuery.GetSingletonEntity().Equals(requestEntity),
                "Source-census fixture must preserve its unique camera-request owner.");
            var request = requestQuery.GetSingleton<RuntimeCameraFocusRequestComponent>();
            Require(request.Requested == 1 && request.UseExplicitPerspective == 1,
                "Source-census fixture did not queue an explicit focus request.");
            Require(math.distance(request.World, new float3(27, 2, 2)) < .001f,
                "Scenario 3 non-attempt-owned focus must retain the original opening offset.");
            float expectedHeight = fixtureMobilePlatform ? 40f : 60f;
            Require(math.abs(request.Perspective.x - expectedHeight) < .001f,
                "Scenario 3 source-census opening height did not match caller-supplied mobile fact.");
        }
    }

    public static int Main()
    {
        string stage = "missing-entity";
        try
        {
            MissingEntityGuard();
            stage = "missing-request";
            MissingRequestGuard();
            stage = "duplicate-request";
            DuplicateRequestGuard();
            stage = "missing-transform";
            MissingTransformGuard();
            stage = "positive-authored-focus-nonmobile";
            PositiveAuthoredArmyFocus(false);
            stage = "positive-authored-focus-mobile";
            PositiveAuthoredArmyFocus(true);
            stage = "positive-source-census-base-nonmobile";
            PositiveSourceCensusBaseFocus(false);
            stage = "positive-source-census-base-mobile";
            PositiveSourceCensusBaseFocus(true);
            Console.WriteLine("[S073OpeningFocusFixture] result=Passed guards=" + passedGuards +
                " positive_nonmobile=Passed positive_mobile=Passed source_census_nonmobile=Passed " +
                "source_census_mobile=Passed scope=ExplicitSourceHostFixtureOnly");
            return 0;
        }
        catch (Exception error)
        {
            Console.Error.WriteLine("[S073OpeningFocusFixture] result=Failed stage=" + stage +
                " guards_passed=" + passedGuards + " error=" + error);
            return 1;
        }
    }
}
