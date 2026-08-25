using Game.Components;
using Game.Missions.Contracts;
using Unity.Collections;
using Unity.Entities;

namespace Game.Runtime
{
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    public partial struct CampaignMissionLaunchSystem : ISystem
    {
        public void OnCreate(ref SystemState state) => state.RequireForUpdate<CampaignMissionRootComponent>();

        public void OnUpdate(ref SystemState state)
        {
            EntityCommandBuffer cleanup = new(Allocator.Temp);
            bool cleanupQueued = false;
            foreach ((RefRO<CampaignMissionCatalogComponent> catalog,
                      RefRW<CampaignMissionLaunchQueueComponent> queue,
                      RefRW<CampaignMissionRuntimeComponent> runtime,
                      RefRW<CampaignMissionAttemptFactsComponent> facts,
                      DynamicBuffer<CampaignMissionLaunchRequestElement> requests,
                      DynamicBuffer<CampaignMissionLaunchResultElement> results,
                      Entity root)
                     in SystemAPI.Query<RefRO<CampaignMissionCatalogComponent>,
                         RefRW<CampaignMissionLaunchQueueComponent>, RefRW<CampaignMissionRuntimeComponent>,
                         RefRW<CampaignMissionAttemptFactsComponent>,
                         DynamicBuffer<CampaignMissionLaunchRequestElement>,
                         DynamicBuffer<CampaignMissionLaunchResultElement>>().WithEntityAccess())
            {
                if (requests.Length == 0)
                    continue;
                CampaignMissionLaunchRequestElement request = requests[0];
                if (!SystemAPI.TryGetSingleton(out ActiveOperationMapComponent activeMap) ||
                    !SystemAPI.TryGetSingleton(out OperationMapReadinessComponent readiness))
                    continue;

                FixedString64Bytes reason = default;
                bool terminalFailure = readiness.FailedFlags != OperationMapReadinessFlags.None;
                bool ready = (readiness.ReadyFlags & readiness.RequiredFlags) == readiness.RequiredFlags;
                bool accepted = !terminalFailure && ready &&
                    TryValidate(in request, in catalog.ValueRO, in activeMap, out reason);
                if (!accepted && !terminalFailure && !ready)
                    continue;
                if (terminalFailure)
                    reason = new FixedString64Bytes("operation-map-readiness-failed");

                if (accepted)
                {
                    QueueAttemptCleanup(state.EntityManager, ref cleanup, root);
                    cleanupQueued = true;
                    runtime.ValueRW = CreateRuntime(in request, catalog.ValueRO.SourceVersion, readiness);
                    facts.ValueRW = default;
                    if (state.EntityManager.HasComponent<CampaignMissionAttemptResourceInitializationComponent>(root))
                    {
                        state.EntityManager.SetComponentData(root,
                            new CampaignMissionAttemptResourceInitializationComponent
                            {
                                SessionToken = request.SessionToken,
                                AttemptOrdinal = request.AttemptOrdinal
                            });
                    }
                    if (state.EntityManager.HasComponent<CampaignMissionAttemptFactProjectionStateComponent>(root))
                    {
                        state.EntityManager.SetComponentData(root,
                            new CampaignMissionAttemptFactProjectionStateComponent
                            {
                                SessionToken = request.SessionToken,
                                AttemptOrdinal = request.AttemptOrdinal,
                                SourceVersion = catalog.ValueRO.SourceVersion
                            });
                    }
                    if (state.EntityManager.HasComponent<CampaignMissionDelayedWaveStateComponent>(root))
                    {
                        state.EntityManager.SetComponentData(root, new CampaignMissionDelayedWaveStateComponent
                        {
                            SessionToken = request.SessionToken,
                            AttemptOrdinal = request.AttemptOrdinal,
                            SourceVersion = catalog.ValueRO.SourceVersion,
                            Initialized = 1
                        });
                    }
                    CampaignMissionLaunchQueueComponent nextQueue = queue.ValueRO;
                    nextQueue.LastTransitionToken = request.TransitionToken;
                    nextQueue.Version++;
                    queue.ValueRW = nextQueue;
                }
                results.Add(new CampaignMissionLaunchResultElement
                {
                    TransitionToken = request.TransitionToken,
                    SessionToken = request.SessionToken,
                    AttemptOrdinal = request.AttemptOrdinal,
                    Accepted = accepted ? (byte)1 : (byte)0,
                    ReasonCode = accepted ? default : reason
                });
                requests.RemoveAt(0);
            }
            if (cleanupQueued)
                cleanup.Playback(state.EntityManager);
            cleanup.Dispose();
        }

        internal static void QueueAttemptCleanup(
            EntityManager entityManager, ref EntityCommandBuffer cleanup, Entity root)
        {
            using EntityQuery persistentMapRoles = new EntityQueryBuilder(Allocator.Temp)
                .WithAll<CampaignMissionUnitRoleComponent, OperationMapBuildingComponent>()
                .Build(entityManager);
            using EntityQuery transientMissionUnits = new EntityQueryBuilder(Allocator.Temp)
                .WithAll<CampaignMissionUnitRoleComponent>()
                .WithNone<OperationMapBuildingComponent>()
                .Build(entityManager);
            using EntityQuery ambientCivilians = new EntityQueryBuilder(Allocator.Temp)
                .WithAll<CampaignMissionAmbientCivilianComponent>()
                .Build(entityManager);
#pragma warning disable 0618 // Capture the exact attempt-owned set before deferred playback.
            cleanup.RemoveComponent<CampaignMissionUnitRoleComponent>(
                persistentMapRoles, EntityQueryCaptureMode.AtRecord);
            cleanup.DestroyEntity(transientMissionUnits, EntityQueryCaptureMode.AtRecord);
            cleanup.DestroyEntity(ambientCivilians, EntityQueryCaptureMode.AtRecord);
#pragma warning restore 0618

            QueueAttemptOwnedRuntimeCleanup(entityManager, ref cleanup, root);
            ThreatWarningRuntimeState.Reset(entityManager);
            ResetRuntimeCameraFocus(entityManager);
            ClearBufferIfPresent<CampaignMissionActionRequestElement>(entityManager, root);
            ClearBufferIfPresent<CampaignMissionActionResultElement>(entityManager, root);
            ClearBufferIfPresent<CampaignMissionLaunchResultElement>(entityManager, root);
            ClearBufferIfPresent<CampaignMissionSettlementRequestElement>(entityManager, root);
            ClearBufferIfPresent<CampaignMissionSettlementResultElement>(entityManager, root);
            ClearBufferIfPresent<CampaignMissionGuidanceAcknowledgementRequestElement>(entityManager, root);
            ResetComponentIfPresent<CampaignMissionAttemptResourceInitializationComponent>(entityManager, root);
            ResetComponentIfPresent<CampaignMissionAttemptFactProjectionStateComponent>(entityManager, root);
            ResetComponentIfPresent<CampaignMissionDelayedWaveStateComponent>(entityManager, root);
            ResetComponentIfPresent<CampaignMissionOpeningPresentationComponent>(entityManager, root);
            ResetComponentIfPresent<CampaignMissionFinalePresentationComponent>(entityManager, root);
            ResetComponentIfPresent<CampaignMissionGuidanceProjectionComponent>(entityManager, root);
            ResetComponentIfPresent<CampaignMissionResultComponent>(entityManager, root);
        }

        private static void QueueAttemptOwnedRuntimeCleanup(
            EntityManager entityManager,
            ref EntityCommandBuffer cleanup,
            Entity root)
        {
            if (!entityManager.HasComponent<CampaignMissionCatalogComponent>(root) ||
                !entityManager.HasComponent<CampaignMissionRuntimeComponent>(root) ||
                !entityManager.HasComponent<CampaignMissionAttemptFactProjectionStateComponent>(root))
                return;

            CampaignMissionCatalogComponent catalog =
                entityManager.GetComponentData<CampaignMissionCatalogComponent>(root);
            CampaignMissionRuntimeComponent runtime =
                entityManager.GetComponentData<CampaignMissionRuntimeComponent>(root);
            CampaignMissionAttemptFactProjectionStateComponent projection =
                entityManager.GetComponentData<CampaignMissionAttemptFactProjectionStateComponent>(root);
            if (projection.Initialized == 0 || projection.SourceVersion != catalog.SourceVersion ||
                !projection.SessionToken.Equals(runtime.SessionToken) ||
                projection.AttemptOrdinal != runtime.AttemptOrdinal)
                return;

            bool hasRequiredBuilding = CampaignMissionAttemptFactProjectionSystem.TryResolveRequiredBuilding(
                in catalog, in runtime, out FixedString128Bytes requiredBuildingId, out _);
            bool hasRequiredUnit = CampaignMissionAttemptFactProjectionSystem.TryResolveRequiredUnit(
                in catalog, in runtime, out FixedString128Bytes requiredUnitId, out _);
            if (!hasRequiredBuilding && !hasRequiredUnit)
                return;

            using EntityQuery boundaryQuery = new EntityQueryBuilder(Allocator.Temp)
                .WithAll<
                    BuildingRuntimeStateTag,
                    BuildingRuntimeSpawnRequest,
                    BuildingRuntimeDeleteRequest,
                    BuildingProducedUnitReadModel>()
                .Build(entityManager);
            if (boundaryQuery.CalculateEntityCount() != 1)
                return;

            Entity boundary = boundaryQuery.GetSingletonEntity();
            if (hasRequiredBuilding)
            {
                DynamicBuffer<BuildingRuntimeSpawnRequest> spawnRequests =
                    entityManager.GetBuffer<BuildingRuntimeSpawnRequest>(boundary, true);
                DynamicBuffer<BuildingRuntimeDeleteRequest> deleteRequests =
                    entityManager.GetBuffer<BuildingRuntimeDeleteRequest>(boundary);
                for (int index = 0; index < spawnRequests.Length; index++)
                {
                    BuildingRuntimeSpawnRequest request = spawnRequests[index];
                    if (request.RequestId <= projection.BuildingRequestBaselineId ||
                        request.RequestKind != BuildingRuntimeSpawnRequest.KindBuilding ||
                        request.Status != BuildingRuntimeSpawnRequest.Succeeded ||
                        request.HasOwnerFaction == 0 ||
                        request.FactionId != FactionIdentity.PlayerFactionId ||
                        request.BuildingRuntimeId <= 0 ||
                        !request.BuildingId.Equals(requiredBuildingId) ||
                        ContainsDeleteRequest(deleteRequests, request.BuildingRuntimeId))
                        continue;

                    deleteRequests.Add(new BuildingRuntimeDeleteRequest
                    {
                        BuildingRuntimeId = request.BuildingRuntimeId
                    });
                }
            }

            if (!hasRequiredUnit)
                return;

            DynamicBuffer<BuildingProducedUnitReadModel> producedUnits =
                entityManager.GetBuffer<BuildingProducedUnitReadModel>(boundary, true);
            int baseline = projection.ProducedUnitReadModelBaselineCount;
            if (baseline < 0 || baseline > producedUnits.Length)
                return;

            for (int index = baseline; index < producedUnits.Length; index++)
            {
                BuildingProducedUnitReadModel produced = producedUnits[index];
                if (produced.HasOwnerFaction == 0 ||
                    produced.OwnerFactionId != FactionIdentity.PlayerFactionId ||
                    !FixedStringsEqual(in produced.UnitSourceKey, in requiredUnitId) ||
                    produced.Unit == Entity.Null || !entityManager.Exists(produced.Unit) ||
                    entityManager.HasComponent<Prefab>(produced.Unit) ||
                    entityManager.HasComponent<CampaignMissionUnitRoleComponent>(produced.Unit))
                    continue;

                cleanup.DestroyEntity(produced.Unit);
            }
        }

        private static bool ContainsDeleteRequest(
            DynamicBuffer<BuildingRuntimeDeleteRequest> requests,
            int buildingRuntimeId)
        {
            for (int index = 0; index < requests.Length; index++)
            {
                if (requests[index].BuildingRuntimeId == buildingRuntimeId)
                    return true;
            }

            return false;
        }

        private static bool FixedStringsEqual(
            in FixedString64Bytes left,
            in FixedString128Bytes right)
        {
            if (left.Length != right.Length)
                return false;

            for (int index = 0; index < left.Length; index++)
            {
                if (left[index] != right[index])
                    return false;
            }

            return true;
        }

        private static void ResetRuntimeCameraFocus(EntityManager entityManager)
        {
            using EntityQuery query = new EntityQueryBuilder(Allocator.Temp)
                .WithAllRW<RuntimeCameraFocusRequestComponent>()
                .Build(entityManager);
            if (query.CalculateEntityCount() == 1)
                entityManager.SetComponentData(query.GetSingletonEntity(), default(RuntimeCameraFocusRequestComponent));
        }

        private static void ClearBufferIfPresent<T>(EntityManager entityManager, Entity root)
            where T : unmanaged, IBufferElementData
        {
            if (entityManager.HasBuffer<T>(root))
                entityManager.GetBuffer<T>(root).Clear();
        }

        private static void ResetComponentIfPresent<T>(EntityManager entityManager, Entity root)
            where T : unmanaged, IComponentData
        {
            if (entityManager.HasComponent<T>(root))
                entityManager.SetComponentData(root, default(T));
        }

        public static bool TryValidate(
            in CampaignMissionLaunchRequestElement request,
            in CampaignMissionCatalogComponent catalog,
            in ActiveOperationMapComponent activeMap,
            out FixedString64Bytes reason)
        {
            reason = default;
            if (request.SchemaVersion != MissionLaunchPayloadFactory.CurrentSchemaVersion ||
                request.MissionId.IsEmpty || request.ScenarioId.IsEmpty || request.OperationMapId.IsEmpty ||
                request.SessionToken.IsEmpty || request.LaunchOrigin == MissionLaunchOriginKind.None ||
                request.RunKind == MissionRunKind.None || request.AttemptOrdinal < 0 ||
                request.DeterministicSeed == 0 || !catalog.Blob.IsCreated || catalog.SourceVersion == 0)
                return Reject("invalid-launch-request", out reason);
            ref CampaignMissionCatalogBlob blob = ref catalog.Blob.Value;
            int matchingDefinition = -1;
            for (int index = 0; index < blob.Missions.Length; index++)
            {
                ref CampaignMissionDefinitionBlob definition = ref blob.Missions[index];
                if (!definition.MissionId.Equals(request.MissionId) ||
                    !definition.ScenarioId.Equals(request.ScenarioId) ||
                    !definition.OperationMapId.Equals(request.OperationMapId))
                    continue;
                if (matchingDefinition >= 0)
                    return Reject("mission-catalog-ambiguous", out reason);
                matchingDefinition = index;
            }
            if (matchingDefinition < 0)
                return Reject("mission-catalog-mismatch", out reason);
            if (!activeMap.MissionId.Equals(request.MissionId) ||
                !activeMap.ScenarioId.Equals(request.ScenarioId) ||
                !activeMap.OperationMapId.Equals(request.OperationMapId))
                return Reject("operation-map-mismatch", out reason);
            return true;
        }

        private static CampaignMissionRuntimeComponent CreateRuntime(
            in CampaignMissionLaunchRequestElement request,
            uint sourceVersion,
            in OperationMapReadinessComponent readiness) => new()
        {
            MissionId = request.MissionId, ScenarioId = request.ScenarioId,
            OperationMapId = request.OperationMapId, SessionToken = request.SessionToken,
            Phase = MissionPhaseKind.Preparing, Outcome = MissionOutcomeKind.None,
            LaunchOrigin = request.LaunchOrigin, RunKind = request.RunKind, Guidance = request.Guidance,
            ReturnDestination = MissionReturnDestinationKind.None, TransitionToken = request.TransitionToken,
            Version = 1, SourceVersion = sourceVersion, AttemptOrdinal = request.AttemptOrdinal,
            DeterministicSeed = request.DeterministicSeed, RequiredReadiness = readiness.RequiredFlags,
            ReadyReadiness = readiness.ReadyFlags, ReplayTutorialEnabled = request.ReplayTutorialEnabled
        };

        private static bool Reject(string code, out FixedString64Bytes reason)
        {
            reason = new FixedString64Bytes(code);
            return false;
        }
    }
}
