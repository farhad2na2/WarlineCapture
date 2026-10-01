# Comic and Pause input regression — 2026-10-01

## Changes

- Raised UI canvases now receive their own GraphicRaycaster. Moving comic and popup graphics onto an override-sorting canvas had removed them from the parent canvas raycaster.
- Comics use sorting order 32764. Pause and Settings use 32766, above comics and the ARIA hand, within Unity's signed 16-bit sort range.
- Existing dialogue reveal/continue logic and existing Skip to Victory action are retained.

## Evidence

- Live primary Editor compilation: completed, failed=false, errors=[]. Unity 6000.5.2f1 / Pipeline at port 7800.
- Automated live-view pointer check: PASS. A current caption was reset to one visible character for deterministic timing. EventSystem.RaycastAll hit InputSurface; pointerClick revealed the same complete line (AdvanceReady, maxVisibleCharacters=int.MaxValue). A second pointerClick advanced to a different line. See pointer-review.txt. This checks the raycaster, existing Button handler, narrative intents and presentation update; it bypasses OS/device input delivery.
- Pause raycast: SkipToVictoryButton was the first target, order 32766, interactable=true, with its own raycaster. Its bound handler was SkipToVictory. Dispatching pointer events through that target changed Engage/None to Result/Victory. The normal debrief presentation appeared and Pause closed.
- Native visual inspection: Persian Steel Push briefing, subsequent caption pages, Pause and victory debrief were inspected in the Editor Game view.
- Campaign progress was redirected to /private/tmp/warline-comic-input-review. Temporary probes and input settings were removed by restoring original settings and exiting the review Play session. Source edits are limited to two shared UI scripts.

## Failed / limited evidence

- Background native mouse automation repeatedly highlighted controls but produced no click-action or pointer-down callback. Application focus was false. Attempts using temporary background input settings and queued device events did not establish physical-input acceptance. Earlier screenshot page changes can also occur through automatic narrative advance and are not counted as a native click pass.
- The first pointer test ran after the debrief had finished and correctly failed because InputSurface was no longer the first raycast target. Repeating immediately on a fresh visible briefing passed.
- Read-only diagnostic eval attempts with incorrect namespaces failed compilation before execution; production compilation passed.
- No full normal-input mission playthrough or real device/player acceptance is claimed by this focused UI fix. Human mouse/touch acceptance remains pending: click while text reveals, click again to advance, then open Pause during a comic and use the existing Win / Skip to Victory control.
