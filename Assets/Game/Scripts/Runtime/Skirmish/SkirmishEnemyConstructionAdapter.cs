using System;
using Game.Components;
using Game.Configs;
using Game.Skirmish.Contracts;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Game.Runtime
{
    // One session-owned paid intent; the existing native construction owner alone
    // validates terrain, creates the body/blocker/storage and returns the result.
    public static class SkirmishEnemyConstructionAdapter
    {
        public readonly struct Quote
        {
            public readonly FixedString64Bytes SessionId, PadId;
            public readonly FixedString128Bytes BuildingId;
            public readonly int2 Origin, Footprint;
            public readonly int MaterialsCost, DesiredCount, ReserveMaterials, CandidateIndex;
            internal Quote(FixedString64Bytes session, string pad, string building, int2 origin,
                int2 footprint, int cost, int desired, int reserve, int candidate)
            { SessionId=session;PadId=new FixedString64Bytes(pad);BuildingId=new FixedString128Bytes(building);
              Origin=origin;Footprint=footprint;MaterialsCost=cost;DesiredCount=desired;ReserveMaterials=reserve;CandidateIndex=candidate; }
        }
        public static bool IsOutstanding(EntityManager em, Entity session) =>
            em.Exists(session) && em.HasComponent<SkirmishEnemyConstructionIntentV1Component>(session) &&
            IsOutstanding(em.GetComponentData<SkirmishEnemyConstructionIntentV1Component>(session));
        // RefundPending is an exact retained settlement debt, not a second native
        // purchase. Ordinary paid spending may create capacity for its native refund.
        public static bool BlocksOtherPaidActions(EntityManager em,Entity session)
        {
            if(!IsOutstanding(em,session))return false;
            return em.GetComponentData<SkirmishEnemyConstructionIntentV1Component>(session).Phase!=(byte)SkirmishEnemyConstructionPhase.RefundPending;
        }
        public static bool IsOutstanding(in SkirmishEnemyConstructionIntentV1Component i) =>
            i.Phase==(byte)SkirmishEnemyConstructionPhase.PaidPending ||
            i.Phase==(byte)SkirmishEnemyConstructionPhase.AwaitingAdoption ||
            i.Phase==(byte)SkirmishEnemyConstructionPhase.RefundPending;

        public static bool TryPrepare(EntityManager em,Entity session,string structureId,int reserveMaterials,
            out Quote quote,int desiredCount=1)
        {
            quote=default;
            if(!TryActive(em,session,out var setup,out _) || reserveMaterials<0 || desiredCount<1 || desiredCount>2 ||
                BlocksOtherPaidActions(em,session) || OtherPaidIntent(em,session))return false;
            string key=SkirmishStructureIds.VisualKey(structureId);
            if(string.IsNullOrEmpty(key) || !TryConfigured(em,key,out var row) || row.CanRequest!=1 ||
                row.MaterialsCost<=0 || math.any(row.FootprintCells<=0) || LivingCount(em,session,key)>=desiredCount ||
                !TryProjectBank(em,session,row.MaterialsCost,reserveMaterials,out _,out _,out _,out _) || !TryGrid(em,out var grid))return false;
            var padKind=PadKind(structureId);
            if(padKind==SkirmishLegalPadKind.None)return false;
            // Deterministic bounded existing-pad search; no relocation or invented pad.
            int padIndex=-1;
            foreach(var pad in setup.EnemyPolicy.ConstructionPads)
            {
                padIndex++;
                if(pad.Kind!=padKind && !(padKind==SkirmishLegalPadKind.Supply && pad.Kind==SkirmishLegalPadKind.SupplyExpansion))continue;
                int2 center=GridUtils.WorldToCell(grid,new float3(pad.X,0,pad.Z));
                int2 half=row.FootprintCells/2;
                for(int offset=0;offset<9;offset++)
                {
                    int n=offset==0?4:(offset-1<4?offset-1:offset);
                    int dx=n%3-1,dz=n/3-1,candidate=padIndex*9+offset;
                    if(em.HasComponent<SkirmishEnemyConstructionIntentV1Component>(session))
                    {var previous=em.GetComponentData<SkirmishEnemyConstructionIntentV1Component>(session);
                     if((previous.Phase==(byte)SkirmishEnemyConstructionPhase.Rejected || previous.Phase==(byte)SkirmishEnemyConstructionPhase.RefundPending) && previous.BuildingId.ToString()==key && candidate<=previous.CandidateIndex)continue;}
                    int2 origin=center-half+new int2(dx*row.FootprintCells.x,dz*row.FootprintCells.y);
                    if(!FitsPad(pad,grid,origin,row.FootprintCells) || OverlapsOwnRuntime(em,session,origin,row.FootprintCells))continue;
                    quote=new Quote(em.GetComponentData<SkirmishExpandedSessionComponent>(session).SessionId,
                        pad.Id,key,origin,row.FootprintCells,row.MaterialsCost,desiredCount,reserveMaterials,candidate);return true;
                }
            }
            return false;
        }
        // Invoked by the single strategic/economy arbiter, never an independent spend loop.
        public static bool TryRequest(EntityManager em,Entity session,in Quote quote,FixedString64Bytes actionKey,uint planRevision)
        {
            if(actionKey.IsEmpty || !TryActive(em,session,out var setup,out float now) || BlocksOtherPaidActions(em,session) ||
                OtherPaidIntent(em,session) || !quote.SessionId.Equals(em.GetComponentData<SkirmishExpandedSessionComponent>(session).SessionId) ||
                !ValidateQuote(em,session,setup,quote))return false;
            if(em.HasComponent<SkirmishEnemyConstructionIntentV1Component>(session))
            {
                var old=em.GetComponentData<SkirmishEnemyConstructionIntentV1Component>(session);
                if(old.ActionKey.Equals(actionKey) || now<old.RetryAfter)return false;
            }
            // Project both genuine native mutations on local copies. Full-bank
            // settlement may use a legal next purchase to create exact headroom.
            // Neither bank nor old debt changes unless native enqueue succeeds.
            if(!TryProjectBank(em,session,quote.MaterialsCost,quote.ReserveMaterials,
                out var bank,out var economy,out var materials,out var oldDebt))return false;
            // Establish optional part before taking buffer handles or debiting.
            if(!em.HasComponent<SkirmishEnemyConstructionIntentV1Component>(session))
                em.AddComponentData(session,new SkirmishEnemyConstructionIntentV1Component{Version=1,SessionId=quote.SessionId});
            var oldIntent=em.GetComponentData<SkirmishEnemyConstructionIntentV1Component>(session);
            var ledgerBefore=SkirmishConstructionLedger.Begin(em,bank,session);
            if(!Enqueue(em,session,quote.BuildingId,quote.Origin,quote.MaterialsCost,out int requestId))return false;
            if(oldDebt>0)RemoveExact(em,session,oldIntent.RequestId,oldIntent.BuildingId,oldIntent.Origin);
            em.SetComponentData(bank,economy);em.SetComponentData(bank,materials);
            em.SetComponentData(session,new SkirmishEnemyConstructionIntentV1Component{
                Version=1,Phase=(byte)SkirmishEnemyConstructionPhase.PaidPending,SessionId=quote.SessionId,
                ActionKey=actionKey,PadId=quote.PadId,BuildingId=quote.BuildingId,Origin=quote.Origin,Footprint=quote.Footprint,
                MaterialsPaid=quote.MaterialsCost,DesiredCount=quote.DesiredCount,RequestId=requestId,CandidateIndex=quote.CandidateIndex,
                FailedResearchId=oldIntent.FailedResearchId,PreviousProducerStableId=oldIntent.PreviousProducerStableId,
                PlanRevision=planRevision,RequestedAt=now,RejectionCount=oldIntent.BuildingId.Equals(quote.BuildingId)?(byte)Math.Min(3,oldIntent.RejectionCount+(oldDebt>0?1:0)):(byte)0});
            SkirmishConstructionLedger.End(ledgerBefore,"EnemyRequest",requestId,0,quote.MaterialsCost,quote.BuildingId.ToString(),quote.Origin);
            return true;
        }
        static bool TryProjectBank(EntityManager em,Entity session,int cost,int reserve,
            out Entity bank,out FactionEconomy economy,out FactionTacticalMaterialsComponent materials,out int debt)
        {
            bank=SkirmishMaterialsService.FindEconomy(em,2);economy=default;materials=default;debt=0;
            if(cost<=0 || reserve<0 || bank==Entity.Null || !em.HasComponent<FactionTacticalMaterialsComponent>(bank))return false;
            if(em.HasComponent<SkirmishEnemyConstructionIntentV1Component>(session))
            {
                var old=em.GetComponentData<SkirmishEnemyConstructionIntentV1Component>(session);
                if(IsOutstanding(old))
                {
                    if(old.Phase!=(byte)SkirmishEnemyConstructionPhase.RefundPending || old.Version!=1 || old.MaterialsPaid<=0 ||
                        !old.SessionId.Equals(em.GetComponentData<SkirmishExpandedSessionComponent>(session).SessionId))return false;
                    debt=old.MaterialsPaid;
                }
            }
            economy=em.GetComponentData<FactionEconomy>(bank);materials=em.GetComponentData<FactionTacticalMaterialsComponent>(bank);
            if(economy.MaterialsOnlyConstruction==0 || materials.Current<(long)cost+reserve ||
                FactionConstructionResourceUtilitySystemHelper.TrySpend(ref economy,ref materials,0,cost)!=FactionConstructionResourceMutationResult.Applied)return false;
            return debt==0 || FactionConstructionResourceUtilitySystemHelper.TryRollback(ref economy,ref materials,0,debt)==FactionConstructionResourceMutationResult.Applied;
        }
        public static void Tick(EntityManager em,Entity session)
        {
            if(!TryActive(em,session,out var setup,out float now) || !em.HasComponent<SkirmishEnemyConstructionIntentV1Component>(session))return;
            var i=em.GetComponentData<SkirmishEnemyConstructionIntentV1Component>(session);
            if(!i.SessionId.Equals(em.GetComponentData<SkirmishExpandedSessionComponent>(session).SessionId) || i.Version!=1)return;
            if(i.Phase==(byte)SkirmishEnemyConstructionPhase.Delivered){RemoveExact(em,session,i.RequestId,i.BuildingId,i.Origin);return;}
            if(i.Phase==(byte)SkirmishEnemyConstructionPhase.RefundPending){RemoveExact(em,session,i.RequestId,i.BuildingId,i.Origin);Refund(em,session,ref i,now);return;}
            if(i.Phase!=(byte)SkirmishEnemyConstructionPhase.PaidPending && i.Phase!=(byte)SkirmishEnemyConstructionPhase.AwaitingAdoption)return;
            if(!TryBoundary(em,out var boundary))return;
            int matches=0;BuildingRuntimeSpawnRequest request=default;
            foreach(var r in em.GetBuffer<BuildingRuntimeSpawnRequest>(boundary))if(Matches(session,i,r)){matches++;request=r;}
            if(matches>1)throw new InvalidOperationException("Duplicate enemy construction correlation.");
            if(matches==1)
            {
                if(request.Status==BuildingRuntimeSpawnRequest.Pending)return;
                if(request.Status==BuildingRuntimeSpawnRequest.Succeeded)
                { RecordNativeResult(em,request);return; }
                var debtBefore=SkirmishConstructionLedger.Begin(em,SkirmishMaterialsService.FindEconomy(em,2),session);i.Phase=(byte)SkirmishEnemyConstructionPhase.RefundPending;em.SetComponentData(session,i);SkirmishConstructionLedger.End(debtBefore,"EnemyRefundDebt",i.RequestId,0,i.MaterialsPaid);
                RemoveExact(em,session,i.RequestId,i.BuildingId,i.Origin);Refund(em,session,ref i,now);return;
            }
            // Transient request buffer is intentionally absent after Continue. Only
            // this persisted already-paid token can recreate its exact request, no debit.
            if(i.Phase==(byte)SkirmishEnemyConstructionPhase.AwaitingAdoption)
                throw new InvalidOperationException("Accepted construction lost its native result before adoption.");
            if(!SavedIntentFits(setup,i) || !TryConfigured(em,i.BuildingId.ToString(),out var row) || row.CanRequest!=1 ||
                row.MaterialsCost!=i.MaterialsPaid || math.any(row.FootprintCells!=i.Footprint))
            { var debtBefore=SkirmishConstructionLedger.Begin(em,SkirmishMaterialsService.FindEconomy(em,2),session);i.Phase=(byte)SkirmishEnemyConstructionPhase.RefundPending;em.SetComponentData(session,i);SkirmishConstructionLedger.End(debtBefore,"EnemyRefundDebt",i.RequestId,0,i.MaterialsPaid);Refund(em,session,ref i,now);return; }
            if(Enqueue(em,session,i.BuildingId,i.Origin,i.MaterialsPaid,out int id)){var recreated=SkirmishConstructionLedger.Begin(em,SkirmishMaterialsService.FindEconomy(em,2),session);i.RequestId=id;em.SetComponentData(session,i);SkirmishConstructionLedger.End(recreated,"EnemyReenqueue",id,0,i.MaterialsPaid);}
        }
        // Native processing calls this BEFORE accepting an exact enemy-paid row.
        // Null PlanEntity and unrelated Campaign/player requests preserve legacy behavior.
        internal static bool IsConstructionRequest(in BuildingRuntimeSpawnRequest r) => r.RequestKind==BuildingRuntimeSpawnRequest.KindSkirmishPaidConstruction;
        internal static bool IsOwnedRequest(EntityManager em,in BuildingRuntimeSpawnRequest r) =>
            IsConstructionRequest(r) &&
            r.PlanEntity!=Entity.Null && em.Exists(r.PlanEntity) && em.HasComponent<SkirmishEnemyConstructionIntentV1Component>(r.PlanEntity) &&
            Matches(r.PlanEntity,em.GetComponentData<SkirmishEnemyConstructionIntentV1Component>(r.PlanEntity),r);
        internal static bool CanProcess(EntityManager em,in BuildingRuntimeSpawnRequest r,out bool defer)
        {
            defer=false;if(!IsOwnedRequest(em,r))return false;
            var i=em.GetComponentData<SkirmishEnemyConstructionIntentV1Component>(r.PlanEntity);
            if(i.Phase!=(byte)SkirmishEnemyConstructionPhase.PaidPending || r.AuthoredStartingGrant!=0 || r.AllowNonBuildableEnemy!=0 ||
                r.RequirePreferredOrigin!=1 || r.HasOwnerFaction!=1 || r.MaterialsCost!=i.MaterialsPaid || r.RequestKind!=BuildingRuntimeSpawnRequest.KindSkirmishPaidConstruction ||
                !em.HasComponent<SkirmishExpandedSessionComponent>(r.PlanEntity))return false;
            var state=em.GetComponentData<SkirmishExpandedSessionComponent>(r.PlanEntity);
            if(state.IsLegacy!=0 || state.Phase!=SkirmishSessionPhase.Playing || !state.SessionId.Equals(i.SessionId))return false;
            if(!TryPolicy(em,r.PlanEntity,out var setup) || !em.HasComponent<SkirmishObjectiveClockComponent>(r.PlanEntity))return false;
            var clock=em.GetComponentData<SkirmishObjectiveClockComponent>(r.PlanEntity);
            if(!float.IsFinite(clock.ElapsedSeconds) || clock.ElapsedSeconds<0)return false;
            if(state.SpawnComplete==0 || clock.Paused!=0 || clock.Playing==0){defer=true;return false;}
            using(var gameplay=em.CreateEntityQuery(typeof(RuntimeGameplayStateComponent)))
            {if(gameplay.CalculateEntityCount()>1)return false;
             if(gameplay.CalculateEntityCount()==1 && gameplay.GetSingleton<RuntimeGameplayStateComponent>().SimulationActive==0){defer=true;return false;}}
            return SavedIntentFits(setup,i) && TryGrid(em,out var grid) && TryPad(setup,i.PadId,out var pad) && FitsPad(pad,grid,i.Origin,i.Footprint);
        }
        internal static bool NativeRectFits(EntityManager em,in BuildingRuntimeSpawnRequest r,int2 origin,int2 size)
        {
            if(!IsOwnedRequest(em,r) || !em.HasComponent<SkirmishResolvedSetupRecord>(r.PlanEntity))return false;
            var setup=em.GetComponentObject<SkirmishResolvedSetupRecord>(r.PlanEntity).Setup;
            var i=em.GetComponentData<SkirmishEnemyConstructionIntentV1Component>(r.PlanEntity);
            return setup?.EnemyPolicy!=null && TryPad(setup,i.PadId,out var pad) && TryGrid(em,out var grid) && FitsPad(pad,grid,origin,size);
        }
        // Atomic result witness/adoption closes success->checkpoint race. Reuses the
        // same BindStructure identity/native census contract, no objective promotion.
        internal static void RecordNativeResult(EntityManager em,in BuildingRuntimeSpawnRequest r)
        {
            if(!IsOwnedRequest(em,r) || r.Status!=BuildingRuntimeSpawnRequest.Succeeded || r.BuildingRuntimeId<=0)return;
            var i=em.GetComponentData<SkirmishEnemyConstructionIntentV1Component>(r.PlanEntity);
            if(i.Phase==(byte)SkirmishEnemyConstructionPhase.Delivered)return;
            if(i.Phase!=(byte)SkirmishEnemyConstructionPhase.PaidPending || math.any(r.ActualOrigin!=i.Origin))
                throw new InvalidOperationException("Native construction accepted a mismatched origin or phase.");
            using var q=em.CreateEntityQuery(typeof(RuntimeBuildingCombatInfo),typeof(UnitHealth),typeof(UnitSourcePrefabKey));
            using var entities=q.ToEntityArray(Allocator.Temp);Entity found=Entity.Null;
            foreach(var actor in entities)
            {
                var info=em.GetComponentData<RuntimeBuildingCombatInfo>(actor);
                if(info.RuntimeBuildingId!=r.BuildingRuntimeId || info.OwnerFactionId!=2 || math.any(info.OriginCell!=r.ActualOrigin) ||
                    math.any(info.FootprintCells!=r.ActualFootprint) || em.HasComponent<OperationMapBuildingComponent>(actor) ||
                    !em.GetComponentData<UnitSourcePrefabKey>(actor).Value.ToString().Equals(i.BuildingId.ToString(),StringComparison.Ordinal))continue;
                if(found!=Entity.Null)throw new InvalidOperationException("Duplicate native construction result.");found=actor;
            }
            if(found==Entity.Null)throw new InvalidOperationException("Native construction result actor missing.");
            if(!em.HasComponent<SkirmishAttemptOwnedComponent>(found))
            {
                SkirmishScenarioSpawnSystem.BindStructure(em,found,i.SessionId,new SkirmishResolvedStructureEntry{
                    FactionId=2,StructureId=SkirmishStructureIds.FromVisualKey(i.BuildingId.ToString()),DesignatedBase=false});
                var owner=em.GetComponentData<SkirmishAttemptOwnedComponent>(found);
                owner.StableObjectId=new FixedString64Bytes("constructed."+r.BuildingRuntimeId);em.SetComponentData(found,owner);
                if(!em.HasComponent<SkirmishSharedActorTag>(found))em.AddComponent<SkirmishSharedActorTag>(found);
                SkirmishRuntimeActorOwnership.DetachFromSourceScene(em,found);
            }
            var actual=em.GetComponentData<SkirmishAttemptOwnedComponent>(found);
            if(!actual.SessionId.Equals(i.SessionId)||actual.FactionId!=2||actual.IsStructure!=1||actual.StableObjectId.IsEmpty)
                throw new InvalidOperationException("Native construction result owner mismatch.");
            var deliveredBefore=SkirmishConstructionLedger.Begin(em,SkirmishMaterialsService.FindEconomy(em,2),r.PlanEntity);
            i.ResultStableObjectId=actual.StableObjectId;i.Phase=(byte)SkirmishEnemyConstructionPhase.Delivered;
            em.SetComponentData(r.PlanEntity,i);
            SkirmishConstructionLedger.End(deliveredBefore,"EnemyDelivered",r.RequestId,0,i.MaterialsPaid,i.BuildingId.ToString(),i.Origin,false,1,1,found);
        }
        internal static void CleanupAttempt(EntityManager em,FixedString64Bytes attempt)
        {
            using var q=em.CreateEntityQuery(typeof(SkirmishExpandedSessionComponent),typeof(SkirmishEnemyConstructionIntentV1Component));
            using var sessions=q.ToEntityArray(Allocator.Temp);
            foreach(var session in sessions)
            {
                var i=em.GetComponentData<SkirmishEnemyConstructionIntentV1Component>(session);if(!i.SessionId.Equals(attempt))continue;
                RemoveExact(em,session,i.RequestId,i.BuildingId,i.Origin);
                if(i.Phase==(byte)SkirmishEnemyConstructionPhase.PaidPending || i.Phase==(byte)SkirmishEnemyConstructionPhase.RefundPending)
                {i.Phase=(byte)SkirmishEnemyConstructionPhase.RefundPending;Refund(em,session,ref i,0);}
            }
        }
        internal static void RecordProducerLoss(EntityManager em,Entity session,Entity structure)
        {
            if(!TryActive(em,session,out _,out _) || !em.Exists(structure) ||
                !em.HasComponent<SkirmishAttemptOwnedComponent>(structure) || !em.HasComponent<UnitHealth>(structure) ||
                !em.HasComponent<SkirmishStructureIdentityComponent>(structure) || em.GetComponentData<UnitHealth>(structure).Current>0 ||
                em.GetComponentData<SkirmishStructureIdentityComponent>(structure).Producer!=SkirmishProducerKind.Barracks ||
                !em.HasBuffer<SkirmishResearchQueueItem>(session))return;
            var owner=em.GetComponentData<SkirmishAttemptOwnedComponent>(structure);
            var attempt=em.GetComponentData<SkirmishExpandedSessionComponent>(session).SessionId;
            if(owner.FactionId!=2 || owner.IsStructure!=1 || !owner.SessionId.Equals(attempt) || owner.StableObjectId.IsEmpty)return;
            SkirmishResearchQueueItem latest=default;
            foreach(var row in em.GetBuffer<SkirmishResearchQueueItem>(session))
                if(row.FactionId==2 && row.Kind==SkirmishResearchKind.Readiness && row.ResearchId>latest.ResearchId)latest=row;
            if(latest.ResearchId==0 || latest.Phase!=SkirmishResearchPhase.Lost)return;
            if(!em.HasComponent<SkirmishEnemyConstructionIntentV1Component>(session))
                em.AddComponentData(session,new SkirmishEnemyConstructionIntentV1Component{Version=1,SessionId=attempt});
            var i=em.GetComponentData<SkirmishEnemyConstructionIntentV1Component>(session);
            if(i.FailedResearchId==latest.ResearchId && !i.PreviousProducerStableId.IsEmpty)return;
            i.FailedResearchId=latest.ResearchId;i.PreviousProducerStableId=owner.StableObjectId;em.SetComponentData(session,i);
        }
        public static bool TryReadResearchRecoveryEvidence(EntityManager em,Entity session,out FixedString64Bytes attempt,
            out uint failedResearchId,out FixedString64Bytes previousProducer,out FixedString64Bytes rebuiltProducer)
        {
            attempt=default;failedResearchId=0;previousProducer=default;rebuiltProducer=default;
            if(!em.Exists(session) || !em.HasComponent<SkirmishEnemyConstructionIntentV1Component>(session) ||
                !em.HasComponent<SkirmishExpandedSessionComponent>(session))return false;
            var i=em.GetComponentData<SkirmishEnemyConstructionIntentV1Component>(session);
            if(i.Version!=1 || i.Phase!=(byte)SkirmishEnemyConstructionPhase.Delivered ||
                i.BuildingId.ToString()!=SkirmishStructureIds.VisualKey(SkirmishStructureIds.Barracks) || i.FailedResearchId==0 ||
                i.PreviousProducerStableId.IsEmpty || i.ResultStableObjectId.IsEmpty || i.PreviousProducerStableId.Equals(i.ResultStableObjectId) ||
                !i.SessionId.Equals(em.GetComponentData<SkirmishExpandedSessionComponent>(session).SessionId))return false;
            attempt=i.SessionId;failedResearchId=i.FailedResearchId;previousProducer=i.PreviousProducerStableId;rebuiltProducer=i.ResultStableObjectId;return true;
        }
        static void Refund(EntityManager em,Entity session,ref SkirmishEnemyConstructionIntentV1Component i,float now)
        {
            if(i.Phase!=(byte)SkirmishEnemyConstructionPhase.RefundPending)return;
            Entity bank=SkirmishMaterialsService.FindEconomy(em,2);if(bank==Entity.Null)return;
            var economy=em.GetComponentData<FactionEconomy>(bank);var materials=em.GetComponentData<FactionTacticalMaterialsComponent>(bank);
            if(FactionConstructionResourceUtilitySystemHelper.TryRollback(ref economy,ref materials,0,i.MaterialsPaid)!=FactionConstructionResourceMutationResult.Applied)return;
            var refundBefore=SkirmishConstructionLedger.Begin(em,bank,session);
            em.SetComponentData(bank,economy);em.SetComponentData(bank,materials);
            i.MaterialsPaid=0;i.Phase=(byte)SkirmishEnemyConstructionPhase.Rejected;i.RejectionCount=(byte)Math.Min(3,i.RejectionCount+1);i.RetryAfter=now+4;
            em.SetComponentData(session,i);
            SkirmishConstructionLedger.End(refundBefore,"EnemyRefund",i.RequestId);
        }
        static bool Enqueue(EntityManager em,Entity session,FixedString128Bytes building,int2 origin,int cost,out int id)
        {
            id=0;if(!TryBoundary(em,out var boundary))return false;
            foreach(var r in em.GetBuffer<BuildingRuntimeSpawnRequest>(boundary))if(r.RequestId>=int.MaxValue-1)return false;
            var helper=new BuildingRuntimeSpawnCommandSystemHelper();
            if(!helper.TryEnqueueRuntimeBuildingSpawnRequest(em,building.ToString(),new Vector2Int(origin.x,origin.y),2,out id))return false;
            var rows=em.GetBuffer<BuildingRuntimeSpawnRequest>(boundary);
            for(int n=0;n<rows.Length;n++)if(rows[n].RequestId==id)
            {var r=rows[n];r.PlanEntity=session;r.RequirePreferredOrigin=1;r.MaterialsCost=cost;r.RequestKind=BuildingRuntimeSpawnRequest.KindSkirmishPaidConstruction;rows[n]=r;return true;}
            return false;
        }
        // Authored visual keys identify actors/checkpoints; native boundary rows
        // use the existing normalized spawnable key. Never rename either owner.
        internal static bool BoundaryKeyEquals(FixedString128Bytes native,FixedString128Bytes visual) =>
            BuildingDefinitionPrefabSystemHelper.NormalizeSpawnableKey(native.ToString())==
            BuildingDefinitionPrefabSystemHelper.NormalizeSpawnableKey(visual.ToString());
        static bool Matches(Entity session,in SkirmishEnemyConstructionIntentV1Component i,in BuildingRuntimeSpawnRequest r) =>
            r.RequestKind==BuildingRuntimeSpawnRequest.KindSkirmishPaidConstruction && r.HasOwnerFaction==1 &&
            r.AuthoredStartingGrant==0 && r.AllowNonBuildableEnemy==0 && r.RequirePreferredOrigin==1 &&
            r.MaterialsCost==i.MaterialsPaid && r.PlanEntity==session && r.FactionId==2 && r.RequestId==i.RequestId &&
            BoundaryKeyEquals(r.BuildingId,i.BuildingId) && math.all(r.PreferredOrigin==i.Origin);
        static void RemoveExact(EntityManager em,Entity session,int id,FixedString128Bytes building,int2 origin)
        {
            if(!TryBoundary(em,out var boundary))return;var rows=em.GetBuffer<BuildingRuntimeSpawnRequest>(boundary);
            for(int n=rows.Length-1;n>=0;n--)if(rows[n].RequestKind==BuildingRuntimeSpawnRequest.KindSkirmishPaidConstruction && rows[n].HasOwnerFaction==1 && rows[n].PlanEntity==session && rows[n].FactionId==2 && rows[n].RequestId==id && BoundaryKeyEquals(rows[n].BuildingId,building) && math.all(rows[n].PreferredOrigin==origin))rows.RemoveAt(n);
        }
        static bool TryPolicy(EntityManager em,Entity session,out SkirmishResolvedSetup setup)
        {
            setup=null;
            if(em==default || !em.Exists(session) || !em.HasComponent<SkirmishResolvedSetupRecord>(session) ||
                !em.HasComponent<SkirmishEnemyPlannerV1Component>(session))return false;
            setup=em.GetComponentObject<SkirmishResolvedSetupRecord>(session).Setup;
            if(setup?.EnemyPolicy==null || setup.EnemyFaction!=2 || (setup.ObjectiveKind!=SkirmishObjectiveKind.BaseAssault&&!SkirmishEnemyObjectivePolicyScope.Supports(setup)))return false;
            var p=em.GetComponentData<SkirmishEnemyPlannerV1Component>(session);
            return p.Initialized==1 && p.PolicyVersion==setup.EnemyPolicy.Version && p.PolicyHash==setup.EnemyPolicy.Hash &&
                p.PolicyFingerprint.ToString()==setup.EnemyPolicy.Fingerprint;
        }
        static bool TryActive(EntityManager em,Entity session,out SkirmishResolvedSetup setup,out float now)
        {
            now=0;
            if(!TryPolicy(em,session,out setup) || !SkirmishEnemyPlannerService.TryReadActiveClock(em,session,out now))return false;
            var state=em.GetComponentData<SkirmishExpandedSessionComponent>(session);
            return state.SpawnComplete==1 && !state.SessionId.IsEmpty;
        }
        static bool OtherPaidIntent(EntityManager em,Entity session)
        {
            if(em.HasComponent<SkirmishEnemyProductionIntentV1Component>(session))
            {var unit=em.GetComponentData<SkirmishEnemyProductionIntentV1Component>(session);
             if(unit.State==SkirmishEnemyProductionIntentState.RequestPending || unit.State==SkirmishEnemyProductionIntentState.AcceptedWaiting)return true;}
            if(em.HasBuffer<SkirmishProductionReservation>(session))foreach(var receipt in em.GetBuffer<SkirmishProductionReservation>(session))
                if(receipt.FactionId==2 && (receipt.Phase==SkirmishReservationPhase.Reserved || receipt.Phase==SkirmishReservationPhase.Producing))return true;
            if(em.HasBuffer<SkirmishResearchQueueItem>(session))foreach(var r in em.GetBuffer<SkirmishResearchQueueItem>(session))
                if(r.FactionId==2 && (r.Phase==SkirmishResearchPhase.Queued || r.Phase==SkirmishResearchPhase.Researching))return true;
            return false;
        }
        static bool TryBoundary(EntityManager em,out Entity boundary)
        {using var q=em.CreateEntityQuery(typeof(BuildingRuntimeStateTag),typeof(BuildingRuntimeSpawnRequest));boundary=q.CalculateEntityCount()==1?q.GetSingletonEntity():Entity.Null;return boundary!=Entity.Null;}
        static bool TryConfigured(EntityManager em,string key,out BuildingConfiguredSpawnableReadModel row)
        {row=default;using var q=em.CreateEntityQuery(typeof(BuildingRuntimeStateTag),typeof(BuildingConfiguredSpawnableReadModel));if(q.CalculateEntityCount()!=1)return false;int matches=0;foreach(var r in em.GetBuffer<BuildingConfiguredSpawnableReadModel>(q.GetSingletonEntity()))if(BoundaryKeyEquals(r.BuildingId,new FixedString128Bytes(key))){row=r;matches++;}return matches==1;}
        static bool TryGrid(EntityManager em,out GridConfig grid)
        {using var q=em.CreateEntityQuery(typeof(GridConfig));grid=default;if(q.CalculateEntityCount()!=1)return false;grid=q.GetSingleton<GridConfig>();return grid.CellSize>0 && float.IsFinite(grid.CellSize);}
        internal static bool FitsPad(SkirmishEnemyConstructionPad pad,GridConfig grid,int2 origin,int2 footprint)
        {
            if(math.any(footprint<=0)||math.any(origin<0)||origin.x>grid.Width-footprint.x||origin.y>grid.Height-footprint.y)return false;
            float x=grid.Origin.x+origin.x*grid.CellSize,z=grid.Origin.z+origin.y*grid.CellSize;
            float ex=x+footprint.x*grid.CellSize,ez=z+footprint.y*grid.CellSize;
            return pad.Contains(x,z)&&pad.Contains(ex,z)&&pad.Contains(x,ez)&&pad.Contains(ex,ez);
        }
        static bool TryPad(SkirmishResolvedSetup setup,FixedString64Bytes id,out SkirmishEnemyConstructionPad pad)
        {pad=default;foreach(var p in setup.EnemyPolicy.ConstructionPads)if(p.Id==id.ToString()){pad=p;return true;}return false;}
        static bool ValidateQuote(EntityManager em,Entity session,SkirmishResolvedSetup setup,in Quote q) =>
            q.MaterialsCost>0 && q.ReserveMaterials>=0 && q.DesiredCount>=1 && q.DesiredCount<=2 && q.CandidateIndex>=0 && q.CandidateIndex/9<setup.EnemyPolicy.ConstructionPads.Count && setup.EnemyPolicy.ConstructionPads[q.CandidateIndex/9].Id==q.PadId.ToString() &&
            (PadKind(SkirmishStructureIds.FromVisualKey(q.BuildingId.ToString()))==setup.EnemyPolicy.ConstructionPads[q.CandidateIndex/9].Kind ||
             PadKind(SkirmishStructureIds.FromVisualKey(q.BuildingId.ToString()))==SkirmishLegalPadKind.Supply && setup.EnemyPolicy.ConstructionPads[q.CandidateIndex/9].Kind==SkirmishLegalPadKind.SupplyExpansion) && TryConfigured(em,q.BuildingId.ToString(),out var row) && row.CanRequest==1 && row.MaterialsCost==q.MaterialsCost && math.all(row.FootprintCells==q.Footprint) && TryPad(setup,q.PadId,out var pad) && TryGrid(em,out var grid) && FitsPad(pad,grid,q.Origin,q.Footprint) && !OverlapsOwnRuntime(em,session,q.Origin,q.Footprint) && LivingCount(em,session,q.BuildingId.ToString())<q.DesiredCount;
        internal static bool SavedIntentFits(SkirmishResolvedSetup setup,in SkirmishEnemyConstructionIntentV1Component i) =>
            setup?.EnemyPolicy!=null && i.Version==1 && !i.SessionId.IsEmpty && !i.ActionKey.IsEmpty && !i.BuildingId.IsEmpty && TryPad(setup,i.PadId,out var pad) &&
            (pad.Faction==0 || pad.Faction==2) && (pad.Kind==PadKind(SkirmishStructureIds.FromVisualKey(i.BuildingId.ToString())) || pad.Kind==SkirmishLegalPadKind.SupplyExpansion && PadKind(SkirmishStructureIds.FromVisualKey(i.BuildingId.ToString()))==SkirmishLegalPadKind.Supply);
        // Pure current-attempt own facility census; accepts logical structure ID.
        public static int CountLiving(EntityManager em,Entity session,string structureId)
        {
            if(!TryPolicy(em,session,out _) || !em.HasComponent<SkirmishExpandedSessionComponent>(session))return 0;
            string building=SkirmishStructureIds.VisualKey(structureId);
            return string.IsNullOrEmpty(building)?0:LivingCount(em,session,building);
        }
        static int LivingCount(EntityManager em,Entity session,string building)
        {
            int count=0;var id=em.GetComponentData<SkirmishExpandedSessionComponent>(session).SessionId;
            using var q=em.CreateEntityQuery(typeof(SkirmishAttemptOwnedComponent),typeof(UnitHealth),typeof(UnitSourcePrefabKey));using var actors=q.ToEntityArray(Allocator.Temp);
            foreach(var actor in actors){var owner=em.GetComponentData<SkirmishAttemptOwnedComponent>(actor);if(owner.SessionId.Equals(id)&&owner.FactionId==2&&owner.IsStructure==1&&em.GetComponentData<UnitHealth>(actor).Current>0&&em.GetComponentData<UnitSourcePrefabKey>(actor).Value.ToString()==building)count++;}return count;
        }
        static bool OverlapsOwnRuntime(EntityManager em,Entity session,int2 origin,int2 footprint)
        {
            using var q=em.CreateEntityQuery(typeof(RuntimeBuildingCombatInfo),typeof(UnitHealth));using var actors=q.ToEntityArray(Allocator.Temp);
            foreach(var actor in actors){if(em.GetComponentData<UnitHealth>(actor).Current<=0)continue;var r=em.GetComponentData<RuntimeBuildingCombatInfo>(actor);
                if(r.OwnerFactionId!=2 || !em.HasComponent<SkirmishAttemptOwnedComponent>(actor) ||
                    !em.GetComponentData<SkirmishAttemptOwnedComponent>(actor).SessionId.Equals(em.GetComponentData<SkirmishExpandedSessionComponent>(session).SessionId))continue;
                if(origin.x<r.OriginCell.x+r.FootprintCells.x&&origin.x+footprint.x>r.OriginCell.x&&origin.y<r.OriginCell.y+r.FootprintCells.y&&origin.y+footprint.y>r.OriginCell.y)return true;}return false;
        }
        static SkirmishLegalPadKind PadKind(string structure) => structure switch {
            SkirmishStructureIds.Barracks=>SkirmishLegalPadKind.BaseBarracks,
            SkirmishStructureIds.GroundStaging=>SkirmishLegalPadKind.GroundStaging,
            SkirmishStructureIds.Helipad=>SkirmishLegalPadKind.AirReturn,
            SkirmishStructureIds.Airport=>SkirmishLegalPadKind.AirReturn,
            SkirmishStructureIds.SatelliteDish=>SkirmishLegalPadKind.Service,
            SkirmishStructureIds.OilPump=>SkirmishLegalPadKind.Supply,
            SkirmishStructureIds.Refinery=>SkirmishLegalPadKind.Supply,
            SkirmishStructureIds.FuelBladder=>SkirmishLegalPadKind.Supply,
            SkirmishStructureIds.FabricationDepot=>SkirmishLegalPadKind.Supply,
            _=>SkirmishLegalPadKind.None };
    }
}
