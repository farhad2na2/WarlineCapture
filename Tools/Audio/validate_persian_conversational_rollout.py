#!/usr/bin/env python3
"""Offline provenance/coverage checks for the approved Farsi delivery rollout."""
import argparse,array,hashlib,json,math,re,wave
from pathlib import Path
import generate_persian_conversational_rollout as rollout
import persian_voice_profile as profile

def main():
 p=argparse.ArgumentParser();p.add_argument('--sources-only',action='store_true');args=p.parse_args();jobs=rollout.jobs();errors=[];signals=[]
 prepared=json.loads(rollout.PAYLOAD.read_text())
 approved={r['assetPath']:r for r in prepared['requests']};local_edits=[]
 for j in jobs:
  current=rollout.request_descriptor(j);original=approved.get(j['assetPath'])
  if current==original:continue
  # A documented local trim keeps the approved take, without changing the paid
  # request history. Only removal of a trailing sentence is eligible here.
  record=j['record'];old_body=dict((original or {}).get('request',{}));new_body=dict(current['request'])
  old_text=old_body.pop('text','');new_text=new_body.pop('text','')
  local_trim=bool(record.get('localEdit')) and old_text.startswith(new_text) and old_body==new_body and original['caption'].startswith(current['caption']) and original['voiceId']==current['voiceId'] and original['outputFormat']==current['outputFormat']
  if local_trim:local_edits.append(j['identity'])
  else:errors.append('Prepared payload/source mismatch: '+j['identity'])
 if set(approved)!={j['assetPath'] for j in jobs}:errors.append('Prepared payload coverage mismatch')
 for j in jobs:
  if re.search(r'\{\d+\}',j['spokenText']):errors.append('Unresolved spoken placeholder: '+j['identity'])
  if j['identity'].startswith('tutorial-') and len(j['text'].encode())>509:errors.append('Tutorial message exceeds ECS UTF8 capacity: '+j['identity'])
  if args.sources_only:continue
  manifest=json.loads((rollout.ROOT/j['manifest']).read_text());record=next((c for c in manifest[j['collection']] if c['assetPath']==j['assetPath']),{})
  path=rollout.ROOT/j['assetPath']
  if record.get('text')!=j['text'] or record.get('spokenText')!=j['spokenText']:errors.append('Caption/voice source mismatch: '+j['identity'])
  if record.get('deliveryProfile')!=profile.PROFILE_ID:errors.append('Old delivery: '+j['identity'])
  if record.get('sha256')!=hashlib.sha256(path.read_bytes()).hexdigest():errors.append('Clip hash mismatch: '+j['identity'])
  if record.get('captionSha256')!=hashlib.sha256(j['text'].encode()).hexdigest():errors.append('Caption hash mismatch: '+j['identity'])
  with wave.open(str(path),'rb') as w:
   duration=w.getnframes()/w.getframerate()
   samples=array.array('h',w.readframes(w.getnframes()))
   peak=max((abs(n) for n in samples),default=0)
   first=next((i for i,n in enumerate(samples) if abs(n)>98),len(samples))
   last=next((i for i in range(len(samples)-1,-1,-1) if abs(samples[i])>98),-1)
   signal={'identity':j['identity'],'duration':round(duration,3),'peakDb':round(20*math.log10(max(peak,1)/32768),2),'leadingSilence':round(first/w.getframerate(),3),'trailingSilence':round((len(samples)-last-1)/w.getframerate(),3),'clippedSamples':sum(abs(n)>=32767 for n in samples)}
   signals.append(signal)
   if signal['peakDb'] < -30 or signal['leadingSilence']>1.5 or signal['trailingSilence']>1.5 or signal['clippedSamples']>0:errors.append('Audio signal needs review: '+j['identity'])
   if (w.getnchannels(),w.getsampwidth(),w.getframerate())!=(1,2,44100):errors.append('Audio format: '+j['identity'])
   if abs(duration-record['durationSeconds'])>.001:errors.append('Duration mismatch: '+j['identity'])
   if 'maximumDurationSeconds' in record and duration>record['maximumDurationSeconds']:errors.append('Comic timing overflow: '+j['identity'])
 if not args.sources_only:
  (rollout.REPORT/'audio_signal_checks.json').write_text(json.dumps({'clips':len(signals),'issues':[s for s in signals if s['peakDb'] < -30 or s['leadingSilence']>1.5 or s['trailingSilence']>1.5 or s['clippedSamples']>0],'samples':signals},ensure_ascii=False,indent=2)+'\n')
  before=json.loads((rollout.ROOT/'.local/persian-tone-rollout/audio_before.json').read_text())
  for path,digest in before.items():
   if '/fa/' in path or path.endswith('_fa.wav'):continue
   if hashlib.sha256((rollout.ROOT/path).read_bytes()).hexdigest()!=digest:errors.append('Unrelated audio changed: '+path)
 result={'result':'Passed' if not errors else 'Failed','clipCount':len(jobs),'activeClipCount':sum(not j.get('retired',False) for j in jobs),'archivedClipCount':sum(j.get('retired',False) for j in jobs),'documentedLocalEdits':local_edits,'sourcesOnly':args.sources_only,'errors':errors}
 print(json.dumps(result,ensure_ascii=False,indent=2));
 if not args.sources_only:(rollout.REPORT/'validation_result.json').write_text(json.dumps(result,ensure_ascii=False,indent=2)+'\n')
 if errors:raise SystemExit(1)
if __name__=='__main__':main()
