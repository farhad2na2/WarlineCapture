#!/usr/bin/env python3
"""Register completed Farsi M4 tutorial clips without changing English event bindings."""
import copy,json
from pathlib import Path
import generate_persian_conversational_rollout as rollout

def main():
 root=rollout.ROOT;path=root/'Assets/Game/Audio/Config/audio_event_catalog_v0_1.json';catalog=json.loads(path.read_text())
 before={e['eventId']:copy.deepcopy(e) for e in catalog['events']}
 template=next(e for e in catalog['events'] if e['eventId']=='VO.ARIA.Tutorial.M02.OpenBuild.En')
 m4=json.loads((root/'Assets/Game/Audio/Narrative/M04Airlift/m04_voice_manifest.json').read_text())
 clips=[c for c in m4['clips'] if c['kind']=='tutorial' and c['locale']=='fa-IR'];assert len(clips)==12
 ids={'vo.aria.tutorial.m04.'+c['id'].rsplit('-',1)[1]+'.fa' for c in clips}
 catalog['events']=[e for e in catalog['events'] if e['eventId'].lower() not in ids]
 for c in clips:
  assert c['deliveryProfile']=='fa-conversational-v1'
  event=copy.deepcopy(template);event['eventId']='vo.aria.tutorial.m04.'+c['id'].rsplit('-',1)[1]+'.fa'
  event['clips']=[{'assetPath':c['assetPath'],'status':'generated-elevenlabs','weight':1}];event.pop('localizedClips',None);catalog['events'].append(event)
 # Retired combined M1 Engage is a compatibility alias, never a second formal take.
 legacy=next(e for e in catalog['events'] if e['eventId']=='VO.ARIA.Tutorial.M01.Engage.Fa')
 legacy['clips']=[{'assetPath':'Assets/Game/Audio/Voice/Tutorial/fa/tutorial_m01_attack_target_aria_fa.wav','status':'generated-elevenlabs','weight':1}]
 changed=ids|{'vo.aria.tutorial.m01.engage.fa'}
 for e in catalog['events']:
  if e['eventId'].lower() not in changed:assert before[e['eventId']]==e
 path.write_text(json.dumps(catalog,ensure_ascii=False,indent=2)+'\n')
 print('[PersianVoiceEvents] result=Passed m04TutorialEvents=12 m01LegacyAlias=updated unrelatedEvents=unchanged')
if __name__=='__main__':main()
