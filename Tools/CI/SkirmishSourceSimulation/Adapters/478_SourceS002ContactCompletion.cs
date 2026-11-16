using System;using Game.Simulation;using Newtonsoft.Json.Linq;namespace Game.Runtime {internal static class SourceS002ContactCompletion {internal static void Observe(ref AriaS002ContactState current,float now){if(current.Phase==AriaS002ContactPhase.Verifying&&now>=current.DueAt)current.Phase=AriaS002ContactPhase.Observing;}
 internal static bool TryCompleteLogicalAction(ref AriaS002ContactState current,in AriaS002Decision decision,JObject receipt,int frame,float now){
  if(receipt==null)throw new InvalidOperationException("Actual source action receipt required");
  bool? accepted=(bool?)receipt["accepted"];
  if(!accepted.HasValue){
   string key=decision.Action switch{
    AriaS002Action.OpenDefensePreview=>"source_preview_open",
    AriaS002Action.SelectDefenseSite=>"source_preview_moved",
    AriaS002Action.CancelPlacement=>"source_preview_cancelled",
    AriaS002Action.InspectHome or AriaS002Action.InspectObjective or AriaS002Action.OpenInspection or AriaS002Action.CloseInspection=>"inspection_completed",
    AriaS002Action.SelectMode=>"logical_selection_mode",
    AriaS002Action.ArmAttack=>"logical_attack_mode",
    _=>null};
   if(key==null||receipt[key]?.Type!=JTokenType.Boolean)throw new NotSupportedException("Source action acceptance evidence missing: "+decision.Action);
   accepted=(bool)receipt[key];
  }
  if(!accepted.Value)return false;
  current.TargetId=decision.TargetId;current.Target=current.Contact=decision.Position;current.Phase=AriaS002ContactPhase.Touching;
  Complete(ref current,frame,now,true);return true;
 }
 internal static void Complete(ref AriaS002ContactState current,int frame,float now,bool hasSkirmish){
                    current.Actions++;
                    current.LastCompletedTargetId=current.TargetId;
                    current.LastCompletedFrame=frame;
                    current.LastCompletedPosition=current.Contact;
                    current.Phase = AriaS002ContactPhase.Verifying;
                    current.DueAt = now + (hasSkirmish ? .65f : 1.5f);}}}
