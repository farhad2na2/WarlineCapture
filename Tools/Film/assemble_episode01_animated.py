#!/usr/bin/env python3
"""Assemble downloaded generative footage; never substitute stills for missing clips."""
import json
import subprocess
import wave
from pathlib import Path

import numpy as np
import build_episode01 as audio

ROOT = Path(__file__).resolve().parents[2]
OUT = ROOT / 'Design/CampaignFilm/Episode01/v02'
WORK = OUT / 'work'
SR = audio.SR
SHOTS = [('01_market',10),('02_attack',15),('03_rescue',15),('04_aria',15),
         ('05_command',15),('06_corridor',15),('07_aftermath',15)]
LINES = [(13.5,'p02_radio'),(20,'p03_radio'),(28,'p04_dalia'),(36.5,'p04_samira'),
         (44.5,'p05_aria'),(53,'p10_aria'),(58,'p14_commander'),(65,'p15_dalia'),
         (71.5,'m01-brief.line.3'),(80,'m01-comms.line.3'),
         (86,'m01-debrief.line.1'),(92,'m01-debrief.line.2')]

def run(args, log):
    p=subprocess.run([str(a) for a in args],capture_output=True)
    (WORK/log).write_bytes(p.stdout+p.stderr)
    if p.returncode: raise RuntimeError(p.stderr.decode(errors='replace')[-3000:])
    return p.stdout

def main():
    # Fail before rendering if any actual generated shot is absent.
    missing=[name for name,_ in SHOTS if not (OUT/'clips'/f'{name}.mp4').is_file()]
    if missing: raise SystemExit('Missing generated footage: '+', '.join(missing))
    WORK.mkdir(parents=True,exist_ok=True)
    for i,(name,duration) in enumerate(SHOTS):
        src=OUT/'clips'/f'{name}.mp4'
        probe=json.loads(subprocess.check_output(['ffprobe','-v','error','-show_format','-show_streams','-of','json',str(src)]))
        actual=float(probe['format']['duration'])
        minimum=13.75 if name=='02_attack' else duration-.25
        if actual<minimum: raise ValueError(f'{name}: short clip {actual}')
        # A 14-second attack take fills its 15-second slot with a small playback
        # speed adjustment; never loop a shot or hold its final frame.
        retime=f'setpts={duration/actual:.9f}*(PTS-STARTPTS),' if actual<duration-.25 else ''
        run(['ffmpeg','-v','error','-y','-i',src,'-t',duration,'-vf',retime+'scale=1280:720:force_original_aspect_ratio=increase,crop=1280:720,setsar=1,fps=24','-an','-c:v','libx264','-preset','fast','-crf','19','-pix_fmt','yuv420p','-threads','2',WORK/f'{i:02}.mp4'],f'normalize-{i:02}.log')
    (WORK/'concat.txt').write_text(''.join(f"file '{i:02}.mp4'\n" for i in range(len(SHOTS))))
    run(['ffmpeg','-v','error','-y','-f','concat','-safe','0','-i',WORK/'concat.txt','-c','copy',WORK/'picture.mp4'],'concat.log')
    lookup={s.get('id'):s for s in audio.plan() if s.get('id')}
    total=sum(d for _,d in SHOTS); n=round(total*SR)
    voice=np.zeros((n,2),np.float32); duck=np.ones(n,np.float32)
    lines=[]
    for start,ident in LINES:
        s=lookup[ident]; a=audio.decode(ROOT/s['voice']); lo=round(start*SR); hi=lo+len(a)
        if hi>n: raise ValueError(f'Dialogue exceeds film: {ident}')
        voice[lo:hi]+=a; duck[lo:hi]=.28
        r=round(.25*SR); left=max(0,lo-r);right=min(n,hi+r)
        duck[left:lo]=np.minimum(duck[left:lo],np.linspace(1,.28,lo-left))
        duck[hi:right]=np.minimum(duck[hi:right],np.linspace(.28,1,right-hi))
        lines.append(dict(id=ident,start=start,end=start+len(a)/SR,speaker=s['speaker'],text=s['text'],voice=s['voice'],voiceId=s['voiceId']))
    env=ROOT/'Assets/Game/Audio/Narrative/FirstLaunch/Environment'
    def track(name,db): return audio.looped(audio.normalized(audio.decode(env/name),db),n)
    calm=track('first_launch_story_calm_loop_01.wav',-29)
    crisis=track('first_launch_story_crisis_loop_01.wav',-28)
    blend=np.zeros(n,np.float32);blend[10*SR:85*SR]=1
    blend[10*SR:12*SR]=np.linspace(0,1,2*SR)
    blend[85*SR:87*SR]=np.linspace(1,0,2*SR)
    score=(calm*(1-blend[:,None])+crisis*blend[:,None])*duck[:,None]
    ambience=track('first_launch_city_market_loop_01.wav',-40)
    room=track('first_launch_command_room_loop_01.wav',-40)
    ambience[40*SR:70*SR]=room[40*SR:70*SR]
    mix=voice+score+ambience
    event=audio.normalized(audio.decode(env/'first_launch_distant_attack_event_01.wav'),-26)[:4*SR]
    mix[12*SR:12*SR+len(event)]+=event
    fade=SR;mix[:fade]*=np.linspace(0,1,fade)[:,None];mix[-fade:]*=np.linspace(1,0,fade)[:,None]
    peak=float(np.max(np.abs(mix)));gain=min(1,.89/max(peak,1e-6));mix*=gain
    with wave.open(str(WORK/'mix.wav'),'wb') as w:
        w.setnchannels(2);w.setsampwidth(2);w.setframerate(SR);w.writeframes((np.clip(mix,-1,1)*32767).astype('<i2').tobytes())
    srt=[]
    for i,s in enumerate(lines,1):
        srt.extend([str(i),audio.timestamp(s['start'])+' --> '+audio.timestamp(s['end']),s['text'],''])
    (OUT/'Episode01_FirstContact.en.srt').write_text('\n'.join(srt))
    # Separately authored text is composited over moving generated footage.
    title=WORK/'title.png';audio.overlay(title,title=['FIRST CONTACT','EPISODE 01'])
    master=OUT/'Episode01_FirstContact_Animated_720p_en.mp4'
    run(['ffmpeg','-v','error','-y','-i',WORK/'picture.mp4','-i',WORK/'mix.wav','-loop','1','-i',title,
         '-filter_complex',"[0:v][2:v]overlay=enable='between(t,1,4)':shortest=1,fade=t=in:d=0.4,fade=t=out:st=99:d=1[v]",
         '-map','[v]','-map','1:a','-t',total,'-c:v','libx264','-preset','fast','-crf','20','-maxrate','3M','-bufsize','6M','-threads','2','-pix_fmt','yuv420p','-c:a','aac','-b:a','128k','-af','loudnorm=I=-16:TP=-1.5:LRA=11','-ar','48000','-movflags','+faststart',master],'master.log')
    run(['ffmpeg','-v','error','-i',master,'-f','null','-'],'decode-check.log')
    (OUT/'edit_manifest.json').write_text(json.dumps({'durationSeconds':total,'shots':SHOTS,'dialogue':lines,'audioPreMasterPeak':peak,'audioSafetyGain':gain,'master':str(master),'sha256':audio.sha(master),'pending':['Human audiovisual review','Real mobile playback','In-game integration'],'production':'Generated actor and environment motion; established ElevenLabs cast played as radio and console dialogue. No still-image substitute.'},indent=2)+'\n')
    print(master)

if __name__=='__main__': main()
