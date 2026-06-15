using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

[BurstCompile]
[UpdateInGroup(typeof(PresentationSystemGroup))]
public partial struct FactionVisualSystem : ISystem
{
    public static bool ProjectConfig(World world, FactionVisualSettingsConfig factionVisualConfig)
    {
        if (world == null || !world.IsCreated || factionVisualConfig == null)
            return false;

        EntityManager em = world.EntityManager;
        FactionVisualConfig config = new()
        {
            PlayerColor = ToFloat4(factionVisualConfig.PlayerColor),
            EnemyColor = ToFloat4(factionVisualConfig.EnemyColor),
            NeutralColor = ToFloat4(factionVisualConfig.NeutralColor)
        };

        using EntityQuery query = em.CreateEntityQuery(ComponentType.ReadWrite<FactionVisualConfig>());
        Entity entity = query.IsEmptyIgnoreFilter
            ? em.CreateEntity(typeof(FactionVisualConfig))
            : query.GetSingletonEntity();
        em.SetComponentData(entity, config);
        return true;
    }

    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<FactionTintTarget>();
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        FactionVisualConfig config = SystemAPI.TryGetSingleton(out FactionVisualConfig singleton)
            ? singleton
            : new FactionVisualConfig
            {
                PlayerColor = new float4(0.12f, 0.72f, 1f, 1f),
                EnemyColor = new float4(1f, 0.35f, 0.2f, 1f),
                NeutralColor = new float4(0.82f, 0.82f, 0.82f, 1f)
            };

        var factionLookup = SystemAPI.GetComponentLookup<Faction>(true);
        var parentLookup = SystemAPI.GetComponentLookup<Parent>(true);

        state.Dependency = new UpdateBaseTintJob
        {
            FactionLookup = factionLookup,
            ParentLookup = parentLookup,
            PlayerColor = config.PlayerColor,
            EnemyColor = config.EnemyColor,
            NeutralColor = config.NeutralColor
        }.ScheduleParallel(state.Dependency);

        state.Dependency = new UpdateSnivelerTintJob
        {
            FactionLookup = factionLookup,
            ParentLookup = parentLookup,
            PlayerColor = config.PlayerColor,
            EnemyColor = config.EnemyColor,
            NeutralColor = config.NeutralColor
        }.ScheduleParallel(state.Dependency);
    }

    [BurstCompile]
    [WithAll(typeof(FactionTintTarget))]
    private partial struct UpdateBaseTintJob : IJobEntity
    {
        [ReadOnly] public ComponentLookup<Faction> FactionLookup;
        [ReadOnly] public ComponentLookup<Parent> ParentLookup;
        public float4 PlayerColor;
        public float4 EnemyColor;
        public float4 NeutralColor;

        public void Execute(ref FactionTintColor tint, in Parent parent)
        {
            tint.Value = FactionVisualColorUtility.ResolveColor(
                parent.Value,
                FactionLookup,
                ParentLookup,
                PlayerColor,
                EnemyColor,
                NeutralColor);
        }
    }

    [BurstCompile]
    [WithAll(typeof(FactionTintTarget))]
    private partial struct UpdateSnivelerTintJob : IJobEntity
    {
        [ReadOnly] public ComponentLookup<Faction> FactionLookup;
        [ReadOnly] public ComponentLookup<Parent> ParentLookup;
        public float4 PlayerColor;
        public float4 EnemyColor;
        public float4 NeutralColor;

        public void Execute(ref FactionSnivelerBaseColor tint, in Parent parent)
        {
            tint.Value = FactionVisualColorUtility.ResolveColor(
                parent.Value,
                FactionLookup,
                ParentLookup,
                PlayerColor,
                EnemyColor,
                NeutralColor);
        }
    }

    private static class FactionVisualColorUtility
    {
        public static float4 ResolveColor(
            Entity entity,
            ComponentLookup<Faction> factionLookup,
            ComponentLookup<Parent> parentLookup,
            float4 playerColor,
            float4 enemyColor,
            float4 neutralColor)
        {
            for (int i = 0; i < 64; i++)
            {
                if (factionLookup.HasComponent(entity))
                {
                    byte factionId = factionLookup[entity].Id;
                    return factionId switch
                    {
                        0 => neutralColor,
                        1 => playerColor,
                        _ => enemyColor
                    };
                }

                if (!parentLookup.HasComponent(entity))
                    break;

                entity = parentLookup[entity].Value;
            }

            return neutralColor;
        }
    }

    private static float4 ToFloat4(Color color)
    {
        return new float4(color.r, color.g, color.b, color.a);
    }
}
