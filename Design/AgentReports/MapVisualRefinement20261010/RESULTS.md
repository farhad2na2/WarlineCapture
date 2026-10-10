# Campaign visual refinement — implementation and evidence

All six approved visual refinements are applied to the 11 existing shared Campaign sources. Skirmish and Operations retain their existing source bindings. No new physical maps were created.

## Visual changes

- Repaired unsupported decorative furnishings, tanks and platforms. Actual mesh raycasts verify roof or ground support. Overlapping orphan platforms cannot validate each other as support. Where roofs have no clear space, industrial decorations can use a checked nearby service yard. Decorative attachments on functional buildings inherit the building's intact-state ownership so destruction does not leave them floating.
- Reduced the yellow wash and restrained bloom dirt, flare and vignette. A separate gameplay volume profile preserves the imported Demo profile. Daytime sun, ambient fill and white balance retain warmth with clearer surface separation; night and dawn timing remain intact.
- Varied parked vehicle direction, checked service-bay gaps and subtle existing-material palette variations. Functional vehicles and gameplay placement remain protected.
- Added restrained roof repair panels, seams, dust and base grime while preserving building atlas colors. Existing props supply supported rooftop detail.
- Added baked activity masks for equipment stains, tire wear, dusty road shoulders, entrances and rock/building contact areas. The masks fade at their bounds and use compressed mobile formats.
- Finished existing water sections with stone-colored bank lips, darker bank faces, continuous water joins and quieter water-only ripples. Cliff and rock bases receive local wear without changing collision or navigation geometry.

## Native counts

| Map | Supported props | Parking changes | Building renderers | Ground renderers | Water sections | Wear stamps |
|---|---:|---:|---:|---:|---:|---:|
| AshLinePort | 0 | 49 | 289 | 1722 | 2 | 2233 |
| CH04M04GroundedSignal | 0 | 2 | 66 | 426 | 0 | 515 |
| CH04M05ArmorBreak | 0 | 21 | 412 | 2545 | 0 | 2769 |
| CH05M01CitywideAlert | 54 | 0 | 39 | 1503 | 46 | 462 |
| CH05M02TrustUnderFire | 0 | 50 | 160 | 1680 | 1 | 1508 |
| CH05M03NetworkCollapse | 54 | 0 | 34 | 1502 | 46 | 437 |
| CH05M04LastCorridor | 54 | 0 | 34 | 1503 | 46 | 437 |
| CH05M05CommandNode | 83 | 0 | 26 | 1479 | 46 | 404 |
| CityEdgeAirfield | 22 | 1 | 522 | 1317 | 0 | 2967 |
| DenseCity | 0 | 0 | 1816 | 8443 | 210 | 10987 |
| RefineryDistrict | 0 | 19 | 370 | 977 | 0 | 2215 |

Totals: 267 prop repairs, 142 parking changes, 1578 vehicle material assignments, 3768 weathered building renderers, 23097 ground material assignments, 397 water sections and 24934 wear stamps.

The source scenes retain **267,478 renderers**. Their aggregate mesh triangle references decrease from 144,464,691 to 143,661,253 (803,438 fewer) through simpler shared water meshes. This is source-geometry accounting, not a draw-call or GPU timing measurement. Water uses 22 shared meshes. Material variation increases material diversity; real-device frame-time acceptance remains pending.

Eleven activity textures use linear data, mipmaps and ASTC 6x6 mobile import settings. Estimated compressed mask memory with mips is about 0.30–2.38 MiB per map, depending on dimensions. These estimates exclude driver overhead and are not device measurements.

## Protection and validation

The T7 branch was fast-forwarded to main `165e2e86a` before the final runtime and graphics verification, with no overlapping files. Each map receipt records source hashes, moves and native protected component/transform checks. Gameplay configuration, functional placement, collision/navigation data and scene renderer counts remain protected. The only ownership changes are journaled decorative attachments and removal of their redundant independent presentation identities.

See [the comparison gallery](gallery.html) for 52 matched before/after native views. Committed WebP files are format-compressed review copies without repainting, cropping or relighting; manifests retain the original native PNG hashes. Raw PNGs and scene backups remain local.

Focused checks, final native captures, cache packaging and runtime evidence are listed separately in `final-validation.json`. Failed attempts remain in `validation/`. A failed wrapper is not counted as passed even when its native operation later completed.

## Corrected failures and limits

Early authoring attempts rejected ambiguous supports, unsuitable roof candidates and asset import delays before acceptance. One broad support attempt moved 114 valid cloth-cover/pot details; those moves were restored through native Unity with exact identity/pose guards before the final repair pass. The first capture set missed waterfront views; later captures reuse the original audited camera. Subsequent visual reviews found the industrial platform support cycle and corrected it. A read-only native probe identified small courtyard terrain meshes incorrectly classified as obstacles; the service-yard fallback now recognizes them as support surfaces. Cold Pipeline acknowledgements and process-resource wrapper failures retain their failed receipts.

The URP resource test now checks the Editor's registered settings list and loaded compute resources because this URP version intentionally clears the serialized runtime settings list. The focused runner restores quality settings after each case. Mobile built-in MSAA is restored to its authored bandwidth policy; High/Ultra URP settings remain separate.

Native visual review and focused automation do **not** establish whole-mission player readiness. Complete normal-input playthroughs, Android GPU/frame pacing measurements and real player/device acceptance were not rerun for this visual pass and remain pending.
