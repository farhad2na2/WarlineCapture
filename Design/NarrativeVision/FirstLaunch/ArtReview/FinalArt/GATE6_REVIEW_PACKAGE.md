# First-Launch Gate 6 Final-Art Review Package

Date: 2026-07-11

Status: Approved 2026-07-11; exact-revision runtime export completed and verified

## Approval Record

The user approved the exact 22 revisions recorded in `FINAL_ART_REVIEW_LEDGER.md` on 2026-07-11 after reviewing the images, panel mockups, and animated sequence. Future changes require a new revision and another disposition for only the affected panel.

## Primary Review Artifacts

- `Evidence/FINAL_ART_CONTACT_16x9.png`: all 22 clean narrative panels in playback order.
- `Evidence/FINAL_ART_SAFEAREA_CONTACT_16x9.png`: subtitle reserve, centered story-safe area, and Skip reserve at 16:9.
- `Evidence/FINAL_ART_SAFEAREA_CONTACT_20x9.png`: the same review at the primary 20:9 phone target.
- `Evidence/FINAL_ART_STORYBOARD_COMPARISON.png`: locked storyboard beside the offered final revision for every panel.
- `Evidence/FINAL_ART_REFERENCE_SUMMARY.png`: shared style, character, ARIA, Old Market, and Relay authorities.
- `Evidence/FINAL_ART_MOTION_PROOF.mp4`: review-only 44-second pan/zoom proof covering `FL-P01` through `FL-P22` in order.
- `Evidence/FINAL_ART_VALIDATION.json`: machine-readable count, naming, dimensions, PNG integrity, preview pairing, and asset-boundary validation.
- `FINAL_ART_PROVENANCE.md`: exact revision lineage and SHA-256 records.
- `FINAL_ART_REVIEW_LEDGER.md`: panel-specific review focus and user disposition register.
- `../PresentationCandidates/RevisionB_UserFeedback/dialogue_candidates/DIALOGUE-B_GraphicNovel_APPROVED_REFERENCE.png`: approved dialogue appearance reference; runtime uses separate 9-sliced frame, icon/portrait, and TMP text.
- `../PresentationCandidates/RevisionB_UserFeedback/interactive_ui/`: separate Commander identity and guidance-choice UI references.

## Product Boundaries

- These are usable clean 2D cinematic panel masters, not flattened videos or UI screenshots.
- Dialogue frames, text, portraits, ARIA speaker icon, Skip, Commander identity, guidance selection, and selectable controls remain separate runtime Unity layers.
- `FL-P08` is intentionally a clean background plate. Portrait selection, name entry, Continue, and Skip are live game UI.
- `FL-P16 R2` is intentionally free of route markings. ARIA route/highlight presentation is a runtime effect, not baked art.
- The MP4 files are review proofs only. Retail playback uses clean approved panels with the future real-time narrative player.
- Exact approved 16:9 and 20:9 panel exports now live under `Assets/Game/Art/Narrative/FirstLaunch/Panels/`; source review files remain under `Design/`.

## Internal Acceptance

- Panel coverage: `22/22`.
- Current masters: `1672 x 941` PNG.
- Preview pairs: `22/22` at `1920 x 1080` and `2400 x 1080`.
- Ordered motion proof: `22/22`, `1280 x 720`, `30fps`, `44.0s`, no audio.
- Structural validation: pass.
- User dispositions: `22/22 Approved` on 2026-07-11.

## Post-Approval Result

Approval is recorded for all 22 exact revisions. `Assets/Game/Art/Narrative/FirstLaunch/approved_first_launch_art_manifest.json` binds each approved source hash to its 16:9 and 20:9 runtime texture. `Evidence/APPROVED_RUNTIME_EXPORT_VALIDATION.json` verifies all 44 textures and Unity sprite-import metadata. Any later art change creates a new revision and resets only that panel to `Pending`.
