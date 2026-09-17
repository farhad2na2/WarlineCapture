# M1–M5 readiness handoff

Updated 2026-09-17. Current status: **Editor gameplay/recovery accepted; Android candidate built and verified.** Phone and unfamiliar-player acceptance remain separate gates.

Final source milestone committed as `b9d17a700`. The subsequent-to-build M1 follow-up restores the full first-launch opening on campaign replay/retry in the saved language, removes the unused guidance-level setup, and loads the saved commander portrait before comic playback. Full EN/FA replay and 45 focused regression tests passed; see [the follow-up report](../../AgentReports/M01StoryReplay/README.md). The APK below predates those source changes and needs rebuilding before device acceptance.

## What changed

- M1: restored the usable group-selection action and removed stale M4 selection routing on re-entry. The approved 18 EN/FA recordings are bound to the preserved legacy comic assets; those assets remain outside the interactive M1 opening because they have no panel artwork.
- M2: placement/replay baselines, Materials Continue, automatic builder closure and training feedback agree with the actual construction mission. Recruitment no longer briefly restarts Build guidance before the finale. Results show the Barracks and trained squad.
- M3: placement preview/commit and the gate's road position are checked; defense teaches Move → Hold without a mandatory removed Stop action. Wave/countdown/combat status remains visible, and the shorter convoy timing addresses the reported long waits.
- M4: the specialist group action avoids helicopter/APC selection overlap; Board guidance frames its actual wheel sector. Clearance explains the wait, resets on leaving, resumes after return and leads to departure. Defeat/retry restores a clean rescue attempt.
- M5: the gate stays between the compound walls and is attackable. Accepted attacks no longer restart guidance; destroyed-target cues clear, and archive recovery identifies the actual area and remaining time. Deadline defeat/retry restores fresh objectives.
- Shared: larger ARIA text without automatic shrinking, scrolling above the action buttons, obstacle-aware animated guide arrows, correct disabled materials after mission transitions, current-language tactical voice routing, and gameplay voice shutdown at victory/defeat. Authored debrief comic audio remains allowed.

The Android build exposed an additional player compilation error: an Editor-only footprint lookup was called by code compiled for the player. The call now has the matching Editor guard; the Editor's bake calculation is unchanged. Subsequent Android script compilation passed. The build also caught duplicated render dependencies; Unity’s isolation repair assigned 78 implicit dependencies a single local owner while preserving all 122 existing addresses. The unchanged content-layout gate now passes; the validated Addressables configuration is included in the source project.

## Evidence

- Current normal-speed English and Farsi M1→M5 journeys reached victory using visible controls and screen-position world commands, with active comics played fully, recovery actions, correct reward settlement and disk reloads. No health, position, objective or outcome edits were used to manufacture completion.
- A separate successful 15-mission chain covers fresh EN/FA progression plus deliberate out-of-order replays. Its earlier scope, skipped comics and source revision are recorded rather than merged into the final audio evidence.
- **269 distinct focused tests pass in their latest runs.** They cover audio lifecycle, guidance/layout, placement transactions, transport validation, native storage cleanup, mission rules and settlement.
- M4 supplemental clearance interruption/re-entry, actual combat defeat, clean Retry and reward isolation passed. M5 actual deadline defeat and visible Retry passed. Accelerated failure simulations are mechanics checks, not pacing evidence.
- Rendered layouts reviewed at EN 1280×720 and FA 2400×1080, plus supplemental 1920×1080 results. Actual playing voice asset paths were checked against the chosen locale; local transcripts checked the reported tactical clips and all new M1 recordings.

See [verification](VERIFICATION.md), [findings](FINDINGS.md), [test summary](Evidence/Completion20260916/focused_summary.json), and [input hashes](Evidence/Completion20260916/source_comparison_before_build.json). Failed validation attempts and their corrections are retained in the evidence directory.

## Candidate artifact

- APK: `Build/ReadinessCandidate/WarlineCapture-M1-M5.apk` — 634,020,483 bytes (about 605 MiB).
- ARM64, minimum Android API 26, non-debuggable release configuration. APK signature and complete ZIP integrity verified.
- SHA-256: `6aa83f296e60f45cc4adb8ddd102c3b1f5fd2b9557bed7589d29ec77790b1173`.
- Built from base commit `b70b529b40c9cb8689402824b29190a2443d420c` plus the recorded dirty working-tree inputs. This is not represented as a clean committed release.
- [Artifact verification](Evidence/Completion20260916/apk_verification.json), [build report](Evidence/Completion20260916/architecture_performance_android_apk_build_report.md), [pipeline history](Evidence/Completion20260916/candidate_pipeline.json).

## Remaining release gates

1. Install the APK on representative phones. Check startup, real touch input, safe areas, frame pacing, memory across repeated missions and thermal behavior. No device or emulator was available here; occasional concurrent-Editor hitches are not a phone-performance pass.
2. Observe 3–5 unfamiliar players without coaching, especially M3's defense wait and M4's transport handoff. This automated/internal pass does not prove first-time comprehension or enjoyment.

No new mission chapter, reward overhaul or extra mode was added. The next roadmap decision follows those external checks; this handoff is not a universal “bug-free” guarantee.
