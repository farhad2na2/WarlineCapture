using System;

namespace Game.Runtime.Movement
{
    public struct EngagedPosition
    {
        public float X,Y,Z;
        public EngagedPosition(float x,float y,float z){X=x;Y=y;Z=z;}
        public static EngagedPosition operator -(EngagedPosition a,EngagedPosition b)=>new EngagedPosition(a.X-b.X,a.Y-b.Y,a.Z-b.Z);
        public static EngagedPosition operator +(EngagedPosition a,EngagedPosition b)=>new EngagedPosition(a.X+b.X,a.Y+b.Y,a.Z+b.Z);
        public static EngagedPosition operator *(EngagedPosition a,float b)=>new EngagedPosition(a.X*b,a.Y*b,a.Z*b);
    }
    public struct EngagedCell { public int X,Z;public EngagedCell(int x,int z){X=x;Z=z;} }
    public struct EngagedScanOrder { public EngagedCell Center;public int RadiusCells;public byte HasStarted; }
    public struct EngagedMovementState
    {
        public EngagedPosition Position,TargetPosition;
        public EngagedCell Cell,TargetCell;
        public int TargetIndex,TargetVersion;
        public byte IsCommanded;
        public float CurrentSpeed;
    }
    public struct EngagedMovementInput
    {
        public int GridWidth,GridHeight,AggroRangeCells;
        public float CellSize,DeltaTime,AttackRange,ChaseBreakDistance,MoveSpeed,RoadSpeedMultiplier;
        public EngagedCell Footprint;
        public byte FactionId;
        public bool IsVehicle;
    }
    public struct EngagedMovementResult
    {
        public bool WriteFacing,DelegateVehicle;
        public EngagedPosition Direction;
        public float DirectMoveSpeed,Remaining;
    }
    // Facts are read in production order. Cached engagement position is intentional;
    // no live target transform refresh or target reacquisition occurs in this step.
    public interface IEngagedMovementContext
    {
        bool DebugTargetForSource();bool TargetIsStatic();bool TargetHasHealth();int TargetHealth();
        bool HoldingPosition();bool TryReadScan(out EngagedScanOrder scan);
        bool TryTargetFootprint(out EngagedCell footprint);
        EngagedCell WorldToCell(EngagedPosition position);
        bool PreferredSurface(bool vehicle,int cellIndex);
        bool CanMoveIntoCell(EngagedCell target,EngagedCell footprint,EngagedCell current,byte faction);
    }
    public static class EngagedMovementKernel
    {
        public static EngagedMovementResult Step<T>(ref T context,in EngagedMovementInput input,ref EngagedMovementState state)
            where T:struct,IEngagedMovementContext
        {
            var result=new EngagedMovementResult();
            float attackRange=Max(0f,input.AttackRange);
            if(attackRange<=0f||(state.TargetIndex==0&&state.TargetVersion==0))return result;
            if(context.DebugTargetForSource()){state.CurrentSpeed=0f;return result;}
            bool isStatic=context.TargetIsStatic();bool hasHealth=!isStatic&&context.TargetHasHealth();
            if(isStatic||(hasHealth&&context.TargetHealth()<=0)){Clear(ref state);state.CurrentSpeed=0f;return result;}
            bool hold=context.HoldingPosition();
            bool scanning=context.TryReadScan(out EngagedScanOrder scan)&&scan.HasStarted!=0&&scan.RadiusCells>0;
            var worldCell=context.WorldToCell(state.Position);if(InBounds(worldCell,input))state.Cell=worldCell;
            var toOther=state.TargetPosition-state.Position;toOther.Y=0f;
            float distSq=LengthSquared(toOther),dist=Sqrt(distSq);
            float selfRadius=CombatRadius(input.Footprint,input.CellSize);
            float targetRadius=context.TryTargetFootprint(out EngagedCell targetFootprint)?CombatRadius(targetFootprint,input.CellSize):0f;
            float effectiveRange=attackRange+selfRadius+targetRadius;
            float leash=Max(Max(0f,input.ChaseBreakDistance),Math.Max(0,input.AggroRangeCells)*input.CellSize);
            if(scanning&&!InsideScan(ref context,state.TargetPosition,scan,input)){Clear(ref state);state.CurrentSpeed=0f;return result;}
            if(hold&&distSq>effectiveRange*effectiveRange){Clear(ref state);state.CurrentSpeed=0f;return result;}
            if(state.IsCommanded==0&&leash>0f&&dist>leash){Clear(ref state);return result;}
            if(distSq<=effectiveRange*effectiveRange)
            {
                state.CurrentSpeed=0f;
                if(!input.IsVehicle&&LengthSquared(toOther)>1e-8f){result.WriteFacing=true;result.Direction=NormalizeSafe(toOther);}
                return result;
            }
            var direction=NormalizeSafe(toOther);result.Direction=direction;
            if(!input.IsVehicle&&distSq>1e-8f)result.WriteFacing=true;
            float desiredDistance=Max(0.05f,effectiveRange-0.05f);
            if(desiredDistance>0f)
            {
                float remaining=dist-desiredDistance;if(remaining<=0f)return result;
                float speed=input.MoveSpeed;
                if(InBounds(state.Cell,input)&&context.PreferredSurface(input.IsVehicle,unchecked(state.Cell.X+state.Cell.Z*input.GridWidth)))speed*=input.RoadSpeedMultiplier;
                float moveDistance=Min(remaining,speed*input.DeltaTime);
                if(input.IsVehicle){result.DelegateVehicle=true;result.DirectMoveSpeed=speed;result.Remaining=remaining;return result;}
                state.CurrentSpeed=0f;
                var next=state.Position+direction*moveDistance;
                if(!CanMoveAlongSegment(ref context,input,state.Position,next,input.Footprint,state.Cell,input.FactionId))return result;
                state.Position+=direction*moveDistance;
                var movedCell=context.WorldToCell(state.Position);if(InBounds(movedCell,input))state.Cell=movedCell;
            }
            return result;
        }
        public static bool CanMoveAlongSegment<T>(ref T context,in EngagedMovementInput input,EngagedPosition start,EngagedPosition end,EngagedCell footprint,EngagedCell current,byte faction)
            where T:struct,IEngagedMovementContext
        {
            // Keep float2 subtraction/length and float3 lerp storage boundaries.
            var delta=new Scalar2(end.X-start.X,end.Z-start.Z);float distance=Sqrt(delta.X*delta.X+delta.Y*delta.Y);
            float sampleDistance=Max(0.05f,input.CellSize*0.45f);int steps=Math.Max(1,(int)(float)Math.Ceiling(distance/sampleDistance));
            for(int i=1;i<=steps;i++)
            {
                float t=(float)i/steps;var sample=start+(end-start)*t;var cell=context.WorldToCell(sample);
                if(cell.X==current.X&&cell.Z==current.Z)continue;
                if(!context.CanMoveIntoCell(cell,footprint,current,faction))return false;
            }
            return true;
        }
        public static bool InBounds(EngagedCell cell,in EngagedMovementInput input)=>(uint)cell.X<(uint)input.GridWidth&&(uint)cell.Z<(uint)input.GridHeight;
        private static bool InsideScan<T>(ref T context,EngagedPosition target,in EngagedScanOrder scan,in EngagedMovementInput input)where T:struct,IEngagedMovementContext
        {
            var cell=context.WorldToCell(target);if(!InBounds(cell,input))return false;
            int dx=Abs(unchecked(cell.X-scan.Center.X)),dz=Abs(unchecked(cell.Z-scan.Center.Z));return Math.Max(dx,dz)<=Math.Max(1,scan.RadiusCells);
        }
        private static void Clear(ref EngagedMovementState state){state.TargetIndex=0;state.TargetVersion=0;state.TargetCell=default;state.TargetPosition=default;state.IsCommanded=0;}
        private static float CombatRadius(EngagedCell size,float cellSize)
        {int x=Math.Max(1,size.X),z=Math.Max(1,size.Z);float halfWidth=Max(0f,(x-1)*0.5f*cellSize),halfDepth=Max(0f,(z-1)*0.5f*cellSize);return Max(halfWidth,halfDepth);}
        private static EngagedPosition NormalizeSafe(EngagedPosition value)
        {float length=LengthSquared(value);var normalized=value*(1f/Sqrt(length));return length>1.17549435e-38f?normalized:default;}
        private static float LengthSquared(EngagedPosition value)=>value.X*value.X+value.Y*value.Y+value.Z*value.Z;
        private static float Sqrt(float value)=>(float)Math.Sqrt((float)value);
        private static float Max(float a,float b)=>float.IsNaN(b)||a>b?a:b;
        private static float Min(float a,float b)=>float.IsNaN(b)||a<b?a:b;
        private static int Abs(int x)=>x<0?unchecked(-x):x;
        private struct Scalar2 {public float X,Y;public Scalar2(float x,float y){X=x;Y=y;}}
    }
}
