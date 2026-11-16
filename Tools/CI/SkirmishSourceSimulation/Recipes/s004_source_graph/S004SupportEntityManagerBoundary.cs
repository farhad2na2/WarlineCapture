using System;
using System.Linq;
using Unity.Collections;

// Diagnostic source-host subset. Canonical bank rows need a separate complete
// component-domain authority and deliberately remain unsupported here.
namespace Unity.Entities
{
    public partial struct ComponentType
    {
        public static bool operator ==(ComponentType left, ComponentType right) => left.Type == right.Type && left.Excluded == right.Excluded;
        public static bool operator !=(ComponentType left, ComponentType right) => !(left == right);
        public override bool Equals(object value) => value is ComponentType other && this == other;
        public override int GetHashCode() => (Type?.GetHashCode() ?? 0) ^ Excluded.GetHashCode();
    }

    public sealed partial class EntityManager
    {
        public NativeArray<ComponentType> GetComponentTypes(Entity entity, Allocator allocator)
        {
            if (!Exists(entity)) throw new InvalidOperationException("Existing component owner required");
            if (bank != null && bank.Contains(entity)) throw new NotSupportedException("Canonical storage component enumeration requires its original owner domain");
            var types = data[entity].Keys.Concat(buffers[entity].Keys).Distinct().Select(type => (ComponentType)type).ToArray();
            return new NativeArray<ComponentType>(types);
        }

        public void RemoveComponent(Entity entity, ComponentType component)
        {
            if (component.Excluded || component.Type == null) throw new ArgumentException("Concrete component type required");
            RemoveComponent(entity, component.Type);
        }

        public void AddComponent<T>(EntityQuery query) where T : struct
        {
            if (!ReferenceEquals(query.Manager, this)) throw new InvalidOperationException("Query must belong to this manager");
            using var rows = query.ToEntityArray(Allocator.Temp);
            foreach (var entity in rows) if (!HasComponent<T>(entity)) AddComponent<T>(entity);
        }
    }
}
