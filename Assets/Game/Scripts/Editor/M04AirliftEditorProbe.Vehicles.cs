using System;
using System.IO;
using System.Collections.Generic;
using Game.Components;
using Game.Runtime;
using Unity.Entities;
using Unity.Transforms;
using Unity.Rendering;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;
namespace Game.Editor
{
    public static partial class M04AirliftEditorProbe
    {
        private static double lastVehicleCapture;
        private static int vehicleStage=-1,vehicleCapture;
        private static void CaptureMovingVehicle(EntityManager em,Entity vehicle,int stage)
        {
            if(stage is not (1 or 3 or 6)||!em.Exists(vehicle)||Camera.main==null)return;
            var cameraSystem=World.DefaultGameObjectInjectionWorld.GetExistingSystemManaged<RtsCameraSystem>();if(cameraSystem==null)return;
            if(vehicleStage!=stage){vehicleStage=stage;vehicleCapture=0;lastVehicleCapture=EditorApplication.timeSinceStartup;}
            var position=em.GetComponentData<LocalTransform>(vehicle).Position;
            float height=vehicleCapture==0?14:vehicleCapture==1?55:140;
            cameraSystem.ApplyPerspectiveCameraModeInstant(Camera.main,position.y+height,50,25,50);
            cameraSystem.MoveCameraGroundCenterTo(Camera.main,position);
            if(vehicleCapture>=3||EditorApplication.timeSinceStartup-lastVehicleCapture<4)return;
            lastVehicleCapture=EditorApplication.timeSinceStartup;vehicleCapture++;
            string id="vehicle-stage-"+stage+"-"+vehicleCapture;
            ScreenCapture.CaptureScreenshot(Output+"/"+id+".png");
            var seen=new HashSet<Entity>();var rows=new List<string>();
            rows.Add("Vehicle="+vehicle+" cameraHeight="+height+" selected="+em.HasComponent<SelectedUnitTag>(vehicle)+" position="+position+" rotation="+em.GetComponentData<LocalTransform>(vehicle).Rotation);
            InspectVehicleTree(em,vehicle,position,seen,rows);
            if(em.HasComponent<UnitDetailedVisualReference>(vehicle))InspectVehicleTree(em,em.GetComponentData<UnitDetailedVisualReference>(vehicle).Root,position,seen,rows);
            if(em.HasComponent<UnitMidLodInstanceReference>(vehicle))InspectVehicleTree(em,em.GetComponentData<UnitMidLodInstanceReference>(vehicle).Instance,position,seen,rows);
            if(em.HasComponent<UnitLowLodInstanceReference>(vehicle))InspectVehicleTree(em,em.GetComponentData<UnitLowLodInstanceReference>(vehicle).Instance,position,seen,rows);
            File.WriteAllLines(Output+"/"+id+".txt",rows);
            Debug.Log("[M04VehicleReview] captured="+id+" entities="+seen.Count);
        }
        private static void InspectVehicleTree(EntityManager em,Entity entity,float3 origin,HashSet<Entity> seen,List<string> rows)
        {
            if(!em.Exists(entity)||!seen.Add(entity))return;
            string row=entity+" name="+em.GetName(entity);
            if(em.HasComponent<LocalToWorld>(entity)){var world=em.GetComponentData<LocalToWorld>(entity);row+=" world="+world.Position+" distance="+math.distance(origin,world.Position);}
            if(em.HasComponent<LocalTransform>(entity))row+=" local="+em.GetComponentData<LocalTransform>(entity).Position;
            if(em.HasComponent<Parent>(entity))row+=" parent="+em.GetComponentData<Parent>(entity).Value;
            row+=" render="+em.HasComponent<MaterialMeshInfo>(entity)+" disabled="+em.HasComponent<Disabled>(entity)+" hidden="+em.HasComponent<DisableRendering>(entity);
            if(em.HasComponent<WorldRenderBounds>(entity)){var bounds=em.GetComponentData<WorldRenderBounds>(entity).Value;row+=" bounds="+bounds.Center+" extents="+bounds.Extents;}
            rows.Add(row);
            if(em.HasBuffer<Child>(entity)){var children=em.GetBuffer<Child>(entity,true);for(int i=0;i<children.Length;i++)InspectVehicleTree(em,children[i].Value,origin,seen,rows);}
        }
    }
}
