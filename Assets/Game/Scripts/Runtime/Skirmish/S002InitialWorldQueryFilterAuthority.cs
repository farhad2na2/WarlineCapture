#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.InteropServices;
using Unity.Entities;

namespace Game.Runtime
{
    // Read-only adapter for the source-pinned mutable query filter. It never
    // infers RequiredChangeVersion from the owning system's LastSystemVersion.
    public static class S002InitialWorldQueryFilterAuthority
    {
        public static unsafe bool BelongsToWorld(EntityQuery query, World world)
        {
            if (world == null || !world.IsCreated || query.Equals(default(EntityQuery))) return false;
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;
            var get = typeof(EntityQuery).GetMethod("_GetImpl", flags, null, Type.EmptyTypes, null);
            var access = typeof(EntityManager).GetMethod("GetCheckedEntityDataAccess", flags, null, Type.EmptyTypes, null);
            if (get == null || !get.ReturnType.IsPointer || access == null || !access.ReturnType.IsPointer)
                throw new InvalidOperationException("Pinned query/EntityManager owner accessors missing.");
            Type impl = get.ReturnType.GetElementType();
            if (!impl.IsLayoutSequential || (int)Marshal.OffsetOf(impl, "_Access") != 0 ||
                !impl.GetField("_Access", flags).FieldType.IsPointer)
                throw new InvalidOperationException("Pinned query owner-prefix layout mismatch.");
            byte* pointer = (byte*)Pointer.Unbox(get.Invoke(query, Array.Empty<object>()));
            if (pointer == null) return false;
            void* queryOwner = *(void**)pointer;
            void* expectedOwner = Pointer.Unbox(access.Invoke(world.EntityManager, Array.Empty<object>()));
            return queryOwner == expectedOwner;
        }

        public static unsafe Dictionary<string, object> Capture(EntityQuery query)
        {
            if (query.Equals(default(EntityQuery))) throw new InvalidOperationException("An actual query is required.");
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;
            var get = typeof(EntityQuery).GetMethod("_GetImpl", flags, null, Type.EmptyTypes, null);
            var indexMethod = typeof(EntityQuery).GetMethod("GetIndexInEntityQuery", flags, null, new[] { typeof(TypeIndex) }, null);
            if (get == null || !get.ReturnType.IsPointer || indexMethod == null)
                throw new InvalidOperationException("Pinned query accessor signature mismatch.");
            Type impl = get.ReturnType.GetElementType();
            var filterField = impl.GetField("_Filter", flags);
            if (filterField == null || !impl.IsLayoutSequential)
                throw new InvalidOperationException("Pinned query filter owner layout mismatch.");
            int filterOffset = checked((int)Marshal.OffsetOf(impl, "_Filter"));
            // Installed source has two pointer fields immediately before _Filter;
            // checking both names/types avoids accepting an unrelated later layout.
            if (filterOffset != 2 * IntPtr.Size || !impl.GetField("_Access", flags).FieldType.IsPointer ||
                !impl.GetField("_QueryData", flags).FieldType.IsPointer)
                throw new InvalidOperationException("Pinned query filter prefix layout mismatch.");
            Type filter = filterField.FieldType;
            int size = Marshal.SizeOf(filter);
            if (!filter.IsLayoutSequential || size != 40)
                throw new InvalidOperationException("Pinned query filter size mismatch.");
            int versionOffset = Offset(filter, "RequiredChangeVersion", 0);
            int sharedOffset = Offset(filter, "Shared", 4);
            int changedOffset = Offset(filter, "Changed", 24);
            int orderOffset = Offset(filter, "_UseOrderFiltering", 36);
            Type sharedType = filter.GetField("Shared", flags).FieldType;
            Type changedType = filter.GetField("Changed", flags).FieldType;
            Offset(sharedType, "Count", 0); Offset(sharedType, "IndexInEntityQuery", 4); Offset(sharedType, "SharedComponentIndex", 12);
            Offset(changedType, "Count", 0); Offset(changedType, "IndexInEntityQuery", 4);
            byte* pointer = (byte*)Pointer.Unbox(get.Invoke(query, Array.Empty<object>()));
            if (pointer == null) throw new InvalidOperationException("Query implementation is absent.");
            var bytes = new byte[size]; Marshal.Copy((IntPtr)(pointer + filterOffset), bytes, 0, size);
            uint required = BitConverter.ToUInt32(bytes, versionOffset), order = BitConverter.ToUInt32(bytes, orderOffset);
            int sharedCount = BitConverter.ToInt32(bytes, sharedOffset), changedCount = BitConverter.ToInt32(bytes, changedOffset);
            if (sharedCount < 0 || sharedCount > 2 || changedCount < 0 || changedCount > 2 || order > 1)
                throw new InvalidOperationException("Mutable query filter exceeds the source layout bounds.");
            var types = new Dictionary<int, Type>();
            foreach (var description in query.GetEntityQueryDescs())
                foreach (var array in new[] { description.All, description.Disabled, description.Present })
                    foreach (var component in array)
                    {
                        int index;
                        try { index = (int)indexMethod.Invoke(query, new object[] { component.TypeIndex }); }
                        catch (TargetInvocationException error) when (error.InnerException is InvalidOperationException) { continue; }
                        if (index < 0) continue;
                        Type type = TypeManager.GetType(component.TypeIndex);
                        if (types.TryGetValue(index, out Type existing) && existing != type)
                            throw new InvalidOperationException("Query filter index maps to multiple source types.");
                        types[index] = type;
                    }
            var changed = new List<object>(); var shared = new List<object>();
            for (int i = 0; i < changedCount; i++)
            {
                int index = BitConverter.ToInt32(bytes, changedOffset + 4 + 4 * i);
                if (!types.TryGetValue(index, out Type type)) throw new InvalidOperationException("Changed filter source type unresolved.");
                changed.Add(new Dictionary<string, object> { ["query_index"] = index, ["source_type"] = type.FullName });
            }
            for (int i = 0; i < sharedCount; i++)
            {
                int index = BitConverter.ToInt32(bytes, sharedOffset + 4 + 4 * i);
                if (!types.TryGetValue(index, out Type type)) throw new InvalidOperationException("Shared filter source type unresolved.");
                shared.Add(new Dictionary<string, object> { ["query_index"] = index, ["source_type"] = type.FullName,
                    ["shared_component_index"] = BitConverter.ToInt32(bytes, sharedOffset + 12 + 4 * i) });
            }
            bool hasFilter = sharedCount != 0 || changedCount != 0 || order != 0;
            if (query.HasFilter() != hasFilter) throw new InvalidOperationException("Query filter changed across inspection.");
            for (int i = 0; i < size; i++) if (bytes[i] != pointer[filterOffset + i])
                throw new InvalidOperationException("Query filter mutated during capture.");
            return new Dictionary<string, object>
            {
                ["supported"] = sharedCount == 0,
                ["has_filter"] = hasFilter,
                ["required_change_version"] = required,
                ["use_order_filtering"] = order != 0,
                ["changed"] = changed,
                ["shared"] = shared,
                ["shared_values_rebind_required"] = sharedCount != 0,
                ["source_codec"] = "unity-entities-query-filter-ef040875dea9-v1"
            };
        }
        static int Offset(Type type, string field, int expected)
        {
            int actual = checked((int)Marshal.OffsetOf(type, field));
            if (!type.IsLayoutSequential || actual != expected)
                throw new InvalidOperationException("Pinned query filter field layout mismatch: " + field);
            return actual;
        }
    }
}
#endif
