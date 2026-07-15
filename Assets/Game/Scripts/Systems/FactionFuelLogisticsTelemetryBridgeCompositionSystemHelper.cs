using Unity.Collections;
using Unity.Entities;
using Game.Components;

namespace Game.Runtime
{
    internal sealed class FactionFuelLogisticsTelemetryBridgeCompositionSystemHelper
    {
        private static readonly FixedString64Bytes TrayTruckSourceKey = new("Unit_Veh_Truck_Tray");
        private World _queryWorld;
        private EntityQuery _query;

        internal void SetResourceHaulStatus(
            EntityManager em,
            Entity entity,
            FuelLogisticsTaskStatusCode statusCode,
            FuelLogisticsBlockReasonCode reasonCode,
            ResourceHaulerUtilitySystemHelper.ResourceHaulKind resourceKind)
        {
            bool enteredBlockedEpisode = statusCode == FuelLogisticsTaskStatusCode.Blocked &&
                                         (!em.HasComponent<UnitResourceHaulStatus>(entity) ||
                                          em.GetComponentData<UnitResourceHaulStatus>(entity).StatusCode !=
                                          (byte)FuelLogisticsTaskStatusCode.Blocked);
            UnitResourceHaulStatus status = new()
            {
                StatusCode = (byte)statusCode,
                ReasonCode = (byte)reasonCode,
                ResourceKind = (byte)resourceKind
            };
            if (em.HasComponent<UnitResourceHaulStatus>(entity))
                em.SetComponentData(entity, status);
            else
                em.AddComponentData(entity, status);

            if (enteredBlockedEpisode)
                RecordRouteFailure(em, entity, resourceKind);
        }

        internal void RecordRouteAssignment(
            EntityManager em,
            Entity entity,
            ResourceHaulerUtilitySystemHelper.ResourceHaulKind resourceKind,
            bool isReassignment)
        {
            if (!IsTrayOilTelemetryEligible(em, entity, resourceKind, out byte factionId) ||
                !TryResolveTelemetryEntity(em, factionId, out Entity telemetryEntity))
            {
                return;
            }

            FactionFuelLogisticsTelemetryComponent telemetry =
                em.GetComponentData<FactionFuelLogisticsTelemetryComponent>(telemetryEntity);
            FactionFuelLogisticsTelemetryUtilitySystemHelper.RecordRouteAssignment(ref telemetry, isReassignment);
            em.SetComponentData(telemetryEntity, telemetry);
        }

        internal void RecordOilDelivery(
            EntityManager em,
            Entity entity,
            RuntimeBuildingEntity destination,
            ResourceHaulerUtilitySystemHelper.ResourceHaulKind resourceKind,
            float deliveredBarrels)
        {
            if (!IsTrayOilTelemetryEligible(em, entity, resourceKind, out byte factionId) ||
                destination == null ||
                destination.OwnerFactionId != factionId ||
                !TryResolveTelemetryEntity(em, factionId, out Entity telemetryEntity))
            {
                return;
            }

            bool isFabricationDepot = ResourceHaulerAutomaticRoutePolicySystemHelper.TryGetMaterialFabrication(
                em,
                destination,
                out MaterialFabricationComponent fabrication) &&
                fabrication.OwnerFactionId == factionId;
            bool isRefinery = !isFabricationDepot && destination.FuelBarrelsPerDay > 0f;
            FactionFuelLogisticsTelemetryComponent telemetry =
                em.GetComponentData<FactionFuelLogisticsTelemetryComponent>(telemetryEntity);
            if (FactionFuelLogisticsTelemetryUtilitySystemHelper.RecordOilDelivery(
                    ref telemetry,
                    deliveredBarrels,
                    isFabricationDepot,
                    isRefinery))
            {
                em.SetComponentData(telemetryEntity, telemetry);
            }
        }

        private void RecordRouteFailure(
            EntityManager em,
            Entity entity,
            ResourceHaulerUtilitySystemHelper.ResourceHaulKind resourceKind)
        {
            if (!IsTrayOilTelemetryEligible(em, entity, resourceKind, out byte factionId) ||
                !TryResolveTelemetryEntity(em, factionId, out Entity telemetryEntity))
            {
                return;
            }

            FactionFuelLogisticsTelemetryComponent telemetry =
                em.GetComponentData<FactionFuelLogisticsTelemetryComponent>(telemetryEntity);
            FactionFuelLogisticsTelemetryUtilitySystemHelper.RecordRouteFailure(ref telemetry);
            em.SetComponentData(telemetryEntity, telemetry);
        }

        private static bool IsTrayOilTelemetryEligible(
            EntityManager em,
            Entity entity,
            ResourceHaulerUtilitySystemHelper.ResourceHaulKind resourceKind,
            out byte factionId)
        {
            factionId = default;
            if (resourceKind != ResourceHaulerUtilitySystemHelper.ResourceHaulKind.Oil ||
                !em.Exists(entity) ||
                !em.HasComponent<UnitSourcePrefabKey>(entity) ||
                !em.HasComponent<Faction>(entity) ||
                !em.GetComponentData<UnitSourcePrefabKey>(entity).Value.Equals(TrayTruckSourceKey))
            {
                return false;
            }

            factionId = em.GetComponentData<Faction>(entity).Id;
            return true;
        }

        private bool TryResolveTelemetryEntity(EntityManager em, byte factionId, out Entity telemetryEntity)
        {
            telemetryEntity = Entity.Null;
            EnsureQuery(em);
            EntityTypeHandle entityType = em.GetEntityTypeHandle();
            ComponentTypeHandle<FactionEconomy> economyType = em.GetComponentTypeHandle<FactionEconomy>(true);
            ComponentTypeHandle<FactionFuelLogisticsTelemetryComponent> telemetryType =
                em.GetComponentTypeHandle<FactionFuelLogisticsTelemetryComponent>(true);
            using NativeArray<ArchetypeChunk> chunks = _query.ToArchetypeChunkArray(Allocator.Temp);
            for (int chunkIndex = 0; chunkIndex < chunks.Length; chunkIndex++)
            {
                ArchetypeChunk chunk = chunks[chunkIndex];
                NativeArray<Entity> entities = chunk.GetNativeArray(entityType);
                NativeArray<FactionEconomy> economies = chunk.GetNativeArray(ref economyType);
                NativeArray<FactionFuelLogisticsTelemetryComponent> telemetry = chunk.GetNativeArray(ref telemetryType);
                for (int entityIndex = 0; entityIndex < entities.Length; entityIndex++)
                {
                    if (economies[entityIndex].FactionId != factionId || telemetry[entityIndex].FactionId != factionId)
                        continue;

                    if (telemetryEntity != Entity.Null)
                    {
                        telemetryEntity = Entity.Null;
                        return false;
                    }

                    telemetryEntity = entities[entityIndex];
                }
            }

            return telemetryEntity != Entity.Null;
        }

        private void EnsureQuery(EntityManager em)
        {
            World world = em.World;
            if (_queryWorld == world && world != null && world.IsCreated)
                return;

            _queryWorld = world;
            _query = em.CreateEntityQuery(
                ComponentType.ReadOnly<FactionEconomy>(),
                ComponentType.ReadOnly<FactionFuelLogisticsTelemetryComponent>());
        }
    }
}
