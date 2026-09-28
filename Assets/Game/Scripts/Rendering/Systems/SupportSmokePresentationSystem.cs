using System.Collections.Generic;
using Game.Components;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;
namespace Game.Rendering
{
    // Presentation only: zone lifetime and damage remain owned by unmanaged ECS.
    [UpdateInGroup(typeof(PresentationSystemGroup))]
    public partial class SupportSmokePresentationSystem : SystemBase
    {
        private readonly Dictionary<Entity,GameObject> visuals=new(8);
        private readonly List<Entity> expired=new(8);
        protected override void OnUpdate()
        {
            bool hasConfig=SystemAPI.TryGetSingleton(out SupportSmokeVisualConfigComponent config) && config.Prefab.Value!=null;
            expired.Clear();
            foreach(var pair in visuals) if(!hasConfig || !EntityManager.Exists(pair.Key) || !EntityManager.HasComponent<SupportSmokeZoneComponent>(pair.Key))
            {Object.Destroy(pair.Value);expired.Add(pair.Key);}
            foreach(var entity in expired)visuals.Remove(entity);
            if(!hasConfig)return;
            foreach(var (zone,entity) in SystemAPI.Query<RefRO<SupportSmokeZoneComponent>>().WithEntityAccess())
            {
                if(visuals.ContainsKey(entity))continue;
                var visual=Object.Instantiate(config.Prefab.Value,(Vector3)zone.ValueRO.Center,Quaternion.identity);
                visual.name="SupportSmoke";
                // Existing source smoke particles are spread over the gameplay footprint; no target-obscuring opaque mesh.
                float radius=Mathf.Sqrt(zone.ValueRO.RadiusSquared);
                ConfigureSmoke(visual,radius);
                visuals.Add(entity,visual);
            }
        }
        public static void ConfigureSmoke(GameObject visual,float radius)
        {
                foreach(var particles in visual.GetComponentsInChildren<ParticleSystem>())
                {
                    particles.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
                    var shape=particles.shape;shape.enabled=true;shape.shapeType=ParticleSystemShapeType.Circle;shape.radius=radius;
                    shape.rotation=new Vector3(-90,0,0);
                    var main=particles.main;main.loop=true;main.startSpeed=.35f;main.startLifetime=5;main.startSize=new ParticleSystem.MinMaxCurve(12,24);
                    main.startColor=new Color(.65f,.68f,.7f,.35f);
                    var size=particles.sizeOverLifetime;size.enabled=false;
                    var velocity=particles.velocityOverLifetime;velocity.enabled=false;
                    var emission=particles.emission;emission.rateOverTime=24;
                    particles.Play();
                }
        }
        protected override void OnDestroy()
        {foreach(var pair in visuals)if(pair.Value!=null)Object.Destroy(pair.Value);visuals.Clear();}
    }
}
