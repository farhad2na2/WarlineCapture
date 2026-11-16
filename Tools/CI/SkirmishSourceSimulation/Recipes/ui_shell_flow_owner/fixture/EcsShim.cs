using System;
using System.Collections.Generic;

namespace Unity.Collections
{
    public struct FixedString64Bytes
    {
        private string value;
        public FixedString64Bytes(string value) => this.value = value;
        public override string ToString() => value ?? string.Empty;
    }
}

namespace UnityEngine
{
    public static class Mathf
    {
        public static float Clamp01(float value) => Math.Clamp(value, 0f, 1f);
    }
}

namespace Unity.Entities
{
    public interface IComponentData { }
    public interface IBufferElementData { }
    public sealed class SimulationSystemGroup { }
    [AttributeUsage(AttributeTargets.Struct)]
    public sealed class UpdateInGroupAttribute : Attribute
    {
        public UpdateInGroupAttribute(Type type) { }
    }
    public struct Entity : IEquatable<Entity>
    {
        public int Id;
        public Entity(int id) => Id = id;
        public static Entity Null => default;
        public bool Equals(Entity other) => Id == other.Id;
        public override int GetHashCode() => Id;
    }
    public readonly struct ComponentType
    {
        public readonly Type Type;
        private ComponentType(Type type) => Type = type;
        public static ComponentType ReadWrite<T>() => new(typeof(T));
    }
    public sealed class EntityQuery
    {
        private readonly EntityManager manager;
        private readonly Type[] required;
        internal EntityQuery(EntityManager manager, Type[] required) { this.manager = manager; this.required = required; }
        public Entity GetSingletonEntity() => manager.GetSingleton(required);
    }
    public sealed class DynamicBuffer<T> where T : struct
    {
        private readonly List<T> values = new();
        public int Length => values.Count;
        public T this[int index] { get => values[index]; set => values[index] = value; }
        public void Add(T value) => values.Add(value);
        public void Clear() => values.Clear();
        public void RemoveAt(int index) => values.RemoveAt(index);
    }
    public sealed partial class EntityManager
    {
        private readonly Dictionary<(int, Type), object> components = new();
        private readonly Dictionary<(int, Type), object> buffers = new();
        public Entity CreateEntity() => new(1);
        public void AddComponentData<T>(Entity entity, T value) where T : struct => components[(entity.Id, typeof(T))] = value;
        public T GetComponentData<T>(Entity entity) where T : struct => (T)components[(entity.Id, typeof(T))];
        public void SetComponentData<T>(Entity entity, T value) where T : struct => components[(entity.Id, typeof(T))] = value;
        public bool HasComponent<T>(Entity entity) where T : struct => components.ContainsKey((entity.Id, typeof(T)));
        public DynamicBuffer<T> GetBuffer<T>(Entity entity) where T : struct
        {
            var key = (entity.Id, typeof(T));
            if (!buffers.TryGetValue(key, out object value)) buffers[key] = value = new DynamicBuffer<T>();
            return (DynamicBuffer<T>)value;
        }
        public EntityQuery GetEntityQuery(params ComponentType[] components) => new(this, Array.ConvertAll(components, component => component.Type));
        internal Entity GetSingleton(Type[] types)
        {
            foreach (int id in new[] { 1 })
            {
                bool found = true;
                foreach (Type type in types)
                {
                    bool present = typeof(IBufferElementData).IsAssignableFrom(type)
                        ? buffers.ContainsKey((id, type))
                        : components.ContainsKey((id, type));
                    found &= present;
                }
                if (found) return new Entity(id);
            }
            throw new InvalidOperationException("Expected one fixture singleton with all required components and buffers.");
        }
    }
    public sealed class World
    {
        public bool IsCreated { get; set; } = true;
        public EntityManager EntityManager { get; } = new();
    }
    public struct SystemState
    {
        public EntityManager EntityManager;
        public World World;
        public EntityQuery GetEntityQuery(params ComponentType[] types) => EntityManager.GetEntityQuery(types);
        public void RequireForUpdate(EntityQuery query) { }
    }
    public interface ISystem
    {
        void OnCreate(ref SystemState state);
        void OnUpdate(ref SystemState state);
    }
}
