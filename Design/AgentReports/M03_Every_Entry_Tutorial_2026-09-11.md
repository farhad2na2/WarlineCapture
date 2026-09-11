# M3 tutorial on every entry

## Problem and changes

M3 inherited the saved assistant guidance level and could suppress its tutorial on replay. Minimal guidance also suppressed the defense guidance projection. The HUD then displayed an empty ARIA card and a misleading one-step counter.

- M3 launch payloads, Campaign deployment, and runtime launch normalization now require Full guidance and replay tutorial delivery. First clear, replay, and retry follow the same policy, without changing the saved assistant preference for other modes.
- The defense guidance projection no longer suppresses M3 for Minimal/replay settings or silently removes contextual lessons. It publishes the next eligible instruction without the extra three-second gap.
- The opening tour explains, in English and Farsi, that ARIA's tutorial follows the camera return. Its text is contained separately from ARIA's access-state text so the delivered HUD still binds correctly. The progress counter stays hidden until a real lesson exists.
- M3 displays Materials, Oil (نفت), Fuel (بنزین), and Civilian Risk. Oil remains visible at zero. Mission credit accounting and rewards are unchanged.
- The opening command camera uses a closer 85-unit height and centers on the friendly units, while the tour still visits the post and approach and returns to its captured command pose. The camera-return control uses the Build popup's steel border and return icon.

## Editor validation

Validation uses an isolated project at `/private/tmp/warline-campaign-return-qa`, with the checked-in macOS GUI-licensing wrapper and Unity Hub left running. The user's campaign save is not used as the fixture. No Android validation or evidence images are included in this change.

The every-entry probe seeds a completed M3 save, sets assistance Off, opens the visible Campaign page, deploys, retries, exits through the Pause popup, verifies the Campaign return, then deploys again. It checks the actual visible ARIA title/body/counter, enabled actions, and advancement through the Do It and warning Jump buttons. It also checks the tour explanation, oil/fuel visibility, camera return, and the squad's viewport positions. Farsi and English are exercised.

The initial direct-launch probe was insufficient: it did not exercise the Campaign route and expected Replay after an interrupted attempt that correctly produced Retry. Those assumptions were corrected. Adding opening copy also exposed ARIA's direct-child text binding contract; the prefab regression now exercises the real binding helper rather than only applying a model to the view.

- Tutorial/HUD regression: 91/91 passed, exit 0. `/private/tmp/warline-m3-entry-regression.xml` and `.log`. Includes launch/guidance, presentation, camera fallback, resource header, mobile HUD, ARIA prefab, and all 17 source-growth architecture checks.
- Actual Campaign entry/retry/re-entry: passed, exit 0, with visible 1/12 and lesson advancement on all three entries. `/private/tmp/warline-m3-every-entry-verified.log`.
- Final squad-centered camera validation: passed, exit 0 on all three entries, including all eight command soldiers inside the unobstructed viewport. `/private/tmp/warline-m3-entry-camera-final.log`. English and Farsi screenshots were inspected outside the repository.
- Camera regression tests: 5/5 passed, exit 0. `/private/tmp/warline-m3-command-camera-tests.xml` and `.log`; covers M3 squad framing, missing-camera/paused-opening/finale fallbacks, and preservation of M2's authored opening.
- Main project refreshed successfully; Editor stopped, compilation complete, `scriptCompilationFailed=false`.

This is entry/tutorial/HUD regression coverage, not a new complete M3/M4 combat playthrough.
