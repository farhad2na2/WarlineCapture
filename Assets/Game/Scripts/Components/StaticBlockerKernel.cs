namespace Game.Components
{
    [System.Serializable] public struct BlockerGrid
    {
        public int Width, Height;
        public float OriginX, OriginY, OriginZ, CellSize;
    }
    [System.Serializable] public struct BlockerRect { public int MinX, MinZ, MaxX, MaxZ; }
    [System.Serializable] public struct BlockerOpening
    {
        public float CenterX, CenterZ, RightX, RightZ, ForwardX, ForwardZ, HalfX, HalfZ;
    }
    [System.Serializable] public struct BlockerVector2
    {
        public float X,Z;
        public BlockerVector2(float x,float z){X=x;Z=z;}
        public static BlockerVector2 operator +(BlockerVector2 a,BlockerVector2 b)=>new BlockerVector2(a.X+b.X,a.Z+b.Z);
        public static BlockerVector2 operator -(BlockerVector2 a,BlockerVector2 b)=>new BlockerVector2(a.X-b.X,a.Z-b.Z);
        public static BlockerVector2 operator *(BlockerVector2 a,float b)=>new BlockerVector2(a.X*b,a.Z*b);
    }
    [System.Serializable] public struct BlockerOwnerIdentity { public int Index, Version; public ulong StorageEpoch; }
    [System.Serializable] public struct BlockerHistory
    {
        public BlockerRect Rect; public byte FriendlyFaction;
        public BlockerOwnerIdentity Owner;
    }
    [System.Serializable] public struct BlockerEpochState
    {
        public ulong StorageEpoch, AppliedStorageEpoch, AppliedRevision;
        public byte WriteInProgress, EpochExhausted;
        public BlockerGrid AppliedGrid;
    }
    public enum BlockerOpeningChange : byte { None, Add, Set, Remove }
    [System.Serializable] public struct BlockerUpdateResult
    {
        public bool Applied; public BlockerOpeningChange PreviousOpeningChange;
    }
    public interface IStaticBlockerStorage
    {
        int CountLength { get; } int BlockedLength { get; } int FriendlyPassLength { get; }
        bool FriendlyPassCreated { get; }
        int ReadCount(int index); void WriteCount(int index,int value);
        void WriteBlocked(int index,bool value); void ClearBlocked(); void WriteFriendlyPass(int index,byte value);
    }
    // Pure shared writer policy. Hosts retain query order: new, existing, then removed.
    // New/removed history changes are deferred until after all three phases.
    public static class StaticBlockerKernel
    {
        static float Abs(float x)=>System.Math.Abs(x);
        static bool Finite(float x)=>!float.IsNaN(x)&&!float.IsInfinity(x);
        static int Clamp(int x,int min,int max){int lowered=max<x?max:x;return min>lowered?min:lowered;}
        public static bool IsValid(in BlockerOpening o)=>Finite(o.CenterX)&&Finite(o.CenterZ)&&Finite(o.RightX)&&Finite(o.RightZ)&&
            Finite(o.ForwardX)&&Finite(o.ForwardZ)&&Finite(o.HalfX)&&Finite(o.HalfZ)&&o.HalfX>0f&&o.HalfZ>0f&&
            Abs(o.RightX*o.RightX+o.RightZ*o.RightZ-1f)<.001f&&Abs(o.ForwardX*o.ForwardX+o.ForwardZ*o.ForwardZ-1f)<.001f&&Abs(o.RightX*o.ForwardX+o.RightZ*o.ForwardZ)<.001f;
        public static bool SameOpening(in BlockerOpening a,in BlockerOpening b)=>a.CenterX==b.CenterX&&a.CenterZ==b.CenterZ&&a.RightX==b.RightX&&a.RightZ==b.RightZ&&a.ForwardX==b.ForwardX&&a.ForwardZ==b.ForwardZ&&a.HalfX==b.HalfX&&a.HalfZ==b.HalfZ;
        public static bool ContainsFullCell(in BlockerGrid grid,in BlockerOpening o,int x,int z)
        {
            if(!IsValid(o)||grid.CellSize<=0f)return false;
            var delta=new BlockerVector2(grid.OriginX,grid.OriginZ)+new BlockerVector2(x+.5f,z+.5f)*grid.CellSize-new BlockerVector2(o.CenterX,o.CenterZ);
            float dx=delta.X,dz=delta.Z;
            float half=grid.CellSize*.5f;
            return Abs(dx*o.RightX+dz*o.RightZ)+half*(Abs(o.RightX)+Abs(o.RightZ))<=o.HalfX&&
                Abs(dx*o.ForwardX+dz*o.ForwardZ)+half*(Abs(o.ForwardX)+Abs(o.ForwardZ))<=o.HalfZ;
        }
        public static BlockerRect Bounds(int x,int z,int sizeX,int sizeZ)=>new BlockerRect {MinX=x,MinZ=z,MaxX=unchecked(x+(sizeX<1?1:sizeX)),MaxZ=unchecked(z+(sizeZ<1?1:sizeZ))};
        public static bool SameRect(in BlockerRect a,in BlockerRect b)=>a.MinX==b.MinX&&a.MinZ==b.MinZ&&a.MaxX==b.MaxX&&a.MaxZ==b.MaxZ;
        public static bool SameLayout(in BlockerGrid a,in BlockerGrid b)=>a.Width==b.Width&&a.Height==b.Height&&a.CellSize==b.CellSize&&a.OriginX==b.OriginX&&a.OriginY==b.OriginY&&a.OriginZ==b.OriginZ;
        public static bool SameOwner(in BlockerOwnerIdentity a,in BlockerOwnerIdentity b)=>a.Index==b.Index&&a.Version==b.Version&&a.StorageEpoch==b.StorageEpoch;
        public static bool NeedsRebuild(in BlockerGrid grid,in BlockerEpochState owner)=>owner.StorageEpoch==0||owner.AppliedStorageEpoch!=owner.StorageEpoch||owner.WriteInProgress!=0||!SameLayout(grid,owner.AppliedGrid);
        public static void Invalidate(ref BlockerEpochState owner)
        {if(owner.StorageEpoch==ulong.MaxValue)owner.EpochExhausted=1;else owner.StorageEpoch++;owner.AppliedStorageEpoch=0;owner.WriteInProgress=1;}
        public static void BeginWrite(ref BlockerEpochState owner){owner.WriteInProgress=1;owner.AppliedStorageEpoch=0;}
        public static void Commit(in BlockerGrid grid,ref BlockerEpochState owner)
        {if(owner.AppliedRevision==ulong.MaxValue)owner.EpochExhausted=1;else owner.AppliedRevision++;owner.AppliedStorageEpoch=owner.StorageEpoch;owner.AppliedGrid=grid;owner.WriteInProgress=0;}
        public static void Clear<T>(ref T storage)where T:struct,IStaticBlockerStorage
        {for(int i=0;i<storage.CountLength;i++)storage.WriteCount(i,0);storage.ClearBlocked();for(int i=0;i<storage.FriendlyPassLength;i++)storage.WriteFriendlyPass(i,byte.MaxValue);}
        public static void ApplyDelta<T>(ref T storage,int index,int delta)where T:struct,IStaticBlockerStorage
        {
            if((uint)index>=(uint)storage.CountLength)return;
            int before=storage.ReadCount(index),after=unchecked(before+delta);storage.WriteCount(index,after);
            if(before<=0&&after>0)storage.WriteBlocked(index,true);else if(before>0&&after<=0)storage.WriteBlocked(index,false);
        }
        public static void ApplyRect<T>(in BlockerGrid grid,ref T storage,in BlockerRect rect,int delta,in BlockerOpening opening)where T:struct,IStaticBlockerStorage
        {
            int minX=Clamp(rect.MinX,0,grid.Width),minZ=Clamp(rect.MinZ,0,grid.Height),maxX=Clamp(rect.MaxX,0,grid.Width),maxZ=Clamp(rect.MaxZ,0,grid.Height);
            bool valid=IsValid(opening);
            for(int z=minZ;z<maxZ;z++){int row=unchecked(z*grid.Width);for(int x=minX;x<maxX;x++)if(!valid||!ContainsFullCell(grid,opening,x,z))ApplyDelta(ref storage,unchecked(row+x),delta);}
        }
        public static void ApplyFriendlyPass<T>(in BlockerGrid grid,ref T storage,in BlockerRect rect,byte faction)where T:struct,IStaticBlockerStorage
        {
            if(!storage.FriendlyPassCreated)return;
            int minX=Clamp(rect.MinX,0,grid.Width),minZ=Clamp(rect.MinZ,0,grid.Height),maxX=Clamp(rect.MaxX,0,grid.Width),maxZ=Clamp(rect.MaxZ,0,grid.Height);
            for(int z=minZ;z<maxZ;z++){int row=unchecked(z*grid.Width);for(int x=minX;x<maxX;x++)storage.WriteFriendlyPass(unchecked(row+x),faction);}
        }
        public static BlockerUpdateResult ApplyNew<T>(in BlockerGrid grid,ref T storage,in BlockerRect rect,byte faction,bool hasOpening,in BlockerOpening opening,in BlockerOwnerIdentity owner,out BlockerHistory history)where T:struct,IStaticBlockerStorage
        {
            ApplyRect(grid,ref storage,rect,1,opening);if(faction!=byte.MaxValue)ApplyFriendlyPass(grid,ref storage,rect,faction);
            history=new BlockerHistory {Rect=rect,FriendlyFaction=faction,Owner=owner};
            return new BlockerUpdateResult {Applied=true,PreviousOpeningChange=hasOpening?BlockerOpeningChange.Add:BlockerOpeningChange.None};
        }
        public static BlockerUpdateResult ApplyExisting<T>(in BlockerGrid grid,ref T storage,bool rebuild,in BlockerRect rect,byte faction,bool hasOpening,in BlockerOpening opening,bool hadOpening,in BlockerOpening oldOpening,in BlockerOwnerIdentity owner,ref BlockerHistory history)where T:struct,IStaticBlockerStorage
        {
            bool boundsChanged=!SameRect(rect,history.Rect),openingChanged=!SameOpening(opening,oldOpening),passChanged=history.FriendlyFaction!=faction;
            if(!rebuild&&!boundsChanged&&!openingChanged&&!passChanged)return default;
            var result=new BlockerUpdateResult {Applied=true};
            if(rebuild||boundsChanged||openingChanged)
            {
                if(!rebuild)ApplyRect(grid,ref storage,history.Rect,-1,oldOpening);
                ApplyRect(grid,ref storage,rect,1,opening);
                result.PreviousOpeningChange=hasOpening?(hadOpening?BlockerOpeningChange.Set:BlockerOpeningChange.Add):(hadOpening?BlockerOpeningChange.Remove:BlockerOpeningChange.None);
            }
            if(!rebuild&&history.FriendlyFaction!=byte.MaxValue)ApplyFriendlyPass(grid,ref storage,history.Rect,byte.MaxValue);
            if(faction!=byte.MaxValue)ApplyFriendlyPass(grid,ref storage,rect,faction);
            history=new BlockerHistory {Rect=rect,FriendlyFaction=faction,Owner=owner};return result;
        }
        public static BlockerUpdateResult ApplyRemoved<T>(in BlockerGrid grid,ref T storage,bool rebuild,in BlockerHistory history,bool hadOpening,in BlockerOpening oldOpening)where T:struct,IStaticBlockerStorage
        {
            if(!rebuild)ApplyRect(grid,ref storage,history.Rect,-1,oldOpening);
            if(!rebuild&&history.FriendlyFaction!=byte.MaxValue)ApplyFriendlyPass(grid,ref storage,history.Rect,byte.MaxValue);
            return new BlockerUpdateResult {Applied=true,PreviousOpeningChange=hadOpening?BlockerOpeningChange.Remove:BlockerOpeningChange.None};
        }
    }
}
