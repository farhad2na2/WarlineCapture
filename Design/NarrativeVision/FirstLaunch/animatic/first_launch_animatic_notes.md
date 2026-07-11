# First-Launch Animatic Notes

Date: 2026-07-10

Status: Gate 5 production draft

## Timing Contract

| Milestone | Time |
|---|---:|
| Logo complete | `2.5s` |
| Identity interaction begins | `36.0s` |
| Identity default route completes | `42.0s` |
| Guidance interaction begins | `46.5s` |
| Guidance default route completes | `51.5s` |
| Illustrated M01 handoff begins | `84.5s` |
| Gameplay handoff | `88.5s` |
| Review-only debrief begins | `90.5s` |
| Command-base reveal completes | `108.5s` |

The normal default route reaches gameplay `1.5s` before the 90-second limit. The revised pacing preserves distinct, natural Microsoft character voices instead of compressing dialogue into the earlier placeholder cadence. Identity and guidance are represented by deterministic review holds, not player input simulation. Runtime may continue immediately after a valid selection.

## Temporary Presentation

- Storyboard frames are review-only `640 x 360` inputs scaled to a `1280 x 720` animatic.
- Normal motion uses restrained centered 3 percent pushes or lateral drifts; no motion exposes image edges.
- Reduced motion uses the same state durations with static holds.
- Temporary subtitles are speaker-labeled and burned into the review videos only.
- All five speaking roles use distinct Microsoft neural voices generated through the project's existing Edge TTS asset pipeline. ARIA remains fixed to `en-US-AriaNeural`; Dalia uses `en-US-MichelleNeural`; Samira uses `en-US-AvaNeural`; the default Commander read uses `en-US-ChristopherNeural`; and radio traffic uses `en-US-EricNeural` with additional radio filtering in the mix.
- The generator rejects duplicate role/voice assignments and produces offline WAV assets, so the review animatic has no runtime cloud dependency. Release use remains subject to a separate current-service and licensing review.
- Temporary ambience and cues are synthesized locally from noise and tones. No third-party music or sound recording is used.
- Identity, guidance, gameplay handoff, and reviewer controls use clearly temporary overlays/cards. They are not runtime UI art.

## Skip And Review Contract

- From logo through `FL-P18`, Skip commits valid default/selected Commander identity and guidance, marks mandatory context viewed, and routes to `first_launch.m01_handoff`.
- From the review-only gameplay placeholder, Jump To Debrief routes to `seq.ch01.m01.debrief`.
- From `FL-P19` through `FL-P21`, Skip routes to `first_launch.command_base_reveal`.
- At `FL-P22`, Continue enters the command-base placeholder.
- Reviewer mode may pause, resume, restart, step previous/next, scrub, capture, toggle reduced motion, skip to gameplay, or jump to debrief without writing Campaign rewards or mission completion.

## Gate 5 Acceptance

- Default handoff at or before `90s`.
- Normal and reduced-motion videos share the same `108.5s` state schedule.
- Every subtitle remains inside the lower safe band and never enters source art.
- Temporary voice clips fit their state holds without truncation.
- Cue levels remain below temporary speech.
- Early, identity, mid-opening, and brief Skip cases resolve all mandatory defaults.
- Timeline JSON, SRT, voice TSV, videos, and timing report agree.
