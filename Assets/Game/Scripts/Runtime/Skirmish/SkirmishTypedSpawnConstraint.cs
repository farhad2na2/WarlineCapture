using System;
using System.Collections.Generic;
using Game.Components;
using Game.Configs;
using Game.Skirmish.Contracts;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Game.Runtime
{
    // Launch/call owned; never a global cache. Cached geometry contains no native
    // allocation or entity handle that can survive disposal/recovery.
    public sealed class SkirmishTypedSpawnConstraint:IDisposable
    {
        readonly EntityManager manager;readonly Entity gridEntity;readonly GridConfig grid;
        readonly SkirmishMapLayoutConfig layout;readonly string semantic;
        readonly Dictionary<string,int[]> allowed=new Dictionary<string,int[]>(StringComparer.Ordinal);
        Entity launchSession;SkirmishResolvedSetup launchSetup;uint launchHash;
        bool disposed;
        public SkirmishTypedSpawnConstraint(EntityManager em,Entity nativeGrid,GridConfig actual,SkirmishMapLayoutConfig actualLayout)
        {
            if(actualLayout==null||actual.Width<=0||actual.Height<=0||!math.isfinite(actual.CellSize)||actual.CellSize<=0)
                throw new ArgumentException("Exact typed layout and native grid required");
            manager=em;gridEntity=nativeGrid;grid=actual;layout=actualLayout;
            semantic=SkirmishMeasuredLayoutBinder.ComputeLayoutSemanticFingerprint(layout);
        }
        public static bool TryForLaunch(EntityManager em,Entity session,SkirmishResolvedSetup setup,out SkirmishTypedSpawnConstraint constraint)
        {
            constraint=null;
            if(!SkirmishMissionLogicalBindingCompiler.RequiresBinding(setup))return true;
            if(!em.Exists(session)||!em.HasComponent<SkirmishLaunchDefinitionRecord>(session)||
                !em.HasComponent<SkirmishExpandedSessionComponent>(session)||!SkirmishMissionLogicalBindingProvider.TryRead(em,session,out _))return false;
            var record=em.GetComponentObject<SkirmishLaunchDefinitionRecord>(session);
            if(record==null||!record.Matches(setup,em.GetComponentData<SkirmishExpandedSessionComponent>(session)))return false;
            using var grids=em.CreateEntityQuery(typeof(GridConfig));
            if(grids.CalculateEntityCount()!=1)return false;
            if(!HasCompleteAllocator(em,grids.GetSingletonEntity(),grids.GetSingleton<GridConfig>()))return false;
            constraint=new SkirmishTypedSpawnConstraint(em,grids.GetSingletonEntity(),grids.GetSingleton<GridConfig>(),record.Definition.MapLayout);
            constraint.launchSession=session;constraint.launchSetup=setup;constraint.launchHash=setup.SetupHash;return true;
        }
        public bool TryMask(SkirmishResolvedForceEntry force,in NativeBitArray actualOccupancy,out NativeBitArray occupancy)
        {
            occupancy=default;
            if(launchSetup!=null&&(!manager.Exists(launchSession)||launchSetup.SetupHash!=launchHash||
                !manager.HasComponent<SkirmishLaunchDefinitionRecord>(launchSession)||!manager.HasComponent<SkirmishExpandedSessionComponent>(launchSession)||
                !manager.GetComponentObject<SkirmishLaunchDefinitionRecord>(launchSession).Matches(launchSetup,manager.GetComponentData<SkirmishExpandedSessionComponent>(launchSession))))return false;
            if(disposed||!manager.Exists(gridEntity)||!manager.HasComponent<GridConfig>(gridEntity)||
                !SameGrid(grid,manager.GetComponentData<GridConfig>(gridEntity))||!actualOccupancy.IsCreated||actualOccupancy.Length!=grid.Width*grid.Height||
                semantic!=SkirmishMeasuredLayoutBinder.ComputeLayoutSemanticFingerprint(layout)||
                !SkirmishMapLayoutBuilder.TryPadForForce(layout,force,out var pad))return false;
            if(!allowed.TryGetValue(pad.PadId,out var cells))
            {
                var quad=new float2[4];float hx=pad.WidthMetres*.5f,hz=pad.DepthMetres*.5f;
                float2[] local={new float2(pad.CenterX-hx,pad.CenterZ-hz),new float2(pad.CenterX+hx,pad.CenterZ-hz),
                    new float2(pad.CenterX+hx,pad.CenterZ+hz),new float2(pad.CenterX-hx,pad.CenterZ+hz)};
                for(int i=0;i<4;i++)
                {if(!layout.TryProjectLocalToMap(local[i].x,local[i].y,out float x,out float z)||!math.isfinite(x)||!math.isfinite(z))return false;quad[i]=new float2(x,z);}
                var list=new List<int>();
                float2 min=quad[0],max=quad[0];foreach(var point in quad){min=math.min(min,point);max=math.max(max,point);}
                int2 lo=math.max(new int2(0),GridUtils.WorldToCell(grid,new float3(min.x,0,min.y)));
                int2 hi=math.min(new int2(grid.Width-1,grid.Height-1),GridUtils.WorldToCell(grid,new float3(max.x,0,max.y)));
                for(int z=lo.y;z<=hi.y;z++)for(int x=lo.x;x<=hi.x;x++)
                {
                    float left=grid.Origin.x+x*grid.CellSize,bottom=grid.Origin.z+z*grid.CellSize;
                    if(Inside(quad,new float2(left,bottom))&&Inside(quad,new float2(left+grid.CellSize,bottom))&&
                        Inside(quad,new float2(left+grid.CellSize,bottom+grid.CellSize))&&Inside(quad,new float2(left,bottom+grid.CellSize)))
                        list.Add(z*grid.Width+x);
                }
                cells=list.ToArray();allowed.Add(pad.PadId,cells);
            }
            occupancy=new NativeBitArray(actualOccupancy.Length,Allocator.Persistent,NativeArrayOptions.ClearMemory);
            try
            {
                occupancy.SetBits(0,true,occupancy.Length);
                foreach(int index in cells)occupancy.Set(index,actualOccupancy.IsSet(index));
                return true;
            }
            catch{occupancy.Dispose();occupancy=default;throw;}
        }
        public bool TryRequestedWorld(SkirmishResolvedForceEntry force,int member,out Vector3 world)
        {
            world=default;
            if(disposed||member<0||semantic!=SkirmishMeasuredLayoutBinder.ComputeLayoutSemanticFingerprint(layout)||
                !SkirmishMapLayoutBuilder.TryPadForForce(layout,force,out var pad)||
                !layout.TryProjectLocalToMap(pad.CenterX,pad.CenterZ,out float x,out float z))return false;
            world=RequestedWorldOnPad(x,z,force.FactionId,force.RoleKind,member);return true;
        }
        public static Vector3 RequestedWorldOnPad(float x,float z,byte faction,SkirmishRoleKind role,int member)
            =>role==SkirmishRoleKind.ConvoyLogic?new Vector3(x,0,z):SkirmishVisualSpawnService.OffsetOnPad(x,z,faction,member);
        public static Unity.Mathematics.Random PlacementRandom(int missionSeed,FixedString64Bytes stableId,int2 requestedCell)
        {
            if(stableId.Length==0)throw new ArgumentException("Actual immutable original stable actor ID required");
            uint placementSeed=2166136261u^unchecked((uint)missionSeed);
            for(int i=0;i<stableId.Length;i++)placementSeed=unchecked((placementSeed^stableId[i])*16777619u);
            placementSeed=math.hash(new uint3(unchecked((uint)requestedCell.x),unchecked((uint)requestedCell.y),placementSeed));
            return new Unity.Mathematics.Random(math.max(1u,placementSeed));
        }
        static bool Inside(float2[] quad,float2 point)
        {
            float sign=0;
            for(int i=0;i<4;i++)
            {float2 a=quad[(i+1)%4]-quad[i],b=point-quad[i];float cross=a.x*b.y-a.y*b.x;
                if(cross==0)continue;if(sign==0)sign=cross;else if((cross>0)!=(sign>0))return false;}
            return sign!=0;
        }
        static bool SameGrid(GridConfig a,GridConfig b)=>a.Width==b.Width&&a.Height==b.Height&&a.CellSize==b.CellSize&&math.all(a.Origin==b.Origin);
        public static bool HasCompleteAllocator(EntityManager em,Entity entity,GridConfig actual)
        {
            if(!em.Exists(entity)||actual.Width<=0||actual.Height<=0||!em.HasBuffer<GridWalkable>(entity)||
                !em.HasComponent<DynamicBlockerComponent>(entity)||!em.HasComponent<DynamicOccupancyComponent>(entity))return false;
            int count=checked(actual.Width*actual.Height);
            var blocked=em.GetComponentData<DynamicBlockerComponent>(entity).Blocked;
            var occupied=em.GetComponentData<DynamicOccupancyComponent>(entity).Occupied;
            return em.GetBuffer<GridWalkable>(entity,true).Length==count&&blocked.IsCreated&&occupied.IsCreated&&blocked.Length==count&&occupied.Length==count;
        }
        public void Dispose(){disposed=true;allowed.Clear();}
    }
}
