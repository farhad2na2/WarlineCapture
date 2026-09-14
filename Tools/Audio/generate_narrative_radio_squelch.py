#!/usr/bin/env python3
"""Deterministic speech-free radio carrier. No recordings, TTS or external service."""
import hashlib
import json
import math
import random
import struct
import wave
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
OUT = ROOT / 'Assets/Game/Audio/Narrative/Shared/narrative_radio_squelch_01.wav'

def main():
    OUT.parent.mkdir(parents=True, exist_ok=True)
    rate, seconds, seed = 44100, 0.8, 14092026
    rng = random.Random(seed)
    low = high = 0.0
    samples = []
    for i in range(int(rate * seconds)):
        t = i / rate
        noise = rng.uniform(-1, 1)
        low += (1-math.exp(-2*math.pi*3400/rate)) * (noise-low)
        high += (1-math.exp(-2*math.pi*900/rate)) * (noise-high)
        envelope = min(1, t/.012) * min(1, (seconds-t)/.04)
        envelope *= .2 + .8*math.exp(-t*8)
        samples.append(round(32767*.4*(low-high)*envelope))
    with wave.open(str(OUT), 'wb') as file:
        file.setparams((1, 2, rate, len(samples), 'NONE', 'not compressed'))
        file.writeframes(struct.pack('<'+'h'*len(samples), *samples))
    OUT.with_suffix('.json').write_text(json.dumps({
        'asset':str(OUT.relative_to(ROOT)), 'source':'Procedural band-limited pseudorandom noise',
        'generator':'Tools/Audio/generate_narrative_radio_squelch.py', 'seed':seed,
        'durationSeconds':seconds, 'sampleRate':rate, 'channels':1,
        'containsRecordedSpeech':False, 'runtimeNetworkTts':False,
        'sha256':hashlib.sha256(OUT.read_bytes()).hexdigest()}, indent=2)+'\n')
    print('[NarrativeRadioSquelch] result=Passed speechSources=0 duration=0.8')

if __name__ == '__main__':
    main()
