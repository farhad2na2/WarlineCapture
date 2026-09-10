using Game.Components;
using Game.Missions.Contracts;
using Game.Runtime;
using NUnit.Framework;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

public sealed partial class BuildingDefenseAttackSystemTests
{
    private const string M2 = "saga.ch01.m02.establish_base";
    private const string M3 = "saga.ch01.m03.radar_warning";
    private const string M4 = "saga.ch01.m04.airlift";

    [TestCase(M2)]
    [TestCase(M3)]
    [TestCase(M4)]
    public void MissionAuthoredDefenses_DoNotShootOrPlayAudio_AndRestoreOnExit(string missionId)
    {
        using World world = new("Mission map combat and audio isolation");
        var em = world.EntityManager;
        using var catalog = CreateDefensePolicyMission(em, missionId, false, out Entity gameplay);
        Entity tower = CreateGuardTower(em, float3.zero, FactionIdentity.PlayerFactionId, 1);
        em.AddComponent<OperationMapBuildingComponent>(tower);
        Entity hostile = CreateTarget(em, new float3(5, 0, 0), 100, FactionIdentity.EnemyFactionId);
        Entity parked = CreateAuthoredAudioVehicle(em);
        Entity alreadyDisabled = CreateAuthoredAudioVehicle(em);
        em.AddComponent<Disabled>(alreadyDisabled);
        Entity missionVehicle = CreateAuthoredAudioVehicle(em);
        em.AddComponent<CampaignMissionUnitRoleComponent>(missionVehicle);
        Entity produced = CreateAuthoredAudioVehicle(em);
        em.RemoveComponent<OperationMapAuthoredVehiclePresentation>(produced);
        var policy = world.CreateSystem<CampaignMissionSpawnSystem>();
        var attack = world.CreateSystem<BuildingDefenseAttackSystem>();
        var motion = world.CreateSystem<UnitMotionAudioSystem>();

        for (int cycle = 0; cycle < 2; cycle++)
        {
            em.SetComponentData(gameplay, new RuntimeGameplayStateComponent { PlayRequested = 1 });
            policy.Update(world.Unmanaged);
            Assert.IsTrue(em.HasComponent<CampaignMissionDormantMapDefenseTag>(tower));
            Assert.IsTrue(em.HasComponent<Disabled>(parked));
            Assert.IsFalse(em.HasComponent<CampaignMissionDormantMapUnitTag>(alreadyDisabled));
            Assert.IsFalse(em.HasComponent<Disabled>(missionVehicle));
            Assert.IsFalse(em.HasComponent<Disabled>(produced));

            GetAudioRequests(em).Clear();
            Update(world, attack, cycle * 2 + .1, .1f);
            motion.Update(world.Unmanaged);
            Assert.AreEqual(100, GetHealth(em, hostile));
            Assert.AreEqual(0, em.GetBuffer<BuildingDefenseAttackSlot>(tower).Length);
            var audio = GetAudioRequests(em);
            Assert.AreEqual(2, audio.Length, "Only real mission/produced vehicles should emit engine audio.");
            foreach (var request in audio)
                Assert.IsTrue(request.SourceEntity == missionVehicle || request.SourceEntity == produced);

            em.SetComponentData(gameplay, default(RuntimeGameplayStateComponent));
            policy.Update(world.Unmanaged);
            Assert.IsFalse(em.HasComponent<CampaignMissionDormantMapDefenseTag>(tower));
            Assert.IsFalse(em.HasComponent<Disabled>(parked));
            Assert.IsFalse(em.HasComponent<CampaignMissionDormantMapUnitTag>(parked));
            Assert.IsTrue(em.HasComponent<Disabled>(alreadyDisabled));
        }
        GetAudioRequests(em).Clear();
        Update(world, attack, 5, .1f);
        Assert.AreEqual(90, GetHealth(em, hostile), "Leaving the mission restores ordinary map combat.");
        Assert.AreEqual(tower, GetAudioRequests(em)[0].SourceEntity);
    }

    [TestCase(M3)]
    [TestCase(M4)]
    public void MissionBuiltTower_StillFiresAndPlaysAudioForRealHostile(string missionId)
    {
        using World world = new("Built defenses remain playable");
        var em = world.EntityManager;
        using var catalog = CreateDefensePolicyMission(em, missionId, false, out _);
        Entity tower = CreateGuardTower(em, float3.zero, FactionIdentity.PlayerFactionId, 1);
        Entity hostile = CreateTarget(em, new float3(5, 0, 0), 100, FactionIdentity.EnemyFactionId);
        world.CreateSystem<CampaignMissionSpawnSystem>().Update(world.Unmanaged);
        Assert.IsFalse(em.HasComponent<CampaignMissionDormantMapDefenseTag>(tower));
        Update(world, world.CreateSystem<BuildingDefenseAttackSystem>(), .1, .1f);
        Assert.AreEqual(90, GetHealth(em, hostile));
        Assert.AreEqual(1, em.GetBuffer<BuildingDefenseAttackSlot>(tower)[0].ShotCounter);
        Assert.AreEqual(1, GetAudioRequests(em).Length);
        Assert.AreEqual(tower, GetAudioRequests(em)[0].SourceEntity);
    }

    [TestCase("saga.ch01.m01.first_contact", false)]
    [TestCase(M2, true)]
    public void AuthoredDefense_ExistingCombatMissionAndExplicitM2DefenseRemainActive(string missionId, bool defend)
    {
        using World world = new("Existing combat policy");
        var em = world.EntityManager;
        using var catalog = CreateDefensePolicyMission(em, missionId, defend, out _);
        Entity tower = CreateGuardTower(em, float3.zero, FactionIdentity.PlayerFactionId, 1);
        em.AddComponent<OperationMapBuildingComponent>(tower);
        Entity hostile = CreateTarget(em, new float3(5, 0, 0), 100, FactionIdentity.EnemyFactionId);
        world.CreateSystem<CampaignMissionSpawnSystem>().Update(world.Unmanaged);
        Update(world, world.CreateSystem<BuildingDefenseAttackSystem>(), .1, .1f);
        Assert.IsFalse(em.HasComponent<CampaignMissionDormantMapDefenseTag>(tower));
        Assert.AreEqual(90, GetHealth(em, hostile));
        Assert.AreEqual(tower, GetAudioRequests(em)[0].SourceEntity);
    }

    [TestCase("absent")]
    [TestCase("allied")]
    [TestCase("neutral")]
    [TestCase("dead")]
    [TestCase("aircraft")]
    [TestCase("out-of-range")]
    [TestCase("debug")]
    [TestCase("dead-tower")]
    public void GuardTower_NoEligibleShotMeansNoWeaponSound(string condition)
    {
        using World world = new("Guard audio follows actual eligible shots");
        var em = world.EntityManager;
        Entity tower = CreateGuardTower(em, float3.zero, FactionIdentity.PlayerFactionId, 1);
        if (condition == "dead-tower") em.SetComponentData(tower, new UnitHealth { Current = 0, Max = 700 });
        if (condition != "absent")
        {
            byte faction = condition == "allied" ? FactionIdentity.PlayerFactionId :
                condition == "neutral" ? FactionIdentity.NeutralFactionId : FactionIdentity.EnemyFactionId;
            Entity target = CreateTarget(em, new float3(condition == "out-of-range" ? 200 : 5, 0, 0),
                condition == "dead" ? 0 : 100, faction, condition == "aircraft");
            if (condition == "debug") em.AddComponent<DebugFireTargetTag>(target);
        }
        var attack = world.CreateSystem<BuildingDefenseAttackSystem>();
        Update(world, attack, .1, .1f);
        Update(world, attack, 1, .9f);
        Assert.AreEqual(0, GetAudioRequests(em).Length);
        foreach (var slot in em.GetBuffer<BuildingDefenseAttackSlot>(tower)) Assert.AreEqual(0, slot.ShotCounter);
    }

    [Test]
    public void GuardTower_AudioStopsDuringCooldownAndAfterTargetDisappears()
    {
        using World world = new("No detached guard fire audio");
        var em = world.EntityManager;
        Entity tower = CreateGuardTower(em, float3.zero, FactionIdentity.PlayerFactionId, 1);
        Entity target = CreateTarget(em, new float3(5, 0, 0), 100, FactionIdentity.EnemyFactionId);
        var attack = world.CreateSystem<BuildingDefenseAttackSystem>();
        Update(world, attack, .1, .1f);
        Assert.AreEqual(1, GetAudioRequests(em).Length);
        Update(world, attack, .15, .05f);
        Assert.AreEqual(1, GetAudioRequests(em).Length, "Cooldown must not replay the previous shot.");
        em.DestroyEntity(target);
        Update(world, attack, 1, .85f);
        Assert.AreEqual(1, GetAudioRequests(em).Length, "An expired target must not leave detached shooting audio.");
        Assert.AreEqual(1, em.GetBuffer<BuildingDefenseAttackSlot>(tower)[0].ShotCounter);
    }

    private static BlobAssetReference<CampaignMissionCatalogBlob> CreateDefensePolicyMission(
        EntityManager em, string missionId, bool defend, out Entity gameplay)
    {
        using var builder = new BlobBuilder(Allocator.Temp);
        ref var catalog = ref builder.ConstructRoot<CampaignMissionCatalogBlob>();
        var missions = builder.Allocate(ref catalog.Missions, 1);
        missions[0].MissionId = missionId; missions[0].ScenarioId = "test.scenario"; missions[0].OperationMapId = "test.map";
        missions[0].MissionRuntimeEnabled = 1;
        missions[0].Defense.Enabled = (byte)(missionId == M3 ? 1 : 0);
        missions[0].Defense.AuthoredMapDefensesDormant = 1;
        missions[0].Extraction.Enabled = (byte)(missionId == M4 ? 1 : 0);
        missions[0].Extraction.AuthoredMapDefensesDormant = 1;
        var objectives = builder.Allocate(ref missions[0].Objectives, 1);
        objectives[0].Rule = defend ? MissionObjectiveRuleKind.DefendMissionRole : MissionObjectiveRuleKind.BuildStructure;
        var blob = builder.CreateBlobAssetReference<CampaignMissionCatalogBlob>(Allocator.Persistent);
        Entity root = em.CreateEntity(typeof(CampaignMissionRootComponent), typeof(CampaignMissionRuntimeComponent), typeof(CampaignMissionCatalogComponent));
        em.SetComponentData(root, new CampaignMissionCatalogComponent { Blob = blob });
        em.SetComponentData(root, new CampaignMissionRuntimeComponent { MissionId = missionId, ScenarioId = "test.scenario", OperationMapId = "test.map" });
        gameplay = em.CreateEntity(typeof(RuntimeGameplayStateComponent));
        em.SetComponentData(gameplay, new RuntimeGameplayStateComponent { PlayRequested = 1 });
        em.CreateEntity(typeof(OperationMapMetadataComponent));
        return blob;
    }

    private static Entity CreateAuthoredAudioVehicle(EntityManager em)
    {
        Entity vehicle = em.CreateEntity(typeof(OperationMapAuthoredVehiclePresentation), typeof(UnitGrid), typeof(UnitMove),
            typeof(UnitMovementBehavior), typeof(UnitVehicleMovement), typeof(UnitMoveVisualComponent), typeof(LocalTransform));
        em.SetComponentData(vehicle, LocalTransform.Identity);
        em.SetComponentData(vehicle, new UnitMovementBehavior { UsesVehicleMotion = 1 });
        em.SetComponentData(vehicle, new UnitMoveVisualComponent { IsMoving = 1 });
        return vehicle;
    }
}
