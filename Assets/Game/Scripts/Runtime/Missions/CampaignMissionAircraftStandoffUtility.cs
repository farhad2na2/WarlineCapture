using Unity.Mathematics;
namespace Game.Runtime
{
    public static class CampaignMissionAircraftStandoffUtility
    {
        public static float3 ResolveGroundAttackGoal(float3 aircraft,float3 target,float attackRange,float padding)
        {
            float distance=math.max(1f,attackRange-math.max(0f,padding));
            float2 direction=math.normalizesafe(aircraft.xz-target.xz,new float2(-1,0));
            var point=target.xz+direction*distance;
            return new float3(point.x,target.y,point.y);
        }
    }
}
