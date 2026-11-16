#!/usr/bin/env python3
"""Assemble the First Contact review cut from existing approved art and recordings.
No generation APIs are called. All outputs are outside Assets; game bindings are untouched.
"""
from pathlib import Path
import argparse, hashlib, json, math, subprocess, textwrap, wave
import numpy as np
from PIL import Image, ImageDraw, ImageFont

ROOT = Path(__file__).resolve().parents[2]
OUT = ROOT / "Design/CampaignFilm/Episode01/v01"
WORK = OUT / "work"
FPS, SR, W, H = 24, 44100, 1280, 720
ART = ROOT / "Assets/Game/Art/Narrative/FirstLaunch/Panels/16x9"
BOLD = "/System/Library/Fonts/Supplemental/Arial Bold.ttf"
REGULAR = "/System/Library/Fonts/Supplemental/Arial.ttf"
NAMES = {"RADIO":"DISTRICT DISPATCH", "DALIA":"MAJOR DALIA RAHIM", "SAMIRA":"ENGINEER SAMIRA HADDAD", "ARIA":"ARIA", "COMMANDER":"COMMANDER"}
COLORS = {"RADIO":"#eed8ac", "DALIA":"#e8c18c", "SAMIRA":"#a9d3b5", "ARIA":"#83dce3", "COMMANDER":"#e7e3d8"}

def run(args, log=None):
    result = subprocess.run([str(a) for a in args], capture_output=True)
    if log: Path(log).write_bytes(result.stdout + result.stderr)
    if result.returncode:
        raise RuntimeError(result.stderr.decode(errors="replace")[-5000:])
    return result.stdout

def sha(p): return hashlib.sha256(Path(p).read_bytes()).hexdigest()
def rel(p): return str(Path(p).relative_to(ROOT))
def font(size, bold=False): return ImageFont.truetype(BOLD if bold else REGULAR,size)
def length(p):
    with wave.open(str(p)) as w: return w.getnframes()/w.getframerate()

def wrapped(text, f, width):
    d=ImageDraw.Draw(Image.new("RGB",(1,1))); lines=[]; current=""
    for word in text.split():
        test=(current+" "+word).strip()
        if d.textlength(test,font=f)>width and current: lines.append(current); current=word
        else: current=test
    if current: lines.append(current)
    return lines

def overlay(path, text=None, speaker=None, title=None):
    im=Image.new("RGBA",(W,H),(0,0,0,0)); d=ImageDraw.Draw(im)
    if title:
        # Editable typography is composited separately from the approved illustration.
        for y in range(H): d.line((0,y,W,y),fill=(7,14,20,175))
        d.rectangle((95,245,153,249),fill="#77d3db")
        d.text((94,193),"WARLINE  /  SHATTERED RELAY",font=font(19,True),fill="#c8d2d4")
        d.text((90,271),title[0],font=font(63,True),fill="#f3ecdf")
        d.text((95,352),title[1],font=font(23),fill="#d4d6cf")
        d.text((95,619),"CHAPTER 01     FIRST RESPONSE",font=font(17,True),fill="#83dce3")
    else:
        lines=wrapped(text,font(30),1120)
        assert len(lines)<=3, (text,lines)
        top=H-48-len(lines)*38-48
        for y in range(top-40,H):
            a=int(218*min(1,max(0,(y-(top-40))/80)))
            d.line((0,y,W,y),fill=(5,10,15,a))
        d.rectangle((70,top+3,74,top+23),fill=COLORS[speaker])
        d.text((88,top),NAMES[speaker],font=font(18,True),fill=COLORS[speaker])
        for i,line in enumerate(lines): d.text((70,top+34+i*38),line,font=font(30),fill="#faf5e9",stroke_width=1,stroke_fill=(0,0,0,100))
    im.save(path)

def timestamp(t):
    ms=round(t*1000); h,ms=divmod(ms,3600000); m,ms=divmod(ms,60000); s,ms=divmod(ms,1000)
    return f"{h:02}:{m:02}:{s:02},{ms:03}"

def plan():
    catalog=json.loads((ROOT/"Assets/Game/Data/Narrative/FirstLaunch/first_launch_english_text_catalog.json").read_text())
    text={c["lineId"]:c for c in catalog["lines"]}
    first=json.loads((ROOT/"Assets/Game/Audio/Narrative/FirstLaunch/first_launch_temp_voice_manifest.json").read_text())
    first={c["clipId"]:c for c in first["clips"]}
    mission=json.loads((ROOT/"Assets/Game/Audio/Narrative/M01FirstContact/m01_comic_voice_manifest.json").read_text())
    mission={c["id"]:c for c in mission["clips"] if c["locale"]=="en-US"}
    scenes=[]
    def add(panel, line=None, group="opening", seconds=None, title=None):
        s={"panel":f"FL-P{panel:02}","image":rel(ART/f"FL-P{panel:02}.png"),"group":group}
        if title:s["title"]=title
        if line:
            c=dict(first[line] if line.startswith("p") else mission[line])
            s.update(id=line,speaker=c["speaker"],voice=c["assetPath"],text=text[line]["text"] if line.startswith("p") else c["text"])
            if line=="p14_commander":
                s["voice"]="Assets/Game/Audio/Narrative/FirstLaunch/Voice/p14_commander_neutral.wav"
                s["voiceId"]="RaldHBrhNdVemVVS3wAW"
            else:s["voiceId"]=c["voiceId"]
            s["lead"]=.6; s["spokenSeconds"]=length(ROOT/s["voice"])
            s["duration"]=math.ceil((s["spokenSeconds"]+1.5)*FPS)/FPS
        else:s["duration"]=seconds
        scenes.append(s)
    add(1,seconds=4,title=["FIRST CONTACT","EPISODE 01"])
    add(1,seconds=4)
    for panel,line in [(2,"p02_radio"),(3,"p03_radio"),(4,"p04_dalia"),(12,"p04_samira"),(5,"p05_aria"),(6,"p06_aria"),(9,"p09_aria"),(10,"p10_aria"),(11,"p11_dalia"),(12,"p12_samira"),(13,"p13_aria"),(14,"p14_commander"),(15,"p15_dalia")]:add(panel,line)
    add(15,"m01-brief.line.2",group="briefing")
    add(16,"m01-brief.line.3",group="briefing")
    add(18,"m01-comms.line.1",group="operation")
    add(18,group="operation",seconds=4)
    add(19,"m01-comms.line.3",group="operation")
    add(19,group="operation",seconds=3)
    add(21,"m01-debrief.line.1",group="aftermath")
    add(20,"m01-debrief.line.2",group="aftermath")
    add(22,group="aftermath",seconds=4)
    add(22,group="aftermath",seconds=4,title=["SHATTERED RELAY","The response has begun."])
    t=0
    for n,s in enumerate(scenes):
        s.update(index=n,start=t,end=t+s["duration"])
        s["imageSha256"]=sha(ROOT/s["image"])
        if s.get("voice"):s["voiceSha256"]=sha(ROOT/s["voice"])
        t=s["end"]
    return scenes

def render_scene(s):
    n=s["index"]; dest=WORK/f"scene-{n:02}.mp4"
    if dest.exists():return
    frames=round(s["duration"]*FPS)
    # Oversample before zoompan to keep subpixel motion smooth at mobile resolution.
    z=f"1.025+0.045*on/{max(1,frames-1)}" if n%2==0 else f"1.07-0.045*on/{max(1,frames-1)}"
    x="iw/2-iw/zoom/2"; y="ih/2-ih/zoom/2"
    graph=f"[0:v]scale=3840:2160:flags=lanczos,zoompan=z='{z}':x='{x}':y='{y}':d={frames}:s=1280x720:fps=24,setsar=1[v]"
    args=["ffmpeg","-hide_banner","-loglevel","error","-y","-filter_complex_threads","1","-i",ROOT/s["image"]]
    if s.get("text") or s.get("title"):
        png=WORK/f"overlay-{n:02}.png";overlay(png,s.get("text"),s.get("speaker"),s.get("title"))
        args += ["-loop","1","-i",png]
        enable="1" if s.get("title") else f"between(t,{s['lead']},{s['lead']+s['spokenSeconds']+.15})"
        graph += f";[v][1:v]overlay=0:0:enable='{enable}':shortest=1[out]"
    else:graph += ";[v]null[out]"
    if n==0 or n==24:
        graph += f";[out]fade=t=in:st=0:d=0.4,fade=t=out:st={s['duration']-.6}:d=0.6[final]"; label="[final]"
    else:label="[out]"
    args += ["-filter_complex",graph,"-map",label,"-frames:v",str(frames),"-an","-c:v","libx264","-preset","fast","-crf","19","-maxrate","3M","-bufsize","6M","-pix_fmt","yuv420p","-threads","2",dest]
    run(args,WORK/f"scene-{n:02}.log")

def decode(p):
    b=run(["ffmpeg","-v","error","-i",p,"-f","f32le","-ac","2","-ar",str(SR),"pipe:1"])
    return np.frombuffer(b,dtype="<f4").reshape(-1,2).copy()

def rms(a):return float(np.sqrt(np.mean(a*a)+1e-12))
def normalized(a,db):return a*(10**(db/20)/max(rms(a),1e-6))
def looped(a,n):
    # Crossfade loop boundaries using a one-second overlap.
    cross=min(SR,len(a)//8); hop=len(a)-cross
    result=np.zeros((n,2),dtype=np.float32)
    for start in range(0,n,hop):
        piece=a[:min(len(a),n-start)].copy()
        if start:
            f=min(cross,len(piece)); piece[:f]*=np.linspace(0,1,f,dtype=np.float32)[:,None]
        if start+len(a)<n:
            piece[-cross:]*=np.linspace(1,0,cross,dtype=np.float32)[:,None]
        result[start:start+len(piece)]+=piece
    return result

def mix(scenes):
    total=scenes[-1]["end"]; n=round(total*SR)
    voices=np.zeros((n,2),np.float32); duck=np.ones(n,np.float32)
    for s in scenes:
        if not s.get("voice"):continue
        a=decode(ROOT/s["voice"]); start=round((s["start"]+s["lead"])*SR); end=start+len(a)
        fade=min(441,len(a)//8); a[:fade]*=np.linspace(0,1,fade)[:,None];a[-fade:]*=np.linspace(1,0,fade)[:,None]
        voices[start:end]+=a
        duck[start:end]=np.minimum(duck[start:end],.28)
        r=round(.3*SR); lo=max(0,start-r); hi=min(n,end+r)
        duck[lo:start]=np.minimum(duck[lo:start],np.linspace(1,.28,start-lo))
        duck[end:hi]=np.minimum(duck[end:hi],np.linspace(.28,1,hi-end))
    envroot=ROOT/"Assets/Game/Audio/Narrative/FirstLaunch/Environment"
    calm=looped(normalized(decode(envroot/"first_launch_story_calm_loop_01.wav"),-28),n)
    crisis=looped(normalized(decode(envroot/"first_launch_story_crisis_loop_01.wav"),-27),n)
    attack=round(scenes[2]["start"]*SR); recovery=round(next(s["start"] for s in scenes if s["group"]=="aftermath")*SR)
    blend=np.zeros(n,np.float32); blend[attack:recovery]=1
    ramp=SR
    blend[attack:attack+ramp]=np.linspace(0,1,ramp); blend[recovery:recovery+ramp]=np.linspace(1,0,ramp)
    score=(calm*(1-blend[:,None])+crisis*blend[:,None])*duck[:,None]
    market=looped(normalized(decode(envroot/"first_launch_city_market_loop_01.wav"),-39),n)
    room=looped(normalized(decode(envroot/"first_launch_command_room_loop_01.wav"),-40),n)
    ambience=np.zeros((n,2),np.float32)
    for s in scenes:
        lo=round(s["start"]*SR);hi=round(s["end"]*SR)
        ambience[lo:hi]=(room if int(s["panel"][-2:]) in (3,5,6,9,14,20,22) else market)[lo:hi]
    mix=voices+score+ambience
    event=normalized(decode(envroot/"first_launch_distant_attack_event_01.wav"),-25)
    event=event[:min(len(event),round(4*SR))]; mix[attack:attack+len(event)]+=event
    fade=round(1.2*SR);mix[:fade]*=np.linspace(0,1,fade)[:,None];mix[-fade:]*=np.linspace(1,0,fade)[:,None]
    peak=float(np.max(np.abs(mix))); gain=min(1,.89/max(peak,1e-6));mix*=gain
    with wave.open(str(WORK/"episode-mix.wav"),"wb") as w:
        w.setnchannels(2);w.setsampwidth(2);w.setframerate(SR);w.writeframes((np.clip(mix,-1,1)*32767).astype("<i2").tobytes())
    return {"preMasterPeak":peak,"safetyGain":gain,"musicDuckingDuringDialogueDb":round(20*math.log10(.28),2)}

def build():
    OUT.mkdir(parents=True,exist_ok=True);WORK.mkdir(exist_ok=True)
    scenes=plan();(OUT/"timeline.json").write_text(json.dumps(scenes,indent=2)+"\n")
    srt=[]
    for s in scenes:
        if s.get("text"):
            srt += [str(len(srt)//4+1),timestamp(s["start"]+s["lead"])+" --> "+timestamp(s["start"]+s["lead"]+s["spokenSeconds"]),NAMES[s["speaker"]]+": "+s["text"],""]
    (OUT/"Episode01_FirstContact.en.srt").write_text("\n".join(srt))
    for s in scenes:
        render_scene(s);print(f"Rendered scene {s['index']+1}/{len(scenes)}: {s['panel']}",flush=True)
    listing=WORK/"concat.txt";listing.write_text("".join(f"file 'scene-{s['index']:02}.mp4'\n" for s in scenes))
    run(["ffmpeg","-v","error","-y","-f","concat","-safe","0","-i",listing,"-c","copy",WORK/"picture.mp4"],WORK/"concat.log")
    audio=mix(scenes)
    video=OUT/"Episode01_FirstContact_720p_en.mp4"
    run(["ffmpeg","-v","error","-y","-i",WORK/"picture.mp4","-i",WORK/"episode-mix.wav","-map","0:v","-map","1:a","-c:v","copy","-c:a","aac","-b:a","128k","-af","loudnorm=I=-16:TP=-1.5:LRA=11","-ar","48000","-movflags","+faststart","-shortest",video],WORK/"master.log")
    operation=next(s["start"] for s in scenes if s["group"]=="operation")
    aftermath=next(s["start"] for s in scenes if s["group"]=="aftermath")
    outputs=[video]
    for name,start,end in [("BeforeMission",0,operation),("Aftermath",aftermath,scenes[-1]["end"])]:
        p=OUT/f"Episode01_{name}_720p_en.mp4"
        run(["ffmpeg","-v","error","-y","-ss",start,"-i",video,"-t",end-start,"-c:v","libx264","-preset","fast","-crf","19","-maxrate","3M","-bufsize","6M","-threads","2","-c:a","aac","-b:a","128k","-af",f"afade=t=in:d=0.25,afade=t=out:st={end-start-.4}:d=0.4","-movflags","+faststart",p],WORK/f"{name}.log")
        outputs.append(p)
    report={"status":"review_cut","episode":"First Contact","locale":"en-US","durationSeconds":scenes[-1]["end"],"format":{"width":W,"height":H,"fps":FPS,"video":"H.264","audio":"AAC stereo 48kHz 128kbps"},"production":"Existing approved illustrations with camera animation; existing ElevenLabs cast and environmental score. No newly generated actor motion.","creditsSpent":0,"pending":["Paid AI animated shots: browser automation unavailable; current Creator plan cannot use video API.","Human audiovisual review and real mobile playback.","In-game integration and native mission acceptance have not been performed."],"commanderVoice":"Existing neutral Commander variant","boundaries":{"operationStartSeconds":operation,"aftermathStartSeconds":aftermath},"audio":audio,"outputs":[{"path":rel(p),"bytes":p.stat().st_size,"sha256":sha(p)} for p in outputs]}
    (OUT/"production_manifest.json").write_text(json.dumps(report,indent=2)+"\n")
    print(json.dumps(report,indent=2),flush=True)

if __name__=="__main__":build()
