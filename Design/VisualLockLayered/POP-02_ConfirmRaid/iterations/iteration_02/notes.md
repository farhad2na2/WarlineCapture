# POP-02 Confirm Raid — Iteration 2

Status: review-frozen; pending explicit user acceptance.

## Correction from Iteration 1

The first implementation used body cards taller than the 496 px modal body,
causing the footer to cut through the target, composition, and warning panels.
That invalid frame was not frozen. Iteration 2 reduces and reflows those panels
to the target geometry so every card ends above the footer divider.

## Implementation contract

- Centered 1008x688 modal inside the 1672x941 reference composition.
- Existing `SCN05_SahrinMissionMap_V3.png` reused with aspect-fill cropping.
- Shared V3/Operations icons; no screen-local icon copies.
- Procedural directional gradients and one 3 px border width.
- Cancel closes without confirmation; Confirm emits the confirmation event and
  closes the popup.

## Validation

- `[ConfirmRaidV3Validation] result=Passed tests=3`
- `[CanvasRouteCaptureValidation] result=Passed` at 1920x1080.
- `[CanvasRouteCaptureValidation] result=Passed` at 4800x2160.

