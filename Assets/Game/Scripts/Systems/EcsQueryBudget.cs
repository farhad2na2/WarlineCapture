using System;
using System.Collections.Generic;
using System.Reflection;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using UnityEngine;

namespace Game.Runtime
{
    /// <summary>
    /// Reads and widens the per-World EntityQueryManager block allocator.
    ///
    /// Entities 6.5 allocates every cached query shape and every (query shape, matching archetype) pair from one
    /// BlockAllocator created with a hard 16 MiB budget ("16MB should be enough", EntityQueryManager.Create). Nothing is
    /// freed until the World is disposed, archetypes are never destroyed, and the default World lives for the whole app
    /// session (menu and every match). When the budget is spent, the next structural change that creates an archetype
    /// throws "Cannot exceed budget of 16777216 in BlockAllocator" half-way through EndStructuralChanges, which leaves query
    /// chunk caches inconsistent (owner S026 playtest 2026-11-17, lane-8 S050 queue).
    ///
    /// <see cref="TryEnsureHeadroom"/> swaps that allocator for an identical BlockAllocator with a larger budget while the
    /// World is being created. Existing query data stays in the old allocator's blocks, which are kept alive (never freed)
    /// until the World is gone, so no pointer changes. Blocks are allocated lazily (64 KiB each), so the larger budget costs
    /// nothing until used. Internal fields are located by reflection; any mismatch leaves the World untouched.
    /// </summary>
    public static unsafe class EcsQueryBudget
    {
        public const int HeadroomBudgetBytes = 128 * 1024 * 1024;

        public struct Snapshot
        {
            public bool Valid;
            public int Archetypes, QueryShapes, UsedBlocks, BudgetBlocks;
            public bool HeadroomApplied;
        }

        private sealed class Layout
        {
            public FieldInfo EntityDataAccess;      // EntityManager.m_EntityDataAccess (EntityDataAccess*)
            public int QueryManagerOffset;          // EntityDataAccess.m_EntityQueryManager
            public int AllocatorOffset;             // EntityQueryManager.m_EntityQueryDataChunkAllocator
            public int QueryDatasLengthOffset;      // EntityQueryManager.m_EntityQueryDatas.m_length
            public int CurrentBlockOffset;          // BlockAllocator.m_currentBlockIndex
            public int AllocationsLengthOffset;     // BlockAllocator.m_allocations.m_length (= budget blocks)
            public Type AllocatorType;
            public int AllocatorSize;
            public ConstructorInfo AllocatorCtor;   // BlockAllocator(AllocatorManager.AllocatorHandle, int)
            public MethodInfo AllocatorDispose;
        }

        private static Layout s_layout;
        private static string s_layoutError;
        private static int s_boxHeader = -1;
        private static readonly Dictionary<World, IntPtr> s_retired = new();
        private static readonly HashSet<World> s_widened = new();
        private static readonly Dictionary<World, int> s_retiredBlocks = new();

        public static string LayoutError => s_layoutError;
        public static bool IsHeadroomApplied(World world) => world != null && s_widened.Contains(world);

        public static bool TryRead(World world, out Snapshot snapshot)
        {
            snapshot = default;
            if (world == null || !world.IsCreated) return false;
            var em = world.EntityManager;
            using (var archetypes = new NativeList<EntityArchetype>(Allocator.Temp))
            {
                em.GetAllArchetypes(archetypes);
                snapshot.Archetypes = archetypes.Length;
            }
            snapshot.HeadroomApplied = s_widened.Contains(world);
            byte* manager = QueryManager(em);
            if (manager == null) { snapshot.QueryShapes = snapshot.UsedBlocks = snapshot.BudgetBlocks = -1; return true; }
            var layout = s_layout;
            snapshot.QueryShapes = *(int*)(manager + layout.QueryDatasLengthOffset);
            byte* allocator = manager + layout.AllocatorOffset;
            // Blocks are taken in index order and never freed. Blocks of the retired (pre-headroom) allocator still hold
            // live query data, so they count as used.
            snapshot.UsedBlocks = *(int*)(allocator + layout.CurrentBlockOffset) + 1 +
                (s_retiredBlocks.TryGetValue(world, out int retiredBlocks) ? retiredBlocks : 0);
            snapshot.BudgetBlocks = *(int*)(allocator + layout.AllocationsLengthOffset);
            snapshot.Valid = true;
            return true;
        }

        /// <summary>Call once while the World is created (before gameplay). Returns true when the budget is now larger.</summary>
        public static bool TryEnsureHeadroom(World world, int budgetBytes, out string detail)
        {
            detail = null;
            ReleaseRetiredAllocators();
            if (world == null || !world.IsCreated) { detail = "worldMissing"; return false; }
            if (s_widened.Contains(world)) { detail = "alreadyApplied"; return true; }
            byte* manager = QueryManager(world.EntityManager);
            if (manager == null) { detail = "layout:" + s_layoutError; return false; }
            var layout = s_layout;
            byte* allocator = manager + layout.AllocatorOffset;
            int budgetBlocks = *(int*)(allocator + layout.AllocationsLengthOffset);
            if ((long)budgetBlocks * EcsQueryBudgetMath.BlockBytes >= budgetBytes) { detail = "budgetAlreadyLarge"; return false; }
            int header = BoxHeader();
            if (header < 0) { detail = "boxHeaderUnknown"; return false; }

            object fresh;
            try { fresh = layout.AllocatorCtor.Invoke(new object[] { AllocatorManager.Persistent, budgetBytes }); }
            catch (Exception error) { detail = "ctor:" + error.GetType().Name; return false; }

            // Keep the old allocator's bytes: its blocks back every query created so far and are released only after the
            // World (and so the EntityQueryManager that reads them in Dispose) is gone.
            IntPtr retired = (IntPtr)UnsafeUtility.Malloc(layout.AllocatorSize, 16, Allocator.Persistent);
            UnsafeUtility.MemCpy((void*)retired, allocator, layout.AllocatorSize);
            byte* data = (byte*)UnsafeUtility.PinGCObjectAndGetAddress(fresh, out ulong handle);
            try { UnsafeUtility.MemCpy(allocator, data + header, layout.AllocatorSize); }
            finally { UnsafeUtility.ReleaseGCObject(handle); }
            s_retiredBlocks[world] = *(int*)((byte*)retired + layout.CurrentBlockOffset) + 1;
            s_retired[world] = retired;
            s_widened.Add(world);
            detail = "budgetBlocks=" + budgetBlocks + "->" + *(int*)(allocator + layout.AllocationsLengthOffset);
            return true;
        }

        /// <summary>Releases kept allocators of Worlds that no longer exist.</summary>
        public static void ReleaseRetiredAllocators()
        {
            if (s_retired.Count == 0 || s_layout == null) return;
            var gone = new List<World>();
            foreach (var entry in s_retired)
                if (entry.Key == null || !entry.Key.IsCreated) gone.Add(entry.Key);
            foreach (var world in gone)
            {
                IntPtr bytes = s_retired[world];
                s_retired.Remove(world);
                s_widened.Remove(world);
                s_retiredBlocks.Remove(world);
                try
                {
                    object boxed = Activator.CreateInstance(s_layout.AllocatorType);
                    byte* data = (byte*)UnsafeUtility.PinGCObjectAndGetAddress(boxed, out ulong handle);
                    try { UnsafeUtility.MemCpy(data + BoxHeader(), (void*)bytes, s_layout.AllocatorSize); }
                    finally { UnsafeUtility.ReleaseGCObject(handle); }
                    s_layout.AllocatorDispose.Invoke(boxed, null);
                }
                catch (Exception error) { Debug.LogWarning("[EcsQueryBudget] retiredReleaseFailed=" + error.GetType().Name); }
                UnsafeUtility.Free((void*)bytes, Allocator.Persistent);
            }
        }

        private static byte* QueryManager(EntityManager em)
        {
            var layout = ResolveLayout();
            if (layout == null) return null;
            object boxed = em;
            object pointer = layout.EntityDataAccess.GetValue(boxed);
            byte* access = pointer is Pointer p ? (byte*)Pointer.Unbox(p) : null;
            return access == null ? null : access + layout.QueryManagerOffset;
        }

        private static Layout ResolveLayout()
        {
            if (s_layout != null || s_layoutError != null) return s_layout;
            try
            {
                const BindingFlags Instance = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
                Assembly entities = typeof(EntityManager).Assembly;
                var layout = new Layout();
                layout.EntityDataAccess = Require(typeof(EntityManager).GetField("m_EntityDataAccess", Instance), "EntityManager.m_EntityDataAccess");
                Type access = Require(entities.GetType("Unity.Entities.EntityDataAccess"), "EntityDataAccess");
                Type manager = Require(entities.GetType("Unity.Entities.EntityQueryManager"), "EntityQueryManager");
                layout.AllocatorType = Require(entities.GetType("Unity.Entities.BlockAllocator"), "BlockAllocator");
                layout.QueryManagerOffset = UnsafeUtility.GetFieldOffset(Require(access.GetField("m_EntityQueryManager", Instance), "m_EntityQueryManager"));
                FieldInfo allocatorField = Require(manager.GetField("m_EntityQueryDataChunkAllocator", Instance), "m_EntityQueryDataChunkAllocator");
                if (allocatorField.FieldType != layout.AllocatorType) throw new MissingMemberException("allocatorType");
                layout.AllocatorOffset = UnsafeUtility.GetFieldOffset(allocatorField);
                FieldInfo datas = Require(manager.GetField("m_EntityQueryDatas", Instance), "m_EntityQueryDatas");
                layout.QueryDatasLengthOffset = UnsafeUtility.GetFieldOffset(datas) +
                    UnsafeUtility.GetFieldOffset(Require(datas.FieldType.GetField("m_length", Instance), "UnsafePtrList.m_length"));
                layout.CurrentBlockOffset = UnsafeUtility.GetFieldOffset(Require(layout.AllocatorType.GetField("m_currentBlockIndex", Instance), "m_currentBlockIndex"));
                FieldInfo allocations = Require(layout.AllocatorType.GetField("m_allocations", Instance), "m_allocations");
                layout.AllocationsLengthOffset = UnsafeUtility.GetFieldOffset(allocations) +
                    UnsafeUtility.GetFieldOffset(Require(allocations.FieldType.GetField("m_length", Instance), "UnsafeList.m_length"));
                layout.AllocatorSize = UnsafeUtility.SizeOf(layout.AllocatorType);
                layout.AllocatorCtor = Require(layout.AllocatorType.GetConstructor(Instance, null,
                    new[] { typeof(AllocatorManager.AllocatorHandle), typeof(int) }, null), "BlockAllocator(handle,int)");
                layout.AllocatorDispose = Require(layout.AllocatorType.GetMethod("Dispose", Instance, null, Type.EmptyTypes, null), "BlockAllocator.Dispose");
                s_layout = layout;
            }
            catch (Exception error)
            {
                s_layoutError = error.GetType().Name + ":" + error.Message;
                Debug.LogWarning("[EcsQueryBudget] layout=Unavailable reason=" + s_layoutError);
            }
            return s_layout;
        }

        private static T Require<T>(T value, string name) where T : class =>
            value ?? throw new MissingMemberException(name);

        // Offset of a boxed value type's payload from the pinned object address (two pointer-sized words on Mono and
        // IL2CPP). Measured, not assumed: an unknown runtime disables the swap.
        private static int BoxHeader()
        {
            if (s_boxHeader >= 0) return s_boxHeader;
            const long Marker = 0x5EC7A11E0BADF00DL;
            object probe = Marker;
            byte* address = (byte*)UnsafeUtility.PinGCObjectAndGetAddress(probe, out ulong handle);
            try
            {
                for (int offset = 0; offset <= 64; offset += IntPtr.Size)
                    if (*(long*)(address + offset) == Marker) { s_boxHeader = offset; break; }
            }
            finally { UnsafeUtility.ReleaseGCObject(handle); }
            return s_boxHeader;
        }

        /// <summary>Toggle-dimension census of the World's archetypes (see <see cref="EcsArchetypeToggleCensus"/>).</summary>
        public static string ToggleReport(World world, int top)
        {
            if (world == null || !world.IsCreated) return "worldMissing";
            var lists = new List<int[]>();
            using (var archetypes = new NativeList<EntityArchetype>(Allocator.Temp))
            {
                world.EntityManager.GetAllArchetypes(archetypes);
                for (int i = 0; i < archetypes.Length; i++)
                {
                    using var types = archetypes[i].GetComponentTypes(Allocator.Temp);
                    var indices = new int[types.Length];
                    for (int t = 0; t < types.Length; t++) indices[t] = types[t].TypeIndex.Value;
                    lists.Add(indices);
                }
            }
            var rows = EcsArchetypeToggleCensus.Rank(lists, top);
            return EcsArchetypeToggleCensus.Format(rows, index =>
            {
                var type = TypeManager.GetType(new TypeIndex { Value = index });
                return type != null ? type.Name : null;
            });
        }
    }
}
