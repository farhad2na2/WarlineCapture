using System;
namespace Game.Simulation {
public static class AriaS002Policy {
public static void Step(in AriaS002Observation view,ref AriaS002PlanState plan,ref AriaS002ContactState touch,out AriaS002Decision output){
 if(view.Scope!=AriaS002Scope.NativeRendered&&view.Scope!=AriaS002Scope.LogicalPublicData)throw new ArgumentException("Explicit source observation scope required");
 if(view.SourceScenarioIndex!=4||!view.ExpandedSession||!view.ExpandedBaseAssault||view.AirProfile||view.CombinedArmsField)throw new NotSupportedException("Only current source S002 ground BaseAssault data profile");
 AriaGroundBaseAssaultPolicy.StepLegacyS002Observation(view,ref plan,ref touch,out output);
}
}
}
