using System;
using System.IO;
using System.Text;
using Game.Configs;
using Game.Skirmish.Contracts;
using UnityEngine;

namespace Game.Runtime
{
    public static class SkirmishCheckpointCodec
    {
        public const int SchemaVersion = 2;

        public static string Encode(SkirmishCheckpointDocument document)
        {
            if (document == null)
                return string.Empty;
            if (document.Header == null)
                document.Header = new SkirmishCheckpointHeader();
            if (document.Payload == null)
                document.Payload = new SkirmishCheckpointPayload();
            document.Header.SchemaVersion = SchemaVersion;
            document.Header.Kind = SkirmishCheckpointSchemaKind.ExpandedV1;
            document.Header.Checksum = string.Empty;
            var wire = WireDocument.From(document);
            string unsigned = JsonUtility.ToJson(wire);
            wire.Checksum = Hash(unsigned);
            document.Header.Checksum = wire.Checksum;
            return JsonUtility.ToJson(wire);
        }

        public static bool TryDecode(string json, out SkirmishCheckpointDocument document, out SkirmishReasonCode reason)
        {
            document = null;
            reason = SkirmishReasonCode.IncompatibleSave;
            if (string.IsNullOrWhiteSpace(json))
                return false;

            WireDocument wire;
            try
            {
                wire = JsonUtility.FromJson<WireDocument>(json);
            }
            catch
            {
                return false;
            }

            if (wire == null || wire.SchemaVersion != SchemaVersion ||
                wire.Kind != (byte)SkirmishCheckpointSchemaKind.ExpandedV1)
                return false;

            string stored = wire.Checksum ?? string.Empty;
            wire.Checksum = string.Empty;
            if (!string.Equals(stored, Hash(JsonUtility.ToJson(wire)), StringComparison.Ordinal))
                return false;

            document = wire.ToDocument();
            reason = SkirmishReasonCode.None;
            return document?.Header != null && document.Payload != null;
        }

        public static bool TryWriteAtomic(string path, SkirmishCheckpointDocument document, out SkirmishReasonCode reason)
        {
            reason = SkirmishReasonCode.IncompatibleSave;
            if (string.IsNullOrWhiteSpace(path) || document == null)
                return false;

            string json = Encode(document);
            if (string.IsNullOrEmpty(json))
                return false;

            string directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(directory))
                Directory.CreateDirectory(directory);

            string temp = path + ".tmp";
            string backup = path + ".bak";
            File.WriteAllText(temp, json, new UTF8Encoding(false));
            using (var stream = new FileStream(temp, FileMode.Open, FileAccess.Read, FileShare.Read))
                stream.Flush(true);

            if (File.Exists(path))
            {
                if (File.Exists(backup))
                    File.Delete(backup);
                File.Replace(temp, path, backup);
            }
            else
                File.Move(temp, path);

            reason = SkirmishReasonCode.None;
            return true;
        }

        public static bool TryRead(string path, out SkirmishCheckpointDocument document, out SkirmishReasonCode reason)
        {
            document = null;
            reason = SkirmishReasonCode.IncompatibleSave;
            if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
                return false;
            return TryDecode(File.ReadAllText(path, new UTF8Encoding(false)), out document, out reason);
        }

        public static bool IsCompatible(SkirmishCheckpointHeader header, SkirmishResolvedSetup setup)
        {
            if (header == null || setup == null)
                return false;
            if (header.SchemaVersion != SchemaVersion || header.Kind != SkirmishCheckpointSchemaKind.ExpandedV1)
                return false;
            if (header.ContentVersion != setup.ContentVersion)
                return false;
            if (header.SetupHash != setup.SetupHash)
                return false;
            return string.Equals(header.CatalogId, setup.CatalogId, StringComparison.Ordinal) &&
                   string.Equals(header.DefinitionId, setup.DefinitionId, StringComparison.Ordinal);
        }

        private static string Hash(string value)
        {
            unchecked
            {
                uint hash = 2166136261;
                if (value != null)
                {
                    for (int i = 0; i < value.Length; i++)
                    {
                        hash ^= value[i];
                        hash *= 16777619;
                    }
                }

                return hash.ToString("X8");
            }
        }

        [Serializable]
        private sealed class WireDocument
        {
            public int SchemaVersion;
            public byte Kind;
            public string SessionId;
            public string CatalogId;
            public string DefinitionId;
            public int ContentVersion;
            public uint SetupHash;
            public int SimulationTick;
            public string Checksum;
            public int Seed;
            public byte SizeId;
            public byte DifficultyId;
            public byte ObjectiveKind;
            public byte Phase;
            public byte IsLegacy;
            public byte IsCustom;
            public float ElapsedSeconds;
            public int DeadlineSeconds;
            public byte Paused;
            public byte Playing;
            public int PlayerMaterials;
            public int PlayerOil;
            public int PlayerFuel;
            public int EnemyMaterials;
            public int EnemyOil;
            public int EnemyFuel;
            public int PlayerInfantryLive;
            public int PlayerGroundLive;
            public int PlayerSupplyLive;
            public int EnemyInfantryLive;
            public int EnemyGroundLive;
            public int EnemySupplyLive;
            public byte PlayerDesignatedAlive;
            public byte EnemyDesignatedAlive;
            public string PlayerBaseObjectId;
            public string EnemyBaseObjectId;
            public byte Outcome;
            public byte EndReason;
            public byte PhysicalSupplyInitialized;
            public SkirmishCheckpointSupplyStore[] SupplyStores;
            public WireActor[] Actors;
            public WireReservation[] Reservations;

            public static WireDocument From(SkirmishCheckpointDocument document)
            {
                SkirmishCheckpointHeader header = document.Header;
                SkirmishCheckpointPayload payload = document.Payload;
                var wire = new WireDocument
                {
                    SchemaVersion = header.SchemaVersion,
                    Kind = (byte)header.Kind,
                    SessionId = header.SessionId ?? string.Empty,
                    CatalogId = header.CatalogId ?? string.Empty,
                    DefinitionId = header.DefinitionId ?? string.Empty,
                    ContentVersion = header.ContentVersion,
                    SetupHash = header.SetupHash,
                    SimulationTick = header.SimulationTick,
                    Checksum = string.Empty,
                    Seed = payload.Seed,
                    SizeId = (byte)payload.SizeId,
                    DifficultyId = (byte)payload.DifficultyId,
                    ObjectiveKind = (byte)payload.ObjectiveKind,
                    Phase = (byte)payload.Phase,
                    IsLegacy = payload.IsLegacy,
                    IsCustom = payload.IsCustom,
                    ElapsedSeconds = payload.ElapsedSeconds,
                    DeadlineSeconds = payload.DeadlineSeconds,
                    Paused = payload.Paused,
                    Playing = payload.Playing,
                    PlayerMaterials = payload.PlayerMaterials,
                    PlayerOil = payload.PlayerOil,
                    PlayerFuel = payload.PlayerFuel,
                    EnemyMaterials = payload.EnemyMaterials,
                    EnemyOil = payload.EnemyOil,
                    EnemyFuel = payload.EnemyFuel,
                    PlayerInfantryLive = payload.PlayerInfantryLive,
                    PlayerGroundLive = payload.PlayerGroundLive,
                    PlayerSupplyLive = payload.PlayerSupplyLive,
                    EnemyInfantryLive = payload.EnemyInfantryLive,
                    EnemyGroundLive = payload.EnemyGroundLive,
                    EnemySupplyLive = payload.EnemySupplyLive,
                    PlayerDesignatedAlive = payload.PlayerDesignatedAlive,
                    EnemyDesignatedAlive = payload.EnemyDesignatedAlive,
                    PlayerBaseObjectId = payload.PlayerBaseObjectId ?? string.Empty,
                    EnemyBaseObjectId = payload.EnemyBaseObjectId ?? string.Empty,
                    Outcome = (byte)payload.Outcome,
                    EndReason = (byte)payload.EndReason,
                    PhysicalSupplyInitialized = payload.PhysicalSupplyInitialized,
                    SupplyStores = payload.SupplyStores,
                    Actors = ToWire(payload.Actors),
                    Reservations = ToWire(payload.Reservations)
                };
                return wire;
            }

            public SkirmishCheckpointDocument ToDocument()
            {
                var header = new SkirmishCheckpointHeader
                {
                    SchemaVersion = SchemaVersion,
                    Kind = (SkirmishCheckpointSchemaKind)Kind,
                    SessionId = SessionId ?? string.Empty,
                    CatalogId = CatalogId ?? string.Empty,
                    DefinitionId = DefinitionId ?? string.Empty,
                    ContentVersion = ContentVersion,
                    SetupHash = SetupHash,
                    SimulationTick = SimulationTick,
                    Checksum = Checksum ?? string.Empty
                };
                var payload = new SkirmishCheckpointPayload
                {
                    SessionId = SessionId ?? string.Empty,
                    CatalogId = CatalogId ?? string.Empty,
                    DefinitionId = DefinitionId ?? string.Empty,
                    ContentVersion = ContentVersion,
                    SetupHash = SetupHash,
                    Seed = Seed,
                    SizeId = (SkirmishSizeId)SizeId,
                    DifficultyId = (SkirmishDifficultyId)DifficultyId,
                    ObjectiveKind = (SkirmishObjectiveKind)ObjectiveKind,
                    Phase = (SkirmishSessionPhase)Phase,
                    IsLegacy = IsLegacy,
                    IsCustom = IsCustom,
                    SimulationTick = SimulationTick,
                    ElapsedSeconds = ElapsedSeconds,
                    DeadlineSeconds = DeadlineSeconds,
                    Paused = Paused,
                    Playing = Playing,
                    PlayerMaterials = PlayerMaterials,
                    PlayerOil = PlayerOil,
                    PlayerFuel = PlayerFuel,
                    EnemyMaterials = EnemyMaterials,
                    EnemyOil = EnemyOil,
                    EnemyFuel = EnemyFuel,
                    PlayerInfantryLive = PlayerInfantryLive,
                    PlayerGroundLive = PlayerGroundLive,
                    PlayerSupplyLive = PlayerSupplyLive,
                    EnemyInfantryLive = EnemyInfantryLive,
                    EnemyGroundLive = EnemyGroundLive,
                    EnemySupplyLive = EnemySupplyLive,
                    PlayerDesignatedAlive = PlayerDesignatedAlive,
                    EnemyDesignatedAlive = EnemyDesignatedAlive,
                    PlayerBaseObjectId = PlayerBaseObjectId ?? string.Empty,
                    EnemyBaseObjectId = EnemyBaseObjectId ?? string.Empty,
                    Outcome = (SkirmishOutcomeKind)Outcome,
                    EndReason = (SkirmishEndReasonKind)EndReason,
                    PhysicalSupplyInitialized = PhysicalSupplyInitialized,
                    SupplyStores = SupplyStores ?? Array.Empty<SkirmishCheckpointSupplyStore>(),
                    Actors = FromWire(Actors),
                    Reservations = FromWire(Reservations)
                };
                return new SkirmishCheckpointDocument { Header = header, Payload = payload };
            }

            private static WireActor[] ToWire(SkirmishCheckpointActor[] actors)
            {
                if (actors == null || actors.Length == 0)
                    return Array.Empty<WireActor>();
                var copy = new WireActor[actors.Length];
                for (int i = 0; i < actors.Length; i++)
                {
                    SkirmishCheckpointActor actor = actors[i] ?? new SkirmishCheckpointActor();
                    copy[i] = new WireActor
                    {
                        StableObjectId = actor.StableObjectId ?? string.Empty,
                        FactionId = actor.FactionId,
                        IsStructure = actor.IsStructure,
                        RoleKind = actor.RoleKind,
                        CurrentHealth = actor.CurrentHealth,
                        MaxHealth = actor.MaxHealth,
                        PrefabKey = actor.PrefabKey ?? string.Empty
                    };
                }

                return copy;
            }

            private static WireReservation[] ToWire(SkirmishCheckpointReservation[] reservations)
            {
                if (reservations == null || reservations.Length == 0)
                    return Array.Empty<WireReservation>();
                var copy = new WireReservation[reservations.Length];
                for (int i = 0; i < reservations.Length; i++)
                {
                    SkirmishCheckpointReservation reservation = reservations[i] ?? new SkirmishCheckpointReservation();
                    copy[i] = new WireReservation
                    {
                        ReservationId = reservation.ReservationId,
                        RoleKind = reservation.RoleKind,
                        Category = reservation.Category,
                        MemberCount = reservation.MemberCount,
                        RemainingMembers = reservation.RemainingMembers,
                        DeliveredMembers = reservation.DeliveredMembers,
                        ProductionGroupId = reservation.ProductionGroupId,
                        ProducerRuntimeId = reservation.ProducerRuntimeId,
                        MaterialsPaid = reservation.MaterialsPaid,
                        SupplyCost = reservation.SupplyCost,
                        Phase = reservation.Phase,
                        FactionId = reservation.FactionId
                    };
                }

                return copy;
            }

            private static SkirmishCheckpointActor[] FromWire(WireActor[] actors)
            {
                if (actors == null || actors.Length == 0)
                    return Array.Empty<SkirmishCheckpointActor>();
                var copy = new SkirmishCheckpointActor[actors.Length];
                for (int i = 0; i < actors.Length; i++)
                {
                    WireActor actor = actors[i] ?? new WireActor();
                    copy[i] = new SkirmishCheckpointActor
                    {
                        StableObjectId = actor.StableObjectId ?? string.Empty,
                        FactionId = actor.FactionId,
                        IsStructure = actor.IsStructure,
                        RoleKind = actor.RoleKind,
                        CurrentHealth = actor.CurrentHealth,
                        MaxHealth = actor.MaxHealth,
                        PrefabKey = actor.PrefabKey ?? string.Empty
                    };
                }

                return copy;
            }

            private static SkirmishCheckpointReservation[] FromWire(WireReservation[] reservations)
            {
                if (reservations == null || reservations.Length == 0)
                    return Array.Empty<SkirmishCheckpointReservation>();
                var copy = new SkirmishCheckpointReservation[reservations.Length];
                for (int i = 0; i < reservations.Length; i++)
                {
                    WireReservation reservation = reservations[i] ?? new WireReservation();
                    copy[i] = new SkirmishCheckpointReservation
                    {
                        ReservationId = reservation.ReservationId,
                        RoleKind = reservation.RoleKind,
                        Category = reservation.Category,
                        MemberCount = reservation.MemberCount,
                        RemainingMembers = reservation.RemainingMembers,
                        DeliveredMembers = reservation.DeliveredMembers,
                        ProductionGroupId = reservation.ProductionGroupId,
                        ProducerRuntimeId = reservation.ProducerRuntimeId,
                        MaterialsPaid = reservation.MaterialsPaid,
                        SupplyCost = reservation.SupplyCost,
                        Phase = reservation.Phase,
                        FactionId = reservation.FactionId
                    };
                }

                return copy;
            }
        }

        [Serializable]
        private sealed class WireActor
        {
            public string StableObjectId;
            public byte FactionId;
            public byte IsStructure;
            public int RoleKind;
            public int CurrentHealth;
            public int MaxHealth;
            public string PrefabKey;
        }

        [Serializable]
        private sealed class WireReservation
        {
            public uint ReservationId;
            public int RoleKind;
            public int Category;
            public int MemberCount;
            public int RemainingMembers;
            public int DeliveredMembers;
            public uint ProductionGroupId;
            public int ProducerRuntimeId;
            public int MaterialsPaid;
            public int SupplyCost;
            public int Phase;
            public byte FactionId;
        }
    }
}
