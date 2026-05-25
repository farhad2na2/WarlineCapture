using NUnit.Framework;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEditor;

public sealed class MissileLauncherRadarAttackValidationTests
{
    private const string AirLauncherConfigPath = "Assets/Game/Configs/Prefabs/Prefab_UnitGrid_Veh_Missle_Launcher_Air_Config.asset";
    private const string GroundLauncherConfigPath = "Assets/Game/Configs/Prefabs/Prefab_UnitGrid_Veh_Missle_Launcher_Ground_Config.asset";

    [Test]
    public void MissileLauncherConfigs_HaveRadarScaleAttackRange()
    {
        UnitGridAuthoringConfig airConfig = AssetDatabase.LoadAssetAtPath<UnitGridAuthoringConfig>(AirLauncherConfigPath);
        UnitGridAuthoringConfig groundConfig = AssetDatabase.LoadAssetAtPath<UnitGridAuthoringConfig>(GroundLauncherConfigPath);

        Assert.NotNull(airConfig);
        Assert.NotNull(groundConfig);
        Assert.That(airConfig.AttackRange, Is.GreaterThanOrEqualTo(600f));
        Assert.That(groundConfig.AttackRange, Is.GreaterThanOrEqualTo(600f));
        Assert.IsTrue(airConfig.CanAttack);
        Assert.IsTrue(groundConfig.CanAttack);
    }

    [Test]
    public void AirMissileLauncherAttackButton_TargetsAirUnitInsideFriendlyAirRadar()
    {
        using var world = new World("AirMissileLauncherRadarAttack");
        World previousWorld = World.DefaultGameObjectInjectionWorld;
        World.DefaultGameObjectInjectionWorld = world;
        try
        {
            EntityManager em = world.EntityManager;
            Entity launcher = CreateLauncher(em, "Unit_Veh_Missle_Launcher_Air", new int2(10, 10));
            CreateDetector(em, 0, ThreatDetectionKind.Air, new int2(20, 20), 40);
            Entity airTarget = CreateUnit(em, 1, new int2(40, 20), true, true);
            CreateUnit(em, 1, new int2(45, 20), false, true);
            CreateUnit(em, 1, new int2(80, 20), true, true);

            Assert.IsTrue(IssueFocusedMissileLauncherRadarAttack(em, launcher));

            Assert.IsTrue(em.HasComponent<EngageTarget>(launcher));
            EngageTarget engage = em.GetComponentData<EngageTarget>(launcher);
            Assert.AreEqual(airTarget, engage.Target);
            Assert.AreEqual(new int2(40, 20), engage.Cell);
            Assert.AreEqual(1, engage.IsCommanded);
        }
        finally
        {
            World.DefaultGameObjectInjectionWorld = previousWorld;
        }
    }

    [Test]
    public void GroundMissileLauncherAttackButton_TargetsGroundUnitInsideFriendlyGroundRadar()
    {
        using var world = new World("GroundMissileLauncherRadarAttack");
        World previousWorld = World.DefaultGameObjectInjectionWorld;
        World.DefaultGameObjectInjectionWorld = world;
        try
        {
            EntityManager em = world.EntityManager;
            Entity launcher = CreateLauncher(em, "Unit_Veh_Missle_Launcher_Ground", new int2(10, 10));
            CreateDetector(em, 0, ThreatDetectionKind.Ground, new int2(20, 20), 40);
            Entity groundTarget = CreateUnit(em, 1, new int2(35, 20), false, true);
            CreateUnit(em, 1, new int2(32, 20), true, true);
            CreateUnit(em, 1, new int2(80, 20), false, true);

            Assert.IsTrue(IssueFocusedMissileLauncherRadarAttack(em, launcher));

            Assert.IsTrue(em.HasComponent<EngageTarget>(launcher));
            EngageTarget engage = em.GetComponentData<EngageTarget>(launcher);
            Assert.AreEqual(groundTarget, engage.Target);
            Assert.AreEqual(new int2(35, 20), engage.Cell);
            Assert.AreEqual(1, engage.IsCommanded);
        }
        finally
        {
            World.DefaultGameObjectInjectionWorld = previousWorld;
        }
    }

    [Test]
    public void MissileLauncherAttackButton_DoesNothingWithoutMatchingRadarCoverage()
    {
        using var world = new World("MissileLauncherNoRadarCoverage");
        World previousWorld = World.DefaultGameObjectInjectionWorld;
        World.DefaultGameObjectInjectionWorld = world;
        try
        {
            EntityManager em = world.EntityManager;
            Entity launcher = CreateLauncher(em, "Unit_Veh_Missle_Launcher_Air", new int2(10, 10));
            CreateDetector(em, 0, ThreatDetectionKind.Ground, new int2(20, 20), 40);
            CreateUnit(em, 1, new int2(40, 20), true, true);

            Assert.IsFalse(IssueFocusedMissileLauncherRadarAttack(em, launcher));
            Assert.IsFalse(em.HasComponent<EngageTarget>(launcher));
        }
        finally
        {
            World.DefaultGameObjectInjectionWorld = previousWorld;
        }
    }

    [Test]
    public void AttackButtonFallback_ArmsExplicitTargetModeForNormalAttackUnits()
    {
        using var world = new World("AttackButtonFallbackTargetMode");
        World previousWorld = World.DefaultGameObjectInjectionWorld;
        World.DefaultGameObjectInjectionWorld = world;
        try
        {
            EntityManager em = world.EntityManager;
            Entity soldier = CreateUnit(em, 0, new int2(10, 10), false, true);
            em.AddComponentData(soldier, new UnitCombat { CanAttack = 1, AutoEngage = 1 });
            em.AddComponentData(soldier, new UnitAttack
            {
                Range = 4f,
                CooldownSeconds = 1f,
                Damage = 10,
                TraceVisibleSeconds = 0.05f
            });

            Assert.IsFalse(IssueFocusedMissileLauncherRadarAttack(em, soldier));
            Assert.IsTrue(em.GetComponentData<UnitCombat>(soldier).CanAttack != 0);
        }
        finally
        {
            World.DefaultGameObjectInjectionWorld = previousWorld;
        }
    }

    private static Entity CreateLauncher(EntityManager em, string sourceKey, int2 cell)
    {
        Entity entity = CreateUnit(em, 0, cell, false, true);
        em.AddComponentData(entity, new UnitSourcePrefabKey { Value = new FixedString64Bytes(sourceKey) });
        em.AddComponentData(entity, new UnitCombat { CanAttack = 1, AutoEngage = 1 });
        em.AddComponentData(entity, new UnitAttack
        {
            Range = 600f,
            CooldownSeconds = 1f,
            Damage = 100,
            TraceVisibleSeconds = 0.05f
        });
        return entity;
    }

    private static bool IssueFocusedMissileLauncherRadarAttack(EntityManager em, Entity launcher)
    {
        var focusedCommand = new FocusedUnitCommandSystem();
        var targetOrder = new UnitTargetOrderSystem();
        return focusedCommand.TryIssueFocusedMissileLauncherRadarAttack(
            em,
            launcher,
            targetOrder,
            out _);
    }

    private static Entity CreateDetector(EntityManager em, byte factionId, ThreatDetectionKind kind, int2 cell, int radiusCells)
    {
        Entity entity = CreateUnit(em, factionId, cell, false, false);
        em.AddComponentData(entity, new ThreatDetector
        {
            Kind = (byte)kind,
            RadiusCells = radiusCells
        });
        return entity;
    }

    private static Entity CreateUnit(EntityManager em, byte factionId, int2 cell, bool air, bool movable)
    {
        Entity entity = em.CreateEntity();
        em.AddComponentData(entity, new Faction { Id = factionId });
        em.AddComponentData(entity, new UnitGrid { Cell = cell });
        em.AddComponentData(entity, new UnitHealth { Current = 100, Max = 100 });
        em.AddComponentData(entity, LocalTransform.FromPosition(new float3(cell.x, 0f, cell.y)));
        if (movable)
        {
            em.AddComponentData(entity, new UnitMove
            {
                Speed = 5f,
                WalkSpeed = 5f,
                RoadSpeedMultiplier = 1f,
                ArriveDistance = 0.05f
            });
        }
        if (air)
        {
            em.AddComponentData(entity, new UnitAirMovement
            {
                CruiseHeight = 6f,
                RunwayTaxiSpeed = 5f
            });
        }

        return entity;
    }
}
