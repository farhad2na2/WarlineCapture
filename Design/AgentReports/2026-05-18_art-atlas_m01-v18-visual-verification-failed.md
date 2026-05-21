# Art/Atlas M01 V18 Visual Verification Failed

Date: 2026-05-18
Owner: Art/Atlas
Status: needs fixes; V18 idle proof not approved
Priority: P0

## Summary

User review questioned whether V18 was visually verified on screen against the target. Follow-up target-aligned crop review confirms V18 is not visually approval-ready.

V18 must not be routed to Gameplay as accepted final art.

## Proof Reviewed

- Original V18 placement proof: `Design/AgentReports/Captures/M01_TargetMatchV18DirectionLockedIdle_AssetPlacementReview_1920x1080.png`
- Target comparison: `Design/AgentReports/Captures/M01_TargetMatchV18DirectionLockedIdle_vs_Target_Comparison.png`
- New target-aligned crop review: `Design/AgentReports/Captures/M01_TargetMatchV18DirectionLockedIdle_TargetAlignedCropReview.png`

## Visual Assessment

### Player Bottom Squad

Status: needs fixes.

- The proof label overlaps the player squad region, weakening the review image.
- The V18 player cells do not yet match the target silhouette read closely enough.
- Target soldiers have a clearer tactical up-screen posture with visible weapon/read direction; V18 reads more like a generic back-facing standing pose.
- Target spacing/scale should be matched directly in the proof rather than approximated.

### Enemy Top Squad

Status: needs fixes.

- The V18 enemy cell reads visually wrong in the crop.
- The current enemy direction-locked construction looks fused/doubled and too bulky.
- Scale and darkness diverge from the target enemy group.
- This cell should be replaced, not approved.

## Root Cause

V18 was mechanically validated for single-component clean cells, but the screen-space art read was not strong enough. The validation proved alpha/cell cleanliness, not visual target match.

## Required Fix

Art/Atlas needs a new V18 revision or V19 candidate with:

- no labels covering the unit proof region
- target-positioned proof crops at the same screen locations and comparable scale
- player bottom soldiers that clearly face up-screen with target-like silhouette/readability
- enemy top soldiers that clearly face down-screen without fused/doubled construction
- baked horizontal-right shadows still connected under the boots
- clean-cell validation after visual replacement

## Binding Guidance

- Do not bind V18 as final.
- Do not route V18 to final Gameplay proof.
- Keep V17 as technical binding proof only.
- The next Art pass must replace the V18 direction cells and show a target-aligned proof before claiming review readiness.
