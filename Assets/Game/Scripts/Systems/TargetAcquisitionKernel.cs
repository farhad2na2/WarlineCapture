using System;

namespace Game.Runtime.Targeting
{
    public struct TargetIdentity
    {
        public int Index, Version;
        public TargetIdentity(int index, int version) { Index=index; Version=version; }
        public bool IsNull => Index==0 && Version==0;
        public bool Same(in TargetIdentity other) => Index==other.Index && Version==other.Version;
    }
    public struct TargetPosition { public float X,Y,Z; public TargetPosition(float x,float y,float z){X=x;Y=y;Z=z;} }
    public struct TargetCell { public int X,Z; public TargetCell(int x,int z){X=x;Z=z;} }
    public struct TargetScanOrder { public int CenterX,CenterZ,RadiusCells; public byte HasStarted,EngageDetectedTargets; }
    public struct TargetPolicyFacts
    {
        public bool HasSourcePolicy,HasTargetPolicy,TargetIsStructure,TargetIsAir,TargetUsesVehicleMotion;
        public byte SourceAllowedTargets,TargetDomain,TargetVisible;
        public bool PairedFog,SameFogSession,FogVisibleToObserver;
    }
    public struct TargetAcquisitionInput
    {
        public TargetIdentity Source;
        public TargetPosition Position;
        public TargetCell GridCell;
        public int GridWidth,GridHeight,AggroRangeCells;
        public float CellSize,AttackRange;
        public byte FactionId,CanAttack,AutoEngage;
    }
    public struct TargetAcquisitionResult
    {
        public bool Acquired,RemoveRecentAttacker,Retaliation;
        public TargetIdentity Target;
        public TargetCell Cell;
        public TargetPosition Position;
        public float WinningScore;
    }
    // Source flags, component presence and ordered candidate membership are explicit
    // facts. BeginCell/NextCell must preserve the native multihash iteration order.
    // AttackerCount is the immutable pre-acquisition snapshot, never incremented here.
    public interface ITargetAcquisitionContext
    {
        bool SourceHasAttackMove(); bool SourceHasManualMove(); bool SourceHasPathFollow(); bool SourceHasPathRequest(); bool SourceHoldsPosition();
        bool TryReadScan(out TargetScanOrder order); bool TryReadRecent(out TargetIdentity target);
        bool BeginCell(int key,out TargetIdentity candidate); bool NextCell(out TargetIdentity candidate);
        int BuildingCount(); TargetIdentity BuildingAt(int index);
        bool HasFaction(TargetIdentity target); byte Faction(TargetIdentity target);
        bool HasTransform(TargetIdentity target); TargetPosition Position(TargetIdentity target);
        bool HasHealth(TargetIdentity target); int Health(TargetIdentity target);
        bool HasEngagement(TargetIdentity target); TargetIdentity EngagementTarget(TargetIdentity target);
        bool ProtectedRecords(TargetIdentity target);
        TargetPolicyFacts PolicyFacts(TargetIdentity source,TargetIdentity target);
        int AttackerCount(TargetIdentity target);
        TargetCell WorldToCell(TargetPosition position);
    }
    // Opt-in for complete, stable readonly fact domains during one acquisition call.
    // Fact reads must not perform lazy admission/proof checks or external writes.
    // Replay/admission providers retain the original Acquire entry and read order.
    public interface IReadOnlyTargetAcquisitionContext : ITargetAcquisitionContext { }
    public static class TargetAcquisitionKernel
    {
        public static bool ShouldClearUnavailableTarget(bool isStatic,bool hasHealth,int health)=>isStatic||(hasHealth&&health<=0);

        public static TargetAcquisitionResult Acquire<T>(ref T context,in TargetAcquisitionInput input)
            where T:struct,ITargetAcquisitionContext => AcquireCore(ref context,input,false);

        public static TargetAcquisitionResult AcquireFromReadOnlyFacts<T>(ref T context,in TargetAcquisitionInput input)
            where T:struct,IReadOnlyTargetAcquisitionContext => AcquireCore(ref context,input,true);

        private static TargetAcquisitionResult AcquireCore<T>(ref T context,in TargetAcquisitionInput input,bool distancePrecheck)
            where T:struct,ITargetAcquisitionContext
        {
            var result=new TargetAcquisitionResult{WinningScore=float.MaxValue};
            if(input.CanAttack==0)return result;
            if(input.AutoEngage==0&&!context.SourceHasAttackMove())return result;
            if(input.FactionId==0)return result;
            bool manual=context.SourceHasManualMove()&&!context.SourceHasAttackMove()&&(context.SourceHasPathFollow()||context.SourceHasPathRequest());
            bool holding=context.SourceHoldsPosition();
            bool scanning=context.TryReadScan(out TargetScanOrder scan)&&scan.HasStarted!=0&&scan.EngageDetectedTargets!=0&&scan.RadiusCells>0;
            int attackRangeCells=input.CellSize>1e-6f&&input.AttackRange>0f?(int)Ceil(input.AttackRange/input.CellSize):0;
            int scanRange=holding?attackRangeCells:Math.Max(Math.Max(0,input.AggroRangeCells),attackRangeCells);
            if(scanning)scanRange=Math.Max(scanRange,Math.Max(1,scan.RadiusCells));
            if(scanRange<=0)return result;
            float maxDist=holding&&input.AttackRange>0f?input.AttackRange:scanRange*input.CellSize;
            if(!holding&&input.AttackRange>0f)maxDist=Max(maxDist,input.AttackRange);
            if(scanning)maxDist=Max(maxDist,Math.Max(1,scan.RadiusCells)*input.CellSize*2f);
            float maxDistSq=maxDist*maxDist;
            if(context.TryReadRecent(out TargetIdentity recent))
            {
                bool blocks=manual&&!scanning;
                if(!blocks&&AllowsPolicy(context.PolicyFacts(input.Source,recent))&&ValidRetaliation(ref context,recent,input.FactionId)&&(!scanning||TargetInsideScan(ref context,recent,scan,input)))
                {
                    var position=context.Position(recent);var delta=new Scalar3(position)-new Scalar3(input.Position);delta.Y=0;
                    if(!holding||LengthSquared(delta)<=maxDistSq)
                    {
                        result.Acquired=true;result.Retaliation=true;result.RemoveRecentAttacker=true;result.Target=recent;
                        result.Position=position;result.Cell=context.WorldToCell(position);return result;
                    }
                }
                result.RemoveRecentAttacker=true;
            }
            if(manual&&!scanning)return result;
            var current=context.WorldToCell(input.Position);if(!InBounds(current,input))current=input.GridCell;
            int centerX=scanning?scan.CenterX:current.X,centerZ=scanning?scan.CenterZ:current.Z;
            int searchRange=scanning?Math.Max(1,scan.RadiusCells):scanRange;
            float bestScore=float.MaxValue;TargetIdentity best=default;
            for(int dz=-searchRange;dz<=searchRange;dz++)
            {
                int z=unchecked(centerZ+dz);if((uint)z>=(uint)input.GridHeight)continue;
                for(int dx=-searchRange;dx<=searchRange;dx++)
                {
                    int x=unchecked(centerX+dx);if((uint)x>=(uint)input.GridWidth)continue;
                    int key=unchecked(x+z*input.GridWidth);
                    if(!context.BeginCell(key,out TargetIdentity candidate))continue;
                    do
                    {
                        if(candidate.Same(input.Source))continue;
                        if(!context.HasFaction(candidate)||!context.HasTransform(candidate))continue;
                        if(context.Faction(candidate)==input.FactionId)
                        {
                            if(context.HasEngagement(candidate))
                            {
                                var allyTarget=context.EngagementTarget(candidate);
                                if(!allyTarget.IsNull)Evaluate(ref context,input,allyTarget,maxDistSq,scanning,scan,ref bestScore,ref best,distancePrecheck);
                            }
                            continue;
                        }
                        Evaluate(ref context,input,candidate,maxDistSq,scanning,scan,ref bestScore,ref best,distancePrecheck);
                    }while(context.NextCell(out candidate));
                }
            }
            if(best.IsNull&&context.SourceHasAttackMove())
                for(int i=0;i<context.BuildingCount();i++)Evaluate(ref context,input,context.BuildingAt(i),maxDistSq,scanning,scan,ref bestScore,ref best,distancePrecheck);
            if(best.IsNull)return result;
            var bestPos=context.HasTransform(best)?context.Position(best):input.Position;
            var bestCell=context.WorldToCell(bestPos);if(!InBounds(bestCell,input))bestCell=input.GridCell;
            result.Acquired=true;result.Target=best;result.Cell=bestCell;result.Position=bestPos;result.WinningScore=bestScore;return result;
        }
        private static void Evaluate<T>(ref T context,in TargetAcquisitionInput input,TargetIdentity candidate,float maxDistSq,bool scanning,
            in TargetScanOrder scan,ref float bestScore,ref TargetIdentity best,bool distancePrecheck)where T:struct,ITargetAcquisitionContext
        {
            if(context.ProtectedRecords(candidate))return;
            TargetPosition position=default;float distSq=0f;
            if(distancePrecheck)
            {
                if(!context.HasTransform(candidate))return;
                position=context.Position(candidate);
                var delta=new Scalar3(position)-new Scalar3(input.Position);delta.Y=0f;
                distSq=LengthSquared(delta);if(distSq>maxDistSq)return;
            }
            if(!AllowsPolicy(context.PolicyFacts(input.Source,candidate)))return;
            if(!context.HasFaction(candidate)||(!distancePrecheck&&!context.HasTransform(candidate)))return;
            byte faction=context.Faction(candidate);if(faction==0||faction==input.FactionId)return;
            if(context.HasHealth(candidate)&&context.Health(candidate)<=0)return;
            if(!distancePrecheck)position=context.Position(candidate);
            if(scanning&&!PositionInsideScan(ref context,position,scan,input))return;
            if(!distancePrecheck)
            {
                var delta=new Scalar3(position)-new Scalar3(input.Position);delta.Y=0f;
                distSq=LengthSquared(delta);if(distSq>maxDistSq)return;
            }
            int attackers=context.AttackerCount(candidate);
            float score=distSq+attackers*(input.CellSize*input.CellSize*9f);
            if(score<bestScore){bestScore=score;best=candidate;}
        }
        private static bool ValidRetaliation<T>(ref T context,TargetIdentity candidate,byte faction)where T:struct,ITargetAcquisitionContext
        {
            if(candidate.IsNull||!context.HasFaction(candidate)||!context.HasTransform(candidate))return false;
            byte other=context.Faction(candidate);if(other==0||other==faction)return false;
            return !context.HasHealth(candidate)||context.Health(candidate)>0;
        }
        private static bool TargetInsideScan<T>(ref T context,TargetIdentity target,in TargetScanOrder scan,in TargetAcquisitionInput input)where T:struct,ITargetAcquisitionContext=>
            context.HasTransform(target)&&PositionInsideScan(ref context,context.Position(target),scan,input);
        private static bool PositionInsideScan<T>(ref T context,TargetPosition position,in TargetScanOrder scan,in TargetAcquisitionInput input)where T:struct,ITargetAcquisitionContext
        {
            var cell=context.WorldToCell(position);if(!InBounds(cell,input))return false;
            int dx=Abs(unchecked(cell.X-scan.CenterX)),dz=Abs(unchecked(cell.Z-scan.CenterZ));
            return Math.Max(dx,dz)<=Math.Max(1,scan.RadiusCells);
        }
        public static bool AllowsPolicy(in TargetPolicyFacts facts)
        {
            byte visible=facts.HasTargetPolicy?facts.TargetVisible:(byte)1;
            byte domain=facts.HasTargetPolicy?facts.TargetDomain:InferDomain(facts.TargetIsStructure,facts.TargetIsAir,facts.TargetUsesVehicleMotion);
            if(facts.PairedFog){if(!facts.SameFogSession||!facts.FogVisibleToObserver)return false;visible=1;}
            if(visible==0)return false;if(!facts.HasSourcePolicy)return true;
            return (facts.SourceAllowedTargets&domain)!=0;
        }
        public static byte InferDomain(bool structure,bool air,bool vehicle)=>structure?(byte)4:air?(byte)8:vehicle?(byte)2:(byte)1;
        private static bool InBounds(TargetCell cell,in TargetAcquisitionInput input)=>(uint)cell.X<(uint)input.GridWidth&&(uint)cell.Z<(uint)input.GridHeight;
        private static int Abs(int value)=>value<0?unchecked(-value):value;
        private static float Max(float a,float b)=>float.IsNaN(b)||a>b?a:b;
        private static float Ceil(float value)=>(float)Math.Ceiling((float)value);
        private struct Scalar3
        {
            public float X,Y,Z;public Scalar3(float x,float y,float z){X=x;Y=y;Z=z;}public Scalar3(TargetPosition p){X=p.X;Y=p.Y;Z=p.Z;}
            public static Scalar3 operator-(Scalar3 a,Scalar3 b)=>new Scalar3(a.X-b.X,a.Y-b.Y,a.Z-b.Z);
        }
        private static float LengthSquared(Scalar3 value)=>value.X*value.X+value.Y*value.Y+value.Z*value.Z;
    }
}
