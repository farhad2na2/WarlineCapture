using System;
using System.Reflection;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;

// Narrow, synchronous engine boundary for the original S073 tactical AI
// systems. It preserves their Execute bodies and mutable buffer/component
// views. It does not reproduce Burst, parallel scheduling, or native World
// scheduling; the source host must call the retained owners in source order.
namespace Unity.Burst.Intrinsics
{
    public struct v128 { }
}

namespace Unity.Jobs
{
    public interface IJob { void Execute(); }

    public static class SourceS073AIJobExtensions
    {
        public static JobHandle Schedule<T>(this T job, JobHandle dependency)
            where T : struct, IJob
        {
            dependency.Complete();
            job.Execute();
            return default;
        }
    }
}

namespace Unity.Collections
{
    public struct NativeReference<T> : IDisposable where T : struct
    {
        sealed class Box { internal T Value; }
        readonly Box box;

        public NativeReference(Allocator allocator) { box = new Box(); }
        public T Value { get => box == null ? throw new InvalidOperationException("Uncreated NativeReference") : box.Value;
            set { if (box == null) throw new InvalidOperationException("Uncreated NativeReference"); box.Value = value; } }
        public bool IsCreated => box != null;
        public void Dispose() { }
    }

    public struct FixedList128Bytes<T> where T : struct
    {
        sealed class Owner { internal readonly System.Collections.Generic.List<T> Values = new(); }
        Owner owner;
        System.Collections.Generic.List<T> Values => owner == null ? null : owner.Values;
        public int Length => Values == null ? 0 : Values.Count;
        public T this[int index] => Values[index];
        public void Add(T value)
        {
            // The only admitted production use is the byte faction set in
            // AICombatOrderSystem. Do not imply general FixedList semantics.
            if (typeof(T) != typeof(byte))
                throw new NotSupportedException("S073 tactical AI fixed-list boundary only admits byte faction ids");
            if (owner == null) owner = new Owner();
            if (owner.Values.Count >= 126)
                throw new InvalidOperationException("S073 fixed-list capacity exceeded");
            owner.Values.Add(value);
        }
    }
}

namespace Unity.Entities
{
    public interface IJobChunk
    {
        void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask,
            in Unity.Burst.Intrinsics.v128 chunkEnabledMask);
    }

    public static class SourceS073AIChunkJobExtensions
    {
        public static JobHandle Schedule<T>(this T job, EntityQuery query, JobHandle dependency)
            where T : struct, IJobChunk
        {
            if (typeof(T).FullName != "Game.Runtime.AITargetingSystem+AssignTargetsJob" ||
                query.Manager == null || !ReferenceEquals(query.Manager, SystemAPI.Manager))
                throw new NotSupportedException("Only the original S073 AITargetingSystem job on its current source manager is admitted");
            dependency.Complete();
            using NativeArray<ArchetypeChunk> chunks = query.ToArchetypeChunkArray(Allocator.TempJob);
            if (chunks.Length > 131072) throw new NotSupportedException("S073 tactical AI chunk bound exceeded");
            MethodInfo execute = typeof(T).GetMethod("Execute", BindingFlags.Instance | BindingFlags.Public);
            if (execute == null) throw new MissingMethodException(typeof(T).FullName, "Execute");
            object boxed = job;
            for (int index = 0; index < chunks.Length; index++)
            {
                try
                {
                    execute.Invoke(boxed, new object[] { chunks[index], index, false, default(Unity.Burst.Intrinsics.v128) });
                }
                catch (TargetInvocationException error)
                {
                    throw error.InnerException ?? error;
                }
            }
            return default;
        }
    }
}

namespace Game.Components
{
    // Compile-time read shape only for CampaignAIBuildEligibility's optional
    // catalog probe. The S073 scheduler rejects any CampaignMissionRuntimeComponent
    // before it calls that owner; it never creates these values or claims the
    // Campaign catalog was loaded.
    public struct CampaignMissionCatalogBlob
    {
        public Unity.Entities.BlobArray<CampaignMissionDefinitionBlob> Missions;
    }
    public struct CampaignMissionDefinitionBlob
    {
        public Unity.Collections.FixedString64Bytes MissionId;
        public byte MissionRuntimeEnabled;
        public CampaignMissionBuildZoneBlob BuildZone;
        public Unity.Entities.BlobArray<CampaignMissionBuildEntryBlob> BuildCatalog;
    }
    public struct CampaignMissionBuildZoneBlob
    {
        public Unity.Collections.FixedString64Bytes AnchorId;
        public int HalfWidthCells;
        public int HalfHeightCells;
    }
    public struct CampaignMissionBuildEntryBlob
    {
        public Unity.Collections.FixedString64Bytes BuildingConfigId;
        public int MaxCount;
    }
    public struct CampaignMissionCatalogComponent : Unity.Entities.IComponentData
    {
        public Unity.Entities.BlobAssetReference<CampaignMissionCatalogBlob> Blob;
        public uint SourceVersion;
        public byte OwnsBlob;
    }
}

namespace Game.Runtime
{
    // Partial declaration allows AISquadSystem's original ordering attribute
    // and the actual production owner source to share the same type. This file
    // provides no production behavior.
    public partial struct AIProductionSystem { }
}
