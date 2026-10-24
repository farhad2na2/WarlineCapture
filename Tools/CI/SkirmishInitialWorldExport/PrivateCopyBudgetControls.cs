using System;using Game.Runtime;
static class PrivateCopyBudgetControls
{
 static int count;static void Need(bool yes,string name){if(!yes)throw new Exception(name);count++;Console.WriteLine("PASS "+name);}static void Refuse(Action action,string name){bool refused=false;try{action();}catch(InvalidOperationException){refused=true;}Need(refused,name);}
 internal static void Run()
 {
  count=0;var exact=new S002InitialWorldPrivateCopyBudget.Ledger(64);exact.Charge(8,4,8,"exact");Need(exact.Used==64,"actual-size byte charge admits exact boundary including header");Refuse(()=>exact.Charge(0,1,0,"aggregate",1),"aggregate one-byte overflow rejected without allocation");Need(exact.Used==64,"rejected charge cannot consume or reset credit");
  var guard=new S002InitialWorldPrivateCopyBudget.Ledger(128);Refuse(()=>guard.Charge(3,4,2,"count"),"oversize count rejects before copy");Refuse(()=>guard.Charge(-1,4,2,"negative"),"negative native length rejects");Refuse(()=>guard.Charge(1,0,2,"size"),"invalid element size rejects");Refuse(()=>guard.Charge(long.MaxValue,int.MaxValue,long.MaxValue,"overflow"),"checked length-times-element arithmetic refuses overflow");Need(guard.Used==0,"all rejected pre-copy bounds leave empty ledger");
  Refuse(()=>S002InitialWorldPrivateCopyBudget.Bound(S002InitialWorldPrivateCopyBudget.MaximumEntities+1L,S002InitialWorldPrivateCopyBudget.MaximumEntities,"entity"),"actual entity bound refuses over-limit count");Refuse(()=>S002InitialWorldPrivateCopyBudget.Bound(S002InitialWorldPrivateCopyBudget.MaximumMapCapacity+1L,S002InitialWorldPrivateCopyBudget.MaximumMapCapacity,"capacity"),"native map capacity bound refuses over-limit capacity");
  Refuse(()=>S002InitialWorldPrivateCopyBudget.Charge(0,1,1,"no-scope"),"copy credit unavailable outside active scope");
  using(S002InitialWorldPrivateCopyBudget.Begin())
  {S002InitialWorldPrivateCopyBudget.Charge(S002InitialWorldPrivateCopyBudget.MaximumBytes-32,1,S002InitialWorldPrivateCopyBudget.MaximumBytes,"outer");using(S002InitialWorldPrivateCopyBudget.Begin())Refuse(()=>S002InitialWorldPrivateCopyBudget.Charge(0,1,0,"nested",1),"nested path/occupancy scope shares exhausted aggregate");Refuse(()=>S002InitialWorldPrivateCopyBudget.Charge(0,1,0,"after-inner",1),"disposing nested scope cannot reset outer credit");}
  using(S002InitialWorldPrivateCopyBudget.Begin()){S002InitialWorldPrivateCopyBudget.Charge(0,1,0,"new",1);Need(true,"new standalone capture receives fresh ledger");}
  var outer=S002InitialWorldPrivateCopyBudget.Begin();var inner=S002InitialWorldPrivateCopyBudget.Begin();Refuse(()=>outer.Dispose(),"out-of-order scope disposal rejected");inner.Dispose();outer.Dispose();using(S002InitialWorldPrivateCopyBudget.Begin()){S002InitialWorldPrivateCopyBudget.Charge(0,1,0,"recovered",1);Need(true,"correct stack disposal restores standalone scope");}
  Console.WriteLine("[InitialWorldPrivateCopyBudget] result=Passed checks="+count+" actualSource=True nativeCopies=UNRUN");
 }
}
