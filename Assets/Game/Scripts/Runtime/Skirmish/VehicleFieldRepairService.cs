using System;
using System.Collections.Generic;
using Game.Components;
using Game.Configs;
using Game.Skirmish.Contracts;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace Game.Runtime
{
    // Both normal player requests and enemy policy use this paid command owner.
    // It never moves a vehicle, changes Fuel, uncovers fog or heals on acceptance.
    public static class VehicleFieldRepairService
    {

        // Observational only. Installed only by an explicit owned capture scope.
        public sealed class RepairLedgerPoint
        {
            public Entity Actor,BankEntity;
            public bool HasBank,HasState,HasOwner,HasHealth;
            public FactionTacticalMaterialsComponent Bank;
            public VehicleRepairStateComponent State;
            public SkirmishAttemptOwnedComponent Owner;
            public UnitHealth Health;
        }
        public sealed class RepairLedgerObservation
        {
            public EntityManager Manager;
            public Entity Session;
            public string Operation;
            public RepairLedgerPoint Before,After;
            public VehicleRepairStateComponent PolicyState;
        }
        public static Action<RepairLedgerObservation> RepairLedgerObserver {get;private set;}
        public static long RepairLedgerFaults {get;private set;}
        public static void OpenRepairLedgerOwned(Action<RepairLedgerObservation> sink)
        {
            if(sink==null)throw new ArgumentNullException(nameof(sink));
            if(RepairLedgerObserver!=null)throw new InvalidOperationException("Repair ledger already observed");
            RepairLedgerObserver=sink;
        }
        public static bool CloseRepairLedgerOwned(Action<RepairLedgerObservation> sink)
        {
            if(sink==null || RepairLedgerObserver!=sink)return false;
            RepairLedgerObserver=null;return true;
        }
        private sealed class RepairLedgerWindow
        {
            public Action<RepairLedgerObservation> Sink;
            public RepairLedgerPoint Before;
            public VehicleRepairStateComponent PolicyState;
        }
        private static RepairLedgerPoint CaptureRepairPoint(EntityManager em,Entity actor,Entity bank)
        {
            var p=new RepairLedgerPoint {Actor=actor,BankEntity=bank,
                HasBank=em.Exists(bank)&&em.HasComponent<FactionTacticalMaterialsComponent>(bank),
                HasState=em.Exists(actor)&&em.HasComponent<VehicleRepairStateComponent>(actor),
                HasOwner=em.Exists(actor)&&em.HasComponent<SkirmishAttemptOwnedComponent>(actor),
                HasHealth=em.Exists(actor)&&em.HasComponent<UnitHealth>(actor)};
            if(p.HasBank)p.Bank=em.GetComponentData<FactionTacticalMaterialsComponent>(bank);
            if(p.HasState)p.State=em.GetComponentData<VehicleRepairStateComponent>(actor);
            if(p.HasOwner)p.Owner=em.GetComponentData<SkirmishAttemptOwnedComponent>(actor);
            if(p.HasHealth)p.Health=em.GetComponentData<UnitHealth>(actor);
            return p;
        }
        private static RepairLedgerWindow BeginRepairLedger(EntityManager em,Entity actor,in VehicleRepairStateComponent policy)
        {
            var sink=RepairLedgerObserver;if(sink==null)return null;
            try{return new RepairLedgerWindow {Sink=sink,Before=CaptureRepairPoint(em,actor,policy.MaterialsBank),PolicyState=policy};}
            catch(Exception error)when(!(error is OutOfMemoryException)){RepairLedgerFaults++;return null;}
        }
        private static RepairLedgerWindow BeginDeferredRepairLedger(EntityManager em,Entity actor,in VehicleRepairStateComponent policy)
        {
            if(RepairLedgerObserver==null)return null;
            // A failed retry that rewrites the same debt is not a new committed paid boundary.
            try
            {
                if(em.HasComponent<VehicleRepairStateComponent>(actor))
                {
                    var prior=em.GetComponentData<VehicleRepairStateComponent>(actor);
                    if(prior.Phase==VehicleRepairPhase.AccountingFailure && prior.RefundDue==policy.RefundDue &&
                        prior.RefundedMaterials==policy.RefundedMaterials)return null;
                }
            }
            catch(Exception error)when(!(error is OutOfMemoryException)){RepairLedgerFaults++;return null;}
            return BeginRepairLedger(em,actor,policy);
        }
        private static void EndRepairLedger(EntityManager em,RepairLedgerWindow window,string operation)
        {
            if(window==null)return;
            try
            {
                if(RepairLedgerObserver!=window.Sink)throw new InvalidOperationException("Repair observer ownership changed");
                window.Sink(new RepairLedgerObservation {Manager=em,Session=window.PolicyState.Session,Operation=operation,
                    Before=window.Before,After=CaptureRepairPoint(em,window.Before.Actor,window.Before.BankEntity),PolicyState=window.PolicyState});
            }
            catch(Exception error)when(!(error is OutOfMemoryException)){RepairLedgerFaults++;}
        }

        public static void NotifyIncomingDamage(EntityManager em,Entity actor)
        {
            if(!em.Exists(actor)||!em.HasComponent<VehicleRepairStateComponent>(actor))return;
            var state=em.GetComponentData<VehicleRepairStateComponent>(actor);
            if(state.Phase!=VehicleRepairPhase.Working)return;state.IncomingDamage=1;em.SetComponentData(actor,state);
        }
        public static void NotifyNewMovement(EntityManager em,Entity actor)
        {
            if(!em.Exists(actor)||!em.HasComponent<VehicleRepairStateComponent>(actor))return;
            var state=em.GetComponentData<VehicleRepairStateComponent>(actor);
            if(state.Phase!=VehicleRepairPhase.Working)return;state.NewMovement=1;em.SetComponentData(actor,state);
        }
        public static bool TryEnqueue(EntityManager em,Entity session,Entity actor,byte faction,VehicleRepairAction action,out string error)
        {
            error="Repair request is unavailable.";
            if(!BasicActor(em,session,actor,faction,out _,out _)||em.HasComponent<VehicleRepairRequestComponent>(actor)||
                (action!=VehicleRepairAction.Start&&action!=VehicleRepairAction.Cancel))return false;
            var prior=em.HasComponent<VehicleRepairStateComponent>(actor)?em.GetComponentData<VehicleRepairStateComponent>(actor):default;
            if(prior.RequestId!=0&&!ReceiptIdentity(em,actor,prior,out _,out _))return false;
            if(prior.RequestId==uint.MaxValue||action==VehicleRepairAction.Start&&(prior.Phase==VehicleRepairPhase.Working||prior.Phase==VehicleRepairPhase.AccountingFailure)||
                action==VehicleRepairAction.Cancel&&prior.Phase!=VehicleRepairPhase.Working)return false;
            em.AddComponentData(actor,new VehicleRepairRequestComponent{Session=session,Faction=faction,Action=action,RequestId=prior.RequestId+1});
            error=null;return true;
        }

        public static bool TryEnqueueQuoted(EntityManager em,Entity session,Entity actor,byte faction,in VehicleRepairPlan expected,out string error)
        {
            error="The displayed repair quote is stale.";
            if(!TryQuote(em,session,actor,faction,out var current,out _)||!SamePlan(current,expected))return false;
            if(!TryEnqueue(em,session,actor,faction,VehicleRepairAction.Start,out error))return false;
            var request=em.GetComponentData<VehicleRepairRequestComponent>(actor);
            var own=em.GetComponentData<SkirmishAttemptOwnedComponent>(actor);
            request.HasExpectedPlan=1;request.QuotedAttemptId=own.SessionId;request.QuotedActorStableId=own.StableObjectId;
            request.QuotedSetupHash=em.GetComponentData<SkirmishExpandedSessionComponent>(session).SetupHash;
            request.ExpectedFractionNumerator=expected.FractionNumerator;request.ExpectedFractionDenominator=expected.FractionDenominator;
            request.ExpectedDurationTicks=expected.DurationTicks;request.ExpectedMaterials=expected.Materials;
            em.SetComponentData(actor,request);error=null;return true;
        }
        private static bool SamePlan(in VehicleRepairPlan a,in VehicleRepairPlan b)=>a.FractionNumerator==b.FractionNumerator&&
            a.FractionDenominator==b.FractionDenominator&&a.DurationTicks==b.DurationTicks&&a.Materials==b.Materials;

        public static bool TryQuote(EntityManager em,Entity session,Entity actor,byte faction,out VehicleRepairPlan plan,out string error)
        {
            plan=default;error="Repair is unavailable.";
            if(!TryContext(em,session,actor,faction,out var context)||
                em.HasComponent<VehicleRepairStateComponent>(actor)&&(em.GetComponentData<VehicleRepairStateComponent>(actor).Phase==VehicleRepairPhase.Working||em.GetComponentData<VehicleRepairStateComponent>(actor).Phase==VehicleRepairPhase.AccountingFailure)||
                !VehicleRepairRules.TryQuote(context.Health.Current,context.Health.Max,out plan)||
                !FactionTacticalMaterialsUtilitySystemHelper.CanAfford(context.Materials,plan.Materials))return false;
            error=null;return true;
        }

        public static bool ProcessRequest(EntityManager em,Entity actor,out string error)
        {
            error="Repair request is unavailable.";
            if(!em.Exists(actor)||!em.HasComponent<VehicleRepairRequestComponent>(actor))return false;
            var request=em.GetComponentData<VehicleRepairRequestComponent>(actor);
            em.RemoveComponent<VehicleRepairRequestComponent>(actor);
            if(!BasicActor(em,request.Session,actor,request.Faction,out _,out _))return false;
            var prior=em.HasComponent<VehicleRepairStateComponent>(actor)?em.GetComponentData<VehicleRepairStateComponent>(actor):default;
            if(request.RequestId==0||request.RequestId<=prior.RequestId)return false;
            if(prior.RequestId!=0&&(!ReceiptIdentity(em,actor,prior,out _,out _)||prior.Session!=request.Session||prior.Faction!=request.Faction))return false;
            if(request.Action==VehicleRepairAction.Cancel)
            {
                if(prior.Phase!=VehicleRepairPhase.Working)return false;
                if(!AccrueWork(em,ref prior,out error))return false;
                prior.RequestId=request.RequestId;
                bool cancelled=Cancel(em,actor,ref prior,false);error=cancelled?null:"Repair refund accounting failed.";return cancelled;
            }
            if(request.Action!=VehicleRepairAction.Start||prior.Phase==VehicleRepairPhase.Working||prior.Phase==VehicleRepairPhase.AccountingFailure||
                !TryContext(em,request.Session,actor,request.Faction,out var context)||
                !VehicleRepairRules.TryQuote(context.Health.Current,context.Health.Max,out var plan))return false;
            if(request.HasExpectedPlan>1)return false;
            if(request.HasExpectedPlan==1&&(request.QuotedAttemptId!=context.Owned.SessionId||
                request.QuotedActorStableId!=context.Owned.StableObjectId||
                request.QuotedSetupHash!=em.GetComponentData<SkirmishExpandedSessionComponent>(request.Session).SetupHash||
                !SamePlan(plan,new VehicleRepairPlan{FractionNumerator=request.ExpectedFractionNumerator,FractionDenominator=request.ExpectedFractionDenominator,
                    DurationTicks=request.ExpectedDurationTicks,Materials=request.ExpectedMaterials})))
            {error="The displayed repair quote is stale.";return false;}
            var materials=context.Materials;
            if(FactionTacticalMaterialsUtilitySystemHelper.TrySpend(ref materials,plan.Materials,FactionTacticalMaterialsSpendKind.Repair)!=FactionTacticalMaterialsMutationResult.Applied)return false;
            // Store this paid reservation once, before any work can accrue.
            var state=new VehicleRepairStateComponent{Session=request.Session,ServiceFacility=context.Facility,MaterialsBank=context.Bank,
                AttemptId=context.Owned.SessionId,ActorStableId=context.Owned.StableObjectId,PadId=new FixedString64Bytes(context.Pad.PadId),
                Faction=request.Faction,Phase=VehicleRepairPhase.Working,RequestId=request.RequestId,
                SetupHash=em.GetComponentData<SkirmishExpandedSessionComponent>(request.Session).SetupHash,
                AcceptedPosition=context.Position,PadPosition=context.PadPosition,
                ServiceFacilityStableId=context.FacilityStableId,ServiceFacilityPosition=context.FacilityPosition,FractionNumerator=plan.FractionNumerator,
                FractionDenominator=plan.FractionDenominator,DurationTicks=plan.DurationTicks,RemainingTicks=plan.DurationTicks,
                PaidMaterials=plan.Materials,LastClockSeconds=context.Clock.ElapsedSeconds,
                LastHealth=context.Health.Current,LastMaximum=context.Health.Max};
            var repairLedger=BeginRepairLedger(em,actor,state);
            em.SetComponentData(context.Bank,materials);Set(em,actor,state);EndRepairLedger(em,repairLedger,"DebitCommitted");error=null;return true;
        }

        public static bool Tick(EntityManager em,Entity actor,out string error)
        {
            error=null;
            if(!em.Exists(actor)||!em.HasComponent<VehicleRepairStateComponent>(actor))return true;
            var state=em.GetComponentData<VehicleRepairStateComponent>(actor);
            if((state.Phase==VehicleRepairPhase.Working||state.Phase==VehicleRepairPhase.AccountingFailure)&&
                !ReceiptIdentity(em,actor,state,out _,out _)){error="Repair reservation identity changed.";return false;}
            if(state.Phase==VehicleRepairPhase.AccountingFailure)
            {
                if(!em.HasComponent<UnitHealth>(actor)||em.GetComponentData<UnitHealth>(actor).Current<=0||em.HasComponent<UnitDeathAnimationComponent>(actor))
                {Cancel(em,actor,ref state,true);return true;}
                return RetryRefund(em,actor,ref state);
            }
            if(state.Phase!=VehicleRepairPhase.Working)return true;
            bool dead=!em.HasComponent<UnitHealth>(actor)||em.GetComponentData<UnitHealth>(actor).Current<=0||em.HasComponent<UnitDeathAnimationComponent>(actor);
            if(dead){Cancel(em,actor,ref state,true);return true;}
            if(!Session(em,state.Session,out var native,out var clock)||native.SessionId!=state.AttemptId)
            {error="Repair session identity changed.";return false;}
            // Match pause/result freeze work; they are never wall-clock progress.
            if(clock.Playing==0||clock.Paused!=0||native.Phase!=SkirmishSessionPhase.Playing)return true;
            if(!double.IsFinite(clock.ElapsedSeconds)||clock.ElapsedSeconds<state.LastClockSeconds)
            {error="Repair authoritative clock regressed.";return false;}
            if(!AccrueWork(em,ref state,out error))return false;
            if(state.IncomingDamage!=0||state.NewMovement!=0||!TryContext(em,state.Session,actor,state.Faction,out var context,state.ServiceFacility)||context.Owned.StableObjectId!=state.ActorStableId||
                context.Facility!=state.ServiceFacility||context.Bank!=state.MaterialsBank||context.Pad.PadId!=state.PadId.ToString()||
                context.FacilityStableId!=state.ServiceFacilityStableId||math.distancesq(context.FacilityPosition,state.ServiceFacilityPosition)>.000001f||
                math.distancesq(context.PadPosition,state.PadPosition)>.000001f||
                math.distancesq(context.Position,state.AcceptedPosition)>.000001f||
                VehicleRepairRules.TookDamage(state.LastHealth,state.LastMaximum,context.Health.Current,context.Health.Max))
                return Cancel(em,actor,ref state,false);
            state.LastHealth=context.Health.Current;state.LastMaximum=context.Health.Max;
            RepairLedgerWindow completionLedger=null;
            if(state.RemainingTicks==0)
            {
                completionLedger=BeginRepairLedger(em,actor,state);
                int heal=VehicleRepairRules.CompletionHeal(state.FractionNumerator,state.FractionDenominator,context.Health.Current,context.Health.Max);
                var hp=context.Health;hp.Current+=heal;em.SetComponentData(actor,hp);state.Phase=VehicleRepairPhase.Completed;
            }
            em.SetComponentData(actor,state);EndRepairLedger(em,completionLedger,"Completed");return true;
        }

        private static bool Cancel(EntityManager em,Entity actor,ref VehicleRepairStateComponent state,bool destroyed)
        {
            state.RefundDue=VehicleRepairRules.Refund(state.PaidMaterials,state.DurationTicks-state.RemainingTicks,destroyed);
            state.Phase=destroyed?VehicleRepairPhase.Destroyed:VehicleRepairPhase.Cancelled;
            if(state.RefundDue==0){var ledger=BeginRepairLedger(em,actor,state);em.SetComponentData(actor,state);EndRepairLedger(em,ledger,destroyed?"Destroyed":"CancelledNoRefund");return true;}
            return RetryRefund(em,actor,ref state);
        }
        private static bool RetryRefund(EntityManager em,Entity actor,ref VehicleRepairStateComponent state)
        {
            if(!ReceiptIdentity(em,actor,state,out _,out _))return false;
            if(state.RefundedMaterials==state.RefundDue){var settledLedger=BeginRepairLedger(em,actor,state);state.Phase=VehicleRepairPhase.Cancelled;em.SetComponentData(actor,state);EndRepairLedger(em,settledLedger,"RefundSettled");return true;}
            if(!em.Exists(state.MaterialsBank)||!em.HasComponent<FactionTacticalMaterialsComponent>(state.MaterialsBank))
            {var deferredLedger=BeginDeferredRepairLedger(em,actor,state);state.Phase=VehicleRepairPhase.AccountingFailure;em.SetComponentData(actor,state);EndRepairLedger(em,deferredLedger,"RefundDeferred");return false;}
            var materials=em.GetComponentData<FactionTacticalMaterialsComponent>(state.MaterialsBank);
            int remaining=state.RefundDue-state.RefundedMaterials;
            if(materials.FactionId!=state.Faction||FactionTacticalMaterialsUtilitySystemHelper.TryRefundRepair(ref materials,remaining)!=FactionTacticalMaterialsMutationResult.Applied)
            {var deferredLedger=BeginDeferredRepairLedger(em,actor,state);state.Phase=VehicleRepairPhase.AccountingFailure;em.SetComponentData(actor,state);EndRepairLedger(em,deferredLedger,"RefundDeferred");return false;}
            // Shared Materials refund owner; never a reward grant.
            var repairLedger=BeginRepairLedger(em,actor,state);
            em.SetComponentData(state.MaterialsBank,materials);state.RefundedMaterials+=remaining;
            state.Phase=VehicleRepairPhase.Cancelled;em.SetComponentData(actor,state);EndRepairLedger(em,repairLedger,"RefundCommitted");return true;
        }

        private static bool AccrueWork(EntityManager em,ref VehicleRepairStateComponent state,out string error)
        {
            error="Repair authoritative clock is invalid.";
            if(!em.Exists(state.Session)||!em.HasComponent<SkirmishObjectiveClockComponent>(state.Session))return false;
            var clock=em.GetComponentData<SkirmishObjectiveClockComponent>(state.Session);
            if(clock.Playing==0||clock.Paused!=0){error=null;return true;}
            double exact=(clock.ElapsedSeconds-state.LastClockSeconds)*TimeSpan.TicksPerSecond+state.FractionalTicks;
            if(!double.IsFinite(exact)||exact<0||exact>long.MaxValue)return false;
            long elapsed=(long)Math.Floor(exact);state.FractionalTicks=exact-elapsed;state.LastClockSeconds=clock.ElapsedSeconds;
            state.RemainingTicks=Math.Max(0,state.RemainingTicks-Math.Min(elapsed,state.RemainingTicks));error=null;return true;
        }
        private static bool ReceiptIdentity(EntityManager em,Entity actor,in VehicleRepairStateComponent state,
            out SkirmishExpandedSessionComponent native,out SkirmishObjectiveClockComponent clock)
        {
            native=default;clock=default;
            if(!em.Exists(actor)||!em.HasComponent<SkirmishAttemptOwnedComponent>(actor)||!em.HasComponent<Faction>(actor)||
                !em.Exists(state.Session)||!em.HasComponent<SkirmishExpandedSessionComponent>(state.Session)||
                !em.HasComponent<SkirmishResolvedSetupRecord>(state.Session)||!em.HasComponent<SkirmishObjectiveClockComponent>(state.Session)||
                !em.HasComponent<SkirmishLaunchDefinitionRecord>(state.Session)||state.RequestId==0||state.SetupHash==0||
                state.Faction<1||state.Faction>2||state.AttemptId.IsEmpty||state.ActorStableId.IsEmpty||state.PadId.IsEmpty||state.ServiceFacilityStableId.IsEmpty||
                !math.all(math.isfinite(state.ServiceFacilityPosition))||!math.all(math.isfinite(state.AcceptedPosition))||!math.all(math.isfinite(state.PadPosition)))return false;
            native=em.GetComponentData<SkirmishExpandedSessionComponent>(state.Session);clock=em.GetComponentData<SkirmishObjectiveClockComponent>(state.Session);
            var owner=em.GetComponentData<SkirmishAttemptOwnedComponent>(actor);
            var launch=em.GetComponentObject<SkirmishLaunchDefinitionRecord>(state.Session);
            if(native.SessionId!=state.AttemptId||native.SetupHash!=state.SetupHash||owner.SessionId!=state.AttemptId||owner.StableObjectId!=state.ActorStableId||
                owner.IsStructure!=0||owner.FactionId!=state.Faction||em.GetComponentData<Faction>(actor).Id!=state.Faction||
                launch==null||!launch.MatchesIdentity(em.GetComponentObject<SkirmishResolvedSetupRecord>(state.Session).Setup,native))return false;
            Entity bank=Entity.Null;
            using var banks=em.CreateEntityQuery(typeof(FactionEconomy),typeof(FactionTacticalMaterialsComponent));
            using var entities=banks.ToEntityArray(Allocator.Temp);
            foreach(var entity in entities)
            {
                if(em.GetComponentData<FactionEconomy>(entity).FactionId!=state.Faction||em.GetComponentData<FactionTacticalMaterialsComponent>(entity).FactionId!=state.Faction)continue;
                if(bank!=Entity.Null)return false;bank=entity;
            }
            return bank!=Entity.Null&&bank==state.MaterialsBank;
        }

        private struct Context
        {
            public Entity Facility,Bank;public SkirmishAttemptOwnedComponent Owned;public SkirmishObjectiveClockComponent Clock;
            public UnitHealth Health;public FactionTacticalMaterialsComponent Materials;public SkirmishLegalPadConfig Pad;
            public FixedString64Bytes FacilityStableId;public float3 Position,PadPosition,FacilityPosition;
        }
        // Shared spatial predicate only; callers retain actual session/facility/movement/threat gates.
        // Preserve the original comparison exactly, including its floating-point behavior.
        public static bool IsWithinServiceRegion(float3 position,float2 center)
            =>math.all(math.isfinite(position))&&!(math.distancesq(position.xz,center)>144);

        private static bool TryContext(EntityManager em,Entity session,Entity actor,byte faction,out Context context,Entity requiredFacility=default)
        {
            context=default;
            if(!BasicActor(em,session,actor,faction,out var native,out var clock)||clock.Paused!=0||
                !em.HasComponent<SkirmishLaunchDefinitionRecord>(session))return false;
            var launch=em.GetComponentObject<SkirmishLaunchDefinitionRecord>(session);
            if(launch==null||!launch.Matches(em.GetComponentObject<SkirmishResolvedSetupRecord>(session).Setup,native)||
                !launch.Definition.MapLayout.TryGetPad(faction,SkirmishLegalPadKind.Service,out var pad)||
                !launch.Definition.MapLayout.TryProjectLocalToMap(pad.CenterX,pad.CenterZ,out float x,out float z))return false;
            var position=em.GetComponentData<LocalTransform>(actor).Position;
            if(!IsWithinServiceRegion(position,new float2(x,z))||Moving(em,actor)||VisibleThreat(em,native.SessionId,faction,position))return false;
            Entity facility=Entity.Null,bank=Entity.Null;FixedString64Bytes facilityId=default;float3 facilityPosition=default;
            double nearestDistance=double.PositiveInfinity;var facilityIds=new HashSet<string>(StringComparer.Ordinal);
            using(var facilities=em.CreateEntityQuery(typeof(SkirmishAttemptOwnedComponent),typeof(SkirmishStructureIdentityComponent),typeof(UnitHealth),typeof(Faction)))
            using(var entities=facilities.ToEntityArray(Allocator.Temp))foreach(var entity in entities)
            {
                var own=em.GetComponentData<SkirmishAttemptOwnedComponent>(entity);
                if(own.SessionId!=native.SessionId||own.FactionId!=faction||own.IsStructure==0||own.CapacityReleased!=0||
                    em.GetComponentData<Faction>(entity).Id!=faction||em.GetComponentData<UnitHealth>(entity).Current<=0||
                    !em.HasComponent<SkirmishSharedActorTag>(entity)||em.HasComponent<UnitDeathAnimationComponent>(entity)||
                    em.GetComponentData<SkirmishStructureIdentityComponent>(entity).StructureId.ToString()!=SkirmishStructureIds.GroundStaging)continue;
                // The source-authored service point stays fixed. Bind a real
                // normal-placement facility by finite, unparented world position;
                // no extra pad, relocated building or guessed infrastructure radius.
                if(own.StableObjectId.IsEmpty||!facilityIds.Add(own.StableObjectId.ToString()))return false;
                if(em.HasComponent<Parent>(entity)||!em.HasComponent<LocalTransform>(entity))continue;
                var actualPosition=em.GetComponentData<LocalTransform>(entity).Position;
                if(!math.all(math.isfinite(actualPosition)))continue;
                if(requiredFacility!=Entity.Null&&entity!=requiredFacility)continue;
                double dx=(double)actualPosition.x-x,dz=(double)actualPosition.z-z,distance=dx*dx+dz*dz;
                if(distance>nearestDistance||distance==nearestDistance&&
                    StringComparer.Ordinal.Compare(own.StableObjectId.ToString(),facilityId.ToString())>=0)continue;
                nearestDistance=distance;facility=entity;facilityId=own.StableObjectId;facilityPosition=actualPosition;
            }
            using(var banks=em.CreateEntityQuery(typeof(FactionEconomy),typeof(FactionTacticalMaterialsComponent)))
            using(var entities=banks.ToEntityArray(Allocator.Temp))foreach(var entity in entities)
            {
                if(em.GetComponentData<FactionEconomy>(entity).FactionId!=faction||em.GetComponentData<FactionTacticalMaterialsComponent>(entity).FactionId!=faction)continue;
                if(bank!=Entity.Null)return false;bank=entity;
            }
            if(facility==Entity.Null||bank==Entity.Null)return false;
            context=new Context{Facility=facility,Bank=bank,Owned=em.GetComponentData<SkirmishAttemptOwnedComponent>(actor),Clock=clock,
                Health=em.GetComponentData<UnitHealth>(actor),Materials=em.GetComponentData<FactionTacticalMaterialsComponent>(bank),
                Pad=pad,Position=position,PadPosition=new float3(x,0,z),FacilityStableId=facilityId,FacilityPosition=facilityPosition};return true;
        }
        private static bool BasicActor(EntityManager em,Entity session,Entity actor,byte faction,
            out SkirmishExpandedSessionComponent native,out SkirmishObjectiveClockComponent clock)
        {
            if(!Session(em,session,out native,out clock)||native.Phase!=SkirmishSessionPhase.Playing||native.SpawnComplete==0||
                clock.Playing==0||faction<1||faction>2||!em.Exists(actor)||em.HasComponent<Disabled>(actor)||
                em.HasComponent<Prefab>(actor)||em.HasComponent<UnitDeathAnimationComponent>(actor)||em.HasComponent<Parent>(actor)||em.HasComponent<UnitAirMovement>(actor)||
                em.HasComponent<UnitAirComponent>(actor)||em.HasComponent<UnitTransportPassenger>(actor)||em.HasComponent<UnitTransportCargoPassenger>(actor)||
                !em.HasComponent<SkirmishAttemptOwnedComponent>(actor)||!em.HasComponent<SkirmishUnitRoleComponent>(actor)||
                !em.HasComponent<SkirmishSharedActorTag>(actor)||!em.HasComponent<Faction>(actor)||!em.HasComponent<UnitHealth>(actor)||
                !em.HasComponent<LocalTransform>(actor)||!em.HasComponent<UnitMovementBehavior>(actor)||
                em.GetComponentData<UnitMovementBehavior>(actor).UsesVehicleMotion==0)return false;
            var own=em.GetComponentData<SkirmishAttemptOwnedComponent>(actor);var role=em.GetComponentData<SkirmishUnitRoleComponent>(actor);
            return own.SessionId==native.SessionId&&own.FactionId==faction&&own.IsStructure==0&&own.CapacityReleased==0&&
                em.GetComponentData<Faction>(actor).Id==faction&&em.GetComponentData<UnitHealth>(actor).Current>0&&
                role.Category==SkirmishRoleIds.Category(role.Role)&&
                (role.Category==SkirmishPopulationCategory.Ground||role.Category==SkirmishPopulationCategory.LogisticsSupport||
                    role.Category==SkirmishPopulationCategory.ObjectiveSupport&&role.Role==SkirmishRoleKind.ConvoyLogic);
        }
        private static bool Session(EntityManager em,Entity session,out SkirmishExpandedSessionComponent native,out SkirmishObjectiveClockComponent clock)
        {
            native=default;clock=default;
            if(!em.Exists(session)||!em.HasComponent<SkirmishExpandedSessionComponent>(session)||
                !em.HasComponent<SkirmishObjectiveClockComponent>(session)||!em.HasComponent<SkirmishResolvedSetupRecord>(session))return false;
            native=em.GetComponentData<SkirmishExpandedSessionComponent>(session);clock=em.GetComponentData<SkirmishObjectiveClockComponent>(session);
            return native.IsLegacy==0&&!native.SessionId.IsEmpty&&
                (!em.HasComponent<SkirmishObjectiveStateComponent>(session)||em.GetComponentData<SkirmishObjectiveStateComponent>(session).Terminal==0&&
                    em.GetComponentData<SkirmishObjectiveStateComponent>(session).Outcome==SkirmishOutcomeKind.None);
        }
        private static bool Moving(EntityManager em,Entity actor)=>em.HasComponent<ManualMoveOrderTag>(actor)||
            em.HasComponent<UnitPathRequest>(actor)||em.HasComponent<UnitPathFollow>(actor)||em.HasComponent<UnitLongDistanceMove>(actor);
        private static bool VisibleThreat(EntityManager em,FixedString64Bytes attempt,byte faction,float3 position)
        {
            using var query=em.CreateEntityQuery(typeof(SkirmishAttemptOwnedComponent),typeof(Faction),typeof(UnitHealth),typeof(LocalTransform));
            using var entities=query.ToEntityArray(Allocator.Temp);
            foreach(var entity in entities)
            {
                var own=em.GetComponentData<SkirmishAttemptOwnedComponent>(entity);
                if(own.SessionId!=attempt||own.FactionId==faction||own.FactionId<1||own.FactionId>2||own.CapacityReleased!=0||
                    em.GetComponentData<Faction>(entity).Id!=own.FactionId||em.GetComponentData<UnitHealth>(entity).Current<=0||em.HasComponent<UnitDeathAnimationComponent>(entity)||
                    em.HasComponent<Parent>(entity)||em.HasComponent<UnitTransportPassenger>(entity)||em.HasComponent<UnitTransportCargoPassenger>(entity)||
                    !SkirmishFogService.IsVisibleToFaction(em,entity,faction))continue;
                bool armed=em.HasComponent<UnitCombat>(entity)&&em.GetComponentData<UnitCombat>(entity).CanAttack!=0||
                    em.HasComponent<UnitAttack>(entity)&&em.GetComponentData<UnitAttack>(entity).Damage>0||
                    em.HasComponent<BuildingDefenseWeapon>(entity)&&em.GetComponentData<BuildingDefenseWeapon>(entity).Damage>0;
                if(!armed)continue;
                var target=em.GetComponentData<LocalTransform>(entity).Position;
                if(math.all(math.isfinite(target))&&math.distancesq(target.xz,position.xz)<=400)return true;
            }
            return false;
        }
        private static void Set<T>(EntityManager em,Entity entity,T value)where T:unmanaged,IComponentData
        {if(em.HasComponent<T>(entity))em.SetComponentData(entity,value);else em.AddComponentData(entity,value);}
    }
}
