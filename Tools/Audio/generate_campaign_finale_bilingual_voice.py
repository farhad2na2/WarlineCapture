#!/usr/bin/env python3
"""Generate and validate the canonical chapter-five finale cast, resumably."""
from __future__ import annotations

import argparse
from array import array
import datetime as dt
import hashlib
import json
import math
from pathlib import Path
import re
import sys
import wave

import generate_m02_bilingual_voice as audio
import generate_m04_bilingual_voice as decode
import persian_voice_profile

ROOT = Path(__file__).resolve().parents[2]
REPORT = ROOT / 'Design/AgentReports/CampaignFinaleVoices/20261014'
MISSIONS = (
    ('CH05M03NetworkCollapse', 'network_collapse', 7),
    ('CH05M04LastCorridor', 'last_corridor', 7),
    ('CH05M05CommandNode', 'command_node', 41),
)
audio.VOICE_IDS.update(
    LAILA='EXAVITQu4vr4xnSDxMaL', QASSEM='JBFqnCBsd6RMkjVDRZzb',
    KARIM='onwK4e9ZLuTAKqWW03F9', YUSUF='cjVigY5qzO86Huf0OWal',
    COMMANDER='N2yn1e9QeLHJNAg4zbMW', COMMANDER_FEMALE='CeoZEQcJ58ClpEHgRyKe',
    COMMANDER_NEUTRAL='RaldHBrhNdVemVVS3wAW')
audio.VOICE_NAMES.update(
    LAILA='Sarah (Captain Laila Nasser)', QASSEM='George (Nadir Qassem)',
    KARIM='Daniel (Warrant Officer Karim Daher)', YUSUF='Eric (Chief Yusuf Darzi)',
    COMMANDER='Warline - Commander', COMMANDER_FEMALE='Warline - Commander Female',
    COMMANDER_NEUTRAL='Warline - Commander Neutral')


def entries():
    result = []
    for folder, prefix, count in MISSIONS:
        source = ROOT / f'Assets/Game/Scripts/Configs/Localization/{folder}NarrativeCopy.cs'
        string = decode.STRING
        rows = re.findall(r'new\(' + string + r',\s*NarrativeSpeakerId\.(\w+),\s*'
                          + string + r',\s*' + string + r'\)', source.read_text())
        if len(rows) != count:
            raise RuntimeError(f'Expected {count} canonical lines in {source}, got {len(rows)}')
        for identity, speaker, english, persian in rows:
            speaker = speaker.upper()
            variants = [('', speaker)]
            if speaker == 'COMMANDER':
                variants += [('_female', 'COMMANDER_FEMALE'), ('_neutral', 'COMMANDER_NEUTRAL')]
            for suffix, voice_speaker in variants:
                for language, locale, text in [('en', 'en-US', english), ('fa', 'fa-IR', persian)]:
                    result.append(dict(folder=folder, prefix=prefix, id=prefix + '-' + decode.decode(identity),
                                       suffix=suffix, speaker=voice_speaker, language=language,
                                       locale=locale, text=decode.decode(text)))
    if len(result) != 126:
        raise RuntimeError(f'Expected 126 recordings including commander variants, got {len(result)}')
    return result


def manifest_path(entry):
    return ROOT / f"Assets/Game/Audio/Narrative/{entry['folder']}/{entry['prefix']}_voice_manifest.json"


def clip_path(entry):
    return manifest_path(entry).parent / 'Voice' / entry['language'] / (entry['id'] + entry['suffix'] + '.wav')


def matching_record(manifest, entry):
    return next((c for c in manifest.get('clips', [])
                 if (c['id'], c['locale'], c.get('variant', '')) ==
                 (entry['id'], entry['locale'], entry['suffix'])), {})


def prepare():
    clips = []
    for index, entry in enumerate(entries()):
        body = dict(text=entry['text'], model_id=audio.MODEL, language_code=entry['language'],
                    seed=16000 + index, apply_text_normalization='on')
        persian_voice_profile.apply(body, entry['language'])
        clips.append(dict(id=entry['id'], variant=entry['suffix'], speaker=entry['speaker'],
                          locale=entry['locale'], caption=entry['text'], request=body,
                          destination=audio.API_ROOT + '/v1/text-to-speech/' + audio.VOICE_IDS[entry['speaker']],
                          assetPath=clip_path(entry).relative_to(ROOT).as_posix()))
    payload = dict(provider='ElevenLabs', model=audio.MODEL, clipCount=len(clips),
                   canonicalLines=55, commanderVariantClips=16, runtimeNetworkTts=False,
                   captionCharacters=sum(len(c['caption']) for c in clips), clips=clips)
    REPORT.mkdir(parents=True, exist_ok=True)
    (REPORT / 'voice_payload.json').write_text(json.dumps(payload, ensure_ascii=False, indent=2) + '\n')
    print(f"[CampaignFinaleVoicePayload] result=Passed lines=55 clips=126 characters={payload['captionCharacters']} externalRequests=0", flush=True)
    return payload


def generate():
    payload = prepare()
    key = audio.read_api_key(ROOT / '.local/secrets/elevenlabs_api_key')
    sub = audio.request_json(key, '/v1/user/subscription')
    if sub.get('status') != 'active' or sub.get('tier') in {None, 'free'}:
        raise RuntimeError('An existing active paid ElevenLabs subscription is required')
    print('[CampaignFinaleSubscription] status=' + str(sub.get('status')) + ' tier=' + str(sub.get('tier')), flush=True)
    manifests = {}
    generated = 0
    for index, entry in enumerate(entries()):
        path = manifest_path(entry)
        if path not in manifests:
            manifests[path] = json.loads(path.read_text()) if path.exists() else {'clips': []}
        manifest = manifests[path]
        dest = clip_path(entry)
        old = matching_record(manifest, entry)
        reused = persian_voice_profile.clip_matches(old, entry['text'], dest, entry['language']) and old.get('voiceId') == audio.VOICE_IDS[entry['speaker']]
        if not reused:
            audio.convert(audio.request_audio(key, audio.VOICE_IDS[entry['speaker']], entry['text'],
                                               entry['language'], 16000 + index), dest, entry['speaker'])
            generated += 1
        record = audio.record('narrative', entry['id'], entry['speaker'], entry['locale'], entry['text'], dest)
        record.update(variant=entry['suffix'], captionSha256=hashlib.sha256(entry['text'].encode()).hexdigest())
        manifest['clips'] = [c for c in manifest['clips'] if (c['id'], c['locale'], c.get('variant', '')) !=
                             (entry['id'], entry['locale'], entry['suffix'])] + [record]
        manifest.update(schema='WarlineCapture.CampaignBilingualVoice.v1', provider='ElevenLabs',
                        model=audio.MODEL, license=audio.RIGHTS, runtimeNetworkTts=False,
                        missionId='saga.ch05.m' + entry['folder'][5:7] + '.' + entry['prefix'],
                        generatedAtUtc=dt.datetime.now(dt.timezone.utc).isoformat(),
                        subscription={k: sub[k] for k in ('tier', 'status')},
                        processing=dict(sampleRateHz=44100, channels=1, sourceEncoding='PCM_S16LE', loudnessLUFS=-18))
        path.parent.mkdir(parents=True, exist_ok=True)
        path.write_text(json.dumps(manifest, ensure_ascii=False, indent=2) + '\n')
        print(f"[CampaignFinaleVoice] {index + 1}/{payload['clipCount']} {dest.stem} {entry['locale']} duration={record['durationSeconds']:.2f}s reused={int(reused)}", flush=True)
    print(f'[CampaignFinaleVoiceGeneration] result=Passed clips=126 generated={generated} runtimeNetworkTts=0', flush=True)


def validate():
    records = []
    for entry in entries():
        path = clip_path(entry)
        manifest = json.loads(manifest_path(entry).read_text())
        matching = [c for c in manifest['clips'] if (c['id'], c['locale'], c.get('variant', '')) ==
                    (entry['id'], entry['locale'], entry['suffix'])]
        if len(matching) != 1:
            raise RuntimeError(f'Duplicate or missing manifest entry: {path}')
        record = matching[0]
        if not persian_voice_profile.clip_matches(record, entry['text'], path, entry['language']):
            raise RuntimeError(f'Caption, hash or Persian delivery mismatch: {path}')
        if record['voiceId'] != audio.VOICE_IDS[entry['speaker']] or record['captionSha256'] != hashlib.sha256(entry['text'].encode()).hexdigest():
            raise RuntimeError(f'Character voice or caption digest mismatch: {path}')
        with wave.open(str(path), 'rb') as wav:
            if (wav.getnchannels(), wav.getframerate(), wav.getsampwidth()) != (1, 44100, 2):
                raise RuntimeError(f'Incorrect waveform format: {path}')
            duration = wav.getnframes() / wav.getframerate()
            samples = array('h', wav.readframes(wav.getnframes()))
        if sys.byteorder != 'little':
            samples.byteswap()
        peak = max(abs(s) for s in samples) / 32768
        rms = math.sqrt(sum(s * s for s in samples) / len(samples)) / 32768
        if not .25 < duration < 120 or rms < .0001 or peak >= .999:
            raise RuntimeError(f'Silent, clipped or invalid duration: {path}')
        records.append(dict(assetPath=record['assetPath'], sha256=record['sha256'],
                            durationSeconds=duration, peak=peak, rms=rms, status='Passed'))
    REPORT.mkdir(parents=True, exist_ok=True)
    (REPORT / 'waveform_qa.json').write_text(json.dumps(dict(status='Passed', clips=len(records),
        audibleListeningAcceptance='Pending', normalInputMissionPlaythrough='Pending', records=records), indent=2) + '\n')
    print('[CampaignFinaleWaveformQA] result=Passed clips=126 locales=2 canonicalCaptions=Exact cast=Established listening=Pending')


if __name__ == '__main__':
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--generate', action='store_true')
    parser.add_argument('--validate', action='store_true')
    args = parser.parse_args()
    if args.generate:
        generate()
    elif not args.validate:
        prepare()
    if args.validate:
        validate()
