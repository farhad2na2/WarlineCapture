using Game.Components;
using Unity.Collections;
using Unity.Entities;
namespace Game.Runtime
{
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(VehicleFuelConsumptionSystem))]
    [UpdateBefore(typeof(SupportSmokeSystem))]
    public partial struct SupportAbilityRequestSystem : ISystem
    {
        public void OnCreate(ref SystemState state) => state.RequireForUpdate<SupportRequestElement>();
        public void OnUpdate(ref SystemState state)
        {
            state.Dependency.Complete();
            if(!SystemAPI.TryGetSingletonEntity<SupportSessionComponent>(out var root))return;
            var requests=state.EntityManager.GetBuffer<SupportRequestElement>(root);
            while(requests.Length>0)
            {
                var request=requests[0];requests.RemoveAt(0);Process(state.EntityManager,root,request);
                requests=state.EntityManager.GetBuffer<SupportRequestElement>(root);
            }
        }
        public static SupportRejectionReason Process(EntityManager em, Entity root, in SupportRequestElement request)
        {
            var session=em.GetComponentData<SupportSessionComponent>(root);
            // Ignore old-attempt identities without modifying the current attempt's high-water marks.
            if(!session.SessionToken.Equals(request.SessionToken) || session.AttemptOrdinal!=request.AttemptOrdinal) return SupportRejectionReason.WrongAttempt;
            uint last=request.Source==SupportRequestSource.Aria ? session.LastAriaRequestId : session.LastPlayerRequestId;
            if(request.RequestId==0 || request.RequestId<=last) return SupportRejectionReason.AlreadyProcessed;
            var reason=SupportTargetValidationUtilitySystemHelper.Validate(em,root,request);
            var preview=em.GetComponentData<SupportPreviewComponent>(root);
            if(reason==SupportRejectionReason.None && (preview.Valid==0 || preview.PreviewId!=request.PreviewId ||
                !SupportTargetValidationUtilitySystemHelper.SameAction(preview.Request,request)))reason=SupportRejectionReason.StalePreview;
            if(request.Source==SupportRequestSource.Aria) session.LastAriaRequestId=request.RequestId; else session.LastPlayerRequestId=request.RequestId;
            em.SetComponentData(root,session);
            if(reason!=SupportRejectionReason.None) return Reject(em,root,request,reason);
            var catalog=em.GetComponentData<SupportCatalogComponent>(root);
            SupportTargetValidationUtilitySystemHelper.TryDefinition(catalog,request.Kind,out var definition);
            bool flight=request.Kind!=SupportAbilityKind.Smoke;
            // The Smoke effect is immediate; aircraft hold physical reservations.
            // Establish the effect entity before committing costs; a scheduling failure cannot consume Fuel.
            Entity effect,payload=Entity.Null;
            if(flight)
            {
                SupportAirValidationUtilitySystemHelper.SafeRoute(em,root,session,out var route,request.Kind!=SupportAbilityKind.Strike,request.Position);
                var prefab=SupportAirValidationUtilitySystemHelper.ResolveAircraftPrefab(em,definition.AircraftSourceKey);
                effect=SupportAirValidationUtilitySystemHelper.CreateFlightPresentation(em,prefab,route.Entry,request.Kind);
                if(request.Kind==SupportAbilityKind.Paratroopers)
                {
                    using var cells=new NativeList<Unity.Mathematics.int2>(Allocator.Temp);
                    using var positions=new NativeList<Unity.Mathematics.float3>(Allocator.Temp);
                    var landing=SupportLandingUtilitySystemHelper.PlanParatroopers(em,root,request,cells,positions);
                    if(landing!=SupportRejectionReason.None){em.DestroyEntity(effect);return Reject(em,root,request,landing);}
                    SupportParatrooperAdapterSystemHelper.PreparePassengers(em,root,effect,cells,positions);
                }
                if(request.Kind==SupportAbilityKind.Supply)payload=SupportSupplyAdapterSystemHelper.Prepare(em,root,effect,request,definition.Materials);
                em.AddBuffer<SupportFuelReservationElement>(effect);
                em.AddComponentData(effect,new SupportFlightCleanupComponent {Root=root,Payload=payload,Request=request});
                em.AddComponentData(effect,new SupportFlightComponent {Root=root,Payload=payload,Request=request,Phase=SupportExecutionPhase.Approaching,
                    StartedAt=session.SimulationSeconds,ApproachSeconds=definition.ApproachSeconds,ExitSeconds=3,Entry=route.Entry,Release=route.Release,Exit=route.Exit,
                    RouteVersion=route.Version,Damage=definition.Damage,FactionId=session.FactionId});

            }
            else { effect=em.CreateEntity();em.AddComponentData(effect,new SupportSmokeZoneComponent { SessionToken=session.SessionToken,AttemptOrdinal=session.AttemptOrdinal,
                Center=request.Position,RadiusSquared=definition.Radius*definition.Radius,ExpiresAt=session.SimulationSeconds+definition.DurationSeconds,DirectDamagePermille=definition.DirectDamagePermille }); }
            if(!(flight?SupportFuelTransactionSystem.TryReserve(em,effect,session.FactionId,definition.FuelCost,request.RequestId):
                SupportFuelTransactionSystem.TryConsumeImmediate(em,session.FactionId,definition.FuelCost)))
            { SupportParatrooperAdapterSystemHelper.DestroyUnreleased(em,effect);SupportSupplyAdapterSystemHelper.DestroyUnreleased(em,effect);em.DestroyEntity(effect);return Reject(em,root,request,SupportRejectionReason.InsufficientFuel); }
            if(!flight && definition.VisualPrefab!=Entity.Null && em.Exists(definition.VisualPrefab))
            {
                var visual=em.Instantiate(definition.VisualPrefab);
                if(em.HasComponent<Unity.Transforms.LocalTransform>(visual)) em.SetComponentData(visual,Unity.Transforms.LocalTransform.FromPosition(request.Position));
                em.AddComponentData(visual,new SupportSmokeVisualOwnerComponent { Zone=effect });
            }
            var abilities=em.GetBuffer<SupportAbilityStateElement>(root);
            double previous=0;
            for(int i=0;i<abilities.Length;i++) if(abilities[i].Kind==request.Kind)
            {
                var a=abilities[i];previous=a.CooldownUntil; a.ChargesRemaining--; a.CooldownUntil=session.SimulationSeconds+definition.CooldownSeconds; a.StateVersion++; abilities[i]=a; break;
            }
            if(request.Source==SupportRequestSource.Aria) { var proposal=em.GetComponentData<SupportProposalComponent>(root); proposal.Consumed=1; em.SetComponentData(root,proposal); }
            em.SetComponentData(root,default(SupportPreviewComponent));
            if(em.HasComponent<SupportInputStateComponent>(root)){var input=em.GetComponentData<SupportInputStateComponent>(root);input.Phase=0;input.Version++;em.SetComponentData(root,input);}
            AppendReceipt(em,root,new SupportReceiptElement { RequestId=request.RequestId,Source=request.Source,Kind=request.Kind,
                Phase=flight?SupportExecutionPhase.Approaching:SupportExecutionPhase.Resolved,SpentFuel=flight?0:definition.FuelCost,
                ReservedFuel=flight?definition.FuelCost:0,Effect=effect,EffectApplied=(byte)(flight?0:1),PreviousCooldown=previous });
            return SupportRejectionReason.None;
        }
        private static void AppendReceipt(EntityManager em,Entity root,SupportReceiptElement receipt)
        {
            receipt.UpdatedAt=em.GetComponentData<SupportSessionComponent>(root).SimulationSeconds;
            var receipts=em.GetBuffer<SupportReceiptElement>(root);
            if(receipts.Length>=64)
                for(int i=0;i<receipts.Length;i++)if(receipts[i].Phase is SupportExecutionPhase.Resolved or SupportExecutionPhase.Aborted or SupportExecutionPhase.PartiallyResolved)
                {receipts.RemoveAt(i);break;} // Accepted in-flight receipts are pinned.
            receipts.Add(receipt);
        }
        private static SupportRejectionReason Reject(EntityManager em,Entity root,in SupportRequestElement request,SupportRejectionReason reason)
        {
            AppendReceipt(em,root,new SupportReceiptElement {RequestId=request.RequestId,Source=request.Source,Kind=request.Kind,Phase=SupportExecutionPhase.Aborted,Reason=reason});
            var preview=em.GetComponentData<SupportPreviewComponent>(root);
            // An old request must not replace a newly chosen target or reopen a canceled preview.
            if(preview.PreviewId==request.PreviewId && SupportTargetValidationUtilitySystemHelper.SameAction(preview.Request,request))
            {
                preview.Valid=0;preview.Reason=reason;em.SetComponentData(root,preview);
                var input=em.GetComponentData<SupportInputStateComponent>(root);
                if(input.Phase==4){input.Phase=3;input.Version++;em.SetComponentData(root,input);}
                var proposal=em.GetComponentData<SupportProposalComponent>(root);proposal.Consumed=1;em.SetComponentData(root,proposal);
            }
            return reason;
        }
    }
}
