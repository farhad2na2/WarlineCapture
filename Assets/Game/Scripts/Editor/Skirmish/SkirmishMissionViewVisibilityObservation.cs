#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Rendering;
using UnityEngine;

namespace Game.Editor
{
    [Serializable] public struct SkirmishViewObstructionCandidate
    {
        public int EntityIndex,EntityVersion,SubjectBoundsIndex,SamplePoint;
        public Bounds WorldBounds;
        public Vector3 TargetWorld;
        public float EntryDistance,TargetDistance;
    }
    [Serializable] public sealed class SkirmishMissionViewVisibilitySample
    {
        public int SchemaVersion=1,Frame,ActiveSceneBoundsScanned;
        public ulong WorldSequence;
        public SkirmishCameraActorMeasurement Actor;
        public SkirmishViewObstructionCandidate[] PotentialObstructions;
        public bool InventoryTruncated;
        public bool RasterOcclusionMeasured=false,ReadablePixelsAccepted=false;
        public string Scope="Actual renderer AABB segment candidates only; native PNG and pixel visibility review required";
    }

    // Diagnostic archive only. No ARIA/public planner input, actor/camera/order
    // mutation or inference that an absent physics hit proves clear pixels.
    public static class SkirmishMissionViewVisibilityObservation
    {
        public static SkirmishMissionViewVisibilitySample Capture(EntityManager em,Entity actor,Camera camera)
        {
            var sample=new SkirmishMissionViewVisibilitySample { Frame=Time.frameCount,
                WorldSequence=em.WorldUnmanaged.SequenceNumber,
                Actor=SkirmishCameraFramingMeasurement.Capture(em,actor,camera) };
            var candidates=new List<SkirmishViewObstructionCandidate>();
            var subject=sample.Actor.RenderBounds;
            if(camera==null||subject==null||subject.Length==0){sample.PotentialObstructions=candidates.ToArray();return sample;}
            var own=new HashSet<Entity>();foreach(var row in subject)own.Add(new Entity {Index=row.EntityIndex,Version=row.EntityVersion});
            using var query=em.CreateEntityQuery(new EntityQueryDesc {
                All=new[]{ComponentType.ReadOnly<WorldRenderBounds>()},
                None=new[]{ComponentType.ReadOnly<Disabled>(),ComponentType.ReadOnly<DisableRendering>(),ComponentType.ReadOnly<Prefab>()} });
            using var entities=query.ToEntityArray(Allocator.Temp);
            foreach(var entity in entities)
            {
                sample.ActiveSceneBoundsScanned++;
                if(own.Contains(entity))continue;
                var raw=em.GetComponentData<WorldRenderBounds>(entity).Value;
                var bounds=new Bounds(raw.Center,raw.Extents*2);
                for(int i=0;i<subject.Length;i++)
                {
                    var b=subject[i].WorldBounds;
                    var targets=new[]{b.center,b.center+Vector3.up*b.extents.y*.75f,b.center-Vector3.up*b.extents.y*.75f,
                        b.center+camera.transform.right*b.extents.magnitude*.25f,b.center-camera.transform.right*b.extents.magnitude*.25f};
                    for(int point=0;point<targets.Length;point++)
                    {
                        if(!TrySegment(bounds,camera.transform.position,targets[point],out float entry,out float distance))continue;
                        if(candidates.Count>=2048){sample.InventoryTruncated=true;continue;}
                        candidates.Add(new SkirmishViewObstructionCandidate { EntityIndex=entity.Index,EntityVersion=entity.Version,
                            SubjectBoundsIndex=i,SamplePoint=point,WorldBounds=bounds,TargetWorld=targets[point],EntryDistance=entry,TargetDistance=distance });
                    }
                }
            }
            sample.PotentialObstructions=candidates.ToArray();return sample;
        }

        public static bool TrySegment(Bounds bounds,Vector3 origin,Vector3 target,out float entry,out float distance)
        {
            entry=0;distance=Vector3.Distance(origin,target);
            if(!Finite(origin)||!Finite(target)||!Finite(bounds.center)||!Finite(bounds.extents)||
                bounds.extents.x<=0||bounds.extents.y<=0||bounds.extents.z<=0||!float.IsFinite(distance)||distance<=0)return false;
            return bounds.IntersectRay(new Ray(origin,(target-origin)/distance),out entry)&&entry>=0&&entry<distance;
        }
        private static bool Finite(Vector3 v)=>float.IsFinite(v.x)&&float.IsFinite(v.y)&&float.IsFinite(v.z);
    }
}
#endif
