# PM Update: PREFAB-04 Style Alignment Gate

Date: 2026-05-07

## Decision

Tightened the `PREFAB-04_AssistantButton` UI task and the global visual target quality gate.

## Reason

AAA visual quality is not enough. A target can look expensive and still fail if it does not align with the existing WarlineCapture visual-lock family.

## Required For PREFAB-04

The UI agent must submit a side-by-side/contact-sheet comparison showing the new `PREFAB-04` target against accepted WarlineCapture targets, at minimum:

- `SCN-08_RTSBattleHUD`
- `PREFAB-05_AssistantPanel`

Additional useful references:

- `POP-10_AssistantTakeover`
- `SCN-03_CommanderProfile`
- `POP-11_CommanderIdentity`

## Acceptance Rule

If the new target looks like high-quality standalone sci-fi art but cannot sit inside `SCN-08_RTSBattleHUD` and the assistant visual family without looking foreign, it is not accepted.
