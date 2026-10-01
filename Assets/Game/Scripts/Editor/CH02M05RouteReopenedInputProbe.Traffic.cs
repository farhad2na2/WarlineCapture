using System;
using System.Collections.Generic;
using Game.Components;
using Game.Runtime;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEditor;
using UnityEngine;
namespace Game.Editor
{
    public static partial class CH02M05RouteReopenedInputProbe
    {
        private static readonly List<Entity> trafficActors=new();
        private static Entity trafficTank;
        private static int trafficLaneIndex,trafficStage,trafficDeckSamples;
        private static float trafficDue,trafficStarted;
        private static bool Traffic=>SessionState.GetBool(Active+".Traffic",false);
        public static void RunTrafficEnglish()
        {
            ResetSpecialRuns();
            try
            {
            var testType=System.Linq.Enumerable.First(AppDomain.CurrentDomain.GetAssemblies(),a=>a.GetType("MatchHudMinimapDataSourceAdapterTests")!=null).GetType("MatchHudMinimapDataSourceAdapterTests");
            var tests=Activator.CreateInstance(testType);int checks=0;
            foreach(var method in testType.GetMethods(System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.Public))
                if(method.DeclaringType==testType&&method.GetParameters().Length==0&&method.ReturnType==typeof(void)){method.Invoke(tests,null);checks++;}
            Debug.Log($"[RouteReopenedSharedMinimap] result=Passed tests={checks} compact-and-layered=Passed legacy-projection=Passed");
            SessionState.SetBool(Active+".Traffic",true);SessionState.SetString(Active+".Negative","");
            SessionState.SetBool(Manual,false);SessionState.SetBool(Active+".ManualInput",true);SessionState.SetString(Active+".Locale","en");
            trafficLaneIndex=trafficStage=trafficDeckSamples=0;trafficActors.Clear();Run();
            }
            catch(Exception ex)
            {
                Debug.LogError("[RouteReopenedSharedMinimap] result=Failed "+ex);
                MissionEditorValidationExit.Complete(false);
            }
        }
        private static bool DriveTraffic(EntityManager em)
        {
            if(!Traffic)return false;
            int lane=trafficLaneIndex==0?505:385;
            if(trafficStage==0)
            {
                trafficTank=SpawnTraffic(em,"Unit_Veh_Tank_USA",new int2(648,lane));
                foreach(int z in new[]{lane-5,lane-2,lane+1})SpawnTraffic(em,"Unit_Veh_Truck_Tanker",new int2(680,z));
                trafficStarted=Time.unscaledTime;trafficDue=trafficStarted+7;
                MoveTraffic(em,trafficTank,new int2(680,lane));trafficStage=1;return true;
            }
            var p=em.GetComponentData<LocalTransform>(trafficTank).Position;
            using(var q=em.CreateEntityQuery(typeof(MapSurfaceComponent)))
            {
                var surface=q.GetSingleton<MapSurfaceComponent>();
                if(p.x>=661&&p.x<=699)
                {
                    if(!MapSurfaceBlobAccess.TryGetPrimarySurface(ref surface.SurfaceBlob.Value,new int2((int)p.x,(int)p.z),out var s)||s.SurfaceType!=MapSurfaceType.BridgeDeck||p.y<s.Height-.25f)
                        throw new InvalidOperationException("Traffic armor left a qualified bridge deck.");
                    trafficDeckSamples++;
                }
            }
            if(Time.unscaledTime-trafficStarted>100)throw new TimeoutException("Traffic armor failed stage "+trafficStage+" at "+p);
            if(trafficStage==1)
            {
                if(Time.unscaledTime<trafficDue)return true;
                if(math.distance(p.xz,new float2(680.5f,lane+.5f))<2.5f)throw new InvalidOperationException("Armor overlapped the occupied staging destination.");
                for(int i=1;i<trafficActors.Count;i++)MoveTraffic(em,trafficActors[i],new int2(740,lane-5+(i-1)*3));
                trafficDue=Time.unscaledTime+7;trafficStage=2;ScreenCapture.CaptureScreenshot(Output+"/traffic-blocked-"+lane+".png");return true;
            }
            if(trafficStage==2)
            {
                if(Time.unscaledTime<trafficDue)return true;
                MoveTraffic(em,trafficTank,new int2(715,lane));trafficStage=3;return true;
            }
            int2 goal=trafficStage switch {3=>new int2(715,lane),4=>new int2(715,lane-15),5=>new int2(715,lane),_=>new int2(648,lane)};
            if(math.distance(p.xz,new float2(goal.x+.5f,goal.y+.5f))>=2.5f)return true;
            if(trafficStage<6)
            {
                trafficStage++;goal=trafficStage switch {4=>new int2(715,lane-15),5=>new int2(715,lane),_=>new int2(648,lane)};
                MoveTraffic(em,trafficTank,goal);return true;
            }
            if(trafficDeckSamples==0)throw new InvalidOperationException("Armor bridge crossing was not observed.");
            Debug.Log($"[RouteReopenedTraffic] result=Passed bridge={lane} armor=actual-prefab haulers=3 blocked-goal=excluded recovery=normal-move-orders turn=Passed staging=Passed deckSamples={trafficDeckSamples}");
            foreach(var actor in trafficActors)if(em.Exists(actor))em.DestroyEntity(actor);trafficActors.Clear();trafficDeckSamples=0;trafficStage=0;
            if(++trafficLaneIndex==2){SessionState.SetBool(Active+".Traffic",false);Complete(true,"fixture=bridge-traffic armor=2-bridges blocked-recovery=Passed turns-and-staging=Passed mission-outcome=not-injected");}
            return true;
        }
        private static Entity SpawnTraffic(EntityManager em,string key,int2 cell)
        {
            Entity prefab=Entity.Null;using(var q=em.CreateEntityQuery(typeof(UnitPrefabRegistryEntry)))
                foreach(var owner in q.ToEntityArray(Unity.Collections.Allocator.Temp))foreach(var item in em.GetBuffer<UnitPrefabRegistryEntry>(owner,true))
                    if(em.Exists(item.Prefab)&&em.HasComponent<UnitSourcePrefabKey>(item.Prefab)&&em.GetComponentData<UnitSourcePrefabKey>(item.Prefab).Value.ToString()==key)prefab=item.Prefab;
            if(prefab==Entity.Null)throw new InvalidOperationException("Traffic prefab missing: "+key);
            using var surfaceQuery=em.CreateEntityQuery(typeof(MapSurfaceComponent));var surface=surfaceQuery.GetSingleton<MapSurfaceComponent>();
            if(!MapSurfaceBlobAccess.TryGetPrimarySurface(ref surface.SurfaceBlob.Value,cell,out var sample))throw new InvalidOperationException("Traffic spawn has no surface.");
            var actor=em.Instantiate(prefab);trafficActors.Add(actor);var t=em.GetComponentData<LocalTransform>(actor);t.Position=new float3(cell.x+.5f,sample.Height+.1f,cell.y+.5f);em.SetComponentData(actor,t);em.SetComponentData(actor,new UnitGrid{Cell=cell});
            var movement=em.GetComponentData<UnitMovementBehavior>(actor);movement.AllowIdleWander=0;em.SetComponentData(actor,movement);
            if(em.HasComponent<UnitFuelConsumption>(actor)){var fuel=em.GetComponentData<UnitFuelConsumption>(actor);fuel.Enabled=0;em.SetComponentData(actor,fuel);}
            em.SetComponentData(actor,new Faction{Id=1});UnitMoveOrderRequestSystem.EnqueueAndProcessClearMovementOrder(em,actor);return actor;
        }
        private static void MoveTraffic(EntityManager em,Entity actor,int2 goal)
        {
            if(!UnitMoveOrderRequestSystem.EnqueueAndProcessImmediateMoveOrder(em,actor,goal))throw new InvalidOperationException("Traffic move order rejected.");
        }
    }
}
