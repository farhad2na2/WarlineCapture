# First-Launch Gate 6 Final-Art Provenance

Date: 2026-07-11

Status: Gate 6 approved provenance register; all 22 exact revisions are user-approved and runtime-export verified

## Control Rules

- This register follows `Design/NarrativeVision/FirstLaunch/IMPLEMENTATION_TRACKER.md`, `Design/NarrativeVision/FirstLaunch/ArtReview/ART_PRODUCTION_MANIFEST.md`, and `Design/NarrativeVision/FirstLaunch/storyboard/first_launch_panel_manifest.json`.
- A file named `FinalCandidate` or `BackgroundCandidate` remains a review source until its exact revision is approved in `FINAL_ART_REVIEW_LEDGER.md`.
- The user approved all 22 offered revisions on 2026-07-11, authorizing exact-revision runtime export.
- Current revision is derived from the SourceMasters filename. Missing revisions are not reconstructed or assumed.
- Generation history is recorded only where repository evidence supports it. Missing prompts, generation job IDs, seeds, edit settings, and superseded rasters remain explicitly undiscovered.
- SHA-256 identifies the exact current candidate bytes. Any pixel edit, metadata rewrite, or re-encode requires a new revision and new hashes.

## Path Keys

| Key | Repository-relative path |
|---|---|
| `AR/` | `Design/NarrativeVision/FirstLaunch/ArtReview/` |
| `FA/` | `Design/NarrativeVision/FirstLaunch/ArtReview/FinalArt/` |
| `SB/` | `Design/NarrativeVision/FirstLaunch/storyboard/` |

## Lineage Confidence

| Value | Meaning |
|---|---|
| `Exact` | Current SourceMaster is byte-identical to a named predecessor. |
| `Documented` | References and intended history are documented, but no byte-identical predecessor or generation record is retained. |
| `Inferred` | Visual/file comparison supports the relationship, but the production record is incomplete. |
| `Planned` | No final-art SourceMaster exists; only storyboard and locked references are available. |

## Current Candidate Register

| Panel | Current artifact | Rev | State | Source/reference paths | Discoverable generation and edit history | Confidence |
|---|---|---:|---|---|---|---|
| `FL-P01` | `FA/SourceMasters/FL-P01_FinalCandidate_R1.png` | `R1` | Candidate | `AR/StyleCandidates/DirectionB_MatchAligned/FL-P01_StyleLock_A_Master_16x9.png`; `AR/WorldCandidates/DirectionB_MatchAligned/WORLD-OLDMARKET-02_LivingMorning_CandidateB.png`; `AR/PresentationCandidates/RevisionB_UserFeedback/dialogue_candidates/LOCATION-A_SahrinOldMarket.png`; `SB/frames/FL-P01.png` | Visual comparison ties the composition most closely to Style Lock A. R1 removes the prohibited clinic cross present in that anchor and contains none of the title/subtitle chrome in the presentation candidate. No exact predecessor, prompt, job ID, or edit settings were found. Batch evidence: `FA/Evidence/PHASE7_BATCH01_INPUT_CONTACT.png` and `PHASE7_BATCH01_FINAL_CONTACT.png`. | Inferred |
| `FL-P02` | `FA/SourceMasters/FL-P02_FinalCandidate_R1.png` | `R1` | Candidate | `AR/WorldCandidates/DirectionB_MatchAligned/WORLD-OLDMARKET-03_AttackBlackout_CandidateB.png`; `SB/frames/FL-P02.png` | SourceMaster is byte-identical to the locked world candidate; only the production filename/revision role changed. Included in Batch 01 input/final evidence. | Exact |
| `FL-P03` | `FA/SourceMasters/FL-P03_FinalCandidate_R1.png` | `R1` | Candidate | `AR/WorldCandidates/DirectionB_MatchAligned/WORLD-RELAY-01_DamagedCommandPost.png`; `SB/frames/FL-P03.png` | SourceMaster is byte-identical to the locked damaged-command-post candidate. Included in Batch 01 input/final evidence. | Exact |
| `FL-P04` | `FA/SourceMasters/FL-P04_FinalCandidate_R1.png` | `R1` | Candidate | `AR/PresentationCandidates/RevisionB_UserFeedback/scene_candidates/FL-P04_ContentCandidate_R1.png`; `AR/ContinuityCandidates/DirectionB_MatchAligned/CHAR-DALIA-01_CandidateA.png`; `AR/ContinuityCandidates/DirectionB_MatchAligned/CHAR-SAMIRA-01_CandidateA.png`; `AR/ContinuityCandidates/DirectionB_MatchAligned/CIVILIANS-01_RolesAndResponders_CandidateB.png`; `SB/frames/FL-P04.png` | SourceMaster is byte-identical to the dedicated presentation scene candidate. No later paintover or compositing record was found. Included in Batch 01 input/final evidence. | Exact |
| `FL-P05` | `FA/SourceMasters/FL-P05_FinalCandidate_R1.png` | `R1` | Candidate | `AR/WorldCandidates/DirectionB_MatchAligned/WORLD-RELAY-02_ARIABootTreatment.png`; `AR/ContinuityCandidates/DirectionB_MatchAligned/CHAR-ARIA-01_CandidateA.png`; `SB/frames/FL-P05.png` | SourceMaster is byte-identical to the locked ARIA boot treatment. Included in `FA/Evidence/PHASE7_BATCH02_INPUT_CONTACT.png` and `PHASE7_BATCH02_FINAL_CONTACT.png`. | Exact |
| `FL-P06` | `FA/SourceMasters/FL-P06_FinalCandidate_R1.png` | `R1` | Candidate | `AR/PresentationCandidates/RevisionB_UserFeedback/scene_candidates/FL-P06_ContentCandidate_R1.png`; `AR/ContinuityCandidates/DirectionB_MatchAligned/CHAR-COMMANDER-02_FacelessFraming.png`; `AR/WorldCandidates/DirectionB_MatchAligned/WORLD-RELAY-01_DamagedCommandPost.png`; `SB/frames/FL-P06.png` | SourceMaster is byte-identical to the faceless-Commander presentation scene candidate. Included in Batch 02 input/final evidence. | Exact |
| `FL-P07` | `FA/SourceMasters/FL-P07_FinalCandidate_R1.png` | `R1` | Candidate | `AR/PresentationCandidates/RevisionB_UserFeedback/scene_candidates/FL-P07_ContentCandidate_R1.png`; `AR/WorldCandidates/DirectionB_MatchAligned/WORLD-OLDMARKET-01_GeographyMaster_CandidateB.png`; `AR/WorldCandidates/DirectionB_MatchAligned/WORLD-RELAY-01_DamagedCommandPost.png`; `SB/frames/FL-P07.png` | SourceMaster is byte-identical to the route-resolution presentation scene candidate. Included in Batch 02 input/final evidence. | Exact |
| `FL-P08` | `FA/SourceMasters/FL-P08_BackgroundCandidate_R1.png` | `R1` | Background candidate | `AR/WorldCandidates/DirectionB_MatchAligned/WORLD-RELAY-01_DamagedCommandPost.png`; `AR/ContinuityCandidates/DirectionB_MatchAligned/CHAR-COMMANDER-01_PortraitChoices.png`; `AR/PresentationCandidates/RevisionB_UserFeedback/interactive_ui/UI-COMMANDER-IDENTITY_Reference.png`; `SB/frames/FL-P08.png` | SourceMaster is byte-identical to `FL-P03` and the damaged-command-post world candidate. This is deliberate background reuse; portrait choices, name, validation, and Continue controls remain live runtime UI. Included in Batch 02 input/final evidence. | Exact |
| `FL-P09` | `FA/SourceMasters/FL-P09_FinalCandidate_R1.png` | `R1` | Candidate | `AR/ContinuityCandidates/DirectionB_MatchAligned/CHAR-ARIA-01_CandidateA.png`; `AR/ContinuityCandidates/DirectionB_MatchAligned/CHAR-COMMANDER-01_PortraitChoices.png`; `AR/WorldCandidates/DirectionB_MatchAligned/WORLD-RELAY-02_ARIABootTreatment.png`; `SB/frames/FL-P09.png` | Distinct Phase 7 raster recorded in `FA/Evidence/PHASE7_BATCH03_FINAL_CONTACT.png` against `PHASE7_BATCH03_STORYBOARD_CONTACT.png`. No byte-identical predecessor, prompt, job ID, seed, or edit record was found. Selected/default portrait remains excluded for runtime composition. | Documented |
| `FL-P10` | `FA/SourceMasters/FL-P10_FinalCandidate_R1.png` | `R1` | Candidate | `AR/WorldCandidates/DirectionB_MatchAligned/WORLD-OLDMARKET-01_GeographyMaster_CandidateB.png`; `AR/WorldCandidates/DirectionB_MatchAligned/WORLD-RELAY-01_DamagedCommandPost.png`; `SB/frames/FL-P10.png` | Distinct Phase 7 raster recorded in Batch 03 storyboard/final evidence. No generation or edit parameters were retained. | Documented |
| `FL-P11` | `FA/SourceMasters/FL-P11_FinalCandidate_R1.png` | `R1` | Candidate | `AR/ContinuityCandidates/DirectionB_MatchAligned/CHAR-DALIA-01_CandidateA.png`; `AR/ContinuityCandidates/DirectionB_MatchAligned/FACTION-JRC-01_SilhouetteEquipment.png`; `SB/frames/FL-P11.png` | Distinct Phase 7 raster recorded in Batch 03 storyboard/final evidence. No generation or edit parameters were retained. | Documented |
| `FL-P12` | `FA/SourceMasters/FL-P12_FinalCandidate_R2.png` | `R2` | Candidate | `AR/ContinuityCandidates/DirectionB_MatchAligned/CHAR-SAMIRA-01_CandidateA.png`; `AR/ContinuityCandidates/DirectionB_MatchAligned/CIVILIANS-01_RolesAndResponders_CandidateB.png`; `SB/frames/FL-P12.png` | R2 is the only retained final-art candidate and is recorded in Batch 03 final evidence. R1, the reason for supersession, prompt, job ID, seed, and edit record were not found; no R1 disposition is inferred. | Documented |
| `FL-P13` | `FA/SourceMasters/FL-P13_FinalCandidate_R1.png` | `R1` | Candidate | `AR/ContinuityCandidates/DirectionB_MatchAligned/CHAR-ARIA-01_CandidateA.png`; `AR/WorldCandidates/DirectionB_MatchAligned/WORLD-FX-01_ReusableEffectsSheet.png`; `SB/frames/FL-P13.png` | Distinct Phase 7 raster recorded in Batch 03 storyboard/final evidence. No generation or edit parameters were retained. | Documented |
| `FL-P14` | `FA/SourceMasters/FL-P14_FinalCandidate_R1.png` | `R1` | Candidate | `AR/ContinuityCandidates/DirectionB_MatchAligned/CHAR-COMMANDER-02_FacelessFraming.png`; `AR/WorldCandidates/DirectionB_MatchAligned/WORLD-RELAY-01_DamagedCommandPost.png`; `SB/frames/FL-P14.png` | Distinct Phase 7 raster recorded in Batch 03 storyboard/final evidence. No generation or edit parameters were retained. | Documented |
| `FL-P15` | `FA/SourceMasters/FL-P15_FinalCandidate_R1.png` | `R1` | Candidate | `AR/ContinuityCandidates/DirectionB_MatchAligned/FACTION-ASH-01_FirstContactPatrol_CandidateB.png`; `AR/WorldCandidates/DirectionB_MatchAligned/WORLD-OLDMARKET-04_M01Handoff_CandidateB.png`; `SB/frames/FL-P15.png` | Distinct Phase 7 raster recorded in `FA/Evidence/PHASE7_BATCH04_FINAL_CONTACT.png` against `PHASE7_BATCH04_STORYBOARD_CONTACT.png`. No byte-identical predecessor, prompt, job ID, seed, or edit record was found. | Documented |
| `FL-P16` | `FA/SourceMasters/FL-P16_FinalCandidate_R2.png` | `R2` | Candidate | `AR/WorldCandidates/DirectionB_MatchAligned/WORLD-OLDMARKET-04_M01Handoff_CandidateB.png`; `AR/ContinuityCandidates/DirectionB_MatchAligned/CIVILIANS-01_RolesAndResponders_CandidateB.png`; `SB/frames/FL-P16.png`; `FA/Evidence/Rejected/FL-P16_FinalCandidate_R1_BAKED_ROUTE_REJECTED.png` | R1 baked cyan/green tactical route overlays into the raster and was moved, with both R1 previews, to `FA/Evidence/Rejected/`. R2 is the clean replacement with those overlays removed; runtime tactical highlights remain separate by contract. `FA/Evidence/FINAL_ART_MOTION_PROOF.mp4` visibly uses clean R2. No prompt, job ID, seed, or edit settings were retained. | Documented |
| `FL-P17` | `FA/SourceMasters/FL-P17_FinalCandidate_R1.png` | `R1` | Candidate | `AR/ContinuityCandidates/DirectionB_MatchAligned/CHAR-DALIA-02_ExpressionPoseSheet.png`; `AR/WorldCandidates/DirectionB_MatchAligned/WORLD-OLDMARKET-04_M01Handoff_CandidateB.png`; `SB/frames/FL-P17.png` | Distinct Phase 7 raster recorded in Batch 04 storyboard/final evidence. No generation or edit parameters were retained. | Documented |
| `FL-P18` | `FA/SourceMasters/FL-P18_FinalCandidate_R1.png` | `R1` | Candidate | `AR/WorldCandidates/DirectionB_MatchAligned/WORLD-OLDMARKET-04_M01Handoff_CandidateB.png`; `Design/M01_FirstContact_Production_Contract.md`; `SB/frames/FL-P18.png` | Distinct Phase 7 raster recorded in Batch 04 storyboard/final evidence. No production M01 camera exists; only a user-approved revision may become later 3D geography/camera authority. No generation or edit parameters were retained. | Documented |
| `FL-P19` | `FA/SourceMasters/FL-P19_FinalCandidate_R2.png` | `R2` | Candidate | `AR/WorldCandidates/DirectionB_MatchAligned/WORLD-OLDMARKET-05_DebriefCorridor_CandidateB.png`; `AR/ContinuityCandidates/DirectionB_MatchAligned/CIVILIANS-01_RolesAndResponders_CandidateB.png`; `SB/frames/FL-P19.png` | R2 is the only retained final-art candidate and is recorded in Batch 04 final evidence. R1, the reason for supersession, prompt, job ID, seed, and edit record were not found; no R1 disposition is inferred. | Documented |
| `FL-P20` | `FA/SourceMasters/FL-P20_FinalCandidate_R1.png` | `R1` | Candidate | `AR/WorldCandidates/DirectionB_MatchAligned/WORLD-PROPS-01_CivicAndCommandProps.png`; `AR/WorldCandidates/DirectionB_MatchAligned/WORLD-FX-01_ReusableEffectsSheet.png`; `SB/frames/FL-P20.png` | Distinct Phase 7 raster recorded in Batch 04 storyboard/final evidence. The visual clue remains abstract and text-free. No generation or edit parameters were retained. | Documented |
| `FL-P21` | `FA/SourceMasters/FL-P21_FinalCandidate_R1.png` | `R1` | Candidate | `AR/ContinuityCandidates/DirectionB_MatchAligned/CHAR-DALIA-01_CandidateA.png`; `AR/WorldCandidates/DirectionB_MatchAligned/WORLD-OLDMARKET-05_DebriefCorridor_CandidateB.png`; `SB/frames/FL-P21.png` | Distinct Phase 7 raster recorded in Batch 04 storyboard/final evidence. No generation or edit parameters were retained. | Documented |
| `FL-P22` | `FA/SourceMasters/FL-P22_FinalCandidate_R1.png` | `R1` | Candidate | `AR/WorldCandidates/DirectionB_MatchAligned/WORLD-RELAY-03_StabilizedCommandPost.png`; `AR/ContinuityCandidates/DirectionB_MatchAligned/CHAR-ARIA-01_CandidateA.png`; `SB/frames/FL-P22.png` | Distinct Phase 7 raster recorded in Batch 04 storyboard/final evidence. No generation or edit parameters were retained. | Documented |

## Artifact Verification

Verification performed 2026-07-11 with ImageMagick decode/identify and SHA-256 hashing.

| Check | Result |
|---|---|
| Current SourceMasters | `22/22` present and decodable. |
| SourceMaster format | All 22 are non-interlaced, 8-bit sRGB truecolor PNG, `1672x941`, RGB without alpha. |
| Revision-matched previews | `22/22` complete pairs: 16:9 `1920x1080` and 20:9 `2400x1080`, 8-bit sRGB truecolor PNG, RGB without alpha. |
| Preview content | Visually consistent resize/crop exports of the matching master revision; no independent authored content discovered. |
| Clean-art inspection | `22/22` current revisions internally pass: no visible subtitle, dialogue frame, title card, readable generated writing, logo, flag, real insignia, or interactive control. `FL-P16 R2` removes the rejected R1 tactical route overlays. Other abstract ARIA/map graphics are treated as diegetic story content pending user review. |
| Reuse check | `FL-P03` and `FL-P08` are intentionally byte-identical; P08 is a background candidate for live identity UI. |
| Layered/editable source | Not found for any panel. Current SourceMasters are flattened RGB composites/backgrounds. |
| Structural validation | `FA/Evidence/FINAL_ART_VALIDATION.json` reports `pass` for 22 masters, 44 previews, dimensions, aspect, naming, PNG integrity, absence of runtime asset leaks, and unexpected files. This does not test art quality, clean-art separation, or approval. |
| Static review contacts | Pass: ordered 16:9, 16:9 safe-area, and 20:9 safe-area contacts use clean `FL-P16 R2` with readable panel labels. `FINAL_ART_STORYBOARD_COMPARISON.png` contains explicit storyboard/final pairs for all 22 panels. The reference summary is current. |
| Motion proof | `FA/Evidence/FINAL_ART_MOTION_PROOF.mp4` decodes as 44.0-second H.264, 1280x720, 30 fps, 1320 frames. It contains 22 ordered, labeled panel segments and visibly uses clean `FL-P16 R2`; internal coverage check passes `22/22`. It is flattened review evidence, not proof of editable layer separation. |
| Approved runtime export | Pass: `APPROVED_RUNTIME_EXPORT_VALIDATION.json` verifies 22 approved revisions, 44 runtime textures, source/runtime SHA-256 values, Unity sprite metadata, and separate layered UI/dialogue composition. |

Technical decode and clean-art checks do not constitute art approval or Gate 6 acceptance.

## SourceMaster SHA-256

| Panel | Revision | SHA-256 |
|---|---:|---|
| `FL-P01` | `R1` | `b219dfbb9813718ab70655b97417e54f2fddf1e3dd7a885fe934abfe0d81abc2` |
| `FL-P02` | `R1` | `604d9b1c04cc49a41555ac6bd8cdd6435bb4a9cd5574d65bedc0b854cbf7aac4` |
| `FL-P03` | `R1` | `13ce503ac277a35f131619a6423b1c6128ee582e3c7d73d7b9bb923cc8312430` |
| `FL-P04` | `R1` | `e1a281729fc09135f7a2ed4d6af1555db7fe4045d3c393a0cc419a1c34120b5e` |
| `FL-P05` | `R1` | `62f6e9785a6d0fb411bcab538703003a1258ec303ad0d57ca8c9d09cbf170610` |
| `FL-P06` | `R1` | `ac6daea06e4a9f4ef732f208acb601d6c30f6df3d32c4ccc9e712b8a5f550d0c` |
| `FL-P07` | `R1` | `ab8f3785e4de8efb61fc8a3fb5010c691213d22aac423ad5c53143de5f33ad33` |
| `FL-P08` | `R1` | `13ce503ac277a35f131619a6423b1c6128ee582e3c7d73d7b9bb923cc8312430` |
| `FL-P09` | `R1` | `2fcd935d35cc7e1ffbf5d7904c3578953f326d9d0fe3348516da0f5b2a47d8d2` |
| `FL-P10` | `R1` | `23fd8bd4a36b1723052441fba9fce0099a542e2cd39e212bfdc642e6146f9850` |
| `FL-P11` | `R1` | `9d052023bdb5aea1dea5313ca9d4778fb549e287b0b80ad3e2cff011aa0a2de6` |
| `FL-P12` | `R2` | `c5defd665ceb333e924f5a8aff86c22769ad88eb293b9c02b88b3fdccd0d5752` |
| `FL-P13` | `R1` | `275ac561ba2fc79fa88a9b44dbbdc3f516460a4cec15a07106b1184e4c32953a` |
| `FL-P14` | `R1` | `c99190fb1e546bf6597a83e6b157d77f3b2bdc31080650db534d035facaba9c6` |
| `FL-P15` | `R1` | `ce780dda6b1162dcf0b013f0e4e1dc0e4b8cc6acae8b8511de3fbc1c9bbb5fb0` |
| `FL-P16` | `R2` | `b6fdba306fe16ccb5f8a6cae2baa8336d4103700a8108ee76c316fa7edc88ee7` |
| `FL-P17` | `R1` | `0d2169c328615af4c3da5f1e3955211fa6fa9617512489f6b53e74cc5fb16c59` |
| `FL-P18` | `R1` | `262a76b67e7fdfb626bcda2a2afcffd8d849f485cef4fa317b501cd46572e70f` |
| `FL-P19` | `R2` | `e69e305abf023c8a555678b3a3a3a541b48c13702afaa753e1a9a150a6149733` |
| `FL-P20` | `R1` | `1891231ce2fd0fbe4279476105998008848004e1b3eb6f5e57ab759ddabdab64` |
| `FL-P21` | `R1` | `d7a1de8ab8f9aab3c7d4e2a8b30d7c3125d4573eadc2f8377ba27aee6227aa49` |
| `FL-P22` | `R1` | `b7170f8ba3984b36fa14b7bbc563064fdad75a12953779d4502a57cf1e7c0695` |

## Preview SHA-256

| Panel | Revision | 16:9 preview SHA-256 | 20:9 preview SHA-256 |
|---|---:|---|---|
| `FL-P01` | `R1` | `5e39d25fb4fbef522877fd0d78a99f6bfb3f30e71ed31ef2a6959f9b0256fea3` | `7e563fc306403169cbc203343cb79486752f90324117aac4078b5e809234b42c` |
| `FL-P02` | `R1` | `d73009810448493b8495ebe4e2e260dbf3634886fb7d3d6bf00516dee87ef7a1` | `7864ef9c457bf5bab4af5fecbbd42b0464b651887b969f9d28f7e40db9221ad0` |
| `FL-P03` | `R1` | `6f26d038c2e3b61c5bb830fe17dac47ba8ddcfedd63d7101276ef0a20534d209` | `62818f3fdb672340db3a06811dc6c2429a93cb74d8019231f0b396fcf5504b44` |
| `FL-P04` | `R1` | `0b248d663d8e47eef78cea9dd866e86a6e20813811138b1ad974a62588411466` | `76a82c0ef87fdaf96fdcc35f119a88edeb5395848cc9b649416220d8de95bb24` |
| `FL-P05` | `R1` | `f4351ce41cac0bf4e0cbd2cbf5cc0096535ec520746b341cb26ac9e96997e8c5` | `aafea55f15dc2a5199e6c98e451ffa9a4bd8e023e5a332b2014aa3859d87f234` |
| `FL-P06` | `R1` | `02fae40c736c7b2790656fe926e281e88c69453d0ebd0c0e94fe62b6ec62c98b` | `f2b92607072dfdcb4a8bced199ed2a7415bcc0aacc47ac22a5cc9d423cd17b0e` |
| `FL-P07` | `R1` | `f2727595bc4dfe051c65343c9fb0ecce6aa351c48239c359e587ffa4eae0a039` | `d3c2bdc59c0f3b23b1da49733341a0e653f1d7a58b6aa886b18f547b7e5c7620` |
| `FL-P08` | `R1` | `36389ad4587a202aa18f72a35b3ed939ff114858bcca412058905eb3e19da340` | `f294afe5dc6ba015459c27307230263406a828649df6c39be7c92a9e5e68ea23` |
| `FL-P09` | `R1` | `de5d81e9b690bda20ff5dad7d310f4d501557334270af92bc8578b2f96bd5096` | `5d915f26a26805b43e627e67d0ece2621c3a1d20fcc9113c3672030967c83341` |
| `FL-P10` | `R1` | `3b89ad3a7d0a85e34e490f97b2ded76044dfcd8c904c10e80b0c0aed8f738476` | `3f8d22b3099fe559a2a5a522720d630a97fa3fa25d322c04b96c1af82a041c2b` |
| `FL-P11` | `R1` | `1de6eb2a3915f13ca52990d19aa1d060810a532df3b5e84e3c270957b6988b44` | `63e85e339652a68dd424e63df8891ddd13d11aa452e0b393c5eb63e807d36eb3` |
| `FL-P12` | `R2` | `eec9d7a72238336dbc25abc2d65b9b854a7244c3709ea162cc93c9263564950e` | `ac6822bc5880cc63897833b30adad402d04e83fe8e0211d3cd501e13d523f7c9` |
| `FL-P13` | `R1` | `90eacb977a21c033692ee46a57856394f530e7f1bf9429b9ca6ffc675df145d9` | `03e15587b3802ddb37e6c30ff03d53eaf2c3610d542eaa606382f1daf45f9632` |
| `FL-P14` | `R1` | `a131fcb07ba07d610d161db0871b172e8a176f920a122369e36b51f07b581f52` | `a53b454c6fe1fced8e6b3d2075b428e63ded17e449563079d8db3602492fdee6` |
| `FL-P15` | `R1` | `d4fb2393551e6dc3acbb9fdeb3a60eeb93831d9c9a331226f204581fc0514ace` | `fb22c7587ab8b0a8b2b74a5e11f78e0004294165e0b8bc9d7aad6aedcf5f166e` |
| `FL-P16` | `R2` | `94952047b892a7c46536dd5a07d1fca44853db5c17567992426503714e03f268` | `cbb087ba7b2b390c5e71dc2d42497529808bbb390bb398ce73b533e5db2074dc` |
| `FL-P17` | `R1` | `aeccb2c51028ea9f622db4619eb62c287cd6e0da4f23af9d5569e048c32a5d09` | `0cd2dd980f855ffdb803583bd8a0935fe9dedae9c42527a8806476ccf1a6b776` |
| `FL-P18` | `R1` | `d68d9a3341ab9493d68d491b1d51eb481bc2fc862c47b57c60affc9572216a54` | `078abb9f5b759a3c606a030e6d44187194c156681db749bd4dbf8bed6cc4d548` |
| `FL-P19` | `R2` | `c1c5d40e6c6e109ff5d4c872c08092f9e5e0b16633db0cd5baaa2b69f9f477ab` | `55eaf4350a0cede6c482f823dfd951c4729631cfda1cc78b7972b6c36c27936b` |
| `FL-P20` | `R1` | `c7029a1a7ea3db32514cb4929548b7e5f6a50d8859ced09926a78e09b9fe4507` | `77065827aca6be7793945df422391bab877e00e3c4e2c6a6bc7cb2fe93835827` |
| `FL-P21` | `R1` | `5880ea994897db8ab74be37605c426db38acc0d34f6a991ca19c4b753cc57c09` | `7c75ccc99d323db1131cdd8afd4217f56ed30d21d8d700a9b255d2960a84aa63` |
| `FL-P22` | `R1` | `6f1d8621afc8c39b1885e941c1aca0e8b3e79657ff101b382f28cf6aa32a8590` | `ffa0ef261f669fc88eebdbfbb9b01476d4c945cb4f72595e5473cc015680a141` |

## Review Evidence SHA-256

| Artifact | SHA-256 |
|---|---|
| `FA/Evidence/FINAL_ART_CONTACT_16x9.png` | `51e179946792c4e59cfa19f351f7d90ab65534f2f9c88a17f211bd6c3639ad59` |
| `FA/Evidence/FINAL_ART_REFERENCE_SUMMARY.png` | `3a391eb46c17467adfa1d5a4a5f798f499ab7e4e3f5cbb575cbc4d29bc1954b5` |
| `FA/Evidence/FINAL_ART_SAFEAREA_CONTACT_16x9.png` | `1d04cf9e3d3961d1a1fa8b00245a116413a3be4c031ce99c842ce18562b2fec3` |
| `FA/Evidence/FINAL_ART_SAFEAREA_CONTACT_20x9.png` | `f22ad6d279e6b4b0bf6f430cca259039c490e6ee4b258b43679ab503f3c7ae0c` |
| `FA/Evidence/FINAL_ART_STORYBOARD_COMPARISON.png` | `c51af2f64d0cb8f280c84865dd7a63d322858c40946bb09d6a895b59364c9d78` |
| `FA/Evidence/FINAL_ART_MOTION_PROOF.mp4` | `e8dcba4337150d3029fa9779821a33ba6c38e2a85c8256f84a2ece07743cf59a` |
| `FA/Evidence/FINAL_ART_VALIDATION.json` | `b6193fdbd5e5ec4c63ce922d47031a345e914e60d89c4c538260345d76db5b9d` |

## Rejected FL-P16 R1 SHA-256

These files preserve superseded internal-review evidence. Their `Rejected` folder role is not a user disposition.

| Artifact | SHA-256 |
|---|---|
| `FA/Evidence/Rejected/FL-P16_FinalCandidate_R1_BAKED_ROUTE_REJECTED.png` | `a621698481f588c639897d2ac83bb86e59acdb08c9f28446210d92b6699566d7` |
| `FA/Evidence/Rejected/FL-P16_16x9_R1_BAKED_ROUTE_REJECTED.png` | `61d10b473a273693abdbf657dfaf0c7f6ded871270327349c1535158eb22c0da` |
| `FA/Evidence/Rejected/FL-P16_20x9_R1_BAKED_ROUTE_REJECTED.png` | `f31b5d1fbcc36c23ec3ef0b88a1340b60593399bdbcce6635942b4eb87a3dc50` |

## Open Provenance Work

- Retain generation prompt, model/tool, job ID, seed when available, reference attachments, and selection rationale for every new revision.
- Record the missing `FL-P12` R1 history if it is recovered; do not recreate a retrospective reason without evidence.
- Record the missing `FL-P19` R1 history if it is recovered; do not infer a retrospective reason.
- Retain the rejected R1 trio as immutable correction evidence; do not return it to SourceMasters or Previews.
- Record future optional overlay sources separately. The current motion proof validates the approved flat-panel pan/zoom presentation; P08 interaction and P16 tactical routes remain separate runtime layers by contract.
