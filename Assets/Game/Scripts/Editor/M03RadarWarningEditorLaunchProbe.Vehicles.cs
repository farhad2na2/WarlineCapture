using System;
using System.Collections.Generic;
using Game.Components;
using Game.Missions.Contracts;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;
using Unity.Transforms;
using UnityEditor;
using UnityEngine;

namespace Game.Editor
{
    public static partial class M03RadarWarningEditorLaunchProbe
    {
        private const string VehicleVisualKey="Warline.M03.VehicleVisualProbe";
        private static readonly HashSet<Entity> inspectedVehicles=new();
        private static Entity inspectingVehicle;
        private static float3 vehicleStart;
        private static double vehicleFocusAt;
        private static int vehicleCaptureFrame;
        private static bool vehicleCommandCaptured;

        public static void RunVehicleVisualValidation()=>RunChecked(()=>
        {
            inspectedVehicles.Clear();inspectingVehicle=Entity.Null;vehicleCommandCaptured=false;vehicleCaptureFrame=0;
            SessionState.SetBool(VehicleVisualKey,true);
            MainMenuV3PrefabBuilder.SetGameViewResolution(2400,1080);Run();
        });

        private static bool AdvanceVehicleVisualValidation(EntityManager em,Entity root,
            in CampaignMissionRuntimeComponent runtime,in CampaignMissionAttemptFactsComponent facts)
        {
            if(!SessionState.GetBool(VehicleVisualKey,false))return false;
            if(runtime.Phase!=MissionPhaseKind.Engage || facts.ElapsedMilliseconds<2500)return true;
            var camera=Camera.main;
            if(!vehicleCommandCaptured)
            {
                if(camera==null || camera.transform.position.y>60)throw new InvalidOperationException("Command camera is still too far away.");
                ScreenCapture.CaptureScreenshot(Output+"/vehicles-command.png");
                vehicleCommandCaptured=true;vehicleCaptureFrame=Time.frameCount;return true;
            }
            if(Time.frameCount-vehicleCaptureFrame<4)return true;
            if(inspectedVehicles.Count>=3)
            {
                Complete(true,"All three moving convoy vehicles keep full meshes; command height=55; contact height=40; captures outside repository");
                return true;
            }
            if(inspectingVehicle==Entity.Null)
            {
                using var query=em.CreateEntityQuery(typeof(CampaignMissionUnitRoleComponent),typeof(UnitMovementBehavior),
                    typeof(Faction),typeof(LocalTransform),typeof(UnitMoveVisualComponent),typeof(UnitHealth));
                using var units=query.ToEntityArray(Allocator.Temp);
                foreach(var unit in units)
                {
                    if(!em.GetComponentData<CampaignMissionUnitRoleComponent>(unit).SessionToken.Equals(runtime.SessionToken) ||
                        em.GetComponentData<UnitMovementBehavior>(unit).UsesVehicleMotion==0 || em.GetComponentData<UnitHealth>(unit).Current<=0)continue;
                    RequireDetailedVehicle(em,unit);
                    if(em.GetComponentData<Faction>(unit).Id!=2 || inspectedVehicles.Contains(unit) || em.GetComponentData<UnitMoveVisualComponent>(unit).IsMoving==0)continue;
                    inspectingVehicle=unit;vehicleStart=em.GetComponentData<LocalTransform>(unit).Position;
                    using var focusQuery=em.CreateEntityQuery(typeof(RuntimeCameraFocusRequestComponent));
                    em.SetComponentData(focusQuery.GetSingletonEntity(),new RuntimeCameraFocusRequestComponent
                    {Requested=1,World=vehicleStart,UseExplicitPerspective=1,Perspective=new float4(vehicleStart.y+40,60,0,50)});
                    vehicleFocusAt=EditorApplication.timeSinceStartup;break;
                }
                return true;
            }
            if(EditorApplication.timeSinceStartup-vehicleFocusAt<1.2)return true;
            var position=em.GetComponentData<LocalTransform>(inspectingVehicle).Position;
            if(math.distance(position,vehicleStart)<.05f)throw new InvalidOperationException("Vehicle inspection did not exercise movement.");
            RequireDetailedVehicle(em,inspectingVehicle);
            var viewport=camera.WorldToViewportPoint(position);
            if(viewport.z<=0 || viewport.x<.15f || viewport.x>.8f || viewport.y<.2f || viewport.y>.85f)
                throw new InvalidOperationException("The focused enemy is hidden by the HUD.");
            int meshes=0;
            if(em.HasComponent<UnitDetailedVisualReference>(inspectingVehicle))
                meshes+=CountVisibleVehicleMeshes(em,em.GetComponentData<UnitDetailedVisualReference>(inspectingVehicle).Root,new HashSet<Entity>());
            if(em.HasComponent<UnitModelInstanceReference>(inspectingVehicle))
                meshes+=CountVisibleVehicleMeshes(em,em.GetComponentData<UnitModelInstanceReference>(inspectingVehicle).Instance,new HashSet<Entity>());
            if(meshes==0)throw new InvalidOperationException("Vehicle has no visible full-model meshes.");
            string source=em.GetComponentData<UnitSourcePrefabKey>(inspectingVehicle).Value.ToString();
            ScreenCapture.CaptureScreenshot(Output+"/vehicle-"+inspectedVehicles.Count+"-"+source+".png");
            Debug.Log($"[M3VehicleVisual] source={source} moving=true detailMeshes={meshes} visual=Detail impostor=false cameraHeight={camera.transform.position.y} viewport={viewport}");
            inspectedVehicles.Add(inspectingVehicle);inspectingVehicle=Entity.Null;vehicleCaptureFrame=Time.frameCount;
            return true;
        }

        private static void RequireDetailedVehicle(EntityManager em,Entity unit)
        {
            if(!em.HasComponent<UnitRenderVisualComponent>(unit))throw new InvalidOperationException("Vehicle visual state missing.");
            var visual=em.GetComponentData<UnitRenderVisualComponent>(unit);
            if(visual.Current!=(byte)UnitRenderVisualKind.Detail || visual.Desired!=(byte)UnitRenderVisualKind.Detail ||
                em.HasComponent<UnitRenderBudgetCulledUnitTag>(unit))throw new InvalidOperationException("A vehicle still uses an LOD/impostor.");
        }
        private static int CountVisibleVehicleMeshes(EntityManager em,Entity entity,HashSet<Entity> seen)
        {
            if(entity==Entity.Null || !em.Exists(entity) || !seen.Add(entity) || em.HasComponent<Disabled>(entity) || em.HasComponent<DisableRendering>(entity))return 0;
            int count=em.HasComponent<MaterialMeshInfo>(entity) && em.IsComponentEnabled<MaterialMeshInfo>(entity) ? 1 : 0;
            if(em.HasBuffer<Child>(entity))foreach(var child in em.GetBuffer<Child>(entity))count+=CountVisibleVehicleMeshes(em,child.Value,seen);
            return count;
        }
    }
}
