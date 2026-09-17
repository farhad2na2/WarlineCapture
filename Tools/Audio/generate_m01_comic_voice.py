#!/usr/bin/env python3
"""Generate only the reviewed M1 comic payload, caching each paid recording."""
import concurrent.futures, hashlib, json, wave
from pathlib import Path
import generate_m02_bilingual_voice as audio
import persian_voice_profile as profile
ROOT=Path(__file__).resolve().parents[2]
PAYLOAD=ROOT/'Design/AgentReports/M01ComicAudio/voice_payload_review.json'
MANIFEST=ROOT/'Assets/Game/Audio/Narrative/M01FirstContact/m01_comic_voice_manifest.json'

def generate(request, key):
 body=request['request'];lang=body['language_code'];speaker=request['speaker']
 expected=profile.apply(dict(text=request['caption'],model_id=audio.MODEL,language_code=lang,seed=body['seed'],apply_text_normalization='on'),lang)
 if body!=expected or request['voiceId']!=audio.VOICE_IDS[speaker]:raise ValueError('Payload/cast mismatch')
 fingerprint=hashlib.sha256(json.dumps(request,sort_keys=True,ensure_ascii=False).encode()).hexdigest()
 folder=ROOT/'.local/m01-comic-audio'/fingerprint;folder.mkdir(parents=True,exist_ok=True)
 source=folder/'source.mp3'
 if not source.exists():source.write_bytes(audio.request_audio(key,request['voiceId'],request['caption'],lang,body['seed']))
 destination=ROOT/request['assetPath']
 audio.convert(source.read_bytes(),destination,speaker)
 with wave.open(str(destination)) as clip:
  if clip.getnchannels()!=1 or clip.getframerate()!=44100 or clip.getnframes()/44100<max(.5,len(request['caption'].split())/6):raise ValueError('Invalid or truncated clip: '+request['lineId'])
 record=audio.record('narrative',request['lineId'],speaker,request['locale'],request['caption'],destination)
 record.update(profile.metadata(lang));record['textKey']=request['textKey']
 return record

def main():
 import argparse
 parser=argparse.ArgumentParser();parser.add_argument('--generate',action='store_true');args=parser.parse_args()
 payload=json.loads(PAYLOAD.read_text());requests=payload['requests'];assert len(requests)==18
 if not args.generate:print('Review payload ready: 18 M1 comic recordings; no network used.');return
 key=audio.read_api_key(ROOT/'.local/secrets/elevenlabs_api_key')
 subscription=audio.request_json(key,'/v1/user/subscription')
 if subscription.get('status')!='active' or subscription.get('tier') in (None,'free'):raise RuntimeError('Existing paid account required')
 records=[]
 with concurrent.futures.ThreadPoolExecutor(max_workers=2) as pool:
  for record in pool.map(lambda r:generate(r,key),requests):
   records.append(record);print(record['id'],record['locale'],record['durationSeconds'],flush=True)
 MANIFEST.parent.mkdir(parents=True,exist_ok=True)
 MANIFEST.write_text(json.dumps(dict(schema='WarlineCapture.M01ComicVoice.v1',provider='ElevenLabs',license=audio.RIGHTS,model=audio.MODEL,runtimeNetworkTts=False,clips=records),ensure_ascii=False,indent=2)+'\n')
 print('[M01ComicVoiceGeneration] result=Passed clips=18',flush=True)
if __name__=='__main__':main()
