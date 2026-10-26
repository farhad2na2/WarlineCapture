using Game.UI.Contracts;
using Game.Skirmish.Contracts;
using Game.Runtime;
using Unity.Entities;
namespace Game.UI.Shell.Ecs {
 public sealed partial class UiShellEcsGateway:IUiSkirmishAcceptedGoalGateway {
  bool IUiSkirmishAcceptedGoalGateway.TryReadAcceptedAssaultGoal(SkirmishPublicArmyOrderGoalKind kind,int publicObjectiveIdentity,float x,float z,out SkirmishPublicArmyOrderGoal goal,out int acceptedMask){
   goal=default;acceptedMask=0;
   if(!TrySkirmish(out EntityManager em,out Entity session,out _)||publicObjectiveIdentity==0||!SkirmishExpandedPresentedOrders.TryReadPage(em,session,out _,out _,out _,out _,out _,out _,out _,out _,out var a,out var b,out var c,out var d,out var e))return false;
   goal=SkirmishAcceptedArmyOrders.PublicGoal(em,session,kind,publicObjectiveIdentity,x,z);
   int bare=(a.AttackOrdered?1:0)|(b.AttackOrdered?2:0)|(c.AttackOrdered?4:0)|(d.AttackOrdered?8:0)|(e.AttackOrdered?16:0);
   acceptedMask=SkirmishAcceptedArmyOrders.ReadMask(em,session,goal,bare,a.GroupId,b.GroupId,c.GroupId,d.GroupId,e.GroupId);return true;
  }
 }
}
