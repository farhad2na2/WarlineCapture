using Game.Components;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;

namespace Game.Runtime
{
    [BurstCompile]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(UnitAttackSystem))]
    [UpdateBefore(typeof(UnitDeathSystem))]
    public partial struct CampaignMissionTutorialProtectionSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<CampaignMissionFinalePresentationComponent>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            CampaignMissionFinalePresentationComponent finale =
                SystemAPI.GetSingleton<CampaignMissionFinalePresentationComponent>();
            if (finale.Required == 0 || finale.Stage is < 2 or > 3)
                return;

            foreach ((RefRW<UnitHealth> health,
                      RefRO<Faction> faction,
                      RefRO<CampaignMissionUnitRoleComponent> role) in
                     SystemAPI.Query<RefRW<UnitHealth>, RefRO<Faction>,
                         RefRO<CampaignMissionUnitRoleComponent>>())
            {
                if (!ShouldProtect(in finale, faction.ValueRO.Id, role.ValueRO.SessionToken) ||
                    health.ValueRO.Max <= 0)
                    continue;

                health.ValueRW = ApplyProtection(health.ValueRO);
            }
        }

        internal static bool ShouldProtect(
            in CampaignMissionFinalePresentationComponent finale,
            byte factionId,
            in Unity.Collections.FixedString64Bytes sessionToken) =>
            finale.Required != 0 && finale.Stage is >= 2 and <= 3 &&
            finale.SessionToken.Equals(sessionToken) &&
            FactionIdentity.IsPlayerControlled(factionId);

        internal static UnitHealth ApplyProtection(in UnitHealth health)
        {
            UnitHealth protectedHealth = health;
            protectedHealth.Current = math.max(protectedHealth.Current, protectedHealth.Max);
            return protectedHealth;
        }
    }
}
