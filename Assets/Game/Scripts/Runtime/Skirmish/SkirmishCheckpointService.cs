using System.Collections.Generic;
using Game.Components;
using Game.Configs;
using Game.Skirmish.Contracts;
using Unity.Collections;
using Unity.Entities;

namespace Game.Runtime
{
    public static class SkirmishCheckpointService
    {
        public static bool TryCapture(EntityManager em, Entity session, out SkirmishCheckpointDocument document)
        {
            document = null;
            if (em == default || session == Entity.Null ||
                !em.HasComponent<SkirmishExpandedSessionComponent>(session) ||
                !em.HasComponent<SkirmishResolvedSetupRecord>(session))
                return false;

            SkirmishExpandedSessionComponent state = em.GetComponentData<SkirmishExpandedSessionComponent>(session);
            if (state.IsLegacy != 0)
                return false;
            SkirmishResolvedSetup setup = em.GetComponentObject<SkirmishResolvedSetupRecord>(session).Setup;
            if (setup == null)
                return false;

            var payload = new SkirmishCheckpointPayload
            {
                SessionId = state.SessionId.ToString(),
                CatalogId = state.CatalogId.ToString(),
                DefinitionId = state.DefinitionId.ToString(),
                ContentVersion = state.ContentVersion,
                SetupHash = state.SetupHash,
                Seed = state.Seed,
                SizeId = state.SizeId,
                DifficultyId = state.DifficultyId,
                ObjectiveKind = state.ObjectiveKind,
                Phase = state.Phase,
                IsLegacy = state.IsLegacy,
                IsCustom = state.IsCustom,
                SimulationTick = state.TickClock,
                PlayerBaseObjectId = setup.PlayerBaseObjectId,
                EnemyBaseObjectId = setup.EnemyBaseObjectId
            };

            if (em.HasComponent<SkirmishObjectiveClockComponent>(session))
            {
                var clock = em.GetComponentData<SkirmishObjectiveClockComponent>(session);
                payload.ElapsedSeconds = clock.ElapsedSeconds;
                payload.DeadlineSeconds = clock.DeadlineSeconds;
                payload.Paused = clock.Paused;
                payload.Playing = clock.Playing;
            }

            if (em.HasComponent<SkirmishEconomyStockComponent>(session))
            {
                var stock = em.GetComponentData<SkirmishEconomyStockComponent>(session);
                payload.PlayerMaterials = stock.Materials;
                payload.PlayerOil = stock.Oil;
                payload.PlayerFuel = stock.Fuel;
            }

            if (em.HasComponent<SkirmishEnemyStockComponent>(session))
            {
                var stock = em.GetComponentData<SkirmishEnemyStockComponent>(session);
                payload.EnemyMaterials = stock.Materials;
                payload.EnemyOil = stock.Oil;
                payload.EnemyFuel = stock.Fuel;
            }

            if (em.HasComponent<SkirmishCapacityComponent>(session))
            {
                var capacity = em.GetComponentData<SkirmishCapacityComponent>(session);
                payload.PlayerInfantryLive = capacity.InfantryLive;
                payload.PlayerGroundLive = capacity.GroundLive;
                payload.PlayerSupplyLive = capacity.SupplyLive;
            }

            if (em.HasComponent<SkirmishEnemyCapacityComponent>(session))
            {
                var capacity = em.GetComponentData<SkirmishEnemyCapacityComponent>(session);
                payload.EnemyInfantryLive = capacity.InfantryLive;
                payload.EnemyGroundLive = capacity.GroundLive;
                payload.EnemySupplyLive = capacity.SupplyLive;
            }

            if (em.HasComponent<SkirmishBaseAssaultFactComponent>(session))
            {
                var facts = em.GetComponentData<SkirmishBaseAssaultFactComponent>(session);
                payload.PlayerDesignatedAlive = facts.PlayerDesignatedAlive;
                payload.EnemyDesignatedAlive = facts.EnemyDesignatedAlive;
            }
            else
            {
                CollectDesignated(em, state.SessionId, payload);
            }

            if (em.HasComponent<SkirmishResultComponent>(session))
            {
                var result = em.GetComponentData<SkirmishResultComponent>(session);
                payload.Outcome = result.Outcome;
                payload.EndReason = result.Reason;
            }

            payload.Actors = CaptureActors(em, state.SessionId);
            payload.Reservations = CaptureReservations(em, session);
            document = new SkirmishCheckpointDocument
            {
                Header = new SkirmishCheckpointHeader
                {
                    SchemaVersion = SkirmishCheckpointCodec.SchemaVersion,
                    Kind = SkirmishCheckpointSchemaKind.ExpandedV1,
                    SessionId = payload.SessionId,
                    CatalogId = payload.CatalogId,
                    DefinitionId = payload.DefinitionId,
                    ContentVersion = payload.ContentVersion,
                    SetupHash = payload.SetupHash,
                    SimulationTick = payload.SimulationTick
                },
                Payload = payload
            };
            return true;
        }

        public static bool TryApply(EntityManager em, Entity session, SkirmishCheckpointDocument document, out SkirmishReasonCode reason)
        {
            reason = SkirmishReasonCode.IncompatibleSave;
            if (document?.Header == null || document.Payload == null ||
                !em.HasComponent<SkirmishExpandedSessionComponent>(session) ||
                !em.HasComponent<SkirmishResolvedSetupRecord>(session))
                return false;

            SkirmishResolvedSetup setup = em.GetComponentObject<SkirmishResolvedSetupRecord>(session).Setup;
            if (!SkirmishCheckpointCodec.IsCompatible(document.Header, setup))
                return false;

            SkirmishCheckpointPayload payload = document.Payload;
            var state = em.GetComponentData<SkirmishExpandedSessionComponent>(session);
            if (!state.SessionId.ToString().Equals(payload.SessionId))
                return false;

            state.TickClock = payload.SimulationTick;
            state.Phase = payload.Phase == SkirmishSessionPhase.None ? state.Phase : payload.Phase;
            em.SetComponentData(session, state);

            if (em.HasComponent<SkirmishObjectiveClockComponent>(session))
            {
                var clock = em.GetComponentData<SkirmishObjectiveClockComponent>(session);
                clock.ElapsedSeconds = payload.ElapsedSeconds;
                clock.DeadlineSeconds = payload.DeadlineSeconds;
                clock.Paused = payload.Paused;
                clock.Playing = payload.Playing;
                em.SetComponentData(session, clock);
            }

            if (em.HasComponent<SkirmishEconomyStockComponent>(session))
            {
                var stock = em.GetComponentData<SkirmishEconomyStockComponent>(session);
                stock.Materials = payload.PlayerMaterials;
                stock.Oil = payload.PlayerOil;
                stock.Fuel = payload.PlayerFuel;
                em.SetComponentData(session, stock);
            }

            if (em.HasComponent<SkirmishEnemyStockComponent>(session))
            {
                var stock = em.GetComponentData<SkirmishEnemyStockComponent>(session);
                stock.Materials = payload.EnemyMaterials;
                stock.Oil = payload.EnemyOil;
                stock.Fuel = payload.EnemyFuel;
                em.SetComponentData(session, stock);
            }

            if (em.HasComponent<SkirmishCapacityComponent>(session))
            {
                var capacity = em.GetComponentData<SkirmishCapacityComponent>(session);
                capacity.InfantryLive = payload.PlayerInfantryLive;
                capacity.GroundLive = payload.PlayerGroundLive;
                capacity.SupplyLive = payload.PlayerSupplyLive;
                em.SetComponentData(session, capacity);
            }

            if (em.HasComponent<SkirmishEnemyCapacityComponent>(session))
            {
                var capacity = em.GetComponentData<SkirmishEnemyCapacityComponent>(session);
                capacity.InfantryLive = payload.EnemyInfantryLive;
                capacity.GroundLive = payload.EnemyGroundLive;
                capacity.SupplyLive = payload.EnemySupplyLive;
                em.SetComponentData(session, capacity);
            }

            ReconstructMissing(em, state.SessionId, payload.Actors);
            ApplyActors(em, state.SessionId, payload.Actors);
            ApplyReservations(em, session, payload.Reservations);

            var facts = new SkirmishBaseAssaultFactComponent
            {
                PlayerDesignatedAlive = payload.PlayerDesignatedAlive,
                EnemyDesignatedAlive = payload.EnemyDesignatedAlive
            };
            if (em.HasComponent<SkirmishBaseAssaultFactComponent>(session))
                em.SetComponentData(session, facts);
            else
                em.AddComponentData(session, facts);

            if (payload.Outcome != SkirmishOutcomeKind.None)
            {
                var result = new SkirmishResultComponent
                {
                    Outcome = payload.Outcome,
                    Reason = payload.EndReason,
                    SetupHash = payload.SetupHash,
                    SessionId = state.SessionId,
                    Frozen = 1,
                    SaveAcknowledged = 0
                };
                if (em.HasComponent<SkirmishResultComponent>(session))
                    em.SetComponentData(session, result);
                else
                    em.AddComponentData(session, result);
            }

            if (!Conserved(em, session, payload))
            {
                reason = SkirmishReasonCode.IncompatibleSave;
                return false;
            }

            reason = SkirmishReasonCode.None;
            return true;
        }

        public static bool Conserved(EntityManager em, Entity session, SkirmishCheckpointPayload payload)
        {
            if (payload == null || !em.HasComponent<SkirmishEconomyStockComponent>(session))
                return false;
            var stock = em.GetComponentData<SkirmishEconomyStockComponent>(session);
            if (stock.Materials != payload.PlayerMaterials || stock.Fuel != payload.PlayerFuel)
                return false;
            if (em.HasComponent<SkirmishObjectiveClockComponent>(session))
            {
                var clock = em.GetComponentData<SkirmishObjectiveClockComponent>(session);
                if (clock.Paused != payload.Paused)
                    return false;
            }

            FixedString64Bytes sessionId = em.GetComponentData<SkirmishExpandedSessionComponent>(session).SessionId;
            if (payload.Actors == null)
                return true;
            using var query = em.CreateEntityQuery(typeof(SkirmishAttemptOwnedComponent));
            using NativeArray<Entity> entities = query.ToEntityArray(Allocator.Temp);
            var present = new HashSet<string>();
            for (int i = 0; i < entities.Length; i++)
            {
                var owned = em.GetComponentData<SkirmishAttemptOwnedComponent>(entities[i]);
                if (owned.SessionId.Equals(sessionId))
                    present.Add(owned.StableObjectId.ToString());
            }

            for (int i = 0; i < payload.Actors.Length; i++)
            {
                if (payload.Actors[i] == null)
                    continue;
                if (!present.Contains(payload.Actors[i].StableObjectId ?? string.Empty))
                    return false;
            }

            return true;
        }

        private static void CollectDesignated(
            EntityManager em,
            FixedString64Bytes sessionId,
            SkirmishCheckpointPayload payload)
        {
            using var query = em.CreateEntityQuery(typeof(SkirmishObjectiveRoleComponent), typeof(UnitHealth));
            using NativeArray<Entity> entities = query.ToEntityArray(Allocator.Temp);
            for (int i = 0; i < entities.Length; i++)
            {
                if (!em.HasComponent<SkirmishAttemptOwnedComponent>(entities[i]) ||
                    !em.GetComponentData<SkirmishAttemptOwnedComponent>(entities[i]).SessionId.Equals(sessionId))
                    continue;
                var role = em.GetComponentData<SkirmishObjectiveRoleComponent>(entities[i]);
                bool alive = em.GetComponentData<UnitHealth>(entities[i]).Current > 0;
                if (role.Role == SkirmishObjectiveRoleKind.PlayerBase)
                    payload.PlayerDesignatedAlive = (byte)(alive ? 1 : 0);
                else if (role.Role == SkirmishObjectiveRoleKind.EnemyBase)
                    payload.EnemyDesignatedAlive = (byte)(alive ? 1 : 0);
            }
        }

        private static SkirmishCheckpointActor[] CaptureActors(EntityManager em, FixedString64Bytes sessionId)
        {
            using var query = em.CreateEntityQuery(typeof(SkirmishAttemptOwnedComponent));
            using NativeArray<Entity> entities = query.ToEntityArray(Allocator.Temp);
            var actors = new List<SkirmishCheckpointActor>(entities.Length);
            for (int i = 0; i < entities.Length; i++)
            {
                var owned = em.GetComponentData<SkirmishAttemptOwnedComponent>(entities[i]);
                if (!owned.SessionId.Equals(sessionId))
                    continue;
                var actor = new SkirmishCheckpointActor
                {
                    StableObjectId = owned.StableObjectId.ToString(),
                    FactionId = owned.FactionId,
                    IsStructure = owned.IsStructure
                };
                if (em.HasComponent<SkirmishUnitRoleComponent>(entities[i]))
                    actor.RoleKind = (int)em.GetComponentData<SkirmishUnitRoleComponent>(entities[i]).Role;
                if (em.HasComponent<UnitHealth>(entities[i]))
                {
                    var health = em.GetComponentData<UnitHealth>(entities[i]);
                    actor.CurrentHealth = health.Current;
                    actor.MaxHealth = health.Max;
                }

                if (em.HasComponent<UnitSourcePrefabKey>(entities[i]))
                    actor.PrefabKey = em.GetComponentData<UnitSourcePrefabKey>(entities[i]).Value.ToString();
                actors.Add(actor);
            }

            return actors.ToArray();
        }

        private static SkirmishCheckpointReservation[] CaptureReservations(EntityManager em, Entity session)
        {
            if (!em.HasBuffer<SkirmishProductionReservation>(session))
                return System.Array.Empty<SkirmishCheckpointReservation>();
            DynamicBuffer<SkirmishProductionReservation> buffer = em.GetBuffer<SkirmishProductionReservation>(session);
            var reservations = new SkirmishCheckpointReservation[buffer.Length];
            for (int i = 0; i < buffer.Length; i++)
            {
                SkirmishProductionReservation item = buffer[i];
                reservations[i] = new SkirmishCheckpointReservation
                {
                    ReservationId = item.ReservationId,
                    RoleKind = (int)item.Role,
                    Category = (int)item.Category,
                    MemberCount = item.MemberCount,
                    RemainingMembers = item.RemainingMembers,
                    MaterialsPaid = item.MaterialsPaid,
                    SupplyCost = item.SupplyCost,
                    Phase = (int)item.Phase,
                    FactionId = item.FactionId
                };
            }

            return reservations;
        }

        private static void ReconstructMissing(
            EntityManager em,
            FixedString64Bytes sessionId,
            SkirmishCheckpointActor[] actors)
        {
            if (actors == null)
                return;
            using var query = em.CreateEntityQuery(typeof(SkirmishAttemptOwnedComponent));
            using NativeArray<Entity> entities = query.ToEntityArray(Allocator.Temp);
            var existing = new HashSet<string>();
            for (int i = 0; i < entities.Length; i++)
            {
                var owned = em.GetComponentData<SkirmishAttemptOwnedComponent>(entities[i]);
                if (owned.SessionId.Equals(sessionId))
                    existing.Add(owned.StableObjectId.ToString());
            }

            for (int i = 0; i < actors.Length; i++)
            {
                SkirmishCheckpointActor actor = actors[i];
                if (actor == null || existing.Contains(actor.StableObjectId ?? string.Empty))
                    continue;
                Entity created = em.CreateEntity();
                em.AddComponentData(created, new SkirmishAttemptOwnedComponent
                {
                    SessionId = sessionId,
                    StableObjectId = new FixedString64Bytes(actor.StableObjectId ?? string.Empty),
                    FactionId = actor.FactionId,
                    IsStructure = actor.IsStructure
                });
                em.AddComponentData(created, new Faction { Id = actor.FactionId });
                if (actor.IsStructure == 0)
                {
                    em.AddComponentData(created, new SkirmishUnitRoleComponent
                    {
                        Role = (SkirmishRoleKind)actor.RoleKind,
                        Category = SkirmishRoleIds.Category((SkirmishRoleKind)actor.RoleKind)
                    });
                }

                if (!string.IsNullOrEmpty(actor.PrefabKey))
                    em.AddComponentData(created, new UnitSourcePrefabKey { Value = new FixedString64Bytes(actor.PrefabKey) });
                if (actor.MaxHealth > 0)
                {
                    em.AddComponentData(created, new UnitHealth
                    {
                        Current = actor.CurrentHealth,
                        Max = actor.MaxHealth
                    });
                }
            }
        }

        private static void ApplyActors(EntityManager em, FixedString64Bytes sessionId, SkirmishCheckpointActor[] actors)
        {
            if (actors == null)
                return;
            using var query = em.CreateEntityQuery(typeof(SkirmishAttemptOwnedComponent));
            using NativeArray<Entity> entities = query.ToEntityArray(Allocator.Temp);
            var lookup = new Dictionary<string, Entity>(entities.Length);
            for (int i = 0; i < entities.Length; i++)
            {
                var owned = em.GetComponentData<SkirmishAttemptOwnedComponent>(entities[i]);
                if (owned.SessionId.Equals(sessionId))
                    lookup[owned.StableObjectId.ToString()] = entities[i];
            }

            for (int i = 0; i < actors.Length; i++)
            {
                SkirmishCheckpointActor actor = actors[i];
                if (actor == null || !lookup.TryGetValue(actor.StableObjectId ?? string.Empty, out Entity entity))
                    continue;
                if (em.HasComponent<UnitHealth>(entity))
                {
                    em.SetComponentData(entity, new UnitHealth
                    {
                        Current = actor.CurrentHealth,
                        Max = actor.MaxHealth
                    });
                }
                else if (actor.MaxHealth > 0)
                {
                    em.AddComponentData(entity, new UnitHealth
                    {
                        Current = actor.CurrentHealth,
                        Max = actor.MaxHealth
                    });
                }
            }
        }

        private static void ApplyReservations(
            EntityManager em,
            Entity session,
            SkirmishCheckpointReservation[] reservations)
        {
            if (!em.HasBuffer<SkirmishProductionReservation>(session))
                em.AddBuffer<SkirmishProductionReservation>(session);
            DynamicBuffer<SkirmishProductionReservation> buffer = em.GetBuffer<SkirmishProductionReservation>(session);
            buffer.Clear();
            if (reservations == null)
                return;
            for (int i = 0; i < reservations.Length; i++)
            {
                SkirmishCheckpointReservation item = reservations[i];
                if (item == null)
                    continue;
                buffer.Add(new SkirmishProductionReservation
                {
                    ReservationId = item.ReservationId,
                    Role = (SkirmishRoleKind)item.RoleKind,
                    Category = (SkirmishPopulationCategory)item.Category,
                    MemberCount = item.MemberCount,
                    RemainingMembers = item.RemainingMembers,
                    MaterialsPaid = item.MaterialsPaid,
                    SupplyCost = item.SupplyCost,
                    Phase = (SkirmishReservationPhase)item.Phase,
                    FactionId = item.FactionId
                });
            }
        }

    }
}
