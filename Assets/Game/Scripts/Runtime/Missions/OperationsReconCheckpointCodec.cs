using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using Game.Components;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace Game.Runtime
{
    /// <summary>Versioned shared-entity checkpoint. Entity handles never cross the disk boundary.</summary>
    public static class OperationsReconCheckpointCodec
    {
        public const int SchemaVersion = 1;
        [Serializable] public sealed class Image
        {
            public int schema = SchemaVersion;
            public string session, content, unityVersion;
            public string mission, evidence, waves;
            public int frame;
            public double worldTime;
            public int evidenceActor, carrier;
            public Site[] sites;
            public Actor[] actors;
        }
        [Serializable] public sealed class Site
        {
            public string state;
            public int actor;
        }
        [Serializable] public sealed class Actor
        {
            public int index, target;
            public bool exists, disabled, revealed, held;
            public string engage, reserve, patrol;
            public Part[] parts;
        }
        [Serializable] public sealed class Part { public string key, data; }
        [Serializable] private sealed class Envelope { public string payload, sha256; }

        public static string Encode(Image image)
        {
            string payload = JsonUtility.ToJson(image);
            return JsonUtility.ToJson(new Envelope { payload = payload, sha256 = Hash(payload) });
        }
        public static bool TryDecode(string json, string session, string content, out Image image)
        {
            image = null;
            try
            {
                var envelope = JsonUtility.FromJson<Envelope>(json);
                if (envelope == null || string.IsNullOrEmpty(envelope.payload) || Hash(envelope.payload) != envelope.sha256) return false;
                var candidate = JsonUtility.FromJson<Image>(envelope.payload);
                if (candidate == null || candidate.schema != SchemaVersion || candidate.session != session ||
                    candidate.content != content || candidate.unityVersion != Application.unityVersion ||
                    candidate.actors == null || candidate.actors.Length != 36 || candidate.sites == null || candidate.sites.Length != 3) return false;
                Validate(candidate);
                image = candidate;
                return true;
            }
            catch (Exception e) when (e is ArgumentException || e is FormatException || e is InvalidOperationException) { return false; }
        }
        public static Image Capture(EntityManager em, Entity root, string content)
        {
            em.CompleteAllTrackedJobs();
            var mission = em.GetComponentData<OperationsReconMissionComponent>(root);
            var records = em.GetBuffer<OperationsReconSpawnRecord>(root);
            var ids = new Dictionary<Entity, int>();
            foreach (var record in records)
                if (em.Exists(record.Unit) && em.HasComponent<UnitHealth>(record.Unit) && em.HasComponent<LocalTransform>(record.Unit))
                    ids[record.Unit] = record.StableIndex;
            int Id(Entity entity) => entity != Entity.Null && ids.TryGetValue(entity, out int id) ? id : 0;
            var evidence = em.GetComponentData<OperationsReconEvidenceComponent>(root);
            var image = new Image { session = mission.SessionId.ToString(), content = content, unityVersion = Application.unityVersion,
                frame = Time.frameCount, worldTime = em.World.Time.ElapsedTime,
                mission = Pack(mission), evidenceActor = Id(evidence.Actor), carrier = Id(evidence.Carrier),
                waves = Pack(em.GetComponentData<OperationsReconWaveComponent>(root)), actors = new Actor[records.Length] };
            evidence.Actor = evidence.Carrier = Entity.Null;
            image.evidence = Pack(evidence);
            var sites = em.GetBuffer<OperationsReconSiteElement>(root);
            image.sites = new Site[sites.Length];
            for (int i = 0; i < sites.Length; i++)
            { var site = sites[i]; int actor = Id(site.Actor); site.Actor = Entity.Null; image.sites[i] = new Site { state = Pack(site), actor = actor }; }
            for (int i = 0; i < records.Length; i++)
            {
                Entity unit = records[i].Unit;
                var actor = new Actor { index = records[i].StableIndex, exists = em.Exists(unit) && em.HasComponent<UnitHealth>(unit) && em.HasComponent<LocalTransform>(unit) };
                image.actors[i] = actor;
                if (!actor.exists) { actor.parts = Array.Empty<Part>(); continue; }
                actor.disabled = em.HasComponent<Disabled>(unit);
                actor.revealed = em.HasComponent<ScanIntelRevealedTag>(unit);
                actor.held = em.HasComponent<HoldPositionOrderTag>(unit);
                var parts = new List<Part>();
                CaptureParts(em, unit, parts); actor.parts = parts.ToArray();
                if (em.HasComponent<EngageTarget>(unit))
                { var engage = em.GetComponentData<EngageTarget>(unit); actor.target = Id(engage.Target); engage.Target = Entity.Null; actor.engage = Pack(engage); }
                if (em.HasComponent<OperationsReconReserveComponent>(unit))
                { var reserve = em.GetComponentData<OperationsReconReserveComponent>(unit); reserve.Session = Entity.Null; actor.reserve = Pack(reserve); }
                if (em.HasComponent<OperationsReconPatrolComponent>(unit))
                { var patrol = em.GetComponentData<OperationsReconPatrolComponent>(unit); patrol.Session = Entity.Null; actor.patrol = Pack(patrol); }
            }
            Validate(image);
            return image;
        }
        // Apply only after the same validated content has instantiated its complete finite roster.
        public static void Apply(EntityManager em, Entity root, Image image)
        {
            Validate(image);
            if (em.GetComponentData<OperationsReconMissionComponent>(root).SessionId.ToString() != image.session)
                throw new InvalidOperationException("Checkpoint belongs to another attempt.");
            em.CompleteAllTrackedJobs();
            if (!em.HasComponent<OperationsReconEvidenceComponent>(root) || !em.HasComponent<OperationsReconWaveComponent>(root) ||
                !em.HasBuffer<OperationsReconSpawnRecord>(root) || !em.HasBuffer<OperationsReconSiteElement>(root) ||
                !em.HasBuffer<OperationsReconActionElement>(root) || em.GetBuffer<OperationsReconSiteElement>(root).Length != image.sites.Length)
                throw new InvalidOperationException("Checkpoint destination is incomplete.");
            var records = em.GetBuffer<OperationsReconSpawnRecord>(root);
            var entities = new Dictionary<int, Entity>();
            var handles = new HashSet<Entity>();
            foreach (var record in records)
            {
                if (record.StableIndex < 1 || record.StableIndex > 36 || entities.ContainsKey(record.StableIndex) ||
                    !handles.Add(record.Unit) || !em.Exists(record.Unit) || !em.HasComponent<OperationsReconMemberComponent>(record.Unit))
                    throw new InvalidOperationException("Checkpoint actor was not instantiated.");
                var member = em.GetComponentData<OperationsReconMemberComponent>(record.Unit);
                if (member.Session != root || member.StableIndex != record.StableIndex)
                    throw new InvalidOperationException("Checkpoint actor belongs to another roster.");
                entities.Add(record.StableIndex, record.Unit);
            }
            if (entities.Count != image.actors.Length) throw new InvalidOperationException("Checkpoint roster does not match content.");
            Entity Resolve(int id) => id != 0 && entities.TryGetValue(id, out var e) && em.Exists(e) ? e : Entity.Null;
            foreach (var actor in image.actors)
            {
                Entity unit = Resolve(actor.index);
                if (unit == Entity.Null) throw new InvalidOperationException("Checkpoint actor was not instantiated.");
                if (!actor.exists) { em.DestroyEntity(unit); continue; }
                ApplyParts(em, unit, actor.parts);
                if (em.HasComponent<AttackMoveOrder>(unit)) { var order = em.GetComponentData<AttackMoveOrder>(unit); order.RetryAt += em.World.Time.ElapsedTime - image.worldTime; em.SetComponentData(unit, order); }
                if (em.HasComponent<ScanIntelLastSeen>(unit)) { var intel = em.GetComponentData<ScanIntelLastSeen>(unit); intel.LastScanFrame += Time.frameCount - image.frame; em.SetComponentData(unit, intel); }
                if (em.HasComponent<UnitPathRetryCooldown>(unit)) { var retry = em.GetComponentData<UnitPathRetryCooldown>(unit); retry.ResumeFrame += Time.frameCount - image.frame; em.SetComponentData(unit, retry); }
                Toggle<HoldPositionOrderTag>(em, unit, actor.held);
                Toggle<Disabled>(em, unit, actor.disabled); Toggle<ScanIntelRevealedTag>(em, unit, actor.revealed);
                if (!string.IsNullOrEmpty(actor.reserve)) { var reserve = Unpack<OperationsReconReserveComponent>(actor.reserve); reserve.Session = root; Set(em, unit, reserve); }
                if (!string.IsNullOrEmpty(actor.patrol)) { var patrol = Unpack<OperationsReconPatrolComponent>(actor.patrol); patrol.Session = root; Set(em, unit, patrol); }
                // Re-plan the saved destination against the rebuilt shared navigation world.
                if (em.HasComponent<UnitPathFollow>(unit)) em.RemoveComponent<UnitPathFollow>(unit);
                if (em.HasComponent<UnitPathRange>(unit)) em.RemoveComponent<UnitPathRange>(unit);
                if (em.HasComponent<UnitTarget>(unit)) Set(em, unit, new UnitPathRequest { Goal = em.GetComponentData<UnitTarget>(unit).Cell });
                else if (em.HasComponent<UnitPathRequest>(unit)) em.RemoveComponent<UnitPathRequest>(unit);
            }
            foreach (var actor in image.actors)
            {
                Entity unit = Resolve(actor.index);
                if (unit == Entity.Null || string.IsNullOrEmpty(actor.engage)) continue;
                var engage = Unpack<EngageTarget>(actor.engage); engage.Target = Resolve(actor.target); Set(em, unit, engage);
            }
            var mission = Unpack<OperationsReconMissionComponent>(image.mission);
            if (mission.SessionId.ToString() != image.session) throw new InvalidOperationException("Checkpoint identity mismatch.");
            em.SetComponentData(root, mission);
            var evidence = Unpack<OperationsReconEvidenceComponent>(image.evidence);
            evidence.Actor = Resolve(image.evidenceActor); evidence.Carrier = Resolve(image.carrier); em.SetComponentData(root, evidence);
            em.SetComponentData(root, Unpack<OperationsReconWaveComponent>(image.waves));
            var sites = em.GetBuffer<OperationsReconSiteElement>(root);
            for (int i = 0; i < image.sites.Length; i++) { var site = Unpack<OperationsReconSiteElement>(image.sites[i].state); site.Actor = Resolve(image.sites[i].actor); sites[i] = site; }
            em.GetBuffer<OperationsReconActionElement>(root).Clear();
        }
        private static void Validate(Image image)
        {
            if (image == null || image.schema != SchemaVersion || image.unityVersion != Application.unityVersion ||
                string.IsNullOrEmpty(image.session) || string.IsNullOrEmpty(image.content) ||
                !double.IsFinite(image.worldTime) || image.worldTime < 0 || image.frame < 0 ||
                image.actors == null || image.actors.Length != 36 || image.sites == null || image.sites.Length != 3)
                throw new InvalidOperationException("Incomplete checkpoint.");
            var mission = Unpack<OperationsReconMissionComponent>(image.mission);
            if (mission.SessionId.ToString() != image.session ||
                mission.Phase is not (OperationsReconPhase.Playing or OperationsReconPhase.Terminal) ||
                !float.IsFinite(mission.DeadlineSeconds) || mission.DeadlineSeconds <= 0 ||
                !float.IsFinite(mission.ScanSeconds) || mission.ScanSeconds <= 0 ||
                !float.IsFinite(mission.EvidenceSeconds) || mission.EvidenceSeconds <= 0 ||
                mission.CompletedScans < 0 || mission.CompletedScans > 3 ||
                !float.IsFinite(mission.ElapsedSeconds) || mission.ElapsedSeconds < 0 || mission.ElapsedSeconds > mission.DeadlineSeconds)
                throw new InvalidOperationException("Invalid checkpoint mission.");
            var evidence = Unpack<OperationsReconEvidenceComponent>(image.evidence);
            if (evidence.Actor != Entity.Null || evidence.Carrier != Entity.Null || !math.all(math.isfinite(evidence.Position)) ||
                !float.IsFinite(evidence.ChannelSeconds) || evidence.ChannelSeconds < 0 || evidence.ChannelSeconds > mission.EvidenceSeconds)
                throw new InvalidOperationException("Invalid checkpoint evidence.");
            Unpack<OperationsReconWaveComponent>(image.waves);
            var ids = new HashSet<int>();
            foreach (var actor in image.actors)
            {
                if (actor == null || actor.index < 1 || actor.index > 36 || !ids.Add(actor.index) || actor.parts == null)
                    throw new InvalidOperationException("Invalid checkpoint roster.");
                if (!string.IsNullOrEmpty(actor.engage) && Unpack<EngageTarget>(actor.engage).Target != Entity.Null ||
                    !string.IsNullOrEmpty(actor.reserve) && Unpack<OperationsReconReserveComponent>(actor.reserve).Session != Entity.Null ||
                    !string.IsNullOrEmpty(actor.patrol) && Unpack<OperationsReconPatrolComponent>(actor.patrol).Session != Entity.Null)
                    throw new InvalidOperationException("Checkpoint contains an unsaved entity reference.");
                ValidateParts(actor.parts);
                if (actor.exists && (!Array.Exists(actor.parts, p => p.key == nameof(LocalTransform)) ||
                    !Array.Exists(actor.parts, p => p.key == nameof(UnitHealth))))
                    throw new InvalidOperationException("Checkpoint actor is missing required state.");
            }
            bool ValidReference(int id) => id == 0 || id >= 1 && id <= 36 && Array.Exists(image.actors, actor => actor.index == id && actor.exists);
            if (!ValidReference(image.evidenceActor) || !ValidReference(image.carrier)) throw new InvalidOperationException("Invalid evidence actor.");
            foreach (var actor in image.actors)
                if (!ValidReference(actor.target)) throw new InvalidOperationException("Invalid combat target.");
            foreach (var site in image.sites)
            {
                if (site == null || !ValidReference(site.actor)) throw new InvalidOperationException("Invalid checkpoint site.");
                var state = Unpack<OperationsReconSiteElement>(site.state);
                if (state.Actor != Entity.Null || !math.all(math.isfinite(state.Position)) || !float.IsFinite(state.ChannelSeconds) ||
                    state.ChannelSeconds < 0 || state.ChannelSeconds > mission.ScanSeconds)
                    throw new InvalidOperationException("Invalid checkpoint scan.");
            }
        }
        private static string Hash(string value) { using var sha = SHA256.Create(); return Convert.ToBase64String(sha.ComputeHash(Encoding.UTF8.GetBytes(value))); }
        private static string Pack<T>(T value) where T : unmanaged
        {
            var data = new NativeArray<T>(1, Allocator.Temp);
            try { data[0] = value; return Convert.ToBase64String(data.Reinterpret<byte>(UnsafeUtility.SizeOf<T>()).ToArray()); }
            finally { data.Dispose(); }
        }
        private static T Unpack<T>(string value) where T : unmanaged
        {
            byte[] bytes = Convert.FromBase64String(value ?? string.Empty);
            if (bytes.Length != UnsafeUtility.SizeOf<T>()) throw new InvalidOperationException("Incompatible checkpoint component layout.");
            using var data = new NativeArray<byte>(bytes, Allocator.Temp); return data.Reinterpret<T>(1)[0];
        }
        private static void Add<T>(EntityManager em, Entity unit, List<Part> parts) where T : unmanaged, IComponentData
        { if (em.HasComponent<T>(unit)) parts.Add(new Part { key = typeof(T).Name, data = Pack(em.GetComponentData<T>(unit)) }); }
        private static void Set<T>(EntityManager em, Entity unit, T value) where T : unmanaged, IComponentData
        { if (em.HasComponent<T>(unit)) em.SetComponentData(unit, value); else em.AddComponentData(unit, value); }
        private static void Toggle<T>(EntityManager em, Entity unit, bool value) where T : unmanaged, IComponentData
        { if (value && !em.HasComponent<T>(unit)) em.AddComponent<T>(unit); else if (!value && em.HasComponent<T>(unit)) em.RemoveComponent<T>(unit); }
        private static void CaptureParts(EntityManager em, Entity unit, List<Part> parts)
        {
            Add<AttackMoveOrder>(em, unit, parts);
            Add<UnitPathRetryCooldown>(em, unit, parts);
            Add<LocalTransform>(em, unit, parts);
            Add<UnitHealth>(em, unit, parts);
            Add<UnitGrid>(em, unit, parts);
            Add<UnitPrevWorldPos>(em, unit, parts);
            Add<UnitMoveVisualComponent>(em, unit, parts);
            Add<UnitIdleWanderComponent>(em, unit, parts);
            Add<UnitAttackCooldownComponent>(em, unit, parts);
            Add<UnitAttackTraceComponent>(em, unit, parts);
            Add<UnitAttackAnimationComponent>(em, unit, parts);
            Add<UnitDeathAnimationComponent>(em, unit, parts);
            Add<UnitTarget>(em, unit, parts);
            Add<UnitLongDistanceMove>(em, unit, parts);
            Add<UnitCombat>(em, unit, parts);
            Add<UnitMovementBehavior>(em, unit, parts);
            Add<UnitRotationHold>(em, unit, parts);
            Add<ScanIntelLastSeen>(em, unit, parts);
        }
        private static void ValidateParts(Part[] parts)
        {
            var keys = new HashSet<string>();
            foreach (var part in parts)
            {
                if (part == null || !keys.Add(part.key)) throw new InvalidOperationException("Invalid checkpoint components.");
                switch (part.key)
                {
                    case nameof(AttackMoveOrder): Unpack<AttackMoveOrder>(part.data); break;
                    case nameof(UnitPathRetryCooldown): Unpack<UnitPathRetryCooldown>(part.data); break;
                    case nameof(LocalTransform):
                        var transform = Unpack<LocalTransform>(part.data);
                        if (!math.all(math.isfinite(transform.Position)) || !math.all(math.isfinite(transform.Rotation.value)) ||
                            !float.IsFinite(transform.Scale) || transform.Scale <= 0)
                            throw new InvalidOperationException("Invalid checkpoint transform.");
                        break;
                    case nameof(UnitHealth):
                        var health = Unpack<UnitHealth>(part.data);
                        if (health.Max <= 0 || health.Current > health.Max) throw new InvalidOperationException("Invalid checkpoint health.");
                        break;
                    case nameof(UnitGrid): Unpack<UnitGrid>(part.data); break;
                    case nameof(UnitPrevWorldPos): Unpack<UnitPrevWorldPos>(part.data); break;
                    case nameof(UnitMoveVisualComponent): Unpack<UnitMoveVisualComponent>(part.data); break;
                    case nameof(UnitIdleWanderComponent): Unpack<UnitIdleWanderComponent>(part.data); break;
                    case nameof(UnitAttackCooldownComponent): Unpack<UnitAttackCooldownComponent>(part.data); break;
                    case nameof(UnitAttackTraceComponent): Unpack<UnitAttackTraceComponent>(part.data); break;
                    case nameof(UnitAttackAnimationComponent): Unpack<UnitAttackAnimationComponent>(part.data); break;
                    case nameof(UnitDeathAnimationComponent): Unpack<UnitDeathAnimationComponent>(part.data); break;
                    case nameof(UnitTarget): Unpack<UnitTarget>(part.data); break;
                    case nameof(UnitLongDistanceMove): Unpack<UnitLongDistanceMove>(part.data); break;
                    case nameof(UnitCombat): Unpack<UnitCombat>(part.data); break;
                    case nameof(UnitMovementBehavior): Unpack<UnitMovementBehavior>(part.data); break;
                    case nameof(UnitRotationHold): Unpack<UnitRotationHold>(part.data); break;
                    case nameof(ScanIntelLastSeen): Unpack<ScanIntelLastSeen>(part.data); break;
                    default: throw new InvalidOperationException("Unknown checkpoint component.");
                }
            }
        }
        private static void ApplyParts(EntityManager em, Entity unit, Part[] parts)
        {
            var values = new Dictionary<string, string>();
            foreach (var part in parts) values.Add(part.key, part.data);
            Restore<AttackMoveOrder>(em, unit, values);
            Restore<UnitPathRetryCooldown>(em, unit, values);
            Restore<LocalTransform>(em, unit, values);
            Restore<UnitHealth>(em, unit, values);
            Restore<UnitGrid>(em, unit, values);
            Restore<UnitPrevWorldPos>(em, unit, values);
            Restore<UnitMoveVisualComponent>(em, unit, values);
            Restore<UnitIdleWanderComponent>(em, unit, values);
            Restore<UnitAttackCooldownComponent>(em, unit, values);
            Restore<UnitAttackTraceComponent>(em, unit, values);
            Restore<UnitAttackAnimationComponent>(em, unit, values);
            Restore<UnitDeathAnimationComponent>(em, unit, values);
            Restore<UnitTarget>(em, unit, values);
            Restore<UnitLongDistanceMove>(em, unit, values);
            Restore<UnitCombat>(em, unit, values);
            Restore<UnitMovementBehavior>(em, unit, values);
            Restore<UnitRotationHold>(em, unit, values);
            Restore<ScanIntelLastSeen>(em, unit, values);
        }
        private static void Restore<T>(EntityManager em, Entity unit, Dictionary<string, string> values) where T : unmanaged, IComponentData
        { if (values.TryGetValue(typeof(T).Name, out string data)) Set(em, unit, Unpack<T>(data)); else if (em.HasComponent<T>(unit)) em.RemoveComponent<T>(unit); }
    }
}
