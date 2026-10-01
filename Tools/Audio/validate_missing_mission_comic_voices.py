#!/usr/bin/env python3
"""Validate the approved 34 local comic recordings without claiming listening QA."""
from __future__ import annotations

from array import array
import hashlib
import json
import math
import sys
import wave

import generate_missing_mission_comic_voices as generator


def main():
    lines = generator.entries()
    expected = generator.payload_for(lines)
    if json.loads(generator.PAYLOAD.read_text()) != expected:
        raise RuntimeError("Canonical caption/request payload differs from approved payload")
    results = []
    for folder, filename, identity, speaker, english, persian in lines:
        manifest = json.loads((generator.ROOT / f"Assets/Game/Audio/Narrative/{folder}/{filename}").read_text())
        for language, locale, text in (("en", "en-US", english), ("fa", "fa-IR", persian)):
            path = generator.ROOT / f"Assets/Game/Audio/Narrative/{folder}/Voice/{language}/{identity}.wav"
            records = [c for c in manifest["clips"] if (c["id"], c["locale"]) == (identity, locale)]
            if len(records) != 1:
                raise RuntimeError(f"Expected unique manifest entry: {identity} {locale}")
            record = records[0]
            if not generator.persian_voice_profile.clip_matches(record, text, path, language):
                raise RuntimeError(f"Caption, payload file hash or delivery profile mismatch: {identity} {locale}")
            if record.get("voiceId") != generator.audio.VOICE_IDS[speaker]:
                raise RuntimeError(f"Established character voice mismatch: {identity} {locale}")
            if record.get("captionSha256") != hashlib.sha256(text.encode()).hexdigest():
                raise RuntimeError(f"Caption digest mismatch: {identity} {locale}")
            with wave.open(str(path), "rb") as wav:
                if (wav.getnchannels(), wav.getframerate(), wav.getsampwidth()) != (1, 44100, 2):
                    raise RuntimeError(f"Incorrect decoded waveform format: {path}")
                duration = wav.getnframes() / wav.getframerate()
                samples = array("h", wav.readframes(wav.getnframes()))
            if sys.byteorder != "little":
                samples.byteswap()
            peak = max(abs(s) for s in samples) / 32768
            rms = math.sqrt(sum(s * s for s in samples) / len(samples)) / 32768
            if not .25 < duration < 90 or rms < .0001 or peak >= .999:
                raise RuntimeError(f"Empty, clipped or invalid-duration waveform: {identity} {locale}")
            results.append({"id": identity, "locale": locale, "durationSeconds": round(duration, 6),
                            "peak": round(peak, 6), "rms": round(rms, 6), "bytes": path.stat().st_size,
                            "sha256": record["sha256"], "status": "Passed"})
    report = {"status": "Passed", "clips": len(results), "totalBytes": sum(c["bytes"] for c in results),
              "checks": ["current canonical text", "approved request payload", "established character voice",
                         "caption and audio SHA-256", "Persian delivery profile", "PCM mono 44.1 kHz 16-bit",
                         "non-silent signal", "sample clipping", "reasonable duration"],
              "audibleListeningAcceptance": "Pending", "unityBindingsAndPlayback": "Pending", "records": results}
    (generator.REPORT / "generated_waveform_qa.json").write_text(json.dumps(report, indent=2) + "\n")
    print(f"[MissingMissionComicWaveformQA] result=Passed clips={len(results)} locales=2 bytes={report['totalBytes']} listening=Pending UnityPlayback=Pending")


if __name__ == "__main__":
    main()
