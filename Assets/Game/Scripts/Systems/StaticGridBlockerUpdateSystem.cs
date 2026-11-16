using Unity.Collections;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Game.Components;

namespace Game.Runtime
{
    [BurstCompile]
    [UpdateAfter(typeof(DynamicBlockerInitSystem))]
    [UpdateBefore(typeof(UnitPathfindingSystem))]
    public partial struct StaticGridBlockerUpdateSystem : ISystem
    {
        private EntityQuery _gridQuery;

        private struct Storage : IStaticBlockerStorage
        {
            public NativeArray<int> Counts; public NativeBitArray Blocked; public NativeArray<byte> Friendly;
            public int CountLength=>Counts.Length;public int BlockedLength=>Blocked.Length;public int FriendlyPassLength=>Friendly.Length;
            public bool FriendlyPassCreated=>Friendly.IsCreated;
            public int ReadCount(int i)=>Counts[i];public void WriteCount(int i,int value)=>Counts[i]=value;
            public void WriteBlocked(int i,bool value)=>Blocked.Set(i,value);public void ClearBlocked()=>Blocked.Clear();public void WriteFriendlyPass(int i,byte value)=>Friendly[i]=value;
        }
        private static BlockerGrid KernelGrid(in GridConfig grid)=>new BlockerGrid {Width=grid.Width,Height=grid.Height,OriginX=grid.Origin.x,OriginY=grid.Origin.y,OriginZ=grid.Origin.z,CellSize=grid.CellSize};
        private static BlockerOwnerIdentity Identity(Entity grid,ulong epoch)=>new BlockerOwnerIdentity {Index=grid.Index,Version=grid.Version,StorageEpoch=epoch};
        private static BlockerHistory History(in StaticBlockerPrevBounds prev)=>new BlockerHistory {Rect=new BlockerRect {MinX=prev.Min.x,MinZ=prev.Min.y,MaxX=prev.Max.x,MaxZ=prev.Max.y},FriendlyFaction=prev.FriendlyPassFactionId,Owner=Identity(prev.AppliedGrid,prev.StorageEpoch)};
        private static StaticBlockerPrevBounds NativeHistory(in BlockerHistory value)=>new StaticBlockerPrevBounds {Min=new int2(value.Rect.MinX,value.Rect.MinZ),Max=new int2(value.Rect.MaxX,value.Rect.MaxZ),FriendlyPassFactionId=value.FriendlyFaction,AppliedGrid=new Entity {Index=value.Owner.Index,Version=value.Owner.Version},StorageEpoch=value.Owner.StorageEpoch};
        private static BlockerEpochState Epoch(in DynamicBlockerComponent owner)=>new BlockerEpochState {StorageEpoch=owner.StorageEpoch,AppliedStorageEpoch=owner.AppliedStorageEpoch,AppliedRevision=owner.AppliedRevision,WriteInProgress=owner.WriteInProgress,EpochExhausted=owner.EpochExhausted,AppliedGrid=KernelGrid(owner.AppliedGrid)};
        private static void WriteEpoch(ref DynamicBlockerComponent owner,in BlockerEpochState value)
        {
            owner.StorageEpoch=value.StorageEpoch;owner.AppliedStorageEpoch=value.AppliedStorageEpoch;owner.AppliedRevision=value.AppliedRevision;owner.WriteInProgress=value.WriteInProgress;owner.EpochExhausted=value.EpochExhausted;
            owner.AppliedGrid=new GridConfig {Width=value.AppliedGrid.Width,Height=value.AppliedGrid.Height,CellSize=value.AppliedGrid.CellSize,Origin=new float3(value.AppliedGrid.OriginX,value.AppliedGrid.OriginY,value.AppliedGrid.OriginZ)};
        }
        private static void ApplyOpeningIntent(EntityCommandBuffer ecb,Entity entity,BlockerOpeningChange change,in StaticGridBlockerSurfaceOpeningComponent opening)
        {
            var value=new StaticGridBlockerPrevSurfaceOpeningComponent {Value=opening};
            if(change==BlockerOpeningChange.Add)ecb.AddComponent(entity,value);
            else if(change==BlockerOpeningChange.Set)ecb.SetComponent(entity,value);
            else if(change==BlockerOpeningChange.Remove)ecb.RemoveComponent<StaticGridBlockerPrevSurfaceOpeningComponent>(entity);
        }

        private static byte GetFriendlyPassFactionId(ComponentLookup<FriendlyPassGridBlocker> friendlyPassLookup, Entity entity)
        {
            return friendlyPassLookup.HasComponent(entity)
                ? friendlyPassLookup[entity].AllowedFactionId
                : byte.MaxValue;
        }

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            _gridQuery = state.GetEntityQuery(ComponentType.ReadOnly<GridConfig>());
            state.RequireForUpdate(_gridQuery);
            state.RequireForUpdate<DynamicBlockerComponent>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            Entity gridEntity = _gridQuery.GetSingletonEntity();
            var grid = SystemAPI.GetComponent<GridConfig>(gridEntity);
            var blockerDataRw = SystemAPI.GetComponentRW<DynamicBlockerComponent>(gridEntity);

            var owner = blockerDataRw.ValueRO;
            var kernelGrid=KernelGrid(grid);var epoch=Epoch(owner);
            bool rebuild=StaticBlockerKernel.NeedsRebuild(kernelGrid,epoch);
            // History is meaningful only for this exact grid entity/version and storage epoch.
            // Scan the same existing/removal domains, without sorting or filtering foreign inputs.
            foreach (var prev in SystemAPI.Query<RefRO<StaticBlockerPrevBounds>>()
                         .WithAll<StaticGridBlocker, UnitGrid, GridBlockerSize>())
            {
                var previous = prev.ValueRO;
                if (!StaticBlockerKernel.SameOwner(Identity(previous.AppliedGrid,previous.StorageEpoch),Identity(gridEntity,owner.StorageEpoch))) rebuild = true;
            }
            foreach (var prev in SystemAPI.Query<RefRO<StaticBlockerPrevBounds>>().WithNone<StaticGridBlocker>())
            {
                var previous = prev.ValueRO;
                if (!StaticBlockerKernel.SameOwner(Identity(previous.AppliedGrid,previous.StorageEpoch),Identity(gridEntity,owner.StorageEpoch))) rebuild = true;
            }
            if (rebuild)
            {
                StaticBlockerKernel.Invalidate(ref epoch);WriteEpoch(ref owner,epoch);
                state.EntityManager.SetComponentData(gridEntity, owner);
                var resetStorage=new Storage {Counts=owner.Counts,Blocked=owner.Blocked,Friendly=owner.FriendlyPassFactionIds};
                StaticBlockerKernel.Clear(ref resetStorage);
            }
            // A failed/partial pass must remain unavailable to diagnostics; gameplay never waits on provenance.
            StaticBlockerKernel.BeginWrite(ref epoch);WriteEpoch(ref owner,epoch);
            state.EntityManager.SetComponentData(gridEntity, owner);
            var counts = blockerDataRw.ValueRW.Counts;
            var blocked = blockerDataRw.ValueRW.Blocked;
            var friendlyPassFactionIds = blockerDataRw.ValueRW.FriendlyPassFactionIds;
            var storage=new Storage {Counts=counts,Blocked=blocked,Friendly=friendlyPassFactionIds};
            var identity=Identity(gridEntity,owner.StorageEpoch);
            var friendlyPassLookup = SystemAPI.GetComponentLookup<FriendlyPassGridBlocker>(true);
            var authoredBuildingLookup = SystemAPI.GetComponentLookup<OperationMapBuildingComponent>(true);
            var buildingInfoLookup = SystemAPI.GetComponentLookup<RuntimeBuildingCombatInfo>(true);
            var openingLookup = SystemAPI.GetComponentLookup<StaticGridBlockerSurfaceOpeningComponent>(true);
            var prevOpeningLookup = SystemAPI.GetComponentLookup<StaticGridBlockerPrevSurfaceOpeningComponent>(true);

            using var ecb = new EntityCommandBuffer(Allocator.Temp);

            foreach (var (unitGrid, size, entity) in
                     SystemAPI.Query<RefRO<UnitGrid>, RefRO<GridBlockerSize>>()
                         .WithAll<StaticGridBlocker>()
                         .WithNone<StaticBlockerPrevBounds>()
                         .WithEntityAccess())
            {
                // Authored map buildings use UnitGrid for their selection centre.
                // Their authoritative foundation origin is stored separately.
                int2 origin = authoredBuildingLookup.HasComponent(entity) && buildingInfoLookup.TryGetComponent(entity, out var buildingInfo)
                    ? buildingInfo.OriginCell : unitGrid.ValueRO.Cell;
                var blockerSize = size.ValueRO.Size;
                var rect=StaticBlockerKernel.Bounds(origin.x,origin.y,blockerSize.x,blockerSize.y);
                byte faction=GetFriendlyPassFactionId(friendlyPassLookup,entity);
                bool hasOpening=openingLookup.HasComponent(entity);var opening=hasOpening?openingLookup[entity]:default;
                var result=StaticBlockerKernel.ApplyNew(kernelGrid,ref storage,rect,faction,hasOpening,opening.ToKernel(),identity,out var history);
                ApplyOpeningIntent(ecb,entity,result.PreviousOpeningChange,opening);
                ecb.AddComponent(entity,NativeHistory(history));
            }

            foreach (var (unitGrid, size, prev, entity) in
                     SystemAPI.Query<RefRO<UnitGrid>, RefRO<GridBlockerSize>, RefRW<StaticBlockerPrevBounds>>()
                         .WithAll<StaticGridBlocker>()
                         .WithEntityAccess())
            {
                int2 origin = authoredBuildingLookup.HasComponent(entity) && buildingInfoLookup.TryGetComponent(entity, out var buildingInfo)
                    ? buildingInfo.OriginCell : unitGrid.ValueRO.Cell;
                var blockerSize = size.ValueRO.Size;
                var rect=StaticBlockerKernel.Bounds(origin.x,origin.y,blockerSize.x,blockerSize.y);
                byte faction=GetFriendlyPassFactionId(friendlyPassLookup,entity);
                bool hasOpening=openingLookup.HasComponent(entity),hadOpening=prevOpeningLookup.HasComponent(entity);
                var opening=hasOpening?openingLookup[entity]:default;var oldOpening=hadOpening?prevOpeningLookup[entity].Value:default;
                var history=History(prev.ValueRO);
                var result=StaticBlockerKernel.ApplyExisting(kernelGrid,ref storage,rebuild,rect,faction,hasOpening,opening.ToKernel(),hadOpening,oldOpening.ToKernel(),identity,ref history);
                if(!result.Applied)continue;
                ApplyOpeningIntent(ecb,entity,result.PreviousOpeningChange,opening);
                prev.ValueRW=NativeHistory(history);
            }

            foreach (var (prev, entity) in
                     SystemAPI.Query<RefRO<StaticBlockerPrevBounds>>()
                         .WithNone<StaticGridBlocker>()
                         .WithEntityAccess())
            {
                bool hadOpening=prevOpeningLookup.HasComponent(entity);var oldOpening=hadOpening?prevOpeningLookup[entity].Value:default;
                var history=History(prev.ValueRO);
                var result=StaticBlockerKernel.ApplyRemoved(kernelGrid,ref storage,rebuild,history,hadOpening,oldOpening.ToKernel());
                ApplyOpeningIntent(ecb,entity,result.PreviousOpeningChange,default);
                ecb.RemoveComponent<StaticBlockerPrevBounds>(entity);
            }

            ecb.Playback(state.EntityManager);
            // Reacquire after structural playback; only successful genuine delta/replay work commits.
            owner = state.EntityManager.GetComponentData<DynamicBlockerComponent>(gridEntity);
            epoch=Epoch(owner);StaticBlockerKernel.Commit(kernelGrid,ref epoch);WriteEpoch(ref owner,epoch);
            state.EntityManager.SetComponentData(gridEntity, owner);
        }
    }
}
