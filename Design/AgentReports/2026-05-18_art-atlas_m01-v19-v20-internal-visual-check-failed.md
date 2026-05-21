# Art/Atlas M01 V19/V20 Internal Visual Check Failed

Date: 2026-05-18
Owner: Art/Atlas
Status: internal candidates failed; do not bind
Priority: P0

## Summary

After user rejection of V18, Art/Atlas generated a fresh imagegen soldier source and tested V19/V20 direction-locked idle candidates against target-aligned crops.

The candidates are not approval-ready and must not be routed as finished.

## Reviewed Proofs

- V19 crop review: `Design/AgentReports/Captures/M01_TargetMatchV19FreshDirectionCandidate_TargetAlignedCropReview.png`
- V19 comparison: `Design/AgentReports/Captures/M01_TargetMatchV19FreshDirectionCandidate_vs_Target_Comparison.png`
- V20 crop review: `Design/AgentReports/Captures/M01_TargetMatchV20ScaledFreshDirectionCandidate_TargetAlignedCropReview.png`
- V20 comparison: `Design/AgentReports/Captures/M01_TargetMatchV20ScaledFreshDirectionCandidate_vs_Target_Comparison.png`

## Assessment

V19 improved source quality compared with V18 because the soldiers came from a fresh imagegen source rather than a fused cutout construction. However, V19 was far too large in the target crop.

V20 scaled the same source down, but it still does not match the target:

- soldiers remain too bulky compared with the target silhouettes
- target player soldiers are slimmer with a clearer rifle/read direction
- enemy soldiers still do not match target scale/readability closely enough
- visual style remains closer to full character render than small RTS runtime sprite

## Binding Guidance

- Do not bind V18, V19, or V20 as final.
- Do not route these candidates to Gameplay final proof.
- Keep V17 only as technical binding proof.

## Required Next Correction

The next generated source must be composed at the intended small RTS sprite scale from the start, not generated as large character renders and downscaled. It must be visually checked in target-aligned crops before any handoff claim.
