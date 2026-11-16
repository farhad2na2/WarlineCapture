#!/usr/bin/env python3
"""Add opt-in nested timing to the measured S004 source-controller read."""
import argparse
import hashlib
import json
from pathlib import Path

ROOT=Path(__file__).resolve().parents[5]
BASE=ROOT/'T Logs/SkirmishProduction/s004-logical-controller-candidate-20261105-T4'

def sha(path): return hashlib.sha256(path.read_bytes()).hexdigest()
def once(text,old,new):
    if text.count(old)!=1: raise ValueError('Exact nested timing anchor changed: '+old[:100])
    return text.replace(old,new)

def generate(output, base=BASE):
    proof=json.loads((base/'receipt.json').read_text()); original=base/'SourceS004LogicalControllerCandidate.cs'
    if proof.get('read_profile_only') is not True or proof.get('output_sha256')!=sha(original) or proof.get('mission_ready') is not False:
        raise ValueError('Exact read-profile candidate required')
    text=original.read_text()
    text=once(text,'bool Recruit(SkirmishRoleKind role,bool sourceAffordability){if(current.PublicView.Finished||!sourceAffordability)return false;return paid.Read(role).CanRequest;}',
      '''long sourceRecruitTicks,sourceReadinessTicks;
 bool Recruit(SkirmishRoleKind role,bool sourceAffordability){long start=System.Diagnostics.Stopwatch.GetTimestamp();try{if(current.PublicView.Finished||!sourceAffordability)return false;return paid.Read(role).CanRequest;}finally{sourceRecruitTicks+=System.Diagnostics.Stopwatch.GetTimestamp()-start;}}
 bool TimedReadiness(){long start=System.Diagnostics.Stopwatch.GetTimestamp();try{return SourceS002ReadinessGateway.Read(em,session,out var readiness)&&readiness.CanUpgrade;}finally{sourceReadinessTicks+=System.Diagnostics.Stopwatch.GetTimestamp()-start;}}''')
    text=once(text,'sourceReadProfile=new JObject();long sourceReadMark=', 'sourceRecruitTicks=0;sourceReadinessTicks=0;sourceReadProfile=new JObject();long sourceReadMark=')
    text=once(text,'SourceS002ReadinessGateway.Read(em,session,out var sourceReadiness)&&sourceReadiness.CanUpgrade','TimedReadiness()')
    text=once(text,'MarkRead("facts_and_capabilities");return facts;',
      'MarkRead("facts_and_capabilities");sourceReadProfile["nested_recruit_reads"]=sourceRecruitTicks/(double)System.Diagnostics.Stopwatch.Frequency;sourceReadProfile["nested_readiness_read"]=sourceReadinessTicks/(double)System.Diagnostics.Stopwatch.Frequency;return facts;')
    output.mkdir(parents=True,exist_ok=False);dest=output/'SourceS004LogicalControllerCandidate.cs';dest.write_text(text)
    result={'status':'Prepared','kind':'S004LogicalControllerSourceCandidate','source_sha256':proof['source_sha256'],'output_sha256':sha(dest),
      'base_candidate_sha256':proof['base_candidate_sha256'],'detail_base_sha256':sha(original),'read_profile_only':True,'read_detail_profile':True,
      'air_service_logical_boundary':True,'source_only':True,'native_input':False,'full_original_policy_parity_proven':False,'mission_ready':False}
    (output/'receipt.json').write_text(json.dumps(result,indent=2)+'\n');return result

if __name__=='__main__':
    p=argparse.ArgumentParser();p.add_argument('--output',type=Path,required=True);p.add_argument('--base',type=Path,default=BASE);a=p.parse_args();print(json.dumps(generate(a.output.resolve(),a.base.resolve())))
