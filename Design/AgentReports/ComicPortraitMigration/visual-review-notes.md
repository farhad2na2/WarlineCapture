# Comic portrait visual review

## Current status

All 238 runtime images have been replaced with the current campaign comic language: 74 Primary cutouts, 74 Card scenes, 74 Action scenes and 16 squad/selection images. The original asset paths and all 238 `.meta` files are unchanged. The application audit confirms 512×512 output, RGBA cutouts, RGB scenes, unique image content and complete prompt/source provenance.

Visual review covers all 20 contact sheets, all 74 Primary cutouts at 64 px and 128 px on dark and light backgrounds, and lower-right crops of all 164 opaque images. These are agent visual checks; user visual review and real player/device acceptance have not occurred.

Native Unity import/binding validation passed for 74 configs, 222 entity-role references and 16 group sprites. All three required markers in `native-hud-01.log` are present and the wrapper exit receipt is 0. Four post-update native HUD captures (English/Persian × rifle/transport) were visually reviewed: selection portraits, command-wheel art and squad tray use the new comic artwork. Existing top-panel cropping and bottom tray/action overlap remain visible, as in the baseline; this art-only change does not resolve those layout issues. Normal-input mission playthroughs, including ARIA, result and return, have not been performed for this artwork change. Static prefab captures do not establish mission readiness.

The generation and failed-validation history below is retained as evidence; intermediate pending statements describe their recorded stage.

The user requested replacement of the old POLYGON portraits with the current campaign comic language. This request authorizes artwork replacement while preserving runtime asset references.

Style references: `Assets/Game/Resources/FutureMissionComics/CH04M02_SteelPush.png` and the generated rifleman card in this report.

Initial visual checks: generic squad, rifle squad, vehicles, aircraft, transports, buildings, mixed force, and mixed soldier/vehicle retain recognizable subjects and illustrated style.

Corrected: mixed soldier/aircraft candidate introduced a national flag on the left soldier's shoulder. A targeted ImageGen edit replaced it with a blank charcoal patch; the revised image was inspected.

Initial transparency concern: raw previews exaggerated red/yellow pixels around Primary silhouettes. The later diagnosis below supersedes this concern; separate alpha cleanup is unnecessary.

Historical status during generation: candidates remained staged outside Assets until complete coverage and review. See the current status above for application and validation results.

## Additional evidence during generation

- Reviewed building candidates through shop and contractor tent at 192 px on dark HUD backing: subjects and day/night role distinctions remain readable.
- Transparency audit found nonzero corner pixels in both guard tower primary cutouts. All primary cutouts also need a colored-fringe review before application.
- A targeted built-in ImageGen background repair of the heavy guard tower retained visible red/yellow edge contamination and was rejected. Original attempt: `exec-a9f98147-3eb8-43da-9680-a1571354b745.png` in the built-in generated image directory.
- `before-hud-01.log` failed on an unrefreshed method; `before-hud-02.log` failed on a null feedback-label binding in the existing capture fixture. The portrait helper was revised to render native selection data directly, with scene and locale restoration in a finally block.
- Revised helper compilation completed with no compiler errors. `before-hud-03.log` failed because another validation was already running in the shared Editor. Its mission probe was left undisturbed. Native preview evidence remains pending.
- At this intermediate point, no runtime portrait bytes had been replaced.

### Transparency diagnosis correction

The earlier fringe concern was overstated by the image preview. Pixel inspection of the staged building primaries found bright pure-red fringe pixels with alpha 1–3/255; tower and fence corners contain only alpha 1/255 noise. The 512 px guard tower composited correctly over `#26333a` renders cleanly (`ReviewSheets/tower-alpha-composited.png`). Generated alpha is preserved without semantic cleanup. The audit now records near-transparent corner noise separately and rejects corners above 3/255. This is an explicit tolerance for quantization noise, not a claim of zero-alpha corners. All remaining primary portraits still require this pixel and composited review.

### Contractor tent framing repair

The first contractor tent Primary had 13 opaque border pixels and cropped rear anchors. The rejected candidate is preserved in `Rejected/contractor-tent-cropped.png`. A targeted built-in ImageGen framing edit restored the complete assembly and a transparent margin. The revised image was inspected and has zero opaque border pixels. The 107 staged candidates pass the current square, transparency, silhouette border and metadata audit.

### Native baseline and thumbnail review

`before-hud-04.log` passed with exit receipt 0 and both required pass markers. Four captures use the actual native HUD prefab and existing portrait bindings (English/Persian, rifle/transport), with scene and locale restoration. These are static prefab previews, not normal-input mission playthroughs. The baseline has the existing panel and tray clipping/overlap; this artwork replacement does not change their layouts.

The first 30 Primary cutouts were reviewed at 64 px on dark backing and 128 px on light backing. Their complete silhouettes, equipment and building structures remain readable. Full-set review remains pending while generation continues.

### Character review during generation

Reviewed contractor, ghillie, insurgent, officer, pilot, grenadier, sniper and scout variants in generated outputs and contact sheets. Original clothing, equipment, pose/activity and day/night context remain distinct. The 163 staged candidates pass the audit; all staged images have exact prompts and original generated output paths recorded. Intermediate contact-sheet labels were shortened and split into two lines to keep long variant names readable.

### Extended character and vehicle review

Reviewed the completed soldier variants, including night scenes, on the generated outputs and shortened-label sheets 12–13. At 187 images, 57 Primary cutouts were reviewed at 64 px on dark backing. Heavy and tracked APCs and the drone Primary/Card images were then reviewed in generated outputs. At 195 candidates the audit passed with no framing issues, and every completed image has its prompt and original generated path recorded. Runtime application remains pending complete coverage.

### Aircraft and late vehicle corrections

A tiny invented signature in the transport helicopter Action scene was rejected and removed through a targeted built-in ImageGen edit. Original rejected image: `Rejected/transport-action-signature.png`. The revised output was inspected. Lower-right corner montages of the 146 opaque completed scenes were reviewed; no other writing was found in the first 144 crops.

The light armored car Primary failed framing with 25 opaque border pixels at its right bumper. Rejected image: `Rejected/light-armored-car-cropped.png`. A targeted built-in ImageGen framing edit restored the complete bumper with a transparent margin. The revised output was inspected and the 219-candidate audit passed. These failures remain recorded; they were repaired before runtime application.

### Final coverage and tray framing repair

The final cargo tray truck Primary had 20 opaque border pixels at the right bumper. Its rejected candidate is retained in `Rejected/tray-truck-cropped.png`. A targeted built-in ImageGen edit restored the full bumper and transparent margin. The replacement was inspected before application. All 238 final candidates pass the transparency, square-image, silhouette-border and metadata audit with no issues. Final corner montages cover all 164 opaque images; no remaining signature was found.

### Post-application validation

`application-audit.json` records all 238 changed runtime images and unchanged metadata. `native-hud-01.log` passed with `[ComicPortraitRuntime]`, `[ComicPortraitNativeHud]` and `[ExistingEditorValidation]` pass markers and exit receipt 0. Captures are in `NativeHud/`; the baseline is in `BeforeHud/`. These static captures are separate from normal-input mission and device/player acceptance, which remain pending.
