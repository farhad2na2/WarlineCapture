using System.Collections.Generic;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using Game.Components;

namespace Game.Rendering
{
    [UpdateAfter(typeof(UnitModelSpawnSystem))]
    public partial class UnitAttachedLightSystem : SystemBase
    {
        private readonly Dictionary<Entity, GameObject[]> _runtimeLights = new();
        private readonly List<Entity> _cleanupEntities = new();
        private readonly List<Entity> _staleEntities = new();

        protected override void OnUpdate()
        {
            _cleanupEntities.Clear();
            foreach (var (_, entity) in SystemAPI
                     .Query<RefRO<UnitAttachedLightCleanupRequest>>()
                     .WithEntityAccess())
            {
                DisposeRuntimeLights(entity);
                _cleanupEntities.Add(entity);
            }

            foreach (var (lightSet, transform, entity) in SystemAPI
                     .Query<DynamicBuffer<UnitAttachedLightSetupElement>, RefRO<LocalTransform>>()
                     .WithNone<UnitAttachedLightCleanupRequest, UnitDeathAnimationComponent, VehicleWreckComponent>()
                     .WithEntityAccess())
            {
                if (lightSet.Length == 0)
                    continue;

                if (!_runtimeLights.TryGetValue(entity, out GameObject[] instances) || instances == null || instances.Length != lightSet.Length)
                {
                    DisposeRuntimeLights(entity);
                    instances = CreateRuntimeLights(lightSet);
                    _runtimeLights[entity] = instances;
                }

                UpdateLights(lightSet, instances, transform.ValueRO);
            }

            _staleEntities.Clear();
            foreach (KeyValuePair<Entity, GameObject[]> entry in _runtimeLights)
            {
                Entity entity = entry.Key;
                if (!EntityManager.Exists(entity) ||
                    !EntityManager.HasBuffer<UnitAttachedLightSetupElement>(entity) ||
                    !EntityManager.HasComponent<LocalTransform>(entity) ||
                    EntityManager.HasComponent<UnitDeathAnimationComponent>(entity) ||
                    EntityManager.HasComponent<VehicleWreckComponent>(entity))
                {
                    _staleEntities.Add(entity);
                }
            }

            for (int i = 0; i < _staleEntities.Count; i++)
                DisposeRuntimeLights(_staleEntities[i]);

            for (int i = 0; i < _cleanupEntities.Count; i++)
            {
                Entity entity = _cleanupEntities[i];
                if (EntityManager.Exists(entity) && EntityManager.HasComponent<UnitAttachedLightCleanupRequest>(entity))
                    EntityManager.RemoveComponent<UnitAttachedLightCleanupRequest>(entity);
            }
        }

        protected override void OnDestroy()
        {
            foreach (KeyValuePair<Entity, GameObject[]> entry in _runtimeLights)
                DestroyInstances(entry.Value);

            _runtimeLights.Clear();
            _cleanupEntities.Clear();
            _staleEntities.Clear();
        }

        internal void DisposeRuntimeLights(Entity entity)
        {
            if (!_runtimeLights.Remove(entity, out GameObject[] instances))
                return;

            DestroyInstances(instances);
        }

        private static GameObject[] CreateRuntimeLights(DynamicBuffer<UnitAttachedLightSetupElement> lightSet)
        {
            var instances = new GameObject[lightSet.Length];
            for (int i = 0; i < lightSet.Length; i++)
            {
                UnitAttachedLightSetupElement entry = lightSet[i];
                string lightName = entry.Name.IsEmpty ? "UnitLight" : entry.Name.ToString();
                GameObject lightObject = new(lightName);
                Light light = lightObject.AddComponent<Light>();
                light.type = entry.Type;
                light.color = entry.Color;
                light.intensity = entry.Intensity;
                light.range = entry.Range;
                light.spotAngle = entry.SpotAngle;
                light.innerSpotAngle = entry.InnerSpotAngle;
                light.shadows = entry.CastShadows != 0 ? LightShadows.Soft : LightShadows.None;
                light.renderMode = LightRenderMode.Auto;
                instances[i] = lightObject;
            }

            return instances;
        }

        private static void UpdateLights(DynamicBuffer<UnitAttachedLightSetupElement> lightSet, GameObject[] instances, LocalTransform unitTransform)
        {
            if (instances == null)
                return;

            int count = math.min(lightSet.Length, instances.Length);
            for (int i = 0; i < count; i++)
            {
                UnitAttachedLightSetupElement entry = lightSet[i];
                GameObject lightObject = instances[i];
                if (lightObject == null)
                    continue;

                Transform lightTransform = lightObject.transform;
                lightTransform.SetPositionAndRotation(
                    unitTransform.Position + math.rotate(unitTransform.Rotation, entry.LocalPosition),
                    math.mul(unitTransform.Rotation, entry.LocalRotation));
            }
        }

        private static void DestroyInstances(GameObject[] instances)
        {
            if (instances == null)
                return;

            for (int i = 0; i < instances.Length; i++)
            {
                if (instances[i] != null)
                    Object.Destroy(instances[i]);
            }
        }
    }
}
