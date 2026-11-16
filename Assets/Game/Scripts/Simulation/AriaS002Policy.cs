using System;
namespace Game.Simulation {
public static class AriaS002Policy {
public static bool TryStep(in AriaS002Observation view,ref AriaS002PlanState plan,ref AriaS002ContactState touch,out AriaS002Decision output){
 if((view.Scope!=AriaS002Scope.NativeRendered&&view.Scope!=AriaS002Scope.LogicalPublicData)||
    view.SourceScenarioIndex!=4||!view.ExpandedSession||!view.ExpandedBaseAssault||view.AirProfile||view.CombinedArmsField){output=default;return false;}
 AriaGroundBaseAssaultPolicy.StepLegacyS002Observation(view,ref plan,ref touch,out output);return true;
}
// Managed validation boundary retained for existing callers. Burst owners use
// TryStep and handle rejection as an explicit status instead of an exception.
public static void Step(in AriaS002Observation view,ref AriaS002PlanState plan,ref AriaS002ContactState touch,out AriaS002Decision output){
 if(TryStep(view,ref plan,ref touch,out output)){AriaGroundBaseAssaultPolicy.ThrowManagedGuardFailure(output.ManagedGuardFailure);return;}
 if(view.Scope!=AriaS002Scope.NativeRendered&&view.Scope!=AriaS002Scope.LogicalPublicData)throw new ArgumentException("Explicit source observation scope required");
 throw new NotSupportedException("Only current source S002 ground BaseAssault data profile");
}
}
}
