using System;using System.Linq;using System.Collections.Generic;using System.Globalization;using System.Text.RegularExpressions;using Newtonsoft.Json.Linq;using Unity.Entities;using Unity.Mathematics;using Game.Configs;using Game.Skirmish.Contracts;
namespace Game.Runtime{
 // Logical source-handler invocation. Parent binds/revalidates the separate public UI
 // before release. This class does NOT certify rendered card/hit-test availability.
 internal sealed class SourceCommandDispatch{
  readonly EntityManager em;readonly Entity session;readonly SkirmishArmyProfileConfig army;readonly Dictionary<string,Entry> receipts=new Dictionary<string,Entry>(StringComparer.Ordinal);sealed class Entry{internal string Control;internal long Tick;internal JObject Result;}
  internal SourceCommandDispatch(EntityManager manager,Entity initialSession,SkirmishArmyProfileConfig initialArmy){em=manager??throw new ArgumentNullException();session=initialSession;army=initialArmy??throw new ArgumentNullException();}
  static uint U(string s){uint n;if(!uint.TryParse(s,NumberStyles.None,CultureInfo.InvariantCulture,out n)||n==0||s!=n.ToString(CultureInfo.InvariantCulture))throw new ArgumentException("Canonical positive group id required");return n;}
  static int I(string s,int low,int high){int n;if(!int.TryParse(s,NumberStyles.Integer,CultureInfo.InvariantCulture,out n)||n<low||n>high||s!=n.ToString(CultureInfo.InvariantCulture))throw new ArgumentException("Logical argument bound");return n;}
  static float F(string s){if(s.Length!=8||!s.All(c=>c>='0'&&c<='9'||c>='a'&&c<='f'))throw new ArgumentException("Exact lowercase f32 bits required");float f=BitConverter.ToSingle(BitConverter.GetBytes(uint.Parse(s,NumberStyles.HexNumber,CultureInfo.InvariantCulture)),0);if(float.IsNaN(f)||float.IsInfinity(f))throw new ArgumentException("Finite source move destination required");return f;}
  internal JObject Dispatch(string worldId,long tick,string request,string control,long expectedTick){if(request==null||request.Length<1||request.Length>128||!Regex.IsMatch(request,"^[A-Za-z0-9_.:-]+$")||control==null||control.Length>512)throw new ArgumentException("Bounded command identity required");if(receipts.TryGetValue(request,out var previous)){if(previous.Control!=control||previous.Tick!=expectedTick)throw new InvalidOperationException("Command identity reused for different control/tick");return(JObject)previous.Result.DeepClone();}if(expectedTick!=tick)throw new InvalidOperationException("Stale command binding tick");if(receipts.Count>=65536)throw new NotSupportedException("Command receipt domain bound");
   bool accepted;string api,reason;JObject source;string[] p=control.Split('/');
   if(p.Length>=2&&p[0]=="presented"){
    api="SkirmishExpandedPresentedOrders.";if(p.Length==3&&p[1]=="group"){api+="TryPresentedGroup";accepted=SkirmishExpandedPresentedOrders.TryPresentedGroup(em,session,U(p[2]));}
    else if(p.Length==3&&p[1]=="toggle"){api+="TryTogglePresentedGroup";accepted=SkirmishExpandedPresentedOrders.TryTogglePresentedGroup(em,session,U(p[2]));}
    else if(p.Length==3&&p[1]=="slot"){api+="TryPresentedSlot";accepted=SkirmishExpandedPresentedOrders.TryPresentedSlot(em,session,I(p[2],0,SkirmishExpandedPresentedOrders.PresentedSlots-1));}
    else if(p.Length==3&&p[1]=="page"&&(p[2]=="+1"||p[2]=="-1")){api+="TryChangePage";accepted=SkirmishExpandedPresentedOrders.TryChangePage(em,session,p[2]=="+1"?1:-1);}
    else if(p.Length==2&&p[1]=="clear"){api+="TryClearSelection";accepted=SkirmishExpandedPresentedOrders.TryClearSelection(em,session);}
    else if(p.Length==2&&p[1]=="hold"){api+="TryHoldSelection";accepted=SkirmishExpandedPresentedOrders.TryHoldSelection(em,session);}
    else throw new NotSupportedException("Unknown logical presented control");reason=accepted?"SourceReturnedTrue":"SourceReturnedFalse";source=new JObject{["source_return"]=accepted};
   }else if(p.Length>=2&&p[0]=="army"){
    SkirmishCommandDecision decision;if(p.Length==2&&p[1]=="move-default"){api="SkirmishArmyCommandService.TryMove(defaultAdvance)";accepted=SkirmishArmyCommandService.TryMove(em,session,out decision);}
    else if(p.Length==5&&p[1]=="move"){api="SkirmishArmyCommandService.TryMove(destination)";accepted=SkirmishArmyCommandService.TryMove(em,session,new float3(F(p[2]),F(p[3]),F(p[4])),out decision);}
    else if(p.Length==3&&p[1]=="attack"){api="SkirmishArmyCommandService.TryAttack";accepted=SkirmishArmyCommandService.TryAttack(em,session,ExactInitialValueCodec.EntityId(p[2]),out decision);}
    else throw new NotSupportedException("Unknown logical army control");reason=decision.Reason.ToString();source=JObject.FromObject(decision);
   }else if(p.Length==4&&p[0]=="production"&&p[1]=="recruit"){
    if(!Regex.IsMatch(p[2],"^[A-Za-z0-9_.:-]{1,128}$"))throw new ArgumentException("Bounded source role id");var catalog=PublicCommandCatalog.Read(em,session,army);if(!catalog.Recruitment.Any(x=>x.RoleId==p[2]))throw new NotSupportedException("Role absent from actual source role catalog");api="SkirmishProductionService.TryQueue";accepted=SkirmishProductionService.TryQueue(em,session,p[2],I(p[3],1,16),army,out var decision);reason=decision.Reason.ToString();source=JObject.FromObject(decision);
   }else throw new NotSupportedException("Unknown/unimplemented source logical control");
   var result=new JObject{["world_id"]=worldId,["tick"]=tick,["command_request_id"]=request,["receipt_id"]=worldId+":"+request,["control_id"]=control,["accepted"]=accepted,["supported"]=true,["reason"]=reason,["source_api"]=api,["source_result"]=source,["qualification"]="SOURCE_HANDLER_ONLY",["native_ui_binding"]="UNKNOWN"};receipts.Add(request,new Entry{Control=control,Tick=expectedTick,Result=(JObject)result.DeepClone()});return result;
  }
 }
}
