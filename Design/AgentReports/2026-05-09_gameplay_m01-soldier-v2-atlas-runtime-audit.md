# Lane
Gameplay

# Task
Audit the Art/Atlas v2 M01 soldier animation atlases for runtime import suitability before PM/user acceptance and before any Gameplay integration.

# Files changed
- `Design/AgentReports/2026-05-09_gameplay_m01-soldier-v2-atlas-runtime-audit.md`
- Refreshed capture evidence:
  - `Design/AgentReports/Captures/2026-05-08_m01-public-launch/campaign-public-m01.png`
  - `Design/AgentReports/Captures/2026-05-08_m01-public-launch/campaign-public-m01-20x9.png`

# Contracts touched
- No runtime code or source contract files changed.
- Audited against:
  - `Design/AgentTasks/gameplay_current.md`
  - `Design/AgentReports/2026-05-09_art-atlas_m01-ai-production-asset-pack.md`
  - `Design/AgentReports/2026-05-09_art-atlas_m01-soldier-animation-atlas-fix-v2.md`
  - `Design/AgentReports/2026-05-09_pm_soldier-v2-preacceptance-audit-routing.md`
  - `Design/AgentReports/2026-05-09_designer_m01-soldier-v2-animation-aaa-audit.md`
  - `Assets/Game/Art/Generated/2DISO/Chapter01/M01_AIProduction/Manifests/m01_ai_production_asset_manifest.json`
  - `Assets/Game/Art/Generated/2DISO/Chapter01/M01_AIProduction/Manifests/m01_soldier_animation_manifest_v2.json`
  - Approved references:
    - `Design/VisualTargets/Gameplay/M01_SelectedReadability/M01_SelectedReadability_Gameplay_Target.png`
    - `Design/VisualTargets/Gameplay/M01_SelectedReadability/M01_SelectedReadability_Isometric_Grid_Proof.png`

# User-visible behavior
No gameplay behavior changed. V2 soldier atlases were not integrated.

The current refreshed public M01 runtime capture still shows the pre-v2 runtime composition and does not yet match the approved true-isometric visual target package or the approved AI production asset pack style. That mismatch is expected because this task is an audit gate, not runtime integration.

# Validation run
- Read all required Gameplay, PM, Art/Atlas, and Designer reports.
- Parsed `m01_soldier_animation_manifest_v2.json`.
- Checked runtime path existence for every manifest-referenced v2 soldier frame and atlas.
- Inspected `Design/VisualLock/Gameplay/M01_AIProductionAssets/ContactSheets/m01_soldier_animation_contact_v2.png`.
- Ran image audits with Python/Pillow:
  - v2 atlas dimensions,
  - frame counts,
  - frame dimensions,
  - alpha/corner transparency,
  - bounding boxes,
  - foot/contact Y consistency,
  - adjacent run-frame image deltas,
  - atlas rect stability and gutter/padding presence,
  - Unity `.meta` presence.
- Main project Unity batchmode was blocked because the project was already open, so capture validation used `/Users/farhad/Projects/WarlineCapture-CodexUnity1`.
- Public route capture validation:
  `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -projectPath /Users/farhad/Projects/WarlineCapture-CodexUnity1 -runTests -testPlatform PlayMode -testFilter Chapter01M01PlayModeValidationTests.PublicCampaignLaunch_ReachesM01ProductionVisibleSlice -testResults /private/tmp/warlinecapture-gameplay-v2-atlas-audit-capture-results.xml -logFile /private/tmp/warlinecapture-gameplay-v2-atlas-audit-capture.log`

# Validation result
needs manifest/layout fixes before Gameplay integration

The v2 package is not blocked for missing images or repeated-pose failure, but it is not yet ready for direct runtime import.

Positive runtime-audit findings:
- All manifest-referenced v2 runtime frame PNGs exist.
- Frame coverage is complete: 2 factions, 4 facings, 6 states, 224 total 256x256 frame PNGs.
- Player and enemy atlases are separate, which is correct for faction tint/readability and avoids mixed-faction lookup.
- Both runtime v2 atlas PNGs exist at `4096x1792`.
- All sampled frames are 256x256 RGBA with non-empty alpha.
- Alpha-transparent corners passed for all audited frames.
- Manifest contains state/facing/frame order, fps, loop flags, atlas rects, runtime paths, and review paths.
- Adjacent run-frame deltas are non-zero. The rejected repeated-pose issue is materially improved.
- Foot/contact bottom Y is stable at `242` across audited frames, which is useful for a derived foot-anchor convention.
- Refreshed public route capture test passed `1/1`, `0` failed.

Blocking runtime/import issues before integration:
- Unity `.meta` files are missing for all v2 soldier runtime assets audited: `226/226` v2 atlas/frame PNGs have no `.meta` files. This means importer settings, GUIDs, compression, mipmaps, alpha handling, max texture size, and Sprite import mode are not source-controlled.
- The v2 manifest files also have no `.meta` files.
- The v2 atlases are `4096x1792`: width is POT, height is NPOT. Unity can generally import NPOT textures, but for mobile import reproducibility this needs explicit importer settings. If mips are enabled or compression differs by platform, this is a risk.
- Atlas rects are exact 256x256 tiles with no gutter/extrude padding. The frame art has transparent edge space, so this is not automatically broken, but it is a bilinear/mipmap bleeding risk unless import settings disable mipmaps or the atlas is repacked with padding/extrusion.
- Manifest metadata is not sufficient for robust ECS atlas animation lookup by itself because it lacks explicit per-frame pivot, foot anchor, contact bounds, and normalized sprite bounds. These can be derived from alpha now, but deriving at runtime or during every import is unnecessary fragility.
- Several contact-sheet rows show small keying/artifact speckles or guide-like fragments around some aim/damaged/run frames. They do not break the image audit, but QA should inspect after import at gameplay scale.

# Runtime capture comparison
- Refreshed public route capture:
  - `Design/AgentReports/Captures/2026-05-08_m01-public-launch/campaign-public-m01.png` (`1280x720`)
  - `Design/AgentReports/Captures/2026-05-08_m01-public-launch/campaign-public-m01-20x9.png` (`1600x720`)
- Approved reference target:
  - `Design/VisualTargets/Gameplay/M01_SelectedReadability/M01_SelectedReadability_Gameplay_Target.png` (`1600x900`)
  - `Design/VisualTargets/Gameplay/M01_SelectedReadability/M01_SelectedReadability_Isometric_Grid_Proof.png` (`1600x900`)

Current runtime does not match the approved reference style yet:
- image/background/map: does not match; current runtime still uses the pre-production tactical scene/map style, not the approved AI production tactical map plate/reference.
- soldiers: does not match v2 package; current runtime still uses the prior selected-readability soldier presentation, not the v2 atlases.
- markers: partially match the prior Gameplay selected-readability fix, but not the approved AI production marker pack as a final reference comparison.
- overall style: does not match the approved AAA true-isometric reference yet; this should be expected until the accepted asset pack is integrated.

# Detailed audit data
- V2 atlas sizes:
  - `player_rifle_squad_animation_atlas_v2.png`: `4096x1792`, no `.meta`
  - `enemy_patrol_animation_atlas_v2.png`: `4096x1792`, no `.meta`
- Frame counts:
  - player rifle squad: `112`
  - enemy patrol: `112`
  - total: `224`
- Missing runtime frame paths: `0`
- Wrong frame sizes: `0`
- Empty alpha frames: `0`
- Non-transparent corner samples: `0`
- Missing `.meta` for v2 atlas/frame PNGs: `226`
- Missing `.meta` for v2 manifests: present for neither `m01_soldier_animation_manifest_v2.json` nor `.md`
- Atlas rects:
  - player: `112` unique rects, all exact 256x256 tiles, no gutters
  - enemy: `112` unique rects, all exact 256x256 tiles, no gutters
- Adjacent run-frame mean RGBA deltas:
  - player facings: min/avg/max ranges across facings roughly `4.51-16.53`
  - enemy facings: min/avg/max ranges across facings roughly `2.15-12.71`

# Recommendation
needs manifest/layout fixes before integration

Do not integrate v2 into Gameplay yet.

Required fixes before Gameplay imports:
- Add Unity `.meta` files or route a Unity import pass that creates stable source-controlled metas for every v2 PNG and manifest.
- Define importer settings for mobile explicitly: texture type/sprite mode, alpha is transparency, compression target, max size, mipmaps, filter mode, wrap mode clamp, and Android/iOS overrides.
- Add per-frame or per-sequence pivot/foot-anchor/contact-bounds metadata to `m01_soldier_animation_manifest_v2.json`. Suggested initial anchor is center X `128`, foot Y `242`, derived from the current alpha audit, but it should be explicit.
- Decide atlas layout policy:
  - keep separate player/enemy atlases for current M01, which is acceptable;
  - either keep the `4096x1792` atlases with mipmaps disabled and clamp/filter settings, or repack to `4096x2048`/state-split atlases with gutters if platform import policy requires POT/padded textures;
  - add 2-4 px extrusion/gutter if mipmaps or bilinear minification will be used.
- Preserve the manifest state/facing/fps/loop data; it is useful and should remain the ECS animator lookup source after the missing anchor/import metadata is added.

# Known gaps
- Main project Unity could not run batchmode because another Unity instance had `/Users/farhad/Projects/WarlineCapture` open. Capture validation used `/Users/farhad/Projects/WarlineCapture-CodexUnity1`, per available validation mirror practice.
- This audit did not integrate v2 or validate live v2 playback in ECS; integration is explicitly blocked pending PM/user acceptance and the runtime import fixes above.
- The current public M01 capture is only a baseline comparison and does not prove v2 visual quality in runtime.
- Visual/art final approval remains PM/user and Art/Atlas scope.

# Cross-lane impacts
- Art/Atlas should provide the missing import/metadata/layout fixes or explicitly hand ownership of Unity import/meta generation to Gameplay.
- PM should not accept v2 for Gameplay integration until the missing `.meta` and anchor/import metadata issues are resolved or consciously assigned to Gameplay.
- QA/HCI should wait for an integrated v2 runtime capture/video before final selected-readability approval.
- Designer's v2 audit can remain accepted visually, but Gameplay runtime acceptance is not ready.

# Next recommended task
PM should route v2 back for manifest/import-layout cleanup, or explicitly assign Gameplay to perform a Unity import/meta generation pass and manifest-anchor augmentation. After that, Gameplay can integrate the v2 atlas into the ECS atlas animator and produce runtime playback capture evidence.
