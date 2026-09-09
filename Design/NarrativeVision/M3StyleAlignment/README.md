# M3 art style alignment — 2026-09-09

Design-folder review images referenced here remain local and are excluded from Git at the user's request. Production game artwork under `Assets` is included.

The preferred M3 art is the canonical direction. Earlier scenes now use its finer facial modelling, richer material detail and warm faceted lighting. Samira, Dalia and ARIA were matched to their M3 references.

## Installed scope

- 22 FirstLaunch/M1 scenes, installed into both existing aspect asset paths (44 PNGs).
- Three M2 scenes and three narrative portraits.
- Main-menu field commander scene, shared by the commander profile and campaign operations builders.
- Commander atlas: all six selectable portraits plus the faceless fallback, retaining the existing sprite rectangles.
- Shared ARIA UI portrait with actual transparent alpha, plus the ARIA Field Trials event illustration. Existing references pick up the replacement in menu, narrative, HUD and other UI consumers.

Total: 54 installed PNG replacements. The seven M3 source panels remain byte-identical. Asset GUIDs and sprite bindings are retained. Earlier story beats and cast roles remain intact. FirstLaunch wide assets use the same source image as the standard assets; the existing EnvelopeParent aspect fitter provides viewport cropping. FL-P11 and FL-P14 received targeted composition corrections to keep Dalia's face clear of the wide-screen transport overlay.

## Editor evidence

Final run 03 used `Tools/CI/invoke_unity_macos.sh` in GUI licensing mode with a 600-second timeout and exited 0. The helper rendered the real narrative prefab and authored dialogue, verified all six selected indices and all seven atlas bounds, and generated English/Persian variants at 1920×1080 and 2400×1080. It passed 180 caption checks, including Persian text presence and caption overflow/truncation assertions.

`EditorQA/` contains 200 narrative/selection captures and four main-menu captures from run 03. Art candidates were visually inspected; final layout review sampled main menus, the six-card picker, closeups, M2 exchanges and the corrected FL-P11/FL-P14 framing. This is art integration evidence, not an exhaustive visual inspection of every screenshot or a full M3 gameplay, audio or performance pass. Android validation was excluded by user instruction. The Editor emitted a persistent-allocation shutdown warning; this art run does not diagnose memory ownership.

`EditorQA-01-Draft/` is superseded evidence: its Persian resolver was unloaded between scenes, and the selection loop included the fallback index. The validation helper was corrected and the final run checks real Persian captions and exactly six selectable cards. Run 02 is superseded by run 03 after the final FL-P11 framing change.

## Existing UI issues found during review

- The fourth gray-haired male commander is labelled “Dalia Rahim,” while story Dalia is a woman. The choice between using story Dalia's portrait or giving the male commander a distinct name is pending user direction; the restyle preserved the original selectable identity.
- The selected-profile name/role/blurb remain static when selecting another card. The portrait and selected index update correctly.
- Some commander-picker copy and the narrative NEXT button remain English in Persian mode. Caption validation does not imply complete UI localization.

These existing text/identity issues remain open. This art pass does not supersede earlier M3 gameplay QA findings.

## Provenance

- `visual_direction.md`: reference identity and rendering rules.
- `generation_manifest.json` and `ui_generation_manifest.json`: exact generation prompts, references, candidates, corrections and installation paths.
- `original_inventory.json` and `ui_original_inventory.json`: original content and metadata hashes.
- `final_evidence.json`: installed content/metadata hashes, retained M3 hashes, final capture hashes and run marker.
- `validation-result.txt`: compact final result; full log is `/private/tmp/warline-m3-style-alignment-03.log`.

Candidates and final game assets are saved inside this workspace. No art changes have been committed or pushed in this pass; the working tree also contains pre-existing M3 implementation work.
