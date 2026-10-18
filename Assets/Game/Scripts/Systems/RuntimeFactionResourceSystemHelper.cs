using Game.Components;
using Unity.Entities;
using UnityEngine;

namespace Game.Runtime
{
    internal sealed class RuntimeFactionResourceSystemHelper
    {
        private EntityManager _entityManager;
        private Entity _controlledFactionResourceEntity;
        private int _pendingInitialDollars;
        private bool _isConfigured;

        public int VisibleConstructionMaterialsCost(int credits, int materials) =>
            TryGetControlledFactionEconomy(out var economy) &&
            Game.Tactical.Contracts.MissionConstructionCostPolicy.TryResolve(economy.MaterialsOnlyConstruction,
                credits, materials, out _, out int cost) ? cost : int.MaxValue;

        public int CurrentDollars => TryGetControlledFactionEconomy(out var economy) ? economy.Money : 0;

        public int CurrentMaterials => TryGetControlledFactionResources(out _, out FactionTacticalMaterialsComponent materials)
            ? materials.Current : 0;

        public void SetInitialDollars(int dollars)
        {
            _pendingInitialDollars = Mathf.Max(0, dollars);
            if (!TryGetControlledFactionEconomy(out var economy))
                return;

            economy.MaterialsOnlyConstruction = 0;
            economy.Money = _pendingInitialDollars;
            _entityManager.SetComponentData(_controlledFactionResourceEntity, economy);
        }

        public void Configure(EntityManager entityManager)
        {
            _entityManager = entityManager;
            _controlledFactionResourceEntity = Entity.Null;
            _isConfigured = true;
            EnsureControlledFactionResources();
        }

        public void AddDollars(int amount)
        {
            amount = Mathf.Max(0, amount);
            if (amount == 0 || !TryGetControlledFactionEconomy(out FactionEconomy economy))
                return;

            economy.Money = economy.Money >= int.MaxValue - amount ? int.MaxValue : economy.Money + amount;
            _entityManager.SetComponentData(_controlledFactionResourceEntity, economy);
        }

        public bool TrySpendDollars(int amount) => amount <= 0 ||
            TrySpendConstructionResources(amount, 0) == FactionConstructionResourceMutationResult.Applied;

        public bool TrySpendMaterials(int amount) => amount <= 0 ||
            TrySpendConstructionResources(0, amount) == FactionConstructionResourceMutationResult.Applied;

        public void RefundMaterials(int amount)
        {
            if (amount > 0)
                TryRestoreConstructionResources(0, amount);
        }

        internal SkirmishConstructionLedger.Point CaptureConstructionLedger(object owner)
        {
            var point=SkirmishConstructionLedger.Begin(_entityManager,_controlledFactionResourceEntity);
            if(point!=null)point.TransactionOwner=owner;
            return point;
        }

        public FactionConstructionResourceMutationResult TrySpendConstructionResources(
            int creditsCost,
            int materialsCost)
        {
            if (!TryGetControlledFactionResources(
                    out FactionEconomy economy,
                    out FactionTacticalMaterialsComponent materials))
                return FactionConstructionResourceMutationResult.InvalidState;

            FactionConstructionResourceMutationResult result =
                FactionConstructionResourceUtilitySystemHelper.TrySpend(
                    ref economy,
                    ref materials,
                    creditsCost,
                    materialsCost);
            if (result != FactionConstructionResourceMutationResult.Applied)
                return result;

            _entityManager.SetComponentData(_controlledFactionResourceEntity, economy);
            if (materialsCost > 0)
                _entityManager.SetComponentData(_controlledFactionResourceEntity, materials);
            return result;
        }

        public FactionConstructionResourceMutationResult EvaluateConstructionResources(
            int creditsCost,
            int materialsCost)
        {
            return TryGetControlledFactionResources(
                out FactionEconomy economy,
                out FactionTacticalMaterialsComponent materials)
                ? FactionConstructionResourceUtilitySystemHelper.Evaluate(
                    economy,
                    materials,
                    creditsCost,
                    materialsCost)
                : FactionConstructionResourceMutationResult.InvalidState;
        }

        public FactionConstructionResourceMutationResult TryRestoreConstructionResources(
            int creditsCost,
            int materialsCost)
        {
            if (!TryGetControlledFactionResources(
                    out FactionEconomy economy,
                    out FactionTacticalMaterialsComponent materials))
                return FactionConstructionResourceMutationResult.InvalidState;

            FactionConstructionResourceMutationResult result =
                FactionConstructionResourceUtilitySystemHelper.TryRollback(
                    ref economy,
                    ref materials,
                    creditsCost,
                    materialsCost);
            if (result != FactionConstructionResourceMutationResult.Applied)
                return result;

            _entityManager.SetComponentData(_controlledFactionResourceEntity, economy);
            if (materialsCost > 0)
                _entityManager.SetComponentData(_controlledFactionResourceEntity, materials);
            return result;
        }

        public bool TryGetFactionResourceEntity(byte factionId, out Entity entity)
        {
            entity = Entity.Null;
            if (!TryGetControlledFactionEconomy(out FactionEconomy economy) ||
                economy.FactionId != factionId)
                return false;

            entity = _controlledFactionResourceEntity;
            return _entityManager.HasComponent<FactionTacticalMaterialsComponent>(entity);
        }

        public CitizenResourceCompositionSystemHelper.Context CreateCitizenResourceContext() =>
            new(() => CurrentDollars, SetDollars);

        private void SetDollars(int value)
        {
            if (!TryGetControlledFactionEconomy(out FactionEconomy economy))
                return;

            economy.Money = Mathf.Max(0, value);
            _entityManager.SetComponentData(_controlledFactionResourceEntity, economy);
        }

        private void EnsureControlledFactionResources()
        {
            if (!IsEntityManagerAvailable())
                return;

            Entity resolvedEntity = Entity.Null;
            FactionEconomy resolvedEconomy = default;
            using (EntityQuery query = _entityManager.CreateEntityQuery(ComponentType.ReadOnly<FactionEconomy>()))
            using (Unity.Collections.NativeArray<ArchetypeChunk> chunks =
                   query.ToArchetypeChunkArray(Unity.Collections.Allocator.Temp))
            {
                EntityTypeHandle entityType = _entityManager.GetEntityTypeHandle();
                ComponentTypeHandle<FactionEconomy> economyType =
                    _entityManager.GetComponentTypeHandle<FactionEconomy>(true);
                for (int chunkIndex = 0; chunkIndex < chunks.Length && resolvedEntity == Entity.Null; chunkIndex++)
                {
                    ArchetypeChunk chunk = chunks[chunkIndex];
                    Unity.Collections.NativeArray<Entity> entities = chunk.GetNativeArray(entityType);
                    Unity.Collections.NativeArray<FactionEconomy> economies = chunk.GetNativeArray(ref economyType);
                    for (int i = 0; i < entities.Length; i++)
                    {
                        if (!FactionIdentity.IsPlayerControlled(economies[i].FactionId))
                            continue;

                        resolvedEntity = entities[i];
                        resolvedEconomy = economies[i];
                        break;
                    }
                }
            }

            if (resolvedEntity != Entity.Null)
            {
                _controlledFactionResourceEntity = resolvedEntity;
                resolvedEconomy.Money = _pendingInitialDollars;
                _entityManager.SetComponentData(resolvedEntity, resolvedEconomy);
                EnsureCompanionComponents(resolvedEntity);
                return;
            }

            _controlledFactionResourceEntity = _entityManager.CreateEntity(
                typeof(FactionEconomy),
                typeof(FactionEconomyPolicy),
                typeof(FactionTacticalMaterialsComponent));
            _entityManager.SetComponentData(_controlledFactionResourceEntity, new FactionEconomy
            {
                FactionId = FactionIdentity.PlayerFactionId,
                Money = _pendingInitialDollars
            });
            _entityManager.SetComponentData(_controlledFactionResourceEntity, new FactionEconomyPolicy
            {
                Enabled = 0,
                IncomeMultiplier = 1f
            });
            _entityManager.SetComponentData(_controlledFactionResourceEntity, new FactionTacticalMaterialsComponent
            {
                FactionId = FactionIdentity.PlayerFactionId
            });
        }

        private void EnsureCompanionComponents(Entity entity)
        {
            if (!_entityManager.HasComponent<FactionEconomyPolicy>(entity))
            {
                _entityManager.AddComponentData(entity, new FactionEconomyPolicy
                {
                    Enabled = 0,
                    IncomeMultiplier = 1f
                });
            }

            if (!_entityManager.HasComponent<FactionTacticalMaterialsComponent>(entity))
            {
                _entityManager.AddComponentData(entity, new FactionTacticalMaterialsComponent
                {
                    FactionId = FactionIdentity.PlayerFactionId
                });
            }
        }

        private bool TryGetControlledFactionEconomy(out FactionEconomy economy)
        {
            economy = default;
            if (!IsEntityManagerAvailable())
                return false;
            if (_controlledFactionResourceEntity == Entity.Null ||
                !_entityManager.Exists(_controlledFactionResourceEntity) ||
                !_entityManager.HasComponent<FactionEconomy>(_controlledFactionResourceEntity))
            {
                EnsureControlledFactionResources();
            }

            if (_controlledFactionResourceEntity == Entity.Null ||
                !_entityManager.Exists(_controlledFactionResourceEntity) ||
                !_entityManager.HasComponent<FactionEconomy>(_controlledFactionResourceEntity))
                return false;

            economy = _entityManager.GetComponentData<FactionEconomy>(_controlledFactionResourceEntity);
            return FactionIdentity.IsPlayerControlled(economy.FactionId);
        }

        private bool TryGetControlledFactionResources(
            out FactionEconomy economy,
            out FactionTacticalMaterialsComponent materials)
        {
            materials = default;
            if (!TryGetControlledFactionEconomy(out economy) ||
                !_entityManager.HasComponent<FactionTacticalMaterialsComponent>(_controlledFactionResourceEntity))
                return false;

            materials = _entityManager.GetComponentData<FactionTacticalMaterialsComponent>(
                _controlledFactionResourceEntity);
            return materials.FactionId == economy.FactionId;
        }

        private bool IsEntityManagerAvailable() => _isConfigured && _entityManager.WorldUnmanaged.IsCreated;
    }
    // Diagnostic-only copies of actual committed construction origins.
    public static class SkirmishConstructionLedger
    {
        public sealed class Point
        {
            public EntityManager Manager;
            public Entity BankEntity,Session;
            public object TransactionOwner;
            public FactionEconomy Economy;
            public FactionTacticalMaterialsComponent Bank;
            public bool HasEnemyIntent,HasRequest;
            public SkirmishEnemyConstructionIntentV1Component EnemyIntent;
            public BuildingRuntimeSpawnRequest Request;
        }
        public sealed class Observation
        {
            public Point Before,After;
            public object TransactionOwner;
            public string Operation,PrefabName;
            public int TransactionId,CreditsQuoted,MaterialsQuoted,CommittedCount,ExpectedCount;
            public Unity.Mathematics.int2 Origin;
            public bool Rotated,HasActorOwner;
            public Entity Actor;
            public SkirmishAttemptOwnedComponent ActorOwner;
        }
        public static System.Action<Observation> Observer;
        public static void OpenOwned(System.Action<Observation> sink)
        {if(sink==null||Observer!=null)throw new System.InvalidOperationException("Unobserved construction sink required");Observer=sink;}
        public static bool CloseOwned(System.Action<Observation> sink)
        {bool owned=sink!=null&&Observer==sink;if(owned)Observer=null;return owned;}
        public static long Faults {get;private set;}
        private static Point Read(EntityManager em,Entity bank,Entity session)
        {
            if(!em.Exists(bank)||!em.HasComponent<FactionEconomy>(bank)||!em.HasComponent<FactionTacticalMaterialsComponent>(bank))
                throw new System.InvalidOperationException("Actual construction bank required");
            var economy=em.GetComponentData<FactionEconomy>(bank);int matches=0;
            using(var q=em.CreateEntityQuery(typeof(FactionEconomy),typeof(FactionTacticalMaterialsComponent)))
            using(var entities=q.ToEntityArray(Unity.Collections.Allocator.Temp))
                foreach(var entity in entities)if(em.GetComponentData<FactionEconomy>(entity).FactionId==economy.FactionId)matches++;
            if(matches!=1)throw new System.InvalidOperationException("Unique actual construction bank required");
            if(session==Entity.Null)
            {
                using var q=em.CreateEntityQuery(typeof(SkirmishExpandedSessionComponent));
                int count=q.CalculateEntityCount();
                if(count>1)throw new System.InvalidOperationException("Ambiguous construction session");
                if(count==1)session=q.GetSingletonEntity();
            }
            var p=new Point {Manager=em,BankEntity=bank,Session=session,Economy=economy,Bank=em.GetComponentData<FactionTacticalMaterialsComponent>(bank)};
            if(economy.FactionId==2 && session!=Entity.Null && em.HasComponent<SkirmishEnemyConstructionIntentV1Component>(session))
            {
                p.HasEnemyIntent=true;p.EnemyIntent=em.GetComponentData<SkirmishEnemyConstructionIntentV1Component>(session);
                using var q=em.CreateEntityQuery(typeof(BuildingRuntimeStateTag),typeof(BuildingRuntimeSpawnRequest));
                if(q.CalculateEntityCount()>1)throw new System.InvalidOperationException("Ambiguous construction boundary");
                if(q.CalculateEntityCount()==1)
                    foreach(var r in em.GetBuffer<BuildingRuntimeSpawnRequest>(q.GetSingletonEntity()))
                        if(r.PlanEntity==session&&r.RequestId==p.EnemyIntent.RequestId&&r.RequestKind==BuildingRuntimeSpawnRequest.KindSkirmishPaidConstruction)
                        {if(p.HasRequest)throw new System.InvalidOperationException("Duplicate construction request");p.HasRequest=true;p.Request=r;}
            }
            return p;
        }
        internal static Point Begin(EntityManager em,Entity bank,Entity session=default)
        {
            if(Observer==null)return null;
            try{return Read(em,bank,session);}
            catch(System.Exception error)when(!(error is System.OutOfMemoryException)){Faults++;return null;}
        }
        internal static void UnsupportedPlacement()
        {if(Observer!=null)Faults++;}
        internal static void End(Point before,string operation,int transactionId=0,int credits=0,int materials=0,
            string prefabName=null,Unity.Mathematics.int2 origin=default,bool rotated=false,int committed=0,int expected=0,Entity actor=default)
        {
            if(before==null||Observer==null)return;
            try
            {
                var em=before.Manager;
                var row=new Observation {Before=before,After=Read(em,before.BankEntity,before.Session),Operation=operation,TransactionOwner=before.TransactionOwner,
                    TransactionId=transactionId,CreditsQuoted=credits,MaterialsQuoted=materials,PrefabName=prefabName,
                    Origin=origin,Rotated=rotated,CommittedCount=committed,ExpectedCount=expected,Actor=actor};
                if(actor!=Entity.Null && em.Exists(actor)&&em.HasComponent<SkirmishAttemptOwnedComponent>(actor))
                {row.HasActorOwner=true;row.ActorOwner=em.GetComponentData<SkirmishAttemptOwnedComponent>(actor);}
                Observer(row);
            }
            catch(System.Exception error)when(!(error is System.OutOfMemoryException)){Faults++;}
        }
    }

}
