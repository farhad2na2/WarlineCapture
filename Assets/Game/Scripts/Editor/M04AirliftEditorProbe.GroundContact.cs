using System;
using System.Collections.Generic;
using System.IO;
using Game.Components;
using Game.Runtime;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;
using Unity.Transforms;
using UnityEditor;
using UnityEngine;

namespace Game.Editor
{
    public static partial class M04AirliftEditorProbe
    {
        private static int contactStage=-1,contactCaptures;
        private static double nextContactCapture;
        public static void RunGroundContactReview()
        {
            MissionSupportUiStyle.Build();
            SessionState.SetBool("Warline.M04.GroundContact",true);
            SessionState.SetString("Warline.M04.ReadinessOutput","/private/tmp/warline-m04-ground-contact");
            RunFinal();
        }
        private static void ReviewGroundContact(EntityManager em,Entity root,Entity carrier,int stage)
        {
            if(stage is not (1 or 3 or 4)||Camera.main==null)return;
            if(contactStage!=stage){contactStage=stage;contactCaptures=0;nextContactCapture=EditorApplication.timeSinceStartup+1;}
            var position=em.GetComponentData<LocalTransform>(carrier).Position;
            var system=World.DefaultGameObjectInjectionWorld.GetExistingSystemManaged<RtsCameraSystem>();
            system.ApplyPerspectiveCameraModeInstant(Camera.main,position.y+5.5f,50,18,stage==1?25:125);
            system.MoveCameraGroundCenterTo(Camera.main,position);
            if(contactCaptures>=3||EditorApplication.timeSinceStartup<nextContactCapture)return;
            contactCaptures++;nextContactCapture=EditorApplication.timeSinceStartup+2;
            var rows=new List<string>();
            using var members=em.GetBuffer<CampaignMissionExtractionMember>(root,true).ToNativeArray(Allocator.Temp);
            foreach(var member in members)
            {
                var entity=member.Entity;if(!em.Exists(entity)||!em.HasComponent<UnitSurfaceComponent>(entity)||em.HasComponent<UnitAirMovement>(entity)||em.HasComponent<UnitTransportPassenger>(entity))continue;
                var transform=em.GetComponentData<LocalTransform>(entity);var surface=em.GetComponentData<UnitSurfaceComponent>(entity);
                float offset=em.HasComponent<UnitGroundOffsetComponent>(entity)?em.GetComponentData<UnitGroundOffsetComponent>(entity).Value:0;
                if(em.HasBuffer<UnitAnimationGroundOffset>(entity)&&em.HasComponent<UnitResolvedAnimationIndex>(entity))
                {
                    var offsets=em.GetBuffer<UnitAnimationGroundOffset>(entity,true);int index=em.GetComponentData<UnitResolvedAnimationIndex>(entity).Value;
                    if((uint)index<(uint)offsets.Length)offset=offsets[index].Value*transform.Scale;
                }
                float error=Mathf.Abs(transform.Position.y-surface.LastSampledHeight-offset);
                if(error>.005f)throw new InvalidOperationException("Ground contact mismatch: "+entity+" error="+error);
                string name=em.HasComponent<UnitSourcePrefabKey>(entity)?em.GetComponentData<UnitSourcePrefabKey>(entity).Value.ToString():entity.ToString();
                if(name.StartsWith("Unit_Chr_",StringComparison.Ordinal)&&!em.HasBuffer<UnitAnimationGroundOffset>(entity))throw new InvalidOperationException("Runtime character lost its baked animation contact profile: "+name);
                rows.Add($"{name} kind={member.Kind} position={transform.Position} road={surface.LastSampledHeight:F6} correction={offset:F6} contactError={error:F6}");
            }
            var seen=new HashSet<Entity>();InspectVehicleTree(em,carrier,position,seen,rows);
            if(em.HasComponent<UnitDetailedVisualReference>(carrier))InspectVehicleTree(em,em.GetComponentData<UnitDetailedVisualReference>(carrier).Root,position,seen,rows);
            string id="contact-"+stage+"-"+contactCaptures;File.WriteAllLines(Output+"/"+id+".txt",rows);ScreenCapture.CaptureScreenshot(Output+"/"+id+".png");
            Debug.Log("[M04GroundContact] capture="+id+" APC="+position+" sampled="+rows.Count);
        }
    }
}
