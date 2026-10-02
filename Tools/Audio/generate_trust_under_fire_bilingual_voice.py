#!/usr/bin/env python3
"""Generate Trust Under Fire's canonical English/Persian comic cast, with resumable manifests."""
from __future__ import annotations
import argparse, datetime as dt, hashlib, json, re
from pathlib import Path
import generate_m02_bilingual_voice as audio
import generate_m04_bilingual_voice as decode
import persian_voice_profile
ROOT=Path(__file__).resolve().parents[2]
SOURCE=ROOT/'Assets/Game/Scripts/Configs/Localization/CH05M02TrustUnderFireCopy.cs'
MANIFEST=ROOT/'Assets/Game/Audio/Narrative/CH05M02TrustUnderFire/trust_under_fire_voice_manifest.json'
REPORT=ROOT/'Design/AgentReports/CH05M02TrustUnderFire/Narrative'
audio.VOICE_IDS.update(LINA='EXAVITQu4vr4xnSDxMaL')
audio.VOICE_NAMES.update(LINA='Sarah - Dr. Lina Mansour')
def lines():
 s=decode.STRING
 rows=re.findall(r'new\('+s+r',\s*NarrativeSpeakerId\.(\w+),\s*'+s+r',\s*'+s+r'\)',SOURCE.read_text())
 if len(rows)!=7:raise RuntimeError('Expected seven canonical dialogue lines')
 return [('trust_under_fire-'+decode.decode(i),sp.upper(),decode.decode(en),decode.decode(fa)) for i,sp,en,fa in rows]
def main():
 parser=argparse.ArgumentParser(description=__doc__);parser.add_argument('--generate',action='store_true');args=parser.parse_args()
 entries=lines();payload={'provider':'ElevenLabs','model':audio.MODEL,'missionId':'saga.ch05.m02.trust_under_fire','clipCount':14,'runtimeNetworkTts':False,'clips':[]}
 for index,(identity,speaker,en,fa) in enumerate(entries):
  for language,locale,text in [('en','en-US',en),('fa','fa-IR',fa)]:
   request={'text':text,'model_id':audio.MODEL,'language_code':language,'seed':15200+index*2+(language=='fa'),'apply_text_normalization':'on'};persian_voice_profile.apply(request,language)
   payload['clips'].append({'id':identity,'speaker':speaker,'locale':locale,'destination':audio.API_ROOT+'/v1/text-to-speech/'+audio.VOICE_IDS[speaker],'request':request})
 REPORT.mkdir(parents=True,exist_ok=True);(REPORT/'voice_payload_review.json').write_text(json.dumps(payload,ensure_ascii=False,indent=2)+'\n')
 if not args.generate:print('[TrustUnderFireVoicePayload] result=Passed lines=7 clips=14 externalRequests=0');return
 key=audio.read_api_key(ROOT/'.local/secrets/elevenlabs_api_key');sub=audio.request_json(key,'/v1/user/subscription')
 if sub.get('status')!='active' or sub.get('tier') in {None,'free'}:raise RuntimeError('Active paid subscription required')
 manifest=json.loads(MANIFEST.read_text()) if MANIFEST.exists() else {'clips':[]}
 manifest.update(schema='WarlineCapture.CampaignBilingualVoice.v1',missionId=payload['missionId'],provider='ElevenLabs',model=audio.MODEL,license=audio.RIGHTS,runtimeNetworkTts=False,generatedAtUtc=dt.datetime.now(dt.timezone.utc).isoformat(),subscription={k:sub[k] for k in ('tier','status')},processing={'sampleRateHz':44100,'channels':1,'sourceEncoding':'PCM_S16LE','loudnessLUFS':-18},castAssignment={'Dalia':'Existing canonical project cast','Samira':'Existing canonical project cast','Lina':'Existing Sarah cast, Dr. Lina Mansour','ARIA':'Existing canonical project cast'})
 for index,(identity,speaker,en,fa) in enumerate(entries):
  for language,locale,text in [('en','en-US',en),('fa','fa-IR',fa)]:
   path=MANIFEST.parent/'Voice'/language/(identity+'.wav');old=next((c for c in manifest['clips'] if c['id']==identity and c['locale']==locale),{})
   if not persian_voice_profile.clip_matches(old,text,path,language):audio.convert(audio.request_audio(key,audio.VOICE_IDS[speaker],text,language,15200+index*2+(language=='fa')),path,speaker)
   rec=audio.record('narrative',identity,speaker,locale,text,path);rec['captionSha256']=hashlib.sha256(text.encode()).hexdigest()
   manifest['clips']=[c for c in manifest['clips'] if (c['id'],c['locale'])!=(identity,locale)]+[rec];MANIFEST.parent.mkdir(parents=True,exist_ok=True);MANIFEST.write_text(json.dumps(manifest,ensure_ascii=False,indent=2)+'\n')
   print(f"[TrustUnderFireVoice] {identity} {locale} duration={rec['durationSeconds']:.2f}s",flush=True)
 print('[TrustUnderFireVoiceGeneration] result=Passed clips=14 locales=2 runtimeNetworkTts=0')
if __name__=='__main__':main()
