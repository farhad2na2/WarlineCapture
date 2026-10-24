#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.InteropServices;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Jobs;

namespace Game.Runtime
{
    // Source-pinned read-only adapter for the protected ECB list. No pointer value
    // leaves this method and no producer handle is polled, completed or reset.
    public static class S002InitialWorldEcbAuthority
    {
        public static unsafe Dictionary<string, object> Capture(EntityCommandBufferSystem owner, bool originalStructuralSyncObserved)
        {
            if (owner == null) throw new ArgumentNullException(nameof(owner));
            if (!originalStructuralSyncObserved)
                return new Dictionary<string, object> { ["source_type"] = owner.GetType().FullName,
                    ["supported"] = false, ["reason"] = "Original source structural synchronization is not proven" };
            ref var state = ref owner.World.Unmanaged.ResolveSystemStateRef(owner.SystemHandle);
            var jobField = typeof(SystemState).GetField("m_JobHandle", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            if (jobField == null || jobField.FieldType != typeof(JobHandle))
                throw new InvalidOperationException("Pinned SystemState dependency layout mismatch.");
            // SystemState is a byref-like unmanaged source struct and cannot be
            // boxed. Read only the verified JobHandle field at its runtime layout
            // offset; never invoke Dependency's state-mutating getter.
            int jobOffset = checked((int)Marshal.OffsetOf(typeof(SystemState), "m_JobHandle"));
            if (!typeof(SystemState).IsLayoutSequential || jobOffset < 0 || jobOffset > sizeof(SystemState) - sizeof(JobHandle))
                throw new InvalidOperationException("SystemState dependency field offset is outside the pinned layout.");
            JobHandle dependency;
            fixed (SystemState* pointerToState = &state)
                dependency = *(JobHandle*)((byte*)pointerToState + jobOffset);
            if (!dependency.Equals(default(JobHandle)))
                return new Dictionary<string, object> { ["source_type"] = owner.GetType().FullName,
                    ["supported"] = false, ["reason"] = "ECB system dependency handle exists; no polling/completion permitted" };
            var flags = BindingFlags.Instance | BindingFlags.NonPublic;
            var field = typeof(EntityCommandBufferSystem).GetField("m_PendingBuffers", flags);
            if (field == null || field.FieldType != typeof(UnsafeList<EntityCommandBuffer>).MakePointerType())
                throw new InvalidOperationException("Pinned ECB pending-list layout does not match installed source.");
            var pointer = (UnsafeList<EntityCommandBuffer>*)Pointer.Unbox(field.GetValue(owner));
            var result = new Dictionary<string, object> { ["source_type"] = owner.GetType().FullName,
                ["list_created"] = pointer != null, ["supported"] = false };
            if (pointer == null) { result["reason"] = "ECB owner has no live pending-list storage"; return result; }
            int count = pointer->Length;
            if (count < 0 || count > 65536 || pointer->Capacity < count)
                throw new InvalidOperationException("ECB pending-list bounds are invalid.");
            var producer = typeof(EntityCommandBufferSystem).GetField("m_ProducerHandle", flags);
            if (producer == null || producer.FieldType != typeof(JobHandle))
                throw new InvalidOperationException("Pinned ECB producer-handle layout does not match installed source.");
            bool hasProducer = !((JobHandle)producer.GetValue(owner)).Equals(default(JobHandle));
            result["producer_handle_is_default"] = !hasProducer;
            result["pending_count"] = count;
            result["pending_capacity"] = pointer->Capacity;
            if (hasProducer) { result["reason"] = "Producer handle exists; no completion or IsCompleted poll permitted"; return result; }
            var rows = new List<object>(); bool allEmpty = true;
            for (int i = 0; i < count; i++)
            {
                var buffer = (*pointer)[i]; bool empty = buffer.IsEmpty;
                rows.Add(new Dictionary<string, object> { ["ordinal"] = i, ["is_created"] = buffer.IsCreated, ["is_empty"] = empty });
                allEmpty &= empty;
            }
            result["buffers"] = rows;
            result["supported"] = allEmpty;
            result["reason"] = allEmpty ? "Observed empty pending command domain; future buffers require source constructors" : "Pending commands require an explicit command codec";
            return result;
        }
    }
}
#endif
