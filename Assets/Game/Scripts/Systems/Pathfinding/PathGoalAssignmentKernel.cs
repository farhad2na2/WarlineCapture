using System;
namespace Game.Runtime.Pathfinding
{
    public interface IPathGoalCandidateContext
    {
        int Width { get; }
        bool CanPlaceGoal(PlacementCell cell);
        bool CanTraverseGoalSurface(PlacementCell cell);
        int ReservedEpoch(int index);
    }
    public interface IPathGoalReservationStorage { void SetEpoch(int index, int generation); }
    public struct PathGoalReservationPrepare
    {
        public bool Reallocate, Clear;
        public int Generation, RememberedGridSize;
    }
    public static class PathGoalReservationKernel
    {
        public static PathGoalReservationPrepare DecidePrepare(bool created, int rememberedGridSize, int generation, int gridSize)
        {
            if (!created || rememberedGridSize != gridSize)
                return new PathGoalReservationPrepare { Reallocate=true, Generation=1, RememberedGridSize=gridSize };
            if (generation==int.MaxValue)
                return new PathGoalReservationPrepare { Clear=true, Generation=1, RememberedGridSize=rememberedGridSize };
            return new PathGoalReservationPrepare { Generation=unchecked(generation+1), RememberedGridSize=rememberedGridSize };
        }
        // Placement validation precedes reservation; do not clamp the rectangle to grid boundaries here.
        public static void ReserveFootprint<T>(ref T storage,int width,int generation,PlacementCell cell,PlacementCell footprint)
            where T : struct,IPathGoalReservationStorage
        {
            var size=PlacementFootprint.ClampSize(footprint);var min=PlacementFootprint.GetMinCell(cell,size);var max=min+size;
            for(int y=min.y;y<max.y;y++)for(int x=min.x;x<max.x;x++)storage.SetEpoch(unchecked(y*width+x),generation);
        }
    }
    public static class PathGoalAssignmentKernel
    {
        // Exact installed UnityEngine.MathematicsModule math.hash(int3) IL; verified against managed and Burst.
        public static uint HashDesiredGoal(int x,int z,int startIndex) => unchecked(
            (uint)x*1283419601u+(uint)z*1210229737u+(uint)startIndex*2864955997u+3525118277u);
        public static bool CanUseGoal<T>(ref T context,PlacementCell cell,PlacementCell footprint,int generation)
            where T : struct,IPathGoalCandidateContext
        {
            if(!context.CanPlaceGoal(cell))return false;
            if(!context.CanTraverseGoalSurface(cell))return false;
            var size=PlacementFootprint.ClampSize(footprint);var min=PlacementFootprint.GetMinCell(cell,size);var max=min+size;
            for(int y=min.y;y<max.y;y++)for(int x=min.x;x<max.x;x++)
                if(context.ReservedEpoch(unchecked(y*context.Width+x))==generation)return false;
            return true;
        }
        public static PlacementCell FindNearestFreeGoal<T>(ref T context,PlacementCell desired,PlacementCell start,PlacementCell footprint,int startIndex)
            where T : struct,IPathGoalIdentityContext
        {
            var best=start;int bestDistanceSq=int.MaxValue;
            if(PathGoalPolicyKernel.TryUseDesiredGoal(ref context,desired,startIndex,out var direct))return direct;
            bool vehicle=footprint.x>1||footprint.y>1;
            int radiusLimit=Math.Min(Math.Max(context.Width,context.Height),vehicle?PathGoalPolicyKernel.VehicleGoalSearchRadius:PathGoalPolicyKernel.InfantryGoalSearchRadius);
            uint seed=HashDesiredGoal(desired.x,desired.y,startIndex);
            for(int radius=1;radius<=radiusLimit;radius++)
            {
                int ringLength=unchecked(8*radius);int first=(int)(seed%(uint)ringLength);
                for(int step=0;step<ringLength;step++)
                {
                    int order=first+step;if(order>=ringLength)order-=ringLength;
                    var offset=SquareRingOffset(radius,order);
                    var cell=new PlacementCell(unchecked(desired.x+offset.x),unchecked(desired.y+offset.y));
                    if(cell.x<0||cell.y<0||cell.x>=context.Width||cell.y>=context.Height)continue;
                    if(unchecked(cell.x+cell.y*context.Width)==startIndex)continue;
                    if(!context.CanUseGoal(cell))continue;
                    int dx=unchecked(cell.x-desired.x),dz=unchecked(cell.y-desired.y);
                    int distance=unchecked(dx*dx+dz*dz);
                    if(distance<bestDistanceSq){bestDistanceSq=distance;best=cell;}
                    context.ReserveGoal(cell);return best;
                }
            }
            if(bestDistanceSq!=int.MaxValue){context.ReserveGoal(best);return best;}
            return start;
        }
        public static PlacementCell SquareRingOffset(int radius,int step)
        {
            int top=unchecked(2*radius+1);if(step<top)return new PlacementCell(unchecked(-radius+step),radius);
            step-=top;int right=unchecked(2*radius);if(step<right)return new PlacementCell(radius,unchecked(radius-1-step));
            step-=right;int bottom=unchecked(2*radius);if(step<bottom)return new PlacementCell(unchecked(radius-1-step),-radius);
            step-=bottom;return new PlacementCell(-radius,unchecked(-radius+1+step));
        }
        public static bool ShouldRestoreDesiredGoal(PlacementCell assigned,PlacementCell start,PlacementCell desired,int width,int height) =>
            assigned.x==start.x&&assigned.y==start.y&&(desired.x!=start.x||desired.y!=start.y)&&
            desired.x>=0&&desired.y>=0&&desired.x<width&&desired.y<height;
        // Call surface validation only when ShouldRestoreDesiredGoal is true, preserving native short-circuit order.
        public static PlacementCell RestoreDesiredGoal(PlacementCell assigned,PlacementCell desired,bool validSurface) => validSurface?desired:assigned;
    }
}
