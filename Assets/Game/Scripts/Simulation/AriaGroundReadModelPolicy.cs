using System;using System.Collections.Generic;
namespace Game.Simulation {
// Plain published coordinates, not a replacement engine vector/API.
public readonly struct AriaGroundPosition {public readonly float X,Y,Z;public AriaGroundPosition(float x,float y,float z){X=x;Y=y;Z=z;}}
public readonly struct AriaAssaultDistances {
 public readonly AriaGroundPosition Nearest;public readonly float SquaredDistance,Distance;public readonly int Close,Assault;
 public readonly bool DistanceVisible,AssaultAtBase;
 public AriaAssaultDistances(AriaGroundPosition nearest,float squared,int close,int assault,int required){Nearest=nearest;SquaredDistance=squared;Close=close;Assault=assault;DistanceVisible=squared<float.MaxValue;Distance=DistanceVisible?(float)Math.Sqrt(squared):0f;AssaultAtBase=assault>=required;}
}
public static class AriaGroundApproachPolicy {
 public static float SquaredDistance(AriaGroundPosition a,AriaGroundPosition b)=>Squared(Difference(a,b));
 public static AriaAssaultDistances Observe(AriaGroundPosition objective,IReadOnlyList<AriaGroundPosition> publicPlayers,float enemyDisplayedHealth,int selectedCount){
  if(publicPlayers==null)throw new ArgumentNullException(nameof(publicPlayers));var nearest=objective;float distance=float.MaxValue;int close=0,assault=0;
  for(int i=0;i<publicPlayers.Count;i++){var p=publicPlayers[i];float d=Squared(Difference(p,objective));if(d<45*45)close++;if(d<50*50)assault++;if(d<distance){distance=d;nearest=p;}}
  int required=enemyDisplayedHealth>0&&enemyDisplayedHealth<=400?1:Math.Max(4,Math.Min(8,selectedCount));return new AriaAssaultDistances(nearest,distance,close,assault,required);
 }
 public static bool AdvancePreferred(bool industrialBasin,in AriaAssaultDistances distances)=>industrialBasin?!distances.AssaultAtBase&&distances.Close<8:distances.Close<4;
 public static AriaGroundPosition Approach(bool industrialBasin,AriaGroundPosition objective,AriaGroundPosition playerObjective,in AriaAssaultDistances distances){
  var nearest=distances.SquaredDistance==float.MaxValue?playerObjective:distances.Nearest;
  var delta=Difference(nearest,objective);delta=new AriaGroundPosition(delta.X,0,delta.Z);
  var normalized=Normalize(delta);float standoff=industrialBasin?30f:25f;return Sum(objective,Scale(normalized,standoff));
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
