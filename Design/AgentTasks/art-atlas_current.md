# Art/Atlas Current Task

Date: 2026-05-08
Status: waiting
Priority: waiting for PM/user temporary-art decision after accepted QA/HCI final rerun

## Assignment

Wait for PM/user to approve or reject the temporary M01 infantry art package. QA/HCI has confirmed the route is stable enough for review.

Art/Atlas has completed the current assessment. Do not regenerate or broaden art work unless PM/user rejects the temporary package after a valid review, or PM requests a specific follow-up such as an enemy variant or final VFX package.

Do not infer broader art work, M02 content, vehicle art, base/build art, or unrelated polish.

## Current Handoff

- `Design/AgentReports/2026-05-08_art-atlas_m01-infantry-atlas-readiness.md`
- `Design/AgentReports/2026-05-08_art-atlas_post-gameplay-readability-watch.md`

## Waiting On

Waiting on lane:
PM/user

Waiting on exact decision:
approve or reject temporary Gate 4 infantry art

Owner of next action:
PM/user

Can my lane still continue fallback work? no

## After PM/User Decision

PM may ask the user to approve or reject `Assets/Game/Art/Generated/2DISO/Units/Unit_Chr_Soldier_Male_02/SpriteSheets/Transparent/Unit_Chr_Soldier_Male_02_FullSetup_4Facing_8State_UnityGrid_960x1680.png` as the temporary Gate 4 M01 infantry atlas source.

## If Approved

Stay waiting unless Gameplay or QA/HCI requests a concrete Art/Atlas follow-up.

## If Rejected Or Partially Approved

Prepare the requested replacement or follow-up package, limited to the explicit decision:

- final or milestone player infantry atlas frames
- enemy red-accent/tinted patrol variant
- tactical impact/projectile VFX art
- destroyed/death atlas-state art
- selected-state art treatment

## Runtime Constraints

Art/Atlas assets must support the accepted ECS atlas presentation path:

- compatible with Gameplay's `MissionRuntimeAtlasQuadRuntime` / ECS atlas animator path
- no dependency on visible legacy `Model` children
- no dependency on `MissionRuntimeSpriteRendererRuntime` as final M01 presentation
- no dependency on old per-Model `MaterialAnimationIndex` output as final public M01 infantry animation
- no separate child `Destroyed` visual requirement

## Current Accepted Inputs

Read first:

- `Design/AgentReports/2026-05-08_qa-hci_gate4-focused-rerun.md`
- `Design/AgentReports/2026-05-08_pm_qa-hci-gate4-focused-rerun-review.md`
- `Design/AgentTasks/gameplay_current.md`
- `Design/AgentTasks/M01_CRITICAL_PATH.md`
- `Design/WarlineCapture_M01_FirstContact_Production_Contract.md`

## Validation Required

No new validation is required while waiting. If PM/user requests new Art/Atlas work, validate by producing a focused report under `Design/AgentReports` with exact files/assets/captures, readiness state, public camera-scale notes, cross-lane integration requirements, and any remaining approval decision.

## Cross-Lane Notes

- Gameplay owns runtime integration, state switching, squad composition, and selected marker implementation.
- UI owns the separate infantry-only HUD affordance mismatch.
- QA/HCI owns final Gate 4 validation after Gameplay proof and any PM/user art decision.
- PM/user owns final art approval only after the manual M01 review route is stable enough to inspect.

## Completion Report

If PM/user requests follow-up work, write the report to:

`Design/AgentReports/2026-05-08_art-atlas_<specific-followup>.md`

Use the exact format from `Design/WarlineCapture_Agent_Coordination_Workflow.md`, and include:

- Lane
- Task
- Files changed
- Contracts touched
- User-visible behavior
- Validation run
- Validation result
- Known gaps
- Cross-lane impacts
- Next recommended task
