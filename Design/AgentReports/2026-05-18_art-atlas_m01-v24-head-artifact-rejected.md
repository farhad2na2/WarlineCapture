# Art/Atlas M01 V24 Head Artifact Rejected

Date: 2026-05-18
Owner: Art/Atlas
Status: rejected; do not bind
Priority: P0

## Summary

User review rejects V24 because the artificial helmet/specular pass introduced an ugly visible artifact on soldier heads.

V24 must not be bound or routed as final.

## Assessment

The artifact came from a manual head glint added during the V24 specular pass. It reads as a pasted dot/patch rather than a natural highlight, especially in the target-aligned crop proof.

## Binding Guidance

- Do not bind V24.
- Do not route V24 to Gameplay final proof.
- Keep V22/V23 only as prior review candidates, not final approval.

## Required Fix

The next pass must remove all artificial head-glint drawing and keep only source-derived, natural small highlights. Stronger baked shadows may be retained if they still read correctly on the background.
