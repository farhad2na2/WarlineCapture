using System;
using Game.Components;
using Game.Configs;
using Game.Editor;
using Game.Runtime;
using NUnit.Framework;
using Unity.Entities;
using UnityEngine;

public sealed class SkirmishStressPresetTests
{
    public static string RunFocusedValidation()
    {
        var tests = new SkirmishStressPresetTests();
        tests.RecipeRequestedCountsAreExplicitPerScaleAndPhase();
        tests.NormalizationKeepsPlayerScenariosAndHiddenStressIndex();
        tests.CensusReportsMeasuredEntitiesInsteadOfRequestedCounts();
        tests.ForceProjectionWritesRequestedEntriesBeforeSpawn();
        tests.SpawnStallCompletesWhenProgressStopsShortOfRequested();
        tests.SpawnProgressResetPreventsFalseStall();
        tests.ScreenshotNameUsesPhaseLayoutAndScale();
        Debug.Log(SkirmishStressValidation.Run());
        return "[SkirmishStressPreset] result=Passed tests=7";
    }

    [Test]
    public void RecipeRequestedCountsAreExplicitPerScaleAndPhase()
    {
        Assert.AreEqual(50, SkirmishStressRecipe.Requested(SkirmishStressScale.P50, SkirmishStressPhase.Idle, false).Combat);
        Assert.AreEqual(100, SkirmishStressRecipe.Requested(SkirmishStressScale.P100, SkirmishStressPhase.MassMove, false).Combat);
        Assert.AreEqual(200, SkirmishStressRecipe.Requested(SkirmishStressScale.P200, SkirmishStressPhase.DenseCombat, false).Combat);
        Assert.AreEqual(350, SkirmishStressRecipe.Requested(SkirmishStressScale.P350, SkirmishStressPhase.Destruction, false).Combat);
        Assert.AreEqual(500, SkirmishStressRecipe.Requested(SkirmishStressScale.P500, SkirmishStressPhase.Warmup, false).Combat);
        var sequence = SkirmishStressRecipe.Requested(SkirmishStressScale.P100, SkirmishStressPhase.Warmup, true);
        Assert.AreEqual(106, sequence.Combat);
        Assert.AreEqual(6, sequence.Air);
        Assert.AreEqual(6, sequence.Support);
        Assert.AreEqual(12, sequence.Buildings);
        Assert.AreEqual(SkirmishStressRecipe.FixedSeed, 104729);
        Assert.AreEqual(2, SkirmishStressRecipe.ScenarioIndex);
    }

    [Test]
    public void NormalizationKeepsPlayerScenariosAndHiddenStressIndex()
    {
        var unknown = QuickGameConfig.Defaults;
        unknown.ScenarioIndex = 7;
        Assert.AreEqual(0, unknown.NormalizeForBaseAssault().ScenarioIndex);
        var city = QuickGameConfig.Defaults;
        city.ScenarioIndex = 1;
        city.MapSeed = 314159;
        Assert.AreEqual(1, city.NormalizeForBaseAssault().ScenarioIndex);
        Assert.AreEqual(314159, city.NormalizeForBaseAssault().MapSeed);
        var stress = QuickGameConfig.Defaults;
        stress.ScenarioIndex = 2;
        stress.MapSeed = SkirmishStressRecipe.FixedSeed;
        Assert.AreEqual(2, stress.NormalizeForBaseAssault().ScenarioIndex);
        Assert.AreEqual("stress_scale_probe", SkirmishSaveMigration.Normalize(new QuickGameSaveData
        {
            schemaVersion = SkirmishSaveMigration.Version,
            configuration = stress.NormalizeForBaseAssault()
        }).presetId);
    }

    [Test]
    public void CensusReportsMeasuredEntitiesInsteadOfRequestedCounts()
    {
        using var world = new World(nameof(CensusReportsMeasuredEntitiesInsteadOfRequestedCounts));
        var em = world.EntityManager;
        var session = new SkirmishStressSession
        {
            RequestedCombat = 100,
            RequestedAir = 4,
            RequestedSupport = 6,
            RequestedBuildings = 12,
            ActivePhase = SkirmishStressPhaseCode.Idle
        };
        Create(em, 1, 12, "Unit_Chr_Soldier_Male_02_Alt_04", false, false);
        Create(em, 2, 12, "Unit_Chr_Ghillie_Male_01", false, false);
        Create(em, 1, 0, "Unit_Veh_Tank_USA", false, false);
        Create(em, 1, 8, "Unit_Veh_Helicopter_Attack", false, false, air: true);
        Create(em, 1, 8, "Unit_Veh_Truck_Tanker", true, false);
        Create(em, 1, 40, "Building_Barrack", false, true);
        var sample = SkirmishStressCensus.Measure(em, session, 3f);
        Assert.AreEqual(100, sample.RequestedCombat);
        Assert.AreEqual(4, sample.SpawnedCombat);
        Assert.AreEqual(3, sample.AliveCombat);
        Assert.AreEqual(1, sample.DestroyedCombat);
        Assert.AreEqual(96, sample.MissingSpawnCombat);
        Assert.AreEqual(1, sample.SpawnedAir);
        Assert.AreEqual(1, sample.SpawnedSupport);
        Assert.AreEqual(1, sample.SpawnedBuildings);
        StringAssert.Contains("spawnedCombat=4", SkirmishStressCensus.FormatLine(sample));
        StringAssert.Contains("requestedCombat=100", SkirmishStressCensus.FormatLine(sample));
    }

    [Test]
    public void ForceProjectionWritesRequestedEntriesBeforeSpawn()
    {
        using var world = new World(nameof(ForceProjectionWritesRequestedEntriesBeforeSpawn));
        var em = world.EntityManager;
        em.CreateEntity(typeof(SkirmishMatchState));
        var startup = em.CreateEntity(typeof(InitialUnitsSpawnConfig), typeof(CustomGameStartupStateComponent));
        em.AddBuffer<InitialUnitsFactionUnitSpawnEntry>(startup);
        em.AddBuffer<CustomGameFactionUnitSourceSpawnEntry>(startup);
        em.AddBuffer<CustomGameUnitSourceRegistryEntry>(startup).Add(new CustomGameUnitSourceRegistryEntry
        {
            SourceKey = "Unit_Chr_Soldier_Male_02_Alt_04"
        });
        var session = new SkirmishStressSession
        {
            Seed = SkirmishStressRecipe.FixedSeed,
            Scale = 50,
            ActivePhase = SkirmishStressPhaseCode.Idle,
            Layout = SkirmishStressLayoutCode.Spread,
            Sequence = 0,
            RequestedCombat = 50
        };
        Assert.IsTrue(SkirmishStressDirector.TryProjectForces(em, session));
        var units = em.GetBuffer<InitialUnitsFactionUnitSpawnEntry>(startup);
        int requested = 0;
        for (int i = 0; i < units.Length; i++)
            requested += units[i].Count;
        Assert.Greater(units.Length, 0);
        Assert.AreEqual(56, requested, "P50 idle projects 50 combat + 6 support across both sides.");
        Assert.AreEqual(12, em.GetComponentData<InitialUnitsSpawnConfig>(startup).SpawnRadiusCells);
        Assert.AreEqual((uint)SkirmishStressRecipe.FixedSeed, em.GetComponentData<InitialUnitsSpawnConfig>(startup).RandomSeed);
    }

    [Test]
    public void SpawnStallCompletesWhenProgressStopsShortOfRequested()
    {
        using var world = new World(nameof(SpawnStallCompletesWhenProgressStopsShortOfRequested));
        var em = world.EntityManager;
        CreatePartialSpawn(em, requested: 10, spawned: 4);
        var session = new SkirmishStressSession { LastObservedSpawned = 4 };
        Assert.IsFalse(SkirmishStressDirector.TryCompleteSpawn(em, ref session, 7.9f));
        Assert.AreEqual(0, session.SpawnComplete);
        Assert.IsTrue(SkirmishStressDirector.TryCompleteSpawn(em, ref session, 0.2f));
        Assert.AreEqual(1, session.SpawnComplete);
        Assert.AreEqual(1, session.SpawnStalled);
        Assert.AreEqual(4, session.LastObservedSpawned);
    }

    [Test]
    public void SpawnProgressResetPreventsFalseStall()
    {
        using var world = new World(nameof(SpawnProgressResetPreventsFalseStall));
        var em = world.EntityManager;
        CreatePartialSpawn(em, requested: 10, spawned: 7);
        var session = new SkirmishStressSession { LastObservedSpawned = 5, SpawnStallSeconds = 7.5f };
        Assert.IsFalse(SkirmishStressDirector.TryCompleteSpawn(em, ref session, 8.1f));
        Assert.AreEqual(0, session.SpawnComplete);
        Assert.AreEqual(0, session.SpawnStalled);
        Assert.AreEqual(7, session.LastObservedSpawned);
        Assert.AreEqual(0f, session.SpawnStallSeconds);
    }

    [Test]
    public void ScreenshotNameUsesPhaseLayoutAndScale()
    {
        var session = new SkirmishStressSession
        {
            Scale = 100,
            Layout = SkirmishStressLayoutCode.Concentrated
        };
        Assert.AreEqual("e03-densecombat-concentrated-p100.png",
            SkirmishStressEditorProbe.ScreenshotFileName(session, SkirmishStressPhaseCode.DenseCombat));
        session.Layout = SkirmishStressLayoutCode.Spread;
        session.Scale = 50;
        Assert.AreEqual("e03-warmup-spread-p50.png",
            SkirmishStressEditorProbe.ScreenshotFileName(session, SkirmishStressPhaseCode.Warmup));
        Assert.AreEqual("e03-player-setup-two-battles.png", SkirmishStressEditorProbe.PlayerSetupScreenshotFile);
    }

    private static void CreatePartialSpawn(EntityManager em, int requested, int spawned)
    {
        var startup = em.CreateEntity(typeof(InitialUnitsSpawnConfig));
        em.AddBuffer<InitialUnitsFactionUnitSpawnEntry>(startup).Add(new InitialUnitsFactionUnitSpawnEntry
        {
            FactionId = 1,
            Count = requested
        });
        em.AddBuffer<InitialUnitsFactionUnitSpawnProgress>(startup).Add(new InitialUnitsFactionUnitSpawnProgress
        {
            Spawned = spawned
        });
    }

    private static void Create(EntityManager em, byte faction, int health, string key, bool support, bool building, bool air = false)
    {
        var entity = em.CreateEntity(typeof(Faction), typeof(UnitHealth), typeof(UnitSourcePrefabKey));
        em.SetComponentData(entity, new Faction { Id = faction });
        em.SetComponentData(entity, new UnitHealth { Current = health, Max = Mathf.Max(1, health) });
        em.SetComponentData(entity, new UnitSourcePrefabKey { Value = key });
        if (support) em.AddComponent<UnitResourceHauler>(entity);
        if (building) em.AddComponent<RuntimeBuildingCombatTag>(entity);
        if (air) em.AddComponent<UnitAirMovement>(entity);
    }
}
