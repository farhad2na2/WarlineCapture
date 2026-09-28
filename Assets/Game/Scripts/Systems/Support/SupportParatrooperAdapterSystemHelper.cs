using Game.Components;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
namespace Game.Runtime
{
    public static class SupportParatrooperAdapterSystemHelper
    {
        public static void PreparePassengers(EntityManager em,Entity root,Entity flight,NativeList<int2> cells,NativeList<float3> positions)
        {
            var payload=em.GetComponentData<SupportPayloadBindingsComponent>(root);var session=em.GetComponentData<SupportSessionComponent>(root);
            em.AddBuffer<UnitTransportPassengerElement>(flight);em.AddBuffer<SupportPassengerReservationElement>(flight);
            em.AddComponentData(flight,new UnitTransportAirdropVisualPrefabs {SoldierParachuteVisualPrefab=payload.ParachutePrefab});
            if(em.HasComponent<UnitTransportPlaneDoorReference>(flight))em.AddComponent<UnitTransportPlaneDoorState>(flight);
            for(int i=0;i<cells.Length;i++)
            {
                var passenger=em.Instantiate(payload.InfantryPrefab);
                em.SetComponentData(passenger,new Faction {Id=session.FactionId});
                if(em.HasComponent<SelectedUnitTag>(passenger))em.RemoveComponent<SelectedUnitTag>(passenger);
                em.AddComponentData(passenger,new CampaignMissionUnitRoleComponent {MissionRoleId="role.friendly.support",UnitGroupId="support.rifle",SessionToken=session.SessionToken});
                em.AddComponent<SupportPassengerTransitTag>(passenger);
                em.AddComponentData(passenger,new SupportPassengerOwnerComponent {Root=root,Flight=flight,SessionToken=session.SessionToken,AttemptOrdinal=session.AttemptOrdinal,LandingCell=cells[i],LandingPosition=positions[i]});
                if(!em.HasBuffer<UnitTransportHiddenVisualScale>(passenger))em.AddBuffer<UnitTransportHiddenVisualScale>(passenger);
                var ecb=new EntityCommandBuffer(Allocator.Temp);
                new UnitTransportPassengerStateSystem().BoardPassenger(em,ref ecb,em.GetBuffer<UnitTransportPassengerElement>(flight),passenger,flight);
                ecb.Playback(em);ecb.Dispose();
                em.GetBuffer<SupportPassengerReservationElement>(flight).Add(new SupportPassengerReservationElement {Passenger=passenger});
            }
        }
        public static void DestroyUnreleased(EntityManager em,Entity flight)
        {
            if(!em.Exists(flight)||!em.HasBuffer<SupportPassengerReservationElement>(flight))return;
            using var entries=em.GetBuffer<SupportPassengerReservationElement>(flight).ToNativeArray(Allocator.Temp);
            foreach(var entry in entries)if(entry.Released==0&&em.Exists(entry.Passenger))em.DestroyEntity(entry.Passenger);
        }
        public static void AdvanceFlight(EntityManager em,Entity entity,SupportFlightComponent flight,SupportSessionComponent session)
        {
            if(flight.Phase==SupportExecutionPhase.Approaching)
            {
                var reason=SupportTargetValidationUtilitySystemHelper.ValidateGround(em,flight.Root,flight.Request.Position);
                if(reason==SupportRejectionReason.None&&(!SupportAirValidationUtilitySystemHelper.SafeRoute(em,flight.Root,session,out var route,true,flight.Request.Position)||route.Version!=flight.RouteVersion))reason=SupportRejectionReason.NoSafeAirRoute;
                if(reason!=SupportRejectionReason.None){SupportFlightSystem.Abort(em,entity,reason);return;}
                float t=math.saturate((float)(session.SimulationSeconds-flight.StartedAt)/math.max(.001f,flight.ApproachSeconds));
                SupportFlightSystem.SetTransform(em,entity,math.lerp(flight.Entry,flight.Release,t),flight.Release-flight.Entry);
                if(t<1||em.HasComponent<UnitTransportAirdropRequest>(entity))return;
                em.AddComponentData(entity,new UnitTransportAirdropRequest {DropReferenceCell=flight.Request.Cell,DropCount=em.GetBuffer<SupportPassengerReservationElement>(entity).Length,
                    SoldierDropCount=em.GetBuffer<SupportPassengerReservationElement>(entity).Length,DropMode=UnitTransportAirdropMode.SoldierOnly,DropIntervalSeconds=.65f,PassReady=1});
            }
            else if(flight.Phase==SupportExecutionPhase.Released)
            {
                if(em.HasComponent<UnitTransportAirdropRequest>(entity))
                {flight.ReleasedAt=session.SimulationSeconds;em.SetComponentData(entity,flight);return;}
                float t=math.saturate((float)(session.SimulationSeconds-flight.ReleasedAt)/math.max(.001f,flight.ExitSeconds));
                SupportFlightSystem.SetTransform(em,entity,math.lerp(flight.Release,flight.Exit,t),flight.Exit-flight.Release);
                if(t>=1)em.DestroyEntity(entity);
            }
        }
        // Runs inside the existing airdrop request pass before any restoration/spawn ECB command.
        public static bool BeforeDrop(EntityManager em,EntityCommandBuffer ecb,Entity transport,Entity passenger,in GridConfig grid,Entity gridEntity,out int2 cell,SupportRejectionReason failure=SupportRejectionReason.None)
        {
            cell=default;var flight=em.GetComponentData<SupportFlightComponent>(transport);var session=em.GetComponentData<SupportSessionComponent>(flight.Root);
            var reason=failure;
            if(session.Active==0)return false;
            if(reason==SupportRejectionReason.None&&(!SupportAirValidationUtilitySystemHelper.SafeRoute(em,flight.Root,session,out var currentRoute,true,flight.Request.Position)||currentRoute.Version!=flight.RouteVersion))reason=SupportRejectionReason.NoSafeAirRoute;
            int population=0;
            if(reason==SupportRejectionReason.None&&!SupportLandingUtilitySystemHelper.CountPopulation(em,session.FactionId,out population))reason=SupportRejectionReason.NotReady;
            else if(reason==SupportRejectionReason.None&&population>em.GetComponentData<SupportMissionPolicyComponent>(flight.Root).PopulationCeiling)reason=SupportRejectionReason.PopulationFull;
            var reservations=em.GetBuffer<SupportPassengerReservationElement>(transport);
            for(int i=0;reason==SupportRejectionReason.None&&i<reservations.Length;i++)
            {
                var entry=reservations[i];if(entry.Released!=0)continue;
                if(!em.Exists(entry.Passenger)||!em.HasComponent<SupportPassengerOwnerComponent>(entry.Passenger)){reason=SupportRejectionReason.TargetGone;break;}
                var owner=em.GetComponentData<SupportPassengerOwnerComponent>(entry.Passenger);

                var grounded=owner.LandingPosition;
                if(!new MapSurfaceSpawnGrounding().TryGroundCellCenter(em,grid,owner.LandingCell,ref grounded,out _)||!math.all(math.isfinite(grounded)))
                {reason=SupportRejectionReason.LandingBlocked;break;}
                owner.LandingPosition=grounded;em.SetComponentData(entry.Passenger,owner);
                var footprint=em.GetComponentData<UnitFootprint>(entry.Passenger).Size;
                if(!SupportLandingUtilitySystemHelper.IsFree(em,flight.Root,gridEntity,grid,owner.LandingCell,footprint,entry.Passenger)||
                    SupportTargetValidationUtilitySystemHelper.ValidateGround(em,flight.Root,owner.LandingPosition)!=SupportRejectionReason.None)
                    reason=SupportRejectionReason.LandingBlocked;
            }
            if(reason==SupportRejectionReason.None&&flight.Phase==SupportExecutionPhase.Approaching&&!SupportFuelTransactionSystem.TryConsumeReserved(em,transport,session.FactionId))reason=SupportRejectionReason.InsufficientFuel;
            if(reason!=SupportRejectionReason.None)
            {
                bool released=flight.Phase>=SupportExecutionPhase.Released;
                if(!released)SupportFuelTransactionSystem.ReleaseReserved(em,transport);
                SupportFlightSystem.SetReceipt(em,flight,released?SupportExecutionPhase.PartiallyResolved:SupportExecutionPhase.Aborted,reason,released);
                foreach(var entry in reservations)if(entry.Released==0&&em.Exists(entry.Passenger))ecb.DestroyEntity(entry.Passenger);
                em.GetBuffer<UnitTransportPassengerElement>(transport).Clear();ecb.RemoveComponent<UnitTransportAirdropRequest>(transport);
                if(!released)ecb.DestroyEntity(transport);
                return false;
            }
            var selected=em.GetComponentData<SupportPassengerOwnerComponent>(passenger);cell=selected.LandingCell;selected.Released=1;em.SetComponentData(passenger,selected);
            for(int i=0;i<reservations.Length;i++)if(reservations[i].Passenger==passenger){var entry=reservations[i];entry.Released=1;reservations[i]=entry;break;}
            flight.Phase=SupportExecutionPhase.Released;flight.ReleasedAt=session.SimulationSeconds;em.SetComponentData(transport,flight);
            var cleanup=em.GetComponentData<SupportFlightCleanupComponent>(transport);cleanup.Released=1;em.SetComponentData(transport,cleanup);
            bool all=true;foreach(var entry in reservations)all&=entry.Released!=0;
            SupportFlightSystem.SetReceipt(em,flight,all?SupportExecutionPhase.Resolved:SupportExecutionPhase.Released,SupportRejectionReason.None,true);
            return true;
        }
    }
}
