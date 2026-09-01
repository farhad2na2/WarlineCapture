# SCN-00 First Launch — Runtime Iteration 4

Status: review candidate only. This iteration is not accepted until the user
explicitly approves it.

Target locks:

- `../../reference/SCN-00_FirstLaunchV3_LanguageChoice_Final_Target.png`
- `../../reference/SCN-00_FirstLaunchV3_CommanderIdentity_Final_Target.png`
- `../../reference/SCN-00_FirstLaunchV3_ComicPlayback_Final_Target.png`
- `../../reference/SCN-00_FirstLaunchV3_ARIAGuidance_Final_Target.png`

Runtime evidence:

- `language_choice_v3_16x9.png` and `language_choice_v3_20x9.png`
- `commander_identity_v3_16x9.png` and `commander_identity_v3_20x9.png`
- `comic_playback_v3_16x9.png` and `comic_playback_v3_20x9.png`
- `aria_guidance_v3_16x9.png` and `aria_guidance_v3_20x9.png`

Implemented corrections:

- rebuilt the functional language, identity, comic, and ARIA guidance states
  against the four V3 target locks
- changed language selection to select first and confirm with Continue
- added procedural directional gradients, independent constant 3px borders,
  selection washes, corner caps, and check marks
- replaced flat or mismatched controls with sharp target-type globe, role,
  playback, subtitle, navigation, map, crosshair, and motion symbols
- generated a new reusable ARIA portrait and preserved its aspect ratio
- kept portraits and comic panels aspect-preserved at both capture sizes
- moved the language backdrop outside the locked composition so it covers the
  full ultrawide canvas without stretching or leaving black gutters
- reused the existing shared V3 sprite atlases without screen-local duplicate
  icon files

Validation evidence:

- `[FirstLaunchNarrativeV3PrefabBuilder] validation=Passed language=select-then-continue identity=6 comic=complete guidance=complete`
- `[FirstLaunchNarrativeV3PrefabBuilder] result=Passed screens=4 layout=1672x941 gradients=procedural borders=3 atlases=shared`
- `[FirstLaunchNarrativeV3PrefabBuilder] capture=Passed size=1920x1080 suffix=16x9`
- `[FirstLaunchNarrativeV3PrefabBuilder] capture=Passed size=4800x2160 suffix=20x9`
- `[FirstLaunchNarrativeV3PlayModeCapture] result=Passed states=4 requested=1920x1080 suffix=16x9`
- `[FirstLaunchNarrativeV3PlayModeCapture] result=Passed states=4 requested=4800x2160 suffix=20x9`
- `[V3SharedBrandLogoMigrationBuilder] result=Passed prefabs=17 references=18 ... duplicate=0`

Editor preview and shared-logo correction (2026-09-01):

- Root cause of the tiny Edit Mode preview: the authored `1672x941`
  composition stayed at scale `1` while the Game view remained at
  `4800x2160`.
- `MainMenuV3SectionLayoutView` now executes in Edit Mode and drives its
  preview RectTransform properties through `DrivenRectTransformTracker`, so
  the reference composition fills the current canvas without creating prefab
  overrides or dirtying `Menu.unity`.
- The live capture command restores the Game view to `1920x1080` after both
  proof sizes and no longer closes the user's Editor.
- Every V3 screen now references one canonical high-resolution
  `shared_brand_logo_lockup.png` sprite. It is packed only in
  `UI_V3_Brand_01.spriteatlas`; screen-specific builders no longer recreate
  the logo from text/rails/chevrons or repack another logo image.
- `V3UiFoundationBuilder.EnsureBuilt()` validates existing shared atlases first
  and only performs a full reimport if validation fails. This keeps visual
  iteration fast while preserving fail-closed atlas checks.

Current live proof files:

- `/private/tmp/warline-first-launch-live-language-v3-16x9.png`
- `/private/tmp/warline-first-launch-live-comic-v3-16x9.png`
- `/private/tmp/warline-first-launch-live-identity-v3-16x9.png`
- `/private/tmp/warline-first-launch-live-guidance-v3-16x9.png`
- `/private/tmp/warline-first-launch-live-language-v3-20x9.png`
- `/private/tmp/warline-first-launch-live-comic-v3-20x9.png`
- `/private/tmp/warline-first-launch-live-identity-v3-20x9.png`
- `/private/tmp/warline-first-launch-live-guidance-v3-20x9.png`

Review notes:

- 16:9 fills the frame and retains the four authored V3 compositions.
- 20:9 keeps all interactive UI inside the centered safe composition while
  non-stretched background art fills or frames the side area as authored.
- Selection borders never intersect adjacent cards or footer panels.
- ARIA, commanders, and comic artwork do not stretch at either aspect ratio.
