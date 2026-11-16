using Game.UI.Contracts;
using Game.Skirmish.Contracts;
namespace Game.UI.Runtime {
 public static partial class UiShellRuntimeGateway {
  public static bool TryReadAcceptedAssaultGoal(SkirmishPublicArmyOrderGoalKind kind,int publicObjectiveIdentity,float x,float z,out SkirmishPublicArmyOrderGoal goal,out int acceptedMask){goal=default;acceptedMask=0;return current is IUiSkirmishAcceptedGoalGateway gateway&&gateway.TryReadAcceptedAssaultGoal(kind,publicObjectiveIdentity,x,z,out goal,out acceptedMask);}
 }
}
