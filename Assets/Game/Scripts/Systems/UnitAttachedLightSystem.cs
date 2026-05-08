using System.Collections.Generic;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

[UpdateAfter(typeof(UnitModelSpawnSystem))]
public partial struct UnitAttachedLightSystem : ISystem
{
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<UnitAttachedLightSet>();
    }

    public void OnUpdate(ref SystemState state)
    {
        EntityManager em = state.EntityManager;
        var entitiesToInitialize = new List<Entity>();
        var runtimesToAttach = new List<UnitAttachedLightRuntime>();

        foreach (var (lightSet, transform, entity) in SystemAPI
                 .Query<UnitAttachedLightSet, RefRO<LocalTransform>>()
                 .WithNone<UnitAttachedLightRuntime>()
                 .WithEntityAccess())
        {
            if (lightSet?.Entries == null || lightSet.Entries.Length == 0)
                continue;

            var runtime = new UnitAttachedLightRuntime
            {
                Instances = new GameObject[lightSet.Entries.Length]
            };

            for (int i = 0; i < lightSet.Entries.Length; i++)
            {
                UnitAttachedLightSet.Entry entry = lightSet.Entries[i];
                if (entry == null)
                    continue;

                GameObject lightObject = new(entry.Name);
                Light light = lightObject.AddComponent<Light>();
                light.type = entry.Type;
                light.color = entry.Color;
                light.intensity = entry.Intensity;
                light.range = entry.Range;
                light.spotAngle = entry.SpotAngle;
                light.innerSpotAngle = entry.InnerSpotAngle;
                light.shadows = entry.CastShadows ? LightShadows.Soft : LightShadows.None;
                light.renderMode = LightRenderMode.Auto;
                runtime.Instances[i] = lightObject;
            }

            entitiesToInitialize.Add(entity);
            runtimesToAttach.Add(runtime);
        }

        for (int i = 0; i < entitiesToInitialize.Count; i++)
        {
            Entity entity = entitiesToInitialize[i];
            if (!em.Exists(entity))
                continue;

            em.AddComponentObject(entity, runtimesToAttach[i]);

            if (!em.HasComponent<UnitAttachedLightRuntime>(entity) || !em.HasComponent<UnitAttachedLightSet>(entity) || !em.HasComponent<LocalTransform>(entity))
                continue;

            UpdateLights(em.GetComponentObject<UnitAttachedLightSet>(entity), em.GetComponentObject<UnitAttachedLightRuntime>(entity), em.GetComponentData<LocalTransform>(entity));
        }

        foreach (var (lightSet, lightRuntime, transform) in SystemAPI
                 .Query<UnitAttachedLightSet, UnitAttachedLightRuntime, RefRO<LocalTransform>>())
        {
            UpdateLights(lightSet, lightRuntime, transform.ValueRO);
        }
    }

    private static void UpdateLights(UnitAttachedLightSet lightSet, UnitAttachedLightRuntime runtime, LocalTransform unitTransform)
    {
        if (lightSet?.Entries == null || runtime?.Instances == null)
            return;

        int count = math.min(lightSet.Entries.Length, runtime.Instances.Length);
        for (int i = 0; i < count; i++)
        {
            UnitAttachedLightSet.Entry entry = lightSet.Entries[i];
            GameObject lightObject = runtime.Instances[i];
            if (entry == null || lightObject == null)
                continue;

            Transform lightTransform = lightObject.transform;
            lightTransform.SetPositionAndRotation(
                unitTransform.Position + math.rotate(unitTransform.Rotation, entry.LocalPosition),
                unitTransform.Rotation * entry.LocalRotation);
        }
    }
}
