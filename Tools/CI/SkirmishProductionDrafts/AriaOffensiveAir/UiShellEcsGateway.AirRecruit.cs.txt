using Game.Components;
using Game.Configs;
using Game.Runtime;
using Game.Skirmish.Contracts;
using Game.UI.Contracts;
using Game.UI.Runtime;
using Unity.Collections;
using Unity.Entities;
namespace Game.UI.Shell.Ecs
{
    public sealed partial class UiShellEcsGateway : IUiAirRecruitGateway
    {
        static AriaPublicAirRole PublicAirRole(SkirmishRoleKind role)=>role switch
        {
            SkirmishRoleKind.AttackHeliLight=>AriaPublicAirRole.AttackHeliLight,SkirmishRoleKind.AttackHeli=>AriaPublicAirRole.AttackHeli,
            SkirmishRoleKind.Fighter=>AriaPublicAirRole.Fighter,SkirmishRoleKind.Strike=>AriaPublicAirRole.Strike,
            SkirmishRoleKind.Drone=>AriaPublicAirRole.Drone,SkirmishRoleKind.TransportHeli=>AriaPublicAirRole.TransportHeli,
            SkirmishRoleKind.TransportPlane=>AriaPublicAirRole.TransportPlane,_=>AriaPublicAirRole.None
        };
        bool IUiAirRecruitGateway.TryReadAirRecruit(out AriaAirRecruitObservation model)
        {
            model=default;
            if(!TrySkirmish(out var em,out var session,out _)||!em.HasComponent<SkirmishResolvedSetupRecord>(session))return false;
            var attempt=em.GetComponentData<SkirmishExpandedSessionComponent>(session);
            if(attempt.IsLegacy!=0||attempt.Phase!=SkirmishSessionPhase.Playing||attempt.ObjectiveKind==SkirmishObjectiveKind.BaseAssault)return false;
            var setup=em.GetComponentObject<SkirmishResolvedSetupRecord>(session).Setup;
            var army=SkirmishArmyProfileConfig.ResolveCached(setup.ArmyProfileId);
            if(army==null)return false;
            AriaAirRecruitChoice Read(SkirmishRoleKind role,string id)
            {
                var row=new AriaAirRecruitChoice {Role=PublicAirRole(role),CanQueue=SkirmishProductionService.EvaluateQueue(em,session,id,1,army,1).Accepted,
                    CanQueueAtFullReadiness=SkirmishProductionService.EvaluateQueueAtReadiness(em,session,id,1,army,1,SkirmishReadinessStage.FullArsenal).Accepted};
                using var q=em.CreateEntityQuery(typeof(SkirmishAttemptOwnedComponent),typeof(SkirmishUnitRoleComponent),typeof(UnitHealth));
                using var values=q.ToEntityArray(Allocator.Temp);
                foreach(var actor in values)
                {
                    var own=em.GetComponentData<SkirmishAttemptOwnedComponent>(actor);
                    if(own.SessionId==attempt.SessionId&&own.FactionId==1&&own.CapacityReleased==0&&
                        em.GetComponentData<SkirmishUnitRoleComponent>(actor).Role==role&&em.GetComponentData<UnitHealth>(actor).Current>0)row.Live++;
                }
                if(em.HasBuffer<SkirmishProductionReservation>(session))foreach(var item in em.GetBuffer<SkirmishProductionReservation>(session,true))
                    if(item.FactionId==1&&item.Role==role&&item.DeliveredMembers<item.MemberCount&&
                        item.Phase is SkirmishReservationPhase.Reserved or SkirmishReservationPhase.Producing)row.Pending=true;
                return row;
            }
            model=new AriaAirRecruitObservation {Presented=true,
                Light=Read(SkirmishRoleKind.AttackHeliLight,SkirmishRoleIds.AttackHeliLight),Heavy=Read(SkirmishRoleKind.AttackHeli,SkirmishRoleIds.AttackHeli),
                Fighter=Read(SkirmishRoleKind.Fighter,SkirmishRoleIds.Fighter),Strike=Read(SkirmishRoleKind.Strike,SkirmishRoleIds.Strike),
                Drone=Read(SkirmishRoleKind.Drone,SkirmishRoleIds.Drone),TransportHeli=Read(SkirmishRoleKind.TransportHeli,SkirmishRoleIds.TransportHeli),
                TransportPlane=Read(SkirmishRoleKind.TransportPlane,SkirmishRoleIds.TransportPlane)};
            return true;
        }
    }
}
