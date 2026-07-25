using System;
using Game.Components;
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
            using EntityQuery vehicleQuery = entityManager.CreateEntityQuery(
                ComponentType.ReadOnly<OperationMapAuthoredVehiclePresentation>(),
                ComponentType.ReadOnly<SceneTag>());
            var contracts = new NativeList<OperationMapEntityPresentationReadinessContract>(
                Allocator.Temp);
            var roots = new NativeList<OperationMapEntityPresentationRoot>(Allocator.Temp);
            var identities = new NativeList<OperationMapEntityPresentationIdentity>(
                Allocator.Temp);
            var generatedIdentities = new NativeList<DenseCityPresentationIdentity>(
                Allocator.Temp);
            var sourceIds = new NativeParallelHashSet<FixedString128Bytes>(
                16384,
                Allocator.Temp);
            var generatedIds = new NativeParallelHashSet<FixedString128Bytes>(
                65536,
                Allocator.Temp);
            int buildingCount = 0;
            int vehicleCount = 0;
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
                    vehicleQuery.SetSharedComponentFilter(sceneTag);

                    Append(contractQuery, ref contracts);
                    Append(rootQuery, ref roots);
                    Append(identityQuery, ref identities);
                    Append(generatedIdentityQuery, ref generatedIdentities);
                    buildingCount += buildingQuery.CalculateEntityCount();
                    vehicleCount += vehicleQuery.CalculateEntityCount();
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

                if (generatedIdentities.Length != contract.ExpectedGeneratedIdentityCount)
                {
                    error =
                        "Packed EntityScene generated identity count differs from the " +
                        $"readiness contract: {generatedIdentities.Length}/" +
                        $"{contract.ExpectedGeneratedIdentityCount}.";
                    return false;
                }
                gameplayBuildings += generatedGameplayBuildings;
                renderOnly += generatedRenderOnly;
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
                if (buildingCount != contract.ExpectedGameplayBuildingCount ||
                    vehicleCount != contract.ExpectedGameplayVehicleCount)
                {
                    error =
                        "Packed EntityScene initial gameplay state is incomplete: " +
                        $"buildings={buildingCount}/{contract.ExpectedGameplayBuildingCount} " +
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
                sourceIds.Dispose();
                generatedIds.Dispose();
            }
        }

        private static void Append<T>(EntityQuery query, ref NativeList<T> destination)
            where T : unmanaged, IComponentData
        {
            using NativeArray<T> values = query.ToComponentDataArray<T>(Allocator.Temp);
            destination.AddRange(values);
        }
    }
}
