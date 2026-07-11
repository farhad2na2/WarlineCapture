# APH-405 Android Voice Pilot Evidence

## Result

`Passed` on the connected Xiaomi 24090RA29G development device. The bounded eight-clip ARIA Voice pilot completed cold and repeated playback with zero probe failures. Android primary-mixer underrun counters and delayed-write counters did not increase during the run.

## Build And Device

| Field | Value |
|---|---|
| Source baseline | `cdd1c96494ef43540a363a7a0fd3be9ce1de92be` plus the tracked APH-405 probe and deterministic manifest refresh |
| Artifact | ignored `Build/AndroidProfiler/WarlineCapture-Profiler.apk` |
| Artifact SHA-256 | `0df15f718420b1e39d48b412055370999967a0a8effa72dc9d52fd2e1e68ddde` |
| Device | Xiaomi 24090RA29G |
| Android / API | Android 16 / API 36 |
| Launch opt-in | `-aph405VoicePilot` |
| Probe scope | development builds and Editor only; release compilation excludes the probe |

## Measurements

| Clip | Cold start ms | Repeated start ms | Runtime memory after load |
|---|---:|---:|---:|
| `aria_message_confirm_destroy_01` | 20.278 | 27.009 | 25,296 B |
| `aria_message_not_enough_money_01` | 33.130 | 16.321 | 14,384 B |
| `aria_message_tactical_command_instruction_attack_01` | 17.435 | 33.724 | 17,904 B |
| `aria_message_tactical_command_instruction_move_01` | 33.396 | 16.568 | 16,432 B |
| `aria_message_tactical_command_reason_no_selection_01` | 16.361 | 16.862 | 26,288 B |
| `aria_message_tactical_feedback_scan_contacts_01` | 32.647 | 17.765 | 16,848 B |
| `aria_message_warning_air_attack_type_01` | 33.194 | 17.375 | 16,944 B |
| `aria_message_warning_ground_attack_type_01` | 33.208 | 16.264 | 23,088 B |
| **Minimum / average / maximum** | **16.361 / 27.456 / 33.396** | **16.264 / 20.236 / 33.724** | **14,384 / 19,648 / 26,288 B** |

All clips began in `Unloaded`, reached `Loaded` after first playback, and remained loaded for the repeated pass. Total post-load runtime memory was `157,184` bytes. The APH-407 inventory records `2,103,058` compressed bytes and a `3,734,052`-byte decoded estimate for the same pilot scope; measured on-demand runtime residency remains materially below the decoded estimate.

## Glitch Evidence

| Counter | Before | After | Delta |
|---|---:|---:|---:|
| Primary normal-mixer partial underruns | 0 | 0 | 0 |
| Primary normal-mixer empty underruns | 0 | 0 | 0 |
| Primary delayed writes | 0 | 0 | 0 |
| Existing device-side track underruns | 20 | 20 | 0 |

Other pre-existing AudioFlinger counters were unchanged. The probe emitted no failed result, fatal exception, or application-not-responding marker. This is objective device evidence; subjective speaker/headphone quality remains part of the later audible smoke gate.

## Reproduction And Raw Evidence

1. Build the Android profiler APK through `Game.Editor.BuildScript.BuildAndroidProfilerApk`.
2. Install with `adb install -r Build/AndroidProfiler/WarlineCapture-Profiler.apk`.
3. Capture `adb shell dumpsys media.audio_flinger` before launch.
4. Launch `com.warlinecapture.game/com.unity3d.player.UnityPlayerGameActivity` with Unity argument `-aph405VoicePilot`.
5. Require one passing discovery marker, eight passing clip markers, and one passing summary marker.
6. Capture AudioFlinger again and require no pilot-window underrun or delayed-write increase.

Local raw evidence is intentionally transient:

| Evidence | SHA-256 |
|---|---|
| `/private/tmp/aph405-device-log.txt` | `ada372e10f4eb0079c688d4f43bc5e112105185573cc4a4c87b2f95fafd44bd9` |
| `/private/tmp/aph405-audio-flinger-before.txt` | `9cc2b2ff92c9d11e35f158f940af022d1224a973f6eaf5539e2a4f19e6d8255a` |
| `/private/tmp/aph405-audio-flinger-after.txt` | `33113bbc7acdfa5fc61b2343aae9cd56096eddcfa944295230379300f505f85d` |

## Decision

Accept the bounded `CompressedInMemory`, `preloadAudioData=false`, `loadInBackground=true` Voice pilot. Expanding the policy is owned by the next tracker item and must retain the existing JSON-driven importer workflow and contract gates.
