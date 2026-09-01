# POP-01 Threat Alert — Iteration 2

Status: review-frozen; pending explicit user acceptance.

## Visible corrections from Iteration 1

- Removed the obsolete `HOSTILE CELL SPOTTED` banner while the V3 popup is
  visible, so two alert systems can no longer stack.
- Reduced and re-spaced the strength meter so it remains inside its frame and
  clear of the action button in the alert state.
- Reflowed the compact route-preview summary and route strip so their borders
  do not overlap.
- Restored full `North Bridge` and `Est. Strength` labels without truncation.
- Preserved one 3 px border thickness and visible directional gradients across
  header, body, summary, strip, and action surfaces.
- Kept the responsive HUD contract: full-width bottom rail and top-right ARIA
  panel at 16:9 and 20:9.

## Implementation contract

- One prefab supplies both alert and route-preview states.
- Existing convoy/vehicle art is reused with aspect-preserving crop behavior.
- Warning, route, strength, and world markers reuse the shared V3 Match atlas.
- Closing either state restores the prior legacy-banner active state.

## Validation

- `[ThreatAlertV3Validation] result=Passed tests=3`
- `[ThreatWarningValidation] result=Passed`
- `[CanvasRouteCaptureValidation] result=Passed` at 1920x1080 for alert.
- `[CanvasRouteCaptureValidation] result=Passed` at 4800x2160 for alert.
- `[CanvasRouteCaptureValidation] result=Passed` at 1920x1080 for route preview.
- `[CanvasRouteCaptureValidation] result=Passed` at 4800x2160 for route preview.

