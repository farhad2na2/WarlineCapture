using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using Game.Components;
using Game.Configs;

namespace Game.Rendering
{
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
            bool useM01ReadabilityTint =
                SystemAPI.TryGetSingleton(out ActiveOperationMapComponent activeMap) &&
                IsM01MissionId(activeMap.MissionId);
            float4 unitPlayerColor = useM01ReadabilityTint
                ? ResolveM01UnitReadableColor(config.PlayerColor)
                : config.PlayerColor;
            float4 unitEnemyColor = useM01ReadabilityTint
                ? ResolveM01UnitReadableColor(config.EnemyColor)
                : config.EnemyColor;

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

            state.Dependency = new UpdateUnitModelBaseTintJob
            {
                FactionLookup = factionLookup,
                ParentLookup = parentLookup,
                PlayerColor = unitPlayerColor,
                EnemyColor = unitEnemyColor,
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

            state.Dependency = new UpdateUnitModelSnivelerTintJob
            {
                FactionLookup = factionLookup,
                ParentLookup = parentLookup,
                PlayerColor = unitPlayerColor,
                EnemyColor = unitEnemyColor,
                NeutralColor = config.NeutralColor
            }.ScheduleParallel(state.Dependency);
        }

        public static float4 ResolveM01UnitReadableColor(float4 factionColor)
        {
            float3 lifted = math.lerp(new float3(1f), factionColor.xyz, 0.65f) * 1.35f;
            return new float4(lifted, factionColor.w);
        }

        public static bool IsM01MissionId(in FixedString64Bytes missionId)
        {
            return missionId.Length == 27 &&
                   missionId[0] == (byte)'s' && missionId[1] == (byte)'a' &&
                   missionId[2] == (byte)'g' && missionId[3] == (byte)'a' &&
                   missionId[4] == (byte)'.' && missionId[5] == (byte)'c' &&
                   missionId[6] == (byte)'h' && missionId[7] == (byte)'0' &&
                   missionId[8] == (byte)'1' && missionId[9] == (byte)'.' &&
                   missionId[10] == (byte)'m' && missionId[11] == (byte)'0' &&
                   missionId[12] == (byte)'1' && missionId[13] == (byte)'.' &&
                   missionId[14] == (byte)'f' && missionId[15] == (byte)'i' &&
                   missionId[16] == (byte)'r' && missionId[17] == (byte)'s' &&
                   missionId[18] == (byte)'t' && missionId[19] == (byte)'_' &&
                   missionId[20] == (byte)'c' && missionId[21] == (byte)'o' &&
                   missionId[22] == (byte)'n' && missionId[23] == (byte)'t' &&
                   missionId[24] == (byte)'a' && missionId[25] == (byte)'c' &&
                   missionId[26] == (byte)'t';
        }

        [BurstCompile]
        [WithAll(typeof(FactionTintTarget))]
        [WithNone(typeof(FactionUnitModelTintTarget))]
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
        [WithAll(typeof(FactionTintTarget), typeof(FactionUnitModelTintTarget))]
        private partial struct UpdateUnitModelBaseTintJob : IJobEntity
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
        [WithNone(typeof(FactionUnitModelTintTarget))]
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

        [BurstCompile]
        [WithAll(typeof(FactionTintTarget), typeof(FactionUnitModelTintTarget))]
        private partial struct UpdateUnitModelSnivelerTintJob : IJobEntity
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
}
