using Game.Components;
using Game.Runtime.Combat;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
namespace Game.Runtime
{
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(SupportAbilityRequestSystem))]
    [UpdateBefore(typeof(UnitAttackSystem))]
    public partial struct SupportFlightSystem : ISystem
    {
        public void OnUpdate(ref SystemState state)
        {
            state.Dependency.Complete();var em=state.EntityManager;
            using var flights=new NativeList<Entity>(Allocator.Temp);
            foreach(var (_,entity) in SystemAPI.Query<RefRO<SupportFlightComponent>>().WithEntityAccess())flights.Add(entity);
            foreach(var entity in flights)Advance(em,entity,(float)SystemAPI.Time.ElapsedTime);
            flights.Clear();
            foreach(var (_,entity) in SystemAPI.Query<RefRO<SupportFlightCleanupComponent>>().WithNone<SupportFlightComponent>().WithEntityAccess())flights.Add(entity);
            foreach(var entity in flights)
            {
                bool heldPassengers=false;
                if(em.HasBuffer<SupportPassengerReservationElement>(entity))foreach(var passenger in em.GetBuffer<SupportPassengerReservationElement>(entity))heldPassengers|=passenger.Released==0;
                SupportParatrooperAdapterSystemHelper.DestroyUnreleased(em,entity);SupportSupplyAdapterSystemHelper.DestroyUnreleased(em,entity);
                var cleanup=em.GetComponentData<SupportFlightCleanupComponent>(entity);
                SupportFuelTransactionSystem.ReleaseReserved(em,entity);
                if((cleanup.Released==0||heldPassengers) && em.Exists(cleanup.Root)&&em.HasComponent<SupportSessionComponent>(cleanup.Root))
                {
                    var session=em.GetComponentData<SupportSessionComponent>(cleanup.Root);
                    if(session.SessionToken.Equals(cleanup.Request.SessionToken)&&session.AttemptOrdinal==cleanup.Request.AttemptOrdinal)
                        SetReceipt(em,new SupportFlightComponent {Root=cleanup.Root,Request=cleanup.Request},cleanup.Released==0?SupportExecutionPhase.Aborted:SupportExecutionPhase.PartiallyResolved,SupportRejectionReason.TargetGone,cleanup.Released!=0);
                }
                em.RemoveComponent<SupportPassengerReservationElement>(entity);em.RemoveComponent<SupportFuelReservationElement>(entity);em.RemoveComponent<SupportFlightCleanupComponent>(entity);
            }
        }
        public static void Advance(EntityManager em,Entity entity,float elapsed=0)
        {
            if(!em.Exists(entity)||!em.HasComponent<SupportFlightComponent>(entity))return;
            var flight=em.GetComponentData<SupportFlightComponent>(entity);
            if(!em.Exists(flight.Root)||!em.HasComponent<SupportSessionComponent>(flight.Root))
            {SupportFuelTransactionSystem.ReleaseReserved(em,entity);em.DestroyEntity(entity);return;}
            var session=em.GetComponentData<SupportSessionComponent>(flight.Root);
            if(!session.SessionToken.Equals(flight.Request.SessionToken)||session.AttemptOrdinal!=flight.Request.AttemptOrdinal)
            {Abort(em,entity,SupportRejectionReason.WrongAttempt);return;}
            bool terminal=session.SessionToken.IsEmpty || em.HasComponent<CampaignMissionRuntimeComponent>(flight.Root) &&
                em.GetComponentData<CampaignMissionRuntimeComponent>(flight.Root).Outcome!=Game.Missions.Contracts.MissionOutcomeKind.None;
            if(terminal){if(flight.Phase<SupportExecutionPhase.Released)Abort(em,entity,SupportRejectionReason.NotActive);else em.DestroyEntity(entity);return;}
            if(em.GetComponentData<SupportCatalogComponent>(flight.Root).Revision!=session.CatalogRevision ||
               em.HasComponent<CampaignMissionRuntimeComponent>(flight.Root)&&em.GetComponentData<CampaignMissionRuntimeComponent>(flight.Root).SourceVersion!=session.MissionSourceVersion)
            {if(flight.Phase<SupportExecutionPhase.Released)Abort(em,entity,SupportRejectionReason.NotReady);else em.DestroyEntity(entity);return;}
            if(session.Active==0)return; // Pause retains committed reservations and the simulation clock.
            if(flight.Request.Kind==SupportAbilityKind.Supply)
            {SupportSupplyAdapterSystemHelper.Advance(em,entity,flight,session,elapsed);return;}
            if(flight.Request.Kind==SupportAbilityKind.Paratroopers)
            {SupportParatrooperAdapterSystemHelper.AdvanceFlight(em,entity,flight,session);return;}
            if(flight.Phase==SupportExecutionPhase.Approaching)
            {
                var reason=SupportAirValidationUtilitySystemHelper.ValidateStrikeTarget(em,session,flight.Request);
                if(reason==SupportRejectionReason.None)reason=SupportTargetValidationUtilitySystemHelper.ValidateGround(em,flight.Root,em.GetComponentData<LocalTransform>(flight.Request.Target).Position);
                if(reason==SupportRejectionReason.None && (!SupportAirValidationUtilitySystemHelper.SafeRoute(em,flight.Root,session,out var route) || route.Version!=flight.RouteVersion))reason=SupportRejectionReason.NoSafeAirRoute;
                if(reason!=SupportRejectionReason.None){Abort(em,entity,reason);return;}
                float t=math.saturate((float)(session.SimulationSeconds-flight.StartedAt)/math.max(.001f,flight.ApproachSeconds));
                SetTransform(em,entity,math.lerp(flight.Entry,flight.Release,t),flight.Release-flight.Entry);
                if(t<1)return;
                // Target and every contribution are revalidated before the single impact.
                if(!SupportFuelTransactionSystem.TryConsumeReserved(em,entity,session.FactionId)){Abort(em,entity,SupportRejectionReason.InsufficientFuel);return;}
                var target=flight.Request.Target;var health=em.GetComponentData<UnitHealth>(target);int previous=health.Current;
                health.Current=math.max(0,health.Current-flight.Damage);em.SetComponentData(target,health);
                var position=em.GetComponentData<LocalTransform>(target).Position;
                using var observation=em.CreateEntityQuery(typeof(CombatDamageObservationQueueComponent));
                CombatDamageObservationUtility.Append(em,CombatDamageObservationUtility.EnsureQueue(em,observation),entity,target,
                    CombatDamageSourceKind.SupportStrike,previous,health.Current,health.Max,(float)session.SimulationSeconds,flight.Release,position);
                if(UnitAttackSystem.TryBuildAttackVfxRequest(em,UnitAttackVfxRequestKind.Impact,entity,target,flight.Release,position,out var vfx))
                    em.AddComponentData(em.CreateEntity(),vfx);
                flight.Phase=SupportExecutionPhase.Released;flight.ReleasedAt=session.SimulationSeconds;em.SetComponentData(entity,flight);
                var cleanup=em.GetComponentData<SupportFlightCleanupComponent>(entity);cleanup.Released=1;em.SetComponentData(entity,cleanup);
                SetReceipt(em,flight,SupportExecutionPhase.Resolved,SupportRejectionReason.None,true);
            }
            else if(flight.Phase==SupportExecutionPhase.Released)
            {
                float t=math.saturate((float)(session.SimulationSeconds-flight.ReleasedAt)/math.max(.001f,flight.ExitSeconds));
                SetTransform(em,entity,math.lerp(flight.Release,flight.Exit,t),flight.Exit-flight.Release);
                if(t>=1)em.DestroyEntity(entity);
            }
        }
        public static void SetTransform(EntityManager em,Entity entity,float3 position,float3 direction)
        {var transform=em.GetComponentData<LocalTransform>(entity);transform.Position=position;transform.Rotation=quaternion.LookRotationSafe(math.normalizesafe(direction),math.up());em.SetComponentData(entity,transform);}
        public static void Abort(EntityManager em,Entity entity,SupportRejectionReason reason)
        {
            if(!em.Exists(entity)||!em.HasComponent<SupportFlightComponent>(entity))return;
            var flight=em.GetComponentData<SupportFlightComponent>(entity);
            if(flight.Phase>=SupportExecutionPhase.Released)return;
            SupportFuelTransactionSystem.ReleaseReserved(em,entity);
            if(em.Exists(flight.Root)&&em.HasComponent<SupportSessionComponent>(flight.Root))
            {
                var session=em.GetComponentData<SupportSessionComponent>(flight.Root);
                if(session.SessionToken.Equals(flight.Request.SessionToken)&&session.AttemptOrdinal==flight.Request.AttemptOrdinal)
                    SetReceipt(em,flight,SupportExecutionPhase.Aborted,reason,false);
            }
            SupportParatrooperAdapterSystemHelper.DestroyUnreleased(em,entity);SupportSupplyAdapterSystemHelper.DestroyUnreleased(em,entity);em.DestroyEntity(entity);
        }
        public static void SetReceipt(EntityManager em,in SupportFlightComponent flight,SupportExecutionPhase phase,SupportRejectionReason reason,bool applied)
        {
            var receipts=em.GetBuffer<SupportReceiptElement>(flight.Root);
            for(int i=0;i<receipts.Length;i++)
            {
                var receipt=receipts[i];
                if(receipt.RequestId!=flight.Request.RequestId||receipt.Source!=flight.Request.Source||(receipt.Phase!=SupportExecutionPhase.Approaching&&receipt.Phase!=SupportExecutionPhase.Released))continue;
                receipt.Phase=phase;receipt.Reason=reason;receipt.UpdatedAt=em.GetComponentData<SupportSessionComponent>(flight.Root).SimulationSeconds;receipt.EffectApplied=(byte)(applied?1:0);
                if(applied){receipt.SpentFuel+=receipt.ReservedFuel;receipt.ReservedFuel=0;}
                else if(receipt.ReservationReleased==0)
                {
                    var abilities=em.GetBuffer<SupportAbilityStateElement>(flight.Root);
                    for(int j=0;j<abilities.Length;j++)if(abilities[j].Kind==flight.Request.Kind)
                    {var a=abilities[j];a.ChargesRemaining++;a.CooldownUntil=receipt.PreviousCooldown;a.StateVersion++;abilities[j]=a;break;}
                    receipt.ReservationReleased=1;receipt.ReservedFuel=0;
                    var input=em.GetComponentData<SupportInputStateComponent>(flight.Root);input.Version++;em.SetComponentData(flight.Root,input);
                }
                receipts[i]=receipt;break;
            }
        }
    }
}
