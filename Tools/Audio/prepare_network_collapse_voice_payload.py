#!/usr/bin/env python3
"""Prepare Network Collapse's local bilingual voice draft. This script never sends requests."""
from __future__ import annotations
import hashlib, json, re
from pathlib import Path
import persian_voice_profile
ROOT=Path(__file__).resolve().parents[2]
SOURCE=ROOT/'Assets/Game/Scripts/Configs/Localization/CH05M03NetworkCollapseNarrativeCopy.cs'
REPORT=ROOT/'Design/AgentReports/CH05M03NetworkCollapse/Narrative'
VOICE_IDS={'DALIA':'MK1Zvh93428YrgOQ8Obr','SAMIRA':'7uxeJ73HfJL9gOH2mttA','ARIA':'Fi9tPTnEcbh3of7hOHC8'}
def lines():
    string=r'"((?:[^"\\]|\\.)*)"'
    rows=re.findall(r'new\('+string+r',\s*NarrativeSpeakerId\.(\w+),\s*'+string+r',\s*'+string+r'\)',SOURCE.read_text())
    if len(rows)!=7:raise RuntimeError('Expected seven canonical Network Collapse lines')
    decode=lambda v:json.loads('"'+v+'"')
    return [('network_collapse-'+decode(i),sp.upper(),decode(en),decode(fa)) for i,sp,en,fa in rows]
def main():
    payload={'status':'LocalDraftOnly_NoUploadAuthorized','missionId':'saga.ch05.m03.network_collapse','providerDraft':'ElevenLabs','modelDraft':'eleven_v3','clipCountDraft':14,'installedClips':0,'externalRequests':0,'runtimeNetworkTts':False,'cast':'Existing canonical Dalia, Samira and ARIA voices; no previous mission voice clips bound','clips':[]}
    for index,(identity,speaker,en,fa) in enumerate(lines()):
        for language,locale,text in [('en','en-US',en),('fa','fa-IR',fa)]:
            request={'text':text,'model_id':'eleven_v3','language_code':language,'seed':15300+index*2+(language=='fa'),'apply_text_normalization':'on'}
            persian_voice_profile.apply(request,language)
            payload['clips'].append({'id':identity,'speaker':speaker,'locale':locale,'voiceIdDraft':VOICE_IDS[speaker],'destinationDraft':'https://api.elevenlabs.io/v1/text-to-speech/'+VOICE_IDS[speaker],'assetPathDraft':'Assets/Game/Audio/Narrative/CH05M03NetworkCollapse/Voice/'+language+'/'+identity+'.wav','caption':text,'captionSha256':hashlib.sha256(text.encode()).hexdigest(),'requestDraft':request})
    REPORT.mkdir(parents=True,exist_ok=True)
    (REPORT/'voice_payload_draft.json').write_text(json.dumps(payload,ensure_ascii=False,indent=2)+'\n')
    print('[NetworkCollapseLocalVoiceDraft] result=Passed lines=7 proposedClips=14 installedClips=0 externalRequests=0')
if __name__=='__main__':main()
