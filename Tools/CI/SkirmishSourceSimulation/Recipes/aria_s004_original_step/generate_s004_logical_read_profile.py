#!/usr/bin/env python3
"""Prepare an opt-in timing-only S004 source controller projection."""
import argparse
import hashlib
import json
from pathlib import Path

ROOT = Path(__file__).resolve().parents[5]
SOURCE = ROOT / 'T Logs/SkirmishProduction/s004-logical-controller-candidate-20261105-T3'


def sha(path): return hashlib.sha256(path.read_bytes()).hexdigest()


def once(text, old, new):
    if text.count(old) != 1: raise ValueError('Exact profile anchor changed: ' + old[:100])
    return text.replace(old, new)


def generate(output, source=SOURCE):
    proof=json.loads((source/'receipt.json').read_text())
    original=source/'SourceS004LogicalControllerCandidate.cs'
    if proof.get('air_service_logical_boundary') is not True or proof.get('output_sha256') != sha(original) or proof.get('mission_ready') is not False:
        raise ValueError('Exact non-native logical air-service candidate required')
    text=original.read_text()
    text=once(text,'AriaS002Observation Read(int frame,float time){',
        '''JObject sourceReadProfile;
 AriaS002Observation Read(int frame,float time){
  sourceReadProfile=new JObject();long sourceReadMark=System.Diagnostics.Stopwatch.GetTimestamp();
  void MarkRead(string phase){long now=System.Diagnostics.Stopwatch.GetTimestamp();sourceReadProfile[phase]=(now-sourceReadMark)/(double)System.Diagnostics.Stopwatch.Frequency;sourceReadMark=now;}''')
    text=once(text,'current=PublicCommandCatalog.Read(em,session,army);var v=current.PublicView;',
        'current=PublicCommandCatalog.Read(em,session,army);MarkRead("scope_and_public_catalog");var v=current.PublicView;')
    text=once(text,'contacts.UpdateAfterFog(frame,time);var snapshot=contacts.ReadSnapshot();',
        'MarkRead("objective");contacts.UpdateAfterFog(frame,time);var snapshot=contacts.ReadSnapshot();MarkRead("contacts");')
    text=once(text,'homeThreat=v.Finished?null:SourceAriaPublicHomeThreat.Read(em,session,current);terrain=bases.EnemyObjectiveAlive?approach.Read():null;',
        'homeThreat=v.Finished?null:SourceAriaPublicHomeThreat.Read(em,session,current);MarkRead("target_and_home_threat");terrain=bases.EnemyObjectiveAlive?approach.Read():null;MarkRead("approach");')
    text=once(text,'var facts=new AriaS002Observation{', 'MarkRead("orders_and_distances");var facts=new AriaS002Observation{')
    text=once(text,'  return facts;\n }\n string Request()', '  MarkRead("facts_and_capabilities");return facts;\n }\n string Request()')
    text=once(text,'["aria_profile_seconds"]=new JObject{', '["source_read_profile_seconds"]=sourceReadProfile,["aria_profile_seconds"]=new JObject{')
    output.mkdir(parents=True,exist_ok=False);dest=output/'SourceS004LogicalControllerCandidate.cs';dest.write_text(text)
    result={'status':'Prepared','kind':'S004LogicalControllerSourceCandidate','source_sha256':proof['source_sha256'],
      'output_sha256':sha(dest),'base_candidate_sha256':sha(original),'air_service_logical_boundary':True,
      'read_profile_only':True,'source_only':True,'native_input':False,'full_original_policy_parity_proven':False,'mission_ready':False}
    (output/'receipt.json').write_text(json.dumps(result,indent=2)+'\n');return result


if __name__=='__main__':
    parser=argparse.ArgumentParser();parser.add_argument('--output',type=Path,required=True);parser.add_argument('--source',type=Path,default=SOURCE)
    args=parser.parse_args();print(json.dumps(generate(args.output.resolve(),args.source.resolve())))
