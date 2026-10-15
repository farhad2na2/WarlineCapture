using Game.Components;
using Game.Configs;
using Game.Skirmish.Contracts;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
namespace Game.Runtime
{
    public static class SkirmishPublicAirPressureFeedback
    {
        public static bool OffensiveRole(SkirmishRoleKind role)=>role is SkirmishRoleKind.AttackHeliLight or
            SkirmishRoleKind.AttackHeli or SkirmishRoleKind.Fighter or SkirmishRoleKind.Strike;
        public static bool Eligible(EntityManager em,Entity actor)
        {
            if(!SkirmishPublicAircraftReturnFeedback.TryReadActor(em,actor,out var session,out var attempt,out _,out _)||
                !em.HasComponent<SkirmishResolvedSetupRecord>(session))return false;
            var role=em.GetComponentData<SkirmishUnitRoleComponent>(actor).Role;
            if(!OffensiveRole(role)||!em.HasComponent<UnitAttack>(actor)||em.GetComponentData<UnitAttack>(actor).Damage<=0||
                !em.HasComponent<UnitCombat>(actor)||em.GetComponentData<UnitCombat>(actor).CanAttack==0||
                !em.HasComponent<CombatTargetPolicy>(actor)||em.GetComponentData<CombatTargetPolicy>(actor).AllowedTargets==CombatTargetDomain.None)return false;
            var setup=em.GetComponentObject<SkirmishResolvedSetupRecord>(session).Setup;
            if(setup==null||setup.SetupHash!=attempt.SetupHash||setup.RoleOverlays==null)return false;
            var army=SkirmishArmyProfileConfig.ResolveCached(setup.ArmyProfileId);if(army==null)return false;
            int matches=0;
            foreach(var overlay in setup.RoleOverlays)if(overlay.RoleKind==role)
            {if(!army.Allows(overlay.RoleId))return false;matches++;}
            return matches==1;
        }
        // Runs only after the ordinary screen-origin Attack Move owner accepted.
        // A single exact living aircraft group avoids aggregate/partial acceptance.
        public static void Observe(EntityManager em,in RtsSelectionCommandIntentRequestElement request,bool accepted)
        {
            try
            {
                if(!accepted||request.Kind!=RtsSelectionCommandIntentKind.Attack||request.HasScreenPosition==0||
                    request.HasTargetCell==0||request.HasTargetEntity!=0||request.RequestId<=0||!math.all(math.isfinite(request.ScreenPosition)))return;
                using var query=em.CreateEntityQuery(typeof(SelectedUnitTag));
                using var selected=query.ToEntityArray(Allocator.Temp);
                if(selected.Length!=1)return;
                var actor=selected[0];
                if(!SkirmishPublicAircraftReturnFeedback.TryReadActor(em,actor,out var session,out var attempt,out var own,out uint group)||
                    !Eligible(em,actor)||!em.HasComponent<AttackMoveOrder>(actor))return;
                var air=em.GetComponentData<UnitAirComponent>(actor);var order=em.GetComponentData<AttackMoveOrder>(actor);
                if(air.ReturningHome!=0||!math.all(math.isfinite(order.Position)))return;
                uint revision=em.HasComponent<SkirmishPublicAirPressureReceipt>(actor)?em.GetComponentData<SkirmishPublicAirPressureReceipt>(actor).Revision:0;
                if(revision==uint.MaxValue)return;
                var row=new SkirmishPublicAirPressureReceipt {Actor=actor,Session=session,World=em.WorldUnmanaged.SequenceNumber,
                    Attempt=attempt.SessionId,StableId=own.StableObjectId,Role=em.GetComponentData<SkirmishUnitRoleComponent>(actor).Role,
                    PrefabKey=em.GetComponentData<UnitSourcePrefabKey>(actor).Value,
                    SetupHash=attempt.SetupHash,GroupId=group,Revision=revision+1,RequestId=request.RequestId,Frame=request.Frame,
                    Screen=request.ScreenPosition,Goal=order.Destination,Position=order.Position};
                if(em.HasComponent<SkirmishPublicAirPressureReceipt>(actor))em.SetComponentData(actor,row);else em.AddComponentData(actor,row);
            }
            catch(System.Exception){ /* Optional observation never changes normal gameplay acceptance. */ }
        }
        public static bool TryRead(EntityManager em,Entity actor,out SkirmishPublicAirPressureReceipt row)
        {
            row=default;
            if(!SkirmishPublicAircraftReturnFeedback.TryReadActor(em,actor,out var session,out var attempt,out var own,out uint group)||
                !Eligible(em,actor)||!em.HasComponent<SkirmishPublicAirPressureReceipt>(actor)||!em.HasComponent<AttackMoveOrder>(actor))return false;
            row=em.GetComponentData<SkirmishPublicAirPressureReceipt>(actor);var order=em.GetComponentData<AttackMoveOrder>(actor);
            return row.Actor==actor&&row.Session==session&&row.Attempt==attempt.SessionId&&row.StableId==own.StableObjectId&&
                row.Role==em.GetComponentData<SkirmishUnitRoleComponent>(actor).Role&&row.PrefabKey==em.GetComponentData<UnitSourcePrefabKey>(actor).Value&&
                row.World==em.WorldUnmanaged.SequenceNumber&&row.SetupHash==attempt.SetupHash&&
                row.GroupId==group&&row.Revision>0&&row.RequestId>0&&row.Frame>=0&&row.Goal.Equals(order.Destination)&&
                row.Position.Equals(order.Position)&&math.all(math.isfinite(row.Position))&&math.all(math.isfinite(row.Screen));
        }
    }
}
