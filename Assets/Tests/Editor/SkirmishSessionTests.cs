using System;
using System.IO;
using Game.Components;
using Game.Composition;
using Game.Configs;
using Game.Runtime;
using Game.UI.Shell.Contracts.Ecs;
using Game.UI.Contracts;
using NUnit.Framework;
using Unity.Entities;
using Unity.Collections;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;

public sealed class SkirmishSessionTests
{
    [Test]
    public void BuildingPlacementRejectsSkirmishMountainsAndKeepsFlatGroundAvailable()
    {
        MapSurfaceDataAsset asset = AssetDatabase.LoadAssetAtPath<MapSurfaceDataAsset>(
            "Assets/Game/Data/MapSurfaces/Match_Map_MapSurfaceData.asset");
        Assert.IsNotNull(asset);
        Assert.IsTrue(asset.TryCreateRuntimeBlobAsset(Allocator.Persistent, out BlobAssetReference<MapSurfaceBlob> blob));
        try
        {
            var surface = new MapSurfaceComponent
            {
                SurfaceBlob = blob,
                GridOrigin = asset.GridOrigin,
                CellSize = asset.CellSize,
                Dimensions = new int2(asset.Dimensions.x, asset.Dimensions.y),
                HasSurfaceData = 1
            };
            Assert.IsFalse(BuildingPlacementAdapterCompositionSystemHelper.IsSkirmishFootprintLevel(
                surface, new RectInt(766, 550, 8, 8)), "Mountain slope must block the whole building footprint.");
            Assert.IsFalse(BuildingPlacementAdapterCompositionSystemHelper.IsSkirmishFootprintLevel(
                surface, new RectInt(768, 546, 2, 2)), "A small building must not fit on a mountain plateau.");
            Assert.IsTrue(BuildingPlacementAdapterCompositionSystemHelper.IsSkirmishFootprintLevel(
                surface, new RectInt(800, 600, 8, 8)), "Ordinary flat ground must remain buildable.");
            Assert.IsTrue(BuildingPlacementAdapterCompositionSystemHelper.IsSkirmishFootprintLevel(
                surface, new RectInt(831, 596, 8, 8)), "The player base area must remain buildable.");
        }
        finally { blob.Dispose(); }
    }
    [Test]
    public void RifleRecruitmentRetainsHelicopterDeliveryWithTheRestrictedSkirmishRoster()
    {
        var preset = UnityEngine.Resources.Load<SkirmishPresetConfig>(SkirmishPresetConfig.ResourceName);
        var roster = preset.buildingPlacement.UnitPrefabRegistryConfig.UnitSpawnPrefabs;
        var rifle = System.Linq.Enumerable.First(roster, prefab =>
            prefab.name == "Unit_Chr_Soldier_Male_02_Alt_04");
        var production = new BuildingProductionQueueCompositionSystemHelper();
        production.ConfigureUnitProductionMetadataResolver(BuildingProductionUnitMetadataPrefabSystemHelper.TryGetMetadata);
        var settings = production.ResolveProductionTransportSettings(rifle, roster, null, null);

        Assert.IsNotNull(settings.TransportPrefab, "Recruitment must not silently fall back to instant spawning.");
        Assert.AreEqual(BuildingProductionQueueCompositionSystemHelper.ProductionTransportMode.Helicopter, settings.Mode);
        using var world = new World(nameof(RifleRecruitmentRetainsHelicopterDeliveryWithTheRestrictedSkirmishRoster));
        world.EntityManager.CreateEntity(typeof(SkirmishMatchState));
        Assert.IsTrue(SkirmishCatalogPolicy.Allows(world.EntityManager, rifle, false));
        Assert.IsFalse(SkirmishCatalogPolicy.Allows(world.EntityManager, settings.TransportPrefab, false),
            "The delivery carrier is presentation, not an extra recruitable Skirmish unit.");
    }

    [Test]
    public void ChangingSetupPreservesAResultWrittenAfterTheSetupWasLoaded()
    {
        string directory = Path.Combine(Path.GetTempPath(), "skirmish-setup-" + Guid.NewGuid().ToString("N"));
        try
        {
            var save = new SaveService(new JsonSaveRepository(directory));
            var store = new QuickCustomGameConfigStore(save);
            var config = store.Current;
            var finished = save.LoadQuickGame();
            finished.lastResult = new SkirmishResultSaveData { sessionId = "new-result", outcome = "Victory", seed = 37 };
            save.SaveQuickGame(finished);
            config.MapSeed = 7919;
            store.Apply(config);
            var loaded = save.LoadQuickGame();
            Assert.AreEqual(7919, loaded.configuration.MapSeed);
            Assert.AreEqual("new-result", loaded.lastResult.sessionId);
            Assert.AreEqual("Victory", loaded.lastResult.outcome);
            Assert.IsFalse(File.Exists(Path.Combine(directory, SaveService.ProfileFileName)));
        }
        finally { if (Directory.Exists(directory)) Directory.Delete(directory, true); }
    }

    [Test]
    public void RetryClearsThePreviousStartFailureWithoutResettingRequestSequence()
    {
        using var world = new World("SkirmishRetryStartup");
        var em = world.EntityManager;
        var boundary = em.CreateEntity(typeof(MatchStartQueueComponent), typeof(MatchStartProgressComponent));
        em.SetComponentData(boundary, new MatchStartQueueComponent { LastRequestId = 17, ActiveRequestId = 17, LastStatus = MatchStartStatusKind.Failed });
        em.AddBuffer<MatchStartResultElement>(boundary).Add(new MatchStartResultElement { RequestId = 17, Status = MatchStartStatusKind.Failed });
        em.AddBuffer<MatchStartRequestElement>(boundary);
        Assert.IsTrue(SkirmishLaunchProjection.TryQueue(em, QuickGameConfig.Defaults));
        var queue = em.GetComponentData<MatchStartQueueComponent>(boundary);
        Assert.AreEqual(17, queue.LastRequestId);
        Assert.AreEqual(MatchStartStatusKind.None, queue.LastStatus);
        Assert.Zero(queue.ActiveRequestId);
        Assert.Zero(em.GetBuffer<MatchStartResultElement>(boundary).Length);
    }

    [Test]
    public void AttackApproachFindsAnOpenFiringPositionInsteadOfTheBlockedBaseCenter()
    {
        var grid=new GridConfig{Width=80,Height=80,CellSize=1};
        using var walkable=new Unity.Collections.NativeArray<GridWalkable>(6400,Unity.Collections.Allocator.Temp);
        using var blocked=new Unity.Collections.NativeBitArray(6400,Unity.Collections.Allocator.Temp);
        var writable=walkable;
        for(int i=0;i<writable.Length;i++)writable[i]=new GridWalkable{Value=1};
        for(int y=35;y<=45;y++)for(int x=35;x<=45;x++)blocked.Set(y*80+x,true);
        Assert.IsTrue(SkirmishCombatApproachSystem.TryFindApproach(grid,walkable,blocked,
            new Unity.Mathematics.float3(5,0,40),new Unity.Mathematics.float3(40,0,40),new Unity.Mathematics.int2(1),16,out var goal));
        Assert.IsFalse(blocked.IsSet(goal.y*80+goal.x));
        Assert.Less(Unity.Mathematics.math.distance(new Unity.Mathematics.float2(goal.x,goal.y),new Unity.Mathematics.float2(40,40)),16);
        blocked.SetBits(0,true,6400);
        Assert.IsFalse(SkirmishCombatApproachSystem.TryFindApproach(grid,walkable,blocked,
            new Unity.Mathematics.float3(5,0,40),new Unity.Mathematics.float3(40,0,40),new Unity.Mathematics.int2(1),16,out _));
    }
    [Test]
    public void InfantryLimitReservesTheWholeProductionBatchForEitherFaction()
    {
        using var world=new World("SkirmishPopulation");
        var em=world.EntityManager;
        var session=em.CreateEntity(typeof(SkirmishMatchState));
        em.SetComponentData(session,new SkirmishMatchState{Phase=SkirmishPhase.Playing});
        var prefab=UnityEngine.Resources.Load<SkirmishPresetConfig>(SkirmishPresetConfig.ResourceName).buildingPlacement.UnitPrefabRegistryConfig.UnitSpawnPrefabs[0];
        try
        {
            foreach(byte faction in new byte[]{1,2})
            {
                for(int i=0;i<20;i++)
                {
                    var unit=em.CreateEntity(typeof(Faction),typeof(UnitHealth),typeof(UnitSourcePrefabKey));
                    em.SetComponentData(unit,new Faction{Id=faction});
                    em.SetComponentData(unit,new UnitHealth{Current=100,Max=100});
                    em.SetComponentData(unit,new UnitSourcePrefabKey{Value=i%2==0?"unit_soldier_variant_a":"unit_soldier_variant_b"});
                }
                var producer=new RuntimeBuildingEntity{OwnerFactionId=faction,
                    Definition=new BuildingDefinition{ProductionSlots=new System.Collections.Generic.List<BuildingDefinition.ProductionSlotDefinition>{new(){Quantity=4}}},
                    PendingProductions=new System.Collections.Generic.List<RuntimeBuildingEntity.PendingProduction>()};
                var buildings=new System.Collections.Generic.Dictionary<int,RuntimeBuildingEntity>{{1,producer}};
                Assert.IsTrue(SkirmishPopulationPolicy.CanQueue(em,buildings,producer,prefab,0));
                producer.PendingProductions.Add(new RuntimeBuildingEntity.PendingProduction{Prefab=prefab,RemainingQuantity=4});
                Assert.IsFalse(SkirmishPopulationPolicy.CanQueue(em,buildings,producer,prefab,0));
                producer.PendingProductions[0].RemainingQuantity=1;
                Assert.IsFalse(SkirmishPopulationPolicy.CanQueue(em,buildings,producer,prefab,0),"Three free spaces cannot accept four soldiers.");
                producer.PendingProductions.Clear();
                Assert.IsTrue(SkirmishPopulationPolicy.CanQueue(em,buildings,producer,prefab,0),"Cancellation releases the reservation.");
            }
        }
        finally { /* The configured prefab is a shared asset, owned by Unity. */ }
    }
    [Test]
    public void SaveRoundTripPreservesSeedAndResultWithoutWritingCampaignProfile()
    {
        string directory=Path.Combine(Path.GetTempPath(),"skirmish-save-"+Guid.NewGuid().ToString("N"));
        try
        {
            var save=new SaveService(new JsonSaveRepository(directory));
            var data=save.LoadQuickGame(); data.configuration.MapSeed=7919;
            data.lastResult=new SkirmishResultSaveData{sessionId="attempt",outcome="Victory",seed=7919};
            save.SaveQuickGame(data);
            var loaded=save.LoadQuickGame();
            Assert.AreEqual(7919,loaded.configuration.MapSeed);
            Assert.AreEqual("attempt",loaded.lastResult.sessionId);
            Assert.AreEqual(QuickGameWinCondition.BaseAssault,loaded.configuration.WinCondition);
            Assert.IsFalse(File.Exists(Path.Combine(directory,SaveService.ProfileFileName)));
        }
        finally { if(Directory.Exists(directory))Directory.Delete(directory,true); }
    }
    [Test]
    public void LegacyAndUnsupportedConfigurationsBecomeTheSupportedPreset()
    {
        var legacy=SkirmishSaveMigration.Normalize(new QuickGameSaveData{enemyCount=3,fogOfWar=true});
        Assert.AreEqual(1,legacy.configuration.EnemyCount); Assert.IsFalse(legacy.configuration.FogOfWar);
        var bad=QuickGameConfig.Defaults; bad.EnemyCount=3;bad.PlayerAutoAIEnabled=true;bad.MapSeed=-1;
        var normalized=bad.NormalizeForBaseAssault();
        Assert.AreEqual(1,normalized.EnemyCount);Assert.IsFalse(normalized.PlayerAutoAIEnabled);
        Assert.AreEqual(QuickGameConfig.Defaults.MapSeed,normalized.MapSeed);
    }
    [Test]
    public void DuplicateDeployKeepsOneSessionAndOriginalSnapshot()
    {
        using var world=new World("SkirmishDuplicateDeploy");var em=world.EntityManager;
        Assert.IsTrue(SkirmishLaunchProjection.TryQueue(em,QuickGameConfig.Defaults));
        var changed=QuickGameConfig.Defaults;changed.MapSeed=37;
        Assert.IsFalse(SkirmishLaunchProjection.TryQueue(em,changed));
        Assert.IsTrue(SkirmishLaunchProjection.TryGet(em,out var entity,out var state));
        Assert.AreEqual(QuickGameConfig.Defaults.MapSeed,state.Seed);
        Assert.AreEqual(state.Seed,em.GetComponentObject<SkirmishLaunchSnapshot>(entity).Configuration.MapSeed);
    }
    [Test]
    public void LaunchUsesExplicitSkirmishIdentity()
    {
        using var world=new World("SkirmishIdentity");
        Assert.IsTrue(SkirmishLaunchProjection.TryQueue(world.EntityManager,QuickGameConfig.Defaults));
        Assert.IsTrue(MatchSceneView.OperationMapLaunchResolver.TryResolve(world,"wrong","wrong","wrong",out var selection,out _,out var error),error);
        Assert.AreEqual(SkirmishLaunchProjection.MissionId,selection.MissionId.ToString());
        Assert.AreEqual(SkirmishLaunchProjection.ScenarioId,selection.ScenarioId.ToString());
    }

    [Test]
    public void OutcomesAreTerminalAndDestructionPrecedesTheDeadline()
    {
        var match=new SkirmishMatchState{Phase=SkirmishPhase.Playing,ElapsedSeconds=899.5f};
        Assert.IsTrue(SkirmishOutcomeRules.Evaluate(ref match,true,false,1,true));
        Assert.AreEqual(SkirmishOutcome.Victory,match.Outcome);
        Assert.IsFalse(SkirmishOutcomeRules.Evaluate(ref match,false,true,20,true));
        Assert.AreEqual(SkirmishOutcome.Victory,match.Outcome);
        Assert.AreEqual(900,match.ElapsedSeconds);
    }
    [Test]
    public void PauseFreezesClockAndBothDeadDraws()
    {
        var match=new SkirmishMatchState{Phase=SkirmishPhase.Playing,ElapsedSeconds=8};
        Assert.IsFalse(SkirmishOutcomeRules.Evaluate(ref match,true,true,30,false));
        Assert.AreEqual(8,match.ElapsedSeconds);
        Assert.IsTrue(SkirmishOutcomeRules.Evaluate(ref match,false,false,0,true));
        Assert.AreEqual(SkirmishEndReason.BothBasesDestroyed,match.Reason);
        Assert.AreEqual(SkirmishOutcome.Draw,match.Outcome);
    }
    [Test]
    public void TimeoutAndSurrenderHaveExplicitReasons()
    {
        var match=new SkirmishMatchState{Phase=SkirmishPhase.Playing,ElapsedSeconds=899};
        Assert.IsTrue(SkirmishOutcomeRules.Evaluate(ref match,true,true,1,true));
        Assert.AreEqual(SkirmishEndReason.TimeLimit,match.Reason);
        match=new SkirmishMatchState{Phase=SkirmishPhase.Playing};
        Assert.IsTrue(SkirmishOutcomeRules.Evaluate(ref match,true,true,0,false,true));
        Assert.AreEqual(SkirmishOutcome.Defeat,match.Outcome);
        Assert.AreEqual(SkirmishEndReason.Surrender,match.Reason);
    }
}
