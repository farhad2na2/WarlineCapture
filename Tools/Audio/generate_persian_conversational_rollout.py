#!/usr/bin/env python3
"""Prepare/resume the approved Farsi-only delivery rollout; preserve all English audio.

--prepare writes the exact reviewable requests without network calls.
--generate uses the existing paid account and permanent cast, caches each result,
then atomically replaces validated local WAVs and their manifest records.
"""
from __future__ import annotations
import argparse, concurrent.futures, datetime as dt, hashlib, json, os, re, sys, tempfile, wave
from pathlib import Path
import generate_m02_bilingual_voice as audio
import generate_m01_tutorial_voice as m1
import generate_m03_bilingual_voice as m3
import generate_m04_bilingual_voice as m4
import generate_m05_bilingual_voice as m5
import generate_bilingual_aria_match_voice as shared
import persian_voice_profile as profile
ROOT=Path(__file__).resolve().parents[2]
sys.path.insert(0,str(ROOT/'Tools/NarrativeVision'))
import generate_first_launch_voice_elevenlabs as first
REPORT=ROOT/'Design/AgentReports/PersianConversationalTone'
PAYLOAD=REPORT/'voice_payload_review.json'
CACHE=ROOT/'.local/persian-tone-rollout/cache'
FIRST=ROOT/'Assets/Game/Audio/Narrative/FirstLaunch/first_launch_persian_voice_manifest.json'

def sha(data): return hashlib.sha256(data).hexdigest()
def read(p): return json.loads(p.read_text())
def write(p,d):
 p.parent.mkdir(parents=True,exist_ok=True);tmp=p.with_suffix(p.suffix+'.tmp');tmp.write_text(json.dumps(d,ensure_ascii=False,indent=2)+'\n');tmp.replace(p)
def jobs():
 result=[]
 def add(manifest,clip,text,speaker='ARIA',spoken=None,collection='clips',first_id=None):
  voice=clip.get('voiceId') or audio.VOICE_IDS[speaker]; path=clip['assetPath']
  j={'assetPath':path,'manifest':str(manifest.relative_to(ROOT)),'collection':collection,'identity':clip.get('id',clip.get('clipId',clip.get('key',str(clip.get('step'))+str(clip.get('phase'))))), 'speaker':speaker,'voiceId':voice,'text':text,'spokenText':spoken or text,'record':dict(clip)}
  if first_id:j['firstLaunchLineId']=first_id;j['processing']=clip['processing']
  result.append(j)
 sp={e['key']:e for e in read(shared.PERSIAN_TEXT_CATALOG)['entries']}
 for c in read(shared.MANIFEST_PATH)['clips']:
  if c['locale']=='fa-IR':
   e=sp[c['key']];add(shared.MANIFEST_PATH,c,e['text'],spoken=e.get('spokenText'))
 p=m1.MANIFEST_PATH;cur={(x[0],x[1]):x[4] for x in m1.CUES if x[2]=='fa-IR'};cur.update({(x[0],x[1]):x[3] for x in m1.PRESERVED_CUES if x[2]=='fa-IR'})
 for c in read(p)['clips']:
  if c['locale']=='fa-IR':add(p,c,cur[(c['step'],c['phase'])])
 for p,cur in [(ROOT/'Assets/Game/Audio/Narrative/M02EstablishBase/m02_narrative_voice_manifest.json',{x[0]:x[-1] for x in audio.NARRATIVE}),(ROOT/'Assets/Game/Audio/Voice/Tutorial/tutorial_m02_aria_voice_manifest.json',{str(x[0]):x[-1] for x in audio.TUTORIAL})]:
  for c in read(p)['clips']:
   if c['locale']=='fa-IR':add(p,c,cur[c['id']],c['speaker'])
 for m in (m3,m4,m5):
  old={c['id']:c for c in read(m.MANIFEST)['clips'] if c['locale']=='fa-IR'}
  source_lines=m.lines(include_retired=True) if m is m3 else m.lines()
  for kind,identity,speaker,en,fa in source_lines:
   c=old.get(identity)
   if not c:
    path=m.MANIFEST.parent/'Voice/fa'/f'{identity}.wav'
    c={'kind':kind,'id':identity,'speaker':speaker,'locale':'fa-IR','text':fa,'assetPath':str(path.relative_to(ROOT)),'voiceId':audio.VOICE_IDS[speaker],'voiceName':audio.VOICE_NAMES[speaker]}
   retired=m is m3 and identity=='tutorial-m03-07'
   # The approved 261-request payload is historical provenance, not permission to
   # regenerate a removed lesson from replacement UI copy.
   add(m.MANIFEST,c,c['text'] if retired else fa,speaker,spoken=c.get('spokenText') if retired else None)
   if retired:result[-1]['retired']=True
 catalog=read(ROOT/'Assets/Game/Data/Narrative/FirstLaunch/first_launch_persian_text_catalog.json');lines={e['lineId']:e for e in catalog['lines']}
 for collection in ('clips','voiceVariants'):
  for c in read(FIRST)[collection]:
   line_id=c.get('sourceLineId',c['clipId']);l=lines[line_id]
   # Fallback lookup must not evaluate unknown cast eagerly.
   j={'assetPath':c['assetPath'],'manifest':str(FIRST.relative_to(ROOT)),'collection':collection,'identity':c['clipId'],'speaker':c['speaker'],'voiceId':c['voiceId'],'text':l['text'],'spokenText':l.get('speechText',l['text']),'record':dict(c),'firstLaunchLineId':line_id,'processing':c.get('processing','commander-clean')}
   result.append(j)
 assert len({j['assetPath'] for j in result})==len(result)
 for i,j in enumerate(result):
  assert '/fa/' in j['assetPath'] or j['assetPath'].endswith('_fa.wav'),j['assetPath']
  assert not re.search(r'\{\d+\}',j['spokenText']),j
  j['seed']=3151000+i;j['deliveryProfile']=profile.PROFILE_ID
 return result

def request_descriptor(j):
 body=profile.apply({'text':j['spokenText'],'model_id':audio.MODEL,'language_code':'fa','seed':j['seed'],'apply_text_normalization':'on'},'fa')
 return {'assetPath':j['assetPath'],'voiceId':j['voiceId'],'caption':j['text'],'request':body,'outputFormat':audio.OUTPUT_FORMAT}

def trim_excess_silence(path):
 """Remove only excessive (>1.5 s) trailing padding; preserve 150 ms after speech."""
 import array
 with wave.open(str(path), 'rb') as w:
  params=w.getparams();frames=w.readframes(w.getnframes())
 samples=array.array('h',frames)
 last=next((i for i in range(len(samples)-1,-1,-1) if abs(samples[i])>98),None)
 if last is None or (len(samples)-last-1)/params.framerate<=1.5:return
 keep=min(len(samples),last+1+int(params.framerate*.15))
 with wave.open(str(path),'wb') as w:w.setparams(params);w.writeframes(frames[:keep*2])

def generate(j,key):
 descriptor=request_descriptor(j);fingerprint=sha(json.dumps(descriptor,sort_keys=True,ensure_ascii=False).encode());folder=CACHE/fingerprint;folder.mkdir(parents=True,exist_ok=True)
 wav=folder/'voice.wav';meta=folder/'metadata.json';mp3=folder/'source.mp3'
 if not meta.exists() or not wav.exists() or read(meta).get('sha256')!=sha(wav.read_bytes()):
  if not mp3.exists():mp3.write_bytes(audio.request_audio(key,j['voiceId'],j['spokenText'],'fa',j['seed']))
  tempo=1.0
  selected_seed=j['seed']
  if 'firstLaunchLineId' in j:
   maximum=first.MAX_DURATIONS[j['firstLaunchLineId']]
   for attempt in range(3):
    source=mp3 if attempt==0 else folder/f'source-take-{attempt+1}.mp3'
    selected_seed=j['seed']+attempt*100000
    if not source.exists():source.write_bytes(audio.request_audio(key,j['voiceId'],j['spokenText'],'fa',selected_seed))
    first.convert_audio('ffmpeg',source,wav,j['processing'],1.0)
    duration=first.wav_duration(wav)
    tempo=max(1.0,duration/(maximum-.08)) if duration>maximum else 1.0
    if tempo>first.MAX_TIMING_COMPRESSION.get(j['firstLaunchLineId'],1.18):
     if attempt==2:raise RuntimeError(f"{j['identity']}: all takes exceed panel timing; shorten copy")
     continue
    if tempo>1.0:first.convert_audio('ffmpeg',source,wav,j['processing'],tempo)
    break
  else:audio.convert(mp3.read_bytes(),wav,j['speaker'])
  trim_excess_silence(wav)
  with wave.open(str(wav),'rb') as w:
   assert w.getframerate()==44100 and w.getnchannels()==1 and w.getsampwidth()==2
   duration=w.getnframes()/w.getframerate()
  minimum=max(.25,len(j['spokenText'].split())/6.0)
  if duration<minimum:raise RuntimeError(f"{j['identity']}: possible truncated speech ({duration:.2f}s)")
  record=dict(j['record']);record.update(text=j['text'],spokenText=j['spokenText'],deliveryProfile=profile.PROFILE_ID,voiceSettings=profile.VOICE_SETTINGS,performanceDirection=profile.DIRECTION,generationSeed=selected_seed,generatedAtUtc=dt.datetime.now(dt.timezone.utc).isoformat(),durationSeconds=round(duration,6),sha256=sha(wav.read_bytes()),captionSha256=sha(j['text'].encode()))
  if 'firstLaunchLineId' in j:
   record.update(timingCompression=round(tempo,4),minimumDurationSeconds=minimum,maximumDurationSeconds=maximum,candidateCount=1,selectedCandidate=1,candidateDurationsSeconds=[round(duration,6)])
   record.pop('requestId',None);record.pop('characterCost',None)
  write(meta,record)
 return j,read(meta),wav

def main():
 p=argparse.ArgumentParser();p.add_argument('--prepare',action='store_true');p.add_argument('--generate',action='store_true');p.add_argument('--jobs',type=int,default=2);args=p.parse_args();entries=jobs()
 payload={'scope':'Approved conversational Persian rollout, first launch, M1-M5, shared ARIA; existing paid account and permanent cast.','provider':audio.API_ROOT,'deliveryProfile':profile.PROFILE_ID,'clipCount':len(entries),'requests':[request_descriptor(j) for j in entries]}
 if args.prepare:write(PAYLOAD,payload);print(f'Prepared {len(entries)} Persian clips, {sum(len(j["spokenText"]) for j in entries)} characters');return
 if not args.generate:raise SystemExit('Choose --prepare or --generate')
 if read(PAYLOAD)!=payload:raise RuntimeError('Prepared payload differs from sources. Prepare again before generating.')
 key=audio.read_api_key(ROOT/'.local/secrets/elevenlabs_api_key');s=audio.request_json(key,'/v1/user/subscription')
 if s.get('status')!='active' or s.get('tier') in (None,'free'):raise RuntimeError('Active paid subscription required')
 print(f'Generating {len(entries)} clips with {args.jobs} workers',flush=True);failures=[];complete=[]
 with concurrent.futures.ThreadPoolExecutor(max_workers=args.jobs) as pool:
  futures={pool.submit(generate,j,key):j for j in entries if not j.get('retired')}
  for f in concurrent.futures.as_completed(futures):
   j=futures[f]
   try:
    result=f.result();complete.append(result);print(f'[{len(complete)}/{len(entries)}] {j["identity"]} {result[1]["durationSeconds"]:.2f}s',flush=True)
   except Exception as e:failures.append({'identity':j['identity'],'error':str(e)});print(f'FAILED {j["identity"]}: {e}',flush=True)
 # Publish successful clips and merge only their records. Failed assets remain untouched and resumable.
 manifests={}
 for j,r,wav in complete:
  destination=ROOT/j['assetPath'];temporary=destination.with_suffix('.wav.tmp');temporary.write_bytes(wav.read_bytes());temporary.replace(destination)
  path=ROOT/j['manifest'];d=manifests.setdefault(path,read(path));cs=d[j['collection']];index=next((i for i,c in enumerate(cs) if c['assetPath']==j['assetPath']),None)
  if index is None:cs.append(r)
  else:cs[index]=r
 for p,d in manifests.items():d['persianDeliveryProfile']=profile.PROFILE_ID;d['lastUpdatedAtUtc']=dt.datetime.now(dt.timezone.utc).isoformat();write(p,d)
 write(REPORT/'generation_result.json',{'requested':len(entries),'completed':len(complete),'failures':failures,'deliveryProfile':profile.PROFILE_ID})
 if failures:raise SystemExit(f'{len(failures)} clips need attention; successful results cached and published.')
 print('[PersianConversationalRollout] result=Passed',flush=True)
if __name__=='__main__':main()
