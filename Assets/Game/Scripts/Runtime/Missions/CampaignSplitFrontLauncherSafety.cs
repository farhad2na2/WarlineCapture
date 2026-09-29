using Game.Components;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
namespace Game.Runtime
{
    /// <summary>Normal attack orders with attempt-owned range and civilian protection.</summary>
    public static class CampaignSplitFrontLauncherSafety
    {
        public static bool IsSafe(EntityManager em, Entity launcher, Entity target)
        {
            if(!em.Exists(launcher)||!em.Exists(target)||!em.HasComponent<SplitFrontLauncherCommandState>(launcher)||
                !em.HasComponent<GroundMissileLauncherComponent>(launcher)||!em.HasComponent<LocalTransform>(launcher)||
                !em.HasComponent<LocalTransform>(target)||!em.HasComponent<UnitHealth>(launcher)||em.GetComponentData<UnitHealth>(launcher).Current<=0||
                !em.HasComponent<UnitHealth>(target)||em.GetComponentData<UnitHealth>(target).Current<=0||
                !em.HasComponent<Faction>(target)||!FactionIdentity.IsHostileToPlayer(em.GetComponentData<Faction>(target).Id)||
                !em.HasComponent<CampaignMissionUnitRoleComponent>(target))return false;
            var role=em.GetComponentData<CampaignMissionUnitRoleComponent>(target);
            if(!em.HasComponent<CampaignMissionUnitRoleComponent>(launcher)||
                !em.GetComponentData<CampaignMissionUnitRoleComponent>(launcher).SessionToken.Equals(role.SessionToken))return false;
            if(em.HasComponent<CampaignMissionCombatSuppressedTag>(target))return false;
            var config=em.GetComponentData<GroundMissileLauncherComponent>(launcher);var command=em.GetComponentData<SplitFrontLauncherCommandState>(launcher);
            var point=em.GetComponentData<LocalTransform>(target).Position;
            float distance=math.distance(em.GetComponentData<LocalTransform>(launcher).Position.xz,point.xz);
            if(distance<config.MinRange||distance>config.MaxRange||math.distance(point.xz,command.ProtectedCenter.xz)<=command.ProtectedRadius+config.DamageRadius)return false;
            using var civilians=em.CreateEntityQuery(typeof(Faction),typeof(UnitHealth),typeof(LocalTransform));
            using var entities=civilians.ToEntityArray(Unity.Collections.Allocator.Temp);
            foreach(var entity in entities)
                if(em.GetComponentData<Faction>(entity).Id==0&&em.GetComponentData<UnitHealth>(entity).Current>0&&
                    math.distance(point.xz,em.GetComponentData<LocalTransform>(entity).Position.xz)<=config.DamageRadius+2)return false;
            return true;
        }
        public static bool TryIssueAttack(EntityManager em,Entity launcher,Entity target)
        {
            if(!em.HasComponent<SplitFrontLauncherCommandState>(launcher))return true;
            if(!IsSafe(em,launcher,target))return false;
            var command=em.GetComponentData<SplitFrontLauncherCommandState>(launcher);
            var state=em.GetComponentData<GroundMissileLauncherStateComponent>(launcher);
            if(state.Phase==(byte)GroundMissileLauncherPhase.Preparing&&state.TargetEntity!=target)
            {state.Phase=(byte)GroundMissileLauncherPhase.Idle;state.TargetEntity=Entity.Null;state.Timer=0;em.SetComponentData(launcher,state);}
            command.CommandedTarget=target;command.AttackOrders++;em.SetComponentData(launcher,command);
            return true;
        }
        public static bool MayFire(EntityManager em,Entity launcher,Entity target)=>!em.HasComponent<SplitFrontLauncherCommandState>(launcher)||
            em.GetComponentData<SplitFrontLauncherCommandState>(launcher).CommandedTarget==target&&IsSafe(em,launcher,target);
        public static bool Stop(EntityManager em,Entity launcher)
        {
            if(!em.Exists(launcher)||!em.HasComponent<SplitFrontLauncherCommandState>(launcher))return false;
            var state=em.GetComponentData<GroundMissileLauncherStateComponent>(launcher);
            var command=em.GetComponentData<SplitFrontLauncherCommandState>(launcher);
            bool stopped=command.CommandedTarget!=Entity.Null||state.Phase==(byte)GroundMissileLauncherPhase.Preparing;
            command.CommandedTarget=Entity.Null;if(stopped)command.Stops++;em.SetComponentData(launcher,command);
            // Hold stops the order. It never changes a launched projectile or the reload cycle.
            if(state.Phase==(byte)GroundMissileLauncherPhase.Preparing&&!em.HasComponent<GroundMissileInFlightComponent>(launcher))
            {state.Phase=(byte)GroundMissileLauncherPhase.Idle;state.TargetEntity=Entity.Null;state.Timer=0;em.SetComponentData(launcher,state);}
            return stopped;
        }
        public static void RecordLaunch(EntityManager em,Entity launcher)
        {
            if(!em.HasComponent<SplitFrontLauncherCommandState>(launcher))return;
            var command=em.GetComponentData<SplitFrontLauncherCommandState>(launcher);command.Launches++;em.SetComponentData(launcher,command);
        }
    }
}
