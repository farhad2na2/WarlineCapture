using System; using Game.Components; using Game.Runtime; using Unity.Collections; using Unity.Entities;
internal static class S004CleanupBoundaryControl {
 static void Need(bool x,string why) {if(!x)throw new Exception(why);}
 static EntityManager Manager() {int id=500000; var em=new EntityManager { LogicalAllocatorAdmitted=true }; em.AllocateLogicalIdentity=()=>new Entity {Index=id++,Version=1}; return em;}
 static Entity Actor(EntityManager em,FixedString64Bytes id) {var e=em.CreateEntity(); em.AddComponentData(e,new SkirmishAttemptOwnedComponent {SessionId=id});return e;}
 static int Main() {try {
  var em=Manager(); var id=new FixedString64Bytes("cleanup-control"); var first=Actor(em,id); var second=Actor(em,id);
  em.AddComponentObject(second,new SkirmishVisualInstanceRecord()); bool rejected=false;
  try {SkirmishScenarioSpawnSystem.DestroyAttemptOwned(em,id);} catch(NotSupportedException) {rejected=true;}
  Need(rejected && em.Exists(first) && em.Exists(second),"Unsupported visual must reject before any actor cleanup");
  var safe=Manager(); var a=Actor(safe,id);var b=Actor(safe,id);
  SkirmishScenarioSpawnSystem.DestroyAttemptOwned(safe,id);
  Need(!safe.Exists(a) && !safe.Exists(b),"Original cleanup did not destroy source actors");
  Console.WriteLine("[S004CleanupBoundaryControl] result=Passed unsupportedVisualAtomic=1 ordinaryOriginalCleanup=1 nativeVisualParity=0");return 0;
 } catch(Exception e) {Console.Error.WriteLine(e);return 1;} }
}
