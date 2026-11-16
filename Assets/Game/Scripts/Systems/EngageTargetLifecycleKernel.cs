namespace Game.Runtime
{
    public struct EngageValidationFacts
    {
        public bool TargetNull,TargetHasTransform,TargetHasHealth;
        public int TargetHealth;
        public bool PairedFog,SameFogSession,FogVisible;
    }
    public struct EngageInvalidationEffects { public bool ReturningHome,RestorePathRequest; }
    public struct EngagementTargetState
    {
        public int TargetIndex,TargetVersion,CellX,CellZ;
        public float X,Y,Z;
        public byte IsCommanded;
    }
    public struct EngagementSyncResult { public EngagementTargetState State; public bool RemoveAtEndSimulation; }
    // Validation removal is immediate local ECB; synchronization removal belongs to
    // EndSimulation. Keep those two publication boundaries distinct in every host.
    public static class EngageTargetLifecycleKernel
    {
        public static bool IsInvalid(in EngageValidationFacts facts)=>
            facts.TargetNull||!facts.TargetHasTransform||(facts.TargetHasHealth&&facts.TargetHealth<=0)||
            (facts.PairedFog&&(!facts.SameFogSession||!facts.FogVisible));
        public static EngageInvalidationEffects PlanInvalidation(bool hasAir,bool hasPathRequest,bool hasUnitTarget)=>
            new EngageInvalidationEffects{ReturningHome=hasAir,RestorePathRequest=!hasAir&&!hasPathRequest&&hasUnitTarget};
        public static EngagementSyncResult Sync(in EngagementTargetState original,bool targetHasTransform,
            float targetX,float targetY,float targetZ,int targetCellX,int targetCellZ)
        {
            var result=new EngagementSyncResult{State=original};
            if((original.TargetIndex==0&&original.TargetVersion==0)||!targetHasTransform)
            {
                result.State.TargetIndex=0;result.State.TargetVersion=0;result.State.CellX=0;result.State.CellZ=0;
                result.State.X=0;result.State.Y=0;result.State.Z=0;result.RemoveAtEndSimulation=true;
            }
            else
            {
                result.State.X=targetX;result.State.Y=targetY;result.State.Z=targetZ;
                result.State.CellX=targetCellX;result.State.CellZ=targetCellZ;
            }
            return result;
        }
    }
}
