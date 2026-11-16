using Game.Skirmish.Contracts;
namespace Game.UI.Contracts {
 public interface IUiSkirmishAcceptedGoalGateway {
  bool TryReadAcceptedAssaultGoal(SkirmishPublicArmyOrderGoalKind kind,int publicObjectiveIdentity,float x,float z,out SkirmishPublicArmyOrderGoal goal,out int acceptedMask);
 }
}
