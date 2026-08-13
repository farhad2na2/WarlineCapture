using Game.Components;
using Game.Missions.Contracts;
using Unity.Collections;
using Unity.Entities;

namespace Game.Runtime
{
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    [UpdateBefore(typeof(CampaignMissionRuntimeSystem))]
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
            using EntityQuery units = entityManager.CreateEntityQuery(
                ComponentType.ReadOnly<CampaignMissionUnitRoleComponent>());
            using NativeArray<Entity> entities = units.ToEntityArray(Allocator.Temp);
            for (int i = 0; i < entities.Length; i++)
                cleanup.DestroyEntity(entities[i]);

            ClearBufferIfPresent<CampaignMissionActionRequestElement>(entityManager, root);
            ClearBufferIfPresent<CampaignMissionActionResultElement>(entityManager, root);
            ClearBufferIfPresent<CampaignMissionLaunchResultElement>(entityManager, root);
            ClearBufferIfPresent<CampaignMissionSettlementRequestElement>(entityManager, root);
            ClearBufferIfPresent<CampaignMissionSettlementResultElement>(entityManager, root);
            ClearBufferIfPresent<CampaignMissionGuidanceAcknowledgementRequestElement>(entityManager, root);
            if (entityManager.HasComponent<CampaignMissionGuidanceProjectionComponent>(root))
                entityManager.SetComponentData(root, default(CampaignMissionGuidanceProjectionComponent));
            if (entityManager.HasComponent<CampaignMissionResultComponent>(root))
                entityManager.SetComponentData(root, default(CampaignMissionResultComponent));
        }

        private static void ClearBufferIfPresent<T>(EntityManager entityManager, Entity root)
            where T : unmanaged, IBufferElementData
        {
            if (entityManager.HasBuffer<T>(root))
                entityManager.GetBuffer<T>(root).Clear();
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
            if (blob.Missions.Length != 1 || !blob.Missions[0].MissionId.Equals(request.MissionId) ||
                !blob.Missions[0].ScenarioId.Equals(request.ScenarioId) ||
                !blob.Missions[0].OperationMapId.Equals(request.OperationMapId))
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
