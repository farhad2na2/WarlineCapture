using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using Game.Components;
using Game.Configs;
using UnityEngine;

namespace Game.Runtime
{
    // Separate from the released fixture schema. Persisting a native capture does
    // not authorize applying it until the fresh-session reconstruction owner passes.
    internal static class SkirmishNativeCheckpointCodec
    {
        private const int WireVersion = 1;
        private const int MaximumBytes = 32 * 1024 * 1024;
        [Serializable] private sealed class Wire
        {
            public int Version;
            public string Payload;
            public string Checksum;
        }

        internal static bool Validate(SkirmishNativeCheckpointSnapshotService.Snapshot snapshot,
            SkirmishResolvedSetup setup)
        {
            if (!BitConverter.IsLittleEndian || snapshot == null || setup == null || snapshot.Version != SkirmishNativeCheckpointSnapshotService.SchemaVersion ||
                snapshot.Support == null || snapshot.Banks == null || (snapshot.Banks.Length != 0 && snapshot.Banks.Length != 2) ||
                snapshot.Actors == null || snapshot.MapReferences == null || snapshot.DropVisuals == null || snapshot.Actors.Length == 0 ||
                snapshot.Actors.Length > 16384 || snapshot.MapReferences.Length > 65536 ||
                string.IsNullOrEmpty(snapshot.SessionId) || snapshot.SessionId.Length > 63 ||
                snapshot.CatalogId != setup.CatalogId || snapshot.SetupHash != setup.SetupHash ||
                !Equal(snapshot.ContentHash, setup.ContentHash) || !Equal(snapshot.OperationMapId, setup.OperationMapId) ||
                !Equal(snapshot.LayoutId, setup.LayoutId) ||
                double.IsNaN(snapshot.WorldTime) || double.IsInfinity(snapshot.WorldTime) || snapshot.WorldTime < 0 ||
                float.IsNaN(snapshot.UnityTime) || float.IsInfinity(snapshot.UnityTime) || snapshot.UnityTime < 0)
                return false;
            try
            {
                var ids = new HashSet<string>(StringComparer.Ordinal) { "$session" };
                var buildingIds = new HashSet<int>();
                foreach (var bank in snapshot.Banks)
                    if (bank == null || bank.Faction < 1 || bank.Faction > 2 || !ids.Add("bank/" + bank.Faction)) return false;
                foreach (var actor in snapshot.Actors)
                {
                    if (actor == null || string.IsNullOrEmpty(actor.Id) || actor.Id.Length > 63 ||
                        !ids.Add(actor.Id) || string.IsNullOrEmpty(actor.PrefabKey) || actor.IsStructure > 1 ||
                        actor.HasProduction != (actor.IsStructure != 0) ||
                        (actor.HasProduction && (actor.Production == null || actor.OldRuntimeBuildingId <= 0 ||
                         !buildingIds.Add(actor.OldRuntimeBuildingId)))) return false;
                }
                foreach (var bank in snapshot.Banks)
                {
                    if (!SkirmishNativeCheckpointParts.Validate(bank.Parts, ids) ||
                        !SkirmishNativeCheckpointParts.TryReadComponent(bank.Parts, out FactionEconomy economy) ||
                        !SkirmishNativeCheckpointParts.TryReadComponent(bank.Parts, out FactionTacticalMaterialsComponent materials) ||
                        economy.FactionId != bank.Faction || materials.FactionId != bank.Faction || materials.Capacity < 0 ||
                        materials.Current < 0 || materials.Current > materials.Capacity) return false;
                }
                foreach (var map in snapshot.MapReferences)
                {
                    if (map == null || string.IsNullOrEmpty(map.PrefabKey) || map.OldRuntimeBuildingId <= 0 ||
                        !MapIdentityMatches(map) ||
                        !ids.Add(map.Id)) return false;
                }
                var actorKinds = new Dictionary<string, byte>(StringComparer.Ordinal);
                foreach (var actor in snapshot.Actors) actorKinds.Add(actor.Id, actor.IsStructure);
                if (!SkirmishNativeCheckpointTransientState.PublishAliases(snapshot.MissileVisuals, snapshot.MissileStates, ids, actorKinds, snapshot.DropVisuals)) return false;
                if (!SkirmishNativeCheckpointSupportState.PublishAliases(snapshot.Support, snapshot.SessionId, ids)) return false;
                if (!SkirmishNativeCheckpointParts.Validate(snapshot.SessionParts, ids) ||
                    !SkirmishNativeCheckpointParts.TryReadComponent(snapshot.SessionParts, out SkirmishExpandedSessionComponent state) ||
                    state.IsLegacy != 0 || state.SessionId.ToString() != snapshot.SessionId ||
                    state.CatalogId.ToString() != setup.CatalogId || state.DefinitionId.ToString() != setup.DefinitionId ||
                    state.ContentVersion != setup.ContentVersion || state.SetupHash != setup.SetupHash ||
                    state.Seed != setup.Seed || state.SizeId != setup.SizeId || state.DifficultyId != setup.DifficultyId)
                    return false;
                bool sharedMaterials = false;
                foreach (var part in snapshot.SessionParts)
                    if (part.Type == typeof(SkirmishSharedMaterialsInitialized).FullName) sharedMaterials = true;
                if (sharedMaterials != (snapshot.Banks.Length == 2)) return false;
                foreach (var actor in snapshot.Actors)
                {
                    if (!SkirmishNativeCheckpointParts.Validate(actor.Parts, ids) ||
                        !SkirmishNativeCheckpointParts.TryReadComponent(actor.Parts, out SkirmishAttemptOwnedComponent owner) ||
                        owner.StableObjectId.ToString() != actor.Id || owner.SessionId.ToString() != snapshot.SessionId ||
                        owner.IsStructure != actor.IsStructure) return false;
                    bool passenger = SkirmishNativeCheckpointParts.TryReadComponent(actor.Parts, out UnitTransportPassenger _);
                    if (!float.IsFinite(actor.HiddenPassengerScale) || actor.HiddenPassengerScale < 0 ||
                        (passenger ? actor.HiddenPassengerScale <= 0 : actor.HiddenPassengerScale != 0)) return false;
                    if (actor.IsStructure != 0 &&
                        (!SkirmishNativeCheckpointParts.TryReadComponent(actor.Parts, out RuntimeBuildingCombatInfo info) ||
                         info.RuntimeBuildingId != actor.OldRuntimeBuildingId)) return false;
                }
                foreach (var map in snapshot.MapReferences)
                {
                    if (!SkirmishNativeCheckpointParts.Validate(map.Parts, ids) ||
                        !SkirmishNativeCheckpointParts.TryReadComponent(map.Parts, out RuntimeBuildingCombatInfo info) ||
                        info.RuntimeBuildingId != map.OldRuntimeBuildingId || info.OwnerFactionId != map.Faction ||
                        info.OriginCell.x != map.OriginX || info.OriginCell.y != map.OriginZ) return false;
                }
                return true;
            }
            catch (Exception exception) when (exception is ArgumentException || exception is FormatException || exception is OverflowException)
            { return false; }
        }

        private static bool MapIdentityMatches(SkirmishNativeCheckpointSnapshotService.MapReference map)
        {
            if (!SkirmishNativeCheckpointParts.TryReadComponent(map.Parts, out RuntimeBuildingCombatInfo info))
                return false;
            SkirmishNativeCheckpointParts.TryReadComponent(map.Parts, out OperationMapBuildingIdentity source);
            return map.Id == SkirmishNativeCheckpointSnapshotService.SourceMapIdentity(source, info, map.PrefabKey);
        }

        internal static string Encode(SkirmishNativeCheckpointSnapshotService.Snapshot snapshot, SkirmishResolvedSetup setup)
        {
            if (!Validate(snapshot, setup)) return null;
            string payload = JsonUtility.ToJson(snapshot);
            string json = JsonUtility.ToJson(new Wire { Version = WireVersion, Payload = payload, Checksum = Hash(payload) });
            return Encoding.UTF8.GetByteCount(json) <= MaximumBytes ? json : null;
        }

        internal static bool TryDecode(string json, SkirmishResolvedSetup setup,
            out SkirmishNativeCheckpointSnapshotService.Snapshot snapshot)
        {
            snapshot = null;
            if (string.IsNullOrEmpty(json) || Encoding.UTF8.GetByteCount(json) > MaximumBytes) return false;
            try
            {
                var wire = JsonUtility.FromJson<Wire>(json);
                if (wire == null || wire.Version != WireVersion || string.IsNullOrEmpty(wire.Payload) ||
                    !string.Equals(wire.Checksum, Hash(wire.Payload), StringComparison.Ordinal)) return false;
                var decoded = JsonUtility.FromJson<SkirmishNativeCheckpointSnapshotService.Snapshot>(wire.Payload);
                if (!Validate(decoded, setup)) return false;
                snapshot = decoded;
                return true;
            }
            catch (ArgumentException) { return false; }
        }

        internal static bool TryWriteAtomic(string path, SkirmishNativeCheckpointSnapshotService.Snapshot snapshot,
            SkirmishResolvedSetup setup)
        {
            string json = Encode(snapshot, setup);
            if (string.IsNullOrWhiteSpace(path) || json == null) return false;
            string temp = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
            try
            {
                string directory = Path.GetDirectoryName(path);
                if (!string.IsNullOrEmpty(directory)) Directory.CreateDirectory(directory);
                byte[] bytes = new UTF8Encoding(false).GetBytes(json);
                using (var stream = new FileStream(temp, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                { stream.Write(bytes, 0, bytes.Length); stream.Flush(true); }
                if (File.Exists(path)) File.Replace(temp, path, path + ".bak");
                else File.Move(temp, path);
                return true;
            }
            catch (Exception exception) when (exception is IOException || exception is UnauthorizedAccessException || exception is ArgumentException)
            { return false; }
            finally
            {
                try { if (File.Exists(temp)) File.Delete(temp); }
                catch (Exception exception) when (exception is IOException || exception is UnauthorizedAccessException) { }
            }
        }

        // Read-only identity discovery. This never authorizes a launch: the caller
        // must compile current packaged content and run full Validate afterward.
        internal static bool TryReadDescriptor(string path, out SkirmishNativeCheckpointSnapshotService.Snapshot snapshot)
        {
            snapshot = null;
            try
            {
                if (string.IsNullOrWhiteSpace(path) || !File.Exists(path) || new FileInfo(path).Length > MaximumBytes) return false;
                var wire = JsonUtility.FromJson<Wire>(File.ReadAllText(path, Encoding.UTF8));
                if (wire == null || wire.Version != WireVersion || string.IsNullOrEmpty(wire.Payload) ||
                    !string.Equals(wire.Checksum, Hash(wire.Payload), StringComparison.Ordinal)) return false;
                var value = JsonUtility.FromJson<SkirmishNativeCheckpointSnapshotService.Snapshot>(wire.Payload);
                if (value == null || value.Version != SkirmishNativeCheckpointSnapshotService.SchemaVersion ||
                    !SkirmishNativeCheckpointParts.TryReadComponent(value.SessionParts, out SkirmishExpandedSessionComponent _)) return false;
                snapshot = value;
                return true;
            }
            catch (Exception exception) when (exception is IOException || exception is UnauthorizedAccessException || exception is ArgumentException) { return false; }
        }

        internal static bool TryRead(string path, SkirmishResolvedSetup setup,
            out SkirmishNativeCheckpointSnapshotService.Snapshot snapshot)
        {
            snapshot = null;
            try
            {
                if (string.IsNullOrWhiteSpace(path) || !File.Exists(path) || new FileInfo(path).Length > MaximumBytes) return false;
                return TryDecode(File.ReadAllText(path, Encoding.UTF8), setup, out snapshot);
            }
            catch (Exception exception) when (exception is IOException || exception is UnauthorizedAccessException || exception is ArgumentException)
            { return false; }
        }

        private static string Hash(string value)
        {
            using var hash = SHA256.Create();
            return BitConverter.ToString(hash.ComputeHash(Encoding.UTF8.GetBytes(value))).Replace("-", "").ToLowerInvariant();
        }

        private static bool Equal(string first, string second) =>
            string.Equals(first ?? string.Empty, second ?? string.Empty, StringComparison.Ordinal);
    }
}
