using System;
using System.Collections.Generic;
using Game.Components;
using Game.Configs;
using Unity.Collections;
using Unity.Entities;
using Unity.Scenes;

namespace Game.Composition
{
    internal static class OperationMapEntityPresentationReadinessUtility
    {
        internal static bool TryValidate(
            EntityManager entityManager,
            Entity sceneEntity,
            string expectedOperationMapId,
            out string error) =>
            TryValidate(
                entityManager,
                sceneEntity,
                expectedOperationMapId,
                OperationMapRenderResidencyMode.ResidentEntities,
                out error);

        internal static bool TryValidate(
            EntityManager entityManager,
            Entity sceneEntity,
            string expectedOperationMapId,
            OperationMapRenderResidencyMode renderResidencyMode,
            out string error)
        {
            if (!entityManager.Exists(sceneEntity) ||
                !entityManager.HasBuffer<ResolvedSectionEntity>(sceneEntity))
            {
                error = "Packed EntityScene metadata is not resolved.";
                return false;
            }

            DynamicBuffer<ResolvedSectionEntity> sections =
                entityManager.GetBuffer<ResolvedSectionEntity>(sceneEntity);
            if (sections.Length == 0)
            {
                error = "Packed EntityScene contains no resolved sections.";
                return false;
            }

            using EntityQuery contractQuery = entityManager.CreateEntityQuery(
                ComponentType.ReadOnly<OperationMapEntityPresentationReadinessContract>(),
                ComponentType.ReadOnly<SceneTag>());
            using EntityQuery rootQuery = entityManager.CreateEntityQuery(
                ComponentType.ReadOnly<OperationMapEntityPresentationRoot>(),
                ComponentType.ReadOnly<SceneTag>());
            using EntityQuery identityQuery = entityManager.CreateEntityQuery(
                ComponentType.ReadOnly<OperationMapEntityPresentationIdentity>(),
                ComponentType.ReadOnly<SceneTag>());
            using EntityQuery generatedIdentityQuery = entityManager.CreateEntityQuery(
                ComponentType.ReadOnly<DenseCityPresentationIdentity>(),
                ComponentType.ReadOnly<SceneTag>());
            using EntityQuery buildingQuery = entityManager.CreateEntityQuery(
                ComponentType.ReadOnly<OperationMapBuildingPresentation>(),
                ComponentType.ReadOnly<SceneTag>());
            using EntityQuery virtualizedBuildingQuery =
                entityManager.CreateEntityQuery(
                    ComponentType.ReadOnly<
                        OperationMapVirtualizedBuildingPresentationComponent>(),
                    ComponentType.ReadOnly<SceneTag>());
            using EntityQuery overlappingBuildingQuery =
                entityManager.CreateEntityQuery(
                    ComponentType.ReadOnly<OperationMapBuildingPresentation>(),
                    ComponentType.ReadOnly<
                        OperationMapVirtualizedBuildingPresentationComponent>(),
                    ComponentType.ReadOnly<SceneTag>());
            using EntityQuery vehicleQuery = entityManager.CreateEntityQuery(
                ComponentType.ReadOnly<OperationMapAuthoredVehiclePresentation>(),
                ComponentType.ReadOnly<SceneTag>());
            using EntityQuery databaseQuery = entityManager.CreateEntityQuery(
                ComponentType.ReadOnly<OperationMapRenderDatabaseComponent>(),
                ComponentType.ReadOnly<SceneTag>());
            using EntityQuery packedReadinessQuery =
                entityManager.CreateEntityQuery(
                    ComponentType.ReadOnly<
                        OperationMapRenderPackedReadinessComponent>(),
                    ComponentType.ReadOnly<SceneTag>());
            using EntityQuery slotQuery = entityManager.CreateEntityQuery(
                ComponentType.ReadOnly<OperationMapRenderProxySlotComponent>(),
                ComponentType.ReadOnly<SceneTag>());
            using EntityQuery eligibleSourceQuery =
                entityManager.CreateEntityQuery(
                    ComponentType.ReadOnly<
                        OperationMapRenderEligibleSourceComponent>(),
                    ComponentType.ReadOnly<SceneTag>());
            var contracts = new NativeList<OperationMapEntityPresentationReadinessContract>(
                Allocator.Temp);
            var roots = new NativeList<OperationMapEntityPresentationRoot>(Allocator.Temp);
            var identities = new NativeList<OperationMapEntityPresentationIdentity>(
                Allocator.Temp);
            var generatedIdentities = new NativeList<DenseCityPresentationIdentity>(
                Allocator.Temp);
            var databaseEntities = new NativeList<Entity>(Allocator.Temp);
            var databases = new NativeList<OperationMapRenderDatabaseComponent>(
                Allocator.Temp);
            var packedReadinessEntities =
                new NativeList<Entity>(Allocator.Temp);
            var packedReadinessContracts =
                new NativeList<OperationMapRenderPackedReadinessComponent>(
                    Allocator.Temp);
            var slots = new NativeList<OperationMapRenderProxySlotComponent>(
                Allocator.Temp);
            var sourceIds = new NativeParallelHashSet<FixedString128Bytes>(
                16384,
                Allocator.Temp);
            var generatedIds = new NativeParallelHashSet<FixedString128Bytes>(
                65536,
                Allocator.Temp);
            int buildingCount = 0;
            int virtualizedBuildingCount = 0;
            int overlappingBuildingCount = 0;
            int vehicleCount = 0;
            int eligibleSourceSurvivorCount = 0;
            try
            {
                for (int sectionIndex = 0; sectionIndex < sections.Length; sectionIndex++)
                {
                    var sceneTag = new SceneTag
                    {
                        SceneEntity = sections[sectionIndex].SectionEntity
                    };
                    contractQuery.SetSharedComponentFilter(sceneTag);
                    rootQuery.SetSharedComponentFilter(sceneTag);
                    identityQuery.SetSharedComponentFilter(sceneTag);
                    generatedIdentityQuery.SetSharedComponentFilter(sceneTag);
                    buildingQuery.SetSharedComponentFilter(sceneTag);
                    virtualizedBuildingQuery.SetSharedComponentFilter(sceneTag);
                    overlappingBuildingQuery.SetSharedComponentFilter(sceneTag);
                    vehicleQuery.SetSharedComponentFilter(sceneTag);
                    databaseQuery.SetSharedComponentFilter(sceneTag);
                    packedReadinessQuery.SetSharedComponentFilter(sceneTag);
                    slotQuery.SetSharedComponentFilter(sceneTag);
                    eligibleSourceQuery.SetSharedComponentFilter(sceneTag);

                    Append(contractQuery, ref contracts);
                    Append(rootQuery, ref roots);
                    Append(identityQuery, ref identities);
                    Append(generatedIdentityQuery, ref generatedIdentities);
                    Append(databaseQuery, ref databaseEntities);
                    Append(databaseQuery, ref databases);
                    Append(packedReadinessQuery, ref packedReadinessEntities);
                    Append(packedReadinessQuery, ref packedReadinessContracts);
                    Append(slotQuery, ref slots);
                    buildingCount += buildingQuery.CalculateEntityCount();
                    virtualizedBuildingCount +=
                        virtualizedBuildingQuery.CalculateEntityCount();
                    overlappingBuildingCount +=
                        overlappingBuildingQuery.CalculateEntityCount();
                    vehicleCount += vehicleQuery.CalculateEntityCount();
                    eligibleSourceSurvivorCount +=
                        eligibleSourceQuery.CalculateEntityCount();
                }

                if (contracts.Length != 1)
                {
                    error =
                        $"Packed EntityScene requires exactly one readiness contract; found {contracts.Length}.";
                    return false;
                }

                OperationMapEntityPresentationReadinessContract contract = contracts[0];
                if (!string.Equals(
                        contract.OperationMapId.ToString(),
                        expectedOperationMapId,
                        StringComparison.Ordinal))
                {
                    error = "Packed EntityScene readiness contract belongs to a different operation map.";
                    return false;
                }
                if (contract.RequiresStaticPresentationPreload != 0)
                {
                    error = "Packed EntityScene readiness contract requires a prohibited static preload.";
                    return false;
                }
                if (roots.Length != contract.ExpectedPresentationRootCount)
                {
                    error =
                        $"Packed EntityScene presentation-root count is {roots.Length}/" +
                        $"{contract.ExpectedPresentationRootCount}.";
                    return false;
                }

                if (!TryValidateRenderResidency(
                        entityManager,
                        sections,
                        expectedOperationMapId,
                        renderResidencyMode,
                        databaseEntities,
                        databases,
                        packedReadinessEntities,
                        packedReadinessContracts,
                        slots,
                        eligibleSourceSurvivorCount,
                        virtualizedBuildingCount,
                        overlappingBuildingCount,
                        out OperationMapRenderPackedReadinessComponent
                            virtualizationContract,
                        out error))
                {
                    return false;
                }

                int gameplayBuildings = 0;
                int gameplayVehicles = 0;
                int renderOnly = 0;
                byte rootRoleMask = 0;
                for (int i = 0; i < roots.Length; i++)
                {
                    OperationMapEntityPresentationRoot root = roots[i];
                    if (!root.OperationMapId.Equals(contract.OperationMapId) ||
                        !root.MigrationRecordSetHash.Equals(contract.MigrationRecordSetHash))
                    {
                        error = "Packed EntityScene presentation roots do not match the readiness contract.";
                        return false;
                    }
                    if (root.Role < 1 || root.Role > 3)
                    {
                        error = $"Packed EntityScene contains unknown presentation-root role {root.Role}.";
                        return false;
                    }
                    byte roleBit = (byte)(1 << root.Role);
                    if ((rootRoleMask & roleBit) != 0)
                    {
                        error = $"Packed EntityScene contains duplicate presentation-root role {root.Role}.";
                        return false;
                    }
                    rootRoleMask |= roleBit;
                }
                if (rootRoleMask != 0b1110)
                {
                    error = "Packed EntityScene presentation-root role set is incomplete.";
                    return false;
                }
                for (int i = 0; i < identities.Length; i++)
                {
                    OperationMapEntityPresentationIdentity identity = identities[i];
                    if (!identity.OperationMapId.Equals(contract.OperationMapId))
                    {
                        error = "Packed EntityScene contains an identity for a different operation map.";
                        return false;
                    }
                    if (!sourceIds.Add(identity.SourceGlobalObjectId))
                    {
                        error =
                            "Packed EntityScene contains duplicate source identity " +
                            $"'{identity.SourceGlobalObjectId}'.";
                        return false;
                    }
                    switch (identity.Role)
                    {
                        case 1:
                            gameplayBuildings++;
                            break;
                        case 2:
                            gameplayVehicles++;
                            break;
                        case 3:
                            renderOnly++;
                            break;
                        default:
                            error = $"Packed EntityScene contains unknown identity role {identity.Role}.";
                            return false;
                    }
                }

                int generatedGameplayBuildings = 0;
                int generatedRenderOnly = 0;
                for (int i = 0; i < generatedIdentities.Length; i++)
                {
                    DenseCityPresentationIdentity identity = generatedIdentities[i];
                    if (identity.StableId.Length == 0 ||
                        !generatedIds.Add(identity.StableId))
                    {
                        error =
                            "Packed EntityScene contains empty or duplicate generated identity " +
                            $"'{identity.StableId}'.";
                        return false;
                    }
                    switch (identity.Role)
                    {
                        case 1:
                            if (identity.Category !=
                                    (byte)DenseCityPresentationSemanticCategory.GameplayBuildingIntact ||
                                identity.Flags != (byte)DenseCityPresentationSemanticFlags.None)
                            {
                                error =
                                    "Packed EntityScene generated building identity has invalid " +
                                    $"semantic metadata: category={identity.Category} flags={identity.Flags}.";
                                return false;
                            }
                            generatedGameplayBuildings++;
                            break;
                        case 3:
                            bool renderOnlyCategory =
                                identity.Category is
                                    (byte)DenseCityPresentationSemanticCategory.Infrastructure or
                                    (byte)DenseCityPresentationSemanticCategory.Vegetation or
                                    (byte)DenseCityPresentationSemanticCategory.Prop or
                                    (byte)DenseCityPresentationSemanticCategory.Horizon;
                            byte knownFlags =
                                (byte)DenseCityPresentationSemanticFlags.AllowsProtectedOverlap;
                            bool overlapIsValid =
                                (identity.Flags & knownFlags) == 0 ||
                                identity.Category ==
                                (byte)DenseCityPresentationSemanticCategory.Infrastructure;
                            if (!renderOnlyCategory ||
                                (identity.Flags & ~knownFlags) != 0 ||
                                !overlapIsValid)
                            {
                                error =
                                    "Packed EntityScene generated render-only identity has invalid " +
                                    $"semantic metadata: category={identity.Category} flags={identity.Flags}.";
                                return false;
                            }
                            generatedRenderOnly++;
                            break;
                        default:
                            error =
                                $"Packed EntityScene contains unknown generated identity role " +
                                $"{identity.Role}.";
                            return false;
                    }
                }

                int virtualizedGeneratedIdentityCount =
                    virtualizationContract.VirtualizedGeneratedBuildingIdentityCount +
                    virtualizationContract.VirtualizedGeneratedRenderOnlyIdentityCount;
                if (generatedIdentities.Length + virtualizedGeneratedIdentityCount !=
                    contract.ExpectedGeneratedIdentityCount)
                {
                    error =
                        "Packed EntityScene generated identity count differs from the " +
                        $"readiness contract: " +
                        $"{generatedIdentities.Length + virtualizedGeneratedIdentityCount}/" +
                        $"{contract.ExpectedGeneratedIdentityCount}.";
                    return false;
                }
                gameplayBuildings += generatedGameplayBuildings +
                                     virtualizationContract
                                         .VirtualizedAcceptedBuildingIdentityCount +
                                     virtualizationContract
                                         .VirtualizedGeneratedBuildingIdentityCount;
                renderOnly += generatedRenderOnly +
                              virtualizationContract
                                  .VirtualizedAcceptedRenderOnlyIdentityCount +
                              virtualizationContract
                                  .VirtualizedGeneratedRenderOnlyIdentityCount;
                if (gameplayBuildings != contract.ExpectedGameplayBuildingCount ||
                    gameplayVehicles != contract.ExpectedGameplayVehicleCount ||
                    renderOnly != contract.ExpectedRenderOnlyCount)
                {
                    error =
                        "Packed EntityScene identity counts differ from the readiness contract: " +
                        $"buildings={gameplayBuildings}/{contract.ExpectedGameplayBuildingCount} " +
                        $"vehicles={gameplayVehicles}/{contract.ExpectedGameplayVehicleCount} " +
                        $"renderOnly={renderOnly}/{contract.ExpectedRenderOnlyCount}.";
                    return false;
                }
                if (buildingCount + virtualizedBuildingCount !=
                        contract.ExpectedGameplayBuildingCount ||
                    vehicleCount != contract.ExpectedGameplayVehicleCount)
                {
                    error =
                        "Packed EntityScene initial gameplay state is incomplete: " +
                        $"buildings={buildingCount + virtualizedBuildingCount}/" +
                        $"{contract.ExpectedGameplayBuildingCount} " +
                        $"vehicles={vehicleCount}/{contract.ExpectedGameplayVehicleCount}.";
                    return false;
                }

                error = null;
                return true;
            }
            finally
            {
                contracts.Dispose();
                roots.Dispose();
                identities.Dispose();
                generatedIdentities.Dispose();
                databaseEntities.Dispose();
                databases.Dispose();
                packedReadinessEntities.Dispose();
                packedReadinessContracts.Dispose();
                slots.Dispose();
                sourceIds.Dispose();
                generatedIds.Dispose();
            }
        }

        private static bool TryValidateRenderResidency(
            EntityManager entityManager,
            DynamicBuffer<ResolvedSectionEntity> sections,
            string expectedOperationMapId,
            OperationMapRenderResidencyMode renderResidencyMode,
            NativeList<Entity> databaseEntities,
            NativeList<OperationMapRenderDatabaseComponent> databases,
            NativeList<Entity> packedReadinessEntities,
            NativeList<OperationMapRenderPackedReadinessComponent>
                packedReadinessContracts,
            NativeList<OperationMapRenderProxySlotComponent> slots,
            int eligibleSourceSurvivorCount,
            int virtualizedBuildingCount,
            int overlappingBuildingCount,
            out OperationMapRenderPackedReadinessComponent readiness,
            out string error)
        {
            readiness = default;
            if (renderResidencyMode !=
                    OperationMapRenderResidencyMode.ResidentEntities &&
                renderResidencyMode !=
                    OperationMapRenderResidencyMode.VirtualizedProxyPool)
            {
                error =
                    $"Unknown operation-map render-residency mode: " +
                    $"{(byte)renderResidencyMode}.";
                return false;
            }

            if (renderResidencyMode ==
                OperationMapRenderResidencyMode.ResidentEntities)
            {
                if (databaseEntities.Length != 0 ||
                    packedReadinessEntities.Length != 0 ||
                    slots.Length != 0 ||
                    eligibleSourceSurvivorCount != 0 ||
                    virtualizedBuildingCount != 0 ||
                    overlappingBuildingCount != 0)
                {
                    error =
                        "ResidentEntities packed content contains virtualized-render ownership.";
                    return false;
                }

                error = null;
                return true;
            }

            if (databaseEntities.Length != 1 || databases.Length != 1)
            {
                error =
                    "VirtualizedProxyPool requires exactly one packed render database.";
                return false;
            }
            if (packedReadinessEntities.Length != 1 ||
                packedReadinessContracts.Length != 1 ||
                packedReadinessEntities[0] != databaseEntities[0])
            {
                error =
                    "VirtualizedProxyPool requires one packed readiness contract on " +
                    "the render database entity.";
                return false;
            }
            if (overlappingBuildingCount != 0)
            {
                error =
                    "A packed building cannot retain resident and virtualized presentation ownership.";
                return false;
            }

            OperationMapRenderDatabaseComponent database = databases[0];
            if (!database.Blob.IsCreated)
            {
                error = "Packed render database blob is not created.";
                return false;
            }
            ref OperationMapRenderDatabaseBlob databaseBlob =
                ref database.Blob.Value;
            if (!string.Equals(
                    databaseBlob.OperationMapId.ToString(),
                    expectedOperationMapId,
                    StringComparison.Ordinal) ||
                database.SchemaVersion != databaseBlob.SchemaVersion ||
                !database.ContentHash.Equals(databaseBlob.ContentHash) ||
                databaseBlob.Prototypes.Length == 0 ||
                databaseBlob.Parts.Length == 0 ||
                databaseBlob.Placements.Length == 0 ||
                databaseBlob.Cells.Length == 0 ||
                databaseBlob.PoolBuckets.Length == 0)
            {
                error =
                    "Packed render database identity, schema, content hash, or logical arrays are invalid.";
                return false;
            }

            readiness = packedReadinessContracts[0];
            int virtualizedBuildingIdentityCount =
                readiness.VirtualizedAcceptedBuildingIdentityCount +
                readiness.VirtualizedGeneratedBuildingIdentityCount;
            int virtualizedRenderOnlyIdentityCount =
                readiness.VirtualizedAcceptedRenderOnlyIdentityCount +
                readiness.VirtualizedGeneratedRenderOnlyIdentityCount;
            if (readiness.ResidencyMode !=
                    (byte)OperationMapRenderResidencyMode.VirtualizedProxyPool ||
                readiness.EligibleSourceRowCount <= 0 ||
                readiness.EligibleSourceRendererCount <= 0 ||
                readiness.EligibleSourceRendererCount >
                    readiness.EligibleSourceRowCount ||
                readiness.ResidentSourceRowCount < 0 ||
                readiness.ProxySlotCount <= 0 ||
                readiness.VirtualizedAcceptedBuildingIdentityCount < 0 ||
                readiness.VirtualizedAcceptedRenderOnlyIdentityCount < 0 ||
                readiness.VirtualizedGeneratedBuildingIdentityCount < 0 ||
                readiness.VirtualizedGeneratedRenderOnlyIdentityCount < 0 ||
                virtualizedBuildingIdentityCount +
                    virtualizedRenderOnlyIdentityCount <= 0 ||
                virtualizedBuildingIdentityCount +
                    virtualizedRenderOnlyIdentityCount >
                    databaseBlob.Placements.Length)
            {
                error = "Packed render readiness metrics are invalid.";
                return false;
            }
            if (eligibleSourceSurvivorCount != 0)
            {
                error =
                    $"Packed EntityScene retains {eligibleSourceSurvivorCount} " +
                    "eligible source render rows.";
                return false;
            }
            if (virtualizedBuildingCount != virtualizedBuildingIdentityCount)
            {
                error =
                    "Packed virtualized-building count does not match its readiness contract.";
                return false;
            }

            int expectedSlotCount = 0;
            for (int bucketIndex = 0;
                 bucketIndex < databaseBlob.PoolBuckets.Length;
                 bucketIndex++)
            {
                OperationMapRenderPoolBucketBlob bucket =
                    databaseBlob.PoolBuckets[bucketIndex];
                if (bucket.FirstSlot != expectedSlotCount ||
                    bucket.Capacity <= 0 ||
                    bucket.PeakRequiredCount <= 0 ||
                    bucket.HeadroomCount < 0)
                {
                    error =
                        "Packed render database contains an invalid pool-bucket range.";
                    return false;
                }
                checked
                {
                    expectedSlotCount += bucket.Capacity;
                }
            }
            if (expectedSlotCount != readiness.ProxySlotCount ||
                slots.Length != readiness.ProxySlotCount)
            {
                error =
                    "Packed proxy-slot count does not match the database and readiness contract.";
                return false;
            }
            var seenSlots = new bool[expectedSlotCount];
            for (int index = 0; index < slots.Length; index++)
            {
                OperationMapRenderProxySlotComponent slot = slots[index];
                if (slot.SlotIndex < 0 ||
                    slot.SlotIndex >= expectedSlotCount ||
                    seenSlots[slot.SlotIndex] ||
                    slot.PoolBucketIndex < 0 ||
                    slot.PoolBucketIndex >= databaseBlob.PoolBuckets.Length)
                {
                    error =
                        "Packed proxy slots contain an invalid or duplicate slot identity.";
                    return false;
                }
                OperationMapRenderPoolBucketBlob bucket =
                    databaseBlob.PoolBuckets[slot.PoolBucketIndex];
                if (slot.SlotIndex < bucket.FirstSlot ||
                    slot.SlotIndex >= bucket.FirstSlot + bucket.Capacity)
                {
                    error =
                        "Packed proxy slot is outside its reported pool-bucket range.";
                    return false;
                }
                seenSlots[slot.SlotIndex] = true;
            }

            Entity databaseEntity = databaseEntities[0];
            if (!entityManager.HasBuffer<
                    OperationMapRenderResidentSourceRowComponent>(databaseEntity))
            {
                error =
                    "Packed render database is missing exact resident-source rows.";
                return false;
            }
            DynamicBuffer<OperationMapRenderResidentSourceRowComponent>
                residentRows = entityManager.GetBuffer<
                    OperationMapRenderResidentSourceRowComponent>(
                    databaseEntity,
                    true);
            if (residentRows.Length != readiness.ResidentSourceRowCount)
            {
                error =
                    "Packed resident-source count does not match its readiness contract.";
                return false;
            }
            var sectionSet = new HashSet<Entity>();
            for (int index = 0; index < sections.Length; index++)
                sectionSet.Add(sections[index].SectionEntity);
            var residentIdentities = new HashSet<PackedSourceRowKey>();
            for (int index = 0; index < residentRows.Length; index++)
            {
                OperationMapRenderResidentSourceRowComponent row =
                    residentRows[index];
                if (!entityManager.Exists(row.RenderEntity) ||
                    !entityManager.HasComponent<SceneTag>(row.RenderEntity) ||
                    !sectionSet.Contains(
                        entityManager.GetSharedComponent<SceneTag>(
                            row.RenderEntity).SceneEntity) ||
                    entityManager.HasComponent<
                        OperationMapRenderEligibleSourceComponent>(
                        row.RenderEntity) ||
                    !residentIdentities.Add(PackedSourceRowKey.From(row)))
                {
                    error =
                        "Packed resident-source rows contain a missing, foreign, eligible, " +
                        "or duplicate entity identity.";
                    return false;
                }
            }

            error = null;
            return true;
        }

        private static void Append<T>(EntityQuery query, ref NativeList<T> destination)
            where T : unmanaged, IComponentData
        {
            using NativeArray<T> values = query.ToComponentDataArray<T>(Allocator.Temp);
            destination.AddRange(values);
        }

        private static void Append(
            EntityQuery query,
            ref NativeList<Entity> destination)
        {
            using NativeArray<Entity> values =
                query.ToEntityArray(Allocator.Temp);
            destination.AddRange(values);
        }

        private readonly struct PackedSourceRowKey :
            IEquatable<PackedSourceRowKey>
        {
            private readonly ulong ownerLow;
            private readonly ulong ownerHigh;
            private readonly ulong pathLow;
            private readonly ulong pathHigh;

            private PackedSourceRowKey(
                OperationMapRenderIdentity128 owner,
                OperationMapRenderIdentity128 path)
            {
                ownerLow = owner.Low;
                ownerHigh = owner.High;
                pathLow = path.Low;
                pathHigh = path.High;
            }

            internal static PackedSourceRowKey From(
                OperationMapRenderResidentSourceRowComponent row) =>
                new(row.OwnerIdentity, row.RendererPathIdentity);

            public bool Equals(PackedSourceRowKey other) =>
                ownerLow == other.ownerLow &&
                ownerHigh == other.ownerHigh &&
                pathLow == other.pathLow &&
                pathHigh == other.pathHigh;

            public override bool Equals(object obj) =>
                obj is PackedSourceRowKey other && Equals(other);

            public override int GetHashCode()
            {
                unchecked
                {
                    int hash = (int)ownerLow ^ (int)(ownerLow >> 32);
                    hash = (hash * 397) ^ (int)ownerHigh ^
                           (int)(ownerHigh >> 32);
                    hash = (hash * 397) ^ (int)pathLow ^
                           (int)(pathLow >> 32);
                    return (hash * 397) ^ (int)pathHigh ^
                           (int)(pathHigh >> 32);
                }
            }
        }
    }
}
