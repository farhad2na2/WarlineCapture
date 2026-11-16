#!/usr/bin/env python3
"""Match Episode 1's existing dialogue to audited speaker footage."""
import concurrent.futures
import hashlib
import json
import subprocess
from pathlib import Path

from PIL import Image, ImageDraw
import build_episode01 as art

ROOT = Path(__file__).resolve().parents[2]
SOURCE = ROOT / 'Design/CampaignFilm/Episode01/v02'
OUT = SOURCE.parent / 'v03'
WORK = OUT / 'work'
FPS = 24
ORIGINAL = SOURCE / 'Episode01_FirstContact_Animated_720p_en.mp4'

# timeline start/end, source, source in/out, visible subject, composition
# Source bounds were inspected at one-second intervals and again at dialogue
# boundaries. Do not let the original model's internal cuts choose speakers.
EDL = [
    (0, 10, 'master', 0, 10, 'OPENING', None),
    (10, 27.125, '02_attack', 0, 14, 'DISPATCH', None),
    (27.125, 31.25, '03_rescue', 0, 3.2, 'DALIA', None),
    # Stop before the source cuts back to Dalia, and omit the 0.75s rear-view
    # insert. Hold the rescue action until Samira's introduction begins.
    (31.25, 36.5, '03_rescue', 4.15, 8.25, 'DALIA_RESCUE', None),
    (36.5, 40.625, '06_corridor', 4.05, 6.75, 'SAMIRA', None),
    (40.625, 44.125, '06_corridor', 9.75, 11.85, 'SAMIRA', None),
    (44.125, 57.25, '04_aria', 0, 15, 'ARIA', None),
    (57.25, 64.25, '05_command', 0, 4.65, 'COMMANDER', None),
    (64.25, 70.75, '05_command', 4.95, 8.5, 'DALIA', None),
    (70.75, 73.25, '06_corridor', 9.75, 11.85, 'SAMIRA', None),
    (73.25, 78, '07_aftermath', .1, 3.4, 'SAMIRA', None),
    # Start after the model's cut from Samira to the workers; its 7s frame
    # still belongs to Samira and otherwise appears as a one-frame flash.
    (78, 80, '06_corridor', 7.25, 9, 'ROAD_CREWS', None),
    (80, 84.125, '06_corridor', 4.05, 6.75, 'SAMIRA', 'crop=1066:600:0:40,'),
    (84.125, 85.75, '07_aftermath', 3.55, 5.175, 'FAMILY_REUNION', None),
    # The aftermath close-up drifted to a different woman and invented a shirt
    # name patch. Cover the complete debrief with the established Dalia design.
    (85.75, 91.75, '05_command', 4.95, 8.5, 'DALIA', 'crop=1066:600:80:0,'),
    (91.75, 95, '04_aria', 12.5, 15, 'ARIA', None),
    # Use only the final network-screen take; exclude the source's late angle cut.
    (95, 100, '07_aftermath', 13.5, 15, 'ARIA', None),
]
LABELS = {
    'RADIO': ('DISTRICT DISPATCH', 'RADIO CHANNEL', '#d7e1e4'),
    'DALIA': ('MAJOR DALIA RAHIM', 'JRC FIELD COMMAND', '#e9bf7a'),
    'SAMIRA': ('ENGINEER SAMIRA HADDAD', 'CIVIL INFRASTRUCTURE', '#ebd07b'),
    'ARIA': ('ARIA', 'CIVIC RELAY ASSISTANT', '#83dce3'),
    'COMMANDER': ('JRC COMMANDER', 'COMMAND CHANNEL', '#a8cce6'),
}
ALLOWED = {'RADIO': {'DISPATCH'}, 'DALIA': {'DALIA', 'DALIA_RESCUE'},
           'SAMIRA': {'SAMIRA'}, 'ARIA': {'ARIA'}, 'COMMANDER': {'COMMANDER'}}


def run(args, log):
    result = subprocess.run([str(a) for a in args], capture_output=True)
    (WORK / log).write_bytes(result.stdout + result.stderr)
    if result.returncode:
        raise RuntimeError(result.stderr.decode(errors='replace')[-2500:])
    return result.stdout


def label(speaker):
    name, role, accent = LABELS[speaker]
    im = Image.new('RGBA', (1280, 720))
    d = ImageDraw.Draw(im)
    width = max(420, int(d.textlength(name, font=art.font(27, True))) + 52)
    d.rounded_rectangle((44, 592, 44 + width, 681), radius=7, fill=(12, 22, 28, 218))
    d.rectangle((44, 603, 48, 669), fill=accent)
    d.text((65, 600), name, font=art.font(27, True), fill='#f5f1e8')
    d.text((66, 640), role, font=art.font(17, True), fill=accent)
    p = WORK / f'label_{speaker}.png'
    im.save(p)
    return p


def encode(item):
    index, (start, end, name, source_in, source_out, subject, crop) = item
    src = ORIGINAL if name == 'master' else SOURCE / 'clips' / f'{name}.mp4'
    first, last = round(source_in * FPS), round(source_out * FPS)
    frames = round((end - start) * FPS)
    factor = (frames - 1) / (last - first - 1)
    vf = (f'trim=start_frame={first}:end_frame={last},'
          f'setpts=(PTS-STARTPTS)*{factor:.12f},fps={FPS},' + (crop or '') +
          'scale=1280:720,setsar=1,'
          # At most two CFR rounding frames; no long hold or looping footage.
          'tpad=stop_mode=clone:stop_duration=0.083333')
    path = WORK / f'{index:02}.mp4'
    run(['ffmpeg', '-v', 'error', '-y', '-i', src, '-vf', vf, '-frames:v', frames,
         '-an', '-c:v', 'libx264', '-preset', 'fast', '-crf', 19, '-pix_fmt', 'yuv420p',
         '-threads', 2, path], f'segment_{index:02}.log')
    probe = json.loads(subprocess.check_output(['ffprobe', '-v', 'error', '-select_streams',
        'v:0', '-show_entries', 'stream=nb_frames', '-of', 'json', str(path)]))
    assert int(probe['streams'][0]['nb_frames']) == frames, (index, probe, frames)
    return dict(index=index, start=start, end=end, source=str(src), sourceIn=first / FPS,
                sourceOut=last / FPS, visibleSubject=subject, frames=frames, crop=crop)


def main():
    WORK.mkdir(parents=True, exist_ok=True)
    lines = json.loads((SOURCE / 'edit_manifest.json').read_text())['dialogue']
    assert EDL[0][0] == 0 and EDL[-1][1] == 100
    assert all(a[1] == b[0] for a, b in zip(EDL, EDL[1:]))
    # Rejected identity-drift footage: a different woman with an invented
    # shirt patch. A speaker label cannot make that render Dalia.
    assert all(not (s[2] == '07_aftermath' and s[3] < 11.5 and s[4] > 8)
               for s in EDL), 'Rejected aftermath character footage reused'
    audit = []
    for line in lines:
        overlap = [i for i, s in enumerate(EDL) if s[0] < line['end'] and s[1] > line['start']]
        assert all(EDL[i][5] in ALLOWED[line['speaker']] for i in overlap), line
        audit.append(dict(**line, shotIndices=overlap,
                          visibleSubjects=[EDL[i][5] for i in overlap]))
    with concurrent.futures.ThreadPoolExecutor(max_workers=2) as pool:
        shots = list(pool.map(encode, enumerate(EDL)))
    (WORK / 'concat.txt').write_text(''.join(f"file '{i:02}.mp4'\n" for i in range(len(EDL))))
    run(['ffmpeg', '-v', 'error', '-y', '-f', 'concat', '-safe', 0, '-i', WORK / 'concat.txt',
         '-c', 'copy', WORK / 'picture.mp4'], 'concat.log')
    args = ['ffmpeg', '-v', 'error', '-y', '-i', WORK / 'picture.mp4', '-i', ORIGINAL]
    graph = []
    previous = '0:v'
    for i, speaker in enumerate(LABELS, 2):
        args += ['-loop', 1, '-i', label(speaker)]
        spans = [x for x in lines if x['speaker'] == speaker]
        # Keep the final identifier inside the same fade, avoiding a separate
        # overlay disappearance just before the picture ends.
        enable = '+'.join(f"between(t,{x['start']:.6f},{100 if x['id']=='m01-debrief.line.2' else x['end']:.6f})" for x in spans)
        output = f'label{i}'
        graph.append(f"[{previous}][{i}:v]overlay=enable='{enable}':shortest=1[{output}]")
        previous = output
    graph.append(f'[{previous}]fade=t=out:st=98.75:d=1[v]')
    master = OUT / 'Episode01_FirstContact_SpeakerAligned_720p.mp4'
    args += ['-filter_complex', ';'.join(graph), '-map', '[v]', '-map', '1:a:0', '-t', 100,
             '-c:v', 'libx264', '-preset', 'fast', '-crf', 20, '-maxrate', '3M', '-bufsize', '6M',
             '-threads', 2, '-pix_fmt', 'yuv420p', '-c:a', 'copy', '-movflags', '+faststart', master]
    run(args, 'master.log')
    run(['ffmpeg', '-v', 'error', '-i', master, '-f', 'null', '-'], 'decode_check.log')
    audio_hashes = []
    for i, p in enumerate((ORIGINAL, master)):
        audio_hashes.append(run(['ffmpeg', '-v', 'error', '-i', p, '-map', '0:a:0', '-c', 'copy',
                                 '-f', 'hash', '-hash', 'sha256', '-'], f'audio_hash_{i}.log').decode().strip())
    assert audio_hashes[0] == audio_hashes[1], audio_hashes
    (OUT / 'speaker_alignment_manifest.json').write_text(json.dumps(dict(
        durationSeconds=100, frameRate=FPS, shots=shots, dialogue=audit,
        audioBitstreamIdentical=True, audioHash=audio_hashes[0], creditsUsed=0,
        master=str(master), sha256=hashlib.sha256(master.read_bytes()).hexdigest(),
        limitations=['Existing generated mouth animation has not been lip-synced.',
                     'Some short speaker shots are slowed or reused with different framing.',
                     'The native ElevenLabs project retains the previous seven-shot edit.']), indent=2) + '\n')
    print(master)


if __name__ == '__main__':
    main()
