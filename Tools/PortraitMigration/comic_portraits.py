#!/usr/bin/env python3
"""Track comic portrait generation and preserve the existing Unity asset identities."""
import argparse
import fcntl
import hashlib
import json
import shutil
import subprocess
from pathlib import Path
from PIL import Image

ROOT = Path(__file__).resolve().parents[2]
REPORT = ROOT / 'Design/AgentReports/ComicPortraitMigration'
MANIFEST = REPORT / 'manifest.json'

def digest(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()

def initialize():
    if MANIFEST.exists():
        return
    previous = json.loads((ROOT / 'Design/VisualLockLayered/PortraitPolygonMigration/runtime_mapping_report.json').read_text())
    entries = []
    for old in previous['mappings']:
        target = ROOT / old['target']
        meta = Path(str(target) + '.meta')
        entries.append({
            'target': old['target'], 'role': old['role'],
            'identity': old.get('entityKey', old.get('name')),
            'config': old.get('config'), 'field': old.get('field'),
            'beforeSha256': digest(target), 'metaSha256': digest(meta),
            'candidate': None, 'prompt': None, 'applied': False,
        })
    assert len(entries) == 238 and len({e['target'] for e in entries}) == 238
    REPORT.mkdir(parents=True, exist_ok=True)
    MANIFEST.write_text(json.dumps({'tool': 'built-in image_gen', 'entries': entries}, indent=2) + '\n')

def stage(target, generated, prompt):
    with MANIFEST.with_suffix('.lock').open('a') as lock:
        fcntl.flock(lock, fcntl.LOCK_EX)
        data = json.loads(MANIFEST.read_text())
        entry = next(e for e in data['entries'] if e['target'] == target)
        destination = REPORT / 'Candidates' / Path(target).name
        destination.parent.mkdir(parents=True, exist_ok=True)
        shutil.copyfile(generated, destination)
        entry.update(candidate=str(destination.relative_to(ROOT)), prompt=prompt, generatedSource=str(generated))
        temporary = MANIFEST.with_suffix('.tmp.json')
        temporary.write_text(json.dumps(data, indent=2) + '\n')
        temporary.replace(MANIFEST)

def apply():
    data = json.loads(MANIFEST.read_text())
    entries = data['entries']
    assert all(e['candidate'] for e in entries), 'Generation must cover all 238 assets before applying.'
    audit()
    for e in entries:
        target = ROOT / e['target']
        assert digest(Path(str(target) + '.meta')) == e['metaSha256'], e['target']
        if e['applied']:
            assert digest(target) == e['afterSha256'], e['target']
            continue
        assert digest(target) == e['beforeSha256'], 'Concurrent asset change: ' + e['target']
        source = ROOT / e['candidate']
        temporary = target.with_suffix('.comic.tmp.png')
        command = ['magick', str(source), '-resize', '512x512!', '-depth', '8', '-strip']
        command += ['-define', 'png:color-type=6'] if e['role'] == 'Primary' else ['-alpha', 'off', '-define', 'png:color-type=2']
        subprocess.run(command + [str(temporary)], check=True)
        temporary.replace(target)
        e.update(applied=True, afterSha256=digest(target))
        MANIFEST.write_text(json.dumps(data, indent=2) + '\n')
    print('[ComicPortraitAssets] result=Passed images=238 metaFilesUnchanged=238')

def audit():
    entries = json.loads(MANIFEST.read_text())['entries']
    generated = 0
    issues = []
    corner_noise = []
    for e in entries:
        assert digest(Path(str(ROOT / e['target']) + '.meta')) == e['metaSha256'], e['target']
        if not e['candidate']:
            continue
        with Image.open(ROOT / e['candidate']) as im:
            assert im.width == im.height, 'Non-square candidate: ' + e['target']
            if e['role'] == 'Primary':
                assert 'A' in im.getbands(), 'Missing alpha: ' + e['target']
                alpha = im.getchannel('A')
                assert alpha.getextrema() == (0, 255), 'Invalid alpha range: ' + e['target']
                corners = [alpha.getpixel(p) for p in [(0,0), (im.width-1,0), (0,im.height-1), (im.width-1,im.height-1)]]
                # Preserve generated alpha. A 1-3/255 alpha speck is quantization noise;
                # it is recorded separately and reviewed with proper alpha compositing.
                if max(corners) > 3:
                    issues.append('Opaque corners: ' + e['target'])
                elif max(corners) > 0:
                    corner_noise.append({'target': e['target'], 'cornerAlpha': corners})
                border = [(x, 0) for x in range(im.width)] + [(x, im.height-1) for x in range(im.width)]
                border += [(0, y) for y in range(im.height)] + [(im.width-1, y) for y in range(im.height)]
                opaque_border = sum(alpha.getpixel(p) >= 128 for p in border)
                if opaque_border:
                    issues.append(f'Cropped silhouette ({opaque_border} border pixels): ' + e['target'])
        generated += 1
    (REPORT / 'candidate-audit.json').write_text(json.dumps({'checked': generated, 'pending': 238-generated, 'issues': issues, 'nearTransparentCornerNoise': corner_noise}, indent=2) + '\n')
    if issues:
        raise ValueError('\n'.join(issues))
    print(f'[ComicPortraitCandidates] checked={generated} pending={238-generated} metaFilesUnchanged=238')

def sheets():
    entries = [e for e in json.loads(MANIFEST.read_text())['entries'] if e['candidate']]
    output = REPORT / 'ReviewSheets'
    output.mkdir(exist_ok=True)
    for start in range(0, len(entries), 12):
        font = Path('/System/Library/Fonts/Supplemental/Arial.ttf')
        if not font.exists():
            font = ROOT / 'Assets/Game/Art/UI/Fonts/NotoSansArabic/NotoSansArabic-Regular.ttf'
        command = ['magick', 'montage', '-background', '#26333a', '-fill', 'white', '-font', str(font), '-pointsize', '12']
        for e in entries[start:start+12]:
            identity = e['identity'].removeprefix('Chr_').removeprefix('Veh_').replace('_', ' ')
            command += ['-label', identity + '\n' + e['role'], str(ROOT / e['candidate'])]
        command += ['-geometry', '192x192+8+8', '-tile', '4x3', str(output / f'comic-{start//12+1:02d}.png')]
        subprocess.run(command, check=True)
    print(f'[ComicPortraitReviewSheets] images={len(entries)} sheets={(len(entries)+11)//12}')

def thumbnails():
    """Render QA contact sheets with correct alpha compositing at HUD sizes."""
    entries = [e for e in json.loads(MANIFEST.read_text())['entries'] if e['candidate'] and e['role'] == 'Primary']
    output = REPORT / 'ReviewSheets'
    output.mkdir(exist_ok=True)
    for size in [64, 128]:
        for name, background, foreground in [('dark', '#26333a', 'white'), ('light', '#ded5c5', '#182228')]:
            command = ['magick', 'montage', '-background', background, '-fill', foreground,
                       '-font', '/System/Library/Fonts/Supplemental/Arial.ttf', '-pointsize', '10']
            for index, e in enumerate(entries):
                command += ['(', '-label', str(index+1), str(ROOT / e['candidate']), '-resize', f'{size}x{size}!',
                            '-background', background, '-alpha', 'remove', ')']
            command += ['-geometry', f'{size}x{size}+6+6', '-tile', '10x', str(output / f'primary-{size}-{name}.png')]
            subprocess.run(command, check=True)
    (output / 'primary-index.json').write_text(json.dumps([{'number': i+1, 'identity': e['identity'], 'target': e['target']} for i, e in enumerate(entries)], indent=2) + '\n')
    print(f'[ComicPortraitThumbnailReview] images={len(entries)} sizes=64,128 backgrounds=dark,light')

if __name__ == '__main__':
    parser = argparse.ArgumentParser()
    parser.add_argument('action', choices=['init', 'stage', 'apply', 'audit', 'sheets', 'thumbnails', 'status'])
    parser.add_argument('--target')
    parser.add_argument('--generated')
    parser.add_argument('--prompt-file')
    parser.add_argument('--prompt')
    args = parser.parse_args()
    if args.action == 'init': initialize()
    elif args.action == 'stage': stage(args.target, args.generated, args.prompt or Path(args.prompt_file).read_text())
    elif args.action == 'apply': apply()
    elif args.action == 'audit': audit()
    elif args.action == 'sheets': sheets()
    elif args.action == 'thumbnails': thumbnails()
    else:
        entries = json.loads(MANIFEST.read_text())['entries']
        print(json.dumps({'total': len(entries), 'generated': sum(bool(e['candidate']) for e in entries), 'applied': sum(e['applied'] for e in entries)}))
