using Game.Components;
using Game.Configs;
using Game.Skirmish.Contracts;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace Game.Runtime
{
    public static class SkirmishConvoySpawnHooks
    {
        public static bool TryResolveDefinition(SkirmishResolvedSetup setup,out SkirmishConvoyTruckDefinition definition,out string error)
        {
            definition=null;error="Compiled CE objective actor definition is unavailable or changed.";
            if(setup==null||setup.ObjectiveKind!=SkirmishObjectiveKind.ConvoyEscort||setup.PlayerObjectiveTrucks!=3||
                setup.EnemyObjectiveTrucks!=0||setup.ObjectiveSupportExtraPlayer!=3||
                string.IsNullOrWhiteSpace(setup.ObjectiveActorDefinitionFingerprint))return false;
            var actual=SkirmishConvoyTruckDefinition.Load();
            if(actual==null||!actual.IsValid||actual.Fingerprint()!=setup.ObjectiveActorDefinitionFingerprint)return false;
            definition=actual;error=null;return true;
        }
        public static bool TrySpecializeOpening(EntityManager em,Entity actor,FixedString64Bytes attempt,
            SkirmishResolvedSetup setup,int forceIndex,int member,out string error)
        {
            error="Exact compiled original Cargo Truck plan and native parking footprint required.";
            if(setup==null||setup.ObjectiveKind!=SkirmishObjectiveKind.ConvoyEscort||!setup.MeasuredLayoutBound||
                setup.PlayerObjectiveTrucks!=3||setup.EnemyObjectiveTrucks!=0||setup.ObjectiveSupportExtraPlayer!=3||
                setup.Forces==null||forceIndex<0||forceIndex>=setup.Forces.Length||member!=0||
                setup.ObjectiveTruckBays==null||setup.ObjectiveTruckBays.Length!=3||
                !SkirmishOriginalObjectiveSpawnHooks.TryPlan(setup,out var plan,out error))return false;
            var force=setup.Forces[forceIndex];
            if(force.RoleKind!=SkirmishRoleKind.ConvoyLogic||force.FactionId!=1||force.Quantity!=1||force.SupplyCost!=0||
                force.SourceConfigPath!=SkirmishConvoyTruckDefinition.AssetPath||force.RuntimePrefabKey!=SkirmishConvoyTruckDefinition.ExistingChassisKey)return false;
            byte slot=force.ObjectiveRoleId=="convoy.1"?(byte)1:force.ObjectiveRoleId=="convoy.2"?(byte)2:force.ObjectiveRoleId=="convoy.3"?(byte)3:(byte)0;
            if(slot==0)return false;
            string originalId=null;foreach(var row in plan)if(row.ForceIndex==forceIndex&&row.MemberIndex==member&&row.ObjectiveRole==SkirmishObjectiveRoleKind.ConvoyTruck)originalId=row.StableActorId;
            if(originalId==null)return false;
            if(!TryResolveDefinition(setup,out var definition,out error))return false;
            SkirmishResolvedConvoyBay bay=default;int matches=0;
            foreach(var candidate in setup.ObjectiveTruckBays)if(candidate.ObjectiveRoleId==force.ObjectiveRoleId){bay=candidate;matches++;}
            if(matches!=1||!TryValidateNativeParkingFootprint(em,actor,bay,out error))return false;
            return SkirmishConvoyTruckRuntime.TrySpecializeFreshInstance(em,actor,definition,attempt,originalId,slot,out error);
        }

        // Native grid footprint containment only. This does not prove road width,
        // traversability to the destination, terrain safety or source clearance.
        public static bool TryValidateNativeParkingFootprint(EntityManager em,Entity actor,in SkirmishResolvedConvoyBay bay,out string error)
        {
            error="Native placed footprint leaves its own explicit measured parking bay.";
            if(!em.Exists(actor)||em.HasComponent<Parent>(actor)||!em.HasComponent<UnitGrid>(actor)||!em.HasComponent<UnitFootprint>(actor)||
                !math.all(math.isfinite(new float4(bay.CenterX,bay.CenterZ,bay.HalfForward,bay.HalfAcross)))||bay.HalfForward<=0||bay.HalfAcross<=0)return false;
            var forward=new float2(bay.ForwardX,bay.ForwardZ);var across=new float2(bay.AcrossX,bay.AcrossZ);
            if(!math.all(math.isfinite(forward))||!math.all(math.isfinite(across))||
                math.abs(math.lengthsq(forward)-1)>.01f||math.abs(math.lengthsq(across)-1)>.01f||math.abs(math.dot(forward,across))>.01f)return false;
            using var grids=em.CreateEntityQuery(typeof(GridConfig));if(grids.CalculateEntityCount()!=1)return false;
            var grid=grids.GetSingleton<GridConfig>();var size=em.GetComponentData<UnitFootprint>(actor).Size;
            if(!math.isfinite(grid.CellSize)||grid.CellSize<=0||!math.all(math.isfinite(grid.Origin))||math.any(size<=0))return false;
            var min=UnitFootprintUtility.GetMinCell(em.GetComponentData<UnitGrid>(actor).Cell,size);
            if(!GridUtils.InBounds(min,grid.Width,grid.Height)||!GridUtils.InBounds(min+size-1,grid.Width,grid.Height))return false;
            for(int x=0;x<2;x++)for(int z=0;z<2;z++)
            {
                var corner=grid.Origin.xz+new float2(min.x+x*size.x,min.y+z*size.y)*grid.CellSize;
                var relative=corner-new float2(bay.CenterX,bay.CenterZ);
                if(math.abs(math.dot(relative,forward))>bay.HalfForward+.001f||math.abs(math.dot(relative,across))>bay.HalfAcross+.001f)return false;
            }
            error=null;return true;
        }
    }
}
