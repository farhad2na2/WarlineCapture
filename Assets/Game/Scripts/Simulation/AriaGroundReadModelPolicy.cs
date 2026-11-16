using System;using System.Collections.Generic;
namespace Game.Simulation {
// Plain published coordinates, not a replacement engine vector/API.
public readonly struct AriaGroundPosition {public readonly float X,Y,Z;public AriaGroundPosition(float x,float y,float z){X=x;Y=y;Z=z;}}
public readonly struct AriaAssaultDistances {
 public readonly AriaGroundPosition Nearest;public readonly float SquaredDistance,Distance;public readonly int Close,Assault;
 public readonly bool DistanceVisible,AssaultAtBase;
 public AriaAssaultDistances(AriaGroundPosition nearest,float squared,int close,int assault,int required){Nearest=nearest;SquaredDistance=squared;Close=close;Assault=assault;DistanceVisible=squared<float.MaxValue;Distance=DistanceVisible?(float)Math.Sqrt(squared):0f;AssaultAtBase=assault>=required;}
}
public enum AriaGroundApproachKind : byte { Ordinary, CityCrossroads, IndustrialBasin, AuthoredLogicalGround }
public readonly struct AriaCityGroundApproach {
 public readonly AriaGroundPosition Flank,Goal;public readonly int Arrived;public readonly bool AdvancePreferred,FocusEnemy;
 public AriaCityGroundApproach(AriaGroundPosition flank,AriaGroundPosition goal,int arrived,bool advancePreferred,bool focusEnemy){Flank=flank;Goal=goal;Arrived=arrived;AdvancePreferred=advancePreferred;FocusEnemy=focusEnemy;}
}
public static class AriaGroundApproachPolicy {
 public static float SquaredDistance(AriaGroundPosition a,AriaGroundPosition b)=>Squared(Difference(a,b));
 public static AriaAssaultDistances Observe(AriaGroundPosition objective,IReadOnlyList<AriaGroundPosition> publicPlayers,float enemyDisplayedHealth,int selectedCount){
  if(publicPlayers==null)throw new ArgumentNullException(nameof(publicPlayers));var nearest=objective;float distance=float.MaxValue;int close=0,assault=0;
  for(int i=0;i<publicPlayers.Count;i++){var p=publicPlayers[i];float d=Squared(Difference(p,objective));if(d<45*45)close++;if(d<50*50)assault++;if(d<distance){distance=d;nearest=p;}}
  int required=enemyDisplayedHealth>0&&enemyDisplayedHealth<=400?1:Math.Max(4,Math.Min(8,selectedCount));return new AriaAssaultDistances(nearest,distance,close,assault,required);
 }
 // Own acquisition reach is planning evidence, never contact visibility or objective completion.
 public static Game.Skirmish.Contracts.SkirmishPublicSelectedApproach ObserveSelectedOwnForce(
  AriaGroundPosition publicObjective, IReadOnlyList<Game.Skirmish.Contracts.SkirmishPublicApproachMember> members){
  var result=new Game.Skirmish.Contracts.SkirmishPublicSelectedApproach();
  if(members==null||members.Count==0){result.UnavailableReason="No selected living own move force.";return result;}
  if(!Finite(publicObjective.X)||!Finite(publicObjective.Y)||!Finite(publicObjective.Z))throw new ArgumentException("Finite public objective required.");
  for(int i=0;i<members.Count;i++){
   var member=members[i];
   if(!Finite(member.X)||!Finite(member.Y)||!Finite(member.Z)||!Finite(member.AcquisitionRadius)||member.AcquisitionRadius<0)throw new ArgumentException("Finite own position and nonnegative original observer metric required.");
   if(member.StructureCapable)result.StructureCapableCount++;
  }
  result.ContextKnown=true;result.SelectedCount=members.Count;
  for(int i=0;i<members.Count;i++){
   var member=members[i];
   if(result.StructureCapableCount>0&&!member.StructureCapable)continue;
   result.RelevantCount++;
   float x=member.X-publicObjective.X,z=member.Z-publicObjective.Z;
   float distanceSquared=x*x+z*z,radiusSquared=member.AcquisitionRadius*member.AcquisitionRadius;
   if(!Finite(distanceSquared)||!Finite(radiusSquared))throw new ArgumentException("Own planning metric outside finite squared-coordinate domain.");
   if(member.AcquisitionRadius>0&&distanceSquared<=radiusSquared)result.InOwnAcquisitionRangeCount++;
  }
  // No visible/contact/arrived flag is emitted from this count.
  return result;
 }
 static bool Finite(float value)=>!float.IsNaN(value)&&!float.IsInfinity(value);
 // Geographic camera-planning fact only. It neither stamps an accepted order
 // nor completes a selected group/mission. Unseen or incapable cohorts never satisfy it.
 public static bool SelectedObjectiveContactReach(in Game.Skirmish.Contracts.SkirmishPublicSelectedApproach projection)=>
  projection.Required&&projection.ContextKnown&&projection.PublicObjectiveContactVisible&&projection.StructureCapableCount>0&&
  projection.RelevantCount==projection.StructureCapableCount&&projection.InOwnAcquisitionRangeCount==projection.RelevantCount;
 public static bool TrySelectedOwnApproach(in Game.Skirmish.Contracts.SkirmishPublicSelectedApproach projection,out AriaGroundPosition goal){
  goal=default;
  if(!projection.Required||!projection.ContextKnown||!projection.LegalGoalAvailable||!projection.LegalGoalWithinOwnAcquisitionRange)return false;
  if(!Finite(projection.GoalX)||!Finite(projection.GoalY)||!Finite(projection.GoalZ))throw new ArgumentException("Finite original legal planning goal required.");
  goal=new AriaGroundPosition(projection.GoalX,projection.GoalY,projection.GoalZ);return true;
 }
 public static bool AdvancePreferred(bool industrialBasin,in AriaAssaultDistances distances)=>industrialBasin?!distances.AssaultAtBase&&distances.Close<8:distances.Close<4;
 public static AriaGroundPosition Approach(bool industrialBasin,AriaGroundPosition objective,AriaGroundPosition playerObjective,in AriaAssaultDistances distances){
  var nearest=distances.SquaredDistance==float.MaxValue?playerObjective:distances.Nearest;
  var delta=Difference(nearest,objective);delta=new AriaGroundPosition(delta.X,0,delta.Z);
  var normalized=Normalize(delta);float standoff=industrialBasin?30f:25f;return Sum(objective,Scale(normalized,standoff));
 }
 public static AriaCityGroundApproach City(AriaGroundPosition objective,AriaGroundPosition playerObjective,IReadOnlyList<AriaGroundPosition> sourcePlayerPositions,bool assaultAtBase,ref bool flankReached){
  if(sourcePlayerPositions==null)throw new ArgumentNullException(nameof(sourcePlayerPositions));
  var forward=Difference(objective,playerObjective);forward=new AriaGroundPosition(forward.X,0,forward.Z);forward=Normalize(forward);
  var side=new AriaGroundPosition(-forward.Z,0,forward.X);
  var flank=Sum(Difference(objective,Scale(forward,50f)),Scale(side,70f));int arrived=0;
  for(int i=0;i<sourcePlayerPositions.Count;i++)if(Squared(Difference(sourcePlayerPositions[i],flank))<35*35)arrived++;
  if(arrived>=4)flankReached=true;
  var goal=flankReached?Sum(objective,Scale(side,20f)):flank;
  return new AriaCityGroundApproach(flank,goal,arrived,!assaultAtBase,flankReached);
 }
 // Preserve the original Vector3 operator/field boundaries: scalar fusion can
 // retain wider intermediates on the admitted Mono -float32 lane.
 [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
 static AriaGroundPosition Difference(AriaGroundPosition a,AriaGroundPosition b)=>new AriaGroundPosition(a.X-b.X,a.Y-b.Y,a.Z-b.Z);
 [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
 static float Squared(AriaGroundPosition p)=>p.X*p.X+p.Y*p.Y+p.Z*p.Z;
 [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
 static AriaGroundPosition Normalize(AriaGroundPosition p){float magnitude=(float)Math.Sqrt(p.X*p.X+p.Y*p.Y+p.Z*p.Z);return magnitude>0.00001f?new AriaGroundPosition(p.X/magnitude,p.Y/magnitude,p.Z/magnitude):default;}
 [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
 static AriaGroundPosition Scale(AriaGroundPosition p,float scale)=>new AriaGroundPosition(p.X*scale,p.Y*scale,p.Z*scale);
 [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
 static AriaGroundPosition Sum(AriaGroundPosition a,AriaGroundPosition b)=>new AriaGroundPosition(a.X+b.X,a.Y+b.Y,a.Z+b.Z);

}
public static class AriaPublicPlayerMarkerPolicy {
 public const int MaxMarkers=1024;public const double RefreshIntervalSeconds=0.2d;
 // Exact ordinary player pass of MatchHudMinimapMarkerSystem; Campaign/defense
 // roster and enemy intel remain with their original native owner.
 public static bool Include(int count,int maximum,byte faction,bool owned,bool disabled,bool passenger,bool passengerOwner,bool landed,int currentHealth)
  =>count<maximum&&faction==1&&!(!owned&&disabled)&&!(owned&&(passenger||passengerOwner&&!landed))&&currentHealth>0&&!disabled;
}
}
