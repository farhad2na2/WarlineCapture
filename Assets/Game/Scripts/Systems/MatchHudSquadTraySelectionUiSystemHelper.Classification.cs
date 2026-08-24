using System;
using Game.Components;
using Game.UI.Contracts;
using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;

namespace Game.Runtime
{
    public sealed partial class MatchHudSquadTraySelectionUiSystemHelper
    {
        private static UnitKind ResolveKind(EntityManager em, Entity entity)
        {
            string source = ResolveSource(em, entity);
            string lower = source.ToLowerInvariant();
            bool isAir = em.HasComponent<UnitAirMovement>(entity);
            bool hasTransport = em.HasComponent<UnitTransportCapacity>(entity) &&
                                em.GetComponentData<UnitTransportCapacity>(entity).SoldierCapacity > 0;
            bool usesVehicleMotion = isAir ||
                                     (em.HasComponent<UnitMovementBehavior>(entity) &&
                                      em.GetComponentData<UnitMovementBehavior>(entity).UsesVehicleMotion != 0);
            bool namedTransport = ContainsAny(lower, "transport", "apc", "truck", "tanker", "hauler", "canopy");
            bool isTransport = hasTransport || namedTransport && (usesVehicleMotion || isAir);
            bool isSoldier = !usesVehicleMotion &&
                             ContainsAny(lower, "chr_soldier", "_soldier_") &&
                             !ContainsAny(lower, "civilian", "contractor", "pilot");
            bool isHelicopter = isAir && ContainsAny(lower, "helicopter", "heli");
            bool isJet = isAir && ContainsAny(lower, "jet", "plane") && !isTransport;
            bool isAttackHelicopter = isHelicopter && !isTransport;
            bool isCombatVehicle = usesVehicleMotion &&
                                   !isAir &&
                                   !isTransport &&
                                   !ContainsAny(lower, "truck", "tanker", "hauler") &&
                                   ContainsAny(lower, "veh", "tank", "armored", "launcher", "radar");

            return new UnitKind(isSoldier, isCombatVehicle, isAttackHelicopter, isJet, isTransport);
        }

        private static string ResolveSource(EntityManager em, Entity entity)
        {
            if (em.HasComponent<UnitSourcePrefabKey>(entity))
            {
                string source = em.GetComponentData<UnitSourcePrefabKey>(entity).Value.ToString();
                if (!string.IsNullOrWhiteSpace(source))
                    return source;
            }

            if (em.HasComponent<UnitDisplayInfo>(entity))
            {
                string displayName = em.GetComponentData<UnitDisplayInfo>(entity).Name.ToString();
                if (!string.IsNullOrWhiteSpace(displayName))
                    return displayName;
            }

            return em.GetName(entity);
        }

        private static void LogPackedVehicleDiagnostics(
            Context context,
            EntityManager em,
            MatchHudSquadTraySlot slot)
        {
            if (context.LogSelectionDiagnostic == null)
                return;

            using EntityQuery query = em.CreateEntityQuery(
                ComponentType.ReadOnly<OperationMapAuthoredVehiclePresentation>());
            using NativeArray<Entity> entities = query.ToEntityArray(Allocator.Temp);
            context.LogSelectionDiagnostic(
                $"result=PackedVehicleDiagnostic slot={slot} authored={entities.Length}");

            for (int i = 0; i < entities.Length; i++)
            {
                Entity entity = entities[i];
                bool hasFaction = em.HasComponent<Faction>(entity);
                byte faction = hasFaction ? em.GetComponentData<Faction>(entity).Id : byte.MaxValue;
                bool hasDetail = em.HasComponent<UnitDetailedVisualReference>(entity);
                Entity detailRoot = hasDetail
                    ? em.GetComponentData<UnitDetailedVisualReference>(entity).Root
                    : Entity.Null;
                bool detailExists = detailRoot != Entity.Null && em.Exists(detailRoot);
                bool hasIdentity = detailExists &&
                                   em.HasComponent<OperationMapEntityPresentationIdentity>(detailRoot);
                OperationMapEntityPresentationIdentity identity = hasIdentity
                    ? em.GetComponentData<OperationMapEntityPresentationIdentity>(detailRoot)
                    : default;
                UnitKind kind = ResolveKind(em, entity);

                context.LogSelectionDiagnostic(
                    $"result=PackedVehicleEntity slot={slot} index={i} entity={entity} " +
                    $"prefab={(em.HasComponent<Prefab>(entity) ? 1 : 0)} " +
                    $"disabled={(em.HasComponent<Disabled>(entity) ? 1 : 0)} " +
                    $"faction={(hasFaction ? faction : -1)} " +
                    $"player={(hasFaction && FactionIdentity.IsPlayerControlled(faction) ? 1 : 0)} " +
                    $"source={ResolveSource(em, entity)} " +
                    $"grid={(em.HasComponent<UnitGrid>(entity) ? 1 : 0)} " +
                    $"move={(em.HasComponent<UnitMove>(entity) ? 1 : 0)} " +
                    $"world={(em.HasComponent<LocalToWorld>(entity) ? 1 : 0)} " +
                    $"movementBehavior={(em.HasComponent<UnitMovementBehavior>(entity) ? 1 : 0)} " +
                    $"transportCapacity={(em.HasComponent<UnitTransportCapacity>(entity) ? 1 : 0)} " +
                    $"air={(em.HasComponent<UnitAirMovement>(entity) ? 1 : 0)} " +
                    $"combat={(kind.IsCombatVehicle ? 1 : 0)} transport={(kind.IsTransport ? 1 : 0)} " +
                    $"detail={(hasDetail ? 1 : 0)} detailExists={(detailExists ? 1 : 0)} " +
                    $"identity={(hasIdentity ? 1 : 0)} identityRole={(hasIdentity ? identity.Role : -1)} " +
                    $"placement={(hasIdentity ? identity.PlacementIndex : -1)} " +
                    $"operationMap={(hasIdentity ? identity.OperationMapId.ToString() : "<missing>")}");
            }
        }

        private static bool ContainsAny(string value, params string[] needles)
        {
            if (string.IsNullOrEmpty(value))
                return false;

            for (int i = 0; i < needles.Length; i++)
            {
                if (value.Contains(needles[i], StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            return false;
        }
    }
}
