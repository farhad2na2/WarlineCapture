using System;using Game.Components;using Game.Configs;using Game.Simulation;using Game.Skirmish.Contracts;using Unity.Entities;using Unity.Mathematics;using Newtonsoft.Json.Linq;
namespace Game.Runtime {
// One source dispatcher, explicit logical-public-data transport. No rendered/pixel/hardware claim.
internal sealed class SourceAriaS002Controller {
 readonly EntityManager em;readonly Entity session;readonly SkirmishArmyProfileConfig army;readonly string worldId;readonly SourceCommandDispatch commands;readonly SourcePublicPlayerContactOwner contacts;readonly SourceAriaObjectiveApproach approach;readonly SourceAriaRecruitmentBoundary paid;readonly SourceS002NumericCameraProvider numericCamera;
 readonly GroundMissionContext groundContext;
 AriaS002PlanState plan;AriaS002ContactState contact;AriaS002CommandReceipt commandReceipt;Entity lastObjective;int objectiveIdentity,commandSequence;long lastTick=-1;float lastTime=float.NegativeInfinity;bool busy,stopped,inspectionOpen,selectionMode,attackMode;
 PublicCommandCatalog.View current;Entity objectiveTarget;SourceAriaHomeThreat? homeThreat;SourceAriaPublicApproach? terrain;uint toggleGroup;int lastCommandFrame;uint lastCommandGroup;Entity lastCommandTarget;string lastCommandId;
 internal SourceAriaS002Controller(EntityManager em,Entity session,SkirmishArmyProfileConfig army,string worldId,SourceAriaRecruitmentBoundary paid,int sourceScenarioIndex,GroundMissionContext groundContext,SourceS002NumericCameraProvider numericCamera=null){
  this.em=em??throw new ArgumentNullException(nameof(em));this.session=session;this.army=army??throw new ArgumentNullException(nameof(army));this.worldId=worldId??throw new ArgumentNullException(nameof(worldId));this.numericCamera=numericCamera;this.paid=paid??throw new NotSupportedException("Complete real source paid catalog/availability context required before full strategy starts");
  if(!ReferenceEquals(paid.WorldIdentity,em)||paid.SessionIdentity!=session||sourceScenarioIndex!=groundContext.SourcePresetLoadIndex||!em.BankBound)throw new NotSupportedException("Same actual source world/session/canonical bank/paid context required");
  this.groundContext=groundContext;SourceGroundMissionFamily.RequireLogicalContext(groundContext,SourceGroundMissionFamily.RequireSession(em,session),em.GetComponentData<SkirmishExpandedSessionComponent>(session),em.GetComponentData<SkirmishMatchState>(session).ScenarioIndex);if(groundContext.SourcePresetLoadIndex!=sourceScenarioIndex)throw new NotSupportedException("Actual original source-preset selection differs");
  contacts=new SourcePublicPlayerContactOwner(em,session);approach=new SourceAriaObjectiveApproach(em,session,contacts,groundContext);commands=new SourceCommandDispatch(em,session,army);contact.Phase=AriaS002ContactPhase.Observing;Scope();
 }
 void Scope(){var s=em.GetComponentData<SkirmishExpandedSessionComponent>(session);var setup=em.GetComponentObject<SkirmishResolvedSetupRecord>(session).Setup;SourceGroundMissionFamily.RequireSession(em,session);
  if(!GroundMissionContextFactory.MatchesLogicalSession(groundContext,setup,s,em.GetComponentData<SkirmishMatchState>(session).ScenarioIndex))throw new NotSupportedException("Actual compiled ground context/session changed");
  if(!em.HasComponent<SkirmishBaseAssaultFactComponent>(session)||!em.HasComponent<SkirmishMatchState>(session)||!em.HasComponent<SkirmishCapacityComponent>(session)||!em.HasComponent<SkirmishObjectiveClockComponent>(session))throw new NotSupportedException("Genuine source objective/public capacity/clock projection required before ARIA");
 }
 static AriaS002ActionTarget Cap(AriaS002Action action,int id,bool admitted,int slot=-1)=>new AriaS002ActionTarget{Action=action,Id=id,Admitted=admitted,Slot=slot};
 bool Recruit(SkirmishRoleKind role,bool sourceAffordability){if(!sourceAffordability)return false;return paid.Read(role).CanRequest;}
 AriaS002Observation Read(int frame,float time){
  Scope();current=PublicCommandCatalog.Read(em,session,army);var v=current.PublicView;var p=current.Page;if(v.AirProfile||current.CombinedArmsField||!v.ReadinessEligible||!p.Expanded||!p.BaseAssault)throw new NotSupportedException("Complete actual ground BaseAssault public page required");
  if(!current.Active&&!v.Finished)throw new NotSupportedException("Inactive actual source session");
  var bases=SkirmishPublicObjectiveProjection.Read(em,session);if(!v.Finished&&!bases.PlayerPositionKnown)throw new NotSupportedException("Original objective/HUD publication must precede ARIA");
  contacts.UpdateAfterFog(frame,time);var snapshot=contacts.ReadSnapshot();var match=em.GetComponentData<SkirmishMatchState>(session);if(match.EnemyMainBase!=lastObjective){lastObjective=match.EnemyMainBase;objectiveIdentity=checked(objectiveIdentity+1);}
  objectiveTarget=Entity.Null;foreach(var t in current.Targets)if(t.Entity==match.EnemyMainBase){if(!t.PublicKnowledge.HasValue||t.PublicKnowledge.Value.Sight!=SkirmishContactSight.Visible)throw new NotSupportedException("Genuine current visible objective knowledge required");objectiveTarget=t.Entity;break;}
  homeThreat=v.Finished?null:SourceAriaPublicHomeThreat.Read(em,session,current);terrain=bases.EnemyObjectiveAlive?approach.Read():null;
  int selectedMembers=0;toggleGroup=0;foreach(var g in current.Groups)if(g.Selected!=0){if(toggleGroup==0)toggleGroup=g.GroupId;selectedMembers=checked(selectedMembers+g.AliveCount);}
  var distances=AriaGroundApproachPolicy.Observe(bases.EnemyPosition,snapshot.Players,current.EnemyBaseHealth.DisplayedCurrent,selectedMembers);
  var facts=new AriaS002Observation{Scope=AriaS002Scope.LogicalPublicData,SourceScenarioIndex=groundContext.PlayableScenarioIndex,Active=current.Active,Finished=v.Finished,ExpandedSession=true,ExpandedBaseAssault=p.BaseAssault,Frame=frame,Time=time,
   PlayerDesignatedAlive=v.PlayerDesignatedAlive,EnemyDesignatedAlive=v.EnemyDesignatedAlive,Infantry=v.OwnInfantry,PlayerHealth=current.PlayerBaseHealth.DisplayedCurrent,EnemyHealth=current.EnemyBaseHealth.DisplayedCurrent,
   AirProfile=v.AirProfile,CombinedArmsField=current.CombinedArmsField,OwnMaterials=v.OwnMaterials,ReadinessEligible=v.ReadinessEligible,PadPresent=v.PadPresent,AirQueueOffered=v.AirQueueOffered,OwnAttackAirLive=v.OwnAttackAirLive,
   LogisticsTruckCommitted=v.LogisticsTruckCommitted,CanAffordLogisticsTruck=v.CanAffordLogisticsTruck,TankersCommitted=v.TankersCommitted,CanAffordTanker=v.CanAffordTanker,
   RifleRecruitPending=v.RifleRecruitPending,RocketeerRecruitPending=v.RocketeerRecruitPending,CanAffordRifle=v.CanAffordRifle,CanAffordRocketeer=v.CanAffordRocketeer,CanAffordAntiAir=v.CanAffordAntiAir,AntiAirCommitted=v.AntiAirCommitted,VisibleHostileAir=v.VisibleHostileAir,OwnStructureAttackUnits=v.OwnStructureAttackUnits,
   CanUpgradeReadiness=SourceS002ReadinessGateway.Read(em,session,out var sourceReadiness)&&sourceReadiness.CanUpgrade,CanBuildAirPad=v.CanBuildAirPad,AirRecruitPending=v.AirRecruitPending,PublicHomeThreat=homeThreat.HasValue,SelectionVisible=current.HasSelectedLivingPlayerMember,SelectionMode=selectionMode,AttackMode=attackMode,InspectionOpen=inspectionOpen,
   ExpandedPageIndex=p.PageIndex,ExpandedPageCount=p.PageCount,ExpandedTotalGroups=p.TotalGroups,ExpandedSelectedGroups=p.SelectedGroups,ExpandedSelectedOffPage=p.SelectedOffPage,ExpandedRosterRevision=p.RosterRevision,
   ExpandedAssaultMask=p.AssaultMask,ExpandedSelectedMask=p.SelectedMask,ExpandedStructureMask=p.StructureMask,ExpandedAttackOrderMask=p.AttackOrderMask,ExpandedAirMask=p.AirMask,ExpandedCanPreviousPage=p.PreviousPage,ExpandedCanNextPage=p.NextPage,
   ExpandedGroup0=p.Group0,ExpandedGroup1=p.Group1,ExpandedGroup2=p.Group2,ExpandedGroup3=p.Group3,ExpandedGroup4=p.Group4,SelectedCount=selectedMembers,PrecisionAttack=commandReceipt,
   AssaultAtBase=distances.AssaultAtBase,AssaultDistanceKnown=bases.EnemyObjectiveAlive&&distances.DistanceVisible,NearestFriendlyObjectiveDistance=distances.Distance,
   PublicSnapshot=new AriaS002PublicSnapshot{Identity=snapshot.Identity,MarkerRevision=snapshot.Generation,MarkerFrame=snapshot.Frame,MarkerTime=snapshot.AriaTime,ObjectiveHudIdentity=objectiveIdentity,ObjectiveWorld=bases.EnemyPosition,OwnedIdentity=true,InspectionScopeOpen=inspectionOpen,Complete=true,SourceScopeAdmitted=bases.EnemyObjectiveAlive,OpenMap=Cap(AriaS002Action.OpenInspection,-20010,!inspectionOpen&&bases.EnemyObjectiveAlive)},
   FocusPlayer=Cap(AriaS002Action.InspectHome,90001,bases.PlayerPositionKnown),FocusEnemy=Cap(AriaS002Action.InspectObjective,90002,bases.EnemyObjectiveAlive),FocusAdvance=Cap(AriaS002Action.InspectObjective,-20011,bases.EnemyObjectiveAlive),
   CloseMap=Cap(AriaS002Action.CloseInspection,90003,inspectionOpen),Select=Cap(AriaS002Action.SelectMode,90004,true),
   PreviousSquadPage=Cap(AriaS002Action.PreviousPage,90005,p.PreviousPage),NextSquadPage=Cap(AriaS002Action.NextPage,90006,p.NextPage),SelectedSquadToToggle=Cap(AriaS002Action.ToggleSelected,90007,toggleGroup!=0),
   Attack=Cap(AriaS002Action.ArmAttack,90008,current.HasSelectedLivingPlayerMember),Hold=Cap(AriaS002Action.Hold,90009,current.HasSelectedLivingPlayerMember),
   EnemyBase=Cap(AriaS002Action.AttackVisibleObjective,-20001,objectiveTarget!=Entity.Null),AdvanceGround=Cap(AriaS002Action.AttackPublicTerrain,90010,terrain.HasValue),
   ThreatGround=Cap(AriaS002Action.AttackPublicTerrain,90011,homeThreat.HasValue),Threat=Cap(AriaS002Action.AttackVisibleObjective,90012,homeThreat.HasValue),
   Recruit=Cap(AriaS002Action.RecruitRifle,90013,Recruit(SkirmishRoleKind.Rifle,v.CanAffordRifle)),RecruitAntiAir=Cap(AriaS002Action.RecruitAntiAir,90014,Recruit(SkirmishRoleKind.AntiAir,v.CanAffordAntiAir)),
   RecruitRocketeer=Cap(AriaS002Action.RecruitRocketeer,90015,Recruit(SkirmishRoleKind.Rocketeer,v.CanAffordRocketeer))
  };
  // Logical profile has no catalog popup or placement/camera UI. Those capabilities are absent;
  // native observations continue to use their real presentation owners. No rendered flags are set.
  facts.Squad0=Cap(AriaS002Action.SelectSquad,90100,p.Group0!=0,0);facts.Squad1=Cap(AriaS002Action.SelectSquad,90101,p.Group1!=0,1);facts.Squad2=Cap(AriaS002Action.SelectSquad,90102,p.Group2!=0,2);facts.Squad3=Cap(AriaS002Action.SelectSquad,90103,p.Group3!=0,3);facts.Squad4=Cap(AriaS002Action.SelectSquad,90104,p.Group4!=0,4);
  if(facts.CanBuildAirPad||facts.AirQueueOffered||facts.OwnAttackAirLive!=0)throw new NotSupportedException("Active air/infrastructure read domain is not this profile");
  return facts;
 }
 void Complete(in AriaS002Decision d,int frame,float time){contact.TargetId=d.TargetId;contact.Target=contact.Contact=d.Position;SourceS002ContactCompletion.Complete(ref contact,frame,time,true);}
 string Request()=> "aria-s002-"+checked(++commandSequence);
 JObject Dispatch(long tick,string control){return commands.Dispatch(worldId,tick,Request(),control,tick);}
 JObject Execute(in AriaS002Decision d,long tick,int frame,float time,float productionTime){
  JObject receipt;switch(d.Action){
   case AriaS002Action.InspectHome:case AriaS002Action.InspectObjective:
    var publicFacts=SkirmishPublicObjectiveProjection.Read(em,session);contacts.ReadSnapshot();
    if(numericCamera==null||!numericCamera.SnapshotAvailable)throw new NotSupportedException("Actual admitted numeric camera required for source public inspection effect");
    var publicPoint=d.Action==AriaS002Action.InspectHome?publicFacts.PlayerPosition:publicFacts.EnemyPosition;
    if(d.Action==AriaS002Action.InspectHome?!publicFacts.PlayerPositionKnown:!publicFacts.EnemyObjectiveAlive)throw new InvalidOperationException("Source public objective camera target expired");
    numericCamera.InspectPublicObjective(new float3(publicPoint.X,publicPoint.Y,publicPoint.Z));
    return new JObject{["inspection_completed"]=true,["camera_request"]="OriginalPublicObjectivePositionOnlySmoothFocus",["camera_smooth_seconds"]=.35f,["troop_order"]=false,["source_api"]="actual public objective/contact snapshot"};
   case AriaS002Action.InspectCohort:throw new NotSupportedException("Original cohort focus target/camera request data not admitted");
   case AriaS002Action.OpenInspection:numericCamera?.ObserveLogicalCameraAction("OpenMap");contacts.ReadSnapshot();inspectionOpen=true;return new JObject{["inspection_completed"]=true,["troop_order"]=false,["data_scope_open"]=true};
   case AriaS002Action.CloseInspection:numericCamera?.ObserveLogicalCameraAction("CloseMap");inspectionOpen=false;return new JObject{["inspection_completed"]=true,["troop_order"]=false,["data_scope_open"]=false};
   case AriaS002Action.SelectMode:selectionMode=true;return new JObject{["logical_selection_mode"]=true,["troop_order"]=false};
   case AriaS002Action.SelectSquad:return Dispatch(tick,"presented/slot/"+d.Slot);
   case AriaS002Action.PreviousPage:return Dispatch(tick,"presented/page/-1");
   case AriaS002Action.NextPage:return Dispatch(tick,"presented/page/+1");
   case AriaS002Action.ToggleSelected:if(toggleGroup==0)throw new InvalidOperationException("Source selected group expired");return Dispatch(tick,"presented/toggle/"+toggleGroup);
   case AriaS002Action.Hold:return Dispatch(tick,"presented/hold");
   case AriaS002Action.ArmAttack:attackMode=true;return new JObject{["logical_attack_mode"]=true,["troop_order"]=false};
   case AriaS002Action.AttackVisibleObjective:
    if(!attackMode||!current.HasSelectedLivingPlayerMember)throw new InvalidOperationException("Source attack mode/selection changed");
    Entity target=d.TargetId==-20001?objectiveTarget:homeThreat?.Entity??Entity.Null;if(target==Entity.Null)throw new InvalidOperationException("Actual source visible target expired");
    uint group=current.Page.SelectedGroups==1&&current.Page.SelectedOffPage==0?SelectedGroup():0;
    int expectedMembers=plan.PrecisionStage==4?ValidatePrecisionGroup(group):0;
    receipt=Dispatch(tick,"army/attack/"+target.Index+":"+target.Version);attackMode=false;
    if(d.TargetId==-20001){var actual=receipt["source_result"]as JObject;int issued=actual?["IssuedCount"]?.Value<int>()??0,excluded=actual?["ExcludedCount"]?.Value<int>()??0;
     if(plan.PrecisionStage==4&&issued+excluded!=expectedMembers)throw new InvalidOperationException("Actual source member result differs from admitted selection");
     if(issued<0||excluded<0)throw new InvalidOperationException("Malformed actual source member counts");
     lastCommandId=receipt["command_request_id"].Value<string>();lastCommandFrame=frame;lastCommandGroup=group;lastCommandTarget=target;
     commandReceipt=new AriaS002CommandReceipt{Scope=AriaS002Scope.LogicalPublicData,ResultKnown=true,Revision=checked(commandReceipt.Revision+1),GroupId=group,Frame=frame,RequestId=commandSequence,PublicTargetId=-20001,AcceptedMembers=issued,SourceMembers=checked(issued+excluded),
      ScopeReleaseVerified=receipt["world_id"].Value<string>()==worldId&&receipt["tick"].Value<long>()==tick&&lastCommandId=="aria-s002-"+commandSequence&&lastCommandGroup!=0&&lastCommandTarget==objectiveTarget&&lastCommandFrame==frame};
    }return receipt;
   case AriaS002Action.AttackPublicTerrain:
    if(!attackMode||!current.HasSelectedLivingPlayerMember)throw new InvalidOperationException("Source attack mode/selection changed");SourceAriaPublicApproach a;
    if(d.TargetId==90011&&homeThreat.HasValue){using var grids=em.CreateEntityQuery(typeof(GridConfig));if(grids.CalculateEntityCount()!=1)throw new NotSupportedException("Actual unique source grid required");var grid=grids.GetSingleton<GridConfig>();var p=homeThreat.Value.PublicPosition;a=new SourceAriaPublicApproach(GridUtils.WorldToCell(grid,p),p);}
    else a=terrain??throw new InvalidOperationException("Actual public approach expired");
    var selectedBefore=SourceMissionValidatorSnapshot.SelectedForIssueSelected(em);var result=AttackMoveSystem.IssueSelected(em,a.Cell,a.Position,frame);var selectedAfter=SourceMissionValidatorSnapshot.SelectedForIssueSelected(em);attackMode=false;return new JObject{["validator_only_dispatch"]=new JObject{["feeds_aria"]=false,["decision_action"]=d.Action.ToString(),["public_control_target_id"]=d.TargetId,["resolved_target_domain"]=d.TargetId==90011&&homeThreat.HasValue?"PublicHomeThreat":"PublicObjectiveApproach",["argument_cell"]=new JObject{["x"]=a.Cell.x,["y"]=a.Cell.y},["argument_position"]=new JObject{["x"]=a.Position.x,["y"]=a.Position.y,["z"]=a.Position.z},["argument_frame"]=frame,["selected_query_before"]=selectedBefore,["selected_query_after"]=selectedAfter,["accepted_member_count_available"]=false,["count_scope"]="Actual API returns TacticalCommandResult without member count; selected snapshots are not accepted-count inference"},["accepted"]=result.Accepted,["source_reason"]=result.ReasonCode.ToString(),["source_api"]="AttackMoveSystem.IssueSelected",["source_result"]=JObject.FromObject(result),["troop_order"]=true};
   case AriaS002Action.RecruitRifle:case AriaS002Action.RecruitAntiAir:case AriaS002Action.RecruitRocketeer:case AriaS002Action.RecruitLogisticsTruck:case AriaS002Action.RecruitTanker:
    var role=d.Action==AriaS002Action.RecruitRifle?SkirmishRoleKind.Rifle:d.Action==AriaS002Action.RecruitAntiAir?SkirmishRoleKind.AntiAir:d.Action==AriaS002Action.RecruitRocketeer?SkirmishRoleKind.Rocketeer:d.Action==AriaS002Action.RecruitLogisticsTruck?SkirmishRoleKind.LogisticsTruck:SkirmishRoleKind.Tanker;
    var paidResult=paid.Request(role,productionTime);return new JObject{["accepted"]=paidResult.Accepted,["source_reason"]=paidResult.SourceReason,["source_receipt_id"]=paidResult.ReceiptId,["delivery_complete"]=false,["source_api"]="SourceCampPrimaryContact -> SAME SourceProductionDeliveryOwner.TryQueue"};
   default:throw new NotSupportedException("Active unsupported source action "+d.Action);
  }
 }
 int ValidatePrecisionGroup(uint expected){
  if(expected==0||expected!=plan.PrecisionGroup||current.Page.RosterRevision!=plan.PrecisionRoster||current.Page.PageIndex!=plan.PrecisionPage||current.Page.Group(plan.PrecisionSlot)!=expected||current.Page.SelectedGroups!=1||current.Page.SelectedOffPage!=0)throw new InvalidOperationException("Actual source precision selection identity changed");
  int count=0;var id=em.GetComponentData<SkirmishExpandedSessionComponent>(session).SessionId;using var q=em.CreateEntityQuery(typeof(SelectedUnitTag),typeof(SkirmishAttemptOwnedComponent),typeof(SkirmishArmyGroupMembershipComponent));using var all=q.ToEntityArray(Unity.Collections.Allocator.Temp);
  foreach(var e in all){if(!em.GetComponentData<SkirmishAttemptOwnedComponent>(e).SessionId.Equals(id)||!SkirmishArmyGroupSystem.IsAlive(em,e))continue;if(em.GetComponentData<SkirmishArmyGroupMembershipComponent>(e).GroupId!=expected)throw new InvalidOperationException("Foreign actual selected group in precision command");count++;}
  if(count==0)throw new InvalidOperationException("Actual precision member domain empty");return count;
 }
 uint SelectedGroup(){for(int i=0;i<5;i++)if((current.Page.SelectedMask&(1<<i))!=0)return current.Page.Group(i);return 0;}
 internal JObject Step(long tick,int frame,float time,float productionTime){
  if(stopped||busy||tick<=lastTick||frame<=0||!float.IsFinite(time)||!float.IsFinite(productionTime)||time<lastTime)throw new InvalidOperationException("Source dispatcher stopped/reentrant/repeated tick or invalid clocks");
  busy=true;try{lastTick=tick;lastTime=time;long readStart=System.Diagnostics.Stopwatch.GetTimestamp();var facts=Read(frame,time);double readSeconds=(System.Diagnostics.Stopwatch.GetTimestamp()-readStart)/(double)System.Diagnostics.Stopwatch.Frequency;if(contact.Phase==AriaS002ContactPhase.Verifying&&time>=contact.DueAt)contact.Phase=AriaS002ContactPhase.Observing;
   long policyStart=System.Diagnostics.Stopwatch.GetTimestamp();AriaGroundBaseAssaultPolicy.Step(groundContext,facts,ref plan,ref contact,out var decision);double policySeconds=(System.Diagnostics.Stopwatch.GetTimestamp()-policyStart)/(double)System.Diagnostics.Stopwatch.Frequency;JObject receipt=null;long dispatchStart=System.Diagnostics.Stopwatch.GetTimestamp();
   if(contact.Phase==AriaS002ContactPhase.Blocked||decision.Kind==AriaS002DecisionKind.Finished){stopped=true;attackMode=false;}
   else if(decision.Action!=AriaS002Action.None&&decision.Kind is AriaS002DecisionKind.Control or AriaS002DecisionKind.WorldTarget){
    if(contact.Phase!=AriaS002ContactPhase.Verifying||time>=contact.DueAt){contact.TargetId=decision.TargetId;contact.Phase=AriaS002ContactPhase.Touching;receipt=Execute(decision,tick,frame,time,productionTime);Complete(decision,frame,time);}
   }
   double dispatchSeconds=(System.Diagnostics.Stopwatch.GetTimestamp()-dispatchStart)/(double)System.Diagnostics.Stopwatch.Frequency;long traceStart=System.Diagnostics.Stopwatch.GetTimestamp();var traceObservation=JObject.FromObject(facts);double traceSeconds=(System.Diagnostics.Stopwatch.GetTimestamp()-traceStart)/(double)System.Diagnostics.Stopwatch.Frequency;
   return new JObject{["aria_profile_seconds"]=new JObject{["observation"]=readSeconds,["policy"]=policySeconds,["dispatch"]=dispatchSeconds,["observation_trace_format"]=traceSeconds},["executed"]=receipt!=null,["receipt_classification"]=receipt==null?"ProposalOrContactWait":(bool?)receipt["troop_order"]==false?"LogicalNonCommandControl":decision.Action==AriaS002Action.RecruitRifle||decision.Action==AriaS002Action.RecruitRocketeer||decision.Action==AriaS002Action.RecruitAntiAir?"SourcePaidRequest":"SourceHandlerResult",["diagnostic_action_id"]=receipt!=null?worldId+":"+tick+":"+frame+":"+decision.Action:null,["observation"]=traceObservation,["frame"]=frame,["time"]=time,["target_id"]=decision.TargetId,["decision_kind"]=decision.Kind.ToString(),["intent"]=plan.Intent.ToString(),["action"]=decision.Action.ToString(),["receipt"]=receipt,["scope"]="LogicalPublicData",["native_rendered_input"]=false,["full_policy_loaded"]=true,["full_strategy"]=false,["full_mission"]=false,["stopped"]=stopped,["source_contact_phase"]=contact.Phase.ToString(),["completed_logical_contacts"]=contact.Actions,["source_last_progress_at"]=plan.LastProgressAt,["source_precision_stage"]=plan.PrecisionStage,["source_inspection_stage"]=plan.AssaultInspectStage};
  }catch{stopped=true;attackMode=false;throw;}finally{busy=false;}
 }
 internal void Stop(){approach.OnAriaInactive();stopped=true;attackMode=false;contact.Phase=AriaS002ContactPhase.Manual;contact.GestureRequested=0;}
}
}
