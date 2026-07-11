# First-Launch Animatic Validation

Date: 2026-07-10

Status: Revised technical and presentation checks passed; Gate 5 locked

## Revised V2 Outputs

- `animatic/revision_v2/first_launch_opening_reference_v2.mp4`: normal motion, `1280 x 720`, `30fps`, H.264 video, stereo `48kHz` AAC audio, exactly `176.5s`.
- `animatic/revision_v2/first_launch_opening_reference_v2_reduced_motion.mp4`: static-hold fallback with identical state order, audio, and exact duration.
- `animatic/revision_v2/first_launch_opening_timing_report.tsv`: 18 contiguous linear story states; Commander identity and guidance are intentionally excluded as live UI states.
- `animatic/revision_v2/audio/first_launch_opening_voice_generation.tsv`: 17 deadline-validated Microsoft neural timing reads across five distinct voices.
- `animatic/revision_v2/evidence/first_launch_opening_v2_contact.png`: checkpoints for location, attack, Dalia, Samira, ARIA, armed-threat identification, and gameplay handoff.

## Revised V2 Validation

| Check | Result |
|---|---|
| Duration and media | Pass: both videos are exactly `176.5s`, `1280 x 720`, `30fps`, with `48kHz` audio. |
| Narrative continuity | Pass: all 18 linear states are contiguous from `0.0-176.5s`. |
| Opening ambience | Pass: `2.5-17.5s` measures `-31.5dB` mean and `-15.1dB` peak; the opening is no longer effectively silent. |
| Presentation | Pass: approved off-white graphic-novel frame, correct Dalia/Samira portraits, and production ARIA icon are represented as separate review layers. |
| Interactive UI | Pass: Commander identity and guidance-choice screens are absent from the video and remain separate live Unity UI contracts. |
| Clarity | Pass: the contact review establishes Sahrin/Old Market, system attacks, Dalia, Samira, ARIA, JRC, civilians, Ash Line, and the bounded first command before handoff. |
| Product form | Pass: both MP4 files are explicitly reference-only; retail playback remains layered and real-time. |

## Accepted Outputs

- `animatic/first_launch_animatic.mp4`: normal motion, `1280 x 720`, `30fps`, `108.5s`, H.264 video and stereo `48kHz` AAC audio.
- `animatic/first_launch_animatic_reduced_motion.mp4`: static-hold fallback with the same resolution, frame rate, audio, state order, and exact `108.5s` duration.
- `animatic/first_launch_timing_report.tsv`: 25 contiguous states from `0.0s` through `108.5s`.
- `animatic/audio/first_launch_temp_voice_generation.tsv`: 23 deadline-validated dialogue clips.
- `evidence/visual/animatic/ANIMATIC_REVIEW_CONTACT.png`: accepted checkpoints for opening, identity, guidance, illustrated handoff, evidence, and command-base reveal.

## Timing And Routing

| Check | Result |
|---|---|
| Gameplay handoff | Pass: `88.5s`, within the `<= 90s` Gate 5 rule. |
| Full review sequence | Pass: normal and reduced-motion videos both end at exactly `108.5s`. |
| State continuity | Pass: all 25 states are gap-free and non-overlapping. |
| Identity hold | Pass: `36.0-42.0s`; deterministic default remains defined. |
| Guidance hold | Pass: `46.5-51.5s`; deterministic default remains defined. |
| Early/middle/identity/brief Skip | Pass: every state through `FL-P18` routes to `first_launch.m01_handoff`. |
| Gameplay review jump | Pass: the gameplay placeholder routes to `seq.ch01.m01.debrief`. |
| Debrief Skip | Pass: `FL-P19` through `FL-P22` route to `first_launch.command_base_reveal`. |
| Reviewer plan | Pass: pause, resume, restart, previous/next, scrub, reduced motion, capture, skip-to-game, and jump-to-debrief are explicitly bounded in `first_launch_animatic_notes.md`. Runtime behavior remains correctly deferred until Gate 9. |

## Voice And Audio

All dialogue is generated as offline WAV review assets through the existing Microsoft Edge neural pipeline. Runtime playback does not call a cloud TTS service.

| Role | Microsoft voice | Character distinction |
|---|---|---|
| ARIA | `en-US-AriaNeural` | Confident tactical system voice. |
| Dalia | `en-US-MichelleNeural` | Firmer field-command register. |
| Samira | `en-US-AvaNeural` | Warmer civilian register. |
| Commander | `en-US-ChristopherNeural` | Grounded authority register. |
| Radio | `en-US-EricNeural` | Separate male traffic voice with radio filtering in the final mix. |

The generator enforces five speakers, five stable speaker/voice mappings, and five unique voice IDs. All 23 clips end before their declared state deadlines. Representative one-second voice-mix windows measured `-26.2dB` to `-21.5dB` mean volume and `-11.8dB` to `-6.6dB` peak volume, confirming audible speech with headroom. Release distribution remains subject to a current Microsoft service/licensing review.

## Visual And Accessibility Review

- Pass: 25 subtitle cues are monotonic, positive-duration, speaker-labeled, and end by `108.1s`.
- Pass: the six-checkpoint contact sheet shows the subtitle band below story-critical faces, routes, and controls.
- Pass: restrained normal motion is present. P01 frame-difference RMSE between `3.0s` and `6.5s` is `0.0813352`.
- Pass: reduced motion remains effectively static. The same P01 comparison is `0.000553951`, attributable to video compression noise.
- Pass: normal and reduced-motion playback use identical timing and audio.
- Pass: temporary identity/guidance/gameplay overlays remain clearly review-only and are not mistaken for final runtime UI.

## Prior V1 Findings

The sections below preserve the first-pass technical evidence and the user findings that triggered revision V2.

## Gate Decision

The V1 artifact passed technical checks but failed presentation review. Revision V2 resolves the recorded findings with audible ambience, approved comic dialogue, production ARIA icon, correct portraits, no baked interactive menus, and slower clarity-first introductions. Gate 5 is internally locked and Phase 7 final-art production may proceed. The MP4 remains reference evidence only and is not a shipping cutscene.
