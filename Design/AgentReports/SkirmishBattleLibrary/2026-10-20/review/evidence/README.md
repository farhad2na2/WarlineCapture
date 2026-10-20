# Final native evidence

Source checkpoint `ac03176`; candidate SHA-256 `ceeb2284efe03f150ffa4e58e8fb3d312e91ff4968fd7d49221e20c2b4c9a0c0`.

[Machine-readable acceptance record](final-validation.json) separates automated checks, actual native journeys and pending owner/device gates. Static capture progress is a labelled fixture. The two journey folders contain actual earned medals, result/return PNGs, scale measurements and fresh-process reload proofs. Their input trace archives retain the original artifact names and full normal-input records.

## Exact final logs

`validation-logs.zip` contains public copies of all named saga validation logs and NUnit XML, including failures and superseded runs. Use these final records:

| Log | Required result |
| --- | --- |
| `logs/skirmish-saga-tests-26.log` | `[SkirmishSagaTests] result=Passed passed=95 failed=0 skipped=0` |
| `logs/skirmish-saga-seat-check-14.log` | `TheatreSeatCheck result=Passed seated=113 blocked=0` |
| `logs/skirmish-saga-capture-15.log` | `[SkirmishSagaCapture] result=Passed files=18 matrix=16 selectionRegressions=2 visualReview=Pending deviceAcceptance=Pending` |
| `logs/skirmish-saga-normal-input-14-en.log` and `logs/skirmish-saga-normal-input-15-fa.log` | `[SagaNativeMedalJourney] result=Passed nativeResult=1 returnSelection=1 stopRibbon=1 diskReload=1 processRestart=Pending`; terminal probe passed with zero interventions |
| `logs/skirmish-saga-process-restart-05-en.log` and `logs/skirmish-saga-process-restart-06-fa.log` | `[SagaProcessRestart] result=Passed reason=coldProfileNativeMenuMedal profileSeeded=0 nativeTouch=1` |

The initial journey proof deliberately records process restart as pending; the separate matching-hash `.process-restart.json` completes that gate in a new PID. All final wrapper invocations exited zero. Earlier native victories belong to their own source hashes and are historical evidence.

[Log copy manifest](log-copy-manifest.json) records raw and public SHA-256 hashes and private host/licensing redaction counts. Full originals remain locally; none were deleted. [Historical evidence index](historical-evidence-index.json) hashes failed/interrupted/superseded artifacts, including locally retained PNGs that are not presented as current screenshots. Per-run logs remain authoritative when an old directory also contains stale `failure.txt`.

[Aircraft dependency audit](aircraft-dependency-audit.zip) contains the exact pre-refresh backups and the field/hash audit for the three dependency stamps. No physical Campaign map geometry or mission bindings were changed by this refresh.

Owner visual review, real player/device acceptance, and the Windows filesystem guard lane remain pending. The optional broader suite failure is recorded in [STATUS.md](../../STATUS.md); model simulations are separate from native outcomes.
