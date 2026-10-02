using Game.Components;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
namespace Game.Runtime
{
    // One ordered transaction builds and validates copies before any authoritative write.
    public static class SupportFuelTransactionSystem
    {
        private struct Contribution { public Entity Entity; public BuildingResourceStorageComponent Copy; public float Amount; }
        public static float Usable(in BuildingResourceStorageComponent s, byte faction) =>
            s.OwnerFactionId==faction && s.FuelStorageCapacity>0 && s.FuelBarrelsPerDay<=0 && s.OilBarrelsPerDay<=0
            ? math.max(0,s.StoredFuelBarrels-s.ReservedFuelOutboundBarrels-s.CivilianFuelReserveBarrels) : 0;
        public static float Available(EntityManager em, byte faction)
        {
            if(CampaignCitywideFuelScope.TryGet(em,out _,out _))return faction==1?CampaignCitywideFuelScope.Total(em):0;
            if(TryScope(em,out var scope))return em.Exists(scope) && em.HasComponent<BuildingResourceStorageComponent>(scope)
                ? Usable(em.GetComponentData<BuildingResourceStorageComponent>(scope),faction):0;
            using var query=em.CreateEntityQuery(ComponentType.ReadOnly<BuildingResourceStorageComponent>());
            using var chunks=query.ToArchetypeChunkArray(Allocator.Temp);
            var storageType=em.GetComponentTypeHandle<BuildingResourceStorageComponent>(true);
            float total=0;
            foreach(var chunk in chunks)
                foreach(var store in chunk.GetNativeArray(ref storageType))total+=Usable(store,faction);
            return total;
        }
        public static bool TryConsumeImmediate(EntityManager em, byte faction, int cost)
        {
            if(cost<0) return false;
            if(cost==0) return true;
            bool scoped=TryScope(em,out var scope);
            using var query=em.CreateEntityQuery(ComponentType.ReadWrite<BuildingResourceStorageComponent>());
            using var chunks=query.ToArchetypeChunkArray(Allocator.Temp);
            var entityType=em.GetEntityTypeHandle();var storageType=em.GetComponentTypeHandle<BuildingResourceStorageComponent>(true);
            using var plan=new NativeList<Contribution>(Allocator.Temp);
            foreach(var chunk in chunks)
            {
                var entities=chunk.GetNativeArray(entityType);var stores=chunk.GetNativeArray(ref storageType);
                for(int i=0;i<entities.Length;i++)if((!scoped || entities[i]==scope || faction==1 && CampaignCitywideFuelScope.IsStorage(em,entities[i])) && Usable(stores[i],faction)>0)plan.Add(new Contribution {Entity=entities[i],Copy=stores[i]});
            }
            var prepared=plan;
            // Deterministic source order; no writes during preparation.
            for(int i=1;i<plan.Length;i++)
            {
                var key=plan[i]; int j=i-1;
                while(j>=0 && (plan[j].Entity.Index>key.Entity.Index || plan[j].Entity.Index==key.Entity.Index && plan[j].Entity.Version>key.Entity.Version))
                { prepared[j+1]=plan[j]; j--; }
                prepared[j+1]=key;
            }
            float remaining=cost;
            for(int i=0;i<plan.Length;i++)
            {
                var contribution=plan[i];var copy=contribution.Copy;
                float amount=math.min(remaining,Usable(copy,faction));
                if(amount<=0) continue;
                if(!BuildingResourceStorageTransferSystemHelper.TryReserveSource(ref copy,(byte)ResourceKind.Fuel,amount) ||
                   !BuildingResourceStorageTransferSystemHelper.TryConsumeSourceReservation(ref copy,(byte)ResourceKind.Fuel,amount)) return false;
                contribution.Copy=copy;contribution.Amount=amount;prepared[i]=contribution;
                remaining-=amount;
                if(remaining<=0) break;
            }
            if(remaining>0) return false;
            foreach(var contribution in plan)if(contribution.Amount>0)em.SetComponentData(contribution.Entity,contribution.Copy);
            return true;
        }
        public static bool TryReserve(EntityManager em,Entity execution,byte faction,int cost,uint requestId)
        {
            if(cost<0 || !em.HasBuffer<SupportFuelReservationElement>(execution))return false;
            bool scoped=TryScope(em,out var scope);
            using var query=em.CreateEntityQuery(ComponentType.ReadOnly<BuildingResourceStorageComponent>());
            using var chunks=query.ToArchetypeChunkArray(Allocator.Temp);
            var entityType=em.GetEntityTypeHandle();var storageType=em.GetComponentTypeHandle<BuildingResourceStorageComponent>(true);
            using var plan=new NativeList<Contribution>(Allocator.Temp);
            foreach(var chunk in chunks)
            {
                var entities=chunk.GetNativeArray(entityType);var stores=chunk.GetNativeArray(ref storageType);
                for(int i=0;i<entities.Length;i++)if((!scoped || entities[i]==scope || faction==1 && CampaignCitywideFuelScope.IsStorage(em,entities[i])) && Usable(stores[i],faction)>0)
                    plan.Add(new Contribution {Entity=entities[i],Copy=stores[i]});
            }
            var prepared=plan;
            for(int i=1;i<plan.Length;i++)
            {
                var key=plan[i];int j=i-1;
                while(j>=0 && (plan[j].Entity.Index>key.Entity.Index || plan[j].Entity.Index==key.Entity.Index && plan[j].Entity.Version>key.Entity.Version))
                {prepared[j+1]=plan[j];j--;}
                prepared[j+1]=key;
            }
            float remaining=cost;
            for(int i=0;i<plan.Length && remaining>0;i++)
            {
                var p=plan[i];p.Amount=math.min(remaining,Usable(p.Copy,faction));
                if(!BuildingResourceStorageTransferSystemHelper.TryReserveSource(ref p.Copy,(byte)ResourceKind.Fuel,p.Amount))return false;
                remaining-=p.Amount;prepared[i]=p;
            }
            if(remaining>0)return false;
            var reservations=em.GetBuffer<SupportFuelReservationElement>(execution);reservations.EnsureCapacity(plan.Length);
            foreach(var p in plan)if(p.Amount>0)
            {
                em.SetComponentData(p.Entity,p.Copy);
                reservations.Add(new SupportFuelReservationElement {SourceStorage=p.Entity,Amount=p.Amount,RequestId=requestId});
            }
            return true;
        }
        public static bool TryConsumeReserved(EntityManager em,Entity execution,byte faction)
        {
            var reservations=em.GetBuffer<SupportFuelReservationElement>(execution);
            using var plan=new NativeList<Contribution>(Allocator.Temp);
            foreach(var r in reservations)
            {
                if(r.Status!=0 || !em.Exists(r.SourceStorage) || !em.HasComponent<BuildingResourceStorageComponent>(r.SourceStorage))return false;
                var copy=em.GetComponentData<BuildingResourceStorageComponent>(r.SourceStorage);
                if(copy.OwnerFactionId!=faction || !BuildingResourceStorageTransferSystemHelper.TryConsumeSourceReservation(ref copy,(byte)ResourceKind.Fuel,r.Amount))return false;
                plan.Add(new Contribution {Entity=r.SourceStorage,Copy=copy});
            }
            foreach(var p in plan)em.SetComponentData(p.Entity,p.Copy);
            for(int i=0;i<reservations.Length;i++){var r=reservations[i];r.Status=1;reservations[i]=r;}
            return true;
        }
        public static void ReleaseReserved(EntityManager em,Entity execution)
        {
            if(!em.Exists(execution)||!em.HasBuffer<SupportFuelReservationElement>(execution))return;
            var reservations=em.GetBuffer<SupportFuelReservationElement>(execution);
            for(int i=0;i<reservations.Length;i++)
            {
                var r=reservations[i];if(r.Status!=0)continue;
                if(em.Exists(r.SourceStorage)&&em.HasComponent<BuildingResourceStorageComponent>(r.SourceStorage))
                {
                    var copy=em.GetComponentData<BuildingResourceStorageComponent>(r.SourceStorage);
                    BuildingResourceStorageTransferSystemHelper.ReleaseSourceReservation(ref copy,(byte)ResourceKind.Fuel,r.Amount);
                    em.SetComponentData(r.SourceStorage,copy);
                }
                r.Status=2;reservations[i]=r;
            }
        }
        private static bool TryScope(EntityManager em,out Entity storage)
        {
            storage=Entity.Null;
            if(CampaignCitywideFuelScope.TryGet(em,out var citywideClinic,out _)){storage=citywideClinic;return true;}
            using var query=em.CreateEntityQuery(ComponentType.ReadOnly<SupportFuelScopeComponent>());
            int count=query.CalculateEntityCount();if(count==0)return false;
            if(count!=1)return true; // Ambiguous authored scope must never fall back to inherited stock.
            var scope=query.GetSingleton<SupportFuelScopeComponent>();storage=scope.Storage;return scope.Required!=0;
        }
    }
}
