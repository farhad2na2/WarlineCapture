# Windows dev results — W15 structure defenders + W15b S002 harness boot (2026-11-18)

Branch `missions/win-w15-structure-defenders`, cut from `missions/integration` `7970b1c54` (includes W11/W12/W14). Owner approved this shared-code task in the Windows session.
Codex gpt-6.2-sol (W15 high, W15b xhigh); briefs, transcripts and reports in `D:\Projects\WarlineCapture-program-logs\2026-11-18\codex\w15-structure-defenders*` and `w15b-s002-harness-boot*`. Workers made no commits; the Windows dev lead reviewed and committed.

## W15 — `74604442a` (test only)
- Cause: stale expectation. Since `080fca838` ("bases see their attackers") a designated base has direct vision (`SkirmishBaseVisionPolicy.RevealRadius`, ~51 m). The fixture pad sat 35 m from the enemy base, so fog recorded the base (Entity 34) as first observer, not the defender (Entity 47).
- Fix (`SkirmishExpandedAriaTests.cs` ~456–520): defender starts 75 m out, so base sight alone drives the first selection (asserted observer = base); new step: pad at 60 m (outside both radii) is hidden and not selected, then selected once the defender itself acquires it (observer = defender). No assertion removed.

## W15b — `beed42648` (one runtime line + test diagnostic)
- With W15 passing, `RunFocusedValidation` reached `SkirmishS002AriaHarnessTests`: 8 tests failed at `BootPlayingSession` (`Expected Playing but was Failed`).
- Pre-existing: UTF on base `7970b1c54` = 9 failures (the W15 defender test + the same 8); on `74604442a` = the same 8 only.
- Cause: since `754e96e09` the post-spawn `SkirmishS005OpeningViewPolicy.TryQueueAfterActualSpawn` check needs a validated active map and the packaged definition; EditMode ledger worlds have neither, so the session went Failed/MissingReference after a successful logical spawn.
- Fix: `SkirmishScenarioSpawnSystem.cs:142` runs that check only when `Application.isPlaying`, like the map, delivery-basis and starting-building steps above it in the same method. Live startup is unchanged. `BootPlayingSession` now prints `FailureCode` in its Playing assertion. The opening-view policy's own tests call `TryQueueAfterActualSpawn` directly and still run.

## Unity (Windows, Editor 6000.5.8f1, `-GuiLicensing`, main checkout detached at `beed42648`)
- `InvokeUnityExecuteMethodValidation.ps1 -ExecuteMethod Game.Tests.Editor.SkirmishExpandedAriaTests.RunFocusedValidation`: exit 0, `[SkirmishS002AriaHarnessTests] result=Passed`, `[SkirmishExpandedAriaTests] result=Passed`, `error CS` 0 — log `win-dev-unity\w15b-tests\unity.log`.
- UTF EditMode `SkirmishExpandedAriaTests|SkirmishS002AriaHarnessTests|SkirmishExpandedDefinitionTests`: 196/196 passed — `win-dev-unity\w15b-utf\results.xml`.
- Earlier evidence: `w15-tests` (marker failed on the 8 harness tests), `w15-utf` (162: 154 pass / 8 fail), `w15-utf-base-7970b1c54` (162: 153 / 9).

## Not covered / open
- Burst BC1016 in `AriaExpandedPreparationPolicy.cs` (`TryStart(..., byte[] order, ...)`) still logs during these runs; it is W17, not W15.
- No Play-mode/menu launch was run for W15b; the changed line only skips a check outside Play mode.
