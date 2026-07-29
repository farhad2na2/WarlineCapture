using Game.Components;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;
using Unity.Transforms;

namespace Game.Rendering
{
    internal enum OperationMapRenderSlotApplyFailure : byte
    {
        None = 0,
        InvalidSlot = 1,
        InvalidCommand = 2,
        InvalidPlacement = 3,
        InvalidPart = 4,
        InvalidRenderData = 5
    }

    [BurstCompile]
    internal struct OperationMapRenderSlotApplyJob : IJobChunk
    {
        [ReadOnly] internal BlobAssetReference<OperationMapRenderDatabaseBlob> Database;
        [ReadOnly] internal NativeArray<OperationMapRenderSlotCommandComponent>
            SlotCommands;

        internal ComponentTypeHandle<OperationMapRenderProxySlotComponent>
            ProxySlotType;
        internal ComponentTypeHandle<LocalToWorld> LocalToWorldType;
        internal ComponentTypeHandle<RenderBounds> RenderBoundsType;
        internal ComponentTypeHandle<MaterialMeshInfo> MaterialMeshInfoType;
        internal ComponentTypeHandle<URPMaterialPropertyBaseColor> BaseColorType;
        internal NativeArray<OperationMapRenderSlotApplyFailure> SlotFailures;

        [BurstCompile]
        public void Execute(
            in ArchetypeChunk chunk,
            int unfilteredChunkIndex,
            bool useEnabledMask,
            in v128 chunkEnabledMask)
        {
            NativeArray<OperationMapRenderProxySlotComponent> proxySlots =
                chunk.GetNativeArray(ref ProxySlotType);
            NativeArray<LocalToWorld> localToWorlds =
                chunk.GetNativeArray(ref LocalToWorldType);
            NativeArray<RenderBounds> renderBounds =
                chunk.GetNativeArray(ref RenderBoundsType);
            NativeArray<MaterialMeshInfo> materialMeshInfos =
                chunk.GetNativeArray(ref MaterialMeshInfoType);
            NativeArray<URPMaterialPropertyBaseColor> baseColors =
                chunk.GetNativeArray(ref BaseColorType);

            for (int index = 0; index < chunk.Count; index++)
            {
                OperationMapRenderProxySlotComponent proxySlot =
                    proxySlots[index];
                int slotIndex = proxySlot.SlotIndex;
                if (slotIndex < 0 ||
                    slotIndex >= SlotCommands.Length ||
                    slotIndex >= SlotFailures.Length)
                {
                    continue;
                }

                OperationMapRenderSlotCommandComponent command =
                    SlotCommands[slotIndex];
                if (command.AssignmentGeneration ==
                    proxySlot.AssignmentGeneration)
                    continue;
                OperationMapRenderSlotApplyFailure failure =
                    ValidateCommand(proxySlot, command);
                if (failure != OperationMapRenderSlotApplyFailure.None)
                {
                    SlotFailures[slotIndex] = failure;
                    continue;
                }

                if (command.Assigned == 0)
                {
                    localToWorlds[index] = new LocalToWorld
                        { Value = float4x4.identity };
                    renderBounds[index] = new RenderBounds
                    {
                        Value = new AABB
                        {
                            Center = float3.zero,
                            Extents = float3.zero
                        }
                    };
                    materialMeshInfos[index] =
                        MaterialMeshInfo.FromRenderMeshArrayIndices(0, 0, 0);
                    baseColors[index] = new URPMaterialPropertyBaseColor
                        { Value = new float4(1f) };
                    proxySlot.PlacementIndex = -1;
                    proxySlot.PartIndex = -1;
                    proxySlot.AssignmentGeneration =
                        command.AssignmentGeneration;
                    proxySlots[index] = proxySlot;
                    chunk.SetComponentEnabled(
                        ref MaterialMeshInfoType, index, false);
                    continue;
                }

                ref OperationMapRenderDatabaseBlob blob = ref Database.Value;
                OperationMapRenderPlacementBlob placement =
                    blob.Placements[command.PlacementIndex];
                OperationMapRenderPrototypePartBlob part =
                    blob.Parts[command.PartIndex];
                localToWorlds[index] = new LocalToWorld
                {
                    Value = math.mul(
                        placement.WorldMatrix,
                        part.LocalToPlacement)
                };
                renderBounds[index] = new RenderBounds
                {
                    Value = new AABB
                    {
                        Center = part.LocalBounds.Center,
                        Extents = part.LocalBounds.Extents
                    }
                };
                materialMeshInfos[index] =
                    MaterialMeshInfo.FromRenderMeshArrayIndices(
                        part.MaterialArrayIndex,
                        part.MeshArrayIndex,
                        (ushort)part.SubMeshIndex);
                baseColors[index] = new URPMaterialPropertyBaseColor
                    { Value = part.LinearBaseColor };
                proxySlot.PlacementIndex = command.PlacementIndex;
                proxySlot.PartIndex = command.PartIndex;
                proxySlot.AssignmentGeneration = command.AssignmentGeneration;
                proxySlots[index] = proxySlot;
                chunk.SetComponentEnabled(
                    ref MaterialMeshInfoType, index, true);
            }
        }

        private OperationMapRenderSlotApplyFailure ValidateCommand(
            OperationMapRenderProxySlotComponent slot,
            OperationMapRenderSlotCommandComponent command)
        {
            if (!Database.IsCreated ||
                command.SlotIndex != slot.SlotIndex ||
                command.AssignmentGeneration <= 0)
            {
                return OperationMapRenderSlotApplyFailure.InvalidCommand;
            }
            ref OperationMapRenderDatabaseBlob blob = ref Database.Value;
            if (slot.PoolBucketIndex < 0 ||
                slot.PoolBucketIndex >= blob.PoolBuckets.Length)
                return OperationMapRenderSlotApplyFailure.InvalidSlot;
            OperationMapRenderPoolBucketBlob bucket =
                blob.PoolBuckets[slot.PoolBucketIndex];
            if (slot.SlotIndex < bucket.FirstSlot ||
                slot.SlotIndex >= bucket.FirstSlot + bucket.Capacity)
                return OperationMapRenderSlotApplyFailure.InvalidSlot;

            if (command.Assigned == 0)
            {
                return command.LogicalRowIndex == -1 &&
                       command.PlacementIndex == -1 &&
                       command.PartIndex == -1 &&
                       command.PoolBucketIndex == -1
                    ? OperationMapRenderSlotApplyFailure.None
                    : OperationMapRenderSlotApplyFailure.InvalidCommand;
            }
            if (command.Assigned != 1 ||
                command.LogicalRowIndex < 0 ||
                command.PoolBucketIndex != slot.PoolBucketIndex ||
                command.PlacementIndex < 0 ||
                command.PlacementIndex >= blob.Placements.Length)
            {
                return OperationMapRenderSlotApplyFailure.InvalidPlacement;
            }

            OperationMapRenderPlacementBlob placement =
                blob.Placements[command.PlacementIndex];
            if (placement.PrototypeIndex < 0 ||
                placement.PrototypeIndex >= blob.Prototypes.Length)
                return OperationMapRenderSlotApplyFailure.InvalidPlacement;
            OperationMapRenderPrototypeBlob prototype =
                blob.Prototypes[placement.PrototypeIndex];
            if (command.PartIndex < prototype.FirstPart ||
                command.PartIndex >= prototype.FirstPart + prototype.PartCount ||
                command.PartIndex < 0 ||
                command.PartIndex >= blob.Parts.Length)
            {
                return OperationMapRenderSlotApplyFailure.InvalidPart;
            }

            OperationMapRenderPrototypePartBlob part =
                blob.Parts[command.PartIndex];
            if (part.PoolBucketIndex != command.PoolBucketIndex ||
                part.MeshArrayIndex < 0 ||
                part.MaterialArrayIndex < 0 ||
                part.SubMeshIndex < 0 ||
                part.SubMeshIndex > ushort.MaxValue ||
                !IsFinite(placement.WorldMatrix) ||
                !IsFinite(part.LocalToPlacement) ||
                !math.all(math.isfinite(part.LocalBounds.Center)) ||
                !math.all(math.isfinite(part.LocalBounds.Extents)) ||
                math.any(part.LocalBounds.Extents < 0f) ||
                !math.all(math.isfinite(part.LinearBaseColor)))
            {
                return OperationMapRenderSlotApplyFailure.InvalidRenderData;
            }
            return OperationMapRenderSlotApplyFailure.None;
        }

        private static bool IsFinite(float4x4 value) =>
            math.all(math.isfinite(value.c0)) &&
            math.all(math.isfinite(value.c1)) &&
            math.all(math.isfinite(value.c2)) &&
            math.all(math.isfinite(value.c3));
    }
}
