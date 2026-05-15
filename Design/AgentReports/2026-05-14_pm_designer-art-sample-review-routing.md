# PM Routing: Designer Rejected M01 Art Sample Until Fixes

Date: 2026-05-14
Topic: M01 AAA layered Art sample after Designer review

## Decision

Designer review is accepted. The Art/Atlas sample is not approved yet.

Designer report:
`Design/AgentReports/2026-05-14_designer_m01-art-sample-alignment-review.md`

## Reason

The sample quality is directionally strong, but Designer confirmed the user's zoom concern. M01-01 and M01-02 do not read as the same tactical camera and zoom even though both claim `camera.default_start`. The sample also needs no-selection, selected-but-no-command-mode, M01-only objective, Build availability, and enemy ring/health state fixes before approval.

## Current Owner

Art/Atlas

## Required Art/Atlas Fixes

- Rebuild M01-01 and M01-02 from one shared tactical camera plate and one shared zoom/framing lock.
- Keep player and enemy squad screen scale consistent between M01-01 and M01-02.
- Preserve the same camera center; selection must not resize or reframe the world.
- M01-01 must show no selection, no selected ring, no command mode, no move/attack/objective markers, neutral or disabled command controls, and M01-only objective text.
- M01-02 must show selected state but no active command mode or Move/Attack highlight.
- Build must not appear available in M01. Hide it or clearly disable it with `MissionDoesNotAllowBuild`.
- Clarify enemy red rings/health as permanent affiliation layers or stateful world markers in both layer manifests; hide them if they are markers.
- Update the sample contact sheet, `LayerPack/manifest.json`, both per-frame layer manifests, and the Art handoff report.

## Held Lanes

Gameplay and QA/HCI remain blocked.

## Next Gate

After Art/Atlas submits the corrected two-frame sample, Designer/PM/user review it again. If approved, Gameplay may implement only `M01-01_TacticalStart` exactly from the approved layered sample and LayerPack. The rest of the sequence remains blocked until that first implementation path is confirmed and PM/user approves continuing.
