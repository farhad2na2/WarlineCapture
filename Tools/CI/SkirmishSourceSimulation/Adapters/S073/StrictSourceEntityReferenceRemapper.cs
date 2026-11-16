using System;
using System.Collections.Generic;
using System.Reflection;
using Unity.Entities;

namespace Game.Runtime
{
    /// <summary>
    /// Remaps Entity fields in value-type ECS data against a complete, validated
    /// source-to-target entity map. This does not clone storage or entities.
    /// </summary>
    internal static class StrictSourceEntityReferenceRemapper
    {
        internal static T Remap<T>(T value, IReadOnlyDictionary<Entity, Entity> entityMap,
            Func<Entity, bool> isSourceEntity, Func<Entity, bool> isTargetEntity)
        {
            if (entityMap == null) throw new ArgumentNullException(nameof(entityMap));
            if (isSourceEntity == null) throw new ArgumentNullException(nameof(isSourceEntity));
            if (isTargetEntity == null) throw new ArgumentNullException(nameof(isTargetEntity));
            ValidateMap(entityMap, isSourceEntity, isTargetEntity);
            object boxed = value;
            object result = RemapValue(boxed, typeof(T), entityMap, isSourceEntity, isTargetEntity, out _);
            return (T)result;
        }

        private static void ValidateMap(IReadOnlyDictionary<Entity, Entity> entityMap,
            Func<Entity, bool> isSourceEntity, Func<Entity, bool> isTargetEntity)
        {
            var targets = new HashSet<Entity>();
            foreach (var pair in entityMap)
            {
                if (pair.Key == Entity.Null || !isSourceEntity(pair.Key))
                    throw new InvalidOperationException("Entity map key is outside the source domain");
                if (pair.Value == Entity.Null || !isTargetEntity(pair.Value))
                    throw new InvalidOperationException("Entity map value is outside the target domain");
                if (!targets.Add(pair.Value))
                    throw new InvalidOperationException("Entity map must be one-to-one");
            }
        }

        private static object RemapValue(object value, Type type,
            IReadOnlyDictionary<Entity, Entity> entityMap,
            Func<Entity, bool> isSourceEntity, Func<Entity, bool> isTargetEntity, out bool changed)
        {
            changed = false;
            if (type == typeof(Entity))
            {
                var source = (Entity)value;
                if (source == Entity.Null) return source;
                if (!isSourceEntity(source))
                    throw new InvalidOperationException("Entity reference is outside the source domain");
                if (!entityMap.TryGetValue(source, out Entity target))
                    throw new InvalidOperationException("Entity reference has no target mapping");
                if (target == Entity.Null || !isTargetEntity(target))
                    throw new InvalidOperationException("Mapped entity is outside the target domain");
                changed = source != target;
                return target;
            }

            if (type.IsPrimitive || type.IsEnum || type == typeof(decimal)) return value;
            if (!type.IsValueType || type.IsPointer || type.IsByRef || type.IsArray)
                throw new NotSupportedException("Only value-type ECS data with Entity references is supported: " + type.FullName);

            object boxed = value;
            foreach (FieldInfo field in type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
            {
                if (field.IsStatic) continue;
                object oldField = field.GetValue(boxed);
                object newField = RemapValue(oldField, field.FieldType, entityMap, isSourceEntity, isTargetEntity, out bool fieldChanged);
                if (fieldChanged)
                {
                    if (field.IsInitOnly)
                        throw new NotSupportedException("Readonly field containing remappable data: " + type.FullName + "." + field.Name);
                    field.SetValue(boxed, newField);
                    changed = true;
                }
            }
            return boxed;
        }
    }
}
