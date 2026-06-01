using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

[BurstCompile]
[UpdateInGroup(typeof(PresentationSystemGroup))]
public partial struct FactionVisualSystem : ISystem
{
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<FactionTintColor>();
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

        state.Dependency = new UpdateTintJob
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
    private partial struct UpdateTintJob : IJobEntity
    {
        [ReadOnly] public ComponentLookup<Faction> FactionLookup;
        [ReadOnly] public ComponentLookup<Parent> ParentLookup;
        public float4 PlayerColor;
        public float4 EnemyColor;
        public float4 NeutralColor;

        public void Execute(ref FactionTintColor tint, in Parent parent)
        {
            if (!TryResolveFaction(parent.Value, out byte factionId))
            {
                tint.Value = NeutralColor;
                return;
            }

            tint.Value = factionId switch
            {
                0 => PlayerColor,
                1 => EnemyColor,
                _ => NeutralColor
            };
        }

        private bool TryResolveFaction(Entity entity, out byte factionId)
        {
            for (int i = 0; i < 8; i++)
            {
                if (FactionLookup.HasComponent(entity))
                {
                    factionId = FactionLookup[entity].Id;
                    return true;
                }

                if (!ParentLookup.HasComponent(entity))
                    break;

                entity = ParentLookup[entity].Value;
            }

            factionId = 0;
            return false;
        }
    }
}
