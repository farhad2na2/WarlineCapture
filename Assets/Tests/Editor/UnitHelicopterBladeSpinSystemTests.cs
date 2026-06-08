#if UNITY_INCLUDE_TESTS && UNITY_EDITOR
using System.IO;
using NUnit.Framework;
using Unity.Core;
using UnityEditor;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

public sealed class UnitHelicopterBladeSpinSystemTests
{
    [Test]
    public void AirborneAirUnitRotatesVisibleDetailBlade()
    {
        using var world = new World(nameof(AirborneAirUnitRotatesVisibleDetailBlade));
        EntityManager em = world.EntityManager;
        Entity helicopter = em.CreateEntity(typeof(UnitAirMovement), typeof(UnitMoveVisualComponent), typeof(LocalTransform), typeof(UnitAirComponent));
        em.SetComponentData(helicopter, new UnitAirMovement { CruiseHeight = 6f, RunwayTaxiSpeed = 0f });
        em.SetComponentData(helicopter, new UnitMoveVisualComponent { IsMoving = 0, StillSeconds = 10f });
        em.SetComponentData(helicopter, LocalTransform.FromPosition(new float3(0f, 6f, 0f)));
        em.SetComponentData(helicopter, new UnitAirComponent
        {
            HomePosition = float3.zero,
            HomeInitialized = 1,
            Airborne = 1
        });

        Entity root = em.CreateEntity(typeof(LocalTransform));
        Entity blade = em.CreateEntity(typeof(LocalTransform));
        em.SetComponentData(root, LocalTransform.Identity);
        em.SetComponentData(blade, LocalTransform.Identity);
        em.SetName(root, "Model");
        em.SetName(blade, "Blades_Main_Y");
        DynamicBuffer<Child> children = em.AddBuffer<Child>(root);
        children.Add(new Child { Value = blade });
        em.AddComponentData(helicopter, new UnitDetailedVisualReference { Root = root });

        quaternion before = em.GetComponentData<LocalTransform>(blade).Rotation;
        SystemHandle system = world.CreateSystem<UnitHelicopterBladeSpinSystem>();
        world.SetTime(new TimeData(1d, 0.25f));

        system.Update(world.Unmanaged);

        quaternion after = em.GetComponentData<LocalTransform>(blade).Rotation;
        Assert.Greater(math.length(after.value - before.value), 0.01f, "Helicopter blades must rotate while airborne or hovering, not only while movement flags are set.");
    }

    [Test]
    public void AirborneAirUnitRotatesBakedBladeReference()
    {
        using var world = new World(nameof(AirborneAirUnitRotatesBakedBladeReference));
        EntityManager em = world.EntityManager;
        Entity helicopter = em.CreateEntity(typeof(UnitAirMovement), typeof(UnitMoveVisualComponent), typeof(LocalTransform), typeof(UnitAirComponent));
        em.SetComponentData(helicopter, new UnitAirMovement { CruiseHeight = 6f, RunwayTaxiSpeed = 0f });
        em.SetComponentData(helicopter, new UnitMoveVisualComponent { IsMoving = 0, StillSeconds = 10f });
        em.SetComponentData(helicopter, LocalTransform.FromPosition(new float3(0f, 6f, 0f)));
        em.SetComponentData(helicopter, new UnitAirComponent
        {
            HomePosition = float3.zero,
            HomeInitialized = 1,
            Airborne = 1
        });

        Entity blade = em.CreateEntity(typeof(LocalTransform));
        em.SetComponentData(blade, LocalTransform.Identity);
        DynamicBuffer<UnitHelicopterBladeReference> blades = em.AddBuffer<UnitHelicopterBladeReference>(helicopter);
        blades.Add(new UnitHelicopterBladeReference { Blade = blade, Axis = 1 });

        quaternion before = em.GetComponentData<LocalTransform>(blade).Rotation;
        SystemHandle system = world.CreateSystem<UnitHelicopterBladeSpinSystem>();
        world.SetTime(new TimeData(1d, 0.25f));

        system.Update(world.Unmanaged);

        quaternion after = em.GetComponentData<LocalTransform>(blade).Rotation;
        Assert.Greater(math.length(after.value - before.value), 0.01f, "The baked blade buffer path must rotate airborne helicopters without relying on entity names.");
    }

    [Test]
    public void LandedAirUnitDoesNotRotateBakedBladeReference()
    {
        using var world = new World(nameof(LandedAirUnitDoesNotRotateBakedBladeReference));
        EntityManager em = world.EntityManager;
        Entity helicopter = em.CreateEntity(typeof(UnitAirMovement), typeof(UnitMoveVisualComponent), typeof(LocalTransform), typeof(UnitAirComponent));
        em.SetComponentData(helicopter, new UnitAirMovement { CruiseHeight = 6f, RunwayTaxiSpeed = 0f });
        em.SetComponentData(helicopter, new UnitMoveVisualComponent { IsMoving = 0, StillSeconds = 10f });
        em.SetComponentData(helicopter, LocalTransform.Identity);
        em.SetComponentData(helicopter, new UnitAirComponent
        {
            HomePosition = float3.zero,
            HomeInitialized = 1,
            Airborne = 0,
            TakeoffRolling = 0,
            LandingRolling = 0,
            ReturningHome = 0
        });

        Entity blade = em.CreateEntity(typeof(LocalTransform));
        em.SetComponentData(blade, LocalTransform.Identity);
        DynamicBuffer<UnitHelicopterBladeReference> blades = em.AddBuffer<UnitHelicopterBladeReference>(helicopter);
        blades.Add(new UnitHelicopterBladeReference { Blade = blade, Axis = 1 });

        quaternion before = em.GetComponentData<LocalTransform>(blade).Rotation;
        SystemHandle system = world.CreateSystem<UnitHelicopterBladeSpinSystem>();
        world.SetTime(new TimeData(1d, 0.25f));

        system.Update(world.Unmanaged);

        quaternion after = em.GetComponentData<LocalTransform>(blade).Rotation;
        Assert.Less(math.length(after.value - before.value), 0.0001f, "Landed helicopters must not spin their blades by default.");
    }

    [Test]
    public void GroundedReturningAirUnitDoesNotRotateBakedBladeReference()
    {
        using var world = new World(nameof(GroundedReturningAirUnitDoesNotRotateBakedBladeReference));
        EntityManager em = world.EntityManager;
        Entity helicopter = em.CreateEntity(typeof(UnitAirMovement), typeof(UnitMoveVisualComponent), typeof(LocalTransform), typeof(UnitAirComponent));
        em.SetComponentData(helicopter, new UnitAirMovement { CruiseHeight = 6f, RunwayTaxiSpeed = 0f });
        em.SetComponentData(helicopter, new UnitMoveVisualComponent { IsMoving = 0, StillSeconds = 10f });
        em.SetComponentData(helicopter, LocalTransform.Identity);
        em.SetComponentData(helicopter, new UnitAirComponent
        {
            HomePosition = float3.zero,
            HomeInitialized = 1,
            Airborne = 0,
            TakeoffRolling = 0,
            LandingRolling = 0,
            ReturningHome = 1
        });

        Entity blade = em.CreateEntity(typeof(LocalTransform));
        em.SetComponentData(blade, LocalTransform.Identity);
        DynamicBuffer<UnitHelicopterBladeReference> blades = em.AddBuffer<UnitHelicopterBladeReference>(helicopter);
        blades.Add(new UnitHelicopterBladeReference { Blade = blade, Axis = 1 });

        quaternion before = em.GetComponentData<LocalTransform>(blade).Rotation;
        SystemHandle system = world.CreateSystem<UnitHelicopterBladeSpinSystem>();
        world.SetTime(new TimeData(1d, 0.25f));

        system.Update(world.Unmanaged);

        quaternion after = em.GetComponentData<LocalTransform>(blade).Rotation;
        Assert.Less(math.length(after.value - before.value), 0.0001f, "A grounded return/taxi state must not keep helicopter blades spinning.");
    }

    [Test]
    public void HelicopterPrefabsDoNotUseCompanionBladeSpinner()
    {
        string[] prefabPaths =
        {
            "Assets/PolygonMilitary/Prefabs/Vehicles/SM_Veh_Helicopter_Attack_01.prefab",
            "Assets/PolygonMilitary/Prefabs/Vehicles/SM_Veh_Helicopter_Attack_02.prefab",
            "Assets/PolygonMilitary/Prefabs/Vehicles/SM_Veh_Helicopter_Transport_01.prefab",
            "Assets/Game/Prefabs/Vehicles/Unit_Veh_Helicopter_Attack.prefab",
            "Assets/Game/Prefabs/Vehicles/Unit_Veh_Helicopter_Attack_Small.prefab",
            "Assets/Game/Prefabs/Vehicles/Unit_Veh_Helicopter_Transport.prefab"
        };

        foreach (string prefabPath in prefabPaths)
        {
            string yaml = File.ReadAllText(prefabPath);
            Assert.False(yaml.Contains("HelicopterBladeSpinner", System.StringComparison.Ordinal), $"{prefabPath} must not serialize a companion GameObject blade spinner.");
            Assert.False(yaml.Contains("f7f99853083b3452884227d49b3baa5b", System.StringComparison.Ordinal), $"{prefabPath} must not reference the removed blade spinner script.");
        }
    }

    [Test]
    public void HelicopterUnitPrefabsExposeBakedBladeTransforms()
    {
        string[] prefabPaths =
        {
            "Assets/Game/Prefabs/Vehicles/Unit_Veh_Helicopter_Attack.prefab",
            "Assets/Game/Prefabs/Vehicles/Unit_Veh_Helicopter_Attack_Small.prefab",
            "Assets/Game/Prefabs/Vehicles/Unit_Veh_Helicopter_Transport.prefab"
        };

        foreach (string prefabPath in prefabPaths)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            Assert.NotNull(prefab, $"Missing helicopter unit prefab at {prefabPath}.");

            Transform[] transforms = prefab.GetComponentsInChildren<Transform>(true);
            int bladeCount = 0;
            for (int i = 0; i < transforms.Length; i++)
            {
                if (IsBakedBladeTransformName(transforms[i].name))
                    bladeCount++;
            }

            Assert.GreaterOrEqual(bladeCount, 2, $"{prefabPath} must expose main and tail blade transforms for ECS baking.");
        }
    }

    [Test]
    public void MatchBootstrapProjectsConfiguredFactionVisualColorsToEcs()
    {
        using var world = new World(nameof(MatchBootstrapProjectsConfiguredFactionVisualColorsToEcs));
        FactionVisualSettingsSceneConfigAsset config = ScriptableObject.CreateInstance<FactionVisualSettingsSceneConfigAsset>();
        try
        {
            SerializedObject serialized = new(config);
            serialized.FindProperty("playerColor").colorValue = Color.white;
            serialized.FindProperty("enemyColor").colorValue = new Color(1f, 0.8f, 0.75f, 1f);
            serialized.FindProperty("neutralColor").colorValue = new Color(0.82f, 0.82f, 0.82f, 1f);
            serialized.ApplyModifiedPropertiesWithoutUndo();

            new MatchBootstrapSystem().ProjectFactionVisualConfig(world, config);

            using EntityQuery query = world.EntityManager.CreateEntityQuery(ComponentType.ReadOnly<FactionVisualConfig>());
            FactionVisualConfig projected = query.GetSingleton<FactionVisualConfig>();
            Assert.AreEqual(new float4(1f, 1f, 1f, 1f), projected.PlayerColor);
            Assert.AreEqual(new float4(1f, 0.8f, 0.75f, 1f), projected.EnemyColor);
            Assert.AreEqual(new float4(0.82f, 0.82f, 0.82f, 1f), projected.NeutralColor);
        }
        finally
        {
            Object.DestroyImmediate(config);
        }
    }

    private static bool IsBakedBladeTransformName(string name)
    {
        return !string.IsNullOrEmpty(name) &&
               name.Contains("Blade", System.StringComparison.Ordinal) &&
               (name.EndsWith("_X", System.StringComparison.Ordinal) ||
                name.EndsWith("_Y", System.StringComparison.Ordinal) ||
                name.EndsWith("_Z", System.StringComparison.Ordinal));
    }
}
#endif
