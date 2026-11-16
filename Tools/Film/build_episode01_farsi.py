#!/usr/bin/env python3
"""Localize the corrected animated cut using existing Persian cast recordings."""
import concurrent.futures
import hashlib
import json
import subprocess
import textwrap
import wave
from pathlib import Path

import numpy as np
import build_episode01 as audio
from recut_episode01_speakers import ALLOWED

ROOT=Path(__file__).resolve().parents[2]
EN=ROOT/'Design/CampaignFilm/Episode01/v03'
OUT=ROOT/'Design/CampaignFilm/Episode01/fa/v01'
WORK=OUT/'work'
FPS=24
SR=audio.SR
EN_DELIVERY=ROOT/'Videos/Episode01_FirstContact_Corrected_720p.mp4'
# Expand speaker blocks to accommodate natural Persian delivery, keeping the
# corrected cuts, rejected-character exclusion and continuous ending intact.
ANCHORS=[(0,0),(10,10),(27.125,28.5),(36.5,37.875),(44.125,47.75),
         (57.25,60.875),(64.25,68.5),(70.75,76),(78,84.75),(80,86.75),
         (84.125,92.25),(85.75,93.875),(91.75,99.875),(100,109.125)]
TOTAL=109.125
STARTS={'p02_radio':13.5,'p03_radio':20,'p04_dalia':28.75,'p04_samira':38.125,
        'p05_aria':48,'p10_aria':54.875,'p14_commander':61.125,'p15_dalia':68.75,
        'm01-brief.line.3':76.25,'m01-comms.line.3':87,
        'm01-debrief.line.1':94.125,'m01-debrief.line.2':100.125}
NAMES={'RADIO':'مرکز اعزام منطقه','DALIA':'سرگرد دالیا رحیم','SAMIRA':'مهندس سمیرا حداد',
       'ARIA':'آریا','COMMANDER':'فرمانده'}

def sha(p): return hashlib.sha256(Path(p).read_bytes()).hexdigest()
def run(args,log):
    result=subprocess.run([str(a) for a in args],capture_output=True)
    (WORK/log).write_bytes(result.stdout+result.stderr)
    if result.returncode: raise RuntimeError(result.stderr.decode(errors='replace')[-3000:])
    return result.stdout
def mapped(t):
    for (old,new),(old_end,new_end) in zip(ANCHORS,ANCHORS[1:]):
        if old<=t<=old_end:
            return round((new+(t-old)/(old_end-old)*(new_end-new))*FPS)/FPS
    raise ValueError(t)
def probe(p):
    return json.loads(subprocess.check_output(['ffprobe','-v','error','-show_streams','-show_format','-of','json',str(p)]))

def render(item):
    index,shot=item
    start,end=mapped(shot['start']),mapped(shot['end'])
    count=round((end-start)*FPS)
    src=EN/'work'/f'{index:02}.mp4'
    original_frames=shot['frames']
    if index==0:
        # Avoid the English title baked into the English opening segment.
        src=ROOT/'Design/CampaignFilm/Episode01/v02/clips/01_market.mp4'
    else:
        assert int(probe(src)['streams'][0]['nb_frames'])==original_frames
    factor=(count-1)/(original_frames-1)
    vf=f'trim=end_frame={original_frames},setpts=(PTS-STARTPTS)*{factor:.12f},fps=24,setsar=1,tpad=stop_mode=clone:stop_duration=0.083333'
    dest=WORK/f'{index:02}.mp4'
    run(['ffmpeg','-v','error','-y','-i',src,'-vf',vf,'-frames:v',count,'-an',
         '-c:v','libx264','-preset','fast','-crf',19,'-pix_fmt','yuv420p','-threads',2,dest],f'segment_{index:02}.log')
    assert int(probe(dest)['streams'][0]['nb_frames'])==count
    return dict(index=index,start=start,end=end,frames=count,visibleSubject=shot['visibleSubject'],
                source=str(src),sourceSha256=sha(src),originalShot=shot)

def main():
    WORK.mkdir(parents=True,exist_ok=True)
    original=json.loads((EN/'speaker_alignment_manifest.json').read_text())
    english_sha=sha(EN_DELIVERY)
    assert original['sha256']==english_sha
    assert len(original['shots'])==17
    first=json.loads((ROOT/'Assets/Game/Audio/Narrative/FirstLaunch/first_launch_persian_voice_manifest.json').read_text())
    mission=json.loads((ROOT/'Assets/Game/Audio/Narrative/M01FirstContact/m01_comic_voice_manifest.json').read_text())
    lookup={x['clipId']:x for x in first['clips']+first['voiceVariants']}
    lookup.update({x['id']:x for x in mission['clips'] if x.get('locale')=='fa-IR'})
    rows=[]
    for old in original['dialogue']:
        ident=old['id']; c=lookup['p14_commander_neutral' if ident=='p14_commander' else ident]
        p=ROOT/c['assetPath']; assert sha(p)==c['sha256']
        duration=audio.length(p)
        rows.append(dict(id=ident,speaker=old['speaker'],start=STARTS[ident],
                         end=STARTS[ident]+duration,text=c['text'],voice=c['assetPath'],
                         voiceId=c['voiceId'],voiceSha256=sha(p),playbackSpeed=1.0))
    for a,b in zip(rows,rows[1:]): assert a['end']<=b['start'],(a,b)
    with concurrent.futures.ThreadPoolExecutor(max_workers=2) as pool:
        shots=list(pool.map(render,enumerate(original['shots'])))
    assert shots[0]['start']==0 and shots[-1]['end']==TOTAL
    assert all(a['end']==b['start'] for a,b in zip(shots,shots[1:]))
    for line in rows:
        overlaps=[s for s in shots if s['start']<line['end'] and s['end']>line['start']]
        assert all(s['visibleSubject'] in ALLOWED[line['speaker']] for s in overlaps),line
        line['pictureSubjects']=[s['visibleSubject'] for s in overlaps]
    (OUT/'dialogue_fa.json').write_text(json.dumps(rows,ensure_ascii=False,indent=2)+'\n')
    (WORK/'concat.txt').write_text(''.join(f"file '{i:02}.mp4'\n" for i in range(len(shots))))
    run(['ffmpeg','-v','error','-y','-f','concat','-safe',0,'-i',WORK/'concat.txt','-c','copy',WORK/'picture.mp4'],'concat.log')
    n=round(TOTAL*SR); voices=np.zeros((n,2),np.float32);duck=np.ones(n,np.float32)
    for line in rows:
        a=audio.normalized(audio.decode(ROOT/line['voice']),-20)
        fade=min(round(.008*SR),len(a)//4)
        a[:fade]*=np.linspace(0,1,fade)[:,None]; a[-fade:]*=np.linspace(1,0,fade)[:,None]
        lo=round(line['start']*SR);hi=lo+len(a)
        assert hi<=n
        voices[lo:hi]+=a;duck[lo:hi]=.28
        ramp=round(.25*SR); left=max(0,lo-ramp);right=min(n,hi+ramp)
        duck[left:lo]=np.minimum(duck[left:lo],np.linspace(1,.28,lo-left))
        duck[hi:right]=np.minimum(duck[hi:right],np.linspace(.28,1,right-hi))
    env=ROOT/'Assets/Game/Audio/Narrative/FirstLaunch/Environment'
    def track(name,db):return audio.looped(audio.normalized(audio.decode(env/name),db),n)
    calm=track('first_launch_story_calm_loop_01.wav',-29)
    crisis=track('first_launch_story_crisis_loop_01.wav',-28)
    blend=np.zeros(n,np.float32); crisis_start=10*SR; recovery=round(92.25*SR)
    blend[crisis_start:recovery]=1;blend[crisis_start:crisis_start+2*SR]=np.linspace(0,1,2*SR)
    blend[recovery:recovery+2*SR]=np.linspace(1,0,2*SR)
    score=(calm*(1-blend[:,None])+crisis*blend[:,None])*duck[:,None]
    ambience=track('first_launch_city_market_loop_01.wav',-40)
    room=track('first_launch_command_room_loop_01.wav',-40)
    for lo,hi in ((47.75,68.5),(99.875,TOTAL)):
        lo,hi=round(lo*SR),round(hi*SR);ambience[lo:hi]=room[lo:hi]
    mix=voices+score+ambience
    event=audio.normalized(audio.decode(env/'first_launch_distant_attack_event_01.wav'),-26)[:4*SR]
    event_start=round(16.3*SR);mix[event_start:event_start+len(event)]+=event
    mix[:SR]*=np.linspace(0,1,SR)[:,None];mix[-SR:]*=np.linspace(1,0,SR)[:,None]
    peak=float(np.abs(mix).max());gain=min(1,.89/max(peak,1e-6));mix*=gain
    with wave.open(str(WORK/'mix_fa.wav'),'wb') as w:
        w.setnchannels(2);w.setsampwidth(2);w.setframerate(SR)
        w.writeframes((np.clip(mix,-1,1)*32767).astype('<i2').tobytes())
    srt=[]
    for index,line in enumerate(rows,1):
        caption='\n'.join(textwrap.wrap(line['text'],width=54,break_long_words=False))
        srt += [str(index),audio.timestamp(line['start'])+' --> '+audio.timestamp(line['end']),NAMES[line['speaker']]+':',caption,'']
    captions=OUT/'Episode01_FirstContact.fa.srt';captions.write_text('\n'.join(srt),encoding='utf-8')
    for name in ['title_fa']+['label_'+s for s in NAMES]: assert (WORK/(name+'.png')).is_file()
    args=['ffmpeg','-v','error','-y','-i',WORK/'picture.mp4','-i',WORK/'mix_fa.wav',
          '-loop',1,'-i',WORK/'title_fa.png']
    graph=["[0:v][2:v]overlay=enable='between(t,1,4)':shortest=1[title]"]
    previous='title'
    for index,speaker in enumerate(NAMES,3):
        args+=['-loop',1,'-i',WORK/('label_'+speaker+'.png')]
        lines=[x for x in rows if x['speaker']==speaker]
        enable='+'.join(f"between(t,{x['start']:.6f},{TOTAL if x['id']=='m01-debrief.line.2' else x['end']:.6f})" for x in lines)
        output='label'+str(index)
        graph.append(f"[{previous}][{index}:v]overlay=enable='{enable}':shortest=1[{output}]")
        previous=output
    graph.append(f'[{previous}]fade=t=in:d=0.4,fade=t=out:st={TOTAL-1.25}:d=1[v]')
    args+=['-i',captions]
    master=OUT/'Episode01_FirstContact_Farsi_720p.mp4'
    args+=['-filter_complex',';'.join(graph),'-map','[v]','-map','1:a:0','-map','8:s:0','-t',TOTAL,
           '-c:v','libx264','-preset','fast','-crf',20,'-maxrate','3M','-bufsize','6M','-threads',2,
           '-pix_fmt','yuv420p','-c:a','aac','-b:a','128k','-af','loudnorm=I=-16:TP=-1.5:LRA=11',
           '-ar',48000,'-c:s','mov_text','-metadata:s:a:0','language=fas','-metadata:s:s:0','language=fas',
           '-metadata:s:s:0','title=فارسی','-disposition:s:0',0,'-metadata','title=وارلاین — نخستین تماس — قسمت اول',
           '-movflags','+faststart',master]
    run(args,'master.log')
    run(['ffmpeg','-v','error','-i',master,'-f','null','-'],'decode_check.log')
    result=probe(master)
    assert int(result['streams'][0]['nb_frames'])==round(TOTAL*FPS)
    assert abs(float(result['format']['duration'])-TOTAL)<.03
    assert sha(EN_DELIVERY)==english_sha
    manifest=dict(locale='fa-IR',durationSeconds=TOTAL,frameRate=FPS,creditsUsed=0,
        generationCalls=0,sourceEnglishSha256=english_sha,shots=shots,dialogue=rows,
        naturalVoiceSpeed=True,subtitlePath=str(captions),master=str(master),sha256=sha(master),
        audioPreMasterPeak=peak,audioSafetyGain=gain,fullDecodePassed=True,
        pending=['Human Persian audiovisual review','Real mobile playback'],
        limitations=['Existing generated mouth animation has not been lip-synced.'])
    (OUT/'production_manifest.json').write_text(json.dumps(manifest,ensure_ascii=False,indent=2)+'\n')
    print(master)

if __name__=='__main__': main()
