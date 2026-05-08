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

        state.Dependency = new UpdateTintJob
        {
            FactionLookup = factionLookup,
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
        public float4 PlayerColor;
        public float4 EnemyColor;
        public float4 NeutralColor;

        public void Execute(ref FactionTintColor tint, in Parent parent)
        {
            if (!FactionLookup.HasComponent(parent.Value))
            {
                tint.Value = NeutralColor;
                return;
            }

            byte factionId = FactionLookup[parent.Value].Id;
            tint.Value = factionId switch
            {
                0 => PlayerColor,
                1 => EnemyColor,
                _ => NeutralColor
            };
        }
    }
}
