#!/usr/bin/env python3
"""Prepare a source-only S004 semantic controller candidate, without UI input."""
import argparse
import hashlib
import json
from pathlib import Path

ROOT = Path(__file__).resolve().parents[5]
SOURCE = ROOT / "Tools/CI/SkirmishSourceSimulation/Adapters/492_SourceAriaS002Controller.command-trace.cs"


def sha(path): return hashlib.sha256(path.read_bytes()).hexdigest()


def once(text, old, new):
    if text.count(old) != 1: raise ValueError("Exact source adapter anchor changed: " + old[:120])
    return text.replace(old, new)


def generate(out, air_service=False):
    text=SOURCE.read_text()
    text=once(text,
      'var p=current.Page;if(v.AirProfile||current.CombinedArmsField||(!v.ReadinessEligible&&groundContext.StartPackageId!=SkirmishStartPackageId.FieldBase)||!p.Expanded||!p.BaseAssault)',
      'var p=current.Page;bool s004=groundContext.CatalogId=="S004";if((v.AirProfile&&!s004)||(s004&&!v.AirProfile)||current.CombinedArmsField||(!v.ReadinessEligible&&groundContext.StartPackageId!=SkirmishStartPackageId.FieldBase)||!p.Expanded||!p.BaseAssault)')
    text=once(text,
      'OwnMaterials=v.OwnMaterials,ReadinessEligible=v.ReadinessEligible,PadPresent=v.PadPresent,AirQueueOffered=v.AirQueueOffered,OwnAttackAirLive=v.OwnAttackAirLive,',
      'OwnMaterials=v.OwnMaterials,OwnDefenseTowers=v.OwnDefenseTowers,ReadinessEligible=v.ReadinessEligible,PadPresent=v.PadPresent,PadReady=v.PadReady,AirQueueOffered=v.AirQueueOffered,OwnAttackAirLive=v.OwnAttackAirLive,OwnAttackAirActive=v.OwnAttackAirActive,OwnAttackAirLanded=v.OwnAttackAirLanded,OwnAirFuel=v.OwnAirFuel,')
    old='if(facts.CanBuildAirPad||facts.AirQueueOffered||facts.OwnAttackAirLive!=0)throw new NotSupportedException("Active air/infrastructure read domain is not this profile");'
    new='''if(groundContext.CatalogId=="S004") {
   // Source semantic capabilities are not rendered controls or touch evidence.
   // Unknown construction and live-air service must fail closed.
   if((facts.CanBuildAirPad&&!facts.PadPresent)||facts.OwnAttackAirLive!=0)
    throw new NotSupportedException("S004 source air-pad construction/live-air service not admitted");
   facts.RecruitLogisticsTruck=Cap(AriaS002Action.RecruitLogisticsTruck,90016,Recruit(SkirmishRoleKind.LogisticsTruck,v.CanAffordLogisticsTruck));
   facts.RecruitTanker=Cap(AriaS002Action.RecruitTanker,90017,Recruit(SkirmishRoleKind.Tanker,v.CanAffordTanker));
   facts.RecruitAir=Cap(AriaS002Action.UnsupportedAir,90018,Recruit(SkirmishRoleKind.AttackHeliLight,v.AirQueueOffered));
  } else if(facts.CanBuildAirPad||facts.AirQueueOffered||facts.OwnAttackAirLive!=0)throw new NotSupportedException("Active air/infrastructure read domain is not this profile");'''
    text=once(text,old,new)
    old='''   default:throw new NotSupportedException("Active unsupported source action "+d.Action);'''
    new='''   case AriaS002Action.UnsupportedAir:
    if(groundContext.CatalogId!="S004"||d.TargetId!=90018)throw new NotSupportedException("Unbound source air action");
    var airResult=paid.Request(SkirmishRoleKind.AttackHeliLight,productionTime);
    return new JObject{["accepted"]=airResult.Accepted,["source_reason"]=airResult.SourceReason,["source_receipt_id"]=airResult.ReceiptId,["delivery_complete"]=false,["source_api"]="Typed unit semantic request -> SourceProductionDeliveryOwner.TryQueue",["troop_order"]=false};
   default:throw new NotSupportedException("Active unsupported source action "+d.Action);'''
    text=once(text,old,new)
    text=once(text,
      'receipt==null?"ProposalOrContactWait":(bool?)receipt["troop_order"]==false?"LogicalNonCommandControl":decision.Action==AriaS002Action.RecruitRifle||decision.Action==AriaS002Action.RecruitRocketeer||decision.Action==AriaS002Action.RecruitAntiAir?"SourcePaidRequest":"SourceHandlerResult"',
      'receipt==null?"ProposalOrContactWait":decision.Action==AriaS002Action.RecruitRifle||decision.Action==AriaS002Action.RecruitRocketeer||decision.Action==AriaS002Action.RecruitAntiAir||decision.Action==AriaS002Action.RecruitLogisticsTruck||decision.Action==AriaS002Action.RecruitTanker||decision.Action==AriaS002Action.UnsupportedAir&&decision.TargetId==90018?"SourcePaidRequest":(bool?)receipt["troop_order"]==false?"LogicalNonCommandControl":"SourceHandlerResult"')
    text=once(text,'["scope"]="LogicalPublicData",["native_rendered_input"]=false,',
      '["scope"]="S004CandidateLogicalPublicData",["native_rendered_input"]=false,["original_generic_policy_full_parity_proven"]=false,')
    if air_service:
        text=once(text,
          'if((facts.CanBuildAirPad&&!facts.PadPresent)||facts.OwnAttackAirLive!=0)\n    throw new NotSupportedException("S004 source air-pad construction/live-air service not admitted");',
          '''if(facts.CanBuildAirPad&&!facts.PadPresent)
    throw new NotSupportedException("S004 source air-pad construction not admitted");
   bool selectedAir=SourceS004SelectedAircraftReturn.TryReadSelected(em,session,1,out _);
   facts.SelectedAircraft=selectedAir;
   facts.ReturnAircraft=Cap(AriaS002Action.UnsupportedAir,90019,selectedAir);''')
        text=once(text,
          'if(groundContext.CatalogId!="S004"||d.TargetId!=90018)throw new NotSupportedException("Unbound source air action");',
          '''if(groundContext.CatalogId!="S004")throw new NotSupportedException("Unbound source air action");
    if(d.TargetId==90019) {
     bool accepted=SourceS004SelectedAircraftReturn.TryIssue(em,session,1,out var actor);
     return new JObject{["accepted"]=accepted,["actor_index"]=accepted?actor.Index:-1,["actor_version"]=accepted?actor.Version:0,
      ["source_api"]="UnitReturnToBaseCommandService.TryIssueOwned",["native_input"]=false,["troop_order"]=true};
    }
    if(d.TargetId!=90018)throw new NotSupportedException("Unbound source air action");''')
    out.mkdir(parents=True,exist_ok=False);dest=out/'SourceS004LogicalControllerCandidate.cs';dest.write_text(text)
    receipt={"status":"Prepared","kind":"S004LogicalControllerSourceCandidate","source_sha256":sha(SOURCE),"output_sha256":sha(dest),
      "source_only":True,"native_input":False,"full_original_policy_parity_proven":False,"mission_ready":False,
      "air_service_logical_boundary":air_service,
      "unbound":"Air-pad construction and unbound UnsupportedAir actions fail closed" if air_service else "Air-pad construction, live-air service and non-recruit UnsupportedAir actions fail closed"}
    (out/'receipt.json').write_text(json.dumps(receipt,indent=2)+'\n');return receipt


if __name__=='__main__':
    parser=argparse.ArgumentParser();parser.add_argument('--output',type=Path,required=True);parser.add_argument('--air-service',action='store_true')
    args=parser.parse_args();print(json.dumps(generate(args.output.resolve(),args.air_service)))
